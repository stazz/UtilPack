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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace NuGetUtils.Tool.Restore
{
   internal static class Program
   {
      public static Task<Int32> Main( String[] args )
         => new NuGetRestoringProgram().MainAsync( args );

   }

   internal sealed class NuGetRestoringProgram : NuGetRestoringProgram<NuGetRestoreConfiguration, ConfigurationConfigurationImpl>
   {

      public NuGetRestoringProgram()
         : base( new DefaultCommandLineDocumentationInfo()
         {
            ExecutableName = "nuget-restore",
            CommandLineGroupInfo = new DefaultDocumentationGroupInfo()
            {
               Purpose = "Restore one or multiple NuGet packages, parametrized by command-line parameters."
            },
            ConfigurationFileGroupInfo = new DefaultDocumentationGroupInfo()
            {
               Purpose = "Restore one or multiple NuGet packages, parametrized by configuration file."
            }
         } )
      {
      }

      protected override Boolean ValidateConfiguration(
         ConfigurationInformation info
         )
      {
         var config = info.Configuration;
         var isMultiPackage = String.IsNullOrEmpty( config.PackageID );
         return isMultiPackage ^ config.PackageIDs.IsNullOrEmpty()
            && ( isMultiPackage ? String.IsNullOrEmpty( config.PackageVersion ) : config.PackageVersions.IsNullOrEmpty() );
      }

      protected override async Task<Int32> UseRestorerAsync(
         ConfigurationInformation info,
         CancellationToken token,
         BoundRestoreCommandUser restorer,
         String sdkPackageID,
         String sdkPackageVersion
         )
      {
         if ( !info.Configuration.SkipRestoringSDKPackage )
         {
            await restorer.RestoreIfNeeded( sdkPackageID, sdkPackageVersion, token );
         }

         var config = info.Configuration;
         var packageID = config.PackageID;
         var packageVersions = config.PackageVersions;
         await restorer.RestoreIfNeeded(
            token,
            String.IsNullOrEmpty( packageID ) ?
               config.PackageIDs.Select( ( pID, idx ) => (pID, packageVersions.GetElementOrDefault( idx )) ).ToArray() :
               new[] { (packageID, config.PackageVersion) }
            );
         return 0;
      }


   }
}
