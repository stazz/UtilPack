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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResourcePooling.Async.Abstractions
{
   /// <summary>
   /// Typically, most client code won't need to use this interface - it is provided for the sake of generic scenarios, where resource pool needs to be instantiated dynamically based on some kind of configuration.
   /// Most common scenario to create resource pools is to directly use vendor-specific class.
   /// </summary>
   public interface AsyncResourceFactoryProvider
   {
      /// <summary>
      /// Gets the type which can be used when e.g. deserializing pool creation parameters from configuration.
      /// </summary>
      /// <value>The type which can be used when e.g. deserializing pool creation parameters from configuration.</value>
      Type DataTypeForCreationParameter { get; }

      /// <summary>
      /// This method will create a new instance of <see cref="AsyncResourceFactory{TResource}"/> that this <see cref="AsyncResourceFactoryProvider"/> is capable of creating.
      /// </summary>
      /// <typeparam name="TResource">The type of the resources the <see cref="AsyncResourcePool{TResource}"/> should create.</typeparam>
      /// <param name="creationParameters">The creation parameters for the pool to use. These will be transformed by this <see cref="AsyncResourceFactoryProvider"/> as needed in order to pass correct ones to the pool.</param>
      /// <returns>A new instance of <see cref="AsyncResourceFactory{TResource}"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="creationParameters"/> is <c>null</c> and this factory deems it to be unacceptable.</exception>
      /// <exception cref="ArgumentException">If resource type <typeparamref name="TResource"/> is not supported by this <see cref="AsyncResourceFactoryProvider"/>.</exception>
      AsyncResourceFactory<TResource> BindCreationParameters<TResource>( Object creationParameters );

   }

   /// <summary>
   /// This class implements <see cref="AsyncResourceFactoryProvider"/> for given types of resource and creation parameters.
   /// </summary>
   /// <typeparam name="TFactoryResource">The type of resource as exposed via <see cref="AsyncResourceFactory{TResource, TParams}"/> interface - the first type parameter of <see cref="AsyncResourceFactory{TResource, TParams}"/>.</typeparam>
   /// <typeparam name="TCreationParameters">The type of the resource creation parameters - the second type parameter of <see cref="AsyncResourceFactory{TResource, TParams}"/>.</typeparam>
   public abstract class AbstractAsyncResourceFactoryProvider<TFactoryResource, TCreationParameters> : AsyncResourceFactoryProvider
   {
      /// <summary>
      /// Initializes a new instance of <see cref="AbstractAsyncResourceFactoryProvider{TFactoryResource, TCreationParameters}"/> with given parameters.
      /// </summary>
      /// <param name="dataType">The type for <see cref="DataTypeForCreationParameter"/>.</param>
      public AbstractAsyncResourceFactoryProvider(
         Type dataType
         )
      {
         this.DataTypeForCreationParameter = dataType;
      }

      /// <inheritdoc />
      public AsyncResourceFactory<TResource> BindCreationParameters<TResource>( Object creationParameters )
      {
         var boundFactory = ( this.CreateFactory() ?? throw new InvalidOperationException( "Failed to create unbound factory." ) )
            .BindCreationParameters( this.TransformFactoryParameters( creationParameters ) ) ?? throw new InvalidOperationException( "Failed to create bound factory." );
         if ( !( boundFactory is AsyncResourceFactory<TResource> retVal ) )
         {
            throw new ArgumentException( $"The type { typeof( TResource ) } is not assignable from { typeof( TFactoryResource ) }." );
         }

         return retVal;

      }

      /// <summary>
      /// Gets the type which can be used when e.g. deserializing pool creation parameters from configuration.
      /// </summary>
      /// <value>The type which can be used when e.g. deserializing pool creation parameters from configuration.</value>
      public Type DataTypeForCreationParameter { get; }

      /// <summary>
      /// Derived classes should implement this method to create a new instance of <see cref="AsyncResourceFactory{TResource, TParams}"/>.
      /// </summary>
      /// <returns>A new instance of <see cref="AsyncResourceFactory{TResource, TParams}"/>.</returns>
      protected abstract AsyncResourceFactory<TFactoryResource, TCreationParameters> CreateFactory();

      /// <summary>
      /// Derived classes should implement this method to perform necessary validations and transformations to a creation parameters passed to <see cref="BindCreationParameters"/> method.
      /// </summary>
      /// <param name="untyped">The creation parameters passed to <see cref="BindCreationParameters"/> method.</param>
      /// <returns>The transformed creation parameters.</returns>
      protected abstract TCreationParameters TransformFactoryParameters( Object untyped );
   }

}
