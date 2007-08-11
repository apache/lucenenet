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

using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using TermQuery = Lucene.Net.Search.TermQuery;
using Directory = Lucene.Net.Store.Directory;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	
    [TestFixture]
    public class TestIndexWriterDelete
	{
		
		// test the simple case
        [Test]
		public virtual void  TestSimpleCase()
		{
			System.String[] keywords = new System.String[]{"1", "2"};
			System.String[] unindexed = new System.String[]{"Netherlands", "Italy"};
			System.String[] unstored = new System.String[]{"Amsterdam has lots of bridges", "Venice has lots of canals"};
			System.String[] text = new System.String[]{"Amsterdam", "Venice"};
			
			Directory dir = new RAMDirectory();
			IndexWriter modifier = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			modifier.SetUseCompoundFile(true);
			modifier.SetMaxBufferedDeleteTerms(1);
			
			for (int i = 0; i < keywords.Length; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
				doc.Add(new Field("id", keywords[i], Field.Store.YES, Field.Index.UN_TOKENIZED));
				doc.Add(new Field("country", unindexed[i], Field.Store.YES, Field.Index.NO));
				doc.Add(new Field("contents", unstored[i], Field.Store.NO, Field.Index.TOKENIZED));
				doc.Add(new Field("city", text[i], Field.Store.YES, Field.Index.TOKENIZED));
				modifier.AddDocument(doc);
			}
			modifier.Optimize();
			
			Term term = new Term("city", "Amsterdam");
			int hitCount = GetHitCount(dir, term);
			Assert.AreEqual(1, hitCount);
			modifier.DeleteDocuments(term);
			hitCount = GetHitCount(dir, term);
			Assert.AreEqual(0, hitCount);
			
			modifier.Close();
		}
		
		// test when delete terms only apply to disk segments
        [Test]
		public virtual void  TestNonRAMDelete()
		{
			Directory dir = new RAMDirectory();
			IndexWriter modifier = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			modifier.SetMaxBufferedDocs(2);
			modifier.SetMaxBufferedDeleteTerms(2);
			
			int id = 0;
			int value_Renamed = 100;
			
			for (int i = 0; i < 7; i++)
			{
				AddDoc(modifier, ++id, value_Renamed);
			}
			modifier.Flush();
			
			Assert.AreEqual(0, modifier.GetRamSegmentCount());
			Assert.IsTrue(0 < modifier.GetSegmentCount());
			
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(7, reader.NumDocs());
			reader.Close();
			
			modifier.DeleteDocuments(new Term("value", System.Convert.ToString(value_Renamed)));
			modifier.DeleteDocuments(new Term("value", System.Convert.ToString(value_Renamed)));
			
			reader = IndexReader.Open(dir);
			Assert.AreEqual(0, reader.NumDocs());
			reader.Close();
			
			modifier.Close();
		}
		
		// test when delete terms only apply to ram segments
        [Test]
		public virtual void  TestRAMDeletes()
		{
			Directory dir = new RAMDirectory();
			IndexWriter modifier = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
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
			
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(1, reader.NumDocs());
			
			int hitCount = GetHitCount(dir, new Term("id", System.Convert.ToString(id)));
			Assert.AreEqual(1, hitCount);
			reader.Close();
			
			modifier.Close();
		}
		
		// test when delete terms apply to both disk and ram segments
        [Test]
		public virtual void  TestBothDeletes()
		{
			Directory dir = new RAMDirectory();
			IndexWriter modifier = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
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
			
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(5, reader.NumDocs());
			
			modifier.Close();
		}
		
		// test that batched delete terms are flushed together
        [Test]
		public virtual void  TestBatchDeletes()
		{
			Directory dir = new RAMDirectory();
			IndexWriter modifier = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			modifier.SetMaxBufferedDocs(2);
			modifier.SetMaxBufferedDeleteTerms(2);
			
			int id = 0;
			int value_Renamed = 100;
			
			for (int i = 0; i < 7; i++)
			{
				AddDoc(modifier, ++id, value_Renamed);
			}
			modifier.Flush();
			
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(7, reader.NumDocs());
			reader.Close();
			
			id = 0;
			modifier.DeleteDocuments(new Term("id", System.Convert.ToString(++id)));
			modifier.DeleteDocuments(new Term("id", System.Convert.ToString(++id)));
			
			reader = IndexReader.Open(dir);
			Assert.AreEqual(5, reader.NumDocs());
			reader.Close();
			
			Term[] terms = new Term[3];
			for (int i = 0; i < terms.Length; i++)
			{
				terms[i] = new Term("id", System.Convert.ToString(++id));
			}
			modifier.DeleteDocuments(terms);
			
			reader = IndexReader.Open(dir);
			Assert.AreEqual(2, reader.NumDocs());
			reader.Close();
			
			modifier.Close();
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
			
			// First build up a starting index:
			RAMDirectory startDir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(startDir, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < 157; i++)
			{
				Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
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
				IndexWriter modifier = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
				
				modifier.SetMaxBufferedDocs(1000); // use flush or Close
				modifier.SetMaxBufferedDeleteTerms(1000); // use flush or Close
				
				// For each disk size, first try to commit against
				// dir that will hit random IOExceptions & disk
				// full; after, give it infinite disk space & turn
				// off random IOExceptions & retry w/ same reader:
				bool success = false;
				
				for (int x = 0; x < 2; x++)
				{
					
					double rate = 0.1;
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
						testName = "disk full during reader.Close() @ " + thisDiskFree + " bytes";
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
									Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
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
							System.Console.Out.WriteLine("  hit IOException: " + e);
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
					IndexFileDeleter d2 = new IndexFileDeleter(infos, dir);
					d2.FindDeletableFiles();
					d2.DeleteFiles();
					System.String[] endFiles = dir.List();
					
					System.Array.Sort(startFiles);
					System.Array.Sort(endFiles);
					
					// for(int i=0;i<startFiles.length;i++) {
					// System.out.println(" startFiles: " + i + ": " + startFiles[i]);
					// }
					
					if (!startFiles.Equals(endFiles))
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
						Assert.Fail("reader.Close() failed to delete unreferenced files after " + successStr + " (" + diskFree + " bytes): before delete:\n    " + ArrayToString(startFiles) + "\n  after delete:\n    " + ArrayToString(endFiles));
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
						Assert.Fail(testName + ":exception when creating IndexReader after disk full during Close: " + e);
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
						Assert.Fail(testName + ": exception when searching: " + e);
					}
					int result2 = hits.Length();
					if (success)
					{
						if (result2 != END_COUNT)
						{
							Assert.Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + END_COUNT);
						}
					}
					else
					{
						// On hitting exception we still may have added
						// all docs:
						if (result2 != START_COUNT && result2 != END_COUNT)
						{
							System.Console.Error.WriteLine(err.StackTrace);
							Assert.Fail(testName + ": method did throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + START_COUNT);
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