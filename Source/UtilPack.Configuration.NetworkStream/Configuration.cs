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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UtilPack.Configuration.NetworkStream
{
   /// <summary>
   /// This class represents typical creation parameters for resource pools using some kind of network protocol.
   /// This class should be subclassed by actual network protocol implementations to bind generic parameters and include protocol-specific information.
   /// </summary>
   /// <typeparam name="TCreationData">The type of typically serializable configuration data, which only has passive properties. Must be or inherit <see cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration of <typeparamref name="TCreationData"/> controlling behaviour of the resource pool. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>
   public class NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>
      where TCreationData : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      /// <summary>
      /// Creates a new instance of <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/> with given <typeparamref name="TCreationData"/>.
      /// </summary>
      /// <param name="data">The <typeparamref name="TCreationData"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c>.</exception>
      public NetworkConnectionCreationInfo( TCreationData data )
      {
         this.CreationData = data ?? throw new ArgumentNullException( nameof( data ) );

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

#if !NETSTANDARD1_0 && !NETSTANDARD1_3
         this.DNSResolve = host =>
#if NET40
            Task.Factory.FromAsync(
               ( hostArg, cb, state ) => Dns.BeginGetHostAddresses( hostArg, cb, state ),
               ( result ) => Dns.EndGetHostAddresses( result ),
               host,
               null
               )
#else
            Dns.GetHostAddressesAsync( host )
#endif
               ;
#endif
      }

      /// <summary>
      /// Gets the <typeparamref name="TCreationData"/> associated with this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.
      /// </summary>
      /// <value>The <typeparamref name="TCreationData"/> associated with this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.</value>
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


      /// <summary>
      /// Gets or sets the callback which should perform DNS resolve from host name.
      /// </summary>
      /// <value>The callback which should perform DNS resolve from host name.</value>
      /// <remarks>
      /// This property is available only on platforms other than .NET Standard 1.0.
      /// It is always automatically set to use methods of <see cref="T:System.Net.Dns"/> by the constructor for platforms other than .NET Standard 1.3.
      /// </remarks>
      public Func<String, Task<IPAddress[]>> DNSResolve { get; set; }

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
      public Func<Task<Stream>> StreamFactory { get; set; }
   }

   /// <summary>
   /// This class represents typical creation parameter data for resource pools using some kind of network protocol.
   /// This class should be subclassed by actual network protocol implementations to bind generic parameters and include protocol-specific information.
   /// </summary>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration controlling behaviour of the resource pool. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>
   public class NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration>
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
   /// This class represents typical configuration data for establishing a network connection to remote endpoint.
   /// This class should be subclassed by actual network protocol implementations to include protocol-specific information.
   /// </summary>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>
   public class NetworkConnectionConfiguration
   {

#if !NETSTANDARD1_0
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
      /// Gets or sets the <see cref="UtilPack.Configuration.NetworkStream.ConnectionSSLMode"/> to control the SSL encryption for the socket connection.
      /// </summary>
      /// <value>The <see cref="UtilPack.Configuration.NetworkStream.ConnectionSSLMode"/> to control the SSL encryption for the socket connection.</value>
      public ConnectionSSLMode ConnectionSSLMode { get; set; }

      /// <summary>
      /// Gets or sets the <see cref="System.Security.Authentication.SslProtocols"/> controlling what kind of SSL encryption will be used for the socket connection.
      /// </summary>
      /// <value>The <see cref="System.Security.Authentication.SslProtocols"/> controlling what kind of SSL encryption will be used for the socket connection.</value>
      /// <remarks>
      /// This field will only be used of <see cref="ConnectionSSLMode"/> property will be something else than <see cref="UtilPack.Configuration.NetworkStream.ConnectionSSLMode.NotRequired"/>
      /// </remarks>
      public System.Security.Authentication.SslProtocols SSLProtocols { get; set; }
#endif

   }

   /// <summary>
   /// This class represents typical configuration data for initializing a network connection to remote endpoint when utilizing network.
   /// This class should be subclassed by actual network protocol implementations to include protocol-specific information.
   /// </summary>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>
   public class NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      /// <summary>
      /// Gets or sets the type containing passive configuration data about the communication protocol -specific settings.
      /// </summary>
      /// <value>The type containing passive configuration data about the communication protocol -specific settings.</value>
      public TProtocolConfiguration Protocol { get; set; }

      /// <summary>
      /// Gets or sets the type containing passive configuration data about the behaviour of the connections when they are used within the connection pool.
      /// </summary>
      /// <value>The type containing passive configuration data about the behaviour of the connections when they are used within the connection pool.</value>
      public TPoolingConfiguration ConnectionPool { get; set; }
   }

   /// <summary>
   /// This class represents typical configuration data for working with resource pools operating on network stream-based protocol abstractions.
   /// This class should be subclassed by actual network protocol implementations to include protocol-specific information.
   /// </summary>
   /// <remarks>
   /// This is a passive class with gettable and settable properties, and should be used as strong-typed configration read from some external configuration file or other source.
   /// </remarks>
   /// <seealso cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>
   public class NetworkPoolingConfiguration
   {
      /// <summary>
      /// Gets or sets the value indicating whether each connection should have its own string pool.
      /// </summary>
      /// <value>The value indicating whether each connection should have its own string pool.</value>
      /// <remarks>
      /// Typically this should be true if the same connection pool is used to access secure data of multiple roles or conceptional users.
      /// </remarks>
      public Boolean ConnectionsOwnStringPool { get; set; }
   }

#if !NETSTANDARD1_0
   /// <summary>
   /// This enumeration tells the behaviour of SSL stream establishment when creating connection.
   /// </summary>
   public enum ConnectionSSLMode
   {
      /// <summary>
      /// This is the default value, that will cause SSL stream establishment never to occur - all data will be passed as-is to the underlying <see cref="Stream"/>.
      /// </summary>
      NotRequired,

      /// <summary>
      /// This mode will cause initiation of SSL stream establishment, but in case of error will silently fallback to non-SSL stream.
      /// </summary>
      Preferred,

      /// <summary>
      /// This mode will cause initiation of SSL stream establishment, and if any error occurs, an exception will be thrown.
      /// </summary>
      Required
   }


   /// <summary>
   /// This delegate is used by signature of <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}.ProvideSSLStream"/> in order to customize providing of SSL stream.
   /// </summary>
   /// <param name="innerStream">The inner, unencrypted stream.</param>
   /// <param name="leaveInnerStreamOpen">Whether to leave inner stream opened.</param>
   /// <param name="userCertificateValidationCallback">The callback to validate user certificates.</param>
   /// <param name="userCertificateSelectionCallback">The callback to select user certificates.</param>
   /// <param name="authenticateAsClientAsync">This parameter should contain the <see cref="AuthenticateAsClientAsync"/> callback that will be used by by this library to authenticate the connection.</param>
   /// <returns>The constructed SSL stream.</returns>
   /// <remarks>
   /// The <paramref name="authenticateAsClientAsync"/> parameter typically is a call to <see cref="M:System.Net.Security.SslStream.AuthenticateAsClientAsync(System.String,System.Security.Cryptography.X509Certificates.X509CertificateCollection,System.Security.Authentication.SslProtocols,System.Boolean)"/> method.
   /// </remarks>
   public delegate Stream ProvideSSLStream(
      Stream innerStream,
      Boolean leaveInnerStreamOpen,
      RemoteCertificateValidationCallback userCertificateValidationCallback,
      LocalCertificateSelectionCallback userCertificateSelectionCallback,
      out AuthenticateAsClientAsync authenticateAsClientAsync
      );


   /// <summary>
   /// This delegate is used by signature of <see cref="ProvideSSLStream"/> in order to validate the certificate of the PostgreSQL backend.
   /// The signature is just a copy of <see cref="T:System.Net.Security.RemoteCertificateValidationCallback"/> which is not available on all platforms.
   /// </summary>
   /// <param name="sender">The sender.</param>
   /// <param name="certificate">The certificate of the PostgreSQL backend.</param>
   /// <param name="chain">The chain of certificate authorities associated with the <paramref name="certificate"/>.</param>
   /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
   /// <returns><c>true</c> if PostgreSQL backend certificate is OK; <c>false</c> otherwise.</returns>
   public delegate Boolean RemoteCertificateValidationCallback(
      Object sender,
      System.Security.Cryptography.X509Certificates.X509Certificate certificate,
      System.Security.Cryptography.X509Certificates.X509Chain chain,
      System.Net.Security.SslPolicyErrors sslPolicyErrors
      );

   /// <summary>
   /// This delegate is used by signature of <see cref="ProvideSSLStream"/> in order to select one local certificate to use from possibly many local certificates.
   /// The signature is just a copy of <see cref="T:System.Net.Security.LocalCertificateSelectionCallback"/> which is not available on all platforms.
   /// </summary>
   /// <param name="sender">The sender.</param>
   /// <param name="targetHost">The host server specified by client.</param>
   /// <param name="localCertificates">Local certificates used.</param>
   /// <param name="remoteCertificate">Remote certificate of PostgreSQL backend.</param>
   /// <param name="acceptableIssuers">Certificate issuers acceptable to PostgreSQL backend.</param>
   /// <returns>A certificate from <paramref name="localCertificates"/> collection that should be used in SSL connection.</returns>
   public delegate System.Security.Cryptography.X509Certificates.X509Certificate LocalCertificateSelectionCallback(
      Object sender,
      String targetHost,
      System.Security.Cryptography.X509Certificates.X509CertificateCollection localCertificates,
      System.Security.Cryptography.X509Certificates.X509Certificate remoteCertificate,
      String[] acceptableIssuers
      );

   /// <summary>
   /// This delegate is used by <see cref="ProvideSSLStream"/> delegate in its signature.
   /// The signature captures the one of <see cref="M:System.Net.Security.SslStream.AuthenticateAsClientAsync(System.String,System.Security.Cryptography.X509Certificates.X509CertificateCollection,System.Security.Authentication.SslProtocols,System.Boolean)"/> method.
   /// </summary>
   /// <param name="stream">The stream provided by <see cref="ProvideSSLStream"/> callback.</param>
   /// <param name="targetHost">The host server sepcified by client.</param>
   /// <param name="clientCertificates">The client certificates.</param>
   /// <param name="enabledSslProtocols">The <see cref="System.Security.Authentication.SslProtocols"/> to use in SSL connection.</param>
   /// <param name="checkCertificateRevocation">Whether to check that certificate has been revoked.</param>
   /// <returns>A task which is completed once authentication is completed.</returns>
   public delegate Task AuthenticateAsClientAsync(
      Stream stream,
      String targetHost,
      System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates,
      System.Security.Authentication.SslProtocols enabledSslProtocols,
      Boolean checkCertificateRevocation
      );

#if NET40

   internal struct AuthenticateAsyncState
   {
      public AuthenticateAsyncState(
         System.Net.Security.SslStream stream,
         String targetHost,
         System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates,
         System.Security.Authentication.SslProtocols enabledSslProtocols,
         Boolean checkCertificateRevocation
         )
      {
         this.Stream = stream;
         this.TargetHost = targetHost;
         this.ClientCertificates = clientCertificates;
         this.EnabledSslProtocols = enabledSslProtocols;
         this.CheckCertificateRevocation = checkCertificateRevocation;
      }

      public System.Net.Security.SslStream Stream { get; }

      public String TargetHost { get; }

      public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get; }
      public System.Security.Authentication.SslProtocols EnabledSslProtocols { get; }
      public Boolean CheckCertificateRevocation { get; }
   }

   /// <summary>
   /// This class contains extension methods for types defined in other assemblies.
   /// </summary>
   public static class UtilPackExtensions
   {
      /// <todo />
      public static Task AuthenticateAsClientAsync(
         this System.Net.Security.SslStream stream,
         String targetHost,
         System.Security.Cryptography.X509Certificates.X509CertificateCollection clientCertificates,
         System.Security.Authentication.SslProtocols enabledSslProtocols,
         Boolean checkCertificateRevocation
         )
      {
         var authArgs = new AuthenticateAsyncState( stream, targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation );
         return Task.Factory.FromAsync(
            ( aArgs, cb, state ) => aArgs.Stream.BeginAuthenticateAsClient( aArgs.TargetHost, aArgs.ClientCertificates, aArgs.EnabledSslProtocols, aArgs.CheckCertificateRevocation, cb, state ),
            result => ( (System.Net.Security.SslStream) result.AsyncState ).EndAuthenticateAsClient( result ),
            authArgs,
            stream
            );
      }
   }
#endif

#endif
}
