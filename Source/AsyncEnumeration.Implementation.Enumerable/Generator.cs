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
using AsyncEnumeration.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Implementation.Enumerable
{

   internal sealed class EnumerableGenerator<T> : IAsyncEnumerable<T>
   {
      private readonly Func<IAsyncEnumerator<T>> _getEnumerator;
      private readonly IAsyncProvider _asyncProvider;

      /// <summary>
      /// Creates a new instance of <see cref="EnumerableWrapper{T}"/> with given callback.
      /// </summary>
      /// <param name="getEnumerator">The callback to create <see cref="IAsyncEnumerator{T}"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="getEnumerator"/> is <c>null</c>.</exception>
      public EnumerableGenerator(
         IAsyncProvider provider,
         Func<IAsyncEnumerator<T>> getEnumerator
         )
      {
         this._getEnumerator = ArgumentValidator.ValidateNotNull( nameof( getEnumerator ), getEnumerator );
         this._asyncProvider = ArgumentValidator.ValidateNotNull( nameof( provider ), provider );
      }

      IAsyncProvider IAsyncEnumerable.AsyncProvider => this._asyncProvider;

      IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator() => this._getEnumerator();
   }

   internal sealed class EnumerableGenerator<T, TArg> : IAsyncEnumerable<T>
   {
      private readonly Func<TArg, IAsyncEnumerator<T>> _getEnumerator;
      private readonly IAsyncProvider _asyncProvider;
      private readonly TArg _arg;

      /// <summary>
      /// Creates a new instance of <see cref="EnumerableWrapper{T}"/> with given callback.
      /// </summary>
      /// <param name="getEnumerator">The callback to create <see cref="IAsyncEnumerator{T}"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="getEnumerator"/> is <c>null</c>.</exception>
      public EnumerableGenerator(
         IAsyncProvider provider,
         TArg arg,
         Func<TArg, IAsyncEnumerator<T>> getEnumerator
         )
      {
         this._getEnumerator = ArgumentValidator.ValidateNotNull( nameof( getEnumerator ), getEnumerator );
         this._arg = arg;
         this._asyncProvider = ArgumentValidator.ValidateNotNull( nameof( provider ), provider );
      }

      IAsyncProvider IAsyncEnumerable.AsyncProvider => this._asyncProvider;

      IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator() => this._getEnumerator( this._arg );
   }

   //internal static class UtilPackExtensions2
   //{
   //   // TODO move to UtilPack
   //   public static Boolean TryAddWithLocking<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, TValue value, Object lockObject = null )
   //   {
   //      lock ( lockObject ?? dictionary )
   //      {
   //         var retVal = !dictionary.ContainsKey( key );
   //         if ( retVal )
   //         {
   //            dictionary.Add( key, value );
   //         }

   //         return retVal;
   //      }
   //   }

   //   public static Boolean TryRemoveWithLocking<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value, Object lockObject = null )
   //   {
   //      lock ( lockObject ?? dictionary )
   //      {
   //         var retVal = dictionary.ContainsKey( key );
   //         value = retVal ? dictionary[key] : default;
   //         dictionary.Remove( key );
   //         return retVal;
   //      }
   //   }

   //   public static void AddWithLocking<TValue>( this IList<TValue> list, TValue item, Object lockObject = null )
   //   {
   //      lock ( lockObject ?? list )
   //      {
   //         list.Add( item );
   //      }
   //   }

   //   public static Boolean TryPopWithLocking<TValue>( this IList<TValue> list, out TValue value, Object lockObject = null )
   //   {
   //      lock ( lockObject ?? list )
   //      {
   //         var count = list.Count;
   //         var retVal = list.Count > 0;

   //         value = retVal ? list[count - 1] : default;
   //         list.RemoveAt( count - 1 );
   //         return retVal;
   //      }
   //   }

   //   public static void ClearWithLocking<TValue>( this ICollection<TValue> collection, Object lockObject = null )
   //   {
   //      lock ( lockObject ?? collection )
   //      {
   //         collection.Clear();
   //      }
   //   }
   //}

}