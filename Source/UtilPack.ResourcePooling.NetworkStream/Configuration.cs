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
using UtilPack.Configuration.NetworkStream;

using TNetworkStream = System.Net.Sockets.NetworkStream;

namespace UtilPack.ResourcePooling.NetworkStream
{

   /// <summary>
   /// This class exists in order to avoid specifying *all* generic parameters when creating <see cref="NetworkStreamFactoryConfiguration{TState}"/> from <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.
   /// </summary>
   /// <typeparam name="TCreationData">The type of typically serializable configuration data, which only has passive properties. Must be or inherit <see cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration of <typeparamref name="TCreationData"/> controlling behaviour of <see cref="AsyncResourcePool{TResource}"/>. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <seealso cref="E_UtilPack.CreateStatefulNetworkStreamFactoryConfiguration"/>
   public sealed class StatefulNetworkStreamFactoryCreator<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>
      where TCreationData : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      private readonly NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration> _creationInfo;

      internal StatefulNetworkStreamFactoryCreator(
          NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration> creationInfo
         )
      {
         this._creationInfo = ArgumentValidator.ValidateNotNull( nameof( creationInfo ), creationInfo );
      }

      /// <summary>
      /// Creates a <see cref="NetworkStreamFactoryConfiguration{TState}"/> along with other items based on the <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/> this instance has.
      /// </summary>
      /// <typeparam name="TIntermediateState">The type of generic parameter of <see cref="NetworkStreamFactoryConfiguration{TState}"/>.</typeparam>
      /// <param name="stateFactory">The callback to create intermediate state.</param>
      /// <param name="encoding">The <see cref="Encoding"/> to use to read strings from stream. Affects the returned <see cref="BinaryStringPool"/> only.</param>
      /// <param name="isSSLPossible">Callback to check whether SSL is possible. If <c>null</c>, then SSL stream creation will not be possible.</param>
      /// <param name="noSSLStreamProvider">The callback to create an <see cref="Exception"/> to be thrown when there are no SSL stream provider, but should be one.</param>
      /// <param name="remoteNoSSLSupport">The callback to create an <see cref="Exception"/> to be thrown when remote does not support SSL when it should.</param>
      /// <param name="sslStreamProviderNoStream">The callback to create an <see cref="Exception"/> to be thrown when <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}.ProvideSSLStream"/> returns <c>null</c>.</param>
      /// <param name="sslStreamProviderNoAuthenticationCallback">The callback to create an <see cref="Exception"/> when <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}.ProvideSSLStream"/> does not provide authentication callback.</param>
      /// <param name="sslStreamOtherError">The callback to create an <see cref="Exception"/> when other SSL-related error occurs.</param>
      /// <returns>A tuple containing <see cref="NetworkStreamFactoryConfiguration{TState}"/> based on the <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/> this instance holds, along with awaitable to get remote <see cref="IPAddress"/>, and <see cref="BinaryStringPool"/> instance.</returns>
      public (NetworkStreamFactoryConfiguration<TIntermediateState>, ReadOnlyResettableAsyncLazy<IPAddress>, BinaryStringPool) Create<TIntermediateState>(
         Func<Socket, TNetworkStream, CancellationToken, TIntermediateState> stateFactory,
         Encoding encoding,
         Func<TIntermediateState, Task<Boolean>> isSSLPossible,
         Func<Exception> noSSLStreamProvider,
         Func<Exception> remoteNoSSLSupport,
         Func<Exception> sslStreamProviderNoStream,
         Func<Exception> sslStreamProviderNoAuthenticationCallback,
         Func<Exception, Exception> sslStreamOtherError
         )
      {
         var creationInfo = this._creationInfo;
         var data = creationInfo.CreationData;
         var conn = data.Connection;
         var host = conn?.Host;
         ReadOnlyResettableAsyncLazy<IPAddress> remoteAddress;
         Func<CancellationToken, ValueTask<IPAddress>> remoteAddressFunc;
         Func<String> unixSocketFunc;
         if ( host == null )
         {
            unixSocketFunc = () => conn?.UnixSocketFilePath;
            remoteAddress = null;
            remoteAddressFunc = null;
         }
         else
         {
            remoteAddress = UtilPackMiscellaneous.CreateAddressOrHostNameResolvingLazy(
               host,
               creationInfo.SelectRemoteIPAddress,
               creationInfo.DNSResolve
               );
            // TODO use token
            remoteAddressFunc = async ( token ) => await remoteAddress;
            unixSocketFunc = null;
         }

         return (new NetworkStreamFactoryConfiguration<TIntermediateState>()
         {
            CreateState = stateFactory,
            SelectLocalIPEndPoint = creationInfo.SelectLocalIPEndPoint,
            RemoteAddress = remoteAddressFunc,
            RemotePort = addr => creationInfo.CreationData?.Connection?.Port ?? throw new ArgumentException( "No port specified" ),
            GetUnixSocketAddress = unixSocketFunc,

            // SSL stuff here
            ConnectionSSLMode = ( state ) => data.Connection?.ConnectionSSLMode ?? ConnectionSSLMode.NotRequired,
            IsSSLPossible = isSSLPossible,
            GetSSLProtocols = ( state ) => data.Connection?.SSLProtocols ?? AbstractNetworkStreamFactoryConfiguration.DEFAULT_SSL_PROTOCOLS,
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
   /// Creates a <see cref="NetworkStreamFactoryConfiguration"/> along with other items based on this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.
   /// </summary>
   /// <param name="creationInfo">This <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.</param>
   /// <param name="encoding">The <see cref="Encoding"/> to use to read strings from stream. Affects the returned <see cref="BinaryStringPool"/> only.</param>
   /// <param name="isSSLPossible">Callback to check whether SSL is possible. If <c>null</c>, then SSL stream creation will not be possible.</param>
   /// <param name="noSSLStreamProvider">The callback to create an <see cref="Exception"/> to be thrown when there are no SSL stream provider, but should be one.</param>
   /// <param name="remoteNoSSLSupport">The callback to create an <see cref="Exception"/> to be thrown when remote does not support SSL when it should.</param>
   /// <param name="sslStreamProviderNoStream">The callback to create an <see cref="Exception"/> to be thrown when <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}.ProvideSSLStream"/> returns <c>null</c>.</param>
   /// <param name="sslStreamProviderNoAuthenticationCallback">The callback to create an <see cref="Exception"/> when <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}.ProvideSSLStream"/> does not provide authentication callback.</param>
   /// <param name="sslStreamOtherError">The callback to create an <see cref="Exception"/> when other SSL-related error occurs.</param>
   /// <returns>A tuple containing <see cref="NetworkStreamFactoryConfiguration"/> based on the <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/> this instance holds, along with awaitable to get remote <see cref="IPAddress"/>, and <see cref="BinaryStringPool"/> instance.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/> is <c>null</c>.</exception>
   public static (NetworkStreamFactoryConfiguration, ReadOnlyResettableAsyncLazy<IPAddress>, BinaryStringPool) CreateNetworkStreamFactoryConfiguration<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>(
      this NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration> creationInfo,
      Encoding encoding,
      Func<Task<Boolean>> isSSLPossible,
      Func<Exception> noSSLStreamProvider,
      Func<Exception> remoteNoSSLSupport,
      Func<Exception> sslStreamProviderNoStream,
      Func<Exception> sslStreamProviderNoAuthenticationCallback,
      Func<Exception, Exception> sslStreamOtherError
      )
      where TCreationData : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      var data = creationInfo.CreationData;
      var conn = data.Connection;
      var host = conn?.Host;
      ReadOnlyResettableAsyncLazy<IPAddress> remoteAddress;
      Func<CancellationToken, ValueTask<IPAddress>> remoteAddressFunc;
      Func<String> unixSocketFunc;
      if ( host == null )
      {
         unixSocketFunc = () => conn?.UnixSocketFilePath;
         remoteAddress = null;
         remoteAddressFunc = null;
      }
      else
      {
         remoteAddress = UtilPackMiscellaneous.CreateAddressOrHostNameResolvingLazy(
            host,
            creationInfo.SelectRemoteIPAddress,
            creationInfo.DNSResolve
            );
         // TODO use token
         remoteAddressFunc = async ( token ) => await remoteAddress;
         unixSocketFunc = null;
      }

      return (new NetworkStreamFactoryConfiguration()
      {
         SelectLocalIPEndPoint = creationInfo.SelectLocalIPEndPoint,
         RemoteAddress = remoteAddressFunc,
         RemotePort = addr => creationInfo.CreationData?.Connection?.Port ?? throw new ArgumentException( "No port specified" ),
         GetUnixSocketAddress = unixSocketFunc,

         // SSL stuff here
         ConnectionSSLMode = () => data.Connection?.ConnectionSSLMode ?? ConnectionSSLMode.NotRequired,
         IsSSLPossible = isSSLPossible,
         GetSSLProtocols = () => data.Connection?.SSLProtocols ?? AbstractNetworkStreamFactoryConfiguration.DEFAULT_SSL_PROTOCOLS,
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
   /// Creates a <see cref="StatefulNetworkStreamFactoryCreator{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/> to use to create <see cref="NetworkStreamFactoryConfiguration{TState}"/> instances based on this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.
   /// </summary>
   /// <typeparam name="TCreationData">The type of typically serializable configuration data, which only has passive properties. Must be or inherit <see cref="NetworkConnectionCreationInfoData{TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TConnectionConfiguration">The type of network connection configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkConnectionConfiguration"/>.</typeparam>
   /// <typeparam name="TInitializationConfiguration">The type of initialization configuration of <typeparamref name="TCreationData"/>. Must be or inherit <see cref="NetworkInitializationConfiguration{TProtocolConfiguration, TPoolingConfiguration}"/>.</typeparam>
   /// <typeparam name="TProtocolConfiguration">The type of protocol configuration of <typeparamref name="TCreationData"/>.</typeparam>
   /// <typeparam name="TPoolingConfiguration">The type of resource pool configuration of <typeparamref name="TCreationData"/> controlling behaviour of <see cref="UtilPack.ResourcePooling.AsyncResourcePool{TResource}"/>. Must be or inherit <see cref="NetworkPoolingConfiguration"/>.</typeparam>
   /// <param name="creationInfo">This <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/>.</param>
   /// <returns>An class that can be used to create <see cref="NetworkStreamFactoryConfiguration{TState}"/> instances.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NetworkConnectionCreationInfo{TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration}"/> is <c>null</c>.</exception>
   public static StatefulNetworkStreamFactoryCreator<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration> CreateStatefulNetworkStreamFactoryConfiguration<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>(
      this NetworkConnectionCreationInfo<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration> creationInfo
      )
      where TCreationData : NetworkConnectionCreationInfoData<TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>
      where TConnectionConfiguration : NetworkConnectionConfiguration
      where TInitializationConfiguration : NetworkInitializationConfiguration<TProtocolConfiguration, TPoolingConfiguration>
      where TPoolingConfiguration : NetworkPoolingConfiguration
   {
      return new StatefulNetworkStreamFactoryCreator<TCreationData, TConnectionConfiguration, TInitializationConfiguration, TProtocolConfiguration, TPoolingConfiguration>( ArgumentValidator.ValidateNotNullReference( creationInfo ) );
   }
}