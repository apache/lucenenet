using System;
using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Codecs.Lucene3x
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IOContext = Lucene.Net.Store.IOContext;
    using LogMergePolicy = Lucene.Net.Index.LogMergePolicy;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SegmentReader = Lucene.Net.Index.SegmentReader;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestTermInfosReaderIndex : LuceneTestCase
    {
        private static int NUMBER_OF_DOCUMENTS;
        private static int NUMBER_OF_FIELDS;
        private static TermInfosReaderIndex Index;
        private static Directory Directory;
        private static SegmentTermEnum TermEnum;
        private static int IndexDivisor;
        private static int TermIndexInterval;
        private static IndexReader Reader;
        private static IList<Term> SampleTerms;

        /// <summary>
        /// we will manually instantiate preflex-rw here
        /// 
        /// LUCENENET specific
        /// Is non-static because OLD_FORMAT_IMPERSONATION_IS_ACTIVE is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public void BeforeClass()
        {
            // NOTE: turn off compound file, this test will open some index files directly.
            OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
            IndexWriterConfig config = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false)).SetUseCompoundFile(false);

            TermIndexInterval = config.TermIndexInterval;
            IndexDivisor = TestUtil.NextInt(Random(), 1, 10);
            NUMBER_OF_DOCUMENTS = AtLeast(100);
            NUMBER_OF_FIELDS = AtLeast(Math.Max(10, 3 * TermIndexInterval * IndexDivisor / NUMBER_OF_DOCUMENTS));

            Directory = NewDirectory();

            config.SetCodec(new PreFlexRWCodec(OLD_FORMAT_IMPERSONATION_IS_ACTIVE));
            LogMergePolicy mp = NewLogMergePolicy();
            // NOTE: turn off compound file, this test will open some index files directly.
            mp.NoCFSRatio = 0.0;
            config.SetMergePolicy(mp);

            Populate(Directory, config);

            DirectoryReader r0 = IndexReader.Open(Directory);
            SegmentReader r = LuceneTestCase.GetOnlySegmentReader(r0);
            string segment = r.SegmentName;
            r.Dispose();

            FieldInfosReader infosReader = (new PreFlexRWCodec(OLD_FORMAT_IMPERSONATION_IS_ACTIVE)).FieldInfosFormat().FieldInfosReader;
            FieldInfos fieldInfos = infosReader.Read(Directory, segment, "", IOContext.READONCE);
            string segmentFileName = IndexFileNames.SegmentFileName(segment, "", Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION);
            long tiiFileLength = Directory.FileLength(segmentFileName);
            IndexInput input = Directory.OpenInput(segmentFileName, NewIOContext(Random()));
            TermEnum = new SegmentTermEnum(Directory.OpenInput(IndexFileNames.SegmentFileName(segment, "", Lucene3xPostingsFormat.TERMS_EXTENSION), NewIOContext(Random())), fieldInfos, false);
            int totalIndexInterval = TermEnum.IndexInterval * IndexDivisor;

            SegmentTermEnum indexEnum = new SegmentTermEnum(input, fieldInfos, true);
            Index = new TermInfosReaderIndex(indexEnum, IndexDivisor, tiiFileLength, totalIndexInterval);
            indexEnum.Dispose();
            input.Dispose();

            Reader = IndexReader.Open(Directory);
            SampleTerms = Sample(Random(), Reader, 1000);
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            TermEnum.Dispose();
            Reader.Dispose();
            Directory.Dispose();
            TermEnum = null;
            Reader = null;
            Directory = null;
            Index = null;
            SampleTerms = null;
        }

        [Test]
        public virtual void TestSeekEnum()
        {
            int indexPosition = 3;
            SegmentTermEnum clone = (SegmentTermEnum)TermEnum.Clone();
            Term term = FindTermThatWouldBeAtIndex(clone, indexPosition);
            SegmentTermEnum enumerator = clone;
            Index.SeekEnum(enumerator, indexPosition);
            Assert.AreEqual(term, enumerator.Term());
            clone.Dispose();
        }

        [Test]
        public virtual void TestCompareTo()
        {
            Term term = new Term("field" + Random().Next(NUMBER_OF_FIELDS), Text);
            for (int i = 0; i < Index.Length(); i++)
            {
                Term t = Index.GetTerm(i);
                int compareTo = term.CompareTo(t);
                Assert.AreEqual(compareTo, Index.CompareTo(term, i));
            }
        }

        [Test]
        public virtual void TestRandomSearchPerformance()
        {
            IndexSearcher searcher = new IndexSearcher(Reader);
            foreach (Term t in SampleTerms)
            {
                TermQuery query = new TermQuery(t);
                TopDocs topDocs = searcher.Search(query, 10);
                Assert.IsTrue(topDocs.TotalHits > 0);
            }
        }

        private static IList<Term> Sample(Random random, IndexReader reader, int size)
        {
            IList<Term> sample = new List<Term>();
            Fields fields = MultiFields.GetFields(reader);
            foreach (string field in fields)
            {
                Terms terms = fields.Terms(field);
                Assert.IsNotNull(terms);
                TermsEnum termsEnum = terms.Iterator(null);
                while (termsEnum.Next() != null)
                {
                    if (sample.Count >= size)
                    {
                        int pos = random.Next(size);
                        sample[pos] = new Term(field, termsEnum.Term());
                    }
                    else
                    {
                        sample.Add(new Term(field, termsEnum.Term()));
                    }
                }
            }
            sample = CollectionsHelper.Shuffle(sample);
            return sample;
        }

        private Term FindTermThatWouldBeAtIndex(SegmentTermEnum termEnum, int index)
        {
            int termPosition = index * TermIndexInterval * IndexDivisor;
            for (int i = 0; i < termPosition; i++)
            {
                // TODO: this test just uses random terms, so this is always possible
                AssumeTrue("ran out of terms", termEnum.Next());
            }
            Term term = termEnum.Term();
            // An indexed term is only written when the term after
            // it exists, so, if the number of terms is 0 mod
            // termIndexInterval, the last index term will not be
            // written; so we require a term after this term
            // as well:
            AssumeTrue("ran out of terms", termEnum.Next());
            return term;
        }

        private void Populate(Directory directory, IndexWriterConfig config)
        {
            RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, config);
            for (int i = 0; i < NUMBER_OF_DOCUMENTS; i++)
            {
                Document document = new Document();
                for (int f = 0; f < NUMBER_OF_FIELDS; f++)
                {
                    document.Add(NewStringField("field" + f, Text, Field.Store.NO));
                }
                writer.AddDocument(document);
            }
            writer.ForceMerge(1);
            writer.Dispose();
        }

        private static string Text
        {
            get
            {
                return Convert.ToString(Random().Next());
            }
        }
    }
}