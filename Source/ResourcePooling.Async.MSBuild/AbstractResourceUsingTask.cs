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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using ResourcePooling.Async.Abstractions;
using ResourcePooling.Async.ConfigurationLoading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using TNuGetPackageResolverCallback = System.Func<System.String, System.String, System.String, System.Threading.CancellationToken, System.Threading.Tasks.Task<System.Reflection.Assembly>>;

namespace ResourcePooling.Async.MSBuild
{


   /// <summary>
   /// This class contains skeleton implementation for MSBuild task, which will create <see cref="AsyncResourcePool{TResource}"/> and use the resource once.
   /// </summary>
   /// <typeparam name="TResource">The actual type of resource.</typeparam>
   /// <remarks>
   /// This task is meant to be used with <see href="">UtilPack.NuGet.MSBuild</see> task factory, since this task will dynamically load NuGet packages.
   /// More specifically, this task should be loaded using 
   /// </remarks>
   public abstract class AbstractResourceUsingTask<TResource> : Microsoft.Build.Utilities.Task, ICancelableTask, ResourceFactoryDynamicCreationNuGetBasedConfiguration
   {
      private readonly TNuGetPackageResolverCallback _nugetPackageResolver;

      /// <summary>
      /// Initializes a new instance of <see cref="AbstractResourceUsingTask{TResource}"/> with given callback to load NuGet assemblies.
      /// </summary>
      /// <param name="nugetAssemblyLoader">The callback to asynchronously load assembly based on NuGet package ID and version.</param>
      /// <remarks>
      /// This constructor will not throw <see cref="ArgumentNullException"/> if  <paramref name="nugetAssemblyLoader"/> is <c>null</c>.
      /// Instead, the <see cref="Execute"/> method will log an error if <paramref name="nugetAssemblyLoader"/> was <c>null</c>.
      /// This is done to prevent ugly exception and error messages when this task is used.
      /// </remarks>
      public AbstractResourceUsingTask(
         TNuGetPackageResolverCallback nugetAssemblyLoader
         )
      {
         this.CancellationTokenSource = new CancellationTokenSource();
         this._nugetPackageResolver = nugetAssemblyLoader;
      }

      /// <summary>
      /// This method implements <see cref="ITask.Execute"/> and will take care of call <see cref="IBuildEngine3.Yield"/> if <see cref="RunSynchronously"/> is <c>true</c>, and then perform the task.
      /// </summary>
      /// <returns><c>true</c> if <see cref="Microsoft.Build.Utilities.TaskLoggingHelper.HasLoggedErrors"/> of this <see cref="Microsoft.Build.Utilities.Task.Log"/> returns <c>false</c> and no exceptions occur; <c>false</c> otherwise.</returns>
      /// <remarks>
      /// This method calls other methods in the following order:
      /// <list type="number">
      /// <item><description><see cref="CheckTaskParametersBeforeResourcePoolUsage"/>,</description></item>
      /// <item><description><see cref="AcquireResourcePoolProvider"/>,</description></item>
      /// <item><description><see cref="ProvideResourcePoolCreationParameters"/>,</description></item>
      /// <item><description><see cref="AcquireResourcePool"/>, and</description></item>
      /// <item><description><see cref="UseResource"/>.</description></item>
      /// </list>
      /// </remarks>
      /// <seealso cref="CheckTaskParametersBeforeResourcePoolUsage"/>
      /// <seealso cref="AcquireResourcePoolProvider"/>
      /// <seealso cref="ProvideResourcePoolCreationParameters"/>
      /// <seealso cref="AcquireResourcePool"/>
      /// <seealso cref="UseResource"/>
      public override Boolean Execute()
      {
         // Reacquire must be called in same thread as yield -> run our Task synchronously
         var retVal = false;
         if ( this.CheckTaskParametersBeforeResourcePoolUsage() )
         {
            var yieldCalled = false;
            var be = (IBuildEngine3) this.BuildEngine;
            try
            {
               try
               {
                  if ( !this.RunSynchronously )
                  {
                     be.Yield();
                     yieldCalled = true;
                  }
                  this.ExecuteTaskAsync().GetAwaiter().GetResult();

                  retVal = !this.Log.HasLoggedErrors;
               }
               catch ( OperationCanceledException )
               {
                  // Canceled, do nothing
               }
               catch ( Exception exc )
               {
                  // Only log if we did not receive cancellation
                  if ( !this.CancellationTokenSource.IsCancellationRequested )
                  {
                     this.Log.LogErrorFromException( exc );
                  }
               }
            }
            finally
            {
               if ( yieldCalled )
               {
                  be.Reacquire();
               }
            }
         }
         return retVal;
      }

      /// <summary>
      /// This method implements <see cref="ICancelableTask.Cancel"/> and will cancel the <see cref="CancellationToken"/> used by this task.
      /// </summary>
      public void Cancel()
      {
         this.CancellationTokenSource.Cancel( false );
      }

      private async Task<Boolean> ExecuteTaskAsync()
      {
         var factory = await this.AcquireResourceFactory();
         var retVal = factory != null;
         if ( retVal )
         {
            return await factory
               .CreateOneTimeUseResourcePool()
               .UseResourceAsync( this.UseResource, this.CancellationToken );
         }
         return retVal;
      }

      private async Task<AsyncResourceFactory<TResource>> AcquireResourceFactory()
      {
         try
         {
            return await this.CreateAsyncResourceFactoryUsingConfiguration<TResource>( this._nugetPackageResolver, this.CancellationToken );
         }
         catch ( Exception e )
         {
            this.Log.LogError( e.Message );
            return null;
         }
      }

      /// <summary>
      /// Derived classes may use this property to get the <see cref="System.Threading.CancellationToken"/> used by this task.
      /// </summary>
      /// <value>The <see cref="System.Threading.CancellationToken"/> used by this task.</value>
      protected CancellationToken CancellationToken => this.CancellationTokenSource.Token;

      /// <summary>
      /// Derived classes may use this property to get the <see cref="System.Threading.CancellationTokenSource"/> used by this task.
      /// </summary>
      /// <value>The <see cref="System.Threading.CancellationTokenSource"/> used by this task.</value>
      protected CancellationTokenSource CancellationTokenSource { get; }

      /// <summary>
      /// Derived classes should implement this method in order to perform any domain-specific checks before the task actually starts executing in <see cref="Execute"/>.
      /// Such checks may be e.g. validation that file paths are correct and the corresponding files exist, etc.
      /// </summary>
      /// <returns><c>true</c> if all checks are ok; <c>false</c> otherwise.</returns>
      /// <remarks>
      /// This method should take care of logging any error messages related to failed checks, the <see cref="Execute"/> method which calls this method will not log anything if this returns <c>false</c>.
      /// </remarks>
      protected abstract Boolean CheckTaskParametersBeforeResourcePoolUsage();

      ///// <summary>
      ///// This method is called by <see cref="Execute"/> after calling <see cref="AcquireResourcePoolProvider"/> in order to potentially asynchronously provide argument object for <see cref="AsyncResourceFactoryProvider.BindCreationParameters"/> method.
      ///// </summary>
      ///// <param name="poolProvider">The pool provider returned by <see cref="AcquireResourcePoolProvider"/> method.</param>
      ///// <returns>A parameter for <see cref="AsyncResourceFactoryProvider.BindCreationParameters"/> method.</returns>
      ///// <remarks>
      ///// This implementation uses <see cref="ConfigurationBuilder"/> in conjunction of <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, String)"/> method to add configuration JSON file, contents of which are specified by <see cref="PoolConfigurationFileContents"/> property, or it is located in path specified by <see cref="PoolConfigurationFilePath"/> property.
      ///// Then, the <see cref="ConfigurationBinder.Get(IConfiguration, Type)"/> method is used on resulting <see cref="IConfiguration"/> to obtain the return value of this method.
      ///// </remarks>
      ///// <seealso cref="PoolConfigurationFileContents"/>
      ///// <seealso cref="PoolConfigurationFilePath"/>
      //protected virtual ValueTask<Object> ProvideResourcePoolCreationParameters(
      //   AsyncResourceFactoryProvider poolProvider
      //   )
      //{
      //   var contents = this.PoolConfigurationFileContents;
      //   IFileProvider fileProvider;
      //   String path;
      //   if ( !String.IsNullOrEmpty( contents ) )
      //   {
      //      path = StringContentFileProvider.PATH;
      //      fileProvider = new StringContentFileProvider( contents );
      //   }
      //   else
      //   {
      //      path = this.PoolConfigurationFilePath;
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

      ///// <summary>
      ///// This method is called by <see cref="Execute"/> after calling <see cref="ProvideResourcePoolCreationParameters"/>
      ///// </summary>
      ///// <param name="poolProvider">The pool provider returned by <see cref="AcquireResourcePoolProvider"/> method.</param>
      ///// <param name="poolCreationArgs">The creation arguments returned by <see cref="ProvideResourcePoolCreationParameters"/> method.</param>
      ///// <returns>An instance of <see cref="AsyncResourcePool{TResource}"/> to use.</returns>
      ///// <remarks>
      ///// This implementation returns one-time use async resource pool.
      ///// </remarks>
      //protected virtual ValueTask<AsyncResourcePool<TResource>> AcquireResourcePool(
      //   AsyncResourceFactoryProvider poolProvider,
      //   Object poolCreationArgs
      //   )
      //{
      //   return new ValueTask<AsyncResourcePool<TResource>>( poolProvider
      //      .BindCreationParameters<TResource>( poolCreationArgs )
      //      .CreateOneTimeUseResourcePool()
      //      .WithoutExplicitAPI()
      //      );
      //}

      /// <summary>
      /// Derived classes should implement this method to perform domain-specific functionality using the given resource.
      /// </summary>
      /// <param name="resource">The resource obtained from <see cref="AsyncResourcePool{TResource}"/>, which was returned by <see cref="AcquireResourcePool"/> method.</param>
      /// <returns></returns>
      protected abstract Task<Boolean> UseResource( TResource resource );

      ///// <summary>
      ///// This method is called by <see cref="Execute"/> after calling <see cref="CheckTaskParametersBeforeResourcePoolUsage"/>.
      ///// </summary>
      ///// <returns>Potentially asynchronously returns <see cref="AsyncResourceFactoryProvider"/> to be used to acquire resource pool.</returns>
      ///// <remarks>
      ///// <para>
      ///// This method will return <c>null</c> instead of throwing an exception, if acquiring a <see cref="AsyncResourceFactoryProvider"/> fails.
      ///// </para>
      ///// <para>
      ///// This method uses values of <see cref="PoolProviderPackageID"/>, <see cref="PoolProviderVersion"/>, <see cref="PoolProviderAssemblyPath"/>, and <see cref="PoolProviderTypeName"/> properties when dynamically instantiating <see cref="AsyncResourceFactoryProvider"/>.
      ///// </para>
      ///// </remarks>
      //protected virtual async ValueTask<AsyncResourceFactoryProvider> AcquireResourcePoolProvider()
      //{
      //   var resolver = this._nugetPackageResolver;
      //   AsyncResourceFactoryProvider retVal = null;
      //   if ( resolver != null )
      //   {
      //      var packageID = this.PoolProviderPackageID;
      //      if ( !String.IsNullOrEmpty( packageID ) )
      //      {
      //         try
      //         {
      //            var assembly = await resolver(
      //               packageID, // package ID
      //               this.PoolProviderVersion,  // optional package version
      //               this.PoolProviderAssemblyPath // optional assembly path within package
      //               );
      //            if ( assembly != null )
      //            {
      //               // Now search for the type
      //               var typeName = this.PoolProviderTypeName;
      //               var parentType = typeof( AsyncResourceFactoryProvider ).GetTypeInfo();
      //               var checkParentType = !String.IsNullOrEmpty( typeName );
      //               Type providerType;
      //               if ( checkParentType )
      //               {
      //                  // Instantiate directly
      //                  providerType = assembly.GetType( typeName, false, false );
      //               }
      //               else
      //               {
      //                  // Search for first available
      //                  providerType = assembly.DefinedTypes.FirstOrDefault( t => !t.IsInterface && !t.IsAbstract && t.IsPublic && parentType.IsAssignableFrom( t ) )?.AsType();
      //               }

      //               if ( providerType != null )
      //               {
      //                  if ( !checkParentType || parentType.IsAssignableFrom( providerType.GetTypeInfo() ) )
      //                  {
      //                     // All checks passed, instantiate the pool provider
      //                     retVal = (AsyncResourceFactoryProvider) Activator.CreateInstance( providerType );
      //                  }
      //                  else
      //                  {
      //                     this.Log.LogError( $"The type \"{providerType.FullName}\" in \"{assembly}\" does not have required parent type \"{parentType.FullName}\"." );
      //                  }
      //               }
      //               else
      //               {
      //                  this.Log.LogError( $"Failed to find type within assembly in \"{assembly}\", try specify \"{nameof( this.PoolProviderTypeName )}\" parameter." );
      //               }
      //            }
      //            else
      //            {
      //               this.Log.LogError( $"Failed to load resource pool provider package \"{packageID}\"." );
      //            }
      //         }
      //         catch ( Exception exc )
      //         {
      //            this.Log.LogError( $"An exception occurred when acquiring resource pool provider: {exc.Message}" );
      //         }
      //      }
      //      else
      //      {
      //         this.Log.LogError( $"No NuGet package ID were provided as \"{nameof( this.PoolProviderPackageID )}\" parameter. The package ID should be of the package holding implementation for \"{nameof( AsyncResourceFactoryProvider )}\" type." );
      //      }
      //   }
      //   else
      //   {
      //      this.Log.LogError( "Task must be provided callback to load NuGet packages (just make constructor taking it as argument and use UtilPack.NuGet.MSBuild task factory)." );
      //   }

      //   return retVal;
      //}

      /// <summary>
      /// Gets or sets the value indicating whether to call <see cref="IBuildEngine3.Yield"/> or not.
      /// By default, the <see cref="IBuildEngine3.Yield"/> will be called.
      /// </summary>
      /// <value>The value indicating whether to call <see cref="IBuildEngine3.Yield"/> or not.</value>
      public Boolean RunSynchronously { get; set; }

      /// <summary>
      /// Gets or sets the value for NuGet package ID of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.
      /// </summary>
      /// <value>The value for NuGet package ID of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.</value>
      /// <remarks>
      /// This property is used by <see cref="AcquireResourcePoolProvider"/> when loading an instance of <see cref="AsyncResourceFactoryProvider"/>.
      /// </remarks>
      /// <seealso cref="PoolProviderVersion"/>
      public String PoolProviderPackageID { get; set; }

      /// <summary>
      /// Gets or sets the value for NuGet package version of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.
      /// </summary>
      /// <value>The value for NuGet package version of the package holding the type implementing <see cref="AsyncResourceFactoryProvider"/>.</value>
      /// <remarks>
      /// This property is used by <see cref="AcquireResourcePoolProvider"/> when loading an instance of <see cref="AsyncResourceFactoryProvider"/>.
      /// The value, if specified, should be parseable into <see cref="T:NuGet.Versioning.VersionRange"/>.
      /// If left out, then the newest version will be used, but this will cause additional overhead when querying for the newest version.
      /// </remarks>
      public String PoolProviderVersion { get; set; }

      /// <summary>
      /// Gets or sets the path within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties where the assembly holding type implementing <see cref="AsyncResourceFactoryProvider"/> resides.
      /// </summary>
      /// <value>The path within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties where the assembly holding type implementing <see cref="AsyncResourceFactoryProvider"/> resides.</value>
      /// <remarks>
      /// This property will be used only for NuGet packages with more than assembly within its framework-specific folder.
      /// </remarks>
      public String PoolProviderAssemblyPath { get; set; }

      /// <summary>
      /// Gets or sets the name of the type implementing <see cref="AsyncResourceFactoryProvider"/>, located in assembly within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties.
      /// </summary>
      /// <value>The name of the type implementing <see cref="AsyncResourceFactoryProvider"/>, located in assembly within NuGet package specified by <see cref="PoolProviderPackageID"/> and <see cref="PoolProviderVersion"/> properties.</value>
      /// <remarks>
      /// This value can be left out so that <see cref="AcquireResourcePoolProvider"/> will search for all types within package implementing <see cref="AsyncResourceFactoryProvider"/> and use the first suitable one.
      /// </remarks>
      public String PoolProviderTypeName { get; set; }

      /// <summary>
      /// Gets or sets the path to the configuration file holding creation parameter for <see cref="AsyncResourceFactoryProvider.BindCreationParameters"/>.
      /// </summary>
      /// <value>The path to the configuration file holding creation parameter for <see cref="AsyncResourceFactoryProvider.BindCreationParameters"/>.</value>
      /// <remarks>
      /// This property is used by <see cref="ProvideResourcePoolCreationParameters"/> method.
      /// This property should not be used together with <see cref="PoolConfigurationFileContents"/>, because <see cref="PoolConfigurationFileContents"/> takes precedence over this property.
      /// </remarks>
      /// <seealso cref="ProvideResourcePoolCreationParameters"/>
      /// <seealso cref="PoolConfigurationFileContents"/>
      public String PoolConfigurationFilePath { get; set; }

      /// <summary>
      /// Gets or sets the configuration file contents in-place, instead of using <see cref="PoolConfigurationFilePath"/> file path.
      /// This property takes precedence over <see cref="PoolConfigurationFilePath"/>
      /// </summary>
      /// <remarks>
      /// This property is used by <see cref="ProvideResourcePoolCreationParameters"/> method.
      /// </remarks>
      /// <seealso cref="ProvideResourcePoolCreationParameters"/>
      /// <seealso cref="PoolConfigurationFilePath"/>
      public String PoolConfigurationFileContents { get; set; }
   }
}
