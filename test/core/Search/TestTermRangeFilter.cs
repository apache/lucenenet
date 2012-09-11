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

using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
	
	/// <summary> A basic 'positive' Unit test class for the TermRangeFilter class.
	/// 
	/// <p/>
	/// NOTE: at the moment, this class only tests for 'positive' results,
	/// it does not verify the results to ensure there are no 'false positives',
	/// nor does it adequately test 'negative' results.  It also does not test
	/// that garbage in results in an Exception.
	/// </summary>
    [TestFixture]
	public class TestTermRangeFilter:BaseTestRangeFilter
	{
		
		public TestTermRangeFilter(System.String name):base(name)
		{
		}
		public TestTermRangeFilter():base()
		{
		}
		
        [Test]
		public virtual void  TestRangeFilterId()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher search = new IndexSearcher(reader);
			
			int medId = ((maxId - minId) / 2);
			
			System.String minIP = Pad(minId);
			System.String maxIP = Pad(maxId);
			System.String medIP = Pad(medId);
			
			int numDocs = reader.NumDocs();
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			ScoreDoc[] result;
			Query q = new TermQuery(new Term("body", "body"));
			
			// test id, bounded on both ends
			
			result = search.Search(q, new TermRangeFilter("id", minIP, maxIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = search.Search(q, new TermRangeFilter("id", minIP, maxIP, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but last");
			
			result = search.Search(q, new TermRangeFilter("id", minIP, maxIP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but first");
			
			result = search.Search(q, new TermRangeFilter("id", minIP, maxIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 2, result.Length, "all but ends");
			
			result = search.Search(q, new TermRangeFilter("id", medIP, maxIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");
			
			result = search.Search(q, new TermRangeFilter("id", minIP, medIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1 + medId - minId, result.Length, "up to med");
			
			// unbounded id
			
			result = search.Search(q, new TermRangeFilter("id", minIP, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "min and up");
			
			result = search.Search(q, new TermRangeFilter("id", null, maxIP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "max and down");
			
			result = search.Search(q, new TermRangeFilter("id", minIP, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");
			
			result = search.Search(q, new TermRangeFilter("id", null, maxIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");
			
			result = search.Search(q, new TermRangeFilter("id", medIP, maxIP, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");
			
			result = search.Search(q, new TermRangeFilter("id", minIP, medIP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(medId - minId, result.Length, "not min, up to med");
			
			// very small sets
			
			result = search.Search(q, new TermRangeFilter("id", minIP, minIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "min,min,F,F");
			result = search.Search(q, new TermRangeFilter("id", medIP, medIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "med,med,F,F");
			result = search.Search(q, new TermRangeFilter("id", maxIP, maxIP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "max,max,F,F");
			
			result = search.Search(q, new TermRangeFilter("id", minIP, minIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "min,min,T,T");
			result = search.Search(q, new TermRangeFilter("id", null, minIP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "nul,min,F,T");
			
			result = search.Search(q, new TermRangeFilter("id", maxIP, maxIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,max,T,T");
			result = search.Search(q, new TermRangeFilter("id", maxIP, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,nul,T,T");
			
			result = search.Search(q, new TermRangeFilter("id", medIP, medIP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "med,med,T,T");
		}
		
        [Test]
		public virtual void  TestRangeFilterIdCollating()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher search = new IndexSearcher(reader);
			
			System.Globalization.CompareInfo c = new System.Globalization.CultureInfo("en").CompareInfo;
			
			int medId = ((maxId - minId) / 2);
			
			System.String minIP = Pad(minId);
			System.String maxIP = Pad(maxId);
			System.String medIP = Pad(medId);
			
			int numDocs = reader.NumDocs();
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			Query q = new TermQuery(new Term("body", "body"));
			
			// test id, bounded on both ends
            int numHits = search.Search(q, new TermRangeFilter("id", minIP, maxIP, T, T, c), 1000).TotalHits;
            Assert.AreEqual(numDocs, numHits, "find all");

            numHits = search.Search(q, new TermRangeFilter("id", minIP, maxIP, T, T, c), 1000).TotalHits;
			Assert.AreEqual(numDocs, numHits, "find all");

            numHits = search.Search(q, new TermRangeFilter("id", minIP, maxIP, T, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 1, numHits, "all but last");

            numHits = search.Search(q, new TermRangeFilter("id", minIP, maxIP, F, T, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 1, numHits, "all but first");

            numHits = search.Search(q, new TermRangeFilter("id", minIP, maxIP, F, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 2, numHits, "all but ends");

            numHits = search.Search(q, new TermRangeFilter("id", medIP, maxIP, T, T, c), 1000).TotalHits;
			Assert.AreEqual(1 + maxId - medId, numHits, "med and up");

            numHits = search.Search(q, new TermRangeFilter("id", minIP, medIP, T, T, c), 1000).TotalHits;
			Assert.AreEqual(1 + medId - minId, numHits, "up to med");
			
			// unbounded id
            numHits = search.Search(q, new TermRangeFilter("id", minIP, null, T, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs, numHits, "min and up");

            numHits = search.Search(q, new TermRangeFilter("id", null, maxIP, F, T, c), 1000).TotalHits;
			Assert.AreEqual(numDocs, numHits, "max and down");

            numHits = search.Search(q, new TermRangeFilter("id", minIP, null, F, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 1, numHits, "not min, but up");

            numHits = search.Search(q, new TermRangeFilter("id", null, maxIP, F, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 1, numHits, "not max, but down");

            numHits = search.Search(q, new TermRangeFilter("id", medIP, maxIP, T, F, c), 1000).TotalHits;
			Assert.AreEqual(maxId - medId, numHits, "med and up, not max");

            numHits = search.Search(q, new TermRangeFilter("id", minIP, medIP, F, T, c), 1000).TotalHits;
			Assert.AreEqual(medId - minId, numHits, "not min, up to med");
			
			// very small sets

            numHits = search.Search(q, new TermRangeFilter("id", minIP, minIP, F, F, c), 1000).TotalHits;
			Assert.AreEqual(0, numHits, "min,min,F,F");
            numHits = search.Search(q, new TermRangeFilter("id", medIP, medIP, F, F, c), 1000).TotalHits;
			Assert.AreEqual(0, numHits, "med,med,F,F");
            numHits = search.Search(q, new TermRangeFilter("id", maxIP, maxIP, F, F, c), 1000).TotalHits;
			Assert.AreEqual(0, numHits, "max,max,F,F");

            numHits = search.Search(q, new TermRangeFilter("id", minIP, minIP, T, T, c), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "min,min,T,T");
            numHits = search.Search(q, new TermRangeFilter("id", null, minIP, F, T, c), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "nul,min,F,T");

            numHits = search.Search(q, new TermRangeFilter("id", maxIP, maxIP, T, T, c), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "max,max,T,T");
            numHits = search.Search(q, new TermRangeFilter("id", maxIP, null, T, F, c), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "max,nul,T,T");

            numHits = search.Search(q, new TermRangeFilter("id", medIP, medIP, T, T, c), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "med,med,T,T");
		}
		
        [Test]
		public virtual void  TestRangeFilterRand()
		{
			
			IndexReader reader = IndexReader.Open(signedIndex.index, true);
			IndexSearcher search = new IndexSearcher(reader);
			
			System.String minRP = Pad(signedIndex.minR);
			System.String maxRP = Pad(signedIndex.maxR);
			
			int numDocs = reader.NumDocs();
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			ScoreDoc[] result;
			Query q = new TermQuery(new Term("body", "body"));
			
			// test extremes, bounded on both ends
			
			result = search.Search(q, new TermRangeFilter("rand", minRP, maxRP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "find all");
			
			result = search.Search(q, new TermRangeFilter("rand", minRP, maxRP, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but biggest");
			
			result = search.Search(q, new TermRangeFilter("rand", minRP, maxRP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "all but smallest");
			
			result = search.Search(q, new TermRangeFilter("rand", minRP, maxRP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 2, result.Length, "all but extremes");
			
			// unbounded
			
			result = search.Search(q, new TermRangeFilter("rand", minRP, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "smallest and up");
			
			result = search.Search(q, new TermRangeFilter("rand", null, maxRP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs, result.Length, "biggest and down");
			
			result = search.Search(q, new TermRangeFilter("rand", minRP, null, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not smallest, but up");
			
			result = search.Search(q, new TermRangeFilter("rand", null, maxRP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(numDocs - 1, result.Length, "not biggest, but down");
			
			// very small sets
			
			result = search.Search(q, new TermRangeFilter("rand", minRP, minRP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "min,min,F,F");
			result = search.Search(q, new TermRangeFilter("rand", maxRP, maxRP, F, F), numDocs).ScoreDocs;
			Assert.AreEqual(0, result.Length, "max,max,F,F");
			
			result = search.Search(q, new TermRangeFilter("rand", minRP, minRP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "min,min,T,T");
			result = search.Search(q, new TermRangeFilter("rand", null, minRP, F, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "nul,min,F,T");
			
			result = search.Search(q, new TermRangeFilter("rand", maxRP, maxRP, T, T), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,max,T,T");
			result = search.Search(q, new TermRangeFilter("rand", maxRP, null, T, F), numDocs).ScoreDocs;
			Assert.AreEqual(1, result.Length, "max,nul,T,T");
		}
		
        [Test]
		public virtual void  TestRangeFilterRandCollating()
		{
			
			// using the unsigned index because collation seems to ignore hyphens
            IndexReader reader = IndexReader.Open(unsignedIndex.index, true);
			IndexSearcher search = new IndexSearcher(reader);
			
			System.Globalization.CompareInfo c = new System.Globalization.CultureInfo("en").CompareInfo;
			
			System.String minRP = Pad(unsignedIndex.minR);
			System.String maxRP = Pad(unsignedIndex.maxR);
			
			int numDocs = reader.NumDocs();
			
			Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");
			
			Query q = new TermQuery(new Term("body", "body"));
			
			// test extremes, bounded on both ends

            int numHits = search.Search(q, new TermRangeFilter("rand", minRP, maxRP, T, T, c), 1000).TotalHits;
			Assert.AreEqual(numDocs, numHits, "find all");

            numHits = search.Search(q, new TermRangeFilter("rand", minRP, maxRP, T, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 1, numHits, "all but biggest");

            numHits = search.Search(q, new TermRangeFilter("rand", minRP, maxRP, F, T, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 1, numHits, "all but smallest");

            numHits = search.Search(q, new TermRangeFilter("rand", minRP, maxRP, F, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 2, numHits, "all but extremes");
			
			// unbounded

            numHits = search.Search(q, new TermRangeFilter("rand", minRP, null, T, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs, numHits, "smallest and up");

            numHits = search.Search(q, new TermRangeFilter("rand", null, maxRP, F, T, c), 1000).TotalHits;
			Assert.AreEqual(numDocs, numHits, "biggest and down");

            numHits = search.Search(q, new TermRangeFilter("rand", minRP, null, F, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 1, numHits, "not smallest, but up");

            numHits = search.Search(q, new TermRangeFilter("rand", null, maxRP, F, F, c), 1000).TotalHits;
			Assert.AreEqual(numDocs - 1, numHits, "not biggest, but down");
			
			// very small sets

            numHits = search.Search(q, new TermRangeFilter("rand", minRP, minRP, F, F, c), 1000).TotalHits;
			Assert.AreEqual(0, numHits, "min,min,F,F");
            numHits = search.Search(q, new TermRangeFilter("rand", maxRP, maxRP, F, F, c), 1000).TotalHits;
			Assert.AreEqual(0, numHits, "max,max,F,F");

            numHits = search.Search(q, new TermRangeFilter("rand", minRP, minRP, T, T, c), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "min,min,T,T");
            numHits = search.Search(q, new TermRangeFilter("rand", null, minRP, F, T, c), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "nul,min,F,T");

            numHits = search.Search(q, new TermRangeFilter("rand", maxRP, maxRP, T, T, c), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "max,max,T,T");
            numHits = search.Search(q, new TermRangeFilter("rand", maxRP, null, T, F, c), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "max,nul,T,T");
		}
		
        [Test]
		public virtual void  TestFarsi()
		{
			
			/* build an index */
			RAMDirectory farsiIndex = new RAMDirectory();
			IndexWriter writer = new IndexWriter(farsiIndex, new SimpleAnalyzer(), T, IndexWriter.MaxFieldLength.LIMITED);
			Document doc = new Document();
			doc.Add(new Field("content", "\u0633\u0627\u0628", Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("body", "body", Field.Store.YES, Field.Index.NOT_ANALYZED));
			writer.AddDocument(doc);
			
			writer.Optimize();
			writer.Close();

            IndexReader reader = IndexReader.Open(farsiIndex, true);
			IndexSearcher search = new IndexSearcher(reader);
			Query q = new TermQuery(new Term("body", "body"));
			
			// Neither Java 1.4.2 nor 1.5.0 has Farsi Locale collation available in
			// RuleBasedCollator.  However, the Arabic Locale seems to order the Farsi
			// characters properly.
			System.Globalization.CompareInfo collator = new System.Globalization.CultureInfo("ar").CompareInfo;
			
			// Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
			// orders the U+0698 character before the U+0633 character, so the single
			// index Term below should NOT be returned by a TermRangeFilter with a Farsi
			// Collator (or an Arabic one for the case when Farsi is not supported).
            int numHits = search.Search(q, new TermRangeFilter("content", "\u062F", "\u0698", T, T, collator), 1000).TotalHits;
            Assert.AreEqual(0, numHits, "The index Term should not be included.");

            numHits = search.Search(q, new TermRangeFilter("content", "\u0633", "\u0638", T, T, collator), 1000).TotalHits;
            Assert.AreEqual(1, numHits, "The index Term should be included.");
			search.Close();
		}
		
        [Test]
		public virtual void  TestDanish()
		{
			
			/* build an index */
			RAMDirectory danishIndex = new RAMDirectory();
			IndexWriter writer = new IndexWriter(danishIndex, new SimpleAnalyzer(), T, IndexWriter.MaxFieldLength.LIMITED);
			// Danish collation orders the words below in the given order
			// (example taken from TestSort.testInternationalSort() ).
			System.String[] words = new System.String[]{"H\u00D8T", "H\u00C5T", "MAND"};
			for (int docnum = 0; docnum < words.Length; ++docnum)
			{
				Document doc = new Document();
				doc.Add(new Field("content", words[docnum], Field.Store.YES, Field.Index.NOT_ANALYZED));
				doc.Add(new Field("body", "body", Field.Store.YES, Field.Index.NOT_ANALYZED));
				writer.AddDocument(doc);
			}
			writer.Optimize();
			writer.Close();

            IndexReader reader = IndexReader.Open(danishIndex, true);
			IndexSearcher search = new IndexSearcher(reader);
			Query q = new TermQuery(new Term("body", "body"));
			
			System.Globalization.CompareInfo collator = new System.Globalization.CultureInfo("da" + "-" + "dk").CompareInfo;
			Query query = new TermRangeQuery("content", "H\u00D8T", "MAND", false, false, collator);
			
			// Unicode order would not include "H\u00C5T" in [ "H\u00D8T", "MAND" ],
			// but Danish collation does.
            int numHits =
                search.Search(q, new TermRangeFilter("content", "H\u00D8T", "MAND", F, F, collator), 1000).TotalHits;
			Assert.AreEqual(1, numHits, "The index Term should be included.");
			
			numHits = search.Search(q, new TermRangeFilter("content", "H\u00C5T", "MAND", F, F, collator), 1000).TotalHits;
			Assert.AreEqual(0, numHits, "The index Term should not be included.");
			search.Close();
		}
	}
}