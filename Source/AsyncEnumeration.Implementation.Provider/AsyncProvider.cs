using AsyncEnumeration.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using UtilPack;

namespace AsyncEnumeration.Implementation.Provider
{

   public sealed partial class DefaultAsyncProvider : IAsyncProvider
   {
      public static IAsyncProvider Instance { get; } = new DefaultAsyncProvider();

      private DefaultAsyncProvider()
      {

      }

      private static IAsyncEnumerable<U> FromTransformCallback<T, U>(
         IAsyncEnumerable<T> enumerable,
         Func<IAsyncEnumerator<T>, IAsyncEnumerator<U>> transform
         )
      {
         return new EnumerableWrapper<T, U>( enumerable, transform );
      }

      private static IAsyncEnumerable<U> FromTransformCallback<T, U, TArg>(
         IAsyncEnumerable<T> enumerable,
         TArg arg,
         Func<IAsyncEnumerator<T>, TArg, IAsyncEnumerator<U>> transform
         )
      {
         return new EnumerableWrapper<T, U, TArg>( enumerable, transform, arg );
      }


      /// <summary>
      /// This is helper utility class to provide callback-based <see cref="IAsyncEnumerator{T}"/> creation.
      /// </summary>
      /// <typeparam name="T">The type of items being enumerated.</typeparam>
      private sealed class EnumerableWrapper<T, U> : IAsyncEnumerable<U>
      {
         private readonly IAsyncEnumerable<T> _enumerable;
         private readonly Func<IAsyncEnumerator<T>, IAsyncEnumerator<U>> _getEnumerator;

         /// <summary>
         /// Creates a new instance of <see cref="EnumerableWrapper{T}"/> with given callback.
         /// </summary>
         /// <param name="getEnumerator">The callback to create <see cref="IAsyncEnumerator{T}"/>.</param>
         /// <exception cref="ArgumentNullException">If <paramref name="getEnumerator"/> is <c>null</c>.</exception>
         public EnumerableWrapper(
            IAsyncEnumerable<T> enumerable,
            Func<IAsyncEnumerator<T>, IAsyncEnumerator<U>> getEnumerator
            )
         {
            this._enumerable = ArgumentValidator.ValidateNotNull( nameof( enumerable ), enumerable );
            this._getEnumerator = ArgumentValidator.ValidateNotNull( nameof( getEnumerator ), getEnumerator );
         }

         IAsyncProvider IAsyncEnumerable.AsyncProvider => this._enumerable.AsyncProvider;

         IAsyncEnumerator<U> IAsyncEnumerable<U>.GetAsyncEnumerator() => this._getEnumerator( this._enumerable.GetAsyncEnumerator() );
      }

      private sealed class EnumerableWrapper<T, U, TArg> : IAsyncEnumerable<U>
      {
         private readonly IAsyncEnumerable<T> _enumerable;
         private readonly Func<IAsyncEnumerator<T>, TArg, IAsyncEnumerator<U>> _getEnumerator;
         private readonly TArg _arg;

         /// <summary>
         /// Creates a new instance of <see cref="EnumerableWrapper{T}"/> with given callback.
         /// </summary>
         /// <param name="getEnumerator">The callback to create <see cref="IAsyncEnumerator{T}"/>.</param>
         /// <exception cref="ArgumentNullException">If <paramref name="getEnumerator"/> is <c>null</c>.</exception>
         public EnumerableWrapper(
            IAsyncEnumerable<T> enumerable,
            Func<IAsyncEnumerator<T>, TArg, IAsyncEnumerator<U>> getEnumerator,
            TArg arg
            )
         {
            this._enumerable = ArgumentValidator.ValidateNotNull( nameof( enumerable ), enumerable );
            this._getEnumerator = ArgumentValidator.ValidateNotNull( nameof( getEnumerator ), getEnumerator );
            this._arg = arg;
         }

         IAsyncProvider IAsyncEnumerable.AsyncProvider => this._enumerable.AsyncProvider;

         IAsyncEnumerator<U> IAsyncEnumerable<U>.GetAsyncEnumerator() => this._getEnumerator( this._enumerable.GetAsyncEnumerator(), this._arg );
      }

   }
}