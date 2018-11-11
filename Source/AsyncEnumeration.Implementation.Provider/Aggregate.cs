/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Implementation.Provider
{


   public partial class DefaultAsyncProvider
   {
      private const Int32 INITIAL = 0;
      private const Int32 FIRST_SEEN = 1;

      /// <summary>
      /// Similarly to <see cref="System.Linq.Enumerable.Aggregate{TSource}(IEnumerable{TSource}, Func{TSource, TSource, TSource})"/> method, this method potentially asynchronously enumerates this <see cref="IAsyncEnumerable{T}"/> and aggregates a single value using the given <paramref name="func"/> synchronous callback.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="func">The synchronous callback function to perform aggregation. First argument is previous element, second argument is current element, and return value is the new aggregated value.</param>
      /// <returns>An aggregated value.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="func"/> is <c>null</c>.</exception>
      /// <exception cref="InvalidOperationException">If this <see cref="IAsyncEnumerable{T}"/> does not contain at least one element.</exception>
      public async Task<T> AggregateAsync<T>( IAsyncEnumerable<T> source, Func<T, T, T> func )
      {
         ArgumentValidator.ValidateNotNullReference( source );
         ArgumentValidator.ValidateNotNull( nameof( func ), func );

         var state = INITIAL;
         T prev = default;
         await source.EnumerateAsync( item =>
         {
            if ( state == INITIAL )
            {
               prev = item;
               Interlocked.Exchange( ref state, FIRST_SEEN );
            }
            else
            {
               prev = func( prev, item );
            }
         } );

         return state == FIRST_SEEN ? prev : throw AsyncProviderUtilities.EmptySequenceException();
      }

      /// <summary>
      /// Similarly to <see cref="System.Linq.Enumerable.Aggregate{TSource}(IEnumerable{TSource}, Func{TSource, TSource, TSource})"/> method, this method potentially asynchronously enumerates this <see cref="IAsyncEnumerable{T}"/> and aggregates a single value using the given <paramref name="asyncFunc"/> potentially asynchronous callback.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="asyncFunc">The potentially asynchronous callback function to perform aggregation. First argument is previous element, second argument is current element, and return value is the new aggregated value.</param>
      /// <returns>An aggregated value.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="asyncFunc"/> is <c>null</c>.</exception>
      /// <exception cref="InvalidOperationException">If this <see cref="IAsyncEnumerable{T}"/> does not contain at least one element.</exception>
      public async Task<T> AggregateAsync<T>( IAsyncEnumerable<T> source, Func<T, T, ValueTask<T>> asyncFunc )
      {
         ArgumentValidator.ValidateNotNullReference( source );
         ArgumentValidator.ValidateNotNull( nameof( asyncFunc ), asyncFunc );

         var state = INITIAL;
         T prev = default;
         await source.EnumerateAsync( async item =>
         {
            if ( state == INITIAL )
            {
               prev = item;
               Interlocked.Exchange( ref state, FIRST_SEEN );
            }
            else
            {
               prev = await asyncFunc( prev, item );
            }
         } );

         return state == FIRST_SEEN ? prev : throw AsyncProviderUtilities.EmptySequenceException();
      }

      /// <summary>
      /// Similarly to <see cref="System.Linq.Enumerable.Aggregate{TSource, TAccumulate}(IEnumerable{TSource}, TAccumulate, Func{TAccumulate, TSource, TAccumulate})"/> method, this method potentially asynchronously enumerates this <see cref="IAsyncEnumerable{T}"/> and aggregates a single value using the given <paramref name="func"/> synchronous callback.
      /// The type of the intermediate and return value is different than the type of elements in this <see cref="IAsyncEnumerable{T}"/>.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <typeparam name="TResult">The type of intermediate and result values.</typeparam>
      /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="func">The synchronous calllback function to perform aggregation. First argument is intermediate value, second argument is current element, and return value is the new intermediate value, or return value if current element is last element.</param>
      /// <param name="seed">The optional initial value for first argument of <paramref name="func"/> callback.</param>
      /// <returns>An aggregated value.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="func"/> is <c>null</c>.</exception>
      public async Task<TResult> AggregateAsync<T, TResult>( IAsyncEnumerable<T> source, Func<TResult, T, TResult> func, TResult seed )
      {
         ArgumentValidator.ValidateNotNullReference( source );
         ArgumentValidator.ValidateNotNull( nameof( func ), func );
         await source.EnumerateAsync( item =>
         {
            seed = func( seed, item );
         } );

         return seed;
      }

      /// <summary>
      /// Similarly to <see cref="System.Linq.Enumerable.Aggregate{TSource, TAccumulate}(IEnumerable{TSource}, TAccumulate, Func{TAccumulate, TSource, TAccumulate})"/> method, this method potentially asynchronously enumerates this <see cref="IAsyncEnumerable{T}"/> and aggregates a single value using the given <paramref name="asyncFunc"/> potentially asynchronous callback.
      /// The type of the intermediate and return value is different than the type of elements in this <see cref="IAsyncEnumerable{T}"/>.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <typeparam name="TResult">The type of intermediate and result values.</typeparam>
      /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="asyncFunc">The potentially asynchronous calllback function to perform aggregation. First argument is intermediate value, second argument is current element, and return value is the new intermediate value, or return value if current element is last element.</param>
      /// <param name="seed">The optional initial value for first argument of <paramref name="asyncFunc"/> callback.</param>
      /// <returns>An aggregated value.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="asyncFunc"/> is <c>null</c>.</exception>
      public async Task<TResult> AggregateAsync<T, TResult>( IAsyncEnumerable<T> source, Func<TResult, T, ValueTask<TResult>> asyncFunc, TResult seed )
      {
         ArgumentValidator.ValidateNotNullReference( source );
         ArgumentValidator.ValidateNotNull( nameof( asyncFunc ), asyncFunc );
         await source.EnumerateAsync( async item =>
         {
            seed = await asyncFunc( seed, item );
         } );

         return seed;
      }
   }
}


