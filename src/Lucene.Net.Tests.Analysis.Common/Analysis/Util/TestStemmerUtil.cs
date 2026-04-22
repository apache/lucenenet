using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Util
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

    /// <summary>
    /// Tests for <see cref="StemmerUtil"/>
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestStemmerUtil : LuceneTestCase
    {
        [Test]
        [TestCase("foobar", 6, "foo", true)]
        [TestCase("foobar", 3, "foo", true)]
        [TestCase("foobar", 6, "bar", false)]
        [TestCase("foobar", 2, "foo", false)]
        public void TestStartsWith(string input, int len, string prefix, bool expected)
        {
            // test len overload
            Assert.AreEqual(expected, StemmerUtil.StartsWith(input.AsSpan(), len, prefix));

            // test no len overload
            Assert.AreEqual(expected, StemmerUtil.StartsWith(input.AsSpan(0, len), prefix));
        }

        [Test]
        [TestCase("foobar", 6, "bar", true)]
        [TestCase("foobar", 3, "bar", false)]
        [TestCase("foobar", 6, "foo", false)]
        [TestCase("foobar", 2, "bar", false)]
        [TestCase("foobar", 3, "foo", true)]
        public void TestEndsWith(string input, int len, string prefix, bool expected)
        {
            // test len overload
            Assert.AreEqual(expected, StemmerUtil.EndsWith(input.AsSpan(), len, prefix));

            // test no len overload
            Assert.AreEqual(expected, StemmerUtil.EndsWith(input.AsSpan(0, len), prefix));
        }

        [Test]
        [TestCase("foobar", 3, 6, "fooar", 5)]
        [TestCase("foobar", 0, 6, "oobar", 5)]
        [TestCase("foobar", 0, 3, "oo", 2)]
        [TestCase("foobar", 5, 6, "fooba", 5)]
        public void TestDelete(string input, int pos, int len, string expected, int expectedLen)
        {
            // test len overload
            char[] buffer = input.ToCharArray();
            Assert.AreEqual(expectedLen, StemmerUtil.Delete(buffer, pos, len));
            Assert.AreEqual(expected, new string(buffer, 0, expectedLen));

            // test no len overload
            buffer = input.ToCharArray();
            Assert.AreEqual(expectedLen, StemmerUtil.Delete(buffer.AsSpan(0, len), pos));
            Assert.AreEqual(expected, new string(buffer, 0, expectedLen));
        }

        [Test]
        [TestCase("foobar", 3, 6, 2, "foor", 4)]
        [TestCase("foobar", 0, 6, 2, "obar", 4)]
        [TestCase("foobar", 0, 3, 2, "o", 1)]
        [TestCase("foobar", 4, 6, 2, "foob", 4)]
        public void TestDeleteN(string input, int pos, int len, int nChars, string expected, int expectedLen)
        {
            // test len overload
            char[] buffer = input.ToCharArray();
            Assert.AreEqual(expectedLen, StemmerUtil.DeleteN(buffer, pos, len, nChars));
            Assert.AreEqual(expected, new string(buffer, 0, expectedLen));

            // test no len overload
            buffer = input.ToCharArray();
            Assert.AreEqual(expectedLen, StemmerUtil.DeleteN(buffer.AsSpan(0, len), pos, nChars));
            Assert.AreEqual(expected, new string(buffer, 0, expectedLen));
        }
    }
}
