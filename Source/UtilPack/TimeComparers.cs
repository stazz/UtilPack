/*
 * Copyright 2014 Stanislav Muhametsin. All rights Reserved.
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

namespace UtilPack
{
   /// <summary>
   /// This class provides a way to compare <see cref="DateTime"/>s with truncated precision.
   /// </summary>
   public sealed class DateTimeComparer : IEqualityComparer<DateTime>, System.Collections.IEqualityComparer
   {
      private static IEqualityComparer<DateTime> DEFAULT = null;

      /// <summary>
      /// Gets the default comparer for <see cref="DateTime"/>, which preforms exact equality check.
      /// </summary>
      public static IEqualityComparer<DateTime> Default
      {
         get
         {
            var retVal = DEFAULT;
            if ( retVal == null )
            {
               retVal = new DateTimeComparer( TimeSpan.Zero );
               DEFAULT = retVal;
            }
            return retVal;
         }
      }

      /// <summary>
      /// Creates a new equality comparer which will truncate both <see cref="DateTime"/>s to certain precision before performing equality check.
      /// </summary>
      /// <param name="truncatePrecision">The truncate precision.</param>
      /// <returns>A new equality comparer which will truncate both <see cref="DateTime"/>s to certain precision before performing equality check.</returns>
      /// <remarks>The return value can be casted to <see cref="System.Collections.IEqualityComparer"/>.</remarks>
      public static IEqualityComparer<DateTime> CreateComparerForTruncatedValues( TimeSpan truncatePrecision )
      {
         return TimeSpan.Zero == truncatePrecision ? Default : new DateTimeComparer( truncatePrecision );
      }

      private readonly TimeSpan _truncPrecision;

      private DateTimeComparer( TimeSpan truncPrecision )
      {
         this._truncPrecision = truncPrecision;
      }

      Boolean IEqualityComparer<DateTime>.Equals( DateTime x, DateTime y )
      {
         return x.Truncate( this._truncPrecision ).Equals( y.Truncate( this._truncPrecision ) );
      }

      Int32 IEqualityComparer<DateTime>.GetHashCode( DateTime obj )
      {
         return obj.Truncate( this._truncPrecision ).GetHashCode();
      }

      Boolean System.Collections.IEqualityComparer.Equals( Object x, Object y )
      {
         return ( x == null && y == null ) || ( x is DateTime && y is DateTime && ( (IEqualityComparer<DateTime>) this ).Equals( (DateTime) x, (DateTime) y ) );
      }

      Int32 System.Collections.IEqualityComparer.GetHashCode( Object obj )
      {
         return obj == null ? 0 : ( (IEqualityComparer<DateTime>) this ).GetHashCode( (DateTime) obj );
      }
   }
}
