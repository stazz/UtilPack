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
               this._thisNuGetVersion = nugetAssembly.GetName().Version;
               var versionOverride = Environment.GetEnvironmentVariable("UTILPACK_NUGET_VERSION");
               if (String.IsNullOrEmpty(versionOverride) || !Version.TryParse(versionOverride, out taskFactoryVersion)) {
                 taskFactoryVersion = this._thisNuGetVersion;
               }
               
               if ( !File.Exists(GetTaskFactoryFilePath( thisDir, thisName, taskFactoryVersion ) ) )
               {
                  // Fallback to deducing suitable version automatically
                  taskFactoryVersion = null;
               }
            }
            
            if ( taskFactoryVersion == null )
            {
               var allVersions = GetAllAvailableTaskFactoryVersions( thisDir, thisName );

               // Remember that allVersions is already sorted from newest to oldest, so .FirstOrDefault is enough
               var nv = this._thisNuGetVersion;
               // Try first bind with version which has same major + minor, and build number >= this build number
               taskFactoryVersion = allVersions.FirstOrDefault( v => v.Major == nv.Major && v.Minor == nv.Minor && v.Build >= nv.Build )
                  ?? ( allVersions.FirstOrDefault( v => v.Major == nv.Major && v.Minor == nv.Minor ) // Try to bind with version which just has same major + minor components
                   ?? ( allVersions.FirstOrDefault( v => v.Major == nv.Major ) // Try to bind with version which just has same major component
                     ?? allVersions.FirstOrDefault() // Bind to newest
                     ) );
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
      
      private static Version[] GetAllAvailableTaskFactoryVersions( String thisDir, String thisName )
      {
         const String DLL = ".dll";
         var retVal = Directory
           .EnumerateFiles( thisDir, thisName + ".*" + DLL, SearchOption.TopDirectoryOnly )
           .Select( fp =>
           {
              var endIdx = fp.LastIndexOf( '.' );
              var startIdx = fp.LastIndexOf( thisName, endIdx - 1 );
              startIdx += thisName.Length + THIS_NAME_SUFFIX.Length;
              try
              {
                 return Version.Parse( fp.Substring( startIdx, endIdx - startIdx ) );
              }
              catch
              {
                 throw new Exception( fp + "\n" + startIdx + ":" + endIdx );
              }
           } )
           .ToArray();

         Array.Sort( retVal, ( v1, v2 ) => -v1.CompareTo( v2 ) );

         return retVal;
      }
      
      private const Int32 DEFAULT_VERSION_COMPONENT_COUNT = 3; // Major, minor,build
      
      private static String GetTaskFactoryFilePath( String thisDir, String thisName, Version version )
      {
         return Path.Combine( thisDir, thisName + THIS_NAME_SUFFIX + ExtractVersionString( version, DEFAULT_VERSION_COMPONENT_COUNT ) + ".dll" );
      }
      
      private static Boolean VersionsMatch(Version v1, Version v2, Int32 componentCount)
      {
         return String.Equals( ExtractVersionString(v1, componentCount), ExtractVersionString(v2, componentCount) );
      }
      
      private static String ExtractVersionString( Version v, Int32 componentCount )
      {
         return v?.ToString( componentCount );
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
            if ( this._thisNuGetVersion != null && !VersionsMatch( this._thisNuGetVersion, this._taskFactoryNuGetVersion, DEFAULT_VERSION_COMPONENT_COUNT) )
            {
               String message = $"There is a mismatch between SDK NuGet version ({ ExtractVersionString( this._thisNuGetVersion, DEFAULT_VERSION_COMPONENT_COUNT ) }) and the NuGet version the task factory was compiled against ({ ExtractVersionString(this._taskFactoryNuGetVersion, DEFAULT_VERSION_COMPONENT_COUNT ) }).";
               if (VersionsMatch(this._thisNuGetVersion, this._taskFactoryNuGetVersion, 2)) {
                  // Only build version mismatch
                 taskFactoryLoggingHost.LogMessageEvent( new BuildMessageEventArgs(
                     message,
                     null,
                     null,
                     MessageImportance.High
                     )
                    );
               } else {
                 // Minor and/or major version mismatch
                 taskFactoryLoggingHost.LogWarningEvent(
                    new BuildWarningEventArgs(
                       "Task factory",
                       "NMSBT010",
                       null,
                       -1,
                       -1,
                       -1,
                       -1,
                       message + " There might occur some exotic errors.",
                       null,
                       nameof( NuGetTaskRunnerFactory )
                    ) );
               }
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
