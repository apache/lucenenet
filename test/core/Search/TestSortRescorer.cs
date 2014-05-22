using System;
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
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Lucene3x") public class TestSortRescorer extends Lucene.Net.Util.LuceneTestCase
	public class TestSortRescorer : LuceneTestCase
	{
	  internal IndexSearcher Searcher;
	  internal DirectoryReader Reader;
	  internal Directory Dir;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), Dir);

		Document doc = new Document();
		doc.add(newStringField("id", "1", Field.Store.YES));
		doc.add(newTextField("body", "some contents and more contents", Field.Store.NO));
		doc.add(new NumericDocValuesField("popularity", 5));
		iw.addDocument(doc);

		doc = new Document();
		doc.add(newStringField("id", "2", Field.Store.YES));
		doc.add(newTextField("body", "another document with different contents", Field.Store.NO));
		doc.add(new NumericDocValuesField("popularity", 20));
		iw.addDocument(doc);

		doc = new Document();
		doc.add(newStringField("id", "3", Field.Store.YES));
		doc.add(newTextField("body", "crappy contents", Field.Store.NO));
		doc.add(new NumericDocValuesField("popularity", 2));
		iw.addDocument(doc);

		Reader = iw.Reader;
		Searcher = new IndexSearcher(Reader);
		iw.close();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  public virtual void TestBasic()
	  {

		// create a sort field and sort by it (reverse order)
		Query query = new TermQuery(new Term("body", "contents"));
		IndexReader r = Searcher.IndexReader;

		// Just first pass query
		TopDocs hits = Searcher.search(query, 10);
		Assert.AreEqual(3, hits.totalHits);
		Assert.AreEqual("3", r.document(hits.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("1", r.document(hits.scoreDocs[1].doc).get("id"));
		Assert.AreEqual("2", r.document(hits.scoreDocs[2].doc).get("id"));

		// Now, rescore:
		Sort sort = new Sort(new SortField("popularity", SortField.Type.INT, true));
		Rescorer rescorer = new SortRescorer(sort);
		hits = rescorer.rescore(Searcher, hits, 10);
		Assert.AreEqual(3, hits.totalHits);
		Assert.AreEqual("2", r.document(hits.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("1", r.document(hits.scoreDocs[1].doc).get("id"));
		Assert.AreEqual("3", r.document(hits.scoreDocs[2].doc).get("id"));

		string expl = rescorer.explain(Searcher, Searcher.explain(query, hits.scoreDocs[0].doc), hits.scoreDocs[0].doc).ToString();

		// Confirm the explanation breaks out the individual
		// sort fields:
		Assert.IsTrue(expl.Contains("= sort field <int: \"popularity\">! value=20"));

		// Confirm the explanation includes first pass details:
		Assert.IsTrue(expl.Contains("= first pass score"));
		Assert.IsTrue(expl.Contains("body:contents in"));
	  }

	  public virtual void TestRandom()
	  {
		Directory dir = newDirectory();
		int numDocs = atLeast(1000);
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		int[] idToNum = new int[numDocs];
		int maxValue = TestUtil.Next(random(), 10, 1000000);
		for (int i = 0;i < numDocs;i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + i, Field.Store.YES));
		  int numTokens = TestUtil.Next(random(), 1, 10);
		  StringBuilder b = new StringBuilder();
		  for (int j = 0;j < numTokens;j++)
		  {
			b.Append("a ");
		  }
		  doc.add(newTextField("field", b.ToString(), Field.Store.NO));
		  idToNum[i] = random().Next(maxValue);
		  doc.add(new NumericDocValuesField("num", idToNum[i]));
		  w.addDocument(doc);
		}
		IndexReader r = w.Reader;
		w.close();

		IndexSearcher s = newSearcher(r);
		int numHits = TestUtil.Next(random(), 1, numDocs);
		bool reverse = random().nextBoolean();

		TopDocs hits = s.search(new TermQuery(new Term("field", "a")), numHits);

		Rescorer rescorer = new SortRescorer(new Sort(new SortField("num", SortField.Type.INT, reverse)));
		TopDocs hits2 = rescorer.rescore(s, hits, numHits);

		int?[] expected = new int?[numHits];
		for (int i = 0;i < numHits;i++)
		{
		  expected[i] = hits.scoreDocs[i].doc;
		}

		int reverseInt = reverse ? - 1 : 1;

		Arrays.sort(expected, new ComparatorAnonymousInnerClassHelper(this, idToNum, r, reverseInt));

		bool fail = false;
		for (int i = 0;i < numHits;i++)
		{
		  fail |= (int)expected[i] != hits2.scoreDocs[i].doc;
		}
		Assert.IsFalse(fail);

		r.close();
		dir.close();
	  }

	  private class ComparatorAnonymousInnerClassHelper : IComparer<int?>
	  {
		  private readonly TestSortRescorer OuterInstance;

		  private int[] IdToNum;
		  private IndexReader r;
		  private int ReverseInt;

		  public ComparatorAnonymousInnerClassHelper(TestSortRescorer outerInstance, int[] idToNum, IndexReader r, int reverseInt)
		  {
			  this.OuterInstance = outerInstance;
			  this.IdToNum = idToNum;
			  this.r = r;
			  this.ReverseInt = reverseInt;
		  }

		  public virtual int Compare(int? a, int? b)
		  {
			try
			{
			  int av = IdToNum[Convert.ToInt32(r.document(a).get("id"))];
			  int bv = IdToNum[Convert.ToInt32(r.document(b).get("id"))];
			  if (av < bv)
			  {
				return -ReverseInt;
			  }
			  else if (bv < av)
			  {
				return ReverseInt;
			  }
			  else
			  {
				// Tie break by docID
				return a - b;
			  }
			}
			catch (IOException ioe)
			{
			  throw new Exception(ioe);
			}
		  }
	  }
	}

}