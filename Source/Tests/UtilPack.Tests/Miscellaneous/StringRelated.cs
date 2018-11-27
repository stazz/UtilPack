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
using UtilPack;

namespace UtilPack.Tests.Miscellaneous
{
   [TestClass]
   public class StringRelatedTest
   {
      [TestMethod]
      public void TestInt32Parsing()
      {
         var array = new Byte[]
         {
            0x31,
            0x32,
            0x33,
            0x34,
            0x35,
            0x00
         };
         var encoding = Encoding.ASCII.CreateDefaultEncodingInfo();
         var idx = 0;
         var number = encoding.ParseInt32Textual( array, ref idx, (6, false) );
         Assert.AreEqual( 5, idx );
         Assert.AreEqual( 12345, number );
         idx = 0;
         number = encoding.ParseInt32Textual( array, ref idx );
         Assert.AreEqual( 5, idx );
         Assert.AreEqual( 12345, number );
      }
   }
}
