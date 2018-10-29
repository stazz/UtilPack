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
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.Documentation;


namespace NuGetUtils.Exec
{
   static class Program
   {

      private const String EXEC_ARGS_SEPARATOR = "--";

      async static Task<Int32> Main( String[] args )
      {
         var retVal = -1;

         NuGetExecutionConfiguration programConfig = null;
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
               configRoot = new ConfigurationBuilder()
                  .AddJsonFile( Path.GetFullPath(
                     new ConfigurationBuilder()
                        .AddCommandLine( args.Take( programArgStart ).ToArray() )
                        .Build()
                        .Get<ConfigurationConfiguration>()
                        .ConfigurationFileLocation
                        )
                     );
            }
            else
            {
               var idx = Array.FindIndex( args, arg => String.Equals( arg, EXEC_ARGS_SEPARATOR ) );
               programArgStart = idx < 0 ? args.Length : idx;
               configRoot = new ConfigurationBuilder()
                  .AddCommandLine( args.Take( programArgStart ).ToArray() );
               if ( idx >= 0 )
               {
                  ++programArgStart;
               }
            }

            if ( programArgStart > 0 )
            {
               args = args.Skip( programArgStart ).ToArray();

               programConfig = configRoot
                  .Build()
                  .Get<NuGetExecutionConfiguration>();
            }
         }
         catch ( Exception exc )
         {
            Console.Error.WriteLine( $"Error with reading configuration, please check your command line parameters! ({exc.Message})" );
         }

         if ( !( programConfig?.PackageID?.IsNullOrEmpty() ?? true ) )
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
                     await new NuGetEntryPointExecutor( args, programConfig, isConfigConfig ).ExecuteMethod( source.Token );
                  }
                  catch ( Exception exc )
                  {
                     Console.Error.WriteLine( $"An error occurred: {exc.Message}." );
                     retVal = -2;
                  }
               }
            }
         }
         else
         {
            Console.Out.WriteLine( GetDocumentation() );
         }


         return retVal;
      }

      private static String GetDocumentation()
      {
         var generator = new CommandLineArgumentsDocumentationGenerator();
         return
            $"nuget-exec version {typeof( Program ).Assembly.GetName().Version} (NuGet version {typeof( NuGet.Common.ILogger ).Assembly.GetName().Version})\n" +
            generator.GenerateParametersDocumentation( new ParameterGroupOrFixedParameter[]
               {
                  new NamedParameterGroup(false, "executable-options"),
                  new GroupContainer(true, new ParameterGroupOrFixedParameter[] {
                     new FixedParameter(false, EXEC_ARGS_SEPARATOR),
                     new NamedParameterGroup(true, "executable-arguments", description: "The arguments for the entrypoint within NuGet-packaged assembly.")
                  } )
               },
               typeof( NuGetExecutionConfiguration ),
               "nuget-exec",
               "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by command-line parameters.",
               "executable-options"
               )
               + "\n\n\n" +
            generator.GenerateParametersDocumentation( new ParameterGroupOrFixedParameter[]
               {
                  new NamedParameterGroup(false, "configuration-options"),
                  new NamedParameterGroup(true, "additional-executable-arguments", description: "The additional arguments for the entrypoint within NuGet-packaged assembly.")
               },
               typeof( ConfigurationConfiguration ),
               "nuget-exec",
               "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by configuration file.",
               "configuration-options"
            );
      }


   }
}
