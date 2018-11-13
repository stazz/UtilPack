/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
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

namespace NetworkUtils.Configuration
{
   /// <summary>
   /// This class extends <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/> to add possibility for protocol-level authentication configuration.
   /// </summary>
   /// <typeparam name="TCreationData">The type of typically serializable configuration data, which only has passive properties. Must be or inherit <see cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration of <typeparamref name="TCreationData"/> controlling behaviour of the resource pool. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <typeparam name="TAuthenticationConfiguration">The type of authentication configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>
   public class NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration, TAuthenticationConfiguration> : NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>
      where TCreationData : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration, TAuthenticationConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration, TAuthenticationConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      /// <summary>
      /// Creates a new instance of <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/> with given <typeparamref name="TCreationData"/>.
      /// </summary>
      /// <param name="data">The <typeparamref name="TCreationData"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c>.</exception>
      public NetworkConnectionCreationInfo( TCreationData data )
         : base( data )
      {
      }
   }

   /// <summary>
   /// This class extends <see cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/> to add possibility for protocol-level authentication configuration.
   /// </summary>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration controlling behaviour of the resource pool. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <typeparam name="TAuthenticationConfiguration">The type of authentication configuration.</typeparam>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>
   public class NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration, TAuthenticationConfiguration> : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration, TAuthenticationConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
   }


   /// <summary>
   /// This class represents extends <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TPoolingConfiguration}"/> to add protocol-level authentication configuration.
   /// Examples of such protocols are protocols with SQL backends, while example of protocol without authentication on protocol level is HTTP.
   /// </summary>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of pooling configuration. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <typeparam name="TAuthenticationConfiguration">The type of authentication configuration.</typeparam>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>
   public class NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration, TAuthenticationConfiguration> : NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {

      /// <summary>
      /// Gets or sets the type containing passive configuration data about the authentication configuration for the underlying protocol.
      /// </summary>
      /// <value>The type containing passive configuration data about the authentication configuration for the underlying protocol.</value>
      public TAuthenticationConfiguration Authentication { get; set; }

   }

}
