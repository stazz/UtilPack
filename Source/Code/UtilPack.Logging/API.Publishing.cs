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
using System.Threading.Tasks;

namespace UtilPack.Logging.Publish
{
   /// <summary>
   /// This interface is used by the developer code in order to log some message with message metadata.
   /// By using this interface, the exact logging process can be abstract away in such way that the caller should care only about one thing: whether to await for the potentially asynchronous logging action to complete, or not.
   /// </summary>
   /// <typeparam name="TMetaData">The type that captures metadata about messages. Typically this is enumeration about log level, e.g. information/warning/error.</typeparam>
   /// <remarks>
   /// Typically, <see cref="Bootstrap.LogRegistration{TMetaData}"/> is used by bootstrapping portion of application to set up how logging is performed.
   /// Then, the <see cref="Bootstrap.LogRegistration{TMetaData}.CreatePublisherFromCurrentRegistrations"/> is called to create instance of this interface, and is free to pass around to whatever code needs to perform logging.
   /// That instance will delegate calls to <see cref="Publish"/> and <see cref="PublishAsync"/> to underlying <see cref="Consume.LogConsumer{TMetaData}"/> instances, and they will perform the actual logging.
   /// </remarks>
   public interface LogPublisher<in TMetaData>
   {
      /// <summary>
      /// Publishes log message without awaiting for the underlying <see cref="Consume.LogConsumer{TMetaData}"/> instances to finish their potentially asynchronous action of writing the log information.
      /// This can be useful for low-importance log messages, or when performance is greater concern.
      /// </summary>
      /// <param name="metadata">The metadata about the log message being published.</param>
      /// <param name="messageFormat">The format string about the log message being published, in same format as <see cref="string.Format(string, object[])"/> expects it.</param>
      /// <param name="messageParameters">The arguments that will be used when creating final log message from <paramref name="messageFormat"/>.</param>
      /// <remarks>
      /// If instance of this interface is acquired via <see cref="Bootstrap.LogRegistration{TMetaData}"/>, then it will be as close to nothrow-method as possible, excluding the OOM etc problematic scenarios.
      /// </remarks>
      void Publish( TMetaData metadata, String messageFormat, params Object[] messageParameters );

      /// <summary>
      /// Publishes log message, and returns task which will complete when all of the underlying <see cref="Consume.LogConsumer{TMetaData}"/> instances have finished their potentially asynchronous action of writing the log information.
      /// This is useful for high-importance log messages, or when performance is not as critical as logging.
      /// </summary>
      /// <param name="metadata">The metadata about the log message being published.</param>
      /// <param name="messageFormat">The format string about the log message being published, in same format as <see cref="string.Format(string, object[])"/> expects it.</param>
      /// <param name="messageParameters">The arguments that will be used when creating final log message from <paramref name="messageFormat"/>.</param>
      /// <returns>A task which will complete when all of the underlying <see cref="Consume.LogConsumer{TMetaData}"/> instance have finished their potentially asynchronous action of writing the log information.</returns>
      /// <remarks>
      /// If instance of this interface is acquired via <see cref="Bootstrap.LogRegistration{TMetaData}"/>, then it will be as close to nothrow-method as possible, excluding the OOM etc problematic scenarios, and also this method will never return <c>null</c> in such case either.
      /// </remarks>
      Task PublishAsync( TMetaData metadata, String messageFormat, params Object[] messageParameters );
   }

}
