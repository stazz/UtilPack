/*
 * Copyright 2019 Stanislav Muhametsin. All rights Reserved.
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
using System;
using System.Text;

namespace UtilPack.JSON.Configuration
{
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

   /// <summary>
   /// This class contains extension methods for types defined in other assemblies.
   /// </summary>
   public static partial class JsonConfigurationExtensions
   {
      /// <summary>
      /// Adds the given string as JSON content for this <see cref="IConfigurationBuilder"/>.
      /// </summary>
      /// <param name="builder">This <see cref="IConfigurationBuilder"/>.</param>
      /// <param name="textualContents">The JSON contents, as <see cref="String"/>.</param>
      /// <returns>The <paramref name="builder"/>.</returns>
      public static IConfigurationBuilder AddJsonContents( this IConfigurationBuilder builder, String textualContents )
      {
         return ArgumentValidator.ValidateNotNullReference( builder )
            .AddJsonFile( new StringContentFileProvider( textualContents ), StringContentFileProvider.PATH, false, false );
      }

      /// <summary>
      /// Adds the given serialized string as JSON content for this <see cref="IConfigurationBuilder"/>.
      /// </summary>
      /// <param name="builder">This <see cref="IConfigurationBuilder"/>.</param>
      /// <param name="stringAsBytes">The JSON contents, as <see cref="Byte"/> array.</param>
      /// <returns>The <paramref name="builder"/>.</returns>
      public static IConfigurationBuilder AddJsonContents( this IConfigurationBuilder builder, Byte[] stringAsBytes )
      {
         return ArgumentValidator.ValidateNotNullReference( builder )
            .AddJsonFile( new StringContentFileProvider( stringAsBytes ), StringContentFileProvider.PATH, false, false );
      }
   }
}
