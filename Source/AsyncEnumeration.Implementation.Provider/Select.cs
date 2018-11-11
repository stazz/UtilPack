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

namespace AsyncEnumeration.Implementation.Provider
{

   public partial class DefaultAsyncProvider
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
      public IAsyncEnumerable<U> Select<T, U>( IAsyncEnumerable<T> enumerable, Func<T, U> selector )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         ArgumentValidator.ValidateNotNull( nameof( selector ), selector );
         return FromTransformCallback( enumerable, selector, ( e, s ) => new SelectEnumerator<T, U>( e, s ) );
      }

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
      public IAsyncEnumerable<U> Select<T, U>( IAsyncEnumerable<T> enumerable, Func<T, ValueTask<U>> asyncSelector )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         ArgumentValidator.ValidateNotNull( nameof( asyncSelector ), asyncSelector );
         return FromTransformCallback( enumerable, asyncSelector, ( e, s ) => new AsyncSelectEnumerator<T, U>( e, s ) );
      }
   }

   internal class SelectEnumerator<T, U> : IAsyncEnumerator<U>
   {
      private readonly IAsyncEnumerator<T> _source;
      private readonly Func<T, U> _selector;

      public SelectEnumerator(
         IAsyncEnumerator<T> source,
         Func<T, U> syncSelector
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._selector = ArgumentValidator.ValidateNotNull( nameof( syncSelector ), syncSelector );
      }

      public Task<Boolean> WaitForNextAsync() => this._source.WaitForNextAsync();

      public U TryGetNext( out Boolean success )
      {
         var item = this._source.TryGetNext( out success );
         return success ? this._selector( item ) : default;
      }

      public Task DisposeAsync() => this._source.DisposeAsync();
   }

   internal sealed class AsyncSelectEnumerator<T, U> : IAsyncEnumerator<U>
   {
      private readonly IAsyncEnumerator<T> _source;
      private readonly Func<T, ValueTask<U>> _selector;
      private readonly Stack<U> _stack;

      public AsyncSelectEnumerator(
         IAsyncEnumerator<T> source,
         Func<T, ValueTask<U>> asyncSelector
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._selector = ArgumentValidator.ValidateNotNull( nameof( asyncSelector ), asyncSelector );
         this._stack = new Stack<U>();
      }

      public async Task<Boolean> WaitForNextAsync()
      {
         var stack = this._stack;
         // Discard any previous items
         stack.Clear();
         // We must use the selector in this method, since this is our only asynchronous method while enumerating
         while ( stack.Count == 0 && await this._source.WaitForNextAsync() )
         {
            Boolean success;
            do
            {
               var item = this._source.TryGetNext( out success );
               if ( success )
               {
                  stack.Push( await this._selector( item ) );
               }
            } while ( success );
         }

         return stack.Count > 0;
      }


      public U TryGetNext( out Boolean success )
      {
         success = this._stack.Count > 0;
         return success ? this._stack.Pop() : default;
      }

      public Task DisposeAsync() => this._source.DisposeAsync();
   }

}
