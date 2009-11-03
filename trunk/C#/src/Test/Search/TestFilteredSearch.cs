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

using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using OpenBitSet = Lucene.Net.Util.OpenBitSet;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	
	/// <summary> </summary>
    [TestFixture]
	public class TestFilteredSearch:LuceneTestCase
	{
		
		public TestFilteredSearch(System.String name):base(name)
		{
		}
		
		private const System.String FIELD = "category";
		
		[Test]
		public virtual void  TestFilteredSearch_Renamed()
		{
			RAMDirectory directory = new RAMDirectory();
			int[] filterBits = new int[]{1, 36};
			Filter filter = new SimpleDocIdSetFilter(filterBits);
			
			
			try
			{
				IndexWriter writer = new IndexWriter(directory, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
				for (int i = 0; i < 60; i++)
				{
					//Simple docs
					Document doc = new Document();
					doc.Add(new Field(FIELD, System.Convert.ToString(i), Field.Store.YES, Field.Index.NOT_ANALYZED));
					writer.AddDocument(doc);
				}
				writer.Close();
				
				BooleanQuery booleanQuery = new BooleanQuery();
				booleanQuery.Add(new TermQuery(new Term(FIELD, "36")), BooleanClause.Occur.SHOULD);
				
				
				IndexSearcher indexSearcher = new IndexSearcher(directory);
				ScoreDoc[] hits = indexSearcher.Search(booleanQuery, filter, 1000).scoreDocs;
				Assert.AreEqual(1, hits.Length, "Number of matched documents");
			}
			catch (System.IO.IOException e)
			{
				Assert.Fail(e.Message);
			}
		}
		
		
		[Serializable]
		public sealed class SimpleDocIdSetFilter:Filter
		{
			private OpenBitSet bits;
			
			public SimpleDocIdSetFilter(int[] docs)
			{
				bits = new OpenBitSet();
				for (int i = 0; i < docs.Length; i++)
				{
					bits.Set(docs[i]);
				}
			}
			
			public override DocIdSet GetDocIdSet(IndexReader reader)
			{
				return bits;
			}
		}
	}
}