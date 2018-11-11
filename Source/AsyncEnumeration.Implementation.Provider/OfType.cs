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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Implementation.Provider
{

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
         return AsyncProviderUtilities.IsOfType(
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
            FromTransformCallback( enumerable, e => new OfTypeEnumerator<T, U>( e ) );
      }
   }


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