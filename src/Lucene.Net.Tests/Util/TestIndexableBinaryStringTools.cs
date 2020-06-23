using NUnit.Framework;
using System;
using System.Globalization;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// @deprecated Remove when IndexableBinaryStringTools is removed.
    [Obsolete("Remove when IndexableBinaryStringTools is removed.")]
    [TestFixture]
    public class TestIndexableBinaryStringTools : LuceneTestCase
    {
        private static int NUM_RANDOM_TESTS;
        private static int MAX_RANDOM_BINARY_LENGTH;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            NUM_RANDOM_TESTS = AtLeast(200);
            MAX_RANDOM_BINARY_LENGTH = AtLeast(300);
        }

        [Test]
        public virtual void TestSingleBinaryRoundTrip()
        {
            sbyte[] binary = new sbyte[] { (sbyte)0x23, unchecked((sbyte)0x98), (sbyte)0x13, unchecked((sbyte)0xE4), (sbyte)0x76, (sbyte)0x41, unchecked((sbyte)0xB2), unchecked((sbyte)0xC9), (sbyte)0x7F, (sbyte)0x0A, unchecked((sbyte)0xA6), unchecked((sbyte)0xD8) };

            int encodedLen = IndexableBinaryStringTools.GetEncodedLength(binary, 0, binary.Length);
            char[] encoded = new char[encodedLen];
            IndexableBinaryStringTools.Encode(binary, 0, binary.Length, encoded, 0, encoded.Length);

            int decodedLen = IndexableBinaryStringTools.GetDecodedLength(encoded, 0, encoded.Length);
            sbyte[] decoded = new sbyte[decodedLen];
            IndexableBinaryStringTools.Decode(encoded, 0, encoded.Length, decoded, 0, decoded.Length);

            Assert.AreEqual(BinaryDump(binary, binary.Length), BinaryDump(decoded, decoded.Length), "Round trip decode/decode returned different results:\noriginal: " + BinaryDump(binary, binary.Length) + "\n encoded: " + CharArrayDump(encoded, encoded.Length) + "\n decoded: " + BinaryDump(decoded, decoded.Length));
        }

        [Test]
        public virtual void TestEncodedSortability()
        {
            sbyte[] originalArray1 = new sbyte[MAX_RANDOM_BINARY_LENGTH];
            char[] originalString1 = new char[MAX_RANDOM_BINARY_LENGTH];
            char[] encoded1 = new char[MAX_RANDOM_BINARY_LENGTH * 10];
            sbyte[] original2 = new sbyte[MAX_RANDOM_BINARY_LENGTH];
            char[] originalString2 = new char[MAX_RANDOM_BINARY_LENGTH];
            char[] encoded2 = new char[MAX_RANDOM_BINARY_LENGTH * 10];

            for (int testNum = 0; testNum < NUM_RANDOM_TESTS; ++testNum)
            {
                int numBytes1 = Random.Next(MAX_RANDOM_BINARY_LENGTH - 1) + 1; // Min == 1

                for (int byteNum = 0; byteNum < numBytes1; ++byteNum)
                {
                    int randomInt = Random.Next(0x100);
                    originalArray1[byteNum] = (sbyte)randomInt;
                    originalString1[byteNum] = (char)randomInt;
                }

                int numBytes2 = Random.Next(MAX_RANDOM_BINARY_LENGTH - 1) + 1; // Min == 1

                for (int byteNum = 0; byteNum < numBytes2; ++byteNum)
                {
                    int randomInt = Random.Next(0x100);
                    original2[byteNum] = (sbyte)randomInt;
                    originalString2[byteNum] = (char)randomInt;
                }
                int originalComparison = String.CompareOrdinal(new string(originalString1, 0, numBytes1),
                                                               new string(originalString2, 0, numBytes2));

                originalComparison = originalComparison < 0 ? -1 : originalComparison > 0 ? 1 : 0;

                int encodedLen1 = IndexableBinaryStringTools.GetEncodedLength(originalArray1, 0, numBytes1);
                if (encodedLen1 > encoded1.Length)
                {
                    encoded1 = new char[ArrayUtil.Oversize(encodedLen1, RamUsageEstimator.NUM_BYTES_CHAR)];
                }
                IndexableBinaryStringTools.Encode(originalArray1, 0, numBytes1, encoded1, 0, encodedLen1);

                int encodedLen2 = IndexableBinaryStringTools.GetEncodedLength(original2, 0, numBytes2);
                if (encodedLen2 > encoded2.Length)
                {
                    encoded2 = new char[ArrayUtil.Oversize(encodedLen2, RamUsageEstimator.NUM_BYTES_CHAR)];
                }
                IndexableBinaryStringTools.Encode(original2, 0, numBytes2, encoded2, 0, encodedLen2);

                int encodedComparison = String.CompareOrdinal(new string(encoded1, 0, encodedLen1),
                                                              new string(encoded2, 0, encodedLen2));

                encodedComparison = encodedComparison < 0 ? -1 : encodedComparison > 0 ? 1 : 0;

                Assert.AreEqual(originalComparison, encodedComparison, "Test #" + (testNum + 1) + ": Original bytes and encoded chars compare differently:" + " \nbinary 1: " + BinaryDump(originalArray1, numBytes1) + " \nbinary 2: " + BinaryDump(original2, numBytes2) + "\nencoded 1: " + CharArrayDump(encoded1, encodedLen1) + "\nencoded 2: " + CharArrayDump(encoded2, encodedLen2));
            }
        }

        [Test]
        public virtual void TestEmptyInput()
        {
            sbyte[] binary = new sbyte[0];

            int encodedLen = IndexableBinaryStringTools.GetEncodedLength(binary, 0, binary.Length);
            char[] encoded = new char[encodedLen];
            IndexableBinaryStringTools.Encode(binary, 0, binary.Length, encoded, 0, encoded.Length);

            int decodedLen = IndexableBinaryStringTools.GetDecodedLength(encoded, 0, encoded.Length);
            sbyte[] decoded = new sbyte[decodedLen];
            IndexableBinaryStringTools.Decode(encoded, 0, encoded.Length, decoded, 0, decoded.Length);

            Assert.AreEqual(decoded.Length, 0, "decoded empty input was not empty");
        }

        [Test]
        public virtual void TestAllNullInput()
        {
            sbyte[] binary = new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            int encodedLen = IndexableBinaryStringTools.GetEncodedLength(binary, 0, binary.Length);
            char[] encoded = new char[encodedLen];
            IndexableBinaryStringTools.Encode(binary, 0, binary.Length, encoded, 0, encoded.Length);

            int decodedLen = IndexableBinaryStringTools.GetDecodedLength(encoded, 0, encoded.Length);
            sbyte[] decoded = new sbyte[decodedLen];
            IndexableBinaryStringTools.Decode(encoded, 0, encoded.Length, decoded, 0, decoded.Length);

            Assert.AreEqual(BinaryDump(binary, binary.Length), BinaryDump(decoded, decoded.Length), "Round trip decode/decode returned different results:" + "\n  original: " + BinaryDump(binary, binary.Length) + "\ndecodedBuf: " + BinaryDump(decoded, decoded.Length));
        }

        [Test]
        public virtual void TestRandomBinaryRoundTrip()
        {
            sbyte[] binary = new sbyte[MAX_RANDOM_BINARY_LENGTH];
            char[] encoded = new char[MAX_RANDOM_BINARY_LENGTH * 10];
            sbyte[] decoded = new sbyte[MAX_RANDOM_BINARY_LENGTH];
            for (int testNum = 0; testNum < NUM_RANDOM_TESTS; ++testNum)
            {
                int numBytes = Random.Next(MAX_RANDOM_BINARY_LENGTH - 1) + 1; // Min == 1

                for (int byteNum = 0; byteNum < numBytes; ++byteNum)
                {
                    binary[byteNum] = (sbyte)Random.Next(0x100);
                }

                int encodedLen = IndexableBinaryStringTools.GetEncodedLength(binary, 0, numBytes);
                if (encoded.Length < encodedLen)
                {
                    encoded = new char[ArrayUtil.Oversize(encodedLen, RamUsageEstimator.NUM_BYTES_CHAR)];
                }
                IndexableBinaryStringTools.Encode(binary, 0, numBytes, encoded, 0, encodedLen);

                int decodedLen = IndexableBinaryStringTools.GetDecodedLength(encoded, 0, encodedLen);
                IndexableBinaryStringTools.Decode(encoded, 0, encodedLen, decoded, 0, decodedLen);

                Assert.AreEqual(BinaryDump(binary, numBytes), BinaryDump(decoded, decodedLen), "Test #" + (testNum + 1) + ": Round trip decode/decode returned different results:" + "\n  original: " + BinaryDump(binary, numBytes) + "\nencodedBuf: " + CharArrayDump(encoded, encodedLen) + "\ndecodedBuf: " + BinaryDump(decoded, decodedLen));
            }
        }

        public virtual string BinaryDump(sbyte[] binary, int numBytes)
        {
            StringBuilder buf = new StringBuilder();
            for (int byteNum = 0; byteNum < numBytes; ++byteNum)
            {
                string hex = (binary[byteNum] & 0xFF).ToString("x");
                if (hex.Length == 1)
                {
                    buf.Append('0');
                }
                buf.Append(CultureInfo.InvariantCulture.TextInfo.ToUpper(hex));
                if (byteNum < numBytes - 1)
                {
                    buf.Append(' ');
                }
            }
            return buf.ToString();
        }

        public virtual string CharArrayDump(char[] charArray, int numBytes)
        {
            StringBuilder buf = new StringBuilder();
            for (int charNum = 0; charNum < numBytes; ++charNum)
            {
                string hex = ((int)charArray[charNum]).ToString("x");
                for (int digit = 0; digit < 4 - hex.Length; ++digit)
                {
                    buf.Append('0');
                }
                buf.Append(CultureInfo.InvariantCulture.TextInfo.ToUpper(hex));
                if (charNum < numBytes - 1)
                {
                    buf.Append(' ');
                }
            }
            return buf.ToString();
        }
    }
}