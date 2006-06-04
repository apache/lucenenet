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
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search.Regex
{
	[TestFixture]
	public class TestRegexQuery
	{
		private IndexSearcher searcher;
		private System.String FN = "field";
		

        [STAThread]
        public static void  Main(System.String[] args)
        {
            TestRegexQuery t = new TestRegexQuery();
            t.SetUp();
            t.TestRegex1();
        }

		[TestFixtureSetUp]
        public virtual void  SetUp()
		{
			RAMDirectory directory = new RAMDirectory();
			try
			{
				IndexWriter writer = new IndexWriter(directory, new SimpleAnalyzer(), true);
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
				doc.Add(new Field(FN, "the quick brown fox jumps over the lazy dog", Field.Store.NO, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
				writer.Optimize();
				writer.Close();
				searcher = new IndexSearcher(directory);
			}
			catch (System.Exception e)
			{
				Assert.Fail(e.ToString());
			}
		}
		
		[TestFixtureTearDown]
        public virtual void  TearDown()
		{
			try
			{
				searcher.Close();
			}
			catch (System.Exception e)
			{
				Assert.Fail(e.ToString());
			}
		}
		
		private Term NewTerm(System.String value_Renamed)
		{
			return new Term(FN, value_Renamed);
		}
		
		private int RegexQueryNrHits(System.String regex)
		{
			Query query = new RegexQuery(NewTerm(regex));
			return searcher.Search(query).Length();
		}
		
		private int SpanRegexQueryNrHits(System.String regex1, System.String regex2, int slop, bool ordered)
		{
			SpanRegexQuery srq1 = new SpanRegexQuery(NewTerm(regex1));
			SpanRegexQuery srq2 = new SpanRegexQuery(NewTerm(regex2));
			SpanNearQuery query = new SpanNearQuery(new SpanQuery[]{srq1, srq2}, slop, ordered);
			return searcher.Search(query).Length();
		}
		
		[Test]
        public virtual void  TestRegex1()
		{
			Assert.AreEqual(1, RegexQueryNrHits("q.[aeiou]c.*"));
		}
		
		[Test]
        public virtual void  TestRegex2()
		{
			Assert.AreEqual(0, RegexQueryNrHits(".[aeiou]c.*"));    // {{Aroush-1.9}} this test is failing
		}
		
		[Test]
        public virtual void  TestRegex3()
		{
			Assert.AreEqual(0, RegexQueryNrHits("q.[aeiou]c"));     // {{Aroush-1.9}} this test is failing
		}
		
		[Test]
        public virtual void  TestSpanRegex1()
		{
			Assert.AreEqual(1, SpanRegexQueryNrHits("q.[aeiou]c.*", "dog", 6, true));
		}
		
		[Test]
        public virtual void  TestSpanRegex2()
		{
			Assert.AreEqual(0, SpanRegexQueryNrHits("q.[aeiou]c.*", "dog", 5, true));
		}
		
		//  public void testPrefix() throws Exception {
		// This test currently fails because RegexTermEnum picks "r" as the prefix
		// but the following "?" makes the "r" optional and should be a hit for the
		// document matching "over".
		//    Assert.AreEqual(1, regexQueryNrHits("r?over"));
		//  }
	}
}