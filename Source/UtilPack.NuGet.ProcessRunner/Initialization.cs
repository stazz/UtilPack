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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UtilPack.NuGet.ProcessRunner
{
   internal class Initialization
   {
      private const String RUNTIME_CONFIG_FW_NAME = "runtimeOptions.framework.name";
      private const String RUNTIME_CONFIG_FW_VERSION = "runtimeOptions.framework.version";
      private const String RUNTIME_CONFIG_PROBING_PATHS = "runtimeOptions.additionalProbingPaths";
      private const String DEPS_EXTENSION = ".deps.json";
      private const String RUNTIME_CONFIG_EXTENSION = ".runtimeconfig.json";

      private readonly InitializationConfiguration _config;

      public Initialization( InitializationConfiguration config )
      {
         this._config = ArgumentValidator.ValidateNotNull( nameof( config ), config );
      }

      // Returns:
      // Full path to target assembly, which should be all set up for execution
      // Target NuGet framework
      internal async Task<(String, NuGetFramework)> PerformInitializationAsync(
         CancellationToken token
         )
      {
         var processConfig = this._config;

         String assemblyToBeExecuted = null;
         NuGetFramework targetFW = null;
         using ( var sourceCache = new SourceCacheContext() )
         {
            var logger = new TextWriterLogger( new TextWriterLoggerOptions()
            {
               DebugWriter = null
            } );
            var nugetSettings = UtilPackNuGetUtility.GetNuGetSettingsWithDefaultRootDirectory(
               Path.GetDirectoryName( new Uri( typeof( Program ).GetTypeInfo().Assembly.CodeBase ).LocalPath ),
               processConfig.NuGetConfigurationFile
            );

            (var identity, var fwInfo, var entryPointAssembly) = await this.PerformInitialRestore(
               nugetSettings,
               sourceCache,
               logger,
               token
               );

            if ( identity != null && fwInfo != null && !String.IsNullOrEmpty( entryPointAssembly ) )
            {
               targetFW = fwInfo.TargetFramework;
               using ( var restorer = new BoundRestoreCommandUser(
                  nugetSettings,
                  sourceCacheContext: sourceCache,
                  nugetLogger: logger,
                  thisFramework: targetFW,
                  leaveSourceCacheOpen: true
                  ) )
               {

                  (var lockFile, var runtimeConfig, var sdkPackageID, var sdkPackageVersion) = await this.PerformActualRestore(
                     restorer,
                     targetFW,
                     identity,
                     entryPointAssembly,
                     token
                     );

                  switch ( processConfig.DeploymentKind )
                  {
                     case DeploymentKind.GenerateConfigFiles:
                        if ( targetFW.IsDesktop() )
                        {
                           // This is not supported for desktop framework
                           // TODO log warning
                           assemblyToBeExecuted = DeployByCopyingAssemblies(
                              restorer,
                              lockFile,
                              targetFW,
                              entryPointAssembly,
                              runtimeConfig,
                              sdkPackageID,
                              sdkPackageVersion,
                              CreateTargetPath()
                              );
                        }
                        else
                        {
                           assemblyToBeExecuted = DeployByGeneratingConfigFiles(
                              lockFile,
                              identity,
                              entryPointAssembly,
                              runtimeConfig,
                              sdkPackageID,
                              sdkPackageVersion,
                              CreateTargetPath()
                              );
                        }
                        break;
                     case DeploymentKind.CopyNonSDKAssemblies:
                        assemblyToBeExecuted = DeployByCopyingAssemblies(
                           restorer,
                           lockFile,
                           targetFW,
                           entryPointAssembly,
                           runtimeConfig,
                           sdkPackageID,
                           sdkPackageVersion,
                           CreateTargetPath()
                           );
                        break;
                     default:
                        throw new NotSupportedException( $"Unrecognized deployment kind: {processConfig.DeploymentKind}." );
                  }
               }
            }
         }

         return (assemblyToBeExecuted, targetFW);
      }

      private async Task<(PackageIdentity, FrameworkSpecificGroup, String)> PerformInitialRestore(
         ISettings nugetSettings,
         SourceCacheContext sourceCache,
         ILogger logger,
         CancellationToken token
         )
      {
         var config = this._config;
         (PackageIdentity, FrameworkSpecificGroup, String) retVal = (null, null, null);
         using ( var restorer = new BoundRestoreCommandUser(
            nugetSettings,
            sourceCacheContext: sourceCache,
            nugetLogger: logger,
            thisFramework: NuGetFramework.AgnosticFramework, // This will cause to only the target package to be restored, and none of its dependencies
            leaveSourceCacheOpen: true
            ) )
         {
            var lockFile = await restorer.RestoreIfNeeded( config.ProcessPackageID, config.ProcessPackageVersion, token );

            var pathWithinRepository = lockFile.Libraries.FirstOrDefault()?.Path;
            var packagePath = String.IsNullOrEmpty( pathWithinRepository ) ?
               null :
               lockFile.PackageFolders
                  .Select( folder => Path.Combine( folder.Path, pathWithinRepository ) )
                  .FirstOrDefault( folder => Directory.Exists( folder ) );
            if ( !String.IsNullOrEmpty( packagePath ) )
            {

               FrameworkSpecificGroup[] libItems;
               PackageIdentity packageID;
               using ( var reader = new PackageFolderReader( packagePath ) )
               {
                  libItems = reader.GetLibItems().ToArray();
                  packageID = reader.GetIdentity();
               }

               var possibleAssemblies = libItems
                  .SelectMany( item => item.Items.Select( i => (item, i) ) )
                  .Where( tuple => PackageHelper.IsAssembly( tuple.Item2 ) )
                  .Select( tuple => (tuple.Item1, Path.GetFullPath( Path.Combine( packagePath, tuple.Item2 ) )) )
                  .ToArray();
               var possibleAssemblyPaths = possibleAssemblies.Select( tuple => tuple.Item2 ).ToArray();
               var matchingAssembly = UtilPackNuGetUtility.GetAssemblyPathFromNuGetAssemblies(
                  possibleAssemblyPaths,
                  packagePath,
                  config.ProcessAssemblyPath
                  );
               if ( !String.IsNullOrEmpty( matchingAssembly ) )
               {
                  var assemblyInfo = possibleAssemblies[Array.IndexOf( possibleAssemblyPaths, matchingAssembly )];
                  retVal = (packageID, assemblyInfo.Item1, assemblyInfo.Item2);
               }
            }
         }

         return retVal;
      }

      private async Task<(LockFile, JToken, String, String)> PerformActualRestore(
         BoundRestoreCommandUser restorer,
         NuGetFramework targetFramework,
         PackageIdentity identity,
         String entryPointAssemblyPath,
         CancellationToken token
         )
      {
         var config = this._config;
         var lockFile = await restorer.RestoreIfNeeded( identity.Id, identity.Version.ToNormalizedString(), token );
         JToken runtimeConfig = null;
         String sdkPackageID = null;
         String sdkPackageVersion = null;
         var runtimeConfigPath = Path.ChangeExtension( entryPointAssemblyPath, RUNTIME_CONFIG_EXTENSION );
         if ( File.Exists( runtimeConfigPath ) )
         {
            try
            {
               using ( var streamReader = new StreamReader( File.OpenRead( runtimeConfigPath ) ) )
               using ( var jsonReader = new Newtonsoft.Json.JsonTextReader( streamReader ) )
               {
                  runtimeConfig = JToken.ReadFrom( jsonReader );
               }
               sdkPackageID = ( runtimeConfig.SelectToken( RUNTIME_CONFIG_FW_NAME ) as JValue )?.Value?.ToString();
               sdkPackageVersion = ( runtimeConfig.SelectToken( RUNTIME_CONFIG_FW_VERSION ) as JValue )?.Value?.ToString();
            }
            catch
            {
               // Ignore
            }
         }

         sdkPackageID = UtilPackNuGetUtility.GetSDKPackageID( targetFramework, config.ProcessFrameworkPackageID ?? sdkPackageID );
         sdkPackageVersion = UtilPackNuGetUtility.GetSDKPackageVersion( targetFramework, sdkPackageID, config.ProcessFrameworkPackageVersion ?? sdkPackageVersion );

         var sdkPackages = new HashSet<String>( lockFile.ComputeClosedSet(
            lockFile.Targets[0],
            sdkPackageID.Singleton()
            )
            .Select( lib => lib.Name )
            );

         // Actually -> return LockFile, but modify it so that sdk packages are removed
         var targetLibs = lockFile.Targets[0].Libraries;
         for ( var i = 0; i < targetLibs.Count; )
         {
            var curLib = targetLibs[i];
            var contains = sdkPackages.Contains( curLib.Name );
            if ( contains
               || (
                  ( curLib.RuntimeAssemblies.Count <= 0 || curLib.RuntimeAssemblies.All( ass => ass.Path.EndsWith( "_._" ) ) )
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

         return (lockFile, runtimeConfig, sdkPackageID, sdkPackageVersion);
      }

      private static String DeployByGeneratingConfigFiles(
         LockFile lockFile,
         PackageIdentity identity,
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
                   lib.Path, // Specify path even for EP package, if it happens to consist of multiple 
                   lib.Files.FirstOrDefault( f => f.EndsWith( "sha512" ) )
                   );
            } ),
            Empty<RuntimeFallbacks>.Enumerable
            );

         // Copy EP Assembly
         var targetAssembly = Path.Combine( targetPath, Path.GetFileName( epAssemblyPath ) );
         File.Copy( epAssemblyPath, targetAssembly );

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
         NuGetFramework targetFW,
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

         if ( targetFW.IsDesktop() )
         {
            // TODO .config file for conflicting file names
         }
         else
         {
            // We have to generate runtimeconfig.file (pass 'null' as LockFile to disable probing path section creation)
            WriteRuntimeConfigFile(
               runtimeConfig,
               sdkPackageID,
               sdkPackageVersion,
               null,
               targetAssemblyName
               );
         }

         return targetAssemblyName;
      }

      private static String CreateTargetPath()
      {
         var retVal = Path.Combine( Path.GetTempPath(), "NuGetProcess_" + Guid.NewGuid().ToString() );
         Directory.CreateDirectory( retVal );
         return retVal;
      }

      private static void WriteRuntimeConfigFile(
         JToken runtimeConfig,
         String sdkPackageID,
         String sdkPackageVersion,
         LockFile lockFile,
         String targetAssemblyPath
         )
      {
         // Unfortunately, there doesn't seem to be API for this like there is Microsoft.Extensions.DependencyModel for .deps.json file... :/
         if ( runtimeConfig == null )
         {
            runtimeConfig = new JObject();
         }
         runtimeConfig.SetToken( RUNTIME_CONFIG_FW_NAME, sdkPackageID );
         runtimeConfig.SetToken( RUNTIME_CONFIG_FW_VERSION, sdkPackageVersion );
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
            runtimeConfig.ToString( Formatting.Indented ), // Seems there is no way to write JTokens directly to stream
            new UTF8Encoding( false, false ) // The Encoding.UTF8 emits byte order mark, which we don't want to do
            );
      }
   }
}

public static class E_ProcessRunner
{
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