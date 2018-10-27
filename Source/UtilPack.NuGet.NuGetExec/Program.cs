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
using NuGet.Utils.Exec.Entrypoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.NuGet;
using UtilPack.NuGet.AssemblyLoading;

using TAssemblyByPathResolverCallback = System.Func<System.String, System.Reflection.Assembly>;
using TAssemblyNameResolverCallback = System.Func<System.Reflection.AssemblyName, System.Reflection.Assembly>;
using TNuGetPackageResolverCallback = System.Func<System.String, System.String, System.String, System.Threading.CancellationToken, System.Threading.Tasks.Task<System.Reflection.Assembly>>;
using TNuGetPackagesResolverCallback = System.Func<System.String[], System.String[], System.String[], System.Threading.CancellationToken, System.Threading.Tasks.Task<System.Reflection.Assembly[]>>;


namespace NuGet.Utils.Exec
{
   static class Program
   {
      private const String DEFAULT_TOOL_PATH = "dotnet";

      // /PackageID=X [/PackageVersion /AssemblyPath /EntrypointTypeName /EntrypointMethodName /RestoreFramework /ProcessSDKFrameworkPackageID /ProcessSDKFrameworkPackageVersion] [-- arg1 arg2 arg3]
      // OR
      // /ConfigurationFileLocation=X arg1 arg2 arg3
      async static Task<Int32> Main( String[] args )
      {
         var retVal = -1;

         NuGetExecutionConfiguration programConfig = null;
         var isConfigConfig = false;
         try
         {
            Int32 programArgStart;
            IConfigurationBuilder configRoot;
            isConfigConfig = args.GetOrDefault( 0 )?.StartsWith( $"/{nameof( ConfigurationConfiguration.ConfigurationFileLocation )}=" ) ?? false;
            if ( isConfigConfig )
            {
               programArgStart = 1;
               configRoot = new ConfigurationBuilder()
                  .AddJsonFile( Path.GetFullPath(
                     new ConfigurationBuilder()
                        .AddCommandLine( args.Take( 1 ).ToArray() )
                        .Build()
                        .Get<ConfigurationConfiguration>()
                        .ConfigurationFileLocation ) );
            }
            else
            {
               var idx = Array.FindIndex( args, arg => String.Equals( arg, "--" ) );
               programArgStart = idx < 0 ? args.Length : idx;
               configRoot = new ConfigurationBuilder()
                  .AddCommandLine( args.Take( programArgStart ).ToArray() );
               if ( idx >= 0 )
               {
                  ++programArgStart;
               }
            }

            if ( programArgStart > 0 )
            {
               args = args.Skip( programArgStart ).ToArray();

               programConfig = configRoot
                  .Build()
                  .Get<NuGetExecutionConfiguration>();
            }
            else
            {
               Console.Out.WriteLine( "TODO help here." );
            }

         }
         catch ( Exception exc )
         {
            Console.Error.WriteLine( $"Error with reading configuration, please check your command line parameters! ({exc.Message})" );
            Console.Error.WriteLine( exc );
         }

         if ( !programConfig?.PackageID?.IsNullOrEmpty() ?? false )
         {
            using ( var source = new CancellationTokenSource() )
            {
               void OnCancelKey( Object sender, ConsoleCancelEventArgs e )
               {
                  e.Cancel = true;
                  source.Cancel();
               }
               Console.CancelKeyPress += OnCancelKey;
               using ( new UsingHelper( () => Console.CancelKeyPress -= OnCancelKey ) )
               {
                  try
                  {
                     await PerformProgram( args, programConfig, isConfigConfig, source.Token );
                  }
                  catch ( Exception exc )
                  {
                     Console.Error.WriteLine( $"An error occurred: {exc.Message}." );
                     retVal = -2;
                  }
               }
            }
         }
         else
         {
            Console.Out.WriteLine( "TODO help here." );
         }


         return retVal;
      }

      private static async Task<Int32> PerformProgram(
         String[] args,
         NuGetExecutionConfiguration programConfig,
         Boolean isConfigConfig,
         CancellationToken token
         )
      {
         Int32 retVal;
         var targetFWString = programConfig.RestoreFramework;
         using ( var restorer = new BoundRestoreCommandUser(
            UtilPackNuGetUtility.GetNuGetSettingsWithDefaultRootDirectory(
                  Path.GetDirectoryName( new Uri( typeof( Program ).GetTypeInfo().Assembly.CodeBase ).LocalPath ),
                  programConfig.NuGetConfigurationFile
               ),
            thisFramework: String.IsNullOrEmpty( targetFWString ) ? null : NuGetFramework.Parse( targetFWString ),
            nugetLogger: new TextWriterLogger( new TextWriterLoggerOptions()
            {
               DebugWriter = null
            } ),
            lockFileCacheEnvironmentVariableName: "NUGET_EXEC_CACHE_DIR",
            getDefaultLockFileCacheDir: homeDir => Path.Combine( homeDir, ".nuget-exec-cache" ),
            disableLockFileCacheDir: programConfig.DisableLockFileCache
            ) )
         {

            var thisFramework = restorer.ThisFramework;
            var sdkPackageID = thisFramework.GetSDKPackageID( programConfig.SDKFrameworkPackageID ); // Typically "Microsoft.NETCore.App"
            var sdkPackageVersion = thisFramework.GetSDKPackageVersion( sdkPackageID, programConfig.SDKFrameworkPackageVersion );
            var loadFromParentForCA = NuGetAssemblyResolverFactory.ReturnFromParentAssemblyLoaderForAssemblies( typeof( ConfiguredEntryPointAttribute ) );

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
                  var programArgs = isConfigConfig ? ( programConfig?.ProcessArguments ?? Empty<String>.Array ).Concat( args ).ToArray() : args;
                  var programArgsConfig = new Lazy<IConfigurationRoot>( () => new ConfigurationBuilder().AddCommandLine( programArgs ).Build() );
                  var ctx = new EntryPointParameterProvidingContext(
                     programArgs,
                     token,
                     assemblyPath => assemblyLoader.LoadOtherAssembly( assemblyPath ),
                     assemblyName => assemblyLoader.TryResolveFromPreviouslyLoaded( assemblyName ),
                     ( packageIDParam, packageVersionParam, assemblyPath, tokenParam ) => assemblyLoader.LoadNuGetAssembly( packageIDParam, packageVersionParam, tokenParam, assemblyPath ),
                     ( packageIDs, packageVersions, assemblyPaths, tokenParam ) => assemblyLoader.LoadNuGetAssemblies( packageIDs, packageVersions, assemblyPaths, tokenParam )
                     );
                  Object invocationResult;
                  try
                  {
                     invocationResult = suitableMethod.Invoke(
                        null,
                        suitableMethod.GetParameters().Select( p => ctx.ProvideEntryPointParameter( p.ParameterType, programArgsConfig ) ).ToArray()
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

      private struct EntryPointParameterProvidingContext
      {
         private static readonly Dictionary<Type, Func<EntryPointParameterProvidingContext, Object>> EntryPointParameterChoosers = new Dictionary<Type, Func<EntryPointParameterProvidingContext, Object>>()
         {
            { typeof(String[]), ctx => ctx.ProgramArgs },
            { typeof(CancellationToken), ctx => ctx.CancellationToken },
            { typeof(TAssemblyByPathResolverCallback), ctx => ctx.AssemblyByPathResolverCallback },
            { typeof(TAssemblyNameResolverCallback), ctx => ctx.AssemblyNameResolverCallback },
            { typeof(TNuGetPackageResolverCallback), ctx => ctx.NuGetPackageResolverCallback },
            { typeof(TNuGetPackagesResolverCallback), ctx => ctx.NuGetPackagesResolverCallback }
         };

         public EntryPointParameterProvidingContext(
            String[] programArgs,
            CancellationToken token,
            TAssemblyByPathResolverCallback assemblyByPathResolverCallback,
            TAssemblyNameResolverCallback assemblyNameResolverCallback,
            TNuGetPackageResolverCallback nuGetPackageResolverCallback,
            TNuGetPackagesResolverCallback nuGetPackagesResolverCallback
            )
         {
            this.ProgramArgs = programArgs;
            this.CancellationToken = token;
            this.AssemblyByPathResolverCallback = assemblyByPathResolverCallback;
            this.AssemblyNameResolverCallback = assemblyNameResolverCallback;
            this.NuGetPackageResolverCallback = nuGetPackageResolverCallback;
            this.NuGetPackagesResolverCallback = nuGetPackagesResolverCallback;
         }

         public String[] ProgramArgs { get; }
         public CancellationToken CancellationToken { get; }
         public TAssemblyByPathResolverCallback AssemblyByPathResolverCallback { get; }
         public TAssemblyNameResolverCallback AssemblyNameResolverCallback { get; }
         public TNuGetPackageResolverCallback NuGetPackageResolverCallback { get; }
         public TNuGetPackagesResolverCallback NuGetPackagesResolverCallback { get; }

         public Object ProvideEntryPointParameter(
            Type parameterType,
            Lazy<IConfigurationRoot> programArgsConfig
            )
         {
            return EntryPointParameterChoosers.TryGetValue( parameterType, out var ctxCreator ) ?
               ctxCreator( this ) :
               programArgsConfig.Value.Get( parameterType );
         }
      }
   }

   internal class NuGetExecutionConfiguration
   {
      public String NuGetConfigurationFile { get; set; }

      public String[] ProcessArguments { get; set; }


      /// <summary>
      /// Gets the package ID of the package to be deployed.
      /// </summary>
      /// <value>The package ID of the package to be deployed.</value>
      public String PackageID { get; set; }

      /// <summary>
      /// Gets the package version of the package to be deployed.
      /// </summary>
      /// <value>The package version of the package to be deployed.</value>
      /// <remarks>
      /// If this property is <c>null</c> or empty string, then NuGet source will be queried for the newest version.
      /// </remarks>
      public String PackageVersion { get; set; }

      /// <summary>
      /// Gets the path within the package where the entrypoint assembly resides.
      /// </summary>
      /// <value>The path within the package where the entrypoint assembly resides.</value>
      /// <remarks>
      /// This property will not be used for NuGet packages with only one assembly.
      /// </remarks>
      public String AssemblyPath { get; set; }

      public String EntrypointTypeName { get; set; }

      public String EntrypointMethodName { get; set; }

      /// <summary>
      /// Gets the framework to use when performing the restore for the target package. By default, the framework is auto-detected to be whichever matches the assembly that was resolved.
      /// </summary>
      /// <value>The framework to use when performing the restore for the target package.</value>
      public String RestoreFramework { get; set; }

      /// <summary>
      /// Gets the package ID of the SDK of the framework of the NuGet package.
      /// </summary>
      /// <value>The package ID of the SDK of the framework of the NuGet package.</value>
      /// <remarks>
      /// If this property is <c>null</c> or empty string, then <see cref="NuGetDeployment"/> will try to use automatic detection of SDK package ID.
      /// </remarks>
      public String SDKFrameworkPackageID { get; set; }

      /// <summary>
      /// Gets the package version of the SDK of the framework of the NuGet package.
      /// </summary>
      /// <value>The package version of the SDK of the framework of the NuGet package.</value>
      /// <remarks>
      /// If this property is <c>null</c> or empty string, then <see cref="NuGetDeployment"/> will try to use automatic detection of SDK package version.
      /// </remarks>
      public String SDKFrameworkPackageVersion { get; set; }

      public Boolean DisableLockFileCache { get; set; }
   }

   internal class ConfigurationConfiguration
   {
      public String ConfigurationFileLocation { get; set; }
   }
}
