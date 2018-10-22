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
using Microsoft.Extensions.Configuration;
using NuGet.Frameworks;
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

namespace NuGet.Utilities.Execute
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
            } )
            ) )
         {

            var thisFramework = restorer.ThisFramework;
            var sdkPackageID = thisFramework.GetSDKPackageID( programConfig.ProcessSDKFrameworkPackageID ); // Typically "Microsoft.NETCore.App"
            var sdkPackageVersion = thisFramework.GetSDKPackageVersion( sdkPackageID, programConfig.ProcessSDKFrameworkPackageVersion );

            using ( var assemblyLoader = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
               restorer,
               await restorer.RestoreIfNeeded(
                  sdkPackageID,
                  sdkPackageVersion,
                  token
                  ),
               out var loadContext,
               additionalCheckForDefaultLoader: NuGetAssemblyResolverFactory.CheckForNuGetAssemblyLoaderAssemblies
               ) )
            {
               var entrypointTypeName = programConfig.EntrypointTypeName;
               var entrypointMethodName = programConfig.EntrypointMethodName;
               var packageID = programConfig.PackageID;
               var packageVersion = programConfig.PackageVersion;

               var assembly = ( await assemblyLoader.LoadNuGetAssembly( packageID, packageVersion, token, programConfig.AssemblyPath ) ) ?? throw new ArgumentException( $"Could not find package \"{packageID}\" at {( packageVersion.IsNullOrEmpty() ? "latest version" : ( "version \"" + packageVersion + "\"" ) )}." );
               MethodInfo suitableMethod = null;
               if ( entrypointTypeName.IsNullOrEmpty() && entrypointMethodName.IsNullOrEmpty() )
               {
                  suitableMethod = assembly.EntryPoint;
               }
               if ( suitableMethod == null )
               {
                  suitableMethod = assembly
                     .GetSuitableTypes( programConfig.EntrypointTypeName )
                     .Select( t => GetSuitableMethod( t, entrypointMethodName ) )
                     .Where( m => m != null )
                     .FirstOrDefault();
               }

               if ( suitableMethod != null )
               {
                  Object invocationResult;
                  try
                  {
                     invocationResult = suitableMethod.Invoke(
                     null,
                     new Object[] {
                        isConfigConfig ?
                           ( programConfig?.ProcessArguments ?? Empty<String>.Array ).Concat( args ).ToArray() :
                           args
                        }
                     );
                  }
                  catch ( TargetInvocationException tie )
                  {
                     throw tie.InnerException;
                  }

                  switch ( invocationResult )
                  {
                     case null:
                        break;
                     //case Int32 i:
                     //   retVal = i;
                     //   break;
                     case Task t:
                        // This handles both Task and Task<T>
                        await t;
                        break;
                     case ValueTask v:
                        // This handles ValueTask
                        await v;
                        break;
                     default:
                        var type = invocationResult.GetType().GetTypeInfo();
                        if (
                           type.IsGenericType
                           && type.GenericTypeParameters.Length == 1
                           && Equals( type.GetGenericTypeDefinition(), typeof( ValueTask<> ) )
                           )
                        {
                           // This handles ValueTask<T>
                           await (dynamic) invocationResult;
                        }
                        break;
                  }
                  retVal = 0;
               }
               else
               {
                  retVal = -3;
               }
            }
         }

         return retVal;
      }

      internal static IEnumerable<TypeInfo> GetSuitableTypes(
         this Assembly assembly,
         String entrypointTypeName
         )
      {
         IEnumerable<Type> suitableTypes;

         if ( entrypointTypeName.IsNullOrEmpty() )
         {
            suitableTypes = assembly.GetTypes();
         }
         else
         {
            suitableTypes = assembly.GetType( entrypointTypeName, true, false ).Singleton();
         }

         return suitableTypes
            .Select( t => t.GetTypeInfo() )
            .Where( t => t.DeclaredMethods.Any( m => m.IsStatic ) );
      }

      private static MethodInfo GetSuitableMethod(
         TypeInfo type,
         String entrypointMethodName
         )
      {
         IEnumerable<MethodInfo> suitableMethods;

         if ( entrypointMethodName.IsNullOrEmpty() )
         {
            suitableMethods = type.GetDeclaredMethod( entrypointMethodName ).Singleton();
         }
         else
         {
            var props = type.DeclaredProperties.SelectMany( GetPropertyMethods ).ToHashSet();
            suitableMethods = type.DeclaredMethods.OrderBy( m => props.Contains( m ) ); // This will order in such way that false (not related to property) comes first
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
      public String ProcessSDKFrameworkPackageID { get; set; }

      /// <summary>
      /// Gets the package version of the SDK of the framework of the NuGet package.
      /// </summary>
      /// <value>The package version of the SDK of the framework of the NuGet package.</value>
      /// <remarks>
      /// If this property is <c>null</c> or empty string, then <see cref="NuGetDeployment"/> will try to use automatic detection of SDK package version.
      /// </remarks>
      public String ProcessSDKFrameworkPackageVersion { get; set; }
   }

   internal class ConfigurationConfiguration
   {
      public String ConfigurationFileLocation { get; set; }
   }
}
