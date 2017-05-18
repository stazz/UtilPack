/*
 * Copyright 2011 Stanislav Muhametsin. All rights Reserved.
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
using System.Reflection;
using System.Text;
using UtilPack;

public static partial class E_UtilPack
{
   /// <summary>
   /// Returns generic definition of <paramref name="type"/> if <see cref="P:System.Type.IsGenericType"/> returns <c>true</c> for <paramref name="type"/>.
   /// </summary>
   /// <param name="type">Type to check.</param>
   /// <returns>Generic definition of <paramref name="type" /> if it is not <c>null</c> and <see cref="P:System.Type.IsGenericType"/> returns <c>true</c>.</returns>
   public static Type GetGenericDefinitionIfGenericType( this Type type )
   {
      return type != null && type
#if NETSTANDARD1_0
         .GetTypeInfo()
#endif
         .IsGenericType ? type.GetGenericTypeDefinition() : type;
   }

   /// <summary>
   /// Returns generic definition of <paramref name="type"/> if <see cref="P:System.Type.ContainsGenericParameters"/> returns <c>true</c> and <see cref="System.Type.IsGenericParameter"/> returns <c>false</c> for <paramref name="type"/>.
   /// </summary>
   /// <param name="type">Type to check.</param>
   /// <returns>Generic definition of <paramref name="type"/> if it is not <c>null</c>, <see cref="P:System.Type.ContainsGenericParameters"/> returns <c>true</c> and <see cref="System.Type.IsGenericParameter"/> returns <c>false</c>.</returns>
   public static Type GetGenericDefinitionIfContainsGenericParameters( this Type type )
   {
      return type != null && type
#if NETSTANDARD1_0
         .GetTypeInfo()
#endif
         .ContainsGenericParameters && !type.IsGenericParameter ? type.GetGenericTypeDefinition() : type;
   }

   ///// <summary>
   ///// Tries to find a value from dictionary with types as keys. If direct lookup fails, this method will accept any value with the key which is any of the given type's parent type (base type or implemented interface).
   ///// </summary>
   ///// <typeparam name="TValue">The type of the values of the <paramref name="dictionary"/>.</typeparam>
   ///// <param name="dictionary">The dictionary to search from.</param>
   ///// <param name="type">The type to search value for.</param>
   ///// <param name="result">This will contain result if return value is <c>true</c>; otherwise <c>default(TValue)</c>.</param>
   ///// <returns>If the dictionary contains key <paramref name="type"/> or any of its parent types, <c>true</c>; otherwise, <c>false</c>.</returns>
   //public static Boolean TryFindInTypeDictionarySearchBaseTypes<TValue>( this IDictionary<Type, TValue> dictionary, Type type, out TValue result )
   //{
   //   var retVal = dictionary != null;
   //   if ( retVal )
   //   {
   //      // First try to find directly
   //      retVal = dictionary.TryGetValue( type, out result );
   //      if ( !retVal )
   //      {
   //         // If direct lookup fails, accept any base type
   //         foreach ( var typeKey in type.GetAllParentTypes( false ) )
   //         {
   //            retVal = dictionary.TryGetValue( typeKey, out result );
   //            if ( retVal )
   //            {
   //               break;
   //            }
   //         }
   //      }
   //   }
   //   else
   //   {
   //      result = default( TValue );
   //   }
   //   return retVal;
   //}

   ///// <summary>
   ///// Tries to find a value from <paramref name="dictionary"/> with types as keys. If direct lookup fails, this method will accept value of bottom-most type of <paramref name="type"/>'s inheritance hierarchy found in <paramref name="dictionary"/>.
   ///// </summary>
   ///// <typeparam name="TValue">The type of the values of the <paramref name="dictionary"/>.</typeparam>
   ///// <param name="type">The type to search value for.</param>
   ///// <param name="dictionary">The dictionary to search from.</param>
   ///// <param name="result">This will contain result if return value is <c>true</c>; otherwise <c>default(TValue)</c>.</param>
   ///// <returns>If the dictionary contains key <paramref name="type"/> or any of the keys has <paramref name="type"/> as its parent type, <c>true</c>; otherwise, <c>false</c>.</returns>
   //public static Boolean TryFindInTypeDictionarySearchBottommostType<TValue>( this IDictionary<Type, TValue> dictionary, Type type, out TValue result )
   //{
   //   var found = dictionary.TryGetValue( type, out result );
   //   if ( !found )
   //   {
   //      // Search for bottom-most type
   //      var current = type;
   //      var currentOK = false;
   //      foreach ( var kvp in dictionary )
   //      {
   //         currentOK = current.IsAssignableFrom( kvp.Key );
   //         found = currentOK || found;
   //         if ( currentOK )
   //         {
   //            result = kvp.Value;
   //            current = kvp.Key;
   //         }
   //      }
   //   }
   //   return found;
   //}

   /// <summary>
   /// Returns <c>true</c> if <see cref="M:System.Type.IsAssignableFrom(System.Type)"/> called on <paramref name="parentType"/> with <paramref name="subType"/> as parameter returns <c>true</c>, or if <paramref name="parentType"/> is generic type definition and any of the <paramref name="subType"/>'s parent types are generic type instantations of <paramref name="parentType"/>.
   /// </summary>
   /// <param name="parentType">The assumed parent type.</param>
   /// <param name="subType">The assumed sub type.</param>
   /// <returns><c>true</c> if both <paramref name="parentType"/> and <paramref name="subType"/> are not <c>null</c>, and if <see cref="M:System.Type.IsAssignableFrom(System.Type)"/> called on <paramref name="parentType"/> with <paramref name="subType"/> as parameter returns <c>true</c>, or if <paramref name="parentType"/> is generic type definition and any of the <paramref name="subType"/>'s parent types are generic type instantations of <paramref name="parentType"/>.</returns>
   public static Boolean IsAssignableFrom_IgnoreGenericArgumentsForGenericTypes( this Type parentType, Type subType )
   {
      return parentType != null && subType != null
         && ( parentType
#if NETSTANDARD1_0
         .GetTypeInfo()
#endif
         .IsAssignableFrom( subType
#if NETSTANDARD1_0
         .GetTypeInfo()
#endif
         )
            || ( parentType
#if NETSTANDARD1_0
            .GetTypeInfo()
#endif
            .IsGenericTypeDefinition
                  && subType.GetAllParentTypes().Any( b => b
#if NETSTANDARD1_0
                  .GetTypeInfo()
#endif
                  .IsGenericType && b.GetGenericTypeDefinition().Equals( parentType ) )
               )
            );
   }



   /// <summary>
   /// Returns only bottom-most types in type hierarchy of <paramref name="items"/>, where each type is extracted by <paramref name="typeSelector"/>.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="items">The items to extract types from.</param>
   /// <param name="typeSelector">The function to extract type from single item.</param>
   /// <returns>Only bottom-most types in type hierarchy of <paramref name="items"/>, where each type is extracted by <paramref name="typeSelector"/>.</returns>
   /// <remarks>
   /// This method uses <see cref="IsAssignableFrom_IgnoreGenericArgumentsForGenericTypes"/> to check whether one type is assignable from another.
   /// </remarks>
   /// <exception cref="NullReferenceException">If <paramref name="items"/> is <c>null</c>.</exception>
   public static T[] GetBottomTypes<T>( this IEnumerable<T> items, Func<T, Type> typeSelector )
   {
      return items == null ? Empty<T>.Array : items.Where( item => !items.Select( typeSelector ).Any( anotherType => !typeSelector( item ).Equals( anotherType ) && typeSelector( item ).IsAssignableFrom_IgnoreGenericArgumentsForGenericTypes( anotherType ) ) ).ToArray();
   }

   /// <summary>
   /// Returns all the implemented interfaces of <paramref name="type"/>. If <paramref name="type"/> is interface, it is also included. If <paramref name="type"/> is <c>null</c>, empty enumerable is returned.
   /// </summary>
   /// <param name="type">The type to extract implemented interfaces from. May be <c>null</c>.</param>
   /// <param name="includeItself">If <c>true</c> and <paramref name="type"/> is not <c>null</c>, it will be included in resulting enumerable.</param>
   /// <returns>All the implemented interfaces of <paramref name="type"/>. If <paramref name="type"/> is interface, it is also included. If <paramref name="type"/> is <c>null</c>, empty enumerable is returned.</returns>
   public static IEnumerable<Type> GetImplementedInterfaces( this Type type, Boolean includeItself = true )
   {
      if ( type != null )
      {
         if ( includeItself && type
#if NETSTANDARD1_0
            .GetTypeInfo()
#endif
            .IsInterface )
         {
            yield return type;
         }
         foreach ( var iFace in type
#if NETSTANDARD1_0
            .GetTypeInfo().ImplementedInterfaces
#else
            .GetInterfaces()
#endif
            )
         {
            yield return iFace;
         }
      }
   }

   /// <summary>
   /// Returns all the classes of the <paramref name="type"/>. If <paramref name="type"/> is <c>null</c> or interface, an empty enumerable is returned. Otherwise, full class hierarchy of <paramref name="type"/> is returned, including <paramref name="type"/> itself.
   /// </summary>
   /// <param name="type">The type to extract inherited classes from. May be <c>null</c>.</param>
   /// <param name="includeItself">If <c>true</c> and <paramref name="type"/> is not <c>null</c>, it will be included in resulting enumerable.</param>
   /// <returns>All the classes of the <paramref name="type"/>. If <paramref name="type"/> is <c>null</c> or interface, an empty enumerable is returned. Otherwise, full class hierarchy of <paramref name="type"/> is returned, including <paramref name="type"/> itself.</returns>
   public static IEnumerable<Type> GetClassHierarchy( this Type type, Boolean includeItself = true )
   {
      return type == null || type
#if NETSTANDARD1_0
         .GetTypeInfo()
#endif
         .IsInterface ?
         Empty<Type>.Enumerable :
         type.AsSingleBranchEnumerable( t => t
#if NETSTANDARD1_0
         .GetTypeInfo()
#endif
         .BaseType, includeFirst: includeItself );
   }

   /// <summary>
   /// Returns all the classes and interfaces of the <paramref name="type"/>. If <paramref name="type"/> is <c>null</c>, an empty enumerable is returned.
   /// </summary>
   /// <param name="type">The type to extract classes and interfaces from. May be <c>null</c>.</param>
   /// <param name="includeItself">If <c>true</c> and <paramref name="type"/> is not <c>null</c>, it will be included in resulting enumerable.</param>
   /// <returns>All the classes and interfaces of the <paramref name="type"/>. If <paramref name="type"/> is <c>null</c>, an empty enumerable is returned.</returns>
   public static IEnumerable<Type> GetAllParentTypes( this Type type, Boolean includeItself = true )
   {
      return ( includeItself ? type : ( type == null ? null : type
#if NETSTANDARD1_0
         .GetTypeInfo()
#endif
         .BaseType ) ).GetClassHierarchy().Concat( type.GetImplementedInterfaces( includeItself ) );
   }

   /// <summary>
   /// Checks whether <paramref name="type"/> is non-<c>null</c> and a nullable type.
   /// If so, the <paramref name="paramType"/> will contain the underlying value type of the value type.
   /// </summary>
   /// <param name="type">The type to check.</param>
   /// <param name="paramType">This will contain underlying value type if this method returns <c>true</c>; otherwise it will be <paramref name="type"/>.</param>
   /// <returns>If the <paramref name="type"/> is non-<c>null</c> and nullable type, <c>true</c>; otherwise, <c>false</c>.</returns>
   public static Boolean IsNullable( this Type type, out Type paramType )
   {
      var result = IsNullable( type );
      paramType = result ? type.GetGenericArguments()[0] : type;
      return result;
   }

   /// <summary>
   /// Checks whether <paramref name="type"/> is non-<c>null</c> and lazy type (instance of <see cref="Lazy{T}"/>).
   /// </summary>
   /// <param name="type">Type to check.</param>
   /// <returns><c>true</c> if <paramref name="type"/> is non-<c>null</c> and lazy type; <c>false</c> otherwise.</returns>
   public static Boolean IsLazy( this Type type )
   {
      return type != null && type.GetGenericDefinitionIfGenericType().Equals( typeof( Lazy<> ) );
   }

   /// <summary>
   /// Checks whether <paramref name="type"/> is non-<c>null</c> and lazy type (instance of <see cref="Lazy{T}"/>).
   /// If so, the <paramref name="paramType"/> will contain the underlying type of the lazy type.
   /// </summary>
   /// <param name="type">The type to check.</param>
   /// <param name="paramType">This will contain underlying type of lazy type if this method returns <c>true</c>; otherwise it will be <paramref name="type"/>.</param>
   /// <returns><c>true</c> if <paramref name="type"/> is non-<c>null</c> and lazy type; <c>false</c> otherwise.</returns>
   public static Boolean IsLazy( this Type type, out Type paramType )
   {
      var result = IsLazy( type );
      paramType = result ? type.GetGenericArguments()[0] : type;
      return result;
   }

   /// <summary>
   /// Returns generic arguments of <paramref name="type"/> if it is not <c>null</c>, is not generic parameter, and is generic type.
   /// </summary>
   /// <param name="type">The type to get generic arguments from. May be <c>null</c>.</param>
   /// <returns>Generic arguments of <paramref name="type"/> if it is not <c>null</c>, is not generic parameter, and is generic type. Otherwise, returns an empty array.</returns>
   public static Type[] GetGenericArgumentsSafe( this Type type )
   {
      return type == null || type.IsGenericParameter || !type
#if NETSTANDARD1_0
         .GetTypeInfo()
#endif
         .IsGenericType ? Empty<Type>.Array : type.GetGenericArguments();
   }

   /// <summary>
   /// Checks whether <paramref name="type"/> is non-<c>null</c> and a nullable type.
   /// </summary>
   /// <param name="type">Type to check.</param>
   /// <returns><c>true</c> if <paramref name="type"/> is non-<c>null</c> and nullable type; <c>false</c> otherwise.</returns>
   public static Boolean IsNullable( this Type type )
   {
      return type != null && type.GetGenericDefinitionIfGenericType().Equals( typeof( Nullable<> ) );
   }

   /// <summary>
   /// Tries to load a method named as <paramref name="methodName"/> from <paramref name="type"/>. If <paramref name="paramCount"/> is specified, it also filters results based on method parameter count.
   /// </summary>
   /// <param name="type">The type which should contain the method.</param>
   /// <param name="methodName">The name of the method.</param>
   /// <param name="paramCount">The amount of parameters for the method to have, or <c>null</c> to accept method with any amount of parameters.</param>
   /// <param name="acceptNonPublic">Whether to accept non-public matching methods.</param>
   /// <returns>A first method suitable for given search criteria. Will never be <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> or <paramref name="methodName"/> are <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the matching method could not be found.</exception>
   public static MethodInfo LoadMethodOrThrow(
      this Type type,
      String methodName,
      Int32? paramCount,
      Boolean acceptNonPublic = false
      )
   {
      var results = type.GetMethodsPortable( methodName, acceptNonPublic );
      if ( paramCount.HasValue )
      {
         results = results.Where( m => m.GetParameters().Length == paramCount.Value );
      }

      var result = results.FirstOrDefault();

      if ( result == null )
      {
         throw new ArgumentException( "Could not find method " + methodName + " in type " + type + "." );
      }
      return result;
   }

   /// <summary>
   /// Tries to load a method named as <paramref name="methodName"/> from <paramref name="type"/>. If <paramref name="paramTypes"/> is non-<c>null</c>, it will only accept methods with given parameter types.
   /// </summary>
   /// <param name="type">The type which should contain the method.</param>
   /// <param name="methodName">The name of the method.</param>
   /// <param name="paramTypes">The types of method parameters. May be <c>null</c> to accept any parameter types.</param>
   /// <param name="acceptNonPublic">Whether to accept non-public matching methods.</param>
   /// <returns>A first method suitable for given search criteria. Will never be <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> or <paramref name="methodName"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the matching method could not be found.</exception>
   public static MethodInfo LoadMethodWithParamTypesOrThrow(
      this Type type,
      String methodName,
      Type[] paramTypes,
      Boolean acceptNonPublic = false
      )
   {
      var results = type.GetMethodsPortable( methodName, acceptNonPublic );
      if ( paramTypes != null )
      {
         results = results.Where( method => method.GetParameters().Length == paramTypes.Length && method.GetParameters().Where( ( info, idx ) => info.ParameterType.Equals( paramTypes[idx] ) ).Count() == paramTypes.Length );
      }

      var result = results.FirstOrDefault();

      if ( result == null )
      {
         throw new ArgumentException( "Could not find method " + methodName + " in type " + type + "." );
      }
      return result;
   }

   /// <summary>
   /// Tries to load a generic method definition named as <paramref name="methodName"/> from <paramref name="type"/>.
   /// </summary>
   /// <param name="type">The type which should contain the method.</param>
   /// <param name="methodName">The name of the method.</param>
   /// <param name="acceptNonPublic">Whether to accept non-public matching methods.</param>
   /// <returns>A first generic method definition suitable for given search criteria. Will never be <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> or <paramref name="methodName"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the matching method could not be found.</exception>
   public static MethodInfo LoadMethodGDefinitionOrThrow(
      this Type type,
      String methodName,
      Boolean acceptNonPublic = false
      )
   {
      var result = type.GetMethodsPortable( methodName, acceptNonPublic ).Where( m => m.IsGenericMethodDefinition ).FirstOrDefault();
      if ( result == null )
      {
         throw new ArgumentException( "Could not find generic method definition " + methodName + " in type " + type + "." );
      }
      return result;
   }

   /// <summary>
   /// Tries to load getter method for a property named <paramref name="propertyName"/> from <paramref name="type"/>.
   /// </summary>
   /// <param name="type">The type which should contain the property.</param>
   /// <param name="propertyName">The name of the property.</param>
   /// <param name="acceptNonPublic">Whether to accept non-public matching methods.</param>
   /// <returns>A getter method for property named <paramref name="propertyName"/> in <paramref name="type"/>. Will never be <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the matching property or getter method could not be found.</exception>
   public static MethodInfo LoadGetterOrThrow(
      this Type type,
      String propertyName,
      Boolean acceptNonPublic = false
      )
   {
      var result = LoadPropertyOrThrow( type, propertyName, acceptNonPublic )
#if NETSTANDARD1_0
         .GetMethod
#else
         .GetGetMethod( true )
#endif
         ;
      if ( result == null )
      {
         throw new ArgumentException( "Could not find property getter for property " + propertyName + " in type " + type + "." );
      }
      return result;
   }

   /// <summary>
   /// Tries to load setter method for a property named <paramref name="propertyName"/> from <paramref name="type"/>.
   /// </summary>
   /// <param name="type">The type which should contain the property.</param>
   /// <param name="propertyName">The name of the property.</param>
   /// <param name="acceptNonPublic">Whether to accept non-public matching methods.</param>
   /// <returns>A setter method for property named <paramref name="propertyName"/> in <paramref name="type"/>. Will never be <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the matching property or setter method could not be found.</exception>
   public static MethodInfo LoadSetterOrThrow(
      this Type type,
      String propertyName,
      Boolean acceptNonPublic = false
      )
   {
      var result = LoadPropertyOrThrow( type, propertyName, acceptNonPublic )
#if NETSTANDARD1_0
         .SetMethod
#else
         .GetSetMethod(true )
#endif
         ;
      if ( result == null )
      {
         throw new ArgumentException( "Could not find property setter for property " + propertyName + " in type " + type + "." );
      }
      return result;
   }

   /// <summary>
   /// Tries to load a field named <paramref name="fieldName"/> from <paramref name="type"/>.
   /// </summary>
   /// <param name="type">The type which should contain the field.</param>
   /// <param name="fieldName">The name of the field.</param>
   /// <param name="acceptNonPublic">Whether to accept non-public matching fields.</param>
   /// <returns>A field named <paramref name="fieldName"/> in <paramref name="type"/>. Will never be <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> or <paramref name="fieldName"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the matching field could not be found.</exception>
   public static FieldInfo LoadFieldOrThrow(
      this Type type,
      String fieldName,
      Boolean acceptNonPublic = false
      )
   {
      var result = type.GetFieldsPortable( fieldName, acceptNonPublic ).FirstOrDefault();
      if ( result == null )
      {
         throw new ArgumentException( "Could not find static field " + fieldName + " in type " + type + "." );
      }
      return result;
   }

   /// <summary>
   /// Tries to load a constructor from <paramref name="type"/>. If <paramref name="paramCount"/> is specified, it filters results based on constructor parameter count.
   /// </summary>
   /// <param name="type">The type which should contain the constructor.</param>
   /// <param name="paramCount">The amount of parameters for the constructor to have, or <c>null</c> to accept constructor with any amount of parameters.</param>
   /// <param name="acceptNonPublic">Whether to accept non-public matching constructors.</param>
   /// <param name="acceptStatic">Whether to accept static constructor.</param>
   /// <returns>A first constructor suitable for given search criteria. Will never be <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the matching constructor could not be found.</exception>
   /// <remarks>Setting parameter <paramref name="acceptStatic"/> to <c>true</c> implicitly sets <paramref name="acceptNonPublic"/> to <c>true</c>.</remarks>
   public static ConstructorInfo LoadConstructorOrThrow(
      this Type type,
      Int32? paramCount,
      Boolean acceptNonPublic = false,
      Boolean acceptStatic = false
      )
   {
      var results = type.GetConstructorsPortable( acceptNonPublic, acceptStatic );

      if ( paramCount.HasValue )
      {
         results = results.Where( ctor => ctor.GetParameters().Length == paramCount.Value );
      }
      var result = results.FirstOrDefault();

      if ( result == null )
      {
         throw new ArgumentException( "Could not find constructor in type " + type + " with parameter count of " + paramCount + "." );
      }
      return result;
   }

   /// <summary>
   /// Tries to load a constructor from <paramref name="type"/>. If <paramref name="paramTypes"/> is non-<c>null</c>, it will only accept constructors with given parameter types.
   /// </summary>
   /// <param name="type">The type which should contain the constructor.</param>
   /// <param name="paramTypes">The types of constructor parameters. May be <c>null</c> to accept any parameter types.</param>
   /// <param name="acceptNonPublic">Whether to accept non-public matching constructors.</param>
   /// <param name="acceptStatic">Whether to accept static constructor.</param>
   /// <returns>A first constructor suitable for given search criteria. Will never be <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the matching constructor could not be found.</exception>
   public static ConstructorInfo LoadConstructorOrThrow(
      this Type type,
      Type[] paramTypes,
      Boolean acceptNonPublic = false,
      Boolean acceptStatic = false
      )
   {
      var results = type.GetConstructorsPortable( acceptNonPublic, acceptStatic );
      if ( paramTypes != null )
      {
         results = results.Where( ctor => ctor.GetParameters().Select( param => param.ParameterType ).SequenceEqual( paramTypes ) );
      }
      var result = results.FirstOrDefault();
      if ( result == null )
      {
         throw new ArgumentException( "Could not find constructor in type " + type + " with parameters " + String.Join( ", ", (Object[]) paramTypes ) + "." );
      }
      return result;
   }

   /// <summary>
   /// Tries to load a property named <paramref name="name"/> from <paramref name="type"/>.
   /// </summary>
   /// <param name="type">The type which should contain the property.</param>
   /// <param name="name">The name of the property to search.</param>
   /// <param name="acceptNonPublic">Whether to accept non-public matching properties.</param>
   /// <returns>A first property suitable for given search criteria. Will never be <c>null</c>.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> or <paramref name="name"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If the matching property could not be found.</exception>
   public static PropertyInfo LoadPropertyOrThrow(
      this Type type,
      String name,
      Boolean acceptNonPublic = false
      )
   {
      var result = type.GetPropertiesPortable( name, acceptNonPublic ).FirstOrDefault();
      if ( result == null )
      {
         throw new ArgumentException( "Could not find property " + name + " in type " + type + "." );
      }
      return result;
   }

   /// <summary>
   /// Recursively gets <paramref name="type"/>'s element type as long as it is by-ref, array, or pointer type. Returns the final result.
   /// </summary>
   /// <param name="type">The type to check.</param>
   /// <returns>The 'root' of the given type, which will not be by-ref, array, or pointer type.</returns>
   public static Type GetRootOfArrayByRefPointerType( this Type type )
   {
      while ( type.IsByRef || type.IsArray || type.IsPointer )
      {
         type = type.GetElementType();
      }
      return type;
   }

   /// <summary>
   /// Recursively gets <paramref name="type"/>'s element type as long as it is by-ref or pointer type. Returns the final result.
   /// </summary>
   /// <param name="type">The type to check.</param>
   /// <returns>The 'root' of the given type, which will not be by-ref or pointer type. It may be array type, however.</returns>
   public static Type GetRootOfByRefPointerType( this Type type )
   {
      while ( type.IsByRef || type.IsPointer )
      {
         type = type.GetElementType();
      }
      return type;
   }

   /// <summary>
   /// Tries to get array type of the <paramref name="type"/>. If the <paramref name="type"/> is array type, this method will return <c>true</c>, otherwise it will return <c>false</c>. The <paramref name="elementType"/> will contain the element type of the <paramref name="type"/> if this method returns <c>true</c>; otherwise <paramref name="elementType"/> will be <c>null</c>.
   /// </summary>
   /// <param name="type">The type to check.</param>
   /// <param name="elementType">This will contain element type of the <paramref name="type"/> if it is array type, otherwise it will be <c>null</c>.</param>
   /// <returns><c>true</c> if <paramref name="type"/> is array type; <c>false</c> otherwise.</returns>
   /// <exception cref="ArgumentNullException">If <paramref name="type"/> is <c>null</c>.</exception>
   public static Boolean TryGetArrayType( this Type type, out Type elementType )
   {
      ArgumentValidator.ValidateNotNull( "Type", type );
      var result = type.IsArray;
      elementType = result ? type.GetElementType() : null;
      return result;
   }

   /// <summary>
   /// Returns <c>true</c> iff <paramref name="first"/> and <paramref name="second"/>
   /// <list type="number">
   /// <item><description>are both <c>null</c>, or</description></item>
   /// <item><description>are both non-<c>null</c> and <see cref="Object.Equals(Object)"/> returns <c>true</c>.</description></item>
   /// </list>
   /// </summary>
   /// <typeparam name="T">The type of the objects.</typeparam>
   /// <param name="first">The first object.</param>
   /// <param name="second">The second object.</param>
   /// <returns><c>true</c> if either 1. <paramref name="first"/> and <paramref name="second"/> are both <c>null</c>, or 2. both are non-<c>null</c> and <see cref="Object.Equals(Object)"/> returns <c>true</c>; <c>false</c> otherwise.</returns>
   /// <remarks>
   /// This is a variant of <see cref="Object.Equals(Object, Object)"/> method which forces both arguments to be casted to same type.
   /// </remarks>
   public static Boolean EqualsTyped<T>( this T first, T second )
      where T : class
   {
      return ( first == null && second == null ) || ( first != null && second != null && first.Equals( second ) );
   }

   /// <summary>
   /// Returns <c>true</c> iff <paramref name="first"/> and <paramref name="second"/>
   /// <list type="number">
   /// <item><description>are both <c>null</c>, or</description></item>
   /// <item><description>are both non-<c>null</c> and <see cref="IEquatable{T}.Equals(T)"/> returns <c>true</c>.</description></item>
   /// </list>
   /// </summary>
   /// <typeparam name="T">The type of the objects.</typeparam>
   /// <param name="first">The first object.</param>
   /// <param name="second">The second object.</param>
   /// <returns><c>true</c> if either 1. <paramref name="first"/> and <paramref name="second"/> are both <c>null</c>, or 2. both are non-<c>null</c> and <see cref="Object.Equals(Object)"/> returns <c>true</c>; <c>false</c> otherwise.</returns>
   public static Boolean EqualsTypedEquatable<T>( this T first, T second )
      where T : class, IEquatable<T>
   {
      return ReferenceEquals( first, second ) || ( first != null && second != null && first.Equals( second ) );
   }

   /// <summary>
   /// Helper method to invoke <see cref="IEquatable{T}.Equals(T)"/> for nullable structs without throwind exception if this struct does not have value.
   /// </summary>
   /// <typeparam name="T">The type of the struct.</typeparam>
   /// <param name="first">The first struct, nullable.</param>
   /// <param name="second">The second struct.</param>
   /// <returns><c>true</c> if <paramref name="first"/> has value and <see cref="IEquatable{T}.Equals(T)"/> returns <c>true</c> when invoked on <paramref name="first"/> with <paramref name="second"/> as argument; <c>false</c> otherwise.</returns>
   public static Boolean EqualsTypedEquatable<T>( this T? first, T second )
      where T : struct, IEquatable<T>
   {
      return first.HasValue && first.Equals( second );
   }

   /// <summary>
   /// Helper method to invoke <see cref="IEquatable{T}.Equals(T)"/> for nullable structs without throwind exception if this struct does not have value.
   /// </summary>
   /// <typeparam name="T">The type of the struct.</typeparam>
   /// <param name="first">The first struct.</param>
   /// <param name="second">The second struct, nullable.</param>
   /// <returns><c>true</c> if <paramref name="second"/> has value and <see cref="IEquatable{T}.Equals(T)"/> returns <c>true</c> when invoked on <paramref name="first"/> with <paramref name="second"/> as argument; <c>false</c> otherwise.</returns>
   public static Boolean EqualsTypedEquatable<T>( this T first, T? second )
      where T : struct, IEquatable<T>
   {
      return second.HasValue && first.Equals( second );
   }

   /// <summary>
   /// Helper method to invoke <see cref="IEquatable{T}.Equals(T)"/> for nullable structs without throwind exception if this struct does not have value.
   /// </summary>
   /// <typeparam name="T">The type of the struct.</typeparam>
   /// <param name="first">The first struct, nullable.</param>
   /// <param name="second">The second struct, nullable.</param>
   /// <returns><c>true</c> if both <paramref name="first"/> and <paramref name="second"/> have value and <see cref="IEquatable{T}.Equals(T)"/> returns <c>true</c> when invoked on <paramref name="first"/> with <paramref name="second"/> as argument; <c>false</c> otherwise.</returns>
   public static Boolean EqualsTypedEquatable<T>( this T? first, T? second )
      where T : struct, IEquatable<T>
   {
      return first.HasValue == second.HasValue && ( !first.HasValue || ( second.HasValue && first.Equals( second.Value ) ) );
   }

   /// <summary>
   /// Returns only bottom-most types in type hierarchy of <paramref name="types"/>.
   /// </summary>
   /// <param name="types">The types to check.</param>
   /// <returns>Only bottom-most types in type hierarchy of <paramref name="types"/>.</returns>
   /// <remarks>
   /// This method uses <see cref="E_UtilPack.IsAssignableFrom_IgnoreGenericArgumentsForGenericTypes"/> to check whether one type is assignable from another.
   /// </remarks>
   public static Type[] GetBottomTypes( this IEnumerable<Type> types )
   {
      return types == null ? Empty<Type>.Array : types.Where( type => !types.Any( anotherType => !type.Equals( anotherType ) && type.IsAssignableFrom_IgnoreGenericArgumentsForGenericTypes( anotherType ) ) ).Distinct().ToArray();
   }

   ///// <summary>
   ///// Returns <c>true</c> iff <paramref name="first"/> and <paramref name="second"/>
   ///// <list type="number">
   ///// <item><description>are both <c>null</c>, or</description></item>
   ///// <item><description>are both non-<c>null</c> and <see cref="Enumerable.SequenceEqual{T}(IEnumerable{T}, IEnumerable{T})"/> returns <c>true</c></description></item>
   ///// </list>
   ///// </summary>
   ///// <typeparam name="U">The type of enumerable elements.</typeparam>
   ///// <param name="first">The first enumerable.</param>
   ///// <param name="second">The second enumerable</param>
   ///// <param name="equalityComparer">The equality comparer to use if both <paramref name="first"/> and <paramref name="second"/> are non-<c>null</c>. May be <c>null</c> in order to use default equality comparer.</param>
   ///// <returns><c>true</c> if either 1. <paramref name="first"/> and <paramref name="second"/> are both <c>null</c>, or 2. both are non-<c>null</c> and <see cref="Enumerable.SequenceEqual{T}(IEnumerable{T}, IEnumerable{T})"/> returns <c>true</c>; <c>false</c> otherwise.</returns>
   //public static Boolean BothNullOrSequenceEquals<U>( IEnumerable<U> first, IEnumerable<U> second, IEqualityComparer<U> equalityComparer = null )
   //{
   //   return ( first == null && second == null ) || ( first != null && second != null && first.SequenceEqual( second, equalityComparer ) );
   //}

   ///// <summary>
   ///// Returns hash code for <paramref name="obj"/> if it is non-<c>null</c>, or <paramref name="nullHashCode"/> if <paramref name="obj"/> is <c>null</c>.
   ///// </summary>
   ///// <param name="obj">Object to get hash code from, or <c>null</c>.</param>
   ///// <param name="nullHashCode">The hash code to return if <paramref name="obj"/> is <c>null</c>.</param>
   ///// <returns>The value of <see cref="Object.GetHashCode()"/> if <paramref name="obj"/> is non-<c>null</c>; otherwise returns <paramref name="nullHashCode"/>.</returns>
   //public static Int32 GetHashCodeOrCustom( Object obj, Int32 nullHashCode = 0 )
   //{
   //   return obj == null ? nullHashCode : obj.GetHashCode();
   //}

   ///// <summary>
   ///// Invokes the event if it is non-<c>null</c>.
   ///// </summary>
   ///// <typeparam name="TEvent">The type of the event.</typeparam>
   ///// <param name="evt">The value of the event field.</param>
   ///// <param name="invoker">The lambda to invoke non-<c>null</c> event.</param>
   ///// <returns><c>true</c> if <paramref name="evt"/> was non-<c>null</c>; <c>false</c> otherwise.</returns>
   //public static Boolean InvokeEventIfNotNull<TEvent>( this TEvent evt, Action<TEvent> invoker )
   //   where TEvent : class
   //{
   //   var result = evt != null;
   //   if ( result )
   //   {
   //      invoker( evt );
   //   }
   //   return result;
   //}

   ///// <summary>
   ///// Invokes all event handlers one by one, even if some of them throw exception.
   ///// </summary>
   ///// <typeparam name="TEvent">The type of the event.</typeparam>
   ///// <param name="evt">The value of the event field.</param>
   ///// <param name="invoker">The lambda to invoke non-<c>null</c> event.</param>
   ///// <param name="throwExceptions">Whether this method should throw exceptions that are thrown by event handlers.</param>
   ///// <returns><c>true</c> if <paramref name="evt"/> was non-<c>null</c>; <c>false</c> otherwise.</returns>
   ///// <exception cref="AggregateException">If <paramref name="throwExceptions"/> is <c>true</c> and any of the event handler throws an exception. The exception(s) will be given to the <see cref="AggregateException"/> constructor.</exception>
   ///// <remarks>If <paramref name="throwExceptions"/> is <c>true</c> and first exception is thrown by last event handler, then that exception is re-thrown instead of throwing <see cref="AggregateException"/>.</remarks>
   //public static Boolean InvokeAllEventHandlers<TEvent>( this TEvent evt, Action<TEvent> invoker, Boolean throwExceptions = true )
   //   where TEvent : class
   //{
   //   LinkedList<Exception> exceptions = null;
   //   var result = evt != null;
   //   if ( result )
   //   {
   //      var invocationList = ( (Delegate) (Object) evt ).GetInvocationList();
   //      for ( var i = 0; i < invocationList.Length; ++i )
   //      {
   //         try
   //         {
   //            invoker( (TEvent) (Object) invocationList[i] );
   //         }
   //         catch ( Exception exc )
   //         {
   //            if ( throwExceptions )
   //            {
   //               if ( exceptions == null )
   //               {
   //                  // Just re-throw if this is last handler and first exception
   //                  if ( i == invocationList.Length - 1 )
   //                  {
   //                     throw;
   //                  }
   //                  else
   //                  {
   //                     exceptions = new LinkedList<Exception>();
   //                  }
   //               }
   //               exceptions.AddLast( exc );
   //            }
   //         }
   //      }
   //   }

   //   if ( exceptions != null )
   //   {
   //      throw new AggregateException( exceptions.ToArray() );
   //   }

   //   return result;
   //}

   ///// <summary>
   ///// Invokes all event handlers one by one, even if some of them throw exception.
   ///// </summary>
   ///// <typeparam name="TEvent">The type of the event.</typeparam>
   ///// <param name="evt">The value of the event field.</param>
   ///// <param name="invoker">The lambda to invoke non-<c>null</c> event.</param>
   ///// <param name="occurredExceptions">This will hold all exceptions thrown by event handlers. Will be <c>null</c> if no exceptions were thrown.</param>
   ///// <returns><c>true</c> if <paramref name="evt"/> was non-<c>null</c>; <c>false</c> otherwise.</returns>
   //public static Boolean InvokeAllEventHandlers<TEvent>( this TEvent evt, Action<TEvent> invoker, out Exception[] occurredExceptions )
   //   where TEvent : class
   //{
   //   LinkedList<Exception> exceptions = null;
   //   var result = evt != null;
   //   if ( result )
   //   {
   //      foreach ( var handler in ( (Delegate) (Object) evt ).GetInvocationList() )
   //      {
   //         try
   //         {
   //            invoker( (TEvent) (Object) handler );
   //         }
   //         catch ( Exception exc )
   //         {
   //            if ( exceptions == null )
   //            {
   //               exceptions = new LinkedList<Exception>();
   //            }
   //            exceptions.AddLast( exc );
   //         }
   //      }
   //   }
   //   if ( exceptions != null )
   //   {
   //      occurredExceptions = exceptions.ToArray();
   //   }
   //   else
   //   {
   //      occurredExceptions = null;
   //   }
   //   return result;
   //}


#if !NETSTANDARD1_0
   // These fields as integers in order not to cause compiler warnings in WP projects
   private const Int32 DEFAULT_METHOD_SEARCH_FLAGS = (Int32) ( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static );
   private const Int32 DEFAULT_CTOR_SEARCH_FLAGS = (Int32) ( BindingFlags.Public | BindingFlags.Instance );
#endif

   private static IEnumerable<MethodInfo> GetMethodsPortable( this Type type, String methodName, Boolean acceptNonPublic )
   {
      ArgumentValidator.ValidateNotNullReference( type );
      ArgumentValidator.ValidateNotNull( "Method name", methodName );

#if NETSTANDARD1_0
      return type.GetRuntimeMethods().Where( m => ( acceptNonPublic || m.IsPublic ) && String.Equals( m.Name, methodName ) );
#else
      var flags = (BindingFlags) DEFAULT_METHOD_SEARCH_FLAGS;
      if ( acceptNonPublic )
      {
         flags |= BindingFlags.NonPublic;
      }
      return type.GetMethods( flags ).Where( m => String.Equals( m.Name, methodName ) );
#endif
   }

   private static IEnumerable<FieldInfo> GetFieldsPortable( this Type type, String fieldName, Boolean acceptNonPublic )
   {
      ArgumentValidator.ValidateNotNullReference( type );
      ArgumentValidator.ValidateNotNull( "Field name", fieldName );
#if NETSTANDARD1_0
      return type.GetRuntimeFields().Where( f => ( acceptNonPublic || f.IsPublic ) && String.Equals( f.Name, fieldName ) );
#else

      var flags = (BindingFlags) DEFAULT_METHOD_SEARCH_FLAGS;
      if ( acceptNonPublic )
      {
         flags |= BindingFlags.NonPublic;
      }
      return type.GetFields( flags ).Where( f => String.Equals( f.Name, fieldName ) );
#endif
   }

   private static IEnumerable<ConstructorInfo> GetConstructorsPortable( this Type type, Boolean acceptNonPublic, Boolean acceptStatic )
   {
      ArgumentValidator.ValidateNotNullReference( type );
#if NETSTANDARD1_0
      var retVal = type.GetTypeInfo().DeclaredConstructors;
      if ( !acceptStatic )
      {
         retVal = retVal.Where( c => !c.IsStatic );
      }
      if ( !acceptNonPublic )
      {
         retVal = retVal.Where( c => !c.IsPublic );
      }

      return retVal;
#else
      var flags = (BindingFlags) DEFAULT_CTOR_SEARCH_FLAGS;
      var retVal = type.GetConstructors( flags );
      if ( acceptStatic || acceptNonPublic )
      {
         flags |= BindingFlags.NonPublic;
      }
      if ( acceptStatic )
      {
         flags |= BindingFlags.Static;
      }

      return type.GetConstructors( flags );
#endif
   }

   private static IEnumerable<PropertyInfo> GetPropertiesPortable( this Type type, String propertyName, Boolean acceptNonPublic )
   {
      ArgumentValidator.ValidateNotNullReference( type );
      ArgumentValidator.ValidateNotNull( "Property name", propertyName );

#if NETSTANDARD1_0
      return type.GetRuntimeProperties().Where( p => String.Equals( p.Name, propertyName ) && ( acceptNonPublic || ( p.GetMethod?.IsPublic ?? false ) || ( p.SetMethod?.IsPublic ?? false ) ) );
#else
      var flags = (BindingFlags) DEFAULT_METHOD_SEARCH_FLAGS;
      if ( acceptNonPublic )
      {
         flags |= BindingFlags.NonPublic;
      }
      return type.GetProperties( flags ).Where( p => String.Equals( p.Name, propertyName ) );
#endif
   }

#if NETSTANDARD1_0

   [System.Runtime.CompilerServices.MethodImpl( System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining )]
   internal static Type[] GetGenericArguments( this Type type )
   {
      return type.GetTypeInfo().GenericTypeParameters;
   }

#endif


   //#if WINDOWS_PHONE_APP_OLD

   //   /// <summary>
   //   /// Helper method to get generic arguments of <see cref="Type"/> on Windows Phone 8.1.
   //   /// </summary>
   //   /// <param name="type">The type.</param>
   //   /// <returns>The value of <see cref="TypeInfo.GenericTypeParameters"/> for <paramref name="type"/>.</returns>
   //   public static Type[] GetGenericArguments(this Type type)
   //   {
   //      return type.GetTypeInfo().GenericTypeParameters;
   //   }

   //#endif

}