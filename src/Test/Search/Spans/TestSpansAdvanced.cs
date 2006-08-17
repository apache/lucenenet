/*
 * Copyright 2005 The Apache Software Foundation
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
using BooleanClause = Lucene.Net.Search.BooleanClause;
using BooleanQuery = Lucene.Net.Search.BooleanQuery;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search.Spans
{
	
	/// <summary>****************************************************************************
	/// Tests the span query bug in Lucene. It demonstrates that SpanTermQuerys don't
	/// work correctly in a BooleanQuery.
	/// 
	/// </summary>
	/// <author>  Reece Wilton
	/// </author>
	[TestFixture]
    public class TestSpansAdvanced
	{
		
		// location to the index
		protected internal Directory mDirectory; 
		
        protected internal IndexSearcher searcher;
		
        // field names in the index
		private const System.String FIELD_ID = "ID";
		protected internal const System.String FIELD_TEXT = "TEXT";
		
		/// <summary> Initializes the tests by adding 4 identical documents to the index.</summary>
		[SetUp]
        public virtual void  SetUp()
		{
			
			// create test index
			mDirectory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(mDirectory, new StandardAnalyzer(), true);
			AddDocument(writer, "1", "I think it should work.");
			AddDocument(writer, "2", "I think it should work.");
			AddDocument(writer, "3", "I think it should work.");
			AddDocument(writer, "4", "I think it should work.");
			writer.Close();
            searcher = new IndexSearcher(mDirectory);
        }
		
		[TearDown]
        public virtual void  TearDown()
		{
            searcher.Close();
            mDirectory.Close();
			mDirectory = null;
		}
		
		/// <summary> Adds the document to the index.
		/// 
		/// </summary>
		/// <param name="writer">the Lucene index writer
		/// </param>
		/// <param name="id">the unique id of the document
		/// </param>
		/// <param name="text">the text of the document
		/// </param>
		/// <throws>  IOException </throws>
		protected internal virtual void  AddDocument(IndexWriter writer, System.String id, System.String text)
		{
			
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document();
			document.Add(new Field(FIELD_ID, id, Field.Store.YES, Field.Index.UN_TOKENIZED));
			document.Add(new Field(FIELD_TEXT, text, Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(document);
		}
		
		/// <summary> Tests two span queries.
		/// 
		/// </summary>
		/// <throws>  IOException </throws>
		[Test]
        public virtual void  TestBooleanQueryWithSpanQueries()
		{
			
			DoTestBooleanQueryWithSpanQueries(searcher, 0.3884282f);
		}
		
		/// <summary> Tests two span queries.
		/// 
		/// </summary>
		/// <throws>  IOException </throws>
		protected internal virtual void  DoTestBooleanQueryWithSpanQueries(IndexSearcher s, float expectedScore)
		{
			
			Query spanQuery = new SpanTermQuery(new Term(FIELD_TEXT, "work"));
			BooleanQuery query = new BooleanQuery();
			query.Add(spanQuery, BooleanClause.Occur.MUST);
			query.Add(spanQuery, BooleanClause.Occur.MUST);
            System.String[] expectedIds = new System.String[]{"1", "2", "3", "4"};
			float[] expectedScores = new float[]{expectedScore, expectedScore, expectedScore, expectedScore};
            AssertHits(s, query, "two span queries", expectedIds, expectedScores);
		}
		
		
		/// <summary> Checks to see if the hits are what we expected.
		/// 
		/// </summary>
		/// <param name="hits">the search results
		/// </param>
		/// <param name="description">the description of the search
		/// </param>
		/// <param name="expectedIds">the expected document ids of the hits
		/// </param>
		/// <param name="expectedScores">the expected scores of the hits
		/// 
		/// </param>
		/// <throws>  IOException </throws>
		protected internal virtual void  AssertHits(Searcher s, Query query, System.String description, System.String[] expectedIds, float[] expectedScores)
		{
            float tolerance = 1e-5f;
			
            // Hits hits = searcher.search(query);
            // hits normalizes and throws things off if one score is greater than 1.0
            TopDocs topdocs = s.Search(query, null, 10000);
			
            /// <summary>**
            /// // display the hits
            /// System.out.println(hits.length() + " hits for search: \"" + description + '\"');
            /// for (int i = 0; i < hits.length(); i++) {
            /// System.out.println("  " + FIELD_ID + ':' + hits.doc(i).get(FIELD_ID) + " (score:" + hits.score(i) + ')');
            /// }
            /// ***
            /// </summary>
			
            // did we get the hits we expected
            Assert.AreEqual(expectedIds.Length, topdocs.totalHits);
            for (int i = 0; i < topdocs.totalHits; i++)
            {
                //System.out.println(i + " exp: " + expectedIds[i]);
                //System.out.println(i + " field: " + hits.doc(i).get(FIELD_ID));
				
                int id = topdocs.scoreDocs[i].doc;
                float score = topdocs.scoreDocs[i].score;
                Lucene.Net.Documents.Document doc = s.Doc(id);
                Assert.AreEqual(expectedIds[i], doc.Get(FIELD_ID));
                bool scoreEq = System.Math.Abs(expectedScores[i] - score) < tolerance;
                if (!scoreEq)
                {
                    System.Console.Out.WriteLine(i + " warning, expected score: " + expectedScores[i] + ", actual " + score);
                    System.Console.Out.WriteLine(s.Explain(query, id));
                }
                Assert.AreEqual(expectedScores[i], score, tolerance);
                Assert.AreEqual(s.Explain(query, id).GetValue(), score, tolerance);
            }
        }
	}
}