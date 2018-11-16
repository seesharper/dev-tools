#!/usr/bin/env dotnet-script
#load "console.csx"
#r "nuget:NuGet.Configuration, 4.5.0"
#r "nuget:NuGet.Packaging.Core.Types, 4.2.0"
#r "nuget:NuGet.Protocol.Core.v3, 4.2.0"
#r "nuget:NuGet.Packaging, 4.2.0"
#r "nuget:NuGet.Packaging.Core, 4.2.0"
#r "nuget:McMaster.Extensions.CommandLineUtils, 2.2.5"
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using McMaster.Extensions.CommandLineUtils;

var app = new CommandLineApplication();
var filterOption = app.Option("-f | --filter", "Filter packages", CommandOptionType.SingleValue);

var updateCommand = app.Command("update", c => {});

updateCommand.OnExecute(() => UpdatePackages());

app.OnExecute(() => ListPackages(filterOption.Value()));


return app.Execute(Args.ToArray());

private async Task<int> ListPackages(string filter)
{
    var rootFolder = Directory.GetCurrentDirectory();

    var packageReferences = GetPackageReferences(filter, rootFolder);
    var latestVersions = await GetLatestVersions(packageReferences.Select(r => r.Name).Distinct().ToArray(), rootFolder);
    var map = latestVersions.ToDictionary(v => v.PackageName);
    foreach (var packageReference in packageReferences)
    {
        var latestVersion = map[packageReference.Name];
        if (latestVersion.NugetVersion > packageReference.Version)
        {
            WriteHighlighted($"{packageReference.Name} {packageReference.Version} => {latestVersion.NugetVersion} ({latestVersion.Feed})");
        }
    }
    return 0;
}

private int UpdatePackages(){
    // TODO updates packages
    WriteLine("Update");
    return 0;
}

private PackageReference[] GetPackageReferences(string filter, string rootFolder)
{
    var projectFiles = Directory.GetFiles(rootFolder, "*.csproj", SearchOption.AllDirectories);

    var packageReferences = new List<PackageReference>();
    foreach (var projectFile in projectFiles)
    {
        WriteNormal($"Analyzing {projectFile}");
        var content = File.ReadAllText(projectFile);
        var packagePattern = "PackageReference Include=\"([^\"]*)\" Version=\"([^\"]*)\"";
        var matcher = new Regex(packagePattern);
        var matches = matcher.Matches(content);
        foreach (var match in matches.Cast<Match>())
        {
            var packageReference = new PackageReference(match.Groups[1].Value, projectFile, NuGetVersion.Parse(match.Groups[2].Value));
            packageReferences.Add(packageReference);
        }
    }
    return packageReferences.ToArray();
}

private class PackageReference
{
    public PackageReference(string name, string projectFile ,NuGetVersion version)
    {
        Name = name;
        ProjectFile = projectFile;
        Version = version;
    }

    public string Name { get; }
    public string ProjectFile { get; }
    public NuGetVersion Version { get; }
}

private class LatestVersion
{
    public LatestVersion(string packageName, NuGetVersion nugetVersion, string feed)
    {
        PackageName = packageName;
        NugetVersion = nugetVersion;
        Feed = feed;
    }

    public string PackageName { get; }
    public NuGetVersion NugetVersion { get; }
    public string Feed { get; }
}

private async Task<LatestVersion[]> GetLatestVersions(string[] packageReferences, string rootFolder)
{
    WriteHighlighted($"Getting the latest package versions. Hang on.....");

    var sourceRepositories = GetSourceRepositories(rootFolder);

    List<LatestVersion> latestVersions = new List<LatestVersion>();
    foreach (var packageReference in packageReferences)
    {
        latestVersions.Add(await GetLatestVersion(packageReference, sourceRepositories));
    }

    return latestVersions.ToArray();
}

private async Task<LatestVersion> GetLatestVersion(string packageName, SourceRepository[] repositories)
{
    List<LatestVersion> allVersions = new List<LatestVersion>();
    foreach (var repository in repositories)
    {
        var searchResource = repository.GetResource<PackageSearchResource>();
        var results = await searchResource.SearchAsync(packageName, new SearchFilter(false), 0,
                        int.MaxValue,  NullLogger.Instance, CancellationToken.None);
        var matches = results.Where(m => m.Identity.Id == packageName);
        foreach (var match in matches)
        {
            var versions = await match.GetVersionsAsync();
            allVersions.AddRange(versions.Select(v => new LatestVersion(packageName,v.Version,repository.ToString())));
        }
    }
    return allVersions.OrderBy(v => v.NugetVersion).Last();
}

private SourceRepository[] GetSourceRepositories(string rootFolder)
{
    var provider = GetSourceRepositoryProvider(rootFolder);
    var repositories = provider.GetRepositories().ToArray();
    WriteNormal("Feeds");
    WriteLine();
    foreach (var repository in repositories)
    {
        WriteNormal($" * {repository.PackageSource.ToString()}");
    }
    WriteLine();

    return repositories.ToArray();
}


private ISourceRepositoryProvider GetSourceRepositoryProvider(string rootFolder)
{
    var settings = global::NuGet.Configuration.Settings.LoadDefaultSettings(rootFolder);
    return new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());
}