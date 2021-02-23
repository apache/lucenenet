// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Path
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

    public class TestReversePathHierarchyTokenizer : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestBasicReverse()
        {
            string path = "/a/b/c";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "/a/b/c", "a/b/c", "b/c", "c" }, new int[] { 0, 1, 3, 5 }, new int[] { 6, 6, 6, 6 }, new int[] { 1, 0, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestEndOfDelimiterReverse()
        {
            string path = "/a/b/c/";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "/a/b/c/", "a/b/c/", "b/c/", "c/" }, new int[] { 0, 1, 3, 5 }, new int[] { 7, 7, 7, 7 }, new int[] { 1, 0, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestStartOfCharReverse()
        {
            string path = "a/b/c";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "a/b/c", "b/c", "c" }, new int[] { 0, 2, 4 }, new int[] { 5, 5, 5 }, new int[] { 1, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestStartOfCharEndOfDelimiterReverse()
        {
            string path = "a/b/c/";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "a/b/c/", "b/c/", "c/" }, new int[] { 0, 2, 4 }, new int[] { 6, 6, 6 }, new int[] { 1, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestOnlyDelimiterReverse()
        {
            string path = "/";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "/" }, new int[] { 0 }, new int[] { 1 }, new int[] { 1 }, path.Length);
        }

        [Test]
        public virtual void TestOnlyDelimitersReverse()
        {
            string path = "//";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "//", "/" }, new int[] { 0, 1 }, new int[] { 2, 2 }, new int[] { 1, 0 }, path.Length);
        }

        [Test]
        public virtual void TestEndOfDelimiterReverseSkip()
        {
            string path = "/a/b/c/";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { "/a/b/", "a/b/", "b/" }, new int[] { 0, 1, 3 }, new int[] { 5, 5, 5 }, new int[] { 1, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestStartOfCharReverseSkip()
        {
            string path = "a/b/c";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { "a/b/", "b/" }, new int[] { 0, 2 }, new int[] { 4, 4 }, new int[] { 1, 0 }, path.Length);
        }

        [Test]
        public virtual void TestStartOfCharEndOfDelimiterReverseSkip()
        {
            string path = "a/b/c/";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { "a/b/", "b/" }, new int[] { 0, 2 }, new int[] { 4, 4 }, new int[] { 1, 0 }, path.Length);
        }

        [Test]
        public virtual void TestOnlyDelimiterReverseSkip()
        {
            string path = "/";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { }, new int[] { }, new int[] { }, new int[] { }, path.Length);
        }

        [Test]
        public virtual void TestOnlyDelimitersReverseSkip()
        {
            string path = "//";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { "/" }, new int[] { 0 }, new int[] { 1 }, new int[] { 1 }, path.Length);
        }

        [Test]
        public virtual void TestReverseSkip2()
        {
            string path = "/a/b/c/";
            ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 2);
            AssertTokenStreamContents(t, new string[] { "/a/", "a/" }, new int[] { 0, 1 }, new int[] { 3, 3 }, new int[] { 1, 0 }, path.Length);
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new ReversePathHierarchyTokenizer(reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        /// <summary>
        /// blast some random large strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomHugeStrings()
        {
            Random random = Random;
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new ReversePathHierarchyTokenizer(reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            CheckRandomData(random, a, 100 * RandomMultiplier, 1027);
        }
    }
}