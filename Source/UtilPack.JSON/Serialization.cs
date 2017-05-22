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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

/// <summary>
/// This class contains extension methods for types in UtilPack.
/// </summary>
public static partial class E_UtilPack
{
   // TODO make it customizable whether numbers are Doubles or Decimals...
   // TODO add support for DateTime & other stuff that is in Newtonsoft.JSON ?

   private const Char STR_START = '"';
   private const Char STR_END = '"';
   private const Char STR_ESCAPE_PREFIX = '\\';
   private const Char STR_FORWARD_SLASH = '/';

   private const Char ARRAY_START = '[';
   private const Char ARRAY_VALUE_DELIM = ',';
   private const Char ARRAY_END = ']';

   private const Char OBJ_START = '{';
   private const Char OBJ_VALUE_DELIM = ',';
   private const Char OBJ_END = '}';
   private const Char OBJ_KEY_VALUE_DELIM = ':';

   private const Byte NUMBER_DECIMAL = (Byte) '.';
   private const Byte NUMBER_EXP_LOW = (Byte) 'e';
   private const Byte NUMBER_EXP_UPPER = (Byte) 'E';

   /// <summary>
   /// Asynchronously reads the JSON value (array, object, or primitive value) from this <see cref="MemorizingPotentiallyAsyncReader{TValue, TBufferItem}"/>.
   /// Tries to keep the buffer of this stream as little as possible, and allocating as little as possible any other extra objects than created JSON objects (currently parsing a <see cref="Double"/> needs to allocate string).
   /// </summary>
   /// <param name="reader">This <see cref="MemorizingPotentiallyAsyncReader{TValue, TBufferItem}"/>.</param>
   /// <returns>A task which will contain deserialized <see cref="JToken"/> on its completion.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="StreamReaderWithResizableBuffer"/> is <c>null</c>.</exception>
   public static ValueTask<JToken> ReadJSONTTokenAsync(
      this MemorizingPotentiallyAsyncReader<Char?, Char> reader
      )
   {
      return PerformReadJSONTTokenAsync(
         ArgumentValidator.ValidateNotNullReference( reader )
         );
   }

   private static async ValueTask<JToken> PerformReadJSONTTokenAsync(
      MemorizingPotentiallyAsyncReader<Char?, Char> reader
      )
   {
      reader.ClearBuffer();
      // Read first non-whitespace character
      var charRead = await reader.PeekUntilAsync( c => !Char.IsWhiteSpace( c ) );
      var prevIdx = reader.BufferCount;

      // We know what kind of JToken we will have based on a single character
      JToken retVal;
      Boolean encounteredContainerEnd;
      switch ( charRead )
      {
         case ARRAY_END:
         case OBJ_END:
            // This happens only when reading empty array/object, and this is called recursively.
            await reader.TryReadNextAsync(); // Consume peeked character
            retVal = null;
            break;
         case ARRAY_START:
            await reader.TryReadNextAsync(); // Consume peeked character
            var array = new JArray();
            encounteredContainerEnd = false;
            // Reuse 'retVal' variable since we really need it only at the end of this case block.
            while ( !encounteredContainerEnd && ( retVal = await PerformReadJSONTTokenAsync( reader ) ) != null )
            {
               array.Add( retVal );
               // Read next non-whitespace character - it will be either array value delimiter (',') or array end (']')
               charRead = await reader.ReadUntilAsync( c => !Char.IsWhiteSpace( c ) );
               encounteredContainerEnd = charRead == ARRAY_END;
            }
            retVal = array;
            break;
         case OBJ_START:
            await reader.TryReadNextAsync(); // Consume peeked character
            var obj = new JObject();
            encounteredContainerEnd = false;
            String keyStr;
            // Reuse 'retVal' variable since we really need it only at the end of this case block.
            while ( !encounteredContainerEnd && ( keyStr = await ReadJSONStringAsync( reader, false ) ) != null )
            {
               // First JToken should be string being the key
               // Skip whitespace and ':'
               charRead = await reader.PeekUntilAsync( c => !Char.IsWhiteSpace( c ) && c != OBJ_KEY_VALUE_DELIM );
               // Read another JToken, this one will be our value
               retVal = await PerformReadJSONTTokenAsync( reader );
               obj.Add( keyStr, retVal );
               // Read next non-whitespace character - it will be either object value delimiter (','), or object end ('}')
               charRead = await reader.ReadUntilAsync( c => !Char.IsWhiteSpace( c ) );
               encounteredContainerEnd = charRead == OBJ_END;
            }
            retVal = obj;
            break;
         case STR_START:
            await reader.TryReadNextAsync(); // Consume peeked character
            retVal = new JValue( await ReadJSONStringAsync( reader, true ) );
            break;
         case 't':
            await reader.TryReadNextAsync(); // Consume peeked character
            // Boolean true
            // read 'r'
            Validate( await reader.ReadNextAsync(), 'r' );
            // read 'u'
            Validate( await reader.ReadNextAsync(), 'u' );
            // read 'e'
            Validate( await reader.ReadNextAsync(), 'e' );
            retVal = new JValue( true );
            break;
         case 'f':
            await reader.TryReadNextAsync(); // Consume peeked character
            //Boolean false
            // read 'a'
            Validate( await reader.ReadNextAsync(), 'a' );
            // read 'l'
            Validate( await reader.ReadNextAsync(), 'l' );
            // read 's'
            Validate( await reader.ReadNextAsync(), 's' );
            // read 'e'
            Validate( await reader.ReadNextAsync(), 'e' );
            retVal = new JValue( false );
            break;
         case 'n':
            await reader.TryReadNextAsync(); // Consume peeked character
            // null
            // read 'u'
            Validate( await reader.ReadNextAsync(), 'u' );
            // read 'l'
            Validate( await reader.ReadNextAsync(), 'l' );
            // read 'l'
            Validate( await reader.ReadNextAsync(), 'l' );
            retVal = JValue.CreateNull();
            break;
         default:
            // The only possibility is number - or malformed JSON string
            var possibleInteger = await reader.TryParseInt64TextualGenericAsync();
            if ( possibleInteger.HasValue )
            {
               // Check if next character is '.' or 'e' or 'E'
               var nextChar = await reader.TryPeekAsync();
               if ( nextChar.HasValue && ( nextChar == NUMBER_DECIMAL || nextChar == NUMBER_EXP_LOW || nextChar == NUMBER_EXP_UPPER ) )
               {
                  // This is not a integer, but is a number -> read until first non-number-character
                  await reader.ReadNextAsync();
                  await reader.PeekUntilAsync( c => !( (c >= '0' && c <= '9') || c == NUMBER_EXP_LOW || c == NUMBER_EXP_UPPER || c == '-' || c == '+' ) );
                  // TODO maybe use Decimal always for non-integers?
                  retVal = new JValue( Double.Parse( new String( reader.Buffer, prevIdx, reader.BufferCount - prevIdx ), System.Globalization.CultureInfo.InvariantCulture.NumberFormat ) );
               }
               else
               {
                  // This is integer
                  retVal = new JValue( possibleInteger.Value );
               }
            }
            else
            {
               // Not a number at all
               retVal = null;
            }
            break;
      }

      return retVal;
   }

   private static async ValueTask<String> ReadJSONStringAsync(
      MemorizingPotentiallyAsyncReader<Char?, Char> reader,
      Boolean startQuoteRead
      )
   {
      Char charRead;
      var proceed = startQuoteRead;
      if ( !proceed )
      {
         charRead = await reader.ReadUntilAsync( c => !Char.IsWhiteSpace( c ) );
         proceed = charRead == STR_START;
      }

      String str;
      if ( proceed )
      {
         reader.ClearBuffer();
         // At this point, we have read the starting quote, now read the contents.

         async ValueTask<Char> DecodeUnicodeEscape()
         {
            Char decoded;
            var decodeStartIdx = reader.BufferCount;
            if ( await reader.TryReadMore( 4 ) == 4 )
            {
               var array = reader.Buffer;
               decoded = (Char)((array[decodeStartIdx].GetHexadecimalValue().GetValueOrDefault() << 12) | (array[decodeStartIdx + 1].GetHexadecimalValue().GetValueOrDefault() << 8) | (array[decodeStartIdx + 2].GetHexadecimalValue().GetValueOrDefault() << 4) | array[decodeStartIdx + 3].GetHexadecimalValue().GetValueOrDefault());
            }
            else
            {
               decoded = '\0';
            }
            return decoded;
         }

         // Read string, but mind the escapes
         // TODO maybe do reader.PeekUntilAsync( c => c == STR_END && reader.Buffer[reader.BufferCount - 2] != STR_ESCAPE ); 
         // And then do escaping in-place...?
         Int32 curIdx;
         do
         {
            curIdx = reader.BufferCount;
            charRead = (await reader.TryReadNextAsync()) ?? STR_END;
            if ( charRead == STR_ESCAPE_PREFIX )
            {
               // Escape handling - next character decides what we will do
               charRead = (await reader.TryReadNextAsync()) ?? STR_END;
               Char replacementByte = '\0';
               switch ( charRead )
               {
                  case STR_END:
                  case STR_ESCAPE_PREFIX:
                  case '/':
                     // Actual value is just just read char minus the '\'
                     replacementByte = charRead;
                     break;
                  case 'b':
                     // Backspace
                     replacementByte = '\b';
                     break;
                  case 'f':
                     // Form feed
                     replacementByte = '\f';
                     break;
                  case 'n':
                     // New line
                     replacementByte = '\n';
                     break;
                  case 'r':
                     // Carriage return
                     replacementByte = '\r';
                     break;
                  case 't':
                     // Horizontal tab
                     replacementByte = '\t';
                     break;
                  case 'u':
                     // Unicode sequence - followed by four hexadecimal digits
                     var decoded = await DecodeUnicodeEscape();
                     reader.Buffer[curIdx++] = decoded;
                     break;
                  default:
                     // Just let it slide
                     curIdx = reader.BufferCount;
                     break;
               }

               if ( replacementByte > 0 )
               {
                  // We just read ASCII char, which should be now replaced
                  reader.Buffer[curIdx++] = replacementByte;
               }

               // Erase anything extra
               reader.EraseBufferSegment( curIdx, reader.BufferCount - curIdx );

               // Always read next char
               charRead = (Char) 0;
            }
         } while ( charRead != STR_END );

         var strCharCount = reader.BufferCount - 1;
         if ( strCharCount <= 0 )
         {
            str = "";
         }
         else
         {
            str = new String( reader.Buffer, 0, strCharCount );
         }
      }
      else
      {
         str = null;
      }

      return str;
   }

   private static void Validate( Char readResult, Char expectedChar )
   {
      if ( readResult != expectedChar )
      {
         throw new FormatException( $"Error in JSON deserialization: expected '{expectedChar}', but got '{readResult}'." );
      }
   }

   /// <summary>
   /// Helper method to calculate how many bytes the given <see cref="JToken"/> will take using this encoding.
   /// </summary>
   /// <param name="encoding">This <see cref="IEncodingInfo"/>.</param>
   /// <param name="jsonValue">The <see cref="JToken"/>.</param>
   /// <returns>The amount of bytes the given <see cref="JToken"/> will take using this <see cref="IEncodingInfo"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IEncodingInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If given <paramref name="jsonValue"/>, or any of the other <see cref="JToken"/>s it contains, is <c>null</c>.</exception>
   /// <exception cref="NotSupportedException">When a <see cref="JToken"/> is encountered which is not <see cref="JArray"/>, <see cref="JObject"/> or <see cref="JValue"/>; or when the value of <see cref="JValue"/> is not recognized.</exception>
   public static Int32 CalculateJTokenTextSize( this IEncodingInfo encoding, JToken jsonValue )
   {
      return PerformCalculateJTokenTextSize( ArgumentValidator.ValidateNotNullReference( encoding ), jsonValue );
   }

   private static Int32 PerformCalculateJTokenTextSize( IEncodingInfo encoding, JToken jsonValue )
   {
      ArgumentValidator.ValidateNotNull( nameof( jsonValue ), jsonValue );

      var asciiSize = encoding.BytesPerASCIICharacter;
      Int32 retVal;
      switch ( jsonValue )
      {
         case JArray array:
            // '['followed by items with ',' in between them followed by ']'
            retVal = ( 2 + Math.Max( array.Count - 1, 0 ) ) * asciiSize + array.Aggregate( 0, ( cur, curToken ) => cur + PerformCalculateJTokenTextSize( encoding, curToken ) );
            break;
         case JObject obj:
            // '{' followed by items with ',' in between them followed by '}'
            // Each item has ':' between key and value
            retVal = ( 2 + Math.Max( obj.Count - 1, 0 ) ) * asciiSize + Enumerable.Aggregate<KeyValuePair<String, JToken>, Int32>( obj, 0, ( cur, curKvp ) => cur + asciiSize + CalculateJSONStringValueSize( curKvp.Key, encoding ) + PerformCalculateJTokenTextSize( encoding, curKvp.Value ) );
            break;
         case JValue simple:
            var val = simple.Value;
            switch ( val )
            {
               case String str:
                  retVal = CalculateJSONStringValueSize( str, encoding );
                  break;
               case Boolean bol:
                  retVal = ( bol ? 4 : 5 ) * asciiSize;
                  break;
               case Byte b:
               case SByte sb:
               case Int16 i16:
               case UInt16 u16:
               case Int32 i32:
               case UInt32 u32:
               case Int64 i64:
                  retVal = encoding.GetTextualIntegerRepresentationSize( (Int64) Convert.ChangeType( val, typeof( Int64 ) ) );
                  break;
               // TODO UInt64
               case Single s:
               case Double d:
               case Decimal dec:
                  retVal = encoding.Encoding.GetByteCount( ( (IFormattable) val ).ToString( null, System.Globalization.CultureInfo.InvariantCulture.NumberFormat ) );
                  break;
               case null:
                  // Word 'null' has 4 ascii characters
                  retVal = 4 * asciiSize;
                  break;
               default:
                  throw new NotSupportedException( $"Unsupported primitive value {val}." );
            }
            break;
         default:
            throw new NotSupportedException( $"Unrecognized JToken type: {jsonValue?.GetType()}." );
      }

      return retVal;
   }

   private static Int32 CalculateJSONStringValueSize( String str, IEncodingInfo encoding )
   {
      // '"' followed by string followed by '"', with '"', '\', '/', and control characters escaped
      var asciiSize = encoding.BytesPerASCIICharacter;
      var retVal = 2 * asciiSize + encoding.Encoding.GetByteCount( str );
      for ( var i = 0; i < str.Length; ++i )
      {
         if ( NeedsEscaping( str[i] ) )
         {
            retVal += asciiSize;
         }
      }

      return retVal;
   }

   private static Boolean NeedsEscaping( Char c )
   {
      switch ( c )
      {
         case STR_END:
         case STR_ESCAPE_PREFIX:
         case '/':
         case '\b':
         case '\f':
         case '\n':
         case '\r':
         case '\t':
            return true;
         default:
            return false;
      }
   }

   /// <summary>
   /// Asynchronously writes JSON value (array, object, or primitive value) to this <see cref="StreamWriterWithResizableBuffer"/> using given <see cref="IEncodingInfo"/>.
   /// Tries to keep the buffer of this stream as little as possible, and allocating as little as possible any other extra objects than created JSON objects (currently parsing a <see cref="Double"/> needs to allocate string).
   /// </summary>
   /// <param name="stream">This <see cref="StreamWriterWithResizableBuffer"/>.</param>
   /// <param name="encoding">The <see cref="IEncodingInfo"/> to use.</param>
   /// <param name="jsonValue">The JSON value.</param>
   /// <returns>A task which when completed will contain the amount of bytes written to this stream.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="StreamWriterWithResizableBuffer"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="encoding"/> is <c>null</c>; or if <paramref name="jsonValue"/> or any of the JSON values it contains is <c>null</c>.</exception>
   /// <exception cref="NotSupportedException">When a <see cref="JToken"/> is encountered which is not <see cref="JArray"/>, <see cref="JObject"/> or <see cref="JValue"/>; or when the value of <see cref="JValue"/> is not recognized.</exception>
   public static ValueTask<Int32> WriteJSONTTokenAsync(
      this StreamWriterWithResizableBuffer stream,
      IEncodingInfo encoding,
      JToken jsonValue
      )
   {
      return PerformWriteJSONTTokenAsync(
         ArgumentValidator.ValidateNotNullReference( stream ),
         ArgumentValidator.ValidateNotNull( nameof( encoding ), encoding ),
         jsonValue
         );
   }

   private static async ValueTask<Int32> PerformWriteJSONTTokenAsync(
      StreamWriterWithResizableBuffer stream,
      IEncodingInfo encoding,
      JToken jsonValue
      )
   {
      ArgumentValidator.ValidateNotNull( nameof( jsonValue ), jsonValue );
      Int32 bytesWritten;
      var asciiSize = encoding.BytesPerASCIICharacter;
      Int32 max;
      (Int32 Offset, Int32 Count) range;
      switch ( jsonValue )
      {
         case JArray array:
            range = stream.ReserveBufferSegment( asciiSize );
            encoding.WriteASCIIByte( stream.Buffer, ref range.Offset, (Byte) ARRAY_START );
            bytesWritten = range.Count;
            max = array.Count;
            if ( max > 0 )
            {
               for ( var i = 0; i < max; ++i )
               {
                  bytesWritten += await PerformWriteJSONTTokenAsync( stream, encoding, array[i] );
                  if ( i < max - 1 )
                  {
                     range = stream.ReserveBufferSegment( asciiSize );
                     encoding.WriteASCIIByte( stream.Buffer, ref range.Offset, (Byte) ARRAY_VALUE_DELIM );
                     bytesWritten += range.Count;
                  }
               }
            }
            range = stream.ReserveBufferSegment( asciiSize );
            encoding.WriteASCIIByte( stream.Buffer, ref range.Offset, (Byte) ARRAY_END );
            bytesWritten += await stream.FlushAsync();
            break;
         case JObject obj:
            range = stream.ReserveBufferSegment( asciiSize );
            encoding.WriteASCIIByte( stream.Buffer, ref range.Offset, (Byte) OBJ_START );
            bytesWritten = range.Count;
            max = obj.Count;
            if ( max > 0 )
            {
               var j = 0;
               foreach ( var kvp in obj )
               {
                  bytesWritten += WriteJSONString( stream, encoding, kvp.Key );
                  range = stream.ReserveBufferSegment( asciiSize );
                  encoding.WriteASCIIByte( stream.Buffer, ref range.Offset, (Byte) OBJ_KEY_VALUE_DELIM );
                  bytesWritten += range.Count;
                  await stream.FlushAsync();
                  bytesWritten += await PerformWriteJSONTTokenAsync( stream, encoding, kvp.Value );
                  if ( ++j < max )
                  {
                     range = stream.ReserveBufferSegment( asciiSize );
                     encoding.WriteASCIIByte( stream.Buffer, ref range.Offset, (Byte) OBJ_VALUE_DELIM );
                     bytesWritten += range.Count;
                  }
               }
            }
            range = stream.ReserveBufferSegment( asciiSize );
            encoding.WriteASCIIByte( stream.Buffer, ref range.Offset, (Byte) OBJ_END );
            bytesWritten += await stream.FlushAsync();
            break;
         case JValue value:
            var val = value.Value;
            switch ( val )
            {
               case String str:
                  bytesWritten = WriteJSONString( stream, encoding, str );
                  break;
               case Boolean boolean:
                  // Write 'true' or 'false'
                  range = stream.ReserveBufferSegment( boolean ? 4 : 5 );
                  encoding.WriteString( stream.Buffer, ref range.Offset, boolean ? "true" : "false" );
                  bytesWritten = range.Count;
                  break;
               case Int64 i64:
                  range = stream.ReserveBufferSegment( encoding.GetTextualIntegerRepresentationSize( i64 ) );
                  encoding.WriteIntegerTextual( stream.Buffer, ref range.Offset, i64 );
                  bytesWritten = range.Count;
                  break;
               case Double dbl:
                  // Have to allocate string :/
                  var dblStr = dbl.ToString( System.Globalization.CultureInfo.InvariantCulture );
                  range = stream.ReserveBufferSegment( encoding.Encoding.GetByteCount( dblStr ) );
                  encoding.WriteString( stream.Buffer, ref range.Offset, dblStr );
                  bytesWritten = range.Count;
                  break;
               case null:
                  // Write 'null'
                  range = stream.ReserveBufferSegment( asciiSize * 4 );
                  encoding.WriteString( stream.Buffer, ref range.Offset, "null" );
                  bytesWritten = range.Count;
                  break;
               default:
                  throw new NotSupportedException( $"Unsupported primitive value {val}." );
            }
            // Remember to flush stream
            await stream.FlushAsync();
            break;
         default:
            throw new NotSupportedException( $"Unrecognized JToken type: {jsonValue?.GetType()}." );
      }

      return bytesWritten;
   }

   private static Int32 WriteJSONString(
      StreamWriterWithResizableBuffer stream,
      IEncodingInfo encoding,
      String str
   )
   {
      // Write starting quote
      var asciiSize = encoding.BytesPerASCIICharacter;
      var eencoding = encoding.Encoding;
      // Allocate enough bytes for whole string and 2 quotes, we will allocate more as we encounter escapable characters
      var range = stream.ReserveBufferSegment( eencoding.GetByteCount( str ) + 2 * asciiSize );
      var array = stream.Buffer;
      var start = range.Offset;
      var idx = start;
      encoding.WriteASCIIByte( array, ref idx, (Byte) STR_START );

      // Write contents in chunks
      var prevStrIdx = 0;
      for ( var i = 0; i < str.Length; ++i )
      {
         var c = str[i];
         if ( NeedsEscaping( c ) )
         {
            // Append previous chunk
            idx += eencoding.GetBytes( str, prevStrIdx, i - prevStrIdx, array, idx );
            // Make sure we have room for escape character
            stream.ReserveBufferSegment( asciiSize );
            array = stream.Buffer;
            // Append escape character
            encoding.WriteASCIIByte( array, ref idx, (Byte) STR_ESCAPE_PREFIX );
            // Append escape sequence latter character
            TransformToEscape( encoding, array, ref idx, c );
            // Update index
            prevStrIdx = i + 1;
         }
      }

      // Append final chunk
      var finalChunkSize = str.Length - prevStrIdx;
      if ( finalChunkSize > 0 )
      {
         idx += eencoding.GetBytes( str, prevStrIdx, finalChunkSize, array, idx );
      }

      // Append closing quote
      encoding.WriteASCIIByte( array, ref idx, (Byte) STR_END );

      return idx - start;
   }

   private static void TransformToEscape(
      IEncodingInfo encoding,
      Byte[] array,
      ref Int32 idx,
      Char charRead
      )
   {
      Byte replacementByte = 0;
      switch ( charRead )
      {
         case STR_END:
         case STR_ESCAPE_PREFIX:
         case '/':
            // Actual value is just just read char minus the '\'
            replacementByte = (Byte) charRead;
            break;
         case '\b':
            // Backspace
            replacementByte = (Byte) 'b';
            break;
         case '\f':
            // Form feed
            replacementByte = (Byte) 'f';
            break;
         case '\n':
            // New line
            replacementByte = (Byte) 'n';
            break;
         case '\r':
            // Carriage return
            replacementByte = (Byte) 'r';
            break;
         case '\t':
            // Horizontal tab
            replacementByte = (Byte) 't';
            break;
         // \u escape is not needed - it is provided mainly for humans.
         default:
            throw new NotSupportedException( $"Unsupported escape sequence: '{charRead}'." );
      }

      if ( replacementByte != 0 )
      {
         encoding.WriteASCIIByte( array, ref idx, replacementByte );
      }
   }
}