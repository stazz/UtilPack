# UtilPack.Cryptography.SASL.SCRAM

This is library implementing SCRAM-(SHA-1|SHA-256|SHA-512) protocol without dynamically allocating any strings.
The SCRAM protocol handlers are accessible via extension methods for BlockDigestAlgorithm interface of UtilPack.Cryptography.Digest project.

Here is an example for authenticating as a client:
```csharp
using UtilPack.Cryptography.Digest;

// Example of using SCRAM-SHA-256
// Variables username, password, and stream are assumed to be coming from elsewhere in this example.
using ( var client = new SHA256().CreateSASLClientSCRAM() )
{
  var encoding = new UTF8Encoding( false, false ).CreateDefaultEncodingInfo();
  var writeArray = new ResizableArray<Byte>();
  var credentials = new SASLCredentialsSCRAMForClient(
    username,
    password // password may be clear-text password as string, or result of PBKDF2 iteration as byte array.
    );

  // Create client-first message
  (var bytesWritten, var challengeResult) = await client.ChallengeOrThrowOnErrorAsync( credentials.CreateChallengeArguments(
    null, // Initial phase does not read anything
    -1,
    -1,
    writeArray,
    0,
    encoding
    ) );

  // Write client-first message
  await stream.WriteAsync( writeArray.Array, 0, bytesWritten );

  // Read server-first message
  var readBytes = new Byte[10000]; // Assume static max size for this small example
  var readCount = await stream.ReadAsync( readBytes, 0, readBytes.Length ); // Assume this simple and naïve read for this small example

  // Create client-final message
  (bytesWritten, challengeResult) = await client.ChallengeOrThrowOnErrorAsync( credentials.CreateChallengeArguments(
    readBytes,
    0,
    readCount,
    writeArray,
    0,
    encoding
    ) );

  // At this point, credentials.PasswordDigest will contain result of PBKDF2 iteration, if cleartext password was specified earlier

  // Write client-final message
  await stream.WriteAsync( writeArray.Array, 0, bytesWritten );

  // Read server-final message
  var readCount = await stream.ReadAsync(readBytes, 0, readBytes.Length );
  
  // Validate server-final message
  (bytesWritten, challengeResult) = await client.ChallengeOrThrowOnErrorAsync( credentials.CreateChallengeArguments(
    readBytes,
    0,
    readCount,
    writeArray,
    0,
    encoding
    ) );

  // Now bytesWritten will be 0, and challengeResult will be SASLChallengeResult.Completed.
  // An exception will be thrown on authentication error, or if server sents wrong messaage.
}
```

# Distribution

See [NuGet package](http://www.nuget.org/packages/UtilPack.Cryptography.SASL.SCRAM) for binary distribution.

# TODO 
Modify code as needed after starting to use Span<T> (currently, the code for client and server SCRAM not the prettiest code there is).
This will require a polyfill (in UtilPack, most likely) for .NET 4.0.
