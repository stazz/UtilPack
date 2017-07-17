# UtilPack.NuGet.Deployment.MSBuild

This project contains MSBuild task, which exposes the functionality of the `NuGetDeployment` class located in [UtilPack.NuGet.Deployment](../UtilPack.NuGet.Deployment) project.

# Input parameters
The input parameters of the `UtilPack.NuGet.Deployment.MSBuild.DeployNuGetPackageTask` are listed below.
* The `ProcessPackageID` is required parameter, it should be the package ID of the NuGet package to be deployed.
* The `ProcessPackageVersion` is optional parameter, it should be the package version of the NuGet package to be deployed. If it is not specified, the newest version of the package will be used.
* The `ProcessFramework` is optional-in-certain-situations parameter, telling the framework name (the folder name under the `lib` directory in the NuGet package) that should be deployed. It is not used for packages targetting only one framework, but it must be specified if package targets multiple frameworks.
* The `ProcessAssemblyPath` is optional-in-certain-situations parameter, telling the assembly path (within the framework folder) of the entrypoint assembly. It is not used for packages having only one assembly, but it must be specified if the framework folder contains multiple assemblies.
* The `ProcessSDKFrameworkPackageID` is optional-in-certain-situations parameter, telling the package ID of the framework main package. For example, for .NET Core, the framework package ID is `Microsoft.NETCore.App`. If not specified, automatic detection will be attempted.
* The `ProcessSDKFrameworkPackageVersion` is optional-in-certain-situations parameter, telling the package version for the `ProcessSDKFrameworkPackageID` parameter.
* The `DeploymentKind` is optional parameter, controlling how dependencies of the NuGet package to be deployed are handled. It has two possible values:
** `GenerateConfigFiles`: this is default value, and this will cause generation of `.deps.json` and `.runtime.config` files in the target directory, describing the locations of the dependency assemblies. This value is not supported for frameworks that are not package-based, i.e. .NET desktop.
** `CopyNonSDKAssemblies`: this value will cause all non-SDK assemblies that the package depends on to be copied in target directory. This is default value if the deployment target framework is .NET Desktop.
* The `NuGetConfigurationFile` is optional parameter, telling the location of the NuGet configuration file to be used when restoring NuGet package.
* The `TargetDirectory` is optional parameter, telling the directory where deployment assemblies and possible configuration files will be deployed. The directory will be created if it does not exist. If this parameter is not specified, then a new directory with unique name will be created in user's temporary directory.

# Output parameters
The `UtilPack.NuGet.Deployment.MSBuild.DeployNuGetPackageTask` has one output parameter.
* The `EntryPointAssemblyPath` will contain full path to the entrypoint assembly of the deployed NuGet package.

# Distribution
The task provided by this project should be loaded using [UtilPack.NuGet.MSBuild](../UtilPack.NuGet.MSBuild) task factory.
The [NuGet package](http://www.nuget.org/packages/UtilPack.NuGet.Deployment.MSBuild) has the same package ID as this folder name.