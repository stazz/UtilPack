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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Reflection;
using UtilPack;
using UtilPack.TabularData;

namespace UtilPack.TabularData
{
   /// <summary>
   /// This interface can be thought as one entrypoint for this library, and it presents a single row in a data which can be represented as a set of rows, each with a set of columns.
   /// The design of this library does not constraint that each of those rows should have same amount of columns, therefore all information about the columns is available through this interface.
   /// The <see cref="AsyncDataRow"/> interface and synchronous version of it still in development, both extend this interface.
   /// </summary>
   /// <seealso cref="AsyncDataRow"/>
   public interface DataRow<out TDataColumn, out TDataColumnMetaData>
      where TDataColumn : AbstractDataColumn
      where TDataColumnMetaData : DataColumnMetaData
   {
      /// <summary>
      /// Gets the <see cref="AsyncDataColumn"/> at given index.
      /// </summary>
      /// <param name="index">The index. Typically is zero-based, but depending on a context, may not be.</param>
      /// <returns>An instance of <see cref="AsyncDataColumn"/> containing information about the column of current <see cref="AsyncDataRow"/>.</returns>
      /// <exception cref="ArgumentException">If given <paramref name="index"/> was not in a valid range.</exception>
      TDataColumn GetColumn( Int32 index );

      /// <summary>
      /// Gets the <see cref="DataRowMetaData"/> object describing the columns and data of each column for this <see cref="AsyncDataRow"/>.
      /// </summary>
      /// <value>The <see cref="DataRowMetaData"/> object describing the columns and data of each column for this <see cref="AsyncDataRow"/>.</value>
      DataRowMetaData<TDataColumnMetaData> Metadata { get; }
   }

   /// <summary>
   /// This interface specializes <see cref="DataRow{TDataColumn, TDataColumnMetaData}"/> interface to constrain this to have <see cref="AsyncDataColumn"/> column objects.
   /// </summary>
   /// <remarks>
   /// Since the data is potentially coming outside this process, and might need IO processing, the API is fully asynchronous.
   /// </remarks>
   public interface AsyncDataRow : DataRow<AsyncDataColumn, AsyncDataColumnMetaData>
   {
   }

   /// <summary>
   /// This interface provides properties related to <see cref="DataRowMetaData{TColumnMetaData}"/> but that do not require generic type parameters.
   /// </summary>
   public interface DataRowMetaData
   {
      /// <summary>
      /// Gets the amount of columns of the <see cref="AsyncDataRow"/> this <see cref="DataRowMetaData"/> was obtained from.
      /// </summary>
      /// <value>The amount of columns of the <see cref="AsyncDataRow"/> this <see cref="DataRowMetaData"/> was obtained from.</value>
      Int32 ColumnCount { get; }

      /// <summary>
      /// Tries to get the index for labeled column.
      /// </summary>
      /// <param name="columnName">The label for column.</param>
      /// <returns>The index for given column label, or <c>null</c> if this row has no column with given label.</returns>
      Int32? GetIndexFor( String columnName );
   }

   /// <summary>
   /// This interface provides API to query information about the structure of the <see cref="AsyncDataRow"/> object, and the data that the <see cref="AsyncDataColumn"/> objects contain.
   /// </summary>
   /// <typeparam name="TColumnMetaData">The actual type of column metadata, must be subtype of <see cref="DataColumnMetaData"/>.</typeparam>
   public interface DataRowMetaData<out TColumnMetaData> : DataRowMetaData
      where TColumnMetaData : DataColumnMetaData
   {
      /// <summary>
      /// Gets the <see cref="DataColumnMetaData"/> for column at given index.
      /// </summary>
      /// <param name="columnIndex">The column index.</param>
      /// <returns>An instance of <see cref="DataColumnMetaData"/> describing the data in given column.</returns>
      /// <exception cref="ArgumentException">If no column exists for given <paramref name="columnIndex"/>.</exception>
      TColumnMetaData GetColumnMetaData( Int32 columnIndex );
   }

   /// <summary>
   /// This interface provides API to query information about the data contained in single <see cref="AsyncDataColumn"/> object.
   /// </summary>
   public interface DataColumnMetaData
   {
      /// <summary>
      /// Gets the <see cref="Type"/> of the data contained in the <see cref="AsyncDataColumn"/> this <see cref="DataColumnMetaData"/> was obtained from.
      /// </summary>
      /// <value>The <see cref="Type"/> of the data contained in the <see cref="AsyncDataColumn"/> this <see cref="DataColumnMetaData"/> was obtained from.</value>
      Type ColumnCLRType { get; }

      /// <summary>
      /// Tries to perform conversion of the column value to given target type.
      /// </summary>
      /// <param name="value">The data that was acquired from <see cref="AsyncDataColumn"/> this <see cref="DataColumnMetaData"/> was obtained from.</param>
      /// <param name="targetType">The type to transform the <paramref name="value"/> to.</param>
      /// <returns>An object that is assignalbe to <paramref name="targetType"/>.</returns>
      /// <remarks>
      /// If the cast is invalid, an exception should be thrown.
      /// This interface does not specify which exception should be then thrown.
      /// </remarks>
      Object ChangeType( Object value, Type targetType );

      /// <summary>
      /// Gets the possible column label for the <see cref="AsyncDataColumn"/> this <see cref="DataColumnMetaData"/> was obtained from.
      /// </summary>
      /// <value>The possible column label for the <see cref="AsyncDataColumn"/> this <see cref="DataColumnMetaData"/> was obtained from.</value>
      String Label { get; }

   }

   /// <summary>
   /// This interface extends <see cref="DataColumnMetaData"/> to provide functionality which is specific for <see cref="AsyncDataColumn"/>.
   /// </summary>
   public interface AsyncDataColumnMetaData : DataColumnMetaData
   {

      /// <summary>
      /// Tries to asynchronously convert the raw byte data into .NET data object.
      /// </summary>
      /// <param name="stream">The stream containing raw byte data representation.</param>
      /// <param name="byteCount">The amount of bytes to read from the stream.</param>
      /// <returns>A task which on completion will have .NET data object.</returns>
      /// <remarks>
      /// If data is malformed, an exception should be thrown.
      /// </remarks>
      ValueTask<Object> ConvertFromBytesAsync( Stream stream, Int32 byteCount );
   }

   /// <summary>
   /// This interface provides querying data of single column of single <see cref="AsyncDataRow"/> object.
   /// </summary>
   public interface AsyncDataColumn : AbstractDataColumn
   {
      /// <summary>
      /// Tries asynchronously to get the data of this <see cref="AsyncDataColumn"/>.
      /// </summary>
      /// <returns>A task which will on completion contain <see cref="ResultOrNone{TResult}"/> struct describing the data.</returns>
      /// <seealso cref="ResultOrNone{TResult}"/>
      /// <remarks>
      /// If value reading has already been started by <see cref="ReadBytesAsync(byte[], int, int)"/> method and not yet finished, this method will return <see cref="ResultOrNone{TResult}"/> such that its <see cref="ResultOrNone{TResult}.HasResult"/> property is <c>false</c>.
      /// If value has already been previously read by this method, it is returned as <see cref="ValueTask{TResult}"/> synchronously.
      /// </remarks>
      ValueTask<ResultOrNone<Object>> TryGetValueAsync();

      // The problem is that we might do some complex casting which most likely results in heap allocations anyway.
      // OR an extremely complex implementation (since we can't cache value into "Object" since that would cause heap allocation).
      // TODO investigate into this later.
      ///// <summary>
      ///// This method is like <see cref="TryGetValueAsync"/> except it allows directly specifying the expected type of value at compile time, thus possibly avoiding extra heap allocations.
      ///// </summary>
      ///// <returns>A task which will on completion contain <see cref="ResultOrNone{TResult}"/> struct describing the data.</returns>
      ///// <seealso cref="ResultOrNone{TResult}"/>
      ///// <remarks>
      ///// If value reading has already been started by <see cref="TryReadBytesAsync(byte[], int, int)"/> method and not yet finished, this method will return <see cref="ResultOrNone{TResult}"/> such that its <see cref="ResultOrNone{TResult}.HasResult"/> property is <c>false</c>.
      ///// </remarks>
      //ValueTask<ResultOrNone<T>> TryGetValueAsync<T>();

      /// <summary>
      /// Tries to asynchronously read data as raw bytes into given byte array.
      /// </summary>
      /// <param name="array">The byte array where to read the data to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing bytes.</param>
      /// <param name="count">The maximum amount of bytes to write.</param>
      /// <returns>Amount of bytes written to array, or <c>-1</c> if <see cref="TryGetValueAsync"/> has been invoked concurrently, or <c>0</c> if end of data has been encountered, or <c>null</c> if this method has been invoked concurrently.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="count"/> is less than <c>0</c>, or if array length is smaller than <paramref name="offset"/> <c>+</c> <paramref name="count"/>.</exception>
      /// <exception cref="ArgumentException">If <paramref name="offset"/> + <paramref name="count"/> is greater than array length.</exception>
      ValueTask<Int32?> ReadBytesAsync( Byte[] array, Int32 offset, Int32 count );
   }

   /// <summary>
   /// This is common interface for <see cref="AsyncDataColumn"/> and synchronous data column interface, which is still under development.
   /// </summary>
   public interface AbstractDataColumn
   {
      /// <summary>
      /// Gets the <see cref="DataColumnMetaData"/> of this <see cref="AsyncDataColumn"/>.
      /// </summary>
      /// <value>The <see cref="DataColumnMetaData"/> of this <see cref="AsyncDataColumn"/>.</value>
      DataColumnMetaData MetaData { get; }

      /// <summary>
      /// Gets the index of this <see cref="AsyncDataColumn"/> in <see cref="AsyncDataRow"/> this was obtained from.
      /// </summary>
      /// <value>The index of this <see cref="AsyncDataColumn"/> in <see cref="AsyncDataRow"/> this was obtained from.</value>
      Int32 ColumnIndex { get; }
   }
}

/// <summary>
/// This class contains extensions methods for types defined in this library.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// Helper method to asynchronously get value from this <see cref="AsyncDataRow"/>, and cast the value to given type.
   /// </summary>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="index">The index of column holding the value.</param>
   /// <param name="type">The type to cast value to.</param>
   /// <returns>A task which will on completion contain casted value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   public static async ValueTask<Object> GetValueAsync( this AsyncDataRow row, Int32 index, Type type )
   {
      return await row.GetColumn( index ).GetValueAsync( type );
   }

   /// <summary>
   /// Helper method to asynchronously get value from this <see cref="AsyncDataRow"/>, and cast the value to given type., which is known at compile time.
   /// </summary>
   /// <typeparam name="T">The type to cast value to.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="index">The index of column holding the value.</param>
   /// <returns>A task which will on completion contain casted value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   public static async ValueTask<T> GetValueAsync<T>( this AsyncDataRow row, Int32 index )
   {
      return (T) ( await row.GetValueAsync( index, typeof( T ) ) );
   }

   /// <summary>
   /// Helper method to asynchronously get value as is from this <see cref="AsyncDataRow"/>, and the value as <see cref="Object"/>.
   /// </summary>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="index">The index of column holding the value.</param>
   /// <returns>A task which will on completion contain casted value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   public static async ValueTask<Object> GetValueAsObjectAsync( this AsyncDataRow row, Int32 index )
   {
      return await row.GetValueAsync( index, typeof( Object ) );
   }

   /// <summary>
   /// Helper method to asynchronously try to get value and cast it to given type, or throw an exception if value can not be fetched or cast is invalid.
   /// </summary>
   /// <param name="column">This <see cref="AsyncDataColumn"/>.</param>
   /// <param name="type">The type to cast value to.</param>
   /// <returns>A task which will on completion contain casted value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataColumn"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">If fetching value fails (<see cref="AsyncDataColumn.TryGetValueAsync"/> method returns such <see cref="ResultOrNone{TResult}"/> that its <see cref="ResultOrNone{TResult}.HasResult"/> property is <c>false</c>).</exception>
   public static async ValueTask<Object> GetValueAsync( this AsyncDataColumn column, Type type )
   {
      var retValOrNone = await column.TryGetValueAsync();

      Object retVal;
      if ( retValOrNone.HasResult )
      {
         retVal = retValOrNone.Result;
         if ( retVal != null && !type.GetTypeInfo().IsAssignableFrom( retVal.GetType().GetTypeInfo() ) )
         {
            retVal = column.MetaData.ChangeType( retVal, type );
         }
      }
      else
      {
         throw new InvalidOperationException( $"No value for index {column.ColumnIndex}." );
      }

      return retVal;
   }

   /// <summary>
   /// Helper method to asynchronously try to get value and cast it to given type, which is known at compile time, or throw an exception if value can not be fetched or cast is invalid.
   /// </summary>
   /// <typeparam name="T">The type to cast value to.</typeparam>
   /// <param name="column">This <see cref="AsyncDataColumn"/>.</param>
   /// <returns>A task which will on completion contain casted value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataColumn"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">If fetching value fails (<see cref="AsyncDataColumn.TryGetValueAsync"/> method returns such <see cref="ResultOrNone{TResult}"/> that its <see cref="ResultOrNone{TResult}.HasResult"/> property is <c>false</c>).</exception>
   public static async ValueTask<T> GetValueAsync<T>( this AsyncDataColumn column )
   {
      return (T) ( await column.GetValueAsync( typeof( T ) ) );
   }

   /// <summary>
   /// Helper method to asynchronously try to get value as is and return it as <see cref="Object"/>, or throw an exception if value can not be fetched or cast is invalid.
   /// </summary>
   /// <param name="column">This <see cref="AsyncDataColumn"/>.</param>
   /// <returns>A task which will on completion contain casted value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataColumn"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">If fetching value fails (<see cref="AsyncDataColumn.TryGetValueAsync"/> method returns such <see cref="ResultOrNone{TResult}"/> that its <see cref="ResultOrNone{TResult}.HasResult"/> property is <c>false</c>).</exception>
   public static ValueTask<Object> GetValueAsObjectAsync( this AsyncDataColumn column )
   {
      return column.GetValueAsync( typeof( Object ) );
   }

   /// <summary>
   /// Helper method to asynchronously get value from this <see cref="AsyncDataRow"/>, and cast the value to given type.
   /// </summary>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="columnLabel">The label of column holding the value.</param>
   /// <param name="type">The type to cast value to.</param>
   /// <returns>A task which will on completion contain casted value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If column labeled as <paramref name="columnLabel"/> is not present in this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<Object> GetValueAsync( this AsyncDataRow row, String columnLabel, Type type )
   {
      return await row.GetColumn( row.Metadata.GetIndexOrThrow( columnLabel ) ).GetValueAsync( type );
   }

   /// <summary>
   /// Helper method to asynchronously get value from this <see cref="AsyncDataRow"/>, and cast the value to given type., which is known at compile time.
   /// </summary>
   /// <typeparam name="T">The type to cast value to.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="columnLabel">The label of column holding the value.</param>
   /// <returns>A task which will on completion contain casted value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If column labeled as <paramref name="columnLabel"/> is not present in this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<T> GetValueAsync<T>( this AsyncDataRow row, String columnLabel )
   {
      return (T) ( await row.GetValueAsync( row.Metadata.GetIndexOrThrow( columnLabel ), typeof( T ) ) );
   }

   /// <summary>
   /// Helper method to asynchronously get value as is from this <see cref="AsyncDataRow"/>, and the value as <see cref="Object"/>.
   /// </summary>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="columnLabel">The label of column holding the value.</param>
   /// <returns>A task which will on completion contain casted value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If column labeled as <paramref name="columnLabel"/> is not present in this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<Object> GetValueAsObjectAsync( this AsyncDataRow row, String columnLabel )
   {
      return await row.GetValueAsync( row.Metadata.GetIndexOrThrow( columnLabel ), typeof( Object ) );
   }

   /// <summary>
   /// Helper method to try to get index for given column label, or throw an exception.
   /// </summary>
   /// <typeparam name="TColumnMetaData">The type of column meta data.</typeparam>
   /// <param name="rowMD">This <see cref="DataRowMetaData{TColumnMetaData}"/>.</param>
   /// <param name="columnLabel">The label of the column.</param>
   /// <returns>An index for column with given label.</returns>
   /// <exception cref="ArgumentException">If <paramref name="columnLabel"/> is <c>null</c> or if column with such label is not present in row this <see cref="DataRowMetaData{TColumnMetaData}"/> was obtained from.</exception>
   public static Int32 GetIndexOrThrow<TColumnMetaData>( this DataRowMetaData<TColumnMetaData> rowMD, String columnLabel )
      where TColumnMetaData : DataColumnMetaData
   {
      return rowMD.GetIndexFor( columnLabel ) ?? throw new ArgumentException( $"No column labeled \"{columnLabel}\"." );
   }

   /// <summary>
   /// Asynchronously retrieves 1 value from this <see cref="AsyncDataRow"/> and transforms it into a <see cref="ValueTuple{T1}"/>.
   /// </summary>
   /// <typeparam name="T1">The type of the value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="index">The index at which to get the value.</param>
   /// <returns>Potentially asynchronously returns a <see cref="ValueTuple{T1}"/> containing the retrieved value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the <paramref name="index"/> is out of range for this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<ValueTuple<T1>> TransformToTuple<T1>( this AsyncDataRow row, Int32 index = 0 )
   {
      return new ValueTuple<T1>( await row.GetValueAsync<T1>( index ) );
   }

   /// <summary>
   /// Asynchronously retrieves 2 subsequent values from this <see cref="AsyncDataRow"/> and transforms them into a <see cref="ValueTuple{T1,T2}"/>.
   /// </summary>
   /// <typeparam name="T1">The type of the first value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T2">The type of the second value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="startIndex">The index at which to get the first value. The second value is retrieved from subsequent index (<paramref name="startIndex"/> + 1).</param>
   /// <returns>Potentially asynchronously returns a <see cref="ValueTuple{T1,T2}"/> containing the retrieved value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If any of the value indices is out of range for this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<ValueTuple<T1, T2>> TransformToTuple<T1, T2>( this AsyncDataRow row, Int32 startIndex = 0 )
   {
      return (
         await row.GetValueAsync<T1>( startIndex ),
         await row.GetValueAsync<T2>( startIndex + 1 )
         );
   }

   /// <summary>
   /// Asynchronously retrieves 3 subsequent values from this <see cref="AsyncDataRow"/> and transforms them into a <see cref="ValueTuple{T1,T2,T3}"/>.
   /// </summary>
   /// <typeparam name="T1">The type of the first value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T2">The type of the second value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T3">The type of the third value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="startIndex">The index at which to get the first value. The rest of the values are retrieved from subsequent indices.</param>
   /// <returns>Potentially asynchronously returns a <see cref="ValueTuple{T1,T2,T3}"/> containing the retrieved value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If any of the value indices is out of range for this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<ValueTuple<T1, T2, T3>> TransformToTuple<T1, T2, T3>( this AsyncDataRow row, Int32 startIndex = 0 )
   {
      return (
         await row.GetValueAsync<T1>( startIndex ),
         await row.GetValueAsync<T2>( startIndex + 1 ),
         await row.GetValueAsync<T3>( startIndex + 2 )
         );
   }

   /// <summary>
   /// Asynchronously retrieves 4 subsequent values from this <see cref="AsyncDataRow"/> and transforms them into a <see cref="ValueTuple{T1,T2,T3,T4}"/>.
   /// </summary>
   /// <typeparam name="T1">The type of the first value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T2">The type of the second value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T3">The type of the third value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T4">The type of the fourth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="startIndex">The index at which to get the first value. The rest of the values are retrieved from subsequent indices.</param>
   /// <returns>Potentially asynchronously returns a <see cref="ValueTuple{T1,T2,T3,T4}"/> containing the retrieved value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If any of the value indices is out of range for this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<ValueTuple<T1, T2, T3, T4>> TransformToTuple<T1, T2, T3, T4>( this AsyncDataRow row, Int32 startIndex = 0 )
   {
      return (
         await row.GetValueAsync<T1>( startIndex ),
         await row.GetValueAsync<T2>( startIndex + 1 ),
         await row.GetValueAsync<T3>( startIndex + 2 ),
         await row.GetValueAsync<T4>( startIndex + 3 )
         );
   }

   /// <summary>
   /// Asynchronously retrieves 5 subsequent values from this <see cref="AsyncDataRow"/> and transforms them into a <see cref="ValueTuple{T1,T2,T3,T4,T5}"/>.
   /// </summary>
   /// <typeparam name="T1">The type of the first value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T2">The type of the second value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T3">The type of the third value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T4">The type of the fourth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T5">The type of the fifth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="startIndex">The index at which to get the first value. The rest of the values are retrieved from subsequent indices.</param>
   /// <returns>Potentially asynchronously returns a <see cref="ValueTuple{T1,T2,T3,T4,T5}"/> containing the retrieved value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If any of the value indices is out of range for this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<ValueTuple<T1, T2, T3, T4, T5>> TransformToTuple<T1, T2, T3, T4, T5>( this AsyncDataRow row, Int32 startIndex = 0 )
   {
      return (
         await row.GetValueAsync<T1>( startIndex ),
         await row.GetValueAsync<T2>( startIndex + 1 ),
         await row.GetValueAsync<T3>( startIndex + 2 ),
         await row.GetValueAsync<T4>( startIndex + 3 ),
         await row.GetValueAsync<T5>( startIndex + 4 )
         );
   }

   /// <summary>
   /// Asynchronously retrieves 6 subsequent values from this <see cref="AsyncDataRow"/> and transforms them into a <see cref="ValueTuple{T1,T2,T3,T4,T5,T6}"/>.
   /// </summary>
   /// <typeparam name="T1">The type of the first value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T2">The type of the second value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T3">The type of the third value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T4">The type of the fourth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T5">The type of the fifth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T6">The type of the sixth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="startIndex">The index at which to get the first value. The rest of the values are retrieved from subsequent indices.</param>
   /// <returns>Potentially asynchronously returns a <see cref="ValueTuple{T1,T2,T3,T4,T5,T6}"/> containing the retrieved value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If any of the value indices is out of range for this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<ValueTuple<T1, T2, T3, T4, T5, T6>> TransformToTuple<T1, T2, T3, T4, T5, T6>( this AsyncDataRow row, Int32 startIndex = 0 )
   {
      return (
         await row.GetValueAsync<T1>( startIndex ),
         await row.GetValueAsync<T2>( startIndex + 1 ),
         await row.GetValueAsync<T3>( startIndex + 2 ),
         await row.GetValueAsync<T4>( startIndex + 3 ),
         await row.GetValueAsync<T5>( startIndex + 4 ),
         await row.GetValueAsync<T6>( startIndex + 5 )
         );
   }

   /// <summary>
   /// Asynchronously retrieves 7 subsequent values from this <see cref="AsyncDataRow"/> and transforms them into a <see cref="ValueTuple{T1,T2,T3,T4,T5,T6,T7}"/>.
   /// </summary>
   /// <typeparam name="T1">The type of the first value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T2">The type of the second value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T3">The type of the third value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T4">The type of the fourth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T5">The type of the fifth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T6">The type of the sixth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T7">The type of the seventh value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="startIndex">The index at which to get the first value. The rest of the values are retrieved from subsequent indices.</param>
   /// <returns>Potentially asynchronously returns a <see cref="ValueTuple{T1,T2,T3,T4,T5,T6,T7}"/> containing the retrieved value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If any of the value indices is out of range for this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> TransformToTuple<T1, T2, T3, T4, T5, T6, T7>( this AsyncDataRow row, Int32 startIndex = 0 )
   {
      return (
         await row.GetValueAsync<T1>( startIndex ),
         await row.GetValueAsync<T2>( startIndex + 1 ),
         await row.GetValueAsync<T3>( startIndex + 2 ),
         await row.GetValueAsync<T4>( startIndex + 3 ),
         await row.GetValueAsync<T5>( startIndex + 4 ),
         await row.GetValueAsync<T6>( startIndex + 5 ),
         await row.GetValueAsync<T7>( startIndex + 6 )
         );
   }

   /// <summary>
   /// Asynchronously retrieves 8 or more subsequent values from this <see cref="AsyncDataRow"/> and transforms them into a <see cref="ValueTuple{T1,T2,T3,T4,T5,T6,T7,TRest}"/>.
   /// </summary>
   /// <typeparam name="T1">The type of the first value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T2">The type of the second value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T3">The type of the third value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T4">The type of the fourth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T5">The type of the fifth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T6">The type of the sixth value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="T7">The type of the seventh value to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <typeparam name="TRest">The type of the rest of the values to get from this <see cref="AsyncDataRow"/>.</typeparam>
   /// <param name="row">This <see cref="AsyncDataRow"/>.</param>
   /// <param name="startIndex">The index at which to get the first value. The rest of the values are retrieved from subsequent indices.</param>
   /// <returns>Potentially asynchronously returns a <see cref="ValueTuple{T1,T2,T3,T4,T5,T6,T7,TRest}"/> containing the retrieved value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncDataRow"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If any of the value indices is out of range for this <see cref="AsyncDataRow"/>.</exception>
   public static async ValueTask<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>> TransformToTuple<T1, T2, T3, T4, T5, T6, T7, TRest>( this AsyncDataRow row, Int32 startIndex = 0 )
      where TRest : struct
   {
      return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
         await row.GetValueAsync<T1>( startIndex ),
         await row.GetValueAsync<T2>( startIndex + 1 ),
         await row.GetValueAsync<T3>( startIndex + 2 ),
         await row.GetValueAsync<T4>( startIndex + 3 ),
         await row.GetValueAsync<T5>( startIndex + 4 ),
         await row.GetValueAsync<T6>( startIndex + 5 ),
         await row.GetValueAsync<T7>( startIndex + 6 ),
         await row.GetValueAsync<TRest>( startIndex + 7 )
         );
   }

}
