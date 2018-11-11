/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
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
using System.Threading.Tasks;
using UtilPack;

namespace AsyncEnumeration.Implementation.Enumerable
{

   internal sealed class StatefulAsyncEnumerableWrapper<T> : IAsyncEnumerable<T>
   {
      private readonly Func<WrappingEnumerationStartInfo<T>> _factory;

      public StatefulAsyncEnumerableWrapper(
         Func<WrappingEnumerationStartInfo<T>> startInfoFactory,
         IAsyncProvider aLINQProvider
      )
      {
         this._factory = ArgumentValidator.ValidateNotNull( nameof( startInfoFactory ), startInfoFactory );
         this.AsyncProvider = aLINQProvider;
      }

      public IAsyncProvider AsyncProvider { get; }

      public IAsyncEnumerator<T> GetAsyncEnumerator()
      {
         return new AsyncEnumeratorWrapper<T>( this._factory() );
      }
   }

   internal sealed class StatelessAsyncEnumerableWrapper<T> : IAsyncEnumerable<T>
   {
      private readonly AsyncEnumeratorWrapper<T> _enumerator;

      public StatelessAsyncEnumerableWrapper(
         WrappingEnumerationStartInfo<T> startInfo,
         IAsyncProvider aLINQProvider
         )
      {
         this._enumerator = new AsyncEnumeratorWrapper<T>( startInfo );
         this.AsyncProvider = aLINQProvider;
      }

      public IAsyncProvider AsyncProvider { get; }

      public IAsyncEnumerator<T> GetAsyncEnumerator()
      {
         return this._enumerator;
      }
   }

   internal sealed class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
   {
      private readonly WaitForNextDelegate _waitForNext;
      private readonly TryGetNextDelegate<T> _tryGetNext;
      private readonly EnumerationEndedDelegate _dispose;

      public AsyncEnumeratorWrapper(
         WrappingEnumerationStartInfo<T> startInfo
         )
      {
         AsyncEnumeratorWrapperInitializer.InitVars( startInfo.WaitForNext, startInfo.TryGetNext, startInfo.Dispose, out this._waitForNext, out this._tryGetNext, out this._dispose );
      }

      public Task<Boolean> WaitForNextAsync() => this._waitForNext();

      public T TryGetNext( out Boolean success ) => this._tryGetNext( out success );

      public Task DisposeAsync() => this._dispose();


   }

   internal static class AsyncEnumeratorWrapperInitializer
   {
      internal static void InitVars<T>(
         WaitForNextDelegate waitForNext,
         TryGetNextDelegate<T> tryGetNext,
         EnumerationEndedDelegate dispose,
         out WaitForNextDelegate fieldWaitForNext,
         out TryGetNextDelegate<T> fieldTryGetNext,
         out EnumerationEndedDelegate fieldDispose
         )
      {
         fieldWaitForNext = ArgumentValidator.ValidateNotNull( nameof( waitForNext ), waitForNext );
         fieldTryGetNext = ArgumentValidator.ValidateNotNull( nameof( tryGetNext ), tryGetNext );
         fieldDispose = dispose ?? ( () => TaskUtils.CompletedTask );
      }
   }
}
