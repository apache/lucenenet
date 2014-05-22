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


	using Codec = Lucene.Net.Codecs.Codec;
	using Document = Lucene.Net.Document.Document;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using Constants = Lucene.Net.Util.Constants;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using InfoStream = Lucene.Net.Util.InfoStream;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestSegmentMerger : LuceneTestCase
	{
	  //The variables for the new merged segment
	  private Directory MergedDir;
	  private string MergedSegment = "test";
	  //First segment to be merged
	  private Directory Merge1Dir;
	  private Document Doc1 = new Document();
	  private SegmentReader Reader1 = null;
	  //Second Segment to be merged
	  private Directory Merge2Dir;
	  private Document Doc2 = new Document();
	  private SegmentReader Reader2 = null;

	  public override void SetUp()
	  {
		base.setUp();
		MergedDir = newDirectory();
		Merge1Dir = newDirectory();
		Merge2Dir = newDirectory();
		DocHelper.setupDoc(Doc1);
		SegmentCommitInfo info1 = DocHelper.writeDoc(random(), Merge1Dir, Doc1);
		DocHelper.setupDoc(Doc2);
		SegmentCommitInfo info2 = DocHelper.writeDoc(random(), Merge2Dir, Doc2);
		Reader1 = new SegmentReader(info1, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, newIOContext(random()));
		Reader2 = new SegmentReader(info2, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, newIOContext(random()));
	  }

	  public override void TearDown()
	  {
		Reader1.close();
		Reader2.close();
		MergedDir.close();
		Merge1Dir.close();
		Merge2Dir.close();
		base.tearDown();
	  }

	  public virtual void Test()
	  {
		Assert.IsTrue(MergedDir != null);
		Assert.IsTrue(Merge1Dir != null);
		Assert.IsTrue(Merge2Dir != null);
		Assert.IsTrue(Reader1 != null);
		Assert.IsTrue(Reader2 != null);
	  }

	  public virtual void TestMerge()
	  {
		Codec codec = Codec.Default;
		SegmentInfo si = new SegmentInfo(MergedDir, Constants.LUCENE_MAIN_VERSION, MergedSegment, -1, false, codec, null);

		SegmentMerger merger = new SegmentMerger(Arrays.asList<AtomicReader>(Reader1, Reader2), si, InfoStream.Default, MergedDir, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL, MergeState.CheckAbort.NONE, new FieldInfos.FieldNumbers(), newIOContext(random()), true);
		MergeState mergeState = merger.merge();
		int docsMerged = mergeState.segmentInfo.DocCount;
		Assert.IsTrue(docsMerged == 2);
		//Should be able to open a new SegmentReader against the new directory
		SegmentReader mergedReader = new SegmentReader(new SegmentCommitInfo(new SegmentInfo(MergedDir, Constants.LUCENE_MAIN_VERSION, MergedSegment, docsMerged, false, codec, null), 0, -1L, -1L), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, newIOContext(random()));
		Assert.IsTrue(mergedReader != null);
		Assert.IsTrue(mergedReader.numDocs() == 2);
		Document newDoc1 = mergedReader.document(0);
		Assert.IsTrue(newDoc1 != null);
		//There are 2 unstored fields on the document
		Assert.IsTrue(DocHelper.numFields(newDoc1) == DocHelper.numFields(Doc1) - DocHelper.unstored.size());
		Document newDoc2 = mergedReader.document(1);
		Assert.IsTrue(newDoc2 != null);
		Assert.IsTrue(DocHelper.numFields(newDoc2) == DocHelper.numFields(Doc2) - DocHelper.unstored.size());

		DocsEnum termDocs = TestUtil.docs(random(), mergedReader, DocHelper.TEXT_FIELD_2_KEY, new BytesRef("field"), MultiFields.getLiveDocs(mergedReader), null, 0);
		Assert.IsTrue(termDocs != null);
		Assert.IsTrue(termDocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);

		int tvCount = 0;
		foreach (FieldInfo fieldInfo in mergedReader.FieldInfos)
		{
		  if (fieldInfo.hasVectors())
		  {
			tvCount++;
		  }
		}

		//System.out.println("stored size: " + stored.size());
		Assert.AreEqual("We do not have 3 fields that were indexed with term vector", 3, tvCount);

		Terms vector = mergedReader.getTermVectors(0).terms(DocHelper.TEXT_FIELD_2_KEY);
		Assert.IsNotNull(vector);
		Assert.AreEqual(3, vector.size());
		TermsEnum termsEnum = vector.iterator(null);

		int i = 0;
		while (termsEnum.next() != null)
		{
		  string term = termsEnum.term().utf8ToString();
		  int freq = (int) termsEnum.totalTermFreq();
		  //System.out.println("Term: " + term + " Freq: " + freq);
		  Assert.IsTrue(DocHelper.FIELD_2_TEXT.IndexOf(term) != -1);
		  Assert.IsTrue(DocHelper.FIELD_2_FREQS[i] == freq);
		  i++;
		}

		TestSegmentReader.CheckNorms(mergedReader);
		mergedReader.close();
	  }

	  private static bool Equals(MergeState.DocMap map1, MergeState.DocMap map2)
	  {
		if (map1.maxDoc() != map2.maxDoc())
		{
		  return false;
		}
		for (int i = 0; i < map1.maxDoc(); ++i)
		{
		  if (map1.get(i) != map2.get(i))
		  {
			return false;
		  }
		}
		return true;
	  }

	  public virtual void TestBuildDocMap()
	  {
		int maxDoc = TestUtil.Next(random(), 1, 128);
		int numDocs = TestUtil.Next(random(), 0, maxDoc);
		int numDeletedDocs = maxDoc - numDocs;
		FixedBitSet liveDocs = new FixedBitSet(maxDoc);
		for (int i = 0; i < numDocs; ++i)
		{
		  while (true)
		  {
			int docID = random().Next(maxDoc);
			if (!liveDocs.get(docID))
			{
			  liveDocs.set(docID);
			  break;
			}
		  }
		}

		MergeState.DocMap docMap = MergeState.DocMap.build(maxDoc, liveDocs);

		Assert.AreEqual(maxDoc, docMap.maxDoc());
		Assert.AreEqual(numDocs, docMap.numDocs());
		Assert.AreEqual(numDeletedDocs, docMap.numDeletedDocs());
		// assert the mapping is compact
		for (int i = 0, del = 0; i < maxDoc; ++i)
		{
		  if (!liveDocs.get(i))
		  {
			Assert.AreEqual(-1, docMap.get(i));
			++del;
		  }
		  else
		  {
			Assert.AreEqual(i - del, docMap.get(i));
		  }
		}
	  }
	}

}