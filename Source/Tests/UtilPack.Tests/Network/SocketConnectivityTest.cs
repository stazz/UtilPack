///*
// * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
// *
// * Licensed  under the  Apache License,  Version 2.0  (the "License");
// * you may not use  this file  except in  compliance with the License.
// * You may obtain a copy of the License at
// *
// *   http://www.apache.org/licenses/LICENSE-2.0
// *
// * Unless required by applicable law or agreed to in writing, software
// * distributed  under the  License is distributed on an "AS IS" BASIS,
// * WITHOUT  WARRANTIES OR CONDITIONS  OF ANY KIND, either  express  or
// * implied.
// *
// * See the License for the specific language governing permissions and
// * limitations under the License. 
// */
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using NetworkUtils.Configuration;
//using NetworkUtils.ResourcePooling;
//using System;
//using System.Collections.Generic;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading.Tasks;

//namespace UtilPack.Tests.Network
//{
//   [TestClass]
//   public class SocketConnectivityTest
//   {
//      [TestMethod
//         , Timeout( 1000 )
//         ]
//      public async Task TestNormalSockets()
//      {
//         var config = new TestNetworkConfig( new TestNetworkConfigData()
//         {
//            Connection = new TestConnectionConfig()
//            {
//               Host = "google.com",
//               Port = 80
//            }
//         } );

//         var nwConfig = config.CreateNetworkStreamFactoryConfiguration(
//            Encoding.UTF8,
//            () => TaskUtils.False,
//            null,
//            null,
//            null,
//            null,
//            null
//            );
//         (var socket, var stream) = await NetworkStreamFactory.AcquireNetworkStreamFromConfiguration( nwConfig.Item1, default );
//         Assert.IsNotNull( socket, "Socket must not be null." );
//         Assert.IsTrue( socket.Connected, "Socket must be connected." );
//      }

//      [TestMethod
//         , Ignore // For now, since this doesn't work on windows
//         ]
//      public async Task TestUnixSockets()
//      {
//         var config = new TestNetworkConfig( new TestNetworkConfigData()
//         {
//            Connection = new TestConnectionConfig()
//            {
//               UnixSocketFilePath = "/var/run/some_socket"
//            }
//         } );

//         var nwConfig = config.CreateNetworkStreamFactoryConfiguration(
//            Encoding.UTF8,
//            () => TaskUtils.False,
//            null,
//            null,
//            null,
//            null,
//            null
//            );
//         (var socket, var stream) = await NetworkStreamFactory.AcquireNetworkStreamFromConfiguration( nwConfig.Item1, default );
//         Assert.IsNotNull( socket, "Socket must not be null." );
//         Assert.IsTrue( socket.Connected, "Socket must be connected." );
//      }

//   }


//   public sealed class TestNetworkConfig : NetworkConnectionCreationInfo<TestNetworkConfigData, TestConnectionConfig, TestInitConfig, TestProtocolConfig, TestPoolConfig>
//   {
//      public TestNetworkConfig(
//         TestNetworkConfigData data
//         ) : base( data )
//      {
//      }
//   }

//   public sealed class TestNetworkConfigData : NetworkConnectionCreationInfoData<TestConnectionConfig, TestInitConfig, TestProtocolConfig, TestPoolConfig>
//   {

//   }

//   public sealed class TestConnectionConfig : NetworkConnectionConfiguration
//   {

//   }

//   public sealed class TestInitConfig : NetworkInitializationConfiguration<TestProtocolConfig, TestPoolConfig>
//   {

//   }

//   public sealed class TestProtocolConfig
//   {

//   }

//   public sealed class TestPoolConfig : NetworkPoolingConfiguration
//   {

//   }
//}
