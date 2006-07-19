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
using Analyzer = Lucene.Net.Analysis.Analyzer;
using StopAnalyzer = Lucene.Net.Analysis.StopAnalyzer;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using WhitespaceTokenizer = Lucene.Net.Analysis.WhitespaceTokenizer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
	
	/// <summary> Tests {@link PhraseQuery}.
	/// 
	/// </summary>
	/// <seealso cref="TestPositionIncrement">
	/// </seealso>
	/// <author>  Erik Hatcher
	/// </author>
	[TestFixture]
    public class TestPhraseQuery
	{
		private class AnonymousClassAnalyzer : Analyzer
		{
			public AnonymousClassAnalyzer(TestPhraseQuery enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestPhraseQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestPhraseQuery enclosingInstance;
			public TestPhraseQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}

            public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				return new WhitespaceTokenizer(reader);
			}
			
			public override int GetPositionIncrementGap(System.String fieldName)
			{
				return 100;
			}
		}

        private IndexSearcher searcher;
		private PhraseQuery query;
		private RAMDirectory directory;
		
		[SetUp]
        public virtual void  SetUp()
		{
            System.Console.Out.WriteLine("Setup()");

			directory = new RAMDirectory();
			Analyzer analyzer = new AnonymousClassAnalyzer(this);
			IndexWriter writer = new IndexWriter(directory, analyzer, true);
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "one two three four five", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("repeated", "this is a repeated field - first part", Field.Store.YES, Field.Index.TOKENIZED));
			Field repeatedField = new Field("repeated", "second part of a repeated field", Field.Store.YES, Field.Index.TOKENIZED);
			doc.Add(repeatedField);
			writer.AddDocument(doc);
			
			writer.Optimize();
			writer.Close();
			
			searcher = new IndexSearcher(directory);
			query = new PhraseQuery();
		}
		
		[TearDown]
        public virtual void  TearDown()
		{
            System.Console.Out.WriteLine("TearDown()");

			searcher.Close();
			directory.Close();
		}
		
		[Test]
        public virtual void  TestNotCloseEnough()
		{
            System.Console.Out.WriteLine("TestNotCloseEnough()");

			query.SetSlop(2);
			query.Add(new Term("field", "one"));
			query.Add(new Term("field", "five"));
			Hits hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length());
		}
		
		[Test]
        public virtual void  TestBarelyCloseEnough()
		{
            System.Console.Out.WriteLine("TestBarelyCloseEnough()");

			query.SetSlop(3);
			query.Add(new Term("field", "one"));
			query.Add(new Term("field", "five"));
			Hits hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length());
		}
		
		/// <summary> Ensures slop of 0 works for exact matches, but not reversed</summary>
		[Test]
        public virtual void  TestExact()
		{
            System.Console.Out.WriteLine("TestExact()");

			// slop is zero by default
			query.Add(new Term("field", "four"));
			query.Add(new Term("field", "five"));
			Hits hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length(), "exact match");
			
			query = new PhraseQuery();
			query.Add(new Term("field", "two"));
			query.Add(new Term("field", "one"));
			hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length(), "reverse not exact");
		}
		
		[Test]
        public virtual void  TestSlop1()
		{
            System.Console.Out.WriteLine("TestSlop1()");

			// Ensures slop of 1 works with terms in order.
			query.SetSlop(1);
			query.Add(new Term("field", "one"));
			query.Add(new Term("field", "two"));
			Hits hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length(), "in order");
			
			// Ensures slop of 1 does not work for phrases out of order;
			// must be at least 2.
			query = new PhraseQuery();
			query.SetSlop(1);
			query.Add(new Term("field", "two"));
			query.Add(new Term("field", "one"));
			hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length(), "reversed, slop not 2 or more");
		}
		
		/// <summary> As long as slop is at least 2, terms can be reversed</summary>
		[Test]
        public virtual void  TestOrderDoesntMatter()
		{
            System.Console.Out.WriteLine("TestOrderDoesntMatter()");

			query.SetSlop(2); // must be at least two for reverse order match
			query.Add(new Term("field", "two"));
			query.Add(new Term("field", "one"));
			Hits hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length(), "just sloppy enough");
			
			query = new PhraseQuery();
			query.SetSlop(2);
			query.Add(new Term("field", "three"));
			query.Add(new Term("field", "one"));
			hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length(), "not sloppy enough");
		}
		
		/// <summary> slop is the total number of positional moves allowed
		/// to line up a phrase
		/// </summary>
		[Test]
        public virtual void  TestMulipleTerms()
		{
            System.Console.Out.WriteLine("TestMulipleTerms()");

			query.SetSlop(2);
			query.Add(new Term("field", "one"));
			query.Add(new Term("field", "three"));
			query.Add(new Term("field", "five"));
			Hits hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length(), "two total moves");
			
			query = new PhraseQuery();
			query.SetSlop(5); // it takes six moves to match this phrase
			query.Add(new Term("field", "five"));
			query.Add(new Term("field", "three"));
			query.Add(new Term("field", "one"));
			hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length(), "slop of 5 not close enough");
			
			query.SetSlop(6);
			hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length(), "slop of 6 just right");
		}
		
		[Test]
        public virtual void  TestPhraseQueryWithStopAnalyzer()
		{
            System.Console.Out.WriteLine("TestPhraseQueryWithStopAnalyzer()");

			RAMDirectory directory = new RAMDirectory();
			StopAnalyzer stopAnalyzer = new StopAnalyzer();
			IndexWriter writer = new IndexWriter(directory, stopAnalyzer, true);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "the stop words are here", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(directory);
			
			// valid exact phrase query
			PhraseQuery query = new PhraseQuery();
			query.Add(new Term("field", "stop"));
			query.Add(new Term("field", "words"));
			Hits hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length());
			
			// currently StopAnalyzer does not leave "holes", so this matches.
			query = new PhraseQuery();
			query.Add(new Term("field", "words"));
			query.Add(new Term("field", "here"));
			hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length());
			
			searcher.Close();
		}
		
		[Test]
        public virtual void  TestPhraseQueryInConjunctionScorer()
		{
            System.Console.Out.WriteLine("TestPhraseQueryInConjunctionScorer()");

			RAMDirectory directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("source", "marketing info", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("contents", "foobar", Field.Store.YES, Field.Index.TOKENIZED));
			doc.Add(new Field("source", "marketing info", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			writer.Optimize();
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(directory);
			
			PhraseQuery phraseQuery = new PhraseQuery();
			phraseQuery.Add(new Term("source", "marketing"));
			phraseQuery.Add(new Term("source", "info"));
			Hits hits = searcher.Search(phraseQuery);
			Assert.AreEqual(2, hits.Length());
			
			TermQuery termQuery = new TermQuery(new Term("contents", "foobar"));
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(termQuery, BooleanClause.Occur.MUST);
			booleanQuery.Add(phraseQuery, BooleanClause.Occur.MUST);
			hits = searcher.Search(booleanQuery);
			Assert.AreEqual(1, hits.Length());
			
			searcher.Close();
			
			writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("contents", "map entry woo", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("contents", "woo map entry", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("contents", "map foobarword entry woo", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			writer.Optimize();
			writer.Close();
			
			searcher = new IndexSearcher(directory);
			
			termQuery = new TermQuery(new Term("contents", "woo"));
			phraseQuery = new PhraseQuery();
			phraseQuery.Add(new Term("contents", "map"));
			phraseQuery.Add(new Term("contents", "entry"));
			
			hits = searcher.Search(termQuery);
			Assert.AreEqual(3, hits.Length());
			hits = searcher.Search(phraseQuery);
			Assert.AreEqual(2, hits.Length());
			
			booleanQuery = new BooleanQuery();
			booleanQuery.Add(termQuery, BooleanClause.Occur.MUST);
			booleanQuery.Add(phraseQuery, BooleanClause.Occur.MUST);
			hits = searcher.Search(booleanQuery);
			Assert.AreEqual(2, hits.Length());
			
			booleanQuery = new BooleanQuery();
			booleanQuery.Add(phraseQuery, BooleanClause.Occur.MUST);
			booleanQuery.Add(termQuery, BooleanClause.Occur.MUST);
			hits = searcher.Search(booleanQuery);
			Assert.AreEqual(2, hits.Length());
			
			searcher.Close();
			directory.Close();
		}
		
		[Test]
        public virtual void  TestSlopScoring()
		{
            System.Console.Out.WriteLine("TestSlopScoring()");

			Directory directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "foo firstname lastname foo", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			
			Lucene.Net.Documents.Document doc2 = new Lucene.Net.Documents.Document();
			doc2.Add(new Field("field", "foo firstname xxx lastname foo", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc2);
			
			Lucene.Net.Documents.Document doc3 = new Lucene.Net.Documents.Document();
			doc3.Add(new Field("field", "foo firstname xxx yyy lastname foo", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(doc3);
			
			writer.Optimize();
			writer.Close();
			
			Searcher searcher = new IndexSearcher(directory);
			PhraseQuery query = new PhraseQuery();
			query.Add(new Term("field", "firstname"));
			query.Add(new Term("field", "lastname"));
			query.SetSlop(System.Int32.MaxValue);
			Hits hits = searcher.Search(query);
			Assert.AreEqual(3, hits.Length());
			// Make sure that those matches where the terms appear closer to
			// each other get a higher score:
			Assert.AreEqual(0.71, hits.Score(0), 0.01);
			Assert.AreEqual(0, hits.Id(0));
			Assert.AreEqual(0.44, hits.Score(1), 0.01);
			Assert.AreEqual(1, hits.Id(1));
			Assert.AreEqual(0.31, hits.Score(2), 0.01);
			Assert.AreEqual(2, hits.Id(2));
		}
		
		[Test]
        public virtual void  TestWrappedPhrase()
		{
            System.Console.Out.WriteLine("TestWrappedPhrase()");

			query.Add(new Term("repeated", "first"));
			query.Add(new Term("repeated", "part"));
			query.Add(new Term("repeated", "second"));
			query.Add(new Term("repeated", "part"));
			query.SetSlop(99);
			
			Hits hits = searcher.Search(query);
			Assert.AreEqual(0, hits.Length());
		}
	}
}