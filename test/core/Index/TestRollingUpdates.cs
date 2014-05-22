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
	using Codec = Lucene.Net.Codecs.Codec;
	using MemoryPostingsFormat = Lucene.Net.Codecs.memory.MemoryPostingsFormat;
	using Lucene.Net.Document;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using Lucene.Net.Store;
	using Lucene.Net.Util;
	using Test = org.junit.Test;

	public class TestRollingUpdates : LuceneTestCase
	{

	  // Just updates the same set of N docs over and over, to
	  // stress out deletions

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRollingUpdates() throws Exception
	  public virtual void TestRollingUpdates()
	  {
		Random random = new Random(random().nextLong());
		BaseDirectoryWrapper dir = newDirectory();
		LineFileDocs docs = new LineFileDocs(random, defaultCodecSupportsDocValues());

		//provider.register(new MemoryCodec());
		if ((!"Lucene3x".Equals(Codec.Default.Name)) && random().nextBoolean())
		{
		  Codec.Default = TestUtil.alwaysPostingsFormat(new MemoryPostingsFormat(random().nextBoolean(), random.nextFloat()));
		}

		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);

		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		int SIZE = atLeast(20);
		int id = 0;
		IndexReader r = null;
		IndexSearcher s = null;
		int numUpdates = (int)(SIZE * (2 + (TEST_NIGHTLY ? 200 * random().NextDouble() : 5 * random().NextDouble())));
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: numUpdates=" + numUpdates);
		}
		int updateCount = 0;
		// TODO: sometimes update ids not in order...
		for (int docIter = 0;docIter < numUpdates;docIter++)
		{
		  Document doc = docs.nextDoc();
		  string myID = "" + id;
		  if (id == SIZE-1)
		  {
			id = 0;
		  }
		  else
		  {
			id++;
		  }
		  if (VERBOSE)
		  {
			Console.WriteLine("  docIter=" + docIter + " id=" + id);
		  }
		  ((Field) doc.getField("docid")).StringValue = myID;

		  Term idTerm = new Term("docid", myID);

		  bool doUpdate;
		  if (s != null && updateCount < SIZE)
		  {
			TopDocs hits = s.search(new TermQuery(idTerm), 1);
			Assert.AreEqual(1, hits.totalHits);
			doUpdate = !w.tryDeleteDocument(r, hits.scoreDocs[0].doc);
			if (VERBOSE)
			{
			  if (doUpdate)
			  {
				Console.WriteLine("  tryDeleteDocument failed");
			  }
			  else
			  {
				Console.WriteLine("  tryDeleteDocument succeeded");
			  }
			}
		  }
		  else
		  {
			doUpdate = true;
			if (VERBOSE)
			{
			  Console.WriteLine("  no searcher: doUpdate=true");
			}
		  }

		  updateCount++;

		  if (doUpdate)
		  {
			w.updateDocument(idTerm, doc);
		  }
		  else
		  {
			w.addDocument(doc);
		  }

		  if (docIter >= SIZE && random().Next(50) == 17)
		  {
			if (r != null)
			{
			  r.close();
			}

			bool applyDeletions = random().nextBoolean();

			if (VERBOSE)
			{
			  Console.WriteLine("TEST: reopen applyDeletions=" + applyDeletions);
			}

			r = w.getReader(applyDeletions);
			if (applyDeletions)
			{
			  s = newSearcher(r);
			}
			else
			{
			  s = null;
			}
			Assert.IsTrue("applyDeletions=" + applyDeletions + " r.numDocs()=" + r.numDocs() + " vs SIZE=" + SIZE, !applyDeletions || r.numDocs() == SIZE);
			updateCount = 0;
		  }
		}

		if (r != null)
		{
		  r.close();
		}

		w.commit();
		Assert.AreEqual(SIZE, w.numDocs());

		w.close();

		TestIndexWriter.AssertNoUnreferencedFiles(dir, "leftover files after rolling updates");

		docs.close();

		// LUCENE-4455:
		SegmentInfos infos = new SegmentInfos();
		infos.read(dir);
		long totalBytes = 0;
		foreach (SegmentCommitInfo sipc in infos)
		{
		  totalBytes += sipc.sizeInBytes();
		}
		long totalBytes2 = 0;
		foreach (string fileName in dir.listAll())
		{
		  if (!fileName.StartsWith(IndexFileNames.SEGMENTS))
		  {
			totalBytes2 += dir.fileLength(fileName);
		  }
		}
		Assert.AreEqual(totalBytes2, totalBytes);
		dir.close();
	  }


	  public virtual void TestUpdateSameDoc()
	  {
		Directory dir = newDirectory();

		LineFileDocs docs = new LineFileDocs(random());
		for (int r = 0; r < 3; r++)
		{
		  IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
		  int numUpdates = atLeast(20);
		  int numThreads = TestUtil.Next(random(), 2, 6);
		  IndexingThread[] threads = new IndexingThread[numThreads];
		  for (int i = 0; i < numThreads; i++)
		  {
			threads[i] = new IndexingThread(docs, w, numUpdates);
			threads[i].Start();
		  }

		  for (int i = 0; i < numThreads; i++)
		  {
			threads[i].Join();
		  }

		  w.close();
		}

		IndexReader open = DirectoryReader.open(dir);
		Assert.AreEqual(1, open.numDocs());
		open.close();
		docs.close();
		dir.close();
	  }

	  internal class IndexingThread : System.Threading.Thread
	  {
		internal readonly LineFileDocs Docs;
		internal readonly IndexWriter Writer;
		internal readonly int Num;

		public IndexingThread(LineFileDocs docs, IndexWriter writer, int num) : base()
		{
		  this.Docs = docs;
		  this.Writer = writer;
		  this.Num = num;
		}

		public override void Run()
		{
		  try
		  {
			DirectoryReader open = null;
			for (int i = 0; i < Num; i++)
			{
			  Document doc = new Document(); // docs.nextDoc();
			  doc.add(newStringField("id", "test", Field.Store.NO));
			  Writer.updateDocument(new Term("id", "test"), doc);
			  if (random().Next(3) == 0)
			  {
				if (open == null)
				{
				  open = DirectoryReader.open(Writer, true);
				}
				DirectoryReader reader = DirectoryReader.openIfChanged(open);
				if (reader != null)
				{
				  open.close();
				  open = reader;
				}
				Assert.AreEqual("iter: " + i + " numDocs: " + open.numDocs() + " del: " + open.numDeletedDocs() + " max: " + open.maxDoc(), 1, open.numDocs());
			  }
			}
			if (open != null)
			{
			  open.close();
			}
		  }
		  catch (Exception e)
		  {
			throw new Exception(e);
		  }
		}
	  }
	}

}