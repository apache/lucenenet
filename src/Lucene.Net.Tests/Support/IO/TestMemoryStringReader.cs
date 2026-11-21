using Lucene.Net.Attributes;
using NUnit.Framework;
using System;

namespace Lucene.Net.IO
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
    public class MemoryStringReaderTests
    {
        [Test]
        public virtual void TestEmptyMemory()
        {
            using MemoryStringReader reader = new MemoryStringReader(ReadOnlyMemory<char>.Empty);

            Assert.AreEqual(-1, reader.Peek());
            Assert.AreEqual(-1, reader.Read());
            Assert.AreEqual(0, reader.Read(new char[4], 0, 4));
            Assert.AreEqual(string.Empty, reader.ReadToEnd());
        }

        [Test]
        public virtual void TestDefaultMemory()
        {
            using MemoryStringReader reader = new MemoryStringReader(default);

            Assert.AreEqual(-1, reader.Peek());
            Assert.AreEqual(-1, reader.Read());
            Assert.AreEqual(0, reader.Read(new char[4], 0, 4));
        }

        [Test]
        public virtual void TestPeekAndRead()
        {
            using MemoryStringReader reader = new MemoryStringReader("foobar".AsMemory());

            Assert.AreEqual('f', reader.Peek());
            Assert.AreEqual('f', reader.Peek());
            Assert.AreEqual('f', reader.Read());
            Assert.AreEqual('o', reader.Read());
            Assert.AreEqual('o', reader.Peek());
            Assert.AreEqual('o', reader.Read());
            Assert.AreEqual('b', reader.Read());
            Assert.AreEqual('a', reader.Read());
            Assert.AreEqual('r', reader.Read());
            Assert.AreEqual(-1, reader.Peek());
            Assert.AreEqual(-1, reader.Read());
        }

        [Test]
        public virtual void TestReadArray()
        {
            using MemoryStringReader reader = new MemoryStringReader("foobar".AsMemory());

            char[] buffer = new char[4];

            Assert.AreEqual(4, reader.Read(buffer, 0, buffer.Length));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.Read(buffer, 0, buffer.Length));
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            Assert.AreEqual(0, reader.Read(buffer, 0, buffer.Length));
        }

        [Test]
        public virtual void TestReadArrayWithOffset()
        {
            using MemoryStringReader reader = new MemoryStringReader("foobar".AsMemory());

            char[] buffer = new char[8];

            Assert.AreEqual(3, reader.Read(buffer, 2, 3));
            Assert.AreEqual("\0\0foo\0\0\0", new string(buffer));

            Assert.AreEqual(3, reader.Read(buffer, 1, 5));
            Assert.AreEqual("bar", new string(buffer, 1, 3));
        }

        [Test]
        public virtual void TestReadArrayZeroCount()
        {
            using MemoryStringReader reader = new MemoryStringReader("foo".AsMemory());

            char[] buffer = new char[4];

            Assert.AreEqual(0, reader.Read(buffer, 0, 0));
            Assert.AreEqual('f', reader.Read());

            Assert.AreEqual(0, reader.Read(buffer, 0, 0));
            Assert.AreEqual(2, reader.Read(buffer, 0, 4));
            Assert.AreEqual(0, reader.Read(buffer, 0, 0));
        }

        [Test]
        public virtual void TestReadArrayArgumentValidation()
        {
            using MemoryStringReader reader = new MemoryStringReader("foo".AsMemory());

            char[] buffer = new char[4];

            Assert.Throws<ArgumentNullException>(() => reader.Read(null, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(buffer, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(buffer, 0, -1));
            Assert.Throws<ArgumentException>(() => reader.Read(buffer, 3, 2));
        }

        [Test]
        public virtual void TestReadMemorySliceWithPartialReads()
        {
            char[] input = "0123456789".ToCharArray();
            using MemoryStringReader reader = new MemoryStringReader(
                new ReadOnlyMemory<char>(input, 2, 5));

            char[] buffer = new char[3];

            Assert.AreEqual(3, reader.Read(buffer, 0, 3));
            Assert.AreEqual("234", new string(buffer));

            Assert.AreEqual(2, reader.Read(buffer, 0, 3));
            Assert.AreEqual("56", new string(buffer, 0, 2));

            Assert.AreEqual(0, reader.Read(buffer, 0, 3));
        }

        [Test]
        public virtual void TestReadToEnd()
        {
            using MemoryStringReader reader = new MemoryStringReader("foobar".AsMemory());

            Assert.AreEqual("foobar", reader.ReadToEnd());
            Assert.AreEqual(string.Empty, reader.ReadToEnd());
            Assert.AreEqual(-1, reader.Read());
        }

        [Test]
        public virtual void TestReadToEndAfterPartialRead()
        {
            using MemoryStringReader reader = new MemoryStringReader("foobar".AsMemory());

            Assert.AreEqual('f', reader.Read());
            Assert.AreEqual("oobar", reader.ReadToEnd());
            Assert.AreEqual(string.Empty, reader.ReadToEnd());
        }

        [Test]
        public virtual void TestReadBlockArray()
        {
            using MemoryStringReader reader = new MemoryStringReader("foobar".AsMemory());

            char[] buffer = new char[4];

            Assert.AreEqual(4, reader.ReadBlock(buffer, 0, buffer.Length));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.ReadBlock(buffer, 0, buffer.Length));
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            Assert.AreEqual(0, reader.ReadBlock(buffer, 0, buffer.Length));
        }

        [Test]
        public virtual void TestReadLine()
        {
            using MemoryStringReader reader = new MemoryStringReader("first\r\nsecond\nthird".AsMemory());

            Assert.AreEqual("first", reader.ReadLine());
            Assert.AreEqual("second", reader.ReadLine());
            Assert.AreEqual("third", reader.ReadLine());
            Assert.IsNull(reader.ReadLine());
        }

        [Test]
        public virtual void TestDispose()
        {
            MemoryStringReader reader = new MemoryStringReader("foobar".AsMemory());

            reader.Dispose();

            Assert.Throws<ObjectDisposedException>(() => reader.Peek());
            Assert.Throws<ObjectDisposedException>(() => reader.Read());
            Assert.Throws<ObjectDisposedException>(() => reader.Read(new char[4], 0, 4));
            Assert.Throws<ObjectDisposedException>(() => reader.ReadBlock(new char[4], 0, 4));
            Assert.Throws<ObjectDisposedException>(() => reader.ReadToEnd());

            // Disposal is idempotent.
            Assert.DoesNotThrow(() => reader.Dispose());
        }

#if FEATURE_TEXTREADER_SPAN
        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadSpan()
        {
            using MemoryStringReader reader = new MemoryStringReader("foobar".AsMemory());

            Span<char> buffer = stackalloc char[4];

            Assert.AreEqual(4, reader.Read(buffer));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.Read(buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, reader.Read(buffer));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadBlockSpan()
        {
            using MemoryStringReader reader = new MemoryStringReader("foobar".AsMemory());

            Span<char> buffer = stackalloc char[4];

            Assert.AreEqual(4, reader.ReadBlock(buffer));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.ReadBlock(buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, reader.ReadBlock(buffer));
        }
#endif
    }
}
