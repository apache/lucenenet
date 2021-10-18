using NUnit.Framework;
using System;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Index
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Constants = Lucene.Net.Util.Constants;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestSegmentMerger : LuceneTestCase
    {
        //The variables for the new merged segment
        private Directory mergedDir;

        private string mergedSegment = "test";

        //First segment to be merged
        private Directory merge1Dir;

        private Document doc1;
        private SegmentReader reader1;

        //Second Segment to be merged
        private Directory merge2Dir;

        private Document doc2;
        private SegmentReader reader2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            this.doc1 = new Document();
            this.doc2 = new Document();
            mergedDir = NewDirectory();
            merge1Dir = NewDirectory();
            merge2Dir = NewDirectory();
            DocHelper.SetupDoc(doc1);
            SegmentCommitInfo info1 = DocHelper.WriteDoc(Random, merge1Dir, doc1);
            DocHelper.SetupDoc(doc2);
            SegmentCommitInfo info2 = DocHelper.WriteDoc(Random, merge2Dir, doc2);
            reader1 = new SegmentReader(info1, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random));
            reader2 = new SegmentReader(info2, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random));
        }

        [TearDown]
        public override void TearDown()
        {
            reader1.Dispose();
            reader2.Dispose();
            mergedDir.Dispose();
            merge1Dir.Dispose();
            merge2Dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void Test()
        {
            Assert.IsTrue(mergedDir != null);
            Assert.IsTrue(merge1Dir != null);
            Assert.IsTrue(merge2Dir != null);
            Assert.IsTrue(reader1 != null);
            Assert.IsTrue(reader2 != null);
        }

        [Test]
        public virtual void TestMerge()
        {
            Codec codec = Codec.Default;
            SegmentInfo si = new SegmentInfo(mergedDir, Constants.LUCENE_MAIN_VERSION, mergedSegment, -1, false, codec, null);

            SegmentMerger merger = new SegmentMerger(new JCG.List<AtomicReader> { reader1, reader2 }, si, (InfoStream)InfoStream.Default, mergedDir, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL, CheckAbort.NONE, new FieldInfos.FieldNumbers(), NewIOContext(Random), true);
            MergeState mergeState = merger.Merge();
            int docsMerged = mergeState.SegmentInfo.DocCount;
            Assert.IsTrue(docsMerged == 2);
            //Should be able to open a new SegmentReader against the new directory
            SegmentReader mergedReader = new SegmentReader(new SegmentCommitInfo(new SegmentInfo(mergedDir, Constants.LUCENE_MAIN_VERSION, mergedSegment, docsMerged, false, codec, null), 0, -1L, -1L), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random));
            Assert.IsTrue(mergedReader != null);
            Assert.IsTrue(mergedReader.NumDocs == 2);
            Document newDoc1 = mergedReader.Document(0);
            Assert.IsTrue(newDoc1 != null);
            //There are 2 unstored fields on the document
            Assert.IsTrue(DocHelper.NumFields(newDoc1) == DocHelper.NumFields(doc1) - DocHelper.Unstored.Count);
            Document newDoc2 = mergedReader.Document(1);
            Assert.IsTrue(newDoc2 != null);
            Assert.IsTrue(DocHelper.NumFields(newDoc2) == DocHelper.NumFields(doc2) - DocHelper.Unstored.Count);

            DocsEnum termDocs = TestUtil.Docs(Random, mergedReader, DocHelper.TEXT_FIELD_2_KEY, new BytesRef("field"), MultiFields.GetLiveDocs(mergedReader), null, 0);
            Assert.IsTrue(termDocs != null);
            Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            int tvCount = 0;
            foreach (FieldInfo fieldInfo in mergedReader.FieldInfos)
            {
                if (fieldInfo.HasVectors)
                {
                    tvCount++;
                }
            }

            //System.out.println("stored size: " + stored.Size());
            Assert.AreEqual(3, tvCount, "We do not have 3 fields that were indexed with term vector");

            Terms vector = mergedReader.GetTermVectors(0).GetTerms(DocHelper.TEXT_FIELD_2_KEY);
            Assert.IsNotNull(vector);
            Assert.AreEqual(3, vector.Count);
            TermsEnum termsEnum = vector.GetEnumerator();

            int i = 0;
            while (termsEnum.MoveNext())
            {
                string term = termsEnum.Term.Utf8ToString();
                int freq = (int)termsEnum.TotalTermFreq;
                //System.out.println("Term: " + term + " Freq: " + freq);
                Assert.IsTrue(DocHelper.FIELD_2_TEXT.IndexOf(term, StringComparison.Ordinal) != -1);
                Assert.IsTrue(DocHelper.FIELD_2_FREQS[i] == freq);
                i++;
            }

            TestSegmentReader.CheckNorms(mergedReader);
            mergedReader.Dispose();
        }

        private static bool Equals(MergeState.DocMap map1, MergeState.DocMap map2)
        {
            if (map1.MaxDoc != map2.MaxDoc)
            {
                return false;
            }
            for (int i = 0; i < map1.MaxDoc; ++i)
            {
                if (map1.Get(i) != map2.Get(i))
                {
                    return false;
                }
            }
            return true;
        }

        [Test]
        public virtual void TestBuildDocMap()
        {
            int maxDoc = TestUtil.NextInt32(Random, 1, 128);
            int numDocs = TestUtil.NextInt32(Random, 0, maxDoc);
            int numDeletedDocs = maxDoc - numDocs;
            FixedBitSet liveDocs = new FixedBitSet(maxDoc);
            for (int i = 0; i < numDocs; ++i)
            {
                while (true)
                {
                    int docID = Random.Next(maxDoc);
                    if (!liveDocs.Get(docID))
                    {
                        liveDocs.Set(docID);
                        break;
                    }
                }
            }

            MergeState.DocMap docMap = MergeState.DocMap.Build(maxDoc, liveDocs);

            Assert.AreEqual(maxDoc, docMap.MaxDoc);
            Assert.AreEqual(numDocs, docMap.NumDocs);
            Assert.AreEqual(numDeletedDocs, docMap.NumDeletedDocs);
            // assert the mapping is compact
            for (int i = 0, del = 0; i < maxDoc; ++i)
            {
                if (!liveDocs.Get(i))
                {
                    Assert.AreEqual(-1, docMap.Get(i));
                    ++del;
                }
                else
                {
                    Assert.AreEqual(i - del, docMap.Get(i));
                }
            }
        }
    }
}