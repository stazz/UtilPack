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
   /// <summary>
   /// This interface is used by <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/> and derivatives to create a new instance of resource.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TParams">The type of parameters used to create a resource.</typeparam>
   public interface ResourceFactory<TResource, in TParams>
   {
      /// <summary>
      /// Asynchronously creates a new instance of resource with given parameters.
      /// </summary>
      /// <param name="parameters">Parameters that are required for resource creation.</param>
      /// <param name="token">Cancellation token to use during resource creation.</param>
      /// <returns>A task which returns a <see cref="ResourceAcquireInfo{TResource}"/> upon completion.</returns>
      ValueTask<ResourceAcquireInfo<TResource>> AcquireResourceAsync( TParams parameters, CancellationToken token );
   }

   /// <summary>
   /// This interface provides information about resource which has been created by <see cref="ResourceFactory{TResource, TParams}"/>.
   /// Use <see cref="IAsyncDisposable.DisposeAsync(CancellationToken)"/> when it is ok to wait for proper resource disposal.
   /// Use <see cref="IDisposable.Dispose"/> method only when it is necessary to immediately close underlying resources.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   public interface ResourceAcquireInfo<out TResource> : IAsyncDisposable, IDisposable
   {
      /// <summary>
      /// Gets the <see cref="ResourceUsageInfo{TResource}"/> to use a resource in association with given <see cref="CancellationToken"/>.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use when reacting to cancellation.</param>
      /// <returns>The <see cref="ResourceUsageInfo{TResource}"/>.</returns>
      /// <seealso cref="ResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/>
      /// <seealso cref="CancelableResourceUsageInfo{TResource}"/>
      ResourceUsageInfo<TResource> GetResourceUsageForToken( CancellationToken token );

      // Return false if e.g. cancellation caused resource disposing.
      /// <summary>
      /// Gets the value indicating whether it is ok to return the resource of this <see cref="ResourceAcquireInfo{TResource}"/> to the pool.
      /// </summary>
      /// <value>The value indicating whether it is ok to return the resource of this <see cref="ResourceAcquireInfo{TResource}"/> to the pool.</value>
      Boolean IsResourceReturnableToPool { get; }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="ResourceAcquireInfo{TResource}"/> using <see cref="CancelableResourceUsageInfo{TResource}"/>.
   /// This class assumes that there is some kind of disposable resource object that is used to communicate with remote resource, represented by <typeparamref name="TPrivateResource"/>.
   /// </summary>
   /// <typeparam name="TPublicResource">The public type of the resource.</typeparam>
   /// <typeparam name="TPrivateResource">The actual type of underlying stream or other disposable resource.</typeparam>
   /// <remarks>
   /// Because most (all?) IO async methods are not cancelable via <see cref="CancellationToken"/> once the underlying native IO calls are invoked, this class simply disposes the underlying object.
   /// Only that way the any pending async IO calls get completed, since the exception will be thrown for them after disposing.
   /// It is not the most optimal solution, but it is how currently things are to be done, if we desire truly cancelable async IO calls.
   /// The alternative (lack of cancelability via <see cref="CancellationToken"/>) is worse option.
   /// </remarks>
   public abstract class ResourceAcquireInfoImpl<TPublicResource, TPrivateResource> : AbstractDisposable, ResourceAcquireInfo<TPublicResource>
   {
      private const Int32 NOT_CANCELED = 0;
      private const Int32 CANCELED = 1;

      private Int32 _cancellationState;

      private readonly Action<TPublicResource, CancellationToken> _setCancellationToken;
      private readonly Action<TPublicResource> _resetCancellationToken;

      /// <summary>
      /// Creates a new instance of <see cref="ResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/> with given resource and disposable object.
      /// </summary>
      /// <param name="publicResource">The public resource.</param>
      /// <param name="privateResource">The private resource or other disposable object.</param>
      /// <param name="setCancellationToken">The callback to set cancellation token to some external resource. Will be invoked in this constructor.</param>
      /// <param name="resetCancellationToken">The callback to reset cancellation token to some external resource. Will be invoked in <see cref="AbstractDisposable.Dispose(bool)"/> method.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="publicResource"/> or <paramref name="privateResource"/> is <c>null</c>.</exception>
      public ResourceAcquireInfoImpl(
         TPublicResource publicResource,
         TPrivateResource privateResource,
         Action<TPublicResource, CancellationToken> setCancellationToken,
         Action<TPublicResource> resetCancellationToken
         )
      {
         this.PublicResource = publicResource;
         this.Channel = privateResource;
         this._setCancellationToken = setCancellationToken;
         this._resetCancellationToken = resetCancellationToken;
      }

      /// <summary>
      /// Gets the value indicating whether this <see cref="ResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/> can be returned back to resource pool.
      /// </summary>
      /// <value>The value indicating whether this <see cref="ResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/> can be returned back to resource pool.</value>
      /// <remarks>
      /// On cancellation via <see cref="CancellationToken"/>, this <see cref="ResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/> will dispose the object used to communicate with remote resource.
      /// Because of this, this property will return <c>false</c> when the <see cref="CancellationToken"/> receives cancellation signal, or when <see cref="PublicResourceCanBeReturnedToPool"/> returns <c>false</c>.
      /// </remarks>
      public Boolean IsResourceReturnableToPool => this._cancellationState == NOT_CANCELED && this.PublicResourceCanBeReturnedToPool();

      /// <summary>
      /// This method implements <see cref="IAsyncDisposable.DisposeAsync(CancellationToken)"/> and will invoke <see cref="DisposeBeforeClosingChannel(CancellationToken)"/> before disposing this <see cref="Channel"/>.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use when disposing.</param>
      /// <returns>A task which will complete once asynchronous diposing routine is done and this <see cref="Channel"/> is closed.</returns>
      public async Task DisposeAsync( CancellationToken token )
      {
         try
         {
            await ( this.DisposeBeforeClosingChannel( token ) ?? TaskUtils.CompletedTask );
         }
         finally
         {
            this.Dispose( true );
         }
      }

      /// <summary>
      /// Returns a new <see cref="ResourceUsageInfo{TResource}"/> in order to start logical scope of using resource, typically at the start of <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.
      /// </summary>
      /// <param name="token">The cancellation token for this resource usage scenario.</param>
      /// <returns>A new instance of <see cref="ResourceUsageInfo{TResource}"/> which should have its <see cref="IDisposable.Dispose"/> method called when the usage scenario ends.</returns>
      public ResourceUsageInfo<TPublicResource> GetResourceUsageForToken( CancellationToken token )
      {
         return new CancelableResourceUsageInfo<TPublicResource>(
            this.PublicResource,
            token,
            token.Register( () =>
            {
               try
               {
                  Interlocked.Exchange( ref this._cancellationState, CANCELED );
               }
               finally
               {
                  // Since the Read/WriteAsync methods for e.g. NetworkStream are not truly async, we must close the whole resource on cancellation token cancel.
                  // This will cause exception to be thrown from Read/WriteAsync methods, and thus allow for execution to proceed, instead of remaining stuck in Read/WriteAsync methods.
                  this.Dispose( true );
               }
            } ),
            this._setCancellationToken,
            this._resetCancellationToken
            );
      }

      /// <summary>
      /// Gets the public resource of this <see cref="ResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/>.
      /// </summary>
      /// <value>The public resource of this <see cref="ResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/>.</value>
      protected TPublicResource PublicResource { get; }

      /// <summary>
      /// Gets the object that is used to communicate with remote resource that this <see cref="PublicResource"/> represents.
      /// </summary>
      /// <value>The object that is used to communicate with remote resource that this <see cref="PublicResource"/> represents.</value>
      protected TPrivateResource Channel { get; }

      /// <summary>
      /// Derived classes should implement the custom logic when closing the resource.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use during disposing.</param>
      /// <returns>A task which performs disposing asynchronously, or <c>null</c> if disposing has been done synchronously.</returns>
      /// <remarks>
      /// Typically disposing process involves sending some data via this <see cref="Channel"/> to the remote resource indicating that this end is closing the resource.
      /// </remarks>
      protected abstract Task DisposeBeforeClosingChannel( CancellationToken token );

      /// <summary>
      /// This method should be overridden by derived classes to perform additional check whether the <see cref="PublicResource"/> can be returned back to resource pool.
      /// </summary>
      /// <returns><c>true</c> if <see cref="PublicResource"/> can be returned to pool; <c>false</c> otherwise.</returns>
      protected abstract Boolean PublicResourceCanBeReturnedToPool();

   }

   /// <summary>
   /// This interface represents a single scope of using one instance of some resource.
   /// </summary>
   /// <typeparam name="TResource"></typeparam>
   /// <remarks>The instances of this interface are obtained by <see cref="ResourceAcquireInfo{TResource}.GetResourceUsageForToken(CancellationToken)"/> method, and <see cref="IDisposable.Dispose"/> method for this <see cref="ResourceUsageInfo{TResource}"/> should be called when usage is over.</remarks>
   public interface ResourceUsageInfo<out TResource> : IDisposable
   {
      /// <summary>
      /// Gets the resource to be used.
      /// </summary>
      /// <value>The resource to be used.</value>
      TResource Resource { get; }
   }

   // TODO move CancelableResourceUsageInfo along other stuff to UtilPack (prolly new project, UtilPack.ResourcePooling).

   /// <summary>
   /// This class provides implementation for <see cref="ResourceUsageInfo{TResource}"/> where the constructor invokes callback to set cancellation token, and <see cref="Dispose(bool)"/> method invokes callback to reset cancellation token.
   /// </summary>
   /// <typeparam name="TResource">The type of useable resource.</typeparam>
   public class CancelableResourceUsageInfo<TResource> : AbstractDisposable, ResourceUsageInfo<TResource>
   {
      // TODO consider extending UtilPack.UsingHelper, since that is essentially what this class does.

      private readonly Action<TResource> _resetCancellationToken;
      private readonly CancellationTokenRegistration _registration;

      /// <summary>
      /// Creates a new instance of <see cref="CancelableResourceUsageInfo{TResource}"/> with given parameters.
      /// </summary>
      /// <param name="resource">The useable resource.</param>
      /// <param name="token">The <see cref="CancellationToken"/>.</param>
      /// <param name="registration">The <see cref="CancellationTokenRegistration"/> associated with this <see cref="CancelableResourceUsageInfo{TResource}"/>.</param>
      /// <param name="setCancellationToken">The callback to set cancellation token to some external resource. Will be invoked in this constructor.</param>
      /// <param name="resetCancellationToken">The callback to reset cancellation token to some external resource. Will be invoked in <see cref="Dispose(bool)"/> method.</param>
      public CancelableResourceUsageInfo(
         TResource resource,
         CancellationToken token,
         CancellationTokenRegistration registration,
         Action<TResource, CancellationToken> setCancellationToken,
         Action<TResource> resetCancellationToken

         )
      {
         this.Resource = resource;
         setCancellationToken?.Invoke( resource, token );
         this._resetCancellationToken = resetCancellationToken;
         this._registration = registration;
      }

      /// <summary>
      /// Gets the resource associated with this <see cref="CancelableResourceUsageInfo{TResource}"/>.
      /// </summary>
      /// <value>The resource associated with this <see cref="CancelableResourceUsageInfo{TResource}"/>.</value>
      public TResource Resource { get; }

      /// <summary>
      /// This method will call the cancellation token reset callback given to constructor, and then dispose the <see cref="CancellationTokenRegistration"/> given to constructor.
      /// </summary>
      /// <param name="disposing">Whether this was called from <see cref="IDisposable.Dispose"/> method.</param>
      protected override void Dispose( Boolean disposing )
      {
         if ( disposing )
         {
            try
            {
               this._resetCancellationToken?.Invoke( this.Resource );
            }
            finally
            {
               this._registration.DisposeSafely();
            }
         }
      }
   }

   /// <summary>
   /// This class implements the <see cref="AsyncResourcePoolObservable{TResource}"/> interface in such way that the resource is disposed of after each use in <see cref="UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TResourceInstance">The type of instance holding the resource.</typeparam>
   /// <typeparam name="TResourceCreationParams">The type of parameters used to create a new instance of <typeparamref name="TResourceInstance"/>.</typeparam>
   /// <remarks>
   /// While this class is useful in simple scenarios, e.g. testing, the actual production environments most likely will want to use <see cref="CachingAsyncResourcePoolWithTimeout{TResource, TResourceCreationParams}"/> which is inherited from this class.
   /// </remarks>
   /// <seealso cref="CachingAsyncResourcePoolWithTimeout{TResource, TResourceCreationParams}"/>
   /// <seealso cref="CachingAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/>
   public class OneTimeUseAsyncResourcePool<TResource, TResourceInstance, TResourceCreationParams> : AsyncResourcePoolObservable<TResource>
   {

      /// <summary>
      /// Creates a new instance of <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/>
      /// </summary>
      /// <param name="factory">The <see cref="ResourceFactory{TResource, TParams}"/> to use for creation of new resources.</param>
      /// <param name="factoryParameters">The parameters to passe to <see cref="ResourceFactory{TResource, TParams}"/> when creating new instances of resources.</param>
      /// <param name="resourceExtractor">The callback to extract <see cref="ResourceAcquireInfo{TResource}"/> from instances of <typeparamref name="TResourceInstance"/>.</param>
      /// <param name="instanceCreator">The callback to create a new instance of <typeparamref name="TResourceInstance"/> from existing <see cref="ResourceAcquireInfo{TResource}"/>.</param>
      /// <exception cref="ArgumentNullException">If any of <paramref name="factory"/>, <paramref name="resourceExtractor"/>, or <paramref name="instanceCreator"/> is <c>null</c>.</exception>
      public OneTimeUseAsyncResourcePool(
         ResourceFactory<TResource, TResourceCreationParams> factory,
         TResourceCreationParams factoryParameters,
         Func<TResourceInstance, ResourceAcquireInfo<TResource>> resourceExtractor,
         Func<ResourceAcquireInfo<TResource>, TResourceInstance> instanceCreator
      )
      {
         this.FactoryParameters = factoryParameters;
         this.Factory = ArgumentValidator.ValidateNotNull( nameof( factory ), factory );
         this.ResourceExtractor = ArgumentValidator.ValidateNotNull( nameof( resourceExtractor ), resourceExtractor );
         this.InstanceCreator = ArgumentValidator.ValidateNotNull( nameof( instanceCreator ), instanceCreator );
      }

      /// <summary>
      /// Gets the <see cref="ResourceFactory{TResource, TParams}"/> used to create new instances of resources.
      /// </summary>
      /// <value>The <see cref="ResourceFactory{TResource, TParams}"/> used to create new instances of resources.</value>
      protected ResourceFactory<TResource, TResourceCreationParams> Factory { get; }

      /// <summary>
      /// Gets the parameters passed to <see cref="ResourceFactory{TResource, TParams}.AcquireResourceAsync(TParams, CancellationToken)"/> to create new instances of resources.
      /// </summary>
      /// <value>The parameters passed to <see cref="ResourceFactory{TResource, TParams}.AcquireResourceAsync(TParams, CancellationToken)"/> to create new instances of resources.</value>
      protected TResourceCreationParams FactoryParameters { get; }

      /// <summary>
      /// Gets the callback to extract <see cref="ResourceAcquireInfo{TResource}"/> from instances of <typeparamref name="TResourceInstance"/>.
      /// </summary>
      /// <value>The callback to extract <see cref="ResourceAcquireInfo{TResource}"/> from instances of <typeparamref name="TResourceInstance"/>.</value>
      protected Func<TResourceInstance, ResourceAcquireInfo<TResource>> ResourceExtractor { get; }

      /// <summary>
      /// Gets the callback to create a new instance of <typeparamref name="TResourceInstance"/> from existing <see cref="ResourceAcquireInfo{TResource}"/>.
      /// </summary>
      /// <value>The callback to create a new instance of <typeparamref name="TResourceInstance"/> from existing <see cref="ResourceAcquireInfo{TResource}"/>.</value>
      protected Func<ResourceAcquireInfo<TResource>, TResourceInstance> InstanceCreator { get; }

      /// <summary>
      /// Implements <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method by validating that the given callback is not <c>null</c> and delegating implementation to private method.
      /// </summary>
      /// <param name="user">The callback to asynchronously use the resource.</param>
      /// <param name="token">The optional <see cref="CancellationToken"/>.</param>
      /// <returns>Task which completes after the <paramref name="user"/> callback completes and this resource pool has cleaned up any of its own internal resources.</returns>
      /// <remarks>
      /// Will return completed task if <paramref name="user"/> is <c>null</c> or cancellation is pequested for given <paramref name="token"/>.
      /// </remarks>
      public Task UseResourceAsync( Func<TResource, Task> user, CancellationToken token = default( CancellationToken ) )
      {
         // Avoid potentially allocating new instance of Task on every call to this method, and check for user and token cancellation first.
         return user == null ? TaskUtils.CompletedTask : ( token.IsCancellationRequested ?
            TaskUtils.FromCanceled( token ) :
            this.DoUseResourceAsync( user, token )
            );

      }

      private async Task DoUseResourceAsync( Func<TResource, Task> executer, CancellationToken token )
      {
         var instance = await this.AcquireResourceAsync( token );

         try
         {
            using ( var usageInfo = this.ResourceExtractor( instance ).GetResourceUsageForToken( token ) )
            {
               await executer( usageInfo.Resource );
            }
         }
         finally
         {
            await ( this.DisposeResourceAsync( instance, token ) ?? TaskUtils.CompletedTask );
         }
      }

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
      /// The implementation in this class always uses <see cref="ResourceFactory{TResource, TParams}.AcquireResourceAsync(TParams, CancellationToken)"/> method of this <see cref="Factory"/>, but derived classes may override this method to cache previously used resources into a pool.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use.</param>
      /// <returns>A task which will have instance of <typeparamref name="TResourceInstance"/> upon completion.</returns>
      protected virtual async ValueTask<TResourceInstance> AcquireResourceAsync( CancellationToken token )
      {
         var connAcquireInfo = await this.Factory.AcquireResourceAsync( this.FactoryParameters, token );
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
            await this.ResourceExtractor( resource ).DisposeAsyncSafely( token );
         }
      }


   }

   /// <summary>
   /// This enumeration controls how <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}.PerformDisposeResourceAsync(TResourceInstance, CancellationToken, ResourceDisposeKind)"/> method will behave.
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
   /// This class extends <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/> and implements rudimentary pooling for resource instances.
   /// The pool is never cleared by this class though, since this class does not define any logic for how to perform clean-up operation for the pool.
   /// The <see cref="CachingAsyncResourcePoolWithTimeout{TResource, TResourceCreationParams}"/> extends this class and defines exactly that.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TCachedResource">The type of instance holding the resource. This will be the type for <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/> object used to pool resource instances.</typeparam>
   /// <typeparam name="TResourceCreationParams">The type of parameters used to create a new instance of <typeparamref name="TCachedResource"/>.</typeparam>
   /// <remarks>
   /// This class is not very useful by itself - instead it provides some common implementation for pooling resource instances.
   /// End-users will find <see cref="CachingAsyncResourcePoolWithTimeout{TResource, TResourceCreationParams}"/> much more useful.
   /// </remarks>
   /// <seealso cref="CachingAsyncResourcePoolWithTimeout{TResource, TResourceCreationParams}"/>
   public class CachingAsyncResourcePool<TResource, TCachedResource, TResourceCreationParams> : OneTimeUseAsyncResourcePool<TResource, TCachedResource, TResourceCreationParams>, IAsyncDisposable, IDisposable
      where TCachedResource : class, InstanceWithNextInfo<TCachedResource>
   {
      private const Int32 NOT_DISPOSED = 0;
      private const Int32 DISPOSED = 1;

      private Int32 _disposed;

      /// <summary>
      /// Creates a new instance of <see cref="CachingAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/> with given parameters.
      /// </summary>
      /// <param name="factory">The <see cref="ResourceFactory{TResource, TParams}"/> to use for creation of new resources.</param>
      /// <param name="factoryParameters">The parameters to passe to <see cref="ResourceFactory{TResource, TParams}"/> when creating new instances of resources.</param>
      /// <param name="resourceExtractor">The callback to extract <see cref="ResourceAcquireInfo{TResource}"/> from instances of <typeparamref name="TCachedResource"/>.</param>
      /// <param name="instanceCreator">The callback to create a new instance of <typeparamref name="TCachedResource"/> from existing <see cref="ResourceAcquireInfo{TResource}"/>.</param>
      /// <exception cref="ArgumentNullException">If any of <paramref name="factory"/>, <paramref name="resourceExtractor"/>, or <paramref name="instanceCreator"/> is <c>null</c>.</exception>
      public CachingAsyncResourcePool(
         ResourceFactory<TResource, TResourceCreationParams> factory,
         TResourceCreationParams factoryParameters,
         Func<TCachedResource, ResourceAcquireInfo<TResource>> resourceExtractor,
         Func<ResourceAcquireInfo<TResource>, TCachedResource> instanceCreator
         ) : base( factory, factoryParameters, resourceExtractor, instanceCreator )
      {
         this.Pool = new LocklessInstancePoolForClassesNoHeapAllocations<TCachedResource>();
      }

      /// <summary>
      /// Gets the actual instance-caching pool used by this <see cref="CachingAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/>.
      /// </summary>
      /// <value>The actual instance-caching pool used by this <see cref="CachingAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/>.</value>
      /// <seealso cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/>
      protected LocklessInstancePoolForClassesNoHeapAllocations<TCachedResource> Pool { get; }

      /// <summary>
      /// Implements <see cref="IAsyncDisposable.DisposeAsync(CancellationToken)"/> method to empty the <see cref="Pool"/> from all resources and dispose them asynchronously.
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
      /// This property checks whether this <see cref="CachingAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/> is disposed, or being in process of disposing.
      /// </summary>
      /// <value><c>true</c> if this <see cref="CachingAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/> is disposed, or being in process of disposing; <c>false</c> otherwise.</value>
      public Boolean Disposed => this._disposed == NOT_DISPOSED;

      /// <summary>
      /// This method overrides <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}.AcquireResourceAsync(CancellationToken)"/> to implement logic where instead of always using <see cref="ResourceFactory{TResource, TParams}"/>, the existing resource instance may be acquired from this <see cref="Pool"/>.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use when acquiring new resource from <see cref="ResourceFactory{TResource, TParams}"/>.</param>
      /// <returns>A task which completes when the resource has been acquired.</returns>
      protected override async ValueTask<TCachedResource> AcquireResourceAsync( CancellationToken token )
      {
         if ( this.Disposed )
         {
            throw new ObjectDisposedException( "This pool is already disposed or being disposed of." );
         }

         var retVal = this.Pool.TakeInstance();
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
      /// This method overrides <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}.DisposeResourceAsync(TResourceInstance, CancellationToken)"/> in order to return the resource to this <see cref="Pool"/>, if it is returnable back to the pool as specified by <see cref="ResourceAcquireInfo{TResource}.IsResourceReturnableToPool"/>.
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
            this.Pool.ReturnInstance( instance );
            // We might've disposed or started to dispose asynchronously after returning to pool in such way that racing condition may occur.
            if ( this.Disposed )
            {
               this.DisposeAllInPool();
            }
         }
         else
         {
            await info.DisposeAsyncSafely( token );
         }
      }
   }

   /// <summary>
   /// This class extends <see cref="CachingAsyncResourcePool{TResource, TCachedResource, TResourceCreationParams}"/> to add information when the resource was returned to the pool, in order to be able to clean it up later using <see cref="CleanUpAsync(TimeSpan, CancellationToken)"/> method.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed to public.</typeparam>
   /// <typeparam name="TResourceCreationParams">The type of parameters used to create a new instance of <see cref="InstanceHolderWithTimestamp{TResource}"/>.</typeparam>
   public class CachingAsyncResourcePoolWithTimeout<TResource, TResourceCreationParams> : CachingAsyncResourcePool<TResource, InstanceHolderWithTimestamp<ResourceAcquireInfo<TResource>>, TResourceCreationParams>, AsyncResourcePoolObservable<TResource, TimeSpan>
   {
      /// <summary>
      /// Creates a new instance of <see cref="CachingAsyncResourcePoolWithTimeout{TResource, TResourceCreationParams}"/> with given parameters.
      /// </summary>
      /// <param name="factory">The <see cref="ResourceFactory{TResource, TParams}"/> to use when needed to create new instances of resource.</param>
      /// <param name="factoryParameters">The parameters to pass when using <see cref="ResourceFactory{TResource, TParams}.AcquireResourceAsync(TParams, CancellationToken)"/> of <paramref name="factory"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="factory"/> is <c>null</c>.</exception>
      public CachingAsyncResourcePoolWithTimeout(
         ResourceFactory<TResource, TResourceCreationParams> factory,
         TResourceCreationParams factoryParameters
         )
         : base( factory, factoryParameters, instance => instance.Instance, resource => new InstanceHolderWithTimestamp<ResourceAcquireInfo<TResource>>( resource ) )
      {
      }

      /// <summary>
      /// This method overrides <see cref="CachingAsyncResourcePool{TResource, TCachedResource, TResourceCreationParams}.DisposeResourceAsync(TCachedResource, CancellationToken)"/> to register return time of the resource.
      /// </summary>
      /// <param name="instance">The resource instance to dispose or to return to the <see cref="CachingAsyncResourcePool{TResource, TCachedResource, TResourceCreationParams}.Pool"/>.</param>
      /// <param name="token">The <see cref="CancellationToken"/> to use when disposing asynchronously.</param>
      /// <returns>A task which completes once all events have been invoked and resource either disposed of or returned to this <see cref="CachingAsyncResourcePool{TResource, TCachedResource, TResourceCreationParams}.Pool"/>.</returns>
      protected override async Task DisposeResourceAsync( InstanceHolderWithTimestamp<ResourceAcquireInfo<TResource>> instance, CancellationToken token )
      {
         instance.JustBeforePuttingBackToPool();
         await base.DisposeResourceAsync( instance, token );
      }

      /// <summary>
      /// This method implements <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync(TCleanUpParameter, CancellationToken)"/> with <see cref="TimeSpan"/> as clean-up parameter.
      /// The <paramref name="maxResourceIdleTime"/> will serve as limit: this method will dispose all resource instances which have been idle in the <see cref="CachingAsyncResourcePool{TResource, TCachedResource, TResourceCreationParams}.Pool"/> for longer than the limit.
      /// During this method execution, this <see cref="CachingAsyncResourcePoolWithTimeout{TResource, TResourceCreationParams}"/> will continue to be usable as normally.
      /// </summary>
      /// <param name="maxResourceIdleTime">The maximum idle time for resource in the <see cref="CachingAsyncResourcePool{TResource, TCachedResource, TResourceCreationParams}.Pool"/> for it to be returned back to the pool. If idle time is longer than (operator <c>&gt;</c>) this parameter, then the resource will be disposed.</param>
      /// <param name="token">The <see cref="CancellationToken"/> to use when disposing.</param>
      /// <returns>A task which will be complete when all of the resource instances of <see cref="CachingAsyncResourcePool{TResource, TCachedResource, TResourceCreationParams}.Pool"/> have been checked and resource eligible for clean-up will be disposed of.</returns>
      public async Task CleanUpAsync( TimeSpan maxResourceIdleTime, CancellationToken token )
      {
         InstanceHolderWithTimestamp<ResourceAcquireInfo<TResource>> instance;
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
   /// This class is used by <see cref="CachingAsyncResourcePoolWithTimeout{TResource, TResourceCreationParams}"/> to capture the time when resource instance was returned to the <see cref="CachingAsyncResourcePool{TResource, TCachedResource, TResourceCreationParams}.Pool"/>.
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