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
#if NETSTANDARD1_5
using NuGet.Frameworks;
using NuGet.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Concurrent;

namespace UtilPack.NuGet.MSBuild
{
   partial class NuGetTaskRunnerFactory
   {
      private NuGetTaskExecutionHelper CreateExecutionHelper(
         Microsoft.Build.Framework.IBuildEngine taskFactoryLoggingHost,
         String taskName,
         NuGetBoundResolver nugetResolver,
         String assemblyPath,
         IDictionary<String, ISet<String>> assemblyPathsBySimpleName
         )
      {
         // .NETStandard 1.5+ uses System.Runtime.Loader.AssemblyContext instead of app domains.
         return new NETStandardExecutionHelper( assemblyPathsBySimpleName, new NuGetResolverWrapper( nugetResolver ), assemblyPath, taskName, taskFactoryLoggingHost );
      }

      // We must subclass System.Runtime.Loader.AssemblyLoadContext, since the default load context does not seem to launch Resolve event for System.* assemblies (and thus we never catch load request for e.g. System.Threading.Tasks.Extensions assembly).
      private sealed class NuGetTaskLoadContext : System.Runtime.Loader.AssemblyLoadContext
      {
         private readonly CommonAssemblyRelatedHelper _helper;

         private readonly ConcurrentDictionary<AssemblyName, Lazy<Assembly>> _loadedAssemblies;

         public NuGetTaskLoadContext( CommonAssemblyRelatedHelper helper )
         {
            this._helper = helper;
            this._loadedAssemblies = new ConcurrentDictionary<AssemblyName, Lazy<Assembly>>(
               ComparerFromFunctions.NewEqualityComparer<AssemblyName>(
                  ( x, y ) => String.Equals( x.Name, y.Name ) && String.Equals( x.CultureName, y.CultureName ) && x.Version.Equals( y.Version ) && ArrayEqualityComparer<Byte>.ArrayEquality( x.GetPublicKeyToken(), y.GetPublicKeyToken() ),
                  x => x.Name.GetHashCode()
                  )
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
               Assembly retVal = null;
               try
               {
                  retVal = Default.LoadFromAssemblyName( assemblyName );
               }
               catch
               {
                  // Ignore
               }
               return retVal ?? this._helper.PerformAssemblyResolve( an );
            } ) ).Value;
         }
      }

      private sealed class NETStandardExecutionHelper : CommonAssemblyRelatedHelper, NuGetTaskExecutionHelper
      {
         private readonly System.Runtime.Loader.AssemblyLoadContext _loader;
         private readonly Lazy<Type> _taskType;
         private Microsoft.Build.Framework.IBuildEngine _be;

         public NETStandardExecutionHelper(
            IDictionary<String, ISet<String>> assemblyPathsBySimpleName,
            NuGetResolverWrapper resolver,
            String targetAssemblyPath,
            String taskName,
            Microsoft.Build.Framework.IBuildEngine taskFactoryBE
            )
            : base( assemblyPathsBySimpleName, resolver, targetAssemblyPath, taskName )
         {
            this._loader = new NuGetTaskLoadContext( this );
            this._be = taskFactoryBE;
            // Register to default load context resolving event to minimize amount of exceptions thrown.
            System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += this._loader_Resolving;
            this._taskType = new Lazy<Type>( () => this.LoadTaskType() );
         }

         private Assembly _loader_Resolving( System.Runtime.Loader.AssemblyLoadContext loader, AssemblyName name )
         {
            return this.PerformAssemblyResolve( name );
         }

         public Object CreateTaskInstance( Type taskType, Microsoft.Build.Framework.IBuildEngine taskLoggingHost )
         {
            // The taskLoggingHost is the BE given to the task.
            this._be = taskLoggingHost;
            return this.PerformCreateTaskInstance( taskType );
         }

         public void Dispose()
         {
            // TODO maybe some day will be possible to say this._loader.UnloadAll()...
            System.Runtime.Loader.AssemblyLoadContext.Default.Resolving -= this._loader_Resolving;
         }

         public Microsoft.Build.Framework.TaskPropertyInfo[] GetTaskParameters()
         {
            return CommonHelpers.GetPropertyInfoFromType(
               this._taskType.Value,
               typeof( Microsoft.Build.Framework.ITask ).GetTypeInfo().Assembly.GetName()
               )
               .Where( kvp => kvp.Value.Item1 != WrappedPropertyKind.BuildEngine && kvp.Value.Item1 != WrappedPropertyKind.TaskHost )
               .Select( kvp => new Microsoft.Build.Framework.TaskPropertyInfo( kvp.Key, kvp.Value.Item3.PropertyType, kvp.Value.Item2 == WrappedPropertyInfo.Out, kvp.Value.Item2 == WrappedPropertyInfo.Required ) )
               .ToArray();
         }

         public Type GetTaskType()
         {
            return this._taskType.Value;
         }

         protected override void LogResolveMessage( String message )
         {
            this._be.LogMessageEvent( new Microsoft.Build.Framework.BuildMessageEventArgs( message, null, null, Microsoft.Build.Framework.MessageImportance.High ) );
         }

         protected override Assembly LoadAssemblyFromPath( String path )
         {
            return this._loader.LoadFromAssemblyPath( path );
         }
      }
   }
}
#endif