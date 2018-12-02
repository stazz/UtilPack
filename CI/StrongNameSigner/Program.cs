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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StrongNameSigner
{
   static class Program
   {
      // First arg - key file, the rest are files
      public static async Task Main( String[] args )
      {
         var signer = new Signer( ReadFromSNKFile( args[0] ) );
         foreach ( var file in args.Skip( 1 ) )
         {
            var tuple = await signer.Sign( file, default );
            if ( tuple.HasValue )
            {
               (var signature, var snOffset, var corFlagsOffset) = tuple.Value;
               using ( var fs = File.Open( file, FileMode.Open, FileAccess.Write | FileAccess.Read, FileShare.Read ) )
               {
                  fs.Seek( corFlagsOffset, SeekOrigin.Begin );
                  var b = new Byte[1];
                  await fs.ReadAsync( b, 0, 1 );
                  Signer.GetCorFlagsLittleByte( ref b[0] );
                  fs.Seek( corFlagsOffset, SeekOrigin.Begin );
                  await fs.WriteAsync( b, 0, 1 );
                  fs.Seek( snOffset, SeekOrigin.Begin );
                  await fs.WriteAsync( signature.ToArray() );
               }
            }
         }
      }

      private static RSAParameters ReadFromSNKFile( String snkFile )
      {

         using ( var fs = File.Open( snkFile, FileMode.Open, FileAccess.Read, FileShare.Read ) )
         {
            fs.Seek( 12, SeekOrigin.Begin ); // Skip header TODO validate that these bytes match the public key bytes.
            using ( var reader = new BinaryReader( fs, Encoding.UTF8, false ) )
            {
               var modulusLength = reader.ReadInt32() / 8;
               var halfModulusLength = ( modulusLength + 1 ) / 2;
               return new RSAParameters()
               {
                  Exponent = reader.ReadBytes( 4 ).Reverse().SkipWhile( b => b == 0 ).ToArray(),
                  Modulus = reader.ReadBytesReversed( modulusLength ),
                  P = reader.ReadBytesReversed( halfModulusLength ),
                  Q = reader.ReadBytesReversed( halfModulusLength ),
                  DP = reader.ReadBytesReversed( halfModulusLength ),
                  DQ = reader.ReadBytesReversed( halfModulusLength ),
                  InverseQ = reader.ReadBytesReversed( halfModulusLength ),
                  D = reader.ReadBytesReversed( modulusLength )
               };
            }
         }
      }

      private static Byte[] ReadBytesReversed( this BinaryReader reader, Int32 size )
      {
         var retVal = reader.ReadBytes( size );
         Array.Reverse( retVal );
         return retVal;
      }

   }
}
