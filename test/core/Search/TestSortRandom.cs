using System;
using System.Collections.Generic;

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
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using StoredField = Lucene.Net.Document.StoredField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Occur = Lucene.Net.Search.BooleanClause.Occur;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// random sorting tests </summary>
	public class TestSortRandom : LuceneTestCase
	{

	  public virtual void TestRandomStringSort()
	  {
		Random random = new Random(random().nextLong());

		int NUM_DOCS = atLeast(100);
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random, dir);
		bool allowDups = random.nextBoolean();
		Set<string> seen = new HashSet<string>();
		int maxLength = TestUtil.Next(random, 5, 100);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS + " maxLength=" + maxLength + " allowDups=" + allowDups);
		}

		int numDocs = 0;
		IList<BytesRef> docValues = new List<BytesRef>();
		// TODO: deletions
		while (numDocs < NUM_DOCS)
		{
		  Document doc = new Document();

		  // 10% of the time, the document is missing the value:
		  BytesRef br;
		  if (random().Next(10) != 7)
		  {
			string s;
			if (random.nextBoolean())
			{
			  s = TestUtil.randomSimpleString(random, maxLength);
			}
			else
			{
			  s = TestUtil.randomUnicodeString(random, maxLength);
			}

			if (!allowDups)
			{
			  if (seen.contains(s))
			  {
				continue;
			  }
			  seen.add(s);
			}

			if (VERBOSE)
			{
			  Console.WriteLine("  " + numDocs + ": s=" + s);
			}

			br = new BytesRef(s);
			if (defaultCodecSupportsDocValues())
			{
			  doc.add(new SortedDocValuesField("stringdv", br));
			  doc.add(new NumericDocValuesField("id", numDocs));
			}
			else
			{
			  doc.add(newStringField("id", Convert.ToString(numDocs), Field.Store.NO));
			}
			doc.add(newStringField("string", s, Field.Store.NO));
			docValues.Add(br);

		  }
		  else
		  {
			br = null;
			if (VERBOSE)
			{
			  Console.WriteLine("  " + numDocs + ": <missing>");
			}
			docValues.Add(null);
			if (defaultCodecSupportsDocValues())
			{
			  doc.add(new NumericDocValuesField("id", numDocs));
			}
			else
			{
			  doc.add(newStringField("id", Convert.ToString(numDocs), Field.Store.NO));
			}
		  }

		  doc.add(new StoredField("id", numDocs));
		  writer.addDocument(doc);
		  numDocs++;

		  if (random.Next(40) == 17)
		  {
			// force flush
			writer.Reader.close();
		  }
		}

		IndexReader r = writer.Reader;
		writer.close();
		if (VERBOSE)
		{
		  Console.WriteLine("  reader=" + r);
		}

		IndexSearcher s = newSearcher(r, false);
		int ITERS = atLeast(100);
		for (int iter = 0;iter < ITERS;iter++)
		{
		  bool reverse = random.nextBoolean();

		  TopFieldDocs hits;
		  SortField sf;
		  bool sortMissingLast;
		  bool missingIsNull;
		  if (defaultCodecSupportsDocValues() && random.nextBoolean())
		  {
			sf = new SortField("stringdv", SortField.Type.STRING, reverse);
			// Can only use sort missing if the DVFormat
			// supports docsWithField:
			sortMissingLast = defaultCodecSupportsDocsWithField() && random().nextBoolean();
			missingIsNull = defaultCodecSupportsDocsWithField();
		  }
		  else
		  {
			sf = new SortField("string", SortField.Type.STRING, reverse);
			sortMissingLast = random().nextBoolean();
			missingIsNull = true;
		  }
		  if (sortMissingLast)
		  {
			sf.MissingValue = SortField.STRING_LAST;
		  }

		  Sort sort;
		  if (random.nextBoolean())
		  {
			sort = new Sort(sf);
		  }
		  else
		  {
			sort = new Sort(sf, SortField.FIELD_DOC);
		  }
		  int hitCount = TestUtil.Next(random, 1, r.maxDoc() + 20);
		  RandomFilter f = new RandomFilter(random, random.nextFloat(), docValues);
		  int queryType = random.Next(3);
		  if (queryType == 0)
		  {
			// force out of order
			BooleanQuery bq = new BooleanQuery();
			// Add a Query with SHOULD, since bw.scorer() returns BooleanScorer2
			// which delegates to BS if there are no mandatory clauses.
			bq.add(new MatchAllDocsQuery(), Occur.SHOULD);
			// Set minNrShouldMatch to 1 so that BQ will not optimize rewrite to return
			// the clause instead of BQ.
			bq.MinimumNumberShouldMatch = 1;
			hits = s.search(bq, f, hitCount, sort, random.nextBoolean(), random.nextBoolean());
		  }
		  else if (queryType == 1)
		  {
			hits = s.search(new ConstantScoreQuery(f), null, hitCount, sort, random.nextBoolean(), random.nextBoolean());
		  }
		  else
		  {
			hits = s.search(new MatchAllDocsQuery(), f, hitCount, sort, random.nextBoolean(), random.nextBoolean());
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter=" + iter + " " + hits.totalHits + " hits; topN=" + hitCount + "; reverse=" + reverse + "; sortMissingLast=" + sortMissingLast + " sort=" + sort);
		  }

		  // Compute expected results:
		  f.MatchValues.Sort(new ComparatorAnonymousInnerClassHelper(this, sortMissingLast));

		  if (reverse)
		  {
			f.MatchValues.Reverse();
		  }
		  IList<BytesRef> expected = f.MatchValues;
		  if (VERBOSE)
		  {
			Console.WriteLine("  expected:");
			for (int idx = 0;idx < expected.Count;idx++)
			{
			  BytesRef br = expected[idx];
			  if (br == null && missingIsNull == false)
			  {
				br = new BytesRef();
			  }
			  Console.WriteLine("    " + idx + ": " + (br == null ? "<missing>" : br.utf8ToString()));
			  if (idx == hitCount - 1)
			  {
				break;
			  }
			}
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("  actual:");
			for (int hitIDX = 0;hitIDX < hits.scoreDocs.length;hitIDX++)
			{
			  FieldDoc fd = (FieldDoc) hits.scoreDocs[hitIDX];
			  BytesRef br = (BytesRef) fd.fields[0];

			  Console.WriteLine("    " + hitIDX + ": " + (br == null ? "<missing>" : br.utf8ToString()) + " id=" + s.doc(fd.doc).get("id"));
			}
		  }
		  for (int hitIDX = 0;hitIDX < hits.scoreDocs.length;hitIDX++)
		  {
			FieldDoc fd = (FieldDoc) hits.scoreDocs[hitIDX];
			BytesRef br = expected[hitIDX];
			if (br == null && missingIsNull == false)
			{
			  br = new BytesRef();
			}

			// Normally, the old codecs (that don't support
			// docsWithField via doc values) will always return
			// an empty BytesRef for the missing case; however,
			// if all docs in a given segment were missing, in
			// that case it will return null!  So we must map
			// null here, too:
			BytesRef br2 = (BytesRef) fd.fields[0];
			if (br2 == null && missingIsNull == false)
			{
			  br2 = new BytesRef();
			}

			Assert.AreEqual("hit=" + hitIDX + " has wrong sort value", br, br2);
		  }
		}

		r.close();
		dir.close();
	  }

	  private class ComparatorAnonymousInnerClassHelper : IComparer<BytesRef>
	  {
		  private readonly TestSortRandom OuterInstance;

		  private bool SortMissingLast;

		  public ComparatorAnonymousInnerClassHelper(TestSortRandom outerInstance, bool sortMissingLast)
		  {
			  this.OuterInstance = outerInstance;
			  this.SortMissingLast = sortMissingLast;
		  }

		  public virtual int Compare(BytesRef a, BytesRef b)
		  {
			if (a == null)
			{
			  if (b == null)
			  {
				return 0;
			  }
			  if (SortMissingLast)
			  {
				return 1;
			  }
			  else
			  {
				return -1;
			  }
			}
			else if (b == null)
			{
			  if (SortMissingLast)
			  {
				return -1;
			  }
			  else
			  {
				return 1;
			  }
			}
			else
			{
			  return a.compareTo(b);
			}
		  }
	  }

	  private class RandomFilter : Filter
	  {
		internal readonly Random Random;
		internal float Density;
		internal readonly IList<BytesRef> DocValues;
		public readonly IList<BytesRef> MatchValues = Collections.synchronizedList(new List<BytesRef>());

		// density should be 0.0 ... 1.0
		public RandomFilter(Random random, float density, IList<BytesRef> docValues)
		{
		  this.Random = random;
		  this.Density = density;
		  this.DocValues = docValues;
		}

		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		{
		  int maxDoc = context.reader().maxDoc();
		  FieldCache.Ints idSource = FieldCache.DEFAULT.getInts(context.reader(), "id", false);
		  Assert.IsNotNull(idSource);
		  FixedBitSet bits = new FixedBitSet(maxDoc);
		  for (int docID = 0;docID < maxDoc;docID++)
		  {
			if (Random.nextFloat() <= Density && (acceptDocs == null || acceptDocs.get(docID)))
			{
			  bits.set(docID);
			  //System.out.println("  acc id=" + idSource.get(docID) + " docID=" + docID + " id=" + idSource.get(docID) + " v=" + docValues.get(idSource.get(docID)).utf8ToString());
			  MatchValues.Add(DocValues[idSource.get(docID)]);
			}
		  }

		  return bits;
		}
	  }
	}

}