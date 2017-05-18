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
using System.Linq;
using System.Collections.Generic;

namespace UtilPack
{
   /// <summary>
   /// Extension method holder to enumerate graphs as depth first IEnumerables and breadth first IEnumerables. Additionally contains method to enumerate single chain as enumerable.
   /// </summary>
   public static class E_TreeToEnumerable
   {
      /// <summary>
      /// Using a starting node and function to get children, returns enumerable which walks transitively through all nodes accessible from the starting node, in depth-first order.
      /// Does not check for loops, so if there are loops, this method will never return, until most likely <see cref="OutOfMemoryException"/> is thrown.
      /// </summary>
      /// <typeparam name="T">The type of the node</typeparam>
      /// <param name="head">Starting node</param>
      /// <param name="childrenFunc">Function to return children given a single node</param>
      /// <param name="returnHead">Whether to return <paramref name="head"/> as first element of resulting enumerable.</param>
      /// <returns>Enumerable to walk through all nodes accessible from start node, in depth-first order</returns>
      /// <remarks>This is not recursive algorithm.</remarks>
      public static IEnumerable<T> AsDepthFirstEnumerable<T>( this T head, Func<T, IEnumerable<T>> childrenFunc, Boolean returnHead = true )
      {
         if ( returnHead )
         {
            yield return head;
         }

         var stack = new Stack<T>( childrenFunc( head ) );
         while ( stack.Count > 0 )
         {
            var cur = stack.Pop();

            yield return cur;

            var children = childrenFunc( cur );

            if ( children != null )
            {
               foreach ( var child in children )
               {
                  stack.Push( child );
               }
            }
         }
      }

      //Stack<T> stk;
      //using ( var enumerator = childrenFunc( head ).GetEnumerator() )
      //{
      //   if ( enumerator.MoveNext() )
      //   {
      //      stk = new Stack<T>();
      //      do
      //      {
      //         stk.Push( enumerator.Current );
      //      } while ( enumerator.MoveNext() );
      //   }
      //   else
      //   {
      //      stk = null;
      //   }
      //}

      //if ( stk != null )
      //{
      //   while ( stk.Count > 0 )
      //   {
      //      var cur = stk.Pop();

      //      yield return cur;

      //      foreach ( var child in childrenFunc( cur ) )
      //      {
      //         stk.Push( child );
      //      }
      //   }
      //}

      /// <summary>
      /// Using a starting node and function to get children, returns enumerable which walks transitively through all nodes accessible from the starting node, in depth-first order.
      /// This algorithm checks for loops, so each reachable node is visited exactly once.
      /// </summary>
      /// <typeparam name="T">The type of the node</typeparam>
      /// <param name="head">Starting node</param>
      /// <param name="childrenFunc">Function to return children given a single node</param>
      /// <param name="returnHead">Whether to return <paramref name="head"/> as first element of resulting enumerable.</param>
      /// <param name="equalityComparer">The equality comparer to use when checking for loops. Can be <c>null</c> for default equality comparer.</param>
      /// <returns>Enumerable to walk through all nodes accessible from start node, in depth-first order.</returns>
      /// <remarks>This is not recursive algorithm.</remarks>
      public static IEnumerable<T> AsDepthFirstEnumerableWithLoopDetection<T>(
         this T head,
         Func<T, IEnumerable<T>> childrenFunc,
         Boolean returnHead = true,
         IEqualityComparer<T> equalityComparer = null
         )
      {
         var set = new HashSet<T>( equalityComparer ?? EqualityComparer<T>.Default );
         if ( returnHead )
         {
            yield return head;
            set.Add( head );
         }

         var stack = new Stack<T>( childrenFunc( head ) );
         while ( stack.Count > 0 )
         {
            var cur = stack.Pop();
            if ( set.Add( cur ) )
            {
               yield return cur;

               var children = childrenFunc( cur );

               if ( children != null )
               {
                  foreach ( var child in children )
                  {
                     stack.Push( child );
                  }
               }
            }
         }
      }

      //if ( returnHead )
      //{
      //   yield return head;
      //}

      //// Avoid allocating new stack object on each call
      //Stack<T> stk;
      //using ( var enumerator = childrenFunc( head ).GetEnumerator() )
      //{
      //   if ( enumerator.MoveNext() )
      //   {
      //      stk = new Stack<T>();
      //      do
      //      {
      //         stk.Push( enumerator.Current );
      //      } while ( enumerator.MoveNext() );
      //   }
      //   else
      //   {
      //      stk = null;
      //   }
      //}

      //if ( stk != null )
      //{
      //   var set = new HashSet<T>( equalityComparer ?? EqualityComparer<T>.Default );
      //   while ( stk.Count > 0 )
      //   {
      //      var cur = stk.Pop();
      //      if ( set.Add( cur ) )
      //      {
      //         yield return cur;

      //         foreach ( var child in childrenFunc( cur ) )
      //         {
      //            stk.Push( child );
      //         }
      //      }
      //   }
      //}


      /// <summary>
      /// Using a starting node and function to get children, returns enumerable which walks transitively through all nodes accessible from the starting node, in breadth-first order.
      /// Does not check for loops, so if there are loops, this method will never return, until most likely <see cref="OutOfMemoryException"/> is thrown.
      /// </summary>
      /// <typeparam name="T">The type of the node</typeparam>
      /// <param name="head">Starting node</param>
      /// <param name="childrenFunc">Function to return children given a single node</param>
      /// <param name="returnHead">Whether to return <paramref name="head"/> as first element of resulting enumerable.</param>
      /// <returns>Enumerable to walk through all nodes accessible from start node, in breadth-first order</returns>
      /// <remarks>This is not a recursive algorithm.</remarks>
      public static IEnumerable<T> AsBreadthFirstEnumerable<T>(
         this T head,
         Func<T, IEnumerable<T>> childrenFunc,
         Boolean returnHead = true
         )
      {
         if ( returnHead )
         {
            yield return head;
         }

         var queue = new Queue<T>( childrenFunc( head ) );
         while ( queue.Count > 0 )
         {
            var cur = queue.Dequeue();
            yield return cur;

            var children = childrenFunc( cur );
            if ( children != null )
            {
               foreach ( var child in children )
               {
                  queue.Enqueue( child );
               }
            }
         }
      }

      //// Avoid allocating new queue object on each call
      //Queue<T> queue;
      //using ( var enumerator = childrenFunc( head ).GetEnumerator() )
      //{
      //   if ( enumerator.MoveNext() )
      //   {
      //      queue = new Queue<T>();
      //      do
      //      {
      //         queue.Enqueue( enumerator.Current );
      //      } while ( enumerator.MoveNext() );
      //   }
      //   else
      //   {
      //      queue = null;
      //   }
      //}

      //if ( queue != null )
      //{
      //   while ( queue.Count > 0 )
      //   {
      //      var cur = queue.Dequeue();
      //      yield return cur;
      //      foreach ( var child in childrenFunc( cur ) )
      //      {
      //         queue.Enqueue( child );
      //      }
      //   }
      //}

      /// <summary>
      /// Using a starting node and function to get children, returns enumerable which walks transitively through all nodes accessible from the starting node, in breadth-first order.
      /// This algorithm checks for loops, so each reachable node is visited exactly once.
      /// </summary>
      /// <typeparam name="T">The type of the node</typeparam>
      /// <param name="head">Starting node</param>
      /// <param name="childrenFunc">Function to return children given a single node</param>
      /// <param name="returnHead">Whether to return <paramref name="head"/> as first element of resulting enumerable.</param>
      /// <param name="equalityComparer">The equality comparer to use when checking for loops. Can be <c>null</c> for default equality comparer.</param>
      /// <returns>Enumerable to walk through all nodes accessible from start node, in breadth-first order</returns>
      /// <remarks>This is not a recursive algorithm.</remarks>
      public static IEnumerable<T> AsBreadthFirstEnumerableWithLoopDetection<T>(
         this T head,
         Func<T, IEnumerable<T>> childrenFunc,
         Boolean returnHead = true,
         IEqualityComparer<T> equalityComparer = null
         )
      {
         var set = new HashSet<T>( equalityComparer ?? EqualityComparer<T>.Default );
         if ( returnHead )
         {
            yield return head;
            set.Add( head );
         }

         var queue = new Queue<T>( childrenFunc( head ) );
         while ( queue.Count > 0 )
         {
            var cur = queue.Dequeue();
            if ( set.Add( cur ) )
            {
               yield return cur;

               var children = childrenFunc( cur );
               if ( children != null )
               {
                  foreach ( var child in children )
                  {
                     queue.Enqueue( child );
                  }
               }
            }
         }
      }


      /// <summary>
      /// Using a starting node and function get child, returns enumerable which walks transitively through all nodes accessible from the starting node.
      /// Does not check for loops, so if there are loops, this method will never return, until most likely <see cref="OutOfMemoryException"/> is thrown.
      /// </summary>
      /// <typeparam name="T">The type of the node.</typeparam>
      /// <param name="head">Starting node.</param>
      /// <param name="childFunc">Function to return child given a single node.</param>
      /// <param name="endCondition">Customizable condition to end enumeration. By default it will end when the child returned by <paramref name="childFunc"/> will be <c>default(T)</c></param>
      /// <param name="includeFirst">Whether to include <paramref name="head"/> in the result. Note that if this is <c>false</c>, the <paramref name="childFunc"/> will be invoked on the <paramref name="head"/> without checking the end-condition.</param>
      /// <returns>Enumerable to walk through all nodes accessible from the start node.</returns>
      /// <remarks>This is not a recursive algorithm.</remarks>
      public static IEnumerable<T> AsSingleBranchEnumerable<T>( this T head, Func<T, T> childFunc, Func<T, Boolean> endCondition = null, Boolean includeFirst = true )
      {
         // Check end condition variable
         if ( endCondition == null )
         {
            var def = default( T );
            endCondition = x => Object.Equals( x, def );
         }
         if ( !includeFirst )
         {
            head = childFunc( head );
         }

         while ( !endCondition( head ) )
         {
            yield return head;
            head = childFunc( head );
         }
      }

      /// <summary>
      /// Using a starting node and function get child, returns enumerable which walks transitively through all nodes accessible from the starting node.
      /// This algorithm checks for loops, so each reachable node is visited exactly once.
      /// </summary>
      /// <typeparam name="T">The type of the node.</typeparam>
      /// <param name="head">Starting node.</param>
      /// <param name="childFunc">Function to return child given a single node.</param>
      /// <param name="endCondition">Customizable condition to end enumeration. By default it will end when the child returned by <paramref name="childFunc"/> will be <c>default(T)</c></param>
      /// <param name="includeFirst">Whether to include <paramref name="head"/> in the result. Note that if this is <c>false</c>, the <paramref name="childFunc"/> will be invoked on the <paramref name="head"/> without checking the end-condition.</param>
      /// <param name="equalityComparer">The equality comparer to use when checking for loops. Can be <c>null</c> for default equality comparer.</param>
      /// <returns>Enumerable to walk through all nodes accessible from the start node.</returns>
      /// <remarks>This is not a recursive algorithm.</remarks>
      public static IEnumerable<T> AsSingleBranchEnumerableWithLoopDetection<T>(
         this T head,
         Func<T, T> childFunc,
         Func<T, Boolean> endCondition = null,
         Boolean includeFirst = true,
         IEqualityComparer<T> equalityComparer = null
         )
      {
         // Check end condition variable
         if ( endCondition == null )
         {
            var def = default( T );
            endCondition = x => Object.Equals( x, def );
         }

         if ( !includeFirst )
         {
            head = childFunc( head );
         }

         var set = new HashSet<T>( equalityComparer ?? EqualityComparer<T>.Default );
         while ( !endCondition( head ) && set.Add( head ) )
         {
            yield return head;
            head = childFunc( head );
         }
      }
   }
}
