using System;
using System.Threading;

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


	using Field = Lucene.Net.Document.Field;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using English = Lucene.Net.Util.English;

	/// <summary>
	/// JUnit testcase to test RAMDirectory. RAMDirectory itself is used in many testcases,
	/// but not one of them uses an different constructor other than the default constructor.
	/// </summary>
	public class TestRAMDirectory : LuceneTestCase
	{

	  private File IndexDir = null;

	  // add enough document so that the index will be larger than RAMDirectory.READ_BUFFER_SIZE
	  private readonly int DocsToAdd = 500;

	  // setup the index
	  public override void SetUp()
	  {
		base.setUp();
		IndexDir = createTempDir("RAMDirIndex");

		Directory dir = newFSDirectory(IndexDir);
		IndexWriter writer = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setOpenMode(IndexWriterConfig.OpenMode_e.CREATE));
		// add some documents
		Document doc = null;
		for (int i = 0; i < DocsToAdd; i++)
		{
		  doc = new Document();
		  doc.add(newStringField("content", English.intToEnglish(i).Trim(), Field.Store.YES));
		  writer.addDocument(doc);
		}
		Assert.AreEqual(DocsToAdd, writer.maxDoc());
		writer.close();
		dir.close();
	  }

	  public virtual void TestRAMDirectory()
	  {

		Directory dir = newFSDirectory(IndexDir);
		MockDirectoryWrapper ramDir = new MockDirectoryWrapper(random(), new RAMDirectory(dir, newIOContext(random())));

		// close the underlaying directory
		dir.close();

		// Check size
		Assert.AreEqual(ramDir.sizeInBytes(), ramDir.RecomputedSizeInBytes);

		// open reader to test document count
		IndexReader reader = DirectoryReader.open(ramDir);
		Assert.AreEqual(DocsToAdd, reader.numDocs());

		// open search zo check if all doc's are there
		IndexSearcher searcher = newSearcher(reader);

		// search for all documents
		for (int i = 0; i < DocsToAdd; i++)
		{
		  Document doc = searcher.doc(i);
		  Assert.IsTrue(doc.getField("content") != null);
		}

		// cleanup
		reader.close();
	  }

	  private readonly int NumThreads = 10;
	  private readonly int DocsPerThread = 40;

	  public virtual void TestRAMDirectorySize()
	  {

		Directory dir = newFSDirectory(IndexDir);
		MockDirectoryWrapper ramDir = new MockDirectoryWrapper(random(), new RAMDirectory(dir, newIOContext(random())));
		dir.close();

		IndexWriter writer = new IndexWriter(ramDir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setOpenMode(IndexWriterConfig.OpenMode_e.APPEND));
		writer.forceMerge(1);

		Assert.AreEqual(ramDir.sizeInBytes(), ramDir.RecomputedSizeInBytes);

		Thread[] threads = new Thread[NumThreads];
		for (int i = 0; i < NumThreads; i++)
		{
		  int num = i;
		  threads[i] = new ThreadAnonymousInnerClassHelper(this, writer, num);
		}
		for (int i = 0; i < NumThreads; i++)
		{
		  threads[i].Start();
		}
		for (int i = 0; i < NumThreads; i++)
		{
		  threads[i].Join();
		}

		writer.forceMerge(1);
		Assert.AreEqual(ramDir.sizeInBytes(), ramDir.RecomputedSizeInBytes);

		writer.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestRAMDirectory OuterInstance;

		  private IndexWriter Writer;
		  private int Num;

		  public ThreadAnonymousInnerClassHelper(TestRAMDirectory outerInstance, IndexWriter writer, int num)
		  {
			  this.OuterInstance = outerInstance;
			  this.Writer = writer;
			  this.Num = num;
		  }

		  public override void Run()
		  {
			for (int j = 1; j < OuterInstance.DocsPerThread; j++)
			{
			  Document doc = new Document();
			  doc.add(newStringField("sizeContent", English.intToEnglish(Num * OuterInstance.DocsPerThread + j).Trim(), Field.Store.YES));
			  try
			  {
				Writer.addDocument(doc);
			  }
			  catch (IOException e)
			  {
				throw new Exception(e);
			  }
			}
		  }
	  }

	  public override void TearDown()
	  {
		// cleanup 
		if (IndexDir != null && IndexDir.exists())
		{
		  RmDir(IndexDir);
		}
		base.tearDown();
	  }

	  // LUCENE-1196
	  public virtual void TestIllegalEOF()
	  {
		RAMDirectory dir = new RAMDirectory();
		IndexOutput o = dir.createOutput("out", newIOContext(random()));
		sbyte[] b = new sbyte[1024];
		o.writeBytes(b, 0, 1024);
		o.close();
		IndexInput i = dir.openInput("out", newIOContext(random()));
		i.seek(1024);
		i.close();
		dir.close();
	  }

	  private void RmDir(File dir)
	  {
		File[] files = dir.listFiles();
		for (int i = 0; i < files.Length; i++)
		{
		  files[i].delete();
		}
		dir.delete();
	  }

	  // LUCENE-2852
	  public virtual void TestSeekToEOFThenBack()
	  {
		RAMDirectory dir = new RAMDirectory();

		IndexOutput o = dir.createOutput("out", newIOContext(random()));
		sbyte[] bytes = new sbyte[3 * RAMInputStream.BUFFER_SIZE];
		o.writeBytes(bytes, 0, bytes.Length);
		o.close();

		IndexInput i = dir.openInput("out", newIOContext(random()));
		i.seek(2 * RAMInputStream.BUFFER_SIZE-1);
		i.seek(3 * RAMInputStream.BUFFER_SIZE);
		i.seek(RAMInputStream.BUFFER_SIZE);
		i.readBytes(bytes, 0, 2 * RAMInputStream.BUFFER_SIZE);
		i.close();
		dir.close();
	  }
	}

}