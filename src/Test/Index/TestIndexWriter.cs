/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	
	
	/// <author>  goller
	/// </author>
    /// <version>  $Id: TestIndexWriter.java 387550 2006-03-21 15:36:32Z yonik $
    /// </version>
	[TestFixture]
    public class TestIndexWriter
	{
		[Test]
        public virtual void  TestDocCount()
		{
			Directory dir = new RAMDirectory();
			
			IndexWriter writer = null;
			IndexReader reader = null;
			int i;
			
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			
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
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
			Assert.AreEqual(100, writer.DocCount());
			writer.Close();
			
			reader = IndexReader.Open(dir);
			Assert.AreEqual(100, reader.MaxDoc());
			Assert.AreEqual(60, reader.NumDocs());
			reader.Close();
			
			// optimize the index and check that the new doc count is correct
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
			writer.Optimize();
			Assert.AreEqual(60, writer.DocCount());
			writer.Close();
			
			// check that the index reader gives the same numbers.
			reader = IndexReader.Open(dir);
			Assert.AreEqual(60, reader.MaxDoc());
			Assert.AreEqual(60, reader.NumDocs());
			reader.Close();
		}
		
		private void  AddDoc(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("content", "aaa", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
		}
	}
}