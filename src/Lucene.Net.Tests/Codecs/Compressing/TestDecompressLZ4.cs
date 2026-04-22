using Lucene.Net.Attributes;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Codecs.Compressing
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
    public class TestDecompressLZ4 : LuceneTestCase
    {
        // LUCENENET specific: backported from upstream Lucene 10.4.0 (apache/lucene#15570)
        [Test]
        public void TestDecompressOffset0()
        {
            byte[] input = new byte[]
            {
                // token
                0xE,
                // offset 0 (invalid)
                0,
                0,
                // last literal
                // token
                7 << 4,
                // literal
                0,
                0,
                0,
                0,
                0,
                0,
                0
            };

            byte[] output = new byte[18];

            var e = Assert.Throws<IOException>(
                () => LZ4.Decompress(new ByteArrayDataInput(input), output.Length, output, 0));
            Assert.AreEqual("offset 0 is invalid", e.Message);
        }

        // LUCENENET specific - confirm the same fail-fast behavior surfaces through the
        // CompressionMode.FAST.NewDecompressor() integration path that callers actually use.
        [Test, LuceneNetSpecific]
        public void TestDecompressOffset0ThroughCompressionMode()
        {
            byte[] input = new byte[]
            {
                0xE,
                0,
                0,
                7 << 4,
                0,
                0,
                0,
                0,
                0,
                0,
                0
            };

            Decompressor decompressor = CompressionMode.FAST.NewDecompressor();
            BytesRef bytes = new BytesRef();

            var e = Assert.Throws<IOException>(
                () => decompressor.Decompress(new ByteArrayDataInput(input), 18, 0, 18, bytes));
            Assert.AreEqual("offset 0 is invalid", e.Message);
        }
    }
}
