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
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Documents
{	
	/// <summary>Tests {@link Document} class.</summary>	
	public class TestDocument:LuceneTestCase
	{		
		internal System.String binaryVal = "this text will be stored as a byte array in the index";
		internal System.String binaryVal2 = "this text will be also stored as a byte array in the index";

	    [Test]
	    public void testBinaryField()
	    {
	        Document doc = new Document();

	        FieldType ft = new FieldType();
	        ft.Stored = true;
	        IIndexableField stringFld = new Field("string", binaryVal, ft);
	        IIndexableField binaryFld = new StoredField("binary", binaryVal.getBytes("UTF-8"));
	        IIndexableField binaryFld2 = new StoredField("binary", binaryVal2.getBytes("UTF-8"));

	        doc.Add(stringFld);
	        doc.Add(binaryFld);

	        assertEquals(2, doc.GetFields().Count);

	        assertTrue(binaryFld.BinaryValue != null);
	        assertTrue(binaryFld.FieldTypeValue.Stored);
	        assertFalse(binaryFld.FieldTypeValue.Indexed);

	        String binaryTest = doc.GetBinaryValue("binary").Utf8ToString();
	        assertTrue(binaryTest.equals(binaryVal));

	        String stringTest = doc.Get("string");
	        assertTrue(binaryTest.equals(stringTest));

	        doc.Add(binaryFld2);

	        assertEquals(3, doc.GetFields().Count);

	        BytesRef[] binaryTests = doc.GetBinaryValues("binary");

	        assertEquals(2, binaryTests.Length);

	        binaryTest = binaryTests[0].Utf8ToString();
	        String binaryTest2 = binaryTests[1].Utf8ToString();

	        assertFalse(binaryTest.equals(binaryTest2));

	        assertTrue(binaryTest.equals(binaryVal));
	        assertTrue(binaryTest2.equals(binaryVal2));

	        doc.RemoveField("string");
	        assertEquals(2, doc.GetFields().Count);

	        doc.RemoveFields("binary");
	        assertEquals(0, doc.GetFields().Count);
	    }

	    /// <summary> Tests {@link Document#RemoveField(String)} method for a brand new Document
	    /// that has not been indexed yet.
	    /// 
	    /// </summary>
	    /// <throws>  Exception on error </throws>
	    [Test]
	    public void testRemoveForNewDocument()
	    {
	        Document doc = makeDocumentWithFields();
	        assertEquals(8, doc.GetFields().size());
	        doc.RemoveFields("keyword");
	        assertEquals(6, doc.GetFields().size());
	        doc.RemoveFields("doesnotexists"); // removing non-existing fields is
	        // siltenlty ignored
	        doc.RemoveFields("keyword"); // removing a field more than once
	        assertEquals(6, doc.GetFields().size());
	        doc.RemoveField("text");
	        assertEquals(5, doc.GetFields().size());
	        doc.RemoveField("text");
	        assertEquals(4, doc.GetFields().size());
	        doc.RemoveField("text");
	        assertEquals(4, doc.GetFields().size());
	        doc.RemoveField("doesnotexists"); // removing non-existing fields is
	        // siltenlty ignored
	        assertEquals(4, doc.GetFields().size());
	        doc.RemoveFields("unindexed");
	        assertEquals(2, doc.GetFields().size());
	        doc.RemoveFields("unstored");
	        assertEquals(0, doc.GetFields().size());
	        doc.RemoveFields("doesnotexists"); // removing non-existing fields is
	        // siltenlty ignored
	        assertEquals(0, doc.GetFields().size());
	    }

	    [Test]
        public void testConstructorExceptions()
        {
            FieldType ft = new FieldType();
            ft.Stored = true;
            new Field("name", "value", ft); // okay
            new StringField("name", "value", Field.Store.NO); // okay
            try
            {
                new Field("name", "value", new FieldType());
                fail();
            }
            catch (ArgumentException e)
            {
                // expected exception
            }
            new Field("name", "value", ft); // okay
            try
            {
                FieldType ft2 = new FieldType();
                ft2.Stored = true;
                ft2.StoreTermVectors = true;
                new Field("name", "value", ft2);
                fail();
            }
            catch (ArgumentException e)
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
		public virtual void testGetValuesForNewDocument()
		{
			doAssert(makeDocumentWithFields(), false);
		}

	    /// <summary> Tests {@link Document#GetValues(String)} method for a Document retrieved from
	    /// an index.
	    /// 
	    /// </summary>
	    /// <throws>  Exception on error </throws>
//	    [Test]
//	    public void testGetValuesForIndexedDocument()
//	    {
//	        Directory dir = newDirectory();
//	        RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
//	        writer.addDocument(makeDocumentWithFields());
//	        IndexReader reader = writer.getReader();
//
//	        IndexSearcher searcher = newSearcher(reader);
//
//	        // search for something that does exists
//	        Query query = new TermQuery(new Term("keyword", "test1"));
//
//	        // ensure that queries return expected results without DateFilter first
//	        ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
//	        assertEquals(1, hits.length);
//
//	        doAssert(searcher.doc(hits[0].doc), true);
//	        writer.close();
//	        reader.close();
//	        dir.close();
//	    }

	    private Document makeDocumentWithFields()
		{
            Document doc = new Document();
            FieldType stored = new FieldType();
            stored.Stored = true;
            doc.Add(new StringField("keyword", "test1", Field.Store.YES));
            doc.Add(new StringField("keyword", "test2", Field.Store.YES));
            doc.Add(new TextField("text", "test1", Field.Store.YES));
            doc.Add(new TextField("text", "test2", Field.Store.YES));
            doc.Add(new Field("unindexed", "test1", stored));
            doc.Add(new Field("unindexed", "test2", stored));
            doc
                .Add(new TextField("unstored", "test1", Field.Store.NO));
            doc
                .Add(new TextField("unstored", "test2", Field.Store.NO));
            return doc;
		}

        private void doAssert(Document doc, bool fromIndex)
        {
            IIndexableField[] keywordFieldValues = doc.GetFields("keyword");
            IIndexableField[] textFieldValues = doc.GetFields("text");
            IIndexableField[] unindexedFieldValues = doc.GetFields("unindexed");
            IIndexableField[] unstoredFieldValues = doc.GetFields("unstored");

            assertTrue(keywordFieldValues.Length == 2);
            assertTrue(textFieldValues.Length == 2);
            assertTrue(unindexedFieldValues.Length == 2);
            // this test cannot work for documents retrieved from the index
            // since unstored fields will obviously not be returned
            if (!fromIndex)
            {
                assertTrue(unstoredFieldValues.Length == 2);
            }

            assertTrue(keywordFieldValues[0].StringValue.equals("test1"));
            assertTrue(keywordFieldValues[1].StringValue.equals("test2"));
            assertTrue(textFieldValues[0].StringValue.equals("test1"));
            assertTrue(textFieldValues[1].StringValue.equals("test2"));
            assertTrue(unindexedFieldValues[0].StringValue.equals("test1"));
            assertTrue(unindexedFieldValues[1].StringValue.equals("test2"));
            // this test cannot work for documents retrieved from the index
            // since unstored fields will obviously not be returned
            if (!fromIndex)
            {
                assertTrue(unstoredFieldValues[0].StringValue.equals("test1"));
                assertTrue(unstoredFieldValues[1].StringValue.equals("test2"));
            }
        }

//	    [Test]
//	    public void testFieldSetValue()
//	    {
//
//	        Field field = new StringField("id", "id1", Field.Store.YES);
//	        Document doc = new Document();
//	        doc.Add(field);
//	        doc.Add(new StringField("keyword", "test", Field.Store.YES));
//
//	        Directory dir = newDirectory();
//	        RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
//	        writer.addDocument(doc);
//	        field.setStringValue("id2");
//	        writer.addDocument(doc);
//	        field.setStringValue("id3");
//	        writer.addDocument(doc);
//
//	        IndexReader reader = writer.getReader();
//	        IndexSearcher searcher = newSearcher(reader);
//
//	        Query query = new TermQuery(new Term("keyword", "test"));
//
//	        // ensure that queries return expected results without DateFilter first
//	        ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
//	        assertEquals(3, hits.length);
//	        int result = 0;
//	        for (int i = 0; i < 3; i++)
//	        {
//	            Document doc2 = searcher.doc(hits[i].doc);
//	            Field f = (Field) doc2.getField("id");
//	            if (f.stringValue().equals("id1")) result |= 1;
//	            else if (f.stringValue().equals("id2")) result |= 2;
//	            else if (f.stringValue().equals("id3")) result |= 4;
//	            else fail("unexpected id field");
//	        }
//	        writer.close();
//	        reader.close();
//	        dir.close();
//	        assertEquals("did not see all IDs", 7, result);
//	    }

	}
}