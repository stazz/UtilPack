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
using FluentCryptography.Abstractions;
using FluentCryptography.Digest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilPack;

namespace FluentCryptography.SASL.SCRAM
{
   /// <summary>
   /// This class contains some information common for both client- and server-side SCRAM mechanisms.
   /// </summary>
   public static class SCRAMCommon
   {
      internal const String WITHOUT_PROOF_PREFIX = "c=biws,r="; // biws = Base64-encoded "n,,"
      internal const String CLIENT_FIRST_PREFIX_1 = "n,,";
      internal const String CLIENT_FIRST_PREFIX_2 = "n=";
      internal const String COMMA_ESCAPE = "=2C";
      internal const String EQUALS_ESCAPE = "=3D";
      internal const String NONCE_PREFIX = "r=";
      internal const String CLIENT_NONCE_PREFIX = "," + NONCE_PREFIX;
      internal const String SALT_PREFIX = ",s=";
      internal const String ITERATION_PREFIX = ",i=";
      internal const String PROOF_PREFIX = "p=";
      internal const String CLIENT_PROOF_PREFIX = "," + PROOF_PREFIX;
      internal const String VALIDATE_PREFIX = "v=";

      internal const Byte COMMA = (Byte) ',';

      /// <summary>
      /// This error code is returned by both client and server when <see cref="SASLMechanism"/> is attempted to use concurrently.
      /// </summary>
      public const Int32 ERROR_CONCURRENT_ACCESS = 1;

      /// <summary>
      /// This error code is returned by both client and server when the remote response is in invalid format.
      /// </summary>
      public const Int32 ERROR_INVALID_RESPONSE_MESSAGE_FORMAT = 2;

      /// <summary>
      /// This error code is returned by both client and server when using <see cref="SASLMechanism.ChallengeAsync"/> is not suitable in its current state. One should call <see cref="SASLMechanism.Reset"/> and re-start authentication process.
      /// </summary>
      public const Int32 ERROR_INVALID_STATE = 3;

      /// <summary>
      /// This error code is returned by client when server sends wrong nonce.
      /// </summary>
      public const Int32 ERROR_SERVER_SENT_WRONG_NONCE = 4;

      /// <summary>
      /// This error code is returned by client when server sends wrong proof.
      /// </summary>
      public const Int32 ERROR_SERVER_SENT_WRONG_PROOF = 5;

      /// <summary>
      /// This error code is returned by server when client sends wrong credentials.
      /// </summary>
      public const Int32 ERROR_CLIENT_SENT_WRONG_CREDENTIALS = 6;

      /// <summary>
      /// This error code is returned by server when client sends invalid nonce.
      /// </summary>
      public const Int32 ERROR_CLIENT_SENT_INVALID_NONCE = 7;

      /// <summary>
      /// This error code is returned by client when <see cref="SASLCredentialsSCRAMForClient"/> object is <c>null</c> or in some way invalid.
      /// </summary>
      public const Int32 ERROR_CLIENT_SUPPLIED_WITH_INVALID_CREDENTIALS = 8;

      internal static Byte[] UseNonceGenerator( BlockDigestAlgorithm algorithm, Func<Byte[]> nonceGenerator )
      {
         var retVal = nonceGenerator?.Invoke();
         return retVal.IsNullOrEmpty() ?
            GenerateNonce( algorithm, 24, 32 ) :
            retVal; // Let's not check it after all - but document that nonce returned by factory is not checked!
      }

      //private static void CheckCustomNonce( BlockDigestAlgorithm algorithm, Byte[] nonce )
      //{
      //   using ( var randomLazy = new LazyDisposable<SecureRandom>( () => new SecureRandom( DigestBasedRandomGenerator.CreateAndSeedWithDefaultLogic( algorithm, skipDisposeAlgorithm: true ) ) ) )
      //   {
      //      for ( var i = 0; i < nonce.Length; ++i )
      //      {
      //         var b = nonce[i];
      //         if (b < )
      //      }
      //   }

      //}

      internal static Byte[] GenerateNonce( BlockDigestAlgorithm algorithm, Int32 minSize, Int32 maxValue )
      {

         using ( var random = new SecureRandom( DigestBasedRandomGenerator.CreateAndSeedWithDefaultLogic( algorithm, skipDisposeAlgorithm: true ) ) )
         {
            var retVal = new Byte[random.Next( minSize, maxValue )];
            // Actual legal values are 0x21-0x7E
            for ( var i = 0; i < retVal.Length; ++i )
            {
               Byte nextVal;
               do
               {
                  nextVal = (Byte) random.Next( 0x21, 0x7F );
                  retVal[i] = nextVal;
               } while ( nextVal == COMMA ); // ... except commas not allowed
            }
            return retVal;
         }

      }

      internal static void ArrayXor( Byte[] x, Byte[] y, Int32 length )
      {
         if ( length < 0 )
         {
            length = x.Length;
         }
         for ( var i = 0; i < length; ++i )
         {
            x[i] ^= y[i];
         }
      }

      internal static void ArrayXor( Byte[] x, Byte[] y, Int32 xOffset, Int32 yOffset, Int32 length )
      {
         for ( var i = 0; i < length; ++i )
         {
            x[i + xOffset] ^= y[i + yOffset];
         }
      }
   }
}
