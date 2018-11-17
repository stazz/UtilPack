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
using UtilPack;


namespace AsyncEnumeration.Abstractions
{
   public partial interface IAsyncProvider
   {
      /// <summary>
      /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return only those items which are of given type.
      /// </summary>
      /// <typeparam name="T">The type of source enumerable items.</typeparam>
      /// <typeparam name="U">The type of target items.</typeparam>
      /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
      /// <returns><see cref="IAsyncEnumerable{T}"/> which will return only those items which are of given type.</returns>
      /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
      /// <seealso cref="System.Linq.Enumerable.OfType{TResult}(System.Collections.IEnumerable)"/>
      IAsyncEnumerable<U> OfType<T, U>( IAsyncEnumerable<T> enumerable );
   }


   public struct OfTypeInvoker<T>
   {
      private readonly IAsyncEnumerable<T> _source;

      public OfTypeInvoker( IAsyncEnumerable<T> source )
      {
         this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
      }

      public IAsyncEnumerable<U> Type<U>()
      {
         return (
            ( this._source ?? throw new InvalidOperationException( "This operation not possible on default-constructed type." ) )
            .AsyncProvider ?? throw AsyncProviderUtilities.NoAsyncProviderException()
            ).OfType<T, U>( this._source );
      }
   }
}

public static partial class E_AsyncEnumeration
{


   // Invoking this is a bit awkward, so OfTypeInfo<T> accepting variants are provided. 
   // Need to wait for https://github.com/dotnet/csharplang/blob/master/proposals/default-interface-methods.md to do this properly.
   // Synchronous version uses IEnumerable interface without generic parameters, but that causes extra API to maintain, and one heap allocation for every struct item encountered (struct -> object boxing).

   /// <summary>
   /// This extension method will return <see cref="IAsyncEnumerable{T}"/> which will return only those items which are of given type.
   /// </summary>
   /// <typeparam name="T">The type of source enumerable items.</typeparam>
   /// <typeparam name="U">The type of target items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns><see cref="IAsyncEnumerable{T}"/> which will return only those items which are of given type.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <seealso cref="System.Linq.Enumerable.OfType{TResult}(System.Collections.IEnumerable)"/>
   public static OfTypeInvoker<T> Of<T>( this IAsyncEnumerable<T> enumerable )
      => new OfTypeInvoker<T>( ArgumentValidator.ValidateNotNullReference( enumerable ) );

}