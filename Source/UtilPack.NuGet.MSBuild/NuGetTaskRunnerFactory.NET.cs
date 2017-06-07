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
using TTaskTypeGenerationParameters = System.ValueTuple<System.Boolean, System.Collections.Generic.IDictionary<System.String, System.ValueTuple<UtilPack.NuGet.MSBuild.WrappedPropertyKind, UtilPack.NuGet.MSBuild.WrappedPropertyInfo>>>;
using TTaskInstanceCreationInfo = System.ValueTuple<UtilPack.NuGet.MSBuild.TaskReferenceHolder, UtilPack.NuGet.MSBuild.ResolverLogger>;
using System.Reflection.Emit;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace UtilPack.NuGet.MSBuild
{
   partial class NuGetTaskRunnerFactory
   {

      private NuGetTaskExecutionHelper CreateExecutionHelper(
         Microsoft.Build.Framework.IBuildEngine taskFactoryLoggingHost,
         XElement taskBodyElement,
         String taskName,
         NuGetResolverWrapper nugetResolver,
         String assemblyPath,
         IDictionary<String, ISet<String>> assemblyPathsBySimpleName
         )
      {
         var assemblyDir = Path.GetDirectoryName( assemblyPath );
         var aSetup = new AppDomainSetup()
         {
            ApplicationBase = assemblyDir,
            ConfigurationFile = assemblyPath + ".config"
         };
         var appDomain = AppDomain.CreateDomain( "Executing task \"" + assemblyPath + "\".", AppDomain.CurrentDomain.Evidence, aSetup );
         var thisAssemblyPath = Path.GetFullPath( new Uri( this.GetType().Assembly.CodeBase ).LocalPath );
         var bootstrapper = (AssemblyLoadHelper) appDomain.CreateInstanceFromAndUnwrap(
               thisAssemblyPath,
               typeof( AssemblyLoadHelper ).FullName,
               false,
               0,
               null,
               new Object[] { },
               null,
               null
               );

         // We can't pass NET45NuGetResolver to AssemblyLoadHelper constructor directly, because that type is in this assembly, which is outside app domains application path.
         // And we can't register to AssemblyResolve because of that reason too.
         // Alternatively we could just make new type which binds AssemblyLoadHelper and NET45NuGetResolver, but let's go with this for now.
         var logger = new ResolverLogger( taskFactoryLoggingHost, nugetResolver.Resolver.NuGetLogger );
         bootstrapper.Initialize( assemblyPathsBySimpleName, nugetResolver, assemblyPath, taskName, logger );
         return new NET45ExecutionHelper(
            taskName,
            appDomain,
            bootstrapper,
            logger
            );
      }

      private sealed class NET45ExecutionHelper : NuGetTaskExecutionHelper
      {
         private readonly AppDomain _domain;
         private readonly AssemblyLoadHelper _bootstrapper;
         private readonly TaskReferenceHolder _taskRef;
         private readonly IDictionary<String, (WrappedPropertyKind, WrappedPropertyInfo)> _propertyInfos;
         private readonly ResolverLogger _logger;

         public NET45ExecutionHelper(
            String taskName,
            AppDomain domain,
            AssemblyLoadHelper bootstrapper,
            ResolverLogger logger
            )
         {
            this._domain = domain;
            this._bootstrapper = bootstrapper;
            // Doing typeof( Microsoft.Build.Framework.ITask ) in original MSBuild appdomain will result in correct MSBuild assembly to be used.
            // However, doing so in task's target domain, at least at the moment, will result in 14.0 version to be loaded from GAC, since net45 build depends on 14.0 MSBuild.
            this._taskRef = bootstrapper.CreateTaskReferenceHolder(
               taskName,
               typeof( Microsoft.Build.Framework.ITask ).Assembly.GetName().FullName,
               out var taskUsesDynamicLoading
               );
            if ( this._taskRef == null )
            {
               throw new Exception( $"Failed to load type {taskName}." );
            }
            this.TaskUsesDynamicLoading = taskUsesDynamicLoading;
            this._propertyInfos = this._taskRef.GetPropertyInfo().ToDictionary( kvp => kvp.Key, kvp => TaskReferenceHolder.DecodeKindAndInfo( kvp.Value ) );
            this._logger = logger;
         }

         public TTaskTypeGenerationParameters GetTaskTypeGenerationParameters()
         {
            return (this._taskRef.IsCancelable, this._propertyInfos);
         }

         public Microsoft.Build.Framework.TaskPropertyInfo[] GetTaskParameters()
         {
            return this._propertyInfos
               .Select( kvp =>
               {
                  var propType = GetPropertyType( kvp.Value.Item1 );
                  var info = kvp.Value.Item2;
                  return propType == null ?
                     null :
                     new Microsoft.Build.Framework.TaskPropertyInfo( kvp.Key, propType, info == WrappedPropertyInfo.Out, info == WrappedPropertyInfo.Required );
               } )
               .Where( propInfo => propInfo != null )
               .ToArray();
         }

         public TTaskInstanceCreationInfo GetTaskInstanceCreationInfo()
         {
            return (this._taskRef, this._logger);
         }

         public Boolean TaskUsesDynamicLoading { get; }

         public void Dispose()
         {
            this._bootstrapper.DisposeSafely();
            try
            {
               AppDomain.Unload( this._domain );
            }
            catch
            {
               // Ignore
            }
         }
      }
   }

   // Instances of this class reside in target task app domain, so we must be careful not to use any UtilPack stuff here! So no ArgumentValidator. etc.
   internal sealed class AssemblyLoadHelper : MarshalByRefObject, IDisposable
   {
      private sealed class NET45AssemblyHelper : CommonAssemblyRelatedHelper
      {
         private readonly ResolverLogger _logger;

         public NET45AssemblyHelper(
            IDictionary<String, ISet<String>> assemblyPathsBySimpleName,
            NuGetResolverWrapper resolver,
            String targetAssemblyPath,
            String taskName,
            ResolverLogger logger
            ) : base( assemblyPathsBySimpleName, resolver, targetAssemblyPath, taskName )
         {
            this._logger = logger;
         }

         protected override Assembly LoadAssemblyFromPath( String path )
         {
            return Assembly.LoadFile( path );
         }

         protected override void LogResolveMessage( String message )
         {
            this._logger.Log( message );
         }
      }

      private NET45AssemblyHelper _helper;

      public AssemblyLoadHelper()
      {
         AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
      }

      internal void Initialize(
         IDictionary<String, ISet<String>> assemblyPathsBySimpleName,
         NuGetResolverWrapper resolver,
         String targetAssemblyPath,
         String taskName,
         ResolverLogger logger
         )
      {
         this._helper = new NET45AssemblyHelper(
            assemblyPathsBySimpleName,
            resolver,
            targetAssemblyPath,
            taskName,
            logger
         );
      }

      public TaskReferenceHolder CreateTaskReferenceHolder(
         String taskName,
         String msbuildFrameworkAssemblyName,
         out Boolean taskUsesDynamicLoading
         )
      {
         Type taskType; ConstructorInfo ctor; Object[] ctorParams;
         (taskType, ctor, ctorParams, taskUsesDynamicLoading) = this._helper.LoadTaskType();
         return taskType == null ? null : new TaskReferenceHolder( ctor.Invoke( ctorParams ), msbuildFrameworkAssemblyName );
      }

      public void Dispose()
      {
         this._helper.DisposeSafely();
         AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
      }

      private Assembly CurrentDomain_AssemblyResolve( Object sender, ResolveEventArgs args )
      {
         if ( this._helper == null )
         {
            // This should *only* happen when calling Initialize method.
            return this.GetType().Assembly;
         }
         else
         {
            return this._helper.PerformAssemblyResolve( new AssemblyName( args.Name ) );
         }
      }
   }

}
#endif