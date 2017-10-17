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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.AsyncEnumeration;

namespace UtilPack.AsyncEnumeration
{
   /// <summary>
   /// This interface augments <see cref="IAsyncConcurrentEnumerable{T}"/> with observation aspect.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   public interface IAsyncConcurrentEnumerableObservable<out T> : IAsyncConcurrentEnumerable<T>, AsyncEnumerationObservation<T>, IAsyncEnumerableObservable<T>
   {
      /// <summary>
      /// Gets the <see cref="IAsyncConcurrentEnumeratorSourceObservable{T}"/> to use to concurrently enumerate this <see cref="IAsyncConcurrentEnumerableObservable{T}"/>.
      /// </summary>
      /// <returns>A <see cref="IAsyncConcurrentEnumeratorSourceObservable{T}"/> which should be used to concurrently enumerate this <see cref="IAsyncConcurrentEnumerableObservable{T}"/>.</returns>
      new IAsyncConcurrentEnumeratorSourceObservable<T> GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments );
   }

   /// <summary>
   /// This interface augments <see cref="IAsyncConcurrentEnumerableObservable{T}"/> with metadata which will be passed on to event handlers.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TMetadata">The type of metadata to pass to event handlers.</typeparam>
   public interface IAsyncConcurrentEnumerableObservable<out T, out TMetadata> : IAsyncConcurrentEnumerableObservable<T>, AsyncEnumerationObservation<T, TMetadata>, IAsyncEnumerableObservable<T, TMetadata>
   {
      /// <summary>
      /// Gets the <see cref="IAsyncConcurrentEnumeratorSourceObservable{T, TMetadata}"/> to use to concurrently enumerate this <see cref="IAsyncConcurrentEnumerableObservable{T, TMetadata}"/>.
      /// </summary>
      /// <returns>A <see cref="IAsyncConcurrentEnumeratorSourceObservable{T, TMetadata}"/> which should be used to concurrently enumerate this <see cref="IAsyncConcurrentEnumerableObservable{T, TMetadata}"/>.</returns>
      new IAsyncConcurrentEnumeratorSourceObservable<T, TMetadata> GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments );
   }

   /// <summary>
   /// This interface augments the <see cref="IAsyncConcurrentEnumeratorSource{T}"/> with observability aspect.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   public interface IAsyncConcurrentEnumeratorSourceObservable<out T> : IAsyncConcurrentEnumeratorSource<T>, AsyncEnumerationObservation<T>
   {

   }

   /// <summary>
   /// This interface augments the <see cref="IAsyncConcurrentEnumeratorSourceObservable{T}"/> with metadata which will be passed on to event handlers.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TMetadata">The type of metadata to pass to event handlers.</typeparam>
   public interface IAsyncConcurrentEnumeratorSourceObservable<out T, out TMetadata> : IAsyncConcurrentEnumeratorSourceObservable<T>, AsyncEnumerationObservation<T, TMetadata>, ObjectWithMetadata<TMetadata>
   {

   }

   internal abstract class AbstractAsyncConcurrentEnumeratorSourceObservable<T, TStartedArgs, TEndedArgs, TItemArgs>
      where TStartedArgs : class
      where TEndedArgs : class
      where TItemArgs : class
   {
      private readonly IAsyncConcurrentEnumeratorSource<T> _source;
      private readonly GenericEventHandler<TStartedArgs> _beforeStarted;
      private readonly GenericEventHandler<TStartedArgs> _afterStarted;
      private readonly GenericEventHandler<TItemArgs> _afterItem;
      private Int32 _state;

      private const Int32 STATE_INITIAL = 0;
      private const Int32 STATE_STARTED = 1;
      private const Int32 STATE_ENDED = 2;

      public AbstractAsyncConcurrentEnumeratorSourceObservable(
         IAsyncConcurrentEnumeratorSource<T> source
         )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         this._beforeStarted = args =>
         {
            if ( Interlocked.CompareExchange( ref this._state, STATE_STARTED, STATE_INITIAL ) == STATE_INITIAL )
            {
               // We have captured the very first start
               this.BeforeEnumerationStart?.InvokeAllEventHandlers( evt => evt( args ), throwExceptions: false );
            }
         };
         this._afterStarted = args =>
         {
            if ( Interlocked.CompareExchange( ref this._state, STATE_ENDED, STATE_STARTED ) == STATE_STARTED )
            {
               this.AfterEnumerationStart?.InvokeAllEventHandlers( evt => evt( args ), throwExceptions: false );
            }
         };
         this._afterItem = args =>
         {
            this.AfterEnumerationItemEncountered?.InvokeAllEventHandlers( evt => evt( args ), throwExceptions: false );
         };
      }

      public IEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsEnumerable()
         => this._source.GetAsyncEnumeratorsEnumerable()
            ?.Select( enumerator => this.CreateObservableAndRegisterHandlers( enumerator, this._beforeStarted, this._afterStarted, this._afterItem ) );

      public IAsyncEnumerable<T> GetWrappedSynchronousSource()
         => this._source.GetWrappedSynchronousSource();

      //public IAsyncEnumerable<IAsyncEnumerator<T>> GetAsyncEnumeratorsAsyncEnumerable()
      //   => this._source.GetAsyncEnumeratorsAsyncEnumerable()
      //      ?.Select( enumerator => this.CreateObservableAndRegisterHandlers( enumerator, this._beforeStarted, this._afterStarted, this._afterItem ) );

      public Task DisposeAsync()
      {
         TEndedArgs args = null;
         this.BeforeEnumerationEnd?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateBeforeEnumerationEndedArgs() ) ), throwExceptions: false );

         return this._source.DisposeAsync().ContinueWith( t =>
            this.AfterEnumerationEnd?.InvokeAllEventHandlers( evt => evt( ( args = this.CreateAfterEnumerationEndedArgs( args ) ) ), throwExceptions: false ),
            TaskContinuationOptions.ExecuteSynchronously );
      }

      public event GenericEventHandler<TStartedArgs> BeforeEnumerationStart;
      public event GenericEventHandler<TStartedArgs> AfterEnumerationStart;

      public event GenericEventHandler<TEndedArgs> BeforeEnumerationEnd;
      public event GenericEventHandler<TEndedArgs> AfterEnumerationEnd;

      public event GenericEventHandler<TItemArgs> AfterEnumerationItemEncountered;

      //protected abstract TStartedArgs CreateBeforeEnumerationStartedArgs();

      //protected abstract TStartedArgs CreateAfterEnumerationStartedArgs( TStartedArgs beforeStart );

      //protected abstract TItemArgs CreateEnumerationItemArgs( T item );


      protected abstract TEndedArgs CreateBeforeEnumerationEndedArgs();

      protected abstract TEndedArgs CreateAfterEnumerationEndedArgs( TEndedArgs beforeEnd );

      protected abstract IAsyncEnumerator<T> CreateObservableAndRegisterHandlers(
         IAsyncEnumerator<T> enumerator,
         GenericEventHandler<TStartedArgs> beforeStart,
         GenericEventHandler<TStartedArgs> afterStart,
         GenericEventHandler<TItemArgs> onItem
         );
   }

   internal sealed class AsyncConcurrentEnumeratorSourceObservable<T> : AbstractAsyncConcurrentEnumeratorSourceObservable<T, EnumerationStartedEventArgs, EnumerationEndedEventArgs, EnumerationItemEventArgs<T>>, IAsyncConcurrentEnumeratorSourceObservable<T>
   {
      public AsyncConcurrentEnumeratorSourceObservable(
         IAsyncConcurrentEnumeratorSource<T> source
         ) : base( source )
      {
      }

      //protected override EnumerationStartedEventArgs CreateBeforeEnumerationStartedArgs()
      //{
      //   return EnumerationEventArgsUtility.StatelessStartArgs;
      //}

      //protected override EnumerationStartedEventArgs CreateAfterEnumerationStartedArgs( EnumerationStartedEventArgs beforeStart )
      //{
      //   return beforeStart ?? this.CreateBeforeEnumerationStartedArgs();
      //}

      //protected override EnumerationItemEventArgs<T> CreateEnumerationItemArgs( T item )
      //{
      //   return new EnumerationItemEventArgsImpl<T>( item );
      //}

      protected override EnumerationEndedEventArgs CreateBeforeEnumerationEndedArgs()
      {
         return EnumerationEventArgsUtility.StatelessEndArgs;
      }

      protected override EnumerationEndedEventArgs CreateAfterEnumerationEndedArgs( EnumerationEndedEventArgs beforeEnd )
      {
         return beforeEnd ?? this.CreateBeforeEnumerationEndedArgs();
      }

      protected override IAsyncEnumerator<T> CreateObservableAndRegisterHandlers(
         IAsyncEnumerator<T> enumerator,
         GenericEventHandler<EnumerationStartedEventArgs> beforeStart,
         GenericEventHandler<EnumerationStartedEventArgs> afterStart,
         GenericEventHandler<EnumerationItemEventArgs<T>> onItem
         )
      {
         var retVal = enumerator.AsObservable();
         retVal.BeforeEnumerationStart += beforeStart;
         retVal.AfterEnumerationStart += afterStart;
         retVal.AfterEnumerationItemEncountered += onItem;
         GenericEventHandler<EnumerationEndedEventArgs> afterEnd = null;
         afterEnd = args =>
         {
            retVal.BeforeEnumerationStart -= beforeStart;
            retVal.AfterEnumerationStart -= afterStart;
            retVal.AfterEnumerationItemEncountered -= onItem;
            retVal.AfterEnumerationEnd -= afterEnd;
         };
         retVal.AfterEnumerationEnd += afterEnd;

         return retVal;
      }
   }

   internal abstract class AbstractAsyncConcurrentEnumeratorSourceObservable<T, TMetadata> : AbstractAsyncConcurrentEnumeratorSourceObservable<T, EnumerationStartedEventArgs<TMetadata>, EnumerationEndedEventArgs<TMetadata>, EnumerationItemEventArgs<T, TMetadata>>, IAsyncConcurrentEnumeratorSourceObservable<T, TMetadata>
   {
      public AbstractAsyncConcurrentEnumeratorSourceObservable(
         IAsyncConcurrentEnumeratorSource<T> source,
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

      //protected override EnumerationStartedEventArgs<TMetadata> CreateAfterEnumerationStartedArgs( EnumerationStartedEventArgs<TMetadata> beforeStart )
      //{
      //   return beforeStart ?? this.CreateBeforeEnumerationStartedArgs();
      //}

      //protected override EnumerationItemEventArgs<T, TMetadata> CreateEnumerationItemArgs( T item )
      //{
      //   return new EnumerationItemEventArgsImpl<T, TMetadata>( item, this.Metadata );
      //}

      protected override EnumerationEndedEventArgs<TMetadata> CreateAfterEnumerationEndedArgs( EnumerationEndedEventArgs<TMetadata> beforeEnd )
      {
         return beforeEnd ?? this.CreateBeforeEnumerationEndedArgs();
      }

      protected override IAsyncEnumerator<T> CreateObservableAndRegisterHandlers( IAsyncEnumerator<T> enumerator, GenericEventHandler<EnumerationStartedEventArgs<TMetadata>> beforeStart, GenericEventHandler<EnumerationStartedEventArgs<TMetadata>> afterStart, GenericEventHandler<EnumerationItemEventArgs<T, TMetadata>> onItem )
      {
         var retVal = this.CreateObservable( enumerator );
         retVal.BeforeEnumerationStart += beforeStart;
         retVal.AfterEnumerationStart += afterStart;
         retVal.AfterEnumerationItemEncountered += onItem;
         GenericEventHandler<EnumerationEndedEventArgs> afterEnd = null;
         afterEnd = args =>
         {
            retVal.BeforeEnumerationStart -= beforeStart;
            retVal.AfterEnumerationStart -= afterStart;
            retVal.AfterEnumerationItemEncountered -= onItem;
            retVal.AfterEnumerationEnd -= afterEnd;
         };
         retVal.AfterEnumerationEnd += afterEnd;

         return retVal;
      }

      protected abstract IAsyncEnumeratorObservable<T, TMetadata> CreateObservable( IAsyncEnumerator<T> source );
   }

   internal sealed class AsyncConcurrentEnumeratorSourceObservable<T, TMetadata> : AbstractAsyncConcurrentEnumeratorSourceObservable<T, TMetadata>
   {
      public AsyncConcurrentEnumeratorSourceObservable(
         IAsyncConcurrentEnumeratorSource<T> source,
         TMetadata metadata
         ) : base( source, metadata )
      {
      }

      //protected override EnumerationStartedEventArgs<TMetadata> CreateBeforeEnumerationStartedArgs()
      //{
      //   
      //}

      protected override EnumerationEndedEventArgs<TMetadata> CreateBeforeEnumerationEndedArgs()
         => new EnumerationEndedEventArgsImpl<TMetadata>( this.Metadata );

      protected override IAsyncEnumeratorObservable<T, TMetadata> CreateObservable( IAsyncEnumerator<T> source )
          => source.AsObservable( this.Metadata );

   }

   internal sealed class AsyncConcurrentEnumeratorSourceObservableFromEnumerable<T, TMetadata> : AbstractAsyncConcurrentEnumeratorSourceObservable<T, TMetadata>
   {
      private readonly Func<EnumerationStartedEventArgs<TMetadata>> _started;
      private readonly Func<EnumerationEndedEventArgs<TMetadata>> _ended;

      public AsyncConcurrentEnumeratorSourceObservableFromEnumerable(
         IAsyncConcurrentEnumeratorSource<T> source,
         TMetadata metadata,
         Func<EnumerationStartedEventArgs<TMetadata>> started,
         Func<EnumerationEndedEventArgs<TMetadata>> ended
         ) : base( source, metadata )
      {
         this._started = ArgumentValidator.ValidateNotNull( nameof( started ), started );
         this._ended = ArgumentValidator.ValidateNotNull( nameof( ended ), ended );
      }

      protected override EnumerationEndedEventArgs<TMetadata> CreateBeforeEnumerationEndedArgs()
      {
         return this._ended();
      }

      protected override IAsyncEnumeratorObservable<T, TMetadata> CreateObservable( IAsyncEnumerator<T> source )
      {
         return new AsyncEnumeratorObservableFromEnumerable<T, TMetadata>(
            source,
            this.Metadata,
            this._started,
            this._ended
            );

      }
   }

   internal sealed class AsyncConcurrentEnumerableObservable<T> : AsyncEnumerableObservable<T, IAsyncConcurrentEnumerable<T>>, IAsyncConcurrentEnumerableObservable<T>
   {
      public AsyncConcurrentEnumerableObservable(
         IAsyncConcurrentEnumerable<T> source
         ) : base( source )
      {
      }

      public IAsyncConcurrentEnumeratorSourceObservable<T> GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments )
          => this.RegisterEvents( this._source.GetConcurrentEnumeratorSource( arguments ).AsObservable() );


      IAsyncConcurrentEnumeratorSource<T> IAsyncConcurrentEnumerable<T>.GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments )
         => this.GetConcurrentEnumeratorSource( arguments );
   }

   internal sealed class AsyncConcurrentEnumerableObservable<T, TMetadata> : AsyncEnumerableObservable<T, IAsyncConcurrentEnumerable<T>, TMetadata>, IAsyncConcurrentEnumerableObservable<T, TMetadata>
   {
      public AsyncConcurrentEnumerableObservable(
         IAsyncConcurrentEnumerable<T> source,
         TMetadata metadata
         ) : base( source, metadata )
      {
      }

      public IAsyncConcurrentEnumeratorSourceObservable<T, TMetadata> GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments )
         => this.RegisterEvents( new AsyncConcurrentEnumeratorSourceObservableFromEnumerable<T, TMetadata>(
            this._source.GetConcurrentEnumeratorSource( arguments ),
            this.Metadata,
            this._getStartedArgs,
            this._getEndedArgs
            ) );

      IAsyncConcurrentEnumeratorSourceObservable<T> IAsyncConcurrentEnumerableObservable<T>.GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments )
         => this.GetConcurrentEnumeratorSource( arguments );

      IAsyncConcurrentEnumeratorSource<T> IAsyncConcurrentEnumerable<T>.GetConcurrentEnumeratorSource( ConcurrentEnumerationArguments arguments )
         => this.GetConcurrentEnumeratorSource( arguments );
   }
}

public partial class E_UtilPack
{
   /// <summary>
   /// Adds observability aspect to this <see cref="IAsyncConcurrentEnumerable{T}"/>, if it is not already present.
   /// </summary>
   /// <typeparam name="T">The type of enumerable items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncConcurrentEnumerable{T}"/>.</param>
   /// <returns>A <see cref="IAsyncConcurrentEnumerableObservable{T}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncConcurrentEnumerable{T}"/> is <c>null</c>.</exception>
   public static IAsyncConcurrentEnumerableObservable<T> AsObservable<T>( this IAsyncConcurrentEnumerable<T> enumerable )
   {
      return ArgumentValidator.ValidateNotNullReference( enumerable ).TryAsConcurrentObservable();
   }

   private static IAsyncConcurrentEnumerableObservable<T> TryAsConcurrentObservable<T>( this IAsyncConcurrentEnumerable<T> enumerable )
   {
      return enumerable == null ?
         null :
         ( enumerable is IAsyncConcurrentEnumerableObservable<T> existing ? existing : new AsyncConcurrentEnumerableObservable<T>( enumerable ) );
   }

   /// <summary>
   /// Adds observability aspect to this <see cref="IAsyncConcurrentEnumeratorSource{T}"/>, if it is not already present.
   /// </summary>
   /// <typeparam name="T">The type of enumerable items.</typeparam>
   /// <param name="source">This <see cref="IAsyncConcurrentEnumeratorSource{T}"/>.</param>
   /// <returns>A <see cref="IAsyncConcurrentEnumeratorSourceObservable{T}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncConcurrentEnumeratorSource{T}"/> is <c>null</c>.</exception>
   public static IAsyncConcurrentEnumeratorSourceObservable<T> AsObservable<T>( this IAsyncConcurrentEnumeratorSource<T> source )
   {
      // Don't wrap too many times
      return ArgumentValidator.ValidateNotNullReference( source ) is IAsyncConcurrentEnumeratorSourceObservable<T> existing ?
         existing :
         new AsyncConcurrentEnumeratorSourceObservable<T>( source );
   }

   /// <summary>
   /// Adds observability with metadata -aspect to this <see cref="IAsyncConcurrentEnumerable{T}"/>, if it is not already present.
   /// </summary>
   /// <typeparam name="T">The type of enumerable items.</typeparam>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncConcurrentEnumerable{T}"/>.</param>
   /// <param name="metadata">The metadata to have.</param>
   /// <returns>A <see cref="IAsyncConcurrentEnumerableObservable{T, TMetadata}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncConcurrentEnumerable{T}"/> is <c>null</c>.</exception>
   public static IAsyncConcurrentEnumerableObservable<T, TMetadata> AsObservable<T, TMetadata>( this IAsyncConcurrentEnumerable<T> enumerable, TMetadata metadata )
   {
      return ArgumentValidator.ValidateNotNullReference( enumerable ).TryAsConcurrentObservable( metadata );
   }

   private static IAsyncConcurrentEnumerableObservable<T, TMetadata> TryAsConcurrentObservable<T, TMetadata>( this IAsyncConcurrentEnumerable<T> enumerable, TMetadata metadata )
   {
      return enumerable == null ?
         null :
         ( enumerable is IAsyncConcurrentEnumerableObservable<T, TMetadata> existing ? existing : new AsyncConcurrentEnumerableObservable<T, TMetadata>( enumerable, metadata ) );
   }

   /// <summary>
   /// Adds concurrent enumeration aspect to this <see cref="IAsyncEnumerableObservable{T, TMetadata}"/>, if it is not already present.
   /// </summary>
   /// <typeparam name="T">The type of enumerable items.</typeparam>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerableObservable{T, TMetadata}"/>.</param>
   /// <returns>A <see cref="IAsyncConcurrentEnumerableObservable{T, TMetadata}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerableObservable{T, TMetadata}"/> is <c>null</c>.</exception>
   public static IAsyncConcurrentEnumerableObservable<T, TMetadata> AsConcurrentEnumerable<T, TMetadata>( this IAsyncEnumerableObservable<T, TMetadata> enumerable )
   {
      return ArgumentValidator.ValidateNotNullReference( enumerable ) is IAsyncConcurrentEnumerableObservable<T, TMetadata> existing ?
         existing :
         new AsyncConcurrentEnumerableObservable<T, TMetadata>( enumerable.AsConcurrentEnumerable(), enumerable.Metadata );
   }

   /// <summary>
   /// Adds observability with metadata -aspect to this <see cref="IAsyncConcurrentEnumeratorSource{T}"/>, if it is not already present.
   /// </summary>
   /// <typeparam name="T">The type of enumerable items.</typeparam>
   /// <typeparam name="TMetadata">The type of metadata.</typeparam>
   /// <param name="source">This <see cref="IAsyncConcurrentEnumeratorSource{T}"/>.</param>
   /// <param name="metadata">The metadata to have.</param>
   /// <returns>A <see cref="IAsyncConcurrentEnumeratorSourceObservable{T, TMetadata}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncConcurrentEnumeratorSource{T}"/> is <c>null</c>.</exception>
   public static IAsyncConcurrentEnumeratorSourceObservable<T, TMetadata> AsObservable<T, TMetadata>( this IAsyncConcurrentEnumeratorSource<T> source, TMetadata metadata )
   {
      return ArgumentValidator.ValidateNotNullReference( source ) is IAsyncConcurrentEnumeratorSourceObservable<T, TMetadata> existing ?
         existing :
         new AsyncConcurrentEnumeratorSourceObservable<T, TMetadata>( source, metadata );
   }
}