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
using TTaskInstanceInfo = System.ValueTuple<UtilPack.NuGet.MSBuild.TaskReferenceHolder, System.Collections.Generic.IDictionary<System.String, System.ValueTuple<UtilPack.NuGet.MSBuild.WrappedPropertyKind, UtilPack.NuGet.MSBuild.WrappedPropertyInfo>>>;
using TTaskTypeGenerationParameters = System.ValueTuple<System.Boolean, System.Collections.Generic.IDictionary<System.String, System.ValueTuple<UtilPack.NuGet.MSBuild.WrappedPropertyKind, UtilPack.NuGet.MSBuild.WrappedPropertyInfo>>>;
using TTaskInstanceCreationInfo = System.ValueTuple<UtilPack.NuGet.MSBuild.TaskReferenceHolder, UtilPack.NuGet.MSBuild.ResolverLogger>;

namespace UtilPack.NuGet.MSBuild
{
   partial class NuGetTaskRunnerFactory
   {
      private NuGetTaskExecutionHelper CreateExecutionHelper(
         IBuildEngine taskFactoryLoggingHost,
         XElement taskBodyElement,
         String taskName,
         NuGetResolverWrapper nugetResolver,
         String assemblyPath,
         IDictionary<String, ISet<String>> assemblyPathsBySimpleName,
         TResolveResult platformFrameworkPaths
         )
      {

         return new NETStandardExecutionHelper(
            taskFactoryLoggingHost,
            taskName,
            nugetResolver,
            assemblyPath,
            assemblyPathsBySimpleName,
            platformFrameworkPaths
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

      private sealed class NETStandardExecutionHelper : CommonAssemblyRelatedHelper, NuGetTaskExecutionHelper
      {
         private readonly NuGetTaskLoadContext _loader;
         private readonly Lazy<(Type, ConstructorInfo, Object[], Boolean)> _taskType;
         private readonly Lazy<TTaskInstanceInfo> _taskInstance;
         private readonly ResolverLogger _logger;

         public NETStandardExecutionHelper(
            IBuildEngine taskFactoryLoggingHost,
            String taskName,
            NuGetResolverWrapper nugetResolver,
            String assemblyPath,
            IDictionary<String, ISet<String>> assemblyPathsBySimpleName,
            TResolveResult platformPackages
            )
            : base( assemblyPathsBySimpleName, nugetResolver, assemblyPath, taskName )
         {
            this._logger = new ResolverLogger( taskFactoryLoggingHost, nugetResolver.Resolver.NuGetLogger );
            this._loader = new NuGetTaskLoadContext( this, platformPackages );
            this._taskType = new Lazy<(Type, ConstructorInfo, Object[], Boolean)>( () => this.LoadTaskType(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication );
            this._taskInstance = new Lazy<TTaskInstanceInfo>( () =>
            {
               var taskRef = new TaskReferenceHolder( this._taskType.Value.Item2.Invoke( this._taskType.Value.Item3 ), typeof( ITask ).GetTypeInfo().Assembly.GetName().FullName );
               return (taskRef, taskRef.GetPropertyInfo().ToDictionary( kvp => kvp.Key, kvp => TaskReferenceHolder.DecodeKindAndInfo( kvp.Value ) ));
            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication );
         }

         public TTaskInstanceCreationInfo GetTaskInstanceCreationInfo()
         {
            return (this._taskInstance.Value.Item1, this._logger);
         }

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

         public TaskPropertyInfo[] GetTaskParameters()
         {
            return this._taskInstance.Value.Item2
               .Select( kvp =>
               {
                  var propType = GetPropertyType( kvp.Value.Item1 );
                  var info = kvp.Value.Item2;
                  return propType == null ?
                     null :
                     new TaskPropertyInfo( kvp.Key, propType, info == WrappedPropertyInfo.Out, info == WrappedPropertyInfo.Required );
               } )
               .Where( propInfo => propInfo != null )
               .ToArray();
         }

         public TTaskTypeGenerationParameters GetTaskTypeGenerationParameters()
         {
            return (this._taskInstance.Value.Item1.IsCancelable, this._taskInstance.Value.Item2);
         }

         public Boolean TaskUsesDynamicLoading => this._taskType.Value.Item4;

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