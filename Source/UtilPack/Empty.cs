/*
 * Copyright 2013 Stanislav Muhametsin. All rights Reserved.
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
   /// Helper class to provide easy access to cached empty array and <see cref="IEnumerable{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of array or enumerable elements.</typeparam>
   public static class Empty<T>
   {
      private static readonly T[] ARRAY = new T[0];
      private static readonly IEnumerable<T> ENUMERABLE = System.Linq.Enumerable.Empty<T>();

      /// <summary>
      /// Returns instance of array with zero elements.
      /// </summary>
      /// <value>Instance of array with zero elements.</value>
      public static T[] Array
      {
         get
         {
            return ARRAY;
         }
      }

      /// <summary>
      /// Returns instance of <see cref="IEnumerable{T}"/> with no elements.
      /// </summary>
      /// <value>Instance of <see cref="IEnumerable{T}"/> with no elements.</value>
      public static IEnumerable<T> Enumerable
      {
         get
         {
            return ENUMERABLE;
         }
      }
   }
}
