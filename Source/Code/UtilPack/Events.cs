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
using System.Text;
using System.Linq;
using UtilPack;

namespace UtilPack
{
   /// <summary>
   /// This delegate type is alternative for <see cref="EventHandler{TEventArgs}"/>.
   /// It differs from the <see cref="EventHandler{TEventArgs}"/> in the following three ways:
   /// <list type="bullet">
   /// <item><description>The generic argument <typeparamref name="TArgs"/> has <c>in</c> contravariance specification,</description></item>
   /// <item><description>The generic argument <typeparamref name="TArgs"/> no longer has a inheritance constraint, and</description></item>
   /// <item><description>The <c>sender</c> parameter of the <see cref="EventHandler{TEventArgs}"/> is missing.</description></item>
   /// </list>
   /// </summary>
   /// <typeparameter name="TArgs">The type of the arguments this delegate will receive.</typeparameter>
#if INTERNALIZE
   internal
#else
   public
#endif

      delegate void GenericEventHandler<in TArgs>( TArgs args );
}

public static partial class E_UtilPack
{
   /// <summary>
   /// Invokes all delegates that this <see cref="GenericEventHandler{TArgs}"/>, catching exceptions, and rethrowing if specified.
   /// </summary>
   /// <typeparam name="TArgs">The type of event arguments of this <see cref="GenericEventHandler{TArgs}"/>.</typeparam>
   /// <param name="evt">This <see cref="GenericEventHandler{TArgs}"/>, may be <c>null</c>.</param>
   /// <param name="args">The argument object to pass to this <see cref="GenericEventHandler{TArgs}"/>.</param>
   /// <param name="throwExceptions">Whether to rethrow any occurred exceptions.</param>
   /// <returns><c>true</c> if invoked any delegates.</returns>
   /// <exception cref="AggregateException">If more than one exception occurredm and <paramref name="throwExceptions"/> was <c>true</c>.</exception>
#if INTERNALIZE
   internal
#else
   public
#endif
      static Boolean InvokeAllEventHandlers<TArgs>( this GenericEventHandler<TArgs> evt, TArgs args, Boolean throwExceptions = true )
   {
      LinkedList<Exception> exceptions = null;
      var result = evt != null;
      if ( result )
      {
         var invocationList = evt.GetInvocationList();
         for ( var i = 0; i < invocationList.Length; ++i )
         {
            try
            {
               ( (GenericEventHandler<TArgs>) invocationList[i] )?.Invoke( args );
            }
            catch ( Exception exc )
            {
               if ( throwExceptions )
               {
                  if ( exceptions == null )
                  {
                     // Just re-throw if this is last handler and first exception
                     if ( i == invocationList.Length - 1 )
                     {
                        throw;
                     }
                     else
                     {
                        exceptions = new LinkedList<Exception>();
                     }
                  }
                  exceptions.AddLast( exc );
               }
            }
         }
      }

      if ( exceptions != null )
      {
         throw new AggregateException( exceptions.ToArray() );
      }

      return result;
   }

   /// <summary>
   /// Invokes all delegates that this <see cref="GenericEventHandler{TArgs}"/>, catching exceptions, and passing them as <c>out</c> parameter.
   /// </summary>
   /// <typeparam name="TArgs">The type of event arguments of this <see cref="GenericEventHandler{TArgs}"/>.</typeparam>
   /// <param name="evt">This <see cref="GenericEventHandler{TArgs}"/>, may be <c>null</c>.</param>
   /// <param name="args">The argument object to pass to this <see cref="GenericEventHandler{TArgs}"/>.</param>
   /// <param name="occurredExceptions">The exceptions that occurred. Will always be non-<c>null</c>. Will be empty if no exceptions occurred.</param>
   /// <returns><c>true</c> if invoked any delegates.</returns>
#if INTERNALIZE
   internal
#else
   public
#endif 
      static Boolean InvokeAllEventHandlers<TArgs>( this GenericEventHandler<TArgs> evt, TArgs args, out Exception[] occurredExceptions )
   {
      LinkedList<Exception> exceptions = null;
      var result = evt != null;
      if ( result )
      {
         foreach ( var handler in evt.GetInvocationList() )
         {
            try
            {
               ( (GenericEventHandler<TArgs>) handler )?.Invoke( args );
            }
            catch ( Exception exc )
            {
               if ( exceptions == null )
               {
                  exceptions = new LinkedList<Exception>();
               }
               exceptions.AddLast( exc );
            }
         }
      }
      if ( exceptions != null )
      {
         occurredExceptions = exceptions.ToArray();
      }
      else
      {
         occurredExceptions = Empty<Exception>.Array;
      }
      return result;
   }
}