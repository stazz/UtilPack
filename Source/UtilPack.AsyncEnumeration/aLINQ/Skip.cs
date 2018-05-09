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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.AsyncEnumeration;
using UtilPack.AsyncEnumeration.LINQ;

public static partial class E_UtilPack
{

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Skip{TSource}(IEnumerable{TSource}, int)"/>
   public static IAsyncEnumerable<T> Skip<T>( this IAsyncEnumerable<T> enumerable, Int32 amount )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      return amount <= 0 ? enumerable : new EnumerableWrapper<T>( () => enumerable.GetAsyncEnumerator().Skip( amount ) );
   }

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Skip{TSource}(IEnumerable{TSource}, int)"/>
   public static IAsyncEnumerable<T> Skip<T>( this IAsyncEnumerable<T> enumerable, Int64 amount )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      return amount <= 0 ? enumerable : new EnumerableWrapper<T>( () => enumerable.GetAsyncEnumerator().Skip( amount ) );
   }


   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerator{T}"/> which will return at most given amount of items.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerator">This <see cref="IAsyncEnumerator{T}"/>.</param>
   /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
   /// <returns><see cref="IAsyncEnumerator{T}"/> which will return at most given amount of items.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Skip{TSource}(IEnumerable{TSource}, int)"/>
   public static IAsyncEnumerator<T> Skip<T>( this IAsyncEnumerator<T> enumerator, Int32 amount )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      // We can use .Where here, since if underlying enumerator is never-ending, it's ok if .Skip is never-ending as well.
      return amount <= 0 ? enumerator : enumerator.Where( item => amount <= 0 || Interlocked.Decrement( ref amount ) < 0 );
   }

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerator{T}"/> which will return at most given amount of items.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerator">This <see cref="IAsyncEnumerator{T}"/>.</param>
   /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
   /// <returns><see cref="IAsyncEnumerator{T}"/> which will return at most given amount of items.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.Skip{TSource}(IEnumerable{TSource}, int)"/>
   public static IAsyncEnumerator<T> Skip<T>( this IAsyncEnumerator<T> enumerator, Int64 amount )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      return amount <= 0 ? enumerator : enumerator.Where( item => amount <= 0 || Interlocked.Decrement( ref amount ) < 0 );
   }

   /// <summary>
   /// This extension 
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="enumerable"></param>
   /// <param name="predicate"></param>
   /// <returns></returns>
   /// <seealso cref="System.Linq.Enumerable.SkipWhile{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
   public static IAsyncEnumerable<T> SkipWhile<T>( this IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( predicate ), predicate );
      return new EnumerableWrapper<T>( () => enumerable.GetAsyncEnumerator().SkipWhile( predicate ) );
   }

   /// <summary>
   /// 
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="enumerable"></param>
   /// <param name="asyncPredicate"></param>
   /// <returns></returns>
   /// <seealso cref="System.Linq.Enumerable.SkipWhile{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
   public static IAsyncEnumerable<T> SkipWhile<T>( this IAsyncEnumerable<T> enumerable, Func<T, ValueTask<Boolean>> asyncPredicate )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( asyncPredicate ), asyncPredicate );

      return new EnumerableWrapper<T>( () => enumerable.GetAsyncEnumerator().SkipWhile( asyncPredicate ) );
   }

   /// <summary>
   /// 
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="enumerator"></param>
   /// <param name="predicate"></param>
   /// <returns></returns>
   /// <seealso cref="System.Linq.Enumerable.SkipWhile{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
   public static IAsyncEnumerator<T> SkipWhile<T>( this IAsyncEnumerator<T> enumerator, Func<T, Boolean> predicate )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      ArgumentValidator.ValidateNotNull( nameof( predicate ), predicate );
      var falseSeen = 0;
      return enumerator.Where( item =>
      {
         if ( falseSeen == 0 && !predicate( item ) )
         {
            Interlocked.Exchange( ref falseSeen, 1 );
         }
         return falseSeen == 1;
      } );
   }

   /// <summary>
   /// 
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="enumerator"></param>
   /// <param name="asyncPredicate"></param>
   /// <returns></returns>
   /// <seealso cref="System.Linq.Enumerable.SkipWhile{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
   public static IAsyncEnumerator<T> SkipWhile<T>( this IAsyncEnumerator<T> enumerator, Func<T, ValueTask<Boolean>> asyncPredicate )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      ArgumentValidator.ValidateNotNull( nameof( asyncPredicate ), asyncPredicate );
      var falseSeen = 0;
      return enumerator.Where( async item =>
      {
         if ( falseSeen == 0 && !( await asyncPredicate( item ) ) )
         {
            Interlocked.Exchange( ref falseSeen, 1 );
         }
         return falseSeen == 1;
      } );
   }

}