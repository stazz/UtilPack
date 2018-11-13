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
   /// This class implements the <see cref="AsyncResourcePoolObservable{TResource}"/> interface in such way that the resource is disposed of after each use in <see cref="UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TResourceInstance">The type of instance holding the resource.</typeparam>
   /// <remarks>
   /// While this class is useful in simple scenarios, e.g. testing, the actual production environments most likely will want to use <see cref="CachingAsyncResourcePoolWithTimeout{TResource}"/> which is inherited from this class.
   /// </remarks>
   /// <seealso cref="CachingAsyncResourcePoolWithTimeout{TResource}"/>
   /// <seealso cref="CachingAsyncResourcePool{TResource, TResourceInstance}"/>
   internal class OneTimeUseAsyncResourcePool<TResource, TResourceInstance> : AsyncResourcePoolObservable<TResource>
   {

      /// <summary>
      /// Creates a new instance of <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance}"/>
      /// </summary>
      /// <param name="factory">The <see cref="AsyncResourceFactory{TResource, TParams}"/> to use for creation of new resources.</param>
      /// <param name="resourceExtractor">The callback to extract <see cref="AsyncResourceAcquireInfo{TResource}"/> from instances of <typeparamref name="TResourceInstance"/>.</param>
      /// <param name="instanceCreator">The callback to create a new instance of <typeparamref name="TResourceInstance"/> from existing <see cref="AsyncResourceAcquireInfo{TResource}"/>.</param>
      /// <exception cref="ArgumentNullException">If any of <paramref name="factory"/>, <paramref name="resourceExtractor"/>, or <paramref name="instanceCreator"/> is <c>null</c>.</exception>
      public OneTimeUseAsyncResourcePool(
         AsyncResourceFactory<TResource> factory,
         Func<TResourceInstance, AsyncResourceAcquireInfo<TResource>> resourceExtractor,
         Func<AsyncResourceAcquireInfo<TResource>, TResourceInstance> instanceCreator
      )
      {
         //this.FactoryParameters = factoryParameters;
         this.Factory = ArgumentValidator.ValidateNotNull( nameof( factory ), factory );
         this.ResourceExtractor = ArgumentValidator.ValidateNotNull( nameof( resourceExtractor ), resourceExtractor );
         this.InstanceCreator = ArgumentValidator.ValidateNotNull( nameof( instanceCreator ), instanceCreator );
      }

      /// <summary>
      /// Gets the <see cref="AsyncResourceFactory{TResource, TParams}"/> used to create new instances of resources.
      /// </summary>
      /// <value>The <see cref="AsyncResourceFactory{TResource, TParams}"/> used to create new instances of resources.</value>
      protected AsyncResourceFactory<TResource> Factory { get; }

      /// <summary>
      /// Gets the callback to extract <see cref="AsyncResourceAcquireInfo{TResource}"/> from instances of <typeparamref name="TResourceInstance"/>.
      /// </summary>
      /// <value>The callback to extract <see cref="AsyncResourceAcquireInfo{TResource}"/> from instances of <typeparamref name="TResourceInstance"/>.</value>
      protected Func<TResourceInstance, AsyncResourceAcquireInfo<TResource>> ResourceExtractor { get; }

      /// <summary>
      /// Gets the callback to create a new instance of <typeparamref name="TResourceInstance"/> from existing <see cref="AsyncResourceAcquireInfo{TResource}"/>.
      /// </summary>
      /// <value>The callback to create a new instance of <typeparamref name="TResourceInstance"/> from existing <see cref="AsyncResourceAcquireInfo{TResource}"/>.</value>
      protected Func<AsyncResourceAcquireInfo<TResource>, TResourceInstance> InstanceCreator { get; }

      /// <summary>
      /// Implements <see cref="ResourceFactoryInformation.ResetFactoryState"/> and calls the same method for this <see cref="Factory"/>.
      /// </summary>
      public void ResetFactoryState()
      {
         this.Factory.ResetFactoryState();
      }

      public ResourceUsage<TResource> GetResourceUsage( CancellationToken token )
      {
         var instanceAcquired = 0;
         TResourceInstance instance = default;
         ResourceUsageInfo<TResource> usage = default;
         return new ResourceUsageImpl<TResource>(
            async () =>
            {
               instance = await this.AcquireResourceAsync( token );
               Interlocked.Exchange( ref instanceAcquired, 1 );
               usage = this.ResourceExtractor( instance ).GetResourceUsageForToken( token );
               return usage.Resource;
            },
            async () =>
            {
               try
               {
                  usage?.Dispose();
               }
               finally
               {
                  if ( instanceAcquired == 1 )
                  {
                     await ( this.DisposeResourceAsync( instance, token ) ?? TaskUtils.CompletedTask );
                  }
               }
            }
            );
      }

      //public Task UseResourceAsync( Func<TResource, Task> user, CancellationToken token = default( CancellationToken ) )
      //{
      //   // Avoid potentially allocating new instance of Task on every call to this method, and check for user and token cancellation first.
      //   return user == null ? TaskUtils.CompletedTask : ( token.IsCancellationRequested ?
      //      TaskUtils.FromCanceled( token ) :
      //      this.DoUseResourceAsync( user, token )
      //      );

      //}

      //private async Task DoUseResourceAsync( Func<TResource, Task> executer, CancellationToken token )
      //{
      //   var instance = await this.AcquireResourceAsync( token );
      //   try
      //   {
      //      using ( var usageInfo = this.ResourceExtractor( instance ).GetResourceUsageForToken( token ) )
      //      {
      //         await executer( usageInfo.Resource );
      //      }
      //   }
      //   finally
      //   {
      //      await ( this.DisposeResourceAsync( instance, token ) ?? TaskUtils.CompletedTask );
      //   }
      //}

      //public async ValueTask<ExplicitResourceAcquireInfo<TResource>> TakeResourceAsync( CancellationToken token )
      //{
      //   var acquireResult = await this.AcquireResourceAsync( token );
      //   return new ExplicitResourceAcquireInfoImpl<TResource, TResourceInstance>( acquireResult, this.ResourceExtractor( acquireResult ), token );
      //}

      //public async ValueTask<Boolean> ReturnResource( ExplicitResourceAcquireInfo<TResource> resourceInfo )
      //{
      //   var retVal = false;
      //   if ( resourceInfo != null )
      //   {
      //      if ( resourceInfo is ExplicitResourceAcquireInfoImpl<TResource, TResourceInstance> resource )
      //      {
      //         retVal = true;
      //         resource.UsageInfo.DisposeSafely();
      //         await this.DisposeResourceAsync( resource.InstanceInfo, resource.Token );
      //      }
      //      else
      //      {
      //         await ( resourceInfo.Resource as IAsyncDisposable ).DisposeAsyncSafely();
      //         ( resourceInfo.Resource as IDisposable ).DisposeSafely();
      //      }
      //   }

      //   return retVal;
      //}

      /// <summary>
      /// Implements <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArgs}.AfterResourceCreationEvent"/>.
      /// </summary>
      public event GenericEventHandler<AfterAsyncResourceCreationEventArgs<TResource>> AfterResourceCreationEvent;

      /// <summary>
      /// Implements <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArgs}.AfterResourceAcquiringEvent"/>.
      /// </summary>
      public event GenericEventHandler<AfterAsyncResourceAcquiringEventArgs<TResource>> AfterResourceAcquiringEvent;

      /// <summary>
      /// Implements <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArgs}.BeforeResourceReturningEvent"/>.
      /// </summary>
      public event GenericEventHandler<BeforeAsyncResourceReturningEventArgs<TResource>> BeforeResourceReturningEvent;

      /// <summary>
      /// Implements <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArgs}.BeforeResourceCloseEvent"/>.
      /// </summary>
      public event GenericEventHandler<BeforeAsyncResourceCloseEventArgs<TResource>> BeforeResourceCloseEvent;

      /// <summary>
      /// Helper property to get instance of <see cref="AfterResourceAcquiringEvent"/> for derived classes.
      /// </summary>
      /// <value>The instance of <see cref="AfterResourceAcquiringEvent"/> for derived classes.</value>
      protected GenericEventHandler<AfterAsyncResourceAcquiringEventArgs<TResource>> AfterResourceAcquiringEventInstance => this.AfterResourceAcquiringEvent;

      /// <summary>
      /// This method is called by <see cref="UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> before invoking the given asynchronous callback.
      /// The implementation in this class always uses <see cref="AsyncResourceFactory{TResource}.CreateAcquireResourceContext(CancellationToken)"/> method of this <see cref="Factory"/>, but derived classes may override this method to cache previously used resources into a pool.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use.</param>
      /// <returns>A task which will have instance of <typeparamref name="TResourceInstance"/> upon completion.</returns>
      protected virtual async ValueTask<TResourceInstance> AcquireResourceAsync( CancellationToken token )
      {
         var connAcquireInfo = await this.Factory.AcquireResourceAsync( token );
         var creationEvent = this.AfterResourceCreationEvent;
         var acquireEvent = this.AfterResourceAcquiringEvent;
         if ( creationEvent != null || acquireEvent != null )
         {
            using ( var usageInfo = connAcquireInfo.GetResourceUsageForToken( token ) )
            {
               await creationEvent.InvokeAndWaitForAwaitables( new DefaultAfterAsyncResourceCreationEventArgs<TResource>( usageInfo.Resource ) );
               await acquireEvent.InvokeAndWaitForAwaitables( new DefaultAfterAsyncResourceAcquiringEventArgs<TResource>( usageInfo.Resource ) );
            }
         }

         return this.InstanceCreator( connAcquireInfo );
      }

      /// <summary>
      /// This method is called by <see cref="UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> after the asynchronous callback has finished using the resource.
      /// </summary>
      /// <param name="resource">The resource that was used by the asynchronous callback of <see cref="UseResourceAsync(Func{TResource, Task}, CancellationToken)"/>.</param>
      /// <param name="token">The <see cref="CancellationToken"/>.</param>
      /// <returns>Task which is completed when disposing the resource is completed.</returns>
      /// <remarks>
      /// This method simply calls <see cref="PerformDisposeResourceAsync(TResourceInstance, CancellationToken, ResourceDisposeKind)"/> and gives <c>true</c> to both boolean arguments.
      /// </remarks>
      protected virtual async Task DisposeResourceAsync( TResourceInstance resource, CancellationToken token )
      {
         await this.PerformDisposeResourceAsync( resource, token, ResourceDisposeKind.ReturnAndDispose );
      }

      /// <summary>
      /// This method will take care of invoking the <see cref="BeforeResourceReturningEvent"/> and <see cref="BeforeResourceCloseEvent"/> before actually disposing the given resource.
      /// </summary>
      /// <param name="resource">The resource instance.</param>
      /// <param name="token">The <see cref="CancellationToken"/>.</param>
      /// <param name="disposeKind">How to dispose the <paramref name="resource"/>.</param>
      /// <returns>A task which completes after all necessary event invocation and resource dispose code is done.</returns>
      /// <seealso cref="ResourceDisposeKind"/>
      protected async Task PerformDisposeResourceAsync(
         TResourceInstance resource,
         CancellationToken token,
         ResourceDisposeKind disposeKind
         )
      {
         var returningEvent = this.BeforeResourceReturningEvent;
         var closingEvent = this.BeforeResourceCloseEvent;
         var isResourceClosed = disposeKind.IsResourceClosed();
         var isResourceReturned = disposeKind.IsResourceReturned();
         if ( ( returningEvent != null && isResourceReturned ) || ( closingEvent != null && isResourceClosed ) )
         {
            using ( var usageInfo = this.ResourceExtractor( resource ).GetResourceUsageForToken( token ) )
            {
               if ( isResourceReturned )
               {
                  await returningEvent.InvokeAndWaitForAwaitables( new DefaultBeforeAsyncResourceReturningEventArgs<TResource>( usageInfo.Resource ) );
               }

               if ( isResourceClosed )
               {
                  await closingEvent.InvokeAndWaitForAwaitables( new DefaultBeforeAsyncResourceCloseEventArgs<TResource>( usageInfo.Resource ) );
               }
            }
         }

         if ( isResourceClosed )
         {
            await this.ResourceExtractor( resource ).DisposeAsyncSafely();
         }
      }



   }

   internal sealed class ResourceUsageImpl<TResource> : ResourceUsage<TResource>
   {
      private const Int32 INITIAL = 0;
      private const Int32 AWAITING = 1;
      private const Int32 AWAITED = 2;
      private const Int32 DISPOSING = 3;

      private Int32 _state;
      private readonly Func<Task<TResource>> _acquire;
      private TResource _resource;
      private readonly Func<Task> _dispose;

      public ResourceUsageImpl(
         Func<Task<TResource>> acquire,
         Func<Task> dispose
         )
      {
         this._acquire = ArgumentValidator.ValidateNotNull( nameof( acquire ), acquire );
         this._dispose = ArgumentValidator.ValidateNotNull( nameof( dispose ), dispose );
      }


      public async Task AwaitForResource()
      {
         if ( Interlocked.CompareExchange( ref this._state, AWAITING, INITIAL ) == INITIAL )
         {
            try
            {
               this._resource = await this._acquire();
            }
            finally
            {
               Interlocked.Exchange( ref this._state, AWAITED );
            }
         }
         else
         {
            throw new InvalidOperationException();
         }
      }

      public TResource Resource
      {
         get
         {
            if ( this._state != AWAITED )
            {
               throw new InvalidOperationException();
            }
            return this._resource;
         }
      }

      public async Task DisposeAsync()
      {
         if ( Interlocked.CompareExchange( ref this._state, DISPOSING, AWAITED ) == AWAITED
            || Interlocked.CompareExchange( ref this._state, DISPOSING, INITIAL ) == INITIAL
            )
         {
            await this._dispose();
         }
      }
   }

   /// <summary>
   /// This enumeration controls how <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance}.PerformDisposeResourceAsync(TResourceInstance, CancellationToken, ResourceDisposeKind)"/> method will behave.
   /// </summary>
   public enum ResourceDisposeKind
   {
      /// <summary>
      /// The resource is closed, but closing happens right after returning the resource to the pool.
      /// Both the <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArgs}.BeforeResourceReturningEvent"/> and <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArgs}.BeforeResourceCloseEvent"/> will be invoked, in that order.
      /// Finally, the resource will be asynchronously disposed of.
      /// </summary>
      ReturnAndDispose,

      /// <summary>
      /// The resource is returned to the pool, but not closed.
      /// Only <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArgs}.BeforeResourceReturningEvent"/> will be invoked, and the resource will not be disposed.
      /// </summary>
      OnlyReturn,

      /// <summary>
      /// The resource is closed, and not returned to the pool.
      /// Only <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArgs}.BeforeResourceCloseEvent"/> will be invoked, and resource will be then disposed.
      /// </summary>
      OnlyDispose
   }

   /// <summary>
   /// This class extends <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance}"/> and implements rudimentary pooling for resource instances.
   /// The pool is never cleared by this class though, since this class does not define any logic for how to perform clean-up operation for the pool.
   /// The <see cref="CachingAsyncResourcePoolWithTimeout{TResource}"/> extends this class and defines exactly that.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TCachedResource">The type of instance holding the resource. This will be the type for <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/> object used to pool resource instances.</typeparam>
   /// <remarks>
   /// This class is not very useful by itself - instead it provides some common implementation for pooling resource instances.
   /// End-users will find <see cref="CachingAsyncResourcePoolWithTimeout{TResource}"/> much more useful.
   /// </remarks>
   /// <seealso cref="CachingAsyncResourcePoolWithTimeout{TResource}"/>
   internal class CachingAsyncResourcePool<TResource, TCachedResource> : OneTimeUseAsyncResourcePool<TResource, TCachedResource>, IAsyncDisposableWithToken, IDisposable
      where TCachedResource : class, InstanceWithNextInfo<TCachedResource>
   {
      private const Int32 NOT_DISPOSED = 0;
      private const Int32 DISPOSED = 1;

      private Int32 _disposed;

      private readonly TakeFromInstancePoolDelegate<TCachedResource> _takeFromPool;
      private readonly ReturnToInstancePoolDelegate<TCachedResource> _returnToPool;

      ///// <summary>
      ///// Creates a new instance of <see cref="CachingAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/> with given parameters.
      ///// </summary>
      ///// <param name="factory">The <see cref="AsyncResourceFactory{TResource, TParams}"/> to use for creation of new resources.</param>
      ///// <param name="factoryParameters">The parameters to passe to <see cref="AsyncResourceFactory{TResource, TParams}"/> when creating new instances of resources.</param>
      ///// <param name="resourceExtractor">The callback to extract <see cref="AsyncResourceAcquireInfo{TResource}"/> from instances of <typeparamref name="TCachedResource"/>.</param>
      ///// <param name="instanceCreator">The callback to create a new instance of <typeparamref name="TCachedResource"/> from existing <see cref="AsyncResourceAcquireInfo{TResource}"/>.</param>
      ///// <exception cref="ArgumentNullException">If any of <paramref name="factory"/>, <paramref name="resourceExtractor"/>, or <paramref name="instanceCreator"/> is <c>null</c>.</exception>
      public CachingAsyncResourcePool(
         AsyncResourceFactory<TResource> factory,
         Func<TCachedResource, AsyncResourceAcquireInfo<TResource>> resourceExtractor,
         Func<AsyncResourceAcquireInfo<TResource>, TCachedResource> instanceCreator,
         TakeFromInstancePoolDelegate<TCachedResource> takeFromPool,
         ReturnToInstancePoolDelegate<TCachedResource> returnToPool
         ) : base( factory, resourceExtractor, instanceCreator )
      {
         this.Pool = new LocklessInstancePoolForClassesNoHeapAllocations<TCachedResource>();
         this._takeFromPool = takeFromPool ?? ( ( p, t ) => new ValueTask<TCachedResource>( p.TakeInstance() ) );
         this._returnToPool = returnToPool ?? ( ( p, t, i ) => { p?.ReturnInstance( i ); return new ValueTask<Boolean>( p != null && i != null ); } );
      }

      /// <summary>
      /// Gets the actual instance-caching pool used by this <see cref="CachingAsyncResourcePool{TResource, TResourceInstance}"/>.
      /// </summary>
      /// <value>The actual instance-caching pool used by this <see cref="CachingAsyncResourcePool{TResource, TResourceInstance}"/>.</value>
      /// <seealso cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/>
      protected LocklessInstancePoolForClassesNoHeapAllocations<TCachedResource> Pool { get; }

      /// <summary>
      /// Implements <see cref="IAsyncDisposableWithToken.DisposeAsync"/> method to empty the <see cref="Pool"/> from all resources and dispose them asynchronously.
      /// </summary>
      /// <param name="token">The cancellation token to use.</param>
      /// <returns>A task which will be completed when all resource instances in <see cref="Pool"/> are asynchronously disposed.</returns>
      public virtual async Task DisposeAsync( CancellationToken token )
      {
         if ( Interlocked.CompareExchange( ref this._disposed, DISPOSED, NOT_DISPOSED ) == NOT_DISPOSED )
         {
            TCachedResource conn;
            while ( ( conn = this.Pool.TakeInstance() ) != null )
            {
               try
               {
                  await base.PerformDisposeResourceAsync( conn, token, ResourceDisposeKind.OnlyDispose );
               }
               catch
               {
                  // Most likely we will not enter here, but better be safe than sorry.
               }
            }
         }
      }

      public Task DisposeAsync() => this.DisposeAsync( default );

      /// <summary>
      /// Implements <see cref="IDisposable.Dispose"/> to empty the <see cref="Pool"/> from all resources and dispose them synchronously.
      /// </summary>
      /// <remarks>
      /// The asynchronous disposing via <see cref="DisposeAsync(CancellationToken)"/> method is preferable, but this method can be used when there is no time to wait potentially long time for all resources to become disposed asynchronously.
      /// </remarks>
      public void Dispose()
      {
         if ( Interlocked.CompareExchange( ref this._disposed, DISPOSED, NOT_DISPOSED ) == NOT_DISPOSED )
         {
            this.DisposeAllInPool();
         }
      }

      private void DisposeAllInPool()
      {
         TCachedResource conn;
         while ( ( conn = this.Pool.TakeInstance() ) != null )
         {
            this.ResourceExtractor( conn ).DisposeSafely();
         }
      }

      /// <summary>
      /// This property checks whether this <see cref="CachingAsyncResourcePool{TResource, TResourceInstance}"/> is disposed, or being in process of disposing.
      /// </summary>
      /// <value><c>true</c> if this <see cref="CachingAsyncResourcePool{TResource, TResourceInstance}"/> is disposed, or being in process of disposing; <c>false</c> otherwise.</value>
      public Boolean Disposed => this._disposed != NOT_DISPOSED;

      /// <summary>
      /// This method overrides <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance}.AcquireResourceAsync(CancellationToken)"/> to implement logic where instead of always using <see cref="AsyncResourceFactory{TResource, TParams}"/>, the existing resource instance may be acquired from this <see cref="Pool"/>.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use when acquiring new resource from <see cref="AsyncResourceFactory{TResource, TParams}"/>.</param>
      /// <returns>A task which completes when the resource has been acquired.</returns>
      protected override async ValueTask<TCachedResource> AcquireResourceAsync( CancellationToken token )
      {
         if ( this.Disposed )
         {
            throw new ObjectDisposedException( "This pool is already disposed or being disposed of." );
         }

         var retVal = await this._takeFromPool( this.Pool, token );
         if ( retVal == null )
         {
            // Create resource and await for events
            retVal = await base.AcquireResourceAsync( token );
         }
         else
         {
            // Just await for acquire event
            var evt = this.AfterResourceAcquiringEventInstance;
            if ( evt != null )
            {
               using ( var usageInfo = this.ResourceExtractor( retVal ).GetResourceUsageForToken( token ) )
               {
                  await evt.InvokeAndWaitForAwaitables( new DefaultAfterAsyncResourceAcquiringEventArgs<TResource>( usageInfo.Resource ) );
               }
            }
         }

         return retVal;
      }

      /// <summary>
      /// This method overrides <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance}.DisposeResourceAsync(TResourceInstance, CancellationToken)"/> in order to return the resource to this <see cref="Pool"/>, if it is returnable back to the pool as specified by <see cref="AbstractResourceAcquireInfo.IsResourceReturnableToPool"/>.
      /// </summary>
      /// <param name="instance">The resource instance to dispose or to return to the <see cref="Pool"/>.</param>
      /// <param name="token">The <see cref="CancellationToken"/> to use when disposing asynchronously.</param>
      /// <returns>A task which completes once all events have been invoked and resource either disposed of or returned to this <see cref="Pool"/>.</returns>
      protected override async Task DisposeResourceAsync( TCachedResource instance, CancellationToken token )
      {
         await this.PerformDisposeResourceAsync( instance, token, ResourceDisposeKind.OnlyReturn );
         var info = this.ResourceExtractor( instance );

         if ( !this.Disposed && info.IsResourceReturnableToPool )
         {
            await this._returnToPool( this.Pool, token, instance );
            // We might've disposed or started to dispose asynchronously after returning to pool in such way that racing condition may occur.
            if ( this.Disposed )
            {
               this.DisposeAllInPool();
            }
         }
         else
         {
            await this._returnToPool( null, token, instance );
            await info.DisposeAsyncSafely( token );
         }
      }
   }

   /// <summary>
   /// This class extends <see cref="CachingAsyncResourcePool{TResource, TCachedResource}"/> to add information when the resource was returned to the pool, in order to be able to clean it up later using <see cref="CleanUpAsync(TimeSpan, CancellationToken)"/> method.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed to public.</typeparam>
   internal class CachingAsyncResourcePoolWithTimeout<TResource> : CachingAsyncResourcePool<TResource, InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>>, AsyncResourcePoolObservable<TResource, TimeSpan>
   {
      ///// <summary>
      ///// Creates a new instance of <see cref="CachingAsyncResourcePoolWithTimeout{TResource, TResourceCreationParams}"/> with given parameters.
      ///// </summary>
      ///// <param name="factory">The <see cref="AsyncResourceFactory{TResource, TParams}"/> to use when needed to create new instances of resource.</param>
      ///// <param name="factoryParameters">The parameters to pass when using <see cref="AsyncResourceFactory{TResource, TParams}.AcquireResourceAsync(TParams, CancellationToken)"/> of <paramref name="factory"/>.</param>
      ///// <exception cref="ArgumentNullException">If <paramref name="factory"/> is <c>null</c>.</exception>
      public CachingAsyncResourcePoolWithTimeout(
         AsyncResourceFactory<TResource> factory,
         TakeFromInstancePoolDelegate<InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>> takeFromPool,
         ReturnToInstancePoolDelegate<InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>> returnToPool
         )
         : base( factory, /*factoryParameters,*/ instance => instance.Instance, resource => new InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>>( resource ), takeFromPool, returnToPool )
      {
      }

      /// <summary>
      /// This method overrides <see cref="CachingAsyncResourcePool{TResource, TCachedResource}.DisposeResourceAsync(TCachedResource, CancellationToken)"/> to register return time of the resource.
      /// </summary>
      /// <param name="instance">The resource instance to dispose or to return to the <see cref="CachingAsyncResourcePool{TResource, TCachedResource}.Pool"/>.</param>
      /// <param name="token">The <see cref="CancellationToken"/> to use when disposing asynchronously.</param>
      /// <returns>A task which completes once all events have been invoked and resource either disposed of or returned to this <see cref="CachingAsyncResourcePool{TResource, TCachedResource}.Pool"/>.</returns>
      protected override async Task DisposeResourceAsync( InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>> instance, CancellationToken token )
      {
         instance.JustBeforePuttingBackToPool();
         await base.DisposeResourceAsync( instance, token );
      }

      /// <summary>
      /// This method implements <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync(TCleanUpParameter, CancellationToken)"/> with <see cref="TimeSpan"/> as clean-up parameter.
      /// The <paramref name="maxResourceIdleTime"/> will serve as limit: this method will dispose all resource instances which have been idle in the <see cref="CachingAsyncResourcePool{TResource, TCachedResource}.Pool"/> for longer than the limit.
      /// During this method execution, this <see cref="CachingAsyncResourcePoolWithTimeout{TResource}"/> will continue to be usable as normally.
      /// </summary>
      /// <param name="maxResourceIdleTime">The maximum idle time for resource in the <see cref="CachingAsyncResourcePool{TResource, TCachedResource}.Pool"/> for it to be returned back to the pool. If idle time is longer than (operator <c>&gt;</c>) this parameter, then the resource will be disposed.</param>
      /// <param name="token">The <see cref="CancellationToken"/> to use when disposing.</param>
      /// <returns>A task which will be complete when all of the resource instances of <see cref="CachingAsyncResourcePool{TResource, TCachedResource}.Pool"/> have been checked and resource eligible for clean-up will be disposed of.</returns>
      public async Task CleanUpAsync( TimeSpan maxResourceIdleTime, CancellationToken token )
      {
         InstanceHolderWithTimestamp<AsyncResourceAcquireInfo<TResource>> instance;
         var tasks = new List<Task>();
         while ( ( instance = this.Pool.TakeInstance() ) != null )
         {
            if ( DateTime.UtcNow - instance.TimeOfReturningBackToPool > maxResourceIdleTime )
            {
               tasks.Add( this.PerformDisposeResourceAsync( instance, token, ResourceDisposeKind.OnlyDispose ) );
            }
            else
            {
               this.Pool.ReturnInstance( instance );
            }
         }

         await
#if NET40
            TaskEx
#else
            Task
#endif
            .WhenAll( tasks );
      }
   }

   /// <summary>
   /// This class is used by <see cref="CachingAsyncResourcePoolWithTimeout{TResource}"/> to capture the time when resource instance was returned to the <see cref="CachingAsyncResourcePool{TResource, TCachedResource}.Pool"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   public sealed class InstanceHolderWithTimestamp<TResource> : InstanceWithNextInfo<InstanceHolderWithTimestamp<TResource>>
   {
      private InstanceHolderWithTimestamp<TResource> _next;
      private Object _lastChanged;

      /// <summary>
      /// Creates a new instance of <see cref="InstanceHolderWithTimestamp{TResource}"/>
      /// </summary>
      /// <param name="instance"></param>
      public InstanceHolderWithTimestamp( TResource instance )
      {
         this.Instance = instance;
      }

      /// <summary>
      /// Gets the resource instance.
      /// </summary>
      /// <value>The resoruce instance.</value>
      public TResource Instance { get; }

      /// <summary>
      /// Gets or sets the next <see cref="InstanceHolderWithTimestamp{TResource}"/> for this pool.
      /// </summary>
      /// <value>The next <see cref="InstanceHolderWithTimestamp{TResource}"/> for this pool.</value>
      public InstanceHolderWithTimestamp<TResource> Next
      {
         get
         {
            return this._next;
         }
         set
         {
            Interlocked.Exchange( ref this._next, value );
         }
      }

      /// <summary>
      /// Gets the <see cref="DateTime"/> when this instance was returned to pool.
      /// </summary>
      /// <value>The <see cref="DateTime"/> when this instance was returned to pool.</value>
      /// <exception cref="NullReferenceException">If this <see cref="InstanceHolderWithTimestamp{TResource}"/> has never been put back to the pool before.</exception>
      public DateTime TimeOfReturningBackToPool
      {
         get
         {
            return (DateTime) this._lastChanged;
         }
      }

      /// <summary>
      /// Marks this <see cref="InstanceHolderWithTimestamp{TResource}"/> as being returned to pool, thus updating the value of <see cref="TimeOfReturningBackToPool"/>.
      /// </summary>
      public void JustBeforePuttingBackToPool()
      {
         Interlocked.Exchange( ref this._lastChanged, DateTime.UtcNow );
      }
   }

   //internal sealed class ExplicitResourceAcquireInfoImpl<TResource, TInstance> : ExplicitResourceAcquireInfo<TResource>
   //{
   //   public ExplicitResourceAcquireInfoImpl(
   //      TInstance instance,
   //      AsyncResourceAcquireInfo<TResource> acquireInfo,
   //      CancellationToken token
   //      )
   //   {
   //      this.Token = token;
   //      this.InstanceInfo = instance;
   //      this.UsageInfo = acquireInfo.GetResourceUsageForToken( token );
   //   }


   //   public TResource Resource => this.UsageInfo.Resource;

   //   internal ResourceUsageInfo<TResource> UsageInfo { get; }

   //   internal TInstance InstanceInfo { get; }

   //   internal CancellationToken Token { get; }
   //}

   //internal class AsyncResourcePoolWrapper<TResource> : AsyncResourcePoolObservable<TResource>
   //{
   //   private readonly AsyncResourcePoolObservable<TResource> _actual;

   //   public AsyncResourcePoolWrapper( AsyncResourcePoolObservable<TResource> actual )
   //   {
   //      this._actual = ArgumentValidator.ValidateNotNull( nameof( actual ), actual );
   //   }

   //   public event GenericEventHandler<AfterAsyncResourceCreationEventArgs<TResource>> AfterResourceCreationEvent
   //   {
   //      add
   //      {
   //         this._actual.AfterResourceCreationEvent += value;
   //      }

   //      remove
   //      {
   //         this._actual.AfterResourceCreationEvent -= value;
   //      }
   //   }

   //   public event GenericEventHandler<AfterAsyncResourceAcquiringEventArgs<TResource>> AfterResourceAcquiringEvent
   //   {
   //      add
   //      {
   //         this._actual.AfterResourceAcquiringEvent += value;
   //      }

   //      remove
   //      {
   //         this._actual.AfterResourceAcquiringEvent -= value;
   //      }
   //   }

   //   public event GenericEventHandler<BeforeAsyncResourceReturningEventArgs<TResource>> BeforeResourceReturningEvent
   //   {
   //      add
   //      {
   //         this._actual.BeforeResourceReturningEvent += value;
   //      }

   //      remove
   //      {
   //         this._actual.BeforeResourceReturningEvent -= value;
   //      }
   //   }

   //   public event GenericEventHandler<BeforeAsyncResourceCloseEventArgs<TResource>> BeforeResourceCloseEvent
   //   {
   //      add
   //      {
   //         this._actual.BeforeResourceCloseEvent += value;
   //      }

   //      remove
   //      {
   //         this._actual.BeforeResourceCloseEvent -= value;
   //      }
   //   }

   //   public void ResetFactoryState()
   //   {
   //      this._actual.ResetFactoryState();
   //   }

   //   public Task UseResourceAsync( Func<TResource, Task> user, CancellationToken token )
   //   {
   //      return this._actual.UseResourceAsync( user, token );
   //   }
   //}

   //internal sealed class CleanUpAsyncResourcePoolWrapper<TResource, TCleanUpParameter> : AsyncResourcePoolWrapper<TResource>, AsyncResourcePoolObservable<TResource, TCleanUpParameter>
   //{
   //   private readonly AsyncResourcePoolObservable<TResource, TCleanUpParameter> _actual;

   //   public CleanUpAsyncResourcePoolWrapper(
   //      AsyncResourcePoolObservable<TResource, TCleanUpParameter> actual
   //      ) : base( actual )
   //   {
   //      this._actual = actual;
   //   }

   //   public Task CleanUpAsync( TCleanUpParameter cleanupParameter, CancellationToken token )
   //   {
   //      return this._actual.CleanUpAsync( cleanupParameter, token );
   //   }

   //   public void Dispose()
   //   {
   //      this._actual.Dispose();
   //   }
   //}
}



/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// Returns <c>true</c> if this <see cref="ResourceDisposeKind"/> is the kind which indicates returning resource to pool.
   /// </summary>
   /// <param name="kind">This <see cref="ResourceDisposeKind"/>.</param>
   /// <returns><c>true</c> if this <see cref="ResourceDisposeKind"/> is either <see cref="ResourceDisposeKind.ReturnAndDispose"/> or <see cref="ResourceDisposeKind.OnlyReturn"/>; <c>false</c> otherwise.</returns>
   public static Boolean IsResourceReturned( this ResourceDisposeKind kind )
   {
      return kind == ResourceDisposeKind.ReturnAndDispose || kind == ResourceDisposeKind.OnlyReturn;
   }

   /// <summary>
   /// Returns <c>true</c> if this <see cref="ResourceDisposeKind"/> is the kind which indicates disposal of resource.
   /// </summary>
   /// <param name="kind">This <see cref="ResourceDisposeKind"/>.</param>
   /// <returns><c>true</c> if this <see cref="ResourceDisposeKind"/> is either <see cref="ResourceDisposeKind.ReturnAndDispose"/> or <see cref="ResourceDisposeKind.OnlyDispose"/>; <c>false</c> otherwise.</returns>
   public static Boolean IsResourceClosed( this ResourceDisposeKind kind )
   {
      return kind == ResourceDisposeKind.ReturnAndDispose || kind == ResourceDisposeKind.OnlyDispose;
   }
}