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
using NuGetUtils.Lib.Restore;
using NuGetUtils.Lib.Tool;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetUtils.Tool.Deploy
{
   static class Program
   {
      public static Task<Int32> Main( String[] args )
         => new NuGetExecutingProgram().MainAsync( args );

   }

   internal sealed class NuGetExecutingProgram : NuGetRestoringProgram<NuGetDeploymentConfigurationImpl, ConfigurationConfigurationImpl>
   {
      public NuGetExecutingProgram()
         : base( new DefaultCommandLineDocumentationInfo()
         {
            ExecutableName = "nuget-deploy",
            CommandLineGroupInfo = new DefaultDocumentationGroupInfo()
            {
               Purpose = "Deploy an assembly from NuGet-package, restoring the package if needed, parametrized by command-line parameters."
            },
            ConfigurationFileGroupInfo = new DefaultDocumentationGroupInfo()
            {
               Purpose = "Deploy an assembly from NuGet-package, restoring the package if needed, parametrized by configuration file."
            }
         } )
      {

      }

      protected override Boolean ValidateConfiguration(
         ConfigurationInformation info
         )
      {
         return !String.IsNullOrEmpty( info.Configuration.PackageID );
      }

      protected override async Task<Int32> UseRestorerAsync(
         ConfigurationInformation info,
         CancellationToken token,
         BoundRestoreCommandUser restorer,
         String sdkPackageID,
         String sdkPackageVersion
         )
      {
         var entryPointAssembly = await info.Configuration.DeployAsync(
            restorer,
            token,
            sdkPackageID,
            sdkPackageVersion
            );

         restorer.NuGetLogger.Log( NuGet.Common.LogLevel.Information, $"Deployed assembly to {entryPointAssembly}." );

         return 0;
      }
   }
}
