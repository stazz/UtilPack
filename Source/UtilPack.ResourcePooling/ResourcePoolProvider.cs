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

namespace UtilPack.ResourcePooling
{
   /// <summary>
   /// This interface provides API to create instances of <see cref="AsyncResourcePoolObservable{TResource}"/> and <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/> resource pools.
   /// The creation parameters are not type-constrained in order for this interface to be used in generic scenarios.
   /// E.g. in SQL resources, the type of resource is constrained for all SQL resources, but creation parameter type is usually very vendor-specific.
   /// </summary>
   /// <typeparam name="TResource"></typeparam>
   /// <remarks>
   /// Typically, most client code won't need to use this interface - it is provided for the sake of generic scenarios, where resource pool needs to be instantiated dynamically based on some kind of configuration.
   /// Most common scenario to create resource pools is to directly use vendor-specific class.
   /// </remarks>
   public interface AsyncResourcePoolProvider<out TResource>
   {
      /// <summary>
      /// Creates a new instance of <see cref="AsyncResourcePoolObservable{TResource}"/>, which will close all resources as they are returned to pool.
      /// This is typically useful in test scenarios.
      /// </summary>
      /// <param name="creationParameters">The creation parameters for the resource pool.</param>
      /// <returns>A new instance of <see cref="AsyncResourcePoolObservable{TResource}"/>.</returns>
      /// <exception cref="ArgumentException">If <paramref name="creationParameters"/> is somehow invalid, e.g. of wrong type.</exception>
      AsyncResourcePoolObservable<TResource> CreateOneTimeUseResourcePool( Object creationParameters );

      /// <summary>
      /// Creates a new instance of <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/>, which will provide a method to clean up resources which have been idle for longer than given time.
      /// </summary>
      /// <param name="creationParameters">The creation parameters for the resource pool.</param>
      /// <returns>A new instance of <see cref="AsyncResourcePoolObservable{TResource, TCleanUpParameter}"/>.</returns>
      /// <exception cref="ArgumentException">If <paramref name="creationParameters"/> is somehow invalid, e.g. of wrong type.</exception>
      /// <remarks>
      /// Note that the returned pool will not clean up resources automatically.
      /// The <see cref="AsyncResourcePoolCleanUp{TCleanUpParameter}.CleanUpAsync(TCleanUpParameter, System.Threading.CancellationToken)"/> method must be invoked explicitly by the user of resource pool.
      /// </remarks>
      AsyncResourcePoolObservable<TResource, TimeSpan> CreateTimeoutingResourcePool( Object creationParameters );

      /// <summary>
      /// Gets the default type of parameter for <see cref="CreateOneTimeUseResourcePool(object)"/> and <see cref="CreateTimeoutingResourcePool(object)"/> methods.
      /// </summary>
      /// <value>The default type of parameter for <see cref="CreateOneTimeUseResourcePool(object)"/> and <see cref="CreateTimeoutingResourcePool(object)"/> methods.</value>
      Type DefaultTypeForCreationParameter { get; }
   }
}
