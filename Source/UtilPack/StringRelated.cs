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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace UtilPack
{
   /// <summary>
   /// This interface encapsulates useful information about an <see cref="System.Text.Encoding"/> in order to enable some functionality related to parsing strings without actually allocating new <see cref="String"/> objects.
   /// </summary>
   /// <remarks>
   /// See also various extension methods for this interface in <see cref="E_UtilPack"/>.
   /// </remarks>
   public interface IEncodingInfo
   {
      /// <summary>
      /// Gets the <see cref="System.Text.Encoding"/> used to encode and decode strings.
      /// </summary>
      /// <value>The <see cref="System.Text.Encoding"/> used to encode and decode strings.</value>
      Encoding Encoding { get; }

      /// <summary>
      /// Gets the amount of bytes used to encode one ASCII character (<c>0 ≤ ascii_char ≤ 127</c>).
      /// </summary>
      /// <value>The amount of bytes used to encode one ASCII character.</value>
      Int32 BytesPerASCIICharacter { get; }

      /// <summary>
      /// This method will read one ASCII character from given byte array, increasing given index as required.
      /// </summary>
      /// <param name="array">The byte array to read ASCII character from.</param>
      /// <param name="idx">The index in <paramref name="array"/> where to start reading.</param>
      /// <returns>The <paramref name="array"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
      Byte ReadASCIIByte( Byte[] array, ref Int32 idx );

      /// <summary>
      /// This method will write one ASCII character to given byte array, increasing given index as required.
      /// </summary>
      /// <param name="array">The byte array to write ASCII character to.</param>
      /// <param name="idx">The index in <paramref name="array"/> where ot start writing.</param>
      /// <param name="asciiByte">The ASCII character (<c>0 ≤ ascii_char ≤ 127</c>).</param>
      /// <returns>This object.</returns>
      IEncodingInfo WriteASCIIByte( Byte[] array, ref Int32 idx, Byte asciiByte );

      /// <summary>
      /// Gets the minimum amount of bytes that is required for any character.
      /// </summary>
      /// <value>The minimum amount of bytes that is required for any character.</value>
      Int32 MinCharByteCount { get; }

      /// <summary>
      /// Gets the maximum amount of bytes that is required for any character.
      /// </summary>
      /// <value>The maximum amount of bytes that is required for any character.</value>
      Int32 MaxCharByteCount { get; }
   }

   /// <summary>
   /// This class implements <see cref="IEncodingInfo"/> for <see cref="UTF8Encoding"/>.
   /// </summary>
   /// <remarks>
   /// This class has no other state than given <see cref="UTF8Encoding"/>.
   /// </remarks>
   public sealed class UTF8EncodingInfo : IEncodingInfo
   {
      /// <summary>
      /// Creates new instance of <see cref="UTF8EncodingInfo"/> with optional existing instance of <see cref="UTF8Encoding"/>.
      /// </summary>
      /// <param name="encoding">The optional existing instance of <see cref="UTF8Encoding"/>.</param>
      /// <remarks>
      /// If <paramref name="encoding"/> is not given, then new instance of <see cref="UTF8Encoding"/> is created, passing <c>false</c> to both boolean parameters of the <see cref="UTF8Encoding(bool, bool)"/>
      /// </remarks>
      public UTF8EncodingInfo(
         UTF8Encoding encoding = null
         )
      {
         this.Encoding = encoding ?? new UTF8Encoding( false, false );
      }

      /// <summary>
      /// Gets the <see cref="UTF8Encoding"/> object.
      /// </summary>
      /// <value>The <see cref="UTF8Encoding"/> object.</value>
      public Encoding Encoding { get; }

      /// <summary>
      /// Returns <c>1</c>.
      /// </summary>
      /// <value><c>1</c>.</value>
      public Int32 BytesPerASCIICharacter
      {
         get
         {
            return 1;
         }
      }

      /// <inheritdoc />
      public Byte ReadASCIIByte( Byte[] array, ref Int32 idx )
      {
         // UTF8 ASCII bytes are just normal bytes
         return ArgumentValidator.ValidateNotNull( nameof( array ), array )[idx++];
      }

      /// <inheritdoc />
      public IEncodingInfo WriteASCIIByte( Byte[] array, ref Int32 idx, Byte asciiByte )
      {
         // UTF8 ASCII bytes are just normal bytes
         ArgumentValidator.ValidateNotNull( nameof( array ), array )[idx++] = asciiByte;
         return this;
      }

      /// <summary>
      /// Returns <c>1</c>.
      /// </summary>
      /// <value><c>1</c>.</value>
      public Int32 MinCharByteCount
      {
         get
         {
            return 1;
         }
      }

      /// <summary>
      /// Returns <c>4</c>.
      /// </summary>
      /// <value><c>4</c>.</value>
      public Int32 MaxCharByteCount
      {
         get
         {
            return 4;
         }
      }
   }

   /// <summary>
   /// This is abstract class for information about little-endian and big-endian UTF-16 encodings represented by <see cref="UnicodeEncoding"/> class.
   /// </summary>
   /// <remarks>
   /// This class has no other state than given <see cref="UnicodeEncoding"/>.
   /// </remarks>
   public abstract class UTF16EncodingInfo : IEncodingInfo
   {
      private const Int32 SIZE = sizeof( Int16 );

      internal UTF16EncodingInfo( UnicodeEncoding encoding )
      {
         this.Encoding = ArgumentValidator.ValidateNotNull( nameof( encoding ), encoding );
      }

      /// <summary>
      /// Gets the <see cref="UnicodeEncoding"/> object.
      /// </summary>
      /// <value>The <see cref="UnicodeEncoding"/> object.</value>
      public Encoding Encoding { get; }

      /// <summary>
      /// Returns <c>2</c>.
      /// </summary>
      /// <value><c>2</c>.</value>
      public Int32 BytesPerASCIICharacter => SIZE;

      /// <summary>
      /// Returns <c>2</c>.
      /// </summary>
      /// <value><c>2</c>.</value>
      public Int32 MinCharByteCount => SIZE;

      /// <summary>
      /// Returns <c>2</c>.
      /// </summary>
      /// <value><c>2</c>.</value>
      public Int32 MaxCharByteCount => SIZE * 2;

      /// <summary>
      /// Reads one byte as ASCII byte.
      /// </summary>
      /// <param name="array">The byte array.</param>
      /// <param name="idx">The index in <paramref name="array"/> to read ASCII byte from. This will be advanced by <c>2</c>.</param>
      /// <returns>The read ASCII byte.</returns>
      public Byte ReadASCIIByte( Byte[] array, ref Int32 idx )
      {
         idx += SIZE;
         return this.DoReadASCIIByte( array, idx - SIZE );
      }

      /// <summary>
      /// Writes one ASCII byte to given array, and clears other byte, depending on endianness.
      /// </summary>
      /// <param name="array">The byte array.</param>
      /// <param name="idx">The index in <paramref name="array"/> to write character data to. This will be advanced by <c>2</c>.</param>
      /// <param name="asciiByte">The ASCII byte to write.</param>
      /// <returns>This <see cref="UTF16EncodingInfo"/>.</returns>
      public IEncodingInfo WriteASCIIByte( Byte[] array, ref Int32 idx, Byte asciiByte )
      {
         idx += SIZE;
         this.DoWriteASCIIByte( array, idx - SIZE, asciiByte );
         return this;
      }

      internal abstract Byte DoReadASCIIByte( Byte[] array, Int32 idx );
      internal abstract void DoWriteASCIIByte( Byte[] array, Int32 idx, Byte asciiByte );

      internal static Boolean IsBigEndian( Encoding encoding )
      {
         var bytez = encoding.GetBytes( "a" );
         return bytez[0] == 0;
      }
   }

   /// <summary>
   /// This class contains information about little-endian UTF-16 encoding.
   /// </summary>
   /// <remarks>
   /// This class has no other state than given <see cref="UnicodeEncoding"/>.
   /// </remarks>
   public sealed class UTF16LEEncodingInfo : UTF16EncodingInfo
   {
      /// <summary>
      /// Creates new instance of <see cref="UTF16LEEncodingInfo"/>.
      /// </summary>
      /// <param name="encoding">The optional <see cref="UnicodeEncoding"/> to use.</param>
      /// <exception cref="ArgumentException">If <paramref name="encoding"/> was specified, and it was not little-endian.</exception>
      public UTF16LEEncodingInfo( UnicodeEncoding encoding )
         : base( encoding ?? new UnicodeEncoding( false, false, false ) )
      {
         if ( encoding != null
            && !ReferenceEquals( encoding, Encoding.Unicode )
            && IsBigEndian( encoding )
            )
         {
            throw new ArgumentException( "The given UTF-16 encoding was big-endian, but this class is for little-endian UTF-16." );
         }
      }

      internal override Byte DoReadASCIIByte( Byte[] array, Int32 idx )
      {
         return array[idx];
      }

      internal override void DoWriteASCIIByte( Byte[] array, Int32 idx, Byte asciiByte )
      {
         array[idx] = asciiByte;
      }
   }

   /// <summary>
   /// This class contains information about big-endian UTF-16 encoding.
   /// </summary>
   /// <remarks>
   /// This class has no other state than given <see cref="UnicodeEncoding"/>.
   /// </remarks>
   public sealed class UTF16BEEncodingInfo : UTF16EncodingInfo
   {
      /// <summary>
      /// Creates new instance of <see cref="UTF16BEEncodingInfo"/>.
      /// </summary>
      /// <param name="encoding">The optional <see cref="UnicodeEncoding"/> to use.</param>
      /// <exception cref="ArgumentException">If <paramref name="encoding"/> was specified, and it was not big-endian.</exception>
      public UTF16BEEncodingInfo( UnicodeEncoding encoding )
         : base( encoding ?? new UnicodeEncoding( true, false, false ) )
      {
         if ( encoding != null
            && !ReferenceEquals( encoding, Encoding.BigEndianUnicode )
            && !IsBigEndian( encoding )
            )
         {
            throw new ArgumentException( "The given UTF-16 encoding was little-endian, but this class is for big-endian UTF-16." );
         }
      }

      internal override Byte DoReadASCIIByte( Byte[] array, Int32 idx )
      {
         return array[idx + 1];
      }

      internal override void DoWriteASCIIByte( Byte[] array, Int32 idx, Byte asciiByte )
      {
         array[idx] = 0;
         array[idx + 1] = asciiByte;
      }
   }

#if NET_40 || NETSTANDARD1_5 || NET_45

   /// <summary>
   /// This class contains information about UTF-32 encoding.
   /// </summary>
   /// <remarks>
   /// This class has no other state than given <see cref="UTF32Encoding"/>.
   /// </remarks>
   public sealed class UTF32EncodingInfo : IEncodingInfo
   {
      private const Int32 SIZE = sizeof( Int32 );

      /// <summary>
      /// Creates a new instance of <see cref="UTF32EncodingInfo"/>.
      /// </summary>
      /// <param name="encoding">The optional <see cref="UTF32Encoding"/> to use.</param>
      public UTF32EncodingInfo(
         UTF32Encoding encoding = null
         )
      {
         var isBigEndian = encoding != null && UTF16EncodingInfo.IsBigEndian( encoding );
         this.IsBigEndian = isBigEndian;
         this.Encoding = encoding ?? new UTF32Encoding( isBigEndian, false, false );
      }

      /// <summary>
      /// Gets the <see cref="UTF32Encoding"/> object.
      /// </summary>
      /// <value>The <see cref="UTF32Encoding"/> object.</value>
      public Encoding Encoding { get; }

      /// <summary>
      /// Gets the value indicating whether this encoding is big-endian.
      /// </summary>
      /// <value>The value indicating whether this encoding is big-endian.</value>
      public Boolean IsBigEndian { get; }

      /// <summary>
      /// Returns <c>4</c>.
      /// </summary>
      /// <value><c>4</c>.</value>
      public Int32 BytesPerASCIICharacter => SIZE;

      /// <summary>
      /// Returns <c>4</c>.
      /// </summary>
      /// <value><c>4</c>.</value>
      public Int32 MinCharByteCount => SIZE;

      /// <summary>
      /// Returns <c>4</c>.
      /// </summary>
      /// <value><c>4</c>.</value>
      public Int32 MaxCharByteCount => SIZE;
      /// <summary>
      /// Reads one byte as ASCII byte.
      /// </summary>
      /// <param name="array">The byte array.</param>
      /// <param name="idx">The index in <paramref name="array"/> to read ASCII byte from. This will be advanced by <c>4</c>.</param>
      /// <returns>The read ASCII byte.</returns>
      public Byte ReadASCIIByte( Byte[] array, ref Int32 idx )
      {
         var retVal = this.IsBigEndian ? array[idx + 3] : array[idx];
         idx += SIZE;
         return retVal;
      }

      /// <summary>
      /// Writes one ASCII byte to given array, and clears other bytes, depending on endianness.
      /// </summary>
      /// <param name="array">The byte array.</param>
      /// <param name="idx">The index in <paramref name="array"/> to write character data to. This will be advanced by <c>4</c>.</param>
      /// <param name="asciiByte">The ASCII byte to write.</param>
      /// <returns>This <see cref="UTF32EncodingInfo"/>.</returns>
      public IEncodingInfo WriteASCIIByte( Byte[] array, ref Int32 idx, Byte asciiByte )
      {
         var isBE = this.IsBigEndian;
         array[isBE ? ( idx + 3 ) : idx] = asciiByte;
         array.Clear( isBE ? idx : idx + 1, 3 );
         return this;
      }
   }

#endif

   public static partial class UtilPackExtensions
   {
      /// <summary>
      /// Creates the default encoding information for given <see cref="Encoding"/>.
      /// </summary>
      /// <param name="encoding">This <see cref="Encoding"/>.</param>
      /// <returns>A new instance of <see cref="IEncodingInfo"/>.</returns>
      /// <exception cref="InvalidOperationException">If this method could not automatically deduct which <see cref="IEncodingInfo"/> to create.</exception>
      public static IEncodingInfo CreateDefaultEncodingInfo( this Encoding encoding )
      {
         ArgumentValidator.ValidateNotNullReference( encoding );

         switch ( encoding )
         {
            case UTF8Encoding utf8:
               return new UTF8EncodingInfo( utf8 );
            case UnicodeEncoding utf16:
               if ( ReferenceEquals( utf16, Encoding.Unicode ) )
               {
                  return new UTF16LEEncodingInfo( utf16 );
               }
               else if ( ReferenceEquals( utf16, Encoding.BigEndianUnicode ) || UTF16EncodingInfo.IsBigEndian( utf16 ) )
               {
                  return new UTF16BEEncodingInfo( utf16 );
               }
               else
               {
                  return new UTF16LEEncodingInfo( utf16 );
               }
#if NET_40 || NETSTANDARD1_5 || NET_45
            case UTF32Encoding utf32:
               return new UTF32EncodingInfo( utf32 );
#endif
            case null:
               return null;
            default:
               throw new InvalidOperationException( $"Could not create default encoding info for {encoding}." );
         }
      }

      /// <summary>
      /// Helper method to count how many times the given string appears in this string.
      /// </summary>
      /// <param name="str">This <see cref="String"/>.</param>
      /// <param name="substring">The string to search. May be <c>null</c>, in which case result is <c>0</c>.</param>
      /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
      /// <returns>The amount of times <paramref name="substring"/> appears in this <see cref="String"/>.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="String"/> is <c>null</c>.</exception>
      public static Int32 CountOccurrances( this String str, String substring, StringComparison comparison )
      {
         ArgumentValidator.ValidateNotNullReference( str );

         var count = 0;

         if ( !String.IsNullOrEmpty( substring ) )
         {
            var idx = 0;
            while ( ( idx = str.IndexOf( substring, idx, comparison ) ) != -1 )
            {
               idx += substring.Length;
               ++count;
            }
         }

         return count;
      }
   }

}

public static partial class E_UtilPack
{

   /// <summary>
   /// Parses ASCII integer string to 32-bit integer from encoded string without allocating a string.
   /// Negative numbers are supported, as is optional <c>+</c> prefix.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array containing encoded string.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start reading from.</param>
   /// <param name="charCount">The optional character count. If this parameter is specified, then this method will only read specified maximum amount of characters. Furthermore, it may be specified that number takes *exactly* the given amount of characters.</param>
   /// <returns>The parsed integer.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If <paramref name="charCount"/> is specified, but the count was invalid.</exception>
   /// <exception cref="FormatException">If number string was malformed, or <paramref name="charCount"/> was specified with its exact match as <c>true</c> and the amount of characters read was less than specified.</exception>
   public static Int32 ParseInt32Textual(
      this IEncodingInfo encoding,
      Byte[] array,
      ref Int32 offset,
      (Int32 CharCount, Boolean CharCountExactMatch)? charCount = null
      )
   {
      if ( !encoding.TryParseInt32Textual( array, ref offset, charCount.HasValue ? charCount.Value.CharCount : -1, out Int32 retVal, out String errorString, out Int32 max ) )
      {
         throw new FormatException( errorString );
      }
      else if ( offset != max && charCount.HasValue )
      {
         if ( charCount.Value.CharCountExactMatch )
         {
            throw new FormatException( "Number ended prematurely." );
         }
         else
         {
            // 'Reverse back'
            offset -= encoding.BytesPerASCIICharacter;
         }
      }
      return retVal;
   }

   /// <summary>
   /// Parses ASCII integer string to 64-bit integer from encoded string without allocating a string.
   /// Negative numbers are supported, as is optional <c>+</c> prefix.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array containing encoded string.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start reading from.</param>
   /// <param name="charCount">The optional character count. If this parameter is specified, then this method will only read specified maximum amount of characters. Furthermore, it may be specified that number takes *exactly* the given amount of characters.</param>
   /// <returns>The parsed integer.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If <paramref name="charCount"/> is specified, but the count was invalid.</exception>
   /// <exception cref="FormatException">If number string was malformed, or <paramref name="charCount"/> was specified with its exact match as <c>true</c> and the amount of characters read was less than specified.</exception>
   public static Int64 ParseInt64Textual(
      this IEncodingInfo encoding,
      Byte[] array,
      ref Int32 offset,
      (Int32 CharCount, Boolean CharCountExactMatch)? charCount = null
      )
   {
      if ( !encoding.TryParseInt64Textual( array, ref offset, charCount.HasValue ? charCount.Value.CharCount : -1, out Int64 retVal, out String errorString, out Int32 max ) )
      {
         throw new FormatException( errorString );
      }
      else if ( offset != max && charCount.HasValue )
      {
         if ( charCount.Value.CharCountExactMatch )
         {
            throw new FormatException( "Number ended prematurely." );
         }
         else
         {
            // 'Reverse back'
            offset -= encoding.BytesPerASCIICharacter;
         }
      }
      return retVal;
   }

   /// <summary>
   /// Tries to parse ASCII integer string to 32-bit integer from encoded string without allocating a string.
   /// Negative numbers are supported, as is optional <c>+</c> prefix.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array containing encoded string.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start reading from.</param>
   /// <param name="charCount">The optional character count. Negative values mean that no character count is specified.</param>
   /// <param name="result">This will contain parsed integer.</param>
   /// <param name="errorString">This will contain error string, if format error is encountered.</param>
   /// <param name="max">The offset in array which was reached when integer ended.</param>
   /// <returns>Whether parsing was successful.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If <paramref name="charCount"/> is specified, but the count was invalid.</exception>
   public static Boolean TryParseInt32Textual(
      this IEncodingInfo encoding,
      Byte[] array,
      ref Int32 offset,
      Int32 charCount,
      out Int32 result,
      out String errorString,
      out Int32 max
      )
   {
      encoding.PrepareIntegerParseTextual( array, ref offset, charCount, encoding.BytesPerASCIICharacter, out max, out Boolean isNegative, out errorString );
      result = 0;
      if ( errorString == null )
      {
         while ( offset < max && errorString == null )
         {
            var prevOffset = offset;
            var b = encoding.ReadASCIIByte( array, ref offset );
            if ( b >= 0x30 && b <= 0x39 ) // '0' and '9'
            {
               result = 10 * result + ( b - 0x30 );
            }
            else if ( charCount < 0 )
            {
               // Char count was not specified, so this is graceful end
               offset = prevOffset;
               max = offset;
            }
            else
            {
               errorString = "Invalid byte at " + offset + ": " + b + ".";
            }
         }

         if ( isNegative )
         {
            result = -result;
         }
      }
      return errorString == null;
   }

   /// <summary>
   /// Tries to parse ASCII integer string to 64-bit integer from encoded string without allocating a string.
   /// Negative numbers are supported, as is optional <c>+</c> prefix.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array containing encoded string.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start reading from.</param>
   /// <param name="charCount">The optional character count. Negative values mean that no character count is specified.</param>
   /// <param name="result">This will contain parsed integer.</param>
   /// <param name="errorString">This will contain error string, if format error is encountered.</param>
   /// <param name="max">The offset in array which was reached when integer ended.</param>
   /// <returns>Whether parsing was successful.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If <paramref name="charCount"/> is specified, but the count was invalid.</exception>
   public static Boolean TryParseInt64Textual(
      this IEncodingInfo encoding,
      Byte[] array,
      ref Int32 offset,
      Int32 charCount,
      out Int64 result,
      out String errorString,
      out Int32 max
      )
   {
      encoding.PrepareIntegerParseTextual( array, ref offset, charCount, encoding.BytesPerASCIICharacter, out max, out Boolean isNegative, out errorString );
      result = 0L;
      if ( errorString == null )
      {
         while ( offset < max && errorString == null )
         {
            var prevOffset = offset;
            var b = encoding.ReadASCIIByte( array, ref offset );
            if ( b >= 0x30 && b <= 0x39 ) // '0' and '9'
            {
               result = 10 * result + ( b - 0x30 );
            }
            else if ( charCount < 0 )
            {
               // Char count was not specified, so this is graceful end
               offset = prevOffset;
               max = offset;
            }
            else
            {
               errorString = "Invalid byte at " + offset + ": " + b + ".";
            }
         }

         if ( isNegative )
         {
            result = -result;
         }
      }

      return errorString == null;
   }

   private static void PrepareIntegerParseTextual(
      this IEncodingInfo encoding,
      Byte[] array,
      ref Int32 offset,
      Int32 charCount,
      Int32 increment,
      out Int32 max,
      out Boolean isNegative,
      out String errorString
      )
   {
      ArgumentValidator.ValidateNotNullReference( encoding );
      ArgumentValidator.ValidateNotNull( nameof( array ), array );

      Int32 count;
      if ( charCount >= 0 )
      {
         count = charCount * increment;
         max = offset + count;
         array.CheckArrayArguments( offset, count );
      }
      else
      {
         count = -1;
         max = array.Length;
      }

      var needToCheckAgain = false;
      isNegative = false;
      var oldOffset = offset;
      var firstChar = encoding.ReadASCIIByte( array, ref offset );
      if ( firstChar == '+' ) // '+'
      {
         needToCheckAgain = true;
      }
      else if ( firstChar == '-' ) // '-'
      {
         isNegative = true;
         needToCheckAgain = true;
      }
      else
      {
         offset = oldOffset;
      }

      errorString = null;
      if ( needToCheckAgain && count >= 0 )
      {
         array.CheckArrayArguments( offset, count - 1 );
         if ( offset >= max )
         {
            errorString = "No characters left for digits.";
         }
      }
   }

   /// <summary>
   /// Writes ASCII representation of given integer in decimal format to byte array using this encoding, and without allocating a string object.
   /// </summary>
   /// <param name="encoding">The <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array to write encoded number string to.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start writing.</param>
   /// <param name="value">The integer to write textual representation of.</param>
   /// <param name="fixedLength">Optional fixed length specification, in characters.</param>
   /// <returns>The <paramref name="encoding"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   public static IEncodingInfo WriteIntegerTextual( this IEncodingInfo encoding, Byte[] array, ref Int32 offset, Int64 value, Int32 fixedLength = -1 )
   {
      if ( value < 0 )
      {
         encoding.WriteASCIIByte( array, ref offset, 0x2D ); // '-'
         value = Math.Abs( value );
      }
      else
      {
         ArgumentValidator.ValidateNotNullReference( encoding );
         ArgumentValidator.ValidateNotNull( nameof( array ), array );
      }

      // Iterate from end towards beginning
      var increment = encoding.BytesPerASCIICharacter;
      var end = offset + ( fixedLength >= 0 ? ( fixedLength * increment ) : encoding.GetTextualIntegerRepresentationSize( value ) );
      for ( var i = end - increment; i >= offset; i -= increment )
      {
         var dummy = i;
         encoding.WriteASCIIByte( array, ref dummy, (Byte) ( ( value % 10 ) + 0x30 ) );
         value /= 10;
      }
      offset = end;

      return encoding;
   }


   /// <summary>
   /// Calculates how many bytes the textual representation of given integer will take if encoded as string using this encoding.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="integer">The integer to calculate size of.</param>
   /// <param name="numberBase">The number base that will be used when writing integer. <c>10</c> by default.</param>
   /// <returns>The amount of bytes the textual representation of given integer wil ltake if encoded as string using this encoding.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   public static Int32 GetTextualIntegerRepresentationSize( this IEncodingInfo encoding, Int64 integer, Int32 numberBase = 10 )
   {
      ArgumentValidator.ValidateNotNullReference( encoding );

      var size = 1;
      if ( integer < 0 )
      {
         ++size; // minus sign
         integer = Math.Abs( integer );
      }

      while ( ( integer /= numberBase ) >= 1 )
      {
         ++size;
      }
      return size * encoding.BytesPerASCIICharacter;
   }

   /// <summary>
   /// Calculates how many bytes the textual representation of given fraction integer part will take if encoded as string using this encoding.
   /// Cuts any trailing zeroes, so that result for <c>500</c> will be <c>1</c> (encoded as just <c>5</c>).
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="decimalInteger">The fraction part, as <see cref="Int64"/>. Negative values are treated as positive values.</param>
   /// <param name="maxFractionDigitCount">The maximum amount of fraction digits.</param>
   /// <param name="numberBase">The number base that will be used when writing the fraction digits. <c>10</c> by default.</param>
   /// <returns>The amount of bytes the textual representation of given fraction part will take if encoded as string using this encoding.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   public static Int32 GetTextualFractionIntegerSize( this IEncodingInfo encoding, Int64 decimalInteger, Int32 maxFractionDigitCount, Int32 numberBase = 10 )
   {
      ArgumentValidator.ValidateNotNullReference( encoding );

      if ( decimalInteger < 0 )
      {
         decimalInteger = Math.Abs( decimalInteger );
      }

      Int32 size;
      if ( decimalInteger > 0 )
      {
         // No more Math.DivRem ;_;
         var trailingZeroCount = 0;
         while ( decimalInteger % numberBase == 0 )
         {
            ++trailingZeroCount;
            decimalInteger /= numberBase;
         }
         size = maxFractionDigitCount - trailingZeroCount;
      }
      else
      {
         size = 0;
      }

      return size * encoding.BytesPerASCIICharacter;
   }

   /// <summary>
   /// Writes ASCII representation of given fraction integer to byte array using this encoding, and without allocating a string object.
   /// Cuts any trailing zeroes, so that <c>500</c> will become <c>5</c>.
   /// </summary>
   /// <param name="encoding">The <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array to write encoded number string to.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start writing.</param>
   /// <param name="decimalInteger">The decimal part, as <see cref="Int64"/>. Negative values are treated as positive values.</param>
   /// <param name="maxFractionDigitCount">The maximum amount of fraction digits.</param>
   /// <returns>The <paramref name="encoding"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   public static IEncodingInfo WriteFractionIntegerTextual( this IEncodingInfo encoding, Byte[] array, ref Int32 offset, Int64 decimalInteger, Int32 maxFractionDigitCount )
   {
      ArgumentValidator.ValidateNotNullReference( encoding );

      if ( decimalInteger != 0 )
      {
         if ( decimalInteger < 0 )
         {
            decimalInteger = Math.Abs( decimalInteger );
         }

         while ( decimalInteger % 10 == 0 )
         {
            decimalInteger /= 10;
            --maxFractionDigitCount;
         }

         // Integer has now its trailing zeroes cut, now just write it
         encoding.WriteIntegerTextual( array, ref offset, decimalInteger, maxFractionDigitCount );
      }
      return encoding;
   }

   /// <summary>
   /// Writes all the characters in given string to given byte array.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array to write ASCII string to.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start writing.</param>
   /// <param name="str">The string to write to. If <c>null</c>, nothing will be written.</param>
   /// <returns>This <see cref="IEncodingInfo"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static IEncodingInfo WriteString( this IEncodingInfo encoding, Byte[] array, ref Int32 offset, String str )
   {
      ArgumentValidator.ValidateNotNullReference( encoding );

      if ( str != null )
      {
         offset += encoding.Encoding.GetBytes( str, 0, str.Length, array, offset );
      }
      return encoding;
   }

   /// <summary>
   /// Writes given byte value as two hexadecimal textual characters to given array without allocating string object.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array to write hexadecimal textual characters to.</param>
   /// <param name="idx">The offset in <paramref name="array"/> where to start writing.</param>
   /// <param name="value">The value to write.</param>
   /// <param name="upperCase">Optional parameter specifying whether alpha characters should be upper-case.</param>
   /// <returns>This <see cref="IEncodingInfo"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static IEncodingInfo WriteHexDecimal( this IEncodingInfo encoding, Byte[] array, ref Int32 idx, Byte value, Boolean upperCase = false )
   {
      Byte ExtractHexChar( Int32 bits, Int32 alphaVal )
      {
         return (Byte) ( bits + ( bits >= 0xA ? alphaVal : 0x30 ) );
      }

      var min = upperCase ? 0x37 : 0x57;
      return encoding
         .WriteASCIIByte( array, ref idx, ExtractHexChar( ( value & 0xF0 ) >> 4, min ) ) // High bits
         .WriteASCIIByte( array, ref idx, ExtractHexChar( value & 0x0F, min ) );   // Low bits
   }

   /// <summary>
   /// Reads the next two ASCII characters as hexadecimal characters and parses the value the characters represent, without allocating string object.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array to read hexadecimal textual charactesr from.</param>
   /// <param name="idx">The offset in <paramref name="array"/> where to start reading.</param>
   /// <returns>The decoded hexadecimal value.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
#if !NET40
   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
#endif
   public static Byte ReadHexDecimal( this IEncodingInfo encoding, Byte[] array, ref Int32 idx )
   {
      return (Byte) ( ( ( (Char) encoding.ReadASCIIByte( array, ref idx ) ).GetHexadecimalValue().GetValueOrDefault() << 4 )
      | ( ( (Char) encoding.ReadASCIIByte( array, ref idx ) ).GetHexadecimalValue().GetValueOrDefault() ) );
   }


   /// <summary>
   /// Tries to parse integer as from a character reader.
   /// </summary>
   /// <param name="reader">This <see cref="PeekablePotentiallyAsyncReader{TValue}"/>.</param>
   /// <returns>Task which either returns parsed integer value, or <c>null</c> if parsing was unsuccessful.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="PeekablePotentiallyAsyncReader{TValue}"/> is <c>null</c>.</exception>
   public static async ValueTask<Int64?> TryParseInt64TextualGenericAsync( this PeekablePotentiallyAsyncReader<Char?> reader )
   {
      var readValue = await ArgumentValidator.ValidateNotNullReference( reader ).TryPeekAsync();
      var isNegative = readValue.HasValue && readValue.Value == '-';
      if ( isNegative || ( readValue.HasValue && readValue.Value == '+' ) )
      {
         // Consume peeked value and read next
         await reader.TryReadNextAsync();
         readValue = await reader.TryPeekAsync();
      }

      Int64? result;
      if ( readValue.HasValue && readValue >= '0' && readValue <= '9' )
      {
         result = 0;
         do
         {
            // Update result
            result = 10 * result + ( readValue - '0' );
            // Consume prev character
            await reader.TryReadNextAsync();
            // Peek next character
            readValue = await reader.TryPeekAsync();
         } while ( readValue.HasValue && readValue >= '0' && readValue <= '9' );

         if ( isNegative )
         {
            result = -result;
         }
      }
      else
      {
         result = null;
      }

      return result;
   }

   /// <summary>
   /// Finds the index at correct byte boundary of the next ASCII character, or returns the maximum index (exclusive) if the next character is not found..
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array containing characters encoded with this <see cref="IEncodingInfo"/>.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start searching.</param>
   /// <param name="count">The maximum amount of bytes to search.</param>
   /// <param name="asciiCharacter">The ASCII character to search.</param>
   /// <returns>The index at correct byte boundary of the next ASCII character, or maximum index (<paramref name="offset"/> + <paramref name="count"/>) if next character is not found.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and/or <paramref name="count"/> are invalid.</exception>
   public static Int32 IndexOfASCIICharacterOrMax( this IEncodingInfo encoding, Byte[] array, Int32 offset, Int32 count, Byte asciiCharacter )
   {
      var retVal = encoding.IndexOfASCIICharacter( array, offset, count, asciiCharacter );
      return retVal < 0 ? ( offset + count ) : retVal;
   }

   /// <summary>
   /// Finds the index at correct byte boundary of the next ASCII character.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="array">The byte array containing characters encoded with this <see cref="IEncodingInfo"/>.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start searching.</param>
   /// <param name="count">The maximum amount of bytes to search.</param>
   /// <param name="asciiCharacter">The ASCII character to search.</param>
   /// <returns>The index at correct byte boundary of the next ASCII character, or <c>-1</c>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> and/or <paramref name="count"/> are invalid.</exception>
   public static Int32 IndexOfASCIICharacter( this IEncodingInfo encoding, Byte[] array, Int32 offset, Int32 count, Byte asciiCharacter )
   {
      ArgumentValidator.ValidateNotNullReference( encoding );

      var max = offset + count;
      var endIdx = Array.IndexOf( array, asciiCharacter, offset, max - offset );

      Int32 min;
      if (
         endIdx > 0
         && endIdx < max
         && ( min = encoding.MinCharByteCount ) > 1
         )
      {
         // Have to detect actual end of string... it depends on endianness of encoding
         var tmp = endIdx;
         if ( endIdx == max - 1
            || ( endIdx > offset + min && encoding.ReadASCIIByte( array, ref tmp ) != asciiCharacter )
            )
         {
            // Must be big-endian
            endIdx -= encoding.MinCharByteCount - 1;
         }
      }

      return endIdx;
   }
}