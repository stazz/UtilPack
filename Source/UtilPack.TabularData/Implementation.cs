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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using UtilPack;
using UtilPack.TabularData;

namespace UtilPack.TabularData
{
   /// <summary>
   /// This class implements <see cref="AsyncDataRow"/> in most straightforward way, with <c>0</c>-based indexing..
   /// </summary>
   public class AsyncDataRowImpl : AsyncDataRow
   {
      /// <summary>
      /// Creates a new instance of <see cref="AsyncDataRowImpl"/> with given <see cref="DataRowMetaData{TColumnMetaData}"/> and <see cref="AsyncDataColumn"/>s.
      /// </summary>
      /// <param name="rowMetadata">The <see cref="DataRowMetaData{TColumnMetaData}"/> for this <see cref="AsyncDataRowImpl"/>.</param>
      /// <param name="columns">The array of <see cref="AsyncDataColumn"/> objects as columns of this data row.</param>
      /// <exception cref="ArgumentNullException">If either of <paramref name="columns"/> or <paramref name="rowMetadata"/> is <c>null</c>.</exception>
      public AsyncDataRowImpl(
         DataRowMetaData<AsyncDataColumnMetaData> rowMetadata,
         AsyncDataColumn[] columns
         )
      {
         this.Metadata = ArgumentValidator.ValidateNotNull( nameof( rowMetadata ), rowMetadata );
         this.Columns = ArgumentValidator.ValidateNotNull( nameof( columns ), columns );
      }

      /// <summary>
      /// Implements the <see cref="DataRow{TDataColumn, TDataColumnMetaData}.GetColumn(int)"/> method.
      /// </summary>
      /// <param name="index">The <c>0</c>-based column index.</param>
      /// <returns>The <see cref="AsyncDataColumn"/> at given <c>0</c>-basd index in <see cref="Columns"/> array.</returns>
      /// <exception cref="ArgumentException">If <paramref name="index"/> is less than <c>0</c> or greater or equal to length of <see cref="Columns"/> array.</exception>
      public virtual AsyncDataColumn GetColumn( Int32 index )
      {
         var cols = this.Columns;
         if ( index < 0 || index >= cols.Length )
         {
            throw new ArgumentException( "Given index was out of bounds", nameof( index ) );
         }

         return this.Columns[index];
      }

      /// <summary>
      /// Implements the <see cref="DataRow{TDataColumn, TDataColumnMetaData}.Metadata"/> property.
      /// </summary>
      /// <value>The <see cref="DataRowMetaData{TColumnMetaData}"/> of this <see cref="AsyncDataRowImpl"/>.</value>
      public DataRowMetaData<AsyncDataColumnMetaData> Metadata { get; }

      /// <summary>
      /// Gets the array of <see cref="AsyncDataColumn"/> objects.
      /// </summary>
      /// <value>The array of <see cref="AsyncDataColumn"/> objects.</value>
      protected AsyncDataColumn[] Columns { get; }
   }

   /// <summary>
   /// This class implements <see cref="AsyncDataColumn"/> in such way that it is agnostic to underlying implementation, but provides constraints regarding to concurrent invocation of methods.
   /// </summary>
   public abstract class AbstractAsyncDataColumn : AsyncDataColumn
   {
      private const Int32 INITIAL = 0;
      private const Int32 COMPLETE = 1;
      private const Int32 READING_WHOLE_VALUE = 2;
      private const Int32 READING_BYTES = 3;
      private const Int32 READING_BYTES_MORE_LEFT = 4;
      private const Int32 FAULTED = 5;

      private Int32 _state;
      private Object _value;

      /// <summary>
      /// Initializes a new instance of <see cref="AbstractAsyncDataColumn"/> with given parameters.
      /// </summary>
      /// <param name="metadata">The column <see cref="DataColumnMetaData"/>.</param>
      /// <param name="columnIndex">The index of this column in <see cref="AsyncDataRow"/> it was obtained from.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="metadata"/> is <c>null</c>.</exception>
      public AbstractAsyncDataColumn(
         DataColumnMetaData metadata,
         Int32 columnIndex
         )
      {
         this.MetaData = ArgumentValidator.ValidateNotNull( nameof( metadata ), metadata );
         this.ColumnIndex = columnIndex;

         this._state = INITIAL;
         this._value = null;
      }

      /// <summary>
      /// This method implements <see cref="AsyncDataColumn.TryGetValueAsync"/> method.
      /// </summary>
      /// <returns>A task which will on completion contain <see cref="ResultOrNone{TResult}"/> struct describing the data.</returns>
      /// <seealso cref="ResultOrNone{TResult}"/>
      /// <remarks>
      /// If value reading has already been started by <see cref="ReadBytesAsync(byte[], int, int)"/> method and not yet finished, this method will return <see cref="ResultOrNone{TResult}"/> such that its <see cref="ResultOrNone{TResult}.HasResult"/> property is <c>false</c>.
      /// </remarks>
      public async ValueTask<ResultOrNone<Object>> TryGetValueAsync()
      {
         Int32 oldState;
         ResultOrNone<Object> retVal;
         if ( ( oldState = Interlocked.CompareExchange( ref this._state, READING_WHOLE_VALUE, INITIAL ) ) > COMPLETE )
         {
            // Either concurrent/re-entrant attempt, or reading by bytes has started, or faulted
            retVal = new ResultOrNone<Object>();
         }
         else
         {
            if ( oldState == INITIAL )
            {
               // First-time acquisition
               var faulted = true;
               try
               {
                  Interlocked.Exchange( ref this._value, await this.PerformReadAsValueAsync() );
                  faulted = false;
               }
               catch
               {
                  Interlocked.Exchange( ref this._state, FAULTED );
                  throw;
               }
               finally
               {
                  if ( !faulted )
                  {
                     Interlocked.Exchange( ref this._state, COMPLETE );
                  }
               }
            }
            retVal = this._state == FAULTED ? default( ResultOrNone<Object> ) : new ResultOrNone<Object>( this._value );
         }

         return retVal;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="array">The byte array where to read the data to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing bytes.</param>
      /// <param name="count">The maximum amount of bytes to write.</param>
      /// <returns>Amount of bytes written to array, or <c>-1</c> if <see cref="TryGetValueAsync"/> has been invoked concurrently, or <c>0</c> if end of data has been encountered, or <c>null</c> if this method has been invoked concurrently.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="count"/> is less than <c>0</c>, or if array length is smaller than <paramref name="offset"/> <c>+</c> <paramref name="count"/>.</exception>
      /// <exception cref="ArgumentException">If <paramref name="offset"/> + <paramref name="count"/> is greater than array length.</exception>
      public async ValueTask<Int32?> ReadBytesAsync( Byte[] array, Int32 offset, Int32 count )
      {
         array.CheckArrayArguments( offset, count, true );
         Int32? retVal;
         Int32 oldState;
         if ( ( oldState = Interlocked.CompareExchange( ref this._state, READING_BYTES, INITIAL ) ) == INITIAL
            || ( oldState = Interlocked.CompareExchange( ref this._state, READING_BYTES, READING_BYTES_MORE_LEFT ) ) == READING_BYTES_MORE_LEFT
            )
         {
            var isComplete = false;
            try
            {
               var tuple = await this.PerformReadToBytes( array, offset, count, oldState == INITIAL );
               isComplete = tuple.IsComplete;

               retVal = tuple.BytesRead;
            }
            finally
            {
               Interlocked.Exchange( ref this._state, isComplete ? COMPLETE : READING_BYTES_MORE_LEFT );
            }
         }
         else if ( oldState == COMPLETE )
         {
            retVal = 0;
         }
         else if ( oldState == READING_BYTES )
         {
            retVal = null;
         }
         else
         {
            retVal = -1;
         }

         return retVal;
      }

      /// <summary>
      /// Implements <see cref="AbstractDataColumn.MetaData"/> property.
      /// Gets the <see cref="DataColumnMetaData"/> of this <see cref="AbstractAsyncDataColumn"/>.
      /// </summary>
      /// <value>The <see cref="DataColumnMetaData"/> of this <see cref="AbstractAsyncDataColumn"/>.</value>
      public DataColumnMetaData MetaData { get; }

      /// <summary>
      /// Implements <see cref="AbstractDataColumn.ColumnIndex"/> property.
      /// Gets the index of this <see cref="AbstractAsyncDataColumn"/> in <see cref="AsyncDataRow"/> it was obtained from.
      /// </summary>
      /// <value>The index of this <see cref="AbstractAsyncDataColumn"/> in <see cref="AsyncDataRow"/> it was obtained from.</value>
      public Int32 ColumnIndex { get; }

      /// <summary>
      /// This method should be overridden in derived class, and is called by <see cref="TryGetValueAsync"/> after concurrency checks pass and value is not cached.
      /// </summary>
      /// <returns>A task which should return the data on completion.</returns>
      protected abstract ValueTask<Object> PerformReadAsValueAsync();

      /// <summary>
      /// This method should be overridden in derived class, and is called by <see cref="ReadBytesAsync(byte[], int, int)"/> after checks for concurrency and parameters pass.
      /// </summary>
      /// <param name="array">The byte array where to read the data to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing bytes.</param>
      /// <param name="count">The maximum amount of bytes to write.</param>
      /// <param name="isInitialRead">Whether this is first call to <see cref="ReadBytesAsync(byte[], int, int)"/> method.</param>
      /// <returns>A task which should return how many bytes has written to <paramref name="array"/>, and whether whole data reading is complete.</returns>
      protected abstract ValueTask<(Int32 BytesRead, Boolean IsComplete)> PerformReadToBytes( Byte[] array, Int32 offset, Int32 count, Boolean isInitialRead );

      /// <summary>
      /// Derived classes may call and override this method to implement state-reset.
      /// Doing this causes cached value to be ignored on next call to <see cref="TryGetValueAsync"/>.
      /// </summary>
      protected virtual void Reset()
      {
         Interlocked.Exchange( ref this._state, INITIAL );
      }
   }

   /// <summary>
   /// This class provides straightforward implementation for <see cref="DataRowMetaData{TColumnMetaData}"/> using <c>0</c>-based column indexing.
   /// </summary>
   public class DataRowMetaDataImpl<TColumnMetaData> : DataRowMetaData<TColumnMetaData>
      where TColumnMetaData : DataColumnMetaData
   {
      private readonly Lazy<IDictionary<String, Int32>> _labels;

      /// <summary>
      /// Creates a new instance of <see cref="DataRowMetaDataImpl{TColumnMetaData}"/> with given column metadata objects.
      /// </summary>
      /// <param name="columnMetaDatas">The arra of <see cref="DataColumnMetaData"/> objects.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="columnMetaDatas"/> is <c>null</c>.</exception>
      public DataRowMetaDataImpl(
         TColumnMetaData[] columnMetaDatas
         )
      {
         this.ColumnMetaDatas = ArgumentValidator.ValidateNotNull( nameof( columnMetaDatas ), columnMetaDatas );

         var columnCount = columnMetaDatas.Length;
         this._labels = new Lazy<IDictionary<String, Int32>>( () =>
         {
            var dic = new Dictionary<String, Int32>();
            for ( var i = 0; i < columnCount; ++i )
            {
               dic[columnMetaDatas[i].Label] = i;
            }
            return dic;
         }, LazyThreadSafetyMode.ExecutionAndPublication );
      }

      /// <summary>
      /// This property implements <see cref="DataRowMetaData.ColumnCount"/>.
      /// Gets the amount of column objects in <see cref="ColumnMetaDatas"/> array.
      /// </summary>
      /// <value>The amount of column objects in <see cref="ColumnMetaDatas"/> array.</value>
      public Int32 ColumnCount => this.ColumnMetaDatas.Length;

      /// <summary>
      /// This method implements the <see cref="DataRowMetaData.GetIndexFor(string)"/> method.
      /// Tries to get an index for column with given label.
      /// </summary>
      /// <param name="columnName">The column label.</param>
      /// <returns>Index of column with given label. Will return <c>null</c> if <paramref name="columnName"/> is <c>null</c> or if column with given label does not exist in the <see cref="DataRow{TDataColumn, TDataColumnMetaData}"/> this <see cref="DataRowMetaDataImpl{TColumnMetaData}"/> was obtained from.</returns>
      public Int32? GetIndexFor( String columnName )
      {
         return columnName == null || !this._labels.Value.TryGetValue( columnName, out var idx ) ?
            (Int32?) null :
            idx;
      }

      /// <summary>
      /// Gets the array of <see cref="DataColumnMetaData"/> objects.
      /// </summary>
      /// <value>the array of <see cref="DataColumnMetaData"/> objects.</value>
      protected TColumnMetaData[] ColumnMetaDatas { get; }

      /// <summary>
      /// Implements <see cref="DataRowMetaData{TColumnMetaData}.GetColumnMetaData(int)"/>
      /// Gets the <see cref="DataColumnMetaData"/> for given column index.
      /// </summary>
      /// <param name="columnIndex">The index of the column in <see cref="ColumnMetaDatas"/> array.</param>
      /// <returns>The element at given index in <see cref="ColumnMetaDatas"/> array.</returns>
      /// <exception cref="ArgumentException">If <paramref name="columnIndex"/> is lesser than zero or greater or equal to the length of <see cref="ColumnMetaDatas"/> array.</exception>
      public TColumnMetaData GetColumnMetaData( Int32 columnIndex )
      {
         return columnIndex < 0 || columnIndex >= this.ColumnMetaDatas.Length ? throw new ArgumentException( nameof( columnIndex ) ) : this.ColumnMetaDatas[columnIndex];
      }
   }

   /// <summary>
   /// This class provides straightforward implementation for <see cref="DataColumnMetaData"/>.
   /// The <see cref="ColumnCLRType"/> and <see cref="Label"/> are read-only properties, and the <see cref="ChangeType(object, Type)"/> method is left <c>abstract</c>.
   /// </summary>
   public abstract class AbstractDataColumnMetaData : DataColumnMetaData
   {
      /// <summary>
      /// Initializes a new instance of <see cref="AbstractDataColumnMetaData"/> with fixed type and label information about the data.
      /// </summary>
      /// <param name="type">The CLR <see cref="Type"/> of the column data.</param>
      /// <param name="label">The column label. May be <c>null</c>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="type"/> is <c>null</c>.</exception>
      public AbstractDataColumnMetaData(
         Type type,
         String label
         )
      {
         this.ColumnCLRType = ArgumentValidator.ValidateNotNull( nameof( type ), type );
         this.Label = label;
      }

      /// <summary>
      /// Implements <see cref="DataColumnMetaData.ColumnCLRType"/> property.
      /// Gets the CLR <see cref="Type"/> of the column data.
      /// </summary>
      /// <value>The CLR <see cref="Type"/> of the column data.</value>
      public Type ColumnCLRType { get; }

      /// <summary>
      /// Implements <see cref="DataColumnMetaData.Label"/> property.
      /// Gets the textual column label. May be <c>null</c>.
      /// </summary>
      /// <value>The textual column label. May be <c>null</c>.</value>
      public String Label { get; }

      /// <summary>
      /// Implements signature of <see cref="DataColumnMetaData.ChangeType(object, Type)"/> method, but leaves implementation for derived classes.
      /// </summary>
      /// <param name="value">The data that was acquired from <see cref="AsyncDataColumn"/> this <see cref="DataColumnMetaData"/> was obtained from.</param>
      /// <param name="targetType">The type to transform the <paramref name="value"/> to.</param>
      /// <returns>An object that is assignalbe to <paramref name="targetType"/>.</returns>
      /// <remarks>
      /// If the cast is invalid, an exception should be thrown.
      /// This interface does not specify which exception should be then thrown.
      /// </remarks>
      public abstract Object ChangeType( Object value, Type targetType );
   }

   /// <summary>
   /// This class provides straightforward implementation for <see cref="AsyncDataColumnMetaData"/>, by extending <see cref="AbstractDataColumnMetaData"/> and leaving <see cref="ConvertFromBytesAsync(Stream, int)"/> method <c>abstract</c>.
   /// </summary>
   public abstract class AbstractAsyncDataColumnMetaData : AbstractDataColumnMetaData, AsyncDataColumnMetaData
   {
      /// <summary>
      /// Initializes a new instance of <see cref="AbstractAsyncDataColumnMetaData"/> with fixed type and label information about the data.
      /// </summary>
      /// <param name="type">The CLR <see cref="Type"/> of the column data.</param>
      /// <param name="label">The column label. May be <c>null</c>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="type"/> is <c>null</c>.</exception>
      public AbstractAsyncDataColumnMetaData(
         Type type,
         String label
         ) : base( type, label )
      {
      }

      /// <summary>
      /// Implements signature of <see cref="AsyncDataColumnMetaData.ConvertFromBytesAsync(Stream, int)"/> method, but leaves implementation for derived classes.
      /// </summary>
      /// <param name="stream">The stream containing raw byte data representation.</param>
      /// <param name="byteCount">The amount of bytes to read from the stream.</param>
      /// <returns>A task which on completion will have .NET data object.</returns>
      /// <remarks>
      /// If data is malformed, an exception should be thrown.
      /// </remarks>
      public abstract ValueTask<Object> ConvertFromBytesAsync( Stream stream, Int32 byteCount );
   }

   /// <summary>
   /// This class extends <see cref="AbstractAsyncDataColumn"/> to provide some common functionality when data originates from <see cref="Stream"/> which is unseekable but of known size (=<c>SUKS</c>).
   /// </summary>
   public abstract class DataColumnSUKS : AbstractAsyncDataColumn
   {
      private Int32 _totalBytesRead;
      private readonly ReadOnlyResettableAsyncLazy<Int32> _byteCount;

      /// <summary>
      /// Initializes a new instance of <see cref="DataColumnSUKS"/> with given parameters.
      /// </summary>
      /// <param name="metadata">The column <see cref="DataColumnMetaData"/>.</param>
      /// <param name="columnIndex">The index of this column in <see cref="AsyncDataRow"/> it was obtained from.</param>
      /// <param name="previousColumn">The previous <see cref="DataColumnSUKS"/> column of the <see cref="AsyncDataRow"/> this belongs to.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="metadata"/> is <c>null</c>, or if <paramref name="columnIndex"/> is greater than <c>0</c> but <paramref name="previousColumn"/> is <c>null</c>.</exception>
      public DataColumnSUKS(
         DataColumnMetaData metadata,
         Int32 columnIndex,
         AsyncDataColumn previousColumn
         ) : base( metadata, columnIndex )
      {
         if ( columnIndex > 0 )
         {
            ArgumentValidator.ValidateNotNull( nameof( previousColumn ), previousColumn );
         }

         this._totalBytesRead = 0;
         this._byteCount = new ReadOnlyResettableAsyncLazy<Int32>( async () =>
         {
            if ( previousColumn != null )
            {
               await previousColumn.SkipBytesAsync( null );
            }
            return await this.ReadByteCountAsync();
         } );
      }

      /// <summary>
      /// Overrides <see cref="AbstractAsyncDataColumn.PerformReadAsValueAsync"/> to first force all previous columns to be read by calling <see cref="E_UtilPack.SkipBytesAsync(AsyncDataColumn, Byte[])"/> method for previous column, if it was given.
      /// Then, the amount of bytes the data takes is read from the stream by calling <see cref="ReadByteCountAsync"/> method.
      /// If the byte count is greater or equal to <c>0</c>, then the <see cref="ReadValueAsync(int)"/> method is called to read actual value, and that is returned.
      /// Otherwise, <c>null</c> is returned.
      /// </summary>
      /// <returns>A task which on completion will have the value of <see cref="ReadValueAsync(int)"/> or <c>null</c>.</returns>
      /// <remarks>
      /// The values of all previous columns are forced to read because the underlying <see cref="Stream"/> is assumed to be unseekable.
      /// Therefore, if user first tries to get value of e.g. 3rd column, the 1st and 2nd column values must be read before that in order for the stream to be in correct position to read value for 3rd column.
      /// </remarks>
      protected override async ValueTask<Object> PerformReadAsValueAsync()
      {
         var byteCount = await this._byteCount;
         Object retVal;
         if ( byteCount >= 0 )
         {
            retVal = await this.ReadValueAsync( byteCount );
         }
         else
         {
            retVal = null;
         }

         return retVal;
      }

      /// <summary>
      /// Overrides <see cref="AbstractAsyncDataColumn.PerformReadToBytes(byte[], int, int, bool)"/> to first, if <paramref name="isInitialRead"/> is <c>true</c>, force all previous columns to be read by calling <see cref="E_UtilPack.SkipBytesAsync(AsyncDataColumn, Byte[])"/> method for previous column, if it was given.
      /// Then, if <paramref name="isInitialRead"/> is <c>true</c>, the amount of bytes the data takes is read from the stream by calling <see cref="ReadByteCountAsync"/> method.
      /// Otherwise the byte count is what was calculated to remain from previous <see cref="PerformReadToBytes(byte[], int, int, bool)"/>.
      /// If the byte count is greater or equal to <c>0</c>, then the <see cref="ReadValueAsync(int)"/> method is called, and the return value of that is returned.
      /// Otherwise, <c>0</c> is returned.
      /// </summary>
      /// <param name="array">The byte array where to read the data to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing bytes.</param>
      /// <param name="count">The maximum amount of bytes to write.</param>
      /// <param name="isInitialRead">Whether this is first call to <see cref="AbstractAsyncDataColumn.ReadBytesAsync(byte[], int, int)"/> method.</param>
      /// <returns>A task which returns how many bytes has written to <paramref name="array"/>, and whether whole data reading is complete.</returns>
      /// <remarks>
      /// The values of all previous columns are forced to read because the underlying <see cref="Stream"/> is assumed to be unseekable.
      /// Therefore, if user first tries to get value of e.g. 3rd column, the 1st and 2nd column values must be read before that in order for the stream to be in correct position to read value for 3rd column.
      /// </remarks>
      protected override async ValueTask<(Int32 BytesRead, Boolean IsComplete)> PerformReadToBytes( Byte[] array, Int32 offset, Int32 count, Boolean isInitialRead )
      {
         var byteCount = await this._byteCount;
         Int32 retVal;
         if ( byteCount == this._totalBytesRead || byteCount <= 0 )
         {
            // we have encountered EOS
            retVal = 0;
         }
         else
         {
            retVal = await this.DoReadFromStreamAsync( array, offset, Math.Min( count, byteCount - this._totalBytesRead ) );
            Interlocked.Exchange( ref this._totalBytesRead, this._totalBytesRead + retVal );
         }

         return (retVal, this._totalBytesRead >= byteCount);
      }

      /// <summary>
      /// This method is called by both <see cref="PerformReadAsValueAsync"/> and <see cref="PerformReadToBytes"/> methods, to get the amount of bytes that data for this column takes.
      /// </summary>
      /// <returns>A task which should return the amount of bytes that data of this column takes.</returns>
      protected abstract ValueTask<Int32> ReadByteCountAsync();

      /// <summary>
      /// This method is called by <see cref="PerformReadAsValueAsync"/> in order to deserialize the data from underlying <see cref="Stream"/> to actual value.
      /// </summary>
      /// <param name="byteCount">The amount of bytes the data takes, as returned by <see cref="ReadByteCountAsync"/> method.</param>
      /// <returns>A task which should return the deserialized data object.</returns>
      protected abstract ValueTask<Object> ReadValueAsync( Int32 byteCount );

      /// <summary>
      /// This method is called by <see cref="PerformReadToBytes(byte[], int, int, bool)"/> in order to read data as raw bytes from underlying <see cref="Stream"/>.
      /// </summary>
      /// <param name="array">The byte array where to read the data to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing bytes.</param>
      /// <param name="count">The maximum amount of bytes to write.</param>
      /// <returns>The amount of bytes read.</returns>
      /// <remarks>
      /// The <see cref="PerformReadToBytes(byte[], int, int, bool)"/> will take care of putting up correct <paramref name="count"/> value.
      /// </remarks>
      protected abstract ValueTask<Int32> DoReadFromStreamAsync( Byte[] array, Int32 offset, Int32 count );

      /// <summary>
      /// This method overrides <see cref="AbstractAsyncDataColumn.Reset"/> in order to perform resetting the private state of this <see cref="DataColumnSUKS"/> in addition to private state of <see cref="AbstractAsyncDataColumn"/>.
      /// </summary>
      protected override void Reset()
      {
         base.Reset();
         this._byteCount.Reset();
         Interlocked.Exchange( ref this._totalBytesRead, 0 );
      }

   }
}

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// Helper method to call <see cref="AsyncDataColumn.TryGetValueAsync"/> if <paramref name="rawBytes"/> is <c>null</c>, or keep reading raw bytes into <paramref name="rawBytes"/>  using <see cref="AsyncDataColumn.ReadBytesAsync(byte[], int, int)"/> until all required bytes have been read.
   /// </summary>
   /// <param name="stream">This <see cref="AsyncDataColumn"/>.</param>
   /// <param name="rawBytes">The byte array to read to using <see cref="AsyncDataColumn.ReadBytesAsync(byte[], int, int)"/>, or <c>null</c> to use <see cref="AsyncDataColumn.TryGetValueAsync"/> instead.</param>
   /// <returns>A task which will always return <c>true</c> on completion.</returns>
   public static async ValueTask<Boolean> SkipBytesAsync( this AsyncDataColumn stream, Byte[] rawBytes )
   {

      if ( rawBytes == null )
      {
         await stream.TryGetValueAsync();
      }
      else
      {
         while ( ( ( await stream.ReadBytesAsync( rawBytes, 0, rawBytes.Length ) ) ?? 0 ) != 0 ) ;
      }
      return false;
   }
}