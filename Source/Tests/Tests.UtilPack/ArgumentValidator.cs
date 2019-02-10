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
using System.Linq;
using System.Text;
using UtilPack;

namespace Tests.UtilPack
{
   [TestClass]
   public class ArgumentValidatorTests
   {
      [TestMethod]
      public void TestValidateNotNullReference()
      {
         var obj = new Object();
         Assert.IsTrue( ReferenceEquals( obj, ArgumentValidator.ValidateNotNullReference( obj ) ) );
      }

      [TestMethod,
         ExpectedException( typeof( NullReferenceException ), ArgumentValidator.NULLREF_MESSAGE, AllowDerivedTypes = false )]
      public void TestValidateNotNullReferenceWithNull()
      {
         ArgumentValidator.ValidateNotNullReference<Object>( null );
      }

      [TestMethod]
      public void TestValidateNotNull()
      {
         var obj = new Object();
         Assert.IsTrue( ReferenceEquals( obj, ArgumentValidator.ValidateNotNull( nameof( obj ), obj ) ) );
      }

      private const String OBJ = "obj";

      [TestMethod,
         ExpectedException( typeof( ArgumentNullException ), OBJ, AllowDerivedTypes = false )]
      public void TestValidateNotNullWithNull()
      {
         ArgumentValidator.ValidateNotNull<Object>( OBJ, null );
      }

      [TestMethod]
      public void TestValidateNotEmptyString()
      {
         ArgumentValidator.ValidateNotEmpty( OBJ, "nonempty" );
      }

      [TestMethod,
         ExpectedException( typeof( ArgumentException ), OBJ + ArgumentValidator.EMPTY_STRING_SUFFIX, AllowDerivedTypes = false )
         ]
      public void TestValidateNotEmptyStringWithEmptyString()
      {
         ArgumentValidator.ValidateNotEmpty( OBJ, "" );
      }

      [TestMethod,
         ExpectedException( typeof( ArgumentNullException ), OBJ, AllowDerivedTypes = false )]
      public void TestValidateNotEmptyStringWithNullString()
      {
         ArgumentValidator.ValidateNotEmpty( OBJ, null );
      }

      [TestMethod]
      public void TestValidateNonEmptyEnumerable()
      {
         ArgumentValidator.ValidateNotEmpty( OBJ, Enumerable.Repeat( new Object(), 1 ) );
      }

      [TestMethod,
         ExpectedException( typeof( ArgumentException ), OBJ + ArgumentValidator.EMPTY_SUFFIX, AllowDerivedTypes = false )]
      public void TestValidateNonEmptyEnumerableWithEmptyEnumerable()
      {
         ArgumentValidator.ValidateNotEmpty( OBJ, Enumerable.Empty<Object>() );
      }

      [TestMethod,
         ExpectedException( typeof( ArgumentNullException ), OBJ, AllowDerivedTypes = false )]
      public void TestValidateNonEmptyEnumerableWithNull()
      {
         ArgumentValidator.ValidateNotEmpty( OBJ, null as IEnumerable<Object> );
      }

      [TestMethod]
      public void TestValidateNonEmptyArray()
      {
         ArgumentValidator.ValidateNotEmpty( OBJ, new[] { new Object() } );
      }

      [TestMethod,
         ExpectedException( typeof( ArgumentException ), OBJ + ArgumentValidator.EMPTY_SUFFIX, AllowDerivedTypes = false )]
      public void TestValidateNonEmptyEnumerableWithEmptyArray()
      {
         ArgumentValidator.ValidateNotEmpty( OBJ, new Object[] { } );
      }

      [TestMethod,
         ExpectedException( typeof( ArgumentNullException ), OBJ, AllowDerivedTypes = false )]
      public void TestValidateNonEmptyArrayWithNull()
      {
         ArgumentValidator.ValidateNotEmpty( OBJ, null as Object[] );
      }

      [TestMethod]
      public void TestValidateAllNotNull()
      {
         ArgumentValidator.ValidateAllNotNull( OBJ, new Object[] { } );
         ArgumentValidator.ValidateAllNotNull( OBJ, new[] { new Object() } );
      }

      [TestMethod,
         ExpectedException( typeof( ArgumentNullException ), AllowDerivedTypes = false )]
      public void TestValidateAllNotNullWithNull()
      {
         ArgumentValidator.ValidateAllNotNull( OBJ, new Object[] { null } );
      }
   }

}
