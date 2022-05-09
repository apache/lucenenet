#if FEATURE_BREAKITERATOR
using ICU4N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Attributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Search.PostingsHighlight
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
    /// LUCENENET specific - Modified the behavior of the PostingsHighlighter in Java to return the
    /// org.ibm.icu.BreakIterator version 60.1 instead of java.text.BreakIterator and modified the original Lucene
    /// tests to pass, then ported to .NET. The only change required was that of the TestEmptyHighlights method
    /// which breaks the sentence in a different place than in the JDK.
    /// <para/>
    /// Although the ICU <see cref="BreakIterator"/> acts slightly different than the JDK's verision, using the default 
    /// behavior of the ICU <see cref="BreakIterator"/> is the most logical default to use in .NET. It is the same
    /// default that was chosen in Apache Harmony.
    /// </summary>
    [SuppressCodecs("MockFixedIntBlock", "MockVariableIntBlock", "MockSep", "MockRandom", "Lucene3x")]
    public class TestICUPostingsHighlighter : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public void TestBasics()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test. Just a test highlighting from postings. Feel free to ignore.");
            iw.AddDocument(doc);
            body.SetStringValue("Highlighting the first term. Hope it works.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "highlighting"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(2, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs);
            assertEquals(2, snippets.Length);
            assertEquals("Just a test <b>highlighting</b> from postings. ", snippets[0]);
            assertEquals("<b>Highlighting</b> the first term. ", snippets[1]);

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestFormatWithMatchExceedingContentLength2()
        {

            String bodyText = "123 TEST 01234 TEST";

            String[]
            snippets = formatWithMatchExceedingContentLength(bodyText);

            assertEquals(1, snippets.Length);
            assertEquals("123 <b>TEST</b> 01234 TE", snippets[0]);
        }

        [Test, LuceneNetSpecific]
        public void TestFormatWithMatchExceedingContentLength3()
        {

            String bodyText = "123 5678 01234 TEST TEST";

            String[]
            snippets = formatWithMatchExceedingContentLength(bodyText);

            assertEquals(1, snippets.Length);
            assertEquals("123 5678 01234 TE", snippets[0]);
        }

        [Test, LuceneNetSpecific]
        public void TestFormatWithMatchExceedingContentLength()
        {

            String bodyText = "123 5678 01234 TEST";

            String[]
            snippets = formatWithMatchExceedingContentLength(bodyText);

            assertEquals(1, snippets.Length);
            // LUCENE-5166: no snippet
            assertEquals("123 5678 01234 TE", snippets[0]);
        }

        private String[] formatWithMatchExceedingContentLength(String bodyText)
        {

            int maxLength = 17;

            Analyzer analyzer = new MockAnalyzer(Random);

            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType fieldType = new FieldType(TextField.TYPE_STORED);
            fieldType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", bodyText, fieldType);

            Document doc = new Document();
            doc.Add(body);

            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);

            Query query = new TermQuery(new Term("body", "test"));

            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);

            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter(maxLength);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs);


            ir.Dispose();
            dir.Dispose();
            return snippets;
        }

        // simple test highlighting last word.
        [Test, LuceneNetSpecific]
        public void TestHighlightLastWord()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "test"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs);
            assertEquals(1, snippets.Length);
            assertEquals("This is a <b>test</b>", snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        // simple test with one sentence documents.
        [Test, LuceneNetSpecific]
        public void TestOneSentence()
        {
            Directory dir = NewDirectory();
            // use simpleanalyzer for more natural tokenization (else "test." is a token)
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test.");
            iw.AddDocument(doc);
            body.SetStringValue("Test a one sentence document.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "test"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(2, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs);
            assertEquals(2, snippets.Length);
            assertEquals("This is a <b>test</b>.", snippets[0]);
            assertEquals("<b>Test</b> a one sentence document.", snippets[1]);

            ir.Dispose();
            dir.Dispose();
        }

        // simple test with multiple values that make a result longer than maxLength.
        [Test, LuceneNetSpecific]
        public void TestMaxLengthWithMultivalue()
        {
            Directory dir = NewDirectory();
            // use simpleanalyzer for more natural tokenization (else "test." is a token)
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Document doc = new Document();

            for (int i = 0; i < 3; i++)
            {
                Field body = new Field("body", "", offsetsType);
                body.SetStringValue("This is a multivalued field");
                doc.Add(body);
            }

            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter(40);
            Query query = new TermQuery(new Term("body", "field"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs);
            assertEquals(1, snippets.Length);
            assertTrue("Snippet should have maximum 40 characters plus the pre and post tags",
                snippets[0].Length == (40 + "<b></b>".Length));

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestMultipleFields()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Field title = new Field("title", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);
            doc.Add(title);

            body.SetStringValue("This is a test. Just a test highlighting from postings. Feel free to ignore.");
            title.SetStringValue("I am hoping for the best.");
            iw.AddDocument(doc);
            body.SetStringValue("Highlighting the first term. Hope it works.");
            title.SetStringValue("But best may not be good enough.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("body", "highlighting")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("title", "best")), Occur.SHOULD);
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(2, topDocs.TotalHits);
            IDictionary<String, String[]> snippets = highlighter.HighlightFields(new String[] { "body", "title" }, query, searcher, topDocs);
            assertEquals(2, snippets.size());
            assertEquals("Just a test <b>highlighting</b> from postings. ", snippets["body"][0]);
            assertEquals("<b>Highlighting</b> the first term. ", snippets["body"][1]);
            assertEquals("I am hoping for the <b>best</b>.", snippets["title"][0]);
            assertEquals("But <b>best</b> may not be good enough.", snippets["title"][1]);
            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestMultipleTerms()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test. Just a test highlighting from postings. Feel free to ignore.");
            iw.AddDocument(doc);
            body.SetStringValue("Highlighting the first term. Hope it works.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("body", "highlighting")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("body", "just")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("body", "first")), Occur.SHOULD);
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(2, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs);
            assertEquals(2, snippets.Length);
            assertEquals("<b>Just</b> a test <b>highlighting</b> from postings. ", snippets[0]);
            assertEquals("<b>Highlighting</b> the <b>first</b> term. ", snippets[1]);

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestMultiplePassages()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test. Just a test highlighting from postings. Feel free to ignore.");
            iw.AddDocument(doc);
            body.SetStringValue("This test is another test. Not a good sentence. Test test test test.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "test"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(2, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 2);
            assertEquals(2, snippets.Length);
            assertEquals("This is a <b>test</b>. Just a <b>test</b> highlighting from postings. ", snippets[0]);
            assertEquals("This <b>test</b> is another <b>test</b>. ... <b>Test</b> <b>test</b> <b>test</b> <b>test</b>.", snippets[1]);

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestUserFailedToIndexOffsets()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType positionsType = new FieldType(TextField.TYPE_STORED);
            positionsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
            Field body = new Field("body", "", positionsType);
            Field title = new StringField("title", "", Field.Store.YES);
            Document doc = new Document();
            doc.Add(body);
            doc.Add(title);

            body.SetStringValue("This is a test. Just a test highlighting from postings. Feel free to ignore.");
            title.SetStringValue("test");
            iw.AddDocument(doc);
            body.SetStringValue("This test is another test. Not a good sentence. Test test test test.");
            title.SetStringValue("test");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "test"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(2, topDocs.TotalHits);
            try
            {
                highlighter.Highlight("body", query, searcher, topDocs, 2);
                fail("did not hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }

            try
            {
                highlighter.Highlight("title", new TermQuery(new Term("title", "test")), searcher, topDocs, 2);
                fail("did not hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestBuddhism()
        {
            String text = "This eight-volume set brings together seminal papers in Buddhist studies from a vast " +
                          "range of academic disciplines published over the last forty years. With a new introduction " +
                          "by the editor, this collection is a unique and unrivalled research resource for both " +
                          "student and scholar. Coverage includes: - Buddhist origins; early history of Buddhism in " +
                          "South and Southeast Asia - early Buddhist Schools and Doctrinal History; Theravada Doctrine " +
                          "- the Origins and nature of Mahayana Buddhism; some Mahayana religious topics - Abhidharma " +
                          "and Madhyamaka - Yogacara, the Epistemological tradition, and Tathagatagarbha - Tantric " +
                          "Buddhism (Including China and Japan); Buddhism in Nepal and Tibet - Buddhism in South and " +
                          "Southeast Asia, and - Buddhism in China, East Asia, and Japan.";
            Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, analyzer);

            FieldType positionsType = new FieldType(TextField.TYPE_STORED);
            positionsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", text, positionsType);
            Document document = new Document();
            document.Add(body);
            iw.AddDocument(document);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher searcher = NewSearcher(ir);
            PhraseQuery query = new PhraseQuery();
            query.Add(new Term("body", "buddhist"));
            query.Add(new Term("body", "origins"));
            TopDocs topDocs = searcher.Search(query, 10);
            assertEquals(1, topDocs.TotalHits);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 2);
            assertEquals(1, snippets.Length);
            assertTrue(snippets[0].Contains("<b>Buddhist</b> <b>origins</b>"));
            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestCuriousGeorge()
        {
            String text = "It’s the formula for success for preschoolers—Curious George and fire trucks! " +
                          "Curious George and the Firefighters is a story based on H. A. and Margret Rey’s " +
                          "popular primate and painted in the original watercolor and charcoal style. " +
                          "Firefighters are a famously brave lot, but can they withstand a visit from one curious monkey?";
            Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, analyzer);
            FieldType positionsType = new FieldType(TextField.TYPE_STORED);
            positionsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", text, positionsType);
            Document document = new Document();
            document.Add(body);
            iw.AddDocument(document);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher searcher = NewSearcher(ir);
            PhraseQuery query = new PhraseQuery();
            query.Add(new Term("body", "curious"));
            query.Add(new Term("body", "george"));
            TopDocs topDocs = searcher.Search(query, 10);
            assertEquals(1, topDocs.TotalHits);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 2);
            assertEquals(1, snippets.Length);
            assertFalse(snippets[0].Contains("<b>Curious</b>Curious"));
            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestCambridgeMA()
        {
            String text;
            using (TextReader r = new StreamReader(this.GetType().getResourceAsStream("CambridgeMA.utf8"), Encoding.UTF8))
            {
                text = r.ReadLine();
            }

            Store.Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, analyzer);
            FieldType positionsType = new FieldType(TextField.TYPE_STORED);
            positionsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", text, positionsType);
            Document document = new Document();
            document.Add(body);
            iw.AddDocument(document);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher searcher = NewSearcher(ir);
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("body", "porter")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("body", "square")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("body", "massachusetts")), Occur.SHOULD);
            TopDocs topDocs = searcher.Search(query, 10);
            assertEquals(1, topDocs.TotalHits);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter(int.MaxValue - 1);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 2);
            assertEquals(1, snippets.Length);
            assertTrue(snippets[0].Contains("<b>Square</b>"));
            assertTrue(snippets[0].Contains("<b>Porter</b>"));
            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestPassageRanking()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test.  Just highlighting from postings. This is also a much sillier test.  Feel free to test test test test test test test.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "test"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 2);
            assertEquals(1, snippets.Length);
            assertEquals("This is a <b>test</b>.  ... Feel free to <b>test</b> <b>test</b> <b>test</b> <b>test</b> <b>test</b> <b>test</b> <b>test</b>.", snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestBooleanMustNot()
        {
            Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, analyzer);
            FieldType positionsType = new FieldType(TextField.TYPE_STORED);
            positionsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "This sentence has both terms.  This sentence has only terms.", positionsType);
            Document document = new Document();
            document.Add(body);
            iw.AddDocument(document);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher searcher = NewSearcher(ir);
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("body", "terms")), Occur.SHOULD);
            BooleanQuery query2 = new BooleanQuery();
            query.Add(query2, Occur.SHOULD);
            query2.Add(new TermQuery(new Term("body", "both")), Occur.MUST_NOT);
            TopDocs topDocs = searcher.Search(query, 10);
            assertEquals(1, topDocs.TotalHits);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter(int.MaxValue - 1);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 2);
            assertEquals(1, snippets.Length);
            assertFalse(snippets[0].Contains("<b>both</b>"));
            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestHighlightAllText()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test.  Just highlighting from postings. This is also a much sillier test.  Feel free to test test test test test test test.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new WholeBreakIteratorPostingsHighlighter(10000);
            Query query = new TermQuery(new Term("body", "test"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 2);
            assertEquals(1, snippets.Length);
            assertEquals("This is a <b>test</b>.  Just highlighting from postings. This is also a much sillier <b>test</b>.  Feel free to <b>test</b> <b>test</b> <b>test</b> <b>test</b> <b>test</b> <b>test</b> <b>test</b>.", snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        internal class WholeBreakIteratorPostingsHighlighter : ICUPostingsHighlighter
        {
            public WholeBreakIteratorPostingsHighlighter()
                : base()
            {
            }

            public WholeBreakIteratorPostingsHighlighter(int maxLength)
                : base(maxLength)
            {
            }

            protected override BreakIterator GetBreakIterator(string field)
            {
                return new WholeBreakIterator();
            }
        }

        [Test, LuceneNetSpecific]
        public void TestSpecificDocIDs()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test. Just a test highlighting from postings. Feel free to ignore.");
            iw.AddDocument(doc);
            body.SetStringValue("Highlighting the first term. Hope it works.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "highlighting"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(2, topDocs.TotalHits);
            ScoreDoc[] hits = topDocs.ScoreDocs;
            int[] docIDs = new int[2];
            docIDs[0] = hits[0].Doc;
            docIDs[1] = hits[1].Doc;
            String[] snippets = highlighter.HighlightFields(new String[] { "body" }, query, searcher, docIDs, new int[] { 1 })["body"];
            assertEquals(2, snippets.Length);
            assertEquals("Just a test <b>highlighting</b> from postings. ", snippets[0]);
            assertEquals("<b>Highlighting</b> the first term. ", snippets[1]);

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestCustomFieldValueSource()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            Document doc = new Document();

            FieldType offsetsType = new FieldType(TextField.TYPE_NOT_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            String text = "This is a test.  Just highlighting from postings. This is also a much sillier test.  Feel free to test test test test test test test.";
            Field body = new Field("body", text, offsetsType);
            doc.Add(body);
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new LoadFieldValuesPostingsHighlighter(10000, text);

            Query query = new TermQuery(new Term("body", "test"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs, 2);
            assertEquals(1, snippets.Length);
            assertEquals("This is a <b>test</b>.  Just highlighting from postings. This is also a much sillier <b>test</b>.  Feel free to <b>test</b> <b>test</b> <b>test</b> <b>test</b> <b>test</b> <b>test</b> <b>test</b>.", snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        internal class LoadFieldValuesPostingsHighlighter : WholeBreakIteratorPostingsHighlighter
        {
            private readonly string text;

            public LoadFieldValuesPostingsHighlighter(int maxLength, string text)
                : base(maxLength)
            {
                this.text = text;
            }

            protected override IList<string[]> LoadFieldValues(IndexSearcher searcher, string[] fields, int[] docids, int maxLength)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(fields.Length == 1);
                    Debugging.Assert(docids.Length == 1);
                }
                String[][] contents = RectangularArrays.ReturnRectangularArray<string>(1, 1); //= new String[1][1];
                contents[0][0] = text;
                return contents;
            }
        }

        /** Make sure highlighter returns first N sentences if
         *  there were no hits. */
        [Test, LuceneNetSpecific]
        public void TestEmptyHighlights()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Document doc = new Document();

            Field body = new Field("body", "test this is.  another sentence this test has.  far away is that planet.", offsetsType);
            doc.Add(body);
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "highlighting"));
            int[] docIDs = new int[] { 0 };
            String[] snippets = highlighter.HighlightFields(new String[] { "body" }, query, searcher, docIDs, new int[] { 2 })["body"];
            assertEquals(1, snippets.Length);
            assertEquals("test this is.  another sentence this test has.  far away is that planet.", snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        /** Make sure highlighter we can customize how emtpy
         *  highlight is returned. */
        [Test, LuceneNetSpecific]
        public void TestCustomEmptyHighlights()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Document doc = new Document();

            Field body = new Field("body", "test this is.  another sentence this test has.  far away is that planet.", offsetsType);
            doc.Add(body);
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new GetEmptyHighlightPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "highlighting"));
            int[] docIDs = new int[] { 0 };
            String[] snippets = highlighter.HighlightFields(new String[] { "body" }, query, searcher, docIDs, new int[] { 2 })["body"];
            assertEquals(1, snippets.Length);
            assertNull(snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        internal class GetEmptyHighlightPostingsHighlighter : ICUPostingsHighlighter
        {
            protected override Passage[] GetEmptyHighlight(string fieldName, BreakIterator bi, int maxPassages)
            {
                return new Passage[0];
            }
        }

        /** Make sure highlighter returns whole text when there
         *  are no hits and BreakIterator is null. */
        [Test, LuceneNetSpecific]
        public void TestEmptyHighlightsWhole()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Document doc = new Document();

            Field body = new Field("body", "test this is.  another sentence this test has.  far away is that planet.", offsetsType);
            doc.Add(body);
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new WholeBreakIteratorPostingsHighlighter(10000);
            Query query = new TermQuery(new Term("body", "highlighting"));
            int[] docIDs = new int[] { 0 };
            String[] snippets = highlighter.HighlightFields(new String[] { "body" }, query, searcher, docIDs, new int[] { 2 })["body"];
            assertEquals(1, snippets.Length);
            assertEquals("test this is.  another sentence this test has.  far away is that planet.", snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        /** Make sure highlighter is OK with entirely missing
         *  field. */
        [Test, LuceneNetSpecific]
        public void TestFieldIsMissing()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Document doc = new Document();

            Field body = new Field("body", "test this is.  another sentence this test has.  far away is that planet.", offsetsType);
            doc.Add(body);
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("bogus", "highlighting"));
            int[] docIDs = new int[] { 0 };
            String[] snippets = highlighter.HighlightFields(new String[] { "bogus" }, query, searcher, docIDs, new int[] { 2 })["bogus"];
            assertEquals(1, snippets.Length);
            assertNull(snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestFieldIsJustSpace()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);

            Document doc = new Document();
            doc.Add(new Field("body", "   ", offsetsType));
            doc.Add(new Field("id", "id", offsetsType));
            iw.AddDocument(doc);

            doc = new Document();
            doc.Add(new Field("body", "something", offsetsType));
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            int docID = searcher.Search(new TermQuery(new Term("id", "id")), 1).ScoreDocs[0].Doc;

            Query query = new TermQuery(new Term("body", "highlighting"));
            int[] docIDs = new int[1];
            docIDs[0] = docID;
            String[] snippets = highlighter.HighlightFields(new String[] { "body" }, query, searcher, docIDs, new int[] { 2 })["body"];
            assertEquals(1, snippets.Length);
            assertEquals("   ", snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestFieldIsEmptyString()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);

            Document doc = new Document();
            doc.Add(new Field("body", "", offsetsType));
            doc.Add(new Field("id", "id", offsetsType));
            iw.AddDocument(doc);

            doc = new Document();
            doc.Add(new Field("body", "something", offsetsType));
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            int docID = searcher.Search(new TermQuery(new Term("id", "id")), 1).ScoreDocs[0].Doc;

            Query query = new TermQuery(new Term("body", "highlighting"));
            int[] docIDs = new int[1];
            docIDs[0] = docID;
            String[] snippets = highlighter.HighlightFields(new String[] { "body" }, query, searcher, docIDs, new int[] { 2 })["body"];
            assertEquals(1, snippets.Length);
            assertNull(snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestMultipleDocs()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);

            int numDocs = AtLeast(100);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                String content = "the answer is " + i;
                if ((i & 1) == 0)
                {
                    content += " some more terms";
                }
                doc.Add(new Field("body", content, offsetsType));
                doc.Add(NewStringField("id", "" + i, Field.Store.YES));
                iw.AddDocument(doc);

                if (Random.nextInt(10) == 2)
                {
                    iw.Commit();
                }
            }

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "answer"));
            TopDocs hits = searcher.Search(query, numDocs);
            assertEquals(numDocs, hits.TotalHits);

            String[] snippets = highlighter.Highlight("body", query, searcher, hits);
            assertEquals(numDocs, snippets.Length);
            for (int hit = 0; hit < numDocs; hit++)
            {
                Document doc = searcher.Doc(hits.ScoreDocs[hit].Doc);
                int id = int.Parse(doc.Get("id"), CultureInfo.InvariantCulture);
                String expected = "the <b>answer</b> is " + id;
                if ((id & 1) == 0)
                {
                    expected += " some more terms";
                }
                assertEquals(expected, snippets[hit]);
            }

            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestMultipleSnippetSizes()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Field title = new Field("title", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);
            doc.Add(title);

            body.SetStringValue("This is a test. Just a test highlighting from postings. Feel free to ignore.");
            title.SetStringValue("This is a test. Just a test highlighting from postings. Feel free to ignore.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ICUPostingsHighlighter();
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("body", "test")), Occur.SHOULD);
            query.Add(new TermQuery(new Term("title", "test")), Occur.SHOULD);
            IDictionary<String, String[]> snippets = highlighter.HighlightFields(new String[] { "title", "body" }, query, searcher, new int[] { 0 }, new int[] { 1, 2 });
            String titleHighlight = snippets["title"][0];
            String bodyHighlight = snippets["body"][0];
            assertEquals("This is a <b>test</b>. ", titleHighlight);
            assertEquals("This is a <b>test</b>. Just a <b>test</b> highlighting from postings. ", bodyHighlight);
            ir.Dispose();
            dir.Dispose();
        }

        [Test, LuceneNetSpecific]
        public void TestEncode()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test. Just a test highlighting from <i>postings</i>. Feel free to ignore.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new GetFormatterPostingsHighlighter();
            Query query = new TermQuery(new Term("body", "highlighting"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs);
            assertEquals(1, snippets.Length);
            assertEquals("Just&#32;a&#32;test&#32;<b>highlighting</b>&#32;from&#32;&lt;i&gt;postings&lt;&#x2F;i&gt;&#46;&#32;", snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        internal class GetFormatterPostingsHighlighter : ICUPostingsHighlighter
        {
            protected override PassageFormatter GetFormatter(string field)
            {
                return new DefaultPassageFormatter("<b>", "</b>", "... ", true);
            }
        }

        /** customizing the gap separator to force a sentence break */
        [Test, LuceneNetSpecific]
        public void TestGapSeparator()
        {
            Directory dir = NewDirectory();
            // use simpleanalyzer for more natural tokenization (else "test." is a token)
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Document doc = new Document();

            Field body1 = new Field("body", "", offsetsType);
            body1.SetStringValue("This is a multivalued field");
            doc.Add(body1);

            Field body2 = new Field("body", "", offsetsType);
            body2.SetStringValue("This is something different");
            doc.Add(body2);

            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new GetMultiValuedSeparatorPostingsHighlighter();

            Query query = new TermQuery(new Term("body", "field"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            String[] snippets = highlighter.Highlight("body", query, searcher, topDocs);
            assertEquals(1, snippets.Length);
            assertEquals("This is a multivalued <b>field</b>\u2029", snippets[0]);

            ir.Dispose();
            dir.Dispose();
        }

        internal class GetMultiValuedSeparatorPostingsHighlighter : ICUPostingsHighlighter
        {
            protected override char GetMultiValuedSeparator(string field)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(field.Equals("body", StringComparison.Ordinal));
                return '\u2029';
            }
        }

        // LUCENE-4906
        [Test, LuceneNetSpecific]
        public void TestObjectFormatter()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
            offsetsType.IndexOptions = (IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
            Field body = new Field("body", "", offsetsType);
            Document doc = new Document();
            doc.Add(body);

            body.SetStringValue("This is a test. Just a test highlighting from postings. Feel free to ignore.");
            iw.AddDocument(doc);

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            ICUPostingsHighlighter highlighter = new ObjectFormatterPostingsHighlighter();

            Query query = new TermQuery(new Term("body", "highlighting"));
            TopDocs topDocs = searcher.Search(query, null, 10, Sort.INDEXORDER);
            assertEquals(1, topDocs.TotalHits);
            int[] docIDs = new int[1];
            docIDs[0] = topDocs.ScoreDocs[0].Doc;
            IDictionary<String, Object[]> snippets = highlighter.HighlightFieldsAsObjects(new String[] { "body" }, query, searcher, docIDs, new int[] { 1 });
            Object[] bodySnippets = snippets["body"];
            assertEquals(1, bodySnippets.Length);
            assertTrue(Arrays.Equals(new String[] { "blah blah", "Just a test <b>highlighting</b> from postings. " }, (String[])bodySnippets[0]));

            ir.Dispose();
            dir.Dispose();
        }

        internal class ObjectFormatterPostingsHighlighter : ICUPostingsHighlighter
        {
            protected override PassageFormatter GetFormatter(string field)
            {
                return new PassageFormatterHelper();
            }

            internal class PassageFormatterHelper : PassageFormatter
            {
                PassageFormatter defaultFormatter = new DefaultPassageFormatter();

                public override object Format(Passage[] passages, string content)
                {
                    // Just turns the String snippet into a length 2
                    // array of String
                    return new String[] { "blah blah", defaultFormatter.Format(passages, content).toString() };
                }
            }
        }
    }
}
#endif