namespace Lucene.Net.Search.Payloads
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

	using Lucene.Net.Analysis;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using English = Lucene.Net.Util.English;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using MultiSpansWrapper = Lucene.Net.Search.Spans.MultiSpansWrapper;
	using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
	using Spans = Lucene.Net.Search.Spans.Spans;
	using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using FieldInvertState = Lucene.Net.Index.FieldInvertState;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;



	/// 
	/// 
	/// 
	public class TestPayloadTermQuery : LuceneTestCase
	{
	  private static IndexSearcher Searcher;
	  private static IndexReader Reader;
	  private static Similarity Similarity = new BoostingSimilarity();
	  private static readonly sbyte[] PayloadField = new sbyte[]{1};
	  private static readonly sbyte[] PayloadMultiField1 = new sbyte[]{2};
	  private static readonly sbyte[] PayloadMultiField2 = new sbyte[]{4};
	  protected internal static Directory Directory;

	  private class PayloadAnalyzer : Analyzer
	  {

		internal PayloadAnalyzer() : base(PER_FIELD_REUSE_STRATEGY)
		{
		}

		public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
		  return new TokenStreamComponents(result, new PayloadFilter(result, fieldName));
		}
	  }

	  private class PayloadFilter : TokenFilter
	  {
		internal readonly string FieldName;
		internal int NumSeen = 0;

		internal readonly PayloadAttribute PayloadAtt;

		public PayloadFilter(TokenStream input, string fieldName) : base(input)
		{
		  this.FieldName = fieldName;
		  PayloadAtt = addAttribute(typeof(PayloadAttribute));
		}

		public override bool IncrementToken()
		{
		  bool hasNext = input.IncrementToken();
		  if (hasNext)
		  {
			if (FieldName.Equals("field"))
			{
			  PayloadAtt.Payload = new BytesRef(PayloadField);
			}
			else if (FieldName.Equals("multiField"))
			{
			  if (NumSeen % 2 == 0)
			  {
				PayloadAtt.Payload = new BytesRef(PayloadMultiField1);
			  }
			  else
			  {
				PayloadAtt.Payload = new BytesRef(PayloadMultiField2);
			  }
			  NumSeen++;
			}
			return true;
		  }
		  else
		  {
			return false;
		  }
		}

		public override void Reset()
		{
		  base.reset();
		  this.NumSeen = 0;
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer()).setSimilarity(Similarity).setMergePolicy(newLogMergePolicy()));
		//writer.infoStream = System.out;
		for (int i = 0; i < 1000; i++)
		{
		  Document doc = new Document();
		  Field noPayloadField = newTextField(PayloadHelper.NO_PAYLOAD_FIELD, English.intToEnglish(i), Field.Store.YES);
		  //noPayloadField.setBoost(0);
		  doc.add(noPayloadField);
		  doc.add(newTextField("field", English.intToEnglish(i), Field.Store.YES));
		  doc.add(newTextField("multiField", English.intToEnglish(i) + "  " + English.intToEnglish(i), Field.Store.YES));
		  writer.addDocument(doc);
		}
		Reader = writer.Reader;
		writer.close();

		Searcher = newSearcher(Reader);
		Searcher.Similarity = Similarity;
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Searcher = null;
		Reader.close();
		Reader = null;
		Directory.close();
		Directory = null;
	  }

	  public virtual void Test()
	  {
		PayloadTermQuery query = new PayloadTermQuery(new Term("field", "seventy"), new MaxPayloadFunction());
		TopDocs hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 100, hits.totalHits == 100);

		//they should all have the exact same score, because they all contain seventy once, and we set
		//all the other similarity factors to be 1

		Assert.IsTrue(hits.MaxScore + " does not equal: " + 1, hits.MaxScore == 1);
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  ScoreDoc doc = hits.scoreDocs[i];
		  Assert.IsTrue(doc.score + " does not equal: " + 1, doc.score == 1);
		}
		CheckHits.checkExplanations(query, PayloadHelper.FIELD, Searcher, true);
		Spans spans = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, query);
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		/*float score = hits.score(0);
		for (int i =1; i < hits.length(); i++)
		{
		  Assert.IsTrue("scores are not equal and they should be", score == hits.score(i));
		}*/

	  }

	  public virtual void TestQuery()
	  {
		PayloadTermQuery boostingFuncTermQuery = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new MaxPayloadFunction());
		QueryUtils.check(boostingFuncTermQuery);

		SpanTermQuery spanTermQuery = new SpanTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"));

		Assert.IsTrue(boostingFuncTermQuery.Equals(spanTermQuery) == spanTermQuery.Equals(boostingFuncTermQuery));

		PayloadTermQuery boostingFuncTermQuery2 = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new AveragePayloadFunction());

		QueryUtils.checkUnequal(boostingFuncTermQuery, boostingFuncTermQuery2);
	  }

	  public virtual void TestMultipleMatchesPerDoc()
	  {
		PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new MaxPayloadFunction());
		TopDocs hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 100, hits.totalHits == 100);

		//they should all have the exact same score, because they all contain seventy once, and we set
		//all the other similarity factors to be 1

		//System.out.println("Hash: " + seventyHash + " Twice Hash: " + 2*seventyHash);
		Assert.IsTrue(hits.MaxScore + " does not equal: " + 4.0, hits.MaxScore == 4.0);
		//there should be exactly 10 items that score a 4, all the rest should score a 2
		//The 10 items are: 70 + i*100 where i in [0-9]
		int numTens = 0;
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  ScoreDoc doc = hits.scoreDocs[i];
		  if (doc.doc % 10 == 0)
		  {
			numTens++;
			Assert.IsTrue(doc.score + " does not equal: " + 4.0, doc.score == 4.0);
		  }
		  else
		  {
			Assert.IsTrue(doc.score + " does not equal: " + 2, doc.score == 2);
		  }
		}
		Assert.IsTrue(numTens + " does not equal: " + 10, numTens == 10);
		CheckHits.checkExplanations(query, "field", Searcher, true);
		Spans spans = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, query);
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		//should be two matches per document
		int count = 0;
		//100 hits times 2 matches per hit, we should have 200 in count
		while (spans.next())
		{
		  count++;
		}
		Assert.IsTrue(count + " does not equal: " + 200, count == 200);
	  }

	  //Set includeSpanScore to false, in which case just the payload score comes through.
	  public virtual void TestIgnoreSpanScorer()
	  {
		PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.MULTI_FIELD, "seventy"), new MaxPayloadFunction(), false);

		IndexReader reader = DirectoryReader.open(Directory);
		IndexSearcher theSearcher = newSearcher(reader);
		theSearcher.Similarity = new FullSimilarity();
		TopDocs hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 100, hits.totalHits == 100);

		//they should all have the exact same score, because they all contain seventy once, and we set
		//all the other similarity factors to be 1

		//System.out.println("Hash: " + seventyHash + " Twice Hash: " + 2*seventyHash);
		Assert.IsTrue(hits.MaxScore + " does not equal: " + 4.0, hits.MaxScore == 4.0);
		//there should be exactly 10 items that score a 4, all the rest should score a 2
		//The 10 items are: 70 + i*100 where i in [0-9]
		int numTens = 0;
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  ScoreDoc doc = hits.scoreDocs[i];
		  if (doc.doc % 10 == 0)
		  {
			numTens++;
			Assert.IsTrue(doc.score + " does not equal: " + 4.0, doc.score == 4.0);
		  }
		  else
		  {
			Assert.IsTrue(doc.score + " does not equal: " + 2, doc.score == 2);
		  }
		}
		Assert.IsTrue(numTens + " does not equal: " + 10, numTens == 10);
		CheckHits.checkExplanations(query, "field", Searcher, true);
		Spans spans = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, query);
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		//should be two matches per document
		int count = 0;
		//100 hits times 2 matches per hit, we should have 200 in count
		while (spans.next())
		{
		  count++;
		}
		reader.close();
	  }

	  public virtual void TestNoMatch()
	  {
		PayloadTermQuery query = new PayloadTermQuery(new Term(PayloadHelper.FIELD, "junk"), new MaxPayloadFunction());
		TopDocs hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 0, hits.totalHits == 0);

	  }

	  public virtual void TestNoPayload()
	  {
		PayloadTermQuery q1 = new PayloadTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "zero"), new MaxPayloadFunction());
		PayloadTermQuery q2 = new PayloadTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "foo"), new MaxPayloadFunction());
		BooleanClause c1 = new BooleanClause(q1, BooleanClause.Occur_e.MUST);
		BooleanClause c2 = new BooleanClause(q2, BooleanClause.Occur_e.MUST_NOT);
		BooleanQuery query = new BooleanQuery();
		query.add(c1);
		query.add(c2);
		TopDocs hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		Assert.IsTrue("hits Size: " + hits.totalHits + " is not: " + 1, hits.totalHits == 1);
		int[] results = new int[1];
		results[0] = 0; //hits.scoreDocs[0].doc;
		CheckHits.checkHitCollector(random(), query, PayloadHelper.NO_PAYLOAD_FIELD, Searcher, results);
	  }

	  internal class BoostingSimilarity : DefaultSimilarity
	  {

		public override float QueryNorm(float sumOfSquaredWeights)
		{
		  return 1;
		}

		public override float Coord(int overlap, int maxOverlap)
		{
		  return 1;
		}

		// TODO: Remove warning after API has been finalized
		public override float ScorePayload(int docId, int start, int end, BytesRef payload)
		{
		  //we know it is size 4 here, so ignore the offset/length
		  return payload.bytes[payload.offset];
		}

		//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		//Make everything else 1 so we see the effect of the payload
		//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		public override float LengthNorm(FieldInvertState state)
		{
		  return state.Boost;
		}

		public override float SloppyFreq(int distance)
		{
		  return 1;
		}

		public override float Idf(long docFreq, long numDocs)
		{
		  return 1;
		}

		public override float Tf(float freq)
		{
		  return freq == 0 ? 0 : 1;
		}
	  }

	  internal class FullSimilarity : DefaultSimilarity
	  {
		public virtual float ScorePayload(int docId, string fieldName, sbyte[] payload, int offset, int length)
		{
		  //we know it is size 4 here, so ignore the offset/length
		  return payload[offset];
		}
	  }

	}

}