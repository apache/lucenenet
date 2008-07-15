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
using FSDirectory = Lucene.Net.Store.FSDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using TermQuery = Lucene.Net.Search.TermQuery;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Index
{
	
	/*
	Verify we can read the pre-2.1 file format, do searches
	against it, and add documents to it.*/
	
	[TestFixture]
	public class TestBackwardsCompatibility : LuceneTestCase
	{
		
		// Uncomment these cases & run in a pre-lockless checkout
		// to create indices:
		
		/*
		public void testCreatePreLocklessCFS() throws IOException {
		CreateIndex("src/test/org/apache/lucene/index/index.prelockless.cfs", true);
		}
		
		public void testCreatePreLocklessNoCFS() throws IOException {
		CreateIndex("src/test/org/apache/lucene/index/index.prelockless.nocfs", false);
		}
		*/
		
		/* Unzips dirName + ".zip" --> dirName, removing dirName
		first */
		public virtual void  Unzip(System.String zipName, System.String destDirName)
		{
#if SHARP_ZIP_LIB
			// get zip input stream
			ICSharpCode.SharpZipLib.Zip.ZipInputStream zipFile;
			zipFile = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(System.IO.File.OpenRead(zipName + ".zip"));

			// get dest directory name
			System.String dirName = FullDir(destDirName);
			System.IO.FileInfo fileDir = new System.IO.FileInfo(dirName);

			// clean up old directory (if there) and create new directory
			RmDir(fileDir.FullName);
			System.IO.Directory.CreateDirectory(fileDir.FullName);

			// copy file entries from zip stream to directory
			ICSharpCode.SharpZipLib.Zip.ZipEntry entry;
			while ((entry = zipFile.GetNextEntry()) != null)
			{
				System.IO.Stream streamout = new System.IO.BufferedStream(new System.IO.FileStream(new System.IO.FileInfo(System.IO.Path.Combine(fileDir.FullName, entry.Name)).FullName, System.IO.FileMode.Create));
				
				byte[] buffer = new byte[8192];
				int len;
				while ((len = zipFile.Read(buffer, 0, buffer.Length)) > 0)
				{
					streamout.Write(buffer, 0, len);
				}
				
				streamout.Close();
			}
			
			zipFile.Close();
#else
			Assert.Fail("Needs integration with SharpZipLib");
#endif
		}
		
		[Test]
		public virtual void  TestCreateCFS()
		{
			System.String dirName = "testindex.cfs";
			CreateIndex(dirName, true);
			RmDir(dirName);
		}
		
		[Test]
		public virtual void  TestCreateNoCFS()
		{
			System.String dirName = "testindex.nocfs";
			CreateIndex(dirName, true);
			RmDir(dirName);
		}
		
		internal System.String[] oldNames = new System.String[]{"prelockless.cfs", "prelockless.nocfs", "presharedstores.cfs", "presharedstores.nocfs"};
		
		[Test]
		public virtual void  TestSearchOldIndex()
		{
			for (int i = 0; i < oldNames.Length; i++)
			{
				System.String dirName = @"Index\index." + oldNames[i];
				Unzip(dirName, oldNames[i]);
				SearchIndex(oldNames[i]);
				RmDir(oldNames[i]);
			}
		}
		
		[Test]
		public virtual void  TestIndexOldIndexNoAdds()
		{
			for (int i = 0; i < oldNames.Length; i++)
			{
				System.String dirName = @"Index\index." + oldNames[i];
				Unzip(dirName, oldNames[i]);
				ChangeIndexNoAdds(oldNames[i], true);
				RmDir(oldNames[i]);
				
				Unzip(dirName, oldNames[i]);
				ChangeIndexNoAdds(oldNames[i], false);
				RmDir(oldNames[i]);
			}
		}
		
		[Test]
		public virtual void  TestIndexOldIndex()
		{
			for (int i = 0; i < oldNames.Length; i++)
			{
				System.String dirName = @"Index\index." + oldNames[i];
				Unzip(dirName, oldNames[i]);
				ChangeIndexWithAdds(oldNames[i], true);
				RmDir(oldNames[i]);
				
				Unzip(dirName, oldNames[i]);
				ChangeIndexWithAdds(oldNames[i], false);
				RmDir(oldNames[i]);
			}
		}
		
		public virtual void  SearchIndex(System.String dirName)
		{
			//QueryParser parser = new QueryParser("contents", new WhitespaceAnalyzer());
			//Query query = parser.parse("handle:1");
			
			dirName = FullDir(dirName);
			
			Directory dir = FSDirectory.GetDirectory(dirName);
			IndexSearcher searcher = new IndexSearcher(dir);
			
			Hits hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(34, hits.Length());
			Document d = hits.Doc(0);
			
			// First document should be #21 since it's norm was increased:
			Assert.AreEqual("21", d.Get("id"), "didn't get the right document first");
			
			searcher.Close();
			dir.Close();
		}
		
		/* Open pre-lockless index, add docs, do a delete &
		* setNorm, and search */
		public virtual void  ChangeIndexWithAdds(System.String dirName, bool autoCommit)
		{
			
			dirName = FullDir(dirName);
			
			Directory dir = FSDirectory.GetDirectory(dirName);
			
			// open writer
			IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false);
			
			// add 10 docs
			for (int i = 0; i < 10; i++)
			{
				AddDoc(writer, 35 + i);
			}
			
			// make sure writer sees right total -- writer seems not to know about deletes in .del?
			Assert.AreEqual(45, writer.DocCount(), "wrong doc count");
			writer.Close();
			
			// make sure searching sees right # hits
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(44, hits.Length(), "wrong number of hits");
			Document d = hits.Doc(0);
			Assert.AreEqual("21", d.Get("id"), "wrong first document");
			searcher.Close();
			
			// make sure we can do delete & setNorm against this
			// pre-lockless segment:
			IndexReader reader = IndexReader.Open(dir);
			Term searchTerm = new Term("id", "6");
			int delCount = reader.DeleteDocuments(searchTerm);
			Assert.AreEqual(1, delCount, "wrong delete count");
			reader.SetNorm(22, "content", (float) 2.0);
			reader.Close();
			
			// make sure they "took":
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(43, hits.Length(), "wrong number of hits");
			d = hits.Doc(0);
			Assert.AreEqual("22", d.Get("id"), "wrong first document");
			searcher.Close();
			
			// optimize
			writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false);
			writer.Optimize();
			writer.Close();
			
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(43, hits.Length(), "wrong number of hits");
			d = hits.Doc(0);
			Assert.AreEqual("22", d.Get("id"), "wrong first document");
			searcher.Close();
			
			dir.Close();
		}
		
		/* Open pre-lockless index, add docs, do a delete &
		* setNorm, and search */
		public virtual void ChangeIndexNoAdds(System.String dirName, bool autoCommit)
		{
			
			dirName = FullDir(dirName);
			
			Directory dir = FSDirectory.GetDirectory(dirName);
			
			// make sure searching sees right # hits
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(34, hits.Length(), "wrong number of hits");
			Document d = hits.Doc(0);
			Assert.AreEqual("21", d.Get("id"), "wrong first document");
			searcher.Close();
			
			// make sure we can do a delete & setNorm against this
			// pre-lockless segment:
			IndexReader reader = IndexReader.Open(dir);
			Term searchTerm = new Term("id", "6");
			int delCount = reader.DeleteDocuments(searchTerm);
			Assert.AreEqual(1, delCount, "wrong delete count");
			reader.SetNorm(22, "content", (float) 2.0);
			reader.Close();
			
			// make sure they "took":
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(33, hits.Length(), "wrong number of hits");
			d = hits.Doc(0);
			Assert.AreEqual("22", d.Get("id"), "wrong first document");
			searcher.Close();
			
			// optimize
			IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), false);
			writer.Optimize();
			writer.Close();
			
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(33, hits.Length(), "wrong number of hits");
			d = hits.Doc(0);
			Assert.AreEqual("22", d.Get("id"), "wrong first document");
			searcher.Close();
			
			dir.Close();
		}
		
		public virtual void  CreateIndex(System.String dirName, bool doCFS)
		{
			
			RmDir(dirName);
			
			dirName = FullDir(dirName);
			
			Directory dir = FSDirectory.GetDirectory(dirName);
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			writer.SetUseCompoundFile(doCFS);
			
			for (int i = 0; i < 35; i++)
			{
				AddDoc(writer, i);
			}
			Assert.AreEqual(35, writer.DocCount(), "wrong doc count");
			writer.Close();
			
			// Delete one doc so we get a .del file:
			IndexReader reader = IndexReader.Open(dir);
			Term searchTerm = new Term("id", "7");
			int delCount = reader.DeleteDocuments(searchTerm);
			Assert.AreEqual(1, delCount, "didn't delete the right number of documents");
			
			// Set one norm so we get a .s0 file:
			reader.SetNorm(21, "content", (float) 1.5);
			reader.Close();
		}
		
		/* Verifies that the expected file names were produced */

		[Test]
		public virtual void  TestExactFileNames()
		{
			
			for (int pass = 0; pass < 2; pass++)
			{
				
				System.String outputDir = "lucene.backwardscompat0.index";
				RmDir(outputDir);
				
				try
				{
					Directory dir = FSDirectory.GetDirectory(FullDir(outputDir));
					
					bool autoCommit = 0 == pass;
					
					IndexWriter writer = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
					writer.SetRAMBufferSizeMB(16.0);
					//IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
					for (int i = 0; i < 35; i++)
					{
						AddDoc(writer, i);
					}
					Assert.AreEqual(35, writer.DocCount());
					writer.Close();
					
					// Delete one doc so we get a .del file:
					IndexReader reader = IndexReader.Open(dir);
					Term searchTerm = new Term("id", "7");
					int delCount = reader.DeleteDocuments(searchTerm);
					Assert.AreEqual(1, delCount, "didn't delete the right number of documents");
					
					// Set one norm so we get a .s0 file:
					reader.SetNorm(21, "content", (float) 1.5);
					reader.Close();
					
					// The numbering of fields can vary depending on which
					// JRE is in use.  On some JREs we see content bound to
					// field 0; on others, field 1.  So, here we have to
					// figure out which field number corresponds to
					// "content", and then set our expected file names below
					// accordingly:
					CompoundFileReader cfsReader = new CompoundFileReader(dir, "_0.cfs");
					FieldInfos fieldInfos = new FieldInfos(cfsReader, "_0.fnm");
					int contentFieldIndex = - 1;
					for (int i = 0; i < fieldInfos.Size(); i++)
					{
						FieldInfo fi = fieldInfos.FieldInfo(i);
						if (fi.Name_ForNUnitTest.Equals("content"))
						{
							contentFieldIndex = i;
							break;
						}
					}
					cfsReader.Close();
					Assert.IsTrue(contentFieldIndex != - 1, "could not locate the 'content' field number in the _2.cfs segment");
					
					// Now verify file names:
					System.String[] expected;
					expected = new System.String[]{"_0.cfs", "_0_1.del", "_0_1.s" + contentFieldIndex, "segments_4", "segments.gen"};
					
					if (!autoCommit)
						expected[3] = "segments_3";
					
					System.String[] actual = dir.List();
					System.Array.Sort(expected);
					System.Array.Sort(actual);
					if (!ArrayEquals(expected, actual))
					{
						Assert.Fail("incorrect filenames in index: expected:\n    " + AsString(expected) + "\n  actual:\n    " + AsString(actual));
					}
					dir.Close();
				}
				finally
				{
					RmDir(outputDir);
				}
			}
		}
		
		private System.String AsString(System.String[] l)
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
		
		private void  AddDoc(IndexWriter writer, int id)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("content", "aaa", Field.Store.NO, Field.Index.TOKENIZED));
			doc.Add(new Field("id", System.Convert.ToString(id), Field.Store.YES, Field.Index.UN_TOKENIZED));
			writer.AddDocument(doc);
		}
		
		private void  RmDir(System.String dir)
		{
			System.IO.FileInfo fileDir = new System.IO.FileInfo(FullDir(dir));
			bool tmpBool;
			if (System.IO.File.Exists(fileDir.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(fileDir.FullName);
			if (tmpBool)
			{
				System.String[] files = System.IO.Directory.GetFileSystemEntries(fileDir.FullName);
				if (files != null)
				{
					for (int i = 0; i < files.Length; i++)
					{
						bool tmpBool2;
						if (System.IO.File.Exists(files[i]))
						{
							System.IO.File.Delete(files[i]);
							tmpBool2 = true;
						}
						else if (System.IO.Directory.Exists(files[i]))
						{
							System.IO.Directory.Delete(files[i]);
							tmpBool2 = true;
						}
						else
							tmpBool2 = false;
						bool generatedAux = tmpBool2;
					}
				}
				bool tmpBool3;
				if (System.IO.File.Exists(fileDir.FullName))
				{
					System.IO.File.Delete(fileDir.FullName);
					tmpBool3 = true;
				}
				else if (System.IO.Directory.Exists(fileDir.FullName))
				{
					System.IO.Directory.Delete(fileDir.FullName);
					tmpBool3 = true;
				}
				else
					tmpBool3 = false;
				bool generatedAux2 = tmpBool3;
			}
		}
		
		public static System.String FullDir(System.String dirName)
		{
			return new System.IO.FileInfo(System.IO.Path.Combine(SupportClass.AppSettings.Get("tempDir", ""), dirName)).FullName;
		}

		public static bool ArrayEquals(System.Array array1, System.Array array2)
		{
			bool result = false;
			if ((array1 == null) && (array2 == null))
				result = true;
			else if ((array1 != null) && (array2 != null))
			{
				if (array1.Length == array2.Length)
				{
					int length = array1.Length;
					result = true;
					for (int index = 0; index < length; index++)
					{
						if (!(array1.GetValue(index).Equals(array2.GetValue(index))))
						{
							result = false;
							break;
						}
					}
				}
			}
			return result;
		}
	}
}