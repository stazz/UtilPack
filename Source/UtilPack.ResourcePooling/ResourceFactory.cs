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
   /// This interface allows the <see cref="AsyncResourceFactory{TResource, TParams}"/> itself and <see cref="AsyncResourcePool{TResource}"/> to clean up the state factory possibly has.
   /// </summary>
   public interface ResourceFactoryInformation
   {
      /// <summary>
      /// This method should reset any state and caches the resource factory currently has.
      /// </summary>
      void ResetFactoryState();
   }

   /// <summary>
   /// This interface is used by <see cref="OneTimeUseAsyncResourcePool{TResource, TResourceInstance, TResourceCreationParams}"/> and derivatives to create a new instance of resource.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TParams">The type of parameters used to create a resource.</typeparam>
   public interface AsyncResourceFactory<TResource, in TParams> : ResourceFactoryInformation
   {
      /// <summary>
      /// Asynchronously creates a new instance of resource with given parameters.
      /// </summary>
      /// <param name="parameters">Parameters that are required for resource creation.</param>
      /// <param name="token">Cancellation token to use during resource creation.</param>
      /// <returns>A task which returns a <see cref="AsyncResourceAcquireInfo{TResource}"/> upon completion.</returns>
      ValueTask<AsyncResourceAcquireInfo<TResource>> AcquireResourceAsync( TParams parameters, CancellationToken token );
   }

   /// <summary>
   /// This interface acts as common interface for <see cref="AsyncResourceAcquireInfo{TResource}"/> and ResourceAcquireInfo.
   /// </summary>
   public interface AbstractResourceAcquireInfo : IDisposable
   {
      // Return false if e.g. cancellation caused resource disposing.
      /// <summary>
      /// Gets the value indicating whether it is ok to return the resource of this <see cref="AsyncResourceAcquireInfo{TResource}"/> to the pool.
      /// </summary>
      /// <value>The value indicating whether it is ok to return the resource of this <see cref="AsyncResourceAcquireInfo{TResource}"/> to the pool.</value>
      Boolean IsResourceReturnableToPool { get; }
   }

   /// <summary>
   /// This interface provides information about resource which has been created by <see cref="AsyncResourceFactory{TResource, TParams}"/>.
   /// Use <see cref="IAsyncDisposable.DisposeAsync(CancellationToken)"/> when it is ok to wait for proper resource disposal.
   /// Use <see cref="IDisposable.Dispose"/> method only when it is necessary to immediately close underlying resources.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   public interface AsyncResourceAcquireInfo<out TResource> : AbstractResourceAcquireInfo, IAsyncDisposable
   {
      /// <summary>
      /// Gets the <see cref="ResourceUsageInfo{TResource}"/> to use a resource in association with given <see cref="CancellationToken"/>.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use when reacting to cancellation.</param>
      /// <returns>The <see cref="ResourceUsageInfo{TResource}"/>.</returns>
      /// <seealso cref="AsyncResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/>
      /// <seealso cref="CancelableResourceUsageInfo{TResource}"/>
      ResourceUsageInfo<TResource> GetResourceUsageForToken( CancellationToken token );


   }

   /// <summary>
   /// This class provides default implementation for <see cref="AsyncResourceAcquireInfo{TResource}"/> using <see cref="CancelableResourceUsageInfo{TResource}"/>.
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
   public abstract class AsyncResourceAcquireInfoImpl<TPublicResource, TPrivateResource> : AbstractDisposable, AsyncResourceAcquireInfo<TPublicResource>
   {
      private const Int32 NOT_CANCELED = 0;
      private const Int32 CANCELED = 1;

      private Int32 _cancellationState;

      private readonly Action<TPublicResource, CancellationToken> _setCancellationToken;
      private readonly Action<TPublicResource> _resetCancellationToken;

      /// <summary>
      /// Creates a new instance of <see cref="AsyncResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/> with given resource and disposable object.
      /// </summary>
      /// <param name="publicResource">The public resource.</param>
      /// <param name="privateResource">The private resource or other disposable object.</param>
      /// <param name="setCancellationToken">The callback to set cancellation token to some external resource. Will be invoked in this constructor.</param>
      /// <param name="resetCancellationToken">The callback to reset cancellation token to some external resource. Will be invoked in <see cref="AbstractDisposable.Dispose(bool)"/> method.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="publicResource"/> or <paramref name="privateResource"/> is <c>null</c>.</exception>
      public AsyncResourceAcquireInfoImpl(
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
      /// Gets the value indicating whether this <see cref="AsyncResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/> can be returned back to resource pool.
      /// </summary>
      /// <value>The value indicating whether this <see cref="AsyncResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/> can be returned back to resource pool.</value>
      /// <remarks>
      /// On cancellation via <see cref="CancellationToken"/>, this <see cref="AsyncResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/> will dispose the object used to communicate with remote resource.
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
      /// Gets the public resource of this <see cref="AsyncResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/>.
      /// </summary>
      /// <value>The public resource of this <see cref="AsyncResourceAcquireInfoImpl{TPublicResource, TPrivateResource}"/>.</value>
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
   /// <remarks>The instances of this interface are obtained by <see cref="AsyncResourceAcquireInfo{TResource}.GetResourceUsageForToken(CancellationToken)"/> method, and <see cref="IDisposable.Dispose"/> method for this <see cref="ResourceUsageInfo{TResource}"/> should be called when usage is over.</remarks>
   public interface ResourceUsageInfo<out TResource> : IDisposable
   {
      /// <summary>
      /// Gets the resource to be used.
      /// </summary>
      /// <value>The resource to be used.</value>
      TResource Resource { get; }
   }

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
}
