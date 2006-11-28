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
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using NUnit.Framework;

namespace Lucene.Net.Search
{
	
	/// <summary> Test of the DisjunctionMaxQuery.
	/// 
	/// </summary>
	[TestFixture]
    public class TestDisjunctionMaxQuery
	{
		public TestDisjunctionMaxQuery()
		{
			InitBlock();
		}
		private void  InitBlock()
		{
			sim = new TestSimilarity();
		}
		
		/// <summary>threshold for comparing floats </summary>
		public const float SCORE_COMP_THRESH = 0.0000f;
		
		/// <summary> Similarity to eliminate tf, idf and lengthNorm effects to
		/// isolate test case.
		/// 
		/// <p>
		/// same as TestRankingSimilarity in TestRanking.zip from
		/// http://issues.apache.org/jira/browse/LUCENE-323
		/// </p>
		/// </summary>
		/// <author>  Williams
		/// </author>
		[Serializable]
		private class TestSimilarity:DefaultSimilarity
		{
			
			public TestSimilarity()
			{
			}
			public override float Tf(float freq)
			{
				if (freq > 0.0f)
					return 1.0f;
				else
					return 0.0f;
			}
			public override float LengthNorm(System.String fieldName, int numTerms)
			{
				return 1.0f;
			}
			public override float Idf(int docFreq, int numDocs)
			{
				return 1.0f;
			}
		}
		
		public Similarity sim;
		public Directory index;
		public IndexReader r;
		public IndexSearcher s;
		
		[SetUp]
        public virtual void  SetUp()
		{
			
			index = new RAMDirectory();
			IndexWriter writer = new IndexWriter(index, new WhitespaceAnalyzer(), true);
			writer.SetSimilarity(sim);
			
			// hed is the most important field, dek is secondary
			
			// d1 is an "ok" match for:  albino elephant
			{
				Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document();
                d1.Add(new Field("id", "d1", Field.Store.YES, Field.Index.UN_TOKENIZED)); //Field.Keyword("id", "d1"));
                d1.Add(new Field("hed", "elephant", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("hed", "elephant"));
                d1.Add(new Field("dek", "elephant", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("dek", "elephant"));
                writer.AddDocument(d1);
			}
			
			// d2 is a "good" match for:  albino elephant
			{
				Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document();
                d2.Add(new Field("id", "d2", Field.Store.YES, Field.Index.UN_TOKENIZED)); //Field.Keyword("id", "d2"));
                d2.Add(new Field("hed", "elephant", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("hed", "elephant"));
                d2.Add(new Field("dek", "albino", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("dek", "albino"));
                d2.Add(new Field("dek", "elephant", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("dek", "elephant"));
                writer.AddDocument(d2);
			}
			
			// d3 is a "better" match for:  albino elephant
			{
				Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document();
                d3.Add(new Field("id", "d3", Field.Store.YES, Field.Index.UN_TOKENIZED)); //Field.Keyword("id", "d3"));
                d3.Add(new Field("hed", "albino", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("hed", "albino"));
                d3.Add(new Field("hed", "elephant", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("hed", "elephant"));
                writer.AddDocument(d3);
			}
			
			// d4 is the "best" match for:  albino elephant
			{
				Lucene.Net.Documents.Document d4 = new Lucene.Net.Documents.Document();
                d4.Add(new Field("id", "d4", Field.Store.YES, Field.Index.UN_TOKENIZED)); //Field.Keyword("id", "d4"));
                d4.Add(new Field("hed", "albino", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("hed", "albino"));
                d4.Add(new Field("hed", "elephant", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("hed", "elephant"));
                d4.Add(new Field("dek", "albino", Field.Store.YES, Field.Index.TOKENIZED)); //Field.Text("dek", "albino"));
                writer.AddDocument(d4);
			}
			
			writer.Close();
			
			r = IndexReader.Open(index);
			s = new IndexSearcher(r);
			s.SetSimilarity(sim);
		}
		
		[Test]
		public virtual void  TestSimpleEqualScores1()
		{
			
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
			q.Add(Tq("hed", "albino"));
			q.Add(Tq("hed", "elephant"));
			
			Hits h = s.Search(q);
			
			try
			{
				Assert.AreEqual(4, h.Length(), "all docs should match " + q.ToString());
				
				float score = h.Score(0);
				for (int i = 1; i < h.Length(); i++)
				{
					Assert.AreEqual(score, h.Score(i), SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testSimpleEqualScores1", h);
				throw e;
			}
		}
		
		[Test]
        public virtual void  TestSimpleEqualScores2()
		{
			
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
			q.Add(Tq("dek", "albino"));
			q.Add(Tq("dek", "elephant"));
			
			Hits h = s.Search(q);
			
			try
			{
				Assert.AreEqual(3, h.Length(), "3 docs should match " + q.ToString());
				float score = h.Score(0);
				for (int i = 1; i < h.Length(); i++)
				{
					Assert.AreEqual(score, h.Score(i), SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testSimpleEqualScores2", h);
				throw e;
			}
		}
		
		[Test]
        public virtual void  TestSimpleEqualScores3()
		{
			
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
			q.Add(Tq("hed", "albino"));
			q.Add(Tq("hed", "elephant"));
			q.Add(Tq("dek", "albino"));
			q.Add(Tq("dek", "elephant"));
			
			Hits h = s.Search(q);
			
			try
			{
				Assert.AreEqual(4, h.Length(), "all docs should match " + q.ToString());
				float score = h.Score(0);
				for (int i = 1; i < h.Length(); i++)
				{
					Assert.AreEqual(score, h.Score(i), SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testSimpleEqualScores3", h);
				throw e;
			}
		}
		
		[Test]
        public virtual void  TestSimpleTiebreaker()
		{
			
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.01f);
			q.Add(Tq("dek", "albino"));
			q.Add(Tq("dek", "elephant"));
			
			Hits h = s.Search(q);
			
			try
			{
				Assert.AreEqual(3, h.Length(), "3 docs should match " + q.ToString());
				Assert.AreEqual("d2", h.Doc(0).Get("id"), "wrong first");
				float score0 = h.Score(0);
				float score1 = h.Score(1);
				float score2 = h.Score(2);
				Assert.IsTrue(score0 > score1, "d2 does not have better score then others: " + score0 + " >? " + score1);
				Assert.AreEqual(score1, score2, SCORE_COMP_THRESH, "d4 and d1 don't have equal scores");
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testSimpleTiebreaker", h);
				throw e;
			}
		}
		
		[Test]
        public virtual void  TestBooleanRequiredEqualScores()
		{
			
			BooleanQuery q = new BooleanQuery();
			{
				DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.0f);
				q1.Add(Tq("hed", "albino"));
				q1.Add(Tq("dek", "albino"));
                q.Add(q1, BooleanClause.Occur.MUST); //false,false);
            }
			{
				DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.0f);
				q2.Add(Tq("hed", "elephant"));
				q2.Add(Tq("dek", "elephant"));
                q.Add(q2, BooleanClause.Occur.MUST); //false,false);
            }
			
			
			Hits h = s.Search(q);
			
			try
			{
				Assert.AreEqual(3, h.Length(), "3 docs should match " + q.ToString());
				float score = h.Score(0);
				for (int i = 1; i < h.Length(); i++)
				{
					Assert.AreEqual(score, h.Score(i), SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testBooleanRequiredEqualScores1", h);
				throw e;
			}
		}
		
		[Test]
		public virtual void  TestBooleanOptionalNoTiebreaker()
		{
			
			BooleanQuery q = new BooleanQuery();
			{
				DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.0f);
				q1.Add(Tq("hed", "albino"));
				q1.Add(Tq("dek", "albino"));
                q.Add(q1, BooleanClause.Occur.SHOULD); //false,false);
            }
			{
				DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.0f);
				q2.Add(Tq("hed", "elephant"));
				q2.Add(Tq("dek", "elephant"));
                q.Add(q2, BooleanClause.Occur.SHOULD); //false,false);
            }
			
			
			Hits h = s.Search(q);
			
			try
			{
				Assert.AreEqual(4, h.Length(), "4 docs should match " + q.ToString());
				float score = h.Score(0);
				for (int i = 1; i < h.Length() - 1; i++)
				{
					/* note: -1 */
					Assert.AreEqual(score, h.Score(i), SCORE_COMP_THRESH, "score #" + i + " is not the same");
				}
				Assert.AreEqual("d1", h.Doc(h.Length() - 1).Get("id"), "wrong last");
				float score1 = h.Score(h.Length() - 1);
				Assert.IsTrue(score > score1, "d1 does not have worse score then others: " + score + " >? " + score1);
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testBooleanOptionalNoTiebreaker", h);
				throw e;
			}
		}
		
		[Test]
		public virtual void  TestBooleanOptionalWithTiebreaker()
		{
			
			BooleanQuery q = new BooleanQuery();
			{
				DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.01f);
				q1.Add(Tq("hed", "albino"));
				q1.Add(Tq("dek", "albino"));
                q.Add(q1, BooleanClause.Occur.SHOULD); //false,false);
            }
			{
				DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.01f);
				q2.Add(Tq("hed", "elephant"));
				q2.Add(Tq("dek", "elephant"));
                q.Add(q2, BooleanClause.Occur.SHOULD); //false,false);
            }
			
			
			Hits h = s.Search(q);
			
			try
			{
				
				Assert.AreEqual(4, h.Length(), "4 docs should match " + q.ToString());
				
				float score0 = h.Score(0);
				float score1 = h.Score(1);
				float score2 = h.Score(2);
				float score3 = h.Score(3);
				
				System.String doc0 = h.Doc(0).Get("id");
				System.String doc1 = h.Doc(1).Get("id");
				System.String doc2 = h.Doc(2).Get("id");
				System.String doc3 = h.Doc(3).Get("id");
				
				Assert.IsTrue(doc0.Equals("d2") || doc0.Equals("d4"), "doc0 should be d2 or d4: " + doc0);
				Assert.IsTrue(doc1.Equals("d2") || doc1.Equals("d4"), "doc1 should be d2 or d4: " + doc0);
				Assert.AreEqual(score0, score1, SCORE_COMP_THRESH, "score0 and score1 should match");
				Assert.AreEqual("d3", doc2, "wrong third");
				Assert.IsTrue(score1 > score2, "d3 does not have worse score then d2 and d4: " + score1 + " >? " + score2);
				
				Assert.AreEqual("d1", doc3, "wrong fourth");
				Assert.IsTrue(score2 > score3, "d1 does not have worse score then d3: " + score2 + " >? " + score3);
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testBooleanOptionalWithTiebreaker", h);
				throw e;
			}
		}
		
		[Test]
		public virtual void  TestBooleanOptionalWithTiebreakerAndBoost()
		{
			
			BooleanQuery q = new BooleanQuery();
			{
				DisjunctionMaxQuery q1 = new DisjunctionMaxQuery(0.01f);
				q1.Add(Tq("hed", "albino", 1.5f));
				q1.Add(Tq("dek", "albino"));
                q.Add(q1, BooleanClause.Occur.SHOULD); //false,false);
            }
			{
				DisjunctionMaxQuery q2 = new DisjunctionMaxQuery(0.01f);
				q2.Add(Tq("hed", "elephant", 1.5f));
				q2.Add(Tq("dek", "elephant"));
                q.Add(q2, BooleanClause.Occur.SHOULD); //false,false);
            }
			
			
			Hits h = s.Search(q);
			
			try
			{
				
				Assert.AreEqual(4, h.Length(), "4 docs should match " + q.ToString());
				
				float score0 = h.Score(0);
				float score1 = h.Score(1);
				float score2 = h.Score(2);
				float score3 = h.Score(3);
				
				System.String doc0 = h.Doc(0).Get("id");
				System.String doc1 = h.Doc(1).Get("id");
				System.String doc2 = h.Doc(2).Get("id");
				System.String doc3 = h.Doc(3).Get("id");
				
				Assert.AreEqual("d4", doc0, "doc0 should be d4: ");
				Assert.AreEqual("d3", doc1, "doc1 should be d3: ");
				Assert.AreEqual("d2", doc2, "doc2 should be d2: ");
				Assert.AreEqual("d1", doc3, "doc3 should be d1: ");
				
				Assert.IsTrue(score0 > score1, "d4 does not have a better score then d3: " + score0 + " >? " + score1);
				Assert.IsTrue(score1 > score2, "d3 does not have a better score then d2: " + score1 + " >? " + score2);
				Assert.IsTrue(score2 > score3, "d3 does not have a better score then d1: " + score2 + " >? " + score3);
			}
			catch (System.ApplicationException e)
			{
				PrintHits("testBooleanOptionalWithTiebreakerAndBoost", h);
				throw e;
			}
		}
		
		
		
		
		
		
		
		/// <summary>macro </summary>
		protected internal virtual Query Tq(System.String f, System.String t)
		{
			return new TermQuery(new Term(f, t));
		}
		/// <summary>macro </summary>
		protected internal virtual Query Tq(System.String f, System.String t, float b)
		{
			Query q = Tq(f, t);
			q.SetBoost(b);
			return q;
		}
		
		
		protected internal virtual void  PrintHits(System.String test, Hits h)
		{
			
			System.Console.Error.WriteLine("------- " + test + " -------");
			
			for (int i = 0; i < h.Length(); i++)
			{
				Lucene.Net.Documents.Document d = h.Doc(i);
				float score = h.Score(i);
                System.Console.Error.WriteLine("#" + i + ": {0.000000000}" + score + " - " + d.Get("id"));
            }
		}
	}
}