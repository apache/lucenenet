using System;

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
	using Occur = Lucene.Net.Search.BooleanClause.Occur;
	using Lucene.Net.Search;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestParallelAtomicReader : LuceneTestCase
	{

	  private IndexSearcher Parallel_Renamed, Single_Renamed;
	  private Directory Dir, Dir1, Dir2;

	  public virtual void TestQueries()
	  {
		Single_Renamed = Single(random());
		Parallel_Renamed = Parallel(random());

		QueryTest(new TermQuery(new Term("f1", "v1")));
		QueryTest(new TermQuery(new Term("f1", "v2")));
		QueryTest(new TermQuery(new Term("f2", "v1")));
		QueryTest(new TermQuery(new Term("f2", "v2")));
		QueryTest(new TermQuery(new Term("f3", "v1")));
		QueryTest(new TermQuery(new Term("f3", "v2")));
		QueryTest(new TermQuery(new Term("f4", "v1")));
		QueryTest(new TermQuery(new Term("f4", "v2")));

		BooleanQuery bq1 = new BooleanQuery();
		bq1.add(new TermQuery(new Term("f1", "v1")), Occur.MUST);
		bq1.add(new TermQuery(new Term("f4", "v1")), Occur.MUST);
		QueryTest(bq1);

		Single_Renamed.IndexReader.close();
		Single_Renamed = null;
		Parallel_Renamed.IndexReader.close();
		Parallel_Renamed = null;
		Dir.close();
		Dir = null;
		Dir1.close();
		Dir1 = null;
		Dir2.close();
		Dir2 = null;
	  }

	  public virtual void TestFieldNames()
	  {
		Directory dir1 = GetDir1(random());
		Directory dir2 = GetDir2(random());
		ParallelAtomicReader pr = new ParallelAtomicReader(SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir1)), SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir2)));
		FieldInfos fieldInfos = pr.FieldInfos;
		Assert.AreEqual(4, fieldInfos.size());
		Assert.IsNotNull(fieldInfos.fieldInfo("f1"));
		Assert.IsNotNull(fieldInfos.fieldInfo("f2"));
		Assert.IsNotNull(fieldInfos.fieldInfo("f3"));
		Assert.IsNotNull(fieldInfos.fieldInfo("f4"));
		pr.close();
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestRefCounts1()
	  {
		Directory dir1 = GetDir1(random());
		Directory dir2 = GetDir2(random());
		AtomicReader ir1, ir2;
		// close subreaders, ParallelReader will not change refCounts, but close on its own close
		ParallelAtomicReader pr = new ParallelAtomicReader(ir1 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir1)), ir2 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir2)));

		// check RefCounts
		Assert.AreEqual(1, ir1.RefCount);
		Assert.AreEqual(1, ir2.RefCount);
		pr.close();
		Assert.AreEqual(0, ir1.RefCount);
		Assert.AreEqual(0, ir2.RefCount);
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestRefCounts2()
	  {
		Directory dir1 = GetDir1(random());
		Directory dir2 = GetDir2(random());
		AtomicReader ir1 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir1));
		AtomicReader ir2 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir2));
		// don't close subreaders, so ParallelReader will increment refcounts
		ParallelAtomicReader pr = new ParallelAtomicReader(false, ir1, ir2);
		// check RefCounts
		Assert.AreEqual(2, ir1.RefCount);
		Assert.AreEqual(2, ir2.RefCount);
		pr.close();
		Assert.AreEqual(1, ir1.RefCount);
		Assert.AreEqual(1, ir2.RefCount);
		ir1.close();
		ir2.close();
		Assert.AreEqual(0, ir1.RefCount);
		Assert.AreEqual(0, ir2.RefCount);
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestCloseInnerReader()
	  {
		Directory dir1 = GetDir1(random());
		AtomicReader ir1 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir1));

		// with overlapping
		ParallelAtomicReader pr = new ParallelAtomicReader(true, new AtomicReader[] {ir1}, new AtomicReader[] {ir1});

		ir1.close();

		try
		{
		  pr.document(0);
		  Assert.Fail("ParallelAtomicReader should be already closed because inner reader was closed!");
		}
		catch (AlreadyClosedException e)
		{
		  // pass
		}

		// noop:
		pr.close();
		dir1.close();
	  }

	  public virtual void TestIncompatibleIndexes()
	  {
		// two documents:
		Directory dir1 = GetDir1(random());

		// one document only:
		Directory dir2 = newDirectory();
		IndexWriter w2 = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document d3 = new Document();

		d3.add(newTextField("f3", "v1", Field.Store.YES));
		w2.addDocument(d3);
		w2.close();

		AtomicReader ir1 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir1));
		AtomicReader ir2 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir2));

		try
		{
		  new ParallelAtomicReader(ir1, ir2);
		  Assert.Fail("didn't get exptected exception: indexes don't have same number of documents");
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}

		try
		{
		  new ParallelAtomicReader(random().nextBoolean(), new AtomicReader[] {ir1, ir2}, new AtomicReader[] {ir1, ir2});
		  Assert.Fail("didn't get expected exception: indexes don't have same number of documents");
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}
		// check RefCounts
		Assert.AreEqual(1, ir1.RefCount);
		Assert.AreEqual(1, ir2.RefCount);
		ir1.close();
		ir2.close();
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestIgnoreStoredFields()
	  {
		Directory dir1 = GetDir1(random());
		Directory dir2 = GetDir2(random());
		AtomicReader ir1 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir1));
		AtomicReader ir2 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir2));

		// with overlapping
		ParallelAtomicReader pr = new ParallelAtomicReader(false, new AtomicReader[] {ir1, ir2}, new AtomicReader[] {ir1});
		Assert.AreEqual("v1", pr.document(0).get("f1"));
		Assert.AreEqual("v1", pr.document(0).get("f2"));
		assertNull(pr.document(0).get("f3"));
		assertNull(pr.document(0).get("f4"));
		// check that fields are there
		Assert.IsNotNull(pr.terms("f1"));
		Assert.IsNotNull(pr.terms("f2"));
		Assert.IsNotNull(pr.terms("f3"));
		Assert.IsNotNull(pr.terms("f4"));
		pr.close();

		// no stored fields at all
		pr = new ParallelAtomicReader(false, new AtomicReader[] {ir2}, new AtomicReader[0]);
		assertNull(pr.document(0).get("f1"));
		assertNull(pr.document(0).get("f2"));
		assertNull(pr.document(0).get("f3"));
		assertNull(pr.document(0).get("f4"));
		// check that fields are there
		assertNull(pr.terms("f1"));
		assertNull(pr.terms("f2"));
		Assert.IsNotNull(pr.terms("f3"));
		Assert.IsNotNull(pr.terms("f4"));
		pr.close();

		// without overlapping
		pr = new ParallelAtomicReader(true, new AtomicReader[] {ir2}, new AtomicReader[] {ir1});
		Assert.AreEqual("v1", pr.document(0).get("f1"));
		Assert.AreEqual("v1", pr.document(0).get("f2"));
		assertNull(pr.document(0).get("f3"));
		assertNull(pr.document(0).get("f4"));
		// check that fields are there
		assertNull(pr.terms("f1"));
		assertNull(pr.terms("f2"));
		Assert.IsNotNull(pr.terms("f3"));
		Assert.IsNotNull(pr.terms("f4"));
		pr.close();

		// no main readers
		try
		{
		  new ParallelAtomicReader(true, new AtomicReader[0], new AtomicReader[] {ir1});
		  Assert.Fail("didn't get expected exception: need a non-empty main-reader array");
		}
		catch (System.ArgumentException iae)
		{
		  // pass
		}

		dir1.close();
		dir2.close();
	  }

	  private void QueryTest(Query query)
	  {
		ScoreDoc[] parallelHits = Parallel_Renamed.search(query, null, 1000).scoreDocs;
		ScoreDoc[] singleHits = Single_Renamed.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(parallelHits.Length, singleHits.Length);
		for (int i = 0; i < parallelHits.Length; i++)
		{
		  Assert.AreEqual(parallelHits[i].score, singleHits[i].score, 0.001f);
		  Document docParallel = Parallel_Renamed.doc(parallelHits[i].doc);
		  Document docSingle = Single_Renamed.doc(singleHits[i].doc);
		  Assert.AreEqual(docParallel.get("f1"), docSingle.get("f1"));
		  Assert.AreEqual(docParallel.get("f2"), docSingle.get("f2"));
		  Assert.AreEqual(docParallel.get("f3"), docSingle.get("f3"));
		  Assert.AreEqual(docParallel.get("f4"), docSingle.get("f4"));
		}
	  }

	  // Fields 1-4 indexed together:
	  private IndexSearcher Single(Random random)
	  {
		Dir = newDirectory();
		IndexWriter w = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)));
		Document d1 = new Document();
		d1.add(newTextField("f1", "v1", Field.Store.YES));
		d1.add(newTextField("f2", "v1", Field.Store.YES));
		d1.add(newTextField("f3", "v1", Field.Store.YES));
		d1.add(newTextField("f4", "v1", Field.Store.YES));
		w.addDocument(d1);
		Document d2 = new Document();
		d2.add(newTextField("f1", "v2", Field.Store.YES));
		d2.add(newTextField("f2", "v2", Field.Store.YES));
		d2.add(newTextField("f3", "v2", Field.Store.YES));
		d2.add(newTextField("f4", "v2", Field.Store.YES));
		w.addDocument(d2);
		w.close();

		DirectoryReader ir = DirectoryReader.open(Dir);
		return newSearcher(ir);
	  }

	  // Fields 1 & 2 in one index, 3 & 4 in other, with ParallelReader:
	  private IndexSearcher Parallel(Random random)
	  {
		Dir1 = GetDir1(random);
		Dir2 = GetDir2(random);
		ParallelAtomicReader pr = new ParallelAtomicReader(SlowCompositeReaderWrapper.wrap(DirectoryReader.open(Dir1)), SlowCompositeReaderWrapper.wrap(DirectoryReader.open(Dir2)));
		TestUtil.checkReader(pr);
		return newSearcher(pr);
	  }

	  private Directory GetDir1(Random random)
	  {
		Directory dir1 = newDirectory();
		IndexWriter w1 = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)));
		Document d1 = new Document();
		d1.add(newTextField("f1", "v1", Field.Store.YES));
		d1.add(newTextField("f2", "v1", Field.Store.YES));
		w1.addDocument(d1);
		Document d2 = new Document();
		d2.add(newTextField("f1", "v2", Field.Store.YES));
		d2.add(newTextField("f2", "v2", Field.Store.YES));
		w1.addDocument(d2);
		w1.close();
		return dir1;
	  }

	  private Directory GetDir2(Random random)
	  {
		Directory dir2 = newDirectory();
		IndexWriter w2 = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)));
		Document d3 = new Document();
		d3.add(newTextField("f3", "v1", Field.Store.YES));
		d3.add(newTextField("f4", "v1", Field.Store.YES));
		w2.addDocument(d3);
		Document d4 = new Document();
		d4.add(newTextField("f3", "v2", Field.Store.YES));
		d4.add(newTextField("f4", "v2", Field.Store.YES));
		w2.addDocument(d4);
		w2.close();
		return dir2;
	  }

	}

}