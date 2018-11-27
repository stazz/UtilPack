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
using UtilPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UtilPack
{
   /// <summary>
   /// Provides an easy way to create (equality) comparer based on lambdas.
   /// </summary>
   public static class ComparerFromFunctions
   {
      private sealed class EqualityComparerWithFunction<T> : IEqualityComparer<T>, System.Collections.IEqualityComparer
      {
         private readonly Equality<T> _equalsFunc;
         private readonly HashCode<T> _hashCodeFunc;

         internal EqualityComparerWithFunction( Equality<T> equalsFunc, HashCode<T> hashCodeFunc )
         {
            ArgumentValidator.ValidateNotNull( "Equality function", equalsFunc );
            ArgumentValidator.ValidateNotNull( "Hash code function", hashCodeFunc );

            this._equalsFunc = equalsFunc;
            this._hashCodeFunc = hashCodeFunc;
         }

         #region IEqualityComparer<T> Members

         Boolean IEqualityComparer<T>.Equals( T x, T y )
         {
            return this._equalsFunc( x, y );
         }

         Int32 IEqualityComparer<T>.GetHashCode( T obj )
         {
            return this._hashCodeFunc( obj );
         }

         #endregion

         Boolean System.Collections.IEqualityComparer.Equals( Object x, Object y )
         {
            return this._equalsFunc( (T) x, (T) y );
         }

         Int32 System.Collections.IEqualityComparer.GetHashCode( Object obj )
         {
            return this._hashCodeFunc( (T) obj );
         }
      }

      private sealed class ComparerWithFunction<T> : IComparer<T>, System.Collections.IComparer
      {
         private readonly Comparison<T> _compareFunc;

         internal ComparerWithFunction( Comparison<T> compareFunc )
         {
            ArgumentValidator.ValidateNotNull( "Comparer function", compareFunc );
            this._compareFunc = compareFunc;
         }

         #region IComparer<T> Members

         Int32 IComparer<T>.Compare( T x, T y )
         {
            return this._compareFunc( x, y );
         }

         #endregion

         Int32 System.Collections.IComparer.Compare( Object x, Object y )
         {
            return this._compareFunc( (T) x, (T) y );
         }
      }

      private sealed class ComparerWithFunctionAndNullStrategy<T> : IComparer<T>, System.Collections.IComparer
      {
         private readonly Comparison<T> _compareFunc;
         private readonly NullSorting _nullSorting;

         internal ComparerWithFunctionAndNullStrategy( Comparison<T> compareFunc, NullSorting nullSorting )
         {
            ArgumentValidator.ValidateNotNull( "Comparer function", compareFunc );
            this._compareFunc = compareFunc;
            this._nullSorting = nullSorting;
         }

         #region IComparer<T> Members

         Int32 IComparer<T>.Compare( T x, T y )
         {
            Int32 retVal;
            if ( this._nullSorting.CheckForNullValues( x, y, out retVal ) )
            {
               retVal = this._compareFunc( x, y );
            }
            return retVal;
         }

         #endregion

         Int32 System.Collections.IComparer.Compare( Object x, Object y )
         {
            Int32 retVal;
            if ( this._nullSorting.CheckForNullValues( x, y, out retVal ) )
            {
               retVal = this._compareFunc( (T) x, (T) y );
            }
            return retVal;
         }
      }

      /// <summary>
      /// Creates a new <see cref="IEqualityComparer{T}"/> which behaves as <paramref name="equals"/> and <paramref name="hashCode"/> callbakcs specify.
      /// </summary>
      /// <typeparam name="T">The type of objects being compared for equality.</typeparam>
      /// <param name="equals">The function for comparing equality for <typeparamref name="T"/>.</param>
      /// <param name="hashCode">The function for calculating hash code for <typeparamref name="T"/>.</param>
      /// <returns>A new <see cref="IEqualityComparer{T}"/> which behaves as parameters specify.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="equals"/> or <paramref name="hashCode"/> is <c>null</c>.</exception>
      /// <remarks>The return value can be casted to <see cref="System.Collections.IEqualityComparer"/>.</remarks>
      public static IEqualityComparer<T> NewEqualityComparer<T>( Equality<T> equals, HashCode<T> hashCode )
      {
         return new EqualityComparerWithFunction<T>( equals, hashCode );
      }

      /// <summary>
      /// Creates a new <see cref="IComparer{T}"/> which behaves as <paramref name="comparison"/> callback specify.
      /// </summary>
      /// <typeparam name="T">The type of object being compared.</typeparam>
      /// <param name="comparison">The function comparing the object, should return same as <see cref="IComparer{T}.Compare(T,T)"/> method.</param>
      /// <returns>A new <see cref="IComparer{T}"/> which behaves as parameters specify.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="comparison"/> is <c>null</c>.</exception>
      /// <remarks>The return value can be casted to <see cref="System.Collections.IComparer"/>.</remarks>
      public static IComparer<T> NewComparer<T>( Comparison<T> comparison )
      {
         return new ComparerWithFunction<T>( comparison );
      }

      /// <summary>
      /// Creates a new <see cref="IComparer{T}"/> which behaves as <paramref name="comparison"/> callback specify, and which sorts <c>null</c> values according to given strategy.
      /// This means that the given callback will never receive <c>null</c> values.
      /// </summary>
      /// <typeparam name="T">The type of object being compared.</typeparam>
      /// <param name="comparison">The function comparing the object, should return same as <see cref="IComparer{T}.Compare(T,T)"/> method.</param>
      /// <param name="nullSortingStrategy">The strategy to sort <c>null</c> values.</param>
      /// <returns>A new <see cref="IComparer{T}"/> which behaves as parameters specify.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="comparison"/> is <c>null</c>.</exception>
      /// <remarks>The return value can be casted to <see cref="System.Collections.IComparer"/>.</remarks>
      public static IComparer<T> NewComparerWithNullStrategy<T>( Comparison<T> comparison, NullSorting nullSortingStrategy )
      {
         return new ComparerWithFunctionAndNullStrategy<T>( comparison, nullSortingStrategy );
      }

      /// <summary>
      /// If <typeparamref name="T"/> is an array, then returns a <see cref="ArrayEqualityComparer{T}.DefaultArrayEqualityComparer"/> for array element type.
      /// If <typeparamref name="T"/> is or implements <see cref="IList{T}"/> exactly once, then returns a <see cref="ListEqualityComparer{T,U}.DefaultListEqualityComparer"/> for list element type.
      /// If <typeparamref name="T"/> is or implements <see cref="ICollection{T}"/> exactly once, then returns a <see cref="CollectionEqualityComparer{T,U}.DefaultCollectionEqualityComparer"/> for collection element type.
      /// Otherwise returns <see cref="EqualityComparer{T}.Default"/>.
      /// </summary>
      /// <typeparam name="T">The type of items in sequence.</typeparam>
      /// <returns>The default equality comparer for array element type, keeping an eye on the fact that <typeparamref name="T"/> may be an array, a list, or a collection.</returns>
      /// <remarks>
      /// Since <see cref="ArrayEqualityComparer{T}.DefaultArrayEqualityComparer"/>, <see cref="ListEqualityComparer{T,U}.DefaultListEqualityComparer"/>, and <see cref="CollectionEqualityComparer{T,U}.DefaultCollectionEqualityComparer"/> all use this method, this will recursively travel all types.
      /// </remarks>
      public static IEqualityComparer<T> GetDefaultItemComparerForSomeSequence<T>()
      {
         var t = typeof( T );
         IEqualityComparer<T> retVal;
         if ( t.IsArray )
         {
            retVal = (IEqualityComparer<T>) GetEqualityComparerForArrayElementType( t.GetElementType() );
         }
         else
         {
            Type elementType;
            var iFaces = t
#if IS_NETSTANDARD
               .GetTypeInfo().ImplementedInterfaces
#else
               .GetInterfaces()
#endif
               ;
            if ( iFaces
               .Where( iface => iface
#if IS_NETSTANDARD
               .GetTypeInfo()
#endif
               .IsGenericType && Equals( iface.GetGenericTypeDefinition(), typeof( IList<> ) ) )
               .TryGetSingle( out elementType )
               )
            {
               retVal = (IEqualityComparer<T>) GetEqualityComparerForListElementType( elementType );
            }
            else if ( iFaces
               .Where( iface => iface
#if IS_NETSTANDARD
               .GetTypeInfo()
#endif
               .IsGenericType && Equals( iface.GetGenericTypeDefinition(), typeof( ICollection<> ) ) )
               .TryGetSingle( out elementType )
            )
            {
               retVal = (IEqualityComparer<T>) GetEqualityComparerForCollectionElementType( elementType );
            }
            else
            {
               retVal = EqualityComparer<T>.Default;
            }
         }
         return retVal;
      }
#if !IS_NETSTANDARD
      private const BindingFlags DEFAULT_METHOD_SEARCH_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
#endif

      private static Object GetEqualityComparerForArrayElementType( Type arrayElementType )
      {
         var method = typeof( ArrayEqualityComparer<> )
               .MakeGenericType( arrayElementType )
#if IS_NETSTANDARD
               .GetRuntimeProperty( nameof( ArrayEqualityComparer<Int32>.DefaultArrayEqualityComparer ) )
               ?.GetMethod
#else
               .GetProperty( nameof( ArrayEqualityComparer<Int32>.DefaultArrayEqualityComparer ), DEFAULT_METHOD_SEARCH_FLAGS )
               ?.GetGetMethod( true )
#endif
               ;
         return ( method ?? throw new InvalidOperationException( "Could not find property which should exist." ) ).Invoke( null, null );
      }

      private static Object GetEqualityComparerForListElementType( Type listType )
      {
         var method = typeof( ListEqualityComparer<,> )
            .MakeGenericType( listType, listType.
#if IS_NETSTANDARD
            GenericTypeArguments
#else
            GetGenericArguments()
#endif
            [0]
            )
#if IS_NETSTANDARD
            .GetRuntimeProperty( nameof( ListEqualityComparer<List<Int32>, Int32>.DefaultListEqualityComparer ) )
            ?.GetMethod
#else
            .GetProperty( nameof( ListEqualityComparer<List<Int32>, Int32>.DefaultListEqualityComparer ), DEFAULT_METHOD_SEARCH_FLAGS )
            ?.GetGetMethod( true )
#endif
            ;
         return ( method ?? throw new InvalidOperationException( "Could not find property which should exist." ) ).Invoke( null, null );
      }

      private static Object GetEqualityComparerForCollectionElementType( Type collectionType )
      {
         var method = typeof( CollectionEqualityComparer<,> )
            .MakeGenericType( collectionType, collectionType.
#if IS_NETSTANDARD
            GenericTypeArguments
#else
            GetGenericArguments()
#endif
            [0]
            )
#if IS_NETSTANDARD
            .GetRuntimeProperty( nameof( CollectionEqualityComparer<List<Int32>, Int32>.DefaultCollectionEqualityComparer ) )
            ?.GetMethod
#else
            .GetProperty( nameof( CollectionEqualityComparer<List<Int32>, Int32>.DefaultCollectionEqualityComparer ), DEFAULT_METHOD_SEARCH_FLAGS )
            ?.GetGetMethod( true )
#endif
            ;
         return ( method ?? throw new InvalidOperationException( "Could not find property which should exist." ) ).Invoke( null, null );
      }
   }

   /// <summary>
   /// This is the enumeration for picking strategy for sorting null values.
   /// </summary>
   /// 
   public enum NullSorting
   {
      /// <summary>
      /// This tells to sort <c>null</c> values first.
      /// </summary>
      NullsFirst,
      /// <summary>
      /// This tells to sort <c>null</c> values last.
      /// </summary>
      NullsLast
   }
}

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// This is helper method, which checks whether one or both of the given objects are <c>null</c>, and if so, assigns comparison result appropriate to this <see cref="NullSorting"/> enumeration, and returns <c>false</c>.
   /// If neither of the given objects are <c>null</c>, this then returns <c>true</c>.
   /// </summary>
   /// <param name="nullSortingStrategy">This <see cref="NullSorting"/> strategy.</param>
   /// <param name="x">The first argument to comparison.</param>
   /// <param name="y">The second argument to comparison.</param>
   /// <param name="comparisonResult">This parameter will hold the result of the comparison, if <paramref name="x"/> or <paramref name="y"/> or both are <c>null</c>.</param>
   /// <returns><c>true</c> if <paramref name="x"/> and <paramref name="y"/> are both non-<c>null</c>; <c>false</c> otherwise.</returns>
   public static Boolean CheckForNullValues( this NullSorting nullSortingStrategy, Object x, Object y, out Int32 comparisonResult )
   {
      Boolean retVal;
      if ( x == null )
      {
         retVal = false;
         comparisonResult = y == null ? 0 : ( nullSortingStrategy == NullSorting.NullsFirst ? -1 : 1 );
      }
      else if ( y == null )
      {
         retVal = false;
         comparisonResult = nullSortingStrategy == NullSorting.NullsFirst ? 1 : -1;
      }
      else
      {
         retVal = true;
         comparisonResult = 0;
      }

      return retVal;

   }
}
