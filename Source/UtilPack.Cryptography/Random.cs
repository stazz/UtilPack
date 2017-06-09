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
using UtilPack.Cryptography;

namespace UtilPack.Cryptography
{
   public interface RandomGenerator : IDisposable
   {
      void AddSeedMaterial( Byte[] material, Int32 offset, Int32 count );
      void AddSeedMaterial( Int64 materialValue );
      void NextBytes( Byte[] array, Int32 offset, Int32 count );
   }

   public abstract class NotThreadsafeRandomGenerator : AbstractDisposable, RandomGenerator
   {

      protected NotThreadsafeRandomGenerator()
      {
         this.ArrayForLong = new Byte[sizeof( Int64 )];
      }

      public void AddSeedMaterial( Int64 materialValue )
      {
         this.ArrayForLong.WriteInt64LEToBytesNoRef( 0, materialValue );
         this.AddSeedMaterial( this.ArrayForLong, 0, sizeof( Int64 ) );
      }

      public abstract void AddSeedMaterial( Byte[] material, Int32 offset, Int32 count );
      public abstract void NextBytes( Byte[] array, Int32 offset, Int32 count );

      // Do *not* use this inside AddSeedMaterial method!
      protected Byte[] ArrayForLong { get; }
   }

   public class SecureRandom : Random, IDisposable
   {
      private readonly RandomGenerator _generator;
      private readonly Byte[] _intBytes;

      public SecureRandom( RandomGenerator generator )
         : base( 0 )
      {
         this._generator = ArgumentValidator.ValidateNotNull( "Generator", generator );
         this._intBytes = new Byte[sizeof( Int64 )];
      }

      public override Int32 Next()
      {
         // The spec is to return non-negative integer.
         return this.NextInt32() & Int32.MaxValue;
      }

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

      public override void NextBytes( Byte[] buffer )
      {
         this.NextBytes( buffer, 0, buffer.Length );
      }

      public void NextBytes( Byte[] buffer, Int32 offset, Int32 length )
      {
         this._generator.NextBytes( buffer, offset, length );
      }

      public override Double NextDouble()
      {
         const Double scale = Int64.MaxValue;
         return Convert.ToDouble( (UInt64) this.NextInt64() ) / scale;
      }

      public Int32 NextInt32()
      {
         this.NextBytes( this._intBytes, 0, sizeof( Int32 ) );
         return this._intBytes.ReadInt32BEFromBytesNoRef( 0 ); // Endianness shouldn't matter here since bytes are random.
      }

      public Int64 NextInt64()
      {
         this.NextBytes( this._intBytes, 0, sizeof( Int64 ) );
         return this._intBytes.ReadInt64BEFromBytesNoRef( 0 );
      }

      public void Dispose()
      {
         this._generator.DisposeSafely();
         Array.Clear( this._intBytes, 0, this._intBytes.Length );
      }

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

public static partial class E_UtilPack
{
   public static void AddSeedMaterial( this RandomGenerator generator, Byte[] array )
   {
      generator.AddSeedMaterial( array, 0, array.Length );
   }

   public static void NextBytes( this RandomGenerator generator, Byte[] array )
   {
      generator.NextBytes( array, 0, array.Length );
   }
}