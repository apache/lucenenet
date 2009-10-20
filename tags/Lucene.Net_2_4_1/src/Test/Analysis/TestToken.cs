/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis
{
    [TestFixture]
    public class TestToken : LuceneTestCase
    {
        [Test]
        public void TestCtor()
        {
            Token t = new Token();
            char[] content = "hello".ToCharArray();
            t.SetTermBuffer(content, 0, content.Length);
            char[] buf = t.TermBuffer();
            Assert.AreNotSame(t.TermBuffer(), content);
            Assert.AreEqual("hello", t.Term());
            Assert.AreEqual("word", t.Type());
            Assert.AreEqual(0, t.GetFlags());

            t = new Token(6, 22);
            t.SetTermBuffer(content, 0, content.Length);
            Assert.AreEqual("hello", t.Term());
            Assert.AreEqual("(hello,6,22)", t.ToString());
            Assert.AreEqual("word", t.Type());
            Assert.AreEqual(0, t.GetFlags());

            t = new Token(6, 22, 7);
            t.SetTermBuffer(content, 0, content.Length);
            Assert.AreEqual("hello", t.Term());
            Assert.AreEqual("(hello,6,22)", t.ToString());
            Assert.AreEqual(7, t.GetFlags());

            t = new Token(6, 22, "junk");
            t.SetTermBuffer(content, 0, content.Length);
            Assert.AreEqual("hello", t.Term());
            Assert.AreEqual("(hello,6,22,type=junk)", t.ToString());
            Assert.AreEqual(0, t.GetFlags());
        }

        [Test]
        public void TestResize()
        {
            Token t = new Token();
            char[] content = "hello".ToCharArray();
            t.SetTermBuffer(content, 0, content.Length);
            for (int i = 0; i < 2000; i++)
            {
                t.ResizeTermBuffer(i);
                Assert.IsTrue(i <= t.TermBuffer().Length);
                Assert.AreEqual("hello", t.Term());
            }
        }

        [Test]
        public void TestGrow()
        {
            Token t = new Token();
            System.Text.StringBuilder buf = new System.Text.StringBuilder("ab");
            for (int i = 0; i < 20; i++)
            {
                char[] content = buf.ToString().ToCharArray();
                t.SetTermBuffer(content, 0, content.Length);
                Assert.AreEqual(buf.Length, t.TermLength());
                Assert.AreEqual(buf.ToString(), t.Term());
                buf.Append(buf.ToString());
            }
            Assert.AreEqual(1048576, t.TermLength());
            Assert.AreEqual(1179654, t.TermBuffer().Length);

            // now as a string, first variant
            t = new Token();
            buf = new System.Text.StringBuilder("ab");
            for (int i = 0; i < 20; i++)
            {
                String content = buf.ToString();
                t.SetTermBuffer(content, 0, content.Length);
                Assert.AreEqual(content.Length, t.TermLength());
                Assert.AreEqual(content, t.Term());
                buf.Append(content);
            }
            Assert.AreEqual(1048576, t.TermLength());
            Assert.AreEqual(1179654, t.TermBuffer().Length);

            // now as a string, second variant
            t = new Token();
            buf = new System.Text.StringBuilder("ab");
            for (int i = 0; i < 20; i++)
            {
                String content = buf.ToString();
                t.SetTermBuffer(content);
                Assert.AreEqual(content.Length, t.TermLength());
                Assert.AreEqual(content, t.Term());
                buf.Append(content);
            }
            Assert.AreEqual(1048576, t.TermLength());
            Assert.AreEqual(1179654, t.TermBuffer().Length);

            // Test for slow growth to a long term
            t = new Token();
            buf = new System.Text.StringBuilder("a");
            for (int i = 0; i < 20000; i++)
            {
                String content = buf.ToString();
                t.SetTermBuffer(content);
                Assert.AreEqual(content.Length, t.TermLength());
                Assert.AreEqual(content, t.Term());
                buf.Append("a");
            }
            Assert.AreEqual(20000, t.TermLength());
            Assert.AreEqual(20331, t.TermBuffer().Length);

            // Test for slow growth to a long term
            t = new Token();
            buf = new System.Text.StringBuilder("a");
            for (int i = 0; i < 20000; i++)
            {
                String content = buf.ToString();
                t.SetTermBuffer(content);
                Assert.AreEqual(content.Length, t.TermLength());
                Assert.AreEqual(content, t.Term());
                buf.Append("a");
            }
            Assert.AreEqual(20000, t.TermLength());
            Assert.AreEqual(20331, t.TermBuffer().Length);
        }

        [Test]
        public virtual void TestToString()
        {
            char[] b = new char[] { 'a', 'l', 'o', 'h', 'a' };
            Token t = new Token("", 0, 5);
            t.SetTermBuffer(b, 0, 5);
            Assert.AreEqual("(aloha,0,5)", t.ToString());

            t.SetTermText("hi there");
            Assert.AreEqual("(hi there,0,5)", t.ToString());
        }

        [Test]
        public virtual void TestMixedStringArray()
        {
            Token t = new Token("hello", 0, 5);
            Assert.AreEqual(t.TermText(), "hello");
            Assert.AreEqual(t.TermLength(), 5);
            Assert.AreEqual(new System.String(t.TermBuffer(), 0, 5), "hello");
            t.SetTermText("hello2");
            Assert.AreEqual(t.TermLength(), 6);
            Assert.AreEqual(new System.String(t.TermBuffer(), 0, 6), "hello2");
            t.SetTermBuffer("hello3".ToCharArray(), 0, 6);
            Assert.AreEqual(t.TermText(), "hello3");

            // Make sure if we get the buffer and change a character
            // that termText() reflects the change
            char[] buffer = t.TermBuffer();
            buffer[1] = 'o';
            Assert.AreEqual(t.TermText(), "hollo3");
        }

        [Test]
        public void TestClone()
        {
            Token t = new Token(0, 5);
            char[] content = "hello".ToCharArray();
            t.SetTermBuffer(content, 0, 5);
            char[] buf = t.TermBuffer();
            Token copy = (Token)t.Clone();
            Assert.AreNotSame(buf, copy.TermBuffer());
        }
    }
}