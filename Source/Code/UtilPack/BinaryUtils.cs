/*
 * Copyright 2014 Stanislav Muhametsin. All rights Reserved.
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

namespace UtilPack
{
   // Having static fields and/or constructors apparently prevents any static method from getting inlined
   // So let's have this in separate class
   internal static class LogTableHolder
   {
      // From http://graphics.stanford.edu/~seander/bithacks.html#IntegerLogLookup

      // The log base 2 of an integer is the same as the position of the highest bit set (or most significant bit set, MSB).

      internal static readonly Int32[] LOG_TABLE_256;

      // From http://graphics.stanford.edu/~seander/bithacks.html#IntegerLog10
      internal static readonly UInt32[] POWERS_OF_10_32;

      internal static readonly UInt64[] POWERS_OF_10_64;

      static LogTableHolder()
      {
         var arr = new Int32[256];
         for ( var i = 2; i < 256; ++i )
         {
            arr[i] = 1 + arr[i / 2];
         }
         arr[0] = BinaryUtils.LOG_2_OF_0;
         LOG_TABLE_256 = arr;

         POWERS_OF_10_32 = new UInt32[] { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000 };

         POWERS_OF_10_64 = new UInt64[] { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000, 10000000000, 100000000000, 1000000000000, 10000000000000, 100000000000000, 1000000000000000, 10000000000000000, 100000000000000000, 1000000000000000000, 10000000000000000000 };
      }
   }

   /// <summary>
   /// This class provides utility methods related to binary operations, which are not sensible to create as extension methods.
   /// </summary>
   public static class BinaryUtils
   {
      internal const Int32 LOG_2_OF_0 = -1;



      /// <summary>
      /// Returns the log base 2 of a given <paramref name="value"/>.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <returns>Log base 2 of <paramref name="value"/>.</returns>
      /// <remarks>
      /// The return value is also the position of the MSB set, with zero-based indexing.
      /// The algorithm is from <see href="http://graphics.stanford.edu/~seander/bithacks.html#IntegerLogLookup"/> .
      /// </remarks>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 Log2( UInt32 value )
      {
         UInt32 tt;

         if ( ( tt = value >> 24 ) != 0u )
         {
            return 24 + LogTableHolder.LOG_TABLE_256[tt];
         }
         else if ( ( tt = value >> 16 ) != 0u )
         {
            return 16 + LogTableHolder.LOG_TABLE_256[tt];
         }
         else if ( ( tt = value >> 8 ) != 0u )
         {
            return 8 + LogTableHolder.LOG_TABLE_256[tt];
         }
         else
         {
            return LogTableHolder.LOG_TABLE_256[value];
         }
      }


      /// <summary>
      /// Given amount of data and page size, calculates amount of pages the data will take.
      /// </summary>
      /// <param name="totalSize">The total size of the data.</param>
      /// <param name="pageSize">The size of a single page.</param>
      /// <returns>The amount of pages the data will take.</returns>
      /// <remarks>
      /// More specifically, this method will return <c>( <paramref name="totalSize" /> + <paramref name="pageSize" /> - 1 ) / <paramref name="pageSize" /></c>
      /// </remarks>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 AmountOfPagesTaken( Int32 totalSize, Int32 pageSize )
      {
         return ( totalSize + pageSize - 1 ) / pageSize;
      }



      /// <summary>
      /// Returns the log base 2 of a given <paramref name="value"/>.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <returns>Log base 2 of <paramref name="value"/>.</returns>
      /// <remarks>
      /// The return value is also the position of the MSB set, with zero-based indexing.
      /// The algorithm uses <see cref="Log2(UInt32)"/> method to calculate return value.
      /// </remarks>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 Log2( UInt64 value )
      {
         var highest = Log2( (UInt32) ( value >> 32 ) );
         if ( highest == BinaryUtils.LOG_2_OF_0 )
         {
            highest = Log2( (UInt32) value );
         }
         else
         {
            highest += 32;
         }
         return highest;
      }

      /// <summary>
      /// Returns the log base 10 of a given <paramref name="value"/>.
      /// </summary>
      /// <param name="value">The value</param>
      /// <returns>Log base 10 of <paramref name="value"/>.</returns>
      /// <remarks>
      /// This method uses <see cref="Log2(uint)"/> and lookup table to compute returned value.
      /// </remarks>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 Log10( UInt32 value )
      {
         var tmp = ( Log2( value ) + 1 ) * 1233 >> 12;
         return tmp - ( value < LogTableHolder.POWERS_OF_10_32[tmp] ? 1 : 0 );
      }

      /// <summary>
      /// Returns the log base 10 of a given <paramref name="value"/>.
      /// </summary>
      /// <param name="value">The value</param>
      /// <returns>Log base 10 of <paramref name="value"/>.</returns>
      /// <remarks>
      /// This method uses <see cref="Log2(ulong)"/> and lookup table to compute returned value.
      /// </remarks>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 Log10( UInt64 value )
      {
         var tmp = ( Log2( value ) + 1 ) * 1233 >> 12;
         return tmp - ( value < LogTableHolder.POWERS_OF_10_64[tmp] ? 1 : 0 );
      }


      /// <summary>
      /// Computes greatest common denominator without recursion.
      /// </summary>
      /// <param name="x">The first number.</param>
      /// <param name="y">The second number.</param>
      /// <returns>The greatest common denominator of <paramref name="x"/> and <paramref name="y"/>.</returns>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 GCD( Int32 x, Int32 y )
      {
         // Unwind recursion
         while ( y != 0 )
         {
            var tmp = y;
            y = x % y;
            x = tmp;
         }

         return x;
      }

      /// <summary>
      /// Computes greatest common denominator without recursion, for <see cref="Int64"/>.
      /// </summary>
      /// <param name="x">The first number.</param>
      /// <param name="y">The second number.</param>
      /// <returns>The greatest common denominator of <paramref name="x"/> and <paramref name="y"/>.</returns>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int64 GCD64( Int64 x, Int64 y )
      {
         // Unwind recursion
         while ( y != 0 )
         {
            var tmp = y;
            y = x % y;
            x = tmp;
         }

         return x;
      }

      /// <summary>
      /// Computes least common multiplier (by using greatest common denominator).
      /// </summary>
      /// <param name="x">The first number.</param>
      /// <param name="y">The second number.</param>
      /// <returns>The least common multiplier of <paramref name="x"/> and <paramref name="y"/>.</returns>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 LCM( Int32 x, Int32 y )
      {
         return ( x * y ) / GCD( x, y );
      }

      /// <summary>
      /// Computes least common multiplier (by using greatest common denominator), for <see cref="Int64"/>.
      /// </summary>
      /// <param name="x">The first number.</param>
      /// <param name="y">The second number.</param>
      /// <returns>The least common multiplier of <paramref name="x"/> and <paramref name="y"/>.</returns>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int64 LCM64( Int64 x, Int64 y )
      {
         return ( x * y ) / GCD64( x, y );
      }

      /// <summary>
      /// Returns greatest power of 2 less than or equal to given number.
      /// </summary>
      /// <param name="x">The number.</param>
      /// <returns>The greatest power of 2 less than or equal to <paramref name="x"/>.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt32 FLP2( UInt32 x )
      {
         return ( 1u << Log2( x ) );
      }

      /// <summary>
      /// Returns greatest power of 2 less than or equal to given 64-bit number.
      /// </summary>
      /// <param name="x">The number.</param>
      /// <returns>The greatest power of 2 less than or equal to <paramref name="x"/>.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt64 FLP264( UInt64 x )
      {
         return ( 1u << Log2( x ) );
      }

      /// <summary>
      /// Returns least power of 2 greater than or equal to given number.
      /// </summary>
      /// <param name="x">The number.</param>
      /// <returns>The least power of 2 greater than or equal to <paramref name="x"/>.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt32 CLP2( UInt32 x )
      {
         return x == 0 ? 0 : ( 1u << ( 1 + Log2( x - 1 ) ) );
      }

      /// <summary>
      /// Returns least power of 2 greater than or equal to given 64-bit number.
      /// </summary>
      /// <param name="x">The number.</param>
      /// <returns>The least power of 2 greater than or equal to <paramref name="x"/>.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt64 CLP264( UInt64 x )
      {
         return x == 0 ? 0 : ( 1ul << ( 1 + Log2( x - 1 ) ) );
      }

      /// <summary>
      /// Calculates the amount of bytes needed when encoding the given integer value using 7-bit encoding.
      /// </summary>
      /// <param name="value">The integer value.</param>
      /// <returns>The amount of bytes needed to encode <paramref name="value"/> using 7-bit encoding.</returns>
      /// <seealso cref="UtilPackExtensions.WriteInt32LEEncoded7Bit"/>
      /// <seealso cref="UtilPackExtensions.WriteInt32BEEncoded7Bit"/>
      /// <seealso cref="UtilPackExtensions.ReadInt32LEEncoded7Bit"/>
      /// <seealso cref="UtilPackExtensions.ReadInt32BEEncoded7Bit"/>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 Calculate7BitEncodingLength( Int32 value )
      {
         return ( Log2( unchecked((UInt32) value) ) / 7 ) + 1;
      }

      /// <summary>
      /// Calculates the amount of bytes needed when encoding the given integer value using 7-bit encoding.
      /// </summary>
      /// <param name="value">The integer value.</param>
      /// <returns>The amount of bytes needed to encode <paramref name="value"/> using 7-bit encoding.</returns>
      /// <seealso cref="UtilPackExtensions.WriteInt64LEEncoded7Bit"/>
      /// <seealso cref="UtilPackExtensions.WriteInt64BEEncoded7Bit"/>
      /// <seealso cref="UtilPackExtensions.ReadInt64LEEncoded7Bit"/>
      /// <seealso cref="UtilPackExtensions.ReadInt64BEEncoded7Bit"/>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 Calculate7BitEncodingLength( Int64 value )
      {
         return ( Log2( unchecked((UInt64) value) ) / 7 ) + 1;
      }

   }

   public static partial class UtilPackExtensions
   {

      /// <summary>
      /// Rotates given <paramref name="value"/> left <paramref name="shift"/> amount of bytes.
      /// </summary>
      /// <param name="value">The value to rotate to left.</param>
      /// <param name="shift">The amount to bits to rotate.</param>
      /// <returns>The rotated value.</returns>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 RotateLeft( this Int32 value, Int32 shift )
      {
         return (Int32) ( (UInt32) value ).RotateLeft( shift );
      }

      /// <summary>
      /// Rotates given <paramref name="value"/> left <paramref name="shift"/> amount of bytes.
      /// </summary>
      /// <param name="value">The value to rotate to left.</param>
      /// <param name="shift">The amount to bits to rotate.</param>
      /// <returns>The rotated value.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt32 RotateLeft( this UInt32 value, Int32 shift )
      {
         return ( value << shift ) | ( value >> ( sizeof( UInt32 ) * 8 - shift ) );
      }

      /// <summary>
      /// Rotates given <paramref name="value"/> right <paramref name="shift"/> amount of bytes.
      /// </summary>
      /// <param name="value">The value to rotate to right.</param>
      /// <param name="shift">The amount to bits to rotate.</param>
      /// <returns>The rotated value.</returns>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 RotateRight( this Int32 value, Int32 shift )
      {
         return (Int32) ( (UInt32) value ).RotateRight( shift );
      }

      /// <summary>
      /// Rotates given <paramref name="value"/> right <paramref name="shift"/> amount of bytes.
      /// </summary>
      /// <param name="value">The value to rotate to right.</param>
      /// <param name="shift">The amount to bits to rotate.</param>
      /// <returns>The rotated value.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt32 RotateRight( this UInt32 value, Int32 shift )
      {
         return ( value >> shift ) | ( value << ( sizeof( UInt32 ) * 8 - shift ) );
      }


      /// <summary>
      /// Rotates given <paramref name="value"/> left <paramref name="shift"/> amount of bytes.
      /// </summary>
      /// <param name="value">The value to rotate to left.</param>
      /// <param name="shift">The amount to bits to rotate.</param>
      /// <returns>The rotated value.</returns>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int64 RotateLeft( this Int64 value, Int32 shift )
      {
         return (Int64) ( (UInt64) value ).RotateLeft( shift );
      }

      /// <summary>
      /// Rotates given <paramref name="value"/> left <paramref name="shift"/> amount of bytes.
      /// </summary>
      /// <param name="value">The value to rotate to left.</param>
      /// <param name="shift">The amount to bits to rotate.</param>
      /// <returns>The rotated value.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt64 RotateLeft( this UInt64 value, Int32 shift )
      {
         return ( value << shift ) | ( value >> ( sizeof( UInt64 ) * 8 - shift ) );
      }

      /// <summary>
      /// Rotates given <paramref name="value"/> right <paramref name="shift"/> amount of bytes.
      /// </summary>
      /// <param name="value">The value to rotate to right.</param>
      /// <param name="shift">The amount to bits to rotate.</param>
      /// <returns>The rotated value.</returns>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int64 RotateRight( this Int64 value, Int32 shift )
      {
         return (Int64) ( (UInt64) value ).RotateRight( shift );
      }

      /// <summary>
      /// Rotates given <paramref name="value"/> right <paramref name="shift"/> amount of bytes.
      /// </summary>
      /// <param name="value">The value to rotate to right.</param>
      /// <param name="shift">The amount to bits to rotate.</param>
      /// <returns>The rotated value.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt64 RotateRight( this UInt64 value, Int32 shift )
      {
         return ( value >> shift ) | ( value << ( sizeof( UInt64 ) * 8 - shift ) );
      }



      /// <summary>
      /// Rounds given value up to next alignment, which should be a power of two.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="multiple">The alignment.</param>
      /// <returns>Value rounded up to next alignment.</returns>
      /// <remarks>
      /// Will return incorrect results if <paramref name="multiple"/> is zero.
      /// </remarks>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 RoundUpI32( this Int32 value, Int32 multiple )
      {
         return ( multiple - 1 + value ) & ~( multiple - 1 );
      }

      /// <summary>
      /// Rounds given value up to next alignment, which should be a power of two.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="multiple">The alignment.</param>
      /// <returns>Value rounded up to next alignment.</returns>
      /// <remarks>
      /// Will return incorrect results if <paramref name="multiple"/> is zero.
      /// </remarks>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt32 RoundUpU32( this UInt32 value, UInt32 multiple )
      {
         return ( multiple - 1 + value ) & ~( multiple - 1 );
      }

      /// <summary>
      /// Rounds given value up to next alignment, which should be a power of two.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="multiple">The alignment.</param>
      /// <returns>Value rounded up to next alignment.</returns>
      /// <remarks>
      /// Will return incorrect results if <paramref name="multiple"/> is zero.
      /// </remarks>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int64 RoundUpI64( this Int64 value, Int64 multiple )
      {
         return ( multiple - 1 + value ) & ~( multiple - 1 );
      }

      /// <summary>
      /// Rounds given value up to next alignment, which should be a power of two.
      /// </summary>
      /// <param name="value">The value.</param>
      /// <param name="multiple">The alignment.</param>
      /// <returns>Value rounded up to next alignment.</returns>
      /// <remarks>
      /// Will return incorrect results if <paramref name="multiple"/> is zero.
      /// </remarks>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt64 RoundUpU64( this UInt64 value, UInt64 multiple )
      {
         return ( multiple - 1 + value ) & ~( multiple - 1 );
      }

      /// <summary>
      /// This method counts how many bits are set in a given value.
      /// </summary>
      /// <param name="value">The value to count bits set.</param>
      /// <returns>How many bits are set in a given value.</returns>
      /// <remarks>
      /// This algorithm is from <see href="https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel"/>.
      /// </remarks>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 CountBitsSetI32( this Int32 value )
      {
         return (Int32) CountBitsSetU32( (UInt32) value );
      }

      /// <summary>
      /// This method counts how many bits are set in a given value.
      /// </summary>
      /// <param name="value">The value to count bits set.</param>
      /// <returns>How many bits are set in a given value.</returns>
      /// <remarks>
      /// This algorithm is from <see href="https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel"/>.
      /// </remarks>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt32 CountBitsSetU32( this UInt32 value )
      {
         unchecked
         {
            value = value - ( ( value >> 1 ) & 0x55555555u );
            value = ( value & 0x33333333u ) + ( ( value >> 2 ) & 0x33333333u );
            return ( ( value + ( value >> 4 ) & 0x0F0F0F0Fu ) * 0x01010101u ) >> 24;
         }
      }

      /// <summary>
      /// This method counts how many bits are set in a given value.
      /// </summary>
      /// <param name="value">The value to count bits set.</param>
      /// <returns>How many bits are set in a given value.</returns>
      /// <remarks>
      /// This algorithm is from <see href="https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel"/>.
      /// </remarks>
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Int32 CountBitsSetI64( this Int64 value )
      {
         return (Int32) CountBitsSetU64( (UInt64) value );
      }

      /// <summary>
      /// This method counts how many bits are set in a given value.
      /// </summary>
      /// <param name="value">The value to count bits set.</param>
      /// <returns>How many bits are set in a given value.</returns>
      /// <remarks>
      /// This algorithm is from <see href="https://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel"/>.
      /// </remarks>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static UInt32 CountBitsSetU64( this UInt64 value )
      {
         unchecked
         {
            value = value - ( ( value >> 1 ) & 0x5555555555555555UL );
            value = ( value & 0x3333333333333333UL ) + ( ( value >> 2 ) & 0x3333333333333333UL );
            return (UInt32) ( ( ( value + ( value >> 4 ) & 0x0F0F0F0F0F0F0F0FUL ) * 0x0101010101010101UL ) >> 56 );
         }
      }



      /// <summary>
      /// Checks whether given unsigned integer is power of two.
      /// </summary>
      /// <param name="val">The integer to check.</param>
      /// <returns><c>true</c> if the integer is power of two (and thus greater than zero); <c>false</c> otherwise.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Boolean IsPowerOfTwo( this UInt32 val )
      {
         return val != 0 && unchecked(( val & ( val - 1 ) )) == 0;
      }

      /// <summary>
      /// Checks whether given unsigned integer is power of two.
      /// </summary>
      /// <param name="val">The integer to check.</param>
      /// <returns><c>true</c> if the integer is power of two (and thus greater than zero); <c>false</c> otherwise.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Boolean IsPowerOfTwo( this UInt64 val )
      {
         return val != 0 && unchecked(( val & ( val - 1 ) )) == 0;
      }

      /// <summary>
      /// Checks whehter given unsigned integer is even.
      /// Zero is considered to be even.
      /// </summary>
      /// <param name="val">The integer to check.</param>
      /// <returns><c>true</c> if the integer is even; <c>false</c> otherwise.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Boolean IsEven( this UInt32 val )
      {
         return ( val & 1 ) == 0;
      }

      /// <summary>
      /// Checks whehter given unsigned integer is even.
      /// Zero is considered to be even.
      /// </summary>
      /// <param name="val">The integer to check.</param>
      /// <returns><c>true</c> if the integer is even; <c>false</c> otherwise.</returns>
      [CLSCompliant( false )]
#if !NET40
      [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
      public static Boolean IsEven( this UInt64 val )
      {
         return ( val & 1 ) == 0;
      }
   }
}
