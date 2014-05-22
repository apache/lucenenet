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

	using IndexReader = Lucene.Net.Index.IndexReader;
	using Term = Lucene.Net.Index.Term;
	using Test = org.junit.Test;

	/// <summary>
	/// A basic 'positive' Unit test class for the TermRangeFilter class.
	/// 
	/// <p>
	/// NOTE: at the moment, this class only tests for 'positive' results, it does
	/// not verify the results to ensure there are no 'false positives', nor does it
	/// adequately test 'negative' results. It also does not test that garbage in
	/// results in an Exception.
	/// </summary>
	public class TestTermRangeFilter : BaseTestRangeFilter
	{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRangeFilterId() throws java.io.IOException
	  public virtual void TestRangeFilterId()
	  {
		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		int medId = ((MaxId - MinId) / 2);

		string minIP = Pad(MinId);
		string maxIP = Pad(MaxId);
		string medIP = Pad(medId);

		int numDocs = reader.numDocs();

		Assert.AreEqual("num of docs", numDocs, 1 + MaxId - MinId);

		ScoreDoc[] result;
		Query q = new TermQuery(new Term("body", "body"));

		// test id, bounded on both ends

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, maxIP, T, T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, maxIP, T, F), numDocs).scoreDocs;
		Assert.AreEqual("all but last", numDocs - 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, maxIP, F, T), numDocs).scoreDocs;
		Assert.AreEqual("all but first", numDocs - 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, maxIP, F, F), numDocs).scoreDocs;
		Assert.AreEqual("all but ends", numDocs - 2, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", medIP, maxIP, T, T), numDocs).scoreDocs;
		Assert.AreEqual("med and up", 1 + MaxId - medId, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, medIP, T, T), numDocs).scoreDocs;
		Assert.AreEqual("up to med", 1 + medId - MinId, result.Length);

		// unbounded id

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, null, T, F), numDocs).scoreDocs;
		Assert.AreEqual("min and up", numDocs, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", null, maxIP, F, T), numDocs).scoreDocs;
		Assert.AreEqual("max and down", numDocs, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, null, F, F), numDocs).scoreDocs;
		Assert.AreEqual("not min, but up", numDocs - 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", null, maxIP, F, F), numDocs).scoreDocs;
		Assert.AreEqual("not max, but down", numDocs - 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", medIP, maxIP, T, F), numDocs).scoreDocs;
		Assert.AreEqual("med and up, not max", MaxId - medId, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, medIP, F, T), numDocs).scoreDocs;
		Assert.AreEqual("not min, up to med", medId - MinId, result.Length);

		// very small sets

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, minIP, F, F), numDocs).scoreDocs;
		Assert.AreEqual("min,min,F,F", 0, result.Length);
		result = search.search(q, TermRangeFilter.newStringRange("id", medIP, medIP, F, F), numDocs).scoreDocs;
		Assert.AreEqual("med,med,F,F", 0, result.Length);
		result = search.search(q, TermRangeFilter.newStringRange("id", maxIP, maxIP, F, F), numDocs).scoreDocs;
		Assert.AreEqual("max,max,F,F", 0, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", minIP, minIP, T, T), numDocs).scoreDocs;
		Assert.AreEqual("min,min,T,T", 1, result.Length);
		result = search.search(q, TermRangeFilter.newStringRange("id", null, minIP, F, T), numDocs).scoreDocs;
		Assert.AreEqual("nul,min,F,T", 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", maxIP, maxIP, T, T), numDocs).scoreDocs;
		Assert.AreEqual("max,max,T,T", 1, result.Length);
		result = search.search(q, TermRangeFilter.newStringRange("id", maxIP, null, T, F), numDocs).scoreDocs;
		Assert.AreEqual("max,nul,T,T", 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("id", medIP, medIP, T, T), numDocs).scoreDocs;
		Assert.AreEqual("med,med,T,T", 1, result.Length);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRangeFilterRand() throws java.io.IOException
	  public virtual void TestRangeFilterRand()
	  {

		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		string minRP = Pad(SignedIndexDir.MinR);
		string maxRP = Pad(SignedIndexDir.MaxR);

		int numDocs = reader.numDocs();

		Assert.AreEqual("num of docs", numDocs, 1 + MaxId - MinId);

		ScoreDoc[] result;
		Query q = new TermQuery(new Term("body", "body"));

		// test extremes, bounded on both ends

		result = search.search(q, TermRangeFilter.newStringRange("rand", minRP, maxRP, T, T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("rand", minRP, maxRP, T, F), numDocs).scoreDocs;
		Assert.AreEqual("all but biggest", numDocs - 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("rand", minRP, maxRP, F, T), numDocs).scoreDocs;
		Assert.AreEqual("all but smallest", numDocs - 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("rand", minRP, maxRP, F, F), numDocs).scoreDocs;
		Assert.AreEqual("all but extremes", numDocs - 2, result.Length);

		// unbounded

		result = search.search(q, TermRangeFilter.newStringRange("rand", minRP, null, T, F), numDocs).scoreDocs;
		Assert.AreEqual("smallest and up", numDocs, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("rand", null, maxRP, F, T), numDocs).scoreDocs;
		Assert.AreEqual("biggest and down", numDocs, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("rand", minRP, null, F, F), numDocs).scoreDocs;
		Assert.AreEqual("not smallest, but up", numDocs - 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("rand", null, maxRP, F, F), numDocs).scoreDocs;
		Assert.AreEqual("not biggest, but down", numDocs - 1, result.Length);

		// very small sets

		result = search.search(q, TermRangeFilter.newStringRange("rand", minRP, minRP, F, F), numDocs).scoreDocs;
		Assert.AreEqual("min,min,F,F", 0, result.Length);
		result = search.search(q, TermRangeFilter.newStringRange("rand", maxRP, maxRP, F, F), numDocs).scoreDocs;
		Assert.AreEqual("max,max,F,F", 0, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("rand", minRP, minRP, T, T), numDocs).scoreDocs;
		Assert.AreEqual("min,min,T,T", 1, result.Length);
		result = search.search(q, TermRangeFilter.newStringRange("rand", null, minRP, F, T), numDocs).scoreDocs;
		Assert.AreEqual("nul,min,F,T", 1, result.Length);

		result = search.search(q, TermRangeFilter.newStringRange("rand", maxRP, maxRP, T, T), numDocs).scoreDocs;
		Assert.AreEqual("max,max,T,T", 1, result.Length);
		result = search.search(q, TermRangeFilter.newStringRange("rand", maxRP, null, T, F), numDocs).scoreDocs;
		Assert.AreEqual("max,nul,T,T", 1, result.Length);
	  }
	}

}