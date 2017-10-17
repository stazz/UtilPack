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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.AsyncEnumeration;

namespace UtilPack.AsyncEnumeration
{
   internal sealed class AsyncSequentialOnlyEnumerable<T> : IAsyncEnumerable<T>
   {
      private readonly Func<SequentialEnumerationStartInfo<T>> _enumerationStart;

      public AsyncSequentialOnlyEnumerable(
         Func<SequentialEnumerationStartInfo<T>> enumerationStart
         )
      {
         this._enumerationStart = ArgumentValidator.ValidateNotNull( nameof( enumerationStart ), enumerationStart );
      }

      public IAsyncEnumerator<T> GetAsyncEnumerator()
      {
         var startInfo = this._enumerationStart();
         return AsyncEnumerationFactory.CreateSequentialEnumerator( startInfo.MoveNext, startInfo.Dispose );
      }
   }

   internal sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>
   {
      private const Int32 STATE_INITIAL = 0;
      private const Int32 MOVE_NEXT_STARTED = 1;
      private const Int32 MOVE_NEXT_ENDED = 2;
      private const Int32 STATE_ENDED = 3;
      private const Int32 DISPOSING = 4;
      private const Int32 DISPOSED = 5;

      private const Int32 MOVE_NEXT_STARTED_CURRENT_NOT_READ = 6;
      private const Int32 MOVE_NEXT_STARTED_CURRENT_READING = 7;

      private Int32 _state;
      private readonly SequentialEnumeratorCurrentInfo<T> _current;

      public AsyncEnumerator(
         SequentialEnumeratorCurrentInfo<T> currentInfo
         )
      {
         this._state = STATE_INITIAL;
         this._current = ArgumentValidator.ValidateNotNull( nameof( currentInfo ), currentInfo );
      }

      //public Boolean IsConcurrentEnumerationSupported => false;

      public async Task<Boolean> WaitForNextAsync()
      {
         // We can call move next only in initial state, or after we have called it once
         Boolean success = false;
         //Int32 prevState;
         if (
            /*( prevState = */Interlocked.CompareExchange( ref this._state, MOVE_NEXT_STARTED, MOVE_NEXT_ENDED )/* ) */== MOVE_NEXT_ENDED // TryGetNext was called and returned false
            || /*( prevState = */Interlocked.CompareExchange( ref this._state, MOVE_NEXT_STARTED, MOVE_NEXT_STARTED_CURRENT_NOT_READ ) /* ) */ == MOVE_NEXT_STARTED_CURRENT_NOT_READ // TryGetNext was not called
            || /*( prevState = */Interlocked.CompareExchange( ref this._state, MOVE_NEXT_STARTED, STATE_INITIAL ) /* ) */ == STATE_INITIAL // Initial call
            )
         {
            T current = default;
            try
            {

               var moveNext = this._current.MoveNext;
               if ( moveNext == null )
               {
                  success = false;
               }
               else
               {
                  (success, current) = await moveNext();
                  if ( success )
                  {
                     this._current.Current = current;
                  }
               }
            }
            finally
            {
               Interlocked.Exchange( ref this._state, success ? MOVE_NEXT_STARTED_CURRENT_NOT_READ : STATE_ENDED );
            }
         }
         else
         {
            // Re-entrancy or concurrent with Reset -> exception
            // TODO -> Maybe use await + Interlocked.CompareExchange-loop to wait... ? Waiting is always prone to deadlocks though.
            throw new InvalidOperationException( "Tried to concurrently move to next or reset." );
         }

         return success;
      }

      public T TryGetNext( out Boolean success )
      {
         var cur = this._current;
         success = Interlocked.CompareExchange( ref this._state, MOVE_NEXT_STARTED_CURRENT_READING, MOVE_NEXT_STARTED_CURRENT_NOT_READ ) == MOVE_NEXT_STARTED_CURRENT_NOT_READ;
         if ( success )
         {
            try
            {
               return cur.Current;
            }
            finally
            {
               Interlocked.Exchange( ref this._state, MOVE_NEXT_ENDED );
            }
         }
         else
         {
            return default;
         }

      }

      public async Task DisposeAsync()
      {
         // We can dispose from STATE_INITIAL, MOVE_NEXT_ENDED, STATE_ENDED, and MOVE_NEXT_STARTED_CURRENT_NOT_READ states
         Int32 prevState;
         if (
            ( prevState = Interlocked.CompareExchange( ref this._state, DISPOSING, STATE_ENDED ) ) == STATE_ENDED
            || ( prevState = Interlocked.CompareExchange( ref this._state, DISPOSING, MOVE_NEXT_ENDED ) ) == MOVE_NEXT_ENDED
            || ( prevState = Interlocked.CompareExchange( ref this._state, DISPOSING, STATE_INITIAL ) ) == STATE_INITIAL
            || ( prevState = Interlocked.CompareExchange( ref this._state, DISPOSING, MOVE_NEXT_STARTED_CURRENT_NOT_READ ) ) == MOVE_NEXT_STARTED_CURRENT_NOT_READ
            )
         {
            try
            {
               var task = this._current.Dispose?.Invoke();
               if ( task != null )
               {
                  await task;
               }
            }
            finally
            {
               Interlocked.Exchange( ref this._state, DISPOSED );
            }
         }
         else
         {
            throw prevState == DISPOSED ?
               new ObjectDisposedException( this.GetType().FullName ) :
               new InvalidOperationException( "Enumerator can not be disposed at this stage." );
         }
      }

   }

   internal abstract class SequentialEnumeratorCurrentInfo<T>
   {
      public SequentialEnumeratorCurrentInfo(
         MoveNextAsyncDelegate<T> moveNext,
         EnumerationEndedDelegate disposeDelegate
      )
      {
         this.MoveNext = moveNext;
         this.Dispose = disposeDelegate;
      }

      public MoveNextAsyncDelegate<T> MoveNext { get; }
      public EnumerationEndedDelegate Dispose { get; }

      public abstract T Current { get; set; }
   }

   internal sealed class SequentialEnumeratorCurrentInfoWithObject<T> : SequentialEnumeratorCurrentInfo<T>
   {
      private Object _current;

      public SequentialEnumeratorCurrentInfoWithObject(
         MoveNextAsyncDelegate<T> moveNext,
         EnumerationEndedDelegate disposeDelegate
         ) : base( moveNext, disposeDelegate )
      {
      }

      public override T Current
      {
         get => (T) this._current;
         set => Interlocked.Exchange( ref this._current, value );
      }

   }

   internal sealed class SequentialEnumeratorCurrentInfoWithInt32 : SequentialEnumeratorCurrentInfo<Int32>
   {
      private Int32 _current;

      public SequentialEnumeratorCurrentInfoWithInt32(
         MoveNextAsyncDelegate<Int32> moveNext,
         EnumerationEndedDelegate disposeDelegate
         ) : base( moveNext, disposeDelegate )
      {
      }

      public override Int32 Current
      {
         get => this._current;
         set => Interlocked.Exchange( ref this._current, value );
      }
   }

   internal sealed class SequentialEnumeratorCurrentInfoWithInt64 : SequentialEnumeratorCurrentInfo<Int64>
   {
      private Int64 _current;

      public SequentialEnumeratorCurrentInfoWithInt64(
         MoveNextAsyncDelegate<Int64> moveNext,
         EnumerationEndedDelegate disposeDelegate
         ) : base( moveNext, disposeDelegate )
      {
      }

      public override Int64 Current
      {
         get => Interlocked.Read( ref this._current );
         set => Interlocked.Exchange( ref this._current, value );
      }
   }

   internal sealed class SequentialEnumeratorCurrentInfoWithFloat32 : SequentialEnumeratorCurrentInfo<Single>
   {
      private Single _current;

      public SequentialEnumeratorCurrentInfoWithFloat32(
         MoveNextAsyncDelegate<Single> moveNext,
         EnumerationEndedDelegate disposeDelegate
         ) : base( moveNext, disposeDelegate )
      {
      }

      public override Single Current
      {
         get => this._current;
         set => Interlocked.Exchange( ref this._current, value );
      }
   }

   internal sealed class SequentialEnumeratorCurrentInfoWithFloat64 : SequentialEnumeratorCurrentInfo<Double>
   {
      private Int64 _current;

      public SequentialEnumeratorCurrentInfoWithFloat64(
         MoveNextAsyncDelegate<Double> moveNext,
         EnumerationEndedDelegate disposeDelegate
         ) : base( moveNext, disposeDelegate )
      {
      }

      public override Double Current
      {
         get => BitConverter.Int64BitsToDouble( Interlocked.Read( ref this._current ) );
         set => Interlocked.Exchange( ref this._current, BitConverter.DoubleToInt64Bits( value ) );
      }
   }
}

/// <summary>
/// This class contains extension methods for UtilPack types.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// This is helper method to sequentially enumerate a <see cref="IAsyncEnumerator{T}"/> and properly dispose it in case of exception.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="action">The callback to invoke for each item. May be <c>null</c>.</param>
   /// <returns>A task which will have enumerated the <see cref="IAsyncEnumerable{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   /// <remarks>
   /// Sequential enumeration means that the next invocation of <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/> will not start until the given callback <paramref name="action"/> is completed.
   /// </remarks>
   public static ValueTask<Int64> EnumerateSequentiallyAsync<T>( this IAsyncEnumerable<T> enumerable, Action<T> action )
   {
      return ArgumentValidator.ValidateNotNullReference( enumerable )
         .GetAsyncEnumerator()
         .EnumerateSequentiallyAsync( action );
   }

   /// <summary>
   /// This is helper method to sequentially enumerate a <see cref="IAsyncEnumerable{T}"/> and properly dispose it in case of an exception.
   /// For each item, a task from given callback is awaited for, if it is not <c>null</c>.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="asyncAction">The callback to invoke for each item. May be <c>null</c>, and may also return <c>null</c>.</param>
   /// <returns>A task which will have enumerated the <see cref="IAsyncEnumerable{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   /// <remarks>
   ///  Sequential enumeration means that the next invocation of <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/> will not start until the given callback <paramref name="asyncAction"/> is completed, synchronously or asynchronously.
   /// </remarks>
   public static ValueTask<Int64> EnumerateSequentiallyAsync<T>( this IAsyncEnumerable<T> enumerable, Func<T, Task> asyncAction )
   {
      return ArgumentValidator.ValidateNotNullReference( enumerable )
         .GetAsyncEnumerator()
         .EnumerateSequentiallyAsync( asyncAction );
   }

   private static async ValueTask<Int64> EnumerateSequentiallyAsync<T>( this IAsyncEnumerator<T> enumerator, Action<T> action, Boolean skipDisposeCall = false )
   {
      //var gracefulEnd = false;
      try
      {
         var retVal = 0L;
         while ( await enumerator.WaitForNextAsync() )
         {
            Boolean success;
            do
            {
               var item = enumerator.TryGetNext( out success );
               if ( success )
               {
                  ++retVal;
                  action?.Invoke( item );
               }
            } while ( success );
         }
         //gracefulEnd = true;
         return retVal;
      }
      finally
      {
         if ( !skipDisposeCall )
         {
            await enumerator.DisposeAsync(); // enumerator.CallEnumerationEndedWithinFinally( gracefulEnd );
         }
      }
   }

   private static async ValueTask<Int64> EnumerateSequentiallyAsync<T>( this IAsyncEnumerator<T> enumerator, Func<T, Task> asyncAction )
   {
      //var gracefulEnd = false;
      try
      {
         var retVal = 0L;
         while ( await enumerator.WaitForNextAsync() )
         {
            Boolean success;
            do
            {
               var item = enumerator.TryGetNext( out success );
               if ( success )
               {
                  ++retVal;
                  var task = asyncAction?.Invoke( item );
                  if ( task != null )
                  {
                     await task;
                  }
               }
            } while ( success );
         }
         //gracefulEnd = true;
         return retVal;
      }
      finally
      {
         await enumerator.DisposeAsync(); //.CallEnumerationEndedWithinFinally( gracefulEnd );
      }
   }

}