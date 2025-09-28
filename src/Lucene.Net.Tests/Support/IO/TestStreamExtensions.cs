// Some tests adapted from Apache Harmony:
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/io/DataInputStreamTest.java
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/io/DataOutputStreamTest.java

using J2N.IO;
using J2N.Text;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support.IO
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
    public class TestStreamExtensions : LuceneTestCase
    {
        private Stream stream;

        private static readonly string unihw = "\u0048\u0065\u006C\u006C\u006F\u0020\u0057\u006F\u0072\u006C\u0064";

        private const string fileString = "Test_All_Tests\nTest_java_io_BufferedInputStream\nTest_java_io_BufferedOutputStream\nTest_java_io_ByteArrayInputStream\nTest_java_io_ByteArrayOutputStream\nTest_DataInputStream\n";

        [Test]
        // LUCENENET note: adapted from test_read$BII() for ByteBuffer-based Read extension method
        public void TestRead()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(fileString);
            stream.Write(bytes, 0, bytes.Length);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            ResetStreamForReading();
            var buffer = ByteBuffer.Allocate((int)stream.Length);
            stream.Read(buffer, 0);
            Assert.IsTrue(Encoding.UTF8.GetString(buffer.Array).Equals(fileString));
        }

        [Test]
        // LUCENENET note: adapted from test_read$BII() for ByteBuffer-based Read extension method
        public void TestRead_Span_Int64()
        {
            FileInfo file = LuceneTestCase.CreateTempFile("TestStreamExtensions", ".tmp");
            using FileStream fileStream = file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            byte[] bytes = Encoding.UTF8.GetBytes(fileString);
            fileStream.Write(bytes, 0, bytes.Length);
            fileStream.Flush(flushToDisk: true);

            Span<byte> buffer = stackalloc byte[Encoding.UTF8.GetByteCount(fileString)];
            Assert.Greater(fileStream.Read(buffer, 0L), 0);
            Assert.IsTrue(Encoding.UTF8.GetString(buffer).Equals(fileString));
        }

        [Test]
        // LUCENENET note: adapted from test_read$BII() for ByteBuffer-based Read extension method
        public void TestRead_Span()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(fileString);
            stream.Write(bytes, 0, bytes.Length);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            ResetStreamForReading();

            Span<byte> buffer = stackalloc byte[Encoding.UTF8.GetByteCount(fileString)];
            Assert.Greater(stream.Read(buffer), 0);
            Assert.IsTrue(Encoding.UTF8.GetString(buffer).Equals(fileString));
        }

        [Test]
        public void TestWrite_Span()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(fileString);
            stream.Write(bytes.AsSpan()); // Method under test

            //stream.Write("Test String".ToCharArray());
            // stream.Dispose(); // LUCENENET - we will reuse stream
            ResetStreamForReading();

            Span<byte> buffer = stackalloc byte[Encoding.UTF8.GetByteCount(fileString)];
            Assert.Greater(stream.Read(buffer), 0);
            Assert.IsTrue(Encoding.UTF8.GetString(buffer).Equals(fileString));
        }

        [Test]
        // LUCENENET note: adapted from test_writeCharsLjava_lang_String()
        public void TestWrite_CharArray()
        {
            stream.Write("Test String".ToCharArray());
            // stream.Dispose(); // LUCENENET - we will reuse stream
            ResetStreamForReading();
            char[] chars = stream.ReadChars((int)stream.Length / 2); // LUCENENET note: we don't have/need a ReadChar method, so reusing ReadChars here
            Assert.AreEqual("Test String", new string(chars, 0, chars.Length), "Incorrect chars written");
        }

        [Test]
        // LUCENENET note: adapted from test_read$BII() for ReadChars extension method
        public void TestReadChars()
        {
            byte[] bytes = Encoding.Unicode.GetBytes(fileString); // NOTE: ReadChars reads UTF-16 chars
            stream.Write(bytes, 0, bytes.Length);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            ResetStreamForReading();
            var chars = stream.ReadChars(fileString.Length);
            Assert.IsTrue(new string(chars).Equals(fileString));
        }

        [Test]
        // LUCENENET note: adapted from test_writeIntI()
        public void TestWrite_Int32()
        {
            stream.Write(9087589);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            ResetStreamForReading();
            int c = stream.ReadInt32();
            // dis.close();
            Assert.AreEqual(9087589, c, "Incorrect int written");
        }

        [Test]
        // LUCENENET note: adapted from test_readInt()
        public void TestReadInt32()
        {
            stream.Write(768347202);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            ResetStreamForReading();
            Assert.AreEqual(768347202, stream.ReadInt32(), "Incorrect int read");
        }

        [Test]
        // LUCENENET note: adapted from test_writeLongJ()
        public void TestWrite_Int64()
        {
            stream.Write(908755555456L);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            ResetStreamForReading();
            long c = stream.ReadInt64();
            // dis.close();
            Assert.AreEqual(908755555456L, c, "Incorrect long written");
        }

        [Test]
        // LUCENENET note: adapted from test_readLong()
        public void TestReadInt64()
        {
            stream.Write(9875645283333L);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            ResetStreamForReading();
            Assert.AreEqual(9875645283333L, stream.ReadInt64(), "Incorrect long read");
        }

        // Additional async tests

        [Test]
        // LUCENENET note: adapted from test_writeInt()
        public async Task TestWriteInt32BigEndianAsync()
        {
            await stream.WriteInt32BigEndianAsync(9087589);
            // Reset the stream so we can read back
            ResetStreamForReading();
            int c = await stream.ReadInt32BigEndianAsync();
            Assert.AreEqual(9087589, c, "Incorrect int written (async)");
        }

        [Test]
        // LUCENENET note: adapted from test_writeLong()
        public async Task TestWriteInt64BigEndianAsync()
        {
            await stream.WriteInt64BigEndianAsync(908755555456L);
            // Reset the stream so we can read back
            ResetStreamForReading();
            long c = await stream.ReadInt64BigEndianAsync();
            Assert.AreEqual(908755555456L, c, "Incorrect long written (async)");
        }

        [Test]
        // LUCENENET note: adapted from test_writeUTF()
        public async Task TestWriteUTFAsync()
        {
            await stream.WriteUTFAsync(unihw);
            // Reset the stream so we can read back
            ResetStreamForReading();
            string result = await stream.ReadUTFAsync();
            Assert.AreEqual(unihw, result, "Incorrect string written (async)");
        }

        [Test]
        // LUCENENET note: adapted from test_readInt()
        public async Task TestReadInt32BigEndianAsync()
        {
            await stream.WriteInt32BigEndianAsync(768347202);
            // Reset the stream so we can read back
            ResetStreamForReading();
            int result = await stream.ReadInt32BigEndianAsync();
            Assert.AreEqual(768347202, result, "Incorrect int read (async)");
        }

        [Test]
        // LUCENENET note: adapted from test_readLong()
        public async Task TestReadInt64BigEndianAsync()
        {
            await stream.WriteInt64BigEndianAsync(9875645283333L);
            // Reset the stream so we can read back
            ResetStreamForReading();
            long result = await stream.ReadInt64BigEndianAsync();
            Assert.AreEqual(9875645283333L, result, "Incorrect long read (async)");
        }

        [Test]
        // LUCENENET note: adapted from test_readUTF()
        public async Task TestReadUTFAsync()
        {
            await stream.WriteUTFAsync(unihw);

            // Check that the length was written correctly (UTF length + 2 bytes for length header)
            long expectedStreamLength = CalculateExpectedUTFStreamLength(unihw);
            Assert.AreEqual(expectedStreamLength, stream.Length, "Failed to write string in UTF format");

            // Reset and read the string
            ResetStreamForReading();
            string result = await stream.ReadUTFAsync();
            Assert.AreEqual(unihw, result, "Incorrect string read (async)");
        }

        /// <summary>
        /// Helper method to calculate expected UTF stream length for validation
        /// Matches DataOutput.writeUTF() spec (Java)
        /// </summary>
        private long CalculateExpectedUTFStreamLength(string value)
        {
            long utfCount = 0;
            foreach (char ch in value)
            {
                if (ch > 0 && ch <= 127)
                    utfCount++;
                else if (ch <= 2047)
                    utfCount += 2;
                else
                    utfCount += 3;
            }
            return utfCount + 2; // +2 for the 2-byte length header
        }

        private void ResetStreamForReading() // LUCENENET - was "OpenDataInputStream" in Harmony tests
        {
            // LUCENENET specific - in the Harmony tests, there were separate streams
            // for input and output. Here, we'll just reuse the same stream, but reset
            // its position to 0 so it's ready to read.
            stream.Position = 0;
        }

        public override void SetUp()
        {
            base.SetUp();
            stream = new MemoryStream();
        }
    }
}
