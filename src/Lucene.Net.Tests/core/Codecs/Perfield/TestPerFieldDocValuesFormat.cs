using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Documents;

namespace Lucene.Net.Codecs.Perfield
{
    using Lucene.Net.Index;
    using NUnit.Framework;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BaseDocValuesFormatTestCase = Lucene.Net.Index.BaseDocValuesFormatTestCase;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using NumericDocValuesField = NumericDocValuesField;
    using Query = Lucene.Net.Search.Query;
    using RandomCodec = Lucene.Net.Index.RandomCodec;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TopDocs = Lucene.Net.Search.TopDocs;

    /// <summary>
    /// Basic tests of PerFieldDocValuesFormat
    /// </summary>
    [TestFixture]
    public class TestPerFieldDocValuesFormat : BaseDocValuesFormatTestCase
    {
        private Codec Codec_Renamed;

        [SetUp]
        public override void SetUp()
        {
            Codec_Renamed = new RandomCodec(new Random(Random().Next()), new HashSet<string>());
            base.SetUp();
        }

        protected override Codec Codec
        {
            get
            {
                return Codec_Renamed;
            }
        }

        protected internal override bool CodecAcceptsHugeBinaryValues(string field)
        {
            return TestUtil.FieldSupportsHugeBinaryDocValues(field);
        }

        // just a simple trivial test
        // TODO: we should come up with a test that somehow checks that segment suffix
        // is respected by all codec apis (not just docvalues and postings)
        [Test]
        public virtual void TestTwoFieldsTwoFormats()
        {
            Analyzer analyzer = new MockAnalyzer(Random());

            Directory directory = NewDirectory();
            // we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            DocValuesFormat fast = DocValuesFormat.ForName("Lucene45");
            DocValuesFormat slow = DocValuesFormat.ForName("Lucene45");
            iwc.SetCodec(new Lucene46CodecAnonymousInnerClassHelper(this, fast, slow));
            IndexWriter iwriter = new IndexWriter(directory, iwc);
            Document doc = new Document();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;
            doc.Add(NewTextField("fieldname", text, Field.Store.YES));
            doc.Add(new NumericDocValuesField("dv1", 5));
            doc.Add(new BinaryDocValuesField("dv2", new BytesRef("hello world")));
            iwriter.AddDocument(doc);
            iwriter.Dispose();

            // Now search the index:
            IndexReader ireader = DirectoryReader.Open(directory); // read-only=true
            IndexSearcher isearcher = NewSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            BytesRef scratch = new BytesRef();
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
                Debug.Assert(ireader.Leaves.Count == 1);
                NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv1");
                Assert.AreEqual(5, dv.Get(hits.ScoreDocs[i].Doc));
                BinaryDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv2");
                dv2.Get(hits.ScoreDocs[i].Doc, scratch);
                Assert.AreEqual(new BytesRef("hello world"), scratch);
            }

            ireader.Dispose();
            directory.Dispose();
        }

        private class Lucene46CodecAnonymousInnerClassHelper : Lucene46Codec
        {
            private readonly TestPerFieldDocValuesFormat OuterInstance;

            private DocValuesFormat Fast;
            private DocValuesFormat Slow;

            public Lucene46CodecAnonymousInnerClassHelper(TestPerFieldDocValuesFormat outerInstance, DocValuesFormat fast, DocValuesFormat slow)
            {
                this.OuterInstance = outerInstance;
                this.Fast = fast;
                this.Slow = slow;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                if ("dv1".Equals(field))
                {
                    return Fast;
                }
                else
                {
                    return Slow;
                }
            }
        }


        #region BaseDocValuesFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestOneNumber()
        {
            base.TestOneNumber();
        }

        [Test]
        public override void TestOneFloat()
        {
            base.TestOneFloat();
        }

        [Test]
        public override void TestTwoNumbers()
        {
            base.TestTwoNumbers();
        }

        [Test]
        public override void TestTwoBinaryValues()
        {
            base.TestTwoBinaryValues();
        }

        [Test]
        public override void TestTwoFieldsMixed()
        {
            base.TestTwoFieldsMixed();
        }

        [Test]
        public override void TestThreeFieldsMixed()
        {
            base.TestThreeFieldsMixed();
        }

        [Test]
        public override void TestThreeFieldsMixed2()
        {
            base.TestThreeFieldsMixed2();
        }

        [Test]
        public override void TestTwoDocumentsNumeric()
        {
            base.TestTwoDocumentsNumeric();
        }

        [Test]
        public override void TestTwoDocumentsMerged()
        {
            base.TestTwoDocumentsMerged();
        }

        [Test]
        public override void TestBigNumericRange()
        {
            base.TestBigNumericRange();
        }

        [Test]
        public override void TestBigNumericRange2()
        {
            base.TestBigNumericRange2();
        }

        [Test]
        public override void TestBytes()
        {
            base.TestBytes();
        }

        [Test]
        public override void TestBytesTwoDocumentsMerged()
        {
            base.TestBytesTwoDocumentsMerged();
        }

        [Test]
        public override void TestSortedBytes()
        {
            base.TestSortedBytes();
        }

        [Test]
        public override void TestSortedBytesTwoDocuments()
        {
            base.TestSortedBytesTwoDocuments();
        }

        [Test]
        public override void TestSortedBytesThreeDocuments()
        {
            base.TestSortedBytesThreeDocuments();
        }

        [Test]
        public override void TestSortedBytesTwoDocumentsMerged()
        {
            base.TestSortedBytesTwoDocumentsMerged();
        }

        [Test]
        public override void TestSortedMergeAwayAllValues()
        {
            base.TestSortedMergeAwayAllValues();
        }

        [Test]
        public override void TestBytesWithNewline()
        {
            base.TestBytesWithNewline();
        }

        [Test]
        public override void TestMissingSortedBytes()
        {
            base.TestMissingSortedBytes();
        }

        [Test]
        public override void TestSortedTermsEnum()
        {
            base.TestSortedTermsEnum();
        }

        [Test]
        public override void TestEmptySortedBytes()
        {
            base.TestEmptySortedBytes();
        }

        [Test]
        public override void TestEmptyBytes()
        {
            base.TestEmptyBytes();
        }

        [Test]
        public override void TestVeryLargeButLegalBytes()
        {
            base.TestVeryLargeButLegalBytes();
        }

        [Test]
        public override void TestVeryLargeButLegalSortedBytes()
        {
            base.TestVeryLargeButLegalSortedBytes();
        }

        [Test]
        public override void TestCodecUsesOwnBytes()
        {
            base.TestCodecUsesOwnBytes();
        }

        [Test]
        public override void TestCodecUsesOwnSortedBytes()
        {
            base.TestCodecUsesOwnSortedBytes();
        }

        [Test]
        public override void TestCodecUsesOwnBytesEachTime()
        {
            base.TestCodecUsesOwnBytesEachTime();
        }

        [Test]
        public override void TestCodecUsesOwnSortedBytesEachTime()
        {
            base.TestCodecUsesOwnSortedBytesEachTime();
        }

        /*
         * Simple test case to show how to use the API
         */
        [Test]
        public override void TestDocValuesSimple()
        {
            base.TestDocValuesSimple();
        }

        [Test]
        public override void TestRandomSortedBytes()
        {
            base.TestRandomSortedBytes();
        }

        [Test]
        public override void TestBooleanNumericsVsStoredFields()
        {
            base.TestBooleanNumericsVsStoredFields();
        }

        [Test]
        public override void TestByteNumericsVsStoredFields()
        {
            base.TestByteNumericsVsStoredFields();
        }

        [Test]
        public override void TestByteMissingVsFieldCache()
        {
            base.TestByteMissingVsFieldCache();
        }

        [Test]
        public override void TestShortNumericsVsStoredFields()
        {
            base.TestShortNumericsVsStoredFields();
        }

        [Test]
        public override void TestShortMissingVsFieldCache()
        {
            base.TestShortMissingVsFieldCache();
        }

        [Test]
        public override void TestIntNumericsVsStoredFields()
        {
            base.TestIntNumericsVsStoredFields();
        }

        [Test]
        public override void TestIntMissingVsFieldCache()
        {
            base.TestIntMissingVsFieldCache();
        }

        [Test]
        public override void TestLongNumericsVsStoredFields()
        {
            base.TestLongNumericsVsStoredFields();
        }

        [Test]
        public override void TestLongMissingVsFieldCache()
        {
            base.TestLongMissingVsFieldCache();
        }

        [Test]
        public override void TestBinaryFixedLengthVsStoredFields()
        {
            base.TestBinaryFixedLengthVsStoredFields();
        }

        [Test]
        public override void TestBinaryVariableLengthVsStoredFields()
        {
            base.TestBinaryVariableLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedFixedLengthVsStoredFields()
        {
            base.TestSortedFixedLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedFixedLengthVsFieldCache()
        {
            base.TestSortedFixedLengthVsFieldCache();
        }

        [Test]
        public override void TestSortedVariableLengthVsFieldCache()
        {
            base.TestSortedVariableLengthVsFieldCache();
        }

        [Test]
        public override void TestSortedVariableLengthVsStoredFields()
        {
            base.TestSortedVariableLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedSetOneValue()
        {
            base.TestSortedSetOneValue();
        }

        [Test]
        public override void TestSortedSetTwoFields()
        {
            base.TestSortedSetTwoFields();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsMerged()
        {
            base.TestSortedSetTwoDocumentsMerged();
        }

        [Test]
        public override void TestSortedSetTwoValues()
        {
            base.TestSortedSetTwoValues();
        }

        [Test]
        public override void TestSortedSetTwoValuesUnordered()
        {
            base.TestSortedSetTwoValuesUnordered();
        }

        [Test]
        public override void TestSortedSetThreeValuesTwoDocs()
        {
            base.TestSortedSetThreeValuesTwoDocs();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsLastMissing()
        {
            base.TestSortedSetTwoDocumentsLastMissing();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsLastMissingMerge()
        {
            base.TestSortedSetTwoDocumentsLastMissingMerge();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsFirstMissing()
        {
            base.TestSortedSetTwoDocumentsFirstMissing();
        }

        [Test]
        public override void TestSortedSetTwoDocumentsFirstMissingMerge()
        {
            base.TestSortedSetTwoDocumentsFirstMissingMerge();
        }

        [Test]
        public override void TestSortedSetMergeAwayAllValues()
        {
            base.TestSortedSetMergeAwayAllValues();
        }

        [Test]
        public override void TestSortedSetTermsEnum()
        {
            base.TestSortedSetTermsEnum();
        }

        [Test]
        public override void TestSortedSetFixedLengthVsStoredFields()
        {
            base.TestSortedSetFixedLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedSetVariableLengthVsStoredFields()
        {
            base.TestSortedSetVariableLengthVsStoredFields();
        }

        [Test]
        public override void TestSortedSetFixedLengthSingleValuedVsStoredFields()
        {
            base.TestSortedSetFixedLengthSingleValuedVsStoredFields();
        }

        [Test]
        public override void TestSortedSetVariableLengthSingleValuedVsStoredFields()
        {
            base.TestSortedSetVariableLengthSingleValuedVsStoredFields();
        }

        [Test]
        public override void TestSortedSetFixedLengthVsUninvertedField()
        {
            base.TestSortedSetFixedLengthVsUninvertedField();
        }

        [Test]
        public override void TestSortedSetVariableLengthVsUninvertedField()
        {
            base.TestSortedSetVariableLengthVsUninvertedField();
        }

        [Test]
        public override void TestGCDCompression()
        {
            base.TestGCDCompression();
        }

        [Test]
        public override void TestZeros()
        {
            base.TestZeros();
        }

        [Test]
        public override void TestZeroOrMin()
        {
            base.TestZeroOrMin();
        }

        [Test]
        public override void TestTwoNumbersOneMissing()
        {
            base.TestTwoNumbersOneMissing();
        }

        [Test]
        public override void TestTwoNumbersOneMissingWithMerging()
        {
            base.TestTwoNumbersOneMissingWithMerging();
        }

        [Test]
        public override void TestThreeNumbersOneMissingWithMerging()
        {
            base.TestThreeNumbersOneMissingWithMerging();
        }

        [Test]
        public override void TestTwoBytesOneMissing()
        {
            base.TestTwoBytesOneMissing();
        }

        [Test]
        public override void TestTwoBytesOneMissingWithMerging()
        {
            base.TestTwoBytesOneMissingWithMerging();
        }

        [Test]
        public override void TestThreeBytesOneMissingWithMerging()
        {
            base.TestThreeBytesOneMissingWithMerging();
        }

        // LUCENE-4853
        [Test]
        public override void TestHugeBinaryValues()
        {
            base.TestHugeBinaryValues();
        }

        // TODO: get this out of here and into the deprecated codecs (4.0, 4.2)
        [Test]
        public override void TestHugeBinaryValueLimit()
        {
            base.TestHugeBinaryValueLimit();
        }

        /// <summary>
        /// Tests dv against stored fields with threads (binary/numeric/sorted, no missing)
        /// </summary>
        [Test]
        public override void TestThreads()
        {
            base.TestThreads();
        }

        /// <summary>
        /// Tests dv against stored fields with threads (all types + missing)
        /// </summary>
        [Test]
        public override void TestThreads2()
        {
            base.TestThreads2();
        }

        // LUCENE-5218
        [Test]
        public override void TestEmptyBinaryValueOnPageSizes()
        {
            base.TestEmptyBinaryValueOnPageSizes();
        }

        #endregion

        #region BaseIndexFileFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestMergeStability()
        {
            base.TestMergeStability();
        }

        #endregion
    }
}