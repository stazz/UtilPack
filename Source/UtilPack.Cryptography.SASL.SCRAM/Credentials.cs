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
using System.Threading;

namespace UtilPack.Cryptography.SASL.SCRAM
{
   public sealed class SASLCredentialsSCRAMForClient
   {
      private Byte[] _pwDigest;

      public SASLCredentialsSCRAMForClient(
         String username,
         String password
         )
      {
         this.Username = username;
         this.Password = password;
      }

      public SASLCredentialsSCRAMForClient(
         String username,
         Byte[] passwordDigest
         )
      {
         this.Username = username;
         this._pwDigest = ArgumentValidator.ValidateNotEmpty( nameof( passwordDigest ), passwordDigest );
      }

      public String Username { get; }

      // password_s (the result of PBKDF2)
      public Byte[] PasswordDigest
      {
         get => this._pwDigest;
         internal set => Interlocked.CompareExchange( ref this._pwDigest, value, null );
      }
      public String Password { get; }
   }

   public sealed class SASLCredentialsSCRAMForServer
   {
      public SASLCredentialsSCRAMForServer(
         Byte[] clientKeyDigest,
         Byte[] serverKey,
         Byte[] salt,
         Int32 iterationCount
         )
      {
         this.ClientKeyDigest = clientKeyDigest;
         this.ServerKey = serverKey;
         this.Salt = salt;
         this.IterationCount = iterationCount;
      }

      public Byte[] Salt { get; }

      public Int32 IterationCount { get; }

      public Byte[] ClientKeyDigest { get; }

      public Byte[] ServerKey { get; }
   }
}
