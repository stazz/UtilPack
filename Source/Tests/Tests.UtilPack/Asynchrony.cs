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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UtilPack;

namespace Tests.UtilPack
{
   [TestClass]
   public class AsynchronyTests
   {
      [TestMethod, Timeout( 1500 )]
      public async Task TestTimeoutAfter()
      {
         await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) )
            .TimeoutAfter( TimeSpan.FromMilliseconds( 2000 ), default );
      }

      [TestMethod,
         Timeout( 1500 ),
         ExpectedException( typeof( TimeoutException ), AllowDerivedTypes = false )
         ]
      public async Task TestTimeoutAfterWithExpectedException()
      {
         await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) )
            .TimeoutAfter( TimeSpan.FromMilliseconds( 500 ), default );
      }

      [TestMethod, Timeout( 1500 )]
      public async Task TestTimeoutAfterWithResult()
      {
         await this.DelayWithResult( TimeSpan.FromMilliseconds( 1000 ), new Object() )
            .TimeoutAfter( TimeSpan.FromMilliseconds( 2000 ), default );
      }

      [TestMethod,
         Timeout( 1500 ),
         ExpectedException( typeof( TimeoutException ), AllowDerivedTypes = false )
         ]
      public async Task TestTimeoutAfterWithresultAndExpectedException()
      {
         await this.DelayWithResult( TimeSpan.FromMilliseconds( 1000 ), new Object() )
            .TimeoutAfter( TimeSpan.FromMilliseconds( 500 ), default );
      }

      private async Task<T> DelayWithResult<T>( TimeSpan delay, T result )
      {
         await Task.Delay( delay );
         return result;
      }
   }
}
