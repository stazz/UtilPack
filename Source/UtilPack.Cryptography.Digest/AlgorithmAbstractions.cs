/*
 * Copyright 2016 Stanislav Muhametsin. All rights Reserved.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.Cryptography.Digest;

namespace UtilPack.Cryptography.Digest
{
   /// <summary>
   /// This interface provides API for all block-based digest-producing cryptographic algorithms.
   /// Typical usecase is to scan through data using <see cref="ProcessBlock"/> method, and finally produce a digest by <see cref="WriteDigest"/> method or <see cref="E_UtilPack.CreateDigest"/> extension method.
   /// </summary>
   public interface BlockDigestAlgorithm : IDisposable
   {
      /// <summary>
      /// Processes given amount of bytes.
      /// </summary>
      /// <param name="data">The array from where to read bytes.</param>
      /// <param name="offset">The offset in <paramref name="data"/> array where to start reading.</param>
      /// <param name="count">The amount of bytes to read from <paramref name="data"/> array.</param>
      void ProcessBlock( Byte[] data, Int32 offset, Int32 count );

      /// <summary>
      /// Signals that all data has been read and that a digest should be produced to given array.
      /// </summary>
      /// <param name="array">The byte array where to write digest to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing.</param>
      /// <seealso cref="DigestByteCount"/>
      void WriteDigest( Byte[] array, Int32 offset );

      /// <summary>
      /// Gets the amount of bytes that digests produced by this algorithm take.
      /// </summary>
      /// <value>The amount of bytes that digests produced by this algorithm take.</value>
      Int32 DigestByteCount { get; }

      /// <summary>
      /// Resets this digest algorithm instance to its initial state.
      /// </summary>
      void Reset();

      /// <summary>
      /// Gets the block size, in bytes, of this <see cref="BlockDigestAlgorithm"/>.
      /// </summary>
      /// <value>The block size, in bytes, of this <see cref="BlockDigestAlgorithm"/>.</value>
      Int32 BlockSize { get; }
   }

   internal sealed class ResizableArrayInstanceInfo : InstanceWithNextInfo<ResizableArrayInstanceInfo>
   {
      private ResizableArrayInstanceInfo _next;

      public ResizableArrayInstanceInfo( ResizableArray<Byte> array )
      {
         this.Array = ArgumentValidator.ValidateNotNull( nameof( array ), array );
      }

      public ResizableArray<Byte> Array { get; }

      public ResizableArrayInstanceInfo Next
      {
         get => this._next;
         set => Interlocked.Exchange( ref this._next, value );
      }
   }

   internal static class AlgorithmUtility
   {
      static AlgorithmUtility()
      {
         ArrayPool = new LocklessInstancePoolForClassesNoHeapAllocations<ResizableArrayInstanceInfo>();
      }

      public static LocklessInstancePoolForClassesNoHeapAllocations<ResizableArrayInstanceInfo> ArrayPool { get; }


   }

   internal sealed class HMACBlockDigestAlgorithm : BlockDigestAlgorithm
   {
      private const Byte INNER_PAD_BYTE = 0x36;
      private const Byte OUTER_PAD_BYTE = 0x5C;

      private const Int32 KEY_ORIGINAL = 0;
      private const Int32 KEY_XORRED_WITH_INNER = 1;
      private const Int32 KEY_XORRED_WITH_OUTER = 2;

      private readonly BlockDigestAlgorithm _actual;
      private readonly Byte[] _key;
      private Int32 _state;

      public HMACBlockDigestAlgorithm(
         BlockDigestAlgorithm actual,
         Byte[] key
         )
      {
         this._actual = ArgumentValidator.ValidateNotNull( nameof( actual ), actual );
         ArgumentValidator.ValidateNotNull( nameof( key ), key );

         // Check if the key is too long
         var blockSize = actual.BlockSize;
         if ( key.Length > blockSize )
         {
            // Compute hash of the key
            actual.ProcessBlock( key );
            key = new Byte[blockSize];
            actual.WriteDigest( key ); // There will be trailing zeroes, as needed
         }

         if ( key.Length < blockSize )
         {
            var newKey = new Byte[blockSize];
            key.CopyTo( newKey );
            key = newKey; // There will be trailing zeroes, as needed
         }

         this._key = key;
      }

      public Int32 DigestByteCount => this._actual.DigestByteCount;

      public Int32 BlockSize => this._actual.BlockSize;

      public void Dispose()
      {
         Array.Clear( this._key, 0, this._key.Length );
         _actual.Dispose();
      }

      public void ProcessBlock( Byte[] data, Int32 offset, Int32 count )
      {
         if ( this._state == KEY_ORIGINAL )
         {
            // Write inner padding
            this._state = KEY_XORRED_WITH_INNER;
            this.WriteInnerPadding( true );
         }
         this._actual.ProcessBlock( data, offset, count );
      }

      public void Reset()
      {
         // Remember to un-xor the key
         switch ( this._state )
         {
            case KEY_XORRED_WITH_INNER:
               this.WriteInnerPadding( false );
               break;
            case KEY_XORRED_WITH_OUTER:
               this.WriteOuterPadding( false );
               break;
         }

         this._state = KEY_ORIGINAL;
         this._actual.Reset();
      }

      public void WriteDigest( Byte[] array, Int32 offset )
      {
         if ( this._state == KEY_ORIGINAL )
         {
            // Write emtpy message
            this.ProcessBlock( Empty<Byte>.Array, 0, 0 );
         }

         // Compute digest of the actual message first
         this._actual.WriteDigest( array, offset );

         // Then compute the digest for outer padding concatenated with digest
         this._state = KEY_XORRED_WITH_OUTER;
         this.WriteOuterPadding( true );
         this._actual.ProcessBlock( array, offset, this._actual.DigestByteCount );
         this._actual.WriteDigest( array, offset );

         // And now we're done.
         this.Reset();
      }

      private void WriteInnerPadding( Boolean processByActual )
      {
         // We can write directly into this._key - no need to allocate arrays
         for ( var i = 0; i < this._key.Length; ++i )
         {
            this._key[i] ^= INNER_PAD_BYTE;
         }

         if ( processByActual )
         {
            this._actual.ProcessBlock( this._key );
         }
      }

      private void WriteOuterPadding( Boolean processByActual )
      {
         // We can write directly into this._key - no need to allocate arrays
         // Remember that our key is XORred with inner padding by WriteInnerPadding method
         var mask = processByActual ?
            (Byte) ( INNER_PAD_BYTE ^ OUTER_PAD_BYTE ) :
            OUTER_PAD_BYTE;
         for ( var i = 0; i < this._key.Length; ++i )
         {
            this._key[i] ^= mask;
         }
         if ( processByActual )
         {
            this._actual.ProcessBlock( this._key );
         }
      }
   }
}

/// <summary>
/// This class contains extensions methods defined in UtilPack products.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// Helper method to compute digest over the whole contents of given byte array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="array">The byte data of which to compute digest from.</param>
   /// <returns>The digest produced by this <see cref="BlockDigestAlgorithm"/>.</returns>
   public static Byte[] ComputeDigest( this BlockDigestAlgorithm transform, Byte[] array )
   {
      return transform.ComputeDigest( array, 0, array?.Length ?? 0 );
   }

   /// <summary>
   /// Helper method to compute digest when all the data has already been read to given array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="array">The data. Reading will start at offset <c>0</c>.</param>
   /// <param name="count">The amount of bytes to read from <paramref name="array"/>.</param>
   /// <returns></returns>
   public static Byte[] ComputeDigest( this BlockDigestAlgorithm transform, Byte[] array, Int32 count )
   {
      return transform.ComputeDigest( array, 0, count );
   }

   /// <summary>
   /// Helper method to compute digest when all the data has already been read to given array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="array">The data.</param>
   /// <param name="offset">The offset in <paramref name="array"/> where to start reading.</param>
   /// <param name="count">The amount of bytes to read from <paramref name="array"/>.</param>
   /// <returns>The digest produced by this <see cref="BlockDigestAlgorithm"/>.</returns>
   public static Byte[] ComputeDigest( this BlockDigestAlgorithm transform, Byte[] array, Int32 offset, Int32 count )
   {
      transform.ProcessBlock( array, offset, count );
      return transform.CreateDigest();
   }

   /// <summary>
   /// Helper method to compute hash from the data of specific stream.
   /// </summary>
   /// <param name="source">The source stream containing the data to be hashed.</param>
   /// <param name="hash">The <see cref="BlockDigestAlgorithm"/> to use.</param>
   /// <param name="buffer">The buffer to use when reading data from <paramref name="source"/>.</param>
   /// <param name="amount">The amount of bytes to read from <paramref name="source"/>.</param>
   /// <param name="token">The cancellation token to use.</param>
   /// <exception cref="System.IO.EndOfStreamException">If the <paramref name="source"/> ends before given <paramref name="amount"/> of bytes is read.</exception>
   public static async Task CopyStreamPartAsync( this BlockDigestAlgorithm hash, System.IO.Stream source, Byte[] buffer, Int64 amount, CancellationToken token = default( CancellationToken ) )
   {
      ArgumentValidator.ValidateNotNull( "Stream", source );
      while ( amount > 0 )
      {
         var amountOfRead = await source.ReadAsync( buffer, 0, (Int32) Math.Min( buffer.Length, amount ), token );
         if ( amountOfRead <= 0 )
         {
            throw new System.IO.EndOfStreamException( "Source stream ended before copying of " + amount + " byte" + ( amount > 1 ? "s" : "" ) + " could be completed." );
         }
         hash.ProcessBlock( buffer, 0, amountOfRead );
         amount -= (UInt32) amountOfRead;
      }
   }

   /// <summary>
   /// Helper method to create a new byte array of required size and call <see cref="BlockDigestAlgorithm.WriteDigest"/>.
   /// </summary>
   /// <param name="algorithm">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <returns>A digest created by this <see cref="BlockDigestAlgorithm"/>.</returns>
   public static Byte[] CreateDigest( this BlockDigestAlgorithm algorithm )
   {
      var retVal = new Byte[algorithm.DigestByteCount];
      algorithm.WriteDigest( retVal, 0 );
      return retVal;
   }

   /// <summary>
   /// Helper method to process whole contents of given byte array.
   /// </summary>
   /// <param name="algorithm">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="block">The byte array to process.</param>
   public static void ProcessBlock( this BlockDigestAlgorithm algorithm, Byte[] block )
   {
      algorithm.ProcessBlock( block, 0, block.Length );
   }

   /// <summary>
   /// Helper method to write digest to given array starting at index <c>0.</c>.
   /// </summary>
   /// <param name="algorithm">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="array">The byte array to write digest to. Writing starts at index <c>0</c>.</param>
   public static void WriteDigest( this BlockDigestAlgorithm algorithm, Byte[] array )
   {
      algorithm.WriteDigest( array, 0 );
   }

   /// <summary>
   /// Creates a new instance of <see cref="BlockDigestAlgorithm"/> which wraps this <see cref="BlockDigestAlgorithm"/>, and adds HMAC support.
   /// </summary>
   /// <param name="algorithm">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="key">The key to use in HMAC.</param>
   /// <returns>A new instance of <see cref="BlockDigestAlgorithm"/> with HMAC support.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
   /// <remarks>
   /// The returned <see cref="BlockDigestAlgorithm"/> will assume the given <paramref name="key"/> as its own, and will write to it. If that is undesirable, please pass a copy of an array as <paramref name="key"/>.
   /// </remarks>
   public static BlockDigestAlgorithm CreateHMAC( this BlockDigestAlgorithm algorithm, Byte[] key )
   {
      return new HMACBlockDigestAlgorithm(
         ArgumentValidator.ValidateNotNullReference( algorithm ),
         key
         );
   }
}