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
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using NUnit.Framework;

namespace Lucene.Net.Search
{
	
	/// <author>  Christoph Goller
	/// </author>
	/// <version>  $rcs = ' $Id: TestBooleanScorer.java 150700 2004-12-10 19:36:40Z goller $ ' ;
	/// </version>
	[TestFixture]
    public class TestBooleanScorer
	{
		
		private const System.String FIELD = "category";
		
		[Test]
        public virtual void  TestMethod()
		{
			RAMDirectory directory = new RAMDirectory();
			
			System.String[] values = new System.String[]{"1", "2", "3", "4"};
			
			try
			{
				IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true);
				for (int i = 0; i < values.Length; i++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
					doc.Add(new Field(FIELD, values[i], Field.Store.YES, Field.Index.UN_TOKENIZED));
					writer.AddDocument(doc);
				}
				writer.Close();
				
				BooleanQuery booleanQuery1 = new BooleanQuery();
				booleanQuery1.Add(new TermQuery(new Term(FIELD, "1")), BooleanClause.Occur.SHOULD);
				booleanQuery1.Add(new TermQuery(new Term(FIELD, "2")), BooleanClause.Occur.SHOULD);
				
				BooleanQuery query = new BooleanQuery();
				query.Add(booleanQuery1, BooleanClause.Occur.MUST);
				query.Add(new TermQuery(new Term(FIELD, "9")), BooleanClause.Occur.MUST_NOT);
				
				IndexSearcher indexSearcher = new IndexSearcher(directory);
				Hits hits = indexSearcher.Search(query);
				Assert.AreEqual(2, hits.Length(), "Number of matched documents");
			}
			catch (System.IO.IOException e)
			{
				Assert.Fail(e.Message);
			}
		}
	}
}