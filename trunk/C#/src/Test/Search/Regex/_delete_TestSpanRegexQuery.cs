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
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Hits = Lucene.Net.Search.Hits;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search.Regex
{
#if DELETE_ME
	[TestFixture]
	public class TestSpanRegexQuery
	{
		[Test]
        public virtual void  TestSpanRegex()
		{
			RAMDirectory directory = new RAMDirectory();
			IndexWriter writer = new IndexWriter(directory, new SimpleAnalyzer(), true);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("field", "the quick brown fox jumps over the lazy dog", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
			writer.Optimize();
			writer.Close();
			
			IndexSearcher searcher = new IndexSearcher(directory);
			SpanRegexQuery srq = new SpanRegexQuery(new Term("field", "q.[aeiou]c.*"));
			SpanTermQuery stq = new SpanTermQuery(new Term("field", "dog"));
			SpanNearQuery query = new SpanNearQuery(new SpanQuery[]{srq, stq}, 6, true);
			Hits hits = searcher.Search(query);
			Assert.AreEqual(1, hits.Length());
		}
	}
#endif
}