using System;
using System.Collections.Generic;

namespace Lucene.Net.Store
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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestNRTCachingDirectory : LuceneTestCase
	{

	  public virtual void TestNRTAndCommit()
	  {
		Directory dir = newDirectory();
		NRTCachingDirectory cachedDir = new NRTCachingDirectory(dir, 2.0, 25.0);
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		RandomIndexWriter w = new RandomIndexWriter(random(), cachedDir, conf);
		LineFileDocs docs = new LineFileDocs(random(), defaultCodecSupportsDocValues());
		int numDocs = TestUtil.Next(random(), 100, 400);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: numDocs=" + numDocs);
		}

		IList<BytesRef> ids = new List<BytesRef>();
		DirectoryReader r = null;
		for (int docCount = 0;docCount < numDocs;docCount++)
		{
		  Document doc = docs.nextDoc();
		  ids.Add(new BytesRef(doc.get("docid")));
		  w.addDocument(doc);
		  if (random().Next(20) == 17)
		  {
			if (r == null)
			{
			  r = DirectoryReader.open(w.w, false);
			}
			else
			{
			  DirectoryReader r2 = DirectoryReader.openIfChanged(r);
			  if (r2 != null)
			  {
				r.close();
				r = r2;
			  }
			}
			Assert.AreEqual(1 + docCount, r.numDocs());
			IndexSearcher s = newSearcher(r);
			// Just make sure search can run; we can't assert
			// totHits since it could be 0
			TopDocs hits = s.search(new TermQuery(new Term("body", "the")), 10);
			// System.out.println("tot hits " + hits.totalHits);
		  }
		}

		if (r != null)
		{
		  r.close();
		}

		// Close should force cache to clear since all files are sync'd
		w.close();

		string[] cachedFiles = cachedDir.listCachedFiles();
		foreach (string file in cachedFiles)
		{
		  Console.WriteLine("FAIL: cached file " + file + " remains after sync");
		}
		Assert.AreEqual(0, cachedFiles.Length);

		r = DirectoryReader.open(dir);
		foreach (BytesRef id in ids)
		{
		  Assert.AreEqual(1, r.docFreq(new Term("docid", id)));
		}
		r.close();
		cachedDir.close();
		docs.close();
	  }

	  // NOTE: not a test; just here to make sure the code frag
	  // in the javadocs is correct!
	  public virtual void VerifyCompiles()
	  {
		Analyzer analyzer = null;

		Directory fsDir = FSDirectory.open(new File("/path/to/index"));
		NRTCachingDirectory cachedFSDir = new NRTCachingDirectory(fsDir, 2.0, 25.0);
		IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		IndexWriter writer = new IndexWriter(cachedFSDir, conf);
	  }

	  public virtual void TestDeleteFile()
	  {
		Directory dir = new NRTCachingDirectory(newDirectory(), 2.0, 25.0);
		dir.createOutput("foo.txt", IOContext.DEFAULT).close();
		dir.deleteFile("foo.txt");
		Assert.AreEqual(0, dir.listAll().length);
		dir.close();
	  }

	  // LUCENE-3382 -- make sure we get exception if the directory really does not exist.
	  public virtual void TestNoDir()
	  {
		File tempDir = createTempDir("doesnotexist");
		TestUtil.rm(tempDir);
		Directory dir = new NRTCachingDirectory(newFSDirectory(tempDir), 2.0, 25.0);
		try
		{
		  DirectoryReader.open(dir);
		  Assert.Fail("did not hit expected exception");
		}
		catch (NoSuchDirectoryException nsde)
		{
		  // expected
		}
		dir.close();
	  }

	  // LUCENE-3382 test that we can add a file, and then when we call list() we get it back
	  public virtual void TestDirectoryFilter()
	  {
		Directory dir = new NRTCachingDirectory(newFSDirectory(createTempDir("foo")), 2.0, 25.0);
		string name = "file";
		try
		{
		  dir.createOutput(name, newIOContext(random())).close();
		  Assert.IsTrue(slowFileExists(dir, name));
		  Assert.IsTrue(Arrays.asList(dir.listAll()).contains(name));
		}
		finally
		{
		  dir.close();
		}
	  }

	  // LUCENE-3382 test that delegate compound files correctly.
	  public virtual void TestCompoundFileAppendTwice()
	  {
		Directory newDir = new NRTCachingDirectory(newDirectory(), 2.0, 25.0);
		CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), true);
		CreateSequenceFile(newDir, "d1", (sbyte) 0, 15);
		IndexOutput @out = csw.createOutput("d.xyz", newIOContext(random()));
		@out.writeInt(0);
		@out.close();
		Assert.AreEqual(1, csw.listAll().length);
		Assert.AreEqual("d.xyz", csw.listAll()[0]);

		csw.close();

		CompoundFileDirectory cfr = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), false);
		Assert.AreEqual(1, cfr.listAll().length);
		Assert.AreEqual("d.xyz", cfr.listAll()[0]);
		cfr.close();
		newDir.close();
	  }

	  /// <summary>
	  /// Creates a file of the specified size with sequential data. The first
	  ///  byte is written as the start byte provided. All subsequent bytes are
	  ///  computed as start + offset where offset is the number of the byte.
	  /// </summary>
	  private void CreateSequenceFile(Directory dir, string name, sbyte start, int size)
	  {
		  IndexOutput os = dir.createOutput(name, newIOContext(random()));
		  for (int i = 0; i < size; i++)
		  {
			  os.writeByte(start);
			  start++;
		  }
		  os.close();
	  }
	}

}