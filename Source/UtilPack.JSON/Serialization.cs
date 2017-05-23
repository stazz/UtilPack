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
using UtilPack.JSON;

namespace UtilPack.JSON
{
   internal static class Consts
   {
      public const Char STR_START = '"';
      public const Char STR_END = '"';
      public const Char STR_ESCAPE_PREFIX = '\\';
      public const Char STR_FORWARD_SLASH = '/';

      public const Char ARRAY_START = '[';
      public const Char ARRAY_VALUE_DELIM = ',';
      public const Char ARRAY_END = ']';

      public const Char OBJ_START = '{';
      public const Char OBJ_VALUE_DELIM = ',';
      public const Char OBJ_END = '}';
      public const Char OBJ_KEY_VALUE_DELIM = ':';

      public const Byte NUMBER_DECIMAL = (Byte) '.';
      public const Byte NUMBER_EXP_LOW = (Byte) 'e';
      public const Byte NUMBER_EXP_UPPER = (Byte) 'E';
   }

   /// <summary>
   /// This clas implements <see cref="PotentiallyAsyncReaderLogic{TValue, TSource}"/> so that it can construct <see cref="JToken"/> objects from a reader which returns characters.
   /// </summary>
   public class JTokenStreamReader : PotentiallyAsyncReaderLogic<JToken, MemorizingPotentiallyAsyncReader<Char?, Char>>
   {
      // TODO make it customizable whether numbers are Doubles or Decimals...
      // TODO add support for DateTime & other stuff that is in Newtonsoft.JSON ?

      /// <summary>
      /// Gets the default, stateless instance.
      /// </summary>
      public static readonly JTokenStreamReader Instance = new JTokenStreamReader();

      private JTokenStreamReader()
      {
      }

      /// <summary>
      /// Asynchronously reads the JSON value (array, object, or primitive value) from given <see cref="MemorizingPotentiallyAsyncReader{TValue, TBufferItem}"/>.
      /// Tries to keep the buffer of this stream as little as possible, and allocating as little as possible any other extra objects than created JSON objects (currently parsing a <see cref="Double"/> needs to allocate string).
      /// </summary>
      /// <param name="source">The <see cref="MemorizingPotentiallyAsyncReader{TValue, TBufferItem}"/>.</param>
      /// <returns>A task which will contain deserialized <see cref="JToken"/> on its completion.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="source"/> is <c>null</c>.</exception>
      public ValueTask<JToken> TryReadNextAsync( MemorizingPotentiallyAsyncReader<Char?, Char> source )
      {
         return PerformReadJSONTTokenAsync( ArgumentValidator.ValidateNotNull( nameof( source ), source ) );
      }

      /// <summary>
      /// Checks whether read result is successful, that is, the given <see cref="JToken"/> is not <c>null</c>.
      /// </summary>
      /// <param name="readResult">The <see cref="JToken"/> read by <see cref="TryReadNextAsync"/>.</param>
      /// <returns><c>true</c> if <paramref name="readResult"/> is not <c>null</c>; <c>false</c> otherwise.</returns>
      public Boolean IsReadSuccessful( JToken readResult )
      {
         return readResult != null;
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
            case Consts.ARRAY_END:
            case Consts.OBJ_END:
               // This happens only when reading empty array/object, and this is called recursively.
               await reader.TryReadNextAsync(); // Consume peeked character
               retVal = null;
               break;
            case Consts.ARRAY_START:
               await reader.TryReadNextAsync(); // Consume peeked character
               var array = new JArray();
               encounteredContainerEnd = false;
               // Reuse 'retVal' variable since we really need it only at the end of this case block.
               while ( !encounteredContainerEnd && ( retVal = await PerformReadJSONTTokenAsync( reader ) ) != null )
               {
                  array.Add( retVal );
                  // Read next non-whitespace character - it will be either array value delimiter (',') or array end (']')
                  charRead = await reader.ReadUntilAsync( c => !Char.IsWhiteSpace( c ) );
                  encounteredContainerEnd = charRead == Consts.ARRAY_END;
               }
               retVal = array;
               break;
            case Consts.OBJ_START:
               await reader.TryReadNextAsync(); // Consume peeked character
               var obj = new JObject();
               encounteredContainerEnd = false;
               String keyStr;
               // Reuse 'retVal' variable since we really need it only at the end of this case block.
               while ( !encounteredContainerEnd && ( keyStr = await ReadJSONStringAsync( reader, false ) ) != null )
               {
                  // First JToken should be string being the key
                  // Skip whitespace and ':'
                  charRead = await reader.PeekUntilAsync( c => !Char.IsWhiteSpace( c ) && c != Consts.OBJ_KEY_VALUE_DELIM );
                  // Read another JToken, this one will be our value
                  retVal = await PerformReadJSONTTokenAsync( reader );
                  obj.Add( keyStr, retVal );
                  // Read next non-whitespace character - it will be either object value delimiter (','), or object end ('}')
                  charRead = await reader.ReadUntilAsync( c => !Char.IsWhiteSpace( c ) );
                  encounteredContainerEnd = charRead == Consts.OBJ_END;
               }
               retVal = obj;
               break;
            case Consts.STR_START:
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
                  if ( nextChar.HasValue && ( nextChar == Consts.NUMBER_DECIMAL || nextChar == Consts.NUMBER_EXP_LOW || nextChar == Consts.NUMBER_EXP_UPPER ) )
                  {
                     // This is not a integer, but is a number -> read until first non-number-character
                     await reader.ReadNextAsync();
                     await reader.PeekUntilAsync( c => !( ( c >= '0' && c <= '9' ) || c == Consts.NUMBER_EXP_LOW || c == Consts.NUMBER_EXP_UPPER || c == '-' || c == '+' ) );
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
            proceed = charRead == Consts.STR_START;
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
                  decoded = (Char) ( ( array[decodeStartIdx].GetHexadecimalValue().GetValueOrDefault() << 12 ) | ( array[decodeStartIdx + 1].GetHexadecimalValue().GetValueOrDefault() << 8 ) | ( array[decodeStartIdx + 2].GetHexadecimalValue().GetValueOrDefault() << 4 ) | array[decodeStartIdx + 3].GetHexadecimalValue().GetValueOrDefault() );
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
               charRead = ( await reader.TryReadNextAsync() ) ?? Consts.STR_END;
               if ( charRead == Consts.STR_ESCAPE_PREFIX )
               {
                  // Escape handling - next character decides what we will do
                  charRead = ( await reader.TryReadNextAsync() ) ?? Consts.STR_END;
                  Char replacementByte = '\0';
                  switch ( charRead )
                  {
                     case Consts.STR_END:
                     case Consts.STR_ESCAPE_PREFIX:
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
            } while ( charRead != Consts.STR_END );

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


   }
}
/// <summary>
/// This class contains extension methods for types in UtilPack.
/// </summary>
public static partial class E_UtilPack
{

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
         case Consts.STR_END:
         case Consts.STR_ESCAPE_PREFIX:
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
   /// Creates <see cref="PotentiallyAsyncWriterAndObservable{TValue}"/> which will write <see cref="JToken"/>s as character enumerables to given sink.
   /// </summary>
   /// <param name="logic">This <see cref="PotentiallyAsyncWriterLogic{TValue, TSink}"/>.</param>
   /// <param name="sink">The sink to which perform writing.</param>
   /// <returns>An instance of <see cref="PotentiallyAsyncWriterAndObservable{TValue}"/> to be used to serialize <see cref="JToken"/>s to <paramref name="sink"/>.</returns>
   public static PotentiallyAsyncWriterAndObservable<JToken> CreateJTokenWriter<TSink>(
      this PotentiallyAsyncWriterLogic<IEnumerable<Char>, TSink> logic,
      TSink sink
      )
   {
      return WriterFactory.CreateTransformableWriter<JToken, TSink, IEnumerable<Char>>(
         ArgumentValidator.ValidateNotNullReference( logic ),
         sink,
         TransformJToken
         );
   }

   private static IEnumerable<Char> TransformJToken( JToken token )
   {
      Int32 max;
      switch ( token )
      {
         case JArray array:
            yield return Consts.ARRAY_START;
            max = array.Count;
            for ( var i = 0; i < max; ++i )
            {
               foreach ( var c in TransformJToken( array[i] ) )
               {
                  yield return c;
               }
               if ( i < max - 1 )
               {
                  yield return Consts.ARRAY_VALUE_DELIM;
               }
            }
            yield return Consts.ARRAY_END;
            break;
         case JObject obj:
            yield return Consts.OBJ_START;
            max = obj.Count;
            if ( max > 0 )
            {
               var j = 0;
               foreach ( var kvp in obj )
               {
                  foreach ( var c in TransformJString( kvp.Key ) )
                  {
                     yield return c;
                  }

                  yield return Consts.OBJ_KEY_VALUE_DELIM;

                  foreach ( var c in TransformJToken( kvp.Value ) )
                  {
                     yield return c;
                  }

                  if ( ++j < max )
                  {
                     yield return Consts.OBJ_VALUE_DELIM;
                  }
               }
            }
            yield return Consts.OBJ_END;
            break;
         case JValue value:
            var val = value.Value;
            IEnumerable<Char> valueEnumerable;
            switch ( val )
            {
               case String str:
                  valueEnumerable = TransformJString( str );
                  break;
               case Boolean boolean:
                  // Write 'true' or 'false'
                  if ( boolean )
                  {
                     yield return 't'; yield return 'r'; yield return 'u'; yield return 'e';
                  }
                  else
                  {
                     yield return 'f'; yield return 'a'; yield return 'l'; yield return 's'; yield return 'e';
                  }
                  valueEnumerable = null;
                  break;
               case Int64 i64:
                  valueEnumerable = i64.AsCharEnumerable();
                  break;
               case Double dbl:
                  // Have to allocate string :/
                  valueEnumerable = dbl.ToString( System.Globalization.CultureInfo.InvariantCulture ).AsCharEnumerable();
                  break;
               case null:
                  // Write 'null'
                  yield return 'n'; yield return 'u'; yield return 'l'; yield return 'l';
                  valueEnumerable = null;
                  break;
               default:
                  throw new NotSupportedException( $"Unsupported primitive value {val}." );
            }
            if ( valueEnumerable != null )
            {
               foreach ( var c in valueEnumerable )
               {
                  yield return c;
               }
            }
            break;
         default:
            throw new NotSupportedException( $"Unrecognized JToken type: {token?.GetType()}." );
      }
   }

   private static IEnumerable<Char> TransformJString( String str )
   {
      yield return Consts.STR_START;

      var max = str.Length;
      for ( var i = 0; i < max; ++i )
      {
         var c = str[i];
         if ( NeedsEscaping( c ) )
         {
            yield return Consts.STR_ESCAPE_PREFIX;
            yield return TransformToEscape( c );
         }
         else
         {
            yield return c;
         }
      }

      yield return Consts.STR_END;
   }

   private static Char TransformToEscape(
      Char charRead
      )
   {
      switch ( charRead )
      {
         case Consts.STR_END:
         case Consts.STR_ESCAPE_PREFIX:
         case '/':
            // Actual value is just just read char minus the '\'
            return charRead;
         case '\b':
            // Backspace
            return 'b';
         case '\f':
            // Form feed
            return 'f';
         case '\n':
            // New line
            return 'n';
         case '\r':
            // Carriage return
            return 'r';
         case '\t':
            // Horizontal tab
            return 't';
         // \u escape is not needed - it is provided mainly for humans.
         default:
            throw new NotSupportedException( $"Unsupported escape sequence: '{charRead}'." );
      }
   }

}