# UtilPack
This project contains the most common and generic code, which is missing from .NET Desktop or Core, or both.
The aspects include serialization (including big-endian and little-endian numeric primitives writing and reading), async utilities, and various other small tweaks and improvements.
Part of the resulting library is also written in IL code, as required functionality could not be done in C# (e.g. `SizeOf.Type<T>` method, and  constraining generic parameter to subclass of `System.Delegate`).

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack) for binary distribution.