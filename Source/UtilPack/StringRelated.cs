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
   }

   /// <summary>
   /// This class implements <see cref="IEncodingInfo"/> for <see cref="UTF8Encoding"/>.
   /// </summary>
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
         this.BytesPerASCIICharacter = 1;
         this.MinCharByteCount = 1;
      }

      /// <inheritdoc />
      public Encoding Encoding { get; }

      /// <summary>
      /// Returns <c>1</c>.
      /// </summary>
      /// <value><c>1</c>.</value>
      public Int32 BytesPerASCIICharacter { get; }

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
      public Int32 MinCharByteCount { get; }
   }

#if NETSTANDARD1_0

   /// <summary>
   /// This class provides functionality to read characters from <see cref="StreamReaderWithResizableBuffer"/>.
   /// </summary>
   public sealed class CharacterReader
   {
      private const Int32 IDLE = 0;
      private const Int32 BUSY = 1;

      private readonly Char[] _chars;
      private readonly Int32 _minChar;
      private Int32 _state;

      /// <summary>
      /// Creates new instance of <see cref="CharacterReader"/> with given <see cref="IEncodingInfo"/>.
      /// </summary>
      /// <param name="encodingInfo">The <see cref="IEncodingInfo"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="encodingInfo"/> is <c>null</c>.</exception>
      public CharacterReader(
         IEncodingInfo encodingInfo
         )
      {
         this.Encoding = ArgumentValidator.ValidateNotNull( nameof( encodingInfo ), encodingInfo );
         this._minChar = encodingInfo.MinCharByteCount;
         this._chars = new Char[2];
      }

      /// <summary>
      /// Gets the <see cref="IEncodingInfo"/> of this <see cref="CharacterReader"/>.
      /// </summary>
      /// <value>The <see cref="IEncodingInfo"/> of this <see cref="CharacterReader"/>.</value>
      public IEncodingInfo Encoding { get; }

      /// <summary>
      /// Tries to read next character from given <paramref name="stream"/>.
      /// </summary>
      /// <param name="stream">The <see cref="StreamReaderWithResizableBuffer"/> to read character from. The <see cref="StreamReaderWithResizableBuffer.TryReadMoreAsync(int)"/> method will be used.</param>
      /// <param name="charChecker">Optional callback to check character. If it is supplied, this method will keep reading characters until this callback returns <c>true</c>.</param>
      /// <returns>A task which will return character read, or <c>null</c> if no more characters could be read from <paramref name="stream"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c>.</exception>
      /// <exception cref="InvalidOperationException">If this reader is currently busy with another read operation.</exception>
      public async ValueTask<Char?> TryReadNextCharacterAsync(
         StreamReaderWithResizableBuffer stream,
         Func<Char, Boolean> charChecker = null // If false -> will read next character
      )
      {
         ArgumentValidator.ValidateNotNull( nameof( stream ), stream );
         if ( Interlocked.CompareExchange( ref this._state, BUSY, IDLE ) == IDLE )
         {
            try
            {
               Boolean charReadSuccessful;
               var encoding = this.Encoding.Encoding;
               var auxArray = this._chars;
               var minChar = this._minChar;
               do
               {

                  var arrayIndex = stream.ReadBytesCount;
                  charReadSuccessful = await stream.TryReadMoreAsync( minChar );
                  if ( charReadSuccessful )
                  {
                     var charCount = 1;
                     while ( charCount == 1 && await stream.TryReadMoreAsync( minChar ) )
                     {
                        charCount = encoding.GetCharCount( stream.Buffer, arrayIndex, stream.ReadBytesCount - arrayIndex );
                     }

                     if ( charCount > 1 )
                     {
                        // Unread peeked byte
                        stream.UnreadBytes( minChar );
                     }

                     encoding.GetChars( stream.Buffer, arrayIndex, stream.ReadBytesCount - arrayIndex, auxArray, 0 );
                  }

               } while ( charReadSuccessful && !( charChecker?.Invoke( auxArray[0] ) ?? true ) );

               return charReadSuccessful ? auxArray[0] : (Char?) null;
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

      /// <summary>
      /// Gets the amount of bytes taken by given character, without allocating new character array.
      /// </summary>
      /// <param name="c">The character to check.</param>
      /// <returns>The amount of bytes the given character takes using the encoding of this reader.</returns>
      /// <exception cref="InvalidOperationException">If this reader is currently busy with another read operation.</exception>
      public Int32 GetByteCount( Char c )
      {
         if ( Interlocked.CompareExchange( ref this._state, BUSY, IDLE ) == IDLE )
         {
            try
            {
               this._chars[0] = c;
               return this.Encoding.Encoding.GetByteCount( this._chars, 0, 1 );
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
      
      /// <summary>
      /// Writes byte representation of a single character to given array starting at given index.
      /// </summary>
      /// <param name="singleChar">The character to encode.</param>
      /// <param name="array">The byte array to write to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing. Will be incremented by the number of bytes written.</param>
      /// <exception cref="InvalidOperationException">If this reader is currently busy with another read operation.</exception>
      public void GetBytes( Char singleChar, Byte[] array, ref Int32 offset )
      {
         if ( Interlocked.CompareExchange( ref this._state, BUSY, IDLE ) == IDLE )
         {
            try
            {
               this._chars[0] = singleChar;
               offset += this.Encoding.Encoding.GetBytes( this._chars, 0, 1, array, offset );
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
      
      /// <summary>
      /// Writes byte representation of a surrage pair to given array starting at given index.
      /// </summary>
      /// <param name="firstSurrogate">The first surrogate character.</param>
      /// <param name="secondSurrogate">The second surrogate character.</param>
      /// <param name="array">The byte array to write to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing. Will be incremented by the number of bytes written.</param>
      /// <exception cref="InvalidOperationException">If this reader is currently busy with another read operation.</exception>
      public void GetBytes( Char firstSurrogate, Char secondSurrogate, Byte[] array, ref Int32 offset )
      {
         if ( Interlocked.CompareExchange( ref this._state, BUSY, IDLE ) == IDLE )
         {
            try
            {
               this._chars[0] = firstSurrogate;
               this._chars[1] = secondSurrogate;
               offset += this.Encoding.Encoding.GetBytes( this._chars, 0, 2, array, offset );
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

#endif

}

public static partial class E_UtilPack
{
#if NETSTANDARD1_0
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
#endif

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
   public static Byte ReadHexDecimal( this IEncodingInfo encoding, Byte[] array, ref Int32 idx )
   {
      Int32 ExtractHexValue( Int32 asciiChar )
      {
         if ( asciiChar <= '9' )
         {
            // Assume '0'-'9'
            return asciiChar - '0';
         }
         else if ( asciiChar <= 'F' )
         {
            // Assume 'A'-'F'
            return asciiChar - '7';
         }
         else
         {
            // Assume 'a'-'f'
            return asciiChar - 'W';
         }
      }

      return (Byte) (( ExtractHexValue( encoding.ReadASCIIByte( array, ref idx ) ) << 4 ) |
             ExtractHexValue( encoding.ReadASCIIByte( array, ref idx ) ) );
   }


#if NETSTANDARD1_0

   /// <summary>
   /// Helper method to try to read next character from <see cref="StreamReaderWithResizableBuffer"/>, or throw if no more characters can be read.
   /// </summary>
   /// <param name="reader">This <see cref="CharacterReader"/>.</param>
   /// <param name="stream">The <see cref="StreamReaderWithResizableBuffer"/> to read from.</param>
   /// <param name="charChecker">Optional callback to check character. If it is supplied, this method will keep reading characters until this callback returns <c>true</c>.</param>
   /// <returns>A task which will return character read.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="CharacterReader"/> is <c>null</c>.</exception>
   /// <exception cref="EndOfStreamException">If no more characters could be read from the stream.</exception>
   public static async ValueTask<Char> ReadNextCharacterAsync(
      this CharacterReader reader,
      StreamReaderWithResizableBuffer stream,
      Func<Char, Boolean> charChecker = null // If false -> will read next character
   )
   {
      return await ArgumentValidator.ValidateNotNullReference( reader ).TryReadNextCharacterAsync( stream, charChecker ) ?? throw new EndOfStreamException();
   }

#endif

}