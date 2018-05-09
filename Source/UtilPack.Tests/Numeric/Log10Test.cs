using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace UtilPack.Tests.Numeric
{
   [TestClass]
   public class NumericTest
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
   }
}
