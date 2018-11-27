# UtilPack.Configuration
This project contains one class (`DynamicConfigurableTypeLoader`) with one method (`InstantiateWithConfiguration`), which makes it easy to process configurations (the ones of [Microsoft.Extensions.Configuration package](http://www.nuget.org/packages/Microsoft.Extensions.Configuration) which require dynamic type loading.
The project does not specify the actual type loader logic, but provides a facility where with one single callback one is able to recursively load an instance of an object with a dynamic type.

One scenario to use this project is when one has a number of components/plugins, which implement certain interface and are located in certain NuGet packages.
The type loading callback provided to `DynamicConfigurableTypeLoader` class in this project can then use `BoundRestoreCommandUser` and extension methods declared on that class in [UtilPack.NuGet](../UtilPack.NuGet) project to load types from NuGet packages, and even restore and install missing packages beforehand.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.Configuration) for binary distribution.