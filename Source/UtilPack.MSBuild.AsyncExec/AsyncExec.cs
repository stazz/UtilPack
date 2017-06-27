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
using Microsoft.Build.Tasks;
using System;
using System.Diagnostics;

namespace UtilPack.MSBuild.AsyncExec
{
   public class AsyncExec : Exec
   {
      protected override Int32 ExecuteTool(
         String pathToTool,
         String responseFileCommands,
         String commandLineCommands
         )
      {
         var process = new Process()
         {
            StartInfo = this.GetProcessStartInfo( pathToTool, commandLineCommands, null ),
            EnableRaisingEvents = false,
         };

         process.Start();
         return 0;
      }

      protected override ProcessStartInfo GetProcessStartInfo(
         String pathToTool,
         String commandLineCommands,
         String responseFileSwitch
         )
      {
         var retVal = base.GetProcessStartInfo( pathToTool, commandLineCommands, responseFileSwitch );
         retVal.UseShellExecute = false;
         retVal.CreateNoWindow = false;
         retVal.RedirectStandardOutput = false;
         retVal.RedirectStandardError = false;
         retVal.RedirectStandardInput = false;
         retVal.StandardErrorEncoding = null;
         retVal.StandardOutputEncoding = null;
         return retVal;
      }

   }
}
