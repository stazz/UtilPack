# UtilPack.NuGet.MSBuild
This project contains task factory, which makes it easy to execute tasks which are in assemblies of NuGet packages, and potentially are dependant on some other NuGet packages.

# Usage
## Introduction
The ```UtilPack.NuGet.MSBuild``` NuGet package provides a MSBuild task factory, which will execute the given MSBuild task located in some NuGet package.
This NuGet package is searched by specifying mandatory package ID and optional package version in the task factory parameters inside ```<Task>``` element of the ```UsingTask``` declaration.
The task factory will then take care of loading the assemblies of given NuGet package, instantiating a task, and load the dependent packages and assemblies on-the-fly.
The task factory will also restore any missing packages from local repository.

N.B.! If you are running MSBuild on .NET Core, please use version 15.3(-Preview).
Task factories are not supported in MSBuild 15.1 for .NET Core.

## Usage From Project File
### Overview
The task factory is used from the project file in the following way:
```xml
  <ItemGroup>
    <!--
    This reference is needed in order to use the task factory.
    The UtilPack.NuGet.MSBuild has no dependencies and no lib folder, so it should be very transparent.
    -->
    <PackageReference Include="UtilPack.NuGet.MSBuild" Version="1.0.0"/>
  </ItemGroup>

  <!-- 
    Declare the task.
    Condition, TaskFactory and AssemblyFile attributes should always be the same.
    TaskName attribute should be the full type name of the task class, however it can be overridden inside <Task> element.
    The UtilPackNuGetMSBuildAssemblyPath property is provided by the UtilPack.NuGet.MSBuild package.
    -->
  <UsingTask
    Condition=" '$(UtilPackNuGetMSBuildAssemblyPath)' != '' "
    TaskFactory="UtilPack.NuGet.MSBuild.NuGetTaskRunnerFactory"
    AssemblyFile="$(UtilPackNuGetMSBuildAssemblyPath)"
    TaskName="YourTaskNameHere"
  >
    <Task>
      <NuGetTaskInfo>
        Task factory parameters...
      </NuGetTaskInfo>
    </Task>
  </UsingTask>
  
  <Target Name="MyTarget">
    <YourTaskNameHere
      TaskParameter1="ValueForFirstTaskParameter"
      TaskParameter2="ValueForSecondTaskParameter"
      ...
    />
  </Target>
```

The following are key aspects when using the task factory from project file:
* Inclusion of ```UtilPack.NuGet.MSBuild``` package via ```PackageReference``` element. This is required in order to enable MSBuild to use the task factory.
* The ```TargetFramework``` property is not affected - the ```UtilPack.NuGet.MSBuild``` package does not have any dependencies nor assemblies in its ```lib``` path, so it is completely transparent for the build.
* The ```UsingTask``` element should have its ```Condition```, ```TaskFactory``` and ```AssemblyFile``` attributes always the same. By default, the only customizable attribute should be ```TaskName```, which should be the full type name of the task to execute.
* The ```UsingTask``` __must__ contain the ```Task``` element as its child, and ```Task``` element __must__ contain ```NuGetTaskInfo``` element as its child. The ```NuGetTaskInfo``` element __must__ adhere to a certain format, explained in next section.
* After declaring the task, the task may be executed just like any other task declared by any other task factory.

### Task Factory Parameters
The ```UtilPack.NuGet.MSBuild``` task factory requires some information about the task to execute.
This information is passed as child elements inside the ```NuGetTaskInfo``` element.

The following information is required:
* ```PackageID```: This element should contain the ID of the package which contains the task to be executed.

The following information is optional:
* ```PackageVersion```: This element should contain the version string of the package which contains the task to be executed. If not present, the newest version will be used. Should be [version range](https://docs.microsoft.com/en-us/nuget/create-packages/dependency-versions#version-ranges).
* ```AssemblyPath```: The assembly path relative to package home folder where the assembly containing the task resides. If the NuGet package contains multiple assemblies, this element __must__ be present.
* ```NuGetConfigurationFile```: The path to NuGet configuration file which will be used when restoring and resolving packages. The [default behaviour](https://docs.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior#config-file-locations-and-uses) will be used if this element is not specified.
* ```TaskName```: The full name of the task class. This will override the ```TaskName``` attribute specified in ```UsingTask``` element. Useful if you want to use one name in MSBuild project file, but the actual task class has some other name.
* ```NuGetFramework```: The string containing framework ID (e.g. ```.netcoreapp```) of [NuGetFramework](https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Frameworks/NuGetFramework.cs) class, which should be used as a platform framework when resolving dependencies and assemblies. Useful when the automatic detection of platform framework fails.
* ```NuGetFrameworkVersion```: The string containing platform framework version, e.g. ```1.1```. Should be parseable by Parse method of [Version](https://docs.microsoft.com/en-us/dotnet/api/system.version) class.
* ```NuGetFrameworkPackageID```: This is used __only__ in package-based frameworks, e.g. .NET Core. If automatic detection fails, this element can be used to specify the platform framework package. For .NET Core, it is ```Microsoft.NETCore.App```.
* ```NuGetFrameworkPackageVersion```: This is used __only__ in package-based frameworks, e.g. .NET Core. If automatic detection fails, this element can be used to specify the version for ```NuGetFrameworkPackageID``` element.

### Example
There is an example of using ```UtilPack.NuGet.MSBuild``` in [here](../UtilPack.NuGet.MSBuild.TestProject).

# Developing MSBuild Tasks Executable By UtilPack.NuGet.MSBuild Task Factory
## Overview
There are no special constraints when developing the MSBuild task that will be executed by ```UtilPack.NuGet.MSBuild``` package.
No certain class is required to be extended, and no dependencies (other than to MSBuild assemblies) are required.
Developing for .NET 4.5+, .NET Standard 1.3+ and .NET Core 1.0+ is supported - indeed, it is enough to target .NET Standard 1.3, and your task will be executable by both desktop and .NET Core MSBuild.
Referencing third-party NuGet packages is supported, as long as the dependencies are visible in ```.nuspec``` file.
The assemblies may reside in ```lib``` or ```build``` folders of the package, both are supported.

## Advanced
Sometimes the task may need to dynamically load a NuGet package assembly, or another assembly by path.
Since code related to both usecases is already implemented in ```UtilPack.NuGet.MSBuild``` task factory, there is no point duplicating it in your task.
Furthermore, assembly loading is radically different .NET Desktop and .NET Core.

This is why the task executed by ```UtilPack.NuGet.MSBuild``` task factory may declare a constructor which takes one or some or all of the following arguments, in no specific order:
* ```Func<String[], String[], String[], Task<Assembly[]>>```: The callback to asynchronously load assemblies from multiple NuGet packages,
* ```Func<String, String, String, Task<Assembly>>```: The callback to asynchronously load single assembly from single NuGet package, and
* ```Func<String, Assembly>```: The callback to load assembly from path.

The NuGet package loader callback has the following parameters, in the following order (the callback to load from multiple NuGet packages has the same parameters, but each type is an array, the meaning still the same):
* ```String packageID```: The NuGet package ID, required.
* ```String packageVersion```: The NuGet package version, optional (newest will be used if not specified). Should be [version range](https://docs.microsoft.com/en-us/nuget/create-packages/dependency-versions#version-ranges).
* ```String assemblyPath```: The path within package home folder where the assembly resides. May be ```null``` or empty if the package only has one assembly.

The path assembly loader callback has the following parameter:
* ```String path```: The path of the assembly which to load, required. The dependencies will not be resolved.

# Under the hood
The ```UtilPack.NuGet.MSBuild``` task factory is implemented by multi-targeting .NET 4.5 and .NET Core 1.1.
The code mostly common for both frameworks is in [NuGetTaskRunnerFactory.cs](NuGetTaskRunnerFactory.cs) file, and the code radically differing for .NET 4.5 and .NET Core 1.1 is in [NuGetTaskRunnerFactory.NET.cs](NuGetTaskRunnerFactory.NET.cs) and [NuGetTaskRunnerFactory.NETCore.cs](NuGetTaskRunnerFactory.NETCore.cs) files, respectively.
To control the on-demand assembly loading caused by loading and executing the task,
* the Desktop version uses [AppDomains](https://docs.microsoft.com/en-us/dotnet/api/system.appdomain?view=netframework-4.5), and
* the Core version uses [AssemblyLoadContexts](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext?view=netcore-1.1).

The common part of assembly loading logic uses [code generation](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit?view=netframework-4.5) to create task proxy, and the [NuGet Restore command](https://github.com/NuGet/NuGet.Client/tree/dev/src/NuGet.Core/NuGet.Commands/RestoreCommand) to search for the dependencies of the task-to-be-executed package in order to know where to load them.
The Restore command also takes care of downloading any missing packages.

In order to seamlessly integrate into the build, all required assemblies are placed under ```build/net45``` and ```build/netcoreapp1.1``` folders, so that the package would not have any dependencies.
Furthermore, the ```build``` folder contains the ```.props``` file, which will setup the property ```UtilPackNuGetMSBuildAssemblyPath``` pointing to correct (Desktop or Core, depending which version of MSBuild is executing the project file) assembly containing the task factory.