/*
 * Copyright 2017 Stanislav Muhametsin. All rights Reserved.
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
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace UtilPack.NuGet.ProcessRunner
{
   class Program
   {
      static Int32 Main( String[] args )
      {
         var retVal = -1;

         InitializationConfiguration initConfig = null;
         MonitoringConfiguration monitorConfig = null;
         try
         {
            var config = new ConfigurationBuilder()
               .AddCommandLine( args )
               .Build();
            initConfig = config.Get<InitializationConfiguration>();
            monitorConfig = config.Get<MonitoringConfiguration>();
         }
         catch ( Exception exc )
         {
            Console.Error.WriteLine( $"Error with reading configuration, please check your command line parameters! ({exc.Message})" );
         }

         if ( initConfig != null && monitorConfig != null )
         {
            try
            {
               var source = new CancellationTokenSource();
               Console.CancelKeyPress += ( s, e ) =>
               {
                  e.Cancel = true;
                  source.Cancel();
               };

               // Initialization step - restore needed packages, copy required files, etc
               (var assemblyPath, var framework) = new Initialization( initConfig )
                  .PerformInitializationAsync( source.Token )
                  .GetAwaiter().GetResult();

               Console.Write( $"\n\nInitialization is complete, starting process located in {assemblyPath}.\n\n" );

               // Monitor step - start process, and keep running until it exits or this process exits.
               // Restart the target process if it requests it.
               const String PROCESS_ARG_PREFIX = "/ProcessArgument:";
               // We don't know how target process parses arguments, and we don't want to make assumptions
               // The Microsoft.Extensions.Configuration will only see one argument with the example arguments:
               // /ProcessArgument:MyArg=34 /ProcessArgument:MyArg:Test=35
               // So we need to parse these ourselves
               using ( var monitoring = new Monitoring(
                  monitorConfig,
                  args.Where( arg => arg.StartsWith( PROCESS_ARG_PREFIX ) )
                  .Select( arg => arg.Substring( PROCESS_ARG_PREFIX.Length ) )
                  ) )
               {
                  try
                  {
                     monitoring.KeepMonitoringAsync( assemblyPath, framework, source.Token ).GetAwaiter().GetResult();
                  }
                  finally
                  {
                     // Make sure to stop the target process if this process is e.g. killed
                     if ( !source.IsCancellationRequested )
                     {
                        source.Cancel();
                     }
                  }
               }

               retVal = 0;
            }
            catch ( Exception exc )
            {
               Console.Error.WriteLine( $"An error occurred: {exc.Message}." );
               retVal = -2;
            }
         }

         return retVal;
      }

   }


   public class InitializationConfiguration
   {
      public String ProcessPackageID { get; set; }
      public String ProcessPackageVersion { get; set; }
      public String ProcessAssemblyPath { get; set; }

      public String NuGetConfigurationFile { get; set; }

      public String ProcessFrameworkPackageID { get; set; }
      public String ProcessFrameworkPackageVersion { get; set; }
      public DeploymentKind DeploymentKind { get; set; }
   }

   public class MonitoringConfiguration
   {
      public String ToolPath { get; set; }
      public String ProcessArgumentPrefix { get; set; } = "/";

      public String ShutdownSemaphoreProcessArgument { get; set; }
      public TimeSpan ShutdownSemaphoreWaitTime { get; set; } = TimeSpan.FromSeconds( 1 );
      public String RestartSemaphoreProcessArgument { get; set; }
   }

   public enum DeploymentKind
   {
      GenerateConfigFiles,
      CopyNonSDKAssemblies
   }
}
