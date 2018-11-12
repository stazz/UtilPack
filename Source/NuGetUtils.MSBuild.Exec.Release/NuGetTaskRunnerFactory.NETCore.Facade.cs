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

namespace NuGetUtils.MSBuild.Exec
{
   public sealed class NuGetTaskRunnerFactory : ITaskFactory
   {
      private readonly Version _thisNuGetVersion;
      private readonly Version _taskFactoryNuGetVersion;
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
           // TODO it is most likely actually an error if we can't see NuGet assembly here, need to figure this out properly later.
         }
         
         Version taskFactoryVersion = null;
         var thisPath = Path.GetFullPath( new Uri( this.GetType().GetTypeInfo().Assembly.CodeBase ).LocalPath );
         var thisDir = Path.GetDirectoryName( thisPath );
         var thisName = Path.GetFileNameWithoutExtension( thisPath );

         try
         {
            if ( nugetAssembly != null )
            {
               taskFactoryVersion = this._thisNuGetVersion = nugetAssembly.GetName().Version;
               if ( !File.Exists(GetTaskFactoryFilePath( thisDir, thisName, taskFactoryVersion ) ) )
               {
                  // Fallback to using newest available version
                  taskFactoryVersion = null;
               }
            }
            
            if ( taskFactoryVersion == null )
            {
               taskFactoryVersion = GetNewestAvailableTaskFactoryVersion( thisDir, thisName );
            }
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
              this._loaded = (ITaskFactory)Activator.CreateInstance( thisLoader.LoadFromAssemblyPath( GetTaskFactoryFilePath( thisDir, thisName, taskFactoryVersion ) ).GetType( this.GetType().FullName ) );
            }
            catch ( Exception exc)
            {
               // Ignore, but save
               this._error = exc;
            }
         }
         
         this._taskFactoryNuGetVersion = taskFactoryVersion;
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
      
      private static String GetTaskFactoryFilePath( String thisDir, String thisName, Version version )
      {
         return Path.Combine( thisDir, thisName + THIS_NAME_SUFFIX + ExtractVersionString( version ) + ".dll" );
      }
      
      private static Boolean VersionsMatch(Version v1, Version v2)
      {
         return String.Equals( ExtractVersionString(v1), ExtractVersionString(v2) );
      }
      
      private static String ExtractVersionString(Version v)
      {
         return v?.ToString( 3 );
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
                 "Task factory",
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
            if ( this._thisNuGetVersion != null && !VersionsMatch( this._thisNuGetVersion, this._taskFactoryNuGetVersion) )
            {
               taskFactoryLoggingHost.LogWarningEvent(
                  new BuildWarningEventArgs(
                    "Task factory",
                    "NMSBT010",
                    null,
                    -1,
                    -1,
                    -1,
                    -1,
                    $"There is a mismatch between SDK NuGet version ({ ExtractVersionString( this._thisNuGetVersion ) }) and the NuGet version the task factory was compiled against ({ ExtractVersionString(this._taskFactoryNuGetVersion ) }). There might occur some exotic errors.",
                    null,
                    nameof( NuGetTaskRunnerFactory )
                 ) );
            }
            
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
