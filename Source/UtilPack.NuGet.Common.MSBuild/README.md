# UtilPack.NuGet.Common.MSBuild

This small project contains common code for scenarios where [NuGet library](https://github.com/NuGet/NuGet.Client) is used inside a MSBuild task.
Currently, there is only one class, `NuGetMSBuildLogger` which implements the NuGet's `ILogger` interface, and passes the log messages to given `IBuildEngine`.
This project is used by the [task factory loading tasks from NuGet packages](../UtilPack.NuGet.MSBuild), [task to deploy a NuGet package](../UtilPack.NuGet.Deployment.MSBuild), and [task to push NuGet package to a source feed](../UtilPack.NuGet.Push.MSBuild).

# Distribution

See [NuGet package](http://www.nuget.org/packages/UtilPack.NuGet.Common.MSBuild) for binary distribution.