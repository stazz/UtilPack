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
using NuGetUtils.Lib.Deployment;
using NuGetUtils.Lib.MSBuild;
using NuGetUtils.Lib.Restore;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using UtilPack;

namespace NuGetUtils.MSBuild.Deployment
{
   public class DeployNuGetPackageTask : Microsoft.Build.Utilities.Task, NuGetDeploymentConfiguration, NuGetUsageConfiguration, ICancelableTask
   {
      private readonly CancellationTokenSource _cancelTokenSource;

      public DeployNuGetPackageTask()
      {
         this._cancelTokenSource = new CancellationTokenSource();
      }

      public override Boolean Execute()
      {
         String epAssembly;
         using ( new UsingHelper( () => this._cancelTokenSource.DisposeSafely() ) )
         {
            epAssembly = this.CreateAndUseRestorerAsync(
               this.GetType(),
               this.LockFileCacheDirEnvName,
               this.LockFileCacheDirWithinHomeDir,
               () => new NuGetMSBuildLogger( "NDE001", "NDE002", this.GetType().FullName, null, this.BuildEngine ),
               restorer => this.DeployAsync( restorer.Restorer, this._cancelTokenSource.Token, restorer.SDKPackageID, restorer.SDKPackageVersion )
               ).GetAwaiter().GetResult().EntryPointAssemblyPath;
         }

         var success = !this.Log.HasLoggedErrors
            && !String.IsNullOrEmpty( epAssembly )
            && File.Exists( epAssembly );
         if ( success )
         {
            this.EntryPointAssemblyPath = Path.GetFullPath( epAssembly );
         }
         return success;
      }

      public void Cancel()
      {
         this._cancelTokenSource.Cancel();
      }

      [Output]
      public String EntryPointAssemblyPath { get; set; }

      public String NuGetConfigurationFile { get; set; }

      public String RestoreFramework { get; set; }

      public String LockFileCacheDirectory { get; set; }

      public String SDKFrameworkPackageID { get; set; }

      public String SDKFrameworkPackageVersion { get; set; }

      public Boolean DisableLockFileCache { get; set; }

      public LogLevel LogLevel { get; set; }

      public Boolean DisableLogging { get; set; }

      [Required]
      public String PackageID { get; set; }

      public String PackageVersion { get; set; }

      public String AssemblyPath { get; set; }

      public String PackageSDKFrameworkPackageID { get; set; }

      public String PackageSDKFrameworkPackageVersion { get; set; }

      public DeploymentKind DeploymentKind { get; set; }

      public Boolean? PackageFrameworkIsPackageBased { get; set; }

      public String TargetDirectory { get; set; }

      public String LockFileCacheDirEnvName { get; set; }
      public String LockFileCacheDirWithinHomeDir { get; set; }
   }
}
