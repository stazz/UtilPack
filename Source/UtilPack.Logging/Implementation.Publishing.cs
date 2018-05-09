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
using UtilPack.Logging.Consume;

namespace UtilPack.Logging.Publish
{
   internal sealed class DynamicLogger<TMetaData> : LogPublisher<TMetaData>
   {
      private readonly Func<TMetaData, String, Object[], Boolean, Task> _handler;

      public DynamicLogger( Func<TMetaData, String, Object[], Boolean, Task> handler )
      {
         this._handler = ArgumentValidator.ValidateNotNull( nameof( handler ), handler );
      }

      public void Publish( TMetaData metadata, String messageFormat, params Object[] messageParameters )
      {
         this._handler( metadata, messageFormat, messageParameters, false );
      }

      public Task PublishAsync( TMetaData metadata, String messageFormat, params Object[] messageParameters )
      {
         return this._handler( metadata, messageFormat, messageParameters, true );
      }
   }

   internal sealed class NullLogger<TMetaData> : LogPublisher<TMetaData>
   {
      public static NullLogger<TMetaData> Instance = new NullLogger<TMetaData>();

      private NullLogger()
      {

      }

      public void Publish( TMetaData metadata, String messageFormat, params Object[] messageParameters )
      {
         // Nothing to do
      }

      public Task PublishAsync( TMetaData metadata, String messageFormat, params Object[] messageParameters )
      {
         // Nothing to do
         return TaskHolder.CompletedTask;
      }
   }

   internal sealed class SingleLogger<TMetaData> : LogPublisher<TMetaData>
   {
      private readonly Func<LogEvent<TMetaData>, Task> _handler;

      public SingleLogger(
         Func<LogEvent<TMetaData>, Task> handler
         )
      {
         this._handler = ArgumentValidator.ValidateNotNull( nameof( handler ), handler );
      }

      public void Publish( TMetaData metadata, String messageFormat, params Object[] messageParameters )
      {
         try
         {
            this._handler( new LogEventImpl<TMetaData>( metadata, messageFormat, messageParameters ) );
         }
         catch
         {
            // Ignore
         }
      }

      public Task PublishAsync( TMetaData metadata, String messageFormat, params Object[] messageParameters )
      {
         Task task;
         try
         {
            task = this._handler( new LogEventImpl<TMetaData>( metadata, messageFormat, messageParameters ) );
         }
         catch
         {
            // Ignore
            task = null;
         }

         return task ?? TaskHolder.CompletedTask;
      }
   }

   internal sealed class CombiningLogger<TMetaData> : LogPublisher<TMetaData>
   {
      private readonly Func<LogEvent<TMetaData>, Task>[] _handlers;

      public CombiningLogger(
         Func<LogEvent<TMetaData>, Task>[] handlers
         )
      {
         this._handlers = ArgumentValidator.ValidateNotEmpty( nameof( handlers ), handlers );
      }

      public void Publish( TMetaData metadata, String messageFormat, params Object[] messageParameters )
      {
         var evt = new LogEventImpl<TMetaData>( metadata, messageFormat, messageParameters );
         foreach ( var handler in this._handlers )
         {
            try
            {
               handler( evt );
            }
            catch
            {
               // Ignore
            }
         }
      }

      public Task PublishAsync( TMetaData metadata, String messageFormat, params Object[] messageParameters )
      {
         var tasks = new List<Task>();
         var evt = new LogEventImpl<TMetaData>( metadata, messageFormat, messageParameters );
         foreach ( var handler in this._handlers )
         {
            Task task;
            try
            {
               task = handler( evt );
            }
            catch
            {
               // Ignore
               task = null;
            }
            if ( task != null )
            {
               tasks.Add( task );
            }
         }

         return tasks.Count > 0 ? Task.WhenAll( tasks ) : TaskHolder.CompletedTask;
      }
   }


}
