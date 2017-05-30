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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace UtilPack
{
   /// <summary>
   /// This interface provides API for functionality which serializes some value to some sink, potentially asynchronously.
   /// </summary>
   /// <typeparam name="TValue">The type of the values to serialize.</typeparam>
   /// <typeparam name="TSink">The type of sink where serialized data is stored to.</typeparam>
   public interface PotentiallyAsyncWriterLogic<in TValue, in TSink>
   {
      /// <summary>
      /// Tries to write the value to serialization sink.
      /// </summary>
      /// <param name="value">The value to serialize.</param>
      /// <param name="sink">The sink to write serialized data to.</param>
      /// <returns>Task which returns amount of units serialized to <paramref name="sink"/>.</returns>
      ValueTask<Int32> TryWriteAsync( TValue value, TSink sink );
   }

   /// <summary>
   /// This class implements <see cref="PotentiallyAsyncWriterLogic{TValue, TSink}"/> to serialize characters to byte stream with given encoding.
   /// </summary>
   public class StreamCharacterWriter : PotentiallyAsyncWriterLogic<IEnumerable<Char>, StreamWriterWithResizableBuffer>
   {
      private const Int32 IDLE = 0;
      private const Int32 BUSY = 1;

      private readonly IEncodingInfo _encoding;
      private readonly Int32 _maxSingleCharSize;
      private readonly Char[] _auxArray;
      private readonly Int32 _maxBufferSize;

      private Int32 _state;

      /// <summary>
      /// Creates new instance of <see cref="StreamCharacterWriter"/> with given parameters.
      /// </summary>
      /// <param name="encoding">The <see cref="IEncodingInfo"/> that will be used to encode characters.</param>
      /// <param name="maxBufferSize">The maximum allowed buffer size that any <see cref="StreamWriterWithResizableBuffer"/> will be allowed to have.</param>
      public StreamCharacterWriter(
         IEncodingInfo encoding,
         Int32 maxBufferSize
         )
      {
         this._encoding = ArgumentValidator.ValidateNotNull( nameof( encoding ), encoding );
         this._maxSingleCharSize = encoding.MaxCharByteCount;
         this._auxArray = new Char[2];
         this._maxBufferSize = Math.Max( maxBufferSize, this._maxSingleCharSize );
      }

      /// <inheritdoc />
      public async ValueTask<Int32> TryWriteAsync( IEnumerable<Char> value, StreamWriterWithResizableBuffer sink )
      {
         if ( Interlocked.CompareExchange( ref this._state, BUSY, IDLE ) == IDLE )
         {
            try
            {
               var total = 0;
               var auxArray = this._auxArray;
               var encoding = this._encoding.Encoding;
               using ( var enumerator = value.GetEnumerator() )
               {
                  while ( enumerator.MoveNext() )
                  {
                     var c = enumerator.Current;
                     auxArray[0] = c;
                     var charCount = 1;
                     if ( Char.IsHighSurrogate( c ) && enumerator.MoveNext() )
                     {
                        // Must read next char
                        auxArray[1] = enumerator.Current;
                        ++charCount;
                     }
                     var count = this._maxSingleCharSize * charCount;
                     if ( sink.ReservedBufferCount + count > this._maxBufferSize )
                     {
                        await sink.FlushAsync();
                     }
                     Int32 offset;
                     (offset, count) = sink.ReserveBufferSegment( count );
                     if ( count > 0 )
                     {
                        var actualCount = encoding.GetBytes( auxArray, 0, charCount, sink.Buffer, offset );
                        total += actualCount;
                        sink.UnreserveBufferSegment( count - actualCount );
                     }
                  }
               }

               if ( total > 0 )
               {
                  await sink.FlushAsync();
               }

               return total;
            }
            finally
            {
               Interlocked.Exchange( ref this._state, IDLE );
            }
         }
         else
         {
            throw BusyException();
         }
      }

      private static InvalidOperationException BusyException()
      {
         return new InvalidOperationException( "This reader is not useable right now." );
      }
   }

   /// <summary>
   /// This interface binds together <see cref="PotentiallyAsyncWriterLogic{TValue, TSource}"/> and some sink, without exposing the sink, and allows <c>in</c> contravariance specification for <typeparamref name="TValue"/>.
   /// </summary>
   /// <typeparam name="TValue">The type of values that this writer can write.</typeparam>
   /// <seealso cref="PotentiallyAsyncWriterObservable{TValue}"/>
   /// <seealso cref="PotentiallyAsyncWriterAndObservable{TValue}"/>
   public interface PotentiallyAsyncWriter<in TValue>
   {
      /// <summary>
      /// This method will try to write given value to the sink bound to this <see cref="PotentiallyAsyncWriter{TValue}"/>.
      /// </summary>
      /// <param name="value">The value to serialize.</param>
      /// <returns>Task which will return amount of units written to sink.</returns>
      ValueTask<Int32> TryWriteAsync( TValue value );
   }

   /// <summary>
   /// This interface exposes event which is raised after each call to <see cref="PotentiallyAsyncWriter{TValue}.TryWriteAsync"/>, and allows <c>out</c> covariance specification for <typeparamref name="TValue"/>.
   /// </summary>
   /// <typeparam name="TValue">The type of values that this writer can write.</typeparam>
   /// <seealso cref="PotentiallyAsyncWriter{TValue}"/>
   /// <seealso cref="PotentiallyAsyncWriterAndObservable{TValue}"/>
   public interface PotentiallyAsyncWriterObservable<out TValue>
   {
      /// <summary>
      /// This event will be triggered after each call to <see cref="PotentiallyAsyncWriter{TValue}.TryWriteAsync"/>.
      /// </summary>
      event GenericEventHandler<WriteCompletedEventArgs<TValue>> WriteCompleted;
   }

   /// <summary>
   /// This interface binds together <see cref="PotentiallyAsyncWriter{TValue}"/> and <see cref="PotentiallyAsyncWriterObservable{TValue}"/>, but loses any variance specifications to <typeparamref name="TValue"/> in doing so.
   /// </summary>
   /// <typeparam name="TValue">The type of values that this writer can write.</typeparam>
   public interface PotentiallyAsyncWriterAndObservable<TValue> : PotentiallyAsyncWriter<TValue>, PotentiallyAsyncWriterObservable<TValue>
   {
   }

   /// <summary>
   /// This is base interface for <see cref="PotentiallyAsyncWriterObservable{TValue}.WriteCompleted"/> containing information that does not require any type parameters.
   /// </summary>
   public interface WriteCompletedEventArgs
   {
      /// <summary>
      /// Gets the amount of units written to sink.
      /// </summary>
      /// <value>The amount of units written to sink.</value>
      Int32 UnitsWritten { get; }
   }

   /// <summary>
   /// This interface augments <see cref="WriteCompletedEventArgs"/> with value type parameter.
   /// </summary>
   /// <typeparam name="TValue">The type of values that can be written.</typeparam>
   public interface WriteCompletedEventArgs<out TValue> : WriteCompletedEventArgs
   {
      /// <summary>
      /// Gets the value that was written to sink.
      /// </summary>
      /// <value>The value that was written to sink.</value>
      TValue Value { get; }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="WriteCompletedEventArgs{TValue}"/>.
   /// </summary>
   /// <typeparam name="TValue">The type of values that can be written.</typeparam>
   public sealed class WriteCompletedEventArgsImpl<TValue> : WriteCompletedEventArgs<TValue>
   {
      /// <summary>
      /// Creates a new instance of <see cref="WriteCompletedEventArgsImpl{TValue}"/> with given parameters.
      /// </summary>
      /// <param name="unitsWritten">The amount of units written in this write.</param>
      /// <param name="value">The value written to sink.</param>
      public WriteCompletedEventArgsImpl(
         Int32 unitsWritten,
         TValue value
         )
      {
         this.UnitsWritten = unitsWritten;
         this.Value = value;
      }

      /// <summary>
      /// Gets the amount of units written to sink.
      /// </summary>
      /// <value>The amount of units written to sink.</value>
      public Int32 UnitsWritten { get; }

      /// <summary>
      /// Gets the value that was written to sink.
      /// </summary>
      /// <value>The value that was written to sink.</value>
      public TValue Value { get; }
   }

   /// <summary>
   /// This class implements <see cref="PotentiallyAsyncWriterAndObservable{TValue}"/> with callback which transforms values to be serialized into values understood by underlying <see cref="PotentiallyAsyncWriterLogic{TValue, TSink}"/>.
   /// </summary>
   /// <typeparam name="TValue">The type of values to be serialized.</typeparam>
   /// <typeparam name="TSink">The type of serialization sink supported by <see cref="PotentiallyAsyncWriterLogic{TValue, TSink}"/>.</typeparam>
   /// <typeparam name="TTransformed">The type of values understood by <see cref="PotentiallyAsyncWriterLogic{TValue, TSink}"/>, which values of type <typeparamref name="TValue"/> can be transformed to.</typeparam>
   public sealed class TransformablePotentiallyAsyncWriter<TValue, TSink, TTransformed> : PotentiallyAsyncWriterAndObservable<TValue>
   {
      private readonly PotentiallyAsyncWriterLogic<TTransformed, TSink> _writer;
      private readonly TSink _sink;
      private readonly Func<TValue, TTransformed> _transformer;

      internal TransformablePotentiallyAsyncWriter(
         PotentiallyAsyncWriterLogic<TTransformed, TSink> writer,
         TSink sink,
         Func<TValue, TTransformed> transformer
         )
      {
         this._writer = ArgumentValidator.ValidateNotNull( nameof( writer ), writer );
         this._sink = sink;
         this._transformer = ArgumentValidator.ValidateNotNull( nameof( transformer ), transformer );
      }

      /// <inheritdoc />
      public async ValueTask<Int32> TryWriteAsync( TValue value )
      {
         var retVal = await this._writer.TryWriteAsync( this._transformer( value ), this._sink );
         this.WriteCompleted?.Invoke( new WriteCompletedEventArgsImpl<TValue>( retVal, value ) );
         return retVal;
      }

      /// <inheritdoc />
      public event GenericEventHandler<WriteCompletedEventArgs<TValue>> WriteCompleted;
   }

   /// <summary>
   /// This class provides methods to create instances of various <see cref="PotentiallyAsyncWriterAndObservable{TValue}"/>.
   /// </summary>
   public static class WriterFactory
   {
      /// <summary>
      /// Creates new <see cref="PotentiallyAsyncWriterAndObservable{TValue}"/> which transforms the values given to <see cref="PotentiallyAsyncWriter{TValue}.TryWriteAsync"/> to values understood by underlying <see cref="PotentiallyAsyncWriterLogic{TValue, TSink}"/>.
      /// </summary>
      /// <param name="writer">The underlying <see cref="PotentiallyAsyncWriterLogic{TValue, TSink}"/> logic.</param>
      /// <param name="sink">The sink to write to.</param>
      /// <param name="transformer">The callback which should transform values of <typeparamref name="TValue"/> into values of type <typeparamref name="TTransformed"/>, understood by <paramref name="writer"/>.</param>
      /// <typeparam name="TValue">Type of values to serialize.</typeparam>
      /// <typeparam name="TSink">Type of sink to serialize to.</typeparam>
      /// <typeparam name="TTransformed">The type of values understood by <paramref name="writer"/>.</typeparam>
      public static PotentiallyAsyncWriterAndObservable<TValue> CreateTransformableWriter<TValue, TSink, TTransformed>(
         PotentiallyAsyncWriterLogic<TTransformed, TSink> writer,
         TSink sink,
         Func<TValue, TTransformed> transformer
         )
      {
         return new TransformablePotentiallyAsyncWriter<TValue, TSink, TTransformed>( writer, sink, transformer );
      }
   }
}