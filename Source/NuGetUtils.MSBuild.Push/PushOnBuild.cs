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
using NuGet.Commands;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Repositories;
using NuGetUtils.Lib.Restore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack.NuGet.Common.MSBuild;

namespace UtilPack.NuGet.Push.MSBuild
{
   public class PushTask : Microsoft.Build.Utilities.Task, ICancelableTask
   {
      private readonly CancellationTokenSource _cancelSource;

      public PushTask()
      {
         this._cancelSource = new CancellationTokenSource();
      }

      public void Cancel()
      {
         this._cancelSource.Cancel( false );
      }

      public override Boolean Execute()
      {
         var sourceNames = this.SourceNames;
         if ( !sourceNames.IsNullOrEmpty() )
         {
            var settings = NuGetUtility.GetNuGetSettingsWithDefaultRootDirectory(
               Path.GetDirectoryName( this.BuildEngine.ProjectFileOfTaskNode ),
               this.NuGetConfigurationFilePath );
            var psp = new PackageSourceProvider( settings );
            var packagePath = Path.GetFullPath( this.PackageFilePath );

            var identity = new Lazy<PackageIdentity>( () =>
            {
               using ( var reader = new PackageArchiveReader( packagePath ) )
               {
                  return reader.GetIdentity();
               }
            } );
            var allRepositories = new Lazy<NuGetv3LocalRepository[]>( () =>
               SettingsUtility.GetGlobalPackagesFolder( settings )
               .Singleton()
               .Concat( SettingsUtility.GetFallbackPackageFolders( settings ) )
               .Select( repoPath => new NuGetv3LocalRepository( repoPath ) )
               .ToArray()
               );
            var logger = new NuGetMSBuildLogger(
               "NP0001",
               "NP0002",
               nameof( PushTask ),
               nameof( PushTask ),
               this.BuildEngine
               );

            // MSBuild tasks are synchronous, so use WaitAll
            // TODO call yield?
            Task.WaitAll( sourceNames.Select( sourceItem => this.PerformPushToSingleSourceAsync(
                 settings,
                 packagePath,
                 psp,
                 identity,
                 allRepositories,
                 logger,
                 sourceItem
                 ) )
                 .ToArray()
               );
         }
         else
         {
            this.Log.LogWarning( $"No sources specified for push command, please specify at least one source via \"{nameof( this.SourceNames )}\" property." );
         }

         return true;
      }

      private async Task PerformPushToSingleSourceAsync(
         ISettings settings,
         String packagePath,
         PackageSourceProvider psp,
         Lazy<PackageIdentity> identity,
         Lazy<NuGetv3LocalRepository[]> allRepositories,
         global::NuGet.Common.ILogger logger,
         ITaskItem sourceItem
         )
      {
         var skipOverwrite = sourceItem.GetMetadata( "SkipOverwriteLocalFeed" ).ParseAsBooleanSafe();
         var skipClearRepositories = sourceItem.GetMetadata( "SkipClearingLocalRepositories" ).ParseAsBooleanSafe();
         var skipOfflineFeedOptimization = sourceItem.GetMetadata( "SkipOfflineFeedOptimization" ).ParseAsBooleanSafe();
         var apiKey = sourceItem.GetMetadata( "ApiKey" );
         var symbolSource = sourceItem.GetMetadata( "SymbolSource" );
         var symbolApiKey = sourceItem.GetMetadata( "SymbolApiKey" );
         var noServiceEndPoint = sourceItem.GetMetadata( "NoServiceEndPoint" ).ParseAsBooleanSafe();

         var source = sourceItem.ItemSpec;
         var isLocal = IsLocalFeed( psp, source, out var localPath );
         if ( isLocal && !skipOverwrite )
         {
            this.DeleteDir( OfflineFeedUtility.GetPackageDirectory( identity.Value, localPath ) );
         }

         if ( isLocal && !skipOfflineFeedOptimization )
         {
            // The default v2 repo detection algorithm for PushRunner (PackageUpdateResource.IsV2LocalRepository) always returns true for empty folders, so let's use the OfflineFeedUtility here right away (which will assume v3 repository)
            await OfflineFeedUtility.AddPackageToSource(
               new OfflineFeedAddContext( packagePath, localPath, logger, true, false, false, true ),
               this._cancelSource.Token
               );
         }
         else
         {
            var timeoutString = sourceItem.GetMetadata( "PushTimeout" );
            if ( String.IsNullOrEmpty( timeoutString ) || !Int32.TryParse( timeoutString, out var timeout ) )
            {
               timeout = 1000;
            }

            try
            {
               await PushRunner.Run(
                  settings,
                  psp,
                  packagePath,
                  source,
                  apiKey,
                  symbolSource,
                  symbolApiKey,
                  timeout,
                  false,
                  String.IsNullOrEmpty( symbolSource ),
                  noServiceEndPoint,
                  logger
                  );
            }
            catch ( HttpRequestException e ) when
               ( e.Message.Contains( "already exists. The server is configured to not allow overwriting packages that already exist." ) )
            {
               // Nuget.Server returns this message when attempting to overwrite a package.
               this.Log.LogMessage( $"Package already exists on source {source}, not updated." );
            }
         }

         if ( !skipClearRepositories )
         {
            foreach ( var repo in allRepositories.Value )
            {
               this.DeleteDir( repo.PathResolver.GetInstallPath( identity.Value.Id, identity.Value.Version ) );
            }
         }
      }

      [Required]
      public String PackageFilePath { get; set; }

      public ITaskItem[] SourceNames { get; set; }

      public String NuGetConfigurationFilePath { get; set; }

      public Int32 RetryTimeoutForDirectoryDeletionFail { get; set; } = 500;

      private void DeleteDir( String dir )
      {
         if ( Directory.Exists( dir ) )
         {
            // There are problems with using Directory.Delete( dir, true ); while other process has file watchers on it
            try
            {
               Directory.Delete( dir, true );
            }
            catch ( Exception exc )
            {
               var retryTimeout = this.RetryTimeoutForDirectoryDeletionFail;
               var success = false;
               if ( retryTimeout > 0 )
               {
                  using ( var mres = new System.Threading.ManualResetEventSlim( false, 0 ) )
                  {
                     mres.Wait( retryTimeout );
                  }

                  try
                  {
                     Directory.Delete( dir, true );
                     success = true;
                  }
                  catch
                  {
                     // Do not retry more times to avoid endless/slow loop

                  }
               }

               if ( !success )
               {
                  this.Log.LogWarning( $"Failed to delete directory {dir}: {exc.Message}." );
               }
            }
         }
      }

      private static Boolean IsLocalFeed(
         PackageSourceProvider psp,
         String source,
         out String path
         )
      {
         PackageSource sourceSpec;
         if ( Path.IsPathRooted( source ) )
         {
            path = source;
         }
         else if ( ( sourceSpec = psp.LoadPackageSources().FirstOrDefault( s => String.Equals( s.Name, source ) ) )?.IsLocal ?? false )
         {
            path = sourceSpec.Source;
         }
         else
         {
            path = null;
         }

         return path != null;
      }
   }
}
