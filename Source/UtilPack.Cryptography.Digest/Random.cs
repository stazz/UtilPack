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
using System.Threading;

namespace UtilPack.Cryptography.Digest
{
   /// <summary>
   /// This class specializes <see cref="NotThreadsafeRandomGenerator"/> and uses <see cref="BlockDigestAlgorithm"/> to mutate its state when producing random data.
   /// </summary>
   /// <seealso cref="CreateAndSeedWithDefaultLogic"/>
   public class DigestBasedRandomGenerator : NotThreadsafeRandomGenerator
   {
      private const Int32 DEFAULT_SEED_CYCLE_COUNT = 10;

      private readonly Int32 _seedCycleCount;
      private readonly Byte[] _seed;
      private readonly Byte[] _state;

      private readonly Boolean _skipDisposeAlgorithm;

      private Int64 _stateCounter;
      private Int64 _seedCounter;

      /// <summary>
      /// Creates a new instance of <see cref="DigestBasedRandomGenerator"/> with given parameters.
      /// </summary>
      /// <param name="algorithm">The <see cref="DigestAlgorithm"/> to use when producing random data.</param>
      /// <param name="seedCycleCount">How often to re-seed the state. Minimum value will be <c>1</c> if if <c>0</c> or less is specified.</param>
      /// <param name="skipDisposeAlgorithm">Optional parameter controlling whether this <see cref="DigestBasedRandomGenerator"/> will, when disposed, dispose also the given <paramref name="algorithm"/>.</param>
      /// <seealso cref="CreateAndSeedWithDefaultLogic"/>
      /// <exception cref="ArgumentNullException">If <paramref name="algorithm"/> is <c>null</c>.</exception>
      public DigestBasedRandomGenerator(
         DigestAlgorithm algorithm,
         Int32 seedCycleCount = 10,
         Boolean skipDisposeAlgorithm = false
         )
      {
         this.Algorithm = ArgumentValidator.ValidateNotNull( "Algorithm", algorithm );
         this._skipDisposeAlgorithm = skipDisposeAlgorithm;
         this._seed = new Byte[this.Algorithm.DigestByteCount];
         this._state = new Byte[this.Algorithm.DigestByteCount];

         this._stateCounter = 0; // When state is first time generated, the state counter will be increased to 1
         this._seedCounter = 0;
         this._seedCycleCount = Math.Max( 1, seedCycleCount );
      }

      /// <summary>
      /// Uses <see cref="DigestAlgorithm.ProcessBlock"/> and <see cref="DigestAlgorithm.WriteDigest"/> methods to mutate the state of this <see cref="DigestBasedRandomGenerator"/>.
      /// </summary>
      /// <param name="material">The random data.</param>
      /// <param name="offset">The offset in <paramref name="material"/> array where to start reading.</param>
      /// <param name="count">The amount of bytes to read from <paramref name="material"/> array.</param>
      public override void AddSeedMaterial( Byte[] material, Int32 offset, Int32 count )
      {
         ArgumentValidator.ValidateNotNull( nameof( material ), material );
         material.CheckArrayArguments( offset, count, false );
         this.Algorithm.ProcessBlock( material, offset, count );
         this.Algorithm.ProcessBlock( this._seed );
         this.Algorithm.WriteDigest( this._seed );
      }

      /// <summary>
      /// Keeps computing digests using <see cref="DigestAlgorithm.ProcessBlock"/> and <see cref="DigestAlgorithm.WriteDigest"/> methods and writing them to given byte array until specified amount of bytes has been written to the array.
      /// </summary>
      /// <param name="array">The array where to write random data to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing.</param>
      /// <param name="count">The amount of bytes to write to <paramref name="array"/>.</param>
      public override void NextBytes( Byte[] array, Int32 offset, Int32 count )
      {
         ArgumentValidator.ValidateNotNull( nameof( array ), array );
         array.CheckArrayArguments( offset, count, false );
         if ( count > 0 )
         {
            var state = this._state;
            do
            {
               this.PopulateState();
               Array.Copy( state, 0, array, offset, Math.Min( count, state.Length ) );
               offset += state.Length;
            } while ( count - offset > 0 );
         }
      }

      /// <summary>
      /// Disposes underlying <see cref="DigestAlgorithm"/> and clears internal state.
      /// </summary>
      /// <param name="disposing">Whether disposing from <see cref="IDisposable.Dispose"/> method.</param>
      protected override void Dispose( Boolean disposing )
      {
         if ( disposing )
         {
            if ( !this._skipDisposeAlgorithm )
            {
               this.Algorithm.DisposeSafely();
            }

            this._state.Clear();
            this._seed.Clear();
         }
      }

      /// <summary>
      /// Gets the <see cref="DigestAlgorithm"/> that this <see cref="DigestBasedRandomGenerator"/> uses.
      /// </summary>
      /// <value>The <see cref="DigestAlgorithm"/> that this <see cref="DigestBasedRandomGenerator"/> uses.</value>
      protected DigestAlgorithm Algorithm { get; }

      private void PopulateState()
      {
         Int64 newStateCounter;
         this.AlgorithmProcessInt64( ( newStateCounter = Interlocked.Increment( ref this._stateCounter ) ) );
         this.Algorithm.ProcessBlock( this._state );
         this.Algorithm.ProcessBlock( this._seed );
         this.Algorithm.WriteDigest( this._state );

         if ( newStateCounter % this._seedCycleCount == 0 )
         {
            this.PopulateSeed();
         }
      }

      private void PopulateSeed()
      {
         this.Algorithm.ProcessBlock( this._seed );
         this.AlgorithmProcessInt64( Interlocked.Increment( ref this._seedCounter ) );
         this.Algorithm.WriteDigest( this._seed );
      }

      private void AlgorithmProcessInt64( Int64 val )
      {
         this.ArrayForLong.WriteInt64LEToBytesNoRef( 0, val );
         this.Algorithm.ProcessBlock( this.ArrayForLong );
      }

      /// <summary>
      /// This is convenience method to create new <see cref="DigestBasedRandomGenerator"/> and seed it with default, secure logic.
      /// </summary>
      /// <param name="algorithm">The algorithm that returned <see cref="DigestBasedRandomGenerator"/> should use.</param>
      /// <param name="seedCycleCount">How often to re-seed the state of returned <see cref="DigestBasedRandomGenerator"/>.</param>
      /// <param name="skipDisposeAlgorithm">Optional parameter controlling whether the <see cref="DigestBasedRandomGenerator"/> will, when disposed, dispose also the given <paramref name="algorithm"/>.</param>
      /// <returns>A new instance of <see cref="DigestBasedRandomGenerator"/> with given parameters.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="algorithm"/> is <c>null</c>.</exception>
      public static DigestBasedRandomGenerator CreateAndSeedWithDefaultLogic(
         BlockDigestAlgorithm algorithm,
         Int32 seedCycleCount = DEFAULT_SEED_CYCLE_COUNT,
         Boolean skipDisposeAlgorithm = false
         )
      {
         var retVal = new DigestBasedRandomGenerator( algorithm, seedCycleCount, skipDisposeAlgorithm );
         // Use Guid as random source (should be version 4)
         retVal.AddSeedMaterial( Guid.NewGuid().ToByteArray() );

         // Use current ticks
         retVal.AddSeedMaterial( DateTime.Now.Ticks );

         // Use Guid again
         retVal.AddSeedMaterial( Guid.NewGuid().ToByteArray() );
         return retVal;
      }

      /// <summary>
      /// Creates default base64 lookup character array, and reorders it using <see cref="DigestBasedRandomGenerator"/> with given parameters.
      /// </summary>
      /// <param name="seed">The seed material to <see cref="DigestBasedRandomGenerator"/>, as <see cref="Int64"/>.</param>
      /// <param name="algorithm">The <see cref="DigestAlgorithm"/> to use. If <c>null</c>, then <see cref="SHA512"/> will be used.</param>
      /// <param name="seedCycleCount">How often the seed will be re-digested.</param>
      /// <param name="isURLSafe">Whether to use url-safe base64 characters.</param>
      /// <returns>A base64 lookup character array, shuffled using the given <paramref name="seed"/>.</returns>
      /// <seealso cref="StringConversions.EncodeBinary(byte[], char[])"/>
      /// <seealso cref="UtilPackExtensions.Shuffle{T}(T[], Random)"/>
      public static Char[] ShuffleBase64CharactersFromSeed(
         Int64 seed,
         DigestAlgorithm algorithm = null,
         Int32 seedCycleCount = 10,
         Boolean isURLSafe = true
         )
      {
         var chars = StringConversions.CreateBase64EncodeLookupTable( isURLSafe );
         ShuffleBinaryEncodingCharactersFromSeed( chars, seed, algorithm: algorithm, seedCycleCount: seedCycleCount );
         return chars;
      }

      /// <summary>
      /// Using the given lookup character array, reorders it using <see cref="DigestBasedRandomGenerator"/> with given parameters.
      /// </summary>
      /// <param name="chars">The lookup character array.</param>
      /// <param name="seed">The seed material to <see cref="DigestBasedRandomGenerator"/>, as <see cref="Int64"/>.</param>
      /// <param name="algorithm">The <see cref="DigestAlgorithm"/> to use. If <c>null</c>, then <see cref="SHA512"/> will be used.</param>
      /// <param name="seedCycleCount">How often the seed will be re-digested.</param>
      /// <seealso cref="StringConversions.EncodeBinary(byte[], char[])"/>
      /// <seealso cref="UtilPackExtensions.Shuffle{T}(T[], Random)"/>
      public static void ShuffleBinaryEncodingCharactersFromSeed(
         Char[] chars,
         Int64 seed,
         DigestAlgorithm algorithm = null,
         Int32 seedCycleCount = 10
         )
      {
         using ( var rng = new DigestBasedRandomGenerator( algorithm ?? new SHA512(), seedCycleCount: seedCycleCount, skipDisposeAlgorithm: false ) )
         {
            rng.AddSeedMaterial( seed );
            using ( var secRandom = new SecureRandom( rng ) )
            {
               chars.Shuffle( secRandom );
            }
         }
      }

      /// <summary>
      /// Creates default base64 lookup character array, and reorders it using <see cref="DigestBasedRandomGenerator"/> with given parameters.
      /// </summary>
      /// <param name="seed">The seed material to <see cref="DigestBasedRandomGenerator"/>, as <see cref="Byte"/> array.</param>
      /// <param name="algorithm">The <see cref="DigestAlgorithm"/> to use. If <c>null</c>, then <see cref="SHA512"/> will be used.</param>
      /// <param name="seedCycleCount">How often the seed will be re-digested.</param>
      /// <param name="isURLSafe">Whether to use url-safe base64 characters.</param>
      /// <returns>A base64 lookup character array, shuffled using the given <paramref name="seed"/>.</returns>
      /// <seealso cref="StringConversions.EncodeBinary(byte[], char[])"/>
      public static Char[] ShuffleBase64CharactersFromSeed(
         Byte[] seed,
         DigestAlgorithm algorithm = null,
         Int32 seedCycleCount = 10,
         Boolean isURLSafe = true
         )
      {
         var chars = StringConversions.CreateBase64EncodeLookupTable( isURLSafe );
         ShuffleBinaryEncodingCharactersFromSeed( chars, seed, algorithm: algorithm, seedCycleCount: seedCycleCount );
         return chars;
      }

      /// <summary>
      /// Using the given lookup character array, reorders it using <see cref="DigestBasedRandomGenerator"/> with given parameters.
      /// </summary>
      /// <param name="chars">The lookup character array.</param>
      /// <param name="seed">The seed material to <see cref="DigestBasedRandomGenerator"/>, as <see cref="Byte"/> array.</param>
      /// <param name="algorithm">The <see cref="DigestAlgorithm"/> to use. If <c>null</c>, then <see cref="SHA512"/> will be used.</param>
      /// <param name="seedCycleCount">How often the seed will be re-digested.</param>
      /// <seealso cref="StringConversions.EncodeBinary(byte[], char[])"/>
      /// <seealso cref="UtilPackExtensions.Shuffle{T}(T[], Random)"/>
      public static void ShuffleBinaryEncodingCharactersFromSeed(
         Char[] chars,
         Byte[] seed,
         DigestAlgorithm algorithm = null,
         Int32 seedCycleCount = 10
         )
      {
         using ( var rng = new DigestBasedRandomGenerator( algorithm ?? new SHA512(), seedCycleCount: seedCycleCount, skipDisposeAlgorithm: false ) )
         {
            rng.AddSeedMaterial( seed );
            using ( var secRandom = new SecureRandom( rng ) )
            {
               chars.Shuffle( secRandom );
            }
         }
      }

   }
}