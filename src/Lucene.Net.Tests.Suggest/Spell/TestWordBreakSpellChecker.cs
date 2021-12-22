using J2N;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Spell
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

    public class TestWordBreakSpellChecker : LuceneTestCase
    {
        private Directory dir = null;

        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, true));

            for (int i = 900; i < 1112; i++)
            {
                Document doc = new Document();
                string num = Regex.Replace(Regex.Replace(English.Int32ToEnglish(i), "[-]", " "), "[,]", "");
                doc.Add(NewTextField("numbers", num, Field.Store.NO));
                writer.AddDocument(doc);
            }

            {
                Document doc = new Document();
                doc.Add(NewTextField("numbers", "thou hast sand betwixt thy toes", Field.Store.NO));
                writer.AddDocument(doc);
            }
            {
                Document doc = new Document();
                doc.Add(NewTextField("numbers", "hundredeight eightyeight yeight", Field.Store.NO));
                writer.AddDocument(doc);
            }
            {
                Document doc = new Document();
                doc.Add(NewTextField("numbers", "tres y cinco", Field.Store.NO));
                writer.AddDocument(doc);
            }

            writer.Commit();
            writer.Dispose();
        }

        public override void TearDown()
        {
            if (dir != null)
            {
                dir.Dispose();
                dir = null;
            }
            base.TearDown();
        }

        [Test]
        public void TestCombiningWords()
        {
            IndexReader ir = null;
            try
            {
                ir = DirectoryReader.Open(dir);
                WordBreakSpellChecker wbsp = new WordBreakSpellChecker();

                {
                    Term[] terms = {
                        new Term("numbers", "one"),
                        new Term("numbers", "hun"),
                        new Term("numbers", "dred"),
                        new Term("numbers", "eight"),
                        new Term("numbers", "y"),
                        new Term("numbers", "eight"),
                    };
                    wbsp.MaxChanges = (3);
                    wbsp.MaxCombineWordLength = (20);
                    wbsp.MinSuggestionFrequency = (1);
                    CombineSuggestion[] cs = wbsp.SuggestWordCombinations(terms, 10, ir, SuggestMode.SUGGEST_ALWAYS);
                    assertTrue(cs.Length == 5);

                    assertTrue(cs[0].OriginalTermIndexes.Length == 2);
                    assertTrue(cs[0].OriginalTermIndexes[0] == 1);
                    assertTrue(cs[0].OriginalTermIndexes[1] == 2);
                    assertTrue(cs[0].Suggestion.String.Equals("hundred", StringComparison.Ordinal));
                    assertTrue(cs[0].Suggestion.Score == 1);

                    assertTrue(cs[1].OriginalTermIndexes.Length == 2);
                    assertTrue(cs[1].OriginalTermIndexes[0] == 3);
                    assertTrue(cs[1].OriginalTermIndexes[1] == 4);
                    assertTrue(cs[1].Suggestion.String.Equals("eighty", StringComparison.Ordinal));
                    assertTrue(cs[1].Suggestion.Score == 1);

                    assertTrue(cs[2].OriginalTermIndexes.Length == 2);
                    assertTrue(cs[2].OriginalTermIndexes[0] == 4);
                    assertTrue(cs[2].OriginalTermIndexes[1] == 5);
                    assertTrue(cs[2].Suggestion.String.Equals("yeight", StringComparison.Ordinal));
                    assertTrue(cs[2].Suggestion.Score == 1);

                    for (int i = 3; i < 5; i++)
                    {
                        assertTrue(cs[i].OriginalTermIndexes.Length == 3);
                        assertTrue(cs[i].Suggestion.Score == 2);
                        assertTrue(
                            (cs[i].OriginalTermIndexes[0] == 1 &&
                             cs[i].OriginalTermIndexes[1] == 2 &&
                             cs[i].OriginalTermIndexes[2] == 3 &&
                             cs[i].Suggestion.String.Equals("hundredeight", StringComparison.Ordinal)) ||
                            (cs[i].OriginalTermIndexes[0] == 3 &&
                             cs[i].OriginalTermIndexes[1] == 4 &&
                             cs[i].OriginalTermIndexes[2] == 5 &&
                             cs[i].Suggestion.String.Equals("eightyeight", StringComparison.Ordinal))
                 );
                    }

                    cs = wbsp.SuggestWordCombinations(terms, 5, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
                    assertTrue(cs.Length == 2);
                    assertTrue(cs[0].OriginalTermIndexes.Length == 2);
                    assertTrue(cs[0].Suggestion.Score == 1);
                    assertTrue(cs[0].OriginalTermIndexes[0] == 1);
                    assertTrue(cs[0].OriginalTermIndexes[1] == 2);
                    assertTrue(cs[0].Suggestion.String.Equals("hundred", StringComparison.Ordinal));
                    assertTrue(cs[0].Suggestion.Score == 1);

                    assertTrue(cs[1].OriginalTermIndexes.Length == 3);
                    assertTrue(cs[1].Suggestion.Score == 2);
                    assertTrue(cs[1].OriginalTermIndexes[0] == 1);
                    assertTrue(cs[1].OriginalTermIndexes[1] == 2);
                    assertTrue(cs[1].OriginalTermIndexes[2] == 3);
                    assertTrue(cs[1].Suggestion.String.Equals("hundredeight", StringComparison.Ordinal));
                }
            }
            //catch (Exception e) // LUCENENET: Senseless to catch and rethrow here
            //{
            //    throw e;
            //}
            finally
            {
                try { ir.Dispose(); } catch (Exception /*e1*/) { }
            }
        }

        [Test]
        public void TestBreakingWords()
        {
            IndexReader ir = null;
            try
            {
                ir = DirectoryReader.Open(dir);
                WordBreakSpellChecker wbsp = new WordBreakSpellChecker();

                {
                    Term term = new Term("numbers", "ninetynine");
                    wbsp.MaxChanges = (1);
                    wbsp.MinBreakWordLength = (1);
                    wbsp.MinSuggestionFrequency = (1);
                    SuggestWord[][] sw = wbsp.SuggestWordBreaks(term, 5, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                    assertTrue(sw.Length == 1);
                    assertTrue(sw[0].Length == 2);
                    assertTrue(sw[0][0].String.Equals("ninety", StringComparison.Ordinal));
                    assertTrue(sw[0][1].String.Equals("nine", StringComparison.Ordinal));
                    assertTrue(sw[0][0].Score == 1);
                    assertTrue(sw[0][1].Score == 1);
                }
                {
                    Term term = new Term("numbers", "onethousand");
                    wbsp.MaxChanges = (1);
                    wbsp.MinBreakWordLength = (1);
                    wbsp.MinSuggestionFrequency = (1);
                    SuggestWord[][] sw = wbsp.SuggestWordBreaks(term, 2, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                    assertTrue(sw.Length == 1);
                    assertTrue(sw[0].Length == 2);
                    assertTrue(sw[0][0].String.Equals("one", StringComparison.Ordinal));
                    assertTrue(sw[0][1].String.Equals("thousand", StringComparison.Ordinal));
                    assertTrue(sw[0][0].Score == 1);
                    assertTrue(sw[0][1].Score == 1);

                    wbsp.MaxChanges = (2);
                    wbsp.MinSuggestionFrequency = (1);
                    sw = wbsp.SuggestWordBreaks(term, 1, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                    assertTrue(sw.Length == 1);
                    assertTrue(sw[0].Length == 2);

                    wbsp.MaxChanges = (2);
                    wbsp.MinSuggestionFrequency = (2);
                    sw = wbsp.SuggestWordBreaks(term, 2, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                    assertTrue(sw.Length == 1);
                    assertTrue(sw[0].Length == 2);

                    wbsp.MaxChanges = (2);
                    wbsp.MinSuggestionFrequency = (1);
                    sw = wbsp.SuggestWordBreaks(term, 2, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                    assertTrue(sw.Length == 2);
                    assertTrue(sw[0].Length == 2);
                    assertTrue(sw[0][0].String.Equals("one", StringComparison.Ordinal));
                    assertTrue(sw[0][1].String.Equals("thousand", StringComparison.Ordinal));
                    assertTrue(sw[0][0].Score == 1);
                    assertTrue(sw[0][1].Score == 1);
                    assertTrue(sw[0][1].Freq > 1);
                    assertTrue(sw[0][0].Freq > sw[0][1].Freq);
                    assertTrue(sw[1].Length == 3);
                    assertTrue(sw[1][0].String.Equals("one", StringComparison.Ordinal));
                    assertTrue(sw[1][1].String.Equals("thou", StringComparison.Ordinal));
                    assertTrue(sw[1][2].String.Equals("sand", StringComparison.Ordinal));
                    assertTrue(sw[1][0].Score == 2);
                    assertTrue(sw[1][1].Score == 2);
                    assertTrue(sw[1][2].Score == 2);
                    assertTrue(sw[1][0].Freq > 1);
                    assertTrue(sw[1][1].Freq == 1);
                    assertTrue(sw[1][2].Freq == 1);
                }
                {
                    Term term = new Term("numbers", "onethousandonehundredeleven");
                    wbsp.MaxChanges = (3);
                    wbsp.MinBreakWordLength = (1);
                    wbsp.MinSuggestionFrequency = (1);
                    SuggestWord[][] sw = wbsp.SuggestWordBreaks(term, 5, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                    assertTrue(sw.Length == 0);

                    wbsp.MaxChanges = (4);
                    sw = wbsp.SuggestWordBreaks(term, 5, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                    assertTrue(sw.Length == 1);
                    assertTrue(sw[0].Length == 5);

                    wbsp.MaxChanges = (5);
                    sw = wbsp.SuggestWordBreaks(term, 5, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                    assertTrue(sw.Length == 2);
                    assertTrue(sw[0].Length == 5);
                    assertTrue(sw[0][1].String.Equals("thousand", StringComparison.Ordinal));
                    assertTrue(sw[1].Length == 6);
                    assertTrue(sw[1][1].String.Equals("thou", StringComparison.Ordinal));
                    assertTrue(sw[1][2].String.Equals("sand", StringComparison.Ordinal));
                }
                {
                    //make sure we can handle 2-char codepoints
                    Term term = new Term("numbers", "\uD864\uDC79");
                    wbsp.MaxChanges = (1);
                    wbsp.MinBreakWordLength = (1);
                    wbsp.MinSuggestionFrequency = (1);
                    SuggestWord[][] sw = wbsp.SuggestWordBreaks(term, 5, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                    assertTrue(sw.Length == 0);
                }

            }
            //catch (Exception e) // LUCENENET: Senseless to catch and rethrow here
            //{
            //    throw e;
            //}
            finally
            {
                try { ir.Dispose(); } catch (Exception /*e1*/) { }
            }
        }

        [Test]
        public void TestRandom()
        {
            int numDocs = TestUtil.NextInt32(Random, (10 * RandomMultiplier),
                (100 * RandomMultiplier));
            Directory dir = null;
            RandomIndexWriter writer = null;
            IndexReader ir = null;
            try
            {
                dir = NewDirectory();
                writer = new RandomIndexWriter(Random, dir, new MockAnalyzer(Random,
                    MockTokenizer.WHITESPACE, false));
                int maxLength = TestUtil.NextInt32(Random, 5, 50);
                IList<string> originals = new JCG.List<string>(numDocs);
                IList<string[]> breaks = new JCG.List<string[]>(numDocs);
                for (int i = 0; i < numDocs; i++)
                {
                    string orig = "";
                    if (Random.nextBoolean())
                    {
                        while (!GoodTestString(orig))
                        {
                            orig = TestUtil.RandomSimpleString(Random, maxLength);
                        }
                    }
                    else
                    {
                        while (!GoodTestString(orig))
                        {
                            orig = TestUtil.RandomUnicodeString(Random, maxLength);
                        }
                    }
                    originals.Add(orig);
                    int totalLength = orig.CodePointCount(0, orig.Length);
                    int breakAt = orig.OffsetByCodePoints(0,
                        TestUtil.NextInt32(Random, 1, totalLength - 1));
                    string[] broken = new string[2];
                    broken[0] = orig.Substring(0, breakAt - 0);
                    broken[1] = orig.Substring(breakAt);
                    breaks.Add(broken);
                    Document doc = new Document();
                    doc.Add(NewTextField("random_break", broken[0] + " " + broken[1],
                        Field.Store.NO));
                    doc.Add(NewTextField("random_combine", orig, Field.Store.NO));
                    writer.AddDocument(doc);
                }
                writer.Commit();
                writer.Dispose();

                ir = DirectoryReader.Open(dir);
                WordBreakSpellChecker wbsp = new WordBreakSpellChecker();
                wbsp.MaxChanges = (1);
                wbsp.MinBreakWordLength = (1);
                wbsp.MinSuggestionFrequency = (1);
                wbsp.MaxCombineWordLength = (maxLength);
                for (int i = 0; i < originals.size(); i++)
                {
                    string orig = originals[i];
                    string left = breaks[i][0];
                    string right = breaks[i][1];
                    {
                        Term term = new Term("random_break", orig);

                        SuggestWord[][] sw = wbsp.SuggestWordBreaks(term, originals.size(),
                            ir, SuggestMode.SUGGEST_ALWAYS,
                            WordBreakSpellChecker.BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY);
                        bool failed = true;
                        foreach (SuggestWord[] sw1 in sw)
                        {
                            assertTrue(sw1.Length == 2);
                            if (sw1[0].String.Equals(left, StringComparison.Ordinal) && sw1[1].String.Equals(right, StringComparison.Ordinal))
                            {
                                failed = false;
                            }
                        }
                        assertFalse("Failed getting break suggestions\n >Original: "
                                  + orig + "\n >Left: " + left + "\n >Right: " + right, failed);
                    }
                    {
                        Term[] terms = {new Term("random_combine", left),
                            new Term("random_combine", right)};
                        CombineSuggestion[] cs = wbsp.SuggestWordCombinations(terms,
                            originals.size(), ir, SuggestMode.SUGGEST_ALWAYS);
                        bool failed = true;
                        foreach (CombineSuggestion cs1 in cs)
                        {
                            assertTrue(cs1.OriginalTermIndexes.Length == 2);
                            if (cs1.Suggestion.String.Equals(left + right, StringComparison.Ordinal))
                            {
                                failed = false;
                            }
                        }
                        assertFalse("Failed getting combine suggestions\n >Original: "
                            + orig + "\n >Left: " + left + "\n >Right: " + right, failed);
                    }
                }

            }
            //catch (Exception e) when (e.IsException()) // LUCENENET: Senseless to catch and rethrow here
            //{
            //    throw e;
            //}
            finally
            {
                try
                {
                    ir.Dispose();
                }
                catch (Exception e1) when (e1.IsException()) { }
                try
                {
                    writer.Dispose();
                }
                catch (Exception e1) when (e1.IsException()) { }
                try
                {
                    dir.Dispose();
                }
                catch (Exception e1) when (e1.IsException()) { }
            }
        }

        private static readonly Regex mockTokenizerWhitespacePattern = new Regex("[ \\t\\r\\n]", RegexOptions.Compiled);

        private bool GoodTestString(string s)
        {
            if (s.CodePointCount(0, s.Length) < 2
                || mockTokenizerWhitespacePattern.Match(s).Success)
            {
                return false;
            }
            return true;
        }
    }
}
