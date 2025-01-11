// Lucene version compatibility level 4.8.1
using J2N.IO;
using J2N.Text;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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
    public class TestCharBlockArray : FacetTestCase
    {
        [Test]
        public virtual void TestArray()
        {
            CharBlockArray array = new CharBlockArray();
            StringBuilder builder = new StringBuilder();

            const int n = 100 * 1000;

            byte[] buffer = new byte[50];

            for (int i = 0; i < n; i++)
            {
                Random.NextBytes(buffer);
                int size = 1 + Random.Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                Encoding decoder = StandardCharsets.UTF_8; // LUCENENET specific: no need to set decoder fallback, because it already replaces by default
                string s = decoder.GetString(buffer, 0, size);
                array.Append(s);
                builder.Append(s);
            }

            for (int i = 0; i < n; i++)
            {
                Random.NextBytes(buffer);
                int size = 1 + Random.Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                Encoding decoder = StandardCharsets.UTF_8; // LUCENENET specific: no need to set decoder fallback, because it already replaces by default
                string s = decoder.GetString(buffer, 0, size);
                array.Append(s);
                builder.Append(s);
            }

            for (int i = 0; i < n; i++)
            {
                Random.NextBytes(buffer);
                int size = 1 + Random.Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                Encoding decoder = StandardCharsets.UTF_8; // LUCENENET specific: no need to set decoder fallback, because it already replaces by default
                string s = decoder.GetString(buffer, 0, size);
                for (int j = 0; j < s.Length; j++)
                {
                    array.Append(s[j]);
                }
                builder.Append(s);
            }

            AssertEqualsInternal("GrowingCharArray<->StringBuilder mismatch.", builder, array);

            DirectoryInfo tempDir = CreateTempDir("growingchararray");
            FileInfo f = new FileInfo(Path.Combine(tempDir.FullName, "GrowingCharArrayTest.tmp"));
            using (var @out = new FileStream(f.FullName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                array.Flush(@out);
                @out.Flush();
            }

            using (var @in = new FileStream(f.FullName, FileMode.Open, FileAccess.Read))
            {
                array = CharBlockArray.Open(@in);
                AssertEqualsInternal("GrowingCharArray<->StringBuilder mismatch after flush/load.", builder, array);
            }
            f.Delete();
        }

        private static void AssertEqualsInternal(string msg, StringBuilder expected, CharBlockArray actual)
        {
            // LUCENENET specific - Indexing a string is much faster than StringBuilder (#295)
            var expected2 = expected.ToString();
            var expected2Len = expected2.Length;
            Assert.AreEqual(expected2Len, actual.Length, msg);
            for (int i = 0; i < expected2Len; i++)
            {
                Assert.AreEqual(expected2[i], actual[i], msg);
            }
        }

        // LUCENENET: Borrowed this test from TestCharTermAttributeImpl
        [Test, LuceneNetSpecific]
        public virtual void TestAppendableInterface()
        {
            CharBlockArray t = new CharBlockArray();
            //Formatter formatter = new Formatter(t, Locale.ROOT);
            //formatter.format("%d", 1234);
            //Assert.AreEqual("1234", t.ToString());
            //formatter.format("%d", 5678);
            // LUCENENET: We don't have a formatter in .NET, so continue from here
            t.Append("12345678"); // LUCENENET specific overload that accepts string
            Assert.AreEqual("12345678", t.ToString());
            t = new CharBlockArray();
            t.Append("12345678".ToCharArray()); // LUCENENET specific overload that accepts char[]
            Assert.AreEqual("12345678", t.ToString());
            t.Append('9');
            Assert.AreEqual("123456789", t.ToString());
            t.Append("0".AsCharSequence());
            Assert.AreEqual("1234567890", t.ToString());
            t.Append("0123456789".AsCharSequence(), 1, 3 - 1); // LUCENENET: Corrected 3rd parameter
            Assert.AreEqual("123456789012", t.ToString());
            //t.Append((ICharSequence) CharBuffer.wrap("0123456789".ToCharArray()), 3, 5);
            t.Append("0123456789".ToCharArray(), 3, 5 - 3); // LUCENENET: no CharBuffer in .NET, so we test char[], start, end overload // LUCENENET: Corrected 3rd parameter
            Assert.AreEqual("12345678901234", t.ToString());
            t.Append((ICharSequence)t);
            Assert.AreEqual("1234567890123412345678901234", t.ToString());
            t.Append(/*(ICharSequence)*/ new StringBuilder("0123456789"), 5, 7 - 5); // LUCENENET: StringBuilder doesn't implement ICharSequence, corrected 3rd argument
            Assert.AreEqual("123456789012341234567890123456", t.ToString());
            t.Append(/*(ICharSequence)*/ new StringBuilder(t.ToString())); // LUCENENET: StringBuilder doesn't implement ICharSequence
            Assert.AreEqual("123456789012341234567890123456123456789012341234567890123456", t.ToString());
            // very wierd, to test if a subSlice is wrapped correct :)
            CharBuffer buf = CharBuffer.Wrap("0123456789".ToCharArray(), 3, 5);
            Assert.AreEqual("34567", buf.ToString());
            t = new CharBlockArray();
            t.Append((ICharSequence)buf, 1, 2 - 1); // LUCENENET: Corrected 3rd parameter
            Assert.AreEqual("4", t.ToString());
            CharBlockArray t2 = new CharBlockArray();
            t2.Append("test");
            t.Append((ICharSequence)t2);
            Assert.AreEqual("4test", t.ToString());
            t.Append((ICharSequence)t2, 1, 2 - 1); // LUCENENET: Corrected 3rd parameter
            Assert.AreEqual("4teste", t.ToString());

            try
            {
                t.Append((ICharSequence)t2, 1, 5 - 1); // LUCENENET: Corrected 3rd parameter
                Assert.Fail("Should throw ArgumentOutOfRangeException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException iobe)
#pragma warning restore 168
            {
            }

            try
            {
                t.Append((ICharSequence)t2, 1, 0 - 1); // LUCENENET: Corrected 3rd parameter
                Assert.Fail("Should throw ArgumentOutOfRangeException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException iobe)
#pragma warning restore 168
            {
            }

            string expected = t.ToString();
            t.Append((ICharSequence)null); // No-op
            Assert.AreEqual(expected, t.ToString());


            // LUCENENET specific - test string overloads
            try
            {
                t.Append((string)t2.ToString(), 1, 5 - 1); // LUCENENET: Corrected 3rd parameter
                Assert.Fail("Should throw ArgumentOutOfRangeException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException iobe)
#pragma warning restore 168
            {
            }

            try
            {
                t.Append((string)t2.ToString(), 1, 0 - 1); // LUCENENET: Corrected 3rd parameter
                Assert.Fail("Should throw ArgumentOutOfRangeException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException iobe)
#pragma warning restore 168
            {
            }

            expected = t.ToString();
            t.Append((string)null); // No-op
            Assert.AreEqual(expected, t.ToString());

            // LUCENENET specific - test char[] overloads
            try
            {
                t.Append((char[])t2.ToString().ToCharArray(), 1, 5 - 1); // LUCENENET: Corrected 3rd parameter
                Assert.Fail("Should throw ArgumentOutOfRangeException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException iobe)
#pragma warning restore 168
            {
            }

            try
            {
                t.Append((char[])t2.ToString().ToCharArray(), 1, 0 - 1); // LUCENENET: Corrected 3rd parameter
                Assert.Fail("Should throw ArgumentOutOfRangeException");
            }
#pragma warning disable 168
            catch (ArgumentOutOfRangeException iobe)
#pragma warning restore 168
            {
            }

            expected = t.ToString();
            t.Append((char[])null); // No-op
            Assert.AreEqual(expected, t.ToString());
        }

        // LUCENENET: Borrowed this test from TestCharTermAttributeImpl
        [Test, LuceneNetSpecific]
        public virtual void TestAppendableInterfaceWithLongSequences()
        {
            CharBlockArray t = new CharBlockArray();
            t.Append("01234567890123456789012345678901234567890123456789"); // LUCENENET specific overload that accepts string
            assertEquals("01234567890123456789012345678901234567890123456789", t.ToString());
            t.Append("01234567890123456789012345678901234567890123456789", 3, 50 - 3); // LUCENENET specific overload that accepts string, startIndex, charCount
            Assert.AreEqual("0123456789012345678901234567890123456789012345678934567890123456789012345678901234567890123456789", t.ToString());
            t = new CharBlockArray();
            t.Append("01234567890123456789012345678901234567890123456789".ToCharArray()); // LUCENENET specific overload that accepts char[]
            assertEquals("01234567890123456789012345678901234567890123456789", t.ToString());
            t.Append("01234567890123456789012345678901234567890123456789".ToCharArray(), 3, 50 - 3); // LUCENENET specific overload that accepts char[], startIndex, charCount
            Assert.AreEqual("0123456789012345678901234567890123456789012345678934567890123456789012345678901234567890123456789", t.ToString());
            t = new CharBlockArray();
            t.Append(new StringCharSequence("01234567890123456789012345678901234567890123456789"));
            //t.Append((ICharSequence) CharBuffer.wrap("01234567890123456789012345678901234567890123456789".ToCharArray()), 3, 50); // LUCENENET: No CharBuffer in .NET
            t.Append("01234567890123456789012345678901234567890123456789".ToCharArray(), 3, 50 - 3); // LUCENENET specific overload that accepts char[], startIndex, charCount
            //              "01234567890123456789012345678901234567890123456789"
            Assert.AreEqual("0123456789012345678901234567890123456789012345678934567890123456789012345678901234567890123456789", t.ToString());
            t = new CharBlockArray();
            t.Append(/*(ICharSequence)*/ new StringBuilder("01234567890123456789"), 5, 17 - 5); // LUCENENET: StringBuilder doesn't implement ICharSequence
            Assert.AreEqual((ICharSequence)new StringCharSequence("567890123456"), t /*.ToString()*/);
            t.Append(new StringBuilder(t.ToString()));
            Assert.AreEqual((ICharSequence)new StringCharSequence("567890123456567890123456"), t /*.ToString()*/);
            // very wierd, to test if a subSlice is wrapped correct :)
            CharBuffer buf = CharBuffer.Wrap("012345678901234567890123456789".ToCharArray(), 3, 15);
            Assert.AreEqual("345678901234567", buf.ToString());
            t = new CharBlockArray();
            t.Append(buf, 1, 14 - 1);
            Assert.AreEqual("4567890123456", t.ToString());

            // finally use a completely custom ICharSequence that is not catched by instanceof checks
            const string longTestString = "012345678901234567890123456789";
            t.Append(new CharSequenceAnonymousClass(longTestString));
            Assert.AreEqual("4567890123456" + longTestString, t.ToString());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestSpanAppendableInterface()
        {
            CharBlockArray t = new CharBlockArray();

            // Test with a span
            t.Append("12345678".AsSpan());
            Assert.AreEqual("12345678", t.ToString());

            // test with a span slice
            t.Append("0123456789".AsSpan(3, 5 - 3));
            Assert.AreEqual("1234567834", t.ToString());

            // test with a long span
            t = new CharBlockArray();
            t.Append("01234567890123456789012345678901234567890123456789".AsSpan());
            Assert.AreEqual("01234567890123456789012345678901234567890123456789", t.ToString());

            // test with a long span slice
            t.Append("01234567890123456789012345678901234567890123456789".AsSpan(3, 50 - 3));
            Assert.AreEqual("0123456789012345678901234567890123456789012345678934567890123456789012345678901234567890123456789", t.ToString());
        }

        private sealed class CharSequenceAnonymousClass : ICharSequence
        {
            private readonly string longTestString; // LUCENENET: made readonly

            public CharSequenceAnonymousClass(string longTestString)
            {
                this.longTestString = longTestString;
            }

            bool ICharSequence.HasValue => longTestString != null; // LUCENENET specific (implementation of ICharSequence)

            public char CharAt(int i)
            {
                return longTestString[i];
            }

            // LUCENENET specific - Added to .NETify
            public char this[int i] => longTestString[i];

            public int Length => longTestString.Length;

            public ICharSequence Subsequence(int startIndex, int length) // LUCENENET: Changed semantics to startIndex/length to match .NET
            {
                return new StringCharSequence(longTestString.Substring(startIndex, length));
            }

            public override string ToString()
            {
                return longTestString;
            }
        }
    }
}
