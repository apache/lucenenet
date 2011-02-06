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
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using Searcher = Lucene.Net.Search.Searcher;
using TermQuery = Lucene.Net.Search.TermQuery;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Document
{
	
	/// <summary> Tests {@link Document} class.
	/// 
	/// </summary>
	/// <author>  Otis Gospodnetic
	/// </author>
	/// <version>  $Id: TestDocument.java 208846 2005-07-02 16:40:44Z dnaber $
	/// </version>
	[TestFixture]
    public class TestDocument
	{
		
		internal System.String binaryVal = "this text will be stored as a byte array in the index";
		internal System.String binaryVal2 = "this text will be also stored as a byte array in the index";
		
        [Test]
		public virtual void  TestBinaryField()
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			Field stringFld = new Field("string", binaryVal, Field.Store.YES, Field.Index.NO);
			Field binaryFld = new Field("binary", (new System.Text.ASCIIEncoding()).GetBytes(binaryVal), Field.Store.YES);
			Field binaryFld2 = new Field("binary", (new System.Text.ASCIIEncoding()).GetBytes(binaryVal2), Field.Store.YES);

			doc.Add(stringFld);
			doc.Add(binaryFld);

			Assert.AreEqual(2, doc.GetFieldsCount());
			
			Assert.IsTrue(binaryFld.IsBinary());
			Assert.IsTrue(binaryFld.IsStored());
			Assert.IsFalse(binaryFld.IsIndexed());
			Assert.IsFalse(binaryFld.IsTokenized());
			
            System.String binaryTest = (new System.Text.ASCIIEncoding()).GetString(doc.GetBinaryValue("binary"));
			Assert.IsTrue(binaryTest.Equals(binaryVal));
			
			System.String stringTest = doc.Get("string");
			Assert.IsTrue(binaryTest.Equals(stringTest));
			
			doc.Add(binaryFld2);
			
            Assert.AreEqual(3, doc.GetFieldsCount());
			
			byte[][] binaryTests = doc.GetBinaryValues("binary");
			
			Assert.AreEqual(2, binaryTests.Length);
			
			binaryTest = new System.String(System.Text.UTF8Encoding.UTF8.GetChars(binaryTests[0]));
			System.String binaryTest2 = new System.String(System.Text.UTF8Encoding.UTF8.GetChars(binaryTests[1]));
			
			Assert.IsFalse(binaryTest.Equals(binaryTest2));
			
			Assert.IsTrue(binaryTest.Equals(binaryVal));
			Assert.IsTrue(binaryTest2.Equals(binaryVal2));
			
			doc.RemoveField("string");
            Assert.AreEqual(2, doc.GetFieldsCount());
			
			doc.RemoveFields("binary");
            Assert.AreEqual(0, doc.GetFieldsCount());
		}
		
		/// <summary> Tests {@link Document#RemoveField(String)} method for a brand new Document
		/// that has not been indexed yet.
		/// 
		/// </summary>
		/// <throws>  Exception on error </throws>
		[Test]
		public virtual void  TestRemoveForNewDocument()
		{
			Lucene.Net.Documents.Document doc = MakeDocumentWithFields();
            Assert.AreEqual(8, doc.GetFieldsCount());
			doc.RemoveFields("keyword");
            Assert.AreEqual(6, doc.GetFieldsCount());
			doc.RemoveFields("doesnotexists"); // removing non-existing fields is siltenlty ignored
			doc.RemoveFields("keyword"); // removing a field more than once
            Assert.AreEqual(6, doc.GetFieldsCount());
			doc.RemoveField("text");
            Assert.AreEqual(5, doc.GetFieldsCount());
			doc.RemoveField("text");
            Assert.AreEqual(4, doc.GetFieldsCount());
			doc.RemoveField("text");
            Assert.AreEqual(4, doc.GetFieldsCount());
			doc.RemoveField("doesnotexists"); // removing non-existing fields is siltenlty ignored
            Assert.AreEqual(4, doc.GetFieldsCount());
			doc.RemoveFields("unindexed");
			Assert.AreEqual(2, doc.GetFieldsCount());
			doc.RemoveFields("unstored");
            Assert.AreEqual(0, doc.GetFieldsCount());
			doc.RemoveFields("doesnotexists"); // removing non-existing fields is siltenlty ignored
            Assert.AreEqual(0, doc.GetFieldsCount());
		}
		
        [Test]
		public virtual void  TestConstructorExceptions()
		{
			new Field("name", "value", Field.Store.YES, Field.Index.NO); // okay
			new Field("name", "value", Field.Store.NO, Field.Index.UN_TOKENIZED); // okay
			try
			{
				new Field("name", "value", Field.Store.NO, Field.Index.NO);
				Assert.Fail();
			}
			catch (System.ArgumentException e)
			{
				// expected exception
			}
			new Field("name", "value", Field.Store.YES, Field.Index.NO, Field.TermVector.NO); // okay
			try
			{
				new Field("name", "value", Field.Store.YES, Field.Index.NO, Field.TermVector.YES);
				Assert.Fail();
			}
			catch (System.ArgumentException e)
			{
				// expected exception
			}
		}
		
		/// <summary> Tests {@link Document#GetValues(String)} method for a brand new Document
		/// that has not been indexed yet.
		/// 
		/// </summary>
		/// <throws>  Exception on error </throws>
		[Test]
        public virtual void  TestGetValuesForNewDocument()
		{
			DoAssert(MakeDocumentWithFields(), false);
		}
		
		/// <summary> Tests {@link Document#GetValues(String)} method for a Document retrieved from
		/// an index.
		/// 
		/// </summary>
		/// <throws>  Exception on error </throws>
		[Test]
        public virtual void  TestGetValuesForIndexedDocument()
		{
			RAMDirectory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(), true);
			writer.AddDocument(MakeDocumentWithFields());
			writer.Close();
			
			Searcher searcher = new IndexSearcher(dir);
			
			// search for something that does exists
			Query query = new TermQuery(new Term("keyword", "test1"));
			
			// ensure that queries return expected results without DateFilter first
			Hits hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length());
			
			DoAssert(hits.Doc(0), true);
			searcher.Close();
		}
		
		private Lucene.Net.Documents.Document MakeDocumentWithFields()
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("keyword", "test1", Field.Store.YES, Field.Index.UN_TOKENIZED));
			doc.Add(new Field("keyword", "test2", Field.Store.YES, Field.Index.UN_TOKENIZED));
			doc.Add(new Field("text", "test1", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("text", "test2", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("unindexed", "test1", Field.Store.YES, Field.Index.NO));
			doc.Add(new Field("unindexed", "test2", Field.Store.YES, Field.Index.NO));
			doc.Add(new Field("unstored", "test1", Field.Store.NO, Field.Index.TOKENIZED));
			doc.Add(new Field("unstored", "test2", Field.Store.NO, Field.Index.TOKENIZED));
			return doc;
		}
		
		private void  DoAssert(Lucene.Net.Documents.Document doc, bool fromIndex)
		{
			System.String[] keywordFieldValues = doc.GetValues("keyword");
			System.String[] textFieldValues = doc.GetValues("text");
			System.String[] unindexedFieldValues = doc.GetValues("unindexed");
			System.String[] unstoredFieldValues = doc.GetValues("unstored");
			
			Assert.IsTrue(keywordFieldValues.Length == 2);
			Assert.IsTrue(textFieldValues.Length == 2);
			Assert.IsTrue(unindexedFieldValues.Length == 2);
			// this test cannot work for documents retrieved from the index
			// since unstored fields will obviously not be returned
			if (!fromIndex)
			{
				Assert.IsTrue(unstoredFieldValues.Length == 2);
			}
			
			Assert.IsTrue(keywordFieldValues[0].Equals("test1"));
			Assert.IsTrue(keywordFieldValues[1].Equals("test2"));
			Assert.IsTrue(textFieldValues[0].Equals("test1"));
			Assert.IsTrue(textFieldValues[1].Equals("test2"));
			Assert.IsTrue(unindexedFieldValues[0].Equals("test1"));
			Assert.IsTrue(unindexedFieldValues[1].Equals("test2"));
			// this test cannot work for documents retrieved from the index
			// since unstored fields will obviously not be returned
			if (!fromIndex)
			{
				Assert.IsTrue(unstoredFieldValues[0].Equals("test1"));
				Assert.IsTrue(unstoredFieldValues[1].Equals("test2"));
			}
		}
	}
}