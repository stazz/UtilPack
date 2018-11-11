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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Implementation.Provider
{


   public partial class DefaultAsyncProvider
   {
      /// <summary>
      /// This is helper method to sequentially enumerate a <see cref="IAsyncEnumerable{T}"/> and properly dispose it in case of an exception, while not reacting to any of the encountered elements.
      /// </summary>
      /// <typeparam name="T">The type of the items being enumerated.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <returns>A task which will have enumerated the <see cref="IAsyncEnumerable{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
      /// <remarks>
      /// Sequential enumeration means that the next invocation of <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/> will not start until all the elements are seen throught <see cref="IAsyncEnumerator{T}.TryGetNext"/>.
      /// </remarks>
      public ValueTask<Int64> EnumerateAsync<T>( IAsyncEnumerable<T> enumerable )
      {
         return AsyncProviderUtilities.EnumerateAsync(
            ArgumentValidator.ValidateNotNullReference( enumerable ),
            (Action<T>) null
            );
      }

      /// <summary>
      /// This is helper method to sequentially enumerate a <see cref="IAsyncEnumerator{T}"/> and properly dispose it in case of exception.
      /// </summary>
      /// <typeparam name="T">The type of the items being enumerated.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="action">The callback to invoke for each item. May be <c>null</c>.</param>
      /// <returns>A task which will have enumerated the <see cref="IAsyncEnumerable{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
      /// <remarks>
      /// Sequential enumeration means that the next invocation of <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/> will not start until the given callback <paramref name="action"/> is completed.
      /// </remarks>
      public ValueTask<Int64> EnumerateAsync<T>( IAsyncEnumerable<T> enumerable, Action<T> action )
      {
         return AsyncProviderUtilities.EnumerateAsync(
            ArgumentValidator.ValidateNotNullReference( enumerable ),
            action
            );
      }

      /// <summary>
      /// This is helper method to sequentially enumerate a <see cref="IAsyncEnumerable{T}"/> and properly dispose it in case of an exception.
      /// For each item, a task from given callback is awaited for, if the callback is not <c>null</c>.
      /// </summary>
      /// <typeparam name="T">The type of the items being enumerated.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <param name="asyncAction">The callback to invoke for each item. May be <c>null</c>, and may also return <c>null</c>.</param>
      /// <returns>A task which will have enumerated the <see cref="IAsyncEnumerable{T}"/> on completion. The return value is amount of items encountered during enumeration.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <exception cref="OverflowException">If there are more than <see cref="Int64.MaxValue"/> amount of items encountered.</exception>
      /// <remarks>
      /// Sequential enumeration means that the next invocation of <see cref="IAsyncEnumerator{T}.WaitForNextAsync"/> will not start until the given callback <paramref name="asyncAction"/> is completed.
      /// </remarks>
      public ValueTask<Int64> EnumerateAsync<T>( IAsyncEnumerable<T> enumerable, Func<T, Task> asyncAction )
      {
         return AsyncProviderUtilities.EnumerateAsync(
            ArgumentValidator.ValidateNotNullReference( enumerable ),
            asyncAction
            );
      }
   }
}

