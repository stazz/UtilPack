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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack.AsyncEnumeration;

namespace UtilPack.Tests.AsyncEnumeration
{
   [TestClass]
   public class SequentialTests
   {
      const Int32 MAX_ITEMS = 10;

      [DataTestMethod,
         DataRow( false ),
         DataRow( true ),
         ]
      public async Task TestSequentialEnumeratorAsync(
         Boolean useConcurrentWrapper
         )
      {
         var start = MAX_ITEMS;
         var completionState = new Int32[start];
         var r = new Random();
         MoveNextAsyncDelegate<Int32> moveNext = async () =>
         {
            var decremented = Interlocked.Decrement( ref start );
            await Task.Delay( r.Next( 100, 500 ) );
            return (decremented >= 0, MAX_ITEMS - decremented - 1);
         };

         var enumerable = AsyncEnumerationFactory.CreateSequentialEnumerable( () => AsyncEnumerationFactory.CreateSequentialStartInfo(
            moveNext,
            null
            ) );
         Func<Int32, Task> callback = async idx =>
         {
            await Task.Delay( r.Next( 100, 900 ) );
            if ( !useConcurrentWrapper )
            {
               Assert.IsTrue( completionState.Take( idx ).All( s => s == 1 ) );
            }
            Interlocked.Increment( ref completionState[idx] );
         };
         var itemsEncountered = await ( useConcurrentWrapper ?
            enumerable.AsConcurrentEnumerable().EnumerateConcurrentlyAsync( callback ) :
            enumerable.EnumerateSequentiallyAsync( callback )
            );
         Assert.AreEqual( itemsEncountered, completionState.Length );
         Assert.IsTrue( completionState.All( s => s == 1 ) );
      }

      [DataTestMethod,
         DataRow( false ),
         DataRow( true )
         ]
      public Task TestSequentialEnumeratorCompletelySync(
          Boolean useConcurrentWrapper
         )
      {
         var start = MAX_ITEMS;
         var completionState = new Int32[start];
         var r = new Random();
         MoveNextAsyncDelegate<Int32> moveNext = () =>
         {
            var decremented = Interlocked.Decrement( ref start );
            return new ValueTask<(Boolean, Int32)>( (decremented >= 0, MAX_ITEMS - decremented - 1) );
         };

         var enumerable = AsyncEnumerationFactory.CreateSequentialEnumerable( () => AsyncEnumerationFactory.CreateSequentialStartInfo(
            moveNext,
            null
            ) );
         Action<Int32> callback = idx =>
         {
            System.Diagnostics.Trace.WriteLine( $"CALLBACK CALLED {useConcurrentWrapper}, {idx}." );
            if ( !useConcurrentWrapper )
            {
               Assert.IsTrue( completionState.Take( idx ).All( s => s == 1 ) );
            }
            Interlocked.Increment( ref completionState[idx] );
         };
         var itemsEncounteredTask = useConcurrentWrapper ?
            enumerable.AsConcurrentEnumerable().EnumerateConcurrentlyAsync( callback ) :
            enumerable.EnumerateSequentiallyAsync( callback );

         // We will always start a new task when concurrently enumerating async enumerable
         Assert.AreEqual( !useConcurrentWrapper, itemsEncounteredTask.IsCompleted );

         Task retVal;
         if ( useConcurrentWrapper )
         {
            retVal = TestSequentialEnumeratorCompletelySync_ConcurrentCompletion( itemsEncounteredTask.AsTask(), completionState );
         }
         else
         {
            TestSequentialEnumeratorCompletelySync_Completion( itemsEncounteredTask.Result, completionState );
            retVal = Task.CompletedTask;
         }
         return retVal;
      }

      private static void TestSequentialEnumeratorCompletelySync_Completion(
         Int64 itemsEncountered,
         Int32[] completionState
         )
      {
         Assert.AreEqual( itemsEncountered, completionState.Length );
         Assert.IsTrue( completionState.All( s => s == 1 ) );
      }

      private static async Task TestSequentialEnumeratorCompletelySync_ConcurrentCompletion(
         Task<Int64> enumerationTask,
         Int32[] completionState
         )
      {
         TestSequentialEnumeratorCompletelySync_Completion( await enumerationTask, completionState );
      }

      [TestMethod]
      public async Task TestParallelEnumeratorSync_SequentialEnumeration()
      {

         var start = MAX_ITEMS;
         var completionState = new Int32[start];
         var r = new Random();
         var enumerable = AsyncEnumerationFactory.CreateConcurrentEnumerable( () => AsyncEnumerationFactory.CreateConcurrentStartInfo(
            () =>
            {
               var decremented = Interlocked.Decrement( ref start );
               return (decremented >= 0, decremented + 1);
            },
            async ( idx ) =>
            {
               await Task.Delay( r.Next( 100, 500 ) );
               return completionState.Length - idx;
            },
            null
            ) );

         var itemsEncountered = await enumerable.EnumerateSequentiallyAsync( cur =>
         {
            Interlocked.Increment( ref completionState[cur] );
         } );

         Assert.AreEqual( itemsEncountered, completionState.Length );
         Assert.IsTrue( completionState.All( s => s == 1 ) );
      }

      [TestMethod]
      public async Task TestParallelEnumeratorASync_SequentialEnumeration()
      {

         var start = MAX_ITEMS;
         var completionState = new Int32[start];
         var r = new Random();
         var enumerable = AsyncEnumerationFactory.CreateConcurrentEnumerable( () => AsyncEnumerationFactory.CreateConcurrentStartInfo(
             () =>
            {
               var decremented = Interlocked.Decrement( ref start );
               return (decremented >= 0, decremented + 1);
            },
            async ( idx ) =>
            {
               await Task.Delay( r.Next( 100, 500 ) );
               return completionState.Length - idx;
            },
            null
            ) );

         var itemsEncountered = await enumerable.EnumerateSequentiallyAsync( async cur =>
         {
            await Task.Delay( r.Next( 100, 500 ) );
            Interlocked.Increment( ref completionState[cur] );
         } );

         Assert.AreEqual( itemsEncountered, completionState.Length );
         Assert.IsTrue( completionState.All( s => s == 1 ) );
      }

      [TestMethod]
      public void TestParallelEnumeratorCompletelySync_SequentialEnumeration()
      {
         var start = MAX_ITEMS * 100000;
         var completionState = new Int32[start];
         var r = new Random();
         var enumerable = AsyncEnumerationFactory.CreateConcurrentEnumerable( () => AsyncEnumerationFactory.CreateConcurrentStartInfo(
             () =>
            {
               var decremented = Interlocked.Decrement( ref start );
               return (decremented >= 0, decremented + 1);
            },
            ( idx ) =>
            {
               return new ValueTask<Int32>( completionState.Length - idx );
            },
            null
            ) );

         var itemsEncounteredTask = enumerable.EnumerateSequentiallyAsync( cur =>
         {
            Interlocked.Increment( ref completionState[cur] );
         } );

         Assert.IsTrue( itemsEncounteredTask.IsCompleted );
         Assert.AreEqual( itemsEncounteredTask.Result, completionState.Length );
         Assert.IsTrue( completionState.All( s => s == 1 ) );
      }

      [TestMethod]
      public async Task TestAsyncLINQ()
      {
         var array = Enumerable.Range( 0, 10 ).ToArray();
         var enumerable = array.AsAsyncEnumerable();
         var array2 = await enumerable.ToArrayAsync();
         Assert.IsTrue( ArrayEqualityComparer<Int32>.ArrayEquality( array, array2 ) );

         var sequentialList = new List<Int32>();
         var itemsEncountered2 = await enumerable.Where( x => x >= 5 ).EnumerateSequentiallyAsync( async cur =>
         {
            await Task.Delay( new Random().Next( 300, 500 ) );
            sequentialList.Add( cur );
         } );
         Assert.IsTrue( ArrayEqualityComparer<Int32>.ArrayEquality( array.Where( x => x >= 5 ).ToArray(), sequentialList.ToArray() ) );
      }
   }
}
