# UtilPack.NuGet.Deployment

This project contains a class, aptly named `NuGetDeployment`, which will help in situations when it is needed to deploy a binary distribution of NuGet package, so that the assembly of the NuGet package could be executed by `dotnet` .NET Core runner.
The `NuGetDeployment` accepts configuration containing various parameters as its single argument to constructor, and exposes single method, `DeployAsync`, which will perform the deploying.
The first step of deploying of NuGet package is to restore the target package if it is missing from current machine, and the assemblies in NuGet package are copied to target folder specified in configuration passed to the constructor of `NuGetDeployment`.
Then, depending on the values in the configuration, either `.deps.json` file is generated, or all binary dependencies (assemblies) are copied to target folder.

# Configuration
The `DeploymentConfiguration` interface (implemented by `DefaultDeploymentConfiguration` class) has static configuration for the `NuGetDeployment` class.
The properties are explained below.
* The `ProcessPackageID` is required parameter, it should be the package ID of the NuGet package to be deployed.
* The `ProcessPackageVersion` is optional parameter, it should be the package version of the NuGet package to be deployed. If it is not specified, the newest version of the package will be used.
* The `ProcessFramework` is optional-in-certain-situations parameter, telling the framework name (the folder name under the `lib` directory in the NuGet package) that should be deployed. It is not used for packages targetting only one framework, but it must be specified if package targets multiple frameworks.
* The `ProcessAssemblyPath` is optional-in-certain-situations parameter, telling the assembly path (within the framework folder) of the entrypoint assembly. It is not used for packages having only one assembly, but it must be specified if the framework folder contains multiple assemblies.
* The `ProcessSDKFrameworkPackageID` is optional-in-certain-situations parameter, telling the package ID of the framework main package. For example, for .NET Core, the framework package ID is `Microsoft.NETCore.App`. If not specified, automatic detection will be attempted.
* The `ProcessSDKFrameworkPackageVersion` is optional-in-certain-situations parameter, telling the package version for the `ProcessSDKFrameworkPackageID` parameter.
* The `DeploymentKind` is optional parameter, controlling how dependencies of the NuGet package to be deployed are handled. It has two possible values:
    * `GenerateConfigFiles`: this is default value, and this will cause generation of `.deps.json` and `.runtime.config` files in the target directory, describing the locations of the dependency assemblies. This value is not supported for frameworks that are not package-based, i.e. .NET desktop.
    * `CopyNonSDKAssemblies`: this value will cause all non-SDK assemblies that the package depends on to be copied in target directory. This is default value if the deployment target framework is .NET Desktop.
* The `RestoreFramework` is optional parameter, controlling which NuGet framework is used when performing the restore of the target package. Both long and folder names are supported.
* The `RuntimeIdentifier` is optional parameter, controlling which runtime identifier the restore of the target package is performed against.

The `DeployAsync` method of the `NuGetDeployment` class has the following parameters.
* `ISettings nugetSettings`: the `ISettings` object to be used when loading and potentially restoring the NuGet package.
* `String targetDirectory`: the directory where to deploy the assemblies. If it does not exist, it will be created.
* `ILogger logger = null`: the optional logger to use when performing NuGet operations.
* `CancellationToken token = default( CancellationToken )`: the optional `CancellationToken` to use for async NuGet operations.

# Distribution

See [NuGet package](http://www.nuget.org/packages/UtilPack.NuGet.Deployment) for binary distribution.