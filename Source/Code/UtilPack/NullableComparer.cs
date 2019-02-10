/*
 * Copyright 2015 Stanislav Muhametsin. All rights Reserved.
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
   /// This class provides equality comparer functionality for nullable types.
   /// </summary>
   /// <typeparam name="T">The nullable type.</typeparam>
   /// <remarks>
   /// The <see cref="Object.Equals(Object)"/> implementation for nullable types works fine, however there is small overhead as value is boxed, and the <see cref="Object.Equals(Object)"/> method is invoked, resulting in more checks and unboxing.
   /// </remarks>
   public sealed class NullableEqualityComparer<T> : IEqualityComparer<T?>
      where T : struct
   {
      /// <summary>
      /// Returns the equality comparer for nullable type <typeparamref name="T"/> which uses the default equality comparer when comparing actual values.
      /// </summary>
      /// <value>The equality comparer for nullable type <typeparamref name="T"/> which uses the default equality comparer when comparing actual values.</value>
      public static IEqualityComparer<T?> DefaultComparer { get; } = new NullableEqualityComparer<T>( null, 0 );
      private readonly IEqualityComparer<T> _itemComparer;
      private readonly Int32 _hashCodeForNoValue;

      private NullableEqualityComparer( IEqualityComparer<T> itemComparer, Int32 hashCodeForNoValue )
      {
         this._itemComparer = itemComparer ?? EqualityComparer<T>.Default;
         this._hashCodeForNoValue = hashCodeForNoValue;
      }

      Boolean IEqualityComparer<T?>.Equals( T? x, T? y )
      {
         return EqualsImpl( x, y, this._itemComparer );
      }

      Int32 IEqualityComparer<T?>.GetHashCode( T? obj )
      {
         return GetHashCodeImpl( obj, this._itemComparer, this._hashCodeForNoValue );
      }

      private static Boolean EqualsImpl( T? x, T? y, IEqualityComparer<T> comparer )
      {
         return x.HasValue == y.HasValue
            && ( !x.HasValue || comparer.Equals( x.Value, y.Value ) );
      }

      private static Int32 GetHashCodeImpl( T? obj, IEqualityComparer<T> comparer, Int32 hashCodeForNoValue )
      {
         return obj.HasValue ? comparer.GetHashCode( obj.Value ) : hashCodeForNoValue;
      }

      /// <summary>
      /// Tests equality for given nullables without creating an instance of this class.
      /// </summary>
      /// <param name="x">The first nullable.</param>
      /// <param name="y">The second nullable.</param>
      /// <param name="comparer">The optional comparer for non-null values.</param>
      /// <returns><c>true</c> if both are null, or if both are non-null and their values equal; <c>false</c> otherwise.</returns>
      public static Boolean Equals( T? x, T? y, IEqualityComparer<T> comparer = null )
      {
         return EqualsImpl( x, y, comparer ?? EqualityComparer<T>.Default );
      }

      /// <summary>
      /// Calculates hash code for given nullable without creating an instance of this class.
      /// </summary>
      /// <param name="obj">The nullable.</param>
      /// <param name="comparer">The optional comparer for non-null values.</param>
      /// <param name="hashCodeForNoValue">The hash code for null value.</param>
      /// <returns>The hash code returned by <paramref name="comparer"/> if <paramref name="obj"/> is not <c>null</c>, otherwise <paramref name="hashCodeForNoValue"/>.</returns>
      public static Int32 GetHashCode( T? obj, IEqualityComparer<T> comparer = null, Int32 hashCodeForNoValue = 0 )
      {
         return GetHashCodeImpl( obj, comparer ?? EqualityComparer<T>.Default, hashCodeForNoValue );
      }

      /// <summary>
      /// Returns a new equality comparer for nullable type <typeparamref name="T"/> which uses given equality comparer when comparing actual values, and returns given, optional, hash code for nullables with no value.
      /// </summary>
      /// <param name="itemComparer">The equality comparer to use when comparing non-null values.</param>
      /// <param name="noValueHashCode">The hash code to return for null values.</param>
      /// <returns>A new equality comparer for nullable type <typeparamref name="T"/>.</returns>
      public static IEqualityComparer<T?> NewComparer( IEqualityComparer<T> itemComparer, Int32 noValueHashCode = 0 )
      {
         return new NullableEqualityComparer<T>( itemComparer, noValueHashCode );
      }
   }

   /// <summary>
   /// This class provides comparer functionality for nullable types.
   /// </summary>
   /// <typeparam name="T">The nullable type</typeparam>
   public sealed class NullableComparer<T> : IComparer<T?>
      where T : struct, IComparable<T>
   {

      private static IComparer<T?> INSTANCE = null;

      /// <summary>
      /// Returns the comparer for nullable type <typeparamref name="T"/> which uses the default comparer when comparing actual values.
      /// </summary>
      /// <value>The comparer for nullable type <typeparamref name="T"/> which uses the equality comparer when comparing actual values.</value>
      /// <remarks>
      /// If nullable with no value is passed to this comparer, it sorts nulls first.
      /// </remarks>
      public static IComparer<T?> DefaultComparer
      {
         get
         {
            var retVal = INSTANCE;
            if ( retVal == null )
            {
               retVal = new NullableComparer<T>( null, true );
               INSTANCE = retVal;
            }

            return retVal;
         }
      }

      /// <summary>
      /// Returns a new comparer for nullable type <typeparamref name="T"/> which uses given comparer when comparing actual values, and sorts nulls first or last.
      /// </summary>
      /// <param name="itemComparer">The optional comparer to use when comparing actual values. Default will be used if no comparer is supplied.</param>
      /// <param name="nullsFirst">Whether to sort null values first.</param>
      /// <returns>A new comparer for nullable type <typeparamref name="T"/>.</returns>
      /// <remarks>
      /// In order for null-sorting to work, make sure your sorting algorithm passes also null values to the comparer returned by this method.
      /// </remarks>
      public static IComparer<T?> NewComparer( IComparer<T> itemComparer = null, Boolean nullsFirst = true )
      {
         return new NullableComparer<T>( itemComparer, nullsFirst );
      }

      /// <summary>
      /// Compares two values without creating an instance of this class.
      /// </summary>
      /// <param name="x">The first nullable value.</param>
      /// <param name="y">The second nullable value.</param>
      /// <param name="itemComparer">The optional comparer to use when comparing actual values. Default will be used if no comparer is supplied.</param>
      /// <param name="nullsFirst">Whether to sort null values first.</param>
      /// <returns>negative value if <paramref name="x"/> is considered to be less than <paramref name="y"/>, positive value if <paramref name="x"/> is considered to be greater than <paramref name="y"/>, or <c>0</c> if <paramref name="x"/> and <paramref name="y"/> are considered to be equal.</returns>
      public static Int32 Compare( T? x, T? y, IComparer<T> itemComparer = null, Boolean nullsFirst = true )
      {
         return CompareImpl( x, y, itemComparer ?? Comparer<T>.Default, nullsFirst );
      }

      private static Int32 CompareImpl( T? x, T? y, IComparer<T> itemComparer, Boolean nullsFirst )
      {
         Int32 retVal;
         if ( x.HasValue == y.HasValue )
         {
            retVal = x.HasValue ? itemComparer.Compare( x.Value, y.Value ) : 0;
         }
         else if ( x.HasValue ) // && !y.HasValue )
         {
            retVal = nullsFirst ? 1 : -1;
         }
         else // !x.HasValue && y.HasValue
         {
            retVal = nullsFirst ? -1 : 1;
         }

         return retVal;
      }

      private readonly Boolean _nullsFirst;
      private readonly IComparer<T> _itemComparer;

      private NullableComparer( IComparer<T> itemComparer, Boolean nullsFirst )
      {
         this._itemComparer = itemComparer ?? Comparer<T>.Default;
         this._nullsFirst = nullsFirst;
      }

      Int32 IComparer<T?>.Compare( T? x, T? y )
      {
         return CompareImpl( x, y, this._itemComparer, this._nullsFirst );
      }
   }
}
