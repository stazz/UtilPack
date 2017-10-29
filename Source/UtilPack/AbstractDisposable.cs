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

   /// <summary>
   /// This class encapsulates <see cref="IDisposable"/> resource which might be needed, or might not.
   /// </summary>
   /// <typeparam name="T">The type of the resource.</typeparam>
   public sealed class LazyDisposable<T> : AbstractDisposable
      where T : IDisposable
   {
      private readonly Lazy<T> _lazy;

      /// <summary>
      /// Creates a new instance of <see cref="LazyDisposable{T}"/> with given <see cref="Lazy{T}"/> containing the lazily initialized <see cref="IDisposable"/> resource.
      /// </summary>
      /// <param name="lazy">The <see cref="Lazy{T}"/> containing the lazily initialized <see cref="IDisposable"/> resource.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="lazy"/> is <c>null</c>.</exception>
      public LazyDisposable( Lazy<T> lazy )
      {
         this._lazy = ArgumentValidator.ValidateNotNull( nameof( lazy ), lazy );
      }

      /// <summary>
      /// Creates a new instance of <see cref="LazyDisposable{T}"/> with given callback which will be called when the <see cref="IDisposable"/> resource is used for the first time.
      /// </summary>
      /// <param name="factory">The callback to call when <see cref="IDisposable"/> resource is used for the first time.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="factory"/> is <c>null</c>.</exception>
      /// <remarks>The <paramref name="factory"/> will be used to create <see cref="Lazy{T}"/> with <see cref="System.Threading.LazyThreadSafetyMode"/> of <see cref="System.Threading.LazyThreadSafetyMode.None"/>.</remarks>
      public LazyDisposable( Func<T> factory )
         : this( new Lazy<T>( factory, System.Threading.LazyThreadSafetyMode.None ) )
      {

      }

      /// <summary>
      /// Gets the actual <see cref="IDisposable"/> resource held by this <see cref="LazyDisposable{T}"/>.
      /// </summary>
      /// <value>The actual <see cref="IDisposable"/> resource held by this <see cref="LazyDisposable{T}"/>.</value>
      public T Value => this._lazy.Value;


      /// <summary>
      /// Disposes this <see cref="LazyDisposable{T}"/> and will also dispose the resource behind the <see cref="Value"/> property, but only if it has been accessed at least once.
      /// </summary>
      /// <param name="disposing">Whether we are calling from <see cref="AbstractDisposable.Dispose()"/>.</param>
      protected override void Dispose( Boolean disposing )
      {
         if ( disposing && this._lazy.IsValueCreated )
         {
            this._lazy.Value.Dispose();
         }
      }
   }
}
