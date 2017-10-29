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
   /// <summary>
   /// This class contains credential information for client-side SCRAM authentication.
   /// The <see cref="SASLChallengeArguments.Credentials"/> property of <see cref="SASLChallengeArguments"/> passed to client-side SCRAM <see cref="SASLMechanism"/> in its <see cref="SASLMechanism.ChallengeAsync"/> method should be instance of this class.
   /// </summary>
   public sealed class SASLCredentialsSCRAMForClient
   {
      private Byte[] _pwDigest;

      /// <summary>
      /// Creates new <see cref="SASLCredentialsSCRAMForClient"/> with given username and cleartext password.
      /// </summary>
      /// <param name="username">The username.</param>
      /// <param name="password">The cleartext password.</param>
      /// <remarks>
      /// The <see cref="SASLMechanism"/> will set <see cref="PasswordDigest"/> property to appropriate digest during authentication process.
      /// If authentication is successful, the <see cref="PasswordDigest"/> maybe saved, so that authentication could proceed next time without cleartext password.
      /// </remarks>
      /// <exception cref="ArgumentNullException">If <paramref name="username"/> or <paramref name="password"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">If <paramref name="username"/> or <paramref name="password"/> is empty.</exception>
      public SASLCredentialsSCRAMForClient(
         String username,
         String password
         )
      {
         this.Username = ArgumentValidator.ValidateNotEmpty( nameof( username ), username );
         this.Password = ArgumentValidator.ValidateNotEmpty( nameof( password ), password ); ;
      }

      /// <summary>
      /// Creates new <see cref="SASLCredentialsSCRAMForClient"/> with given username and password digest.
      /// </summary>
      /// <param name="username">The username.</param>
      /// <param name="passwordDigest">The password digest (result of PBKDF2 processing).</param>
      /// <remarks>
      /// The password digest should be the result of PBKDF2 processing of cleartext password.
      /// </remarks>
      public SASLCredentialsSCRAMForClient(
         String username,
         Byte[] passwordDigest
         )
      {
         this.Username = ArgumentValidator.ValidateNotEmpty( nameof( username ), username );
         this._pwDigest = ArgumentValidator.ValidateNotEmpty( nameof( passwordDigest ), passwordDigest );
      }

      /// <summary>
      /// Gets the username supplied to this <see cref="SASLCredentialsSCRAMForClient"/>.
      /// </summary>
      /// <value>The username supplied to this <see cref="SASLCredentialsSCRAMForClient"/>.</value>
      public String Username { get; }

      /// <summary>
      /// Gets the password digest, either supplied to this <see cref="SASLCredentialsSCRAMForClient"/> via constructor, or set by <see cref="SASLMechanism"/>.
      /// </summary>
      /// <value>The password digest, either supplied to this <see cref="SASLCredentialsSCRAMForClient"/> via constructor, or set by <see cref="SASLMechanism"/>.</value>
      /// <remarks>
      /// This value should be result of PBKDF2 processing.
      /// </remarks>
      public Byte[] PasswordDigest
      {
         get => this._pwDigest;
         internal set => Interlocked.CompareExchange( ref this._pwDigest, value, null );
      }

      /// <summary>
      /// Gets the cleartext password supplied to this <see cref="SASLCredentialsSCRAMForClient"/>.
      /// </summary>
      /// <value>The cleartext password supplied to this <see cref="SASLCredentialsSCRAMForClient"/>.</value>
      /// <remarks>
      /// This value may be <c>null</c>.
      /// </remarks>
      public String Password { get; }
   }

   /// <summary>
   /// This class contains credentials information for server-side SCRAM authentication.
   /// The <see cref="SASLChallengeArguments.Credentials"/> property should be set to <see cref="SASLCredentialsHolder"/>, and its <see cref="SASLCredentialsHolder.Credentials"/> property will be then modified by <see cref="SASLMechanism"/> to instance of this class.
   /// </summary>
   /// <remarks>
   /// The instances of this classes are created by callback supplied to <see cref="UtilPackUtility.CreateSASLServerSCRAM"/> method.
   /// </remarks>
   public sealed class SASLCredentialsSCRAMForServer
   {
      /// <summary>
      /// Creates a new instance of <see cref="SASLCredentialsSCRAMForServer"/> with given parameters.
      /// </summary>
      /// <param name="clientKeyDigest">The hash of the key_c.</param>
      /// <param name="serverKey">The key_s.</param>
      /// <param name="salt">The per-user salt.</param>
      /// <param name="iterationCount">The per-user iteration count for PBKDF2 processing.</param>
      /// <exception cref="ArgumentNullException">If any of the <paramref name="clientKeyDigest"/>, <paramref name="serverKey"/>, or <paramref name="salt"/> is <c>null</c>.</exception>
      /// <exception cref="ArgumentException">If any of the <paramref name="clientKeyDigest"/>, <paramref name="serverKey"/>, or <paramref name="salt"/> is empty.</exception>
      public SASLCredentialsSCRAMForServer(
         Byte[] clientKeyDigest,
         Byte[] serverKey,
         Byte[] salt,
         Int32 iterationCount
         )
      {
         this.ClientKeyDigest = ArgumentValidator.ValidateNotEmpty( nameof( clientKeyDigest ), clientKeyDigest );
         this.ServerKey = ArgumentValidator.ValidateNotEmpty( nameof( serverKey ), serverKey );
         this.Salt = ArgumentValidator.ValidateNotEmpty( nameof( salt ), salt );
         this.IterationCount = iterationCount;
      }

      /// <summary>
      /// Gets the salt as byte array.
      /// </summary>
      /// <value>The salt as byte array.</value>
      public Byte[] Salt { get; }

      /// <summary>
      /// Gets the iteration count.
      /// </summary>
      /// <value>The iteration count.</value>
      public Int32 IterationCount { get; }

      /// <summary>
      /// Gets the client key digest.
      /// </summary>
      /// <value>The client key digest.</value>
      public Byte[] ClientKeyDigest { get; }

      /// <summary>
      /// Gets the server key.
      /// </summary>
      /// <value>The server key.</value>
      public Byte[] ServerKey { get; }
   }
}
