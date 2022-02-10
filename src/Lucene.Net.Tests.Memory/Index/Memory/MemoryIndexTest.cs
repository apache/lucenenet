/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Index.Memory
{
    public class MemoryIndexTest : BaseTokenStreamTestCase
    {
        private ISet<string> queries = new JCG.HashSet<string>();

        public static readonly int ITERATIONS = 100 * RandomMultiplier;


        public override void SetUp()
        {
            base.SetUp();
            queries.addAll(ReadQueries("testqueries.txt"));
            queries.addAll(ReadQueries("testqueries2.txt"));
        }

        /**
         * read a set of queries from a resource file
         */
        private ISet<string> ReadQueries(string resource)
        {
            ISet<string> queries = new JCG.HashSet<string>();
            Stream stream = GetType().getResourceAsStream(resource);
            TextReader reader = new StreamReader(stream, Encoding.UTF8);
            String line = null;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal) && !line.StartsWith("//", StringComparison.Ordinal))
                {
                    queries.add(line);
                }
            }
            return queries;
        }


        /**
         * runs random tests, up to ITERATIONS times.
         */
        [Test]
        [Slow]
        public void TestRandomQueries()
        {
            MemoryIndex index = new MemoryIndex(Random.nextBoolean(), Random.nextInt(50) * 1024 * 1024);
            for (int i = 0; i < ITERATIONS; i++)
            {
                AssertAgainstRAMDirectory(index);
            }
        }

        /**
         * Build a randomish document for both RAMDirectory and MemoryIndex,
         * and run all the queries against it.
         */
        public void AssertAgainstRAMDirectory(MemoryIndex memory)
        {
            memory.Reset();
            StringBuilder fooField = new StringBuilder();
            StringBuilder termField = new StringBuilder();

            // add up to 250 terms to field "foo"
            int numFooTerms = Random.nextInt(250 * RandomMultiplier);
            for (int i = 0; i < numFooTerms; i++)
            {
                fooField.append(" ");
                fooField.append(RandomTerm());
            }

            // add up to 250 terms to field "term"
            int numTermTerms = Random.nextInt(250 * RandomMultiplier);
            for (int i = 0; i < numTermTerms; i++)
            {
                termField.append(" ");
                termField.append(RandomTerm());
            }

            Store.Directory ramdir = new RAMDirectory();
            Analyzer analyzer = RandomAnalyzer();
            IndexWriter writer = new IndexWriter(ramdir,
                                                 new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat())));
            Document doc = new Document();
            Field field1 = NewTextField("foo", fooField.toString(), Field.Store.NO);
            Field field2 = NewTextField("term", termField.toString(), Field.Store.NO);
            doc.Add(field1);
            doc.Add(field2);
            writer.AddDocument(doc);
            writer.Dispose();

            memory.AddField("foo", fooField.toString(), analyzer);
            memory.AddField("term", termField.toString(), analyzer);

            if (Verbose)
            {
                Console.WriteLine("Random MemoryIndex:\n" + memory.toString());
                Console.WriteLine("Same index as RAMDirectory: " +
                  RamUsageEstimator.HumanReadableUnits(RamUsageEstimator.SizeOf(ramdir)));
                Console.WriteLine();
            }
            else
            {
                assertTrue(memory.GetMemorySize() > 0L);
            }
            AtomicReader reader = (AtomicReader)memory.CreateSearcher().IndexReader;
            DirectoryReader competitor = DirectoryReader.Open(ramdir);
            DuellReaders(competitor, reader);
            IOUtils.Dispose(reader, competitor);
            AssertAllQueries(memory, ramdir, analyzer);
            ramdir.Dispose();
        }

        private void DuellReaders(CompositeReader other, AtomicReader memIndexReader)
        {
            AtomicReader competitor = SlowCompositeReaderWrapper.Wrap(other);
            Fields memFields = memIndexReader.Fields;
            foreach (string field in competitor.Fields)
            {
                Terms memTerms = memFields.GetTerms(field);
                Terms iwTerms = memIndexReader.GetTerms(field);
                if (iwTerms is null)
                {
                    assertNull(memTerms);
                }
                else
                {
                    NumericDocValues normValues = competitor.GetNormValues(field);
                    NumericDocValues memNormValues = memIndexReader.GetNormValues(field);
                    if (normValues != null)
                    {
                        // mem idx always computes norms on the fly
                        assertNotNull(memNormValues);
                        assertEquals(normValues.Get(0), memNormValues.Get(0));
                    }

                    assertNotNull(memTerms);
                    assertEquals(iwTerms.DocCount, memTerms.DocCount);
                    assertEquals(iwTerms.SumDocFreq, memTerms.SumDocFreq);
                    assertEquals(iwTerms.SumTotalTermFreq, memTerms.SumTotalTermFreq);
                    TermsEnum iwTermsIter = iwTerms.GetEnumerator();
                    TermsEnum memTermsIter = memTerms.GetEnumerator();
                    if (iwTerms.HasPositions)
                    {
                        bool offsets = iwTerms.HasOffsets && memTerms.HasOffsets;

                        while (iwTermsIter.MoveNext())
                        {
                            assertTrue(memTermsIter.MoveNext());
                            assertEquals(iwTermsIter.Term, memTermsIter.Term);
                            DocsAndPositionsEnum iwDocsAndPos = iwTermsIter.DocsAndPositions(null, null);
                            DocsAndPositionsEnum memDocsAndPos = memTermsIter.DocsAndPositions(null, null);
                            while (iwDocsAndPos.NextDoc() != DocsAndPositionsEnum.NO_MORE_DOCS)
                            {
                                assertEquals(iwDocsAndPos.DocID, memDocsAndPos.NextDoc());
                                assertEquals(iwDocsAndPos.Freq, memDocsAndPos.Freq);
                                for (int i = 0; i < iwDocsAndPos.Freq; i++)
                                {
                                    assertEquals("term: " + iwTermsIter.Term.Utf8ToString(), iwDocsAndPos.NextPosition(), memDocsAndPos.NextPosition());
                                    if (offsets)
                                    {
                                        assertEquals(iwDocsAndPos.StartOffset, memDocsAndPos.StartOffset);
                                        assertEquals(iwDocsAndPos.EndOffset, memDocsAndPos.EndOffset);
                                    }
                                }

                            }

                        }
                    }
                    else
                    {
                        while (iwTermsIter.MoveNext())
                        {
                            assertEquals(iwTermsIter.Term, memTermsIter.Term);
                            DocsEnum iwDocsAndPos = iwTermsIter.Docs(null, null);
                            DocsEnum memDocsAndPos = memTermsIter.Docs(null, null);
                            while (iwDocsAndPos.NextDoc() != DocsAndPositionsEnum.NO_MORE_DOCS)
                            {
                                assertEquals(iwDocsAndPos.DocID, memDocsAndPos.NextDoc());
                                assertEquals(iwDocsAndPos.Freq, memDocsAndPos.Freq);
                            }
                        }
                    }
                }

            }
        }

        /**
         * Run all queries against both the RAMDirectory and MemoryIndex, ensuring they are the same.
         */
        public void AssertAllQueries(MemoryIndex memory, Store.Directory ramdir, Analyzer analyzer)
        {
            IndexReader reader = DirectoryReader.Open(ramdir);
            IndexSearcher ram = NewSearcher(reader);
            IndexSearcher mem = memory.CreateSearcher();
            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "foo", analyzer)
            {
                // LUCENENET specific - to avoid random failures, set the culture
                // of the QueryParser to invariant
                Locale = CultureInfo.InvariantCulture
            };
            foreach (string query in queries)
            {
                TopDocs ramDocs = ram.Search(qp.Parse(query), 1);
                TopDocs memDocs = mem.Search(qp.Parse(query), 1);

                assertEquals(query, ramDocs.TotalHits, memDocs.TotalHits);
            }
            reader.Dispose();
        }

        internal class RandomAnalyzerHelper : Analyzer
        {
            private readonly MemoryIndexTest outerInstance;
            public RandomAnalyzerHelper(MemoryIndexTest outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new CrazyTokenFilter(tokenizer));
            }
        }

        /**
         * Return a random analyzer (Simple, Stop, Standard) to analyze the terms.
         */
        private Analyzer RandomAnalyzer()
        {
            switch (Random.nextInt(4))
            {
                case 0: return new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
                case 1: return new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
                case 2: return new RandomAnalyzerHelper(this);
                //            return new Analyzer() {

                //        protected TokenStreamComponents createComponents(string fieldName, TextReader reader)
                //{
                //    Tokenizer tokenizer = new MockTokenizer(reader);
                //    return new TokenStreamComponents(tokenizer, new CrazyTokenFilter(tokenizer));
                //}
                //      };
                default: return new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            }
        }



        // a tokenfilter that makes all terms starting with 't' empty strings
        internal sealed class CrazyTokenFilter : TokenFilter
        {
            private readonly ICharTermAttribute termAtt;


            public CrazyTokenFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
            }

            public override bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    if (termAtt.Length > 0 && termAtt.Buffer[0] == 't')
                    {
                        termAtt.SetLength(0);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        };

        /**
         * Some terms to be indexed, in addition to random words. 
         * These terms are commonly used in the queries. 
         */
        private static readonly string[] TEST_TERMS = {"term", "Term", "tErm", "TERM",
            "telm", "stop", "drop", "roll", "phrase", "a", "c", "bar", "blar",
            "gack", "weltbank", "worlbank", "hello", "on", "the", "apache", "Apache",
            "copyright", "Copyright"};


        /**
         * half of the time, returns a random term from TEST_TERMS.
         * the other half of the time, returns a random unicode string.
         */
        private string RandomTerm()
        {
            if (Random.nextBoolean())
            {
                // return a random TEST_TERM
                return TEST_TERMS[Random.nextInt(TEST_TERMS.Length)];
            }
            else
            {
                // return a random unicode term
                return TestUtil.RandomUnicodeString(Random);
            }
        }

        [Test]
        public void TestDocsEnumStart()
        {
            Analyzer analyzer = new MockAnalyzer(Random);
            MemoryIndex memory = new MemoryIndex(Random.nextBoolean(), Random.nextInt(50) * 1024 * 1024);
            memory.AddField("foo", "bar", analyzer);
            AtomicReader reader = (AtomicReader)memory.CreateSearcher().IndexReader;
            DocsEnum disi = TestUtil.Docs(Random, reader, "foo", new BytesRef("bar"), null, null, DocsFlags.NONE);
            int docid = disi.DocID;
            assertEquals(-1, docid);
            assertTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            // now reuse and check again
            TermsEnum te = reader.GetTerms("foo").GetEnumerator();
            assertTrue(te.SeekExact(new BytesRef("bar")));
            disi = te.Docs(null, disi, DocsFlags.NONE);
            docid = disi.DocID;
            assertEquals(-1, docid);
            assertTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            reader.Dispose();
        }

        private ByteBlockPool.Allocator RandomByteBlockAllocator()
        {
            if (Random.nextBoolean())
            {
                return new RecyclingByteBlockAllocator();
            }
            else
            {
                return new ByteBlockPool.DirectAllocator();
            }
        }

        [Test]
        public void RestDocsAndPositionsEnumStart()
        {
            Analyzer analyzer = new MockAnalyzer(Random);
            int numIters = AtLeast(3);
            MemoryIndex memory = new MemoryIndex(true, Random.nextInt(50) * 1024 * 1024);
            for (int i = 0; i < numIters; i++)
            { // check reuse
                memory.AddField("foo", "bar", analyzer);
                AtomicReader reader = (AtomicReader)memory.CreateSearcher().IndexReader;
                assertEquals(1, reader.GetTerms("foo").SumTotalTermFreq);
                DocsAndPositionsEnum disi = reader.GetTermPositionsEnum(new Term("foo", "bar"));
                int docid = disi.DocID;
                assertEquals(-1, docid);
                assertTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                assertEquals(0, disi.NextPosition());
                assertEquals(0, disi.StartOffset);
                assertEquals(3, disi.EndOffset);

                // now reuse and check again
                TermsEnum te = reader.GetTerms("foo").GetEnumerator();
                assertTrue(te.SeekExact(new BytesRef("bar")));
                disi = te.DocsAndPositions(null, disi);
                docid = disi.DocID;
                assertEquals(-1, docid);
                assertTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                reader.Dispose();
                memory.Reset();
            }
        }

        // LUCENE-3831
        [Test]
        public void TestNullPointerException()
        {
            RegexpQuery regex = new RegexpQuery(new Term("field", "worl."));
            SpanQuery wrappedquery = new SpanMultiTermQueryWrapper<RegexpQuery>(regex);

            MemoryIndex mindex = new MemoryIndex(Random.nextBoolean(), Random.nextInt(50) * 1024 * 1024);
            mindex.AddField("field", new MockAnalyzer(Random).GetTokenStream("field", "hello there"));

            // This throws an NPE
            assertEquals(0, mindex.Search(wrappedquery), 0.00001f);
        }

        // LUCENE-3831
        [Test]
        public void TestPassesIfWrapped()
        {
            RegexpQuery regex = new RegexpQuery(new Term("field", "worl."));
            SpanQuery wrappedquery = new SpanOrQuery(new SpanMultiTermQueryWrapper<RegexpQuery>(regex));

            MemoryIndex mindex = new MemoryIndex(Random.nextBoolean(), Random.nextInt(50) * 1024 * 1024);
            mindex.AddField("field", new MockAnalyzer(Random).GetTokenStream("field", "hello there"));

            // This passes though
            assertEquals(0, mindex.Search(wrappedquery), 0.00001f);
        }

        [Test]
        public void TestSameFieldAddedMultipleTimes()
        {
            MemoryIndex mindex = new MemoryIndex(Random.nextBoolean(), Random.nextInt(50) * 1024 * 1024);
            MockAnalyzer mockAnalyzer = new MockAnalyzer(Random);
            mindex.AddField("field", "the quick brown fox", mockAnalyzer);
            mindex.AddField("field", "jumps over the", mockAnalyzer);
            AtomicReader reader = (AtomicReader)mindex.CreateSearcher().IndexReader;
            assertEquals(7, reader.GetTerms("field").SumTotalTermFreq);
            PhraseQuery query = new PhraseQuery();
            query.Add(new Term("field", "fox"));
            query.Add(new Term("field", "jumps"));
            assertTrue(mindex.Search(query) > 0.1);
            mindex.Reset();
            mockAnalyzer.SetPositionIncrementGap(1 + Random.nextInt(10));
            mindex.AddField("field", "the quick brown fox", mockAnalyzer);
            mindex.AddField("field", "jumps over the", mockAnalyzer);
            assertEquals(0, mindex.Search(query), 0.00001f);
            query.Slop = (10);
            assertTrue("posGap" + mockAnalyzer.GetPositionIncrementGap("field"), mindex.Search(query) > 0.0001);
        }

        [Test]
        public void TestNonExistingsField()
        {
            MemoryIndex mindex = new MemoryIndex(Random.nextBoolean(), Random.nextInt(50) * 1024 * 1024);
            MockAnalyzer mockAnalyzer = new MockAnalyzer(Random);
            mindex.AddField("field", "the quick brown fox", mockAnalyzer);
            AtomicReader reader = (AtomicReader)mindex.CreateSearcher().IndexReader;
            assertNull(reader.GetNumericDocValues("not-in-index"));
            assertNull(reader.GetNormValues("not-in-index"));
            assertNull(reader.GetTermDocsEnum(new Term("not-in-index", "foo")));
            assertNull(reader.GetTermPositionsEnum(new Term("not-in-index", "foo")));
            assertNull(reader.GetTerms("not-in-index"));
        }

        [Test]
        public void TestDuellMemIndex()
        {
            LineFileDocs lineFileDocs = new LineFileDocs(Random);
            int numDocs = AtLeast(10);
            MemoryIndex memory = new MemoryIndex(Random.nextBoolean(), Random.nextInt(50) * 1024 * 1024);
            for (int i = 0; i < numDocs; i++)
            {
                Store.Directory dir = NewDirectory();
                MockAnalyzer mockAnalyzer = new MockAnalyzer(Random);
                mockAnalyzer.MaxTokenLength = (TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH));
                IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, mockAnalyzer));
                Document nextDoc = lineFileDocs.NextDoc();
                Document doc = new Document();
                foreach (IIndexableField field in nextDoc.Fields)
                {
                    if (field.IndexableFieldType.IsIndexed)
                    {
                        doc.Add(field);
                        if (Random.nextInt(3) == 0)
                        {
                            doc.Add(field);  // randomly add the same field twice
                        }
                    }
                }

                writer.AddDocument(doc);
                writer.Dispose();
                foreach (IIndexableField field in doc.Fields)
                {
                    memory.AddField(field.Name, ((Field)field).GetStringValue(), mockAnalyzer);
                }
                DirectoryReader competitor = DirectoryReader.Open(dir);
                AtomicReader memIndexReader = (AtomicReader)memory.CreateSearcher().IndexReader;
                DuellReaders(competitor, memIndexReader);
                IOUtils.Dispose(competitor, memIndexReader);
                memory.Reset();
                dir.Dispose();
            }
            lineFileDocs.Dispose();
        }

        // LUCENE-4880
        [Test]
        public void TestEmptyString()
        {
            MemoryIndex memory = new MemoryIndex();
            memory.AddField("foo", new CannedTokenStream(new Analysis.Token("", 0, 5)));
            IndexSearcher searcher = memory.CreateSearcher();
            TopDocs docs = searcher.Search(new TermQuery(new Term("foo", "")), 10);
            assertEquals(1, docs.TotalHits);
        }

        [Test]
        public void TestDuelMemoryIndexCoreDirectoryWithArrayField()
        {

            string field_name = "text";
            MockAnalyzer mockAnalyzer = new MockAnalyzer(Random);
            if (Random.nextBoolean())
            {
                mockAnalyzer.SetOffsetGap(Random.nextInt(100));
            }
            //index into a random directory
            FieldType type = new FieldType(TextField.TYPE_STORED);
            type.StoreTermVectorOffsets = (true);
            type.StoreTermVectorPayloads = (false);
            type.StoreTermVectorPositions = (true);
            type.StoreTermVectors = (true);
            type.Freeze();

            Document doc = new Document();
            doc.Add(new Field(field_name, "la la", type));
            doc.Add(new Field(field_name, "foo bar foo bar foo", type));

            Store.Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, mockAnalyzer));
            writer.UpdateDocument(new Term("id", "1"), doc);
            writer.Commit();
            writer.Dispose();
            DirectoryReader reader = DirectoryReader.Open(dir);

            //Index document in Memory index
            MemoryIndex memIndex = new MemoryIndex(true);
            memIndex.AddField(field_name, "la la", mockAnalyzer);
            memIndex.AddField(field_name, "foo bar foo bar foo", mockAnalyzer);

            //compare term vectors
            Terms ramTv = reader.GetTermVector(0, field_name);
            IndexReader memIndexReader = memIndex.CreateSearcher().IndexReader;
            Terms memTv = memIndexReader.GetTermVector(0, field_name);

            CompareTermVectors(ramTv, memTv, field_name);
            memIndexReader.Dispose();
            reader.Dispose();
            dir.Dispose();

        }

        protected void CompareTermVectors(Terms terms, Terms memTerms, string field_name)
        {

            TermsEnum termEnum = terms.GetEnumerator();
            TermsEnum memTermEnum = memTerms.GetEnumerator();

            while (termEnum.MoveNext())
            {
                assertTrue(memTermEnum.MoveNext());

                assertEquals(termEnum.TotalTermFreq, memTermEnum.TotalTermFreq);

                DocsAndPositionsEnum docsPosEnum = termEnum.DocsAndPositions(null, null, 0);
                DocsAndPositionsEnum memDocsPosEnum = memTermEnum.DocsAndPositions(null, null, 0);
                String currentTerm = termEnum.Term.Utf8ToString();


                assertEquals("Token mismatch for field: " + field_name, currentTerm, memTermEnum.Term.Utf8ToString());

                docsPosEnum.NextDoc();
                memDocsPosEnum.NextDoc();

                int freq = docsPosEnum.Freq;
                assertEquals(freq, memDocsPosEnum.Freq);
                for (int i = 0; i < freq; i++)
                {
                    string failDesc = " (field:" + field_name + " term:" + currentTerm + ")";
                    int memPos = memDocsPosEnum.NextPosition();
                    int pos = docsPosEnum.NextPosition();
                    assertEquals("Position test failed" + failDesc, memPos, pos);
                    assertEquals("Start offset test failed" + failDesc, memDocsPosEnum.StartOffset, docsPosEnum.StartOffset);
                    assertEquals("End offset test failed" + failDesc, memDocsPosEnum.EndOffset, docsPosEnum.EndOffset);
                    assertEquals("Missing payload test failed" + failDesc, docsPosEnum.GetPayload(), null);
                }
            }
            assertFalse("Still some tokens not processed", memTermEnum.MoveNext());
        }
    }
}
