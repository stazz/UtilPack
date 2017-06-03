# Running Custom MSBuild Tasks, Which Are NuGet Packages
## Introduction
The ```UtilPack.NuGet.MSBuild``` NuGet package provides a MSBuild task factory, which will execute the given MSBuild task located in some NuGet package.
This NuGet package is searched by specifying mandatory package ID and optional package version in the task factory parameters inside ```<Task>``` element of the ```UsingTask``` declaration.
The task factory will then take care of loading the assemblies of given NuGet package, instantiating a task, and load the dependent packages and assemblies on-the-fly.

N.B.! Currently, the ```UtilPack.NuGet.MSBuild``` task factory does not restore any packages - both task package and all of its dependencies must be present in the local NuGet repository/repositories!
Hopefully soon the task factory will also take care of restoring the packages.

N.B.2.! If you are running MSBuild on .NET Core, please use version 15.3(-Preview).
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
    <PackageReference Include="UtilPack.NuGet.MSBuild" Version="1.0.0-RC1"/>
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
    </Task
  </UsingTask>
```

The following are key aspects when using the task factory from project file:
* Inclusion of ```UtilPack.NuGet.MSBuild``` package via ```PackageReference``` element. This is required in order to enable MSBuild to use the task factory.
* The ```TargetFramework``` property is not affected - the ```UtilPack.NuGet.MSBuild``` package does not have any dependencies nor assemblies in its ```lib``` path, so it is completely transparent for the build.
* The ```UsingTask``` element should have its ```Condition```, ```TaskFactory``` and ```AssemblyFile``` attributes always the same. By default, the only customizable attribute should be ```TaskName```, which should be the full type name of the task to execute.
* The ```UsingTask``` __must__ contain the ```Task``` element as its child, and ```Task``` element __must__ contain ```NuGetTaskInfo``` element as its child. The ```NuGetTaskInfo``` element __must__ adhere to a certain format, explained in next section.

### Task Factory Parameters
The ```UtilPack.NuGet.MSBuild``` task factory requires some information about the task to execute.
This information is passed as child elements inside the ```NuGetTaskInfo``` element.

The following information is required:
* ```PackageID```: This element should contain the ID of the package which contains the task to be executed.

The following information is optional:
* ```PackageVersion```: This element should contain the version string of the package which contains the task to be executed. If not present, the newest version will be used.
* ```AssemblyPath```: The assembly path relative to package home folder where the assembly containing the task resides. If the NuGet package contains multiple assemblies, this element __must__ be present.
* ```RepositoryPath```: The path to repository to use when discovering task and its dependencies. By default, the [default path](http://lastexitcode.com/projects/NuGet/FileLocations/) will be used. Specify multiple ```RepositoryPath``` elements to make the task factory search multiple local repositories.
* ```TaskName```: The full name of the task class. This will override the ```TaskName``` attribute specified in ```UsingTask``` element. Useful if you want to use one name in MSBuild project file, but the actual task class has some other name.
* ```NuGetFramework```: The string parseable by ```Parse``` method in [NuGetFramework](https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Frameworks/NuGetFramework.cs) class, which should be used as a platform framework when resolving dependencies and assemblies. Useful when the automatic detection of platform framework fails.
* ```NuGetFrameworkPackageID```: This is used __only__ in package-based frameworks, e.g. .NET Core. If automatic detection fails, this element can be used to specify the platform framework package. For .NET Core, it is ```Microsoft.NETCore.App```.
* ```NuGetFrameworkPackageVersion```: This is used __only__ in package-based frameworks, e.g. .NET Core. If automatic detection fails, this element can be used to specify the version for ```NuGetFrameworkPackageID``` element.

# Developing MSBuild Tasks Executable By UtilPack.NuGet.MSBuild Task Factory
## Overview
There are no special constraints when developing the MSBuild task that will be executed by ```UtilPack.NuGet.MSBuild``` package.
No certain class is required to be extended, and no dependencies (other than to MSBuild assemblies) are required.
Developing for .NET 4.5+, .NET Standard 1.3+ and .NET Core 1.0+ is supported - indeed, it is enough to target .NET Standard 1.3, and your task will be executable by both desktop and .NET Core MSBuild.
Referencing third-party NuGet packages is supported, as long as they are visible in ```.nuspec``` file.
The assemblies may reside in ```lib``` or ```build``` folders of the package, both are supported.

## Advanced
Sometimes the task may need to dynamically load a NuGet package assembly, or another assembly by path.
Since code related to both usecases is already implemented in ```UtilPack.NuGet.MSBuild``` task factory, there is no point duplicating it in your task.
Furthermore, assembly loading is radically different .NET Desktop and .NET Core.

This is why the task executed by ```UtilPack.NuGet.MSBuild``` task factory may declare a constructor which takes one or both of the following arguments, in no specific order:
* ```Func<String, String, String[], Boolean, String, Assembly>```: The callback to load assembly from NuGet package, and
* ```Func<String, Assembly>```: The callback to load assembly from path.

The NuGet package loader callback has the following parameters:
* ```String packageID```: The NuGet package ID, required.
* ```String packageVersion```: The NuGet package version, optional (newest will be used if not specified).
* ```String[] repositoryPaths```: The paths to NuGet local repositories where to search package and its dependencies. May be ```null``` or empty, in such case, the default repository path will be used.
* ```Boolean checkDependencies```: Whether to scan and learn about all the dependencies. In most cases (especially if the types of returned assembly will be used to create objects etc) this should be ```true```.
* ```String assemblyPath```: The path within package home folder where the assembly resides. May be ```null``` or empty if the package only has one assembly.

The path assembly loader callback has the following parameters:
* ```String path```: The path of the assembly which to load, required. The dependencies will not be resolved.
