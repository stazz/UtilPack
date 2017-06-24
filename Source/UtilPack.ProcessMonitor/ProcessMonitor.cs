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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace UtilPack.ProcessMonitor
{
   public class ProcessMonitor
   {
      private readonly MonitoringConfiguration _config;
      private readonly String[] _args;
#if NETSTANDARD1_5
      private readonly System.Runtime.Loader.AssemblyLoadContext _assemblyLoadContext;
#endif

      public ProcessMonitor(
         MonitoringConfiguration config,
         IEnumerable<String> args
         )
      {
         this._config = ArgumentValidator.ValidateNotNull( nameof( config ), config );
         this._args = args?.ToArray() ?? Empty<String>.Array;
#if NETSTANDARD1_5
         this._assemblyLoadContext = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext( this.GetType().GetTypeInfo().Assembly );
#endif
      }

      public async Task MonitorAsync(
         String assemblyPath,
         CancellationToken token
         )
      {
         var config = this._config;

         String location;
         StringBuilder argsBuilder;
         var tool = config.ToolPath;
         if ( !String.IsNullOrEmpty( tool ) )
         {
            location = tool;
            argsBuilder = new StringBuilder( EscapeArgumentString( assemblyPath ) );
         }
         else
         {
            location = assemblyPath;
            argsBuilder = new StringBuilder();
         }

         var prefix = this._config.ProcessArgumentPrefix;
         foreach ( var arg in this._args )
         {
            if ( argsBuilder.Length > 0 )
            {
               argsBuilder.Append( " " );
            }
            argsBuilder
               .Append( prefix )
               .Append( EscapeArgumentString( arg ) );
         }

         var argsString = argsBuilder.ToString();
         while ( !token.IsCancellationRequested && await this.PerformSingleCycle( location, argsString, Path.GetDirectoryName( assemblyPath ), token ) )
         {
            // TODO provide "RestartStateFile" argument to process, so it can convey state between restarts.
            // Or use memory mapped files
            Console.Write( "\n\nProcess requested restart...\n\n" );
         }

         if ( !token.IsCancellationRequested )
         {
            Console.Write( "\n\nProcess has exited.\n\n" );
         }
      }

      // returns true if process has signalled that it should be restarted
      private async Task<Boolean> PerformSingleCycle(
         String location,
         String argsString,
         String workingDir,
         CancellationToken token
         )
      {
         var config = this._config;
         var argPrefix = config.ProcessArgumentPrefix;
         Semaphore shutdownSemaphore = null;
         Semaphore restartSemaphore = null;
         try
         {

            shutdownSemaphore = this.CreateSemaphore( config.ShutdownSemaphoreProcessArgument, "ShutdownSemaphore_", ref argsString );
            restartSemaphore = this.CreateSemaphore( config.RestartSemaphoreProcessArgument, "RestartSemaphore_", ref argsString );

            var startInfo = new ProcessStartInfo()
            {
               FileName = location,
               Arguments = argsString,
               CreateNoWindow = true,
               WorkingDirectory = workingDir, // Path.GetDirectoryName(epAssembly)
               RedirectStandardOutput = true,
               RedirectStandardError = true,
               RedirectStandardInput = true,
               UseShellExecute = false
            };
            var process = new Process()
            {
               StartInfo = startInfo,
               EnableRaisingEvents = true
            };
            process.OutputDataReceived += ( s, e ) =>
            {
               if ( e.Data != null ) // e.Data will be null on process closedown
               {
                  Console.WriteLine( String.Format( "[{0}]: {1}", DateTime.UtcNow, e.Data ) );
               }
            };
            process.ErrorDataReceived += ( s, e ) =>
            {
               if ( e.Data != null ) // e.Data will be null on process closedown
               {
                  Console.Write( String.Format( "[{0}] ", DateTime.UtcNow ) );
                  var oldColor = Console.ForegroundColor;
                  Console.ForegroundColor = ConsoleColor.Red;
                  Console.Write( "ERROR" );
                  Console.ForegroundColor = oldColor;
                  Console.WriteLine( String.Format( ": {0}", e.Data ) );
               }
            };


#if NETSTANDARD1_5
            Action<System.Runtime.Loader.AssemblyLoadContext> thisProcessExitHandler = ( ctx ) =>
            {
               try
               {
                  process.Kill();
               }
               catch
               {
                  // Ignore
               }
            };
            this._assemblyLoadContext.Unloading += thisProcessExitHandler;
#else
#if !NETSTANDARD1_3
            EventHandler thisProcessExitHandler = ( sender, args ) =>
            {
               try
               {
                  process.Kill();
               }
               catch
               {
                  // Ignore
               }
            };
            AppDomain.CurrentDomain.ProcessExit += thisProcessExitHandler;
#endif
#endif

#if NETSTANDARD1_5 || !NETSTANDARD1_3
            using ( new UsingHelper( () =>
#if NETSTANDARD1_5
            this._assemblyLoadContext.Unloading
#else
            AppDomain.CurrentDomain.ProcessExit
#endif
            -= thisProcessExitHandler
            ) )
            {
#endif

               // Start the process
               process.Start();
               process.BeginOutputReadLine();
               process.BeginErrorReadLine();

               var restart = false;
               DateTime? shutdownSignalledTime = null;
               using ( var cancelRegistration = token.Register( () =>
               {
                  try
                  {
                     if ( shutdownSemaphore == null )
                     {
                        // Kill the process
                        process.Kill();
                     }
                     else
                     {
                        // Signal the process to shut down
                        shutdownSignalledTime = DateTime.UtcNow;
                        shutdownSemaphore.Release();
                     }
                  }
                  catch
                  {
                     // Make the main loop stop
                     shutdownSignalledTime = DateTime.UtcNow;
                  }
               } ) )
               {

                  var hasExited = false;
                  while ( !hasExited )
                  {
                     if ( process.WaitForExit( 0 ) )
                     {
                        // The process has exited, clean up our stuff

                        // Process.HasExited has following documentation:
                        // When standard output has been redirected to asynchronous event handlers, it is possible that output processing will
                        // not have completed when this property returns true. To ensure that asynchronous event handling has been completed,
                        // call the WaitForExit() overload that takes no parameter before checking HasExited.
                        process.WaitForExit();
                        hasExited = true;

                        // Now, check if restart semaphore has been signalled
                        restart = restartSemaphore != null && restartSemaphore.WaitOne( 0 );
                     }
                     else if ( shutdownSignalledTime.HasValue && DateTime.UtcNow - shutdownSignalledTime.Value > config.ShutdownSemaphoreWaitTime )
                     {
                        // We have signalled shutdown, but process has not exited in time
                        try
                        {
                           process.Kill();
                        }
                        catch
                        {
                           // Nothing we can do, really
                           hasExited = true;
                        }
                     }
                     else
                     {
                        // Wait async
                        await
#if NET40
                        TaskEx
#else
                        Task
#endif
                        .Delay( 100 );
                     }
                  }
               }

               return restart;
#if NETSTANDARD1_5 || !NETSTANDARD1_3
            }
#endif
         }
         finally
         {
            shutdownSemaphore?.DisposeSafely();
            restartSemaphore?.DisposeSafely();
         }

      }

      private Semaphore CreateSemaphore( String argumentName, String namePrefix, ref String argsString )
      {
         Semaphore retVal = null;
         if ( !String.IsNullOrEmpty( argumentName ) )
         {
            retVal = this.CreateSemaphore( namePrefix, out var semaName );
            argsString += " " + EscapeArgumentString( String.Format( "{0}{1}={2}", this._config.ProcessArgumentPrefix, argumentName, semaName ) );
         }

         return retVal;
      }

      private Semaphore CreateSemaphore( String namePrefix, out String semaphoreName )
      {
         var bytez = new Byte[32];
         Semaphore retVal;
         do
         {
            semaphoreName = @"Global\" + namePrefix + StringConversions.EncodeBase64( Guid.NewGuid().ToByteArray(), true );
            retVal = new Semaphore( 0, Int32.MaxValue, semaphoreName, out var createdNewSemaphore );
            if ( !createdNewSemaphore )
            {
               retVal.DisposeSafely();
               retVal = null;
            }
         } while ( retVal == null );

         return retVal;
      }

      private static String EscapeArgumentString( String argString )
      {
         if ( argString.IndexOf( "\"" ) >= 0 )
         {
            argString = "\"" + argString.Replace( "\"", "\\\"" );
         }

         return argString;
      }

   }

   public interface MonitoringConfiguration
   {
      String ToolPath { get; }
      String ProcessArgumentPrefix { get; }

      String ShutdownSemaphoreProcessArgument { get; }
      TimeSpan ShutdownSemaphoreWaitTime { get; }
      String RestartSemaphoreProcessArgument { get; }
   }

   public class DefaultMonitoringConfiguration : MonitoringConfiguration
   {
      public String ToolPath { get; set; }
      public String ProcessArgumentPrefix { get; set; } = "/";

      public String ShutdownSemaphoreProcessArgument { get; set; }
      public TimeSpan ShutdownSemaphoreWaitTime { get; set; } = TimeSpan.FromSeconds( 1 );
      public String RestartSemaphoreProcessArgument { get; set; }
   }
}
