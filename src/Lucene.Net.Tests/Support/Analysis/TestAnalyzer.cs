using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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
    [LuceneNetSpecific]

    public class TestAnalyzer : LuceneTestCase
    {
        [Test]
        public void TestGetTokenStream_TextReader()
        {
            using var analyzer = new MockAnalyzer(Random);

            TokenStream stream = analyzer.GetTokenStream(
                "field", new StringReader("first second third"));

            AssertTerms(stream, "first", "second", "third");
        }

        [Test]
        public void TestGetTokenStream_String()
        {
            using var analyzer = new MockAnalyzer(Random);

            TokenStream stream = analyzer.GetTokenStream(
                "field", "first second third");

            AssertTerms(stream, "first", "second", "third");
        }

        [Test]
        public void TestGetTokenStream_ReadOnlyMemory_Unbuffered()
        {
            using var analyzer = new MockAnalyzer(Random);

            char[] buffer = "xxalpha betaYY".ToCharArray();
            ReadOnlyMemory<char> memory =
                new ReadOnlyMemory<char>(buffer, 2, 10);

            TokenStream stream = analyzer.GetTokenStream(
                "field", memory, buffered: false);

            AssertTerms(stream, "alpha", "beta");
        }

        [Test]
        public void TestGetTokenStream_ReadOnlyMemory_Buffered()
        {
            using var analyzer = new MockAnalyzer(Random);

            char[] buffer = "first second third".ToCharArray();
            ReadOnlyMemory<char> memory = buffer;

            TokenStream stream = analyzer.GetTokenStream(
                "field", memory, buffered: true);

            buffer[0] = 'X';

            AssertTerms(stream, "first", "second", "third");
        }

        [Test]
        public void TestGetTokenStream_ReadOnlySpan()
        {
            using var analyzer = new MockAnalyzer(Random);

            char[] buffer = "first second third".ToCharArray();

            TokenStream stream = analyzer.GetTokenStream(
                "field", buffer.AsSpan());

            buffer[0] = 'X';

            AssertTerms(stream, "first", "second", "third");
        }

        [Test]
        public void TestGetTokenStream_ReuseComponentsAcrossOverloads()
        {
            using var analyzer = new MockAnalyzer(Random);

            TokenStream expectedStream = analyzer.GetTokenStream(
                "field", new StringReader("reader input"));

            AssertTerms(expectedStream, "reader", "input");

            TokenStream stream = analyzer.GetTokenStream(
                "field", "string input");

            Assert.AreSame(expectedStream, stream);
            AssertTerms(stream, "string", "input");

            char[] memoryBuffer = "memory input".ToCharArray();

            stream = analyzer.GetTokenStream(
                "field", new ReadOnlyMemory<char>(memoryBuffer),
                buffered: false);

            Assert.AreSame(expectedStream, stream);
            AssertTerms(stream, "memory", "input");

            stream = analyzer.GetTokenStream(
                "field",
                new ReadOnlyMemory<char>("buffered input".ToCharArray()),
                buffered: true);

            Assert.AreSame(expectedStream, stream);
            AssertTerms(stream, "buffered", "input");

            char[] spanBuffer = "span input".ToCharArray();

            stream = analyzer.GetTokenStream(
                "field", spanBuffer.AsSpan());

            Assert.AreSame(expectedStream, stream);
            AssertTerms(stream, "span", "input");
        }

        private static void AssertTerms(TokenStream stream, params string[] expected)
        {
            try
            {
                ICharTermAttribute termAttribute =
                    stream.AddAttribute<ICharTermAttribute>();

                var actual = new JCG.List<string>();

                stream.Reset();
                while (stream.IncrementToken())
                {
                    actual.Add(termAttribute.ToString());
                }
                stream.End();

                Assert.AreEqual(expected, actual);
            }
            finally
            {
                stream.Close();
            }
        }
    }
}
