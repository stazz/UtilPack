using NuGet.Common;
using NuGet.ProjectModel;
using NuGet.Utils.Exec.Entrypoint;
using System;
using System.Collections.Generic;
using System.Text;
using UtilPack.Documentation;
using UtilPack.NuGet;

namespace NuGet.Utils.Exec
{
   internal sealed class NuGetExecutionConfiguration
   {
      [
         Description( ValueName = "path", Description = "The path to NuGet configuration file containing the settings to use. The default NuGet setting file behaviour is the default if this is not specified." )
         ]
      public String NuGetConfigurationFile { get; set; }


      [IgnoreInDocumentation]
      public String[] ProcessArguments { get; set; }


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


      [
         Description( ValueName = "framework", Description = "The name of the current framework of the process (e.g. \".NETCoreApp,v=2.1\"). If automatic detection of the process framework does not work, then use this parameter to override." )
         ]
      public String RestoreFramework { get; set; }


      [
         Description( ValueName = "path", Description = "The path of the directory acting as cache directory for restored package information (serialized " + nameof( LockFile ) + "s). By default, the environment variable \"" + Programm.LOCK_FILE_CACHE_DIR_ENV_NAME + "\" is first checked, and if that is not present, the \"" + Programm.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR + "\" directory within current home directory is used." )
         ]
      public String LockFileCacheDirectory { get; set; }


      [
         Description( ValueName = "packageID", Description = "The package ID of the current framework SDK package. This is \"" + UtilPackNuGetUtility.SDK_PACKAGE_NETCORE + "\" by default." )
         ]
      public String SDKFrameworkPackageID { get; set; }


      [
         Description( ValueName = "version", Description = "The package version of the framework SDK package. By default, it is deduced from path of the assembly containing " + nameof( Object ) + " type." )]
      public String SDKFrameworkPackageVersion { get; set; }


      [
         Description( Description = "Whether to disable using cache directory for restored package information (serialized " + nameof( LockFile ) + "s)." )
         ]
      public Boolean DisableLockFileCache { get; set; }


      [
         Description( ValueName = "level", Description = "Which log level to use for NuGet logger. By default, this is " + nameof( LogLevel.Information ) + "." )
         ]
      public LogLevel LogLevel { get; set; } = LogLevel.Information;


      [
         Description( Description = "Whether to disable logging completely." )
         ]
      public Boolean DisableLogging { get; set; }
   }

   internal class ConfigurationConfiguration
   {
      [
         Required,
         Description( ValueName = "path", Description = "The path to the JSON file containing the configuration with all the same information as normal command line parameters. In addition, the \"" + nameof( NuGetExecutionConfiguration.ProcessArguments ) + "\" key (with JSON array as value) may be specified for always-present process arguments." )
         ]
      public String ConfigurationFileLocation { get; set; }
   }
}
