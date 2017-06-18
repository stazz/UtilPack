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
#if NET45
using NuGet.Frameworks;
using NuGet.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Reflection.Emit;
using System.Xml.Linq;
using System.Threading.Tasks;
using UtilPack.NuGet.AssemblyLoading;

using TNuGetPackageResolverCallback = System.Func<System.String, System.String, System.String, System.Threading.Tasks.Task<System.Reflection.Assembly>>;
using TAssemblyByPathResolverCallback = System.Func<System.String, System.Reflection.Assembly>;

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
         String assemblyCopyTargetFolder
         )
      {
         var assemblyDir = Path.GetDirectoryName( taskAssemblyPath );
         var aSetup = new AppDomainSetup()
         {
            ApplicationBase = assemblyDir,
            ConfigurationFile = taskAssemblyPath + ".config"
         };

         var mbfAssembly = typeof( Microsoft.Build.Framework.ITask ).Assembly;
         var mbfAssemblyDir = Path.GetDirectoryName( new Uri( mbfAssembly.CodeBase ).LocalPath );
         var thisLoader = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
            restorer,
            aSetup,
            out var createdDomain,
            defaultGetFiles: getFiles,
            overrideLocation: ( assemblyName ) =>
            {
               String overrideLocation;
               if ( IsMBFAssembly( assemblyName ) )
               {
                  // don't use nuget-based resolver for MSBuild assemblies
                  overrideLocation = Path.Combine( mbfAssemblyDir, assemblyName.Name + ".dll" );
               }
               else
               {
                  overrideLocation = null;
               }

               return overrideLocation;
            },
            pathProcessor: CreatePathProcessor( assemblyCopyTargetFolder )
            );
         var thisAssemblyPath = Path.GetFullPath( new Uri( this.GetType().Assembly.CodeBase ).LocalPath );
         var creator = (TaskReferenceCreator) createdDomain.CreateInstanceFromAndUnwrap(
               thisAssemblyPath,
               typeof( TaskReferenceCreator ).FullName,
               false,
               0,
               null,
               new Object[] { },
               null,
               null
            );

         return new TaskReferenceHolderInfo(
            creator.CreateTaskReferenceHolder(
               taskName,
               thisLoader,
               taskPackageID,
               taskPackageVersion,
               taskAssemblyPath,
               mbfAssembly.GetName().FullName,
               resolverLogger
               ),
            resolverLogger,
            () =>
            {
               thisLoader.DisposeSafely();
               try
               {
                  AppDomain.Unload( createdDomain );
               }
               catch
               {
                  // Ignore
               }
            }
         );
      }
   }

   internal sealed class TaskReferenceCreator : MarshalByRefObject
   {
      internal TaskReferenceHolder CreateTaskReferenceHolder(
         String taskTypeName,
         NuGetAssemblyResolver resolver,
         String packageID,
         String packageVersion,
         String assemblyPath,
         String msbuildFrameworkAssemblyName,
         ResolverLogger logger
         )
      {
         // This code executes in task app domain.
         NuGetTaskRunnerFactory.RegisterToResolverEvents( resolver, logger );

         NuGetTaskRunnerFactory.LoadTaskType(
            taskTypeName,
            resolver,
            packageID,
            packageVersion,
            assemblyPath,
            out var ctor,
            out var ctorParams,
            out var taskUsesDynamicLoading
            );
         return new TaskReferenceHolder( ctor?.Invoke( ctorParams ), msbuildFrameworkAssemblyName, taskUsesDynamicLoading );
      }
   }

}
#endif