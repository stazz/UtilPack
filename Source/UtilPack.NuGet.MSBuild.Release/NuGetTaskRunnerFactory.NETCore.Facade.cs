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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UtilPack.NuGet.MSBuild
{
   public sealed class NuGetTaskRunnerFactory : ITaskFactory
   {
      private readonly ITaskFactory _loaded;
      private readonly Exception _error;
      
      private const String THIS_NAME_SUFFIX = ".NuGet.";
      
      public NuGetTaskRunnerFactory()
      {
         var thisLoader = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext( this.GetType().GetTypeInfo().Assembly );
         // Load NuGet assembly
         Assembly nugetAssembly = null;
         try
         {
            nugetAssembly = thisLoader.LoadFromAssemblyName( new AssemblyName( "NuGet.Commands" ) );
         }
         catch
         {
           // Ignore, and then just use newest
         }
         
         Version taskFactoryVersion = null;
         var thisPath = Path.GetFullPath( new Uri( this.GetType().GetTypeInfo().Assembly.CodeBase ).LocalPath );
         var thisDir = Path.GetDirectoryName( thisPath );
         var thisName = Path.GetFileNameWithoutExtension( thisPath );

         try
         {
            taskFactoryVersion = nugetAssembly == null ?
               GetNewestAvailableTaskFactoryVersion( thisDir, thisName ) :
               GetTaskFactoryVersionFromNuGetAssembly( nugetAssembly );
         }
         catch ( Exception exc )
         {
            // Ignore, but save
            this._error = exc;
         }
         
         if ( taskFactoryVersion != null )
         {
            try
            {
              this._loaded = (ITaskFactory)Activator.CreateInstance( thisLoader.LoadFromAssemblyPath( Path.Combine( thisDir, thisName + THIS_NAME_SUFFIX + taskFactoryVersion.ToString( 3 ) + ".dll" ) ).GetType( this.GetType().FullName ) );
            }
            catch ( Exception exc)
            {
               // Ignore, but save
               this._error = exc;
            }
         }
      }
      
      private static Version GetTaskFactoryVersionFromNuGetAssembly( Assembly nugetAssembly )
      {
         return nugetAssembly.GetName().Version;
      }
      
      private static Version GetNewestAvailableTaskFactoryVersion( String thisDir, String thisName )
      {
         const String DLL = ".dll";
         return Directory
           .EnumerateFiles( thisDir, thisName + ".*" + DLL, SearchOption.TopDirectoryOnly )
           .Select( fp =>
           {
               var endIdx = fp.LastIndexOf( '.' );
               var startIdx = fp.LastIndexOf( thisName, endIdx - 1 );
               startIdx += thisName.Length + THIS_NAME_SUFFIX.Length;
               try {return Version.Parse( fp.Substring( startIdx, endIdx - startIdx ) ); } catch { throw new Exception(fp + "\n" + startIdx + ":" + endIdx ); }
            } )
            .OrderByDescending( v => v )
            .FirstOrDefault();
      }

      public Boolean Initialize(
         String taskName,
         IDictionary<String, TaskPropertyInfo> parameterGroup,
         String taskBody,
         IBuildEngine taskFactoryLoggingHost
         )
      {
         var loaded = this._loaded;
         Boolean retVal;
         if ( loaded == null )
         {
            retVal = false;
            taskFactoryLoggingHost.LogErrorEvent(
               new BuildErrorEventArgs(
                 "Task factory error",
                 "NMSBT000",
                 null,
                 -1,
                 -1,
                 -1,
                 -1,
                 $"Failed to load actual task factory assembly { this._error?.ToString() ?? "because of unspecified error" }.",
                 null,
                 nameof( NuGetTaskRunnerFactory )
              ) );
         }
         else
         {
            retVal = this._loaded.Initialize( taskName, parameterGroup, taskBody, taskFactoryLoggingHost );
         }
         
         return retVal;
      }

      public String FactoryName => this._loaded.FactoryName;

      public Type TaskType => this._loaded.TaskType;

      public void CleanupTask( ITask task )
      {
         this._loaded.CleanupTask( task );
      }

      public ITask CreateTask( IBuildEngine taskFactoryLoggingHost )
      {
         return this._loaded.CreateTask( taskFactoryLoggingHost );
      }

      public TaskPropertyInfo[] GetTaskParameters()
      {
         return this._loaded.GetTaskParameters();
      }

   }
}
