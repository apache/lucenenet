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
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary> </summary>
	/// <version>  $rcs = ' $Id: TestBooleanScorer.java 583534 2007-10-10 16:46:35Z mikemccand $ ' ;
	/// </version>
	[TestFixture]
	public class TestBooleanScorer : LuceneTestCase
	{
		
		//public TestBooleanScorer(System.String name) : base(name)
		//{
		//}
		
		private const System.String FIELD = "category";
		
		[Test]
		public virtual void  TestMethod()
		{
			RAMDirectory directory = new RAMDirectory();
			
			System.String[] values = new System.String[]{"1", "2", "3", "4"};
			
			try
			{
				IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
				for (int i = 0; i < values.Length; i++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
					doc.Add(new Field(FIELD, values[i], Field.Store.YES, Field.Index.NOT_ANALYZED));
					writer.AddDocument(doc);
				}
				writer.Close();
				
				BooleanQuery boolQuery1 = new BooleanQuery();
				boolQuery1.Add(new TermQuery(new Term(FIELD, "1")), BooleanClause.Occur.SHOULD);
				boolQuery1.Add(new TermQuery(new Term(FIELD, "2")), BooleanClause.Occur.SHOULD);
				
				BooleanQuery query = new BooleanQuery();
				query.Add(boolQuery1, BooleanClause.Occur.MUST);
				query.Add(new TermQuery(new Term(FIELD, "9")), BooleanClause.Occur.MUST_NOT);
				
				IndexSearcher indexSearcher = new IndexSearcher(directory);
				ScoreDoc[] hits = indexSearcher.Search(query, null, 1000).scoreDocs;
				Assert.AreEqual(2, hits.Length, "Number of matched documents");
			}
			catch (System.IO.IOException e)
			{
				Assert.Fail(e.Message);
			}
		}
	}
}