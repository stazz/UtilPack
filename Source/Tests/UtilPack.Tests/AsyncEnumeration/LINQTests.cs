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
//using AsyncEnumeration.Implementation.Enumerable;
//using AsyncEnumeration.Implementation.Provider;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace UtilPack.Tests.AsyncEnumeration
//{
//   [TestClass]
//   public class LINQTests
//   {
//      [TestMethod]
//      public async Task TestTake()
//      {
//         var array = await AsyncEnumerable.Repeat( count: 5, item: 1, provider: DefaultAsyncProvider.Instance ).Take( 3 ).ToArrayAsync();

//         Assert.IsTrue( ArrayEqualityComparer<Int32>.ArrayEquality( array, Enumerable.Repeat( 1, 3 ).ToArray() ) );
//      }

//      [TestMethod, Timeout( 1000 )]
//      public async Task TestTakeNeverEnding()
//      {
//         var array = await AsyncEnumerable.Neverending( 1, provider: DefaultAsyncProvider.Instance ).Take( 3 ).ToArrayAsync();
//         Assert.IsTrue( ArrayEqualityComparer<Int32>.ArrayEquality( array, Enumerable.Repeat( 1, 3 ).ToArray() ) );
//      }

//      [TestMethod, Timeout( 1000 )]
//      public async Task TestTakeWhile()
//      {
//         var idx = 0;
//         var array = await AsyncEnumerable.Neverending( 1, provider: DefaultAsyncProvider.Instance ).TakeWhile( item => ++idx <= 3 ).ToArrayAsync();
//         Assert.IsTrue( ArrayEqualityComparer<Int32>.ArrayEquality( array, Enumerable.Repeat( 1, 3 ).ToArray() ) );
//      }

//      [TestMethod]
//      public async Task TestTakeWhileAsync()
//      {
//         var idx = 0;
//         var array = await AsyncEnumerable.Neverending( 1, provider: DefaultAsyncProvider.Instance ).TakeWhile( async item => { await Task.Delay( 100 ); return ++idx <= 3; } ).ToArrayAsync();
//         Assert.IsTrue( ArrayEqualityComparer<Int32>.ArrayEquality( array, Enumerable.Repeat( 1, 3 ).ToArray() ) );
//      }
//   }
//}
