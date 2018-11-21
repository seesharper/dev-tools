#!/usr/bin/env dotnet-script
#load "console.csx"
#load "command.csx"
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
using System.Collections.Concurrent;

var app = new CommandLineApplication();
var filterOption = app.Option("-f | --filter", "Filter packages to be processed.", CommandOptionType.SingleValue);

var updateCommand = app.Command("update", c => {
    c.Description = "Updates packages to their latest versions";
});

updateCommand.OnExecute(() => ProcessPackages(filterOption.Value(), update:true));

app.OnExecute(() => ProcessPackages(filterOption.Value(), update: false));

var helpOption = app.HelpOption("-h | --help");

return app.Execute(Args.ToArray());

private async Task<int> ProcessPackages(string filter, bool update)
{
    var rootFolder = Directory.GetCurrentDirectory();

    var packageReferences = GetPackageReferences(filter, rootFolder);
    var latestVersions = await GetLatestVersions(packageReferences.Select(r => r.Name).Distinct().ToArray(), rootFolder);
    var packageReferencesGroupedByProject = packageReferences.Where(p => latestVersions[p.Name].NugetVersion > p.Version).GroupBy(p => p.ProjectFile);

    foreach (var grouping in packageReferencesGroupedByProject)
    {
        if (grouping.Count() == 0)
        {
            continue;
        }

        WriteHeader(Path.GetRelativePath(rootFolder,grouping.Key));
        WriteLine();
        foreach (var packageReference in grouping)
        {
            var latestVersion = latestVersions[packageReference.Name];

            if (update)
            {
                Command.Capture("dotnet", $"add {packageReference.ProjectFile} package {packageReference.Name}").EnsureSuccessfulExitCode();
                WriteHighlighted($"{packageReference.Name} {packageReference.Version} => {latestVersion.NugetVersion} ({latestVersion.Feed}) \u2705");
            }
            else
            {
                WriteHighlighted($"{packageReference.Name} {packageReference.Version} => {latestVersion.NugetVersion} ({latestVersion.Feed})");
            }
        }

        WriteLine();
    }
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
            var packageName = match.Groups[1].Value;
            var version = NuGetVersion.Parse(match.Groups[2].Value);
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (packageName.StartsWith(filter))
                {
                    packageReferences.Add(new PackageReference(packageName, projectFile, version));
                }
            }
            else
            {
                packageReferences.Add(new PackageReference(packageName, projectFile, version));
            }
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

private async Task<IDictionary<string, LatestVersion>> GetLatestVersions(string[] packageNames, string rootFolder)
{
    WriteHighlighted($"Getting the latest package versions. Hang on.....");

    var sourceRepositories = GetSourceRepositories(rootFolder);

    var result = new ConcurrentBag<LatestVersion>();

    await Task.WhenAll(packageNames.Select(name => GetLatestVersion(name, sourceRepositories, result)));

    return result.ToDictionary(v => v.PackageName);
}

private async Task GetLatestVersion(string packageName, SourceRepository[] repositories, ConcurrentBag<LatestVersion> result)
{
    List<LatestVersion> allLatestVersions = new List<LatestVersion>();
    foreach (var repository in repositories)
    {
        var findResource = repository.GetResource<FindPackageByIdResource>();
        var allVersions = await findResource.GetAllVersionsAsync(packageName, new SourceCacheContext(), NullLogger.Instance, CancellationToken.None);
        var latestVersionInRepository = allVersions.Where(v => !v.IsPrerelease).LastOrDefault();
        if (latestVersionInRepository != null)
        {
            allLatestVersions.Add(new LatestVersion(packageName, latestVersionInRepository, repository.ToString()));
        }
    }

    result.Add(allLatestVersions.OrderBy(v => v.NugetVersion).Last());
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