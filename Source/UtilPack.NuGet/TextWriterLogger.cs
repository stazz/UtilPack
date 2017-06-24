using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UtilPack.NuGet
{
   /// <summary>
   /// This class implements <see cref="global::NuGet.Common.ILogger"/> using <see cref="TextWriter"/>s.
   /// </summary>
   /// <seealso cref="TextWriterLoggerOptions"/>
   public class TextWriterLogger : global::NuGet.Common.ILogger
   {

      private readonly TextWriterLoggerOptions _options;

      /// <summary>
      /// Creates a new instance of <see cref="TextWriterLogger"/> with given optional <see cref="TextWriterLoggerOptions"/>.
      /// </summary>
      /// <param name="options">The given <see cref="TextWriterLoggerOptions"/>. If not supplied, a new instance of <see cref="TextWriterLoggerOptions"/> will be created and the default values will be used.</param>
      public TextWriterLogger( TextWriterLoggerOptions options = null )
      {
         this._options = options ?? new TextWriterLoggerOptions();
      }

      /// <summary>
      /// This event will be invoked just before writing message in <see cref="LogDebug"/> method.
      /// </summary>
      /// <remarks>The message may be changed by the handler, see <see cref="LogMessageEventArgs"/>.</remarks>
      public event GenericEventHandler<LogMessageEventArgs> LogDebugEvent;

      /// <summary>
      /// This event will be invoked just before writing message in <see cref="LogError"/> method.
      /// </summary>
      /// <remarks>The message may be changed by the handler, see <see cref="LogMessageEventArgs"/>.</remarks>
      public event GenericEventHandler<LogMessageEventArgs> LogErrorEvent;

      /// <summary>
      /// This event will be invoked just before writing message in <see cref="LogErrorSummary"/> method.
      /// </summary>
      /// <remarks>The message may be changed by the handler, see <see cref="LogMessageEventArgs"/>.</remarks>
      public event GenericEventHandler<LogMessageEventArgs> LogErrorSummaryEvent;

      /// <summary>
      /// This event will be invoked just before writing message in <see cref="LogInformation"/> method.
      /// </summary>
      /// <remarks>The message may be changed by the handler, see <see cref="LogMessageEventArgs"/>.</remarks>
      public event GenericEventHandler<LogMessageEventArgs> LogInformationEvent;

      /// <summary>
      /// This event will be invoked just before writing message in <see cref="LogInformationSummary"/> method.
      /// </summary>
      /// <remarks>The message may be changed by the handler, see <see cref="LogMessageEventArgs"/>.</remarks>
      public event GenericEventHandler<LogMessageEventArgs> LogInformationSummaryEvent;

      /// <summary>
      /// This event will be invoked just before writing message in <see cref="LogMinimal"/> method.
      /// </summary>
      /// <remarks>The message may be changed by the handler, see <see cref="LogMessageEventArgs"/>.</remarks>
      public event GenericEventHandler<LogMessageEventArgs> LogMinimalEvent;

      /// <summary>
      /// This event will be invoked just before writing message in <see cref="LogVerbose"/> method.
      /// </summary>
      /// <remarks>The message may be changed by the handler, see <see cref="LogMessageEventArgs"/>.</remarks>
      public event GenericEventHandler<LogMessageEventArgs> LogVerboseEvent;

      /// <summary>
      /// This event will be invoked just before writing message in <see cref="LogWarning"/> method.
      /// </summary>
      /// <remarks>The message may be changed by the handler, see <see cref="LogMessageEventArgs"/>.</remarks>
      public event GenericEventHandler<LogMessageEventArgs> LogWarningEvent;

      /// <inheritdoc/>
      public void LogDebug( String data )
      {
         LogWithEvent(
            this._options.DebugWriter,
            String.Format( this._options.DebugFormat ?? TextWriterLoggerOptions.DEBUG_STRING, data ),
            this.LogDebugEvent
            );
      }

      /// <inheritdoc/>
      public void LogError( String data )
      {
         LogWithEvent(
            this._options.ErrorWriter,
            String.Format( this._options.ErrorFormat ?? TextWriterLoggerOptions.ERROR_STRING, data ),
            this.LogErrorEvent
            );
      }

      /// <inheritdoc/>
      public void LogErrorSummary( String data )
      {
         LogWithEvent(
            this._options.ErrorSummaryWriter,
            String.Format( this._options.ErrorSummaryFormat ?? TextWriterLoggerOptions.ERROR_SUMMARY_STRING, data ),
            this.LogErrorSummaryEvent
            );
      }

      /// <inheritdoc/>
      public void LogInformation( String data )
      {
         LogWithEvent(
            this._options.InfoWriter,
            String.Format( this._options.InfoFormat ?? TextWriterLoggerOptions.INFO_STRING, data ),
            this.LogInformationEvent
            );
      }

      /// <inheritdoc/>
      public void LogInformationSummary( String data )
      {
         LogWithEvent(
            this._options.InfoSummaryWriter,
            String.Format( this._options.InfoSummaryFormat ?? TextWriterLoggerOptions.INFO_SUMMARY_STRING, data ),
            this.LogInformationSummaryEvent
            );
      }

      /// <inheritdoc/>
      public void LogMinimal( String data )
      {
         LogWithEvent(
            this._options.MinimalWriter,
            String.Format( this._options.MinimalFormat ?? TextWriterLoggerOptions.MINIMAL_STRING, data ),
            this.LogMinimalEvent
            );
      }

      /// <inheritdoc/>
      public void LogVerbose( String data )
      {
         LogWithEvent(
            this._options.VerboseWriter,
            String.Format( this._options.VerboseFormat ?? TextWriterLoggerOptions.VERBOSE_STRING, data ),
            this.LogVerboseEvent
            );
      }

      /// <inheritdoc/>
      public void LogWarning( String data )
      {
         LogWithEvent(
            this._options.WarningWriter,
            String.Format( this._options.WarningFormat ?? TextWriterLoggerOptions.WARNING_STRING, data ),
            this.LogWarningEvent
            );
      }

      private static void LogWithEvent( TextWriter writer, String data, GenericEventHandler<LogMessageEventArgs> evt )
      {
         if ( writer != null )
         {
            if ( evt != null )
            {
               var args = new LogMessageEventArgs( data );
               evt( args );
               data = args.Message;
            }
            if ( !String.IsNullOrEmpty( data ) )
            {
               writer.WriteLine( data );
            }
         }
      }
   }

   /// <summary>
   /// This class provides a way to mutate a message in logging events of <see cref="TextWriterLogger"/>.
   /// </summary>
   public class LogMessageEventArgs
   {
      /// <summary>
      /// Creates a new instance of <see cref="LogMessageEventArgs"/> with given message.
      /// </summary>
      /// <param name="message">The message.</param>
      public LogMessageEventArgs( String message )
      {
         this.Message = message;
      }

      /// <summary>
      /// Gets or sets the message to log.
      /// </summary>
      /// <value>The message to log.</value>
      public String Message { get; set; }
   }

   /// <summary>
   /// This class encapsulates all options for <see cref="TextWriterLogger"/>.
   /// </summary>
   public class TextWriterLoggerOptions
   {
      private const String GLOBAL_PREFIX = "[NuGet ";
      private const String GLOBAL_SUFFIX = "]: {0}";
      internal const String DEBUG_STRING = GLOBAL_PREFIX + "Debug" + GLOBAL_SUFFIX;
      internal const String ERROR_STRING = GLOBAL_PREFIX + "Error" + GLOBAL_SUFFIX;
      internal const String ERROR_SUMMARY_STRING = GLOBAL_PREFIX + "ErrorSummary" + GLOBAL_SUFFIX;
      internal const String INFO_STRING = GLOBAL_PREFIX + "Info" + GLOBAL_SUFFIX;
      internal const String INFO_SUMMARY_STRING = GLOBAL_PREFIX + "InfoSummary" + GLOBAL_SUFFIX;
      internal const String MINIMAL_STRING = GLOBAL_PREFIX + "Minimal" + GLOBAL_SUFFIX;
      internal const String VERBOSE_STRING = GLOBAL_PREFIX + "Verbose" + GLOBAL_SUFFIX;
      internal const String WARNING_STRING = GLOBAL_PREFIX + "Warning" + GLOBAL_SUFFIX;

      /// <summary>
      /// Gets or sets the format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogDebug"/> method.
      /// The first argument for the format string is the message to be logged.
      /// </summary>
      /// <value>The format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogDebug"/> method.</value>
      public String DebugFormat { get; set; } = DEBUG_STRING;

      /// <summary>
      /// Gets or sets the format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogError"/> method.
      /// The first argument for the format string is the message to be logged.
      /// </summary>
      /// <value>The format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogError"/> method.</value>
      public String ErrorFormat { get; set; } = ERROR_STRING;

      /// <summary>
      /// Gets or sets the format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogErrorSummary"/> method.
      /// The first argument for the format string is the message to be logged.
      /// </summary>
      /// <value>The format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogErrorSummary"/> method.</value>
      public String ErrorSummaryFormat { get; set; } = ERROR_SUMMARY_STRING;

      /// <summary>
      /// Gets or sets the format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogInformation"/> method.
      /// The first argument for the format string is the message to be logged.
      /// </summary>
      /// <value>The format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogInformation"/> method.</value>
      public String InfoFormat { get; set; } = INFO_STRING;

      /// <summary>
      /// Gets or sets the format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogInformationSummary"/> method.
      /// The first argument for the format string is the message to be logged.
      /// </summary>
      /// <value>The format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogInformationSummary"/> method.</value>
      public String InfoSummaryFormat { get; set; } = INFO_SUMMARY_STRING;

      /// <summary>
      /// Gets or sets the format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogMinimal"/> method.
      /// The first argument for the format string is the message to be logged.
      /// </summary>
      /// <value>The format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogMinimal"/> method.</value>
      public String MinimalFormat { get; set; } = MINIMAL_STRING;

      /// <summary>
      /// Gets or sets the format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogVerbose"/> method.
      /// The first argument for the format string is the message to be logged.
      /// </summary>
      /// <value>The format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogVerbose"/> method.</value>
      public String VerboseFormat { get; set; } = VERBOSE_STRING;

      /// <summary>
      /// Gets or sets the format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogWarning"/> method.
      /// The first argument for the format string is the message to be logged.
      /// </summary>
      /// <value>The format string for messages logged using <see cref="global::NuGet.Common.ILogger.LogWarning"/> method.</value>
      public String WarningFormat { get; set; } = WARNING_STRING;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogDebug"/> method.
      /// By default, this is the <see cref="Console.Out"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="global::NuGet.Common.ILogger.LogDebug"/> method
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogDebug"/> method.</value>
      public TextWriter DebugWriter { get; set; } = Console.Out;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogError"/> method.
      /// By default, this is the <see cref="Console.Error"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="global::NuGet.Common.ILogger.LogError"/> method
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogError"/> method.</value>
      public TextWriter ErrorWriter { get; set; } = Console.Error;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogErrorSummary"/> method.
      /// By default, this is the <see cref="Console.Error"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="global::NuGet.Common.ILogger.LogErrorSummary"/> method
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogErrorSummary"/> method.</value>
      public TextWriter ErrorSummaryWriter { get; set; } = Console.Error;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogInformation"/> method.
      /// By default, this is the <see cref="Console.Out"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="global::NuGet.Common.ILogger.LogInformation"/> method
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogInformation"/> method.</value>
      public TextWriter InfoWriter { get; set; } = Console.Out;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogInformationSummary"/> method.
      /// By default, this is the <see cref="Console.Out"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="global::NuGet.Common.ILogger.LogInformationSummary"/> method
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogInformationSummary"/> method.</value>
      public TextWriter InfoSummaryWriter { get; set; } = Console.Out;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogMinimal"/> method.
      /// By default, this is the <see cref="Console.Out"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="global::NuGet.Common.ILogger.LogMinimal"/> method
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogMinimal"/> method.</value>
      public TextWriter MinimalWriter { get; set; } = Console.Out;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogVerbose"/> method.
      /// By default, this is the <see cref="Console.Out"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="global::NuGet.Common.ILogger.LogVerbose"/> method
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogVerbose"/> method.</value>
      public TextWriter VerboseWriter { get; set; } = Console.Out;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogWarning"/> method.
      /// By default, this is the <see cref="Console.Error"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="global::NuGet.Common.ILogger.LogWarning"/> method
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="global::NuGet.Common.ILogger.LogWarning"/> method.</value>
      public TextWriter WarningWriter { get; set; } = Console.Error;
   }
}
