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
using UtilPack.ResourcePooling;

namespace UtilPack.ResourcePooling
{
   public delegate ValueTask<TInstance> TakeFromInstancePoolDelegate<TInstance>( LocklessInstancePoolForClassesNoHeapAllocations<TInstance> pool, CancellationToken token )
      where TInstance : class, InstanceWithNextInfo<TInstance>;

   public delegate ValueTask<Boolean> ReturnToInstancePoolDelegate<TInstance>( LocklessInstancePoolForClassesNoHeapAllocations<TInstance> pool, CancellationToken token, TInstance instance )
      where TInstance : class, InstanceWithNextInfo<TInstance>;
}

public static partial class E_UtilPack
{
   public static AsyncResourcePoolObservable<TResource> CreateOneTimeUseResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters
      )
   {
      return new AsyncResourcePoolWrapper<TResource>( factory.CreateExplicitOneTimeUseResourcePool( creationParameters ) );
   }

   public static ExplicitAsyncResourcePoolObservable<TResource> CreateExplicitOneTimeUseResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters
      )
   {
      return new OneTimeUseAsyncResourcePool<TResource, AsyncResourceAcquireInfo<TResource>, TResourceCreationParameters>(
         factory,
         creationParameters,
         instance => instance,
         acquireInfo => acquireInfo
         );
   }

   public static AsyncResourcePoolObservable<TResource, TimeSpan> CreateTimeoutingResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters
      )
   {
      return new CleanUpAsyncResourcePoolWrapper<TResource, TimeSpan>( factory.CreateExplicitTimeoutingResourcePool( creationParameters ) );
   }

   public static ExplicitAsyncResourcePoolObservable<TResource, TimeSpan> CreateExplicitTimeoutingResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters
      )
   {
      return new CachingAsyncResourcePoolWithTimeout<TResource, TResourceCreationParameters>(
         factory,
         creationParameters,
         null,
         null
         );
   }

   public static AsyncResourcePoolObservable<TResource> CreateLimitedResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      Func<Int32> getMaximumConcurrentlyUsedResources
      )
   {
      return new AsyncResourcePoolWrapper<TResource>( factory.CreateExplicitLimitedResourcePool( creationParameters, getMaximumConcurrentlyUsedResources ) );
   }

   public static ExplicitAsyncResourcePoolObservable<TResource> CreateExplicitLimitedResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      Func<Int32> getMaximumConcurrentlyUsedResources
      )
   {
      var state = new PoolLimitState( getMaximumConcurrentlyUsedResources );
      return factory.CreateExplicitGenericCachingResourcePool(
         creationParameters,
         ( pool, token ) => GetFromPoolLimited( pool, token, state ),
         ( pool, token, instance ) => ReturnToPoolLimited( pool, token, instance, state )
         );
   }

   public static AsyncResourcePoolObservable<TResource, TimeSpan> CreateTimeoutingAndLimitedResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      Func<Int32> getMaximumConcurrentlyUsedResources
      )
   {
      return new CleanUpAsyncResourcePoolWrapper<TResource, TimeSpan>( factory.CreateExplicitTimeoutingAndLimitedResourcePool( creationParameters, getMaximumConcurrentlyUsedResources ) );
   }

   public static ExplicitAsyncResourcePoolObservable<TResource, TimeSpan> CreateExplicitTimeoutingAndLimitedResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      Func<Int32> getMaximumConcurrentlyUsedResources
      )
   {
      var state = new PoolLimitState( getMaximumConcurrentlyUsedResources );
      return factory.CreateExplicitGenericTimeoutingResourcePool(
         creationParameters,
         ( pool, token ) => GetFromPoolLimited( pool, token, state ),
         ( pool, token, instance ) => ReturnToPoolLimited( pool, token, instance, state )
         );
   }

   public static AsyncResourcePoolObservable<TResource> CreateLimitedResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      Int32 maximumConcurrentlyUsedResources
   )
   {
      return factory.CreateLimitedResourcePool( creationParameters, () => maximumConcurrentlyUsedResources );
   }

   public static ExplicitAsyncResourcePoolObservable<TResource> CreateExplicitLimitedResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      Int32 maximumConcurrentlyUsedResources
      )
   {
      return factory.CreateExplicitLimitedResourcePool( creationParameters, () => maximumConcurrentlyUsedResources );
   }

   public static AsyncResourcePoolObservable<TResource, TimeSpan> CreateTimeoutingAndLimitedResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      Int32 maximumConcurrentlyUsedResources
      )
   {
      return factory.CreateTimeoutingAndLimitedResourcePool( creationParameters, () => maximumConcurrentlyUsedResources );
   }

   public static ExplicitAsyncResourcePoolObservable<TResource, TimeSpan> CreateExplicitTimeoutingAndLimitedResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      Int32 maximumConcurrentlyUsedResources
      )
   {
      return factory.CreateExplicitTimeoutingAndLimitedResourcePool( creationParameters, () => maximumConcurrentlyUsedResources );
   }

   public static AsyncResourcePoolObservable<TResource> CreateGenericCachingResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      TakeFromInstancePoolDelegate<InstanceHolder<AsyncResourceAcquireInfo<TResource>>> takeFromPool,
      ReturnToInstancePoolDelegate<InstanceHolder<AsyncResourceAcquireInfo<TResource>>> returnToPool
      )
   {
      return new AsyncResourcePoolWrapper<TResource>( factory.CreateExplicitGenericCachingResourcePool( creationParameters, takeFromPool, returnToPool ) );
   }

   public static ExplicitAsyncResourcePoolObservable<TResource> CreateExplicitGenericCachingResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      TakeFromInstancePoolDelegate<InstanceHolder<AsyncResourceAcquireInfo<TResource>>> takeFromPool,
      ReturnToInstancePoolDelegate<InstanceHolder<AsyncResourceAcquireInfo<TResource>>> returnToPool
      )
   {
      return new CachingAsyncResourcePool<TResource, InstanceHolder<AsyncResourceAcquireInfo<TResource>>, TResourceCreationParameters>(
         factory,
         creationParameters,
         holder => holder.Instance,
         info => new InstanceHolder<AsyncResourceAcquireInfo<TResource>>( info ),
         takeFromPool,
         returnToPool
         );
   }

   public static AsyncResourcePoolObservable<TResource, TimeSpan> CreateGenericTimeoutingResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      TakeFromInstancePoolDelegate<InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>> takeFromPool,
      ReturnToInstancePoolDelegate<InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>> returnToPool
      )
   {
      return new CleanUpAsyncResourcePoolWrapper<TResource, TimeSpan>( factory.CreateExplicitGenericTimeoutingResourcePool(
         creationParameters,
         takeFromPool,
         returnToPool
         ) );
   }

   public static ExplicitAsyncResourcePoolObservable<TResource, TimeSpan> CreateExplicitGenericTimeoutingResourcePool<TResource, TResourceCreationParameters>(
      this AsyncResourceFactory<TResource, TResourceCreationParameters> factory,
      TResourceCreationParameters creationParameters,
      TakeFromInstancePoolDelegate<InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>> takeFromPool,
      ReturnToInstancePoolDelegate<InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>> returnToPool
      )
   {
      return new CachingAsyncResourcePoolWithTimeout<TResource, TResourceCreationParameters>(
         factory,
         creationParameters,
         takeFromPool,
         returnToPool
         );
   }


   private sealed class PoolLimitState
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
   }

   private static async ValueTask<TInstance> GetFromPoolLimited<TInstance>(
      LocklessInstancePoolForClassesNoHeapAllocations<TInstance> pool,
      CancellationToken token,
      PoolLimitState state
      )
      where TInstance : class, InstanceWithNextInfo<TInstance>
   {
      TInstance instance = null;
      var acquired = false;
      do
      {
         var seenMax = state.maxGetter();
         var seenCurrent = state.currentlyUsedUpResources;
         if ( seenCurrent >= seenMax )
         {
            await
#if NET40
               TaskEx
#else
               Task
#endif
               .Delay( state.tick, token );
         }
         else if ( Interlocked.CompareExchange( ref state.currentlyUsedUpResources, seenCurrent + 1, seenCurrent ) == seenCurrent )
         {
            acquired = true;
            instance = pool.TakeInstance();
         }
      } while ( !acquired && !token.IsCancellationRequested );

      return instance;
   }

   private static ValueTask<Boolean> ReturnToPoolLimited<TInstance>(
      LocklessInstancePoolForClassesNoHeapAllocations<TInstance> pool,
      CancellationToken token,
      TInstance instance,
      PoolLimitState state
      )
      where TInstance : class, InstanceWithNextInfo<TInstance>
   {
      var retVal = instance != null;
      if ( retVal )
      {
         try
         {
            pool.ReturnInstance( instance );
         }
         finally
         {
            Interlocked.Decrement( ref state.currentlyUsedUpResources );
         }
      }
      return new ValueTask<Boolean>( retVal );
   }


   // TODO "Upper-bound-limited" caching pools, with configurable max amount of concurrently opened resources (passing somethign else than two nulls as last parameters)

}
