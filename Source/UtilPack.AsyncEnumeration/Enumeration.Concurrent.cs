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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack.AsyncEnumeration;
using System.Linq;

using TConcurrentExceptionList = System.Collections.
#if NETSTANDARD1_0
   Generic.List
#else
   Concurrent.ConcurrentBag
#endif
   <System.Exception>;
using UtilPack;

namespace UtilPack.AsyncEnumeration
{
   /// <summary>
   /// This interface extends <see cref="IAsyncEnumerable{T}"/> to provide ability to enumerate concurrently asynchronously - the task returned by enumeration callback is not waited before acquiring next item.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated. This parameter is covariant.</typeparam>
   /// <seealso cref="E_UtilPack.AsConcurrentEnumerable{T}(IAsyncEnumerable{T})"/>
   /// <seealso cref="AsyncEnumerationFactory.CreateConcurrentEnumerable{T, TState}(Func{ConcurrentEnumerationStartInfo{T, TState}})"/>
   /// <seealso cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Action{T}, ConcurrentEnumerationArguments)"/>
   /// <seealso cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Func{T, Task}, ConcurrentEnumerationArguments)"/>
   public interface IAsyncConcurrentEnumerable<out T> : IAsyncEnumerable<T>
   {
      /// <summary>
      /// This method will return <see cref="IAsyncConcurrentEnumeratorSource{T}"/> that can be used to enumerate this <see cref="IAsyncConcurrentEnumerable{T}"/> concurrently.
      /// </summary>
      /// <param name="arguments">The <see cref="ConcurrentEnumerationArguments"/> specifying how the returned <see cref="IAsyncConcurrentEnumeratorSource{T}"/> will behave concurrently-wise.</param>
      /// <returns>A <see cref="IAsyncConcurrentEnumeratorSource{T}"/> that can be used to enumerate concurrently.</returns>
      /// <remarks>
      /// Typically this method is not used directly, but instead <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Action{T}, ConcurrentEnumerationArguments)"/> or <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Func{T, Task}, ConcurrentEnumerationArguments)"/> methods are used.
      /// </remarks>
      IAsyncConcurrentEnumeratorSource<T> GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments );
   }

   /// <summary>
   /// This interface provides a source to enumerate <see cref="IAsyncConcurrentEnumerable{T}"/> concurrently.
   /// Typically, this interface is not used directly, but instead the <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Action{T}, ConcurrentEnumerationArguments)"/> or <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Func{T, Task}, ConcurrentEnumerationArguments)"/> methods are used.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated. This parameter is covariant.</typeparam>
   public interface IAsyncConcurrentEnumeratorSource<out T> : IAsyncDisposable
   {
      /// <summary>
      /// Gets the synchronous <see cref="IEnumerable{T}"/> containing all <see cref="IAsyncEnumerator{T}"/> objects that will be enumerated concurrently.
      /// </summary>
      /// <returns>The synchronous <see cref="IEnumerable{T}"/> containing all <see cref="IAsyncEnumerator{T}"/> objects that will be enumerated concurrently.</returns>
      /// <remarks>
      /// This method is invoked first by <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Action{T}, ConcurrentEnumerationArguments)"/> and <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Func{T, Task}, ConcurrentEnumerationArguments)"/>.
      /// If this returns <c>null</c>, then <see cref="GetWrappedSynchronousSource"/> is used.
      /// </remarks>
      IEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsEnumerable();

      /// <summary>
      /// This method will be used *only* if <see cref="GetAsyncEnumeratorsEnumerable"/> returns <c>null</c>.
      /// </summary>
      /// <returns>Wrapped synchronous enumeration source, or <c>null</c>.</returns>
      /// <remarks>
      /// The behaviour of <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Action{T}, ConcurrentEnumerationArguments)"/> and <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Func{T, Task}, ConcurrentEnumerationArguments)"/> when using this method is to simply start a new task which will execute the given callback, and then immediately enumerate next item of sequential <see cref="IAsyncEnumerator{T}"/>.
      /// </remarks>
      IAsyncEnumerable<T> GetWrappedSynchronousSource();

      ///// <summary>
      ///// Gets the asynchronous <see cref="IAsyncEnumerable{T}"/> containing all <see cref="IAsyncEnumerator{T}"/> objects that will be enumerated concurrently.
      ///// The <see cref="IAsyncEnumerable{T}"/> itself will be enumerated sequentially.
      ///// </summary>
      ///// <returns>The asynchronous <see cref="IAsyncEnumerable{T}"/> containing all <see cref="IAsyncEnumerator{T}"/> objects that will be enumerated concurrently.</returns>
      ///// <remarks>
      ///// This method is invoked by <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Action{T}, ConcurrentEnumerationArguments)"/> and <see cref="E_UtilPack.EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Func{T, Task}, ConcurrentEnumerationArguments)"/> only when <see cref="GetAsyncEnumeratorsEnumerable"/> returns <c>null</c>.
      ///// </remarks>
      //IAsyncEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsAsyncEnumerable();
   }

   /// <summary>
   /// This struct contains arguments affecting how <see cref="IAsyncConcurrentEnumeratorSource{T}"/> will behave when created by <see cref="IAsyncConcurrentEnumerable{T}.GetConcurrentEnumeratorSource"/>
   /// </summary>
   public struct ConcurrentEnumerationArguments
   {
      /// <summary>
      /// Creates a new instance of <see cref="ConcurrentEnumerationArguments"/>.
      /// </summary>
      /// <param name="maximumConcurrentTasks">The maximum amount of concurrent tasks when enumerating. If ≤ 0, then enumeration will have no concurrent task limit.</param>
      public ConcurrentEnumerationArguments( Int32 maximumConcurrentTasks )
      {
         this.MaximumConcurrentTasks = maximumConcurrentTasks;
      }

      /// <summary>
      /// Gets the maximum amount of concurrent tasks when enumerating.
      /// If ≤ 0, then enumeration will have no concurrent task limit.
      /// </summary>
      /// <value>The maximum amount of concurrent tasks when enumerating.</value>
      public Int32 MaximumConcurrentTasks { get; }
   }

   internal sealed class AsyncConcurrentEnumeratorSourceForSyncLoop<T> : IAsyncConcurrentEnumeratorSource<T>
   {
      private readonly IEnumerable<IAsyncEnumerator<T>> _enumerators;
      private readonly EnumerationEndedDelegate _dispose;

      public AsyncConcurrentEnumeratorSourceForSyncLoop(
         IEnumerable<IAsyncEnumerator<T>> enumerators,
         EnumerationEndedDelegate dispose
         )
      {
         this._enumerators = ArgumentValidator.ValidateNotNull( nameof( enumerators ), enumerators );
         this._dispose = dispose;
      }

      public IEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsEnumerable()
         => this._enumerators;

      //public IAsyncEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsAsyncEnumerable()
      //   => null;

      public IAsyncEnumerable<T> GetWrappedSynchronousSource()
         => null;

      public Task DisposeAsync() =>
         this._dispose?.Invoke() ?? TaskUtils.CompletedTask;
   }

   internal sealed class AsyncConcurrentEnumeratorSourceForAsyncLoop<T> : IAsyncConcurrentEnumeratorSource<T>
   {
      private readonly IAsyncEnumerable<T> _source;

      public AsyncConcurrentEnumeratorSourceForAsyncLoop(
         IAsyncEnumerable<T> source
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
      }

      public IEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsEnumerable()
         => null;

      public IAsyncEnumerable<T> GetWrappedSynchronousSource() // IAsyncEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsAsyncEnumerable()
         => this._source;

      public Task DisposeAsync() =>
         TaskUtils.CompletedTask;
   }

   internal sealed class AsyncConcurrentEnumerable<T, TState> : IAsyncConcurrentEnumerable<T>
   {
      private readonly Func<ConcurrentEnumerationStartInfo<T, TState>> _factory;

      public AsyncConcurrentEnumerable(
         Func<ConcurrentEnumerationStartInfo<T, TState>> enumerationStart
         )
      {
         this._factory = ArgumentValidator.ValidateNotNull( nameof( enumerationStart ), enumerationStart );
      }

      public IAsyncEnumerator<T> GetAsyncEnumerator()
      {
         var info = this._factory();
         var hasNext = info.HasNext;
         var getNext = info.GetNext;
         return AsyncEnumerationFactory.CreateSequentialEnumerator(
            hasNext == null || getNext == null ? default( MoveNextAsyncDelegate<T> ) : async () =>
            {
               (var hasMore, var state) = hasNext();
               return (hasMore, hasMore ? await getNext( state ) : default);
            },
            info.Dispose
            );
      }

      public IAsyncConcurrentEnumeratorSource<T> GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments )
      {
         var max = arguments.MaximumConcurrentTasks;
         var info = this._factory();
         var hasNext = info.HasNext;
         var getNext = info.GetNext;
         return new AsyncConcurrentEnumeratorSourceForSyncLoop<T>(
            hasNext == null || getNext == null ? Empty<IAsyncEnumerator<T>>.Enumerable : ( max <= 0 ? this.GetAsyncEnumeratorsUnlimited( hasNext, getNext ) : this.GetAsyncEnumeratorsLimited( hasNext, getNext, max ) ),
            info.Dispose
            );
      }

      private IEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsUnlimited(
         HasNextDelegate<TState> hasNext,
         GetNextItemAsyncDelegate<T, TState> getNext
         )
      {
         (Boolean, TState) tuple;
         while ( ( tuple = hasNext() ).Item1 )
         {
            yield return new LINQ.ValueTaskAsyncSingletonEnumerator<T>( getNext( tuple.Item2 ) );
         }
      }

      private IEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsLimited(
         HasNextDelegate<TState> hasNext,
         GetNextItemAsyncDelegate<T, TState> getNext,
         Int32 max
         )
      {
         var sema = new SemaphoreSlim( max, max );
         return Enumerable.Repeat(
            AsyncEnumerationFactory.CreateSequentialEnumerator(
               async () =>
               {
                  await sema.WaitAsync();
                  try
                  {
                     (var hasMore, var state) = hasNext();
                     return (hasMore, hasMore ? await getNext( state ) : default);
                  }
                  finally
                  {
                     sema.Release();
                  }
               },
               default
               ),
            max
            );
      }
   }

   internal sealed class AsyncConcurrentEnumerableWrapper<T> : IAsyncConcurrentEnumerable<T>
   {
      private const Int32 STATE_INITIAL = 0;
      private const Int32 WAIT_RETURNED_TRUE = 1;
      private const Int32 WAIT_RETURNED_FALSE = 2;
      private const Int32 GET_RETURNED_TRUE = 3;
      private readonly IAsyncEnumerable<T> _source;

      public AsyncConcurrentEnumerableWrapper(
         IAsyncEnumerable<T> source
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
      }

      public IAsyncEnumerator<T> GetAsyncEnumerator() => this._source.GetAsyncEnumerator();

      public IAsyncConcurrentEnumeratorSource<T> GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments )
      {
         var max = arguments.MaximumConcurrentTasks;
         var enumerator = this._source.GetAsyncEnumerator();
         return max <= 0 ?
            (IAsyncConcurrentEnumeratorSource<T>) new AsyncConcurrentEnumeratorSourceForAsyncLoop<T>( this._source ) :
            new AsyncConcurrentEnumeratorSourceForSyncLoop<T>( this.GetAsyncEnumeratorsLimited( enumerator, max ), enumerator.DisposeAsync );
      }

      //private IAsyncEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsUnlimited(
      //   IAsyncEnumerator<T> enumerator
      //   )
      //{
      //   var asyncLock = new AsyncLock();
      //   var state = STATE_INITIAL;
      //   return AsyncEnumerationFactory.CreateSequentialEnumerable( () => AsyncEnumerationFactory.CreateSequentialStartInfo(
      //      async () =>
      //      {
      //         T item = default;

      //         if ( state != WAIT_RETURNED_FALSE )
      //         {
      //            using ( await asyncLock.LockAsync() )
      //            {
      //               if ( state != WAIT_RETURNED_FALSE )
      //               {
      //                  // Use enumerator within mutex to guarantee sequential access
      //                  Boolean success;
      //                  do
      //                  {
      //                     if ( state == GET_RETURNED_TRUE )
      //                     {
      //                        item = enumerator.TryGetNext( out success );
      //                        if ( !success )
      //                        {
      //                           Interlocked.Exchange( ref state, STATE_INITIAL );
      //                        }
      //                     }
      //                     else
      //                     {
      //                        success = await enumerator.WaitForNextAsync();
      //                        Interlocked.Exchange( ref state, success ? WAIT_RETURNED_TRUE : WAIT_RETURNED_FALSE );
      //                        if ( success )
      //                        {
      //                           item = enumerator.TryGetNext( out success );
      //                           if ( success )
      //                           {
      //                              Interlocked.Exchange( ref state, GET_RETURNED_TRUE );
      //                           }
      //                        }
      //                     }
      //                  } while ( state == STATE_INITIAL || state == WAIT_RETURNED_TRUE );

      //               }
      //            }
      //         }

      //         return (state != WAIT_RETURNED_FALSE, new LINQ.SingletonEnumerator<T>( item ));
      //      },
      //      default ) );
      //}

      private IEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsLimited(
         IAsyncEnumerator<T> enumerator,
         Int32 max
         )
      {
         var sema = new SemaphoreSlim( max, max );
         var asyncLock = max == 1 ? null : new AsyncLock();
         var state = STATE_INITIAL;

         async Task<T> GetNextWithinMutex()
         {
            Boolean success;
            T item;
            do
            {
               if ( state == GET_RETURNED_TRUE )
               {
                  item = enumerator.TryGetNext( out success );
                  if ( !success )
                  {
                     Interlocked.Exchange( ref state, STATE_INITIAL );
                  }
               }
               else
               {
                  success = await enumerator.WaitForNextAsync();
                  Interlocked.Exchange( ref state, success ? WAIT_RETURNED_TRUE : WAIT_RETURNED_FALSE );
                  if ( success )
                  {
                     item = enumerator.TryGetNext( out success );
                     if ( success )
                     {
                        Interlocked.Exchange( ref state, GET_RETURNED_TRUE );
                     }
                  }
                  else
                  {
                     item = default;
                  }
               }
            } while ( state == STATE_INITIAL || state == WAIT_RETURNED_TRUE );

            return item;
         }

         return Enumerable.Repeat(
            AsyncEnumerationFactory.CreateSequentialEnumerator(
               async () =>
               {
                  await sema.WaitAsync();
                  try
                  {
                     T item = default;

                     if ( state != WAIT_RETURNED_FALSE )
                     {
                        if ( max == 1 )
                        {
                           // Semaphore already acts as a mutex
                           item = await GetNextWithinMutex();
                        }
                        else
                        {
                           using ( await asyncLock.LockAsync() )
                           {
                              if ( state != WAIT_RETURNED_FALSE )
                              {
                                 // Use enumerator within mutex to guarantee sequential access
                                 item = await GetNextWithinMutex();
                              }
                           }
                        }
                     }

                     return (state != WAIT_RETURNED_FALSE, item);
                  }
                  finally
                  {
                     sema.Release();
                  }
               },
               default
               ),
            max
            );
      }
   }
}

public static partial class E_UtilPack
{
   /// <summary>
   /// This method wraps this <see cref="IAsyncEnumerable{T}"/> into a <see cref="IAsyncConcurrentEnumerable{T}"/> (if it is not already <see cref="IAsyncConcurrentEnumerable{T}"/>).
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>A <see cref="IAsyncConcurrentEnumerable{T}"/> which will use this <see cref="IAsyncEnumerable{T}"/> as its underlying source.</returns>
   /// <remarks>
   /// The returned <see cref="IAsyncConcurrentEnumerable{T}"/> will still call this <see cref="IAsyncEnumerable{T}"/> in sequential fashion.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   public static IAsyncConcurrentEnumerable<T> AsConcurrentEnumerable<T>( this IAsyncEnumerable<T> enumerable )
   {
      return enumerable is IAsyncConcurrentEnumerable<T> existing ?
         existing :
         new AsyncConcurrentEnumerableWrapper<T>( ArgumentValidator.ValidateNotNullReference( enumerable ) );
   }

   const Int32 MOVE_NEXT_ENDED = 0;
   const Int32 MOVE_NEXT_SUCCESS = 1;

   /// <summary>
   /// Enumerates this <see cref="IAsyncConcurrentEnumerable{T}"/> concurrently as specified by given <see cref="ConcurrentEnumerationArguments"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncConcurrentEnumerable{T}"/>.</param>
   /// <param name="action">The synchronous callback to run for each encountered item. This callback may be invoked concurrently.</param>
   /// <param name="arguments">The <see cref="ConcurrentEnumerationArguments"/> controlling and limiting concurrency.</param>
   /// <returns>A task which completes when this <see cref="IAsyncConcurrentEnumerable{T}"/> is fully enumerated.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncConcurrentEnumerable{T}"/> is <c>null</c>.</exception>
   public static ValueTask<Int64> EnumerateConcurrentlyAsync<T>( this IAsyncConcurrentEnumerable<T> enumerable, Action<T> action, ConcurrentEnumerationArguments arguments = default )
      => enumerable.PerformConcurrentEnumeration(
         action,
         ( enumerator, actionParam ) => enumerator.EnumerateSequentiallyAsync( actionParam ),
         ( actionParam, item ) => { actionParam( item ); return null; },
         arguments );

   /// <summary>
   /// Enumerates this <see cref="IAsyncConcurrentEnumerable{T}"/> concurrently as specified by given <see cref="ConcurrentEnumerationArguments"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncConcurrentEnumerable{T}"/>.</param>
   /// <param name="asyncAction">The asynchronous callback to run for each encountered item. This callback may be invoked concurrently.</param>
   /// <param name="arguments">The <see cref="ConcurrentEnumerationArguments"/> controlling and limiting concurrency.</param>
   /// <returns>A task which completes when this <see cref="IAsyncConcurrentEnumerable{T}"/> is fully enumerated and all tasks started by <paramref name="asyncAction"/> are completed.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncConcurrentEnumerable{T}"/> is <c>null</c>.</exception>
   public static ValueTask<Int64> EnumerateConcurrentlyAsync<T>( this IAsyncConcurrentEnumerable<T> enumerable, Func<T, Task> asyncAction, ConcurrentEnumerationArguments arguments = default )
      => enumerable.PerformConcurrentEnumeration(
         asyncAction,
         ( enumerator, actionParam ) => enumerator.EnumerateSequentiallyAsync( actionParam ),
         ( actionParam, item ) => actionParam( item ),
         arguments
         );

   private static ValueTask<Int64> PerformConcurrentEnumeration<T, TArg>(
      this IAsyncConcurrentEnumerable<T> enumerable,
      TArg arg,
      Func<IAsyncEnumerator<T>, TArg, ValueTask<Int64>> sequentialCall,
      Func<TArg, T, Task> startAndForgetCall,
      ConcurrentEnumerationArguments arguments
      )

   {
      var source = enumerable.GetConcurrentEnumeratorSource( arguments ) ?? throw new InvalidOperationException( $"The returned {nameof( IAsyncConcurrentEnumeratorSource<T> )} was null." );
      var enumerators = source.GetAsyncEnumeratorsEnumerable();
      return enumerators == null ?
         ( source.GetWrappedSynchronousSource() ?? throw new InvalidOperationException( $"The returned {nameof( IAsyncEnumerable<IAsyncEnumerator<T>> ) } was null." ) ).GetAsyncEnumerator().PerformConcurrentEnumeration_AsyncLoop( arg, startAndForgetCall ) :
         source.PerformConcurrentEnumeration_SyncLoop( enumerators, arg, sequentialCall );
   }

   private static ValueTask<Int64> PerformConcurrentEnumeration_SyncLoop<T, TArg>(
      this IAsyncConcurrentEnumeratorSource<T> source,
      IEnumerable<IAsyncEnumerator<T>> enumerators,
      TArg arg,
      Func<IAsyncEnumerator<T>, TArg, ValueTask<Int64>> sequentialCall
      )
   {
      TaskCompletionSource<Int64> src = null;
      TConcurrentExceptionList exceptions = null;
      var totalItemsEncountered = 0L;
      var tasksStarted = 0L;
      var moveNext = MOVE_NEXT_SUCCESS;
      //var synchronousGracefulEnd = false;
      try
      {
         foreach ( var asyncEnumerator in source.GetAsyncEnumeratorsEnumerable() )
         {
            var task = sequentialCall( asyncEnumerator, arg );
            if ( task.IsCompleted )
            {
               Interlocked.Add( ref totalItemsEncountered, task.Result );
            }
            else
            {
               CreateCompletionSourceIfNeeded( ref src, ref tasksStarted );
               task.AsTask().ContinueWith( completedTask =>
               {
                  ConcurrentTaskContinuation( source, completedTask, completedTask, src, null, ref exceptions, ref moveNext, ref tasksStarted, ref totalItemsEncountered );
               }, TaskContinuationOptions.ExecuteSynchronously );
            }
         }
         Interlocked.Exchange( ref moveNext, MOVE_NEXT_ENDED );
         //synchronousGracefulEnd = true;
      }
      finally
      {
         if ( src == null )
         {
            // No asynchrony at all
            var endTask = source.DisposeAsync(); //.CallEnumerationEndedWithinFinally( synchronousGracefulEnd );
            if ( !endTask.IsCompleted )
            {
               // Enumeration ending is asynchronous
               src = new TaskCompletionSource<Int64>();
               endTask.ContinueWith( t =>
               {
                  PerformTaskCompletion( t, src, exceptions, Interlocked.Read( ref totalItemsEncountered ) );
               } );

            }
         }
      }
      ValueTask<Int64> retVal;
      if ( src == null )
      {
         // Completed fully synchronously.
         retVal = new ValueTask<Int64>( totalItemsEncountered );
      }
      else if ( src.Task.IsCompleted )
      {
         // There was some asynchrony, but the synchronous loop was slower
         retVal = new ValueTask<Int64>( src.Task.Result );
      }
      else
      {
         // Completed asynchronously
         if ( Interlocked.Read( ref tasksStarted ) == 0 )
         {
            // This can happen when "moveNextSuccess" is set to MOVE_NEXT_ENDED right after last task completion reads it
            ConcurrentTaskContinuation_LastTask( source, null, src, ref exceptions, ref totalItemsEncountered );
         }
         retVal = new ValueTask<Int64>( src.Task );
      }

      return retVal;
   }

   private static ValueTask<Int64> PerformConcurrentEnumeration_AsyncLoop<T, TArg>(
      this IAsyncEnumerator<T> enumerator,
      TArg arg,
      Func<TArg, T, Task> startAndForgetCall
      )
   {
      ArgumentValidator.ValidateNotNull( nameof( enumerator ), enumerator );

      TaskCompletionSource<Int64> src = null;
      TConcurrentExceptionList exceptions = null;
      var itemsEncountered = 0L;
      var tasksStarted = 0L;
      var moveNext = MOVE_NEXT_SUCCESS; // In this version, the 'moveNext' variable is actually never set
      //var synchronousGracefulEnd = false;
      ValueTask<Int64>? enumerationTask = null;
      try
      {
         enumerationTask = enumerator.EnumerateSequentiallyAsync( item =>
         {
            Interlocked.Increment( ref itemsEncountered );
            CreateCompletionSourceIfNeeded( ref src, ref tasksStarted );
            Task.Factory.StartNew( state =>
            {
               var tuple = (ValueTuple<Func<TArg, T, Task>, TArg, T>) state;
               return tuple.Item1?.Invoke( tuple.Item2, tuple.Item3 );
            }, (startAndForgetCall, arg, item) ).ContinueWith( completedTask =>
            {
               var startedTask = completedTask.Status == TaskStatus.RanToCompletion ? completedTask.Result : null;
               if ( startedTask != null )
               {
                  startedTask.ContinueWith( actualCompletedTask => ConcurrentTaskContinuation( enumerator, actualCompletedTask, null, src, null, ref exceptions, ref moveNext, ref tasksStarted, ref itemsEncountered ) );
               }
               else
               {
                  ConcurrentTaskContinuation( enumerator, completedTask, null, src, null, ref exceptions, ref moveNext, ref tasksStarted, ref itemsEncountered );
               }
            }, TaskContinuationOptions.ExecuteSynchronously );

         }, true ); // Don't call dispose on enumerator, as we will call it within ConcurrentTaskContinuation after the last task has completed.
      }
      finally
      {
         if ( !enumerationTask.HasValue )
         {
            // Synchronous exception -> fire-and-forget
            enumerator.DisposeAsync();
         }
         else if ( enumerationTask.Value.IsCompleted && src == null )
         {
            // We have completed synchronously without an exception, and we don't have any tasks running, since 'src' is null
            var endTask = enumerator.DisposeAsync();
            if ( !endTask.IsCompleted )
            {
               // Enumeration ending is asynchronous
               src = new TaskCompletionSource<Int64>();
               endTask.ContinueWith( t =>
               {
                  PerformTaskCompletion( t, src, exceptions, Interlocked.Read( ref itemsEncountered ) );
               } );
            }
         }
      }

      // At this point, enumerationTask *will* have value, as the only way it wouldn't would be to have an exception to occur synchronously,
      ValueTask<Int64> retVal;
      if ( enumerationTask.Value.IsCompleted && src == null )
      {
         // Completed fully synchronously.
         retVal = new ValueTask<Int64>( itemsEncountered );
      }
      // This is not possible in async loop version, since ConcurrentTaskContinuation will never end up setting task completed since it moveNext is set to MOVE_NEXT_ENDED only below
      //else if ( src != null && src.Task.IsCompleted )
      //{
      //   // There was some asynchrony, but the synchronous code was slower
      //   retVal = new ValueTask<Int64>( src.Task.Result );
      //}
      else
      {
         void AfterAsyncLoopEnded( Task t )
         {
            // Signal that async loop is done.
            Interlocked.Exchange( ref moveNext, MOVE_NEXT_ENDED );
            if ( Interlocked.Read( ref tasksStarted ) == 0 )
            {
               // This can happen when "moveNextSuccess" is set to MOVE_NEXT_ENDED right after last task completion reads it
               ConcurrentTaskContinuation_LastTask( enumerator, t, src, ref exceptions, ref itemsEncountered );
            }
         }

         if ( enumerationTask.Value.IsCompleted )
         {
            // The src is not null here, so we have started some tasks.
            AfterAsyncLoopEnded( null );
         }
         else
         {
            // enumerationTask is not completed, and src may be null or non-null, we must create it if it is null
            if ( src == null )
            {
               var dummy = 0L;
               // Create completion source if needed - but we are not starting any tasks.
               CreateCompletionSourceIfNeeded( ref src, ref dummy );
            }

            enumerationTask.Value.AsTask().ContinueWith( AfterAsyncLoopEnded );
         }

         retVal = new ValueTask<Int64>( src.Task );
      }

      return retVal;
   }

   private static void CreateCompletionSourceIfNeeded<T>( ref TaskCompletionSource<T> source, ref Int64 tasksStarted )
   {
      if ( source == null )
      {
         Interlocked.CompareExchange( ref source, new TaskCompletionSource<T>(), null );
      }
      Interlocked.Increment( ref tasksStarted );
   }

   private static void ConcurrentTaskContinuation(
      IAsyncDisposable enumerator,
      Task task,
      Task<Int64> itemModifyingTask,
      TaskCompletionSource<Int64> src,
      Exception catched,
      ref TConcurrentExceptionList exceptions,
      ref Int32 moveNextSuccess,
      ref Int64 tasksStarted,
      ref Int64 itemsEncountered // This must be 'ref' because of concurrency
      )
   {

      AddToExceptionList( ref exceptions, catched );
      AddToExceptionList( ref exceptions, task.Exception );

      if ( itemModifyingTask != null && itemModifyingTask.Status == TaskStatus.RanToCompletion )
      {
         Interlocked.Add( ref itemsEncountered, itemModifyingTask.Result );
      }

      if (
         Interlocked.Decrement( ref tasksStarted ) == 0 // This is the last async invocation
         && moveNextSuccess == MOVE_NEXT_ENDED // loop as ended
         )
      {
         ConcurrentTaskContinuation_LastTask( enumerator, task, src, ref exceptions, ref itemsEncountered );
         //PerformTaskCompletion( task, src, exceptions, itemsEncountered );
      }


   }

   private static void AddToExceptionList( ref TConcurrentExceptionList allExceptions, Exception exception )
   {
      if ( exception != null )
      {
         if ( allExceptions == null )
         {
            Interlocked.CompareExchange( ref allExceptions, new TConcurrentExceptionList(), null );
         }
#if NETSTANDARD1_0
         lock ( allExceptions )
         {
#endif
         allExceptions.Add( exception );
#if NETSTANDARD1_0
         }
#endif
      }
   }

   private static void ConcurrentTaskContinuation_LastTask(
      IAsyncDisposable enumerator,
      Task task,
      TaskCompletionSource<Int64> src,
      ref TConcurrentExceptionList exceptions,
      ref Int64 itemsEncountered // This must be 'ref' because of concurrency
      )
   {
      Exception endException = null;
      Task endTask = default;
      try
      {
         endTask = enumerator.DisposeAsync();
      }
      catch ( Exception exc )
      {
         endException = exc;
      }

      if ( endException != null )
      {
         AddToExceptionList( ref exceptions, endException );
         PerformTaskCompletion( task, src, exceptions, itemsEncountered );
      }
      else
      {
         var excTmp = exceptions;
         var itemsEncounteredTmp = itemsEncountered;
         endTask.ContinueWith( et => PerformTaskCompletion( et, src, excTmp, itemsEncounteredTmp ) );
      }
   }

   private static void PerformTaskCompletion(
      Task task,
      TaskCompletionSource<Int64> src,
      TConcurrentExceptionList allExceptions,
      Int64 itemsEncountered
      )
   {
      if ( task?.IsCanceled ?? false )
      {
         src.TrySetCanceled();
      }
      else if ( ( allExceptions?.Count ?? 0 ) > 0 || ( task?.IsFaulted ?? false ) )
      {
         AddToExceptionList( ref allExceptions, task.Exception );
         src.TrySetException( allExceptions.Count == 1 ?
            allExceptions
#if NETSTANDARD1_0
                  [0]
#else
                  .First()
#endif
                  :
            new AggregateException( allExceptions.ToArray() )
            );
      }
      else if ( ( task?.IsCompleted ?? true ) )
      {
         src.TrySetResult( Interlocked.Read( ref itemsEncountered ) );
      }
      else
      {
         System.Diagnostics.Debug.Assert( false, "When does this happen? Task continued with, but not completed." );
      }
   }

   /// <summary>
   /// This method will call the <see cref="EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Action{T}, ConcurrentEnumerationArguments)"/> method if this <see cref="IAsyncEnumerable{T}"/> is <see cref="IAsyncConcurrentEnumerable{T}"/>.
   /// Otherwise it will call the <see cref="EnumerateSequentiallyAsync{T}(IAsyncEnumerable{T}, Action{T})"/> method.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/> which may be <see cref="IAsyncConcurrentEnumerable{T}"/>.</param>
   /// <param name="action">The callback to call for each encountered item.</param>
   /// <returns>A task which asynchronously returns amount of items encountered.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   public static ValueTask<Int64> EnumerateConcurrentlyIfPossible<T>( this IAsyncEnumerable<T> enumerable, Action<T> action )
   {
      var concurrent = ArgumentValidator.ValidateNotNullReference( enumerable ) as IAsyncConcurrentEnumerable<T>;
      return concurrent == null ? enumerable.EnumerateSequentiallyAsync( action ) : concurrent.EnumerateConcurrentlyAsync( action );
   }

   /// <summary>
   /// This method will call the <see cref="EnumerateConcurrentlyAsync{T}(IAsyncConcurrentEnumerable{T}, Func{T, Task}, ConcurrentEnumerationArguments)"/> method if this <see cref="IAsyncEnumerable{T}"/> is <see cref="IAsyncConcurrentEnumerable{T}"/>.
   /// Otherwise it will call the <see cref="EnumerateSequentiallyAsync{T}(IAsyncEnumerable{T}, Func{T, Task})"/> method.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/> which may be <see cref="IAsyncConcurrentEnumerable{T}"/>.</param>
   /// <param name="asyncAction">The asynchronous callback to call for each encountered item.</param>
   /// <returns>A task which asynchronously returns amount of items encountered.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   public static ValueTask<Int64> EnumerateConcurrentlyIfPossible<T>( this IAsyncEnumerable<T> enumerable, Func<T, Task> asyncAction )
   {
      var concurrent = ArgumentValidator.ValidateNotNullReference( enumerable ) as IAsyncConcurrentEnumerable<T>;
      return concurrent == null ? enumerable.EnumerateSequentiallyAsync( asyncAction ) : concurrent.EnumerateConcurrentlyAsync( asyncAction );
   }

}
