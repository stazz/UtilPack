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
using System.Linq;
using System.Text;
using UtilPack;

namespace Tests.UtilPack
{
   [TestClass]
   public class BinaryUtilsTest
   {

      [TestMethod]
      public void TestLog10()
      {
         Assert.AreEqual( -1, BinaryUtils.Log10( 0 ) );
         Assert.AreEqual( 0, BinaryUtils.Log10( 1 ) );
         Assert.AreEqual( 0, BinaryUtils.Log10( 2 ) );
         Assert.AreEqual( 0, BinaryUtils.Log10( 3 ) );
         Assert.AreEqual( 0, BinaryUtils.Log10( 4 ) );
         Assert.AreEqual( 0, BinaryUtils.Log10( 5 ) );
         Assert.AreEqual( 0, BinaryUtils.Log10( 6 ) );
         Assert.AreEqual( 0, BinaryUtils.Log10( 7 ) );
         Assert.AreEqual( 0, BinaryUtils.Log10( 8 ) );
         Assert.AreEqual( 0, BinaryUtils.Log10( 9 ) );
         Assert.AreEqual( 1, BinaryUtils.Log10( 10 ) );
         Assert.AreEqual( 1, BinaryUtils.Log10( 11 ) );
         Assert.AreEqual( 18, BinaryUtils.Log10( 9223372036854775807UL ) );  // 2^63-1
         Assert.AreEqual( 18, BinaryUtils.Log10( 9223372036854775808UL ) );  // 2^63
         Assert.AreEqual( 18, BinaryUtils.Log10( 9999999999999999999UL ) );
         Assert.AreEqual( 19, BinaryUtils.Log10( 10000000000000000000UL ) );
         Assert.AreEqual( 19, BinaryUtils.Log10( 18446744073709551615UL ) );  // 2^64
      }

      [TestMethod]
      public void TestLog2()
      {
         Assert.AreEqual( -1, BinaryUtils.Log2( 0 ) );

         foreach ( var i in Enumerable.Range( 1, 31 ) )
         {
            var number = unchecked((UInt32) 1 << i);
            Assert.AreEqual( i, BinaryUtils.Log2( number ) );
            Assert.AreEqual( i - 1, BinaryUtils.Log2( number - 1 ) );
         }
         Assert.AreEqual( 31, BinaryUtils.Log2( UInt32.MaxValue ) );

         Assert.AreEqual( -1, BinaryUtils.Log2( 0UL ) );

         foreach ( var i in Enumerable.Range( 1, 31 ) )
         {
            var number = unchecked((UInt64) 1L << i);
            Assert.AreEqual( i, BinaryUtils.Log2( number ) );
            Assert.AreEqual( i - 1, BinaryUtils.Log2( number - 1 ) );
         }
         Assert.AreEqual( 63, BinaryUtils.Log2( UInt64.MaxValue ) );
      }
   }
}
