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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace ResourcePooling.Async.Abstractions
{
   /// <summary>
   /// This interface is typically entrypoint for scenarios using CBAM.
   /// It provides a way to use resources via <see cref="UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   public interface AsyncResourcePool<out TResource> : ResourceFactoryInformation
   {
      ResourceUsage<TResource> GetResourceUsage( CancellationToken token );
   }



   public interface ResourceUsage<out TResource> : IAsyncDisposable
   {
      Task AwaitForResource();

      TResource Resource { get; }
   }

   /// <summary>
   /// This interface exposes events related to observing a <see cref="AsyncResourcePool{TResource}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <typeparam name="TCreationArgs">The type of event arguments for <see cref="AfterResourceCreationEvent"/> event.</typeparam>
   /// <typeparam name="TAcquiringArgs">The type of event arguments for <see cref="AfterResourceAcquiringEvent"/> event.</typeparam>
   /// <typeparam name="TReturningArgs">The type of event arguments for <see cref="BeforeResourceReturningEvent"/> event.</typeparam>
   /// <typeparam name="TCloseArgs">The type of event arguments for <see cref="BeforeResourceCloseEvent"/> event.</typeparam>
   public interface ResourcePoolObservation<out TResource, out TCreationArgs, out TAcquiringArgs, out TReturningArgs, out TCloseArgs>
      where TCreationArgs : AbstractResourcePoolEventArgs<TResource>
      where TAcquiringArgs : AbstractResourcePoolEventArgs<TResource>
      where TReturningArgs : AbstractResourcePoolEventArgs<TResource>
      where TCloseArgs : AbstractResourcePoolEventArgs<TResource>
   {
      /// <summary>
      /// This event is triggered after a new instance of <typeparamref name="TResource"/> is created (i.e. when there was no previously used pooled resource available).
      /// </summary>
      event GenericEventHandler<TCreationArgs> AfterResourceCreationEvent;

      /// <summary>
      /// This event is triggered just before an instance of <typeparamref name="TResource"/> is given to callback of <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.
      /// </summary>
      event GenericEventHandler<TAcquiringArgs> AfterResourceAcquiringEvent;

      /// <summary>
      /// This event is triggered right after an instance of <typeparamref name="TResource"/> is re-acquired by resource pool from callback in <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.
      /// </summary>
      event GenericEventHandler<TReturningArgs> BeforeResourceReturningEvent;

      /// <summary>
      /// This event is triggered just before the resource is closed (and thus becomes unusuable) by <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> or <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync(TCleanUpParameter, CancellationToken)"/> methods.
      /// </summary>
      event GenericEventHandler<TCloseArgs> BeforeResourceCloseEvent;
   }

   /// <summary>
   /// This interface augments <see cref="AsyncResourcePool{TResource}"/> with observability aspect from <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArg}"/> interface.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <seealso cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArg}"/>
   public interface AsyncResourcePoolObservable<out TResource> : AsyncResourcePool<TResource>, ResourcePoolObservation<TResource, AfterAsyncResourceCreationEventArgs<TResource>, AfterAsyncResourceAcquiringEventArgs<TResource>, BeforeAsyncResourceReturningEventArgs<TResource>, BeforeAsyncResourceCloseEventArgs<TResource>>
   {

   }

   /// <summary>
   /// This interface can be used to augment <see cref="AsyncResourcePool{TResource}"/> with clean-up routine.
   /// </summary>
   /// <typeparam name="TCleanUpParameter">The type of parameter for clean-up routine.</typeparam>
   /// <remarks>
   /// Typically <typeparamref name="TCleanUpParameter"/> is <see cref="TimeSpan"/> in order to clean up all resource that have been idle for at least given time period.
   /// </remarks>
   public interface AsyncResourcePoolCleanUp<in TCleanUpParameter> : IDisposable
   {
      /// <summary>
      /// Asynchronously cleans up resources that do not fulfille the given requirements specified by <paramref name="cleanupParameter"/>.
      /// Typically this means cleaning up all resources that have been idle longer than given <see cref="TimeSpan"/>, when <typeparamref name="TCleanUpParameter"/> is <see cref="TimeSpan"/>.
      /// </summary>
      /// <param name="cleanupParameter">The clean up parameter.</param>
      /// <param name="token">The optional <see cref="CancellationToken"/> to use.</param>
      /// <returns>A <see cref="Task"/> which will be completed asynchronously.</returns>
      Task CleanUpAsync( TCleanUpParameter cleanupParameter, CancellationToken token = default( CancellationToken ) );
   }

   /// <summary>
   /// This interface augments <see cref="AsyncResourcePool{TResource}"/> with clean-up aspect from <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}"/> interface.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <typeparam name="TCleanUpParameters">The type of parameter for <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync(TCleanUpParameter, CancellationToken)"/> method.</typeparam>
   /// <seealso cref="AsyncResourcePoolCleanUp{TCleanUpParameter}"/>
   public interface AsyncResourcePool<out TResource, in TCleanUpParameters>
      : AsyncResourcePool<TResource>,
        AsyncResourcePoolCleanUp<TCleanUpParameters>
   {

   }

   /// <summary>
   /// This interface further augments <see cref="AsyncResourcePool{TResource, TCleanUpParameters}"/> with observability aspect from <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArg}"/> interface.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <typeparam name="TCleanUpParameter">The type of parameter for <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync(TCleanUpParameter, CancellationToken)"/> method.</typeparam>
   public interface AsyncResourcePoolObservable<out TResource, in TCleanUpParameter> : AsyncResourcePool<TResource, TCleanUpParameter>, AsyncResourcePoolObservable<TResource>
   {

   }

   /// <summary>
   /// This is common interface for event arguments for events in <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArg}"/> interface.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   public interface AbstractResourcePoolEventArgs<out TResource>
   {
      /// <summary>
      /// Gets the resource related to this event argument interface.
      /// </summary>
      /// <value>The resource related to this event argument interface.</value>
      TResource Resource { get; }
   }

   /// <summary>
   /// This interface is common interface for event arguments for events in <see cref="AsyncResourcePoolObservable{TResource}"/> and <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   /// <remarks>
   /// This interface extends <see cref="EventArgsWithAsyncContext"/>, so that event handlers could add asynchronous callbacks to be waited by the invoker of the event.
   /// </remarks>
   public interface AbstractAsyncResourcePoolEventArgs<out TResource> : AbstractResourcePoolEventArgs<TResource>, EventArgsWithAsyncContext
   {
   }

   /// <summary>
   /// This is event argument interface for asynchronous version of <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArg}.AfterResourceCreationEvent"/> event.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   /// <remarks>
   /// This interface extends <see cref="EventArgsWithAsyncContext"/>, so that event handlers could add asynchronous callbacks to be waited by the invoker of the event.
   /// </remarks>
   public interface AfterAsyncResourceCreationEventArgs<out TResource> : AbstractAsyncResourcePoolEventArgs<TResource>
   {
   }

   /// <summary>
   /// This is event argument interface for asynchronous version of <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArg}.AfterResourceAcquiringEvent"/> event.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   /// <remarks>
   /// This interface extends <see cref="EventArgsWithAsyncContext"/>, so that event handlers could add asynchronous callbacks to be waited by the invoker of the event.
   /// </remarks>
   public interface AfterAsyncResourceAcquiringEventArgs<out TResource> : AbstractAsyncResourcePoolEventArgs<TResource>
   {
   }

   /// <summary>
   /// This is event argument interface for asynchronous version of <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArg}.BeforeResourceReturningEvent"/> event.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   /// <remarks>
   /// This interface extends <see cref="EventArgsWithAsyncContext"/>, so that event handlers could add asynchronous callbacks to be waited by the invoker of the event.
   /// </remarks>
   public interface BeforeAsyncResourceReturningEventArgs<out TResource> : AbstractAsyncResourcePoolEventArgs<TResource>
   {
   }

   /// <summary>
   /// This is event argument interface for asynchronous version of <see cref="ResourcePoolObservation{TResource, TCreationArgs, TAcquiringArgs, TReturningArgs, TCloseArg}.BeforeResourceCloseEvent"/> event.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   /// <remarks>
   /// This interface extends <see cref="EventArgsWithAsyncContext"/>, so that event handlers could add asynchronous callbacks to be waited by the invoker of the event.
   /// </remarks>
   public interface BeforeAsyncResourceCloseEventArgs<out TResource> : AbstractAsyncResourcePoolEventArgs<TResource>
   {
   }

   /// <summary>
   /// This class provides default implementation for <see cref="AbstractAsyncResourcePoolEventArgs{TResource}"/> interface.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   public class DefaultAbstractAsyncResourcePoolEventArgs<TResource> : EventArgsWithAsyncContextImpl, AbstractAsyncResourcePoolEventArgs<TResource>
   {
      /// <summary>
      /// Creates a new instance of <see cref="DefaultAbstractAsyncResourcePoolEventArgs{TResource}"/> with given resource.
      /// </summary>
      /// <param name="resource">The resource related to the event argument.</param>
      public DefaultAbstractAsyncResourcePoolEventArgs(
         TResource resource
         )
      {
         this.Resource = resource;
      }

      /// <summary>
      /// Gets the resource related to the event argument.
      /// </summary>
      /// <value>The resource related to the event argument.</value>
      public TResource Resource { get; }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="AfterAsyncResourceCreationEventArgs{TResource}"/> interface.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   public class DefaultAfterAsyncResourceCreationEventArgs<TResource> : DefaultAbstractAsyncResourcePoolEventArgs<TResource>, AfterAsyncResourceCreationEventArgs<TResource>
   {
      /// <summary>
      /// Creates new instance of <see cref="DefaultAfterAsyncResourceCreationEventArgs{TResource}"/> with given resource.
      /// </summary>
      /// <param name="resource">The resource related to the event argument.</param>
      public DefaultAfterAsyncResourceCreationEventArgs(
         TResource resource
         )
         : base( resource )
      {
      }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="AfterAsyncResourceAcquiringEventArgs{TResource}"/> interface.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   public class DefaultAfterAsyncResourceAcquiringEventArgs<TResource> : DefaultAbstractAsyncResourcePoolEventArgs<TResource>, AfterAsyncResourceAcquiringEventArgs<TResource>
   {
      /// <summary>
      /// Creates new instance of <see cref="DefaultAfterAsyncResourceAcquiringEventArgs{TResource}"/> with given resource.
      /// </summary>
      /// <param name="resource">The resource related to the event argument.</param>
      public DefaultAfterAsyncResourceAcquiringEventArgs(
         TResource resource
         )
         : base( resource )
      {
      }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="BeforeAsyncResourceReturningEventArgs{TResource}"/> interface.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   public class DefaultBeforeAsyncResourceReturningEventArgs<TResource> : DefaultAbstractAsyncResourcePoolEventArgs<TResource>, BeforeAsyncResourceReturningEventArgs<TResource>
   {
      /// <summary>
      /// Creates new instance of <see cref="DefaultBeforeAsyncResourceReturningEventArgs{TResource}"/> with given resource.
      /// </summary>
      /// <param name="resource">The resource related to the event argument.</param>
      public DefaultBeforeAsyncResourceReturningEventArgs(
         TResource resource
         )
         : base( resource )
      {
      }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="BeforeAsyncResourceCloseEventArgs{TResource}"/> interface.
   /// </summary>
   /// <typeparam name="TResource">The type of resource exposed by this event argument interface.</typeparam>
   public class DefaultBeforeAsyncResourceCloseEventArgs<TResource> : DefaultAbstractAsyncResourcePoolEventArgs<TResource>, BeforeAsyncResourceCloseEventArgs<TResource>
   {
      /// <summary>
      /// Creates new instance of <see cref="DefaultBeforeAsyncResourceCloseEventArgs{TResource}"/> with given resource.
      /// </summary>
      /// <param name="resource">The resource related to the event argument.</param>
      public DefaultBeforeAsyncResourceCloseEventArgs(
         TResource resource
         )
         : base( resource )
      {
      }
   }

   /// <summary>
   /// This interface represents a <see cref="AsyncResourcePool{TResource}"/> bound to specific <see cref="CancellationToken"/>.
   /// It is useful when one wants to bind the cancellation token, and let custom callbacks only customize the callback for <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by the originating pool.</typeparam>
   public interface AsyncResourcePoolUser<out TResource>
   {
      /// <summary>
      /// Gets the <see cref="CancellationToken"/> that will be passed to <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.
      /// </summary>
      /// <value>The <see cref="CancellationToken"/> that will be passed to <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> method.</value>
      CancellationToken Token { get; }

      /// <summary>
      /// Invokes the <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> with given asynchronous callback, and the value of <see cref="Token"/> property as <see cref="CancellationToken"/>.
      /// </summary>
      /// <param name="user">The asynchronous callback to use the resource.</param>
      /// <returns>A task which completes when <paramref name="user"/> callback completes and resource is returned back to the pool.</returns>
      Task UseResourceAsync( Func<TResource, Task> user );
   }

   /// <summary>
   /// This class provides default implementation for <see cref="AsyncResourcePoolUser{TResource}"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by the originating pool.</typeparam>
   public class DefaultAsyncResourcePoolUser<TResource> : AsyncResourcePoolUser<TResource>
   {
      private readonly AsyncResourcePool<TResource> _pool;

      /// <summary>
      /// Creates a new instance of <see cref="DefaultAsyncResourcePoolUser{TResource}"/> with given parameters.
      /// </summary>
      /// <param name="pool">The resource pool.</param>
      /// <param name="token">The cancellation token.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="pool"/> is <c>null</c>.</exception>
      public DefaultAsyncResourcePoolUser( AsyncResourcePool<TResource> pool, CancellationToken token )
      {
         this._pool = ArgumentValidator.ValidateNotNull( nameof( pool ), pool );
         this.Token = token;
      }

      /// <inheritdoc />
      public CancellationToken Token { get; }

      /// <inheritdoc />
      public Task UseResourceAsync( Func<TResource, Task> user )
      {
         return this._pool.UseResourceAsync( user, this.Token );
      }
   }
}

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_ResourcePooling
{
   /// <summary>
   /// Takes an existing resource or creates a new one, runs the given asynchronous callback for it, and returns it back into the pool.
   /// </summary>
   /// <param name="user">The asynchronous callback to use the resource.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use during asynchronous operations inside <paramref name="user"/> callback.</param>
   /// <returns>A task which completes when <paramref name="user"/> callback completes and resource is returned back to the pool.</returns>
   public static async Task UseResourceAsync<TResource>( this AsyncResourcePool<TResource> pool, Func<TResource, Task> user, CancellationToken token )
   {
      var usage = pool.GetResourceUsage( token );
      try
      {
         await usage.AwaitForResource();
         await user( usage.Resource );
      }
      finally
      {
         await usage.DisposeAsync();
      }
   }


   /// <summary>
   /// Helper method to invoke <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> with callback which asynchronously returns value of type <typeparamref name="T"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <typeparam name="T">The type of return value of asynchronous callback.</typeparam>
   /// <param name="pool">This <see cref="AsyncResourcePool{TResource}"/>.</param>
   /// <param name="user">The callback which asynchronously returns some value of type <typeparamref name="T"/>.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use during asynchronous operations inside <paramref name="user"/> callback.</param>
   /// <returns>A task which returns the result of <paramref name="user"/> on its completion.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourcePool{TResource}"/> is <c>null</c>.</exception>
   public static async Task<T> UseResourceAsync<TResource, T>( this AsyncResourcePool<TResource> pool, Func<TResource, Task<T>> user, CancellationToken token )
   {
      var retVal = default( T );
      await pool.UseResourceAsync( async resource => retVal = await user( resource ), token );
      return retVal;
   }

   /// <summary>
   /// Helper method to invoke <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> with callback which synchronously returns value of type <typeparamref name="T"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <typeparam name="T">The type of return value of synchronous callback.</typeparam>
   /// <param name="pool">This <see cref="AsyncResourcePool{TResource}"/>.</param>
   /// <param name="user">The callback which synchronously returns some value of type <typeparamref name="T"/>.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use during resource acquirement.</param>
   /// <returns>A task which returns the result of <paramref name="user"/> on its completion.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourcePool{TResource}"/> is <c>null</c>.</exception>
   public static async Task<T> UseResourceAsync<TResource, T>( this AsyncResourcePool<TResource> pool, Func<TResource, T> user, CancellationToken token )
   {
      var retVal = default( T );
      await pool.UseResourceAsync( resource =>
      {
         retVal = user( resource );
         return TaskUtils.CompletedTask;
      }, token );

      return retVal;
   }

   /// <summary>
   /// Helper method to invoke <see cref="AsyncResourcePool{TResource}.UseResourceAsync(Func{TResource, Task}, CancellationToken)"/> with callback which synchronously uses the resource.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <param name="pool">This <see cref="AsyncResourcePool{TResource}"/>.</param>
   /// <param name="user">The callback which synchronously uses resource.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use during resource acquirement.</param>
   /// <returns>A task which completes after resource has been returned to the pool.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourcePool{TResource}"/> is <c>null</c>.</exception>
   public static async Task UseResourceAsync<TResource>( this AsyncResourcePool<TResource> pool, Action<TResource> user, CancellationToken token )
   {
      await pool.UseResourceAsync( resource =>
      {
         user( resource );
         return TaskUtils.CompletedTask;
      }, token );
   }

   /// <summary>
   /// Helper method to invoke <see cref="AsyncResourcePoolUser{TResource}.UseResourceAsync(Func{TResource, Task})"/> with callback which asynchronously returns value of type <typeparamref name="T"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <typeparam name="T">The type of return value of asynchronous callback.</typeparam>
   /// <param name="pool">This <see cref="AsyncResourcePoolUser{TResource}"/>.</param>
   /// <param name="user">The callback which asynchronously returns some value of type <typeparamref name="T"/>.</param>
   /// <returns>A task which returns the result of <paramref name="user"/> on its completion.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourcePoolUser{TResource}"/> is <c>null</c>.</exception>
   public static async Task<T> UseResourceAsync<TResource, T>( this AsyncResourcePoolUser<TResource> pool, Func<TResource, Task<T>> user )
   {
      var retVal = default( T );
      await pool.UseResourceAsync( async resource => retVal = await user( resource ) );
      return retVal;
   }

   /// <summary>
   /// Helper method to invoke <see cref="AsyncResourcePoolUser{TResource}.UseResourceAsync(Func{TResource, Task})"/> with callback which synchronously returns value of type <typeparamref name="T"/>.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <typeparam name="T">The type of return value of synchronous callback.</typeparam>
   /// <param name="pool">This <see cref="AsyncResourcePoolUser{TResource}"/>.</param>
   /// <param name="user">The callback which synchronously returns some value of type <typeparamref name="T"/>.</param>
   /// <returns>A task which returns the result of <paramref name="user"/> on its completion.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourcePoolUser{TResource}"/> is <c>null</c>.</exception>
   public static async Task<T> UseResourceAsync<TResource, T>( this AsyncResourcePoolUser<TResource> pool, Func<TResource, T> user )
   {
      var retVal = default( T );
      await pool.UseResourceAsync( resource =>
      {
         retVal = user( resource );
         return TaskUtils.CompletedTask;
      } );

      return retVal;
   }

   /// <summary>
   /// Helper method to invoke <see cref="AsyncResourcePoolUser{TResource}.UseResourceAsync(Func{TResource, Task})"/> with callback which synchronously uses the resource.
   /// </summary>
   /// <typeparam name="TResource">The type of resources handled by this pool.</typeparam>
   /// <param name="pool">This <see cref="AsyncResourcePoolUser{TResource}"/>.</param>
   /// <param name="user">The callback which synchronously uses resource.</param>
   /// <returns>A task which completes after resource has been returned to the pool.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="AsyncResourcePoolUser{TResource}"/> is <c>null</c>.</exception>
   public static async Task UseResourceAsync<TResource>( this AsyncResourcePoolUser<TResource> pool, Action<TResource> user )
   {
      await pool.UseResourceAsync( resource =>
      {
         user( resource );
         return TaskUtils.CompletedTask;
      } );
   }
}