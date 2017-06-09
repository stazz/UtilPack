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
   public class DigestBasedRandomGenerator : NotThreadsafeRandomGenerator
   {
      private const Int32 DEFAULT_SEED_CYCLE_COUNT = 10;

      private readonly Int32 _seedCycleCount;
      private readonly Byte[] _seed;
      private readonly Byte[] _state;

      private Int64 _stateCounter;
      private Int64 _seedCounter;

      public DigestBasedRandomGenerator(
         BlockDigestAlgorithm algorithm,
         Int32 seedCycleCount = DEFAULT_SEED_CYCLE_COUNT
         )
      {
         this.Algorithm = ArgumentValidator.ValidateNotNull( "Algorithm", algorithm );
         this._seed = new Byte[this.Algorithm.DigestByteCount];
         this._state = new Byte[this.Algorithm.DigestByteCount];

         this._stateCounter = 0; // When state is first time generated, the state counter will be increased to 1
         this._seedCounter = 0;
         this._seedCycleCount = seedCycleCount;
      }

      public override void AddSeedMaterial( Byte[] material, Int32 offset, Int32 count )
      {
         this.Algorithm.ProcessBlock( material, offset, count );
         this.Algorithm.ProcessBlock( this._seed );
         this.Algorithm.WriteDigest( this._seed );
      }

      public override void NextBytes( Byte[] array, Int32 offset, Int32 count )
      {
         var state = this._state;
         do
         {
            this.PopulateState();
            Array.Copy( state, 0, array, offset, Math.Min( count, state.Length ) );
            offset += state.Length;
         } while ( count - offset > 0 );
      }

      protected override void Dispose( Boolean disposing )
      {
         this.Algorithm.Reset();
         Array.Clear( this._state, 0, this._state.Length );
         Array.Clear( this._seed, 0, this._seed.Length );
      }

      protected BlockDigestAlgorithm Algorithm { get; }

      private void PopulateState()
      {
         var newStateCounter = this.AlgorithmProcessInt64( Interlocked.Increment( ref this._stateCounter ) );
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

      private Int64 AlgorithmProcessInt64( Int64 val )
      {
         this.ArrayForLong.WriteInt64LEToBytesNoRef( 0, val );
         this.Algorithm.ProcessBlock( this.ArrayForLong );
         return val;
      }

      public static DigestBasedRandomGenerator CreateAndSeedWithDefaultLogic( BlockDigestAlgorithm algorithm, Int32 seedCycleCount = DEFAULT_SEED_CYCLE_COUNT )
      {

         var retVal = new DigestBasedRandomGenerator( algorithm, seedCycleCount );
         // Use Guid as random source (should be version 4)
         retVal.AddSeedMaterial( Guid.NewGuid().ToByteArray() );

         // Use current ticks
         retVal.AddSeedMaterial( DateTime.Now.Ticks );

         // Use Guid again
         retVal.AddSeedMaterial( Guid.NewGuid().ToByteArray() );
         return retVal;
      }

   }
}