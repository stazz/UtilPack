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
using AsyncEnumeration.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using TSequentialCurrentInfoFactory = System.Func<System.Object, AsyncEnumeration.Implementation.Enumerable.EnumerationEndedDelegate, System.Object>;

namespace AsyncEnumeration.Implementation.Enumerable
{
   /// <summary>
   /// This class provides static factory methods to create objects of type <see cref="IAsyncEnumerator{T}"/>.
   /// </summary>
   public static class AsyncEnumerationFactory
   {
      public static IAsyncEnumerable<T> FromGeneratorCallback<T>(
         Func<IAsyncEnumerator<T>> getEnumerator,
         IAsyncProvider asyncProvider
         )
      {
         return new EnumerableGenerator<T>( asyncProvider, getEnumerator );
      }

      public static IAsyncEnumerable<T> FromGeneratorCallback<T, TArg>(
         TArg arg,
         Func<TArg, IAsyncEnumerator<T>> getEnumerator,
         IAsyncProvider asyncProvider
         )
      {
         return new EnumerableGenerator<T, TArg>( asyncProvider, arg, getEnumerator );
      }

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
         Func<SequentialEnumerationStartInfo<T>> enumerationStart,
         IAsyncProvider aLINQProvider
         ) => enumerationStart == null ? EmptyAsync<T>.Enumerable : new AsyncSequentialOnlyEnumerable<T>( enumerationStart, aLINQProvider );

      /// <summary>
      /// Creates a new <see cref="IAsyncEnumerable{T}"/> which will allow at most one <see cref="IAsyncEnumerator{T}"/> to be active at once.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="startInfo">The <see cref="SequentialEnumerationStartInfo{T}"/> containing callbacks to use.</param>
      /// <returns>A new instance of <see cref="IAsyncEnumerable{T}"/> which behaves like callbacks in <paramref name="startInfo"/> specified.</returns>
      public static IAsyncEnumerable<T> CreateExclusiveSequentialEnumerable<T>(
         SequentialEnumerationStartInfo<T> startInfo,
         IAsyncProvider aLINQProvider
         ) => new AsyncEnumerableExclusive<T>( SequentialCurrentInfoFactory.GetInstance( startInfo.MoveNext, startInfo.Dispose ), aLINQProvider );

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
      /// This creates a <see cref="IAsyncEnumerable{T}"/> that will behave like the given set of delegates.
      /// Each enumeration is assumed to have inner state, meaning that the call to create delegates is done on every call to <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/>.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="startInfoFactory">The callback creating a structure containing all the delegates that the calls to methods of <see cref="IAsyncEnumerator{T}"/> will be call-through to.</param>
      /// <returns>A new <see cref="IAsyncEnumerable{T}"/>, which will call the given <paramref name="startInfoFactory"/> on each call to <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/>, and the resulting <see cref="IAsyncEnumerator{T}"/> will pass each invocation of its methods the set of delegates returned by <paramref name="startInfoFactory"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="startInfoFactory"/> is <c>null</c>.</exception>
      /// <seealso cref="WrappingEnumerationStartInfo{T}"/>
      /// <seealso cref="CreateWrappingStartInfo"/>
      public static IAsyncEnumerable<T> CreateStatefulWrappingEnumerable<T>(
         Func<WrappingEnumerationStartInfo<T>> startInfoFactory,
         IAsyncProvider aLINQProvider
         )
      {
         return new StatefulAsyncEnumerableWrapper<T>( startInfoFactory, aLINQProvider );
      }

      /// <summary>
      /// This creates a <see cref="IAsyncEnumerable{T}"/> that will behave like the given set of delegates.
      /// Each enumeration is assumed to be stateless, meaning that the same set of delegates is shared by all instances returned by <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/>.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="startInfo">The <see cref="WrappingEnumerationStartInfo{T}"/> containing delegates that capture all signatures of all methods of <see cref="IAsyncEnumerator{T}"/> interface.</param>
      /// <returns>A new <see cref="IAsyncEnumerable{T}"/>, which will share the given delegates by all instances returned by <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/>.</returns>
      /// <exception cref="ArgumentNullException">If either of <see cref="WrappingEnumerationStartInfo{T}.WaitForNext"/> or <see cref="WrappingEnumerationStartInfo{T}.TryGetNext"/> delegates of <paramref name="startInfo"/> are <c>null</c>.</exception>
      /// <seealso cref="WrappingEnumerationStartInfo{T}"/>
      /// <seealso cref="CreateWrappingStartInfo"/>
      public static IAsyncEnumerable<T> CreateStatelessWrappingEnumerable<T>(
         WrappingEnumerationStartInfo<T> startInfo,
         IAsyncProvider aLINQProvider
         )
      {
         return new StatelessAsyncEnumerableWrapper<T>( startInfo, aLINQProvider );
      }

      /// <summary>
      /// This creates a <see cref="IAsyncEnumerator{T}"/> that will behave like the given set of delegates.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="startInfo">The <see cref="WrappingEnumerationStartInfo{T}"/> containing delegates that capture all signatures of all methods of <see cref="IAsyncEnumerator{T}"/> interface.</param>
      /// <returns>A new <see cref="IAsyncEnumerator{T}"/>, which will behave like the given set of delegates.</returns>
      /// <exception cref="ArgumentNullException">If either of <see cref="WrappingEnumerationStartInfo{T}.WaitForNext"/> or <see cref="WrappingEnumerationStartInfo{T}.TryGetNext"/> delegates of <paramref name="startInfo"/> are <c>null</c>.</exception>
      /// <seealso cref="WrappingEnumerationStartInfo{T}"/>
      /// <seealso cref="CreateWrappingStartInfo"/>
      public static IAsyncEnumerator<T> CreateWrappingEnumerator<T>(
         WrappingEnumerationStartInfo<T> startInfo
         )
      {
         return new AsyncEnumeratorWrapper<T>( startInfo );
      }

      /// <summary>
      ///  Helper method to invoke constructor of <see cref="WrappingEnumerationStartInfo{T}"/> without explicitly specifying generic type arguments.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      /// <param name="waitForNext">The callback that will be used by <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/>.</param>
      /// <param name="tryGetNext">The callback that will be used by <see cref="IAsyncEnumerator{T}.TryGetNext"/>.</param>
      /// <param name="dispose">The optional callback that will be used by <see cref="IAsyncDisposable.DisposeAsync"/>.</param>
      /// <returns>A new instance of <see cref="WrappingEnumerationStartInfo{T}"/>.</returns>
      /// <exception cref="ArgumentNullException">If either of <paramref name="waitForNext"/> or <paramref name="tryGetNext"/> is <c>null</c>.</exception>
      /// <seealso cref="WrappingEnumerationStartInfo{T}"/>
      public static WrappingEnumerationStartInfo<T> CreateWrappingStartInfo<T>(
         WaitForNextDelegate waitForNext,
         TryGetNextDelegate<T> tryGetNext,
         EnumerationEndedDelegate dispose
         )
      {
         return new WrappingEnumerationStartInfo<T>( waitForNext, tryGetNext, dispose );
      }

      public static WrappingEnumerationStartInfo<T> CreateSynchronousWrappingStartInfo<T>(
         TryGetNextDelegate<T> tryGetNext,
         EnumerationEndedDelegate dispose = null
         )
      {
         const Int32 INITIAL = 0;
         const Int32 STARTED = 1;
         var state = INITIAL;
         return CreateWrappingStartInfo(
            () =>
            {
               return TaskUtils.TaskFromBoolean( Interlocked.CompareExchange( ref state, STARTED, INITIAL ) == INITIAL );
            },
            tryGetNext,
            dispose
            );

      }
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
   /// This delegate captures signature of <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/>, and is used by <see cref="WrappingEnumerationStartInfo{T}"/>.
   /// </summary>
   /// <returns>Potentially asynchronously returns value indicating whether there are more elements to be enumerated.</returns>
   public delegate Task<Boolean> WaitForNextDelegate();

   /// <summary>
   /// This delegate captures signature of <see cref="IAsyncEnumerator{T}.TryGetNext"/>, and is used by <see cref="WrappingEnumerationStartInfo{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="success">Whether this call was successful in retrieving next item.</param>
   /// <returns>The next item.</returns>
   public delegate T TryGetNextDelegate<T>( out Boolean success );

   /// <summary>
   /// This type contains a set of delegates required to mimic <see cref="IAsyncEnumerator{T}"/>.
   /// It is used by <see cref="AsyncEnumerationFactory.CreateStatefulWrappingEnumerable"/> and <see cref="AsyncEnumerationFactory.CreateStatelessWrappingEnumerable"/> methods to capture required delegates as single type.
   /// The <see cref="AsyncEnumerationFactory.CreateWrappingStartInfo"/> can be used to create instances of this type when the type arguments can be implicitly deduced.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   public struct WrappingEnumerationStartInfo<T>
   {
      /// <summary>
      /// Creates a new instance of <see cref="WrappingEnumerationStartInfo{T}"/>.
      /// </summary>
      /// <param name="waitForNext">The callback that will be used by <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/>.</param>
      /// <param name="tryGetNext">The callback that will be used by <see cref="IAsyncEnumerator{T}.TryGetNext"/>.</param>
      /// <param name="dispose">The optional callback that will be used by <see cref="IAsyncDisposable.DisposeAsync"/>.</param>
      /// <exception cref="ArgumentNullException">If either of <paramref name="waitForNext"/> or <paramref name="tryGetNext"/> is <c>null</c>.</exception>
      public WrappingEnumerationStartInfo(
         WaitForNextDelegate waitForNext,
         TryGetNextDelegate<T> tryGetNext,
         EnumerationEndedDelegate dispose
         )
      {
         this.WaitForNext = waitForNext;
         this.TryGetNext = tryGetNext;
         this.Dispose = dispose;
      }

      /// <summary>
      /// Gets the callback for <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/> method.
      /// </summary>
      /// <value>The callback for <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/> method.</value>
      /// <seealso cref="WaitForNextDelegate"/>
      public WaitForNextDelegate WaitForNext { get; }

      /// <summary>
      /// Gets the callback for <see cref="IAsyncEnumerator{T}.TryGetNext"/> method.
      /// </summary>
      /// <value>The callback for <see cref="IAsyncEnumerator{T}.TryGetNext"/> method.</value>
      /// <seealso cref="WaitForNextDelegate"/>
      public TryGetNextDelegate<T> TryGetNext { get; }

      /// <summary>
      /// Gets the callback for <see cref="IAsyncDisposable.DisposeAsync"/> method.
      /// </summary>
      /// <value>The callback for <see cref="IAsyncDisposable.DisposeAsync"/> method.</value>
      /// <seealso cref="EnumerationEndedDelegate"/>
      public EnumerationEndedDelegate Dispose { get; }
   }
}
