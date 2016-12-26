using System.IO;
using NUnit.Framework;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;

namespace Lucene.Net.Analysis.Gl
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
    /// Simple tests for <seealso cref="GalicianMinimalStemmer"/>
    /// </summary>
    public class TestGalicianMinimalStemFilter : BaseTokenStreamTestCase
    {
        internal Analyzer a = new AnalyzerAnonymousInnerClassHelper();

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            public AnalyzerAnonymousInnerClassHelper()
            {
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new GalicianMinimalStemFilter(tokenizer));
            }
        }

        [Test]
        public virtual void TestPlural()
        {
            CheckOneTerm(a, "elefantes", "elefante");
            CheckOneTerm(a, "elefante", "elefante");
            CheckOneTerm(a, "kalóres", "kalór");
            CheckOneTerm(a, "kalór", "kalór");
        }

        [Test]
        public virtual void TestExceptions()
        {
            CheckOneTerm(a, "mas", "mas");
            CheckOneTerm(a, "barcelonês", "barcelonês");
        }

        [Test]
        public virtual void TestKeyword()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("elefantes"), false);
            Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, exclusionSet);
            CheckOneTerm(a, "elefantes", "elefantes");
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestGalicianMinimalStemFilter outerInstance;

            private CharArraySet exclusionSet;

            public AnalyzerAnonymousInnerClassHelper2(TestGalicianMinimalStemFilter outerInstance, CharArraySet exclusionSet)
            {
                this.outerInstance = outerInstance;
                this.exclusionSet = exclusionSet;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
                return new TokenStreamComponents(source, new GalicianMinimalStemFilter(sink));
            }
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random(), a, 1000 * RANDOM_MULTIPLIER);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
            CheckOneTerm(a, "", "");
        }

        private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
        {
            private readonly TestGalicianMinimalStemFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper3(TestGalicianMinimalStemFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new GalicianMinimalStemFilter(tokenizer));
            }
        }
    }
}