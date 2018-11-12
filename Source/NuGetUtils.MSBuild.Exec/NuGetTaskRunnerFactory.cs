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
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.RuntimeModel;
using NuGet.Versioning;
using NuGetUtils.Lib.AssemblyResolving;
using NuGetUtils.Lib.MSBuild;
using NuGetUtils.Lib.Restore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml.Linq;
using UtilPack;


namespace NuGetUtils.MSBuild.Exec
{
   using TPropertyInfo = ValueTuple<WrappedPropertyKind, WrappedPropertyTypeModifier, WrappedPropertyInfo, PropertyInfo>;
   using TTaskPropertyInfoDictionary = IDictionary<String, ValueTuple<WrappedPropertyKind, WrappedPropertyTypeModifier, WrappedPropertyInfo>>;

   internal struct RestorerCacheKey : IEquatable<RestorerCacheKey>
   {
      public RestorerCacheKey(
         NuGetFramework framework,
         ISettings settings,
         String runtimeID,
         String runtimeGraphPackageID
         )
      {
         this.NuGetFramework = framework;
         this.PackageFolders = new HashSet<String>( SettingsUtility.GetFallbackPackageFolders( settings ).Prepend( SettingsUtility.GetGlobalPackagesFolder( settings ) ) );
         this.RuntimeID = runtimeID;
         this.RuntimeGraphPackageID = runtimeGraphPackageID;
      }

      public Boolean Equals( RestorerCacheKey other )
      {
         return ( this.NuGetFramework?.Equals( other.NuGetFramework ) ?? ( other.NuGetFramework is null ) )
            && String.Equals( this.RuntimeID, other.RuntimeID )
            && String.Equals( this.RuntimeGraphPackageID, other.RuntimeGraphPackageID )
            && ( this.PackageFolders?.SetEquals( other.PackageFolders ?? Empty<String>.Enumerable ) ?? ( other.PackageFolders is null ) );
      }

      public override Boolean Equals( Object obj )
      {
         return obj is RestorerCacheKey && this.Equals( (RestorerCacheKey) obj );
      }

      public override Int32 GetHashCode()
      {
         return this.NuGetFramework?.GetHashCode() ?? 0;
      }

      public NuGetFramework NuGetFramework { get; }

      public ISet<String> PackageFolders { get; }

      public String RuntimeID { get; }

      public String RuntimeGraphPackageID { get; }
   }

   // On first task create the task type is dynamically generated, and app domain initialized
   // On cleanup, app domain will be unloaded, but task type kept
   // On subsequent uses, app-domain will be re-initialized and unloaded again, but the generated type prevails.
   public partial class NuGetTaskRunnerFactory : ITaskFactory
   {

      private sealed class TaskReferenceHolderInfo : IDisposable
      {
         private readonly Lazy<TTaskPropertyInfoDictionary> _propertyInfo;
         private readonly Action _dispose;

         public TaskReferenceHolderInfo(
            TaskReferenceHolder taskRef,
            ResolverLogger resolverLogger,
            Action dispose
            )
         {
            this.TaskReference = taskRef;
            this.Logger = resolverLogger;
            this._dispose = dispose;
            this._propertyInfo = new Lazy<TTaskPropertyInfoDictionary>( () => taskRef.GetPropertyInfo().ToDictionary( kvp => kvp.Key, kvp => TaskReferenceHolder.DecodeKindAndInfo( kvp.Value ) ) );
         }

         public TaskReferenceHolder TaskReference { get; }

         public ResolverLogger Logger { get; }

         public TTaskPropertyInfoDictionary PropertyInfo => this._propertyInfo.Value;

         public void Dispose()
         {
            this._dispose?.Invoke();
         }
      }

      private sealed class TaskUsageInfo
      {
         public TaskUsageInfo(
            ReadOnlyResettableLazy<TaskReferenceHolderInfo> referenceHolder,
            BoundRestoreCommandUser restorer,
            GenericEventHandler<PackageSpecCreatedArgs> packageSpecCreatedHandler
            )
         {
            this.ReferenceHolder = referenceHolder;
            this.Restorer = restorer;
            this.PackageSpecCreatedHandler = packageSpecCreatedHandler;
         }

         public ReadOnlyResettableLazy<TaskReferenceHolderInfo> ReferenceHolder { get; }

         public BoundRestoreCommandUser Restorer { get; }

         public GenericEventHandler<PackageSpecCreatedArgs> PackageSpecCreatedHandler { get; }
      }

      // Currently only way to share state between task factory usage in different build files.
      private static readonly ConcurrentDictionary<RestorerCacheKey, BoundRestoreCommandUser> RestorersCache = new ConcurrentDictionary<RestorerCacheKey, BoundRestoreCommandUser>();

      private const String PACKAGE_ID = "PackageID";
      private const String PACKAGE_ID_IS_SELF = "PackageIDIsSelf";
      private const String PACKAGE_VERSION = "PackageVersion";
      private const String ASSEMBLY_PATH = "AssemblyPath";
      private const String NUGET_FW = "NuGetFramework";
      private const String NUGET_FW_VERSION = "NuGetFrameworkVersion";
      private const String NUGET_FW_PACKAGE_ID = "NuGetFrameworkPackageID";
      private const String NUGET_FW_PACKAGE_VERSION = "NuGetFrameworkPackageVersion";
      private const String NUGET_RID = "NuGetPlatformRID";
      private const String NUGET_RID_CATALOG_PACKAGE_ID = "NuGetPlatformRIDCatalogPackageID";
      private const String NUGET_CONFIG_FILE = "NuGetConfigurationFile";
      private const String COPY_TO_TEMPORARY_FOlDER_BEFORE_LOAD = "CopyToFolderBeforeLoad";
      private const String TASK_NAME = "TaskName";
      private const String UNMANAGED_ASSEMBLIES_MAP = "UnmanagedAssemblyReferenceMap";
      private const String NO_DEDICATED_APPDOMAIN = "NoDedicatedAppDomain";

      private const String MATCH_PLATFORM = "MatchPlatform";
      private const String EXCEPT_PLATFORM = "ExceptPlatform";
      private const String UNMANAGED_ASSEMBLY_REF = "AssemblyReference";
      private const String MAPPED_NAME = "MappedTo";

      private const String CACHE_USAGE = "CacheUsage";
      private const String CACHE_USAGE_ENABLED = "Enabled";
      private const String CACHE_USAGE_STRICT = "Strict";
      private const String CACHE_USAGE_DISABLED = "Disabled";

      //private const String KNOWN_SDK_PACKAGE = "KnownSDKPackage";

      // We will re-create anything that needs re-creating between mutiple task usages from this same lazy.
      private TaskUsageInfo _helper;

      // We will generate task type only exactly once, no matter how many times the actual task is created.
      private readonly Lazy<Type> _taskType;

      public NuGetTaskRunnerFactory()
      {
         this._taskType = new Lazy<Type>( () =>
         {
            var holder = this._helper.ReferenceHolder.Value;
            return TaskCodeGenerator.GenerateTaskType( (holder.TaskReference.IsCancelable, holder.PropertyInfo) );
         } );
      }

      public String FactoryName => nameof( NuGetTaskRunnerFactory );

      public Type TaskType
      {
         get
         {
            return this._taskType.Value;
         }
      }

      public void CleanupTask( ITask task )
      {
         var info = this._helper;

         info.Restorer.PackageSpecCreated -= info.PackageSpecCreatedHandler;
         var holder = info.ReferenceHolder;
         if ( holder.IsValueCreated && holder.Value.TaskReference.TaskUsesDynamicLoading )
         {
            // In .NET Desktop, task factory logger seems to become invalid almost immediately after initialize method, so...
            // Don't log.

            //this._logger.LogMessageEvent( new BuildMessageEventArgs(
            //   "Cleaning up task since it was detected to be using dynamic loading.",
            //   null,
            //   this.FactoryName,
            //   MessageImportance.Normal,
            //   DateTime.UtcNow
            //   ) );

            // Reset tasks that do dynamic NuGet package assembly loading
            // On .NET Desktop, this will cause app domain unload
            // On .NET Core, this will cause assembly load context to be disposed
            holder.Value.DisposeSafely();
            holder.Reset();

         }
      }

      public ITask CreateTask(
         IBuildEngine taskFactoryLoggingHost
         )
      {
         var holder = this._helper.ReferenceHolder.Value;
         return (ITask) this._taskType.Value.GetConstructors()[0].Invoke( new Object[] { holder.TaskReference, holder.Logger } );
      }

      public TaskPropertyInfo[] GetTaskParameters()
      {
         return this._helper.ReferenceHolder.Value.PropertyInfo
            .Select( kvp =>
            {
               var propType = TaskCodeGenerator.GetPropertyType( kvp.Value.Item1, kvp.Value.Item2 );
               var info = kvp.Value.Item3;
               return propType == null ?
                  null :
                  new Microsoft.Build.Framework.TaskPropertyInfo( kvp.Key, propType, info == WrappedPropertyInfo.Out, info == WrappedPropertyInfo.Required );
            } )
            .Where( propInfo => propInfo != null )
            .ToArray();
      }

      public Boolean Initialize(
         String taskName,
         IDictionary<String, TaskPropertyInfo> parameterGroup,
         String taskBody,
         IBuildEngine taskFactoryLoggingHost
         )
      {
         var retVal = false;
         var nugetLogger = new NuGetMSBuildLogger(
            "NR0001",
            "NR0002",
            this.FactoryName,
            this.FactoryName,
            taskFactoryLoggingHost
            );
         try
         {
            //this._logger = taskFactoryLoggingHost;

            var taskBodyElement = XElement.Parse( taskBody );

            // Nuget stuff
            var thisFW = NuGetUtility.TryAutoDetectThisProcessFramework( (taskBodyElement.ElementAnyNS( NUGET_FW )?.Value, taskBodyElement.ElementAnyNS( NUGET_FW_VERSION )?.Value) );

            var nugetSettings = NuGetUtility.GetNuGetSettingsWithDefaultRootDirectory(
               Path.GetDirectoryName( taskFactoryLoggingHost.ProjectFileOfTaskNode ),
               taskBodyElement.ElementAnyNS( NUGET_CONFIG_FILE )?.Value
               );
            var runtimeIdentifier = ( taskBodyElement.ElementAnyNS( NUGET_RID )?.Value ).DefaultIfNullOrEmpty( () => NuGetUtility.TryAutoDetectThisProcessRuntimeIdentifier() );
            var runtimeGraph = ( taskBodyElement.ElementAnyNS( NUGET_RID_CATALOG_PACKAGE_ID )?.Value ).DefaultIfNullOrEmpty( BoundRestoreCommandUser.DEFAULT_RUNTIME_GRAPH_PACKAGE_ID );

            BoundRestoreCommandUser nugetResolver;
            BoundRestoreCommandUser RestorerFactory() => new BoundRestoreCommandUser(
               nugetSettings,
               thisFramework: thisFW,
               nugetLogger: nugetLogger,
               runtimeIdentifier: runtimeIdentifier,
               runtimeGraph: runtimeGraph
               );
            var cachePolicy = ( taskBodyElement.ElementAnyNS( CACHE_USAGE )?.Value ).DefaultIfNullOrEmpty( CACHE_USAGE_ENABLED );
            if ( String.Equals( cachePolicy, CACHE_USAGE_DISABLED, StringComparison.OrdinalIgnoreCase ) )
            {
               nugetResolver = RestorerFactory();
            }
            else
            {
               var cacheKey = String.Equals( cachePolicy, CACHE_USAGE_STRICT, StringComparison.OrdinalIgnoreCase ) ?
                  new RestorerCacheKey( thisFW, nugetSettings, runtimeIdentifier, runtimeGraph ) :
                  default;
               var createdNew = false;
               nugetResolver = RestorersCache.GetOrAdd( cacheKey, _ =>
               {
                  createdNew = true;
                  return RestorerFactory();
               } );

               if ( !createdNew )
               {
                  taskFactoryLoggingHost.LogMessageEvent( new BuildMessageEventArgs(
                     "Using cached NuGet restorer from previous task factory usage",
                     null,
                     null,
                     MessageImportance.Normal
                     ) );
               }

            }

            // TODO this will cause some logging to become garbled in concurrent scenarios.
            // Luckily, I am quite sure MSBuild runs concurrently only on process level, and within the same process, the execution is always sequential.
            // But this will need to be addressed at some point; then logging and event handler should be specified as parameters to RestoreIfNeeded method.
            // Those would then override whatever logging and event handlers are on BoundRestoreCommandUser object itself.
            ( (NuGetMSBuildLogger) nugetResolver.NuGetLogger ).BuildEngine = taskFactoryLoggingHost;

            taskFactoryLoggingHost.LogMessageEvent( new BuildMessageEventArgs(
               $"Detected current NuGet framework to be \"{thisFW}\", with RID \"{nugetResolver.RuntimeIdentifier}\", and local repositories: {String.Join( ";", nugetResolver.LocalRepositories.Keys )}.",
               null,
               null,
               MessageImportance.Normal
               ) );


            // Restore task package
            // TODO cancellation token source + cancel on Ctrl-C (since Inititalize method offers no support for asynchrony/cancellation)
            (var packageID, var packageVersion) = this.GetPackageIDAndVersion( taskFactoryLoggingHost, taskBodyElement, nugetResolver );
            if ( !String.IsNullOrEmpty( packageID ) )
            {
               var taskProjectFilePath = taskFactoryLoggingHost.ProjectFileOfTaskNode;
               void OnPackageSpecCreation( PackageSpecCreatedArgs pscArgs )
               {
                  var pSpec = pscArgs.PackageSpec;
                  pSpec.FilePath = taskProjectFilePath;
               };
               nugetResolver.PackageSpecCreated += OnPackageSpecCreation;
               using ( new UsingHelper( () => { if ( !retVal ) { nugetResolver.PackageSpecCreated -= OnPackageSpecCreation; } } ) )
               using ( var cancelSource = new CancellationTokenSource() )
               {
                  void OnCancel( Object sender, ConsoleCancelEventArgs args )
                  {
                     cancelSource.Cancel();
                  }
                  Console.CancelKeyPress += OnCancel;
                  using ( new UsingHelper( () => Console.CancelKeyPress -= OnCancel ) )
                  {
                     var restoreResult = nugetResolver.RestoreIfNeeded(
                        packageID,
                        packageVersion,
                        cancelSource.Token
                        ).GetAwaiter().GetResult();
                     if ( restoreResult != null
                        && !String.IsNullOrEmpty( ( packageVersion = restoreResult.Libraries.Where( lib => String.Equals( lib.Name, packageID ) ).FirstOrDefault()?.Version?.ToNormalizedString() ) )
                        )
                     {
                        GetFileItemsDelegate getFiles = ( rGraph, rid, lib, libs ) => GetSuitableFiles( thisFW, rGraph, rid, lib, libs );
                        // On Desktop we must always load everything, since it's possible to have assemblies compiled against .NET Standard having references to e.g. System.IO.FileSystem.dll, which is not present in GAC
#if !IS_NETSTANDARD
                        AppDomainSetup appDomainSetup = null;
#else

                        var sdkPackageID = thisFW.GetSDKPackageID( taskBodyElement.ElementAnyNS( NUGET_FW_PACKAGE_ID )?.Value );
                        var sdkRestoreResult = nugetResolver.RestoreIfNeeded(
                            sdkPackageID,
                            thisFW.GetSDKPackageVersion( sdkPackageID, taskBodyElement.ElementAnyNS( NUGET_FW_PACKAGE_VERSION )?.Value ),
                            cancelSource.Token
                            ).GetAwaiter().GetResult();
#endif

                        var taskAssemblies = nugetResolver.ExtractAssemblyPaths(
                           restoreResult,
                           getFiles
                           )[packageID];
                        var assemblyPathHint = taskBodyElement.ElementAnyNS( ASSEMBLY_PATH )?.Value;
                        var assemblyPath = NuGetUtility.GetAssemblyPathFromNuGetAssemblies(
                           packageID,
                           taskAssemblies.Assemblies,
                           assemblyPathHint
                           );
                        if ( !String.IsNullOrEmpty( assemblyPath ) )
                        {
                           taskName = this.ProcessTaskName( taskBodyElement, taskName );
                           var givenTempFolder = taskBodyElement.ElementAnyNS( COPY_TO_TEMPORARY_FOlDER_BEFORE_LOAD )?.Value;
                           var wasBoolean = false;
                           var noTempFolder = String.IsNullOrEmpty( givenTempFolder ) || ( ( wasBoolean = Boolean.TryParse( givenTempFolder, out var createTempFolder ) ) && !createTempFolder );
                           var explicitTempFolder = !noTempFolder && !wasBoolean ? givenTempFolder : null;
                           this._helper = new TaskUsageInfo(
                              LazyFactory.NewReadOnlyResettableLazy( () =>
                              {
                                 try
                                 {

                                    var tempFolder = noTempFolder ?
                                       null :
                                       ( explicitTempFolder ?? Path.Combine( Path.GetTempPath(), "NuGetAssemblies_" + packageID + "_" + packageVersion + "_" + ( Guid.NewGuid().ToString() ) ) );
                                    if ( !String.IsNullOrEmpty( tempFolder ) && ( String.IsNullOrEmpty( explicitTempFolder ) || !Directory.Exists( explicitTempFolder ) ) )
                                    {
                                       Directory.CreateDirectory( tempFolder );
                                    }

                                    return this.CreateExecutionHelper(
                                       taskName,
                                       taskBodyElement,
                                       packageID,
                                       packageVersion,
                                       assemblyPath,
                                       assemblyPathHint,
                                       nugetResolver,
                                       new ResolverLogger( nugetLogger ),
                                       getFiles,
                                       tempFolder,
#if !IS_NETSTANDARD
                                 ref appDomainSetup
#else
                                 sdkRestoreResult
#endif
                              );
                                 }
                                 catch ( Exception exc )
                                 {
                                    Console.Error.WriteLine( "Exception when creating task: " + exc );
                                    throw;
                                 }
                              }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ),
                              nugetResolver,
                              OnPackageSpecCreation
                              );
                           // Force initialization right here, in order to provide better logging (since taskFactoryLoggingHost passed to this method is not useable once this method completes)
                           var dummy = this._helper.ReferenceHolder.Value.TaskReference;
                           retVal = true;
                        }
                        else
                        {
                           nugetResolver.LogAssemblyPathResolveError( packageID, taskAssemblies.Assemblies, assemblyPathHint, assemblyPath );
                           taskFactoryLoggingHost.LogErrorEvent(
                              new BuildErrorEventArgs(
                                 "Task factory",
                                 "NMSBT004",
                                 null,
                                 -1,
                                 -1,
                                 -1,
                                 -1,
                                 $"Failed to find suitable assembly in {packageID}@{packageVersion}.",
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
                              "Task factory",
                              "NMSBT003",
                              null,
                              -1,
                              -1,
                              -1,
                              -1,
                              $"Failed to find main package {packageID}@{packageVersion}.",
                              null,
                              this.FactoryName
                           )
                        );
                     }
                  }
               }
            }
         }
         catch ( Exception exc )
         {
            taskFactoryLoggingHost.LogErrorEvent( new BuildErrorEventArgs(
               "Task factory",
               "NMSBT001",
               null,
               -1,
               -1,
               -1,
               -1,
               $"Exception in initialization: {exc}",
               null,
               this.FactoryName
               ) );
         }

         // We can't use IBuildEngine given to this method once it completes.
         nugetLogger.BuildEngine = null;
         return retVal;
      }

      private (String, String) GetPackageIDAndVersion(
         IBuildEngine taskFactoryLoggingHost,
         XElement taskBodyElement,
         BoundRestoreCommandUser restorer
         )
      {
         const String SELF = "self";

         var packageID = taskBodyElement.ElementAnyNS( PACKAGE_ID )?.Value;
         var stringWasSelf = false;
         global::NuGet.Packaging.Core.PackageIdentity selfPackageIdentity = null;
         var projectFile = taskFactoryLoggingHost.ProjectFileOfTaskNode;
         var packageIDWasSelfValue = taskBodyElement.ElementAnyNS( PACKAGE_ID_IS_SELF )?.Value;
         if ( String.IsNullOrEmpty( packageID ) )
         {
            stringWasSelf = packageIDWasSelfValue?.ParseAsBooleanSafe() ?? false;
            if ( stringWasSelf )
            {
               // Package ID was specified as self
               if ( String.IsNullOrEmpty( projectFile ) )
               {
                  taskFactoryLoggingHost.LogErrorEvent(
                     new BuildErrorEventArgs(
                        "Task factory",
                        "NMSBT005",
                        null,
                        -1,
                        -1,
                        -1,
                        -1,
                        $"The \"{PACKAGE_ID_IS_SELF}\" element is not supported when the caller file of this task factory is not known.",
                        null,
                        this.FactoryName
                     )
                  );
               }
               else
               {
                  projectFile = Path.GetFullPath( projectFile );
                  // The usage of this task factory comes from the package itself, deduce the package ID
                  selfPackageIdentity = SearchWithinNuGetRepository(
                     projectFile,
                     restorer.LocalRepositories
                        .FirstOrDefault( kvp => projectFile.StartsWith( Path.GetFullPath( kvp.Key ) ) )
                        .Value
                        ?.RepositoryRoot
                     );

                  if ( selfPackageIdentity == null )
                  {
                     // Failed to deduce this package ID
                     // No PackageID element and no PackageIDIsSelf element either
                     taskFactoryLoggingHost.LogErrorEvent(
                        new BuildErrorEventArgs(
                           "Task factory",
                           "NMSBT007",
                           null,
                           -1,
                           -1,
                           -1,
                           -1,
                           $"Failed to deduce self package ID from file {projectFile}.",
                           null,
                           this.FactoryName
                        )
                     );
                  }
                  else
                  {
                     packageID = selfPackageIdentity.Id;
                  }


               }
            }
            else
            {
               // No PackageID element and no PackageIDIsSelf element either
               taskFactoryLoggingHost.LogErrorEvent(
                     new BuildErrorEventArgs(
                        "Task factory",
                        "NMSBT002",
                        null,
                        -1,
                        -1,
                        -1,
                        -1,
                        $"Failed to find main package, check that you have suitable \"{PACKAGE_ID}\" or \"{PACKAGE_ID_IS_SELF}\" element in task body.",
                        null,
                        this.FactoryName
                     )
                  );
            }
         }
         else if ( !String.IsNullOrEmpty( packageIDWasSelfValue ) )
         {
            packageID = null;
            taskFactoryLoggingHost.LogErrorEvent(
               new BuildErrorEventArgs(
                  "Task factory",
                  "NMSBT008",
                  null,
                  -1,
                  -1,
                  -1,
                  -1,
                  $"The parameters \"{PACKAGE_ID}\" and \"{PACKAGE_ID_IS_SELF}\" are mutually exclusive, please specify exactly one of them.",
                  null,
                  this.FactoryName
               )
            );
         }

         String packageVersion = null;
         if ( !String.IsNullOrEmpty( packageID ) )
         {

            packageVersion = taskBodyElement.ElementAnyNS( PACKAGE_VERSION )?.Value;
            if ( ( String.IsNullOrEmpty( packageVersion ) && stringWasSelf ) || ( stringWasSelf = String.Equals( packageVersion, SELF, StringComparison.OrdinalIgnoreCase ) ) )
            {
               // Instead of floating version, we need to deduce our version
               NuGetVersion deducedVersion = null;
               if ( selfPackageIdentity == null )
               {
                  // <PackageID> was specified normally, and <PackageVersion> was self
                  var localPackage = restorer.LocalRepositories.Values
                     .SelectMany( lr => lr.FindPackagesById( packageID ) )
                     .Where( lp => projectFile.StartsWith( lp.ExpandedPath ) )
                     .FirstOrDefault();
                  if ( localPackage == null )
                  {
                     taskFactoryLoggingHost.LogErrorEvent(
                        new BuildErrorEventArgs(
                           "Task factory",
                           "NMSBT009",
                           null,
                           -1,
                           -1,
                           -1,
                           -1,
                           $"Failed to find any package with ID {packageID} which would have {projectFile} stored within it.",
                           null,
                           this.FactoryName
                        )
                     );
                  }
                  else
                  {
                     deducedVersion = localPackage.Version;
                  }
               }
               else
               {
                  // <PackageIDIsSelf> was specified, and no version was specified
                  deducedVersion = selfPackageIdentity.Version;
               }

               packageVersion = deducedVersion?.ToNormalizedString();
            }
         }

         return (packageID, packageVersion);
      }

      private static PackageIdentity SearchWithinNuGetRepository(
         String projectFile,
         String localRepoRoot
         )
      {
         // Both V2 and V3 repositories have .nupkg file in its package root directory, so we search for that.
         // But if localRepoRoot is specified, then we assume it is for V3 repository, so we will also require .nuspec file to be present.

         var root = Path.GetPathRoot( projectFile );
         var nupkgFileFilter = "*" + PackagingCoreConstants.NupkgExtension;
         var nuspecFileFilter = "*" + PackagingConstants.ManifestExtension;

         var splitDirs = Path
            .GetDirectoryName( projectFile ).Substring( root.Length ) // Remove root
            .Split( new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries ) // Get all directory components
            .AggregateIntermediate_AfterAggregation( root, ( accumulated, item ) => Path.Combine( accumulated, item ) ); // Transform ["dir1", "dir2", "dir3"] into ["root:\dir1", "root:\dir1\dir2", "root:\dir1\dir2\dir3" ]
         if ( !String.IsNullOrEmpty( localRepoRoot ) )
         {
            // Only include subfolders of localRepoRoot
            localRepoRoot = Path.GetFullPath( localRepoRoot );
            var repoRootDirLength = localRepoRoot.Substring( root.Length ).Length;
            splitDirs = splitDirs.Where( dir => dir.Length > repoRootDirLength );
         }

         var nupkgFileInfo = splitDirs
            .Reverse() // Enumerate from innermost directory towards outermost directory
            .Select( curDir =>
            {
               var curDirInfo = new DirectoryInfo( curDir );
               var thisNupkgFileInfo = curDirInfo
                  .EnumerateFiles( nupkgFileFilter, SearchOption.TopDirectoryOnly )
                  .FirstOrDefault();

               FileInfo thisNuspecFileInfo = null;
               if (
                  thisNupkgFileInfo != null
                  && !String.IsNullOrEmpty( localRepoRoot )
                  && ( thisNuspecFileInfo = curDirInfo.EnumerateFiles( nuspecFileFilter, SearchOption.TopDirectoryOnly ).FirstOrDefault() ) == null
                  )
               {
                  // .nuspec file is not present, and we are within v3 repo root, so maybe this nupkg file is content... ?
                  thisNupkgFileInfo = null;
               }

               return (thisNupkgFileInfo, thisNuspecFileInfo);
            } )
            .FirstOrDefault( tuple =>
            {
               return tuple.thisNupkgFileInfo != null;
            } );

         PackageIdentity retVal;
         if ( nupkgFileInfo.thisNupkgFileInfo != null )
         {
            // See if we have nuspec information
            if ( nupkgFileInfo.thisNuspecFileInfo != null )
            {
               // We can read package identity from nuspec file
               retVal = new NuspecReader( nupkgFileInfo.thisNuspecFileInfo.FullName ).GetIdentity();
            }
            else
            {
               // Have to read package identity from nupkg file
               using ( var archiveReader = new PackageArchiveReader( nupkgFileInfo.thisNupkgFileInfo.FullName ) )
               {
                  retVal = archiveReader.GetIdentity();
               }
            }
         }
         else
         {
            retVal = null;
         }

         return retVal;
      }

      private static IEnumerable<String> GetSuitableFiles(
         NuGetFramework thisFramework,
         Lazy<RuntimeGraph> runtimeGraph,
         String runtimeIdentifier,
         LockFileTargetLibrary targetLibrary,
         Lazy<IDictionary<String, LockFileLibrary>> libraries
         )
      {
         var retVal = NuGetUtility.GetRuntimeAssembliesDelegate( runtimeGraph, runtimeIdentifier, targetLibrary, libraries );
         if ( !retVal.Any() && libraries.Value.TryGetValue( targetLibrary.Name, out var lib ) )
         {

            // targetLibrary does not list stuff like build/net45/someassembly.dll
            // So let's do manual matching
            var fwGroups = lib.Files.Where( f =>
            {
               return f.StartsWith( PackagingConstants.Folders.Build, StringComparison.OrdinalIgnoreCase )
                      && PackageHelper.IsAssembly( f )
                      && Path.GetDirectoryName( f ).Length > PackagingConstants.Folders.Build.Length + 1;
            } ).GroupBy( f =>
            {
               try
               {
                  return NuGetFramework.ParseFolder( f.Split( '/' )[1] );
               }
               catch
               {
                  return null;
               }
            } )
           .Where( g => g.Key != null )
           .Select( g => new FrameworkSpecificGroup( g.Key, g ) );

            var matchingGroup = NuGetFrameworkUtility.GetNearest(
               fwGroups,
               thisFramework,
               g => g.TargetFramework );
            retVal = matchingGroup?.Items;
         }

         return retVal;
      }

      private String ProcessTaskName(
         XElement taskBodyElement,
         String taskName
         )
      {
         var overrideTaskName = taskBodyElement.ElementAnyNS( TASK_NAME )?.Value;
         return String.IsNullOrEmpty( overrideTaskName ) ? taskName : overrideTaskName;
      }

      internal static void RegisterToResolverEvents(
         NuGetAssemblyResolver resolver,
         ResolverLogger logger
         )
      {
         resolver.OnAssemblyLoadSuccess += args => logger.Log( $"Resolved {args.AssemblyName} located in {args.OriginalPath} and loaded from {( String.Equals( args.OriginalPath, args.ActualPath ) ? "same path" : args.ActualPath )}." );
         resolver.OnAssemblyLoadFail += args => logger.Log( $"Failed to resolve {args.AssemblyName}." );
#if IS_NETSTANDARD
         resolver.OnUnmanagedAssemblyLoadSuccess += args => logger.Log( $"Resolved unmanaged assembly \"{args.AssemblyName}\" located in {args.OriginalPath} and loaded from {( String.Equals( args.OriginalPath, args.ActualPath ) ? "same path" : args.ActualPath )}." );
         resolver.OnUnmanagedAssemblyLoadFail += args => logger.Log( $"Failed to resolve unmanaged assembly \"{args.AssemblyName}\", with all seen unmanaged DLL paths: {String.Join( ";", args.AllSeenUnmanagedAssembliesPaths )}." );
#endif
      }

      private static Func<String, String> CreatePathProcessor( String assemblyCopyTargetFolder )
      {
         return String.IsNullOrEmpty( assemblyCopyTargetFolder ) ? (Func<String, String>) null : originalPath =>
         {
            var newPath = Path.Combine( assemblyCopyTargetFolder, Path.GetFileName( originalPath ) );
            File.Copy( originalPath, newPath, true );
            return newPath;
         };
      }


      internal static void LoadTaskType(
         String taskTypeName,
         NuGetAssemblyResolver resolver,
         String packageID,
         String packageVersion,
         String assemblyPath,
         out ConstructorInfo taskConstructor,
         out Object[] constructorArguments,
         out Boolean usesDynamicLoading
         )
      {

         // This should never cause any actual async waiting, since LockFile for task package has been already cached by restorer
         var taskAssembly = resolver.LoadNuGetAssembly( packageID, packageVersion, default, assemblyPath: assemblyPath ).GetAwaiter().GetResult();
         var taskType = taskAssembly.GetType( taskTypeName, true, false );
         if ( taskType == null )
         {
            throw new Exception( $"Could not find task with type {taskTypeName} from assembly {taskAssembly}." );
         }
         GetTaskConstructorInfo( resolver, taskType, out taskConstructor, out constructorArguments );
         usesDynamicLoading = ( constructorArguments?.Length ?? 0 ) > 0;

      }

      private static void GetTaskConstructorInfo(
         NuGetAssemblyResolver resolver,
         Type type,
         out ConstructorInfo matchingCtor,
         out Object[] ctorParams
         )
      {
         var ctors = type
#if IS_NETSTANDARD
            .GetTypeInfo()
#endif
            .GetConstructors();
         matchingCtor = null;
         ctorParams = null;
         if ( ctors.Length > 0 )
         {
            if ( ctors.Length == 1 && ctors[0].GetParameters().Length == 0 )
            {
               // Default parameterless constructor
               matchingCtor = ctors[0];
            }

            if ( matchingCtor == null )
            {
               MatchConstructorToParameters(
                  ctors,
                  new Object[]
                  {
                     resolver.CreateNuGetPackageResolverCallback(),
                     resolver.CreateNuGetPackagesResolverCallback(),
                     resolver.CreateAssemblyByPathResolverCallback(),
                     resolver.CreateAssemblyNameResolverCallback(),
                     resolver.CreateTypeStringResolverCallback(),
                  }.ToDictionary( o => o.GetType(), o => o ),
                  ref matchingCtor,
                  ref ctorParams
                  );
            }
         }

         if ( matchingCtor == null )
         {
            throw new Exception( $"No public suitable constructors found for type {type.AssemblyQualifiedName}." );
         }

      }

      private static void MatchConstructorToParameters(
         ConstructorInfo[] ctors,
         IDictionary<Type, Object> allPossibleParameters,
         ref ConstructorInfo matchingCtor,
         ref Object[] ctorParams
         )
      {
         // Find public constructor with maximum amount of parameters which has all required types, in any order
         var ctorsAndParams = ctors
            .Select( ctor => new KeyValuePair<ConstructorInfo, ParameterInfo[]>( ctor, ctor.GetParameters() ) )
            .Where( info => info.Value.Select( p => p.ParameterType ).Distinct().Count() == info.Value.Length ) // All types must be unique
            .ToArray();

         // Sort descending
         Array.Sort( ctorsAndParams, ( left, right ) => right.Value.Length.CompareTo( left.Value.Length ) );
         // Get the first one which matches
         var matching = ctorsAndParams.FirstOrDefault( info => info.Value.All( p => allPossibleParameters.ContainsKey( p.ParameterType ) ) );
         if ( matching.Key != null )
         {
            matchingCtor = matching.Key;
            ctorParams = new Object[matching.Value.Length];
            for ( var i = 0; i < ctorParams.Length; ++i )
            {
               ctorParams[i] = allPossibleParameters[matching.Value[i].ParameterType];
            }
         }
      }

      private static Boolean IsMBFAssembly( AssemblyName an )
      {
         switch ( an.Name )
         {
            case "Microsoft.Build":
            case "Microsoft.Build.Framework":
            case "Microsoft.Build.Tasks.Core":
            case "Microsoft.Build.Utilities.Core":
               return true;
            default:
               return false;
         }
      }
   }

   internal static class TaskCodeGenerator
   {
      public static Type GenerateTaskType( (Boolean IsCancelable, TTaskPropertyInfoDictionary propertyInfos) parameters )
      {
         // Since we are executing task in different app domain, our task type must inherit MarshalByRefObject
         // However, we don't want to impose such restriction to task writers - ideal situation would be for task writer to only target .netstandard 1.3 (or .netstandard1.4+ and .net45+, but we still don't want to make such restriction).
         // Furthermore, tasks which only target .netstandard 1.3 don't even have MarshalByRefObject.
         // So, let's generate our own dynamic task type.

         // We should load the actual task type in different domain and collect all public properties with getter and setter.
         // Then, we generate type with same property names, but property types should be Either String or ITaskItem[].
         // All getter and setter logic is forwarded by this generated type to our TaskReferenceHolder class, inheriting MarshalByRefObject and residing in actual task's AppDomain.
         // The TaskReferenceHolder will take care of converting required stuff.

         // public class NuGetTaskWrapper : ITask
         // {
         //    private readonly TaskReferenceHolder _task;
         //
         //    public String SomeProperty
         //    {
         //       get
         //       {
         //           return this._task.GetProperty("SomeProperty");
         //       }
         //       set
         //       {
         //           this._task.SetProperty("SomeProperty", value);
         //       }
         //     }
         //     ...
         // }

         var isCancelable = parameters.Item1;
         var propertyInfos = parameters.Item2;

         var ab = AssemblyBuilder.DefineDynamicAssembly( new AssemblyName( "NuGetTaskWrapperDynamicAssembly" ), AssemblyBuilderAccess.RunAndCollect );
         var mb = ab.DefineDynamicModule( "NuGetTaskWrapperDynamicAssembly.dll"
#if !IS_NETSTANDARD
               , false
#endif
               );
         var tb = mb.DefineType( "NuGetTaskWrapper", TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public );
         tb.AddInterfaceImplementation( typeof( ITask ) );

         var taskField = tb.DefineField( "_task", typeof( TaskReferenceHolder ), FieldAttributes.Private | FieldAttributes.InitOnly );
         var loggerField = tb.DefineField( "_logger", typeof( ResolverLogger ), FieldAttributes.Private | FieldAttributes.InitOnly );

         // Constructor
         var ctor = tb.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
            CallingConventions.HasThis,
            new Type[] { typeof( TaskReferenceHolder ), typeof( ResolverLogger ) }
            );
         var il = ctor.GetILGenerator();
         il.Emit( OpCodes.Ldarg_0 );
         il.Emit( OpCodes.Call, typeof( Object ).GetConstructor( new Type[] { } ) );

         il.Emit( OpCodes.Ldarg_0 );
         il.Emit( OpCodes.Ldarg_1 );
         il.Emit( OpCodes.Stfld, taskField );

         il.Emit( OpCodes.Ldarg_0 );
         il.Emit( OpCodes.Ldarg_2 );
         il.Emit( OpCodes.Stfld, loggerField );

         il.Emit( OpCodes.Ret );
         // Properties
         var taskRefGetter = typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.GetProperty ) ) ?? throw new Exception( "Internal error: no property getter." );
         var taskRefSetter = typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.SetProperty ) ) ?? throw new Exception( "Internal error: no property getter." );
         var toStringCall = typeof( Convert ).GetMethod( nameof( Convert.ToString ), new Type[] { typeof( Object ) } ) ?? throw new Exception( "Internal error: no Convert.ToString." );
         ;
         var requiredAttribute = typeof( RequiredAttribute ).GetConstructor( new Type[] { } ) ?? throw new Exception( "Internal error: no Required attribute constructor." );
         ;
         var outAttribute = typeof( OutputAttribute ).GetConstructor( new Type[] { } ) ?? throw new Exception( "Internal error: no Out attribute constructor." );
         ;
         var beSetter = typeof( ResolverLogger ).GetMethod( nameof( ResolverLogger.TaskBuildEngineSet ) ) ?? throw new Exception( "Internal error: no log setter." );
         var beReady = typeof( ResolverLogger ).GetMethod( nameof( ResolverLogger.TaskBuildEngineIsReady ) ) ?? throw new Exception( "Internal error: no log state updater." );
         ;

         var outPropertyInfos = new List<(String, WrappedPropertyKind, Type, FieldBuilder)>();
         void EmitPropertyConversionCode( ILGenerator curIL, WrappedPropertyKind curKind, Type curPropType )
         {
            if ( curKind != WrappedPropertyKind.StringNoConversion )
            {
               // Emit conversion
               if ( curKind == WrappedPropertyKind.String )
               {
                  // Call to Convert.ToString
                  il.Emit( OpCodes.Call, toStringCall );
               }
               else
               {
                  // Just cast
                  il.Emit( OpCodes.Castclass, curPropType );
               }
            }
         }
         foreach ( var kvp in propertyInfos )
         {
            (var kind, var typeMod, var info) = kvp.Value;
            var propType = GetPropertyType( kind, typeMod );
            if ( propType == null )
            {
               switch ( kind )
               {
                  case WrappedPropertyKind.BuildEngine:
                     propType = typeof( IBuildEngine );
                     break;
                  case WrappedPropertyKind.TaskHost:
                     propType = typeof( ITaskHost );
                     break;
                  default:
                     throw new Exception( $"Property handling code has changed, unknown wrapped property kind: {kind}." );
               }
            }

            var methodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig;
            if ( kind == WrappedPropertyKind.BuildEngine || kind == WrappedPropertyKind.TaskHost )
            {
               // Virtual is required for class methods implementing interface methods
               methodAttributes |= MethodAttributes.Virtual;
            }

            var getter = tb.DefineMethod(
               "get_" + kvp.Key,
               methodAttributes
               );
            getter.SetReturnType( propType );
            il = getter.GetILGenerator();

            if ( info == WrappedPropertyInfo.Out )
            {
               var outField = tb.DefineField( "_out" + outPropertyInfos.Count, propType, FieldAttributes.Private );
               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldfld, outField );
               outPropertyInfos.Add( (kvp.Key, kind, propType, outField) );
            }
            else
            {
               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldfld, taskField );
               il.Emit( OpCodes.Ldstr, kvp.Key );
               il.Emit( OpCodes.Callvirt, taskRefGetter );
               EmitPropertyConversionCode( il, kind, propType );
            }
            il.Emit( OpCodes.Ret );

            MethodBuilder setter;
            if ( info == WrappedPropertyInfo.Out )
            {
               setter = null;
            }
            else
            {
               setter = tb.DefineMethod(
                  "set_" + kvp.Key,
                  methodAttributes
                  );
               setter.SetParameters( new Type[] { propType } );
               il = setter.GetILGenerator();
               if ( kind == WrappedPropertyKind.BuildEngine )
               {
                  // Update the logger
                  il.Emit( OpCodes.Ldarg_0 );
                  il.Emit( OpCodes.Ldfld, loggerField );
                  il.Emit( OpCodes.Ldarg_1 );
                  il.Emit( OpCodes.Callvirt, beSetter );
               }

               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldfld, taskField );
               il.Emit( OpCodes.Ldstr, kvp.Key );
               il.Emit( OpCodes.Ldarg_1 );
               il.Emit( OpCodes.Callvirt, taskRefSetter );
               il.Emit( OpCodes.Ret );
            }
            var prop = tb.DefineProperty(
               kvp.Key,
               PropertyAttributes.None,
               propType,
               new Type[] { }
               );
            prop.SetGetMethod( getter );
            if ( setter != null )
            {
               prop.SetSetMethod( setter );
            }

            switch ( info )
            {
               case WrappedPropertyInfo.Required:
                  prop.SetCustomAttribute( new CustomAttributeBuilder( requiredAttribute, new Object[] { } ) );
                  break;
               case WrappedPropertyInfo.Out:
                  prop.SetCustomAttribute( new CustomAttributeBuilder( outAttribute, new Object[] { } ) );
                  break;
            }
         }
         // Execute method
         var execute = tb.DefineMethod(
            nameof( Microsoft.Build.Framework.ITask.Execute ),
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
            typeof( Boolean ),
            new Type[] { }
            );
         il = execute.GetILGenerator();
         il.Emit( OpCodes.Ldarg_0 );
         il.Emit( OpCodes.Ldfld, loggerField );
         il.Emit( OpCodes.Callvirt, beReady );

         if ( outPropertyInfos.Count > 0 )
         {
            // try { return this._task.Execute(); } finally { this.OutProperty = this._task.GetProperty( "Out" ); }
            var retValLocal = il.DeclareLocal( typeof( Boolean ) );
            il.Emit( OpCodes.Ldc_I4_0 );
            il.Emit( OpCodes.Stloc, retValLocal );
            il.BeginExceptionBlock();
            il.Emit( OpCodes.Ldarg_0 );
            il.Emit( OpCodes.Ldfld, taskField );
            il.Emit( OpCodes.Callvirt, typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.Execute ) ) );
            il.Emit( OpCodes.Stloc, retValLocal );
            il.BeginFinallyBlock();
            foreach ( var outSetter in outPropertyInfos )
            {
               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldfld, taskField );
               il.Emit( OpCodes.Ldstr, outSetter.Item1 );
               il.Emit( OpCodes.Callvirt, taskRefGetter );
               EmitPropertyConversionCode( il, outSetter.Item2, outSetter.Item3 );
               il.Emit( OpCodes.Stfld, outSetter.Item4 );
            }
            il.EndExceptionBlock();

            il.Emit( OpCodes.Ldloc, retValLocal );
         }
         else
         {
            // return this._task.Execute();
            il.Emit( OpCodes.Ldarg_0 );
            il.Emit( OpCodes.Ldfld, taskField );
            il.Emit( OpCodes.Tailcall );
            il.Emit( OpCodes.Callvirt, typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.Execute ) ) );
         }
         il.Emit( OpCodes.Ret );

         // Canceability
         if ( isCancelable )
         {
            tb.AddInterfaceImplementation( typeof( Microsoft.Build.Framework.ICancelableTask ) );
            var cancel = tb.DefineMethod(
               nameof( Microsoft.Build.Framework.ICancelableTask.Cancel ),
               MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
               typeof( void ),
               new Type[] { }
               );
            var cancelMethod = typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.Cancel ) );
            if ( cancelMethod == null )
            {
               throw new Exception( "Internal error: no cancel." );
            }
            il = cancel.GetILGenerator();
            // Call cancel to TaskReferenceHolder which will forward it to actual task
            il.Emit( OpCodes.Ldarg_0 );
            il.Emit( OpCodes.Ldfld, taskField );
            il.Emit( OpCodes.Tailcall );
            il.Emit( OpCodes.Callvirt, cancelMethod );
            il.Emit( OpCodes.Ret );
         }

         // We are ready
         return tb.
#if !IS_NETSTANDARD
            CreateType()
#else
            CreateTypeInfo().AsType()
#endif
            ;



      }

      public static Type GetPropertyType( WrappedPropertyKind kind, WrappedPropertyTypeModifier modifier )
      {
         Type retVal;
         switch ( kind )
         {
            case WrappedPropertyKind.String:
            case WrappedPropertyKind.StringNoConversion:
               retVal = typeof( String );
               break;
            case WrappedPropertyKind.TaskItem:
               retVal = typeof( Microsoft.Build.Framework.ITaskItem );
               break;
            case WrappedPropertyKind.TaskItem2:
               retVal = typeof( Microsoft.Build.Framework.ITaskItem2 );
               break;
            default:
               retVal = null;
               break;
         }

         if ( retVal != null )
         {
            switch ( modifier )
            {
               case WrappedPropertyTypeModifier.Array:
                  retVal = retVal.MakeArrayType();
                  break;
            }
         }

         return retVal;
      }
   }

   // Instances of this class reside in target task app domain, so we must be careful not to use any UtilPack stuff here! So no ArgumentValidator. etc.
   public sealed class TaskReferenceHolder
#if !IS_NETSTANDARD
      : MarshalByRefObject
#endif
   {
      private sealed class TaskPropertyInfo
      {
         public TaskPropertyInfo(
            WrappedPropertyKind wrappedPropertyKind,
            WrappedPropertyTypeModifier typeMod,
            WrappedPropertyInfo wrappedPropertyInfo,
            Func<Object> getter,
            Action<Object> setter,
            Func<Object, Object> converter
            )
         {
            this.WrappedPropertyKind = wrappedPropertyKind;
            this.WrappedPropertyTypeModifier = typeMod;
            this.WrappedPropertyInfo = wrappedPropertyInfo;
            this.Getter = getter;
            this.Setter = setter;
            this.Converter = converter;
         }

         public WrappedPropertyKind WrappedPropertyKind { get; }
         public WrappedPropertyTypeModifier WrappedPropertyTypeModifier { get; }
         public WrappedPropertyInfo WrappedPropertyInfo { get; }
         public Func<Object> Getter { get; }
         public Action<Object> Setter { get; }
         public Func<Object, Object> Converter { get; }
      }

      private readonly Object _task;
      private readonly MethodInfo _executeMethod;
      private readonly MethodInfo _cancelMethod;
      private readonly IDictionary<String, TaskPropertyInfo> _propertyInfos;

      public TaskReferenceHolder( Object task, String msbuildFrameworkAssemblyName, Boolean taskUsesDynamicLoading )
      {
         this._task = task ?? throw new Exception( "Failed to create the task object." );
         this.TaskUsesDynamicLoading = taskUsesDynamicLoading;
         var taskType = this._task.GetType();
         //var mbfAssemblyName = new AssemblyName( msbuildFrameworkAssemblyName );
         //var mbfAssemblyToken = mbfAssemblyName.GetPublicKeyToken() ?? Empty<Byte>.Array;
         var mbfInterfaces = taskType.GetInterfaces()
            .Where( iFace =>
            {
               // MSBuild does not understand loading multiple MSBuild assemblies, so we probably shouldn't either.
               //var iFaceAssemblyName = iFace.GetTypeInfo().Assembly.GetName();
               //return String.Equals( iFaceAssemblyName.Name, mbfAssemblyName.Name )
               //   && ArrayEqualityComparer<Byte>.ArrayEquality( iFaceAssemblyName.GetPublicKeyToken() ?? Empty<Byte>.Array, mbfAssemblyToken );
               return iFace
                  .GetTypeInfo()
                  .Assembly.GetName().FullName.Equals( msbuildFrameworkAssemblyName );
            } )
            .ToArray();
         var iTask = mbfInterfaces
            .Where( iFace => iFace.FullName.Equals( CommonHelpers.MBF + nameof( Microsoft.Build.Framework.ITask ) ) )
            .FirstOrDefault() ?? throw new ArgumentException( $"The task \"{taskType.FullName}\" located in \"{taskType.GetTypeInfo().Assembly.CodeBase}\" does not seem to implement \"{nameof( Microsoft.Build.Framework.ITask )}\" interface. Make sure the MSBuild target version is at least 14.3. Seen interfaces: {String.Join( ",", taskType.GetInterfaces().Select( i => i.AssemblyQualifiedName ) )}. Seen assemblies: {String.Join( ",", taskType.GetInterfaces().Select( i => i.GetTypeInfo().Assembly.CodeBase ) )}. Seen MBF interfaces: {String.Join( ",", mbfInterfaces.Select( i => i.FullName ) )}. MBF assembly name: \"{msbuildFrameworkAssemblyName}\"." );

         // TODO explicit implementations
         this._executeMethod = iTask.GetMethods()
            .FirstOrDefault( m =>
               m.Name.Equals( nameof( Microsoft.Build.Framework.ITask.Execute ) )
               && m.GetParameters().Length == 0
               && m.ReturnType.FullName.Equals( typeof( Boolean ).FullName )
               ) ?? throw new ArgumentException( $"The task \"{taskType.FullName}\" does not seem to have implicit implementation of \"{nameof( Microsoft.Build.Framework.ITask.Execute )}\" method." );

         this._cancelMethod = mbfInterfaces
            .FirstOrDefault( iFace => iFace.FullName.Equals( CommonHelpers.MBF + nameof( Microsoft.Build.Framework.ICancelableTask ) ) )
            ?.GetMethods()?.FirstOrDefault( m => m.Name.Equals( nameof( Microsoft.Build.Framework.ICancelableTask.Cancel ) ) && m.GetParameters().Length == 0 );

         this._propertyInfos = CommonHelpers.GetPropertyInfoFromType(
            task.GetType(),
            new AssemblyName( msbuildFrameworkAssemblyName )
            ).ToDictionary(
               kvp => kvp.Key,
               kvp =>
               {
                  var curProperty = kvp.Value.Item4;
                  var propType = curProperty.PropertyType;
                  var isArray = kvp.Value.Item2 == WrappedPropertyTypeModifier.Array;
                  if ( isArray )
                  {
                     propType = propType.GetElementType();
                  }
                  var converter = kvp.Value.Item1 == WrappedPropertyKind.String ?
                     ( propType.GetTypeInfo().IsEnum ? (Func<Object, Object>) ( str => Enum.Parse( propType, (String) str, true ) ) : ( str => Convert.ChangeType( (String) str, propType ) ) ) :
                     (Func<Object, Object>) null;
                  if ( converter != null && isArray )
                  {
                     var itemConverter = converter;
                     converter = arrayObj =>
                     {
                        var array = (Array) arrayObj;
                        var retValArray = Array.CreateInstance( propType, array.Length );
                        for ( var i = 0; i < array.Length; ++i )
                        {
                           retValArray.SetValue( itemConverter( array.GetValue( i ) ), i );
                        }

                        return retValArray;
                     };
                  }

                  return new TaskPropertyInfo(
                     kvp.Value.Item1,
                     kvp.Value.Item2,
                     kvp.Value.Item3,
                     () => curProperty.GetMethod.Invoke( this._task, null ),
                     val => curProperty.SetMethod.Invoke( this._task, new[] { val } ),
                     converter
                  );
               } );

      }

      // Passing value tuples thru appdomain boundaries is errorprone, so just use normal integers here
      internal IDictionary<String, Int32> GetPropertyInfo()
      {
         return this._propertyInfos.ToDictionary( kvp => kvp.Key, kvp => EncodeKindAndInfo( kvp.Value.WrappedPropertyKind, kvp.Value.WrappedPropertyTypeModifier, kvp.Value.WrappedPropertyInfo ) );
      }

      internal Boolean IsCancelable => this._cancelMethod != null;

      internal Boolean TaskUsesDynamicLoading { get; }

      // Called by generated task type
      public void Cancel()
      {
         this._cancelMethod.Invoke( this._task, null );
      }

      // Called by generated task type
      public Object GetProperty( String propertyName )
      {
         return this._propertyInfos.TryGetValue( propertyName, out var info ) ?
            info.Getter() :
            throw new InvalidOperationException( $"Property \"{propertyName}\" is not supported for this task." );
      }

      // Called by generated task type
      public void SetProperty( String propertyName, Object value )
      {
         if ( this._propertyInfos.TryGetValue( propertyName, out var info ) )
         {
            if ( info.Converter != null )
            {
               value = info.Converter( (String) value );
            }
            info.Setter( value );
         }
         else
         {
            throw new InvalidOperationException( $"Property \"{propertyName}\" is not supported for this task." );
         }
      }

      // Called by generated task type
      public Boolean Execute()
      {
         // We can't cast to Microsoft.Build.Framework.ITask, since the 14.0 version will be loaded (from GAC), if target task assembly is netstandard assembly.
         // This is because this project depends on msbuild 14.3 in net45 build.

         // So... just invoke dynamically.
         return (Boolean) this._executeMethod.Invoke( this._task, null );
      }

      internal static Int32 EncodeKindAndInfo( WrappedPropertyKind kind, WrappedPropertyTypeModifier typeMod, WrappedPropertyInfo info )
      {
         // 2 lowest bits to info, then 1 bit to type mod, and remaining bits to kind
         return ( ( (Int32) kind ) << 3 ) | ( ( ( (Int32) typeMod ) & 0x01 ) << 2 ) | ( ( (Int32) info ) & 0x03 );
      }

      internal static (WrappedPropertyKind, WrappedPropertyTypeModifier, WrappedPropertyInfo) DecodeKindAndInfo( Int32 encoded )
      {
         return ((WrappedPropertyKind) ( ( encoded & 0xF8 ) >> 3 ), (WrappedPropertyTypeModifier) ( ( encoded & 0x04 ) >> 2 ), (WrappedPropertyInfo) ( ( encoded & 0x03 ) ));
      }

   }

   // Instances of this class reside in task factory app domain.
   // Has to be public, since it is used by dynamically generated task type.
   public sealed class ResolverLogger
#if !IS_NETSTANDARD
      : MarshalByRefObject
#endif
   {
      private const Int32 INITIAL = 0;
      private const Int32 TASK_BE_INITIALIZING = 1;
      private const Int32 TASK_BE_READY = 2;

      private IBuildEngine _be;
      private Int32 _state;
      private readonly List<String> _queuedMessages;
      private readonly NuGetMSBuildLogger _nugetLogger;

      internal ResolverLogger( NuGetMSBuildLogger nugetLogger )
      {
         this._queuedMessages = new List<String>();
         this._nugetLogger = nugetLogger;
      }

      // This is called by generated task type in its IBuildEngine setter
      public void TaskBuildEngineSet( IBuildEngine be )
      {
         if ( be != null && Interlocked.CompareExchange( ref this._state, TASK_BE_INITIALIZING, INITIAL ) == INITIAL )
         {
            Interlocked.Exchange( ref this._be, be );
            this._nugetLogger.BuildEngine = null;
         }
      }

      // This is called by generated task type in its Execute method start
      public void TaskBuildEngineIsReady()
      {
         if ( Interlocked.CompareExchange( ref this._state, TASK_BE_READY, TASK_BE_INITIALIZING ) == TASK_BE_INITIALIZING )
         {
            this._nugetLogger.BuildEngine = this._be;
            // process all queued messages
            foreach ( var msg in this._queuedMessages )
            {
               this.Log( msg );
            }
            this._queuedMessages.Clear();
         }
      }

      public void Log( String message )
      {
         switch ( this._state )
         {
            case TASK_BE_READY:
               this._be.LogMessageEvent( new BuildMessageEventArgs(
                  message,
                  null,
                  "NuGetPackageAssemblyResolver",
                  MessageImportance.Low,
                  DateTime.UtcNow
               ) );
               break;
            default:
               // When assembly resolve happens during task initialization (setting BuildEngine etc properties).
               // Using BuildEngine then will cause NullReferenceException as its LoggingContext property is not yet set.
               // And task factory logging context has already been marked inactive, so this is when we can't immediately log.
               // In this case, just queue message, and log them once task's Execute method has been invoked.
               this._queuedMessages.Add( message );
               break;
         }

      }
   }



#if !IS_NETSTANDARD
   [Serializable] // We want to be serializable instead of MarshalByRef as we want to copy these objects
#endif
   internal sealed class ResolvedPackageInfo
   {
      public ResolvedPackageInfo( String packageDirectory, String[] assemblies )
      {
         this.PackageDirectory = packageDirectory;
         this.Assemblies = assemblies;
      }

      public String PackageDirectory { get; }
      public String[] Assemblies { get; }
   }

   // These methods are used by both .net45 and .netstandard.
   // This class has no implemented interfaces and extends System.Object.
   // Therefore using this static method from another appdomain won't cause any assembly resolves.
   internal static class CommonHelpers
   {
      internal const String MBF = "Microsoft.Build.Framework.";

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
            var typeMod = WrappedPropertyTypeModifier.None;
            if ( actualType.IsArray )
            {
               actualType = actualType.GetElementType();
               typeMod = WrappedPropertyTypeModifier.Array;
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
#if !IS_NETSTANDARD
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

               retVal.Add( curProperty.Name, (kind.Value, typeMod, info, curProperty) );
            }
         }

         return retVal;
      }

      private static Boolean ISMFBType( Type type, AssemblyName mfbAssembly )
      {
         var an = type
#if IS_NETSTANDARD
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

   internal enum WrappedPropertyTypeModifier
   {
      None,
      Array
   }

   internal enum WrappedPropertyInfo
   {
      None,
      Required,
      Out
   }

}

public static partial class E_UtilPack
{
   // From https://stackoverflow.com/questions/1145659/ignore-namespaces-in-linq-to-xml
   internal static IEnumerable<XElement> ElementsAnyNS<T>( this IEnumerable<T> source, String localName )
      where T : XContainer
   {
      return source.Elements().Where( e => e.Name.LocalName == localName );
   }

   internal static XElement ElementAnyNS<T>( this IEnumerable<T> source, String localName )
      where T : XContainer
   {
      return source.ElementsAnyNS( localName ).FirstOrDefault();
   }

   internal static IEnumerable<XElement> ElementsAnyNS( this XContainer source, String localName )
   {
      return source.Elements().Where( e => e.Name.LocalName == localName );
   }

   internal static XElement ElementAnyNS( this XContainer source, String localName )
   {
      return source.ElementsAnyNS( localName ).FirstOrDefault();
   }

   internal static String DefaultIfNullOrEmpty( this String value, String defaultValue )
   {
      return String.IsNullOrEmpty( value ) ? defaultValue : value;
   }

   internal static String DefaultIfNullOrEmpty( this String value, Func<String> defaultValueFactory )
   {
      return String.IsNullOrEmpty( value ) ? defaultValueFactory() : value;
   }
}