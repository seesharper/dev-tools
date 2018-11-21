#!/usr/bin/env dotnet-script
#load "git.csx"
#load "browser.csx"

Browser.OpenUrl(Git.GetRemoteUrl());



