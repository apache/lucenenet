// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.De
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

    public class TestGermanAnalyzer : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new GermanAnalyzer(TEST_VERSION_CURRENT);
            CheckOneTerm(a, "Tisch", "tisch");
            CheckOneTerm(a, "Tische", "tisch");
            CheckOneTerm(a, "Tischen", "tisch");
        }

        [Test]
        public virtual void TestWithKeywordAttribute()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("fischen");
            GermanStemFilter filter = new GermanStemFilter(new SetKeywordMarkerFilter(new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader("Fischen Trinken")), set));
            AssertTokenStreamContents(filter, new string[] { "fischen", "trink" });
        }

        [Test]
        public virtual void TestStemExclusionTable()
        {
            GermanAnalyzer a = new GermanAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, new CharArraySet(TEST_VERSION_CURRENT, AsSet("tischen"), false));
            CheckOneTerm(a, "tischen", "tischen");
        }

        /// <summary>
        /// test some features of the new snowball filter
        /// these only pass with LUCENE_CURRENT, not if you use o.a.l.a.de.GermanStemmer
        /// </summary>
        [Test]
        public virtual void TestGermanSpecials()
        {
            GermanAnalyzer a = new GermanAnalyzer(TEST_VERSION_CURRENT);
            // a/o/u + e is equivalent to the umlaut form
            CheckOneTerm(a, "Schaltflächen", "schaltflach");
            CheckOneTerm(a, "Schaltflaechen", "schaltflach");
            // here they are with the old stemmer
#pragma warning disable 612, 618
            a = new GermanAnalyzer(LuceneVersion.LUCENE_30);
#pragma warning restore 612, 618
            CheckOneTerm(a, "Schaltflächen", "schaltflach");
            CheckOneTerm(a, "Schaltflaechen", "schaltflaech");
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new GermanAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}