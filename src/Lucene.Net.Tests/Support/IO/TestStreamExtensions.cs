// Some tests adapted from Apache Harmony:
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/io/DataInputStreamTest.java
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/io/DataOutputStreamTest.java

using J2N.IO;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

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
    public class TestStreamExtensions : LuceneTestCase
    {
        private Stream stream;

        private const string fileString = "Test_All_Tests\nTest_java_io_BufferedInputStream\nTest_java_io_BufferedOutputStream\nTest_java_io_ByteArrayInputStream\nTest_java_io_ByteArrayOutputStream\nTest_DataInputStream\n";

        [Test]
        // LUCENENET note: adapted from test_read$BII() for ByteBuffer-based Read extension method
        public void TestRead()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(fileString);
            stream.Write(bytes, 0, bytes.Length);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            OpenDataInputStream();
            var buffer = ByteBuffer.Allocate((int)stream.Length);
            stream.Read(buffer, 0);
            Assert.IsTrue(Encoding.UTF8.GetString(buffer.Array).Equals(fileString));
        }

        [Test]
        // LUCENENET note: adapted from test_writeCharsLjava_lang_String()
        public void TestWrite_CharArray()
        {
            stream.Write("Test String".ToCharArray());
            // stream.Dispose(); // LUCENENET - we will reuse stream
            OpenDataInputStream();
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
            OpenDataInputStream();
            var chars = stream.ReadChars(fileString.Length);
            Assert.IsTrue(new string(chars).Equals(fileString));
        }

        [Test]
        // LUCENENET note: adapted from test_writeIntI()
        public void TestWrite_Int32()
        {
            stream.Write(9087589);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            OpenDataInputStream();
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
            OpenDataInputStream();
            Assert.AreEqual(768347202, stream.ReadInt32(), "Incorrect int read");
        }

        [Test]
        // LUCENENET note: adapted from test_writeLongJ()
        public void TestWrite_Int64()
        {
            stream.Write(908755555456L);
            // stream.Dispose(); // LUCENENET - we will reuse stream
            OpenDataInputStream();
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
            OpenDataInputStream();
            Assert.AreEqual(9875645283333L, stream.ReadInt64(), "Incorrect long read");
        }

        private void OpenDataInputStream()
        {
            // LUCENENET specific - we'll just reuse the same stream, but set its position to 0 for reading
            stream.Position = 0;
        }

        public override void SetUp()
        {
            base.SetUp();
            stream = new MemoryStream();
        }
    }
}
