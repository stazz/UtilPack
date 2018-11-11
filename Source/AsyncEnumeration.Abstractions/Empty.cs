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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Abstractions
{
   /// <summary>
   /// This is utility class for various empty asynchronous enumerables and enumerators.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   public static class EmptyAsync<T>
   {
      private sealed class EmptyAsyncEnumerable : IAsyncEnumerable<T>
      {
         public IAsyncEnumerator<T> GetAsyncEnumerator()
            => Enumerator;

         public IAsyncProvider AsyncProvider
            => EmptyAsyncProvider.Instance;

      }

      private sealed class EmptyAsyncEnumerator : IAsyncEnumerator<T>
      {
         public Task<Boolean> WaitForNextAsync()
            => TaskUtils.False;

         public T TryGetNext( out Boolean success )
         {
            success = false;
            return default;
         }

         public Task DisposeAsync()
            => TaskUtils.CompletedTask;
      }

      /// <summary>
      /// Gets the <see cref="IAsyncEnumerator{T}"/> which will return no items.
      /// </summary>
      /// <value>The <see cref="IAsyncEnumerator{T}"/> which will return no items.</value>
      public static IAsyncEnumerator<T> Enumerator { get; } = new EmptyAsyncEnumerator();

      /// <summary>
      /// Gets the <see cref="IAsyncConcurrentEnumerable{T}"/> which will always return <see cref="IAsyncConcurrentEnumerable{T}"/> with no items.
      /// </summary>
      /// <value>The <see cref="IAsyncConcurrentEnumerable{T}"/> which will always return <see cref="IAsyncConcurrentEnumerable{T}"/> with no items.</value>
      public static IAsyncEnumerable<T> Enumerable { get; } = new EmptyAsyncEnumerable();

   }

   internal sealed class EmptyAsyncProvider : IAsyncProvider
   {
      public static EmptyAsyncProvider Instance { get; } = new EmptyAsyncProvider();

      private EmptyAsyncProvider()
      {

      }

      public Task<T> AggregateAsync<T>( IAsyncEnumerable<T> source, Func<T, T, T> func )
         => throw AsyncProviderUtilities.EmptySequenceException();

      public Task<T> AggregateAsync<T>( IAsyncEnumerable<T> source, Func<T, T, ValueTask<T>> asyncFunc )
         => throw AsyncProviderUtilities.EmptySequenceException();

      public Task<TResult> AggregateAsync<T, TResult>( IAsyncEnumerable<T> source, Func<TResult, T, TResult> func, TResult seed )
         =>
#if NET40
         TaskEx
#else
         Task
#endif
         .FromResult( seed );

      public Task<TResult> AggregateAsync<T, TResult>( IAsyncEnumerable<T> source, Func<TResult, T, ValueTask<TResult>> asyncFunc, TResult seed )
         =>
#if NET40
         TaskEx
#else
         Task
#endif
         .FromResult( seed );

      public Task<Boolean> AllAsync<T>( IAsyncEnumerable<T> source, Func<T, Boolean> predicate )
         => TaskUtils.True;

      public Task<Boolean> AllAsync<T>( IAsyncEnumerable<T> source, Func<T, ValueTask<Boolean>> asyncPredicate )
         => TaskUtils.True;

      public Task<Boolean> AnyAsync<T>( IAsyncEnumerable<T> source )
         => TaskUtils.False;

      public Task<Boolean> AnyAsync<T>( IAsyncEnumerable<T> source, Func<T, Boolean> predicate )
         => TaskUtils.False;

      public Task<Boolean> AnyAsync<T>( IAsyncEnumerable<T> source, Func<T, ValueTask<Boolean>> asyncPredicate )
         => TaskUtils.False;

      public ValueTask<Int64> EnumerateAsync<T>( IAsyncEnumerable<T> enumerable )
         => new ValueTask<Int64>( 0 );

      public ValueTask<Int64> EnumerateAsync<T>( IAsyncEnumerable<T> enumerable, Action<T> action )
         => new ValueTask<Int64>( 0 );

      public ValueTask<Int64> EnumerateAsync<T>( IAsyncEnumerable<T> enumerable, Func<T, Task> asyncAction )
         => new ValueTask<Int64>( 0 );

      public Task<T> FirstAsync<T>( IAsyncEnumerable<T> enumerable )
         => throw AsyncProviderUtilities.EmptySequenceException();

      public Task<T> FirstOrDefaultAsync<T>( IAsyncEnumerable<T> enumerable )
         =>
#if NET40
         TaskEx
#else
         Task
#endif
         .FromResult( default( T ) );

      public IAsyncEnumerable<U> OfType<T, U>( IAsyncEnumerable<T> enumerable )
         => AsyncProviderUtilities.IsOfType(
            typeof( T )
#if !NET40
         .GetTypeInfo()
#endif
         , typeof( U )
#if !NET40
         .GetTypeInfo()
#endif
         ) ?
            (IAsyncEnumerable<U>) enumerable :
            EmptyAsync<U>.Enumerable;

      public IAsyncEnumerable<U> Select<T, U>( IAsyncEnumerable<T> enumerable, Func<T, U> selector )
         => EmptyAsync<U>.Enumerable;

      public IAsyncEnumerable<U> Select<T, U>( IAsyncEnumerable<T> enumerable, Func<T, ValueTask<U>> asyncSelector )
         => EmptyAsync<U>.Enumerable;

      public IAsyncEnumerable<U> SelectMany<T, U>( IAsyncEnumerable<T> enumerable, Func<T, IEnumerable<U>> selector )
         => EmptyAsync<U>.Enumerable;

      public IAsyncEnumerable<U> SelectMany<T, U>( IAsyncEnumerable<T> enumerable, Func<T, IAsyncEnumerable<U>> asyncSelector )
         => EmptyAsync<U>.Enumerable;

      public IAsyncEnumerable<T> Skip<T>( IAsyncEnumerable<T> enumerable, Int32 amount )
         => enumerable;

      public IAsyncEnumerable<T> Skip<T>( IAsyncEnumerable<T> enumerable, Int64 amount )
         => enumerable;

      public IAsyncEnumerable<T> SkipWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate )
         => enumerable;

      public IAsyncEnumerable<T> SkipWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, ValueTask<Boolean>> asyncPredicate )
         => enumerable;

      public IAsyncEnumerable<T> Take<T>( IAsyncEnumerable<T> enumerable, Int32 amount )
         => enumerable;

      public IAsyncEnumerable<T> Take<T>( IAsyncEnumerable<T> enumerable, Int64 amount )
         => enumerable;

      public IAsyncEnumerable<T> TakeWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate )
         => enumerable;

      public IAsyncEnumerable<T> TakeWhile<T>( IAsyncEnumerable<T> enumerable, Func<T, Task<Boolean>> asyncPredicate )
         => enumerable;

      public IAsyncEnumerable<T> Where<T>( IAsyncEnumerable<T> enumerable, Func<T, Boolean> predicate )
         => enumerable;

      public IAsyncEnumerable<T> Where<T>( IAsyncEnumerable<T> enumerable, Func<T, Task<Boolean>> asyncPredicate )
         => enumerable;
   }
}
