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
using System.Threading;

namespace UtilPack
{
   /// <summary>
   /// This is common interface for classes providing instance pool functionality for types which are reference types.
   /// </summary>
   /// <typeparam name="TInstance"></typeparam>
   public interface LocklessInstancePoolForClasses<TInstance>
      where TInstance : class
   {
      /// <summary>
      /// Takes an existing instance from this pool and returns it, or returns <c>null</c> if no existing instance is available.
      /// </summary>
      /// <returns>An existing instance of <typeparamref name="TInstance"/>, or <c>null</c> if no existing instance is available.</returns>
      TInstance TakeInstance();

      /// <summary>
      /// Returns an existing instance to this pool. Nothing is done if <paramref name="instance"/> is <c>null</c>.
      /// </summary>
      /// <param name="instance">The instance to return to this pool.</param>
      void ReturnInstance( TInstance instance );
   }

   /// <summary>
   /// This class implements semantics of containing multiple instances of some type, with methods for taking and returning instances.
   /// This class is fully threadsafe and does not use locks in its implementation.
   /// The type must be class or interface.
   /// </summary>
   /// <typeparam name="TInstance">The type of the instances to hold.</typeparam>
   public sealed class DefaultLocklessInstancePoolForClasses<TInstance> : LocklessInstancePoolForClasses<TInstance>
      where TInstance : class
   {
      private readonly LocklessInstancePoolForClassesNoHeapAllocations<InstanceHolder<TInstance>> _pool;

      /// <summary>
      /// Creates new instance of <see cref="DefaultLocklessInstancePoolForClasses{TInstance}"/>.
      /// </summary>
      public DefaultLocklessInstancePoolForClasses()
      {
         this._pool = new LocklessInstancePoolForClassesNoHeapAllocations<InstanceHolder<TInstance>>();
      }

      /// <inheritdoc />
      public TInstance TakeInstance()
      {
         var retVal = this._pool.TakeInstance();
         return retVal == null ? null : retVal.Instance;
      }

      /// <inheritdoc />
      public void ReturnInstance( TInstance instance )
      {
         if ( instance != null )
         {
            this._pool.ReturnInstance( new InstanceHolder<TInstance>( instance ) );
         }
      }
   }

   /// <summary>
   /// This class implements semantics of containing multiple instances of some type, with methods for taking and returning instances.
   /// This class is fully threadsafe and does not use locks in its implementation.
   /// </summary>
   /// <typeparam name="TInstance">The type of the instances to hold.</typeparam>
   /// <remarks>
   /// One should use <see cref="DefaultLocklessInstancePoolForClasses{TInstance}"/> if <typeparamref name="TInstance"/> is known at compile time never to be a struct.
   /// This class is a bit slower than <see cref="DefaultLocklessInstancePoolForClasses{TInstance}"/> and contains different API.
   /// </remarks>
   public sealed class LocklessInstancePoolGeneric<TInstance>
   {
      private readonly LocklessInstancePoolForClassesNoHeapAllocations<InstanceHolder<TInstance>> _pool;

      /// <summary>
      /// Creates new instance of <see cref="LocklessInstancePoolGeneric{TInstance}"/>.
      /// </summary>
      public LocklessInstancePoolGeneric()
      {
         this._pool = new LocklessInstancePoolForClassesNoHeapAllocations<InstanceHolder<TInstance>>();
      }

      /// <summary>
      /// Attempts to remove an instance from this <see cref="LocklessInstancePoolGeneric{TInstance}"/>.
      /// </summary>
      /// <param name="item">When this method returns, this will contain instance of <typeparamref name="TInstance"/> if this method returned <c>true</c>, or default value of <typeparamref name="TInstance"/> if this method returned <c>false</c>.</param>
      /// <returns><c>true</c> if an instance was acquired successfully; <c>false</c> otherwise.</returns>
      public Boolean TryTake( out TInstance item )
      {
         var result = this._pool.TakeInstance();
         var retVal = result != null;
         item = retVal ? result.Instance : default( TInstance );
         return retVal;
      }

      /// <summary>
      /// Attemts to fetch an instance from this <see cref="LocklessInstancePoolGeneric{TInstance}"/> without removing it.
      /// </summary>
      /// <param name="item">When this method returns, this will contain instance of <typeparamref name="TInstance"/> if this method returned <c>true</c>, or default value of <typeparamref name="TInstance"/> if this method returned <c>false</c>.</param>
      /// <returns><c>true</c> if an instance was fetched successfully; <c>false</c> otherwise.</returns>
      public Boolean TryPeek( out TInstance item )
      {
         var retVal = this.TryTake( out item );
         if ( retVal )
         {
            this.ReturnInstance( item );
         }
         return retVal;
      }

      /// <summary>
      /// Returns an existing instance to this <see cref="LocklessInstancePoolGeneric{TInstance}"/>.
      /// </summary>
      /// <param name="item">The instance to return.</param>
      public void ReturnInstance( TInstance item )
      {
         this._pool.ReturnInstance( new InstanceHolder<TInstance>( item ) );
      }
   }

   /// <summary>
   /// This class is generic instance holder, suitable to use with <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/>.
   /// </summary>
   /// <typeparam name="TInstance">The type of actual instance.</typeparam>
   public sealed class InstanceHolder<TInstance> : InstanceWithNextInfo<InstanceHolder<TInstance>>
   {
      private readonly TInstance _instance;
      private InstanceHolder<TInstance> _next;

      /// <summary>
      /// Creates new instance of <see cref="InstanceHolder{TInstance}"/>.
      /// </summary>
      /// <param name="instance">The actual instance.</param>
      public InstanceHolder( TInstance instance )
      {
         this._instance = instance;
      }

      /// <summary>
      /// Gets the current instance.
      /// </summary>
      /// <value>The current instance.</value>
      public TInstance Instance
      {
         get
         {
            return this._instance;
         }
      }

      /// <summary>
      /// Gets or sets the next <see cref="InstanceHolder{TInstance}"/> in the instance holder chain.
      /// This should never be used directly - instead let <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/> manage this.
      /// </summary>
      /// <value>The next <see cref="InstanceHolder{TInstance}"/> in the instance holder chain.</value>
      public InstanceHolder<TInstance> Next
      {
         get
         {
            return this._next;
         }
         set
         {
            Interlocked.Exchange( ref this._next, value );
         }
      }
   }

   /// <summary>
   /// This class acts as instance pool for types, which can hold their 'next' value.
   /// </summary>
   /// <typeparam name="TInstance">The type of instances.</typeparam>
   public sealed class LocklessInstancePoolForClassesNoHeapAllocations<TInstance> : LocklessInstancePoolForClasses<TInstance>
      where TInstance : class, InstanceWithNextInfo<TInstance>
   {
      private TInstance _firstInstance;

      /// <summary>
      /// Creates a new instance of <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/>
      /// </summary>
      public LocklessInstancePoolForClassesNoHeapAllocations()
      {
         this._firstInstance = null;
      }

      /// <inheritdoc />
      public TInstance TakeInstance()
      {
         TInstance result;
         do
         {
            result = this._firstInstance;
         } while ( result != null && !ReferenceEquals( result, Interlocked.CompareExchange( ref this._firstInstance, result.Next, result ) ) );

         return result;
      }

      /// <inheritdoc />
      public void ReturnInstance( TInstance instance )
      {
         if ( instance != null )
         {
            TInstance first;
            do
            {
               first = this._firstInstance;
               instance.Next = first;
            } while ( !ReferenceEquals( first, Interlocked.CompareExchange( ref this._firstInstance, instance, first ) ) );
         }
      }
   }

   /// <summary>
   /// This class wraps another <see cref="LocklessInstancePoolForClasses{TInstance}"/> to provide easy-to-use API (with <c>using</c> keyword) for renting instances from the instance pool.
   /// The requirement is that it should be able to create a new instance of <typeparamref name="TInstance"/> without parameters.
   /// </summary>
   /// <typeparam name="TInstance">The type of instances to rent from the pool.</typeparam>
   /// <seealso cref="UseInstance"/>
   public sealed class InstancePoolForContextlessCreation<TInstance> : LocklessInstancePoolForClasses<TInstance>
      where TInstance : class
   {
      private readonly LocklessInstancePoolForClasses<TInstance> _pool;
      private readonly Func<TInstance> _factory;

      /// <summary>
      /// Creates a new instance of <see cref="InstancePoolForContextlessCreation{TInstance}"/>.
      /// </summary>
      /// <param name="pool">The pool to wrap.</param>
      /// <param name="factory">The callback to use to create a new instance of type <typeparamref name="TInstance"/>.</param>
      /// <exception cref="ArgumentNullException">If either or <paramref name="pool"/> or <paramref name="factory"/> is <c>null</c>.</exception>
      public InstancePoolForContextlessCreation(
         LocklessInstancePoolForClasses<TInstance> pool,
         Func<TInstance> factory
         )
      {
         this._pool = ArgumentValidator.ValidateNotNull( nameof( pool ), pool );
         this._factory = ArgumentValidator.ValidateNotNull( nameof( factory ), factory );
      }

      void LocklessInstancePoolForClasses<TInstance>.ReturnInstance( TInstance instance )
      {
         this._pool.ReturnInstance( instance );
      }

      TInstance LocklessInstancePoolForClasses<TInstance>.TakeInstance()
      {
         return this._pool.TakeInstance();
      }

      /// <summary>
      /// Returns <see cref="InstanceUsage{TInstance}"/> to use the instance of type <typeparamref name="TInstance"/>.
      /// </summary>
      /// <returns>A <see cref="InstanceUsage{TInstance}"/> to be used in <c>using</c> statement.</returns>
      public InstanceUsage<TInstance> UseInstance()
      {
         return new InstanceUsage<TInstance>( this._pool, this._factory );
      }

   }

   /// <summary>
   /// This struct helps to capture the span of code using instance rented from <see cref="LocklessInstancePoolForClasses{TInstance}"/>.
   /// One should use <c>using</c> statement with this struct.
   /// </summary>
   /// <typeparam name="TInstance">The type of instances to rent from <see cref="LocklessInstancePoolForClasses{TInstance}"/>.</typeparam>
   public struct InstanceUsage<TInstance> : IDisposable
      where TInstance : class
   {
      private readonly LocklessInstancePoolForClasses<TInstance> _pool;

      /// <summary>
      /// Creates a new instance of <see cref="InstanceUsage{TInstance}"/>.
      /// </summary>
      /// <param name="pool">The <see cref="LocklessInstancePoolForClasses{TInstance}"/>.</param>
      /// <param name="factory">The callback to create a new instance of <typeparamref name="TInstance"/>, if the pool currently does not have instances available.</param>
      /// <exception cref="ArgumentNullException">If either of <paramref name="pool"/> or <paramref name="factory"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">If <paramref name="factory"/> is used and it returns <c>null</c>.</exception>
      public InstanceUsage(
         LocklessInstancePoolForClasses<TInstance> pool,
         Func<TInstance> factory
         )
      {
         this._pool = ArgumentValidator.ValidateNotNull( nameof( pool ), pool );
         this.Instance = ( pool.TakeInstance() ?? ArgumentValidator.ValidateNotNull( nameof( factory ), factory )() ) ?? throw new ArgumentException( "instance" );
      }

      /// <summary>
      /// Gets the instance being rented from <see cref="LocklessInstancePoolForClasses{TInstance}"/>.
      /// </summary>
      public TInstance Instance { get; }

      /// <summary>
      /// Returns the current 
      /// </summary>
      public void Dispose()
      {
         this._pool.ReturnInstance( this.Instance );
      }
   }

   /// <summary>
   /// This inteface captures constraints required for <see cref="LocklessInstancePoolForClassesNoHeapAllocations{TInstance}"/>
   /// </summary>
   /// <typeparam name="TInstance">The type of instance.</typeparam>
   public interface InstanceWithNextInfo<TInstance>
   {
      /// <summary>
      /// Gets or sets the instance next in chain.
      /// </summary>
      /// <value>The instance next in chain.</value>
      TInstance Next { get; set; }
   }
}
