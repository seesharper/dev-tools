#!/usr/bin/env dotnet-script
#load "build-context.csx"
#load "nuget:Dotnet.Build, 0.3.9"

var rootFolder = FileUtils.GetScriptFolder();
var pathToDepsTests = Path.Combine(rootFolder, "../src/test/deps.tests.csx");

DotNet.Test(pathToDepsTests);

var pathToDepsScript = Path.Combine(rootFolder, "../src/deps.csx");
var pathToMakeGlobalToolScript = Path.Combine(rootFolder, "../src/make-global-tool.csx");

Command.Execute("make-global-tool",$"{pathToDepsScript} {File.ReadAllText("deps.args")} -o {Path.Combine(rootFolder, "Artifacts", "NuGet")}");

Command.Execute("make-global-tool",$"{pathToMakeGlobalToolScript} {File.ReadAllText("make-global-tool.args")} -o {Path.Combine(rootFolder, "Artifacts", "NuGet")}");


