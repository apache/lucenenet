using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Phonetic.Language.Bm;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Phonetic
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
    /// Tests <see cref="BeiderMorseFilter"/>
    /// </summary>
    public class TestBeiderMorseFilter : BaseTokenStreamTestCase
    {
        private Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            return new TokenStreamComponents(tokenizer,
                new BeiderMorseFilter(tokenizer, new PhoneticEngine(NameType.GENERIC, RuleType.EXACT, true)));
        });


        /** generic, "exact" configuration */
        [Test]
        public void TestBasicUsage()
        {
            AssertAnalyzesTo(analyzer, "Angelo",
            new String[] { "anZelo", "andZelo", "angelo", "anhelo", "anjelo", "anxelo" },
            new int[] { 0, 0, 0, 0, 0, 0 },
            new int[] { 6, 6, 6, 6, 6, 6 },
            new int[] { 1, 0, 0, 0, 0, 0 });


            AssertAnalyzesTo(analyzer, "D'Angelo",
                new String[] { "anZelo", "andZelo", "angelo", "anhelo", "anjelo", "anxelo",
                  "danZelo", "dandZelo", "dangelo", "danhelo", "danjelo", "danxelo" },
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                new int[] { 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8 },
                new int[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        }

        /** restrict the output to a set of possible origin languages */
        [Test]
        public void TestLanguageSet()
        {
            LanguageSet languages = LanguageSet.From(new JCG.HashSet<String>() {
                "italian", "greek", "spanish"
            });
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer,
                    new BeiderMorseFilter(tokenizer,
                        new PhoneticEngine(NameType.GENERIC, RuleType.EXACT, true), languages));
            });

            AssertAnalyzesTo(analyzer, "Angelo",
                new String[] { "andZelo", "angelo", "anxelo" },
                new int[] { 0, 0, 0, },
                new int[] { 6, 6, 6, },
                new int[] { 1, 0, 0, });
        }

        /** for convenience, if the input yields no output, we pass it thru as-is */
        [Test]
        public void TestNumbers()
        {
            AssertAnalyzesTo(analyzer, "1234",
                new String[] { "1234" },
                new int[] { 0 },
                new int[] { 4 },
                new int[] { 1 });
        }

        [Test]
        public void TestRandom()
        {
            CheckRandomData(Random, analyzer, 1000 * RandomMultiplier);
        }

        [Test]
        public void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new BeiderMorseFilter(tokenizer, new PhoneticEngine(NameType.GENERIC, RuleType.EXACT, true)));
            });

            CheckOneTerm(a, "", "");
        }

        [Test]
        public void TestCustomAttribute()
        {
            TokenStream stream = new KeywordTokenizer(new StringReader("D'Angelo"));
            stream = new PatternKeywordMarkerFilter(stream, new Regex(".*"));
            stream = new BeiderMorseFilter(stream, new PhoneticEngine(NameType.GENERIC, RuleType.EXACT, true));
            IKeywordAttribute keyAtt = stream.AddAttribute<IKeywordAttribute>();
            stream.Reset();
            int i = 0;
            while (stream.IncrementToken())
            {
                assertTrue(keyAtt.IsKeyword);
                i++;
            }
            assertEquals(12, i);
            stream.End();
            stream.Dispose();
        }
    }
}
