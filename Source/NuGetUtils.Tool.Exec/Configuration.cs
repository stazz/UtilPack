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
using NuGetUtils.Lib.EntryPoint;
using NuGetUtils.Lib.Exec;
using NuGetUtils.Lib.Restore;
using NuGetUtils.Lib.Tool;
using System;
using UtilPack.Documentation;
using static NuGetUtils.Lib.Tool.DefaultDocumentation;

namespace NuGetUtils.Tool.Exec
{
   internal sealed class NuGetExecutionConfigurationImpl : NuGetExecutionConfiguration, NuGetUsageConfiguration
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
         Required( Conditional = true ),
         Description( ValueName = "type", Description = "The full name of the type which contains entry point method. Is optional if assembly is built as EXE, or if assembly contains " + nameof( ConfiguredEntryPointAttribute ) + " attribute. Otherwise it is required." )
         ]
      public String EntrypointTypeName { get; set; }


      [
         Required( Conditional = true ),
         Description( ValueName = "method", Description = "The name of the method which is an entry point method. Is optional if assembly is built as EXE, or if assembly contains " + nameof( ConfiguredEntryPointAttribute ) + " attribute. Otherwise it is required." )
         ]
      public String EntrypointMethodName { get; set; }

      [IgnoreInDocumentation]
      public String[] ProcessArguments { get; set; }

      [
         Description( Description = "Whether to explicitly restore SDK package. Use only if getting assembly load errors." )
         ]
      public Boolean RestoreSDKPackage { get; set; }

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
         Description( ValueName = ConfigurationFileLocationValue, Description = ConfigurationFileLocationDescription + " In addition, the \"" + nameof( NuGetExecutionConfigurationImpl.ProcessArguments ) + "\" key (with JSON array as value) may be specified for always-present process arguments." )
         ]
      public String ConfigurationFileLocation { get; set; }
   }
}
