/*
 * Copyright 2013 Stanislav Muhametsin. All rights Reserved.
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
using UtilPack;

/// <summary>
/// Helper class to contain useful extension methods that should be directly visible to users of this assembly.
/// </summary>
public static partial class E_UtilPack
{
   //   /// <summary>
   //   /// Writes whole contents of <paramref name="array"/> to <paramref name="stream"/>.
   //   /// </summary>
   //   /// <param name="stream">The stream to write bytes to.</param>
   //   /// <param name="array">The bytes to write.</param>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static void Write( this Stream stream, Byte[] array )
   //   {
   //      stream.Write( array, 0, array.Length );
   //   }

   //   /// <summary>
   //   /// Writes <paramref name="count"/> amount of bytes from the start of <paramref name="array"/> to <paramref name="stream"/>.
   //   /// </summary>
   //   /// <param name="stream">The stream to write bytes to.</param>
   //   /// <param name="array">The byte array to use.</param>
   //   /// <param name="count">The amount of bytes from the beginning of <paramref name="array"/> to write.</param>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static void Write( this Stream stream, Byte[] array, Int32 count )
   //   {
   //      stream.Write( array, 0, count );
   //   }

   //   /// <summary>
   //   /// Reads <c>array.Length</c> amount of bytes from <paramref name="stream"/> into <paramref name="array"/>. Does not return until all bytes have been read.
   //   /// </summary>
   //   /// <param name="stream">The stream to read bytes from.</param>
   //   /// <param name="array">The byte array to read bytes to.</param>
   //   /// <remarks>
   //   /// See <see cref="ReadSpecificAmount(Stream, Byte[], Int32, Int32)"/> for exceptions that may occur.
   //   /// </remarks>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static void ReadWholeArray( this Stream stream, Byte[] array )
   //   {
   //      ReadSpecificAmount( stream, array, 0, array.Length );
   //   }

   //   /// <summary>
   //   /// Creates a new byte array of given size and reads all of its contents from the <see cref="Stream"/>.
   //   /// </summary>
   //   /// <param name="stream">The <see cref="Stream"/> to read bytes from.</param>
   //   /// <param name="arraySize">The amount of bytes to read. This wille be the size of the returned array.</param>
   //   /// <returns>The array of bytes read from the <paramref name="stream"/>.</returns>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte[] ReadWholeArray( this Stream stream, Int32 arraySize )
   //   {
   //      var array = new Byte[arraySize];
   //      ReadWholeArray( stream, array );
   //      return array;
   //   }

   //   /// <summary>
   //   /// Reads <paramref name="amount"/> of bytes from <paramref name="stream"/> into <paramref name="array"/> at specific <paramref name="offset"/>. Does not return until all bytes have been read.
   //   /// </summary>
   //   /// <param name="stream">The stream to read bytes from.</param>
   //   /// <param name="array">The byte array to read bytes to.</param>
   //   /// <param name="offset">The array offset of the first byte read.</param>
   //   /// <param name="amount">The amount of bytes to read.</param>
   //   /// <exception cref="ArgumentException">The sum of <paramref name="offset" /> and <paramref name="amount" /> is larger than the <paramref name="array"/> length. </exception>
   //   /// <exception cref="ArgumentNullException"><paramref name="array" /> is null. </exception>
   //   /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset" /> is negative. </exception>
   //   /// <exception cref="IOException">An I/O error occurs. </exception>
   //   /// <exception cref="NotSupportedException">The stream does not support reading. </exception>
   //   /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed. </exception>
   //   /// <exception cref="EndOfStreamException">If end of stream is reached before <paramref name="amount"/> of bytes could be read.</exception>
   //   /// <exception cref="NullReferenceException">If <paramref name="stream"/> is <c>null</c>.</exception>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static void ReadSpecificAmount( this Stream stream, Byte[] array, Int32 offset, Int32 amount )
   //   {
   //      while ( amount > 0 )
   //      {
   //         var amountOfRead = stream.Read( array, offset, amount );
   //         if ( amountOfRead <= 0 )
   //         {
   //            throw new EndOfStreamException( "Source stream ended before reading of " + amount + " byte" + ( amount > 1 ? "s" : "" ) + " could be completed." );
   //         }
   //         amount -= amountOfRead;
   //         offset += amountOfRead;
   //      }
   //   }

   //   /// <summary>
   //   /// Copies <paramref name="amount"/> of bytes from the <paramref name="source"/> stream into the <paramref name="destination"/> stream using <paramref name="buffer"/> as temporary buffer.
   //   /// </summary>
   //   /// <param name="source">The source stream to copy data from.</param>
   //   /// <param name="destination">The destination stream to copy data to.</param>
   //   /// <param name="buffer">The temporary buffer to use.</param>
   //   /// <param name="amount">Amount of bytes to copy.</param>
   //   /// <exception cref="System.ArgumentNullException"><paramref name="buffer" /> is null. </exception>
   //   /// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
   //   /// <exception cref="System.NotSupportedException">The stream does not support reading. </exception>
   //   /// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
   //   /// <exception cref="System.IO.EndOfStreamException">If end of stream is reached before <paramref name="amount"/> of bytes could be read from <paramref name="source"/> stream.</exception>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static void CopyStreamPart( this Stream source, Stream destination, Byte[] buffer, Int64 amount )
   //   {
   //      while ( amount > 0 )
   //      {
   //         var amountOfRead = source.Read( buffer, 0, (Int32) Math.Min( buffer.Length, amount ) );
   //         if ( amountOfRead <= 0 )
   //         {
   //            throw new EndOfStreamException( "Source stream ended before copying of " + amount + " byte" + ( amount > 1 ? "s" : "" ) + " could be completed." );
   //         }
   //         destination.Write( buffer, 0, amountOfRead );
   //         amount -= (UInt32) amountOfRead;
   //      }
   //   }

   //   /// <summary>
   //   /// Copies all remaining contents of the <paramref name="source"/> stream into the <paramref name="destination"/> stream using <paramref name="buffer"/> as temporary buffer. The advantage of this method over <see cref="Stream.CopyTo(Stream)"/> and <see cref="Stream.CopyTo(Stream, Int32)"/> is that this method allows the user to specify buffer directly.
   //   /// </summary>
   //   /// <param name="source">The source stream to copy data from.</param>
   //   /// <param name="destination">The destination stream to copy data to.</param>
   //   /// <param name="buffer">The temporary buffer to use.</param>
   //   /// <exception cref="System.ArgumentNullException"><paramref name="buffer" /> is null. </exception>
   //   /// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
   //   /// <exception cref="System.NotSupportedException">The stream does not support reading. </exception>
   //   /// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static void CopyStream( this Stream source, Stream destination, Byte[] buffer )
   //   {
   //      Int32 amountOfRead;
   //      while ( ( amountOfRead = source.Read( buffer, 0, buffer.Length ) ) > 0 )
   //      {
   //         destination.Write( buffer, 0, amountOfRead );
   //      }
   //   }

   /// <summary>
   /// This is alias for <see cref="Stream.Seek(Int64, SeekOrigin)"/> method with <see cref="SeekOrigin.Current"/> as second parameter.
   /// </summary>
   /// <param name="stream">The <see cref="Stream"/>.</param>
   /// <param name="amount">How many bytes to advance or go back.</param>
   /// <returns>The <paramref name="stream"/>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="stream"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Stream SeekFromCurrent( this Stream stream, Int64 amount )
   {
      stream.Seek( amount, SeekOrigin.Current );
      return stream;
   }

   /// <summary>
   /// This is alias for <see cref="Stream.Seek(Int64, SeekOrigin)"/> method with <see cref="SeekOrigin.Begin"/> as second parameter.
   /// </summary>
   /// <param name="stream">The <see cref="Stream"/>.</param>
   /// <param name="position">How many bytes to seek from the beginning.</param>
   /// <returns>The <paramref name="stream"/>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="stream"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Stream SeekFromBegin( this Stream stream, Int64 position )
   {
      stream.Seek( position, SeekOrigin.Begin );
      return stream;
   }

   //   /// <summary>
   //   /// Reads a single byte from <see cref="Stream"/> and throws an exception if end of stream is encountered.
   //   /// </summary>
   //   /// <param name="stream">The <see cref="Stream"/>.</param>
   //   /// <returns>The next <see cref="Byte"/> of the stream.</returns>
   //   /// <exception cref="EndOfStreamException">If end of the <paramref name="stream"/> is encountered (<see cref="Stream.ReadByte"/> returns <c>-1</c>).</exception>
   //   /// <exception cref="NullReferenceException">If <paramref name="stream"/> is <c>null</c>.</exception>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte ReadByteFromStream( this Stream stream )
   //   {
   //      var b = stream.ReadByte();
   //      if ( b == -1 )
   //      {
   //         throw new EndOfStreamException();
   //      }
   //      return (Byte) b;
   //   }

   //   /// <summary>
   //   /// Reads a whole stream and returns its contents as single byte array.
   //   /// </summary>
   //   /// <param name="stream">The stream to read.</param>
   //   /// <param name="buffer">The optional buffer to use. If not specified, then a buffer of <c>1024</c> bytes will be used. The buffer will only be used if stream does not support querying length and position.</param>
   //   /// <returns>The stream contents as single byte array.</returns>
   //   /// <exception cref="NullReferenceException">If <paramref name="stream"/> is <c>null</c>.</exception>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte[] ReadUntilTheEnd( this Stream stream, Byte[] buffer = null )
   //   {
   //      Int64 arrayLen = -1;
   //      if ( stream.CanSeek )
   //      {
   //         try
   //         {
   //            arrayLen = stream.Length - stream.Position;
   //         }
   //         catch ( NotSupportedException )
   //         {
   //            // stream can't be queried for length or position
   //         }
   //      }

   //      Byte[] retVal;
   //      if ( arrayLen < 0 )
   //      {
   //         // Have to read using the buffer.
   //         if ( buffer == null )
   //         {
   //            buffer = new Byte[1024];
   //         }

   //         using ( var ms = new MemoryStream() )
   //         {
   //            Int32 read;
   //            while ( ( read = stream.Read( buffer, 0, buffer.Length ) ) > 0 )
   //            {
   //               ms.Write( buffer, 0, read );
   //            }
   //            retVal = ms.ToArray();
   //         }
   //      }
   //      else if ( arrayLen == 0 )
   //      {
   //         retVal = Empty<Byte>.Array;
   //      }
   //      else
   //      {
   //         retVal = new Byte[arrayLen];
   //         stream.ReadWholeArray( retVal );
   //      }

   //      return retVal;
   //   }

   /// <summary>
   /// Reads a single byte at specified index in byte array.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index to read byte at. Will be incremented by one.</param>
   /// <returns>The byte at specified index.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte ReadByteFromBytes( this Byte[] array, ref Int32 idx )
   {
      return array[idx++];
   }

   /// <summary>
   /// Reads a single byte as <see cref="SByte"/> at specified index in byte array.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index to read byte at. Will be incremented by one.</param>
   /// <returns>The byte at specified index casted to <see cref="SByte"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static SByte ReadSByteFromBytes( this Byte[] array, ref Int32 idx )
   {
      return (SByte) array[idx++];
   }

   #region Little-Endian Conversions

   /// <summary>
   /// Reads <see cref="Int16"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 2.</param>
   /// <returns>The decoded <see cref="Int16"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int16 ReadInt16LEFromBytes( this Byte[] array, ref Int32 idx )
   {
      idx += 2;
      return (Int16) ( ( array[idx - 1] << 8 ) | array[idx - 2] );
   }

   /// <summary>
   /// Reads <see cref="Int16"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Int16"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int16 ReadInt16LEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return (Int16) ( ( array[idx + 1] << 8 ) | array[idx] );
   }

   /// <summary>
   /// Reads <see cref="UInt16"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 2.</param>
   /// <returns>The decoded <see cref="UInt16"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt16 ReadUInt16LEFromBytes( this Byte[] array, ref Int32 idx )
   {
      return (UInt16) ReadInt16LEFromBytes( array, ref idx );
   }

   /// <summary>
   /// Reads <see cref="UInt16"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="UInt16"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt16 ReadUInt16LEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return (UInt16) ReadInt16LEFromBytesNoRef( array, idx );
   }

   /// <summary>
   /// Reads <see cref="Int32"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 4.</param>
   /// <returns>The decoded <see cref="Int32"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int32 ReadInt32LEFromBytes( this Byte[] array, ref Int32 idx )
   {
      idx += 4;
      return ( array[idx - 1] << 24 ) | ( array[idx - 2] << 16 ) | ( array[idx - 3] << 8 ) | array[idx - 4];
   }

   /// <summary>
   /// Reads <see cref="Int32"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>Decoded <see cref="Int32"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int32 ReadInt32LEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return ( array[idx + 3] << 24 ) | ( array[idx + 2] << 16 ) | ( array[idx + 1] << 8 ) | array[idx];
   }

   /// <summary>
   /// Reads <see cref="UInt32"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 4.</param>
   /// <returns>The decoded <see cref="UInt32"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt32 ReadUInt32LEFromBytes( this Byte[] array, ref Int32 idx )
   {
      return (UInt32) ReadInt32LEFromBytes( array, ref idx );
   }

   /// <summary>
   /// Reads <see cref="UInt32"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="UInt32"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt32 ReadUInt32LEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return (UInt32) ReadInt32LEFromBytesNoRef( array, idx );
   }

   /// <summary>
   /// Reads <see cref="Int64"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 8.</param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int64 ReadInt64LEFromBytes( this Byte[] array, ref Int32 idx )
   {
      idx += 8;
      return ( ( (Int64) ReadInt32LEFromBytesNoRef( array, idx - 4 ) ) << 32 ) | ( ( (UInt32) ReadInt32LEFromBytesNoRef( array, idx - 8 ) ) );
   }

   /// <summary>
   /// Reads <see cref="Int64"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int64 ReadInt64LEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return ( ( (Int64) ReadInt32LEFromBytesNoRef( array, idx + 4 ) ) << 32 ) | ( ( (UInt32) ReadInt32LEFromBytesNoRef( array, idx ) ) );
   }

   /// <summary>
   /// Reads <see cref="Int64"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 8.</param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt64 ReadUInt64LEFromBytes( this Byte[] array, ref Int32 idx )
   {
      return (UInt64) ReadInt64LEFromBytes( array, ref idx );
   }

   /// <summary>
   /// Reads <see cref="Int64"/> starting at specified index in byte array using little-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt64 ReadUInt64LEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return (UInt64) ReadInt64LEFromBytes( array, ref idx );
   }

   ///// <summary>
   ///// Reads Int32 bits starting at specified index in byte array in little-endian order and changes value to <see cref="Single"/>.
   ///// </summary>
   ///// <param name="array">The byte array.</param>
   ///// <param name="idx">The index of array to start reading. Will be incremented by 4.</param>
   ///// <returns>The decoded <see cref="Single"/>.</returns>
   ///// <exception cref="NullReferenceException">If <paramref name="array"/> is <c>null</c>.</exception>
   ///// <remarks>This method is <c>unsafe</c>.</remarks>
   //public static unsafe Single ReadSingleLEFromBytesUnsafe( this Byte[] array, ref Int32 idx )
   //{
   //   var value = array.ReadInt32LEFromBytes( ref idx );
   //   return *(Single*) ( &value );
   //}

   /// <summary>
   /// Reads Int32 bits starting at specified index in byte array in little-endian order and changes value to <see cref="Single"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 4.</param>
   /// <returns>The decoded <see cref="Single"/>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="array"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Single ReadSingleLEFromBytes( this Byte[] array, ref Int32 idx )
   {
      if ( BitConverter.IsLittleEndian )
      {
         idx += sizeof( Single );
         return BitConverter.ToSingle( array, idx - sizeof( Single ) );
      }
      else
      {
         // Read big-endian Int32, get bytes for it, and convert back to single
         return BitConverter.ToSingle( BitConverter.GetBytes( array.ReadInt32BEFromBytes( ref idx ) ), 0 );

      }
   }

   ///// <summary>
   ///// Reads Int32 bits starting at specified index in byte array in little-endian order and changes value to <see cref="Single"/>.
   ///// </summary>
   ///// <param name="array">The byte array.</param>
   ///// <param name="idx">The index of array to start reading.</param>
   ///// <returns>The decoded <see cref="Single"/>.</returns>
   ///// <exception cref="NullReferenceException">If <paramref name="array"/> is <c>null</c>.</exception>
   ///// <remarks>This code will use <c>unsafe</c> method.</remarks>
   //public static Single ReadSingleLEFromBytesUnsafeNoRef( this Byte[] array, Int32 idx )
   //{
   //   return ReadSingleLEFromBytesUnsafe( array, ref idx );
   //}

   /// <summary>
   /// Reads Int32 bits starting at specified index in byte array in little-endian order and changes value to <see cref="Single"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Single"/>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="array"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Single ReadSingleLEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return ReadSingleLEFromBytes( array, ref idx );
   }

   /// <summary>
   /// Reads Int64 bits starting at specified index in byte array in little-endian order and changes value to <see cref="Double"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 8.</param>
   /// <returns>The decoded <see cref="Double"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Double ReadDoubleLEFromBytes( this Byte[] array, ref Int32 idx )
   {
      return BitConverter.Int64BitsToDouble( array.ReadInt64LEFromBytes( ref idx ) );
   }

   /// <summary>
   /// Reads Int64 bits starting at specified index in byte array in little-endian order and changes value to <see cref="Double"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Double"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Double ReadDoubleLEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return BitConverter.Int64BitsToDouble( array.ReadInt64LEFromBytesNoRef( idx ) );
   }

   /// <summary>
   /// Reads given amount of integers from byte array starting at given offset, and returns the integers as array.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start reading. It will be incremented by <paramref name="intArrayLen"/> * 4.</param>
   /// <param name="intArrayLen">The amount of integers to read.</param>
   /// <returns>The integer array.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int32[] ReadInt32ArrayLEFromBytes( this Byte[] array, ref Int32 idx, Int32 intArrayLen )
   {
      var result = new Int32[intArrayLen];
      for ( var i = 0; i < result.Length; ++i )
      {
         result[i] = array.ReadInt32LEFromBytes( ref idx );
      }
      return result;
   }

   /// <summary>
   /// Reads given amount of unsigned integers from byte array starting at given offset, and returns the unsigned integers as array.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start reading. It will be incremented by <paramref name="intArrayLen"/> * 4.</param>
   /// <param name="intArrayLen">The amount of unsigned integers to read.</param>
   /// <returns>The unsigned integer array.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt32[] ReadUInt32ArrayLEFromBytes( this Byte[] array, ref Int32 idx, Int32 intArrayLen )
   {
      var result = new UInt32[intArrayLen];
      for ( var i = 0; i < result.Length; ++i )
      {
         result[i] = array.ReadUInt32LEFromBytes( ref idx );
      }
      return result;
   }

   /// <summary>
   /// Writes a given <see cref="Int16"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 2.</param>
   /// <param name="value">The <see cref="Int16"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt16LEToBytes( this Byte[] array, ref Int32 idx, Int16 value )
   {
      array[idx] = (Byte) value;
      array[idx + 1] = (Byte) ( value >> 8 );
      idx += 2;
      return array;
   }

   /// <summary>
   /// Writes a given <see cref="Int16"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Int16"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt16LEToBytesNoRef( this Byte[] array, Int32 idx, Int16 value )
   {
      return WriteInt16LEToBytes( array, ref idx, value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt16"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 2.</param>
   /// <param name="value">The <see cref="UInt16"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt16LEToBytes( this Byte[] array, ref Int32 idx, UInt16 value )
   {
      return WriteInt16LEToBytes( array, ref idx, (Int16) value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt16"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="UInt16"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt16LEToBytesNoRef( this Byte[] array, Int32 idx, UInt16 value )
   {
      return WriteInt16LEToBytes( array, ref idx, (Int16) value );
   }

   /// <summary>
   /// Writes a given <see cref="Int32"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 4.</param>
   /// <param name="value">The <see cref="Int32"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt32LEToBytes( this Byte[] array, ref Int32 idx, Int32 value )
   {
      array[idx] = (Byte) value;
      array[idx + 1] = (Byte) ( value >> 8 );
      array[idx + 2] = (Byte) ( value >> 16 );
      array[idx + 3] = (Byte) ( value >> 24 );
      idx += 4;
      return array;
   }

   /// <summary>
   /// Writes a given <see cref="Int32"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Int32"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt32LEToBytesNoRef( this Byte[] array, Int32 idx, Int32 value )
   {
      return WriteInt32LEToBytes( array, ref idx, value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt32"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 4.</param>
   /// <param name="value">The <see cref="UInt32"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt32LEToBytes( this Byte[] array, ref Int32 idx, UInt32 value )
   {
      return WriteInt32LEToBytes( array, ref idx, (Int32) value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt32"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="UInt32"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt32LEToBytesNoRef( this Byte[] array, Int32 idx, UInt32 value )
   {
      return WriteInt32LEToBytes( array, ref idx, (Int32) value );
   }

   /// <summary>
   /// Writes a given <see cref="Int64"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 8.</param>
   /// <param name="value">The <see cref="Int64"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt64LEToBytes( this Byte[] array, ref Int32 idx, Int64 value )
   {
      array[idx] = (Byte) value;
      array[idx + 1] = (Byte) ( value >> 8 );
      array[idx + 2] = (Byte) ( value >> 16 );
      array[idx + 3] = (Byte) ( value >> 24 );
      array[idx + 4] = (Byte) ( value >> 32 );
      array[idx + 5] = (Byte) ( value >> 40 );
      array[idx + 6] = (Byte) ( value >> 48 );
      array[idx + 7] = (Byte) ( value >> 56 );
      idx += 8;
      return array;
   }

   /// <summary>
   /// Writes a given <see cref="Int64"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Int64"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt64LEToBytesNoRef( this Byte[] array, Int32 idx, Int64 value )
   {
      return WriteInt64LEToBytes( array, ref idx, value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt64"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 8.</param>
   /// <param name="value">The <see cref="UInt64"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt64LEToBytes( this Byte[] array, ref Int32 idx, UInt64 value )
   {
      return WriteInt64LEToBytes( array, ref idx, (Int64) value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt64"/> in byte array starting at specified offset, using little-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="UInt64"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt64LEToBytesNoRef( this Byte[] array, Int32 idx, UInt64 value )
   {
      return WriteInt64LEToBytes( array, ref idx, (Int64) value );
   }

   ///// <summary>
   ///// Writes Int32 bits of given <see cref="Single"/> value in little-endian orger to given array starting at specified offset.
   ///// </summary>
   ///// <param name="array">The byte array.</param>
   ///// <param name="idx">The offset to start writing. Will be incremented by 4.</param>
   ///// <param name="value">The <see cref="Single"/> value to write.</param>
   ///// <returns>The <paramref name="array"/>.</returns>
   ///// <remarks>This method is <c>unsafe</c>.</remarks>
   //public static unsafe Byte[] WriteSingleLEToBytesUnsafe( this Byte[] array, ref Int32 idx, Single value )
   //{
   //   return WriteInt32LEToBytes( array, ref idx, *(Int32*) ( &value ) );
   //}

   /// <summary>
   /// Writes Int32 bits of given <see cref="Single"/> value in little-endian orger to given array starting at specified offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 4.</param>
   /// <param name="value">The <see cref="Single"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteSingleLEToBytes( this Byte[] array, ref Int32 idx, Single value )
   {
      if ( BitConverter.IsLittleEndian )
      {
         BitConverter.GetBytes( value ).CopyTo( array, idx );
         idx += sizeof( Single );
      }
      else
      {
         var arr = BitConverter.GetBytes( value );
         array[idx] = arr[3];
         array[idx + 1] = arr[2];
         array[idx + 2] = arr[1];
         array[idx + 3] = arr[0];
         idx += sizeof( Single );
      }
      return array;
   }

   /// <summary>
   /// Writes Int32 bits of given <see cref="Single"/> value in little-endian orger to given array starting at specified offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Single"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteSingleLEToBytesNoRef( this Byte[] array, Int32 idx, Single value )
   {
      return WriteSingleLEToBytes( array, ref idx, value );
   }

   ///// <summary>
   ///// Writes Int32 bits of given <see cref="Single"/> value in little-endian orger to given array starting at specified offset.
   ///// </summary>
   ///// <param name="array">The byte array.</param>
   ///// <param name="idx">The offset to start writing.</param>
   ///// <param name="value">The <see cref="Single"/> value to write.</param>
   ///// <returns>The <paramref name="array"/>.</returns>
   ///// <remarks>This code will use <c>unsafe</c> method.</remarks>
   //public static Byte[] WriteSingleLEToBytesUnsafeNoRef( this Byte[] array, Int32 idx, Single value )
   //{
   //   return WriteSingleLEToBytesUnsafe( array, ref idx, value );
   //}

   /// <summary>
   /// Writes Int64 bits of given <see cref="Double"/> value in little-endian order to given array starting at specified offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 8.</param>
   /// <param name="value">The <see cref="Double"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteDoubleLEToBytes( this Byte[] array, ref Int32 idx, Double value )
   {
      return array.WriteInt64LEToBytes( ref idx, BitConverter.DoubleToInt64Bits( value ) );
   }

   /// <summary>
   /// Writes Int64 bits of given <see cref="Double"/> value in little-endian order to given array starting at specified offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Double"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteDoubleLEToBytesNoRef( this Byte[] array, Int32 idx, Double value )
   {
      return array.WriteDoubleLEToBytes( ref idx, value );
   }

   #endregion


   #region Big-Endian Conversions

   /// <summary>
   /// Reads <see cref="Int16"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 2.</param>
   /// <returns>The decoded <see cref="Int16"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int16 ReadInt16BEFromBytes( this Byte[] array, ref Int32 idx )
   {
      idx += 2;
      return (Int16) ( ( array[idx - 2] << 8 ) | array[idx - 1] );
   }

   /// <summary>
   /// Reads <see cref="Int16"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Int16"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int16 ReadInt16BEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return (Int16) ( ( array[idx] << 8 ) | array[idx + 1] );
   }

   /// <summary>
   /// Reads <see cref="UInt16"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 2.</param>
   /// <returns>The decoded <see cref="UInt16"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt16 ReadUInt16BEFromBytes( this Byte[] array, ref Int32 idx )
   {
      return (UInt16) ReadInt16BEFromBytes( array, ref idx );
   }

   /// <summary>
   /// Reads <see cref="UInt16"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="UInt16"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt16 ReadUInt16BEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return (UInt16) ReadInt16BEFromBytesNoRef( array, idx );
   }

   /// <summary>
   /// Reads <see cref="Int32"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 4.</param>
   /// <returns>The decoded <see cref="Int32"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int32 ReadInt32BEFromBytes( this Byte[] array, ref Int32 idx )
   {
      idx += 4;
      return ( array[idx - 4] << 24 ) | ( array[idx - 3] << 16 ) | ( array[idx - 2] << 8 ) | array[idx - 1];
   }

   /// <summary>
   /// Reads <see cref="Int32"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>Decoded <see cref="Int32"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int32 ReadInt32BEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return ( array[idx] << 24 ) | ( array[idx + 1] << 16 ) | ( array[idx + 2] << 8 ) | array[idx + 3];
   }

   /// <summary>
   /// Reads <see cref="UInt32"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 4.</param>
   /// <returns>The decoded <see cref="UInt32"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt32 ReadUInt32BEFromBytes( this Byte[] array, ref Int32 idx )
   {
      return (UInt32) ReadInt32BEFromBytes( array, ref idx );
   }

   /// <summary>
   /// Reads <see cref="UInt32"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="UInt32"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt32 ReadUInt32BEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return (UInt32) ReadInt32BEFromBytesNoRef( array, idx );
   }

   /// <summary>
   /// Reads <see cref="Int64"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 8.</param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int64 ReadInt64BEFromBytes( this Byte[] array, ref Int32 idx )
   {
      idx += 8;
      return ( ( (Int64) ReadInt32BEFromBytesNoRef( array, idx - 8 ) ) << 32 ) | ( ( (UInt32) ReadInt32BEFromBytesNoRef( array, idx - 4 ) ) );
   }

   /// <summary>
   /// Reads <see cref="Int64"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int64 ReadInt64BEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return ( ( (Int64) ReadInt32BEFromBytesNoRef( array, idx ) ) << 32 ) | ( ( (UInt32) ReadInt32BEFromBytesNoRef( array, idx + 4 ) ) );
   }

   /// <summary>
   /// Reads <see cref="Int64"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 8.</param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt64 ReadUInt64BEFromBytes( this Byte[] array, ref Int32 idx )
   {
      return (UInt64) ReadInt64BEFromBytes( array, ref idx );
   }

   /// <summary>
   /// Reads <see cref="Int64"/> starting at specified index in byte array using big-endian decoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt64 ReadUInt64BEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return (UInt64) ReadInt64BEFromBytesNoRef( array, idx );
   }

   ///// <summary>
   ///// Reads Int32 bits starting at specified index in byte array in big-endian order and changes value to <see cref="Single"/>.
   ///// </summary>
   ///// <param name="array">The byte array.</param>
   ///// <param name="idx">The index of array to start reading. Will be incremented by 4.</param>
   ///// <returns>The decoded <see cref="Single"/>.</returns>
   ///// <exception cref="NullReferenceException">If <paramref name="array"/> is <c>null</c>.</exception>
   ///// <remarks>This is <c>unsafe</c> method.</remarks>
   //public static unsafe Single ReadSingleBEFromBytesUnsafe( this Byte[] array, ref Int32 idx )
   //{
   //   var value = array.ReadInt32BEFromBytes( ref idx );
   //   return *(Single*) ( &value );
   //}

   /// <summary>
   /// Reads Int32 bits starting at specified index in byte array in big-endian order and changes value to <see cref="Single"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 4.</param>
   /// <returns>The decoded <see cref="Single"/>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="array"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Single ReadSingleBEFromBytes( this Byte[] array, ref Int32 idx )
   {
      if ( BitConverter.IsLittleEndian )
      {
         // Read little-endian Int32, get bytes for it, and convert back to single
         return BitConverter.ToSingle( BitConverter.GetBytes( array.ReadInt32LEFromBytes( ref idx ) ), 0 );
      }
      else
      {
         idx += sizeof( Single );
         return BitConverter.ToSingle( array, idx - sizeof( Single ) );
      }
   }

   ///// <summary>
   ///// Reads Int32 bits starting at specified index in byte array in big-endian order and changes value to <see cref="Single"/>.
   ///// </summary>
   ///// <param name="array">The byte array.</param>
   ///// <param name="idx">The index of array to start reading.</param>
   ///// <returns>The decoded <see cref="Single"/>.</returns>
   ///// <exception cref="NullReferenceException">If <paramref name="array"/> is <c>null</c>.</exception>
   ///// <remarks>This code will use <c>unsafe</c> method.</remarks>
   //public static Single ReadSingleBEFromBytesUnsafeNoRef( this Byte[] array, Int32 idx )
   //{
   //   return ReadSingleBEFromBytesUnsafe( array, ref idx );
   //}

   /// <summary>
   /// Reads Int32 bits starting at specified index in byte array in big-endian order and changes value to <see cref="Single"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Single"/>.</returns>
   /// <exception cref="NullReferenceException">If <paramref name="array"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Single ReadSingleBEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return ReadSingleBEFromBytes( array, ref idx );
   }

   /// <summary>
   /// Reads Int64 bits starting at specified index in byte array in big-endian order and changes value to <see cref="Double"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading. Will be incremented by 8.</param>
   /// <returns>The decoded <see cref="Double"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Double ReadDoubleBEFromBytes( this Byte[] array, ref Int32 idx )
   {
      return BitConverter.Int64BitsToDouble( array.ReadInt64BEFromBytes( ref idx ) );
   }

   /// <summary>
   /// Reads Int64 bits starting at specified index in byte array in big-endian order and changes value to <see cref="Double"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index of array to start reading.</param>
   /// <returns>The decoded <see cref="Double"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Double ReadDoubleBEFromBytesNoRef( this Byte[] array, Int32 idx )
   {
      return BitConverter.Int64BitsToDouble( array.ReadInt64BEFromBytesNoRef( idx ) );
   }

   /// <summary>
   /// Reads given amount of integers from byte array starting at given offset, and returns the integers as array.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start reading. It will be incremented by <paramref name="intArrayLen"/> * 4.</param>
   /// <param name="intArrayLen">The amount of integers to read.</param>
   /// <returns>The integer array.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int32[] ReadInt32ArrayBEFromBytes( this Byte[] array, ref Int32 idx, Int32 intArrayLen )
   {
      var result = new Int32[intArrayLen];
      for ( var i = 0; i < result.Length; ++i )
      {
         result[i] = array.ReadInt32BEFromBytes( ref idx );
      }
      return result;
   }

   /// <summary>
   /// Reads given amount of unsigned integers from byte array starting at given offset, and returns the unsigned integers as array.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start reading. It will be incremented by <paramref name="intArrayLen"/> * 4.</param>
   /// <param name="intArrayLen">The amount of unsigned integers to read.</param>
   /// <returns>The unsigned integer array.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static UInt32[] ReadUInt32ArrayBEFromBytes( this Byte[] array, ref Int32 idx, Int32 intArrayLen )
   {
      var result = new UInt32[intArrayLen];
      for ( var i = 0; i < result.Length; ++i )
      {
         result[i] = array.ReadUInt32BEFromBytes( ref idx );
      }
      return result;
   }

   /// <summary>
   /// Writes a given <see cref="Int16"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 2.</param>
   /// <param name="value">The <see cref="Int16"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt16BEToBytes( this Byte[] array, ref Int32 idx, Int16 value )
   {
      array[idx] = (Byte) ( value >> 8 );
      array[idx + 1] = (Byte) value;
      idx += 2;
      return array;
   }

   /// <summary>
   /// Writes a given <see cref="Int16"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Int16"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt16BEToBytesNoRef( this Byte[] array, Int32 idx, Int16 value )
   {
      return WriteInt16BEToBytes( array, ref idx, value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt16"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 2.</param>
   /// <param name="value">The <see cref="UInt16"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt16BEToBytes( this Byte[] array, ref Int32 idx, UInt16 value )
   {
      return WriteInt16BEToBytes( array, ref idx, (Int16) value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt16"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="UInt16"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt16BEToBytesNoRef( this Byte[] array, Int32 idx, UInt16 value )
   {
      return WriteInt16BEToBytes( array, ref idx, (Int16) value );
   }

   /// <summary>
   /// Writes a given <see cref="Int32"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 4.</param>
   /// <param name="value">The <see cref="Int32"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt32BEToBytes( this Byte[] array, ref Int32 idx, Int32 value )
   {
      array[idx] = (Byte) ( value >> 24 );
      array[idx + 1] = (Byte) ( value >> 16 );
      array[idx + 2] = (Byte) ( value >> 8 );
      array[idx + 3] = (Byte) value;

      idx += 4;
      return array;
   }

   /// <summary>
   /// Writes a given <see cref="Int32"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Int32"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt32BEToBytesNoRef( this Byte[] array, Int32 idx, Int32 value )
   {
      return WriteInt32BEToBytes( array, ref idx, value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt32"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 4.</param>
   /// <param name="value">The <see cref="UInt32"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt32BEToBytes( this Byte[] array, ref Int32 idx, UInt32 value )
   {
      return WriteInt32BEToBytes( array, ref idx, (Int32) value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt32"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="UInt32"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt32BEToBytesNoRef( this Byte[] array, Int32 idx, UInt32 value )
   {
      return WriteInt32BEToBytes( array, ref idx, (Int32) value );
   }

   /// <summary>
   /// Writes a given <see cref="Int64"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 8.</param>
   /// <param name="value">The <see cref="Int64"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt64BEToBytes( this Byte[] array, ref Int32 idx, Int64 value )
   {
      array[idx] = (Byte) ( value >> 56 );
      array[idx + 1] = (Byte) ( value >> 48 );
      array[idx + 2] = (Byte) ( value >> 40 );
      array[idx + 3] = (Byte) ( value >> 32 );
      array[idx + 4] = (Byte) ( value >> 24 );
      array[idx + 5] = (Byte) ( value >> 16 );
      array[idx + 6] = (Byte) ( value >> 8 );
      array[idx + 7] = (Byte) value;

      idx += 8;
      return array;
   }

   /// <summary>
   /// Writes a given <see cref="Int64"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Int64"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt64BEToBytesNoRef( this Byte[] array, Int32 idx, Int64 value )
   {
      return WriteInt64BEToBytes( array, ref idx, value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt64"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 8.</param>
   /// <param name="value">The <see cref="UInt64"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt64BEToBytes( this Byte[] array, ref Int32 idx, UInt64 value )
   {
      return WriteInt64BEToBytes( array, ref idx, (Int64) value );
   }

   /// <summary>
   /// Writes a given <see cref="UInt64"/> in byte array starting at specified offset, using big-endian encoding.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="UInt64"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteUInt64BEToBytesNoRef( this Byte[] array, Int32 idx, UInt64 value )
   {
      return WriteInt64BEToBytes( array, ref idx, (Int64) value );
   }

   ///// <summary>
   ///// Writes Int32 bits of given <see cref="Single"/> value in big-endian orger to given array starting at specified offset.
   ///// </summary>
   ///// <param name="array">The byte array.</param>
   ///// <param name="idx">The offset to start writing. Will be incremented by 4.</param>
   ///// <param name="value">The <see cref="Single"/> value to write.</param>
   ///// <returns>The <paramref name="array"/>.</returns>
   ///// <remarks>This method is <c>unsafe</c>.</remarks>
   //public static unsafe Byte[] WriteSingleBEToBytesUnsafe( this Byte[] array, ref Int32 idx, Single value )
   //{
   //   return WriteInt32BEToBytes( array, ref idx, *(Int32*) ( &value ) );
   //}

   /// <summary>
   /// Writes Int32 bits of given <see cref="Single"/> value in big-endian orger to given array starting at specified offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 4.</param>
   /// <param name="value">The <see cref="Single"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteSingleBEToBytes( this Byte[] array, ref Int32 idx, Single value )
   {
      if ( BitConverter.IsLittleEndian )
      {
         var arr = BitConverter.GetBytes( value );
         array[idx] = arr[3];
         array[idx + 1] = arr[2];
         array[idx + 2] = arr[1];
         array[idx + 3] = arr[0];
         idx += 4;
      }
      else
      {
         BitConverter.GetBytes( value ).CopyTo( array, idx );
         idx += sizeof( Single );
      }
      return array;
   }

   ///// <summary>
   ///// Writes Int32 bits of given <see cref="Single"/> value in big-endian orger to given array starting at specified offset.
   ///// </summary>
   ///// <param name="array">The byte array.</param>
   ///// <param name="idx">The offset to start writing.</param>
   ///// <param name="value">The <see cref="Single"/> value to write.</param>
   ///// <returns>The <paramref name="array"/>.</returns>
   ///// <remarks>This code uses <c>unsafe</c> method.</remarks>
   //public static Byte[] WriteSingleBEToBytesUnsafeNoRef( this Byte[] array, Int32 idx, Single value )
   //{
   //   return WriteSingleBEToBytesUnsafe( array, ref idx, value );
   //}

   /// <summary>
   /// Writes Int32 bits of given <see cref="Single"/> value in big-endian orger to given array starting at specified offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Single"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteSingleBEToBytesNoRef( this Byte[] array, Int32 idx, Single value )
   {
      return WriteSingleBEToBytes( array, ref idx, value );
   }

   /// <summary>
   /// Writes Int64 bits of given <see cref="Double"/> value in big-endian order to given array starting at specified offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by 8.</param>
   /// <param name="value">The <see cref="Double"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteDoubleBEToBytes( this Byte[] array, ref Int32 idx, Double value )
   {
      return array.WriteInt64BEToBytes( ref idx, BitConverter.DoubleToInt64Bits( value ) );
   }

   /// <summary>
   /// Writes Int64 bits of given <see cref="Double"/> value in big-endian order to given array starting at specified offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing.</param>
   /// <param name="value">The <see cref="Double"/> value to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteDoubleBEToBytesNoRef( this Byte[] array, Int32 idx, Double value )
   {
      return WriteDoubleBEToBytes( array, ref idx, value );
   }

   #endregion


   /// <summary>
   /// Reads a string starting at specified index in byte array, assuming that each byte is an ASCII character.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="offset">The offset to start reading.</param>
   /// <param name="length">The amount of bytes to read.</param>
   /// <returns>ASCII-encoded string with <paramref name="length"/> characters.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static String ReadASCIIStringFromBytes( this Byte[] array, Int32 offset, Int32 length )
   {
      var charBuf = new Char[length];
      for ( var i = 0; i < length; ++i )
      {
         charBuf[i] = (Char) array[i + offset];
      }
      return new String( charBuf, 0, length );
   }

   /// <summary>
   /// Reads a string starting at specified index in byte array, assuming that the string will be terminated by zero byte or by the array ending.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start reading. This will be incremented by amount of bytes read, including the zero byte.</param>
   /// <param name="encoding">The encoding to use to decode string.</param>
   /// <returns>The decoded string.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="encoding"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static String ReadZeroTerminatedStringFromBytes( this Byte[] array, ref Int32 idx, Encoding encoding )
   {
      var curIdx = idx;
      while ( curIdx < array.Length && array[curIdx] != 0 )
      {
         ++curIdx;
      }
      var result = encoding.GetString( array, idx, curIdx - idx );
      idx = curIdx + 1;
      return result;
   }

   /// <summary>
   /// Reads a string at specified index in byte array using specified <see cref="Encoding"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start reading. This will be incremeted by <paramref name="byteLen"/>.</param>
   /// <param name="byteLen">The amount of bytes to read.</param>
   /// <param name="encoding">The <see cref="Encoding"/> to use.</param>
   /// <returns>The string decoded using a given <paramref name="encoding"/>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="encoding"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static String ReadStringWithEncoding( this Byte[] array, ref Int32 idx, Int32 byteLen, Encoding encoding )
   {
      ArgumentValidator.ValidateNotNull( "Encoding", encoding );
      idx += byteLen;
      return encoding.GetString( array, idx - byteLen, byteLen );
   }

   private const Int32 GUID_SIZE = 16;

   /// <summary>
   /// Reads a <see cref="Guid"/> from specified offset in byte array.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start reading. Will be incremented by 16.</param>
   /// <returns>The decoded <see cref="Guid"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Guid ReadGUIDFromBytes( this Byte[] array, ref Int32 idx )
   {
      var arrayCopy = array.CreateArrayCopy( idx, GUID_SIZE );
      idx += GUID_SIZE;
      return new Guid( arrayCopy );
   }


   //   /// <summary>
   //   /// Creates a new byte array, which will be a copy of given byte array.
   //   /// </summary>
   //   /// <param name="sourceArray">The array to copy bytes from.</param>
   //   /// <returns>A new array having its contents copied from <paramref name="sourceArray"/>.</returns>
   //   /// <remarks>
   //   /// The <see cref="Buffer.BlockCopy(Array, Int32, Array, Int32, Int32)"/> method will be used to copy bytes.
   //   /// </remarks>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte[] CreateBlockCopy( this Byte[] sourceArray )
   //   {
   //      var idx = 0;
   //      return sourceArray.IsNullOrEmpty() ? sourceArray : sourceArray.CreateAndBlockCopyTo( ref idx, sourceArray.Length );
   //   }

   //   /// <summary>
   //   /// Creates a new byte array, which will be a copy of given byte array.
   //   /// </summary>
   //   /// <param name="sourceArray">The array to copy bytes from.</param>
   //   /// <param name="count">The amount of bytes to copy.</param>
   //   /// <returns>A new array having its contents copied from <paramref name="sourceArray"/>.</returns>
   //   /// <remarks>
   //   /// The <see cref="Buffer.BlockCopy(Array, Int32, Array, Int32, Int32)"/> method will be used to copy bytes.
   //   /// </remarks>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte[] CreateBlockCopy( this Byte[] sourceArray, Int32 count )
   //   {
   //      var idx = 0;
   //      return sourceArray.IsNullOrEmpty() ? sourceArray : sourceArray.CreateAndBlockCopyTo( ref idx, count );
   //   }

   //   /// <summary>
   //   /// Creates a new byte array, which will have given amount of bytes copied from given source array starting at specified index.
   //   /// </summary>
   //   /// <param name="sourceArray">The array to copy bytes from.</param>
   //   /// <param name="sourceArrayIndex">The offset to start copying. This will be incremented by <paramref name="amount"/>.</param>
   //   /// <param name="amount">The amount of bytes to copy.</param>
   //   /// <returns>A new array having its contents copied from <paramref name="sourceArray"/> starting from <paramref name="sourceArrayIndex"/> and having <paramref name="amount"/> elements.</returns>
   //   /// <remarks>
   //   /// The <see cref="Buffer.BlockCopy(Array, Int32, Array, Int32, Int32)"/> method will be used to copy bytes.
   //   /// </remarks>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte[] CreateAndBlockCopyTo( this Byte[] sourceArray, ref Int32 sourceArrayIndex, Int32 amount )
   //   {
   //      return BlockCopyTo( sourceArray, ref sourceArrayIndex, amount == 0 ? Empty<Byte>.Array : new Byte[amount], 0, amount );
   //   }

   //   /// <summary>
   //   /// This is helper method to call <see cref="Buffer.BlockCopy(Array, Int32, Array, Int32, Int32)"/> method and increment source array index.
   //   /// </summary>
   //   /// <param name="sourceArray">The source array.</param>
   //   /// <param name="sourceArrayIndex">The offset in source array to start copying from. This will be incremented by <paramref name="amount"/>.</param>
   //   /// <param name="targetArray">The target array.</param>
   //   /// <param name="targetArrayIndex">The offset in target array to start copying to.</param>
   //   /// <param name="amount">The amount of bytes to copy.</param>
   //   /// <returns>The <paramref name="targetArray"/>.</returns>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte[] BlockCopyTo( this Byte[] sourceArray, ref Int32 sourceArrayIndex, Byte[] targetArray, Int32 targetArrayIndex, Int32 amount )
   //   {
   //      // TODO Does Buffer.BlockCopy support Int64 for source position?
   //      if ( amount > 0 )
   //      {
   //         Buffer.BlockCopy( sourceArray, sourceArrayIndex, targetArray, targetArrayIndex, amount );
   //         sourceArrayIndex += amount;
   //      }
   //      return targetArray;
   //   }

   //   /// <summary>
   //   /// This is helper method to call <see cref="Buffer.BlockCopy(Array, Int32, Array, Int32, Int32)"/> method and increment target array index.
   //   /// </summary>
   //   /// <param name="targetArray">The target array.</param>
   //   /// <param name="targetArrayIndex">The offset in target array to start copying to. This will be incremented by <paramref name="amount"/>.</param>
   //   /// <param name="sourceArray">The source array.</param>
   //   /// <param name="sourceArrayIndex">The offset in source array to start copying from.</param>
   //   /// <param name="amount">The amount of bytes to copy.</param>
   //   /// <returns>The <paramref name="targetArray"/>.</returns>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte[] BlockCopyFrom( this Byte[] targetArray, ref Int32 targetArrayIndex, Byte[] sourceArray, Int32 sourceArrayIndex, Int32 amount )
   //   {
   //      // TODO Does Buffer.BlockCopy support Int64 for source position?
   //      Buffer.BlockCopy( sourceArray, sourceArrayIndex, targetArray, targetArrayIndex, amount );
   //      targetArrayIndex += amount;
   //      return targetArray;
   //   }

   //   /// <summary>
   //   /// This is helper method to call <see cref="Buffer.BlockCopy(Array, Int32, Array, Int32, Int32)"/> method and increment target array index.
   //   /// </summary>
   //   /// <param name="targetArray">The target array.</param>
   //   /// <param name="targetArrayIndex">The offset in target array to start copying to. This will be incremented by length of <paramref name="sourceArray"/> minus <paramref name="sourceArrayIndex"/>.</param>
   //   /// <param name="sourceArray">The source array. All of its contents starting from <paramref name="sourceArrayIndex"/> will be copied.</param>
   //   /// <param name="sourceArrayIndex">The offset in source array to start copying from.</param>
   //   /// <returns>The <paramref name="targetArray"/>.</returns>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte[] BlockCopyFrom( this Byte[] targetArray, ref Int32 targetArrayIndex, Byte[] sourceArray, Int32 sourceArrayIndex )
   //   {
   //      return BlockCopyFrom( targetArray, ref targetArrayIndex, sourceArray, sourceArray.Length );
   //   }

   //   /// <summary>
   //   /// This is helper method to call <see cref="Buffer.BlockCopy(Array, Int32, Array, Int32, Int32)"/> method and increment target array index.
   //   /// </summary>
   //   /// <param name="targetArray">The target array.</param>
   //   /// <param name="targetArrayIndex">The offset in target array to start copying to. This will be incremented by length of <paramref name="sourceArray"/>.</param>
   //   /// <param name="sourceArray">The source array. All of its contents will be copied.</param>
   //   /// <returns>The <paramref name="targetArray"/>.</returns>
   //#if !NET40
   //   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   //#endif
   //   public static Byte[] BlockCopyFrom( this Byte[] targetArray, ref Int32 targetArrayIndex, Byte[] sourceArray )
   //   {
   //      return BlockCopyFrom( targetArray, ref targetArrayIndex, sourceArray, 0, sourceArray.Length );
   //   }

   /// <summary>
   /// Sets a single byte in byte array at specified offset to given value, and increments the offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to set byte. Will be incremented by 1.</param>
   /// <param name="aByte">The value to set.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteByteToBytes( this Byte[] array, ref Int32 idx, Byte aByte )
   {
      array[idx++] = aByte;
      return array;
   }

   /// <summary>
   /// Sets a single byte in byte array at specified offset to given value, and increments the offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to set byte. Will be incremented by 1.</param>
   /// <param name="sByte">The value to set. Even though it is integer, it is interpreted as signed byte.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteSByteToBytes( this Byte[] array, ref Int32 idx, SByte sByte )
   {
      array[idx++] = sByte < 0 ? (Byte) ( 256 + sByte ) : (Byte) sByte;
      return array;
   }

   /// <summary>
   /// Writes a given string in byte array starting at specified offset and using specified <see cref="Encoding"/>.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by the amount of bytes the <paramref name="str"/> will take using the <paramref name="encoding"/>.</param>
   /// <param name="encoding">The <see cref="Encoding"/> to use.</param>
   /// <param name="str">The string to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="encoding"/> or <paramref name="str"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteStringToBytes( this Byte[] array, ref Int32 idx, Encoding encoding, String str )
   {
      ArgumentValidator.ValidateNotNull( "Encoding", encoding );
      ArgumentValidator.ValidateNotNull( "String", str );

      idx += encoding.GetBytes( str, 0, str.Length, array, idx );
      return array;
   }

   /// <summary>
   /// Writes a given <see cref="Guid"/> in byte array starting at specified offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing. Will be incremented by <c>16</c>.</param>
   /// <param name="guid">The <see cref="Guid"/> to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteGUIDToBytes( this Byte[] array, ref Int32 idx, Guid guid )
   {
      // TODO optimize (so won't need to create array)
      guid.ToByteArray().CopyTo( array, idx );
      idx += GUID_SIZE;
      return array;
   }

   /// <summary>
   /// Increments index and returns the array.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The current index. Will be incremented by <paramref name="count"/>.</param>
   /// <param name="count">The amount to increment <paramref name="idx"/>.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] Skip( this Byte[] array, ref Int32 idx, Int32 count )
   {
      idx += count;
      return array;
   }

   /// <summary>
   /// Writes a string using casting each character to <see cref="Byte"/>. Will not throw on invalid bytes.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to start writing at. Will be incremented by the length of <paramref name="str"/>.</param>
   /// <param name="str">The string to write.</param>
   /// <param name="terminatingZero">Whether to write a terminating zero following the string.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteASCIIString( this Byte[] array, ref Int32 idx, String str, Boolean terminatingZero )
   {
      for ( var i = 0; i < str.Length; ++i )
      {
         array[idx++] = (Byte) str[i];
      }
      if ( terminatingZero )
      {
         array[idx++] = 0;
      }
      return array;
   }

   /// <summary>
   /// Reads 7-bit encoded <see cref="Int32"/> in little-endian format from byte array.
   /// This kind of variable-length encoding is used by <see cref="System.IO.BinaryReader"/> when it deserializes strings.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index in the <paramref name="array"/> where to start to read 7-bit encoded <see cref="Int32"/>. This parameter will be incremented by how many bytes were needed to read the value.</param>
   /// <param name="throwOnInvalid">
   /// Whether to throw an <see cref="InvalidOperationException"/> if the integer has invalid encoded value.
   /// The value is considered to be encoded in invalid way if fifth byte has its highest bit set.
   /// </param>
   /// <returns>The decoded <see cref="Int32"/>.</returns>
   /// <exception cref="InvalidOperationException">If the <paramref name="throwOnInvalid"/> is <c>true</c> and fifth read byte has its highest bit set.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int32 ReadInt32LEEncoded7Bit( this Byte[] array, ref Int32 idx, Boolean throwOnInvalid = false )
   {
      // Int32 encoded as 1-5 bytes. If highest bit set -> more bytes to follow.
      var retVal = 0;
      var shift = 0;
      byte b;
      do
      {
         if ( shift > 32 )  // 5 bytes max per Int32, shift += 7
         {
            if ( throwOnInvalid )
            {
               throw new InvalidOperationException( "7-bit encoded Int32 had its fifth byte highest bit set." );
            }
            else
            {
               break;
            }
         }

         b = array[idx++];
         retVal |= ( b & 0x7F ) << shift;
         shift += 7;
      } while ( ( b & 0x80 ) != 0 );

      return retVal;
   }

   /// <summary>
   /// Reads 7-bit encoded <see cref="Int64"/> in little-endian format from byte array.
   /// This kind of variable-length encoding is used by <see cref="System.IO.BinaryReader"/> when it deserializes strings.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index in the <paramref name="array"/> where to start to read 7-bit encoded <see cref="Int64"/>. This parameter will be incremented by how many bytes were needed to read the value.</param>
   /// <param name="throwOnInvalid">
   /// Whether to throw an <see cref="InvalidOperationException"/> if the integer has invalid encoded value.
   /// The value is considered to be encoded in invalid way if fifth byte has its highest bit set.
   /// </param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
   /// <exception cref="InvalidOperationException">If the <paramref name="throwOnInvalid"/> is <c>true</c> and fifth read byte has its highest bit set.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int64 ReadInt64LEEncoded7Bit( this Byte[] array, ref Int32 idx, Boolean throwOnInvalid = false )
   {
      // Int64 encoded as 1-9 bytes. If highest bit set -> more bytes to follow.
      var retVal = 0L;
      var shift = 0;
      byte b;
      do
      {
         if ( shift > 64 )  // 9 bytes max per Int64, shift += 7
         {
            if ( throwOnInvalid )
            {
               throw new InvalidOperationException( "7-bit encoded Int32 had its fifth byte highest bit set." );
            }
            else
            {
               break;
            }
         }

         b = array[idx++];
         retVal |= ( (Int64) ( b & 0x7F ) ) << shift;
         shift += 7;
      } while ( ( b & 0x80 ) != 0 );

      return retVal;
   }

   /// <summary>
   /// Writes 7-bit encoded <see cref="Int32"/> in little-endian format to byte array.
   /// This kind of variable-length encoding is used by <see cref="System.IO.BinaryWriter"/> when it serializes strings.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index in the <paramref name="array"/> where to start to write 7-bit encoded <see cref="Int32"/>. This parameter will be incremented by how many bytes were needed to write the value.</param>
   /// <param name="value">The value to encode.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt32LEEncoded7Bit( this Byte[] array, ref Int32 idx, Int32 value )
   {
      // Write 7 bits at a time 
      var uValue = unchecked((UInt32) value);
      Boolean cont;
      do
      {
         cont = uValue >= 0x80u;
         array[idx++] = unchecked((Byte) ( cont ? ( uValue | 0x80u ) : uValue ));
         uValue >>= 7;
      } while ( cont );
      return array;
   }

   /// <summary>
   /// Writes 7-bit encoded <see cref="Int64"/> in little-endian format to byte array.
   /// This kind of variable-length encoding is used by <see cref="System.IO.BinaryWriter"/> when it serializes strings.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index in the <paramref name="array"/> where to start to write 7-bit encoded <see cref="Int32"/>. This parameter will be incremented by how many bytes were needed to write the value.</param>
   /// <param name="value">The value to encode.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt64LEEncoded7Bit( this Byte[] array, ref Int32 idx, Int64 value )
   {
      // Write 7 bits at a time 
      var uValue = unchecked((UInt64) value);
      Boolean cont;
      do
      {
         cont = uValue >= 0x80u;
         array[idx++] = unchecked((Byte) ( cont ? ( uValue | 0x80u ) : uValue ));
         uValue >>= 7;
      } while ( cont );
      return array;
   }

   /// <summary>
   /// Reads 7-bit encoded <see cref="Int32"/> in big-endian format from byte array.
   /// This kind of variable-length encoding is used by <see cref="System.IO.BinaryReader"/> when it deserializes strings.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index in the <paramref name="array"/> where to start to read 7-bit encoded <see cref="Int32"/>. This parameter will be incremented by how many bytes were needed to read the value.</param>
   /// <param name="throwOnInvalid">
   /// Whether to throw an <see cref="InvalidOperationException"/> if the integer has invalid encoded value.
   /// The value is considered to be encoded in invalid way if fifth byte has its highest bit set.
   /// </param>
   /// <returns>The decoded <see cref="Int32"/>.</returns>
   /// <exception cref="InvalidOperationException">If the <paramref name="throwOnInvalid"/> is <c>true</c> and fifth read byte has its highest bit set.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int32 ReadInt32BEEncoded7Bit( this Byte[] array, ref Int32 idx, Boolean throwOnInvalid = false )
   {
      // Int32 encoded as 1-5 bytes. If highest bit set -> more bytes to follow.
      var retVal = 0;
      var shift = 0;
      byte b;
      do
      {
         if ( shift > 32 )  // 5 bytes max per Int32, shift += 7
         {
            if ( throwOnInvalid )
            {
               throw new InvalidOperationException( "7-bit encoded Int32 had its fifth byte highest bit set." );
            }
            else
            {
               break;
            }
         }

         b = array[idx++];
         retVal = ( retVal << shift ) | ( b & 0x7F );
         shift += 7;
      } while ( ( b & 0x80 ) != 0 );

      return retVal;
   }

   /// <summary>
   /// Reads 7-bit encoded <see cref="Int64"/> in big-endian format from byte array.
   /// This kind of variable-length encoding is used by <see cref="System.IO.BinaryReader"/> when it deserializes strings.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index in the <paramref name="array"/> where to start to read 7-bit encoded <see cref="Int64"/>. This parameter will be incremented by how many bytes were needed to read the value.</param>
   /// <param name="throwOnInvalid">
   /// Whether to throw an <see cref="InvalidOperationException"/> if the integer has invalid encoded value.
   /// The value is considered to be encoded in invalid way if fifth byte has its highest bit set.
   /// </param>
   /// <returns>The decoded <see cref="Int64"/>.</returns>
   /// <exception cref="InvalidOperationException">If the <paramref name="throwOnInvalid"/> is <c>true</c> and fifth read byte has its highest bit set.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Int64 ReadInt64BEEncoded7Bit( this Byte[] array, ref Int32 idx, Boolean throwOnInvalid = false )
   {
      // Int64 encoded as 1-9 bytes. If highest bit set -> more bytes to follow.
      var retVal = 0L;
      var shift = 0;
      byte b;
      do
      {
         if ( shift > 64 )  // 9 bytes max per Int64, shift += 7
         {
            if ( throwOnInvalid )
            {
               throw new InvalidOperationException( "7-bit encoded Int32 had its fifth byte highest bit set." );
            }
            else
            {
               break;
            }
         }

         b = array[idx++];
         retVal = ( retVal << shift ) | (Int64) ( b & 0x7F );
         shift += 7;
      } while ( ( b & 0x80 ) != 0 );

      return retVal;
   }

   /// <summary>
   /// Writes 7-bit encoded <see cref="Int32"/> in big-endian format to byte array.
   /// This kind of variable-length encoding is used by <see cref="System.IO.BinaryWriter"/> when it serializes strings.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index in the <paramref name="array"/> where to start to write 7-bit encoded <see cref="Int32"/>. This parameter will be incremented by how many bytes were needed to write the value.</param>
   /// <param name="value">The value to encode.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt32BEEncoded7Bit( this Byte[] array, ref Int32 idx, Int32 value )
   {
      // Find out the amount of bytes taken
      var len = BinaryUtils.Calculate7BitEncodingLength( value );
      idx += len;
      var newIdx = idx;
      // Write 7 bits at a time, notice that we are starting from the end and decreasing index
      var uValue = unchecked((UInt32) value);
      Boolean cont;
      do
      {
         cont = uValue >= 0x80u;
         array[--newIdx] = unchecked((Byte) ( cont ? ( uValue | 0x80u ) : uValue ));
         uValue >>= 7;
      } while ( cont );
      return array;
   }

   /// <summary>
   /// Writes 7-bit encoded <see cref="Int64"/> in big-endian format to byte array.
   /// This kind of variable-length encoding is used by <see cref="System.IO.BinaryWriter"/> when it serializes strings.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index in the <paramref name="array"/> where to start to write 7-bit encoded <see cref="Int32"/>. This parameter will be incremented by how many bytes were needed to write the value.</param>
   /// <param name="value">The value to encode.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] WriteInt64BEEncoded7Bit( this Byte[] array, ref Int32 idx, Int64 value )
   {
      // Find out the amount of bytes taken
      var len = BinaryUtils.Calculate7BitEncodingLength( value );
      idx += len;
      var newIdx = idx;
      // Write 7 bits at a time, notice that we are starting from the end and decreasing index
      var uValue = unchecked((UInt64) value);
      Boolean cont;
      do
      {
         cont = uValue >= 0x80u;
         array[--newIdx] = unchecked((Byte) ( cont ? ( uValue | 0x80u ) : uValue ));
         uValue >>= 7;
      } while ( cont );
      return array;
   }

   //private static UInt32 SwapEndianness32( UInt32 val )
   //{
   //   // From http://stackoverflow.com/questions/19560436/bitwise-endian-swap-for-various-types

   //   // Swap adjacent 16-bit blocks
   //   val = val.RotateRight( 16 );
   //   // Swap adjacent 8-bit blocks
   //   return ( ( val & 0xFF00FF00 ) >> 8 ) | ( ( val & 0x00FF00FF ) << 8 );
   //}

   //private static UInt64 SwapEndianness64( UInt64 val )
   //{
   //   // From http://stackoverflow.com/questions/19560436/bitwise-endian-swap-for-various-types

   //   // Swap adjacent 32-bit blocks
   //   val = val.RotateRight( 32 );

   //   // Swap adjacent 16-bit blocks
   //   val = ( ( val & 0xFFFF0000FFFF0000 ) >> 16 ) | ( ( val & 0x0000FFFF0000FFFF ) << 16 );

   //   // Swap adjacent 8-bit blocks
   //   return ( ( val & 0xFF00FF00FF00FF00 ) >> 8 ) | ( ( val & 0x00FF00FF00FF00FF ) << 8 );
   //}

   /// <summary>
   /// Fills array with zeroes, starting at specified offset and writing specified amount of zeroes.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The index to start. Will be incremented by <paramref name="count"/> when this method finishes.</param>
   /// <param name="count">The amount of zeroes to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte[] ZeroOut( this Byte[] array, ref Int32 idx, Int32 count )
   {
      if ( count > 0 )
      {
         Array.Clear( array, idx, count );
         idx += count;
      }
      return array;
   }
}