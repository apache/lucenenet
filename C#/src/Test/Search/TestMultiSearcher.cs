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
using KeywordAnalyzer = Lucene.Net.Analysis.KeywordAnalyzer;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using NUnit.Framework;

namespace Lucene.Net.Search
{
	
	/// <summary> Tests {@link MultiSearcher} class.
	/// 
	/// </summary>
	/// <version>  $Id: TestMultiSearcher.java 354819 2005-12-07 17:48:37Z yonik $
	/// </version>
	[TestFixture]
    public class TestMultiSearcher
	{
		
		/// <summary> ReturnS a new instance of the concrete MultiSearcher class
		/// used in this test.
		/// </summary>
		protected internal virtual MultiSearcher GetMultiSearcherInstance(Searcher[] searchers)
		{
			return new MultiSearcher(searchers);
		}
		
		[Test]
        public virtual void  TestEmptyIndex()
		{
			// creating two directories for indices
			Directory indexStoreA = new RAMDirectory();
			Directory indexStoreB = new RAMDirectory();
			
			// creating a document to store
			Lucene.Net.Documents.Document lDoc = new Lucene.Net.Documents.Document();
			lDoc.Add(new Field("fulltext", "Once upon a time.....", Field.Store.YES, Field.Index.TOKENIZED));
			lDoc.Add(new Field("id", "doc1", Field.Store.YES, Field.Index.UN_TOKENIZED));
			lDoc.Add(new Field("handle", "1", Field.Store.YES, Field.Index.UN_TOKENIZED));
			
			// creating a document to store
			Lucene.Net.Documents.Document lDoc2 = new Lucene.Net.Documents.Document();
			lDoc2.Add(new Field("fulltext", "in a galaxy far far away.....", Field.Store.YES, Field.Index.TOKENIZED));
			lDoc2.Add(new Field("id", "doc2", Field.Store.YES, Field.Index.UN_TOKENIZED));
			lDoc2.Add(new Field("handle", "1", Field.Store.YES, Field.Index.UN_TOKENIZED));
			
			// creating a document to store
			Lucene.Net.Documents.Document lDoc3 = new Lucene.Net.Documents.Document();
			lDoc3.Add(new Field("fulltext", "a bizarre bug manifested itself....", Field.Store.YES, Field.Index.TOKENIZED));
			lDoc3.Add(new Field("id", "doc3", Field.Store.YES, Field.Index.UN_TOKENIZED));
			lDoc3.Add(new Field("handle", "1", Field.Store.YES, Field.Index.UN_TOKENIZED));
			
			// creating an index writer for the first index
			IndexWriter writerA = new IndexWriter(indexStoreA, new StandardAnalyzer(), true);
			// creating an index writer for the second index, but writing nothing
			IndexWriter writerB = new IndexWriter(indexStoreB, new StandardAnalyzer(), true);
			
			//--------------------------------------------------------------------
			// scenario 1
			//--------------------------------------------------------------------
			
			// writing the documents to the first index
			writerA.AddDocument(lDoc);
			writerA.AddDocument(lDoc2);
			writerA.AddDocument(lDoc3);
			writerA.Optimize();
			writerA.Close();
			
			// closing the second index
			writerB.Close();
			
			// creating the query
            Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser("fulltext", new StandardAnalyzer());
            Query query = parser.Parse("handle:1");
			
			// building the searchables
			Searcher[] searchers = new Searcher[2];
			// VITAL STEP:adding the searcher for the empty index first, before the searcher for the populated index
			searchers[0] = new IndexSearcher(indexStoreB);
			searchers[1] = new IndexSearcher(indexStoreA);
			// creating the multiSearcher
			Searcher mSearcher = GetMultiSearcherInstance(searchers);
			// performing the search
			Hits hits = mSearcher.Search(query);
			
			Assert.AreEqual(3, hits.Length());
			
			// iterating over the hit documents
			for (int i = 0; i < hits.Length(); i++)
			{
				Lucene.Net.Documents.Document d = hits.Doc(i);
			}
			mSearcher.Close();
			
			
			//--------------------------------------------------------------------
			// scenario 2
			//--------------------------------------------------------------------
			
			// adding one document to the empty index
			writerB = new IndexWriter(indexStoreB, new StandardAnalyzer(), false);
			writerB.AddDocument(lDoc);
			writerB.Optimize();
			writerB.Close();
			
			// building the searchables
			Searcher[] searchers2 = new Searcher[2];
			// VITAL STEP:adding the searcher for the empty index first, before the searcher for the populated index
			searchers2[0] = new IndexSearcher(indexStoreB);
			searchers2[1] = new IndexSearcher(indexStoreA);
			// creating the mulitSearcher
			Searcher mSearcher2 = GetMultiSearcherInstance(searchers2);
			// performing the same search
			Hits hits2 = mSearcher2.Search(query);
			
			Assert.AreEqual(4, hits2.Length());
			
			// iterating over the hit documents
			for (int i = 0; i < hits2.Length(); i++)
			{
				// no exception should happen at this point
				Lucene.Net.Documents.Document d = hits2.Doc(i);
			}
			mSearcher2.Close();
			
			//--------------------------------------------------------------------
			// scenario 3
			//--------------------------------------------------------------------
			
			// deleting the document just added, this will cause a different exception to take place
			Term term = new Term("id", "doc1");
			IndexReader readerB = IndexReader.Open(indexStoreB);
			readerB.DeleteDocuments(term);
			readerB.Close();
			
			// optimizing the index with the writer
			writerB = new IndexWriter(indexStoreB, new StandardAnalyzer(), false);
			writerB.Optimize();
			writerB.Close();
			
			// building the searchables
			Searcher[] searchers3 = new Searcher[2];
			
			searchers3[0] = new IndexSearcher(indexStoreB);
			searchers3[1] = new IndexSearcher(indexStoreA);
			// creating the mulitSearcher
			Searcher mSearcher3 = GetMultiSearcherInstance(searchers3);
			// performing the same search
			Hits hits3 = mSearcher3.Search(query);
			
			Assert.AreEqual(3, hits3.Length());
			
			// iterating over the hit documents
			for (int i = 0; i < hits3.Length(); i++)
			{
				Lucene.Net.Documents.Document d = hits3.Doc(i);
			}
			mSearcher3.Close();
		}
		
		private static Lucene.Net.Documents.Document CreateDocument(System.String contents1, System.String contents2)
		{
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document();
			
			document.Add(new Field("contents", contents1, Field.Store.YES, Field.Index.UN_TOKENIZED));
			
			if (contents2 != null)
			{
				document.Add(new Field("contents", contents2, Field.Store.YES, Field.Index.UN_TOKENIZED));
			}
			
			return document;
		}
		
		private static void  InitIndex(Directory directory, int nDocs, bool create, System.String contents2)
		{
			IndexWriter indexWriter = null;
			
			try
			{
				indexWriter = new IndexWriter(directory, new KeywordAnalyzer(), create);
				
				for (int i = 0; i < nDocs; i++)
				{
					indexWriter.AddDocument(CreateDocument("doc" + i, contents2));
				}
			}
			finally
			{
				if (indexWriter != null)
				{
					indexWriter.Close();
				}
			}
		}
		
		/* uncomment this when the highest score is always normalized to 1.0, even when it was < 1.0
		public void testNormalization1() throws IOException {
		testNormalization(1, "Using 1 document per index:");
		}
		*/
		
		[Test]
        public virtual void  TestNormalization10()
		{
			_TestNormalization(10, "Using 10 documents per index:");
		}
		
        private void  _TestNormalization(int nDocs, System.String message)
		{
			Query query = new TermQuery(new Term("contents", "doc0"));
			
			RAMDirectory ramDirectory1;
			IndexSearcher indexSearcher1;
			Hits hits;
			
			ramDirectory1 = new RAMDirectory();
			
			// First put the documents in the same index
			InitIndex(ramDirectory1, nDocs, true, null); // documents with a single token "doc0", "doc1", etc...
			InitIndex(ramDirectory1, nDocs, false, "x"); // documents with two tokens "doc0" and "x", "doc1" and x, etc...
			
			indexSearcher1 = new IndexSearcher(ramDirectory1);
			
			hits = indexSearcher1.Search(query);
			
			Assert.AreEqual(2, hits.Length(), message);
			
			Assert.AreEqual(1, hits.Score(0), 1e-6, message); // hits.score(0) is 0.594535 if only a single document is in first index
			
			// Store the scores for use later
			float[] scores = new float[]{hits.Score(0), hits.Score(1)};
			
			Assert.IsTrue(scores[0] > scores[1], message);
			
			indexSearcher1.Close();
			ramDirectory1.Close();
			hits = null;
			
			
			
			RAMDirectory ramDirectory2;
			IndexSearcher indexSearcher2;
			
			ramDirectory1 = new RAMDirectory();
			ramDirectory2 = new RAMDirectory();
			
			// Now put the documents in a different index
			InitIndex(ramDirectory1, nDocs, true, null); // documents with a single token "doc0", "doc1", etc...
			InitIndex(ramDirectory2, nDocs, true, "x"); // documents with two tokens "doc0" and "x", "doc1" and x, etc...
			
			indexSearcher1 = new IndexSearcher(ramDirectory1);
			indexSearcher2 = new IndexSearcher(ramDirectory2);
			
			Searcher searcher = GetMultiSearcherInstance(new Searcher[]{indexSearcher1, indexSearcher2});
			
			hits = searcher.Search(query);
			
			Assert.AreEqual(2, hits.Length(), message);
			
			// The scores should be the same (within reason)
			Assert.AreEqual(scores[0], hits.Score(0), 1e-6, message); // This will a document from ramDirectory1
			Assert.AreEqual(scores[1], hits.Score(1), 1e-6, message); // This will a document from ramDirectory2
			
			
			
			// Adding a Sort.RELEVANCE object should not change anything
			hits = searcher.Search(query, Sort.RELEVANCE);
			
			Assert.AreEqual(2, hits.Length(), message);
			
			Assert.AreEqual(scores[0], hits.Score(0), 1e-6, message); // This will a document from ramDirectory1
			Assert.AreEqual(scores[1], hits.Score(1), 1e-6, message); // This will a document from ramDirectory2
			
			searcher.Close();
			
			ramDirectory1.Close();
			ramDirectory2.Close();
		}
	}
}