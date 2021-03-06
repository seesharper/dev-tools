#!/usr/bin/env dotnet-script
#load "utils.csx"
#load "command.csx"
#load "console.csx"
#r "nuget:McMaster.Extensions.CommandLineUtils, 2.2.5"
using System.Text.RegularExpressions;
using System.Xml.Linq;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;

private const string ProjectTemplate = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.1</TargetFramework>
    <IsPackable>true</IsPackable>
    <PackAsTool>true</PackAsTool>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Dotnet.Script.Core"" Version=""0.28.0""/>
  </ItemGroup>
</Project>";

private const string ProgramTemplate = @"
using System;
using System.IO;
using System.Threading.Tasks;
using Dotnet.Script.Core;
using Dotnet.Script.Core.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace ScriptWrapper
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var pathToScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ""$SCRIPTFILE"");
            ExecuteScriptCommandOptions options = new ExecuteScriptCommandOptions(new ScriptFile(pathToScript) , args, OptimizationLevel.Release, Array.Empty<string>(), false, false);
            var executeScriptCommand = new ExecuteScriptCommand(ScriptConsole.Default, (type) => (m,l,e) => {});
            return await executeScriptCommand.Run<int, CommandLineScriptGlobals>(options);
        }
    }
}
";
public static int Execute(params string[] args)
{
    var app = new CommandLineApplication(throwOnUnexpectedArg:false);

    var helpOption = app.HelpOption("-h | --help");

    var fileArgument = app.Argument("file", "The script file for which to create a global tool",c=> {
        c.IsRequired();
    });

    var descriptionOption = app.Option("-d | --description", "The package description", CommandOptionType.SingleValue);
    descriptionOption.IsRequired(allowEmptyStrings:false, "The package needs a description");

    var packageIdOption = app.Option("-i | --id", "The id/name of the package to create (default value is 'dotnet-[script-name]')", CommandOptionType.SingleValue);

    var toolNameOption = app.Option("-n | ---name", "The name of the command (default value is script file name)",CommandOptionType.SingleValue);

    var versionOption  = app.Option("-v | --version", "The version to be used when creating the package", CommandOptionType.SingleValue);

    var outputFolderOption = app.Option("-o | --output", "The output folder for the NuGet package", CommandOptionType.SingleValue);

    app.OnExecute(() =>
    {
        if (string.IsNullOrEmpty(fileArgument.Value))
        {
            app.ShowHelp();
        }

        var scriptFile = Path.GetFullPath(fileArgument.Value);
        var toolCommandName = toolNameOption.HasValue() ? toolNameOption.Value() : Path.GetFileNameWithoutExtension(scriptFile);
        var packageId = packageIdOption.HasValue() ? packageIdOption.Value() : $"dotnet-{toolCommandName}";
        var packageVersion = versionOption.HasValue() ? versionOption.Value() : "1.0.0";
        var outputFolder = outputFolderOption.HasValue() ? outputFolderOption.Value() :  Path.GetDirectoryName(scriptFile);
        var description = descriptionOption.Value();
        CreateToolPackage(scriptFile, packageId, toolCommandName, packageVersion, description, outputFolder);
        return 0;
    });


    return app.Execute(args.ToArray());
}

public static void CreateToolPackage(string pathToScriptFile, string packageId, string toolCommandName, string packageVersion, string description, string outputFolder)
{
    var pathToScriptFolder = Path.GetDirectoryName(pathToScriptFile);

    var fileName = Path.GetFileName(pathToScriptFile);

    using (var buildFolder = new DisposableFolder())
    {
        var projectFile = XDocument.Parse(ProjectTemplate);
        var itemGroupElement = projectFile.Descendants("ItemGroup").Single();

        var scriptFiles = new ScriptFilesResolver().GetScriptFiles(pathToScriptFile);
        foreach (var scriptFile in scriptFiles)
        {
            var relativePathToScriptFile = Path.GetRelativePath(pathToScriptFolder, scriptFile);
            var relativePathToScriptFolder = Path.GetDirectoryName(relativePathToScriptFile);
            if (string.IsNullOrWhiteSpace(relativePathToScriptFolder))
            {
                relativePathToScriptFolder = ".";
            }
            var fullPathToDestination = Path.Combine(buildFolder.Path, relativePathToScriptFile);
            File.Copy(scriptFile, fullPathToDestination);
            var includeElement = new XElement("None", new XAttribute("Include", relativePathToScriptFile), new XAttribute("Pack", true),  new XAttribute("PackagePath", relativePathToScriptFolder), new XAttribute("CopyToOutputDirectory", "Always"));
            itemGroupElement.Add(includeElement);
        }

        var propertyGroupElement = projectFile.Descendants("PropertyGroup").Single();
        propertyGroupElement.Add(new XElement("ToolCommandName", toolCommandName));
        propertyGroupElement.Add(new XElement("Description", description));
        propertyGroupElement.Add(new XElement("AssemblyName", packageId));
        propertyGroupElement.Add(new XElement("Version", packageVersion));


        var pathToProjectFile = Path.Combine(buildFolder.Path, "project.csproj");
        projectFile.Save(pathToProjectFile);

        var pathToProgramFile = Path.Combine(buildFolder.Path, "program.cs");
        File.WriteAllText(pathToProgramFile, ProgramTemplate.Replace("$SCRIPTFILE", fileName));

        Command.Capture("dotnet", $"pack -c release -o {outputFolder}", buildFolder.Path).EnsureSuccessfulExitCode();
        WriteSuccess($"Successfully created global tool package {packageId}.{packageVersion}.nupkg");
        WriteLine();
        WriteHighlighted("Try it out locally by executing the following command");
        WriteLine($"dotnet tool install -g {packageId} --add-source .");
    }
}

public class ScriptFilesResolver
    {
        public HashSet<string> GetScriptFiles(string csxFile)
        {
            HashSet<string> result = new HashSet<string>();
            Process(csxFile, result);
            return result;
        }

        private void Process(string csxFile, HashSet<string> result)
        {
            if (result.Add(csxFile))
            {
                var loadDirectives = GetLoadDirectives(File.ReadAllText(csxFile));
                foreach (var loadDirective in loadDirectives)
                {
                    string referencedScript;
                    if (!Path.IsPathRooted(loadDirective))
                    {
                        referencedScript = Path.GetFullPath((new Uri(Path.Combine(Path.GetDirectoryName(csxFile), loadDirective))).LocalPath);
                    }
                    else
                    {
                        referencedScript = loadDirective;
                    }

                    Process(referencedScript, result);
                }
            }
        }

        private static string[] GetLoadDirectives(string content)
        {
            var matches = Regex.Matches(content, @"^\s*#load\s*""\s*(.+)\s*""", RegexOptions.Multiline);
            List<string> result = new List<string>();
            foreach (var match in matches.Cast<Match>())
            {
                var value = match.Groups[1].Value;
                if (value.StartsWith("nuget", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                result.Add(value);
            }

            return result.ToArray();
        }
    }
