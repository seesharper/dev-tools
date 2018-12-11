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

public static int Execute(params string[] args)
{
    var app = new CommandLineApplication();
    var filterOption = app.Option("-f | --filter", "Filter packages to be processed.", CommandOptionType.SingleValue);

    var updateCommand = app.Command("update", c => {
        c.Description = "Updates packages to their latest versions";
    });

    var versionOption = app.VersionOption("-v | --version", "1.0.1");
    var cwd = app.Option("-cwd |--workingdirectory <currentworkingdirectory>", "Working directory for the code compiler. Defaults to current directory.", CommandOptionType.SingleValue);
    var preReleaseOption = app.Option("-p || --pre ", "Allow prerelease packages", CommandOptionType.NoValue);


    updateCommand.OnExecute(() =>
    {
        var workingDirectory = cwd.HasValue() ? cwd.Value() : Directory.GetCurrentDirectory();
        return ProcessPackages(workingDirectory,filterOption.Value(), preReleaseOption.HasValue(),update:true);
    });



    app.OnExecute(() =>
    {
        var workingDirectory = cwd.HasValue() ? cwd.Value() : Directory.GetCurrentDirectory();
        return ProcessPackages(workingDirectory, filterOption.Value(), preReleaseOption.HasValue(), update: false);
    });

    var helpOption = app.HelpOption("-h | --help");

    return app.Execute(args);
}



private static async Task<int> ProcessPackages(string rootFolder, string filter, bool preRelease, bool update)
{
    var packageReferences = GetPackageReferences(filter, rootFolder);
    var latestVersions = await GetLatestVersions(packageReferences.Select(r => r.Name).Distinct().ToArray(), rootFolder, preRelease);
    var packageReferencesGroupedByProject = packageReferences.Where(p => !latestVersions[p.Name].IsValid || latestVersions[p.Name].NugetVersion > p.Version).GroupBy(p => p.ProjectFile);

    foreach (var grouping in packageReferencesGroupedByProject)
    {
        WriteHeader(Path.GetRelativePath(rootFolder,grouping.Key));
        WriteLine();
        foreach (var packageReference in grouping)
        {
            var latestVersion = latestVersions[packageReference.Name];
            if (!latestVersion.IsValid)
            {
                WriteError($"Unable to find package {packageReference.Name} ({packageReference.Version})");
                continue;
            }
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

private static PackageReference[] GetPackageReferences(string filter, string rootFolder)
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
                    packageReferences.Add(new PackageReference(packageName.Trim(), projectFile, version));
                }
            }
            else
            {
                packageReferences.Add(new PackageReference(packageName.Trim(), projectFile, version));
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

    public LatestVersion(string packageName) => PackageName = packageName;

    public string PackageName { get; }
    public NuGetVersion NugetVersion { get; }
    public string Feed { get; }

    public bool IsValid { get => !string.IsNullOrWhiteSpace(Feed); }
}

private static async Task<IDictionary<string, LatestVersion>> GetLatestVersions(string[] packageNames, string rootFolder, bool preRelease)
{
    WriteHighlighted($"Getting the latest package versions. Hang on.....");

    var sourceRepositories = GetSourceRepositories(rootFolder);

    var result = new ConcurrentBag<LatestVersion>();

    await Task.WhenAll(packageNames.Select(name => GetLatestVersion(name, preRelease, sourceRepositories, result)));

    return result.ToDictionary(v => v.PackageName);
}

private static async Task GetLatestVersion(string packageName, bool preRelease, SourceRepository[] repositories, ConcurrentBag<LatestVersion> result)
{
    List<LatestVersion> allLatestVersions = new List<LatestVersion>();
    foreach (var repository in repositories)
    {
        var findResource = repository.GetResource<FindPackageByIdResource>();
        var allVersions = await findResource.GetAllVersionsAsync(packageName, new SourceCacheContext(), NullLogger.Instance, CancellationToken.None);
        NuGetVersion latestVersionInRepository;

        if (preRelease)
        {
            latestVersionInRepository = allVersions.LastOrDefault();
        }
        else
        {
            latestVersionInRepository = allVersions.Where(v => !v.IsPrerelease).LastOrDefault();
        }

        if (latestVersionInRepository != null)
        {
            allLatestVersions.Add(new LatestVersion(packageName, latestVersionInRepository, repository.ToString()));
        }
    }
    if (!allLatestVersions.Any())
    {
        result.Add(new LatestVersion(packageName));
    }
    else
    {
        result.Add(allLatestVersions.OrderBy(lv => lv.NugetVersion).Last());
    }


}

private static SourceRepository[] GetSourceRepositories(string rootFolder)
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

private static ISourceRepositoryProvider GetSourceRepositoryProvider(string rootFolder)
{
    var settings = global::NuGet.Configuration.Settings.LoadDefaultSettings(rootFolder);
    return new SourceRepositoryProvider(settings, Repository.Provider.GetCoreV3());
}