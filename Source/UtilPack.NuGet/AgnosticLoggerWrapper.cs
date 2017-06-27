using NuGet.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace UtilPack.NuGet
{
   // During initial restore, we get two error messages that package is not compatible with Agnostic framework, and we would like to suppress that error message
   public sealed class AgnosticFrameworkLoggerWrapper : ILogger
   {
      private readonly ILogger _logger;

      public AgnosticFrameworkLoggerWrapper( ILogger logger )
      {
         this._logger = logger;
      }

      public void LogDebug( String data )
      {
         this._logger?.LogDebug( data );
      }

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

      public void LogErrorSummary( String data )
      {
         this._logger?.LogErrorSummary( data );
      }

      public void LogInformation( String data )
      {
         this._logger?.LogInformation( data );
      }

      public void LogInformationSummary( String data )
      {
         this._logger?.LogInformationSummary( data );
      }

      public void LogMinimal( String data )
      {
         this._logger?.LogMinimal( data );
      }

      public void LogVerbose( String data )
      {
         this._logger?.LogVerbose( data );
      }

      public void LogWarning( String data )
      {
         this._logger?.LogWarning( data );
      }
   }
}
