using NuGet.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace UtilPack.NuGet
{
   /// <summary>
   /// During restore for agnostic framework, there will be two error messages that package is not compatible with Agnostic framework.
   /// Use this class to suppress those and only those two messages, and everything else to pass thru to wrapped logger.
   /// </summary>
   public sealed class AgnosticFrameworkLoggerWrapper : ILogger
   {
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
      public void LogDebug( String data )
      {
         this._logger?.LogDebug( data );
      }

      /// <inheritdoc/>
      public void LogError( String data )
      {
         if ( this._logger != null && !String.IsNullOrEmpty( data ) )
         {
            if (
               data.IndexOf( "is not compatible with agnostic (Agnostic,Version=v0.0)." ) < 0
               && data.IndexOf( "One or more packages are incompatible with Agnostic,Version=v0.0." ) < 0
               )
            {
               this._logger.LogError( data );
            }
         }
      }

      /// <inheritdoc/>
      public void LogErrorSummary( String data )
      {
         this._logger?.LogErrorSummary( data );
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
