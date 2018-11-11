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
      /// <summary>
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.
      /// </summary>
      /// <typeparam name="T">The type of items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.Skip{TSource}(IEnumerable{TSource}, Int32)"/>
      public IAsyncEnumerable<T> Skip<T>( IAsyncEnumerable<T> enumerable, Int32 amount )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         return amount <= 0 ?
            enumerable :
            this.Where( enumerable, item => amount <= 0 || Interlocked.Decrement( ref amount ) < 0 );
      }

      /// <summary>
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.
      /// </summary>
      /// <typeparam name="T">The type of items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.Skip{TSource}(IEnumerable{TSource}, Int32)"/>
      public IAsyncEnumerable<T> Skip<T>( IAsyncEnumerable<T> enumerable, Int64 amount )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         return amount <= 0 ?
            enumerable :
            this.Where( enumerable, item => amount <= 0 || Interlocked.Decrement( ref amount ) < 0 );
      }


      /// <summary>
      /// This extension 
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="enumerable"></param>
      /// <param name="predicate"></param>
      /// <returns></returns>
      /// <seealso cref="System.Linq.Enumerable.SkipWhile{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      public IAsyncEnumerable<T> SkipWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         ArgumentValidator.ValidateNotNull( nameof( predicate ), predicate );
         var falseSeen = 0;
         return this.Where( enumerable, item =>
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
      /// <param name="enumerable"></param>
      /// <param name="asyncPredicate"></param>
      /// <returns></returns>
      /// <seealso cref="System.Linq.Enumerable.SkipWhile{TSource}(IEnumerable{TSource}, Func{TSource, Boolean})"/>
      public IAsyncEnumerable<T> SkipWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, ValueTask<Boolean>> asyncPredicate )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         ArgumentValidator.ValidateNotNull( nameof( asyncPredicate ), asyncPredicate );
         var falseSeen = 0;
         return this.Where( enumerable, async item =>
         {
            if ( falseSeen == 0 && !( await asyncPredicate( item ) ) )
            {
               Interlocked.Exchange( ref falseSeen, 1 );
            }
            return falseSeen == 1;
         } );
      }
   }
}
