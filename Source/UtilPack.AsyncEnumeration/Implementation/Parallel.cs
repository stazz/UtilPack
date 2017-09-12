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

      protected readonly
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

      public virtual async ValueTask<TAsyncPotentialToken> MoveNextAsync( CancellationToken token )
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
               throw new InvalidOperationException( "Duplicate retrieval token?" );
            }
         }
         else
         {
            retVal = null;
            await this.PerformDispose( token );
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

      public ValueTask<Boolean> TryResetAsync( CancellationToken token )
      {
         Interlocked.Exchange( ref this._curToken, 0 );
         return this.PerformDispose( token );
      }

      protected virtual ValueTask<T> CallNext( TMoveNext moveNextResult, CancellationToken token )
      {
         return this._next( moveNextResult, token );
      }

      protected virtual async ValueTask<Boolean> PerformDispose( CancellationToken token )
      {
         var dispose = this._dispose;
         var retVal = false;
         if ( dispose != null )
         {
            await dispose( token );
            retVal = true;
         }

         return retVal;
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

   internal abstract class AsyncParallelEnumeratorObservableImpl<T, TMoveNext, TStartedArgs, TEndedArgs, TItemArgs> : AsyncParallelEnumeratorImpl<T, TMoveNext>
      where TStartedArgs : class, EnumerationStartedEventArgs
      where TEndedArgs : class, EnumerationEndedEventArgs
      where TItemArgs : class, EnumerationItemEventArgs<T>
   {
      private const Int32 INITIAL = 0;
      private const Int32 INITIALIZING = 1;
      private const Int32 STARTED = 2;

      private Int32 _enumerationState;
      protected readonly Func<GenericEventHandler<TStartedArgs>> _getGlobalBeforeEnumerationExecutionStart;
      protected readonly Func<GenericEventHandler<TStartedArgs>> _getGlobalAfterEnumerationExecutionStart;
      protected readonly Func<GenericEventHandler<TEndedArgs>> _getGlobalBeforeEnumerationExecutionEnd;
      protected readonly Func<GenericEventHandler<TEndedArgs>> _getGlobalAfterEnumerationExecutionEnd;
      protected readonly Func<GenericEventHandler<TItemArgs>> _getGlobalAfterEnumerationExecutionItemEncountered;

      public AsyncParallelEnumeratorObservableImpl(
         SynchronousMoveNextDelegate<TMoveNext> hasNext,
         GetNextItemAsyncDelegate<T, TMoveNext> getNext,
         ResetAsyncDelegate dispose,
         Func<GenericEventHandler<TStartedArgs>> getGlobalBeforeEnumerationExecutionStart,
         Func<GenericEventHandler<TStartedArgs>> getGlobalAfterEnumerationExecutionStart,
         Func<GenericEventHandler<TEndedArgs>> getGlobalBeforeEnumerationExecutionEnd,
         Func<GenericEventHandler<TEndedArgs>> getGlobalAfterEnumerationExecutionEnd,
         Func<GenericEventHandler<TItemArgs>> getGlobalAfterEnumerationExecutionItemEncountered
         ) : base( hasNext, getNext, dispose )
      {
         this._getGlobalBeforeEnumerationExecutionStart = getGlobalBeforeEnumerationExecutionStart;
         this._getGlobalAfterEnumerationExecutionStart = getGlobalAfterEnumerationExecutionStart;
         this._getGlobalBeforeEnumerationExecutionEnd = getGlobalBeforeEnumerationExecutionEnd;
         this._getGlobalAfterEnumerationExecutionEnd = getGlobalAfterEnumerationExecutionEnd;
         this._getGlobalAfterEnumerationExecutionItemEncountered = getGlobalAfterEnumerationExecutionItemEncountered;
      }

      public event GenericEventHandler<TStartedArgs> BeforeEnumerationStart;
      public event GenericEventHandler<TStartedArgs> AfterEnumerationStart;
      public event GenericEventHandler<TItemArgs> AfterEnumerationItemEncountered;
      public event GenericEventHandler<TEndedArgs> BeforeEnumerationEnd;
      public event GenericEventHandler<TEndedArgs> AfterEnumerationEnd;

      public override async ValueTask<TAsyncPotentialToken> MoveNextAsync( CancellationToken token )
      {
         TAsyncPotentialToken retVal;
         if ( Interlocked.CompareExchange( ref this._enumerationState, INITIALIZING, INITIAL ) == INITIAL )
         {
            TStartedArgs args = null;
            try
            {
               this.BeforeEnumerationStart?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateBeforeEnumerationStartedArgs() ) ), throwExceptions: false );
               this._getGlobalBeforeEnumerationExecutionStart?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? ( args = this.CreateBeforeEnumerationStartedArgs() ) ), throwExceptions: false );

               retVal = await base.MoveNextAsync( token );
            }
            finally
            {
               // These two invocations should be no-throw
               this.AfterEnumerationStart?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateAfterEnumerationStartedArgs( args ) ) ), throwExceptions: false );
               this._getGlobalAfterEnumerationExecutionStart?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? this.CreateAfterEnumerationStartedArgs( args ) ), throwExceptions: false );
               Interlocked.CompareExchange( ref this._enumerationState, STARTED, INITIALIZING );
            }
         }
         else
         {
            retVal = await base.MoveNextAsync( token );
         }

         if ( retVal.HasValue )
         {
            TItemArgs args = null;
            var item = this._seenByMoveNext[retVal.Value];
            this.AfterEnumerationItemEncountered?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateEnumerationItemArgs( item ) ) ), throwExceptions: false );
            this._getGlobalAfterEnumerationExecutionItemEncountered?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? this.CreateEnumerationItemArgs( item ) ), throwExceptions: false );
         }

         return retVal;
      }

      protected override ValueTask<Boolean> PerformDispose( CancellationToken token )
      {
         TEndedArgs args = null;
         this.BeforeEnumerationEnd?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateBeforeEnumerationEndedArgs() ) ), throwExceptions: false );
         this._getGlobalBeforeEnumerationExecutionEnd?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? ( args = this.CreateBeforeEnumerationEndedArgs() ) ), throwExceptions: false );
         try
         {
            return base.PerformDispose( token );
         }
         finally
         {
            this.AfterEnumerationEnd?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateAfterEnumerationEndedArgs( args ) ) ), throwExceptions: false );
            this._getGlobalAfterEnumerationExecutionEnd?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? this.CreateAfterEnumerationEndedArgs( args ) ), throwExceptions: false );
         }
      }

      protected abstract TStartedArgs CreateBeforeEnumerationStartedArgs();

      protected abstract TStartedArgs CreateAfterEnumerationStartedArgs( TStartedArgs beforeStart );

      protected abstract TItemArgs CreateEnumerationItemArgs( T item );

      protected abstract TEndedArgs CreateBeforeEnumerationEndedArgs();

      protected abstract TEndedArgs CreateAfterEnumerationEndedArgs( TEndedArgs beforeEnd );
   }

   internal sealed class AsyncParallelEnumeratorObservableImpl<T, TMoveNext> : AsyncParallelEnumeratorObservableImpl<T, TMoveNext, EnumerationStartedEventArgs, EnumerationEndedEventArgs, EnumerationItemEventArgs<T>>, AsyncEnumeratorObservable<T>
   {
      public AsyncParallelEnumeratorObservableImpl(
         SynchronousMoveNextDelegate<TMoveNext> hasNext,
         GetNextItemAsyncDelegate<T, TMoveNext> getNext,
         ResetAsyncDelegate dispose,
         Func<GenericEventHandler<EnumerationStartedEventArgs>> getGlobalBeforeEnumerationExecutionStart,
         Func<GenericEventHandler<EnumerationStartedEventArgs>> getGlobalAfterEnumerationExecutionStart,
         Func<GenericEventHandler<EnumerationEndedEventArgs>> getGlobalBeforeEnumerationExecutionEnd,
         Func<GenericEventHandler<EnumerationEndedEventArgs>> getGlobalAfterEnumerationExecutionEnd,
         Func<GenericEventHandler<EnumerationItemEventArgs<T>>> getGlobalAfterEnumerationExecutionItemEncountered
         ) : base( hasNext, getNext, dispose, getGlobalBeforeEnumerationExecutionStart, getGlobalAfterEnumerationExecutionStart, getGlobalBeforeEnumerationExecutionEnd, getGlobalAfterEnumerationExecutionEnd, getGlobalAfterEnumerationExecutionItemEncountered )
      {
      }

      protected override EnumerationStartedEventArgs CreateBeforeEnumerationStartedArgs()
      {
         return EnumerationEventArgsUtility.StatelessStartArgs;
      }

      protected override EnumerationStartedEventArgs CreateAfterEnumerationStartedArgs( EnumerationStartedEventArgs beforeStart )
      {
         return beforeStart ?? this.CreateBeforeEnumerationStartedArgs();
      }

      protected override EnumerationItemEventArgs<T> CreateEnumerationItemArgs( T item )
      {
         return new EnumerationItemEventArgsImpl<T>( item );
      }

      protected override EnumerationEndedEventArgs CreateBeforeEnumerationEndedArgs()
      {
         return EnumerationEventArgsUtility.StatelessEndArgs;
      }

      protected override EnumerationEndedEventArgs CreateAfterEnumerationEndedArgs( EnumerationEndedEventArgs beforeEnd )
      {
         return beforeEnd ?? this.CreateBeforeEnumerationEndedArgs();
      }
   }

   internal sealed class AsyncParallelEnumeratorObservableImpl<T, TMoveNext, TMetadata> : AsyncParallelEnumeratorObservableImpl<T, TMoveNext, EnumerationStartedEventArgs<TMetadata>, EnumerationEndedEventArgs<TMetadata>, EnumerationItemEventArgs<T, TMetadata>>, AsyncEnumeratorObservable<T, TMetadata>
   {
      public AsyncParallelEnumeratorObservableImpl(
         SynchronousMoveNextDelegate<TMoveNext> hasNext,
         GetNextItemAsyncDelegate<T, TMoveNext> getNext,
         ResetAsyncDelegate dispose,
         TMetadata metadata,
         Func<GenericEventHandler<EnumerationStartedEventArgs<TMetadata>>> getGlobalBeforeEnumerationExecutionStart,
         Func<GenericEventHandler<EnumerationStartedEventArgs<TMetadata>>> getGlobalAfterEnumerationExecutionStart,
         Func<GenericEventHandler<EnumerationEndedEventArgs<TMetadata>>> getGlobalBeforeEnumerationExecutionEnd,
         Func<GenericEventHandler<EnumerationEndedEventArgs<TMetadata>>> getGlobalAfterEnumerationExecutionEnd,
         Func<GenericEventHandler<EnumerationItemEventArgs<T, TMetadata>>> getGlobalAfterEnumerationExecutionItemEncountered
         ) : base( hasNext, getNext, dispose, getGlobalBeforeEnumerationExecutionStart, getGlobalAfterEnumerationExecutionStart, getGlobalBeforeEnumerationExecutionEnd, getGlobalAfterEnumerationExecutionEnd, getGlobalAfterEnumerationExecutionItemEncountered )
      {
         this.Metadata = metadata;
      }

      event GenericEventHandler<EnumerationStartedEventArgs> AsyncEnumerationObservation<T>.BeforeEnumerationStart
      {
         add
         {
            this.BeforeEnumerationStart += value;
         }

         remove
         {
            this.BeforeEnumerationStart -= value;
         }
      }

      event GenericEventHandler<EnumerationStartedEventArgs> AsyncEnumerationObservation<T>.AfterEnumerationStart
      {
         add
         {
            this.AfterEnumerationStart += value;
         }

         remove
         {
            this.AfterEnumerationStart -= value;
         }
      }

      event GenericEventHandler<EnumerationItemEventArgs<T>> AsyncEnumerationObservation<T>.AfterEnumerationItemEncountered
      {
         add
         {
            this.AfterEnumerationItemEncountered += value;
         }
         remove
         {
            this.AfterEnumerationItemEncountered -= value;
         }
      }

      event GenericEventHandler<EnumerationEndedEventArgs> AsyncEnumerationObservation<T>.BeforeEnumerationEnd
      {
         add
         {
            this.BeforeEnumerationEnd += value;
         }

         remove
         {
            this.BeforeEnumerationEnd -= value;
         }
      }

      event GenericEventHandler<EnumerationEndedEventArgs> AsyncEnumerationObservation<T>.AfterEnumerationEnd
      {
         add
         {
            this.AfterEnumerationEnd += value;
         }

         remove
         {
            this.AfterEnumerationEnd -= value;
         }
      }

      public TMetadata Metadata { get; }

      protected override EnumerationStartedEventArgs<TMetadata> CreateBeforeEnumerationStartedArgs()
      {
         return new EnumerationStartedEventArgsImpl<TMetadata>( this.Metadata );
      }

      protected override EnumerationStartedEventArgs<TMetadata> CreateAfterEnumerationStartedArgs( EnumerationStartedEventArgs<TMetadata> beforeStart )
      {
         return beforeStart ?? this.CreateBeforeEnumerationStartedArgs();
      }

      protected override EnumerationItemEventArgs<T, TMetadata> CreateEnumerationItemArgs( T item )
      {
         return new EnumerationItemEventArgsImpl<T, TMetadata>( item, this.Metadata );
      }

      protected override EnumerationEndedEventArgs<TMetadata> CreateBeforeEnumerationEndedArgs()
      {
         return new EnumerationEndedEventArgsImpl<TMetadata>( this.Metadata );
      }

      protected override EnumerationEndedEventArgs<TMetadata> CreateAfterEnumerationEndedArgs( EnumerationEndedEventArgs<TMetadata> beforeEnd )
      {
         return beforeEnd ?? this.CreateBeforeEnumerationEndedArgs();
      }
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
