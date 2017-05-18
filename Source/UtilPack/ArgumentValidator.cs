/*
 * Copyright 2007 Niclas Hedhman.
 * (org.qi4j.api.util.NullArgumentException class)
 * See NOTICE file.
 * 
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
using System.Linq;

namespace UtilPack
{
   /// <summary>
   /// Helper class to easily verify whether some method parameter is <c>null</c> or empty.
   /// </summary>
   public static class ArgumentValidator
   {
      /// <summary>
      /// Checks whether a method parameter is <c>null</c>.
      /// </summary>
      /// <typeparam name="T">Type of parameter, must be class; to ensure that this method won't be called for struct parameters.</typeparam>
      /// <param name="parameterName">The name of the parameter.</param>
      /// <param name="value">The given parameter.</param>
      /// <returns>The <paramref name="value"/>.</returns>
      /// <exception cref="ArgumentNullException">If the <paramref name="value"/> is <c>null</c>.</exception>
      public static T ValidateNotNull<T>( String parameterName, T value )
         where T : class
      {
         if (value == null )
         {
            throw new ArgumentNullException( parameterName );
         }
         return value;
      }

      /// <summary>
      /// Checks whether the <c>this</c> parameter for extension method is <c>null</c>.
      /// </summary>
      /// <typeparam name="T">Type of parameter, must be class; to ensure that this method won't be called for struct parameters.</typeparam>
      /// <param name="value">The <c>this</c> parameter.</param>
      /// <returns>The <paramref name="value"/></returns>
      /// <exception cref="NullReferenceException">If <paramref name="value"/> is <c>null</c>.</exception>
      /// <remarks>
      /// This method throws <see cref="NullReferenceException"/> instead of <see cref="ArgumentNullException"/> because it is intended to be used solely as validating the <c>this</c> parameter of an extension method.
      /// If the extension method is later added to the interface itself instead of being an extension method, the exception behaviour will not change, and the client code don't need to re-adapt their catch-handlers.
      /// </remarks>
      public static T ValidateNotNullReference<T>( T value )
         where T : class
      {
         if (value == null)
         {
            throw new NullReferenceException( "Extension method 'this' parameter is null." );
         }
         return value;
      }

      /// <summary>
      /// Checks whether given enumerable parameter has any elements.
      /// </summary>
      /// <typeparam name="T">The type of the enumerable element.</typeparam>
      /// <param name="parameterName">The name of the parameter.</param>
      /// <param name="value">The given parameter.</param>
      /// <returns>The <paramref name="value"/>.</returns>
      /// <exception cref="ArgumentNullException">If the <paramref name="value"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">If the <paramref name="value"/> is empty.</exception>
      public static IEnumerable<T> ValidateNotEmpty<T>( String parameterName, IEnumerable<T> value )
      {
         ValidateNotNull( parameterName, value );
         if ( !value.Any() )
         {
            throw new ArgumentException( parameterName + " was empty." );
         }
         return value;
      }

      /// <summary>
      /// Checks whether given array parameter has any elements. Is somewhat faster than the <see cref="ArgumentValidator.ValidateNotEmpty{T}(System.String, IEnumerable{T})"/>.
      /// </summary>
      /// <typeparam name="T">The type of the array element.</typeparam>
      /// <param name="parameterName">The name of the parameter</param>
      /// <param name="value">The given parameter</param>
      /// <returns>The <paramref name="value"/>.</returns>
      /// <exception cref="ArgumentNullException">If the <paramref name="value"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">If the <paramref name="value"/> is empty.</exception>
      public static T[] ValidateNotEmpty<T>( String parameterName, T[] value )
      {
         ValidateNotNull( parameterName, value );
         if ( value.Length <= 0 )
         {
            throw new ArgumentException( parameterName + " was empty." );
         }
         return value;
      }

      /// <summary>
      /// Checks whether given string parameter contains any characters.
      /// </summary>
      /// <param name="parameterName">The name of the parameter</param>
      /// <param name="value">The given parameter</param>
      /// <returns>The <paramref name="value"/>.</returns>
      /// <exception cref="ArgumentNullException">If the <paramref name="value"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">If the <paramref name="value"/> is empty.</exception>
      public static String ValidateNotEmpty( String parameterName, String value )
      {
         ValidateNotNull( parameterName, value );
         if ( value.Length == 0 )
         {
            throw new ArgumentException( parameterName + " was empty string." );
         }
         return value;
      }

      /// <summary>
      /// Checks that <paramref name="values"/> is not <c>null</c>, and that all items in <paramref name="values"/> are not nulls either.
      /// Will enumerate the <paramref name="values"/> once.
      /// </summary>
      /// <typeparam name="T">The type of items.</typeparam>
      /// <param name="parameterName">The name of the parameter.</param>
      /// <param name="values">The given paramter.</param>
      /// <returns>The <paramref name="values"/>.</returns>
      /// <exception cref="ArgumentNullException">If <paramref name="values"/> is <c>null</c>, or if it contains at least one <c>null</c> item.</exception>
      public static IEnumerable<T> ValidateAllNotNull<T>( String parameterName, IEnumerable<T> values )
         where T : class
      {
         ValidateNotNull( parameterName, values );
         var idx = 0UL;
         foreach ( var val in values )
         {
            if ( val == null )
            {
               throw new ArgumentNullException( $"The item at index ${idx} was null." );
            }
            ++idx;
         }

         return values;
      }
   }
}
