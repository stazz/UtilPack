# UtilPack.MSBuild.AsyncExec

This project contains a MSBuild task that will start a process like normal [Exec](https://docs.microsoft.com/en-us/visualstudio/msbuild/exec-task) task, but will not wait for the process to exit.

All the parameters are same as the [Exec task](https://docs.microsoft.com/en-us/visualstudio/msbuild/exec-task), since this task extends it.

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
        <PackageID>UtilPack.MSBuild.AsyncExec</PackageID>
        <PackageVersion>1.0.0</PackageVersion>
      </NuGetTaskInfo>
    </Task>
  </UsingTask>

   <Target Name="AfterBuild">
    <UtilPack.MSBuild.AsyncExec.AsyncExecTask
        Command="path/to/executable" />
  </Target>
```

# Distribution

The [NuGet package](http://www.nuget.org/packages/UtilPack.MSBuild.AsyncExec) has the same package ID as this folder name.
__The task provided by this project should be loaded using [UtilPack.NuGet.MSBuild](../UtilPack.NuGet.MSBuild) task factory.__