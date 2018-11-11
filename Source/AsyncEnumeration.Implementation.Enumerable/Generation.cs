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
using AsyncEnumeration.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncEnumeration.Implementation.Enumerable
{
   /// <summary>
   /// This class provides some general ways to generate instances of <see cref="IAsyncEnumerable{T}"/>, similar to how <see cref="System.Linq.Enumerable"/> provides general ways to generate instances of <see cref="IEnumerable{T}"/>.
   /// </summary>
   public static class AsyncEnumerable
   {

      /// <summary>
      /// Returns <see cref="IAsyncConcurrentEnumerable{T}"/> (which is also <see cref="IAsyncEnumerable{T}"/>) that will return the given item specified amount of times.
      /// </summary>
      /// <typeparam name="T">The type of item to repeat.</typeparam>
      /// <param name="item">The item to repeat.</param>
      /// <param name="count">Amount of times to repeat the <paramref name="item"/>.</param>
      /// <returns>An empty <see cref="IAsyncConcurrentEnumerable{T}"/> if <paramref name="count"/> is <c>0</c> or less; otherwise returns <see cref="IAsyncConcurrentEnumerable{T}"/> which will repeat <paramref name="item"/> <paramref name="count"/> amount of times.</returns>
      /// <seealso cref="System.Linq.Enumerable.Repeat{TResult}(TResult, Int32)"/>
      /// <seealso cref="Repeat{T}(T, Int64)"/>
      public static IAsyncEnumerable<T> Repeat<T>(
         T item,
         Int32 count,
         IAsyncProvider provider
         )
      {
         return count <= 0 ?
            EmptyAsync<T>.Enumerable :
            Neverending( () => item, provider ).Take( count );
      }

      /// <summary>
      /// Returns <see cref="IAsyncConcurrentEnumerable{T}"/> (which is also <see cref="IAsyncEnumerable{T}"/>) that will return the given item specified amount of times, expressed as 64-bit integer.
      /// </summary>
      /// <typeparam name="T">The type of item to repeat.</typeparam>
      /// <param name="item">The item to repeat.</param>
      /// <param name="count">Amount of times to repeat the <paramref name="item"/>, as 64-bit integer.</param>
      /// <returns>An empty <see cref="IAsyncConcurrentEnumerable{T}"/> if <paramref name="count"/> is <c>0</c> or less; otherwise returns <see cref="IAsyncConcurrentEnumerable{T}"/> which will repeat <paramref name="item"/> <paramref name="count"/> amount of times.</returns>
      /// <seealso cref="System.Linq.Enumerable.Repeat{TResult}(TResult, Int32)"/>
      /// <seealso cref="Repeat{T}(T, Int32)"/>
      public static IAsyncEnumerable<T> Repeat<T>(
         T item,
         Int64 count,
         IAsyncProvider provider
         )
      {
         return count <= 0 ?
            EmptyAsync<T>.Enumerable :
            Neverending( () => item, provider ).Take( count );
      }

      /// <summary>
      /// Returns <see cref="IAsyncConcurrentEnumerable{T}"/> (which is also <see cref="IAsyncEnumerable{T}"/>) that will return the result of given synchronous item factory callback specified amount of times.
      /// </summary>
      /// <typeparam name="T">The type of item to repeat.</typeparam>
      /// <param name="generator">The synchronous callback to generate an item to repeat.</param>
      /// <param name="count">Amount of times to repeat the item.</param>
      /// <returns>An empty <see cref="IAsyncConcurrentEnumerable{T}"/> if <paramref name="count"/> is <c>0</c> or less; otherwise returns <see cref="IAsyncConcurrentEnumerable{T}"/> which will repeat result of calling <paramref name="generator"/> <paramref name="count"/> amount of times.</returns>
      /// <seealso cref="System.Linq.Enumerable.Repeat{TResult}(TResult, Int32)"/>
      public static IAsyncEnumerable<T> Repeat<T>(
         Func<T> generator,
         Int32 count,
         IAsyncProvider provider
         )
      {
         return count <= 0 ?
            EmptyAsync<T>.Enumerable :
            Neverending( generator, provider ).Take( count );
      }

      /// <summary>
      /// Returns <see cref="IAsyncConcurrentEnumerable{T}"/> (which is also <see cref="IAsyncEnumerable{T}"/>) that will return the result of given potentially asynchronous item factory callback specified amount of times.
      /// </summary>
      /// <typeparam name="T">The type of item to repeat.</typeparam>
      /// <param name="asyncGenerator">The potentially asynchronous callback to generate an item to repeat.</param>
      /// <param name="count">Amount of times to repeat the item.</param>
      /// <returns>An empty <see cref="IAsyncConcurrentEnumerable{T}"/> if <paramref name="count"/> is <c>0</c> or less; otherwise returns <see cref="IAsyncConcurrentEnumerable{T}"/> which will repeat result of calling <paramref name="asyncGenerator"/> <paramref name="count"/> amount of times.</returns>
      /// <seealso cref="System.Linq.Enumerable.Repeat{TResult}(TResult, Int32)"/>
      public static IAsyncEnumerable<T> Repeat<T>(
         Func<ValueTask<T>> asyncGenerator,
         Int32 count,
         IAsyncProvider provider
         )
      {
         return count <= 0 ?
            EmptyAsync<T>.Enumerable :
            Neverending( asyncGenerator, provider ).Take( count );
      }


      /// <summary>
      /// Returns <see cref="IAsyncConcurrentEnumerable{T}"/> (which is also <see cref="IAsyncEnumerable{T}"/>) that will return numbers in given range specification.
      /// </summary>
      /// <param name="initial">The start of the range, inclusive.</param>
      /// <param name="target">The end of the range, exclusive.</param>
      /// <param name="step">The amount to increase for each number within the range. By default, is <c>1</c> for increasing ranges, and <c>-1</c> for decreasing ranges. Specifying invalid values will reset this to default value.</param>
      /// <returns>An enumerable that contains numbers within the given range specification.</returns>
      /// <remarks>
      /// Note that unlike <see cref="System.Linq.Enumerable.Range(Int32, Int32)"/>, this method has exclusive _maximum_ amount as second parameter, instead of amount of values to generate.
      /// This method also handles both increasing and decreasing number ranges.
      /// </remarks>
      /// <seealso cref="System.Linq.Enumerable.Range(Int32, Int32)"/>
      public static IAsyncEnumerable<Int32> Range(
         Int32 initial,
         Int32 target,
         IAsyncProvider provider,
         Int32 step = 1
         )
      {
         IAsyncEnumerable<Int32> retVal;
         if ( target > initial )
         {
            // Increasing range from initial to target, step must be 1 or greater
            step = Math.Max( 1, step );
            retVal = AsyncEnumerationFactory.CreateStatefulWrappingEnumerable( () =>
            {
               var current = initial;
               return AsyncEnumerationFactory.CreateSynchronousWrappingStartInfo(
               ( out Boolean success ) =>
               {
                  var prev = current;
                  success = current < target;
                  current += step;
                  return prev;
               } );
            }, provider );
         }
         else if ( initial == target )
         {
            // Empty
            retVal = EmptyAsync<Int32>.Enumerable;
         }
         else
         {
            // Decreasing range from target to initial, step must be -1 or less
            step = Math.Min( -1, step );
            retVal = AsyncEnumerationFactory.CreateStatefulWrappingEnumerable( () =>
            {
               var current = initial;
               return AsyncEnumerationFactory.CreateSynchronousWrappingStartInfo(
               ( out Boolean success ) =>
               {
                  var prev = current;
                  success = current > target;
                  current -= step;
                  return prev;
               } );
            }, provider );
         }

         return retVal;
      }

      /// <summary>
      /// Returns <see cref="IAsyncConcurrentEnumerable{T}"/> (which is also <see cref="IAsyncEnumerable{T}"/>) that will return numbers in given range specification, as 64-bit integers.
      /// </summary>
      /// <param name="initial">The start of the range, inclusive.</param>
      /// <param name="target">The end of the range, exclusive.</param>
      /// <param name="step">The amount to increase for each number within the range. By default, is <c>1</c> for increasing ranges, and <c>-1</c> for decreasing ranges. Specifying invalid values will reset this to default value.</param>
      /// <returns>An enumerable that contains numbers within the given range specification.</returns>
      /// <remarks>
      /// Note that unlike <see cref="System.Linq.Enumerable.Range(Int32, Int32)"/>, this method has exclusive _maximum_ amount as second parameter, instead of amount of values to generate.
      /// This method also handles both increasing and decreasing number ranges.
      /// </remarks>
      /// <seealso cref="System.Linq.Enumerable.Range(Int32, Int32)"/>
      public static IAsyncEnumerable<Int64> Range(
         Int64 initial,
         Int64 target,
         IAsyncProvider provider,
         Int64 step = 1
         )
      {
         IAsyncEnumerable<Int64> retVal;
         if ( target > initial )
         {
            // Increasing range from initial to target, step must be 1 or greater
            step = Math.Max( 1, step );
            retVal = AsyncEnumerationFactory.CreateStatefulWrappingEnumerable( () =>
            {
               var current = initial;
               return AsyncEnumerationFactory.CreateSynchronousWrappingStartInfo(
               ( out Boolean success ) =>
               {
                  var prev = current;
                  success = current < target;
                  current += step;
                  return prev;
               } );
            }, provider );
         }
         else if ( initial == target )
         {
            // Empty
            retVal = EmptyAsync<Int64>.Enumerable;
         }
         else
         {
            // Decreasing range from target to initial, step must be -1 or less
            step = Math.Min( -1, step );
            retVal = AsyncEnumerationFactory.CreateStatefulWrappingEnumerable( () =>
            {
               var current = initial;
               return AsyncEnumerationFactory.CreateSynchronousWrappingStartInfo(
               ( out Boolean success ) =>
               {
                  var prev = current;
                  success = current > target;
                  current -= step;
                  return prev;
               } );
            }, provider );
         }

         return retVal;
      }

      /// <summary>
      /// Returns <see cref="IAsyncConcurrentEnumerable{T}"/> (which is also <see cref="IAsyncEnumerable{T}"/>) that will indefinetly repeat the given value.
      /// </summary>
      /// <typeparam name="T">The type of item to repeat.</typeparam>
      /// <param name="item">An item to repeat.</param>
      /// <returns>An enumerable that will indefinetly repeat the given value.</returns>
      public static IAsyncEnumerable<T> Neverending<T>(
         T item,
         IAsyncProvider provider
         )
      {
         return AsyncEnumerationFactory.CreateStatelessWrappingEnumerable( AsyncEnumerationFactory.CreateSynchronousWrappingStartInfo(
            ( out Boolean success ) =>
            {
               success = true;
               return item;
            }
            ), provider );
      }

      /// <summary>
      /// Returns <see cref="IAsyncConcurrentEnumerable{T}"/> (which is also <see cref="IAsyncEnumerable{T}"/>) that will indefinetly repeat the value returned by given synchronous factory callback.
      /// </summary>
      /// <typeparam name="T">The type of item to repeat.</typeparam>
      /// <param name="generator">A synchronous callback to dynamically generate the value to repeat.</param>
      /// <returns>An enumerable that will indefinetly repeat the given value.</returns>
      public static IAsyncEnumerable<T> Neverending<T>(
         Func<T> generator,
         IAsyncProvider provider
         )
      {
         return AsyncEnumerationFactory.CreateStatelessWrappingEnumerable( AsyncEnumerationFactory.CreateSynchronousWrappingStartInfo(
            ( out Boolean success ) =>
            {
               success = true;
               return generator();
            }
            ), provider );
      }

      /// <summary>
      /// Returns <see cref="IAsyncConcurrentEnumerable{T}"/> (which is also <see cref="IAsyncEnumerable{T}"/>) that will indefinetly repeat the value returned by given potentially asynchronous factory callback.
      /// </summary>
      /// <typeparam name="T">The type of item to repeat.</typeparam>
      /// <param name="asyncGenerator">A potentially asynchronous callback to dynamically generate the value to repeat.</param>
      /// <returns>An enumerable that will indefinetly repeat the given value.</returns>
      public static IAsyncEnumerable<T> Neverending<T>(
         Func<ValueTask<T>> asyncGenerator,
         IAsyncProvider provider
         )
      {
         return AsyncEnumerationFactory.CreateSequentialEnumerable( () => AsyncEnumerationFactory.CreateSequentialStartInfo(
            async () =>
            {
               return (true, await asyncGenerator());
            },
            null
            ), provider );
      }
   }
}
