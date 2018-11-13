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
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using ResourcePooling.Async.Abstractions;

using TTypeInfo = System.
#if NET40
   Type
#else
   Reflection.TypeInfo
#endif
   ;

namespace ResourcePooling.Async.Abstractions
{

   public interface ResourceFactoryDynamicCreationConfiguration
   {
      /// <summary>
      /// Gets or sets the value for NuGet package ID of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.
      /// </summary>
      /// <value>The value for NuGet package ID of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.</value>
      /// <remarks>
      /// This property is used by <see cref="AcquireResourcePoolProvider"/> when loading an instance of <see cref="AsyncResourceFactoryProvider"/>.
      /// </remarks>
      /// <seealso cref="PoolProviderVersion"/>
      String PoolProviderPackageID { get; }

      /// <summary>
      /// Gets or sets the value for NuGet package version of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.
      /// </summary>
      /// <value>The value for NuGet package version of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.</value>
      /// <remarks>
      /// This property is used by <see cref="AcquireResourcePoolProvider"/> when loading an instance of <see cref="AsyncResourceFactoryProvider"/>.
      /// The value, if specified, should be parseable into <see cref="T:NuGet.Versioning.VersionRange"/>.
      /// If left out, then the newest version will be used, but this will cause additional overhead when querying for the newest version.
      /// </remarks>
      String PoolProviderVersion { get; }

      /// <summary>
      /// Gets or sets the path within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties where the assembly holding type implementing <see cref="AsyncResourceFactoryProvider"/> resides.
      /// </summary>
      /// <value>The path within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties where the assembly holding type implementing <see cref="AsyncResourceFactoryProvider"/> resides.</value>
      /// <remarks>
      /// This property will be used only for NuGet packages with more than assembly within its framework-specific folder.
      /// </remarks>
      String PoolProviderAssemblyPath { get; }

      /// <summary>
      /// Gets or sets the name of the type implementing <see cref="AsyncResourceFactoryProvider"/>, located in assembly within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties.
      /// </summary>
      /// <value>The name of the type implementing <see cref="AsyncResourceFactoryProvider"/>, located in assembly within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties.</value>
      /// <remarks>
      /// This value can be left out so that <see cref="AcquireResourcePoolProvider"/> will search for all types within package implementing <see cref="AsyncResourceFactoryProvider"/> and use the first suitable one.
      /// </remarks>
      String PoolProviderTypeName { get; }


   }

   public class DefaultResourceFactoryDynamicCreationConfiguration : ResourceFactoryDynamicCreationConfiguration
   {
      public String PoolProviderPackageID { get; set; }

      public String PoolProviderVersion { get; set; }

      public String PoolProviderAssemblyPath { get; set; }

      public String PoolProviderTypeName { get; set; }
   }
}

public static partial class E_ResourcePooling
{
   public static async Task<AsyncResourceFactory<TResource>> CreateAsyncResourceFactory<TResource>(
      this ResourceFactoryDynamicCreationConfiguration configuration,
      Func<String, String, String, CancellationToken, Task<Assembly>> assemblyLoader,
      Func<AsyncResourceFactoryProvider, Object> creationParametersProvider,
      CancellationToken token
      )
   {
      (var factoryProvider, var errorMessage) = await AcquireResourcePoolProvider( configuration, assemblyLoader, token );

      return ( factoryProvider ?? throw new InvalidOperationException( errorMessage ?? "Unspecified error." ) )
         .BindCreationParameters<TResource>( creationParametersProvider( factoryProvider ) );
   }

   private static async Task<(AsyncResourceFactoryProvider FactoryProvider, String ErrorMessage)> AcquireResourcePoolProvider(
      ResourceFactoryDynamicCreationConfiguration configuration,
      Func<String, String, String, CancellationToken, Task<Assembly>> assemblyLoader,
      CancellationToken token
      )
   {
      AsyncResourceFactoryProvider retVal = null;
      String errorMessage = null;
      if ( assemblyLoader != null )
      {
         var packageID = configuration.PoolProviderPackageID;
         if ( !String.IsNullOrEmpty( packageID ) )
         {
            try
            {
               var assembly = await assemblyLoader(
                  packageID, // package ID
                  configuration.PoolProviderVersion,  // optional package version
                  configuration.PoolProviderAssemblyPath, // optional assembly path within package
                  token
                  );
               if ( assembly != null )
               {
                  // Now search for the type
                  var typeName = configuration.PoolProviderTypeName;
                  var parentType = typeof( AsyncResourceFactoryProvider ).GetTypeInfo();
                  var checkParentType = !String.IsNullOrEmpty( typeName );
                  Type providerType;
                  if ( checkParentType )
                  {
                     // Instantiate directly
                     providerType = assembly.GetType( typeName ); //, false, false );
                  }
                  else
                  {
                     // Search for first available
                     providerType = assembly.
#if NET40
                           GetTypes()
#else
                           DefinedTypes
#endif
                           .FirstOrDefault( t => !t.IsInterface && !t.IsAbstract && t.IsPublic && parentType.IsAssignableFromIgnoreAssemblyVersion( t ) )
#if !NET40
                           ?.AsType()
#endif
                           ;
                  }

                  if ( providerType != null )
                  {
                     if ( !checkParentType || parentType.IsAssignableFromIgnoreAssemblyVersion( providerType.GetTypeInfo() ) )
                     {
                        // All checks passed, instantiate the pool provider
                        retVal = (AsyncResourceFactoryProvider) Activator.CreateInstance( providerType );
                     }
                     else
                     {
                        errorMessage = $"The type \"{providerType.FullName}\" in \"{assembly}\" does not have required parent type \"{parentType.FullName}\".";
                     }
                  }
                  else
                  {
                     errorMessage = $"Failed to find type within assembly in \"{assembly}\", try specify \"{nameof( configuration.PoolProviderTypeName )}\" configuration parameter.";
                  }
               }
               else
               {
                  errorMessage = $"Failed to load resource pool provider package \"{packageID}\".";
               }
            }
            catch ( Exception exc )
            {
               errorMessage = $"An exception occurred when acquiring resource pool provider: {exc.Message}";
            }
         }
         else
         {
            errorMessage = $"No NuGet package ID were provided as \"{nameof( configuration.PoolProviderPackageID )}\" configuration parameter. The package ID should be of the package holding implementation for \"{nameof( AsyncResourceFactoryProvider )}\" type.";
         }
      }
      else
      {
         errorMessage = "Task must be provided callback to load NuGet packages (just make constructor taking it as argument and use UtilPack.NuGet.MSBuild task factory).";
      }

      return (retVal, errorMessage);
   }

   private static Boolean IsAssignableFromIgnoreAssemblyVersion( this TTypeInfo parentType, TTypeInfo childType )
   {
      return parentType.IsAssignableFrom( childType ) || childType.AsDepthFirstEnumerable( t => t.BaseType?.GetTypeInfo().Singleton().Concat( t.
#if NET40
         GetInterfaces()
#else
         ImplementedInterfaces
#endif
         .Select( i => i.GetTypeInfo() )
            ) ).Any( t =>
            String.Equals( t.Namespace, parentType.Namespace )
            && String.Equals( t.Name, parentType.Name )
            && String.Equals( t.Assembly.GetName().Name, parentType.Assembly.GetName().Name )
            && ArrayEqualityComparer<Byte>.ArrayEquality( parentType.Assembly.GetName().GetPublicKeyToken(), t.Assembly.GetName().GetPublicKeyToken() )
            );
   }

   //private static Object ProvideResourcePoolCreationParameters(
   //   ResourcePoolDynamicCreationConfiguration configuration,
   //   AsyncResourceFactoryProvider poolProvider
   //   )
   //{
   //   var contents = configuration.PoolConfigurationFileContents;
   //   IFileProvider fileProvider;
   //   String path;
   //   if ( !String.IsNullOrEmpty( contents ) )
   //   {
   //      path = StringContentFileProvider.PATH;
   //      fileProvider = new StringContentFileProvider( contents );
   //   }
   //   else
   //   {
   //      path = configuration.PoolConfigurationFilePath;
   //      if ( String.IsNullOrEmpty( path ) )
   //      {
   //         throw new InvalidOperationException( "Configuration file path was not provided." );
   //      }
   //      else
   //      {
   //         path = System.IO.Path.GetFullPath( path );
   //         fileProvider = null; // Use defaults
   //      }
   //   }


   //   return new ValueTask<Object>( new ConfigurationBuilder()
   //      .AddJsonFile( fileProvider, path, false, false )
   //      .Build()
   //      .Get( poolProvider.DataTypeForCreationParameter ) );
   //}

   //private static AsyncResourcePool<TResource> AcquireResourcePool<TResource>(
   //   AsyncResourceFactoryProvider poolProvider,
   //   Object poolCreationArgs
   //   )
   //{
   //   return poolProvider
   //      .BindCreationParameters<TResource>( poolCreationArgs )
   //      .CreateOneTimeUseResourcePool()
   //      .WithoutExplicitAPI();
   //}
}