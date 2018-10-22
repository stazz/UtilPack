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
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UtilPack.NuGet.AssemblyLoading;
using UtilPack.ResourcePooling;
using UtilPack.ResourcePooling.NuGetAssemblyLoading;

namespace UtilPack.ResourcePooling.NuGetAssemblyLoading
{
   public static class Defaults
   {
      public static Func<String, String, String, CancellationToken, Task<Assembly>> DefaultAssemblyLoader { get; } = ( packageID, packageVersion, assemblyPath, token ) =>
             ( NuGetAssemblyResolverFactory.GetAssemblyResolver( typeof( Defaults ).
#if !NET46
            GetTypeInfo().
#endif
            Assembly ) ?? throw new InvalidOperationException( $"This type must be loaded using {nameof( NuGetAssemblyResolver )}." ) )
                .LoadNuGetAssembly( packageID, packageVersion, token, assemblyPath );
   }
}

public static partial class E_UtilPack
{
   public static Task<AsyncResourceFactory<TResource>> CreateAsyncResourceFactoryUsingNuGetAssemblyLoading<TResource>(
      this ResourceFactoryDynamicCreationConfiguration configuration,
      Func<AsyncResourceFactoryProvider, Object> creationParametersProvider,
      CancellationToken token,
      Func<String, String, String, CancellationToken, Task<Assembly>> assemblyLoader = null
      )
   {
      return configuration.CreateAsyncResourceFactory<TResource>(
         assemblyLoader ?? Defaults.DefaultAssemblyLoader,
         creationParametersProvider,
         token
         );
   }
}