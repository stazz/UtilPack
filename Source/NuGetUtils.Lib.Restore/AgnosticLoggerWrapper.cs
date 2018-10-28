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
using NuGet.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UtilPack.NuGet
{
   /// <summary>
   /// During restore for agnostic framework, there will be two error messages that package is not compatible with Agnostic framework.
   /// Use this class to suppress those and only those two messages, and everything else to pass thru to wrapped logger.
   /// </summary>
   public sealed class AgnosticFrameworkLoggerWrapper : ILogger
   {
      private const String AGNOSTIC_ERROR_1 = "is not compatible with agnostic (Agnostic,Version=v0.0).";
      private const String AGNOSTIC_ERROR_2 = "One or more packages are incompatible with Agnostic,Version=v0.0.";

      private readonly ILogger _logger;

      /// <summary>
      /// Creates a new instance of <see cref="AgnosticFrameworkLoggerWrapper"/> with given logger to delegate log methods to.
      /// </summary>
      /// <param name="logger">The wrapped logger. May be <c>null</c>, in which case the log methods become pass-thru.</param>
      public AgnosticFrameworkLoggerWrapper( ILogger logger )
      {
         this._logger = logger;
      }

      /// <inheritdoc/>
      public void Log( LogLevel level, String data )
      {
         if ( this._logger != null
            && level == LogLevel.Error
            && !String.IsNullOrEmpty( data )
            && data.IndexOf( AGNOSTIC_ERROR_1 ) < 0
            && data.IndexOf( AGNOSTIC_ERROR_2 ) < 0
            )
         {
            this._logger.Log( level, data );
         }
      }

      /// <inheritdoc/>
      public void Log( ILogMessage message )
      {
         String data;
         if ( this._logger != null
            && message != null
            && message.Level == LogLevel.Error
            && !String.IsNullOrEmpty( data = message.Message )
            && data.IndexOf( AGNOSTIC_ERROR_1 ) < 0
            && data.IndexOf( AGNOSTIC_ERROR_2 ) < 0
            )
         {
            this._logger.Log( message );
         }
      }

      /// <inheritdoc/>
      public Task LogAsync( LogLevel level, String data )
      {
         return this._logger != null
            && level == LogLevel.Error
            && !String.IsNullOrEmpty( data )
            && data.IndexOf( AGNOSTIC_ERROR_1 ) < 0
            && data.IndexOf( AGNOSTIC_ERROR_2 ) < 0 ?
               this._logger.LogAsync( level, data ) :
               TaskUtils.CompletedTask;
      }

      /// <inheritdoc/>
      public Task LogAsync( ILogMessage message )
      {
         String data;
         return this._logger != null
            && message != null
            && message.Level == LogLevel.Error
            && !String.IsNullOrEmpty( data = message.Message )
            && data.IndexOf( AGNOSTIC_ERROR_1 ) < 0
            && data.IndexOf( AGNOSTIC_ERROR_2 ) < 0 ?
               this._logger.LogAsync( message ) :
               TaskUtils.CompletedTask;
      }

      /// <inheritdoc/>
      public void LogDebug( String data )
      {
         this._logger?.LogDebug( data );
      }

      /// <inheritdoc/>
      public void LogError( String data )
      {
         if (
            this._logger != null
            && !String.IsNullOrEmpty( data )
            && data.IndexOf( AGNOSTIC_ERROR_1 ) < 0
            && data.IndexOf( AGNOSTIC_ERROR_2 ) < 0
            )
         {
            this._logger.LogError( data );
         }
      }

      /// <inheritdoc/>
      public void LogInformation( String data )
      {
         this._logger?.LogInformation( data );
      }

      /// <inheritdoc/>
      public void LogInformationSummary( String data )
      {
         this._logger?.LogInformationSummary( data );
      }

      /// <inheritdoc/>
      public void LogMinimal( String data )
      {
         this._logger?.LogMinimal( data );
      }

      /// <inheritdoc/>
      public void LogVerbose( String data )
      {
         this._logger?.LogVerbose( data );
      }

      /// <inheritdoc/>
      public void LogWarning( String data )
      {
         this._logger?.LogWarning( data );
      }
   }
}
