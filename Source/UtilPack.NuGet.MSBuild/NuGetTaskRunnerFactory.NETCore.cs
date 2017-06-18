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
      private TaskReferenceHolderInfo CreateExecutionHelper(
         String taskName,
         XElement taskBodyElement,
         String taskPackageID,
         String taskPackageVersion,
         String taskAssemblyPath,
         BoundRestoreCommandUser restorer,
         ResolverLogger resolverLogger,
         GetFileItemsDelegate getFiles,
         global::NuGet.ProjectModel.LockFile thisFrameworkRestoreResult
         )
      {

         var thisLoader = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
            restorer,
            thisFrameworkRestoreResult,
            out var assemblyLoader,
            defaultGetFiles: getFiles,
            additionalCheckForDefaultLoader: IsMBFAssembly // Use default loader for Microsoft.Build assemblies
            );

         var taskTypeInfo = LoadTaskType( taskName, thisLoader, taskPackageID, taskPackageVersion, taskAssemblyPath );

         return new TaskReferenceHolderInfo(
            new TaskReferenceHolder(
               taskTypeInfo.Item2.Invoke( taskTypeInfo.Item3 ),
               typeof( ITask ).GetTypeInfo().Assembly.GetName().FullName,
               taskTypeInfo.Item4
               ),
            resolverLogger,
            () => thisLoader.DisposeSafely()
            );
      }

   }
}
#endif