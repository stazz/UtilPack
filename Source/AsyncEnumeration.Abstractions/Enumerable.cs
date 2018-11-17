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
using System.Threading.Tasks;

namespace System.Collections.Generic
{
   /// <summary>
   /// This interface mimics <see cref="IEnumerable{T}"/> for enumerables which must perform asynchronous waiting when moving to next item.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated. This parameter is covariant.</typeparam>
   public interface IAsyncEnumerable<out T> : IAsyncEnumerable
   {
      /// <summary>
      /// Gets the <see cref="IAsyncEnumerator{T}"/> to use to enumerate this <see cref="IAsyncEnumerable{T}"/>.
      /// </summary>
      /// <returns>A <see cref="IAsyncEnumerator{T}"/> which should be used to enumerate this <see cref="IAsyncEnumerable{T}"/>.</returns>
      IAsyncEnumerator<T> GetAsyncEnumerator();

   }

   /// <summary>
   /// This interface mimics <see cref="IEnumerator{T}"/> for enumerators which can potentially cause asynchronous waiting.
   /// Such scenario is common in e.g. enumerating SQL query results.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated. This parameter is covariant.</typeparam>
   public interface IAsyncEnumerator<out T> : IAsyncDisposable
   {
      /// <summary>
      /// This method mimics <see cref="System.Collections.IEnumerator.MoveNext"/> method in order to asynchronously read the next item.
      /// Please note that instead of directly using this method, one should use <see cref="E_UtilPack.EnumerateAsync{T}(IAsyncEnumerable{T}, Action{T})"/>, <see cref="E_UtilPack.EnumerateAsync{T}(IAsyncEnumerable{T}, Func{T, Task})"/>´extension methods, as those methods will take care of properly finishing enumeration in case of exceptions.
      /// </summary>
      /// <returns>A task, which will return <c>true</c> if next item is encountered, and <c>false</c> if this enumeration ended.</returns>
      Task<Boolean> WaitForNextAsync();

      /// <summary>
      /// This method mimics <see cref="IEnumerator{T}.Current"/> property in order to get one or more items previously fetched by <see cref="WaitForNextAsync"/>.
      /// </summary>
      /// <param name="success">Whether getting next value was successful.</param>
      T TryGetNext( out Boolean success );
   }
}