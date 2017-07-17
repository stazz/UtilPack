# UtilPack
Home of UtilPack - library with various useful and generic stuff for .NET Desktop and Core.

## Portability
UtilPack is designed to be extremely portable.
Currently, most of the projects target .NET 4 and .NET Standard 1.0.

## Projects
There are currently 16 projects in UtilPack repository, with 1 being project for (currently small amount of) tests.
Each of the remaining 15 projects is its own NuGet package, with the NuGet package ID being the project assembly name.
The amount of projects to handle infrastructure and various assembly-loading aspects when working with NuGet packages is 8 (all of UtilPack.NuGet.* projects).
The rest of the projects fall under the broad "miscellaneous" category - to find out more about each project, browse the subdirectories of [Source](./Source) directory.

## TODO
One of the most important thing to be done is adding proper unit tests for all the code.
Due to a bit too hasty development model, and the ancientness of the project (back when making unit tests in VS wasn't as simple as it is now, in 2017), there are no unit tests that would test just the code of UtilPack.
Thus the tests are something that need to be added very soon.


# UtilPack.JSON
This project uses various types located in UtilPack in order to provide fully asynchronous functionality to serialize and deserialize JSON objects (the JToken and its derivatives in Newtonsoft.JSON package).
The deserialization functionality is available through ```UtilPack.JSON.JTokenStreamReader``` class.
The serialization functionality is available through extension method to ```UtilPack.PotentiallyAsyncWriterLogic<IEnumerable<Char>, TSink>``` class.

The UtilPack.JSON is located at http://www.nuget.org/packages/UtilPack.JSON

# UtilPack.NuGet
This project uses the ```NuGet.Client``` library to resolve package dependencies and paths to assemblies in those dependencies.

The UtilPack.NuGet is located at http://www.nuget.org/packages/UtilPack.NuGet

# UtilPack.NuGet.MSBuild
This project exposes a task factory, which executes MSBuild tasks located in NuGet packages.
These tasks are allowed to have NuGet dependencies to other packages, and are not required to bundle all of their assembly dependencies in ```build``` folder.
Indeed, they can be built just like any normal NuGet package, containing their assembly files in ```lib``` directory, and no need to reference exactly the same runtime as where they will be executed (e.g. task built against .NET Standard 1.3 will be runnable in both Desktop and Core MSBuild runtimes).

See [more detailed documentation](Source/UtilPack.NuGet.MSBuild).

TODO this needs further testing on how it behaves when target task is compiled against different MSBuild version.

The UtilPack.NuGet.MSBuild is located at http://www.nuget.org/packages/UtilPack.NuGet.MSBuild
