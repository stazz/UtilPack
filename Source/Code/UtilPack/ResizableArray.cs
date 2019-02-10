/*
* Copyright 2015 Stanislav Muhametsin. All rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
* implied.
*
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UtilPack;

namespace UtilPack
{
   /// <summary>
   /// This is helper class to hold an array, which can be resized.
   /// Unlike <see cref="List{T}"/>, this class provides direct access to the array.
   /// </summary>
   /// <typeparam name="T">The type of elements in the array.</typeparam>
   public class ResizableArray<T>
   {
      private Int32 _exponentialResize;
      private Int32 _currentCapacity;
      private T[] _array;


      /// <summary>
      /// Creates a new <see cref="ResizableArray{T}"/> with given initial size and maximum size.
      /// </summary>
      /// <param name="initialSize">The initial size of the array. If this is less than <c>0</c>, then the initial size of the array will be <c>0</c>.</param>
      /// <param name="maxLimit">The maximum limit. If this is less than <c>0</c>, then the array may grow indefinetly.</param>
      /// <param name="exponentialResize">Whether to resize array exponentially, that is, do all resizes by doubling array length.</param>
      public ResizableArray( Int32 initialSize = 0, Int32 maxLimit = -1, Boolean exponentialResize = true )
      {
         if ( initialSize < 0 )
         {
            initialSize = 0;
         }
         this.MaximumSize = maxLimit;
         this._currentCapacity = initialSize;
         this._array = new T[initialSize];
         this.ExponentialResize = exponentialResize;
      }

      /// <summary>
      /// Gets or sets the current array size.
      /// If setter gets value smaller than current array size, it does nothing.
      /// </summary>      
      /// <remarks>
      /// Setter may grow the array, so that the array reference that the array acquired prior to calling this method may no longer reference the same array that is returned by <see cref="Array"/> property after calling this method.
      /// </remarks>
      public Int32 CurrentMaxCapacity
      {
         get
         {
            return this._currentCapacity;
         }
         set
         {
            var curCap = this._currentCapacity;
            if ( value > 0 && curCap < value )
            {
               this.EnsureArraySize( value, curCap );
               Interlocked.Exchange( ref this._currentCapacity, value );
            }
         }
      }

      /// <summary>
      /// Gets or sets the resize strategy for this resizable array.
      /// </summary>
      /// <value>
      /// The resize strategy for this resizable array.
      /// </value>
      /// <remarks>
      /// When this is <c>true</c>, then array is grown by doubling previous length, until it is at least new size.
      /// Otherwise, array is grown by resizing array directly to new size.
      /// No matter what this value is, the new size will never be greater than <see cref="MaximumSize"/>.
      /// </remarks>
      public Boolean ExponentialResize
      {
         get
         {
            return Convert.ToBoolean( this._exponentialResize );
         }
         set
         {
            Interlocked.Exchange( ref this._exponentialResize, Convert.ToInt32( value ) );
         }
      }

      /// <summary>
      /// Gets the maximum size for this <see cref="ResizableArray{T}"/>, as specified in constructor.
      /// </summary>
      public Int32 MaximumSize { get; }

      /// <summary>
      /// Gets the reference to the current array of this <see cref="ResizableArray{T}"/>.
      /// </summary>
      /// <value>The reference to the current array of this <see cref="ResizableArray{T}"/>.</value>
      /// <remarks>
      /// Setting <see cref="CurrentMaxCapacity"/> may cause this to return reference to different instance of the array.
      /// </remarks>
      public T[] Array
      {
         get
         {
            return this._array;
         }
      }

      private void EnsureArraySize( Int32 size, Int32 currentCapacity )
      {
         var max = this.MaximumSize;
         if ( max < 0 || size <= max )
         {
            var array = this._array;
            if ( array.Length < size )
            {
               T[] newArray;
               if ( Convert.ToBoolean( this._exponentialResize ) )
               {
                  var newLen = array.Length < 4 ? 2 : array.Length;
                  do
                  {
                     newLen *= 2;
                  } while ( newLen < size );
                  if ( max >= 0 && newLen > max )
                  {
                     newLen = max;
                  }
                  newArray = new T[newLen];
               }
               else
               {
                  newArray = new T[size];
               }
               Interlocked.Exchange( ref this._array, newArray );
               System.Array.Copy( array, 0, newArray, 0, array.Length );
            }
         }
         else
         {
            throw new InvalidOperationException( "The wanted size " + size + " exceeds maximum limit of " + max + " for this resizable array." );
         }
      }

   }
}

public static partial class E_UtilPack
{
   /// <summary>
   /// Helper method to ensure that given <see cref="ResizableArray{T}"/> will have at least <paramref name="currentIndex"/> + <paramref name="amountToAdd"/> elements.
   /// </summary>
   /// <typeparam name="T">The type of elements in <paramref name="array"/>.</typeparam>
   /// <param name="array">The <see cref="ResizableArray{T}"/>.</param>
   /// <param name="currentIndex">The current index of array.</param>
   /// <param name="amountToAdd">The amount of elements to add.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<T> EnsureThatCanAdd<T>( this ResizableArray<T> array, Int32 currentIndex, Int32 amountToAdd )
   {
      array.CurrentMaxCapacity = currentIndex + amountToAdd;
      return array;
   }

   /// <summary>
   /// Writes contents of <paramref name="sourceArray"/> to this <see cref="ResizableArray{T}" />.
   /// </summary>
   /// <typeparam name="T">The type of elements in the <paramref name="array"/>.</typeparam>
   /// <param name="array">The <see cref="ResizableArray{T}" /></param>
   /// <param name="idx">The offset at which to start copying elements into the <paramref name="array"/>.</param>
   /// <param name="sourceArray">The array to read elements from. May be <c>null</c>.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<T> WriteArray<T>( this ResizableArray<T> array, ref Int32 idx, T[] sourceArray )
   {
      return sourceArray.IsNullOrEmpty() ?
         array :
         array.WriteArray( ref idx, sourceArray, 0, sourceArray.Length );
   }

   /// <summary>
   /// Writes given amount of elements from <paramref name="sourceArray"/> into this <see cref="ResizableArray{T}" />
   /// </summary>
   /// <typeparam name="T">The type of elements in the <paramref name="array"/>.</typeparam>
   /// <param name="array">The <see cref="ResizableArray{T}" /></param>
   /// <param name="idx">The offset at which to start copying elements into the <paramref name="array"/>.</param>
   /// <param name="sourceArray">The array to read elements from.</param>
   /// <param name="count">The amount of elements to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<T> WriteArray<T>( this ResizableArray<T> array, ref Int32 idx, T[] sourceArray, Int32 count )
   {
      return array.WriteArray( ref idx, sourceArray, 0, count );
   }

   /// <summary>
   /// Starting at given offset, writes given amount of elements from <paramref name="sourceArray"/> into this <see cref="ResizableArray{T}" />
   /// </summary>
   /// <typeparam name="T">The type of elements in the <paramref name="array"/>.</typeparam>
   /// <param name="array">The <see cref="ResizableArray{T}" /></param>
   /// <param name="idx">The offset at which to start copying elements into the <paramref name="array"/>.</param>
   /// <param name="sourceArray">The array to read elements from.</param>
   /// <param name="offset">The offset at which to start reading elements from <paramref name="sourceArray"/>.</param>
   /// <param name="count">The amount of elements to write.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<T> WriteArray<T>( this ResizableArray<T> array, ref Int32 idx, T[] sourceArray, Int32 offset, Int32 count )
   {
      array.EnsureThatCanAdd( idx, count );
      Array.Copy( sourceArray, offset, array.Array, idx, count );
      idx += count;
      return array;
   }

   /// <summary>
   /// Sets a single byte in byte array at specified offset to given value, and increments the offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to set byte. Will be incremented by 1.</param>
   /// <param name="value">The value to set.</param>
   /// <returns>The <paramref name="array"/>.</returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<Byte> WriteByteToBytes( this ResizableArray<Byte> array, ref Int32 idx, Byte value )
   {
      array
         .EnsureThatCanAdd( idx, 1 )
         .Array.WriteByteToBytes( ref idx, value );
      return array;
   }

   /// <summary>
   /// Sets a single byte in byte array at specified offset to given value, and increments the offset.
   /// </summary>
   /// <param name="array">The byte array.</param>
   /// <param name="idx">The offset to set byte. Will be incremented by 1.</param>
   /// <param name="value">The value to set. Even though it is integer, it is interpreted as signed byte.</param>
   /// <returns>The <paramref name="array"/>.</returns>
   [CLSCompliant( false )]
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<Byte> WriteSByteToBytes( this ResizableArray<Byte> array, ref Int32 idx, SByte value )
   {
      array
         .EnsureThatCanAdd( idx, 1 )
         .Array.WriteSByteToBytes( ref idx, value );
      return array;
   }

   #region Little-Endian Conversions

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
   public static ResizableArray<Byte> WriteInt16LEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Int16 value )
   {
      array
         .EnsureThatCanAdd( idx, 2 )
         .Array.WriteInt16LEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteInt16LEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Int16 value )
   {
      return array.WriteInt16LEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteUInt16LEToBytes( this ResizableArray<Byte> array, ref Int32 idx, UInt16 value )
   {
      return array.WriteInt16LEToBytes( ref idx, (Int16) value );
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
   public static ResizableArray<Byte> WriteUInt16LEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, UInt16 value )
   {
      return array.WriteInt16LEToBytes( ref idx, (Int16) value );
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
   public static ResizableArray<Byte> WriteInt32LEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Int32 value )
   {
      array
         .EnsureThatCanAdd( idx, 4 )
         .Array.WriteInt32LEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteInt32LEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Int32 value )
   {
      return array.WriteInt32LEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteUInt32LEToBytes( this ResizableArray<Byte> array, ref Int32 idx, UInt32 value )
   {
      return array.WriteInt32LEToBytes( ref idx, (Int32) value );
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
   public static ResizableArray<Byte> WriteUInt32LEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, UInt32 value )
   {
      return array.WriteInt32LEToBytes( ref idx, (Int32) value );
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
   public static ResizableArray<Byte> WriteInt64LEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Int64 value )
   {
      array
         .EnsureThatCanAdd( idx, 8 )
         .Array.WriteInt64LEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteInt64LEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Int64 value )
   {
      return array.WriteInt64LEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteUInt64LEToBytes( this ResizableArray<Byte> array, ref Int32 idx, UInt64 value )
   {
      return array.WriteInt64LEToBytes( ref idx, (Int64) value );
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
   public static ResizableArray<Byte> WriteUInt64LEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, UInt64 value )
   {
      return array.WriteInt64LEToBytes( ref idx, (Int64) value );
   }

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
   public static ResizableArray<Byte> WriteSingleLEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Single value )
   {
      array
         .EnsureThatCanAdd( idx, 4 )
         .Array.WriteSingleLEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteSingleLEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Single value )
   {
      return array.WriteSingleLEToBytes( ref idx, value );
   }

   ///// <summary>
   ///// Writes Int32 bits of given <see cref="Single"/> value in little-endian orger to given array starting at specified offset.
   ///// </summary>
   ///// <param name="array">The byte array.</param>
   ///// <param name="idx">The offset to start writing.</param>
   ///// <param name="value">The <see cref="Single"/> value to write.</param>
   ///// <returns>The <paramref name="array"/>.</returns>
   ///// <remarks>This code will use <c>unsafe</c> method.</remarks>
   //public static ResizableArray<Byte> WriteSingleLEToBytesUnsafeNoRef( this ResizableArray<Byte> array, Int32 idx, Single value )
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
   public static ResizableArray<Byte> WriteDoubleLEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Double value )
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
   public static ResizableArray<Byte> WriteDoubleLEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Double value )
   {
      return array.WriteDoubleLEToBytes( ref idx, value );
   }

   #endregion


   #region Big-Endian Conversions


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
   public static ResizableArray<Byte> WriteInt16BEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Int16 value )
   {
      array
         .EnsureThatCanAdd( idx, 2 )
         .Array.WriteInt16BEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteInt16BEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Int16 value )
   {
      return array.WriteInt16BEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteUInt16BEToBytes( this ResizableArray<Byte> array, ref Int32 idx, UInt16 value )
   {
      return array.WriteInt16BEToBytes( ref idx, (Int16) value );
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
   public static ResizableArray<Byte> WriteUInt16BEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, UInt16 value )
   {
      return array.WriteInt16BEToBytes( ref idx, (Int16) value );
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
   public static ResizableArray<Byte> WriteInt32BEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Int32 value )
   {
      array
         .EnsureThatCanAdd( idx, 4 )
         .Array.WriteInt32BEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteInt32BEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Int32 value )
   {
      return array.WriteInt32BEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteUInt32BEToBytes( this ResizableArray<Byte> array, ref Int32 idx, UInt32 value )
   {
      return array.WriteInt32BEToBytes( ref idx, (Int32) value );
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
   public static ResizableArray<Byte> WriteUInt32BEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, UInt32 value )
   {
      return array.WriteInt32BEToBytes( ref idx, (Int32) value );
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
   public static ResizableArray<Byte> WriteInt64BEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Int64 value )
   {
      array
         .EnsureThatCanAdd( idx, 8 )
         .Array.WriteInt64BEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteInt64BEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Int64 value )
   {
      return array.WriteInt64BEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteUInt64BEToBytes( this ResizableArray<Byte> array, ref Int32 idx, UInt64 value )
   {
      return array.WriteInt64BEToBytes( ref idx, (Int64) value );
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
   public static ResizableArray<Byte> WriteUInt64BEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, UInt64 value )
   {
      return array.WriteInt64BEToBytes( ref idx, (Int64) value );
   }

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
   public static ResizableArray<Byte> WriteSingleBEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Single value )
   {
      array
         .EnsureThatCanAdd( idx, 4 )
         .Array.WriteSingleBEToBytes( ref idx, value );
      return array;
   }

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
   public static ResizableArray<Byte> WriteSingleBEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Single value )
   {
      return array.WriteSingleBEToBytes( ref idx, value );
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
   public static ResizableArray<Byte> WriteDoubleBEToBytes( this ResizableArray<Byte> array, ref Int32 idx, Double value )
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
   public static ResizableArray<Byte> WriteDoubleBEToBytesNoRef( this ResizableArray<Byte> array, Int32 idx, Double value )
   {
      return array.WriteDoubleBEToBytes( ref idx, value );
   }

   #endregion

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
   public static ResizableArray<Byte> ZeroOut( this ResizableArray<Byte> array, ref Int32 idx, Int32 count )
   {
      if ( count > 0 )
      {
         array.FillWithOffsetAndCount( idx, count, (Byte) 0 );
         idx += count;
      }
      return array;
   }

   /// <summary>
   /// This is method to quickly fill array with values, utilizing the fact that <see cref="Array.Copy(Array, Array, Int32)"/> methods are very, very fast.
   /// </summary>
   /// <typeparam name="T">The type of array elements.</typeparam>
   /// <param name="destinationArray">The array to be filled with values.</param>
   /// <param name="value">The values to fill array with.</param>
   /// <returns>The <paramref name="destinationArray"/></returns>
   /// <exception cref="ArgumentNullException">If <paramref name="destinationArray"/> or <paramref name="value"/> are null.</exception>
   /// <exception cref="ArgumentException">If <paramref name="destinationArray"/> is not empty, and length of <paramref name="value"/> is greater than length of <paramref name="destinationArray"/>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<T> Fill<T>( this ResizableArray<T> destinationArray, params T[] value )
   {
      return destinationArray.FillWithOffsetAndCount( 0, destinationArray.CurrentMaxCapacity, value );
   }

   /// <summary>
   /// This is helper method to fill some class-based array with <c>null</c>s.
   /// Since the call <c>array.Fill(null)</c> will cause the actual array to be <c>null</c> instead of creating an array with <c>null</c> value.
   /// </summary>
   /// <typeparam name="T">The type of array elements.</typeparam>
   /// <param name="destinationArray">The array to be filled with values.</param>
   /// <returns>The <paramref name="destinationArray"/></returns>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<T> FillWithNulls<T>( this ResizableArray<T> destinationArray )
      where T : class
   {
      return destinationArray.Fill( new T[] { null } );
   }

   /// <summary>
   /// This is method to quickly fill array with values, utilizing the fact that <see cref="Array.Copy(Array, Array, Int32)"/> methods are very, very fast.
   /// </summary>
   /// <typeparam name="T">The type of array elements.</typeparam>
   /// <param name="destinationArray">The array to be filled with values.</param>
   /// <param name="value">The values to fill array with.</param>
   /// <param name="offset">The offset at which to start filling array.</param>
   /// <returns>The <paramref name="destinationArray"/></returns>
   /// <exception cref="ArgumentNullException">If <paramref name="destinationArray"/> or <paramref name="value"/> are null.</exception>
   /// <exception cref="ArgumentException">If <paramref name="destinationArray"/> is not empty, and length of <paramref name="value"/> is greater than length of <paramref name="destinationArray"/>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<T> FillWithOffset<T>( this ResizableArray<T> destinationArray, Int32 offset, params T[] value )
   {
      return destinationArray.FillWithOffsetAndCount( offset, destinationArray.CurrentMaxCapacity - offset, value );
   }

   /// <summary>
   /// This is method to quickly fill array with values, utilizing the fact that <see cref="Array.Copy(Array, Array, Int32)"/> methods are very, very fast.
   /// </summary>
   /// <typeparam name="T">The type of array elements.</typeparam>
   /// <param name="destinationArray">The array to be filled with values.</param>
   /// <param name="value">The values to fill array with.</param>
   /// <param name="offset">The offset at which to start filling array.</param>
   /// <param name="count">How many items to fill.</param>
   /// <returns>The <paramref name="destinationArray"/></returns>
   /// <remarks>
   /// Original source code is found at <see href="http://stackoverflow.com/questions/5943850/fastest-way-to-fill-an-array-with-a-single-value"/> and <see href="http://coding.grax.com/2014/04/better-array-fill-function.html"/>.
   /// According to first link, "<c>In my test with 20,000,000 array items, this function is twice as fast as a for loop.</c>".
   /// The source code was modified to fix a bug and also to support offset and count parameters.
   /// </remarks>
   /// <exception cref="ArgumentNullException">If <paramref name="destinationArray"/> or <paramref name="value"/> are null.</exception>
   /// <exception cref="ArgumentException">If <paramref name="destinationArray"/> is not empty, and length of <paramref name="value"/> is greater than length of <paramref name="destinationArray"/>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static ResizableArray<T> FillWithOffsetAndCount<T>( this ResizableArray<T> destinationArray, Int32 offset, Int32 count, params T[] value )
   {
      ArgumentValidator.ValidateNotNull( "Destination array", destinationArray );
      if ( offset < 0 )
      {
         throw new ArgumentOutOfRangeException( "Offset" );
      }
      destinationArray.CurrentMaxCapacity = offset + count;

      if ( count > 0 )
      {
         ArgumentValidator.ValidateNotEmpty( "Value array", value );

         var array = destinationArray.Array;


         var max = offset + count;
         if ( value.Length > count )
         {
            throw new ArgumentException( "Length of value array must not be more than count in destination" );
         }

         // set the initial array value
         Array.Copy( value, 0, array, offset, value.Length );

         Int32 copyLength;

         for ( copyLength = value.Length; copyLength + copyLength < count; copyLength <<= 1 )
         {
            Array.Copy( array, offset, array, offset + copyLength, copyLength );
         }

         Array.Copy( array, offset, array, offset + copyLength, count - copyLength );

      }
      return destinationArray;
   }

   ///// <summary>
   ///// Reads specific amount of bytes from <see cref="System.IO.Stream"/> into this resizable array, and returns the actual byte array.
   ///// </summary>
   ///// <param name="array">The <see cref="ResizableArray{T}"/>.</param>
   ///// <param name="stream">The <see cref="System.IO.Stream"/>.</param>
   ///// <param name="count">The amount of bytes to read.</param>
   ///// <returns>The actual byte array containing the bytes read (and possibly any other data following after this, if the <see cref="ResizableArray{T}"/>'s array had that before calling this method).</returns>
   //public static Byte[] ReadIntoResizableArray( this ResizableArray<Byte> array, System.IO.Stream stream, Int32 count )
   //{
   //   array.CurrentMaxCapacity = count;
   //   var retVal = array.Array;
   //   stream.ReadSpecificAmount( retVal, 0, count );
   //   return retVal;
   //}

   /// <summary>
   /// Helper function to set <see cref="ResizableArray{T}.CurrentMaxCapacity"/> and then return the <see cref="ResizableArray{T}.Array"/>.
   /// </summary>
   /// <typeparam name="T">The type of the array.</typeparam>
   /// <param name="array">The <see cref="ResizableArray{T}"/></param>
   /// <param name="capacity">The capacity.</param>
   /// <returns>The <see cref="ResizableArray{T}.Array"/>, which will be at least the size of given <paramref name="capacity"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="ResizableArray{T}"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static T[] SetCapacityAndReturnArray<T>( this ResizableArray<T> array, Int32 capacity )
   {
      array.CurrentMaxCapacity = capacity;
      return array.Array;
   }

}
