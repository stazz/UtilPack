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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UtilPack.ResourcePooling
{
   public interface ExplicitAsyncResourcePool<TResource> : AsyncResourcePool<TResource>
   {
      ValueTask<ExplicitResourceAcquireInfo<TResource>> TakeResourceAsync( CancellationToken token );
      ValueTask<Boolean> ReturnResource( ExplicitResourceAcquireInfo<TResource> resourceInfo );
   }

   public interface ExplicitAsyncResourcePoolObservable<TResource> : ExplicitAsyncResourcePool<TResource>, AsyncResourcePoolObservable<TResource>
   {

   }

   public interface ExplicitAsyncResourcePool<TResource, in TCleanupParameter> : ExplicitAsyncResourcePool<TResource>, AsyncResourcePool<TResource, TCleanupParameter>
   {

   }

   public interface ExplicitAsyncResourcePoolObservable<TResource, in TCleanupParameter> : ExplicitAsyncResourcePool<TResource, TCleanupParameter>, ExplicitAsyncResourcePoolObservable<TResource>, AsyncResourcePoolObservable<TResource, TCleanupParameter>
   {

   }

   public interface ExplicitResourceAcquireInfo<out TResource>
   {
      TResource Resource { get; }
   }
}
