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
#if !NET45
using Microsoft.Build.Framework;
using NuGet.Frameworks;
using NuGet.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Concurrent;
using UtilPack;
using NuGet.Packaging;
using System.Xml.Linq;
using System.IO;
using System.Runtime.Loader;
using System.Threading.Tasks;

using TResolveResult = System.Collections.Generic.IDictionary<System.String, UtilPack.NuGet.MSBuild.ResolvedPackageInfo>;
using UtilPack.NuGet.AssemblyLoading;

namespace UtilPack.NuGet.MSBuild
{
   partial class NuGetTaskRunnerFactory
   {
      private const String OTHER_LOADERS_REGISTRATION = "OtherLoadersRegistration";

      private TaskReferenceHolderInfo CreateExecutionHelper(
         String taskName,
         XElement taskBodyElement,
         String taskPackageID,
         String taskPackageVersion,
         String taskAssemblyFullPath,
         String taskAssemblyPathHint,
         BoundRestoreCommandUser restorer,
         ResolverLogger resolverLogger,
         GetFileItemsDelegate getFiles,
         String assemblyCopyTargetFolder,
         global::NuGet.ProjectModel.LockFile thisFrameworkRestoreResult
         )
      {
         var otherLoadersRegistrationString = taskBodyElement.ElementAnyNS( OTHER_LOADERS_REGISTRATION )?.Value;
         var otherLoadersRegistration = OtherLoadersRegistration.Default | OtherLoadersRegistration.Current;
         if ( !String.IsNullOrEmpty( otherLoadersRegistrationString ) )
         {
            Enum.TryParse( otherLoadersRegistrationString, out otherLoadersRegistration );
         }

         var thisLoader = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
            restorer,
            thisFrameworkRestoreResult,
            out var assemblyLoader,
            defaultGetFiles: getFiles,
            additionalCheckForDefaultLoader: IsMBFAssembly, // Use default loader for Microsoft.Build assemblies
            pathProcessor: CreatePathProcessor( assemblyCopyTargetFolder ),
            loadersRegistration: otherLoadersRegistration
            );
         RegisterToResolverEvents( thisLoader, resolverLogger );

         LoadTaskType(
            taskName,
            thisLoader,
            taskPackageID,
            taskPackageVersion,
            taskAssemblyPathHint,
            out var taskCtor,
            out var taskCtorArgs,
            out var taskUsesDynamicLoading
            );

         return new TaskReferenceHolderInfo(
            new TaskReferenceHolder(
               taskCtor?.Invoke( taskCtorArgs ),
               typeof( ITask ).GetTypeInfo().Assembly.GetName().FullName,
               taskUsesDynamicLoading
               ),
            resolverLogger,
            () => thisLoader.DisposeSafely()
            );
      }

   }
}
#endif