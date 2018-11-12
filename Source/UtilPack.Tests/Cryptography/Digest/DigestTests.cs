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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UtilPack.Tests.Cryptography.Digest
{
   using TNativeAlgorithmFactory = Func<Byte[], System.Security.Cryptography.HashAlgorithm>;
   using TUtilPackAlgorithmFactory = Func<Byte[], BlockDigestAlgorithm>;

   [TestClass]
   public class DigestTests
   {
      private static readonly TNativeAlgorithmFactory NativeMD5 = ( key ) => System.Security.Cryptography.MD5.Create();
      private static readonly TNativeAlgorithmFactory NativeSHA128 = ( key ) => System.Security.Cryptography.SHA1.Create();
      private static readonly TNativeAlgorithmFactory NativeSHA256 = ( key ) => System.Security.Cryptography.SHA256.Create();
      private static readonly TNativeAlgorithmFactory NativeSHA384 = ( key ) => System.Security.Cryptography.SHA384.Create();
      private static readonly TNativeAlgorithmFactory NativeSHA512 = ( key ) => System.Security.Cryptography.SHA512.Create();

      private static readonly TNativeAlgorithmFactory NativeHMACMD5 = ( key ) => new System.Security.Cryptography.HMACMD5( key );
      private static readonly TNativeAlgorithmFactory NativeHMACSHA128 = ( key ) => new System.Security.Cryptography.HMACSHA1( key );
      private static readonly TNativeAlgorithmFactory NativeHMACSHA256 = ( key ) => new System.Security.Cryptography.HMACSHA256( key );
      private static readonly TNativeAlgorithmFactory NativeHMACSHA384 = ( key ) => new System.Security.Cryptography.HMACSHA384( key );
      private static readonly TNativeAlgorithmFactory NativeHMACSHA512 = ( key ) => new System.Security.Cryptography.HMACSHA512( key );

      private static readonly TUtilPackAlgorithmFactory UtilPackMD5 = ( key ) => new MD5();
      private static readonly TUtilPackAlgorithmFactory UtilPackSHA128 = ( key ) => new SHA128();
      private static readonly TUtilPackAlgorithmFactory UtilPackSHA256 = ( key ) => new SHA256();
      private static readonly TUtilPackAlgorithmFactory UtilPackSHA384 = ( key ) => new SHA384();
      private static readonly TUtilPackAlgorithmFactory UtilPackSHA512 = ( key ) => new SHA512();


      private static readonly TUtilPackAlgorithmFactory UtilPackHMACMD5 = ( key ) => new MD5().CreateHMAC( key );
      private static readonly TUtilPackAlgorithmFactory UtilPackHMACSHA128 = ( key ) => new SHA128().CreateHMAC( key );
      private static readonly TUtilPackAlgorithmFactory UtilPackHMACSHA256 = ( key ) => new SHA256().CreateHMAC( key );
      private static readonly TUtilPackAlgorithmFactory UtilPackHMACSHA384 = ( key ) => new SHA384().CreateHMAC( key );
      private static readonly TUtilPackAlgorithmFactory UtilPackHMACSHA512 = ( key ) => new SHA512().CreateHMAC( key );

      private static T PickBasedOnKey<T>( T keyless, T keyful, Byte[] key )
      {
         return key.IsNullOrEmpty() ? keyless : keyful;
      }

      [DataTestMethod,
         DataRow( null ),
         DataRow( new Byte[] { 1, 2, 3 } )
         ]
      public void TestMD5(
         Byte[] key
         )
      {
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeMD5, NativeHMACMD5, key ),
            PickBasedOnKey( UtilPackMD5, UtilPackHMACMD5, key ),
            key,
            1, 10
            );
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeMD5, NativeHMACMD5, key ),
            PickBasedOnKey( UtilPackMD5, UtilPackHMACMD5, key ),
            key,
            1000,
            2000
            );
      }

      [DataTestMethod,
         DataRow( null ),
         DataRow( new Byte[] { 1, 2, 3 } )]
      public void TestSHA128(
         Byte[] key
         )
      {
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeSHA128, NativeHMACSHA128, key ),
            PickBasedOnKey( UtilPackSHA128, UtilPackHMACSHA128, key ),
            key,
            1, 10
            );
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeSHA128, NativeHMACSHA128, key ),
            PickBasedOnKey( UtilPackSHA128, UtilPackHMACSHA128, key ),
            key,
            1000,
            2000
            );
      }

      [DataTestMethod,
         DataRow( null ),
         DataRow( new Byte[] { 1, 2, 3 } )]
      public void TestSHA256(
         Byte[] key
         )
      {
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeSHA256, NativeHMACSHA256, key ),
            PickBasedOnKey( UtilPackSHA256, UtilPackHMACSHA256, key ),
            key,
            1, 10
            );
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeSHA256, NativeHMACSHA256, key ),
            PickBasedOnKey( UtilPackSHA256, UtilPackHMACSHA256, key ),
            key,
            1000,
            2000
            );
      }

      [DataTestMethod,
         DataRow( null ),
         DataRow( new Byte[] { 1, 2, 3 } )]
      public void TestSHA384(
         Byte[] key
         )
      {
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeSHA384, NativeHMACSHA384, key ),
            PickBasedOnKey( UtilPackSHA384, UtilPackHMACSHA384, key ),
            key,
            1, 10
            );
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeSHA384, NativeHMACSHA384, key ),
            PickBasedOnKey( UtilPackSHA384, UtilPackHMACSHA384, key ),
            key,
            1000,
            2000
            );
      }

      [DataTestMethod,
         DataRow( null ),
         DataRow( new Byte[] { 1, 2, 3 } )]
      public void TestSHA512(
         Byte[] key
         )
      {
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeSHA512, NativeHMACSHA512, key ),
            PickBasedOnKey( UtilPackSHA512, UtilPackHMACSHA512, key ),
            key,
            1, 10
            );
         this.VerifyNativeVsUtilPack(
            PickBasedOnKey( NativeSHA512, NativeHMACSHA512, key ),
            PickBasedOnKey( UtilPackSHA512, UtilPackHMACSHA512, key ),
            key,
            1000,
            2000
            );
      }

      [TestMethod]
      public void TestMultipleSmallWrites()
      {
         var b1 = new Byte[] { 1, 2, 3 };
         var b2 = new Byte[] { 4, 5, 6 };
         Byte[] nativeHash;
         using ( var native = System.Security.Cryptography.SHA512.Create() )
         {
            nativeHash = native.ComputeHash( b1.Concat( b2 ).ToArray() );
         }

         var utilPackHash = new Byte[SHA512.DIGEST_BYTE_COUNT];
         using ( var utilPack = new SHA512() )
         {
            utilPack.ProcessBlock( b1.ToArray() );
            utilPack.ProcessBlock( b2.ToArray() );
            utilPack.WriteDigest( utilPackHash );
         }

         Assert.IsTrue( ArrayEqualityComparer<Byte>.ArrayEquality( nativeHash, utilPackHash ) );
      }

      private void VerifyNativeVsUtilPack(
         TNativeAlgorithmFactory nativeFactory,
         TUtilPackAlgorithmFactory utilPackFactory,
         Byte[] key,
         Int32 minLength,
         Int32 maxLength
         )
      {
         var r = new Random();
         var count = minLength + ( Math.Abs( r.NextInt32() ) % ( maxLength - minLength ) );
         var bytez = r.NextBytes( count );

         Byte[] nativeHash;
         using ( var native = nativeFactory( key.CreateArrayCopy() ) )
         {
            nativeHash = native.ComputeHash( bytez );
         }

         Byte[] camHash;
         using ( var cam = utilPackFactory( key.CreateArrayCopy() ) )
         {
            camHash = cam.ComputeDigest( bytez, 0, bytez.Length );

            Assert.IsTrue(
               ArrayEqualityComparer<Byte>.ArrayEquality( nativeHash, camHash ),
               "The hash differed:\nNative hash: {0}\nUtilPack hash: {1}\ninput: {2}",
               StringConversions.CreateHexString( nativeHash ),
               StringConversions.CreateHexString( camHash ),
               StringConversions.CreateHexString( bytez )
               );

            // Test that resetting works by computing same digest again
            camHash = cam.ComputeDigest( bytez, 0, bytez.Length );
            Assert.IsTrue(
               ArrayEqualityComparer<Byte>.ArrayEquality( nativeHash, camHash ),
               "The hash differed:\nNative hash: {0}\nUtilPack hash: {1}\ninput: {2}",
               StringConversions.CreateHexString( nativeHash ),
               StringConversions.CreateHexString( camHash ),
               StringConversions.CreateHexString( bytez )
               );
         }


      }

   }


}

public static partial class E_UtilPackTests
{

   // From Jon Skeet's answer on http://stackoverflow.com/questions/609501/generating-a-random-decimal-in-c-sharp
   /// <summary>
   /// Returns an Int32 with a random value across the entire range of
   /// possible values.
   /// </summary>
   public static Int32 NextInt32( this Random rng )
   {
      unchecked
      {
         var firstBits = rng.Next( 0, 1 << 4 ) << 28;
         var lastBits = rng.Next( 0, 1 << 28 );
         return firstBits | lastBits;
      }
   }

   public static Byte[] NextBytes( this Random rng, Int32 byteCount )
   {
      var bytez = new Byte[byteCount];
      rng.NextBytes( bytez );
      return bytez;
   }
}