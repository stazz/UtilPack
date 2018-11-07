using NuGet.Common;
using NuGet.Frameworks;
using NuGetUtils.Lib.Restore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UtilPack;

namespace NuGetUtils.Lib.Restore
{
   /// <summary>
   /// This is data interface for configuration which deals with usecase of creating <see cref="BoundRestoreCommandUser"/> and using it somehow.
   /// </summary>
   /// <remarks>All properties are optional.</remarks>
   /// <seealso cref="E_NuGetUtils.CreateAndUseRestorerAsync"/>
   public interface NuGetUsageConfiguration
   {
      /// <summary>
      /// Gets the path to NuGet configuration file.
      /// </summary>
      /// <value>The path to NuGet configuration file.</value>
      String NuGetConfigurationFile { get; }

      /// <summary>
      /// Gets the name of the NuGet framework that restoring is performed against.
      /// </summary>
      /// <value>The name of the NuGet framework that restoring is performed against.</value>
      String RestoreFramework { get; }

      /// <summary>
      /// Gets the lock file cache directory path.
      /// </summary>
      /// <value>The lock file cache directory path.</value>
      String LockFileCacheDirectory { get; }

      /// <summary>
      /// Gets the SDK NuGet package ID.
      /// </summary>
      /// <value>The SDK NuGet package ID.</value>
      String SDKFrameworkPackageID { get; }

      /// <summary>
      /// Gets the SDK NuGet package version.
      /// </summary>
      /// <value>The SDK NuGet package version.</value>
      String SDKFrameworkPackageVersion { get; }

      /// <summary>
      /// Gets the value indicating whether to disable caching <see cref="NuGet.ProjectModel.LockFile"/>s to disk.
      /// </summary>
      /// <value>The value indicating whether to disable caching <see cref="NuGet.ProjectModel.LockFile"/>s to disk.</value>
      Boolean DisableLockFileCache { get; }

      /// <summary>
      /// Gets the <see cref="NuGet.Common.LogLevel"/> for NuGet logger.
      /// </summary>
      /// <value>the <see cref="NuGet.Common.LogLevel"/> for NuGet logger.</value>
      LogLevel LogLevel { get; }

      /// <summary>
      /// Gets the value indicating whether to disable logging altogether.
      /// </summary>
      /// <value>The value indicating whether to disable logging altogether.</value>
      Boolean DisableLogging { get; }
   }
}

public static partial class E_NuGetUtils
{
   /// <summary>
   /// Given this <see cref="NuGetUsageConfiguration"/>, creates a new instance of <see cref="BoundRestoreCommandUser"/> utilizing the properties of <see cref="NuGetUsageConfiguration"/>, and then calls given <paramref name="callback"/>.
   /// </summary>
   /// <typeparam name="TResult">The return type of given <paramref name="callback"/>.</typeparam>
   /// <param name="configuration">This <see cref="NuGetUsageConfiguration"/>.</param>
   /// <param name="nugetSettingsPath">The object specifying what to pass as first parameter <see cref="NuGetUtility.GetNuGetSettingsWithDefaultRootDirectory"/>: if <see cref="String"/>, then it is passed directly as is, otherwise when it is <see cref="Type"/>, the <see cref="Assembly.CodeBase"/> of the <see cref="Assembly"/> holding the given <see cref="Type"/> is used to extract directory, and that directory is then passed on to <see cref="NuGetUtility.GetNuGetSettingsWithDefaultRootDirectory"/> method.</param>
   /// <param name="lockFileCacheDirEnvName">The environment name of the variable holding default lock file cache directory.</param>
   /// <param name="lockFileCacheDirWithinHomeDir">The directory name within home directory of current user which can be used as lock file cache directory.</param>
   /// <param name="callback">The callback to use created <see cref="BoundRestoreCommandUser"/>. The parameter contains <see cref="BoundRestoreCommandUser"/> as first tuple component, the SDK package ID deduced using <see cref="BoundRestoreCommandUser.ThisFramework"/> and <see cref="NuGetUsageConfiguration.SDKFrameworkPackageID"/> as second tuple component, and the SDK package version deduced using <see cref="BoundRestoreCommandUser.ThisFramework"/>, SDK package ID, and <see cref="NuGetUsageConfiguration.SDKFrameworkPackageVersion"/> as third tuple component.</param>
   /// <returns>The return value of <paramref name="callback"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NuGetUsageConfiguration"/> is <c>null</c>.</exception>
   public static TResult CreateAndUseRestorerAsync<TResult>(
      this NuGetUsageConfiguration configuration,
      EitherOr<String, Type> nugetSettingsPath,
      String lockFileCacheDirEnvName,
      String lockFileCacheDirWithinHomeDir,
      Func<(BoundRestoreCommandUser Restorer, String SDKPackageID, String SDKPackageVersion), TResult> callback
      )
   {
      var targetFWString = configuration.RestoreFramework;

      using ( var restorer = new BoundRestoreCommandUser(
         NuGetUtility.GetNuGetSettingsWithDefaultRootDirectory(
            nugetSettingsPath.IsFirst ? nugetSettingsPath.First : Path.GetDirectoryName( new Uri( nugetSettingsPath.Second.GetTypeInfo().Assembly.CodeBase ).LocalPath ),
            configuration.NuGetConfigurationFile
            ),
         thisFramework: String.IsNullOrEmpty( targetFWString ) ? null : NuGetFramework.Parse( targetFWString ),
         nugetLogger: configuration.DisableLogging ? null : new TextWriterLogger()
         {
            VerbosityLevel = configuration.LogLevel
         },
         lockFileCacheDir: configuration.LockFileCacheDirectory,
         lockFileCacheEnvironmentVariableName: lockFileCacheDirEnvName,
         getDefaultLockFileCacheDir: homeDir => Path.Combine( homeDir, lockFileCacheDirWithinHomeDir ),
         disableLockFileCacheDir: configuration.DisableLockFileCache
         ) )
      {

         var thisFramework = restorer.ThisFramework;
         var sdkPackageID = thisFramework.GetSDKPackageID( configuration.SDKFrameworkPackageID );

         return callback( (
            restorer,
            sdkPackageID,
            thisFramework.GetSDKPackageVersion( sdkPackageID, configuration.SDKFrameworkPackageVersion )
            ) );
      }
   }
}