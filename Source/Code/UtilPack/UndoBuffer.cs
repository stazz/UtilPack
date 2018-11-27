/*
 * Copyright 2014 Stanislav Muhametsin. All rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
 * implied.
 *
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using UtilPack;

//namespace UtilPack
//{
//   /// <summary>
//   /// This undo buffer implementation only stores whatever undo units your application uses.
//   /// It does not try to solve the problems of both state and undo buffer, instead it concentrates on latter.
//   /// This class is designed so that as many operations as possible would be of <c>O(1)</c> time complexity.
//   /// Indeed, the only operation that has a possibility of non-<c>O(1)</c> complexity is shrinking the buffer to the size which is less than the current amount of undo units.
//   /// </summary>
//   /// <remarks>
//   /// This class is not threadsafe.
//   /// </remarks>
//   /// <typeparam name="T">The type of undo units.</typeparam>
//   public class UndoUnitBuffer<T>
//   {
//      // Sadly, the .Clear() method of linked list is O(n) and proper slicing API is missing (because it is hazardous if exposed publicly).
//      // So, let's do our own linked list.
//      private sealed class UndoUnitNode
//      {
//         private readonly T _value;
//         private UndoUnitNode _next;
//         private UndoUnitNode _prev;
//         internal UndoUnitNode( T value )
//         {
//            this._value = value;
//         }
//         public T Value
//         {
//            get
//            {
//               return this._value;
//            }
//         }
//         public UndoUnitNode Next
//         {
//            get
//            {
//               return this._next;
//            }
//            internal set
//            {
//               Interlocked.Exchange( ref this._next, value );
//            }
//         }
//         public UndoUnitNode Previous
//         {
//            get
//            {
//               return this._prev;
//            }
//            internal set
//            {
//               Interlocked.Exchange( ref this._prev, value );
//            }
//         }
//      }
//      private sealed class UndoUnitList
//      {
//         // Head's prev = tail
//         private UndoUnitNode _head;
//         private Int32 _count;
//         private Int32 _version;
//         public UndoUnitList()
//         {
//            this._head = null;
//         }
//         public Int32 Count
//         {
//            get
//            {
//               return this._count;
//            }
//         }
//         public UndoUnitNode First
//         {
//            get
//            {
//               return this._head;
//            }
//         }
//         public UndoUnitNode Last
//         {
//            get
//            {
//               return this._head == null ? null : this._head.Previous;
//            }
//         }
//         public void Clear()
//         {
//            this._head = null;
//            this._count = 0;
//            ++this._version;
//         }
//         public UndoUnitNode AddLast( T item )
//         {
//            var node = new UndoUnitNode( item );
//            if ( this._head == null )
//            {
//               this.AddToEmptyList( node );
//            }
//            else
//            {
//               this.InsertNodeBefore( this._head, node );
//            }
//            return node;
//         }
//         public void RemoveFirst()
//         {
//            if ( this._head == null )
//            {
//               throw new InvalidOperationException( "Can not remove from empty undo unit list." );
//            }
//            this.RemoveNode( this._head );
//         }
//         public void RemoveAllAfter( UndoUnitNode node, Int32 newCount )
//         {
//            ArgumentValidator.ValidateNotNull( "Node", node );
//            // Remember that tail is head's prev.
//            // Therefore we have to make 'node' as new tail
//            // Don't perform other sanity checks as this is not a public api...
//            this._head.Previous = node;
//            node.Next = this._head;
//            this._count = newCount;
//            ++this._version;
//         }
//         //public void RemoveAllBefore( UndoUnitNode node, Int32 newCount )
//         //{
//         // ArgumentValidator.ValidateNotNull( "Node", node );
//         // // We have to make 'node' as new head
//         // // Don't perform other sanity checks as this is not a public api...
//         // var oldHead = this._head;
//         // node.Previous = oldHead.Previous;
//         // node.Previous.Next = node;
//         // this._head = node;
//         // this._count = newCount;
//         //}
//         private void AddToEmptyList( UndoUnitNode node )
//         {
//            node.Next = node;
//            node.Previous = node;
//            this._head = node;
//            ++this._count;
//            ++this._version;
//         }
//         private void InsertNodeBefore( UndoUnitNode before, UndoUnitNode node )
//         {
//            node.Next = before;
//            node.Previous = before.Previous;
//            before.Previous.Next = node;
//            before.Previous = node;
//            ++this._count;
//            ++this._version;
//         }
//         private void RemoveNode( UndoUnitNode node )
//         {
//            // Special case - one item link, where head is also tail
//            if ( Object.ReferenceEquals( node, node.Next ) )
//            {
//               this._head = null;
//            }
//            else
//            {
//               // Fix prev and ndex
//               node.Next.Previous = node.Previous;
//               node.Previous.Next = node.Next;
//               if ( Object.ReferenceEquals( this._head, node ) )
//               {
//                  this._head = node.Next;
//               }
//            }
//            --this._count;
//            ++this._version;
//         }
//         internal Int32 Version
//         {
//            get
//            {
//               return this._version;
//            }
//         }
//      }
//      private sealed class UndoUnitEnumerable : IEnumerable<T>
//      {
//         private readonly UndoUnitBuffer<T> _buffer;
//         private readonly Boolean _isUndo;
//         internal UndoUnitEnumerable( UndoUnitBuffer<T> buffer, Boolean isUndo )
//         {
//            ArgumentValidator.ValidateNotNull( "Buffer", buffer );
//            this._buffer = buffer;
//            this._isUndo = isUndo;
//         }
//         private sealed class Enumerator : IEnumerator<T>
//         {
//            private readonly UndoUnitEnumerable _enumerable;
//            private readonly UndoUnitNode _startingNode;
//            private readonly Int32 _version;
//            private UndoUnitNode _node;
//            private T _current;
//            internal Enumerator( UndoUnitEnumerable enumerable )
//            {
//               this._enumerable = enumerable;
//               UndoUnitNode node;
//               if ( enumerable._isUndo )
//               {
//                  // Undo-order
//                  enumerable._buffer.TryGetNextToUndoNode( out node );
//               }
//               else
//               {
//                  // Redo-order
//                  enumerable._buffer.TryGetNextToRedoNode( out node );
//               }
//               this._startingNode = node;
//               this._node = this._startingNode;
//               this._version = enumerable._buffer._units.Version;
//            }
//            public T Current
//            {
//               get
//               {
//                  return this._current;
//               }
//            }
//            Object System.Collections.IEnumerator.Current
//            {
//               get
//               {
//                  return this.Current;
//               }
//            }
//            public Boolean MoveNext()
//            {
//               this.ThrowIfVersionMismatch();
//               var retVal = this._node != null;
//               if ( retVal )
//               {
//                  this._current = this._node.Value;
//                  var isRedo = !this._enumerable._isUndo;
//                  this._node = isRedo ? this._node.Next : this._node.Previous;
//                  var list = this._enumerable._buffer._units;
//                  if ( Object.ReferenceEquals(
//                  this._node,
//                  isRedo ? list.First : list.Last
//                  ) )
//                  {
//                     this._node = null;
//                  }
//               }
//               return retVal;
//            }
//            public void Reset()
//            {
//               this.ThrowIfVersionMismatch();
//               this._node = this._startingNode;
//            }
//            public void Dispose()
//            {
//               // Nothing to do
//            }
//            private void ThrowIfVersionMismatch()
//            {
//               if ( this._version != this._enumerable._buffer._units.Version )
//               {
//                  throw new InvalidOperationException( "Underlying undo buffer changed during enumeration." );
//               }
//            }
//         }
//         public IEnumerator<T> GetEnumerator()
//         {
//            return new Enumerator( this );
//         }
//         System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
//         {
//            return this.GetEnumerator();
//         }
//      }

//      private Int32 _undoBufferLength;
//      private readonly UndoUnitList _units;
//      // next to undo - 1. node 2. its index
//      // Will be null if nothing left to undo
//      private Tuple<UndoUnitNode, Int32> _current;

//      /// <summary>
//      /// Creates new instance of <see cref="UndoUnitBuffer{T}"/> with given buffer length.
//      /// </summary>
//      /// <param name="undoBufferLength">The length of buffer.</param>
//      /// <remarks>If <paramref name="undoBufferLength"/> is less than <c>0</c>, the <c>0</c> will be used instead as length.</remarks>
//      public UndoUnitBuffer( Int32 undoBufferLength )
//      {
//         this._units = new UndoUnitList();
//         this.BufferLength = undoBufferLength;
//      }

//      /// <summary>
//      /// This method should be invoked whenever a normal operation (not an undo or redo) completes within an application and should be added as newest undoable unit.
//      /// </summary>
//      /// <param name="unit">The unit that completed.</param>
//      /// <remarks>
//      /// If the buffer's current mark is in middle of undo stack, all units that follow will be discarded.
//      /// This is a <c>O(1)</c> operation in time complexity.
//      /// </remarks>
//      public void NormalUnitCompleted( T unit )
//      {
//         var current = this._current;
//         var units = this._units;
//         if ( current == null )
//         {
//            // First unit or we are at bottom of undo stack (no more to undo)
//            if ( units.Count > 0 )
//            {
//               // We are at bottom of undo stack -> just clear ( O(1) operation )
//               units.Clear();
//            }
//         }
//         else
//         {
//            if ( !this.IsLast( current ) )
//            {
//               // We are in middle of undo stack - remove all following the current ( O(1) operation)
//               units.RemoveAllAfter( current.Item1, current.Item2 + 1 );
//            }
//         }
//         // O(1) operation
//         var idx = units.Count;
//         this._current = Tuple.Create( this._units.AddLast( unit ), idx );
//         // Check length after addition only, because of the condition in that method.
//         // (if this class is used threadsafely, this will be O(1) operation )
//         this.CheckLength();
//      }

//      /// <summary>
//      /// Tries to get the next unit to undo. This operation will not modify the buffer state.
//      /// </summary>
//      /// <param name="unit">This will contain the next unit to undo, if there are any.</param>
//      /// <returns><c>true</c> if there are any units to undo; <c>false</c> otherwise.</returns>
//      /// <remarks>
//      /// This is a <c>O(1)</c> operation in time complexity.
//      /// </remarks>
//      public Boolean TryGetNextToUndo( out T unit )
//      {
//         UndoUnitNode node;
//         var retVal = this.TryGetNextToUndoNode( out node );
//         unit = retVal ? node.Value : default( T );
//         return retVal;
//      }

//      /// <summary>
//      /// Tries to get the next unit to redo. This operation will not modify the buffer state.
//      /// </summary>
//      /// <param name="unit">This will contain the next unit to redo, if there are any.</param>
//      /// <returns><c>true</c> if there are any units to redo; <c>false</c> otherwise.</returns>
//      /// <remarks>
//      /// This is a <c>O(1)</c> operation in time complexity.
//      /// </remarks>
//      public Boolean TryGetNextToRedo( out T unit )
//      {
//         UndoUnitNode node;
//         var retVal = this.TryGetNextToRedoNode( out node );
//         unit = retVal ? node.Value : default( T );
//         return retVal;
//      }

//      /// <summary>
//      /// This method will signal the buffer that an undo operation was successfully completed, and the state will be modified accordingly.
//      /// </summary>
//      /// <remarks>
//      /// This is a <c>O(1)</c> operation in time complexity.
//      /// </remarks>
//      /// <exception cref="InvalidOperationException">If this buffer is in such state that undo is not possible to perform.</exception>
//      public void UndoCompleted()
//      {
//         //if ( cur == null )
//         if ( !this.CanUndo() )
//         {
//            // There could not have been undo, if there was nothing to undo
//            this.ThrowUnderflow( true );
//         }
//         // Move the next-to-undo item to the previous of the buffer. If this was the first item of the buffer, set to 'null' to mark that we can't undo anymore.
//         var cur = this._current;
//         this._current = this.IsFirst( cur ) ? null : Tuple.Create( cur.Item1.Previous, cur.Item2 - 1 );
//      }

//      /// <summary>
//      /// This method will signal the buffer that a redo operation was successfully completed, and the state will be modified accordingly.
//      /// </summary>
//      /// <remarks>
//      /// This is a <c>O(1)</c> operation in time complexity.
//      /// </remarks>
//      /// <exception cref="InvalidOperationException">If this buffer is in such state that redo is not possible to perform.</exception>
//      public void RedoCompleted()
//      {
//         if ( !this.CanRedo() )
//         {
//            this.ThrowUnderflow( false );
//         }
//         var cur = this._current;
//         UndoUnitNode newCurrent;
//         // Do we have something as next-to-undo?
//         if ( cur == null )
//         {
//            // We didn't - so it should be the first item in buffer
//            newCurrent = this._units.First;
//            //// Check that the buffer actually had something
//            //if ( newCurrent == null )
//            //{
//            // throw new InvalidOperationException( "Redo stack underflow." );
//            //}
//         }
//         else
//         {
//            // We had something as next-to-undo, but it must not be as last item, since that means there was nothing to redo in a first place.
//            //if ( this.IsLast( cur ) )
//            //{
//            // throw new InvalidOperationException( "Redo stack underflow." );
//            //}
//            newCurrent = cur.Item1.Next;
//         }
//         this._current = Tuple.Create( newCurrent, cur == null ? 0 : ( cur.Item2 + 1 ) );
//      }

//      /// <summary>
//      /// Gets or sets the maximum amount of undo units this buffer can hold.
//      /// </summary>
//      /// <value>The maximum amount of undo units this buffer can hold.</value>
//      /// <remarks>
//      /// The getter is an <c>O(1)</c> operation in time complexity.
//      /// The setter is an <c>O(1)</c> operation in time complexity, except when buffer is shrinked.
//      /// In such case, the setter is an <c>O(n)</c> operation, where n is amount of units to be removed during shrinking.
//      /// The setter will use <c>0</c> as new buffer length if it is given less that <c>0</c> as new value.
//      /// </remarks>
//      /// <exception cref="InvalidOperationException">If the buffer mark is not on top of undo stack, i.e. if there is something to redo.</exception>
//      public Int32 BufferLength
//      {
//         get
//         {
//            return this._undoBufferLength;
//         }
//         set
//         {
//            value = Math.Max( 0, value );
//            this._undoBufferLength = value;
//            this.CheckLength();
//         }
//      }

//      /// <summary>
//      /// Gets the total amount of undo units currently held by this buffer.
//      /// </summary>
//      /// <value>The total amount of undo units currently held by this buffer.</value>
//      /// <remarks>
//      /// This is a <c>O(1)</c> operation in time complexity.
//      /// </remarks>
//      public Int32 Count
//      {
//         get
//         {
//            return this._units.Count;
//         }
//      }

//      /// <summary>
//      /// Removes all units from this buffer.
//      /// </summary>
//      /// <remarks>
//      /// This is a <c>O(1)</c> operation in time complexity.
//      /// </remarks>
//      public void Clear()
//      {
//         this._units.Clear();
//         this._current = null;
//      }

//      /// <summary>
//      /// Returns units to undo as an <see cref="IEnumerable{T}"/>.
//      /// The first element is next to undo as per state of buffer, the second is next to undo after that, etc.
//      /// </summary>
//      /// <value>Next units to undo.</value>
//      public IEnumerable<T> UnitsToUndo
//      {
//         get
//         {
//            return new UndoUnitEnumerable( this, true );
//         }
//      }

//      /// <summary>
//      /// Returns units to redo as an <see cref="IEnumerable{T}"/>.
//      /// The first element is next to redo as per state of buffer, the second is next to redo after that, etc.
//      /// </summary>
//      /// <value>Next units to redo.</value>
//      public IEnumerable<T> UnitsToRedo
//      {
//         get
//         {
//            return new UndoUnitEnumerable( this, false );
//         }
//      }

//      //public IEnumerable<UndoUnit> UndoUnits
//      //{
//      // get
//      // {
//      // return this._units;
//      // }
//      //}
//      private void CheckLength()
//      {
//         if ( this.CanRedo() )
//         {
//            throw new InvalidOperationException( "Undo unit buffer resize only doable when at top of undo stack." );
//         }
//         var units = this._units;
//         var length = this._undoBufferLength;
//         while ( units.Count > length )
//         {
//            units.RemoveFirst();
//         }
//         if ( length <= 0 )
//         {
//            this._current = null;
//         }
//      }

//      private Boolean IsLast( Tuple<UndoUnitNode, Int32> nodeInfo )
//      {
//         return Object.ReferenceEquals( this._units.Last, nodeInfo.Item1 );
//      }

//      private Boolean IsFirst( Tuple<UndoUnitNode, Int32> nodeInfo )
//      {
//         return Object.ReferenceEquals( this._units.First, nodeInfo.Item1 );
//      }

//      internal void ThrowUnderflow( Boolean isUndo )
//      {
//         throw new InvalidOperationException( ( isUndo ? "Undo" : "Redo" ) + " stack underflow." );
//      }

//      private Boolean TryGetNextToUndoNode( out UndoUnitNode uNode )
//      {
//         // Get the next unit to undo
//         var node = this._current;
//         // If it is null, we can't undo anymore
//         var retVal = node != null;
//         // Get the unit, if can
//         uNode = retVal ? node.Item1 : null;
//         // Return success status
//         return retVal;
//      }

//      private Boolean TryGetNextToRedoNode( out UndoUnitNode uNode )
//      {
//         // Get next unit to undo
//         var node = this._current;
//         Boolean retVal;
//         // Check if we have anything to undo
//         if ( node == null )
//         {
//            // If there is nothing to undo, we can redo only if there are undo units in buffer
//            retVal = this._units.Count > 0;
//            // The undo unit will be the first unit of the buffer
//            uNode = retVal ? this._units.First : null;
//         }
//         else
//         {
//            // If there is something to undo, we can redo only if next to undo is not the last unit in buffer
//            retVal = !this.IsLast( node );
//            // The undo unit will be the next value of the next-to-undo item.
//            uNode = retVal ? node.Item1.Next : null;
//         }
//         return retVal;
//      }
//   }
//}

//public static partial class E_UtilPack
//{
//   /// <summary>
//   /// Helper method to check whether given <see cref="UndoUnitBuffer{T}"/> can perform an undo operation.
//   /// </summary>
//   /// <typeparam name="T">The type of undo units.</typeparam>
//   /// <param name="buffer">The <see cref="UndoUnitBuffer{T}"/>.</param>
//   /// <returns><c>true</c> if <paramref name="buffer"/> is in such state that undo operation is possible; <c>false</c> otherwise.</returns>
//   /// <remarks>
//   /// This is a <c>O(1)</c> operation in time complexity.
//   /// </remarks>
//   public static Boolean CanUndo<T>( this UndoUnitBuffer<T> buffer )
//   {
//      T dummy;
//      return buffer.TryGetNextToUndo( out dummy );
//   }
//   /// <summary>
//   /// Helper method to check whether given <see cref="UndoUnitBuffer{T}"/> can perform an redo operation.
//   /// </summary>
//   /// <typeparam name="T">The type of undo units.</typeparam>
//   /// <param name="buffer">The <see cref="UndoUnitBuffer{T}"/>.</param>
//   /// <returns><c>true</c> if <paramref name="buffer"/> is in such state that redo operation is possible; <c>false</c> otherwise.</returns>
//   /// <remarks>
//   /// This is a <c>O(1)</c> operation in time complexity.
//   /// </remarks>
//   public static Boolean CanRedo<T>( this UndoUnitBuffer<T> buffer )
//   {
//      T dummy;
//      return buffer.TryGetNextToRedo( out dummy );
//   }
//   /// <summary>
//   /// Helper method to get the next unit to undo, or throw an <see cref="InvalidOperationException"/>.
//   /// </summary>
//   /// <typeparam name="T">The type of undo units.</typeparam>
//   /// <param name="buffer">The <see cref="UndoUnitBuffer{T}"/>.</param>
//   /// <returns>The next unit to undo.</returns>
//   /// <remarks>
//   /// This is a <c>O(1)</c> operation in time complexity.
//   /// </remarks>
//   /// <exception cref="InvalidOperationException">If there are no more units to undo.</exception>
//   public static T GetNextToUndo<T>( this UndoUnitBuffer<T> buffer )
//   {
//      T retVal;
//      if ( !buffer.TryGetNextToUndo( out retVal ) )
//      {
//         buffer.ThrowUnderflow( true );
//      }
//      return retVal;
//   }
//   /// <summary>
//   /// Helper method to get the next unit to redo, or throw an <see cref="InvalidOperationException"/>.
//   /// </summary>
//   /// <typeparam name="T">The type of undo units.</typeparam>
//   /// <param name="buffer">The <see cref="UndoUnitBuffer{T}"/>.</param>
//   /// <returns>The next unit to redo.</returns>
//   /// <remarks>
//   /// This is a <c>O(1)</c> operation in time complexity.
//   /// </remarks>
//   /// <exception cref="InvalidOperationException">If there are no more units to redo.</exception>
//   public static T GetNextToRedo<T>( this UndoUnitBuffer<T> buffer )
//   {
//      T retVal;
//      if ( !buffer.TryGetNextToRedo( out retVal ) )
//      {
//         buffer.ThrowUnderflow( false );
//      }
//      return retVal;
//   }
//}