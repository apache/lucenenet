using J2N.Collections.Generic.Extensions;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

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
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using IBits = Lucene.Net.Util.IBits;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestPerSegmentDeletes : LuceneTestCase
    {
        [Test]
        public virtual void TestDeletes1()
        {
            //IndexWriter.debug2 = System.out;
            Directory dir = new MockDirectoryWrapper(new J2N.Randomizer(Random.NextInt64()), new RAMDirectory());
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergeScheduler(new SerialMergeScheduler());
            iwc.SetMaxBufferedDocs(5000);
            iwc.SetRAMBufferSizeMB(100);
            RangeMergePolicy fsmp = new RangeMergePolicy(this, false);
            iwc.SetMergePolicy(fsmp);
            IndexWriter writer = new IndexWriter(dir, iwc);
            for (int x = 0; x < 5; x++)
            {
                writer.AddDocument(DocHelper.CreateDocument(x, "1", 2));
                //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
            }
            //System.out.println("commit1");
            writer.Commit();
            Assert.AreEqual(1, writer.SegmentCount);
            for (int x = 5; x < 10; x++)
            {
                writer.AddDocument(DocHelper.CreateDocument(x, "2", 2));
                //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
            }
            //System.out.println("commit2");
            writer.Commit();
            Assert.AreEqual(2, writer.SegmentCount);

            for (int x = 10; x < 15; x++)
            {
                writer.AddDocument(DocHelper.CreateDocument(x, "3", 2));
                //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
            }

            writer.DeleteDocuments(new Term("id", "1"));

            writer.DeleteDocuments(new Term("id", "11"));

            // flushing without applying deletes means
            // there will still be deletes in the segment infos
            writer.Flush(false, false);
            Assert.IsTrue(writer.bufferedUpdatesStream.Any());

            // get reader flushes pending deletes
            // so there should not be anymore
            IndexReader r1 = writer.GetReader();
            Assert.IsFalse(writer.bufferedUpdatesStream.Any());
            r1.Dispose();

            // delete id:2 from the first segment
            // merge segments 0 and 1
            // which should apply the delete id:2
            writer.DeleteDocuments(new Term("id", "2"));
            writer.Flush(false, false);
            fsmp = (RangeMergePolicy)writer.Config.MergePolicy;
            fsmp.doMerge = true;
            fsmp.start = 0;
            fsmp.length = 2;
            writer.MaybeMerge();

            Assert.AreEqual(2, writer.SegmentCount);

            // id:2 shouldn't exist anymore because
            // it's been applied in the merge and now it's gone
            IndexReader r2 = writer.GetReader();
            int[] id2docs = ToDocsArray(new Term("id", "2"), null, r2);
            Assert.IsTrue(id2docs is null);
            r2.Dispose();

            /*
            /// // added docs are in the ram buffer
            /// for (int x = 15; x < 20; x++) {
            ///  writer.AddDocument(TestIndexWriterReader.CreateDocument(x, "4", 2));
            ///  System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
            /// }
            /// Assert.IsTrue(writer.numRamDocs() > 0);
            /// // delete from the ram buffer
            /// writer.DeleteDocuments(new Term("id", Integer.toString(13)));
            ///
            /// Term id3 = new Term("id", Integer.toString(3));
            ///
            /// // delete from the 1st segment
            /// writer.DeleteDocuments(id3);
            ///
            /// Assert.IsTrue(writer.numRamDocs() > 0);
            ///
            /// //System.out
            /// //    .println("segdels1:" + writer.docWriter.deletesToString());
            ///
            /// //Assert.IsTrue(writer.docWriter.segmentDeletes.Size() > 0);
            ///
            /// // we cause a merge to happen
            /// fsmp.doMerge = true;
            /// fsmp.start = 0;
            /// fsmp.length = 2;
            /// System.out.println("maybeMerge "+writer.SegmentInfos);
            ///
            /// SegmentInfo info0 = writer.SegmentInfos[0];
            /// SegmentInfo info1 = writer.SegmentInfos[1];
            ///
            /// writer.MaybeMerge();
            /// System.out.println("maybeMerge after "+writer.SegmentInfos);
            /// // there should be docs in RAM
            /// Assert.IsTrue(writer.numRamDocs() > 0);
            ///
            /// // assert we've merged the 1 and 2 segments
            /// // and still have a segment leftover == 2
            /// Assert.AreEqual(2, writer.SegmentInfos.Size());
            /// Assert.IsFalse(segThere(info0, writer.SegmentInfos));
            /// Assert.IsFalse(segThere(info1, writer.SegmentInfos));
            ///
            /// //System.out.println("segdels2:" + writer.docWriter.deletesToString());
            ///
            /// //Assert.IsTrue(writer.docWriter.segmentDeletes.Size() > 0);
            ///
            /// IndexReader r = writer.GetReader();
            /// IndexReader r1 = r.getSequentialSubReaders()[0];
            /// printDelDocs(r1.GetLiveDocs());
            /// int[] docs = toDocsArray(id3, null, r);
            /// System.out.println("id3 docs:"+Arrays.toString(docs));
            /// // there shouldn't be any docs for id:3
            /// Assert.IsTrue(docs is null);
            /// r.Dispose();
            ///
            /// part2(writer, fsmp);
            ///
            */
            // System.out.println("segdels2:"+writer.docWriter.segmentDeletes.toString());
            //System.out.println("close");
            writer.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// static boolean hasPendingDeletes(SegmentInfos infos) {
        ///  for (SegmentInfo info : infos) {
        ///    if (info.deletes.Any()) {
        ///      return true;
        ///    }
        ///  }
        ///  return false;
        /// }
        ///
        /// </summary>
        internal virtual void Part2(IndexWriter writer, RangeMergePolicy fsmp)
        {
            for (int x = 20; x < 25; x++)
            {
                writer.AddDocument(DocHelper.CreateDocument(x, "5", 2));
                //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
            }
            writer.Flush(false, false);
            for (int x = 25; x < 30; x++)
            {
                writer.AddDocument(DocHelper.CreateDocument(x, "5", 2));
                //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
            }
            writer.Flush(false, false);

            //System.out.println("infos3:"+writer.SegmentInfos);

            Term delterm = new Term("id", "8");
            writer.DeleteDocuments(delterm);
            //System.out.println("segdels3:" + writer.docWriter.deletesToString());

            fsmp.doMerge = true;
            fsmp.start = 1;
            fsmp.length = 2;
            writer.MaybeMerge();

            // deletes for info1, the newly created segment from the
            // merge should have no deletes because they were applied in
            // the merge
            //SegmentInfo info1 = writer.SegmentInfos[1];
            //Assert.IsFalse(exists(info1, writer.docWriter.segmentDeletes));

            //System.out.println("infos4:"+writer.SegmentInfos);
            //System.out.println("segdels4:" + writer.docWriter.deletesToString());
        }

        internal virtual bool SegThere(SegmentCommitInfo info, SegmentInfos infos)
        {
            foreach (SegmentCommitInfo si in infos.Segments)
            {
                if (si.Info.Name.Equals(info.Info.Name, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        public static void PrintDelDocs(IBits bits)
        {
            if (bits is null)
            {
                return;
            }
            for (int x = 0; x < bits.Length; x++)
            {
                Console.WriteLine(x + ":" + bits.Get(x));
            }
        }

        public virtual int[] ToDocsArray(Term term, IBits bits, IndexReader reader)
        {
            Fields fields = MultiFields.GetFields(reader);
            Terms cterms = fields.GetTerms(term.Field);
            TermsEnum ctermsEnum = cterms.GetEnumerator();
            if (ctermsEnum.SeekExact(new BytesRef(term.Text)))
            {
                DocsEnum docsEnum = TestUtil.Docs(Random, ctermsEnum, bits, null, DocsFlags.NONE);
                return ToArray(docsEnum);
            }
            return null;
        }

        public static int[] ToArray(DocsEnum docsEnum)
        {
            IList<int> docs = new JCG.List<int>();
            while (docsEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                int docID = docsEnum.DocID;
                docs.Add(docID);
            }
            return docs.ToArray(); // LUCENENET: ArrayUtil.ToIntArray() call unnecessary because we aren't dealing with reference types
        }

        public class RangeMergePolicy : MergePolicy
        {
            private readonly TestPerSegmentDeletes outerInstance;

            internal bool doMerge = false;
            internal int start;
            internal int length;

            internal readonly bool useCompoundFile;

            internal RangeMergePolicy(TestPerSegmentDeletes outerInstance, bool useCompoundFile)
            {
                this.outerInstance = outerInstance;
                this.useCompoundFile = useCompoundFile;
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override MergeSpecification FindMerges(MergeTrigger mergeTrigger, SegmentInfos segmentInfos)
            {
                MergeSpecification ms = new MergeSpecification();
                if (doMerge)
                {
                    OneMerge om = new OneMerge(segmentInfos.AsList().GetView(start, length)); // LUCENENET: Converted end index to length
                    ms.Add(om);
                    doMerge = false;
                    return ms;
                }
                return null;
            }

            public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool> segmentsToMerge)
            {
                return null;
            }

            public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
            {
                return null;
            }

            public override bool UseCompoundFile(SegmentInfos segments, SegmentCommitInfo newSegment)
            {
                return useCompoundFile;
            }
        }
    }
}