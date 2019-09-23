#!/usr/bin/env dotnet-script
#load "git.csx"
#load "browser.csx"
#load "console.csx"

var localFolder = Git.GetLocalFolder();
var repo = Git.GetRepositoryInfo();


if (UsesAppVeyor(localFolder))
{
    Browser.OpenUrl($"https://ci.appveyor.com/project/{repo.Owner}/{repo.ProjectName.Replace(".", "-")}/history");
}
else
if (UsesTravis(localFolder))
{
    Browser.OpenUrl($"https://travis-ci.org/{repo.Owner}/{repo.ProjectName}/builds");
}
else
if (UsesAzurePipelines(localFolder))
{
    var devopsUsername = Environment.GetEnvironmentVariable("AZURE_DEVOPS_USERNAME");
    if (string.IsNullOrWhiteSpace(devopsUsername))
    {
        WriteError("Azure Devops user name need to be defined as an environment variable. export AZURE_DEVOPS_USERNAME=your-user-name");
    }
    else
    {
        Browser.OpenUrl($"https://{devopsUsername}.visualstudio.com/{repo.ProjectName}/_build");
    }
}
else
{
    WriteHighlighted("No CI configuration found. Supported CI systems are Travis, AppVeyor and Azure Pipelines");
}


private static bool UsesTravis(string localFolder)
{
    return File.Exists(Path.Combine(localFolder, ".travis.yml"));
}

private static bool UsesAppVeyor(string localFolder)
{
    return File.Exists(Path.Combine(localFolder, "appveyor.yml"));
}

private static bool UsesAzurePipelines(string localFolder)
{
    return File.Exists(Path.Combine(localFolder, "azure-pipelines.yml"));
}