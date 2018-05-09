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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.ResourcePooling.NetworkStream;

using TNetworkStream = System.Net.Sockets.NetworkStream;

namespace UtilPack.ResourcePooling.NetworkStream
{
   /// <summary>
   /// This class represents typical creation parameters for resource pools using <see cref="NetworkStreamFactory"/> and thus <see cref="NetworkStreamFactoryConfiguration"/>.
   /// This class should be subclassed by actual network protocol implementations to bind generic parameters and include protocol-specific information.
   /// </summary>
   /// <typeparam name="TCreationData">The type of typically serializable configuration data, which only has passive properties. Must be or inherit <see cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TAuthenticationConfiguration">The type of authentication configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration of <typeparamref name="TCreationData"/> controlling behaviour of <see cref="ResourcePooling.AsyncResourcePool{TResource}"/>. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>
   /// <seealso cref="E_UtilPack.CreateNetworkStreamFactoryConfiguration"/>
   /// <seealso cref="E_UtilPack.CreateStatefulNetworkStreamFactoryConfiguration"/>
   public class NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TCreationData : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      /// <summary>
      /// Creates a new instance of <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/> with given <typeparamref name="TCreationData"/>.
      /// </summary>
      /// <param name="data">The <typeparamref name="TCreationData"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c>.</exception>
      public NetworkConnectionCreationInfo( TCreationData data )
      {
         this.CreationData = ArgumentValidator.ValidateNotNull( nameof( data ), data );

#if NETSTANDARD2_0 || NETCOREAPP1_1 || NET45 || NET40
         this.ProvideSSLStream = (
            Stream innerStream,
            Boolean leaveInnerStreamOpen,
            RemoteCertificateValidationCallback userCertificateValidationCallback,
            LocalCertificateSelectionCallback userCertificateSelectionCallback,
            out AuthenticateAsClientAsync authenticateAsClientAsync
            ) =>
         {
            authenticateAsClientAsync = (
               Stream stream,
               String targetHost,
               System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates,
               System.Security.Authentication.SslProtocols enabledSslProtocols,
               Boolean checkCertificateRevocation
            ) =>
            {
               return ( (System.Net.Security.SslStream) stream ).AuthenticateAsClientAsync( targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation );
            };

            return new System.Net.Security.SslStream(
               innerStream,
               leaveInnerStreamOpen,
                  (
                     Object sender,
                     System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                     System.Security.Cryptography.X509Certificates.X509Chain chain,
                     System.Net.Security.SslPolicyErrors sslPolicyErrors
                     ) => userCertificateValidationCallback?.Invoke( sender, certificate, chain, sslPolicyErrors ) ?? true,
               userCertificateSelectionCallback == null ?
                  (System.Net.Security.LocalCertificateSelectionCallback) null :
                  (
                     Object sender,
                     String targetHost,
                     System.Security.Cryptography.X509Certificates.X509CertificateCollection localCertificates,
                     System.Security.Cryptography.X509Certificates.X509Certificate remoteCertificate,
                     String[] acceptableIssuers
                  ) => userCertificateSelectionCallback( sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers ),
               System.Net.Security.EncryptionPolicy.RequireEncryption
               );
         };
#endif
      }

      /// <summary>
      /// Gets the <typeparamref name="TCreationData"/> associated with this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.
      /// </summary>
      /// <value>The <typeparamref name="TCreationData"/> associated with this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</value>
      public TCreationData CreationData { get; }

#if !NETSTANDARD1_0

      /// <summary>
      /// Gets or sets the callback which should select single <see cref="IPAddress"/> from an array of <see cref="IPAddress"/> that were resolved from the hostname.
      /// </summary>
      /// <value>The callback which should select single <see cref="IPAddress"/> from an array of <see cref="IPAddress"/> that were resolved from the hostname.</value>
      /// <remarks>
      /// This will be invoked only when the amount of resolved <see cref="IPAddress"/>es is more than <c>1</c>.
      /// If this returns <c>null</c>, the first <see cref="IPAddress"/> will be used.
      /// </remarks>
      public Func<IPAddress[], IPAddress> SelectRemoteIPAddress { get; set; }

      /// <summary>
      /// Gets or sets the callback which should provide the local <see cref="IPEndPoint"/> given remote <see cref="IPEndPoint"/>.
      /// </summary>
      /// <value>The callback which should provide the local <see cref="IPEndPoint"/> given remote <see cref="IPEndPoint"/>.</value>
      /// <remarks>
      /// If this is <c>null</c> or returns <c>null</c>, a first free local endpoint will be used.
      /// </remarks>
      public Func<IPEndPoint, IPEndPoint> SelectLocalIPEndPoint { get; set; }

#if NETSTANDARD1_3

      /// <summary>
      /// Gets or sets the callback which should perform DNS resolve from host name.
      /// </summary>
      /// <value>The callback which should perform DNS resolve from host name.</value>
      /// <remarks>
      /// This property is available only on platforms .NET Standard 1.3-1.6.
      /// </remarks>
      public Func<String, ValueTask<IPAddress[]>> DNSResolve { get; set; }

#endif

      /// <summary>
      /// This event is used to add client certificates to <see cref="System.Security.Cryptography.X509Certificates.X509CertificateCollection"/> when using SSL to connect to the backend.
      /// </summary>
      public event Action<System.Security.Cryptography.X509Certificates.X509CertificateCollection> ProvideClientCertificatesEvent;
      internal Action<System.Security.Cryptography.X509Certificates.X509CertificateCollection> ProvideClientCertificates => this.ProvideClientCertificatesEvent;

      /// <summary>
      /// This callback is used to create SSL stream when using SSL to connect to backend.
      /// </summary>
      /// <value>The callback to create SSL stream when using SSL to connect to backend.</value>
      /// <remarks>
      /// In .NET Core App 1.1+ and .NET Desktop 4.0+ environments this will be set to default by the constructor.
      /// </remarks>
      public ProvideSSLStream ProvideSSLStream { get; set; }

      /// <summary>
      /// This callback will be used to validate server certificate when using SSL to connect to the backend.
      /// </summary>
      /// <value>The callback will to validate server certificate when using SSL to connect to the backend.</value>
      /// <remarks>
      /// When not specified (i.e. left to <c>null</c>), server certificate (if provided) will always be accepted.
      /// </remarks>
      public RemoteCertificateValidationCallback ValidateServerCertificate { get; set; }

      /// <summary>
      /// This callback will be used to select local certificate when using SSL to connect to the backend.
      /// </summary>
      /// <value>The callback to select local certificate when using SSL to connect to the backend.</value>
      /// <remarks>
      /// When not specified (i.e. left to <c>null</c>), the first of the certificates is selected, if any.
      /// </remarks>
      public LocalCertificateSelectionCallback SelectLocalCertificate { get; set; }

#endif

      /// <summary>
      /// This property is special in a sense that it is only one visible in .NET Standard 1.0-1.2 environments, and thus is mandatory on those platforms.
      /// On .NET Standard 1.3, .NET Core App 1.1+, and .NET Desktop 4.0+, this property is optional, and may be used to override socket initialization routine and create <see cref="Stream"/> right away.
      /// </summary>
      /// <value>Callback to override socket intialization and create <see cref="Stream"/> right away.</value>
      /// <remarks>
      /// </remarks>
      public Func<ValueTask<Stream>> StreamFactory { get; set; }
   }

   /// <summary>
   /// This class represents typical creation parameter data for resource pools utilizing <see cref="NetworkStreamFactory"/> and thus <see cref="NetworkStreamFactoryConfiguration"/>.
   /// This class should be subclassed by actual network protocol implementations to bind generic parameters and include protocol-specific information.
   /// </summary>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration.</typeparam>
   /// <typeparam name="TAuthenticationConfiguration">The type of authentication configuration.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration controlling behaviour of <see cref="ResourcePooling.AsyncResourcePool{TResource}"/>. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>
   public class NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
#if !NETSTANDARD1_0

      /// <summary>
      /// Gets or sets the <typeparamref name="TConnectionConfiguration"/>, holding data related to socket-based connections.
      /// </summary>
      /// <value>The <typeparamref name="TConnectionConfiguration"/>, holding data related to socket-based connections.</value>
      public TConnectionConfiguration Connection { get; set; }

#endif

      /// <summary>
      /// Gets or sets the <typeparamref name="TInitializationConfiguration"/>, holding data related to initialization process of the underlying protocol.
      /// </summary>
      /// <value>The <typeparamref name="TInitializationConfiguration"/>, holding data related to initialization process of the underlying protocol.</value>
      public TInitializationConfiguration Initialization { get; set; }
   }

   /// <summary>
   /// This class represents typical configuration data for establishing a network connection to remote endpoint when utilizing <see cref="NetworkStreamFactory"/>.
   /// This class should be subclassed by actual network protocol implementations to include protocol-specific information.
   /// </summary>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>
   public class NetworkConnectionConfiguration
   {
      /// <summary>
      /// This constant defines default SSL protocol, if SSL is enabled.
      /// </summary>
      /// <remarks>
      /// In .NET 4.0 environment, this is Tls. In other environments, it is Tls1.2.
      /// </remarks>
      public const System.Security.Authentication.SslProtocols DEFAULT_SSL_PROTOCOL = System.Security.Authentication.SslProtocols
#if NET40
            .Tls
#else
            .Tls12
#endif
         ;

      /// <summary>
      /// Creates a new instance of <see cref="NetworkConnectionConfiguration"/> with default values.
      /// </summary>
      /// <remarks>
      /// This constructor sets <see cref="SSLProtocols"/> to <see cref="DEFAULT_SSL_PROTOCOL"/> value.
      /// </remarks>
      public NetworkConnectionConfiguration()
      {
         this.SSLProtocols = DEFAULT_SSL_PROTOCOL;
      }

      /// <summary>
      /// Gets or sets the host name of the PostgreSQL backend process.
      /// </summary>
      /// <value>The host name of the PostgreSQL backend process.</value>
      /// <remarks>
      /// This should be either textual IP address, or host name.
      /// </remarks>
      public String Host { get; set; }

      /// <summary>
      /// Gets or sets the port of the PostgreSQL backend process.
      /// </summary>
      /// <value>The port of the PostgreSQL backend process.</value>
      public Int32 Port { get; set; }

      ///// <summary>
      ///// Gets or sets the host name to use for local endpoint of the socket connection.
      ///// May be <c>null</c>, in which case default is used.
      ///// </summary>
      ///// <value>The host name to use for local endpoint of the socket connection.</value>
      //public String LocalHost { get; set; }

      ///// <summary>
      ///// Gets or sets the port to use for local endpoint of the socket connection.
      ///// </summary>
      ///// <value>The port to use for local endpoint of the socket connection.</value>
      //public Int32 LocalPort { get; set; }

      /// <summary>
      /// Gets or sets the <see cref="UtilPack.ResourcePooling.NetworkStream.ConnectionSSLMode"/> to control the SSL encryption for the socket connection.
      /// </summary>
      /// <value>The <see cref="UtilPack.ResourcePooling.NetworkStream.ConnectionSSLMode"/> to control the SSL encryption for the socket connection.</value>
      public ConnectionSSLMode ConnectionSSLMode { get; set; }

      /// <summary>
      /// Gets or sets the <see cref="System.Security.Authentication.SslProtocols"/> controlling what kind of SSL encryption will be used for the socket connection.
      /// </summary>
      /// <value>The <see cref="System.Security.Authentication.SslProtocols"/> controlling what kind of SSL encryption will be used for the socket connection.</value>
      /// <remarks>
      /// This field will only be used of <see cref="ConnectionSSLMode"/> property will be something else than <see cref="UtilPack.ResourcePooling.NetworkStream.ConnectionSSLMode.NotRequired"/>
      /// </remarks>
      public System.Security.Authentication.SslProtocols SSLProtocols { get; set; }
   }

   /// <summary>
   /// This class represents typical configuration data for initializing a network connection to remote endpoint when utilizing <see cref="NetworkStreamFactory"/>.
   /// This class should be subclassed by actual network protocol implementations to include protocol-specific information.
   /// </summary>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>
   public class NetworkInitializationConfiguration<TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      /// <summary>
      /// Gets or sets the type containing passive configuration data about the communication protocol -specific settings.
      /// </summary>
      /// <value>The type containing passive configuration data about the communication protocol -specific settings.</value>
      public TProtocolConfiguration Protocol { get; set; }

      /// <summary>
      /// Gets or sets the type containing passive configuration data about the authentication configuration for the underlying protocol.
      /// </summary>
      /// <value>The type containing passive configuration data about the authentication configuration for the underlying protocol.</value>
      public TAuthenticationConfiguration Authentication { get; set; }

      /// <summary>
      /// Gets or sets the type containing passive configuration data about the behaviour of the connections when they are used within the connection pool.
      /// </summary>
      /// <value>The type containing passive configuration data about the behaviour of the connections when they are used within the connection pool.</value>
      public TPoolingConfiguration ConnectionPool { get; set; }
   }

   /// <summary>
   /// This class represents typical configuration data for working with <see cref="AsyncResourcePool{TResource}"/> operating on <see cref="NetworkStreamFactory"/>.
   /// This class should be subclassed by actual network protocol implementations to include protocol-specific information.
   /// </summary>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>
   public class NetworkPoolingConfiguration
   {
      /// <summary>
      /// Gets or sets the value indicating whether each connection should have its own <see cref="BinaryStringPool"/>.
      /// </summary>
      /// <value>The value indicating whether each connection should have its own <see cref="BinaryStringPool"/>.</value>
      /// <remarks>
      /// Typically this should be true if the same connection pool is used to access secure data of multiple roles or conceptional users.
      /// </remarks>
      public Boolean ConnectionsOwnStringPool { get; set; }
   }

   /// <summary>
   /// This class exists in order to avoid specifying *all* generic parameters when creating <see cref="NetworkStreamFactoryConfiguration{TState}"/> from <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.
   /// </summary>
   /// <typeparam name="TCreationData">The type of typically serializable configuration data, which only has passive properties. Must be or inherit <see cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TAuthenticationConfiguration">The type of authentication configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration of <typeparamref name="TCreationData"/> controlling behaviour of <see cref="AsyncResourcePool{TResource}"/>. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <seealso cref="E_UtilPack.CreateStatefulNetworkStreamFactoryConfiguration"/>
   public sealed class StatefulNetworkStreamFactoryCreator<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TCreationData : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      private readonly NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration> _creationInfo;

      internal StatefulNetworkStreamFactoryCreator(
          NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration> creationInfo
         )
      {
         this._creationInfo = ArgumentValidator.ValidateNotNull( nameof( creationInfo ), creationInfo );
      }

      /// <summary>
      /// Creates a <see cref="NetworkStreamFactoryConfiguration{TState}"/> along with other items based on the <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/> this instance has.
      /// </summary>
      /// <typeparam name="TIntermediateState">The type of generic parameter of <see cref="NetworkStreamFactoryConfiguration{TState}"/>.</typeparam>
      /// <param name="stateFactory">The callback to create intermediate state.</param>
      /// <param name="encoding">The <see cref="Encoding"/> to use to read strings from stream. Affects the returned <see cref="BinaryStringPool"/> only.</param>
      /// <param name="isSSLPossible">Callback to check whether SSL is possible. If <c>null</c>, then SSL stream creation will not be possible.</param>
      /// <param name="getSSLProtocols">Callback to get the <see cref="System.Security.Authentication.SslProtocols"/> to use. If <c>null</c>, the TLS1.2 will be used.</param>
      /// <param name="noSSLStreamProvider">The callback to create an <see cref="Exception"/> to be thrown when there are no SSL stream provider, but should be one.</param>
      /// <param name="remoteNoSSLSupport">The callback to create an <see cref="Exception"/> to be thrown when remote does not support SSL when it should.</param>
      /// <param name="sslStreamProviderNoStream">The callback to create an <see cref="Exception"/> to be thrown when <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}.ProvideSSLStream"/> returns <c>null</c>.</param>
      /// <param name="sslStreamProviderNoAuthenticationCallback">The callback to create an <see cref="Exception"/> when <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}.ProvideSSLStream"/> does not provide authentication callback.</param>
      /// <param name="sslStreamOtherError">The callback to create an <see cref="Exception"/> when other SSL-related error occurs.</param>
      /// <returns>A tuple containing <see cref="NetworkStreamFactoryConfiguration{TState}"/> based on the <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/> this instance holds, along with awaitable to get remote <see cref="IPAddress"/>, and <see cref="BinaryStringPool"/> instance.</returns>
      public (NetworkStreamFactoryConfiguration<TIntermediateState>, ReadOnlyResettableAsyncLazy<IPAddress>, BinaryStringPool) Create<TIntermediateState>(
         Func<Socket, TNetworkStream, CancellationToken, TIntermediateState> stateFactory,
         Encoding encoding,
         Func<TIntermediateState, Task<Boolean>> isSSLPossible,
         Func<TIntermediateState, System.Security.Authentication.SslProtocols> getSSLProtocols,
         Func<Exception> noSSLStreamProvider,
         Func<Exception> remoteNoSSLSupport,
         Func<Exception> sslStreamProviderNoStream,
         Func<Exception> sslStreamProviderNoAuthenticationCallback,
         Func<Exception, Exception> sslStreamOtherError
         )
      {
         var creationInfo = this._creationInfo;

         var data = creationInfo.CreationData;
         var host = data.Connection?.Host;
         var remoteAddress = host.CreateAddressOrHostNameResolvingLazy(
               creationInfo.SelectRemoteIPAddress,
#if NETSTANDARD1_3
               creationInfo.DNSResolve
#else
               null
#endif
            );

         return (new NetworkStreamFactoryConfiguration<TIntermediateState>()
         {
            CreateState = stateFactory,
            SelectLocalIPEndPoint = creationInfo.SelectLocalIPEndPoint,
            RemoteAddress = async ( token ) => await remoteAddress,
            RemotePort = addr => creationInfo.CreationData?.Connection?.Port ?? 5432,

            // SSL stuff here
            ConnectionSSLMode = ( state ) => data.Connection?.ConnectionSSLMode ?? ConnectionSSLMode.NotRequired,
            IsSSLPossible = isSSLPossible,
            GetSSLProtocols = getSSLProtocols,
            ProvideSSLStream = creationInfo.ProvideSSLStream,
            ProtocolType = System.Net.Sockets.ProtocolType.Tcp,
            SocketType = System.Net.Sockets.SocketType.Stream,
            ProvideSSLHost = ( state ) => host,
            SelectLocalCertificate = creationInfo.SelectLocalCertificate,
            ValidateServerCertificate = creationInfo.ValidateServerCertificate,

            // Exception creation callbacks
            NoSSLStreamProvider = noSSLStreamProvider,
            RemoteNoSSLSupport = remoteNoSSLSupport,
            SSLStreamProviderNoStream = sslStreamProviderNoStream,
            SSLStreamProviderNoAuthenticationCallback = sslStreamProviderNoAuthenticationCallback,
            SSLStreamOtherError = sslStreamOtherError
         }, remoteAddress, ( creationInfo.CreationData?.Initialization?.ConnectionPool?.ConnectionsOwnStringPool ?? default ) ? default : BinaryStringPoolFactory.NewConcurrentBinaryStringPool( encoding ));
      }
   }
}

/// <summary>
/// Contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// Creates a <see cref="NetworkStreamFactoryConfiguration"/> along with other items based on this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.
   /// </summary>
   /// <param name="creationInfo">This <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</param>
   /// <param name="encoding">The <see cref="Encoding"/> to use to read strings from stream. Affects the returned <see cref="BinaryStringPool"/> only.</param>
   /// <param name="isSSLPossible">Callback to check whether SSL is possible. If <c>null</c>, then SSL stream creation will not be possible.</param>
   /// <param name="getSSLProtocols">Callback to get the <see cref="System.Security.Authentication.SslProtocols"/> to use. If <c>null</c>, the TLS1.2 will be used.</param>
   /// <param name="noSSLStreamProvider">The callback to create an <see cref="Exception"/> to be thrown when there are no SSL stream provider, but should be one.</param>
   /// <param name="remoteNoSSLSupport">The callback to create an <see cref="Exception"/> to be thrown when remote does not support SSL when it should.</param>
   /// <param name="sslStreamProviderNoStream">The callback to create an <see cref="Exception"/> to be thrown when <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}.ProvideSSLStream"/> returns <c>null</c>.</param>
   /// <param name="sslStreamProviderNoAuthenticationCallback">The callback to create an <see cref="Exception"/> when <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}.ProvideSSLStream"/> does not provide authentication callback.</param>
   /// <param name="sslStreamOtherError">The callback to create an <see cref="Exception"/> when other SSL-related error occurs.</param>
   /// <returns>A tuple containing <see cref="NetworkStreamFactoryConfiguration"/> based on the <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/> this instance holds, along with awaitable to get remote <see cref="IPAddress"/>, and <see cref="BinaryStringPool"/> instance.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/> is <c>null</c>.</exception>
   public static (NetworkStreamFactoryConfiguration, ReadOnlyResettableAsyncLazy<IPAddress>, BinaryStringPool) CreateNetworkStreamFactoryConfiguration<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>(
      this NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration> creationInfo,
      Encoding encoding,
      Func<Task<Boolean>> isSSLPossible,
      Func<System.Security.Authentication.SslProtocols> getSSLProtocols,
      Func<Exception> noSSLStreamProvider,
      Func<Exception> remoteNoSSLSupport,
      Func<Exception> sslStreamProviderNoStream,
      Func<Exception> sslStreamProviderNoAuthenticationCallback,
      Func<Exception, Exception> sslStreamOtherError
      )
      where TCreationData : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      var data = creationInfo.CreationData;
      var host = data.Connection?.Host;
      var remoteAddress = host.CreateAddressOrHostNameResolvingLazy(
            creationInfo.SelectRemoteIPAddress,
#if NETSTANDARD1_3
               creationInfo.DNSResolve
#else
               null
#endif
            );
      return (new NetworkStreamFactoryConfiguration()
      {
         SelectLocalIPEndPoint = creationInfo.SelectLocalIPEndPoint,
         RemoteAddress = async ( token ) => await remoteAddress,
         RemotePort = addr => creationInfo.CreationData?.Connection?.Port ?? 5432,

         // SSL stuff here
         ConnectionSSLMode = () => data.Connection?.ConnectionSSLMode ?? ConnectionSSLMode.NotRequired,
         IsSSLPossible = isSSLPossible,
         GetSSLProtocols = getSSLProtocols,
         ProvideSSLStream = creationInfo.ProvideSSLStream,
         ProtocolType = System.Net.Sockets.ProtocolType.Tcp,
         SocketType = System.Net.Sockets.SocketType.Stream,
         ProvideSSLHost = () => host,
         SelectLocalCertificate = creationInfo.SelectLocalCertificate,
         ValidateServerCertificate = creationInfo.ValidateServerCertificate,

         // Exception creation callbacks
         NoSSLStreamProvider = noSSLStreamProvider,
         RemoteNoSSLSupport = remoteNoSSLSupport,
         SSLStreamProviderNoStream = sslStreamProviderNoStream,
         SSLStreamProviderNoAuthenticationCallback = sslStreamProviderNoAuthenticationCallback,
         SSLStreamOtherError = sslStreamOtherError
      }, remoteAddress, ( creationInfo.CreationData?.Initialization?.ConnectionPool?.ConnectionsOwnStringPool ?? default ) ? default : BinaryStringPoolFactory.NewConcurrentBinaryStringPool( encoding ));
   }

   /// <summary>
   /// Creates a <see cref="StatefulNetworkStreamFactoryCreator{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/> to use to create <see cref="NetworkStreamFactoryConfiguration{TState}"/> instances based on this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.
   /// </summary>
   /// <typeparam name="TCreationData">The type of typically serializable configuration data, which only has passive properties. Must be or inherit <see cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TAuthenticationConfiguration">The type of authentication configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration of <typeparamref name="TCreationData"/> controlling behaviour of <see cref="UtilPack.ResourcePooling.AsyncResourcePool{TResource}"/>. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <param name="creationInfo">This <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/>.</param>
   /// <returns>An class that can be used to create <see cref="NetworkStreamFactoryConfiguration{TState}"/> instances.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration}"/> is <c>null</c>.</exception>
   public static StatefulNetworkStreamFactoryCreator<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration> CreateStatefulNetworkStreamFactoryConfiguration<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>(
      this NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration> creationInfo
      )
      where TCreationData : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      return new StatefulNetworkStreamFactoryCreator<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TAuthenticationConfiguration, TPoolingConfiguration>( ArgumentValidator.ValidateNotNullReference( creationInfo ) );
   }
}