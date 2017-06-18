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
   /// Provides skeleton implementation of dispose pattern in .NET.
   /// It will ensure dispose is called exactly once even in concurrent scenarios.
   /// </summary>
   public abstract class AbstractDisposable : IDisposable
   {
      private Int32 _disposed;

      /// <inheritdoc />
      public void Dispose()
      {
         if ( System.Threading.Interlocked.CompareExchange( ref this._disposed, 1, 0 ) == 0 )
         {
            this.Dispose( true );
            GC.SuppressFinalize( this );
         }
      }

      /// <summary>
      /// This method should do the actual disposing logic.
      /// </summary>
      /// <param name="disposing"><c>true</c> if this is called from <see cref="Dispose()"/> method; <c>false</c> if this is called from destructor.</param>
      protected abstract void Dispose( Boolean disposing );

      /// <summary>
      /// Throws an <see cref="ObjectDisposedException"/> with optional given message, if this <see cref="AbstractDisposable"/> is disposed.
      /// </summary>
      /// <param name="msg">The optional message. The default message will be used if this is not given.</param>
      public virtual void ThrowIfDisposed( String msg = null )
      {
         if ( this._disposed != 0 )
         {
            throw new ObjectDisposedException( msg ?? "Can not access disposed " + this.GetType() + "." );
         }
      }

      /// <summary>
      /// Gets value whether this object has been disposed.
      /// </summary>
      /// <value>Whether this object has been disposed.</value>
      public Boolean Disposed
      {
         get
         {
            return this._disposed != 0;
         }
      }

      /// <summary>
      /// This destructor will call the <see cref="Dispose(Boolean)"/> with <c>false</c> as parameter.
      /// </summary>
      /// <remarks>
      /// This destructor will silently discard any exceptions that are thrown within <see cref="Dispose(Boolean)"/> method.
      /// </remarks>
      ~AbstractDisposable()
      {
         try
         {
            this.Dispose( false );
         }
         catch
         {
            // Don't leak exception.
         }
      }
   }
}
