// Lucene version compatibility level 4.8.1
using J2N;
using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Miscellaneous
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

    public class TestStemmerOverrideFilter : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestOverride()
        {
            // lets make booked stem to books
            // the override filter will convert "booked" to "books",
            // but also mark it with KeywordAttribute so Porter will not change it.
            StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder();
            builder.Add("booked", "books");
            Tokenizer tokenizer = new KeywordTokenizer(new StringReader("booked"));
            TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.Build()));
            AssertTokenStreamContents(stream, new string[] { "books" });
        }

        [Test]
        public virtual void TestIgnoreCase()
        {
            // lets make booked stem to books
            // the override filter will convert "booked" to "books",
            // but also mark it with KeywordAttribute so Porter will not change it.
            StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(true);
            builder.Add("boOkEd", "books");
            Tokenizer tokenizer = new KeywordTokenizer(new StringReader("BooKeD"));
            TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.Build()));
            AssertTokenStreamContents(stream, new string[] { "books" });
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIgnoreCase_CharArray()
        {
            // lets make booked stem to books
            // the override filter will convert "booked" to "books",
            // but also mark it with KeywordAttribute so Porter will not change it.
            StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(true);
            builder.Add("boOkEd".ToCharArray(), "books");
            Tokenizer tokenizer = new KeywordTokenizer(new StringReader("BooKeD"));
            TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.Build()));
            AssertTokenStreamContents(stream, new string[] { "books" });
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIgnoreCase_CharSequence()
        {
            // lets make booked stem to books
            // the override filter will convert "booked" to "books",
            // but also mark it with KeywordAttribute so Porter will not change it.
            StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(true);
            builder.Add("boOkEd".AsCharSequence(), "books");
            Tokenizer tokenizer = new KeywordTokenizer(new StringReader("BooKeD"));
            TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.Build()));
            AssertTokenStreamContents(stream, new string[] { "books" });
        }

        [Test]
        public virtual void TestNoOverrides()
        {
            StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(true);
            Tokenizer tokenizer = new KeywordTokenizer(new StringReader("book"));
            TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.Build()));
            AssertTokenStreamContents(stream, new string[] { "book" });
        }

        [Test]
        public virtual void TestRandomRealisticWhiteSpace()
        {
            IDictionary<string, string> map = new Dictionary<string, string>();
            int numTerms = AtLeast(50);
            for (int i = 0; i < numTerms; i++)
            {
                string randomRealisticUnicodeString = TestUtil.RandomRealisticUnicodeString(Random);
                char[] charArray = randomRealisticUnicodeString.ToCharArray();
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < charArray.Length;)
                {
                    int cp = Character.CodePointAt(charArray, j, charArray.Length);
                    if (!Character.IsWhiteSpace(cp))
                    {
                        sb.AppendCodePoint(cp);
                    }
                    j += Character.CharCount(cp);
                }
                if (sb.Length > 0)
                {
                    string value = TestUtil.RandomSimpleString(Random);
                    map[sb.ToString()] = value.Length == 0 ? "a" : value;

                }
            }
            if (map.Count == 0)
            {
                map["booked"] = "books";
            }
            StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(Random.nextBoolean());
            IDictionary<string, string> entrySet = map;
            StringBuilder input = new StringBuilder();
            IList<string> output = new JCG.List<string>();
            foreach (KeyValuePair<string, string> entry in entrySet)
            {
                builder.Add(entry.Key, entry.Value);
                if (Random.nextBoolean() || output.Count == 0)
                {
                    input.Append(entry.Key).Append(' ');
                    output.Add(entry.Value);
                }
            }
            Tokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(input.ToString()));
            TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.Build()));
            AssertTokenStreamContents(stream, output.ToArray());
        }

        [Test]
        public virtual void TestRandomRealisticKeyword()
        {
            IDictionary<string, string> map = new Dictionary<string, string>();
            int numTerms = AtLeast(50);
            for (int i = 0; i < numTerms; i++)
            {
                string randomRealisticUnicodeString = TestUtil.RandomRealisticUnicodeString(Random);
                if (randomRealisticUnicodeString.Length > 0)
                {
                    string value = TestUtil.RandomSimpleString(Random);
                    map[randomRealisticUnicodeString] = value.Length == 0 ? "a" : value;
                }
            }
            if (map.Count == 0)
            {
                map["booked"] = "books";
            }
            StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(Random.nextBoolean());
            IDictionary<string, string> entrySet = map;
            foreach (KeyValuePair<string, string> entry in entrySet)
            {
                builder.Add(entry.Key, entry.Value);
            }
            StemmerOverrideFilter.StemmerOverrideMap build = builder.Build();
            foreach (KeyValuePair<string, string> entry in entrySet)
            {
                if (Random.nextBoolean())
                {
                    Tokenizer tokenizer = new KeywordTokenizer(new StringReader(entry.Key));
                    TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, build));
                    AssertTokenStreamContents(stream, new string[] { entry.Value });
                }
            }
        }
    }
}