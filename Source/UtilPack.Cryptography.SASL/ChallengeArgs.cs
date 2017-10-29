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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace UtilPack.Cryptography.SASL
{
   /// <summary>
   /// This structure encapsulates all information required for <see cref="SASLMechanism"/> to perform its challenge.
   /// </summary>
   public struct SASLChallengeArguments
   {
      /// <summary>
      /// Initializes new instance of <see cref="SASLChallengeArguments"/> with given parameters.
      /// </summary>
      /// <param name="readArray">The array where to read the response sent by remote. May be <c>null</c> if no response was received yet.</param>
      /// <param name="readOffset">The offset in <paramref name="readArray"/> where the remote response starts.</param>
      /// <param name="readCount">The amount of bytes in <paramref name="readArray"/> reserved by remote response.</param>
      /// <param name="writeArray">The <see cref="ResizableArray{T}"/> where to write the response by the challenge. May not be <c>null</c>.</param>
      /// <param name="writeOffset">The offset in <paramref name="writeArray"/> where to start writing response by this challenge.</param>
      /// <param name="encoding">The <see cref="IEncodingInfo"/> used by the protocol. May be <c>null</c> if protocol is not textual.</param>
      /// <param name="credentials">The protocol specific credentials object. May be <c>null</c>.</param>
      /// <exception cref="ArgumentNullException">If <paramref name="writeArray"/> is <c>null</c>.</exception>
      public SASLChallengeArguments(
         Byte[] readArray,
         Int32 readOffset,
         Int32 readCount,
         ResizableArray<Byte> writeArray,
         Int32 writeOffset,
         IEncodingInfo encoding,
         Object credentials
         )
      {
         if ( readArray == null )
         {
            this.ReadArray = Empty<Byte>.Array;
            this.ReadOffset = this.ReadCount = -1;
         }
         else
         {
            this.ReadArray = readArray;
            this.ReadOffset = readOffset;
            this.ReadCount = readCount;
         }

         this.WriteArray = ArgumentValidator.ValidateNotNull( nameof( writeArray ), writeArray );
         this.WriteOffset = writeOffset;
         this.Encoding = encoding;
         this.Credentials = credentials;
      }

      /// <summary>
      /// Gets the byte array containing the response sent by remote end.
      /// </summary>
      /// <value>The byte array containing the response sent by remote end.</value>
      public Byte[] ReadArray { get; }

      /// <summary>
      /// Gets the offset in <see cref="ReadArray"/> where the remote response starts.
      /// </summary>
      /// <value>The offset in <see cref="ReadArray"/> where the remote response starts.</value>
      public Int32 ReadOffset { get; }

      /// <summary>
      /// Gets the amount of bytes the <see cref="ReadArray"/> contains which are remote response bytes.
      /// </summary>
      /// <value>The amount of bytes the <see cref="ReadArray"/> contains which are remote response bytes.</value>
      public Int32 ReadCount { get; }

      /// <summary>
      /// Gets the <see cref="ResizableArray{T}"/> where to write this challenge response.
      /// </summary>
      /// <value>The <see cref="ResizableArray{T}"/> where to write this challenge response.</value>
      public ResizableArray<Byte> WriteArray { get; }

      /// <summary>
      /// Gets the offset in <see cref="WriteArray"/> where to start writing this challenge response.
      /// </summary>
      /// <value>The offset in <see cref="WriteArray"/> where to start writing this challenge response.</value>
      public Int32 WriteOffset { get; }

      /// <summary>
      /// Gets the <see cref="IEncodingInfo"/> to use for textual content of the SASL protocol.
      /// </summary>
      /// <value>The <see cref="IEncodingInfo"/> to use for textual content of the SASL protocol.</value>
      public IEncodingInfo Encoding { get; }

      /// <summary>
      /// Gets the protocol-specific credentials object as passed to constructor.
      /// </summary>
      /// <value>The protocol-specific credentials object as passed to constructor.</value>
      public Object Credentials { get; }
   }

   /// <summary>
   /// This class contains the reference to protocol-specific credential object, which may be modified.
   /// It is used typically by server side of protocol, where the e.g. username is not known before first call to <see cref="SASLMechanism.ChallengeAsync"/>
   /// </summary>
   public sealed class SASLCredentialsHolder
   {
      private Object _credentials;

      /// <summary>
      /// Gets or sets the protocol-specific credentials object.
      /// </summary>
      /// <value>The protocol-specific credentials object.</value>
      public Object Credentials
      {
         get => this._credentials;
         set => Interlocked.CompareExchange( ref this._credentials, value, null );
      }
   }
}
