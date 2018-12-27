/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
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
using UtilPack;

namespace Tests.UtilPack
{
   [TestClass]
   public class BinaryStringPoolTests
   {
      [TestMethod]
      public void TestBinaryStringPoolCaching()
      {
         var encoding = new UTF8Encoding( false, false );
         var pool = BinaryStringPoolFactory.NewNotConcurrentBinaryStringPool( encoding );
         var str1 = "Test";
         var str1Pooled = pool.GetString( encoding.GetBytes( str1 ) );
         Assert.AreEqual( str1, str1Pooled );
         Assert.AreNotSame( str1, str1Pooled );
         var str1PooledAgain = pool.GetString( encoding.GetBytes( str1 ) );
         Assert.AreSame( str1Pooled, str1PooledAgain );
      }

      [TestMethod]
      public void TestBinaryStringPoolDifferentStrings()
      {
         var encoding = new UTF8Encoding( false, false );
         var pool = BinaryStringPoolFactory.NewNotConcurrentBinaryStringPool( encoding );
         var str1 = "Test";
         var str1Pooled = pool.GetString( encoding.GetBytes( str1 ) );
         Assert.AreEqual( str1, str1Pooled );
         Assert.AreNotSame( str1, str1Pooled );
         var str2 = "AnotherTest";
         var str2Pooled = pool.GetString( encoding.GetBytes( str2 ) );
         Assert.AreEqual( str2, str2Pooled );
         Assert.AreNotSame( str2, str2Pooled );
      }

      [TestMethod]
      public void TestBinaryStringPoolArrayModification()
      {
         var encoding = new UTF8Encoding( false, false );
         var pool = BinaryStringPoolFactory.NewNotConcurrentBinaryStringPool( encoding );
         var str = "Test";
         var strBytes = encoding.GetBytes( str );
         var strPooled = pool.GetString( strBytes );
         strBytes[1] = 1;
         var strPooled2 = pool.GetString( encoding.GetBytes( str ) );
         Assert.AreSame( strPooled, strPooled2 );
      }
   }
}
