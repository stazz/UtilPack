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
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will flatten the items returned by given selector callback.
      /// </summary>
      /// <typeparam name="T">The type of source items.</typeparam>
      /// <typeparam name="U">The type of target items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="selector">The callback to transform a single item into enumerable of items of type <typeparamref name="U"/>.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return items as flattened asynchronous enumerable.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="selector"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.SelectMany{TSource, TResult}(IEnumerable{TSource}, Func{TSource, IEnumerable{TResult}})"/>
      IAsyncEnumerable<U> SelectMany<T, U>( IAsyncEnumerable<T> enumerable, Func<T, IEnumerable<U>> selector );

      /// <summary>
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will asynchronously flatten the items returned by given selector callback.
      /// </summary>
      /// <typeparam name="T">The type of source items.</typeparam>
      /// <typeparam name="U">The type of target items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="asyncSelector">The callback to transform a single item into asynchronous enumerable of items of type <typeparamref name="U"/>.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return items as flattened asynchronous enumerable.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="asyncSelector"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.SelectMany{TSource, TResult}(IEnumerable{TSource}, Func{TSource, IEnumerable{TResult}})"/>
      IAsyncEnumerable<U> SelectMany<T, U>( IAsyncEnumerable<T> enumerable, Func<T, IAsyncEnumerable<U>> asyncSelector );
   }

}

public static partial class E_AsyncEnumeration
{
   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will flatten the items returned by given selector callback.
   /// </summary>
   /// <typeparam name="T">The type of source items.</typeparam>
   /// <typeparam name="U">The type of target items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="selector">The callback to transform a single item into enumerable of items of type <typeparamref name="U"/>.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will return items as flattened asynchronous enumerable.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="selector"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.SelectMany{TSource, TResult}(IEnumerable{TSource}, Func{TSource, IEnumerable{TResult}})"/>
   public static IAsyncEnumerable<U> SelectMany<T, U>( this IAsyncEnumerable<T> enumerable, Func<T, IEnumerable<U>> selector )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).SelectMany( enumerable, selector );

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will asynchronously flatten the items returned by given selector callback.
   /// </summary>
   /// <typeparam name="T">The type of source items.</typeparam>
   /// <typeparam name="U">The type of target items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="asyncSelector">The callback to transform a single item into asynchronous enumerable of items of type <typeparamref name="U"/>.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will return items as flattened asynchronous enumerable.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="asyncSelector"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.SelectMany{TSource, TResult}(IEnumerable{TSource}, Func{TSource, IEnumerable{TResult}})"/>
   public static IAsyncEnumerable<U> SelectMany<T, U>( this IAsyncEnumerable<T> enumerable, Func<T, IAsyncEnumerable<U>> asyncSelector )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).SelectMany( enumerable, asyncSelector );
}
