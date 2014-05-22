using System;

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


	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using Term = Lucene.Net.Index.Term;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Directory = Lucene.Net.Store.Directory;
	using Test = org.junit.Test;

	/// <summary>
	/// A basic 'positive' Unit test class for the FieldCacheRangeFilter class.
	/// 
	/// <p>
	/// NOTE: at the moment, this class only tests for 'positive' results,
	/// it does not verify the results to ensure there are no 'false positives',
	/// nor does it adequately test 'negative' results.  It also does not test
	/// that garbage in results in an Exception.
	/// </summary>
	public class TestFieldCacheRangeFilter : BaseTestRangeFilter
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
		Query q = new TermQuery(new Term("body","body"));

		// test id, bounded on both ends
		result = search.search(q, FieldCacheRangeFilter.newStringRange("id",minIP,maxIP,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",minIP,maxIP,T,F), numDocs).scoreDocs;
		Assert.AreEqual("all but last", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",minIP,maxIP,F,T), numDocs).scoreDocs;
		Assert.AreEqual("all but first", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",minIP,maxIP,F,F), numDocs).scoreDocs;
		Assert.AreEqual("all but ends", numDocs - 2, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",medIP,maxIP,T,T), numDocs).scoreDocs;
		Assert.AreEqual("med and up", 1 + MaxId - medId, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",minIP,medIP,T,T), numDocs).scoreDocs;
		Assert.AreEqual("up to med", 1 + medId - MinId, result.Length);

		// unbounded id

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",null,null,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",minIP,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("min and up", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",null,maxIP,F,T), numDocs).scoreDocs;
		Assert.AreEqual("max and down", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",minIP,null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not min, but up", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",null,maxIP,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not max, but down", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",medIP,maxIP,T,F), numDocs).scoreDocs;
		Assert.AreEqual("med and up, not max", MaxId - medId, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",minIP,medIP,F,T), numDocs).scoreDocs;
		Assert.AreEqual("not min, up to med", medId - MinId, result.Length);

		// very small sets

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",minIP,minIP,F,F), numDocs).scoreDocs;
		Assert.AreEqual("min,min,F,F", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",medIP,medIP,F,F), numDocs).scoreDocs;
		Assert.AreEqual("med,med,F,F", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",maxIP,maxIP,F,F), numDocs).scoreDocs;
		Assert.AreEqual("max,max,F,F", 0, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",minIP,minIP,T,T), numDocs).scoreDocs;
		Assert.AreEqual("min,min,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",null,minIP,F,T), numDocs).scoreDocs;
		Assert.AreEqual("nul,min,F,T", 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",maxIP,maxIP,T,T), numDocs).scoreDocs;
		Assert.AreEqual("max,max,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",maxIP,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("max,nul,T,T", 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("id",medIP,medIP,T,T), numDocs).scoreDocs;
		Assert.AreEqual("med,med,T,T", 1, result.Length);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFieldCacheRangeFilterRand() throws java.io.IOException
	  public virtual void TestFieldCacheRangeFilterRand()
	  {

		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		string minRP = Pad(SignedIndexDir.MinR);
		string maxRP = Pad(SignedIndexDir.MaxR);

		int numDocs = reader.numDocs();

		Assert.AreEqual("num of docs", numDocs, 1 + MaxId - MinId);

		ScoreDoc[] result;
		Query q = new TermQuery(new Term("body","body"));

		// test extremes, bounded on both ends

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",minRP,maxRP,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",minRP,maxRP,T,F), numDocs).scoreDocs;
		Assert.AreEqual("all but biggest", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",minRP,maxRP,F,T), numDocs).scoreDocs;
		Assert.AreEqual("all but smallest", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",minRP,maxRP,F,F), numDocs).scoreDocs;
		Assert.AreEqual("all but extremes", numDocs - 2, result.Length);

		// unbounded

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",minRP,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("smallest and up", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",null,maxRP,F,T), numDocs).scoreDocs;
		Assert.AreEqual("biggest and down", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",minRP,null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not smallest, but up", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",null,maxRP,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not biggest, but down", numDocs - 1, result.Length);

		// very small sets

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",minRP,minRP,F,F), numDocs).scoreDocs;
		Assert.AreEqual("min,min,F,F", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",maxRP,maxRP,F,F), numDocs).scoreDocs;
		Assert.AreEqual("max,max,F,F", 0, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",minRP,minRP,T,T), numDocs).scoreDocs;
		Assert.AreEqual("min,min,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",null,minRP,F,T), numDocs).scoreDocs;
		Assert.AreEqual("nul,min,F,T", 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",maxRP,maxRP,T,T), numDocs).scoreDocs;
		Assert.AreEqual("max,max,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newStringRange("rand",maxRP,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("max,nul,T,T", 1, result.Length);
	  }

	  // byte-ranges cannot be tested, because all ranges are too big for bytes, need an extra range for that

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFieldCacheRangeFilterShorts() throws java.io.IOException
	  public virtual void TestFieldCacheRangeFilterShorts()
	  {

		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		int numDocs = reader.numDocs();
		int medId = ((MaxId - MinId) / 2);
		short? minIdO = Convert.ToInt16((short) MinId);
		short? maxIdO = Convert.ToInt16((short) MaxId);
		short? medIdO = Convert.ToInt16((short) medId);

		Assert.AreEqual("num of docs", numDocs, 1 + MaxId - MinId);

		ScoreDoc[] result;
		Query q = new TermQuery(new Term("body","body"));

		// test id, bounded on both ends
		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,maxIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,maxIdO,T,F), numDocs).scoreDocs;
		Assert.AreEqual("all but last", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,maxIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("all but first", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,maxIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("all but ends", numDocs - 2, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",medIdO,maxIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("med and up", 1 + MaxId - medId, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,medIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("up to med", 1 + medId - MinId, result.Length);

		// unbounded id

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",null,null,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("min and up", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",null,maxIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("max and down", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not min, but up", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",null,maxIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not max, but down", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",medIdO,maxIdO,T,F), numDocs).scoreDocs;
		Assert.AreEqual("med and up, not max", MaxId - medId, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,medIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("not min, up to med", medId - MinId, result.Length);

		// very small sets

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,minIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("min,min,F,F", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",medIdO,medIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("med,med,F,F", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",maxIdO,maxIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("max,max,F,F", 0, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",minIdO,minIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("min,min,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",null,minIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("nul,min,F,T", 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",maxIdO,maxIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("max,max,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",maxIdO,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("max,nul,T,T", 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",medIdO,medIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("med,med,T,T", 1, result.Length);

		// special cases
		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",Convert.ToInt16(short.MaxValue),null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("overflow special case", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",null,Convert.ToInt16(short.MinValue),F,F), numDocs).scoreDocs;
		Assert.AreEqual("overflow special case", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newShortRange("id",maxIdO,minIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("inverse range", 0, result.Length);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFieldCacheRangeFilterInts() throws java.io.IOException
	  public virtual void TestFieldCacheRangeFilterInts()
	  {

		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		int numDocs = reader.numDocs();
		int medId = ((MaxId - MinId) / 2);
		int? minIdO = Convert.ToInt32(MinId);
		int? maxIdO = Convert.ToInt32(MaxId);
		int? medIdO = Convert.ToInt32(medId);

		Assert.AreEqual("num of docs", numDocs, 1 + MaxId - MinId);

		ScoreDoc[] result;
		Query q = new TermQuery(new Term("body","body"));

		// test id, bounded on both ends

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,maxIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,maxIdO,T,F), numDocs).scoreDocs;
		Assert.AreEqual("all but last", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,maxIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("all but first", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,maxIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("all but ends", numDocs - 2, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",medIdO,maxIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("med and up", 1 + MaxId - medId, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,medIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("up to med", 1 + medId - MinId, result.Length);

		// unbounded id

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",null,null,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("min and up", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",null,maxIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("max and down", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not min, but up", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",null,maxIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not max, but down", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",medIdO,maxIdO,T,F), numDocs).scoreDocs;
		Assert.AreEqual("med and up, not max", MaxId - medId, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,medIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("not min, up to med", medId - MinId, result.Length);

		// very small sets

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,minIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("min,min,F,F", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",medIdO,medIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("med,med,F,F", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",maxIdO,maxIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("max,max,F,F", 0, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",minIdO,minIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("min,min,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",null,minIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("nul,min,F,T", 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",maxIdO,maxIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("max,max,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",maxIdO,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("max,nul,T,T", 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",medIdO,medIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("med,med,T,T", 1, result.Length);

		// special cases
		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",Convert.ToInt32(int.MaxValue),null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("overflow special case", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",null,Convert.ToInt32(int.MinValue),F,F), numDocs).scoreDocs;
		Assert.AreEqual("overflow special case", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newIntRange("id",maxIdO,minIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("inverse range", 0, result.Length);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFieldCacheRangeFilterLongs() throws java.io.IOException
	  public virtual void TestFieldCacheRangeFilterLongs()
	  {

		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		int numDocs = reader.numDocs();
		int medId = ((MaxId - MinId) / 2);
		long? minIdO = Convert.ToInt64(MinId);
		long? maxIdO = Convert.ToInt64(MaxId);
		long? medIdO = Convert.ToInt64(medId);

		Assert.AreEqual("num of docs", numDocs, 1 + MaxId - MinId);

		ScoreDoc[] result;
		Query q = new TermQuery(new Term("body","body"));

		// test id, bounded on both ends

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,maxIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,maxIdO,T,F), numDocs).scoreDocs;
		Assert.AreEqual("all but last", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,maxIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("all but first", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,maxIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("all but ends", numDocs - 2, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",medIdO,maxIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("med and up", 1 + MaxId - medId, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,medIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("up to med", 1 + medId - MinId, result.Length);

		// unbounded id

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",null,null,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("min and up", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",null,maxIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("max and down", numDocs, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not min, but up", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",null,maxIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("not max, but down", numDocs - 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",medIdO,maxIdO,T,F), numDocs).scoreDocs;
		Assert.AreEqual("med and up, not max", MaxId - medId, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,medIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("not min, up to med", medId - MinId, result.Length);

		// very small sets

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,minIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("min,min,F,F", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",medIdO,medIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("med,med,F,F", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",maxIdO,maxIdO,F,F), numDocs).scoreDocs;
		Assert.AreEqual("max,max,F,F", 0, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",minIdO,minIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("min,min,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",null,minIdO,F,T), numDocs).scoreDocs;
		Assert.AreEqual("nul,min,F,T", 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",maxIdO,maxIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("max,max,T,T", 1, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",maxIdO,null,T,F), numDocs).scoreDocs;
		Assert.AreEqual("max,nul,T,T", 1, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",medIdO,medIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("med,med,T,T", 1, result.Length);

		// special cases
		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",Convert.ToInt64(long.MaxValue),null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("overflow special case", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",null,Convert.ToInt64(long.MinValue),F,F), numDocs).scoreDocs;
		Assert.AreEqual("overflow special case", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newLongRange("id",maxIdO,minIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("inverse range", 0, result.Length);
	  }

	  // float and double tests are a bit minimalistic, but its complicated, because missing precision

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFieldCacheRangeFilterFloats() throws java.io.IOException
	  public virtual void TestFieldCacheRangeFilterFloats()
	  {

		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		int numDocs = reader.numDocs();
		float? minIdO = Convert.ToSingle(MinId + .5f);
		float? medIdO = Convert.ToSingle((float)minIdO + ((MaxId - MinId)) / 2.0f);

		ScoreDoc[] result;
		Query q = new TermQuery(new Term("body","body"));

		result = search.search(q,FieldCacheRangeFilter.newFloatRange("id",minIdO,medIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs / 2, result.Length);
		int count = 0;
		result = search.search(q,FieldCacheRangeFilter.newFloatRange("id",null,medIdO,F,T), numDocs).scoreDocs;
		count += result.Length;
		result = search.search(q,FieldCacheRangeFilter.newFloatRange("id",medIdO,null,F,F), numDocs).scoreDocs;
		count += result.Length;
		Assert.AreEqual("sum of two concenatted ranges", numDocs, count);
		result = search.search(q,FieldCacheRangeFilter.newFloatRange("id",null,null,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newFloatRange("id",Convert.ToSingle(float.PositiveInfinity),null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("infinity special case", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newFloatRange("id",null,Convert.ToSingle(float.NegativeInfinity),F,F), numDocs).scoreDocs;
		Assert.AreEqual("infinity special case", 0, result.Length);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFieldCacheRangeFilterDoubles() throws java.io.IOException
	  public virtual void TestFieldCacheRangeFilterDoubles()
	  {

		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		int numDocs = reader.numDocs();
		double? minIdO = Convert.ToDouble(MinId + .5);
		double? medIdO = Convert.ToDouble((float)minIdO + ((MaxId - MinId)) / 2.0);

		ScoreDoc[] result;
		Query q = new TermQuery(new Term("body","body"));

		result = search.search(q,FieldCacheRangeFilter.newDoubleRange("id",minIdO,medIdO,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs / 2, result.Length);
		int count = 0;
		result = search.search(q,FieldCacheRangeFilter.newDoubleRange("id",null,medIdO,F,T), numDocs).scoreDocs;
		count += result.Length;
		result = search.search(q,FieldCacheRangeFilter.newDoubleRange("id",medIdO,null,F,F), numDocs).scoreDocs;
		count += result.Length;
		Assert.AreEqual("sum of two concenatted ranges", numDocs, count);
		result = search.search(q,FieldCacheRangeFilter.newDoubleRange("id",null,null,T,T), numDocs).scoreDocs;
		Assert.AreEqual("find all", numDocs, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newDoubleRange("id",Convert.ToDouble(double.PositiveInfinity),null,F,F), numDocs).scoreDocs;
		Assert.AreEqual("infinity special case", 0, result.Length);
		result = search.search(q,FieldCacheRangeFilter.newDoubleRange("id",null, Convert.ToDouble(double.NegativeInfinity),F,F), numDocs).scoreDocs;
		Assert.AreEqual("infinity special case", 0, result.Length);
	  }

	  // test using a sparse index (with deleted docs).
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSparseIndex() throws java.io.IOException
	  public virtual void TestSparseIndex()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		for (int d = -20; d <= 20; d++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", Convert.ToString(d), Field.Store.NO));
		  doc.add(newStringField("body", "body", Field.Store.NO));
		  writer.addDocument(doc);
		}

		writer.forceMerge(1);
		writer.deleteDocuments(new Term("id","0"));
		writer.close();

		IndexReader reader = DirectoryReader.open(dir);
		IndexSearcher search = newSearcher(reader);
		Assert.IsTrue(reader.hasDeletions());

		ScoreDoc[] result;
		Query q = new TermQuery(new Term("body","body"));

		result = search.search(q,FieldCacheRangeFilter.newByteRange("id",Convert.ToByte((sbyte) - 20),Convert.ToByte((sbyte) 20),T,T), 100).scoreDocs;
		Assert.AreEqual("find all", 40, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newByteRange("id",Convert.ToByte((sbyte) 0),Convert.ToByte((sbyte) 20),T,T), 100).scoreDocs;
		Assert.AreEqual("find all", 20, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newByteRange("id",Convert.ToByte((sbyte) - 20),Convert.ToByte((sbyte) 0),T,T), 100).scoreDocs;
		Assert.AreEqual("find all", 20, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newByteRange("id",Convert.ToByte((sbyte) 10),Convert.ToByte((sbyte) 20),T,T), 100).scoreDocs;
		Assert.AreEqual("find all", 11, result.Length);

		result = search.search(q,FieldCacheRangeFilter.newByteRange("id",Convert.ToByte((sbyte) - 20),Convert.ToByte((sbyte) - 10),T,T), 100).scoreDocs;
		Assert.AreEqual("find all", 11, result.Length);
		reader.close();
		dir.close();
	  }

	}

}