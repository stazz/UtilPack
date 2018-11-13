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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UtilPack;

namespace NuGetUtils.Lib.Common
{
   /// <summary>
   /// This class implements <see cref="ILogger"/> using <see cref="TextWriter"/>s.
   /// </summary>
   /// <seealso cref="TextWriterLoggerOptions"/>
   public class TextWriterLogger : LoggerBase
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
      /// This event will be invoked just before writing log message.
      /// </summary>
      /// <remarks>The message may be changed by the handler, see <see cref="LogMessageEventArgs"/>.</remarks>
      public event GenericEventHandler<LogMessageEventArgs> LogEvent;

      /// <inheritdoc/>
      public override void Log( ILogMessage message )
      {
         // For some reason, we get here even when this.DisplayMessage returns false - this should not happen at least according to the code of LoggerBase...
         if ( message != null && this.DisplayMessage( message.Level ) )
         {
            var writer = this.GetWriter( message );
            if ( writer != null )
            {
               message = InvokeEvent( message, this.LogEvent );
               if ( message != null )
               {
                  writer.WriteLine( message.Message );
               }
            }
         }
      }

      /// <inheritdoc/>
      public override async Task LogAsync( ILogMessage message )
      {
         if ( message != null && this.DisplayMessage( message.Level ) )
         {
            var writer = this.GetWriter( message );
            if ( writer != null )
            {
               message = InvokeEvent( message, this.LogEvent );
               if ( message != null )
               {
                  await writer.WriteLineAsync( message.Message );
               }
            }
         }
      }

      private TextWriter GetWriter( ILogMessage msg )
      {
         TextWriter retVal = null;
         TextWriterLoggerOptions options;
         if (
            msg != null
            && ( options = this._options ) != null
            )
         {
            // TODO dictionary to options
            switch ( msg.Level )
            {
               case LogLevel.Debug:
                  retVal = options.DebugWriter;
                  break;
               case LogLevel.Verbose:
                  retVal = options.VerboseWriter;
                  break;
               case LogLevel.Information:
                  retVal = options.InfoWriter;
                  break;
               case LogLevel.Minimal:
                  retVal = options.MinimalWriter;
                  break;
               case LogLevel.Warning:
                  retVal = options.WarningWriter;
                  break;
               case LogLevel.Error:
                  retVal = options.ErrorWriter;
                  break;
            }
         }

         return retVal;
      }

      private static ILogMessage InvokeEvent(
         ILogMessage msg,
         GenericEventHandler<LogMessageEventArgs> evt
         )
      {
         if ( evt != null )
         {
            var args = new LogMessageEventArgs( msg );
            try
            {
               evt( args );
            }
            catch
            {
               // Ignore
            }
            msg = args.Message;
         }


         return msg;
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
      public LogMessageEventArgs( ILogMessage message )
      {
         this.Message = message;
      }

      /// <summary>
      /// Gets or sets the message to log.
      /// </summary>
      /// <value>The message to log.</value>
      public ILogMessage Message { get; }
   }

   /// <summary>
   /// This class encapsulates all options for <see cref="TextWriterLogger"/>.
   /// </summary>
   public class TextWriterLoggerOptions
   {
      //private const String DEFAULT_FORMAT = "[NuGet {0}]: {1}";

      ///// <summary>
      ///// Gets or sets the format string for messages logged.
      ///// The arguments for format string are the following, in that order: <see cref="ILogMessage.Level"/>, <see cref="ILogMessage.Message"/>, and <see cref="ILogMessage"/>.
      ///// </summary>
      ///// <value>The format string for messages logged.</value>
      //public String Format { get; set; } = DEFAULT_FORMAT;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="LogLevel.Debug"/>
      /// By default, this is the <see cref="Console.Out"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="LogLevel.Debug"/>.
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="LogLevel.Debug"/>.</value>
      public TextWriter DebugWriter { get; set; } = Console.Out;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="LogLevel.Error"/>.
      /// By default, this is the <see cref="Console.Error"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="LogLevel.Error"/>.
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="LogLevel.Error"/>.</value>
      public TextWriter ErrorWriter { get; set; } = Console.Error;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="LogLevel.Information"/>.
      /// By default, this is the <see cref="Console.Out"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="LogLevel.Information"/>.
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="LogLevel.Information"/>.</value>
      public TextWriter InfoWriter { get; set; } = Console.Out;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="LogLevel.Minimal"/>.
      /// By default, this is the <see cref="Console.Out"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="LogLevel.Minimal"/>.
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="LogLevel.Minimal"/>.</value>
      public TextWriter MinimalWriter { get; set; } = Console.Out;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="LogLevel.Verbose"/>.
      /// By default, this is the <see cref="Console.Out"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="LogLevel.Verbose"/>.
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="LogLevel.Verbose"/>.</value>
      public TextWriter VerboseWriter { get; set; } = Console.Out;

      /// <summary>
      /// Gets or sets the <see cref="TextWriter"/> for <see cref="LogLevel.Warning"/>.
      /// By default, this is the <see cref="Console.Error"/>.
      /// Set to <c>null</c> to disable logging done via <see cref="LogLevel.Warning"/>.
      /// </summary>
      /// <value>The <see cref="TextWriter"/> for <see cref="LogLevel.Warning"/>.</value>
      public TextWriter WarningWriter { get; set; } = Console.Error;
   }
}
