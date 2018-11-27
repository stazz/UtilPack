/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
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

namespace UtilPack.Logging.Consume
{
   /// <summary>
   /// This class implements <see cref="LogConsumerFactory{TMetaData}"/> which will choose which <see cref="LogConsumer{TMetaData}"/> it will use on each log event.
   /// The choosing logic is based on given multiple <see cref="LogConsumer{TMetaData}"/> instances, each associated by <typeparamref name="TMetaData"/>.
   /// </summary>
   /// <typeparam name="TMetaData">The type that captures metadata about messages.</typeparam>
   public class ChoosingLoggerFactory<TMetaData> : LogConsumerFactory<TMetaData>
   {
      private readonly LogConsumer<TMetaData> _consumer;

      /// <summary>
      /// Creates new instance of <see cref="ChoosingLoggerFactory{TMetaData}"/>, with each <see cref="LogConsumer{TMetaData}"/> associated to a specified <typeparamref name="TMetaData"/>.
      /// </summary>
      /// <param name="loggers">The dictionary holding the <typeparamref name="TMetaData"/>-to-<see cref="LogConsumer{TMetaData}"/> associations.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="loggers"/> is <c>null</c>.</exception>
      /// <remarks>
      /// If the dictionary is changed after creation of instance of this class, the changes will be reflected to the behaviour of the instance.
      /// </remarks>
      public ChoosingLoggerFactory( IDictionary<TMetaData, LogConsumer<TMetaData>> loggers )
      {
         ArgumentValidator.ValidateNotNull( nameof( loggers ), loggers );
         this._consumer = new DelegatingLogConsumer<TMetaData>( args =>
         {
            return loggers.TryGetValue( args.MetaData, out var logger ) ?
               logger?.ConsumeLogEventAsync( args ) :
               null;
         } );
      }

      /// <inheritdoc />
      public LogConsumer<TMetaData> CreateLogConsumer()
      {
         return this._consumer;
      }

   }

   /// <summary>
   /// This class implements <see cref="LogConsumer{TMetaData}"/> which will delegate its <see cref="LogConsumer{TMetaData}.ConsumeLogEventAsync"/> method to the delegate callback specified in constructor.
   /// </summary>
   /// <typeparam name="TMetaData">The type that captures metadata about messages.</typeparam>
   public sealed class DelegatingLogConsumer<TMetaData> : LogConsumer<TMetaData>
   {
      private readonly Func<LogEvent<TMetaData>, Task> _callback;

      /// <summary>
      /// Creates a new instance of <see cref="DelegatingLogConsumer{TMetaData}"/> with given delegate callback for <see cref="LogConsumer{TMetaData}.ConsumeLogEventAsync"/> implementation
      /// </summary>
      /// <param name="callback">The callback delegate for <see cref="LogConsumer{TMetaData}.ConsumeLogEventAsync"/> method.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="callback"/> is <c>null</c>.</exception>
      public DelegatingLogConsumer(
         Func<LogEvent<TMetaData>, Task> callback
         )
      {
         this._callback = ArgumentValidator.ValidateNotNull( nameof( callback ), callback );
      }

      /// <inheritdoc />
      public Task ConsumeLogEventAsync( LogEvent<TMetaData> args )
      {
         return this._callback( args ) ?? TaskHolder.CompletedTask;
      }
   }

   /// <summary>
   /// This class implements <see cref="LogConsumer{TMetaData}"/> and provides logging to a <see cref="TextWriter"/> specified to constructor.
   /// </summary>
   /// <typeparam name="TMetaData">The type that captures metadata about messages.</typeparam>
   public sealed class TextWriterLogger<TMetaData> : LogConsumer<TMetaData>
   {
      private readonly Func<LogEvent<TMetaData>, Task> _logAction;

      /// <summary>
      /// Creates a new instance of <see cref="TextWriterLogger{TMetaData}"/> and binds it to given <see cref="TextWriter"/>.
      /// </summary>
      /// <param name="writer">The <see cref="TextWriter"/>.</param>
      /// <param name="writeNewLine">Whether to use <see cref="TextWriter.WriteLineAsync(String)"/>. If <c>false</c>, the <see cref="TextWriter.WriteAsync(String)"/> will be used.</param>
      /// <param name="messageGetter">Optional callback to extract the textual message. By default, will use <see cref="LogEvent{TMetaData}.Message"/> directly.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="writer"/> is <c>null</c>.</exception>
      public TextWriterLogger(
         TextWriter writer,
         Boolean writeNewLine = true,
         Func<LogEvent<TMetaData>, String> messageGetter = null
         )
      {
         ArgumentValidator.ValidateNotNull( nameof( writer ), writer );

         if ( messageGetter == null )
         {
            messageGetter = evt => evt.Message;
         }

         this._logAction = writeNewLine ?
            args => writer.WriteLineAsync( messageGetter( args ) ) :
            new Func<LogEvent<TMetaData>, Task>( args => writer.WriteAsync( messageGetter( args ) ) );
      }

      /// <inheritdoc />
      public Task ConsumeLogEventAsync( LogEvent<TMetaData> args )
      {
         // TODO flushing.
         return this._logAction( args );
      }

   }

#if !NETSTANDARD1_0
   // Not sealed so that subclasses can add some kind of configuration of their own, and also to bind TMetaData

   /// <summary>
   /// This class extends <see cref="ChoosingLoggerFactory{TMetaData}"/> in order to provide functionality to create <see cref="LogConsumer{TMetaData}"/> instances which will log to <see cref="Console.Error"/> or <see cref="Console.Out"/>, based on value of <typeparamref name="TMetaData"/>.
   /// </summary>
   /// <typeparam name="TMetaData">The type that captures metadata about messages.</typeparam>
   public class ConsoleLoggerFactory<TMetaData> : ChoosingLoggerFactory<TMetaData>
   {
      /// <summary>
      /// Creates a new instance of <see cref="ConsoleLoggerFactory{TMetaData}"/> with given parameters.
      /// </summary>
      /// <param name="errorMDs">The <typeparamref name="TMetaData"/> instances which will signal to log to <see cref="Console.Error"/>.</param>
      /// <param name="notErrorMDs">The <typeparamref name="TMetaData"/> instances which will signal to log to <see cref="Console.Out"/>.</param>
      /// <param name="writeErrorNewLine">Whether to write newline when writing to <see cref="Console.Error"/>.</param>
      /// <param name="writeNotErrorNewLine">Whether to write newline when writing to <see cref="Console.Out"/>.</param>
      public ConsoleLoggerFactory(
         IEnumerable<TMetaData> errorMDs,
         IEnumerable<TMetaData> notErrorMDs,
         Boolean writeErrorNewLine,
         Boolean writeNotErrorNewLine
         )
         : base( CreateLogConsumers( errorMDs, notErrorMDs, writeErrorNewLine, writeNotErrorNewLine ) )
      {
      }

      private static IDictionary<TMetaData, LogConsumer<TMetaData>> CreateLogConsumers(
         IEnumerable<TMetaData> errorMDs,
         IEnumerable<TMetaData> notErrorMDs,
         Boolean writeErrorNewLine,
         Boolean writeNotErrorNewLine
         )
      {
         var errLogger = new Lazy<LogConsumer<TMetaData>>( () => new TextWriterLogger<TMetaData>( Console.Error, writeErrorNewLine ), LazyThreadSafetyMode.None );
         var outLogger = new Lazy<LogConsumer<TMetaData>>( () => new TextWriterLogger<TMetaData>( Console.Out, writeNotErrorNewLine ), LazyThreadSafetyMode.None );
         return errorMDs.Select( md => new KeyValuePair<TMetaData, Lazy<LogConsumer<TMetaData>>>( md, errLogger ) )
            .Concat( notErrorMDs.Select( md => new KeyValuePair<TMetaData, Lazy<LogConsumer<TMetaData>>>( md, outLogger ) ) )
            .ToDictionary( kvp => kvp.Key, kvp => kvp.Value.Value );
      }
   }
#endif
}
