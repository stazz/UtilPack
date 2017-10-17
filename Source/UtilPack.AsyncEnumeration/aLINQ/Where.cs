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
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.AsyncEnumeration;
using UtilPack.AsyncEnumeration.LINQ;

namespace UtilPack.AsyncEnumeration.LINQ
{
   internal sealed class WhereEnumerator<T> : IAsyncEnumerator<T>
   {
      private readonly IAsyncEnumerator<T> _source;
      private readonly Func<T, Boolean> _predicate;

      public WhereEnumerator(
         IAsyncEnumerator<T> source,
         Func<T, Boolean> syncPredicate
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._predicate = ArgumentValidator.ValidateNotNull( nameof( syncPredicate ), syncPredicate );
      }

      public Task<Boolean> WaitForNextAsync() => this._source.WaitForNextAsync();

      public T TryGetNext( out Boolean success )
      {
         var encountered = false;
         T item;
         do
         {
            item = this._source.TryGetNext( out success );
            encountered = success && this._predicate( item );
         } while ( success && !encountered );

         success = encountered;
         return item;
      }

      public Task DisposeAsync() => this._source.DisposeAsync();
   }

   internal sealed class AsyncWhereEnumerator<T> : IAsyncEnumerator<T>
   {
      private readonly IAsyncEnumerator<T> _source;
      private readonly Func<T, ValueTask<Boolean>> _predicate;
      private readonly Stack<T> _stack;

      public AsyncWhereEnumerator(
         IAsyncEnumerator<T> source,
         Func<T, ValueTask<Boolean>> asyncPredicate
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._predicate = asyncPredicate;
         this._stack = new Stack<T>();
      }

      //public Boolean IsConcurrentEnumerationSupported => this._source.IsConcurrentEnumerationSupported;

      public async Task<Boolean> WaitForNextAsync()
      {
         var stack = this._stack;
         // Discard any previous items
         stack.Clear();
         // We must use the predicate in this method, since this is our only asynchronous method while enumerating
         while ( stack.Count == 0 && await this._source.WaitForNextAsync() )
         {
            Boolean success;
            do
            {
               var item = this._source.TryGetNext( out success );
               if ( success && await this._predicate( item ) )
               {
                  stack.Push( item );
               }
            } while ( success );
         }

         return stack.Count > 0;
      }


      public T TryGetNext( out Boolean success )
      {
         success = this._stack.Count > 0;
         return success ? this._stack.Pop() : default;
      }

      public Task DisposeAsync() => this._source.DisposeAsync();
   }
}


public static partial class E_UtilPack
{

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will filter items based on given predicate callback.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="predicate">The callback which will filter the results. By returning <c>true</c>, the result will be included in returned enumerable; otherwise not.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will filter items based on given predicate callback.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="predicate"/> is <c>null</c>.</exception>
   public static IAsyncEnumerable<T> Where<T>( this IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( predicate ), predicate );
      return new EnumerableWrapper<T>( () => enumerable.GetAsyncEnumerator().Where( predicate ) );
   }

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will filter items based on given asynchronous predicate callback.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="asyncPredicate">The callback which will asynchronously filter the results. By returning <c>true</c>, the result will be included in returned enumerable; otherwise not.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will filter items based on given predicate callback.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="asyncPredicate"/> is <c>null</c>.</exception>
   public static IAsyncEnumerable<T> Where<T>( this IAsyncEnumerable<T> enumerable, Func<T, ValueTask<Boolean>> asyncPredicate )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( asyncPredicate ), asyncPredicate );
      return new EnumerableWrapper<T>( () => enumerable.GetAsyncEnumerator().Where( asyncPredicate ) );
   }


   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerator{T}"/> which will filter items based on given predicate callback.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerator">This <see cref="IAsyncEnumerator{T}"/>.</param>
   /// <param name="predicate">The callback which will filter the results. By returning <c>true</c>, the result will be included in returned enumerator; otherwise not.</param>
   /// <returns><see cref="IAsyncEnumerator{T}"/> which will filter items based on given predicate callback.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="predicate"/> is <c>null</c>.</exception>
   public static IAsyncEnumerator<T> Where<T>( this IAsyncEnumerator<T> enumerator, Func<T, Boolean> predicate )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      ArgumentValidator.ValidateNotNull( nameof( predicate ), predicate );
      return new WhereEnumerator<T>( enumerator, predicate );
   }

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerator{T}"/> which will filter items based on given asynchronous predicate callback.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerator">This <see cref="IAsyncEnumerator{T}"/>.</param>
   /// <param name="asyncPredicate">The callback which will asynchronously filter the results. By returning <c>true</c>, the result will be included in returned enumerator; otherwise not.</param>
   /// <returns><see cref="IAsyncEnumerator{T}"/> which will filter items based on given predicate callback.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="asyncPredicate"/> is <c>null</c>.</exception>
   public static IAsyncEnumerator<T> Where<T>( this IAsyncEnumerator<T> enumerator, Func<T, ValueTask<Boolean>> asyncPredicate )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      ArgumentValidator.ValidateNotNull( nameof( asyncPredicate ), asyncPredicate );
      return new AsyncWhereEnumerator<T>( enumerator, asyncPredicate );
   }

}