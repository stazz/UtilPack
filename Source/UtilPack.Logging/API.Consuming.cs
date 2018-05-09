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
using System.Text;
using System.Threading.Tasks;

namespace UtilPack.Logging.Consume
{
   /// <summary>
   /// This interface is not directly used by this library but it is useful when the actual <see cref="LogConsumer{TMetaData}"/> should be created based on external configuration.
   /// </summary>
   /// <typeparam name="TMetaData">The type of logging metadata.</typeparam>
   /// <seealso cref="Bootstrap.LogRegistration{TMetaData}"/>
   /// <seealso cref="LogConsumer{TMetaData}"/>
   public interface LogConsumerFactory<in TMetaData>
   {
      /// <summary>
      /// Creates an instance of <see cref="LogConsumer{TMetaData}"/> based on the internal configuration and state of this <see cref="LogConsumerFactory{TMetaData}"/>.
      /// </summary>
      /// <returns>A new instance of <see cref="LogConsumer{TMetaData}"/>.</returns>
      LogConsumer<TMetaData> CreateLogConsumer();
   }

   /// <summary>
   /// This interface is used by <see cref="Bootstrap.LogRegistration{TMetaData}.RegisterLoggers"/> during bootstrap phase when setting up loggers e.g. from configuration.
   /// This interface represents the actual action that is done when log message is published by <see cref="Publish.LogPublisher{TMetaData}"/>.
   /// This action could be e.g. writing to a file, writing to a database, or something else.
   /// In most cases, this is asynchronous action, that is why the return value of <see cref="ConsumeLogEventAsync"/> is <see cref="Task"/>.
   /// Synchronous actions can return <c>null</c> or pre-cached completed tasks.
   /// </summary>
   /// <typeparam name="TMetaData">The type of metadata about the log event.</typeparam>
   /// <seealso cref="LogEvent{TMetaData}"/>
   /// <seealso cref="LogConsumerFactory{TMetaData}"/>
   /// <seealso cref="Publish.LogPublisher{TMetaData}"/>
   public interface LogConsumer<in TMetaData>
   {
      /// <summary>
      /// Asynchronously consumes the <see cref="LogEvent{TMetaData}"/>.
      /// Whether the returned task will be awaited, will depend on how the original <see cref="Publish.LogPublisher{TMetaData}"/> was called.
      /// </summary>
      /// <param name="args">The <see cref="LogEvent{TMetaData}"/>.</param>
      /// <returns>The task to potentially await for, or <c>null</c> or pre-cached completed task, if this action is actually synchronous.</returns>
      /// <remarks>
      /// Any exception thrown by this method will be ignored, if <see cref="Bootstrap.LogRegistration{TMetaData}"/> was used for initial logging infrastructure setup.
      /// </remarks>
      Task ConsumeLogEventAsync( LogEvent<TMetaData> args );
   }

   /// <summary>
   /// This interface captures information about a single logging event.
   /// </summary>
   /// <typeparam name="TMetaData">The type of metadata associated with this logging event.</typeparam>
   /// <seealso cref="Publish.LogPublisher{TMetaData}.Publish(TMetaData, string, object[])"/>
   /// <seealso cref="LogConsumer{TMetaData}.ConsumeLogEventAsync(LogEvent{TMetaData})"/>
   public interface LogEvent<out TMetaData>
   {
      /// <summary>
      /// Gets the metadata associated with this logging event.
      /// </summary>
      /// <value>The metadata associated with this logging event.</value>
      TMetaData MetaData { get; }

      /// <summary>
      /// Gets the full message of this log event.
      /// </summary>
      /// <value>The full message of this log event.</value>
      String Message { get; }

      /// <summary>
      /// Gets the time when this <see cref="LogEvent{TMetaData}"/> was created.
      /// </summary>
      /// <value>The time when this <see cref="LogEvent{TMetaData}"/> was created.</value>
      /// <remarks>
      /// The returned time is in UTC "time zone".
      /// </remarks>
      DateTime CreationTimeUTC { get; }

   }

   internal sealed class LogEventImpl<TMetaData> : LogEvent<TMetaData>
   {
      private readonly Lazy<String> _message;
    
      public LogEventImpl(
         TMetaData metadata,
         String messageFormat,
         Object[] messageParameters
         )
      {
         this.CreationTimeUTC = DateTime.UtcNow;
         this.MetaData = metadata;
         this._message = new Lazy<String>( () => String.Format( messageFormat, messageParameters ), System.Threading.LazyThreadSafetyMode.None );
      }

      public TMetaData MetaData { get; }

      public String Message => this._message.Value;

      public DateTime CreationTimeUTC { get; }

   }
}
