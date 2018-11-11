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

namespace AsyncEnumeration.Abstractions
{
   public partial interface IAsyncProvider
   {
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
      Task<T> AggregateAsync<T>( IAsyncEnumerable<T> source, Func<T, T, T> func );

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
      Task<T> AggregateAsync<T>( IAsyncEnumerable<T> source, Func<T, T, ValueTask<T>> asyncFunc );

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
      Task<TResult> AggregateAsync<T, TResult>( IAsyncEnumerable<T> source, Func<TResult, T, TResult> func, TResult seed );

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
      Task<TResult> AggregateAsync<T, TResult>( IAsyncEnumerable<T> source, Func<TResult, T, ValueTask<TResult>> asyncFunc, TResult seed );
   }


}


public static partial class E_AsyncEnumeration
{


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
   public static Task<T> AggregateAsync<T>( this IAsyncEnumerable<T> source, Func<T, T, T> func )
      => ( source.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).AggregateAsync( source, func );

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
   public static Task<T> AggregateAsync<T>( this IAsyncEnumerable<T> source, Func<T, T, ValueTask<T>> asyncFunc )
      => ( source.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).AggregateAsync( source, asyncFunc );

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
   public static Task<TResult> AggregateAsync<T, TResult>( this IAsyncEnumerable<T> source, Func<TResult, T, TResult> func, TResult seed = default )
      => ( source.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).AggregateAsync( source, func, seed );

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
   public static Task<TResult> AggregateAsync<T, TResult>( this IAsyncEnumerable<T> source, Func<TResult, T, ValueTask<TResult>> asyncFunc, TResult seed = default )
      => ( source.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).AggregateAsync( source, asyncFunc, seed );
}