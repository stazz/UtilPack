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
using System.Threading.Tasks;
using UtilPack;
using UtilPack.Logging;
using UtilPack.Logging.Bootstrap;
using UtilPack.Logging.Consume;
using UtilPack.Logging.Publish;



namespace UtilPack.Logging
{
   namespace Bootstrap
   {
      /// <summary>
      /// This class is designed to help with bootstrap process of setting up the <see cref="LogConsumer{TMetaData}"/> instances, and then creating <see cref="LogPublisher{TMetaData}"/> instance to pass around to any application component, which can perform logging.
      /// </summary>
      /// <typeparam name="TMetaData">The type that captures metadata about messages. Typically this is enumeration about log level, e.g. information/warning/error.</typeparam>
      public sealed class LogRegistration<TMetaData>
      {

         /// <summary>
         /// Creates a new instance of <see cref="LogRegistration{TMetaData}"/>.
         /// </summary>
         public LogRegistration()
         {
            this.DynamicLogger = new DynamicLogger<TMetaData>( ( metadata, messageFormat, messageParameters, isAsync ) =>
            {
               return this.LogEvent?.InvokeAllEventHandlers( new LogEventImpl<TMetaData>( metadata, messageFormat, messageParameters ), isAsync ? new List<Task>() : null ) ?? TaskHolder.CompletedTask;
            } );
         }

         // We use event in order to easily have the immutable list of delegates on single access.
         // TODO refactor this into a LogConsumer<TMetaData>[]
         private event Func<LogEvent<TMetaData>, Task> LogEvent;

         /// <summary>
         /// Registers the given <see cref="LogConsumer{TMetaData}"/> instances to this <see cref="LogRegistration{TMetaData}"/>.
         /// This affects how <see cref="DynamicLogger"/> behaves and the return value of <see cref="CreateHandlerFromCurrentRegistrations"/>.
         /// </summary>
         /// <param name="consumers">The <see cref="LogConsumer{TMetaData}"/> instances to register. Can be <c>null</c> or empty, and can contain <c>null</c> elements.</param>
         public void RegisterLoggers( IEnumerable<LogConsumer<TMetaData>> consumers )
         {
            if ( consumers != null )
            {
               foreach ( var consumer in consumers.Where( c => c != null ) )
               {
                  this.LogEvent += consumer.ConsumeLogEventAsync;
               }
            }
         }

         /// <summary>
         /// Gets an instance of <see cref="LogPublisher{TMetaData}"/> which will always check the current registration state of this <see cref="LogRegistration{TMetaData}"/> on every log publish call.
         /// </summary>
         /// <value>An instance of <see cref="LogPublisher{TMetaData}"/> which will always check the current registration state of this <see cref="LogRegistration{TMetaData}"/> on every log publish call.</value>
         public LogPublisher<TMetaData> DynamicLogger { get; }

         /// <summary>
         /// Creates an instance of <see cref="LogPublisher{TMetaData}"/> based on current registrations done via <see cref="RegisterLoggers"/> method.
         /// Subsequent modifications via <see cref="RegisterLoggers"/> method will not affect the <see cref="LogPublisher{TMetaData}"/> returned by this method.
         /// </summary>
         /// <returns>An instance of <see cref="LogPublisher{TMetaData}"/> that will use current registrations, but not subsequent ones.</returns>
         public LogPublisher<TMetaData> CreateHandlerFromCurrentRegistrations()
         {
            var handlers = this.LogEvent?.GetInvocationList();
            var len = handlers?.Length ?? 0;
            LogPublisher<TMetaData> retVal;
            if ( len > 0 )
            {
               retVal = len == 1 ?
                  (LogPublisher<TMetaData>) new SingleLogger<TMetaData>( (Func<LogEvent<TMetaData>, Task>) handlers[0] ) :
                  new CombiningLogger<TMetaData>( handlers.Cast<Func<LogEvent<TMetaData>, Task>>().ToArray() );
            }
            else
            {
               retVal = NullLogger<TMetaData>.Instance;
            }
            return retVal;
         }

      }
   }

   internal sealed class TaskHolder
   {
      public static Task CompletedTask { get; } = Task.FromResult( false );

   }

   internal static class InternalExtensions
   {
      public static Task InvokeAllEventHandlers<TArgs>( this Func<TArgs, Task> evt, TArgs args, List<Task> tasks )
      {
         foreach ( var handler in evt.GetInvocationList() )
         {
            Task task;
            try
            {
               task = ( (Func<TArgs, Task>) handler )?.Invoke( args );
            }
            catch
            {
               task = null;
            }
            if ( task != null )
            {
               tasks?.Add( task );
            }
         }

         return ( tasks?.Count ?? 0 ) > 0 ? Task.WhenAll( tasks ) : TaskHolder.CompletedTask;
      }
   }
}


/// <summary>
/// Contains extension method for types defined in this assembly.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// Helper method to register a single <see cref="LogConsumer{TMetaData}"/> returned by given <see cref="LogConsumerFactory{TMetaData}"/> to this <see cref="LogRegistration{TMetaData}"/>.
   /// </summary>
   /// <typeparam name="TMetaData">The type that captures metadata about messages.</typeparam>
   /// <param name="registration">This <see cref="LogRegistration{TMetaData}"/>.</param>
   /// <param name="consumerFactory">The single <see cref="LogConsumerFactory{TMetaData}"/>.</param>
   /// <exception cref="NullReferenceException">If this <see cref="LogRegistration{TMetaData}"/> is <c>null</c>.</exception>
   public static void RegisterLogger<TMetaData>( this LogRegistration<TMetaData> registration, LogConsumerFactory<TMetaData> consumerFactory )
   {
      if ( consumerFactory != null )
      {
         registration.RegisterLoggers( new LogConsumer<TMetaData>[] { consumerFactory.CreateLogConsumer() } );
      }
   }

   /// <summary>
   /// Helper method to register multiple <see cref="LogConsumer{TMetaData}"/> instances returned by given <see cref="LogConsumerFactory{TMetaData}"/> instances to this <see cref="LogRegistration{TMetaData}"/>.
   /// </summary>
   /// <typeparam name="TMetaData">The type that captures metadata about messages.</typeparam>
   /// <param name="registration">This <see cref="LogRegistration{TMetaData}"/>.</param>
   /// <param name="factories">The <see cref="LogConsumerFactory{TMetaData}"/> instances.</param>
   /// <exception cref="NullReferenceException">If this <see cref="LogRegistration{TMetaData}"/> is <c>null</c>.</exception>
   public static void RegisterLoggers<TMetaData>( this LogRegistration<TMetaData> registration, IEnumerable<LogConsumerFactory<TMetaData>> factories )
   {
      registration.RegisterLoggers( factories.Select( f => f.CreateLogConsumer() ) );
   }

   /// <summary>
   /// Helper method to register a single given <see cref="LogConsumer{TMetaData}"/> to this <see cref="LogRegistration{TMetaData}"/>.
   /// </summary>
   /// <typeparam name="TMetaData">The type that captures metadata about messages.</typeparam>
   /// <param name="registration">This <see cref="LogRegistration{TMetaData}"/>.</param>
   /// <param name="consumer">The single <see cref="LogConsumer{TMetaData}"/>.</param>
   /// <exception cref="NullReferenceException">If this <see cref="LogRegistration{TMetaData}"/> is <c>null</c>.</exception>
   public static void RegisterLogger<TMetaData>( this LogRegistration<TMetaData> registration, LogConsumer<TMetaData> consumer )
   {
      if ( consumer != null )
      {
         registration.RegisterLoggers( new LogConsumer<TMetaData>[] { consumer } );
      }
   }
}