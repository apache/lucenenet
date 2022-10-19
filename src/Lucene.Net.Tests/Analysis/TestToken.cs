using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    using Attribute = Lucene.Net.Util.Attribute;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IAttribute = Lucene.Net.Util.IAttribute;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestToken : LuceneTestCase
    {
        [Test]
        public virtual void TestCtor()
        {
            Token t = new Token();
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, content.Length);
            Assert.AreNotSame(t.Buffer, content);
            Assert.AreEqual(0, t.StartOffset);
            Assert.AreEqual(0, t.EndOffset);
            Assert.AreEqual("hello", t.ToString());
            Assert.AreEqual("word", t.Type);
            Assert.AreEqual(0, t.Flags);

            t = new Token(6, 22);
            t.CopyBuffer(content, 0, content.Length);
            Assert.AreEqual("hello", t.ToString());
            Assert.AreEqual("hello", t.ToString());
            Assert.AreEqual(6, t.StartOffset);
            Assert.AreEqual(22, t.EndOffset);
            Assert.AreEqual("word", t.Type);
            Assert.AreEqual(0, t.Flags);

            t = new Token(6, 22, 7);
            t.CopyBuffer(content, 0, content.Length);
            Assert.AreEqual("hello", t.ToString());
            Assert.AreEqual("hello", t.ToString());
            Assert.AreEqual(6, t.StartOffset);
            Assert.AreEqual(22, t.EndOffset);
            Assert.AreEqual("word", t.Type);
            Assert.AreEqual(7, t.Flags);

            t = new Token(6, 22, "junk");
            t.CopyBuffer(content, 0, content.Length);
            Assert.AreEqual("hello", t.ToString());
            Assert.AreEqual("hello", t.ToString());
            Assert.AreEqual(6, t.StartOffset);
            Assert.AreEqual(22, t.EndOffset);
            Assert.AreEqual("junk", t.Type);
            Assert.AreEqual(0, t.Flags);
        }

        [Test]
        public virtual void TestResize()
        {
            Token t = new Token();
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, content.Length);
            for (int i = 0; i < 2000; i++)
            {
                t.ResizeBuffer(i);
                Assert.IsTrue(i <= t.Buffer.Length);
                Assert.AreEqual("hello", t.ToString());
            }
        }

        [Test]
        public virtual void TestGrow()
        {
            Token t = new Token();
            StringBuilder buf = new StringBuilder("ab");
            for (int i = 0; i < 20; i++)
            {
                char[] content = buf.ToString().ToCharArray();
                t.CopyBuffer(content, 0, content.Length);
                Assert.AreEqual(buf.Length, t.Length);
                Assert.AreEqual(buf.ToString(), t.ToString());
                buf.Append(buf); // LUCENENET: CA1830: Prefer strongly-typed Append and Insert method overloads on StringBuilder
            }
            Assert.AreEqual(1048576, t.Length);

            // now as a string, second variant
            t = new Token();
            buf = new StringBuilder("ab");
            for (int i = 0; i < 20; i++)
            {
                t.SetEmpty().Append(buf);
                string content = buf.ToString();
                Assert.AreEqual(content.Length, t.Length);
                Assert.AreEqual(content, t.ToString());
                buf.Append(content);
            }
            Assert.AreEqual(1048576, t.Length);

            // Test for slow growth to a long term
            t = new Token();
            buf = new StringBuilder("a");
            for (int i = 0; i < 20000; i++)
            {
                t.SetEmpty().Append(buf);
                string content = buf.ToString();
                Assert.AreEqual(content.Length, t.Length);
                Assert.AreEqual(content, t.ToString());
                buf.Append('a');
            }
            Assert.AreEqual(20000, t.Length);

            // Test for slow growth to a long term
            t = new Token();
            buf = new StringBuilder("a");
            for (int i = 0; i < 20000; i++)
            {
                t.SetEmpty().Append(buf);
                string content = buf.ToString();
                Assert.AreEqual(content.Length, t.Length);
                Assert.AreEqual(content, t.ToString());
                buf.Append('a');
            }
            Assert.AreEqual(20000, t.Length);
        }

        [Test]
        public virtual void TestToString()
        {
            char[] b = new char[] { 'a', 'l', 'o', 'h', 'a' };
            Token t = new Token("", 0, 5);
            t.CopyBuffer(b, 0, 5);
            Assert.AreEqual("aloha", t.ToString());

            t.SetEmpty().Append("hi there");
            Assert.AreEqual("hi there", t.ToString());
        }

        [Test]
        public virtual void TestTermBufferEquals()
        {
            Token t1a = new Token();
            char[] content1a = "hello".ToCharArray();
            t1a.CopyBuffer(content1a, 0, 5);
            Token t1b = new Token();
            char[] content1b = "hello".ToCharArray();
            t1b.CopyBuffer(content1b, 0, 5);
            Token t2 = new Token();
            char[] content2 = "hello2".ToCharArray();
            t2.CopyBuffer(content2, 0, 6);
            Assert.IsTrue(t1a.Equals(t1b));
            Assert.IsFalse(t1a.Equals(t2));
            Assert.IsFalse(t2.Equals(t1b));
        }

        [Test]
        public virtual void TestMixedStringArray()
        {
            Token t = new Token("hello", 0, 5);
            Assert.AreEqual(t.Length, 5);
            Assert.AreEqual(t.ToString(), "hello");
            t.SetEmpty().Append("hello2");
            Assert.AreEqual(t.Length, 6);
            Assert.AreEqual(t.ToString(), "hello2");
            t.CopyBuffer("hello3".ToCharArray(), 0, 6);
            Assert.AreEqual(t.ToString(), "hello3");

            char[] buffer = t.Buffer;
            buffer[1] = 'o';
            Assert.AreEqual(t.ToString(), "hollo3");
        }

        [Test]
        public virtual void TestClone()
        {
            Token t = new Token(0, 5);
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, 5);
            char[] buf = t.Buffer;
            Token copy = AssertCloneIsEqual(t);
            Assert.AreEqual(t.ToString(), copy.ToString());
            Assert.AreNotSame(buf, copy.Buffer);

            BytesRef pl = new BytesRef(new byte[] { 1, 2, 3, 4 });
            t.Payload = pl;
            copy = AssertCloneIsEqual(t);
            Assert.AreEqual(pl, copy.Payload);
            Assert.AreNotSame(pl, copy.Payload);
        }

        [Test]
        public virtual void TestCopyTo()
        {
            Token t = new Token();
            Token copy = AssertCopyIsEqual(t);
            Assert.AreEqual("", t.ToString());
            Assert.AreEqual("", copy.ToString());

            t = new Token(0, 5);
            char[] content = "hello".ToCharArray();
            t.CopyBuffer(content, 0, 5);
            char[] buf = t.Buffer;
            copy = AssertCopyIsEqual(t);
            Assert.AreEqual(t.ToString(), copy.ToString());
            Assert.AreNotSame(buf, copy.Buffer);

            BytesRef pl = new BytesRef(new byte[] { 1, 2, 3, 4 });
            t.Payload = pl;
            copy = AssertCopyIsEqual(t);
            Assert.AreEqual(pl, copy.Payload);
            Assert.AreNotSame(pl, copy.Payload);
        }

        public interface ISenselessAttribute : Lucene.Net.Util.IAttribute
        {
        }

        public sealed class SenselessAttribute : Attribute, ISenselessAttribute
        {
            public override void CopyTo(IAttribute target)
            {
            }

            public override void Clear()
            {
            }

            public override bool Equals(object o)
            {
                return (o is SenselessAttribute);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Test]
        public virtual void TestTokenAttributeFactory()
        {
            TokenStream ts = new MockTokenizer(Token.TOKEN_ATTRIBUTE_FACTORY, new StringReader("foo bar"), MockTokenizer.WHITESPACE, false, MockTokenizer.DEFAULT_MAX_TOKEN_LENGTH);

            Assert.IsTrue(ts.AddAttribute<ISenselessAttribute>() is SenselessAttribute, "SenselessAttribute is not implemented by SenselessAttributeImpl");

            Assert.IsTrue(ts.AddAttribute<ICharTermAttribute>() is Token, "CharTermAttribute is not implemented by Token");
            Assert.IsTrue(ts.AddAttribute<IOffsetAttribute>() is Token, "OffsetAttribute is not implemented by Token");
            Assert.IsTrue(ts.AddAttribute<IFlagsAttribute>() is Token, "FlagsAttribute is not implemented by Token");
            Assert.IsTrue(ts.AddAttribute<IPayloadAttribute>() is Token, "PayloadAttribute is not implemented by Token");
            Assert.IsTrue(ts.AddAttribute<IPositionIncrementAttribute>() is Token, "PositionIncrementAttribute is not implemented by Token");
            Assert.IsTrue(ts.AddAttribute<ITypeAttribute>() is Token, "TypeAttribute is not implemented by Token");
        }

        [Test]
        public virtual void TestAttributeReflection()
        {
            Token t = new Token("foobar", 6, 22, 8);
            TestUtil.AssertAttributeReflection(t, new Dictionary<string, object>()
            {
                { typeof(ICharTermAttribute).Name + "#term", "foobar" },
                { typeof(ITermToBytesRefAttribute).Name + "#bytes", new BytesRef("foobar") },
                { typeof(IOffsetAttribute).Name + "#startOffset", 6 },
                { typeof(IOffsetAttribute).Name + "#endOffset", 22 },
                { typeof(IPositionIncrementAttribute).Name + "#positionIncrement", 1 },
                { typeof(IPayloadAttribute).Name + "#payload", null },
                { typeof(ITypeAttribute).Name + "#type", TypeAttribute.DEFAULT_TYPE },
                { typeof(IFlagsAttribute).Name + "#flags", 8 }
            });
        }

        public static T AssertCloneIsEqual<T>(T att) where T : Attribute
        {
            T clone = (T)att.Clone();
            Assert.AreEqual(att, clone, "Clone must be equal");
            Assert.AreEqual(att.GetHashCode(), clone.GetHashCode(), "Clone's hashcode must be equal");
            return clone;
        }

        public static T AssertCopyIsEqual<T>(T att) where T : Attribute
        {
            T copy = (T)System.Activator.CreateInstance(att.GetType());
            att.CopyTo(copy);
            Assert.AreEqual(att, copy, "Copied instance must be equal");
            Assert.AreEqual(att.GetHashCode(), copy.GetHashCode(), "Copied instance's hashcode must be equal");
            return copy;
        }
    }
}