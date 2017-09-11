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

using TAsyncPotentialToken = System.Nullable<System.Int64>;
using TAsyncToken = System.Int64;
using TSequentialCurrentInfoFactory = System.Func<System.Object, UtilPack.AsyncEnumeration.ResetAsyncDelegate, System.Object, System.Object>;

namespace UtilPack.AsyncEnumeration
{
   /// <summary>
   /// This class provides static factory methods to create objects of type <see cref="AsyncEnumerator{T}"/>, <see cref="AsyncEnumerator{T, TMetadata}"/>, <see cref="AsyncEnumeratorObservable{T}"/>, and <see cref="AsyncSequentialOnlyEnumeratorObservableImpl{T, TMetadata}"/>.
   /// </summary>
   public static class AsyncEnumeratorFactory
   {
      /// <summary>
      /// Creates a new instance of <see cref="AsyncEnumerator{T}"/> which will behave as specified by given <see cref="InitialMoveNextAsyncDelegate{T}"/> delegate.
      /// </summary>
      /// <typeparam name="T">The type of the items being enumerated.</typeparam>
      /// <param name="initialMoveNext">The <see cref="InitialMoveNextAsyncDelegate{T}"/> callback which will control how resulting <see cref="AsyncEnumerator{T}"/> will behave.</param>
      /// <returns>A new <see cref="AsyncEnumerator{T}"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="initialMoveNext"/> is <c>null</c>.</exception>
      /// <remarks>
      /// The parallel enumeration by <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/> and <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/> will not be supported on returned <see cref="AsyncEnumerator{T}"/>.
      /// </remarks>
      public static AsyncEnumerator<T> CreateSequentialEnumerator<T>(
         InitialMoveNextAsyncDelegate<T> initialMoveNext
         )
      {
         return new AsyncSequentialOnlyEnumeratorImplNonObservable<T>( initialMoveNext, SequentialCurrentInfoFactory.Get( typeof( T ) ) );
      }

      /// <summary>
      /// Creates a new intance of <see cref="AsyncEnumerator{T, TMetadata}"/> which will behave as specified by given <see cref="InitialMoveNextAsyncDelegate{T}"/> delegate, and have given metadata.
      /// </summary>
      /// <typeparam name="T">The type of the items being enumerated.</typeparam>
      /// <typeparam name="TMetadata">The type of metadata.</typeparam>
      /// <param name="initialMoveNext">The <see cref="InitialMoveNextAsyncDelegate{T}"/> callback which will control how resulting <see cref="AsyncEnumerator{T, TMetadata}"/> will behave.</param>
      /// <param name="metadata">The metadata.</param>
      /// <returns>A new <see cref="AsyncEnumerator{T, TMetadata}"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="initialMoveNext"/> is <c>null</c>.</exception>
      /// <remarks>
      /// The parallel enumeration by <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/> and <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/> will not be supported on returned <see cref="AsyncEnumerator{T}"/>.
      /// </remarks>
      public static AsyncEnumerator<T, TMetadata> CreateSequentialEnumerator<T, TMetadata>(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         TMetadata metadata
         )
      {
         return new AsyncSequentialOnlyEnumeratorImplNonObservable<T, TMetadata>( initialMoveNext, SequentialCurrentInfoFactory.Get( typeof( T ) ), metadata );
      }

      /// <summary>
      /// Creates a new instance of <see cref="AsyncEnumeratorObservable{T}"/> which will behave as specified by given <see cref="InitialMoveNextAsyncDelegate{T}"/> delegate, and also can be observed using the events of <see cref="AsyncEnumerationObservation{T}"/>.
      /// </summary>
      /// <typeparam name="T">The type of the items being enumerated.</typeparam>
      /// <param name="initialMoveNext">The <see cref="InitialMoveNextAsyncDelegate{T}"/> callback which will control how resulting <see cref="AsyncEnumeratorObservable{T}"/> will behave.</param>
      /// <param name="getGlobalBeforeEnumerationExecutionStart">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T}.BeforeEnumerationStart"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <param name="getGlobalAfterEnumerationExecutionStart">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T}.AfterEnumerationStart"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <param name="getGlobalBeforeEnumerationExecutionEnd">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T}.BeforeEnumerationEnd"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <param name="getGlobalAfterEnumerationExecutionEnd">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T}.AfterEnumerationEnd"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <param name="getGlobalAfterEnumerationExecutionItemEncountered">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T}.AfterEnumerationItemEncountered"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <returns>A new <see cref="AsyncEnumeratorObservable{T}"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="initialMoveNext"/> is <c>null</c>.</exception>
      /// <remarks>
      /// The parallel enumeration by <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/> and <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/> will not be supported on returned <see cref="AsyncEnumerator{T}"/>.
      /// </remarks>
      public static AsyncEnumeratorObservable<T> CreateSequentialObservableEnumerator<T>(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         Func<GenericEventHandler<EnumerationStartedEventArgs>> getGlobalBeforeEnumerationExecutionStart = null,
         Func<GenericEventHandler<EnumerationStartedEventArgs>> getGlobalAfterEnumerationExecutionStart = null,
         Func<GenericEventHandler<EnumerationEndedEventArgs>> getGlobalBeforeEnumerationExecutionEnd = null,
         Func<GenericEventHandler<EnumerationEndedEventArgs>> getGlobalAfterEnumerationExecutionEnd = null,
         Func<GenericEventHandler<EnumerationItemEventArgs<T>>> getGlobalAfterEnumerationExecutionItemEncountered = null
         )
      {
         return new AsyncSequentialOnlyEnumeratorObservableImpl<T>(
            initialMoveNext,
            SequentialCurrentInfoFactory.Get( typeof( T ) ),
            getGlobalBeforeEnumerationExecutionStart,
            getGlobalAfterEnumerationExecutionStart,
            getGlobalBeforeEnumerationExecutionEnd,
            getGlobalAfterEnumerationExecutionEnd,
            getGlobalAfterEnumerationExecutionItemEncountered
            );
      }

      /// <summary>
      /// Creates a new instance of <see cref="AsyncEnumeratorObservable{T, TMetadata}"/> which will behave as specified by given <see cref="InitialMoveNextAsyncDelegate{T}"/> delegate, and also can be observed using the events of <see cref="AsyncEnumerationObservation{T}"/>.
      /// </summary>
      /// <typeparam name="T">The type of the items being enumerated.</typeparam>
      /// <typeparam name="TMetadata">The of the metadata.</typeparam>
      /// <param name="initialMoveNext">The <see cref="InitialMoveNextAsyncDelegate{T}"/> callback which will control how resulting <see cref="AsyncEnumerator{T, TMetadata}"/> will behave.</param>
      /// <param name="metadata">The metadata.</param>
      /// <param name="getGlobalBeforeEnumerationExecutionStart">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T, TMetadata}.BeforeEnumerationStart"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <param name="getGlobalAfterEnumerationExecutionStart">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T, TMetadata}.AfterEnumerationStart"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <param name="getGlobalBeforeEnumerationExecutionEnd">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T, TMetadata}.BeforeEnumerationEnd"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <param name="getGlobalAfterEnumerationExecutionEnd">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T, TMetadata}.AfterEnumerationEnd"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <param name="getGlobalAfterEnumerationExecutionItemEncountered">The optional callback to get global-scope <see cref="AsyncEnumerationObservation{T, TMetadata}.AfterEnumerationItemEncountered"/> event, which will be invoked after event of the returned enumerator.</param>
      /// <returns>A new <see cref="AsyncEnumeratorObservable{T, TMetadata}"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="initialMoveNext"/> is <c>null</c>.</exception>
      /// <remarks>
      /// The parallel enumeration by <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/> and <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/> will not be supported on returned <see cref="AsyncEnumerator{T}"/>.
      /// </remarks>
      public static AsyncEnumeratorObservable<T, TMetadata> CreateSequentialObservableEnumerator<T, TMetadata>(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         TMetadata metadata,
         Func<GenericEventHandler<EnumerationStartedEventArgs<TMetadata>>> getGlobalBeforeEnumerationExecutionStart = null,
         Func<GenericEventHandler<EnumerationStartedEventArgs<TMetadata>>> getGlobalAfterEnumerationExecutionStart = null,
         Func<GenericEventHandler<EnumerationEndedEventArgs<TMetadata>>> getGlobalBeforeEnumerationExecutionEnd = null,
         Func<GenericEventHandler<EnumerationEndedEventArgs<TMetadata>>> getGlobalAfterEnumerationExecutionEnd = null,
         Func<GenericEventHandler<EnumerationItemEventArgs<T, TMetadata>>> getGlobalAfterEnumerationExecutionItemEncountered = null
         )
      {
         return new AsyncSequentialOnlyEnumeratorObservableImpl<T, TMetadata>(
            initialMoveNext,
            SequentialCurrentInfoFactory.Get( typeof( T ) ),
            metadata,
            getGlobalBeforeEnumerationExecutionStart,
            getGlobalAfterEnumerationExecutionStart,
            getGlobalBeforeEnumerationExecutionEnd,
            getGlobalAfterEnumerationExecutionEnd,
            getGlobalAfterEnumerationExecutionItemEncountered
            );
      }

      /// <summary>
      /// Creates a new <see cref="AsyncEnumerator{T}"/>, which will support both parallel and sequential enumeration.
      /// </summary>
      /// <typeparam name="T">The type of the items being enumerated.</typeparam>
      /// <typeparam name="TMoveNext">The type of the state to pass from <see cref="SynchronousMoveNextDelegate{T}"/> to <see cref="GetNextItemAsyncDelegate{T, TMoveNextResult}"/>.</typeparam>
      /// <param name="hasNext">The synchronous callback to check whether there are more items left.</param>
      /// <param name="getNext">The asynchronous callback to retrieve an item, given current retrieval token.</param>
      /// <param name="dispose">The optional asynchronous callback to reset the returned <see cref="AsyncEnumerator{T}"/>.</param>
      /// <returns>A new <see cref="AsyncEnumerator{T}"/>.</returns>
      /// <exception cref="ArgumentNullException">If any of <paramref name="hasNext"/> or <paramref name="getNext"/> is <c>null</c>.</exception>
      /// <seealso cref="E_UtilPack.EnumerateSequentiallyAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/>
      /// <seealso cref="E_UtilPack.EnumerateSequentiallyAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/>
      /// <seealso cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/>
      /// <seealso cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/>
      public static AsyncEnumerator<T> CreateParallelEnumerator<T, TMoveNext>(
         SynchronousMoveNextDelegate<TMoveNext> hasNext,
         GetNextItemAsyncDelegate<T, TMoveNext> getNext,
         ResetAsyncDelegate dispose
         )
      {
         return new AsyncParallelEnumeratorImplSealed<T, TMoveNext>(
            hasNext,
            getNext,
            dispose
            );
      }

      /// <summary>
      /// Creates a new <see cref="AsyncEnumerator{T, TMetadata}"/>, which will support both parallel and sequential enumeration.
      /// </summary>
      /// <typeparam name="T">The type of the items being enumerated.</typeparam>
      /// <typeparam name="TMoveNext">The type of the state to pass from <see cref="SynchronousMoveNextDelegate{T}"/> to <see cref="GetNextItemAsyncDelegate{T, TMoveNextResult}"/>.</typeparam>
      /// <typeparam name="TMetadata">The type of the metadata for <see cref="AsyncEnumerator{T, TMetadata}"/> to hold.</typeparam>
      /// <param name="hasNext">The synchronous callback to check whether there are more items left.</param>
      /// <param name="getNext">The asynchronous callback to retrieve an item, given current retrieval token.</param>
      /// <param name="dispose">The optional asynchronous callback to reset the returned <see cref="AsyncEnumerator{T}"/>.</param>
      /// <param name="metadata">The metadata.</param>
      /// <returns>A new <see cref="AsyncEnumerator{T, TMetadata}"/>.</returns>
      /// <exception cref="ArgumentNullException">If any of <paramref name="hasNext"/> or <paramref name="getNext"/> is <c>null</c>.</exception>
      /// <seealso cref="E_UtilPack.EnumerateSequentiallyAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/>
      /// <seealso cref="E_UtilPack.EnumerateSequentiallyAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/>
      /// <seealso cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/>
      /// <seealso cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/>
      public static AsyncEnumerator<T, TMetadata> CreateParallelEnumerator<T, TMoveNext, TMetadata>(
         SynchronousMoveNextDelegate<TMoveNext> hasNext,
         GetNextItemAsyncDelegate<T, TMoveNext> getNext,
         ResetAsyncDelegate dispose,
         TMetadata metadata
         )
      {
         return new AsyncParallelEnumeratorImpl<T, TMoveNext, TMetadata>(
            hasNext,
            getNext,
            dispose,
            metadata
            );
      }

   }

   internal static class SequentialCurrentInfoFactory
   {
      private static readonly Dictionary<Type, TSequentialCurrentInfoFactory> CustomFactories = new Dictionary<Type, TSequentialCurrentInfoFactory>()
      {
         { typeof(Int32), (moveNext, disposeAsync, current) => new SequentialEnumeratorCurrentInfoWithInt32((MoveNextAsyncDelegate<Int32>)moveNext, disposeAsync, (Int32) current ) },
         { typeof(Int64), (moveNext, disposeAsync, current) => new SequentialEnumeratorCurrentInfoWithInt64((MoveNextAsyncDelegate<Int64>)moveNext, disposeAsync, (Int64)current ) }
      };

      internal static TSequentialCurrentInfoFactory Get( Type type )
      {
         CustomFactories.TryGetValue( type, out var retVal );
         return retVal;
      }
   }

   /// <summary>
   /// This delegate is the entrypoint for enumerating items sequentially asynchronously.
   /// It is used by classes implementing <see cref="AsyncEnumerator{T}"/> in order to provide the functionality.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <param name="token">The cancellation token which was passed to <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method.</param>
   /// <returns>A value task with information about initial <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> call.</returns>
   /// <remarks>
   /// The information is a tuple, where the elements are interpreted as following:
   /// <list type="number">
   /// <item><term><see cref="System.Boolean"/></term><description>Whether initial fetch was a success. Empty enumerable should return <c>false</c> here.</description></item>
   /// <item><term><typeparamref name="T"/></term><description>The item fetched.</description></item>
   /// <item><term><see cref="MoveNextAsyncDelegate{T}"/></term><description>The callback to fetch next item. Will only be used if initial fetch is a success.</description></item>
   /// <item><term><see cref="ResetAsyncDelegate"/></term><description>The callback to call after enumerable has been enumerated. This will be called even if initial fetch is not success.</description></item>
   /// </list>
   /// </remarks>
   /// <seealso cref="AsyncEnumeratorFactory.CreateSequentialEnumerator{T, TMetadata}(InitialMoveNextAsyncDelegate{T}, TMetadata)"/>
   /// <seealso cref="AsyncEnumeratorFactory.CreateSequentialEnumerator{T}(InitialMoveNextAsyncDelegate{T})"/>
   public delegate ValueTask<(Boolean, T, MoveNextAsyncDelegate<T>, ResetAsyncDelegate)> InitialMoveNextAsyncDelegate<T>( CancellationToken token );

   /// <summary>
   /// This delegate will be used by the sequential version of the <see cref="AsyncEnumerator{T}.MoveNextAsync"/> and <see cref="AsyncEnumerator{T}.TryResetAsync(CancellationToken)"/> methods.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <param name="token">The cancellation token which was passed to <see cref="AsyncEnumerator{T}.MoveNextAsync"/> or <see cref="AsyncEnumerator{T}.TryResetAsync(CancellationToken)"/> method.</param>
   /// <returns>A value task with information about next item fetched.</returns>
   /// <remarks>
   /// The information is a tuple, where the elements are interpreted as following:
   /// <list type="number">
   /// <item><term><see cref="System.Boolean"/></term><description>Whether this fetch was a success. If enumerable has no more items, this should be <c>false</c>.</description></item>
   /// <item><term><typeparamref name="T"/></term><description>The item fetched.</description></item>
   /// </list>
   /// </remarks>
   /// <seealso cref="AsyncEnumeratorFactory.CreateSequentialEnumerator{T, TMetadata}(InitialMoveNextAsyncDelegate{T}, TMetadata)"/>
   /// <seealso cref="AsyncEnumeratorFactory.CreateSequentialEnumerator{T}(InitialMoveNextAsyncDelegate{T})"/>
   public delegate ValueTask<(Boolean, T)> MoveNextAsyncDelegate<T>( CancellationToken token );

   /// <summary>
   /// This delegate will be used by the sequential version of the <see cref="AsyncEnumerator{T}.MoveNextAsync"/> and all versions of <see cref="AsyncEnumerator{T}.TryResetAsync(CancellationToken)"/> methods when enumeration end is encountered.
   /// </summary>
   /// <param name="token">The cancellation token which was passed to <see cref="AsyncEnumerator{T}.MoveNextAsync"/> or <see cref="AsyncEnumerator{T}.TryResetAsync(CancellationToken)"/> method.</param>
   /// <returns>A task to perform asynchronous disposing. If no asynchronous disposing is needed, this delegate can return <c>null</c>.</returns>
   /// <seealso cref="AsyncEnumeratorFactory.CreateSequentialEnumerator{T, TMetadata}(InitialMoveNextAsyncDelegate{T}, TMetadata)"/>
   /// <seealso cref="AsyncEnumeratorFactory.CreateSequentialEnumerator{T}(InitialMoveNextAsyncDelegate{T})"/>
   /// <seealso cref="AsyncEnumeratorFactory.CreateParallelEnumerator{T, TMoveNext}(SynchronousMoveNextDelegate{TMoveNext}, GetNextItemAsyncDelegate{T, TMoveNext}, ResetAsyncDelegate)"/>
   /// <seealso cref="AsyncEnumeratorFactory.CreateParallelEnumerator{T, TMoveNext, TMetadata}(SynchronousMoveNextDelegate{TMoveNext}, GetNextItemAsyncDelegate{T, TMoveNext}, ResetAsyncDelegate, TMetadata)"/>
   public delegate Task ResetAsyncDelegate( CancellationToken token );

   /// <summary>
   /// This delegate is used by parallel version of the <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method.
   /// </summary>
   /// <typeparam name="T">The type of state to pass to <see cref="GetNextItemAsyncDelegate{T, TMoveNextResult}"/>.</typeparam>
   /// <returns>A tuple with information about success movenext operation.</returns>
   /// <remarks>
   /// The information is a tuple, where the elements are interpreted as following:
   /// <list type="number">
   /// <item><term><see cref="System.Boolean"/></term><description>Whether this fetch was a success. If enumerable has no more items, this should be <c>false</c>.</description></item>
   /// <item><term><typeparamref name="T"/></term><description>The state to pass to <see cref="GetNextItemAsyncDelegate{T, TMoveNextResult}"/>.</description></item>
   /// </list>
   /// </remarks>
   /// <seealso cref="AsyncEnumeratorFactory.CreateParallelEnumerator{T, TMoveNext}(SynchronousMoveNextDelegate{TMoveNext}, GetNextItemAsyncDelegate{T, TMoveNext}, ResetAsyncDelegate)"/>
   /// <seealso cref="AsyncEnumeratorFactory.CreateParallelEnumerator{T, TMoveNext, TMetadata}(SynchronousMoveNextDelegate{TMoveNext}, GetNextItemAsyncDelegate{T, TMoveNext}, ResetAsyncDelegate, TMetadata)"/>
   public delegate (Boolean, T) SynchronousMoveNextDelegate<T>();

   /// <summary>
   /// This delegate is used by parallel version of the <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TMoveNextResult">The type of state returned by <see cref="SynchronousMoveNextDelegate{T}"/>.</typeparam>
   /// <param name="moveNextResult">The state component of the result of the <see cref="SynchronousMoveNextDelegate{T}"/> call.</param>
   /// <param name="token">The current cancellation token.</param>
   /// <returns>A task to potentially asycnhronously fetch the next item.</returns>
   /// <seealso cref="AsyncEnumeratorFactory.CreateParallelEnumerator{T, TMoveNext}(SynchronousMoveNextDelegate{TMoveNext}, GetNextItemAsyncDelegate{T, TMoveNext}, ResetAsyncDelegate)"/>
   /// <seealso cref="AsyncEnumeratorFactory.CreateParallelEnumerator{T, TMoveNext, TMetadata}(SynchronousMoveNextDelegate{TMoveNext}, GetNextItemAsyncDelegate{T, TMoveNext}, ResetAsyncDelegate, TMetadata)"/>
   public delegate ValueTask<T> GetNextItemAsyncDelegate<T, in TMoveNextResult>( TMoveNextResult moveNextResult, CancellationToken token );
}
