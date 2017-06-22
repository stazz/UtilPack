using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UtilPack.NuGet
{
   public class TextWriterLogger : global::NuGet.Common.ILogger
   {
      public static TextWriterLogger ConsoleLogger = new TextWriterLogger();

      private readonly TextWriterLoggerOptions _options;

      public TextWriterLogger( TextWriterLoggerOptions options = null )
      {
         this._options = options ?? new TextWriterLoggerOptions();
      }

      public void LogDebug( String data )
      {
         this._options.DebugWriter?.WriteLine( String.Format( this._options.DebugFormat ?? TextWriterLoggerOptions.DEBUG_STRING, data ) );
      }

      public void LogError( String data )
      {
         this._options.ErrorWriter?.WriteLine( String.Format( this._options.ErrorFormat ?? TextWriterLoggerOptions.ERROR_STRING, data ) );
      }

      public void LogErrorSummary( String data )
      {
         this._options.ErrorSummaryWriter?.WriteLine( String.Format( this._options.ErrorSummaryFormat ?? TextWriterLoggerOptions.INFO_SUMMARY_STRING, data ) );
      }

      public void LogInformation( String data )
      {
         this._options.InfoWriter?.WriteLine( String.Format( this._options.InfoFormat ?? TextWriterLoggerOptions.INFO_STRING, data ) );
      }

      public void LogInformationSummary( String data )
      {
         this._options.InfoSummaryWriter?.WriteLine( String.Format( this._options.InfoSummaryFormat ?? TextWriterLoggerOptions.INFO_SUMMARY_STRING, data ) );
      }

      public void LogMinimal( String data )
      {
         this._options.MinimalWriter?.WriteLine( String.Format( this._options.MinimalFormat ?? TextWriterLoggerOptions.MINIMAL_STRING, data ) );
      }

      public void LogVerbose( String data )
      {
         this._options.VerboseWriter?.WriteLine( String.Format( this._options.VerboseFormat ?? TextWriterLoggerOptions.VERBOSE_STRING, data ) );
      }

      public void LogWarning( String data )
      {
         this._options.WarningWriter?.WriteLine( String.Format( this._options.WarningFormat ?? TextWriterLoggerOptions.WARNING_STRING, data ) );
      }
   }

   // set writers to null to disable logging
   public class TextWriterLoggerOptions
   {
      public const String GLOBAL_PREFIX = "[NuGet ";
      public const String GLOBAL_SUFFIX = "]: {0}";
      public const String DEBUG_STRING = GLOBAL_PREFIX + "Debug" + GLOBAL_SUFFIX;
      public const String ERROR_STRING = GLOBAL_PREFIX + "Error" + GLOBAL_SUFFIX;
      public const String ERROR_SUMMARY_STRING = GLOBAL_PREFIX + "ErrorSummary" + GLOBAL_SUFFIX;
      public const String INFO_STRING = GLOBAL_PREFIX + "Info" + GLOBAL_SUFFIX;
      public const String INFO_SUMMARY_STRING = GLOBAL_PREFIX + "InfoSummary" + GLOBAL_SUFFIX;
      public const String MINIMAL_STRING = GLOBAL_PREFIX + "Minimal" + GLOBAL_SUFFIX;
      public const String VERBOSE_STRING = GLOBAL_PREFIX + "Verbose" + GLOBAL_SUFFIX;
      public const String WARNING_STRING = GLOBAL_PREFIX + "Warning" + GLOBAL_SUFFIX;

      public String DebugFormat { get; set; } = DEBUG_STRING;
      public String ErrorFormat { get; set; } = ERROR_STRING;
      public String ErrorSummaryFormat { get; set; } = ERROR_SUMMARY_STRING;
      public String InfoFormat { get; set; } = INFO_STRING;
      public String InfoSummaryFormat { get; set; } = INFO_SUMMARY_STRING;
      public String MinimalFormat { get; set; } = MINIMAL_STRING;
      public String VerboseFormat { get; set; } = VERBOSE_STRING;
      public String WarningFormat { get; set; } = WARNING_STRING;

      public TextWriter DebugWriter { get; set; } = Console.Out;
      public TextWriter ErrorWriter { get; set; } = Console.Error;
      public TextWriter ErrorSummaryWriter { get; set; } = Console.Error;
      public TextWriter InfoWriter { get; set; } = Console.Out;
      public TextWriter InfoSummaryWriter { get; set; } = Console.Out;
      public TextWriter MinimalWriter { get; set; } = Console.Out;
      public TextWriter VerboseWriter { get; set; } = Console.Out;
      public TextWriter WarningWriter { get; set; } = Console.Error;
   }
}
