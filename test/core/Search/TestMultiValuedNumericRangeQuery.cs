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
	using IntField = Lucene.Net.Document.IntField;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestMultiValuedNumericRangeQuery : LuceneTestCase
	{

	  /// <summary>
	  /// Tests NumericRangeQuery on a multi-valued field (multiple numeric values per document).
	  /// this test ensures, that a classical TermRangeQuery returns exactly the same document numbers as
	  /// NumericRangeQuery (see SOLR-1322 for discussion) and the multiple precision terms per numeric value
	  /// do not interfere with multiple numeric values.
	  /// </summary>
	  public virtual void TestMultiValuedNRQ()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(TestUtil.Next(random(), 50, 1000)));

		DecimalFormat format = new DecimalFormat("00000000000", new DecimalFormatSymbols(Locale.ROOT));

		int num = atLeast(500);
		for (int l = 0; l < num; l++)
		{
		  Document doc = new Document();
		  for (int m = 0, c = random().Next(10); m <= c; m++)
		  {
			int value = random().Next(int.MaxValue);
			doc.add(newStringField("asc", format.format(value), Field.Store.NO));
			doc.add(new IntField("trie", value, Field.Store.NO));
		  }
		  writer.addDocument(doc);
		}
		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);
		num = atLeast(50);
		for (int i = 0; i < num; i++)
		{
		  int lower = random().Next(int.MaxValue);
		  int upper = random().Next(int.MaxValue);
		  if (lower > upper)
		  {
			int a = lower;
			lower = upper;
			upper = a;
		  }
		  TermRangeQuery cq = TermRangeQuery.newStringRange("asc", format.format(lower), format.format(upper), true, true);
		  NumericRangeQuery<int?> tq = NumericRangeQuery.newIntRange("trie", lower, upper, true, true);
		  TopDocs trTopDocs = searcher.search(cq, 1);
		  TopDocs nrTopDocs = searcher.search(tq, 1);
		  Assert.AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal", trTopDocs.totalHits, nrTopDocs.totalHits);
		}
		reader.close();
		directory.close();
	  }

	}

}