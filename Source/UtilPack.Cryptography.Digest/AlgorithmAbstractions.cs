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
      /// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c>.</exception>
      void ProcessBlock( Byte[] data, Int32 offset, Int32 count );

      /// <summary>
      /// Signals that all data has been read and that a digest should be produced to given array.
      /// </summary>
      /// <param name="array">The byte array where to write digest to.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start writing.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
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
   /// <param name="source">The byte data of which to compute digest from.</param>
   /// <returns>The digest produced by this <see cref="BlockDigestAlgorithm"/>.</returns>
   public static Byte[] ComputeDigest( this BlockDigestAlgorithm transform, Byte[] source )
      => transform.ComputeDigest( source, 0, source?.Length ?? 0 );


   /// <summary>
   /// Helper method to compute digest when all the data has already been read to given array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="source">The data. Reading will start at offset <c>0</c>.</param>
   /// <param name="sourceCount">The amount of bytes to read from <paramref name="source"/>.</param>
   /// <returns>The digest produced by this <see cref="BlockDigestAlgorithm"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="source"/> is <c>null</c>.</exception>
   public static Byte[] ComputeDigest( this BlockDigestAlgorithm transform, Byte[] source, Int32 sourceCount )
      => transform.ComputeDigest( source, 0, sourceCount );

   /// <summary>
   /// Helper method to compute digest when all the data has already been read to given array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="source">The data.</param>
   /// <param name="sourceOffset">The offset in <paramref name="source"/> where to start reading.</param>
   /// <param name="sourceCount">The amount of bytes to read from <paramref name="source"/>.</param>
   /// <returns>The digest produced by this <see cref="BlockDigestAlgorithm"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="source"/> is <c>null</c>.</exception>
   public static Byte[] ComputeDigest( this BlockDigestAlgorithm transform, Byte[] source, Int32 sourceOffset, Int32 sourceCount )
   {
      transform.ProcessBlock( source, sourceOffset, sourceCount );
      return transform.CreateDigest();
   }

   /// <summary>
   /// Helper method to write digest to a target array, when source data is already completely in source array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="sourceArray">The array holding all of the data.</param>
   /// <param name="targetArray">The array where to write the digest, starting at index <c>0</c>.</param>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="sourceArray"/> or <paramref name="targetArray"/> is <c>null</c>.</exception>
   public static void ComputeDigest( this BlockDigestAlgorithm transform, Byte[] sourceArray, Byte[] targetArray )
      => transform.ComputeDigest( sourceArray, 0, sourceArray?.Length ?? 0, targetArray, 0 );

   /// <summary>
   /// Helper method to write digest to a target array, when source data is already completely in source array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="sourceArray">The array holding all of the data.</param>
   /// <param name="sourceCount">The amount of data in the <paramref name="sourceArray"/>, starting at index <c>0</c>.</param>
   /// <param name="targetArray">The array where to write the digest, starting at index <c>0</c>.</param>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="sourceArray"/> or <paramref name="targetArray"/> is <c>null</c>.</exception>
   public static void ComputeDigest( this BlockDigestAlgorithm transform, Byte[] sourceArray, Int32 sourceCount, Byte[] targetArray )
      => transform.ComputeDigest( sourceArray, 0, sourceCount, targetArray, 0 );


   /// <summary>
   /// Helper method to write digest to a target array, when source data is already completely in source array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="sourceArray">The array holding all of the data.</param>
   /// <param name="sourceOffset">The offset in <paramref name="sourceArray"/> where the data starts.</param>
   /// <param name="sourceCount">The amount of data in <paramref name="sourceArray"/>.</param>
   /// <param name="targetArray">The array where to write the digest, starting at index <c>0</c>.</param>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="sourceArray"/> or <paramref name="targetArray"/> is <c>null</c>.</exception>
   public static void ComputeDigest( this BlockDigestAlgorithm transform, Byte[] sourceArray, Int32 sourceOffset, Int32 sourceCount, Byte[] targetArray )
      => transform.ComputeDigest( sourceArray, sourceOffset, sourceCount, targetArray, 0 );


   /// <summary>
   /// Helper method to write digest to a target array, when source data is already completely in source array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="sourceArray">The array holding all of the data.</param>
   /// <param name="targetArray">The array where to write the digest, starting at <paramref name="targetOffset"/>.</param>
   /// <param name="targetOffset">The index in <paramref name="targetArray"/> where to start writing digest.</param>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="sourceArray"/> or <paramref name="targetArray"/> is <c>null</c>.</exception>
   public static void ComputeDigest( this BlockDigestAlgorithm transform, Byte[] sourceArray, Byte[] targetArray, Int32 targetOffset )
      => transform.ComputeDigest( sourceArray, 0, sourceArray?.Length ?? 0, targetArray, 0 );

   /// <summary>
   /// Helper method to write digest to a target array, when source data is already completely in source array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="sourceArray">The array holding all of the data.</param>
   /// <param name="sourceCount">The amount of data in the <paramref name="sourceArray"/>, starting at index <c>0</c>.</param>
   /// <param name="targetArray">The array where to write the digest, starting at <paramref name="targetOffset"/>.</param>
   /// <param name="targetOffset">The index in <paramref name="targetArray"/> where to start writing digest.</param>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="sourceArray"/> or <paramref name="targetArray"/> is <c>null</c>.</exception>
   public static void ComputeDigest( this BlockDigestAlgorithm transform, Byte[] sourceArray, Int32 sourceCount, Byte[] targetArray, Int32 targetOffset )
      => transform.ComputeDigest( sourceArray, 0, sourceCount, targetArray, targetOffset );

   /// <summary>
   /// Helper method to write digest to a target array, when source data is already completely in source array.
   /// </summary>
   /// <param name="transform">This <see cref="BlockDigestAlgorithm"/>.</param>
   /// <param name="sourceArray">The array holding all of the data.</param>
   /// <param name="sourceOffset">The offset in <paramref name="sourceArray"/> where the data starts.</param>
   /// <param name="sourceCount">The amount of data in <paramref name="sourceArray"/>.</param>
   /// <param name="targetArray">The array where to write the digest, starting at <paramref name="targetOffset"/>.</param>
   /// <param name="targetOffset">The index in <paramref name="targetArray"/> where to start writing digest.</param>
   /// <exception cref="NullReferenceException">If this <see cref="BlockDigestAlgorithm"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="sourceArray"/> or <paramref name="targetArray"/> is <c>null</c>.</exception>
   public static void ComputeDigest( this BlockDigestAlgorithm transform, Byte[] sourceArray, Int32 sourceOffset, Int32 sourceCount, Byte[] targetArray, Int32 targetOffset )
   {
      transform.ProcessBlock( sourceArray, sourceOffset, sourceCount );
      transform.WriteDigest( targetArray, targetOffset );
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

}