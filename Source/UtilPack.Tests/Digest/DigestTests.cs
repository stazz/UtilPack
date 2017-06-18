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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using UtilPack.Cryptography.Digest;
using System.Linq;

namespace UtilPack.Tests.Digest
{
   [TestClass]
   public class DigestTests
   {
      private static readonly Func<System.Security.Cryptography.HashAlgorithm> NativeMD5 = () => System.Security.Cryptography.MD5.Create();
      private static readonly Func<System.Security.Cryptography.HashAlgorithm> NativSHA128 = () => System.Security.Cryptography.SHA1.Create();
      private static readonly Func<System.Security.Cryptography.HashAlgorithm> NativeSHA256 = () => System.Security.Cryptography.SHA256.Create();
      private static readonly Func<System.Security.Cryptography.HashAlgorithm> NativeSHA384 = () => System.Security.Cryptography.SHA384.Create();
      private static readonly Func<System.Security.Cryptography.HashAlgorithm> NativeSHA512 = () => System.Security.Cryptography.SHA512.Create();

      private static readonly Func<BlockDigestAlgorithm> UtilPackMD5 = () => new MD5();
      private static readonly Func<BlockDigestAlgorithm> UtilPackSHA128 = () => new SHA128();
      private static readonly Func<BlockDigestAlgorithm> UtilPackSHA256 = () => new SHA256();
      private static readonly Func<BlockDigestAlgorithm> UtilPackSHA384 = () => new SHA384();
      private static readonly Func<BlockDigestAlgorithm> UtilPackSHA512 = () => new SHA512();


      [TestMethod]
      public void TestMD5()
      {
         VerifyNativeVsUtilPack(
            NativeMD5,
            UtilPackMD5,
            1, 10
            );
         VerifyNativeVsUtilPack(
            NativeMD5,
            UtilPackMD5,
            1000,
            2000
            );
      }

      [TestMethod]
      public void TestSHA128()
      {
         VerifyNativeVsUtilPack(
            NativSHA128,
            UtilPackSHA128,
            1, 10
            );
         VerifyNativeVsUtilPack(
            NativSHA128,
            UtilPackSHA128,
            1000,
            2000
            );
      }

      [TestMethod]
      public void TestSHA256()
      {
         VerifyNativeVsUtilPack(
            NativeSHA256,
            UtilPackSHA256,
            1, 10
            );
         VerifyNativeVsUtilPack(
            NativeSHA256,
            UtilPackSHA256,
            1000,
            2000
            );
      }

      [TestMethod]
      public void TestSHA384()
      {
         VerifyNativeVsUtilPack(
            NativeSHA384,
            UtilPackSHA384,
            1, 10
            );
         VerifyNativeVsUtilPack(
            NativeSHA384,
            UtilPackSHA384,
            1000,
            2000
            );
      }

      [TestMethod]
      public void TestSHA512()
      {
         VerifyNativeVsUtilPack(
            NativeSHA512,
            UtilPackSHA512,
            1, 10
            );
         VerifyNativeVsUtilPack(
            NativeSHA512,
            UtilPackSHA512,
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
         Func<System.Security.Cryptography.HashAlgorithm> nativeFactory,
         Func<BlockDigestAlgorithm> utilPackFactory,
         Int32 minLength,
         Int32 maxLength
         )
      {
         var r = new Random();
         var count = minLength + ( Math.Abs( r.NextInt32() ) % ( maxLength - minLength ) );
         var bytez = r.NextBytes( count );

         Byte[] nativeHash;
         using ( var native = nativeFactory() )
         {
            nativeHash = native.ComputeHash( bytez );
         }

         Byte[] camHash;
         using ( var cam = utilPackFactory() )
         {
            camHash = cam.ComputeDigest( bytez, 0, bytez.Length );
         }

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