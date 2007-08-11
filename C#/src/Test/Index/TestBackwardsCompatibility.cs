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
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using TermQuery = Lucene.Net.Search.TermQuery;
using Hits = Lucene.Net.Search.Hits;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;

namespace Lucene.Net.Index
{
	
	/*
	Verify we can read the pre-XXX file format, do searches
	against it, and add documents to it.*/
	
    [TestFixture]
	public class TestBackwardsCompatibility
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
		public virtual void  Unzip(System.String dirName)
		{
            Assert.Fail("needs integration with SharpZipLib");

            /*
			RmDir(dirName);
			
			System.Collections.IEnumerator entries;
			ZipFile zipFile;
			zipFile = new ZipFile(dirName + ".zip");
			
			entries = zipFile.Entries();
			System.IO.FileInfo fileDir = new System.IO.FileInfo(dirName);
			System.IO.Directory.CreateDirectory(fileDir.FullName);
			
			while (entries.MoveNext())
			{
				ZipEntry entry = (ZipEntry) entries.Current;
				
				System.IO.Stream in_Renamed = zipFile.GetInputStream(entry);
				System.IO.Stream out_Renamed = new System.IO.BufferedStream(new System.IO.FileStream(new System.IO.FileInfo(System.IO.Path.Combine(fileDir.FullName, entry.getName())).FullName, System.IO.FileMode.Create));
				
				byte[] buffer = new byte[8192];
				int len;
				while ((len = SupportClass.ReadInput(in_Renamed, buffer, 0, buffer.Length)) >= 0)
				{
					out_Renamed.Write(SupportClass.ToByteArray(buffer), 0, len);
				}
				
				in_Renamed.Close();
				out_Renamed.Close();
			}
			
			zipFile.Close();
            */
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
		
        [Test]
		public virtual void  TestSearchOldIndex()
		{
			System.String[] oldNames = new System.String[]{"prelockless.cfs", "prelockless.nocfs"};
			for (int i = 0; i < oldNames.Length; i++)
			{
				System.String dirName = "src/test/org/apache/lucene/index/index." + oldNames[i];
				Unzip(dirName);
				SearchIndex(dirName);
				RmDir(dirName);
			}
		}
		
        [Test]
		public virtual void  TestIndexOldIndexNoAdds()
		{
			System.String[] oldNames = new System.String[]{"prelockless.cfs", "prelockless.nocfs"};
			for (int i = 0; i < oldNames.Length; i++)
			{
				System.String dirName = "src/test/org/apache/lucene/index/index." + oldNames[i];
				Unzip(dirName);
				ChangeIndexNoAdds(dirName);
				RmDir(dirName);
			}
		}
		
        [Test]
		public virtual void  TestIndexOldIndex()
		{
			System.String[] oldNames = new System.String[]{"prelockless.cfs", "prelockless.nocfs"};
			for (int i = 0; i < oldNames.Length; i++)
			{
				System.String dirName = "src/test/org/apache/lucene/index/index." + oldNames[i];
				Unzip(dirName);
				ChangeIndexWithAdds(dirName);
				RmDir(dirName);
			}
		}
		
		public virtual void  SearchIndex(System.String dirName)
		{
			//QueryParser parser = new QueryParser("contents", new WhitespaceAnalyzer());
			//Query query = parser.parse("handle:1");
			
			Directory dir = FSDirectory.GetDirectory(dirName);
			IndexSearcher searcher = new IndexSearcher(dir);
			
			Hits hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(34, hits.Length());
			Lucene.Net.Documents.Document d = hits.Doc(0);
			
			// First document should be #21 since it's norm was increased:
			Assert.AreEqual("21", d.Get("id"), "didn't get the right document first");
			
			searcher.Close();
			dir.Close();
		}
		
		/* Open pre-lockless index, add docs, do a delete &
		* setNorm, and search */
		public virtual void  ChangeIndexWithAdds(System.String dirName)
		{
			
			Directory dir = FSDirectory.GetDirectory(dirName);
			// open writer
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
			
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
			Lucene.Net.Documents.Document d = hits.Doc(0);
			Assert.AreEqual("21", d.Get("id"), "wrong first document");
			searcher.Close();
			
			// make sure we can do another delete & another setNorm against this
			// pre-lockless segment:
			IndexReader reader = IndexReader.Open(dir);
			Term searchTerm = new Term("id", "6");
			int delCount = reader.DeleteDocuments(searchTerm);
			Assert.AreEqual(1, delCount, "wrong delete count");
			reader.SetNorm(22, "content", (float) 2.0);
			reader.Close();
			
			// make sure 2nd delete & 2nd norm "took":
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(43, hits.Length(), "wrong number of hits");
			d = hits.Doc(0);
			Assert.AreEqual("22", d.Get("id"), "wrong first document");
			searcher.Close();
			
			// optimize
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
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
		public virtual void  ChangeIndexNoAdds(System.String dirName)
		{
			
			Directory dir = FSDirectory.GetDirectory(dirName);
			
			// make sure searching sees right # hits
			IndexSearcher searcher = new IndexSearcher(dir);
			Hits hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(34, hits.Length(), "wrong number of hits");
			Lucene.Net.Documents.Document d = hits.Doc(0);
			Assert.AreEqual("21", d.Get("id"), "wrong first document");
			searcher.Close();
			
			// make sure we can do another delete & another setNorm against this
			// pre-lockless segment:
			IndexReader reader = IndexReader.Open(dir);
			Term searchTerm = new Term("id", "6");
			int delCount = reader.DeleteDocuments(searchTerm);
			Assert.AreEqual(1, delCount, "wrong delete count");
			reader.SetNorm(22, "content", (float) 2.0);
			reader.Close();
			
			// make sure 2nd delete & 2nd norm "took":
			searcher = new IndexSearcher(dir);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")));
			Assert.AreEqual(33, hits.Length(), "wrong number of hits");
			d = hits.Doc(0);
			Assert.AreEqual("22", d.Get("id"), "wrong first document");
			searcher.Close();
			
			// optimize
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
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
		
		// disable until hardcoded file names are fixes:
        [Test]
		public virtual void  TestExactFileNames()
		{
			
			System.String outputDir = "lucene.backwardscompat0.index";
			Directory dir = FSDirectory.GetDirectory(outputDir);
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
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
			
			// The numbering of fields can vary depending on which
			// JRE is in use.  On some JREs we see content bound to
			// field 0; on others, field 1.  So, here we have to
			// figure out which field number corresponds to
			// "content", and then set our expected file names below
			// accordingly:
			CompoundFileReader cfsReader = new CompoundFileReader(dir, "_2.cfs");
			FieldInfos fieldInfos = new FieldInfos(cfsReader, "_2.fnm");
			int contentFieldIndex = - 1;
			for (int i = 0; i < fieldInfos.Size(); i++)
			{
				FieldInfo fi = fieldInfos.FieldInfo(i);
				if (fi.Name.Equals("content"))
				{
					contentFieldIndex = i;
					break;
				}
			}
			cfsReader.Close();
			Assert.IsTrue(contentFieldIndex != - 1, "could not locate the 'content' field number in the _2.cfs segment");
			
			// Now verify file names:
			System.String[] expected = new System.String[]{"_0.cfs", "_0_1.del", "_1.cfs", "_2.cfs", "_2_1.s" + contentFieldIndex, "_3.cfs", "segments_a", "segments.gen"};
			
			System.String[] actual = dir.List();
			System.Array.Sort(expected);
			System.Array.Sort(actual);
			if (!ArrayEquals(expected, actual))
			{
				Assert.Fail("incorrect filenames in index: expected:\n    " + AsString(expected) + "\n  actual:\n    " + AsString(actual));
			}
			dir.Close();
			
			RmDir(outputDir);
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
			System.IO.FileInfo fileDir = new System.IO.FileInfo(dir);
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