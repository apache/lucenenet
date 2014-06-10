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


	using Assert = junit.framework.Assert;

	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// Utility class for asserting expected hits in tests.
	/// </summary>
	public class CheckHits
	{

	  /// <summary>
	  /// Some explains methods calculate their values though a slightly
	  /// different  order of operations from the actual scoring method ...
	  /// this allows for a small amount of relative variation
	  /// </summary>
	  public static float EXPLAIN_SCORE_TOLERANCE_DELTA = 0.001f;

	  /// <summary>
	  /// In general we use a relative epsilon, but some tests do crazy things
	  /// like boost documents with 0, creating tiny tiny scores where the
	  /// relative difference is large but the absolute difference is tiny.
	  /// we ensure the the epsilon is always at least this big.
	  /// </summary>
	  public static float EXPLAIN_SCORE_TOLERANCE_MINIMUM = 1e-6f;

	  /// <summary>
	  /// Tests that all documents up to maxDoc which are *not* in the
	  /// expected result set, have an explanation which indicates that 
	  /// the document does not match
	  /// </summary>
	  public static void CheckNoMatchExplanations(Query q, string defaultFieldName, IndexSearcher searcher, int[] results)
	  {

		string d = q.ToString(defaultFieldName);
		Set<int?> ignore = new SortedSet<int?>();
		for (int i = 0; i < results.Length; i++)
		{
		  ignore.add(Convert.ToInt32(results[i]));
		}

		int maxDoc = searcher.IndexReader.maxDoc();
		for (int doc = 0; doc < maxDoc; doc++)
		{
		  if (ignore.contains(Convert.ToInt32(doc)))
		  {
			  continue;
		  }

		  Explanation exp = searcher.explain(q, doc);
		  Assert.Assert.IsNotNull("Explanation of [[" + d + "]] for #" + doc + " is null", exp);
		  Assert.Assert.IsFalse("Explanation of [[" + d + "]] for #" + doc + " doesn't indicate non-match: " + exp.ToString(), exp.Match);
		}

	  }

	  /// <summary>
	  /// Tests that a query matches the an expected set of documents using a
	  /// HitCollector.
	  /// 
	  /// <p>
	  /// Note that when using the HitCollector API, documents will be collected
	  /// if they "match" regardless of what their score is.
	  /// </p> </summary>
	  /// <param name="query"> the query to test </param>
	  /// <param name="searcher"> the searcher to test the query against </param>
	  /// <param name="defaultFieldName"> used for displaying the query in assertion messages </param>
	  /// <param name="results"> a list of documentIds that must match the query </param>
	  /// <seealso cref= #checkHits </seealso>
	  public static void CheckHitCollector(Random random, Query query, string defaultFieldName, IndexSearcher searcher, int[] results)
	  {

		QueryUtils.Check(random,query,searcher);

		Set<int?> correct = new SortedSet<int?>();
		for (int i = 0; i < results.Length; i++)
		{
		  correct.add(Convert.ToInt32(results[i]));
		}
		Set<int?> actual = new SortedSet<int?>();
		Collector c = new SetCollector(actual);

		searcher.search(query, c);
		Assert.Assert.AreEqual("Simple: " + query.ToString(defaultFieldName), correct, actual);

		for (int i = -1; i < 2; i++)
		{
		  actual.clear();
		  IndexSearcher s = QueryUtils.WrapUnderlyingReader(random, searcher, i);
		  s.search(query, c);
		  Assert.Assert.AreEqual("Wrap Reader " + i + ": " + query.ToString(defaultFieldName), correct, actual);
		}
	  }

	  /// <summary>
	  /// Just collects document ids into a set.
	  /// </summary>
	  public class SetCollector : Collector
	  {
		internal readonly Set<int?> Bag;
		public SetCollector(Set<int?> bag)
		{
		  this.Bag = bag;
		}
		internal int @base = 0;
		public override Scorer Scorer
		{
			set
			{
			}
		}
		public override void Collect(int doc)
		{
		  Bag.add(Convert.ToInt32(doc + @base));
		}
		public override AtomicReaderContext NextReader
		{
			set
			{
			  @base = value.docBase;
			}
		}
		public override bool AcceptsDocsOutOfOrder()
		{
		  return true;
		}
	  }

	  /// <summary>
	  /// Tests that a query matches the an expected set of documents using Hits.
	  /// 
	  /// <p>
	  /// Note that when using the Hits API, documents will only be returned
	  /// if they have a positive normalized score.
	  /// </p> </summary>
	  /// <param name="query"> the query to test </param>
	  /// <param name="searcher"> the searcher to test the query against </param>
	  /// <param name="defaultFieldName"> used for displaing the query in assertion messages </param>
	  /// <param name="results"> a list of documentIds that must match the query </param>
	  /// <seealso cref= #checkHitCollector </seealso>
	  public static void CheckHits(Random random, Query query, string defaultFieldName, IndexSearcher searcher, int[] results)
	  {

		ScoreDoc[] hits = searcher.search(query, 1000).scoreDocs;

		Set<int?> correct = new SortedSet<int?>();
		for (int i = 0; i < results.Length; i++)
		{
		  correct.add(Convert.ToInt32(results[i]));
		}

		Set<int?> actual = new SortedSet<int?>();
		for (int i = 0; i < hits.Length; i++)
		{
		  actual.add(Convert.ToInt32(hits[i].doc));
		}

		Assert.Assert.AreEqual(query.ToString(defaultFieldName), correct, actual);

		QueryUtils.Check(random, query,searcher, LuceneTestCase.Rarely(random));
	  }

	  /// <summary>
	  /// Tests that a Hits has an expected order of documents </summary>
	  public static void CheckDocIds(string mes, int[] results, ScoreDoc[] hits)
	  {
		Assert.Assert.AreEqual(mes + " nr of hits", hits.Length, results.Length);
		for (int i = 0; i < results.Length; i++)
		{
		  Assert.Assert.AreEqual(mes + " doc nrs for hit " + i, results[i], hits[i].doc);
		}
	  }

	  /// <summary>
	  /// Tests that two queries have an expected order of documents,
	  /// and that the two queries have the same score values.
	  /// </summary>
	  public static void CheckHitsQuery(Query query, ScoreDoc[] hits1, ScoreDoc[] hits2, int[] results)
	  {

		CheckDocIds("hits1", results, hits1);
		CheckDocIds("hits2", results, hits2);
		CheckEqual(query, hits1, hits2);
	  }

	  public static void CheckEqual(Query query, ScoreDoc[] hits1, ScoreDoc[] hits2)
	  {
		 const float scoreTolerance = 1.0e-6f;
		 if (hits1.Length != hits2.Length)
		 {
		   Assert.Assert.Fail("Unequal lengths: hits1=" + hits1.Length + ",hits2=" + hits2.Length);
		 }
		for (int i = 0; i < hits1.Length; i++)
		{
		  if (hits1[i].doc != hits2[i].doc)
		  {
			Assert.Assert.Fail("Hit " + i + " docnumbers don't match\n" + Hits2str(hits1, hits2,0,0) + "for query:" + query.ToString());
		  }

		  if ((hits1[i].doc != hits2[i].doc) || Math.Abs(hits1[i].score - hits2[i].score) > scoreTolerance)
		  {
			Assert.Assert.Fail("Hit " + i + ", doc nrs " + hits1[i].doc + " and " + hits2[i].doc + "\nunequal       : " + hits1[i].score + "\n           and: " + hits2[i].score + "\nfor query:" + query.ToString());
		  }
		}
	  }

	  public static string Hits2str(ScoreDoc[] hits1, ScoreDoc[] hits2, int start, int end)
	  {
		StringBuilder sb = new StringBuilder();
		int len1 = hits1 == null ? 0 : hits1.Length;
		int len2 = hits2 == null ? 0 : hits2.Length;
		if (end <= 0)
		{
		  end = Math.Max(len1,len2);
		}

		  sb.Append("Hits length1=").Append(len1).Append("\tlength2=").Append(len2);

		sb.Append('\n');
		for (int i = start; i < end; i++)
		{
			sb.Append("hit=").Append(i).Append(':');
		  if (i < len1)
		  {
			  sb.Append(" doc").Append(hits1[i].doc).Append('=').Append(hits1[i].score);
		  }
		  else
		  {
			sb.Append("               ");
		  }
		  sb.Append(",\t");
		  if (i < len2)
		  {
			sb.Append(" doc").Append(hits2[i].doc).Append('=').Append(hits2[i].score);
		  }
		  sb.Append('\n');
		}
		return sb.ToString();
	  }


	  public static string TopdocsString(TopDocs docs, int start, int end)
	  {
		StringBuilder sb = new StringBuilder();
		  sb.Append("TopDocs totalHits=").Append(docs.totalHits).Append(" top=").Append(docs.scoreDocs.length).Append('\n');
		if (end <= 0)
		{
			end = docs.scoreDocs.length;
		}
		else
		{
			end = Math.Min(end,docs.scoreDocs.length);
		}
		for (int i = start; i < end; i++)
		{
		  sb.Append('\t');
		  sb.Append(i);
		  sb.Append(") doc=");
		  sb.Append(docs.scoreDocs[i].doc);
		  sb.Append("\tscore=");
		  sb.Append(docs.scoreDocs[i].score);
		  sb.Append('\n');
		}
		return sb.ToString();
	  }

	  /// <summary>
	  /// Asserts that the explanation value for every document matching a
	  /// query corresponds with the true score. 
	  /// </summary>
	  /// <seealso cref= ExplanationAsserter </seealso>
	  /// <seealso cref= #checkExplanations(Query, String, IndexSearcher, boolean) for a
	  /// "deep" testing of the explanation details.
	  /// </seealso>
	  /// <param name="query"> the query to test </param>
	  /// <param name="searcher"> the searcher to test the query against </param>
	  /// <param name="defaultFieldName"> used for displaing the query in assertion messages </param>
	  public static void CheckExplanations(Query query, string defaultFieldName, IndexSearcher searcher)
	  {
		CheckExplanations(query, defaultFieldName, searcher, false);
	  }

	  /// <summary>
	  /// Asserts that the explanation value for every document matching a
	  /// query corresponds with the true score.  Optionally does "deep" 
	  /// testing of the explanation details.
	  /// </summary>
	  /// <seealso cref= ExplanationAsserter </seealso>
	  /// <param name="query"> the query to test </param>
	  /// <param name="searcher"> the searcher to test the query against </param>
	  /// <param name="defaultFieldName"> used for displaing the query in assertion messages </param>
	  /// <param name="deep"> indicates whether a deep comparison of sub-Explanation details should be executed </param>
	  public static void CheckExplanations(Query query, string defaultFieldName, IndexSearcher searcher, bool deep)
	  {

		searcher.search(query, new ExplanationAsserter(query, defaultFieldName, searcher, deep));

	  }

	  /// <summary>
	  /// returns a reasonable epsilon for comparing two floats,
	  ///  where minor differences are acceptable such as score vs. explain 
	  /// </summary>
	  public static float ExplainToleranceDelta(float f1, float f2)
	  {
		return Math.Max(EXPLAIN_SCORE_TOLERANCE_MINIMUM, Math.Max(Math.Abs(f1), Math.Abs(f2)) * EXPLAIN_SCORE_TOLERANCE_DELTA);
	  }

	  /// <summary>
	  /// Assert that an explanation has the expected score, and optionally that its
	  /// sub-details max/sum/factor match to that score.
	  /// </summary>
	  /// <param name="q"> String representation of the query for assertion messages </param>
	  /// <param name="doc"> Document ID for assertion messages </param>
	  /// <param name="score"> Real score value of doc with query q </param>
	  /// <param name="deep"> indicates whether a deep comparison of sub-Explanation details should be executed </param>
	  /// <param name="expl"> The Explanation to match against score </param>
	  public static void VerifyExplanation(string q, int doc, float score, bool deep, Explanation expl)
	  {
		float value = expl.Value;
		Assert.Assert.AreEqual(q + ": score(doc=" + doc + ")=" + score + " != explanationScore=" + value + " Explanation: " + expl, score,value,ExplainToleranceDelta(score, value));

		if (!deep)
		{
			return;
		}

		Explanation[] detail = expl.Details;
		// TODO: can we improve this entire method? its really geared to work only with TF/IDF
		if (expl.Description.EndsWith("computed from:"))
		{
		  return; // something more complicated.
		}
		if (detail != null)
		{
		  if (detail.Length == 1)
		  {
			// simple containment, unless its a freq of: (which lets a query explain how the freq is calculated), 
			// just verify contained expl has same score
			if (!expl.Description.EndsWith("with freq of:"))
			{
			  VerifyExplanation(q,doc,score,deep,detail[0]);
			}
		  }
		  else
		  {
			// explanation must either:
			// - end with one of: "product of:", "sum of:", "max of:", or
			// - have "max plus <x> times others" (where <x> is float).
			float x = 0;
			string descr = expl.Description.ToLower(Locale.ROOT);
			bool productOf = descr.EndsWith("product of:");
			bool sumOf = descr.EndsWith("sum of:");
			bool maxOf = descr.EndsWith("max of:");
			bool maxTimesOthers = false;
			if (!(productOf || sumOf || maxOf))
			{
			  // maybe 'max plus x times others'
			  int k1 = descr.IndexOf("max plus ");
			  if (k1 >= 0)
			  {
				k1 += "max plus ".Length;
				int k2 = descr.IndexOf(" ",k1);
				try
				{
				  x = Convert.ToSingle(descr.Substring(k1, k2 - k1).Trim());
				  if (descr.Substring(k2).Trim().Equals("times others of:"))
				  {
					maxTimesOthers = true;
				  }
				}
				catch (NumberFormatException e)
				{
				}
			  }
			}
			// TODO: this is a TERRIBLE assertion!!!!
			Assert.Assert.IsTrue(q + ": multi valued explanation description=\"" + descr + "\" must be 'max of plus x times others' or end with 'product of'" + " or 'sum of:' or 'max of:' - " + expl, productOf || sumOf || maxOf || maxTimesOthers);
			float sum = 0;
			float product = 1;
			float max = 0;
			for (int i = 0; i < detail.Length; i++)
			{
			  float dval = detail[i].Value;
			  VerifyExplanation(q,doc,dval,deep,detail[i]);
			  product *= dval;
			  sum += dval;
			  max = Math.Max(max,dval);
			}
			float combined = 0;
			if (productOf)
			{
			  combined = product;
			}
			else if (sumOf)
			{
			  combined = sum;
			}
			else if (maxOf)
			{
			  combined = max;
			}
			else if (maxTimesOthers)
			{
			  combined = max + x * (sum - max);
			}
			else
			{
				Assert.Assert.IsTrue("should never get here!",false);
			}
			Assert.Assert.AreEqual(q + ": actual subDetails combined==" + combined + " != value=" + value + " Explanation: " + expl, combined,value,ExplainToleranceDelta(combined, value));
		  }
		}
	  }

	  /// <summary>
	  /// an IndexSearcher that implicitly checks hte explanation of every match
	  /// whenever it executes a search.
	  /// </summary>
	  /// <seealso cref= ExplanationAsserter </seealso>
	  public class ExplanationAssertingSearcher : IndexSearcher
	  {
		public ExplanationAssertingSearcher(IndexReader r) : base(r)
		{
		}
		protected internal virtual void CheckExplanations(Query q)
		{
		  base.search(q, null, new ExplanationAsserter(q, null, this));
		}
		public override TopFieldDocs Search(Query query, Filter filter, int n, Sort sort)
		{

		  CheckExplanations(query);
		  return base.search(query,filter,n,sort);
		}
		public override void Search(Query query, Collector results)
		{
		  CheckExplanations(query);
		  base.search(query, results);
		}
		public override void Search(Query query, Filter filter, Collector results)
		{
		  CheckExplanations(query);
		  base.search(query, filter, results);
		}
		public override TopDocs Search(Query query, Filter filter, int n)
		{

		  CheckExplanations(query);
		  return base.search(query,filter, n);
		}
	  }

	  /// <summary>
	  /// Asserts that the score explanation for every document matching a
	  /// query corresponds with the true score.
	  /// 
	  /// NOTE: this HitCollector should only be used with the Query and Searcher
	  /// specified at when it is constructed.
	  /// </summary>
	  /// <seealso cref= CheckHits#verifyExplanation </seealso>
	  public class ExplanationAsserter : Collector
	  {

		internal Query q;
		internal IndexSearcher s;
		internal string d;
		internal bool Deep;

		internal Scorer Scorer_Renamed;
		internal int @base = 0;

		/// <summary>
		/// Constructs an instance which does shallow tests on the Explanation </summary>
		public ExplanationAsserter(Query q, string defaultFieldName, IndexSearcher s) : this(q,defaultFieldName,s,false)
		{
		}
		public ExplanationAsserter(Query q, string defaultFieldName, IndexSearcher s, bool deep)
		{
		  this.q = q;
		  this.s = s;
		  this.d = q.ToString(defaultFieldName);
		  this.Deep = deep;
		}

		public override Scorer Scorer
		{
			set
			{
			  this.Scorer_Renamed = value;
			}
		}

		public override void Collect(int doc)
		{
		  Explanation exp = null;
		  doc = doc + @base;
		  try
		  {
			exp = s.explain(q, doc);
		  }
		  catch (IOException e)
		  {
			throw new Exception("exception in hitcollector of [[" + d + "]] for #" + doc, e);
		  }

		  Assert.Assert.IsNotNull("Explanation of [[" + d + "]] for #" + doc + " is null", exp);
		  VerifyExplanation(d,doc,Scorer_Renamed.score(),Deep,exp);
		  Assert.Assert.IsTrue("Explanation of [[" + d + "]] for #" + doc + " does not indicate match: " + exp.ToString(), exp.Match);
		}
		public override AtomicReaderContext NextReader
		{
			set
			{
			  @base = value.docBase;
			}
		}
		public override bool AcceptsDocsOutOfOrder()
		{
		  return true;
		}
	  }

	}



}