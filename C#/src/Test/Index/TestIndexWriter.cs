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
using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using Lock = Lucene.Net.Store.Lock;
using LockFactory = Lucene.Net.Store.LockFactory;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using SingleInstanceLockFactory = Lucene.Net.Store.SingleInstanceLockFactory;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using Token = Lucene.Net.Analysis.Token;
using TokenFilter = Lucene.Net.Analysis.TokenFilter;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using WhitespaceTokenizer = Lucene.Net.Analysis.WhitespaceTokenizer;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using StandardTokenizer = Lucene.Net.Analysis.Standard.StandardTokenizer;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using TermQuery = Lucene.Net.Search.TermQuery;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using _TestUtil = Lucene.Net.Util._TestUtil;

namespace Lucene.Net.Index
{
	
	
	/// <summary> </summary>
	/// <version>  $Id: TestIndexWriter.java 628085 2008-02-15 15:18:22Z mikemccand $
	/// </version>
	[TestFixture]
	public class TestIndexWriter : LuceneTestCase
	{
		public class MyRAMDirectory : RAMDirectory
		{
			private void  InitBlock(TestIndexWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private LockFactory myLockFactory;
			internal MyRAMDirectory(TestIndexWriter enclosingInstance)
			{
				InitBlock(enclosingInstance);
				lockFactory = null;
				myLockFactory = new SingleInstanceLockFactory();
			}
			public override Lock MakeLock(System.String name)
			{
				return myLockFactory.MakeLock(name);
			}
		}
		
		private class AnonymousClassAnalyzer : Analyzer
		{
			public AnonymousClassAnalyzer(TestIndexWriter enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			
			private class AnonymousClassTokenFilter : TokenFilter
			{
				public AnonymousClassTokenFilter(AnonymousClassAnalyzer enclosingInstance, TokenStream ts) : base(ts)
				{
					InitBlock(enclosingInstance);
				}
				private void  InitBlock(AnonymousClassAnalyzer enclosingInstance)
				{
					this.enclosingInstance = enclosingInstance;
				}
				private AnonymousClassAnalyzer enclosingInstance;
				public AnonymousClassAnalyzer Enclosing_Instance
				{
					get
					{
						return enclosingInstance;
					}
					
				}
				private int count = 0;
				
				public override Token Next()
				{
					if (count++ == 5)
					{
						throw new System.IO.IOException();
					}
					return input.Next();
				}
			}
			private void  InitBlock(TestIndexWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				return new AnonymousClassTokenFilter(this, new StandardTokenizer(reader));
			}
		}
		
		private class AnonymousClassAnalyzer1 : Analyzer
		{
			public AnonymousClassAnalyzer1(TestIndexWriter enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestIndexWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				return new CrashingFilter(Enclosing_Instance, fieldName, new WhitespaceTokenizer(reader));
			}
		}
		
		private class AnonymousClassAnalyzer2 : Analyzer
		{
			public AnonymousClassAnalyzer2(TestIndexWriter enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestIndexWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				return new CrashingFilter(Enclosing_Instance, fieldName, new WhitespaceTokenizer(reader));
			}
		}
		
		private class AnonymousClassThread : SupportClass.ThreadClass
		{
			public AnonymousClassThread(int NUM_ITER, IndexWriter writer, int finalI, TestIndexWriter enclosingInstance)
			{
				InitBlock(NUM_ITER, writer, finalI, enclosingInstance);
			}
			private void  InitBlock(int NUM_ITER, IndexWriter writer, int finalI, TestIndexWriter enclosingInstance)
			{
				this.NUM_ITER = NUM_ITER;
				this.writer = writer;
				this.finalI = finalI;
				this.enclosingInstance = enclosingInstance;
			}
			private int NUM_ITER;
			private IndexWriter writer;
			private int finalI;
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			override public void  Run()
			{
				try
				{
					for (int iter = 0; iter < NUM_ITER; iter++)
					{
						Document doc = new Document();
						doc.Add(new Field("contents", "here are some contents", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
						writer.AddDocument(doc);
						writer.AddDocument(doc);
						doc.Add(new Field("crash", "this should crash after 4 terms", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
						doc.Add(new Field("other", "this will not get indexed", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
						try
						{
							writer.AddDocument(doc);
							Assert.Fail("did not hit expected exception");
						}
						catch (System.IO.IOException)
						{
						}
						
						if (0 == finalI)
						{
							doc = new Document();
							doc.Add(new Field("contents", "here are some contents", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
							writer.AddDocument(doc);
							writer.AddDocument(doc);
						}
					}
				}
				catch (System.Exception t)
				{
					lock (this)
					{
						System.Console.Out.WriteLine(SupportClass.ThreadClass.Current().Name + ": ERROR: hit unexpected exception");
						System.Console.Out.WriteLine(t.StackTrace);
					}
					Assert.Fail();
				}
			}
		}
		
		private class AnonymousClassThread1 : SupportClass.ThreadClass
		{
			public AnonymousClassThread1(IndexWriter finalWriter, Document doc, System.Collections.ArrayList failure, TestIndexWriter enclosingInstance)
			{
				InitBlock(finalWriter, doc, failure, enclosingInstance);
			}
			private void  InitBlock(IndexWriter finalWriter, Document doc, System.Collections.ArrayList failure, TestIndexWriter enclosingInstance)
			{
				this.finalWriter = finalWriter;
				this.doc = doc;
				this.failure = failure;
				this.enclosingInstance = enclosingInstance;
			}
			private IndexWriter finalWriter;
			private Document doc;
			private System.Collections.ArrayList failure;
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			override public void  Run()
			{
				bool done = false;
				while (!done)
				{
					for (int i = 0; i < 100; i++)
					{
						try
						{
							finalWriter.AddDocument(doc);
						}
						catch (AlreadyClosedException)
						{
							done = true;
							break;
						}
						catch (System.NullReferenceException)
						{
							done = true;
							break;
						}
						catch (System.Exception e)
						{
							System.Console.Out.WriteLine(e.StackTrace);
							failure.Add(e);
							done = true;
							break;
						}
					}
					System.Threading.Thread.Sleep(0);
				}
			}
		}
		
		[Test]
		public virtual void  TestDocCount()
		{
			Directory dir = new RAMDirectory();
			
			IndexWriter writer = null;
			IndexReader reader = null;
			int i;
			
			IndexWriter.SetDefaultWriteLockTimeout(2000);
			Assert.AreEqual(2000, IndexWriter.GetDefaultWriteLockTimeout());
			
			writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			
			IndexWriter.SetDefaultWriteLockTimeout(1000);
			
			// add 100 documents
			for (i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}
			Assert.AreEqual(100, writer.DocCount());
			writer.Close();
			
			// delete 40 documents
			reader = IndexReader.Open(dir);
			for (i = 0; i < 40; i++)
			{
				reader.DeleteDocument(i);
			}
			reader.Close();
			
			// test doc count before segments are merged/index is optimized
			writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			Assert.AreEqual(100, writer.DocCount());
			writer.Close();
			
			reader = IndexReader.Open(dir);
			Assert.AreEqual(100, reader.MaxDoc());
			Assert.AreEqual(60, reader.NumDocs());
			reader.Close();
			
			// optimize the index and check that the new doc count is correct
			writer = new IndexWriter(dir, true, new WhitespaceAnalyzer());
			writer.Optimize();
			Assert.AreEqual(60, writer.DocCount());
			writer.Close();
			
			// check that the index reader gives the same numbers.
			reader = IndexReader.Open(dir);
			Assert.AreEqual(60, reader.MaxDoc());
			Assert.AreEqual(60, reader.NumDocs());
			reader.Close();
			
			// make sure opening a new index for create over
			// this existing one works correctly:
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			Assert.AreEqual(0, writer.DocCount());
			writer.Close();
		}
		
		private void  AddDoc(IndexWriter writer)
		{
			Document doc = new Document();
			doc.Add(new Field("content", "aaa", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
		}
		
		private void  AddDocWithIndex(IndexWriter writer, int index)
		{
			Document doc = new Document();
			doc.Add(new Field("content", "aaa " + index, Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("id", "" + index, Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
		}
		
		/*
		Test: make sure when we run out of disk space or hit
		random IOExceptions in any of the addIndexes(*) calls
		that 1) index is not corrupt (searcher can open/search
		it) and 2) transactional semantics are followed:
		either all or none of the incoming documents were in
		fact added.
		*/
		[Test]
		public virtual void  TestAddIndexOnDiskFull()
		{
			int START_COUNT = 57;
			int NUM_DIR = 50;
			int END_COUNT = START_COUNT + NUM_DIR * 25;
			
			bool debug = false;
			
			// Build up a bunch of dirs that have indexes which we
			// will then merge together by calling addIndexes(*):
			Directory[] dirs = new Directory[NUM_DIR];
			long inputDiskUsage = 0;
			for (int i = 0; i < NUM_DIR; i++)
			{
				dirs[i] = new RAMDirectory();
				IndexWriter writer = new IndexWriter(dirs[i], new WhitespaceAnalyzer(), true);
				for (int j = 0; j < 25; j++)
				{
					AddDocWithIndex(writer, 25 * i + j);
				}
				writer.Close();
				System.String[] files = dirs[i].List();
				for (int j = 0; j < files.Length; j++)
				{
					inputDiskUsage += dirs[i].FileLength(files[j]);
				}
			}
			
			// Now, build a starting index that has START_COUNT docs.  We
			// will then try to addIndexes into a copy of this:
			RAMDirectory startDir = new RAMDirectory();
			IndexWriter writer2 = new IndexWriter(startDir, new WhitespaceAnalyzer(), true);
			for (int j = 0; j < START_COUNT; j++)
			{
				AddDocWithIndex(writer2, j);
			}
			writer2.Close();
			
			// Make sure starting index seems to be working properly:
			Term searchTerm = new Term("content", "aaa");
			IndexReader reader = IndexReader.Open(startDir);
			Assert.AreEqual(57, reader.DocFreq(searchTerm), "first docFreq");
			
			IndexSearcher searcher = new IndexSearcher(reader);
			Hits hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(57, hits.Length(), "first number of hits");
			searcher.Close();
			reader.Close();
			
			// Iterate with larger and larger amounts of free
			// disk space.  With little free disk space,
			// addIndexes will certainly run out of space &
			// fail.  Verify that when this happens, index is
			// not corrupt and index in fact has added no
			// documents.  Then, we increase disk space by 2000
			// bytes each iteration.  At some point there is
			// enough free disk space and addIndexes should
			// succeed and index should show all documents were
			// added.
			
			// String[] files = startDir.list();
			long diskUsage = startDir.SizeInBytes();
			
			long startDiskUsage = 0;
			System.String[] files2 = startDir.List();
			for (int i = 0; i < files2.Length; i++)
			{
				startDiskUsage += startDir.FileLength(files2[i]);
			}
			
			for (int iter = 0; iter < 6; iter++)
			{
				
				if (debug)
					System.Console.Out.WriteLine("TEST: iter=" + iter);
				
				// Start with 100 bytes more than we are currently using:
				long diskFree = diskUsage + 100;
				
				bool autoCommit = iter % 2 == 0;
				int method = iter / 2;
				
				bool success = false;
				bool done = false;
				
				System.String methodName;
				if (0 == method)
				{
					methodName = "addIndexes(Directory[])";
				}
				else if (1 == method)
				{
					methodName = "addIndexes(IndexReader[])";
				}
				else
				{
					methodName = "addIndexesNoOptimize(Directory[])";
				}
				
				while (!done)
				{
					
					// Make a new dir that will enforce disk usage:
					MockRAMDirectory dir = new MockRAMDirectory(startDir);
					writer2 = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false);
					System.IO.IOException err = null;
					
					MergeScheduler ms = writer2.GetMergeScheduler();
					for (int x = 0; x < 2; x++)
					{
						if (ms is ConcurrentMergeScheduler)
						// This test intentionally produces exceptions
						// in the threads that CMS launches; we don't
						// want to pollute test output with these.
							if (0 == x)
								((ConcurrentMergeScheduler)ms).SetSuppressExceptions_ForNUnitTest();
							else
								((ConcurrentMergeScheduler) ms).ClearSuppressExceptions_ForNUnitTest();
						
						// Two loops: first time, limit disk space &
						// throw random IOExceptions; second time, no
						// disk space limit:
						
						double rate = 0.05;
						double diskRatio = ((double) diskFree) / diskUsage;
						long thisDiskFree;
						
						System.String testName = null;
						
						if (0 == x)
						{
							thisDiskFree = diskFree;
							if (diskRatio >= 2.0)
							{
								rate /= 2;
							}
							if (diskRatio >= 4.0)
							{
								rate /= 2;
							}
							if (diskRatio >= 6.0)
							{
								rate = 0.0;
							}
							if (debug)
								testName = "disk full test " + methodName + " with disk full at " + diskFree + " bytes autoCommit=" + autoCommit;
						}
						else
						{
							thisDiskFree = 0;
							rate = 0.0;
							if (debug)
								testName = "disk full test " + methodName + " with unlimited disk space autoCommit=" + autoCommit;
						}
						
						if (debug)
							System.Console.Out.WriteLine("\ncycle: " + testName);
						
						dir.SetMaxSizeInBytes(thisDiskFree);
						dir.SetRandomIOExceptionRate(rate, diskFree);
						
						try
						{
							
							if (0 == method)
							{
								writer2.AddIndexes(dirs);
							}
							else if (1 == method)
							{
								IndexReader[] readers = new IndexReader[dirs.Length];
								for (int i = 0; i < dirs.Length; i++)
								{
									readers[i] = IndexReader.Open(dirs[i]);
								}
								try
								{
									writer2.AddIndexes(readers);
								}
								finally
								{
									for (int i = 0; i < dirs.Length; i++)
									{
										readers[i].Close();
									}
								}
							}
							else
							{
								writer2.AddIndexesNoOptimize(dirs);
							}
							
							success = true;
							if (debug)
							{
								System.Console.Out.WriteLine("  success!");
							}
							
							if (0 == x)
							{
								done = true;
							}
						}
						catch (System.IO.IOException e)
						{
							success = false;
							err = e;
							if (debug)
							{
								System.Console.Out.WriteLine("  hit IOException: " + e);
								System.Console.Out.WriteLine(e.StackTrace);
							}
							
							if (1 == x)
							{
								System.Console.Out.WriteLine(e.StackTrace);
								Assert.Fail(methodName + " hit IOException after disk space was freed up");
							}
						}
						
						// Make sure all threads from
						// ConcurrentMergeScheduler are done
						_TestUtil.SyncConcurrentMerges(writer2);
						
						if (autoCommit)
						{
							
							// Whether we succeeded or failed, check that
							// all un-referenced files were in fact
							// deleted (ie, we did not create garbage).
							// Only check this when autoCommit is true:
							// when it's false, it's expected that there
							// are unreferenced files (ie they won't be
							// referenced until the "commit on close").
							// Just create a new IndexFileDeleter, have it
							// delete unreferenced files, then verify that
							// in fact no files were deleted:
							
							System.String successStr;
							if (success)
							{
								successStr = "success";
							}
							else
							{
								successStr = "IOException";
							}
							System.String message = methodName + " failed to delete unreferenced files after " + successStr + " (" + diskFree + " bytes)";
							AssertNoUnreferencedFiles(dir, message);
						}
						
						if (debug)
						{
							System.Console.Out.WriteLine("  now test readers");
						}
						
						// Finally, verify index is not corrupt, and, if
						// we succeeded, we see all docs added, and if we
						// failed, we see either all docs or no docs added
						// (transactional semantics):
						try
						{
							reader = IndexReader.Open(dir);
						}
						catch (System.IO.IOException e)
						{
							System.Console.Out.WriteLine(e.StackTrace);
							Assert.Fail(testName + ": exception when creating IndexReader: " + e);
						}
						int result = reader.DocFreq(searchTerm);
						if (success)
						{
							if (autoCommit && result != END_COUNT)
							{
								Assert.Fail(testName + ": method did not throw exception but docFreq('aaa') is " + result + " instead of expected " + END_COUNT);
							}
							else if (!autoCommit && result != START_COUNT)
							{
								Assert.Fail(testName + ": method did not throw exception but docFreq('aaa') is " + result + " instead of expected " + START_COUNT + " [autoCommit = false]");
							}
						}
						else
						{
							// On hitting exception we still may have added
							// all docs:
							if (result != START_COUNT && result != END_COUNT)
							{
								System.Console.Out.WriteLine(err.StackTrace);
								Assert.Fail(testName + ": method did throw exception but docFreq('aaa') is " + result + " instead of expected " + START_COUNT + " or " + END_COUNT);
							}
						}
						
						searcher = new IndexSearcher(reader);
						try
						{
							hits = searcher.Search(new TermQuery(searchTerm));
						}
						catch (System.IO.IOException e)
						{
							System.Console.Out.WriteLine(e.StackTrace);
							Assert.Fail(testName + ": exception when searching: " + e);
						}
						int result2 = hits.Length();
						if (success)
						{
							if (result2 != result)
							{
								Assert.Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + result);
							}
						}
						else
						{
							// On hitting exception we still may have added
							// all docs:
							if (result2 != result)
							{
								System.Console.Out.WriteLine(err.StackTrace);
								Assert.Fail(testName + ": method did throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + result);
							}
						}
						
						searcher.Close();
						reader.Close();
						if (debug)
						{
							System.Console.Out.WriteLine("  count is " + result);
						}
						
						if (done || result == END_COUNT)
						{
							break;
						}
					}
					
					if (debug)
					{
						System.Console.Out.WriteLine("  start disk = " + startDiskUsage + "; input disk = " + inputDiskUsage + "; max used = " + dir.GetMaxUsedSizeInBytes());
					}
					
					if (done)
					{
						// Javadocs state that temp free Directory space
						// required is at most 2X total input size of
						// indices so let's make sure:
						Assert.IsTrue(
							(dir.GetMaxUsedSizeInBytes() - startDiskUsage) < 2 * (startDiskUsage + inputDiskUsage),
							"max free Directory space required exceeded 1X the total input index sizes during " + methodName + ": max temp usage = " + (dir.GetMaxUsedSizeInBytes() - startDiskUsage) + " bytes; " + "starting disk usage = " + startDiskUsage + " bytes; " + "input index disk usage = " + inputDiskUsage + " bytes"
						);
					}
					
					writer2.Close();
					
					// Wait for all BG threads to finish else
					// dir.close() will throw IOException because
					// there are still open files
					_TestUtil.SyncConcurrentMerges(ms);
					
					dir.Close();
					
					// Try again with 2000 more bytes of free space:
					diskFree += 2000;
				}
			}
			
			startDir.Close();
		}
		
		/*
		* Make sure IndexWriter cleans up on hitting a disk
		* full exception in addDocument.
		*/
		[Test]
		public virtual void  TestAddDocumentOnDiskFull()
		{
			
			bool debug = false;
			
			for (int pass = 0; pass < 3; pass++)
			{
				if (debug)
					System.Console.Out.WriteLine("TEST: pass=" + pass);
				bool autoCommit = pass == 0;
				bool doAbort = pass == 2;
				long diskFree = 200;
				while (true)
				{
					if (debug)
						System.Console.Out.WriteLine("TEST: cycle: diskFree=" + diskFree);
					MockRAMDirectory dir = new MockRAMDirectory();
					dir.SetMaxSizeInBytes(diskFree);
					IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
					
					MergeScheduler ms = writer.GetMergeScheduler();
					if (ms is ConcurrentMergeScheduler)
					// This test intentionally produces exceptions
					// in the threads that CMS launches; we don't
					// want to pollute test output with these.
						((ConcurrentMergeScheduler)ms).SetSuppressExceptions_ForNUnitTest();
					
					bool hitError = false;
					try
					{
						for (int i = 0; i < 200; i++)
						{
							AddDoc(writer);
						}
					}
					catch (System.IO.IOException e)
					{
						if (debug)
						{
							System.Console.Out.WriteLine("TEST: exception on addDoc");
							System.Console.Out.WriteLine(e.StackTrace);
						}
						hitError = true;
					}
					
					if (hitError)
					{
						if (doAbort)
						{
							writer.Abort();
						}
						else
						{
							try
							{
								writer.Close();
							}
							catch (System.IO.IOException e)
							{
								if (debug)
								{
									System.Console.Out.WriteLine("TEST: exception on close");
									System.Console.Out.WriteLine(e.StackTrace);
								}
								dir.SetMaxSizeInBytes(0);
								writer.Close();
							}
						}
						
						_TestUtil.SyncConcurrentMerges(ms);
						
						AssertNoUnreferencedFiles(dir, "after disk full during addDocument with autoCommit=" + autoCommit);
						
						// Make sure reader can open the index:
						IndexReader.Open(dir).Close();
						
						dir.Close();
						
						// Now try again w/ more space:
						diskFree += 500;
					}
					else
					{
						_TestUtil.SyncConcurrentMerges(writer);
						dir.Close();
						break;
					}
				}
			}
		}
		
		public static void  AssertNoUnreferencedFiles(Directory dir, System.String message)
		{
			System.String[] startFiles = dir.List();
			SegmentInfos infos = new SegmentInfos();
			infos.Read(dir);
			new IndexFileDeleter(dir, new KeepOnlyLastCommitDeletionPolicy(), infos, null, null);
			System.String[] endFiles = dir.List();
			
			System.Array.Sort(startFiles);
			System.Array.Sort(endFiles);

			//if (!startFiles.Equals(endFiles))
			//{
			//    Assert.Fail(message + ": before delete:\n    " + ArrayToString(startFiles) + "\n  after delete:\n    " + ArrayToString(endFiles));
			//}
			string startArray = ArrayToString(startFiles);
			string endArray = ArrayToString(endFiles);
			if (!startArray.Equals(endArray))
			{
				Assert.Fail(message + ": before delete:\n    " + startArray + "\n  after delete:\n    " + endArray);
			}
		}
		
		/// <summary> Make sure we skip wicked long terms.</summary>
		[Test]
		public virtual void TestWickedLongTerm()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(), true);
			
			char[] chars = new char[16383];
			for (int index = 0; index < chars.Length; index++)
				chars.SetValue('x', index);
			Document doc = new Document();
			System.String bigTerm = new System.String(chars);
			
			// Max length term is 16383, so this contents produces
			// a too-long term:
			System.String contents = "abc xyz x" + bigTerm + " another term";
			doc.Add(new Field("content", contents, Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			// Make sure we can add another normal document
			doc = new Document();
			doc.Add(new Field("content", "abc bbb ccc", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			writer.Close();
			
			IndexReader reader = IndexReader.Open(dir);
			
			// Make sure all terms < max size were indexed
			Assert.AreEqual(2, reader.DocFreq(new Term("content", "abc")));
			Assert.AreEqual(1, reader.DocFreq(new Term("content", "bbb")));
			Assert.AreEqual(1, reader.DocFreq(new Term("content", "term")));
			Assert.AreEqual(1, reader.DocFreq(new Term("content", "another")));
			
			// Make sure position is still incremented when
			// massive term is skipped:
			TermPositions tps = reader.TermPositions(new Term("content", "another"));
			Assert.IsTrue(tps.Next());
			Assert.AreEqual(1, tps.Freq());
			Assert.AreEqual(3, tps.NextPosition());
			
			// Make sure the doc that has the massive term is in
			// the index:
			Assert.AreEqual(2, reader.NumDocs(), "document with wicked long term should is not in the index!");
			
			reader.Close();
			
			// Make sure we can add a document with exactly the
			// maximum length term, and search on that term:
			doc = new Document();
			doc.Add(new Field("content", bigTerm, Field.Store.NO, Field.Index.TOKENIZED));
			StandardAnalyzer sa = new StandardAnalyzer();
			sa.SetMaxTokenLength(100000);
			writer = new IndexWriter(dir, sa);
			writer.AddDocument(doc);
			writer.Close();
			reader = IndexReader.Open(dir);
			Assert.AreEqual(1, reader.DocFreq(new Term("content", bigTerm)));
			reader.Close();
			
			dir.Close();
		}
		
		[Test]
		public virtual void  TestOptimizeMaxNumSegments()
		{
			
			MockRAMDirectory dir = new MockRAMDirectory();
			
			Document doc = new Document();
			doc.Add(new Field("content", "aaa", Field.Store.YES, Field.Index.TOKENIZED));
			
			for (int numDocs = 38; numDocs < 500; numDocs += 38)
			{
				IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
				LogDocMergePolicy ldmp = new LogDocMergePolicy();
				ldmp.SetMinMergeDocs(1);
				writer.SetMergePolicy(ldmp);
				writer.SetMergeFactor(5);
				writer.SetMaxBufferedDocs(2);
				for (int j = 0; j < numDocs; j++)
					writer.AddDocument(doc);
				writer.Close();
				
				SegmentInfos sis = new SegmentInfos();
				sis.Read(dir);
				int segCount = sis.Count;
				
				writer = new IndexWriter(dir, new WhitespaceAnalyzer());
				writer.SetMergePolicy(ldmp);
				writer.SetMergeFactor(5);
				writer.Optimize(3);
				writer.Close();
				
				sis = new SegmentInfos();
				sis.Read(dir);
				int optSegCount = sis.Count;
				
				if (segCount < 3)
					Assert.AreEqual(segCount, optSegCount);
				else
					Assert.AreEqual(3, optSegCount);
			}
		}
		
		[Test]
		public virtual void  TestOptimizeMaxNumSegments2()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			
			Document doc = new Document();
			doc.Add(new Field("content", "aaa", Field.Store.YES, Field.Index.TOKENIZED));
			
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			LogDocMergePolicy ldmp = new LogDocMergePolicy();
			ldmp.SetMinMergeDocs(1);
			writer.SetMergePolicy(ldmp);
			writer.SetMergeFactor(4);
			writer.SetMaxBufferedDocs(2);
			
			for (int iter = 0; iter < 10; iter++)
			{
				
				for (int i = 0; i < 19; i++)
					writer.AddDocument(doc);
				
				writer.Flush();
				
				SegmentInfos sis = new SegmentInfos();
				((ConcurrentMergeScheduler) writer.GetMergeScheduler()).Sync();
				sis.Read(dir);
				
				int segCount = sis.Count;
				
				writer.Optimize(7);
				
				sis = new SegmentInfos();
				((ConcurrentMergeScheduler) writer.GetMergeScheduler()).Sync();
				sis.Read(dir);
				int optSegCount = sis.Count;
				
				if (segCount < 7)
					Assert.AreEqual(segCount, optSegCount);
				else
					Assert.AreEqual(7, optSegCount);
			}
		}
		
		/// <summary> Make sure optimize doesn't use any more than 1X
		/// starting index size as its temporary free space
		/// required.
		/// </summary>
		[Test]
		public virtual void  TestOptimizeTempSpaceUsage()
		{
			
			MockRAMDirectory dir = new MockRAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			for (int j = 0; j < 500; j++)
			{
				AddDocWithIndex(writer, j);
			}
			writer.Close();
			
			long startDiskUsage = 0;
			System.String[] files = dir.List();
			for (int i = 0; i < files.Length; i++)
			{
				startDiskUsage += dir.FileLength(files[i]);
			}
			
			dir.ResetMaxUsedSizeInBytes();
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
			writer.Optimize();
			writer.Close();
			long maxDiskUsage = dir.GetMaxUsedSizeInBytes();
			
			Assert.IsTrue(
				maxDiskUsage <= 2 * startDiskUsage,
				"optimized used too much temporary space: starting usage was " + startDiskUsage + " bytes; max temp usage was " + maxDiskUsage + " but should have been " + (2 * startDiskUsage) + " (= 2X starting usage)"
			);
			dir.Close();
		}
		
		internal static System.String ArrayToString(System.String[] l)
		{
			System.String s = "";
			for (int i = 0; i < l.Length; i++)
			{
				if (i > 0)
				{
					s += "\n    ";
				}
				s += l[i];
			}
			return s;
		}
		
		// Make sure we can open an index for create even when a
		// reader holds it open (this fails pre lock-less
		// commits on windows):
		[Test]
		public virtual void  TestCreateWithReader()
		{
			System.String tempDir = System.IO.Path.GetTempPath();
			if (tempDir == null)
				throw new System.IO.IOException("java.io.tmpdir undefined, cannot run test");
			System.IO.FileInfo indexDir = new System.IO.FileInfo(tempDir + "\\" + "lucenetestindexwriter");
			
			try
			{
				Directory dir = FSDirectory.GetDirectory(indexDir);
				
				// add one document & close writer
				IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
				AddDoc(writer);
				writer.Close();
				
				// now open reader:
				IndexReader reader = IndexReader.Open(dir);
				Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
				
				// now open index for create:
				writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
				Assert.AreEqual(writer.DocCount(), 0, "should be zero documents");
				AddDoc(writer);
				writer.Close();
				
				Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
				IndexReader reader2 = IndexReader.Open(dir);
				Assert.AreEqual(reader2.NumDocs(), 1, "should be one document");
				reader.Close();
				reader2.Close();
			}
			finally
			{
				RmDir(indexDir);
			}
		}
		
		
		// Same test as above, but use IndexWriter constructor
		// that takes File:
		[Test]
		public virtual void  TestCreateWithReader2()
		{
			System.String tempDir = System.IO.Path.GetTempPath();
			if (tempDir == null)
				throw new System.IO.IOException("java.io.tmpdir undefined, cannot run test");
			System.IO.FileInfo indexDir = new System.IO.FileInfo(tempDir + "\\" + "lucenetestindexwriter");
			try
			{
				// add one document & close writer
				IndexWriter writer = new IndexWriter(indexDir, new WhitespaceAnalyzer(), true);
				AddDoc(writer);
				writer.Close();
				
				// now open reader:
				IndexReader reader = IndexReader.Open(indexDir);
				Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
				
				// now open index for create:
				writer = new IndexWriter(indexDir, new WhitespaceAnalyzer(), true);
				Assert.AreEqual(writer.DocCount(), 0, "should be zero documents");
				AddDoc(writer);
				writer.Close();
				
				Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
				IndexReader reader2 = IndexReader.Open(indexDir);
				Assert.AreEqual(reader2.NumDocs(), 1, "should be one document");
				reader.Close();
				reader2.Close();
			}
			finally
			{
				RmDir(indexDir);
			}
		}
		
		// Same test as above, but use IndexWriter constructor
		// that takes String:
		[Test]
		public virtual void  TestCreateWithReader3()
		{
			System.String tempDir = SupportClass.AppSettings.Get("tempDir", "");
			if (tempDir == null)
				throw new System.IO.IOException("java.io.tmpdir undefined, cannot run test");
			
			System.String dirName = tempDir + "/lucenetestindexwriter";
			try
			{
				
				// add one document & close writer
				IndexWriter writer = new IndexWriter(dirName, new WhitespaceAnalyzer(), true);
				AddDoc(writer);
				writer.Close();
				
				// now open reader:
				IndexReader reader = IndexReader.Open(dirName);
				Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
				
				// now open index for create:
				writer = new IndexWriter(dirName, new WhitespaceAnalyzer(), true);
				Assert.AreEqual(writer.DocCount(), 0, "should be zero documents");
				AddDoc(writer);
				writer.Close();
				
				Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
				IndexReader reader2 = IndexReader.Open(dirName);
				Assert.AreEqual(reader2.NumDocs(), 1, "should be one document");
				reader.Close();
				reader2.Close();
			}
			finally
			{
				RmDir(new System.IO.FileInfo(dirName));
			}
		}
		
		// Simulate a writer that crashed while writing segments
		// file: make sure we can still open the index (ie,
		// gracefully fallback to the previous segments file),
		// and that we can add to the index:
		[Test]
		public virtual void  TestSimulatedCrashedWriter()
		{
			Directory dir = new RAMDirectory();
			
			IndexWriter writer = null;
			
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			
			// add 100 documents
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}
			
			// close
			writer.Close();
			
			long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
			Assert.IsTrue(gen > 1, "segment generation should be > 1 but got " + gen);
			
			// Make the next segments file, with last byte
			// missing, to simulate a writer that crashed while
			// writing segments file:
			System.String fileNameIn = SegmentInfos.GetCurrentSegmentFileName(dir);
			System.String fileNameOut = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen);
			IndexInput in_Renamed = dir.OpenInput(fileNameIn);
			IndexOutput out_Renamed = dir.CreateOutput(fileNameOut);
			long length = in_Renamed.Length();
			for (int i = 0; i < length - 1; i++)
			{
				out_Renamed.WriteByte(in_Renamed.ReadByte());
			}
			in_Renamed.Close();
			out_Renamed.Close();
			
			IndexReader reader = null;
			try
			{
				reader = IndexReader.Open(dir);
			}
			catch (System.Exception)
			{
				Assert.Fail("reader failed to open on a crashed index");
			}
			reader.Close();
			
			try
			{
				writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			}
			catch (System.Exception)
			{
				Assert.Fail("writer failed to open on a crashed index");
			}
			
			// add 100 documents
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}
			
			// close
			writer.Close();
		}
		
		// Simulate a corrupt index by removing last byte of
		// latest segments file and make sure we get an
		// IOException trying to open the index:
		[Test]
		public virtual void  TestSimulatedCorruptIndex1()
		{
			Directory dir = new RAMDirectory();
			
			IndexWriter writer = null;
			
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			
			// add 100 documents
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}
			
			// close
			writer.Close();
			
			long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
			Assert.IsTrue(gen > 1, "segment generation should be > 1 but got " + gen);
			
			System.String fileNameIn = SegmentInfos.GetCurrentSegmentFileName(dir);
			System.String fileNameOut = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen);
			IndexInput in_Renamed = dir.OpenInput(fileNameIn);
			IndexOutput out_Renamed = dir.CreateOutput(fileNameOut);
			long length = in_Renamed.Length();
			for (int i = 0; i < length - 1; i++)
			{
				out_Renamed.WriteByte(in_Renamed.ReadByte());
			}
			in_Renamed.Close();
			out_Renamed.Close();
			dir.DeleteFile(fileNameIn);
			
			IndexReader reader = null;
			try
			{
				reader = IndexReader.Open(dir);
				Assert.Fail("reader did not hit IOException on opening a corrupt index");
			}
			catch (System.Exception)
			{
			}
			if (reader != null)
			{
				reader.Close();
			}
		}
		
		[Test]
		public virtual void  TestChangesAfterClose()
		{
			Directory dir = new RAMDirectory();
			
			IndexWriter writer = null;
			
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			AddDoc(writer);
			
			// close
			writer.Close();
			try
			{
				AddDoc(writer);
				Assert.Fail("did not hit AlreadyClosedException");
			}
			catch (AlreadyClosedException)
			{
				// expected
			}
		}
		
		
		// Simulate a corrupt index by removing one of the cfs
		// files and make sure we get an IOException trying to
		// open the index:
		[Test]
		public virtual void  TestSimulatedCorruptIndex2()
		{
			Directory dir = new RAMDirectory();
			
			IndexWriter writer = null;
			
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			
			// add 100 documents
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}
			
			// close
			writer.Close();
			
			long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
			Assert.IsTrue(gen > 1, "segment generation should be > 1 but got " + gen);
			
			System.String[] files = dir.List();
			for (int i = 0; i < files.Length; i++)
			{
				if (files[i].EndsWith(".cfs"))
				{
					dir.DeleteFile(files[i]);
					break;
				}
			}
			
			IndexReader reader = null;
			try
			{
				reader = IndexReader.Open(dir);
				Assert.Fail("reader did not hit IOException on opening a corrupt index");
			}
			catch (System.Exception)
			{
			}
			if (reader != null)
			{
				reader.Close();
			}
		}
		
		/*
		* Simple test for "commit on close": open writer with
		* autoCommit=false, so it will only commit on close,
		* then add a bunch of docs, making sure reader does not
		* see these docs until writer is closed.
		*/
		[Test]
		public virtual void  TestCommitOnClose()
		{
			Directory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < 14; i++)
			{
				AddDoc(writer);
			}
			writer.Close();
			
			Term searchTerm = new Term("content", "aaa");
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(14, hits.Length(), "first number of hits");
			searcher.Close();
			
			IndexReader reader = IndexReader.Open(dir);
			
			writer = new IndexWriter(dir, false, new WhitespaceAnalyzer());
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 11; j++)
				{
					AddDoc(writer);
				}
				searcher = new IndexSearcher(dir);
				hits = searcher.Search(new TermQuery(searchTerm));
				Assert.AreEqual(14, hits.Length(), "reader incorrectly sees changes from writer with autoCommit disabled");
				searcher.Close();
				Assert.IsTrue(reader.IsCurrent(), "reader should have still been current");
			}
			
			// Now, close the writer:
			writer.Close();
			Assert.IsFalse(reader.IsCurrent(), "reader should not be current now");
			
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(47, hits.Length(), "reader did not see changes after writer was closed");
			searcher.Close();
		}
		
		/*
		* Simple test for "commit on close": open writer with
		* autoCommit=false, so it will only commit on close,
		* then add a bunch of docs, making sure reader does not
		* see them until writer has closed.  Then instead of
		* closing the writer, call abort and verify reader sees
		* nothing was added.  Then verify we can open the index
		* and add docs to it.
		*/
		[Test]
		public virtual void  TestCommitOnCloseAbort()
		{
			Directory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetMaxBufferedDocs(10);
			for (int i = 0; i < 14; i++)
			{
				AddDoc(writer);
			}
			writer.Close();
			
			Term searchTerm = new Term("content", "aaa");
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(14, hits.Length(), "first number of hits");
			searcher.Close();
			
			writer = new IndexWriter(dir, false, new WhitespaceAnalyzer(), false);
			writer.SetMaxBufferedDocs(10);
			for (int j = 0; j < 17; j++)
			{
				AddDoc(writer);
			}
			// Delete all docs:
			writer.DeleteDocuments(searchTerm);
			
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(14, hits.Length(), "reader incorrectly sees changes from writer with autoCommit disabled");
			searcher.Close();
			
			// Now, close the writer:
			writer.Abort();
			
			AssertNoUnreferencedFiles(dir, "unreferenced files remain after abort()");
			
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(14, hits.Length(), "saw changes after writer.abort");
			searcher.Close();
			
			// Now make sure we can re-open the index, add docs,
			// and all is good:
			writer = new IndexWriter(dir, false, new WhitespaceAnalyzer(), false);
			writer.SetMaxBufferedDocs(10);
			for (int i = 0; i < 12; i++)
			{
				for (int j = 0; j < 17; j++)
				{
					AddDoc(writer);
				}
				searcher = new IndexSearcher(dir);
				hits = searcher.Search(new TermQuery(searchTerm));
				Assert.AreEqual(14, hits.Length(), "reader incorrectly sees changes from writer with autoCommit disabled");
				searcher.Close();
			}
			
			writer.Close();
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(218, hits.Length(), "didn't see changes after close");
			searcher.Close();
			
			dir.Close();
		}
		
		/*
		* Verify that a writer with "commit on close" indeed
		* cleans up the temp segments created after opening
		* that are not referenced by the starting segments
		* file.  We check this by using MockRAMDirectory to
		* measure max temp disk space used.
		*/
		[Test]
		public virtual void  TestCommitOnCloseDiskUsage()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			for (int j = 0; j < 30; j++)
			{
				AddDocWithIndex(writer, j);
			}
			writer.Close();
			dir.ResetMaxUsedSizeInBytes();
			
			long startDiskUsage = dir.GetMaxUsedSizeInBytes();
			writer = new IndexWriter(dir, false, new WhitespaceAnalyzer(), false);
			for (int j = 0; j < 1470; j++)
			{
				AddDocWithIndex(writer, j);
			}
			long midDiskUsage = dir.GetMaxUsedSizeInBytes();
			dir.ResetMaxUsedSizeInBytes();
			writer.Optimize();
			writer.Close();
			long endDiskUsage = dir.GetMaxUsedSizeInBytes();
			
			// Ending index is 50X as large as starting index; due
			// to 2X disk usage normally we allow 100X max
			// transient usage.  If something is wrong w/ deleter
			// and it doesn't delete intermediate segments then it
			// will exceed this 100X:
			// System.out.println("start " + startDiskUsage + "; mid " + midDiskUsage + ";end " + endDiskUsage);
			Assert.IsTrue(midDiskUsage < 100 * startDiskUsage, "writer used to much space while adding documents when autoCommit=false");
			Assert.IsTrue(endDiskUsage < 100 * startDiskUsage, "writer used to much space after close when autoCommit=false");
		}
		
		
		/*
		* Verify that calling optimize when writer is open for
		* "commit on close" works correctly both for abort()
		* and close().
		*/
		[Test]
		public virtual void  TestCommitOnCloseOptimize()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetMaxBufferedDocs(10);
			for (int j = 0; j < 17; j++)
			{
				AddDocWithIndex(writer, j);
			}
			writer.Close();
			
			writer = new IndexWriter(dir, false, new WhitespaceAnalyzer(), false);
			writer.Optimize();
			
			// Open a reader before closing (commiting) the writer:
			IndexReader reader = IndexReader.Open(dir);
			
			// Reader should see index as unoptimized at this
			// point:
			Assert.IsFalse(reader.IsOptimized(), "Reader incorrectly sees that the index is optimized");
			reader.Close();
			
			// Abort the writer:
			writer.Abort();
			AssertNoUnreferencedFiles(dir, "aborted writer after optimize");
			
			// Open a reader after aborting writer:
			reader = IndexReader.Open(dir);
			
			// Reader should still see index as unoptimized:
			Assert.IsFalse(reader.IsOptimized(), "Reader incorrectly sees that the index is optimized");
			reader.Close();
			
			writer = new IndexWriter(dir, false, new WhitespaceAnalyzer(), false);
			writer.Optimize();
			writer.Close();
			AssertNoUnreferencedFiles(dir, "aborted writer after optimize");
			
			// Open a reader after aborting writer:
			reader = IndexReader.Open(dir);
			
			// Reader should still see index as unoptimized:
			Assert.IsTrue(reader.IsOptimized(), "Reader incorrectly sees that the index is unoptimized");
			reader.Close();
		}
		
		[Test]
		public virtual void  TestIndexNoDocuments()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.Flush();
			writer.Close();
			
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(0, reader.MaxDoc());
			Assert.AreEqual(0, reader.NumDocs());
			reader.Close();
			
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
			writer.Flush();
			writer.Close();
			
			reader = IndexReader.Open(dir);
			Assert.AreEqual(0, reader.MaxDoc());
			Assert.AreEqual(0, reader.NumDocs());
			reader.Close();
		}
		
		[Test]
		public virtual void  TestManyFields()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetMaxBufferedDocs(10);
			for (int j = 0; j < 100; j++)
			{
				Document doc = new Document();
				doc.Add(new Field("a" + j, "aaa" + j, Field.Store.YES, Field.Index.TOKENIZED));
				doc.Add(new Field("b" + j, "aaa" + j, Field.Store.YES, Field.Index.TOKENIZED));
				doc.Add(new Field("c" + j, "aaa" + j, Field.Store.YES, Field.Index.TOKENIZED));
				doc.Add(new Field("d" + j, "aaa", Field.Store.YES, Field.Index.TOKENIZED));
				doc.Add(new Field("e" + j, "aaa", Field.Store.YES, Field.Index.TOKENIZED));
				doc.Add(new Field("f" + j, "aaa", Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
			}
			writer.Close();
			
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(100, reader.MaxDoc());
			Assert.AreEqual(100, reader.NumDocs());
			for (int j = 0; j < 100; j++)
			{
				Assert.AreEqual(1, reader.DocFreq(new Term("a" + j, "aaa" + j)));
				Assert.AreEqual(1, reader.DocFreq(new Term("b" + j, "aaa" + j)));
				Assert.AreEqual(1, reader.DocFreq(new Term("c" + j, "aaa" + j)));
				Assert.AreEqual(1, reader.DocFreq(new Term("d" + j, "aaa")));
				Assert.AreEqual(1, reader.DocFreq(new Term("e" + j, "aaa")));
				Assert.AreEqual(1, reader.DocFreq(new Term("f" + j, "aaa")));
			}
			reader.Close();
			dir.Close();
		}
		
		[Test]
		public virtual void  TestSmallRAMBuffer()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetRAMBufferSizeMB(0.000001);
			int lastNumFile = dir.List().Length;
			for (int j = 0; j < 9; j++)
			{
				Document doc = new Document();
				doc.Add(new Field("field", "aaa" + j, Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
				int numFile = dir.List().Length;
				// Verify that with a tiny RAM buffer we see new
				// segment after every doc
				Assert.IsTrue(numFile > lastNumFile);
				lastNumFile = numFile;
			}
			writer.Close();
			dir.Close();
		}
		
		// Make sure it's OK to change RAM buffer size and
		// maxBufferedDocs in a write session
		[Test]
		public virtual void  TestChangingRAMBuffer()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetMaxBufferedDocs(10);
			writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
			
			long lastGen = - 1;
			for (int j = 1; j < 52; j++)
			{
				Document doc = new Document();
				doc.Add(new Field("field", "aaa" + j, Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
				_TestUtil.SyncConcurrentMerges(writer);
				long gen = SegmentInfos.GenerationFromSegmentsFileName(SegmentInfos.GetCurrentSegmentFileName(dir.List()));
				if (j == 1)
					lastGen = gen;
				else if (j < 10)
				// No new files should be created
					Assert.AreEqual(gen, lastGen);
				else if (10 == j)
				{
					Assert.IsTrue(gen > lastGen);
					lastGen = gen;
					writer.SetRAMBufferSizeMB(0.000001);
					writer.SetMaxBufferedDocs(IndexWriter.DISABLE_AUTO_FLUSH);
				}
				else if (j < 20)
				{
					Assert.IsTrue(gen > lastGen);
					lastGen = gen;
				}
				else if (20 == j)
				{
					writer.SetRAMBufferSizeMB(16);
					writer.SetMaxBufferedDocs(IndexWriter.DISABLE_AUTO_FLUSH);
					lastGen = gen;
				}
				else if (j < 30)
				{
					Assert.AreEqual(gen, lastGen);
				}
				else if (30 == j)
				{
					writer.SetRAMBufferSizeMB(0.000001);
					writer.SetMaxBufferedDocs(IndexWriter.DISABLE_AUTO_FLUSH);
				}
				else if (j < 40)
				{
					Assert.IsTrue(gen > lastGen);
					lastGen = gen;
				}
				else if (40 == j)
				{
					writer.SetMaxBufferedDocs(10);
					writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
					lastGen = gen;
				}
				else if (j < 50)
				{
					Assert.AreEqual(gen, lastGen);
					writer.SetMaxBufferedDocs(10);
					writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
				}
				else if (50 == j)
				{
					Assert.IsTrue(gen > lastGen);
				}
			}
			writer.Close();
			dir.Close();
		}
		
		[Test]
		public virtual void  TestChangingRAMBuffer2()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetMaxBufferedDocs(10);
			writer.SetMaxBufferedDeleteTerms(10);
			writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
			
			for (int j = 1; j < 52; j++)
			{
				Document doc = new Document();
				doc.Add(new Field("field", "aaa" + j, Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
			}
			
			long lastGen = - 1;
			for (int j = 1; j < 52; j++)
			{
				writer.DeleteDocuments(new Term("field", "aaa" + j));
				_TestUtil.SyncConcurrentMerges(writer);
				long gen = SegmentInfos.GenerationFromSegmentsFileName(SegmentInfos.GetCurrentSegmentFileName(dir.List()));
				if (j == 1)
					lastGen = gen;
				else if (j < 10)
				{
					// No new files should be created
					Assert.AreEqual(gen, lastGen);
				}
				else if (10 == j)
				{
					Assert.IsTrue(gen > lastGen);
					lastGen = gen;
					writer.SetRAMBufferSizeMB(0.000001);
					writer.SetMaxBufferedDeleteTerms(IndexWriter.DISABLE_AUTO_FLUSH);
				}
				else if (j < 20)
				{
					Assert.IsTrue(gen > lastGen);
					lastGen = gen;
				}
				else if (20 == j)
				{
					writer.SetRAMBufferSizeMB(16);
					writer.SetMaxBufferedDeleteTerms(IndexWriter.DISABLE_AUTO_FLUSH);
					lastGen = gen;
				}
				else if (j < 30)
				{
					Assert.AreEqual(gen, lastGen);
				}
				else if (30 == j)
				{
					writer.SetRAMBufferSizeMB(0.000001);
					writer.SetMaxBufferedDeleteTerms(IndexWriter.DISABLE_AUTO_FLUSH);
				}
				else if (j < 40)
				{
					Assert.IsTrue(gen > lastGen);
					lastGen = gen;
				}
				else if (40 == j)
				{
					writer.SetMaxBufferedDeleteTerms(10);
					writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
					lastGen = gen;
				}
				else if (j < 50)
				{
					Assert.AreEqual(gen, lastGen);
					writer.SetMaxBufferedDeleteTerms(10);
					writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
				}
				else if (50 == j)
				{
					Assert.IsTrue(gen > lastGen);
				}
			}
			writer.Close();
			dir.Close();
		}
		
		[Test]
		public virtual void  TestDiverseDocs()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetRAMBufferSizeMB(0.5);
			System.Random rand = new System.Random((System.Int32) 31415);
			for (int i = 0; i < 3; i++)
			{
				// First, docs where every term is unique (heavy on
				// Posting instances)
				for (int j = 0; j < 100; j++)
				{
					Document doc = new Document();
					for (int k = 0; k < 100; k++)
					{
						doc.Add(new Field("field", System.Convert.ToString(rand.Next()), Field.Store.YES, Field.Index.TOKENIZED));
					}
					writer.AddDocument(doc);
				}
				
				// Next, many single term docs where only one term
				// occurs (heavy on byte blocks)
				for (int j = 0; j < 100; j++)
				{
					Document doc = new Document();
					doc.Add(new Field("field", "aaa aaa aaa aaa aaa aaa aaa aaa aaa aaa", Field.Store.YES, Field.Index.TOKENIZED));
					writer.AddDocument(doc);
				}
				
				// Next, many single term docs where only one term
				// occurs but the terms are very long (heavy on
				// char[] arrays)
				for (int j = 0; j < 100; j++)
				{
					System.Text.StringBuilder b = new System.Text.StringBuilder();
					System.String x = System.Convert.ToString(j) + ".";
					for (int k = 0; k < 1000; k++)
						b.Append(x);
					System.String longTerm = b.ToString();
					
					Document doc = new Document();
					doc.Add(new Field("field", longTerm, Field.Store.YES, Field.Index.TOKENIZED));
					writer.AddDocument(doc);
				}
			}
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(new TermQuery(new Term("field", "aaa")));
			Assert.AreEqual(300, hits.Length());
			searcher.Close();
			
			dir.Close();
		}
		
		[Test]
		public virtual void  TestEnablingNorms()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetMaxBufferedDocs(10);
			// Enable norms for only 1 doc, pre flush
			for (int j = 0; j < 10; j++)
			{
				Document doc = new Document();
				Field f = new Field("field", "aaa", Field.Store.YES, Field.Index.TOKENIZED);
				if (j != 8)
				{
					f.SetOmitNorms(true);
				}
				doc.Add(f);
				writer.AddDocument(doc);
			}
			writer.Close();
			
			Term searchTerm = new Term("field", "aaa");
			
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(10, hits.Length());
			searcher.Close();
			
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetMaxBufferedDocs(10);
			// Enable norms for only 1 doc, post flush
			for (int j = 0; j < 27; j++)
			{
				Document doc = new Document();
				Field f = new Field("field", "aaa", Field.Store.YES, Field.Index.TOKENIZED);
				if (j != 26)
				{
					f.SetOmitNorms(true);
				}
				doc.Add(f);
				writer.AddDocument(doc);
			}
			writer.Close();
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(27, hits.Length());
			searcher.Close();
			
			IndexReader reader = IndexReader.Open(dir);
			reader.Close();
			
			dir.Close();
		}
		
		[Test]
		public virtual void  TestHighFreqTerm()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetRAMBufferSizeMB(0.01);
			writer.SetMaxFieldLength(100000000);
			// Massive doc that has 128 K a's
			System.Text.StringBuilder b = new System.Text.StringBuilder(1024 * 1024);
			for (int i = 0; i < 4096; i++)
			{
				b.Append(" a a a a a a a a");
				b.Append(" a a a a a a a a");
				b.Append(" a a a a a a a a");
				b.Append(" a a a a a a a a");
			}
			Document doc = new Document();
			doc.Add(new Field("field", b.ToString(), Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
			writer.AddDocument(doc);
			writer.Close();
			
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(1, reader.MaxDoc());
			Assert.AreEqual(1, reader.NumDocs());
			Term t = new Term("field", "a");
			Assert.AreEqual(1, reader.DocFreq(t));
			TermDocs td = reader.TermDocs(t);
			td.Next();
			Assert.AreEqual(128 * 1024, td.Freq());
			reader.Close();
			dir.Close();
		}
		
		// Make sure that a Directory implementation that does
		// not use LockFactory at all (ie overrides makeLock and
		// implements its own private locking) works OK.  This
		// was raised on java-dev as loss of backwards
		// compatibility.
		[Test]
		public virtual void  TestNullLockFactory()
		{
			
			
			Directory dir = new MyRAMDirectory(this);
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}
			writer.Close();
			Term searchTerm = new Term("content", "aaa");
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(new TermQuery(searchTerm));
			Assert.AreEqual(100, hits.Length(), "did not get right number of hits");
			writer.Close();
			
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.Close();
			
			dir.Close();
		}
		
		[Test]
		public virtual void  TestFlushWithNoMerging()
		{
			Directory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetMaxBufferedDocs(2);
			Document doc = new Document();
			doc.Add(new Field("field", "aaa", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
			for (int i = 0; i < 19; i++)
				writer.AddDocument(doc);
			writer.Flush(false, true);
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			// Since we flushed w/o allowing merging we should now
			// have 10 segments
			System.Diagnostics.Debug.Assert(sis.Count == 10);
		}
		
		// Make sure we can flush segment w/ norms, then add
		// empty doc (no norms) and flush
		[Test]
		public virtual void  TestEmptyDocAfterFlushingRealDoc()
		{
			Directory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			Document doc = new Document();
			doc.Add(new Field("field", "aaa", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
			writer.AddDocument(doc);
			writer.Flush();
			writer.AddDocument(new Document());
			writer.Close();
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(2, reader.NumDocs());
		}
		
		// Test calling optimize(false) whereby optimize is kicked
		// off but we don't wait for it to finish (but
		// writer.close()) does wait
		[Test]
		public virtual void  TestBackgroundOptimize()
		{
			
			Directory dir = new MockRAMDirectory();
			for (int pass = 0; pass < 2; pass++)
			{
				IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
				writer.SetMergeScheduler(new ConcurrentMergeScheduler());
				Document doc = new Document();
				doc.Add(new Field("field", "aaa", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
				writer.SetMaxBufferedDocs(2);
				writer.SetMergeFactor(101);
				for (int i = 0; i < 200; i++)
					writer.AddDocument(doc);
				writer.Optimize(false);
				
				if (0 == pass)
				{
					writer.Close();
					IndexReader reader = IndexReader.Open(dir);
					Assert.IsTrue(reader.IsOptimized());
					reader.Close();
				}
				else
				{
					// Get another segment to flush so we can verify it is
					// NOT included in the optimization
					writer.AddDocument(doc);
					writer.AddDocument(doc);
					writer.Close();
					
					IndexReader reader = IndexReader.Open(dir);
					Assert.IsTrue(!reader.IsOptimized());
					reader.Close();
					
					SegmentInfos infos = new SegmentInfos();
					infos.Read(dir);
					Assert.AreEqual(2, infos.Count);
				}
			}
			
			dir.Close();
		}
		
		private void  RmDir(System.IO.FileInfo dir)
		{
			String[] fullpathnames = System.IO.Directory.GetFileSystemEntries(dir.FullName);
			System.IO.FileInfo[] files = new System.IO.FileInfo[fullpathnames.Length];
			for (int i = 0; i < files.Length; i++)
				files[i] = new System.IO.FileInfo(fullpathnames[i]);

			if (files != null)
			{
				for (int i = 0; i < files.Length; i++)
				{
					bool tmpBool;
					if (System.IO.File.Exists(files[i].FullName))
					{
						System.IO.File.Delete(files[i].FullName);
						tmpBool = true;
					}
					else if (System.IO.Directory.Exists(files[i].FullName))
					{
						System.IO.Directory.Delete(files[i].FullName);
						tmpBool = true;
					}
					else
						tmpBool = false;
					bool generatedAux = tmpBool;
				}
			}
			bool tmpBool2;
			if (System.IO.File.Exists(dir.FullName))
			{
				System.IO.File.Delete(dir.FullName);
				tmpBool2 = true;
			}
			else if (System.IO.Directory.Exists(dir.FullName))
			{
				System.IO.Directory.Delete(dir.FullName);
				tmpBool2 = true;
			}
			else
				tmpBool2 = false;
			bool generatedAux2 = tmpBool2;
		}
		
		/// <summary> Test that no NullPointerException will be raised,
		/// when adding one document with a single, empty field
		/// and term vectors enabled.
		/// </summary>
		/// <throws>  IOException </throws>
		/// <summary> 
		/// </summary>
		[Test]
		public virtual void  TestBadSegment()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			IndexWriter ir = new IndexWriter(dir, new StandardAnalyzer(), true);
			
			Document document = new Document();
			document.Add(new Field("tvtest", "", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.YES));
			ir.AddDocument(document);
			ir.Close();
			dir.Close();
		}
		
		// LUCENE-1008
		[Test]
		public virtual void  TestNoTermVectorAfterTermVector()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			IndexWriter iw = new IndexWriter(dir, new StandardAnalyzer(), true);
			Document document = new Document();
			document.Add(new Field("tvtest", "a b c", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.YES));
			iw.AddDocument(document);
			document = new Document();
			document.Add(new Field("tvtest", "x y z", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.NO));
			iw.AddDocument(document);
			// Make first segment
			iw.Flush();
			
			document.Add(new Field("tvtest", "a b c", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.YES));
			iw.AddDocument(document);
			// Make 2nd segment
			iw.Flush();
			
			iw.Optimize();
			iw.Close();
			dir.Close();
		}
		
		// LUCENE-1010
		[Test]
		public virtual void  TestNoTermVectorAfterTermVectorMerge()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			IndexWriter iw = new IndexWriter(dir, new StandardAnalyzer(), true);
			Document document = new Document();
			document.Add(new Field("tvtest", "a b c", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.YES));
			iw.AddDocument(document);
			iw.Flush();
			
			document = new Document();
			document.Add(new Field("tvtest", "x y z", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.NO));
			iw.AddDocument(document);
			// Make first segment
			iw.Flush();
			
			iw.Optimize();
			
			document.Add(new Field("tvtest", "a b c", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.YES));
			iw.AddDocument(document);
			// Make 2nd segment
			iw.Flush();
			iw.Optimize();
			
			iw.Close();
			dir.Close();
		}
		
		// LUCENE-1036
		[Test]
		public virtual void  TestMaxThreadPriority()
		{
			int pri = (System.Int32) SupportClass.ThreadClass.Current().Priority;
			try
			{
				MockRAMDirectory dir = new MockRAMDirectory();
				IndexWriter iw = new IndexWriter(dir, new StandardAnalyzer(), true);
				Document document = new Document();
				document.Add(new Field("tvtest", "a b c", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.YES));
				iw.SetMaxBufferedDocs(2);
				iw.SetMergeFactor(2);
				SupportClass.ThreadClass.Current().Priority = (System.Threading.ThreadPriority) System.Threading.ThreadPriority.Highest;
				for (int i = 0; i < 4; i++)
					iw.AddDocument(document);
				iw.Close();
			}
			finally
			{
				SupportClass.ThreadClass.Current().Priority = (System.Threading.ThreadPriority) pri;
			}
		}
		
		// Just intercepts all merges & verifies that we are never
		// merging a segment with >= 20 (maxMergeDocs) docs
		private class MyMergeScheduler : MergeScheduler
		{
			public MyMergeScheduler(TestIndexWriter enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestIndexWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override void  Merge(IndexWriter writer)
			{
				lock (this)
				{
					
					while (true)
					{
						MergePolicy.OneMerge merge = writer.GetNextMerge();
						if (merge == null)
							break;
						for (int i = 0; i < merge.Segments_ForNUnitTest.Count; i++)
							System.Diagnostics.Debug.Assert(merge.Segments_ForNUnitTest.Info(i).docCount < 20);
						writer.Merge(merge);
					}
				}
			}
			
			public override void  Close()
			{
			}
		}
		
		// LUCENE-1013
		[Test]
		public virtual void  TestSetMaxMergeDocs()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			IndexWriter iw = new IndexWriter(dir, new StandardAnalyzer(), true);
			iw.SetMergeScheduler(new MyMergeScheduler(this));
			iw.SetMaxMergeDocs(20);
			iw.SetMaxBufferedDocs(2);
			iw.SetMergeFactor(2);
			Document document = new Document();
			document.Add(new Field("tvtest", "a b c", Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.YES));
			for (int i = 0; i < 177; i++)
				iw.AddDocument(document);
			iw.Close();
		}
		
		// LUCENE-1072
		[Test]
		public virtual void  TestExceptionFromTokenStream()
		{
			RAMDirectory dir = new MockRAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new AnonymousClassAnalyzer(this), true);
			
			Document doc = new Document();
			System.String contents = "aa bb cc dd ee ff gg hh ii jj kk";
			doc.Add(new Field("content", contents, Field.Store.NO, Field.Index.TOKENIZED));
			try
			{
				writer.AddDocument(doc);
				Assert.Fail("did not hit expected exception");
			}
			catch (System.Exception)
			{
			}
			
			// Make sure we can add another normal document
			doc = new Document();
			doc.Add(new Field("content", "aa bb cc dd", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			// Make sure we can add another normal document
			doc = new Document();
			doc.Add(new Field("content", "aa bb cc dd", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			writer.Close();
			IndexReader reader = IndexReader.Open(dir);
			Term t = new Term("content", "aa");
			Assert.AreEqual(reader.DocFreq(t), 3);
			
			// Make sure the doc that hit the exception was marked
			// as deleted:
			TermDocs tdocs = reader.TermDocs(t);
			int count = 0;
			while (tdocs.Next())
			{
				count++;
			}
			Assert.AreEqual(2, count);
			
			Assert.AreEqual(reader.DocFreq(new Term("content", "gg")), 0);
			reader.Close();
			dir.Close();
		}
		
		private class FailOnlyOnFlush : MockRAMDirectory.Failure
		{
			new internal bool doFail = false;
			//internal int count;
			
			public override void  SetDoFail()
			{
				this.doFail = true;
			}
			public override void ClearDoFail()
			{
				this.doFail = false;
			}
			
			public override void  Eval(MockRAMDirectory dir)
			{
				if (doFail)
				{
					// {{DOUG-2.3.1}} this code is suspect.  i have preserved the original (below) for 
					// comparative purposes.
					System.Exception e = new System.Exception();
					if (e.ToString().Contains("Lucene.Net.Index.DocumentsWriter") && e.StackTrace.Contains("appendPostings"))
					{
						doFail = false;
						throw new System.IO.IOException("now failing during flush");
					}
					//StackTraceElement[] trace = new System.Exception().getStackTrace();
					//for (int i = 0; i < trace.Length; i++)
					//{
					//    if ("Lucene.Net.Index.DocumentsWriter".Equals(trace[i].getClassName()) && "appendPostings".Equals(trace[i].getMethodName()) && count++ == 30)
					//    {
					//        doFail = false;
					//        throw new System.IO.IOException("now failing during flush");
					//    }
					//}
				}
			}
		}
		
		// LUCENE-1072: make sure an errant exception on flushing
		// one segment only takes out those docs in that one flush
		[Test]
		public virtual void  TestDocumentsWriterAbort()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			FailOnlyOnFlush failure = new FailOnlyOnFlush();
			failure.SetDoFail();
			dir.FailOn(failure);
			
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			writer.SetMaxBufferedDocs(2);
			Document doc = new Document();
			System.String contents = "aa bb cc dd ee ff gg hh ii jj kk";
			doc.Add(new Field("content", contents, Field.Store.NO, Field.Index.TOKENIZED));
			bool hitError = false;
			for (int i = 0; i < 200; i++)
			{
				try
				{
					writer.AddDocument(doc);
				}
				catch (System.IO.IOException)
				{
					// only one flush should fail:
					Assert.IsFalse(hitError);
					hitError = true;
				}
			}
			Assert.IsTrue(hitError);
			writer.Close();
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(198, reader.DocFreq(new Term("content", "aa")));
			reader.Close();
		}
		
		private class CrashingFilter : TokenFilter
		{
			private void  InitBlock(TestIndexWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal System.String fieldName;
			internal int count;
			
			public CrashingFilter(TestIndexWriter enclosingInstance, System.String fieldName, TokenStream input):base(input)
			{
				InitBlock(enclosingInstance);
				this.fieldName = fieldName;
			}

			public override Token Next(Token result)
			{
				if (this.fieldName.Equals("crash") && count++ >= 4)
					throw new System.IO.IOException("I'm experiencing problems");
				return input.Next(result);
			}

			public override void Reset()
			{
				base.Reset();
				count = 0;
			}
		}
		
		[Test]
		public virtual void  TestDocumentsWriterExceptions()
		{
			Analyzer analyzer = new AnonymousClassAnalyzer1(this);
			
			for (int i = 0; i < 2; i++)
			{
				MockRAMDirectory dir = new MockRAMDirectory();
				IndexWriter writer = new IndexWriter(dir, analyzer);
				//writer.setInfoStream(System.out);
				Document doc = new Document();
				doc.Add(new Field("contents", "here are some contents", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
				writer.AddDocument(doc);
				writer.AddDocument(doc);
				doc.Add(new Field("crash", "this should crash after 4 terms", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
				doc.Add(new Field("other", "this will not get indexed", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
				try
				{
					writer.AddDocument(doc);
					Assert.Fail("did not hit expected exception");
				}
				catch (System.IO.IOException)
				{
				}
				
				if (0 == i)
				{
					doc = new Document();
					doc.Add(new Field("contents", "here are some contents", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
					writer.AddDocument(doc);
					writer.AddDocument(doc);
				}
				writer.Close();
				
				IndexReader reader = IndexReader.Open(dir);
				int expected = 3 + (1 - i) * 2;
				Assert.AreEqual(expected, reader.DocFreq(new Term("contents", "here")));
				Assert.AreEqual(expected, reader.MaxDoc());
				int numDel = 0;
				for (int j = 0; j < reader.MaxDoc(); j++)
				{
					if (reader.IsDeleted(j))
						numDel++;
					else
						reader.Document(j);
					reader.GetTermFreqVectors(j);
				}
				reader.Close();
				
				Assert.AreEqual(1, numDel);
				
				writer = new IndexWriter(dir, analyzer);
				writer.SetMaxBufferedDocs(10);
				doc = new Document();
				doc.Add(new Field("contents", "here are some contents", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
				for (int j = 0; j < 17; j++)
					writer.AddDocument(doc);
				writer.Optimize();
				writer.Close();
				
				reader = IndexReader.Open(dir);
				expected = 19 + (1 - i) * 2;
				Assert.AreEqual(expected, reader.DocFreq(new Term("contents", "here")));
				Assert.AreEqual(expected, reader.MaxDoc());
				numDel = 0;
				for (int j = 0; j < reader.MaxDoc(); j++)
				{
					if (reader.IsDeleted(j))
						numDel++;
					else
						reader.Document(j);
					reader.GetTermFreqVectors(j);
				}
				reader.Close();
				Assert.AreEqual(0, numDel);
				
				dir.Close();
			}
		}
		
		[Test]
		public virtual void  TestDocumentsWriterExceptionThreads()
		{
			Analyzer analyzer = new AnonymousClassAnalyzer2(this);
			
			int NUM_THREAD = 3;
			int NUM_ITER = 100;
			
			for (int i = 0; i < 2; i++)
			{
				MockRAMDirectory dir = new MockRAMDirectory();
				{
					IndexWriter writer = new IndexWriter(dir, analyzer);
					
					int finalI = i;
					
					SupportClass.ThreadClass[] threads = new SupportClass.ThreadClass[NUM_THREAD];
					for (int t = 0; t < NUM_THREAD; t++)
					{
						threads[t] = new AnonymousClassThread(NUM_ITER, writer, finalI, this);
						threads[t].Start();
					}
					
					for (int t = 0; t < NUM_THREAD; t++)
						while (true)
							try
							{
								threads[t].Join();
								break;
							}
							catch (System.Threading.ThreadInterruptedException)
							{
								SupportClass.ThreadClass.Current().Interrupt();
							}
					
					writer.Close();
				}
				
				IndexReader reader = IndexReader.Open(dir);
				int expected = (3 + (1 - i) * 2) * NUM_THREAD * NUM_ITER;
				Assert.AreEqual(expected, reader.DocFreq(new Term("contents", "here")));
				Assert.AreEqual(expected, reader.MaxDoc());
				int numDel = 0;
				for (int j = 0; j < reader.MaxDoc(); j++)
				{
					if (reader.IsDeleted(j))
						numDel++;
					else
						reader.Document(j);
					reader.GetTermFreqVectors(j);
				}
				reader.Close();
				
				Assert.AreEqual(NUM_THREAD * NUM_ITER, numDel);
				
				IndexWriter writer2 = new IndexWriter(dir, analyzer);
				writer2.SetMaxBufferedDocs(10);
				Document doc = new Document();
				doc.Add(new Field("contents", "here are some contents", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
				for (int j = 0; j < 17; j++)
					writer2.AddDocument(doc);
				writer2.Optimize();
				writer2.Close();
				
				reader = IndexReader.Open(dir);
				expected += 17 - NUM_THREAD * NUM_ITER;
				Assert.AreEqual(expected, reader.DocFreq(new Term("contents", "here")));
				Assert.AreEqual(expected, reader.MaxDoc());
				numDel = 0;
				for (int j = 0; j < reader.MaxDoc(); j++)
				{
					if (reader.IsDeleted(j))
						numDel++;
					else
						reader.Document(j);
					reader.GetTermFreqVectors(j);
				}
				reader.Close();
				Assert.AreEqual(0, numDel);
				
				dir.Close();
			}
		}
		
		[Test]
		public virtual void  TestVariableSchema()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			int delID = 0;
			for (int i = 0; i < 20; i++)
			{
				IndexWriter writer = new IndexWriter(dir, false, new WhitespaceAnalyzer());
				writer.SetMaxBufferedDocs(2);
				writer.SetMergeFactor(2);
				writer.SetUseCompoundFile(false);
				Document doc = new Document();
				System.String contents = "aa bb cc dd ee ff gg hh ii jj kk";
				
				if (i == 7)
				{
					// Add empty docs here
					doc.Add(new Field("content3", "", Field.Store.NO, Field.Index.TOKENIZED));
				}
				else
				{
					Field.Store storeVal;
					if (i % 2 == 0)
					{
						doc.Add(new Field("content4", contents, Field.Store.YES, Field.Index.TOKENIZED));
						storeVal = Field.Store.YES;
					}
					else
						storeVal = Field.Store.NO;
					doc.Add(new Field("content1", contents, storeVal, Field.Index.TOKENIZED));
					doc.Add(new Field("content3", "", Field.Store.YES, Field.Index.TOKENIZED));
					doc.Add(new Field("content5", "", storeVal, Field.Index.TOKENIZED));
				}
				
				for (int j = 0; j < 4; j++)
					writer.AddDocument(doc);
				
				writer.Close();
				IndexReader reader = IndexReader.Open(dir);
				reader.DeleteDocument(delID++);
				reader.Close();
				
				if (0 == i % 4)
				{
					writer = new IndexWriter(dir, false, new WhitespaceAnalyzer());
					writer.SetUseCompoundFile(false);
					writer.Optimize();
					writer.Close();
				}
			}
		}
		
		//[Test]
		//public virtual void  TestNoWaitClose()
		//{
		//    RAMDirectory directory = new MockRAMDirectory();
			
		//    Document doc = new Document();
		//    Field idField = new Field("id", "", Field.Store.YES, Field.Index.UN_TOKENIZED);
		//    doc.Add(idField);
			
		//    for (int pass = 0; pass < 3; pass++)
		//    {
		//        bool autoCommit = pass % 2 == 0;
		//        IndexWriter writer = new IndexWriter(directory, autoCommit, new WhitespaceAnalyzer(), true);
				
		//        //System.out.println("TEST: pass=" + pass + " ac=" + autoCommit + " cms=" + (pass >= 2));
		//        for (int iter = 0; iter < 10; iter++)
		//        {
		//            //System.out.println("TEST: iter=" + iter);
		//            MergeScheduler ms;
		//            if (pass >= 2)
		//                ms = new ConcurrentMergeScheduler();
		//            else
		//                ms = new SerialMergeScheduler();
					
		//            writer.SetMergeScheduler(ms);
		//            writer.SetMaxBufferedDocs(2);
		//            writer.SetMergeFactor(100);
					
		//            for (int j = 0; j < 199; j++)
		//            {
		//                idField.SetValue(System.Convert.ToString(iter * 201 + j));
		//                writer.AddDocument(doc);
		//            }
					
		//            int delID = iter * 199;
		//            for (int j = 0; j < 20; j++)
		//            {
		//                writer.DeleteDocuments(new Term("id", System.Convert.ToString(delID)));
		//                delID += 5;
		//            }
					
		//            // Force a bunch of merge threads to kick off so we
		//            // stress out aborting them on close:
		//            writer.SetMergeFactor(2);
					
		//            IndexWriter finalWriter = writer;
		//            System.Collections.ArrayList failure = new System.Collections.ArrayList();
		//            SupportClass.ThreadClass t1 = new AnonymousClassThread1(finalWriter, doc, failure, this);
					
		//            if (failure.Count > 0)
		//            {
		//                throw (System.Exception) failure[0];
		//            }
					
		//            t1.Start();
					
		//            writer.Close(false);
		//            while (true)
		//            {
		//                try
		//                {
		//                    t1.Join();
		//                    break;
		//                }
		//                catch (System.Threading.ThreadInterruptedException)
		//                {
		//                    SupportClass.ThreadClass.Current().Interrupt();
		//                }
		//            }
					
		//            // Make sure reader can read
		//            IndexReader reader = IndexReader.Open(directory);
		//            reader.Close();
					
		//            // Reopen
		//            writer = new IndexWriter(directory, autoCommit, new WhitespaceAnalyzer(), false);
		//        }
		//        writer.Close();
		//    }
			
		//    directory.Close();
		//}
		
		// Used by test cases below
		private class IndexerThread : SupportClass.ThreadClass
		{
			private void  InitBlock(TestIndexWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			internal bool diskFull;
			internal System.Exception error;
			//internal AlreadyClosedException ace;
			internal IndexWriter writer;
			internal bool noErrors;
			
			public IndexerThread(TestIndexWriter enclosingInstance, IndexWriter writer, bool noErrors)
			{
				InitBlock(enclosingInstance);
				this.writer = writer;
				this.noErrors = noErrors;
			}
			
			override public void  Run()
			{
				
				Document doc = new Document();
				doc.Add(new Field("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
				
				int idUpto = 0;
				int fullCount = 0;
				long stopTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000 + 500;
				
				while ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 < stopTime)
				{
					try
					{
						writer.UpdateDocument(new Term("id", "" + (idUpto++)), doc);
					}
					catch (System.IO.IOException ioe)
					{
						if (ioe.Message.StartsWith("fake disk full at") || ioe.Message.Equals("now failing on purpose"))
						{
							diskFull = true;
							try
							{
								System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 1));
							}
							catch (System.Threading.ThreadInterruptedException)
							{
								SupportClass.ThreadClass.Current().Interrupt();
							}
							if (fullCount++ >= 5)
								break;
						}
						else
						{
							if (noErrors)
							{
								System.Console.Out.WriteLine(SupportClass.ThreadClass.Current().Name + ": ERROR: unexpected IOException:");
								System.Console.Out.WriteLine(ioe.StackTrace);
								error = ioe;
							}
							break;
						}
					}
					catch (System.Exception t)
					{
						if (noErrors)
						{
							System.Console.Out.WriteLine(SupportClass.ThreadClass.Current().Name + ": ERROR: unexpected Throwable:");
							System.Console.Out.WriteLine(t.StackTrace);
							error = t;
						}
						break;
					}
				}
			}
		}
		
		// LUCENE-1130: make sure we can close() even while
		// threads are trying to add documents.  Strictly
		// speaking, this isn't valid us of Lucene's APIs, but we
		// still want to be robust to this case:
		[Test]
		public virtual void  TestCloseWithThreads()
		{
			int NUM_THREADS = 3;
			
			for (int iter = 0; iter < 50; iter++)
			{
				MockRAMDirectory dir = new MockRAMDirectory();
				IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer());
				ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
				
				writer.SetMergeScheduler(cms);
				writer.SetMaxBufferedDocs(10);
				writer.SetMergeFactor(4);
				
				IndexerThread[] threads = new IndexerThread[NUM_THREADS];
				//bool diskFull = false;
				
				for (int i = 0; i < NUM_THREADS; i++)
					threads[i] = new IndexerThread(this, writer, false);
				
				for (int i = 0; i < NUM_THREADS; i++)
					threads[i].Start();
				
				try
				{
					System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 50));
				}
				catch (System.Threading.ThreadInterruptedException)
				{
					SupportClass.ThreadClass.Current().Interrupt();
				}
				
				writer.Close(false);
				
				// Make sure threads that are adding docs are not hung:
				for (int i = 0; i < NUM_THREADS; i++)
				{
					while (true)
					{
						try
						{
							// Without fix for LUCENE-1130: one of the
							// threads will hang
							threads[i].Join();
							break;
						}
						catch (System.Threading.ThreadInterruptedException)
						{
							SupportClass.ThreadClass.Current().Interrupt();
						}
					}
					if (threads[i].IsAlive)
						Assert.Fail("thread seems to be hung");
				}
				
				// Quick test to make sure index is not corrupt:
				IndexReader reader = IndexReader.Open(dir);
				TermDocs tdocs = reader.TermDocs(new Term("field", "aaa"));
				int count = 0;
				while (tdocs.Next())
				{
					count++;
				}
				Assert.IsTrue(count > 0);
				reader.Close();
				
				dir.Close();
			}
		}
		
		// LUCENE-1130: make sure immeidate disk full on creating
		// an IndexWriter (hit during DW.ThreadState.init()) is
		// OK:
		[Test]
		public virtual void  TestImmediateDiskFull()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			dir.SetMaxSizeInBytes(dir.GetRecomputedActualSizeInBytes());
			writer.SetMaxBufferedDocs(2);
			Document doc = new Document();
			doc.Add(new Field("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
			try
			{
				writer.AddDocument(doc);
				Assert.Fail("did not hit disk full");
			}
			catch (System.IO.IOException)
			{
			}
			// Without fix for LUCENE-1130: this call will hang:
			try
			{
				writer.AddDocument(doc);
				Assert.Fail("did not hit disk full");
			}
			catch (System.IO.IOException)
			{
			}
			try
			{
				writer.Close(false);
				Assert.Fail("did not hit disk full");
			}
			catch (System.IO.IOException)
			{
			}
		}
		
		// LUCENE-1130: make sure immeidate disk full on creating
		// an IndexWriter (hit during DW.ThreadState.init()), with
		// multiple threads, is OK:
		[Test]
		public virtual void  TestImmediateDiskFullWithThreads()
		{
			
			int NUM_THREADS = 3;
			
			for (int iter = 0; iter < 10; iter++)
			{
				MockRAMDirectory dir = new MockRAMDirectory();
				IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer());
				ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
				// We expect disk full exceptions in the merge threads
				cms.SetSuppressExceptions_ForNUnitTest();
				writer.SetMergeScheduler(cms);
				writer.SetMaxBufferedDocs(2);
				writer.SetMergeFactor(4);
				dir.SetMaxSizeInBytes(4 * 1024 + 20 * iter);
				
				IndexerThread[] threads = new IndexerThread[NUM_THREADS];
				//bool diskFull = false;
				
				for (int i = 0; i < NUM_THREADS; i++)
					threads[i] = new IndexerThread(this, writer, true);
				
				for (int i = 0; i < NUM_THREADS; i++)
					threads[i].Start();
				
				for (int i = 0; i < NUM_THREADS; i++)
				{
					while (true)
					{
						try
						{
							// Without fix for LUCENE-1130: one of the
							// threads will hang
							threads[i].Join();
							break;
						}
						catch (System.Threading.ThreadInterruptedException)
						{
							SupportClass.ThreadClass.Current().Interrupt();
						}
					}
					if (threads[i].IsAlive)
						Assert.Fail("thread seems to be hung");
					else
						Assert.IsTrue(threads[i].error == null, "hit unexpected Throwable");
				}
				
				try
				{
					writer.Close(false);
				}
				catch (System.IO.IOException)
				{
				}
				
				dir.Close();
			}
		}
		
		// Throws IOException during FieldsWriter.flushDocument and during DocumentsWriter.abort
		private class FailOnlyOnAbortOrFlush : MockRAMDirectory.Failure
		{
			private bool onlyOnce;
			public FailOnlyOnAbortOrFlush(bool onlyOnce)
			{
				this.onlyOnce = true;
			}
			public override void Eval(MockRAMDirectory dir)
			{
				if (doFail)
				{
					// {{DOUG-2.3.1}} this code is suspect.  i have preserved the original (below) for 
					// comparative purposes.
					System.String trace = new System.Exception().StackTrace;
					if (trace.Contains("abort") || trace.Contains("flushDocument"))
					{
						if (onlyOnce)
							doFail = false;
						throw new System.IO.IOException("now failing on purpose");
					}  
					//StackTraceElement[] trace = new System.Exception().getStackTrace();
					//for (int i = 0; i < trace.Length; i++)
					//{
					//    if ("abort".Equals(trace[i].getMethodName()) || "flushDocument".Equals(trace[i].getMethodName()))
					//    {
					//        if (onlyOnce)
					//            doFail = false;
					//        throw new System.IO.IOException("now failing on purpose");
					//    }
					//}
				}
			}
		}
		
		// Runs test, with one thread, using the specific failure
		// to trigger an IOException
		public virtual void  _testSingleThreadFailure(MockRAMDirectory.Failure failure)
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			writer.SetMaxBufferedDocs(2);
			Document doc = new Document();
			doc.Add(new Field("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
			
			for (int i = 0; i < 6; i++)
				writer.AddDocument(doc);
			
			dir.FailOn(failure);
			failure.SetDoFail();
			try
			{
				writer.AddDocument(doc);
				writer.AddDocument(doc);
				Assert.Fail("did not hit exception");
			}
			catch (System.IO.IOException)
			{
			}
			failure.ClearDoFail();
			writer.AddDocument(doc);
			writer.Close(false);
		}
		
		// Runs test, with multiple threads, using the specific
		// failure to trigger an IOException
		public virtual void  _testMultipleThreadsFailure(MockRAMDirectory.Failure failure)
		{
			
			int NUM_THREADS = 3;
			
			for (int iter = 0; iter < 5; iter++)
			{
				MockRAMDirectory dir = new MockRAMDirectory();
				IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer());
				ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
				// We expect disk full exceptions in the merge threads
				cms.SetSuppressExceptions_ForNUnitTest();
				writer.SetMergeScheduler(cms);
				writer.SetMaxBufferedDocs(2);
				writer.SetMergeFactor(4);
				
				IndexerThread[] threads = new IndexerThread[NUM_THREADS];
				//bool diskFull = false;
				
				for (int i = 0; i < NUM_THREADS; i++)
					threads[i] = new IndexerThread(this, writer, true);
				
				for (int i = 0; i < NUM_THREADS; i++)
					threads[i].Start();
				
				try
				{
					System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 10));
				}
				catch (System.Threading.ThreadInterruptedException)
				{
					SupportClass.ThreadClass.Current().Interrupt();
				}
				
				dir.FailOn(failure);
				failure.SetDoFail();
				
				for (int i = 0; i < NUM_THREADS; i++)
				{
					while (true)
					{
						try
						{
							threads[i].Join();
							break;
						}
						catch (System.Threading.ThreadInterruptedException)
						{
							SupportClass.ThreadClass.Current().Interrupt();
						}
					}
					if (threads[i].IsAlive)
						Assert.Fail("thread seems to be hung");
					else
						Assert.IsTrue(threads[i].error == null, "hit unexpected Throwable");
				}
				
				bool success = false;
				try
				{
					writer.Close(false);
					success = true;
				}
				catch (System.IO.IOException)
				{
				}
				
				if (success)
				{
					IndexReader reader = IndexReader.Open(dir);
					for (int j = 0; j < reader.MaxDoc(); j++)
					{
						if (!reader.IsDeleted(j))
						{
							reader.Document(j);
							reader.GetTermFreqVectors(j);
						}
					}
					reader.Close();
				}
				
				dir.Close();
			}
		}
		
		// LUCENE-1130: make sure initial IOException, and then 2nd
		// IOException during abort(), is OK:
		[Test]
		public virtual void  TestIOExceptionDuringAbort()
		{
			_testSingleThreadFailure(new FailOnlyOnAbortOrFlush(false));
		}
		
		// LUCENE-1130: make sure initial IOException, and then 2nd
		// IOException during abort(), is OK:
		[Test]
		public virtual void  TestIOExceptionDuringAbortOnlyOnce()
		{
			_testSingleThreadFailure(new FailOnlyOnAbortOrFlush(true));
		}
		
		// LUCENE-1130: make sure initial IOException, and then 2nd
		// IOException during abort(), with multiple threads, is OK:
		[Test]
		public virtual void  TestIOExceptionDuringAbortWithThreads()
		{
			_testMultipleThreadsFailure(new FailOnlyOnAbortOrFlush(false));
		}
		
		// LUCENE-1130: make sure initial IOException, and then 2nd
		// IOException during abort(), with multiple threads, is OK:
		[Test]
		public virtual void  TestIOExceptionDuringAbortWithThreadsOnlyOnce()
		{
			_testMultipleThreadsFailure(new FailOnlyOnAbortOrFlush(true));
		}
		
		// Throws IOException during DocumentsWriter.closeDocStore
		private class FailOnlyInCloseDocStore : MockRAMDirectory.Failure
		{
			private bool onlyOnce;
			public FailOnlyInCloseDocStore(bool onlyOnce)
			{
				this.onlyOnce = true;
			}
			public override void Eval(MockRAMDirectory dir)
			{
				if (doFail)
				{
					// {{DOUG-2.3.1}} this code is suspect.  i have preserved the original (below) for 
					// comparative purposes.
					if (new System.Exception().StackTrace.Contains("closeDocStore"))
					{
						if (onlyOnce)
							doFail = false;
						throw new System.IO.IOException("now failing on purpose");
					}
					//StackTraceElement[] trace = new System.Exception().getStackTrace();
					//for (int i = 0; i < trace.Length; i++)
					//{
					//    if ("closeDocStore".Equals(trace[i].getMethodName()))
					//    {
					//        if (onlyOnce)
					//            doFail = false;
					//        throw new System.IO.IOException("now failing on purpose");
					//    }
					//}
				}
			}
		}
		
		// LUCENE-1130: test IOException in closeDocStore
		[Test]
		public virtual void  TestIOExceptionDuringCloseDocStore()
		{
			_testSingleThreadFailure(new FailOnlyInCloseDocStore(false));
		}
		
		// LUCENE-1130: test IOException in closeDocStore
		[Test]
		public virtual void  TestIOExceptionDuringCloseDocStoreOnlyOnce()
		{
			_testSingleThreadFailure(new FailOnlyInCloseDocStore(true));
		}
		
		// LUCENE-1130: test IOException in closeDocStore, with threads
		[Test]
		public virtual void  TestIOExceptionDuringCloseDocStoreWithThreads()
		{
			_testMultipleThreadsFailure(new FailOnlyInCloseDocStore(false));
		}
		
		// LUCENE-1130: test IOException in closeDocStore, with threads
		[Test]
		public virtual void  TestIOExceptionDuringCloseDocStoreWithThreadsOnlyOnce()
		{
			_testMultipleThreadsFailure(new FailOnlyInCloseDocStore(true));
		}
		
		// Throws IOException during DocumentsWriter.writeSegment
		private class FailOnlyInWriteSegment : MockRAMDirectory.Failure
		{
			private bool onlyOnce;
			public FailOnlyInWriteSegment(bool onlyOnce)
			{
				this.onlyOnce = true;
			}
			public override void Eval(MockRAMDirectory dir)
			{
				if (doFail)
				{
					// {{DOUG-2.3.1}} this code is suspect.  i have preserved the original (below) for 
					// comparative purposes.
					if (new System.Exception().StackTrace.Contains("writeSegment"))
					{
						if (onlyOnce)
							doFail = false;
						// new RuntimeException().printStackTrace(System.out);
						throw new System.IO.IOException("now failing on purpose");
					}
					//StackTraceElement[] trace = new System.Exception().getStackTrace();
					//for (int i = 0; i < trace.Length; i++)
					//{
					//    if ("writeSegment".Equals(trace[i].getMethodName()))
					//    {
					//        if (onlyOnce)
					//            doFail = false;
					//        // new RuntimeException().printStackTrace(System.out);
					//        throw new System.IO.IOException("now failing on purpose");
					//    }
					//}
				}
			}
		}
		
		// LUCENE-1130: test IOException in writeSegment
		[Test]
		public virtual void  TestIOExceptionDuringWriteSegment()
		{
			_testSingleThreadFailure(new FailOnlyInWriteSegment(false));
		}
		
		// LUCENE-1130: test IOException in writeSegment
		[Test]
		public virtual void  TestIOExceptionDuringWriteSegmentOnlyOnce()
		{
			_testSingleThreadFailure(new FailOnlyInWriteSegment(true));
		}
		
		// LUCENE-1130: test IOException in writeSegment, with threads
		[Test]
		public virtual void  TestIOExceptionDuringWriteSegmentWithThreads()
		{
			_testMultipleThreadsFailure(new FailOnlyInWriteSegment(false));
		}
		
		// LUCENE-1130: test IOException in writeSegment, with threads
		[Test]
		public virtual void  TestIOExceptionDuringWriteSegmentWithThreadsOnlyOnce()
		{
			_testMultipleThreadsFailure(new FailOnlyInWriteSegment(true));
		}
		
		// LUCENE-1168
		[Test]
		public virtual void  TestTermVectorCorruption()
		{
			
			Directory dir = new MockRAMDirectory();
			for (int iter = 0; iter < 4; iter++)
			{
				bool autoCommit = 1 == iter / 2;
				IndexWriter writer = new IndexWriter(dir, autoCommit, new StandardAnalyzer());
				writer.SetMaxBufferedDocs(2);
				writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
				writer.SetMergeScheduler(new SerialMergeScheduler());
				writer.SetMergePolicy(new LogDocMergePolicy());
				
				Document document = new Document();
				
				Field storedField = new Field("stored", "stored", Field.Store.YES, Field.Index.NO);
				document.Add(storedField);
				writer.AddDocument(document);
				writer.AddDocument(document);
				
				document = new Document();
				document.Add(storedField);
				Field termVectorField = new Field("termVector", "termVector", Field.Store.NO, Field.Index.UN_TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS);
				
				document.Add(termVectorField);
				writer.AddDocument(document);
				writer.Optimize();
				writer.Close();
				
				IndexReader reader = IndexReader.Open(dir);
				for (int i = 0; i < reader.NumDocs(); i++)
				{
					reader.Document(i);
					reader.GetTermFreqVectors(i);
				}
				reader.Close();
				
				writer = new IndexWriter(dir, autoCommit, new StandardAnalyzer());
				writer.SetMaxBufferedDocs(2);
				writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
				writer.SetMergeScheduler(new SerialMergeScheduler());
				writer.SetMergePolicy(new LogDocMergePolicy());
				
				Directory[] indexDirs = new Directory[]{dir};
				writer.AddIndexes(indexDirs);
				writer.Close();
			}
			dir.Close();
		}
		
		// LUCENE-1168
		[Test]
		public virtual void  TestTermVectorCorruption2()
		{
			Directory dir = new MockRAMDirectory();
			for (int iter = 0; iter < 4; iter++)
			{
				bool autoCommit = 1 == iter / 2;
				IndexWriter writer = new IndexWriter(dir, autoCommit, new StandardAnalyzer());
				writer.SetMaxBufferedDocs(2);
				writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
				writer.SetMergeScheduler(new SerialMergeScheduler());
				writer.SetMergePolicy(new LogDocMergePolicy());
				
				Document document = new Document();
				
				Field storedField = new Field("stored", "stored", Field.Store.YES, Field.Index.NO);
				document.Add(storedField);
				writer.AddDocument(document);
				writer.AddDocument(document);
				
				document = new Document();
				document.Add(storedField);
				Field termVectorField = new Field("termVector", "termVector", Field.Store.NO, Field.Index.UN_TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS);
				document.Add(termVectorField);
				writer.AddDocument(document);
				writer.Optimize();
				writer.Close();
				
				IndexReader reader = IndexReader.Open(dir);
				Assert.IsTrue(reader.GetTermFreqVectors(0) == null);
				Assert.IsTrue(reader.GetTermFreqVectors(1) == null);
				Assert.IsTrue(reader.GetTermFreqVectors(2) != null);
				reader.Close();
			}
			dir.Close();
		}
		
		// LUCENE-1168
		[Test]
		public virtual void  TestTermVectorCorruption3()
		{
			Directory dir = new MockRAMDirectory();
			IndexWriter writer = new IndexWriter(dir, false, new StandardAnalyzer());
			writer.SetMaxBufferedDocs(2);
			writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
			writer.SetMergeScheduler(new SerialMergeScheduler());
			writer.SetMergePolicy(new LogDocMergePolicy());
			
			Document document = new Document();
			
			document = new Document();
			Field storedField = new Field("stored", "stored", Field.Store.YES, Field.Index.NO);
			document.Add(storedField);
			Field termVectorField = new Field("termVector", "termVector", Field.Store.NO, Field.Index.UN_TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS);
			document.Add(termVectorField);
			for (int i = 0; i < 10; i++)
				writer.AddDocument(document);
			writer.Close();
			
			writer = new IndexWriter(dir, false, new StandardAnalyzer());
			writer.SetMaxBufferedDocs(2);
			writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
			writer.SetMergeScheduler(new SerialMergeScheduler());
			writer.SetMergePolicy(new LogDocMergePolicy());
			for (int i = 0; i < 6; i++)
				writer.AddDocument(document);
			
			writer.Optimize();
			writer.Close();
			
			IndexReader reader = IndexReader.Open(dir);
			for (int i = 0; i < 10; i++)
			{
				reader.GetTermFreqVectors(i);
				reader.Document(i);
			}
			reader.Close();
			dir.Close();
		}
		
		// Just intercepts all merges & verifies that we are never
		// merging a segment with >= 20 (maxMergeDocs) docs
		private class MyIndexWriter : IndexWriter
		{
			private void  InitBlock(TestIndexWriter enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriter enclosingInstance;
			public TestIndexWriter Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal int mergeCount;
			internal Directory myDir;
			public MyIndexWriter(TestIndexWriter enclosingInstance, Directory dir):base(dir, new StandardAnalyzer())
			{
				InitBlock(enclosingInstance);
				myDir = dir;
			}
			public override MergePolicy.OneMerge GetNextMerge()
			{
				lock (this)
				{
					MergePolicy.OneMerge merge = base.GetNextMerge();
					if (merge != null)
						mergeCount++;
					return merge;
				}
			}
		}
		
		[Test]
		public virtual void  TestOptimizeOverMerge()
		{
			Directory dir = new MockRAMDirectory();
			IndexWriter writer = new IndexWriter(dir, false, new StandardAnalyzer());
			writer.SetMaxBufferedDocs(2);
			writer.SetMergeFactor(100);
			writer.SetRAMBufferSizeMB(IndexWriter.DISABLE_AUTO_FLUSH);
			
			Document document = new Document();
			
			document = new Document();
			Field storedField = new Field("stored", "stored", Field.Store.YES, Field.Index.NO);
			document.Add(storedField);
			Field termVectorField = new Field("termVector", "termVector", Field.Store.NO, Field.Index.UN_TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS);
			document.Add(termVectorField);
			for (int i = 0; i < 170; i++)
				writer.AddDocument(document);
			
			writer.Close();
			MyIndexWriter myWriter = new MyIndexWriter(this, dir);
			myWriter.Optimize();
			Assert.AreEqual(10, myWriter.mergeCount);
		}
		
		// LUCENE-1179
		[Test]
		public virtual void  TestEmptyFieldName()
		{
			MockRAMDirectory dir = new MockRAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			Document doc = new Document();
			doc.Add(new Field("", "a b c", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			writer.Close();
		}
	}
}