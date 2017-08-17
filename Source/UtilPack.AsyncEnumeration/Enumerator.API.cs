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
using UtilPack;
using UtilPack.AsyncEnumeration;

namespace UtilPack.AsyncEnumeration
{
   /// <summary>
   /// This interface mimics <see cref="IEnumerator{T}"/> for enumerators which can potentially cause asynchronous waiting.
   /// Such scenario is common in e.g. enumerating SQL query results.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   public interface AsyncEnumerator<out T>
   {
      /// <summary>
      /// This method mimics <see cref="System.Collections.IEnumerator.MoveNext"/> method in order to asynchronously read the next item.
      /// Please note that instead of directly using this method, one should use <see cref="E_UtilPack.EnumerateAsync{T}(AsyncEnumerator{T}, Action{T})"/> and <see cref="E_UtilPack.EnumerateAsync{T}(AsyncEnumerator{T}, Func{T, Task})"/> extension methods, as those methods will take care of properly finishing enumeration in case of exceptions.
      /// </summary>
      /// <returns>A task, which will return <c>true</c> if next item is encountered, and <c>false</c> if this enumeration ended.</returns>
      /// <remarks>
      /// The return type is <see cref="ValueTask{TResult}"/>, which helps abstracting away e.g. buffering functionality (since the one important motivation for buffering is to avoid allocating many <see cref="Task{TResult}"/> objects from heap).
      /// </remarks>
      /// <exception cref="InvalidOperationException">If this method is invoked concurrently (without waiting for previous invocation to complete).</exception>
      ValueTask<Boolean> MoveNextAsync( CancellationToken token = default( CancellationToken ) );

      /// <summary>
      /// This property mimics <see cref="IEnumerator{T}.Current"/> in order to get the item previously fetched by <see cref="MoveNextAsync"/>.
      /// </summary>
      /// <value>The item previously fetched by <see cref="MoveNextAsync"/>.</value>
      /// <remarks>
      /// Calling this property getter will never throw.
      /// </remarks>
      T Current { get; }

      /// <summary>
      /// This method mimics <see cref="System.Collections.IEnumerator.Reset"/> method in order to asynchronously reset this enumerator.
      /// </summary>
      /// <returns>A task, which will return <c>true</c> if reset is successful, and <c>false</c> otherwise.</returns>
      /// <remarks>
      /// Note that unlike <see cref="MoveNextAsync"/>, this method will not throw when invoked concurrently. Instead, it will just return <c>false</c>.
      /// </remarks>
      ValueTask<Boolean> TryResetAsync( CancellationToken token = default( CancellationToken ) );
   }

   /// <summary>
   /// This interface augments <see cref="AsyncEnumerator{T}"/> with a getter to metadata of this enumeration.
   /// One example of such metadata usage could be e.g. SQL statement which this enumerator will enumerate.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   /// <typeparam name="TMetadata">The type of the metadata. This parameter is covariant.</typeparam>
   public interface AsyncEnumerator<out T, out TMetadata> : AsyncEnumerator<T>, ObjectWithMetadata<TMetadata>
   {

   }

   /// <summary>
   /// This interface is common abstraction for anything with typed metadata object.
   /// </summary>
   /// <typeparam name="TMetadata">The type of the metadata. This parameter is covariant.</typeparam>
   public interface ObjectWithMetadata<out TMetadata>
   {
      /// <summary>
      /// Gets the metadata object supplied to this <see cref="AsyncEnumerator{T, TMetadata}"/> at creation time.
      /// </summary>
      /// <value>The metadata object supplied to this <see cref="AsyncEnumerator{T, TMetadata}"/> at creation time.</value>
      TMetadata Metadata { get; }
   }

   /// <summary>
   /// This interface augments <see cref="AsyncEnumerator{T}"/> with ability to observe various events that enumerating will cause.
   /// These events are contained in <see cref="AsyncEnumerationObservation{T}"/> interface.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   public interface AsyncEnumeratorObservable<out T> : AsyncEnumerator<T>, AsyncEnumerationObservation<T>
   {

   }

   /// <summary>
   /// This interface augments <see cref="AsyncEnumerator{T, Metadata}"/> with ability to observe various events that enumerating will cause.
   /// These events are contained in <see cref="AsyncEnumerationObservation{T, TMetadata}"/> interface.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   /// <typeparam name="TMetadata">The type of the metadata. This parameter is covariant.</typeparam>
   public interface AsyncEnumeratorObservable<out T, out TMetadata> : AsyncEnumerator<T, TMetadata>, AsyncEnumeratorObservable<T>, AsyncEnumerationObservation<T, TMetadata>
   {

   }

   /// <summary>
   /// This interface groups together all events which may occur when enumerating a <see cref="AsyncEnumerator{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   /// <remarks>
   /// In order to achieve covariance on <typeparamref name="T"/> (which may be very important requirement in certain situation), <see cref="GenericEventHandler{TArgs}"/> delegate is used instead of <see cref="EventHandler{TEventArgs}"/>.
   /// </remarks>
   public interface AsyncEnumerationObservation<out T>
   {
      /// <summary>
      /// This event occurs just before starting enumeration in initial <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call.
      /// </summary>
      event GenericEventHandler<EnumerationStartedEventArgs> BeforeEnumerationStart;

      /// <summary>
      /// This event occurs just after starting enumeration in initial <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call.
      /// </summary>
      event GenericEventHandler<EnumerationStartedEventArgs> AfterEnumerationStart;

      /// <summary>
      /// This event occurs after each time when next item is asynchronously fetched in <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call.
      /// </summary>
      event GenericEventHandler<EnumerationItemEventArgs<T>> AfterEnumerationItemEncountered;

      /// <summary>
      /// This event occurs after enumeration end is detected in <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call (causing it to return <c>false</c>).
      /// The difference to <see cref="AfterEnumerationEnd"/> event is that this event is triggered before asynchronous dispose action is invoked.
      /// </summary>
      event GenericEventHandler<EnumerationEndedEventArgs> BeforeEnumerationEnd;

      /// <summary>
      /// This event occurs after enumeration end is detected in <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call (causing it to return <c>false</c>).
      /// The difference to <see cref="BeforeEnumerationEnd"/> event is that this event is triggered after asynchronous dispose action is invoked.
      /// </summary>
      event GenericEventHandler<EnumerationEndedEventArgs> AfterEnumerationEnd;
   }

   /// <summary>
   /// This interface groups together all events which may occur when enumerating a <see cref="AsyncEnumerator{T, TMetadata}"/> which also has a metadata object bound to it.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   /// <typeparam name="TMetadata">The type of the metadata. This parameter is covariant.</typeparam>
   /// <remarks>
   /// In order to achieve covariance on <typeparamref name="T"/> and <typeparamref name="TMetadata"/> (which may be very important requirement in certain situation), <see cref="GenericEventHandler{TArgs}"/> delegate is used instead of <see cref="EventHandler{TEventArgs}"/>.
   /// </remarks>
   public interface AsyncEnumerationObservation<out T, out TMetadata> : AsyncEnumerationObservation<T>
   {
      /// <summary>
      /// This event occurs just before starting enumeration in initial <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call.
      /// </summary>
      new event GenericEventHandler<EnumerationStartedEventArgs<TMetadata>> BeforeEnumerationStart;

      /// <summary>
      /// This event occurs just after starting enumeration in initial <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call.
      /// </summary>
      new event GenericEventHandler<EnumerationStartedEventArgs<TMetadata>> AfterEnumerationStart;

      /// <summary>
      /// This event occurs after each time when next item is asynchronously fetched in <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call.
      /// </summary>
      new event GenericEventHandler<EnumerationItemEventArgs<T, TMetadata>> AfterEnumerationItemEncountered;

      /// <summary>
      /// This event occurs after enumeration end is detected in <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call (causing it to return <c>false</c>).
      /// The difference to <see cref="AfterEnumerationEnd"/> event is that this event is triggered before asynchronous dispose action is invoked.
      /// </summary>
      new event GenericEventHandler<EnumerationEndedEventArgs<TMetadata>> BeforeEnumerationEnd;

      /// <summary>
      /// This event occurs after enumeration end is detected in <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> method call (causing it to return <c>false</c>).
      /// The difference to <see cref="BeforeEnumerationEnd"/> event is that this event is triggered after asynchronous dispose action is invoked.
      /// </summary>
      new event GenericEventHandler<EnumerationEndedEventArgs<TMetadata>> AfterEnumerationEnd;
   }

   /// <summary>
   /// This interface is for event arguments object in <see cref="AsyncEnumerationObservation{T}.BeforeEnumerationStart"/> and <see cref="AsyncEnumerationObservation{T}.AfterEnumerationStart"/> events.
   /// </summary>
   /// <seealso cref="EnumerationEventArgsUtility.StatelessStartArgs"/>
   public interface EnumerationStartedEventArgs
   {
   }

   /// <summary>
   /// This interface is for event arguments object in <see cref="AsyncEnumerationObservation{T}.AfterEnumerationItemEncountered"/> event.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   /// <seealso cref="EnumerationItemEventArgsImpl{TEnumerableItem}"/>
   public interface EnumerationItemEventArgs<out T>
   {
      /// <summary>
      /// Gets the item that was fetched asynchronously.
      /// </summary>
      /// <value>The item that was fetched asynchronously.</value>
      T Item { get; }
   }

   /// <summary>
   /// This interface is for event arguments object in <see cref="AsyncEnumerationObservation{T}.BeforeEnumerationEnd"/> and <see cref="AsyncEnumerationObservation{T}.AfterEnumerationEnd"/> events.
   /// </summary>
   /// <seealso cref="EnumerationEventArgsUtility.StatelessEndArgs"/>
   public interface EnumerationEndedEventArgs : EnumerationStartedEventArgs
   {

   }

   /// <summary>
   /// This interface augments <see cref="EnumerationStartedEventArgs"/> with metadata object given to <see cref="AsyncEnumerator{T, TMetadata}"/>.
   /// </summary>
   /// <typeparam name="TMetadata">The type of the metadata. This parameter is covariant.</typeparam>
   public interface EnumerationStartedEventArgs<out TMetadata> : EnumerationStartedEventArgs, ObjectWithMetadata<TMetadata>
   {

   }

   /// <summary>
   /// This interface augments <see cref="EnumerationEndedEventArgs"/> with metadata object given to <see cref="AsyncEnumerator{T, TMetadata}"/>.
   /// </summary>
   /// <typeparam name="TMetadata">The type of the metadata. This parameter is covariant.</typeparam>
   public interface EnumerationEndedEventArgs<out TMetadata> : EnumerationStartedEventArgs<TMetadata>, EnumerationEndedEventArgs
   {

   }

   /// <summary>
   /// This interface augments <see cref="EnumerationItemEventArgs{T}"/> with metadata object given to <see cref="AsyncEnumerator{T, TMetadata}"/>.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   /// <typeparam name="TMetadata">The type of the metadata. This parameter is covariant.</typeparam>
   public interface EnumerationItemEventArgs<out T, out TMetadata> : EnumerationItemEventArgs<T>, ObjectWithMetadata<TMetadata>
   {

   }

   /// <summary>
   /// This static class contains few useful members when working with event arguments of events of <see cref="AsyncEnumerationObservation{T}"/> interface.
   /// </summary>
   public static class EnumerationEventArgsUtility
   {
      private sealed class EnumerationStarted : EnumerationStartedEventArgs
      {
      }

      private sealed class EnumerationEnded : EnumerationEndedEventArgs
      {

      }

      static EnumerationEventArgsUtility()
      {
         StatelessStartArgs = new EnumerationStarted();
         StatelessEndArgs = new EnumerationEnded();
      }

      /// <summary>
      /// Gets the stateless default instance of type <see cref="EnumerationStartedEventArgs"/>, used by <see cref="AsyncEnumerationObservation{T}.BeforeEnumerationStart"/> and <see cref="AsyncEnumerationObservation{T}.AfterEnumerationStart"/> events.
      /// </summary>
      /// <value>The stateless default instance of type <see cref="EnumerationStartedEventArgs"/>.</value>
      public static EnumerationStartedEventArgs StatelessStartArgs { get; }

      /// <summary>
      /// Gets the stateless default instance of type <see cref="EnumerationEndedEventArgs"/>, used by <see cref="AsyncEnumerationObservation{T}.BeforeEnumerationEnd"/> and <see cref="AsyncEnumerationObservation{T}.AfterEnumerationEnd"/> events.
      /// </summary>
      /// <value>The stateless default instance of type <see cref="EnumerationEndedEventArgs"/>.</value>
      public static EnumerationEndedEventArgs StatelessEndArgs { get; }
   }

   /// <summary>
   /// This delegate is the entrypoint for enumerating items asynchronously.
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
   /// <item><term><see cref="DisposeAsyncDelegate"/></term><description>The callback to call after enumerable has been enumerated. This will be called even if initial fetch is not success.</description></item>
   /// </list>
   /// </remarks>
   public delegate ValueTask<(Boolean, T, MoveNextAsyncDelegate<T>, DisposeAsyncDelegate)> InitialMoveNextAsyncDelegate<T>( CancellationToken token );

   /// <summary>
   /// This delegate will be used by <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> and <see cref="AsyncEnumerator{T}.TryResetAsync(CancellationToken)"/> methods.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <param name="token">The cancellation token which was passed to <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> or <see cref="AsyncEnumerator{T}.TryResetAsync(CancellationToken)"/> method.</param>
   /// <returns>A value task with information about next item fetched.</returns>
   /// <remarks>
   /// The information is a tuple, where the elements are interpreted as following:
   /// <list type="number">
   /// <item><term><see cref="System.Boolean"/></term><description>Whether this fetch was a success. If enumerable has no more items, this should be <c>false</c>.</description></item>
   /// <item><term><typeparamref name="T"/></term><description>The item fetched.</description></item>
   /// </list>
   /// </remarks>
   public delegate ValueTask<(Boolean, T)> MoveNextAsyncDelegate<T>( CancellationToken token );

   /// <summary>
   /// This delegate will be used by <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> and <see cref="AsyncEnumerator{T}.TryResetAsync(CancellationToken)"/> methods when enumeration end is encountered.
   /// </summary>
   /// <param name="token">The cancellation token which was passed to <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/> or <see cref="AsyncEnumerator{T}.TryResetAsync(CancellationToken)"/> method.</param>
   /// <returns>A task to perform asynchronous disposing. If no asynchronous disposing is needed, this delegate can return <c>null</c>.</returns>
   public delegate Task DisposeAsyncDelegate( CancellationToken token );

   /// <summary>
   /// This class provides static factory methods to create objects of type <see cref="AsyncEnumerator{T}"/>, <see cref="AsyncEnumerator{T, TMetadata}"/>, <see cref="AsyncEnumeratorObservable{T}"/>, and <see cref="AsyncEnumeratorObservableImpl{T, TMetadata}"/>.
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
      public static AsyncEnumerator<T> CreateEnumerator<T>(
         InitialMoveNextAsyncDelegate<T> initialMoveNext
         )
      {
         return new AsyncEnumeratorImplNonObservable<T>( initialMoveNext );
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
      public static AsyncEnumerator<T, TMetadata> CreateEnumerator<T, TMetadata>(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         TMetadata metadata
         )
      {
         return new AsyncEnumeratorImplNonObservable<T, TMetadata>( initialMoveNext, metadata );
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
      public static AsyncEnumeratorObservable<T> CreateObservableEnumerator<T>(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         Func<GenericEventHandler<EnumerationStartedEventArgs>> getGlobalBeforeEnumerationExecutionStart = null,
         Func<GenericEventHandler<EnumerationStartedEventArgs>> getGlobalAfterEnumerationExecutionStart = null,
         Func<GenericEventHandler<EnumerationEndedEventArgs>> getGlobalBeforeEnumerationExecutionEnd = null,
         Func<GenericEventHandler<EnumerationEndedEventArgs>> getGlobalAfterEnumerationExecutionEnd = null,
         Func<GenericEventHandler<EnumerationItemEventArgs<T>>> getGlobalAfterEnumerationExecutionItemEncountered = null
         )
      {
         return new AsyncEnumeratorObservableImpl<T>(
            initialMoveNext,
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
      public static AsyncEnumeratorObservable<T, TMetadata> CreateObservableEnumerator<T, TMetadata>(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         TMetadata metadata,
         Func<GenericEventHandler<EnumerationStartedEventArgs<TMetadata>>> getGlobalBeforeEnumerationExecutionStart = null,
         Func<GenericEventHandler<EnumerationStartedEventArgs<TMetadata>>> getGlobalAfterEnumerationExecutionStart = null,
         Func<GenericEventHandler<EnumerationEndedEventArgs<TMetadata>>> getGlobalBeforeEnumerationExecutionEnd = null,
         Func<GenericEventHandler<EnumerationEndedEventArgs<TMetadata>>> getGlobalAfterEnumerationExecutionEnd = null,
         Func<GenericEventHandler<EnumerationItemEventArgs<T, TMetadata>>> getGlobalAfterEnumerationExecutionItemEncountered = null
         )
      {
         return new AsyncEnumeratorObservableImpl<T, TMetadata>(
            initialMoveNext,
            metadata,
            getGlobalBeforeEnumerationExecutionStart,
            getGlobalAfterEnumerationExecutionStart,
            getGlobalBeforeEnumerationExecutionEnd,
            getGlobalAfterEnumerationExecutionEnd,
            getGlobalAfterEnumerationExecutionItemEncountered
            );
      }

   }

}

/// <summary>
/// This class contains extension methods for UtilPack types.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// This is helper method to enumerate a <see cref="AsyncEnumerator{T}"/> and properly dispose it in case of exception.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <param name="enumerator">This <see cref="AsyncEnumerator{T}"/>.</param>
   /// <param name="action">The callback to invoke for each item. May be <c>null</c>.</param>
   /// <returns>A task which will have enumerated the <see cref="AsyncEnumerator{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   public static async ValueTask<Int64> EnumerateAsync<T>( this AsyncEnumerator<T> enumerator, Action<T> action )
   {
      ArgumentValidator.ValidateNotNullReference( enumerator );
      try
      {
         var retVal = 0L;
         while ( await enumerator.MoveNextAsync() )
         {
            ++retVal;
            action?.Invoke( enumerator.Current );
         }

         return retVal;
      }
      catch
      {
         try
         {
            while ( await enumerator.MoveNextAsync() ) ;
         }
         catch
         {
            // Ignore
         }

         throw;
      }
   }

   /// <summary>
   /// This is helper method to enumerate a <see cref="AsyncEnumerator{T}"/> and properly dispose it in case of an exception.
   /// For each item, a task from given callback is awaited for, if it is not <c>null</c>.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <param name="enumerator">This <see cref="AsyncEnumerator{T}"/>.</param>
   /// <param name="asyncAction">The callback to invoke for each item. May be <c>null</c>, and may also return <c>null</c>.</param>
   /// <returns>A task which will have enumerated the <see cref="AsyncEnumerator{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
   public static async ValueTask<Int64> EnumerateAsync<T>( this AsyncEnumerator<T> enumerator, Func<T, Task> asyncAction )
   {
      try
      {
         var retVal = 0L;
         while ( await enumerator.MoveNextAsync() )
         {
            ++retVal;
            Task task;
            if ( asyncAction != null && ( task = asyncAction( enumerator.Current ) ) != null )
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
            while ( await enumerator.MoveNextAsync() ) ;
         }
         catch
         {
            // Ignore
         }

         throw;
      }
   }
}