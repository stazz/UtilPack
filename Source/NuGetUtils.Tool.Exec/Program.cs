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
using NuGetUtils.Lib.AssemblyResolving;
using NuGetUtils.Lib.EntryPoint;
using NuGetUtils.Lib.Restore;
using NuGetUtils.Lib.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.Documentation;


namespace NuGetUtils.Tool.Exec
{

   static class Program
   {
      public static Task<Int32> Main( String[] args )
         => new NuGetExecutingProgram().MainAsync( args, NuGetExecutingProgram.EXEC_ARGS_SEPARATOR );

   }

   internal sealed class NuGetExecutingProgram : NuGetRestoringProgram<NuGetExecutionConfigurationImpl, ConfigurationConfigurationImpl>
   {
      internal const String EXEC_ARGS_SEPARATOR = "--";

      public NuGetExecutingProgram()
         : base( new DefaultCommandLineDocumentationInfo()
         {
            ExecutableName = "nuget-exec",
            CommandLineGroupInfo = new DefaultDocumentationGroupInfo()
            {
               GroupName = "executable-options",
               AdditionalGroups = new[] {new GroupContainer(true, new ParameterGroupOrFixedParameter[] {
                     new FixedParameter(false, EXEC_ARGS_SEPARATOR),
                     new NamedParameterGroup(true, "executable-arguments", description: "The arguments for the entrypoint within NuGet-packaged assembly.")
                  } )},
               Purpose = "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by command-line parameters."
            },
            ConfigurationFileGroupInfo = new DefaultDocumentationGroupInfo()
            {
               AdditionalGroups = new[] { new NamedParameterGroup( true, "additional-executable-arguments", description: "The additional arguments for the entrypoint within NuGet-packaged assembly." ) },
               Purpose = "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by configuration file."
            }
         } )
      {

      }

      protected override Boolean ValidateConfiguration(
         ConfigurationInformation info
         )
      {
         return !String.IsNullOrEmpty( info.Configuration.PackageID );
      }

      protected override Task<Int32> UseRestorerAsync(
         ConfigurationInformation info,
         CancellationToken token,
         BoundRestoreCommandUser restorer,
         String sdkPackageID,
         String sdkPackageVersion
         )
      {
         var config = info.Configuration;
         var programArgs = new Lazy<String[]>( () => info.IsConfigurationConfiguration ? ( config.ProcessArguments ?? Empty<String>.Array ).Concat( info.RemainingArguments ).ToArray() : info.RemainingArguments.ToArray() );
         var programArgsConfig = new Lazy<IConfigurationRoot>( () => new ConfigurationBuilder().AddCommandLine( programArgs.Value ).Build() );

         return config.ExecuteNuGetAssemblyEntryPointAsync(
            token,
            restorer,
            type =>
            {
               return Equals( type, typeof( String[] ) ) ?
                  programArgs.Value :
                  programArgsConfig.Value.Get( type );
            },
            sdkPackageID,
            sdkPackageVersion
            );
      }
   }
}
