// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.In
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
    /// Test IndicNormalizer
    /// </summary>
    public class TestIndicNormalizer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// Test some basic normalization
        /// </summary>
        [Test]
        public virtual void TestBasics()
        {
            check("अाॅअाॅ", "ऑऑ");
            check("अाॆअाॆ", "ऒऒ");
            check("अाेअाे", "ओओ");
            check("अाैअाै", "औऔ");
            check("अाअा", "आआ");
            check("अाैर", "और");
            // khanda-ta
            check("ত্‍", "ৎ");
        }

        private void check(string input, string output)
        {
            Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
            TokenFilter tf = new IndicNormalizationFilter(tokenizer);
            AssertTokenStreamContents(tf, new string[] { output });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new IndicNormalizationFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUnknownScript()
        {
            check("foo", "foo");
            check("bar", "bar");
        }
    }
}