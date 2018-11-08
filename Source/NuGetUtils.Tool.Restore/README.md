# NuGetUtils.Tool.Restore

This project contains code for `nuget-restore` [.NET Core Global Tool](https://aka.ms/global-tools).
The tool will restore the given NuGet package, downloading it from remote NuGet source if required.

## Command-line documentation

The `nuget-restore` tool will print the following help:
```
nuget-restore version 1.0.0.0 (NuGet version 4.8.0.6)
Usage: nuget-restore nuget-options

Restore one or multiple NuGet packages, parametrized by command-line parameters.

nuget-options:
+ PackageID <packageID>                  The ID of the single package to be restored. If this property is specified, the options "PackageIDs" and "PackageVersions" *must not* be specified.
+ PackageIDs <packageID list>            The IDs of the multiple packages to be restored. If this property is specified, the options "PackageID" and "PackageVersion" *must not* be specified.
  DisableLockFileCache <true|false>      Whether to disable using cache directory for restored package information (serialized LockFiles).
  DisableLogging <true|false>            Whether to disable NuGet logging completely.
  LockFileCacheDirectory <path>          The path of the directory acting as cache directory for restored package information (serialized LockFiles). By default, the environment variable "NUGET_UTILS_CACHE_DIR" is first checked, and if that is not present, the ".nuget-utils-cache" directory within current home directory is used.
  LogLevel <level>                       Which log level to use for NuGet logger. By default, this is Information.
  NuGetConfigurationFile <path>          The path to NuGet configuration file containing the settings to use. The default NuGet setting file behaviour is the default if this is not specified.
  PackageVersion <packageVersion>        The version of the package to be restored, ID of which was specified using "PackageID" option. The normal NuGet version notation is supported. If this is not specified, then highest floating version is assumed, thus causing queries to remote NuGet servers.
  PackageVersions <packageVersion list>  The versions of the packages to be restored, IDs of which were specified using "PackageIDs" option. For each version, the normal NuGet version notation is supported. If the version is not specified, then highest floating version is assumed, thus causing queries to remote NuGet servers.
  RestoreFramework <framework>           The name of the current framework of the process (e.g. ".NETCoreApp,v=2.1"). If automatic detection of the process framework does not work, then use this parameter to override.
  SDKFrameworkPackageID <packageID>      The package ID of the current framework SDK package. This depends on a auto-detected or explicitly specified restore framework, but is typically "Microsoft.NETCore.App".
  SDKFrameworkPackageVersion <version>   The package version of the framework SDK package. By default, it is deduced from path of the assembly containing Object type.
  SkipRestoringSDKPackage <true|false>   Whether to restored the SDK package (typically "Microsoft.NETCore.App") as well. This is useful in conjunction with nuget-exec tool. The default value is true.


Usage: nuget-restore configuration-options

Restore one or multiple NuGet packages, parametrized by configuration file.

configuration-options:
* ConfigurationFileLocation <path>  The path to the JSON file containing the configuration with all the same information as normal command line parameters.
```


# Distribution

Use `dotnet tool install --global NuGetUtils.Tool.Restore` command to install the binary distribution.