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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//using TChallengeResult = System.Threading.Tasks.ValueTask<System.ValueTuple<System.Int32, UtilPack.Cryptography.SASL.SASLChallengeResult>>;
using TSyncChallengeResult = UtilPack.EitherOr<System.ValueTuple<System.Int32, UtilPack.Cryptography.SASL.SASLChallengeResult>, System.Int32>;
using TProtectedSyncChallengeResult = UtilPack.EitherOr<System.ValueTuple<System.Int32, UtilPack.Cryptography.SASL.SASLChallengeResult>, System.Int32>;

namespace UtilPack.Cryptography.SASL
{
   using TAsyncSyncChallengeResult = System.Threading.Tasks.ValueTask<TSyncChallengeResult>;
   using TProtectedAsyncSyncChallengeResult = System.Threading.Tasks.ValueTask<TProtectedSyncChallengeResult>;

   public interface SASLMechanism : IDisposable
   {
      TAsyncSyncChallengeResult Challenge(
         SASLAuthenticationArguments args
         );

      void Reset();
   }


   public enum SASLChallengeResult
   {
      MoreToCome,
      Completed
   }

   public struct SASLAuthenticationArguments
   {
      public SASLAuthenticationArguments(
         Byte[] readArray,
         Int32 readOffset,
         Int32 readCount,
         ResizableArray<Byte> writeArray,
         Int32 writeOffset,
         IEncodingInfo encoding,
         Object credentials
         )
      {
         this.ReadArray = ArgumentValidator.ValidateNotNull( nameof( readArray ), readArray );
         this.ReadOffset = readOffset;
         this.ReadCount = readCount;
         this.WriteArray = writeArray;
         this.WriteOffset = writeOffset;
         this.Encoding = encoding;
         this.Credentials = credentials;
      }

      public Byte[] ReadArray { get; }
      public Int32 ReadOffset { get; }
      public Int32 ReadCount { get; }
      public ResizableArray<Byte> WriteArray { get; }
      public Int32 WriteOffset { get; }
      public IEncodingInfo Encoding { get; }
      public Object Credentials { get; }
   }

   public static class SASLAuthenticationArgumentsFactory
   {
      public static SASLAuthenticationArguments CreateClientArguments(
         Byte[] readArray,
         Int32 readOffset,
         Int32 readCount,
         ResizableArray<Byte> writeArray,
         Int32 writeOffset,
         IEncodingInfo encoding,
         Object credentials
         )
      {
         return new SASLAuthenticationArguments( readArray, readOffset, readCount, writeArray, writeOffset, encoding, credentials );
      }

      public static SASLAuthenticationArguments CreateServerArguments(
         Byte[] readArray,
         Int32 readOffset,
         Int32 readCount,
         ResizableArray<Byte> writeArray,
         Int32 writeOffset,
         IEncodingInfo encoding,
         SASLCredentialsHolder credentials
         )
      {
         return new SASLAuthenticationArguments( readArray, readOffset, readCount, writeArray, writeOffset, encoding, credentials );
      }
   }

   public sealed class SASLCredentialsHolder
   {
      private Object _credentials;

      public Object Credentials
      {
         get => this._credentials;
         set => Interlocked.CompareExchange( ref this._credentials, value, null );
      }
   }

   public abstract class AbstractSASLMechanism : AbstractDisposable, SASLMechanism
   {
      public abstract TAsyncSyncChallengeResult Challenge( SASLAuthenticationArguments args );
      public abstract void Reset();

      protected abstract Int32 GetExceptionErrorCode( Exception exc );

      protected static Int32 MakeSureIsNegative( Int32 errorCode )
      {
         return errorCode == 0 ?
            -1 : ( errorCode > 0 ? -errorCode : errorCode );

      }
   }


   public abstract class AbstractSyncSASLMechanism<TCredentials> : AbstractSASLMechanism
   {
      public sealed override TAsyncSyncChallengeResult Challenge(
         SASLAuthenticationArguments args
      )
      {
         try
         {
            return new TAsyncSyncChallengeResult( this.Challenge( ref args, (TCredentials) args.Credentials ) );
         }
         catch ( Exception exc )
         {
            return new TAsyncSyncChallengeResult( MakeSureIsNegative( this.GetExceptionErrorCode( exc ) ) );
         }
      }

      protected abstract TProtectedSyncChallengeResult Challenge(
         ref SASLAuthenticationArguments args,
         TCredentials credentials
         );
   }

   public abstract class AbstractAsyncSASLMechanism<TCredentials> : AbstractSASLMechanism
   {
      public sealed override async TAsyncSyncChallengeResult Challenge(
         SASLAuthenticationArguments args
      )
      {
         try
         {
            return await this.Challenge( args, (TCredentials) args.Credentials );
         }
         catch ( Exception exc )
         {
            return new TSyncChallengeResult( MakeSureIsNegative( this.GetExceptionErrorCode( exc ) ) );
         }
      }

      protected abstract TProtectedAsyncSyncChallengeResult Challenge(
         SASLAuthenticationArguments args,
         TCredentials credentials
         );
   }

   public abstract class AbstractServerSASLMechanism<TCredentials> : AbstractAsyncSASLMechanism<SASLCredentialsHolder>
      where TCredentials : class
   {
      protected override TProtectedAsyncSyncChallengeResult Challenge(
         SASLAuthenticationArguments args,
         SASLCredentialsHolder credentials
         )
         => this.ChallengeServer( args, credentials, credentials.Credentials as TCredentials );

      protected abstract TProtectedAsyncSyncChallengeResult ChallengeServer(
         SASLAuthenticationArguments args,
         SASLCredentialsHolder credentialsHolder,
         TCredentials credentials
         );
   }

   public delegate String CustomStringDenormalizer( IEncodingInfo encoding, Byte[] array, Int32 index );
   public delegate String CustomStringNormalizer( String str, Int32 charIndex );

   public static class SASLUtility
   {
      private const Char MIN_SPECIAL_CHAR = '\u00A0';

      // String preparation, as specified in RFC4013

      // Returns -1 if all string characters pass all RFC4013 checks
      public static Int32 CheckString(
         String str,
         CustomStringNormalizer normalizer = null
         )
      {
         var retVal = -1;
         for ( var i = 0; i < str.Length && retVal == -1; ++i )
         {
            var c = str[i];
            if ( Char.IsControl( c ) )
            {
               // Control characters not allowed
               throw new ArgumentException( "Control characters are not allowed.", nameof( str ) );
            }
            else if ( c >= MIN_SPECIAL_CHAR )
            {
               // It is possible that we need to replace character
               if ( IsNonAsciiSpace( c ) )
               {
                  // Mapping special space into "normal" space (0x20)
                  retVal = i;
               }
               else if ( IsCommonlyMappedToNothing( c ) )
               {
                  // Create builder but don't append anything, as this character is 'nothing'
                  retVal = i;
               }
               else if ( IsProhibited( str, i ) )
               {
                  // Illegal character
                  throw new ArgumentException( $"Prohibited character at index {i}", nameof( str ) );
               }
               else if ( normalizer?.Invoke( str, i ) != null )
               {
                  retVal = i;
               }
            }
            else if ( normalizer?.Invoke( str, i ) != null )
            {
               retVal = i;
            }
         }

         return retVal;
      }


      public static void WriteString(
         String str,
         IEncodingInfo encodingInfo,
         ResizableArray<Byte> array,
         ref Int32 idx,
         CustomStringNormalizer normalizer = null,
         Int32? checkResult = null
         )
      {
         var checkResultValue = checkResult ?? CheckString( str, normalizer );
         var encoding = encodingInfo.Encoding;
         if ( checkResultValue == -1 )
         {
            // String may be written as-is
            array.CurrentMaxCapacity = idx + encoding.GetByteCount( str );
            encodingInfo.WriteString( array.Array, ref idx, str );
         }
         else
         {
            var maxCharByteCountDiff = encodingInfo.MaxCharByteCount - 1;

            String tmpStr;

            // Calculate byte size (actually, we are calculating the upper bound for byte size, but that's ok)
            var byteSize = encoding.GetByteCount( str );
            for ( var i = checkResultValue; i < str.Length; ++i )
            {
               var c = str[i];
               if ( c >= MIN_SPECIAL_CHAR )
               {
                  // It is possible that we need to replace character
                  if ( IsNonAsciiSpace( c ) )
                  {
                     // Mapping special space into "normal" space (0x20)
                     // Byte size doesn't grow at least.
                  }
                  else if ( IsCommonlyMappedToNothing( c ) )
                  {
                     --byteSize;
                  }
                  else if ( ( tmpStr = normalizer?.Invoke( str, i ) ) != null )
                  {
                     byteSize += encoding.GetByteCount( tmpStr ) - 1;
                  }
               }
            }

            // Now we know that the string will take max 'byteSize' amount of bytes
            array.CurrentMaxCapacity = idx + byteSize;
            // We can get the actual array now, since we won't be resizing the ResizableArray anymore
            var buffer = array.Array;

            // Helper method to write chunks of "normal" characters
            var prev = 0;
            void WritePreviousChunk( Int32 charIdx, ref Int32 writeIdx )
            {
               writeIdx += encoding.GetBytes( str, prev, charIdx - prev, buffer, writeIdx );
               prev = charIdx + 1;

            }

            for ( var i = checkResultValue; i < str.Length; ++i )
            {
               var c = str[i];
               if ( c >= MIN_SPECIAL_CHAR )
               {
                  // It is possible that we need to replace character
                  if ( IsNonAsciiSpace( c ) )
                  {
                     // Mapping special space into "normal" space (0x20)
                     WritePreviousChunk( i, ref idx );
                     encodingInfo.WriteASCIIByte( buffer, ref idx, 0x20 );
                  }
                  else if ( IsCommonlyMappedToNothing( c ) )
                  {
                     // Nothing additional to write
                     WritePreviousChunk( i, ref idx );
                  }
                  else if ( ( tmpStr = normalizer?.Invoke( str, i ) ) != null )
                  {
                     WritePreviousChunk( i, ref idx );
                     if ( tmpStr.Length > 0 )
                     {
                        encodingInfo.WriteString( buffer, ref idx, tmpStr );
                     }
                  }
               }
            }

            if ( prev < str.Length )
            {
               // Write final chunk
               WritePreviousChunk( str.Length - 1, ref idx );
            }
         }
      }

      public static String ReadString(
         IEncodingInfo encodingInfo,
         Byte[] array,
         ref Int32 idx,
         Int32 maxCount, // exclusive
         CustomStringDenormalizer denormalizer = null
         )
      {
         var max = idx + maxCount;
         var endIdx = Array.IndexOf( array, (Byte) ',', idx, max - idx );
         if ( endIdx < 0 )
         {
            endIdx = max;
         }

         var min = encodingInfo.MinCharByteCount;
         Int32 count;
         if ( endIdx < max && min > 1 )
         {
            // Have to detect actual end of string... it depends on endianness of encoding
            count = endIdx;
            if ( endIdx == max - 1
               || ( endIdx > idx + min && encodingInfo.ReadASCIIByte( array, ref count ) != ',' )
               )
            {
               // Must be big-endian
               endIdx -= encodingInfo.MinCharByteCount - 1;
            }
         }

         String retVal;
         count = endIdx - idx;
         if ( denormalizer == null )
         {
            // Can just create string
            retVal = encodingInfo.Encoding.GetString( array, idx, count );
         }
         else
         {
            StringBuilder sb = null;
            var encoding = encodingInfo.Encoding;
            var prev = idx;
            for ( var i = idx; i < endIdx; i += min )
            {
               var replacement = denormalizer( encodingInfo, array, i );
               if ( replacement != null )
               {
                  if ( sb == null )
                  {
                     sb = new StringBuilder();
                  }
                  // Append previous chunk
                  sb.Append( encoding.GetString( array, prev, i - prev ) );
                  sb.Append( replacement );
               }
            }


            if ( sb == null )
            {
               // No escapable strings
               retVal = encoding.GetString( array, idx, count );
            }
            else
            {
               // Append final chunk
               sb.Append( encoding.GetString( array, prev, endIdx - prev ) );
               retVal = sb.ToString();
            }
         }
         idx += count;

         return retVal;
      }

      private static Boolean IsNonAsciiSpace( Char c )
      {
         switch ( c )
         {
            case MIN_SPECIAL_CHAR: // NO-BREAK SPACE
            case '\u1680': // OGHAM SPACE MARK
            case '\u2000': // EN QUAD
            case '\u2001': // EM QUAD
            case '\u2002': // EN SPACE
            case '\u2003': // EM SPACE
            case '\u2004': // THREE-PER-EM SPACE
            case '\u2005': // FOUR-PER-EM SPACE
            case '\u2006': // SIX-PER-EM SPACE
            case '\u2007': // FIGURE SPACE
            case '\u2008': // PUNCTUATION SPACE
            case '\u2009': // THIN SPACE
            case '\u200A': // HAIR SPACE
            case '\u200B': // ZERO WIDTH SPACE
            case '\u202F': // NARROW NO-BREAK SPACE
            case '\u205F': // MEDIUM MATHEMATICAL SPACE
            case '\u3000': // IDEOGRAPHIC SPACE
               return true;
            default:
               return false;
         }
      }

      static Boolean IsCommonlyMappedToNothing( Char c )
      {
         switch ( c )
         {
            case '\u00AD': // SOFT HYPHEN
            case '\u034F': // COMBINING GRAPHEME JOINER
            case '\u1806': // MONGOLIAN TODO SOFT HYPHEN
            case '\u180B': // MONGOLIAN FREE VARIATION SELECTOR ONE
            case '\u180C': // MONGOLIAN FREE VARIATION SELECTOR TWO
            case '\u180D': // MONGOLIAN FREE VARIATION SELECTOR THREE
            case '\u200B': // ZERO WIDTH SPACE
            case '\u200C': // ZERO WIDTH NON-JOINER
            case '\u200D': // ZERO WIDTH JOINER
            case '\u2060': // WORD JOINER
            case '\uFE00': // VARIATION SELECTOR-1
            case '\uFE01': // VARIATION SELECTOR-2
            case '\uFE02': // VARIATION SELECTOR-3
            case '\uFE03': // VARIATION SELECTOR-4
            case '\uFE04': // VARIATION SELECTOR-5
            case '\uFE05': // VARIATION SELECTOR-6
            case '\uFE06': // VARIATION SELECTOR-7
            case '\uFE07': // VARIATION SELECTOR-8
            case '\uFE08': // VARIATION SELECTOR-9
            case '\uFE09': // VARIATION SELECTOR-10
            case '\uFE0A': // VARIATION SELECTOR-11
            case '\uFE0B': // VARIATION SELECTOR-12
            case '\uFE0C': // VARIATION SELECTOR-13
            case '\uFE0D': // VARIATION SELECTOR-14
            case '\uFE0E': // VARIATION SELECTOR-15
            case '\uFE0F': // VARIATION SELECTOR-16
            case '\uFEFF': // ZERO WIDTH NO-BREAK SPACE
               return true;
            default:
               return false;
         }
      }

      static Boolean IsProhibited( String s, Int32 index )
      {
         var u = Char.ConvertToUtf32( s, index );
         Boolean retVal;
         if ( u >= 0x0340 )
         {
            // There is possibility that this character is prohibited
            if ( u >= 0x2FF0 )
            {
               // Inappropriate for plain text characters: http://tools.ietf.org/html/rfc3454#appendix-C.6
               switch ( u )
               {
                  case 0xFFF9: // INTERLINEAR ANNOTATION ANCHOR
                  case 0xFFFA: // INTERLINEAR ANNOTATION SEPARATOR
                  case 0xFFFB: // INTERLINEAR ANNOTATION TERMINATOR
                  case 0xFFFC: // OBJECT REPLACEMENT CHARACTER
                  case 0xFFFD: // REPLACEMENT CHARACTER
                     retVal = true;
                     break;
                  default:
                     retVal =
                        // Private Use characters: http://tools.ietf.org/html/rfc3454#appendix-C.3
                        ( u >= 0xE000 && u <= 0xF8FF ) || ( u >= 0xF0000 && u <= 0xFFFFD ) || ( u >= 0x100000 && u <= 0x10FFFD ) ||
                        // Non-character code points: http://tools.ietf.org/html/rfc3454#appendix-C.4
                        ( u >= 0xFDD0 && u <= 0xFDEF ) || ( u >= 0xFFFE && u <= 0xFFFF ) || ( u >= 0x1FFFE && u <= 0x1FFFF ) ||
                        ( u >= 0x2FFFE && u <= 0x2FFFF ) || ( u >= 0x3FFFE && u <= 0x3FFFF ) || ( u >= 0x4FFFE && u <= 0x4FFFF ) ||
                        ( u >= 0x5FFFE && u <= 0x5FFFF ) || ( u >= 0x6FFFE && u <= 0x6FFFF ) || ( u >= 0x7FFFE && u <= 0x7FFFF ) ||
                        ( u >= 0x8FFFE && u <= 0x8FFFF ) || ( u >= 0x9FFFE && u <= 0x9FFFF ) || ( u >= 0xAFFFE && u <= 0xAFFFF ) ||
                        ( u >= 0xBFFFE && u <= 0xBFFFF ) || ( u >= 0xCFFFE && u <= 0xCFFFF ) || ( u >= 0xDFFFE && u <= 0xDFFFF ) ||
                        ( u >= 0xEFFFE && u <= 0xEFFFF ) || ( u >= 0xFFFFE && u <= 0xFFFFF ) || ( u >= 0x10FFFE && u <= 0x10FFFF ) ||
                        // Surrogate code points: http://tools.ietf.org/html/rfc3454#appendix-C.5
                        ( u >= 0xD800 && u <= 0xDFFF ) ||
                        // Inappropriate for canonical representation: http://tools.ietf.org/html/rfc3454#appendix-C.7
                        ( u >= 0x2FF0 && u <= 0x2FFB ) ||
                        // Tagging characters: http://tools.ietf.org/html/rfc3454#appendix-C.9
                        ( u == 0xE0001 || ( u >= 0xE0020 && u <= 0xE007F ) );
                     break;
               }
            }
            else
            {
               // Change display properties or are deprecated: http://tools.ietf.org/html/rfc3454#appendix-C.8
               switch ( u )
               {
                  case 0x0340: // COMBINING GRAVE TONE MARK
                  case 0x0341: // COMBINING ACUTE TONE MARK
                  case 0x200E: // LEFT-TO-RIGHT MARK
                  case 0x200F: // RIGHT-TO-LEFT MARK
                  case 0x202A: // LEFT-TO-RIGHT EMBEDDING
                  case 0x202B: // RIGHT-TO-LEFT EMBEDDING
                  case 0x202C: // POP DIRECTIONAL FORMATTING
                  case 0x202D: // LEFT-TO-RIGHT OVERRIDE
                  case 0x202E: // RIGHT-TO-LEFT OVERRIDE
                  case 0x206A: // INHIBIT SYMMETRIC SWAPPING
                  case 0x206B: // ACTIVATE SYMMETRIC SWAPPING
                  case 0x206C: // INHIBIT ARABIC FORM SHAPING
                  case 0x206D: // ACTIVATE ARABIC FORM SHAPING
                  case 0x206E: // NATIONAL DIGIT SHAPES
                  case 0x206F: // NOMINAL DIGIT SHAPES
                     retVal = true;
                     break;
                  default:
                     retVal = false;
                     break;
               }
            }
         }
         else
         {
            // This character is definetly not prohibited
            retVal = false;
         }
         return retVal;
      }
   }

}
