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
using TPropertyInfo = System.ValueTuple<System.ValueTuple<UtilPack.NuGet.MSBuild.WrappedPropertyKind, UtilPack.NuGet.MSBuild.WrappedPropertyInfo>, System.Func<System.Object>, System.Action<System.Object>, System.Func<System.String, System.Object>>;
using System.Reflection.Emit;
using TNuGetPackageResolverCallback = System.Func<System.String, System.String, System.String[], System.Boolean, System.String, System.Reflection.Assembly>;
using TAssemblyByPathResolverCallback = System.Func<System.String, System.Reflection.Assembly>;
using TNuGetResolverCallback = System.Func<System.String, System.String, System.String[], System.Boolean, System.Tuple<System.Collections.Generic.Dictionary<System.String, System.String[]>, System.String, System.String>>;

namespace UtilPack.NuGet.MSBuild
{
   partial class NuGetTaskRunnerFactory
   {

      private NuGetTaskExecutionHelper CreateExecutionHelper(
         Microsoft.Build.Framework.IBuildEngine taskFactoryLoggingHost,
         String taskName,
         NuGetBoundResolver nugetResolver,
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
               new Object[] { thisAssemblyPath, assemblyPath, assemblyPathsBySimpleName },
               null,
               null
               );


         // We can't pass NET45NuGetResolver to AssemblyLoadHelper constructor directly, because that type is in this assembly, which is outside app domains application path.
         // And we can't register to AssemblyResolve because of that reason too.
         // Alternatively we could just make new type which binds AssemblyLoadHelper and NET45NuGetResolver, but let's go with this for now.
         var logger = new ResolverLogger( taskFactoryLoggingHost );
         bootstrapper.Initialize( new NET45NuGetResolver( nugetResolver ), logger );

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
            this._taskRef = bootstrapper.CreateTaskReferenceHolder( taskName, typeof( Microsoft.Build.Framework.ITask ).Assembly.GetName().FullName );
            if ( this._taskRef == null )
            {
               throw new Exception( $"Failed to load type {taskName}." );
            }
            this._propertyInfos = this._taskRef.GetPropertyInfo().ToDictionary( kvp => kvp.Key, kvp => TaskReferenceHolder.DecodeKindAndInfo( kvp.Value ) );
            this._logger = logger;
         }

         public Type GetTaskType()
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
            var ab = AssemblyBuilder.DefineDynamicAssembly( new AssemblyName( "NuGetTaskWrapperDynamicAssembly" ), AssemblyBuilderAccess.RunAndCollect );
            var mb = ab.DefineDynamicModule( "NuGetTaskWrapperDynamicAssembly.dll", false );
            var tb = mb.DefineType( "NuGetTaskWrapper", TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public );
            tb.AddInterfaceImplementation( typeof( Microsoft.Build.Framework.ITask ) );
            // TODO implement ICancelableTask if target type also implements it

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
            var requiredAttribute = typeof( Microsoft.Build.Framework.RequiredAttribute ).GetConstructor( new Type[] { } );
            var outAttribute = typeof( Microsoft.Build.Framework.OutputAttribute ).GetConstructor( new Type[] { } );
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
            foreach ( var kvp in this._propertyInfos )
            {
               (var kind, var info) = kvp.Value;
               Type propType;
               switch ( kind )
               {
                  case WrappedPropertyKind.String:
                  case WrappedPropertyKind.StringNoConversion:
                     propType = typeof( String );
                     break;
                  case WrappedPropertyKind.TaskItem:
                     propType = typeof( Microsoft.Build.Framework.ITaskItem );
                     break;
                  case WrappedPropertyKind.TaskItem2:
                     propType = typeof( Microsoft.Build.Framework.ITaskItem2 );
                     break;
                  case WrappedPropertyKind.BuildEngine:
                     propType = typeof( Microsoft.Build.Framework.IBuildEngine );
                     break;
                  case WrappedPropertyKind.TaskHost:
                     propType = typeof( Microsoft.Build.Framework.ITaskHost );
                     break;
                  default:
                     throw new Exception( $"Property handling code has changed, unknown wrapped property kind: {kind}." );

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
               il.Emit( OpCodes.Call, typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.Execute ) ) );
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
               il.Emit( OpCodes.Ldloc, retValLocal );
            }
            else
            {
               // return this._task.Execute();
               // TODO out parameters
               il.Emit( OpCodes.Ldarg_0 );
               il.Emit( OpCodes.Ldfld, taskField );
               il.Emit( OpCodes.Call, typeof( TaskReferenceHolder ).GetMethod( nameof( TaskReferenceHolder.Execute ) ) );
               il.Emit( OpCodes.Ret );
            }
            // We are ready
            return tb.CreateType();
         }

         public Microsoft.Build.Framework.TaskPropertyInfo[] GetTaskParameters()
         {
            return this._propertyInfos
               .Select( kvp =>
               {
                  Type propType;
                  switch ( kvp.Value.Item1 )
                  {
                     case WrappedPropertyKind.String:
                     case WrappedPropertyKind.StringNoConversion:
                        propType = typeof( String );
                        break;
                     case WrappedPropertyKind.TaskItem:
                        propType = typeof( Microsoft.Build.Framework.ITaskItem );
                        break;
                     case WrappedPropertyKind.TaskItem2:
                        propType = typeof( Microsoft.Build.Framework.ITaskItem2 );
                        break;
                     default:
                        propType = null;
                        break;
                  }
                  var info = kvp.Value.Item2;
                  return propType == null ?
                     null :
                     new Microsoft.Build.Framework.TaskPropertyInfo( kvp.Key, propType, info == WrappedPropertyInfo.Out, info == WrappedPropertyInfo.Required );
               } )
               .Where( propInfo => propInfo != null )
               .ToArray();
         }

         public Object CreateTaskInstance( Type taskType, Microsoft.Build.Framework.IBuildEngine taskFactoryLoggingHost )
         {
            return taskType.GetConstructors()[0].Invoke( new Object[] { this._taskRef, this._logger } );
         }

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

      private readonly String _callerAssemblyPath;
      private readonly String _targetAssemblyPath;
      // TODO make key AssemblyName instead of String... But that would require IO-operation for each dependency on start-up, resulting in quite a bit of slowness.
      // Another option would be to make value type into List<Lazy<Assembly>> and find first suitable (matching given assembly name).
      private readonly IDictionary<String, IDictionary<String, Lazy<Assembly>>> _assemblyPathsBySimpleName; // We will get multiple requests to load same assembly, so cache them
      private readonly String _thisAssemblyName;

      private NET45NuGetResolver _resolver;
      private ResolverLogger _logger;

      public AssemblyLoadHelper(
         String callerAssemblyPath,
         String targetAssemblyPath,
         IDictionary<String, ISet<String>> assemblyPathsBySimpleName
         )
      {
         this._callerAssemblyPath = callerAssemblyPath;
         this._targetAssemblyPath = targetAssemblyPath;
         this._assemblyPathsBySimpleName = assemblyPathsBySimpleName.ToDictionary(
            kvp => kvp.Key,
            kvp => (IDictionary<String, Lazy<Assembly>>) kvp.Value.ToDictionary( fullPath => fullPath, fullPath => new Lazy<Assembly>( () => Assembly.LoadFile( fullPath ) ) )
         );
         this._thisAssemblyName = this.GetType().Assembly.FullName;
         AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
      }

      public TaskReferenceHolder CreateTaskReferenceHolder(
         String taskName,
         String msbuildFrameworkAssemblyName
         )
      {
         var taskAssembly = Assembly.Load( AssemblyName.GetAssemblyName( this._targetAssemblyPath ) );
         var taskType = taskAssembly.GetType( taskName, false, false );
         return taskType == null ? null : new TaskReferenceHolder( this.CreateTaskReferenceHolderInstance( taskType ), msbuildFrameworkAssemblyName );
      }

      internal void Initialize( NET45NuGetResolver resolver, ResolverLogger logger )
      {
         Interlocked.CompareExchange( ref this._resolver, resolver, null );
         Interlocked.CompareExchange( ref this._logger, logger, null );
      }

      private Object CreateTaskReferenceHolderInstance( Type type )
      {
         var ctors = type.GetConstructors();
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

         return matchingCtor.Invoke( ctorParams );
      }

      public void Dispose()
      {
         AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
      }

      private Assembly CurrentDomain_AssemblyResolve( Object sender, ResolveEventArgs args )
      {
         var assemblyName = new AssemblyName( args.Name );
         Assembly retVal;
         if ( String.Equals( this._thisAssemblyName, args.Name ) )
         {
            // Happens when calling Initialize method (because the original appdomain has this assembly loaded "normally", but this app-domain has this assembly loaded via file path because of CreateInstanceFrom method.)
            retVal = this.GetType().Assembly;
         }
         else if ( this._assemblyPathsBySimpleName.TryGetValue( assemblyName.Name, out var assemblyLazies ) )
         {
            retVal = assemblyLazies.FirstOrDefault( kvp =>
            {
               var defName = kvp.Value.IsValueCreated ? kvp.Value.Value.GetName() : AssemblyName.GetAssemblyName( kvp.Key );

               return AssemblyNamesMatch( assemblyName, defName );
            } ).Value?.Value;

            this._logger.Log( retVal == null ?
               $"Assembly reference did not match definition for \"{args.Name}\", considered \"{String.Join( ";", assemblyLazies.Keys )}\"." :
               $"Found \"{args.Name}\" by simple name \"{assemblyName.Name}\" in \"{retVal.CodeBase}\"." );
         }
         else
         {
            this._logger.Log( $"Failed to find \"{args.Name}\" by simple name \"{assemblyName.Name}\"." );
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

      // This method can get called by target task to dynamically load nuget assemblies.
      private Assembly LoadNuGetAssembly(
         String packageID,
         String packageVersion,
         String[] repositories,
         Boolean loadDependencies,
         String assemblyPath
         )
      {
         var assemblyInfos = this._resolver.ResolveNuGetPackageAssemblies( packageID, packageVersion, repositories, loadDependencies, out var packageKey, out var packagePath );
         Assembly retVal = null;
         if ( assemblyInfos != null )
         {
            var assembliesBySimpleName = this._assemblyPathsBySimpleName;
            foreach ( var kvp in assemblyInfos )
            {
               foreach ( var nugetAssemblyPath in kvp.Value )
               {
                  var curPath = nugetAssemblyPath;
                  assembliesBySimpleName
                     .GetOrAdd_NotThreadSafe( Path.GetFileNameWithoutExtension( curPath ), sn => new Dictionary<String, Lazy<Assembly>>() )
                     .GetOrAdd_NotThreadSafe( curPath, cp => new Lazy<Assembly>( () => Assembly.LoadFile( cp ) ) );
               }
            }

            var possibleAssemblyPaths = assemblyInfos[packageKey];
            assemblyPath = NuGetBoundResolver.GetAssemblyPathFromNuGetAssemblies( possibleAssemblyPaths, packagePath, assemblyPath );
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
               .GetOrAdd_NotThreadSafe( Path.GetFileNameWithoutExtension( assemblyPath ), ap => new Dictionary<String, Lazy<Assembly>>() )
               .GetOrAdd_NotThreadSafe( assemblyPath, ap => new Lazy<Assembly>( () => Assembly.LoadFile( ap ) ) )
               .Value;
         }
         return retVal;
      }
   }

   // Instances of this class reside in target task app domain, so we must be careful not to use any UtilPack stuff here! So no ArgumentValidator. etc.
   public sealed class TaskReferenceHolder : MarshalByRefObject
   {
      private const String MBF = "Microsoft.Build.Framework.";

      private readonly Object _task;
      private readonly MethodInfo _executeMethod;
      private readonly IDictionary<String, TPropertyInfo> _propertyInfos;

      public TaskReferenceHolder( Object task, String msbuildFrameworkAssemblyName )
      {
         this._task = task ?? throw new Exception( "Failed to create the task object." );
         this._executeMethod = this._task.GetType().GetInterfaces()
            .Where( iFace => iFace.Assembly.GetName().FullName.Equals( msbuildFrameworkAssemblyName ) && iFace.FullName.Equals( MBF + nameof( Microsoft.Build.Framework.ITask ) ) )
            .First().GetMethods().First( m => m.Name.Equals( nameof( Microsoft.Build.Framework.ITask.Execute ) ) && m.GetParameters().Length == 0 && m.ReturnType.FullName.Equals( "System.Boolean" ) );

         // the name of the microsoft.build.framework assembly in this app domain.
         // Actually, we can't do this. Doing typeof( Microsoft.Build.Framework.ITask ).Assembly.GetName().FullName; will cause MSBuild 14.0 assembly to be loaded, if target assembly is .netstandard assembly.
         // This most likely due the fact that this is net45 build, and net45 only supports msbuild 14.X (msbuild 15.X requires net46).
         // So, just get the msbuildTaskAssemblyName from original appdomain
         // That is why MBF string consts & other helper constructs exist, and why we can't cast stuff directly to Microsoft.Build.Framework types.

         var propInfo = new Dictionary<String, TPropertyInfo>();
         // Iterate all public properties of the type
         foreach ( var property in task.GetType().GetRuntimeProperties().Where( p => ( p.GetMethod?.IsPublic ?? false ) && ( p.SetMethod?.IsPublic ?? false ) ) )
         {
            var curProperty = property;
            var propertyType = curProperty.PropertyType;
            var actualType = propertyType;
            if ( actualType.IsArray )
            {
               actualType = actualType.GetElementType();
            }
            Func<String, Object> converter;
            WrappedPropertyKind? kind;
            switch ( Type.GetTypeCode( actualType ) )
            {
               case TypeCode.Object:
                  if ( actualType.Assembly.GetName().FullName.Equals( msbuildFrameworkAssemblyName ) )
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
                  converter = null;
                  break;
               case TypeCode.DBNull:
               case TypeCode.Empty:
                  kind = null;
                  converter = null;
                  break;
               case TypeCode.String:
                  kind = WrappedPropertyKind.StringNoConversion;
                  converter = null;
                  break;
               default:
                  kind = WrappedPropertyKind.String;
                  converter = obj => Convert.ChangeType( obj, propertyType );
                  break;
            }

            if ( kind.HasValue )
            {
               WrappedPropertyInfo info;
               var customMBFAttrs = curProperty.GetCustomAttributes( true )
                  .Where( ca => ca.GetType().Assembly.GetName().FullName.Equals( msbuildFrameworkAssemblyName ) )
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

               propInfo.Add( curProperty.Name, (
                  (kind.Value, info),
                  new Func<Object>( () => curProperty.GetMethod.Invoke( this._task, null ) ),
                  new Action<Object>( val => curProperty.SetMethod.Invoke( this._task, new[] { val } ) ),
                  converter)
                  );
            }
         }

         this._propertyInfos = propInfo;
      }

      // Passing value tuples thru appdomain boundaries is errorprone, so just use normal integers here
      internal IDictionary<String, Int32> GetPropertyInfo()
      {
         return this._propertyInfos.ToDictionary( kvp => kvp.Key, kvp => EncodeKindAndInfo( kvp.Value.Item1.Item1, kvp.Value.Item1.Item2 ) );
      }

      public Object GetProperty( String propertyName )
      {
         return this._propertyInfos.TryGetValue( propertyName, out var info ) ?
            info.Item2() :
            throw new InvalidOperationException( $"Property \"{propertyName}\" is not supported for this task." );
      }

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

   // Instances of this class reside in task factory app domain.
   internal sealed class NET45NuGetResolver : MarshalByRefObject
   {
      private readonly NuGetBoundResolver _resolver;

      public NET45NuGetResolver( NuGetBoundResolver resolver )
      {
         this._resolver = resolver;
      }

      public IDictionary<String, String[]> ResolveNuGetPackageAssemblies(
         String packageID,
         String packageVersion,
         String[] repositoryPaths,
         Boolean loadDependencies,
         out String givenPackageString,
         out String packageExpandedPath
         )
      {
         var retVal = this._resolver.ResolveNuGetPackages( packageID, packageVersion, repositoryPaths, loadDependencies, out var package );
         givenPackageString = package?.ToString();
         packageExpandedPath = package?.ExpandedPath;
         return retVal.ToDictionary( kvp => kvp.Key.ToString(), kvp => kvp.Value );
      }
   }

   // Instances of this class reside in task factory app domain.
   // Has to be public, since it is used by dynamically generated task type.
   public sealed class ResolverLogger : MarshalByRefObject
   {
      private const Int32 USING_TASK_FACTORY_BE = 0;
      private const Int32 TASK_BE_INITIALIZING = 1;
      private const Int32 TASK_BE_READY = 2;

      private Microsoft.Build.Framework.IBuildEngine _be;
      private Int32 _state;
      private readonly List<String> _queuedMessages;

      public ResolverLogger( Microsoft.Build.Framework.IBuildEngine be )
      {
         this._be = be;
         this._queuedMessages = new List<String>();
      }

      // This is called by generated task type in its IBuildEngine setter
      public void TaskBuildEngineSet( Microsoft.Build.Framework.IBuildEngine be )
      {
         if ( be != null && Interlocked.CompareExchange( ref this._state, TASK_BE_INITIALIZING, USING_TASK_FACTORY_BE ) == USING_TASK_FACTORY_BE )
         {
            Interlocked.Exchange( ref this._be, be );
         }
      }

      // This is called by generated task type in its Execute method start
      public void TaskBuildEngineIsReady()
      {
         if ( Interlocked.CompareExchange( ref this._state, TASK_BE_READY, TASK_BE_INITIALIZING ) == TASK_BE_INITIALIZING )
         {
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
            case USING_TASK_FACTORY_BE:
            case TASK_BE_READY:
               this._be.LogMessageEvent( new Microsoft.Build.Framework.BuildMessageEventArgs(
                  message,
                  null,
                  "NuGetPackageAssemblyResolver",
                  Microsoft.Build.Framework.MessageImportance.Low,
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

}
#endif