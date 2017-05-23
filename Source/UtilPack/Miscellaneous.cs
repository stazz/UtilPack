/*
 * Copyright 2014 Stanislav Muhametsin. All rights Reserved.
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
using System.Linq;
using System.Text;
using UtilPack;
using System.Threading;
using System.Reflection;

namespace UtilPack
{

#if SILVERLIGHT
   /// <summary>
   /// This is helper class to provide similar functionality to <see cref="T:System.Threading.ThreadLocal`1"/>, which is missing in Silverlight.
   /// </summary>
   /// <typeparam name="T">The type of the values to hold.</typeparam>
   public sealed class ThreadLocal<T>
   {
      // Helper class to hold values, so T typeparam wouldn't have any generic constraints.
      private sealed class ValueHolder
      {
         internal T _value;
         internal ValueHolder( T value )
         {
            this._value = value;
         }
      }

      private static readonly Func<T> _defaultFactory = () => default( T );

      // Table holding all instances of ThreadLocals in this thread. Since they are weak references, they should get GC'd without big issues.
      [ThreadStatic]
      private static System.Runtime.CompilerServices.ConditionalWeakTable<ThreadLocal<T>, ValueHolder> _table;

      // Factory callback
      private readonly Func<T> _factory;

      /// <summary>
      /// Creates a new instance of <see cref="ThreadLocal{T}"/> with optional factory callback.
      /// </summary>
      /// <param name="factory">The optional factory callback. If not supplied (i.e. is <c>null</c>), then a factory callback will return default value ofr type <typeparamref name="T"/>.</param>
      public ThreadLocal( Func<T> factory = null )
      {
         this._factory = factory ?? _defaultFactory;
      }

      /// <summary>
      /// Gets or sets value that this <see cref="ThreadLocal{T}"/> holds in current thread.
      /// </summary>
      /// <value>The value that this <see cref="ThreadLocal{T}"/> holds in current thread.</value>
      public T Value
      {
         get
         {
            ValueHolder holder;
            T retVal;
            if ( _table != null && _table.TryGetValue( this, out holder ) )
            {
               retVal = holder._value;
            }
            else
            {
               retVal = this._factory();
               this.Value = retVal;
            }
            return retVal;
         }
         set
         {
            if ( _table == null )
            {
               _table = new System.Runtime.CompilerServices.ConditionalWeakTable<ThreadLocal<T>, ValueHolder>();
            }
            _table.GetOrCreateValue( this )._value = value;
         }
      }
   }
#endif

   /// <summary>
   /// This delegate type is alternative for <see cref="EventHandler{TEventArgs}"/>.
   /// It differs from the <see cref="EventHandler{TEventArgs}"/> in the following three ways:
   /// <list type="bullet">
   /// <item><description>The generic argument <typeparamref name="TArgs"/> has <c>in</c> contravariance specification,</description></item>
   /// <item><description>The generic argument <typeparamref name="TArgs"/> no longer has a inheritance constraint, and</description></item>
   /// <item><description>The <c>sender</c> parameter of the <see cref="EventHandler{TEventArgs}"/> is missing.</description></item>
   /// </list>
   /// </summary>
   /// <typeparameter name="TArgs">The type of the arguments this delegate will receive.</typeparameter>
   public delegate void GenericEventHandler<in TArgs>( TArgs args );

   /// <summary>
   /// This class contains extension method which are for types not contained in this library.
   /// </summary>
   public static partial class UtilPackExtensions
   {
      /// <summary>
      /// Gets given integer as enumerable of characters.
      /// </summary>
      /// <param name="i64">This integer.</param>
      /// <returns>Enumerable of characters that are textual representation of this integer.</returns>
      public static IEnumerable<Char> AsCharEnumerable( this Int64 i64 )
      {
         if ( i64 == 0 )
         {
            yield return '0';
         }
         else
         {
            if ( i64 < 0 )
            {
               yield return '-';
               i64 = Math.Abs( i64 );
            }

            var div = 1;
            var original = i64;
            while ( ( i64 /= 10 ) > 0 )
            {
               div *= 10;
            }

            while ( div > 0 )
            {
               yield return (Char) ( original / div + '0' );
               original %= div;
               div /= 10;
            }
         }
      }

      /// <summary>
      /// Helper method to return string as enumerable of characters.
      /// </summary>
      /// <param name="str">This <see cref="String"/>. May be <c>null</c>, then empty enumerable is returned.</param>
      public static IEnumerable<Char> AsCharEnumerable( this String str )
      {
         if ( str != null )
         {
            var max = str.Length;
            for ( var i = 0; i < max; ++i )
            {
               yield return str[i];
            }
         }
      }
   }

   /// <summary>
   /// This class holds reference to <see cref="Func{T, TResult}"/> which directly returns the given argument, i.e. identity function.
   /// </summary>
   /// <typeparam name="T">The type of argument and return value of callback.</typeparam>
   public static class Identity<T>
   {
      /// <summary>
      /// Gets the identity function for type <typeparamref name="T"/>.
      /// </summary>
      /// <value>The identity function for type <typeparamref name="T"/>.</value>
      public static Func<T, T> Function { get; }

      static Identity()
      {
         Function = item => item;
      }
   }

   /// <summary>
   /// This struct captures situation when result can be unavailable (due to error or something else).
   /// </summary>
   /// <typeparam name="TResult">The type of the result.</typeparam>
   public struct ResultOrNone<TResult>
   {
      /// <summary>
      /// Creates a new <see cref="ResultOrNone{TResult}"/> with given result.
      /// </summary>
      /// <param name="result">The result.</param>
      public ResultOrNone( TResult result )
      {
         this.HasResult = true;
         this.Result = result;
      }

      /// <summary>
      /// Gets the result, or returns default for <typeparamref name="TResult"/> if no result has been specified.
      /// </summary>
      /// <value>The result that was given, or default ofr <typeparamref name="TResult"/>.</value>
      /// <seealso cref="HasResult"/>
      public TResult Result { get; }

      /// <summary>
      /// Gets the value indicating whether this <see cref="ResultOrNone{TResult}"/> was given a result.
      /// This is useful to separate scenario when the result was given, but it is actually default for <typeparamref name="TResult"/>.
      /// </summary>
      /// <value>The value indicating whether this <see cref="ResultOrNone{TResult}"/> was given a result.</value>
      public Boolean HasResult { get; }

      /// <summary>
      /// Implicitly casts the given result object to <see cref="ResultOrNone{TResult}"/>,
      /// </summary>
      /// <param name="result">The result.</param>
      public static implicit operator ResultOrNone<TResult>( TResult result )
      {
         return new ResultOrNone<TResult>( result );
      }

      /// <summary>
      /// Explicitly casts the given <see cref="ResultOrNone{TResult}"/> to result.
      /// Will throw, if the <paramref name="resultOrNone"/> does not have result specified.
      /// </summary>
      /// <param name="resultOrNone">The <see cref="ResultOrNone{TResult}"/>.</param>
      /// <exception cref="InvalidOperationException">If <paramref name="resultOrNone"/> does not have result specified (its <see cref="ResultOrNone{TResult}.HasResult"/> property returns <c>false</c>).</exception>
      public static explicit operator TResult( ResultOrNone<TResult> resultOrNone )
      {
         return resultOrNone.GetResultOrThrow();
      }
   }

   /// <summary>
   /// This struct captures the value which can be one of two mutually exclusive types.
   /// </summary>
   /// <typeparam name="T1">The type for first possible value.</typeparam>
   /// <typeparam name="T2">The type for second possible value.</typeparam>
   public struct EitherOr<T1, T2>
   {
      private readonly T1 _first;
      private readonly T2 _second;

      /// <summary>
      /// Creates a new instance of <see cref="EitherOr{T1, T2}"/> bound to value of the first type <typeparamref name="T1"/>.
      /// </summary>
      /// <param name="first">The first value.</param>
      public EitherOr( T1 first )
      {
         this._first = first;
         this._second = default( T2 );
         this.IsFirst = true;
         this.IsSecond = false;
      }

      /// <summary>
      /// Creates a new instance of <see cref="EitherOr{T1, T2}"/> bound to value of the second type <typeparamref name="T2"/>.
      /// </summary>
      /// <param name="second">The second value.</param>
      public EitherOr( T2 second )
      {
         this._first = default( T1 );
         this._second = second;
         this.IsFirst = false;
         this.IsSecond = true;
      }

      /// <summary>
      /// Gets the value of the type <typeparamref name="T1"/>, or throws an exception if this <see cref="EitherOr{T1, T2}"/> is not bound to first type.
      /// </summary>
      /// <value>The value of the type <typeparamref name="T1"/>.</value>
      /// <exception cref="InvalidOperationException">If this <see cref="EitherOr{T1, T2}"/> is not bound to first type (the <see cref="IsFirst"/> returns <c>false</c>).</exception>
      public T1 First
      {
         get
         {
            if ( !this.IsFirst )
            {
               throw new InvalidOperationException( "The first value is not accessible" );
            }

            return this._first;
         }
      }

      /// <summary>
      /// Gets the value of the type <typeparamref name="T2"/>, or throws an exception if this <see cref="EitherOr{T1, T2}"/> is not bound to second type.
      /// </summary>
      /// <value>The value of the type <typeparamref name="T2"/>.</value>
      /// <exception cref="InvalidOperationException">If this <see cref="EitherOr{T1, T2}"/> is not bound to second type (the <see cref="IsSecond"/> returns <c>false</c>).</exception>
      public T2 Second
      {
         get
         {
            if ( !this.IsSecond )
            {
               throw new InvalidOperationException( "The second value is not accessible" );
            }

            return this._second;
         }
      }

      /// <summary>
      /// Gets the value indicating whether this <see cref="EitherOr{T1, T2}"/> is bound to first type <typeparamref name="T1"/>.
      /// </summary>
      /// <value>The value indicating whether this <see cref="EitherOr{T1, T2}"/> is bound to first type <typeparamref name="T1"/>.</value>
      public Boolean IsFirst { get; }

      /// <summary>
      /// Gets the value indicating whether this <see cref="EitherOr{T1, T2}"/> is bound to second type <typeparamref name="T2"/>.
      /// </summary>
      /// <value>The value indicating whether this <see cref="EitherOr{T1, T2}"/> is bound to second type <typeparamref name="T2"/>.</value>
      public Boolean IsSecond { get; }

      /// <summary>
      /// Performs implicit conversion of the value of the first type <typeparamref name="T1"/> to <see cref="EitherOr{T1, T2}"/> bound to the first type.
      /// </summary>
      /// <param name="first">The value of the first type <typeparamref name="T1"/>.</param>
      public static implicit operator EitherOr<T1, T2>( T1 first )
      {
         return new EitherOr<T1, T2>( first );
      }

      /// <summary>
      /// Performs implicit conversion of the value of the second type <typeparamref name="T2"/> to <see cref="EitherOr{T1, T2}"/> bound to the second type.
      /// </summary>
      /// <param name="second">The value of the second type <typeparamref name="T2"/>.</param>
      public static implicit operator EitherOr<T1, T2>( T2 second )
      {
         return new EitherOr<T1, T2>( second );
      }

      /// <summary>
      /// Performs explicit conversion of the <see cref="EitherOr{T1, T2}"/> to the value of the first type <typeparamref name="T1"/>.
      /// </summary>
      /// <param name="either">The <see cref="EitherOr{T1, T2}"/>.</param>
      /// <exception cref="InvalidOperationException">If <paramref name="either"/> is not bound to first type (its <see cref="IsFirst"/> returns <c>false</c>).</exception>
      public static explicit operator T1( EitherOr<T1, T2> either )
      {
         return either.First;
      }

      /// <summary>
      /// Performs explicit conversion of the <see cref="EitherOr{T1, T2}"/> to the value of the second type <typeparamref name="T2"/>.
      /// </summary>
      /// <param name="either">The <see cref="EitherOr{T1, T2}"/>.</param>
      /// <exception cref="InvalidOperationException">If <paramref name="either"/> is not bound to second type (its <see cref="IsSecond"/> returns <c>false</c>).</exception>
      public static explicit operator T2( EitherOr<T1, T2> either )
      {
         return either.Second;
      }
   }

#if IS_NETSTANDARD
   /// <summary>
   /// This class extends <see cref="EventArgs"/> to provide a list of awaitable objects which will be awaited after event invokation is done.
   /// </summary>
   /// <typeparam name="TAwaitable">The type of the awaitable.</typeparam>
   /// <remarks>
   /// Because of how <c>public async void</c> methods behave, they only make sense when they are run in UI thread or similar (otherwise, they will cause hard crash, <see href="https://msdn.microsoft.com/en-us/magazine/jj991977.aspx"/>).
   /// </remarks>
   public class EventArgsWithAsyncContext<TAwaitable> : EventArgs
   {
      private List<TAwaitable> _awaitables;

      /// <summary>
      /// Creates a new instance of <see cref="EventArgsWithAsyncContext{TAwaitable}"/>
      /// </summary>
      public EventArgsWithAsyncContext()
      {
         this._awaitables = null;
      }

      /// <summary>
      /// Gets the mutable list of awaitables.
      /// </summary>
      public List<TAwaitable> Awaitables
      {
         get
         {
            if ( this._awaitables == null )
            {
               this._awaitables = new List<TAwaitable>();
            }
            return this._awaitables;
         }
      }

      /// <summary>
      /// Gets the array of awaitables added to <see cref="Awaitables"/> list, or <c>null</c> if no awaitables have been added.
      /// </summary>
      /// <returns>The array of awaitables added to <see cref="Awaitables"/> list, or <c>null</c> if no awaitables have been added.</returns>
      public TAwaitable[] GetAwaitableArray()
      {
         var list = this._awaitables;
         return ( list?.Count ?? 0 ) > 0 ? list.ToArray() : null;
      }
   }

   /// <summary>
   /// This class restricts the type argument of <see cref="EventArgsWithAsyncContext{TAwaitable}"/> to <see cref="System.Threading.Tasks.Task"/>.
   /// </summary>
   public class EventArgsWithAsyncContext : EventArgsWithAsyncContext<System.Threading.Tasks.Task>
   {

   }

   /// <summary>
   /// The .NET Standard library does not have Task.CompletedTask property (at least for all versions).
   /// Hence, this class provides it.
   /// </summary>
   public static class TaskUtils
   {
      /// <summary>
      /// Gets the task which is completed.
      /// </summary>
      public static System.Threading.Tasks.Task CompletedTask { get; }

      static TaskUtils()
      {
         var src = new System.Threading.Tasks.TaskCompletionSource<Object>();
         src.SetResult( null );

         CompletedTask = src.Task;
      }
   }

   /// <summary>
   /// This interface provides a method which performs disposing of some resource asynchronously.
   /// </summary>
   public interface IAsyncDisposable
   {
      /// <summary>
      /// Performs the disposing of some resource asynchrnonously, with given optional <see cref="CancellationToken"/>.
      /// </summary>
      /// <param name="token">The optional <see cref="CancellationToken"/>.</param>
      /// <returns>The task to await for.</returns>
      System.Threading.Tasks.Task DisposeAsync( CancellationToken token = default( CancellationToken ) );
   }

   /// <summary>
   /// Provides support for asynchronous lazy initialization. This type is fully threadsafe.
   /// The class is from <see href="https://blog.stephencleary.com/2012/08/asynchronous-lazy-initialization.html"/> with few small modifications.
   /// </summary>
   /// <typeparam name="T">The type of object that is being asynchronously initialized.</typeparam>
   public sealed class AsyncLazy<T>
   {
      private readonly Lazy<System.Threading.Tasks.Task<T>> _instance;

      /// <summary>
      /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class.
      /// </summary>
      /// <param name="factory">The delegate that is invoked on a background thread to produce the value when it is needed.</param>
      public AsyncLazy( Func<T> factory )
         : this( () => System.Threading.Tasks.Task.Run( factory ) )
      {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class.
      /// </summary>
      /// <param name="asyncFactory">The asynchronous delegate that is invoked on a background thread to produce the value when it is needed.</param>
      public AsyncLazy( Func<System.Threading.Tasks.Task<T>> asyncFactory )
      {
         this._instance = new Lazy<System.Threading.Tasks.Task<T>>( () => System.Threading.Tasks.Task.Run( asyncFactory ), LazyThreadSafetyMode.ExecutionAndPublication );
      }

      /// <summary>
      /// Asynchronous infrastructure support. This method permits instances of <see cref="AsyncLazy{T}"/> to be awaited.
      /// </summary>
      public System.Runtime.CompilerServices.TaskAwaiter<T> GetAwaiter()
      {
         return this._instance.Value.GetAwaiter();
      }

      /// <summary>
      /// Starts the asynchronous initialization, if it has not already started.
      /// </summary>
      public void Start()
      {
         var unused = this._instance.Value;
      }
   }

   /// <summary>
   /// This class combines the asynchronousness of <see cref="AsyncLazy{T}"/> with the resettability of <see cref="ReadOnlyResettableLazy{T}"/>.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   public sealed class ReadOnlyResettableAsyncLazy<T>
   {
      // Possible state transitions
      // Initial -> Started
      // Started -> Completed
      // Started -> Resetting
      // Completed -> Resetting
      // Resetting -> Initial
      //private const Int32 INITIAL = 0;
      //private const Int32 STARTED = 1;
      //private const Int32 COMPLETED = 2;
      //private const Int32 RESETTING = 3;

      //private readonly Func<System.Threading.Tasks.Task<T>> _factory;
      private ReadOnlyResettableLazy<System.Threading.Tasks.Task<T>> _instance;
      //private Int32 _state;


      /// <summary>
      /// Initializes a new instance of the <see cref="ReadOnlyResettableAsyncLazy{T}"/> class.
      /// </summary>
      /// <param name="factory">The delegate that is invoked on a background thread to produce the value when it is needed.</param>
      public ReadOnlyResettableAsyncLazy( Func<T> factory )
         : this( () => System.Threading.Tasks.Task.Run( factory ) )
      {

      }

      /// <summary>
      /// Initializes a new instance of the <see cref="ReadOnlyResettableAsyncLazy{T}"/> class.
      /// </summary>
      /// <param name="asyncFactory">The asynchronous delegate that is invoked on a background thread to produce the value when it is needed.</param>
      public ReadOnlyResettableAsyncLazy( Func<System.Threading.Tasks.Task<T>> asyncFactory )
      {
         //this._factory = async () =>
         //{
         //   // First, wait for the reset to complete
         //   Interlocked.CompareExchange( ref this._state, STARTED, INITIAL );
         //   while ( this._state == RESETTING )
         //   {
         //      await System.Threading.Tasks.Task.Delay( tick );
         //   }
         //   asyncFactory
         //};
         this._instance = new ReadOnlyResettableLazy<System.Threading.Tasks.Task<T>>( asyncFactory, LazyThreadSafetyMode.ExecutionAndPublication );
      }

      /// <summary>
      /// Asynchronous infrastructure support. This method permits instances of <see cref="ReadOnlyResettableAsyncLazy{T}"/> to be awaited.
      /// </summary>
      public System.Runtime.CompilerServices.TaskAwaiter<T> GetAwaiter()
      {
         return this._instance.Value.GetAwaiter();
      }

      /// <summary>
      /// Starts the asynchronous initialization, if it has not already started.
      /// </summary>
      public void Start()
      {
         var unused = this._instance.Value;
      }

      /// <summary>
      /// Resets this <see cref="ReadOnlyResettableAsyncLazy{T}"/>.
      /// </summary>
      public void Reset()
      {
         this._instance.Reset();
         //if ( Interlocked.CompareExchange( ref this._state, RESETTING, STARTED ) == STARTED
         //   || Interlocked.CompareExchange( ref this._state, RESETTING, COMPLETED ) == COMPLETED )
         //{
         //   // We have captured the correct transitions (started -> resetting, or completed -> resetting)
         //   try
         //   {
         //      this._instance = CreateLazy( this._factory );
         //   }
         //   finally
         //   {
         //      Interlocked.Exchange( ref this._state, INITIAL );
         //   }
         //}
      }

      //private static Lazy<System.Threading.Tasks.Task<T>> CreateLazy( Func<System.Threading.Tasks.Task<T>> asyncFactory )
      //{
      //   return new Lazy<System.Threading.Tasks.Task<T>>( () => System.Threading.Tasks.Task.Run( asyncFactory ), LazyThreadSafetyMode.ExecutionAndPublication );
      //}

      //private async System.Threading.Tasks.Task WaitForResetToComplete( Int32 tick = 50 )
      //{
      //   Interlocked.CompareExchange( ref this._state, STARTED, INITIAL );
      //   while ( this._state == RESETTING )
      //   {
      //      await System.Threading.Tasks.Task.Delay( tick );
      //   }
      //}
   }

   internal sealed class TransitionableState<TState>
   {
      public TransitionableState( TState value )
      {
         this.Value = value;
      }

      public TState Value { get; }
   }

   /// <summary>
   /// This class performs tasks related to objects to "change type", typically on first access (e.g. from <see cref="Nullable{T}"/> to actual object).
   /// Furthermore, this class supports the transition which is done asynchronously, unlike <see cref="Lazy{T}"/>.
   /// </summary>
   /// <typeparam name="T1">The type of the initial value.</typeparam>
   /// <typeparam name="T2">The type of the transformed value.</typeparam>
   public class Transformable<T1, T2>
   {
      // TODO this class needs better API (.TryGetOriginal, .TryGetTransformed) and may as well be completely replaced by async lazy.
      internal Object _state;
      internal TransitionableState<T1> _originalState;

      /// <summary>
      /// Creates a new instance of <see cref="Transformable{T1, T2}"/> with given original value.
      /// </summary>
      /// <param name="value">The value of the original type.</param>
      public Transformable( T1 value )
      {
         this._originalState = new TransitionableState<T1>( value );
         this._state = this._originalState;
      }

      /// <summary>
      /// Gets the original value, or throws an exception if original value is no longer available.
      /// </summary>
      /// <value>The original value, or throws an exception if original value is no longer available.</value>
      /// <exception cref="InvalidOperationException">If original value is no longer available, because of asynchronous transition started, or possibly asynchronous transition completed.</exception>
      public T1 Original
      {
         get
         {
            var state = this._state;

            if ( state == null || !ReferenceEquals( state, this._originalState ) )
            {
               throw new InvalidOperationException( "The original value is not accessible" );
            }

            return ( (TransitionableState<T1>) state ).Value;
         }
      }

      /// <summary>
      /// Gets the transformed value, or throws an exception if transformed value is not available yet.
      /// </summary>
      /// <value>The transformed value, or throws an exception if transformed value is not available yet.</value>
      /// <exception cref="InvalidOperationException">If transformed value is not yet available.</exception>
      public T2 Transformed
      {
         get
         {
            var state = this._state;
            if ( state == null || ReferenceEquals( state, this._originalState ) )
            {
               throw new InvalidOperationException( "The transformed value is not accessible" );
            }

            return ( (TransitionableState<T2>) state ).Value;
         }
      }

      /// <summary>
      /// Gets the value indicating whether this <see cref="Transformable{T1, T2}"/> is of original value.
      /// </summary>
      /// <value>The value indicating whether this <see cref="Transformable{T1, T2}"/> is of original value.</value>
      public Boolean IsOriginal
      {
         get
         {
            return ReferenceEquals( this._originalState, this._state );
         }
      }

      /// <summary>
      /// Gets the value indicating whether this <see cref="Transformable{T1, T2}"/> is of transformed value.
      /// </summary>
      /// <value>The value indicating whether this <see cref="Transformable{T1, T2}"/> is of transformed value.</value>
      public Boolean IsTransformed
      {
         get
         {
            var curState = this._state;
            return !ReferenceEquals( this._originalState, curState ) && curState != null;
         }
      }

      /// <summary>
      /// Tries synchronous transition from original value to the transformed value returned by given lambda.
      /// </summary>
      /// <param name="transitioner">The lambda to return transitioned value.</param>
      /// <returns><c>true</c> if transformation from original to the value returned by lambda was successful; <c>false</c> otherwise.</returns>
      public Boolean TryTransition( Func<T1, T2> transitioner )
      {
         var oldState = this._state;
         return ReferenceEquals( oldState, this._originalState )
            && ReferenceEquals( Interlocked.CompareExchange( ref this._state, new TransitionableState<T2>( transitioner( ( (TransitionableState<T1>) oldState ).Value ) ), oldState ), oldState );
      }

      /// <summary>
      /// Tries asynchronous transition from original value to the transformed value returned by given asynchronous lambda.
      /// </summary>
      /// <param name="transitioner">The asynchronous lambda to return transitioned value.</param>
      /// <returns><c>true</c> if transformation from original to the value returned by lambda was successful; <c>false</c> otherwise.</returns>
      public async System.Threading.Tasks.Task<Boolean> TryTransitionAsync( Func<T1, System.Threading.Tasks.Task<T2>> transitioner )
      {
         var oldState = this._state;
         var captured = ReferenceEquals( oldState, this._originalState )
            && ReferenceEquals( Interlocked.CompareExchange( ref this._state, null, oldState ), oldState );
         if ( captured )
         {
            // We have captured the transition
            var completed = false;
            try
            {
               Interlocked.CompareExchange( ref this._state, new TransitionableState<T2>( await transitioner( ( (TransitionableState<T1>) oldState ).Value ) ), null );
               completed = true;
            }
            finally
            {
               if ( !completed )
               {
                  // Try revert back to old value
                  Interlocked.CompareExchange( ref this._state, this._originalState, null );
               }
            }
         }

         return captured;
      }


   }

   /// <summary>
   /// Augments the <see cref="Transformable{T1, T2}"/> class with possibility of resetting back to original value.
   /// </summary>
   /// <typeparam name="T1">The type of the initial value.</typeparam>
   /// <typeparam name="T2">The type of the transformed value.</typeparam>
   public class ResettableTransformable<T1, T2> : Transformable<T1, T2>
   {
      /// <summary>
      /// Creates a new instance of <see cref="ResettableTransformable{T1, T2}"/> with given original value.
      /// </summary>
      /// <param name="value">The value of the original type.</param>
      public ResettableTransformable(
         T1 value
         )
         : base( value )
      {
      }

      /// <summary>
      /// Tries to reset the current value back to the original value.
      /// </summary>
      /// <returns><c>true</c> if reset was successful; <c>false</c> otherwise.</returns>
      public Boolean TryReset()
      {
         var oldState = this._state;
         return
            !ReferenceEquals( oldState, this._originalState )
            && ReferenceEquals( Interlocked.CompareExchange( ref this._state, this._originalState, oldState ), oldState );
      }
   }

#endif

}

public static partial class E_UtilPack
{
   private const Int32 NO_TIMEOUT = -1;
   private const Int32 DEFAULT_TICK = 50;

#if IS_NETSTANDARD

   /// <summary>
   /// Helper method to call <see cref="IAsyncDisposable.DisposeAsync(CancellationToken)"/> method and ignore any exception thrown.
   /// </summary>
   /// <param name="disposable">The <see cref="IAsyncDisposable"/>. May be <c>null</c>.</param>
   /// <param name="token">The optional cancellation token to pass to <see cref="IAsyncDisposable.DisposeAsync(CancellationToken)"/>.</param>
   /// <returns>The task to await for <see cref="IAsyncDisposable.DisposeAsync(CancellationToken)"/> to end.</returns>
   public static async System.Threading.Tasks.Task DisposeAsyncSafely( this IAsyncDisposable disposable, CancellationToken token = default( CancellationToken ) )
   {
      if ( disposable != null )
      {
         try
         {
            await ( disposable.DisposeAsync( token ) ?? TaskUtils.CompletedTask );
         }
         catch
         {
            // Ignore
         }
      }
   }

   /// <summary>
   /// Checks whether this <see cref="Transformable{T1, T2}"/> is in middle of asynchronous transition.
   /// </summary>
   /// <typeparam name="T1">The type of the initial value.</typeparam>
   /// <typeparam name="T2">The type of the transformed value.</typeparam>
   /// <param name="transformable">This <see cref="Transformable{T1, T2}"/>.</param>
   /// <returns><c>true</c> if transformable is in middle of asynchronous transition; <c>false</c> otherwise.</returns>
   public static Boolean IsMidTransition<T1, T2>( this Transformable<T1, T2> transformable )
   {
      return transformable != null && !transformable.IsOriginal && !transformable.IsTransformed;
   }

   /// <summary>
   /// Helper method to try asynchronous transition, or wait till it is complete, if it is invoked elsewhere.
   /// </summary>
   /// <typeparam name="T1">The type of the initial value.</typeparam>
   /// <typeparam name="T2">The type of the transformed value.</typeparam>
   /// <param name="transformable">This <see cref="Transformable{T1, T2}"/>.</param>
   /// <param name="transitioner">The lambda to perform transition.</param>
   /// <returns>The task to await to.</returns>
   public static async System.Threading.Tasks.Task TryTransitionOrWaitAsync<T1, T2>( this Transformable<T1, T2> transformable, Func<T1, System.Threading.Tasks.Task<T2>> transitioner )
   {
      if ( transformable.IsOriginal )
      {
         await transformable.TryTransitionAsync( transitioner );
      }

      if ( transformable.IsMidTransition() )
      {
         using ( var mres = new ManualResetEventSlim( false ) )
         {
            while ( transformable.IsMidTransition() )
            {
               mres.Wait( DEFAULT_TICK );
            }
         }
      }
   }

#endif

   /// <summary>
   /// Disposes the given <paramref name="disposable"/> without leaking any exceptions.
   /// </summary>
   /// <param name="disposable">The <see cref="IDisposable"/> to call <see cref="IDisposable.Dispose"/> method on. May be <c>null</c>, in which case, nothing is done.</param>
   public static void DisposeSafely( this IDisposable disposable )
   {
      if ( disposable != null )
      {
         try
         {
            disposable.Dispose();
         }
         catch
         {
            // Ignore
         }
      }
   }

   /// <summary>
   /// Disposes the given <paramref name="disposable"/> without leaking any exceptions, but giving out occurred exception, if any.
   /// </summary>
   /// <param name="disposable">The <see cref="IDisposable"/> to call <see cref="IDisposable.Dispose"/> method on. May be <c>null</c>, in which case, nothing is done.</param>
   /// <param name="exception">Will hold an exception thrown by <see cref="IDisposable.Dispose"/> method, if method is invoked and it throws.</param>
   /// <returns><c>true</c> if NO exception occurred; <c>false</c> otherwise.</returns>
   public static Boolean DisposeSafely( this IDisposable disposable, out Exception exception )
   {
      exception = null;
      if ( disposable != null )
      {
         try
         {
            disposable.Dispose();
         }
         catch ( Exception exc )
         {
            exception = exc;
         }
      }

      return exception == null;
   }

   /// <summary>
   /// Checks whether given nullable boolean has value and that value is <c>true</c>.
   /// </summary>
   /// <param name="nullable">Nullable boolean.</param>
   /// <returns><c>true</c> if <paramref name="nullable"/> has value and that value is <c>true</c>; <c>false</c> otherwise.</returns>
   public static Boolean IsTrue( this Boolean? nullable )
   {
      return nullable.HasValue && nullable.Value;
   }

   /// <summary>
   /// Checks whether given nullable boolean has value and that value is <c>false</c>.
   /// </summary>
   /// <param name="nullable">Nullable boolean.</param>
   /// <returns><c>true</c> if <paramref name="nullable"/> has value and that value is <c>false</c>; <c>false</c> otherwise.</returns>
   public static Boolean IsFalse( this Boolean? nullable )
   {
      return nullable.HasValue && !nullable.Value;
   }

   /// <summary>
   /// Checks that string is non-<c>null</c> and equivalent to <see cref="Boolean.TrueString"/>.
   /// </summary>
   /// <param name="str">The string to check.</param>
   /// <returns><c>true</c> if <paramref name="str"/> is equivalent to <see cref="Boolean.TrueString"/>; <c>false</c> otherwise.</returns>
   /// <remarks>
   /// This should be a no-throw method.
   /// </remarks>
   public static Boolean ParseAsBooleanSafe( this String str )
   {
      return Boolean.TryParse( str, out Boolean parsedBoolean ) && parsedBoolean;
   }

   /// <summary>
   /// Helper method to return result of <see cref="Object.ToString"/> or other string if object is <c>null</c>.
   /// </summary>
   /// <param name="obj">The object.</param>
   /// <param name="nullString">The string to return if <paramref name="obj"/> is <c>null</c>.</param>
   /// <returns>The result of <see cref="Object.ToString"/> if <paramref name="obj"/> is not <c>null</c>, <paramref name="nullString"/> otherwise.</returns>
   public static String ToStringSafe<T>( this T obj, String nullString = "" )
      where T : class
   {
      return obj == null ? nullString : obj.ToString();
   }

   /// <summary>
   /// Helper method to return string value of <see cref="Nullable{T}.Value"/> or custom string if the nullable does not have a value.
   /// </summary>
   /// <typeparam name="T">The nullable value type.</typeparam>
   /// <param name="obj">The nullable struct.</param>
   /// <param name="nullString">The string to return if <paramref name="obj"/> does not have a value.</param>
   /// <returns>The string of the nullable value or <paramref name="nullString"/> if <paramref name="obj"/> does not have a value.</returns>
   public static String ToStringSafe<T>( this T? obj, String nullString = "" )
      where T : struct
   {
      return obj.HasValue ? obj.Value.ToString() : nullString;
   }

   /// <summary>
   /// Helper method to return result of <see cref="Object.GetHashCode"/> or custom hash code if object is <c>null</c>.
   /// </summary>
   /// <param name="obj">The object.</param>
   /// <param name="nullHashCode">The hash code to return if <paramref name="obj"/> is <c>null</c>.</param>
   /// <returns>The result of <see cref="Object.GetHashCode"/> if <paramref name="obj"/> is not <c>null</c>, <paramref name="nullHashCode"/> otherwise.</returns>
   public static Int32 GetHashCodeSafe<T>( this T obj, Int32 nullHashCode = 0 )
      where T : class
   {
      return obj == null ? nullHashCode : obj.GetHashCode();
   }

   /// <summary>
   /// Helper method to return hash code of <see cref="Nullable{T}.Value"/> or custom hash code if the nullable does not have a value.
   /// </summary>
   /// <typeparam name="T">The nullable value type.</typeparam>
   /// <param name="obj">The nullable struct.</param>
   /// <param name="nullHashCode">The hash code to return if <paramref name="obj"/> does not have a value.</param>
   /// <returns>The hash code of the nullable value or <paramref name="nullHashCode"/> if <paramref name="obj"/> does not have a value.</returns>
   public static Int32 GetHashCodeSafe<T>( this T? obj, Int32 nullHashCode = 0 )
      where T : struct
   {
      return obj.HasValue ? obj.Value.GetHashCode() : nullHashCode;
   }

   /// <summary>
   /// Helper method to get the type of object or <c>null</c> if object is <c>null</c>.
   /// </summary>
   /// <typeparam name="T">The type of object reference.</typeparam>
   /// <param name="obj">The object.</param>
   /// <returns>The type of <paramref name="obj"/>, or <c>null</c> if <paramref name="obj"/> is <c>null</c>.</returns>
   public static Type GetTypeSafe<T>( this T obj )
      where T : class
   {
      return obj?.GetType();
   }

   /// <summary>
   /// Helper method to get the type of <see cref="Nullable{T}.Value"/> or <c>null</c> if the nullable does not have a value.
   /// </summary>
   /// <typeparam name="T">The nullable value type.</typeparam>
   /// <param name="obj">The nullable struct.</param>
   /// <returns>The type of nullable value, or <c>null</c> if <paramref name="obj"/> does not have a value.</returns>
   public static Type GetTypeSafe<T>( this T? obj )
      where T : struct
   {
      return obj.HasValue ? obj.Value.GetType() : null;
   }

   /// <summary>
   /// Tries to interpret this character as hexadecimal character (0-9, A-F, or a-f), and return the hexadecimal value.
   /// </summary>
   /// <param name="c">This character.</param>
   /// <returns>The hexadecimal value (<c>0 ≤ return_value ≤ 16</c>), or <c>null</c> if this character is not a hexadecimal character.</returns>
   public static Int32? GetHexadecimalValue( this Char c )
   {
      if ( c >= '0' && c <= '9' )
      {
         // Assume '0'-'9'
         return c - '0';
      }
      else if ( c >= 'A' && c <= 'F' )
      {
         // Assume 'A'-'F'
         return c - '7';
      }
      else if ( c >= 'a' && c <= 'f' )
      {
         // Assume 'a'-'f'
         return c - 'W';
      }
      else
      {
         return null;
      }
   }

   ///// <summary>
   ///// Helper method to get the value from nullable, if it has a value, or return default value for type, if the nullable does not have a value.
   ///// </summary>
   ///// <typeparam name="T">The nullable type.</typeparam>
   ///// <param name="nullable">The nullable value.</param>
   ///// <returns><see cref="Nullable{T}.Value"/> if <see cref="Nullable{T}.HasValue"/> is <c>true</c> for <paramref name="nullable"/>; otherwise default value for <typeparamref name="T"/>.</returns>
   //public static T GetValueOrDefault<T>( this T? nullable )
   //   where T : struct
   //{
   //   return nullable.HasValue ? nullable.Value : default( T );
   //}


   /// <summary>
   /// This is helper method to wait for specific <see cref="WaitHandle"/>, while keeping an eye for cancellation signalled through optional <see cref="CancellationToken"/>.
   /// </summary>
   /// <param name="evt">The <see cref="WaitHandle"/> to wait for.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use when checking for cancellation.</param>
   /// <param name="timeout">The optional maximum time to wait. By default no timeout.</param>
   /// <param name="tick">The optional tick, in milliseconds, between checks for <paramref name="evt"/>. By default is 50 milliseconds.</param>
   /// <returns><c>true</c> if given <paramref name="evt"/> was set during waiting perioud; <c>false</c> otherwise.</returns>
   public static Boolean WaitWhileKeepingEyeForCancel( this WaitHandle evt, CancellationToken token = default( CancellationToken ), Int32 timeout = NO_TIMEOUT, Int32 tick = DEFAULT_TICK )
   {
      Int32 timeWaited = 0;
      while ( !token.IsCancellationRequested && !evt.WaitOne( tick ) && ( timeout < 0 || timeWaited < timeout ) )
      {
         timeWaited += tick;
      }
      return !token.IsCancellationRequested && ( timeout < 0 || timeWaited < timeout );
   }
#if !SILVERLIGHT

   /// <summary>
   /// This is helper method to wait for specific <see cref="ManualResetEventSlim"/>, while keeping an eye for cancellation signalled through optional <see cref="CancellationToken"/>.
   /// </summary>
   /// <param name="evt">The <see cref="ManualResetEventSlim"/> to wait for.</param>
   /// <param name="token">The optional <see cref="CancellationToken"/> to use when checking for cancellation.</param>
   /// <param name="timeout">The optional maximum time to wait. By default no timeout.</param>
   /// <param name="tick">The optional tick, in milliseconds, between checks for <paramref name="evt"/>. By default is 50 milliseconds.</param>
   /// <returns><c>true</c> if given <paramref name="evt"/> was set during waiting perioud; <c>false</c> otherwise.</returns>
   /// <remarks>
   /// Unlike <see cref="ManualResetEventSlim.Wait(Int32, CancellationToken)"/>, this method will not throw <see cref="OperationCanceledException"/> if cancellation is requested. Instead, this method will return <c>false</c>.
   /// </remarks>
   public static Boolean WaitWhileKeepingEyeForCancel( this ManualResetEventSlim evt, CancellationToken token, Int32 timeout = NO_TIMEOUT, Int32 tick = DEFAULT_TICK )
   {
      Int32 timeWaited = 0;
      while ( !token.IsCancellationRequested && !evt.Wait( tick ) && ( timeout < 0 || timeWaited < timeout ) )
      {
         timeWaited += tick;
      }
      return !token.IsCancellationRequested && ( timeout < 0 || timeWaited < timeout );
   }

#endif

   /// <summary>
   /// This is utility method to truncate given <see cref="DateTime"/> to a certain precision.
   /// </summary>
   /// <param name="dateTime">The <see cref="DateTime"/> to truncate.</param>
   /// <param name="timeSpan">The precision to use.</param>
   /// <returns>Truncated <see cref="DateTime"/>.</returns>
   /// <remarks>
   /// The code is from <see href="http://stackoverflow.com/questions/1004698/how-to-truncate-milliseconds-off-of-a-net-datetime"/>.
   /// </remarks>
   public static DateTime Truncate( this DateTime dateTime, TimeSpan timeSpan )
   {
      return TimeSpan.Zero == timeSpan ? dateTime : dateTime.AddTicks( -( dateTime.Ticks % timeSpan.Ticks ) );
   }

   /// <summary>
   /// Helper method to call <see cref="Truncate"/> with required argument for truncating given <see cref="DateTime"/> to whole milliseconds.
   /// </summary>
   /// <param name="dateTime">The <see cref="DateTime"/> to truncate.</param>
   /// <returns>a <see cref="DateTime"/> truncated to whole milliseconds.</returns>
   public static DateTime TruncateToWholeMilliseconds( this DateTime dateTime )
   {
      return dateTime.Truncate( TimeSpan.FromTicks( TimeSpan.TicksPerMillisecond ) );
   }

   /// <summary>
   /// Helper method to call <see cref="Truncate"/> with required argument for truncating given <see cref="DateTime"/> to whole seconds.
   /// </summary>
   /// <param name="dateTime">The <see cref="DateTime"/> to truncate.</param>
   /// <returns>a <see cref="DateTime"/> truncated to whole seconds.</returns>
   public static DateTime TruncateToWholeSeconds( this DateTime dateTime )
   {
      return dateTime.Truncate( TimeSpan.FromTicks( TimeSpan.TicksPerSecond ) );
   }

   /// <summary>
   /// Helper method to call <see cref="Truncate"/> with required argument for truncating given <see cref="DateTime"/> to whole minutes.
   /// </summary>
   /// <param name="dateTime">The <see cref="DateTime"/> to truncate.</param>
   /// <returns>a <see cref="DateTime"/> truncated to whole minutes.</returns>
   public static DateTime TruncateToWholeMinutes( this DateTime dateTime )
   {
      return dateTime.Truncate( TimeSpan.FromTicks( TimeSpan.TicksPerMinute ) );
   }

   /// <summary>
   /// Helper method to call <see cref="Truncate"/> with required argument for truncating given <see cref="DateTime"/> to whole hours.
   /// </summary>
   /// <param name="dateTime">The <see cref="DateTime"/> to truncate.</param>
   /// <returns>a <see cref="DateTime"/> truncated to whole hours.</returns>
   public static DateTime TruncateToWholeHours( this DateTime dateTime )
   {
      return dateTime.Truncate( TimeSpan.FromTicks( TimeSpan.TicksPerHour ) );
   }

   /// <summary>
   /// Helper method to call <see cref="Truncate"/> with required argument for truncating given <see cref="DateTime"/> to whole days.
   /// </summary>
   /// <param name="dateTime">The <see cref="DateTime"/> to truncate.</param>
   /// <returns>a <see cref="DateTime"/> truncated to whole days.</returns>
   public static DateTime TruncateToWholeDays( this DateTime dateTime )
   {
      return dateTime.Truncate( TimeSpan.FromTicks( TimeSpan.TicksPerDay ) );
   }

   /// <summary>
   /// Gets the result from <see cref="ResultOrNone{TResult}"/>, or throws it it doesn't have a result.
   /// </summary>
   /// <typeparam name="T">The type of the result.</typeparam>
   /// <param name="resultOrNone">The <see cref="ResultOrNone{TResult}"/>.</param>
   /// <returns>The result of this <see cref="ResultOrNone{TResult}"/>, if it has one.</returns>
   /// <exception cref="InvalidOperationException">If this <see cref="ResultOrNone{TResult}"/> does not have a result (its <see cref="ResultOrNone{TResult}.HasResult"/> property is <c>false</c>).</exception>
   public static T GetResultOrThrow<T>( this ResultOrNone<T> resultOrNone )
   {
      return resultOrNone.HasResult ? resultOrNone.Result : throw new InvalidOperationException( "Result is not available." );
   }

   /// <summary>
   /// Checks whether <paramref name="type"/> is non-<c>null</c> and a nullable type.
   /// If so, the <paramref name="paramType"/> will contain the underlying value type of the value type.
   /// </summary>
   /// <param name="type">The type to check.</param>
   /// <param name="paramType">This will contain underlying value type if this method returns <c>true</c>; otherwise it will be <paramref name="type"/>.</param>
   /// <returns>If the <paramref name="type"/> is non-<c>null</c> and nullable type, <c>true</c>; otherwise, <c>false</c>.</returns>
   public static Boolean IsNullable( this Type type, out Type paramType )
   {
      paramType = IsNullable( type ) ? type.
#if IS_NETSTANDARD
         GetTypeInfo()
         .GenericTypeParameters
#else

         GetGenericArguments()
#endif
         [0] : type;
      return !ReferenceEquals( paramType, type );
   }

   /// <summary>
   /// Checks whether <paramref name="type"/> is non-<c>null</c> and lazy type (instance of <see cref="Lazy{T}"/>).
   /// </summary>
   /// <param name="type">Type to check.</param>
   /// <returns><c>true</c> if <paramref name="type"/> is non-<c>null</c> and lazy type; <c>false</c> otherwise.</returns>
   public static Boolean IsLazy( this Type type )
   {
      return ( type?.
#if IS_NETSTANDARD
         GetTypeInfo()?.
#endif
         IsGenericType ?? false ) && Equals( type.GetGenericTypeDefinition(), typeof( Lazy<> ) );
   }

   /// <summary>
   /// Checks whether <paramref name="type"/> is non-<c>null</c> and lazy type (instance of <see cref="Lazy{T}"/>).
   /// If so, the <paramref name="paramType"/> will contain the underlying type of the lazy type.
   /// </summary>
   /// <param name="type">The type to check.</param>
   /// <param name="paramType">This will contain underlying type of lazy type if this method returns <c>true</c>; otherwise it will be <paramref name="type"/>.</param>
   /// <returns><c>true</c> if <paramref name="type"/> is non-<c>null</c> and lazy type; <c>false</c> otherwise.</returns>
   public static Boolean IsLazy( this Type type, out Type paramType )
   {

      paramType = IsLazy( type ) ? type.
#if IS_NETSTANDARD
         GetTypeInfo()
         .GenericTypeParameters
#else

         GetGenericArguments()
#endif
         [0] : type;
      return !ReferenceEquals( paramType, type );
   }

   /// <summary>
   /// Checks whether <paramref name="type"/> is non-<c>null</c> and a nullable type.
   /// </summary>
   /// <param name="type">Type to check.</param>
   /// <returns><c>true</c> if <paramref name="type"/> is non-<c>null</c> and nullable type; <c>false</c> otherwise.</returns>
   public static Boolean IsNullable( this Type type )
   {
      return ( type?.
#if IS_NETSTANDARD
         GetTypeInfo()?.
#endif
         IsGenericType ?? false ) && Equals( type.GetGenericTypeDefinition(), typeof( Nullable<> ) );
   }

#if IS_NETSTANDARD

   /// <summary>
   /// Helper method to invoke the event and then wait for any awaitables stored to the list of <see cref="EventArgsWithAsyncContext"/>.
   /// </summary>
   /// <typeparam name="TEventArgs">The actual type of event args, must inherit from <see cref="EventArgsWithAsyncContext"/>.</typeparam>
   /// <param name="evt">The event, may be <c>null</c>.</param>
   /// <param name="args">The event arguments to be passed to event.</param>
   /// <param name="sender">The sender to be passed to event.</param>
   /// <returns>The task to await for.</returns>
   public static async System.Threading.Tasks.Task InvokeAndWaitForAwaitables<TEventArgs>( this EventHandler<TEventArgs> evt, TEventArgs args, Object sender = null )
      where TEventArgs : EventArgsWithAsyncContext
   {
      if ( evt != null )
      {
         evt( sender, args );
         System.Threading.Tasks.Task[] awaitables;
         if ( ( awaitables = args?.GetAwaitableArray() ) != null )
         {
            await System.Threading.Tasks.Task.WhenAll( awaitables );
         }
      }
   }

#endif
}