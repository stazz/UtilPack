# UtilPack.NuGet.MSBuild
This project contains task factory, which makes it easy to execute tasks which are in assemblies of NuGet packages, and potentially are dependant on some other NuGet packages.
The task factory will take care of restoring all dependent packages before running the task, and loading all the assemblies of the dependant NuGet packages and any SDK assemblies as well, on-the-fly.
Thus, using `CopyLocalLockFileAssemblies` property should no longer be necessary.

# Usage

## Usage Within NuGet Package
The task factory should be used in a separate `.targets` file within the package.
The usage itself is as in explicit case.
One should use the [ready-made template](../UtilPack.NuGet.MSBuild.Template) to handle all the mandatory necesseties.
The template also contains documentation on how to use it.

## Usage Explicitly
### Overview
The task factory is used from the project file in the following way:
```xml
  <ItemGroup>
    <!--
    This reference is needed in order to use the task factory.
    The UtilPack.NuGet.MSBuild has no dependencies and no lib folder, so it should be very transparent.
    -->
    <PackageReference Include="UtilPack.NuGet.MSBuild" Version="2.0.0"/>
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
* Inclusion of `UtilPack.NuGet.MSBuild` package via `PackageReference` element. This is required in order to enable MSBuild to use the task factory.
* The `TargetFramework` property is not affected - the `UtilPack.NuGet.MSBuild` package does not have any dependencies nor assemblies in its `lib` path, so it is completely transparent for the build.
* The `UsingTask` element should have its `Condition`, `TaskFactory` and `AssemblyFile` attributes always the same. By default, the only customizable attribute should be `TaskName`, which should be the full type name of the task to execute.
* The `UsingTask` __must__ contain the `Task` element as its child, and `Task` element __must__ contain `NuGetTaskInfo` element as its child. The `NuGetTaskInfo` element __must__ adhere to a certain format, explained in next section.
* After declaring the task, the task may be executed just like any other task declared by any other task factory.

### Task Factory Parameters
The `UtilPack.NuGet.MSBuild` task factory requires some information about the task to execute.
This information is passed as child elements inside the `NuGetTaskInfo` element.

The __exactly one__ of the following two properties is required:
* `PackageID`: This element should contain the ID of the package which contains the task to be executed.
* `PackageIDIsSelf`: This element is interpreted as boolean, case-insensitively, which is by default `false`. If it is `true`, then automatic package ID detection based on the this file directory is attempted. This is useful when the task factory is used within the NuGet package to invoke the task located in the package itself. Setting this to `true` in a file which is not located within NuGet repository package home folder will cause an error. Note that setting this to `true` implicitly sets the `PackageVersion` parameter to `self`.

The following information is optional:
* `PackageVersion`: This element should contain the version string of the package which contains the task to be executed. If not present, the newest version will be used. Should be [version range](https://docs.microsoft.com/en-us/nuget/create-packages/dependency-versions#version-ranges). It may also be `self` (case-insensitively), in which case an automatic package version detection is attempted, in similar manner as with the use of `PackageIDIsSelf` parameter.
* `AssemblyPath`: The assembly path relative to package target framework folder (e.g. `lib/netstandard1.0`) where the assembly containing the task resides. If the NuGet package contains multiple assemblies in a single target framework folder, this element __must__ be present. If the package is multi-targeting, but still only has one assembly in each of its target framework folder, this property is not needed.
* `NuGetConfigurationFile`: The path to NuGet configuration file which will be used when restoring and resolving packages. The [default behaviour](https://docs.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior#config-file-locations-and-uses) will be used if this element is not specified.
* `TaskName`: The full name of the task class. This will override the `TaskName` attribute specified in `UsingTask` element. Useful if you want to use one name in MSBuild project file, but the actual task class has some other name.
* `NuGetFramework`: The string containing framework ID (e.g. `.netcoreapp`) of [NuGetFramework](https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Frameworks/NuGetFramework.cs) class, which should be used as a platform framework when resolving dependencies and assemblies. Useful when the automatic detection of platform framework fails.
* `NuGetFrameworkVersion`: The string containing platform framework version, e.g. `1.1`. Should be parseable by Parse method of [Version](https://docs.microsoft.com/en-us/dotnet/api/system.version) class.
* `NuGetFrameworkPackageID`: This is used __only__ in package-based frameworks, e.g. .NET Core. If automatic detection fails, this element can be used to specify the platform framework package. For .NET Core, it is `Microsoft.NETCore.App`.
* `NuGetFrameworkPackageVersion`: This is used __only__ in package-based frameworks, e.g. .NET Core. If automatic detection fails, this element can be used to specify the version for `NuGetFrameworkPackageID` element.
* `NuGetPlatformRID`: This may be used to override default platform RID used by restorer. The runtime RID is detected automatically, but sometimes that detectio nmay fail. Then this parameter may be used to specify RID manually. The RID is used when detecting which native DLLs of NuGet packages should be considered when loading unmanaged DLL in .NET Core. On .NET Desktop, typically modifying `PATH` environment variable is used to make runtime load unmanaged assemblies. [Official documentation](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) is available.
* `NuGetPlatformRIDCatalogPackageID`: When automatic platform RID detection is used, this parameter may be used to control which package ID is used as package containing `runtime.json` describing runtime RIDs and their dependencies. The default value is `Microsoft.NETCore.Platforms`, as per [Official documentation](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog). Newest version of the package will always be used.
* `OtherLoadersRegistration`: This is used __only__ in .NET Core environment. It controls how assembly load context registers itself to [`Resolving`](https://docs.microsoft.com/en-gb/dotnet/api/system.runtime.loader.assemblyloadcontext.resolving) event of other [`AssemblyLoadContext`s](https://docs.microsoft.com/en-gb/dotnet/api/system.runtime.loader.assemblyloadcontext). It can have the following values:
    * `None`: The `Resolving` event handlers will not be registered at all. This effectively makes [`Type.GetType`](https://docs.microsoft.com/en-gb/dotnet/api/system.type.gettype?view=netcore-2.0#System_Type_GetType_System_String_) fail when trying to resolve type located in one of the loaded NuGet packages by string.
    * `Default`: The `Resolving` event handler is registered to [`AssemblyLoadContext.Default`](https://docs.microsoft.com/en-gb/dotnet/api/system.runtime.loader.assemblyloadcontext.default). This makes [`Type.GetType`](https://docs.microsoft.com/en-gb/dotnet/api/system.type.gettype?view=netcore-2.0#System_Type_GetType_System_String_) work.
    * `Current`: The `Resolving` event handler is registered to the [`AssemblyLoadContext`](https://docs.microsoft.com/en-gb/dotnet/api/system.runtime.loader.assemblyloadcontext) that loaded the task factory.
    * `Default,Current`: This is the __default value__. It behaves like the combination of `Default` and `Current`, but it doesn't register the handler twice to the same event.
* `CopyToFolderBeforeLoad`: This controls the behaviour after assembly has been successfully located but not yet loaded. Manipulating this value controls whether assembly is copied to another directory and loaded from there.
    * `false` case insensitively: The default value, disables copying, and assemblies are loaded directly from within the package repository.
    * `true` case insensitively: enables copying to randomly-name-generated folder in system's temporary folder.
    * any non-empty string: enables copying to folder specified by string.

### Example
There is an example of explicitly using `UtilPack.NuGet.MSBuild` in [here](../UtilPack.NuGet.MSBuild.TestProject).

# Developing MSBuild Tasks Executable By UtilPack.NuGet.MSBuild Task Factory
## Overview
There are just a few constraints when developing the MSBuild task that will be executed by `UtilPack.NuGet.MSBuild` package:
* Minimum MSBuild version supported is `14.3`, and
* It is recommended to use [the template](../UtilPack.NuGet.MSBuild.Template) for tasks which are intented to hook into build process and be automatically run by just adding package reference to the consumer project.

No certain class is required to be extended, and no dependencies (other than to MSBuild assemblies) are required.
If your task uses framework assemblies and other NuGet packages only (the majority of the cases), the `CopyLocalLockFileAssemblies` build property is not required - the task factory will take care of restoring and loading the dependant assemblies from their corresponding NuGet packages.
Developing for .NET 4.5+, .NET Standard 1.3+ and .NET Core 1.0+ is supported - indeed, it is enough to target .NET Standard 1.3, and your task will be executable by both desktop and .NET Core MSBuild.
Referencing third-party NuGet packages is supported, as long as the dependencies are visible in `.nuspec` file.
The assemblies may reside in `lib` or `build` folders of the package, both are supported.

Some caution needs to be excercised when using dynamic loading of assemblies or types, discussed in next section.

## Advanced
Sometimes the task may need to dynamically load a NuGet package assembly, or another assembly by path, or a type based on type string.
Since code related to both usecases is already implemented in `UtilPack.NuGet.MSBuild` task factory, there is no point duplicating it in your task.
Furthermore, assembly loading is radically different .NET Desktop and .NET Core.

This is why the task executed by `UtilPack.NuGet.MSBuild` task factory may declare a constructor which takes one or some or all of the following arguments, in no specific order:
* `Func<String[], String[], String[], Task<Assembly[]>>`: The callback to asynchronously load assemblies from multiple NuGet packages,
* `Func<String, String, String, Task<Assembly>>`: The callback to asynchronously load single assembly from single NuGet package,
* `Func<String, Assembly>`: The callback to load assembly from path,
* `Func<AssemblyName, Assembly>`: The callback to load assembly located in previously restored NuGet packages based on the [`AssemblyName`](https://docs.microsoft.com/dotnet/api/system.reflection.assemblyname) of the assembly, and
* `Func<String, Type>`: The callback to load type from assembly located in previously restored NuGet packages based on assembly-name-qualified type string. This callback is alternative to [`Type.GetType`](https://docs.microsoft.com/en-gb/dotnet/api/system.type.gettype?view=netcore-2.0#System_Type_GetType_System_String_), if for some reason `OtherLoadersRegistration` task factory parameter is set to value which does not register to [`Resolving`](https://docs.microsoft.com/en-gb/dotnet/api/system.runtime.loader.assemblyloadcontext.resolving) event of other [`AssemblyLoadContext`s](https://docs.microsoft.com/en-gb/dotnet/api/system.runtime.loader.assemblyloadcontext).

The NuGet package loader callback has the following parameters, in the following order (the callback to load from multiple NuGet packages has the same parameters, but each type is an array, the meaning still the same):
* `String packageID`: The NuGet package ID, required.
* `String packageVersion`: The NuGet package version, optional (newest will be used if not specified). Should be [version range](https://docs.microsoft.com/en-us/nuget/create-packages/dependency-versions#version-ranges).
* `String assemblyPath`: The path within package home folder where the assembly resides. May be `null` or empty if the package only has one assembly.

The path assembly loader callback has the following parameter:
* `String path`: The path of the assembly which to load, required. The dependencies will not be resolved.

# Under the hood
The `UtilPack.NuGet.MSBuild` task factory is implemented by multi-targeting .NET 4.5 and .NET Core 1.1.
The code mostly common for both frameworks is in [NuGetTaskRunnerFactory.cs](NuGetTaskRunnerFactory.cs) file, and the code radically differing for .NET 4.5 and .NET Core 1.1 is in [NuGetTaskRunnerFactory.NET.cs](NuGetTaskRunnerFactory.NET.cs) and [NuGetTaskRunnerFactory.NETCore.cs](NuGetTaskRunnerFactory.NETCore.cs) files, respectively.
To control the on-demand assembly loading caused by loading and executing the task,
* the Desktop version uses [AppDomains](https://docs.microsoft.com/en-us/dotnet/api/system.appdomain?view=netframework-4.5), and
* the Core version uses [AssemblyLoadContexts](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext?view=netcore-1.1).

The common part of assembly loading logic uses [code generation](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit?view=netframework-4.5) to create task proxy, and the [NuGet Restore command](https://github.com/NuGet/NuGet.Client/tree/dev/src/NuGet.Core/NuGet.Commands/RestoreCommand) to search for the dependencies of the task-to-be-executed package in order to know where to load them.
The Restore command also takes care of downloading any missing packages.

In order to seamlessly integrate into the build, all required assemblies are placed under `build/net45` and `build/netcoreapp1.1` folders, so that the package would not have any dependencies.
Furthermore, the `build` folder contains the `.props` file, which will setup the property `UtilPackNuGetMSBuildAssemblyPath` pointing to correct (Desktop or Core, depending which version of MSBuild is executing the project file) assembly containing the task factory.