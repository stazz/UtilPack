# UtilPack
Home of UtilPack - library with various useful and generic stuff for .NET Desktop and Core, and also for managing NuGet packages and assemblies.

## Projects
There are currently 19 projects in UtilPack repository, with 1 being project for (currently small amount of) tests.
Each of the remaining 18 projects is its own NuGet package, with the NuGet package ID being the project assembly name.
The amount of projects to handle infrastructure and various assembly-loading aspects when working with NuGet packages is 8 (all of UtilPack.NuGet.* projects).
The rest of the projects fall under the broad "miscellaneous" category.

### UtilPack
This package contains the most generic and all-round useful extension methods, interfaces, and classes.

[Read More ->](./Source/UtilPack)

### UtilPack.AsyncEnumeration
This package contains API and full implementation for enumerating asynchronous enumerables, which [are currently still under design to get to C# itself](https://github.com/dotnet/csharplang/issues/43).

[Read More ->](./Source/UtilPack.AsyncEnumeration)

### UtilPack.Configuration
This small package contains class which uses given callback to recursively load instances of types contained in [configuration](http://www.nuget.org/packages/Microsoft.Extensions.Configuration) describing assemblies and type names to load.

[Read More ->](./Source/UtilPack.Configuration)

### UtilPack.Cryptography
This small package contains API and skeleton implementation for various aspects of cryptography, since they are lacking in e.g. .NET Standard 1.0.

[Read More ->](./Source/UtilPack.Cryptography)

### UtilPack.Cryptography.Digest
This package contains API and implementation for most commonly used digest algorithms (SHA-128,256,384, and 512), as well as concrete implementation for digest-based random data generator API in [UtilPack.Cryptography](./Source/UtilPack.Cryptography) package.

[Read More ->](./Source/UtilPack.Cryptography.Digest)

### UtilPack.JSON
This package contains implementation that uses (de)serialization types in [UtilPack](./Source/UtilPack) package in order to (de)serialize JSON objects.

[Read More ->](./Source/UtilPack.JSON)

### UtilPack.MSBuild.AsyncExec
This small package contains one class, `AsyncExecTask`, which extends [Exec](https://docs.microsoft.com/en-us/visualstudio/msbuild/exec-task) MSBuild task to start a process but doesn't wait for it to terminate.

[Read More ->](./Source/UtilPack.MSBuild.AsyncExec)

### UtilPack.ProcessMonitor
This package contains `ProcessMonitor` class, which can be used to start and monitor other processes, with a support of graceful shutdown and restart.

[Read More ->](./Source/UtilPack.ProcessMonitor)

### UtilPack.ResourcePooling
This package contains API and implementation for pooling and using the pool of asynchronous and synchronous resources.

[Read More ->](./Source/UtilPack.ResourcePooling)

### UtilPack.TabularData
This package contains API and skeleton implementation for data which is in tabular format, meaning it has rows and columns.

[Read More ->](./Source/UtilPack.TabularData)

### UtilPack.NuGet
This package contains classes and methods that make life easier when working with [NuGet library](https://github.com/NuGet/NuGet.Client), most notably this package has `BoundRestoreCommandUser` to easily restore packages on local machine.

[Read More ->](./Source/UtilPack.NuGet)

### UtilPack.NuGet.AssemblyLoading
This package contains API and implementation for NuGet package -based assembly loader, which can operate in both .NET Desktop and Core environments.

[Read More ->](./Source/UtilPack.NuGet.AssemblyLoading)

### UtilPack.NuGet.Common.MSBuild
This package contains utilities usually used in MSBuild tasks which operate with NuGet packages, e.g. logger class which will relay all NuGet log messages to MSBuild's `IBuildEngine`.

[Read More ->](./Source/UtilPack.NuGet.Common.MSBuild)

### UtilPack.NuGet.Deployment
This package contains `NuGetDeployment` class which will deploy an assembly from NuGet package so that it could be executed by .NET (Desktop or Core) environment (by restoring packages and generating `.deps.json` file or copying all referenced assemblies into target folder).

[Read More ->](./Source/UtilPack.NuGet.Deployment)

### UtilPack.NuGet.Deployment.MSBuild
This package wraps the functionality of [UtilPack.NuGet.Deployment](./Source/UtilPack.NuGet.Deployment) package into MSBuild task.

[Read More ->](./Source/UtilPack.NuGet.Deployment.MSBuild)

### UtilPack.NuGet.MSBuild
This package contains a MSBuild task factory, which will restore and execute MSBuild tasks located in given NuGet packages, dynamically loading dependencies (assemblies from other NuGet packages) on-the-fly as needed.

[Read More ->](./Source/UtilPack.NuGet.MSBuild)

### UtilPack.NuGet.ProcessRunner
This package deploys a NuGet package (restoring it if needed), and executes entrypoint assembly as .NET process, combining functionality of [UtilPack.NuGet.Deployment](./Source/UtilPack.NuGet.Deployment) and [UtilPack.ProcessMonitor](./Source/UtilPack.ProcessMonitor) packages.

[Read More ->](./Source/UtilPack.NuGet.ProcessRunner)

### UtilPack.NuGet.Push.MSBuild
This package provides functionality of `PushRunner.Run` method of the [NuGet library](https://github.com/NuGet/NuGet.Client) as MSBuild task, which can be integrated into build process to automatically push package on every build.

[Read More ->](./Source/UtilPack.NuGet.Push.MSBuild)

## Portability
UtilPack is designed to be extremely portable.
Currently, most of the projects target .NET 4 and .NET Standard 1.0.

## TODO
One of the most important thing to be done is adding proper unit tests for all the code.
Due to a bit too hasty development model, and the ancientness of the project (back when making unit tests in VS wasn't as simple as it is now, in 2017), there are no unit tests that would test just the code of UtilPack.
Thus the tests are something that need to be added very soon.