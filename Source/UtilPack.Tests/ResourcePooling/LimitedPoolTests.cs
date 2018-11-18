///*
// * Copyright 2017 Stanislav Muhametsin. All rights Reserved.
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
//using ResourcePooling.Async.Abstractions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace UtilPack.Tests.ResourcePooling
//{
//   [TestClass]
//   public class LimitedPoolTests
//   {
//      [TestMethod]
//      public async Task TestCachingLimitedPool()
//      {
//         var random = new Random();
//         var factory = new DefaultAsyncResourceFactory<TestResource, Func<Int32>>( p => new AsyncTestResourceFactory( p ) )
//            .BindCreationParameters( () => random.Next( 100, 200 ) );

//         var pool = factory.CreateLimitedResourcePool( 10 );
//         await Task.WhenAll( Enumerable
//            .Repeat( 0, 20 )
//            .Select( unused => pool.UseResourceAsync( async resource =>
//            {
//               await Task.Delay( 50 );
//            }, default )
//            )
//            .ToArray()
//            );
//      }
//   }

//   public class AsyncTestResourceFactory : DefaultBoundAsyncResourceFactory<TestResource, Func<Int32>>
//   {
//      private Int32 _id;

//      public AsyncTestResourceFactory( Func<Int32> config )
//         : base( config )
//      {
//      }

//      protected override async ValueTask<AsyncResourceAcquireInfo<TestResource>> AcquireResourceAsync( CancellationToken token )
//      {
//         await Task.Delay( this.CreationParameters(), token );

//         return new TestResourceAcquireInfo( new TestResource( Interlocked.Increment( ref this._id ) ) );
//      }

//      public override void ResetFactoryState()
//      {
//         Interlocked.Exchange( ref this._id, 0 );
//      }
//   }

//   public class TestResource
//   {
//      public TestResource( Int32 id )
//      {
//         this.ID = id;
//      }

//      public Int32 ID { get; }
//   }

//   public class TestResourceAcquireInfo : AsyncResourceAcquireInfoImpl<TestResource, TestResource>
//   {
//      public TestResourceAcquireInfo(
//         TestResource publicResource
//         ) : base( publicResource, publicResource, ( res, tkn ) => { }, ( res ) => { } )
//      {
//      }

//      protected override void Dispose( Boolean disposing )
//      {
//      }

//      protected override Task DisposeBeforeClosingChannel( CancellationToken token )
//      {
//         return Task.CompletedTask;
//      }

//      protected override Boolean PublicResourceCanBeReturnedToPool()
//      {
//         return true;
//      }
//   }
//}
