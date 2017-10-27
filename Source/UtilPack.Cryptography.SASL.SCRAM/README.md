# UtilPack.Cryptography.SASL.SCRAM

This is library implementing SCRAM-(SHA128|256|512) protocol without dynamically allocating any strings.
The SCRAM protocol handlers are accessible via extension methods for `BlockDigestAlgorithm` interface of [UtilPack.Cryptography.Digest](../UtilPack.Cryptography.Digest) project.

TODO modify code as needed after starting to use Span<T> (currently not the prettiest code there is).
This will require a polyfill (in UtilPack, most likely) for .NET 4.0.