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
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Abstractions
{

   public partial interface IAsyncProvider
   {
      /// <summary>
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return items as transformed by given selector callback.
      /// </summary>
      /// <typeparam name="T">The type of source items.</typeparam>
      /// <typeparam name="U">The type of target items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="selector">The callback to transform a single item into <typeparamref name="U"/>.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return items as transformed by given selector callback.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="selector"/> is <c>null</c>.</exception>
      /// <seealso cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>
      IAsyncEnumerable<U> Select<T, U>( IAsyncEnumerable<T> enumerable, Func<T, U> selector );

      /// <summary>
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return items as transformed by given asynchronous selector callback.
      /// </summary>
      /// <typeparam name="T">The type of source items.</typeparam>
      /// <typeparam name="U">The type of target items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="asyncSelector">The callback to asynchronously transform a single item into <typeparamref name="U"/>.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return items as transformed by given selector callback.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="asyncSelector"/> is <c>null</c>.</exception>
      /// <seealso cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>
      IAsyncEnumerable<U> Select<T, U>( IAsyncEnumerable<T> enumerable, Func<T, ValueTask<U>> asyncSelector );
   }


}

public static partial class E_AsyncEnumeration
{
   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return items as transformed by given selector callback.
   /// </summary>
   /// <typeparam name="T">The type of source items.</typeparam>
   /// <typeparam name="U">The type of target items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="selector">The callback to transform a single item into <typeparamref name="U"/>.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will return items as transformed by given selector callback.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="selector"/> is <c>null</c>.</exception>
   /// <seealso cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>
   public static IAsyncEnumerable<U> Select<T, U>( this IAsyncEnumerable<T> enumerable, Func<T, U> selector )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).Select( enumerable, selector );

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return items as transformed by given asynchronous selector callback.
   /// </summary>
   /// <typeparam name="T">The type of source items.</typeparam>
   /// <typeparam name="U">The type of target items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="asyncSelector">The callback to asynchronously transform a single item into <typeparamref name="U"/>.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will return items as transformed by given selector callback.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="asyncSelector"/> is <c>null</c>.</exception>
   /// <seealso cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>
   public static IAsyncEnumerable<U> Select<T, U>( this IAsyncEnumerable<T> enumerable, Func<T, ValueTask<U>> asyncSelector )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).Select( enumerable, asyncSelector );

}