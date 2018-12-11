#!/usr/bin/env dotnet-script
#load "git.csx"
#load "browser.csx"

var repo = Git.GetRepositoryInfo();

Browser.OpenUrl($"https://ci.appveyor.com/project/{repo.Owner}/{repo.ProjectName.Replace(".", "-")}/history");