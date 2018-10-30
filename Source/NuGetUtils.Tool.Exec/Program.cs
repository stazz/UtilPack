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
using NuGetUtils.Lib.AssemblyResolving;
using NuGetUtils.Lib.EntryPoint;
using NuGetUtils.Lib.Restore;
using NuGetUtils.Lib.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.Documentation;


namespace NuGetUtils.Tool.Exec
{

   static class Program
   {
      public static Task<Int32> Main( String[] args )
         => new NuGetExecutingProgram().MainAsync( args, NuGetExecutingProgram.EXEC_ARGS_SEPARATOR );

   }

   internal sealed class NuGetExecutingProgram : NuGetRestoringProgram<NuGetExecutionConfiguration, ConfigurationConfigurationImpl>
   {
      internal const String EXEC_ARGS_SEPARATOR = "--";

      public NuGetExecutingProgram()
         : base( new DefaultCommandLineDocumentationInfo()
         {
            ExecutableName = "nuget-exec",
            CommandLineGroupInfo = new DefaultDocumentationGroupInfo()
            {
               GroupName = "executable-options",
               AdditionalGroups = new[] {new GroupContainer(true, new ParameterGroupOrFixedParameter[] {
                     new FixedParameter(false, EXEC_ARGS_SEPARATOR),
                     new NamedParameterGroup(true, "executable-arguments", description: "The arguments for the entrypoint within NuGet-packaged assembly.")
                  } )},
               Purpose = "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by command-line parameters."
            },
            ConfigurationFileGroupInfo = new DefaultDocumentationGroupInfo()
            {
               AdditionalGroups = new[] { new NamedParameterGroup( true, "additional-executable-arguments", description: "The additional arguments for the entrypoint within NuGet-packaged assembly." ) },
               Purpose = "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by configuration file."
            }
         } )
      {

      }

      protected override Boolean ValidateConfiguration(
         ConfigurationInformation info
         )
      {
         return !String.IsNullOrEmpty( info.Configuration.PackageID );
      }

      protected override async Task<Int32> UseRestorerAsync(
         ConfigurationInformation info,
         CancellationToken token,
         BoundRestoreCommandUser restorer,
         String sdkPackageID,
         String sdkPackageVersion
         )
      {
         var docu = this.GetDocumentation();

         var programConfig = info.Configuration;
         var loadFromParentForCA = NuGetAssemblyResolverFactory.ReturnFromParentAssemblyLoaderForAssemblies( new[] { typeof( ConfiguredEntryPointAttribute ) } );
         Int32 retVal;

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

            var assembly = ( await assemblyLoader.LoadNuGetAssembly( packageID, packageVersion, token, programConfig.AssemblyPath ) ) ?? throw new ArgumentException( $"Could not find package \"{packageID}\" at {( String.IsNullOrEmpty( packageVersion ) ? "latest version" : ( "version \"" + packageVersion + "\"" ) )}." );
            var suitableMethod = new MethodSearcher( assembly, programConfig.EntrypointTypeName, programConfig.EntrypointMethodName ).GetSuitableMethod();

            if ( suitableMethod != null )
            {
               var args = info.RemainingArguments;
               var programArgs = new Lazy<String[]>( () => info.IsConfigurationConfiguration ? ( programConfig?.ProcessArguments ?? Empty<String>.Array ).Concat( args ).ToArray() : args.ToArray() );
               var programArgsConfig = new Lazy<IConfigurationRoot>( () => new ConfigurationBuilder().AddCommandLine( programArgs.Value ).Build() );
               var paramsByType = new (Object, Type, Func<Object>)[]
               {
                     (programArgs, typeof(String[]), () => programArgs.Value),
                     (token, null, null),
                     (assemblyLoader.CreateAssemblyByPathResolverCallback(), null, null),
                     (assemblyLoader.CreateAssemblyNameResolverCallback(), null, null),
                     (assemblyLoader.CreateNuGetPackageResolverCallback(), null, null),
                     (assemblyLoader.CreateNuGetPackagesResolverCallback(), null, null),
                     (assemblyLoader.CreateTypeStringResolverCallback(), null, null)
               }.ToDictionary( o => o.Item2 ?? o.Item1.GetType(), o => o.Item3 ?? ( () => o.Item1 ) );
               Object invocationResult;
               try
               {
                  invocationResult = suitableMethod.Invoke(
                     null,
                     suitableMethod.GetParameters()
                        .Select( p =>
                           paramsByType.TryGetValue( p.ParameterType, out var paramValue ) ?
                              paramValue() :
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

                     var type = invocationResult.GetType().GetTypeInfo();
                     if (
                        ( ( type.IsGenericType && type.GenericTypeArguments.Length == 1 && Equals( type.GetGenericTypeDefinition(), typeof( ValueTask<> ) ) ) // Check for ValueTask<X>
                        ||
                        // The real return type of e.g. Task<X> is System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1+AsyncStateMachineBox`1[X,SomeDelegateType], so we need to explore base types of return value.
                        type
                           .AsSingleBranchEnumerable( t => t.BaseType?.GetTypeInfo(), includeFirst: true )
                           .Any( t =>
                               t.IsGenericType
                               && t.GenericTypeArguments.Length == 1
                               && Equals( t.GetGenericTypeDefinition(), typeof( Task<> ) )
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

         return retVal;
      }

      //protected override String GetDocumentation()
      //{
      //   var generator = new CommandLineArgumentsDocumentationGenerator();
      //   return
      //      $"nuget-exec version {typeof( Program ).Assembly.GetName().Version} (NuGet version {typeof( NuGet.Common.ILogger ).Assembly.GetName().Version})\n" +
      //      generator.GenerateParametersDocumentation( new ParameterGroupOrFixedParameter[]
      //         {
      //            new NamedParameterGroup(false, "executable-options"),
      //            new GroupContainer(true, new ParameterGroupOrFixedParameter[] {
      //               new FixedParameter(false, EXEC_ARGS_SEPARATOR),
      //               new NamedParameterGroup(true, "executable-arguments", description: "The arguments for the entrypoint within NuGet-packaged assembly.")
      //            } )
      //         },
      //         typeof( NuGetExecutionConfiguration ),
      //         "nuget-exec",
      //         "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by command-line parameters.",
      //         "executable-options"
      //         )
      //         + "\n\n\n" +
      //      generator.GenerateParametersDocumentation( new ParameterGroupOrFixedParameter[]
      //         {
      //            new NamedParameterGroup(false, "configuration-options"),
      //            new NamedParameterGroup(true, "additional-executable-arguments", description: "The additional arguments for the entrypoint within NuGet-packaged assembly.")
      //         },
      //         typeof( ConfigurationConfigurationImpl ),
      //         "nuget-exec",
      //         "Execute a method from NuGet-packaged assembly, restoring the package if needed, parametrized by configuration file.",
      //         "configuration-options"
      //      );
      //}
   }

   internal sealed class MethodSearcher
   {
      private readonly Assembly _assembly;
      private readonly String _entryPointTypeName;
      private readonly String _entryPointMethodName;

      public MethodSearcher(
         Assembly assembly,
         String entryPointTypeName,
         String entryPointMethodName
         )
      {
         this._assembly = ArgumentValidator.ValidateNotNull( nameof( assembly ), assembly );
         this._entryPointTypeName = entryPointTypeName;
         this._entryPointMethodName = entryPointMethodName;
      }

      public MethodInfo GetSuitableMethod()
      {
         MethodInfo suitableMethod = null;
         ConfiguredEntryPointAttribute configuredEP = null;
         var assembly = this._assembly;
         if ( this._entryPointTypeName.IsNullOrEmpty() && this._entryPointMethodName.IsNullOrEmpty() )
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
            suitableMethod = this.SearchSuitableMethod();
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
                     suitableMethod = this.SearchSuitableMethod( suitableType.GetTypeInfo() );
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

      private MethodInfo SearchSuitableMethod()
      {
         var entryPointTypeName = this._entryPointTypeName;
         return ( entryPointTypeName.IsNullOrEmpty() ? this._assembly.GetTypes() : this._assembly.GetType( entryPointTypeName, true, false ).Singleton() )
            .Select( t => t.GetTypeInfo() )
            .Where( t => t.DeclaredMethods.Any( m => m.IsStatic ) )
            .Select( t => this.SearchSuitableMethod( t ) )
            .Where( m => m != null )
            .FirstOrDefault();
      }

      private MethodInfo SearchSuitableMethod(
         TypeInfo type
         )
      {
         IEnumerable<MethodInfo> suitableMethods;
         var entryPointMethodName = this._entryPointMethodName;
         if ( entryPointMethodName.IsNullOrEmpty() )
         {
            var props = type.DeclaredProperties.SelectMany( this.GetPropertyMethods ).ToHashSet();
            suitableMethods = type.DeclaredMethods.OrderBy( m => props.Contains( m ) ); // This will order in such way that false (not related to property) comes first
         }
         else
         {
            suitableMethods = type.GetDeclaredMethod( entryPointMethodName ).Singleton();
         }

         return suitableMethods
            .Where( m => m.IsStatic )
            .FirstOrDefault();
      }

      private IEnumerable<MethodInfo> GetPropertyMethods(
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

      //private Boolean HasSuitableSignature(
      //   MethodInfo method
      //   )
      //{
      //   // TODO when entrypointMethodName is specified, we allow always true, and then dynamically parse from ConfigurationBuilder
      //   var parameters = method.GetParameters();
      //   return parameters.Length == 1
      //      && parameters[0].ParameterType.IsSZArray
      //      && Equals( parameters[0].ParameterType.GetElementType(), typeof( String ) );
      //}

   }

}
