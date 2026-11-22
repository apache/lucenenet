using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
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

    [TestFixture]
    [LuceneNetSpecific]
    public class TestNumberFormatter
    {
        // The test values below were directly generated from Java using the following Lucene test:

        /*
        public void testGenerateBinaryFormatTestCases()
        {
            int[] edgeValues = {
                0,
                1,
                -1,
                2,
                -2,
                Integer.MIN_VALUE,
                Integer.MAX_VALUE,
                Integer.MIN_VALUE + 1,
                1024,
                1 << 30,
                0x55555555,
                0xAAAAAAAA
              };

            java.util.List<Integer> values = new java.util.ArrayList<>(edgeValues.length + 16);
            for (int v : edgeValues)
            {
                values.add(v);
            }

            int extraCount = random().nextInt(8) + 8; // e.g., 8 to 15 random values
            for (int i = 0; i < extraCount; i++)
            {
                values.add(random().nextInt());
            }

            StringBuilder sb = new StringBuilder();
            sb.append(System.lineSeparator());
            for (int value : values)
            {
                String binary = Integer.toBinaryString(value);
                String testName = "TryFormatBinary_" + value;
                sb.append("yield return new TestCaseData(")
                    .append(value)
                    .append(", \"")
                    .append(binary)
                    .append("\").SetName(\"")
                    .append(testName)
                    .append("\");")
                    .append(System.lineSeparator());
            }

            System.out.println(sb.toString());
        }
        */

        public static IEnumerable<TestCaseData> BinaryFormatCases()
        {
            yield return new TestCaseData(0, "0").SetName("TryFormatBinary_0");
            yield return new TestCaseData(1, "1").SetName("TryFormatBinary_1");
            yield return new TestCaseData(-1, "11111111111111111111111111111111").SetName("TryFormatBinary_-1");
            yield return new TestCaseData(2, "10").SetName("TryFormatBinary_2");
            yield return new TestCaseData(-2, "11111111111111111111111111111110").SetName("TryFormatBinary_-2");
            yield return new TestCaseData(-2147483648, "10000000000000000000000000000000").SetName("TryFormatBinary_-2147483648");
            yield return new TestCaseData(2147483647, "1111111111111111111111111111111").SetName("TryFormatBinary_2147483647");
            yield return new TestCaseData(-2147483647, "10000000000000000000000000000001").SetName("TryFormatBinary_-2147483647");
            yield return new TestCaseData(1024, "10000000000").SetName("TryFormatBinary_1024");
            yield return new TestCaseData(1073741824, "1000000000000000000000000000000").SetName("TryFormatBinary_1073741824");
            yield return new TestCaseData(1431655765, "1010101010101010101010101010101").SetName("TryFormatBinary_1431655765");
            yield return new TestCaseData(-1431655766, "10101010101010101010101010101010").SetName("TryFormatBinary_-1431655766");
            yield return new TestCaseData(-1485190184, "10100111011110011100101111011000").SetName("TryFormatBinary_-1485190184");
            yield return new TestCaseData(1748212933, "1101000001100111001110011000101").SetName("TryFormatBinary_1748212933");
            yield return new TestCaseData(580693292, "100010100111001010110100101100").SetName("TryFormatBinary_580693292");
            yield return new TestCaseData(-42962626, "11111101011100000111000100111110").SetName("TryFormatBinary_-42962626");
            yield return new TestCaseData(-1518818317, "10100101011110001010101111110011").SetName("TryFormatBinary_-1518818317");
            yield return new TestCaseData(970095694, "111001110100100111110001001110").SetName("TryFormatBinary_970095694");
            yield return new TestCaseData(1741670988, "1100111110011111100101001001100").SetName("TryFormatBinary_1741670988");
            yield return new TestCaseData(-1810768333, "10010100000100011101111000110011").SetName("TryFormatBinary_-1810768333");
            yield return new TestCaseData(2039777444, "1111001100101001000100010100100").SetName("TryFormatBinary_2039777444");
            yield return new TestCaseData(138928837, "1000010001111110001011000101").SetName("TryFormatBinary_138928837");
        }

        [Test]
        [TestCaseSource(nameof(BinaryFormatCases))]
        public void TestTryFormatBinary_ExactBuffer(int value, string expected)
        {
            char[] buffer = new char[expected.Length];

            bool success = NumberFormatter.TryFormatBinary(value, buffer, out int charsWritten);

            Assert.IsTrue(success);
            Assert.AreEqual(expected.Length, charsWritten);
            Assert.AreEqual(expected, new string(buffer, 0, charsWritten));
        }

        [Test]
        [TestCaseSource(nameof(BinaryFormatCases))]
        public void TestTryFormatBinary_OversizedBufferDoesNotOverwritePastEnd(int value, string expected)
        {
            const char sentinel = '?';
            char[] buffer = new char[32 + 4];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = sentinel;
            }

            bool success = NumberFormatter.TryFormatBinary(value, buffer.AsSpan(0, 32), out int charsWritten);

            Assert.IsTrue(success);
            Assert.AreEqual(expected.Length, charsWritten);
            Assert.AreEqual(expected, new string(buffer, 0, charsWritten));

            for (int i = charsWritten; i < buffer.Length; i++)
            {
                Assert.AreEqual(sentinel, buffer[i], $"Unexpected write at index {i}");
            }
        }

        [Test]
        [TestCaseSource(nameof(BinaryFormatCases))]
        public void TestTryFormatBinary_TooSmallBufferFails(int value, string expected)
        {
            char[] buffer = new char[Math.Max(0, expected.Length - 1)];

            bool success = NumberFormatter.TryFormatBinary(value, buffer, out int charsWritten);

            Assert.IsFalse(success);
            Assert.AreEqual(0, charsWritten);
        }
    }
}
