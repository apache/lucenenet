using J2N.Text;
using Lucene.Net.Attributes;
using NUnit.Framework;
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

    [TestFixture]
    public class TestBytesRef : LuceneTestCase
    {
        [Test]
        public virtual void TestEmpty()
        {
            BytesRef b = new BytesRef();
            Assert.AreEqual(BytesRef.EMPTY_BYTES, b.Bytes);
            Assert.AreEqual(0, b.Offset);
            Assert.AreEqual(0, b.Length);
        }

        [Test]
        public virtual void TestFromBytes()
        {
            var bytes = new [] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes);
            Assert.AreEqual(bytes, b.Bytes);
            Assert.AreEqual(0, b.Offset);
            Assert.AreEqual(4, b.Length);

            BytesRef b2 = new BytesRef(bytes, 1, 3);
            Assert.AreEqual("bcd", b2.Utf8ToString());

            Assert.IsFalse(b.Equals(b2));
        }

        [Test]
        public virtual void TestFromChars()
        {
            for (int i = 0; i < 100; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                string s2 = (new BytesRef(s)).Utf8ToString();
                Assert.AreEqual(s, s2);
            }

            // only for 4.x
            Assert.AreEqual("\uFFFF", (new BytesRef("\uFFFF")).Utf8ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestFromCharSequence()
        {
            for (int i = 0; i < 100; i++)
            {
                ICharSequence s = new StringCharSequence(TestUtil.RandomUnicodeString(Random));
                ICharSequence s2 = (new BytesRef(s)).Utf8ToString().AsCharSequence();
                Assert.AreEqual(s, s2);
            }

            // only for 4.x
            Assert.AreEqual("\uFFFF", (new BytesRef("\uFFFF")).Utf8ToString());
        }

        // LUCENE-3590, AIOOBE if you append to a bytesref with offset != 0
        [Test]
        public virtual void TestAppend()
        {
            var bytes = new[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes, 1, 3); // bcd
            b.Append(new BytesRef("e"));
            Assert.AreEqual("bcde", b.Utf8ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a bytesref with offset != 0
        [Test]
        public virtual void TestCopyBytes()
        {
            var bytes = new[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes, 1, 3); // bcd
            b.CopyBytes(new BytesRef("bcde"));
            Assert.AreEqual("bcde", b.Utf8ToString());
        }

#if FEATURE_SERIALIZABLE

        [Test, LuceneNetSpecific]
        public void TestSerialization()
        {
            byte[] bytes = new byte[] { 44, 66, 77, 33, 99, 13, 74, 26 };

            var bytesRef = new BytesRef(bytes, 2, 4);

            Assert.AreEqual(4, bytesRef.Length);
            Assert.AreSame(bytes, bytesRef.Bytes);
            Assert.AreEqual(bytes, bytesRef.Bytes);
            Assert.AreEqual(2, bytesRef.Offset);

            var clone = Clone(bytesRef);

            Assert.AreEqual(4, clone.Length);
            Assert.AreNotSame(bytes, clone.Bytes);
            Assert.AreEqual(bytes, clone.Bytes);
            Assert.AreEqual(2, clone.Offset);
        }
#endif
    }
}