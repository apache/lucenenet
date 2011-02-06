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

using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using StopAnalyzer = Lucene.Net.Analysis.StopAnalyzer;
using StopFilter = Lucene.Net.Analysis.StopFilter;
using Token = Lucene.Net.Analysis.Token;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary> Term position unit test.
	/// 
	/// 
	/// </summary>
	/// <version>  $Revision: 607591 $
	/// </version>
	[TestFixture]
	public class TestPositionIncrement : LuceneTestCase
	{
		private class AnonymousClassAnalyzer : Analyzer
		{
			public AnonymousClassAnalyzer(TestPositionIncrement enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}

			private class AnonymousClassTokenStream : TokenStream
			{
				public AnonymousClassTokenStream(AnonymousClassAnalyzer enclosingInstance)
				{
					InitBlock(enclosingInstance);
				}
				private void  InitBlock(AnonymousClassAnalyzer enclosingInstance)
				{
					this.enclosingInstance = enclosingInstance;
				}
				private AnonymousClassAnalyzer enclosingInstance;
				public AnonymousClassAnalyzer Enclosing_Instance
				{
					get
					{
						return enclosingInstance;
					}
					
				}

				private System.String[] TOKENS = new System.String[]{"1", "2", "3", "4", "5"};
				private int[] INCREMENTS = new int[]{1, 2, 1, 0, 1};
				private int i = 0;
				
				public override Token Next()
				{
					if (i == TOKENS.Length)
						return null;
					Token t = new Token(TOKENS[i], i, i);
					t.SetPositionIncrement(INCREMENTS[i]);
					i++;
					return t;
				}
			}
			private void  InitBlock(TestPositionIncrement enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestPositionIncrement enclosingInstance;
			public TestPositionIncrement Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				return new AnonymousClassTokenStream(this);
			}
		}
		
		private class AnonymousClassAnalyzer1 : Analyzer
		{
			public AnonymousClassAnalyzer1(TestPositionIncrement enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(TestPositionIncrement enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TestPositionIncrement enclosingInstance;
			public TestPositionIncrement Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal WhitespaceAnalyzer a = new WhitespaceAnalyzer();
			public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				TokenStream ts = a.TokenStream(fieldName, reader);
				return new StopFilter(ts, new System.String[]{"stop"});
			}
		}
		
		[Test]
		public virtual void  TestSetPosition()
		{
			Analyzer analyzer = new AnonymousClassAnalyzer(this);
			RAMDirectory store = new RAMDirectory();
			IndexWriter writer = new IndexWriter(store, analyzer, true);
			Document d = new Document();
			d.Add(new Field("field", "bogus", Field.Store.YES, Field.Index.TOKENIZED));
			writer.AddDocument(d);
			writer.Optimize();
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(store);
			PhraseQuery q;
			Hits hits;
			
			q = new PhraseQuery();
			q.Add(new Term("field", "1"));
			q.Add(new Term("field", "2"));
			hits = searcher.Search(q);
			Assert.AreEqual(0, hits.Length());
			
			// same as previous, just specify positions explicitely.
			q = new PhraseQuery();
			q.Add(new Term("field", "1"), 0);
			q.Add(new Term("field", "2"), 1);
			hits = searcher.Search(q);
			Assert.AreEqual(0, hits.Length());
			
			// specifying correct positions should find the phrase.
			q = new PhraseQuery();
			q.Add(new Term("field", "1"), 0);
			q.Add(new Term("field", "2"), 2);
			hits = searcher.Search(q);
			Assert.AreEqual(1, hits.Length());
			
			q = new PhraseQuery();
			q.Add(new Term("field", "2"));
			q.Add(new Term("field", "3"));
			hits = searcher.Search(q);
			Assert.AreEqual(1, hits.Length());
			
			q = new PhraseQuery();
			q.Add(new Term("field", "3"));
			q.Add(new Term("field", "4"));
			hits = searcher.Search(q);
			Assert.AreEqual(0, hits.Length());
			
			// phrase query would find it when correct positions are specified. 
			q = new PhraseQuery();
			q.Add(new Term("field", "3"), 0);
			q.Add(new Term("field", "4"), 0);
			hits = searcher.Search(q);
			Assert.AreEqual(1, hits.Length());
			
			// phrase query should fail for non existing searched term 
			// even if there exist another searched terms in the same searched position. 
			q = new PhraseQuery();
			q.Add(new Term("field", "3"), 0);
			q.Add(new Term("field", "9"), 0);
			hits = searcher.Search(q);
			Assert.AreEqual(0, hits.Length());
			
			// multi-phrase query should succed for non existing searched term
			// because there exist another searched terms in the same searched position. 
			MultiPhraseQuery mq = new MultiPhraseQuery();
			mq.Add(new Term[]{new Term("field", "3"), new Term("field", "9")}, 0);
			hits = searcher.Search(mq);
			Assert.AreEqual(1, hits.Length());
			
			q = new PhraseQuery();
			q.Add(new Term("field", "2"));
			q.Add(new Term("field", "4"));
			hits = searcher.Search(q);
			Assert.AreEqual(1, hits.Length());
			
			q = new PhraseQuery();
			q.Add(new Term("field", "3"));
			q.Add(new Term("field", "5"));
			hits = searcher.Search(q);
			Assert.AreEqual(1, hits.Length());
			
			q = new PhraseQuery();
			q.Add(new Term("field", "4"));
			q.Add(new Term("field", "5"));
			hits = searcher.Search(q);
			Assert.AreEqual(1, hits.Length());
			
			q = new PhraseQuery();
			q.Add(new Term("field", "2"));
			q.Add(new Term("field", "5"));
			hits = searcher.Search(q);
			Assert.AreEqual(0, hits.Length());
			
			// analyzer to introduce stopwords and increment gaps 
			Analyzer stpa = new AnonymousClassAnalyzer1(this);
			
			// should not find "1 2" because there is a gap of 1 in the index
			Lucene.Net.QueryParsers.QueryParser qp = new Lucene.Net.QueryParsers.QueryParser("field", stpa);
			q = (PhraseQuery) qp.Parse("\"1 2\"");
			hits = searcher.Search(q);
			Assert.AreEqual(0, hits.Length());
			
			// omitted stop word cannot help because stop filter swallows the increments. 
			q = (PhraseQuery) qp.Parse("\"1 stop 2\"");
			hits = searcher.Search(q);
			Assert.AreEqual(0, hits.Length());
			
			// query parser alone won't help, because stop filter swallows the increments. 
			qp.SetEnablePositionIncrements(true);
			q = (PhraseQuery) qp.Parse("\"1 stop 2\"");
			hits = searcher.Search(q);
			Assert.AreEqual(0, hits.Length());
			
			bool dflt = StopFilter.GetEnablePositionIncrementsDefault();
			try
			{
				// stop filter alone won't help, because query parser swallows the increments. 
				qp.SetEnablePositionIncrements(false);
				StopFilter.SetEnablePositionIncrementsDefault(true);
				q = (PhraseQuery) qp.Parse("\"1 stop 2\"");
				hits = searcher.Search(q);
				Assert.AreEqual(0, hits.Length());
				
				// when both qp qnd stopFilter propagate increments, we should find the doc.
				qp.SetEnablePositionIncrements(true);
				q = (PhraseQuery) qp.Parse("\"1 stop 2\"");
				hits = searcher.Search(q);
				Assert.AreEqual(1, hits.Length());
			}
			finally
			{
				StopFilter.SetEnablePositionIncrementsDefault(dflt);
			}
		}
		
		/// <summary> Basic analyzer behavior should be to keep sequential terms in one
		/// increment from one another.
		/// </summary>
		[Test]
		public virtual void  TestIncrementingPositions()
		{
			Analyzer analyzer = new WhitespaceAnalyzer();
			TokenStream ts = analyzer.TokenStream("field", new System.IO.StringReader("one two three four five"));
			
			while (true)
			{
				Token token = ts.Next();
				if (token == null)
					break;
				Assert.AreEqual(1, token.GetPositionIncrement(), token.TermText());
			}
		}
	}
}