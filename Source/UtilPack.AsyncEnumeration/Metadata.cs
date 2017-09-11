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
   /// This interface augments <see cref="AsyncEnumerator{T}"/> with a getter to metadata of this enumeration.
   /// One example of such metadata usage could be e.g. SQL statement which this enumerator will enumerate.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   /// <typeparam name="TMetadata">The type of the metadata. This parameter is covariant.</typeparam>
   public interface AsyncEnumerator<out T, out TMetadata> : AsyncEnumerator<T>, ObjectWithMetadata<TMetadata>
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
}
