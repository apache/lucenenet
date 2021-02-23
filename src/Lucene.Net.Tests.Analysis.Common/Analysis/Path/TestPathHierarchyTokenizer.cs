// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.CharFilters;
using NUnit.Framework;
using System;
using System.IO;
using Reader = System.IO.TextReader;

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

    public class TestPathHierarchyTokenizer : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestBasic()
        {
            string path = "/a/b/c";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "/a", "/a/b", "/a/b/c" }, new int[] { 0, 0, 0 }, new int[] { 2, 4, 6 }, new int[] { 1, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestEndOfDelimiter()
        {
            string path = "/a/b/c/";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "/a", "/a/b", "/a/b/c", "/a/b/c/" }, new int[] { 0, 0, 0, 0 }, new int[] { 2, 4, 6, 7 }, new int[] { 1, 0, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestStartOfChar()
        {
            string path = "a/b/c";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "a", "a/b", "a/b/c" }, new int[] { 0, 0, 0 }, new int[] { 1, 3, 5 }, new int[] { 1, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestStartOfCharEndOfDelimiter()
        {
            string path = "a/b/c/";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "a", "a/b", "a/b/c", "a/b/c/" }, new int[] { 0, 0, 0, 0 }, new int[] { 1, 3, 5, 6 }, new int[] { 1, 0, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestOnlyDelimiter()
        {
            string path = "/";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "/" }, new int[] { 0 }, new int[] { 1 }, new int[] { 1 }, path.Length);
        }

        [Test]
        public virtual void TestOnlyDelimiters()
        {
            string path = "//";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
            AssertTokenStreamContents(t, new string[] { "/", "//" }, new int[] { 0, 0 }, new int[] { 1, 2 }, new int[] { 1, 0 }, path.Length);
        }

        [Test]
        public virtual void TestReplace()
        {
            string path = "/a/b/c";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), '/', '\\');
            AssertTokenStreamContents(t, new string[] { "\\a", "\\a\\b", "\\a\\b\\c" }, new int[] { 0, 0, 0 }, new int[] { 2, 4, 6 }, new int[] { 1, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestWindowsPath()
        {
            string path = "c:\\a\\b\\c";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), '\\', '\\');
            AssertTokenStreamContents(t, new string[] { "c:", "c:\\a", "c:\\a\\b", "c:\\a\\b\\c" }, new int[] { 0, 0, 0, 0 }, new int[] { 2, 4, 6, 8 }, new int[] { 1, 0, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestNormalizeWinDelimToLinuxDelim()
        {
            NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
            builder.Add("\\", "/");
            NormalizeCharMap normMap = builder.Build();
            string path = "c:\\a\\b\\c";
            Reader cs = new MappingCharFilter(normMap, new StringReader(path));
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(cs);
            AssertTokenStreamContents(t, new string[] { "c:", "c:/a", "c:/a/b", "c:/a/b/c" }, new int[] { 0, 0, 0, 0 }, new int[] { 2, 4, 6, 8 }, new int[] { 1, 0, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestBasicSkip()
        {
            string path = "/a/b/c";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { "/b", "/b/c" }, new int[] { 2, 2 }, new int[] { 4, 6 }, new int[] { 1, 0 }, path.Length);
        }

        [Test]
        public virtual void TestEndOfDelimiterSkip()
        {
            string path = "/a/b/c/";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { "/b", "/b/c", "/b/c/" }, new int[] { 2, 2, 2 }, new int[] { 4, 6, 7 }, new int[] { 1, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestStartOfCharSkip()
        {
            string path = "a/b/c";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { "/b", "/b/c" }, new int[] { 1, 1 }, new int[] { 3, 5 }, new int[] { 1, 0 }, path.Length);
        }

        [Test]
        public virtual void TestStartOfCharEndOfDelimiterSkip()
        {
            string path = "a/b/c/";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { "/b", "/b/c", "/b/c/" }, new int[] { 1, 1, 1 }, new int[] { 3, 5, 6 }, new int[] { 1, 0, 0 }, path.Length);
        }

        [Test]
        public virtual void TestOnlyDelimiterSkip()
        {
            string path = "/";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { }, new int[] { }, new int[] { }, new int[] { }, path.Length);
        }

        [Test]
        public virtual void TestOnlyDelimitersSkip()
        {
            string path = "//";
            PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
            AssertTokenStreamContents(t, new string[] { "/" }, new int[] { 1 }, new int[] { 2 }, new int[] { 1 }, path.Length);
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new PathHierarchyTokenizer(reader);
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
                Tokenizer tokenizer = new PathHierarchyTokenizer(reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            CheckRandomData(random, a, 100 * RandomMultiplier, 1027);
        }
    }
}