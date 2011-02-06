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
using Directory = Lucene.Net.Store.Directory;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using TermQuery = Lucene.Net.Search.TermQuery;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Index
{
	
	[TestFixture]
	public class TestIndexWriterDelete : LuceneTestCase
	{
		
		private class AnonymousClassFailure : MockRAMDirectory.Failure
		{
			public AnonymousClassFailure(TestIndexWriterDelete enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestIndexWriterDelete enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriterDelete enclosingInstance;
			public TestIndexWriterDelete Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal bool sawMaybe = false;
			internal bool failed = false;
			public override MockRAMDirectory.Failure Reset()
			{
				sawMaybe = false;
				failed = false;
				return this;
			}
			public override void  Eval(MockRAMDirectory dir)
			{
				if (sawMaybe && !failed)
				{
					bool seen = false;
					System.Diagnostics.StackFrame[] frames = new System.Diagnostics.StackTrace().GetFrames();
					for (int i = 0; i < frames.Length; i++)
					{
						System.String methodName = frames[i].GetMethod().Name;
						if ("ApplyDeletes".Equals(methodName))
						{
							seen = true;
							break;
						}
					}
					if (!seen)
					{
						// Only fail once we are no longer in applyDeletes
						failed = true;
						throw new System.IO.IOException("fail after applyDeletes");
					}
				}
				if (!failed)
				{
					System.Diagnostics.StackFrame[] frames = new System.Diagnostics.StackTrace().GetFrames();
					for (int i = 0; i < frames.Length; i++)
					{
						System.String methodName = frames[i].GetMethod().Name;
						if ("ApplyDeletes".Equals(methodName))
						{
							sawMaybe = true;
							break;
						}
					}
				}
			}
		}
		
		private class AnonymousClassFailure1 : MockRAMDirectory.Failure
		{
			public AnonymousClassFailure1(TestIndexWriterDelete enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestIndexWriterDelete enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestIndexWriterDelete enclosingInstance;
			public TestIndexWriterDelete Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal bool failed = false;
			public override MockRAMDirectory.Failure Reset()
			{
				failed = false;
				return this;
			}
			public override void  Eval(MockRAMDirectory dir)
			{
				if (!failed)
				{
					failed = true;
					throw new System.IO.IOException("fail in add doc");
				}
			}
		}
		
		// test the simple case
		[Test]
		public virtual void  TestSimpleCase()
		{
			System.String[] keywords = new System.String[]{"1", "2"};
			System.String[] unindexed = new System.String[]{"Netherlands", "Italy"};
			System.String[] unstored = new System.String[]{"Amsterdam has lots of bridges", "Venice has lots of canals"};
			System.String[] text = new System.String[]{"Amsterdam", "Venice"};
			
			for (int pass = 0; pass < 2; pass++)
			{
				bool autoCommit = (0 == pass);
				
				Directory dir = new RAMDirectory();
				IndexWriter modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
				modifier.SetUseCompoundFile(true);
				modifier.SetMaxBufferedDeleteTerms(1);
				
				for (int i = 0; i < keywords.Length; i++)
				{
					Document doc = new Document();
					doc.Add(new Field("id", keywords[i], Field.Store.YES, Field.Index.UN_TOKENIZED));
					doc.Add(new Field("country", unindexed[i], Field.Store.YES, Field.Index.NO));
					doc.Add(new Field("contents", unstored[i], Field.Store.NO, Field.Index.TOKENIZED));
					doc.Add(new Field("city", text[i], Field.Store.YES, Field.Index.TOKENIZED));
					modifier.AddDocument(doc);
				}
				modifier.Optimize();
				
				if (!autoCommit)
				{
					modifier.Close();
				}
				
				Term term = new Term("city", "Amsterdam");
				int hitCount = GetHitCount(dir, term);
				Assert.AreEqual(1, hitCount);
				if (!autoCommit)
				{
					modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer());
					modifier.SetUseCompoundFile(true);
				}
				modifier.DeleteDocuments(term);
				if (!autoCommit)
				{
					modifier.Close();
				}
				hitCount = GetHitCount(dir, term);
				Assert.AreEqual(0, hitCount);
				
				if (autoCommit)
				{
					modifier.Close();
				}
				dir.Close();
			}
		}
		
		// test when delete terms only apply to disk segments
		[Test]
		public virtual void  TestNonRAMDelete()
		{
			for (int pass = 0; pass < 2; pass++)
			{
				bool autoCommit = (0 == pass);
				
				Directory dir = new RAMDirectory();
				IndexWriter modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
				modifier.SetMaxBufferedDocs(2);
				modifier.SetMaxBufferedDeleteTerms(2);
				
				int id = 0;
				int value_Renamed = 100;
				
				for (int i = 0; i < 7; i++)
				{
					AddDoc(modifier, ++id, value_Renamed);
				}
				modifier.Flush();
				
				Assert.AreEqual(0, modifier.GetNumBufferedDocuments());
				Assert.IsTrue(0 < modifier.GetSegmentCount());
				
				if (!autoCommit)
				{
					modifier.Close();
				}
				
				IndexReader reader = IndexReader.Open(dir);
				Assert.AreEqual(7, reader.NumDocs());
				reader.Close();
				
				if (!autoCommit)
				{
					modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer());
					modifier.SetMaxBufferedDocs(2);
					modifier.SetMaxBufferedDeleteTerms(2);
				}
				
				modifier.DeleteDocuments(new Term("value", System.Convert.ToString(value_Renamed)));
				modifier.DeleteDocuments(new Term("value", System.Convert.ToString(value_Renamed)));
				
				if (!autoCommit)
				{
					modifier.Close();
				}
				
				reader = IndexReader.Open(dir);
				Assert.AreEqual(0, reader.NumDocs());
				reader.Close();
				if (autoCommit)
				{
					modifier.Close();
				}
				dir.Close();
			}
		}
		
		// test when delete terms only apply to ram segments
		[Test]
		public virtual void  TestRAMDeletes()
		{
			for (int pass = 0; pass < 2; pass++)
			{
				bool autoCommit = (0 == pass);
				Directory dir = new RAMDirectory();
				IndexWriter modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
				modifier.SetMaxBufferedDocs(4);
				modifier.SetMaxBufferedDeleteTerms(4);
				
				int id = 0;
				int value_Renamed = 100;
				
				AddDoc(modifier, ++id, value_Renamed);
				modifier.DeleteDocuments(new Term("value", System.Convert.ToString(value_Renamed)));
				AddDoc(modifier, ++id, value_Renamed);
				modifier.DeleteDocuments(new Term("value", System.Convert.ToString(value_Renamed)));
				
				Assert.AreEqual(2, modifier.GetNumBufferedDeleteTerms());
				Assert.AreEqual(1, modifier.GetBufferedDeleteTermsSize());
				
				AddDoc(modifier, ++id, value_Renamed);
				Assert.AreEqual(0, modifier.GetSegmentCount());
				modifier.Flush();
				
				if (!autoCommit)
				{
					modifier.Close();
				}
				
				IndexReader reader = IndexReader.Open(dir);
				Assert.AreEqual(1, reader.NumDocs());
				
				int hitCount = GetHitCount(dir, new Term("id", System.Convert.ToString(id)));
				Assert.AreEqual(1, hitCount);
				reader.Close();
				if (autoCommit)
				{
					modifier.Close();
				}
				dir.Close();
			}
		}
		
		// test when delete terms apply to both disk and ram segments
		[Test]
		public virtual void  TestBothDeletes()
		{
			for (int pass = 0; pass < 2; pass++)
			{
				bool autoCommit = (0 == pass);
				
				Directory dir = new RAMDirectory();
				IndexWriter modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
				modifier.SetMaxBufferedDocs(100);
				modifier.SetMaxBufferedDeleteTerms(100);
				
				int id = 0;
				int value_Renamed = 100;
				
				for (int i = 0; i < 5; i++)
				{
					AddDoc(modifier, ++id, value_Renamed);
				}
				
				value_Renamed = 200;
				for (int i = 0; i < 5; i++)
				{
					AddDoc(modifier, ++id, value_Renamed);
				}
				modifier.Flush();
				
				for (int i = 0; i < 5; i++)
				{
					AddDoc(modifier, ++id, value_Renamed);
				}
				modifier.DeleteDocuments(new Term("value", System.Convert.ToString(value_Renamed)));
				
				modifier.Flush();
				if (!autoCommit)
				{
					modifier.Close();
				}
				
				IndexReader reader = IndexReader.Open(dir);
				Assert.AreEqual(5, reader.NumDocs());
				if (autoCommit)
				{
					modifier.Close();
				}
			}
		}
		
		// test that batched delete terms are flushed together
		[Test]
		public virtual void  TestBatchDeletes()
		{
			for (int pass = 0; pass < 2; pass++)
			{
				bool autoCommit = (0 == pass);
				Directory dir = new RAMDirectory();
				IndexWriter modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
				modifier.SetMaxBufferedDocs(2);
				modifier.SetMaxBufferedDeleteTerms(2);
				
				int id = 0;
				int value_Renamed = 100;
				
				for (int i = 0; i < 7; i++)
				{
					AddDoc(modifier, ++id, value_Renamed);
				}
				modifier.Flush();
				if (!autoCommit)
				{
					modifier.Close();
				}
				
				IndexReader reader = IndexReader.Open(dir);
				Assert.AreEqual(7, reader.NumDocs());
				reader.Close();
				
				if (!autoCommit)
				{
					modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer());
					modifier.SetMaxBufferedDocs(2);
					modifier.SetMaxBufferedDeleteTerms(2);
				}
				
				id = 0;
				modifier.DeleteDocuments(new Term("id", System.Convert.ToString(++id)));
				modifier.DeleteDocuments(new Term("id", System.Convert.ToString(++id)));
				
				if (!autoCommit)
				{
					modifier.Close();
				}
				
				reader = IndexReader.Open(dir);
				Assert.AreEqual(5, reader.NumDocs());
				reader.Close();
				
				Term[] terms = new Term[3];
				for (int i = 0; i < terms.Length; i++)
				{
					terms[i] = new Term("id", System.Convert.ToString(++id));
				}
				if (!autoCommit)
				{
					modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer());
					modifier.SetMaxBufferedDocs(2);
					modifier.SetMaxBufferedDeleteTerms(2);
				}
				modifier.DeleteDocuments(terms);
				if (!autoCommit)
				{
					modifier.Close();
				}
				reader = IndexReader.Open(dir);
				Assert.AreEqual(2, reader.NumDocs());
				reader.Close();
				
				if (autoCommit)
				{
					modifier.Close();
				}
				dir.Close();
			}
		}
		
		private void  AddDoc(IndexWriter modifier, int id, int value_Renamed)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("content", "aaa", Field.Store.NO, Field.Index.TOKENIZED));
			doc.Add(new Field("id", System.Convert.ToString(id), Field.Store.YES, Field.Index.UN_TOKENIZED));
			doc.Add(new Field("value", System.Convert.ToString(value_Renamed), Field.Store.NO, Field.Index.UN_TOKENIZED));
			modifier.AddDocument(doc);
		}
		
		private int GetHitCount(Directory dir, Term term)
		{
			IndexSearcher searcher = new IndexSearcher(dir);
			int hitCount = searcher.Search(new TermQuery(term)).Length();
			searcher.Close();
			return hitCount;
		}
		
		[Test]
		public virtual void  TestDeletesOnDiskFull()
		{
			TestOperationsOnDiskFull(false);
		}
		
		[Test]
		public virtual void  TestUpdatesOnDiskFull()
		{
			TestOperationsOnDiskFull(true);
		}
		
		/// <summary> Make sure if modifier tries to commit but hits disk full that modifier
		/// remains consistent and usable. Similar to TestIndexReader.testDiskFull().
		/// </summary>
		private void  TestOperationsOnDiskFull(bool updates)
		{
			
			bool debug = false;
			Term searchTerm = new Term("content", "aaa");
			int START_COUNT = 157;
			int END_COUNT = 144;
			
			for (int pass = 0; pass < 2; pass++)
			{
				bool autoCommit = (0 == pass);
				
				// First build up a starting index:
				RAMDirectory startDir = new RAMDirectory();
				IndexWriter writer = new IndexWriter(startDir, autoCommit, new WhitespaceAnalyzer(), true);
				for (int i = 0; i < 157; i++)
				{
					Document d = new Document();
					d.Add(new Field("id", System.Convert.ToString(i), Field.Store.YES, Field.Index.UN_TOKENIZED));
					d.Add(new Field("content", "aaa " + i, Field.Store.NO, Field.Index.TOKENIZED));
					writer.AddDocument(d);
				}
				writer.Close();
				
				long diskUsage = startDir.SizeInBytes();
				long diskFree = diskUsage + 10;
				
				System.IO.IOException err = null;
				
				bool done = false;
				
				// Iterate w/ ever increasing free disk space:
				while (!done)
				{
					MockRAMDirectory dir = new MockRAMDirectory(startDir);
					IndexWriter modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer());
					
					modifier.SetMaxBufferedDocs(1000); // use flush or close
					modifier.SetMaxBufferedDeleteTerms(1000); // use flush or close
					
					// For each disk size, first try to commit against
					// dir that will hit random IOExceptions & disk
					// full; after, give it infinite disk space & turn
					// off random IOExceptions & retry w/ same reader:
					bool success = false;
					
					for (int x = 0; x < 2; x++)
					{
						
						double rate = 0.1;
						//UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
						double diskRatio = ((double) diskFree) / diskUsage;
						long thisDiskFree;
						System.String testName;
						
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
							{
								System.Console.Out.WriteLine("\ncycle: " + diskFree + " bytes");
							}
							testName = "disk full during reader.close() @ " + thisDiskFree + " bytes";
						}
						else
						{
							thisDiskFree = 0;
							rate = 0.0;
							if (debug)
							{
								System.Console.Out.WriteLine("\ncycle: same writer: unlimited disk space");
							}
							testName = "reader re-use after disk full";
						}
						
						dir.SetMaxSizeInBytes(thisDiskFree);
						dir.SetRandomIOExceptionRate(rate, diskFree);
						
						try
						{
							if (0 == x)
							{
								int docId = 12;
								for (int i = 0; i < 13; i++)
								{
									if (updates)
									{
										Document d = new Document();
										d.Add(new Field("id", System.Convert.ToString(i), Field.Store.YES, Field.Index.UN_TOKENIZED));
										d.Add(new Field("content", "bbb " + i, Field.Store.NO, Field.Index.TOKENIZED));
										modifier.UpdateDocument(new Term("id", System.Convert.ToString(docId)), d);
									}
									else
									{
										// deletes
										modifier.DeleteDocuments(new Term("id", System.Convert.ToString(docId)));
										// modifier.setNorm(docId, "contents", (float)2.0);
									}
									docId += 12;
								}
							}
							modifier.Close();
							success = true;
							if (0 == x)
							{
								done = true;
							}
						}
						catch (System.IO.IOException e)
						{
							if (debug)
							{
								//UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.toString' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
								System.Console.Out.WriteLine("  hit IOException: " + e);
								System.Console.Out.WriteLine(e.StackTrace);
							}
							err = e;
							if (1 == x)
							{
								System.Console.Error.WriteLine(e.StackTrace);
								Assert.Fail(testName + " hit IOException after disk space was freed up");
							}
						}
						
						// Whether we succeeded or failed, check that all
						// un-referenced files were in fact deleted (ie,
						// we did not create garbage). Just create a
						// new IndexFileDeleter, have it delete
						// unreferenced files, then verify that in fact
						// no files were deleted:
						System.String[] startFiles = dir.List();
						SegmentInfos infos = new SegmentInfos();
						infos.Read(dir);
						new IndexFileDeleter(dir, new KeepOnlyLastCommitDeletionPolicy(), infos, null, null);
						System.String[] endFiles = dir.List();
						
						//UPGRADE_TODO: Method 'java.util.Arrays.sort' was converted to 'System.Array.Sort' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilArrayssort_javalangObject[]'"
						System.Array.Sort(startFiles);
						//UPGRADE_TODO: Method 'java.util.Arrays.sort' was converted to 'System.Array.Sort' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilArrayssort_javalangObject[]'"
						System.Array.Sort(endFiles);
						
						// for(int i=0;i<startFiles.length;i++) {
						// System.out.println(" startFiles: " + i + ": " + startFiles[i]);
						// }
						
						if (!SupportClass.Compare.CompareStringArrays(startFiles, endFiles))
						{
							System.String successStr;
							if (success)
							{
								successStr = "success";
							}
							else
							{
								successStr = "IOException";
								System.Console.Error.WriteLine(err.StackTrace);
							}
							Assert.Fail("reader.close() failed to delete unreferenced files after " + successStr + " (" + diskFree + " bytes): before delete:\n    " + ArrayToString(startFiles) + "\n  after delete:\n    " + ArrayToString(endFiles));
						}
						
						// Finally, verify index is not corrupt, and, if
						// we succeeded, we see all docs changed, and if
						// we failed, we see either all docs or no docs
						// changed (transactional semantics):
						IndexReader newReader = null;
						try
						{
							newReader = IndexReader.Open(dir);
						}
						catch (System.IO.IOException e)
						{
							System.Console.Error.WriteLine(e.StackTrace);
							//UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.toString' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
							Assert.Fail(testName + ":exception when creating IndexReader after disk full during close: " + e);
						}
						
						IndexSearcher searcher = new IndexSearcher(newReader);
						Hits hits = null;
						try
						{
							hits = searcher.Search(new TermQuery(searchTerm));
						}
						catch (System.IO.IOException e)
						{
							System.Console.Error.WriteLine(e.StackTrace);
							//UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.toString' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
							Assert.Fail(testName + ": exception when searching: " + e);
						}
						int result2 = hits.Length();
						if (success)
						{
							if (x == 0 && result2 != END_COUNT)
							{
								Assert.Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + END_COUNT);
							}
							else if (x == 1 && result2 != START_COUNT && result2 != END_COUNT)
							{
								// It's possible that the first exception was
								// "recoverable" wrt pending deletes, in which
								// case the pending deletes are retained and
								// then re-flushing (with plenty of disk
								// space) will succeed in flushing the
								// deletes:
								Assert.Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + START_COUNT + " or " + END_COUNT);
							}
						}
						else
						{
							// On hitting exception we still may have added
							// all docs:
							if (result2 != START_COUNT && result2 != END_COUNT)
							{
								System.Console.Error.WriteLine(err.StackTrace);
								Assert.Fail(testName + ": method did throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + START_COUNT + " or " + END_COUNT);
							}
						}
						
						searcher.Close();
						newReader.Close();
						
						if (result2 == END_COUNT)
						{
							break;
						}
					}
					
					dir.Close();
					
					// Try again with 10 more bytes of free space:
					diskFree += 10;
				}
			}
		}
		
		// This test tests that buffered deletes are cleared when
		// an Exception is hit during flush.
		[Test]
		public virtual void  TestErrorAfterApplyDeletes()
		{
			
			MockRAMDirectory.Failure failure = new AnonymousClassFailure(this);
			
			// create a couple of files
			
			System.String[] keywords = new System.String[]{"1", "2"};
			System.String[] unindexed = new System.String[]{"Netherlands", "Italy"};
			System.String[] unstored = new System.String[]{"Amsterdam has lots of bridges", "Venice has lots of canals"};
			System.String[] text = new System.String[]{"Amsterdam", "Venice"};
			
			for (int pass = 0; pass < 2; pass++)
			{
				bool autoCommit = (0 == pass);
				MockRAMDirectory dir = new MockRAMDirectory();
				IndexWriter modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
				modifier.SetUseCompoundFile(true);
				modifier.SetMaxBufferedDeleteTerms(2);
				
				dir.FailOn(failure.Reset());
				
				for (int i = 0; i < keywords.Length; i++)
				{
					Document doc = new Document();
					doc.Add(new Field("id", keywords[i], Field.Store.YES, Field.Index.UN_TOKENIZED));
					doc.Add(new Field("country", unindexed[i], Field.Store.YES, Field.Index.NO));
					doc.Add(new Field("contents", unstored[i], Field.Store.NO, Field.Index.TOKENIZED));
					doc.Add(new Field("city", text[i], Field.Store.YES, Field.Index.TOKENIZED));
					modifier.AddDocument(doc);
				}
				// flush (and commit if ac)
				
				modifier.Optimize();
				
				// commit if !ac
				
				if (!autoCommit)
				{
					modifier.Close();
				}
				// one of the two files hits
				
				Term term = new Term("city", "Amsterdam");
				int hitCount = GetHitCount(dir, term);
				Assert.AreEqual(1, hitCount);
				
				// open the writer again (closed above)
				
				if (!autoCommit)
				{
					modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer());
					modifier.SetUseCompoundFile(true);
				}
				
				// delete the doc
				// max buf del terms is two, so this is buffered
				
				modifier.DeleteDocuments(term);
				
				// add a doc (needed for the !ac case; see below)
				// doc remains buffered
				
				Document doc2 = new Document();
				modifier.AddDocument(doc2);
				
				// flush the changes, the buffered deletes, and the new doc
				
				// The failure object will fail on the first write after the del
				// file gets created when processing the buffered delete
				
				// in the ac case, this will be when writing the new segments
				// files so we really don't need the new doc, but it's harmless
				
				// in the !ac case, a new segments file won't be created but in
				// this case, creation of the cfs file happens next so we need
				// the doc (to test that it's okay that we don't lose deletes if
				// failing while creating the cfs file
				
				bool failed = false;
				try
				{
					modifier.Flush();
				}
				catch (System.IO.IOException)
				{
					failed = true;
				}
				
				Assert.IsTrue(failed);
				
				// The flush above failed, so we need to retry it (which will
				// succeed, because the failure is a one-shot)
				
				if (!autoCommit)
				{
					modifier.Close();
				}
				else
				{
					modifier.Flush();
				}
				
				hitCount = GetHitCount(dir, term);
				
				// If the delete was not cleared then hit count will
				// be 0.  With autoCommit=false, we hit the exception
				// on creating the compound file, so the delete was
				// flushed successfully.
				Assert.AreEqual(autoCommit?1:0, hitCount);
				
				if (autoCommit)
				{
					modifier.Close();
				}
				
				dir.Close();
			}
		}
		
		// This test tests that the files created by the docs writer before
		// a segment is written are cleaned up if there's an i/o error
		
		[Test]
		public virtual void  TestErrorInDocsWriterAdd()
		{
			
			MockRAMDirectory.Failure failure = new AnonymousClassFailure1(this);
			
			// create a couple of files
			
			System.String[] keywords = new System.String[]{"1", "2"};
			System.String[] unindexed = new System.String[]{"Netherlands", "Italy"};
			System.String[] unstored = new System.String[]{"Amsterdam has lots of bridges", "Venice has lots of canals"};
			System.String[] text = new System.String[]{"Amsterdam", "Venice"};
			
			for (int pass = 0; pass < 2; pass++)
			{
				bool autoCommit = (0 == pass);
				MockRAMDirectory dir = new MockRAMDirectory();
				IndexWriter modifier = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
				
				dir.FailOn(failure.Reset());
				
				for (int i = 0; i < keywords.Length; i++)
				{
					Document doc = new Document();
					doc.Add(new Field("id", keywords[i], Field.Store.YES, Field.Index.UN_TOKENIZED));
					doc.Add(new Field("country", unindexed[i], Field.Store.YES, Field.Index.NO));
					doc.Add(new Field("contents", unstored[i], Field.Store.NO, Field.Index.TOKENIZED));
					doc.Add(new Field("city", text[i], Field.Store.YES, Field.Index.TOKENIZED));
					try
					{
						modifier.AddDocument(doc);
					}
					catch (System.IO.IOException)
					{
						break;
					}
				}
				
				System.String[] startFiles = dir.List();
				SegmentInfos infos = new SegmentInfos();
				infos.Read(dir);
				new IndexFileDeleter(dir, new KeepOnlyLastCommitDeletionPolicy(), infos, null, null);
				System.String[] endFiles = dir.List();
				
				if (!SupportClass.Compare.CompareStringArrays(startFiles, endFiles))
				{
					Assert.Fail("docswriter abort() failed to delete unreferenced files:\n  before delete:\n    " + ArrayToString(startFiles) + "\n  after delete:\n    " + ArrayToString(endFiles));
				}
				
				modifier.Close();
			}
		}
		
		private System.String ArrayToString(System.String[] l)
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
	}
}