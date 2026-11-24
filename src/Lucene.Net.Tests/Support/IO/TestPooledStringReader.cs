using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

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
    public class TestPooledStringReader : LuceneTestCase
    {
        [Test]
        public virtual void TestInitialState()
        {
            using PooledStringReader reader = new PooledStringReader(ReadOnlySpan<char>.Empty);

            Assert.AreEqual(-1, reader.Peek());
            Assert.AreEqual(-1, reader.Read());
            Assert.AreEqual(0, reader.Read(new char[1], 0, 1));
            Assert.AreEqual(0, reader.Read(new char[2], 1, 1));
            Assert.AreEqual(string.Empty, reader.ReadToEnd());
        }

        [Test]
        public virtual void TestPeekAndRead()
        {
            using PooledStringReader reader = new PooledStringReader("abc".AsSpan());

            Assert.AreEqual('a', reader.Peek());
            Assert.AreEqual('a', reader.Peek());
            Assert.AreEqual('a', reader.Read());
            Assert.AreEqual('b', reader.Peek());
            Assert.AreEqual('b', reader.Read());
            Assert.AreEqual('c', reader.Read());
            Assert.AreEqual(-1, reader.Peek());
            Assert.AreEqual(-1, reader.Read());
        }

        [Test]
        public virtual void TestReadArray()
        {
            char[] buffer = new char[4];

            using PooledStringReader reader = new PooledStringReader("foobar".AsSpan());

            Assert.AreEqual(4, reader.Read(buffer, 0, 4));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.Read(buffer, 0, 2));
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            Assert.AreEqual(0, reader.Read(buffer, 2, 0));
            Assert.AreEqual(0, reader.Read(buffer, 0, 1));
        }

        [Test]
        public virtual void TestReadArrayWithOffset()
        {
            char[] buffer = new char[4];

            using PooledStringReader reader = new PooledStringReader("foobar".AsSpan());

            Assert.AreEqual(0, reader.Read(buffer, 1, 0));
            Assert.AreEqual(3, reader.Read(buffer, 1, 3));
            Assert.AreEqual("foo", new string(buffer, 1, 3));

            Assert.AreEqual(2, reader.Read(buffer, 2, 2));
            Assert.AreEqual("ba", new string(buffer, 2, 2));

            Assert.AreEqual('r', (char)reader.Read());
            Assert.AreEqual(0, reader.Read(buffer, 2, 0));
        }

        [Test]
        public virtual void TestReadArrayArgumentValidation()
        {
            using PooledStringReader reader = new PooledStringReader("abc".AsSpan());

            Assert.Throws<ArgumentNullException>(() => reader.Read(null, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(new char[1], -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(new char[1], 0, -1));
            Assert.Throws<ArgumentException>(() => reader.Read(new char[1], 1, 1));
            Assert.Throws<ArgumentException>(() => reader.Read(new char[1], 2, 0));
        }

        [Test]
        public virtual void TestInputIsCopied()
        {
            char[] input = "foobar".ToCharArray();

            using PooledStringReader reader = new PooledStringReader(input.AsSpan());

            input[0] = 'X';

            Assert.AreEqual("foobar", reader.ReadToEnd());
        }

        [Test]
        public virtual void TestReadToEnd()
        {
            using PooledStringReader reader = new PooledStringReader("foobar".AsSpan());

            Assert.AreEqual("foobar", reader.ReadToEnd());
            Assert.AreEqual(string.Empty, reader.ReadToEnd());
            Assert.AreEqual(-1, reader.Read());
        }

        [Test]
        public virtual void TestReadToEndAfterPartialRead()
        {
            using PooledStringReader reader = new PooledStringReader("foobar".AsSpan());

            Assert.AreEqual('f', reader.Read());
            Assert.AreEqual("oobar", reader.ReadToEnd());
            Assert.AreEqual(string.Empty, reader.ReadToEnd());
        }

        [Test]
        public virtual void TestReadBlock()
        {
            char[] buffer = new char[4];

            using PooledStringReader reader = new PooledStringReader("foobar".AsSpan());

            Assert.AreEqual(4, reader.ReadBlock(buffer, 0, 4));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.ReadBlock(buffer, 0, 4));
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            Assert.AreEqual(0, reader.ReadBlock(buffer, 0, 4));
            Assert.AreEqual(0, reader.ReadBlock(buffer, 0, 0));
        }

        [Test]
        public virtual void TestReadLine()
        {
            using PooledStringReader reader = new PooledStringReader("first\r\nsecond\nthird".AsSpan());

            Assert.AreEqual("first", reader.ReadLine());
            Assert.AreEqual("second", reader.ReadLine());
            Assert.AreEqual("third", reader.ReadLine());
            Assert.IsNull(reader.ReadLine());
        }

#if FEATURE_TEXTREADER_SPAN
        [Test]
        public virtual void TestReadSpan()
        {
            Span<char> buffer = stackalloc char[4];

            using PooledStringReader reader = new PooledStringReader("foobar".AsSpan());

            Assert.AreEqual(4, reader.Read(buffer));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.Read(buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, reader.Read(buffer));
        }

        [Test]
        public virtual void TestReadSpanWithEmptyBuffer()
        {
            using PooledStringReader reader = new PooledStringReader("abc".AsSpan());

            Assert.AreEqual(0, reader.Read(Span<char>.Empty));
            Assert.AreEqual('a', reader.Read());
        }

        [Test]
        public virtual void TestReadBlockSpan()
        {
            Span<char> buffer = stackalloc char[4];

            using PooledStringReader reader = new PooledStringReader("foobar".AsSpan());

            Assert.AreEqual(4, reader.ReadBlock(buffer));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.ReadBlock(buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, reader.ReadBlock(buffer));
        }
#endif

        [Test]
        public virtual void TestReadAllCharacters()
        {
            using PooledStringReader reader = new PooledStringReader("foobar".AsSpan());

            StringBuilder builder = new StringBuilder();
            int ch;

            while ((ch = reader.Read()) != -1)
            {
                builder.Append((char)ch);
            }

            Assert.AreEqual("foobar", builder.ToString());
        }

        [Test]
        public virtual void TestDispose()
        {
            PooledStringReader reader = new PooledStringReader("foobar".AsSpan());

            reader.Dispose();

            // Disposal is safe to repeat. The reader is not reusable, so no
            // further reads are attempted after this point.
            Assert.DoesNotThrow(() => reader.Dispose());
        }

        [Test]
        public virtual void TestOperationsAfterDisposeThrow()
        {
            PooledStringReader reader = new PooledStringReader("foobar".AsSpan());
            char[] buffer = new char[4];

            reader.Dispose();

            Assert.Throws<ObjectDisposedException>(() => reader.Peek());
            Assert.Throws<ObjectDisposedException>(() => reader.Read());
            Assert.Throws<ObjectDisposedException>(() => reader.Read(buffer, 0, buffer.Length));
            Assert.Throws<ObjectDisposedException>(() => reader.ReadBlock(buffer, 0, buffer.Length));
            Assert.Throws<ObjectDisposedException>(() => reader.ReadToEnd());
            Assert.Throws<ObjectDisposedException>(() => reader.ReadLine());

            // Disposal takes precedence over argument validation.
            Assert.Throws<ObjectDisposedException>(() => reader.Read(null, -1, -1));

#if FEATURE_TEXTREADER_SPAN
            AssertDisposedSpanReadThrows(reader);
            AssertDisposedSpanReadBlockThrows(reader);
#endif

            // Disposal is idempotent.
            Assert.DoesNotThrow(() => reader.Dispose());
        }

#if FEATURE_TEXTREADER_SPAN
        private static void AssertDisposedSpanReadThrows(PooledStringReader reader)
        {
            Assert.Throws<ObjectDisposedException>(() => reader.Read(stackalloc char[4]));
        }

        private static void AssertDisposedSpanReadBlockThrows(PooledStringReader reader)
        {
            Assert.Throws<ObjectDisposedException>(() => reader.ReadBlock(stackalloc char[4]));
        }
#endif
    }
}
