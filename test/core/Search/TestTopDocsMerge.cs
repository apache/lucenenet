using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search
{

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

	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FloatField = Lucene.Net.Document.FloatField;
	using IntField = Lucene.Net.Document.IntField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using CompositeReaderContext = Lucene.Net.Index.CompositeReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using ReaderUtil = Lucene.Net.Index.ReaderUtil;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;


	public class TestTopDocsMerge : LuceneTestCase
	{

	  private class ShardSearcher : IndexSearcher
	  {
		internal readonly IList<AtomicReaderContext> Ctx;

		public ShardSearcher(AtomicReaderContext ctx, IndexReaderContext parent) : base(parent)
		{
		  this.Ctx = Collections.singletonList(ctx);
		}

		public virtual void Search(Weight weight, Collector collector)
		{
		  search(Ctx, weight, collector);
		}

		public virtual TopDocs Search(Weight weight, int topN)
		{
		  return search(Ctx, weight, null, topN);
		}

		public override string ToString()
		{
		  return "ShardSearcher(" + Ctx[0] + ")";
		}
	  }

	  public virtual void TestSort_1()
	  {
		TestSort(false);
	  }

	  public virtual void TestSort_2()
	  {
		TestSort(true);
	  }

	  internal virtual void TestSort(bool useFrom)
	  {

		IndexReader reader = null;
		Directory dir = null;

		int numDocs = atLeast(1000);
		//final int numDocs = atLeast(50);

		string[] tokens = new string[] {"a", "b", "c", "d", "e"};

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: make index");
		}

		{
		  dir = newDirectory();
		  RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		  // w.setDoRandomForceMerge(false);

		  // w.w.getConfig().setMaxBufferedDocs(atLeast(100));

		  string[] content = new string[atLeast(20)];

		  for (int contentIDX = 0;contentIDX < content.Length;contentIDX++)
		  {
			StringBuilder sb = new StringBuilder();
			int numTokens = TestUtil.Next(random(), 1, 10);
			for (int tokenIDX = 0;tokenIDX < numTokens;tokenIDX++)
			{
			  sb.Append(tokens[random().Next(tokens.Length)]).Append(' ');
			}
			content[contentIDX] = sb.ToString();
		  }

		  for (int docIDX = 0;docIDX < numDocs;docIDX++)
		  {
			Document doc = new Document();
			doc.add(newStringField("string", TestUtil.randomRealisticUnicodeString(random()), Field.Store.NO));
			doc.add(newTextField("text", content[random().Next(content.Length)], Field.Store.NO));
			doc.add(new FloatField("float", random().nextFloat(), Field.Store.NO));
			int intValue;
			if (random().Next(100) == 17)
			{
			  intValue = int.MinValue;
			}
			else if (random().Next(100) == 17)
			{
			  intValue = int.MaxValue;
			}
			else
			{
			  intValue = random().Next();
			}
			doc.add(new IntField("int", intValue, Field.Store.NO));
			if (VERBOSE)
			{
			  Console.WriteLine("  doc=" + doc);
			}
			w.addDocument(doc);
		  }

		  reader = w.Reader;
		  w.close();
		}

		// NOTE: sometimes reader has just one segment, which is
		// important to test
		IndexSearcher searcher = newSearcher(reader);
		IndexReaderContext ctx = searcher.TopReaderContext;

		ShardSearcher[] subSearchers;
		int[] docStarts;

		if (ctx is AtomicReaderContext)
		{
		  subSearchers = new ShardSearcher[1];
		  docStarts = new int[1];
		  subSearchers[0] = new ShardSearcher((AtomicReaderContext) ctx, ctx);
		  docStarts[0] = 0;
		}
		else
		{
		  CompositeReaderContext compCTX = (CompositeReaderContext) ctx;
		  int size = compCTX.leaves().size();
		  subSearchers = new ShardSearcher[size];
		  docStarts = new int[size];
		  int docBase = 0;
		  for (int searcherIDX = 0;searcherIDX < subSearchers.Length;searcherIDX++)
		  {
			AtomicReaderContext leave = compCTX.leaves().get(searcherIDX);
			subSearchers[searcherIDX] = new ShardSearcher(leave, compCTX);
			docStarts[searcherIDX] = docBase;
			docBase += leave.reader().maxDoc();
		  }
		}

		IList<SortField> sortFields = new List<SortField>();
		sortFields.Add(new SortField("string", SortField.Type.STRING, true));
		sortFields.Add(new SortField("string", SortField.Type.STRING, false));
		sortFields.Add(new SortField("int", SortField.Type.INT, true));
		sortFields.Add(new SortField("int", SortField.Type.INT, false));
		sortFields.Add(new SortField("float", SortField.Type.FLOAT, true));
		sortFields.Add(new SortField("float", SortField.Type.FLOAT, false));
		sortFields.Add(new SortField(null, SortField.Type.SCORE, true));
		sortFields.Add(new SortField(null, SortField.Type.SCORE, false));
		sortFields.Add(new SortField(null, SortField.Type.DOC, true));
		sortFields.Add(new SortField(null, SortField.Type.DOC, false));

		for (int iter = 0;iter < 1000 * RANDOM_MULTIPLIER;iter++)
		{

		  // TODO: custom FieldComp...
		  Query query = new TermQuery(new Term("text", tokens[random().Next(tokens.Length)]));

		  Sort sort;
		  if (random().Next(10) == 4)
		  {
			// Sort by score
			sort = null;
		  }
		  else
		  {
			SortField[] randomSortFields = new SortField[TestUtil.Next(random(), 1, 3)];
			for (int sortIDX = 0;sortIDX < randomSortFields.Length;sortIDX++)
			{
			  randomSortFields[sortIDX] = sortFields[random().Next(sortFields.Count)];
			}
			sort = new Sort(randomSortFields);
		  }

		  int numHits = TestUtil.Next(random(), 1, numDocs + 5);
		  //final int numHits = 5;

		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: search query=" + query + " sort=" + sort + " numHits=" + numHits);
		  }

		  int from = -1;
		  int size = -1;
		  // First search on whole index:
		  TopDocs topHits;
		  if (sort == null)
		  {
			if (useFrom)
			{
			  TopScoreDocCollector c = TopScoreDocCollector.create(numHits, random().nextBoolean());
			  searcher.search(query, c);
			  from = TestUtil.Next(random(), 0, numHits - 1);
			  size = numHits - from;
			  TopDocs tempTopHits = c.topDocs();
			  if (from < tempTopHits.scoreDocs.length)
			  {
				// Can't use TopDocs#topDocs(start, howMany), since it has different behaviour when start >= hitCount
				// than TopDocs#merge currently has
				ScoreDoc[] newScoreDocs = new ScoreDoc[Math.Min(size, tempTopHits.scoreDocs.length - from)];
				Array.Copy(tempTopHits.scoreDocs, from, newScoreDocs, 0, newScoreDocs.Length);
				tempTopHits.scoreDocs = newScoreDocs;
				topHits = tempTopHits;
			  }
			  else
			  {
				topHits = new TopDocs(tempTopHits.totalHits, new ScoreDoc[0], tempTopHits.MaxScore);
			  }
			}
			else
			{
			  topHits = searcher.search(query, numHits);
			}
		  }
		  else
		  {
			TopFieldCollector c = TopFieldCollector.create(sort, numHits, true, true, true, random().nextBoolean());
			searcher.search(query, c);
			if (useFrom)
			{
			  from = TestUtil.Next(random(), 0, numHits - 1);
			  size = numHits - from;
			  TopDocs tempTopHits = c.topDocs();
			  if (from < tempTopHits.scoreDocs.length)
			  {
				// Can't use TopDocs#topDocs(start, howMany), since it has different behaviour when start >= hitCount
				// than TopDocs#merge currently has
				ScoreDoc[] newScoreDocs = new ScoreDoc[Math.Min(size, tempTopHits.scoreDocs.length - from)];
				Array.Copy(tempTopHits.scoreDocs, from, newScoreDocs, 0, newScoreDocs.Length);
				tempTopHits.scoreDocs = newScoreDocs;
				topHits = tempTopHits;
			  }
			  else
			  {
				topHits = new TopDocs(tempTopHits.totalHits, new ScoreDoc[0], tempTopHits.MaxScore);
			  }
			}
			else
			{
			  topHits = c.topDocs(0, numHits);
			}
		  }

		  if (VERBOSE)
		  {
			if (useFrom)
			{
			  Console.WriteLine("from=" + from + " size=" + size);
			}
			Console.WriteLine("  top search: " + topHits.totalHits + " totalHits; hits=" + (topHits.scoreDocs == null ? "null" : topHits.scoreDocs.length + " maxScore=" + topHits.MaxScore));
			if (topHits.scoreDocs != null)
			{
			  for (int hitIDX = 0;hitIDX < topHits.scoreDocs.length;hitIDX++)
			  {
				ScoreDoc sd = topHits.scoreDocs[hitIDX];
				Console.WriteLine("    doc=" + sd.doc + " score=" + sd.score);
			  }
			}
		  }

		  // ... then all shards:
		  Weight w = searcher.createNormalizedWeight(query);

		  TopDocs[] shardHits = new TopDocs[subSearchers.Length];
		  for (int shardIDX = 0;shardIDX < subSearchers.Length;shardIDX++)
		  {
			TopDocs subHits;
			ShardSearcher subSearcher = subSearchers[shardIDX];
			if (sort == null)
			{
			  subHits = subSearcher.Search(w, numHits);
			}
			else
			{
			  TopFieldCollector c = TopFieldCollector.create(sort, numHits, true, true, true, random().nextBoolean());
			  subSearcher.Search(w, c);
			  subHits = c.topDocs(0, numHits);
			}

			shardHits[shardIDX] = subHits;
			if (VERBOSE)
			{
			  Console.WriteLine("  shard=" + shardIDX + " " + subHits.totalHits + " totalHits hits=" + (subHits.scoreDocs == null ? "null" : subHits.scoreDocs.length));
			  if (subHits.scoreDocs != null)
			  {
				foreach (ScoreDoc sd in subHits.scoreDocs)
				{
				  Console.WriteLine("    doc=" + sd.doc + " score=" + sd.score);
				}
			  }
			}
		  }

		  // Merge:
		  TopDocs mergedHits;
		  if (useFrom)
		  {
			mergedHits = TopDocs.merge(sort, from, size, shardHits);
		  }
		  else
		  {
			mergedHits = TopDocs.merge(sort, numHits, shardHits);
		  }

		  if (mergedHits.scoreDocs != null)
		  {
			// Make sure the returned shards are correct:
			for (int hitIDX = 0;hitIDX < mergedHits.scoreDocs.length;hitIDX++)
			{
			  ScoreDoc sd = mergedHits.scoreDocs[hitIDX];
			  Assert.AreEqual("doc=" + sd.doc + " wrong shard", ReaderUtil.subIndex(sd.doc, docStarts), sd.shardIndex);
			}
		  }

		  TestUtil.Assert.AreEqual(topHits, mergedHits);
		}
		reader.close();
		dir.close();
	  }
	}

}