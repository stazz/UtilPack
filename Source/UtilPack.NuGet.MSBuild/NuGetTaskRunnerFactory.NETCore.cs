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
#if IS_NETSTANDARD
using Microsoft.Build.Framework;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Repositories;
using NuGetUtils.Lib.AssemblyResolving;
using NuGetUtils.Lib.Restore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UtilPack;
using TResolveResult = System.Collections.Generic.IDictionary<System.String, UtilPack.NuGet.MSBuild.ResolvedPackageInfo>;

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

         var nativeAssembliesMapInfo = taskBodyElement.ElementAnyNS( UNMANAGED_ASSEMBLIES_MAP );
         UnmanagedAssemblyPathProcessorDelegate unmanagedAssemblyPathProcessor;
         if ( nativeAssembliesMapInfo == null )
         {
            unmanagedAssemblyPathProcessor = null;
         }
         else
         {
            var rGraph = restorer.RuntimeGraph.Value;
            var rid = restorer.RuntimeIdentifier;
            var map = nativeAssembliesMapInfo.Elements()
               .Where( element =>
               {
                  String attr1, attr2;
                  var retVal = !String.IsNullOrEmpty( element.Attribute( UNMANAGED_ASSEMBLY_REF )?.Value )
                     && !String.IsNullOrEmpty( element.Attribute( MAPPED_NAME )?.Value );
                  if (
                  ( !String.IsNullOrEmpty( attr1 = element.Attribute( MATCH_PLATFORM )?.Value )
                     && ( attr2 = element.Attribute( EXCEPT_PLATFORM )?.Value ) == null )
                     ||
                     ( ( attr1 = element.Attribute( MATCH_PLATFORM )?.Value ) == null
                     && !String.IsNullOrEmpty( attr2 = element.Attribute( EXCEPT_PLATFORM )?.Value ) )
                     )
                  {
                     if ( String.IsNullOrEmpty( attr1 ) )
                     {
                        // This is "ExceptPlatform" match
                        retVal = !rGraph.AreCompatible( rid, attr2 );
                     }
                     else
                     {
                        // This is "MatchPlatform" match
                        retVal = rGraph.AreCompatible( rid, attr1 );
                     }
                  }

                  return retVal;
               } )
               .ToDictionary_Overwrite( element =>
               {
                  // Key -> assembly ref name
                  return element.Attribute( UNMANAGED_ASSEMBLY_REF ).Value;
               }, element =>
               {
                  // VAlue -> mapped assembly name
                  return element.Attribute( MAPPED_NAME ).Value;
               } );
            unmanagedAssemblyPathProcessor = ( platformRID, unmanagedAssemblyName, allUnmanagedAssemblyPaths ) =>
            {
               IEnumerable<String> retVal;
               if ( map.TryGetValue( unmanagedAssemblyName, out var mappedName ) )
               {
                  if ( Path.IsPathRooted( mappedName ) )
                  {
                     retVal = mappedName.Singleton();
                  }
                  else
                  {
                     retVal = allUnmanagedAssemblyPaths
                        .Where( p => Path.GetFileNameWithoutExtension( p ).IndexOf( unmanagedAssemblyName ) >= 0 )
                        .Select( p => Path.Combine( Path.GetDirectoryName( p ), mappedName + Path.GetExtension( p ) ) );
                  }
               }
               else
               {
                  retVal = NuGetAssemblyResolverFactory.GetDefaultUnmanagedAssemblyPathCandidates( platformRID, unmanagedAssemblyName, allUnmanagedAssemblyPaths );
               }

               return retVal;
            };
         }


         var thisLoader = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
            restorer,
            out var assemblyLoader,
            thisFrameworkRestoreResult: thisFrameworkRestoreResult,
            defaultGetFiles: getFiles,
            additionalCheckForDefaultLoader: IsMBFAssembly, // Use default loader for Microsoft.Build assemblies
            pathProcessor: CreatePathProcessor( assemblyCopyTargetFolder ),
            loadersRegistration: otherLoadersRegistration,
            unmanagedAssemblyNameProcessor: unmanagedAssemblyPathProcessor
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