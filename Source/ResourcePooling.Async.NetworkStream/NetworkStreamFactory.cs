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
using ResourcePooling.Async.NetworkStream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using static UtilPack.Configuration.NetworkStream.ConnectionSSLMode;
//#if NETSTANDARD1_3
//   UtilPack.Configuration.NetworkStream
//#else
//   System.Net.Security
//#endif
//   .LocalCertificateSelectionCallback;

using TAuthenticateAsClientAsync = UtilPack.Configuration.NetworkStream.AuthenticateAsClientAsync;
//#if NETSTANDARD1_3
//   UtilPack.Configuration.NetworkStream
//#else
//   System.Net.Security
//#endif
//   .RemoteCertificateValidationCallback;

using TLocalCertificateSelectionCallback = UtilPack.Configuration.NetworkStream.LocalCertificateSelectionCallback;
using TNetworkStream = System.Net.Sockets.NetworkStream;
using TProvideSSLStream = UtilPack.Configuration.NetworkStream.ProvideSSLStream;
using TRemoteCertificateValidationCallback = UtilPack.Configuration.NetworkStream.RemoteCertificateValidationCallback;

namespace ResourcePooling.Async.NetworkStream
{
   /// <summary>
   /// This is abstract base class for <see cref="NetworkStreamFactory"/> and <see cref="NetworkStreamFactory{TState}"/>.
   /// </summary>
   /// <typeparam name="TConfiguration">The type of configuration. Typically <see cref="NetworkStreamFactoryConfiguration"/> or <see cref="NetworkStreamFactoryConfiguration{TState}"/>.</typeparam>
   public abstract class AbstractNetworkStreamFactory<TConfiguration> : DefaultAsyncResourceFactory<Stream, TConfiguration>
   {
      /// <summary>
      /// Initializes a new instance of <see cref="AbstractNetworkStreamFactory{TConfiguration}"/> with given callback to create <see cref="Socket"/> and <see cref="Stream"/> from given configuration.
      /// </summary>
      /// <param name="factory">The callback to create <see cref="Socket"/> and <see cref="Stream"/> from given configuration.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="factory"/> is <c>null</c>.</exception>
      public AbstractNetworkStreamFactory(
         Func<TConfiguration, CancellationToken, ValueTask<(Socket, Stream)>> factory
         ) : base( CreateCallback( factory ) )
      {

      }

      private static Func<TConfiguration, AsyncResourceFactory<Stream>> CreateCallback(
         Func<TConfiguration, CancellationToken, ValueTask<(Socket, Stream)>> factory
         )
      {
         ArgumentValidator.ValidateNotNull( nameof( factory ), factory );
         return config => new StatelessBoundAsyncResourceFactory<Stream, TConfiguration>( config, async ( config2, token ) => new StreamAcquireInfo( await factory( config2, token ) ) );
      }

      private sealed class StreamAcquireInfo : AsyncResourceAcquireInfoImpl<Stream, Socket>
      {
         public StreamAcquireInfo(
            (Socket Socket, Stream Stream) tuple
            ) : base( ArgumentValidator.ValidateNotNull( nameof( tuple.Stream ), tuple.Stream ), ArgumentValidator.ValidateNotNull( nameof( tuple.Socket ), tuple.Socket ), ( s, t ) => { }, s => { } )
         {
         }

         protected override void Dispose( Boolean disposing )
         {
            if ( disposing )
            {
               this.PublicResource.Dispose();
            }
         }

         protected override Task DisposeBeforeClosingChannel( CancellationToken token )
         {
            return null;
         }

         protected override Boolean PublicResourceCanBeReturnedToPool()
         {
            return this.Channel.Connected;
         }
      }
   }

   /// <summary>
   /// This class extends <see cref="AbstractNetworkStreamFactory{TConfiguration}"/> and provides stateless functionality (all required information in configuration parameters) to create connection to remote endpoint.
   /// </summary>
   /// <seealso cref="NetworkStreamFactoryConfiguration"/>
   public class NetworkStreamFactory : AbstractNetworkStreamFactory<NetworkStreamFactoryConfiguration>
   {
      /// <summary>
      /// Gets the static stateless instance of <see cref="NetworkStreamFactory"/>.
      /// </summary>
      /// <value>The static stateless instance of <see cref="NetworkStreamFactory"/>.</value>
      public static NetworkStreamFactory Instance { get; } = new NetworkStreamFactory();

      /// <summary>
      /// Creates a new instance of <see cref="NetworkStreamFactory"/>.
      /// </summary>
      protected NetworkStreamFactory()
         : base( async ( config, token ) =>
          {
             var tuple = await NetworkStreamFactory<Object>.AcquireNetworkStreamFromConfiguration(
                config.AsStateful<Object>(),
                token
                );
             return (tuple.Item1, tuple.Item2);
          } )
      {

      }

      ///// <summary>
      ///// Implements <see cref="AbstractNetworkStreamFactory{TConfiguration}.AcquireStreamAndSocket"/> by connecting to remote endpoint using only information in <see cref="NetworkStreamFactoryConfiguration"/>.
      ///// </summary>
      ///// <param name="parameters">The <see cref="NetworkStreamFactoryConfiguration"/> to use.</param>
      ///// <param name="token">The cancellation token to use.</param>
      ///// <returns>Acquired socket and stream.</returns>
      ///// <seealso cref="NetworkStreamFactoryConfiguration"/>
      //protected override async ValueTask<(Socket, Stream)> AcquireStreamAndSocket( NetworkStreamFactoryConfiguration parameters, CancellationToken token )
      //{
      //   return await AcquireNetworkStreamFromConfiguration( parameters, token );
      //}

      /// <summary>
      /// Acquires the <see cref="Socket"/>, <see cref="Stream"/> when given <see cref="NetworkStreamFactoryConfiguration"/> and <see cref="CancellationToken"/>.
      /// </summary>
      /// <param name="parameters">The <see cref="NetworkStreamFactoryConfiguration"/> to use.</param>
      /// <param name="token">The cancellation token to use.</param>
      /// <returns>Asynchronously acquired <see cref="Socket"/> and <see cref="Stream"/>.</returns>
      /// <remarks>
      /// This method will take care of creating and initializing SSL stream, if <paramref name="parameters"/> so requires.
      /// That is why the stream component of returned tuple is <see cref="Stream"/> instead of <see cref="NetworkStream"/>.
      /// </remarks>
      /// <seealso cref="NetworkStreamFactoryConfiguration"/>
      public static async Task<(Socket, Stream)> AcquireNetworkStreamFromConfiguration(
         NetworkStreamFactoryConfiguration parameters,
         CancellationToken token
         )
      {
         var tuple = await NetworkStreamFactory<Object>.AcquireNetworkStreamFromConfiguration(
            parameters.AsStateful<Object>(),
            token
            );
         return (tuple.Item1, tuple.Item2);
      }
   }

   /// <summary>
   /// This class extends <see cref="AbstractNetworkStreamFactory{TConfiguration}"/> and provides stateful functionality (a custom state is created, which is then given to other callbacks of <see cref="NetworkStreamFactoryConfiguration{TState}"/>) by connecting to remote endpoint using only information in <see cref="NetworkStreamFactoryConfiguration{TState}"/>.
   /// </summary>
   /// <typeparam name="TState">The type of state created by <see cref="NetworkStreamFactoryConfiguration{TState}.CreateState"/> callback.</typeparam>
   public class NetworkStreamFactory<TState> : AbstractNetworkStreamFactory<NetworkStreamFactoryConfiguration<TState>>
   {
      /// <summary>
      /// Gets the static stateless instance of <see cref="NetworkStreamFactory{TState}"/>.
      /// </summary>
      /// <value>The static stateless instance of <see cref="NetworkStreamFactory{TState}"/>.</value>
      public static NetworkStreamFactory<TState> Instance { get; } = new NetworkStreamFactory<TState>();

      /// <summary>
      /// Creates a new instance of <see cref="NetworkStreamFactory{TState}"/>.
      /// </summary>
      protected NetworkStreamFactory()
         : base( async ( config, token ) =>
          {
             var tuple = await AcquireNetworkStreamFromConfiguration( config, token );
             return (tuple.Item1, tuple.Item2);
          } )
      {

      }

      /// <summary>
      /// This class extends <see cref="EndPoint"/> in order to provide functionality specific for Unix domain sockets.
      /// </summary>
      public sealed class UnixEndPoint : EndPoint
      {
         private static readonly Encoding _Encoding = new UTF8Encoding( false, false );

         private String _filePath;

         /// <summary>
         /// Creates a new <see cref="UnixEndPoint"/> with given file path as Unix domain socket.
         /// </summary>
         /// <param name="filePath">The file path where the server socket resides.</param>
         /// <remarks>
         /// Empty file path will be used if <paramref name="filePath"/> is <c>null</c>.
         /// </remarks>
         public UnixEndPoint( String filePath )
         {
            this.FilePath = filePath;
         }

         /// <summary>
         /// Gets or sets the file path of the Unix domain socket.
         /// </summary>
         /// <value>The file path of the Unix domain socket.</value>
         public String FilePath
         {
            get
            {
               return this._filePath;
            }
            set
            {
               this._filePath = value ?? String.Empty;
            }
         }

         /// <summary>
         /// Returns <see cref="AddressFamily.Unix"/>.
         /// </summary>
         /// <value>The <see cref="AddressFamily.Unix"/>.</value>
         public override AddressFamily AddressFamily => AddressFamily.Unix;

         /// <summary>
         /// Creates a new <see cref="UnixEndPoint"/> from serialized <see cref="SocketAddress"/>.
         /// </summary>
         /// <param name="socketAddress">A serialized <see cref="SocketAddress"/>.</param>
         /// <returns>A deserialized <see cref="UnixEndPoint"/>.</returns>
         /// <exception cref="ArgumentException">If given <paramref name="socketAddress"/> does not represent address family of <see cref="System.Net.Sockets.AddressFamily.Unix"/>.</exception>
         public override EndPoint Create( SocketAddress socketAddress )
         {
            var serializedAddressFamily = (AddressFamily) ( socketAddress[0] | ( socketAddress[1] << 8 ) );
            if ( serializedAddressFamily != AddressFamily.Unix )
            {
               throw new ArgumentException( "Given socket address is not a Unix socket address." );
            }

            var socketSize = socketAddress.Size;
            String fileName;
            if ( socketSize <= 2 )
            {
               fileName = String.Empty;
            }
            else
            {
               //There may be junk after null terminator so we need to examine every byte
               var i = 0;
               var max = socketAddress.Size - 2;
               var bytes = new Byte[max];
               Byte b;
               while ( i < max && ( b = socketAddress[i] ) != 0 )
               {
                  bytes[i++] = b;
               }
               fileName = _Encoding.GetString( bytes, 0, i );
            }

            return new UnixEndPoint( fileName );
         }

         /// <summary>
         /// Serializes this instance into <see cref="SocketAddress"/>.
         /// </summary>
         /// <returns>A serialized <see cref="SocketAddress"/>.</returns>
         public override SocketAddress Serialize()
         {
            // We must allocate new array and then copy manually, as there is no copy method provided for socket address...
            var bytes = _Encoding.GetBytes( this.FilePath );
            // 2 extra bytes for address family, 1 for null terminator
            var address = new SocketAddress( AddressFamily.Unix, bytes.Length + 3 );
            // Address family in first bytes, set by SocketAddress constructor
            // No copying into socket address API, so iterate byte by byte
            for ( var i = 0; i < bytes.Length; ++i )
            {
               address[i + 2] = bytes[i];
            }

            // Explicitly set null terminator in case socket address got some junk in its array
            address[bytes.Length + 2] = 0;
            return address;
         }

         /// <summary>
         /// Returns <see cref="FilePath"/>.
         /// </summary>
         /// <returns>The value of <see cref="FilePath"/>.</returns>
         public override String ToString() => this.FilePath;

         /// <summary>
         /// Gets the hash code of this <see cref="UnixEndPoint"/>.
         /// </summary>
         /// <returns></returns>
         public override Int32 GetHashCode() => this.FilePath?.GetHashCode() ?? 0;

         /// <summary>
         /// Checks that given object is <see cref="UnixEndPoint"/> and that it equals to this <see cref="UnixEndPoint"/>.
         /// </summary>
         /// <param name="obj">The object to check.</param>
         /// <returns><c>true</c> if <paramref name="obj"/> is <see cref="UnixEndPoint"/> and equals to this <see cref="UnixEndPoint"/>; <c>false</c> otherwise.</returns>
         public override Boolean Equals( Object obj ) => obj is UnixEndPoint other && String.Equals( this.FilePath, other.FilePath );
      }

      /// <summary>
      /// Acquires the <see cref="Socket"/>, <see cref="Stream"/> and <typeparamref name="TState"/> given <see cref="NetworkStreamFactoryConfiguration{TState}"/> and <see cref="CancellationToken"/>.
      /// </summary>
      /// <param name="parameters">The <see cref="NetworkStreamFactoryConfiguration{TState}"/> to use.</param>
      /// <param name="token">The cancellation token to use.</param>
      /// <returns>Asynchronously acquired <see cref="Socket"/> and <see cref="Stream"/>, and <typeparamref name="TState"/> returned by <see cref="NetworkStreamFactoryConfiguration{TState}.CreateState"/>.</returns>
      /// <remarks>
      /// This method will take care of creating and initializing SSL stream, if <paramref name="parameters"/> so requires.
      /// That is why the stream component of returned tuple is <see cref="Stream"/> instead of <see cref="NetworkStream"/>.
      /// </remarks>
      /// <seealso cref="NetworkStreamFactoryConfiguration{TState}"/>
      public static async Task<(Socket, Stream, TState)> AcquireNetworkStreamFromConfiguration(
         NetworkStreamFactoryConfiguration<TState> parameters,
         CancellationToken token
         )
      {
         EndPoint remoteEP;
         EndPoint localEP;
         SocketType socketType;
         ProtocolType protocolType;
         var remoteAddressGetter = parameters.RemoteAddress;
         if ( remoteAddressGetter == null )
         {
            remoteEP = new UnixEndPoint( parameters.GetUnixSocketFilePath?.Invoke() ?? throw new ArgumentException( "No remote IP address getter nor Unix socket filename specified." ) );
            localEP = null;
            socketType = SocketType.Stream;
            protocolType = ProtocolType.IP;
         }
         else
         {
            var remoteAddress = await ArgumentValidator.ValidateNotNull( nameof( parameters.RemoteAddress ), parameters.RemoteAddress )( token );
            var remoteIPEP = new IPEndPoint( remoteAddress, ArgumentValidator.ValidateNotNull( nameof( parameters.RemotePort ), parameters.RemotePort )( remoteAddress ) );
            remoteEP = remoteIPEP;
            localEP = parameters.SelectLocalIPEndPoint?.Invoke( remoteIPEP );
            socketType = parameters.SocketType;
            protocolType = parameters.ProtocolType;
         }

         Socket CreateSocket()
         {
            return new Socket( remoteEP.AddressFamily, socketType, protocolType );
         }

         async Task<TNetworkStream> InitNetworkStream( Socket thisSocket )
         {
            if ( localEP != null )
            {
               thisSocket.Bind( localEP );
            }

            await thisSocket.ConnectAsync( remoteEP );
            return new TNetworkStream( thisSocket, true );
         }

         var socket = CreateSocket();
         var errorOccurred = false;
         Stream stream = null;
         TState state = default;
         try
         {
            var nwStream = await InitNetworkStream( socket );
            var stateInit = parameters.CreateState;
            if ( stateInit != null )
            {
               state = stateInit( socket, nwStream, token );
            }
            stream = nwStream;
            var remoteCanSSL = await ( parameters.IsSSLPossible?.Invoke( state ) ?? TaskUtils.False );
            var connectionMode = parameters.ConnectionSSLMode?.Invoke( state ) ?? NotRequired;
            var isSSLRequired = connectionMode == Required;
            var isSSLPreferred = connectionMode == Preferred;
            if ( remoteCanSSL && ( isSSLRequired || isSSLPreferred ) )
            {
               // Start SSL session
               Stream sslStream = null;
               try
               {
                  var provideSSLStream = parameters.ProvideSSLStream;
                  if ( provideSSLStream != null )
                  {
                     var clientCerts = new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
                     parameters.ProvideClientCertificates?.Invoke( clientCerts );
                     sslStream = provideSSLStream( stream, false, parameters.ValidateServerCertificate, parameters.SelectLocalCertificate, out var authenticateAsClient );
                     if ( isSSLRequired )
                     {
                        if ( sslStream == null )
                        {
                           throw parameters.SSLStreamProviderNoStream?.Invoke() ?? new InvalidOperationException( "SSL stream creation callback returned null." );
                        }
                        else if ( authenticateAsClient == null )
                        {
                           throw parameters.SSLStreamProviderNoAuthenticationCallback?.Invoke() ?? new InvalidOperationException( "Authentication callback given by SSL stream creation callback was null." );
                        }
                     }

                     if ( sslStream != null && authenticateAsClient != null )
                     {
                        await authenticateAsClient(
                           sslStream,
                           parameters.ProvideSSLHost?.Invoke( state ) ?? throw new ArgumentException( "Please specify remote host in connection configuration." ),
                           clientCerts,
                           parameters.GetSSLProtocols?.Invoke( state ) ?? AbstractNetworkStreamFactoryConfiguration.DEFAULT_SSL_PROTOCOLS,
                           true
                           );
                        stream = sslStream;
                     }
                  }
                  else if ( isSSLRequired )
                  {
                     throw parameters.NoSSLStreamProvider() ?? new InvalidOperationException( "Creation parameters did not have callback to create SSL stream." );
                  }
               }
               catch ( Exception exc )
               {
                  if ( !isSSLRequired )
                  {
                     // We close SSL stream in case authentication failed.
                     // Closing SSL stream will close underlying stream, which will close the socket...
                     // So we have to reconnect afterwards.
                     ( sslStream ?? stream ).DisposeSafely();
                     // ... so re-create it
                     socket = CreateSocket();
                     stream = await InitNetworkStream( socket );
                  }
                  else
                  {
                     var otherExc = parameters.SSLStreamOtherError?.Invoke( exc );
                     if ( otherExc == null )
                     {
                        throw;
                     }
                     else
                     {
                        throw otherExc;
                     }
                  }
               }
            }
            else if ( !remoteCanSSL && ( connectionMode == NotRequired || isSSLPreferred ) )
            {
               // Nothing to do - continue normally
            }
            else
            {
               // SSL session start was unsuccessful, and it is required -> can not continue
               throw parameters.RemoteNoSSLSupport?.Invoke() ?? new InvalidOperationException( "Remote does not support SSL." );
            }

         }
         catch
         {
            errorOccurred = true;
            throw;
         }
         finally
         {
            if ( errorOccurred )
            {
               stream.DisposeSafely();
            }
         }

         if ( stream == null )
         {
            throw new InvalidOperationException( "Unable to acquire stream." );
         }

         var transformer = parameters.TransformStreamAfterCreation;
         if ( transformer != null )
         {
            var newStream = transformer( stream );
            if ( newStream != null )
            {
               stream = newStream;
            }
         }

         return (socket, stream, state);
      }
   }

   /// <summary>
   /// This is base class for <see cref="NetworkStreamFactoryConfiguration"/> and <see cref="NetworkStreamFactoryConfiguration{TState}"/> containing properties which are common for both.
   /// </summary>
   /// <seealso cref="E_UtilPack.CreateNetworkStreamFactoryConfiguration"/>
   /// <seealso cref="E_UtilPack.CreateStatefulNetworkStreamFactoryConfiguration"/>
   public abstract class AbstractNetworkStreamFactoryConfiguration
   {

      internal const System.Security.Authentication.SslProtocols DEFAULT_SSL_PROTOCOLS = System.Security.Authentication.SslProtocols.
#if NET40
         Tls
#else
         Tls12
#endif
         ;

      /// <summary>
      /// Initializes new instance of <see cref="AbstractNetworkStreamFactoryConfiguration"/>.
      /// </summary>
      /// <remarks>
      /// In .NET Core App 1.1+ and .NET Desktop 4.0+ this constructor sets the value for <see cref="ProvideSSLStream"/> callback.
      /// </remarks>
      public AbstractNetworkStreamFactoryConfiguration()
      {
         this.SocketType = SocketType.Stream;
         this.ProtocolType = ProtocolType.Tcp;
#if !NETSTANDARD1_3
         this.ProvideSSLStream = (
            Stream innerStream,
            Boolean leaveInnerStreamOpen,
            TRemoteCertificateValidationCallback userCertificateValidationCallback,
            TLocalCertificateSelectionCallback userCertificateSelectionCallback,
            out TAuthenticateAsClientAsync authenticateAsClientAsync
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
      /// Gets or sets the callback to get file path of unix domain socket. Note that <see cref="RemoteAddress"/> takes precedence over this property, if both are specified.
      /// </summary>
      /// <value>The callback to get file path of unix domain socket.</value>
      public Func<String> GetUnixSocketFilePath { get; set; }

      /// <summary>
      /// Gets or sets the callback to potentially asynchronously get remote <see cref="IPAddress"/>.
      /// This property is not optional - if <c>null</c>, an exception will be thrown.
      /// </summary>
      /// <value>The callback to potentially asynchronously get remote <see cref="IPAddress"/>.</value>
      /// <remarks>
      /// The first parameter is the cancellation token. The callback should potentially asynchronously return the <see cref="IPAddress"/> that the socket will get connected to.
      /// </remarks>
      public Func<CancellationToken, ValueTask<IPAddress>> RemoteAddress { get; set; }

      /// <summary>
      /// Gets or sets the callback to get port for given remote <see cref="IPAddress"/>.
      /// This property is not optional - if <c>null</c>, an exception will be thrown.
      /// </summary>
      /// <value>The callback to get port for given remote <see cref="IPAddress"/>.</value>
      /// <remarks>
      /// The first parameter is the address returned by <see cref="RemoteAddress"/>. The callback should return the port to use, as an integer.
      /// </remarks>
      public Func<IPAddress, Int32> RemotePort { get; set; }

      /// <summary>
      /// Gets or sets the <see cref="System.Net.Sockets.SocketType"/> for socket.
      /// </summary>
      /// <value>The <see cref="System.Net.Sockets.SocketType"/> for socket.</value>
      public SocketType SocketType { get; set; }

      /// <summary>
      /// Gets or sets the <see cref="System.Net.Sockets.ProtocolType"/> for socket.
      /// </summary>
      /// <value>The <see cref="System.Net.Sockets.ProtocolType"/> for socket.</value>
      public ProtocolType ProtocolType { get; set; }

      /// <summary>
      /// Gets or sets the callback to select local <see cref="IPEndPoint"/> to use when connecting to remote resource.
      /// This property is optional - if <c>null</c>, any available local <see cref="IPEndPoint"/> will be used.
      /// </summary>
      /// <value>The callback to select local <see cref="IPEndPoint"/> to use when connecting to remote resource.</value>
      /// <remarks>
      /// The first parameter is the remote <see cref="IPEndPoint"/>, as constructed from return values of <see cref="RemoteAddress"/> and <see cref="RemotePort"/> callbacks. The callback should return local <see cref="IPEndPoint"/> to bind socket to, or <c>null</c> if any available local <see cref="IPEndPoint"/> is ok.
      /// </remarks>
      public Func<IPEndPoint, IPEndPoint> SelectLocalIPEndPoint { get; set; }

      /// <summary>
      /// This event is used to add client certificates to <see cref="System.Security.Cryptography.X509Certificates.X509CertificateCollection"/> when using SSL to connect to the backend.
      /// </summary>
      public event Action<System.Security.Cryptography.X509Certificates.X509CertificateCollection> ProvideClientCertificatesEvent;
      internal Action<System.Security.Cryptography.X509Certificates.X509CertificateCollection> ProvideClientCertificates => this.ProvideClientCertificatesEvent;

      /// <summary>
      /// This callback is used to create SSL stream when using SSL to connect to remote endpoint.
      /// </summary>
      /// <value>The callback to create SSL stream when using SSL to connect to remote endpoint.</value>
      /// <remarks>
      /// In .NET Core App 1.1+ and .NET Desktop 4.0+ environments this will be set to default by the constructor.
      /// </remarks>
      public TProvideSSLStream ProvideSSLStream { get; set; }

      /// <summary>
      /// This callback will be used to validate server certificate when using SSL to connect to the remote endpoint.
      /// </summary>
      /// <value>The callback will to validate server certificate when using SSL to connect to the remote endpoint.</value>
      /// <remarks>
      /// When not specified (i.e. left to <c>null</c>), server certificate (if provided) will always be accepted.
      /// </remarks>
      public TRemoteCertificateValidationCallback ValidateServerCertificate { get; set; }

      /// <summary>
      /// This callback will be used to select local certificate when using SSL to connect to the remote endpoint.
      /// </summary>
      /// <value>The callback to select local certificate when using SSL to connect to the remote endpoint.</value>
      /// <remarks>
      /// When not specified (i.e. left to <c>null</c>), the first of the certificates is selected, if any.
      /// </remarks>
      public TLocalCertificateSelectionCallback SelectLocalCertificate { get; set; }


      /// <summary>
      /// This is callback to create an <see cref="Exception"/> when <see cref="NetworkStreamFactoryConfiguration.IsSSLPossible"/>/<see cref="NetworkStreamFactoryConfiguration{TState}.IsSSLPossible"/> callback returns <c>false</c>, but the <see cref="NetworkStreamFactoryConfiguration.ConnectionSSLMode"/>/<see cref="NetworkStreamFactoryConfiguration{TState}.ConnectionSSLMode"/> returned <see cref="ConnectionSSLMode.Required"/>.
      /// </summary>
      /// <value>The callback to create an <see cref="Exception"/> when SSL is required, but remote endpoint does not support it.</value>
      /// <remarks>
      /// If this callback is needed, and it is <c>null</c> or returns <c>null</c>, an <see cref="InvalidOperationException"/> is thrown.
      /// </remarks>
      public Func<Exception> RemoteNoSSLSupport { get; set; }

      /// <summary>
      /// This is callback to create an <see cref="Exception"/> when the <see cref="NetworkStreamFactoryConfiguration.ConnectionSSLMode"/>/<see cref="NetworkStreamFactoryConfiguration{TState}.ConnectionSSLMode"/> returned <see cref="ConnectionSSLMode.Required"/>, but <see cref="ProvideSSLStream"/> callback is <c>null</c>.
      /// </summary>
      /// <value>The callback to create an <see cref="Exception"/> when SSL is required, but the <see cref="ProvideSSLStream"/> callback to create SSL stream was not supplied.</value>
      /// <remarks>
      /// If this callback is needed, and it is <c>null</c> or returns <c>null</c>, an <see cref="InvalidOperationException"/> is thrown.
      /// </remarks>
      public Func<Exception> NoSSLStreamProvider { get; set; }

      /// <summary>
      /// This is callback to create an <see cref="Exception"/> when the <see cref="NetworkStreamFactoryConfiguration.ConnectionSSLMode"/>/<see cref="NetworkStreamFactoryConfiguration{TState}.ConnectionSSLMode"/> returned <see cref="ConnectionSSLMode.Required"/>, but the <see cref="ProvideSSLStream"/> callback returned <c>null</c>.
      /// </summary>
      /// <value>The callback to create an <see cref="Exception"/> when <see cref="ProvideSSLStream"/> returns <c>null</c>.</value>
      /// <remarks>
      /// If this callback is needed, and it is <c>null</c> or returns <c>null</c>, an <see cref="InvalidOperationException"/> is thrown.
      /// </remarks>
      public Func<Exception> SSLStreamProviderNoStream { get; set; }

      /// <summary>
      /// This is callback to create an <see cref="Exception"/> when the <see cref="NetworkStreamFactoryConfiguration.ConnectionSSLMode"/>/<see cref="NetworkStreamFactoryConfiguration{TState}.ConnectionSSLMode"/> returned <see cref="ConnectionSSLMode.Required"/>, but the <see cref="ProvideSSLStream"/> callback set <c>null</c> to its <see cref="AuthenticateAsClientAsync"/> parameter.
      /// </summary>
      /// <value>The callback to create an <see cref="Exception"/> when <see cref="ProvideSSLStream"/> does not set its <see cref="AuthenticateAsClientAsync"/> parameter.</value>
      /// <remarks>
      /// If this callback is needed, and it is <c>null</c> or returns <c>null</c>, an <see cref="InvalidOperationException"/> is thrown.
      /// </remarks>
      public Func<Exception> SSLStreamProviderNoAuthenticationCallback { get; set; }

      /// <summary>
      /// This is callback to create an <see cref="Exception"/> when the <see cref="NetworkStreamFactoryConfiguration.ConnectionSSLMode"/>/<see cref="NetworkStreamFactoryConfiguration{TState}.ConnectionSSLMode"/> returned <see cref="ConnectionSSLMode.Required"/>, but something throws an exception when preparing and authenticating SSL stream.
      /// </summary>
      /// <value>The callback to create an <see cref="Exception"/> when something throws an exception when preparing and authenticating SSL stream.</value>
      /// <remarks>
      /// The first parameter is the catched exception. If this callback is needed, and it is <c>null</c> or returns <c>null</c>, the exception is re-thrown.
      /// </remarks>
      public Func<Exception, Exception> SSLStreamOtherError { get; set; }

      /// <summary>
      /// Gets or sets optional callback to transform a stream into another after successful stream creation and initialization (including creating SSL stream).
      /// </summary>
      /// <value>Optional callback to transform a stream into another after successful stream creation and initialization (including creating SSL stream).</value>
      /// <remarks>
      /// This callback is useful to e.g. use <see cref="DuplexBufferedAsyncStream"/>.
      /// </remarks>
      public Func<Stream, Stream> TransformStreamAfterCreation { get; set; }
   }

   /// <summary>
   /// This class extends <see cref="AbstractNetworkStreamFactoryConfiguration"/> to provide required callbacks for stateless network stream initialization.
   /// </summary>
   /// <seealso cref="E_UtilPack.CreateNetworkStreamFactoryConfiguration"/>
   /// <seealso cref="E_UtilPack.CreateStatefulNetworkStreamFactoryConfiguration"/>
   public class NetworkStreamFactoryConfiguration : AbstractNetworkStreamFactoryConfiguration
   {
      /// <summary>
      /// Gets or sets the callback to get <see cref="Configuration.NetworkStream.ConnectionSSLMode"/>.
      /// </summary>
      /// <value>The callback to get <see cref="Configuration.NetworkStream.ConnectionSSLMode"/>.</value>
      /// <remarks>
      /// If this callback is <c>null</c>, the <see cref="NetworkStreamFactory"/> will default to <see cref="Configuration.NetworkStream.ConnectionSSLMode.NotRequired"/>.
      /// </remarks>
      public Func<UtilPack.Configuration.NetworkStream.ConnectionSSLMode> ConnectionSSLMode { get; set; }

      /// <summary>
      /// Gets or sets the callback to check whether SSL is possible.
      /// </summary>
      /// <value>The callback to check whether SSL is possible.</value>
      /// <remarks>
      /// If this callback is <c>null</c>, the <see cref="NetworkStreamFactory"/> will assume SSL is not possible.
      /// </remarks>
      public Func<Task<Boolean>> IsSSLPossible { get; set; }

      /// <summary>
      /// Gets or sets the callback to get <see cref="System.Security.Authentication.SslProtocols"/> when authenticating as client over SSL stream.
      /// </summary>
      /// <value>The callback to get <see cref="System.Security.Authentication.SslProtocols"/> when authenticating as client over SSL stream.</value>
      /// <remarks>
      /// If this callback is <c>null</c>, the <see cref="NetworkStreamFactory"/> will use default value, which varies based on platform.
      /// </remarks>
      public Func<System.Security.Authentication.SslProtocols> GetSSLProtocols { get; set; }

      /// <summary>
      /// Gets or sets the callback to get host name when authenticating as client over SSL stream.
      /// </summary>
      /// <value>The callback to get host name when authenticating as client over SSL stream.</value>
      /// <remarks>
      /// If this callback is <c>null</c>, the <see cref="NetworkStreamFactory"/> will throw <see cref="ArgumentException"/>.
      /// </remarks>
      public Func<String> ProvideSSLHost { get; set; }
   }

   /// <summary>
   /// This class extends <see cref="AbstractNetworkStreamFactoryConfiguration"/> to provide required callbacks for stateful network stream initialization.
   /// </summary>
   /// <typeparam name="TState">The type of the intermediate state that is passed to callbacks.</typeparam>
   /// <seealso cref="E_UtilPack.CreateNetworkStreamFactoryConfiguration"/>
   /// <seealso cref="E_UtilPack.CreateStatefulNetworkStreamFactoryConfiguration"/>
   public class NetworkStreamFactoryConfiguration<TState> : AbstractNetworkStreamFactoryConfiguration
   {
      /// <summary>
      /// Gets or sets the callback used to create a state object after socket and stream have been initialized.
      /// </summary>
      /// <value>The callback used to create a state object after socket and stream have been initialized.</value>
      /// <remarks>
      /// The callback receives <see cref="Socket"/>, <see cref="TNetworkStream"/>, and <see cref="CancellationToken"/>, and it is assumed that these are available in returned <typeparamref name="TState"/>.
      /// </remarks>
      public Func<Socket, TNetworkStream, CancellationToken, TState> CreateState { get; set; }

      /// <summary>
      /// Gets or sets the callback to get <see cref="Configuration.NetworkStream.ConnectionSSLMode"/>.
      /// </summary>
      /// <value>The callback to get <see cref="Configuration.NetworkStream.ConnectionSSLMode"/>.</value>
      /// <remarks>
      /// If this callback is <c>null</c>, the <see cref="NetworkStreamFactory{TState}"/> will default to <see cref="Configuration.NetworkStream.ConnectionSSLMode.NotRequired"/>.
      /// </remarks>
      public Func<TState, UtilPack.Configuration.NetworkStream.ConnectionSSLMode> ConnectionSSLMode { get; set; }

      /// <summary>
      /// Gets or sets the callback to potentially asynchronously check whether SSL is possible.
      /// </summary>
      /// <value>The callback to check whether SSL is possible.</value>
      /// <remarks>
      /// If this callback is <c>null</c>, the <see cref="NetworkStreamFactory{TState}"/> will assume SSL is not possible.
      /// </remarks>
      public Func<TState, Task<Boolean>> IsSSLPossible { get; set; }

      /// <summary>
      /// Gets or sets the callback to get <see cref="System.Security.Authentication.SslProtocols"/> when authenticating as client over SSL stream.
      /// </summary>
      /// <value>The callback to get <see cref="System.Security.Authentication.SslProtocols"/> when authenticating as client over SSL stream.</value>
      /// <remarks>
      /// If this callback is <c>null</c>, the <see cref="NetworkStreamFactory{TState}"/> will use default value, which varies based on platform.
      /// </remarks>
      public Func<TState, System.Security.Authentication.SslProtocols> GetSSLProtocols { get; set; }

      /// <summary>
      /// Gets or sets the callback to get host name when authenticating as client over SSL stream.
      /// </summary>
      /// <value>The callback to get host name when authenticating as client over SSL stream.</value>
      /// <remarks>
      /// If this callback is <c>null</c>, the <see cref="NetworkStreamFactory{TState}"/> will throw <see cref="ArgumentException"/>.
      /// </remarks>
      public Func<TState, String> ProvideSSLHost { get; set; }
   }
}

/// <summary>
/// This class contains extensions methods for types defined in this assembly.
/// </summary>
public static partial class E_ResourcePooling
{
   /// <summary>
   /// Helper method to transform this stateless <see cref="NetworkStreamFactoryConfiguration"/> into stateful <see cref="NetworkStreamFactoryConfiguration{TState}"/>.
   /// </summary>
   /// <typeparam name="TState">The type of state.</typeparam>
   /// <param name="configuration">This <see cref="NetworkStreamFactoryConfiguration"/>.</param>
   /// <param name="createState">The optional callback for <see cref="NetworkStreamFactoryConfiguration{TState}.CreateState"/>.</param>
   /// <returns>A new instance of <see cref="NetworkStreamFactoryConfiguration{TState}"/> which will delegate all of its functionality to this <see cref="NetworkStreamFactoryConfiguration"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NetworkStreamFactoryConfiguration"/> is <c>null</c>.</exception>
   public static NetworkStreamFactoryConfiguration<TState> AsStateful<TState>( this NetworkStreamFactoryConfiguration configuration, Func<Socket, TNetworkStream, CancellationToken, TState> createState = null )
   {
      ArgumentValidator.ValidateNotNullReference( configuration );

      var retVal = new NetworkStreamFactoryConfiguration<TState>()
      {
         // Call-thru for stateful callbacks
         IsSSLPossible = ( state ) => configuration.IsSSLPossible?.Invoke() ?? TaskUtils.TaskFromBoolean( default ),
         GetSSLProtocols = ( state ) => configuration.GetSSLProtocols?.Invoke() ?? AbstractNetworkStreamFactoryConfiguration.DEFAULT_SSL_PROTOCOLS,
         ProvideSSLHost = ( state ) => configuration.ProvideSSLHost?.Invoke(),
         ConnectionSSLMode = ( state ) => configuration.ConnectionSSLMode?.Invoke() ?? NotRequired,
         CreateState = createState,
         TransformStreamAfterCreation = configuration.TransformStreamAfterCreation,

         // The rest are copypaste
         RemoteAddress = configuration.RemoteAddress == null ? null : new Func<CancellationToken, ValueTask<IPAddress>>( async token => await configuration.RemoteAddress( token ) ),
         GetUnixSocketFilePath = configuration.GetUnixSocketFilePath,
         RemotePort = addr => configuration.RemotePort?.Invoke( addr ) ?? 0,
         SocketType = configuration.SocketType,
         ProtocolType = configuration.ProtocolType,
         SelectLocalIPEndPoint = localEP => configuration.SelectLocalIPEndPoint?.Invoke( localEP ),
         ProvideSSLStream = ( Stream arg1, Boolean arg2, TRemoteCertificateValidationCallback arg3, TLocalCertificateSelectionCallback arg4, out TAuthenticateAsClientAsync arg5 ) =>
         {
            arg5 = null;
            return configuration.ProvideSSLStream?.Invoke( arg1, arg2, arg3, arg4, out arg5 );
         },
         ValidateServerCertificate = ( arg1, arg2, arg3, arg4 ) => configuration.ValidateServerCertificate?.Invoke( arg1, arg2, arg3, arg4 ) ?? true,
         SelectLocalCertificate = ( arg1, arg2, arg3, arg4, arg5 ) => configuration.SelectLocalCertificate?.Invoke( arg1, arg2, arg3, arg4, arg5 ),
         RemoteNoSSLSupport = () => configuration.RemoteNoSSLSupport?.Invoke(),
         NoSSLStreamProvider = () => configuration.NoSSLStreamProvider?.Invoke(),
         SSLStreamProviderNoStream = () => configuration.SSLStreamProviderNoStream?.Invoke(),
         SSLStreamProviderNoAuthenticationCallback = () => configuration.SSLStreamProviderNoAuthenticationCallback?.Invoke(),
         SSLStreamOtherError = ( inner ) => configuration.SSLStreamOtherError?.Invoke( inner )
      };

      retVal.ProvideClientCertificatesEvent += a => configuration.ProvideClientCertificates?.Invoke( a );

      return retVal;
   }
}