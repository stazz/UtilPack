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
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.
      /// </summary>
      /// <typeparam name="T">The type of items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, Int32)"/>
      IAsyncEnumerable<T> Take<T>( IAsyncEnumerable<T> enumerable, Int32 amount );

      /// <summary>
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.
      /// </summary>
      /// <typeparam name="T">The type of items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, Int32)"/>
      IAsyncEnumerable<T> Take<T>( IAsyncEnumerable<T> enumerable, Int64 amount );

      /// <summary>
      /// This method returns new <see cref="IAsyncEnumerable{T}"/> that will include only the first elements of this <see cref="IAsyncEnumerable{T}"/> which satisfy condition expressed by given synchronous <paramref name="predicate"/>.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="predicate">The synchronous callback to check whether element satisfies condition.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will include only the first elements of this <see cref="IAsyncEnumerable{T}"/> which satisfy the condition expressed by <paramref name="predicate"/>.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="predicate"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.TakeWhile{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      IAsyncEnumerable<T> TakeWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate );

      /// <summary>
      /// This method returns new <see cref="IAsyncEnumerable{T}"/> that will include only the first elements of this <see cref="IAsyncEnumerable{T}"/> which satisfy condition expressed by given potentially asynchronous <paramref name="asyncPredicate"/>.
      /// </summary>
      /// <typeparam name="T">The type of elements being enumerated.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="asyncPredicate">The potentially asynchronous callback to check whether element satisfies condition.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will include only the first elements of this <see cref="IAsyncEnumerable{T}"/> which satisfy the condition expressed by <paramref name="asyncPredicate"/>.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentNullException">If <paramref name="asyncPredicate"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.TakeWhile{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      IAsyncEnumerable<T> TakeWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, Task<Boolean>> asyncPredicate );
   }

}


public static partial class E_AsyncEnumeration
{

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, Int32)"/>
   public static IAsyncEnumerable<T> Take<T>( this IAsyncEnumerable<T> enumerable, Int32 amount )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).Take( enumerable, amount );

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, Int32)"/>
   public static IAsyncEnumerable<T> Take<T>( this IAsyncEnumerable<T> enumerable, Int64 amount )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).Take( enumerable, amount );

   /// <summary>
   /// This method returns new <see cref="IAsyncEnumerable{T}"/> that will include only the first elements of this <see cref="IAsyncEnumerable{T}"/> which satisfy condition expressed by given synchronous <paramref name="predicate"/>.
   /// </summary>
   /// <typeparam name="T">The type of elements being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="predicate">The synchronous callback to check whether element satisfies condition.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will include only the first elements of this <see cref="IAsyncEnumerable{T}"/> which satisfy the condition expressed by <paramref name="predicate"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="predicate"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.TakeWhile{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
   public static IAsyncEnumerable<T> TakeWhile<T>( this IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).TakeWhile( enumerable, predicate );

   /// <summary>
   /// This method returns new <see cref="IAsyncEnumerable{T}"/> that will include only the first elements of this <see cref="IAsyncEnumerable{T}"/> which satisfy condition expressed by given potentially asynchronous <paramref name="asyncPredicate"/>.
   /// </summary>
   /// <typeparam name="T">The type of elements being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="asyncPredicate">The potentially asynchronous callback to check whether element satisfies condition.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will include only the first elements of this <see cref="IAsyncEnumerable{T}"/> which satisfy the condition expressed by <paramref name="asyncPredicate"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="asyncPredicate"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.TakeWhile{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
   public static IAsyncEnumerable<T> TakeWhile<T>( this IAsyncEnumerable<T> enumerable, Func<T, Task<Boolean>> asyncPredicate )
      => ( enumerable.AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException() ).TakeWhile( enumerable, asyncPredicate );

}