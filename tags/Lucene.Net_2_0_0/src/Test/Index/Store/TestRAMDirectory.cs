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
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using English = Lucene.Net.Util.English;

namespace Lucene.Net.Index.Store
{
	
	/// <summary> JUnit testcase to test RAMDirectory. RAMDirectory itself is used in many testcases,
	/// but not one of them uses an different constructor other than the default constructor.
	/// 
	/// </summary>
	/// <author>  Bernhard Messer
	/// 
	/// </author>
	/// <version>  $Id: RAMDirectory.java 150537 2004-09-28 22:45:26 +0200 (Di, 28 Sep 2004) cutting $
	/// </version>
	[TestFixture]
    public class TestRAMDirectory
	{
		
		private System.IO.FileInfo indexDir = null;
		
		// add enough document so that the index will be larger than RAMDirectory.READ_BUFFER_SIZE
		private int docsToAdd = 500;
		
		// setup the index
        [SetUp]
		public virtual void  SetUp()
		{
			System.String tempDir = System.IO.Path.GetTempPath();
			if (tempDir == null)
				throw new System.IO.IOException("java.io.tmpdir undefined, cannot run test");
			indexDir = new System.IO.FileInfo(System.IO.Path.Combine(tempDir, "RAMDirIndex"));
			
			IndexWriter writer = new IndexWriter(indexDir, new WhitespaceAnalyzer(), true);
			// add some documents
			Lucene.Net.Documents.Document doc = null;
			for (int i = 0; i < docsToAdd; i++)
			{
				doc = new Lucene.Net.Documents.Document();
				doc.Add(new Field("content", English.IntToEnglish(i).Trim(), Field.Store.YES, Field.Index.UN_TOKENIZED));
				writer.AddDocument(doc);
			}
			Assert.AreEqual(docsToAdd, writer.DocCount());
			writer.Optimize();
			writer.Close();
		}
		
		[Test]
        public virtual void  TestRAMDirectory_Renamed_Method()
		{
			
			Directory dir = FSDirectory.GetDirectory(indexDir, false);
			RAMDirectory ramDir = new RAMDirectory(dir);
			
			// close the underlaying directory and delete the index
			dir.Close();
			
			// open reader to test document count
			IndexReader reader = IndexReader.Open(ramDir);
			Assert.AreEqual(docsToAdd, reader.NumDocs());
			
			// open search zo check if all doc's are there
			IndexSearcher searcher = new IndexSearcher(reader);
			
			// search for all documents
			for (int i = 0; i < docsToAdd; i++)
			{
				Lucene.Net.Documents.Document doc = searcher.Doc(i);
				Assert.IsTrue(doc.GetField("content") != null);
			}
			
			// cleanup
			reader.Close();
			searcher.Close();
		}
		
        [Test]
		public virtual void  TestRAMDirectoryFile()
		{
			
			RAMDirectory ramDir = new RAMDirectory(indexDir);
			
			// open reader to test document count
			IndexReader reader = IndexReader.Open(ramDir);
			Assert.AreEqual(docsToAdd, reader.NumDocs());
			
			// open search zo check if all doc's are there
			IndexSearcher searcher = new IndexSearcher(reader);
			
			// search for all documents
			for (int i = 0; i < docsToAdd; i++)
			{
				Lucene.Net.Documents.Document doc = searcher.Doc(i);
				Assert.IsTrue(doc.GetField("content") != null);
			}
			
			// cleanup
			reader.Close();
			searcher.Close();
		}
		
		[Test]
        public virtual void  TestRAMDirectoryString()
		{
			
			RAMDirectory ramDir = new RAMDirectory(indexDir.FullName);
			
			// open reader to test document count
			IndexReader reader = IndexReader.Open(ramDir);
			Assert.AreEqual(docsToAdd, reader.NumDocs());
			
			// open search zo check if all doc's are there
			IndexSearcher searcher = new IndexSearcher(reader);
			
			// search for all documents
			for (int i = 0; i < docsToAdd; i++)
			{
				Lucene.Net.Documents.Document doc = searcher.Doc(i);
				Assert.IsTrue(doc.GetField("content") != null);
			}
			
			// cleanup
			reader.Close();
			searcher.Close();
		}
		
        [TearDown]
		public virtual void  TearDown()
		{
			// cleanup 
			bool tmpBool;
			if (System.IO.File.Exists(indexDir.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(indexDir.FullName);
			if (indexDir != null && tmpBool)
			{
				RmDir(indexDir);
			}
		}

		private void  RmDir(System.IO.FileInfo dir)
		{
			System.IO.FileInfo[] files = SupportClass.FileSupport.GetFiles(dir);
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
	}
}