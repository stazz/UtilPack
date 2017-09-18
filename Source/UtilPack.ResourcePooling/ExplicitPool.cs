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

namespace UtilPack.ResourcePooling
{
   /// <summary>
   /// This interface extends <see cref="AsyncResourcePool{TResource}"/> to provide explicit control of taking resource from the pool and returning it back.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <remarks>
   /// Because of the signatures of the methods this interface contains, the <typeparamref name="TResource"/> is not longer <c>out</c> covariant.
   /// </remarks>
   public interface ExplicitAsyncResourcePool<TResource> : AsyncResourcePool<TResource>
   {
      /// <summary>
      /// Potentially asynchronously obtains a resource this pool.
      /// </summary>
      /// <param name="token">The optional cancellation token to use when obtaining the resource.</param>
      /// <returns>An instance of <see cref="ExplicitResourceAcquireInfo{TResource}"/> which holds the resource, and should be passed to <see cref="ReturnResource"/> method once resource usage is over.</returns>
      ValueTask<ExplicitResourceAcquireInfo<TResource>> TakeResourceAsync( CancellationToken token = default );

      /// <summary>
      /// Returns the resource obtained from <see cref="TakeResourceAsync"/> method back to this pool.
      /// </summary>
      /// <param name="resourceInfo">The <see cref="ExplicitResourceAcquireInfo{TResource}"/> obtained from <see cref="TakeResourceAsync"/> method.</param>
      /// <returns>A task which completes once resource has been returned to the pool.</returns>
      ValueTask<Boolean> ReturnResource( ExplicitResourceAcquireInfo<TResource> resourceInfo );
   }

   /// <summary>
   /// This interface augments the <see cref="AsyncResourcePoolObservable{TResource}"/> with explicit resource management of <see cref="ExplicitAsyncResourcePool{TResource}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <remarks>
   /// Because of the signatures of the methods this interface contains, the <typeparamref name="TResource"/> is not longer <c>out</c> covariant.
   /// </remarks>
   public interface ExplicitAsyncResourcePoolObservable<TResource> : ExplicitAsyncResourcePool<TResource>, AsyncResourcePoolObservable<TResource>
   {

   }

   /// <summary>
   /// This interface augments the <see cref="AsyncResourcePool{TResource, TCleanUpParameters}"/> with explicit resource management of <see cref="ExplicitAsyncResourcePool{TResource}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TCleanUpParameter">The type of parameter for <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync"/> method.</typeparam>
   /// <remarks>
   /// Because of the signatures of the methods this interface contains, the <typeparamref name="TResource"/> is not longer <c>out</c> covariant.
   /// </remarks>
   public interface ExplicitAsyncResourcePool<TResource, in TCleanUpParameter> : ExplicitAsyncResourcePool<TResource>, AsyncResourcePool<TResource, TCleanUpParameter>
   {

   }

   /// <summary>
   /// This interface augments the <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/> with explicit resource management of <see cref="ExplicitAsyncResourcePool{TResource}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TCleanUpParameter">The type of parameter for <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync"/> method.</typeparam>
   /// <remarks>
   /// Because of the signatures of the methods this interface contains, the <typeparamref name="TResource"/> is not longer <c>out</c> covariant.
   /// </remarks>
   public interface ExplicitAsyncResourcePoolObservable<TResource, in TCleanUpParameter> : ExplicitAsyncResourcePool<TResource, TCleanUpParameter>, ExplicitAsyncResourcePoolObservable<TResource>, AsyncResourcePoolObservable<TResource, TCleanUpParameter>
   {

   }

   /// <summary>
   /// This interface provides the getter to get the resource after calling <see cref="ExplicitAsyncResourcePool{TResource}.TakeResourceAsync"/> method.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   public interface ExplicitResourceAcquireInfo<out TResource>
   {
      /// <summary>
      /// Gets the resource to be used.
      /// </summary>
      /// <value>The resource to be used.</value>
      TResource Resource { get; }
   }
}
