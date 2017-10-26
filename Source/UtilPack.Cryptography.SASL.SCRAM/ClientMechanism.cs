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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UtilPack;
using UtilPack.Cryptography.Digest;
using UtilPack.Cryptography.SASL;
using UtilPack.Cryptography.SASL.SCRAM;

using TSyncChallengeResult = UtilPack.EitherOr<System.ValueTuple<System.Int32, UtilPack.Cryptography.SASL.SASLChallengeResult>, System.Int32>;


namespace UtilPack.Cryptography.SASL.SCRAM
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
         ref SASLAuthenticationArguments args,
         SASLCredentialsSCRAMForClient credentials
         )
      {
         var prevState = this._state;
         Int32 nextState;
         var writeOffset = args.WriteOffset;
         var challengeResult = SASLChallengeResult.MoreToCome;
         Int32 errorCode;
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

         Int32 bytesWritten;
         if ( errorCode < 0
            || nextState < 0
            || Interlocked.CompareExchange( ref this._state, nextState, prevState ) != prevState )
         {
            bytesWritten = MakeSureIsNegative( errorCode );
         }
         else
         {
            bytesWritten = writeOffset - args.WriteOffset;
         }

         return bytesWritten < 0 ? new TSyncChallengeResult( bytesWritten ) : new TSyncChallengeResult( (bytesWritten, challengeResult) );
      }

      protected override Int32 GetExceptionErrorCode( Exception exc )
         => SCRAMCommon.ERROR_INVALID_FORMAT;

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
         ref SASLAuthenticationArguments args,
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
         ref SASLAuthenticationArguments args,
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
            retVal = SCRAMCommon.ERROR_INVALID_FORMAT;
         }

         return retVal;
      }

      private Int32 PerformFinalWrite(
         ref SASLAuthenticationArguments args,
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
               pw = WritePBKDF2(
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
            var xorStart = digestStart + UtilPackUtility.GetBase64CharCount( digestByteCount, true );
            writeArray.CurrentMaxCapacity = xorStart + digestByteCount;
            writeArray.Array.CopyTo( writeArray.Array, ref hmacEnd, xorStart, digestByteCount );

            // Write the base64 XORred key at the end of the message first
            var digestEnd = digestStart;
            encodingInfo.WriteBinaryAsBase64ASCIICharactersTrimEnd( writeArray.Array, xorStart, digestByteCount, writeArray.Array, ref digestEnd, false );

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
         ref SASLAuthenticationArguments args,
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
         var count = args.ReadCount;
         var encoding = args.Encoding;
         var writeArray = args.WriteArray;

         // Message starts with "r="
         encoding.VerifyASCIIBytes( array, ref offset, SCRAMCommon.NONCE_PREFIX );

         // Now follows server nonce, but skip it, since it does not have '=' characters between client nonce string and the rest
         serverNonceReadOffset = offset;
         serverNonceReadCount = Array.IndexOf( array, SCRAMCommon.COMMA, offset ) - offset;
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
         var keyReadCount = Array.IndexOf( array, SCRAMCommon.COMMA, offset ) - offset;
         encoding.ReadBase64ASCIICharactersAsBinaryTrimEnd(
            array,
            offset,
            keyReadCount,
            writeArray,
            ref writeOffset,
            false
            );
         keyWriteCount = writeOffset - keyWriteOffset;
         offset += keyReadCount;

         // Then "i="
         encoding.VerifyASCIIBytes( array, ref offset, SCRAMCommon.ITERATION_PREFIX );

         // Then a number
         iterationCount = encoding.ParseInt32Textual( array, ref offset, (( count - offset ) / encoding.BytesPerASCIICharacter, true) );
         seenReadCount = offset - args.ReadOffset;
      }

      private Int32 PerformValidate(
         ref SASLAuthenticationArguments args,
         SASLCredentialsSCRAMForClient credentials
         )
      {
         var encodingInfo = args.Encoding;
         var readArray = args.ReadArray;
         var readOffset = args.ReadOffset;

         encodingInfo.VerifyASCIIBytes( readArray, ref readOffset, SCRAMCommon.VALIDATE_PREFIX );
         // Using PBKDF2 result as HMAC key, calculate digest of "Server Key" ASCII bytes
         var algorithm = this._algorithm;
         var digestByteCount = algorithm.DigestByteCount;
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
         var writeIndex = 0;
         encodingInfo.ReadBase64ASCIICharactersAsBinaryTrimEnd(
            readArray,
            readOffset,
            args.ReadCount - readOffset,
            writeArray,
            ref writeIndex,
            false
            );
         // Verify that writeArray.Array[digestByteCount .. writeArrayReservedCount] segment equals to writeArray.Array[writeArrayReservedCount..writeArrayReservedCount+digestByteCount]segment
         var messageOK = writeIndex == digestByteCount;
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

         return messageOK ? 0 : SCRAMCommon.ERROR_SERVER_SENT_WRONG_PROOF;
      }

   }

   internal static partial class UtilPackUtility
   {
      public static Int32 GetBase64CharCount( Int32 byteCount, Boolean pad )
      {
         var raw = BinaryUtils.AmountOfPagesTaken( byteCount * E_TODO.BYTE_SIZE, 6 );
         if ( pad )
         {
            while ( raw % 4 != 0 )
            {
               ++raw;
            }
         }

         return raw;
      }
   }
}

public static partial class E_UtilPack
{
   public static SASLMechanism CreateSASLClientSCRAM( this BlockDigestAlgorithm algorithm, Func<Byte[]> clientNonceGenerator = null )
   {
      return new SASLMechanismSCRAMForClient( ArgumentValidator.ValidateNotNullReference( algorithm ), clientNonceGenerator );
   }
}

internal static partial class E_TODO
{

   // TODO move these to UtilPack
   internal const Int32 BYTE_SIZE = 8;
   private const Int32 ALL_ONES = Byte.MaxValue;
   private const Byte BASE64_PADDING = (Byte) '=';

   public static void WriteBinaryAsBase64ASCIICharactersTrimEnd(
      this IEncodingInfo encoding,
      Byte[] sourceArray,
      Byte[] targetArray,
      ref Int32 targetArrayIndex,
      Boolean isURLSafe
      )
      => encoding.WriteBinaryAsASCIICharactersTrimEnd(
         sourceArray,
         0,
         sourceArray.Length,
         StringConversions.CreateBase64EncodeLookupTable( isURLSafe ),
         targetArray,
         ref targetArrayIndex,
         BASE64_PADDING
         );

   public static void WriteBinaryAsBase64ASCIICharactersTrimEnd(
      this IEncodingInfo encoding,
      Byte[] sourceArray,
      Int32 sourceOffset,
      Int32 sourceCount,
      Byte[] targetArray,
      ref Int32 targetArrayIndex,
      Boolean isURLSafe
      )
   => encoding.WriteBinaryAsASCIICharactersTrimEnd(
      sourceArray,
      sourceOffset,
      sourceCount,
      StringConversions.CreateBase64EncodeLookupTable( isURLSafe ),
      targetArray,
      ref targetArrayIndex,
      BASE64_PADDING
      );

   public static void WriteBinaryAsASCIICharacters(
      this IEncodingInfo encoding,
      Byte[] sourceArray,
      Char[] lookupArray,
      Byte[] targetArray,
      ref Int32 targetArrayIndex
      )
      => encoding.WriteBinaryAsASCIICharacters( sourceArray, 0, sourceArray?.Length ?? 0, lookupArray, targetArray, ref targetArrayIndex, out var dummy );

   public static Int32 WriteBinaryAsASCIICharactersTrimEnd(
      this IEncodingInfo encoding,
      Byte[] sourceArray,
      Char[] lookupArray,
      Byte[] targetArray,
      ref Int32 targetArrayIndex,
      Byte padding
      )
      => encoding.WriteBinaryAsASCIICharactersTrimEnd( sourceArray, 0, sourceArray.Length, lookupArray, targetArray, ref targetArrayIndex, padding );

   public static Int32 WriteBinaryAsASCIICharactersTrimEnd(
     this IEncodingInfo encoding,
     Byte[] sourceArray,
     Int32 sourceOffset,
     Int32 sourceCount,
     Char[] lookupArray,
     Byte[] targetArray,
     ref Int32 targetArrayIndex,
     Byte padding
     )
   {
      var charCount = encoding.WriteBinaryAsASCIICharacters( sourceArray, sourceOffset, sourceCount, lookupArray, targetArray, ref targetArrayIndex, out var unitSize );

      Int32 charChunkSize;
      switch ( unitSize )
      {
         case 6:
            charChunkSize = 4;
            break;
         case 3:
         case 5:
         case 7:
            charChunkSize = 8;
            break;
         default:
            charChunkSize = -1;
            break;
      }

      if ( charChunkSize > 0 )
      {
         while ( charCount % charChunkSize != 0 )
         {
            encoding.WriteASCIIByte( targetArray, ref targetArrayIndex, padding );
            ++charCount;
         }
      }

      return charCount;
   }



   public static Int32 WriteBinaryAsASCIICharacters(
      this IEncodingInfo encoding,
      Byte[] sourceArray,
      Int32 sourceOffset,
      Int32 sourceCount,
      Char[] lookupArray,
      Byte[] targetArray,
      ref Int32 targetArrayIndex,
      out Int32 unitSize
      )
   {
      sourceArray.CheckArrayArguments( sourceOffset, sourceCount, false );
      ArgumentValidator.ValidateNotNull( nameof( lookupArray ), lookupArray );
      ArgumentValidator.ValidateNotNull( nameof( targetArray ), targetArray );
      Int32 charCount;
      if ( sourceCount > 0 )
      {
         unitSize = CheckUnitSize( lookupArray.Length );

         // Compute amount of characters needed
         charCount = BinaryUtils.AmountOfPagesTaken( sourceCount * BYTE_SIZE, unitSize );

         var bit = sourceOffset * BYTE_SIZE;
         var max = sourceOffset + sourceCount;
         for ( var i = 0; i < charCount; ++i, bit += unitSize )
         {
            var startByte = bit / BYTE_SIZE;
            var endByte = ( bit + unitSize - 1 ) / BYTE_SIZE;
            // How many MSB's to skip
            var msbSkip = bit % BYTE_SIZE;
            Int32 idx;
            if ( startByte == endByte )
            {
               // Skip msbSkip MSB, extract next unitSize bits, and skip final 8 - unitsize - msbSkip bits
               var lsbSkip = BYTE_SIZE - unitSize - msbSkip;
               var mask = ( ALL_ONES >> msbSkip ) & ( ALL_ONES << lsbSkip );
               idx = ( sourceArray[startByte] & mask ) >> lsbSkip;
            }
            else
            {
               // For first byte: skip msbSkip MSB, extract final bits
               // For next byte: extract unitsize - previous byte final bits size MSB
               var firstMask = ALL_ONES >> msbSkip;
               // Amount of bits in second byte
               var secondByteBits = ( unitSize + msbSkip - BYTE_SIZE );
               idx = ( ( sourceArray[startByte] & firstMask ) << secondByteBits );
               if ( endByte < max )
               {
                  var secondMask = ( ALL_ONES << ( BYTE_SIZE - secondByteBits ) ) & ALL_ONES;
                  idx |= ( ( sourceArray[endByte] & secondMask ) >> ( BYTE_SIZE - secondByteBits ) );
               }
            }

            encoding.WriteASCIIByte( targetArray, ref targetArrayIndex, (Byte) lookupArray[idx] );
         }

      }
      else
      {
         charCount = 0;
         unitSize = -1;
      }

      return charCount;

   }

   public static Int32 ReadBase64ASCIICharactersAsBinaryTrimEnd(
      this IEncodingInfo encoding,
      Byte[] sourceArray,
      Int32 sourceOffset,
      Int32 sourceCount,
      ResizableArray<Byte> targetArray,
      ref Int32 targetArrayIndex,
      Boolean isURLSafe
      )
      => encoding.ReadASCIICharactersAsBinaryTrimEnd( sourceArray, sourceOffset, sourceCount, 6, StringConversions.CreateBase64DecodeLookupTable( isURLSafe ), targetArray, ref targetArrayIndex, BASE64_PADDING );

   public static Int32 ReadASCIICharactersAsBinaryTrimEnd(
      this IEncodingInfo encoding,
      Byte[] sourceArray,
      Int32 sourceOffset,
      Int32 sourceCount,
      Int32 unitSize, // To make sense using this in this method, this must be 3,5,6, or 7
      Int32[] lookupTable,
      ResizableArray<Byte> targetArray,
      ref Int32 targetArrayIndex,
      Byte padding
      )
   {
      var delta = encoding.BytesPerASCIICharacter;
      var idx = sourceOffset + sourceCount - delta;
      Byte asciiByte;
      while ( idx >= sourceOffset && idx >= 0 && lookupTable[asciiByte = encoding.ReadASCIIByte( sourceArray, ref idx )] < 0 )
      {
         if ( asciiByte != padding )
         {
            throw new FormatException( $"Invalid base{1 << unitSize} string" );
         }

         idx -= delta * 2;
      }

      sourceCount = idx - sourceOffset;
      encoding.ReadASCIICharactersAsBinary( sourceArray, sourceOffset, sourceCount, unitSize, lookupTable, targetArray, ref targetArrayIndex );
      return sourceCount;
   }

   public static void ReadASCIICharactersAsBinary(
      this IEncodingInfo encoding,
      Byte[] sourceArray,
      Int32 sourceOffset,
      Int32 sourceCount,
      Int32 unitSize,
      Int32[] lookupTable,
      ResizableArray<Byte> targetArray,
      ref Int32 targetArrayIndex
      )
   {
      sourceArray.CheckArrayArguments( sourceOffset, sourceCount, false );
      ArgumentValidator.ValidateNotNull( "Lookup table", lookupTable );

      var sMax = targetArrayIndex;
      targetArrayIndex += ( sourceCount * unitSize / BYTE_SIZE ); //  BinaryUtils.AmountOfPagesTaken( sourceCount * unitSize, BYTE_SIZE );
      targetArray.CurrentMaxCapacity = targetArrayIndex;
      var targetBuffer = targetArray.Array;
      // Clear target buffer because it will mess up things otherwise
      targetBuffer.Clear( sMax, targetArrayIndex - sMax );

      var bit = sMax * BYTE_SIZE;
      sMax = sourceOffset + sourceCount;
      for ( ; sourceOffset < sMax; bit += unitSize )
      {
         var c = encoding.ReadASCIIByte( sourceArray, ref sourceOffset );
         var value = lookupTable[c];
         if ( value == -1 )
         {
            throw new InvalidOperationException( "Character \"" + (Char) c + "\" not found from lookup table." );
         }
         else
         {
            var startByte = bit / BYTE_SIZE;
            var endByte = ( bit + unitSize - 1 ) / BYTE_SIZE;
            // How many MSB's to skip
            var skip = bit % BYTE_SIZE;
            if ( startByte == endByte )
            {
               // Keep the MSB, shift value to left (in case e.g. unit size is 3 and MSB is 1) 
               targetBuffer[startByte] = unchecked((Byte) ( targetBuffer[startByte] | ( value << ( BYTE_SIZE - unitSize - skip ) ) ));
            }
            else
            {
               // Keep the existing MSB, and extract the MSB from the value
               // Amount of bits to use up in second byte
               skip = unitSize + skip - BYTE_SIZE;
               // Extract the MSB from value
               targetBuffer[startByte] = unchecked((Byte) ( targetBuffer[startByte] | ( value >> skip ) ));

               if ( endByte < targetArrayIndex )
               {
                  // Extract the LSB from the value
                  targetBuffer[endByte] = unchecked((Byte) ( value << ( BYTE_SIZE - skip ) ));
               }
            }
         }
      }
   }

   private static Int32 CheckUnitSize( Int32 lookupTableSize )
   {
      lookupTableSize = BinaryUtils.Log2( (UInt32) lookupTableSize );
      if ( lookupTableSize < 1 )
      {
         throw new ArgumentException( "Unit size must be at least one." );
      }
      else if ( lookupTableSize > BYTE_SIZE )
      {
         lookupTableSize = BYTE_SIZE; // throw new ArgumentException( "Unit size must be at most " + BYTE_SIZE + "." );
      }
      return lookupTableSize;
   }

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
      // TODO some kind of pipeline API which accepts sequence of callbacks, and instead of throwing, will just skip calling subsequent callbacks
      // public struct TryBlock { ... } ?
      if ( !encoding.TryVerifyASCIIBytes( array, ref offset, required ) )
      {
         throw new FormatException( "Illegal character in input." );
      }
   }

}