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
using FluentCryptography.Abstractions;
using System;
using UtilPack;

namespace FluentCryptography.Abstractions
{
   /// <summary>
   /// This interface provides API for cryptographic random number generators.
   /// </summary>
   public interface RandomGenerator : IDisposable
   {
      /// <summary>
      /// Adds seed material as a number of bytes to the current state of random generator.
      /// </summary>
      /// <param name="material">The seed material array.</param>
      /// <param name="offset">The offset in <paramref name="material"/> where to start to read for bytes.</param>
      /// <param name="count">How many bytes to read from <paramref name="material"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="material"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="count"/> is less than <c>0</c>.</exception>
      /// <exception cref="ArgumentException">If <paramref name="offset"/> + <paramref name="count"/> is greater than array length.</exception>
      void AddSeedMaterial( Byte[] material, Int32 offset, Int32 count );

      /// <summary>
      /// Adds seed material as 64-bit number to the current state of random generator.
      /// </summary>
      /// <param name="materialValue">The randomess to add, as a 64-bit number.</param>
      void AddSeedMaterial( Int64 materialValue );

      /// <summary>
      /// Generates next random bytes into given array.
      /// </summary>
      /// <param name="array">The array to generate random bytes to.</param>
      /// <param name="offset">The offset where to start generating.</param>
      /// <param name="count">Amount of bytes to generate.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="count"/> is less than <c>0</c>.</exception>
      /// <exception cref="ArgumentException">If <paramref name="offset"/> + <paramref name="count"/> is greater than array length.</exception>
      void NextBytes( Byte[] array, Int32 offset, Int32 count );
   }

   /// <summary>
   /// This class provides skeleton implementation for <see cref="RandomGenerator"/> which also makes this class not safe to use concurrently.
   /// </summary>
   public abstract class NotThreadsafeRandomGenerator : AbstractDisposable, RandomGenerator
   {
      /// <summary>
      /// Creates new instance of <see cref="NotThreadsafeRandomGenerator"/>.
      /// </summary>
      protected NotThreadsafeRandomGenerator()
      {
         this.ArrayForLong = new Byte[sizeof( Int64 )];
      }

      /// <inheritdoc />
      public void AddSeedMaterial( Int64 materialValue )
      {
         this.ArrayForLong.WriteInt64LEToBytesNoRef( 0, materialValue );
         this.AddSeedMaterial( this.ArrayForLong, 0, sizeof( Int64 ) );
      }

      /// <inheritdoc />
      public abstract void AddSeedMaterial( Byte[] material, Int32 offset, Int32 count );

      /// <inheritdoc />
      public abstract void NextBytes( Byte[] array, Int32 offset, Int32 count );

      /// <summary>
      /// Gets temporary array where integers (32-bit and 64-bit ones) can be written during random generation process.
      /// </summary>
      /// <value>The temporary array where integers (32-bit and 64-bit ones) can be written during random generation process.</value>
      protected Byte[] ArrayForLong { get; }
   }

   /// <summary>
   /// This class extends <see cref="Random"/> in order to provide cryptographical random number generation to APIs which accept <see cref="Random"/> class.
   /// It uses <see cref="RandomGenerator"/> underneath.
   /// </summary>
   public class SecureRandom : Random, IDisposable
   {
      private readonly RandomGenerator _generator;
      private readonly Byte[] _intBytes;

      /// <summary>
      /// Creates new instance of <see cref="SecureRandom"/> with given <see cref="RandomGenerator"/>.
      /// </summary>
      /// <param name="generator">The <see cref="RandomGenerator"/> to use to generate random numbers.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="generator"/> is <c>null</c>.</exception>
      public SecureRandom( RandomGenerator generator )
         : base( 0 )
      {
         this._generator = ArgumentValidator.ValidateNotNull( "Generator", generator );
         this._intBytes = new Byte[sizeof( Int64 )];
      }

      /// <inheritdoc/>
      public override Int32 Next()
      {
         // The spec is to return non-negative integer.
         return this.NextInt32() & Int32.MaxValue;
      }

      /// <inheritdoc/>
      public override Int32 Next( Int32 maxValue )
      {
         // The spec is to return non-negative integer lesser than maxValue
         Int32 retVal;
         if ( maxValue < 2 )
         {
            if ( maxValue < 0 )
            {
               throw new ArgumentOutOfRangeException( "maxValue", "should be at least zero." );
            }
            else
            {
               // Return 0 for 0 and 1 max value
               retVal = 0;
            }
         }
         else
         {
            retVal = ( this.NextInt32() & Int32.MaxValue ) % maxValue;
         }

         return retVal;
      }

      /// <inheritdoc/>
      public override Int32 Next( Int32 minValue, Int32 maxValue )
      {
         // Here both minValue and maxValue can be negative
         Int32 retVal;
         if ( maxValue < minValue )
         {
            throw new ArgumentException( "Max value should be at least as min value." );
         }
         else if ( maxValue == minValue || maxValue == minValue + 1 )
         {
            retVal = minValue;
         }
         else
         {
            // Diff will be always at least 2, since previous if-conditions filter out all other options.
            var diff = (Int64) maxValue - (Int64) minValue;
            if ( diff <= Int32.MaxValue )
            {
               retVal = minValue + this.Next( (Int32) diff );
            }
            else
            {
               retVal = (Int32) ( (Int64) minValue + ( this.NextInt64() & Int64.MaxValue ) % diff );
            }
         }
         return retVal;
      }

      /// <inheritdoc/>
      public override void NextBytes( Byte[] buffer )
      {
         this.NextBytes( buffer, 0, buffer.Length );
      }

      /// <inheritdoc/>
      public void NextBytes( Byte[] buffer, Int32 offset, Int32 length )
      {
         this._generator.NextBytes( buffer, offset, length );
      }

      /// <inheritdoc/>
      public override Double NextDouble()
      {
         const Double scale = Int64.MaxValue;
         return Convert.ToDouble( (UInt64) this.NextInt64() ) / scale;
      }

      /// <summary>
      /// Generates new 32-bit integer within whole range of <see cref="Int32"/>, including negative values.
      /// </summary>
      /// <returns>New 32-bit integer within whole range of <see cref="Int32"/>, including negative values.</returns>
      public Int32 NextInt32()
      {
         this.NextBytes( this._intBytes, 0, sizeof( Int32 ) );
         return this._intBytes.ReadInt32BEFromBytesNoRef( 0 ); // Endianness shouldn't matter here since bytes are random.
      }

      /// <summary>
      /// Generates new 64-bit integer within whole range of <see cref="Int64"/>, including negative values.
      /// </summary>
      /// <returns>New 642-bit integer within whole range of <see cref="Int64"/>, including negative values.</returns>
      public Int64 NextInt64()
      {
         this.NextBytes( this._intBytes, 0, sizeof( Int64 ) );
         return this._intBytes.ReadInt64BEFromBytesNoRef( 0 );
      }

      /// <summary>
      /// Disposes the underlying <see cref="RandomGenerator"/> and clears the state of this generator.
      /// </summary>
      public void Dispose()
      {
         this._generator.DisposeSafely();
         this._intBytes.Clear();
      }

      /// <summary>
      /// Clears state of this <see cref="SecureRandom"/> and underlying <see cref="RandomGenerator"/>.
      /// </summary>
      ~SecureRandom()
      {
         try
         {
            this.Dispose();
         }
         catch
         {
            // Ignore
         }
      }
   }
}

/// <summary>
/// This class contains extensions methods defined in UtilPack products.
/// </summary>
public static partial class E_FluentCryptography
{
   /// <summary>
   /// Helper method to add whole contents of given byte array as seed material to this <see cref="RandomGenerator"/>.
   /// </summary>
   /// <param name="generator">This <see cref="RandomGenerator"/>.</param>
   /// <param name="array">The seed material.</param>
   /// <exception cref="NullReferenceException">If this <see cref="RandomGenerator"/> is <c>null</c>.</exception>
   public static void AddSeedMaterial( this RandomGenerator generator, Byte[] array )
   {
      generator.AddSeedMaterial( array, 0, array.Length );
   }

   /// <summary>
   /// Helper method to populate whole contents of given array with random data.
   /// </summary>
   /// <param name="generator">This <see cref="RandomGenerator"/>.</param>
   /// <param name="array">The array where to write data. All of the contents of the array will be overwritten.</param>
   /// <exception cref="NullReferenceException">If this <see cref="RandomGenerator"/> is <c>null</c>.</exception>
   public static void NextBytes( this RandomGenerator generator, Byte[] array )
   {
      generator.NextBytes( array, 0, array.Length );
   }
}