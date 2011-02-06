/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using TermQuery = Lucene.Net.Search.TermQuery;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using _TestUtil = Lucene.Net.Util._TestUtil;

namespace Lucene.Net.Store
{
	[TestFixture]
	public class TestBufferedIndexInput : LuceneTestCase
	{
		// Call readByte() repeatedly, past the buffer boundary, and see that it
		// is working as expected.
		// Our input comes from a dynamically generated/ "file" - see
		// MyBufferedIndexInput below.
		[Test]
		public virtual void  TestReadByte()
		{
			MyBufferedIndexInput input = new MyBufferedIndexInput();
			for (int i = 0; i < BufferedIndexInput.BUFFER_SIZE * 10; i++)
			{
				Assert.AreEqual(input.ReadByte(), Byten(i));
			}
		}
		
		// Call readBytes() repeatedly, with various chunk sizes (from 1 byte to
		// larger than the buffer size), and see that it returns the bytes we expect.
		// Our input comes from a dynamically generated "file" -
		// see MyBufferedIndexInput below.
		[Test]
		public virtual void  TestReadBytes()
		{
			MyBufferedIndexInput input = new MyBufferedIndexInput();
			int pos = 0;
			// gradually increasing size:
			for (int size = 1; size < BufferedIndexInput.BUFFER_SIZE * 10; size = size + size / 200 + 1)
			{
				CheckReadBytes(input, size, pos);
				pos += size;
			}
			// wildly fluctuating size:
			for (long i = 0; i < 1000; i++)
			{
				// The following function generates a fluctuating (but repeatable)
				// size, sometimes small (<100) but sometimes large (>10000)
				int size1 = (int) (i % 7 + 7 * (i % 5) + 7 * 5 * (i % 3) + 5 * 5 * 3 * (i % 2));
				int size2 = (int) (i % 11 + 11 * (i % 7) + 11 * 7 * (i % 5) + 11 * 7 * 5 * (i % 3) + 11 * 7 * 5 * 3 * (i % 2));
				int size = (i % 3 == 0)?size2 * 10:size1;
				CheckReadBytes(input, size, pos);
				pos += size;
			}
			// constant small size (7 bytes):
			for (int i = 0; i < BufferedIndexInput.BUFFER_SIZE; i++)
			{
				CheckReadBytes(input, 7, pos);
				pos += 7;
			}
		}
		private void  CheckReadBytes(BufferedIndexInput input, int size, int pos)
		{
			// Just to see that "offset" is treated properly in readBytes(), we
			// add an arbitrary offset at the beginning of the array
			int offset = size % 10; // arbitrary
			byte[] b = new byte[offset + size];
			input.ReadBytes(b, offset, size);
			for (int i = 0; i < size; i++)
			{
				Assert.AreEqual(b[offset + i], Byten(pos + i));
			}
		}
		
		// This tests that attempts to readBytes() past an EOF will fail, while
		// reads up to the EOF will succeed. The EOF is determined by the
		// BufferedIndexInput's arbitrary length() value.
		[Test]
		public virtual void  TestEOF()
		{
			MyBufferedIndexInput input = new MyBufferedIndexInput(1024);
			// see that we can read all the bytes at one go:
			CheckReadBytes(input, (int) input.Length(), 0);
			// go back and see that we can't read more than that, for small and
			// large overflows:
			int pos = (int) input.Length() - 10;
			input.Seek(pos);
			CheckReadBytes(input, 10, pos);
			input.Seek(pos);
			try
			{
				CheckReadBytes(input, 11, pos);
				Assert.Fail("Block read past end of file");
			}
			catch (System.IO.IOException)
			{
				/* success */
			}
			input.Seek(pos);
			try
			{
				CheckReadBytes(input, 50, pos);
				Assert.Fail("Block read past end of file");
			}
			catch (System.IO.IOException)
			{
				/* success */
			}
			input.Seek(pos);
			try
			{
				CheckReadBytes(input, 100000, pos);
				Assert.Fail("Block read past end of file");
			}
			catch (System.IO.IOException)
			{
				/* success */
			}
		}
		
		// byten emulates a file - Byten(n) returns the n'th byte in that file.
		// MyBufferedIndexInput reads this "file".
		private static byte Byten(long n)
		{
			return (byte) (n * n % 256);
		}

		private class MyBufferedIndexInput : BufferedIndexInput
		{
			private long pos;
			private long len;
			public MyBufferedIndexInput(long len)
			{
				this.len = len;
				this.pos = 0;
			}
			public MyBufferedIndexInput() : this(System.Int64.MaxValue)
			{
			}

			protected override void  ReadInternal(byte[] b, int offset, int length)
			{
				for (int i = offset; i < offset + length; i++)
					b[i] = Lucene.Net.Store.TestBufferedIndexInput.Byten(pos++);
			}
			
			protected override void  SeekInternal(long pos)
			{
				this.pos = pos;
			}
			
			public override void  Close()
			{
			}
			
			public override long Length()
			{
				return len;
			}
		}
		
		[Test]
		public virtual void  TestSetBufferSize()
		{
			System.IO.FileInfo indexDir = new System.IO.FileInfo(System.IO.Path.Combine(SupportClass.AppSettings.Get("tempDir", ""), "testSetBufferSize"));
			MockFSDirectory dir = new MockFSDirectory(indexDir);
			try
			{
				IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
				writer.SetUseCompoundFile(false);
				for (int i = 0; i < 37; i++)
				{
					Document doc = new Document();
					doc.Add(new Field("content", "aaa bbb ccc ddd" + i, Field.Store.YES, Field.Index.TOKENIZED));
					doc.Add(new Field("id", "" + i, Field.Store.YES, Field.Index.TOKENIZED));
					writer.AddDocument(doc);
				}
				writer.Close();
				
				dir.allIndexInputs.Clear();
				
				IndexReader reader = IndexReader.Open(dir);
				Term aaa = new Term("content", "aaa");
				Term bbb = new Term("content", "bbb");
				Term ccc = new Term("content", "ccc");
				Assert.AreEqual(reader.DocFreq(ccc), 37);
				reader.DeleteDocument(0);
				Assert.AreEqual(reader.DocFreq(aaa), 37);
				dir.TweakBufferSizes();
				reader.DeleteDocument(4);
				Assert.AreEqual(reader.DocFreq(bbb), 37);
				dir.TweakBufferSizes();
				
				IndexSearcher searcher = new IndexSearcher(reader);
				Hits hits = searcher.Search(new TermQuery(bbb));
				dir.TweakBufferSizes();
				Assert.AreEqual(35, hits.Length());
				dir.TweakBufferSizes();
				hits = searcher.Search(new TermQuery(new Term("id", "33")));
				dir.TweakBufferSizes();
				Assert.AreEqual(1, hits.Length());
				hits = searcher.Search(new TermQuery(aaa));
				dir.TweakBufferSizes();
				Assert.AreEqual(35, hits.Length());
				searcher.Close();
				reader.Close();
			}
			finally
			{
				_TestUtil.RmDir(indexDir);
			}
		}
		
		private class MockFSDirectory : Directory
		{
			
			internal System.Collections.IList allIndexInputs = new System.Collections.ArrayList();
			
			internal System.Random rand = new System.Random();
			
			private Directory dir;
			
			public MockFSDirectory(System.IO.FileInfo path)
			{
				lockFactory = new NoLockFactory();
				dir = FSDirectory.GetDirectory(path);
			}
			
			public override IndexInput OpenInput(System.String name)
			{
				return OpenInput(name, BufferedIndexInput.BUFFER_SIZE);
			}
			
			public virtual void  TweakBufferSizes()
			{
				System.Collections.IEnumerator it = allIndexInputs.GetEnumerator();
				int count = 0;
				while (it.MoveNext())
				{
					BufferedIndexInput bii = (BufferedIndexInput) it.Current;
					int bufferSize = 1024 + (int) System.Math.Abs(rand.Next() % 32768);
					bii.SetBufferSize(bufferSize);
					count++;
				}
				//System.out.println("tweak'd " + count + " buffer sizes");
			}
			
			public override IndexInput OpenInput(System.String name, int bufferSize)
			{
				// Make random changes to buffer size
				bufferSize = 1 + (int) System.Math.Abs(rand.Next() % 10);
				IndexInput f = dir.OpenInput(name, bufferSize);
				allIndexInputs.Add(f);
				return f;
			}
			
			public override IndexOutput CreateOutput(System.String name)
			{
				return dir.CreateOutput(name);
			}
			
			public override void  Close()
			{
				dir.Close();
			}
			
			public override void  DeleteFile(System.String name)
			{
				dir.DeleteFile(name);
			}
			public override void  TouchFile(System.String name)
			{
				dir.TouchFile(name);
			}
			public override long FileModified(System.String name)
			{
				return dir.FileModified(name);
			}
			public override bool FileExists(System.String name)
			{
				return dir.FileExists(name);
			}
			public override System.String[] List()
			{
				return dir.List();
			}
			
			public override long FileLength(System.String name)
			{
				return dir.FileLength(name);
			}
			public override void  RenameFile(System.String from, System.String to)
			{
				dir.RenameFile(from, to);
			}
		}
	}
}