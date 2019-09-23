#load "nuget: ScriptUnit, 0.1.4"
#load "../deps.app.csx"
#r "nuget: FluentAssertions, 5.5.3"

using static ScriptUnit;
using FluentAssertions;
using System.Xml.Linq;

await new TestRunner().AddTestsFrom<DependencyTests>().Execute();

public class DependencyTests
{
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
  </ItemGroup>
</Project>";



    public void ShouldListOutdatedDependency()
    {
        using (var projectFolder = new DisposableFolder())
        {
            var projectFileContent = CreateProjectFile("LightInject", "5.1.0");
            var pathToProjectFile = Path.Combine(projectFolder.Path, "project.csproj");
            File.WriteAllText(pathToProjectFile, projectFileContent);
            Execute("-cwd", projectFolder.Path);
            TestContext.StandardOut.Should().Contain("LightInject 5.1.0 =>");
        }
    }

    


    public void ShouldOutputWarningWhenPackageIsNotFound()
    {
        using (var projectFolder = new DisposableFolder())
        {
            var projectFileContent = CreateProjectFile("UnknownPackage", "5.1.0");
            var pathToProjectFile = Path.Combine(projectFolder.Path, "project.csproj");
            File.WriteAllText(pathToProjectFile, projectFileContent);
            Execute("-cwd", projectFolder.Path);
            TestContext.StandardOut.Should().Contain("Unable to find package UnknownPackage");
        }
    }

    private string CreateProjectFile(string packageName, string version)
    {
        XDocument projectFile = XDocument.Parse(ProjectTemplate);
        var itemGroupElement = projectFile.Descendants("ItemGroup").Single();
        var packageElement = new XElement("PackageReference", new XAttribute("Include",packageName), new XAttribute("Version", version));
        itemGroupElement.Add(packageElement);
        return projectFile.ToString();
    }
}