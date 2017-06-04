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

namespace UtilPack.NuGet.MSBuild
{
   partial class NuGetTaskRunnerFactory
   {
      private async ValueTask<NuGetTaskExecutionHelper> CreateExecutionHelper(
         Microsoft.Build.Framework.IBuildEngine taskFactoryLoggingHost,
         XElement taskBodyElement,
         String taskName,
         NuGetResolverWrapper nugetResolver,
         String assemblyPath,
         IDictionary<String, ISet<String>> assemblyPathsBySimpleName
         )
      {
         return await NETStandardExecutionHelper.Create(
            taskFactoryLoggingHost,
            taskBodyElement,
            taskName,
            nugetResolver,
            assemblyPath,
            assemblyPathsBySimpleName
            );
      }

      // We must subclass System.Runtime.Loader.AssemblyLoadContext, since the default load context does not seem to launch Resolve event for System.* assemblies (and thus we never catch load request for e.g. System.Threading.Tasks.Extensions assembly, since UtilPack references newer than is in e.g. 1.1.2 app folder).
      private sealed class NuGetTaskLoadContext : AssemblyLoadContext, IDisposable
      {
         private readonly CommonAssemblyRelatedHelper _helper;
         private readonly ConcurrentDictionary<AssemblyName, Lazy<Assembly>> _loadedAssemblies;
         private readonly ISet<String> _frameworkAssemblySimpleNames;
         private readonly AssemblyLoadContext _defaultLoadContext;

         private NuGetTaskLoadContext(
            CommonAssemblyRelatedHelper helper,
            IDictionary<String, String[]> frameworkAssemblyInfo
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
               .SelectMany( p => p )
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
               Boolean tryDefaultFirst;
               switch ( an.Name )
               {
                  case "Microsoft.Build":
                  case "Microsoft.Build.Framework":
                  case "Microsoft.Build.Tasks.Core":
                  case "Microsoft.Build.Utilities.Core":
                     // We'll always try load msbuild assemblies with default loader first.
                     tryDefaultFirst = true;
                     break;
                  default:
                     // Use default loader only if simple name matches a set of framework assemblies.
                     tryDefaultFirst = this._frameworkAssemblySimpleNames.Contains( an.Name );
                     break;
               }

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

         public static async System.Threading.Tasks.Task<NuGetTaskLoadContext> Create(
            CommonAssemblyRelatedHelper helper,
            NuGetResolverWrapper nugetResolver,
            XElement taskBodyElement
            )
         {
            var platformFrameworkPaths = nugetResolver.TransformToAssemblyPathDictionary(
                  await nugetResolver.Resolver.ResolveNuGetPackages(
                     taskBodyElement.Element( NuGetBoundResolver.NUGET_FW_PACKAGE_ID )?.Value ?? "Microsoft.NETCore.App", // This value is hard-coded in Microsoft.NET.Sdk.Common.targets, and currently no proper API exists to map NuGetFrameworks into package ID (+ version combination).
                     taskBodyElement.Element( NuGetBoundResolver.NUGET_FW_PACKAGE_VERSION )?.Value ?? "1.1.2"
                  ),
                  new NuGetPathResolverV2( r =>
                  {
                     return r.GetLibItems( PackagingConstants.Folders.Ref ).Concat( r.GetLibItems() );
                  } )
                  );
            return new NuGetTaskLoadContext( helper, platformFrameworkPaths );
         }
      }

      private sealed class NETStandardExecutionHelper : CommonAssemblyRelatedHelper, NuGetTaskExecutionHelper
      {
         private NuGetTaskLoadContext _loader;
         private readonly Lazy<Type> _taskType;
         private Microsoft.Build.Framework.IBuildEngine _be;

         private NETStandardExecutionHelper(
            Microsoft.Build.Framework.IBuildEngine taskFactoryLoggingHost,
            String taskName,
            NuGetResolverWrapper nugetResolver,
            String assemblyPath,
            IDictionary<String, ISet<String>> assemblyPathsBySimpleName
            )
            : base( assemblyPathsBySimpleName, nugetResolver, assemblyPath, taskName )
         {
            this._be = taskFactoryLoggingHost;
            // Register to default load context resolving event to minimize amount of exceptions thrown (we probably don't wanna pollute default loader with all the loaded stuff actually).
            //System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += this._loader_Resolving;
            //this._loader.Resolving += this._loader_Resolving;
            //System.Runtime.Loader.AssemblyLoadContext.GetLoadContext( Assembly.GetEntryAssembly() ).Resolving += this._loader_Resolving;
            this._taskType = new Lazy<Type>( () => this.LoadTaskType(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication );
         }

         private void SetLoader( NuGetTaskLoadContext loader )
         {
            System.Threading.Interlocked.CompareExchange( ref this._loader, loader, null );
         }

         //private Assembly _loader_Resolving( System.Runtime.Loader.AssemblyLoadContext loader, AssemblyName name )
         //{
         //   return this.PerformAssemblyResolve( name );
         //}

         public Object CreateTaskInstance( Type taskType, Microsoft.Build.Framework.IBuildEngine taskLoggingHost )
         {
            // The taskLoggingHost is the BE given to the task.
            this._be = taskLoggingHost;
            this._resolver.Resolver.NuGetLogger.SetBuildEngine( taskLoggingHost );
            return this.PerformCreateTaskInstance( taskType );
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
               //System.Runtime.Loader.AssemblyLoadContext.Default.Resolving -= this._loader_Resolving;
               this._loader.DisposeSafely();
            }
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
            this._be.LogMessageEvent( new Microsoft.Build.Framework.BuildMessageEventArgs( message, null, null, Microsoft.Build.Framework.MessageImportance.Low, DateTime.UtcNow ) );
         }

         protected override Assembly LoadAssemblyFromPath( String path )
         {
            return this._loader.LoadFromAssemblyPath( path );
         }

         public static async System.Threading.Tasks.Task<NETStandardExecutionHelper> Create(
            Microsoft.Build.Framework.IBuildEngine taskFactoryLoggingHost,
            XElement taskBodyElement,
            String taskName,
            NuGetResolverWrapper nugetResolver,
            String assemblyPath,
            IDictionary<String, ISet<String>> assemblyPathsBySimpleName
            )
         {
            var retVal = new NETStandardExecutionHelper( taskFactoryLoggingHost, taskName, nugetResolver, assemblyPath, assemblyPathsBySimpleName );
            var loader = await NuGetTaskLoadContext.Create( retVal, nugetResolver, taskBodyElement );
            retVal.SetLoader( loader );
            return retVal;
         }
      }
   }
}
#endif