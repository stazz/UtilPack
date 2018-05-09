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
using System.Threading.Tasks;

public static partial class E_UtilPack
{
   /// <summary>
   /// Asynchronously fetches the first item in this <see cref="IAsyncEnumerable{T}"/> and discards any other items.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>The first item returned by <see cref="IAsyncEnumerator{T}"/> of this <see cref="IAsyncEnumerable{T}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">If this <see cref="IAsyncEnumerable{T}"/> has no elements.</exception>
   /// <seealso cref="System.Linq.Enumerable.First{TSource}(IEnumerable{TSource})"/>
   public static ValueTask<T> FirstAsync<T>( this IAsyncEnumerable<T> enumerable )
       => enumerable.GetAsyncEnumerator().FirstAsync( true );

   /// <summary>
   /// Asynchronously fetches the first item in this <see cref="IAsyncEnumerator{T}"/> and discards any other items.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerator">This <see cref="IAsyncEnumerator{T}"/>.</param>
   /// <returns>The first item returned by this <see cref="IAsyncEnumerable{T}"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">If this <see cref="IAsyncEnumerator{T}"/> has no elements.</exception>
   /// <seealso cref="System.Linq.Enumerable.First{TSource}(IEnumerable{TSource})"/>
   public static ValueTask<T> FirstAsync<T>( this IAsyncEnumerator<T> enumerator )
      => enumerator.FirstAsync( true );

   /// <summary>
   /// Asynchronously fetches the first item in this <see cref="IAsyncEnumerable{T}"/> and discards any other items.
   /// If there are no items, the the default is returned for type <typeparamref name="T"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>The first item returned by <see cref="IAsyncEnumerator{T}"/> of this <see cref="IAsyncEnumerable{T}"/>, or default for type <typeparamref name="T"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>
   public static ValueTask<T> FirstOrDefaultAsync<T>( this IAsyncEnumerable<T> enumerable )
      => enumerable.GetAsyncEnumerator().FirstAsync( false );

   /// <summary>
   /// Asynchronously fetches the first item in this <see cref="IAsyncEnumerator{T}"/> and discards any other items.
   /// If there are no items, the the default is returned for type <typeparamref name="T"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerator">This <see cref="IAsyncEnumerator{T}"/>.</param>
   /// <returns>The first item returned by this <see cref="IAsyncEnumerator{T}"/>, or default for type <typeparamref name="T"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerator{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>
   public static ValueTask<T> FirstOrDefaultAsync<T>( this IAsyncEnumerator<T> enumerator )
      => enumerator.FirstAsync( false );

   private static async ValueTask<T> FirstAsync<T>( this IAsyncEnumerator<T> enumerator, Boolean throwIfNone )
   {
      T retVal;
      try
      {
         var success = await enumerator.WaitForNextAsync();
         retVal = success ? enumerator.TryGetNext( out success ) : default;
         if ( !success )
         {
            if ( throwIfNone )
            {
               throw new InvalidOperationException( "Sequence contains no elements." );
            }
            else
            {
               retVal = default;
            }
         }
      }
      finally
      {
         await enumerator.DisposeAsync();
      }

      return retVal;

   }
}