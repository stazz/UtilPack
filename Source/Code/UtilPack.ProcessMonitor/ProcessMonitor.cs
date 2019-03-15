/*
 * Copyright 2019 Stanislav Muhametsin. All rights Reserved.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

using TaskEx = System.Threading.Tasks.
#if NET40
   TaskEx
#else
   Task
#endif
   ;


namespace UtilPack.ProcessMonitor
{

   public static class ProcessUtils
   {

      public static Process CreateProcess(
         String fileName,
         String
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
            []
#endif
            arguments,
         Action<String> onStdOutLine = null,
         Action<String> onStdErrLine = null
         )
      {
         var startInfo = new ProcessStartInfo()
         {
            FileName = fileName,
#if NET40 || NET45 || NETSTANDARD2_0 || NETCOREAPP1_1 || NETCOREAPP2_0
            Arguments = arguments,
#endif
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = onStdOutLine != null,
            RedirectStandardError = onStdErrLine != null,
         };
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
         foreach ( var arg in arguments )
         {
            startInfo.ArgumentList.Add( arg );
         }
#endif
         var p = new Process()
         {
            StartInfo = startInfo
         };

         if ( startInfo.RedirectStandardOutput )
         {
            p.OutputDataReceived += ( sender, args ) =>
            {
               if ( args.Data is String line ) // Will be null on process shutdown
               {
                  onStdOutLine( line );
               }
            };
         }
         if ( startInfo.RedirectStandardError )
         {
            p.ErrorDataReceived += ( sender, args ) =>
            {
               if ( args.Data is String line ) // Will be null on process shutdown
               {
                  onStdErrLine( line );
               }
            };
         }

         return p;
      }

      public static async Task StartProcessAsync(
         Process p,
         Func<StreamWriter, Task> stdinWriter = null
         )
      {
         var redirectStdIn = stdinWriter != null;
         p.StartInfo.RedirectStandardInput = redirectStdIn;
         p.Start();
         p.BeginOutputReadLine();
         p.BeginErrorReadLine();

         // Pass serialized configuration via stdin
         if ( redirectStdIn )
         {
            using ( var stdin = p.StandardInput )
            {
               await stdinWriter( stdin );
            }
         }
      }
   }

   public static class ProcessMonitorWithGracefulCancelability
   {
      public static TimeSpan DefaultShutdownSemaphoreWaitTime = TimeSpan.FromSeconds( 1 );

      public static async Task<EitherOr<TOutput, String>> CallProcessAndGetResultAsync<TOutput>(
         String processPath,
         String shutdownSemaphoreName,
         String
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
            []
#endif
            processArguments,
         Func<StreamWriter, Task> inputWriter,
         Func<StringBuilder, TOutput> outputDeserializer,
         CancellationToken token,
         TimeSpan? shutdownSemaphoreWaitTime = default
         )
      {

         (var stdinWriter, var stdinSuccess) = GetStdInWriter( inputWriter );
         (var exitCode, var stdout, var stderr) = await processPath.ExecuteAsFileAtThisPathWithCancelabilityCollectingOutputToString(
            processArguments,
            token,
            shutdownSemaphoreName,
            shutdownSemaphoreWaitTime ?? DefaultShutdownSemaphoreWaitTime,
            stdinWriter: stdinWriter
            );

         String GetErrorString()
         {
            var errorString = stderr.ToString();
            return String.IsNullOrEmpty( errorString ) ?
               ( exitCode == 0 ? "Unspecified error" : $"Non-zero return code of {processPath}" ) :
               errorString;
         }

         return stderr.Length > 0 || !stdinSuccess() || exitCode != 0 ?
            new EitherOr<TOutput, String>( GetErrorString() ) :
            outputDeserializer( stdout );
      }

      public static async Task<Int32?> CallProcessAndStreamOutputAsync(
         String processPath,
         String shutdownSemaphoreName,
         String
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
            []
#endif
            processArguments,
         Func<StreamWriter, Task> inputWriter,
         Func<String, Boolean, Task> onStdOutOrErrLine,
         CancellationToken token,
         TimeSpan? shutdownSemaphoreWaitTime = default
         )
      {
         (var stdinWriter, var stdinSuccess) = GetStdInWriter( inputWriter );
         var returnCode = await processPath.ExecuteAsFileAtThisPathWithCancelabilityAndRedirects(
               processArguments,
               token,
               shutdownSemaphoreName,
               shutdownSemaphoreWaitTime ?? DefaultShutdownSemaphoreWaitTime,
               stdinWriter: stdinWriter,
               onStdOutOrErrLine: onStdOutOrErrLine
               );

         return stdinSuccess() ?
            returnCode :
            default;
      }

      private static (Func<StreamWriter, Task> StdInWriter, Func<Boolean> StdInSuccess) GetStdInWriter(
         Func<StreamWriter, Task> inputWriter
         )
      {
         var stdinSuccess = false;
         return (
            async stdin =>
            {
               try
               {
                  await inputWriter( stdin );
                  stdinSuccess = true;
               }
               catch
               {
                  // Ignore
               }
            }
         ,
            () => stdinSuccess
         );
      }


      public static async Task<Int32?> StartAndWaitForExitAsync(
         this Process process, // Typically created by  ProcessMonitor.CreateProcess(), just don't start it!
                               // String shutdownSemaphoreArgumentName,
         CancellationToken token,
         String shutdownSemaphoreName,
         TimeSpan shutdownSemaphoreMaxWaitTime,
         // Boolean cancelabilityIsOptional = false,
         Func<StreamWriter, Task> stdinWriter = null,
         Func<Task> onTick = null
         )
      {
         Int32? exitCode = null;
         using ( var shutdownSemaphore = token.CanBeCanceled ? ShutdownSemaphoreFactory.CreateSignaller( shutdownSemaphoreName ) : default )
         using ( process ) // var process = ProcessMonitor.CreateProcess( fileName, arguments, onStdOutLine, onStdErrLine ) )
         {
            process.EnableRaisingEvents = true;

            DateTime? shutdownSignalledTime = null;

            Task cancelTask = null;

            void OnCancel()
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
                     cancelTask = shutdownSemaphore.SignalAsync( default ); // Don't pass 'token' here as it is already canceled.
                  }
               }
               catch
               {
                  // Make the main loop stop
                  shutdownSignalledTime = DateTime.UtcNow;
               }
            }

            try
            {
               using ( token.Register( OnCancel ) )
               {
                  await ProcessUtils.StartProcessAsync( process, stdinWriter: stdinWriter );

                  var hasExited = false;

                  while ( !hasExited )
                  {
                     var tickTask = onTick?.Invoke();
                     if ( tickTask != null )
                     {
                        await tickTask;
                     }

                     if ( process.WaitForExit( 0 ) )
                     {
                        // The process has exited, clean up our stuff

                        // Process.HasExited has following documentation:
                        // When standard output has been redirected to asynchronous event handlers, it is possible that output processing will
                        // not have completed when this property returns true. To ensure that asynchronous event handling has been completed,
                        // call the WaitForExit() overload that takes no parameter before checking HasExited.
                        process.WaitForExit();
                        while ( !process.HasExited )
                        {
                           await TaskEx.Delay( 50 );
                        }

                        hasExited = true;

                        // Now, check if restart semaphore has been signalled
                        // restart = restartSemaphore != null && restartSemaphore.WaitOne( 0 );
                     }
                     else if ( shutdownSignalledTime.HasValue && DateTime.UtcNow - shutdownSignalledTime.Value > shutdownSemaphoreMaxWaitTime )
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
                        await TaskEx.Delay( 50 );
                     }
                  }
               }
            }
            finally
            {
               if ( cancelTask != null )
               {
                  await cancelTask;
               }
            }

            try
            {
               exitCode = process.ExitCode;
            }
            catch
            {
               // Ignore
            }

         }

         return exitCode;

      }

      public static async Task<(Int32? ReturnCode, StringBuilder StdOut, StringBuilder StdErr)> ExecuteAsFileAtThisPathWithCancelabilityCollectingOutputToString(
         this String fileName,
         String
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
            []
#endif
            arguments,
         CancellationToken token,
         String shutdownSemaphoreName,
         TimeSpan shutdownSemaphoreMaxWaitTime,
         Func<StreamWriter, Task> stdinWriter = null
         )
      {
         var stdout = new StringBuilder();
         var stderr = new StringBuilder();
         var retVal = await fileName.ExecuteAsFileAtThisPathWithCancelabilityAndRedirects(
            arguments,
            token,
            shutdownSemaphoreName,
            shutdownSemaphoreMaxWaitTime,
            stdinWriter: stdinWriter,
            onStdOutOrErrLine: ( line, isError ) =>
            {
               ( isError ? stderr : stdout ).Append( line ).Append( '\n' );
               return null;
            } );
         return (retVal, stdout, stderr);
      }

      public static async Task<Int32?> ExecuteAsFileAtThisPathWithCancelabilityAndRedirects(
         this String fileName,
         String
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
            []
#endif
            arguments,
         CancellationToken token,
         String shutdownSemaphoreName,
         TimeSpan shutdownSemaphoreMaxWaitTime,
         Func<StreamWriter, Task> stdinWriter = null,
         Func<String, Boolean, Task> onStdOutOrErrLine = null
         )
      {
         var processOutput = new ConcurrentQueue<(Boolean IsError, DateTime Timestamp, String Data)>();
         try
         {
            return await ProcessUtils.CreateProcess(
               fileName,
               arguments,
               onStdOutLine: outLine => processOutput.Enqueue( (false, DateTime.UtcNow, outLine) ),
               onStdErrLine: errLine => processOutput.Enqueue( (true, DateTime.UtcNow, errLine) ) )
               .StartAndWaitForExitAsync(
                  token,
                  shutdownSemaphoreName,
                  shutdownSemaphoreMaxWaitTime,
                  stdinWriter: stdinWriter,
                  onTick: async () => await ProcessOutput( processOutput, onStdOutOrErrLine )
               );
         }
         finally
         {
            // Flush any 'leftover' messages
            await ProcessOutput( processOutput, onStdOutOrErrLine );
         }
      }

      private static async Task ProcessOutput(
         ConcurrentQueue<(Boolean IsError, DateTime Timestamp, String Data)> processOutput,
         Func<String, Boolean, Task> onStdOutOrErrLine
         )
      {
         if ( onStdOutOrErrLine == null )
         {
            onStdOutOrErrLine = ( line, isError ) => ( isError ? Console.Error : Console.Out ).WriteLineAsync( line );
         }

         while ( processOutput.TryDequeue( out var output ) )
         {
            var t = onStdOutOrErrLine( output.Data, output.IsError );
            if ( t != null )
            {
               await t;
            }
         }
      }

   }
}