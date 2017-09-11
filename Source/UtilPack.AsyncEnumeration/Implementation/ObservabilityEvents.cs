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
   /// This class provides default implementation for <see cref="EnumerationStartedEventArgs{TMetadata}"/>.
   /// </summary>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <seealso cref="ObjectWithMetadata{TMetadata}.Metadata"/>
   /// <seealso cref="AsyncEnumerator{T, TMetadata}"/>
   public class EnumerationStartedEventArgsImpl<TMetadata> : EnumerationStartedEventArgs<TMetadata>
   {
      /// <summary>
      /// Creates a new instance of <see cref="EnumerationStartedEventArgsImpl{TMetadata}"/> with given metadata.
      /// </summary>
      /// <param name="metadata">The metadata.</param>
      public EnumerationStartedEventArgsImpl(
         TMetadata metadata
         )
      {
         this.Metadata = metadata;
      }

      /// <summary>
      /// Gets the metadata.
      /// </summary>
      /// <value>The metadata.</value>
      public TMetadata Metadata { get; }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="EnumerationItemEventArgs{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <seealso cref="AsyncEnumerator{T}.MoveNextAsync"/>
   /// <seealso cref="AsyncEnumerator{T}.OneTimeRetrieve"/>
   public class EnumerationItemEventArgsImpl<T> : EnumerationItemEventArgs<T>
   {
      /// <summary>
      /// Creates a new instance of <see cref="EnumerationItemEventArgsImpl{T}"/> with given item.
      /// </summary>
      /// <param name="item">The item that was encountered by <see cref="AsyncEnumerator{T}.MoveNextAsync"/>.</param>
      public EnumerationItemEventArgsImpl(
         T item
         )
      {
         this.Item = item;
      }

      /// <summary>
      /// Gets the item encountered by <see cref="AsyncEnumerator{T}.MoveNextAsync"/>.
      /// </summary>
      /// <value>The item encountered by <see cref="AsyncEnumerator{T}.MoveNextAsync"/>.</value>
      public T Item { get; }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="EnumerationEndedEventArgs{TMetadata}"/>.
   /// </summary>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <seealso cref="ObjectWithMetadata{TMetadata}.Metadata"/>
   /// <seealso cref="AsyncEnumerator{T, TMetadata}"/>
   public class EnumerationEndedEventArgsImpl<TMetadata> : EnumerationStartedEventArgsImpl<TMetadata>, EnumerationEndedEventArgs<TMetadata>
   {
      /// <summary>
      /// Creates a new instance of <see cref="EnumerationEndedEventArgsImpl{TMetadata}"/> with given metadata.
      /// </summary>
      /// <param name="metadata">The metadata.</param>
      public EnumerationEndedEventArgsImpl(
         TMetadata metadata
         ) : base( metadata )
      {
      }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="EnumerationItemEventArgs{T, TMetadata}"/>.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <seealso cref="AsyncEnumerator{T}.MoveNextAsync"/>
   /// <seealso cref="AsyncEnumerator{T}.OneTimeRetrieve"/>
   /// <seealso cref="ObjectWithMetadata{TMetadata}.Metadata"/>
   /// <seealso cref="AsyncEnumerator{T, TMetadata}"/>
   public class EnumerationItemEventArgsImpl<T, TMetadata> : EnumerationItemEventArgsImpl<T>, EnumerationItemEventArgs<T, TMetadata>
   {
      /// <summary>
      /// Creates a new instance of <see cref="EnumerationItemEventArgsImpl{T, TMetadata}"/> with given item and metadata.
      /// </summary>
      /// <param name="item">The item that was encountered by <see cref="AsyncEnumerator{T}.MoveNextAsync"/></param>
      /// <param name="metadata">The metadata.</param>
      public EnumerationItemEventArgsImpl(
         T item,
         TMetadata metadata
         )
         : base( item )
      {
         this.Metadata = metadata;
      }

      /// <summary>
      /// Gets the metadata.
      /// </summary>
      /// <value>The metadata.</value>
      public TMetadata Metadata { get; }
   }
}
