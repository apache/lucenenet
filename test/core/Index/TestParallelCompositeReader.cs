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
	using ReaderClosedListener = Lucene.Net.Index.IndexReader.ReaderClosedListener;
	using Occur = Lucene.Net.Search.BooleanClause.Occur_e;
	using Lucene.Net.Search;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestParallelCompositeReader : LuceneTestCase
	{

	  private IndexSearcher Parallel_Renamed, Single_Renamed;
	  private Directory Dir, Dir1, Dir2;

	  public virtual void TestQueries()
	  {
		Single_Renamed = Single(random(), false);
		Parallel_Renamed = Parallel(random(), false);

		Queries();

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

	  public virtual void TestQueriesCompositeComposite()
	  {
		Single_Renamed = Single(random(), true);
		Parallel_Renamed = Parallel(random(), true);

		Queries();

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

	  private void Queries()
	  {
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
	  }

	  public virtual void TestRefCounts1()
	  {
		Directory dir1 = GetDir1(random());
		Directory dir2 = GetDir2(random());
		DirectoryReader ir1, ir2;
		// close subreaders, ParallelReader will not change refCounts, but close on its own close
		ParallelCompositeReader pr = new ParallelCompositeReader(ir1 = DirectoryReader.open(dir1), ir2 = DirectoryReader.open(dir2));
		IndexReader psub1 = pr.SequentialSubReaders.get(0);
		// check RefCounts
		Assert.AreEqual(1, ir1.RefCount);
		Assert.AreEqual(1, ir2.RefCount);
		Assert.AreEqual(1, psub1.RefCount);
		pr.close();
		Assert.AreEqual(0, ir1.RefCount);
		Assert.AreEqual(0, ir2.RefCount);
		Assert.AreEqual(0, psub1.RefCount);
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestRefCounts2()
	  {
		Directory dir1 = GetDir1(random());
		Directory dir2 = GetDir2(random());
		DirectoryReader ir1 = DirectoryReader.open(dir1);
		DirectoryReader ir2 = DirectoryReader.open(dir2);

		// don't close subreaders, so ParallelReader will increment refcounts
		ParallelCompositeReader pr = new ParallelCompositeReader(false, ir1, ir2);
		IndexReader psub1 = pr.SequentialSubReaders.get(0);
		// check RefCounts
		Assert.AreEqual(2, ir1.RefCount);
		Assert.AreEqual(2, ir2.RefCount);
		Assert.AreEqual("refCount must be 1, as the synthetic reader was created by ParallelCompositeReader", 1, psub1.RefCount);
		pr.close();
		Assert.AreEqual(1, ir1.RefCount);
		Assert.AreEqual(1, ir2.RefCount);
		Assert.AreEqual("refcount must be 0 because parent was closed", 0, psub1.RefCount);
		ir1.close();
		ir2.close();
		Assert.AreEqual(0, ir1.RefCount);
		Assert.AreEqual(0, ir2.RefCount);
		Assert.AreEqual("refcount should not change anymore", 0, psub1.RefCount);
		dir1.close();
		dir2.close();
	  }

	  // closeSubreaders=false
	  public virtual void TestReaderClosedListener1()
	  {
		Directory dir1 = GetDir1(random());
		CompositeReader ir1 = DirectoryReader.open(dir1);

		// with overlapping
		ParallelCompositeReader pr = new ParallelCompositeReader(false, new CompositeReader[] {ir1}, new CompositeReader[] {ir1});

		int[] listenerClosedCount = new int[1];

		Assert.AreEqual(3, pr.leaves().size());

		foreach (AtomicReaderContext cxt in pr.leaves())
		{
		  cxt.reader().addReaderClosedListener(new ReaderClosedListenerAnonymousInnerClassHelper(this, listenerClosedCount));
		}
		pr.close();
		ir1.close();
		Assert.AreEqual(3, listenerClosedCount[0]);
		dir1.close();
	  }

	  private class ReaderClosedListenerAnonymousInnerClassHelper : ReaderClosedListener
	  {
		  private readonly TestParallelCompositeReader OuterInstance;

		  private int[] ListenerClosedCount;

		  public ReaderClosedListenerAnonymousInnerClassHelper(TestParallelCompositeReader outerInstance, int[] listenerClosedCount)
		  {
			  this.OuterInstance = outerInstance;
			  this.ListenerClosedCount = listenerClosedCount;
		  }

		  public override void OnClose(IndexReader reader)
		  {
			ListenerClosedCount[0]++;
		  }
	  }

	  // closeSubreaders=true
	  public virtual void TestReaderClosedListener2()
	  {
		Directory dir1 = GetDir1(random());
		CompositeReader ir1 = DirectoryReader.open(dir1);

		// with overlapping
		ParallelCompositeReader pr = new ParallelCompositeReader(true, new CompositeReader[] {ir1}, new CompositeReader[] {ir1});

		int[] listenerClosedCount = new int[1];

		Assert.AreEqual(3, pr.leaves().size());

		foreach (AtomicReaderContext cxt in pr.leaves())
		{
		  cxt.reader().addReaderClosedListener(new ReaderClosedListenerAnonymousInnerClassHelper2(this, listenerClosedCount));
		}
		pr.close();
		Assert.AreEqual(3, listenerClosedCount[0]);
		dir1.close();
	  }

	  private class ReaderClosedListenerAnonymousInnerClassHelper2 : ReaderClosedListener
	  {
		  private readonly TestParallelCompositeReader OuterInstance;

		  private int[] ListenerClosedCount;

		  public ReaderClosedListenerAnonymousInnerClassHelper2(TestParallelCompositeReader outerInstance, int[] listenerClosedCount)
		  {
			  this.OuterInstance = outerInstance;
			  this.ListenerClosedCount = listenerClosedCount;
		  }

		  public override void OnClose(IndexReader reader)
		  {
			ListenerClosedCount[0]++;
		  }
	  }

	  public virtual void TestCloseInnerReader()
	  {
		Directory dir1 = GetDir1(random());
		CompositeReader ir1 = DirectoryReader.open(dir1);
		Assert.AreEqual(1, ir1.SequentialSubReaders.get(0).RefCount);

		// with overlapping
		ParallelCompositeReader pr = new ParallelCompositeReader(true, new CompositeReader[] {ir1}, new CompositeReader[] {ir1});

		IndexReader psub = pr.SequentialSubReaders.get(0);
		Assert.AreEqual(1, psub.RefCount);

		ir1.close();

		Assert.AreEqual("refCount of synthetic subreader should be unchanged", 1, psub.RefCount);
		try
		{
		  psub.document(0);
		  Assert.Fail("Subreader should be already closed because inner reader was closed!");
		}
		catch (AlreadyClosedException e)
		{
		  // pass
		}

		try
		{
		  pr.document(0);
		  Assert.Fail("ParallelCompositeReader should be already closed because inner reader was closed!");
		}
		catch (AlreadyClosedException e)
		{
		  // pass
		}

		// noop:
		pr.close();
		Assert.AreEqual(0, psub.RefCount);
		dir1.close();
	  }

	  public virtual void TestIncompatibleIndexes1()
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

		DirectoryReader ir1 = DirectoryReader.open(dir1), ir2 = DirectoryReader.open(dir2);
		try
		{
		  new ParallelCompositeReader(ir1, ir2);
		  Assert.Fail("didn't get expected exception: indexes don't have same number of documents");
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}
		try
		{
		  new ParallelCompositeReader(random().nextBoolean(), ir1, ir2);
		  Assert.Fail("didn't get expected exception: indexes don't have same number of documents");
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}
		Assert.AreEqual(1, ir1.RefCount);
		Assert.AreEqual(1, ir2.RefCount);
		ir1.close();
		ir2.close();
		Assert.AreEqual(0, ir1.RefCount);
		Assert.AreEqual(0, ir2.RefCount);
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestIncompatibleIndexes2()
	  {
		Directory dir1 = GetDir1(random());
		Directory dir2 = GetInvalidStructuredDir2(random());

		DirectoryReader ir1 = DirectoryReader.open(dir1), ir2 = DirectoryReader.open(dir2);
		CompositeReader[] readers = new CompositeReader[] {ir1, ir2};
		try
		{
		  new ParallelCompositeReader(readers);
		  Assert.Fail("didn't get expected exception: indexes don't have same subreader structure");
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}
		try
		{
		  new ParallelCompositeReader(random().nextBoolean(), readers, readers);
		  Assert.Fail("didn't get expected exception: indexes don't have same subreader structure");
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}
		Assert.AreEqual(1, ir1.RefCount);
		Assert.AreEqual(1, ir2.RefCount);
		ir1.close();
		ir2.close();
		Assert.AreEqual(0, ir1.RefCount);
		Assert.AreEqual(0, ir2.RefCount);
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestIncompatibleIndexes3()
	  {
		Directory dir1 = GetDir1(random());
		Directory dir2 = GetDir2(random());

		CompositeReader ir1 = new MultiReader(DirectoryReader.open(dir1), SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir1))), ir2 = new MultiReader(DirectoryReader.open(dir2), DirectoryReader.open(dir2));
		CompositeReader[] readers = new CompositeReader[] {ir1, ir2};
		try
		{
		  new ParallelCompositeReader(readers);
		  Assert.Fail("didn't get expected exception: indexes don't have same subreader structure");
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}
		try
		{
		  new ParallelCompositeReader(random().nextBoolean(), readers, readers);
		  Assert.Fail("didn't get expected exception: indexes don't have same subreader structure");
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}
		Assert.AreEqual(1, ir1.RefCount);
		Assert.AreEqual(1, ir2.RefCount);
		ir1.close();
		ir2.close();
		Assert.AreEqual(0, ir1.RefCount);
		Assert.AreEqual(0, ir2.RefCount);
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestIgnoreStoredFields()
	  {
		Directory dir1 = GetDir1(random());
		Directory dir2 = GetDir2(random());
		CompositeReader ir1 = DirectoryReader.open(dir1);
		CompositeReader ir2 = DirectoryReader.open(dir2);

		// with overlapping
		ParallelCompositeReader pr = new ParallelCompositeReader(false, new CompositeReader[] {ir1, ir2}, new CompositeReader[] {ir1});
		Assert.AreEqual("v1", pr.document(0).get("f1"));
		Assert.AreEqual("v1", pr.document(0).get("f2"));
		assertNull(pr.document(0).get("f3"));
		assertNull(pr.document(0).get("f4"));
		// check that fields are there
		AtomicReader slow = SlowCompositeReaderWrapper.wrap(pr);
		Assert.IsNotNull(slow.terms("f1"));
		Assert.IsNotNull(slow.terms("f2"));
		Assert.IsNotNull(slow.terms("f3"));
		Assert.IsNotNull(slow.terms("f4"));
		pr.close();

		// no stored fields at all
		pr = new ParallelCompositeReader(false, new CompositeReader[] {ir2}, new CompositeReader[0]);
		assertNull(pr.document(0).get("f1"));
		assertNull(pr.document(0).get("f2"));
		assertNull(pr.document(0).get("f3"));
		assertNull(pr.document(0).get("f4"));
		// check that fields are there
		slow = SlowCompositeReaderWrapper.wrap(pr);
		assertNull(slow.terms("f1"));
		assertNull(slow.terms("f2"));
		Assert.IsNotNull(slow.terms("f3"));
		Assert.IsNotNull(slow.terms("f4"));
		pr.close();

		// without overlapping
		pr = new ParallelCompositeReader(true, new CompositeReader[] {ir2}, new CompositeReader[] {ir1});
		Assert.AreEqual("v1", pr.document(0).get("f1"));
		Assert.AreEqual("v1", pr.document(0).get("f2"));
		assertNull(pr.document(0).get("f3"));
		assertNull(pr.document(0).get("f4"));
		// check that fields are there
		slow = SlowCompositeReaderWrapper.wrap(pr);
		assertNull(slow.terms("f1"));
		assertNull(slow.terms("f2"));
		Assert.IsNotNull(slow.terms("f3"));
		Assert.IsNotNull(slow.terms("f4"));
		pr.close();

		// no main readers
		try
		{
		  new ParallelCompositeReader(true, new CompositeReader[0], new CompositeReader[] {ir1});
		  Assert.Fail("didn't get expected exception: need a non-empty main-reader array");
		}
		catch (System.ArgumentException iae)
		{
		  // pass
		}

		dir1.close();
		dir2.close();
	  }

	  public virtual void TestToString()
	  {
		Directory dir1 = GetDir1(random());
		CompositeReader ir1 = DirectoryReader.open(dir1);
		ParallelCompositeReader pr = new ParallelCompositeReader(new CompositeReader[] {ir1});

		string s = pr.ToString();
		Assert.IsTrue("toString incorrect: " + s, s.StartsWith("ParallelCompositeReader(ParallelAtomicReader("));

		pr.close();
		dir1.close();
	  }

	  public virtual void TestToStringCompositeComposite()
	  {
		Directory dir1 = GetDir1(random());
		CompositeReader ir1 = DirectoryReader.open(dir1);
		ParallelCompositeReader pr = new ParallelCompositeReader(new CompositeReader[] {new MultiReader(ir1)});

		string s = pr.ToString();
		Assert.IsTrue("toString incorrect: " + s, s.StartsWith("ParallelCompositeReader(ParallelCompositeReader(ParallelAtomicReader("));

		pr.close();
		dir1.close();
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
	  private IndexSearcher Single(Random random, bool compositeComposite)
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
		Document d3 = new Document();
		d3.add(newTextField("f1", "v3", Field.Store.YES));
		d3.add(newTextField("f2", "v3", Field.Store.YES));
		d3.add(newTextField("f3", "v3", Field.Store.YES));
		d3.add(newTextField("f4", "v3", Field.Store.YES));
		w.addDocument(d3);
		Document d4 = new Document();
		d4.add(newTextField("f1", "v4", Field.Store.YES));
		d4.add(newTextField("f2", "v4", Field.Store.YES));
		d4.add(newTextField("f3", "v4", Field.Store.YES));
		d4.add(newTextField("f4", "v4", Field.Store.YES));
		w.addDocument(d4);
		w.close();

		CompositeReader ir;
		if (compositeComposite)
		{
		  ir = new MultiReader(DirectoryReader.open(Dir), DirectoryReader.open(Dir));
		}
		else
		{
		  ir = DirectoryReader.open(Dir);
		}
		return newSearcher(ir);
	  }

	  // Fields 1 & 2 in one index, 3 & 4 in other, with ParallelReader:
	  private IndexSearcher Parallel(Random random, bool compositeComposite)
	  {
		Dir1 = GetDir1(random);
		Dir2 = GetDir2(random);
		CompositeReader rd1, rd2;
		if (compositeComposite)
		{
		  rd1 = new MultiReader(DirectoryReader.open(Dir1), DirectoryReader.open(Dir1));
		  rd2 = new MultiReader(DirectoryReader.open(Dir2), DirectoryReader.open(Dir2));
		  Assert.AreEqual(2, rd1.Context.children().size());
		  Assert.AreEqual(2, rd2.Context.children().size());
		}
		else
		{
		  rd1 = DirectoryReader.open(Dir1);
		  rd2 = DirectoryReader.open(Dir2);
		  Assert.AreEqual(3, rd1.Context.children().size());
		  Assert.AreEqual(3, rd2.Context.children().size());
		}
		ParallelCompositeReader pr = new ParallelCompositeReader(rd1, rd2);
		return newSearcher(pr);
	  }

	  // subreader structure: (1,2,1) 
	  private Directory GetDir1(Random random)
	  {
		Directory dir1 = newDirectory();
		IndexWriter w1 = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
		Document d1 = new Document();
		d1.add(newTextField("f1", "v1", Field.Store.YES));
		d1.add(newTextField("f2", "v1", Field.Store.YES));
		w1.addDocument(d1);
		w1.commit();
		Document d2 = new Document();
		d2.add(newTextField("f1", "v2", Field.Store.YES));
		d2.add(newTextField("f2", "v2", Field.Store.YES));
		w1.addDocument(d2);
		Document d3 = new Document();
		d3.add(newTextField("f1", "v3", Field.Store.YES));
		d3.add(newTextField("f2", "v3", Field.Store.YES));
		w1.addDocument(d3);
		w1.commit();
		Document d4 = new Document();
		d4.add(newTextField("f1", "v4", Field.Store.YES));
		d4.add(newTextField("f2", "v4", Field.Store.YES));
		w1.addDocument(d4);
		w1.close();
		return dir1;
	  }

	  // subreader structure: (1,2,1) 
	  private Directory GetDir2(Random random)
	  {
		Directory dir2 = newDirectory();
		IndexWriter w2 = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
		Document d1 = new Document();
		d1.add(newTextField("f3", "v1", Field.Store.YES));
		d1.add(newTextField("f4", "v1", Field.Store.YES));
		w2.addDocument(d1);
		w2.commit();
		Document d2 = new Document();
		d2.add(newTextField("f3", "v2", Field.Store.YES));
		d2.add(newTextField("f4", "v2", Field.Store.YES));
		w2.addDocument(d2);
		Document d3 = new Document();
		d3.add(newTextField("f3", "v3", Field.Store.YES));
		d3.add(newTextField("f4", "v3", Field.Store.YES));
		w2.addDocument(d3);
		w2.commit();
		Document d4 = new Document();
		d4.add(newTextField("f3", "v4", Field.Store.YES));
		d4.add(newTextField("f4", "v4", Field.Store.YES));
		w2.addDocument(d4);
		w2.close();
		return dir2;
	  }

	  // this dir has a different subreader structure (1,1,2);
	  private Directory GetInvalidStructuredDir2(Random random)
	  {
		Directory dir2 = newDirectory();
		IndexWriter w2 = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
		Document d1 = new Document();
		d1.add(newTextField("f3", "v1", Field.Store.YES));
		d1.add(newTextField("f4", "v1", Field.Store.YES));
		w2.addDocument(d1);
		w2.commit();
		Document d2 = new Document();
		d2.add(newTextField("f3", "v2", Field.Store.YES));
		d2.add(newTextField("f4", "v2", Field.Store.YES));
		w2.addDocument(d2);
		w2.commit();
		Document d3 = new Document();
		d3.add(newTextField("f3", "v3", Field.Store.YES));
		d3.add(newTextField("f4", "v3", Field.Store.YES));
		w2.addDocument(d3);
		Document d4 = new Document();
		d4.add(newTextField("f3", "v4", Field.Store.YES));
		d4.add(newTextField("f4", "v4", Field.Store.YES));
		w2.addDocument(d4);
		w2.close();
		return dir2;
	  }

	}

}