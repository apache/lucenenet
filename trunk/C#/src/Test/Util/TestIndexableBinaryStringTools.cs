/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

namespace Lucene.Net.Util
{
	
    [TestFixture]
	public class TestIndexableBinaryStringTools:LuceneTestCase
	{
		private const int NUM_RANDOM_TESTS = 20000;
		private const int MAX_RANDOM_BINARY_LENGTH = 300;
		
        [Test]
		public virtual void  TestSingleBinaryRoundTrip()
		{
            System.Diagnostics.Debug.Fail("Port issue:", "byteBuf = ByteBuffer.wrap(newBuffer)"); // {{Aroush-2.9}}

			//byte[] binary = new byte[]{(byte) 0x23, (byte) SupportClass.Identity(0x98), (byte) 0x13, (byte) SupportClass.Identity(0xE4), (byte) 0x76, (byte) 0x41, (byte) SupportClass.Identity(0xB2), (byte) SupportClass.Identity(0xC9), (byte) 0x7F, (byte) 0x0A, (byte) SupportClass.Identity(0xA6), (byte) SupportClass.Identity(0xD8)};
			//
			//System.IO.MemoryStream binaryBuf = ByteBuffer.wrap(binary);
			//System.IO.MemoryStream encoded = IndexableBinaryStringTools.Encode(binaryBuf);
			//ByteBuffer decoded = IndexableBinaryStringTools.Decode(encoded);
            //Assert.AreEqual(binaryBuf, decoded, "Round trip decode/decode returned different results:" + System.Environment.NewLine + "original: " + BinaryDump(binaryBuf) + System.Environment.NewLine + " encoded: " + CharArrayDump(encoded) + System.Environment.NewLine + " decoded: " + BinaryDump(decoded));
		}
		
        [Test]
		public virtual void  TestEncodedSortability()
		{
			System.Random random = NewRandom();
			byte[] originalArray1 = new byte[MAX_RANDOM_BINARY_LENGTH];
            System.IO.MemoryStream originalBuf1 = new System.IO.MemoryStream(originalArray1);
			byte[] originalString1 = new byte[MAX_RANDOM_BINARY_LENGTH];    // {{Aroush-2.9}} this is char[] in Java
			System.IO.MemoryStream originalStringBuf1 = new System.IO.MemoryStream(originalString1);
			byte[] encoded1 = new byte[IndexableBinaryStringTools.GetEncodedLength(originalBuf1)];  // {{Aroush-2.9}} this is char[] in Java
			System.IO.MemoryStream encodedBuf1 = new System.IO.MemoryStream(encoded1);
			byte[] original2 = new byte[MAX_RANDOM_BINARY_LENGTH];
			System.IO.MemoryStream originalBuf2 = new System.IO.MemoryStream(original2);
			byte[] originalString2 = new byte[MAX_RANDOM_BINARY_LENGTH];    // {{Aroush-2.9}} this is char[] in Java
			System.IO.MemoryStream originalStringBuf2 = new System.IO.MemoryStream(originalString2);
			byte[] encoded2 = new byte[IndexableBinaryStringTools.GetEncodedLength(originalBuf2)];  // {{Aroush-2.9}} this is char[] in Java
			System.IO.MemoryStream encodedBuf2 = new System.IO.MemoryStream(encoded2);
			for (int testNum = 0; testNum < NUM_RANDOM_TESTS; ++testNum)
			{
				int numBytes1 = random.Next(MAX_RANDOM_BINARY_LENGTH - 1) + 1; // Min == 1
				originalBuf1.Capacity = numBytes1;
				originalStringBuf1.Capacity = numBytes1;
				
				for (int byteNum = 0; byteNum < numBytes1; ++byteNum)
				{
					int randomInt = random.Next(0x100);
					originalArray1[byteNum] = (byte) randomInt;
					originalString1[byteNum] = (byte) randomInt;
				}
				
				int numBytes2 = random.Next(MAX_RANDOM_BINARY_LENGTH - 1) + 1; // Min == 1
				originalBuf2.Capacity = numBytes2;
				originalStringBuf2.Capacity = numBytes2;
				for (int byteNum = 0; byteNum < numBytes2; ++byteNum)
				{
					int randomInt = random.Next(0x100);
					original2[byteNum] = (byte) randomInt;
					originalString2[byteNum] = (byte) randomInt;
				}
                System.Diagnostics.Debug.Fail("Port issue:", "int originalComparison = originalStringBuf1.CompareTo(originalStringBuf2);"); // {{Aroush-2.9}}
                int originalComparison = 0;
				//int originalComparison = originalStringBuf1.CompareTo(originalStringBuf2);
				//originalComparison = originalComparison < 0?- 1:(originalComparison > 0?1:0);
				
				IndexableBinaryStringTools.Encode(originalBuf1, encodedBuf1);
				IndexableBinaryStringTools.Encode(originalBuf2, encodedBuf2);

                System.Diagnostics.Debug.Fail("Port issue:", "int encodedComparison = encodedBuf1.CompareTo(encodedBuf2);"); // {{Aroush-2.9}}
                int encodedComparison = 0;
				//int encodedComparison = encodedBuf1.CompareTo(encodedBuf2);
				//encodedComparison = encodedComparison < 0?- 1:(encodedComparison > 0?1:0);
				
				Assert.AreEqual(originalComparison, encodedComparison, "Test #" + (testNum + 1) + ": Original bytes and encoded chars compare differently:" + System.Environment.NewLine + " binary 1: " + BinaryDump(originalBuf1) + System.Environment.NewLine + " binary 2: " + BinaryDump(originalBuf2) + System.Environment.NewLine + "encoded 1: " + CharArrayDump(encodedBuf1) + System.Environment.NewLine + "encoded 2: " + CharArrayDump(encodedBuf2) + System.Environment.NewLine);
			}
		}
		
        [Test]
		public virtual void  TestEmptyInput()
		{
			byte[] binary = new byte[0];
            System.IO.MemoryStream encoded = IndexableBinaryStringTools.Encode((new System.IO.MemoryStream(binary)));
			System.IO.MemoryStream decoded = IndexableBinaryStringTools.Decode(encoded);
			Assert.IsNotNull(decoded, "decode() returned null");
			Assert.AreEqual(decoded.Capacity, 0, "decoded empty input was not empty");
		}
		
        [Test]
		public virtual void  TestAllNullInput()
		{
			byte[] binary = new byte[]{0, 0, 0, 0, 0, 0, 0, 0, 0};
			System.IO.MemoryStream binaryBuf = new System.IO.MemoryStream(binary);
			System.IO.MemoryStream encoded = IndexableBinaryStringTools.Encode(binaryBuf);
			Assert.IsNotNull(encoded, "encode() returned null");
			System.IO.MemoryStream decodedBuf = IndexableBinaryStringTools.Decode(encoded);
			Assert.IsNotNull(decodedBuf, "decode() returned null");
			Assert.AreEqual(binaryBuf, decodedBuf, "Round trip decode/decode returned different results:" + System.Environment.NewLine + "  original: " + BinaryDump(binaryBuf) + System.Environment.NewLine + "decodedBuf: " + BinaryDump(decodedBuf));
		}
		
        [Test]
		public virtual void  TestRandomBinaryRoundTrip()
		{
			System.Random random = NewRandom();
			byte[] binary = new byte[MAX_RANDOM_BINARY_LENGTH];
			System.IO.MemoryStream binaryBuf = new System.IO.MemoryStream(binary);
			byte[] encoded = new byte[IndexableBinaryStringTools.GetEncodedLength(binaryBuf)];  // {{Aroush-2.9}} this is char[] in Java
			System.IO.MemoryStream encodedBuf = new System.IO.MemoryStream(encoded);
			byte[] decoded = new byte[MAX_RANDOM_BINARY_LENGTH];
			System.IO.MemoryStream decodedBuf = new System.IO.MemoryStream(decoded);
			for (int testNum = 0; testNum < NUM_RANDOM_TESTS; ++testNum)
			{
				int numBytes = random.Next(MAX_RANDOM_BINARY_LENGTH - 1) + 1; // Min == 1
				binaryBuf.Capacity = numBytes;
				for (int byteNum = 0; byteNum < numBytes; ++byteNum)
				{
					binary[byteNum] = (byte) random.Next(0x100);
				}
				IndexableBinaryStringTools.Encode(binaryBuf, encodedBuf);
				IndexableBinaryStringTools.Decode(encodedBuf, decodedBuf);
				Assert.AreEqual(binaryBuf, decodedBuf, "Test #" + (testNum + 1) + ": Round trip decode/decode returned different results:" + System.Environment.NewLine + "  original: " + BinaryDump(binaryBuf) + System.Environment.NewLine + "encodedBuf: " + CharArrayDump(encodedBuf) + System.Environment.NewLine + "decodedBuf: " + BinaryDump(decodedBuf));
			}
		}
		
		public virtual System.String BinaryDump(System.IO.MemoryStream binaryBuf)
		{
			System.Text.StringBuilder buf = new System.Text.StringBuilder();
			long numBytes = binaryBuf.Capacity - binaryBuf.Position;
			byte[] binary = binaryBuf.ToArray();
			for (int byteNum = 0; byteNum < numBytes; ++byteNum)
			{
				System.String hex = System.Convert.ToString((int) binary[byteNum] & 0xFF, 16);
				if (hex.Length == 1)
				{
					buf.Append('0');
				}
				buf.Append(hex.ToUpper());
				if (byteNum < numBytes - 1)
				{
					buf.Append(' ');
				}
			}
			return buf.ToString();
		}
		
		public virtual System.String CharArrayDump(System.IO.MemoryStream charBuf)
		{
			System.Text.StringBuilder buf = new System.Text.StringBuilder();
			long numBytes = charBuf.Capacity - charBuf.Position;
			byte[] charArray = charBuf.GetBuffer(); // {{Aroush-2.9}} this is char[] in Java
			for (int charNum = 0; charNum < numBytes; ++charNum)
			{
				System.String hex = System.Convert.ToString((int) charArray[charNum], 16);
				for (int digit = 0; digit < 4 - hex.Length; ++digit)
				{
					buf.Append('0');
				}
				buf.Append(hex.ToUpper());
				if (charNum < numBytes - 1)
				{
					buf.Append(' ');
				}
			}
			return buf.ToString();
		}
	}
}