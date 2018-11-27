///*
// * Copyright 2017 Stanislav Muhametsin. All rights Reserved.
// *
// * Licensed  under the  Apache License,  Version 2.0  (the "License");
// * you may not use  this file  except in  compliance with the License.
// * You may obtain a copy of the License at
// *
// *   http://www.apache.org/licenses/LICENSE-2.0
// *
// * Unless required by applicable law or agreed to in writing, software
// * distributed  under the  License is distributed on an "AS IS" BASIS,
// * WITHOUT  WARRANTIES OR CONDITIONS  OF ANY KIND, either  express  or
// * implied.
// *
// * See the License for the specific language governing permissions and
// * limitations under the License. 
// */
//using FluentCryptography.Digest;
//using FluentCryptography.SASL;
//using FluentCryptography.SASL.SCRAM;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace UtilPack.Tests.Cryptography.SASL.SCRAM
//{
//   [TestClass]
//   public class SCRAMTest
//   {
//      // This tests with messages defined in RFC-5802 example (section 5) and RFC-7677 (section 3)
//      [DataTestMethod,
//         DataRow( typeof( SHA128 ), "user", "pencil", "fyko+d2lbbFgONRv9qkxdawL", "QSXCR+Q6sek8bf92", "3rfcNHYJY1ZVvWVs7j", 4096, "v0X8v3Bz2T0CJGbJQyF0X+HI4Ts=", "rmF9pqV8S7suAoZWja4dJRkFsKQ=", "HZbuOlKbWl+eR8AfIposuKbhX30=" ),
//         DataRow( typeof( SHA256 ), "user", "pencil", "rOprNGfwEbeRWgbNEkqO", "W22ZaJ0SNY7soEsUEjb6gQ==", "%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0", 4096, "dHzbZapWIk4jUhN+Ute9ytag9zjfMHgsqmmiz7AndVQ=", "6rriTRBi23WpRR/wtup+mMhUZUn/dB5nLTJRsjl95G4=", "xKSVEDI6tPlSysH6mUQZOeeOp01r6B3fcJbodRPcYV0=" )
//         ]
//      public void TestSCRAMClient(
//         Type algorithmType,
//         String username,
//         String password,
//         String clientNonce,
//         String serverSalt,
//         String serverNonce,
//         Int32 iterationCount,
//         String clientProof,
//         String serverProof,
//         String clientPasswordDigest
//         )
//      {

//         var encoding = new UTF8Encoding( false, false ).CreateDefaultEncodingInfo();
//         using ( var client = ( (BlockDigestAlgorithm) Activator.CreateInstance( algorithmType ) ).CreateSASLClientSCRAM( () => encoding.Encoding.GetBytes( clientNonce ) ) )
//         {
//            var writeArray = new ResizableArray<Byte>();
//            var credentials = new SASLCredentialsSCRAMForClient(
//               username,
//               password
//               );

//            // Phase 1.
//            (var bytesWritten, var challengeResult) = client.ChallengeAsync( credentials.CreateChallengeArguments(
//               null, // Initial phase does not read anything
//               -1,
//               -1,
//               writeArray,
//               0,
//               encoding
//               ) ).Result.First;
//            Assert.IsTrue( bytesWritten > 0 );
//            Assert.AreEqual( challengeResult, SASLChallengeResult.MoreToCome );
//            Assert.AreEqual( "n,,n=" + username + ",r=" + clientNonce, encoding.Encoding.GetString( writeArray.Array, 0, bytesWritten ) );

//            // Phase 2.
//            var serverBytes = encoding.Encoding.GetBytes( "r=" + clientNonce + serverNonce + ",s=" + serverSalt + ",i=" + iterationCount );
//            (bytesWritten, challengeResult) = client.ChallengeAsync( credentials.CreateChallengeArguments(
//               serverBytes,
//               0,
//               serverBytes.Length,
//               writeArray,
//               0,
//               encoding
//               ) ).Result.First;
//            Assert.IsTrue( bytesWritten > 0 );
//            Assert.AreEqual( challengeResult, SASLChallengeResult.MoreToCome );
//            Assert.AreEqual( "c=biws,r=" + clientNonce + serverNonce + ",p=" + clientProof, encoding.Encoding.GetString( writeArray.Array, 0, bytesWritten ) );
//            Assert.AreEqual( clientPasswordDigest, Convert.ToBase64String( credentials.PasswordDigest ) );

//            // Phase 3
//            serverBytes = encoding.Encoding.GetBytes( "v=" + serverProof );
//            (bytesWritten, challengeResult) = client.ChallengeAsync( credentials.CreateChallengeArguments(
//               serverBytes,
//               0,
//               serverBytes.Length,
//               writeArray,
//               0,
//               encoding
//               ) ).Result.First;
//            Assert.AreEqual( challengeResult, SASLChallengeResult.Completed );
//            Assert.AreEqual( bytesWritten, 0 );
//         }

//      }

//      [DataTestMethod,
//         DataRow( typeof( SHA128 ), "user", "pencil", "6dlGYMOdZcOPutkcNY8U2g7vK9Y=", "D+CSWLOshSulAsxiupA+qs2/fTE=", "QSXCR+Q6sek8bf92", 4096 ),
//         DataRow( typeof( SHA256 ), "user", "pencil", "WG5d8oPm3OtcPnkdi4Uo7BkeZkBFzpcXkuLmtbsT4qY=", "wfPLwcE6nTWhTAmQ7tl2KeoiWGPlZqQxSrmfPwDl2dU=", "W22ZaJ0SNY7soEsUEjb6gQ==", 4096 )
//      ]
//      public void TestSCRAMClientAndServerInterop(
//         Type algorithmType,
//         String username,
//         String password,
//         String clientKeyDigest, // H(key_c)
//         String serverKey, // key_s
//         String serverSalt,
//         Int32 iterationCount
//         )
//      {
//         var encoding = new UTF8Encoding( false, false ).CreateDefaultEncodingInfo();
//         var serverCallbackCalled = 0;
//         using ( var client = ( (BlockDigestAlgorithm) Activator.CreateInstance( algorithmType ) ).CreateSASLClientSCRAM() )
//         using ( var server = ( (BlockDigestAlgorithm) Activator.CreateInstance( algorithmType ) ).CreateSASLServerSCRAM( async serverUsername =>
//         {
//            Assert.AreEqual( username, serverUsername );

//            // Simulate fetching data from DB (each user should have all these 4 attributes)
//            await Task.Delay( 100 );

//            Interlocked.Exchange( ref serverCallbackCalled, 1 );
//            return new SASLCredentialsSCRAMForServer(
//               Convert.FromBase64String( clientKeyDigest ),
//               Convert.FromBase64String( serverKey ),
//               Convert.FromBase64String( serverSalt ),
//               iterationCount
//               );
//         } ) )
//         {
//            var clientWriteArray = new ResizableArray<Byte>();
//            var clientCredentials = new SASLCredentialsSCRAMForClient(
//               username,
//               password
//               );

//            var serverWriteArray = new ResizableArray<Byte>();
//            var serverCredentials = new SASLCredentialsHolder();

//            // Client-first
//            (var bytesWritten, var challengeResult) = client.ChallengeAsync( clientCredentials.CreateChallengeArguments(
//               null, // Initial phase does not read anything
//               -1,
//               -1,
//               clientWriteArray,
//               0,
//               encoding
//               ) ).Result.First;
//            Assert.IsTrue( bytesWritten > 0 );
//            Assert.AreEqual( challengeResult, SASLChallengeResult.MoreToCome );

//            // Server-first
//            (bytesWritten, challengeResult) = server.ChallengeAsync( serverCredentials.CreateServerMechanismArguments(
//               clientWriteArray.Array,
//               0,
//               bytesWritten,
//               serverWriteArray,
//               0,
//               encoding
//               ) ).Result.First;
//            Assert.IsTrue( bytesWritten > 0 );
//            Assert.AreEqual( challengeResult, SASLChallengeResult.MoreToCome );
//            Assert.AreNotEqual( 0, serverCallbackCalled );

//            // Client-final
//            (bytesWritten, challengeResult) = client.ChallengeAsync( clientCredentials.CreateChallengeArguments(
//               serverWriteArray.Array,
//               0,
//               bytesWritten,
//               clientWriteArray,
//               0,
//               encoding
//               ) ).Result.First;
//            Assert.IsTrue( bytesWritten > 0 );
//            Assert.AreEqual( challengeResult, SASLChallengeResult.MoreToCome );

//            // Server-final
//            (bytesWritten, challengeResult) = server.ChallengeAsync( serverCredentials.CreateServerMechanismArguments(
//               clientWriteArray.Array,
//               0,
//               bytesWritten,
//               serverWriteArray,
//               0,
//               encoding
//               ) ).Result.First;
//            Assert.IsTrue( bytesWritten > 0 );
//            Assert.AreEqual( challengeResult, SASLChallengeResult.Completed );

//            // Client-validate
//            (bytesWritten, challengeResult) = client.ChallengeAsync( clientCredentials.CreateChallengeArguments(
//               serverWriteArray.Array,
//               0,
//               bytesWritten,
//               clientWriteArray,
//               0,
//               encoding
//               ) ).Result.First;
//            Assert.AreEqual( bytesWritten, 0 );
//            Assert.AreEqual( challengeResult, SASLChallengeResult.Completed );
//         }
//      }
//   }
//}
