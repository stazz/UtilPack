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
using System.IO;
using System.Reflection;
using NuGet.Frameworks;
using UtilPack.ProcessMonitor;
using UtilPack.NuGet.Deployment;

namespace UtilPack.NuGet.ProcessRunner
{
   class Program
   {
      static Int32 Main( String[] args )
      {
         var retVal = -1;

         DeploymentConfiguration deployConfig = null;
         DefaultMonitoringConfiguration monitorConfig = null;
         NuGetConfiguration nugetConfig = null;
         try
         {
            var config = new ConfigurationBuilder()
               .AddCommandLine( args )
               .Build();
            deployConfig = config.Get<DefaultDeploymentConfiguration>();
            monitorConfig = config.Get<DefaultMonitoringConfiguration>();
            nugetConfig = config.Get<NuGetConfiguration>();
         }
         catch ( Exception exc )
         {
            Console.Error.WriteLine( $"Error with reading configuration, please check your command line parameters! ({exc.Message})" );
         }

         if ( deployConfig != null && monitorConfig != null && nugetConfig != null )
         {
            var source = new CancellationTokenSource();
            try
            {

               Console.CancelKeyPress += ( s, e ) =>
               {
                  e.Cancel = true;
                  source.Cancel();
               };


               var targetDirectory = Path.Combine( Path.GetTempPath(), "NuGetProcess_" + Guid.NewGuid().ToString() );

               // Initialization step - restore needed packages, copy required files, etc
               (var assemblyPath, var framework) = new NuGetDeployment( deployConfig )
                  .DeployAsync(
                     UtilPackNuGetUtility.GetNuGetSettingsWithDefaultRootDirectory(
                        Path.GetDirectoryName( new Uri( typeof( Program ).GetTypeInfo().Assembly.CodeBase ).LocalPath ),
                        nugetConfig.NuGetConfigurationFile
                     ),
                     targetDirectory,
                     token: source.Token,
                     logger: new TextWriterLogger( new TextWriterLoggerOptions()
                     {
                        DebugWriter = null
                     } )
                     ).GetAwaiter().GetResult();

               if ( framework != null && String.IsNullOrEmpty( monitorConfig.ToolPath ) )
               {
                  monitorConfig.ToolPath = TryAutoDetectTool( framework );
               }

               Console.Write( $"\n\nInitialization is complete, starting process located in {assemblyPath}.\n\n" );

               // Monitor step - start process, and keep running until it exits or this process exits.
               // Restart the target process if it requests it.
               const String PROCESS_ARG_PREFIX = "/ProcessArgument:";
               // We don't know how target process parses arguments, and we don't want to make assumptions
               // The Microsoft.Extensions.Configuration will only see one argument with the example arguments:
               // /ProcessArgument:MyArg=34 /ProcessArgument:MyArg:Test=35
               // So we need to parse these ourselves
               var monitoring = new ProcessMonitor.ProcessMonitor(
                  monitorConfig,
                  args.Where( arg => arg.StartsWith( PROCESS_ARG_PREFIX ) )
                  .Select( arg => arg.Substring( PROCESS_ARG_PREFIX.Length ) )
                  );
               monitoring.MonitorAsync(
                  assemblyPath,
                  source.Token
                  ).GetAwaiter().GetResult();

               retVal = 0;
            }
            catch ( Exception exc )
            {
               Console.Error.WriteLine( $"An error occurred: {exc.Message}." );
               retVal = -2;
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

         return retVal;
      }

      private static String TryAutoDetectTool( NuGetFramework targetFW )
      {
         String retVal;
         if ( targetFW.IsDesktop() )
         {
            retVal = null;
         }
         else
         {
            switch ( targetFW.Framework )
            {
               case FrameworkConstants.FrameworkIdentifiers.NetCoreApp:
                  retVal = "dotnet";
                  break;
               default:
                  retVal = null;
                  break;

            }
         }

         return retVal;
      }

   }

   public class NuGetConfiguration
   {
      public String NuGetConfigurationFile { get; set; }
   }
}
