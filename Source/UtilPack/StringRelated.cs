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
   /// This interface provides abstract way to read characters possibly asynchronously from some source (e.g. stream or <see cref="String"/>).
   /// </summary>
   /// <typeparam name="TValue">The type of values that this reader produces.</typeparam>
   /// <typeparam name="TSource">The type of the source from which to read the next character.</typeparam>
   public interface PotentiallyAsyncReader<TValue, in TSource>
   {
      /// <summary>
      /// Tries to read next item from given <paramref name="source"/>.
      /// </summary>
      /// <param name="source">The <typeparamref name="TSource"/> to read next item from.</param>
      /// <returns>A task which will return item read, also indicating whether more items are available.</returns>
      ValueTask<TValue> TryReadNextAsync( TSource source );
   }
   
   /// <summary>
   /// This class binds the source type parameter of <see cref="PotentiallyAsyncReader{TValue, TSource}"/> in order to provide read method without parameters.
   /// This class is intended to be used as parameter type to methods instead of passing a pair of <see cref="PotentiallyAsyncReader{TValue, TSource}"/> and the source object.
   /// </summary>
   public abstract class PotentiallyAsyncReader<TValue>
   {
      internal PotentiallyAsyncReader()
      {
      }
      
      /// <summary>
      /// <see cref="PotentiallyAsyncReader{TValue, TSource}.TryReadNextAsync"/>.
      /// </summary>
      /// <returns><see cref="PotentiallyAsyncReader{TValue, TSource}.TryReadNextAsync"/>.</returns>
      public abstract ValueTask<TValue> TryReadNextAsync();
   }
   
   /// <summary>
/// This class augments <see cref="PotentiallyAsyncReader{TValue}"/> with peekability.
   /// </summary>
   public abstract class PeekablePotentiallyAsyncReader<TValue> : PotentiallyAsyncReader<TValue>
   {
      internal PeekablePotentiallyAsyncReader()
      {
      }
      
      /// <summary>
      /// <see cref="PeekablePotentiallyAsyncReader{TValue, TSource}.TryPeekAsync"/>.
      /// </summary>
      /// <returns><see cref="PeekablePotentiallyAsyncReader{TValue, TSource}.TryPeekAsync"/>.</returns>
      public abstract ValueTask<TValue> TryPeekAsync();
   }
   
   /// <summary>
   /// This class provides implementation for <see cref="PotentiallyAsyncReader{TValue}.TryReadNextAsync"/>.
   /// </summary>
   public sealed class BoundPotentiallyAsyncReader<TValue, TSource> : PotentiallyAsyncReader<TValue>
   {
      
      /// <summary>
      /// Creates a new instance of <see cref="BoundPotentiallyAsyncReader{TValue, TSource}"/> with given reader and source.
      /// </summary>
      /// <param name="reader">The <see cref="PotentiallyAsyncReader{TValue, TSource}"/>.</param>
      /// <param name="source">The source for <paramref name="reader"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="reader"/> is <c>null</c>.</exception>
      public BoundPotentiallyAsyncReader(
        PotentiallyAsyncReader<TValue, TSource> reader,
        TSource source
        )
      {
         this.Reader = ArgumentValidator.ValidateNotNull( nameof(reader), reader);
         this.Source = source;
      }
      
      /// <summary>
      /// Gets the source of this reader.
      /// </summary>
      /// <value>The source of this reader.</value>
      public TSource Source { get; }
      
      /// <summary>
      /// Gets the underlying reader.
      /// </summary>
      /// <value>The underlying reader.</value>
      public PotentiallyAsyncReader<TValue, TSource> Reader { get; }
      
      /// <summary>
      /// <see cref="PotentiallyAsyncReader{TValue, TSource}.TryReadNextAsync"/>.
      /// </summary>
      /// <returns><see cref="PotentiallyAsyncReader{TValue, TSource}.TryReadNextAsync"/>.</returns>
      public override ValueTask<TValue> TryReadNextAsync()
      {
         return this.Reader.TryReadNextAsync( this.Source );
      }
   }
   
   /// <summary>
   /// This class provides implementation for <see cref="PotentiallyAsyncReader{TValue}.TryReadNextAsync"/> and <see cref="PeekablePotentiallyAsyncReader{TValue}.TryPeekAsync"/>.
   /// </summary>
   public sealed class BoundPeekablePotentiallyAsyncReader<TValue, TSource> : PeekablePotentiallyAsyncReader<TValue>
   {
      
      /// <summary>
      /// Creates a new instance of <see cref="BoundPeekablePotentiallyAsyncReader{TValue, TSource}"/> with given reader and source.
      /// </summary>
      /// <param name="reader">The <see cref="PeekablePotentiallyAsyncReader{TValue, TSource}"/>.</param>
      /// <param name="source">The source for <paramref name="reader"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="reader"/> is <c>null</c>.</exception>
      public BoundPeekablePotentiallyAsyncReader(
        PeekablePotentiallyAsyncReader<TValue, TSource> reader,
        TSource source
        )
      {
         this.Reader = ArgumentValidator.ValidateNotNull( nameof(reader), reader);
         this.Source = source;
      }
      
      /// <summary>
      /// Gets the source of this reader.
      /// </summary>
      /// <value>The source of this reader.</value>
      public TSource Source { get; }
      
      /// <summary>
      /// Gets the underlying reader.
      /// </summary>
      /// <value>The underlying reader.</value>
      public PeekablePotentiallyAsyncReader<TValue, TSource> Reader { get; }
      
      /// <summary>
      /// <see cref="PotentiallyAsyncReader{TValue, TSource}.TryReadNextAsync"/>.
      /// </summary>
      /// <returns><see cref="PotentiallyAsyncReader{TValue, TSource}.TryReadNextAsync"/>.</returns>
      public override ValueTask<TValue> TryReadNextAsync()
      {
         return this.Reader.TryReadNextAsync( this.Source );
      }
      
      /// <summary>
      /// <see cref="PeekablePotentiallyAsyncReader{TValue, TSource}.TryPeekAsync"/>.
      /// </summary>
      /// <returns><see cref="PeekablePotentiallyAsyncReader{TValue, TSource}.TryPeekAsync"/>.</returns>
      public override ValueTask<TValue> TryPeekAsync()
      {
         return this.Reader.TryPeekAsync( this.Source );
      }
   }
   
   /// <summary>
   /// This class represents incrementable index in a string.
   /// </summary>
   public sealed class StringIndex
   {
      private Int32 _idx;
      private readonly Int32 _max;
      
      /// <summary>
      /// Creates a new instance of <see cref="StringIndex"/> with given string and optional range.
      /// </summary>
      /// <param name="str">The string.</param>
      /// <param name="offset">The starting index.</param>
      /// <param name="count">The amount of characters to include to be indexed. Negative values are interpreted as "the rest starting from <paramref name="offset"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="str"/> is <c>null</c>.</exception>
      public StringIndex( 
         String str,
         Int32 offset = 0,
         Int32 count = -1
         )
      {
         this.String = ArgumentValidator.ValidateNotNull( nameof( str ), str );
         this._idx = Math.Max( offset, 0 );
         if ( count < 0 )
         {
            count = str.Length - this._idx;
         }
         this._max = Math.Min( this._idx + count, str.Length );
      }
      
      /// <summary>
      /// Gets the full string as given to this <see cref="StringIndex"/>.
      /// </summary>
      /// <value>The full string as given to this <see cref="StringIndex"/>.</value>
      public String String { get; }
      
      /// <summary>
      /// Gets the current index to the string of this <see cref="StringIndex"/>.
      /// </summary>
      /// <value>The current index to the string of this <see cref="StringIndex"/>.</value>
      public Int32 CurrentIndex 
      {
         get
         {
            return this._idx;
         }
      }
      
      /// <summary>
      /// Tries to get index for the next character.
      /// </summary>
      /// <param name="idx">This parameter will hold the index.</param>
      /// <returns><c>true</c> if <paramref name="idx"/> will fall within acceptable range; <c>false</c> otherwise.</returns>
      public Boolean TryGetNextIndex( out Int32 idx )
      {
         if ( this._idx < this._max && ( idx = Interlocked.Increment( ref this._idx ) ) <= this._max )
         {
            // Interlocked.Increment returns incremented value
            --idx;
         }
         else
         {
            idx = this._max;
         }
         
         return idx < this._max;
      }
   }
   
   /// <summary>
   /// This class implements <see cref="PotentiallyAsyncReader{TValue, TSource}"/> to provide pseudo-asynchronous character reading over a <see cref="StringIndex"/>.
   /// </summary>
   public sealed class StringCharacterReader : PotentiallyAsyncReader<Char?, StringIndex>
   {
      /// <summary>
      /// Gets the default, stateless instance.
      /// </summary>
      public static readonly StringCharacterReader Instance = new StringCharacterReader();
      
      private StringCharacterReader()
      {
      }
      
      /// <inheritdoc />
      public ValueTask<Char?> TryReadNextAsync( StringIndex source )
      {
         ArgumentValidator.ValidateNotNull( nameof( source ), source );
         return new ValueTask<Char?>( source.TryGetNextIndex( out Int32 idx ) ? source.String[idx] : (Char?)null );
      }
   }
   
   /// <summary>
   /// This class provides method to easily crate <see cref="PeekablePotentiallyAsyncReader{TValue, TSource}"/> instances.
   /// </summary>
   public static class PeekableReaderFactory
   {
      /// <summary>
      /// Creates new <see cref="PeekablePotentiallyAsyncReader{TValue, TSource}"/> which reads nullable struct values.
      /// </summary>
      /// <typeparam name="TValue">The struct type to read.</typeparam>
      /// <typeparam name="TSource">The source from which to read.</typeparam>
      /// <returns>A new <see cref="PeekablePotentiallyAsyncReader{TValue, TSource}"/> which reads nullable struct values.</returns>
      public static PeekablePotentiallyAsyncReader<TValue?, TSource> NewNullableValueReader<TValue, TSource>(
        PotentiallyAsyncReader<TValue?, TSource> reader
        )
        where TValue : struct
      {
         return new PeekablePotentiallyAsyncReader<TValue?, TSource>(
            reader,
            nullable => nullable.HasValue
            );
      }
   }
   
   /// <summary>
   /// This class wraps another <see cref="PotentiallyAsyncReader{TValue, TSource}"/> to implement peek functionality.
   /// </summary>
   public sealed class PeekablePotentiallyAsyncReader<TValue, TSource> : PotentiallyAsyncReader<TValue, TSource>
   {
      private readonly Func<TValue, Boolean> _readSuccessful;
      private Boolean _hasPeekedValue;
      private TValue _peekedValue;
      private Boolean _ended;
      
      /// <summary>
      /// Creates new instance of <see cref="PeekablePotentiallyAsyncReader{TValue, TSource}"/> wrapping another <see cref="PotentiallyAsyncReader{TValue, TSource}"/>.
      /// </summary>
      /// <param name="reader">The actual reader performing reading.</param>
      /// <param name="readSuccessful">The callback to check whether result of reader was successful.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="reader"/> is <c>null</c>.</exception>
      public PeekablePotentiallyAsyncReader(
         PotentiallyAsyncReader<TValue, TSource> reader,
         Func<TValue, Boolean> readSuccessful
         )
      {
         this.Reader = ArgumentValidator.ValidateNotNull( nameof(reader), reader );
         this._readSuccessful = ArgumentValidator.ValidateNotNull( nameof(readSuccessful), readSuccessful);
      }
      
      /// <summary>
      /// Gets the underlying reader of this <see cref="PeekablePotentiallyAsyncReader{TValue, TSource}"/>.
      /// </summary>
      /// <value>the underlying reader of this <see cref="PeekablePotentiallyAsyncReader{TValue, TSource}"/>.</value>
      public PotentiallyAsyncReader<TValue, TSource> Reader { get; }
      
      /// <inheritdoc />
      public async ValueTask<TValue> TryReadNextAsync( TSource source )
      {
         // TODO Interlocked.CompareExchange(ref this._state ... ) guards
         TValue retVal;
         if ( this._ended )
         {
            retVal = this._peekedValue;
         }
         else if ( this._hasPeekedValue)
         {
            retVal = this._peekedValue;
            this._hasPeekedValue = false;
         }
         else
         {
            retVal = await this.Reader.TryReadNextAsync( source );
            if ( !this._readSuccessful( retVal ) )
            {
               this._ended = true;
               this._peekedValue = retVal;
            }
         }
         
         return retVal;
      }
      
      /// <summary>
      /// Tries to peek the next value asynchronously.
      /// Subsequent calls to this method will use cached peeked value, until <see cref="TryReadNextAsync"/> is called.
      /// </summary>
      /// <param name="source">The source to peek from.</param>
      public async ValueTask<TValue> TryPeekAsync( TSource source )
      {
         TValue retVal;
         if ( this._ended )
         {
            retVal = this._peekedValue;
         }
         else if ( this._hasPeekedValue )
         {
            retVal = this._peekedValue;
         }
         else
         {
            retVal = await this.Reader.TryReadNextAsync( source );
            this._peekedValue = retVal;
            if ( !this._readSuccessful( retVal ) )
            {
               this._ended = true;
            }
         }
         
         return retVal;
      }
   }
   /// <summary>
   /// This class provides functionality to read characters from <see cref="StreamReaderWithResizableBuffer"/>.
   /// </summary>
   public sealed class StreamCharacterReader : PotentiallyAsyncReader<Char?, StreamReaderWithResizableBuffer>
   {
      private const Int32 IDLE = 0;
      private const Int32 BUSY = 1;

      private readonly Char[] _chars;
      private readonly Int32 _minChar;
      private Int32 _state;

      /// <summary>
      /// Creates new instance of <see cref="StreamCharacterReader"/> with given <see cref="IEncodingInfo"/>.
      /// </summary>
      /// <param name="encodingInfo">The <see cref="IEncodingInfo"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="encodingInfo"/> is <c>null</c>.</exception>
      public StreamCharacterReader(
         IEncodingInfo encodingInfo
         )
      {
         this.Encoding = ArgumentValidator.ValidateNotNull( nameof( encodingInfo ), encodingInfo );
         this._minChar = encodingInfo.MinCharByteCount;
         this._chars = new Char[2];
      }

      /// <summary>
      /// Gets the <see cref="IEncodingInfo"/> of this <see cref="StreamCharacterReader"/>.
      /// </summary>
      /// <value>The <see cref="IEncodingInfo"/> of this <see cref="StreamCharacterReader"/>.</value>
      public IEncodingInfo Encoding { get; }

      /// <summary>
      /// Tries to read next character from given <paramref name="stream"/>.
      /// </summary>
      /// <param name="stream">The <see cref="StreamReaderWithResizableBuffer"/> to read character from. The <see cref="StreamReaderWithResizableBuffer.TryReadMoreAsync(int)"/> method will be used.</param>
      /// <returns>A task which will return character read, or <c>null</c> if no more characters could be read from <paramref name="stream"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c>.</exception>
      /// <exception cref="InvalidOperationException">If this reader is currently busy with another read operation.</exception>
      public async ValueTask<Char?> TryReadNextAsync(
         StreamReaderWithResizableBuffer stream
      )
      {
         ArgumentValidator.ValidateNotNull( nameof( stream ), stream );
         if ( Interlocked.CompareExchange( ref this._state, BUSY, IDLE ) == IDLE )
         {
            try
            {
               var encoding = this.Encoding.Encoding;
               var auxArray = this._chars;
               var minChar = this._minChar;
               var arrayIndex = stream.ReadBytesCount;
               var charReadSuccessful = await stream.TryReadMoreAsync( minChar );
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
   /// Helper method to try to read next character from <typeparamref name="TSource"/>, or throw if no more characters can be read.
   /// </summary>
   /// <typeparam name="TValue">The type of values that this reader produces.</typeparam>
   /// <typeparam name="TSource">The type of the source of this <see cref="PotentiallyAsyncReader{TValue, TSource}"/>.</typeparam>
   /// <param name="reader">This <see cref="PotentiallyAsyncReader{TValue, TSource}"/>.</param>
   /// <param name="source">The <typeparamref name="TSource"/> to read from.</param>
   /// <returns>A task which will return character read.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="PotentiallyAsyncReader{TValue, TSource}"/> is <c>null</c>.</exception>
   /// <exception cref="EndOfStreamException">If no more characters could be read from the source.</exception>
   public static async ValueTask<TValue> ReadNextAsync<TValue, TSource>(
      this PotentiallyAsyncReader<TValue?, TSource> reader,
      TSource source
   )
      where TValue : struct
   {
      return await ArgumentValidator.ValidateNotNullReference( reader ).TryReadNextAsync( source ) ?? throw new EndOfStreamException();
   }
   
   /// <summary>
   /// Helper method to try to read next character from bound <see cref="PotentiallyAsyncReader{TValue}"/>, or throw if no more characters can be read.
   /// </summary>
   /// <typeparam name="TValue">The type of values that this reader produces.</typeparam>
   /// <param name="reader">This <see cref="PotentiallyAsyncReader{TValue}"/>.</param>
   /// <returns>A task which will return character read.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="PotentiallyAsyncReader{TValue}"/> is <c>null</c>.</exception>
   /// <exception cref="EndOfStreamException">If no more characters could be read from the source.</exception>
   public static async ValueTask<TValue> ReadNextAsync<TValue>(
      this PotentiallyAsyncReader<TValue?> reader
   )
      where TValue : struct
   {
      return await ArgumentValidator.ValidateNotNullReference( reader ).TryReadNextAsync( ) ?? throw new EndOfStreamException();
   }

   /// <summary>
   /// Helper method to try to read next value from <typeparamref name="TSource"/> until suitable value has been read, or values will end.
   /// </summary>
   /// <typeparam name="TValue">The type of values that this reader produces.</typeparam>
   /// <typeparam name="TSource">The type of the source of this <see cref="PotentiallyAsyncReader{TValue, TSource}"/>.</typeparam>
   /// <param name="reader">This <see cref="PotentiallyAsyncReader{TValue, TSource}"/>.</param>
   /// <param name="source">The <typeparamref name="TSource"/> to read from.</param>
   /// <param name="checker">Optional callback to check value. If it is supplied, this method will keep reading values until this callback returns <c>true</c>.</param>
   /// <returns>A task which will return value read.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="PotentiallyAsyncReader{TValue, TSource}"/> is <c>null</c>.</exception>
   public static async ValueTask<TValue?> TryReadNextAsync<TValue, TSource>(
      this PotentiallyAsyncReader<TValue?, TSource> reader,
      TSource source,
      Func<TValue, Boolean> checker
   )
      where TValue : struct
   {
      ArgumentValidator.ValidateNotNullReference( reader );
      TValue? charRead;
      do
      {
         charRead = await reader.TryReadNextAsync( source );
      } while ( charRead.HasValue && !(checker?.Invoke( charRead.Value ) ?? true) );
      
      return charRead;
   }
   
   /// <summary>
   /// Helper method to try to read next value from <typeparamref name="TSource"/>, or throw if no more values can be read.
   /// </summary>
   /// <typeparam name="TValue">The type of values that this reader produces.</typeparam>
   /// <typeparam name="TSource">The type of the source of this <see cref="PotentiallyAsyncReader{TValue, TSource}"/>.</typeparam>
   /// <param name="reader">This <see cref="PotentiallyAsyncReader{TValue, TSource}"/>.</param>
   /// <param name="source">The <typeparamref name="TSource"/> to read from.</param>
   /// <param name="checker">Optional callback to check value. If it is supplied, this method will keep reading values until this callback returns <c>true</c>.</param>
   /// <returns>A task which will return value read.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="PotentiallyAsyncReader{TValue, TSource}"/> is <c>null</c>.</exception>
   /// <exception cref="EndOfStreamException">If no more values could be read from the source.</exception>
   public static async ValueTask<TValue> ReadNextAsync<TValue, TSource>(
      this PotentiallyAsyncReader<TValue?, TSource> reader,
      TSource source,
      Func<TValue, Boolean> checker
   )
      where TValue : struct
   {
      return await ArgumentValidator.ValidateNotNullReference( reader ).TryReadNextAsync( source, checker ) ?? throw new EndOfStreamException();
   }

#endif

}