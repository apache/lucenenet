using Lucene.Net.Attributes;
using Lucene.Net.Text;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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
    public class TestBufferedReusableStringReader : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            char[] buf = new char[4];

            BufferedReusableStringReader reader = new BufferedReusableStringReader();
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
            BufferedReusableStringReader reader = new BufferedReusableStringReader();

            Assert.AreEqual(-1, reader.Peek());
            Assert.AreEqual(-1, reader.Read());
            Assert.AreEqual(-1, reader.Read(new char[1], 0, 1));
            Assert.AreEqual(-1, reader.Read(new char[2], 1, 1));
            Assert.AreEqual(-1, reader.Read(new char[2], 1, 0));
            Assert.AreEqual("", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestRead()
        {
            char[] buf = new char[4];

            BufferedReusableStringReader reader = new BufferedReusableStringReader();

            reader.SetValue("foobar".AsSpan());

            Assert.AreEqual('f', (char)reader.Peek());
            Assert.AreEqual('f', (char)reader.Peek());
            Assert.AreEqual('f', (char)reader.Read());
            Assert.AreEqual('o', (char)reader.Peek());

            Assert.AreEqual(3, reader.Read(buf, 0, 3));
            Assert.AreEqual("oob", new string(buf, 0, 3));

            Assert.AreEqual(2, reader.Read(buf, 0, 2));
            Assert.AreEqual("ar", new string(buf, 0, 2));

            Assert.AreEqual(-1, reader.Read());
            Assert.AreEqual(-1, reader.Read(buf, 0, buf.Length));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadArrayZeroCount()
        {
            char[] buf = new char[4];
            BufferedReusableStringReader reader = new BufferedReusableStringReader();

            reader.SetValue("foobar".AsSpan());

            Assert.AreEqual(0, reader.Read(buf, 1, 0));
            Assert.AreEqual('f', (char)reader.Read());

            Assert.AreEqual(0, reader.Read(buf, 1, 0));
            Assert.AreEqual(3, reader.Read(buf, 1, 3));
            Assert.AreEqual("oob", new string(buf, 1, 3));

            Assert.AreEqual(2, reader.Read(buf, 2, 2));
            Assert.AreEqual("ar", new string(buf, 2, 2));

            // LUCENENET: Preserve the legacy -1 result, even for a zero-count read at EOF.
            Assert.AreEqual(-1, reader.Read(buf, 2, 0));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadArrayArgumentValidation()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();
            reader.SetValue("abc".AsSpan());

            Assert.Throws<ArgumentNullException>(
                () => reader.Read(null, 0, 1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => reader.Read(new char[3], -1, 1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => reader.Read(new char[3], 0, -1));

            Assert.Throws<ArgumentException>(
                () => reader.Read(new char[3], 2, 2));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestSetValueSpanCopiesInput()
        {
            char[] input = "foobar".ToCharArray();
            BufferedReusableStringReader reader = new BufferedReusableStringReader();

            reader.SetValue(input.AsSpan());

            // Changing the source after SetValue must not affect the reader.
            input[0] = 'X';

            Assert.AreEqual("foobar", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestSetValueResetsPosition()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();

            reader.SetValue("first".AsSpan());
            Assert.AreEqual('f', (char)reader.Read());
            Assert.AreEqual('i', (char)reader.Read());

            reader.SetValue("second".AsSpan());

            Assert.AreEqual('s', (char)reader.Peek());
            Assert.AreEqual("second", reader.ReadToEnd());

            reader.SetValue("third".AsSpan());
            Assert.AreEqual("third", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadToEnd()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();

            reader.SetValue("foobar".AsSpan());

            Assert.AreEqual('f', (char)reader.Read());
            Assert.AreEqual("oobar", reader.ReadToEnd());

            // ReadToEnd at EOF returns an empty string, not null.
            Assert.AreEqual("", reader.ReadToEnd());
            Assert.AreEqual(-1, reader.Read());

            reader.SetValue("again".AsSpan());
            Assert.AreEqual("again", reader.ReadToEnd());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadBlock()
        {
            char[] buf = new char[5];
            BufferedReusableStringReader reader = new BufferedReusableStringReader();

            reader.SetValue("abc".AsSpan());

            Assert.AreEqual(3, reader.ReadBlock(buf, 0, buf.Length));
            Assert.AreEqual("abc", new string(buf, 0, 3));

            // ReadBlock uses the standard .NET EOF result, even though Read()
            // preserves the legacy -1 result.
            Assert.AreEqual(0, reader.ReadBlock(buf, 0, buf.Length));
            Assert.AreEqual(0, reader.ReadBlock(buf, 0, 0));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadLine()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();

            reader.SetValue("first\r\nsecond\nthird\rfourth".AsSpan());

            Assert.AreEqual("first", reader.ReadLine());
            Assert.AreEqual("second", reader.ReadLine());
            Assert.AreEqual("third", reader.ReadLine());
            Assert.AreEqual("fourth", reader.ReadLine());
            Assert.IsNull(reader.ReadLine());
        }

#if FEATURE_TEXTREADER_SPAN
        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadSpan()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();
            reader.SetValue("foobar".AsSpan());

            Span<char> buf = stackalloc char[4];

            Assert.AreEqual(4, reader.Read(buf));
            Assert.AreEqual("foob", buf.ToString());

            Assert.AreEqual(2, reader.Read(buf));
            Assert.AreEqual("ar", buf.Slice(0, 2).ToString());

            // The span overload follows .NET EOF semantics.
            Assert.AreEqual(0, reader.Read(buf));
            Assert.AreEqual(0, reader.Read(Span<char>.Empty));
        }
#endif

        [Test]
        [LuceneNetSpecific]
        public virtual void TestDisposeAndReuse()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();

            reader.SetValue("first".AsSpan());
            Assert.AreEqual('f', (char)reader.Read());

            reader.Dispose();

            Assert.AreEqual(-1, reader.Peek());
            Assert.AreEqual(-1, reader.Read());
            Assert.AreEqual("", reader.ReadToEnd());

            // LUCENENET: Dispose does not permanently close this reusable reader.
            reader.SetValue("second".AsSpan());
            Assert.AreEqual("second", reader.ReadToEnd());

            reader.Dispose();
            reader.SetValue("third".AsSpan());
            Assert.AreEqual("third", reader.ReadToEnd());
        }



        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadAsync()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();
            char[] buffer = new char[4];

            reader.SetValue("foobar".AsSpan());

            Assert.AreEqual(4, reader.ReadAsync(buffer, 0, 4).GetAwaiter().GetResult());
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.ReadAsync(buffer, 0, 4).GetAwaiter().GetResult());
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            Assert.AreEqual(0, reader.ReadAsync(buffer, 0, 4).GetAwaiter().GetResult());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadBlockAsync()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();
            char[] buffer = new char[4];

            reader.SetValue("foobar".AsSpan());

            Assert.AreEqual(4, reader.ReadBlockAsync(buffer, 0, 4).GetAwaiter().GetResult());
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, reader.ReadBlockAsync(buffer, 0, 4).GetAwaiter().GetResult());
            Assert.AreEqual("ar", new string(buffer, 0, 2));

            Assert.AreEqual(0, reader.ReadBlockAsync(buffer, 0, 4).GetAwaiter().GetResult());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadLineAsync()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();
            reader.SetValue("first\r\nsecond\nthird".AsSpan());

            Assert.AreEqual("first", reader.ReadLineAsync().GetAwaiter().GetResult());
            Assert.AreEqual("second", reader.ReadLineAsync().GetAwaiter().GetResult());
            Assert.AreEqual("third", reader.ReadLineAsync().GetAwaiter().GetResult());
            Assert.IsNull(reader.ReadLineAsync().GetAwaiter().GetResult());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestReadToEndAsync()
        {
            BufferedReusableStringReader reader = new BufferedReusableStringReader();
            reader.SetValue("foobar".AsSpan());

            Assert.AreEqual("foobar", reader.ReadToEndAsync().GetAwaiter().GetResult());
            Assert.AreEqual("", reader.ReadToEndAsync().GetAwaiter().GetResult());

            reader.SetValue("again".AsSpan());
            Assert.AreEqual("again", reader.ReadToEndAsync().GetAwaiter().GetResult());
        }
    }
}
