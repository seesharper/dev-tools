#!/usr/bin/env dotnet-script
#load "git.csx"
#load "browser.csx"

var repo = Git.GetRepositoryInfo();

Browser.OpenUrl($"https://travis-ci.org/{repo.Owner}/{repo.ProjectName}/builds");