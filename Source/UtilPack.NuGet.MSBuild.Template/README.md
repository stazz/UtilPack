# Template for tasks utilizing UtilPack.NuGet.MSBuild

This folder contains template file for MSBuild projects which contain a custom MSBuild task.
Typically, this task may have some third party NuGet package dependencies, and the [UtilPack.NuGet.MSBuild](../UtilPack.NuGet.MSBuild) task factory can be used to handle all the assembly loading hassle when the MSBuild process executes the custom MSBuild task.
To gain the benefit of freely referencing any NuGet packages and have them restored and loaded during runtime without the need to bloat your package via `CopyLocalLockFileAssemblies` property, there are some necessary things to do in your NuGet package.
This template project provides an easy way to generate and handle those necessary things, while allowing to concentrate purely on the code of your custom MSBuild task.

# Instructions

1. Copy the `YourPackage.csproj.template` file in this directory to the desired directory where you wish your project to reside, and rename it to meaningful name, typically `MyCustomTask.csproj`.
2. Run the build for the new `.csproj` file once, either via `dotnet msbuild /t:Restore;Build MyCustomTask.csproj` command, or via Visual Studio IDE. Doing so successfully should generate four files:
    - `BuildHook.props` - the `.props` file containing skeleton implementation for the hook into the build process of the consumer project. Feel free to modify this file for your needs.
    - `Functionality.targets` - the `.targets` file containing the call to your custom MSBuild task, written in C#/VB.NET/F#. Typically you'll need only to modify the `TaskName` property of `UsingTask` directive, and possibly the contents (but not the `Name` attribute) of the `Target` element.
    - `Infrastructure.targets` - the `.targets` file containing the necessary things mentioned above. In most cases, you won't need to modify anything in this file.
    - `Task.cs` - the C# file containing skeleton for your custom MSBuild task. Feel free to modify the contents and the file name as needed.   

    Only `Task.cs` file is freely renameable without any extra work. If you wish to rename the `BuildHook.props`, `Functionality.targets` or `Infrastructure.targets` file, you should also edit the `.csproj` file and change the properties containing those filenames.

# Usage
After initial build, you can freely pack and distribute your package.
The `.csproj` file contains all the necessary instructions to properly name the `.targets` and `.props` file and put them in such folder that consumer project will pick up the build hooks.