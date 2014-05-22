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
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Codec = Lucene.Net.Codecs.Codec;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Directory = Lucene.Net.Store.Directory;
	using AttributeSource = Lucene.Net.Util.AttributeSource;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using StringHelper = Lucene.Net.Util.StringHelper;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Create an index with random unicode terms
	/// Generates random prefix queries, and validates against a simple impl.
	/// </summary>
	public class TestPrefixRandom : LuceneTestCase
	{
	  private IndexSearcher Searcher;
	  private IndexReader Reader;
	  private Directory Dir;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.KEYWORD, false)).setMaxBufferedDocs(TestUtil.Next(random(), 50, 1000)));

		Document doc = new Document();
		Field field = newStringField("field", "", Field.Store.NO);
		doc.add(field);

		// we generate aweful prefixes: good for testing.
		// but for preflex codec, the test can be very slow, so use less iterations.
		string codec = Codec.Default.Name;
		int num = codec.Equals("Lucene3x") ? 200 * RANDOM_MULTIPLIER : atLeast(1000);
		for (int i = 0; i < num; i++)
		{
		  field.StringValue = TestUtil.randomUnicodeString(random(), 10);
		  writer.addDocument(doc);
		}
		Reader = writer.Reader;
		Searcher = newSearcher(Reader);
		writer.close();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  /// <summary>
	  /// a stupid prefix query that just blasts thru the terms </summary>
	  private class DumbPrefixQuery : MultiTermQuery
	  {
		  private readonly TestPrefixRandom OuterInstance;

		internal readonly BytesRef Prefix;

		internal DumbPrefixQuery(TestPrefixRandom outerInstance, Term term) : base(term.field())
		{
			this.OuterInstance = outerInstance;
		  Prefix = term.bytes();
		}

		protected internal override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
		{
		  return new SimplePrefixTermsEnum(this, terms.iterator(null), Prefix);
		}

		private class SimplePrefixTermsEnum : FilteredTermsEnum
		{
			private readonly TestPrefixRandom.DumbPrefixQuery OuterInstance;

		  internal readonly BytesRef Prefix;

		  internal SimplePrefixTermsEnum(TestPrefixRandom.DumbPrefixQuery outerInstance, TermsEnum tenum, BytesRef prefix) : base(tenum)
		  {
			  this.OuterInstance = outerInstance;
			this.Prefix = prefix;
			InitialSeekTerm = new BytesRef("");
		  }

		  protected internal override AcceptStatus Accept(BytesRef term)
		  {
			return StringHelper.StartsWith(term, Prefix) ? AcceptStatus.YES : AcceptStatus.NO;
		  }
		}

		public override string ToString(string field)
		{
		  return field.ToString() + ":" + Prefix.ToString();
		}
	  }

	  /// <summary>
	  /// test a bunch of random prefixes </summary>
	  public virtual void TestPrefixes()
	  {
		  int num = atLeast(100);
		  for (int i = 0; i < num; i++)
		  {
			AssertSame(TestUtil.randomUnicodeString(random(), 5));
		  }
	  }

	  /// <summary>
	  /// check that the # of hits is the same as from a very
	  /// simple prefixquery implementation.
	  /// </summary>
	  private void AssertSame(string prefix)
	  {
		PrefixQuery smart = new PrefixQuery(new Term("field", prefix));
		DumbPrefixQuery dumb = new DumbPrefixQuery(this, new Term("field", prefix));

		TopDocs smartDocs = Searcher.search(smart, 25);
		TopDocs dumbDocs = Searcher.search(dumb, 25);
		CheckHits.checkEqual(smart, smartDocs.scoreDocs, dumbDocs.scoreDocs);
	  }
	}

}