#load "nuget:Dotnet.Build, 0.3.9"
using static FileUtils;

var owner = "seesharper";
var projectName = "dev-tools";
var root = FileUtils.GetScriptFolder();

var artifactsFolder = CreateDirectory(root, "Artifacts");
var gitHubArtifactsFolder = CreateDirectory(artifactsFolder, "GitHub");
var nuGetArtifactsFolder = CreateDirectory(artifactsFolder, "NuGet");

var pathToReleaseNotes = Path.Combine(gitHubArtifactsFolder, "ReleaseNotes.md");