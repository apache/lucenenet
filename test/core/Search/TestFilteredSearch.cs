using System;

/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Lucene.Net.Search
{

	using Field = Lucene.Net.Document.Field;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using Term = Lucene.Net.Index.Term;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;



	/// 
	public class TestFilteredSearch : LuceneTestCase
	{

	  private const string FIELD = "category";

	  public virtual void TestFilteredSearch()
	  {
		bool enforceSingleSegment = true;
		Directory directory = newDirectory();
		int[] filterBits = new int[] {1, 36};
		SimpleDocIdSetFilter filter = new SimpleDocIdSetFilter(filterBits);
		IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		SearchFiltered(writer, directory, filter, enforceSingleSegment);
		// run the test on more than one segment
		enforceSingleSegment = false;
		writer.close();
		writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy()));
		// we index 60 docs - this will create 6 segments
		SearchFiltered(writer, directory, filter, enforceSingleSegment);
		writer.close();
		directory.close();
	  }

	  public virtual void SearchFiltered(IndexWriter writer, Directory directory, Filter filter, bool fullMerge)
	  {
		for (int i = 0; i < 60; i++) //Simple docs
		{
		  Document doc = new Document();
		  doc.add(newStringField(FIELD, Convert.ToString(i), Field.Store.YES));
		  writer.addDocument(doc);
		}
		if (fullMerge)
		{
		  writer.forceMerge(1);
		}
		writer.close();

		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(new TermQuery(new Term(FIELD, "36")), BooleanClause.Occur_e.SHOULD);


		IndexReader reader = DirectoryReader.open(directory);
		IndexSearcher indexSearcher = newSearcher(reader);
		ScoreDoc[] hits = indexSearcher.search(booleanQuery, filter, 1000).scoreDocs;
		Assert.AreEqual("Number of matched documents", 1, hits.Length);
		reader.close();
	  }

	  public sealed class SimpleDocIdSetFilter : Filter
	  {
		internal readonly int[] Docs;

		public SimpleDocIdSetFilter(int[] docs)
		{
		  this.Docs = docs;
		}

		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		{
		  assertNull("acceptDocs should be null, as we have an index without deletions", acceptDocs);
		  FixedBitSet set = new FixedBitSet(context.reader().maxDoc());
		  int docBase = context.docBase;
		  int limit = docBase + context.reader().maxDoc();
		  for (int index = 0;index < Docs.Length; index++)
		  {
			int docId = Docs[index];
			if (docId >= docBase && docId < limit)
			{
			  set.set(docId - docBase);
			}
		  }
		  return set.cardinality() == 0 ? null:set;
		}
	  }

	}

}