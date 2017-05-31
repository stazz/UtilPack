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
using Microsoft.Build.Framework;
using NuGet.Common;
using NuGet.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using NuGet.Versioning;
using System.Reflection;
using NuGet.Frameworks;

namespace UtilPack.NuGet.MSBuild
{
   public partial class NuGetTaskRunnerFactory : ITaskFactory
   {
      private const String PACKAGE_ID = "PackageID";
      private const String PACKAGE_VERSION = "PackageVersion";
      private const String ASSEMBLY_PATH = "AssemblyPath";
      private const String REPOSITORY_PATH = "RepositoryPath";

      private NuGetTaskExecutionHelper _helper;
      private Type _taskType;

      public String FactoryName => nameof( NuGetTaskRunnerFactory );

      public Type TaskType
      {
         get
         {
            var retVal = this._taskType;
            if ( retVal == null )
            {
               retVal = this._helper.GetTaskType();
               this._taskType = retVal;
            }

            return retVal;
         }
      }

      public void CleanupTask( Microsoft.Build.Framework.ITask task )
      {
         this._helper.DisposeSafely();
      }

      public Microsoft.Build.Framework.ITask CreateTask(
         Microsoft.Build.Framework.IBuildEngine taskFactoryLoggingHost
         )
      {
         return (ITask) this._helper.CreateTaskInstance( this.TaskType, taskFactoryLoggingHost );
      }

      public Microsoft.Build.Framework.TaskPropertyInfo[] GetTaskParameters()
      {
         return this._helper.GetTaskParameters();
      }

      public Boolean Initialize(
         String taskName,
         IDictionary<String, Microsoft.Build.Framework.TaskPropertyInfo> parameterGroup,
         String taskBody,
         Microsoft.Build.Framework.IBuildEngine taskFactoryLoggingHost
         )
      {
         var taskBodyElement = XElement.Parse( taskBody );
         var nugetResolver = new NuGetBoundResolver();
         var repositoryPaths = taskBodyElement.Elements( "Repositories" ).Select( el => el.Value ).ToArray();
         var resolveInfo = nugetResolver.ResolveNuGetPackages(
            taskBodyElement.Element( PACKAGE_ID )?.Value,
            taskBodyElement.Element( "PackageVersion" )?.Value,
            repositoryPaths,
            false,
            out var package
            );
         var retVal = false;
         if ( resolveInfo != null && resolveInfo.TryGetValue( package, out var assemblyPaths ) )
         {
            var assemblyPath = NuGetBoundResolver.GetAssemblyPathFromNuGetAssemblies( assemblyPaths, package.ExpandedPath, taskBodyElement.Element( "AssemblyPath" )?.Value );

            if ( !String.IsNullOrEmpty( assemblyPath ) )
            {
               this._helper = this.CreateExecutionHelper(
                  taskFactoryLoggingHost,
                  this.ProcessTaskName( taskBodyElement, taskName ),
                  nugetResolver,
                  assemblyPath,
                  GroupDependenciesBySimpleAssemblyName( nugetResolver.ResolveNuGetPackages(
                     package.Id,
                     package.Version.ToNormalizedString(),
                     repositoryPaths,
                     true,
                     out package )
                     )
                  );
               retVal = true;
            }
            else
            {
               taskFactoryLoggingHost.LogErrorEvent(
                  new BuildErrorEventArgs(
                     "Task factory error",
                     "NMSBT003",
                     null,
                     -1,
                     -1,
                     -1,
                     -1,
                     $"Failed to find suitable assembly in {package}.",
                     null,
                     this.FactoryName
                  )
               );
            }
         }
         else
         {
            taskFactoryLoggingHost.LogErrorEvent(
               new BuildErrorEventArgs(
                  "Task factory error",
                  "NMSBT002",
                  null,
                  -1,
                  -1,
                  -1,
                  -1,
                  $"Failed to find main package, check that you have suitable {PACKAGE_ID} element in task body and that package is installed.",
                  null,
                  this.FactoryName
               )
            );
         }
         return retVal;
      }

      private String ProcessTaskName(
         XElement taskBodyElement,
         String taskName
         )
      {
         var overrideTaskName = taskBodyElement.Element( "TaskName" )?.Value;
         return String.IsNullOrEmpty( overrideTaskName ) ? taskName : taskName;
      }

      protected static IDictionary<String, ISet<String>> GroupDependenciesBySimpleAssemblyName(
         IDictionary<LocalPackageInfo, String[]> packageDependencyInfo,
         IDictionary<String, ISet<String>> existing = null
         )
      {
         if ( existing == null )
         {
            existing = new Dictionary<String, ISet<String>>();
         }

         foreach ( var kvp in packageDependencyInfo )
         {
            foreach ( var packageAssemblyPath in kvp.Value )
            {
               var simpleName = System.IO.Path.GetFileNameWithoutExtension( packageAssemblyPath );
               if ( !existing.TryGetValue( simpleName, out ISet<String> allPaths ) )
               {
                  allPaths = new HashSet<String>();
                  existing.Add( simpleName, allPaths );
               }

               allPaths.Add( packageAssemblyPath );
            }
         }
         return existing;
      }
   }

   internal sealed class NuGetBoundResolver
   {
      private readonly NuGetFramework _thisFW;
      private readonly NuGetv3LocalRepository _defaultLocalRepo;
      private readonly NuGetPathResolver _resolver;

      public NuGetBoundResolver()
      {
         this._thisFW = Assembly.GetEntryAssembly().GetNuGetFrameworkFromAssembly();
         this._defaultLocalRepo = new NuGetv3LocalRepository( GetDefaultNuGetLocalRepositoryPath() );
         this._resolver = new NuGetPathResolver( reader =>
         {
            var libs = reader.GetLibItems().ToArray();
            if ( libs.Length == 0 )
            {
               libs = reader.GetBuildItems().ToArray();
            }
            return libs;
         }
         );
      }

      public IDictionary<LocalPackageInfo, String[]> ResolveNuGetPackages(
         String packageID,
         String version,
         String[] repositoryPaths,
         Boolean loadDependencies,
         out LocalPackageInfo package
         )
      {
         IDictionary<LocalPackageInfo, String[]> retVal = null;
         package = null;
         if ( !String.IsNullOrEmpty( packageID ) )
         {
            List<NuGetv3LocalRepository> repositories;
            if ( repositoryPaths.IsNullOrEmpty() )
            {
               repositories = new List<NuGetv3LocalRepository>() { this._defaultLocalRepo };
            }
            else
            {
               repositories = repositoryPaths
                  .Select( p => Path.GetFullPath( p ) )
                  .Where( p => System.IO.Directory.Exists( p ) && !String.Equals( p, this._defaultLocalRepo.RepositoryRoot ) )
                  .Distinct()
                  .Select( p => new NuGetv3LocalRepository( p ) )
                  .Append( this._defaultLocalRepo )
                  .ToList();
            }

            // Try to find our package
            NuGetv3LocalRepository matchingRepo = null;
            if ( !String.IsNullOrEmpty( version ) && NuGetVersion.TryParse( version, out var nugetVersion ) )
            {
               // Find by package id + version combination
               (matchingRepo, package) = repositories
                  .Select( r => (r, r.FindPackage( packageID, nugetVersion )) )
                  .FirstOrDefault();
            }

            if ( matchingRepo == null || package == null )
            {
               // Find by package id, and use newest
               (matchingRepo, package) = repositories
                  .SelectMany( r => r.FindPackagesById( packageID ).Select( p => (r, p) ) )
                  .OrderByDescending( l => l.Item2.Version, VersionComparer.Default )
                  .FirstOrDefault();
            }

            if ( matchingRepo != null && package != null )
            {
               String[] possibleAssemblies;
               if ( loadDependencies )
               {
                  // Set up repositories list
                  if ( repositories.Count > 1 )
                  {
                     repositories.Remove( matchingRepo );
                     repositories.Insert( 0, matchingRepo );
                  }

                  retVal = this._resolver.GetNuGetPackageAssembliesAndDependencies( package, this._thisFW, repositories );
                  possibleAssemblies = retVal.GetOrDefault( package );
               }
               else
               {
                  possibleAssemblies = this._resolver.GetSingleNuGetPackageAssemblies( package, this._thisFW );
                  retVal = new Dictionary<LocalPackageInfo, String[]>( NuGetPathResolver.PackageIDEqualityComparer ) { { package, possibleAssemblies } };
               }
            }
         }

         return retVal;
      }

      private static String GetDefaultNuGetLocalRepositoryPath()
      {
         return Path.Combine( NuGetEnvironment.GetFolderPath( NuGetFolderPath.NuGetHome ), "packages" );
      }

      // This method is in this class since this class has no implemented interfaces and extends System.Object.
      // Therefore using this static method from another appdomain won't cause any assembly resolves.
      public static String GetAssemblyPathFromNuGetAssemblies(
         String[] assemblyPaths,
         String packageExpandedPath,
         String optionalGivenAssemblyPath
         )
      {
         String assemblyPath = null;
         if ( assemblyPaths.Length == 1 || (
               assemblyPaths.Length > 1 // There is more than 1 possible assembly
               && !String.IsNullOrEmpty( ( assemblyPath = optionalGivenAssemblyPath ) ) // AssemblyPath task property was given
               && ( assemblyPath = Path.GetFullPath( ( Path.Combine( packageExpandedPath, assemblyPath ) ) ) ).StartsWith( packageExpandedPath ) // The given assembly path truly resides in the package folder
               ) )
         {
            // TODO maybe check that assembly path is in possibleAssemblies array?
            if ( assemblyPath == null )
            {
               assemblyPath = assemblyPaths[0];
            }
         }
         return assemblyPath;
      }
   }

   internal interface NuGetTaskExecutionHelper : IDisposable
   {
      Type GetTaskType();

      TaskPropertyInfo[] GetTaskParameters();

      Object CreateTaskInstance( Type taskType, IBuildEngine taskFactoryLoggingHost );
   }
}
