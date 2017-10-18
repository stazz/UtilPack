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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TSequentialCurrentInfoFactory = System.Func<System.Object, UtilPack.AsyncEnumeration.EnumerationEndedDelegate, System.Object>;

namespace UtilPack.AsyncEnumeration
{
   /// <summary>
   /// This class provides static factory methods to create objects of type <see cref="IAsyncEnumerator{T}"/>.
   /// </summary>
   public static class AsyncEnumerationFactory
   {
      ///// <summary>
      ///// Creates a new instance of <see cref="IAsyncEnumerable{T}"/> which will behave as specified by given <see cref="InitialMoveNextAsyncDelegate{T}"/> delegate.
      ///// </summary>
      ///// <typeparam name="T">The type of the items being enumerated.</typeparam>
      ///// <param name="initialMoveNext">The <see cref="InitialMoveNextAsyncDelegate{T}"/> callback which will control how resulting <see cref="IAsyncEnumerable{T}"/> will behave.</param>
      ///// <returns>A new <see cref="IAsyncEnumerable{T}"/>.</returns>
      ///// <exception cref="ArgumentNullException">If <paramref name="initialMoveNext"/> is <c>null</c>.</exception>
      //public static IAsyncEnumerable<T> CreateSequentialEnumerable<T>(
      //   InitialMoveNextAsyncDelegate<T> initialMoveNext
      //   ) => new EnumerableWrapper<T>( () => CreateSequentialEnumerator( initialMoveNext ) );

      ///// <summary>
      ///// Creates a new instance of <see cref="IAsyncEnumerator{T}"/> which will behave as specified by given <see cref="InitialMoveNextAsyncDelegate{T}"/> delegate.
      ///// </summary>
      ///// <typeparam name="T">The type of the items being enumerated.</typeparam>
      ///// <param name="initialMoveNext">The <see cref="InitialMoveNextAsyncDelegate{T}"/> callback which will control how resulting <see cref="IAsyncEnumerator{T}"/> will behave.</param>
      ///// <returns>A new <see cref="IAsyncEnumerator{T}"/>.</returns>
      ///// <exception cref="ArgumentNullException">If <paramref name="initialMoveNext"/> is <c>null</c>.</exception>
      //public static IAsyncEnumerator<T> CreateSequentialEnumerator<T>(
      //   InitialMoveNextAsyncDelegate<T> initialMoveNext
      //   )
      //{
      //   SequentialCurrentInfoFactory.Get( typeof( T ) )()
      //   return new AsyncSequentialOnlyEnumeratorImpl<T>( initialMoveNext, SequentialCurrentInfoFactory.Get( typeof( T ) ) );
      //}

      /// <summary>
      /// Creates a new instance of <see cref="IAsyncEnumerable{T}"/> with given callback to create <see cref="SequentialEnumerationStartInfo{T}"/> for each new <see cref="IAsyncEnumerator{T}"/>.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="enumerationStart">The callback to create a new <see cref="SequentialEnumerationStartInfo{T}"/> for each new <see cref="IAsyncEnumerator{T}"/>.</param>
      /// <returns>A new instance of <see cref="IAsyncEnumerable{T}"/> which behaves like <see cref="SequentialEnumerationStartInfo{T}"/> returned by <paramref name="enumerationStart"/> specifies.</returns>
      /// <remarks>
      /// If <paramref name="enumerationStart"/> is <c>null</c>, then result is empty enumerable.
      /// </remarks>
      /// <seealso cref="CreateSequentialStartInfo{T}(MoveNextAsyncDelegate{T}, EnumerationEndedDelegate)"/>
      public static IAsyncEnumerable<T> CreateSequentialEnumerable<T>(
         Func<SequentialEnumerationStartInfo<T>> enumerationStart
         ) => enumerationStart == null ? (IAsyncEnumerable<T>) EmptyAsync<T>.Enumerable : new AsyncSequentialOnlyEnumerable<T>( enumerationStart );

      /// <summary>
      /// Creates a new <see cref="IAsyncEnumerable{T}"/> which will allow at most one <see cref="IAsyncEnumerator{T}"/> to be active at once.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="startInfo">The <see cref="SequentialEnumerationStartInfo{T}"/> containing callbacks to use.</param>
      /// <returns>A new instance of <see cref="IAsyncEnumerable{T}"/> which behaves like callbacks in <paramref name="startInfo"/> specified.</returns>
      public static IAsyncEnumerable<T> CreateExclusiveSequentialEnumerable<T>(
         SequentialEnumerationStartInfo<T> startInfo
         ) => new AsyncEnumerableExclusive<T>( SequentialCurrentInfoFactory.GetInstance( startInfo.MoveNext, startInfo.Dispose ) );

      /// <summary>
      /// Helper method to invoke constructor of <see cref="SequentialEnumerationStartInfo{T}"/> without explicitly specifying generic type arguments.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="moveNext">The callback for potentially asynchronously fetching next item.</param>
      /// <param name="dispose">The callback to dispose enumerator.</param>
      /// <returns>A new <see cref="SequentialEnumerationStartInfo{T}"/>.</returns>
      /// <seealso cref="SequentialEnumerationStartInfo{T}.SequentialEnumerationStartInfo(MoveNextAsyncDelegate{T}, EnumerationEndedDelegate)"/>
      public static SequentialEnumerationStartInfo<T> CreateSequentialStartInfo<T>(
         MoveNextAsyncDelegate<T> moveNext,
         EnumerationEndedDelegate dispose
         ) => new SequentialEnumerationStartInfo<T>( moveNext, dispose );

      /// <summary>
      /// Creates a new instance of <see cref="IAsyncEnumerator{T}"/> which fetches one item at a time using given callback.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="moveNext">The callback for potentially asynchronously fetching next item.</param>
      /// <param name="dispose">The callback to dispose enumerator.</param>
      /// <returns>A new instance of <see cref="IAsyncEnumerator{T}"/> which behaves like <paramref name="moveNext"/> and <paramref name="dispose"/> specify.</returns>
      /// <remarks>
      /// The returned <see cref="IAsyncEnumerator{T}"/> will have guard code to prevent concurrent invocation.
      /// </remarks>
      public static IAsyncEnumerator<T> CreateSequentialEnumerator<T>(
         MoveNextAsyncDelegate<T> moveNext,
         EnumerationEndedDelegate dispose
         ) => new AsyncEnumerator<T>( SequentialCurrentInfoFactory.GetInstance( moveNext, dispose ) );

      /// <summary>
      /// Creates a new instance of <see cref="IAsyncConcurrentEnumerable{T}"/> with given callback to create <see cref="ConcurrentEnumerationStartInfo{T, TState}"/> for each new <see cref="IAsyncConcurrentEnumeratorSource{T}"/>.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <typeparam name="TState">The type of state to transfer between <see cref="ConcurrentEnumerationStartInfo{T, TState}.HasNext"/> and <see cref="ConcurrentEnumerationStartInfo{T, TState}.GetNext"/> invocations.</typeparam>
      /// <param name="enumerationStart">The callback to create a new <see cref="ConcurrentEnumerationStartInfo{T, TState}"/> for each new <see cref="IAsyncConcurrentEnumeratorSource{T}"/>.</param>
      /// <returns>A new instance of <see cref="IAsyncConcurrentEnumerable{T}"/> which behaves like <see cref="ConcurrentEnumerationStartInfo{T, TState}"/> returned by <paramref name="enumerationStart"/> specifies.</returns>
      public static IAsyncConcurrentEnumerable<T> CreateConcurrentEnumerable<T, TState>(
         Func<ConcurrentEnumerationStartInfo<T, TState>> enumerationStart
         ) => new AsyncConcurrentEnumerable<T, TState>( enumerationStart );

      /// <summary>
      /// Helper method to invoke constructor of <see cref="ConcurrentEnumerationStartInfo{T, TState}"/> without explicitly specifying generic type arguments.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <typeparam name="TState">The type of state to transfer between <see cref="ConcurrentEnumerationStartInfo{T, TState}.HasNext"/> and <see cref="ConcurrentEnumerationStartInfo{T, TState}.GetNext"/> invocations.</typeparam>
      /// <param name="hasNext">The callback to synchronously check whether there are more items.</param>
      /// <param name="getNext">The callback for potentially asynchronously fetching next item.</param>
      /// <param name="dispose">The callback to dispose enumerator.</param>
      /// <returns>A new <see cref="ConcurrentEnumerationStartInfo{T, TState}"/>.</returns>
      /// <seealso cref="ConcurrentEnumerationStartInfo{T, TState}.ConcurrentEnumerationStartInfo(HasNextDelegate{TState}, GetNextItemAsyncDelegate{T, TState}, EnumerationEndedDelegate)"/>
      public static ConcurrentEnumerationStartInfo<T, TState> CreateConcurrentStartInfo<T, TState>(
         HasNextDelegate<TState> hasNext,
         GetNextItemAsyncDelegate<T, TState> getNext,
         EnumerationEndedDelegate dispose
         ) => new ConcurrentEnumerationStartInfo<T, TState>( hasNext, getNext, dispose );

   }

   internal static class SequentialCurrentInfoFactory
   {
      private static readonly Dictionary<Type, TSequentialCurrentInfoFactory> CustomFactories = new Dictionary<Type, TSequentialCurrentInfoFactory>()
      {
         { typeof(Int32), (moveNext, disposeAsync) => new SequentialEnumeratorCurrentInfoWithInt32((MoveNextAsyncDelegate<Int32>)moveNext, disposeAsync ) },
         { typeof(Int64), (moveNext, disposeAsync) => new SequentialEnumeratorCurrentInfoWithInt64((MoveNextAsyncDelegate<Int64>)moveNext, disposeAsync ) },
         { typeof(Single), (moveNext, disposeAsync) => new SequentialEnumeratorCurrentInfoWithFloat32((MoveNextAsyncDelegate<Single>)moveNext, disposeAsync ) },
         { typeof(Double), (moveNext, disposeAsync) => new SequentialEnumeratorCurrentInfoWithFloat64((MoveNextAsyncDelegate<Double>)moveNext, disposeAsync ) }
      };

      internal static TSequentialCurrentInfoFactory GetFactory( Type type )
      {
         CustomFactories.TryGetValue( type, out var retVal );
         return retVal;
      }

      internal static SequentialEnumeratorCurrentInfo<T> GetInstance<T>(
         MoveNextAsyncDelegate<T> moveNext,
         EnumerationEndedDelegate dispose
         )
      {
         return (SequentialEnumeratorCurrentInfo<T>) GetFactory( typeof( T ) )?.Invoke( moveNext, dispose ) ?? new SequentialEnumeratorCurrentInfoWithObject<T>( moveNext, dispose );
      }
   }

   /// <summary>
   /// This delegate will be used by the <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/> method of <see cref="IAsyncEnumerator{T}"/> returned by <see cref="AsyncEnumerationFactory.CreateSequentialEnumerator"/> and <see cref="AsyncEnumerationFactory.CreateSequentialEnumerable"/> methods.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <returns>A value task with information about next item fetched.</returns>
   /// <remarks>
   /// The information is a tuple, where the elements are interpreted as following:
   /// <list type="number">
   /// <item><term><see cref="System.Boolean"/></term><description>Whether this fetch was a success. If enumerable has no more items, this should be <c>false</c>.</description></item>
   /// <item><term><typeparamref name="T"/></term><description>The item fetched.</description></item>
   /// </list>
   /// </remarks>
   /// <seealso cref="AsyncEnumerationFactory.CreateSequentialEnumerator{T}(MoveNextAsyncDelegate{T}, EnumerationEndedDelegate)"/>
   public delegate ValueTask<(Boolean, T)> MoveNextAsyncDelegate<T>();

   /// <summary>
   /// This delegate will be used by <see cref="IAsyncDisposable.DisposeAsync"/> methods when enumeration end is encountered.
   /// </summary>
   /// <returns>A task to perform asynchronous disposing. If no asynchronous disposing is needed, this delegate can return <c>null</c>.</returns>
   /// <seealso cref="AsyncEnumerationFactory.CreateSequentialEnumerator{T}(MoveNextAsyncDelegate{T}, EnumerationEndedDelegate)"/>
   public delegate Task EnumerationEndedDelegate();

   /// <summary>
   /// This delegate is used by <see cref="IAsyncConcurrentEnumerable{T}"/> created by <see cref="AsyncEnumerationFactory.CreateConcurrentEnumerable"/>.
   /// </summary>
   /// <typeparam name="TState">The type of state to pass to <see cref="GetNextItemAsyncDelegate{T, TMoveNextResult}"/>.</typeparam>
   /// <returns>A tuple with information about success movenext operation.</returns>
   /// <remarks>
   /// The information is a tuple, where the elements are interpreted as following:
   /// <list type="number">
   /// <item><term><see cref="System.Boolean"/></term><description>Whether this fetch was a success. If enumerable has no more items, this should be <c>false</c>.</description></item>
   /// <item><term><typeparamref name="TState"/></term><description>The state to pass to <see cref="GetNextItemAsyncDelegate{T, TMoveNextResult}"/>.</description></item>
   /// </list>
   /// </remarks>
   public delegate (Boolean, TState) HasNextDelegate<TState>();

   /// <summary>
   /// This delegate is used by <see cref="IAsyncConcurrentEnumerable{T}"/> created by <see cref="AsyncEnumerationFactory.CreateConcurrentEnumerable"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TMoveNextResult">The type of state returned by <see cref="HasNextDelegate{T}"/>.</typeparam>
   /// <param name="moveNextResult">The state component of the result of the <see cref="HasNextDelegate{T}"/> call.</param>
   /// <returns>A task to potentially asynchronously fetch the next item.</returns>
   public delegate ValueTask<T> GetNextItemAsyncDelegate<T, in TMoveNextResult>( TMoveNextResult moveNextResult );

   /// <summary>
   /// This struct captures information required for callback-based <see cref="IAsyncEnumerator{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <seealso cref="AsyncEnumerationFactory.CreateSequentialEnumerable"/>
   /// <seealso cref="AsyncEnumerationFactory.CreateSequentialStartInfo"/>
   public struct SequentialEnumerationStartInfo<T>
   {
      /// <summary>
      /// Initializes a new <see cref="SequentialEnumerationStartInfo{T}"/> with given callbacks.
      /// </summary>
      /// <param name="moveNext">The callback to fetch next item.</param>
      /// <param name="dispose">The optional callback to dispose enumerator. May be <c>null</c>.</param>
      /// <remarks>
      /// If <paramref name="moveNext"/> is <c>null</c>, then enumeration ends immediately.
      /// </remarks>
      /// <seealso cref="AsyncEnumerationFactory.CreateSequentialStartInfo"/>
      public SequentialEnumerationStartInfo(
         MoveNextAsyncDelegate<T> moveNext,
         EnumerationEndedDelegate dispose
         )
      {
         this.MoveNext = moveNext;
         this.Dispose = dispose;
      }

      /// <summary>
      /// Gets the callback to potentially asynchronously fetch the next item.
      /// </summary>
      /// <value>The callback to potentially asynchronously fetch the next item.</value>
      public MoveNextAsyncDelegate<T> MoveNext { get; }

      /// <summary>
      /// Gets the callback to dispose enumerator.
      /// </summary>
      /// <value>The callback to dispose enumerator.</value>
      public EnumerationEndedDelegate Dispose { get; }
   }

   /// <summary>
   /// This struct captures information required for callback-based <see cref="IAsyncConcurrentEnumerable{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TState">The type of state to pass between <see cref="HasNext"/> and <see cref="GetNext"/> invocations.</typeparam>
   /// <seealso cref="AsyncEnumerationFactory.CreateConcurrentEnumerable"/>
   /// <seealso cref="AsyncEnumerationFactory.CreateConcurrentStartInfo"/>
   public struct ConcurrentEnumerationStartInfo<T, TState>
   {
      /// <summary>
      /// Initializes a new <see cref="ConcurrentEnumerationStartInfo{T, TState}"/> with given callbacks.
      /// </summary>
      /// <param name="hasNext">The callback to synchronously check whether there are more items.</param>
      /// <param name="getNext">The callback to potentially asynchronously fetch next item.</param>
      /// <param name="dispose">The optional callback to dispose the enumerator. May be <c>null</c>.</param>
      /// <remarks>
      /// If <paramref name="hasNext"/> or <paramref name="getNext"/> is <c>null</c>, then the enumerable is considered to be empty.
      /// </remarks>
      /// <seealso cref="AsyncEnumerationFactory.CreateConcurrentStartInfo"/>
      public ConcurrentEnumerationStartInfo(
         HasNextDelegate<TState> hasNext,
         GetNextItemAsyncDelegate<T, TState> getNext,
         EnumerationEndedDelegate dispose
         )
      {
         this.HasNext = hasNext;
         this.GetNext = getNext;
         this.Dispose = dispose;
      }

      /// <summary>
      /// Gets the callback to synchronously check whether there are more items.
      /// </summary>
      /// <value>The callback to synchronously check whether there are more items.</value>
      public HasNextDelegate<TState> HasNext { get; }

      /// <summary>
      /// Gets the callback to potentially asynchronously fetch next item.
      /// </summary>
      /// <value>The callback to potentially asynchronously fetch next item.</value>
      public GetNextItemAsyncDelegate<T, TState> GetNext { get; }

      /// <summary>
      /// Gets the optional callback to dispose the enumerator.
      /// </summary>
      /// <value>The optional callback to dispose the enumerator.</value>
      public EnumerationEndedDelegate Dispose { get; }
   }
}
