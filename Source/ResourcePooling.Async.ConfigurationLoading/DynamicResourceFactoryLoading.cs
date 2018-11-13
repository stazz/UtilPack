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
using Microsoft.Extensions.FileProviders;
using ResourcePooling.Async.Abstractions;
using ResourcePooling.Async.ConfigurationLoading;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UtilPack;

namespace ResourcePooling.Async.ConfigurationLoading
{
   public interface ResourceFactoryDynamicCreationNuGetBasedConfiguration : ResourceFactoryDynamicCreationConfiguration
   {
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
      String PoolConfigurationFilePath { get; }

      /// <summary>
      /// Gets or sets the configuration file contents in-place, instead of using <see cref="PoolConfigurationFilePath"/> file path.
      /// This property takes precedence over <see cref="PoolConfigurationFilePath"/>
      /// </summary>
      /// <remarks>
      /// This property is used by <see cref="ProvideResourcePoolCreationParameters"/> method.
      /// </remarks>
      /// <seealso cref="ProvideResourcePoolCreationParameters"/>
      /// <seealso cref="PoolConfigurationFilePath"/>
      String PoolConfigurationFileContents { get; }
   }

   public class DefaultResourceFactoryDynamicCreationNuGetBasedConfiguration : DefaultResourceFactoryDynamicCreationConfiguration, ResourceFactoryDynamicCreationNuGetBasedConfiguration
   {
      public String PoolConfigurationFilePath { get; set; }

      public String PoolConfigurationFileContents { get; set; }
   }

   public static class Defaults
   {
      public static Func<AsyncResourceFactoryProvider, Object> CreateDefaultCreationParametersProvider(
         ResourceFactoryDynamicCreationNuGetBasedConfiguration configuration
         )
      {
         return factoryProvider =>
         {
            var contents = configuration.PoolConfigurationFileContents;
            IFileProvider fileProvider;
            String path;
            if ( !String.IsNullOrEmpty( contents ) )
            {
               path = StringContentFileProvider.PATH;
               fileProvider = new StringContentFileProvider( contents );
            }
            else
            {
               path = configuration.PoolConfigurationFilePath;
               if ( String.IsNullOrEmpty( path ) )
               {
                  throw new InvalidOperationException( "Configuration file path was not provided." );
               }
               else
               {
                  path = System.IO.Path.GetFullPath( path );
                  fileProvider = null; // Use defaults
               }
            }


            return new ConfigurationBuilder()
               .AddJsonFile( fileProvider, path, false, false )
               .Build()
               .Get( factoryProvider.DataTypeForCreationParameter );
         };
      }
   }


   internal sealed class StringContentFileProvider : IFileProvider
   {
      public const String PATH = ":::non-existing:::";

      private readonly FileInfo _fileInfo;


      public StringContentFileProvider( String stringContents )
         : this( new FileInfo( stringContents ) )
      {

      }

      public StringContentFileProvider( Byte[] serializedContents )
         : this( new FileInfo( serializedContents ) )
      {
      }

      private StringContentFileProvider( FileInfo info )
      {
         this._fileInfo = ArgumentValidator.ValidateNotNull( nameof( info ), info );
      }



      public IDirectoryContents GetDirectoryContents( String subpath )
      {
         return NotFoundDirectoryContents.Singleton;
      }

      public IFileInfo GetFileInfo( String subpath )
      {
         return String.Equals( PATH, subpath, StringComparison.Ordinal ) ?
            this._fileInfo :
            null;
      }

      public Microsoft.Extensions.Primitives.IChangeToken Watch( String filter )
      {
         return ChangeToken.Instance;
      }

      private sealed class FileInfo : IFileInfo
      {

         private static readonly Encoding TheEncoding = new UTF8Encoding( false, false );

         private readonly Byte[] _contentsAsBytes;

         public FileInfo( String stringContents )
            : this( TheEncoding.GetBytes( stringContents ) )
         {

         }

         public FileInfo( Byte[] serializedContents )
         {
            this._contentsAsBytes = serializedContents;
         }

         public Boolean Exists => true;

         public Int64 Length => this._contentsAsBytes.Length;

         public String PhysicalPath => PATH;

         public String Name => PATH;

         public DateTimeOffset LastModified => DateTimeOffset.MinValue;

         public Boolean IsDirectory => false;

         public System.IO.Stream CreateReadStream()
         {
            return new System.IO.MemoryStream( this._contentsAsBytes, 0, this._contentsAsBytes.Length, false, false );
         }
      }

      private sealed class ChangeToken : Microsoft.Extensions.Primitives.IChangeToken
      {
         public static ChangeToken Instance = new ChangeToken();

         private ChangeToken()
         {

         }

         public Boolean HasChanged => false;

         public Boolean ActiveChangeCallbacks => true;

         public IDisposable RegisterChangeCallback(
            Action<Object> callback,
            Object state
            )
         {
            return NoOpDisposable.Instance;
         }
      }


   }

   internal static class Extensions
   {
      public static IConfigurationBuilder AddJsonContents( this IConfigurationBuilder builder, String textualContents )
      {
         return builder.AddJsonFile( new StringContentFileProvider( textualContents ), StringContentFileProvider.PATH, false, false );
      }

      public static IConfigurationBuilder AddJsonContents( this IConfigurationBuilder builder, Byte[] stringAsBytes )
      {
         return builder.AddJsonFile( new StringContentFileProvider( stringAsBytes ), StringContentFileProvider.PATH, false, false );
      }
   }
}

public static partial class E_ResourcePooling
{
   public static Task<AsyncResourceFactory<TResource>> CreateAsyncResourceFactoryUsingConfiguration<TResource>(
      this ResourceFactoryDynamicCreationNuGetBasedConfiguration configuration,
      Func<String, String, String, CancellationToken, Task<Assembly>> assemblyLoader,
      CancellationToken token,
      Func<AsyncResourceFactoryProvider, Object> creationParametersProvider = null
      )
   {
      return configuration.CreateAsyncResourceFactory<TResource>(
         assemblyLoader,
         creationParametersProvider ?? Defaults.CreateDefaultCreationParametersProvider( configuration ),
         token
         );
   }
}