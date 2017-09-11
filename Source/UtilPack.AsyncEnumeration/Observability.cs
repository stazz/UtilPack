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

namespace UtilPack.AsyncEnumeration
{
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
      /// This event occurs just before starting enumeration in initial <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call.
      /// </summary>
      event GenericEventHandler<EnumerationStartedEventArgs> BeforeEnumerationStart;

      /// <summary>
      /// This event occurs just after starting enumeration in initial <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call.
      /// </summary>
      event GenericEventHandler<EnumerationStartedEventArgs> AfterEnumerationStart;

      /// <summary>
      /// This event occurs after each time when next item is asynchronously fetched in <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call.
      /// </summary>
      event GenericEventHandler<EnumerationItemEventArgs<T>> AfterEnumerationItemEncountered;

      /// <summary>
      /// This event occurs after enumeration end is detected in <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call (causing it to return <c>false</c>).
      /// The difference to <see cref="AfterEnumerationEnd"/> event is that this event is triggered before asynchronous dispose action is invoked.
      /// </summary>
      event GenericEventHandler<EnumerationEndedEventArgs> BeforeEnumerationEnd;

      /// <summary>
      /// This event occurs after enumeration end is detected in <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call (causing it to return <c>false</c>).
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
      /// This event occurs just before starting enumeration in initial <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call.
      /// </summary>
      new event GenericEventHandler<EnumerationStartedEventArgs<TMetadata>> BeforeEnumerationStart;

      /// <summary>
      /// This event occurs just after starting enumeration in initial <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call.
      /// </summary>
      new event GenericEventHandler<EnumerationStartedEventArgs<TMetadata>> AfterEnumerationStart;

      /// <summary>
      /// This event occurs after each time when next item is asynchronously fetched in <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call.
      /// </summary>
      new event GenericEventHandler<EnumerationItemEventArgs<T, TMetadata>> AfterEnumerationItemEncountered;

      /// <summary>
      /// This event occurs after enumeration end is detected in <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call (causing it to return <c>false</c>).
      /// The difference to <see cref="AfterEnumerationEnd"/> event is that this event is triggered before asynchronous dispose action is invoked.
      /// </summary>
      new event GenericEventHandler<EnumerationEndedEventArgs<TMetadata>> BeforeEnumerationEnd;

      /// <summary>
      /// This event occurs after enumeration end is detected in <see cref="AsyncEnumerator{T}.MoveNextAsync"/> method call (causing it to return <c>false</c>).
      /// The difference to <see cref="BeforeEnumerationEnd"/> event is that this event is triggered after asynchronous dispose action is invoked.
      /// </summary>
      new event GenericEventHandler<EnumerationEndedEventArgs<TMetadata>> AfterEnumerationEnd;
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
         internal EnumerationStarted()
         {

         }
      }

      private sealed class EnumerationEnded : EnumerationEndedEventArgs
      {
         internal EnumerationEnded()
         {

         }
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
}
