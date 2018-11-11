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
      /// <seealso cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, Int32)"/>
      public IAsyncEnumerable<T> Take<T>( IAsyncEnumerable<T> enumerable, Int32 amount )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         return amount <= 0 ?
            EmptyAsync<T>.Enumerable :
            FromTransformCallback( enumerable, amount, ( e, a ) => new TakeEnumerator32<T>( e, a ) );
      }

      /// <summary>
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.
      /// </summary>
      /// <typeparam name="T">The type of items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="amount">The maximum amount of items to return. If zero or less, will return empty enumerable.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return at most given amount of items.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, Int32)"/>
      public IAsyncEnumerable<T> Take<T>( IAsyncEnumerable<T> enumerable, Int64 amount )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         return amount <= 0 ?
            EmptyAsync<T>.Enumerable :
            FromTransformCallback( enumerable, amount, ( e, a ) => new TakeEnumerator64<T>( e, a ) );
      }

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
      public IAsyncEnumerable<T> TakeWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         ArgumentValidator.ValidateNotNull( nameof( predicate ), predicate );
         return FromTransformCallback( enumerable, predicate, ( e, p ) => new TakeWhileEnumeratorSync<T>( e, p ) );
      }

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
      public IAsyncEnumerable<T> TakeWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, Task<Boolean>> asyncPredicate )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         ArgumentValidator.ValidateNotNull( nameof( asyncPredicate ), asyncPredicate );
         return FromTransformCallback( enumerable, asyncPredicate, ( e, p ) => new TakeWhileEnumeratorAsync<T>( e, p ) );
      }
   }

   internal sealed class TakeEnumerator32<T> : IAsyncEnumerator<T>
   {
      private readonly IAsyncEnumerator<T> _source;
      private Int32 _amount;

      public TakeEnumerator32(
         IAsyncEnumerator<T> source,
         Int32 amount
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._amount = Math.Max( amount, 0 );
      }

      public Task<Boolean> WaitForNextAsync() => this._amount <= 0 ? TaskUtils.False : this._source.WaitForNextAsync();

      public T TryGetNext( out Boolean success )
      {
         success = this._amount > 0;
         var retVal = success ? this._source.TryGetNext( out success ) : default;
         if ( success )
         {
            --this._amount;
         }
         return retVal;
      }

      public Task DisposeAsync() => this._source.DisposeAsync();
   }

   internal sealed class TakeEnumerator64<T> : IAsyncEnumerator<T>
   {
      private readonly IAsyncEnumerator<T> _source;
      private Int64 _amount;

      public TakeEnumerator64(
         IAsyncEnumerator<T> source,
         Int64 amount
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._amount = Math.Max( amount, 0 );
      }

      public Task<Boolean> WaitForNextAsync() => this._amount <= 0 ? TaskUtils.False : this._source.WaitForNextAsync();

      public T TryGetNext( out Boolean success )
      {
         success = this._amount > 0;
         var retVal = success ? this._source.TryGetNext( out success ) : default;
         if ( success )
         {
            --this._amount;
         }
         return retVal;
      }

      public Task DisposeAsync() => this._source.DisposeAsync();
   }

   internal sealed class TakeWhileEnumeratorSync<T> : IAsyncEnumerator<T>
   {
      private const Int32 FALSE_NOT_SEEN = 0;
      private const Int32 FALSE_SEEN = 1;

      private readonly IAsyncEnumerator<T> _source;
      private readonly Func<T, Boolean> _predicate;
      private Int32 _state;

      public TakeWhileEnumeratorSync(
         IAsyncEnumerator<T> source,
         Func<T, Boolean> predicate
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._predicate = ArgumentValidator.ValidateNotNull( nameof( predicate ), predicate );
      }

      public Task<Boolean> WaitForNextAsync() => this._state == FALSE_NOT_SEEN ? this._source.WaitForNextAsync() : TaskUtils.False;

      public T TryGetNext( out Boolean success )
      {
         success = this._state == FALSE_NOT_SEEN;
         T retVal;
         if ( success )
         {
            retVal = this._source.TryGetNext( out success );
            if ( success )
            {
               success = this._predicate( retVal );
               if ( !success )
               {
                  this._state = FALSE_SEEN;
                  retVal = default;
               }
            }
         }
         else
         {
            retVal = default;
         }
         return retVal;
      }

      public Task DisposeAsync() => this._source.DisposeAsync();
   }

   internal sealed class TakeWhileEnumeratorAsync<T> : IAsyncEnumerator<T>
   {
      private const Int32 FALSE_NOT_SEEN = 0;
      private const Int32 FALSE_SEEN = 1;

      private readonly IAsyncEnumerator<T> _source;
      private readonly Func<T, Task<Boolean>> _predicate;
      private readonly Stack<T> _stack;
      private Int32 _state;

      public TakeWhileEnumeratorAsync(
         IAsyncEnumerator<T> source,
         Func<T, Task<Boolean>> asyncPredicate
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._predicate = ArgumentValidator.ValidateNotNull( nameof( asyncPredicate ), asyncPredicate );
         this._stack = new Stack<T>();
      }

      public Task<Boolean> WaitForNextAsync()
      {
         return this._state == FALSE_NOT_SEEN ?
            this.PeekNextAsync() :
            TaskUtils.False;
      }

      public T TryGetNext( out Boolean success )
      {
         success = this._stack.Count > 0;
         return success ? this._stack.Pop() : default;
      }

      public Task DisposeAsync() => this._source.DisposeAsync();

      private async Task<Boolean> PeekNextAsync()
      {
         var stack = this._stack;
         // Discard any previous items
         stack.Clear();
         var falseNotSeen = true;
         while ( falseNotSeen && stack.Count == 0 && await this._source.WaitForNextAsync() )
         {
            Boolean success;
            do
            {
               var item = this._source.TryGetNext( out success );
               if ( success )
               {
                  if ( await this._predicate( item ) )
                  {
                     stack.Push( item );
                  }
                  else
                  {
                     this._state = FALSE_SEEN;
                     success = false;
                     falseNotSeen = false;
                  }
               }
            } while ( success );
         }

         return stack.Count > 0;
      }
   }


}
