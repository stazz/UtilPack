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
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol.Core.Types;
using NuGet.Repositories;
using NuGet.Versioning;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilPack.NuGet
{
   /// <summary>
   /// This class binds itself to specific <see cref="NuGetFramework"/> and then performs restore commands via <see cref="RestoreIfNeeded(string, string)"/> method.
   /// It also caches results so that restore command is invoked only if needed.
   /// </summary>
   public class BoundRestoreCommandUser : IDisposable
   {
      private const String NUGET_FW = "NuGetFramework";
      internal const String NUGET_FW_PACKAGE_ID = "NuGetFrameworkPackageID";
      internal const String NUGET_FW_PACKAGE_VERSION = "NuGetFrameworkPackageVersion";


      private readonly SourceCacheContext _cacheContext;
      private readonly RestoreCommandProviders _restoreCommandProvider;
      private readonly String _nugetRestoreRootDir; // NuGet restore command never writes anything to disk (apart from packages themselves), but if certain file paths are omitted, it simply fails with argumentnullexception when invoking Path.Combine or Path.GetFullName. So this can be anything, really, as long as it's understandable by Path class.
      private readonly TargetFrameworkInformation _restoreTargetFW;
      private LockFile _previousLockFile;
      private readonly ConcurrentDictionary<String, ConcurrentDictionary<NuGetVersion, LockFile>> _allLockFiles;

      /// <summary>
      /// Creates new instance of <see cref="BoundRestoreCommandUser"/> with given parameters.
      /// </summary>
      /// <param name="thisFramework">The framework to bind to.</param>
      /// <param name="nugetSettings">The settings to use.</param>
      /// <param name="nugetLogger">The logger to use in restore command.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="thisFramework"/> or <paramref name="nugetSettings"/> is <c>null</c>.</exception>
      public BoundRestoreCommandUser(
         NuGetFramework thisFramework,
         ISettings nugetSettings,
         ILogger nugetLogger
         )
      {
         this.ThisFramework = ArgumentValidator.ValidateNotNull( nameof( thisFramework ), thisFramework );
         ArgumentValidator.ValidateNotNull( nameof( nugetSettings ), nugetSettings );
         if ( nugetLogger == null )
         {
            nugetLogger = NullLogger.Instance;
         }

         var global = SettingsUtility.GetGlobalPackagesFolder( nugetSettings );
         var fallbacks = SettingsUtility.GetFallbackPackageFolders( nugetSettings );
         var ctx = new SourceCacheContext();
         var psp = new PackageSourceProvider( nugetSettings );
         var csp = new global::NuGet.Protocol.CachingSourceProvider( psp );
         this._cacheContext = ctx;

         this.NuGetLogger = nugetLogger;
         this._restoreCommandProvider = RestoreCommandProviders.Create(
            global,
            fallbacks,
            new PackageSourceProvider( nugetSettings ).LoadPackageSources().Where( s => s.IsEnabled ).Select( s => csp.CreateRepository( s ) ),
            ctx,
            nugetLogger
            );
         this._nugetRestoreRootDir = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() );
         this._restoreTargetFW = new TargetFrameworkInformation()
         {
            FrameworkName = this.ThisFramework
         };
         this.LocalRepositories = this._restoreCommandProvider.GlobalPackages.Singleton().Concat( this._restoreCommandProvider.FallbackPackageFolders ).ToDictionary( r => r.RepositoryRoot, r => r );
         this._allLockFiles = new ConcurrentDictionary<String, ConcurrentDictionary<NuGetVersion, LockFile>>();
      }

      /// <summary>
      /// Gets the framework that this <see cref="BoundRestoreCommandUser"/> is bound to.
      /// </summary>
      /// <value>The framework that this <see cref="BoundRestoreCommandUser"/> is bound to.</value>
      public NuGetFramework ThisFramework { get; }

      /// <summary>
      /// Gets the logger used in restore command.
      /// </summary>
      /// <value>The framework used in restore command.</value>
      public ILogger NuGetLogger { get; }

      /// <summary>
      /// Gets the local repositories by their root path.
      /// </summary>
      /// <value>The local repositories by their root path.</value>
      public IReadOnlyDictionary<String, NuGetv3LocalRepository> LocalRepositories { get; }

      /// <summary>
      /// Performs restore command for given package and version, if not already cached.
      /// Returns resulting lock file.
      /// </summary>
      /// <param name="packageID">The package ID to use.</param>
      /// <param name="version">The version to use. Should be parseable into <see cref="VersionRange"/>. If <c>null</c> or empty, <see cref="VersionRange.AllFloating"/> will be used.</param>
      /// <returns>Cached or restored lock file.</returns>
      /// <seealso cref="RestoreCommand"/>
      public async ValueTask<LockFile> RestoreIfNeeded(
         String packageID,
         String version
         )
      {
         // Prepare for invoking restore command
         LockFile retVal;
         if ( !String.IsNullOrEmpty( packageID ) )
         {
            VersionRange versionRange;
            if ( String.IsNullOrEmpty( version ) )
            {
               // Accept all versions, and pick the newest
               versionRange = VersionRange.AllFloating;
            }
            else
            {
               // Accept specific version range
               versionRange = VersionRange.Parse( version );
            }

            // Invoking restore command is quite expensive, so let's try to see if we have cached result
            retVal = null;
            if ( !versionRange.IsFloating && this._allLockFiles.TryGetValue( packageID, out var thisPackageLockFiles ) )
            {
               var matchingActualVersion = versionRange.FindBestMatch( thisPackageLockFiles.Keys.Where( v => versionRange.Satisfies( v ) ) );
               if ( matchingActualVersion != null )
               {
                  retVal = thisPackageLockFiles[matchingActualVersion];
               }
            }

            if ( retVal == null )
            {
               retVal = await this.PerformRestore( packageID, versionRange );
               var actualVersion = retVal.Libraries.First( l => String.Equals( l.Name, packageID ) ).Version;
               this._allLockFiles
                  .GetOrAdd( packageID, p => new ConcurrentDictionary<NuGetVersion, LockFile>() )
                  .TryAdd( actualVersion, retVal );
            }


            // Restore command never modifies existing lock file object, instead it creates new one
            // Just update to newest (thus the next request will be able to use cached information and be faster)
            System.Threading.Interlocked.Exchange( ref this._previousLockFile, retVal );

         }
         else
         {
            retVal = null;
         }

         return retVal;
      }

      /// <summary>
      /// This method is invoked by <see cref="RestoreIfNeeded"/> when the lock file is not in cache and restore command needs to be actually run.
      /// </summary>
      /// <param name="packageID">The package ID.</param>
      /// <param name="versionRange">The <see cref="VersionRange"/> of package.</param>
      /// <returns>The <see cref="LockFile"/> generated by <see cref="RestoreCommand"/>.</returns>
      protected virtual async System.Threading.Tasks.Task<LockFile> PerformRestore(
         String packageID,
         VersionRange versionRange
         )
      {
         var spec = new PackageSpec()
         {
            Name = $"Restoring {packageID}",
            FilePath = Path.Combine( this._nugetRestoreRootDir, "dummy" )
         };
         spec.TargetFrameworks.Add( this._restoreTargetFW );

         spec.Dependencies.Add( new LibraryDependency()
         {
            LibraryRange = new LibraryRange( packageID, versionRange, LibraryDependencyTarget.Package )
         } );

         var request = new RestoreRequest(
            spec,
            this._restoreCommandProvider,
            this._cacheContext,
            this.NuGetLogger
            )
         {
            ProjectStyle = ProjectStyle.Standalone,
            RestoreOutputPath = this._nugetRestoreRootDir,
            ExistingLockFile = this._previousLockFile
         };
         return ( await ( new RestoreCommand( request ).ExecuteAsync() ) ).LockFile;
      }

      /// <summary>
      /// Disposes the managed resources held by this <see cref="BoundRestoreCommandUser"/>
      /// </summary>
      public void Dispose()
      {
         this._cacheContext.DisposeSafely();
      }
   }
}
