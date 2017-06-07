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
#if !NET45
using Microsoft.Build.Framework;
using NuGet.Frameworks;
using NuGet.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using UtilPack;
using NuGet.Packaging;
using System.Xml.Linq;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;

using TResolveResult = System.Collections.Generic.IDictionary<System.String, UtilPack.NuGet.MSBuild.ResolvedPackageInfo>;

namespace UtilPack.NuGet.MSBuild
{
   partial class NuGetTaskRunnerFactory
   {
      private TaskReferenceHolderInfo CreateExecutionHelper(
         IBuildEngine taskFactoryLoggingHost,
         XElement taskBodyElement,
         String taskName,
         NuGetResolverWrapper nugetResolver,
         String assemblyPath,
         IDictionary<String, ISet<String>> assemblyPathsBySimpleName,
         ResolverLogger resolverLogger,
         TResolveResult platformFrameworkPaths
         )
      {
         var helper = new NETStandardExecutionHelper(
            taskName,
            nugetResolver,
            assemblyPath,
            assemblyPathsBySimpleName,
            resolverLogger,
            platformFrameworkPaths
            );
         return new TaskReferenceHolderInfo(
            helper.TaskReference,
            resolverLogger,
            () => helper.Dispose()
            );
      }

      // We must subclass System.Runtime.Loader.AssemblyLoadContext, since the default load context does not seem to launch Resolve event for System.* assemblies (and thus we never catch load request for e.g. System.Threading.Tasks.Extensions assembly, since UtilPack references newer than is in e.g. 1.1.2 app folder).
      private sealed class NuGetTaskLoadContext : AssemblyLoadContext, IDisposable
      {
         private readonly CommonAssemblyRelatedHelper _helper;
         private readonly ConcurrentDictionary<AssemblyName, Lazy<Assembly>> _loadedAssemblies;
         private readonly ISet<String> _frameworkAssemblySimpleNames;
         private readonly AssemblyLoadContext _defaultLoadContext;

         public NuGetTaskLoadContext(
            CommonAssemblyRelatedHelper helper,
            TResolveResult frameworkAssemblyInfo
            )
         {
            this._helper = helper;
            this._defaultLoadContext = GetLoadContext( this.GetType().GetTypeInfo().Assembly );
            this._loadedAssemblies = new ConcurrentDictionary<AssemblyName, Lazy<Assembly>>(
               ComparerFromFunctions.NewEqualityComparer<AssemblyName>(
                  ( x, y ) => String.Equals( x.Name, y.Name ) && String.Equals( x.CultureName, y.CultureName ) && x.Version.Equals( y.Version ) && ArrayEqualityComparer<Byte>.ArrayEquality( x.GetPublicKeyToken(), y.GetPublicKeyToken() ),
                  x => x.Name.GetHashCode()
                  )
               );
            this._frameworkAssemblySimpleNames = new HashSet<String>( frameworkAssemblyInfo.Values
               .SelectMany( p => p.Assemblies )
               .Select( p => Path.GetFileNameWithoutExtension( p ) )
               );
         }

         protected override Assembly Load( AssemblyName assemblyName )
         {
            // Some System.* packages are provided by .NET Core runtime, and some via NuGet extensions
            // Unfortunately, the only way to distinguish them (in order to avoid loading assembly duplicates from NuGet packages) is to try using default context *first*, and then use our own custom context in case the default context fails to load the assembly.
            // There is no other way to see failed assembly load than catching exception ( LoadFromAssemblyName never returns null ).
            // A pity that API was designed like that.

            // To save amount of catches we do, cache all loaded assembly by their assembly names.
            return this._loadedAssemblies.GetOrAdd( assemblyName, an => new Lazy<Assembly>( () =>
            {
               var tryDefaultFirst = this._helper.IsMBFAssembly( an ) || this._frameworkAssemblySimpleNames.Contains( an.Name );
               Assembly retVal = null;
               if ( tryDefaultFirst )
               {
                  // We use default loader only for assemblies which are not part of our used non-system packages.
                  // Otherwise we will get exceptions in Release build, since e.g. UtilPack will have exactly same full assembly name (since its public key won't be null), and since this assembly was loaded using LoadFromAssemblyPath by MSBuild, it will fail to resolve the dependencies (at least System.Threading.Tasks.Extensions).
                  try
                  {
                     retVal = this._defaultLoadContext.LoadFromAssemblyName( assemblyName );
                  }
                  catch
                  {
                     // Ignore
                  }
               }
               return retVal ?? this._helper.PerformAssemblyResolve( an );
            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) ).Value;
         }

         public void Dispose()
         {
            this._loadedAssemblies.Clear();
         }
      }

      private sealed class NETStandardExecutionHelper : CommonAssemblyRelatedHelper
      {
         private readonly NuGetTaskLoadContext _loader;
         private readonly ResolverLogger _logger;

         public NETStandardExecutionHelper(
            String taskName,
            NuGetResolverWrapper nugetResolver,
            String assemblyPath,
            IDictionary<String, ISet<String>> assemblyPathsBySimpleName,
            ResolverLogger logger,
            TResolveResult platformPackages
            )
            : base( assemblyPathsBySimpleName, nugetResolver, assemblyPath, taskName )
         {
            this._logger = logger;
            this._loader = new NuGetTaskLoadContext( this, platformPackages );
            var taskTypeInfo = this.LoadTaskType();
            this.TaskReference = new TaskReferenceHolder(
                  taskTypeInfo.Item2.Invoke( taskTypeInfo.Item3 ),
                  typeof( ITask ).GetTypeInfo().Assembly.GetName().FullName,
                  taskTypeInfo.Item4
                  );
         }

         public TaskReferenceHolder TaskReference { get; }

         public override void Dispose()
         {
            // TODO maybe some day will be possible to say this._loader.UnloadAll()...
            try
            {
               base.Dispose();
            }
            finally
            {
               this._loader.DisposeSafely();
            }
         }

         protected override void LogResolveMessage( String message )
         {
            this._logger.Log( message );
         }

         protected override Assembly LoadAssemblyFromPath( String path )
         {
            return this._loader.LoadFromAssemblyPath( path );
         }
      }
   }
}
#endif