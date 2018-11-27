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
using System.Threading;

namespace UtilPack
{
   /// <summary>
   /// This enumeration will tell what type the <see cref="IAbstractLazy{T}"/> actually is.
   /// </summary>
   public enum LazyKind
   {
      /// <summary>
      /// The lazy is of type <see cref="ReadOnlyLazy{T}"/>.
      /// </summary>
      ReadOnly,
      /// <summary>
      /// The lazy is of type <see cref="ReadOnlyResettableLazy{T}"/>.
      /// </summary>
      ReadOnlyResettable,
      /// <summary>
      /// The lazy is of type <see cref="WriteableLazy{T}"/>.
      /// </summary>
      Writeable,
      /// <summary>
      /// The lazy is of type <see cref="WriteableResettableLazy{T}"/>.
      /// </summary>
      WriteableResettable
   }

   /// <summary>
   /// This enumeration describes whether the lazy object can be written.
   /// </summary>
   public enum LazyWriteabilityKind
   {
      /// <summary>
      /// The lazy value can not be overwritten.
      /// </summary>
      ReadOnly,
      /// <summary>
      /// The lazy value can be overwritten.
      /// </summary>
      Writeable
   }

   /// <summary>
   /// This enumeration describes whether the lazy object can be re-set to its original value.
   /// </summary>
   public enum LazyResettabilityKind
   {
      /// <summary>
      /// The lazy value can not be re-set.
      /// </summary>
      NotResettable,
      /// <summary>
      /// The lazy value can be re-set.
      /// </summary>
      Resettable
   }

   /// <summary>
   /// This is common interface for all lazy types in this library.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   /// <seealso cref="IReadOnlyLazy{T}"/>
   /// <seealso cref="IResettableLazy{T}"/>
   /// <seealso cref="IWriteableLazy{T}"/>
   /// <seealso cref="ReadOnlyLazy{T}"/>
   /// <seealso cref="ReadOnlyResettableLazy{T}"/>
   /// <seealso cref="WriteableLazy{T}"/>
   /// <seealso cref="WriteableResettableLazy{T}"/>
   public interface IAbstractLazy<out T>
   {
      /// <summary>
      /// Lazily initializes the value, if required, and returns it.
      /// </summary>
      T Value { get; }

      /// <summary>
      /// Checks whether the value has been initialized.
      /// </summary>
      /// <value><c>true</c>, if value has been initialized; <c>false</c> otherwise.</value>
      Boolean IsValueCreated { get; }

      /// <summary>
      /// Gets the <see cref="UtilPack.LazyKind"/> enumeration telling which lazy type this object really is.
      /// </summary>
      LazyKind LazyKind { get; }

      /// <summary>
      /// Gets the <see cref="LazyWriteabilityKind"/> of this lazy.
      /// </summary>
      /// <value>The <see cref="LazyWriteabilityKind"/> of this lazy.</value>
      LazyWriteabilityKind WriteabilityKind { get; }

      /// <summary>
      /// Gets the <see cref="LazyResettabilityKind"/> of this lazy.
      /// </summary>
      /// <value>The <see cref="LazyResettabilityKind"/> of this lazy.</value>
      LazyResettabilityKind ResettabilityKind { get; }
   }

   /// <summary>
   /// This is interface implemented by read-only lazies: <see cref="ReadOnlyLazy{T}"/> and <see cref="ReadOnlyResettableLazy{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   public interface IReadOnlyLazy<out T> : IAbstractLazy<T>
   {

   }

   /// <summary>
   /// This is interface implemented by lazies which provide reset-functionality: <see cref="ReadOnlyResettableLazy{T}"/> and <see cref="WriteableResettableLazy{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   public interface IResettableLazy<out T> : IAbstractLazy<T>
   {
      /// <summary>
      /// Resets the 
      /// </summary>
      void Reset();
   }

   /// <summary>
   /// This is interface implemented by lazies which provide write-functionality: <see cref="WriteableLazy{T}"/> and <see cref="WriteableResettableLazy{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   public interface IWriteableLazy<T> : IAbstractLazy<T>
   {
      /// <summary>
      /// Gets or sets the value.
      /// </summary>
      /// <remarks>
      /// The setter can be used at any time, before or after calling getter.
      /// Once the setter has been called, the value returned by getter will be the one passed to setter.
      /// The lazy initializer will be called if getter is used before setter.
      /// </remarks>
      /// <value>
      /// The lazily initialized or manually set value.
      /// </value>
      new T Value { get; set; }
   }

   /// <summary>
   /// This is base class for all types providing lazy initialization functionality.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   /// <seealso cref="ReadOnlyLazy{T}"/>
   /// <seealso cref="ReadOnlyResettableLazy{T}"/>
   /// <seealso cref="WriteableLazy{T}"/>
   /// <seealso cref="WriteableResettableLazy{T}"/>
   public abstract class AbstractLazy<T>
   {
      internal AbstractLazy()
      {

      }

      /// <summary>
      /// Checks whether the value has been initialized.
      /// </summary>
      /// <value><c>true</c>, if value has been initialized; <c>false</c> otherwise.</value>
      public abstract Boolean IsValueCreated { get; }

      /// <summary>
      /// Gets the <see cref="UtilPack.LazyKind"/> enumeration telling which lazy type this object really is.
      /// </summary>
      public abstract LazyKind LazyKind { get; }

      /// <summary>
      /// Gets the <see cref="LazyWriteabilityKind"/> of this lazy.
      /// </summary>
      /// <value>The <see cref="LazyWriteabilityKind"/> of this lazy.</value>
      public abstract LazyWriteabilityKind WriteabilityKind { get; }

      /// <summary>
      /// Gets the <see cref="LazyResettabilityKind"/> of this lazy.
      /// </summary>
      /// <value>The <see cref="LazyResettabilityKind"/> of this lazy.</value>
      public abstract LazyResettabilityKind ResettabilityKind { get; }
   }


   /// <summary>
   /// This class implements the <see cref="IReadOnlyLazy{T}"/> functionality.
   /// In essence, it is identical to <see cref="Lazy{T}"/> class.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   public sealed class ReadOnlyLazy<T> : AbstractLazy<T>, IReadOnlyLazy<T>
   {
      private readonly Lazy<T> _lazy;

      internal ReadOnlyLazy( Func<T> factory, LazyThreadSafetyMode threadSafety )
      {
         this._lazy = new Lazy<T>( factory, threadSafety );
      }

      /// <inheritdoc />
      public T Value
      {
         get
         {
            return this._lazy.Value;
         }
      }

      /// <inheritdoc />
      public override Boolean IsValueCreated
      {
         get
         {
            return this._lazy.IsValueCreated;
         }
      }

      /// <inheritdoc />
      public override LazyKind LazyKind
      {
         get
         {
            return LazyKind.ReadOnly;
         }
      }

      /// <inheritdoc />
      public override LazyWriteabilityKind WriteabilityKind
      {
         get
         {
            return LazyWriteabilityKind.ReadOnly;
         }
      }

      /// <inheritdoc />
      public override LazyResettabilityKind ResettabilityKind
      {
         get
         {
            return LazyResettabilityKind.NotResettable;
         }
      }

   }

   /// <summary>
   /// This class provides some common functionality for <see cref="ReadOnlyResettableLazy{T}"/>, <see cref="WriteableLazy{T}"/>, and <see cref="WriteableResettableLazy{T}"/> classes.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   public abstract class LazyWithSetter<T> : AbstractLazy<T>
   {
      private readonly LazyThreadSafetyMode _threadSafety;
      private Lazy<T> _lazy;

      internal LazyWithSetter( Func<T> factory, LazyThreadSafetyMode threadSafety )
      {
         this._lazy = new Lazy<T>( factory, threadSafety );
         this._threadSafety = threadSafety;
      }

      internal Lazy<T> Lazy
      {
         get
         {
            return this._lazy;
         }
         set
         {
            if ( this._threadSafety == LazyThreadSafetyMode.None )
            {
               this._lazy = value;
            }
            else
            {
               Interlocked.Exchange( ref this._lazy, value );
            }
         }
      }

      /// <inheritdoc />
      public override Boolean IsValueCreated
      {
         get
         {
            return this._lazy.IsValueCreated;
         }
      }

      /// <inheritdoc />
      protected LazyThreadSafetyMode LazyThreadSafety
      {
         get
         {
            return this._threadSafety;
         }
      }
   }

   /// <summary>
   /// This class provides functionality of read-only lazily initializable value, with a option to reset the value to its initial state.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   /// <seealso cref="IReadOnlyLazy{T}"/>
   /// <seealso cref="IResettableLazy{T}"/>
   public sealed class ReadOnlyResettableLazy<T> : LazyWithSetter<T>, IReadOnlyLazy<T>, IResettableLazy<T>
   {
      private readonly Func<T> _factory;

      internal ReadOnlyResettableLazy( Func<T> factory, LazyThreadSafetyMode threadSafety )
         : base( factory, threadSafety )
      {
         this._factory = factory;
      }

      /// <inheritdoc />
      public void Reset()
      {
         var lazy = this.Lazy;
         if ( lazy.IsValueCreated )
         {
            this.Lazy = new Lazy<T>( this._factory, this.LazyThreadSafety );
         }
      }

      /// <inheritdoc />
      public T Value
      {
         get
         {
            return this.Lazy.Value;
         }
      }

      /// <inheritdoc />
      public override LazyKind LazyKind
      {
         get
         {
            return LazyKind.ReadOnlyResettable;
         }
      }

      /// <inheritdoc />
      public override LazyWriteabilityKind WriteabilityKind
      {
         get
         {
            return LazyWriteabilityKind.ReadOnly;
         }
      }

      /// <inheritdoc />
      public override LazyResettabilityKind ResettabilityKind
      {
         get
         {
            return LazyResettabilityKind.Resettable;
         }
      }

   }

   /// <summary>
   /// This class provides some common functionality for <see cref="WriteableLazy{T}"/> and <see cref="WriteableResettableLazy{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   public abstract class AbstractWriteableLazy<T> : LazyWithSetter<T>, IWriteableLazy<T>
   {
      private Object _setValue;

      internal AbstractWriteableLazy( Func<T> factory, LazyThreadSafetyMode threadSafety )
         : base( factory, threadSafety )
      {

      }

      /// <inheritdoc />
      public T Value
      {
         get
         {
            var lazy = this.Lazy; // Read field only once in case it changes after check and 2nd reading
            return lazy == null ? (T) this._setValue : lazy.Value;
         }
         set
         {
            Interlocked.Exchange( ref this._setValue, value );
            this.Lazy = null;
         }
      }

      /// <inheritdoc />
      public override Boolean IsValueCreated
      {
         get
         {
            var lazy = this.Lazy;
            return lazy == null || lazy.IsValueCreated;
         }
      }
   }

   /// <summary>
   /// This class provides functionality of writeable lazy, where the lazy value may be manually re-specified or overwritten.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   public sealed class WriteableLazy<T> : AbstractWriteableLazy<T>
   {
      internal WriteableLazy( Func<T> factory, LazyThreadSafetyMode threadSafety )
         : base( factory, threadSafety )
      {

      }

      /// <inheritdoc />
      public override LazyKind LazyKind
      {
         get
         {
            return LazyKind.Writeable;
         }
      }

      /// <inheritdoc />
      public override LazyWriteabilityKind WriteabilityKind
      {
         get
         {
            return LazyWriteabilityKind.Writeable;
         }
      }

      /// <inheritdoc />
      public override LazyResettabilityKind ResettabilityKind
      {
         get
         {
            return LazyResettabilityKind.NotResettable;
         }
      }
   }

   /// <summary>
   /// This class provides functionality of writeable lazy value, where the lazy value may be manually re-specified or overwritten, and with an option to reset the lazy value to its initial state.
   /// </summary>
   /// <typeparam name="T">The type of lazily initialized object.</typeparam>
   /// <seealso cref="IWriteableLazy{T}"/>
   /// <seealso cref="IResettableLazy{T}"/>
   public sealed class WriteableResettableLazy<T> : AbstractWriteableLazy<T>, IResettableLazy<T>
   {
      private Func<T> _factory;

      internal WriteableResettableLazy( Func<T> factory, LazyThreadSafetyMode threadSafety )
         : base( factory, threadSafety )
      {
         this._factory = factory;
      }

      /// <summary>
      /// Resets the current value to the one provided by lazy value factory, but only if the current value has not been set through <see cref="AbstractWriteableLazy{T}.Value"/> setter.
      /// </summary>
      public void Reset()
      {
         this.Reset( false );
      }

      /// <summary>
      /// Resets the current value to the one provided by lazy value factory.
      /// </summary>
      /// <param name="resetEvenIfManuallySet">If this is <c>true</c>, then the value will be reset even if current value has been set through <see cref="AbstractWriteableLazy{T}.Value"/> setter.</param>
      public void Reset( Boolean resetEvenIfManuallySet )
      {
         var lazy = this.Lazy;
         if ( resetEvenIfManuallySet || ( lazy != null && lazy.IsValueCreated ) )
         {
            this.Lazy = new Lazy<T>( this._factory, this.LazyThreadSafety );
         }
      }

      /// <inheritdoc />
      public override LazyKind LazyKind
      {
         get
         {
            return LazyKind.WriteableResettable;
         }
      }

      /// <inheritdoc />
      public override LazyWriteabilityKind WriteabilityKind
      {
         get
         {
            return LazyWriteabilityKind.Writeable;
         }
      }

      /// <inheritdoc />
      public override LazyResettabilityKind ResettabilityKind
      {
         get
         {
            return LazyResettabilityKind.Resettable;
         }
      }
   }

   /// <summary>
   /// This is factory class to create various lazily initializable objects provided by this library.
   /// </summary>
   /// <seealso cref="IAbstractLazy{T}"/>
   /// <seealso cref="IReadOnlyLazy{T}"/>
   /// <seealso cref="IResettableLazy{T}"/>
   /// <seealso cref="IWriteableLazy{T}"/>
   /// <seealso cref="ReadOnlyLazy{T}"/>
   /// <seealso cref="ReadOnlyResettableLazy{T}"/>
   /// <seealso cref="WriteableLazy{T}"/>
   /// <seealso cref="WriteableResettableLazy{T}"/>
   public static class LazyFactory
   {
      /// <summary>
      /// Creates a new lazy, type and behaviour of which will be decided by given <see cref="LazyKind"/> enumeration.
      /// </summary>
      /// <typeparam name="T">The type of lazily initialized object.</typeparam>
      /// <param name="kind">The <see cref="LazyKind"/>.</param>
      /// <param name="valueFactory">The value factory, may be <c>null</c>.</param>
      /// <param name="threadSafety">The <see cref="LazyThreadSafetyMode"/>.</param>
      /// <returns>A lazily initializing object, of type 
      /// <list type="bullet">
      /// <item>
      /// <description><see cref="ReadOnlyLazy{T}"/>, if <paramref name="kind"/> is <see cref="LazyKind.ReadOnly"/>,</description>
      /// </item>
      /// <item>
      /// <description><see cref="ReadOnlyResettableLazy{T}"/>, if <paramref name="kind"/> is <see cref="LazyKind.ReadOnlyResettable"/>,</description>
      /// </item>
      /// <item>
      /// <description><see cref="WriteableLazy{T}"/>, if <paramref name="kind"/> is <see cref="LazyKind.Writeable"/>, or</description>
      /// </item>
      /// <item>
      /// <description><see cref="WriteableResettableLazy{T}"/>, if <paramref name="kind"/> is <see cref="LazyKind.WriteableResettable"/>.</description>
      /// </item>
      /// </list>
      /// </returns>
      /// <exception cref="ArgumentException">If <paramref name="kind"/> is not one of the values specified in <see cref="LazyKind"/> enumeration.</exception>
      public static IAbstractLazy<T> NewLazy<T>( LazyKind kind, Func<T> valueFactory, LazyThreadSafetyMode threadSafety )
      {
         switch ( kind )
         {
            case LazyKind.ReadOnly:
               return NewReadOnlyLazy( valueFactory, threadSafety );
            case LazyKind.ReadOnlyResettable:
               return NewReadOnlyResettableLazy( valueFactory, threadSafety );
            case LazyKind.Writeable:
               return NewWriteableLazy( valueFactory, threadSafety );
            case LazyKind.WriteableResettable:
               return NewWriteableResettableLazy( valueFactory, threadSafety );
            default:
               throw new ArgumentException( "Invalid lazy kind: " + kind + "." );
         }
      }

      /// <summary>
      /// Creates a new instance of <see cref="ReadOnlyLazy{T}"/> with given value factory and thread safety.
      /// </summary>
      /// <typeparam name="T">The type of lazily initialized object.</typeparam>
      /// <param name="valueFactory">The value factory, may be <c>null</c>.</param>
      /// <param name="threadSafety">The <see cref="LazyThreadSafetyMode"/>.</param>
      /// <returns>A new instance of <see cref="ReadOnlyLazy{T}"/> with given value factory and thread safety.</returns>
      public static ReadOnlyLazy<T> NewReadOnlyLazy<T>( Func<T> valueFactory, LazyThreadSafetyMode threadSafety )
      {
         return new ReadOnlyLazy<T>( valueFactory, threadSafety );
      }

      /// <summary>
      /// Creates a new instance of <see cref="ReadOnlyResettableLazy{T}"/> with given value factory and thread safety.
      /// </summary>
      /// <typeparam name="T">The type of lazily initialized object.</typeparam>
      /// <param name="valueFactory">The value factory. May be <c>null</c>.</param>
      /// <param name="threadSafety">The <see cref="LazyThreadSafetyMode"/>.</param>
      /// <returns>A new instance of <see cref="ReadOnlyResettableLazy{T}"/> with given value factory and thread safety.</returns>
      public static ReadOnlyResettableLazy<T> NewReadOnlyResettableLazy<T>( Func<T> valueFactory, LazyThreadSafetyMode threadSafety )
      {
         return new ReadOnlyResettableLazy<T>( valueFactory, threadSafety );
      }

      /// <summary>
      /// Creates a new instance of <see cref="WriteableLazy{T}"/> with given value factory and thread safety.
      /// </summary>
      /// <typeparam name="T">The type of lazily initialized object.</typeparam>
      /// <param name="valueFactory">The value factory. May be <c>null</c>.</param>
      /// <param name="threadSafety">The <see cref="LazyThreadSafetyMode"/>.</param>
      /// <returns>A new instance of <see cref="WriteableLazy{T}"/> with given value factory and thread safety.</returns>
      public static WriteableLazy<T> NewWriteableLazy<T>( Func<T> valueFactory, LazyThreadSafetyMode threadSafety )
      {
         return new WriteableLazy<T>( valueFactory, threadSafety );
      }

      /// <summary>
      /// Creates a new instance of <see cref="WriteableResettableLazy{T}"/> with given value factory and thread safety.
      /// </summary>
      /// <typeparam name="T">The type of lazily initialized object.</typeparam>
      /// <param name="valueFactory">The value factory. May be <c>null</c>.</param>
      /// <param name="threadSafety">The <see cref="LazyThreadSafetyMode"/>.</param>
      /// <returns>A new instance of <see cref="WriteableResettableLazy{T}"/> with given value factory and thread safety.</returns>
      public static WriteableResettableLazy<T> NewWriteableResettableLazy<T>( Func<T> valueFactory, LazyThreadSafetyMode threadSafety )
      {
         return new WriteableResettableLazy<T>( valueFactory, threadSafety );
      }

      /// <summary>
      /// Creates a new lazy, which will be <see cref="WriteableResettableLazy{T}"/> or <see cref="ReadOnlyResettableLazy{T}"/>, depending on <paramref name="isWriteable"/> parameter.
      /// </summary>
      /// <typeparam name="T">The type of lazily initialized object.</typeparam>
      /// <param name="isWriteable">Whether to create writeable resettable lazy.</param>
      /// <param name="valueFactory">The value factory. May be <c>null</c>.</param>
      /// <param name="threadSafety">The <see cref="LazyThreadSafetyMode"/>.</param>
      /// <returns>A new instance of <see cref="WriteableResettableLazy{T}"/> if <paramref name="isWriteable"/> is <c>true</c>; a new instance of <see cref="ReadOnlyResettableLazy{T}"/> otherwise.</returns>
      public static IResettableLazy<T> NewResettableLazy<T>( Boolean isWriteable, Func<T> valueFactory, LazyThreadSafetyMode threadSafety )
      {
         return isWriteable ? (IResettableLazy<T>) NewWriteableResettableLazy( valueFactory, threadSafety ) : NewReadOnlyResettableLazy( valueFactory, threadSafety );
      }

      /// <summary>
      /// Creates a new lazy, which will be <see cref="WriteableResettableLazy{T}"/> or <see cref="WriteableLazy{T}"/>, depending on <paramref name="isResettable"/> parameter.
      /// </summary>
      /// <typeparam name="T">The type of lazily initialized object.</typeparam>
      /// <param name="isResettable">Whether to create writeable resettable lazy.</param>
      /// <param name="valueFactory">The value factory. May be <c>null</c>.</param>
      /// <param name="threadSafety">The <see cref="LazyThreadSafetyMode"/>.</param>
      /// <returns>A new instance of <see cref="WriteableResettableLazy{T}"/> if <paramref name="isResettable"/> is <c>true</c>; a new instance of <see cref="WriteableLazy{T}"/> otherwise.</returns>
      public static IWriteableLazy<T> NewWriteableLazy<T>( Boolean isResettable, Func<T> valueFactory, LazyThreadSafetyMode threadSafety )
      {
         return isResettable ? (IWriteableLazy<T>) NewWriteableResettableLazy( valueFactory, threadSafety ) : NewWriteableLazy( valueFactory, threadSafety );
      }

      /// <summary>
      /// Creates a new lazy, which will be <see cref="ReadOnlyResettableLazy{T}"/> or <see cref="ReadOnlyLazy{T}"/>, depending on <paramref name="isResettable"/> parameter.
      /// </summary>
      /// <typeparam name="T">The type of lazily initialized object.</typeparam>
      /// <param name="isResettable">Whether to create writeable resettable lazy.</param>
      /// <param name="valueFactory">The value factory. May be <c>null</c>.</param>
      /// <param name="threadSafety">The <see cref="LazyThreadSafetyMode"/>.</param>
      /// <returns>A new instance of <see cref="ReadOnlyResettableLazy{T}"/> if <paramref name="isResettable"/> is <c>true</c>; a new instance of <see cref="ReadOnlyLazy{T}"/> otherwise.</returns>
      public static IReadOnlyLazy<T> NewReadOnlyLazy<T>( Boolean isResettable, Func<T> valueFactory, LazyThreadSafetyMode threadSafety )
      {
         return isResettable ? (IReadOnlyLazy<T>) NewReadOnlyResettableLazy( valueFactory, threadSafety ) : NewReadOnlyLazy( valueFactory, threadSafety );
      }

      /// <summary>
      /// Creates a new resettable lazy, which will re-set the lazy value if an exception is thrown from value factory.
      /// </summary>
      /// <typeparam name="TLazy">The type of the lazy.</typeparam>
      /// <typeparam name="TValue">The type of the value.</typeparam>
      /// <param name="lazy">The reference to lazy variable.</param>
      /// <param name="valueFactory">The value factory.</param>
      /// <param name="threadSafety">The <see cref="LazyThreadSafetyMode"/>.</param>
      /// <param name="writeability">The writeability of the lazy to create, <see cref="LazyWriteabilityKind.ReadOnly"/> for <see cref="ReadOnlyResettableLazy{T}"/>, and <see cref="LazyWriteabilityKind.Writeable"/> for <see cref="WriteableResettableLazy{T}"/>.</param>
      /// <exception cref="InvalidCastException">If <typeparamref name="TLazy"/> and <paramref name="writeability"/> do not match.</exception>
      public static void NewResettableOnErrorLazy<TLazy, TValue>( out TLazy lazy, Func<TValue> valueFactory, LazyThreadSafetyMode threadSafety, LazyWriteabilityKind writeability )
         where TLazy : class, IResettableLazy<TValue>
      {
         TLazy theLazy = null;
         Func<TValue> newValueFactory = () =>
         {
            try
            {
               return valueFactory();
            }
            catch
            {
               theLazy.Reset();
               throw;
            }
         };
         switch ( writeability )
         {
            case LazyWriteabilityKind.ReadOnly:
               theLazy = (TLazy) (Object) NewReadOnlyResettableLazy( newValueFactory, threadSafety );
               break;
            case LazyWriteabilityKind.Writeable:
               theLazy = (TLazy) (Object) NewWriteableResettableLazy( newValueFactory, threadSafety );
               break;
            default:
               throw new ArgumentException( "Invalid writeability kind: " + writeability + "." );
         }

         lazy = theLazy;
      }
   }
}
