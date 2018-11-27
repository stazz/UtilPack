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
   /// Helper class wrapping a generic <see cref="IEqualityComparer{T}"/>, and implementing both generic <see cref="IEqualityComparer{T}"/> and non-generic <see cref="System.Collections.IEqualityComparer"/>.
   /// </summary>
   /// <typeparam name="TValue">The type of the elements to compare.</typeparam>
   public class EqualityComparerWrapper<TValue> : System.Collections.IEqualityComparer, IEqualityComparer<TValue>
   {
      private readonly IEqualityComparer<TValue> _comparer;

      /// <summary>
      /// Creates a new instance of <see cref="EqualityComparerWrapper{T}"/> with given comparer to delegate comparison to.
      /// </summary>
      /// <param name="comparer">The actual equality comparer.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="comparer"/> is <c>null</c>.</exception>
      public EqualityComparerWrapper( IEqualityComparer<TValue> comparer )
      {
         ArgumentValidator.ValidateNotNull( "Equality comparer", comparer );

         this._comparer = comparer;
      }

      Boolean System.Collections.IEqualityComparer.Equals( Object x, Object y )
      {
         return this.Equals( (TValue) x, (TValue) y );
      }

      Int32 System.Collections.IEqualityComparer.GetHashCode( Object obj )
      {
         return this.GetHashCode( (TValue) obj );
      }

      /// <summary>
      /// Delegates this call to the comparer given to <see cref="EqualityComparerWrapper{T}(IEqualityComparer{T})"/>.
      /// </summary>
      /// <param name="x">The first value to compare.</param>
      /// <param name="y">The second value to compare.</param>
      /// <returns>The result of comparer given to <see cref="EqualityComparerWrapper{T}(IEqualityComparer{T})"/>.</returns>
      /// <remarks>This method is virtual and is called by explicitly implemented method of <see cref="System.Collections.IEqualityComparer.Equals(Object, Object)"/>.</remarks>
      public virtual Boolean Equals( TValue x, TValue y )
      {
         return this._comparer.Equals( x, y );
      }

      /// <summary>
      /// Delegates this call to the comparer given to <see cref="EqualityComparerWrapper{T}(IEqualityComparer{T})"/>
      /// </summary>
      /// <param name="obj">The object to get hash code for.</param>
      /// <returns>The value of comparer given to <see cref="EqualityComparerWrapper{T}(IEqualityComparer{T})"/>.</returns>
      /// <remarks>This method is virtual and is called by explicitly implemented method of <see cref="System.Collections.IEqualityComparer.GetHashCode(Object)"/>.</remarks>
      public virtual Int32 GetHashCode( TValue obj )
      {
         return this._comparer.GetHashCode( obj );
      }
   }

   /// <summary>
   /// Helper class wrapping a generic <see cref="IComparer{T}"/>, and implementing both generic <see cref="IComparer{T}"/> and non-generic <see cref="System.Collections.IComparer"/>.
   /// </summary>
   /// <typeparam name="TValue">The type of the elements to compare.</typeparam>
   public class ComparerWrapper<TValue> : System.Collections.IComparer, IComparer<TValue>
   {
      private readonly IComparer<TValue> _comparer;

      /// <summary>
      /// Creates a new instance of <see cref="ComparerWrapper{T}"/> with given comparer to delegate comparison to.
      /// </summary>
      /// <param name="comparer">The actual comparer.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="comparer"/> is <c>null</c>.</exception>
      public ComparerWrapper( IComparer<TValue> comparer )
      {
         ArgumentValidator.ValidateNotNull( "Comparer", comparer );

         this._comparer = comparer;
      }

      Int32 System.Collections.IComparer.Compare( Object x, Object y )
      {
         return this.Compare( (TValue) x, (TValue) y );
      }

      /// <summary>
      /// Delegates this call to the comparer given to <see cref="ComparerWrapper{T}(IComparer{T})"/>
      /// </summary>
      /// <param name="x">The first value to compare.</param>
      /// <param name="y">The second value to compare.</param>
      /// <returns>The result of comparer given to <see cref="ComparerWrapper{T}(IComparer{T})"/>.</returns>
      /// <remarks>This method is virtual and is called by explicitly implemented method of <see cref="System.Collections.IComparer.Compare(Object, Object)"/>.</remarks>
      public virtual Int32 Compare( TValue x, TValue y )
      {
         return this._comparer.Compare( x, y );
      }
   }
}
