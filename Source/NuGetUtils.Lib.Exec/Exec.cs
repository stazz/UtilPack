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
using NuGet.ProjectModel;
using NuGetUtils.Lib.AssemblyResolving;
using NuGetUtils.Lib.EntryPoint;
using NuGetUtils.Lib.Exec;
using NuGetUtils.Lib.Restore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace NuGetUtils.Lib.Exec
{
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
            configuredEP = assembly.GetCustomAttribute<ConfiguredEntryPointAttribute>();
            if ( configuredEP == null )
            {
               suitableMethod = assembly.EntryPoint;
               if ( suitableMethod != null && suitableMethod.IsSpecialName )
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
            var props =
#if NET46 || NETSTANDARD1_6
               new HashSet<MethodInfo>(
#endif
               type.DeclaredProperties.SelectMany( this.GetPropertyMethods )
#if NET46 || NETSTANDARD1_6
               )
#else
               .ToHashSet()
#endif
               ;
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

/// <summary>
/// This class contains extension methods for types defined in this assembly.
/// </summary>
public static class E_NuGetUtils
{
#if NET46
   /// <summary>
   /// Using information from this <see cref="NuGetExecutionConfiguration"/>, restores the NuGet package, finds an assembly, and executes a method within the assembly.
   /// If the method returns <see cref="ValueTask{TResult}"/> (also non-generic variant for .NET Core 2.1), <see cref="Task"/> or <see cref="Task{TResult}"/>, it is <c>await</c>ed upon.
   /// </summary>
   /// <param name="configuration">This <see cref="NuGetExecutionConfiguration"/></param>
   /// <param name="token">The <see cref="CancellationToken"/> to use when performing <c>async</c> operations.</param>
   /// <param name="restorer">The <see cref="BoundRestoreCommandUser"/> to use for restoring.</param>
   /// <param name="additionalParameterTypeProvider">The callback to provide values for method parameters with custom types.</param>
   /// <returns>The return value of the method, if the method returns integer synchronously or asynchronously.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NuGetExecutionConfiguration"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="restorer"/> is <c>null</c>.</exception>
   /// <remarks>The <paramref name="additionalParameterTypeProvider"/> is only used when the method parameter type is not <see cref="CancellationToken"/>, or <see cref="Func{T, TResult}"/> delegate types which represent signatures of <see cref="NuGetAssemblyResolver"/> methods.</remarks>
#else
   /// <summary>
   /// Using information from this <see cref="NuGetExecutionConfiguration"/>, restores the NuGet package, finds an assembly, and executes a method within the assembly.
   /// If the method returns <see cref="ValueTask{TResult}"/> (also non-generic variant for .NET Core 2.1), <see cref="Task"/> or <see cref="Task{TResult}"/>, it is <c>await</c>ed upon.
   /// </summary>
   /// <param name="configuration">This <see cref="NuGetExecutionConfiguration"/></param>
   /// <param name="token">The <see cref="CancellationToken"/> to use when performing <c>async</c> operations.</param>
   /// <param name="restorer">The <see cref="BoundRestoreCommandUser"/> to use for restoring.</param>
   /// <param name="additionalParameterTypeProvider">The callback to provide values for method parameters with custom types.</param>
   /// <param name="sdkPackageID">The SDK package ID.</param>
   /// <param name="sdkPackageVersion">The SDK package version.</param>
   /// <returns>The return value of the method, if the method returns integer synchronously or asynchronously.</returns>
   /// <exception cref="NullReferenceException">If this <see cref="NuGetExecutionConfiguration"/> is <c>null</c>.</exception>
   /// <exception cref="ArgumentNullException">If <paramref name="restorer"/> is <c>null</c>.</exception>
   /// <remarks>The <paramref name="additionalParameterTypeProvider"/> is only used when the method parameter type is not <see cref="CancellationToken"/>, or <see cref="Func{T, TResult}"/> delegate types which represent signatures of <see cref="NuGetAssemblyResolver"/> methods.</remarks>
#endif
   public static async Task<Int32> ExecuteMethodWithinNuGetAssemblyAsync(
      this NuGetExecutionConfiguration configuration,
      CancellationToken token,
      BoundRestoreCommandUser restorer,
      Func<Type, Object> additionalParameterTypeProvider
#if !NET46
      , EitherOr<IEnumerable<String>, LockFile> thisFrameworkRestoreResult = default
#endif
      )
   {
      ArgumentValidator.ValidateNotNullReference( configuration );
      ArgumentValidator.ValidateNotNull( nameof( restorer ), restorer );
      Int32 retVal;

      using ( var assemblyLoader = NuGetAssemblyResolverFactory.NewNuGetAssemblyResolver(
         restorer,
#if NET46
         new AppDomainSetup()
         {

         },
         out var appDomain
#else
         out var loadContext,
         thisFrameworkRestoreResult: thisFrameworkRestoreResult,
         additionalCheckForDefaultLoader: NuGetAssemblyResolverFactory.ReturnFromParentAssemblyLoaderForAssemblies( new[] { typeof( ConfiguredEntryPointAttribute ) } )
#endif
         ) )
#if NET46
      using ( new UsingHelper( () => { try { AppDomain.Unload( appDomain ); } catch { } } ) )
#endif
      {
         var packageID = configuration.PackageID;
         var packageVersion = configuration.PackageVersion;

         var assembly = ( await assemblyLoader.LoadNuGetAssembly( packageID, packageVersion, token, configuration.AssemblyPath ) ) ?? throw new ArgumentException( $"Could not find package \"{packageID}\" at {( String.IsNullOrEmpty( packageVersion ) ? "latest version" : ( "version \"" + packageVersion + "\"" ) )}." );
         var suitableMethod = new MethodSearcher( assembly, configuration.EntrypointTypeName, configuration.EntrypointMethodName ).GetSuitableMethod();

         if ( suitableMethod != null )
         {
            var paramsByType = new Object[]
            {
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
                           additionalParameterTypeProvider( p.ParameterType ) )
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
#if NETCOREAPP2_1
               case ValueTask v:

                  // This handles ValueTask
                  await v;
                  retVal = 0;
                  break;
#endif
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
}

