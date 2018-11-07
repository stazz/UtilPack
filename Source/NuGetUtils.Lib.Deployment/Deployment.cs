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
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol.Core.Types;
using NuGetUtils.Lib.Deployment;
using NuGetUtils.Lib.Restore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace NuGetUtils.Lib.Deployment
{
   ///// <summary>
   ///// This class contains configurable implementation for deploying NuGet packages.
   ///// Deploying in this context means restoring any missing packages, and copying and generating all the required files so that the package can be executed using <c>dotnet</c> tool.
   ///// This means that only one framework of the package will be used.
   ///// </summary>
   //public class NuGetDeployment
   //{
   //   private const String RUNTIME_CONFIG_FW_NAME = "runtimeOptions.framework.name";
   //   private const String RUNTIME_CONFIG_FW_VERSION = "runtimeOptions.framework.version";
   //   private const String RUNTIME_CONFIG_TFM = "runtimeOptions.tfm";
   //   private const String RUNTIME_CONFIG_PROBING_PATHS = "runtimeOptions.additionalProbingPaths";
   //   private const String DEPS_EXTENSION = ".deps.json";
   //   private const String RUNTIME_CONFIG_EXTENSION = ".runtimeconfig.json";

   //   private readonly DeploymentConfiguration _config;

   //   /// <summary>
   //   /// Creates a new instance of <see cref="NuGetDeployment"/> with given <see cref="DeploymentConfiguration"/>.
   //   /// </summary>
   //   /// <param name="config">The deployment configuration.</param>
   //   /// <exception cref="ArgumentNullException">If <paramref name="config"/> is <c>null</c>.</exception>
   //   public NuGetDeployment( DeploymentConfiguration config )
   //   {
   //      this._config = ArgumentValidator.ValidateNotNull( nameof( config ), config );
   //   }

   //   /// <summary>
   //   /// Performs deployment asynchronously.
   //   /// </summary>
   //   /// <param name="nugetSettings">The NuGet settings to use.</param>
   //   /// <param name="targetDirectory">The directory where to place required files. If <c>null</c> or empty, then a new directory with randomized name will be created in temporary folder of the current user.</param>
   //   /// <param name="logger">The optional logger to use.</param>
   //   /// <param name="token">The optional cancellation token to use.</param>
   //   /// <returns>The full path to the assembly which should be executed, along with the resolved target framework as <see cref="NuGetFramework"/> object.</returns>
   //   /// <exception cref="ArgumentNullException">If <paramref name="nugetSettings"/> is <c>null</c>.</exception>
   //   public async Task<(String, NuGetFramework)> DeployAsync(
   //      ISettings nugetSettings,
   //      String targetDirectory,
   //      ILogger logger = null,
   //      CancellationToken token = default( CancellationToken )
   //      )
   //   {
   //      var processConfig = this._config;

   //      String assemblyToBeExecuted = null;
   //      NuGetFramework targetFW = null;
   //      using ( var sourceCache = new SourceCacheContext() )
   //      {
   //         if ( logger == null )
   //         {
   //            logger = NullLogger.Instance;
   //         }

   //         (var identity, var fwInfo, var entryPointAssembly) = await this.PerformInitialRestore(
   //            nugetSettings,
   //            sourceCache,
   //            new AgnosticFrameworkLoggerWrapper( logger ),
   //            token
   //            );

   //         if ( identity != null && fwInfo != null && !String.IsNullOrEmpty( entryPointAssembly ) )
   //         {
   //            targetFW = fwInfo.TargetFramework;
   //            var targetFWString = processConfig.RestoreFramework;
   //            NuGetFramework newTargetFW;
   //            if ( !String.IsNullOrEmpty( targetFWString ) && ( newTargetFW = NuGetFramework.Parse( targetFWString ) ) != null )
   //            {
   //               targetFW = newTargetFW;
   //            }

   //            using ( var restorer = new BoundRestoreCommandUser(
   //               nugetSettings,
   //               sourceCacheContext: sourceCache,
   //               nugetLogger: logger,
   //               thisFramework: targetFW,
   //               runtimeIdentifier: processConfig.RuntimeIdentifier,
   //               leaveSourceCacheOpen: true
   //               ) )
   //            {

   //               (var lockFile, var runtimeConfig, var sdkPackageID, var sdkPackageVersion) = await this.PerformActualRestore(
   //                  restorer,
   //                  targetFW,
   //                  identity,
   //                  entryPointAssembly,
   //                  token
   //                  );

   //               targetDirectory = CreateTargetDirectory( targetDirectory );

   //               switch ( processConfig.DeploymentKind )
   //               {
   //                  case DeploymentKind.GenerateConfigFiles:
   //                     if ( targetFW.IsDesktop() )
   //                     {
   //                        // This is not supported for desktop framework
   //                        // TODO log warning
   //                        assemblyToBeExecuted = DeployByCopyingAssemblies(
   //                           restorer,
   //                           lockFile,
   //                           targetFW,
   //                           entryPointAssembly,
   //                           runtimeConfig,
   //                           sdkPackageID,
   //                           sdkPackageVersion,
   //                           targetDirectory
   //                           );
   //                     }
   //                     else
   //                     {
   //                        assemblyToBeExecuted = DeployByGeneratingConfigFiles(
   //                           lockFile,
   //                           identity,
   //                           entryPointAssembly,
   //                           runtimeConfig,
   //                           sdkPackageID,
   //                           sdkPackageVersion,
   //                           targetDirectory
   //                           );
   //                     }
   //                     break;
   //                  case DeploymentKind.CopyNonSDKAssemblies:
   //                     assemblyToBeExecuted = DeployByCopyingAssemblies(
   //                        restorer,
   //                        lockFile,
   //                        targetFW,
   //                        entryPointAssembly,
   //                        runtimeConfig,
   //                        sdkPackageID,
   //                        sdkPackageVersion,
   //                        targetDirectory
   //                        );
   //                     break;
   //                  default:
   //                     throw new NotSupportedException( $"Unrecognized deployment kind: {processConfig.DeploymentKind}." );
   //               }
   //            }
   //         }
   //      }

   //      return (assemblyToBeExecuted, targetFW);
   //   }

   //   private async Task<(PackageIdentity, FrameworkSpecificGroup, String)> PerformInitialRestore(
   //      ISettings nugetSettings,
   //      SourceCacheContext sourceCache,
   //      ILogger logger,
   //      CancellationToken token
   //      )
   //   {
   //      var config = this._config;
   //      (PackageIdentity, FrameworkSpecificGroup, String) retVal = (null, null, null);
   //      using ( var restorer = new BoundRestoreCommandUser(
   //         nugetSettings,
   //         sourceCacheContext: sourceCache,
   //         nugetLogger: logger,
   //         thisFramework: NuGetFramework.AgnosticFramework, // This will cause to only the target package to be restored, and none of its dependencies
   //         leaveSourceCacheOpen: true
   //         ) )
   //      {
   //         var lockFile = await restorer.RestoreIfNeeded( config.ProcessPackageID, config.ProcessPackageVersion, token );

   //         var pathWithinRepository = lockFile.Libraries.FirstOrDefault()?.Path;
   //         var packagePath = restorer.ResolveFullPath( lockFile, pathWithinRepository );
   //         if ( !String.IsNullOrEmpty( packagePath ) )
   //         {

   //            FrameworkSpecificGroup[] libItems;
   //            PackageIdentity packageID;
   //            using ( var reader = new PackageFolderReader( packagePath ) )
   //            {
   //               libItems = reader.GetLibItems().ToArray();
   //               packageID = reader.GetIdentity();
   //            }

   //            var possibleAssemblies = libItems
   //               .SelectMany( item => item.Items.Select( i => (item, i) ) )
   //               .Where( tuple => PackageHelper.IsAssembly( tuple.Item2 ) )
   //               .Select( tuple => (tuple.Item1, Path.GetFullPath( Path.Combine( packagePath, tuple.Item2 ) )) )
   //               .ToArray();

   //            var targetFWString = config.ProcessFramework;
   //            if ( !String.IsNullOrEmpty( targetFWString ) )
   //            {
   //               var targetFW = NuGetFramework.ParseFolder( targetFWString );

   //               var nearestFW = new FrameworkReducer().GetNearest( targetFW, possibleAssemblies.Select( t => t.Item1.TargetFramework ) );
   //               if ( nearestFW == null )
   //               {
   //                  nearestFW = targetFW;
   //               }

   //               possibleAssemblies = possibleAssemblies
   //                  .Where( t => t.Item1.TargetFramework.Equals( nearestFW ) )
   //                  .ToArray();
   //            }

   //            if ( possibleAssemblies.Length > 0 )
   //            {
   //               var possibleAssemblyPaths = possibleAssemblies.Select( tuple => tuple.Item2 ).ToArray();
   //               var matchingAssembly = NuGetUtility.GetAssemblyPathFromNuGetAssemblies(
   //                  packageID.Id,
   //                  possibleAssemblyPaths,
   //                  config.ProcessAssemblyPath
   //                  );

   //               if ( !String.IsNullOrEmpty( matchingAssembly ) )
   //               {
   //                  var assemblyInfo = possibleAssemblies[Array.IndexOf( possibleAssemblyPaths, matchingAssembly )];
   //                  retVal = (packageID, assemblyInfo.Item1, assemblyInfo.Item2);
   //               }

   //            }
   //            else
   //            {
   //               logger.LogError( $"No suitable assemblies found for {packageID} and framework \"{targetFWString}\"." );
   //            }
   //         }
   //      }

   //      return retVal;
   //   }

   //   private async Task<(LockFile, JToken, String, String)> PerformActualRestore(
   //      BoundRestoreCommandUser restorer,
   //      NuGetFramework targetFramework,
   //      PackageIdentity identity,
   //      String entryPointAssemblyPath,
   //      CancellationToken token
   //      )
   //   {
   //      var config = this._config;
   //      var lockFile = await restorer.RestoreIfNeeded( identity.Id, identity.Version.ToNormalizedString(), token );
   //      var sdkPackageContainsAllPackagesAsAssemblies = this._config.SDKPackageContainsAllPackagesAsAssemblies;
   //      JToken runtimeConfig = null;
   //      String sdkPackageID = null;
   //      String sdkPackageVersion = null;
   //      if ( String.IsNullOrEmpty( config.RestoreFramework ) )
   //      {
   //         var runtimeConfigPath = Path.ChangeExtension( entryPointAssemblyPath, RUNTIME_CONFIG_EXTENSION );
   //         if ( File.Exists( runtimeConfigPath ) )
   //         {
   //            try
   //            {
   //               using ( var streamReader = new StreamReader( File.OpenRead( runtimeConfigPath ) ) )
   //               using ( var jsonReader = new Newtonsoft.Json.JsonTextReader( streamReader ) )
   //               {
   //                  runtimeConfig = JToken.ReadFrom( jsonReader );
   //               }
   //               sdkPackageID = ( runtimeConfig.SelectToken( RUNTIME_CONFIG_FW_NAME ) as JValue )?.Value?.ToString();
   //               sdkPackageVersion = ( runtimeConfig.SelectToken( RUNTIME_CONFIG_FW_VERSION ) as JValue )?.Value?.ToString();
   //            }
   //            catch
   //            {
   //               // Ignore
   //            }
   //         }
   //      }

   //      sdkPackageID = NuGetUtility.GetSDKPackageID( targetFramework, config.ProcessSDKFrameworkPackageID ?? sdkPackageID );
   //      sdkPackageVersion = NuGetUtility.GetSDKPackageVersion( targetFramework, sdkPackageID, config.ProcessSDKFrameworkPackageVersion ?? sdkPackageVersion );

   //      var sdkPackages = new HashSet<String>( lockFile.Targets[0].GetAllDependencies(
   //         sdkPackageID.Singleton()
   //         )
   //         .Select( lib => lib.Name )
   //         );

   //      // In addition, check all compile assemblies from sdk package (e.g. Microsoft.NETCore.App )
   //      // Starting from 2.0.0, all assemblies from all dependent packages are marked as compile-assemblies stored in sdk package.
   //      Version.TryParse( sdkPackageVersion, out var sdkPkgVer );
   //      if ( sdkPackageContainsAllPackagesAsAssemblies.IsTrue() ||
   //         ( !sdkPackageContainsAllPackagesAsAssemblies.HasValue && sdkPackageID == NuGetUtility.SDK_PACKAGE_NETCORE && sdkPkgVer != null && sdkPkgVer.Major >= 2 )
   //         )
   //      {
   //         var sdkPackageLibraries = lockFile.Targets[0].Libraries.Where( l => l.Name == sdkPackageID );

   //         if ( sdkPkgVer != null )
   //         {
   //            sdkPackageLibraries = sdkPackageLibraries.Where( l => l.Version.Version >= sdkPkgVer );
   //         }

   //         var sdkPackageLibrary = sdkPackageLibraries.FirstOrDefault();

   //         if ( sdkPackageLibrary == null && sdkPkgVer != null )
   //         {
   //            // We need to restore the correctly versioned SDK package
   //            sdkPackageLibrary = ( await restorer.RestoreIfNeeded( sdkPackageID, sdkPackageVersion, token ) ).Targets[0].Libraries.Where( l => l.Name == sdkPackageID ).FirstOrDefault();
   //         }

   //         if ( sdkPackageLibrary != null )
   //         {
   //            sdkPackages.UnionWith( sdkPackageLibrary.CompileTimeAssemblies.Select( cta => Path.GetFileNameWithoutExtension( cta.Path ) ) );
   //         }
   //      }

   //      // Actually -> return LockFile, but modify it so that sdk packages are removed
   //      var targetLibs = lockFile.Targets[0].Libraries;
   //      for ( var i = 0; i < targetLibs.Count; )
   //      {
   //         var curLib = targetLibs[i];
   //         var contains = sdkPackages.Contains( curLib.Name );
   //         if ( contains
   //            || (
   //               ( curLib.RuntimeAssemblies.Count <= 0 || curLib.RuntimeAssemblies.All( ass => ass.Path.EndsWith( "_._" ) ) )
   //               && curLib.RuntimeTargets.Count <= 0
   //               && curLib.ResourceAssemblies.Count <= 0
   //               && curLib.NativeLibraries.Count <= 0
   //               )
   //            )
   //         {
   //            targetLibs.RemoveAt( i );
   //            if ( !contains )
   //            {
   //               sdkPackages.Add( curLib.Name );
   //            }
   //         }
   //         else
   //         {
   //            ++i;
   //         }
   //      }

   //      var libs = lockFile.Libraries;
   //      for ( var i = 0; i < libs.Count; )
   //      {
   //         var curLib = libs[i];
   //         if ( sdkPackages.Contains( curLib.Name ) )
   //         {
   //            libs.RemoveAt( i );
   //         }
   //         else
   //         {
   //            ++i;
   //         }
   //      }

   //      return (lockFile, runtimeConfig, sdkPackageID, sdkPackageVersion);
   //   }



   //   private static String CreateTargetDirectory(
   //      String targetDirectory
   //      )
   //   {
   //      if ( String.IsNullOrEmpty( targetDirectory ) )
   //      {
   //         targetDirectory = Path.Combine( Path.GetTempPath(), "NuGetProcess_" + Guid.NewGuid() );
   //      }

   //      if ( !Directory.Exists( targetDirectory ) )
   //      {
   //         Directory.CreateDirectory( targetDirectory );
   //      }

   //      return targetDirectory;
   //   }


   //}

   ///// <summary>
   ///// This class provides easy-to-use implementation of <see cref="DeploymentConfiguration"/>.
   ///// </summary>
   //public class DefaultDeploymentConfiguration : DeploymentConfiguration
   //{
   //   /// <inheritdoc />
   //   public String ProcessPackageID { get; set; }

   //   /// <inheritdoc />
   //   public String ProcessPackageVersion { get; set; }

   //   /// <inheritdoc />
   //   public String ProcessAssemblyPath { get; set; }

   //   /// <inheritdoc />
   //   public String ProcessFramework { get; set; }

   //   /// <inheritdoc />
   //   public String ProcessSDKFrameworkPackageID { get; set; }

   //   /// <inheritdoc />
   //   public String ProcessSDKFrameworkPackageVersion { get; set; }

   //   /// <inheritdoc />
   //   public DeploymentKind DeploymentKind { get; set; }

   //   /// <inheritdoc />
   //   public Boolean? SDKPackageContainsAllPackagesAsAssemblies { get; set; }

   //   /// <inheritdoc />
   //   public String RestoreFramework { get; set; }

   //   /// <inheritdoc />
   //   public String RuntimeIdentifier { get; set; }
   //}


}

public static class E_UtilPack
{
   private const String RUNTIME_CONFIG_TFM = "runtimeOptions.tfm";
   private const String RUNTIME_CONFIG_FW_NAME = "runtimeOptions.framework.name";
   private const String RUNTIME_CONFIG_FW_VERSION = "runtimeOptions.framework.version";
   private const String RUNTIME_CONFIG_PROBING_PATHS = "runtimeOptions.additionalProbingPaths";
   private const String DEPS_EXTENSION = ".deps.json";
   private const String RUNTIME_CONFIG_EXTENSION = ".runtimeconfig.json";

   public static async Task<(String EntryPointAssemblyPath, NuGetFramework DeployedPackageFramework)> DeployAsync(
      this NuGetDeploymentConfiguration config,
      BoundRestoreCommandUser restorer,
      CancellationToken token,
      String sdkPackageID,
      String sdkPackageVersion
      )
   {
      (var lockFile, var packageFramework, var entryPointAssembly, var runtimeConfig) = await config.RestoreAndFilterOutSDKPackages(
         restorer,
         token,
         sdkPackageID,
         sdkPackageVersion
         );

      var targetDirectory = CreateTargetDirectory( config.TargetDirectory );

      String assemblyToBeExecuted;

      switch ( config.DeploymentKind )
      {
         case DeploymentKind.GenerateConfigFiles:
            if ( restorer.ThisFramework.IsDesktop() )
            {
               // This is not supported for desktop framework
               // TODO log warning
               assemblyToBeExecuted = DeployByCopyingAssemblies(
                  restorer,
                  lockFile,
                  entryPointAssembly,
                  runtimeConfig,
                  sdkPackageID,
                  sdkPackageVersion,
                  targetDirectory
                  );
            }
            else
            {
               assemblyToBeExecuted = DeployByGeneratingConfigFiles(
                  lockFile,
                  entryPointAssembly,
                  runtimeConfig,
                  sdkPackageID,
                  sdkPackageVersion,
                  targetDirectory
                  );
            }
            break;
         case DeploymentKind.CopyNonSDKAssemblies:
            assemblyToBeExecuted = DeployByCopyingAssemblies(
               restorer,
               lockFile,
               entryPointAssembly,
               runtimeConfig,
               sdkPackageID,
               sdkPackageVersion,
               targetDirectory
               );
            break;
         default:
            throw new NotSupportedException( $"Unrecognized deployment kind: {config.DeploymentKind}." );
      }

      return (assemblyToBeExecuted, packageFramework);
   }

   private static async Task<(LockFile, NuGetFramework, String, JToken)> RestoreAndFilterOutSDKPackages(
      this NuGetDeploymentConfiguration config,
      BoundRestoreCommandUser restorer,
      CancellationToken token,
      String restoreSDKPackageID,
      String restoreSDKPackageVersion
      )
   {
      var packageID = config.PackageID;
      var lockFile = await restorer.RestoreIfNeeded( packageID, config.PackageVersion, token );
      var kek = lockFile.Libraries.FirstOrDefault( l => String.Equals( l.Name, packageID, StringComparison.OrdinalIgnoreCase ) );
      var lel = lockFile.Targets[0].GetTargetLibrary( packageID );
      // TODO better error messages
      var packagePath = restorer.ResolveFullPath(
         lockFile,
         lockFile.Libraries.FirstOrDefault( l => String.Equals( l.Name, packageID, StringComparison.OrdinalIgnoreCase ) )?.Path
         );
      var epAssemblyPath = NuGetUtility.GetAssemblyPathFromNuGetAssemblies(
         packageID,
         lockFile
            .Targets[0]
            .GetTargetLibrary( packageID )
            .RuntimeAssemblies
            .Select( ra => Path.GetFullPath( Path.Combine( packagePath, ra.Path ) ) )
            .ToArray(),
         config.AssemblyPath
         );

      // We might be restoring .NETStandard package against .NETCoreApp framework, so find out the actual framework the package was built against.
      // So from path "/path/to/nuget/repo/package-id/package-version/lib/target-fw/package-id.dll" we want to extract the target-fw portion.
      // packagePath variable holds everything before the 'lib', so we just need to name of the folder 1 hierarchy level down from packagePath.
      var start = GetNextSeparatorIndex( epAssemblyPath, packagePath.Length + 1 ) + 1;
      var end = GetNextSeparatorIndex( epAssemblyPath, start );
      var packageFramework = NuGetFramework.ParseFolder( epAssemblyPath.Substring( start, end - start ) );

      JToken runtimeConfig = null;
      var runtimeConfigPath = Path.ChangeExtension( epAssemblyPath, RUNTIME_CONFIG_EXTENSION );
      if ( File.Exists( runtimeConfigPath ) )
      {
         try
         {
            using ( var streamReader = new StreamReader( File.OpenRead( runtimeConfigPath ) ) )
            using ( var jsonReader = new JsonTextReader( streamReader ) )
            {
               runtimeConfig = JToken.ReadFrom( jsonReader );
            }
         }
         catch
         {
            // Ignore
         }
      }
      //}
      //var targetFramework = restorer.ThisFramework;
      //sdkPackageID = NuGetUtility.GetSDKPackageID( targetFramework, config.ProcessSDKFrameworkPackageID ?? sdkPackageID );
      //sdkPackageVersion = NuGetUtility.GetSDKPackageVersion( targetFramework, sdkPackageID, config.ProcessSDKFrameworkPackageVersion ?? sdkPackageVersion );

      // Warning: The code below exploits the de facto behaviour that package named "System.XYZ" will contain assembly named "System.XYZ". Should sometime in the future this change, this code will then result in possibly wrong behaviour.
      // TODO I wonder if this assumption is needed anymore with .NET Standard and .NET Core 2+?

      var packageSDKPackageID = packageFramework.GetSDKPackageID( config.PackageSDKFrameworkPackageID );
      var sdkPackages = new HashSet<String>( StringComparer.OrdinalIgnoreCase );
      if ( !String.Equals( packageSDKPackageID, restoreSDKPackageID, StringComparison.OrdinalIgnoreCase ) )
      {
         // Typically when package is for .NET Standard and target framework is .NET Core
         var restoreLockFile = await restorer.RestoreIfNeeded( restoreSDKPackageID, restoreSDKPackageVersion, token );
         sdkPackages.UnionWith( restoreLockFile.Targets[0].GetAllDependencies( restoreSDKPackageID.Singleton() )
            .SelectMany( lib => lib.Name.Singleton().Concat( lib.CompileTimeAssemblies.Select( cta => Path.GetFileNameWithoutExtension( cta.Path ) ).FilterUnderscores() ) )
            );
      }
      sdkPackages.UnionWith( lockFile.Targets[0].GetAllDependencies( packageSDKPackageID.Singleton() ).Select( lib => lib.Name ) );


      var packageSDKPackageVersion = packageFramework.GetSDKPackageVersion( packageSDKPackageID, config.PackageSDKFrameworkPackageVersion );
      // In addition, check all compile assemblies from sdk package (e.g. Microsoft.NETCore.App )
      // Starting from 2.0.0, all assemblies from all dependent packages are marked as compile-assemblies stored in sdk package.
      var sdkPackageContainsAllPackagesAsAssemblies = config.PackageFrameworkIsPackageBased;
      Version.TryParse( packageSDKPackageVersion, out var sdkPkgVer );
      if ( sdkPackageContainsAllPackagesAsAssemblies.IsTrue() ||
         ( !sdkPackageContainsAllPackagesAsAssemblies.HasValue && packageFramework.IsPackageBased ) // sdkPackageID == NuGetUtility.SDK_PACKAGE_NETCORE && sdkPkgVer != null && sdkPkgVer.Major >= 2 )
         )
      {
         var sdkPackageLibraries = lockFile.Targets[0].Libraries.Where( l => l.Name == packageSDKPackageID );

         if ( sdkPkgVer != null )
         {
            sdkPackageLibraries = sdkPackageLibraries.Where( l => l.Version.Version >= sdkPkgVer );
         }

         var sdkPackageLibrary = sdkPackageLibraries.FirstOrDefault();

         if ( sdkPackageLibrary == null && sdkPkgVer != null )
         {
            // We need to restore the correctly versioned SDK package
            sdkPackageLibrary = ( await restorer.RestoreIfNeeded( packageSDKPackageID, packageSDKPackageVersion, token ) ).Targets[0].GetTargetLibrary( packageSDKPackageID );
         }

         if ( sdkPackageLibrary != null )
         {
            sdkPackages.UnionWith( sdkPackageLibrary.CompileTimeAssemblies.Select( cta => Path.GetFileNameWithoutExtension( cta.Path ) ).FilterUnderscores() );
         }
      }

      // Actually -> return LockFile, but modify it so that sdk packages are removed
      var targetLibs = lockFile.Targets[0].Libraries;
      for ( var i = 0; i < targetLibs.Count; )
      {
         var curLib = targetLibs[i];
         var contains = sdkPackages.Contains( curLib.Name );
         if ( contains
            || (
               ( curLib.RuntimeAssemblies.Count <= 0 || !curLib.RuntimeAssemblies.Select( ra => ra.Path ).FilterUnderscores().Any() )
               && curLib.RuntimeTargets.Count <= 0
               && curLib.ResourceAssemblies.Count <= 0
               && curLib.NativeLibraries.Count <= 0
               )
            )
         {
            targetLibs.RemoveAt( i );
            if ( !contains )
            {
               sdkPackages.Add( curLib.Name );
            }
         }
         else
         {
            ++i;
         }
      }

      var libs = lockFile.Libraries;
      for ( var i = 0; i < libs.Count; )
      {
         var curLib = libs[i];
         if ( sdkPackages.Contains( curLib.Name ) )
         {
            libs.RemoveAt( i );
         }
         else
         {
            ++i;
         }
      }

      return (lockFile, packageFramework, epAssemblyPath, runtimeConfig);
   }

   private static Int32 GetNextSeparatorIndex( String path, Int32 start )
   {
      return path.IndexOfAny(
         new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
         start );

   }

   private static String CreateTargetDirectory(
      String targetDirectory
      )
   {
      if ( String.IsNullOrEmpty( targetDirectory ) )
      {
         targetDirectory = Path.Combine( Path.GetTempPath(), "NuGetProcess_" + Guid.NewGuid() );
      }

      if ( !Directory.Exists( targetDirectory ) )
      {
         Directory.CreateDirectory( targetDirectory );
      }

      return targetDirectory;
   }

   private static String DeployByGeneratingConfigFiles(
      LockFile lockFile,
      String epAssemblyPath,
      JToken runtimeConfig,
      String sdkPackageID,
      String sdkPackageVersion,
      String targetPath
      )
   {
      // Create DependencyContext, which will be the contents of our .deps.json file
      // deps.json file is basically lock file, with a bit different structure and less information
      // So we can generate it from our lock file.
      var target = lockFile.Targets[0];
      var allLibs = lockFile.Libraries
         .ToDictionary( lib => lib.Name, lib => lib );
      // TODO at least in .NET Core 2.0, all the SDK assemblies ('trusted' assemblies) will *not* have "runtime" entry in their library json.
      var ctx = new DependencyContext(
         new TargetInfo( target.Name, null, null, true ), // portable will be false for native deployments, TODO detect that
         new CompilationOptions( Empty<String>.Enumerable, null, null, null, null, null, null, null, null, null, null, null ),
         Empty<CompilationLibrary>.Enumerable,
         target.Libraries.Select( targetLib =>
         {
            var lib = allLibs[targetLib.Name];
            var hash = lib.Sha512;
            if ( !String.IsNullOrEmpty( hash ) )
            {
               hash = "sha512-" + hash;
            }
            return new RuntimeLibrary(
                lib.Type,
                lib.Name.ToLowerInvariant(),
                lib.Version.ToNormalizedString(),
                hash,
                new RuntimeAssetGroup( "", targetLib.RuntimeAssemblies.Select( ra => ra.Path ) ).Singleton().Concat( TransformRuntimeTargets( targetLib.RuntimeTargets, "runtime" ) ).ToList(),
                new RuntimeAssetGroup( "", targetLib.NativeLibraries.Select( ra => ra.Path ) ).Singleton().Concat( TransformRuntimeTargets( targetLib.RuntimeTargets, "native" ) ).ToList(),
                targetLib.ResourceAssemblies.Select( ra => new ResourceAssembly( ra.Path, ra.Properties["locale"] ) ).ToList(),
                targetLib.Dependencies
                  .Where( dep => allLibs.ContainsKey( dep.Id ) )
                  .Select( dep => new Dependency( dep.Id.ToLowerInvariant(), allLibs[dep.Id].Version.ToNormalizedString() ) ),
                true,
                lib.Path, // Specify path even for EP package, if it happens to consist of multiple assemblies
                lib.Files.FirstOrDefault( f => f.EndsWith( "sha512" ) )
                );
         } ),
         Empty<RuntimeFallbacks>.Enumerable // TODO proper generation of this, prolly related to native stuff
         );

      // Copy EP Assembly
      var targetAssembly = Path.Combine( targetPath, Path.GetFileName( epAssemblyPath ) );
      File.Copy( epAssemblyPath, targetAssembly, true );

      // Write .deps.json file
      // The .deps.json extension is in Microsoft.Extensions.DependencyModel.DependencyContextLoader as a const field, but it is private... :/
      using ( var fs = File.Open( Path.ChangeExtension( targetAssembly, DEPS_EXTENSION ), FileMode.Create, FileAccess.Write, FileShare.None ) )
      {
         new DependencyContextWriter().Write( ctx, fs );
      }

      // Write runtimeconfig.json file
      WriteRuntimeConfigFile(
         runtimeConfig,
         sdkPackageID,
         sdkPackageVersion,
         lockFile,
         true,
         targetAssembly
         );
      return targetAssembly;
   }

   private static IEnumerable<RuntimeAssetGroup> TransformRuntimeTargets( IEnumerable<LockFileRuntimeTarget> runtimeTargets, String key )
   {
      return runtimeTargets
         .GroupBy( rt => rt.Runtime )
         .Where( grp => grp.Any( rtLib => String.Equals( key, rtLib.AssetType, StringComparison.OrdinalIgnoreCase ) ) )
         .Select( grp => new RuntimeAssetGroup( grp.Key, grp.Where( rtLib => String.Equals( key, rtLib.AssetType, StringComparison.OrdinalIgnoreCase ) ).Select( rtLib => rtLib.Path ) ) );
   }

   private static String DeployByCopyingAssemblies(
      BoundRestoreCommandUser restorer,
      LockFile lockFile,
      String epAssemblyPath,
      JToken runtimeConfig,
      String sdkPackageID,
      String sdkPackageVersion,
      String targetPath
      )
   {
      var allAssemblyPaths = restorer.ExtractAssemblyPaths(
         lockFile,
         ( packageID, assemblies ) => assemblies
         ).Values
         .SelectMany( v => v )
         .Select( p => Path.GetFullPath( p ) )
         .ToArray();

      // TODO flat copy will cause problems for assemblies with same simple name but different public key token
      // We need to put conflicting files into separate directories and generate appropriate runtime.config file (or .(dll|exe).config for desktop frameworks!)
      Parallel.ForEach( allAssemblyPaths, curPath => File.Copy( curPath, Path.Combine( targetPath, Path.GetFileName( curPath ) ), false ) );

      var targetAssemblyName = Path.Combine( targetPath, Path.GetFileName( epAssemblyPath ) );

      if ( restorer.ThisFramework.IsDesktop() )
      {
         // TODO .config file for conflicting file names
      }
      else
      {
         // We have to generate runtimeconfig.file (but disable probing path section creation)
         WriteRuntimeConfigFile(
            runtimeConfig,
            sdkPackageID,
            sdkPackageVersion,
            lockFile,
            false,
            targetAssemblyName
            );
      }

      return targetAssemblyName;
   }

   private static void WriteRuntimeConfigFile(
      JToken runtimeConfig,
      String sdkPackageID,
      String sdkPackageVersion,
      LockFile lockFile,
      Boolean generateProbingPaths,
      String targetAssemblyPath
      )
   {
      // Unfortunately, there doesn't seem to be API for this like there is Microsoft.Extensions.DependencyModel for .deps.json file... :/
      // So we need to directly manipulate JSON structure.
      if ( runtimeConfig == null )
      {
         runtimeConfig = new JObject();
      }
      runtimeConfig.SetToken( RUNTIME_CONFIG_FW_NAME, sdkPackageID );
      runtimeConfig.SetToken( RUNTIME_CONFIG_FW_VERSION, sdkPackageVersion );
      runtimeConfig.SetToken( RUNTIME_CONFIG_TFM, lockFile.Targets[0].TargetFramework.GetShortFolderName() );
      if ( lockFile != null )
      {
         var probingPaths = ( runtimeConfig.SelectToken( RUNTIME_CONFIG_PROBING_PATHS ) as JArray ) ?? new JArray();
         probingPaths.AddRange( lockFile.PackageFolders.Select( folder =>
         {
            var packageFolder = Path.GetFullPath( folder.Path );
            // We must strip trailing '\' or '/', otherwise probing will fail
            var lastChar = packageFolder[packageFolder.Length - 1];
            if ( lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar )
            {
               packageFolder = packageFolder.Substring( 0, packageFolder.Length - 1 );
            }

            return (JToken) packageFolder;
         } ) );
         runtimeConfig.SetToken( RUNTIME_CONFIG_PROBING_PATHS, probingPaths );
      }

      File.WriteAllText(
         Path.ChangeExtension( targetAssemblyPath, RUNTIME_CONFIG_EXTENSION ),
         runtimeConfig.ToString( Formatting.Indented ), // Runtimeconfig is small file usually so just use this instead of filestream + textwriter + jsontextwriter combo.
         new UTF8Encoding( false, false ) // The Encoding.UTF8 emits byte order mark, which we don't want to do
         );
   }

   internal static void SetToken( this JToken obj, String path, JToken value )
   {
      var existing = obj.SelectToken( path );
      if ( existing == null )
      {
         var cur = (JObject) obj;
         foreach ( var prop in path.Split( '.' ) )
         {
            if ( cur[prop] == null )
            {
               cur.Add( new JProperty( prop, new JObject() ) );
            }
            cur = (JObject) cur[prop];
         }
         cur.Replace( value );
      }
      else
      {
         existing.Replace( value );
      }
   }
}