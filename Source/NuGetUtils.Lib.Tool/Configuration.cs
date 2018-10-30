/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
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
using NuGet.Common;
using NuGet.ProjectModel;
using NuGetUtils.Lib.Restore;
using System;

namespace NuGetUtils.Lib.Tool
{
   public interface NuGetUsageConfiguration
   {
      String NuGetConfigurationFile { get; }


      String RestoreFramework { get; }

      String LockFileCacheDirectory { get; }


      String SDKFrameworkPackageID { get; }


      String SDKFrameworkPackageVersion { get; }


      Boolean DisableLockFileCache { get; }


      LogLevel LogLevel { get; }


      Boolean DisableLogging { get; }
   }

   public interface ConfigurationConfiguration
   {
      String ConfigurationFileLocation { get; }
   }

   public static class DefaultDocumentation
   {
      public const String NuGetConfigurationFileValue = "path";
      public const String NuGetConfigurationFileDescription = "The path to NuGet configuration file containing the settings to use. The default NuGet setting file behaviour is the default if this is not specified.";

      public const String RestoreFrameworkValue = "framework";
      public const String RestoreFrameworkDescription = "The name of the current framework of the process (e.g. \".NETCoreApp,v=2.1\"). If automatic detection of the process framework does not work, then use this parameter to override.";

      public const String LockFileCacheDirectoryValue = "path";

      public const String LockFileCacheDirectoryDescription1 = "The path of the directory acting as cache directory for restored package information (serialized " + nameof( LockFile ) + "s). By default, the environment variable \"";
      public const String LockFileCacheDirectoryDescription2 = "\" is first checked, and if that is not present, the \"";
      public const String LockFileCacheDirectoryDescription3 = "\" directory within current home directory is used.";
      public const String LockFileCacheDirectoryDescription = LockFileCacheDirectoryDescription1 + Consts.LOCK_FILE_CACHE_DIR_ENV_NAME + LockFileCacheDirectoryDescription2 + Consts.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR + LockFileCacheDirectoryDescription3;

      public const String SDKFrameworkPackageIDValue = "packageID";
      public const String SDKFrameworkPackageIDDescription = "The package ID of the current framework SDK package. This depends on a auto-detected or explicitly specified restore framework, but is typically \"" + UtilPackNuGetUtility.SDK_PACKAGE_NETCORE + "\".";

      public const String SDKFrameworkPackageVersionValue = "version";
      public const String SDKFrameworkPackageVersionDescription = "The package version of the framework SDK package. By default, it is deduced from path of the assembly containing " + nameof( Object ) + " type.";

      public const String DisableLockFileCacheDescription = "Whether to disable using cache directory for restored package information (serialized " + nameof( LockFile ) + "s).";


      public const String LogLevelValue = "level";
      public const String LogLevelDescription = "Which log level to use for NuGet logger. By default, this is " + nameof( LogLevel.Information ) + ".";

      public const String DisableLoggingDescription = "Whether to disable NuGet logging completely.";

      public const String ConfigurationFileLocationValue = "path";
      public const String ConfigurationFileLocationDescription = "The path to the JSON file containing the configuration with all the same information as normal command line parameters.";
   }
}
