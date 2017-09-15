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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.AsyncEnumeration;

using TAsyncPotentialToken = System.Nullable<System.Int64>;
using TAsyncToken = System.Int64;


namespace UtilPack.AsyncEnumeration
{
   /// <summary>
   /// This interface mimics <see cref="IEnumerator{T}"/> for enumerators which can potentially cause asynchronous waiting.
   /// Such scenario is common in e.g. enumerating SQL query results.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   public interface AsyncEnumerator<out T>
   {
      /// <summary>
      /// Gets the value indicating whether this <see cref="AsyncEnumerator{T}"/> supports parallel enumeration.
      /// </summary>
      /// <value>The value indicating whether this <see cref="AsyncEnumerator{T}"/> supports parallel enumeration.</value>
      Boolean IsParallelEnumerationSupported { get; }

      /// <summary>
      /// This method mimics <see cref="System.Collections.IEnumerator.MoveNext"/> method in order to asynchronously read the next item.
      /// Please note that instead of directly using this method, one should use <see cref="E_UtilPack.EnumerateSequentiallyAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/>, <see cref="E_UtilPack.EnumerateSequentiallyAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/>, <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Action{T}, CancellationToken)"/>, or <see cref="E_UtilPack.EnumerateInParallelAsync{T}(AsyncEnumerator{T}, Func{T, Task}, CancellationToken)"/> extension methods, as those methods will take care of properly finishing enumeration in case of exceptions.
      /// </summary>
      /// <returns>A task, which will return <see cref="TAsyncPotentialToken"/> if next item is encountered, and <c>null</c> if this enumeration ended.</returns>
      /// <remarks>
      /// The return type is <see cref="ValueTask{TResult}"/>, which helps abstracting away e.g. buffering functionality (since the one important motivation for buffering is to avoid allocating many <see cref="Task{TResult}"/> objects from heap).
      /// </remarks>
      ValueTask<TAsyncPotentialToken> MoveNextAsync( CancellationToken token = default );

      /// <summary>
      /// This method mimics <see cref="IEnumerator{T}.Current"/> property in order to get the item previously fetched by <see cref="MoveNextAsync"/>.
      /// </summary>
      /// <param name="retrievalToken">The value of the retrieval token returned by <see cref="MoveNextAsync"/> method.</param>
      T OneTimeRetrieve( TAsyncToken retrievalToken );

      /// <summary>
      /// This method mimics <see cref="System.Collections.IEnumerator.Reset"/> method in order to asynchronously reset this enumerator.
      /// </summary>
      /// <returns>A task, which will return <c>true</c> if reset is successful, and <c>false</c> otherwise.</returns>
      /// <remarks>
      /// Note that unlike <see cref="MoveNextAsync"/>, this method will not throw when invoked concurrently. Instead, it will just return <c>false</c>.
      /// </remarks>
      ValueTask<Boolean> EnumerationEnded( CancellationToken token = default );
   }

}