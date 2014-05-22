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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Create an index with terms from 000-999.
	/// Generates random regexps according to simple patterns,
	/// and validates the correct number of hits are returned.
	/// </summary>
	public class TestRegexpRandom : LuceneTestCase
	{
	  private IndexSearcher Searcher;
	  private IndexReader Reader;
	  private Directory Dir;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(TestUtil.Next(random(), 50, 1000)));

		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.OmitNorms = true;
		Field field = newField("field", "", customType);
		doc.add(field);

		NumberFormat df = new DecimalFormat("000", new DecimalFormatSymbols(Locale.ROOT));
		for (int i = 0; i < 1000; i++)
		{
		  field.StringValue = df.format(i);
		  writer.addDocument(doc);
		}

		Reader = writer.Reader;
		writer.close();
		Searcher = newSearcher(Reader);
	  }

	  private char N()
	  {
		return (char)(0x30 + random().Next(10));
	  }

	  private string FillPattern(string wildcardPattern)
	  {
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < wildcardPattern.Length; i++)
		{
		  switch (wildcardPattern[i])
		  {
			case 'N':
			  sb.Append(N());
			  break;
			default:
			  sb.Append(wildcardPattern[i]);
		  break;
		  }
		}
		return sb.ToString();
	  }

	  private void AssertPatternHits(string pattern, int numHits)
	  {
		Query wq = new RegexpQuery(new Term("field", FillPattern(pattern)));
		TopDocs docs = Searcher.search(wq, 25);
		Assert.AreEqual("Incorrect hits for pattern: " + pattern, numHits, docs.totalHits);
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  public virtual void TestRegexps()
	  {
		int num = atLeast(1);
		for (int i = 0; i < num; i++)
		{
		  AssertPatternHits("NNN", 1);
		  AssertPatternHits(".NN", 10);
		  AssertPatternHits("N.N", 10);
		  AssertPatternHits("NN.", 10);
		}

		for (int i = 0; i < num; i++)
		{
		  AssertPatternHits(".{1,2}N", 100);
		  AssertPatternHits("N.{1,2}", 100);
		  AssertPatternHits(".{1,3}", 1000);

		  AssertPatternHits("NN[3-7]", 5);
		  AssertPatternHits("N[2-6][3-7]", 25);
		  AssertPatternHits("[1-5][2-6][3-7]", 125);
		  AssertPatternHits("[0-4][3-7][4-8]", 125);
		  AssertPatternHits("[2-6][0-4]N", 25);
		  AssertPatternHits("[2-6]NN", 5);

		  AssertPatternHits("NN.*", 10);
		  AssertPatternHits("N.*", 100);
		  AssertPatternHits(".*", 1000);

		  AssertPatternHits(".*NN", 10);
		  AssertPatternHits(".*N", 100);

		  AssertPatternHits("N.*N", 10);

		  // combo of ? and * operators
		  AssertPatternHits(".N.*", 100);
		  AssertPatternHits("N..*", 100);

		  AssertPatternHits(".*N.", 100);
		  AssertPatternHits(".*..", 1000);
		  AssertPatternHits(".*.N", 100);
		}
	  }
	}

}