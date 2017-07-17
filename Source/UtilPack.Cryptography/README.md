# UtilPack.Cryptography

This project contains some common abstractions related to cryptography.
The `RandomGenerator` interface provides API to generate cryptographically random data, whereas `SecureRandom` uses that interface to provide cryptographically strong random generation via .NET age-old `Random` class.

There is a non-abstract implementation of the `RandomGenerator` interface in the [UtilPack.Cryptography.Digest](../UtilPack.Cryptography.Digest) project.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.Cryptography) for binary distribution.