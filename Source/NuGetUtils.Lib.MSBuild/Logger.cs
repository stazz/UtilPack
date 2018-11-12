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
using NuGet.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NuGetUtils.Lib.MSBuild
{
   /// <summary>
   /// This class delegates NuGet logging messages to underlying <see cref="IBuildEngine"/>, if it is specified.
   /// </summary>
   /// <remarks>
   /// TODO: customize prefixes and make this as configurable and customizable as TextWriterLogger in UtilPack.NuGet project.
   /// </remarks>
   public class NuGetMSBuildLogger : global::NuGet.Common.LoggerBase
   {
      private IBuildEngine _be;

      private readonly String _errorCode;
      private readonly String _warningCode;
      private readonly String _senderName;
      private readonly String _subCategory;

      /// <summary>
      /// Creates a new instance of <see cref="NuGetMSBuildLogger"/> with given parameters.
      /// </summary>
      /// <param name="errorCode">The error code to use for <see cref="global::NuGet.Common.LogLevel.Error"/>.</param>
      /// <param name="warningCode">The error code to use for <see cref="global::NuGet.Common.LogLevel.Warning"/>.</param>
      /// <param name="senderName">The sender name to use when logging.</param>
      /// <param name="subCategory">The sub category name to use when doing error and warning events.</param>
      /// <param name="be">The <see cref="IBuildEngine"/> to use. May be <c>null</c>.</param>
      public NuGetMSBuildLogger(
         String errorCode,
         String warningCode,
         String senderName,
         String subCategory,
         IBuildEngine be
         )
      {
         this._errorCode = errorCode;
         this._warningCode = warningCode;
         this._senderName = senderName;
         this._subCategory = subCategory;
         this._be = be;
      }

      /// <inheritdoc/>
      public override void Log( ILogMessage message )
      {
         IBuildEngine be;
         LogLevel level;
         if ( message != null
            && ( be = this._be ) != null
            && this.DisplayMessage( level = message.Level )
            )
         {
            var data = message.Message;
            if ( level < LogLevel.Warning )
            {
               MessageImportance importance;
               if ( level < LogLevel.Information )
               {
                  importance = MessageImportance.Low;
               }
               else if ( level < LogLevel.Minimal )
               {
                  importance = MessageImportance.Normal;
               }
               else
               {
                  importance = MessageImportance.High;
               }
               be.LogMessageEvent( new BuildMessageEventArgs( "[NuGet " + level + "]: " + data, null, this._senderName, importance ) );
            }
            else if ( level < LogLevel.Error )
            {
               be.LogWarningEvent( new BuildWarningEventArgs( this._subCategory, this._warningCode, null, -1, -1, -1, -1, "[NuGet " + level + "]: " + data, null, this._senderName ) );
            }
            else
            {
               be.LogErrorEvent( new BuildErrorEventArgs( this._subCategory, this._errorCode, null, -1, -1, -1, -1, "[NuGet " + level + "]: " + data, null, this._senderName ) );
            }

         }
      }

      /// <inheritdoc/>
      public override Task LogAsync( ILogMessage message )
      {
         this.Log( message );
         return Task.CompletedTask;

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
