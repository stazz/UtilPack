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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace UtilPack.ResourcePooling
{
   /// <summary>
   /// Typically, most client code won't need to use this interface - it is provided for the sake of generic scenarios, where resource pool needs to be instantiated dynamically based on some kind of configuration.
   /// Most common scenario to create resource pools is to directly use vendor-specific class.
   /// </summary>
   public interface AsyncResourceFactoryProvider//<out TResource>
   {
      ///// <summary>
      ///// Creates a new instance of <see cref="AsyncResourcePoolObservable{TResource}"/>, which will close all resources as they are returned to pool.
      ///// This is typically useful in test scenarios.
      ///// </summary>
      ///// <param name="creationParameters">The creation parameters for the resource pool.</param>
      ///// <returns>A new instance of <see cref="AsyncResourcePoolObservable{TResource}"/>.</returns>
      ///// <exception cref="ArgumentException">If <paramref name="creationParameters"/> is somehow invalid, e.g. of wrong type.</exception>
      //AsyncResourcePoolObservable<TResource> CreateOneTimeUseResourcePool( Object creationParameters );

      ///// <summary>
      ///// Creates a new instance of <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/>, which will provide a method to clean up resources which have been idle for longer than given time.
      ///// </summary>
      ///// <param name="creationParameters">The creation parameters for the resource pool.</param>
      ///// <returns>A new instance of <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/>.</returns>
      ///// <exception cref="ArgumentException">If <paramref name="creationParameters"/> is somehow invalid, e.g. of wrong type.</exception>
      ///// <remarks>
      ///// Note that the returned pool will not clean up resources automatically.
      ///// The <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync(TCleanUpParameter, System.Threading.CancellationToken)"/> method must be invoked explicitly by the user of resource pool.
      ///// </remarks>
      //AsyncResourcePoolObservable<TResource, TimeSpan> CreateTimeoutingResourcePool( Object creationParameters );

      ///// <summary>
      ///// Gets the default type of parameter for <see cref="CreateOneTimeUseResourcePool(object)"/> and <see cref="CreateTimeoutingResourcePool(object)"/> methods.
      ///// </summary>
      ///// <value>The default type of parameter for <see cref="CreateOneTimeUseResourcePool(object)"/> and <see cref="CreateTimeoutingResourcePool(object)"/> methods.</value>

      /// <summary>
      /// Gets the type which can be used when e.g. deserializing pool creation parameters from configuration.
      /// </summary>
      /// <value>The type which can be used when e.g. deserializing pool creation parameters from configuration.</value>
      Type DataTypeForCreationParameter { get; }

      /// <summary>
      /// This method will create a new instance of <see cref="AsyncResourceFactory{TResource, TParams}"/> that this <see cref="AsyncResourceFactoryProvider"/> is capable of creating, wrap it so that second type parameter of <see cref="AsyncResourceFactory{TResource, TParams}"/> will be <see cref="Object"/>, and invoke the given callback passing wrapper as argument.
      /// </summary>
      /// <typeparam name="TResource">The type of the resources the <see cref="AsyncResourcePool{TResource}"/> should create.</typeparam>
      /// <typeparam name="TResult">The return value of the callback.</typeparam>
      /// <param name="func">The callback to invoke. The first parameter will be <see cref="AsyncResourceFactory{TResource, TParams}"/> wrapper, and second parameter will be transformed <paramref name="creationParameters"/>.</param>
      /// <param name="creationParameters">The creation parameters for the pool to use. These will be transformed by this <see cref="AsyncResourceFactoryProvider"/> as needed in order to pass correct ones to the pool. Never use this parameter inside the <paramref name="func"/>!</param>
      /// <returns>The return value of the <paramref name="func"/> - which will typically be the <see cref="AsyncResourcePool{TResource}"/> or one of its derived interfaces.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="func"/> is <c>null</c>. May also throw is <paramref name="creationParameters"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">If a combination of <typeparamref name="TResource"/> and <typeparamref name="TResult"/> type parameters is not understood by this <see cref="AsyncResourceFactoryProvider"/>.</exception>
      TResult UseFactoryToCreatePool<TResource, TResult>( Func<AsyncResourceFactory<TResource, Object>, Object, TResult> func, Object creationParameters );

   }

   /// <summary>
   /// This class implements <see cref="AsyncResourceFactoryProvider"/> for given types of resource and creation parameters.
   /// </summary>
   /// <typeparam name="TCreationParameters">The type of the resource creation parameters - the second type parameter of <see cref="AsyncResourceFactory{TResource, TParams}"/>.</typeparam>
   public abstract class AbstractAsyncResourceFactoryProvider<TCreationParameters> : AsyncResourceFactoryProvider
   {
      /// <summary>
      /// Initializes a new instance of <see cref="AbstractAsyncResourceFactoryProvider{TFactoryResource, TCreationParameters}"/> with given parameters.
      /// </summary>
      /// <param name="resourceType">The actual type of the resources (first type parameter for <see cref="AsyncResourceFactory{TResource, TParams}"/>).</param>
      /// <param name="dataType">The type for <see cref="DataTypeForCreationParameter"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="resourceType"/> is <c>null</c>.</exception>
      public AbstractAsyncResourceFactoryProvider(
         Type resourceType,
         Type dataType
         )
      {
         this.ResourceType = ArgumentValidator.ValidateNotNull( nameof( resourceType ), resourceType );
         this.DataTypeForCreationParameter = dataType;
      }

      /// <summary>
      /// Implements <see cref="AsyncResourceFactoryProvider.UseFactoryToCreatePool"/>, by using <see cref="CreateFactory"/> and <see cref="TransformFactoryParameters"/> methods.
      /// </summary>
      /// <typeparam name="TResource">The type of the resources the <see cref="AsyncResourcePool{TResource}"/> should create.</typeparam>
      /// <typeparam name="TResult">The return value of the callback.</typeparam>
      /// <param name="func">The callback to invoke. The first parameter will be <see cref="AsyncResourceFactory{TResource, TParams}"/> wrapper, and second parameter will be transformed <paramref name="creationParameters"/>.</param>
      /// <param name="creationParameters">The creation parameters for the pool to use. These will be transformed by this <see cref="AsyncResourceFactoryProvider"/> as needed in order to pass correct ones to the pool. Never use this parameter inside the <paramref name="func"/>!</param>
      /// <returns>The return value of the <paramref name="func"/> - which will typically be the <see cref="AsyncResourcePool{TResource}"/> or one of its derived interfaces.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="func"/> is <c>null</c>. May also throw is <paramref name="creationParameters"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">If a combination of <typeparamref name="TResource"/> and <typeparamref name="TResult"/> type parameters is not understood by this <see cref="AsyncResourceFactoryProvider"/>.</exception>
      public TResult UseFactoryToCreatePool<TResource, TResult>( Func<AsyncResourceFactory<TResource, Object>, Object, TResult> func, Object creationParameters )
      {
         ArgumentValidator.ValidateNotNull( nameof( func ), func );

         var transformedParameters = this.TransformFactoryParameters( creationParameters );
         var factoryWrapper = (AsyncResourceFactory<TResource, Object>) typeof( FactoryWrapper<,,> )
            .MakeGenericType( this.ResourceType, typeof( TResource ), typeof( TCreationParameters ) )
#if NETSTANDARD1_0
            .GetTypeInfo()
            .DeclaredConstructors
            .First()
#else
            .GetConstructors()
            [0]
#endif
            .Invoke( new Object[] { this.CreateFactory( transformedParameters ) } );
         return func( factoryWrapper, transformedParameters );
      }

      /// <summary>
      /// Gets the type which can be used when e.g. deserializing pool creation parameters from configuration.
      /// </summary>
      /// <value>The type which can be used when e.g. deserializing pool creation parameters from configuration.</value>
      public Type DataTypeForCreationParameter { get; }

      /// <summary>
      /// Gets the actual type of the resources (first type parameter for <see cref="AsyncResourceFactory{TResource, TParams}"/>).
      /// </summary>
      /// <value>The actual type of the resources (first type parameter for <see cref="AsyncResourceFactory{TResource, TParams}"/>).</value>
      protected Type ResourceType { get; }

      /// <summary>
      /// Derived classes should implement this method to create a new instance of <see cref="AsyncResourceFactory{TResource, TParams}"/>.
      /// </summary>
      /// <param name="transformedCreationParameters">The creation parameters, as returned by <see cref="TransformFactoryParameters"/> method.</param>
      /// <returns>A new instance of <see cref="AsyncResourceFactory{TResource, TParams}"/>.</returns>
      protected abstract Object CreateFactory( TCreationParameters transformedCreationParameters );

      /// <summary>
      /// Derived classes should implement this method to perform necessary validations and transformations to a creation parameters passed to <see cref="UseFactoryToCreatePool"/> method.
      /// </summary>
      /// <param name="untyped">The creation parameters passed to <see cref="UseFactoryToCreatePool"/> method.</param>
      /// <returns>The transformed creation parameters.</returns>
      protected abstract TCreationParameters TransformFactoryParameters( Object untyped );
   }

   /// <summary>
   /// This class extends <see cref="AbstractAsyncResourceFactoryProvider{TCreationParameters}"/> in order to constraint the resource type when it is known at compile-time, and visibility constraints allow it to be specified in class hierarchy.
   /// </summary>
   /// <typeparam name="TCreationParameters">The type of the resource creation parameters - the second type parameter of <see cref="AsyncResourceFactory{TResource, TParams}"/>.</typeparam>
   /// <typeparam name="TFactoryResource">The type of the factory resource - the first type parameter of <see cref="AsyncResourceFactory{TResource, TParams}"/>.</typeparam>
   public abstract class AbstractAsyncResourceFactoryProvider<TCreationParameters, TFactoryResource> : AbstractAsyncResourceFactoryProvider<TCreationParameters>
   {
      /// <summary>
      /// Initializes a new instance of <see cref="AbstractAsyncResourceFactoryProvider{TCreationParameters, TFactoryResource}"/> with given parameters.
      /// </summary>
      /// <param name="dataType">The type of <see cref="AsyncResourceFactoryProvider.DataTypeForCreationParameter"/>.</param>
      public AbstractAsyncResourceFactoryProvider( Type dataType ) : base( typeof( TFactoryResource ), dataType )
      {
      }
   }


   internal sealed class FactoryWrapper<TActualResource, TSeenResource, TCreationParameters> : AsyncResourceFactory<TSeenResource, Object>
      where TActualResource : class, TSeenResource
   {
      private readonly AsyncResourceFactory<TActualResource, TCreationParameters> _factory;

      public FactoryWrapper(
         AsyncResourceFactory<TActualResource, TCreationParameters> factory
         )
      {
         this._factory = ArgumentValidator.ValidateNotNull( nameof( factory ), factory );
      }

      public async ValueTask<AsyncResourceAcquireInfo<TSeenResource>> AcquireResourceAsync( Object parameters, CancellationToken token ) =>
         await this._factory.AcquireResourceAsync( (TCreationParameters) parameters, token );

      public void ResetFactoryState() => this._factory.ResetFactoryState();

   }
}
