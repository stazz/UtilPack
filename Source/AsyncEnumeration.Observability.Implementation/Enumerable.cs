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
using AsyncEnumeration.Observability;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Observability
{


   internal abstract class AbstractAsyncEnumeratorObservable<T, TStartedArgs, TEndedArgs, TItemArgs> : IAsyncEnumerator<T>
      where TStartedArgs : class
      where TEndedArgs : class
      where TItemArgs : class
   {
      private readonly IAsyncEnumerator<T> _source;
      private Int32 _state;

      private const Int32 STATE_INITIAL = 0;
      private const Int32 STATE_STARTED = 1;
      private const Int32 STATE_ENDED = 2;

      protected AbstractAsyncEnumeratorObservable(
         IAsyncEnumerator<T> source
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
      }

      //public Boolean IsConcurrentEnumerationSupported => this._source.IsConcurrentEnumerationSupported;

      public async Task<Boolean> WaitForNextAsync()
      {
         Boolean retVal;
         if ( Interlocked.CompareExchange( ref this._state, STATE_STARTED, STATE_INITIAL ) == STATE_INITIAL )
         {
            // Initial call
            TStartedArgs args = null;
            this.BeforeEnumerationStart?.InvokeAllEventHandlers( args = this.CreateBeforeEnumerationStartedArgs(), throwExceptions: false );
            try
            {
               retVal = await this._source.WaitForNextAsync();
            }
            finally
            {
               this.AfterEnumerationStart?.InvokeAllEventHandlers( args = this.CreateAfterEnumerationStartedArgs( args ), throwExceptions: false );
            }
         }
         else
         {
            retVal = await this._source.WaitForNextAsync();
         }

         //if ( !retVal )
         //{
         //   Interlocked.CompareExchange( ref this._state, STATE_ENDED, STATE_STARTED );
         //}

         return retVal;
      }

      public T TryGetNext( out Boolean success )
      {
         var item = this._source.TryGetNext( out success );
         if ( success )
         {
            TItemArgs args = null;
            this.AfterEnumerationItemEncountered?.InvokeAllEventHandlers( args = this.CreateEnumerationItemArgs( item ), throwExceptions: false );
         }
         return item;
      }

      public Task DisposeAsync()
      {
         //var moveNextReturnedFalse = this._state == STATE_ENDED;
         TEndedArgs args = null;
         this.BeforeEnumerationEnd?.InvokeAllEventHandlers( args = this.CreateBeforeEnumerationEndedArgs(), throwExceptions: false );

         return this._source.DisposeAsync().ContinueWith( t =>
            this.AfterEnumerationEnd?.InvokeAllEventHandlers( args = this.CreateAfterEnumerationEndedArgs( args ), throwExceptions: false ),
            TaskContinuationOptions.ExecuteSynchronously );
      }

      public event GenericEventHandler<TStartedArgs> BeforeEnumerationStart;
      public event GenericEventHandler<TStartedArgs> AfterEnumerationStart;

      public event GenericEventHandler<TEndedArgs> BeforeEnumerationEnd;
      public event GenericEventHandler<TEndedArgs> AfterEnumerationEnd;

      public event GenericEventHandler<TItemArgs> AfterEnumerationItemEncountered;

      protected abstract TStartedArgs CreateBeforeEnumerationStartedArgs();

      protected abstract TStartedArgs CreateAfterEnumerationStartedArgs( TStartedArgs beforeStart );

      protected abstract TItemArgs CreateEnumerationItemArgs( T item );

      protected abstract TEndedArgs CreateBeforeEnumerationEndedArgs( /*Boolean moveNextReturnedFalse*/ );

      protected abstract TEndedArgs CreateAfterEnumerationEndedArgs( /*Boolean moveNextReturnedFalse,*/ TEndedArgs beforeEnd );

   }

   internal sealed class AsyncEnumeratorObservable<T> : AbstractAsyncEnumeratorObservable<T, EnumerationStartedEventArgs, EnumerationEndedEventArgs, EnumerationItemEventArgs<T>>, AsyncEnumerationObservation<T>
   {
      public AsyncEnumeratorObservable(
         IAsyncEnumerator<T> source
         ) : base( source )
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

      protected override EnumerationEndedEventArgs CreateBeforeEnumerationEndedArgs(/* Boolean moveNextReturnedFalse*/ )
      {
         return EnumerationEventArgsUtility.StatelessEndArgs;// moveNextReturnedFalse ? EnumerationEventArgsUtility.StatelessEndWithSuccessArgs : EnumerationEventArgsUtility.StatelessEndWithNoSuccessArgs;
      }

      protected override EnumerationEndedEventArgs CreateAfterEnumerationEndedArgs( /*Boolean moveNextReturnedFalse,*/ EnumerationEndedEventArgs beforeEnd )
      {
         return beforeEnd ?? this.CreateBeforeEnumerationEndedArgs();
      }
   }

   internal abstract class AbstractAsyncEnumeratorObservable<T, TMetadata> : AbstractAsyncEnumeratorObservable<T, EnumerationStartedEventArgs<TMetadata>, EnumerationEndedEventArgs<TMetadata>, EnumerationItemEventArgs<T, TMetadata>>, AsyncEnumerationObservation<T, TMetadata>
   {
      protected AbstractAsyncEnumeratorObservable(
         IAsyncEnumerator<T> source,
         TMetadata metadata
         ) : base( source )
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

      protected override EnumerationStartedEventArgs<TMetadata> CreateAfterEnumerationStartedArgs( EnumerationStartedEventArgs<TMetadata> beforeStart )
      {
         return beforeStart ?? this.CreateBeforeEnumerationStartedArgs();
      }

      protected override EnumerationItemEventArgs<T, TMetadata> CreateEnumerationItemArgs( T item )
      {
         return new EnumerationItemEventArgsImpl<T, TMetadata>( item, this.Metadata );
      }

      protected override EnumerationEndedEventArgs<TMetadata> CreateAfterEnumerationEndedArgs( /*Boolean moveNextReturnedFalse,*/ EnumerationEndedEventArgs<TMetadata> beforeEnd )
      {
         return beforeEnd ?? this.CreateBeforeEnumerationEndedArgs( /*moveNextReturnedFalse*/ );
      }

   }

   internal sealed class AsyncEnumeratorObservable<T, TMetadata> : AbstractAsyncEnumeratorObservable<T, TMetadata>
   {
      public AsyncEnumeratorObservable(
         IAsyncEnumerator<T> source,
         TMetadata metadata
         ) : base( source, metadata )
      {
      }

      protected override EnumerationStartedEventArgs<TMetadata> CreateBeforeEnumerationStartedArgs()
      {
         return new EnumerationStartedEventArgsImpl<TMetadata>( this.Metadata );
      }

      protected override EnumerationEndedEventArgs<TMetadata> CreateBeforeEnumerationEndedArgs( /* Boolean moveNextReturnedFalse*/ )
      {
         return new EnumerationEndedEventArgsImpl<TMetadata>( this.Metadata );
      }


   }

   internal sealed class AsyncEnumeratorObservableFromEnumerable<T, TMetadata> : AbstractAsyncEnumeratorObservable<T, TMetadata>
   {
      private readonly Func<EnumerationStartedEventArgs<TMetadata>> _started;
      private readonly Func<EnumerationEndedEventArgs<TMetadata>> _ended;

      public AsyncEnumeratorObservableFromEnumerable(
         IAsyncEnumerator<T> source,
         TMetadata metadata,
         Func<EnumerationStartedEventArgs<TMetadata>> started,
         Func<EnumerationEndedEventArgs<TMetadata>> ended
         ) : base( source, metadata )
      {
         this._started = ArgumentValidator.ValidateNotNull( nameof( started ), started );
         this._ended = ArgumentValidator.ValidateNotNull( nameof( ended ), ended );
      }

      protected override EnumerationStartedEventArgs<TMetadata> CreateBeforeEnumerationStartedArgs()
      {
         return this._started();
      }

      protected override EnumerationEndedEventArgs<TMetadata> CreateBeforeEnumerationEndedArgs( /*Boolean moveNextReturnedFalse*/ )
      {
         return this._ended();
      }
   }

   internal abstract class AbstractAsyncEnumerableObservable<TStartedArgs, TEndedArgs, TItemArgs, TEnumerable>
      where TEnumerable : class
   {
      protected readonly TEnumerable _source;
      protected readonly GenericEventHandler<TStartedArgs> _beforeStart;
      protected readonly GenericEventHandler<TStartedArgs> _afterStart;
      protected readonly GenericEventHandler<TItemArgs> _afterItem;
      protected readonly GenericEventHandler<TEndedArgs> _beforeEnd;
      protected readonly GenericEventHandler<TEndedArgs> _afterEnd;


      protected AbstractAsyncEnumerableObservable(
         TEnumerable source
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._beforeStart = ( args ) => this.BeforeEnumerationStart?.InvokeAllEventHandlers( args, throwExceptions: false );
         this._afterStart = ( args ) => this.AfterEnumerationStart?.InvokeAllEventHandlers( args, throwExceptions: false );
         this._afterItem = ( args ) => this.AfterEnumerationItemEncountered?.InvokeAllEventHandlers( args, throwExceptions: false );
         this._beforeEnd = ( args ) => this.BeforeEnumerationEnd?.InvokeAllEventHandlers( args, throwExceptions: false );
         this._afterEnd = ( args ) => this.AfterEnumerationEnd?.InvokeAllEventHandlers( args, throwExceptions: false );
      }

      public event GenericEventHandler<TStartedArgs> BeforeEnumerationStart;
      public event GenericEventHandler<TStartedArgs> AfterEnumerationStart;
      public event GenericEventHandler<TItemArgs> AfterEnumerationItemEncountered;
      public event GenericEventHandler<TEndedArgs> BeforeEnumerationEnd;
      public event GenericEventHandler<TEndedArgs> AfterEnumerationEnd;
   }

   internal sealed class AsyncEnumerableObservable<T> : AbstractAsyncEnumerableObservable<EnumerationStartedEventArgs, EnumerationEndedEventArgs, EnumerationItemEventArgs<T>, IAsyncEnumerable<T>>, IAsyncEnumerableObservable<T>
   {
      public AsyncEnumerableObservable(
         IAsyncEnumerable<T> source
         ) : base( source )
      {
      }

      public IAsyncEnumerator<T> GetAsyncEnumerator()
      {
         var enumerator = this._source.GetAsyncEnumerator();
         var retVal = enumerator is AsyncEnumeratorObservable<T> existing ? existing : new AsyncEnumeratorObservable<T>( enumerator );
         this.RegisterEvents( retVal );
         return retVal;
      }

      IAsyncProvider IAsyncEnumerable.AsyncProvider => this._source.AsyncProvider;

      private void RegisterEvents( AsyncEnumerationObservation<T> enumerator )
      {
         enumerator.BeforeEnumerationStart += this._beforeStart;
         enumerator.AfterEnumerationStart += this._afterStart;
         enumerator.AfterEnumerationItemEncountered += this._afterItem;
         enumerator.BeforeEnumerationEnd += this._beforeEnd;

         void afterEnd( EnumerationEndedEventArgs args )
         {
            this._afterEnd( args );
            enumerator.BeforeEnumerationStart -= this._beforeStart;
            enumerator.AfterEnumerationStart -= this._afterStart;
            enumerator.AfterEnumerationItemEncountered -= this._afterItem;
            enumerator.BeforeEnumerationEnd -= this._beforeEnd;
            enumerator.AfterEnumerationEnd -= afterEnd;
         }

         enumerator.AfterEnumerationEnd += afterEnd;
      }
   }

   internal sealed class AsyncEnumerableObservable<T, TMetadata> : AbstractAsyncEnumerableObservable<EnumerationStartedEventArgs<TMetadata>, EnumerationEndedEventArgs<TMetadata>, EnumerationItemEventArgs<T, TMetadata>, IAsyncEnumerable<T>>, IAsyncEnumerableObservable<T, TMetadata>
   {
      private EnumerationStartedEventArgs<TMetadata> _cachedStarted;
      private EnumerationEndedEventArgs<TMetadata> _cachedEnded;

      private readonly Func<EnumerationStartedEventArgs<TMetadata>> _getStartedArgs;
      private readonly Func<EnumerationEndedEventArgs<TMetadata>> _getEndedArgs;
      //private EnumerationEndedEventArgs<TMetadata> _cachedEndedNoSuccess;

      public AsyncEnumerableObservable(
         IAsyncEnumerable<T> source,
         TMetadata metadata
         ) : base( source )
      {
         this.Metadata = metadata;
         this._getStartedArgs = () => CreateStartedArgs( ref this._cachedStarted, this.Metadata );
         this._getEndedArgs = () => CreateEndedArgs( ref this._cachedEnded, this.Metadata );
      }

      public TMetadata Metadata { get; }

      IAsyncProvider IAsyncEnumerable.AsyncProvider => this._source.AsyncProvider;

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

      public IAsyncEnumerator<T> GetAsyncEnumerator()
      {
         var enumerator = this._source.GetAsyncEnumerator();
         var retVal = enumerator is AsyncEnumeratorObservableFromEnumerable<T, TMetadata> existing ? existing : new AsyncEnumeratorObservableFromEnumerable<T, TMetadata>( enumerator, this.Metadata, this._getStartedArgs, this._getEndedArgs );
         this.RegisterEvents( retVal );
         return retVal;
      }


      private void RegisterEvents( AsyncEnumerationObservation<T, TMetadata> enumerator )
      {
         enumerator.BeforeEnumerationStart += this._beforeStart;
         enumerator.AfterEnumerationStart += this._afterStart;
         enumerator.AfterEnumerationItemEncountered += this._afterItem;
         enumerator.BeforeEnumerationEnd += this._beforeEnd;
         GenericEventHandler<EnumerationEndedEventArgs<TMetadata>> afterEnd = null;
         afterEnd = ( args ) =>
         {
            this._afterEnd( args );
            enumerator.BeforeEnumerationStart -= this._beforeStart;
            enumerator.AfterEnumerationStart -= this._afterStart;
            enumerator.AfterEnumerationItemEncountered -= this._afterItem;
            enumerator.BeforeEnumerationEnd -= this._beforeEnd;
            enumerator.AfterEnumerationEnd -= afterEnd;
         };
         enumerator.AfterEnumerationEnd += afterEnd;
      }

      private static EnumerationStartedEventArgs<TMetadata> CreateStartedArgs( ref EnumerationStartedEventArgs<TMetadata> field, TMetadata metadata )
      {
         Interlocked.CompareExchange( ref field, new EnumerationStartedEventArgsImpl<TMetadata>( metadata ), null );
         return field;
      }

      private static EnumerationEndedEventArgs<TMetadata> CreateEndedArgs( ref EnumerationEndedEventArgs<TMetadata> field, TMetadata metadata )
      {
         Interlocked.CompareExchange( ref field, new EnumerationEndedEventArgsImpl<TMetadata>( metadata ), null );
         return field;
      }
   }
}

public static partial class E_UtilPack
{
   /// <summary>
   /// Adds observability aspect to this <see cref="IAsyncEnumerable{T}"/>, if it is not already present.
   /// </summary>
   /// <typeparam name="T">The type of enumerable items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>A <see cref="IAsyncEnumerableObservable{T}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   public static IAsyncEnumerableObservable<T> AsObservable<T>(
      this IAsyncEnumerable<T> enumerable
      )
   {
      // Don't wrap too many times
      return enumerable is IAsyncEnumerableObservable<T> existing ?
         existing :
         new AsyncEnumerableObservable<T>( enumerable );
   }

   /// <summary>
   /// Adds observability with metadata -aspect to this <see cref="IAsyncEnumerable{T}"/>, if it is not already present.
   /// </summary>
   /// <typeparam name="T">The type of enumerable items.</typeparam>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="metadata">The metadata to have.</param>
   /// <returns>A <see cref="IAsyncEnumerableObservable{T, TMetadata}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   public static IAsyncEnumerableObservable<T, TMetadata> AsObservable<T, TMetadata>(
      this IAsyncEnumerable<T> enumerable,
      TMetadata metadata
      )
   {
      // Don't wrap too many times
      return enumerable is IAsyncEnumerableObservable<T, TMetadata> existing ?
         existing :
         new AsyncEnumerableObservable<T, TMetadata>( enumerable, metadata );
   }

}
