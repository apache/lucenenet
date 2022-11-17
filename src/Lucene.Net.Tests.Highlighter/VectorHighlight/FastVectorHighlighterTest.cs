using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.VectorHighlight
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

    public class FastVectorHighlighterTest : LuceneTestCase
    {
        [Test]
        public void TestSimpleHighlightTest()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType type = new FieldType(TextField.TYPE_STORED);
            type.StoreTermVectorOffsets = (true);
            type.StoreTermVectorPositions = (true);
            type.StoreTermVectors = (true);
            type.Freeze();
            Field field = new Field("field", "This is a test where foo is highlighed and should be highlighted", type);

            doc.Add(field);
            writer.AddDocument(doc);
            FastVectorHighlighter highlighter = new FastVectorHighlighter();

            IndexReader reader = DirectoryReader.Open(writer, true);
            int docId = 0;
            FieldQuery fieldQuery = highlighter.GetFieldQuery(new TermQuery(new Term("field", "foo")), reader);
            String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 54, 1);
            // highlighted results are centered 
            assertEquals("This is a test where <b>foo</b> is highlighed and should be highlighted", bestFragments[0]);
            bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 52, 1);
            assertEquals("This is a test where <b>foo</b> is highlighed and should be", bestFragments[0]);
            bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 30, 1);
            assertEquals("a test where <b>foo</b> is highlighed", bestFragments[0]);
            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestPhraseHighlightLongTextTest()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType type = new FieldType(TextField.TYPE_STORED);
            type.StoreTermVectorOffsets = (true);
            type.StoreTermVectorPositions = (true);
            type.StoreTermVectors = (true);
            type.Freeze();
            Field text = new Field("text",
                "Netscape was the general name for a series of web browsers originally produced by Netscape Communications Corporation, now a subsidiary of AOL The original browser was once the dominant browser in terms of usage share, but as a result of the first browser war it lost virtually all of its share to Internet Explorer Netscape was discontinued and support for all Netscape browsers and client products was terminated on March 1, 2008 Netscape Navigator was the name of Netscape\u0027s web browser from versions 1.0 through 4.8 The first beta release versions of the browser were released in 1994 and known as Mosaic and then Mosaic Netscape until a legal challenge from the National Center for Supercomputing Applications (makers of NCSA Mosaic, which many of Netscape\u0027s founders used to develop), led to the name change to Netscape Navigator The company\u0027s name also changed from Mosaic Communications Corporation to Netscape Communications Corporation The browser was easily the most advanced...", type);
            doc.Add(text);
            writer.AddDocument(doc);
            FastVectorHighlighter highlighter = new FastVectorHighlighter();
            IndexReader reader = DirectoryReader.Open(writer, true);
            int docId = 0;
            String field = "text";
            {
                BooleanQuery query = new BooleanQuery();
                query.Add(new TermQuery(new Term(field, "internet")), Occur.MUST);
                query.Add(new TermQuery(new Term(field, "explorer")), Occur.MUST);
                FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);
                String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                    docId, field, 128, 1);
                // highlighted results are centered
                assertEquals(1, bestFragments.Length);
                assertEquals("first browser war it lost virtually all of its share to <b>Internet</b> <b>Explorer</b> Netscape was discontinued and support for all Netscape browsers", bestFragments[0]);
            }

            {
                PhraseQuery query = new PhraseQuery();
                query.Add(new Term(field, "internet"));
                query.Add(new Term(field, "explorer"));
                FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);
                String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                    docId, field, 128, 1);
                // highlighted results are centered
                assertEquals(1, bestFragments.Length);
                assertEquals("first browser war it lost virtually all of its share to <b>Internet Explorer</b> Netscape was discontinued and support for all Netscape browsers", bestFragments[0]);
            }
            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        // see LUCENE-4899
        [Test]
        public void TestPhraseHighlightTest()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType type = new FieldType(TextField.TYPE_STORED);
            type.StoreTermVectorOffsets = (true);
            type.StoreTermVectorPositions = (true);
            type.StoreTermVectors = (true);
            type.Freeze();
            Field longTermField = new Field("long_term", "This is a test thisisaverylongwordandmakessurethisfails where foo is highlighed and should be highlighted", type);
            Field noLongTermField = new Field("no_long_term", "This is a test where foo is highlighed and should be highlighted", type);

            doc.Add(longTermField);
            doc.Add(noLongTermField);
            writer.AddDocument(doc);
            FastVectorHighlighter highlighter = new FastVectorHighlighter();
            IndexReader reader = DirectoryReader.Open(writer, true);
            int docId = 0;
            String field = "no_long_term";
            {
                BooleanQuery query = new BooleanQuery();
                query.Add(new TermQuery(new Term(field, "test")), Occur.MUST);
                query.Add(new TermQuery(new Term(field, "foo")), Occur.MUST);
                query.Add(new TermQuery(new Term(field, "highlighed")), Occur.MUST);
                FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);
                String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                    docId, field, 18, 1);
                // highlighted results are centered
                assertEquals(1, bestFragments.Length);
                assertEquals("<b>foo</b> is <b>highlighed</b> and", bestFragments[0]);
            }
            {
                BooleanQuery query = new BooleanQuery();
                PhraseQuery pq = new PhraseQuery();
                pq.Add(new Term(field, "test"));
                pq.Add(new Term(field, "foo"));
                pq.Add(new Term(field, "highlighed"));
                pq.Slop = (5);
                query.Add(new TermQuery(new Term(field, "foo")), Occur.MUST);
                query.Add(pq, Occur.MUST);
                query.Add(new TermQuery(new Term(field, "highlighed")), Occur.MUST);
                FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);
                String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                    docId, field, 18, 1);
                // highlighted results are centered
                assertEquals(0, bestFragments.Length);
                bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                          docId, field, 30, 1);
                // highlighted results are centered
                assertEquals(1, bestFragments.Length);
                assertEquals("a <b>test</b> where <b>foo</b> is <b>highlighed</b> and", bestFragments[0]);

            }
            {
                PhraseQuery query = new PhraseQuery();
                query.Add(new Term(field, "test"));
                query.Add(new Term(field, "foo"));
                query.Add(new Term(field, "highlighed"));
                query.Slop = (3);
                FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);
                String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                    docId, field, 18, 1);
                // highlighted results are centered
                assertEquals(0, bestFragments.Length);
                bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                          docId, field, 30, 1);
                // highlighted results are centered
                assertEquals(1, bestFragments.Length);
                assertEquals("a <b>test</b> where <b>foo</b> is <b>highlighed</b> and", bestFragments[0]);

            }
            {
                PhraseQuery query = new PhraseQuery();
                query.Add(new Term(field, "test"));
                query.Add(new Term(field, "foo"));
                query.Add(new Term(field, "highlighted"));
                query.Slop = (30);
                FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);
                String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                    docId, field, 18, 1);
                assertEquals(0, bestFragments.Length);
            }
            {
                BooleanQuery query = new BooleanQuery();
                PhraseQuery pq = new PhraseQuery();
                pq.Add(new Term(field, "test"));
                pq.Add(new Term(field, "foo"));
                pq.Add(new Term(field, "highlighed"));
                pq.Slop = (5);
                BooleanQuery inner = new BooleanQuery();
                inner.Add(pq, Occur.MUST);
                inner.Add(new TermQuery(new Term(field, "foo")), Occur.MUST);
                query.Add(inner, Occur.MUST);
                query.Add(pq, Occur.MUST);
                query.Add(new TermQuery(new Term(field, "highlighed")), Occur.MUST);
                FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);
                String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                    docId, field, 18, 1);
                assertEquals(0, bestFragments.Length);

                bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                          docId, field, 30, 1);
                // highlighted results are centered
                assertEquals(1, bestFragments.Length);
                assertEquals("a <b>test</b> where <b>foo</b> is <b>highlighed</b> and", bestFragments[0]);
            }

            field = "long_term";
            {
                BooleanQuery query = new BooleanQuery();
                query.Add(new TermQuery(new Term(field,
                          "thisisaverylongwordandmakessurethisfails")), Occur.MUST);
                query.Add(new TermQuery(new Term(field, "foo")), Occur.MUST);
                query.Add(new TermQuery(new Term(field, "highlighed")), Occur.MUST);
                FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);
                String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader,
                    docId, field, 18, 1);
                // highlighted results are centered
                assertEquals(1, bestFragments.Length);
                assertEquals("<b>thisisaverylongwordandmakessurethisfails</b>",
                    bestFragments[0]);
            }
            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestBoostedPhraseHighlightTest()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType type = new FieldType(TextField.TYPE_STORED);
            type.StoreTermVectorOffsets = (true);
            type.StoreTermVectorPositions = (true);
            type.StoreTermVectors = (true);
            type.Freeze();
            StringBuilder text = new StringBuilder();
            text.append("words words junk junk junk junk junk junk junk junk highlight junk junk junk junk together junk ");
            for (int i = 0; i < 10; i++)
            {
                text.append("junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk ");
            }
            text.append("highlight words together ");
            for (int i = 0; i < 10; i++)
            {
                text.append("junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk junk ");
            }
            doc.Add(new Field("text", text.toString().Trim(), type));
            writer.AddDocument(doc);
            FastVectorHighlighter highlighter = new FastVectorHighlighter();
            IndexReader reader = DirectoryReader.Open(writer, true);

            // This mimics what some query parsers do to <highlight words together>
            BooleanQuery terms = new BooleanQuery();
            terms.Add(clause("text", "highlight"), Occur.MUST);
            terms.Add(clause("text", "words"), Occur.MUST);
            terms.Add(clause("text", "together"), Occur.MUST);
            // This mimics what some query parsers do to <"highlight words together">
            BooleanQuery phrase = new BooleanQuery();
            phrase.Add(clause("text", "highlight", "words", "together"), Occur.MUST);
            phrase.Boost = (100);
            // Now combine those results in a boolean query which should pull the phrases to the front of the list of fragments 
            BooleanQuery query = new BooleanQuery();
            query.Add(phrase, Occur.MUST);
            query.Add(phrase, Occur.SHOULD);
            FieldQuery fieldQuery = new FieldQuery(query, reader, true, false);
            String fragment = highlighter.GetBestFragment(fieldQuery, reader, 0, "text", 100);
            assertEquals("junk junk junk junk junk junk junk junk <b>highlight words together</b> junk junk junk junk junk junk junk junk", fragment);

            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestCommonTermsQueryHighlight()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET)));
            FieldType type = new FieldType(TextField.TYPE_STORED);
            type.StoreTermVectorOffsets = (true);
            type.StoreTermVectorPositions = (true);
            type.StoreTermVectors = (true);
            type.Freeze();
            String[] texts = {
                "Hello this is a piece of text that is very long and contains too much preamble and the meat is really here which says kennedy has been shot",
                "This piece of text refers to Kennedy at the beginning then has a longer piece of text that is very long in the middle and finally ends with another reference to Kennedy",
                "JFK has been shot", "John Kennedy has been shot",
                "This text has a typo in referring to Keneddy",
                "wordx wordy wordz wordx wordy wordx worda wordb wordy wordc", "y z x y z a b", "lets is a the lets is a the lets is a the lets" };
            for (int i = 0; i < texts.Length; i++)
            {
                Document doc = new Document();
                Field field = new Field("field", texts[i], type);
                doc.Add(field);
                writer.AddDocument(doc);
            }
            CommonTermsQuery query = new CommonTermsQuery(Occur.MUST, Occur.SHOULD, 2);
            query.Add(new Term("field", "text"));
            query.Add(new Term("field", "long"));
            query.Add(new Term("field", "very"));

            FastVectorHighlighter highlighter = new FastVectorHighlighter();
            IndexReader reader = DirectoryReader.Open(writer, true);
            IndexSearcher searcher = NewSearcher(reader);
            TopDocs hits = searcher.Search(query, 10);
            assertEquals(2, hits.TotalHits);
            FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);
            String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader, hits.ScoreDocs[0].Doc, "field", 1000, 1);
            assertEquals("This piece of <b>text</b> refers to Kennedy at the beginning then has a longer piece of <b>text</b> that is <b>very</b> <b>long</b> in the middle and finally ends with another reference to Kennedy", bestFragments[0]);

            fieldQuery = highlighter.GetFieldQuery(query, reader);
            bestFragments = highlighter.GetBestFragments(fieldQuery, reader, hits.ScoreDocs[1].Doc, "field", 1000, 1);
            assertEquals("Hello this is a piece of <b>text</b> that is <b>very</b> <b>long</b> and contains too much preamble and the meat is really here which says kennedy has been shot", bestFragments[0]);

            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }
        [Test]
        public void TestMatchedFields()
        {
            // Searching just on the stored field doesn't highlight a stopword
            matchedFieldsTestCase(false, true, "a match", "a <b>match</b>",
              clause("field", "a"), clause("field", "match"));

            // Even if you add an unqueried matched field that would match it
            matchedFieldsTestCase("a match", "a <b>match</b>",
              clause("field", "a"), clause("field", "match"));

            // Nor if you query the field but don't add it as a matched field to the highlighter
            matchedFieldsTestCase(false, false, "a match", "a <b>match</b>",
              clause("field_exact", "a"), clause("field", "match"));

            // But if you query the field and add it as a matched field to the highlighter then it is highlighted
            matchedFieldsTestCase("a match", "<b>a</b> <b>match</b>",
              clause("field_exact", "a"), clause("field", "match"));

            // It is also ok to match just the matched field but get highlighting from the stored field
            matchedFieldsTestCase("a match", "<b>a</b> <b>match</b>",
              clause("field_exact", "a"), clause("field_exact", "match"));

            // Boosted matched fields work too
            matchedFieldsTestCase("a match", "<b>a</b> <b>match</b>",
              clause("field_exact", 5, "a"), clause("field", "match"));

            // It is also ok if both the stored and the matched field match the term
            matchedFieldsTestCase("a match", "a <b>match</b>",
              clause("field_exact", "match"), clause("field", "match"));

            // And the highlighter respects the boosts on matched fields when sorting fragments
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "junk junk <b>a cat</b> junk junk",
              clause("field", "cat"), clause("field_exact", 5, "a", "cat"));
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "<b>cat</b> <b>cat</b> junk junk junk junk",
              clause("field", "cat"), clause("field_exact", "a", "cat"));

            // The same thing works across three fields as well
            matchedFieldsTestCase("cat cat CAT junk junk junk junk junk junk junk a cat junk junk",
              "junk junk <b>a cat</b> junk junk",
              clause("field", "cat"), clause("field_exact", 200, "a", "cat"), clause("field_super_exact", 5, "CAT"));
            matchedFieldsTestCase("a cat cat junk junk junk junk junk junk junk a CAT junk junk",
              "junk junk <b>a CAT</b> junk junk",
              clause("field", "cat"), clause("field_exact", 5, "a", "cat"), clause("field_super_exact", 200, "a", "CAT"));

            // And across fields with different tokenizers!
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "junk junk <b>a cat</b> junk junk",
              clause("field_exact", 5, "a", "cat"), clause("field_characters", "c"));
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "<b>c</b>at <b>c</b>at junk junk junk junk",
              clause("field_exact", "a", "cat"), clause("field_characters", "c"));
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "ca<b>t</b> ca<b>t</b> junk junk junk junk",
              clause("field_exact", "a", "cat"), clause("field_characters", "t"));
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "<b>cat</b> <b>cat</b> junk junk junk junk", // See how the phrases are joined?
              clause("field", "cat"), clause("field_characters", 5, "c"));
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "junk junk <b>a cat</b> junk junk",
              clause("field", "cat"), clause("field_characters", 5, "a", " ", "c", "a", "t"));

            // Phrases and tokens inside one another are joined
            matchedFieldsTestCase("cats wow", "<b>cats w</b>ow",
              clause("field", "cats"), clause("field_tripples", "s w"));

            // Everything works pretty well even if you don't require a field match
            matchedFieldsTestCase(true, false, "cat cat junk junk junk junk junk junk junk a cat junk junk",
              "junk junk <b>a cat</b> junk junk",
              clause("field", "cat"), clause("field_characters", 10, "a", " ", "c", "a", "t"));

            // Even boosts keep themselves pretty much intact
            matchedFieldsTestCase(true, false, "a cat cat junk junk junk junk junk junk junk a CAT junk junk",
              "junk junk <b>a CAT</b> junk junk",
              clause("field", "cat"), clause("field_exact", 5, "a", "cat"), clause("field_super_exact", 200, "a", "CAT"));
            matchedFieldsTestCase(true, false, "cat cat CAT junk junk junk junk junk junk junk a cat junk junk",
              "junk junk <b>a cat</b> junk junk",
              clause("field", "cat"), clause("field_exact", 200, "a", "cat"), clause("field_super_exact", 5, "CAT"));

            // Except that all the matched field matches apply even if they aren't mentioned in the query
            // which can make for some confusing scoring.  This isn't too big a deal, just something you
            // need to think about when you don't force a field match.
            matchedFieldsTestCase(true, false, "cat cat junk junk junk junk junk junk junk a cat junk junk",
              "<b>cat</b> <b>cat</b> junk junk junk junk",
              clause("field", "cat"), clause("field_characters", 4, "a", " ", "c", "a", "t"));

            // It is also cool to match fields that don't have _exactly_ the same text so long as you are careful.
            // In this case field_sliced is a prefix of field.
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "<b>cat</b> <b>cat</b> junk junk junk junk", clause("field_sliced", "cat"));

            // Multiple matches add to the score of the segment
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "<b>cat</b> <b>cat</b> junk junk junk junk",
              clause("field", "cat"), clause("field_sliced", "cat"), clause("field_exact", 2, "a", "cat"));
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "junk junk <b>a cat</b> junk junk",
              clause("field", "cat"), clause("field_sliced", "cat"), clause("field_exact", 4, "a", "cat"));

            // Even fields with tokens on top of one another are ok
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "<b>cat</b> cat junk junk junk junk",
              clause("field_der_red", 2, "der"), clause("field_exact", "a", "cat"));
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "<b>cat</b> cat junk junk junk junk",
              clause("field_der_red", 2, "red"), clause("field_exact", "a", "cat"));
            matchedFieldsTestCase("cat cat junk junk junk junk junk junk junk a cat junk junk",
              "<b>cat</b> cat junk junk junk junk",
              clause("field_der_red", "red"), clause("field_der_red", "der"), clause("field_exact", "a", "cat"));
        }

        [Test]
        public void TestMultiValuedSortByScore()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType type = new FieldType(TextField.TYPE_STORED);
            type.StoreTermVectorOffsets = (true);
            type.StoreTermVectorPositions = (true);
            type.StoreTermVectors = (true);
            type.Freeze();
            doc.Add(new Field("field", "zero if naught", type)); // The first two fields contain the best match
            doc.Add(new Field("field", "hero of legend", type)); // but total a lower score (3) than the bottom
            doc.Add(new Field("field", "naught of hero", type)); // two fields (4)
            doc.Add(new Field("field", "naught of hero", type));
            writer.AddDocument(doc);

            FastVectorHighlighter highlighter = new FastVectorHighlighter();

            ScoreOrderFragmentsBuilder fragmentsBuilder = new ScoreOrderFragmentsBuilder();
            fragmentsBuilder.IsDiscreteMultiValueHighlighting = (true);
            IndexReader reader = DirectoryReader.Open(writer, true);
            String[] preTags = new String[] { "<b>" };
            String[] postTags = new String[] { "</b>" };
            IEncoder encoder = new DefaultEncoder();
            int docId = 0;
            BooleanQuery query = new BooleanQuery();
            query.Add(clause("field", "hero"), Occur.SHOULD);
            query.Add(clause("field", "of"), Occur.SHOULD);
            query.Add(clause("field", "legend"), Occur.SHOULD);
            FieldQuery fieldQuery = highlighter.GetFieldQuery(query, reader);

            foreach (IFragListBuilder fragListBuilder in new IFragListBuilder[] {
      new SimpleFragListBuilder(), new WeightedFragListBuilder() })
            {
                String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 20, 1,
                    fragListBuilder, fragmentsBuilder, preTags, postTags, encoder);
                assertEquals("<b>hero</b> <b>of</b> <b>legend</b>", bestFragments[0]);
                bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 28, 1,
                          fragListBuilder, fragmentsBuilder, preTags, postTags, encoder);
                assertEquals("<b>hero</b> <b>of</b> <b>legend</b>", bestFragments[0]);
                bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 30000, 1,
                          fragListBuilder, fragmentsBuilder, preTags, postTags, encoder);
                assertEquals("<b>hero</b> <b>of</b> <b>legend</b>", bestFragments[0]);
            }

            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestBooleanPhraseWithSynonym()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document doc = new Document();
            FieldType type = new FieldType(TextField.TYPE_NOT_STORED);
            type.StoreTermVectorOffsets = (true);
            type.StoreTermVectorPositions = (true);
            type.StoreTermVectors = (true);
            type.Freeze();
            Token syn = new Token("httpwwwfacebookcom", 6, 29);
            syn.PositionIncrement = (0);
            CannedTokenStream ts = new CannedTokenStream(
                new Token("test", 0, 4),
                new Token("http", 6, 10),
                syn,
                new Token("www", 13, 16),
                new Token("facebook", 17, 25),
                new Token("com", 26, 29)
            );
            Field field = new Field("field", ts, type);
            doc.Add(field);
            doc.Add(new StoredField("field", "Test: http://www.facebook.com"));
            writer.AddDocument(doc);
            FastVectorHighlighter highlighter = new FastVectorHighlighter();

            IndexReader reader = DirectoryReader.Open(writer, true);
            int docId = 0;

            // query1: match
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("field", "test"));
            pq.Add(new Term("field", "http"));
            pq.Add(new Term("field", "www"));
            pq.Add(new Term("field", "facebook"));
            pq.Add(new Term("field", "com"));
            FieldQuery fieldQuery = highlighter.GetFieldQuery(pq, reader);
            String[] bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 54, 1);
            assertEquals("<b>Test: http://www.facebook.com</b>", bestFragments[0]);

            // query2: match
            PhraseQuery pq2 = new PhraseQuery();
            pq2.Add(new Term("field", "test"));
            pq2.Add(new Term("field", "httpwwwfacebookcom"));
            pq2.Add(new Term("field", "www"));
            pq2.Add(new Term("field", "facebook"));
            pq2.Add(new Term("field", "com"));
            fieldQuery = highlighter.GetFieldQuery(pq2, reader);
            bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 54, 1);
            assertEquals("<b>Test: http://www.facebook.com</b>", bestFragments[0]);

            // query3: OR query1 and query2 together
            BooleanQuery bq = new BooleanQuery();
            bq.Add(pq, Occur.SHOULD);
            bq.Add(pq2, Occur.SHOULD);
            fieldQuery = highlighter.GetFieldQuery(bq, reader);
            bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 54, 1);
            assertEquals("<b>Test: http://www.facebook.com</b>", bestFragments[0]);

            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        private void matchedFieldsTestCase(String fieldValue, String expected, params Query[] queryClauses)
        {
            matchedFieldsTestCase(true, true, fieldValue, expected, queryClauses);
        }

        private void matchedFieldsTestCase(bool useMatchedFields, bool fieldMatch, String fieldValue, String expected, params Query[] queryClauses)
        {
            Document doc = new Document();
            FieldType stored = new FieldType(TextField.TYPE_STORED);
            stored.StoreTermVectorOffsets = (true);
            stored.StoreTermVectorPositions = (true);
            stored.StoreTermVectors = (true);
            stored.Freeze();
            FieldType matched = new FieldType(TextField.TYPE_NOT_STORED);
            matched.StoreTermVectorOffsets = (true);
            matched.StoreTermVectorPositions = (true);
            matched.StoreTermVectors = (true);
            matched.Freeze();
            doc.Add(new Field("field", fieldValue, stored));               // Whitespace tokenized with English stop words
            doc.Add(new Field("field_exact", fieldValue, matched));        // Whitespace tokenized without stop words
            doc.Add(new Field("field_super_exact", fieldValue, matched));  // Whitespace tokenized without toLower
            doc.Add(new Field("field_characters", fieldValue, matched));   // Each letter is a token
            doc.Add(new Field("field_tripples", fieldValue, matched));     // Every three letters is a token
            doc.Add(new Field("field_sliced", fieldValue.Substring(0,       // Sliced at 10 chars then analyzed just like field
              Math.Min(fieldValue.Length - 1, 10) - 0), matched));
            doc.Add(new Field("field_der_red", new CannedTokenStream(        // Hacky field containing "der" and "red" at pos = 0
                  token("der", 1, 0, 3),
                  token("red", 0, 0, 3)
                ), matched));

            Analyzer analyzer = new AnalyzerWrapperAnonymousClass();

            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            writer.AddDocument(doc);

            FastVectorHighlighter highlighter = new FastVectorHighlighter();
            IFragListBuilder fragListBuilder = new SimpleFragListBuilder();
            IFragmentsBuilder fragmentsBuilder = new ScoreOrderFragmentsBuilder();
            IndexReader reader = DirectoryReader.Open(writer, true);
            String[] preTags = new String[] { "<b>" };
            String[] postTags = new String[] { "</b>" };
            IEncoder encoder = new DefaultEncoder();
            int docId = 0;
            BooleanQuery query = new BooleanQuery();
            foreach (Query clause in queryClauses)
            {
                query.Add(clause, Occur.MUST);
            }
            FieldQuery fieldQuery = new FieldQuery(query, reader, true, fieldMatch);
            String[] bestFragments;
            if (useMatchedFields)
            {
                ISet<String> matchedFields = new JCG.HashSet<String>();
                matchedFields.Add("field");
                matchedFields.Add("field_exact");
                matchedFields.Add("field_super_exact");
                matchedFields.Add("field_characters");
                matchedFields.Add("field_tripples");
                matchedFields.Add("field_sliced");
                matchedFields.Add("field_der_red");
                bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", matchedFields, 25, 1,
                  fragListBuilder, fragmentsBuilder, preTags, postTags, encoder);
            }
            else
            {
                bestFragments = highlighter.GetBestFragments(fieldQuery, reader, docId, "field", 25, 1,
                  fragListBuilder, fragmentsBuilder, preTags, postTags, encoder);
            }
            assertEquals(expected, bestFragments[0]);

            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        private sealed class AnalyzerWrapperAnonymousClass : AnalyzerWrapper
        {
            IDictionary<String, Analyzer> fieldAnalyzers = new JCG.SortedDictionary<String, Analyzer>(StringComparer.Ordinal);

#pragma warning disable 612, 618 // LUCENENET NOTE: Class calls obsolete (default) constructor
            public AnalyzerWrapperAnonymousClass()
            {
                fieldAnalyzers["field"] = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, true, MockTokenFilter.ENGLISH_STOPSET);
                fieldAnalyzers["field_exact"] = new MockAnalyzer(Random);
                fieldAnalyzers["field_super_exact"] = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
                fieldAnalyzers["field_characters"] = new MockAnalyzer(Random, new CharacterRunAutomaton(new RegExp(".").ToAutomaton()), true);
                fieldAnalyzers["field_tripples"] = new MockAnalyzer(Random, new CharacterRunAutomaton(new RegExp("...").ToAutomaton()), true);
                fieldAnalyzers["field_sliced"] = fieldAnalyzers["field"];
                fieldAnalyzers["field_der_red"] = fieldAnalyzers["field"];  // This is required even though we provide a token stream
            }
#pragma warning restore 612, 618
            protected override Analyzer GetWrappedAnalyzer(string fieldName)
            {
                return fieldAnalyzers[fieldName];
            }
        }

        private Query clause(String field, params String[] terms)
        {
            return clause(field, 1, terms);
        }

        private Query clause(String field, float boost, params String[] terms)
        {
            Query q;
            if (terms.Length == 1)
            {
                q = new TermQuery(new Term(field, terms[0]));
            }
            else
            {
                PhraseQuery pq = new PhraseQuery();
                foreach (String term in terms)
                {
                    pq.Add(new Term(field, term));
                }
                q = pq;
            }
            q.Boost = (boost);
            return q;
        }

        private static Token token(String term, int posInc, int startOffset, int endOffset)
        {
            Token t = new Token(term, startOffset, endOffset);
            t.PositionIncrement = (posInc);
            return t;
        }
    }
}
