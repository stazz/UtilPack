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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UtilPack
{
   /// <summary>
   /// This class wraps <see cref="ResizableArray{T}"/> behind a <see cref="Stream"/> API.
   /// However, the <see cref="ResizableArray{T}"/> instance is not owned by this stream, and the caller is let to coordinate it, so in such, this class is a bit like <see cref="MemoryStream"/> with reusable byte array contents.
   /// </summary>
   public sealed class ResizableArrayStream : Stream
   {
      private readonly ResizableArray<Byte> _buffer;
      private readonly Boolean _readable;
      private readonly Boolean _writeable;

      private readonly Int32 _origin;
      private Int32 _maxLength;
      private Int32 _position;

      /// <summary>
      /// Creates a new instance of <see cref="ResizableArrayStream"/>.
      /// </summary>
      /// <param name="buffer">The <see cref="ResizableArray{T}"/> instance.</param>
      /// <param name="readable">Whether this stream is readable.</param>
      /// <param name="writeable">Whether this stream is writeable.</param>
      /// <param name="position">The initial position within <paramref name="buffer"/> to start reading/writing.</param>
      /// <param name="maxLength">The maximum amount of bytes that can be read or written to this stream.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="buffer"/> is <c>null</c>.</exception>
      public ResizableArrayStream(
         ResizableArray<Byte> buffer,
         Boolean readable = true,
         Boolean writeable = true,
         Int32 position = 0,
         Int32 maxLength = 0
         )
      {
         this._buffer = ArgumentValidator.ValidateNotNull( nameof( buffer ), buffer );
         this._readable = readable;
         this._writeable = writeable;
         this._position = this._origin = position;
         this._maxLength = maxLength;
      }

      /// <inheritdoc />
      public override Boolean CanRead => this._readable;

      /// <inheritdoc />
      public override Boolean CanSeek => true;

      /// <inheritdoc />
      public override Boolean CanWrite => this._writeable;

      /// <inheritdoc />
      public override Int64 Length => 0;

      /// <inheritdoc />
      public override Int64 Position
      {
         get => this._position - this._origin;
         set
         {
            var max = this._maxLength;
            if ( value < 0 || ( max >= 0 && value > max ) )
            {
               throw new ArgumentException();
            }

            var newPos = this._origin + (Int32) value;
            var position = this._position;
            if ( newPos > position )
            {
               this._buffer.CurrentMaxCapacity = newPos;
               Array.Clear( this._buffer.Array, position, newPos - position );
            }
            this._position = newPos;
         }
      }

      /// <summary>
      /// This method does nothing in this class.
      /// </summary>
      public override void Flush()
      {
         // Nothing to do
      }

      /// <summary>
      /// This method always returns <see cref="TaskUtils.CompletedTask"/> without any asynchrony.
      /// </summary>
      /// <param name="cancellationToken">Ignored.</param>
      /// <returns>Always <see cref="TaskUtils.CompletedTask"/>.</returns>
      public
#if !NET40
         override
#endif
         Task FlushAsync( CancellationToken cancellationToken )
      {
         this.Flush();
         return TaskUtils.CompletedTask;
      }

      /// <inheritdoc />
      public override Int64 Seek( Int64 offset, SeekOrigin origin )
      {
         switch ( origin )
         {
            case SeekOrigin.Current:
               offset += this.Position;
               break;
            case SeekOrigin.End:
               offset = this.Position - offset;
               break;
         }

         if ( offset < 0 )
         {
            throw new ArgumentOutOfRangeException( nameof( offset ) );
         }

         this.Position = offset;

         return offset;
      }

      /// <inheritdoc />
      public override void SetLength( Int64 value )
      {
         this._maxLength = checked((Int32) value);
      }

      /// <inheritdoc />
      public override Int32 Read( Byte[] buffer, Int32 offset, Int32 count )
      {
         if ( !this._readable )
         {
            throw new NotSupportedException();
         }
         buffer.CheckArrayArguments( offset, count, true );
         var max = this._maxLength;
         count = Math.Min( count, ( max < 0 ? this._buffer.CurrentMaxCapacity : max ) - this._position );
         if ( count > 0 )
         {
            this._buffer.Array.CopyTo( buffer, ref this._position, offset, count );
         }
         return Math.Max( count, 0 );

      }

      /// <summary>
      /// For this class, this method is just call-through to <see cref="Read(byte[], int, int)"/> method.
      /// </summary>
      /// <param name="buffer">The array to write to.</param>
      /// <param name="offset">The offset in <paramref name="buffer"/> to start writing bytes to.</param>
      /// <param name="count">The maximum amount of bytes to write.</param>
      /// <param name="cancellationToken">This is ignored.</param>
      /// <returns>Always returns task wrapping the result of <see cref="Read(byte[], int, int)"/>.</returns>
      public
#if !NET40
         override
#endif
         Task<Int32> ReadAsync( Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken )
      {
         // TODO task instance caching
         return
#if NET40
            TaskEx
#else
            Task
#endif
            .FromResult( this.Read( buffer, offset, count ) );
      }

      /// <inheritdoc />
      public override void Write( Byte[] buffer, Int32 offset, Int32 count )
      {
         if ( !this._writeable )
         {
            throw new NotSupportedException();
         }
         buffer.CheckArrayArguments( offset, count, false );
         if ( count > 0 )
         {
            Array.Copy( buffer, offset, this._buffer.SetCapacityAndReturnArray( this._position + count ), this._position, count );
            this._position += count;
         }
      }

      /// <summary>
      /// For this class, this method is just call-through to <see cref="Write(byte[], int, int)"/> method.
      /// </summary>
      /// <param name="buffer">The array to read from.</param>
      /// <param name="offset">The offset in <paramref name="buffer"/> to start reading bytes from.</param>
      /// <param name="count">The amount of bytes to read.</param>
      /// <param name="cancellationToken">This is ignored.</param>
      /// <returns>Always returns <see cref="TaskUtils.CompletedTask"/> after calling <see cref="Write(byte[], int, int)"/>.</returns>
      public
#if !NET40
         override
#endif
         Task WriteAsync( Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken )
      {
         this.Write( buffer, offset, count );
         return TaskUtils.CompletedTask;
      }


   }


   /// <summary>
   /// This class is a bit like <see cref="T:System.IO.BufferedStream"/>, but it does not support synchronous API, nor it has any kind of synchronization mechanism for reads and writes.
   /// The <see cref="T:System.IO.BufferedStream"/> has inherent and not-customizable lock mechanism for asynchronous API, which makes it extremely ineffective in most cases (it uses <see cref="SemaphoreSlim"/> which acts as a mutex lock with async API, so simulatenous write and read is impossible).
   /// Furthermore, usually there is some additional state related to reading/writing to stream, which takes care of appropriate locking and synchronization strategies, thus making those aspects at <see cref="System.IO.Stream"/> level just pointless overhead.
   /// </summary>
   /// <remarks>
   /// This class does not support synchronous write/read operations.
   /// </remarks>
   public sealed class DuplexBufferedAsyncStream : Stream
   {
      private const Int32 DEFAULT_BUFFER_SIZE = 0x1000;

      private readonly Stream _stream;

      private readonly Byte[] _readBuffer;
      private Int32 _readPosition;
      private Int32 _readLength;
      private Task<Int32> _cachedReadTask;

      private readonly Byte[] _writeBuffer;
      private Int32 _writePosition;
      private Boolean _flushEvenIfNoData;

      /// <summary>
      /// Creates a new instance of <see cref="DuplexBufferedAsyncStream"/> which wraps given stream and has given buffer sizes.
      /// </summary>
      /// <param name="stream">The underlying stream to buffer.</param>
      /// <param name="readBufferSize">The size of the buffer for read operations.</param>
      /// <param name="writeBufferSize">The size of the buffer for write operations.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c>.</exception>
      /// <remarks>If either <paramref name="readBufferSize"/> or <paramref name="writeBufferSize"/> is less than <c>1</c>, the <c>1</c> will be used instead.</remarks>
      public DuplexBufferedAsyncStream(
         Stream stream,
         Int32 readBufferSize = DEFAULT_BUFFER_SIZE,
         Int32 writeBufferSize = DEFAULT_BUFFER_SIZE
         )
      {
         this._stream = ArgumentValidator.ValidateNotNull( nameof( stream ), stream );
         if ( !stream.CanWrite && !stream.CanRead )
         {
            throw new ObjectDisposedException( nameof( stream ) );
         }

         this._readBuffer = stream.CanRead ? new Byte[Math.Max( 1, readBufferSize )] : null;
         this._writeBuffer = stream.CanWrite ? new Byte[Math.Max( 1, writeBufferSize )] : null;
      }

      /// <inheritdoc />
      public override Boolean CanRead => this._stream.CanRead;

      /// <inheritdoc />
      public override Boolean CanSeek => this._stream.CanSeek;

      /// <inheritdoc />
      public override Boolean CanWrite => this._stream.CanWrite;

      /// <inheritdoc />
      public override Int64 Length => this._stream.Length;

      /// <inheritdoc />
      public override Int64 Position { get => this._stream.Position; set => this._stream.Position = value; }

      /// <summary>
      /// Will call <see cref="IDisposable.Dispose"/> on underlying stream, if <paramref name="disposing"/> is <c>true</c>.
      /// </summary>
      /// <param name="disposing"><c>true</c> if we are disposing ourselves; <c>false</c> if by GC.</param>
      protected override void Dispose( Boolean disposing )
      {
         if ( disposing )
         {
            this._stream.Dispose();
         }
      }

      /// <summary>
      /// This method always throws <see cref="NotSupportedException"/>, as synchronous API is not supported by this class.
      /// </summary>
      public override void Flush()
      {
         throw new NotSupportedException();
      }

      /// <summary>
      /// This method always throws <see cref="NotSupportedException"/>, as synchronous API is not supported by this class.
      /// </summary>
      /// <param name="buffer">This parameter is ignored.</param>
      /// <param name="offset">This parameter is ignored.</param>
      /// <param name="count">This parameter is ignored.</param>
      /// <returns>This method never returns normally, but throws an exception instead.</returns>
      /// <exception cref="NotSupportedException">This exception is thrown always.</exception>
      public override Int32 Read( Byte[] buffer, Int32 offset, Int32 count )
      {
         throw new NotSupportedException();
      }

      /// <summary>
      /// This method always throws <see cref="NotSupportedException"/>, as synchronous API is not supported by this class.
      /// </summary>
      /// <param name="offset">This parameter is ignored.</param>
      /// <param name="origin">This parameter is ignored.</param>
      /// <returns>This method never returns normally, but throws an exception instead.</returns>
      /// <exception cref="NotSupportedException">This exception is thrown always.</exception>
      public override Int64 Seek( Int64 offset, SeekOrigin origin )
      {
         throw new NotSupportedException();
      }

      /// <summary>
      /// This method always throws <see cref="NotSupportedException"/>, as synchronous API is not supported by this class.
      /// </summary>
      /// <param name="value">This parameter is ignored.</param>
      /// <exception cref="NotSupportedException">This exception is thrown always.</exception>
      public override void SetLength( Int64 value )
      {
         throw new NotSupportedException();
      }

      /// <summary>
      /// This method always throws <see cref="NotSupportedException"/>, as synchronous API is not supported by this class.
      /// </summary>
      /// <param name="buffer">This parameter is ignored.</param>
      /// <param name="offset">This parameter is ignored.</param>
      /// <param name="count">This parameter is ignored.</param>
      /// <exception cref="NotSupportedException">This exception is thrown always.</exception>
      public override void Write( Byte[] buffer, Int32 offset, Int32 count )
      {
         throw new NotSupportedException();
      }

      /// <inheritdoc />
      public
#if !NET40
            override
#endif
            Task<Int32> ReadAsync( Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken )
      {
         if ( this._readBuffer == null )
         {
            throw new NotSupportedException( "Reading not supported by underlying stream." );
         }
         buffer.CheckArrayArguments( offset, count, true );
         Task<Int32> retVal;
         var remainingInBuffer = this._readLength - this._readPosition;
         if ( remainingInBuffer > 0 )
         {
            var bytesReadFromBuffer = Math.Min( count, remainingInBuffer );
            Buffer.BlockCopy( this._readBuffer, this._readPosition, buffer, offset, bytesReadFromBuffer );
            this._readPosition += bytesReadFromBuffer;

            var cached = this._cachedReadTask;
            if ( cached != null && cached.Result == bytesReadFromBuffer )
            {
               retVal = cached;
            }
            else
            {
               retVal = this._cachedReadTask =
#if NET40
                     TaskEx
#else
                     Task
#endif
                     .FromResult( bytesReadFromBuffer );
            }
         }
         else
         {
            retVal = this.ReadFromUnderlyingStreamAsync( buffer, offset, count, cancellationToken );
         }

         return retVal;
      }

      /// <inheritdoc />
      public
#if !NET40
            override
#endif
            Task WriteAsync( Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken )
      {
         var thisBuffer = this._writeBuffer;
         if ( thisBuffer == null )
         {
            throw new NotSupportedException( "Writing not supported by underlying stream." );
         }

         buffer.CheckArrayArguments( offset, count, false );
         Task retVal = null;
         if ( count > 0 )
         {
            var remainingInBuffer = thisBuffer.Length - this._writePosition;
            if ( remainingInBuffer >= count )
            {
               Buffer.BlockCopy( buffer, offset, thisBuffer, this._writePosition, count );
               this._writePosition += count;
            }
            else
            {
               retVal = this.WriteToUnderlyingStreamAsync( buffer, offset, count, cancellationToken );
            }
         }

         return retVal ?? TaskUtils.CompletedTask;
      }

      /// <inheritdoc />
      public
#if !NET40
            override
#endif
           Task FlushAsync( CancellationToken cancellationToken )
      {
         return this._writePosition > 0 || this._flushEvenIfNoData ?
            this.FlushToUnderlyingStreamAsync( cancellationToken ) :
            TaskUtils.CompletedTask;
      }

      private async Task<Int32> ReadFromUnderlyingStreamAsync( Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken )
      {
         var thisBuffer = this._readBuffer;
         Int32 bytesRead;
         if ( count >= thisBuffer.Length )
         {
            // Just read directly into target array and skip this buffer
            bytesRead = await this._stream.ReadAsync( buffer, offset, count, cancellationToken );
         }
         else
         {
            bytesRead = await this._stream.ReadAsync( thisBuffer, 0, thisBuffer.Length, cancellationToken );
            if ( bytesRead > 0 )
            {
               var bytesCopied = Math.Min( bytesRead, count );
               Buffer.BlockCopy( thisBuffer, 0, buffer, offset, bytesCopied );
               this._readPosition = bytesCopied;
               this._readLength = bytesRead;
               bytesRead = bytesCopied;
            }
            else
            {
               this._readPosition = this._readLength = 0;
            }
         }

         this._cachedReadTask =
#if NET40
               TaskEx
#else
               Task
#endif
               .FromResult( bytesRead );
         return bytesRead;
      }

      private async Task WriteToUnderlyingStreamAsync( Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken )
      {
         await this.WriteToUnderlyingStreamAsync( cancellationToken );
         await this._stream.WriteAsync( buffer, offset, count, cancellationToken );
         this._flushEvenIfNoData = true;
      }

      private async Task FlushToUnderlyingStreamAsync( CancellationToken cancellationToken )
      {
         await this.WriteToUnderlyingStreamAsync( cancellationToken );
         await this._stream.FlushAsync( cancellationToken );
         this._flushEvenIfNoData = false;
      }

      private Task WriteToUnderlyingStreamAsync( CancellationToken cancellationToken )
      {
         Task retVal;
         if ( this._writePosition > 0 )
         {
            retVal = this._stream.WriteAsync( this._writeBuffer, 0, this._writePosition, cancellationToken );
            this._writePosition = 0;
         }
         else
         {
            retVal = TaskUtils.CompletedTask;
         }

         return retVal;
      }


   }

}
