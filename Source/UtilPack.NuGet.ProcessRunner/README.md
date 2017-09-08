# UtilPack.NuGet.ProcessRunner

This project combines the functionalities of [UtilPack.NuGet.Deployment](../UtilPack.NuGet.Deployment) and [UtilPack.ProcessMonitor](../UtilPack.ProcessMonitor) libraries into one executable .NET assembly.
The given NuGet package is restored, if needed, and then started up.
After starting up, the process is monitored in such way that it is possible for the monitored process to signal e.g. graceful shutdown, or graceful restart.

# Command-line parameters
The command-line parameters are the properties of `DeploymentConfiguration` interface in [UtilPack.NuGet.Deployment](../UtilPack.NuGet.Deployment), `MonitoringConfiguration` interface in [UtilPack.ProcessMonitor](../UtilPack.ProcessMonitor), and `ProcessRunnerConfiguration` of this project.
For convenience, all of the supported command-line options are listed below.
The format for parameters is `/<parameter-name>[=value]`, i.e. default syntax that [Microsoft.Extensions.Configuration.CommandLine](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.CommandLine/) supports.
## Deployment-related parameters
* The `ProcessPackageID` is required parameter, it should be the package ID of the NuGet package to be deployed.
* The `ProcessPackageVersion` is optional parameter, it should be the package version of the NuGet package to be deployed. If it is not specified, the newest version of the package will be used.
* The `ProcessFramework` is optional-in-certain-situations parameter, telling the framework name (the folder name under the `lib` directory in the NuGet package) that should be deployed. It is not used for packages targetting only one framework, but it must be specified if package targets multiple frameworks.
* The `ProcessAssemblyPath` is optional-in-certain-situations parameter, telling the assembly path (within the framework folder) of the entrypoint assembly. It is not used for packages having only one assembly, but it must be specified if the framework folder contains multiple assemblies.
* The `ProcessSDKFrameworkPackageID` is optional-in-certain-situations parameter, telling the package ID of the framework main package. For example, for .NET Core, the framework package ID is `Microsoft.NETCore.App`. If not specified, automatic detection will be attempted.
* The `ProcessSDKFrameworkPackageVersion` is optional-in-certain-situations parameter, telling the package version for the `ProcessSDKFrameworkPackageID` parameter.
* The `DeploymentKind` is optional parameter, controlling how dependencies of the NuGet package to be deployed are handled. It has two possible values:
    * `GenerateConfigFiles`: this is default value, and this will cause generation of `.deps.json` and `.runtime.config` files in the target directory, describing the locations of the dependency assemblies. This value is not supported for frameworks that are not package-based, i.e. .NET desktop.
    * `CopyNonSDKAssemblies`: this value will cause all non-SDK assemblies that the package depends on to be copied in target directory. This is default value if the deployment target framework is .NET Desktop.
## Monitoring-related parameters
* The `ToolPath` is optional parameter, and should specify the executable to run instead of the directly running the deployed entrypoint assembly. Even though optional, in 99% of the scenarios especially in .NET Core, this should be a path to a `dotnet` tool.
* The `ProcessArgumentPrefix` is optional parameter, that should contain string that process parameters will be prefixed with. By default, the value of this is `/`.
* The `ShutdownSemaphoreProcessArgument` is optional parameter, that should be the name of the process parameter that accepts shutdown semaphore name. This semaphore will be used to implement graceful process shutdown. By default, this parameter will not be used. Read more about shutdown semaphores in [UtilPack.ProcessMonitor project documentation](../UtilPack.ProcessMonitor).
* The `ShutdownSemaphoreWaitTime` is optional parameter containing the `TimeSpan` for how long to wait for process graceful shutdown. This parameter will only be used if `ShutdownSemaphoreProcessArgument` is specified. The default value is one second.
* The `RestartSemaphoreProcessArgument` is optional parameter, that should be the name of the process parameter that accepts restart semaphore name. This semaphore will be used to implemented graceful process restart. By default, this parameter will not be used. Read more about shutdown semaphores in [UtilPack.ProcessMonitor project documentation](../UtilPack.ProcessMonitor).
* The `RestartWaitTime` is optional parameter containing the `TimeSpan` for how long to wait before restarting the process in the graceful restart situation. This parameter will only be used if `RestartSemaphoreProcessArgument` is specified. The default value is zero seconds, that is, no wait at all.
## Infrastructure-related parameters
* The `NuGetConfigurationFile` is optional parameter specifying where the NuGet configuration file is located. By default, the default machine-specific NuGet configuration file will be used.
* The `PauseBeforeExitIfErrorsSeen` is optional parameter specifying whether for this program to wait for console key press if it sees output in monitored process error stream, or if monitored process returns non-zero.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.NuGet.ProcessRunner) for binary distribution.