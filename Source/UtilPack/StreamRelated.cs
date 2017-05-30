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
using UtilPack;

namespace UtilPack
{
   /// <summary>
   /// This is base interface for objects which encapsulate a <see cref="Stream"/> along with growable buffer.
   /// </summary>
   public interface AbstractStreamWithResizableBuffer
   {
      /// <summary>
      /// Do not cache instances of the return value of this property!
      /// Once System.Span &amp; Co will arrive, this property will become System.Span or maybe System.Buffer or maybe System.Memory.
      /// </summary>
      Byte[] Buffer { get; }

      /// <summary>
      /// Gets the <see cref="System.Threading.CancellationToken"/> used for async operations of the underlying <see cref="Stream"/>.
      /// </summary>
      /// <value>The <see cref="System.Threading.CancellationToken"/> used for async operations of the underlying <see cref="Stream"/>.</value>
      CancellationToken CancellationToken { get; }
   }

   /// <summary>
   /// This interface provides methods to read from the underlying <see cref="Stream"/>.
   /// The basic principle is that there is a marker index (accessible by <see cref="ReadBytesCount"/>), which can be expanded by <see cref="TryReadMoreAsync(int)"/> method.
   /// The <see cref="TryReadAsync(int)"/> method will discard any bytes previously read into buffer and read the next bytes starting from <c>0</c> index.
   /// The amount of useable bytes of <see cref="AbstractStreamWithResizableBuffer.Buffer"/> will always be <see cref="ReadBytesCount"/>, starting from <c>0</c> index of <see cref="AbstractStreamWithResizableBuffer.Buffer"/>.
   /// </summary>
   /// <remarks>
   /// The default implementation <see cref="StreamReaderWithResizableBufferImpl"/> will read underlying <see cref="Stream"/> in chunks, therefore the return types of <see cref="TryReadAsync(int)"/> and <see cref="TryReadMoreAsync(int)"/> are <see cref="ValueTask{TResult}"/>, since asynchrony will happen only when it is actually needed to read from stream.
   /// <see cref="StreamFactory"/> class provides methods to create objects implementing this interface.
   /// </remarks>
   public interface StreamReaderWithResizableBuffer : AbstractStreamWithResizableBuffer
   {
      /// <summary>
      /// Discards previously read bytes that have been read into buffer using <see cref="EraseReadBytesFromBuffer"/>, and tries to read more bytes from the stream.
      /// After the returned task completes, the following condition will apply: <c>0 ≤ </c><see cref="ReadBytesCount"/><c> ≤ </c><paramref name="amount"/>.
      /// </summary>
      /// <param name="amount">The amount of bytes to read.</param>
      /// <returns>The task which will indicate whether given amount of bytes has been read.</returns>
      /// <exception cref="InvalidOperationException">If this stream reader is currently unusable (concurrent read or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      ValueTask<Boolean> TryReadAsync( Int32 amount );

      /// <summary>
      /// Tries to read more bytes from the stream into buffer, placing them after current <see cref="ReadBytesCount"/>.
      /// After the returned task completes, the following condition will apply (x = value of <see cref="ReadBytesCount"/> when this method was called):<c>x ≤ </c><see cref="ReadBytesCount"/><c> ≤ x + </c><paramref name="amount"/>.
      /// </summary>
      /// <param name="amount">The amount of bytes to read.</param>
      /// <returns>The task which will indicate whether given amount of bytes has been read.</returns>
      /// <exception cref="InvalidOperationException">If this stream reader is currently unusable (concurrent read or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      ValueTask<Boolean> TryReadMoreAsync( Int32 amount );

      /// <summary>
      /// Creates <see cref="InnerStreamReaderWithResizableBufferAndLimitedSize"/> to be used to read the next <paramref name="byteCount"/> bytes.
      /// If creation is successful, the read bytes will be erased using <see cref="EraseReadBytesFromBuffer"/> before returning the <see cref="InnerStreamReaderWithResizableBufferAndLimitedSize"/>.
      /// This <see cref="StreamReaderWithResizableBuffer"/> will become unusable until the <see cref="IDisposable.Dispose"/> method is called on returned <see cref="InnerStreamReaderWithResizableBufferAndLimitedSize"/>.
      /// </summary>
      /// <param name="byteCount">The amount of bytes to reserve to returned <see cref="InnerStreamReaderWithResizableBufferAndLimitedSize"/>.</param>
      /// <returns>The <see cref="InnerStreamReaderWithResizableBufferAndLimitedSize"/> with <c>0 ≤ </c><see cref="LimitedSizeInfo.TotalByteCount"/><c> ≤ </c><paramref name="byteCount"/>.</returns>
      /// <exception cref="InvalidOperationException">If this stream reader is currently unusable (concurrent read or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      InnerStreamReaderWithResizableBufferAndLimitedSize CreateWithLimitedSizeAndSharedBuffer( Int64 byteCount );

      /// <summary>
      /// Gets the amount of useable bytes in <see cref="AbstractStreamWithResizableBuffer.Buffer"/>.
      /// </summary>
      /// <value>The amount of useable bytes in <see cref="AbstractStreamWithResizableBuffer.Buffer"/>.</value>
      Int32 ReadBytesCount { get; }

      /// <summary>
      /// Erases all read bytes.
      /// When this method completes, the <see cref="ReadBytesCount"/> will be <c>0</c>.
      /// </summary>
      /// <exception cref="InvalidOperationException">If this stream reader is currently unusable (concurrent read or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      void EraseReadBytesFromBuffer();

      /// <summary>
      /// Erases a specific segment in read bytes.
      /// When this method completes, the <see cref="ReadBytesCount"/> will be decremented by <paramref name="count"/>.
      /// </summary>
      /// <param name="firstDeletableByteIndex">The index of the first byte to be deleted.</param>
      /// <param name="count">The amount of bytes to delete.</param>
      /// <exception cref="InvalidOperationException">If this stream reader is currently unusable (concurrent read or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      /// <remarks>
      /// This method will do nothing if parameters are invalid in some way (e.g. out of range).
      /// </remarks>
      void EraseReadBufferSegment( Int32 firstDeletableByteIndex, Int32 count );

      /// <summary>
      /// Moves read marker backwards without erasing bytes.
      /// When this method completes, the <see cref="ReadBytesCount"/> will be decremented by <paramref name="amount"/>, or will be <c>0</c> if <paramref name="amount"/> is negative.
      /// </summary>
      /// <param name="amount">The amount of bytes to move marker backwards. If negative, then the marker will be moved to start.</param>
      /// <exception cref="InvalidOperationException">If this stream reader is currently unusable (concurrent read or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      void UnreadBytes( Int32 amount = -1 );

      /// <summary>
      /// Gets amount of bytes that is used as starting point when determining how many bytes to read from underlying stream for one call of <see cref="TryReadAsync(int)"/> or <see cref="TryReadMoreAsync(int)"/>.
      /// </summary>
      /// <value>The amount of bytes that is used as starting point when determining how many bytes to read from underlying stream.</value>
      Int32 ChunkSize { get; }
   }

   /// <summary>
   /// This is common interface for stream readers and writers with limited size, <see cref="StreamReaderWithResizableBufferAndLimitedSize"/> and <see cref="StreamWriterWithResizableBufferAndLimitedSize"/>.
   /// </summary>
   public interface LimitedSizeInfo
   {
      /// <summary>
      /// Gets the initial byte count.
      /// </summary>
      /// <value>The initial byte count.</value>
      Int64 TotalByteCount { get; }

      /// <summary>
      /// Gets the value indicating how many bytes are left for operations of this stream reader or writer.
      /// </summary>
      /// <value>The value indicating how many bytes are left for operations of this stream reader or writer.</value>
      Int64 BytesLeft { get; }
   }

   /// <summary>
   /// This interface further specializes <see cref="StreamReaderWithResizableBuffer"/> introducing limitation on how many bytes can be read from the underlying <see cref="Stream"/>.
   /// </summary>
   /// <remarks>
   /// <see cref="StreamFactory"/> class provides methods to create objects implementing this interface.
   /// </remarks>
   public interface StreamReaderWithResizableBufferAndLimitedSize : StreamReaderWithResizableBuffer, LimitedSizeInfo
   {
   }

   /// <summary>
   /// This interface augments <see cref="StreamReaderWithResizableBufferAndLimitedSize"/> interface with <see cref="IDisposable.Dispose"/> method from <see cref="IDisposable"/>.
   /// By calling the <see cref="IDisposable.Dispose"/> method, the <see cref="StreamReaderWithResizableBuffer"/> which created this <see cref="InnerStreamReaderWithResizableBufferAndLimitedSize"/> will become useable again.
   /// </summary>
   public interface InnerStreamReaderWithResizableBufferAndLimitedSize : StreamReaderWithResizableBufferAndLimitedSize, IDisposable
   {
   }

   /// <summary>
   /// This interface provides methods to write to the underlying <see cref="Stream"/>.
   /// The buffer segments are reserved using <see cref="ReserveBufferSegment"/> method, or bytes can appended to buffer right awayt with <see cref="E_UtilPack.AppendToBytes"/> helper method. Once appended to buffer, the bytes are written to underlying stream using <see cref="FlushAsync"/> method.
   /// </summary>
   /// <remarks>
   /// <see cref="StreamFactory"/> class provides methods to create objects implementing this interface.
   /// </remarks>
   public interface StreamWriterWithResizableBuffer : AbstractStreamWithResizableBuffer
   {
      /// <summary>
      /// This method tries to grow the <see cref="AbstractStreamWithResizableBuffer.Buffer"/> by given <paramref name="count"/>, and returns the range that it succeeded to reserve.
      /// </summary>
      /// <param name="count">The amount of bytes to reserve.</param>
      /// <returns>The range information about actually reserved segment.</returns>
      /// <exception cref="InvalidOperationException">If this stream writer is currently unusable (concurrent write or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      (Int32 Offset, Int32 Count) ReserveBufferSegment( Int32 count );

      /// <summary>
      /// This method marks the given amount of bytes as free at the end of the buffer.
      /// </summary>
      /// <param name="count">The amount of bytes to free up. Negative values are treated as freeing up all of the buffer.</param>
      /// <exception cref="InvalidOperationException">If this stream writer is currently unusable (concurrent write or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      void UnreserveBufferSegment( Int32 count );

      /// <summary>
      /// This method asynchronously flushes the buffer contents to underlying <see cref="Stream"/>.
      /// </summary>
      /// <returns>The task which returns amount of bytes actually written to underlying <see cref="Stream"/>.</returns>
      /// <exception cref="InvalidOperationException">If this stream writer is currently unusable (concurrent write or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      ValueTask<Int32> FlushAsync();

      /// <summary>
      /// Returns the current amount of reserved bytes in this <see cref="StreamWriterWithResizableBuffer"/>.
      /// </summary>
      /// <value>The current amount of reserved bytes in this <see cref="StreamWriterWithResizableBuffer"/>.</value>
      Int32 ReservedBufferCount { get; }

      /// <summary>
      /// Asynchronously creates a new <see cref="InnerStreamWriterWithResizableBufferAndLimitedSize"/> with given byte limit.
      /// This <see cref="StreamWriterWithResizableBuffer"/> will become unusable until the <see cref="IDisposable.Dispose"/> method is called on returned <see cref="InnerStreamWriterWithResizableBufferAndLimitedSize"/>.
      /// </summary>
      /// <param name="byteLimit">The maximum amount of bytes that returned <see cref="InnerStreamWriterWithResizableBufferAndLimitedSize"/> may write to underlying <see cref="Stream"/>.</param>
      /// <returns>The task which will result in <see cref="InnerStreamWriterWithResizableBufferAndLimitedSize"/>.</returns>
      /// <exception cref="InvalidOperationException">If this stream writer is currently unusable (concurrent write or usage of nested buffer created by <see cref="CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
      ValueTask<InnerStreamWriterWithResizableBufferAndLimitedSize> CreateWithLimitedSizeAndSharedBuffer( Int64 byteLimit );
   }


   /// <summary>
   /// This interface further specializes <see cref="StreamWriterWithResizableBuffer"/> to include boundary on maximum amount of bytes that may be written to underlying <see cref="Stream"/>.
   /// </summary>
   /// <remarks>
   /// <see cref="StreamFactory"/> class provides methods to create objects implementing this interface.
   /// </remarks>
   public interface StreamWriterWithResizableBufferAndLimitedSize : StreamWriterWithResizableBuffer, LimitedSizeInfo
   {
   }

   /// <summary>
   /// This interface augments <see cref="StreamWriterWithResizableBufferAndLimitedSize"/> interface with <see cref="IDisposable.Dispose"/> method from <see cref="IDisposable"/>.
   /// By calling the <see cref="IDisposable.Dispose"/> method, the <see cref="StreamWriterWithResizableBuffer"/> which created this <see cref="InnerStreamWriterWithResizableBufferAndLimitedSize"/> will become useable again.
   /// </summary>
   public interface InnerStreamWriterWithResizableBufferAndLimitedSize : StreamWriterWithResizableBufferAndLimitedSize, IDisposable
   {
   }

   /// <summary>
   /// This class provides methods to create instances of <see cref="StreamReaderWithResizableBuffer"/>, <see cref="StreamReaderWithResizableBufferAndLimitedSize"/>, <see cref="StreamWriterWithResizableBuffer"/>, and <see cref="StreamWriterWithResizableBufferAndLimitedSize"/>.
   /// </summary>
   public static class StreamFactory
   {
      // TODO maybe implicit casts? Stream -> StreamReaderWithResizableBuffer, Stream -> StreamWriterWithResizableBuffer
      // TODO Maybe make calls to stream callbacks and pass the callbacks to Readers/Writers.
      //      Then we can detect MemoryStream and pass fully synchronous callbacks -> all stuff will always be completely synchronous.

      private const Int32 DEFAULT_CHUNK_SIZE = 1024;

      /// <summary>
      /// Creates new <see cref="StreamReaderWithResizableBuffer"/> with given underlying <see cref="Stream"/>.
      /// </summary>
      /// <param name="stream">The underlying <see cref="Stream"/> from which to read bytes.</param>
      /// <param name="token">Optional <see cref="CancellationToken"/> that will be used for read operations on given <see cref="Stream"/>.</param>
      /// <param name="buffer">Optional growing buffer to use.</param>
      /// <param name="chunkSize">Optional maximum amount of bytes that will be read in single chunk when there is need to read from stream.</param>
      /// <returns>A new instance of <see cref="StreamReaderWithResizableBuffer"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c>.</exception>
      public static StreamReaderWithResizableBuffer CreateUnlimitedReader(
         Stream stream,
         CancellationToken token = default( CancellationToken ),
         ResizableArray<Byte> buffer = null,
         Int32 chunkSize = DEFAULT_CHUNK_SIZE
         )
      {
         return new StreamReaderWithResizableBufferImpl( stream, token, buffer, chunkSize );
      }

      /// <summary>
      /// Creates new <see cref="StreamReaderWithResizableBufferAndLimitedSize"/> with given underlying <see cref="Stream"/> and maximum amount of bytes that can be read from the stream.
      /// </summary>
      /// <param name="stream">The underlying <see cref="Stream"/>.</param>
      /// <param name="byteCount">The maximum amount of bytes that can be read from underlying <see cref="Stream"/>. Negative values are interpreted as <c>0</c>.</param>
      /// <param name="token">Optional <see cref="CancellationToken"/> that will be used for read operations on given <see cref="Stream"/>.</param>
      /// <param name="buffer">Optional growing buffer to use.</param>
      /// <param name="chunkSize">Optional maximum amount of bytes that will be read in single chunk when there is need to read from stream.</param>
      /// <returns>A new instance of <see cref="StreamReaderWithResizableBufferAndLimitedSize"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="byteCount"/><c> &gt; 0</c> and <paramref name="stream"/> is <c>null</c>.</exception>
      public static StreamReaderWithResizableBufferAndLimitedSize CreateLimitedReader(
         Stream stream,
         Int64 byteCount,
         CancellationToken token = default( CancellationToken ),
         ResizableArray<Byte> buffer = null,
         Int32 chunkSize = DEFAULT_CHUNK_SIZE
         )
      {
         return byteCount <= 0 ?
            (StreamReaderWithResizableBufferAndLimitedSize) new EmptyReader( token ) :
            new StreamReaderWithResizableBufferAndLimitedSizeImpl( stream, token, buffer, chunkSize, byteCount );
      }

      /// <summary>
      /// Creates new <see cref="StreamWriterWithResizableBuffer"/> with given underlying <see cref="Stream"/>
      /// </summary>
      /// <param name="stream">The underlying <see cref="Stream"/> to which to write bytes.</param>
      /// <param name="token">Optional <see cref="CancellationToken"/> that will be used for write operations on given <see cref="Stream"/>.</param>
      /// <param name="buffer">Optional growing buffer to use.</param>
      /// <returns>A new instance of <see cref="StreamWriterWithResizableBuffer"/>.</returns>
      /// <exception cref=" ArgumentNullException">If <paramref name="stream"/> is <c>null</c>.</exception>
      public static StreamWriterWithResizableBuffer CreateUnlimitedWriter(
         Stream stream,
         CancellationToken token = default( CancellationToken ),
         ResizableArray<Byte> buffer = null
         )
      {
         return new StreamWriterWithResizableBufferImpl( stream, token, buffer );
      }

      /// <summary>
      /// Creates new <see cref="StreamWriterWithResizableBufferAndLimitedSize"/> with given underlying <see cref="Stream"/> and maximum amount of bytse that may be written to it.
      /// </summary>
      /// <param name="stream">The underlying <see cref="Stream"/>.</param>
      /// <param name="byteCount">The maximum amount of bytes that may be written to underlying <see cref="Stream"/>.</param>
      /// <param name="token">Optional <see cref="CancellationToken"/> that will be used for write operations on given <see cref="Stream"/>.</param>
      /// <param name="buffer">Optional growing buffer to use.</param>
      /// <returns>A new instance of <see cref="StreamWriterWithResizableBufferAndLimitedSize"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="byteCount"/><c> &gt; 0</c> and <paramref name="stream"/> is <c>null</c>.</exception>
      public static StreamWriterWithResizableBufferAndLimitedSize CreateLimitedWriter(
         Stream stream,
         Int64 byteCount,
         CancellationToken token = default( CancellationToken ),
         ResizableArray<Byte> buffer = null
         )
      {
         return byteCount <= 0 ?
            (StreamWriterWithResizableBufferAndLimitedSize) new EmptyWriter( token ) :
            new StreamWriterWithResizableBufferAndLimitedSizeImpl( stream, token, buffer, byteCount );
      }
   }

   internal class StreamReaderWithResizableBufferImpl : StreamReaderWithResizableBuffer
   {
      private const Int32 IDLE = 0;
      private const Int32 READING = 1;
      private const Int32 RESERVED_FOR_INNER_READER = 3;
      private const Int32 MANIPULATING_BUFFER = 4;
      private const Int32 WAITING_ON_INNER_REMAINING_READ = 5;
      private const Int32 READING_INNER_REMAINING = 6;

      private readonly Stream _stream;
      private readonly ResizableArray<Byte> _buffer;
      private readonly Int32 _chunkSize;

      protected Int32 _bytesInBufferUsedUp;
      protected Int32 _bytesInBuffer;
      private Int32 _state;
      private Int64 _innerStreamReadRemains;

      public StreamReaderWithResizableBufferImpl(
         Stream stream,
         CancellationToken token,
         ResizableArray<Byte> buffer,
         Int32 chunkSize
         ) : this( stream, token, buffer, 0, chunkSize, true )
      {
      }

      protected StreamReaderWithResizableBufferImpl(
         Stream stream,
         CancellationToken token,
         ResizableArray<Byte> buffer,
         Int32 bytesInBuffer,
         Int32 chunkSize,
         Boolean checkForStream
         )
      {
         if ( checkForStream )
         {
            ArgumentValidator.ValidateNotNull( nameof( stream ), stream );
         }


         this._stream = stream;
         this.CancellationToken = token;
         this._buffer = buffer ?? new ResizableArray<Byte>();
         this._chunkSize = Math.Max( 1, chunkSize );
         if ( buffer != null )
         {
            this._bytesInBuffer = Math.Max( this._bytesInBufferUsedUp, bytesInBuffer );
         }
      }

      public Byte[] Buffer => this._buffer.Array;

      public CancellationToken CancellationToken { get; }

      public Int32 ChunkSize => this._chunkSize;

      public void EraseReadBytesFromBuffer()
      {
         if ( Interlocked.CompareExchange( ref this._state, MANIPULATING_BUFFER, IDLE ) == IDLE )
         {
            try
            {
               this.ForgetUsedBytes();
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

      public Int32 ReadBytesCount
      {
         get
         {
            return this._bytesInBufferUsedUp;
         }
      }

      public void EraseReadBufferSegment( Int32 firstDeletableByteIndex, Int32 count )
      {
         if ( Interlocked.CompareExchange( ref this._state, MANIPULATING_BUFFER, IDLE ) == IDLE )
         {
            try
            {
               if ( firstDeletableByteIndex >= 0 && count > 0 && firstDeletableByteIndex + count <= this._bytesInBufferUsedUp )
               {
                  this.Buffer.ShiftArraySegmentLeft( ref this._bytesInBuffer, firstDeletableByteIndex, count );
                  Interlocked.Exchange( ref this._bytesInBufferUsedUp, this._bytesInBufferUsedUp - count );
               }
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

      public void UnreadBytes( Int32 amount )
      {
         if ( Interlocked.CompareExchange( ref this._state, MANIPULATING_BUFFER, IDLE ) == IDLE )
         {
            try
            {
               if ( amount < 0 )
               {
                  amount = this._bytesInBufferUsedUp;
               }

               if ( amount > 0 && amount <= this._bytesInBufferUsedUp )
               {
                  Interlocked.Exchange( ref this._bytesInBufferUsedUp, this._bytesInBufferUsedUp - amount );
                  this.ReadCompletedSynchronously( -amount );
               }
            }
            finally
            {
               Interlocked.Exchange( ref this._state, IDLE );
            }
         }

      }

      public async ValueTask<Boolean> TryReadAsync( Int32 count )
      {
         return await this.PerformReadAsync( count, true );
      }

      public async ValueTask<Boolean> TryReadMoreAsync( Int32 count )
      {
         return await this.PerformReadAsync( count, false );
      }

      public InnerStreamReaderWithResizableBufferAndLimitedSize CreateWithLimitedSizeAndSharedBuffer( Int64 byteCount )
      {
         InnerStreamReaderWithResizableBufferAndLimitedSize retVal;
         if ( Interlocked.CompareExchange( ref this._state, RESERVED_FOR_INNER_READER, IDLE ) == IDLE )
         {
            var resetState = true;
            try
            {
               if ( byteCount > 0 && ( byteCount = this.CheckByteCountForNewLimitedStream( byteCount ) ) > 0 )
               {
                  this.ForgetUsedBytes();
                  resetState = false;
                  var needsStream = byteCount > this._bytesInBuffer - this._bytesInBufferUsedUp;
                  retVal = new InnerStreamReaderWithResizableBufferAndLimitedSizeImpl(
                     needsStream ? this._stream : null,
                     this.CancellationToken,
                     this._buffer,
                     this._chunkSize,
                     byteCount,
                     this._bytesInBuffer,
                     this.OnInnerStreamDispose
                     );

               }
               else
               {
                  retVal = new EmptyReader( this.CancellationToken );
               }
            }
            finally
            {
               if ( resetState )
               {
                  Interlocked.Exchange( ref this._state, IDLE );
               }
            }
         }
         else
         {
            throw BusyException();
         }

         return retVal;
      }

      private async ValueTask<Boolean> PerformReadAsync(
         Int32 count,
         Boolean forgetUsedBytes
         )
      {
         await this.TryReadRemainsOfInnerStream();

         var retVal = count > 0;
         if ( retVal )
         {
            if ( Interlocked.CompareExchange( ref this._state, READING, IDLE ) == IDLE )
            {
               try
               {
                  if ( forgetUsedBytes )
                  {
                     this.ForgetUsedBytes();
                  }

                  count = this.CheckByteCountForBufferRead( count );
                  retVal = count > 0;
                  if ( retVal )
                  {
                     var newLimit = this._bytesInBufferUsedUp + count;
                     if ( newLimit <= this._bytesInBuffer )
                     {
                        // No need to read from stream
                        Interlocked.Exchange( ref this._bytesInBufferUsedUp, newLimit );
                        this.ReadCompletedSynchronously( count );
                     }
                     else
                     {
                        // Calculate how much we really need to read
                        var amountToReadFromStream = Math.Max( newLimit - this._bytesInBuffer, this._chunkSize );
                        amountToReadFromStream = this.CheckByteCountForBufferRead( amountToReadFromStream );
                        retVal = amountToReadFromStream > 0 || this._bytesInBufferUsedUp < this._bytesInBuffer;
                        if ( retVal )
                        {

                           if ( amountToReadFromStream > 0 )
                           {
                              // make sure buffer will have room
                              this._buffer.CurrentMaxCapacity = this._bytesInBuffer + amountToReadFromStream;

                              // Perform read async
                              amountToReadFromStream = await this._stream.TryReadSpecificAmountAsync( this.Buffer, this._bytesInBuffer, amountToReadFromStream, this.CancellationToken );
                              Interlocked.Exchange( ref this._bytesInBuffer, this._bytesInBuffer + amountToReadFromStream );
                           }

                           count = Math.Min( count, this._bytesInBuffer - this._bytesInBufferUsedUp );
                           retVal = count > 0;
                           if ( retVal )
                           {
                              Interlocked.Exchange( ref this._bytesInBufferUsedUp, this._bytesInBufferUsedUp + count );

                              if ( amountToReadFromStream > 0 )
                              {
                                 this.ReadCompletedAsynchronously( count, amountToReadFromStream );
                              }
                              else
                              {
                                 this.ReadCompletedSynchronously( count );
                              }
                           }
                        }
                     }
                  }
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

         return retVal;
      }

      private void ForgetUsedBytes()
      {
         if ( this._bytesInBufferUsedUp > 0 && this._bytesInBufferUsedUp <= this._bytesInBuffer )
         {
            this._buffer.Array.ShiftArraySegmentLeft( ref this._bytesInBuffer, 0, this._bytesInBufferUsedUp );
            Interlocked.Exchange( ref this._bytesInBufferUsedUp, 0 );
         }
      }

      private async ValueTask<Boolean> TryReadRemainsOfInnerStream()
      {
         Int32 prevState;
         var retVal = ( prevState = Interlocked.CompareExchange( ref this._state, READING_INNER_REMAINING, WAITING_ON_INNER_REMAINING_READ ) ) == WAITING_ON_INNER_REMAINING_READ;
         if ( retVal )
         {
            try
            {
               var curRead = this._innerStreamReadRemains;
               if ( curRead > this._buffer.CurrentMaxCapacity )
               {
                  this._buffer.CurrentMaxCapacity = Min( curRead, this._chunkSize );
               }
               var bytesRemaining = curRead;
               Int32 lastReadCount;
               do
               {
                  lastReadCount = await this._stream.TryReadSpecificAmountAsync( this.Buffer, 0, Min( curRead, this._chunkSize ), this.CancellationToken );
                  curRead -= lastReadCount;
               } while ( lastReadCount > 0 && curRead > 0 );

               this.ReadCompletedAsynchronously( 0, this._innerStreamReadRemains );
               Interlocked.Exchange( ref this._innerStreamReadRemains, 0 );
               Interlocked.Exchange( ref this._bytesInBufferUsedUp, 0 );
               Interlocked.Exchange( ref this._bytesInBuffer, 0 );
            }
            finally
            {
               Interlocked.Exchange( ref this._state, IDLE );
            }
         }
         else if ( prevState == READING_INNER_REMAINING )
         {
            throw BusyException();
         }

         return retVal;
      }

      protected virtual Int64 CheckByteCountForNewLimitedStream( Int64 byteCount )
      {
         return byteCount;
      }

      protected virtual void ReadCompletedSynchronously( Int64 count )
      {
         // Do nothing
      }

      protected virtual Int32 CheckByteCountForBufferRead( Int32 givenCount )
      {
         return givenCount;
      }

      protected virtual void ReadCompletedAsynchronously( Int64 count, Int64 streamReadCount )
      {
         // Do nothing
      }

      protected void OnInnerStreamDispose(
         Int64 innerStreamSize,
         Int64 bytesLeft,
         Int64 initialStreamBytesLeftToRead,
         Int64 streamBytesLeftToRead,
         Int32 bytesInBufferUsedUp,
         Int32 bytesInBufferTotal
         )
      {
         System.Diagnostics.Debug.Assert( streamBytesLeftToRead <= bytesLeft );

         var innerReaderNeededStream = innerStreamSize > this._bytesInBuffer - this._bytesInBufferUsedUp;
         if ( streamBytesLeftToRead <= 0 )
         {
            // No asynchrony will be needed, just update variables
            if ( innerReaderNeededStream )
            {
               // This only happens when inner stream read from System.IO.Stream at least once
               Interlocked.Exchange( ref this._bytesInBuffer, bytesInBufferTotal );
               Interlocked.Exchange( ref this._bytesInBufferUsedUp, bytesInBufferUsedUp + (Int32) bytesLeft );
            }
            else
            {
               Interlocked.Exchange( ref this._bytesInBufferUsedUp, this._bytesInBufferUsedUp + (Int32) innerStreamSize );
            }

            this.ReadCompletedAsynchronously( innerStreamSize, initialStreamBytesLeftToRead );

            // Then forget the bytes
            this.ForgetUsedBytes();

            // Reset our state
            Interlocked.Exchange( ref this._state, IDLE );
         }
         else
         {
            // We must read the remaining stream bytes on next call to TryRead/TryReadMore
            this.ReadCompletedSynchronously( bytesLeft );

            // Set up state to be waiting for async read
            Interlocked.Exchange( ref this._state, WAITING_ON_INNER_REMAINING_READ );
         }
      }

      private static InvalidOperationException BusyException()
      {
         return new InvalidOperationException( "This buffer is not useable right now." );
      }

      internal static Int32 Min( Int64 i64, Int32 i32 )
      {
         return i64 < i32 ? (Int32) i64 : i32;
      }
   }

   internal class StreamReaderWithResizableBufferAndLimitedSizeImpl : StreamReaderWithResizableBufferImpl, StreamReaderWithResizableBufferAndLimitedSize
   {
      protected Int64 _bytesLeft;
      protected Int64 _streamBytesLeftToRead;
      protected readonly Int64 _initialStreamBytesLeftToRead;

      public StreamReaderWithResizableBufferAndLimitedSizeImpl(
         Stream stream,
         CancellationToken token,
         ResizableArray<Byte> buffer,
         Int32 chunkSize,
         Int64 byteCount
         ) : this( stream, token, buffer, chunkSize, byteCount, 0, true )
      {
      }

      protected StreamReaderWithResizableBufferAndLimitedSizeImpl(
         Stream stream,
         CancellationToken token,
         ResizableArray<Byte> buffer,
         Int32 chunkSize,
         Int64 byteCount,
         Int32 bytesInBuffer,
         Boolean checkForStream
         ) : base( stream, token, buffer, bytesInBuffer: Min( byteCount, bytesInBuffer ), chunkSize: chunkSize, checkForStream: checkForStream )
      {
         this._bytesLeft = this.TotalByteCount = byteCount;
         this._initialStreamBytesLeftToRead = this._streamBytesLeftToRead = Math.Max( byteCount - bytesInBuffer, 0 );
      }

      public Int64 TotalByteCount { get; }

      public Int64 BytesLeft => this._bytesLeft;

      protected override Int32 CheckByteCountForBufferRead( Int32 givenCount ) => Min( this._bytesLeft, givenCount );

      protected override Int64 CheckByteCountForNewLimitedStream( Int64 byteCount ) => Math.Min( this._bytesLeft, byteCount );

      protected override void ReadCompletedAsynchronously( Int64 count, Int64 streamReadCount )
      {
         this.ReadCompletedSynchronously( count );
         System.Diagnostics.Debug.Assert( this._streamBytesLeftToRead - streamReadCount >= 0 );
         Interlocked.Exchange( ref this._streamBytesLeftToRead, this._streamBytesLeftToRead - streamReadCount );
      }

      protected override void ReadCompletedSynchronously( Int64 count )
      {
         System.Diagnostics.Debug.Assert( this._bytesLeft - count >= 0 );
         Interlocked.Exchange( ref this._bytesLeft, this._bytesLeft - count );
      }


   }

   internal sealed class InnerStreamReaderWithResizableBufferAndLimitedSizeImpl : StreamReaderWithResizableBufferAndLimitedSizeImpl, InnerStreamReaderWithResizableBufferAndLimitedSize
   {
      private readonly Action<Int64, Int64, Int64, Int64, Int32, Int32> _onDispose;

      public InnerStreamReaderWithResizableBufferAndLimitedSizeImpl(
         Stream stream,
         CancellationToken token,
         ResizableArray<Byte> buffer,
         Int32 chunkSize,
         Int64 byteCount,
         Int32 bytesInBuffer,
         Action<Int64, Int64, Int64, Int64, Int32, Int32> onDispose
         ) : base( stream, token, buffer, chunkSize, byteCount, bytesInBuffer, false )
      {
         this._onDispose = onDispose;
      }

      public void Dispose()
      {
         this._onDispose?.Invoke( this.TotalByteCount, this._bytesLeft, this._initialStreamBytesLeftToRead, this._streamBytesLeftToRead, this._bytesInBufferUsedUp, this._bytesInBuffer );
      }
   }

   internal sealed class EmptyReader : InnerStreamReaderWithResizableBufferAndLimitedSize
   {
      public EmptyReader( CancellationToken token )
      {
         this.CancellationToken = token;
      }

      public Byte[] Buffer => Empty<Byte>.Array;

      public CancellationToken CancellationToken { get; }

      public Int64 TotalByteCount => 0;

      public Int64 BytesLeft => 0;

      public Int32 ReadBytesCount => 0;

      // Return 1 to adhere to contract of Streamreaderwithresizablebuffer.
      public Int32 ChunkSize => 1;

      public InnerStreamReaderWithResizableBufferAndLimitedSize CreateWithLimitedSizeAndSharedBuffer( Int64 byteCount )
      {
         return this;
      }

      public void EraseReadBufferSegment( Int32 firstDeletableByteIndex, Int32 count )
      {
      }

      public void EraseReadBytesFromBuffer()
      {
      }

      public void UnreadBytes( Int32 amount = -1 )
      {
      }

      public ValueTask<Boolean> TryReadAsync( Int32 amount )
      {
         return new ValueTask<Boolean>( amount <= 0 );
      }

      public ValueTask<Boolean> TryReadMoreAsync( int amount )
      {
         return new ValueTask<Boolean>( amount <= 0 );
      }

      public void Dispose()
      {
         // Nothing to do.
      }
   }



   internal class StreamWriterWithResizableBufferImpl : StreamWriterWithResizableBuffer
   {
      private const Int32 IDLE = 0;
      private const Int32 OPERATING_STREAM = 1;
      private const Int32 OPERATING_SUB_STREAM = 2;

      private readonly Stream _stream;
      private readonly ResizableArray<Byte> _buffer;

      protected Int32 _appendedByteCount;
      private Int32 _state;

      public StreamWriterWithResizableBufferImpl(
         Stream stream,
         CancellationToken token,
         ResizableArray<Byte> buffer
         )
      {
         this._stream = stream;
         this.CancellationToken = token;
         this._buffer = buffer ?? new ResizableArray<Byte>();
      }

      public Byte[] Buffer => this._buffer.Array;

      public Int32 ReservedBufferCount => this._appendedByteCount;

      public CancellationToken CancellationToken { get; }

      public (Int32 Offset, Int32 Count) ReserveBufferSegment( Int32 count )
      {
         Int32 offset;
         if ( Interlocked.CompareExchange( ref this._state, OPERATING_STREAM, IDLE ) == IDLE )
         {
            try
            {
               if ( count > 0 && ( count = this.ProcessCountForArrayWrite( count ) ) > 0 )
               {
                  offset = this._appendedByteCount;
                  var newByteCount = this._appendedByteCount + count;
                  this._buffer.CurrentMaxCapacity = newByteCount;
                  Interlocked.Exchange( ref this._appendedByteCount, newByteCount );
               }
               else
               {
                  offset = -1;
                  count = 0;
               }
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

         return (offset, count);
      }

      public void UnreserveBufferSegment( Int32 count )
      {
         if ( count != 0 )
         {
            if ( Interlocked.CompareExchange( ref this._state, OPERATING_STREAM, IDLE ) == IDLE )
            {
               try
               {
                  if ( count < 0 || count > this._appendedByteCount )
                  {
                     count = this._appendedByteCount;
                  }
                  Interlocked.Exchange( ref this._appendedByteCount, this._appendedByteCount - count );
                  this.AfterUnreserve( count );
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
      }

      public async ValueTask<Int32> FlushAsync()
      {
         Int32 retVal;
         if ( Interlocked.CompareExchange( ref this._state, OPERATING_STREAM, IDLE ) == IDLE )
         {
            try
            {
               retVal = this._appendedByteCount;
               if ( retVal > 0 )
               {
                  await this.WriteToStreamAsync( this._stream, this.Buffer, 0, this._appendedByteCount, this.CancellationToken, true );
                  Interlocked.Exchange( ref this._appendedByteCount, 0 );
               }
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

         return retVal;
      }

      public async ValueTask<InnerStreamWriterWithResizableBufferAndLimitedSize> CreateWithLimitedSizeAndSharedBuffer( Int64 byteLimit )
      {
         InnerStreamWriterWithResizableBufferAndLimitedSize retVal;
         if ( Interlocked.CompareExchange( ref this._state, OPERATING_SUB_STREAM, IDLE ) == IDLE )
         {
            var resetState = true;
            try
            {
               if ( byteLimit > 0 && ( byteLimit = this.ProcessCountForInnerStream( byteLimit ) ) > 0 )
               {
                  resetState = false;
                  await this.FlushAsync( OPERATING_SUB_STREAM );
                  retVal = new InnerStreamWriterWithResizableBufferAndLimitedSizeImpl(
                     this._stream,
                     this.CancellationToken,
                     this._buffer,
                     byteLimit,
                     ( currentAppendedCount, bytesLeft ) =>
                     {
                        if ( bytesLeft > 0 )
                        {
                           // Write zeroes for the rest
                           Array.Clear( this._buffer.Array, currentAppendedCount, (Int32) bytesLeft );
                        }

                        Interlocked.Exchange( ref this._appendedByteCount, (Int32) ( currentAppendedCount + bytesLeft ) );
                        Interlocked.Exchange( ref this._state, IDLE );
                     }
                     );
               }
               else
               {
                  retVal = new EmptyWriter( this.CancellationToken );
               }
            }
            finally
            {
               if ( resetState )
               {
                  Interlocked.Exchange( ref this._state, IDLE );
               }
            }
         }
         else
         {
            throw BusyException();
         }

         return retVal;
      }

      private async ValueTask<Int32> FlushAsync( Int32 prevState )
      {
         Int32 retVal;
         if ( Interlocked.CompareExchange( ref this._state, OPERATING_STREAM, prevState ) == prevState )
         {
            try
            {
               retVal = this._appendedByteCount;
               if ( retVal > 0 )
               {
                  await this.WriteToStreamAsync( this._stream, this.Buffer, 0, this._appendedByteCount, this.CancellationToken, true );
                  Interlocked.Exchange( ref this._appendedByteCount, 0 );
               }
            }
            finally
            {
               Interlocked.Exchange( ref this._state, prevState );
            }
         }
         else
         {
            throw BusyException();
         }

         return retVal;
      }

      protected virtual Int32 ProcessCountForArrayWrite( Int32 count )
      {
         return count;
      }

      protected virtual void AfterUnreserve( Int32 unreserveCount )
      {
      }

      protected virtual Int64 ProcessCountForInnerStream( Int64 count )
      {
         return count;
      }

      protected virtual async Task WriteToStreamAsync( Stream stream, Byte[] array, Int32 offset, Int32 count, CancellationToken token, Boolean flush )
      {
         await stream.WriteAsync( array, offset, count, token );
         if ( flush )
         {
            await stream.FlushAsync( token );
         }
      }

      private static InvalidOperationException BusyException()
      {
         return new InvalidOperationException( "This writer is not useable right now." );
      }
   }

   internal class StreamWriterWithResizableBufferAndLimitedSizeImpl : StreamWriterWithResizableBufferImpl, StreamWriterWithResizableBufferAndLimitedSize
   {
      protected Int64 _bytesLeft;
      private Boolean _performFlush;

      public StreamWriterWithResizableBufferAndLimitedSizeImpl(
         Stream stream,
         CancellationToken token,
         ResizableArray<Byte> buffer,
         Int64 byteCount
         ) : base( stream, token, buffer )
      {
         this._bytesLeft = this.TotalByteCount = byteCount;
      }

      public Int64 TotalByteCount { get; }

      public Int64 BytesLeft => this._bytesLeft;

      protected override Int32 ProcessCountForArrayWrite( Int32 count )
      {
         count = StreamReaderWithResizableBufferImpl.Min( this._bytesLeft, count );
         if ( count > 0 )
         {
            Interlocked.Exchange( ref this._bytesLeft, this._bytesLeft - count );
            if ( this._bytesLeft == 0 )
            {
               this._performFlush = true;
            }
         }
         return count;
      }

      protected override void AfterUnreserve( Int32 unreserveCount )
      {
         Interlocked.Exchange( ref this._bytesLeft, this._bytesLeft + unreserveCount );
         if ( this._performFlush && this._bytesLeft > 0 )
         {
            this._performFlush = false;
         }
      }

      protected override Int64 ProcessCountForInnerStream( Int64 count )
      {
         count = Math.Min( this._bytesLeft, count );
         if ( count > 0 )
         {
            Interlocked.Exchange( ref this._bytesLeft, this._bytesLeft - count );
            if ( this._bytesLeft == 0 )
            {
               this._performFlush = true;
            }
         }
         return count;
      }

      protected override async Task WriteToStreamAsync( Stream stream, Byte[] array, Int32 offset, Int32 count, CancellationToken token, Boolean flush )
      {
         await base.WriteToStreamAsync( stream, array, offset, count, token, this._performFlush );
         if ( this._performFlush )
         {
            this._performFlush = false;
         }
      }


   }

   internal sealed class InnerStreamWriterWithResizableBufferAndLimitedSizeImpl : StreamWriterWithResizableBufferAndLimitedSizeImpl, InnerStreamWriterWithResizableBufferAndLimitedSize
   {
      private readonly Action<Int32, Int64> _onDispose;

      public InnerStreamWriterWithResizableBufferAndLimitedSizeImpl(
         Stream stream,
         CancellationToken token,
         ResizableArray<Byte> buffer,
         Int64 byteCount,
         Action<Int32, Int64> onDispose
      ) : base( stream, token, buffer, byteCount )
      {
         this._onDispose = onDispose;
      }

      public void Dispose()
      {
         try
         {
            this._onDispose?.Invoke( this._appendedByteCount, this._bytesLeft );
         }
         catch
         {
            // Ignore
         }
      }
   }

   internal sealed class EmptyWriter : InnerStreamWriterWithResizableBufferAndLimitedSize
   {
      private readonly CancellationToken _token;

      public EmptyWriter( CancellationToken token )
      {
         this._token = token;
      }

      public Byte[] Buffer => Empty<Byte>.Array;

      public Int32 ReservedBufferCount => 0;

      public CancellationToken CancellationToken { get; }

      public Int64 TotalByteCount => 0;

      public Int64 BytesLeft => 0;

      public (Int32 Offset, Int32 Count) ReserveBufferSegment( Int32 count ) => (0, 0);

      public void UnreserveBufferSegment( Int32 count ) { }

      public ValueTask<InnerStreamWriterWithResizableBufferAndLimitedSize> CreateWithLimitedSizeAndSharedBuffer( Int64 byteLimit ) => new ValueTask<InnerStreamWriterWithResizableBufferAndLimitedSize>( byteLimit <= 0 ? this : null );

      public ValueTask<Int32> FlushAsync() => new ValueTask<Int32>( 0 );

      public void Dispose()
      {
      }
   }

#if NET40
   // Theraot.Core does not provide (yet?) extension methods for async write/read for streams

   public static partial class UtilPackExtensions
   {
      /// <todo />
      public static Task<Int32> ReadAsync( this Stream stream, Byte[] buffer, Int32 offset, Int32 count, CancellationToken token )
      {
         token.ThrowIfCancellationRequested();
         var readArgs = (stream, buffer, offset, count);
         return Task.Factory.FromAsync(
           (rArgs, cb, state) => rArgs.Item1.BeginRead( rArgs.Item2, rArgs.Item3, rArgs.Item4, cb, state ),
           (result) => ((Stream)result.AsyncState).EndRead( result ),
           readArgs,
           stream
           );
      }

      /// <todo />
      public static Task WriteAsync( this Stream stream, Byte[] buffer, Int32 offset, Int32 count, CancellationToken token )
      {
         token.ThrowIfCancellationRequested();
         var writeArgs = (stream, buffer, offset, count);
         return Task.Factory.FromAsync(
           (wArgs, cb, state) => wArgs.Item1.BeginWrite( wArgs.Item2, wArgs.Item3, wArgs.Item4, cb, state),
           (result) => ((Stream)result.AsyncState).EndWrite( result ),
           writeArgs,
           stream
           );
      }
      
      /// <todo />
      public static Task FlushAsync( this Stream stream, CancellationToken token )
      {
         return TaskEx.Run(() => stream.Flush(), token);
      }
   }
#endif
}


public static partial class E_UtilPack
{
   /// <summary>
   /// Helper method to read all the remaining bytes in underlying <see cref="Stream"/> into the <see cref="AbstractStreamWithResizableBuffer.Buffer"/> of this <see cref="StreamReaderWithResizableBuffer"/>.
   /// </summary>
   /// <param name="stream">This <see cref="StreamReaderWithResizableBuffer"/>.</param>
   /// <returns>Task which completes when the last byte has been read into <see cref="AbstractStreamWithResizableBuffer.Buffer"/>, returning amount of bytes read.</returns>
   /// <exception cref="NullReferenceException">If this <paramref name="stream"/> is <c>null</c>.</exception>
   /// <exception cref="ArithmeticException">If there are more bytes left than can be fit into 32-bit integer.</exception>
   public static async ValueTask<Int32> ReadAllBytesToBuffer( this StreamReaderWithResizableBuffer stream )
   {
      var start = stream.ReadBytesCount;
      var chunkSize = ArgumentValidator.ValidateNotNullReference( stream ).ChunkSize;
      if ( chunkSize > 0 )
      {
         while ( await stream.TryReadMoreAsync( chunkSize ) ) ;
         // One more time, since TryReadMoreAsync returns false even if it read 1 byte less than given.
         await stream.TryReadMoreAsync( chunkSize );
      }
      return stream.ReadBytesCount - start;
   }

   /// <summary>
   /// Helper method to read all the reamining bytes of this <see cref="StreamReaderWithResizableBufferAndLimitedSize"/> into the <see cref="AbstractStreamWithResizableBuffer.Buffer"/>.
   /// </summary>
   /// <param name="stream">This <see cref="StreamReaderWithResizableBufferAndLimitedSize"/>.</param>
   /// <returns>Task which completes when the last byte has been read into <see cref="AbstractStreamWithResizableBuffer.Buffer"/>, always returning <c>true</c>.</returns>
   /// <exception cref="NullReferenceException">If this <paramref name="stream"/> is <c>null</c>.</exception>
   /// <exception cref="ArithmeticException">If the value of <see cref="LimitedSizeInfo.TotalByteCount"/> of this <paramref name="stream"/> can not fit into 32-bit integer.</exception>
   public static async ValueTask<Boolean> ReadAllBytesToBuffer( this StreamReaderWithResizableBufferAndLimitedSize stream )
   {
      return await stream.ReadMoreOrThrow( (Int32) ArgumentValidator.ValidateNotNullReference( stream ).BytesLeft );
   }

   /// <summary>
   /// Helper method to skip through the remaining bytes of <see cref="StreamReaderWithResizableBuffer"/>.
   /// </summary>
   /// <param name="stream">This <see cref="StreamReaderWithResizableBuffer"/>.</param>
   /// <returns>Task which completes when the last available byte has been read into <see cref="AbstractStreamWithResizableBuffer.Buffer"/>, always returning <c>true</c>.</returns>
   /// <exception cref="NullReferenceException">If this <paramref name="stream"/> is <c>null</c>.</exception>
   public static async ValueTask<Boolean> SkipThroughRemainingBytes( this StreamReaderWithResizableBuffer stream )
   {
      var chunkSize = ArgumentValidator.ValidateNotNullReference( stream ).ChunkSize;
      if ( chunkSize > 0 )
      {
         while ( await stream.TryReadAsync( chunkSize ) ) ;
         // One more time, since TryReadAsync returns false even if it read 1 byte less than given.
         await stream.TryReadAsync( chunkSize );
      }
      return true;
   }

   /// <summary>
   /// Helper method to read and append specific amount of bytes, and throw an <see cref="EndOfStreamException"/> if the limit of this <paramref name="stream"/> has been encountered.
   /// </summary>
   /// <param name="stream">This <see cref="StreamReaderWithResizableBuffer"/>.</param>
   /// <param name="count">The amount of bytes to read and append to buffer.</param>
   /// <returns>Task which completes when last of the required bytes has been read into <see cref="AbstractStreamWithResizableBuffer.Buffer"/>, always returning <c>true</c>.</returns>
   /// <exception cref="NullReferenceException">If this <paramref name="stream"/> is <c>null</c>.</exception>
   /// <exception cref="EndOfStreamException">If <see cref="StreamReaderWithResizableBuffer.TryReadMoreAsync(int)"/> method returns <c>false</c>.</exception>
   public static async ValueTask<Boolean> ReadMoreOrThrow( this StreamReaderWithResizableBuffer stream, Int32 count )
   {
      if ( !await ArgumentValidator.ValidateNotNullReference( stream ).TryReadMoreAsync( count ) )
      {
         throw new EndOfStreamException();
      }
      return true;
   }

   internal static void ShiftArraySegmentLeft( this Array array, ref Int32 wholeSegmentCount, Int32 deletableStart, Int32 deletableCount )
   {
      if ( deletableCount > 0 )
      {
         var copyCount = wholeSegmentCount - deletableStart - deletableCount;
         if ( copyCount <= 0 )
         {
            Array.Clear( array, deletableStart, deletableCount );
         }
         else
         {
            var deletableEnd = deletableStart + deletableCount;
            Array.Copy( array, deletableEnd, array, deletableStart, wholeSegmentCount - deletableStart - deletableCount );
         }

         Interlocked.Exchange( ref wholeSegmentCount, wholeSegmentCount - deletableCount );
      }
   }

   /// <summary>
   /// Helper method to read specific amount of bytes, and throw an <see cref="EndOfStreamException"/> if the limit of this <paramref name="stream"/> has been encountered.
   /// </summary>
   /// <param name="stream">This <see cref="StreamReaderWithResizableBuffer"/>.</param>
   /// <param name="count">The amount of bytes to read and overwrite to buffer.</param>
   /// <returns>Task which completes when last of the required bytes has been read into <see cref="AbstractStreamWithResizableBuffer.Buffer"/>, always returning <c>true</c>.</returns>
   /// <exception cref="NullReferenceException">If this <paramref name="stream"/> is <c>null</c>.</exception>
   /// <exception cref="EndOfStreamException">If <see cref="StreamReaderWithResizableBuffer.TryReadAsync(int)"/> returns <c>false</c>.</exception>
   public static async ValueTask<Boolean> ReadOrThrow( this StreamReaderWithResizableBuffer stream, Int32 count )
   {
      if ( !await ArgumentValidator.ValidateNotNullReference( stream ).TryReadAsync( count ) )
      {
         throw new EndOfStreamException();
      }

      return true;
   }

   /// <summary>
   /// Helper method to read specific amount of bytes from <see cref="Stream"/> into byte array.
   /// </summary>
   /// <param name="stream">The stream to read bytes from.</param>
   /// <param name="array">The byte array to read bytes to.</param>
   /// <param name="offset">The offset where to start writing.</param>
   /// <param name="count">The amount of bytes to read.</param>
   /// <param name="token">The <see cref="CancellationToken"/> to use with stream operations.</param>
   /// <returns>Task which completes when last of the required bytes has been read into array, always returning <c>true</c>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="stream"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If <paramref name="offset"/> and <paramref name="count"/> are incorrect.</exception>
   /// <exception cref="EndOfStreamException">If end of stream encountered before given <paramref name="count"/> amount of bytes could be read.</exception>
   public static async ValueTask<Boolean> ReadSpecificAmountAsync( this Stream stream, Byte[] array, Int32 offset, Int32 count, CancellationToken token )
   {
      if ( ( await stream.TryReadSpecificAmountAsync( array, offset, count, token ) ) != count )
      {
         throw new EndOfStreamException();
      }
      return true;
   }

   /// <summary>
   /// Helper method to try to read specific amount of bytes from <see cref="Stream"/> into byte array.
   /// </summary>
   /// <param name="stream">The stream to read bytes from.</param>
   /// <param name="array">The byte array to read bytes to.</param>
   /// <param name="offset">The offset where to start writing.</param>
   /// <param name="count">The amount of bytes to read.</param>
   /// <param name="token">The <see cref="CancellationToken"/> to use with stream operations.</param>
   /// <returns>Task which completes when last of the required bytes has been read into array, always returning <c>true</c>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="stream"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If <paramref name="offset"/> and <paramref name="count"/> are incorrect.</exception>
   public static async ValueTask<Int32> TryReadSpecificAmountAsync( this Stream stream, Byte[] array, Int32 offset, Int32 count, CancellationToken token )
   {
      ArgumentValidator.ValidateNotNullReference( stream );
      ArgumentValidator.ValidateNotNull( nameof( array ), array ).CheckArrayArguments( offset, count );

      if ( count > 0 )
      {
         Int32 bytesRead;
         Int32 originalCount = count;
         do
         {
            bytesRead = await stream.ReadAsync( array, offset, count, token );
            count -= bytesRead;
            offset += bytesRead;
         } while ( bytesRead > 0 && count > 0 );
         count = originalCount - count;
      }
      return count;
   }

   /// <summary>
   /// Helper method to read specific amount of bytes from <see cref="Stream"/> into <see cref="ResizableArray{T}"/>.
   /// The max capacity of <see cref="ResizableArray{T}"/> will be increased if needed in order to fit the bytes.
   /// </summary>
   /// <param name="stream">The stream to read bytes from.</param>
   /// <param name="array">The byte array to read bytes to.</param>
   /// <param name="offset">The offset where to start writing.</param>
   /// <param name="count">The amount of bytes to read.</param>
   /// <param name="token">The <see cref="CancellationToken"/> to use with stream operations.</param>
   /// <returns>Task which completes when last of the required bytes has been read into array, always returning <c>true</c>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="stream"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If <paramref name="offset"/> and <paramref name="count"/> are incorrect.</exception>
   /// <exception cref="EndOfStreamException">If end of stream encountered before given <paramref name="count"/> amount of bytes could be read.</exception>
   public static async ValueTask<Int32> TryReadSpecificAmountAsync( this Stream stream, ResizableArray<Byte> array, Int32 offset, Int32 count, CancellationToken token )
   {
      if ( array != null )
      {
         array.CurrentMaxCapacity = offset + count;
      }

      return await stream.TryReadSpecificAmountAsync( array?.Array, offset, count, token );
   }

   /// <summary>
   /// Helper method to try to read specific amount of bytes from <see cref="Stream"/> into <see cref="ResizableArray{T}"/>.
   /// The max capacity of <see cref="ResizableArray{T}"/> will be increased if needed in order to fit the bytes.
   /// </summary>
   /// <param name="stream">The stream to read bytes from.</param>
   /// <param name="array">The byte array to read bytes to.</param>
   /// <param name="offset">The offset where to start writing.</param>
   /// <param name="count">The amount of bytes to read.</param>
   /// <param name="token">The <see cref="CancellationToken"/> to use with stream operations.</param>
   /// <returns>Task which completes when last of the required bytes has been read into array, always returning <c>true</c>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="stream"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If <paramref name="offset"/> and <paramref name="count"/> are incorrect.</exception>
   public static async ValueTask<Boolean> ReadSpecificAmountAsync( this Stream stream, ResizableArray<Byte> array, Int32 offset, Int32 count, CancellationToken token )
   {
      if ( ( await stream.TryReadSpecificAmountAsync( array, offset, count, token ) ) != count )
      {
         throw new EndOfStreamException();
      }
      return true;
   }

   /// <summary>
   /// This method tries to grow the <see cref="AbstractStreamWithResizableBuffer.Buffer"/> by given <paramref name="count"/>, and then invokes lambda to append to bytes.
   /// </summary>
   /// <param name="writer">This <see cref="StreamWriterWithResizableBuffer"/>.</param>
   /// <param name="count">The amount of bytes to append.</param>
   /// <param name="appender">The lambda to append the bytes.</param>
   /// <returns>The actual amount of bytes that will be taken into account when using <see cref="StreamWriterWithResizableBuffer.FlushAsync"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="StreamWriterWithResizableBuffer"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">If this stream writer is currently unusable (concurrent write or usage of nested buffer created by <see cref="StreamWriterWithResizableBuffer.CreateWithLimitedSizeAndSharedBuffer"/>).</exception>
   public static Int32 AppendToBytes( this StreamWriterWithResizableBuffer writer, Int32 count, Action<Byte[], Int32, Int32> appender )
   {
      Int32 offset;
      (offset, count) = ArgumentValidator.ValidateNotNullReference( writer ).ReserveBufferSegment( count );
      if ( count > 0 )
      {
         appender( writer.Buffer, offset, count );
      }

      return count;
   }

}