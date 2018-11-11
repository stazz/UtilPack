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
using System.Threading.Tasks;

namespace AsyncEnumeration.Abstractions
{
   public partial interface IAsyncProvider
   {
      /// <summary>
      /// Asynchronously fetches the first item in this <see cref="IAsyncEnumerable{T}"/> and discards any other items.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <returns>The first item returned by <see cref="IAsyncEnumerator{T}"/> of this <see cref="IAsyncEnumerable{T}"/>.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="InvalidOperationException">If this <see cref="IAsyncEnumerable{T}"/> has no elements.</exception>
      /// <seealso cref="System.Linq.Enumerable.First{TSource}(IEnumerable{TSource})"/>
      Task<T> FirstAsync<T>( IAsyncEnumerable<T> enumerable );

      /// <summary>
      /// Asynchronously fetches the first item in this <see cref="IAsyncEnumerable{T}"/> and discards any other items.
      /// If there are no items, the the default is returned for type <typeparamref name="T"/>.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <returns>The first item returned by <see cref="IAsyncEnumerator{T}"/> of this <see cref="IAsyncEnumerable{T}"/>, or default for type <typeparamref name="T"/>.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>
      Task<T> FirstOrDefaultAsync<T>( IAsyncEnumerable<T> enumerable );

   }


}

public static partial class E_AsyncEnumeration
{
   /// <summary>
   /// Asynchronously fetches the first item in this <see cref="IAsyncEnumerable{T}"/> and discards any other items.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>The first item returned by <see cref="IAsyncEnumerator{T}"/> of this <see cref="IAsyncEnumerable{T}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">If this <see cref="IAsyncEnumerable{T}"/> has no elements.</exception>
   /// <seealso cref="System.Linq.Enumerable.First{TSource}(IEnumerable{TSource})"/>
   public static Task<T> FirstAsync<T>( this IAsyncEnumerable<T> enumerable )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).FirstAsync( enumerable );

   /// <summary>
   /// Asynchronously fetches the first item in this <see cref="IAsyncEnumerable{T}"/> and discards any other items.
   /// If there are no items, the the default is returned for type <typeparamref name="T"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>The first item returned by <see cref="IAsyncEnumerator{T}"/> of this <see cref="IAsyncEnumerable{T}"/>, or default for type <typeparamref name="T"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>
   public static Task<T> FirstOrDefaultAsync<T>( this IAsyncEnumerable<T> enumerable )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).FirstOrDefaultAsync( enumerable );
}