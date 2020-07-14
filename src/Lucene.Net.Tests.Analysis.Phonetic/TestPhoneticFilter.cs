using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Phonetic.Language;
using NUnit.Framework;
using System;
using System.IO;

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
    /// Tests <see cref="PhoneticFilter"/>
    /// </summary>
    public class TestPhoneticFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestAlgorithms()
        {
            assertAlgorithm(new Metaphone(), true, "aaa bbb ccc easgasg",
                new String[] { "A", "aaa", "B", "bbb", "KKK", "ccc", "ESKS", "easgasg" });
            assertAlgorithm(new Metaphone(), false, "aaa bbb ccc easgasg",
                new String[] { "A", "B", "KKK", "ESKS" });


            assertAlgorithm(new DoubleMetaphone(), true, "aaa bbb ccc easgasg",
                new String[] { "A", "aaa", "PP", "bbb", "KK", "ccc", "ASKS", "easgasg" });
            assertAlgorithm(new DoubleMetaphone(), false, "aaa bbb ccc easgasg",
                new String[] { "A", "PP", "KK", "ASKS" });


            assertAlgorithm(new Soundex(), true, "aaa bbb ccc easgasg",
                new String[] { "A000", "aaa", "B000", "bbb", "C000", "ccc", "E220", "easgasg" });
            assertAlgorithm(new Soundex(), false, "aaa bbb ccc easgasg",
                new String[] { "A000", "B000", "C000", "E220" });


            assertAlgorithm(new RefinedSoundex(), true, "aaa bbb ccc easgasg",
                new String[] { "A0", "aaa", "B1", "bbb", "C3", "ccc", "E034034", "easgasg" });
            assertAlgorithm(new RefinedSoundex(), false, "aaa bbb ccc easgasg",
                new String[] { "A0", "B1", "C3", "E034034" });


            assertAlgorithm(new Caverphone2(), true, "Darda Karleen Datha Carlene",
                new String[] { "TTA1111111", "Darda", "KLN1111111", "Karleen",
                    "TTA1111111", "Datha", "KLN1111111", "Carlene" });
            assertAlgorithm(new Caverphone2(), false, "Darda Karleen Datha Carlene",
                new String[] { "TTA1111111", "KLN1111111", "TTA1111111", "KLN1111111" });
        }


        static void assertAlgorithm(IStringEncoder encoder, bool inject, String input,
            String[] expected)
        {
            Tokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT,
            new StringReader(input));
            PhoneticFilter filter = new PhoneticFilter(tokenizer, encoder, inject);
            AssertTokenStreamContents(filter, expected);
        }

        /** blast some random strings through the analyzer */
        [Test]
        [Slow]
        public void TestRandomStrings()
        {
            IStringEncoder[] encoders = new IStringEncoder[] {
                new Metaphone(), new DoubleMetaphone(), new Soundex(), new RefinedSoundex(), new Caverphone2()
            };

            foreach (IStringEncoder e in encoders)
            {
                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                    return new TokenStreamComponents(tokenizer, new PhoneticFilter(tokenizer, e, false));
                });

                CheckRandomData(Random, a, 1000 * RandomMultiplier);

                Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                    return new TokenStreamComponents(tokenizer, new PhoneticFilter(tokenizer, e, false));
                });


                CheckRandomData(Random, b, 1000 * RandomMultiplier);
            }
        }

        [Test]
        public void TestEmptyTerm()
        {
            IStringEncoder[] encoders = new IStringEncoder[] {
                new Metaphone(), new DoubleMetaphone(), new Soundex()/*, new RefinedSoundex()*/, new Caverphone2()
            };
            foreach (IStringEncoder e in encoders)
            {
                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new KeywordTokenizer(reader);
                    return new TokenStreamComponents(tokenizer, new PhoneticFilter(tokenizer, e, Random.nextBoolean()));
                });

                CheckOneTerm(a, "", "");
            }
        }
    }
}
