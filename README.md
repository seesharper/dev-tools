## dev-scripts

Just a collection of C# scripts that makes my day. 

### Prerequisites 

* .Net Core 
* dotnet-script
* You are on *nix (because I don't use Windows 😎)

### Bootstrapping 

Start by cloning this repo

```shell
git clone https://github.com/seesharper/dev-scripts.git
```

In the repo folder

```shell
dotnet script install-script.csx install-script.csx
```

> Yup, look a bit funky, but that's bootstrapping for ya

Look through the following list of commands and if you like, you can "install" it like this.

```shell
install-script repo.csx
```

> Makes a the script `repo.csx` globally available as `repo`

### Commands

#### repo

Opens the current git repository in the default web browser.

#### appveyor

Opens the AppVeyor build history based on the current git repository.

#### travis

Opens the Travis build history based on the current git repository.

#### deps

Compares the NuGet package references found in `csproj` files with the latest versions found in the available NuGet feeds. 

To get a an overview packages that can be updated, simply execute the following command in your repo/project folder.

```shell
deps
```

Example output

```shell
Analyzing /Users/someUser/somerepo/project_a.csproj
Analyzing /Users/someUser/somerepo/project_a.csproj

Getting the latest package versions. Hang on.....
Feeds

 * nuget.org [https://api.nuget.org/v3/index.json]

/Users/someUser/somerepo/project_a.csproj

xunit 2.4.0 => 2.4.1 (nuget.org)
xunit.runner.visualstudio 2.4.0 => 2.4.1 (nuget.org)

/Users/someUser/somerepo/project_b.csproj

System.Configuration.ConfigurationManager 4.4.1 => 4.5.0 (nuget.org)
```

Update all NuGet packages

```shell
deps update
```

Update packages matching simple filter (contains)



