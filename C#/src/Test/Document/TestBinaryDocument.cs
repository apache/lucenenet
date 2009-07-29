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

using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;

namespace Lucene.Net.Documents
{
	
	/// <summary> Tests {@link Document} class.
	/// 
	/// 
	/// </summary>
	/// <version>  $Id: TestBinaryDocument.java 598296 2007-11-26 14:52:01Z mikemccand $
	/// </version>
	[TestFixture]
	public class TestBinaryDocument : LuceneTestCase    
	{
		
		internal System.String binaryValStored = "this text will be stored as a byte array in the index";
		internal System.String binaryValCompressed = "this text will be also stored and compressed as a byte array in the index";
		
		[Test]
		public virtual void  TestBinaryFieldInIndex()
		{
			Lucene.Net.Documents.Fieldable binaryFldStored = new Field("binaryStored", System.Text.UTF8Encoding.UTF8.GetBytes(binaryValStored), Field.Store.YES);
			Lucene.Net.Documents.Fieldable binaryFldCompressed = new Field("binaryCompressed", System.Text.UTF8Encoding.UTF8.GetBytes(binaryValCompressed), Field.Store.COMPRESS);
			Lucene.Net.Documents.Fieldable stringFldStored = new Field("stringStored", binaryValStored, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
			Lucene.Net.Documents.Fieldable stringFldCompressed = new Field("stringCompressed", binaryValCompressed, Field.Store.COMPRESS, Field.Index.NO, Field.TermVector.NO);
			
			try
			{
				// binary fields with store off are not allowed
				new Field("fail", System.Text.UTF8Encoding.UTF8.GetBytes(binaryValCompressed), Field.Store.NO);
				Assert.Fail();
			}
			catch (System.ArgumentException)
			{
			}
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			
			doc.Add(binaryFldStored);
			doc.Add(binaryFldCompressed);
			
			doc.Add(stringFldStored);
			doc.Add(stringFldCompressed);
			
			/** test for field count */
			Assert.AreEqual(4, doc.GetFieldsCount());
			
			/** add the doc to a ram index */
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
			writer.AddDocument(doc);
			writer.Close();
			
			/** open a reader and fetch the document */
			IndexReader reader = IndexReader.Open(dir);
			Lucene.Net.Documents.Document docFromReader = reader.Document(0);
			Assert.IsTrue(docFromReader != null);
			
			/** fetch the binary stored field and compare it's content with the original one */
			System.String binaryFldStoredTest = System.Text.UTF8Encoding.UTF8.GetString(docFromReader.GetBinaryValue("binaryStored"));
			Assert.IsTrue(binaryFldStoredTest.Equals(binaryValStored));
			
			/** fetch the binary compressed field and compare it's content with the original one */
			System.String binaryFldCompressedTest = System.Text.UTF8Encoding.UTF8.GetString(docFromReader.GetBinaryValue("binaryCompressed"));
			Assert.IsTrue(binaryFldCompressedTest.Equals(binaryValCompressed));
			
			/** fetch the string field and compare it's content with the original one */
			System.String stringFldStoredTest = docFromReader.Get("stringStored");
			Assert.IsTrue(stringFldStoredTest.Equals(binaryValStored));
			
			/** fetch the compressed string field and compare it's content with the original one */
			System.String stringFldCompressedTest = docFromReader.Get("stringCompressed");
			Assert.IsTrue(stringFldCompressedTest.Equals(binaryValCompressed));
			
			/** delete the document from index */
			reader.DeleteDocument(0);
			Assert.AreEqual(0, reader.NumDocs());
			
			reader.Close();
		}
	}
}