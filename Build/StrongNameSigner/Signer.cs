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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace StrongNameSigner
{
   public class Signer
   {
      private readonly RSAParameters _key;

      public Signer(
         RSAParameters key
         )
      {
         this._key = key;
      }

      // The returned DirectoryEntry is actually raw offset and not RVA!
      public async Task<(ImmutableArray<Byte> Signature, Int32 SignatureOffsetInFile, Int32 CorFlagsOffsetInFile)?> Sign(
         String assemblyPath,
         CancellationToken token
         )
      {
         (ImmutableArray<Byte>, Int32, Int32)? retVal = null;
         using ( var stream = File.Open( assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
         {
            IEnumerable<StreamRange> ranges;
            RawDirectoryEntry snDir;
            Int32 corFlagsOffset;
            using ( var reader = new PEReader( stream, PEStreamOptions.LeaveOpen ) )
            {
               var headers = reader.PEHeaders;
               (snDir, ranges) = this.GetHashableRanges( headers );
               corFlagsOffset = headers.CorHeaderStartOffset + 16; // ECMA-335, II.25.3.3 CLIheader, Flags field
            }

            if ( ranges != null )
            {
               var signature = ( await this.GetStrongNameSignatureAsync(
                  stream,
                  ranges,
                  corFlagsOffset,
                  token
                  ) )?.ToImmutableArray();
               if ( signature is ImmutableArray<Byte> nonNullSignature && nonNullSignature.Length > 0 && nonNullSignature.Length <= snDir.Size )
               {
                  retVal = (nonNullSignature, snDir.OffsetInFile, corFlagsOffset);
               }
            }
         }

         return retVal;

      }

      private (RawDirectoryEntry, IEnumerable<StreamRange>) GetHashableRanges(
         PEHeaders headers
         )
      {
         var snDir = headers.CorHeader.StrongNameSignatureDirectory;
         RawDirectoryEntry rawSNDir;
         return snDir.Size > 0 && headers.TryGetDirectoryOffset( snDir, out var snOffset ) ?
            (rawSNDir = new RawDirectoryEntry( snOffset, snDir.Size ), this.CalculateRangesWithValidSNDir( headers, rawSNDir )) :
            default;
      }

      private IEnumerable<StreamRange> CalculateRangesWithValidSNDir(
         PEHeaders headers,
         RawDirectoryEntry snDir
         )
      {
         // ECMA-335, page 117:
         // Except for the following, all portions of the PE File are hashed: 
         // - The Authenticode Signature entry: PE files can be authenticode signed. The authenticode signature is contained in the 8 - byte entry 
         //   at offset 128 of the PE Header Data Directory(“Certificate Table” in §II.25.2.3.3) and the contents of the PE File in the range specified by this directory entry.
         // - The Strong Name Blob: The 8 - byte entry at offset 32 of the CLI Header (“StrongNameSignature” in §II.25.3.3) and the contents of the hash data contained
         //   at this RVA in the PE File.If the 8 - byte entry is 0, there is no associated strong name signature.
         // - The PE Header Checksum: The 4 - byte entry at offset 64 of the PE Header Windows NT-Specific Fields(“File Checksum” in §II.25.2.3.2).

         // Basically, we hash unaligned PE header (with zeroed-out checksum and authenticode data directory) and everything else except strong name signature entry
         var peHeader = headers.PEHeader;
         if ( peHeader.CheckSum != 0 || peHeader.CertificateTableDirectory.Size > 0 )
         {
            throw new NotImplementedException( "TODO: Ability to skip the checksum and authenticode signature entry" );
         }

         var sectionHeaders = headers.SectionHeaders;
         var machine = headers.CoffHeader.Machine;
         var peHeadersSizeSignable = ComputeSizeOfPEHeaders(
            sectionHeaders.Length,
            machine != Machine.Amd64 && machine != Machine.IA64 && machine != Machine.Arm64
            );
         var peHeadersSizeAligned = RoundUpI32( peHeadersSizeSignable, peHeader.FileAlignment );
         var snOffset = snDir.OffsetInFile;
         var snAfterLast = snOffset + snDir.Size;
         var isFirst = true;

         foreach ( var sectionHeader in sectionHeaders )
         {
            var start = sectionHeader.PointerToRawData;
            var length = sectionHeader.SizeOfRawData;
            if ( isFirst )
            {
               length += start;
               start = 0;
               isFirst = false;
            }
            while ( length > 0 )
            {
               if ( peHeadersSizeAligned > 0 )
               {
                  Int32 peLength;
                  if ( peHeadersSizeSignable > 0 )
                  {
                     peLength = Math.Min( peHeadersSizeSignable, length );
                     yield return new StreamRange( start, peLength );
                     peHeadersSizeSignable -= peLength;
                  }
                  else
                  {
                     peLength = Math.Min( peHeadersSizeAligned, length );
                  }

                  peHeadersSizeAligned -= peLength;
                  start += peLength;
                  length -= peLength;
               }
               else
               {
                  if ( start <= snOffset && start + length >= snAfterLast )
                  {
                     // This section contains strong name directory
                     if ( start < snOffset )
                     {
                        yield return new StreamRange( start, snOffset - start );
                     }
                     if ( start + length > snAfterLast )
                     {
                        yield return new StreamRange( snAfterLast, start + length - snAfterLast );
                     }
                  }
                  else
                  {
                     // Normal section without strong name directory
                     yield return new StreamRange( start, length );
                  }

                  // Break from while-loop
                  length = 0;

               }
            }
         }
      }

      // Copypasta from UtilPack and old CAM project
      public static Int32 RoundUpI32( Int32 value, Int32 multiple )
      {
         return ( multiple - 1 + value ) & ~( multiple - 1 );
      }

      private static Int32 ComputeSizeOfPEHeaders(
         Int32 sectionCount,
         Boolean is32Bit
         )
      {
         return 128 // DOS header
            + 4 + 20 + ComputeSizeOfPEHeader( is32Bit ) + 40 * sectionCount;
      }

      private static Int32 ComputeSizeOfPEHeader( Boolean is32Bit )
      {
         return 72 + 4 * ( is32Bit ? 4 : 8 ) + 4 + 4 + 128;
      }

      private async Task<Byte[]> GetStrongNameSignatureAsync(
         Stream stream,
         IEnumerable<StreamRange> hashableRanges,
         Int32 corFlagsOffset,
         CancellationToken token
         )
      {
         using ( var rsa = RSA.Create() )
         {
            rsa.ImportParameters( this._key );
            var hash = rsa.SignHash(
               await this.CalculateHash( stream, hashableRanges, corFlagsOffset, token ),
               HashAlgorithmName.SHA1,
               RSASignaturePadding.Pkcs1
               );
            Array.Reverse( hash );
            return hash;
         }
      }

      private async Task<Byte[]> CalculateHash(
         Stream stream,
         IEnumerable<StreamRange> hashableRanges,
         Int32 corFlagsOffset,
         CancellationToken token
         )
      {
         const Int32 BUFFER_SIZE = 4096;
         var bytes = new Byte[BUFFER_SIZE];
         using ( var hasher = IncrementalHash.CreateHash( HashAlgorithmName.SHA1 ) )
         {
            var dummy = hashableRanges.ToArray();
            foreach ( var range in hashableRanges.OrderBy( r => r.StartFromBeginning ) )
            {
               var start = range.StartFromBeginning;
               stream.Seek( start, SeekOrigin.Begin );

               var remaining = range.Count;
               Int32 length;
               do
               {
                  var prev = stream.Position;
                  length = await stream.ReadAsync( bytes, 0, Math.Min( remaining, BUFFER_SIZE ), token );
                  if ( prev <= corFlagsOffset && prev + length > corFlagsOffset )
                  {
                     GetCorFlagsLittleByte( ref bytes[corFlagsOffset - prev] );
                  }
                  hasher.AppendData( bytes, 0, length );
                  remaining -= length;
               } while ( length > 0 && remaining > 0 );
            }

            return hasher.GetHashAndReset();
         }
      }


      private struct StreamRange
      {
         public StreamRange( Int32 start, Int32 count )
         {
            if ( start < 0 )
            {
               throw new ArgumentException( nameof( start ) );
            }

            if ( count < 0 )
            {
               count = 0;
            }

            this.StartFromBeginning = start;
            this.Count = count;
         }
         public Int32 StartFromBeginning { get; }
         public Int32 Count { get; }
      }

      public static void GetCorFlagsLittleByte( ref Byte currentByte )
      {
         currentByte = (Byte) ( currentByte | (Int32) CorFlags.StrongNameSigned );
      }

   }

   public struct RawDirectoryEntry
   {
      public RawDirectoryEntry( Int32 offset, Int32 size )
      {
         this.OffsetInFile = offset;
         this.Size = size;
      }
      public Int32 OffsetInFile { get; }
      public Int32 Size { get; }
   }
}
