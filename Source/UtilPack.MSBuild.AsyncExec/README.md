# UtilPack.MSBuild.AsyncExec

This project contains a MSBuild task that will start a process like normal [Exec](https://docs.microsoft.com/en-us/visualstudio/msbuild/exec-task) task, but will not wait for the process to exit.

All the parameters are same as the [Exec task](https://docs.microsoft.com/en-us/visualstudio/msbuild/exec-task), since this task extends it.

# Distribution

The [NuGet package](http://www.nuget.org/packages/UtilPack.MSBuild.AsyncExec) has the same package ID as this folder name.
__The task provided by this project should be loaded using [UtilPack.NuGet.MSBuild](../UtilPack.NuGet.MSBuild) task factory.__