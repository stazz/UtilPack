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
using Microsoft.Extensions.Configuration;
using NuGet.Frameworks;
using NuGetUtils.Lib.Restore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.Documentation;

namespace NuGetUtils.Lib.Tool
{
   public abstract class Program<TCommandLineConfiguration, TConfigurationConfiguration>
      where TCommandLineConfiguration : class
      where TConfigurationConfiguration : class, ConfigurationConfiguration
   {
      public async Task<Int32> MainAsync(
         String[] args,
         String argsEndMark = null
         )

      {
         TCommandLineConfiguration programConfig = null;
         var isConfigConfig = false;
         try
         {
            Int32 programArgStart;
            IConfigurationBuilder configRoot;
            var first = args.GetOrDefault( 0 );

            isConfigConfig = !first.IsNullOrEmpty() && (
               first.StartsWith( $"/{nameof( ConfigurationConfiguration.ConfigurationFileLocation )}", StringComparison.OrdinalIgnoreCase )
               || first.StartsWith( $"-{nameof( ConfigurationConfiguration.ConfigurationFileLocation )}", StringComparison.OrdinalIgnoreCase )
               || first.StartsWith( $"--{nameof( ConfigurationConfiguration.ConfigurationFileLocation )}", StringComparison.OrdinalIgnoreCase )
               );
            if ( isConfigConfig )
            {
               programArgStart = first.Contains( '=' ) ? 1 : 2;
               if ( String.IsNullOrEmpty( argsEndMark ) && programArgStart < args.Length )
               {
                  // Extra arguments
                  configRoot = null;
               }
               else
               {
                  configRoot = new ConfigurationBuilder()
                     .AddJsonFile( Path.GetFullPath(
                        new ConfigurationBuilder()
                           .AddCommandLine( args.Take( programArgStart ).ToArray() )
                           .Build()
                           .Get<TConfigurationConfiguration>()
                           .ConfigurationFileLocation
                           )
                        );
               }
            }
            else
            {
               String[] configArgs;
               if ( String.IsNullOrEmpty( argsEndMark ) )
               {
                  programArgStart = args.Length;
                  configArgs = args;
               }
               else
               {
                  var idx = Array.FindIndex( args, arg => String.Equals( arg, argsEndMark ) );
                  if ( idx < 0 )
                  {
                     programArgStart = args.Length;
                     configArgs = args;
                  }
                  else
                  {
                     programArgStart = idx + 1;
                     configArgs = args.Take( idx ).ToArray();
                  }
               }

               configRoot = new ConfigurationBuilder()
                  .AddCommandLine( configArgs );
            }

            if ( configRoot != null )
            {
               if ( args.Length == 0 )
               {
                  // The Get<x> method returns always null in this case
                  programConfig = Activator.CreateInstance<TCommandLineConfiguration>(); // Using Activator is not always ok, but we are only using it once here so should be fine.
               }
               else
               {
                  args = args.Skip( programArgStart ).ToArray();

                  programConfig = configRoot
                     .Build()
                     .Get<TCommandLineConfiguration>();
               }
            }
         }
         catch ( Exception exc )
         {
            Console.Error.WriteLine( $"Error with reading configuration, please check your command line parameters! ({exc.Message})" );
         }

         Int32? retVal = null;
         if ( programConfig != null )
         {
            var info = new ConfigurationInformation( programConfig, isConfigConfig, args.ToImmutableArray() );
            if ( this.ValidateConfiguration( info ) )
            {
               using ( var source = new CancellationTokenSource() )
               {
                  void OnCancelKey( Object sender, ConsoleCancelEventArgs e )
                  {
                     e.Cancel = true;
                     source.Cancel();
                  }
                  Console.CancelKeyPress += OnCancelKey;
                  using ( new UsingHelper( () => Console.CancelKeyPress -= OnCancelKey ) )
                  {
                     try
                     {
                        retVal = await this.PerformWithConfigAsync( info, source.Token );
                     }
                     catch ( Exception exc )
                     {
                        Console.Error.WriteLine( $"An error occurred: {exc.Message}." );
                        retVal = -2;
                     }
                  }
               }
            }
         }

         if ( !retVal.HasValue )
         {
            Console.Out.WriteLine( this.GetDocumentation() );
            retVal = -1;
         }


         return retVal.Value;
      }

      protected abstract String GetDocumentation();
      protected abstract Boolean ValidateConfiguration( ConfigurationInformation info );
      protected abstract Task<Int32> PerformWithConfigAsync( ConfigurationInformation info, CancellationToken token );

      protected struct ConfigurationInformation
      {

         public ConfigurationInformation(
            TCommandLineConfiguration configuration,
            Boolean isConfigurationConfiguration,
            ImmutableArray<String> remainingArguments
            )
         {
            this.Configuration = ArgumentValidator.ValidateNotNull( nameof( configuration ), configuration );
            this.IsConfigurationConfiguration = isConfigurationConfiguration;
            this.RemainingArguments = remainingArguments;
         }

         public TCommandLineConfiguration Configuration { get; }
         public Boolean IsConfigurationConfiguration { get; }
         public ImmutableArray<String> RemainingArguments { get; }
      }

   }

   public static class Consts
   {

      public const String LOCK_FILE_CACHE_DIR_ENV_NAME = "NUGET_UTILS_CACHE_DIR";
      public const String LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR = ".nuget-utils-cache";
   }

   public abstract class NuGetRestoringProgram<TCommandLineConfiguration, TConfigurationConfiguration> : Program<TCommandLineConfiguration, TConfigurationConfiguration>
      where TCommandLineConfiguration : class, NuGetUsageConfiguration
      where TConfigurationConfiguration : class, ConfigurationConfiguration
   {


      public NuGetRestoringProgram(
         CommandLineDocumentationInfo documentationInfo,
         String lockFileCacheDirEnvName = Consts.LOCK_FILE_CACHE_DIR_ENV_NAME,
         String lockFileCacheDirWithinHomeDir = Consts.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR
         )
      {
         this.LockFileCacheDirEnvName = String.IsNullOrEmpty( lockFileCacheDirEnvName ) ? Consts.LOCK_FILE_CACHE_DIR_ENV_NAME : lockFileCacheDirEnvName;
         this.LockFileCacheDirWithinHomeDir = String.IsNullOrEmpty( lockFileCacheDirWithinHomeDir ) ? Consts.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR : lockFileCacheDirWithinHomeDir;
         this.DocumentationInfo = ArgumentValidator.ValidateNotNull( nameof( documentationInfo ), documentationInfo );
      }

      protected String LockFileCacheDirEnvName { get; }

      protected String LockFileCacheDirWithinHomeDir { get; }

      protected CommandLineDocumentationInfo DocumentationInfo { get; }

      protected override Task<Int32> PerformWithConfigAsync(
         ConfigurationInformation info,
         CancellationToken token
         )
      {
         var programConfig = info.Configuration;
         var targetFWString = programConfig.RestoreFramework;

         using ( var restorer = new BoundRestoreCommandUser(
            UtilPackNuGetUtility.GetNuGetSettingsWithDefaultRootDirectory(
                  Path.GetDirectoryName( new Uri( this.GetType().GetTypeInfo().Assembly.CodeBase ).LocalPath ),
                  programConfig.NuGetConfigurationFile
               ),
            thisFramework: String.IsNullOrEmpty( targetFWString ) ? null : NuGetFramework.Parse( targetFWString ),
            nugetLogger: programConfig.DisableLogging ? null : new TextWriterLogger()
            {
               VerbosityLevel = programConfig.LogLevel
            },
            lockFileCacheDir: programConfig.LockFileCacheDirectory,
            lockFileCacheEnvironmentVariableName: this.LockFileCacheDirEnvName,
            getDefaultLockFileCacheDir: homeDir => Path.Combine( homeDir, this.LockFileCacheDirWithinHomeDir ),
            disableLockFileCacheDir: programConfig.DisableLockFileCache
            ) )
         {

            var thisFramework = restorer.ThisFramework;
            var sdkPackageID = thisFramework.GetSDKPackageID( programConfig.SDKFrameworkPackageID ); // Typically "Microsoft.NETCore.App"
            var sdkPackageVersion = thisFramework.GetSDKPackageVersion( sdkPackageID, programConfig.SDKFrameworkPackageVersion );

            return this.UseRestorerAsync(
               info,
               token,
               restorer,
               thisFramework.GetSDKPackageID( programConfig.SDKFrameworkPackageID ),
               thisFramework.GetSDKPackageVersion( sdkPackageID, programConfig.SDKFrameworkPackageVersion )
               );
         }
      }

      protected abstract Task<Int32> UseRestorerAsync(
         ConfigurationInformation info,
         CancellationToken token,
         BoundRestoreCommandUser restorer,
         String sdkPackageID,
         String sdkPackageVersion
         );

      protected override String GetDocumentation()
      {
         var docInfo = this.DocumentationInfo;
         var execName = ArgumentValidator.ValidateNotEmpty( nameof( docInfo.ExecutableName ), docInfo.ExecutableName );
         var cmdLineInfo = ArgumentValidator.ValidateNotNull( nameof( docInfo.CommandLineGroupInfo ), docInfo.CommandLineGroupInfo );
         var configInfo = ArgumentValidator.ValidateNotNull( nameof( docInfo.ConfigurationFileGroupInfo ), docInfo.ConfigurationFileGroupInfo );
         var cmdLineName = cmdLineInfo.GroupName;
         if ( String.IsNullOrEmpty( cmdLineName ) )
         {
            cmdLineName = "nuget-options";
         }
         var configName = configInfo.GroupName;
         if ( String.IsNullOrEmpty( configName ) )
         {
            configName = "configuration-options";
         }

         var generator = new CommandLineArgumentsDocumentationGenerator();
         return
            $"{execName} version {this.GetType().Assembly.GetName().Version} (NuGet version {typeof( NuGet.Common.ILogger ).Assembly.GetName().Version})\n" +
            generator.GenerateParametersDocumentation(
               ( cmdLineInfo.AdditionalGroups ?? Empty<ParameterGroupOrFixedParameter>.Enumerable ).Prepend( new NamedParameterGroup( false, cmdLineName ) ),
               //new ParameterGroupOrFixedParameter[]
               //{
               //   new NamedParameterGroup(false, CMD_LINE_OPTIONS_GROUP),
               //   new GroupContainer(true, new ParameterGroupOrFixedParameter[] {
               //      new FixedParameter(false, EXEC_ARGS_SEPARATOR),
               //      new NamedParameterGroup(true, "executable-arguments", description: "The arguments for the entrypoint within NuGet-packaged assembly.")
               //   } )
               //},
               typeof( TCommandLineConfiguration ),
               execName,
               ArgumentValidator.ValidateNotEmpty( nameof( cmdLineInfo.Purpose ), cmdLineInfo.Purpose ), // "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by command-line parameters.",
               cmdLineName
               )
               + "\n\n\n" +
            generator.GenerateParametersDocumentation( ( configInfo.AdditionalGroups ?? Empty<ParameterGroupOrFixedParameter>.Enumerable ).Prepend( new NamedParameterGroup( false, configName ) ),
               //{
               //   new NamedParameterGroup(false, "configuration-options"),
               //   new NamedParameterGroup(true, "additional-executable-arguments", description: "The additional arguments for the entrypoint within NuGet-packaged assembly.")
               //},
               typeof( TConfigurationConfiguration ),
               execName,
               ArgumentValidator.ValidateNotEmpty( nameof( configInfo.Purpose ), configInfo.Purpose ), // "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by configuration file.",
               configName
            );
      }
   }

   public interface CommandLineDocumentationInfo
   {
      String ExecutableName { get; }
      DocumentationGroupInfo CommandLineGroupInfo { get; }
      DocumentationGroupInfo ConfigurationFileGroupInfo { get; }
   }

   public interface DocumentationGroupInfo
   {
      String GroupName { get; }
      IEnumerable<ParameterGroupOrFixedParameter> AdditionalGroups { get; }
      String Purpose { get; }
   }

   public class DefaultCommandLineDocumentationInfo : CommandLineDocumentationInfo
   {
      public String ExecutableName { get; set; }
      public DocumentationGroupInfo CommandLineGroupInfo { get; set; }
      public DocumentationGroupInfo ConfigurationFileGroupInfo { get; set; }
   }

   public class DefaultDocumentationGroupInfo : DocumentationGroupInfo
   {
      public String GroupName { get; set; }
      public IEnumerable<ParameterGroupOrFixedParameter> AdditionalGroups { get; set; }
      public String Purpose { get; set; }
   }
}

