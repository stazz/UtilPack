# NuGetUtils.Tool.Deploy

This project contains code for `nuget-deploy` [.NET Core Global Tool](https://aka.ms/global-tools).
The tool will restore the given NuGet package, and then either copy the assembly and generate required `.deps.json` and `.runtimeconfig.json` files, or copy the assembly and all non-framework dependencies to target folder.

## Command-line documentation

The `nuget-deploy` tool will print the following help:
```
nuget-deploy version 1.0.0.0 (NuGet version 4.8.0.6)
Usage: nuget-deploy nuget-options

Deploy an assembly from NuGet-package, restoring the package if needed, parametrized by command-line parameters.

nuget-options:
* PackageID <packageID>                                      The package ID of the NuGet package containing the entry point assembly.
+ AssemblyPath <path>                                        The path within the resolved library folder of the package, where the assembly resides. Will not be used if there is only one assembly, and is optional if there is assembly with the same name as the package. Otherwise this is required.
  DeploymentKind <GenerateConfigFiles|CopyNonSDKAssemblies>  How the deployment is performed: by generating .deps.json and .runtimeconfig.json files (GenerateConfigFiles), or by copying all non-framework dependency assemblies to target folder (CopyNonSDKAssemblies). The GenerateConfigFiles option is not available for .NET Desktop deployments.
  DisableLockFileCache <true|false>                          Whether to disable using cache directory for restored package information (serialized LockFiles).
  DisableLogging <true|false>                                Whether to disable NuGet logging completely.
  LockFileCacheDirectory <path>                              The path of the directory acting as cache directory for restored package information (serialized LockFiles). By default, the environment variable "NUGET_UTILS_CACHE_DIR" is first checked, and if that is not present, the ".nuget-utils-cache" directory within current home directory is used.
  LogLevel <level>                                           Which log level to use for NuGet logger. By default, this is Information.
  NuGetConfigurationFile <path>                              The path to NuGet configuration file containing the settings to use. The default NuGet setting file behaviour is the default if this is not specified.
  PackageVersion <packageVersion>                            The package version of the NuGet package containing the entry point assembly. The normal NuGet version notation is supported. If this is not specified, then highest floating version is assumed, thus causing queries to remote NuGet servers.
  RestoreFramework <framework>                               The name of the current framework of the process (e.g. ".NETCoreApp,v=2.1"). If automatic detection of the process framework does not work, then use this parameter to override.
  SDKFrameworkPackageID <packageID>                          The package ID of the current framework SDK package. This depends on a auto-detected or explicitly specified restore framework, but is typically "Microsoft.NETCore.App".
  SDKFrameworkPackageVersion <version>                       The package version of the framework SDK package. By default, it is deduced from path of the assembly containing Object type.
  TargetDirectory <path>                                     Where to deploy the assembly and supplemental files. If not specified, a randomly-generated folder will be created within the system temporary directory.


Usage: nuget-deploy configuration-options

Deploy an assembly from NuGet-package, restoring the package if needed, parametrized by configuration file.

configuration-options:
* ConfigurationFileLocation <path>  The path to the JSON file containing the configuration with all the same information as normal command line parameters.
```


# Distribution

Use `dotnet tool install --global NuGetUtils.Tool.Deploy` command to install the binary distribution.