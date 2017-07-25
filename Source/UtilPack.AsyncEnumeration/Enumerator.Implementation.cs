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
using UtilPack;

namespace UtilPack.AsyncEnumeration
{
   internal abstract class AsyncEnumeratorImpl<T> : AsyncEnumerator<T>
   {
      // This will cause one heap allocation every time Current setter is called when type T is a struct.
      // It is fine for now, since asynchronous enumerators for struct types will most likely be very rare.
      internal sealed class CurrentInfo
      {
         private Object _current;

         public CurrentInfo(
            T current,
            MoveNextAsyncDelegate<T> moveNext,
            DisposeAsyncDelegate disposeDelegate
            )
         {
            this.MoveNext = moveNext;
            this.Current = current;
            this.Dispose = disposeDelegate;
         }

         public MoveNextAsyncDelegate<T> MoveNext { get; }
         public DisposeAsyncDelegate Dispose { get; }
         public T Current
         {
            get
            {
               return (T) this._current;
            }
            set
            {
               Interlocked.Exchange( ref this._current, value );
            }
         }
      }

      private const Int32 STATE_INITIAL = 0;
      private const Int32 MOVE_NEXT_STARTED = 1;
      private const Int32 MOVE_NEXT_ENDED = 2;
      private const Int32 STATE_ENDED = 3;
      private const Int32 RESETTING = 4;

      private Int32 _state;
      private CurrentInfo _current;
      private readonly InitialMoveNextAsyncDelegate<T> _initialMoveNext;


      public AsyncEnumeratorImpl(
         InitialMoveNextAsyncDelegate<T> initialMoveNext
         )
      {
         this._state = STATE_INITIAL;
         this._current = null;
         this._initialMoveNext = ArgumentValidator.ValidateNotNull( nameof( initialMoveNext ), initialMoveNext );
      }

      public async ValueTask<Boolean> MoveNextAsync( CancellationToken token )
      {
         // We can call move next only in initial state, or after we have called it once
         Boolean retVal = false;
         var wasNotInitial = Interlocked.CompareExchange( ref this._state, MOVE_NEXT_STARTED, MOVE_NEXT_ENDED ) == MOVE_NEXT_ENDED;
         if ( wasNotInitial || Interlocked.CompareExchange( ref this._state, MOVE_NEXT_STARTED, STATE_INITIAL ) == STATE_INITIAL )
         {
            DisposeAsyncDelegate disposeDelegate = null;

            try
            {

               if ( wasNotInitial )
               {
                  var moveNext = this._current.MoveNext;
                  if ( moveNext == null )
                  {
                     retVal = false;
                  }
                  else
                  {
                     T current;
                     (retVal, current) = await moveNext( token );
                     if ( retVal )
                     {
                        this._current.Current = current;
                     }
                  }
               }
               else
               {
                  // First time calling move next
                  var result = await this.CallInitialMoveNext( token, this._initialMoveNext );
                  retVal = result.Item1;
                  if ( retVal )
                  {
                     Interlocked.Exchange( ref this._current, new CurrentInfo( result.Item2, result.Item3, result.Item4 ) );
                  }
                  else
                  {
                     disposeDelegate = result.Item4;
                  }
               }
            }
            finally
            {
               try
               {
                  if ( retVal )
                  {
                     var t = this.AfterMoveNextSucessful();
                     if ( t != null )
                     {
                        await t;
                     }
                  }
                  else
                  {
                     await this.PerformDispose( token, disposeDelegate );
                  }
               }
               catch
               {
                  // Ignore.
               }

               if ( !retVal )
               {
                  Interlocked.Exchange( ref this._current, null );
               }
               Interlocked.Exchange( ref this._state, retVal ? MOVE_NEXT_ENDED : STATE_ENDED );
            }
         }
         else if ( this._state != STATE_ENDED )
         {
            // Re-entrancy or concurrent with Reset -> exception
            throw new InvalidOperationException( "Tried to concurrently move to next or reset." );
         }
         return retVal;
      }

      public T Current
      {
         get
         {
            var cur = this._current;
            return cur == null ? default( T ) : cur.Current;
         }
      }

      public async ValueTask<Boolean> TryResetAsync( CancellationToken token )
      {
         // We can reset from MOVE_NEXT_STARTED and STATE_ENDED states
         var retVal = false;
         if (
            Interlocked.CompareExchange( ref this._state, RESETTING, MOVE_NEXT_STARTED ) == MOVE_NEXT_STARTED
            || Interlocked.CompareExchange( ref this._state, RESETTING, STATE_ENDED ) == STATE_ENDED
            )
         {
            try
            {
               var moveNext = this._current?.MoveNext;
               if ( moveNext != null )
               {
                  while ( ( await moveNext( token ) ).Item1 ) ;
               }
            }
            finally
            {
               try
               {
                  await this.PerformDispose( token );
               }
               catch
               {
                  // Ignore
               }

               Interlocked.Exchange( ref this._state, STATE_INITIAL );
               retVal = true;
            }
         }
         //else if ( this._state != STATE_INITIAL )
         //{
         //   // Re-entrancy or concurrent with move next -> exception
         //   throw new InvalidOperationException( "Tried to concurrently reset or move to next." );
         //}

         return retVal;
      }

      protected virtual async ValueTask<Boolean> PerformDispose( CancellationToken token, DisposeAsyncDelegate disposeDelegate = null )
      {
         var prev = Interlocked.Exchange( ref this._current, null );
         var retVal = false;
         if ( prev != null || disposeDelegate != null )
         {
            if ( disposeDelegate == null )
            {
               disposeDelegate = prev.Dispose;
            }

            if ( disposeDelegate != null )
            {
               var taskToAwait = disposeDelegate( token );
               if ( taskToAwait != null )
               {
                  await taskToAwait;
               }
               retVal = true;
            }
         }

         return retVal;
      }

      protected virtual ValueTask<(Boolean, T, MoveNextAsyncDelegate<T>, DisposeAsyncDelegate)> CallInitialMoveNext( CancellationToken token, InitialMoveNextAsyncDelegate<T> initialMoveNext )
      {
         return initialMoveNext( token );
      }

      protected virtual Task AfterMoveNextSucessful()
      {
         return null;
      }

   }

   internal sealed class AsyncEnumeratorImplNonObservable<T, TMetadata> : AsyncEnumeratorImpl<T>, AsyncEnumerator<T, TMetadata>
   {
      public AsyncEnumeratorImplNonObservable(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         TMetadata metadata
         ) : base( initialMoveNext )
      {
         this.Metadata = metadata;
      }

      public TMetadata Metadata { get; }
   }

   internal sealed class AsyncEnumeratorImplNonObservable<T> : AsyncEnumeratorImpl<T>
   {
      public AsyncEnumeratorImplNonObservable(
         InitialMoveNextAsyncDelegate<T> initialMoveNext
         ) : base( initialMoveNext )
      {
      }
   }

   internal abstract class AsyncEnumeratorObservableImpl<T, TStartedArgs, TEndedArgs, TItemArgs> : AsyncEnumeratorImpl<T>
      where TStartedArgs : class, EnumerationStartedEventArgs
      where TEndedArgs : class, EnumerationEndedEventArgs
      where TItemArgs : class, EnumerationItemEventArgs<T>
   {
      protected readonly Func<GenericEventHandler<TStartedArgs>> _getGlobalBeforeEnumerationExecutionStart;
      protected readonly Func<GenericEventHandler<TStartedArgs>> _getGlobalAfterEnumerationExecutionStart;
      protected readonly Func<GenericEventHandler<TEndedArgs>> _getGlobalBeforeEnumerationExecutionEnd;
      protected readonly Func<GenericEventHandler<TEndedArgs>> _getGlobalAfterEnumerationExecutionEnd;
      protected readonly Func<GenericEventHandler<TItemArgs>> _getGlobalAfterEnumerationExecutionItemEncountered;

      protected AsyncEnumeratorObservableImpl(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         Func<GenericEventHandler<TStartedArgs>> getGlobalBeforeEnumerationExecutionStart,
         Func<GenericEventHandler<TStartedArgs>> getGlobalAfterEnumerationExecutionStart,
         Func<GenericEventHandler<TEndedArgs>> getGlobalBeforeEnumerationExecutionEnd,
         Func<GenericEventHandler<TEndedArgs>> getGlobalAfterEnumerationExecutionEnd,
         Func<GenericEventHandler<TItemArgs>> getGlobalAfterEnumerationExecutionItemEncountered
         ) : base( initialMoveNext )
      {
         this._getGlobalBeforeEnumerationExecutionStart = getGlobalBeforeEnumerationExecutionStart;
         this._getGlobalAfterEnumerationExecutionStart = getGlobalAfterEnumerationExecutionStart;
         this._getGlobalBeforeEnumerationExecutionEnd = getGlobalBeforeEnumerationExecutionEnd;
         this._getGlobalAfterEnumerationExecutionEnd = getGlobalAfterEnumerationExecutionEnd;
         this._getGlobalAfterEnumerationExecutionItemEncountered = getGlobalAfterEnumerationExecutionItemEncountered;
      }

      public event GenericEventHandler<TStartedArgs> BeforeEnumerationStart;
      public event GenericEventHandler<TStartedArgs> AfterEnumerationStart;

      public event GenericEventHandler<TEndedArgs> BeforeEnumerationEnd;
      public event GenericEventHandler<TEndedArgs> AfterEnumerationEnd;

      public event GenericEventHandler<TItemArgs> AfterEnumerationItemEncountered;

      protected override async ValueTask<(Boolean, T, MoveNextAsyncDelegate<T>, DisposeAsyncDelegate)> CallInitialMoveNext( CancellationToken token, InitialMoveNextAsyncDelegate<T> initialMoveNext )
      {
         TStartedArgs args = null;
         this.BeforeEnumerationStart?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateBeforeEnumerationStartedArgs() ) ), throwExceptions: false );
         this._getGlobalBeforeEnumerationExecutionStart?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? ( args = this.CreateBeforeEnumerationStartedArgs() ) ), throwExceptions: false );
         try
         {
            return await base.CallInitialMoveNext( token, initialMoveNext );
         }
         finally
         {
            this.AfterEnumerationStart?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateAfterEnumerationStartedArgs( args ) ) ), throwExceptions: false );
            this._getGlobalAfterEnumerationExecutionStart?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? this.CreateAfterEnumerationStartedArgs( args ) ), throwExceptions: false );
         }
      }

      protected override Task AfterMoveNextSucessful()
      {
         TItemArgs args = null;
         this.AfterEnumerationItemEncountered?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateEnumerationItemArgs() ) ), throwExceptions: false );
         this._getGlobalAfterEnumerationExecutionItemEncountered?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? this.CreateEnumerationItemArgs() ), throwExceptions: false );
         return base.AfterMoveNextSucessful();
      }

      protected override ValueTask<Boolean> PerformDispose( CancellationToken token, DisposeAsyncDelegate disposeDelegate = null )
      {
         TEndedArgs args = null;
         this.BeforeEnumerationEnd?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateBeforeEnumerationEndedArgs() ) ), throwExceptions: false );
         this._getGlobalBeforeEnumerationExecutionEnd?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? ( args = this.CreateBeforeEnumerationEndedArgs() ) ), throwExceptions: false );
         try
         {
            return base.PerformDispose( token, disposeDelegate );
         }
         finally
         {
            this.AfterEnumerationEnd?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateAfterEnumerationEndedArgs( args ) ) ), throwExceptions: false );
            this._getGlobalAfterEnumerationExecutionEnd?.Invoke()?.InvokeAllEventHandlers( evt => evt( args ?? this.CreateAfterEnumerationEndedArgs( args ) ), throwExceptions: false );
         }

      }

      protected abstract TStartedArgs CreateBeforeEnumerationStartedArgs();

      protected abstract TStartedArgs CreateAfterEnumerationStartedArgs( TStartedArgs beforeStart );

      protected abstract TItemArgs CreateEnumerationItemArgs();

      protected abstract TEndedArgs CreateBeforeEnumerationEndedArgs();

      protected abstract TEndedArgs CreateAfterEnumerationEndedArgs( TEndedArgs beforeEnd );
   }

   internal sealed class AsyncEnumeratorObservableImpl<T> : AsyncEnumeratorObservableImpl<T, EnumerationStartedEventArgs, EnumerationEndedEventArgs, EnumerationItemEventArgs<T>>, AsyncEnumeratorObservable<T>
   {
      public AsyncEnumeratorObservableImpl(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         Func<GenericEventHandler<EnumerationStartedEventArgs>> getGlobalBeforeEnumerationExecutionStart,
         Func<GenericEventHandler<EnumerationStartedEventArgs>> getGlobalAfterEnumerationExecutionStart,
         Func<GenericEventHandler<EnumerationEndedEventArgs>> getGlobalBeforeEnumerationExecutionEnd,
         Func<GenericEventHandler<EnumerationEndedEventArgs>> getGlobalAfterEnumerationExecutionEnd,
         Func<GenericEventHandler<EnumerationItemEventArgs<T>>> getGlobalAfterEnumerationExecutionItemEncountered
         ) : base( initialMoveNext, getGlobalBeforeEnumerationExecutionStart, getGlobalAfterEnumerationExecutionStart, getGlobalBeforeEnumerationExecutionEnd, getGlobalAfterEnumerationExecutionEnd, getGlobalAfterEnumerationExecutionItemEncountered )
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

      protected override EnumerationItemEventArgs<T> CreateEnumerationItemArgs()
      {
         return new EnumerationItemEventArgsImpl<T>( this.Current );
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

   internal sealed class AsyncEnumeratorObservableImpl<T, TMetadata> : AsyncEnumeratorObservableImpl<T, EnumerationStartedEventArgs<TMetadata>, EnumerationEndedEventArgs<TMetadata>, EnumerationItemEventArgs<T, TMetadata>>, AsyncEnumeratorObservable<T, TMetadata>
   {

      public AsyncEnumeratorObservableImpl(
         InitialMoveNextAsyncDelegate<T> initialMoveNext,
         TMetadata metadata,
         Func<GenericEventHandler<EnumerationStartedEventArgs<TMetadata>>> getGlobalBeforeEnumerationExecutionStart,
         Func<GenericEventHandler<EnumerationStartedEventArgs<TMetadata>>> getGlobalAfterEnumerationExecutionStart,
         Func<GenericEventHandler<EnumerationEndedEventArgs<TMetadata>>> getGlobalBeforeEnumerationExecutionEnd,
         Func<GenericEventHandler<EnumerationEndedEventArgs<TMetadata>>> getGlobalAfterEnumerationExecutionEnd,
         Func<GenericEventHandler<EnumerationItemEventArgs<T, TMetadata>>> getGlobalAfterEnumerationExecutionItemEncountered
         ) : base( initialMoveNext, getGlobalBeforeEnumerationExecutionStart, getGlobalAfterEnumerationExecutionStart, getGlobalBeforeEnumerationExecutionEnd, getGlobalAfterEnumerationExecutionEnd, getGlobalAfterEnumerationExecutionItemEncountered )
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

      protected override EnumerationItemEventArgs<T, TMetadata> CreateEnumerationItemArgs()
      {
         return new EnumerationItemEventArgsImpl<T, TMetadata>( this.Current, this.Metadata );
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

   /// <summary>
   /// This class provides default implementation for <see cref="EnumerationStartedEventArgs{TMetadata}"/>.
   /// </summary>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <seealso cref="ObjectWithMetadata{TMetadata}.Metadata"/>
   /// <seealso cref="AsyncEnumerator{T, TMetadata}"/>
   public class EnumerationStartedEventArgsImpl<TMetadata> : EnumerationStartedEventArgs<TMetadata>
   {
      /// <summary>
      /// Creates a new instance of <see cref="EnumerationStartedEventArgsImpl{TMetadata}"/> with given metadata.
      /// </summary>
      /// <param name="metadata">The metadata.</param>
      public EnumerationStartedEventArgsImpl(
         TMetadata metadata
         )
      {
         this.Metadata = metadata;
      }

      /// <summary>
      /// Gets the metadata.
      /// </summary>
      /// <value>The metadata.</value>
      public TMetadata Metadata { get; }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="EnumerationItemEventArgs{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <seealso cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/>
   /// <seealso cref="AsyncEnumerator{T}.Current"/>
   public class EnumerationItemEventArgsImpl<T> : EnumerationItemEventArgs<T>
   {
      /// <summary>
      /// Creates a new instance of <see cref="EnumerationItemEventArgsImpl{T}"/> with given item.
      /// </summary>
      /// <param name="item">The item that was encountered by <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/>.</param>
      public EnumerationItemEventArgsImpl(
         T item
         )
      {
         this.Item = item;
      }

      /// <summary>
      /// Gets the item encountered by <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/>.
      /// </summary>
      /// <value>The item encountered by <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/>.</value>
      public T Item { get; }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="EnumerationEndedEventArgs{TMetadata}"/>.
   /// </summary>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <seealso cref="ObjectWithMetadata{TMetadata}.Metadata"/>
   /// <seealso cref="AsyncEnumerator{T, TMetadata}"/>
   public class EnumerationEndedEventArgsImpl<TMetadata> : EnumerationStartedEventArgsImpl<TMetadata>, EnumerationEndedEventArgs<TMetadata>
   {
      /// <summary>
      /// Creates a new instance of <see cref="EnumerationEndedEventArgsImpl{TMetadata}"/> with given metadata.
      /// </summary>
      /// <param name="metadata">The metadata.</param>
      public EnumerationEndedEventArgsImpl(
         TMetadata metadata
         ) : base( metadata )
      {
      }
   }

   /// <summary>
   /// This class provides default implementation for <see cref="EnumerationItemEventArgs{T, TMetadata}"/>.
   /// </summary>
   /// <typeparam name="T">The type of the items being enumerated.</typeparam>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <seealso cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/>
   /// <seealso cref="AsyncEnumerator{T}.Current"/>
   /// <seealso cref="ObjectWithMetadata{TMetadata}.Metadata"/>
   /// <seealso cref="AsyncEnumerator{T, TMetadata}"/>
   public class EnumerationItemEventArgsImpl<T, TMetadata> : EnumerationItemEventArgsImpl<T>, EnumerationItemEventArgs<T, TMetadata>
   {
      /// <summary>
      /// Creates a new instance of <see cref="EnumerationItemEventArgsImpl{T, TMetadata}"/> with given item and metadata.
      /// </summary>
      /// <param name="item">The item that was encountered by <see cref="AsyncEnumerator{T}.MoveNextAsync(CancellationToken)"/></param>
      /// <param name="metadata">The metadata.</param>
      public EnumerationItemEventArgsImpl(
         T item,
         TMetadata metadata
         )
         : base( item )
      {
         this.Metadata = metadata;
      }

      /// <summary>
      /// Gets the metadata.
      /// </summary>
      /// <value>The metadata.</value>
      public TMetadata Metadata { get; }
   }
}
