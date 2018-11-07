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
   /// <summary>
   /// This is data interface for configuration which describes the JSON file where the actual configuration should be read from, typically using <see cref="Program{TCommandLineConfiguration, TConfigurationConfiguration}"/>.
   /// </summary>
   public interface ConfigurationConfiguration
   {
      /// <summary>
      /// Gets the path to the JSON file containing the actual configuration.
      /// </summary>
      /// <value>The path to the JSON file containing the actual configuration.</value>
      String ConfigurationFileLocation { get; }
   }

   /// <summary>
   /// This class contains string constants that can be used in <see cref="UtilPack.Documentation.DescriptionAttribute"/> for properties of types implementing <see cref="NuGetUsageConfiguration"/> and <see cref="ConfigurationConfiguration"/>.
   /// </summary>
   public static class DefaultDocumentation
   {
      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.ValueName"/> for property <see cref="NuGetUsageConfiguration.NuGetConfigurationFile"/>.
      /// </summary>
      public const String NuGetConfigurationFileValue = "path";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.Description"/> for property <see cref="NuGetUsageConfiguration.NuGetConfigurationFile"/>.
      /// </summary>
      public const String NuGetConfigurationFileDescription = "The path to NuGet configuration file containing the settings to use. The default NuGet setting file behaviour is the default if this is not specified.";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.ValueName"/> for property <see cref="NuGetUsageConfiguration.RestoreFramework"/>.
      /// </summary>
      public const String RestoreFrameworkValue = "framework";
      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.Description"/> for property <see cref="NuGetUsageConfiguration.RestoreFramework"/>.
      /// </summary>
      public const String RestoreFrameworkDescription = "The name of the current framework of the process (e.g. \".NETCoreApp,v=2.1\"). If automatic detection of the process framework does not work, then use this parameter to override.";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.ValueName"/> for property <see cref="NuGetUsageConfiguration.LockFileCacheDirectory"/>.
      /// </summary>
      public const String LockFileCacheDirectoryValue = "path";

      /// <summary>
      /// The first part of the <see cref="LockFileCacheDirectoryDescription"/>, containing the string before the <see cref="NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_ENV_NAME"/>.
      /// </summary>
      public const String LockFileCacheDirectoryDescription1 = "The path of the directory acting as cache directory for restored package information (serialized " + nameof( LockFile ) + "s). By default, the environment variable \"";

      /// <summary>
      /// The second part of the <see cref="LockFileCacheDirectoryDescription"/>, containing the string after the <see cref="NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_ENV_NAME"/> and before the <see cref="NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR"/>.
      /// </summary>
      public const String LockFileCacheDirectoryDescription2 = "\" is first checked, and if that is not present, the \"";
      /// <summary>
      /// The third part of the <see cref="LockFileCacheDirectoryDescription"/>, containing the string after the <see cref="NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR"/>.
      /// </summary>
      public const String LockFileCacheDirectoryDescription3 = "\" directory within current home directory is used.";
      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.Description"/> for property <see cref="NuGetUsageConfiguration.LockFileCacheDirectory"/>.
      /// </summary>
      public const String LockFileCacheDirectoryDescription = LockFileCacheDirectoryDescription1 + NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_ENV_NAME + LockFileCacheDirectoryDescription2 + NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR + LockFileCacheDirectoryDescription3;

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.ValueName"/> for property <see cref="NuGetUsageConfiguration.SDKFrameworkPackageID"/>.
      /// </summary>
      public const String SDKFrameworkPackageIDValue = "packageID";
      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.Description"/> for property <see cref="NuGetUsageConfiguration.SDKFrameworkPackageID"/>.
      /// </summary>
      public const String SDKFrameworkPackageIDDescription = "The package ID of the current framework SDK package. This depends on a auto-detected or explicitly specified restore framework, but is typically \"" + NuGetUtility.SDK_PACKAGE_NETCORE + "\".";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.ValueName"/> for property <see cref="NuGetUsageConfiguration.SDKFrameworkPackageVersion"/>.
      /// </summary>
      public const String SDKFrameworkPackageVersionValue = "version";
      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.Description"/> for property <see cref="NuGetUsageConfiguration.SDKFrameworkPackageVersion"/>.
      /// </summary>
      public const String SDKFrameworkPackageVersionDescription = "The package version of the framework SDK package. By default, it is deduced from path of the assembly containing " + nameof( Object ) + " type.";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.Description"/> for property <see cref="NuGetUsageConfiguration.DisableLockFileCache"/>.
      /// </summary>

      public const String DisableLockFileCacheDescription = "Whether to disable using cache directory for restored package information (serialized " + nameof( LockFile ) + "s).";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.ValueName"/> for property <see cref="NuGetUsageConfiguration.LogLevel"/>.
      /// </summary>
      public const String LogLevelValue = "level";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.Description"/> for property <see cref="NuGetUsageConfiguration.LogLevel"/>.
      /// </summary>
      public const String LogLevelDescription = "Which log level to use for NuGet logger. By default, this is " + nameof( LogLevel.Information ) + ".";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.Description"/> for property <see cref="NuGetUsageConfiguration.DisableLogging"/>.
      /// </summary>
      public const String DisableLoggingDescription = "Whether to disable NuGet logging completely.";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.ValueName"/> for property <see cref="ConfigurationConfiguration.ConfigurationFileLocation"/>.
      /// </summary>
      public const String ConfigurationFileLocationValue = "path";

      /// <summary>
      /// The value for <see cref="UtilPack.Documentation.DescriptionAttribute.Description"/> for property <see cref="ConfigurationConfiguration.ConfigurationFileLocation"/>.
      /// </summary>
      public const String ConfigurationFileLocationDescription = "The path to the JSON file containing the configuration with all the same information as normal command line parameters.";
   }
}
