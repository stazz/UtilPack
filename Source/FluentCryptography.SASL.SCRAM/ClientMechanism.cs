/*
 * Copyright 2017 Stanislav Muhametsin. All rights Reserved.
 *
 * Licensed  under the  Apache License,  Version 2.0  (the "License");
 * you may not use  this file  except in  compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed  under the  License is distributed on an "AS IS" BASIS,
 * WITHOUT  WARRANTIES OR CONDITIONS  OF ANY KIND, either  express  or
 * implied.
 *
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */
using FluentCryptography.Digest;
using FluentCryptography.SASL;
using FluentCryptography.SASL.SCRAM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UtilPack;
using TSyncChallengeResult = UtilPack.EitherOr<System.ValueTuple<System.Int32, FluentCryptography.SASL.SASLChallengeResult>, System.Int32>;


namespace FluentCryptography.SASL.SCRAM
{


   internal sealed class SASLMechanismSCRAMForClient : AbstractSyncSASLMechanism<SASLCredentialsSCRAMForClient>
   {

      private static readonly Byte[] ClientKeyBytes = new Byte[] // No Encoding.ASCII in .NET Standard 1.0
      {
         (Byte)'C', (Byte)'l', (Byte)'i', (Byte)'e', (Byte)'n', (Byte)'t', (Byte)' ', (Byte)'K', (Byte)'e', (Byte)'y'
      };
      private static readonly Byte[] ServerKeyBytes = new Byte[]
      {
         (Byte)'S', (Byte)'e', (Byte)'r', (Byte)'v', (Byte)'e', (Byte)'r', (Byte)' ', (Byte)'K', (Byte)'e', (Byte)'y'
      };


      private const Int32 STATE_INITIAL = 0;
      private const Int32 STATE_FINAL = 1;
      private const Int32 STATE_VALIDATE = 2;
      private const Int32 STATE_COMPLETED = 3;

      private Int32 _state;
      private readonly Func<Byte[]> _nonceGenerator;
      private readonly BlockDigestAlgorithm _algorithm;

      private readonly ResizableArray<Byte> _clientMessage; // Temporary storage for data to be preserved between Challenge invocations

      public SASLMechanismSCRAMForClient(
         BlockDigestAlgorithm algorithm,
         Func<Byte[]> clientNonceGenerator
         )
      {
         this._algorithm = ArgumentValidator.ValidateNotNull( nameof( algorithm ), algorithm );
         this._nonceGenerator = clientNonceGenerator;
         this._clientMessage = new ResizableArray<Byte>( exponentialResize: false );
      }

      protected override TSyncChallengeResult Challenge(
         ref SASLChallengeArguments args,
         SASLCredentialsSCRAMForClient credentials
         )
      {
         var prevState = this._state;
         var challengeResult = SASLChallengeResult.MoreToCome;
         var writeOffset = args.WriteOffset;

         Int32 errorCode;
         Int32 nextState;
         if ( credentials == null )
         {
            errorCode = SCRAMCommon.ERROR_CLIENT_SUPPLIED_WITH_INVALID_CREDENTIALS;
            nextState = -1;
         }
         else
         {
            switch ( prevState )
            {
               case STATE_INITIAL:
                  errorCode = this.PerformInitial( ref args, credentials, ref writeOffset );
                  nextState = STATE_FINAL;
                  break;
               case STATE_FINAL:
                  // Read server response and write our response
                  errorCode = this.PerformFinal( ref args, credentials, ref writeOffset );
                  nextState = STATE_VALIDATE;
                  break;
               case STATE_VALIDATE:
                  errorCode = this.PerformValidate( ref args, credentials );
                  nextState = STATE_COMPLETED;
                  challengeResult = SASLChallengeResult.Completed;
                  break;
               default:
                  nextState = -1;
                  errorCode = SCRAMCommon.ERROR_INVALID_STATE;
                  break;
            }
         }

         TSyncChallengeResult retVal;
         if ( errorCode != 0
            || nextState < 0
            || Interlocked.CompareExchange( ref this._state, nextState, prevState ) != prevState )
         {
            retVal = new TSyncChallengeResult( errorCode == 0 ? SCRAMCommon.ERROR_CONCURRENT_ACCESS : errorCode );
         }
         else
         {
            retVal = new TSyncChallengeResult( (writeOffset - args.WriteOffset, challengeResult) );
         }

         return retVal;
      }

      protected override Int32 GetExceptionErrorCode( Exception exc )
         => SCRAMCommon.ERROR_INVALID_RESPONSE_MESSAGE_FORMAT;

      protected override void Dispose( Boolean disposing )
      {
         if ( disposing )
         {
            this._algorithm.DisposeSafely();
            this.ResetThis();
         }
      }

      public override void Reset()
      {
         this._algorithm.Reset();
         this.ResetThis();
      }

      private void ResetThis()
      {
         this._clientMessage.Array.Clear();
         Interlocked.Exchange( ref this._state, STATE_INITIAL );
      }

      private Int32 PerformInitial(
         ref SASLChallengeArguments args,
         SASLCredentialsSCRAMForClient credentials,
         ref Int32 writeOffset
         )
      {
         var encoding = args.Encoding;
         var array = args.WriteArray;

         // Write "n,,n=<username>,r=<client nonce>
         // Write "n,,n="

         array.CurrentMaxCapacity = encoding.Encoding.GetByteCount( SCRAMCommon.CLIENT_FIRST_PREFIX_1 + SCRAMCommon.CLIENT_FIRST_PREFIX_2 );
         encoding.WriteString( array.Array, ref writeOffset, SCRAMCommon.CLIENT_FIRST_PREFIX_1 );
         var messageStart = writeOffset;
         encoding.WriteString( array.Array, ref writeOffset, SCRAMCommon.CLIENT_FIRST_PREFIX_2 );

         // Write processed and escaped username
         SASLUtility.WriteString(
            credentials.Username,
            encoding,
            array,
            ref writeOffset,
            normalizer: ( name, nameIdx ) =>
            {
               // Escape the ',' and '=' characters, since they are reserved.
               switch ( name[nameIdx] )
               {
                  case ',':
                     return SCRAMCommon.COMMA_ESCAPE;
                  case '=':
                     return SCRAMCommon.EQUALS_ESCAPE;
                  default:
                     return default;
               }
            } );

         // Write ",r="
         var nonce = SCRAMCommon.UseNonceGenerator( this._algorithm, this._nonceGenerator );
         array.CurrentMaxCapacity = writeOffset + encoding.Encoding.GetByteCount( SCRAMCommon.CLIENT_NONCE_PREFIX ) + nonce.Length; // UtilPackUtility.GetBase64CharCount( nonce.Length, true );
         encoding.WriteString( array.Array, ref writeOffset, SCRAMCommon.CLIENT_NONCE_PREFIX );
         var dummy = 0;
         nonce.CopyTo( array.Array, ref dummy, writeOffset, nonce.Length );
         writeOffset += nonce.Length;

         // Remember this message (except "n,," prefix), it will be needed in next phase
         var msgLen = writeOffset - messageStart;
         this._clientMessage.CurrentMaxCapacity = msgLen + 1;
         array.Array.CopyTo( this._clientMessage.Array, ref messageStart, 0, msgLen );
         this._clientMessage.Array[msgLen] = 0; // Set terminating zero so next phase will know where the message ends

         return 0;
      }

      private Int32 PerformFinal(
         ref SASLChallengeArguments args,
         SASLCredentialsSCRAMForClient credentials,
         ref Int32 writeOffset
         )
      {
         this.PerformFinalRead(
            ref args,
            writeOffset,
            out var serverNonceReadOffset,
            out var serverNonceReadCount,
            //out var serverNonceWriteOffset,
            //out var serverNonceWriteCount,
            out var keyWriteOffset,
            out var keyWriteCount,
            out var iterationCount,
            out var seenReadCount
            );

         // Check that server nonce starts with client nonce
         // Since the client nonce is not stored as string, but as actual raw bytes, do array comparison between decoded server nonce and client nonce
         // TODO make array range equality comparisons to UtilPack (or wait for Span<T>)
         var messageOK = seenReadCount == args.ReadCount;
         var retVal = 0;
         if ( messageOK )
         {
            var clientMsg = this._clientMessage.Array;
            var prevMsgLen = clientMsg.Length;
            while ( clientMsg[prevMsgLen - 1] == 0 )
            {
               --prevMsgLen;
            }
            // Find ','
            var clientNonceStart = Array.LastIndexOf( clientMsg, SCRAMCommon.COMMA, prevMsgLen ) + args.Encoding.BytesPerASCIICharacter * 3;

            // Verify that serverNonce is clientNonce padded with random data
            var clientNonceStringLength = prevMsgLen - clientNonceStart;
            messageOK = serverNonceReadCount > clientNonceStringLength;
            if ( messageOK )
            {
               var readArray = args.ReadArray;
               for ( var i = 0; i < clientNonceStringLength && messageOK; ++i )
               {
                  if ( clientMsg[i + clientNonceStart] != readArray[i + serverNonceReadOffset] )
                  {
                     messageOK = false;
                  }
               }
            }

            if ( messageOK )
            {
               retVal = this.PerformFinalWrite(
                  ref args,
                  credentials,
                  ref writeOffset,
                  serverNonceReadOffset,
                  serverNonceReadCount,
                  keyWriteOffset,
                  keyWriteCount,
                  iterationCount,
                  prevMsgLen
                  );
            }
            else
            {
               retVal = SCRAMCommon.ERROR_SERVER_SENT_WRONG_NONCE;
            }
         }
         else
         {
            retVal = SCRAMCommon.ERROR_INVALID_RESPONSE_MESSAGE_FORMAT;
         }

         return retVal;
      }

      private Int32 PerformFinalWrite(
         ref SASLChallengeArguments args,
         SASLCredentialsSCRAMForClient credentials,
         ref Int32 writeOffset,
         Int32 serverNonceReadOffset,
         Int32 serverNonceReadCount,
         Int32 keyWriteOffset,
         Int32 keyWriteCount,
         Int32 iterationCount,
         Int32 prevMsgLen
         )
      {
         var writeArray = args.WriteArray;
         var encodingInfo = args.Encoding;
         var encoding = encodingInfo.Encoding;
         var algorithm = this._algorithm;

         var digestByteCount = algorithm.DigestByteCount;
         var blockSize = algorithm.BlockSize;
         // The server nonce and salt are in the write array
         // Expand salt (it will overwrite iteration data, but that is already parsed)
         var pw = credentials.PasswordDigest;
         String stringPW = null;
         var errorCode = !pw.IsNullOrEmpty() || !String.IsNullOrEmpty( stringPW = credentials.Password ) ?
            0 :
            SCRAMCommon.ERROR_CLIENT_SUPPLIED_WITH_INVALID_CREDENTIALS;

         if ( errorCode == 0 )
         {
            if ( pw.IsNullOrEmpty() )
            {
               var pwStart = keyWriteOffset + keyWriteCount + 4;
               writeArray.CurrentMaxCapacity = pwStart;
               writeArray.Array.WriteInt32BEToBytesNoRef( keyWriteOffset + keyWriteCount, 1 );

               // Write processed password
               var pwEnd = pwStart;
               SASLUtility.WriteString(
                  stringPW,
                  args.Encoding,
                  writeArray,
                  ref pwEnd
                  );
               // Make sure HMAC won't allocate new array
               writeArray.CurrentMaxCapacity = pwStart + blockSize;
               pw = this.WritePBKDF2(
                  algorithm,
                  writeArray.Array,
                  pwStart,
                  pwEnd - pwStart,
                  writeArray.Array,
                  keyWriteOffset,
                  keyWriteCount + 4,
                  iterationCount
                  );
               credentials.PasswordDigest = pw;
            }

            var clientKeyDigestStart = keyWriteOffset + digestByteCount;
            var hmacKeyStart = clientKeyDigestStart + digestByteCount;
            writeArray.CurrentMaxCapacity = hmacKeyStart + blockSize;
            var dummy = 0;
            pw.CopyTo( writeArray.Array, ref dummy, hmacKeyStart, digestByteCount );
            using ( var hmac = algorithm.CreateHMAC( writeArray.Array, hmacKeyStart, digestByteCount, skipDisposeAlgorithm: true ) )
            {
               // We can overwrite server salt with this digest, as the salt is no longer needed
               hmac.ComputeDigest( ClientKeyBytes, writeArray.Array, keyWriteOffset );
            }

            // Client key is now in writeArray. Write hashed key to writeArray
            algorithm.ComputeDigest( writeArray.Array, keyWriteOffset, digestByteCount, writeArray.Array, clientKeyDigestStart );

            // Hashed client key is now in writeArray.Array, starting at keyWriteOffset.
            // Compute HMAC using hashed client key as HMAC key
            // Make sure HMAC won't allocate new array
            var hmacEnd = clientKeyDigestStart + blockSize;
            writeArray.CurrentMaxCapacity = hmacEnd + digestByteCount;
            Int32 withoutProofIndex;
            Int32 withoutProofCount;
            using ( var hmac = algorithm.CreateHMAC( writeArray.Array, clientKeyDigestStart, digestByteCount, skipDisposeAlgorithm: true ) )
            {
               // We need to create HMAC of whole previous client message + "," + whole server response + "," + message without proof
               // Furthermore, we need to save this whole HMACed data for next phase processing

               // Calculate the amount of bytes needed
               var clientMsg = this._clientMessage;
               var asciiSize = encodingInfo.BytesPerASCIICharacter;
               var cbindInput = SCRAMCommon.WITHOUT_PROOF_PREFIX; // TODO add ability to do channel binds.
               clientMsg.CurrentMaxCapacity =
                  prevMsgLen // Client message length in bytes
                  + 2 * asciiSize // two commas
                  + args.ReadCount // this server response message length in bytes
                  + encoding.GetByteCount( cbindInput ) // Length of constant prefix
                  + serverNonceReadCount // Length of textual server nonce
                  + 1 // Terminating zero
                  ;
               var msg = clientMsg.Array;
               var idx = prevMsgLen;
               encodingInfo.WriteASCIIByte( msg, ref idx, SCRAMCommon.COMMA );
               dummy = args.ReadOffset;
               args.ReadArray.CopyTo( msg, ref dummy, idx, args.ReadCount );
               idx += args.ReadCount;
               encodingInfo.WriteASCIIByte( msg, ref idx, SCRAMCommon.COMMA );
               withoutProofIndex = idx;
               idx += encoding.GetBytes( cbindInput, 0, cbindInput.Length, msg, idx );
               args.ReadArray.CopyTo( msg, ref serverNonceReadOffset, idx, serverNonceReadCount );
               idx += serverNonceReadCount;
               msg.Clear( idx, msg.Length - idx ); // Terminating zero, so that next phase will find the end.
               withoutProofCount = idx - withoutProofIndex;

               // We have created and writen the message, now compute HMAC of it
               hmac.ComputeDigest( msg, 0, idx, writeArray.Array, hmacEnd );
            }

            // HMAC digest of message starting at index hmacEnd
            // Do XOR for client key
            SCRAMCommon.ArrayXor( writeArray.Array, writeArray.Array, hmacEnd, keyWriteOffset, digestByteCount );

            // Our response is: withoutproof part of this._clientMessage + ",p=" + base64 string of XORred key
            var digestStart = withoutProofCount + 3 * encodingInfo.BytesPerASCIICharacter;
            var xorStart = digestStart + StringConversions.GetBase64CharCount( digestByteCount, true );
            writeArray.CurrentMaxCapacity = xorStart + digestByteCount;
            writeArray.Array.CopyTo( writeArray.Array, ref hmacEnd, xorStart, digestByteCount );

            // Write the base64 XORred key at the end of the message first
            var digestEnd = digestStart;
            encodingInfo.WriteBinaryAsBase64ASCIICharactersWithPadding( writeArray.Array, xorStart, digestByteCount, writeArray.Array, ref digestEnd, false );

            // Now start writing the beginning of the message
            this._clientMessage.Array.CopyTo( writeArray.Array, ref withoutProofIndex, writeOffset, withoutProofCount );
            writeOffset += withoutProofCount;
            writeOffset += encoding.GetBytes( SCRAMCommon.CLIENT_PROOF_PREFIX, 0, SCRAMCommon.CLIENT_PROOF_PREFIX.Length, writeArray.Array, writeOffset );
            writeOffset += digestEnd - digestStart;
            // We're done
         }

         return errorCode;
      }

      private Byte[] WritePBKDF2(
         BlockDigestAlgorithm algorithm,
         Byte[] serverKey,
         Int32 keyOffset,
         Int32 keyLength,
         Byte[] initialHash,
         Int32 initialHashOffset,
         Int32 initialHashCount,
         Int32 iterationCount
         )
      {
         using ( var hmac = algorithm.CreateHMAC( serverKey, keyOffset, keyLength, skipDisposeAlgorithm: true ) )
         {
            // Prepare
            var cur = hmac.ComputeDigest( initialHash, initialHashOffset, initialHashCount );
            var result = new Byte[algorithm.DigestByteCount];
            cur.CopyTo( result, 0 );
            var tmp = new Byte[algorithm.DigestByteCount];

            // Perform iteration
            for ( var i = 1; i < iterationCount; ++i )
            {
               hmac.ComputeDigest( cur, tmp );
               SCRAMCommon.ArrayXor( result, tmp, tmp.Length );
               tmp.CopyTo( cur );
            }

            return result;

         }
      }



      private void PerformFinalRead(
         ref SASLChallengeArguments args,
         Int32 writeOffset,
         out Int32 serverNonceReadOffset,
         out Int32 serverNonceReadCount,
         //out Int32 serverNonceWriteOffset,
         //out Int32 serverNonceWriteCount,
         out Int32 keyWriteOffset,
         out Int32 keyWriteCount,
         out Int32 iterationCount,
         out Int32 seenReadCount
         )
      {
         var array = args.ReadArray;
         var offset = args.ReadOffset;
         var encoding = args.Encoding;
         var writeArray = args.WriteArray;

         // Message starts with "r="
         encoding.VerifyASCIIBytes( array, ref offset, SCRAMCommon.NONCE_PREFIX );

         // Now follows server nonce, but skip it, since it does not have '=' characters between client nonce string and the rest
         serverNonceReadOffset = offset;
         serverNonceReadCount = encoding.IndexOfASCIICharacter( array, offset, args.ReadOffset + args.ReadCount - offset, SCRAMCommon.COMMA ) - offset;
         offset += serverNonceReadCount;
         //serverNonceWriteOffset = writeOffset;
         //encoding.ReadASCIICharactersAsBinaryTrimEnd(
         //   array,
         //   offset,
         //   serverNonceReadCount,
         //   6,
         //   BASE_64_DECODE,
         //   writeArray,
         //   ref writeOffset
         //   );
         //serverNonceWriteCount = writeOffset - serverNonceWriteOffset;

         // Then "s="
         encoding.VerifyASCIIBytes( array, ref offset, SCRAMCommon.SALT_PREFIX );

         // Now follows salt
         keyWriteOffset = writeOffset;
         var keyReadCount = encoding.IndexOfASCIICharacter( array, offset, args.ReadOffset + args.ReadCount - offset, SCRAMCommon.COMMA ) - offset;
         writeArray.CurrentMaxCapacity = writeOffset + StringConversions.GetBase64CharBinaryCount( keyReadCount / encoding.MinCharByteCount );
         encoding.ReadBase64PaddedASCIICharactersAsBinary(
            array,
            offset,
            keyReadCount,
            writeArray.Array,
            ref writeOffset,
            false
            );
         keyWriteCount = writeOffset - keyWriteOffset;
         offset += keyReadCount;

         // Then "i="
         encoding.VerifyASCIIBytes( array, ref offset, SCRAMCommon.ITERATION_PREFIX );

         // Then a number
         var start = args.ReadOffset;
         iterationCount = encoding.ParseInt32Textual( array, ref offset, (( start + args.ReadCount - offset ) / encoding.BytesPerASCIICharacter, true) );
         seenReadCount = offset - start;
      }

      private Int32 PerformValidate(
         ref SASLChallengeArguments args,
         SASLCredentialsSCRAMForClient credentials
         )
      {
         var encodingInfo = args.Encoding;
         var readArray = args.ReadArray;
         var readOffset = args.ReadOffset;
         var algorithm = this._algorithm;
         var digestByteCount = algorithm.DigestByteCount;

         encodingInfo.VerifyASCIIBytes( readArray, ref readOffset, SCRAMCommon.VALIDATE_PREFIX );
         var writeIndex = args.WriteOffset;
         var serverProofReadSize = args.ReadOffset + args.ReadCount - readOffset;
         var messageOK = serverProofReadSize == StringConversions.GetBase64CharCount( digestByteCount, true ) * encodingInfo.BytesPerASCIICharacter;
         if ( messageOK )
         {
            // Using PBKDF2 result as HMAC key, calculate digest of "Server Key" ASCII bytes
            var writeArray = args.WriteArray;
            var computedStart = algorithm.BlockSize;
            var writeArrayReservedCount = computedStart + digestByteCount;
            writeArray.CurrentMaxCapacity = writeArrayReservedCount;
            using ( var hmac = algorithm.CreateHMAC( credentials.PasswordDigest, 0, algorithm.DigestByteCount, skipDisposeAlgorithm: true, skipZeroingOutKey: true ) )
            {
               hmac.ComputeDigest( ServerKeyBytes, writeArray.Array );
            }

            // Using the computed digest as HMAC key, calculate digest for the concatenation of messages, stored to this._clientMessage in previous phase
            var clientMsg = this._clientMessage;
            var prevMsgLen = clientMsg.CurrentMaxCapacity;
            while ( clientMsg.Array[prevMsgLen - 1] == 0 )
            {
               --prevMsgLen;
            }
            using ( var hmac = algorithm.CreateHMAC( writeArray.Array, 0, digestByteCount, skipDisposeAlgorithm: true ) )
            {
               hmac.ComputeDigest( clientMsg.Array, 0, prevMsgLen, writeArray.Array, computedStart );
            }

            // The digest is now in writeArray.Array starting at index digestByteCount
            // We must now verify that that digest equals to the one sent by the server.
            // We can either:
            // 1. Decode-base64 server digest to writeArray and verify that array sections equal, or
            // 2. Encode-base64 computed digest to writeArray and verify that textual data in readArray and writeArray equal.
            // We choose #1 as it uses less memory, and we can detect invalid server tokens better.
            writeArray.CurrentMaxCapacity = writeIndex + digestByteCount;
            encodingInfo.ReadBase64PaddedASCIICharactersAsBinary(
               readArray,
               readOffset,
               serverProofReadSize,
               writeArray.Array,
               ref writeIndex,
               false
               );
            // Verify that writeArray.Array[digestByteCount .. writeArrayReservedCount] segment equals to writeArray.Array[writeArrayReservedCount..writeArrayReservedCount+digestByteCount]segment
            messageOK = writeIndex - args.WriteOffset == digestByteCount;
            if ( messageOK )
            {
               var array = writeArray.Array;
               for ( var i = 0; i < digestByteCount && messageOK; ++i )
               {
                  if ( array[i + computedStart] != array[i] )
                  {
                     messageOK = false;
                  }
               }
            }
         }

         return messageOK ? 0 : SCRAMCommon.ERROR_SERVER_SENT_WRONG_PROOF;
      }

   }

   /// <summary>
   /// This class contains utility methods and extension methods for types defined in other assemblies than this.
   /// </summary>
   public static partial class UtilPackUtility
   {
      /// <summary>
      /// Creates a new instance of <see cref="SASLMechanism"/> that implements client-side SCRAM mechanism with this <see cref="BlockDigestAlgorithm"/>.
      /// </summary>
      /// <param name="algorithm">This <see cref="BlockDigestAlgorithm"/>, that will be used by SCRAM mechanism as its digest provider.</param>
      /// <param name="nonceGenerator">The optional custom callback to provide client nonce. Please read remarks if supplying value.</param>
      /// <returns>A new <see cref="SASLMechanism"/> which will behave as client-side when authenticating with SCRAM.</returns>
      /// <remarks>
      /// <para>
      /// The returned <see cref="SASLMechanism"/> will expect the <see cref="SASLChallengeArguments.Credentials"/> field to always be non-null and of type <see cref="SASLCredentialsSCRAMForClient"/>.
      /// </para>
      /// <para>
      /// The <paramref name="nonceGenerator"/>, if supplied, *must* return valid nonce (e.g. not containing any commas, printable ASCII characters, etc).
      /// The returned nonce will be directly written to the message - it is not base64-encoded!
      /// </para>
      /// </remarks>
      /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
      /// <seealso cref="SASLCredentialsSCRAMForClient"/>
      /// <seealso cref="SCRAMCommon"/>
      public static SASLMechanism CreateSASLClientSCRAM( this BlockDigestAlgorithm algorithm, Func<Byte[]> nonceGenerator = null )
      {
         return new SASLMechanismSCRAMForClient( ArgumentValidator.ValidateNotNullReference( algorithm ), nonceGenerator );
      }
   }

}

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// Creates new <see cref="SASLChallengeArguments"/> from this client-side <see cref="SASLCredentialsSCRAMForClient"/>.
   /// </summary>
   /// <param name="credentials">This <see cref="SASLCredentialsSCRAMForClient"/>.</param>
   /// <param name="readArray">The array containing remote response.</param>
   /// <param name="readOffset">The offset in <paramref name="readArray"/> where remote response starts.</param>
   /// <param name="readCount">The amount of bytes in <paramref name="readArray"/> that remote response takes.</param>
   /// <param name="writeArray">The <see cref="ResizableArray{T}"/> where to write this response.</param>
   /// <param name="writeOffset">The offset in <paramref name="writeArray"/> where to start writing.</param>
   /// <param name="encoding">The <see cref="IEncodingInfo"/> to use for textual data.</param>
   /// <returns>A new instance of <see cref="SASLChallengeArguments"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="SASLCredentialsSCRAMForClient"/> is <c>null</c>.</exception>
   public static SASLChallengeArguments CreateChallengeArguments(
      this SASLCredentialsSCRAMForClient credentials,
      Byte[] readArray,
      Int32 readOffset,
      Int32 readCount,
      ResizableArray<Byte> writeArray,
      Int32 writeOffset,
      IEncodingInfo encoding
      )
   {
      return new SASLChallengeArguments( readArray, readOffset, readCount, writeArray, writeOffset, encoding, ArgumentValidator.ValidateNotNullReference( credentials ) );
   }
}

internal static partial class E_TODO
{
   // TODO not sure what to do with these methods. The pipeline idea seems more smart instead of just throwing an exception right away.
   // TODO some kind of pipeline API which accepts sequence of callbacks, and instead of throwing, will just skip calling subsequent callbacks
   // public struct TryBlock { ... } ?
   // PipeLineDefinition { PipeLineInstance CreatePipeLine(SomeKindOfContext context); } ?

   public static Boolean TryVerifyASCIIBytesNoRef( this IEncodingInfo encoding, Byte[] array, Int32 offset, String required )
      => encoding.TryVerifyASCIIBytes( array, ref offset, required );

   public static Boolean TryVerifyASCIIBytes( this IEncodingInfo encoding, Byte[] array, ref Int32 offset, String required )
   {
      var retVal = true;
      for ( var i = 0; i < required.Length && retVal; ++i )
      {
         if ( required[i] != encoding.ReadASCIIByte( array, ref offset ) )
         {
            retVal = false;
         }
      }

      return retVal;
   }

   public static void VerifyASCIIBytes( this IEncodingInfo encoding, Byte[] array, ref Int32 offset, String required )
   {
      if ( !encoding.TryVerifyASCIIBytes( array, ref offset, required ) )
      {
         throw new FormatException( "Illegal character in input." );
      }
   }

}