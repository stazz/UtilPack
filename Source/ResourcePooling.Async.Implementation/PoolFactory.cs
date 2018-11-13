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
using ResourcePooling.Async.Abstractions;
using ResourcePooling.Async.Implementation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace ResourcePooling.Async.Implementation
{
   /// <summary>
   /// This delegate can be used to customize what exactly happens when taking the resource from <see cref="AsyncResourcePool{TResource}"/> which caches resources.
   /// </summary>
   /// <typeparam name="TInstance">The type of objects stored in <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/> used by <see cref="AsyncResourcePool{TResource}"/>.</typeparam>
   /// <param name="pool">The <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/> pool where to acquire the instance.</param>
   /// <param name="token">The cancellation token to use.</param>
   /// <returns>Potentially asynchronously returns instance from <paramref name="pool"/>.</returns>
   public delegate ValueTask<TInstance> TakeFromInstancePoolDelegate<TInstance>( LocklessInstancePoolForClassesNoHeapAllocations<TInstance> pool, CancellationToken token )
      where TInstance : class, InstanceWithNextInfo<TInstance>;

   /// <summary>
   /// This delegate can be used to cusomize what exactly happens when returning the resource to <see cref="AsyncResourcePool{TResource}"/> which caches instances.
   /// </summary>
   /// <typeparam name="TInstance">The type of objects stored in <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/> used by <see cref="AsyncResourcePool{TResource}"/>.</typeparam>
   /// <param name="pool">The <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/> pool where to acquire the instance.</param>
   /// <param name="token">The cancellation token to use.</param>
   /// <param name="instance">The instance to return to <paramref name="pool"/>.</param>
   /// <returns>Potentially asynchronously returns value indicating whether <paramref name="instance"/> was returned to <paramref name="pool"/>.</returns>
   public delegate ValueTask<Boolean> ReturnToInstancePoolDelegate<TInstance>( LocklessInstancePoolForClassesNoHeapAllocations<TInstance> pool, CancellationToken token, TInstance instance )
      where TInstance : class, InstanceWithNextInfo<TInstance>;

   internal sealed class PoolLimitState
   {
      private const Int32 DEFAULT_TICK = 100;

      internal Int32 currentlyUsedUpResources;
      internal readonly Func<Int32> maxGetter;
      internal readonly TimeSpan tick;

      public PoolLimitState(
         Func<Int32> getMaximumConcurrentlyUsedResources,
         Int32 waitTick = DEFAULT_TICK
         )
      {
         this.maxGetter = ArgumentValidator.ValidateNotNull( nameof( getMaximumConcurrentlyUsedResources ), getMaximumConcurrentlyUsedResources );
         this.tick = TimeSpan.FromTicks( Math.Max( waitTick, DEFAULT_TICK ) );
      }

      internal async ValueTask<TInstance> GetFromPoolLimited<TInstance>(
         LocklessInstancePoolForClassesNoHeapAllocations<TInstance> pool,
         CancellationToken token
      )
         where TInstance : class, InstanceWithNextInfo<TInstance>
      {
         TInstance instance = null;
         var acquired = false;
         do
         {
            var seenMax = this.maxGetter();
            var seenCurrent = this.currentlyUsedUpResources;
            if ( seenCurrent >= seenMax )
            {
               await
#if NET40
               TaskEx
#else
               Task
#endif
               .Delay( this.tick, token );
            }
            else if ( Interlocked.CompareExchange( ref this.currentlyUsedUpResources, seenCurrent + 1, seenCurrent ) == seenCurrent )
            {
               acquired = true;
               instance = pool.TakeInstance();
            }
         } while ( !acquired && !token.IsCancellationRequested );

         return instance;
      }

      internal ValueTask<Boolean> ReturnToPoolLimited<TInstance>(
         LocklessInstancePoolForClassesNoHeapAllocations<TInstance> pool,
         CancellationToken token,
         TInstance instance
         )
         where TInstance : class, InstanceWithNextInfo<TInstance>
      {
         var retVal = instance != null;
         if ( retVal )
         {
            try
            {
               pool?.ReturnInstance( instance );
            }
            finally
            {
               Interlocked.Decrement( ref this.currentlyUsedUpResources );
            }
         }
         return new ValueTask<Boolean>( retVal );
      }
   }
}

public static partial class E_UtilPack
{

   /// <summary>
   /// Binds the given creation parameters and creates a <see cref="AsyncResourcePoolObservable{TResource}"/> which will always create new instance of resource when invoking <see cref="AsyncResourcePool{TResource}.UseResourceAsync"/> and <see cref="AsyncResourcePool{TResource}.TakeResourceAsync"/> methods.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <param name="factory">This <see cref="AsyncResourceFactory{TResource}"/>.</param>
   /// <returns>An <see cref="AsyncResourcePoolObservable{TResource}"/> using this <see cref="AsyncResourceFactory{TResource}"/> to create new resources.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourceFactory{TResource}"/> is <c>null</c>.</exception>
   public static AsyncResourcePoolObservable<TResource> CreateOneTimeUseResourcePool<TResource>(
      this AsyncResourceFactory<TResource> factory
      ) => new OneTimeUseAsyncResourcePool<TResource, AsyncResourceAcquireInfo<TResource>>(
         ArgumentValidator.ValidateNotNullReference( factory ),
         instance => instance,
         acquireInfo => acquireInfo
         );

   /// <summary>
   /// Binds the given creation parameters and creates a <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/> which will cache the resources it manages, but will not have upper bound on how many resources it caches. By calling <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync"/> method it will close all resources which have been opened for too long.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <param name="factory">This <see cref="AsyncResourceFactory{TResource}"/>.</param>
   /// <returns>An <see cref="AsyncResourcePoolObservable{TResource, TCleanUp}"/> using this <see cref="AsyncResourceFactory{TResource}"/> to create new resources.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourceFactory{TResource}"/> is <c>null</c>.</exception>
   public static AsyncResourcePoolObservable<TResource, TimeSpan> CreateTimeoutingResourcePool<TResource>(
     this AsyncResourceFactory<TResource> factory
     ) => factory.CreateGenericTimeoutingResourcePool( null, null );

   /// <summary>
   /// Binds the given creation parameters and creates a <see cref="AsyncResourcePoolObservable{TResource}"/> which will create up to given dynamic maximum of resources when invoking <see cref="AsyncResourcePool{TResource}.UseResourceAsync"/> and <see cref="AsyncResourcePool{TResource}.TakeResourceAsync"/> methods, but will never release them.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <param name="factory">This <see cref="AsyncResourceFactory{TResource}"/>.</param>
   /// <param name="getMaximumConcurrentlyUsedResources">The callback to get maximum amount of resources that can be concurrently in use.</param>
   /// <returns>An <see cref="AsyncResourcePoolObservable{TResource}"/> using this <see cref="AsyncResourceFactory{TResource}"/> to create new resources.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourceFactory{TResource}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="getMaximumConcurrentlyUsedResources"/> is <c>null</c>.</exception>
   public static AsyncResourcePoolObservable<TResource> CreateLimitedResourcePool<TResource>(
      this AsyncResourceFactory<TResource> factory,
      Func<Int32> getMaximumConcurrentlyUsedResources
      )
   {
      ArgumentValidator.ValidateNotNullReference( factory );
      var state = new PoolLimitState( getMaximumConcurrentlyUsedResources );
      return factory.CreateGenericCachingResourcePool(
         state.GetFromPoolLimited,
         state.ReturnToPoolLimited
         );
   }

   /// <summary>
   /// Binds the given creation parameters and creates a <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/> which will cache the resources it manages, and also will have a dynamic upper bound on how many resources it caches. By calling <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync"/> method it will close all resources which have been opened for too long.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <param name="factory">This <see cref="AsyncResourceFactory{TResource}"/>.</param>
   /// <param name="getMaximumConcurrentlyUsedResources">The callback to get maximum amount of resources that can be concurrently in use.</param>
   /// <returns>An <see cref="AsyncResourcePoolObservable{TResource, TCleanUp}"/> using this <see cref="AsyncResourceFactory{TResource}"/> to create new resources.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourceFactory{TResource}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="getMaximumConcurrentlyUsedResources"/> is <c>null</c>.</exception>
   public static AsyncResourcePoolObservable<TResource, TimeSpan> CreateTimeoutingAndLimitedResourcePool<TResource>(
      this AsyncResourceFactory<TResource> factory,
      Func<Int32> getMaximumConcurrentlyUsedResources
      )
   {
      ArgumentValidator.ValidateNotNullReference( factory );
      var state = new PoolLimitState( getMaximumConcurrentlyUsedResources );
      return factory.CreateGenericTimeoutingResourcePool(
         state.GetFromPoolLimited,
         state.ReturnToPoolLimited
         );
   }

   /// <summary>
   /// Binds the given creation parameters and creates a <see cref="AsyncResourcePoolObservable{TResource}"/> which will create up to given static maximum of resources when invoking <see cref="AsyncResourcePool{TResource}.UseResourceAsync"/> and <see cref="AsyncResourcePool{TResource}.TakeResourceAsync"/> methods.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <param name="factory">This <see cref="AsyncResourceFactory{TResource}"/>.</param>
   /// <param name="maximumConcurrentlyUsedResources">The static integer indicating the maximum amount of resources that can be concurrently in use.</param>
   /// <returns>An <see cref="AsyncResourcePoolObservable{TResource}"/> using this <see cref="AsyncResourceFactory{TResource}"/> to create new resources.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourceFactory{TResource}"/> is <c>null</c>.</exception>
   public static AsyncResourcePoolObservable<TResource> CreateLimitedResourcePool<TResource>(
      this AsyncResourceFactory<TResource> factory,
      Int32 maximumConcurrentlyUsedResources
      ) => factory.CreateLimitedResourcePool( () => maximumConcurrentlyUsedResources );

   /// <summary>
   /// Binds the given creation parameters and creates a <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/> which will cache the resources it manages, and also will have a static upper bound on how many resources it caches. By calling <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync"/> method it will close all resources which have been opened for too long.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <param name="factory">This <see cref="AsyncResourceFactory{TResource}"/>.</param>
   /// <param name="maximumConcurrentlyUsedResources">The static integer indicating the maximum amount of resources that can be concurrently in use.</param>
   /// <returns>An <see cref="AsyncResourcePoolObservable{TResource, TCleanUp}"/> using this <see cref="AsyncResourceFactory{TResource}"/> to create new resources.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourceFactory{TResource}"/> is <c>null</c>.</exception>
   public static AsyncResourcePoolObservable<TResource, TimeSpan> CreateTimeoutingAndLimitedResourcePool<TResource>(
       this AsyncResourceFactory<TResource> factory,
       Int32 maximumConcurrentlyUsedResources
       ) => factory.CreateTimeoutingAndLimitedResourcePool( () => maximumConcurrentlyUsedResources );

   /// <summary>
   /// Binds the given creation parameters and creates a <see cref="AsyncResourcePoolObservable{TResource}"/> which will call given callbacks when invoking <see cref="AsyncResourcePool{TResource}.UseResourceAsync"/>, <see cref="AsyncResourcePool{TResource}.TakeResourceAsync"/> and <see cref="AsyncResourcePool{TResource}.ReturnResource"/> methods.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <param name="factory">This <see cref="AsyncResourceFactory{TResource}"/>.</param>
   /// <param name="takeFromPool">The callback to call when taking the resource from the pool.</param>
   /// <param name="returnToPool">The callback to call when resource to the pool.</param>
   /// <returns>An <see cref="AsyncResourcePoolObservable{TResource}"/> using this <see cref="AsyncResourceFactory{TResource}"/> to create new resources.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourceFactory{TResource}"/> is <c>null</c>.</exception>
   /// <seealso cref="TakeFromInstancePoolDelegate{TInstance}"/>
   /// <seealso cref="ReturnToInstancePoolDelegate{TInstance}"/>
   public static AsyncResourcePoolObservable<TResource> CreateGenericCachingResourcePool<TResource>(
      this AsyncResourceFactory<TResource> factory,
      TakeFromInstancePoolDelegate<InstanceHolder<AsyncResourceAcquireInfo<TResource>>> takeFromPool,
      ReturnToInstancePoolDelegate<InstanceHolder<AsyncResourceAcquireInfo<TResource>>> returnToPool
      ) => new CachingAsyncResourcePool<TResource, InstanceHolder<AsyncResourceAcquireInfo<TResource>>>(
         ArgumentValidator.ValidateNotNullReference( factory ),
         holder => holder.Instance,
         info => new InstanceHolder<AsyncResourceAcquireInfo<TResource>>( info ),
         takeFromPool,
         returnToPool
         );

   /// <summary>
   /// Binds the given creation parameters and creates a <see cref="AsyncResourcePoolObservable{TResource, TCleanUp}"/> which will cache the resources, and call given callbacks when invoking <see cref="AsyncResourcePool{TResource}.UseResourceAsync"/>, <see cref="AsyncResourcePool{TResource}.TakeResourceAsync"/> and <see cref="AsyncResourcePool{TResource}.ReturnResource"/> methods. By calling <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync"/> method it will close all resources which have been opened for too long.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <param name="factory">This <see cref="AsyncResourceFactory{TResource}"/>.</param>
   /// <param name="takeFromPool">The callback to call when taking the resource from the pool.</param>
   /// <param name="returnToPool">The callback to call when resource to the pool.</param>
   /// <returns>An <see cref="AsyncResourcePoolObservable{TResource, TCleanUp}"/> using this <see cref="AsyncResourceFactory{TResource}"/> to create new resources.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourceFactory{TResource}"/> is <c>null</c>.</exception>
   /// <seealso cref="TakeFromInstancePoolDelegate{TInstance}"/>
   /// <seealso cref="ReturnToInstancePoolDelegate{TInstance}"/>
   public static AsyncResourcePoolObservable<TResource, TimeSpan> CreateGenericTimeoutingResourcePool<TResource>(
      this AsyncResourceFactory<TResource> factory,
      TakeFromInstancePoolDelegate<InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>> takeFromPool,
      ReturnToInstancePoolDelegate<InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>> returnToPool
      ) => new CachingAsyncResourcePoolWithTimeout<TResource>(
         ArgumentValidator.ValidateNotNullReference( factory ),
         takeFromPool,
         returnToPool
         );

   ///// <summary>
   ///// This helper method will create a wrapper around this <see cref="ExplicitAsyncResourcePoolObservable{TResource}"/> which will expose only <see cref="AsyncResourcePoolObservable{TResource}"/> portion of its API.
   ///// </summary>
   ///// <typeparam name="TResource">The type of resource.</typeparam>
   ///// <param name="explicitPool">This <see cref="ExplicitAsyncResourcePoolObservable{TResource}"/>.</param>
   ///// <returns>An <see cref="AsyncResourcePoolObservable{TResource}"/> which will delegate the calls to this <see cref="ExplicitAsyncResourcePoolObservable{TResource}"/>.</returns>
   ///// <exception cref="NullReferenceException">If this <see cref="ExplicitAsyncResourcePoolObservable{TResource}"/> is <c>null</c>.</exception>
   //public static AsyncResourcePoolObservable<TResource> WithoutExplicitAPI<TResource>( this ExplicitAsyncResourcePoolObservable<TResource> explicitPool )
   //    => new AsyncResourcePoolWrapper<TResource>( ArgumentValidator.ValidateNotNullReference( explicitPool ) );

   ///// <summary>
   ///// This helper method will create a wrapper around this <see cref="ExplicitAsyncResourcePoolObservable{TResource, TCleanUp}"/> which will expose only <see cref="AsyncResourcePoolObservable{TResource, TCleanUp}"/> portion of its API.
   ///// </summary>
   ///// <typeparam name="TResource">The type of resource.</typeparam>
   ///// <typeparam name="TCleanUp">The type of parameter for <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync"/> method.</typeparam>
   ///// <param name="explicitPool">This <see cref="ExplicitAsyncResourcePoolObservable{TResource, TCleanUp}"/>.</param>
   ///// <returns>An <see cref="AsyncResourcePoolObservable{TResource, TCleanUp}"/> which will delegate the calls to this <see cref="ExplicitAsyncResourcePoolObservable{TResource, TCleanUp}"/>.</returns>
   ///// <exception cref="NullReferenceException">If this <see cref="ExplicitAsyncResourcePoolObservable{TResource, TCleanUp}"/> is <c>null</c>.</exception>
   //public static AsyncResourcePoolObservable<TResource, TCleanUp> WithoutExplicitAPI<TResource, TCleanUp>( this ExplicitAsyncResourcePoolObservable<TResource, TCleanUp> explicitPool )
   //   => new CleanUpAsyncResourcePoolWrapper<TResource, TCleanUp>( ArgumentValidator.ValidateNotNullReference( explicitPool ) );
}
