# UtilPack.NuGet.Deployment

This project contains a class, aptly named `NuGetDeployment`, which will help in situations when it is needed to deploy a binary distribution of NuGet package, so that the assembly of the NuGet package could be executed by `dotnet` .NET Core runner.
The `NuGetDeployment` accepts configuration containing various parameters as its single argument to constructor, and exposes single method, `DeployAsync`, which will perform the deploying.
The first step of deploying of NuGet package is to restore the target package if it is missing from current machine, and the assemblies in NuGet package are copied to target folder specified in configuration passed to the constructor of `NuGetDeployment`.
Then, depending on the values in the configuration, either `.deps.json` file is generated, or all binary dependencies (assemblies) are copied to target folder.

See [NuGet package](http://www.nuget.org/packages/UtilPack.NuGet.Deployment) for binary distribution.