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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Lucene.Net.Index;
	using Entry = Lucene.Net.Search.FieldValueHitQueue.Entry;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Lucene.Net.Store;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using BytesRef = Lucene.Net.Util.BytesRef;

	public class TestElevationComparator : LuceneTestCase
	{

	  private readonly IDictionary<BytesRef, int?> Priority = new Dictionary<BytesRef, int?>();

	  //@Test
	  public virtual void TestSorting()
	  {
		Directory directory = newDirectory();
		IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(1000)).setSimilarity(new DefaultSimilarity()));
		writer.addDocument(Adoc(new string[] {"id", "a", "title", "ipod", "str_s", "a"}));
		writer.addDocument(Adoc(new string[] {"id", "b", "title", "ipod ipod", "str_s", "b"}));
		writer.addDocument(Adoc(new string[] {"id", "c", "title", "ipod ipod ipod", "str_s","c"}));
		writer.addDocument(Adoc(new string[] {"id", "x", "title", "boosted", "str_s", "x"}));
		writer.addDocument(Adoc(new string[] {"id", "y", "title", "boosted boosted", "str_s","y"}));
		writer.addDocument(Adoc(new string[] {"id", "z", "title", "boosted boosted boosted","str_s", "z"}));

		IndexReader r = DirectoryReader.open(writer, true);
		writer.close();

		IndexSearcher searcher = newSearcher(r);
		searcher.Similarity = new DefaultSimilarity();

		RunTest(searcher, true);
		RunTest(searcher, false);

		r.close();
		directory.close();
	  }

	  private void RunTest(IndexSearcher searcher, bool reversed)
	  {

		BooleanQuery newq = new BooleanQuery(false);
		TermQuery query = new TermQuery(new Term("title", "ipod"));

		newq.add(query, BooleanClause.Occur_e.SHOULD);
		newq.add(GetElevatedQuery(new string[] {"id", "a", "id", "x"}), BooleanClause.Occur_e.SHOULD);

		Sort sort = new Sort(new SortField("id", new ElevationComparatorSource(Priority), false), new SortField(null, SortField.Type.SCORE, reversed)
		 );

		TopDocsCollector<Entry> topCollector = TopFieldCollector.create(sort, 50, false, true, true, true);
		searcher.search(newq, null, topCollector);

		TopDocs topDocs = topCollector.topDocs(0, 10);
		int nDocsReturned = topDocs.scoreDocs.length;

		Assert.AreEqual(4, nDocsReturned);

		// 0 & 3 were elevated
		Assert.AreEqual(0, topDocs.scoreDocs[0].doc);
		Assert.AreEqual(3, topDocs.scoreDocs[1].doc);

		if (reversed)
		{
		  Assert.AreEqual(2, topDocs.scoreDocs[2].doc);
		  Assert.AreEqual(1, topDocs.scoreDocs[3].doc);
		}
		else
		{
		  Assert.AreEqual(1, topDocs.scoreDocs[2].doc);
		  Assert.AreEqual(2, topDocs.scoreDocs[3].doc);
		}

		/*
		for (int i = 0; i < nDocsReturned; i++) {
		 ScoreDoc scoreDoc = topDocs.scoreDocs[i];
		 ids[i] = scoreDoc.doc;
		 scores[i] = scoreDoc.score;
		 documents[i] = searcher.doc(ids[i]);
		 System.out.println("ids[i] = " + ids[i]);
		 System.out.println("documents[i] = " + documents[i]);
		 System.out.println("scores[i] = " + scores[i]);
	   }
		*/
	  }

	 private Query GetElevatedQuery(string[] vals)
	 {
	   BooleanQuery q = new BooleanQuery(false);
	   q.Boost = 0;
	   int max = (vals.Length / 2) + 5;
	   for (int i = 0; i < vals.Length - 1; i += 2)
	   {
		 q.add(new TermQuery(new Term(vals[i], vals[i + 1])), BooleanClause.Occur_e.SHOULD);
		 Priority[new BytesRef(vals[i + 1])] = Convert.ToInt32(max--);
		 // System.out.println(" pri doc=" + vals[i+1] + " pri=" + (1+max));
	   }
	   return q;
	 }

	 private Document Adoc(string[] vals)
	 {
	   Document doc = new Document();
	   for (int i = 0; i < vals.Length - 2; i += 2)
	   {
		 doc.add(newTextField(vals[i], vals[i + 1], Field.Store.YES));
	   }
	   return doc;
	 }
	}

	internal class ElevationComparatorSource : FieldComparatorSource
	{
	  private readonly IDictionary<BytesRef, int?> Priority;

	  public ElevationComparatorSource(IDictionary<BytesRef, int?> boosts)
	  {
	   this.Priority = boosts;
	  }

	  public override FieldComparator<int?> NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
	  {
	   return new FieldComparatorAnonymousInnerClassHelper(this, fieldname, numHits);
	  }

	 private class FieldComparatorAnonymousInnerClassHelper : FieldComparator<int?>
	 {
		 private readonly ElevationComparatorSource OuterInstance;

		 private string Fieldname;
		 private int NumHits;

		 public FieldComparatorAnonymousInnerClassHelper(ElevationComparatorSource outerInstance, string fieldname, int numHits)
		 {
			 this.OuterInstance = outerInstance;
			 this.Fieldname = fieldname;
			 this.NumHits = numHits;
			 values = new int[numHits];
			 tempBR = new BytesRef();
		 }


		 internal SortedDocValues idIndex;
		 private readonly int[] values;
		 private readonly BytesRef tempBR;
		 internal int bottomVal;

		 public override int Compare(int slot1, int slot2)
		 {
		   return values[slot2] - values[slot1]; // values will be small enough that there is no overflow concern
		 }

		 public override int Bottom
		 {
			 set
			 {
			   bottomVal = values[value];
			 }
		 }

		 public override int? TopValue
		 {
			 set
			 {
			   throw new System.NotSupportedException();
			 }
		 }

		 private int DocVal(int doc)
		 {
		   int ord = idIndex.getOrd(doc);
		   if (ord == -1)
		   {
			 return 0;
		   }
		   else
		   {
			 idIndex.lookupOrd(ord, tempBR);
			 int? prio = OuterInstance.Priority[tempBR];
			 return prio == null ? 0 : (int)prio;
		   }
		 }

		 public override int CompareBottom(int doc)
		 {
		   return docVal(doc) - bottomVal;
		 }

		 public override void Copy(int slot, int doc)
		 {
		   values[slot] = docVal(doc);
		 }

		 public override FieldComparator<int?> SetNextReader(AtomicReaderContext context)
		 {
		   idIndex = FieldCache.DEFAULT.getTermsIndex(context.reader(), Fieldname);
		   return this;
		 }

		 public override int? Value(int slot)
		 {
		   return Convert.ToInt32(values[slot]);
		 }

		 public override int CompareTop(int doc)
		 {
		   throw new System.NotSupportedException();
		 }
	 }
	}

}