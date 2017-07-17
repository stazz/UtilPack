# UtilPack.Cryptography

This project contains some common abstractions related to cryptography.
The `RandomGenerator` interface provides API to generate cryptographically random data, whereas `SecureRandom` uses that interface to provide cryptographically strong random generation via .NET age-old `Random` class.

The `RandomGenerator` interface contains non-abstract implementation in [UtilPack.Cryptography.Digest](../UtilPack.Cryptography.Digest) project.

See [NuGet package](http://www.nuget.org/packages/UtilPack.Cryptography) for binary distribution.