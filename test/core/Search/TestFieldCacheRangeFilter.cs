/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for Additional information regarding copyright ownership.
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

using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
	
	/// <summary> A basic 'positive' Unit test class for the FieldCacheRangeFilter class.
	/// 
	/// <p/>
	/// NOTE: at the moment, this class only tests for 'positive' results,
	/// it does not verify the results to ensure there are no 'false positives',
	/// nor does it adequately test 'negative' results.  It also does not test
	/// that garbage in results in an Exception.
	/// </summary>
    [TestFixture]
	public class TestFieldCacheRangeFilter:BaseTestRangeFilter
	{
		
		public TestFieldCacheRangeFilter(System.String name):base(name)
		{
		}
		public TestFieldCacheRangeFilter():base()
		{
		}
		
        [Test]
		public virtual void  TestRangeFilterId()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher Search = new IndexSearcher(reader);
			
			int medId = ((maxId - minId) / 2);
			
			System.String minIP = Pad(minId);
			System.String maxIP = Pad(maxId);
			System.String medIP = Pad(medId);
			
			int numDocs = reader.NumDocs();
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			ScoreDoc[] result;
			Query q = new TermQuery(new Term("body", "body"));
			
			// test id, bounded on both ends
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, maxIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, maxIP, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but last");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, maxIP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but first");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, maxIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 2, result.Length, "all but ends");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", medIP, maxIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, medIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + medId - minId, result.Length, "up to med");
			
			// unbounded id
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", null, null, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "min and up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", null, maxIP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "max and down");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", null, maxIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", medIP, maxIP, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, medIP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(medId - minId, result.Length, "not min, up to med");
			
			// very small sets
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, minIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "min,min,F,F");
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", medIP, medIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "med,med,F,F");
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", maxIP, maxIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "max,max,F,F");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, minIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "min,min,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", null, minIP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "nul,min,F,T");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", maxIP, maxIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,max,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", maxIP, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,nul,T,T");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("id", medIP, medIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "med,med,T,T");
		}
		
        [Test]
		public virtual void  TestFieldCacheRangeFilterRand()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher Search = new IndexSearcher(reader);
			
			System.String minRP = Pad(signedIndex.minR);
			System.String maxRP = Pad(signedIndex.maxR);
			
			int numDocs = reader.NumDocs();
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			ScoreDoc[] result;
			Query q = new TermQuery(new Term("body", "body"));
			
			// test extremes, bounded on both ends
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, maxRP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, maxRP, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but biggest");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, maxRP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but smallest");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, maxRP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 2, result.Length, "all but extremes");
			
			// unbounded
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "smallest and up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", null, maxRP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "biggest and down");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not smallest, but up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", null, maxRP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not biggest, but down");
			
			// very small sets
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, minRP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "min,min,F,F");
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", maxRP, maxRP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "max,max,F,F");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, minRP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "min,min,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", null, minRP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "nul,min,F,T");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", maxRP, maxRP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,max,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", maxRP, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,nul,T,T");
		}
		
		// byte-ranges cannot be tested, because all ranges are too big for bytes, need an extra range for that
		
        [Test]
		public virtual void  TestFieldCacheRangeFilterShorts()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher Search = new IndexSearcher(reader);
			
			int numDocs = reader.NumDocs();
			int medId = ((maxId - minId) / 2);
			System.Int16 minIdO = (short) minId;
			System.Int16 maxIdO = (short) maxId;
			System.Int16 medIdO = (short) medId;
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			ScoreDoc[] result;
			Query q = new TermQuery(new Term("body", "body"));
			
			// test id, bounded on both ends
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, maxIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, maxIdO, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but last");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, maxIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but first");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, maxIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 2, result.Length, "all but ends");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", medIdO, maxIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + medId - minId, result.Length, "up to med");
			
			// unbounded id
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", null, null, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "min and up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", null, maxIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "max and down");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", null, maxIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", medIdO, maxIdO, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, medIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(medId - minId, result.Length, "not min, up to med");
			
			// very small sets
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, minIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "min,min,F,F");
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", medIdO, medIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "med,med,F,F");
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", maxIdO, maxIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "max,max,F,F");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", minIdO, minIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "min,min,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", null, minIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "nul,min,F,T");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", maxIdO, maxIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,max,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", maxIdO, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,nul,T,T");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", medIdO, medIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "med,med,T,T");
			
			// special cases
			System.Int16 tempAux = (short) System.Int16.MaxValue;
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", tempAux, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "overflow special case");
			System.Int16 tempAux2 = (short) System.Int16.MinValue;
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", null, tempAux2, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "overflow special case");
			result = Search.Search(q, FieldCacheRangeFilter.NewShortRange("id", maxIdO, minIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "inverse range");
		}
		
        [Test]
		public virtual void  TestFieldCacheRangeFilterInts()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher Search = new IndexSearcher(reader);
			
			int numDocs = reader.NumDocs();
			int medId = ((maxId - minId) / 2);
			System.Int32 minIdO = (System.Int32) minId;
			System.Int32 maxIdO = (System.Int32) maxId;
			System.Int32 medIdO = (System.Int32) medId;
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			ScoreDoc[] result;
			Query q = new TermQuery(new Term("body", "body"));
			
			// test id, bounded on both ends
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, maxIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, maxIdO, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but last");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, maxIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but first");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, maxIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 2, result.Length, "all but ends");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", medIdO, maxIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + medId - minId, result.Length, "up to med");
			
			// unbounded id
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", null, null, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "min and up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", null, maxIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "max and down");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", null, maxIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", medIdO, maxIdO, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, medIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(medId - minId, result.Length, "not min, up to med");
			
			// very small sets
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, minIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "min,min,F,F");
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", medIdO, medIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "med,med,F,F");
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", maxIdO, maxIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "max,max,F,F");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", minIdO, minIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "min,min,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", null, minIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "nul,min,F,T");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", maxIdO, maxIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,max,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", maxIdO, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,nul,T,T");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", medIdO, medIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "med,med,T,T");
			
			// special cases
			System.Int32 tempAux = (System.Int32) System.Int32.MaxValue;
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", tempAux, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "overflow special case");
			System.Int32 tempAux2 = (System.Int32) System.Int32.MinValue;
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", null, tempAux2, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "overflow special case");
			result = Search.Search(q, FieldCacheRangeFilter.NewIntRange("id", maxIdO, minIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "inverse range");
		}
		
        [Test]
		public virtual void  TestFieldCacheRangeFilterLongs()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher Search = new IndexSearcher(reader);
			
			int numDocs = reader.NumDocs();
			int medId = ((maxId - minId) / 2);
			System.Int64 minIdO = (long) minId;
			System.Int64 maxIdO = (long) maxId;
			System.Int64 medIdO = (long) medId;
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			ScoreDoc[] result;
			Query q = new TermQuery(new Term("body", "body"));
			
			// test id, bounded on both ends
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, maxIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, maxIdO, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but last");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, maxIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but first");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, maxIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 2, result.Length, "all but ends");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", medIdO, maxIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + medId - minId, result.Length, "up to med");
			
			// unbounded id
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", null, null, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "min and up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", null, maxIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "max and down");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", null, maxIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", medIdO, maxIdO, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, medIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(medId - minId, result.Length, "not min, up to med");
			
			// very small sets
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, minIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "min,min,F,F");
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", medIdO, medIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "med,med,F,F");
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", maxIdO, maxIdO, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "max,max,F,F");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", minIdO, minIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "min,min,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", null, minIdO, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "nul,min,F,T");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", maxIdO, maxIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,max,T,T");
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", maxIdO, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,nul,T,T");
			
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", medIdO, medIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "med,med,T,T");
			
			// special cases
			System.Int64 tempAux = (long) System.Int64.MaxValue;
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", tempAux, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "overflow special case");
			System.Int64 tempAux2 = (long) System.Int64.MinValue;
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", null, tempAux2, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "overflow special case");
			result = Search.Search(q, FieldCacheRangeFilter.NewLongRange("id", maxIdO, minIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "inverse range");
		}
		
		// float and double tests are a bit minimalistic, but its complicated, because missing precision
		
        [Test]
		public virtual void  TestFieldCacheRangeFilterFloats()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher Search = new IndexSearcher(reader);
			
			int numDocs = reader.NumDocs();
			System.Single minIdO = (float) (minId + .5f);
			System.Single medIdO = (float) ((float) minIdO + ((float) (maxId - minId)) / 2.0f);
			
			ScoreDoc[] result;
			Query q = new TermQuery(new Term("body", "body"));
			
			result = Search.Search(q, FieldCacheRangeFilter.NewFloatRange("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs / 2, result.Length, "find all");
			int count = 0;
			result = Search.Search(q, FieldCacheRangeFilter.NewFloatRange("id", null, medIdO, F, T), numDocs).ScoreDocs;
			count += result.Length;
			result = Search.Search(q, FieldCacheRangeFilter.NewFloatRange("id", medIdO, null, F, F), numDocs).ScoreDocs;
			count += result.Length;
			Assert.AreEqual(numDocs, count, "sum of two concenatted ranges");
			result = Search.Search(q, FieldCacheRangeFilter.NewFloatRange("id", null, null, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			System.Single tempAux = (float) System.Single.PositiveInfinity;
			result = Search.Search(q, FieldCacheRangeFilter.NewFloatRange("id", tempAux, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "infinity special case");
			System.Single tempAux2 = (float) System.Single.NegativeInfinity;
			result = Search.Search(q, FieldCacheRangeFilter.NewFloatRange("id", null, tempAux2, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "infinity special case");
		}
		
        [Test]
		public virtual void  TestFieldCacheRangeFilterDoubles()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher Search = new IndexSearcher(reader);
			
			int numDocs = reader.NumDocs();
			System.Double minIdO = (double) (minId + .5);
			System.Double medIdO = (double) ((float) minIdO + ((double) (maxId - minId)) / 2.0);
			
			ScoreDoc[] result;
			Query q = new TermQuery(new Term("body", "body"));
			
			result = Search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs / 2, result.Length, "find all");
			int count = 0;
			result = Search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", null, medIdO, F, T), numDocs).ScoreDocs;
			count += result.Length;
			result = Search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", medIdO, null, F, F), numDocs).ScoreDocs;
			count += result.Length;
			Assert.AreEqual(numDocs, count, "sum of two concenatted ranges");
			result = Search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", null, null, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			System.Double tempAux = (double) System.Double.PositiveInfinity;
			result = Search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", tempAux, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "infinity special case");
			System.Double tempAux2 = (double) System.Double.NegativeInfinity;
			result = Search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", null, tempAux2, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "infinity special case");
		}

        [Test]
        // test using a sparse index (with deleted docs). The DocIdSet should be not cacheable, as it uses TermDocs if the range contains 0
  public void TestSparseIndex()
        {
    RAMDirectory dir = new RAMDirectory();
    IndexWriter writer = new IndexWriter(dir, new SimpleAnalyzer(), T, IndexWriter.MaxFieldLength.LIMITED);

    for (int d = -20; d <= 20; d++) {
      Document doc = new Document();
      doc.Add(new Field("id",d.ToString(), Field.Store.NO, Field.Index.NOT_ANALYZED));
      doc.Add(new Field("body","body", Field.Store.NO, Field.Index.NOT_ANALYZED));
      writer.AddDocument(doc);
    }
    
    writer.Optimize();
    writer.DeleteDocuments(new Term("id","0"));
    writer.Close();

    IndexReader reader = IndexReader.Open(dir, true);
    IndexSearcher Search = new IndexSearcher(reader);
    Assert.True(reader.HasDeletions);

    ScoreDoc[] result;
    Query q = new TermQuery(new Term("body","body"));

    FieldCacheRangeFilter<sbyte?> fcrf;
    result = Search.Search(q, fcrf = FieldCacheRangeFilter.NewByteRange("id", -20, 20, T, T), 100).ScoreDocs;
    Assert.False(fcrf.GetDocIdSet(reader.GetSequentialSubReaders()[0]).IsCacheable, "DocIdSet must be not cacheable");
    Assert.AreEqual(40, result.Length, "find all");

    result = Search.Search(q, fcrf = FieldCacheRangeFilter.NewByteRange("id", 0, 20, T, T), 100).ScoreDocs;
    Assert.False(fcrf.GetDocIdSet(reader.GetSequentialSubReaders()[0]).IsCacheable, "DocIdSet must be not cacheable");
    Assert.AreEqual( 20, result.Length, "find all");

            result = Search.Search(q, fcrf = FieldCacheRangeFilter.NewByteRange("id", -20, 0, T, T), 100).ScoreDocs;
    Assert.False(fcrf.GetDocIdSet(reader.GetSequentialSubReaders()[0]).IsCacheable, "DocIdSet must be not cacheable");
    Assert.AreEqual( 20, result.Length, "find all");

    result = Search.Search(q, fcrf = FieldCacheRangeFilter.NewByteRange("id", 10, 20, T, T), 100).ScoreDocs;
    Assert.True(fcrf.GetDocIdSet(reader.GetSequentialSubReaders()[0]).IsCacheable, "DocIdSet must be not cacheable");
    Assert.AreEqual( 11, result.Length, "find all");

    result = Search.Search(q, fcrf = FieldCacheRangeFilter.NewByteRange("id", -20, -10, T, T), 100).ScoreDocs;
    Assert.True(fcrf.GetDocIdSet(reader.GetSequentialSubReaders()[0]).IsCacheable, "DocIdSet must be not cacheable");
    Assert.AreEqual( 11, result.Length, "find all");
  }
	}
}