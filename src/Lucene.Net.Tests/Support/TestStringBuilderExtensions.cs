/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Text;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestStringBuilderExtensions
    {
        [Test, LuceneNetSpecific]
        public virtual void TestReverse()
        {
            var sb = new StringBuilder("foo 𝌆 bar𫀁mañana");

            sb.Reverse();

            Assert.AreEqual("anañam𫀁rab 𝌆 oof", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointBmp()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = 97; // a

            sb.AppendCodePoint(codePoint);

            Assert.AreEqual("foo bara", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointUnicode()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = 3594; // ช

            sb.AppendCodePoint(codePoint);

            Assert.AreEqual("foo barช", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointUTF16Surrogates()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = 176129; // '\uD86C', '\uDC01' (𫀁)

            sb.AppendCodePoint(codePoint);

            Assert.AreEqual("foo bar𫀁", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointTooHigh()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = Character.MAX_CODE_POINT + 1;

            Assert.Throws<ArgumentException>(() => sb.AppendCodePoint(codePoint));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointTooLow()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = Character.MIN_CODE_POINT - 1;

            Assert.Throws<ArgumentException>(() => sb.AppendCodePoint(codePoint));
        }
    }
}
