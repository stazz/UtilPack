# UtilPack.Cryptography.SASL

This project contains the general purpose interfaces and utilities for SASL protocol.
The `SASLMechanism` interface is meant useable for any SASL mechanism, both client-side and server-side.
The conrecte implementations for `SASLMechanism` are available in e.g. [UtilPack.Cryptography.SASL.SCRAM](../UtilPack.Cryptography.SASL.SCRAM) project.

The `SASLUtility` class contains method related to string processing in SASL.
There are also some skeleton implementations for `SASLMechanism` interface: `AbstractSyncSASLMechanism` for mechanisms which are always synchronous, `AbstractAsyncSASLMechanism` for mechanisms which may potentially be asynchronous, and `AbstractServerSASLMechanism` for typical server-side SASL mechanism implementation.

# Distribution

See [NuGet package](http://www.nuget.org/packages/UtilPack.Cryptography.SASL) for binary distribution.