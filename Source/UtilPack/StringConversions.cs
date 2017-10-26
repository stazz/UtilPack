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
   /// <summary>
   /// This class contains useful methods to convert things to and from strings, including e.g. hexadecimal and base64 conversions.
   /// </summary>
   public static class StringConversions
   {
      private static readonly Char[] LOOKUP_UC = new Char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
      private static readonly Char[] LOOKUP_LC = new Char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

      /// <summary>
      /// This method creates a textual representation in hexadecimal format of byte array <paramref name="data"/>.
      /// </summary>
      /// <param name="data">Byte array to create textual representation from.</param>
      /// <param name="upperCaseHexes"><c>true</c> if alpha characters in should be in upper case; <c>false</c> otherwise.</param>
      /// <returns><c>null</c> if <paramref name="data"/> is <c>null</c>; otherwise textual representation of the byte array in hexadecimal format.</returns>
      /// <remarks>This is modified version represented on <see href="http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/3928b8cb-3703-4672-8ccd-33718148d1e3/" /> , Matthew Fraser's and following PZahra's posts.</remarks>
      public static String CreateHexString( this Byte[] data, Boolean upperCaseHexes = false )
      {
         return CreateHexString( data, 0, data.Length, upperCaseHexes );
      }

      /// <summary>
      /// This method creates a textual representation in hexadecimal format of byte array <paramref name="data"/>.
      /// </summary>
      /// <param name="data">Byte array to create textual representation from.</param>
      /// <param name="offset">Offset in the array to start reading bytes.</param>
      /// <param name="count">How many bytes read from array.</param>
      /// <param name="upperCaseHexes"><c>true</c> if alpha characters in should be in upper case; <c>false</c> otherwise.</param>
      /// <returns><c>null</c> if <paramref name="data"/> is <c>null</c>; otherwise textual representation of the byte array in hexadecimal format.</returns>
      /// <remarks>This is modified version represented on <see href="http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/3928b8cb-3703-4672-8ccd-33718148d1e3/" /> , Matthew Fraser's and following PZahra's posts.</remarks>
      public static String CreateHexString( this Byte[] data, Int32 offset, Int32 count, Boolean upperCaseHexes )
      {
         String result;
         if ( data == null )
         {
            result = null;
         }
         else
         {
            var lookup = upperCaseHexes ? LOOKUP_UC : LOOKUP_LC;
            Int32 p = 0;
            Char[] c = new Char[count * 2];
            Byte d;
            Int32 i = 0;
            while ( i < count )
            {
               d = data[offset++];
               c[p++] = lookup[d / 0x10];
               c[p++] = lookup[d % 0x10];
               ++i;
            }
            result = new String( c, 0, c.Length );
         }
         return result;
      }

      /// <summary>
      /// This method creates a byte array based on string which is assumed to be in hexadecimal format.
      /// </summary>
      /// <param name="str">The string containing byte array in hexadecimal format.</param>
      /// <param name="offset">The offset in string to start reading characters.</param>
      /// <param name="step">How many characters to skip after each successful single byte read.</param>
      /// <param name="tail">How many characters to leave unread at the end of the string.</param>
      /// <returns>A byte array containing logically same bytes as the given string in hexadecimal format.</returns>
      /// <remarks>This is modified version represented on <see href="http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/3928b8cb-3703-4672-8ccd-33718148d1e3/" /> , Matthew Fraser's and following PZahra's posts.</remarks>
      public static Byte[] CreateHexBytes( this String str, Int32 offset = 0, Int32 step = 0, Int32 tail = 0 )
      {
         Byte[] b;
         if ( str != null )
         {
            b = new Byte[( str.Length - offset - tail + step ) / ( 2 + step )];
            Byte c1, c2;
            Int32 l = str.Length - tail;
            Int32 s = step + 1;
            for ( Int32 y = 0, x = offset; x < l; ++y, x += s )
            {
               c1 = (Byte) str[x];
               if ( c1 > 0x60 )
               {
                  c1 -= 0x57;
               }
               else if ( c1 > 0x40 )
               {
                  c1 -= 0x37;
               }
               else
               {
                  c1 -= 0x30;
               }
               c2 = (Byte) str[++x];
               if ( c2 > 0x60 )
               {
                  c2 -= 0x57;
               }
               else if ( c2 > 0x40 )
               {
                  c2 -= 0x37;
               }
               else
               {
                  c2 -= 0x30;
               }
               b[y] = (Byte) ( ( c1 << 4 ) + c2 );
            }
         }
         else
         {
            b = null;
         }
         return b;
      }

      private const Int32 BYTE_SIZE = 8;
      private const Int32 ALL_ONES = Byte.MaxValue;

      private const Char CHAR62_URLSAFE = '-';
      private const Char CHAR62_URLUNSAFE = '+';
      private const Char CHAR63_URLSAFE = '_';
      private const Char CHAR63_URLUNSAFE = '/';

      /// <summary>
      /// Helper method to call <see cref="EncodeBinary(Byte[], Int32, Int32,Char[])"/> using base64-encoding.
      /// </summary>
      /// <param name="bytes">The bytes to encode.</param>
      /// <param name="useURLSafeEncoding">Whether to use characters safe for URL addresses for characters for indices <c>62</c> and <c>63</c>.</param>
      /// <returns>Base64-encoded string of <paramref name="bytes"/>.</returns>
      /// <remarks>
      /// When <paramref name="useURLSafeEncoding"/> is <c>true</c>, the characters for indices <c>62</c> and <c>63</c> are <c>-</c> and <c>_</c>, respectively.
      /// Otherwise, the characters for indices <c>62</c> and <c>63</c> are <c>+</c> and <c>/</c>, respectively.
      /// </remarks>
      public static String EncodeBase64( this Byte[] bytes, Boolean useURLSafeEncoding )
      {
         return bytes.EncodeBase64( 0, bytes == null ? -1 : bytes.Length, useURLSafeEncoding );
      }

      /// <summary>
      /// Helper method to call <see cref="EncodeBinary(Byte[], Int32, Int32,Char[])"/> using base64-encoding.
      /// </summary>
      /// <param name="bytes">The bytes to encode.</param>
      /// <param name="start">The offset where to start encoding bytes.</param>
      /// <param name="length">The amount of bytes to encode.</param>
      /// <param name="useURLSafeEncoding">Whether to use characters safe for URL addresses for characters for indices <c>62</c> and <c>63</c>.</param>
      /// <returns>Base64-encoded string of <paramref name="bytes"/>.</returns>
      /// <remarks>
      /// When <paramref name="useURLSafeEncoding"/> is <c>true</c>, the characters for indices <c>62</c> and <c>63</c> are <c>-</c> and <c>_</c>, respectively.
      /// Otherwise, the characters for indices <c>62</c> and <c>63</c> are <c>+</c> and <c>/</c>, respectively.
      /// </remarks>
      public static String EncodeBase64( this Byte[] bytes, Int32 start, Int32 length, Boolean useURLSafeEncoding )
      {
         return bytes.EncodeBinary( start, length, CreateBase64EncodeLookupTable( useURLSafeEncoding ) );
      }


      /// <summary>
      /// Encodes whole given byte array using given character array as a lookup array for each unit.
      /// </summary>
      /// <param name="bytes">The byte array to encode.</param>
      /// <param name="lookupTable">The lookup table to use.</param>
      /// <returns>Encoded string.</returns>
      /// <exception cref="ArgumentException">If <paramref name="lookupTable"/> is <c>null</c> or empty.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="bytes"/> or <paramref name="lookupTable"/> are <c>null</c>.</exception>
      /// <remarks>
      /// Currently no padding is used.
      /// The option to control emitting of padding is to be added in the future.
      /// The block size is based on the size of the lookup table, however up to 8 bits.
      /// So for example, in order to encode binary using radix-64 representation (e.g. base64), the lookup table should contain at least 64 and at most 127 elements.
      /// </remarks>
      public static String EncodeBinary( this Byte[] bytes, Char[] lookupTable )
      {
         return EncodeBinary( bytes, 0, bytes == null ? -1 : bytes.Length, lookupTable );
      }

      /// <summary>
      /// Encodes given byte array using given character array as a lookup array for each unit.
      /// </summary>
      /// <param name="bytes">The byte array to encode.</param>
      /// <param name="start">The index to start reading the array.</param>
      /// <param name="length">The amount of bytes to read from the array.</param>
      /// <param name="lookupTable">The mapping from encoding unit into its character representation. Should have (at least) one element.</param> 
      /// <returns>String representing binary data encoded with <paramref name="lookupTable"/>.</returns>
      /// <exception cref="ArgumentException">If <paramref name="lookupTable"/> is <c>null</c> or empty, or if <paramref name="start"/> and <paramref name="length"/> would result in reading outside array bounds.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="bytes"/> or <paramref name="lookupTable"/> are <c>null</c>.</exception>
      /// <remarks>
      /// Currently no padding is used.
      /// The option to control emitting of padding is to be added in the future.
      /// The block size is based on the size of the lookup table, however up to 8 bits.
      /// So for example, in order to encode binary using radix-64 representation (e.g. base64), the lookup table should contain at least 64 and at most 127 elements.
      /// </remarks>
      public static String EncodeBinary(
         this Byte[] bytes,
         Int32 start,
         Int32 length,
         Char[] lookupTable
         )
      {
         // <c>2^</c><paramref name="unitSize"/> elements.</param>
         // <param name="unitSize">The amount of bits of each encoding unit. For example, to encode in Base64, this should be set to <c>6</c>, as <c>2^6</c> is <c>64</c>.</param>
         // <exception cref="NotImplementedException">If <paramref name="unitSize"/> is more than <c>8</c>.</exception>
         bytes.CheckArrayArguments( start, length );
         ArgumentValidator.ValidateNotNull( "Lookup table", lookupTable );
         var unitSize = CheckUnitSize( lookupTable.Length );

         // Compute amount of characters needed
         var chars = new Char[BinaryUtils.AmountOfPagesTaken( ( length - start ) * BYTE_SIZE, unitSize )];

         var bit = start * BYTE_SIZE;
         var max = start + length;
         for ( var i = 0; i < chars.Length; ++i, bit += unitSize )
         {
            var startByte = bit / BYTE_SIZE;
            var endByte = ( bit + unitSize - 1 ) / BYTE_SIZE;
            // How many MSB's to skip
            var msbSkip = bit % BYTE_SIZE;
            Int32 idx;
            if ( startByte == endByte )
            {
               // Skip msbSkip MSB, extract next unitSize bits, and skip final 8 - unitsize - msbSkip bits
               var lsbSkip = BYTE_SIZE - unitSize - msbSkip;
               var mask = ( ALL_ONES >> msbSkip ) & ( ALL_ONES << lsbSkip );
               idx = ( bytes[startByte] & mask ) >> lsbSkip;
            }
            else
            {
               // For first byte: skip msbSkip MSB, extract final bits
               // For next byte: extract unitsize - previous byte final bits size MSB
               var firstMask = ALL_ONES >> msbSkip;
               // Amount of bits in second byte
               var secondByteBits = ( unitSize + msbSkip - BYTE_SIZE );
               idx = ( ( bytes[startByte] & firstMask ) << secondByteBits );
               if ( endByte < max )
               {
                  var secondMask = ( ALL_ONES << ( BYTE_SIZE - secondByteBits ) ) & ALL_ONES;
                  idx |= ( ( bytes[endByte] & secondMask ) >> ( BYTE_SIZE - secondByteBits ) );
               }
            }

            chars[i] = lookupTable[idx];
         }

         return new String( chars );
      }

      /// <summary>
      /// Helper method to call <see cref="DecodeBinary(String, Int32, Int32[])"/> using base64-decoding.
      /// </summary>
      /// <param name="encoded">The base64-encoded string.</param>
      /// <param name="useURLSafeEncoding">Whether the string was encoded using characters safe for URLs, for indices <c>62</c> and <c>63</c>.</param>
      /// <returns>Byte array representing base64-decoded byte sequence from given string.</returns>
      public static Byte[] DecodeBase64( this String encoded, Boolean useURLSafeEncoding )
      {
         return encoded.DecodeBinary( 6, CreateBase64DecodeLookupTable( useURLSafeEncoding ) );
      }

      /// <summary>
      /// Decodes given string into a byte array.
      /// </summary>
      /// <param name="encoded">The encoded string.</param>
      /// <param name="unitSize">The amount of bits single character takes. This should be <c>6</c> for radix-64 encoding.</param>
      /// <param name="lookupTable">The lookup table for each byte. The integer value of characters in string is used as index to this array.</param>
      /// <returns>Byte array representing decoded byte sequence from given string.</returns>
      public static Byte[] DecodeBinary(
         this String encoded,
         Int32 unitSize,
         Int32[] lookupTable
         )
      {
         //var unitSize = CheckUnitSize( lookupTable.Length );
         var gcd = BinaryUtils.GCD( unitSize, BYTE_SIZE );
         var charBlockSize = BYTE_SIZE / gcd;
         var lastBlockSize = encoded.Length % charBlockSize;
         var arrayLen = ( encoded.Length / charBlockSize ) * ( unitSize / gcd );
         if ( lastBlockSize != 0 )
         {
            arrayLen += ( lastBlockSize * unitSize ) / BYTE_SIZE;
         }
         var array = new Byte[arrayLen];
         DecodeBinary( encoded, array, 0, arrayLen, unitSize, lookupTable );
         return array;
      }

      /// <summary>
      /// Decodes given string into a byte array, starting at given offset in byte array, and reading up to given byte count.
      /// </summary>
      /// <param name="encoded">The encoded string.</param>
      /// <param name="bytes">The byte array to decode bytes to.</param>
      /// <param name="bytesStart">The offset in byte array to start decoding bytes to.</param>
      /// <param name="bytesCount">The maximum amount of bytes to decode.</param>
      /// <param name="unitSize">The amount of bits single character takes. This should be <c>6</c> for radix-64 encoding.</param>
      /// <param name="lookupTable">The lookup table for each byte. The integer value of characters in string is used as index to this array.</param>
      /// <returns>Amount of bytes read.</returns>
      public static Int32 DecodeBinary(
         this String encoded,
         Byte[] bytes,
         Int32 bytesStart,
         Int32 bytesCount,
         Int32 unitSize,
         Int32[] lookupTable
         )
      {
         bytes.CheckArrayArguments( bytesStart, bytesCount );
         ArgumentValidator.ValidateNotNull( "Encoded string", encoded );
         ArgumentValidator.ValidateNotNull( "Lookup table", lookupTable );

         // Each iteration goes 1 character forward in string
         var sMax = encoded.Length;
         var byteMax = bytesStart + bytesCount;
         var bMax = byteMax * BYTE_SIZE;
         var bit = bytesStart * BYTE_SIZE;

         for ( var strIdx = 0; strIdx < sMax && bit < bMax; ++strIdx, bit += unitSize )
         {
            var c = encoded[strIdx];
            var value = lookupTable[(Int32) c];
            if ( value == -1 )
            {
               throw new InvalidOperationException( "Character \"" + c + "\" not found from lookup table." );
            }
            else
            {
               var startByte = bit / BYTE_SIZE;
               var endByte = ( bit + unitSize - 1 ) / BYTE_SIZE;
               // How many MSB's to skip
               var skip = bit % BYTE_SIZE;
               if ( startByte == endByte )
               {
                  // Keep the MSB, shift value to left (in case e.g. unit size is 3 and MSB is 1) 
                  bytes[startByte] = (Byte) ( bytes[startByte] | ( value << ( BYTE_SIZE - unitSize - skip ) ) );
               }
               else
               {
                  // Keep the existing MSB, and extract the MSB from the value
                  // Amount of bits to use up in second byte
                  skip = unitSize + skip - BYTE_SIZE;
                  // Extract the MSB from value
                  bytes[startByte] = (Byte) ( bytes[startByte] | ( value >> skip ) );

                  if ( endByte < byteMax )
                  {
                     // Extract the LSB from the value
                     bytes[endByte] = (Byte) ( value << ( BYTE_SIZE - skip ) );
                  }
               }
            }
         }

         return bit / BYTE_SIZE;
      }

      private static Int32 CheckUnitSize( Int32 lookupTableSize )
      {
         lookupTableSize = BinaryUtils.Log2( (UInt32) lookupTableSize );
         if ( lookupTableSize < 1 )
         {
            throw new ArgumentException( "Unit size must be at least one." );
         }
         else if ( lookupTableSize > BYTE_SIZE )
         {
            lookupTableSize = BYTE_SIZE; // throw new ArgumentException( "Unit size must be at most " + BYTE_SIZE + "." );
         }
         return lookupTableSize;
      }

      /// <summary>
      /// This is helper method to generate lookup table for encoding byte arrays using normal URL-safe or unsafe base64 encoding.
      /// </summary>
      /// <param name="isURLSafe">Whether encoding should produce strings that are safe to use in URLs.</param>
      /// <returns>
      /// The standard base64 encoding array with <c>-</c> and <c>_</c> as last characters, if <paramref name="isURLSafe"/> is true.
      /// Otherwise the last two characters will be <c>+</c> and <c>/</c>.
      /// </returns>
      public static Char[] CreateBase64EncodeLookupTable( Boolean isURLSafe )
      {
         // Use fully const arrays in order for them to be incorporated into binary file and thus have very fast initialization.
         return isURLSafe ?
            new[]
            {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
            'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
            'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
            'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
            'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
            'w', 'x', 'y', 'z', '0', '1', '2', '3',
            '4', '5', '6', '7', '8', '9', CHAR62_URLSAFE, CHAR63_URLSAFE
            } :
            new[]
            {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
            'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
            'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
            'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
            'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
            'w', 'x', 'y', 'z', '0', '1', '2', '3',
            '4', '5', '6', '7', '8', '9', CHAR62_URLUNSAFE, CHAR63_URLUNSAFE
            };
      }

      public static Int32[] CreateBase64DecodeLookupTable( Boolean isURLSafe )
      {
         // Use fully const arrays in order for them to be incorporated into binary file and thus have very fast initialization.
         return isURLSafe ?
            new[]
            {
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, 62, -1, -1, 52, 53,
               54, 55, 56, 57, 58, 59, 60, 61, -1, -1,
               -1, -1, -1, -1, -1,  0,  1,  2,  3,  4,
                5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
               15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
               25, -1, -1, -1, -1, 63, -1, 26, 27, 28,
               29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
               39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
               49, 50, 51, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1
            } :
            new[]
            {
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, 62, -1, -1, -1, 63, 52, 53,
               54, 55, 56, 57, 58, 59, 60, 61, -1, -1,
               -1, -1, -1, -1, -1,  0,  1,  2,  3,  4,
                5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
               15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
               25, -1, -1, -1, -1, 63, -1, 26, 27, 28,
               29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
               39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
               49, 50, 51, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
               -1, -1, -1, -1, -1, -1
            };
      }

      /// <summary>
      /// This is helper method to generate lookup table for encoding byte arrays using customized base64 encoding.
      /// </summary>
      /// <param name="char62">The character for index <c>62</c>.</param>
      /// <param name="char63">The character for index <c>63</c>.</param>
      /// <returns>The standard base64 encoding array with customized characters for indices <c>62</c> and <c>63</c>.</returns>
      /// <seealso cref="EncodeBinary(Byte[], Int32, Int32, Char[])"/>
      /// <seealso cref="EncodeBase64(Byte[], Boolean)"/>
      public static Char[] CreateBase64EncodeLookupTable( Char char62, Char char63 )
      {
         return new[]
         {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
            'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
            'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
            'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
            'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
            'w', 'x', 'y', 'z', '0', '1', '2', '3',
            '4', '5', '6', '7', '8', '9', char62, char63
         };
      }


      /// <summary>
      /// This is helper method to generate lookup table for decoding strings, encoded with given look up table using various encoding methods (e.g. <see cref="EncodeBinary(Byte[], Int32, Int32, Char[])"/>, <see cref="EncodeBase64(Byte[], Boolean)"/>) in this class.
      /// </summary>
      /// <param name="encodeLookupTable">The table used to encode binary data.</param>
      /// <returns>The array with 256 elements, with character used as index giving the numerical value of it.</returns>
      /// <exception cref="IndexOutOfRangeException">If any character in <paramref name="encodeLookupTable"/> has its integer value 256 or greater.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="encodeLookupTable"/> is <c>null</c>.</exception>
      public static Int32[] CreateDecodeLookupTable( Char[] encodeLookupTable )
      {
         ArgumentValidator.ValidateNotNull( "Encode lookup table", encodeLookupTable );
         var retVal = new Int32[Byte.MaxValue];
         retVal.Fill( -1 );
         for ( var i = 0; i < encodeLookupTable.Length; ++i )
         {
            retVal[(Int32) encodeLookupTable[i]] = i;
         }

         return retVal;
      }
   }
}
