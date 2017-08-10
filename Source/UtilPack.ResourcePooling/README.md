# UtilPack.ResourcePooling
This project contains interfaces that provide API to use pooled resources in synchronous and asynchronous way.
Additionally, classes completely implementing the interfaces are also exposed.
The resource pool API is also observable, allowing registering for events when resource is created, starting to be used, returned back to pool, and closed.
While the resource pools do not clean up themselves automatically nor periodically, they provide API to invoke clean up routine whenever the user or manager of the pool so requires.

Currently, synchronous API is not there - only asynchronous is implemented.

Using API of this library most usually starts with `AsyncResourcePool` interface, which acts as entrypoint for asynchronous API.
It exposes one method, `UseResourceAsync`, which accepts callback which receives the resource and performs some asychronous actions on it.

When the type constraints for the resources are known (e.g. must implement specific interface), but the exact type of the resource is unknown at compile time, the `ResourcePoolProvider` interface can be used to create a resource pool.
The configuration (e.g. file on disk) can provide the assembly name and type name of type implementing `ResourcePoolProvider`, which can be then instantiated and used to obtain various kinds (one-time-use, caching with clean-up, etc) of resource pools.

Each connection pool class will require implementation of `ResourceFactory` in order to create new instances of resource, as well as instance of parameters to give to `AcquireResourceAsync` method of `ResourceFactory`.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.ResourcePooling) for binary distribution.