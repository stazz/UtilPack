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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Abstractions
{
   public partial interface IAsyncProvider
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
      /// <seealso cref="System.Linq.Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      IAsyncEnumerable<T> Where<T>( IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate );

      /// <summary>
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will filter items based on given asynchronous predicate callback.
      /// </summary>
      /// <typeparam name="T">The type of items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="asyncPredicate">The callback which will asynchronously filter the results. By returning <c>true</c>, the result will be included in returned enumerable; otherwise not.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will filter items based on given predicate callback.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="asyncPredicate"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      IAsyncEnumerable<T> Where<T>( IAsyncEnumerable<T> enumerable, Func<T, Task<Boolean>> asyncPredicate );
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
   /// <seealso cref="System.Linq.Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
   public static IAsyncEnumerable<T> Where<T>( this IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).Where( enumerable, predicate );

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will filter items based on given asynchronous predicate callback.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="asyncPredicate">The callback which will asynchronously filter the results. By returning <c>true</c>, the result will be included in returned enumerable; otherwise not.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will filter items based on given predicate callback.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="asyncPredicate"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Where{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
   public static IAsyncEnumerable<T> Where<T>( this IAsyncEnumerable<T> enumerable, Func<T, Task<Boolean>> asyncPredicate )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).Where( enumerable, asyncPredicate );
}