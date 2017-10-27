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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.Cryptography.Digest;
using UtilPack.Cryptography.SASL;
using UtilPack.Cryptography.SASL.SCRAM;

using TSyncChallengeResult = UtilPack.EitherOr<System.ValueTuple<System.Int32, UtilPack.Cryptography.SASL.SASLChallengeResult>, System.Int32>;

namespace UtilPack.Cryptography.SASL.SCRAM
{
   internal sealed class SASLMechanismSCRAMForServer : AbstractServerSASLMechanism<SASLCredentialsSCRAMForServer>
   {
      private const Int32 STATE_INITIAL = 0;
      private const Int32 STATE_VALIDATE = 1;
      private const Int32 STATE_COMPLETE = 2;

      private readonly BlockDigestAlgorithm _algorithm;
      private readonly ResizableArray<Byte> _auxArray;
      private readonly Func<String, ValueTask<SASLCredentialsSCRAMForServer>> _getCredentials;
      private readonly Func<Byte[]> _nonceGenerator;

      private Int32 _state;
      private (Int32, Int32) _fullNonceInfo;
      private Int32 _currentAuxCount;


      public SASLMechanismSCRAMForServer(
         BlockDigestAlgorithm algorithm,
         Func<String, ValueTask<SASLCredentialsSCRAMForServer>> getCredentials,
         Func<Byte[]> nonceGenerator
         )
      {
         this._algorithm = ArgumentValidator.ValidateNotNull( nameof( algorithm ), algorithm );
         this._getCredentials = ArgumentValidator.ValidateNotNull( nameof( getCredentials ), getCredentials );

         this._nonceGenerator = nonceGenerator;
         this._auxArray = new ResizableArray<Byte>();
      }

      protected override async ValueTask<TSyncChallengeResult> ChallengeServer(
         SASLAuthenticationArguments args,
         SASLCredentialsHolder credentialsHolder,
         SASLCredentialsSCRAMForServer credentials
         )
      {
         var prevState = this._state;

         Int32 nextState;
         var writeOffset = args.WriteOffset;
         var challengeResult = SASLChallengeResult.MoreToCome;
         Int32 errorCode;
         switch ( this._state )
         {
            case STATE_INITIAL:
               (credentials, writeOffset, errorCode) = await this.PerformInitial( args );
               credentialsHolder.Credentials = credentials;
               nextState = STATE_VALIDATE;
               break;
            case STATE_VALIDATE:
               errorCode = this.PerformValidate( ref args, credentials, ref writeOffset );
               challengeResult = SASLChallengeResult.Completed;
               nextState = STATE_COMPLETE;
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

      public override void Reset()
      {
         this._algorithm.Reset();
         this.ResetThis();
      }

      protected override void Dispose( Boolean disposing )
      {
         if ( disposing )
         {
            this._algorithm.DisposeSafely();
            this.ResetThis();
         }
      }

      private void ResetThis()
      {
         this._auxArray.Array.Clear();
         this._fullNonceInfo = (0, 0);
         this._currentAuxCount = 0;
         Interlocked.Exchange( ref this._state, STATE_INITIAL );
      }

      private async ValueTask<(SASLCredentialsSCRAMForServer, Int32, Int32)> PerformInitial(
         SASLAuthenticationArguments args
         )
      {
         var errorCode = this.PerformInitialRead(
            ref args,
            out var username,
            out var clientNonceReadOffset,
            out var clientNonceReadCount,
            out var messageReadOffset
            );

         SASLCredentialsSCRAMForServer credentials;
         Int32 bytesWritten = -1;
         if ( errorCode == 0 )
         {
            var readMessageCount = args.ReadOffset + args.ReadCount - messageReadOffset;
            this._auxArray.CurrentMaxCapacity = readMessageCount;
            args.ReadArray.CopyTo( this._auxArray.Array, ref messageReadOffset, 0, readMessageCount );

            credentials = await this._getCredentials( username );
            if ( credentials == null )
            {
               errorCode = SCRAMCommon.ERROR_CLIENT_SENT_WRONG_CREDENTIALS;
            }
            else
            {
               var writeEnd = args.WriteOffset;
               this.PerformInitialWrite(
                  ref args,
                  ref credentials,
                  ref writeEnd,
                  clientNonceReadOffset,
                  clientNonceReadCount,
                  out var fullNonceWriteOffset,
                  out var fullNonceWriteCount
                  );
               bytesWritten = writeEnd - args.WriteOffset;
               if ( bytesWritten > 0 )
               {
                  // Save ',' + our response
                  var encoding = args.Encoding;
                  var writeStart = args.WriteOffset;
                  var writeCount = writeEnd - writeStart;
                  this._auxArray.CurrentMaxCapacity = readMessageCount + 2 * encoding.BytesPerASCIICharacter + writeCount;
                  encoding.WriteASCIIByte( this._auxArray.Array, ref readMessageCount, SCRAMCommon.COMMA );
                  this._fullNonceInfo = (readMessageCount + fullNonceWriteOffset, fullNonceWriteCount);
                  args.WriteArray.Array.CopyTo( this._auxArray.Array, ref writeStart, readMessageCount, writeCount );
                  readMessageCount += writeCount;
                  encoding.WriteASCIIByte( this._auxArray.Array, ref readMessageCount, SCRAMCommon.COMMA );
                  this._currentAuxCount = readMessageCount;
               }
               else
               {
                  errorCode = SCRAMCommon.ERROR_INVALID_FORMAT;
               }
            }
         }
         else
         {
            credentials = default;
            bytesWritten = -1;
         }

         return (credentials, bytesWritten, errorCode);

      }

      private Int32 PerformInitialRead(
         ref SASLAuthenticationArguments args,
         out String username,
         out Int32 clientNonceReadOffset,
         out Int32 clientNonceReadCount,
         out Int32 messageReadOffset
         )
      {
         // Read client nonce, and save whole message (apart from gs2-cbind-flag)
         var encoding = args.Encoding;
         var readArray = args.ReadArray;
         var readOffset = args.ReadOffset;
         var readMax = readOffset + args.ReadCount;
         encoding.VerifyASCIIBytes( readArray, ref readOffset, SCRAMCommon.CLIENT_FIRST_PREFIX_1 );
         messageReadOffset = readOffset;
         encoding.VerifyASCIIBytes( readArray, ref readOffset, SCRAMCommon.CLIENT_FIRST_PREFIX_2 );

         // Username
         username = SASLUtility.ReadString( encoding, readArray, ref readOffset, readMax - readOffset, SCRAMCommon.COMMA, Denormalize );

         // Nonce prefix
         encoding.VerifyASCIIBytes( readArray, ref readOffset, SCRAMCommon.CLIENT_NONCE_PREFIX );

         // Actual nonce (this will throw on invalid chars)
         clientNonceReadOffset = readOffset;
         var min = encoding.MinCharByteCount;
         clientNonceReadCount = readMax - clientNonceReadOffset;

         // Verify that all the rest of the characters are ASCII
         return min > 1 && ( readMax - readOffset ) % min != 0 ?
            SCRAMCommon.ERROR_INVALID_FORMAT :
            0;
      }

      private static String Denormalize( IEncodingInfo encoding, Byte[] array, Int32 index )
      {
         return encoding.TryVerifyASCIIBytesNoRef( array, index, SCRAMCommon.COMMA_ESCAPE ) ?
            "," :
            ( encoding.TryVerifyASCIIBytesNoRef( array, index, SCRAMCommon.EQUALS_ESCAPE ) ? "=" : null );
      }

      private void PerformInitialWrite(
         ref SASLAuthenticationArguments args,
         ref SASLCredentialsSCRAMForServer credentials,
         ref Int32 writeOffset,
         Int32 clientNonceReadOffset,
         Int32 clientNonceReadCount,
         out Int32 fullNonceWriteOffset,
         out Int32 fullNonceWriteCount
         )
      {
         // Write client nonce + server nonce + salt + iteration count, and save whole message
         var encodingInfo = args.Encoding;
         var nonce = SCRAMCommon.UseNonceGenerator( this._algorithm, this._nonceGenerator );
         var salt = credentials.Salt;
         var iterations = credentials.IterationCount;

         args.WriteArray.CurrentMaxCapacity = writeOffset
            + ( SCRAMCommon.NONCE_PREFIX.Length + SCRAMCommon.SALT_PREFIX.Length + SCRAMCommon.ITERATION_PREFIX.Length + nonce.Length ) * encodingInfo.BytesPerASCIICharacter // All constant strings + our nonce length (which will be just random characters instead of base64)
            + clientNonceReadCount // client nonce
            + StringConversions.GetBase64CharCount( salt.Length, true )
            + encodingInfo.GetTextualIntegerRepresentationSize( iterations );

         // Now we can write whole message
         var array = args.WriteArray.Array;
         var start = writeOffset;
         encodingInfo.WriteString( array, ref writeOffset, SCRAMCommon.NONCE_PREFIX );
         fullNonceWriteOffset = writeOffset;
         args.ReadArray.CopyTo( array, ref clientNonceReadOffset, writeOffset, clientNonceReadCount );
         writeOffset += clientNonceReadCount;
         clientNonceReadOffset = 0;
         nonce.CopyTo( array, ref clientNonceReadOffset, writeOffset, nonce.Length );
         writeOffset += nonce.Length;
         fullNonceWriteCount = writeOffset - fullNonceWriteOffset;

         encodingInfo.WriteString( array, ref writeOffset, SCRAMCommon.SALT_PREFIX );
         encodingInfo.WriteBinaryAsBase64ASCIICharactersWithPadding( salt, 0, salt.Length, array, ref writeOffset, false );
         encodingInfo.WriteString( array, ref writeOffset, SCRAMCommon.ITERATION_PREFIX );
         encodingInfo.WriteIntegerTextual( array, ref writeOffset, iterations );

         // We're done
      }

      private Int32 PerformValidate(
         ref SASLAuthenticationArguments args,
         SASLCredentialsSCRAMForServer credentials,
         ref Int32 writeOffset
         )
      {
         var errorCode = this.PerformValidateRead(
            ref args,
            out var beforeProofReadCount,
            out var proofReadOffset,
            out var proofReadCount
            );

         var algorithm = this._algorithm;
         var digestByteCount = algorithm.DigestByteCount;
         var encodingInfo = args.Encoding;
         if ( errorCode == 0 )
         {
            // We haven't read proof yet from read array, but let's start the validation

            // Calculate h = HMAC(H(key_c), auth)
            // auth is what we have in aux array: "<client-first>,<server-first>,"
            // And what we have in read array: "client-final-without-proof"
            // Allocate room for hmac key + digest
            var curAux = this._currentAuxCount;
            var digestStart = curAux + algorithm.BlockSize;
            this._auxArray.CurrentMaxCapacity = digestStart + digestByteCount;
            var auxArray = this._auxArray.Array;
            var dummy = 0;
            credentials.ClientKeyDigest.CopyTo( auxArray, ref dummy, curAux, digestByteCount );
            using ( var hmac = algorithm.CreateHMAC( auxArray, curAux, algorithm.BlockSize, skipDisposeAlgorithm: true ) )
            {
               hmac.ProcessBlock( auxArray, 0, curAux );
               hmac.ProcessBlock( args.ReadArray, args.ReadOffset, beforeProofReadCount );
               hmac.WriteDigest( auxArray, digestStart );
            }

            // Calculate h_xor = XOR(h, proof from client message) (write h_xor to writeArray) (PerformValidateRead made sure that base64 string is of correct size)
            var writeArray = args.WriteArray;
            writeArray.CurrentMaxCapacity = writeOffset + 2 * digestByteCount;
            var wArray = writeArray.Array;
            var writeStart = writeOffset;
            encodingInfo.ReadBase64PaddedASCIICharactersAsBinary( args.ReadArray, proofReadOffset, proofReadCount, wArray, ref writeOffset, false );
            SCRAMCommon.ArrayXor( auxArray, wArray, digestStart, writeStart, digestByteCount );

            // Calculate H(h_xor) and validate that equals to credentials.ClientKeyDigest
            algorithm.ComputeDigest( auxArray, digestStart, digestByteCount, wArray, writeStart );


            for ( var i = 0; i < digestByteCount && errorCode == 0; ++i )
            {
               if ( wArray[writeStart + i] != credentials.ClientKeyDigest[i] )
               {
                  errorCode = SCRAMCommon.ERROR_CLIENT_SENT_WRONG_CREDENTIALS;
               }
            }

            if ( errorCode == 0 )
            {
               // Send our proof
               writeOffset = writeStart;
               this.PerformValidateWrite( ref args, credentials, ref writeOffset, curAux, beforeProofReadCount );
            }
         }

         return errorCode;
      }

      private Int32 PerformValidateRead(
         ref SASLAuthenticationArguments args,
         out Int32 beforeProofReadCount,
         out Int32 proofReadOffset,
         out Int32 proofReadCount
         )
      {
         // Validate that message is: client-message + server responce + client responce
         // Client responce must be "c=biws,r=<client nonce><server nonce>"
         var encodingInfo = args.Encoding;
         var array = args.ReadArray;
         var readOffset = args.ReadOffset;
         encodingInfo.VerifyASCIIBytes( array, ref readOffset, SCRAMCommon.WITHOUT_PROOF_PREFIX );

         var auxArray = this._auxArray.Array;
         (var nonceOffset, var nonceCount) = this._fullNonceInfo;
         var errorCode = nonceCount > 0 ? 0 : SCRAMCommon.ERROR_SERVER_SENT_WRONG_NONCE;

         for ( var i = 0; i < nonceCount && errorCode == 0; ++i )
         {
            if ( array[readOffset + i] != auxArray[nonceOffset + i] )
            {
               errorCode = SCRAMCommon.ERROR_SERVER_SENT_WRONG_NONCE;
            }
         }
         readOffset += nonceCount;
         beforeProofReadCount = readOffset - args.ReadOffset;

         if ( errorCode == 0 )
         {
            encodingInfo.VerifyASCIIBytes( array, ref readOffset, SCRAMCommon.CLIENT_PROOF_PREFIX );
            proofReadOffset = readOffset;
            proofReadCount = StringConversions.GetBase64CharCount( this._algorithm.DigestByteCount, true ) * encodingInfo.BytesPerASCIICharacter;
            if ( args.ReadOffset + args.ReadCount != readOffset + proofReadCount )
            {
               // Too small/big message
               errorCode = SCRAMCommon.ERROR_INVALID_FORMAT;
            }
         }
         else
         {
            proofReadCount = proofReadOffset = -1;
         }

         return errorCode;
      }

      private void PerformValidateWrite(
         ref SASLAuthenticationArguments args,
         SASLCredentialsSCRAMForServer credentials,
         ref Int32 writeOffset,
         Int32 auxArrayCount,
         Int32 readArrayCount
         )
      {
         var encodingInfo = args.Encoding;
         var writeArray = args.WriteArray;
         var algorithm = this._algorithm;
         var digestByteCount = algorithm.DigestByteCount;

         // Reserve enough space
         writeArray.CurrentMaxCapacity = writeOffset
            + SCRAMCommon.VALIDATE_PREFIX.Length * encodingInfo.BytesPerASCIICharacter
            + StringConversions.GetBase64CharCount( digestByteCount, true )
            ;
         var array = writeArray.Array;

         // Write pefix
         encodingInfo.WriteString( array, ref writeOffset, SCRAMCommon.VALIDATE_PREFIX );

         // Write actual proof
         this._auxArray.CurrentMaxCapacity = digestByteCount;
         var auxArray = this._auxArray.Array;
         using ( var hmac = algorithm.CreateHMAC( credentials.ServerKey, skipDisposeAlgorithm: true, skipZeroingOutKey: true ) )
         {
            hmac.ProcessBlock( auxArray, 0, auxArrayCount );
            hmac.ProcessBlock( args.ReadArray, args.ReadOffset, readArrayCount );
            // We can write to aux array
            hmac.WriteDigest( auxArray, 0 );
         }

         // Proof is now in the beginning of aux array
         encodingInfo.WriteBinaryAsBase64ASCIICharactersWithPadding( auxArray, 0, digestByteCount, array, ref writeOffset, false );
      }

   }

   public static partial class UtilPackUtility
   {
      public static SASLMechanism CreateSASLServerSCRAM(
         this BlockDigestAlgorithm algorithm,
         Func<String, ValueTask<SASLCredentialsSCRAMForServer>> getCredentials,
         Func<Byte[]> nonceGenerator = null
         )
      {
         return new SASLMechanismSCRAMForServer( ArgumentValidator.ValidateNotNullReference( algorithm ), getCredentials, nonceGenerator );
      }
   }
}
