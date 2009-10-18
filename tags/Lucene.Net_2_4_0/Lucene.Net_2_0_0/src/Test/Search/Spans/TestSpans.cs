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
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using CheckHits = Lucene.Net.Search.CheckHits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Query = Lucene.Net.Search.Query;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using NUnit.Framework;

namespace Lucene.Net.Search.Spans
{
	[TestFixture]
	public class TestSpans
	{
		private IndexSearcher searcher;
		
		public const System.String field = "field";
		
		[SetUp]
        public virtual void  SetUp()
		{
			RAMDirectory directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
			for (int i = 0; i < docFields.Length; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
				doc.Add(new Field(field, docFields[i], Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(doc);
			}
			writer.Close();
			searcher = new IndexSearcher(directory);
			//System.out.println("set up " + getName());
		}
		
		private System.String[] docFields = new System.String[]{"w1 w2 w3 w4 w5", "w1 w3 w2 w3", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3", "u2 u2 u1", "u2 xx u2 u1", "u2 u2 xx u1", "u2 xx u2 yy u1", "u2 xx u1 u2", "u2 u1 xx u2", "u1 u2 xx u2", "t1 t2 t1 t3 t2 t3"};
		
		public virtual SpanTermQuery MakeSpanTermQuery(System.String text)
		{
			return new SpanTermQuery(new Term(field, text));
		}
		
		private void  CheckHits(Query query, int[] results)
		{
			Lucene.Net.Search.CheckHits.CheckHits_Renamed_Method(query, field, searcher, results);
		}
		
		private void  OrderedSlopTest3SQ(SpanQuery q1, SpanQuery q2, SpanQuery q3, int slop, int[] expectedDocs)
		{
			bool ordered = true;
			SpanNearQuery snq = new SpanNearQuery(new SpanQuery[]{q1, q2, q3}, slop, ordered);
			CheckHits(snq, expectedDocs);
		}
		
		public virtual void  OrderedSlopTest3(int slop, int[] expectedDocs)
		{
			OrderedSlopTest3SQ(MakeSpanTermQuery("w1"), MakeSpanTermQuery("w2"), MakeSpanTermQuery("w3"), slop, expectedDocs);
		}
		
		public virtual void  OrderedSlopTest3Equal(int slop, int[] expectedDocs)
		{
			OrderedSlopTest3SQ(MakeSpanTermQuery("w1"), MakeSpanTermQuery("w3"), MakeSpanTermQuery("w3"), slop, expectedDocs);
		}
		
		public virtual void  OrderedSlopTest1Equal(int slop, int[] expectedDocs)
		{
			OrderedSlopTest3SQ(MakeSpanTermQuery("u2"), MakeSpanTermQuery("u2"), MakeSpanTermQuery("u1"), slop, expectedDocs);
		}
		
		[Test]
        public virtual void  TestSpanNearOrdered01()
		{
			OrderedSlopTest3(0, new int[]{0});
		}
		
		[Test]
        public virtual void  TestSpanNearOrdered02()
		{
			OrderedSlopTest3(1, new int[]{0, 1});
		}
		
		[Test]
        public virtual void  TestSpanNearOrdered03()
		{
			OrderedSlopTest3(2, new int[]{0, 1, 2});
		}
		
		[Test]
        public virtual void  TestSpanNearOrdered04()
		{
			OrderedSlopTest3(3, new int[]{0, 1, 2, 3});
		}
		
		[Test]
        public virtual void  TestSpanNearOrdered05()
		{
			OrderedSlopTest3(4, new int[]{0, 1, 2, 3});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedEqual01()
		{
			OrderedSlopTest3Equal(0, new int[]{});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedEqual02()
		{
			OrderedSlopTest3Equal(1, new int[]{1});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedEqual03()
		{
			OrderedSlopTest3Equal(2, new int[]{1});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedEqual04()
		{
			OrderedSlopTest3Equal(3, new int[]{1, 3});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedEqual11()
		{
			OrderedSlopTest1Equal(0, new int[]{4});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedEqual12()
		{
			OrderedSlopTest1Equal(0, new int[]{4});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedEqual13()
		{
			OrderedSlopTest1Equal(1, new int[]{4, 5, 6});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedEqual14()
		{
			OrderedSlopTest1Equal(2, new int[]{4, 5, 6, 7});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedEqual15()
		{
			OrderedSlopTest1Equal(3, new int[]{4, 5, 6, 7});
		}
		
		[Test]
        public virtual void  TestSpanNearOrderedOverlap()
		{
			bool ordered = true;
			int slop = 1;
			SpanNearQuery snq = new SpanNearQuery(new SpanQuery[]{MakeSpanTermQuery("t1"), MakeSpanTermQuery("t2"), MakeSpanTermQuery("t3")}, slop, ordered);
			Spans spans = snq.GetSpans(searcher.GetIndexReader());
			
			Assert.IsTrue(spans.Next(), "first range");
			Assert.AreEqual(11, spans.Doc(), "first doc");
			Assert.AreEqual(0, spans.Start(), "first start");
			Assert.AreEqual(4, spans.End(), "first end");
			
			Assert.IsTrue(spans.Next(), "second range");
			Assert.AreEqual(11, spans.Doc(), "second doc");
			Assert.AreEqual(2, spans.Start(), "second start");
			Assert.AreEqual(6, spans.End(), "second end");
			
			Assert.IsFalse(spans.Next(), "third range");
		}
	}
}