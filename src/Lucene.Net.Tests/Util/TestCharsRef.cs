using J2N.Text;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Text;
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
    public class TestCharsRef : LuceneTestCase
    {
        [Test]
        public virtual void TestUTF16InUTF8Order()
        {
            int numStrings = AtLeast(1000);
            BytesRef[] utf8 = new BytesRef[numStrings];
            CharsRef[] utf16 = new CharsRef[numStrings];

            for (int i = 0; i < numStrings; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                utf8[i] = new BytesRef(s);
                utf16[i] = new CharsRef(s);
            }

            Array.Sort(utf8);
#pragma warning disable 612, 618
            Array.Sort(utf16, CharsRef.UTF16SortedAsUTF8Comparer);
#pragma warning restore 612, 618

            for (int i = 0; i < numStrings; i++)
            {
                Assert.AreEqual(utf8[i].Utf8ToString(), utf16[i].ToString());
            }
        }

        [Test]
        public virtual void TestAppend()
        {
            CharsRef @ref = new CharsRef();
            StringBuilder builder = new StringBuilder();
            int numStrings = AtLeast(10);
            for (int i = 0; i < numStrings; i++)
            {
                char[] charArray = TestUtil.RandomRealisticUnicodeString(Random, 1, 100).ToCharArray();
                int offset = Random.Next(charArray.Length);
                int length = charArray.Length - offset;
                builder.Append(charArray, offset, length);
                @ref.Append(charArray, offset, length);
            }

            Assert.AreEqual(builder.ToString(), @ref.ToString());
        }

        [Test]
        public virtual void TestCopy()
        {
            int numIters = AtLeast(10);
            for (int i = 0; i < numIters; i++)
            {
                CharsRef @ref = new CharsRef();
                char[] charArray = TestUtil.RandomRealisticUnicodeString(Random, 1, 100).ToCharArray();
                int offset = Random.Next(charArray.Length);
                int length = charArray.Length - offset;
                string str = new string(charArray, offset, length);
                @ref.CopyChars(charArray, offset, length);
                Assert.AreEqual(str, @ref.ToString());
            }
        }

        // LUCENE-3590, AIOOBE if you append to a charsref with offset != 0
        [Test]
        public virtual void TestAppendChars()
        {
            char[] chars = new char[] { 'a', 'b', 'c', 'd' };
            CharsRef c = new CharsRef(chars, 1, 3); // bcd
            c.Append(new char[] { 'e' }, 0, 1);
            Assert.AreEqual("bcde", c.ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a charsref with offset != 0
        [Test]
        public virtual void TestCopyChars()
        {
            char[] chars = new char[] { 'a', 'b', 'c', 'd' };
            CharsRef c = new CharsRef(chars, 1, 3); // bcd
            char[] otherchars = new char[] { 'b', 'c', 'd', 'e' };
            c.CopyChars(otherchars, 0, 4);
            Assert.AreEqual("bcde", c.ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a charsref with offset != 0
        [Test]
        public virtual void TestCopyCharsRef()
        {
            char[] chars = new char[] { 'a', 'b', 'c', 'd' };
            CharsRef c = new CharsRef(chars, 1, 3); // bcd
            char[] otherchars = new char[] { 'b', 'c', 'd', 'e' };
            c.CopyChars(new CharsRef(otherchars, 0, 4));
            Assert.AreEqual("bcde", c.ToString());
        }

        // LUCENENET NOTE: Removed the CharAt(int) method from the 
        // ICharSequence interface and replaced with this[int]
        //// LUCENE-3590: fix charsequence to fully obey interface
        //[Test]
        //public virtual void TestCharSequenceCharAt()
        //{
        //    CharsRef c = new CharsRef("abc");

        //    Assert.AreEqual('b', c.CharAt(1));

        //    try
        //    {
        //        c.CharAt(-1);
        //        Assert.Fail();
        //    }
        //    catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
        //    {
        //        // expected exception
        //    }

        //    try
        //    {
        //        c.CharAt(3);
        //        Assert.Fail();
        //    }
        //    catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
        //    {
        //        // expected exception
        //    }
        //}

        // LUCENE-3590: fix charsequence to fully obey interface
        [Test, LuceneNetSpecific]
        public virtual void TestCharSequenceIndexer()
        {
            CharsRef c = new CharsRef("abc");

            Assert.AreEqual('b', c[1]);

            try
            {
                var _ = c[-1];
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }

            try
            {
                var _ = c[3];
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }
        }

        // LUCENE-3590: fix off-by-one in subsequence, and fully obey interface
        // LUCENE-4671: fix subSequence
        [Test]
        public virtual void TestCharSequenceSubSequence()
        {
            ICharSequence[] sequences = {
                new CharsRef("abc"),
                new CharsRef("0abc".ToCharArray(), 1, 3),
                new CharsRef("abc0".ToCharArray(), 0, 3),
                new CharsRef("0abc0".ToCharArray(), 1, 3)
            };

            foreach (ICharSequence c in sequences)
            {
                DoTestSequence(c);
            }
        }

        private void DoTestSequence(ICharSequence c)
        {
            // slice
            Assert.AreEqual("a", c.Subsequence(0, 1 - 0).ToString()); // LUCENENET: Corrected 2nd parameter
            // mid subsequence
            Assert.AreEqual("b", c.Subsequence(1, 2 - 1).ToString()); // LUCENENET: Corrected 2nd parameter
            // end subsequence
            Assert.AreEqual("bc", c.Subsequence(1, 3 - 1).ToString()); // LUCENENET: Corrected 2nd parameter
            // empty subsequence
            Assert.AreEqual("", c.Subsequence(0, 0 - 0).ToString()); // LUCENENET: Corrected 2nd parameter

            try
            {
                c.Subsequence(-1, 1 - -1); // LUCENENET: Corrected 2nd parameter
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }

            try
            {
                c.Subsequence(0, -1 - 0); // LUCENENET: Corrected 2nd parameter
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }

            try
            {
                c.Subsequence(0, 4 - 0); // LUCENENET: Corrected 2nd parameter
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }

            try
            {
                c.Subsequence(2, 1 - 2); // LUCENENET: Corrected 2nd parameter
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }
        }

#if FEATURE_SERIALIZABLE

        [Test, LuceneNetSpecific]
        public void TestSerialization()
        {
            var chars = "The quick brown fox jumped over the lazy dog.".ToCharArray();

            var charsRef = new CharsRef(chars, 8, 10);

            Assert.AreEqual(10, charsRef.Length);
            Assert.AreSame(chars, charsRef.Chars);
            Assert.AreEqual(chars, charsRef.Chars);
            Assert.AreEqual(8, charsRef.Offset);

            var clone = Clone(charsRef);

            Assert.AreEqual(10, clone.Length);
            Assert.AreNotSame(chars, clone.Chars);
            Assert.AreEqual(chars, clone.Chars);
            Assert.AreEqual(8, clone.Offset);
        }
#endif
    }
}