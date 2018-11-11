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

namespace AsyncEnumeration.Implementation.Enumerable
{
   internal sealed class ArrayEnumerator<T> : IAsyncEnumerator<T>
   {
      private const Int32 NOT_FETCHED = 0;
      private const Int32 FETCHED = 1;

      private readonly T[] _array;
      private Int32 _index;

      public ArrayEnumerator( T[] array )
         => this._array = ArgumentValidator.ValidateNotNull( nameof( array ), array );

      public Task<Boolean> WaitForNextAsync()
         => TaskUtils.TaskFromBoolean( this._index < this._array.Length );

      public T TryGetNext( out Boolean success )
      {
         var array = this._array;
         var idx = Interlocked.Increment( ref this._index );
         success = idx <= array.Length;
         return success ? array[idx - 1] : default;
      }

      public Task DisposeAsync()
         => TaskUtils.CompletedTask;
   }

   internal sealed class SynchronousEnumerableEnumerator<T> : IAsyncEnumerator<T>
   {
      private const Int32 STATE_INITIAL = 0;
      private const Int32 STATE_MOVENEXT_CALLED = 1;
      private const Int32 STATE_ENDED = 2;

      private readonly IEnumerator<T> _enumerator;
      private Int32 _state;

      public SynchronousEnumerableEnumerator( IEnumerator<T> syncEnumerator )
         => this._enumerator = ArgumentValidator.ValidateNotNull( nameof( syncEnumerator ), syncEnumerator );

      public Task<Boolean> WaitForNextAsync()
         => TaskUtils.TaskFromBoolean( Interlocked.CompareExchange( ref this._state, STATE_MOVENEXT_CALLED, STATE_INITIAL ) == STATE_INITIAL && this._enumerator.MoveNext() );

      public T TryGetNext( out Boolean success )
      {
         success = this._state == STATE_MOVENEXT_CALLED;
         var retVal = success ? this._enumerator.Current : default;
         if ( success && !this._enumerator.MoveNext() )
         {
            Interlocked.Exchange( ref this._state, STATE_ENDED );
         }
         return retVal;
      }


      public Task DisposeAsync()
      {
         this._enumerator.Dispose();
         return TaskUtils.CompletedTask;
      }
   }





   /// <summary>
   /// This class contains extension methods for types not defined in this assembly.
   /// </summary>
   public static partial class AsyncEnumerationExtensions
   {
      /// <summary>
      /// This extension method will wrap this array into <see cref="IAsyncEnumerable{T}"/>.
      /// </summary>
      /// <typeparam name="T">The type of array elements.</typeparam>
      /// <param name="array">This array.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will enumerate over the contents of the array.</returns>
      /// <exception cref="NullReferenceException">If this array is <c>null</c>.</exception>
      public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(
         this T[] array,
         IAsyncProvider alinqProvider = null
         ) => AsyncEnumerationFactory.FromGeneratorCallback( ArgumentValidator.ValidateNotNullReference( array ), a => new ArrayEnumerator<T>( a ), alinqProvider );

      /// <summary>
      /// This extension method will wrap this <see cref="IEnumerable{T}"/> into <see cref="IAsyncEnumerable{T}"/>.
      /// </summary>
      /// <typeparam name="T">The type of <see cref="IEnumerable{T}"/> elements.</typeparam>
      /// <param name="enumerable">This <see cref="IEnumerable{T}"/>.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will enumerate over this <see cref="IEnumerable{T}"/>.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IEnumerable{T}"/> is <c>null</c>.</exception>
      public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(
         this IEnumerable<T> enumerable,
         IAsyncProvider alinqProvider = null
         ) => AsyncEnumerationFactory.FromGeneratorCallback( ArgumentValidator.ValidateNotNullReference( enumerable ), e => new SynchronousEnumerableEnumerator<T>( e.GetEnumerator() ), alinqProvider );
   }
}