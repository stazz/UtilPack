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
using System.IO;
using System.Reflection;
using UtilPack.NuGet.Common.MSBuild;

namespace UtilPack.NuGet.Deployment.MSBuild
{
   public class DeployNuGetPackageTask : Microsoft.Build.Utilities.Task, DeploymentConfiguration
   {
      public override Boolean Execute()
      {
         // TODO maybe use this.BuildEngine.Yield?
         var retVal = new NuGetDeployment( this ).DeployAsync(
            UtilPackNuGetUtility.GetNuGetSettingsWithDefaultRootDirectory(
               Path.GetDirectoryName( this.BuildEngine.ProjectFileOfTaskNode ),
               this.NuGetConfigurationFile
            ),
            this.TargetDirectory,
            logger: new NuGetMSBuildLogger( "NDE", "NDE", "NDE", this.GetType().FullName, null, this.BuildEngine )
            ).GetAwaiter().GetResult();

         var epAssembly = retVal.Item1;

         var success = !this.Log.HasLoggedErrors
            && !String.IsNullOrEmpty( epAssembly )
            && File.Exists( epAssembly );
         if ( success )
         {
            this.EntryPointAssemblyPath = Path.GetFullPath( epAssembly );
         }
         return success;
      }

      public String ProcessPackageID { get; set; }

      public String ProcessPackageVersion { get; set; }

      public String ProcessFramework { get; set; }

      public String ProcessAssemblyPath { get; set; }

      public String ProcessSDKFrameworkPackageID { get; set; }

      public String ProcessSDKFrameworkPackageVersion { get; set; }

      public DeploymentKind DeploymentKind { get; set; }

      public String NuGetConfigurationFile { get; set; }

      public String TargetDirectory { get; set; }

      [Output]
      public String EntryPointAssemblyPath { get; set; }
   }
}
