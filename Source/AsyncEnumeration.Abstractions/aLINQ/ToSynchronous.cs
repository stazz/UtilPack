/*
 * Copyright 2017 Stanislav Muhametsin. All rights Reserved.
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
using AsyncEnumeration.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

#if !NETSTANDARD1_0
using System.Collections.Concurrent;
#endif

public static partial class E_AsyncEnumeration
{
   /// <summary>
   /// General-purpose extension method to add all items of this <see cref="IAsyncEnumerable{T}"/> to given collection.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TCollection">The type of collection to add items to.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="collection">The collection to add to.</param>
   /// <param name="addItem">The callback to add the to the <paramref name="collection"/>.</param>
   /// <returns>Potentially asynchronously returns the amount of items encountered.</returns>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateAsync{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="addItem"/> is <c>null</c>.</exception>
   public static ValueTask<Int64> AddToCollectionAsync<T, TCollection>( this IAsyncEnumerable<T> enumerable, TCollection collection, Action<TCollection, T> addItem )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( addItem ), addItem );

      return enumerable.EnumerateAsync( item => addItem( collection, item ) );
   }

   /// <summary>
   /// General-purpose extension method to add all items of this <see cref="IAsyncEnumerable{T}"/> to given collection.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TCollection">The type of collection to add items to.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="collection">The collection to add to.</param>
   /// <param name="addItem">The callback to add the to the <paramref name="collection"/>.</param>
   /// <returns>Potentially asynchronously returns the amount of items encountered.</returns>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateAsync{T}(IAsyncEnumerable{T}, Func{T, Task})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="addItem"/> is <c>null</c>.</exception>
   public static ValueTask<Int64> AddToCollectionAsync<T, TCollection>( this IAsyncEnumerable<T> enumerable, TCollection collection, Func<TCollection, T, Task> addItem )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( addItem ), addItem );

      return enumerable.EnumerateAsync( item => { return addItem( collection, item ); } );
   }

   /// <summary>
   /// This extension method will enumerate this <see cref="IAsyncEnumerable{T}"/> into an array.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>An array of enumerated items.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateAsync{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   public static async Task<T[]> ToArrayAsync<T>( this IAsyncEnumerable<T> enumerable )
      => ( await enumerable.ToListAsync() ).ToArray();

   /// <summary>
   /// This extension method will enumerate this <see cref="IAsyncEnumerable{T}"/> into a <see cref="List{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>A <see cref="List{T}"/> of enumerated items.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateAsync{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   public static async Task<List<T>> ToListAsync<T>( this IAsyncEnumerable<T> enumerable )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      var retVal = new List<T>();
      await enumerable.AddToCollectionAsync( retVal, ( list, item ) => list.Add( item ) );
      return retVal;
   }

   /// <summary>
   /// This extension method will enumerate this <see cref="IAsyncEnumerable{T}"/> into a <see cref="IDictionary{TKey, TValue}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
   /// <typeparam name="TValue">The type of dictionary values.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="keySelector">The callback to create a dictionary key from enumerable item.</param>
   /// <param name="valueSelector">The callback to create a dictionary value from enumerable item.</param>
   /// <param name="equalityComparer">The optional <see cref="IEqualityComparer{T}"/> to use when creating dictionary.</param>
   /// <returns>Asynchronously returns a <see cref="IDictionary{TKey, TValue}"/> containing keys and values as returned by <paramref name="keySelector"/> and <paramref name="valueSelector"/>.</returns>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateAsync{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="keySelector"/> or <paramref name="valueSelector"/> is <c>null</c>.</exception>
   public static async Task<IDictionary<TKey, TValue>> ToDictionaryAsync<T, TKey, TValue>(
      this IAsyncEnumerable<T> enumerable,
      Func<T, TKey> keySelector,
      Func<T, TValue> valueSelector,
      IEqualityComparer<TKey> equalityComparer = null
      )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( keySelector ), keySelector );
      ArgumentValidator.ValidateNotNull( nameof( valueSelector ), valueSelector );

      var retVal = new Dictionary<TKey, TValue>( equalityComparer );
      await enumerable.AddToCollectionAsync( retVal, ( dictionary, item ) => dictionary.Add( keySelector( item ), valueSelector( item ) ) );
      return retVal;
   }

   /// <summary>
   /// This extension method will enumerate this <see cref="IAsyncEnumerable{T}"/> into a <see cref="IDictionary{TKey, TValue}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
   /// <typeparam name="TValue">The type of dictionary values.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="keySelector">The callback to potentially asynchronously create a dictionary key from enumerable item.</param>
   /// <param name="valueSelector">The callback to potentially asynchronously create a dictionary value from enumerable item.</param>
   /// <param name="equalityComparer">The optional <see cref="IEqualityComparer{T}"/> to use when creating dictionary.</param>
   /// <returns>Asynchronously returns a <see cref="IDictionary{TKey, TValue}"/> containing keys and values as returned by <paramref name="keySelector"/> and <paramref name="valueSelector"/>.</returns>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateAsync{T}(IAsyncEnumerable{T}, Func{T, Task})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="keySelector"/> or <paramref name="valueSelector"/> is <c>null</c>.</exception>
   public static async Task<IDictionary<TKey, TValue>> ToDictionaryAsync<T, TKey, TValue>(
      this IAsyncEnumerable<T> enumerable,
      Func<T, ValueTask<TKey>> keySelector,
      Func<T, ValueTask<TValue>> valueSelector,
      IEqualityComparer<TKey> equalityComparer = null
      )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( keySelector ), keySelector );
      ArgumentValidator.ValidateNotNull( nameof( valueSelector ), valueSelector );

      var retVal = new Dictionary<TKey, TValue>( equalityComparer );
      await enumerable.AddToCollectionAsync( retVal, async ( dictionary, item ) => dictionary.Add( await keySelector( item ), await valueSelector( item ) ) );
      return retVal;
   }

   /// <summary>
   /// General-purpose extension method to add all items of this <see cref="IAsyncEnumerable{T}"/> to given concurrent collection.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TCollection">The type of collection to add items to.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="collection">The collection to add to.</param>
   /// <param name="addItem">The callback to add the to the <paramref name="collection"/>. May be executed concurrently.</param>
   /// <returns>Potentially asynchronously returns the amount of items encountered.</returns>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="addItem"/> is <c>null</c>.</exception>
   public static ValueTask<Int64> AddToConcurrentCollectionAsync<T, TCollection>( this IAsyncEnumerable<T> enumerable, TCollection collection, Action<TCollection, T> addItem )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( addItem ), addItem );

      return enumerable.EnumerateAsync( item => addItem( collection, item ) );
   }

   /// <summary>
   /// General-purpose extension method to add all items of this <see cref="IAsyncEnumerable{T}"/> to given concurrent collection.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TCollection">The type of collection to add items to.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="collection">The collection to add to.</param>
   /// <param name="addItem">The callback to asynchronously add the to the <paramref name="collection"/>. May be executed concurrently.</param>
   /// <returns>Potentially asynchronously returns the amount of items encountered.</returns>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Func{T, Task})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="addItem"/> is <c>null</c>.</exception>
   public static ValueTask<Int64> AddToConcurrentCollectionAsync<T, TCollection>( this IAsyncEnumerable<T> enumerable, TCollection collection, Func<TCollection, T, Task> addItem )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( addItem ), addItem );

      return enumerable.EnumerateAsync( item => { return addItem( collection, item ); } );
   }

#if !NETSTANDARD1_0

   /// <summary>
   /// This extension method creates a new <see cref="ConcurrentBag{T}"/> and possibly concurrently enumerates this <see cref="IAsyncEnumerable{T}"/>, adding each encountered item to the <see cref="ConcurrentBag{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>A new <see cref="ConcurrentBag{T}"/> holding all items encountered while enumerating this <see cref="IAsyncEnumerable{T}"/>.</returns>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   public static async Task<ConcurrentBag<T>> ToConcurrentBagAsync<T>( this IAsyncEnumerable<T> enumerable )
   {
      var retVal = new ConcurrentBag<T>();
      await enumerable.AddToConcurrentCollectionAsync( retVal, ( bag, item ) => bag.Add( item ) );
      return retVal;
   }

   /// <summary>
   /// This extension method creates a new <see cref="ConcurrentQueue{T}"/> and possibly concurrently enumerates this <see cref="IAsyncEnumerable{T}"/>, adding each encountered item to the <see cref="ConcurrentQueue{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>A new <see cref="ConcurrentQueue{T}"/> holding all items encountered while enumerating this <see cref="IAsyncEnumerable{T}"/>.</returns>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   public static async Task<ConcurrentQueue<T>> ToConcurrentQueueAsync<T>( this IAsyncEnumerable<T> enumerable )
   {
      var retVal = new ConcurrentQueue<T>();
      await enumerable.AddToConcurrentCollectionAsync( retVal, ( queue, item ) => queue.Enqueue( item ) );
      return retVal;
   }

   /// <summary>
   /// This extension method creates a new <see cref="ConcurrentStack{T}"/> and possibly concurrently enumerates this <see cref="IAsyncEnumerable{T}"/>, adding each encountered item to the <see cref="ConcurrentStack{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <returns>A new <see cref="ConcurrentStack{T}"/> holding all items encountered while enumerating this <see cref="IAsyncEnumerable{T}"/>.</returns>
   /// <remarks>
   /// This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   public static async Task<ConcurrentStack<T>> ToConcurrentStackAsync<T>( this IAsyncEnumerable<T> enumerable )
   {
      var retVal = new ConcurrentStack<T>();
      await enumerable.AddToConcurrentCollectionAsync( retVal, ( stack, item ) => stack.Push( item ) );
      return retVal;
   }

   /// <summary>
   /// This extension method creates a new <see cref="ConcurrentBag{T}"/> and possibly concurrently enumerates this <see cref="IAsyncEnumerable{T}"/>, adding each encountered item transformed by given <paramref name="selector"/> to the <see cref="ConcurrentBag{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="U">The type of items added to <see cref="ConcurrentBag{T}"/>.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="selector">The callback to asynchronously select an object to be added to <see cref="ConcurrentBag{T}"/>.</param>
   /// <returns>A new <see cref="ConcurrentBag{T}"/> holding all transformed items encountered while enumerating this <see cref="IAsyncEnumerable{T}"/>.</returns>
   /// <remarks>
   /// <para>
   /// The motivation for this method is that often the items enumerated by <see cref="IAsyncEnumerable{T}"/> are "incomplete" in a sense that they require additional asynchronous processing (e.g. reading SQL row values, or reading the content of HTTP response).
   /// Using <see cref="Select{T, U}(IAsyncEnumerable{T}, Func{T, ValueTask{U}})"/> method will force the <see cref="IAsyncEnumerable{T}"/> into sequential enumerable, which may be undesired.
   /// Therefore, using this method directly it is possible to enumerate this <see cref="IAsyncEnumerable{T}"/> possibly concurrently into <see cref="ConcurrentBag{T}"/> while transforming each enumerable item into other type.
   /// </para>
   /// <para>
   /// This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </para>
   /// </remarks>
   public static async Task<ConcurrentBag<U>> ToConcurrentBagAsync<T, U>( this IAsyncEnumerable<T> enumerable, Func<T, Task<U>> selector )
   {
      ArgumentValidator.ValidateNotNull( nameof( selector ), selector );
      var retVal = new ConcurrentBag<U>();
      await enumerable.AddToConcurrentCollectionAsync( retVal, async ( bag, item ) => bag.Add( await selector( item ) ) );
      return retVal;
   }

   /// <summary>
   /// This extension method creates a new <see cref="ConcurrentQueue{T}"/> and possibly concurrently enumerates this <see cref="IAsyncEnumerable{T}"/>, adding each encountered item transformed by given <paramref name="selector"/> to the <see cref="ConcurrentQueue{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="U">The type of items added to <see cref="ConcurrentQueue{T}"/>.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="selector">The callback to asynchronously select an object to be added to <see cref="ConcurrentQueue{T}"/>.</param>
   /// <returns>A new <see cref="ConcurrentQueue{T}"/> holding all transformed items encountered while enumerating this <see cref="IAsyncEnumerable{T}"/>.</returns>
   /// <remarks>
   /// <para>
   /// The motivation for this method is that often the items enumerated by <see cref="IAsyncEnumerable{T}"/> are "incomplete" in a sense that they require additional asynchronous processing (e.g. reading SQL row values, or reading the content of HTTP response).
   /// Using <see cref="Select{T, U}(IAsyncEnumerable{T}, Func{T, ValueTask{U}})"/> method will force the <see cref="IAsyncEnumerable{T}"/> into sequential enumerable, which may be undesired.
   /// Therefore, using this method directly it is possible to enumerate this <see cref="IAsyncEnumerable{T}"/> possibly concurrently into <see cref="ConcurrentQueue{T}"/> while transforming each enumerable item into other type.
   /// </para>
   /// <para>
   /// This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </para>
   /// </remarks>
   public static async Task<ConcurrentQueue<U>> ToConcurrentQueueAsync<T, U>( this IAsyncEnumerable<T> enumerable, Func<T, Task<U>> selector )
   {
      ArgumentValidator.ValidateNotNull( nameof( selector ), selector );
      var retVal = new ConcurrentQueue<U>();
      await enumerable.AddToConcurrentCollectionAsync( retVal, async ( queue, item ) => queue.Enqueue( await selector( item ) ) );
      return retVal;
   }

   /// <summary>
   /// This extension method creates a new <see cref="ConcurrentStack{T}"/> and possibly concurrently enumerates this <see cref="IAsyncEnumerable{T}"/>, adding each encountered item transformed by given <paramref name="selector"/> to the <see cref="ConcurrentStack{T}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="U">The type of items added to <see cref="ConcurrentStack{T}"/>.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="selector">The callback to asynchronously select an object to be added to <see cref="ConcurrentStack{T}"/>.</param>
   /// <returns>A new <see cref="ConcurrentStack{T}"/> holding all transformed items encountered while enumerating this <see cref="IAsyncEnumerable{T}"/>.</returns>
   /// <remarks>
   /// <para>
   /// The motivation for this method is that often the items enumerated by <see cref="IAsyncEnumerable{T}"/> are "incomplete" in a sense that they require additional asynchronous processing (e.g. reading SQL row values, or reading the content of HTTP response).
   /// Using <see cref="Select{T, U}(IAsyncEnumerable{T}, Func{T, ValueTask{U}})"/> method will force the <see cref="IAsyncEnumerable{T}"/> into sequential enumerable, which may be undesired.
   /// Therefore, using this method directly it is possible to enumerate this <see cref="IAsyncEnumerable{T}"/> possibly concurrently into <see cref="ConcurrentStack{T}"/> while transforming each enumerable item into other type.
   /// </para>
   /// <para>
   /// This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </para>
   /// </remarks>
   public static async Task<ConcurrentStack<U>> ToConcurrentStackAsync<T, U>( this IAsyncEnumerable<T> enumerable, Func<T, Task<U>> selector )
   {
      ArgumentValidator.ValidateNotNull( nameof( selector ), selector );
      var retVal = new ConcurrentStack<U>();
      await enumerable.AddToConcurrentCollectionAsync( retVal, async ( stack, item ) => stack.Push( await selector( item ) ) );
      return retVal;
   }

   /// <summary>
   /// This extension method will possibly concurrently enumerate this <see cref="IAsyncEnumerable{T}"/> into a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
   /// <typeparam name="TValue">The type of dictionary values.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="keySelector">The callback to create a dictionary key from enumerable item.</param>
   /// <param name="valueSelector">The callback to create a dictionary value from enumerable item.</param>
   /// <param name="equalityComparer">The optional <see cref="IEqualityComparer{T}"/> to use when creating dictionary.</param>
   /// <returns>Asynchronously returns a <see cref="IDictionary{TKey, TValue}"/> containing keys and values as returned by <paramref name="keySelector"/> and <paramref name="valueSelector"/>.</returns>
   /// <remarks>
   /// <para>This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </para>
   /// <para>
   /// TODO currently this will not throw if there are duplicate keys, unlike <see cref="ToDictionaryAsync{T, TKey, TValue}(IAsyncEnumerable{T}, Func{T, TKey}, Func{T, TValue}, IEqualityComparer{TKey})"/> method.
   /// The behaviour needs to be unified/parametrized at some point.
   /// </para>
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="keySelector"/> or <paramref name="valueSelector"/> is <c>null</c>.</exception>
   public static async Task<ConcurrentDictionary<TKey, TValue>> ToConcurrentDictionaryAsync<T, TKey, TValue>(
      this IAsyncEnumerable<T> enumerable,
      Func<T, TKey> keySelector,
      Func<T, TValue> valueSelector,
      IEqualityComparer<TKey> equalityComparer = null
      )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( keySelector ), keySelector );
      ArgumentValidator.ValidateNotNull( nameof( valueSelector ), valueSelector );

      // Normal Dictionary<TKey, TValue> constructor accepts null as equality comparer, but ConcurrentDictionary throws... sigh. :)
      var retVal = new ConcurrentDictionary<TKey, TValue>( equalityComparer ?? EqualityComparer<TKey>.Default );
      await enumerable.AddToConcurrentCollectionAsync( retVal, ( dictionary, item ) => dictionary.TryAdd( keySelector( item ), valueSelector( item ) ) );
      return retVal;
   }

   /// <summary>
   /// This extension method will possibly concurrently enumerate this <see cref="IAsyncEnumerable{T}"/> into a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
   /// </summary>
   /// <typeparam name="T">The type of items being enumerated.</typeparam>
   /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
   /// <typeparam name="TValue">The type of dictionary values.</typeparam>
   /// <param name="enumerable">This <see cref="IAsyncEnumerable{T}"/>.</param>
   /// <param name="keySelector">The callback to potentially asynchronously create a dictionary key from enumerable item.</param>
   /// <param name="valueSelector">The callback to potentially asynchronously create a dictionary value from enumerable item.</param>
   /// <param name="equalityComparer">The optional <see cref="IEqualityComparer{T}"/> to use when creating dictionary.</param>
   /// <returns>Asynchronously returns a <see cref="IDictionary{TKey, TValue}"/> containing keys and values as returned by <paramref name="keySelector"/> and <paramref name="valueSelector"/>.</returns>
   /// <remarks>
   /// <para>This method will always use <see cref="E_UtilPack.EnumerateConcurrentlyIfPossible{T}(IAsyncEnumerable{T}, Action{T})"/> method to enumerate this <see cref="IAsyncEnumerable{T}"/>.
   /// </para>
   /// <para>
   /// TODO currently this will not throw if there are duplicate keys, unlike <see cref="ToDictionaryAsync{T, TKey, TValue}(IAsyncEnumerable{T}, Func{T, TKey}, Func{T, TValue}, IEqualityComparer{TKey})"/> method.
   /// The behaviour needs to be unified/parametrized at some point.
   /// </para>
   /// </remarks>
   /// <exception cref="NullReferenceException">If this <see cref="IAsyncEnumerable{T}"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If either of <paramref name="keySelector"/> or <paramref name="valueSelector"/> is <c>null</c>.</exception>
   public static async Task<ConcurrentDictionary<TKey, TValue>> ToConcurrentDictionaryAsync<T, TKey, TValue>(
      this IAsyncEnumerable<T> enumerable,
      Func<T, ValueTask<TKey>> keySelector,
      Func<T, ValueTask<TValue>> valueSelector,
      IEqualityComparer<TKey> equalityComparer = null
      )
   {
      ArgumentValidator.ValidateNotNullReference( enumerable );
      ArgumentValidator.ValidateNotNull( nameof( keySelector ), keySelector );
      ArgumentValidator.ValidateNotNull( nameof( valueSelector ), valueSelector );

      // Normal Dictionary<TKey, TValue> constructor accepts null as equality comparer, but ConcurrentDictionary throws... sigh. :)
      var retVal = new ConcurrentDictionary<TKey, TValue>( equalityComparer ?? EqualityComparer<TKey>.Default );
      await enumerable.AddToConcurrentCollectionAsync( retVal, async ( dictionary, item ) => dictionary.TryAdd( await keySelector( item ), await valueSelector( item ) ) );
      return retVal;
   }

#endif
}