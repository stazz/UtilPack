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
using System.Collections.Generic;
using System.Text;
using UtilPack;

namespace UtilPack
{
   /// <summary>
   /// This interface represents a pool of strings, which have been deserialized from some binary data.
   /// The pool stores the deserialized strings, and if on next call the deserialization would lead to allocating string with same contents as some string pooled by this <see cref="BinaryStringPool"/>, the pooled string will be returned instead of allocating a new string.
   /// Implementations in this assembly are optimized to perform with absolute minimum heap allocation, causing new heap allocation *only* when the string is not pooled.
   /// </summary>
   public interface BinaryStringPool
   {
      /// <summary>
      /// Gets the string corresponding to given binary data from underlying pool of strings, or deserializes string and pools it.
      /// </summary>
      /// <param name="array">The binary data to deserialize string from.</param>
      /// <param name="offset">The offset in <paramref name="array"/> where to start deserializing.</param>
      /// <param name="count">The amount of bytes to deserialize.</param>
      /// <returns>A pooled or newly created string.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="array"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="count"/> is less than <c>0</c>.</exception>
      /// <exception cref="ArgumentException">If <paramref name="offset"/> + <paramref name="count"/> is greater than array length.</exception>
      String GetString( Byte[] array, Int32 offset, Int32 count );

      /// <summary>
      /// Clears this string pool.
      /// </summary>
      void ClearPool();
   }

   /// <summary>
   /// This class is factory class for instances of <see cref="BinaryStringPool"/>.
   /// </summary>
   public static class BinaryStringPoolFactory
   {
      /// <summary>
      /// Creates a new instance of <see cref="BinaryStringPool"/> which will perform correctly in single-threaded scenarios only.
      /// </summary>
      /// <param name="encoding">The encoding to use when deserializing strings. If <c>null</c>, then <see cref="UTF8Encoding"/> will be used, passing <c>false</c> to both parameters of <see cref="UTF8Encoding(Boolean, Boolean)"/></param>
      /// <returns>A new instance of <see cref="BinaryStringPool"/> which will perform correctly in single-threaded scenarios only.</returns>
      public static BinaryStringPool NewNotConcurrentBinaryStringPool( Encoding encoding = null )
      {
         return new DefaultBinaryStringPool(
            new Dictionary<ArrayInformation, String>(),
            encoding ?? new UTF8Encoding( false, false )
            );
      }

      /// <summary>
      /// Creates a new instance of <see cref="BinaryStringPool"/> which will perform correctly in both single- and multi-threaded scenarios.
      /// </summary>
      /// <param name="encoding">The encoding to use when deserializing strings. If <c>null</c>, then <see cref="UTF8Encoding"/> will be used, passing <c>false</c> to both parameters of <see cref="UTF8Encoding(Boolean, Boolean)"/></param>
      /// <returns>A new instance of <see cref="BinaryStringPool"/> which will perform correctly in single-threaded scenarios only.</returns>
      public static BinaryStringPool NewConcurrentBinaryStringPool( Encoding encoding = null )
      {
         return new
#if NETSTANDARD1_0
            LockingBinaryStringPool(
#else
            DefaultBinaryStringPool( new System.Collections.Concurrent.ConcurrentDictionary<ArrayInformation, String>(),
#endif
            encoding ?? new UTF8Encoding( false, false )
            );
      }
   }

   internal struct ArrayInformation : IEquatable<ArrayInformation>
   {
      // Pre-calculate hash code, since data will never change
      private readonly Int32 _hashCode;
      private readonly Byte[] _array;
      private readonly Int32 _offset;
      private readonly Int32 _count;

      public ArrayInformation( Byte[] array, Int32 offset, Int32 count )
      {
         this._array = array;
         this._offset = offset;
         this._count = count;

         // Jon Skeet's answer on http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
         var hashCode = 17;
         var max = offset + count;
         for ( var i = offset; i < max; ++i )
         {
            hashCode = unchecked(hashCode * 23 + array[i]);
         }
         this._hashCode = hashCode;
      }

      public override Boolean Equals( Object obj )
      {
         return obj is ArrayInformation other && this.Equals( other );
      }

      public override Int32 GetHashCode()
      {
         return this._hashCode;
      }

      public Boolean Equals( ArrayInformation other )
      {
         var retVal = this._count == other._count && this._hashCode == other._hashCode;
         if ( retVal )
         {
            var max = this._offset + this._count;
            var thisArray = this._array;
            var otherArray = other._array;
            for ( Int32 i = this._offset, j = other._offset; i < max; ++i, ++j )
            {
               if ( thisArray[i] != otherArray[j] )
               {
                  retVal = false;
                  break;
               }
            }
         }

         return retVal;
      }

   }

   internal sealed class DefaultBinaryStringPool : BinaryStringPool
   {
      private readonly Encoding _encoding;
      private readonly IDictionary<ArrayInformation, String> _pool;

      public DefaultBinaryStringPool(
         IDictionary<ArrayInformation, String> pool,
         Encoding encoding
         )
      {
         this._pool = ArgumentValidator.ValidateNotNull( nameof( pool ), pool );
         this._encoding = ArgumentValidator.ValidateNotNull( nameof( encoding ), encoding );
      }

      public String GetString( Byte[] array, Int32 offset, Int32 count )
      {
         String retVal;
         if ( count == 0 )
         {
            retVal = String.Empty;
         }
         else
         {
            array.CheckArrayArguments( offset, count, true );
            if ( !this._pool.TryGetValue( new ArrayInformation( array, offset, count ), out retVal ) )
            {
               // Since ArrayInformation will continue to hold on array, we must create copy (ofc also because someone may modify the original one)
               retVal = this._encoding.GetString( array, offset, count );
               this._pool[new ArrayInformation( array.CreateArrayCopy( offset, count ), 0, count )] = retVal;
            }
         }
         return retVal;
      }

      public void ClearPool()
      {
         this._pool.Clear();
      }
   }

#if NETSTANDARD1_0
   internal sealed class LockingBinaryStringPool : BinaryStringPool
   {
      private readonly Encoding _encoding;

      // 
      private readonly IDictionary<ArrayInformation, String> _pool;

      public LockingBinaryStringPool(
         Encoding encoding
         )
      {
         this._pool = new Dictionary<ArrayInformation, String>();
         this._encoding = ArgumentValidator.ValidateNotNull( nameof( encoding ), encoding );
      }

      public String GetString( Byte[] array, Int32 offset, Int32 count )
      {
         String retVal;
         if ( count == 0 )
         {
            retVal = String.Empty;
         }
         else
         {
            array.CheckArrayArguments( offset, count, false );
            var aInfo = new ArrayInformation( array, offset, count );
            if ( !this._pool.TryGetValue( aInfo, out retVal ) )
            {
               lock ( this._pool )
               {
                  if ( !this._pool.TryGetValue( aInfo, out retVal ) )
                  {
                     // Since ArrayInformation will continue to hold on array, we must create copy (ofc also because someone may modify the original one)
                     retVal = this._encoding.GetString( array, offset, count );
                     this._pool[new ArrayInformation( array.CreateArrayCopy( offset, count ), 0, count )] = retVal;
                  }
               }
            }
         }
         return retVal;
      }

      public void ClearPool()
      {
         lock ( this._pool )
         {
            this._pool.Clear();
         }
      }
   }
#endif
}

public static partial class E_UtilPack
{
   /// <summary>
   /// This is helper method to invoke <see cref="BinaryStringPool.GetString"/> giving whole array as argumnent.
   /// </summary>
   /// <param name="pool">This <see cref="BinaryStringPool"/>.</param>
   /// <param name="array">The array containing serialized string.</param>
   /// <returns>Newly created or cached string.</returns>
   public static String GetString(
      this BinaryStringPool pool,
      Byte[] array )
   {
      return pool.GetString( array, 0, ArgumentValidator.ValidateNotNull( nameof( array ), array ).Length );
   }
}