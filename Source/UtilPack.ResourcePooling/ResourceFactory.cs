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
using UtilPack.ResourcePooling;

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
   /// This interface is used by pools to create a new instance of resource.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TParams">The type of parameters used to create a resource.</typeparam>
   public interface AsyncResourceFactory<out TResource, in TParams>
   {
      /// <summary>
      /// Binds the creation parameters and returns version of <see cref="AsyncResourceFactory{TResource}"/> which allows creation of resource without specifying parameter.
      /// </summary>
      /// <param name="parameters">The resource creation parameteres to bind to.</param>
      /// <returns>A <see cref="AsyncResourceFactory{TResource}"/>.</returns>
      AsyncResourceFactory<TResource> BindCreationParameters( TParams parameters );
   }

   /// <summary>
   /// This is class to <see cref="AsyncResourceFactory{TResource, TParams}"/> instances which have no state.
   /// </summary>
   /// <typeparam name="TResource">The type of resources this factory creates.</typeparam>
   public sealed class StatelessAsyncResourceFactory<TResource> : AsyncResourceFactory<TResource, Object>, AsyncResourceFactory<TResource>
   {
      private readonly Func<CancellationToken, ValueTask<AsyncResourceAcquireInfo<TResource>>> _acquireResource;

      /// <summary>
      /// Creates a new instance of <see cref="StatelessAsyncResourceFactory{TResource}"/> with given callback to acquire resource.
      /// </summary>
      /// <param name="acquireResource">A callback to potentially asynchronously acquire <see cref="AsyncResourceAcquireInfo{TResource}"/>.</param>
      public StatelessAsyncResourceFactory(
          Func<CancellationToken, ValueTask<AsyncResourceAcquireInfo<TResource>>> acquireResource
         )
      {
         this._acquireResource = ArgumentValidator.ValidateNotNull( nameof( acquireResource ), acquireResource );
      }

      /// <summary>
      /// Implements <see cref="AsyncResourceFactory{TResource, TParams}.BindCreationParameters"/> by returning this factory.
      /// </summary>
      /// <param name="parameters">The creation parameters, ignored.</param>
      /// <returns>This factory.</returns>
      public AsyncResourceFactory<TResource> BindCreationParameters( Object parameters )
      {
         return this;
      }

      /// <inheritdoc />
      public AsyncResourceAcquireContext<TResource> CreateAcquireResourceContext( CancellationToken token )
      {
         return new ValueTaskAsyncResourceAcquireContext<TResource>( this._acquireResource( token ) );
      }

      /// <summary>
      /// Implements <see cref="ResourceFactoryInformation.ResetFactoryState"/> and is no-op.
      /// </summary>
      public void ResetFactoryState()
      {
         // Nothing to do since stateless
      }
   }

   /// <summary>
   /// Implements <see cref="AsyncResourceFactory{TResource, TParams}"/> by delegating the <see cref="AsyncResourceFactory{TResource, TParams}.BindCreationParameters"/> call to given callback.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TParams">The type of parameters used to create a resource.</typeparam>
   public class DefaultAsyncResourceFactory<TResource, TParams> : AsyncResourceFactory<TResource, TParams>
   {
      private readonly Func<TParams, AsyncResourceFactory<TResource>> _creator;

      /// <summary>
      /// Creates new instance of <see cref="DefaultAsyncResourceFactory{TResource, TParams}"/> with given callback which creates <see cref="AsyncResourceFactory{TResource}"/>.
      /// </summary>
      /// <param name="creator">The callback to create <see cref="AsyncResourceFactory{TResource}"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="creator"/> is <c>null</c>.</exception>
      public DefaultAsyncResourceFactory(
         Func<TParams, AsyncResourceFactory<TResource>> creator
         )
      {
         this._creator = ArgumentValidator.ValidateNotNull( nameof( creator ), creator );
      }

      /// <summary>
      /// Implements <see cref="AsyncResourceFactory{TResource, TParams}.BindCreationParameters"/> and delegates call to callback given to constructor.
      /// </summary>
      /// <param name="parameters">The creation parameters.</param>
      /// <returns>The result of callback given to constructor.</returns>
      public AsyncResourceFactory<TResource> BindCreationParameters( TParams parameters )
      {
         return this._creator( parameters );
      }
   }

   /// <summary>
   /// This interface is bound <see cref="AsyncResourceFactory{TResource, TParams}"/>, which knows the creation parameters and can contain some state calculated based on the parameters.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   public interface AsyncResourceFactory<out TResource> : ResourceFactoryInformation
   {
      /// <summary>
      /// Potentially asynchronously acquires the resource, given the <see cref="CancellationToken"/>.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/>.</param>
      /// <returns>Potentially asynchronously acquired resource, as <see cref="AsyncResourceAcquireInfo{TResource}"/>.</returns>

      AsyncResourceAcquireContext<TResource> CreateAcquireResourceContext( CancellationToken token ); // ValueTask<AsyncResourceAcquireInfo<TResource>>
   }

   /// <summary>
   /// This interface splits asynchronous acquiring of <see cref="AsyncResourceAcquireInfo{TResource}"/> into two stages: waiting for asynchronous action to complete, and then getting the <see cref="AcquiredResourceInfo"/> property.
   /// This way, it is possible to have <c>out</c> modifier for <typeparamref name="TResource"/>, a very important property, since it also cascades to <see cref="AsyncResourceFactory{TResource}"/> and <see cref="AsyncResourceFactory{TResource, TParams}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resource being acquired.</typeparam>
   public interface AsyncResourceAcquireContext<out TResource>
   {
      /// <summary>
      /// Gets the task to wait until the <see cref="AcquiredResourceInfo"/> property becomes available.
      /// </summary>
      /// <returns>The task to wait until the <see cref="AcquiredResourceInfo"/> property becomes available.</returns>
      Task WaitTillAcquiredAsync();

      /// <summary>
      /// Gets the <see cref="AsyncResourceAcquireInfo{TResource}"/> that has been acquired after awaiting for task returned by <see cref="WaitTillAcquiredAsync"/>.
      /// </summary>
      /// <value>The <see cref="AsyncResourceAcquireInfo{TResource}"/> that has been acquired after awaiting for task returned by <see cref="WaitTillAcquiredAsync"/>.</value>
      /// <exception cref="InvalidOperationException">If the underlying asynchronous operation is not yet completed.</exception>
      AsyncResourceAcquireInfo<TResource> AcquiredResourceInfo { get; }
   }

   /// <summary>
   /// This class implements <see cref="AsyncResourceAcquireContext{TResource}"/> by wrapping a single <see cref="ValueTask{TResult}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of the resource being acquired.</typeparam>
   public sealed class ValueTaskAsyncResourceAcquireContext<TResource> : AsyncResourceAcquireContext<TResource>
   {
      private readonly ValueTask<AsyncResourceAcquireInfo<TResource>> _task;

      /// <summary>
      /// Creates a new instance of <see cref="ValueTaskAsyncResourceAcquireContext{TResource}"/> with given <see cref="ValueTask{TResult}"/>.
      /// </summary>
      /// <param name="task">The potentially asynchronously completing task returning <see cref="AsyncResourceAcquireInfo{TResource}"/>.</param>
      public ValueTaskAsyncResourceAcquireContext( ValueTask<AsyncResourceAcquireInfo<TResource>> task )
      {
         this._task = task;
      }

      /// <summary>
      /// Gets the result of current <see cref="ValueTask{TResult}"/>.
      /// </summary>
      /// <value>The result of current <see cref="ValueTask{TResult}"/>.</value>
      /// <exception cref="InvalidOperationException">If current <see cref="ValueTask{TResult}"/> is not yet completed.</exception>
      /// <seealso cref="WaitTillAcquiredAsync"/>
      public AsyncResourceAcquireInfo<TResource> AcquiredResourceInfo
      {
         get
         {
            return this._task.IsCompleted ? this._task.Result : throw new InvalidOperationException( "Task is not yet completed." );
         }
      }

      /// <summary>
      /// Asynchronously waits until wrapped <see cref="ValueTask{TResult}"/> completes.
      /// Will return completed task if wrapped <see cref="ValueTask{TResult}"/> is completed.
      /// </summary>
      /// <returns><see cref="Task"/> which will asynchronously complete once wrapped <see cref="ValueTask{TResult}"/> has completed.</returns>
      public Task WaitTillAcquiredAsync()
      {
         return this._task.IsCompleted ?
            TaskUtils.CompletedTask :
            this._task.AsTask();
      }
   }

   /// <summary>
   /// This class implements <see cref="AsyncResourceAcquireContext{TResource}"/> by wrapping a single <see cref="Task{TResult}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of the resource being acquired.</typeparam>
   public sealed class TaskAsyncResourceAcquireContext<TResource> : AsyncResourceAcquireContext<TResource>
   {
      private readonly Task<AsyncResourceAcquireInfo<TResource>> _task;

      /// <summary>
      /// Creates a new <see cref="TaskAsyncResourceAcquireContext{TResource}"/> with given <see cref="Task{TResult}"/>.
      /// </summary>
      /// <param name="task">The potentially asynchronously completing task returning <see cref="AsyncResourceAcquireInfo{TResource}"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="task"/> is <c>null</c>.</exception>
      public TaskAsyncResourceAcquireContext(
         Task<AsyncResourceAcquireInfo<TResource>> task
         )
      {
         this._task = ArgumentValidator.ValidateNotNull( nameof( task ), task );
      }

      /// <summary>
      /// Gets the result of current <see cref="Task{TResult}"/>.
      /// </summary>
      /// <value>The result of current <see cref="Task{TResult}"/>.</value>
      /// <exception cref="InvalidOperationException">If current <see cref="Task{TResult}"/> is not yet completed.</exception>
      /// <seealso cref="WaitTillAcquiredAsync"/>
      public AsyncResourceAcquireInfo<TResource> AcquiredResourceInfo
      {
         get
         {
            return this._task.IsCompleted ? this._task.Result : throw new InvalidOperationException( "Task is not yet completed." );
         }
      }

      /// <summary>
      /// Asynchronously waits until wrapped <see cref="Task{TResult}"/> completes.
      /// Will return completed task if wrapped <see cref="Task{TResult}"/> is completed.
      /// </summary>
      /// <returns><see cref="Task"/> which will asynchronously complete once wrapped <see cref="Task{TResult}"/> has completed.</returns>
      public Task WaitTillAcquiredAsync()
      {
         return this._task;
      }
   }

   /// <summary>
   /// This class provides default skeleton implementation for <see cref="AsyncResourceFactory{TResource}"/>, which stores the creation parameters used to create resources.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TParams">The type of parameters used to create a resource.</typeparam>
   public abstract class DefaultBoundAsyncResourceFactory<TResource, TParams> : AsyncResourceFactory<TResource>
   {

      /// <summary>
      /// Creates a new instance of <see cref="DefaultBoundAsyncResourceFactory{TResource, TParams}"/> with given creation parameters and callback to create a <see cref="AsyncResourceAcquireInfo{TResource}"/>.
      /// </summary>
      /// <param name="parameters">The resource creation parameters.</param>
      public DefaultBoundAsyncResourceFactory(
         TParams parameters
         )
      {
         this.CreationParameters = parameters;

      }

      /// <summary>
      /// Gets the creation parameters passed to constructor.
      /// </summary>
      /// <value>The creation parameters passed to constructor.</value>
      protected TParams CreationParameters { get; }

      /// <summary>
      /// Implements <see cref="AsyncResourceFactory{TResource}.CreateAcquireResourceContext"/> by calling <see cref="AcquireResourceAsync(CancellationToken)"/>.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/>.</param>
      /// <returns>The result of callback given to constructor.</returns>
      public AsyncResourceAcquireContext<TResource> CreateAcquireResourceContext( CancellationToken token )
      {
         return new ValueTaskAsyncResourceAcquireContext<TResource>( this.AcquireResourceAsync( token ) );
      }

      /// <summary>
      /// Derived classes should implement this to provide potentially asynchronous functionality to create <see cref="AsyncResourceAcquireInfo{TResource}"/>.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> being used.</param>
      /// <returns>Potentially asynchronously returns <see cref="AsyncResourceAcquireInfo{TResource}"/>.</returns>
      protected abstract ValueTask<AsyncResourceAcquireInfo<TResource>> AcquireResourceAsync( CancellationToken token );

      /// <summary>
      /// Derived classes should implement this to reset factory state, if any such state exists.
      /// </summary>
      public abstract void ResetFactoryState();
   }

   /// <summary>
   /// This class provides callback-based implementation for <see cref="AsyncResourceFactory{TResource}"/> which has no other state than creation parameters themselves.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <typeparam name="TParams">The type of parameters used to create a resource.</typeparam>
   public sealed class StatelessBoundAsyncResourceFactory<TResource, TParams> : DefaultBoundAsyncResourceFactory<TResource, TParams>
   {

      private readonly Func<TParams, CancellationToken, ValueTask<AsyncResourceAcquireInfo<TResource>>> _factory;

      /// <summary>
      /// Creates a new instance of <see cref="StatelessBoundAsyncResourceFactory{TResource, TParams}"/> with given creation parameters and callback to create a <see cref="AsyncResourceAcquireInfo{TResource}"/>.
      /// </summary>
      /// <param name="parameters">The resource creation parameters.</param>
      /// <param name="factory">The callback to create <see cref="AsyncResourceAcquireInfo{TResource}"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="factory"/> is <c>null</c>.</exception>
      public StatelessBoundAsyncResourceFactory(
         TParams parameters,
         Func<TParams, CancellationToken, ValueTask<AsyncResourceAcquireInfo<TResource>>> factory
         ) : base( parameters )
      {
         this._factory = ArgumentValidator.ValidateNotNull( nameof( factory ), factory );
      }

      /// <summary>
      /// Implements <see cref="DefaultBoundAsyncResourceFactory{TResource, TParams}.AcquireResourceAsync(CancellationToken)"/> by calling the callback given to constructor.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use.</param>
      /// <returns>Potentially asynchronously returns <see cref="AsyncResourceAcquireInfo{TResource}"/>.</returns>
      protected override ValueTask<AsyncResourceAcquireInfo<TResource>> AcquireResourceAsync( CancellationToken token )
      {
         return this._factory( this.CreationParameters, token );
      }

      /// <summary>
      /// Implements <see cref="ResourceFactoryInformation.ResetFactoryState"/> and is no-op.
      /// </summary>
      public override void ResetFactoryState()
      {

      }
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
   /// Use <see cref="IAsyncDisposableWithToken.DisposeAsync"/> when it is ok to wait for proper resource disposal.
   /// Use <see cref="IDisposable.Dispose"/> method only when it is necessary to immediately close underlying resources.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   public interface AsyncResourceAcquireInfo<out TResource> : AbstractResourceAcquireInfo, IAsyncDisposableWithToken
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
      /// This method implements <see cref="IAsyncDisposableWithToken.DisposeAsync"/> and will invoke <see cref="DisposeBeforeClosingChannel(CancellationToken)"/> before disposing this <see cref="Channel"/>.
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
      /// This method implements <see cref="IAsyncDisposable.DisposeAsync"/>  and will invoke this <see cref="DisposeAsync(CancellationToken)"/> with non-cancelable token.
      /// </summary>
      /// <returns>A task which will complete once asynchronous diposing routine is done and this <see cref="Channel"/> is closed.</returns>
      public Task DisposeAsync() => this.DisposeAsync( default );

      /// <summary>
      /// Returns a new <see cref="ResourceUsageInfo{TResource}"/> in order to start logical scope of using resource, typically at the start of <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.
      /// </summary>
      /// <param name="token">The cancellation token for this resource usage scenario.</param>
      /// <returns>A new instance of <see cref="ResourceUsageInfo{TResource}"/> which should have its <see cref="IDisposable.Dispose"/> method called when the usage scenario ends.</returns>
      public ResourceUsageInfo<TPublicResource> GetResourceUsageForToken( CancellationToken token )
      {
         var retVal = new CancelableResourceUsageInfo<TPublicResource>(
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

         return this.CreateResourceUsageInfoWrapper( retVal, token ) ?? retVal;
      }

      /// <summary>
      /// Derived classes may override this method in order to create a custom wrapper around the <see cref="ResourceUsageInfo{TResource}"/> about to be returned by <see cref="GetResourceUsageForToken"/>.
      /// Return <c>null</c> to signal that no wrapper should be created - this is what this method always returns.
      /// </summary>
      /// <param name="resourceUsageInfo">The <see cref="ResourceUsageInfo{TResource}"/> about to be returned by <see cref="GetResourceUsageForToken"/>.</param>
      /// <param name="token">The <see cref="CancellationToken"/> given to <see cref="GetResourceUsageForToken"/>.</param>
      /// <returns>A wrapper around <paramref name="resourceUsageInfo"/>, or <c>null</c>.</returns>
      protected virtual ResourceUsageInfo<TPublicResource> CreateResourceUsageInfoWrapper( ResourceUsageInfo<TPublicResource> resourceUsageInfo, CancellationToken token )
      {
         return null;
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

public static partial class E_UtilPack
{
   ///// <summary>
   ///// Asynchronously creates a new instance of resource with given parameters.
   ///// </summary>
   ///// <typeparam name="TResource">The type of resource.</typeparam>
   ///// <typeparam name="TParams">The type of parameters used to create a resource.</typeparam>
   ///// <param name="factory">This <see cref="AsyncResourceFactory{TResource, TParams}"/>.</param>
   ///// <param name="parameters">Parameters that are required for resource creation.</param>
   ///// <param name="token">Cancellation token to use during resource creation.</param>
   ///// <returns>A task which returns a <see cref="AsyncResourceAcquireInfo{TResource}"/> upon completion.</returns>
   //public static ValueTask<AsyncResourceAcquireInfo<TResource>> AcquireResourceAsync<TResource, TParams>( this AsyncResourceFactory<TResource, TParams> factory, TParams parameters, CancellationToken token )
   //    => factory.BindCreationParameters( parameters ).AcquireResourceAsync( token );

   /// <summary>
   /// This is helper method to call <see cref="AsyncResourceFactory{TResource}.CreateAcquireResourceContext(CancellationToken)"/>, and use it correctly to potentially asynchronously extract <see cref="AsyncResourceAcquireInfo{TResource}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resource.</typeparam>
   /// <param name="factory">This <see cref="AsyncResourceFactory{TResource}"/>.</param>
   /// <param name="token">The <see cref="CancellationToken"/> to use.</param>
   /// <returns>Potentially asynchronously returns <see cref="AsyncResourceAcquireInfo{TResource}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourceFactory{TResource}"/> is <c>null</c>.</exception>
   public static async ValueTask<AsyncResourceAcquireInfo<TResource>> AcquireResourceAsync<TResource>( this AsyncResourceFactory<TResource> factory, CancellationToken token )
   {
      var ctx = factory.CreateAcquireResourceContext( token );
      await ctx.WaitTillAcquiredAsync();
      return ctx.AcquiredResourceInfo;
   }
}
