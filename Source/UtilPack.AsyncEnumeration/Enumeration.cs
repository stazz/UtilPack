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

using TAsyncPotentialToken = System.Nullable<System.Int64>;
using TAsyncToken = System.Int64;
using TConcurrentExceptionList = System.Collections.
#if NETSTANDARD1_0
   Generic.List
#else
   Concurrent.ConcurrentBag
#endif
   <System.Exception>;

/// <summary>
/// This class contains extension methods for UtilPack types.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// This is helper method to sequentially enumerate a <see cref="AsyncEnumerator{T}"/> and properly dispose it in case of exception.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <param name="enumerator">This <see cref="AsyncEnumerator{T}"/>.</param>
   /// <param name="action">The callback to invoke for each item. May be <c>null</c>.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use when enumerating.</param>
   /// <returns>A task which will have enumerated the <see cref="AsyncEnumerator{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   /// <remarks>
   /// Sequential enumeration means that the next invocation of <see cref="AsyncEnumerator{T}.MoveNextAsync"/> will not start until the given callback <paramref name="action"/> is completed.
   /// </remarks>
   public static async ValueTask<Int64> EnumerateSequentiallyAsync<T>( this AsyncEnumerator<T> enumerator, Action<T> action, CancellationToken token = default )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      try
      {
         var retVal = 0L;
         TAsyncPotentialToken retrievalToken;
         while ( ( retrievalToken = await enumerator.MoveNextAsync() ).HasValue )
         {
            ++retVal;
            action?.Invoke( enumerator.OneTimeRetrieve( retrievalToken.Value ) );
         }

         return retVal;
      }
      catch
      {
         try
         {
            await enumerator.TryResetAsync( token );
         }
         catch
         {
            // Ignore
         }

         throw;
      }
   }

   /// <summary>
   /// This is helper method to sequentially enumerate a <see cref="AsyncEnumerator{T}"/> and properly dispose it in case of an exception.
   /// For each item, a task from given callback is awaited for, if it is not <c>null</c>.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <param name="enumerator">This <see cref="AsyncEnumerator{T}"/>.</param>
   /// <param name="asyncAction">The callback to invoke for each item. May be <c>null</c>, and may also return <c>null</c>.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use when enumerating.</param>
   /// <returns>A task which will have enumerated the <see cref="AsyncEnumerator{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   /// <remarks>
   ///  Sequential enumeration means that the next invocation of <see cref="AsyncEnumerator{T}.MoveNextAsync"/> will not start until the given callback <paramref name="asyncAction"/> is completed, synchronously or asynchronously.
   /// </remarks>
   public static async ValueTask<Int64> EnumerateSequentiallyAsync<T>( this AsyncEnumerator<T> enumerator, Func<T, Task> asyncAction, CancellationToken token = default )
   {
      try
      {
         var retVal = 0L;
         TAsyncPotentialToken retrievalToken;
         while ( ( retrievalToken = await enumerator.MoveNextAsync( token ) ).HasValue )
         {
            ++retVal;
            Task task;
            if ( asyncAction != null && ( task = asyncAction( enumerator.OneTimeRetrieve( retrievalToken.Value ) ) ) != null )
            {
               await task;
            }
         }

         return retVal;
      }
      catch
      {
         try
         {
            await enumerator.TryResetAsync( token );
         }
         catch
         {
            // Ignore
         }

         throw;
      }
   }

   const Int32 MOVE_NEXT_ENDED = 0;
   const Int32 MOVE_NEXT_SUCCESS = 1;

   /// <summary>
   /// This is helper method to enumerate this <see cref="AsyncEnumerator{T}"/> in parallel, and properly handle all the tasks started by parallel enumeration.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="enumerator">This <see cref="AsyncEnumerator{T}"/>.</param>
   /// <param name="action">The callback to invoke for each item. May be <c>null</c>.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use when enumerating.</param>
   /// <returns>A task which will have enumerated the <see cref="AsyncEnumerator{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   /// <exception cref="InvalidOperationException">If this <see cref="AsyncEnumerator{T}"/> does not support parallel enumeration.</exception>
   /// <remarks>
   /// Enumeration in parallel will not wait for <see cref="AsyncEnumerator{T}.MoveNextAsync"/> to complete before calling it again.
   /// This implies that <paramref name="action"/> may be run concurrently.
   /// This can cause potential overhead, if this <see cref="AsyncEnumerator{T}"/> will find out about the end of the enumeration only asynchronously, and not synchronously.
   /// </remarks>
   public static ValueTask<Int64> EnumerateInParallelAsync<T>( this AsyncEnumerator<T> enumerator, Action<T> action, CancellationToken token = default )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      TaskCompletionSource<Int64> src = null;
      TConcurrentExceptionList exceptions = null;
      var itemsEncountered = 0L;
      try
      {
         var moveNextSuccess = MOVE_NEXT_SUCCESS;
         var tasksStarted = 0L;

         void InvokeAction( TAsyncPotentialToken retrievalToken )
         {
            if ( retrievalToken.HasValue )
            {
               Interlocked.Increment( ref itemsEncountered );
               action?.Invoke( enumerator.OneTimeRetrieve( retrievalToken.Value ) );
            }
            else
            {
               Interlocked.CompareExchange( ref moveNextSuccess, MOVE_NEXT_ENDED, MOVE_NEXT_SUCCESS );
            }

         }

         do
         {
            var task = enumerator.MoveNextAsync();
            if ( task.IsCompleted )
            {
               // We completed synchronously
               InvokeAction( task.Result );
            }
            else
            {
               // Truly asynchronous call -> need to use some manual plumbing code
               if ( src == null )
               {
                  src = new TaskCompletionSource<Int64>();
               }

               Interlocked.Increment( ref tasksStarted );

               task.AsTask().ContinueWith( t =>
               {
                  Exception catched = null;
                  try
                  {
                     if ( t.Status == TaskStatus.RanToCompletion )
                     {
                        // We have completed successfully
                        InvokeAction( t.Result );
                     }
                  }
                  catch ( Exception exc )
                  {
                     catched = exc;
                  }
                  finally
                  {
                     ParallelTaskContinuation( t, src, catched, ref exceptions, ref moveNextSuccess, ref tasksStarted, ref itemsEncountered );
                  }
               }, TaskContinuationOptions.ExecuteSynchronously );
            }
         } while ( moveNextSuccess == MOVE_NEXT_SUCCESS );
      }
      catch ( Exception exc )
      {
         var rethrow = true;
         try
         {
            var resetTask = enumerator.TryResetAsync( token );
            rethrow = src == null || resetTask.IsCompleted;
            if ( !rethrow )
            {
               if ( src == null )
               {
                  src = new TaskCompletionSource<Int64>();
               }
               src.SetException( exc );
            }
         }
         catch
         {
            // Ignore
         }

         if ( rethrow )
         {
            throw;
         }
      }

      ValueTask<Int64> retVal;
      if ( src == null )
      {
         // Completed fully synchronously.
         retVal = new ValueTask<Int64>( itemsEncountered );
      }
      else if ( src.Task.IsCompleted )
      {
         // There was some asynchrony, but the synchronous loop was slower
         retVal = new ValueTask<Int64>( src.Task.Result );
      }
      else
      {
         // Completed asynchronously
         retVal = new ValueTask<Int64>( src.Task );
      }

      return retVal;
   }

   /// <summary>
   /// This is helper method to enumerate this <see cref="AsyncEnumerator{T}"/> in parallel, and properly handle all the tasks started by parallel enumeration.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="enumerator">This <see cref="AsyncEnumerator{T}"/>.</param>
   /// <param name="asyncAction">The asynchronous callback to invoke for each item. May be <c>null</c>.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use when enumerating.</param>
   /// <returns>A task which will have enumerated the <see cref="AsyncEnumerator{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   /// <exception cref="InvalidOperationException">If this <see cref="AsyncEnumerator{T}"/> does not support parallel enumeration.</exception>
   /// <remarks>
   /// Enumeration in parallel will not wait for <see cref="AsyncEnumerator{T}.MoveNextAsync"/> nor <paramref name="asyncAction"/> to complete before calling <see cref="AsyncEnumerator{T}.MoveNextAsync"/> again.
   /// This implies that <paramref name="asyncAction"/> may be run concurrently.
   /// This can cause potential overhead, if this <see cref="AsyncEnumerator{T}"/> will find out about the end of the enumeration only asynchronously, and not synchronously.
   /// </remarks>
   public static ValueTask<Int64> EnumerateInParallelAsync<T>( this AsyncEnumerator<T> enumerator, Func<T, Task> asyncAction, CancellationToken token = default )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      TaskCompletionSource<Int64> src = null;
      TConcurrentExceptionList exceptions = null;
      var itemsEncountered = 0L;
      try
      {
         var moveNextSuccess = MOVE_NEXT_SUCCESS;
         var tasksStarted = 0L;

         void InvokeAction( TAsyncPotentialToken retrievalToken )
         {
            if ( retrievalToken.HasValue )
            {
               Interlocked.Increment( ref itemsEncountered );
               var actionTask = asyncAction?.Invoke( enumerator.OneTimeRetrieve( retrievalToken.Value ) );
               if ( actionTask != null && !actionTask.IsCompleted )
               {
                  Interlocked.Increment( ref tasksStarted );
                  actionTask.ContinueWith( t =>
                  {
                     ParallelTaskContinuation( t, src, t.Exception, ref exceptions, ref moveNextSuccess, ref tasksStarted, ref itemsEncountered );
                  } );
               }
            }
            else
            {
               Interlocked.Exchange( ref moveNextSuccess, MOVE_NEXT_ENDED );
            }

         }

         do
         {
            var task = enumerator.MoveNextAsync();

            if ( task.IsCompleted )
            {
               // We completed synchronously
               InvokeAction( task.Result );
            }
            else
            {
               // Truly asynchronous call -> need to use some manual plumbing code
               if ( src == null )
               {
                  src = new TaskCompletionSource<Int64>();
               }

               Interlocked.Increment( ref tasksStarted );

               task.AsTask().ContinueWith( t =>
               {
                  Exception catched = null;
                  try
                  {
                     if ( t.Status == TaskStatus.RanToCompletion )
                     {
                        // We have completed successfully
                        InvokeAction( t.Result );
                     }
                  }
                  catch ( Exception exc )
                  {
                     catched = exc;
                  }
                  finally
                  {
                     ParallelTaskContinuation( t, src, catched, ref exceptions, ref moveNextSuccess, ref tasksStarted, ref itemsEncountered );
                  }

               }, TaskContinuationOptions.ExecuteSynchronously );
            }
         } while ( moveNextSuccess == MOVE_NEXT_SUCCESS );
      }
      catch ( Exception exc )
      {
         var rethrow = true;
         try
         {
            var resetTask = enumerator.TryResetAsync( token );
            rethrow = src == null || resetTask.IsCompleted;
            if ( !rethrow )
            {
               if ( src == null )
               {
                  src = new TaskCompletionSource<Int64>();
               }
               src.SetException( exc );
            }
         }
         catch
         {
            // Ignore
         }

         if ( rethrow )
         {
            throw;
         }
      }

      ValueTask<Int64> retVal;
      if ( src == null )
      {
         // Completed fully synchronously.
         retVal = new ValueTask<Int64>( itemsEncountered );
      }
      else if ( src.Task.IsCompleted )
      {
         // There was some asynchrony, but the synchronous loop was slower
         retVal = new ValueTask<Int64>( src.Task.Result );
      }
      else
      {
         // Completed asynchronously
         retVal = new ValueTask<Int64>( src.Task );
      }

      return retVal;
   }

   private static void ParallelTaskContinuation(
      Task task,
      TaskCompletionSource<Int64> src,
      Exception catched,
      ref TConcurrentExceptionList exceptions,
      ref Int32 moveNextSuccess,
      ref Int64 tasksStarted,
      ref Int64 itemsEncountered // This must be 'ref' because of concurrency
      )
   {
      void AddToExceptionList( ref TConcurrentExceptionList allExceptions, Exception exception )
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

      AddToExceptionList( ref exceptions, catched );

      if (
         moveNextSuccess == MOVE_NEXT_ENDED // The synchronous loop ended before we arrived here
         && Interlocked.Decrement( ref tasksStarted ) == 0 // This is the last async invocation
         )
      {

         // We need to set task completion source
         if ( task.IsCanceled )
         {
            src.SetCanceled();
         }
         else if ( ( exceptions?.Count ?? 0 ) > 0 || task.IsFaulted )
         {
            AddToExceptionList( ref exceptions, task.Exception );
            src.SetException( exceptions.Count == 1 ?
               exceptions
#if NETSTANDARD1_0
               [0]
#else
               .First()
#endif
               :
               new AggregateException( exceptions.ToArray() )
               );
         }
         else if ( task.IsCompleted )
         {
            src.SetResult( Interlocked.Read( ref itemsEncountered ) );
         }
         else
         {
            System.Diagnostics.Debug.Assert( false, "When does this happen? Task continued with, but not completed." );
         }
      }
   }

   /// <summary>
   /// This is helper method to enumerate this <see cref="AsyncEnumerator{T}"/> in parallel, if it is supported, or sequentially, if parallel is not supported.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerator">This <see cref="AsyncEnumerator{T}"/>.</param>
   /// <param name="action">The callback to invoke for each item. May be <c>null</c>.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use when enumerating.</param>
   /// <returns>A task which will have enumerated the <see cref="AsyncEnumerator{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   /// <exception cref="InvalidOperationException">If this <see cref="AsyncEnumerator{T}"/> does not support parallel enumeration.</exception>
   /// <seealso cref="AsyncEnumerator{T}.IsParallelEnumerationSupported"/>
   public static ValueTask<Int64> EnumeratePreferParallel<T>( this AsyncEnumerator<T> enumerator, Action<T> action, CancellationToken token = default )
   {
      return enumerator.IsParallelEnumerationSupported ? enumerator.EnumerateInParallelAsync( action, token ) : enumerator.EnumerateSequentiallyAsync( action, token );
   }

   /// <summary>
   /// This is helper method to enumerate this <see cref="AsyncEnumerator{T}"/> in parallel, if it is supported, or sequentially, if parallel is not supported.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerator">This <see cref="AsyncEnumerator{T}"/>.</param>
   /// <param name="asyncAction">The asynchronous callback to invoke for each item. May be <c>null</c>.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use when enumerating.</param>
   /// <returns>A task which will have enumerated the <see cref="AsyncEnumerator{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   /// <exception cref="InvalidOperationException">If this <see cref="AsyncEnumerator{T}"/> does not support parallel enumeration.</exception>
   /// <seealso cref="AsyncEnumerator{T}.IsParallelEnumerationSupported"/>
   public static ValueTask<Int64> EnumeratePreferParallel<T>( this AsyncEnumerator<T> enumerator, Func<T, Task> asyncAction, CancellationToken token = default )
   {
      return enumerator.IsParallelEnumerationSupported ? enumerator.EnumerateInParallelAsync( asyncAction, token ) : enumerator.EnumerateSequentiallyAsync( asyncAction, token );
   }


}