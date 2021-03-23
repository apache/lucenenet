// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Pattern
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

    public class TestPatternCaptureGroupTokenFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestNoPattern()
        {
            TestPatterns("foobarbaz", new string[] { }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, false);
            TestPatterns("foobarbaz", new string[] { }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, true);

            TestPatterns("foo bar baz", new string[] { }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, false);

            TestPatterns("foo bar baz", new string[] { }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, true);
        }

        [Test]
        public virtual void TestNoMatch()
        {
            TestPatterns("foobarbaz", new string[] { "xx" }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, false);
            TestPatterns("foobarbaz", new string[] { "xx" }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, true);

            TestPatterns("foo bar baz", new string[] { "xx" }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, false);

            TestPatterns("foo bar baz", new string[] { "xx" }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, true);
        }

        [Test]
        public virtual void TestNoCapture()
        {
            TestPatterns("foobarbaz", new string[] { ".." }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, false);
            TestPatterns("foobarbaz", new string[] { ".." }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, true);

            TestPatterns("foo bar baz", new string[] { ".." }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, false);

            TestPatterns("foo bar baz", new string[] { ".." }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, true);
        }

        [Test]
        public virtual void TestEmptyCapture()
        {
            TestPatterns("foobarbaz", new string[] { ".(y*)" }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, false);
            TestPatterns("foobarbaz", new string[] { ".(y*)" }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, true);

            TestPatterns("foo bar baz", new string[] { ".(y*)" }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, false);

            TestPatterns("foo bar baz", new string[] { ".(y*)" }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, true);
        }

        [Test]
        public virtual void TestCaptureAll()
        {
            TestPatterns("foobarbaz", new string[] { "(.+)" }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, false);
            TestPatterns("foobarbaz", new string[] { "(.+)" }, new string[] { "foobarbaz" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, true);

            TestPatterns("foo bar baz", new string[] { "(.+)" }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, false);

            TestPatterns("foo bar baz", new string[] { "(.+)" }, new string[] { "foo", "bar", "baz" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, true);
        }

        [Test]
        public virtual void TestCaptureStart()
        {
            TestPatterns("foobarbaz", new string[] { "^(.)" }, new string[] { "f" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, false);
            TestPatterns("foobarbaz", new string[] { "^(.)" }, new string[] { "foobarbaz", "f" }, new int[] { 0, 0 }, new int[] { 9, 9 }, new int[] { 1, 0 }, true);

            TestPatterns("foo bar baz", new string[] { "^(.)" }, new string[] { "f", "b", "b" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, false);

            TestPatterns("foo bar baz", new string[] { "^(.)" }, new string[] { "foo", "f", "bar", "b", "baz", "b" }, new int[] { 0, 0, 4, 4, 8, 8 }, new int[] { 3, 3, 7, 7, 11, 11 }, new int[] { 1, 0, 1, 0, 1, 0 }, true);
        }

        [Test]
        public virtual void TestCaptureMiddle()
        {
            TestPatterns("foobarbaz", new string[] { "^.(.)." }, new string[] { "o" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, false);
            TestPatterns("foobarbaz", new string[] { "^.(.)." }, new string[] { "foobarbaz", "o" }, new int[] { 0, 0 }, new int[] { 9, 9 }, new int[] { 1, 0 }, true);

            TestPatterns("foo bar baz", new string[] { "^.(.)." }, new string[] { "o", "a", "a" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, false);

            TestPatterns("foo bar baz", new string[] { "^.(.)." }, new string[] { "foo", "o", "bar", "a", "baz", "a" }, new int[] { 0, 0, 4, 4, 8, 8 }, new int[] { 3, 3, 7, 7, 11, 11 }, new int[] { 1, 0, 1, 0, 1, 0 }, true);
        }

        [Test]
        public virtual void TestCaptureEnd()
        {
            TestPatterns("foobarbaz", new string[] { "(.)$" }, new string[] { "z" }, new int[] { 0 }, new int[] { 9 }, new int[] { 1 }, false);
            TestPatterns("foobarbaz", new string[] { "(.)$" }, new string[] { "foobarbaz", "z" }, new int[] { 0, 0 }, new int[] { 9, 9 }, new int[] { 1, 0 }, true);

            TestPatterns("foo bar baz", new string[] { "(.)$" }, new string[] { "o", "r", "z" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, false);

            TestPatterns("foo bar baz", new string[] { "(.)$" }, new string[] { "foo", "o", "bar", "r", "baz", "z" }, new int[] { 0, 0, 4, 4, 8, 8 }, new int[] { 3, 3, 7, 7, 11, 11 }, new int[] { 1, 0, 1, 0, 1, 0 }, true);
        }

        [Test]
        public virtual void TestCaptureStartMiddle()
        {
            TestPatterns("foobarbaz", new string[] { "^(.)(.)" }, new string[] { "f", "o" }, new int[] { 0, 0 }, new int[] { 9, 9 }, new int[] { 1, 0 }, false);
            TestPatterns("foobarbaz", new string[] { "^(.)(.)" }, new string[] { "foobarbaz", "f", "o" }, new int[] { 0, 0, 0 }, new int[] { 9, 9, 9 }, new int[] { 1, 0, 0 }, true);

            TestPatterns("foo bar baz", new string[] { "^(.)(.)" }, new string[] { "f", "o", "b", "a", "b", "a" }, new int[] { 0, 0, 4, 4, 8, 8 }, new int[] { 3, 3, 7, 7, 11, 11 }, new int[] { 1, 0, 1, 0, 1, 0 }, false);

            TestPatterns("foo bar baz", new string[] { "^(.)(.)" }, new string[] { "foo", "f", "o", "bar", "b", "a", "baz", "b", "a" }, new int[] { 0, 0, 0, 4, 4, 4, 8, 8, 8 }, new int[] { 3, 3, 3, 7, 7, 7, 11, 11, 11 }, new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0 }, true);
        }

        [Test]
        public virtual void TestCaptureStartEnd()
        {
            TestPatterns("foobarbaz", new string[] { "^(.).+(.)$" }, new string[] { "f", "z" }, new int[] { 0, 0 }, new int[] { 9, 9 }, new int[] { 1, 0 }, false);
            TestPatterns("foobarbaz", new string[] { "^(.).+(.)$" }, new string[] { "foobarbaz", "f", "z" }, new int[] { 0, 0, 0 }, new int[] { 9, 9, 9 }, new int[] { 1, 0, 0 }, true);

            TestPatterns("foo bar baz", new string[] { "^(.).+(.)$" }, new string[] { "f", "o", "b", "r", "b", "z" }, new int[] { 0, 0, 4, 4, 8, 8 }, new int[] { 3, 3, 7, 7, 11, 11 }, new int[] { 1, 0, 1, 0, 1, 0 }, false);

            TestPatterns("foo bar baz", new string[] { "^(.).+(.)$" }, new string[] { "foo", "f", "o", "bar", "b", "r", "baz", "b", "z" }, new int[] { 0, 0, 0, 4, 4, 4, 8, 8, 8 }, new int[] { 3, 3, 3, 7, 7, 7, 11, 11, 11 }, new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0 }, true);
        }

        [Test]
        public virtual void TestCaptureMiddleEnd()
        {
            TestPatterns("foobarbaz", new string[] { "(.)(.)$" }, new string[] { "a", "z" }, new int[] { 0, 0 }, new int[] { 9, 9 }, new int[] { 1, 0 }, false);
            TestPatterns("foobarbaz", new string[] { "(.)(.)$" }, new string[] { "foobarbaz", "a", "z" }, new int[] { 0, 0, 0 }, new int[] { 9, 9, 9 }, new int[] { 1, 0, 0 }, true);

            TestPatterns("foo bar baz", new string[] { "(.)(.)$" }, new string[] { "o", "o", "a", "r", "a", "z" }, new int[] { 0, 0, 4, 4, 8, 8 }, new int[] { 3, 3, 7, 7, 11, 11 }, new int[] { 1, 0, 1, 0, 1, 0 }, false);

            TestPatterns("foo bar baz", new string[] { "(.)(.)$" }, new string[] { "foo", "o", "o", "bar", "a", "r", "baz", "a", "z" }, new int[] { 0, 0, 0, 4, 4, 4, 8, 8, 8 }, new int[] { 3, 3, 3, 7, 7, 7, 11, 11, 11 }, new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0 }, true);
        }

        [Test]
        public virtual void TestMultiCaptureOverlap()
        {
            TestPatterns("foobarbaz", new string[] { "(.(.(.)))" }, new string[] { "foo", "oo", "o", "bar", "ar", "r", "baz", "az", "z" }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 9, 9, 9, 9, 9, 9, 9, 9, 9 }, new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 }, false);
            TestPatterns("foobarbaz", new string[] { "(.(.(.)))" }, new string[] { "foobarbaz", "foo", "oo", "o", "bar", "ar", "r", "baz", "az", "z" }, new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new int[] { 9, 9, 9, 9, 9, 9, 9, 9, 9, 9 }, new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, true);

            TestPatterns("foo bar baz", new string[] { "(.(.(.)))" }, new string[] { "foo", "oo", "o", "bar", "ar", "r", "baz", "az", "z" }, new int[] { 0, 0, 0, 4, 4, 4, 8, 8, 8 }, new int[] { 3, 3, 3, 7, 7, 7, 11, 11, 11 }, new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0 }, false);

            TestPatterns("foo bar baz", new string[] { "(.(.(.)))" }, new string[] { "foo", "oo", "o", "bar", "ar", "r", "baz", "az", "z" }, new int[] { 0, 0, 0, 4, 4, 4, 8, 8, 8 }, new int[] { 3, 3, 3, 7, 7, 7, 11, 11, 11 }, new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0 }, true);
        }

        [Test]
        public virtual void TestMultiPattern()
        {
            TestPatterns("aaabbbaaa", new string[] { "(aaa)", "(bbb)", "(ccc)" }, new string[] { "aaa", "bbb", "aaa" }, new int[] { 0, 0, 0 }, new int[] { 9, 9, 9 }, new int[] { 1, 0, 0 }, false);
            TestPatterns("aaabbbaaa", new string[] { "(aaa)", "(bbb)", "(ccc)" }, new string[] { "aaabbbaaa", "aaa", "bbb", "aaa" }, new int[] { 0, 0, 0, 0 }, new int[] { 9, 9, 9, 9 }, new int[] { 1, 0, 0, 0 }, true);

            TestPatterns("aaa bbb aaa", new string[] { "(aaa)", "(bbb)", "(ccc)" }, new string[] { "aaa", "bbb", "aaa" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, false);

            TestPatterns("aaa bbb aaa", new string[] { "(aaa)", "(bbb)", "(ccc)" }, new string[] { "aaa", "bbb", "aaa" }, new int[] { 0, 4, 8 }, new int[] { 3, 7, 11 }, new int[] { 1, 1, 1 }, true);
        }


        [Test]
        public virtual void TestCamelCase()
        {
            TestPatterns("letsPartyLIKEits1999_dude", new string[] { "([A-Z]{2,})", "(?<![A-Z])([A-Z][a-z]+)", "(?:^|\\b|(?<=[0-9_])|(?<=[A-Z]{2}))([a-z]+)", "([0-9]+)" }, new string[] { "lets", "Party", "LIKE", "its", "1999", "dude" }, new int[] { 0, 0, 0, 0, 0, 0 }, new int[] { 25, 25, 25, 25, 25, 25 }, new int[] { 1, 0, 0, 0, 0, 0, 0 }, false);
            TestPatterns("letsPartyLIKEits1999_dude", new string[] { "([A-Z]{2,})", "(?<![A-Z])([A-Z][a-z]+)", "(?:^|\\b|(?<=[0-9_])|(?<=[A-Z]{2}))([a-z]+)", "([0-9]+)" }, new string[] { "letsPartyLIKEits1999_dude", "lets", "Party", "LIKE", "its", "1999", "dude" }, new int[] { 0, 0, 0, 0, 0, 0, 0 }, new int[] { 25, 25, 25, 25, 25, 25, 25 }, new int[] { 1, 0, 0, 0, 0, 0, 0, 0 }, true);
        }

        [Test]
        public virtual void TestRandomString()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new PatternCaptureGroupTokenFilter(tokenizer, false, new Regex("((..)(..))", RegexOptions.Compiled)));
            });

            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        private void TestPatterns(string input, string[] regexes, string[] tokens, int[] startOffsets, int[] endOffsets, int[] positions, bool preserveOriginal)
        {
            Regex[] patterns = new Regex[regexes.Length];
            for (int i = 0; i < regexes.Length; i++)
            {
                patterns[i] = new Regex(regexes[i], RegexOptions.Compiled);
            }
            TokenStream ts = new PatternCaptureGroupTokenFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), preserveOriginal, patterns);
            AssertTokenStreamContents(ts, tokens, startOffsets, endOffsets, positions);
        }
    }
}