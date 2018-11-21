#!/usr/bin/env dotnet-script
#load "nuget:Dotnet.Build, 0.3.9"
using static Command;

// TODO

var test = Command.Execute("deps", "-h");

var result = Command.Capture("make-global-tool",$"../src/make-global-tool.csx{File.ReadAllText("make-global-tool.args")})");

Console.WriteLine("Hello world!");
