# UtilPack.NuGet.Push.MSBuild

This project contains MSBuild task `UtilPack.NuGet.Push.MSBuild.PushTask`, which exposes the `PushRunner.Run` method of the [NuGet library](https://github.com/NuGet/NuGet.Client) to MSBuild users.
However, the task does not just simply call the `Run` method of the `PushRunner`, since the automatic deduction of local source (offline feed) in the `PackageUpdateResource.IsV2LocalRepository` method always returns `true` for empty folders, causing empty folders to be always treated as v2 sources.
Instead, this task will detect situation when push to local source is happening, and unless a `SkipOfflineFeedOptimization` task item metadata is set to `true`, it will treat the local source as v3 source, and use `OfflineFeedUtility.AddPackageToSource` method directly.

This task is also optimized for `push-on-build` scenario, where every time one builds the project, it is pushed to local repository.
Because of this, unless `SkipClearingLocalRepositories` task item metadata is set to `true`, the task will also delete folder from local repository cache (typically located in the `~/.nuget/packages` folder).

# Input parameters
The input parameters of the `UtilPack.NuGet.Push.MSBuild.PushTask` are listed below.
* The `PackageFilePath` is required parameter, which should be a path to the package being pushed.
* The `SourceNames` is optional parameter, and is MSBuild task item collection of all the sources where to push the package. This parameter is "optional" in a sense that omitting it does not cause errors, but then this task will simply do nothing. The task will check for the following metadata for each item.
  * The `SkipOverwriteLocalFeed` metadata is interpreted as `System.Boolean`, and if `true`, the task won't try to delete the folder before pushing to local source.
  * The `SkipClearingLocalRepositories` metadata is interpreted as `System.Boolean`, and if `true`, the task won't try to delete the folder in all package repositories after the push.
  * The `SkipOfflineFeedOptimization` metadata is interpreted as `System.Boolean`, and if `true`, the task will use the default `PushRunner.Run` method instead of `OfflineFeedUtility.AddPackageToSource` when pushing to local source.
  * The `ApiKey` metadata contains the API key used to authenticate when pushing to a remote source.
  * The `SymbolSource` metadata contains the name of the Nuget source to push the symbols package to. If omitted, no symbols will be pushed.
  * The `SymbolApiKey` metadata contains the API keys used to authenticate when pushing to a remote symbol source.
* The `NuGetConfigurationFilePath` is optional parameter, containing the path to the NuGet configuration file. By default, machine-wide configuration file will be used.
* The `RetryTimeoutForDirectoryDeletionFail` is optional parameter, which is used when deleting existing source directory (when the source to push is local feed, and `SkipOverwriteLocalFeed` item metadata is not `true`) and deleting existing package repository folder (when the `SkipClearingLocalRepositories` item metadata is not `true`). This parameter contains the timeout, in milliseconds, how much to wait between 2nd attempt to delete the existing source directory. By default the value is `500`. Scenarios when this is useful and needed is when there exist file watchers for the folder being deleted, which causes the first delete to fail. If file watchers then immediately dispose themselves, the second deletion will be successful.

# Output parameters
This task has no output parameters.

# Example
Here is a small example of how to use this task within your `.csproj` file:
```xml
  <ItemGroup>
    <PackageReference Include="UtilPack.NuGet.MSBuild" Version="2.4.0"/>
  </ItemGroup>
  <UsingTask
     Condition=" '$(UtilPackNuGetMSBuildAssemblyPath)' != '' "
     TaskFactory="UtilPack.NuGet.MSBuild.NuGetTaskRunnerFactory"
     AssemblyFile="$(UtilPackNuGetMSBuildAssemblyPath)"
     TaskName="UtilPack.MSBuild.AsyncExec.AsyncExecTask">
    <Task>
      <NuGetTaskInfo>
        <PackageID>UtilPack.NuGet.Push.MSBuild</PackageID>
        <PackageVersion>2.0.0</PackageVersion>
      </NuGetTaskInfo>
    </Task>
  </UsingTask>

   <Target Name="AfterBuild">
    <UtilPack.NuGet.Push.MSBuild.PushTask
      PackageFilePath="path/to/file.nupkg"
      SourceNames="items with the names of the sources to push to"
      />
  </Target>
```

# Distribution
The [NuGet package](http://www.nuget.org/packages/UtilPack.NuGet.Push.MSBuild) has the same package ID as this folder name.
__The task provided by this project should be loaded using [UtilPack.NuGet.MSBuild](../UtilPack.NuGet.MSBuild) task factory.__