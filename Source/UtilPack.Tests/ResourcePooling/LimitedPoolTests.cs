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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using UtilPack.ResourcePooling;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace UtilPack.Tests.ResourcePooling
{
   [TestClass]
   public class LimitedPoolTests
   {
      [TestMethod]
      public async Task TestCachingLimitedPool()
      {
         var random = new Random();
         var factory = new AsyncTestResourceFactory();
         var pool = factory.CreateLimitedResourcePool( () => random.Next( 100, 200 ), 10 );
         await Task.WhenAll( Enumerable
            .Repeat( 0, 20 )
            .Select( unused => pool.UseResourceAsync( async resource =>
            {
               await Task.Delay( 50 );
            } )
            )
            .ToArray()
            );


      }
   }

   public class AsyncTestResourceFactory : AsyncResourceFactory<TestResource, Func<Int32>>
   {
      private Int32 _id;

      public async ValueTask<AsyncResourceAcquireInfo<TestResource>> AcquireResourceAsync( Func<Int32> parameters, CancellationToken token )
      {
         await Task.Delay( parameters(), token );

         return new TestResourceAcquireInfo( new TestResource( Interlocked.Increment( ref this._id ) ) );
      }

      public void ResetFactoryState()
      {
         Interlocked.Exchange( ref this._id, 0 );
      }
   }

   public class TestResource
   {
      public TestResource( Int32 id )
      {
         this.ID = id;
      }

      public Int32 ID { get; }
   }

   public class TestResourceAcquireInfo : AsyncResourceAcquireInfoImpl<TestResource, TestResource>
   {
      public TestResourceAcquireInfo(
         TestResource publicResource
         ) : base( publicResource, publicResource, ( res, tkn ) => { }, ( res ) => { } )
      {
      }

      protected override void Dispose( Boolean disposing )
      {
      }

      protected override Task DisposeBeforeClosingChannel( CancellationToken token )
      {
         return Task.CompletedTask;
      }

      protected override Boolean PublicResourceCanBeReturnedToPool()
      {
         return true;
      }
   }
}
