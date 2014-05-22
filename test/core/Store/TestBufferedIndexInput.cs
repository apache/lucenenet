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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using Term = Lucene.Net.Index.Term;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using ArrayUtil = Lucene.Net.Util.ArrayUtil;

	public class TestBufferedIndexInput : LuceneTestCase
	{

	  private static void WriteBytes(File aFile, long size)
	  {
		OutputStream stream = null;
		try
		{
		  stream = new FileOutputStream(aFile);
		  for (int i = 0; i < size; i++)
		  {
			stream.write(Byten(i));
		  }
		  stream.flush();
		}
		finally
		{
		  if (stream != null)
		  {
			stream.close();
		  }
		}
	  }

	  private const long TEST_FILE_LENGTH = 100 * 1024;

	  // Call readByte() repeatedly, past the buffer boundary, and see that it
	  // is working as expected.
	  // Our input comes from a dynamically generated/ "file" - see
	  // MyBufferedIndexInput below.
	  public virtual void TestReadByte()
	  {
		MyBufferedIndexInput input = new MyBufferedIndexInput();
		for (int i = 0; i < BufferedIndexInput.BUFFER_SIZE * 10; i++)
		{
		  Assert.AreEqual(input.readByte(), Byten(i));
		}
	  }

	  // Call readBytes() repeatedly, with various chunk sizes (from 1 byte to
	  // larger than the buffer size), and see that it returns the bytes we expect.
	  // Our input comes from a dynamically generated "file" -
	  // see MyBufferedIndexInput below.
	  public virtual void TestReadBytes()
	  {
		MyBufferedIndexInput input = new MyBufferedIndexInput();
		RunReadBytes(input, BufferedIndexInput.BUFFER_SIZE, random());
	  }

	  private void RunReadBytesAndClose(IndexInput input, int bufferSize, Random r)
	  {
		try
		{
		  RunReadBytes(input, bufferSize, r);
		}
		finally
		{
		  input.close();
		}
	  }

	  private void RunReadBytes(IndexInput input, int bufferSize, Random r)
	  {

		int pos = 0;
		// gradually increasing size:
		for (int size = 1; size < bufferSize * 10; size = size + size / 200 + 1)
		{
		  CheckReadBytes(input, size, pos);
		  pos += size;
		  if (pos >= TEST_FILE_LENGTH)
		  {
			// wrap
			pos = 0;
			input.seek(0L);
		  }
		}
		// wildly fluctuating size:
		for (long i = 0; i < 100; i++)
		{
		  int size = r.Next(10000);
		  CheckReadBytes(input, 1 + size, pos);
		  pos += 1 + size;
		  if (pos >= TEST_FILE_LENGTH)
		  {
			// wrap
			pos = 0;
			input.seek(0L);
		  }
		}
		// constant small size (7 bytes):
		for (int i = 0; i < bufferSize; i++)
		{
		  CheckReadBytes(input, 7, pos);
		  pos += 7;
		  if (pos >= TEST_FILE_LENGTH)
		  {
			// wrap
			pos = 0;
			input.seek(0L);
		  }
		}
	  }

	  private sbyte[] Buffer = new sbyte[10];

	  private void CheckReadBytes(IndexInput input, int size, int pos)
	  {
		// Just to see that "offset" is treated properly in readBytes(), we
		// add an arbitrary offset at the beginning of the array
		int offset = size % 10; // arbitrary
		Buffer = ArrayUtil.grow(Buffer, offset + size);
		Assert.AreEqual(pos, input.FilePointer);
		long left = TEST_FILE_LENGTH - input.FilePointer;
		if (left <= 0)
		{
		  return;
		}
		else if (left < size)
		{
		  size = (int) left;
		}
		input.readBytes(Buffer, offset, size);
		Assert.AreEqual(pos + size, input.FilePointer);
		for (int i = 0; i < size; i++)
		{
		  Assert.AreEqual("pos=" + i + " filepos=" + (pos + i), Byten(pos + i), Buffer[offset + i]);
		}
	  }

	  // this tests that attempts to readBytes() past an EOF will fail, while
	  // reads up to the EOF will succeed. The EOF is determined by the
	  // BufferedIndexInput's arbitrary length() value.
	  public virtual void TestEOF()
	  {
		 MyBufferedIndexInput input = new MyBufferedIndexInput(1024);
		 // see that we can read all the bytes at one go:
		 CheckReadBytes(input, (int)input.Length(), 0);
		 // go back and see that we can't read more than that, for small and
		 // large overflows:
		 int pos = (int)input.Length() - 10;
		 input.seek(pos);
		 CheckReadBytes(input, 10, pos);
		 input.seek(pos);
		 try
		 {
		   CheckReadBytes(input, 11, pos);
			   Assert.Fail("Block read past end of file");
		 }
		   catch (IOException e)
		   {
			   /* success */
		   }
		 input.seek(pos);
		 try
		 {
		   CheckReadBytes(input, 50, pos);
			   Assert.Fail("Block read past end of file");
		 }
		   catch (IOException e)
		   {
			   /* success */
		   }
		 input.seek(pos);
		 try
		 {
		   CheckReadBytes(input, 100000, pos);
			   Assert.Fail("Block read past end of file");
		 }
		   catch (IOException e)
		   {
			   /* success */
		   }
	  }

		// byten emulates a file - byten(n) returns the n'th byte in that file.
		// MyBufferedIndexInput reads this "file".
		private static sbyte Byten(long n)
		{
		  return (sbyte)(n * n % 256);
		}

		private class MyBufferedIndexInput : BufferedIndexInput
		{
		  internal long Pos;
		  internal long Len;
		  public MyBufferedIndexInput(long len) : base("MyBufferedIndexInput(len=" + len + ")", BufferedIndexInput.BUFFER_SIZE)
		  {
			this.Len = len;
			this.Pos = 0;
		  }
		  public MyBufferedIndexInput() : this(long.MaxValue)
		  {
			// an infinite file
		  }
		  protected internal override void ReadInternal(sbyte[] b, int offset, int length)
		  {
			for (int i = offset; i < offset + length; i++)
			{
			  b[i] = Byten(Pos++);
			}
		  }

		  protected internal override void SeekInternal(long pos)
		  {
			this.Pos = pos;
		  }

		  public override void Close()
		  {
		  }

		  public override long Length()
		  {
			return Len;
		  }
		}

		public virtual void TestSetBufferSize()
		{
		  File indexDir = createTempDir("testSetBufferSize");
		  MockFSDirectory dir = new MockFSDirectory(indexDir, random());
		  try
		  {
			IndexWriter writer = new IndexWriter(dir, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setOpenMode(IndexWriterConfig.OpenMode.CREATE).setMergePolicy(newLogMergePolicy(false)));
			for (int i = 0;i < 37;i++)
			{
			  Document doc = new Document();
			  doc.add(newTextField("content", "aaa bbb ccc ddd" + i, Field.Store.YES));
			  doc.add(newTextField("id", "" + i, Field.Store.YES));
			  writer.addDocument(doc);
			}

			dir.AllIndexInputs.Clear();

			IndexReader reader = DirectoryReader.open(writer, true);
			Term aaa = new Term("content", "aaa");
			Term bbb = new Term("content", "bbb");

			reader.close();

			dir.TweakBufferSizes();
			writer.deleteDocuments(new Term("id", "0"));
			reader = DirectoryReader.open(writer, true);
			IndexSearcher searcher = newSearcher(reader);
			ScoreDoc[] hits = searcher.search(new TermQuery(bbb), null, 1000).scoreDocs;
			dir.TweakBufferSizes();
			Assert.AreEqual(36, hits.Length);

			reader.close();

			dir.TweakBufferSizes();
			writer.deleteDocuments(new Term("id", "4"));
			reader = DirectoryReader.open(writer, true);
			searcher = newSearcher(reader);

			hits = searcher.search(new TermQuery(bbb), null, 1000).scoreDocs;
			dir.TweakBufferSizes();
			Assert.AreEqual(35, hits.Length);
			dir.TweakBufferSizes();
			hits = searcher.search(new TermQuery(new Term("id", "33")), null, 1000).scoreDocs;
			dir.TweakBufferSizes();
			Assert.AreEqual(1, hits.Length);
			hits = searcher.search(new TermQuery(aaa), null, 1000).scoreDocs;
			dir.TweakBufferSizes();
			Assert.AreEqual(35, hits.Length);
			writer.close();
			reader.close();
		  }
		  finally
		  {
			TestUtil.rm(indexDir);
		  }
		}

		private class MockFSDirectory : BaseDirectory
		{

		  internal IList<IndexInput> AllIndexInputs = new List<IndexInput>();

		  internal Random Rand;

		  internal Directory Dir;

		  public MockFSDirectory(File path, Random rand)
		  {
			this.Rand = rand;
			lockFactory = NoLockFactory.NoLockFactory;
			Dir = new SimpleFSDirectory(path, null);
		  }

		  public virtual void TweakBufferSizes()
		  {
			//int count = 0;
			foreach (IndexInput ip in AllIndexInputs)
			{
			  BufferedIndexInput bii = (BufferedIndexInput) ip;
			  int bufferSize = 1024 + Math.Abs(Rand.Next() % 32768);
			  bii.BufferSize = bufferSize;
			  //count++;
			}
			//System.out.println("tweak'd " + count + " buffer sizes");
		  }

		  public override IndexInput OpenInput(string name, IOContext context)
		  {
			// Make random changes to buffer size
			//bufferSize = 1+Math.abs(rand.nextInt() % 10);
			IndexInput f = Dir.openInput(name, context);
			AllIndexInputs.Add(f);
			return f;
		  }

		  public override IndexOutput CreateOutput(string name, IOContext context)
		  {
			return Dir.createOutput(name, context);
		  }

		  public override void Close()
		  {
			Dir.close();
		  }

		  public override void DeleteFile(string name)
		  {
			Dir.deleteFile(name);
		  }
		  public override bool FileExists(string name)
		  {
			return Dir.fileExists(name);
		  }
		  public override string[] ListAll()
		  {
			return Dir.listAll();
		  }
		  public override void Sync(ICollection<string> names)
		  {
			Dir.sync(names);
		  }
		  public override long FileLength(string name)
		  {
			return Dir.fileLength(name);
		  }
		}
	}

}