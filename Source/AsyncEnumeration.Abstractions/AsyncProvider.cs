/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
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
using System.Threading.Tasks;
using TTypeInfo =
#if NET40
   System.Type
#else
   System.Reflection.TypeInfo
#endif
   ;

namespace AsyncEnumeration.Abstractions
{
   public interface IAsyncEnumerable
   {
      IAsyncProvider AsyncProvider { get; }

   }

   public partial interface IAsyncProvider
   {

   }

   public static class AsyncProviderUtilities
   {
      public static InvalidOperationException EmptySequenceException() => new InvalidOperationException( "Empty sequence" );

      public static InvalidOperationException NoAsyncProviderException() => new InvalidOperationException( "No async provider" );

      public static Boolean IsOfType(
         TTypeInfo t,
         TTypeInfo u
         )
      {
         // When both types are non-structs and non-generic-parameters, and u is supertype of t, then we don't need new enumerable/enumerator
         return Equals( t, u ) || ( !t.IsValueType && !u.IsValueType && !t.IsGenericParameter && !u.IsGenericParameter && u.IsAssignableFrom( t ) );
      }


      public static async ValueTask<Int64> EnumerateAsync<T>(
         IAsyncEnumerable<T> enumerable,
         Action<T> action
         )
      {
         var enumerator = enumerable.GetAsyncEnumerator();
         try
         {
            var retVal = 0L;
            while ( await enumerator.WaitForNextAsync() )
            {
               Boolean success;
               do
               {
                  var item = enumerator.TryGetNext( out success );
                  if ( success )
                  {
                     ++retVal;
                     action?.Invoke( item );
                  }
               } while ( success );
            }
            return retVal;
         }
         finally
         {
            await enumerator.DisposeAsync();
         }
      }

      public static async ValueTask<Int64> EnumerateAsync<T>(
         IAsyncEnumerable<T> enumerable,
         Func<T, Task> asyncAction
         )
      {
         var enumerator = enumerable.GetAsyncEnumerator();
         try
         {
            var retVal = 0L;
            while ( await enumerator.WaitForNextAsync() )
            {
               Boolean success;
               do
               {
                  var item = enumerator.TryGetNext( out success );
                  if ( success )
                  {
                     ++retVal;
                     var task = asyncAction?.Invoke( item );
                     if ( task != null )
                     {
                        await task;
                     }
                  }
               } while ( success );
            }
            return retVal;
         }
         finally
         {
            await enumerator.DisposeAsync();
         }
      }
   }

}