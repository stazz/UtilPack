# UtilPack.ResourcePooling.NetworkStream

This project contains common code when dealing with resources which utilize `System.Net.Sockets.NetworkStream` and underlying `System.Net.Sockets.Socket`.
Because the actual `System.Net.Sockets.NetworkStream` may be (and usually is) encrypted, it is exposed via `System.IO.Stream` class it inherits (since `System.Net.Security.SslStream` inherits `System.IO.Stream` as well).

The `NetworkStreamFactory` classes (the one without generic type parameters, and the one with one generic type parameter) implement the `AsyncResourceFactory` interface from [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project.
This allows to easily create resource pools which behave in various ways by using extension methods on `NetworkStreamFactory`.
These resource pools may be then used to easily get and return instances of `System.IO.Stream`, abstracting away the actual logic of possible DNS resolve and SSL stream creation.

# Distribution

See [NuGet package](http://www.nuget.org/packages/UtilPack.ResourcePooling.NetworkStream) for binary distribution.