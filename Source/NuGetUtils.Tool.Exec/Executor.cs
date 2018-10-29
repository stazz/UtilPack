/*
 * Copyright 2018 Stanislav Muhametsin. All rights Reserved.
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
using Microsoft.Extensions.Configuration;
using NuGet.Frameworks;
using NuGetUtils.Lib.AssemblyResolving;
using NuGetUtils.Lib.EntryPoint;
using NuGetUtils.Lib.Restore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace NuGetUtils.Exec
{
   class NuGetEntryPointExecutor
   {
      internal const String LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR = ".nuget-exec-cache";
      internal const String LOCK_FILE_CACHE_DIR_ENV_NAME = "NUGET_EXEC_CACHE_DIR";

      private readonly String[] _args;
      private readonly NuGetExecutionConfiguration _programConfig;
      private readonly Boolean _isConfigConfig;

      public NuGetEntryPointExecutor(
         String[] args,
         NuGetExecutionConfiguration programConfig,
         Boolean isConfigConfig
         )
      {
         this._args = args;
         this._programConfig = programConfig;
         this._isConfigConfig = isConfigConfig;
      }



      public async Task<Int32> ExecuteMethod(
         CancellationToken token
         )
      {
         var programConfig = this._programConfig;
         Int32 retVal;
         var targetFWString = programConfig.RestoreFramework;

         using ( var restorer = new BoundRestoreCommandUser(
            UtilPackNuGetUtility.GetNuGetSettingsWithDefaultRootDirectory(
                  Path.GetDirectoryName( new Uri( typeof( Program ).GetTypeInfo().Assembly.CodeBase ).LocalPath ),
                  programConfig.NuGetConfigurationFile
               ),
            thisFramework: String.IsNullOrEmpty( targetFWString ) ? null : NuGetFramework.Parse( targetFWString ),
            nugetLogger: programConfig.DisableLogging ? null : new TextWriterLogger()
            {
               VerbosityLevel = programConfig.LogLevel
            },
            lockFileCacheDir: programConfig.LockFileCacheDirectory,
            lockFileCacheEnvironmentVariableName: LOCK_FILE_CACHE_DIR_ENV_NAME,
            getDefaultLockFileCacheDir: homeDir => Path.Combine( homeDir, LOCK_FILE_CACHE_DIR_WITHIN_HOME_DIR ),
            disableLockFileCacheDir: programConfig.DisableLockFileCache
            ) )
         {

            var thisFramework = restorer.ThisFramework;
            var sdkPackageID = thisFramework.GetSDKPackageID( programConfig.SDKFrameworkPackageID ); // Typically "Microsoft.NETCore.App"
            var sdkPackageVersion = thisFramework.GetSDKPackageVersion( sdkPackageID, programConfig.SDKFrameworkPackageVersion );
            var loadFromParentForCA = NuGetAssemblyResolverFactory.ReturnFromParentAssemblyLoaderForAssemblies( new[] { typeof( ConfiguredEntryPointAttribute ) } );

            using ( var assemblyLoader = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
               restorer,
               await restorer.RestoreIfNeeded(
                  sdkPackageID,
                  sdkPackageVersion,
                  token
                  ),
               out var loadContext,
               additionalCheckForDefaultLoader: an => loadFromParentForCA( an ) // || NuGetAssemblyResolverFactory.CheckForNuGetAssemblyLoaderAssemblies(an)
               ) )
            {
               var packageID = programConfig.PackageID;
               var packageVersion = programConfig.PackageVersion;

               var assembly = ( await assemblyLoader.LoadNuGetAssembly( packageID, packageVersion, token, programConfig.AssemblyPath ) ) ?? throw new ArgumentException( $"Could not find package \"{packageID}\" at {( packageVersion.IsNullOrEmpty() ? "latest version" : ( "version \"" + packageVersion + "\"" ) )}." );
               var suitableMethod = GetSuitableMethod( programConfig.EntrypointTypeName, programConfig.EntrypointMethodName, assembly );

               if ( suitableMethod != null )
               {
                  var args = this._args;
                  var programArgs = this._isConfigConfig ? ( programConfig?.ProcessArguments ?? Empty<String>.Array ).Concat( args ).ToArray() : args;
                  var programArgsConfig = new Lazy<IConfigurationRoot>( () => new ConfigurationBuilder().AddCommandLine( programArgs ).Build() );
                  var paramsByType = new Object[]
                  {
                     programArgs,
                     token,
                     assemblyLoader.CreateAssemblyByPathResolverCallback(),
                     assemblyLoader.CreateAssemblyNameResolverCallback(),
                     assemblyLoader.CreateNuGetPackageResolverCallback(),
                     assemblyLoader.CreateNuGetPackagesResolverCallback(),
                     assemblyLoader.CreateTypeStringResolverCallback()
                  }.ToDictionary( o => o.GetType(), o => o );
                  Object invocationResult;
                  try
                  {
                     invocationResult = suitableMethod.Invoke(
                        null,
                        suitableMethod.GetParameters()
                           .Select( p =>
                              paramsByType.TryGetValue( p.ParameterType, out var paramValue ) ?
                                 paramValue :
                                 programArgsConfig.Value.Get( p.ParameterType ) )
                           .ToArray()
                        );
                  }
                  catch ( TargetInvocationException tie )
                  {
                     throw tie.InnerException;
                  }

                  switch ( invocationResult )
                  {
                     case null:
                        retVal = 0;
                        break;
                     case Int32 i:
                        retVal = i;
                        break;
                     case ValueTask v:
                        // This handles ValueTask
                        await v;
                        retVal = 0;
                        break;
                     default:
                        // The real return type of e.g. Task<X> is System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[X,SomeDelegateType]
                        var type = invocationResult.GetType().GetTypeInfo();
                        if (
                           type
                              .AsSingleBranchEnumerable( t => t.BaseType?.GetTypeInfo(), includeFirst: true )
                              .Any( t =>
                                  t.IsGenericType
                                  && t.GenericTypeArguments.Length == 1
                                  && (
                                     Equals( t.GetGenericTypeDefinition(), typeof( Task<> ) )
                                     || Equals( t.GetGenericTypeDefinition(), typeof( ValueTask<> ) )
                                     )
                               )
                           && ( (Object) await (dynamic) invocationResult ) is Int32 returnedInt
                           )
                        {
                           // This handles Task<T> and ValueTask<T>
                           retVal = returnedInt;
                        }
                        else
                        {
                           if ( invocationResult is Task voidTask )
                           {
                              // This handles Task
                              await voidTask;
                           }
                           retVal = 0;
                        }
                        break;
                  }
               }
               else
               {
                  retVal = -3;
               }
            }
         }

         return retVal;
      }

      private static MethodInfo GetSuitableMethod(
         String entrypointTypeName,
         String entrypointMethodName,
         Assembly assembly
         )
      {
         MethodInfo suitableMethod = null;
         ConfiguredEntryPointAttribute configuredEP = null;
         if ( entrypointTypeName.IsNullOrEmpty() && entrypointMethodName.IsNullOrEmpty() )
         {
            suitableMethod = assembly.EntryPoint;
            if ( suitableMethod == null )
            {
               configuredEP = assembly.GetCustomAttribute<ConfiguredEntryPointAttribute>();
            }
            else if ( suitableMethod.IsSpecialName )
            {
               // Synthetic Main method which actually wraps the async main method (e.g. "<Main>" -> "Main")
               var actualName = suitableMethod.Name.Substring( 1, suitableMethod.Name.Length - 2 );
               var actual = suitableMethod.DeclaringType.GetTypeInfo().DeclaredMethods.FirstOrDefault( m => String.Equals( actualName, m.Name ) );
               if ( actual != null )
               {
                  suitableMethod = actual;
               }
            }
         }


         if ( suitableMethod == null && configuredEP == null )
         {
            suitableMethod = SearchSuitableMethod( assembly, entrypointTypeName, entrypointMethodName );
         }

         if ( suitableMethod != null )
         {
            configuredEP = suitableMethod.GetCustomAttribute<ConfiguredEntryPointAttribute>();
         }

         if ( configuredEP != null )
         {
            // Post process for customized config
            var suitableType = configuredEP.EntryPointType ?? suitableMethod?.DeclaringType;
            if ( suitableType != null )
            {
               var suitableName = configuredEP.EntryPointMethodName ?? suitableMethod?.Name;
               if ( String.IsNullOrEmpty( suitableName ) )
               {
                  if ( suitableMethod == null )
                  {
                     suitableMethod = SearchSuitableMethod( entrypointMethodName, suitableType.GetTypeInfo() );
                  }
               }
               else
               {
                  var newSuitableMethod = suitableType.GetTypeInfo().DeclaredMethods.FirstOrDefault( m => String.Equals( m.Name, suitableName ) && !Equals( m, suitableMethod ) );
                  if ( newSuitableMethod != null )
                  {
                     suitableMethod = newSuitableMethod;
                  }
               }
            }
         }


         return suitableMethod;
      }

      private static MethodInfo SearchSuitableMethod(
         Assembly assembly,
         String entrypointTypeName,
         String entrypointMethodName
         )
      {
         return ( entrypointTypeName.IsNullOrEmpty() ? assembly.GetTypes() : assembly.GetType( entrypointTypeName, true, false ).Singleton() )
            .Select( t => t.GetTypeInfo() )
            .Where( t => t.DeclaredMethods.Any( m => m.IsStatic ) )
            .Select( t => SearchSuitableMethod( entrypointMethodName, t ) )
            .Where( m => m != null )
            .FirstOrDefault();
      }

      private static MethodInfo SearchSuitableMethod(
         String entrypointMethodName,
         TypeInfo type
         )
      {
         IEnumerable<MethodInfo> suitableMethods;
         if ( entrypointMethodName.IsNullOrEmpty() )
         {
            var props = type.DeclaredProperties.SelectMany( GetPropertyMethods ).ToHashSet();
            suitableMethods = type.DeclaredMethods.OrderBy( m => props.Contains( m ) ); // This will order in such way that false (not related to property) comes first
         }
         else
         {
            suitableMethods = type.GetDeclaredMethod( entrypointMethodName ).Singleton();
         }

         return suitableMethods
            .Where( m => m.IsStatic && m.IsPublic && HasSuitableSignature( m ) )
            .FirstOrDefault();
      }

      private static IEnumerable<MethodInfo> GetPropertyMethods(
         PropertyInfo property
         )
      {
         var method = property.GetMethod;
         if ( method != null )
         {
            yield return method;
         }

         method = property.SetMethod;
         if ( method != null )
         {
            yield return method;
         }
      }

      private static Boolean HasSuitableSignature(
         MethodInfo method
         )
      {
         // TODO when entrypointMethodName is specified, we allow always true, and then dynamically parse from ConfigurationBuilder
         var parameters = method.GetParameters();
         return parameters.Length == 1
            && parameters[0].ParameterType.IsSZArray
            && Equals( parameters[0].ParameterType.GetElementType(), typeof( String ) );
      }

   }
}
