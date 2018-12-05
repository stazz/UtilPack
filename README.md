[![Build status](https://ci.appveyor.com/api/projects/status/9d0wp07j7vo9f4qc/branch/develop?svg=true)](https://ci.appveyor.com/project/stazz/utilpack/branch/develop)

# UtilPack
Home of UtilPack - library with various useful and generic stuff for .NET Desktop and Core, and also for managing NuGet packages and assemblies.

## Projects
There are currently 8 projects in UtilPack repository, with 1 being project for (currently small amount of) tests.

## Migration
The UtilPack.NuGet.* projects have migrated to [NuGetUtils](https://github.com/stazz/NuGetUtils) repository.
The UtilPack.Cryptography.* projects have migrated to [FluentCryptography](https://github.com/stazz/FluentCryptography) repository.
The UtilPack.ResourcePooling.* (except .NetworkStream) projects have migrated to [ResourcePooling](https://github.com/stazz/ResourcePooling) repository.
The UtilPack.Configuration.NetworkStream and UtilPack.ResourcePooling.NetworkStream projects have migrated to [IOUtils](https://github.com/stazz/IOUtils) repository.
The UtilPack.AsyncEnumeration projet has migrated to [AsyncEnumeration](https://github.com/stazz/AsyncEnumeration) repository.

### Documentation
The current projects are browsable in the [source directory](./Source/Code).

## Portability
UtilPack is designed to be extremely portable.
Currently, most of the projects target .NET 4+ and .NET Standard 1.0+.
