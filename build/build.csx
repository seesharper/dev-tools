#!/usr/bin/env dotnet-script
#load "build-context.csx"
#load "nuget:Dotnet.Build, 0.3.9"
#load "nuget:github-changelog, 0.1.5"
using static ChangeLog;
using static ReleaseManagement;

var rootFolder = FileUtils.GetScriptFolder();
var pathToDepsTests = Path.Combine(rootFolder, "../src/test/deps.tests.csx");

DotNet.Test(pathToDepsTests);

var pathToDepsScript = Path.Combine(rootFolder, "../src/deps.csx");
var pathToMakeGlobalToolScript = Path.Combine(rootFolder, "../src/make-global-tool.csx");

Command.Execute("make-global-tool",$"{pathToDepsScript} {File.ReadAllText(Path.Combine(rootFolder, "deps.args"))} -o {Path.Combine(rootFolder, "Artifacts", "NuGet")}");

Command.Execute("make-global-tool",$"{pathToMakeGlobalToolScript} {File.ReadAllText(Path.Combine(rootFolder, "make-global-tool.args"))} -o {Path.Combine(rootFolder, "Artifacts", "NuGet")}");




if (BuildEnvironment.IsSecure)
{
    await CreateReleaseNotes();
    if (Git.Default.IsTagCommit())
    {
        Git.Default.RequreCleanWorkingTree();
        await ReleaseManagerFor(owner, projectName,BuildEnvironment.GitHubAccessToken)
            .CreateRelease(Git.Default.GetLatestTag(), pathToReleaseNotes, Array.Empty<ReleaseAsset>());
            NuGet.TryPush(nuGetArtifactsFolder);
    }
}

private async Task CreateReleaseNotes()
{
    Logger.Log("Creating release notes");
    var generator = ChangeLogFrom(owner, projectName, BuildEnvironment.GitHubAccessToken).SinceLatestTag();
    if (!Git.Default.IsTagCommit())
    {
        generator = generator.IncludeUnreleased();
    }
    await generator.Generate(pathToReleaseNotes);
}