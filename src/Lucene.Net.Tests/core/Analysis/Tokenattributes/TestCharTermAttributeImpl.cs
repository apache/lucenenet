using Lucene.Net.Support;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Tokenattributes
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestCharTermAttributeImpl : LuceneTestCase
    {
        [Test]
        public virtual void TestResize()
        {
            CharTermAttribute t = new CharTermAttribute();
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, content.Length);
            for (int i = 0; i < 2000; i++)
            {
                t.ResizeBuffer(i);
                Assert.IsTrue(i <= t.Buffer().Length);
                Assert.AreEqual("hello", t.ToString());
            }
        }

        [Test]
        public virtual void TestGrow()
        {
            CharTermAttribute t = new CharTermAttribute();
            StringBuilder buf = new StringBuilder("ab");
            for (int i = 0; i < 20; i++)
            {
                char[] content = buf.ToString().ToCharArray();
                t.CopyBuffer(content, 0, content.Length);
                Assert.AreEqual(buf.Length, t.Length);
                Assert.AreEqual(buf.ToString(), t.ToString());
                buf.Append(buf.ToString());
            }
            Assert.AreEqual(1048576, t.Length);

            // now as a StringBuilder, first variant
            t = new CharTermAttribute();
            buf = new StringBuilder("ab");
            for (int i = 0; i < 20; i++)
            {
                t.SetEmpty().Append(buf);
                Assert.AreEqual(buf.Length, t.Length);
                Assert.AreEqual(buf.ToString(), t.ToString());
                buf.Append(t);
            }
            Assert.AreEqual(1048576, t.Length);

            // Test for slow growth to a long term
            t = new CharTermAttribute();
            buf = new StringBuilder("a");
            for (int i = 0; i < 20000; i++)
            {
                t.SetEmpty().Append(buf);
                Assert.AreEqual(buf.Length, t.Length);
                Assert.AreEqual(buf.ToString(), t.ToString());
                buf.Append("a");
            }
            Assert.AreEqual(20000, t.Length);
        }

        [Test]
        public virtual void TestToString()
        {
            char[] b = new char[] { 'a', 'l', 'o', 'h', 'a' };
            CharTermAttribute t = new CharTermAttribute();
            t.CopyBuffer(b, 0, 5);
            Assert.AreEqual("aloha", t.ToString());

            t.SetEmpty().Append("hi there");
            Assert.AreEqual("hi there", t.ToString());
        }

        [Test]
        public virtual void TestClone()
        {
            CharTermAttribute t = new CharTermAttribute();
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, 5);
            char[] buf = t.Buffer();
            CharTermAttribute copy = TestToken.AssertCloneIsEqual(t);
            Assert.AreEqual(t.ToString(), copy.ToString());
            Assert.AreNotSame(buf, copy.Buffer());
        }

        [Test]
        public virtual void TestEquals()
        {
            CharTermAttribute t1a = new CharTermAttribute();
            char[] content1a = "hello".ToCharArray();
            t1a.CopyBuffer(content1a, 0, 5);
            CharTermAttribute t1b = new CharTermAttribute();
            char[] content1b = "hello".ToCharArray();
            t1b.CopyBuffer(content1b, 0, 5);
            CharTermAttribute t2 = new CharTermAttribute();
            char[] content2 = "hello2".ToCharArray();
            t2.CopyBuffer(content2, 0, 6);
            Assert.IsTrue(t1a.Equals(t1b));
            Assert.IsFalse(t1a.Equals(t2));
            Assert.IsFalse(t2.Equals(t1b));
        }

        [Test]
        public virtual void TestCopyTo()
        {
            CharTermAttribute t = new CharTermAttribute();
            CharTermAttribute copy = TestToken.AssertCopyIsEqual(t);
            Assert.AreEqual("", t.ToString());
            Assert.AreEqual("", copy.ToString());

            t = new CharTermAttribute();
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, 5);
            char[] buf = t.Buffer();
            copy = TestToken.AssertCopyIsEqual(t);
            Assert.AreEqual(t.ToString(), copy.ToString());
            Assert.AreNotSame(buf, copy.Buffer());
        }

        [Test]
        public virtual void TestAttributeReflection()
        {
            CharTermAttribute t = new CharTermAttribute();
            t.Append("foobar");
            TestUtil.AssertAttributeReflection(t, new Dictionary<string, object>()
            {
                    { typeof(ICharTermAttribute).Name + "#term", "foobar" },
                    { typeof(ITermToBytesRefAttribute).Name + "#bytes", new BytesRef("foobar") }
            });
        }

        [Test]
        public virtual void TestCharSequenceInterface()
        {
            const string s = "0123456789";
            CharTermAttribute t = new CharTermAttribute();
            t.Append(s);

            Assert.AreEqual(s.Length, t.Length);
            Assert.AreEqual("12", t.SubSequence(1, 3).ToString());
            Assert.AreEqual(s, t.SubSequence(0, s.Length).ToString());

            Assert.IsTrue(Regex.IsMatch(t.ToString(), "01\\d+"));
            Assert.IsTrue(Regex.IsMatch(t.SubSequence(3, 5).ToString(), "34"));

            Assert.AreEqual(s.Substring(3, 4), t.SubSequence(3, 7).ToString());

            for (int i = 0; i < s.Length; i++)
            {
                Assert.IsTrue(t.CharAt(i) == s[i]);
            }

            // LUCENENET specific to test indexer
            for (int i = 0; i < s.Length; i++)
            {
                Assert.IsTrue(t[i] == s[i]);
            }
        }

        [Test]
        public virtual void TestAppendableInterface()
        {
            CharTermAttribute t = new CharTermAttribute();
            //Formatter formatter = new Formatter(t, Locale.ROOT);
            //formatter.format("%d", 1234);
            //Assert.AreEqual("1234", t.ToString());
            //formatter.format("%d", 5678);
            // LUCENENET: We don't have a formatter in .NET, so continue from here
            t.Append("12345678"); // LUCENENET specific overload that accepts string
            Assert.AreEqual("12345678", t.ToString());
            t.SetEmpty().Append("12345678".ToCharArray()); // LUCENENET specific overload that accepts char[]
            Assert.AreEqual("12345678", t.ToString());
            t.Append('9');
            Assert.AreEqual("123456789", t.ToString());
            t.Append(new StringCharSequenceWrapper("0"));
            Assert.AreEqual("1234567890", t.ToString());
            t.Append(new StringCharSequenceWrapper("0123456789"), 1, 3);
            Assert.AreEqual("123456789012", t.ToString());
            //t.Append((ICharSequence) CharBuffer.wrap("0123456789".ToCharArray()), 3, 5);
            t.Append("0123456789".ToCharArray(), 3, 5); // LUCENENET: no CharBuffer in .NET, so we test char[], start, end overload
            Assert.AreEqual("12345678901234", t.ToString());
            t.Append((ICharSequence)t);
            Assert.AreEqual("1234567890123412345678901234", t.ToString());
            t.Append(/*(ICharSequence)*/ new StringBuilder("0123456789").ToString(), 5, 7); // LUCENENET: StringBuilder doesn't implement ICharSequence
            Assert.AreEqual("123456789012341234567890123456", t.ToString());
            t.Append(/*(ICharSequence)*/ new StringBuilder(t.ToString()));
            Assert.AreEqual("123456789012341234567890123456123456789012341234567890123456", t.ToString()); // LUCENENET: StringBuilder doesn't implement ICharSequence
            // very wierd, to test if a subSlice is wrapped correct :)
            //CharBuffer buf = CharBuffer.wrap("0123456789".ToCharArray(), 3, 5); // LUCENENET: No CharBuffer in .NET
            StringBuilder buf = new StringBuilder("0123456789", 3, 5, 16);
            Assert.AreEqual("34567", buf.ToString());
            t.SetEmpty().Append(/*(ICharSequence)*/ buf, 1, 2); // LUCENENET: StringBuilder doesn't implement ICharSequence
            Assert.AreEqual("4", t.ToString());
            ICharTermAttribute t2 = new CharTermAttribute();
            t2.Append("test");
            t.Append((ICharSequence)t2);
            Assert.AreEqual("4test", t.ToString());
            t.Append((ICharSequence)t2, 1, 2);
            Assert.AreEqual("4teste", t.ToString());

            try
            {
                t.Append((ICharSequence)t2, 1, 5);
                Assert.Fail("Should throw ArgumentOutOfRangeException");
            }
            catch (System.IndexOutOfRangeException iobe)
            {
            }

            try
            {
                t.Append((ICharSequence)t2, 1, 0);
                Assert.Fail("Should throw ArgumentOutOfRangeException");
            }
            catch (System.IndexOutOfRangeException iobe)
            {
            }

            t.Append((ICharSequence)null);
            Assert.AreEqual("4testenull", t.ToString());


            // LUCENENET specific - test string overloads
            try
            {
                t.Append((string)t2.ToString(), 1, 5);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (System.IndexOutOfRangeException iobe)
            {
            }

            try
            {
                t.Append((string)t2.ToString(), 1, 0);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (System.IndexOutOfRangeException iobe)
            {
            }

            t.Append((string)null);
            Assert.AreEqual("4testenullnull", t.ToString());


            // LUCENENET specific - test char[] overloads
            try
            {
                t.Append((char[])t2.ToString().ToCharArray(), 1, 5);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (System.IndexOutOfRangeException iobe)
            {
            }

            try
            {
                t.Append((char[])t2.ToString().ToCharArray(), 1, 0);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (System.IndexOutOfRangeException iobe)
            {
            }

            t.Append((char[])null);
            Assert.AreEqual("4testenullnullnull", t.ToString());
        }

        [Test]
        public virtual void TestAppendableInterfaceWithLongSequences()
        {
            CharTermAttribute t = new CharTermAttribute();
            t.Append("01234567890123456789012345678901234567890123456789"); // LUCENENET specific overload that accepts string
            assertEquals("01234567890123456789012345678901234567890123456789", t.ToString());
            t.Append("01234567890123456789012345678901234567890123456789", 3, 50); // LUCENENET specific overload that accepts string, start, end
            Assert.AreEqual("0123456789012345678901234567890123456789012345678934567890123456789012345678901234567890123456789", t.ToString());
            t.SetEmpty();
            t.Append("01234567890123456789012345678901234567890123456789".ToCharArray()); // LUCENENET specific overload that accepts char[]
            assertEquals("01234567890123456789012345678901234567890123456789", t.ToString());
            t.Append("01234567890123456789012345678901234567890123456789".ToCharArray(), 3, 50); // LUCENENET specific overload that accepts char[], start, end
            Assert.AreEqual("0123456789012345678901234567890123456789012345678934567890123456789012345678901234567890123456789", t.ToString());
            t.SetEmpty();
            t.Append(new StringCharSequenceWrapper("01234567890123456789012345678901234567890123456789"));
            //t.Append((ICharSequence) CharBuffer.wrap("01234567890123456789012345678901234567890123456789".ToCharArray()), 3, 50); // LUCENENET: No CharBuffer in .NET
            t.Append("01234567890123456789012345678901234567890123456789".ToCharArray(), 3, 50); // LUCENENET specific overload that accepts char[], start, end
            //              "01234567890123456789012345678901234567890123456789"
            Assert.AreEqual("0123456789012345678901234567890123456789012345678934567890123456789012345678901234567890123456789", t.ToString());
            t.SetEmpty().Append(/*(ICharSequence)*/ new StringBuilder("01234567890123456789"), 5, 17); // LUCENENET: StringBuilder doesn't implement ICharSequence
            Assert.AreEqual(new StringCharSequenceWrapper("567890123456"), t.ToString());
            t.Append(new StringBuilder(t.ToString()));
            Assert.AreEqual(new StringCharSequenceWrapper("567890123456567890123456"), t.ToString());
            // very wierd, to test if a subSlice is wrapped correct :)
            //CharBuffer buf = CharBuffer.wrap("012345678901234567890123456789".ToCharArray(), 3, 15); // LUCENENET: No CharBuffer in .NET
            StringBuilder buf = new StringBuilder("012345678901234567890123456789", 3, 15, 16);
            Assert.AreEqual("345678901234567", buf.ToString());
            t.SetEmpty().Append(buf, 1, 14);
            Assert.AreEqual("4567890123456", t.ToString());

            // finally use a completely custom ICharSequence that is not catched by instanceof checks
            const string longTestString = "012345678901234567890123456789";
            t.Append(new CharSequenceAnonymousInnerClassHelper(this, longTestString));
            Assert.AreEqual("4567890123456" + longTestString, t.ToString());
        }

        private class CharSequenceAnonymousInnerClassHelper : ICharSequence
        {
            private readonly TestCharTermAttributeImpl OuterInstance;

            private string LongTestString;

            public CharSequenceAnonymousInnerClassHelper(TestCharTermAttributeImpl outerInstance, string longTestString)
            {
                this.OuterInstance = outerInstance;
                this.LongTestString = longTestString;
            }

            public char CharAt(int i)
            {
                return LongTestString[i];
            }

            // LUCENENET specific - Added to .NETify
            public char this[int i]
            {
                get { return LongTestString[i]; }
            }

            public int Length
            {
                get
                {
                    return LongTestString.Length;
                }
            }

            public ICharSequence SubSequence(int start, int end)
            {
                return new StringCharSequenceWrapper(LongTestString.Substring(start, end - start));
            }

            public override string ToString()
            {
                return LongTestString;
            }
        }

        [Test]
        public virtual void TestNonCharSequenceAppend()
        {
            CharTermAttribute t = new CharTermAttribute();
            t.Append("0123456789");
            t.Append("0123456789");
            Assert.AreEqual("01234567890123456789", t.ToString());
            t.Append(new StringBuilder("0123456789"));
            Assert.AreEqual("012345678901234567890123456789", t.ToString());
            ICharTermAttribute t2 = new CharTermAttribute();
            t2.Append("test");
            t.Append(t2);
            Assert.AreEqual("012345678901234567890123456789test", t.ToString());
            t.Append((string)null);
            t.Append((StringBuilder)null);
            t.Append((ICharTermAttribute)null);
            Assert.AreEqual("012345678901234567890123456789testnullnullnull", t.ToString());
        }

        [Test]
        public virtual void TestExceptions()
        {
            CharTermAttribute t = new CharTermAttribute();
            t.Append("test");
            Assert.AreEqual("test", t.ToString());

            try
            {
                t.CharAt(-1);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (System.IndexOutOfRangeException)
            {
            }

            try
            {
                t.CharAt(4);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (System.IndexOutOfRangeException)
            {
            }

            try
            {
                t.SubSequence(0, 5);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (System.IndexOutOfRangeException)
            {
            }

            try
            {
                t.SubSequence(5, 0);
                Assert.Fail("Should throw IndexOutOfBoundsException");
            }
            catch (System.IndexOutOfRangeException)
            {
            }
        }

        /*

        // test speed of the dynamic instanceof checks in append(ICharSequence),
        // to find the best max length for the generic while (start<end) loop:
        public void testAppendPerf() {
          CharTermAttributeImpl t = new CharTermAttributeImpl();
          final int count = 32;
          ICharSequence[] csq = new ICharSequence[count * 6];
          final StringBuilder sb = new StringBuilder();
          for (int i=0,j=0; i<count; i++) {
            sb.append(i%10);
            final String testString = sb.toString();
            CharTermAttribute cta = new CharTermAttributeImpl();
            cta.append(testString);
            csq[j++] = cta;
            csq[j++] = testString;
            csq[j++] = new StringBuilder(sb);
            csq[j++] = new StringBuffer(sb);
            csq[j++] = CharBuffer.wrap(testString.toCharArray());
            csq[j++] = new ICharSequence() {
              public char charAt(int i) { return testString.charAt(i); }
              public int length() { return testString.length(); }
              public ICharSequence subSequence(int start, int end) { return testString.subSequence(start, end); }
              public String toString() { return testString; }
            };
          }

          Random rnd = newRandom();
          long startTime = System.currentTimeMillis();
          for (int i=0; i<100000000; i++) {
            t.SetEmpty().append(csq[rnd.nextInt(csq.length)]);
          }
          long endTime = System.currentTimeMillis();
          System.out.println("Time: " + (endTime-startTime)/1000.0 + " s");
        }

        */
    }
}