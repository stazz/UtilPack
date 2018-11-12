# NuGetUtils.Tool.Exec

This project contains code for `nuget-exec` [.NET Core Global Tool](https://aka.ms/global-tools).
The tool will restore the given NuGet package, and then execute a static method within the assembly of the package, automatically loading dependant assemblies from dependant package as needed.
If the method returns awaitable (`ValueTask`, `ValueTask<T>`, `Task`, or `Task<T>`), this tool will `await` on it.

## Typical usage

The typical usage would be to create an executable of your project by using `<OutputType>Exe</OutputType>` attribute in your `.csproj` file.
The tool will then detect the entrypoint method and pass the additional command line arguments to the main method.
This way your DLL also remains executable by `dotnet`, and there is little difference here than using [nuget-deploy](../NuGetUtils.Tool.Deploy) .NET Core Global Tool and then just running `dotnet`.

## Advanced usage

The more advanced usage relies on passing information to the `nuget-exec` tool about the entrypoint method via C# custom attributes, and having something else than `String[]` as method parameters.
The custom attribute is located in [NuGetUtils.Lib.EntryPoint](../NuGetUtils.Lib.EntryPoint) project, and it may be applied to assembly or method.
The most common usage of this attribute would look something like this:
```cs
[assembly: ConfiguredEntryPoint( typeof( MyClass ), nameof( MyClass.MyMethod ) )]

public class MyClass {
   public static void MyMethod() {
      // your code here...
   }
}
```

For the method parameter types, the following strategy is used:
- `String[]` - will contain the command line arguments passed to `nuget-exec` tool after `--` switch.
- `CancellationToken` - will contain the cancellation token which will get canceled on `Console.CancelKeyPress` event.
- `Func<String, Assembly>` - will contain callback to load assembly by path.
- `Func<AssemblyName, Assembly>` - will contain callback to load previously restored assembly by AssemblyName.
- `Func<String, String, String, CancellationToken, Task<Assembly>>` - will contain callback to restore NuGet package and load assembly, by given package ID, package version (optional), assembly path within target folder (optional), and cancellation token (optional).
- `Func<String[], String[], String[], CancellationToken, Task<Assembly[]>>`- will contain callback to restore NuGet packages and load multiple assemblies, by given package IDs, package versions (optional), assembly paths within target folders (optional), and cancellation token (optional).
- `Func<String, Type>` - will contain callback to load type from previously restored assemblies given the assembly-qualified type name.
- Anything else - the `Microsoft.Extensions.Configuration.Binder` package will be used to create instance of given type from the command line arguments passed to `nuget-exec` tool after `--` switch.

Note that when using `ConfiguredEntryPointAttribute`, the assembly does not need to be compiled as executable with `<OutputType>Exe</OutputType>`.
So to fully tune in with `nuget-exec` tool, one can do the following:
```cs
[assembly: ConfiguredEntryPoint( typeof( MyClass ), nameof( MyClass.MyMethod ) )]

public class MyClass {
   public static void MyMethod(
     CancellationToken cancellationToken,
     MyConfiguration config
   ) {
      // your code here...
   }
}

public class MyConfiguration {
   public String StringParameter { get; set; }
   public Boolean BooleanParameter { get; set; }
}
```

Compile the code as a normal library, push it to NuGet registry, and invoke using e.g. `nuget-exec /PackageID=MyPushedPackageID -- /StringParameter=SomeValue /BooleanParameter=true`.
The parameters for `MyConfiguration` type will be automatically handled by `nuget-exec`, which will also provide cancelable `CancellationToken` for the method.

## Command-line documentation

The `nuget-exec` tool will print the following help:
```
nuget-exec version 1.0.0.0 (NuGet version 4.8.0.6)
Usage: nuget-exec executable-options [-- [executable-arguments] ]

Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by command-line parameters.

executable-options:
* PackageID <packageID>                 The package ID of the NuGet package containing the entry point assembly.
+ AssemblyPath <path>                   The path within the resolved library folder of the package, where the assembly resides. Will not be used if there is only one assembly, and is optional if there is assembly with the same name as the package. Otherwise this is required.
+ EntrypointMethodName <method>         The name of the method which is an entry point method. Is optional if assembly is built as EXE, or if assembly contains ConfiguredEntryPointAttribute attribute. Otherwise it is required.
+ EntrypointTypeName <type>             The full name of the type which contains entry point method. Is optional if assembly is built as EXE, or if assembly contains ConfiguredEntryPointAttribute attribute. Otherwise it is required.
  DisableLockFileCache <true|false>     Whether to disable using cache directory for restored package information (serialized LockFiles).
  DisableLogging <true|false>           Whether to disable NuGet logging completely.
  LockFileCacheDirectory <path>         The path of the directory acting as cache directory for restored package information (serialized LockFiles). By default, the environment variable "NUGET_UTILS_CACHE_DIR" is first checked, and if that is not present, the ".nuget-utils-cache" directory within current home directory is used.
  LogLevel <level>                      Which log level to use for NuGet logger. By default, this is Information.
  NuGetConfigurationFile <path>         The path to NuGet configuration file containing the settings to use. The default NuGet setting file behaviour is the default if this is not specified.
  PackageVersion <packageVersion>       The package version of the NuGet package containing the entry point assembly. The normal NuGet version notation is supported. If this is not specified, then highest floating version is assumed, thus causing queries to remote NuGet servers.
  RestoreFramework <framework>          The name of the current framework of the process (e.g. ".NETCoreApp,v=2.1"). If automatic detection of the process framework does not work, then use this parameter to override.
  RestoreSDKPackage <true|false>        Whether to explicitly restore SDK package. Use only if getting assembly load errors.
  SDKFrameworkPackageID <packageID>     The package ID of the current framework SDK package. This depends on a auto-detected or explicitly specified restore framework, but is typically "Microsoft.NETCore.App".
  SDKFrameworkPackageVersion <version>  The package version of the framework SDK package. By default, it is deduced from path of the assembly containing Object type.

executable-arguments:
  The arguments for the entrypoint within NuGet-packaged assembly.


Usage: nuget-exec configuration-options [additional-executable-arguments]

Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by configuration file.

configuration-options:
* ConfigurationFileLocation <path>  The path to the JSON file containing the configuration with all the same information as normal command line parameters. In addition, the "ProcessArguments" key (with JSON array as value) may be specified for always-present process arguments.

additional-executable-arguments:
  The additional arguments for the entrypoint within NuGet-packaged assembly.
```


# Distribution

Use `dotnet tool install --global NuGetUtils.Tool.Restore` command to install the binary distribution.