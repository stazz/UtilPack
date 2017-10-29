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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.Cryptography.SASL;

using TSuccessfulChallenge = System.ValueTuple<System.Int32, UtilPack.Cryptography.SASL.SASLChallengeResult>;
using TUnsuccessfulChallenge = System.Int32;

namespace UtilPack
{
   using TSyncChallengeResult = EitherOr<TSuccessfulChallenge, TUnsuccessfulChallenge>;

   namespace Cryptography.SASL
   {
      using TAsyncSyncChallengeResult = ValueTask<TSyncChallengeResult>;

      /// <summary>
      /// This interface represents any SASL (<see href="https://tools.ietf.org/html/rfc4422">RFC 4422</see>) mechanism (e.g. SASL-SCRAM-SHA128, and others).
      /// </summary>
      /// <remarks>
      /// Typically, one instance of tthis interface can be used for one authentication attempt at a time.
      /// </remarks>
      public interface SASLMechanism : IDisposable
      {

         /// <summary>
         /// Given the current state of this <see cref="SASLMechanism"/>, performs a potentially asynchronous challenge and returns the result.
         /// </summary>
         /// <param name="args">The <see cref="SASLChallengeArguments"/> containing information about previously read remote response (if any), and the array where to write response, along with encoding and credential information.</param>
         /// <returns>The <see cref="EitherOr{T1, T2}"/> object, which will have <see cref="EitherOr{T1, T2}.IsFirst"/> set to <c>true</c> in case of successful challenge. For unsuccessful challenges, <see cref="EitherOr{T1, T2}.IsSecond"/> is set to <c>true</c></returns>
         /// <remarks>
         /// The type of successful challenge result is a tuple of amount of bytes written to <see cref="SASLChallengeArguments.WriteArray"/> of <paramref name="args"/>, and the <see cref="SASLChallengeResult"/> enumeration describing whether this <see cref="SASLMechanism"/> expects more calls to <see cref="Challenge"/>-
         /// The type of unsuccesssful challenge result is a mechanism-specific error code, which should be negative integer.
         /// </remarks>
         /// <seealso cref="SASLChallengeArguments"/>
         TAsyncSyncChallengeResult Challenge(
            SASLChallengeArguments args
            );

         /// <summary>
         /// Resets the state, if any, of this <see cref="SASLMechanism"/>.
         /// </summary>
         void Reset();
      }

      /// <summary>
      /// This enumeration represents a result of successful SASL challenge result (e.g. from <see cref="SASLMechanism.Challenge"/> method).
      /// </summary>
      public enum SASLChallengeResult
      {
         /// <summary>
         /// The challenge was successful, but remote end is expected to respond to whatever this <see cref="SASLMechanism"/> wrote to <see cref="SASLChallengeArguments.WriteArray"/>.
         /// </summary>
         MoreToCome,

         /// <summary>
         /// The challenge was successful, and remote is not expected to respond anything anymore. The session is considered to be authenticated.
         /// </summary>
         Completed
      }

      /// <summary>
      /// This class implements <see cref="SASLMechanism"/> in such way that <see cref="SASLMechanism.Challenge"/> is always synchronous.
      /// </summary>
      /// <typeparam name="TCredentials">The expected type of <see cref="SASLChallengeArguments.Credentials"/>.</typeparam>
      public abstract class AbstractSyncSASLMechanism<TCredentials> : AbstractDisposable, SASLMechanism
      {
         /// <summary>
         /// Implements <see cref="SASLMechanism.Challenge"/> by calling <see cref="Challenge(ref SASLChallengeArguments, TCredentials)"/>.
         /// If <see cref="SASLChallengeArguments.Credentials"/> is not of type <typeparamref name="TCredentials"/>, or if <see cref="Challenge(ref SASLChallengeArguments, TCredentials)"/> throws, then the exception is catched and error code returned by <see cref="GetExceptionErrorCode(Exception)"/> is returned.
         /// </summary>
         /// <param name="args">The <see cref="SASLChallengeArguments"/>.</param>
         /// <returns>The result of <see cref="Challenge(ref SASLChallengeArguments, TCredentials)"/>, or error code returned by <see cref="GetExceptionErrorCode(Exception)"/>.</returns>
         public TAsyncSyncChallengeResult Challenge(
            SASLChallengeArguments args
         )
         {
            try
            {
               return new TAsyncSyncChallengeResult( this.Challenge( ref args, (TCredentials) args.Credentials ) );
            }
            catch ( Exception exc )
            {
               return new TAsyncSyncChallengeResult( this.GetExceptionErrorCode( exc ) );
            }
         }

         /// <summary>
         /// Leaves implementation of <see cref="SASLMechanism.Reset"/> to be filled in by the derived classes.
         /// </summary>
         public abstract void Reset();

         /// <summary>
         /// The derived classes should implement this method for their SASL challenge logic.
         /// </summary>
         /// <param name="args">The <see cref="SASLChallengeArguments"/>.</param>
         /// <param name="credentials">The credentials.</param>
         /// <returns>Result indicating how the challenge went, either tuple of how many bytes were written along with <see cref="SASLChallengeResult"/>, or a integer with error code.</returns>
         protected abstract TSyncChallengeResult Challenge(
            ref SASLChallengeArguments args,
            TCredentials credentials
            );

         /// <summary>
         /// The derived classes should implement this method to get an error code from occurred exception.
         /// </summary>
         /// <param name="exception">The occurred exception.</param>
         /// <returns>The error code for <paramref name="exception"/>.</returns>
         protected abstract Int32 GetExceptionErrorCode( Exception exception );
      }

      /// <summary>
      /// This class implements <see cref="SASLMechanism"/> in such way that <see cref="SASLMechanism.Challenge"/> implementation may be asynchronous.
      /// </summary>
      /// <typeparam name="TCredentials">The expected type of <see cref="SASLChallengeArguments.Credentials"/>.</typeparam>
      public abstract class AbstractAsyncSASLMechanism<TCredentials> : AbstractDisposable, SASLMechanism
      {
         /// <summary>
         /// Implements <see cref="SASLMechanism.Challenge"/> by calling <see cref="Challenge(SASLChallengeArguments, TCredentials)"/>.
         /// If <see cref="SASLChallengeArguments.Credentials"/> is not of type <typeparamref name="TCredentials"/>, or if <see cref="Challenge(SASLChallengeArguments, TCredentials)"/> throws, then the exception is catched and error code returned by <see cref="GetExceptionErrorCode(Exception)"/> is returned.
         /// </summary>
         /// <param name="args">The <see cref="SASLChallengeArguments"/>.</param>
         /// <returns>The result of <see cref="Challenge(SASLChallengeArguments, TCredentials)"/>, or error code returned by <see cref="GetExceptionErrorCode(Exception)"/>.</returns>
         public async TAsyncSyncChallengeResult Challenge(
            SASLChallengeArguments args
         )
         {
            try
            {
               return await this.Challenge( args, (TCredentials) args.Credentials );
            }
            catch ( Exception exc )
            {
               return new TSyncChallengeResult( this.GetExceptionErrorCode( exc ) );
            }
         }

         /// <summary>
         /// Leaves implementation of <see cref="SASLMechanism.Reset"/> to be filled in by the derived classes.
         /// </summary>
         public abstract void Reset();

         /// <summary>
         /// The derived classes should implement this method for their SASL challenge logic.
         /// </summary>
         /// <param name="args">The <see cref="SASLChallengeArguments"/>.</param>
         /// <param name="credentials">The credentials.</param>
         /// <returns>Result indicating how the challenge went, either tuple of how many bytes were written along with <see cref="SASLChallengeResult"/>, or a integer with error code.</returns>
         protected abstract TAsyncSyncChallengeResult Challenge(
            SASLChallengeArguments args,
            TCredentials credentials
            );

         /// <summary>
         /// The derived classes should implement this method to get an error code from occurred exception.
         /// </summary>
         /// <param name="exception">The occurred exception.</param>
         /// <returns>The error code for <paramref name="exception"/>.</returns>
         protected abstract Int32 GetExceptionErrorCode( Exception exception );
      }

      /// <summary>
      /// This class extends <see cref="AbstractAsyncSASLMechanism{TCredentials}"/> in such way that is most typical for server-side SASL implementations.
      /// Since these implementations will need to look up some authentication data (from e.g. DB) based on username, at least one step in their authentication will be asynchronous.
      /// Furthermore, once the authentication data is fetched, it would be saved to <see cref="SASLCredentialsHolder.Credentials"/> property of <see cref="SASLCredentialsHolder"/> for further processing.
      /// </summary>
      /// <typeparam name="TCredentials">The type of credentials stored in <see cref="SASLCredentialsHolder.Credentials"/> property of <see cref="SASLCredentialsHolder"/>.</typeparam>
      public abstract class AbstractServerSASLMechanism<TCredentials> : AbstractAsyncSASLMechanism<SASLCredentialsHolder>
         where TCredentials : class
      {
         /// <summary>
         /// Implements <see cref="AbstractAsyncSASLMechanism{TCredentials}.Challenge(SASLChallengeArguments, TCredentials)"/> by delegating implementation to <see cref="Challenge(SASLChallengeArguments, SASLCredentialsHolder, TCredentials)"/>.
         /// </summary>
         /// <param name="args">The <see cref="SASLChallengeArguments"/>.</param>
         /// <param name="credentials">The <see cref="SASLCredentialsHolder"/>.</param>
         /// <returns>Result of <see cref="Challenge(SASLChallengeArguments, SASLCredentialsHolder, TCredentials)"/>.</returns>
         protected sealed override TAsyncSyncChallengeResult Challenge(
            SASLChallengeArguments args,
            SASLCredentialsHolder credentials
            )
            => this.Challenge( args, credentials, (TCredentials) ( credentials ?? throw new InvalidOperationException( "No credential holder supplied" ) ).Credentials );

         /// <summary>
         /// The derived classes should implement this method for their SASL challenge logic.
         /// </summary>
         /// <param name="args">The <see cref="SASLChallengeArguments"/>.</param>
         /// <param name="credentialsHolder">The <see cref="SASLCredentialsHolder"/>.</param>
         /// <param name="credentials">The credentials from <see cref="SASLCredentialsHolder.Credentials"/> property of <see cref="SASLCredentialsHolder"/>.</param>
         /// <returns>Result indicating how the challenge went, either tuple of how many bytes were written along with <see cref="SASLChallengeResult"/>, or a integer with error code.</returns>
         protected abstract TAsyncSyncChallengeResult Challenge(
            SASLChallengeArguments args,
            SASLCredentialsHolder credentialsHolder,
            TCredentials credentials
            );
      }

   }
}

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static partial class E_UtilPack
{
   /// <summary>
   /// Helper method to create a new <see cref="SASLChallengeArguments"/> from this <see cref="SASLCredentialsHolder"/>.
   /// </summary>
   /// <param name="credentials">This <see cref="SASLCredentialsHolder"/>.</param>
   /// <param name="readArray">The array containing remote response.</param>
   /// <param name="readOffset">The offset in <paramref name="readArray"/> where remote response starts.</param>
   /// <param name="readCount">The amount of bytes in <paramref name="readArray"/> that remote response takes.</param>
   /// <param name="writeArray">The <see cref="ResizableArray{T}"/> where to write this response.</param>
   /// <param name="writeOffset">The offset in <paramref name="writeArray"/> where to start writing.</param>
   /// <param name="encoding">The <see cref="IEncodingInfo"/> to use for textual data.</param>
   /// <returns>A new instance of <see cref="SASLChallengeArguments"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="SASLCredentialsHolder"/> is <c>null</c>.</exception>
   public static SASLChallengeArguments CreateServerMechanismArguments(
      this SASLCredentialsHolder credentials,
      Byte[] readArray,
      Int32 readOffset,
      Int32 readCount,
      ResizableArray<Byte> writeArray,
      Int32 writeOffset,
      IEncodingInfo encoding
   )
   {
      return new SASLChallengeArguments( readArray, readOffset, readCount, writeArray, writeOffset, encoding, ArgumentValidator.ValidateNotNullReference( credentials ) );
   }

   /// <summary>
   /// This helper method will invoke <see cref="SASLMechanism.Challenge"/> method of this <see cref="SASLMechanism"/> and throw an exception if it returns error code.
   /// </summary>
   /// <param name="mechanism">This <see cref="SASLMechanism"/>.</param>
   /// <param name="args">The <see cref="SASLChallengeArguments"/>.</param>
   /// <returns>Information about how many bytes were written to <see cref="SASLChallengeArguments.WriteArray"/>, along with <see cref="SASLChallengeArguments"/>.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="SASLMechanism"/> is <c>null</c>.</exception>
   /// <exception cref="InvalidOperationException">If <see cref="SASLMechanism.Challenge"/> returns error code.</exception>
   public static async ValueTask<TSuccessfulChallenge> ChallengeOrThrowOnError( this SASLMechanism mechanism, SASLChallengeArguments args )
   {
      var challengeResult = await mechanism.Challenge( args );
      return challengeResult.IsFirst ? challengeResult.First : throw new InvalidOperationException( $"SASL challenge failed with exit code {challengeResult.GetSecondOrDefault()}." );
   }
}