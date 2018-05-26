# UtilPack.ResourcePooling.NetworkStream

This project contains common code when dealing with resources which utilize `System.Net.Sockets.NetworkStream` and underlying `System.Net.Sockets.Socket`.
Because the actual `System.Net.Sockets.NetworkStream` may be (and usually is) encrypted, it is exposed via `System.IO.Stream` class it inherits (since `System.Net.Security.SslStream` inherits `System.IO.Stream` as well).

The `NetworkStreamFactory` classes (the one without generic type parameters, and the one with one generic type parameter) implement the `AsyncResourceFactory` interface from [UtilPack.ResourcePooling](../UtilPack.ResourcePooling) project.
This allows to easily create resource pools which behave in various ways by using extension methods on `NetworkStreamFactory`.
These resource pools may be then used to easily get and return instances of `System.IO.Stream`, abstracting away the actual logic of possible DNS resolve and SSL stream creation.

The most common usecase it to have a configuration object inheriting from the ones defined in [UtilPack.Configuration.NetworkStream](../UtilPack.Configuration.NetworkStream), and then use extension methods in this library to create `NetworkStreamFactoryConfiguration`.
This created object may be used to call asynchronous socket and stream initialization method `NetworkStreamFactory.AcquireNetworkStreamFromConfiguration`.
Typically the socket and stream are then hidden behind some protocol-specific facade, like the ones in [CBAM](../../CBAM) project, but that is out of scope of this library.

# Distribution

See [NuGet package](http://www.nuget.org/packages/UtilPack.ResourcePooling.NetworkStream) for binary distribution.