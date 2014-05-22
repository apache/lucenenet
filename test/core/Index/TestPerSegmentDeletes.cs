using System;
using System.Collections.Generic;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using ArrayUtil = Lucene.Net.Util.ArrayUtil;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestPerSegmentDeletes : LuceneTestCase
	{
	  public virtual void TestDeletes1()
	  {
		//IndexWriter.debug2 = System.out;
		Directory dir = new MockDirectoryWrapper(new Random(random().nextLong()), new RAMDirectory());
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.MergeScheduler = new SerialMergeScheduler();
		iwc.MaxBufferedDocs = 5000;
		iwc.RAMBufferSizeMB = 100;
		RangeMergePolicy fsmp = new RangeMergePolicy(this, false);
		iwc.MergePolicy = fsmp;
		IndexWriter writer = new IndexWriter(dir, iwc);
		for (int x = 0; x < 5; x++)
		{
		  writer.addDocument(DocHelper.createDocument(x, "1", 2));
		  //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
		}
		//System.out.println("commit1");
		writer.commit();
		Assert.AreEqual(1, writer.segmentInfos.size());
		for (int x = 5; x < 10; x++)
		{
		  writer.addDocument(DocHelper.createDocument(x, "2", 2));
		  //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
		}
		//System.out.println("commit2");
		writer.commit();
		Assert.AreEqual(2, writer.segmentInfos.size());

		for (int x = 10; x < 15; x++)
		{
		  writer.addDocument(DocHelper.createDocument(x, "3", 2));
		  //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
		}

		writer.deleteDocuments(new Term("id", "1"));

		writer.deleteDocuments(new Term("id", "11"));

		// flushing without applying deletes means
		// there will still be deletes in the segment infos
		writer.flush(false, false);
		Assert.IsTrue(writer.bufferedUpdatesStream.any());

		// get reader flushes pending deletes
		// so there should not be anymore
		IndexReader r1 = writer.Reader;
		Assert.IsFalse(writer.bufferedUpdatesStream.any());
		r1.close();

		// delete id:2 from the first segment
		// merge segments 0 and 1
		// which should apply the delete id:2
		writer.deleteDocuments(new Term("id", "2"));
		writer.flush(false, false);
		fsmp = (RangeMergePolicy) writer.Config.MergePolicy;
		fsmp.DoMerge = true;
		fsmp.Start = 0;
		fsmp.Length = 2;
		writer.maybeMerge();

		Assert.AreEqual(2, writer.segmentInfos.size());

		// id:2 shouldn't exist anymore because
		// it's been applied in the merge and now it's gone
		IndexReader r2 = writer.Reader;
		int[] id2docs = ToDocsArray(new Term("id", "2"), null, r2);
		Assert.IsTrue(id2docs == null);
		r2.close();

		/// <summary>
		/// // added docs are in the ram buffer
		/// for (int x = 15; x < 20; x++) {
		///  writer.addDocument(TestIndexWriterReader.createDocument(x, "4", 2));
		///  System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
		/// }
		/// Assert.IsTrue(writer.numRamDocs() > 0);
		/// // delete from the ram buffer
		/// writer.deleteDocuments(new Term("id", Integer.toString(13)));
		/// 
		/// Term id3 = new Term("id", Integer.toString(3));
		/// 
		/// // delete from the 1st segment
		/// writer.deleteDocuments(id3);
		/// 
		/// Assert.IsTrue(writer.numRamDocs() > 0);
		/// 
		/// //System.out
		/// //    .println("segdels1:" + writer.docWriter.deletesToString());
		/// 
		/// //Assert.IsTrue(writer.docWriter.segmentDeletes.size() > 0);
		/// 
		/// // we cause a merge to happen
		/// fsmp.doMerge = true;
		/// fsmp.start = 0;
		/// fsmp.length = 2;
		/// System.out.println("maybeMerge "+writer.segmentInfos);
		/// 
		/// SegmentInfo info0 = writer.segmentInfos.info(0);
		/// SegmentInfo info1 = writer.segmentInfos.info(1);
		/// 
		/// writer.maybeMerge();
		/// System.out.println("maybeMerge after "+writer.segmentInfos);
		/// // there should be docs in RAM
		/// Assert.IsTrue(writer.numRamDocs() > 0);
		/// 
		/// // assert we've merged the 1 and 2 segments
		/// // and still have a segment leftover == 2
		/// Assert.AreEqual(2, writer.segmentInfos.size());
		/// Assert.IsFalse(segThere(info0, writer.segmentInfos));
		/// Assert.IsFalse(segThere(info1, writer.segmentInfos));
		/// 
		/// //System.out.println("segdels2:" + writer.docWriter.deletesToString());
		/// 
		/// //Assert.IsTrue(writer.docWriter.segmentDeletes.size() > 0);
		/// 
		/// IndexReader r = writer.getReader();
		/// IndexReader r1 = r.getSequentialSubReaders()[0];
		/// printDelDocs(r1.getLiveDocs());
		/// int[] docs = toDocsArray(id3, null, r);
		/// System.out.println("id3 docs:"+Arrays.toString(docs));
		/// // there shouldn't be any docs for id:3
		/// Assert.IsTrue(docs == null);
		/// r.close();
		/// 
		/// part2(writer, fsmp);
		/// 
		/// </summary>
		// System.out.println("segdels2:"+writer.docWriter.segmentDeletes.toString());
		//System.out.println("close");
		writer.close();
		dir.close();
	  }

	  /// <summary>
	  /// static boolean hasPendingDeletes(SegmentInfos infos) {
	  ///  for (SegmentInfo info : infos) {
	  ///    if (info.deletes.any()) {
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
		  writer.addDocument(DocHelper.createDocument(x, "5", 2));
		  //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
		}
		writer.flush(false, false);
		for (int x = 25; x < 30; x++)
		{
		  writer.addDocument(DocHelper.createDocument(x, "5", 2));
		  //System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
		}
		writer.flush(false, false);

		//System.out.println("infos3:"+writer.segmentInfos);

		Term delterm = new Term("id", "8");
		writer.deleteDocuments(delterm);
		//System.out.println("segdels3:" + writer.docWriter.deletesToString());

		fsmp.DoMerge = true;
		fsmp.Start = 1;
		fsmp.Length = 2;
		writer.maybeMerge();

		// deletes for info1, the newly created segment from the
		// merge should have no deletes because they were applied in
		// the merge
		//SegmentInfo info1 = writer.segmentInfos.info(1);
		//Assert.IsFalse(exists(info1, writer.docWriter.segmentDeletes));

		//System.out.println("infos4:"+writer.segmentInfos);
		//System.out.println("segdels4:" + writer.docWriter.deletesToString());
	  }

	  internal virtual bool SegThere(SegmentCommitInfo info, SegmentInfos infos)
	  {
		foreach (SegmentCommitInfo si in infos)
		{
		  if (si.info.name.Equals(info.info.name))
		  {
			  return true;
		  }
		}
		return false;
	  }

	  public static void PrintDelDocs(Bits bits)
	  {
		if (bits == null)
		{
			return;
		}
		for (int x = 0; x < bits.length(); x++)
		{
		  Console.WriteLine(x + ":" + bits.get(x));
		}
	  }

	  public virtual int[] ToDocsArray(Term term, Bits bits, IndexReader reader)
	  {
		Fields fields = MultiFields.getFields(reader);
		Terms cterms = fields.terms(term.field);
		TermsEnum ctermsEnum = cterms.iterator(null);
		if (ctermsEnum.seekExact(new BytesRef(term.text())))
		{
		  DocsEnum docsEnum = TestUtil.docs(random(), ctermsEnum, bits, null, DocsEnum.FLAG_NONE);
		  return ToArray(docsEnum);
		}
		return null;
	  }

	  public static int[] ToArray(DocsEnum docsEnum)
	  {
		IList<int?> docs = new List<int?>();
		while (docsEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		{
		  int docID = docsEnum.docID();
		  docs.Add(docID);
		}
		return ArrayUtil.toIntArray(docs);
	  }

	  public class RangeMergePolicy : MergePolicy
	  {
		  private readonly TestPerSegmentDeletes OuterInstance;

		internal bool DoMerge = false;
		internal int Start;
		internal int Length;

		internal readonly bool UseCompoundFile_Renamed;

		internal RangeMergePolicy(TestPerSegmentDeletes outerInstance, bool useCompoundFile)
		{
			this.OuterInstance = outerInstance;
		  this.UseCompoundFile_Renamed = useCompoundFile;
		}

		public override void Close()
		{
		}
		public override MergeSpecification FindMerges(MergeTrigger mergeTrigger, SegmentInfos segmentInfos)
		{
		  MergeSpecification ms = new MergeSpecification();
		  if (DoMerge)
		  {
			OneMerge om = new OneMerge(segmentInfos.asList().subList(Start, Start + Length));
			ms.add(om);
			DoMerge = false;
			return ms;
		  }
		  return null;
		}

		public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool?> segmentsToMerge)
		{
		  return null;
		}

		public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
		{
		  return null;
		}

		public override bool UseCompoundFile(SegmentInfos segments, SegmentCommitInfo newSegment)
		{
		  return UseCompoundFile_Renamed;
		}
	  }
	}

}