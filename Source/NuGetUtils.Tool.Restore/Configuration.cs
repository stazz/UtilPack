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
using NuGetUtils.Lib.Common;
using NuGetUtils.Lib.Restore;
using NuGetUtils.Lib.Tool;
using System;
using UtilPack.Documentation;
using static NuGetUtils.Lib.Tool.DefaultDocumentation;

namespace NuGetUtils.Tool.Restore
{
   internal sealed class NuGetRestoreConfiguration : NuGetUsageConfiguration
   {
      [Required( Conditional = true ), Description( ValueName = "packageID", Description = "The ID of the single package to be restored. If this property is specified, the options \"" + nameof( PackageIDs ) + "\" and \"" + nameof( PackageVersions ) + "\" *must not* be specified." )]
      public String PackageID { get; set; }


      [Description( ValueName = "packageVersion", Description = "The version of the package to be restored, ID of which was specified using \"" + nameof( PackageID ) + "\" option. The normal NuGet version notation is supported. If this is not specified, then highest floating version is assumed, thus causing queries to remote NuGet servers." )]
      public String PackageVersion { get; set; }

      [Required( Conditional = true ), Description( ValueName = "packageID list", Description = "The IDs of the multiple packages to be restored. If this property is specified, the options \"" + nameof( PackageID ) + "\" and \"" + nameof( PackageVersion ) + "\" *must not* be specified." )]
      public String[] PackageIDs { get; set; }

      [Description( ValueName = "packageVersion list", Description = "The versions of the packages to be restored, IDs of which were specified using \"" + nameof( PackageIDs ) + "\" option. For each version, the normal NuGet version notation is supported. If the version is not specified, then highest floating version is assumed, thus causing queries to remote NuGet servers." )]
      public String[] PackageVersions { get; set; }

      [Description( Description = "Whether to restore the SDK package (typically \"" + NuGetUtility.SDK_PACKAGE_NETCORE + "\") as well. This is useful in conjunction with nuget-exec tool. The default value is true." )]
      public Boolean SkipRestoringSDKPackage { get; set; }

      [
         Description( ValueName = NuGetConfigurationFileValue, Description = NuGetConfigurationFileDescription )
         ]
      public String NuGetConfigurationFile { get; set; }

      [
         Description( ValueName = RestoreFrameworkValue, Description = RestoreFrameworkDescription )
         ]
      public String RestoreFramework { get; set; }


      [
         Description( ValueName = LockFileCacheDirectoryValue, Description = LockFileCacheDirectoryDescription )
         ]
      public String LockFileCacheDirectory { get; set; }


      [
         Description( ValueName = SDKFrameworkPackageIDValue, Description = SDKFrameworkPackageIDDescription )
         ]
      public String SDKFrameworkPackageID { get; set; }


      [
         Description( ValueName = SDKFrameworkPackageVersionValue, Description = SDKFrameworkPackageVersionDescription )]
      public String SDKFrameworkPackageVersion { get; set; }


      [
         Description( Description = DisableLockFileCacheDescription )
         ]
      public Boolean DisableLockFileCache { get; set; }


      [
         Description( ValueName = LogLevelValue, Description = LogLevelDescription )
         ]
      public LogLevel LogLevel { get; set; } = LogLevel.Information;


      [
         Description( Description = DisableLoggingDescription )
         ]
      public Boolean DisableLogging { get; set; }

   }

   internal class ConfigurationConfigurationImpl : ConfigurationConfiguration
   {
      [
         Required,
         Description( ValueName = ConfigurationFileLocationValue, Description = ConfigurationFileLocationDescription )
         ]
      public String ConfigurationFileLocation { get; set; }
   }
}
