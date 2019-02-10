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
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.TabularData;

namespace Tests.UtilPack.TabularData
{
   [TestClass]
   public class DataColumnTests
   {
      [TestMethod, Timeout( 1000 )]
      public async Task TestSkippingColumns()
      {
         var val1 = 100;
         var val2 = 200;
         var col1 = new TestDataColumn( new TestDataColumnMetaData( "col1" ), 0, null, val1 );
         var col2 = new TestDataColumn( new TestDataColumnMetaData( "col2" ), 1, col1, val2 );
         var row = new AsyncDataRowImpl(
            new DataRowMetaDataImpl<AsyncDataColumnMetaData>( new[] { (AsyncDataColumnMetaData) col1.MetaData, (AsyncDataColumnMetaData) col2.MetaData } ),
            new[] { col1, col2 }
            );

         var val2Actual = await row.GetValueAsync<Int32>( 1 );
         Assert.AreEqual( val2, val2Actual );
         // The first column must've had its value read just from reading the second column, since we are inheriting DataColumnSUKS
         Assert.AreEqual( sizeof( Int32 ), col1.Index );
      }
   }

   internal sealed class TestDataColumn : DataColumnSUKS
   {
      private readonly Byte[] _array;
      private Int32 _index;

      public TestDataColumn( DataColumnMetaData md, Int32 columnIndex, AsyncDataColumn prevColumn, Int32 value )
         : base( md, columnIndex, prevColumn )
      {
         this._array = new Byte[sizeof( Int32 )];
         this._array.WriteInt32LEToBytesNoRef( 0, value );
      }

      protected override ValueTask<Int32> DoReadFromStreamAsync( Byte[] array, Int32 offset, Int32 count )
      {
         Assert.AreEqual( 0, this._index );
         this._array.CopyTo( array, ref this._index, offset, count );
         return new ValueTask<Int32>( count );
      }

      protected override ValueTask<Int32> ReadByteCountAsync()
      {
         return new ValueTask<Int32>( this._array.Length );
      }

      protected override ValueTask<Object> ReadValueAsync( Int32 byteCount )
      {
         Assert.AreEqual( sizeof( Int32 ), byteCount );
         Assert.AreEqual( 0, this._index );
         return new ValueTask<Object>( this._array.ReadInt32LEFromBytes( ref this._index ) );
      }

      public Int32 Index => this._index;
   }

   internal sealed class TestDataColumnMetaData : AbstractAsyncDataColumnMetaData
   {
      public TestDataColumnMetaData(
         String label
         ) : base( typeof( Int32 ), label )
      {
      }

      public override Object ChangeType( Object value, Type targetType )
      {
         return (Int32) value;
      }

      public override ValueTask<Object> ConvertFromBytesAsync( Stream stream, Int32 byteCount )
      {
         throw new NotImplementedException();
      }
   }
}
