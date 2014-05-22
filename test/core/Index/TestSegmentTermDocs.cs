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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestSegmentTermDocs : LuceneTestCase
	{
	  private Document TestDoc = new Document();
	  private Directory Dir;
	  private SegmentCommitInfo Info;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		DocHelper.setupDoc(TestDoc);
		Info = DocHelper.writeDoc(random(), Dir, TestDoc);
	  }

	  public override void TearDown()
	  {
		Dir.close();
		base.tearDown();
	  }

	  public virtual void Test()
	  {
		Assert.IsTrue(Dir != null);
	  }

	  public virtual void TestTermDocs()
	  {
		TestTermDocs(1);
	  }

	  public virtual void TestTermDocs(int indexDivisor)
	  {
		//After adding the document, we should be able to read it back in
		SegmentReader reader = new SegmentReader(Info, indexDivisor, newIOContext(random()));
		Assert.IsTrue(reader != null);
		Assert.AreEqual(indexDivisor, reader.TermInfosIndexDivisor);

		TermsEnum terms = reader.fields().terms(DocHelper.TEXT_FIELD_2_KEY).iterator(null);
		terms.seekCeil(new BytesRef("field"));
		DocsEnum termDocs = TestUtil.docs(random(), terms, reader.LiveDocs, null, DocsEnum.FLAG_FREQS);
		if (termDocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		{
		  int docId = termDocs.docID();
		  Assert.IsTrue(docId == 0);
		  int freq = termDocs.freq();
		  Assert.IsTrue(freq == 3);
		}
		reader.close();
	  }

	  public virtual void TestBadSeek()
	  {
		TestBadSeek(1);
	  }

	  public virtual void TestBadSeek(int indexDivisor)
	  {
	  {
		  //After adding the document, we should be able to read it back in
		  SegmentReader reader = new SegmentReader(Info, indexDivisor, newIOContext(random()));
		  Assert.IsTrue(reader != null);
		  DocsEnum termDocs = TestUtil.docs(random(), reader, "textField2", new BytesRef("bad"), reader.LiveDocs, null, 0);

		  assertNull(termDocs);
		  reader.close();
		}
		{
		  //After adding the document, we should be able to read it back in
		  SegmentReader reader = new SegmentReader(Info, indexDivisor, newIOContext(random()));
		  Assert.IsTrue(reader != null);
		  DocsEnum termDocs = TestUtil.docs(random(), reader, "junk", new BytesRef("bad"), reader.LiveDocs, null, 0);
		  assertNull(termDocs);
		  reader.close();
		}
	  }

	  public virtual void TestSkipTo()
	  {
		TestSkipTo(1);
	  }

	  public virtual void TestSkipTo(int indexDivisor)
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));

		Term ta = new Term("content","aaa");
		for (int i = 0; i < 10; i++)
		{
		  AddDoc(writer, "aaa aaa aaa aaa");
		}

		Term tb = new Term("content","bbb");
		for (int i = 0; i < 16; i++)
		{
		  AddDoc(writer, "bbb bbb bbb bbb");
		}

		Term tc = new Term("content","ccc");
		for (int i = 0; i < 50; i++)
		{
		  AddDoc(writer, "ccc ccc ccc ccc");
		}

		// assure that we deal with a single segment  
		writer.forceMerge(1);
		writer.close();

		IndexReader reader = DirectoryReader.open(dir, indexDivisor);

		DocsEnum tdocs = TestUtil.docs(random(), reader, ta.field(), new BytesRef(ta.text()), MultiFields.getLiveDocs(reader), null, DocsEnum.FLAG_FREQS);

		// without optimization (assumption skipInterval == 16)

		// with next
		Assert.IsTrue(tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(0, tdocs.docID());
		Assert.AreEqual(4, tdocs.freq());
		Assert.IsTrue(tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(1, tdocs.docID());
		Assert.AreEqual(4, tdocs.freq());
		Assert.IsTrue(tdocs.advance(2) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(2, tdocs.docID());
		Assert.IsTrue(tdocs.advance(4) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(4, tdocs.docID());
		Assert.IsTrue(tdocs.advance(9) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(9, tdocs.docID());
		Assert.IsFalse(tdocs.advance(10) != DocIdSetIterator.NO_MORE_DOCS);

		// without next
		tdocs = TestUtil.docs(random(), reader, ta.field(), new BytesRef(ta.text()), MultiFields.getLiveDocs(reader), null, 0);

		Assert.IsTrue(tdocs.advance(0) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(0, tdocs.docID());
		Assert.IsTrue(tdocs.advance(4) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(4, tdocs.docID());
		Assert.IsTrue(tdocs.advance(9) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(9, tdocs.docID());
		Assert.IsFalse(tdocs.advance(10) != DocIdSetIterator.NO_MORE_DOCS);

		// exactly skipInterval documents and therefore with optimization

		// with next
		tdocs = TestUtil.docs(random(), reader, tb.field(), new BytesRef(tb.text()), MultiFields.getLiveDocs(reader), null, DocsEnum.FLAG_FREQS);

		Assert.IsTrue(tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(10, tdocs.docID());
		Assert.AreEqual(4, tdocs.freq());
		Assert.IsTrue(tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(11, tdocs.docID());
		Assert.AreEqual(4, tdocs.freq());
		Assert.IsTrue(tdocs.advance(12) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(12, tdocs.docID());
		Assert.IsTrue(tdocs.advance(15) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(15, tdocs.docID());
		Assert.IsTrue(tdocs.advance(24) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(24, tdocs.docID());
		Assert.IsTrue(tdocs.advance(25) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(25, tdocs.docID());
		Assert.IsFalse(tdocs.advance(26) != DocIdSetIterator.NO_MORE_DOCS);

		// without next
		tdocs = TestUtil.docs(random(), reader, tb.field(), new BytesRef(tb.text()), MultiFields.getLiveDocs(reader), null, DocsEnum.FLAG_FREQS);

		Assert.IsTrue(tdocs.advance(5) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(10, tdocs.docID());
		Assert.IsTrue(tdocs.advance(15) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(15, tdocs.docID());
		Assert.IsTrue(tdocs.advance(24) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(24, tdocs.docID());
		Assert.IsTrue(tdocs.advance(25) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(25, tdocs.docID());
		Assert.IsFalse(tdocs.advance(26) != DocIdSetIterator.NO_MORE_DOCS);

		// much more than skipInterval documents and therefore with optimization

		// with next
		tdocs = TestUtil.docs(random(), reader, tc.field(), new BytesRef(tc.text()), MultiFields.getLiveDocs(reader), null, DocsEnum.FLAG_FREQS);

		Assert.IsTrue(tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(26, tdocs.docID());
		Assert.AreEqual(4, tdocs.freq());
		Assert.IsTrue(tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(27, tdocs.docID());
		Assert.AreEqual(4, tdocs.freq());
		Assert.IsTrue(tdocs.advance(28) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(28, tdocs.docID());
		Assert.IsTrue(tdocs.advance(40) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(40, tdocs.docID());
		Assert.IsTrue(tdocs.advance(57) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(57, tdocs.docID());
		Assert.IsTrue(tdocs.advance(74) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(74, tdocs.docID());
		Assert.IsTrue(tdocs.advance(75) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(75, tdocs.docID());
		Assert.IsFalse(tdocs.advance(76) != DocIdSetIterator.NO_MORE_DOCS);

		//without next
		tdocs = TestUtil.docs(random(), reader, tc.field(), new BytesRef(tc.text()), MultiFields.getLiveDocs(reader), null, 0);
		Assert.IsTrue(tdocs.advance(5) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(26, tdocs.docID());
		Assert.IsTrue(tdocs.advance(40) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(40, tdocs.docID());
		Assert.IsTrue(tdocs.advance(57) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(57, tdocs.docID());
		Assert.IsTrue(tdocs.advance(74) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(74, tdocs.docID());
		Assert.IsTrue(tdocs.advance(75) != DocIdSetIterator.NO_MORE_DOCS);
		Assert.AreEqual(75, tdocs.docID());
		Assert.IsFalse(tdocs.advance(76) != DocIdSetIterator.NO_MORE_DOCS);

		reader.close();
		dir.close();
	  }

	  public virtual void TestIndexDivisor()
	  {
		TestDoc = new Document();
		DocHelper.setupDoc(TestDoc);
		DocHelper.writeDoc(random(), Dir, TestDoc);
		TestTermDocs(2);
		TestBadSeek(2);
		TestSkipTo(2);
	  }

	  private void AddDoc(IndexWriter writer, string value)
	  {
		  Document doc = new Document();
		  doc.add(newTextField("content", value, Field.Store.NO));
		  writer.addDocument(doc);
	  }
	}

}