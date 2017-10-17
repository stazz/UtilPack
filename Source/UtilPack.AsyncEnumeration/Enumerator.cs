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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.AsyncEnumeration;

namespace System.Collections.Generic
{
   /// <summary>
   /// This interface mimics <see cref="IEnumerable{T}"/> for enumerables which must perform asynchronous waiting when moving to next item.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated. This parameter is covariant.</typeparam>
   public interface IAsyncEnumerable<out T>
   {
      /// <summary>
      /// Gets the <see cref="IAsyncEnumerator{T}"/> to use to enumerate this <see cref="IAsyncEnumerable{T}"/>.
      /// </summary>
      /// <returns>A <see cref="IAsyncEnumerator{T}"/> which should be used to enumerate this <see cref="IAsyncEnumerable{T}"/>.</returns>
      IAsyncEnumerator<T> GetAsyncEnumerator();
   }

   /// <summary>
   /// This interface mimics <see cref="IEnumerator{T}"/> for enumerators which can potentially cause asynchronous waiting.
   /// Such scenario is common in e.g. enumerating SQL query results.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   public interface IAsyncEnumerator<out T> : IAsyncDisposable
   {
      /// <summary>
      /// This method mimics <see cref="System.Collections.IEnumerator.MoveNext"/> method in order to asynchronously read the next item.
      /// Please note that instead of directly using this method, one should use <see cref="E_UtilPack.EnumerateSequentiallyAsync{T}(IAsyncEnumerable{T}, Action{T})"/>, <see cref="E_UtilPack.EnumerateSequentiallyAsync{T}(IAsyncEnumerable{T}, Func{T, Task})"/>´extension methods, as those methods will take care of properly finishing enumeration in case of exceptions.
      /// </summary>
      /// <returns>A task, which will return <c>true</c> if next item is encountered, and <c>false</c> if this enumeration ended.</returns>
      Task<Boolean> WaitForNextAsync();

      /// <summary>
      /// This method mimics <see cref="IEnumerator{T}.Current"/> property in order to get one or more items previously fetched by <see cref="WaitForNextAsync"/>.
      /// </summary>
      /// <param name="success">Whether getting next value was successful.</param>
      T TryGetNext( out Boolean success );
   }
}

namespace UtilPack.AsyncEnumeration
{
   //public interface IAsyncEnumerationInformation
   //{
   //   /// <summary>
   //   /// Gets the value indicating whether this <see cref="IAsyncEnumerationInformation"/> supports parallel enumeration.
   //   /// </summary>
   //   /// <value>The value indicating whether this <see cref="IAsyncEnumerationInformation"/> supports parallel enumeration.</value>
   //   Boolean IsConcurrentEnumerationSupported { get; }
   //}

   //public interface AsyncEnumerable<out T> : IAsyncEnumerable<T>, IAsyncEnumerationInformation
   //{
   //}


   /// <summary>
   /// This is helper utility class to provide callback-based <see cref="IAsyncEnumerator{T}"/> creation.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   public sealed class EnumerableWrapper<T> : IAsyncEnumerable<T>
   {
      //private readonly Func<Boolean> _concurrentSupported;
      private readonly Func<IAsyncEnumerator<T>> _getEnumerator;

      /// <summary>
      /// Creates a new instance of <see cref="EnumerableWrapper{T}"/> with given callback.
      /// </summary>
      /// <param name="getEnumerator">The callback to create <see cref="IAsyncEnumerator{T}"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="getEnumerator"/> is <c>null</c>.</exception>
      public EnumerableWrapper(
         Func<IAsyncEnumerator<T>> getEnumerator
         //Func<Boolean> concurrentSupported = null
         )
      {
         this._getEnumerator = ArgumentValidator.ValidateNotNull( nameof( getEnumerator ), getEnumerator );
         //this._concurrentSupported = concurrentSupported;
      }

      //public Boolean IsConcurrentEnumerationSupported => this._concurrentSupported?.Invoke() ?? false;

      IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator() => this._getEnumerator();
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