# UtilPack.Configuration.NetworkStream

This library contains types commonly used to define configurable remote endpoint information, and other information related to protocols that work over networks.
The `NetworkConnectionCreationInfo` is the entrypoint type, containing passive data information (`NetworkConnectionCreationInfoData`), which can be deserialized e.g. using [Microsoft.Extensions.Configuration.Binder](http://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder) package.
This entrypoint type also contains some callbacks used when establishing SSL encrypted connection.

This library is typically used together with [UtilPack.ResourcePooling.NetworkStream](../UtilPack.ResourcePooling.NetworkStream) package, in order to establish connection to remote endpoint.
This socket-based connection is typically facaded by protocol-specific type, like the [CBAM](../../CBAM) project does, however both connection establishing and facading is out of scope for this library.

# Distribution

See [NuGet package](http://www.nuget.org/packages/UtilPack.Configuration.NetworkStream) for binary distribution.