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
using NuGet.Common;
using NuGet.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using NuGet.Versioning;
using System.Reflection;
using NuGet.Frameworks;

using TPropertyInfo = System.ValueTuple<UtilPack.NuGet.MSBuild.WrappedPropertyKind, UtilPack.NuGet.MSBuild.WrappedPropertyInfo, System.Reflection.PropertyInfo>;
using TTaskPropertyInfo = System.ValueTuple<System.ValueTuple<UtilPack.NuGet.MSBuild.WrappedPropertyKind, UtilPack.NuGet.MSBuild.WrappedPropertyInfo>, System.Func<System.Object>, System.Action<System.Object>, System.Func<System.String, System.Object>>;
using TNuGetPackageResolverCallback = System.Func<System.String, System.String, System.String, System.Threading.Tasks.Task<System.Reflection.Assembly>>;
using TAssemblyByPathResolverCallback = System.Func<System.String, System.Reflection.Assembly>;
using System.Collections.Concurrent;
using NuGet.Configuration;
using NuGet.ProjectModel;
using NuGet.Commands;
using NuGet.Protocol.Core.Types;
using NuGet.Packaging;
using NuGet.LibraryModel;

using TResolveResult = System.Collections.Generic.IDictionary<System.String, UtilPack.NuGet.MSBuild.ResolvedPackageInfo>;
using System.Reflection.Emit;
using System.Threading;

using TTaskTypeGenerationParameters = System.ValueTuple<System.Boolean, System.Collections.Generic.IDictionary<System.String, System.ValueTuple<UtilPack.NuGet.MSBuild.WrappedPropertyKind, UtilPack.NuGet.MSBuild.WrappedPropertyInfo>>>;
using TTaskInstanceCreationInfo = System.ValueTuple<UtilPack.NuGet.MSBuild.TaskReferenceHolder, UtilPack.NuGet.MSBuild.ResolverLogger>;

namespace UtilPack.NuGet.MSBuild
{
   public partial class NuGetTaskRunnerFactory : ITaskFactory
   {
      private sealed class TaskReferenceHolderInfo : IDisposable
      {
         private readonly Lazy<IDictionary<String, (WrappedPropertyKind, WrappedPropertyInfo)>> _propertyInfo;
         private readonly Action _dispose;

         public TaskReferenceHolderInfo(
            TaskReferenceHolder taskRef,
            ResolverLogger resolverLogger,
            Action dispose
            )
         {
            this.TaskReference = taskRef;
            this.Logger = resolverLogger;
            this._dispose = dispose;
            this._propertyInfo = new Lazy<IDictionary<string, (WrappedPropertyKind, WrappedPropertyInfo)>>( () => taskRef.GetPropertyInfo().ToDictionary( kvp => kvp.Key, kvp => TaskReferenceHolder.DecodeKindAndInfo( kvp.Value ) ) );
         }

         public TaskReferenceHolder TaskReference { get; }

         public ResolverLogger Logger { get; }

         public IDictionary<String, (WrappedPropertyKind, WrappedPropertyInfo)> PropertyInfo => this._propertyInfo.Value;

         public void Dispose()
         {
            this._dispose?.Invoke();
         }
      }

      private const String PACKAGE_ID = "PackageID";
      private const String PACKAGE_VERSION = "PackageVersion";
      private const String ASSEMBLY_PATH = "AssemblyPath";
      private const String NUGET_FW = "NuGetFramework";

      // We will re-create anything that needs re-creating between mutiple task usages from this same lazy.
      private ReadOnlyResettableLazy<TaskReferenceHolderInfo> _helper;

      // We will generate task type only exactly once, no matter how many times the actual task is created.
      private readonly Lazy<Type> _taskType;

      // Logger for this task factory
      private IBuildEngine _logger;

      public NuGetTaskRunnerFactory()
      {
         this._taskType = new Lazy<Type>( () => GenerateTaskType( (this._helper.Value.TaskReference.IsCancelable, this._helper.Value.PropertyInfo) ) );
      }

      public String FactoryName => nameof( NuGetTaskRunnerFactory );

      public Type TaskType
      {
         get
         {
            return this._taskType.Value;
         }
      }

      public void CleanupTask( ITask task )
      {
         if ( this._helper.IsValueCreated && this._helper.Value.TaskReference.TaskUsesDynamicLoading )
         {
            // In .NET Desktop, task factory logger seems to become invalid almost immediately after initialize method, so...
            // Don't log.

            //this._logger.LogMessageEvent( new BuildMessageEventArgs(
            //   "Cleaning up task since it was detected to be using dynamic loading.",
            //   null,
            //   this.FactoryName,
            //   MessageImportance.Normal,
            //   DateTime.UtcNow
            //   ) );

            // Reset tasks that do dynamic NuGet package assembly loading
            // On .NET Desktop, this will cause app domain unload
            // On .NET Core, this will cause assembly load context to be disposed
            this._helper.Value.DisposeSafely();
            this._helper.Reset();
         }
      }

      public ITask CreateTask(
         IBuildEngine taskFactoryLoggingHost
         )
      {
         return (ITask) this._taskType.Value.GetConstructors()[0].Invoke( new Object[] { this._helper.Value.TaskReference, this._helper.Value.Logger } );
      }

      public TaskPropertyInfo[] GetTaskParameters()
      {
         return this._helper.Value.PropertyInfo
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

      public Boolean Initialize(
         String taskName,
         IDictionary<String, TaskPropertyInfo> parameterGroup,
         String taskBody,
         IBuildEngine taskFactoryLoggingHost
         )
      {
         this._logger = taskFactoryLoggingHost;

         var taskBodyElement = XElement.Parse( taskBody );

         var nugetFrameworkFromProjectFile = taskBodyElement.Element( NUGET_FW )?.Value;
         var thisFW = this.GetNuGetFrameworkForRuntime( taskFactoryLoggingHost );

         var nugetResolver = new NuGetBoundResolver(
            taskFactoryLoggingHost,
            this.FactoryName,
            taskBodyElement,
            thisFW,
            ( lib, libs ) => GetSuitableFiles( thisFW, lib, libs )
            );
         String packageID;
         var resolveInfo = nugetResolver.ResolveNuGetPackages(
            ( packageID = taskBodyElement.Element( PACKAGE_ID )?.Value ),
            taskBodyElement.Element( "PackageVersion" )?.Value
            ).GetAwaiter().GetResult();

         var retVal = false;
         if ( resolveInfo != null
            && resolveInfo.TryGetValue( packageID, out var assemblyPaths )
            )
         {
            var assemblyPath = CommonHelpers.GetAssemblyPathFromNuGetAssemblies(
               assemblyPaths.Assemblies, assemblyPaths.PackageDirectory, taskBodyElement.Element( "AssemblyPath" )?.Value );
            if ( !String.IsNullOrEmpty( assemblyPath ) )
            {
               var resolverWrapper = new NuGetResolverWrapper( nugetResolver );
               taskName = this.ProcessTaskName( taskBodyElement, taskName );
               var deps = GroupDependenciesBySimpleAssemblyName( resolveInfo );
#if !NET45
               var platformFrameworkPaths = nugetResolver.ResolveNuGetPackages(
                  taskBodyElement.Element( NuGetBoundResolver.NUGET_FW_PACKAGE_ID )?.Value ?? "Microsoft.NETCore.App", // This value is hard-coded in Microsoft.NET.Sdk.Common.targets, and currently no proper API exists to map NuGetFrameworks into package ID (+ version combination).
                  taskBodyElement.Element( NuGetBoundResolver.NUGET_FW_PACKAGE_VERSION )?.Value ?? "1.1.2",
                  ( lib, libs ) => lib.CompileTimeAssemblies.Select( i => i.Path )
               ).GetAwaiter().GetResult();
#endif

               this._helper = LazyFactory.NewReadOnlyResettableLazy( () => this.CreateExecutionHelper(
                   taskFactoryLoggingHost,
                   taskBodyElement,
                   taskName,
                   resolverWrapper,
                   assemblyPath,
                   deps,
                   new ResolverLogger( nugetResolver.NuGetLogger )
#if !NET45
                   , platformFrameworkPaths
#endif
                   ), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication );
               retVal = true;
            }
            else
            {
               taskFactoryLoggingHost.LogErrorEvent(
                  new BuildErrorEventArgs(
                     "Task factory error",
                     "NMSBT003",
                     null,
                     -1,
                     -1,
                     -1,
                     -1,
                     $"Failed to find suitable assembly in {packageID}.",
                     null,
                     this.FactoryName
                  )
               );
            }
         }
         else
         {
            taskFactoryLoggingHost.LogErrorEvent(
               new BuildErrorEventArgs(
                  "Task factory error",
                  "NMSBT002",
                  null,
                  -1,
                  -1,
                  -1,
                  -1,
                  $"Failed to find main package, check that you have suitable {PACKAGE_ID} element in task body and that package is installed.",
                  null,
                  this.FactoryName
               )
            );
         }
         return retVal;
      }

      private static IEnumerable<String> GetSuitableFiles(
         NuGetFramework thisFramework,
         LockFileTargetLibrary targetLibrary,
         Lazy<IDictionary<String, LockFileLibrary>> libraries
         )
      {
         var runtime = targetLibrary.RuntimeAssemblies;
         IEnumerable<String> retVal;
         if ( runtime.Count > 0 )
         {
            retVal = runtime.Select( i => i.Path );
         }
         else if ( libraries.Value.TryGetValue( targetLibrary.Name, out var lib ) )
         {
            // targetLibrary does not list stuff like build/net45/someassembly.dll
            // So let's do manual matching
            var fwGroups = lib.Files.Where( f =>
            {
               return f.StartsWith( PackagingConstants.Folders.Build, StringComparison.OrdinalIgnoreCase )
                      && PackageHelper.IsAssembly( f )
                      && Path.GetDirectoryName( f ).Length > PackagingConstants.Folders.Build.Length + 1;
            } ).GroupBy( f =>
            {
               try
               {
                  return NuGetFramework.ParseFolder( f.Split( '/' )[1] );
               }
               catch
               {
                  return null;
               }
            } )
           .Where( g => g.Key != null )
           .Select( g => new FrameworkSpecificGroup( g.Key, g ) );

            var matchingGroup = NuGetFrameworkUtility.GetNearest(
               fwGroups,
               thisFramework,
               g => g.TargetFramework );
            retVal = matchingGroup?.Items;
         }
         else
         {
            retVal = null;
         }

         return retVal;
      }

      private String ProcessTaskName(
         XElement taskBodyElement,
         String taskName
         )
      {
         var overrideTaskName = taskBodyElement.Element( "TaskName" )?.Value;
         return String.IsNullOrEmpty( overrideTaskName ) ? taskName : taskName;
      }

      internal static IDictionary<String, ISet<String>> GroupDependenciesBySimpleAssemblyName(
         TResolveResult packageDependencyInfo
         )
      {
         var retVal = new Dictionary<String, ISet<String>>();

         foreach ( var kvp in packageDependencyInfo )
         {
            foreach ( var packageAssemblyPath in kvp.Value.Assemblies )
            {
               var simpleName = System.IO.Path.GetFileNameWithoutExtension( packageAssemblyPath );
               if ( !retVal.TryGetValue( simpleName, out ISet<String> allPaths ) )
               {
                  allPaths = new HashSet<String>();
                  retVal.Add( simpleName, allPaths );
               }

               allPaths.Add( packageAssemblyPath );
            }
         }
         return retVal;
      }


      private NuGetFramework GetNuGetFrameworkForRuntime(
         IBuildEngine be
         )
      {
#if NET45
         return Assembly.GetEntryAssembly().GetNuGetFrameworkFromAssembly();
#else
         var fwName = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
         NuGetFramework retVal = null;
         var senderName = this.FactoryName;
         if ( !String.IsNullOrEmpty( fwName ) && fwName.StartsWith( ".NET Core" ) )
         {
            if ( Version.TryParse( fwName.Substring( 10 ), out var netCoreVersion ) )
            {
               if ( netCoreVersion.Major == 4 )
               {
                  if ( netCoreVersion.Minor == 0 )
                  {
                     retVal = FrameworkConstants.CommonFrameworks.NetCoreApp10;
                  }
                  else if ( netCoreVersion.Minor == 6 )
                  {
                     retVal = FrameworkConstants.CommonFrameworks.NetCoreApp11;
                  }
               }
            }
            else
            {
               be.LogWarningEvent( new BuildWarningEventArgs(
                  "NuGetFrameworkError",
                  "NMSBT004",
                  null,
                  -1,
                  -1,
                  -1,
                  -1,
                  $"Failed to parse version from .NET Core framework \"{fwName}\".",
                  null,
                  senderName
                  ) );
            }
         }
         else
         {
            be.LogWarningEvent( new BuildWarningEventArgs(
               "NuGetFrameworkError",
               "NMSBT003",
               null,
               -1,
               -1,
               -1,
               -1,
               $"Unrecognized framework name: \"{fwName}\", try specifying NuGet framework and package strings that describes this process runtime in <Task> element (using \"{NUGET_FW}\", \"{NuGetBoundResolver.NUGET_FW_PACKAGE_ID}\", and \"{NuGetBoundResolver.NUGET_FW_PACKAGE_VERSION}\" elements)!",
               null,
               senderName
               ) );
         }

         if ( retVal == null )
         {
            retVal = FrameworkConstants.CommonFrameworks.NetCoreApp11;
            be.LogWarningEvent( new BuildWarningEventArgs(
               "NuGetFrameworkError",
               "NMSBT005",
               null,
               -1,
               -1,
               -1,
               -1,
               $"Failed to automatically deduct NuGet framework of running process, defaulting to \"{retVal}\". Expect possible failures.",
               null,
               senderName
               ) );
         }

         return retVal;
#endif
      }

      private static Type GenerateTaskType( TTaskTypeGenerationParameters parameters )
      {
         // Since we are executing task in different app domain, our task type must inherit MarshalByRefObject
         // However, we don't want to impose such restriction to task writers - ideal situation would be for task writer to only target .netstandard 1.3 (or .netstandard1.4+ and .net45+, but we still don't want to make such restriction).
         // Furthermore, tasks which only target .netstandard 1.3 don't even have MarshalByRefObject.
         // So, let's generate our own dynamic task type.

         // We should load the actual task type in different domain and collect all public properties with getter and setter.
         // Then, we generate type with same property names, but property types should be Either String or ITaskItem[].
         // All getter and setter logic is forwarded by this generated type to our TaskReferenceHolder class, inheriting MarshalByRefObject and residing in actual task's AppDomain.
         // The TaskReferenceHolder will take care of converting required stuff.

         // public class NuGetTaskWrapper : ITask
         // {
         //    private readonly TaskReferenceHolder _task;
         //
         //    public String SomeProperty
         //    {
         //       get
         //       {
         //           return this._task.GetProperty("SomeProperty");
         //       }
         //       set
         //       {
         //           this._task.SetProperty("SomeProperty", value);
         //       }
         //     }
         //     ...
         // }

         var isCancelable = parameters.Item1;
         var propertyInfos = parameters.Item2;

         var ab = AssemblyBuilder.DefineDynamicAssembly( new AssemblyName( "NuGetTaskWrapperDynamicAssembly" ), AssemblyBuilderAccess.RunAndCollect );
         var mb = ab.DefineDynamicModule( "NuGetTaskWrapperDynamicAssembly.dll"
#if NET45
               , false
#endif
               );
         var tb = mb.DefineType( "NuGetTaskWrapper", TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public );
         tb.AddInterfaceImplementation( typeof( ITask ) );

         var taskField = tb.DefineField( "_task", typeof( TaskReferenceHolder ), FieldAttributes.Private | FieldAttributes.InitOnly );
         var loggerField = tb.DefineField( "_logger", typeof( ResolverLogger ), FieldAttributes.Private | FieldAttributes.InitOnly );

         // Constructor
         var ctor = tb.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
            CallingConventions.HasThis,
            new Type[] { typeof( TaskReferenceHolder ), typeof( ResolverLogger ) }
            );
         var il = ctor.GetILGenerator();
         il.Emit( OpCodes.Ldarg_0 );
         il.Emit( OpCodes.Call, typeof( Object ).GetConstructor( new Type[] { } ) );

         il.Emit( OpCodes.Ldarg_0 );
         il.Emit( OpCodes.Ldarg_1 );
         il.Emit( OpCodes.Stfld, taskField );

         il.Emit( OpCodes.Ldarg_0 );
         il.Emit( OpCodes.Ldarg_2 );
         il.Emit( OpCodes.Stfld, loggerField );

         il.Emit( OpCodes.Ret );
         // Properties
         var taskRefGetter = typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.GetProperty ) );
         var taskRefSetter = typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.SetProperty ) );
         var toStringCall = typeof( Convert ).GetMethod( nameof( Convert.ToString ), new Type[] { typeof( Object ) } );
         var requiredAttribute = typeof( RequiredAttribute ).GetConstructor( new Type[] { } );
         var outAttribute = typeof( OutputAttribute ).GetConstructor( new Type[] { } );
         var beSetter = typeof( ResolverLogger ).GetMethod( nameof( ResolverLogger.TaskBuildEngineSet ) );
         var beReady = typeof( ResolverLogger ).GetMethod( nameof( ResolverLogger.TaskBuildEngineIsReady ) );
         if ( taskRefGetter == null )
         {
            throw new Exception( "Internal error: no property getter." );
         }
         else if ( taskRefSetter == null )
         {
            throw new Exception( "Internal error: no property getter." );
         }
         else if ( toStringCall == null )
         {
            throw new Exception( "Internal error: no Convert.ToString." );
         }
         else if ( requiredAttribute == null )
         {
            throw new Exception( "Internal error: no Required attribute constructor." );
         }
         else if ( outAttribute == null )
         {
            throw new Exception( "Internal error: no Out attribute constructor." );
         }
         else if ( beSetter == null )
         {
            throw new Exception( "Internal error: no log setter." );
         }
         else if ( beReady == null )
         {
            throw new Exception( "Internal error: no log state updater." );
         }

         var outPropertyInfos = new List<(String, WrappedPropertyKind, Type, FieldBuilder)>();
         void EmitPropertyConversionCode( ILGenerator curIL, WrappedPropertyKind curKind, Type curPropType )
         {
            if ( curKind != WrappedPropertyKind.StringNoConversion )
            {
               // Emit conversion
               if ( curKind == WrappedPropertyKind.String )
               {
                  // Call to Convert.ToString
                  il.Emit( OpCodes.Call, toStringCall );
               }
               else
               {
                  // Just cast
                  il.Emit( OpCodes.Castclass, curPropType );
               }
            }
         }
         foreach ( var kvp in propertyInfos )
         {
            (var kind, var info) = kvp.Value;
            var propType = GetPropertyType( kind );
            if ( propType == null )
            {
               switch ( kind )
               {
                  case WrappedPropertyKind.BuildEngine:
                     propType = typeof( IBuildEngine );
                     break;
                  case WrappedPropertyKind.TaskHost:
                     propType = typeof( ITaskHost );
                     break;
                  default:
                     throw new Exception( $"Property handling code has changed, unknown wrapped property kind: {kind}." );
               }
            }

            var methodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig;
            if ( kind == WrappedPropertyKind.BuildEngine || kind == WrappedPropertyKind.TaskHost )
            {
               // Virtual is required for class methods implementing interface methods
               methodAttributes |= MethodAttributes.Virtual;
            }

            var getter = tb.DefineMethod(
               "get_" + kvp.Key,
               methodAttributes
               );
            getter.SetReturnType( propType );
            il = getter.GetILGenerator();

            if ( info == WrappedPropertyInfo.Out )
            {
               var outField = tb.DefineField( "_out" + outPropertyInfos.Count, propType, FieldAttributes.Private );
               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldfld, outField );
               outPropertyInfos.Add( (kvp.Key, kind, propType, outField) );
            }
            else
            {
               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldfld, taskField );
               il.Emit( OpCodes.Ldstr, kvp.Key );
               il.Emit( OpCodes.Callvirt, taskRefGetter );
               EmitPropertyConversionCode( il, kind, propType );
            }
            il.Emit( OpCodes.Ret );

            MethodBuilder setter;
            if ( info == WrappedPropertyInfo.Out )
            {
               setter = null;
            }
            else
            {
               setter = tb.DefineMethod(
                  "set_" + kvp.Key,
                  methodAttributes
                  );
               setter.SetParameters( new Type[] { propType } );
               il = setter.GetILGenerator();
               if ( kind == WrappedPropertyKind.BuildEngine )
               {
                  // Update the logger
                  il.Emit( OpCodes.Ldarg_0 );
                  il.Emit( OpCodes.Ldfld, loggerField );
                  il.Emit( OpCodes.Ldarg_1 );
                  il.Emit( OpCodes.Callvirt, beSetter );
               }

               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldfld, taskField );
               il.Emit( OpCodes.Ldstr, kvp.Key );
               il.Emit( OpCodes.Ldarg_1 );
               il.Emit( OpCodes.Callvirt, taskRefSetter );
               il.Emit( OpCodes.Ret );
            }
            var prop = tb.DefineProperty(
               kvp.Key,
               PropertyAttributes.None,
               propType,
               new Type[] { }
               );
            prop.SetGetMethod( getter );
            if ( setter != null )
            {
               prop.SetSetMethod( setter );
            }

            switch ( info )
            {
               case WrappedPropertyInfo.Required:
                  prop.SetCustomAttribute( new CustomAttributeBuilder( requiredAttribute, new object[] { } ) );
                  break;
               case WrappedPropertyInfo.Out:
                  prop.SetCustomAttribute( new CustomAttributeBuilder( outAttribute, new object[] { } ) );
                  break;
            }
         }
         // Execute method
         var execute = tb.DefineMethod(
            nameof( Microsoft.Build.Framework.ITask.Execute ),
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
            typeof( Boolean ),
            new Type[] { }
            );
         il = execute.GetILGenerator();
         il.Emit( OpCodes.Ldarg_0 );
         il.Emit( OpCodes.Ldfld, loggerField );
         il.Emit( OpCodes.Callvirt, beReady );

         if ( outPropertyInfos.Count > 0 )
         {
            // try { return this._task.Execute(); } finally { this.OutProperty = this._task.GetProperty( "Out" ); }
            var retValLocal = il.DeclareLocal( typeof( Boolean ) );
            il.Emit( OpCodes.Ldc_I4_0 );
            il.Emit( OpCodes.Stloc, retValLocal );
            il.BeginExceptionBlock();
            il.Emit( OpCodes.Ldarg_0 );
            il.Emit( OpCodes.Ldfld, taskField );
            il.Emit( OpCodes.Callvirt, typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.Execute ) ) );
            il.Emit( OpCodes.Stloc, retValLocal );
            il.BeginFinallyBlock();
            foreach ( var outSetter in outPropertyInfos )
            {
               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldfld, taskField );
               il.Emit( OpCodes.Ldstr, outSetter.Item1 );
               il.Emit( OpCodes.Callvirt, taskRefGetter );
               EmitPropertyConversionCode( il, outSetter.Item2, outSetter.Item3 );
               il.Emit( OpCodes.Stfld, outSetter.Item4 );
            }
            il.EndExceptionBlock();

            il.Emit( OpCodes.Ldloc, retValLocal );
         }
         else
         {
            // return this._task.Execute();
            il.Emit( OpCodes.Ldarg_0 );
            il.Emit( OpCodes.Ldfld, taskField );
            il.Emit( OpCodes.Tailcall );
            il.Emit( OpCodes.Callvirt, typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.Execute ) ) );
         }
         il.Emit( OpCodes.Ret );

         // Canceability
         if ( isCancelable )
         {
            tb.AddInterfaceImplementation( typeof( Microsoft.Build.Framework.ICancelableTask ) );
            var cancel = tb.DefineMethod(
               nameof( Microsoft.Build.Framework.ICancelableTask.Cancel ),
               MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
               typeof( void ),
               new Type[] { }
               );
            var cancelMethod = typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.Cancel ) );
            if ( cancelMethod == null )
            {
               throw new Exception( "Internal error: no cancel." );
            }
            il = cancel.GetILGenerator();
            // Call cancel to TaskReferenceHolder which will forward it to actual task
            il.Emit( OpCodes.Ldarg_0 );
            il.Emit( OpCodes.Ldfld, taskField );
            il.Emit( OpCodes.Tailcall );
            il.Emit( OpCodes.Callvirt, cancelMethod );
            il.Emit( OpCodes.Ret );
         }

         // We are ready
         return tb.
#if NET45
            CreateType()
#else 
            CreateTypeInfo().AsType()
#endif
            ;



      }

      private static Type GetPropertyType( WrappedPropertyKind kind )
      {
         switch ( kind )
         {
            case WrappedPropertyKind.String:
            case WrappedPropertyKind.StringNoConversion:
               return typeof( String );
            case WrappedPropertyKind.TaskItem:
               return typeof( Microsoft.Build.Framework.ITaskItem[] );
            case WrappedPropertyKind.TaskItem2:
               return typeof( Microsoft.Build.Framework.ITaskItem2[] );
            default:
               return null;
         }
      }
   }

   // Instances of this class reside in target task app domain, so we must be careful not to use any UtilPack stuff here! So no ArgumentValidator. etc.
   public sealed class TaskReferenceHolder
#if NET45
      : MarshalByRefObject
#endif
   {
      private readonly Object _task;
      private readonly MethodInfo _executeMethod;
      private readonly MethodInfo _cancelMethod;
      private readonly IDictionary<String, TTaskPropertyInfo> _propertyInfos;

      public TaskReferenceHolder( Object task, String msbuildFrameworkAssemblyName, Boolean taskUsesDynamicLoading )
      {
         this._task = task ?? throw new Exception( "Failed to create the task object." );
         this.TaskUsesDynamicLoading = taskUsesDynamicLoading;
         var mbfInterfaces = this._task.GetType().GetInterfaces()
            .Where( iFace => iFace
#if !NET45
            .GetTypeInfo()
#endif
            .Assembly.GetName().FullName.Equals( msbuildFrameworkAssemblyName ) )
            .ToArray();
         // TODO explicit implementations
         this._executeMethod = mbfInterfaces
            .Where( iFace => iFace.FullName.Equals( CommonHelpers.MBF + nameof( Microsoft.Build.Framework.ITask ) ) )
            .First().GetMethods().First( m => m.Name.Equals( nameof( Microsoft.Build.Framework.ITask.Execute ) ) && m.GetParameters().Length == 0 && m.ReturnType.FullName.Equals( typeof( Boolean ).FullName ) );
         this._cancelMethod = mbfInterfaces
            .FirstOrDefault( iFace => iFace.FullName.Equals( CommonHelpers.MBF + nameof( Microsoft.Build.Framework.ICancelableTask ) ) )
            ?.GetMethods()?.First( m => m.Name.Equals( nameof( Microsoft.Build.Framework.ICancelableTask.Cancel ) ) && m.GetParameters().Length == 0 );

         this._propertyInfos = CommonHelpers.GetPropertyInfoFromType(
            task.GetType(),
            new AssemblyName( msbuildFrameworkAssemblyName )
            ).ToDictionary(
               kvp => kvp.Key,
               kvp =>
               {
                  var curProperty = kvp.Value.Item3;
                  var propType = curProperty.PropertyType;
                  var converter = kvp.Value.Item1 == WrappedPropertyKind.String ?
                     str => Convert.ChangeType( str, propType ) :
                     (Func<String, Object>) null;
                  return (
                  (kvp.Value.Item1, kvp.Value.Item2),
                  new Func<Object>( () => curProperty.GetMethod.Invoke( this._task, null ) ),
                  new Action<Object>( val => curProperty.SetMethod.Invoke( this._task, new[] { val } ) ),
                  converter);
               } );

      }

      // Passing value tuples thru appdomain boundaries is errorprone, so just use normal integers here
      internal IDictionary<String, Int32> GetPropertyInfo()
      {
         return this._propertyInfos.ToDictionary( kvp => kvp.Key, kvp => EncodeKindAndInfo( kvp.Value.Item1.Item1, kvp.Value.Item1.Item2 ) );
      }

      internal Boolean IsCancelable => this._cancelMethod != null;

      internal Boolean TaskUsesDynamicLoading { get; }

      // Called by generated task type
      public void Cancel()
      {
         this._cancelMethod.Invoke( this._task, null );
      }

      // Called by generated task type
      public Object GetProperty( String propertyName )
      {
         return this._propertyInfos.TryGetValue( propertyName, out var info ) ?
            info.Item2() :
            throw new InvalidOperationException( $"Property \"{propertyName}\" is not supported for this task." );
      }

      // Called by generated task type
      public void SetProperty( String propertyName, Object value )
      {
         if ( this._propertyInfos.TryGetValue( propertyName, out var info ) )
         {
            if ( info.Item4 != null )
            {
               value = info.Item4( (String) value );
            }
            info.Item3( value );
         }
         else
         {
            throw new InvalidOperationException( $"Property \"{propertyName}\" is not supported for this task." );
         }
      }

      // Called by generated task type
      public Boolean Execute()
      {
         // We can't cast to Microsoft.Build.Framework.ITask, since the 14.0 version will be loaded (from GAC), if target task assembly is netstandard assembly.
         // This is because this project depends on msbuild 14.3 in net45 build.

         // So... just invoke dynamically.
         return (Boolean) this._executeMethod.Invoke( this._task, null );
      }

      internal static Int32 EncodeKindAndInfo( WrappedPropertyKind kind, WrappedPropertyInfo info )
      {
         // 3 lowest bits to info and all upper bits to kind
         return ( ( (Int32) kind ) << 3 ) | ( ( (Int32) info ) & 0x03 );
      }

      internal static (WrappedPropertyKind, WrappedPropertyInfo) DecodeKindAndInfo( Int32 encoded )
      {
         return ((WrappedPropertyKind) ( ( encoded & 0xF8 ) >> 3 ), (WrappedPropertyInfo) ( ( encoded & 0x03 ) ));

      }
   }

   // Instances of this class reside in task factory app domain.
   // Has to be public, since it is used by dynamically generated task type.
   public sealed class ResolverLogger
#if NET45
      : MarshalByRefObject
#endif
   {
      private const Int32 INITIAL = 0;
      private const Int32 TASK_BE_INITIALIZING = 1;
      private const Int32 TASK_BE_READY = 2;

      private IBuildEngine _be;
      private Int32 _state;
      private readonly List<String> _queuedMessages;
      private readonly NuGetMSBuildLogger _nugetLogger;

      internal ResolverLogger( NuGetMSBuildLogger nugetLogger )
      {
         this._queuedMessages = new List<String>();
         this._nugetLogger = nugetLogger;
      }

      // This is called by generated task type in its IBuildEngine setter
      public void TaskBuildEngineSet( IBuildEngine be )
      {
         if ( be != null && Interlocked.CompareExchange( ref this._state, TASK_BE_INITIALIZING, INITIAL ) == INITIAL )
         {
            Interlocked.Exchange( ref this._be, be );
            this._nugetLogger.SetBuildEngine( null );
         }
      }

      // This is called by generated task type in its Execute method start
      public void TaskBuildEngineIsReady()
      {
         if ( Interlocked.CompareExchange( ref this._state, TASK_BE_READY, TASK_BE_INITIALIZING ) == TASK_BE_INITIALIZING )
         {
            this._nugetLogger.SetBuildEngine( this._be );
            // process all queued messages
            foreach ( var msg in this._queuedMessages )
            {
               this.Log( msg );
            }
            this._queuedMessages.Clear();
         }
      }

      public void Log( String message )
      {
         switch ( this._state )
         {
            case TASK_BE_READY:
               this._be.LogMessageEvent( new BuildMessageEventArgs(
                  message,
                  null,
                  "NuGetPackageAssemblyResolver",
                  MessageImportance.Low,
                  DateTime.UtcNow,
                  null
               ) );
               break;
            case TASK_BE_INITIALIZING:
               // When assembly resolve happens during task initialization (setting BuildEngine etc properties).
               // Using BuildEngine then will cause NullReferenceException as its LoggingContext property is not yet set.
               // And task factory logging context has already been marked inactive, so this is when we can't immediately log.
               // In this case, just queue message, and log them once task's Execute method has been invoked.
               this._queuedMessages.Add( message );
               break;
         }

      }
   }

   internal sealed class NuGetMSBuildLogger : global::NuGet.Common.ILogger
   {
      private const String CAT = "NuGetRestore";

      private IBuildEngine _be;

      public NuGetMSBuildLogger( IBuildEngine be )
      {
         this._be = be;
      }

      public void LogDebug( String data )
      {
         var args = new BuildMessageEventArgs( "[NuGet Debug]: " + data, null, CAT, MessageImportance.Low );
         this._be.LogMessageEvent( args );
      }

      public void LogError( String data )
      {
         var args = new BuildErrorEventArgs( CAT, "NR0001", null, -1, -1, -1, -1, "[NuGet Error]: " + data, null, CAT );
         this._be.LogErrorEvent( args );
      }

      public void LogErrorSummary( String data )
      {
         var args = new BuildErrorEventArgs( CAT, "NR0002", null, -1, -1, -1, -1, "[NuGet ErrorSummary]: " + data, null, CAT );
         this._be.LogErrorEvent( args );
      }

      public void LogInformation( String data )
      {
         var args = new BuildMessageEventArgs( "[NuGet Info]: " + data, null, CAT, MessageImportance.High );
         this._be.LogMessageEvent( args );
      }

      public void LogInformationSummary( String data )
      {
         var args = new BuildMessageEventArgs( "[NuGet InfoSummary]: " + data, null, CAT, MessageImportance.High );
         this._be.LogMessageEvent( args );
      }

      public void LogMinimal( String data )
      {
         var args = new BuildMessageEventArgs( "[NuGet Minimal]: " + data, null, CAT, MessageImportance.Low );
         this._be.LogMessageEvent( args );
      }

      public void LogVerbose( String data )
      {
         var args = new BuildMessageEventArgs( "[NuGet Verbose]: " + data, null, CAT, MessageImportance.Normal );
         this._be.LogMessageEvent( args );
      }

      public void LogWarning( String data )
      {
         var args = new BuildWarningEventArgs( CAT, "NR0003", null, -1, -1, -1, -1, "[NuGet Warning]: " + data, null, CAT );
         this._be.LogWarningEvent( args );
      }

      public void SetBuildEngine( IBuildEngine be )
      {
         System.Threading.Interlocked.Exchange( ref this._be, be );
      }
   }

   internal delegate IEnumerable<String> GetFileItemsDelegate( LockFileTargetLibrary targetLibrary, Lazy<IDictionary<String, LockFileLibrary>> libraries );

   internal sealed class NuGetBoundResolver : IDisposable
   {


      private const String NUGET_FW = "NuGetFramework";
      internal const String NUGET_FW_PACKAGE_ID = "NuGetFrameworkPackageID";
      internal const String NUGET_FW_PACKAGE_VERSION = "NuGetFrameworkPackageVersion";


      //private readonly ISettings _nugetSettings;
      private readonly SourceCacheContext _cacheContext;
      private readonly RestoreCommandProviders _restoreCommandProvider;
      private readonly String _nugetRestoreRootDir; // NuGet restore command never writes anything to disk (apart from packages themselves), but if certain file paths are omitted, it simply fails with argumentnullexception when invoking Path.Combine or Path.GetFullName. So this can be anything, really, as long as it's understandable by Path class.
      private readonly TargetFrameworkInformation _restoreTargetFW;
      private LockFile _previousLockFile;
      private readonly IDictionary<String, NuGetv3LocalRepository> _localRepos;
      private readonly GetFileItemsDelegate _defaultFileGetter;
      private readonly ConcurrentDictionary<String, ConcurrentDictionary<NuGetVersion, LockFile>> _allLockFiles;

      public NuGetBoundResolver(
         IBuildEngine be,
         String senderName,
         XElement taskBodyElement,
         NuGetFramework thisFramework,
         GetFileItemsDelegate defaultFileGetter = null
         )
      {
         this.ThisFramework = thisFramework;
         //be.LogMessageEvent( new BuildMessageEventArgs(
         //   $"Using {this.ThisFramework} as NuGet framework representing this runtime.",
         //   null,
         //   senderName,
         //   MessageImportance.High
         //   ) );

         String nugetConfig;
         ISettings nugetSettings;
         if ( String.IsNullOrEmpty( ( nugetConfig = taskBodyElement.Element( "NuGetConfigurationFile" )?.Value ) ) )
         {
            nugetSettings = Settings.LoadDefaultSettings( Path.GetDirectoryName( be.ProjectFileOfTaskNode ), null, new XPlatMachineWideSetting() );
         }
         else
         {
            var fp = Path.GetFullPath( nugetConfig );
            nugetSettings = Settings.LoadSpecificSettings( Path.GetDirectoryName( fp ), Path.GetFileName( fp ) );
         }

         var global = SettingsUtility.GetGlobalPackagesFolder( nugetSettings );
         var fallbacks = SettingsUtility.GetFallbackPackageFolders( nugetSettings );
         var ctx = new SourceCacheContext();
         var nugetLogger = new NuGetMSBuildLogger( be );
         var psp = new PackageSourceProvider( nugetSettings );
         var csp = new global::NuGet.Protocol.CachingSourceProvider( psp );
         this._cacheContext = ctx;
         this.NuGetLogger = nugetLogger;
         this._restoreCommandProvider = RestoreCommandProviders.Create(
            global,
            fallbacks,
            new PackageSourceProvider( nugetSettings ).LoadPackageSources().Where( s => s.IsEnabled ).Select( s => csp.CreateRepository( s ) ),
            ctx,
            nugetLogger
            );
         this._nugetRestoreRootDir = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() );
         this._restoreTargetFW = new TargetFrameworkInformation()
         {
            FrameworkName = this.ThisFramework
         };
         this._localRepos = this._restoreCommandProvider.GlobalPackages.Singleton().Concat( this._restoreCommandProvider.FallbackPackageFolders ).ToDictionary( r => r.RepositoryRoot, r => r );
         this._defaultFileGetter = defaultFileGetter ?? ( ( lib, libs ) => lib.RuntimeAssemblies.Select( i => i.Path ) );
         this._allLockFiles = new ConcurrentDictionary<String, ConcurrentDictionary<NuGetVersion, LockFile>>();
      }

      public NuGetFramework ThisFramework { get; }

      public NuGetMSBuildLogger NuGetLogger { get; }

      public async System.Threading.Tasks.Task<TResolveResult> ResolveNuGetPackages(
         String packageID,
         String version,
         GetFileItemsDelegate fileGetter = null
         )
      {
         // Prepare for invoking restore command
         TResolveResult retVal;
         if ( !String.IsNullOrEmpty( packageID ) )
         {
            VersionRange versionRange;
            if ( String.IsNullOrEmpty( version ) )
            {
               // Accept all versions, and pick the newest
               versionRange = VersionRange.AllFloating;
            }
            else
            {
               // Accept specific version range
               versionRange = VersionRange.Parse( version );
            }

            // Invoking restore command is quite expensive, so let's try to see if we have cached result
            LockFile lockFile = null;
            if ( !versionRange.IsFloating && this._allLockFiles.TryGetValue( packageID, out var thisPackageLockFiles ) )
            {
               var matchingActualVersion = versionRange.FindBestMatch( thisPackageLockFiles.Keys.Where( v => versionRange.Satisfies( v ) ) );
               if ( matchingActualVersion != null )
               {
                  lockFile = thisPackageLockFiles[matchingActualVersion];
               }
            }

            if ( lockFile == null )
            {
               lockFile = await this.PerformRestore( packageID, versionRange );
               var actualVersion = lockFile.Libraries.First( l => String.Equals( l.Name, packageID ) ).Version;
               this._allLockFiles
                  .GetOrAdd( packageID, p => new ConcurrentDictionary<NuGetVersion, LockFile>() )
                  .TryAdd( actualVersion, lockFile );
            }
            // We will always have only one target, since we are running restore always against one target framework
            retVal = new Dictionary<String, ResolvedPackageInfo>();
            if ( fileGetter == null )
            {
               fileGetter = this._defaultFileGetter;
            }
            var libDic = new Lazy<IDictionary<String, LockFileLibrary>>( () =>
            {
               return lockFile.Libraries.ToDictionary( lib => lib.Name, lib => lib );
            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication );
            foreach ( var targetLib in lockFile.Targets[0].Libraries )
            {
               var curLib = targetLib;
               var targetLibFullPath = lockFile.PackageFolders
                  .Select( f =>
                  {
                     return this._localRepos.TryGetValue( f.Path, out var curRepo ) ?
                        Path.Combine( curRepo.RepositoryRoot, curRepo.PathResolver.GetPackageDirectory( curLib.Name, curLib.Version ) ) :
                        null;
                  } )
                  .FirstOrDefault( fp => !String.IsNullOrEmpty( fp ) && Directory.Exists( fp ) );
               if ( !String.IsNullOrEmpty( targetLibFullPath ) )
               {
                  retVal.Add( curLib.Name, new ResolvedPackageInfo(
                     targetLibFullPath,
                     fileGetter( curLib, libDic )
                        ?.Select( p => Path.Combine( targetLibFullPath, p ) )
                        ?.ToArray() ?? Empty<String>.Array
                     ) );
               }
            }


            // Restore command never modifies existing lock file object, instead it creates new one
            // Just update to newest (thus the next request will be able to use cached information and be faster)
            System.Threading.Interlocked.Exchange( ref this._previousLockFile, lockFile );

         }
         else
         {
            retVal = null;
         }

         return retVal;
      }

      private async System.Threading.Tasks.Task<LockFile> PerformRestore(
         String packageID,
         VersionRange versionRange
         )
      {
         var spec = new PackageSpec()
         {
            Name = $"Restoring {packageID}",
            FilePath = Path.Combine( this._nugetRestoreRootDir, "dummy" )
         };
         spec.TargetFrameworks.Add( this._restoreTargetFW );

         spec.Dependencies.Add( new LibraryDependency()
         {
            LibraryRange = new LibraryRange( packageID, versionRange, LibraryDependencyTarget.Package )
         } );

         var request = new RestoreRequest(
            spec,
            this._restoreCommandProvider,
            this._cacheContext,
            this.NuGetLogger )
         {
            ProjectStyle = ProjectStyle.Standalone,
            RestoreOutputPath = this._nugetRestoreRootDir,
            ExistingLockFile = this._previousLockFile
         };
         return ( await ( new RestoreCommand( request ).ExecuteAsync() ) ).LockFile;
      }

      public void Dispose()
      {
         this._cacheContext.DisposeSafely();
      }
   }

#if NET45
   [Serializable] // We want to be serializable instead of MarshalByRef as we want to copy these objects
#endif
   internal sealed class ResolvedPackageInfo
   {
      public ResolvedPackageInfo( String packageDirectory, String[] assemblies )
      {
         this.PackageDirectory = packageDirectory;
         this.Assemblies = assemblies;
      }

      public String PackageDirectory { get; }
      public String[] Assemblies { get; }
   }

   // These methods are used by both .net45 and .netstandard.
   // This class has no implemented interfaces and extends System.Object.
   // Therefore using this static method from another appdomain won't cause any assembly resolves.
   internal static class CommonHelpers
   {
      internal const String MBF = "Microsoft.Build.Framework.";

      public static String GetAssemblyPathFromNuGetAssemblies(
         String[] assemblyPaths,
         String packageExpandedPath,
         String optionalGivenAssemblyPath
         )
      {
         String assemblyPath = null;
         if ( assemblyPaths.Length == 1 || (
               assemblyPaths.Length > 1 // There is more than 1 possible assembly
               && !String.IsNullOrEmpty( ( assemblyPath = optionalGivenAssemblyPath ) ) // AssemblyPath task property was given
               && ( assemblyPath = Path.GetFullPath( ( Path.Combine( packageExpandedPath, assemblyPath ) ) ) ).StartsWith( packageExpandedPath ) // The given assembly path truly resides in the package folder
               ) )
         {
            // TODO maybe check that assembly path is in possibleAssemblies array?
            if ( assemblyPath == null )
            {
               assemblyPath = assemblyPaths[0];
            }
         }
         return assemblyPath;
      }

      public static IDictionary<String, TPropertyInfo> GetPropertyInfoFromType(
         Type type,
         AssemblyName msbuildFrameworkAssemblyName
         )
      {
         // Doing typeof( Microsoft.Build.Framework.ITask ).Assembly.GetName().FullName; will cause MSBuild 14.0 assembly to be loaded in net45 build, if target assembly is .netstandard assembly.
         // This most likely due the fact that net45 build requires msbuild 14.X (msbuild 15.X requires net46).
         // So, just get the msbuildTaskAssemblyName from original appdomain as a parameter to this method.
         // That is why MBF string consts & other helper constructs exist, and why we can't cast stuff directly to Microsoft.Build.Framework types.


         var retVal = new Dictionary<String, TPropertyInfo>();
         foreach ( var property in type.GetRuntimeProperties().Where( p => ( p.GetMethod?.IsPublic ?? false ) && ( p.SetMethod?.IsPublic ?? false ) ) )
         {
            var curProperty = property;
            var propertyType = curProperty.PropertyType;
            var actualType = propertyType;
            if ( actualType.IsArray )
            {
               actualType = actualType.GetElementType();
            }
            WrappedPropertyKind? kind;
            switch ( Type.GetTypeCode( actualType ) )
            {
               case TypeCode.Object:
                  if ( ISMFBType( actualType, msbuildFrameworkAssemblyName ) )
                  {
                     if ( Equals( actualType.FullName, MBF + nameof( Microsoft.Build.Framework.IBuildEngine ) ) )
                     {
                        kind = WrappedPropertyKind.BuildEngine;
                     }
                     else if ( Equals( actualType.FullName, MBF + nameof( Microsoft.Build.Framework.ITaskHost ) ) )
                     {
                        kind = WrappedPropertyKind.TaskHost;
                     }
                     else if ( Equals( actualType.FullName, MBF + nameof( Microsoft.Build.Framework.ITaskItem ) ) )
                     {
                        kind = WrappedPropertyKind.TaskItem;
                     }
                     else if ( Equals( actualType.FullName, MBF + nameof( Microsoft.Build.Framework.ITaskItem2 ) ) )
                     {
                        kind = WrappedPropertyKind.TaskItem2;
                     }
                     else
                     {
                        kind = null;
                     }
                  }
                  else
                  {
                     kind = null;
                  }
                  break;
#if NET45
               case TypeCode.DBNull:
#endif
               case TypeCode.Empty:
                  kind = null;
                  break;
               case TypeCode.String:
                  kind = WrappedPropertyKind.StringNoConversion;
                  break;
               default:
                  kind = WrappedPropertyKind.String;
                  break;
            }

            if ( kind.HasValue )
            {
               WrappedPropertyInfo info;
               var customMBFAttrs = curProperty.GetCustomAttributes( true )
                  .Where( ca => ISMFBType( ca.GetType(), msbuildFrameworkAssemblyName ) )
                  .ToArray();
               if ( customMBFAttrs.Any( ca => Equals( ca.GetType().FullName, MBF + nameof( Microsoft.Build.Framework.RequiredAttribute ) ) ) )
               {
                  info = WrappedPropertyInfo.Required;
               }
               else if ( customMBFAttrs.Any( ca => Equals( ca.GetType().FullName, MBF + nameof( Microsoft.Build.Framework.OutputAttribute ) ) ) )
               {
                  info = WrappedPropertyInfo.Out;
               }
               else
               {
                  info = WrappedPropertyInfo.None;
               }

               retVal.Add( curProperty.Name, (kind.Value, info, curProperty) );
            }
         }

         return retVal;
      }

      private static Boolean ISMFBType( Type type, AssemblyName mfbAssembly )
      {
         var an = type
#if !NET45
                     .GetTypeInfo()
#endif
                     .Assembly.GetName();
         Byte[] pk;
         return String.Equals( an.Name, mfbAssembly.Name )
            && ( pk = an.GetPublicKeyToken() ) != null
            && mfbAssembly.GetPublicKeyToken().SequenceEqual( pk );
      }
   }

   internal enum WrappedPropertyKind
   {
      String,
      StringNoConversion,
      TaskItem,
      TaskItem2,
      BuildEngine,
      TaskHost
   }

   internal enum WrappedPropertyInfo
   {
      None,
      Required,
      Out
   }

   internal abstract class CommonAssemblyRelatedHelper : IDisposable
   {
      private readonly ConcurrentDictionary<String, ConcurrentDictionary<String, Lazy<Assembly>>> _assemblyPathsBySimpleName; // We will get multiple requests to load same assembly, so cache them
      //private readonly String _thisAssemblyName;
      protected readonly NuGetResolverWrapper _resolver;
      private readonly String _targetAssemblyPath;
      private readonly String _taskName;

      protected CommonAssemblyRelatedHelper(
         IDictionary<String, ISet<String>> assemblyPathsBySimpleName,
         NuGetResolverWrapper resolver,
         String targetAssemblyPath,
         String taskName
         )
      {
         this._assemblyPathsBySimpleName = new ConcurrentDictionary<String, ConcurrentDictionary<String, Lazy<Assembly>>>( assemblyPathsBySimpleName.Select(
            kvp => new KeyValuePair<String, ConcurrentDictionary<String, Lazy<Assembly>>>(
               kvp.Key,
               new ConcurrentDictionary<String, Lazy<Assembly>>( kvp.Value.Select( fullPath => new KeyValuePair<String, Lazy<Assembly>>( fullPath, new Lazy<Assembly>( () => this.LoadAssemblyFromPath( fullPath ), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) ) ) )
               )
            ) );
         this._resolver = resolver;
         this._targetAssemblyPath = targetAssemblyPath;
         this._taskName = taskName;
      }

      public virtual void Dispose()
      {
         this._assemblyPathsBySimpleName.Clear();
         this._resolver.Resolver.DisposeSafely();
      }

      internal protected Assembly PerformAssemblyResolve( AssemblyName assemblyName )
      {
         Assembly retVal;
         if ( this._assemblyPathsBySimpleName.TryGetValue( assemblyName.Name, out var assemblyLazies ) )
         {
            retVal = assemblyLazies.FirstOrDefault( kvp =>
            {
               var defName = kvp.Value.IsValueCreated ? kvp.Value.Value.GetName() :
#if !NET45
               System.Runtime.Loader.AssemblyLoadContext
#else
               AssemblyName
#endif
               .GetAssemblyName( kvp.Key );

               return AssemblyNamesMatch( assemblyName, defName );
            } ).Value?.Value;

            // Turn on logging when the problem related to resolving MSBuild.dll is solved
            // This method is called for "MSBuild" assembly, which is where task factory logging host type resides.
            // Then this method calls LogResolveMessage, which tries to load "MSBuild" assembly again, since it uses task factory logging host to log
            // -> Stack overflow
            // Returning 'null' for "MSBuild" will then cause exception which results in build fail.

            //this.LogResolveMessage( retVal == null ?
            //   $"Assembly reference did not match definition for \"{assemblyName}\", considered \"{String.Join( ";", assemblyLazies.Keys )}\"." :
            //   $"Found \"{assemblyName}\" by simple name \"{assemblyName.Name}\" in \"{retVal.CodeBase}\"." );
         }
         else
         {
            //this.LogResolveMessage( $"Failed to find \"{assemblyName}\" by simple name \"{assemblyName.Name}\"." );
            retVal = null;
         }
         return retVal;
      }

      private static Boolean AssemblyNamesMatch(
         AssemblyName reference,
         AssemblyName definition
         )
      {
         String refStr; String defStr;
         if ( reference.Flags.HasFlag( AssemblyNameFlags.Retargetable ) )
         {
            refStr = reference.Name;
            defStr = definition.Name;
         }
         else
         {
            refStr = reference.FullName;
            defStr = reference.FullName;
         }

         return String.Equals( refStr, defStr );
      }

      internal protected (Type, ConstructorInfo, Object[], Boolean) LoadTaskType()
      {
         var taskAssembly = this.PerformAssemblyResolve(
#if !NET45
            System.Runtime.Loader.AssemblyLoadContext.GetAssemblyName
#else
            AssemblyName.GetAssemblyName
#endif
            ( this._targetAssemblyPath )
            );

         var taskType = taskAssembly.GetType( this._taskName, false, false );

         (var taskCtor, var ctorParameters) = this.GetTaskConstructorInfo( taskType );

         return (taskType, taskCtor, ctorParameters, ( ctorParameters?.Length ?? 0 ) > 0);
      }

      private (ConstructorInfo, Object[]) GetTaskConstructorInfo(
         Type type
         )
      {
         var ctors = type
#if !NET45
            .GetTypeInfo()
#endif
            .GetConstructors();
         ConstructorInfo matchingCtor = null;
         Object[] ctorParams = null;
         if ( ctors.Length > 0 )
         {
            var ctorInfo = new Dictionary<Int32, IDictionary<ISet<Type>, ConstructorInfo>>();

            foreach ( var ctor in ctors )
            {
               var paramz = ctor.GetParameters();
               ctorInfo
                  .GetOrAdd_NotThreadSafe( paramz.Length, pl => new Dictionary<ISet<Type>, ConstructorInfo>( SetEqualityComparer<Type>.DefaultEqualityComparer ) )
                  .Add( new HashSet<Type>( paramz.Select( p => p.ParameterType ) ), ctor );
            }

            if (
               ctorInfo.TryGetValue( 2, out var curInfo )
               && curInfo.TryGetValue( new HashSet<Type>() { typeof( TNuGetPackageResolverCallback ), typeof( TAssemblyByPathResolverCallback ) }, out matchingCtor )
               )
            {
               ctorParams = new Object[2];
               ctorParams[Array.FindIndex( matchingCtor.GetParameters(), p => p.ParameterType.Equals( typeof( TNuGetPackageResolverCallback ) ) )] = (TNuGetPackageResolverCallback) this.LoadNuGetAssembly;
               ctorParams[Array.FindIndex( matchingCtor.GetParameters(), p => p.ParameterType.Equals( typeof( TAssemblyByPathResolverCallback ) ) )] = (TAssemblyByPathResolverCallback) this.LoadOtherAssembly;
            }
            else if ( ctorInfo.TryGetValue( 1, out curInfo ) )
            {
               if ( curInfo.TryGetValue( new HashSet<Type>( typeof( TNuGetPackageResolverCallback ).Singleton() ), out matchingCtor ) )
               {
                  ctorParams = new Object[] { (TNuGetPackageResolverCallback) this.LoadNuGetAssembly };
               }
               else if ( curInfo.TryGetValue( new HashSet<Type>( typeof( TAssemblyByPathResolverCallback ).Singleton() ), out matchingCtor ) )
               {
                  ctorParams = new Object[] { (TAssemblyByPathResolverCallback) this.LoadOtherAssembly };
               }
            }
            else if ( ctorInfo.TryGetValue( 0, out curInfo ) )
            {
               matchingCtor = curInfo.Values.First();
            }
         }

         if ( matchingCtor == null )
         {
            throw new Exception( $"No public suitable constructors found for type {type.AssemblyQualifiedName}." );
         }

         return (matchingCtor, ctorParams);
      }

      // This method can get called by target task to dynamically load nuget assemblies.
      private async System.Threading.Tasks.Task<Assembly> LoadNuGetAssembly(
         String packageID,
         String packageVersion,
         String assemblyPath
         )
      {
         // TODO Path.GetFileNameWithoutExtension( curPath ) should be replaced with AssemblyName.GetAssemblyName( String path ) for kinky situations when assembly name is with different casing than its file name.
         // Obviously, this slows down things by a lot, and will change data structures a bit, but it should be done at some point.

         var assemblyInfos = await
#if NET45
            this.UseResolver( packageID, packageVersion )
#else
            this._resolver.ResolveNuGetPackageAssemblies( packageID, packageVersion )
#endif
            ;

         Assembly retVal = null;
         if ( assemblyInfos != null )
         {
            var assembliesBySimpleName = this._assemblyPathsBySimpleName;
            foreach ( var kvp in assemblyInfos )
            {
               foreach ( var nugetAssemblyPath in kvp.Value.Assemblies )
               {
                  var curPath = nugetAssemblyPath;
                  assembliesBySimpleName
                     .GetOrAdd( Path.GetFileNameWithoutExtension( curPath ), sn => new ConcurrentDictionary<String, Lazy<Assembly>>() )
                     .TryAdd( curPath, new Lazy<Assembly>( () => this.LoadAssemblyFromPath( curPath ), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) );
               }
            }

            var possibleAssemblyPaths = assemblyInfos[packageID];
            assemblyPath = CommonHelpers.GetAssemblyPathFromNuGetAssemblies( possibleAssemblyPaths.Assemblies, possibleAssemblyPaths.PackageDirectory, assemblyPath );
            if ( !String.IsNullOrEmpty( assemblyPath ) )
            {
               retVal = assembliesBySimpleName
                  [Path.GetFileNameWithoutExtension( assemblyPath )]
                  [assemblyPath]
                  .Value;
            }
         }

         return retVal;
      }

      // This method can get called by target task to dynamically load assemblies by path.
      private Assembly LoadOtherAssembly(
         String assemblyPath
         )
      {
         assemblyPath = Path.GetFullPath( assemblyPath );
         Assembly retVal = null;
         if ( File.Exists( assemblyPath ) )
         {
            retVal = this._assemblyPathsBySimpleName
               .GetOrAdd( Path.GetFileNameWithoutExtension( assemblyPath ), ap => new ConcurrentDictionary<String, Lazy<Assembly>>() )
               .GetOrAdd( assemblyPath, ap => new Lazy<Assembly>( () => this.LoadAssemblyFromPath( ap ), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication ) )
               .Value;
         }
         return retVal;
      }

      protected abstract void LogResolveMessage( String message );

      protected abstract Assembly LoadAssemblyFromPath( String path );

      public Boolean IsMBFAssembly( AssemblyName an )
      {
         switch ( an.Name )
         {
            case "Microsoft.Build":
            case "Microsoft.Build.Framework":
            case "Microsoft.Build.Tasks.Core":
            case "Microsoft.Build.Utilities.Core":
               return true;
            default:
               return false;
         }
      }


#if NET45

      private System.Threading.Tasks.Task<TResolveResult> UseResolver(
         String packageID,
         String packageVersion
         )
      {
         var setter = new MarshaledResultSetter<TResolveResult>();
         this._resolver.ResolveNuGetPackageAssemblies( packageID, packageVersion, setter );
         return setter.Task;
      }
#endif

   }

#if NET45

   internal sealed class MarshaledResultSetter<T> : MarshalByRefObject
   {
      private readonly System.Threading.Tasks.TaskCompletionSource<T> _tcs;

      public MarshaledResultSetter()
      {
         this._tcs = new System.Threading.Tasks.TaskCompletionSource<T>();
      }

      public void SetResult( T result ) => this._tcs.SetResult( result );
      public System.Threading.Tasks.Task<T> Task => this._tcs.Task;
   }

#endif


   // Instances of this class reside in task factory app domain.
   internal sealed class NuGetResolverWrapper
#if NET45
      : MarshalByRefObject
#endif
   {

      public NuGetResolverWrapper(
         NuGetBoundResolver resolver
         )
      {
         this.Resolver = resolver;
      }

      public
#if NET45
         void
#else
         System.Threading.Tasks.Task<TResolveResult>
#endif
         ResolveNuGetPackageAssemblies(
         String packageID,
         String packageVersion
#if NET45
         , MarshaledResultSetter<TResolveResult> setter
#endif
         )
      {
#if NET45
         var task = this.PerformResolve( packageID, packageVersion );
         task.ContinueWith( prevTask =>
         {
            try
            {
               var dic = prevTask.Result;
               setter.SetResult( dic );
            }
            catch
            {
               setter.SetResult( null );
            }
         } );

#else
         return this.PerformResolve( packageID, packageVersion );
#endif
      }

      private async System.Threading.Tasks.Task<TResolveResult> PerformResolve(
         String packageID,
         String packageVersion
         )
      {
         return await this.Resolver.ResolveNuGetPackages( packageID, packageVersion );
      }

      public NuGetBoundResolver Resolver { get; }
   }
}
