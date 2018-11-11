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

namespace AsyncEnumeration.Implementation.Enumerable
{
   internal sealed class SingletonEnumerator<T> : IAsyncEnumerator<T>
   {
      private const Int32 STATE_INITIAL = 0;
      private const Int32 STATE_WAIT_CALLED = 1;
      private const Int32 STATE_GET_CALLED = 2;

      private readonly T _value;
      private Int32 _state;

      public SingletonEnumerator( T value )
         => this._value = value;

      public Task<Boolean> WaitForNextAsync()
         => TaskUtils.TaskFromBoolean( Interlocked.CompareExchange( ref this._state, STATE_WAIT_CALLED, STATE_INITIAL ) == STATE_INITIAL );

      public T TryGetNext( out Boolean success )
      {
         success = Interlocked.CompareExchange( ref this._state, STATE_GET_CALLED, STATE_WAIT_CALLED ) == STATE_WAIT_CALLED;
         return success ? this._value : default;
      }

      public Task DisposeAsync()
         => TaskUtils.CompletedTask;
   }

   internal sealed class AsyncSingletonEnumerator<T> : IAsyncEnumerator<T>
   {
      private const Int32 STATE_INITIAL = 0;
      private const Int32 STATE_WAIT_CALLED = 1;
      private const Int32 STATE_GET_CALLED = 2;

      private readonly Task<T> _asyncValue;
      private Int32 _state;

      public AsyncSingletonEnumerator( Task<T> asyncValue )
         => this._asyncValue = ArgumentValidator.ValidateNotNull( nameof( asyncValue ), asyncValue );

      public Task<Boolean> WaitForNextAsync()
      {
         Task<Boolean> retVal;
         if ( Interlocked.CompareExchange( ref this._state, STATE_WAIT_CALLED, STATE_INITIAL ) == STATE_INITIAL )
         {
            if ( this._asyncValue.IsCompleted )
            {
               retVal = TaskUtils.True;
            }
            else
            {
               retVal = this.ReallyWaitForNextAsync();
            }
         }
         else
         {
            retVal = TaskUtils.False;
         }
         return retVal;
      }

      public T TryGetNext( out Boolean success )
      {
         success = Interlocked.CompareExchange( ref this._state, STATE_GET_CALLED, STATE_WAIT_CALLED ) == STATE_WAIT_CALLED;
         return success ? this._asyncValue.Result : default;
      }

      public Task DisposeAsync()
         => TaskUtils.CompletedTask;

      private async Task<Boolean> ReallyWaitForNextAsync()
      {
         await this._asyncValue;
         return true;
      }
   }

   internal sealed class ValueTaskAsyncSingletonEnumerator<T> : IAsyncEnumerator<T>
   {
      private const Int32 STATE_INITIAL = 0;
      private const Int32 STATE_WAIT_CALLED = 1;
      private const Int32 STATE_GET_CALLED = 2;

      private readonly ValueTask<T> _asyncValue;
      private Int32 _state;

      public ValueTaskAsyncSingletonEnumerator( ValueTask<T> asyncValue )
      {
         this._asyncValue = asyncValue;
      }

      public Task<Boolean> WaitForNextAsync()
      {
         Task<Boolean> retVal;
         if ( Interlocked.CompareExchange( ref this._state, STATE_WAIT_CALLED, STATE_INITIAL ) == STATE_INITIAL )
         {
            if ( this._asyncValue.IsCompleted )
            {
               retVal = TaskUtils.True;
            }
            else
            {
               retVal = this.ReallyWaitForNextAsync();
            }
         }
         else
         {
            retVal = TaskUtils.False;
         }
         return retVal;
      }

      public T TryGetNext( out Boolean success )
      {
         success = Interlocked.CompareExchange( ref this._state, STATE_GET_CALLED, STATE_WAIT_CALLED ) == STATE_WAIT_CALLED;
         return success ? this._asyncValue.Result : default;
      }

      public Task DisposeAsync() =>
         TaskUtils.CompletedTask;

      private async Task<Boolean> ReallyWaitForNextAsync()
      {
         await this._asyncValue;
         return true;
      }
   }



   public static partial class AsyncEnumerationExtensions
   {
      /// <summary>
      /// Encapsulates this single value as <see cref="IAsyncEnumerable{T}"/> containing only this value.
      /// </summary>
      /// <typeparam name="T">The type of this value.</typeparam>
      /// <param name="value">This value</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> containing only this value.</returns>
      public static IAsyncEnumerable<T> AsSingletonAsync<T>(
         this T value,
         IAsyncProvider aLINQProvider = null
         )
      {
         return AsyncEnumerationFactory.FromGeneratorCallback( value, v => new SingletonEnumerator<T>( v ), aLINQProvider );
      }

      /// <summary>
      /// Encapsulates this asynchronous value as <see cref="IAsyncEnumerable{T}"/> containing only this value.
      /// </summary>
      /// <typeparam name="T">The type of this value.</typeparam>
      /// <param name="task">The task acquiring this value.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> containing only this value.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="Task{TResult}"/> is <c>null</c>.</exception>
      public static IAsyncEnumerable<T> AsSingletonAsync<T>(
         this Task<T> task,
         IAsyncProvider aLINQProvider = null
         ) => AsyncEnumerationFactory.FromGeneratorCallback( ArgumentValidator.ValidateNotNullReference( task ), t => new AsyncSingletonEnumerator<T>( t ), aLINQProvider );

      /// <summary>
      /// Encapsulates this asynchronous value as <see cref="IAsyncEnumerable{T}"/> containing only this value.
      /// </summary>
      /// <typeparam name="T">The type of this value.</typeparam>
      /// <param name="task">The task acquiring this value.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> containing only this value.</returns>
      public static IAsyncEnumerable<T> AsSingletonAsync<T>(
         this ValueTask<T> task,
         IAsyncProvider aLINQProvider = null
         ) => AsyncEnumerationFactory.FromGeneratorCallback( task, t => new ValueTaskAsyncSingletonEnumerator<T>( t ), aLINQProvider );

   }
}
