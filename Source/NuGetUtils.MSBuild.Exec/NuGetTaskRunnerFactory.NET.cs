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
#if !IS_NETSTANDARD
using NuGet.Frameworks;
using NuGet.Repositories;
using NuGetUtils.Lib.AssemblyResolving;
using NuGetUtils.Lib.Restore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuGetUtils.MSBuild.Exec
{
   partial class NuGetTaskRunnerFactory
   {

      private static Boolean IsMono { get; } = Type.GetType( "Mono.Runtime", false, false ) != null;

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
         ref AppDomainSetup appDomainSetup
         )
      {
         // Only create dedicated app domain if we are not running on Mono, since AppDomains are broken in Mono. (According to discussion on https://github.com/xunit/xunit/issues/1357 )
         // ... Unless the value is overridden in task body options element.
         if ( appDomainSetup == null && !( taskBodyElement.ElementAnyNS( NO_DEDICATED_APPDOMAIN )?.Value?.ParseAsBooleanSafe() ?? IsMono ) )
         {
            Interlocked.CompareExchange( ref appDomainSetup, this.CreateAppDomainSetup( taskAssemblyFullPath ), null );
         }

         var isDedicatedDomain = appDomainSetup != null;

         var mbfAssembly = typeof( Microsoft.Build.Framework.ITask ).Assembly;
         var mbfAssemblyDir = Path.GetDirectoryName( new Uri( mbfAssembly.CodeBase ).LocalPath );
         var thisLoader = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
            restorer,
            appDomainSetup, // This will always be null on Mono, causing current AppDomain to be used instead of dedicated one
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
               taskAssemblyPathHint,
               mbfAssembly.GetName().FullName,
               resolverLogger
               ),
            resolverLogger,
            () =>
            {
               thisLoader.DisposeSafely();
               if ( isDedicatedDomain )
               {
                  try
                  {
                     AppDomain.Unload( createdDomain );
                  }
                  catch
                  {
                     // Ignore
                  }
               }
            }
         );
      }

      private AppDomainSetup CreateAppDomainSetup(
         String taskAssemblyFullPath
         )
      {
         var assemblyDir = Path.GetDirectoryName( taskAssemblyFullPath );
         var aSetup = new AppDomainSetup()
         {
            ApplicationBase = assemblyDir,
            ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
            DisallowBindingRedirects = false,
         };

         // TODO if the DLL has its own configuration file, merge it with the one loaded by this app domain
         // Configuration file of this domain will be typically {msbuild bin path}\MSBuild.exe.config, and will take care of version redirects for MSBuild assemblies

         return aSetup;
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