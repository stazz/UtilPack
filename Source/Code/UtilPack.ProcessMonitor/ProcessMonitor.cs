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
   /// <summary>
   /// This static class provides methods for executing processes while keeping an eye on given <see cref="CancellationToken"/>.
   /// </summary>
   /// <remarks>
   /// The invokable process is assumed to implement graceful shutdown whenever semaphore with given name detects cancellation signal from this process.
   /// See <see cref="ShutdownSemaphoreFactory"/> and <see cref="ShutdownSemaphoreAwaiter"/> for more details about implementing such functionality in the invoked process.
   /// </remarks>
   public static class ProcessMonitorWithGracefulCancelability
   {
      /// <summary>
      /// The default maximum time to wait for process to gracefully terminate after the cancellation token is canceled.
      /// The value is 1 second.
      /// </summary>
      public static TimeSpan DefaultShutdownSemaphoreWaitTime = TimeSpan.FromSeconds( 1 );

      /// <summary>
      /// Asynchronously starts process at given path, optionally writes input to it, then waits for it to exit while keeping an eye for given <see cref="CancellationToken"/>, and invokes given deserialization callback on contents of standard output, if the execution was successful.
      /// </summary>
      /// <typeparam name="TOutput">The deserialized type of standard output.</typeparam>
      /// <param name="processPath">The path to the process executable.</param>
      /// <param name="processArguments">The parameters for the process.</param>
      /// <param name="shutdownSemaphoreName">The name of the semaphore used to signal graceful shutdown after <paramref name="token"/> cancellation. Do not use <c>"Global\"</c> prefix.</param>
      /// <param name="outputDeserializer">The callback to deserialize <typeparamref name="TOutput"/> from standard output, in case process returned successfully.</param>
      /// <param name="token">The <see cref="CancellationToken"/> to use to check on cancellation.</param>
      /// <param name="inputWriter">The optional callback to write input to the process. If no value is specified, the process will not have standard input.</param>
      /// <param name="shutdownSemaphoreMaxWaitTime">The maximum time to wait for process to gracefully terminate after the cancellation token is canceled. By default, is value of <see cref="DefaultShutdownSemaphoreWaitTime"/>.</param>
      /// <returns>Asynchronously returns either deserialized instance of <typeparamref name="TOutput"/>, or error string. The error string will be <c>null</c> if given <paramref name="token"/> is canceled, otherwise it will be either contents of standard error, or fixed error message.</returns>
      public static async Task<EitherOr<TOutput, String>> CallProcessAndGetResultAsync<TOutput>(
         String processPath,
         String
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
            []
#endif
            processArguments,
         String shutdownSemaphoreName,
         Func<StringBuilder, TOutput> outputDeserializer,
         CancellationToken token,
         Func<StreamWriter, Task> inputWriter = null,
         TimeSpan? shutdownSemaphoreMaxWaitTime = default
         )
      {

         (var stdinWriter, var stdinSuccess) = inputWriter == null ? default : GetStdInWriter( inputWriter );
         (var exitCode, var stdout, var stderr) = await CallProcessAndCollectOutputToString(
            processPath,
            processArguments,
            shutdownSemaphoreName,
            token,
            stdinWriter: stdinWriter,
            shutdownSemaphoreMaxWaitTime: shutdownSemaphoreMaxWaitTime
            );

         String GetErrorString()
         {
            var errorString = stderr.ToString();
            return exitCode.HasValue ?
               ( String.IsNullOrEmpty( errorString ) ? ( exitCode == 0 ? "Unspecified error" : $"Non-zero return code of {processPath}" ) : errorString )
               : null;
         }

         return stderr.Length > 0 || !CheckStdInSuccess( stdinSuccess ) || !exitCode.HasValue || exitCode.Value != 0 ?
            new EitherOr<TOutput, String>( GetErrorString() ) :
            outputDeserializer( stdout );
      }

      //      /// <summary>
      //      /// Asynchronously starts process at given path, optionally writes input to it, then waits for it to exit while keeping an eye for given <see cref="CancellationToken"/>, and streaming standard output and error streams.
      //      /// </summary>
      //      /// <param name="processPath">The path to the process executable.</param>
      //      /// <param name="processArguments">The parameters for the process.</param>
      //      /// <param name="shutdownSemaphoreName">The name of the semaphore used to signal graceful shutdown after <paramref name="token"/> cancellation. Do not use <c>"Global\"</c> prefix.</param>
      //      /// <param name="onStdOutOrErrLine">The callback invoked on each event of <see cref="Process.OutputDataReceived"/>. The tuple first item is the string, the tuple second item is <c>true</c> if the string originates from error stream, and third item is the UTC <see cref="DateTime"/> when it was received by <see cref="Process.OutputDataReceived"/>.</param>
      //      /// <param name="token">The <see cref="CancellationToken"/> to use to check on cancellation.</param>
      //      /// <param name="inputWriter">The optional callback to write input to the process. If no value is specified, the process will not have standard input.</param>
      //      /// <param name="shutdownSemaphoreMaxWaitTime">The maximum time to wait for process to gracefully terminate after the cancellation token is canceled. By default, is value of <see cref="DefaultShutdownSemaphoreWaitTime"/>.</param>
      //      /// <returns>Asynchronously returns the process exit code. Will return <c>null</c> if given <paramref name="token"/> is canceled.</returns>
      //      /// <exception cref="InvalidOperationException">If the given <paramref name="inputWriter"/> was specified, but did not complete successfully.</exception>
      //      public static async Task<Int32?> CallProcessAndStreamOutputAsync(
      //         String processPath,
      //         String
      //#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
      //            []
      //#endif
      //            processArguments,
      //         String shutdownSemaphoreName,
      //         Func<(String Data, Boolean IsError, DateTime Timestamp), Task> onStdOutOrErrLine,
      //         CancellationToken token,
      //         Func<StreamWriter, Task> inputWriter = null,
      //         TimeSpan? shutdownSemaphoreMaxWaitTime = default
      //         )
      //      {
      //         (var stdinWriter, var stdinSuccess) = inputWriter == null ? default : GetStdInWriter( inputWriter );
      //         var returnCode = await processPath.CallProcessWithRedirects(
      //               processArguments,
      //               shutdownSemaphoreName,
      //               token,
      //               stdinWriter: stdinWriter,
      //               shutdownSemaphoreMaxWaitTime: shutdownSemaphoreMaxWaitTime,
      //               onStdOutOrErrLine: onStdOutOrErrLine
      //               );

      //         return CheckStdInSuccess( stdinSuccess ) ?
      //            returnCode :
      //            throw new InvalidOperationException( "Standard input writer did not complete successfully." );
      //      }

      private static Boolean CheckStdInSuccess(
         Func<Boolean> stdInSuccess
         )
      {
         return stdInSuccess == null || stdInSuccess();
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



      /// <summary>
      /// Asynchronously starts process at given path, optionally writes input to it, then waits for it to exit while keeping an eye for given <see cref="CancellationToken"/>, and streaming standard output and error streams.
      /// </summary>
      /// <param name="processPath">The path to the process executable.</param>
      /// <param name="processArguments">The parameters for the process.</param>
      /// <param name="shutdownSemaphoreName">The name of the semaphore used to signal graceful shutdown after <paramref name="token"/> cancellation. Do not use <c>"Global\"</c> prefix.</param>
      /// <param name="token">The <see cref="CancellationToken"/> to use to check on cancellation.</param>
      /// <param name="stdinWriter">The optional callback to write input to the process. If no value is specified, the process will not have standard input.</param>
      /// <param name="shutdownSemaphoreMaxWaitTime">The maximum time to wait for process to gracefully terminate after the cancellation token is canceled. By default, is value of <see cref="DefaultShutdownSemaphoreWaitTime"/>.</param>
      /// <returns>Asynchronously returns the process exit code. Will return <c>null</c> if given <paramref name="token"/> is canceled. Along with the exit code are also returned standard output and error <see cref="StringBuilder"/> instances.</returns>
      public static async Task<(Int32? ReturnCode, StringBuilder StdOut, StringBuilder StdErr)> CallProcessAndCollectOutputToString(
         String processPath,
         String
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
            []
#endif
            processArguments,
         String shutdownSemaphoreName,
         CancellationToken token,
         Func<StreamWriter, Task> stdinWriter = null,
         TimeSpan? shutdownSemaphoreMaxWaitTime = null
         )
      {
         var stdout = new StringBuilder();
         var stderr = new StringBuilder();
         var retVal = await CallProcessWithRedirects(
            processPath,
            processArguments,
            shutdownSemaphoreName,
            token,
            stdinWriter: stdinWriter,
            shutdownSemaphoreMaxWaitTime: shutdownSemaphoreMaxWaitTime,
            onStdOutOrErrLine: tuple =>
            {
               (var line, var isError, var timestamp) = tuple;
               ( isError ? stderr : stdout ).Append( line ).Append( '\n' );
               return null;
            } );
         return (retVal, stdout, stderr);
      }


      /// <summary>
      /// Asynchronously starts process at given path, optionally writes input to it, then waits for it to exit while keeping an eye for given <see cref="CancellationToken"/>, and streaming standard output and error streams.
      /// </summary>
      /// <param name="processPath">The path to the process executable.</param>
      /// <param name="processArguments">The parameters for the process.</param>
      /// <param name="shutdownSemaphoreName">The name of the semaphore used to signal graceful shutdown after <paramref name="token"/> cancellation. Do not use <c>"Global\"</c> prefix.</param>
      /// <param name="token">The <see cref="CancellationToken"/> to use to check on cancellation.</param>
      /// <param name="stdinWriter">The optional callback to write input to the process. If no value is specified, the process will not have standard input.</param>
      /// <param name="shutdownSemaphoreMaxWaitTime">The maximum time to wait for process to gracefully terminate after the cancellation token is canceled. By default, is value of <see cref="DefaultShutdownSemaphoreWaitTime"/>.</param>
      /// <returns>Asynchronously returns the process exit code. Will return <c>null</c> if given <paramref name="token"/> is canceled.</returns>
      public static async Task<Int32?> CallProcessWithRedirects(
         String processPath,
         String
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
            []
#endif
            processArguments,
         String shutdownSemaphoreName,
         CancellationToken token,
         Func<StreamWriter, Task> stdinWriter = null,
         TimeSpan? shutdownSemaphoreMaxWaitTime = null,
         Func<(String Data, Boolean IsError, DateTime Timestamp), Task> onStdOutOrErrLine = null
         )
      {
         var processOutput = new ConcurrentQueue<(String Data, Boolean IsError, DateTime Timestamp)>();
         try
         {
            return await ProcessUtils.StartAndWaitForExitAsync(
               ProcessUtils.CreateProcess(
                  processPath,
                  processArguments,
                  onStdOutLine: outLine => processOutput.Enqueue( (outLine, false, DateTime.UtcNow) ),
                  onStdErrLine: errLine => processOutput.Enqueue( (errLine, true, DateTime.UtcNow) ) ),
               shutdownSemaphoreName,
               token,
               stdinWriter: stdinWriter,
               onTick: async () => await ProcessOutput( processOutput, onStdOutOrErrLine ),
               shutdownSemaphoreMaxWaitTime: shutdownSemaphoreMaxWaitTime
               );
         }
         finally
         {
            // Flush any 'leftover' messages
            await ProcessOutput( processOutput, onStdOutOrErrLine );
         }
      }

      private static async Task ProcessOutput(
         ConcurrentQueue<(String Data, Boolean IsError, DateTime Timestamp)> processOutput,
         Func<(String Data, Boolean IsError, DateTime Timestamp), Task> onStdOutOrErrLine
         )
      {
         if ( onStdOutOrErrLine == null )
         {
            onStdOutOrErrLine = ( tuple ) => ( tuple.IsError ? Console.Error : Console.Out ).WriteLineAsync( tuple.Data );
         }

         while ( processOutput.TryDequeue( out var output ) )
         {
            var t = onStdOutOrErrLine( output );
            if ( t != null )
            {
               await t;
            }
         }
      }

   }

   /// <summary>
   /// This class is mainly for internal usage, but is exposed in case some functionality is needed from other libraries.
   /// </summary>
   public static class ProcessUtils
   {
      /// <summary>
      /// Creates a new instance of <see cref="Process"/> but doesn't start it.
      /// </summary>
      /// <param name="processPath">The path to the process executable.</param>
      /// <param name="processArguments">The parameters for the process.</param>
      /// <param name="onStdOutLine">The optional callback to invoke when standard output has been received.</param>
      /// <param name="onStdErrLine">The optional callback to invoke when standard error output has been received.</param>
      /// <returns></returns>
      public static Process CreateProcess(
         String processPath,
         String
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
            []
#endif
            processArguments,
         Action<String> onStdOutLine = null,
         Action<String> onStdErrLine = null
         )
      {
         var startInfo = new ProcessStartInfo()
         {
            FileName = processPath,
#if NET40 || NET45 || NETSTANDARD2_0 || NETCOREAPP1_1 || NETCOREAPP2_0
            Arguments = processArguments,
#endif
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = onStdOutLine != null,
            RedirectStandardError = onStdErrLine != null,
         };
#if !NET40 && !NET45 && !NETSTANDARD2_0 && !NETCOREAPP1_1 && !NETCOREAPP2_0
         foreach ( var arg in processArguments )
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

      /// <summary>
      /// Starts this process and asynchronously writes data to standard input, if <paramref name="stdinWriter"/> parameter is specified.
      /// </summary>
      /// <param name="p">The <see cref="Process"/>.</param>
      /// <param name="stdinWriter">Optional callback to write to input.</param>
      /// <returns>Asynchronously returns <c>void</c>.</returns>
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

         if ( redirectStdIn )
         {
            using ( var stdin = p.StandardInput )
            {
               await stdinWriter( stdin );
            }
         }
      }

      /// <summary>
      /// This utility method will start the given <see cref="Process"/> and then will wait for it to complete, while keeping an eye for the given <see cref="CancellationToken"/>.
      /// </summary>
      /// <param name="process">This <see cref="Process"/>.</param>
      /// <param name="shutdownSemaphoreName">The name of the semaphore used to signal graceful shutdown after <paramref name="token"/> cancellation. Do not use <c>"Global\"</c> prefix.</param>
      /// <param name="token">The <see cref="CancellationToken"/> to use.</param>
      /// <param name="stdinWriter">The optional callback to write input to the process. If no value is specified, the process will not have standard input.</param>
      /// <param name="shutdownSemaphoreWaitTime">The maximum time to wait for process to gracefully terminate after the cancellation token is canceled. By default, is value of <see cref="DefaultShutdownSemaphoreWaitTime"/>.</param>
      /// <param name="onTick">The optional callback to perform some action between polling for process state.</param>
      /// <returns>Asynchronously returns the process exit code. Will return <c>null</c> if given <paramref name="token"/> is canceled.</returns>
      /// <remarks>
      /// Typically the <paramref name="process"/> to start is created by <see cref="ProcessUtils.CreateProcess"/>.
      /// </remarks>
      public static async Task<Int32?> StartAndWaitForExitAsync(
         Process process,
         String shutdownSemaphoreName,
         CancellationToken token,
         Func<StreamWriter, Task> stdinWriter = null,
         TimeSpan? shutdownSemaphoreMaxWaitTime = null,
         Func<Task> onTick = null
         )
      {
         Int32? exitCode = null;
         var maxTime = shutdownSemaphoreMaxWaitTime ?? ProcessMonitorWithGracefulCancelability.DefaultShutdownSemaphoreWaitTime;
         using ( var shutdownSemaphore = token.CanBeCanceled ? ShutdownSemaphoreFactory.CreateSignaller( shutdownSemaphoreName ) : default )
         using ( process )
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
                     else if ( shutdownSignalledTime.HasValue && DateTime.UtcNow - shutdownSignalledTime.Value > maxTime )
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

            if ( !token.IsCancellationRequested )
            {
               try
               {
                  exitCode = process.ExitCode;
               }
               catch
               {
                  // Ignore
               }
            }

         }

         return exitCode;

      }
   }
}