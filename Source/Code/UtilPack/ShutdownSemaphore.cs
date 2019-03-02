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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;


namespace UtilPack
{
#if !NET40
   using TaskEx = Task;
#endif

   /// <summary>
   /// This interface is used by the parent process which can signal the shutdown for the process it spawns.
   /// The child process typically uses <see cref="ShutdownSemaphoreAwaiter"/> to await for the cancellation signal.
   /// </summary>
   public interface ShutdownSemaphoreSignaller : IDisposable
   {
      /// <summary>
      /// This method should be invoked when the cancellation signal should be sent for child process.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> to use for any possible asynchronous operation.</param>
      /// <returns>Task to await on, or <c>null</c>.</returns>
      Task SignalAsync( CancellationToken token );
   }

   /// <summary>
   /// This interface is used by the child process which watches for cancellation signal sent by parent process.
   /// The parent process typically uses <see cref="ShutdownSemaphoreSignaller"/> to signal the cancellation.
   /// </summary>
   public interface ShutdownSemaphoreAwaiter : IDisposable
   {
      /// <summary>
      /// This method will asynchronously await for the cancellation signal from the parent process.
      /// When the returned task completes, the cancellation signal has been sent, or given <see cref="CancellationToken"/> has been set to canceled state.
      /// The task will not complete before that.
      /// </summary>
      /// <param name="token">The <see cref="CancellationToken"/> used to control premature exit of this method.</param>
      /// <returns>A task which completes either when parent process sents cancellation request, or when given <paramref name="token"/> is set to canceled state.</returns>
      Task WaitForShutdownSignal( CancellationToken token );
   }

   /// <summary>
   /// This class contains factory methods for <see cref="ShutdownSemaphoreSignaller"/> and <see cref="ShutdownSemaphoreAwaiter"/>.
   /// Theses classes either wrap native named global <see cref="Semaphore"/> functionality, or use file system to implement signalling (e.g. on Alpine Linux, where named synhcronization primitivies are not supported).
   /// </summary>
   public static class ShutdownSemaphoreFactory
   {
      private const String SEMAPHORE_PREFIX = @"Global\";

      /// <summary>
      /// Creates a new <see cref="ShutdownSemaphoreSignaller"/> with given semaphore name.
      /// If global named semaphores are not supported on this platform, it will create a file in temporary folder (name of the file containing given <paramref name="semaphoreName"/>) which will be used to implement signalling.
      /// </summary>
      /// <param name="semaphoreName">The name of the semaphore. Will be prefixed with <c>Global\</c> string when given to <see cref="Semaphore"/>.</param>
      /// <returns>A new instance of <see cref="ShutdownSemaphoreSignaller"/>, which will either wrap a <see cref="Semaphore"/>, or use file system.</returns>
      public static ShutdownSemaphoreSignaller CreateSignaller( String semaphoreName )
      {
         try
         {
            return new SignallerByWrapper( CreateSemaphore( semaphoreName ) );
         }
         catch ( PlatformNotSupportedException )
         {
#if NETSTANDARD1_0 || NETSTANDARD1_1
            throw new PlatformNotSupportedException( "File-based shutdown semaphore functionality not supported on .NET Standard 1.0/1.1." )
#else
            return new SignallerByFile( GetFilePath( semaphoreName ) )
#endif
               ;
         }
      }

      /// <summary>
      /// Creates a new <see cref="ShutdownSemaphoreAwaiter"/> with given semaphore name.
      /// If global named semaphores are not supported on this platform, it will use a file in temporary folder (named in same way as <see cref="CreateSignaller"/> method does) to watch cancellation signal.
      /// </summary>
      /// <param name="semaphoreName">The name of the semaphore. Will be prefixed with <c>Global\</c> string when given to <see cref="Semaphore"/> opening method.</param>
      /// <returns>A new instance of <see cref="ShutdownSemaphoreAwaiter"/>, which will either wrap a <see cref="Semaphore"/>, or use file system.</returns>
      public static ShutdownSemaphoreAwaiter CreateAwaiter( String semaphoreName )
      {
         Semaphore semaphore = null;
         try
         {
#if NET40
            semaphore = Semaphore.OpenExisting(
#else
            Semaphore.TryOpenExisting(
#endif
               SEMAPHORE_PREFIX + semaphoreName
#if !NET40
               , out semaphore
#endif
               );
         }
         catch ( PlatformNotSupportedException )
         {

         }
         return semaphore == null ?
#if NETSTANDARD1_0 || NETSTANDARD1_1
            throw new PlatformNotSupportedException( "File-based shutdown semaphore functionality not supported on .NET Standard 1.0/1.1." )
#else
            (ShutdownSemaphoreAwaiter) new AwaiterByFile( GetFilePath( semaphoreName ) )
#endif
            : new AwaiterByWrapper( semaphore );
      }

#if !NETSTANDARD1_0 && !NETSTANDARD1_1
      private static String GetFilePath( String semaphoreName )
      {
         return Path.Combine( Path.GetTempPath(), "ShutdownFile_" + semaphoreName );
      }
#endif
      private static Semaphore CreateSemaphore( String semaphoreName )
      {
         semaphoreName = SEMAPHORE_PREFIX + semaphoreName;
         var retVal = new Semaphore( 0, Int32.MaxValue, semaphoreName, out var createdNewSemaphore );
         if ( !createdNewSemaphore )
         {
            retVal.DisposeSafely();
            throw new ArgumentException( "Semaphore name " + semaphoreName + " already existed." );
         }
         return retVal;
      }

      private sealed class SignallerByWrapper : AbstractDisposable, ShutdownSemaphoreSignaller
      {
         private readonly Semaphore _semaphore;

         public SignallerByWrapper( Semaphore semaphore )
         {
            this._semaphore = ArgumentValidator.ValidateNotNull( nameof( semaphore ), semaphore );
         }

         public Task SignalAsync( CancellationToken token )
         {
            this._semaphore.Release();
            return null;
         }

         protected override void Dispose( Boolean disposing )
         {
            if ( disposing )
            {
               this._semaphore.DisposeSafely();
            }
         }
      }

#if !NETSTANDARD1_0 && !NETSTANDARD1_1
      private sealed class SignallerByFile : AbstractDisposable, ShutdownSemaphoreSignaller
      {
         private readonly String _filePath;
         private readonly Stream _stream;

         public SignallerByFile( String filePath )
         {
            this._filePath = ArgumentValidator.ValidateNotEmpty( nameof( filePath ), filePath );
            this._stream = File.Open( filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read );
         }

         public Task SignalAsync( CancellationToken token )
         {
            // Since writing to file is native operation, cancellation token check occurs only on entrypoint of WriteAsync method.
            // Therefore, we must close stream on cancellation, in order to cause exit of WriteAsync method (via exception)
            using ( token.Register( () => this._stream.DisposeSafely() ) )
            {
               return this._stream.WriteAsync( new Byte[] { 0 }, 0, 1, token );
            }
         }

         protected override void Dispose( Boolean disposing )
         {
            if ( disposing )
            {
               this._stream.DisposeSafely();
               File.Delete( this._filePath );
            }
         }
      }
#endif

      private sealed class AwaiterByWrapper : AbstractDisposable, ShutdownSemaphoreAwaiter
      {
         private readonly Semaphore _semaphore;

         public AwaiterByWrapper( Semaphore semaphore )
         {
            this._semaphore = ArgumentValidator.ValidateNotNull( nameof( semaphore ), semaphore );
         }

         public async Task WaitForShutdownSignal( CancellationToken token )
         {
            var sema = this._semaphore;
            while (
               !token.IsCancellationRequested
               && !sema.WaitOne( 0 )
               )
            {
               await TaskEx.Delay( 100 );
            }
         }

         protected override void Dispose( Boolean disposing )
         {
            this._semaphore.DisposeSafely();
         }
      }

#if !NETSTANDARD1_0 && !NETSTANDARD1_1

      private sealed class AwaiterByFile : AbstractDisposable, ShutdownSemaphoreAwaiter
      {
         private readonly Stream _stream;

         public AwaiterByFile( String filePath )
         {
            this._stream = File.Open( filePath, FileMode.Open, FileAccess.Read, FileShare.Read );
         }

         public async Task WaitForShutdownSignal( CancellationToken token )
         {
            while (
               !token.IsCancellationRequested
               && this._stream.Length == 0
               )
            {
               await TaskEx.Delay( 100 );
            }
         }

         protected override void Dispose( Boolean disposing )
         {
            this._stream.DisposeSafely();
         }
      }

#endif

   }
}