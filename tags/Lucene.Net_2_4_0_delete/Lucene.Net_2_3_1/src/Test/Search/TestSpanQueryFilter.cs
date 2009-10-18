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
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
using English = Lucene.Net.Util.English;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	[TestFixture]
	public class TestSpanQueryFilter : LuceneTestCase
	{
		
		[Test]
		public virtual void  TestFilterWorks()
		{
			Directory dir = new RAMDirectory();
			IndexWriter writer = new IndexWriter(dir, new SimpleAnalyzer(), true);
			for (int i = 0; i < 500; i++)
			{
				Document document = new Document();
				document.Add(new Field("field", English.IntToEnglish(i) + " equals " + English.IntToEnglish(i), Field.Store.NO, Field.Index.TOKENIZED));
				writer.AddDocument(document);
			}
			writer.Close();
			
			IndexReader reader = IndexReader.Open(dir);
			
			SpanTermQuery query = new SpanTermQuery(new Term("field", English.IntToEnglish(10).Trim()));
			SpanQueryFilter filter = new SpanQueryFilter(query);
			SpanFilterResult result = filter.BitSpans(reader);
			System.Collections.BitArray bits = result.GetBits();
			Assert.IsTrue(bits != null, "bits is null and it shouldn't be");
			Assert.IsTrue(bits.Get(10), "tenth bit is not on");
			System.Collections.IList spans = result.GetPositions();
			Assert.IsTrue(spans != null, "spans is null and it shouldn't be");
			int cardinality = 0;
			for (int i = 0; i < bits.Count; i++)
			{
				if (bits.Get(i)) cardinality++;
			}
			Assert.IsTrue(spans.Count == cardinality, "spans Size: " + spans.Count + " is not: " + cardinality);
			for (System.Collections.IEnumerator iterator = spans.GetEnumerator(); iterator.MoveNext(); )
			{
				SpanFilterResult.PositionInfo info = (SpanFilterResult.PositionInfo) iterator.Current;
				Assert.IsTrue(info != null, "info is null and it shouldn't be");
				//The doc should indicate the bit is on
				Assert.IsTrue(bits.Get(info.GetDoc()), "Bit is not on and it should be");
				//There should be two positions in each
				Assert.IsTrue(info.GetPositions().Count == 2, "info.getPositions() Size: " + info.GetPositions().Count + " is not: " + 2);
			}
			reader.Close();
		}
	}
}