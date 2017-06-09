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
   /// 
   /// </summary>
   public interface BlockDigestAlgorithm : IDisposable
   {
      /// <summary>
      /// 
      /// </summary>
      /// <param name="data"></param>
      /// <param name="offset"></param>
      /// <param name="count"></param>
      void ProcessBlock( Byte[] data, Int32 offset, Int32 count );

      /// <summary>
      /// 
      /// </summary>
      /// <param name="array"></param>
      /// <param name="offset"></param>
      /// <returns></returns>
      void WriteDigest( Byte[] array, Int32 offset );

      /// <summary>
      /// 
      /// </summary>
      /// <value></value>
      Int32 DigestByteCount { get; }

      /// <summary>
      /// Resets this digest algorithm instance to its initial state.
      /// </summary>
      void Reset();
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
}

public static partial class E_UtilPack
{
   /// <summary>
   /// 
   /// </summary>
   /// <param name="transform"></param>
   /// <param name="array"></param>
   /// <param name="offset"></param>
   /// <param name="count"></param>
   /// <returns></returns>
   public static Byte[] ComputeHash( this BlockDigestAlgorithm transform, Byte[] array, Int32 offset, Int32 count )
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
   /// 
   /// </summary>
   /// <param name="algorithm"></param>
   /// <returns></returns>
   public static Byte[] CreateDigest( this BlockDigestAlgorithm algorithm )
   {
      var retVal = new Byte[algorithm.DigestByteCount];
      algorithm.WriteDigest( retVal, 0 );
      return retVal;
   }

   /// <summary>
   /// 
   /// </summary>
   /// <param name="algorithm"></param>
   /// <param name="block"></param>
   public static void ProcessBlock( this BlockDigestAlgorithm algorithm, Byte[] block )
   {
      algorithm.ProcessBlock( block, 0, block.Length );
   }

   /// <summary>
   /// 
   /// </summary>
   /// <param name="algorithm"></param>
   /// <param name="array"></param>
   public static void WriteDigest( this BlockDigestAlgorithm algorithm, Byte[] array )
   {
      algorithm.WriteDigest( array, 0 );
   }
}