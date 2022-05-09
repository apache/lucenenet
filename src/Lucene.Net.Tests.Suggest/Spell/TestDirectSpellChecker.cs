using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

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

    public class TestDirectSpellChecker : LuceneTestCase
    {
        [Test]
        public void TestInternalLevenshteinDistance()
        {
            DirectSpellChecker spellchecker = new DirectSpellChecker();
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir,
                new MockAnalyzer(Random, MockTokenizer.KEYWORD, true));

            string[] termsToAdd = { "metanoia", "metanoian", "metanoiai", "metanoias", "metanoið‘" };
            for (int i = 0; i < termsToAdd.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("repentance", termsToAdd[i], Field.Store.NO));
                writer.AddDocument(doc);
            }

            IndexReader ir = writer.GetReader();
            string misspelled = "metanoix";
            SuggestWord[] similar = spellchecker.SuggestSimilar(new Term("repentance", misspelled), 4, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertTrue(similar.Length == 4);

            IStringDistance sd = spellchecker.Distance;
            assertTrue(sd is LuceneLevenshteinDistance);
            foreach (SuggestWord word in similar)
            {
                assertTrue(word.Score == sd.GetDistance(word.String, misspelled));
                assertTrue(word.Score == sd.GetDistance(misspelled, word.String)); // LUCNENET TODO: Perhaps change this to word.ToString()?
            }

            ir.Dispose();
            writer.Dispose();
            dir.Dispose();
        }
        [Test]
        public void TestSimpleExamples()
        {
            DirectSpellChecker spellChecker = new DirectSpellChecker();
            spellChecker.MinQueryLength = (0);
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir,
                new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));

            for (int i = 0; i < 20; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("numbers", English.Int32ToEnglish(i), Field.Store.NO));
                writer.AddDocument(doc);
            }

            IndexReader ir = writer.GetReader();

            SuggestWord[] similar = spellChecker.SuggestSimilar(new Term("numbers",
                "fvie"), 2, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertTrue(similar.Length > 0);
            assertEquals("five", similar[0].String);

            similar = spellChecker.SuggestSimilar(new Term("numbers", "five"), 2, ir,
                    SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            if (similar.Length > 0)
            {
                assertFalse(similar[0].String.Equals("five", StringComparison.Ordinal)); // don't suggest a word for itself
            }

            similar = spellChecker.SuggestSimilar(new Term("numbers", "fvie"), 2, ir,
                SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertTrue(similar.Length > 0);
            assertEquals("five", similar[0].String);

            similar = spellChecker.SuggestSimilar(new Term("numbers", "fiv"), 2, ir,
                    SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertTrue(similar.Length > 0);
            assertEquals("five", similar[0].String);

            similar = spellChecker.SuggestSimilar(new Term("numbers", "fives"), 2, ir,
                    SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertTrue(similar.Length > 0);
            assertEquals("five", similar[0].String);

            assertTrue(similar.Length > 0);
            similar = spellChecker.SuggestSimilar(new Term("numbers", "fie"), 2, ir,
                    SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertEquals("five", similar[0].String);

            // add some more documents
            for (int i = 1000; i < 1100; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("numbers", English.Int32ToEnglish(i), Field.Store.NO));
                writer.AddDocument(doc);
            }

            ir.Dispose();
            ir = writer.GetReader();

            // look ma, no spellcheck index rebuild
            similar = spellChecker.SuggestSimilar(new Term("numbers", "tousand"), 10,
                ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertTrue(similar.Length > 0);
            assertEquals("thousand", similar[0].String);

            ir.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestOptions()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir,
                new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));

            Document doc = new Document();
            doc.Add(NewTextField("text", "foobar", Field.Store.NO));
            writer.AddDocument(doc);
            doc.Add(NewTextField("text", "foobar", Field.Store.NO));
            writer.AddDocument(doc);
            doc.Add(NewTextField("text", "foobaz", Field.Store.NO));
            writer.AddDocument(doc);
            doc.Add(NewTextField("text", "fobar", Field.Store.NO));
            writer.AddDocument(doc);

            IndexReader ir = writer.GetReader();

            DirectSpellChecker spellChecker = new DirectSpellChecker();
            spellChecker.MaxQueryFrequency = (0F);
            SuggestWord[] similar = spellChecker.SuggestSimilar(new Term("text",
                "fobar"), 1, ir, SuggestMode.SUGGEST_MORE_POPULAR);
            assertEquals(0, similar.Length);

            spellChecker = new DirectSpellChecker(); // reset defaults
            spellChecker.MinQueryLength = (5);
            similar = spellChecker.SuggestSimilar(new Term("text", "foba"), 1, ir,
                SuggestMode.SUGGEST_MORE_POPULAR);
            assertEquals(0, similar.Length);

            spellChecker = new DirectSpellChecker(); // reset defaults
            spellChecker.MaxEdits = (1);
            similar = spellChecker.SuggestSimilar(new Term("text", "foobazzz"), 1, ir,
                SuggestMode.SUGGEST_MORE_POPULAR);
            assertEquals(0, similar.Length);

            spellChecker = new DirectSpellChecker(); // reset defaults
            spellChecker.Accuracy = (0.9F);
            similar = spellChecker.SuggestSimilar(new Term("text", "foobazzz"), 1, ir,
                SuggestMode.SUGGEST_MORE_POPULAR);
            assertEquals(0, similar.Length);

            spellChecker = new DirectSpellChecker(); // reset defaults
            spellChecker.MinPrefix = (0);
            similar = spellChecker.SuggestSimilar(new Term("text", "roobaz"), 1, ir,
                SuggestMode.SUGGEST_MORE_POPULAR);
            assertEquals(1, similar.Length);
            similar = spellChecker.SuggestSimilar(new Term("text", "roobaz"), 1, ir,
                    SuggestMode.SUGGEST_MORE_POPULAR);

            spellChecker = new DirectSpellChecker(); // reset defaults
            spellChecker.MinPrefix = (1);
            similar = spellChecker.SuggestSimilar(new Term("text", "roobaz"), 1, ir,
                SuggestMode.SUGGEST_MORE_POPULAR);
            assertEquals(0, similar.Length);

            spellChecker = new DirectSpellChecker(); // reset defaults
            spellChecker.MaxEdits = (2);
            similar = spellChecker.SuggestSimilar(new Term("text", "fobar"), 2, ir,
                SuggestMode.SUGGEST_ALWAYS);
            assertEquals(2, similar.Length);

            ir.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestBogusField()
        {
            DirectSpellChecker spellChecker = new DirectSpellChecker();
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir,
                new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));

            for (int i = 0; i < 20; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("numbers", English.Int32ToEnglish(i), Field.Store.NO));
                writer.AddDocument(doc);
            }

            IndexReader ir = writer.GetReader();

            SuggestWord[] similar = spellChecker.SuggestSimilar(new Term(
                "bogusFieldBogusField", "fvie"), 2, ir,
                SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertEquals(0, similar.Length);
            ir.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        // simple test that transpositions work, we suggest five for fvie with ed=1
        [Test]
        public void TestTransposition()
        {
            DirectSpellChecker spellChecker = new DirectSpellChecker();
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir,
                new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));

            for (int i = 0; i < 20; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("numbers", English.Int32ToEnglish(i), Field.Store.NO));
                writer.AddDocument(doc);
            }

            IndexReader ir = writer.GetReader();

            SuggestWord[] similar = spellChecker.SuggestSimilar(new Term(
                "numbers", "fvie"), 1, ir,
                SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertEquals(1, similar.Length);
            assertEquals("five", similar[0].String);
            ir.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        // simple test that transpositions work, we suggest seventeen for seevntene with ed=2
        [Test]
        public void TestTransposition2()
        {
            DirectSpellChecker spellChecker = new DirectSpellChecker();
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir,
                new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));

            for (int i = 0; i < 20; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("numbers", English.Int32ToEnglish(i), Field.Store.NO));
                writer.AddDocument(doc);
            }

            IndexReader ir = writer.GetReader();

            SuggestWord[] similar = spellChecker.SuggestSimilar(new Term(
                "numbers", "seevntene"), 2, ir,
                SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertEquals(1, similar.Length);
            assertEquals("seventeen", similar[0].String);
            ir.Dispose();
            writer.Dispose();
            dir.Dispose();
        }
    }
}
