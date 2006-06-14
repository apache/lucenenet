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
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Index
{
	[TestFixture]
	public class TestIndexReader
	{
		/// <summary>Main for running test case by itself. </summary>
		[STAThread]
		public static void  Main(System.String[] args)
		{
			// NUnit.Core.TestRunner(new NUnit.Core.TestSuite(typeof(TestIndexReader)));  // {{Aroush}} where is 'TestRunner'?
			//        TestRunner.run (new TestIndexReader("testBasicDelete"));
			//        TestRunner.run (new TestIndexReader("testDeleteReaderWriterConflict"));
			//        TestRunner.run (new TestIndexReader("testDeleteReaderReaderConflict"));
			//        TestRunner.run (new TestIndexReader("testFilesOpenClose"));
		}
		
		public virtual void  TestIsCurrent()
		{
			RAMDirectory d = new RAMDirectory();
			IndexWriter writer = new IndexWriter(d, new StandardAnalyzer(), true);
			AddDocumentWithFields(writer);
			writer.Close();
			// set up reader:
			IndexReader reader = IndexReader.Open(d);
			Assert.IsTrue(reader.IsCurrent());
			// modify index by adding another document:
			writer = new IndexWriter(d, new StandardAnalyzer(), false);
			AddDocumentWithFields(writer);
			writer.Close();
			Assert.IsFalse(reader.IsCurrent());
			// re-create index:
			writer = new IndexWriter(d, new StandardAnalyzer(), true);
			AddDocumentWithFields(writer);
			writer.Close();
			Assert.IsFalse(reader.IsCurrent());
			reader.Close();
		}
		
		/// <summary> Tests the IndexReader.getFieldNames implementation</summary>
		/// <throws>  Exception on error </throws>
		public virtual void  TestGetFieldNames()
		{
			RAMDirectory d = new RAMDirectory();
			// set up writer
			IndexWriter writer = new IndexWriter(d, new StandardAnalyzer(), true);
			AddDocumentWithFields(writer);
			writer.Close();
			// set up reader
			IndexReader reader = IndexReader.Open(d);
			System.Collections.ICollection fieldNames = reader.GetFieldNames(IndexReader.FieldOption.ALL);
			Assert.IsTrue(CollectionContains(fieldNames, "keyword"));
			Assert.IsTrue(CollectionContains(fieldNames, "text"));
			Assert.IsTrue(CollectionContains(fieldNames, "unindexed"));
			Assert.IsTrue(CollectionContains(fieldNames, "unstored"));
			// add more documents
			writer = new IndexWriter(d, new StandardAnalyzer(), false);
			// want to get some more segments here
			for (int i = 0; i < 5 * writer.GetMergeFactor(); i++)
			{
				AddDocumentWithFields(writer);
			}
			// new fields are in some different segments (we hope)
			for (int i = 0; i < 5 * writer.GetMergeFactor(); i++)
			{
				AddDocumentWithDifferentFields(writer);
			}
			// new termvector fields
			for (int i = 0; i < 5 * writer.GetMergeFactor(); i++)
			{
				AddDocumentWithTermVectorFields(writer);
			}
			
			writer.Close();
			// verify fields again
			reader = IndexReader.Open(d);
			fieldNames = reader.GetFieldNames(IndexReader.FieldOption.ALL);
			Assert.AreEqual(13, fieldNames.Count); // the following fields
			Assert.IsTrue(CollectionContains(fieldNames, "keyword"));
			Assert.IsTrue(CollectionContains(fieldNames, "text"));
			Assert.IsTrue(CollectionContains(fieldNames, "unindexed"));
			Assert.IsTrue(CollectionContains(fieldNames, "unstored"));
			Assert.IsTrue(CollectionContains(fieldNames, "keyword2"));
			Assert.IsTrue(CollectionContains(fieldNames, "text2"));
			Assert.IsTrue(CollectionContains(fieldNames, "unindexed2"));
			Assert.IsTrue(CollectionContains(fieldNames, "unstored2"));
			Assert.IsTrue(CollectionContains(fieldNames, "tvnot"));
			Assert.IsTrue(CollectionContains(fieldNames, "termvector"));
			Assert.IsTrue(CollectionContains(fieldNames, "tvposition"));
			Assert.IsTrue(CollectionContains(fieldNames, "tvoffset"));
			Assert.IsTrue(CollectionContains(fieldNames, "tvpositionoffset"));
			
			// verify that only indexed fields were returned
			fieldNames = reader.GetFieldNames(IndexReader.FieldOption.INDEXED);
			Assert.AreEqual(11, fieldNames.Count); // 6 original + the 5 termvector fields 
			Assert.IsTrue(CollectionContains(fieldNames, "keyword"));
			Assert.IsTrue(CollectionContains(fieldNames, "text"));
			Assert.IsTrue(CollectionContains(fieldNames, "unstored"));
			Assert.IsTrue(CollectionContains(fieldNames, "keyword2"));
			Assert.IsTrue(CollectionContains(fieldNames, "text2"));
			Assert.IsTrue(CollectionContains(fieldNames, "unstored2"));
			Assert.IsTrue(CollectionContains(fieldNames, "tvnot"));
			Assert.IsTrue(CollectionContains(fieldNames, "termvector"));
			Assert.IsTrue(CollectionContains(fieldNames, "tvposition"));
			Assert.IsTrue(CollectionContains(fieldNames, "tvoffset"));
			Assert.IsTrue(CollectionContains(fieldNames, "tvpositionoffset"));
			
			// verify that only unindexed fields were returned
			fieldNames = reader.GetFieldNames(IndexReader.FieldOption.UNINDEXED);
			Assert.AreEqual(2, fieldNames.Count); // the following fields
			Assert.IsTrue(CollectionContains(fieldNames, "unindexed"));
			Assert.IsTrue(CollectionContains(fieldNames, "unindexed2"));
			
			// verify index term vector fields  
			fieldNames = reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR);
			Assert.AreEqual(1, fieldNames.Count); // 1 field has term vector only
			Assert.IsTrue(CollectionContains(fieldNames, "termvector"));
			
			fieldNames = reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR_WITH_POSITION);
			Assert.AreEqual(1, fieldNames.Count); // 4 fields are indexed with term vectors
			Assert.IsTrue(CollectionContains(fieldNames, "tvposition"));
			
			fieldNames = reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR_WITH_OFFSET);
			Assert.AreEqual(1, fieldNames.Count); // 4 fields are indexed with term vectors
			Assert.IsTrue(CollectionContains(fieldNames, "tvoffset"));
			
			fieldNames = reader.GetFieldNames(IndexReader.FieldOption.TERMVECTOR_WITH_POSITION_OFFSET);
			Assert.AreEqual(1, fieldNames.Count); // 4 fields are indexed with term vectors
			Assert.IsTrue(CollectionContains(fieldNames, "tvpositionoffset"));
		}

        public static bool CollectionContains(System.Collections.ICollection col, System.String val)
        {
            for (System.Collections.IEnumerator iterator = col.GetEnumerator(); iterator.MoveNext(); )
            {
                System.Collections.DictionaryEntry fi = (System.Collections.DictionaryEntry) iterator.Current;
                System.String s = fi.Key.ToString();
                if (s == val)
                    return true;
            }
            return false;
        }
		
		private void  AssertTermDocsCount(System.String msg, IndexReader reader, Term term, int expected)
		{
			TermDocs tdocs = null;
			
			try
			{
				tdocs = reader.TermDocs(term);
				Assert.IsNotNull(tdocs, msg + ", null TermDocs");
				int count = 0;
				while (tdocs.Next())
				{
					count++;
				}
				Assert.AreEqual(expected, count, msg + ", count mismatch");
			}
			finally
			{
				if (tdocs != null)
					tdocs.Close();
			}
		}
		
		
		[Test]
		public virtual void  TestBasicDelete()
		{
			Directory dir = new RAMDirectory();
			
			IndexWriter writer = null;
			IndexReader reader = null;
			Term searchTerm = new Term("content", "aaa");
			
			//  add 100 documents with term : aaa
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer, searchTerm.Text());
			}
			writer.Close();
			
			// OPEN READER AT THIS POINT - this should fix the view of the
			// index at the point of having 100 "aaa" documents and 0 "bbb"
			reader = IndexReader.Open(dir);
			Assert.AreEqual(100, reader.DocFreq(searchTerm), "first docFreq");
			AssertTermDocsCount("first reader", reader, searchTerm, 100);
			
			// DELETE DOCUMENTS CONTAINING TERM: aaa
			int deleted = 0;
			reader = IndexReader.Open(dir);
			deleted = reader.Delete(searchTerm);
			Assert.AreEqual(100, deleted, "deleted count");
			Assert.AreEqual(100, reader.DocFreq(searchTerm), "deleted docFreq");
			AssertTermDocsCount("deleted termDocs", reader, searchTerm, 0);
			reader.Close();
			
			// CREATE A NEW READER and re-test
			reader = IndexReader.Open(dir);
			Assert.AreEqual(100, reader.DocFreq(searchTerm), "deleted docFreq");
			AssertTermDocsCount("deleted termDocs", reader, searchTerm, 0);
			reader.Close();
		}
		
		[Test]
		public virtual void  TestDeleteReaderWriterConflictUnoptimized()
		{
			DeleteReaderWriterConflict(false);
		}
		
		[Test]
        public virtual void  TestDeleteReaderWriterConflictOptimized()
		{
			DeleteReaderWriterConflict(true);
		}
		
		private void  DeleteReaderWriterConflict(bool optimize)
		{
			//Directory dir = new RAMDirectory();
			Directory dir = GetDirectory(true);
			
			Term searchTerm = new Term("content", "aaa");
			Term searchTerm2 = new Term("content", "bbb");
			
			//  add 100 documents with term : aaa
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer, searchTerm.Text());
			}
			writer.Close();
			
			// OPEN READER AT THIS POINT - this should fix the view of the
			// index at the point of having 100 "aaa" documents and 0 "bbb"
			IndexReader reader = IndexReader.Open(dir);
			Assert.AreEqual(100, reader.DocFreq(searchTerm), "first docFreq");
			Assert.AreEqual(0, reader.DocFreq(searchTerm2), "first docFreq");
			AssertTermDocsCount("first reader", reader, searchTerm, 100);
			AssertTermDocsCount("first reader", reader, searchTerm2, 0);
			
			// add 100 documents with term : bbb
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer, searchTerm2.Text());
			}
			
			// REQUEST OPTIMIZATION
			// This causes a new segment to become current for all subsequent
			// searchers. Because of this, deletions made via a previously open
			// reader, which would be applied to that reader's segment, are lost
			// for subsequent searchers/readers
			if (optimize)
				writer.Optimize();
			writer.Close();
			
			// The reader should not see the new data
			Assert.AreEqual(100, reader.DocFreq(searchTerm), "first docFreq");
			Assert.AreEqual(0, reader.DocFreq(searchTerm2), "first docFreq");
			AssertTermDocsCount("first reader", reader, searchTerm, 100);
			AssertTermDocsCount("first reader", reader, searchTerm2, 0);
			
			
			// DELETE DOCUMENTS CONTAINING TERM: aaa
			// NOTE: the reader was created when only "aaa" documents were in
			int deleted = 0;
			try
			{
				deleted = reader.Delete(searchTerm);
				Assert.Fail("Delete allowed on an index reader with stale segment information");
			}
			catch (System.IO.IOException e)
			{
				/* success */
			}
			
			// Re-open index reader and try again. This time it should see
			// the new data.
			reader.Close();
			reader = IndexReader.Open(dir);
			Assert.AreEqual(100, reader.DocFreq(searchTerm), "first docFreq");
			Assert.AreEqual(100, reader.DocFreq(searchTerm2), "first docFreq");
			AssertTermDocsCount("first reader", reader, searchTerm, 100);
			AssertTermDocsCount("first reader", reader, searchTerm2, 100);
			
			deleted = reader.Delete(searchTerm);
			Assert.AreEqual(100, deleted, "deleted count");
			Assert.AreEqual(100, reader.DocFreq(searchTerm), "deleted docFreq");
			Assert.AreEqual(100, reader.DocFreq(searchTerm2), "deleted docFreq");
			AssertTermDocsCount("deleted termDocs", reader, searchTerm, 0);
			AssertTermDocsCount("deleted termDocs", reader, searchTerm2, 100);
			reader.Close();
			
			// CREATE A NEW READER and re-test
			reader = IndexReader.Open(dir);
			Assert.AreEqual(100, reader.DocFreq(searchTerm), "deleted docFreq");
			Assert.AreEqual(100, reader.DocFreq(searchTerm2), "deleted docFreq");
			AssertTermDocsCount("deleted termDocs", reader, searchTerm, 0);
			AssertTermDocsCount("deleted termDocs", reader, searchTerm2, 100);
			reader.Close();
		}
		
		private Directory GetDirectory(bool create)
		{
			return FSDirectory.GetDirectory(System.IO.Path.Combine(SupportClass.AppSettings.Get("tempDir", ""), "testIndex"), create);
		}
		
		[Test]
        public virtual void  TestFilesOpenClose()
		{
			// Create initial data set
			Directory dir = GetDirectory(true);
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			AddDoc(writer, "test");
			writer.Close();
			dir.Close();
			
			// Try to erase the data - this ensures that the writer closed all files
			dir = GetDirectory(true);
			
			// Now create the data set again, just as before
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			AddDoc(writer, "test");
			writer.Close();
			dir.Close();
			
			// Now open existing directory and test that reader closes all files
			dir = GetDirectory(false);
			IndexReader reader1 = IndexReader.Open(dir);
			reader1.Close();
			dir.Close();
			
			// The following will fail if reader did not close all files
			dir = GetDirectory(true);
		}
		
		[Test]
        public virtual void  TestLastModified()
		{
			Assert.IsFalse(IndexReader.IndexExists("there_is_no_such_index"));
			Directory dir = new RAMDirectory();
			Assert.IsFalse(IndexReader.IndexExists(dir));
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			AddDocumentWithFields(writer);
			Assert.IsTrue(IndexReader.IsLocked(dir)); // writer open, so dir is locked
			writer.Close();
			Assert.IsTrue(IndexReader.IndexExists(dir));
			IndexReader reader = IndexReader.Open(dir);
			Assert.IsFalse(IndexReader.IsLocked(dir)); // reader only, no lock
			long version = IndexReader.LastModified(dir);
			reader.Close();
			// modify index and check version has been incremented:
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			AddDocumentWithFields(writer);
			writer.Close();
			reader = IndexReader.Open(dir);
			Assert.IsTrue(version < IndexReader.GetCurrentVersion(dir));
			reader.Close();
		}
		
		[Test]
        public virtual void  TestLock()
		{
			Directory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			AddDocumentWithFields(writer);
			writer.Close();
			writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
			IndexReader reader = IndexReader.Open(dir);
			try
			{
				reader.Delete(0);
				Assert.Fail("expected lock");
			}
			catch (System.IO.IOException e)
			{
				// expected exception
			}
			IndexReader.Unlock(dir); // this should not be done in the real world! 
			reader.Delete(0);
			reader.Close();
			writer.Close();
		}
		
		[Test]
        public virtual void  TestUndeleteAll()
		{
			Directory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			AddDocumentWithFields(writer);
			AddDocumentWithFields(writer);
			writer.Close();
			IndexReader reader = IndexReader.Open(dir);
			reader.Delete(0);
			reader.Delete(1);
			reader.UndeleteAll();
			reader.Close();
			reader = IndexReader.Open(dir);
			Assert.AreEqual(2, reader.NumDocs()); // nothing has really been deleted thanks to undeleteAll()
			reader.Close();
		}
		
		[Test]
        public virtual void  TestDeleteReaderReaderConflictUnoptimized()
		{
			DeleteReaderReaderConflict(false);
		}
		
		[Test]
        public virtual void  TestDeleteReaderReaderConflictOptimized()
		{
			DeleteReaderReaderConflict(true);
		}
		
		private void  DeleteReaderReaderConflict(bool optimize)
		{
			Directory dir = GetDirectory(true);
			
			Term searchTerm1 = new Term("content", "aaa");
			Term searchTerm2 = new Term("content", "bbb");
			Term searchTerm3 = new Term("content", "ccc");
			
			//  add 100 documents with term : aaa
			//  add 100 documents with term : bbb
			//  add 100 documents with term : ccc
			IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer, searchTerm1.Text());
				AddDoc(writer, searchTerm2.Text());
				AddDoc(writer, searchTerm3.Text());
			}
			if (optimize)
				writer.Optimize();
			writer.Close();
			
			// OPEN TWO READERS
			// Both readers get segment info as exists at this time
			IndexReader reader1 = IndexReader.Open(dir);
			Assert.AreEqual(100, reader1.DocFreq(searchTerm1), "first opened");
			Assert.AreEqual(100, reader1.DocFreq(searchTerm2), "first opened");
			Assert.AreEqual(100, reader1.DocFreq(searchTerm3), "first opened");
			AssertTermDocsCount("first opened", reader1, searchTerm1, 100);
			AssertTermDocsCount("first opened", reader1, searchTerm2, 100);
			AssertTermDocsCount("first opened", reader1, searchTerm3, 100);
			
			IndexReader reader2 = IndexReader.Open(dir);
			Assert.AreEqual(100, reader2.DocFreq(searchTerm1), "first opened");
			Assert.AreEqual(100, reader2.DocFreq(searchTerm2), "first opened");
			Assert.AreEqual(100, reader2.DocFreq(searchTerm3), "first opened");
			AssertTermDocsCount("first opened", reader2, searchTerm1, 100);
			AssertTermDocsCount("first opened", reader2, searchTerm2, 100);
			AssertTermDocsCount("first opened", reader2, searchTerm3, 100);
			
			// DELETE DOCS FROM READER 2 and CLOSE IT
			// delete documents containing term: aaa
			// when the reader is closed, the segment info is updated and
			// the first reader is now stale
			reader2.Delete(searchTerm1);
			Assert.AreEqual(100, reader2.DocFreq(searchTerm1), "after delete 1");
			Assert.AreEqual(100, reader2.DocFreq(searchTerm2), "after delete 1");
			Assert.AreEqual(100, reader2.DocFreq(searchTerm3), "after delete 1");
			AssertTermDocsCount("after delete 1", reader2, searchTerm1, 0);
			AssertTermDocsCount("after delete 1", reader2, searchTerm2, 100);
			AssertTermDocsCount("after delete 1", reader2, searchTerm3, 100);
			reader2.Close();
			
			// Make sure reader 1 is unchanged since it was open earlier
			Assert.AreEqual(100, reader1.DocFreq(searchTerm1), "after delete 1");
			Assert.AreEqual(100, reader1.DocFreq(searchTerm2), "after delete 1");
			Assert.AreEqual(100, reader1.DocFreq(searchTerm3), "after delete 1");
			AssertTermDocsCount("after delete 1", reader1, searchTerm1, 100);
			AssertTermDocsCount("after delete 1", reader1, searchTerm2, 100);
			AssertTermDocsCount("after delete 1", reader1, searchTerm3, 100);
			
			
			// ATTEMPT TO DELETE FROM STALE READER
			// delete documents containing term: bbb
			try
			{
				reader1.Delete(searchTerm2);
				Assert.Fail("Delete allowed from a stale index reader");
			}
			catch (System.IO.IOException e)
			{
				/* success */
			}
			
			// RECREATE READER AND TRY AGAIN
			reader1.Close();
			reader1 = IndexReader.Open(dir);
			Assert.AreEqual(100, reader1.DocFreq(searchTerm1), "reopened");
			Assert.AreEqual(100, reader1.DocFreq(searchTerm2), "reopened");
			Assert.AreEqual(100, reader1.DocFreq(searchTerm3), "reopened");
			AssertTermDocsCount("reopened", reader1, searchTerm1, 0);
			AssertTermDocsCount("reopened", reader1, searchTerm2, 100);
			AssertTermDocsCount("reopened", reader1, searchTerm3, 100);
			
			reader1.Delete(searchTerm2);
			Assert.AreEqual(100, reader1.DocFreq(searchTerm1), "deleted 2");
			Assert.AreEqual(100, reader1.DocFreq(searchTerm2), "deleted 2");
			Assert.AreEqual(100, reader1.DocFreq(searchTerm3), "deleted 2");
			AssertTermDocsCount("deleted 2", reader1, searchTerm1, 0);
			AssertTermDocsCount("deleted 2", reader1, searchTerm2, 0);
			AssertTermDocsCount("deleted 2", reader1, searchTerm3, 100);
			reader1.Close();
			
			// Open another reader to confirm that everything is deleted
			reader2 = IndexReader.Open(dir);
			Assert.AreEqual(100, reader2.DocFreq(searchTerm1), "reopened 2");
			Assert.AreEqual(100, reader2.DocFreq(searchTerm2), "reopened 2");
			Assert.AreEqual(100, reader2.DocFreq(searchTerm3), "reopened 2");
			AssertTermDocsCount("reopened 2", reader2, searchTerm1, 0);
			AssertTermDocsCount("reopened 2", reader2, searchTerm2, 0);
			AssertTermDocsCount("reopened 2", reader2, searchTerm3, 100);
			reader2.Close();
			
			dir.Close();
		}
		
		
		private void  AddDocumentWithFields(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("keyword", "test1", Field.Store.YES, Field.Index.UN_TOKENIZED));
			doc.Add(new Field("text", "test1", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("unindexed", "test1", Field.Store.YES, Field.Index.NO));
			doc.Add(new Field("unstored", "test1", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
		}
		
		private void  AddDocumentWithDifferentFields(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("keyword2", "test1", Field.Store.YES, Field.Index.UN_TOKENIZED));
			doc.Add(new Field("text2", "test1", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("unindexed2", "test1", Field.Store.YES, Field.Index.NO));
			doc.Add(new Field("unstored2", "test1", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
		}
		
		private void  AddDocumentWithTermVectorFields(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("tvnot", "tvnot", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.NO));
			doc.Add(new Field("termvector", "termvector", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.YES));
			doc.Add(new Field("tvoffset", "tvoffset", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_OFFSETS));
			doc.Add(new Field("tvposition", "tvposition", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS));
			doc.Add(new Field("tvpositionoffset", "tvpositionoffset", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
			
			writer.AddDocument(doc);
		}
		
		private void  AddDoc(IndexWriter writer, System.String value_Renamed)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("content", value_Renamed, Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
		}
	}
}