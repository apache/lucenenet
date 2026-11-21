// Lucene version compatibility level 4.8.1
using Lucene.Net.Attributes;
using Lucene.Net.Text;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis
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
    public class TestReusableStringReader : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            char[] buf = new char[4];

            ReusableStringReader reader = new ReusableStringReader();
            Assert.AreEqual(-1, reader.Read());
            Assert.AreEqual(-1, reader.Read(new char[1], 0, 1));
            Assert.AreEqual(-1, reader.Read(new char[2], 1, 1));
            //Assert.AreEqual(-1, reader.Read(CharBuffer.wrap(new char[2]))); // LUCENENET specific - CharBuffer is not used in .NET

            reader.SetValue("foobar");
            Assert.AreEqual(4, reader.Read(buf, 0, 4));
            Assert.AreEqual("foob", new string(buf));
            Assert.AreEqual(2, reader.Read(buf, 0, 2));
            Assert.AreEqual("ar", new string(buf, 0, 2));
            Assert.AreEqual(-1, reader.Read(buf, 2, 0));
            reader.Dispose();

            reader.SetValue("foobar");
            Assert.AreEqual(0, reader.Read(buf, 1, 0));
            Assert.AreEqual(3, reader.Read(buf, 1, 3));
            Assert.AreEqual("foo", new string(buf, 1, 3));
            Assert.AreEqual(2, reader.Read(buf, 2, 2));
            Assert.AreEqual("ba", new string(buf, 2, 2));
            Assert.AreEqual('r', (char)reader.Read());
            Assert.AreEqual(-1, reader.Read(buf, 2, 0));
            reader.Dispose();

            reader.SetValue("foobar");
            using ValueStringBuilder sb = new(stackalloc char[8]);
            int ch;
            while ((ch = reader.Read()) != -1)
            {
                sb.Append((char)ch);
            }
            Assert.AreEqual("foobar", sb.ToString());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestInitialState()
        {
            ReusableStringReader reader = new ReusableStringReader();

            Assert.AreEqual(-1, reader.Peek());
            Assert.AreEqual(-1, reader.Read());
            Assert.AreEqual(-1, reader.Read(new char[1], 0, 1));
            Assert.AreEqual(-1, reader.Read(new char[2], 1, 1));
            Assert.AreEqual(string.Empty, reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestPeekAndRead()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("abc");

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
        [LuceneNetSpecific]
        public virtual void TestReadArray()
        {
            char[] buffer = new char[4];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual(4, reader.Read(buffer, 0, 4));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.Read(buffer, 0, 2));
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            Assert.AreEqual(-1, reader.Read(buffer, 2, 0));
            Assert.AreEqual(-1, reader.Read(buffer, 0, 1));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadArrayWithOffset()
        {
            char[] buffer = new char[4];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual(0, reader.Read(buffer, 1, 0));
            Assert.AreEqual(3, reader.Read(buffer, 1, 3));
            Assert.AreEqual("foo", new string(buffer, 1, 3));

            Assert.AreEqual(2, reader.Read(buffer, 2, 2));
            Assert.AreEqual("ba", new string(buffer, 2, 2));

            Assert.AreEqual('r', (char)reader.Read());
            Assert.AreEqual(-1, reader.Read(buffer, 2, 0));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadArrayArgumentValidation()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("abc");

            Assert.Throws<ArgumentNullException>(() => reader.Read(null!, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(new char[1], -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Read(new char[1], 0, -1));
            Assert.Throws<ArgumentException>(() => reader.Read(new char[1], 1, 1));
            Assert.Throws<ArgumentException>(() => reader.Read(new char[1], 2, 0));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestSetValueString()
        {
            ReusableStringReader reader = new ReusableStringReader();

            reader.SetValue("foobar");
            Assert.AreEqual('f', reader.Read());

            reader.SetValue("abc");
            Assert.AreEqual('a', reader.Read());
            Assert.AreEqual("bc", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestSetValueMemory()
        {
            char[] input = "foobar".ToCharArray();

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue(new ReadOnlyMemory<char>(input));

            Assert.AreEqual("foobar", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestInputIsNotCopied()
        {
            char[] input = "foobar".ToCharArray();

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue(new ReadOnlyMemory<char>(input));

            // The reader retains the supplied memory rather than copying it.
            input[0] = 'X';

            Assert.AreEqual("Xoobar", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestMemorySlice()
        {
            char[] input = "0123456789".ToCharArray();

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue(new ReadOnlyMemory<char>(input, 2, 5));

            Assert.AreEqual("23456", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadToEnd()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual("foobar", reader.ReadToEnd());
            Assert.AreEqual(string.Empty, reader.ReadToEnd());
            Assert.AreEqual(-1, reader.Read());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadToEndAfterPartialRead()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual('f', reader.Read());
            Assert.AreEqual("oobar", reader.ReadToEnd());
            Assert.AreEqual(string.Empty, reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadBlock()
        {
            char[] buffer = new char[4];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual(4, reader.ReadBlock(buffer, 0, 4));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.ReadBlock(buffer, 0, 4));
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            Assert.AreEqual(0, reader.ReadBlock(buffer, 0, 4));
            Assert.AreEqual(0, reader.ReadBlock(buffer, 0, 0));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadLine()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("first\r\nsecond\nthird");

            Assert.AreEqual("first", reader.ReadLine());
            Assert.AreEqual("second", reader.ReadLine());
            Assert.AreEqual("third", reader.ReadLine());
            Assert.IsNull(reader.ReadLine());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestDisposeAndReuse()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual('f', reader.Read());

            reader.Dispose();

            // Dispose releases the previous input reference, but does not
            // permanently close this reusable reader.
            Assert.AreEqual(-1, reader.Read());
            Assert.AreEqual(string.Empty, reader.ReadToEnd());

            reader.SetValue("abc");

            Assert.AreEqual("abc", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadAllCharacters()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int ch;

            while ((ch = reader.Read()) != -1)
            {
                builder.Append((char)ch);
            }

            Assert.AreEqual("foobar", builder.ToString());
        }

#if FEATURE_TEXTREADER_SPAN
        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadSpan()
        {
            Span<char> buffer = stackalloc char[4];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual(4, reader.Read(buffer));
            Assert.AreEqual("foob", buffer.ToString());

            Assert.AreEqual(2, reader.Read(buffer));
            Assert.AreEqual("ar", buffer.Slice(0, 2).ToString());

            Assert.AreEqual(0, reader.Read(buffer));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadSpanWithEmptyBuffer()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("abc");

            Assert.AreEqual(0, reader.Read(Span<char>.Empty));
            Assert.AreEqual('a', reader.Read());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadBlockSpan()
        {
            Span<char> buffer = stackalloc char[4];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual(4, reader.ReadBlock(buffer));
            Assert.AreEqual("foob", buffer.ToString());

            Assert.AreEqual(2, reader.ReadBlock(buffer));
            Assert.AreEqual("ar", buffer.Slice(0, 2).ToString());

            Assert.AreEqual(0, reader.ReadBlock(buffer));
        }
#endif

        [Test]
        [LuceneNetSpecific]
        public virtual async Task TestReadAsync()
        {
            char[] buffer = new char[4];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual(4, await reader.ReadAsync(buffer, 0, 4));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, await reader.ReadAsync(buffer, 0, 4));
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            // Async APIs use standard .NET EOF semantics.
            Assert.AreEqual(0, await reader.ReadAsync(buffer, 0, 4));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual async Task TestReadBlockAsync()
        {
            char[] buffer = new char[4];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual(4, await reader.ReadBlockAsync(buffer, 0, 4));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, await reader.ReadBlockAsync(buffer, 0, 4));
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            Assert.AreEqual(0, await reader.ReadBlockAsync(buffer, 0, 4));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual async Task TestReadLineAsync()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("first\r\nsecond");

            Assert.AreEqual("first", await reader.ReadLineAsync());
            Assert.AreEqual("second", await reader.ReadLineAsync());
            Assert.IsNull(await reader.ReadLineAsync());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual async Task TestReadToEndAsync()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual("foobar", await reader.ReadToEndAsync());
            Assert.AreEqual(string.Empty, await reader.ReadToEndAsync());
        }

#if FEATURE_TEXTREADER_MEMORY
        [Test]
        [LuceneNetSpecific]
        public virtual async Task TestReadAsyncMemory()
        {
            Memory<char> buffer = new char[4];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual(4, await reader.ReadAsync(buffer));
            Assert.AreEqual("foob", buffer.Slice(0, 4).ToString());

            Assert.AreEqual(2, await reader.ReadAsync(buffer));
            Assert.AreEqual("ar", buffer.Slice(0, 2).ToString());

            Assert.AreEqual(0, await reader.ReadAsync(buffer));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual async Task TestReadBlockAsyncMemory()
        {
            Memory<char> buffer = new char[4];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("foobar");

            Assert.AreEqual(4, await reader.ReadBlockAsync(buffer));
            Assert.AreEqual("foob", buffer.Slice(0, 4).ToString());

            Assert.AreEqual(2, await reader.ReadBlockAsync(buffer));
            Assert.AreEqual("ar", buffer.Slice(0, 2).ToString());

            Assert.AreEqual(0, await reader.ReadBlockAsync(buffer));
        }
#endif

#if FEATURE_TEXTREADER_ASYNC_CANCELLATION
        [Test]
        [LuceneNetSpecific]
        public virtual async Task TestReadLineAsyncCancellation()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("first");

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            NUnit.Framework.Assert.ThrowsAsync<TaskCanceledException>(
                async () => await reader.ReadLineAsync(cancellationTokenSource.Token));

            // Cancellation must not consume input.
            Assert.AreEqual("first", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual async Task TestReadToEndAsyncCancellation()
        {
            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue("first");

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            NUnit.Framework.Assert.ThrowsAsync<TaskCanceledException>(
                async () => await reader.ReadToEndAsync(cancellationTokenSource.Token));

            // Cancellation must not consume input.
            Assert.AreEqual("first", reader.ReadToEnd());
        }
#endif

        [Test]
        [LuceneNetSpecific]
        public virtual void TestMemorySliceWithPartialReads()
        {
            char[] input = "0123456789".ToCharArray();
            char[] buffer = new char[3];

            ReusableStringReader reader = new ReusableStringReader();
            reader.SetValue(new ReadOnlyMemory<char>(input, 2, 5));

            Assert.AreEqual(3, reader.Read(buffer, 0, 3));
            Assert.AreEqual("234", new string(buffer));

            Assert.AreEqual(2, reader.Read(buffer, 0, 3));
            Assert.AreEqual("56", new string(buffer, 0, 2));

            Assert.AreEqual(-1, reader.Read(buffer, 0, 3));
        }
    }
}
