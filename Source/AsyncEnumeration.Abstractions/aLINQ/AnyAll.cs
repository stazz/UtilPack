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
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Abstractions
{
   public partial interface IAsyncProvider
   {
      /// <summary>
      /// Similarly to <see cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource})"/>, this method checks whether this <see cref="IAsyncEnumerable{T}"/> contains at least one element.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> has at least one element; <c>false</c> otherwise.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource})"/>
      Task<Boolean> AnyAsync<T>( IAsyncEnumerable<T> source );

      /// <summary>
      /// Similarly to <see cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>, this method checks whether this <see cref="IAsyncEnumerable{T}"/> contains at least one element, that satisfies condition checked by given <paramref name="predicate"/> synchronous callback.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="predicate">The synchronous callback to check whether an element satifies some condition. If <c>null</c>, then the first item will always satisfy the condition.</param>
      /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> has at least one element that satifies condition checked by <paramref name="predicate"/>; <c>false</c> otherwise.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      Task<Boolean> AnyAsync<T>( IAsyncEnumerable<T> source, Func<T, Boolean> predicate );

      /// <summary>
      /// Similarly to <see cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>, this method checks whether this <see cref="IAsyncEnumerable{T}"/> contains at least one element, that satisfies condition checked by given <paramref name="asyncPredicate"/> potentially asynchronous callback.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="asyncPredicate">The potentially asynchronous callback to check whether an element satifies some condition. If <c>null</c>, then the first item will always satisfy the condition.</param>
      /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> has at least one element that satifies condition checked by <paramref name="asyncPredicate"/>; <c>false</c> otherwise.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      Task<Boolean> AnyAsync<T>( IAsyncEnumerable<T> source, Func<T, ValueTask<Boolean>> asyncPredicate );

      /// <summary>
      /// Checks that all items in this <see cref="IAsyncEnumerable{T}"/> adher to condition checked by given synchronous <paramref name="predicate"/>.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="predicate">The synchronous callback to check whether an element satifies some condition.</param>
      /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> is empty, or if all elements of the enumerable satisfy condition checked by <paramref name="predicate"/>; <c>false</c> otherwise.</returns>
      /// <seealso cref="System.Linq.Enumerable.All{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="predicate"/> is <c>null</c>.</exception>
      Task<Boolean> AllAsync<T>( IAsyncEnumerable<T> source, Func<T, Boolean> predicate );

      /// <summary>
      /// Checks that all items in this <see cref="IAsyncEnumerable{T}"/> adher to condition checked by given potentially asynchronous <paramref name="asyncPredicate"/>.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="asyncPredicate">The potentially asynchronous callback to check whether an element satifies some condition.</param>
      /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> is empty, or if all elements of the enumerable satisfy condition checked by <paramref name="asyncPredicate"/>; <c>false</c> otherwise.</returns>
      /// <seealso cref="System.Linq.Enumerable.All{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="asyncPredicate"/> is <c>null</c>.</exception>
      Task<Boolean> AllAsync<T>( IAsyncEnumerable<T> source, Func<T, ValueTask<Boolean>> asyncPredicate );
   }


}

public static partial class E_AsyncEnumeration
{
   /// <summary>
   /// Similarly to <see cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource})"/>, this method checks whether this <see cref="IAsyncEnumerable{T}"/> contains at least one element.
   /// </summary>
   /// <typeparam name="T">The type of elements being enumerated.</typeparam>
   /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> has at least one element; <c>false</c> otherwise.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource})"/>
   public static Task<Boolean> AnyAsync<T>( this IAsyncEnumerable<T> source )
      => ( source.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).AnyAsync( source );

   /// <summary>
   /// Similarly to <see cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>, this method checks whether this <see cref="IAsyncEnumerable{T}"/> contains at least one element, that satisfies condition checked by given <paramref name="predicate"/> synchronous callback.
   /// </summary>
   /// <typeparam name="T">The type of elements being enumerated.</typeparam>
   /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="predicate">The synchronous callback to check whether an element satifies some condition. If <c>null</c>, then the first item will always satisfy the condition.</param>
   /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> has at least one element that satifies condition checked by <paramref name="predicate"/>; <c>false</c> otherwise.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
   public static Task<Boolean> AnyAsync<T>( this IAsyncEnumerable<T> source, Func<T, Boolean> predicate )
      => ( source.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).AnyAsync( source, predicate );

   /// <summary>
   /// Similarly to <see cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>, this method checks whether this <see cref="IAsyncEnumerable{T}"/> contains at least one element, that satisfies condition checked by given <paramref name="asyncPredicate"/> potentially asynchronous callback.
   /// </summary>
   /// <typeparam name="T">The type of elements being enumerated.</typeparam>
   /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="asyncPredicate">The potentially asynchronous callback to check whether an element satifies some condition. If <c>null</c>, then the first item will always satisfy the condition.</param>
   /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> has at least one element that satifies condition checked by <paramref name="asyncPredicate"/>; <c>false</c> otherwise.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
   public static Task<Boolean> AnyAsync<T>( this IAsyncEnumerable<T> source, Func<T, ValueTask<Boolean>> asyncPredicate )
      => ( source.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).AnyAsync( source, asyncPredicate );

   /// <summary>
   /// Checks that all items in this <see cref="IAsyncEnumerable{T}"/> adher to condition checked by given synchronous <paramref name="predicate"/>.
   /// </summary>
   /// <typeparam name="T">The type of elements being enumerated.</typeparam>
   /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="predicate">The synchronous callback to check whether an element satifies some condition.</param>
   /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> is empty, or if all elements of the enumerable satisfy condition checked by <paramref name="predicate"/>; <c>false</c> otherwise.</returns>
   /// <seealso cref="System.Linq.Enumerable.All{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="predicate"/> is <c>null</c>.</exception>
   public static Task<Boolean> AllAsync<T>( this IAsyncEnumerable<T> source, Func<T, Boolean> predicate )
      => ( source.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).AllAsync( source, predicate );

   /// <summary>
   /// Checks that all items in this <see cref="IAsyncEnumerable{T}"/> adher to condition checked by given potentially asynchronous <paramref name="asyncPredicate"/>.
   /// </summary>
   /// <typeparam name="T">The type of elements being enumerated.</typeparam>
   /// <param name="source">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="asyncPredicate">The potentially asynchronous callback to check whether an element satifies some condition.</param>
   /// <returns>Potentially asynchronously returns <c>true</c> if this <see cref="IAsyncEnumerable{T}"/> is empty, or if all elements of the enumerable satisfy condition checked by <paramref name="asyncPredicate"/>; <c>false</c> otherwise.</returns>
   /// <seealso cref="System.Linq.Enumerable.All{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="asyncPredicate"/> is <c>null</c>.</exception>
   public static Task<Boolean> AllAsync<T>( this IAsyncEnumerable<T> source, Func<T, ValueTask<Boolean>> asyncPredicate )
      => ( source.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).AllAsync( source, asyncPredicate );
}
