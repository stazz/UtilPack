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

using TPropertyInfo = System.ValueTuple<UtilPack.NuGet.MSBuild.WrappedPropertyKind, UtilPack.NuGet.MSBuild.WrappedPropertyInfo, System.Reflection.PropertyInfo>;
using TNuGetPackageResolverCallback = System.Func<System.String, System.String, System.String[], System.Boolean, System.String, System.Reflection.Assembly>;
using TAssemblyByPathResolverCallback = System.Func<System.String, System.Reflection.Assembly>;
using System.Collections.Concurrent;

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
         var nugetResolver = new NuGetBoundResolver(
            taskFactoryLoggingHost,
            this.FactoryName,
            taskBodyElement
            );
         var repositoryPaths = taskBodyElement.Elements( "Repositories" ).Select( el => el.Value ).ToArray();
         var resolveInfo = nugetResolver.ResolveNuGetPackages(
            taskBodyElement.Element( PACKAGE_ID )?.Value,
            taskBodyElement.Element( "PackageVersion" )?.Value,
            repositoryPaths,
            false,
            out var package
            );
         var retVal = false;
         if ( resolveInfo != null && package != null && resolveInfo.TryGetValue( package, out var assemblyPaths ) && !assemblyPaths.IsNullOrEmpty() )
         {
            var assemblyPath = CommonHelpers.GetAssemblyPathFromNuGetAssemblies( assemblyPaths, package.ExpandedPath, taskBodyElement.Element( "AssemblyPath" )?.Value );
            if ( !String.IsNullOrEmpty( assemblyPath ) )
            {
               this._helper = this.CreateExecutionHelper(
                  taskFactoryLoggingHost,
                  taskBodyElement,
                  this.ProcessTaskName( taskBodyElement, taskName ),
                  nugetResolver,
                  assemblyPath,
                  GroupDependenciesBySimpleAssemblyName( nugetResolver.ResolveNuGetPackages(
                     package.Id,
                     package.Version.ToNormalizedString(),
                     repositoryPaths,
                     true,
                     out package )
                     ),
                  repositoryPaths
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


      private const String NUGET_FW = "NuGetFramework";
      internal const String NUGET_FW_PACKAGE_ID = "NuGetFrameworkPackageID";
      internal const String NUGET_FW_PACKAGE_VERSION = "NuGetFrameworkPackageVersion";

      private readonly NuGetFramework _thisFW;
      private readonly NuGetv3LocalRepository _defaultLocalRepo;
      private readonly NuGetPathResolver _resolver;


      public NuGetBoundResolver(
         IBuildEngine be,
         String senderName,
         XElement taskBodyElement
         )
      {
         var nugetFrameworkFromProjectFile = taskBodyElement.Element( NUGET_FW )?.Value;

         this._thisFW = String.IsNullOrEmpty( nugetFrameworkFromProjectFile ) ?
#if NET45
            Assembly.GetEntryAssembly().GetNuGetFrameworkFromAssembly()
#else
            GetNuGetFrameworkForRuntime( be, senderName )
#endif
            : NuGetFramework.ParseFrameworkName( nugetFrameworkFromProjectFile, new DefaultFrameworkNameProvider() )
            ;
         be.LogMessageEvent( new BuildMessageEventArgs(
            $"Using {this._thisFW} as NuGet framework representing this runtime.",
            null,
            senderName,
            MessageImportance.High
            ) );

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

#if !NET45

      private static NuGetFramework GetNuGetFrameworkForRuntime(
         IBuildEngine be,
         String senderName
         )
      {
         var fwName = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
         NuGetFramework retVal = null;
         if ( !String.IsNullOrEmpty( fwName ) && fwName.StartsWith( ".NET Core" ) )
         {
            if ( Version.TryParse( fwName.Substring( 10 ), out var netCoreVersion ) )
            {
               if ( netCoreVersion.Major == 4 )
               {
                  if ( netCoreVersion.Minor == 0 )
                  {
                     retVal = FrameworkConstants.CommonFrameworks.NetCoreApp10;
                  }
                  else if ( netCoreVersion.Minor == 6 )
                  {
                     retVal = FrameworkConstants.CommonFrameworks.NetCoreApp11;
                  }
               }
            }
            else
            {
               be.LogWarningEvent( new BuildWarningEventArgs(
                  "NuGetFrameworkError",
                  "NMSBT004",
                  null,
                  -1,
                  -1,
                  -1,
                  -1,
                  $"Failed to parse version from .NET Core framework \"{fwName}\".",
                  null,
                  senderName
                  ) );
            }
         }
         else
         {
            be.LogWarningEvent( new BuildWarningEventArgs(
               "NuGetFrameworkError",
               "NMSBT003",
               null,
               -1,
               -1,
               -1,
               -1,
               $"Unrecognized framework name: \"{fwName}\", try specifying NuGet framework and package strings that describes this process runtime in <Task> element (using \"{NUGET_FW}\", \"{NUGET_FW_PACKAGE_ID}\", and \"{NUGET_FW_PACKAGE_VERSION}\" elements)!",
               null,
               senderName
               ) );
         }

         if ( retVal == null )
         {
            retVal = FrameworkConstants.CommonFrameworks.NetCoreApp11;
            be.LogWarningEvent( new BuildWarningEventArgs(
               "NuGetFrameworkError",
               "NMSBT005",
               null,
               -1,
               -1,
               -1,
               -1,
               $"Failed to automatically deduct NuGet framework of running process, defaulting to \"{retVal}\". Expect possible failures.",
               null,
               senderName
               ) );
         }

         return retVal;
      }

#endif

      public IDictionary<LocalPackageInfo, String[]> ResolveNuGetPackages(
         String packageID,
         String version,
         String[] repositoryPaths,
         Boolean loadDependencies,
         out LocalPackageInfo package,
         NuGetPathResolver resolverToUse = null
         )
      {
         IDictionary<LocalPackageInfo, String[]> retVal = null;
         package = null;
         if ( !String.IsNullOrEmpty( packageID ) )
         {
            var repositories = this.GetRepositories( repositoryPaths );

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
               var resolver = resolverToUse ?? this._resolver;
               if ( loadDependencies )
               {
                  // Set up repositories list
                  if ( repositories.Count > 1 )
                  {
                     repositories.Remove( matchingRepo );
                     repositories.Insert( 0, matchingRepo );
                  }

                  retVal = resolver.GetNuGetPackageAssembliesAndDependencies( package, this._thisFW, repositories );
               }
               else
               {
                  var possibleAssemblies = resolver.GetSingleNuGetPackageAssemblies( package, this._thisFW );
                  retVal = new Dictionary<LocalPackageInfo, String[]>( NuGetPathResolver.PackageIDEqualityComparer ) { { package, possibleAssemblies } };
               }
            }
         }

         return retVal;
      }

      public List<NuGetv3LocalRepository> GetRepositories(
         String[] repositoryPaths
         )
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

         return repositories;
      }

      private static String GetDefaultNuGetLocalRepositoryPath()
      {
         return Path.Combine( NuGetEnvironment.GetFolderPath( NuGetFolderPath.NuGetHome ), "packages" );
      }


   }

   // These methods are used by both .net45 and .netstandard.
   // This class has no implemented interfaces and extends System.Object.
   // Therefore using this static method from another appdomain won't cause any assembly resolves.
   internal static class CommonHelpers
   {
      internal const String MBF = "Microsoft.Build.Framework.";

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

      public static IDictionary<String, TPropertyInfo> GetPropertyInfoFromType(
         Type type,
         AssemblyName msbuildFrameworkAssemblyName
         )
      {
         // Doing typeof( Microsoft.Build.Framework.ITask ).Assembly.GetName().FullName; will cause MSBuild 14.0 assembly to be loaded in net45 build, if target assembly is .netstandard assembly.
         // This most likely due the fact that net45 build requires msbuild 14.X (msbuild 15.X requires net46).
         // So, just get the msbuildTaskAssemblyName from original appdomain as a parameter to this method.
         // That is why MBF string consts & other helper constructs exist, and why we can't cast stuff directly to Microsoft.Build.Framework types.


         var retVal = new Dictionary<String, TPropertyInfo>();
         foreach ( var property in type.GetRuntimeProperties().Where( p => ( p.GetMethod?.IsPublic ?? false ) && ( p.SetMethod?.IsPublic ?? false ) ) )
         {
            var curProperty = property;
            var propertyType = curProperty.PropertyType;
            var actualType = propertyType;
            if ( actualType.IsArray )
            {
               actualType = actualType.GetElementType();
            }
            WrappedPropertyKind? kind;
            switch ( Type.GetTypeCode( actualType ) )
            {
               case TypeCode.Object:
                  if ( ISMFBType( actualType, msbuildFrameworkAssemblyName ) )
                  {
                     if ( Equals( actualType.FullName, MBF + nameof( Microsoft.Build.Framework.IBuildEngine ) ) )
                     {
                        kind = WrappedPropertyKind.BuildEngine;
                     }
                     else if ( Equals( actualType.FullName, MBF + nameof( Microsoft.Build.Framework.ITaskHost ) ) )
                     {
                        kind = WrappedPropertyKind.TaskHost;
                     }
                     else if ( Equals( actualType.FullName, MBF + nameof( Microsoft.Build.Framework.ITaskItem ) ) )
                     {
                        kind = WrappedPropertyKind.TaskItem;
                     }
                     else if ( Equals( actualType.FullName, MBF + nameof( Microsoft.Build.Framework.ITaskItem2 ) ) )
                     {
                        kind = WrappedPropertyKind.TaskItem2;
                     }
                     else
                     {
                        kind = null;
                     }
                  }
                  else
                  {
                     kind = null;
                  }
                  break;
#if NET45
               case TypeCode.DBNull:
#endif
               case TypeCode.Empty:
                  kind = null;
                  break;
               case TypeCode.String:
                  kind = WrappedPropertyKind.StringNoConversion;
                  break;
               default:
                  kind = WrappedPropertyKind.String;
                  break;
            }

            if ( kind.HasValue )
            {
               WrappedPropertyInfo info;
               var customMBFAttrs = curProperty.GetCustomAttributes( true )
                  .Where( ca => ISMFBType( ca.GetType(), msbuildFrameworkAssemblyName ) )
                  .ToArray();
               if ( customMBFAttrs.Any( ca => Equals( ca.GetType().FullName, MBF + nameof( Microsoft.Build.Framework.RequiredAttribute ) ) ) )
               {
                  info = WrappedPropertyInfo.Required;
               }
               else if ( customMBFAttrs.Any( ca => Equals( ca.GetType().FullName, MBF + nameof( Microsoft.Build.Framework.OutputAttribute ) ) ) )
               {
                  info = WrappedPropertyInfo.Out;
               }
               else
               {
                  info = WrappedPropertyInfo.None;
               }

               retVal.Add( curProperty.Name, (kind.Value, info, curProperty) );
            }
         }

         return retVal;
      }

      private static Boolean ISMFBType( Type type, AssemblyName mfbAssembly )
      {
         var an = type
#if !NET45
                     .GetTypeInfo()
#endif
                     .Assembly.GetName();
         Byte[] pk;
         return String.Equals( an.Name, mfbAssembly.Name )
            && ( pk = an.GetPublicKeyToken() ) != null
            && mfbAssembly.GetPublicKeyToken().SequenceEqual( pk );
      }
   }

   internal enum WrappedPropertyKind
   {
      String,
      StringNoConversion,
      TaskItem,
      TaskItem2,
      BuildEngine,
      TaskHost
   }

   internal enum WrappedPropertyInfo
   {
      None,
      Required,
      Out
   }


   internal interface NuGetTaskExecutionHelper : IDisposable
   {
      Type GetTaskType();

      TaskPropertyInfo[] GetTaskParameters();

      Object CreateTaskInstance( Type taskType, IBuildEngine taskFactoryLoggingHost );
   }

   internal abstract class CommonAssemblyRelatedHelper : IDisposable
   {
      private readonly ConcurrentDictionary<String, ConcurrentDictionary<String, Lazy<Assembly>>> _assemblyPathsBySimpleName; // We will get multiple requests to load same assembly, so cache them
      //private readonly String _thisAssemblyName;
      private readonly NuGetResolverWrapper _resolver;
      private readonly String _targetAssemblyPath;
      private readonly String _taskName;

      protected CommonAssemblyRelatedHelper(
         IDictionary<String, ISet<String>> assemblyPathsBySimpleName,
         NuGetResolverWrapper resolver,
         String targetAssemblyPath,
         String taskName
         )
      {
         this._assemblyPathsBySimpleName = new ConcurrentDictionary<String, ConcurrentDictionary<String, Lazy<Assembly>>>( assemblyPathsBySimpleName.Select(
            kvp => new KeyValuePair<String, ConcurrentDictionary<String, Lazy<Assembly>>>(
               kvp.Key,
               new ConcurrentDictionary<String, Lazy<Assembly>>( kvp.Value.Select( fullPath => new KeyValuePair<String, Lazy<Assembly>>( fullPath, new Lazy<Assembly>( () => this.LoadAssemblyFromPath( fullPath ), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) ) ) )
               )
            ) );
         this._resolver = resolver;
         this._targetAssemblyPath = targetAssemblyPath;
         this._taskName = taskName;
         //         this._thisAssemblyName = this.GetType()
         //#if NETSTANDARD1_5
         //            .GetTypeInfo()
         //#endif
         //            .Assembly.FullName;
      }

      public virtual void Dispose()
      {
         this._assemblyPathsBySimpleName.Clear();
      }

      internal protected Assembly PerformAssemblyResolve( AssemblyName assemblyName )
      {
         Assembly retVal;
         //         if ( String.Equals( this._thisAssemblyName, assemblyName.FullName ) )
         //         {
         //            // Happens when calling Initialize method (because the original appdomain has this assembly loaded "normally", but this app-domain has this assembly loaded via file path because of CreateInstanceFrom method.)
         //            retVal = this.GetType()
         //#if NETSTANDARD1_5
         //            .GetTypeInfo()
         //#endif
         //               .Assembly;
         //         }
         //         else 
         if ( this._assemblyPathsBySimpleName.TryGetValue( assemblyName.Name, out var assemblyLazies ) )
         {
            retVal = assemblyLazies.FirstOrDefault( kvp =>
            {
               var defName = kvp.Value.IsValueCreated ? kvp.Value.Value.GetName() :
#if !NET45
               System.Runtime.Loader.AssemblyLoadContext
#else
               AssemblyName
#endif
               .GetAssemblyName( kvp.Key );

               return AssemblyNamesMatch( assemblyName, defName );
            } ).Value?.Value;

            this.LogResolveMessage( retVal == null ?
               $"Assembly reference did not match definition for \"{assemblyName}\", considered \"{String.Join( ";", assemblyLazies.Keys )}\"." :
               $"Found \"{assemblyName}\" by simple name \"{assemblyName.Name}\" in \"{retVal.CodeBase}\"." );
         }
         else
         {
            this.LogResolveMessage( $"Failed to find \"{assemblyName}\" by simple name \"{assemblyName.Name}\"." );
            retVal = null;
         }
         return retVal;
      }

      private static Boolean AssemblyNamesMatch(
         AssemblyName reference,
         AssemblyName definition
         )
      {
         String refStr; String defStr;
         if ( reference.Flags.HasFlag( AssemblyNameFlags.Retargetable ) )
         {
            refStr = reference.Name;
            defStr = definition.Name;
         }
         else
         {
            refStr = reference.FullName;
            defStr = reference.FullName;
         }

         return String.Equals( refStr, defStr );
      }

      internal protected Type LoadTaskType()
      {
         var taskAssembly = this.PerformAssemblyResolve(
#if !NET45
            System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName
#else
            AssemblyName.GetAssemblyName
#endif
            ( this._targetAssemblyPath )
            );

         return taskAssembly.GetType( this._taskName, false, false );
      }

      internal protected Object PerformCreateTaskInstance(
         Type type
         )
      {
         var ctors = type
#if !NET45
            .GetTypeInfo()
#endif
            .GetConstructors();
         ConstructorInfo matchingCtor = null;
         Object[] ctorParams = null;
         if ( ctors.Length > 0 )
         {
            var ctorInfo = new Dictionary<Int32, IDictionary<ISet<Type>, ConstructorInfo>>();

            foreach ( var ctor in ctors )
            {
               var paramz = ctor.GetParameters();
               ctorInfo
                  .GetOrAdd_NotThreadSafe( paramz.Length, pl => new Dictionary<ISet<Type>, ConstructorInfo>( SetEqualityComparer<Type>.DefaultEqualityComparer ) )
                  .Add( new HashSet<Type>( paramz.Select( p => p.ParameterType ) ), ctor );
            }

            if (
               ctorInfo.TryGetValue( 2, out var curInfo )
               && curInfo.TryGetValue( new HashSet<Type>() { typeof( TNuGetPackageResolverCallback ), typeof( TAssemblyByPathResolverCallback ) }, out matchingCtor )
               )
            {
               ctorParams = new Object[2];
               ctorParams[Array.FindIndex( matchingCtor.GetParameters(), p => p.ParameterType.Equals( typeof( TNuGetPackageResolverCallback ) ) )] = (TNuGetPackageResolverCallback) this.LoadNuGetAssembly;
               ctorParams[Array.FindIndex( matchingCtor.GetParameters(), p => p.ParameterType.Equals( typeof( TAssemblyByPathResolverCallback ) ) )] = (TAssemblyByPathResolverCallback) this.LoadOtherAssembly;
            }
            else if ( ctorInfo.TryGetValue( 1, out curInfo ) )
            {
               if ( curInfo.TryGetValue( new HashSet<Type>( typeof( TNuGetPackageResolverCallback ).Singleton() ), out matchingCtor ) )
               {
                  ctorParams = new Object[] { (TNuGetPackageResolverCallback) this.LoadNuGetAssembly };
               }
               else if ( curInfo.TryGetValue( new HashSet<Type>( typeof( TAssemblyByPathResolverCallback ).Singleton() ), out matchingCtor ) )
               {
                  ctorParams = new Object[] { (TAssemblyByPathResolverCallback) this.LoadOtherAssembly };
               }
            }
            else if ( ctorInfo.TryGetValue( 0, out curInfo ) )
            {
               matchingCtor = curInfo.Values.First();
            }
         }

         if ( matchingCtor == null )
         {
            throw new Exception( $"No public suitable constructors found for type {type.AssemblyQualifiedName}." );
         }

         return matchingCtor.Invoke( ctorParams );
      }

      // This method can get called by target task to dynamically load nuget assemblies.
      private Assembly LoadNuGetAssembly(
         String packageID,
         String packageVersion,
         String[] repositories,
         Boolean loadDependencies,
         String assemblyPath
         )
      {
         // TODO Path.GetFileNameWithoutExtension( curPath ) should be replaced with AssemblyName.GetAssemblyName( String path ) for kinky situations when assembly name is with different casing than its file name.
         // Obviously, this slows down things by a lot, and will change data structures a bit, but it should be done at some point.

         var assemblyInfos = this._resolver.ResolveNuGetPackageAssemblies( packageID, packageVersion, repositories, loadDependencies, out var packageKey, out var packagePath );
         Assembly retVal = null;
         if ( assemblyInfos != null )
         {
            var assembliesBySimpleName = this._assemblyPathsBySimpleName;
            foreach ( var kvp in assemblyInfos )
            {
               foreach ( var nugetAssemblyPath in kvp.Value )
               {
                  var curPath = nugetAssemblyPath;
                  assembliesBySimpleName
                     .GetOrAdd( Path.GetFileNameWithoutExtension( curPath ), sn => new ConcurrentDictionary<String, Lazy<Assembly>>() )
                     .TryAdd( curPath, new Lazy<Assembly>( () => this.LoadAssemblyFromPath( curPath ), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) );
               }
            }

            var possibleAssemblyPaths = assemblyInfos[packageKey];
            assemblyPath = CommonHelpers.GetAssemblyPathFromNuGetAssemblies( possibleAssemblyPaths, packagePath, assemblyPath );
            if ( !String.IsNullOrEmpty( assemblyPath ) )
            {
               retVal = assembliesBySimpleName
                  [Path.GetFileNameWithoutExtension( assemblyPath )]
                  [assemblyPath]
                  .Value;
            }
         }

         return retVal;
      }

      // This method can get called by target task to dynamically load assemblies by path.
      private Assembly LoadOtherAssembly(
         String assemblyPath
         )
      {
         assemblyPath = Path.GetFullPath( assemblyPath );
         Assembly retVal = null;
         if ( File.Exists( assemblyPath ) )
         {
            retVal = this._assemblyPathsBySimpleName
               .GetOrAdd( Path.GetFileNameWithoutExtension( assemblyPath ), ap => new ConcurrentDictionary<String, Lazy<Assembly>>() )
               .GetOrAdd( assemblyPath, ap => new Lazy<Assembly>( () => this.LoadAssemblyFromPath( ap ), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) )
               .Value;
         }
         return retVal;
      }

      protected abstract void LogResolveMessage( String message );

      protected abstract Assembly LoadAssemblyFromPath( String path );
   }

   // Instances of this class reside in task factory app domain.
   internal sealed class NuGetResolverWrapper
#if NET45
      : MarshalByRefObject
#endif
   {
      private readonly NuGetBoundResolver _resolver;

      public NuGetResolverWrapper( NuGetBoundResolver resolver )
      {
         this._resolver = resolver;
      }

      public IDictionary<String, String[]> ResolveNuGetPackageAssemblies(
         String packageID,
         String packageVersion,
         String[] repositoryPaths,
         Boolean loadDependencies,
         out String givenPackageString,
         out String packageExpandedPath
         )
      {
         var retVal = this._resolver.ResolveNuGetPackages( packageID, packageVersion, repositoryPaths, loadDependencies, out var package );
         givenPackageString = package?.ToString();
         packageExpandedPath = package?.ExpandedPath;
         return retVal.ToDictionary( kvp => kvp.Key.ToString(), kvp => kvp.Value );
      }
   }
}
