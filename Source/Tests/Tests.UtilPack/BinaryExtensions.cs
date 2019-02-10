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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Tests.UtilPack;
using UtilPack;

namespace Tests.UtilPack
{
   [TestClass]
   public class BinaryExtensions
   {
      [TestMethod]
      public void TestStreamExtensions()
      {
         using ( var stream = new MemoryStream( new Byte[20] ) )
         {
            Assert.AreEqual( 0, stream.Position );
            stream.SeekFromBegin( 10 );
            Assert.AreEqual( 10, stream.Position );
            stream.SeekFromCurrent( 10 );
            Assert.AreEqual( 20, stream.Position );
            stream.SeekFromCurrent( -10 );
            Assert.AreEqual( 10, stream.Position );
            stream.SeekFromBegin( 0 );
            Assert.AreEqual( 0, stream.Position );
         }
      }
   }

   [TestClass]
   public class ByteArraySerializationFuzzyTests
   {
      private static ByteArraySerializationFuzzyTestPerformer _performer = new ByteArraySerializationFuzzyTestPerformer();

      [TestMethod]
      public void TestSByte()
      {
         _performer.PerformTest(
            val => unchecked((SByte) val),
            UtilPackExtensions.WriteSByteToBytes,
            UtilPackExtensions.ReadSByteFromBytes,
            UtilPackExtensions.WriteSByteToBytesNoRef,
            UtilPackExtensions.ReadSByteFromBytesNoRef
            );
      }

      [TestMethod]
      public void TestByte()
      {
         _performer.PerformTest(
            val => unchecked((Byte) val),
            UtilPackExtensions.WriteByteToBytes,
            UtilPackExtensions.ReadByteFromBytes,
            UtilPackExtensions.WriteByteToBytesNoRef,
            UtilPackExtensions.ReadByteFromBytesNoRef
            );
      }

      [TestMethod]
      public void TestInt16()
      {
         _performer.PerformTest(
            val => unchecked((Int16) val),
            UtilPackExtensions.WriteInt16LEToBytes,
            UtilPackExtensions.ReadInt16LEFromBytes,
            UtilPackExtensions.WriteInt16LEToBytesNoRef,
            UtilPackExtensions.ReadInt16LEFromBytesNoRef,
            UtilPackExtensions.WriteInt16BEToBytes,
            UtilPackExtensions.ReadInt16BEFromBytes,
            UtilPackExtensions.WriteInt16BEToBytesNoRef,
            UtilPackExtensions.ReadInt16BEFromBytesNoRef
            );
      }

      [TestMethod]
      public void TestUInt16()
      {
         _performer.PerformTest(
            val => unchecked((UInt16) val),
            UtilPackExtensions.WriteUInt16LEToBytes,
            UtilPackExtensions.ReadUInt16LEFromBytes,
            UtilPackExtensions.WriteUInt16LEToBytesNoRef,
            UtilPackExtensions.ReadUInt16LEFromBytesNoRef,
            UtilPackExtensions.WriteUInt16BEToBytes,
            UtilPackExtensions.ReadUInt16BEFromBytes,
            UtilPackExtensions.WriteUInt16BEToBytesNoRef,
            UtilPackExtensions.ReadUInt16BEFromBytesNoRef
            );
      }

      [TestMethod]
      public void TestInt32()
      {
         _performer.PerformTest(
            val => unchecked((Int32) val),
            UtilPackExtensions.WriteInt32LEToBytes,
            UtilPackExtensions.ReadInt32LEFromBytes,
            UtilPackExtensions.WriteInt32LEToBytesNoRef,
            UtilPackExtensions.ReadInt32LEFromBytesNoRef,
            UtilPackExtensions.WriteInt32BEToBytes,
            UtilPackExtensions.ReadInt32BEFromBytes,
            UtilPackExtensions.WriteInt32BEToBytesNoRef,
            UtilPackExtensions.ReadInt32BEFromBytesNoRef
            );
      }

      [TestMethod]
      public void TestUInt32()
      {
         _performer.PerformTest(
            val => unchecked((UInt32) val),
            UtilPackExtensions.WriteUInt32LEToBytes,
            UtilPackExtensions.ReadUInt32LEFromBytes,
            UtilPackExtensions.WriteUInt32LEToBytesNoRef,
            UtilPackExtensions.ReadUInt32LEFromBytesNoRef,
            UtilPackExtensions.WriteUInt32BEToBytes,
            UtilPackExtensions.ReadUInt32BEFromBytes,
            UtilPackExtensions.WriteUInt32BEToBytesNoRef,
            UtilPackExtensions.ReadUInt32BEFromBytesNoRef
            );
      }

      [TestMethod]
      public void TestInt64()
      {
         _performer.PerformTest(
            val => val,
            UtilPackExtensions.WriteInt64LEToBytes,
            UtilPackExtensions.ReadInt64LEFromBytes,
            UtilPackExtensions.WriteInt64LEToBytesNoRef,
            UtilPackExtensions.ReadInt64LEFromBytesNoRef,
            UtilPackExtensions.WriteInt64BEToBytes,
            UtilPackExtensions.ReadInt64BEFromBytes,
            UtilPackExtensions.WriteInt64BEToBytesNoRef,
            UtilPackExtensions.ReadInt64BEFromBytesNoRef
            );
      }

      [TestMethod]
      public void TestUInt64()
      {
         _performer.PerformTest(
            val => unchecked((UInt64) val),
            UtilPackExtensions.WriteUInt64LEToBytes,
            UtilPackExtensions.ReadUInt64LEFromBytes,
            UtilPackExtensions.WriteUInt64LEToBytesNoRef,
            UtilPackExtensions.ReadUInt64LEFromBytesNoRef,
            UtilPackExtensions.WriteUInt64BEToBytes,
            UtilPackExtensions.ReadUInt64BEFromBytes,
            UtilPackExtensions.WriteUInt64BEToBytesNoRef,
            UtilPackExtensions.ReadUInt64BEFromBytesNoRef
            );
      }

      [TestMethod]
      public void TestDouble()
      {
         _performer.PerformTest(
            val => BitConverter.Int64BitsToDouble( val ),
            UtilPackExtensions.WriteDoubleLEToBytes,
            UtilPackExtensions.ReadDoubleLEFromBytes,
            UtilPackExtensions.WriteDoubleLEToBytesNoRef,
            UtilPackExtensions.ReadDoubleLEFromBytesNoRef,
            UtilPackExtensions.WriteDoubleBEToBytes,
            UtilPackExtensions.ReadDoubleBEFromBytes,
            UtilPackExtensions.WriteDoubleBEToBytesNoRef,
            UtilPackExtensions.ReadDoubleBEFromBytesNoRef
            );
      }

      [TestMethod]
      public void TestSingle()
      {
         _performer.PerformTest(
            val => BitConverter.Int32BitsToSingle( unchecked((Int32) val) ),
            UtilPackExtensions.WriteSingleLEToBytes,
            UtilPackExtensions.ReadSingleLEFromBytes,
            UtilPackExtensions.WriteSingleLEToBytesNoRef,
            UtilPackExtensions.ReadSingleLEFromBytesNoRef,
            UtilPackExtensions.WriteSingleBEToBytes,
            UtilPackExtensions.ReadSingleBEFromBytes,
            UtilPackExtensions.WriteSingleBEToBytesNoRef,
            UtilPackExtensions.ReadSingleBEFromBytesNoRef
            );
      }
   }

   public delegate Byte[] WriteWithRef<T>( Byte[] array, ref Int32 index, T value );

   public delegate T ReadWithRef<T>( Byte[] array, ref Int32 index );

   public delegate Byte[] WriteWithoutRef<T>( Byte[] array, Int32 index, T value );

   public delegate T ReadWithoutRef<T>( Byte[] array, Int32 index );


   public sealed class ByteArraySerializationFuzzyTestPerformer
   {
      public ByteArraySerializationFuzzyTestPerformer(
         )
      {
         var array = new Byte[sizeof( Int64 )];

         RandomNumberGenerator.Fill( array );
         this.Value = BitConverter.ToInt64( array );
      }

      public Int64 Value { get; }

      public void PerformTest<T>(
         Func<Int64, T> convert,
         WriteWithRef<T> write,
         ReadWithRef<T> read,
         WriteWithoutRef<T> writeNoRef,
         ReadWithoutRef<T> readNoRef
         )
      {
         var val = convert( this.Value );
         PerformTestWithRef( val, write, read );
         PerformTestWithoutRef( val, writeNoRef, readNoRef );
      }

      private static void PerformTestWithRef<T>(
         T val,
         WriteWithRef<T> write,
         ReadWithRef<T> read
         )
      {
         var size = Marshal.SizeOf<T>();
         var array = new Byte[size];
         var idx = 0;
         write( array, ref idx, val );
         Assert.AreEqual( size, idx );
         idx = 0;
         var deserialized = read( array, ref idx );
         Assert.AreEqual( size, idx );
         Assert.AreEqual( deserialized, val, "Value was different for methods: " + write.Method + ", " + read.Method );
      }

      private static void PerformTestWithoutRef<T>(
         T val,
         WriteWithoutRef<T> write,
         ReadWithoutRef<T> read
         )
      {
         var size = Marshal.SizeOf<T>();
         var array = new Byte[size];
         var idx = 0;
         write( array, idx, val );
         var deserialized = read( array, idx );
         Assert.AreEqual( deserialized, val );
      }

   }
}

public static partial class E_UtilPack
{
   public static void PerformTest<T>(
      this ByteArraySerializationFuzzyTestPerformer performer,
      Func<Int64, T> convert,
      WriteWithRef<T> writeLE,
      ReadWithRef<T> readLE,
      WriteWithoutRef<T> writeNoRefLE,
      ReadWithoutRef<T> readNoRefLE,
      WriteWithRef<T> writeBE,
      ReadWithRef<T> readBE,
      WriteWithoutRef<T> writeNoRefBE,
      ReadWithoutRef<T> readNoRefBE
      )
   {
      performer.PerformTest( convert, writeLE, readLE, writeNoRefLE, readNoRefLE );
      performer.PerformTest( convert, writeBE, readBE, writeNoRefBE, readNoRefBE );
   }
}