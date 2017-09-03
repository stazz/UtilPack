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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;
using UtilPack.ResourcePooling;

using TNuGetPackageResolverCallback = System.Func<System.String, System.String, System.String, System.Threading.Tasks.Task<System.Reflection.Assembly>>;

namespace UtilPack.ResourcePooling.MSBuild
{
   /// <summary>
   /// This class contains skeleton implementation for MSBuild task, which will create <see cref="AsyncResourcePool{TResource}"/> and use the resource once.
   /// </summary>
   /// <typeparam name="TResource">The actual type of resource.</typeparam>
   /// <remarks>
   /// This task is meant to be used with <see href="">UtilPack.NuGet.MSBuild</see> task factory, since this task will dynamically load NuGet packages.
   /// </remarks>
   public abstract class AbstractResourceUsingTask<TResource> : Microsoft.Build.Utilities.Task, ICancelableTask
   {
      private readonly CancellationTokenSource _cancellationSource;
      private readonly TNuGetPackageResolverCallback _nugetPackageResolver;

      /// <summary>
      /// Creates a new instance of <see cref="AbstractResourceUsingTask{TResource}"/> with given callback to load NuGet assemblies.
      /// </summary>
      /// <param name="nugetAssemblyLoader">The callback to asynchronously load assembly based on NuGet package ID and version.</param>
      /// <remarks>
      /// This constructor will not throw <see cref="ArgumentNullException"/> if  <paramref name="nugetAssemblyLoader"/> is <c>null</c>.
      /// Instead, the <see cref="Execute"/> method will log an error if <paramref name="nugetAssemblyLoader"/> was <c>null</c>.
      /// This is done to prevent ugly exception and error messages when this task is used.
      /// </remarks>
      public AbstractResourceUsingTask( TNuGetPackageResolverCallback nugetAssemblyLoader )
      {
         this._cancellationSource = new CancellationTokenSource();
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
                  if ( !this._cancellationSource.IsCancellationRequested )
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
         this._cancellationSource.Cancel( false );
      }

      private async Task<Boolean> ExecuteTaskAsync()
      {
         var poolProvider = await this.AcquireResourcePoolProvider();
         Boolean retVal;
         if ( poolProvider == null )
         {
            this.Log.LogError( "Failed to acquire resource pool provider." );
            retVal = false;
         }
         else
         {
            var poolCreationArgs = await this.ProvideResourcePoolCreationParameters( poolProvider );
            var pool = await this.AcquireResourcePool( poolProvider, poolCreationArgs );
            Func<TResource, Task<Boolean>> func = this.UseResource;
            retVal = await pool.UseResourceAsync<TResource, Boolean>( func, this._cancellationSource.Token );
         }

         return retVal;
      }

      /// <summary>
      /// Derived classes should implement this method in order to perform any domain-specific checks before the task actually starts executing in <see cref="Execute"/>.
      /// Such checks may be e.g. validation that file paths are correct and the corresponding files exist, etc.
      /// </summary>
      /// <returns><c>true</c> if all checks are ok; <c>false</c> otherwise.</returns>
      /// <remarks>
      /// This method should take care of logging any error messages related to failed checks, the <see cref="Execute"/> method which calls this method will not log anything if this returns <c>false</c>.
      /// </remarks>
      protected abstract Boolean CheckTaskParametersBeforeResourcePoolUsage();

      /// <summary>
      /// This method is called by <see cref="Execute"/> after calling <see cref="AcquireResourcePoolProvider"/> in order to potentially asynchronously provide argument object for <see cref="AsyncResourcePoolProvider.CreateOneTimeUseResourcePool"/> method.
      /// </summary>
      /// <param name="poolProvider">The pool provider returned by <see cref="AcquireResourcePoolProvider"/> method.</param>
      /// <returns>A parameter for <see cref="AsyncResourcePoolProvider{TResource}.CreateOneTimeUseResourcePool"/> or <see cref="AsyncResourcePoolProvider{TResource}.CreateTimeoutingResourcePool"/> method.</returns>
      /// <remarks>
      /// This implementation uses <see cref="ConfigurationBuilder"/> in conjunction of <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, string)"/> method to add configuration JSON file located in <see cref="PoolConfigurationFilePath"/>.
      /// Then, the <see cref="ConfigurationBinder.Get(IConfiguration, Type)"/> method is used on resulting <see cref="IConfiguration"/> to obtain the return value of this method.
      /// </remarks>
      protected virtual ValueTask<Object> ProvideResourcePoolCreationParameters(
         AsyncResourcePoolProvider<TResource> poolProvider
         )
      {
         var path = this.PoolConfigurationFilePath;
         if ( String.IsNullOrEmpty( path ) )
         {
            throw new InvalidOperationException( "Configuration file path was not provided." );
         }

         return new ValueTask<Object>( new ConfigurationBuilder()
            .AddJsonFile( System.IO.Path.GetFullPath( path ) )
            .Build()
            .Get( poolProvider.DefaultTypeForCreationParameter ) );
      }

      /// <summary>
      /// This method is called by <see cref="Execute"/> after calling <see cref="ProvideResourcePoolCreationParameters"/>
      /// </summary>
      /// <param name="poolProvider">The pool provider returned by <see cref="AcquireResourcePoolProvider"/> method.</param>
      /// <param name="poolCreationArgs">The creation arguments returned by <see cref="ProvideResourcePoolCreationParameters"/> method.</param>
      /// <returns>An instance of <see cref="AsyncResourcePool{TResource}"/> to use.</returns>
      /// <remarks>
      /// This implementation returns the result of <see cref="AsyncResourcePoolProvider{TResource}.CreateOneTimeUseResourcePool"/>.
      /// </remarks>
      protected virtual ValueTask<AsyncResourcePool<TResource>> AcquireResourcePool(
         AsyncResourcePoolProvider<TResource> provider,
         Object poolCreationArgs
         )
      {
         return new ValueTask<AsyncResourcePool<TResource>>( provider.CreateOneTimeUseResourcePool( poolCreationArgs ) );
      }

      /// <summary>
      /// Derived classes should implement this method to perform domain-specific functionality using the given resource.
      /// </summary>
      /// <param name="resource">The resource obtained from <see cref="AsyncResourcePool{TResource}"/>, which was returned by <see cref="AcquireResourcePool"/> method.</param>
      /// <returns></returns>
      protected abstract Task<Boolean> UseResource( TResource resource );

      /// <summary>
      /// This method is called by <see cref="Execute"/> after calling <see cref="CheckTaskParametersBeforeResourcePoolUsage"/>.
      /// </summary>
      /// <returns>Potentially asynchronously returns <see cref="AsyncResourcePoolProvider{TResource}"/> to be used to acquire resource pool.</returns>
      /// <remarks>
      /// <para>
      /// This method will return <c>null</c> instead of throwing an exception, if acquiring a <see cref="AsyncResourcePoolProvider{TResource}"/> fails.
      /// </para>
      /// <para>
      /// This method uses values of <see cref="PoolProviderPackageID"/>, <see cref="PoolProviderVersion"/>, <see cref="PoolProviderAssemblyPath"/>, and <see cref="PoolProviderTypeName"/> properties when dynamically instantiating <see cref="AsyncResourcePoolProvider{TResource}"/>.
      /// </para>
      /// </remarks>
      protected virtual async ValueTask<AsyncResourcePoolProvider<TResource>> AcquireResourcePoolProvider()
      {
         var resolver = this._nugetPackageResolver;
         AsyncResourcePoolProvider<TResource> retVal = null;
         if ( resolver != null )
         {
            var assembly = await this._nugetPackageResolver(
               this.PoolProviderPackageID, // package ID
               this.PoolProviderVersion,  // optional package version
               this.PoolProviderAssemblyPath // optional assembly path within package
               );
            if ( assembly != null )
            {
               // Now search for the type
               var typeName = this.PoolProviderTypeName;
               var parentType = typeof( AsyncResourcePoolProvider<TResource> ).GetTypeInfo();
               var checkParentType = !String.IsNullOrEmpty( typeName );
               Type providerType;
               if ( checkParentType )
               {
                  // Instantiate directly
                  providerType = assembly.GetType( typeName, false, false );
               }
               else
               {
                  // Search for first available
                  providerType = assembly.DefinedTypes.FirstOrDefault( t => !t.IsInterface && !t.IsAbstract && parentType.IsAssignableFrom( t ) )?.AsType();
               }

               if ( providerType != null )
               {
                  if ( !checkParentType || parentType.IsAssignableFrom( providerType.GetTypeInfo() ) )
                  {
                     // All checks passed, instantiate the pool provider
                     retVal = (AsyncResourcePoolProvider<TResource>) Activator.CreateInstance( providerType );
                  }
                  else
                  {
                     this.Log.LogError( $"The type \"{providerType.FullName}\" in \"{assembly}\" does not have required parent type \"{parentType.FullName}\"." );
                  }
               }
               else
               {
                  this.Log.LogError( $"Failed to find type within assembly in \"{assembly}\", try specify \"{nameof( PoolProviderTypeName )}\" parameter." );
               }
            }
            else
            {
               this.Log.LogError( $"Failed to load resource pool provider package \"{this.PoolProviderPackageID}\"." );
            }
         }
         else
         {
            this.Log.LogError( "Task must be provided callback to load NuGet packages (just make constructor taking it as argument and use UtilPack.NuGet.MSBuild task factory)." );
         }

         return retVal;
      }

      public Boolean RunSynchronously { get; set; }

      [Required]
      public String PoolProviderPackageID { get; set; }

      public String PoolProviderVersion { get; set; }

      public String PoolProviderAssemblyPath { get; set; }

      public String PoolProviderTypeName { get; set; }

      public String PoolConfigurationFilePath { get; set; }
   }
}
