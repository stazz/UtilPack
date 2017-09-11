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

using TAsyncPotentialToken = System.Nullable<System.Int64>;
using TAsyncToken = System.Int64;

namespace UtilPack.AsyncEnumeration
{
   internal class AsyncParallelEnumeratorImpl<T, TMoveNext> : AsyncEnumerator<T>
   {

      private readonly
#if NETSTANDARD1_0
         Dictionary
#else
         System.Collections.Concurrent.ConcurrentDictionary
#endif 
         <TAsyncToken, T> _seenByMoveNext;
      private readonly SynchronousMoveNextDelegate<TMoveNext> _hasNext;
      private readonly GetNextItemAsyncDelegate<T, TMoveNext> _next;
      private readonly ResetAsyncDelegate _dispose;

      private TAsyncToken _curToken;

      public AsyncParallelEnumeratorImpl(
         SynchronousMoveNextDelegate<TMoveNext> hasNext,
         GetNextItemAsyncDelegate<T, TMoveNext> getNext,
         ResetAsyncDelegate dispose
         )
      {
         this._hasNext = ArgumentValidator.ValidateNotNull( nameof( hasNext ), hasNext );
         this._next = ArgumentValidator.ValidateNotNull( nameof( getNext ), getNext );
         this._dispose = dispose;
         this._seenByMoveNext = new
#if NETSTANDARD1_0
            Dictionary
#else
            System.Collections.Concurrent.ConcurrentDictionary
#endif
            <TAsyncToken, T>();
      }

      public Boolean IsParallelEnumerationSupported => true;

      public async ValueTask<TAsyncPotentialToken> MoveNextAsync( CancellationToken token )
      {
         TAsyncPotentialToken retVal;
         (var hasNext, var moveNextResult) = this._hasNext();
         if ( hasNext )
         {
            retVal = Interlocked.Increment( ref this._curToken ); // Guid.NewGuid();
            if ( !this._seenByMoveNext.
#if NETSTANDARD1_0
               TryAddWithLocking
#else
               TryAdd
#endif
               ( retVal.Value, await this.CallNext( moveNextResult, token ) )
               )
            {
               throw new InvalidOperationException( "Duplicate GUID?" );
            }
         }
         else
         {
            retVal = null;
         }

         return retVal;
      }

      public T OneTimeRetrieve( TAsyncToken guid )
      {
         this._seenByMoveNext.
#if NETSTANDARD1_0
            TryRemoveWithLocking
#else
            TryRemove
#endif
            ( guid, out var retVal );
         return retVal;
      }

      public async ValueTask<Boolean> TryResetAsync( CancellationToken token )
      {
         Interlocked.Exchange( ref this._curToken, 0 );
         var dispose = this._dispose;
         if ( dispose != null )
         {
            await dispose( token );
         }
         return true;
      }

      protected virtual ValueTask<T> CallNext( TMoveNext moveNextResult, CancellationToken token )
      {
         return this._next( moveNextResult, token );
      }
   }

   internal sealed class AsyncParallelEnumeratorImplSealed<T, TMoveNext> : AsyncParallelEnumeratorImpl<T, TMoveNext>
   {
      public AsyncParallelEnumeratorImplSealed(
         SynchronousMoveNextDelegate<TMoveNext> hasNext,
         GetNextItemAsyncDelegate<T, TMoveNext> getNext,
         ResetAsyncDelegate dispose
         ) : base( hasNext, getNext, dispose )
      {
      }
   }

   internal sealed class AsyncParallelEnumeratorImpl<T, TMoveNext, TMetadata> : AsyncParallelEnumeratorImpl<T, TMoveNext>, AsyncEnumerator<T, TMetadata>
   {
      public AsyncParallelEnumeratorImpl(
         SynchronousMoveNextDelegate<TMoveNext> hasNext,
         GetNextItemAsyncDelegate<T, TMoveNext> getNext,
         ResetAsyncDelegate dispose,
         TMetadata metadata
         ) : base( hasNext, getNext, dispose )
      {
         this.Metadata = metadata;
      }

      public TMetadata Metadata { get; }
   }

   internal static class UtilPackExtensions
   {
      // TODO move to UtilPack
      public static Boolean TryAddWithLocking<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, TValue value, Object lockObject = null )
      {
         lock ( lockObject ?? dictionary )
         {
            var retVal = !dictionary.ContainsKey( key );
            if ( retVal )
            {
               dictionary.Add( key, value );
            }

            return retVal;
         }
      }

      public static Boolean TryRemoveWithLocking<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value, Object lockObject = null )
      {
         lock ( lockObject ?? dictionary )
         {
            var retVal = dictionary.ContainsKey( key );
            value = retVal ? dictionary[key] : default;
            dictionary.Remove( key );
            return retVal;
         }
      }
   }
}
