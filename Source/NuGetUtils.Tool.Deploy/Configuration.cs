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
using NuGetUtils.Lib.Deployment;
using NuGetUtils.Lib.Restore;
using NuGetUtils.Lib.Tool;
using System;
using UtilPack.Documentation;
using static NuGetUtils.Lib.Tool.DefaultDocumentation;

namespace NuGetUtils.Tool.Deploy
{
   internal sealed class NuGetDeploymentConfigurationImpl : NuGetDeploymentConfiguration, NuGetUsageConfiguration
   {

      [
         Required,
         Description( ValueName = "packageID", Description = "The package ID of the NuGet package containing the entry point assembly." )
         ]
      public String PackageID { get; set; }


      [
         Description( ValueName = "packageVersion", Description = "The package version of the NuGet package containing the entry point assembly. The normal NuGet version notation is supported. If this is not specified, then highest floating version is assumed, thus causing queries to remote NuGet servers." )
         ]
      public String PackageVersion { get; set; }


      [
         Required( Conditional = true ),
         Description( ValueName = "path", Description = "The path within the resolved library folder of the package, where the assembly resides. Will not be used if there is only one assembly, and is optional if there is assembly with the same name as the package. Otherwise this is required." )
         ]
      public String AssemblyPath { get; set; }

      [
         Description( ValueName = "GenerateConfigFiles|CopyNonSDKAssemblies", Description = "How the deployment is performed: by generating .deps.json and .runtimeconfig.json files (GenerateConfigFiles), or by copying all non-framework dependency assemblies to target folder (CopyNonSDKAssemblies). The GenerateConfigFiles option is not available for .NET Desktop deployments." )
         ]
      public DeploymentKind DeploymentKind { get; set; }

      [
         Description( Description = "Set this to true in order to force package framework to be interpreted as package-based-framework." )
         ]
      public Boolean? PackageFrameworkIsPackageBased { get; set; }

      [
         Description( ValueName = "path", Description = "Where to deploy the assembly and supplemental files. If not specified, a randomly-generated folder will be created within the system temporary directory." )
         ]
      public String TargetDirectory { get; set; }

      [
         Description( ValueName = "packageID", Description = "Use this to override the package ID of the SDK package of the framework of the restored NuGet package." )
         ]
      public String PackageSDKFrameworkPackageID { get; }

      [
         Description( ValueName = "packageVersion", Description = "Use this to override the package version of the SDK package of the framework of the restored NuGet package." )
         ]
      public String PackageSDKFrameworkPackageVersion { get; }


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
