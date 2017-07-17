# UtilPack
Home of UtilPack - library with various useful and generic stuff for .NET Desktop and Core, and also for managing NuGet packages and assemblies.

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