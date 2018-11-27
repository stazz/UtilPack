/*
 * Copyright 2012 Stanislav Muhametsin. All rights Reserved.
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
using System.Runtime.CompilerServices;

namespace UtilPack
{
   /// <summary>
   /// This class will perform reference equality matching on its target type.
   /// </summary>
   /// <typeparam name="T">The type of objects being compared for equality.</typeparam>
   public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>, System.Collections.IEqualityComparer
   {
      private static readonly IEqualityComparer<T> INSTANCE = new ReferenceEqualityComparer<T>();

      /// <summary>
      /// Returns the reference-based equality comparer for <typeparamref name="T"/>.
      /// </summary>
      /// <value>The reference-based equality comparer for <typeparamref name="T"/>.</value>
      /// <remarks>The return value can be casted to <see cref="System.Collections.IEqualityComparer"/>.</remarks>
      public static IEqualityComparer<T> ReferenceBasedComparer
      {
         get
         {
            return INSTANCE;
         }
      }

      #region IEqualityComparer<T> Members

      Boolean IEqualityComparer<T>.Equals( T x, T y )
      {
         return Object.ReferenceEquals( x, y );
      }

      Int32 IEqualityComparer<T>.GetHashCode( T obj )
      {
         return RuntimeHelpers.GetHashCode( obj );
      }

      #endregion

      Boolean System.Collections.IEqualityComparer.Equals( Object x, Object y )
      {
         return Object.ReferenceEquals( x, y );
      }

      Int32 System.Collections.IEqualityComparer.GetHashCode( Object obj )
      {
         return RuntimeHelpers.GetHashCode( obj );
      }
   }
}
