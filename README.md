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

Filter packages 

```shell
deps -f Microsoft
```

Update all NuGet packages

```shell
deps update
```

Update packages matching a filter 

```shell
deps update -f Microsoft
```

#### make-global-tool

Turns you favourite script into a dotnet global tool.

```Shell
make-global-tool main.csx -d "Some description" -v 1.0.0
```

We probably don't want to enter the description, tags and so on for every time we create a new version. The easy solution here is to put those parameters in a parameters file.

Let's call it `package.specs`

```shell
-d "Some description"
```

The we can call it like this 

```shell
make-global-tool $(cat package.specs) -v 1.0.1
```

The `make-global-tool` is in fact already available as a global tool on NuGet so you can install it and use it without having `dotnet-script` installed. 

```shell
dotnet tool install dotnet-make-global-tool -g
```



