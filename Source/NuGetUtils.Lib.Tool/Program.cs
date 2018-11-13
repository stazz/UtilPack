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
using NuGetUtils.Lib.Common;
using NuGetUtils.Lib.Restore;
using NuGetUtils.Lib.Tool;
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
   // TODO additional type parameter: TConfigurationValidation, and additional abstract method IsValid(TConfigurationValidation). Then the GetDocumentation method could get TConfigurationValidation as parameter in order to be able to provide contextual documentation.

   /// <summary>
   /// This is base class for command line programs, which accept their configuration directly as command line arguments, or via JSON file.
   /// </summary>
   /// <typeparam name="TCommandLineConfiguration">The actual type that is the configuration for the program.</typeparam>
   /// <typeparam name="TConfigurationConfiguration">The actual type which specifies the JSON configuration file location.</typeparam>
   /// <remarks>
   /// <para>The configuration types are type parameters so that the actual types could have their own dedicated <see cref="RequiredAttribute"/> and <see cref="DescriptionAttribute"/> attributes for the documentation.</para>
   /// <para>The command line arguments (or the JSON file contents) are bound to configuration type by first loading them using <see cref="CommandLineConfigurationExtensions.AddCommandLine(IConfigurationBuilder, global::System.String[])"/> or <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, String)"/> methods, and then extracting the type using <see cref="ConfigurationBinder.Get{T}(IConfiguration)"/> method.
   /// This typically results in direct argument name -> property name binding.</para>
   /// <para>For example, specifying "--MyArgument=MyValue" would require the <typeparamref name="TCommandLineConfiguration"/> to have a (string) property named "MyArgument" which would also need to be settable.
   /// Then, after the <see cref="ConfigurationBinder.Get{T}(IConfiguration)"/> method invocation, the "MyArgument" property would have value "MyValue".</para>
   /// </remarks>
   public abstract class Program<TCommandLineConfiguration, TConfigurationConfiguration>
      where TCommandLineConfiguration : class
      where TConfigurationConfiguration : class, ConfigurationConfiguration
   {
      /// <summary>
      /// This error code signifies that the given command line arguments were invalid (e.g. wrong syntax), or produced invalid configuration.
      /// </summary>
      public const Int32 ERROR_INVALID_CONFIG = -1;

      /// <summary>
      /// This error code signifies that the <see cref="PerformWithConfigAsync"/> method threw an exception.
      /// </summary>
      public const Int32 ERROR_PROGRAM_THREW_EXCEPTION = -2;

      /// <summary>
      /// This method will parse the textual command line arguments into <typeparamref name="TCommandLineConfiguration"/>, check its validity using <see cref="ValidateConfiguration"/> method, and then invokes <see cref="PerformWithConfigAsync"/> method to actually perform the program.
      /// </summary>
      /// <param name="args">The command line arguments.</param>
      /// <param name="argsEndMark">If this value is specified, then only those command line arguments present in <paramref name="args"/> *before* this value will be used to create instance of <typeparamref name="TCommandLineConfiguration"/>.</param>
      /// <returns>Asynchronously returns integer return value. Will be either <see cref="ERROR_INVALID_CONFIG"/> or <see cref="ERROR_PROGRAM_THREW_EXCEPTION"/> or whatever the <see cref="PerformWithConfigAsync"/> returns.</returns>
      /// <remarks>If <see cref="ERROR_INVALID_CONFIG"/> is being returned, then help string returned by <see cref="GetDocumentation"/> method is printed.</remarks>
      public async Task<Int32> MainAsync(
         String[] args,
         String argsEndMark = null
         )

      {
         TCommandLineConfiguration programConfig = null;
         var isConfigConfig = false;
         var hasHelp = false;
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
                  hasHelp = HasHelp( args, null );
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
               else if ( !( hasHelp = HasHelp( args, programArgStart ) ) )
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
                        retVal = ERROR_PROGRAM_THREW_EXCEPTION;
                     }
                  }
               }
            }
         }

         if ( !retVal.HasValue )
         {
            Console.Out.WriteLine( this.GetDocumentation() );
            retVal = hasHelp ? 0 : ERROR_INVALID_CONFIG;
         }


         return retVal.Value;
      }

      private static Boolean HasHelp( String[] args, Int32? count )
      {
         return Array.FindIndex(
            args,
            0,
            count ?? args.Length,
            arg => String.Equals( "--help", arg, StringComparison.OrdinalIgnoreCase ) || String.Equals( "-help", arg, StringComparison.OrdinalIgnoreCase )
            ) >= 0;
      }

      /// <summary>
      /// Subclasses should implement this method to perform validation on configuration.
      /// </summary>
      /// <param name="info">The <see cref="ConfigurationInformation"/> about the configuration.</param>
      /// <returns>Should return <c>true</c> if configuration is deemed to be valid; <c>false</c> otherwise.</returns>
      protected abstract Boolean ValidateConfiguration( ConfigurationInformation info );

      /// <summary>
      /// Subclasses should implement this method to perform their task. This method is only invoked if <see cref="ValidateConfiguration"/> returns <c>true</c>.
      /// </summary>
      /// <param name="info">The <see cref="ConfigurationInformation"/> about the configuration.</param>
      /// <param name="token">The cancellation token which gets canceled on <see cref="Console.CancelKeyPress"/> event.</param>
      /// <returns>An integer return value which will be further returned by <see cref="MainAsync"/>.</returns>
      protected abstract Task<Int32> PerformWithConfigAsync( ConfigurationInformation info, CancellationToken token );

      /// <summary>
      /// Subclasses should implement this method to create a documentation string.
      /// </summary>
      /// <returns></returns>
      protected abstract String GetDocumentation();

      /// <summary>
      /// This structure captures information about configuration created by <see cref="MainAsync"/> method.
      /// </summary>
      protected struct ConfigurationInformation
      {

         /// <summary>
         /// Creates a new instance of <see cref="ConfigurationInformation"/>.
         /// </summary>
         /// <param name="configuration">The configuration.</param>
         /// <param name="isConfigurationConfiguration"><c>true</c> if configuration was read from JSON file; <c>false</c> if configuration was read from command line arguments directly.</param>
         /// <param name="remainingArguments">Any remaining arguments in case argsEndMark parameter was specified for <see cref="MainAsync"/>.</param>
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

         /// <summary>
         /// Gets the actual configuration instance.
         /// </summary>
         /// <value>The actual configuration instance.</value>
         public TCommandLineConfiguration Configuration { get; }

         /// <summary>
         /// <c>true</c> if configuration was read from JSON file; <c>false</c> if configuration was read from command line arguments directly.
         /// </summary>
         /// <value><c>true</c> if configuration was read from JSON file; <c>false</c> if configuration was read from command line arguments directly.</value>
         public Boolean IsConfigurationConfiguration { get; }

         /// <summary>
         /// Gets any remaining arguments in case argsEndMark parameter was specified for <see cref="MainAsync"/>.
         /// </summary>
         /// <value>Any remaining arguments in case argsEndMark parameter was specified for <see cref="MainAsync"/>.</value>
         public ImmutableArray<String> RemainingArguments { get; }
      }

   }

   /// <summary>
   /// This is type parameterless class to hold some string constants used by <see cref="NuGetRestoringProgram{TCommandLineConfiguration, TConfigurationConfiguration}"/>.
   /// </summary>
   public static class NuGetRestoringProgramConsts
   {
      /// <summary>
      /// This is the default environment variable name that is used when trying to deduce lock file cache directory.
      /// </summary>
      public const String LOCK_FILE_CACHE_DIR_ENV_NAME = "NUGET_UTILS_CACHE_DIR";

      /// <summary>
      /// This is the default directory name within home directory which will hold the lock file cache directory.
      /// </summary>
      public const String LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR = ".nuget-utils-cache";
   }

   /// <summary>
   /// This class further specializes the <see cref="Program{TCommandLineConfiguration, TConfigurationConfiguration}"/> for programs that use <see cref="BoundRestoreCommandUser"/> in their task.
   /// </summary>
   /// <typeparam name="TCommandLineConfiguration">The actual type that is the configuration for the program.</typeparam>
   /// <typeparam name="TConfigurationConfiguration">The actual type which specifies the JSON configuration file location.</typeparam>
   /// <remarks>
   /// <para>The configuration types are type parameters so that the actual types could have their own dedicated <see cref="RequiredAttribute"/> and <see cref="DescriptionAttribute"/> attributes for the documentation.</para>
   /// <para>The command line arguments (or the JSON file contents) are bound to configuration type by first loading them using <see cref="CommandLineConfigurationExtensions.AddCommandLine(IConfigurationBuilder, global::System.String[])"/> or <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, String)"/> methods, and then extracting the type using <see cref="ConfigurationBinder.Get{T}(IConfiguration)"/> method.
   /// This typically results in direct argument name -> property name binding.</para>
   /// <para>For example, specifying "--MyArgument=MyValue" would require the <typeparamref name="TCommandLineConfiguration"/> to have a (string) property named "MyArgument" which would also need to be settable.
   /// Then, after the <see cref="ConfigurationBinder.Get{T}(IConfiguration)"/> method invocation, the "MyArgument" property would have value "MyValue".</para>
   /// </remarks>
   public abstract class NuGetRestoringProgram<TCommandLineConfiguration, TConfigurationConfiguration> : Program<TCommandLineConfiguration, TConfigurationConfiguration>
      where TCommandLineConfiguration : class, NuGetUsageConfiguration
      where TConfigurationConfiguration : class, ConfigurationConfiguration
   {

      private readonly Lazy<String> _documentation;

      /// <summary>
      /// Creates a new instance of <see cref="NuGetRestoringProgram{TCommandLineConfiguration, TConfigurationConfiguration}"/> with given documentation information and lock file cache information.
      /// </summary>
      /// <param name="documentationInfo">The <see cref="CommandLineDocumentationInfo"/> which will be used when creating help string via <see cref="GetDocumentation"/>.</param>
      /// <param name="lockFileCacheDirEnvName">The environment variable name that is used when trying to deduce lock file cache directory. By default is <see cref="NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_ENV_NAME"/>.</param>
      /// <param name="lockFileCacheDirWithinHomeDir">The default directory name within home directory which will hold the lock file cache directory. By default is <see cref="NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR"/>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="documentationInfo"/> is <c>null</c>.</exception>
      public NuGetRestoringProgram(
         CommandLineDocumentationInfo documentationInfo,
         String lockFileCacheDirEnvName = NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_ENV_NAME,
         String lockFileCacheDirWithinHomeDir = NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR
         )
      {
         this.LockFileCacheDirEnvName = String.IsNullOrEmpty( lockFileCacheDirEnvName ) ? NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_ENV_NAME : lockFileCacheDirEnvName;
         this.LockFileCacheDirWithinHomeDir = String.IsNullOrEmpty( lockFileCacheDirWithinHomeDir ) ? NuGetRestoringProgramConsts.LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR : lockFileCacheDirWithinHomeDir;
         this.DocumentationInfo = ArgumentValidator.ValidateNotNull( nameof( documentationInfo ), documentationInfo );
         this._documentation = new Lazy<String>( () => this.DocumentationInfo.CreateCommandLineDocumentation( this.GetType(), typeof( TCommandLineConfiguration ), typeof( TConfigurationConfiguration ) ), LazyThreadSafetyMode.ExecutionAndPublication );
      }

      /// <summary>
      /// Gets the environment variable name which, if specified, should hold the lock file cache directory.
      /// </summary>
      /// <value>The environment variable name which, if specified, should hold the lock file cache directory.</value>
      protected String LockFileCacheDirEnvName { get; }

      /// <summary>
      /// Gets the directory name, within the home directory, which is default for lock file cache. 
      /// </summary>
      /// <value>The directory name, within the home directory, which is default for lock file cache. </value>
      protected String LockFileCacheDirWithinHomeDir { get; }

      /// <summary>
      /// Gets the <see cref="CommandLineDocumentationInfo"/> use to build help string in <see cref="GetDocumentation"/>.
      /// </summary>
      /// <value>The <see cref="CommandLineDocumentationInfo"/> use to build help string in <see cref="GetDocumentation"/>.</value>
      protected CommandLineDocumentationInfo DocumentationInfo { get; }

      /// <summary>
      /// Implements <see cref="Program{TCommandLineConfiguration, TConfigurationConfiguration}.PerformWithConfigAsync"/> by creating a new <see cref="BoundRestoreCommandUser"/>, auto-deducing current NuGet framework and SDK package ID and version, and then calling <see cref="UseRestorerAsync"/>.
      /// </summary>
      /// <param name="info">The <see cref="Program{TCommandLineConfiguration, TConfigurationConfiguration}.ConfigurationInformation"/> about configuration.</param>
      /// <param name="token">The <see cref="CancellationToken"/> which gets canceled when the <see cref="Console.CancelKeyPress"/> event occurs.</param>
      /// <returns>Asynchornously returns return value of <see cref="UseRestorerAsync"/>.</returns>
      /// <remarks>The <see cref="BoundRestoreCommandUser"/> is disposed after the <see cref="UseRestorerAsync"/> asynchronously completes.</remarks>
      protected override Task<Int32> PerformWithConfigAsync(
         ConfigurationInformation info,
         CancellationToken token
         )
      {
         return info.Configuration.CreateAndUseRestorerAsync(
            this.GetType(),
            this.LockFileCacheDirEnvName,
            this.LockFileCacheDirWithinHomeDir,
            () => new TextWriterLogger()
            {
               VerbosityLevel = info.Configuration.LogLevel
            },
            restorer => this.UseRestorerAsync( info, token, restorer.Restorer, restorer.SDKPackageID, restorer.SDKPackageVersion )
            );
      }

      /// <summary>
      /// Subclasses should override this method in order to implement their own functionality requiring <see cref="BoundRestoreCommandUser"/>.
      /// </summary>
      /// <param name="info">The <see cref="Program{TCommandLineConfiguration, TConfigurationConfiguration}.ConfigurationInformation"/> about configuration.</param>
      /// <param name="token">The <see cref="CancellationToken"/> which gets canceled when the <see cref="Console.CancelKeyPress"/> event occurs.</param>
      /// <param name="restorer">The <see cref="BoundRestoreCommandUser"/> created by the <see cref="PerformWithConfigAsync"/>.</param>
      /// <param name="sdkPackageID">The SDK package ID auto-deduced by the <see cref="PerformWithConfigAsync"/>. Typically <c>Microsoft.NETCore.App</c>.</param>
      /// <param name="sdkPackageVersion">The SDK packageversion auto-deduced by the <see cref="PerformWithConfigAsync"/>.</param>
      /// <returns>The return value for <see cref="Program{TCommandLineConfiguration, TConfigurationConfiguration}.MainAsync"/>.</returns>
      protected abstract Task<Int32> UseRestorerAsync(
         ConfigurationInformation info,
         CancellationToken token,
         BoundRestoreCommandUser restorer,
         String sdkPackageID,
         String sdkPackageVersion
         );

      /// <summary>
      /// Implements the <see cref="Program{TCommandLineConfiguration, TConfigurationConfiguration}.GetDocumentation"/> and returns string created by <see cref="E_NuGetUtils.CreateCommandLineDocumentation"/>.
      /// </summary>
      /// <returns>Help string created by <see cref="E_NuGetUtils.CreateCommandLineDocumentation"/>.</returns>
      protected sealed override String GetDocumentation()
      {
         return this._documentation.Value;
      }
   }

   /// <summary>
   /// This data interface contains information about generating command line help string.
   /// </summary>
   public interface CommandLineDocumentationInfo
   {
      /// <summary>
      /// Gets the name of the executable, as seen to end user.
      /// </summary>
      /// <value>The name of the executable, as seen to end user.</value>
      String ExecutableName { get; }

      /// <summary>
      /// Gets the <see cref="DocumentationGroupInfo"/> about the direct command line arguments.
      /// </summary>
      /// <value>The <see cref="DocumentationGroupInfo"/> about the direct command line arguments.</value>
      DocumentationGroupInfo CommandLineGroupInfo { get; }

      /// <summary>
      /// Gets the <see cref="DocumentationGroupInfo"/> about the command line arguments used to specify file containing actual configuration.
      /// </summary>
      /// <value>The <see cref="DocumentationGroupInfo"/> about the command line arguments used to specify file containing actual configuration.</value>
      DocumentationGroupInfo ConfigurationFileGroupInfo { get; }
   }

   /// <summary>
   /// This data interface contains information about single command line argument group.
   /// </summary>
   public interface DocumentationGroupInfo
   {
      /// <summary>
      /// Gets the main group name.
      /// </summary>
      /// <value>The main group name.</value>
      String GroupName { get; }

      /// <summary>
      /// Gets any additional <see cref="ParameterGroupOrFixedParameter"/> instances that are part of this main group.
      /// </summary>
      IEnumerable<ParameterGroupOrFixedParameter> AdditionalGroups { get; }

      /// <summary>
      /// Gets the purpose of this documentation group info.
      /// </summary>
      /// <value>The purpose of this documentation group info.</value>
      String Purpose { get; }
   }

   /// <summary>
   /// This class provides ease-of-life implementation of <see cref="CommandLineDocumentationInfo"/> with settable properties for nicer coding experience.
   /// </summary>
   public class DefaultCommandLineDocumentationInfo : CommandLineDocumentationInfo
   {
      /// <inheritdoc />
      public String ExecutableName { get; set; }

      /// <inheritdoc />
      public DocumentationGroupInfo CommandLineGroupInfo { get; set; }

      /// <inheritdoc />
      public DocumentationGroupInfo ConfigurationFileGroupInfo { get; set; }
   }

   /// <summary>
   /// This class provides ease-of-life implementation of <see cref="DocumentationGroupInfo"/> with settable properties for nicer coding experience.
   /// </summary>
   public class DefaultDocumentationGroupInfo : DocumentationGroupInfo
   {
      /// <inheritdoc />
      public String GroupName { get; set; }

      /// <inheritdoc />
      public IEnumerable<ParameterGroupOrFixedParameter> AdditionalGroups { get; set; }

      /// <inheritdoc />
      public String Purpose { get; set; }
   }
}

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_NuGetUtils
{
   /// <summary>
   /// Creates a string which contains help text about how the command line utility is used, given the <see cref="CommandLineDocumentationInfo"/> and information about types which it was created from.
   /// </summary>
   /// <param name="docInfo">This <see cref="CommandLineDocumentationInfo"/>.</param>
   /// <param name="versionSource">The type, the assembly version of which will be used as version information.</param>
   /// <param name="commandLineConfigType">The type of the direct command line configuration.</param>
   /// <param name="configConfigType">The type of the indirect, file-based command line configuration.</param>
   /// <returns>A string which contains help text about how the command line utility is used.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="CommandLineDocumentationInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If any of the <paramref name="versionSource"/>, <paramref name="commandLineConfigType"/>, or <paramref name="configConfigType"/> is <c>null</c>; or if some property of <see cref="CommandLineDocumentationInfo"/> or <see cref="DocumentationGroupInfo"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentException">If some of the <see cref="CommandLineDocumentationInfo"/> or <see cref="DocumentationGroupInfo"/> string properties are empty strings.</exception>
   public static String CreateCommandLineDocumentation(
      this CommandLineDocumentationInfo docInfo,
      Type versionSource,
      Type commandLineConfigType,
      Type configConfigType
      )
   {
      ArgumentValidator.ValidateNotNullReference( docInfo );
      ArgumentValidator.ValidateNotNull( nameof( versionSource ), versionSource );
      ArgumentValidator.ValidateNotNull( nameof( commandLineConfigType ), commandLineConfigType );
      ArgumentValidator.ValidateNotNull( nameof( configConfigType ), configConfigType );


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
         $"{execName} version {versionSource.Assembly.GetName().Version} (NuGet version {typeof( NuGet.Common.ILogger ).Assembly.GetName().Version})\n" +
         generator.GenerateParametersDocumentation(
            ( cmdLineInfo.AdditionalGroups ?? Empty<ParameterGroupOrFixedParameter>.Enumerable ).Prepend( new NamedParameterGroup( false, cmdLineName ) ),
            commandLineConfigType,
            execName,
            ArgumentValidator.ValidateNotEmpty( nameof( cmdLineInfo.Purpose ), cmdLineInfo.Purpose ),
            cmdLineName
            )
            + "\n\n\n" +
         generator.GenerateParametersDocumentation( ( configInfo.AdditionalGroups ?? Empty<ParameterGroupOrFixedParameter>.Enumerable ).Prepend( new NamedParameterGroup( false, configName ) ),
            configConfigType,
            execName,
            ArgumentValidator.ValidateNotEmpty( nameof( configInfo.Purpose ), configInfo.Purpose ),
            configName
         );
   }
}