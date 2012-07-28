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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Reverse;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Reverse
{
    [TestFixture]
    public class TestReverseStringFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestFilter()
        {
            TokenStream stream = new WhitespaceTokenizer(
                new StringReader("Do have a nice day"));     // 1-4 length string
            ReverseStringFilter filter = new ReverseStringFilter(stream);
            ITermAttribute text = filter.GetAttribute<ITermAttribute>();
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("oD", text.Term);
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("evah", text.Term);
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("a", text.Term);
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("ecin", text.Term);
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("yad", text.Term);
            Assert.False(filter.IncrementToken());
        }

        [Test]
        public void TestFilterWithMark()
        {
            TokenStream stream = new WhitespaceTokenizer(new StringReader(
                "Do have a nice day")); // 1-4 length string
            ReverseStringFilter filter = new ReverseStringFilter(stream, '\u0001');
            ITermAttribute text = filter.GetAttribute<ITermAttribute>();
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001oD", text.Term);
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001evah", text.Term);
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001a", text.Term);
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001ecin", text.Term);
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001yad", text.Term);
            Assert.False(filter.IncrementToken());
        }

        [Test]
        public void TestReverseString()
        {
            Assert.AreEqual("A", ReverseStringFilter.Reverse("A"));
            Assert.AreEqual("BA", ReverseStringFilter.Reverse("AB"));
            Assert.AreEqual("CBA", ReverseStringFilter.Reverse("ABC"));
        }

        [Test]
        public void TestReverseChar()
        {
            char[] buffer = { 'A', 'B', 'C', 'D', 'E', 'F' };
            ReverseStringFilter.Reverse(buffer, 2, 3);
            Assert.AreEqual("ABEDCF", new String(buffer));
        }
    }
}
