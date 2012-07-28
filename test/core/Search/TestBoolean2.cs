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
using Lucene.Net.Store;
using NUnit.Framework;

using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using ParseException = Lucene.Net.QueryParsers.ParseException;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary>Test BooleanQuery2 against BooleanQuery by overriding the standard query parser.
	/// This also tests the scoring order of BooleanQuery.
	/// </summary>
    [TestFixture]
	public class TestBoolean2:LuceneTestCase
	{
		[Serializable]
		private class AnonymousClassDefaultSimilarity:DefaultSimilarity
		{
			public AnonymousClassDefaultSimilarity(TestBoolean2 enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestBoolean2 enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestBoolean2 enclosingInstance;
			public TestBoolean2 Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override float Coord(int overlap, int maxOverlap)
			{
				return overlap / ((float) maxOverlap - 1);
			}
		}
		private IndexSearcher searcher;
	    private IndexSearcher bigSearcher;
	    private IndexReader reader;
	    private static int NUM_EXTRA_DOCS = 6000;
		
		public const System.String field = "field";
	    private Directory dir2;
	    private int mulFactor;
		
		[SetUp]
		public override void  SetUp()
		{
			base.SetUp();
			RAMDirectory directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
			for (int i = 0; i < docFields.Length; i++)
			{
				Document document = new Document();
				document.Add(new Field(field, docFields[i], Field.Store.NO, Field.Index.ANALYZED));
				writer.AddDocument(document);
			}
			writer.Close();
			searcher = new IndexSearcher(directory, true);

            // Make big index
		    dir2 = new MockRAMDirectory(directory);

            // First multiply small test index:
		    mulFactor = 1;
		    int docCount = 0;
		    do
		    {
		        Directory copy = new RAMDirectory(dir2);
                IndexWriter indexWriter = new IndexWriter(dir2, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
		        indexWriter.AddIndexesNoOptimize(new[] {copy});
		        docCount = indexWriter.MaxDoc();
		        indexWriter.Close();
		        mulFactor *= 2;
		    } while (docCount < 3000);

		    IndexWriter w = new IndexWriter(dir2, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
		    Document doc = new Document();
            doc.Add(new Field("field2", "xxx", Field.Store.NO, Field.Index.ANALYZED));
            for (int i = 0; i < NUM_EXTRA_DOCS / 2; i++)
            {
                w.AddDocument(doc);
            }
            doc = new Document();
            doc.Add(new Field("field2", "big bad bug", Field.Store.NO, Field.Index.ANALYZED));
            for (int i = 0; i < NUM_EXTRA_DOCS / 2; i++)
            {
                w.AddDocument(doc);
            }
            // optimize to 1 segment
		    w.Optimize();
		    reader = w.GetReader();
		    w.Close();
		    bigSearcher = new IndexSearcher(reader);
		}

        [TearDown]
        public override void TearDown()
        {
            reader.Close();
            dir2.Close();
        }
		
		private System.String[] docFields = new System.String[]{"w1 w2 w3 w4 w5", "w1 w3 w2 w3", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3"};
		
		public virtual Query MakeQuery(System.String queryText)
		{
			Query q = (new QueryParser(Util.Version.LUCENE_CURRENT, field, new WhitespaceAnalyzer())).Parse(queryText);
			return q;
		}

        public virtual void QueriesTest(System.String queryText, int[] expDocNrs)
        {
            //System.out.println();
            //System.out.println("Query: " + queryText);
            Query query1 = MakeQuery(queryText);
            TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, false);
            searcher.Search(query1, null, collector);
            ScoreDoc[] hits1 = collector.TopDocs().ScoreDocs;

            Query query2 = MakeQuery(queryText); // there should be no need to parse again...
            collector = TopScoreDocCollector.Create(1000, true);
            searcher.Search(query2, null, collector);
            ScoreDoc[] hits2 = collector.TopDocs().ScoreDocs;

            Assert.AreEqual(mulFactor*collector.internalTotalHits, bigSearcher.Search(query1, 1).TotalHits);

            CheckHits.CheckHitsQuery(query2, hits1, hits2, expDocNrs);
        }

	    [Test]
		public virtual void  TestQueries01()
		{
			System.String queryText = "+w3 +xx";
			int[] expDocNrs = new int[]{2, 3};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries02()
		{
			System.String queryText = "+w3 xx";
			int[] expDocNrs = new int[]{2, 3, 1, 0};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries03()
		{
			System.String queryText = "w3 xx";
			int[] expDocNrs = new int[]{2, 3, 1, 0};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries04()
		{
			System.String queryText = "w3 -xx";
			int[] expDocNrs = new int[]{1, 0};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries05()
		{
			System.String queryText = "+w3 -xx";
			int[] expDocNrs = new int[]{1, 0};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries06()
		{
			System.String queryText = "+w3 -xx -w5";
			int[] expDocNrs = new int[]{1};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries07()
		{
			System.String queryText = "-w3 -xx -w5";
			int[] expDocNrs = new int[]{};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries08()
		{
			System.String queryText = "+w3 xx -w5";
			int[] expDocNrs = new int[]{2, 3, 1};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries09()
		{
			System.String queryText = "+w3 +xx +w2 zz";
			int[] expDocNrs = new int[]{2, 3};
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestQueries10()
		{
			System.String queryText = "+w3 +xx +w2 zz";
			int[] expDocNrs = new int[]{2, 3};
			searcher.Similarity = new AnonymousClassDefaultSimilarity(this);
			QueriesTest(queryText, expDocNrs);
		}
		
		[Test]
		public virtual void  TestRandomQueries()
		{
			System.Random rnd = NewRandom();
			
			System.String[] vals = new System.String[]{"w1", "w2", "w3", "w4", "w5", "xx", "yy", "zzz"};
			
			int tot = 0;
			
			BooleanQuery q1 = null;
			try
			{
				
				// increase number of iterations for more complete testing
				for (int i = 0; i < 1000; i++)
				{
					int level = rnd.Next(3);
					q1 = RandBoolQuery(new System.Random(rnd.Next(System.Int32.MaxValue)), rnd.Next(0, 2) == 0 ? false : true, level, field, vals, null);
					
					// Can't sort by relevance since floating point numbers may not quite
					// match up.
					Sort sort = Sort.INDEXORDER;
					
					QueryUtils.Check(q1, searcher);

				    TopFieldCollector collector = TopFieldCollector.Create(sort, 1000, false, true, true, true);
				    searcher.Search(q1, null, collector);
					ScoreDoc[] hits1 = collector.TopDocs().ScoreDocs;

				    collector = TopFieldCollector.Create(sort, 1000, false, true, true, false);
				    searcher.Search(q1, null, collector);
					ScoreDoc[] hits2 = collector.TopDocs().ScoreDocs;
					tot += hits2.Length;
					CheckHits.CheckEqual(q1, hits1, hits2);

                    BooleanQuery q3 = new BooleanQuery();
				    q3.Add(q1, Occur.SHOULD);
                    q3.Add(new PrefixQuery(new Term("field2", "b")), Occur.SHOULD);
				    TopDocs hits4 = bigSearcher.Search(q3, 1);
				    Assert.AreEqual(mulFactor*collector.internalTotalHits + NUM_EXTRA_DOCS/2, hits4.TotalHits);
				}
			}
			catch (System.Exception e)
			{
				// For easier debugging
				System.Console.Out.WriteLine("failed query: " + q1);
				throw e;
			}
			
			// System.out.println("Total hits:"+tot);
		}
		
		
		// used to set properties or change every BooleanQuery
		// generated from randBoolQuery.
		public interface Callback
		{
			void  PostCreate(BooleanQuery q);
		}
		
		// Random rnd is passed in so that the exact same random query may be created
		// more than once.
		public static BooleanQuery RandBoolQuery(System.Random rnd, bool allowMust, int level, System.String field, System.String[] vals, TestBoolean2.Callback cb)
		{
			BooleanQuery current = new BooleanQuery(rnd.Next() < 0);
			for (int i = 0; i < rnd.Next(vals.Length) + 1; i++)
			{
				int qType = 0; // term query
				if (level > 0)
				{
					qType = rnd.Next(10);
				}
				Query q;
                if (qType < 3)
                {
                    q = new TermQuery(new Term(field, vals[rnd.Next(vals.Length)]));
                }
                else if (qType < 7)
                {
                    q = new WildcardQuery(new Term(field, "w*"));
                }
                else
                {
                    q = RandBoolQuery(rnd, allowMust, level - 1, field, vals, cb);
                }

			    int r = rnd.Next(10);
				Occur occur;
                if (r < 2)
                {
                    occur = Occur.MUST_NOT;
                }
                else if (r < 5)
                {
                    occur = allowMust ? Occur.MUST : Occur.SHOULD;
                }
                else
                {
                    occur = Occur.SHOULD;
                }
				
				current.Add(q, occur);
			}
			if (cb != null)
				cb.PostCreate(current);
			return current;
		}
	}
}