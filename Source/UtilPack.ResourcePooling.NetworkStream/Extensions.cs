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
using System.Net;
using System.Threading.Tasks;
using UtilPack;

namespace UtilPack.ResourcePooling.NetworkStream
{
   /// <summary>
   /// This class contains extensions methods for types not defined in this assembly.
   /// </summary>
   public static class UtilPackExtensions
   {
      /// <summary>
      /// This is helper method to create a <see cref="ReadOnlyResettableAsyncLazy{T}"/> which will resolve host name or textual IP address into <see cref="IPAddress"/>.
      /// The lazy can be resetted using <see cref="ReadOnlyResettableAsyncLazy{T}.Reset"/> method, if it is needed to resolve again (e.g. dns cache modification).
      /// </summary>
      /// <param name="addressOrHostName">The host name or textual IP address.</param>
      /// <param name="addressSelector">The callback to select one address from potentially many addresses. Only used if this <paramref name="addressOrHostName"/> is host name.</param>
      /// <param name="dnsResolve">The optional callback to perform DNS resolve. If <c>null</c>, then <paramref name="addressSelector"/> will get <c>null</c> as its argument.</param>
      /// <returns>A new <see cref="ReadOnlyResettableAsyncLazy{T}"/> which will asynchronously </returns>
      public static ReadOnlyResettableAsyncLazy<IPAddress> CreateAddressOrHostNameResolvingLazy(
         this String addressOrHostName,
         Func<IPAddress[], IPAddress> addressSelector,
         Func<String, ValueTask<IPAddress[]>> dnsResolve = null
         )
      {
#if !NETSTANDARD1_3
         if ( dnsResolve == null )
         {
            dnsResolve = async host => await
#if NET40
                  DnsEx
#else
                  Dns
#endif
                  .GetHostAddressesAsync( host );
         }
#endif
         ReadOnlyResettableAsyncLazy<IPAddress> retVal;

         if ( IPAddress.TryParse( addressOrHostName, out var thisAddress ) )
         {
            retVal = new ReadOnlyResettableAsyncLazy<IPAddress>( () => thisAddress );
         }
         else
         {
            retVal = new ReadOnlyResettableAsyncLazy<IPAddress>( async () =>
            {
               var allIPs = await ( dnsResolve?.Invoke( addressOrHostName ) ?? new ValueTask<IPAddress[]>( (IPAddress[]) null ) );
               IPAddress resolvedAddress = null;
               if ( ( allIPs?.Length ?? 0 ) > 1 )
               {
                  resolvedAddress = addressSelector?.Invoke( allIPs );
               }

               if ( resolvedAddress == null )
               {
                  resolvedAddress = allIPs.GetElementOrDefault( 0 );
               }

               return resolvedAddress;

            } );

         }

         return retVal;


      }
   }

}
