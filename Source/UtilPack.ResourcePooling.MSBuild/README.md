# UtilPack.ResourcePooling.MSBuild

Acting as a bridge between [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project and MSBuild world, this project provides only one type, `AbstractResourceUsingTask`.
This type should be extended by MSBuild custom tasks, which intend to use the [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project to deliver their functionality.
One such example of task is a [task](https://github.com/stazz/CBAM/tree/develop/Source/CBAM.SQL.MSBuild) which dumps the contents of SQL file into the database.

The `AbstractResourceUsingTask` class implements canceability fully, and leaves two abstract methods for derived classes to implement.
Most of other methods are virtual, allowing derived classes to override them.

## Abstract methods
### CheckTaskParametersBeforeResourcePoolUsage

This method is used as a sanity check before actually starting to search for `AsyncResourceFactoryProvider` type from [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project.
The SQL dump running task could here e.g. check whether file exists.

### UseResource

This method should perform the actual domain-specific functionality, using the resource it receives as parameter.
The SQL dump running task would here read the SQL statements from file and execute them using the resource (which would be [of type `SQLConnection`](https://github.com/stazz/CBAM/tree/develop/Source/CBAM.SQL)).

## Parameters

The `AbstractResourceUsingTask` introduces a number of task parameters, none of them statically marked as required, since most of the implementation can be overridden.
However, the default implementation, unless overridden, requires the following parameters:
* `PoolProviderPackageID` of type `String`: should specify the NuGet package ID of the package holding type implementing the `AsyncResourceFactoryProvider` type from [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project.
* `PoolConfigurationFileContents` of type `String`: sometimes it is more conventient to give configuration file contents directly, then this property should be used. This property takes precedence over `PoolConfigurationFilePath`.
* `PoolConfigurationFilePath` of type `String`: should specify the path to JSON file containing serialized configuration for object passed as parameter to `CreateOneTimeUseResourcePool` method of `AsyncResourceFactoryProvider` type from [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project. The [Microsoft.Extensions.Configuration.Json](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json/) package will be used to deserialize the value.

The default implementation, unless overridden, has the following optional parameters:
* `PoolProviderVersion` of type `String`: the version part paired with `PoolProviderPackageID`, should specify the version of the NuGet package holding type implementing `AsyncResourceFactoryProvider` type from [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project. If not specified, newest version will be used.
* `PoolProviderAssemblyPath` of type `String`: the path within the NuGet package specified by `PoolProviderPackageID` and `PoolProviderVersion` parameters, where the assembly holding type implementing `AsyncResourceFactoryProvider` type from [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project resides. Is used only for NuGet packages with more than one assembly in their framework-specific folder.
* `PoolProviderTypeName` of type `String`: once the assembly is loaded using `PoolProviderPackageID`, `PoolProviderVersion` and `PoolProviderAssemblyPath` parameters, this parameter may be used to specify the name of the type implementing `AsyncResourceFactoryProvider` type from [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project. If left out, the first suitable type from all types defined in the assembly will be used.
* `RunSynchronously` of type `Boolean`: this is infrastructure-related parameter, and actually is always used, since the usage is in non-virtual method. This parameter, if `true`, will skip calling [Yield](https://docs.microsoft.com/en-us/dotnet/api/microsoft.build.framework.ibuildengine3.yield) method.

# Distribution
The [NuGet package](http://www.nuget.org/packages/UtilPack.NuGet.Deployment.MSBuild) has the same package ID as this folder name.
__The task provided by this project should be loaded using [UtilPack.NuGet.MSBuild](../UtilPack.NuGet.MSBuild) task factory.__
