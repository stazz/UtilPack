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
using FluentCryptography.Digest;
using System;
using System.Collections.Generic;
using System.Text;
using UtilPack;

namespace FluentCryptography.Digest
{
   internal sealed class HMACBlockDigestAlgorithm : BlockDigestAlgorithm
   {
      private const Byte INNER_PAD_BYTE = 0x36;
      private const Byte OUTER_PAD_BYTE = 0x5C;

      private const Int32 KEY_ORIGINAL = 0;
      private const Int32 KEY_XORRED_WITH_INNER = 1;
      private const Int32 KEY_XORRED_WITH_OUTER = 2;

      private readonly BlockDigestAlgorithm _actual;
      private readonly Byte[] _key;
      private readonly Int32 _keyOffset;
      private readonly Boolean _skipDisposeAlgorithm;
      private readonly Boolean _skipZeroingKeyOnDispose;
      private Int32 _state;

      public HMACBlockDigestAlgorithm(
         BlockDigestAlgorithm actual,
         Byte[] key,
         Int32 keyOffset,
         Int32 keyLength,
         Boolean skipDisposeAlgorithm,
         Boolean skipZeroingKeyOnDispose
         )
      {
         this._actual = ArgumentValidator.ValidateNotNull( nameof( actual ), actual );
         ArgumentValidator.ValidateNotNull( nameof( key ), key );

         // Check if the key is too long
         var blockSize = actual.BlockSize;
         this._skipDisposeAlgorithm = skipDisposeAlgorithm;
         this._skipZeroingKeyOnDispose = skipZeroingKeyOnDispose;
         if ( keyLength > blockSize )
         {
            // Compute hash of the key
            actual.ProcessBlock( key );
            actual.WriteDigest( key, keyOffset );
            // Explicitly set trailing zeroes
            key.Clear( keyOffset + actual.DigestByteCount, keyLength - actual.DigestByteCount );
         }
         else if ( keyLength < blockSize )
         {
            if ( key.Length < keyOffset + blockSize )
            {
               var newKey = new Byte[blockSize];
               key.CopyTo( newKey );
               keyOffset = 0;
               key = newKey; // There will be trailing zeroes, as a new array was allocated
               this._skipZeroingKeyOnDispose = false; // Always zero out, since we created the array here.
            }
            else
            {
               // Explicitly set trailing zeroes
               key.Clear( keyOffset + keyLength, blockSize - keyLength );
            }
         }

         this._key = key;
         this._keyOffset = keyOffset;
      }

      public Int32 DigestByteCount => this._actual.DigestByteCount;

      public Int32 BlockSize => this._actual.BlockSize;

      public void Dispose()
      {
         if ( !this._skipZeroingKeyOnDispose )
         {
            this._key.Clear( this._keyOffset, this._actual.BlockSize );
         }

         if ( this._skipDisposeAlgorithm )
         {
            this._actual.Reset();
         }
         else
         {
            this._actual.Dispose();
         }

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
         var count = this._actual.BlockSize;
         for ( var i = 0; i < count; ++i )
         {
            this._key[i + this._keyOffset] ^= INNER_PAD_BYTE;
         }

         if ( processByActual )
         {
            this._actual.ProcessBlock( this._key, this._keyOffset, count );
         }
      }

      private void WriteOuterPadding( Boolean processByActual )
      {
         // We can write directly into this._key - no need to allocate arrays
         // Remember that our key is XORred with inner padding by WriteInnerPadding method
         var mask = processByActual ?
            (Byte) ( INNER_PAD_BYTE ^ OUTER_PAD_BYTE ) :
            OUTER_PAD_BYTE;
         var count = this._actual.BlockSize;
         for ( var i = 0; i < count; ++i )
         {
            this._key[i + this._keyOffset] ^= mask;
         }
         if ( processByActual )
         {
            this._actual.ProcessBlock( this._key, this._keyOffset, count );
         }
      }
   }
}

public static partial class E_UtilPack
{
   /// <summary>
   /// Creates a new instance of <see cref="BlockDigestAlgorithm"/> which wraps this <see cref="BlockDigestAlgorithm"/>, and adds HMAC support.
   /// </summary>
   /// <param name="algorithm">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="key">The key to use in HMAC.</param>
   /// <param name="skipDisposeAlgorithm">Whether to skip disposing this <see cref="BlockDigestAlgorithm"/> when returned HMAC <see cref="BlockDigestAlgorithm"/> is disposed.</param>
   /// <param name="skipZeroingOutKey">Whether to skip zeroing out the key array when the returned HMAC <see cref="BlockDigestAlgorithm"/> is disposed.</param>
   /// <returns>A new instance of <see cref="BlockDigestAlgorithm"/> with HMAC support.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
   /// <remarks>
   /// The returned <see cref="BlockDigestAlgorithm"/> will assume the given <paramref name="key"/> as its own, and will write to it. If that is undesirable, please pass a copy of an array as <paramref name="key"/>.
   /// </remarks>
   public static BlockDigestAlgorithm CreateHMAC(
      this BlockDigestAlgorithm algorithm,
      Byte[] key,
      Boolean skipDisposeAlgorithm = false,
      Boolean skipZeroingOutKey = false
      )
      => algorithm.CreateHMAC( key, 0, key?.Length ?? 0, skipDisposeAlgorithm: skipDisposeAlgorithm, skipZeroingOutKey: skipZeroingOutKey );

   /// <summary>
   /// Creates a new instance of <see cref="BlockDigestAlgorithm"/> which wraps this <see cref="BlockDigestAlgorithm"/>, and adds HMAC support.
   /// </summary>
   /// <param name="algorithm">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="key">The key to use in HMAC.</param>
   /// <param name="offset">The offset in <paramref name="key"/> array where the key content starts.</param>
   /// <param name="skipDisposeAlgorithm">Whether to skip disposing this <see cref="BlockDigestAlgorithm"/> when returned HMAC <see cref="BlockDigestAlgorithm"/> is disposed.</param>
   /// <param name="skipZeroingOutKey">Whether to skip zeroing out the key array when the returned HMAC <see cref="BlockDigestAlgorithm"/> is disposed.</param>
   /// <returns>A new instance of <see cref="BlockDigestAlgorithm"/> with HMAC support.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
   /// <remarks>
   /// The returned <see cref="BlockDigestAlgorithm"/> will assume the given <paramref name="key"/> as its own, and will write to it. If that is undesirable, please pass a copy of an array as <paramref name="key"/>.
   /// </remarks>
   public static BlockDigestAlgorithm CreateHMAC(
      this BlockDigestAlgorithm algorithm,
      Byte[] key,
      Int32 offset,
      Boolean skipDisposeAlgorithm = false,
      Boolean skipZeroingOutKey = false
      )
      => algorithm.CreateHMAC( key, offset, key?.Length ?? 0 - offset, skipDisposeAlgorithm: skipDisposeAlgorithm, skipZeroingOutKey: skipZeroingOutKey );

   /// <summary>
   /// Creates a new instance of <see cref="BlockDigestAlgorithm"/> which wraps this <see cref="BlockDigestAlgorithm"/>, and adds HMAC support.
   /// </summary>
   /// <param name="algorithm">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="key">The key to use in HMAC.</param>
   /// <param name="offset">The offset in <paramref name="key"/> array where the key content starts.</param>
   /// <param name="count">The amount of bytes in <paramref name="key"/> which are key bytes.</param>
   /// <param name="skipDisposeAlgorithm">Whether to skip disposing this <see cref="BlockDigestAlgorithm"/> when returned HMAC <see cref="BlockDigestAlgorithm"/> is disposed.</param>
   /// <param name="skipZeroingOutKey">Whether to skip zeroing out the key array when the returned HMAC <see cref="BlockDigestAlgorithm"/> is disposed.</param>
   /// <returns>A new instance of <see cref="BlockDigestAlgorithm"/> with HMAC support.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="key"/> is <c>null</c>.</exception>
   /// <remarks>
   /// The returned <see cref="BlockDigestAlgorithm"/> will assume the given <paramref name="key"/> as its own, and will write to it. If that is undesirable, please pass a copy of an array as <paramref name="key"/>.
   /// </remarks>
   public static BlockDigestAlgorithm CreateHMAC(
      this BlockDigestAlgorithm algorithm,
      Byte[] key,
      Int32 offset,
      Int32 count,
      Boolean skipDisposeAlgorithm = false,
      Boolean skipZeroingOutKey = false
      )
   {
      return new HMACBlockDigestAlgorithm(
         ArgumentValidator.ValidateNotNullReference( algorithm ),
         key,
         offset,
         count,
         skipDisposeAlgorithm,
         skipZeroingOutKey
         );
   }
}