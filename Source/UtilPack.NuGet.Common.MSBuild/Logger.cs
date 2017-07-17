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
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace UtilPack.NuGet.Common.MSBuild
{
   /// <summary>
   /// This class delegates NuGet logging messages to underlying <see cref="IBuildEngine"/>, if it is specified.
   /// </summary>
   /// <remarks>
   /// TODO: customize prefixes and make this as configurable and customizable as TextWriterLogger in UtilPack.NuGet project.
   /// </remarks>
   public class NuGetMSBuildLogger : global::NuGet.Common.ILogger
   {
      private IBuildEngine _be;

      private readonly String _errorCode;
      private readonly String _errorSummaryCode;
      private readonly String _warningCode;
      private readonly String _senderName;
      private readonly String _subCategory;

      /// <summary>
      /// Creates a new instance of <see cref="NuGetMSBuildLogger"/> with given parameters.
      /// </summary>
      /// <param name="errorCode">The error code to use in <see cref="LogError(string)"/> method.</param>
      /// <param name="errorSummaryCode">The error code to use in <see cref="LogErrorSummary(string)"/> method.</param>
      /// <param name="warningCode">The error code to use in <see cref="LogWarning(string)"/> method.</param>
      /// <param name="senderName">The sender name to use when logging.</param>
      /// <param name="subCategory">The sub category name to use when doing error and warning events.</param>
      /// <param name="be">The <see cref="IBuildEngine"/> to use. May be <c>null</c>.</param>
      public NuGetMSBuildLogger(
         String errorCode,
         String errorSummaryCode,
         String warningCode,
         String senderName,
         String subCategory,
         IBuildEngine be
         )
      {
         this._errorCode = errorCode;
         this._errorSummaryCode = errorSummaryCode;
         this._warningCode = warningCode;
         this._senderName = senderName;
         this._subCategory = subCategory;
         this._be = be;
      }

      /// <inheritdoc/>
      public void LogDebug( String data )
      {
         this._be?.LogMessageEvent( new BuildMessageEventArgs( "[NuGet Debug]: " + data, null, this._senderName, MessageImportance.Low ) );
      }

      /// <inheritdoc/>
      public void LogError( String data )
      {
         this._be?.LogErrorEvent( new BuildErrorEventArgs( this._subCategory, this._errorCode, null, -1, -1, -1, -1, "[NuGet Error]: " + data, null, this._senderName ) );
      }

      /// <inheritdoc/>
      public void LogErrorSummary( String data )
      {
         this._be?.LogErrorEvent( new BuildErrorEventArgs( this._subCategory, this._errorSummaryCode, null, -1, -1, -1, -1, "[NuGet ErrorSummary]: " + data, null, this._senderName ) );
      }

      /// <inheritdoc/>
      public void LogInformation( String data )
      {
         this._be?.LogMessageEvent( new BuildMessageEventArgs( "[NuGet Info]: " + data, null, this._senderName, MessageImportance.High ) );
      }

      /// <inheritdoc/>
      public void LogInformationSummary( String data )
      {
         this._be?.LogMessageEvent( new BuildMessageEventArgs( "[NuGet InfoSummary]: " + data, null, this._senderName, MessageImportance.High ) );
      }

      /// <inheritdoc/>
      public void LogMinimal( String data )
      {
         this._be?.LogMessageEvent( new BuildMessageEventArgs( "[NuGet Minimal]: " + data, null, this._senderName, MessageImportance.Low ) );
      }

      /// <inheritdoc/>
      public void LogVerbose( String data )
      {
         this._be?.LogMessageEvent( new BuildMessageEventArgs( "[NuGet Verbose]: " + data, null, this._senderName, MessageImportance.Normal ) );
      }

      /// <inheritdoc/>
      public void LogWarning( String data )
      {
         this._be?.LogWarningEvent( new BuildWarningEventArgs( this._subCategory, this._warningCode, null, -1, -1, -1, -1, "[NuGet Warning]: " + data, null, this._senderName ) );
      }

      /// <summary>
      /// Gets or sets the current <see cref="IBuildEngine"/>.
      /// </summary>
      public IBuildEngine BuildEngine
      {
         get
         {
            return this._be;
         }
         set
         {
            System.Threading.Interlocked.Exchange( ref this._be, value );
         }
      }
   }
}
