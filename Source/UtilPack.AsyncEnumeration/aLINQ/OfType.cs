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
using UtilPack.AsyncEnumeration;
using UtilPack.AsyncEnumeration.LINQ;
using System.Reflection;

using TTypeInfo =
#if NET40
   System.Type
#else
   System.Reflection.TypeInfo
#endif
   ;

namespace UtilPack.AsyncEnumeration
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

   public partial class DefaultAsyncProvider
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
      public IAsyncEnumerable<U> OfType<T, U>( IAsyncEnumerable<T> enumerable )
      {
         ArgumentValidator.ValidateNotNullReference( enumerable );
         return IsOfType(
            typeof( T )
#if !NET40
         .GetTypeInfo()
#endif
         , typeof( U )
#if !NET40
         .GetTypeInfo()
#endif
         ) ?
            (IAsyncEnumerable<U>) enumerable :
            AsyncEnumerationFactory.FromTransformCallback( enumerable, e => new OfTypeEnumerator<T, U>( e ) );
      }

      public static Boolean IsOfType(
         TTypeInfo t,
         TTypeInfo u
         )
      {
         // When both types are non-structs and non-generic-parameters, and u is supertype of t, then we don't need new enumerable/enumerator
         return Equals( t, u ) || ( !t.IsValueType && !u.IsValueType && !t.IsGenericParameter && !u.IsGenericParameter && u.IsAssignableFrom( t ) );
      }
   }


   namespace LINQ
   {
      internal sealed class OfTypeEnumerator<T, U> : IAsyncEnumerator<U>
      {
         private readonly IAsyncEnumerator<T> _source;

         public OfTypeEnumerator(
            IAsyncEnumerator<T> source
            )
         {
            this._source = ArgumentValidator.ValidateNotNull( nameof( source ), source );
         }

         public Task<Boolean> WaitForNextAsync()
            => this._source.WaitForNextAsync();

         public U TryGetNext( out Boolean success )
         {
            var encountered = false;
            T item;
            U returnedItem = default;
            do
            {
               item = this._source.TryGetNext( out success );
               if ( success && item is U tmp )
               {
                  encountered = true;
                  returnedItem = tmp;
               }
            } while ( success && !encountered );

            success = encountered;
            return returnedItem;
         }

         public Task DisposeAsync()
            => this._source.DisposeAsync();
      }
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
         return ( ( this._source ?? throw new InvalidOperationException( "This operation not possible on default-constructed type." ) ).AsyncProvider ?? DefaultAsyncProvider.Instance ).OfType<T, U>( this._source );
      }
   }
}

public static partial class E_UtilPack
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
      => new OfTypeInvoker<T>( enumerable );

}