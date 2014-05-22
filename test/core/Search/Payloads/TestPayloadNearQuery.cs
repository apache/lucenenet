using System;

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
	using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldInvertState = Lucene.Net.Index.FieldInvertState;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
	using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
	using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using English = Lucene.Net.Util.English;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;


	public class TestPayloadNearQuery : LuceneTestCase
	{
	  private static IndexSearcher Searcher;
	  private static IndexReader Reader;
	  private static Directory Directory;
	  private static BoostingSimilarity Similarity = new BoostingSimilarity();
	  private static sbyte[] Payload2 = new sbyte[]{2};
	  private static sbyte[] Payload4 = new sbyte[]{4};

	  private class PayloadAnalyzer : Analyzer
	  {
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
		internal readonly PayloadAttribute PayAtt;

		public PayloadFilter(TokenStream input, string fieldName) : base(input)
		{
		  this.FieldName = fieldName;
		  PayAtt = addAttribute(typeof(PayloadAttribute));
		}

		public override bool IncrementToken()
		{
		  bool result = false;
		  if (input.IncrementToken())
		  {
			if (NumSeen % 2 == 0)
			{
			  PayAtt.Payload = new BytesRef(Payload2);
			}
			else
			{
			  PayAtt.Payload = new BytesRef(Payload4);
			}
			NumSeen++;
			result = true;
		  }
		  return result;
		}

		public override void Reset()
		{
		  base.reset();
		  this.NumSeen = 0;
		}
	  }

	  private PayloadNearQuery NewPhraseQuery(string fieldName, string phrase, bool inOrder, PayloadFunction function)
	  {
		string[] words = phrase.Split("[\\s]+", true);
		SpanQuery[] clauses = new SpanQuery[words.Length];
		for (int i = 0;i < clauses.Length;i++)
		{
		  clauses[i] = new SpanTermQuery(new Term(fieldName, words[i]));
		}
		return new PayloadNearQuery(clauses, 0, inOrder, function);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer()).setSimilarity(Similarity));
		//writer.infoStream = System.out;
		for (int i = 0; i < 1000; i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("field", English.intToEnglish(i), Field.Store.YES));
		  string txt = English.intToEnglish(i) + ' ' + English.intToEnglish(i + 1);
		  doc.add(newTextField("field2", txt, Field.Store.YES));
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
		PayloadNearQuery query;
		TopDocs hits;

		query = NewPhraseQuery("field", "twenty two", true, new AveragePayloadFunction());
		QueryUtils.check(query);

		// all 10 hits should have score = 3 because adjacent terms have payloads of 2,4
		// and all the similarity factors are set to 1
		hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		Assert.IsTrue("should be 10 hits", hits.totalHits == 10);
		for (int j = 0; j < hits.scoreDocs.length; j++)
		{
		  ScoreDoc doc = hits.scoreDocs[j];
		  Assert.IsTrue(doc.score + " does not equal: " + 3, doc.score == 3);
		}
		for (int i = 1;i < 10;i++)
		{
		  query = NewPhraseQuery("field", English.intToEnglish(i) + " hundred", true, new AveragePayloadFunction());
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: run query=" + query);
		  }
		  // all should have score = 3 because adjacent terms have payloads of 2,4
		  // and all the similarity factors are set to 1
		  hits = Searcher.search(query, null, 100);
		  Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		  Assert.AreEqual("should be 100 hits", 100, hits.totalHits);
		  for (int j = 0; j < hits.scoreDocs.length; j++)
		  {
			ScoreDoc doc = hits.scoreDocs[j];
			//        System.out.println("Doc: " + doc.toString());
			//        System.out.println("Explain: " + searcher.explain(query, doc.doc));
			Assert.IsTrue(doc.score + " does not equal: " + 3, doc.score == 3);
		  }
		}
	  }


	  public virtual void TestPayloadNear()
	  {
		SpanNearQuery q1, q2;
		PayloadNearQuery query;
		//SpanNearQuery(clauses, 10000, false)
		q1 = SpanNearQuery("field2", "twenty two");
		q2 = SpanNearQuery("field2", "twenty three");
		SpanQuery[] clauses = new SpanQuery[2];
		clauses[0] = q1;
		clauses[1] = q2;
		query = new PayloadNearQuery(clauses, 10, false);
		//System.out.println(query.toString());
		Assert.AreEqual(12, Searcher.search(query, null, 100).totalHits);
		/*
		System.out.println(hits.totalHits);
		for (int j = 0; j < hits.scoreDocs.length; j++) {
		  ScoreDoc doc = hits.scoreDocs[j];
		  System.out.println("doc: "+doc.doc+", score: "+doc.score);
		}
		*/
	  }

	  public virtual void TestAverageFunction()
	  {
		PayloadNearQuery query;
		TopDocs hits;

		query = NewPhraseQuery("field", "twenty two", true, new AveragePayloadFunction());
		QueryUtils.check(query);
		// all 10 hits should have score = 3 because adjacent terms have payloads of 2,4
		// and all the similarity factors are set to 1
		hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		Assert.IsTrue("should be 10 hits", hits.totalHits == 10);
		for (int j = 0; j < hits.scoreDocs.length; j++)
		{
		  ScoreDoc doc = hits.scoreDocs[j];
		  Assert.IsTrue(doc.score + " does not equal: " + 3, doc.score == 3);
		  Explanation explain = Searcher.explain(query, hits.scoreDocs[j].doc);
		  string exp = explain.ToString();
		  Assert.IsTrue(exp, exp.IndexOf("AveragePayloadFunction") > -1);
		  Assert.IsTrue(hits.scoreDocs[j].score + " explain value does not equal: " + 3, explain.Value == 3f);
		}
	  }
	  public virtual void TestMaxFunction()
	  {
		PayloadNearQuery query;
		TopDocs hits;

		query = NewPhraseQuery("field", "twenty two", true, new MaxPayloadFunction());
		QueryUtils.check(query);
		// all 10 hits should have score = 4 (max payload value)
		hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		Assert.IsTrue("should be 10 hits", hits.totalHits == 10);
		for (int j = 0; j < hits.scoreDocs.length; j++)
		{
		  ScoreDoc doc = hits.scoreDocs[j];
		  Assert.IsTrue(doc.score + " does not equal: " + 4, doc.score == 4);
		  Explanation explain = Searcher.explain(query, hits.scoreDocs[j].doc);
		  string exp = explain.ToString();
		  Assert.IsTrue(exp, exp.IndexOf("MaxPayloadFunction") > -1);
		  Assert.IsTrue(hits.scoreDocs[j].score + " explain value does not equal: " + 4, explain.Value == 4f);
		}
	  }
	  public virtual void TestMinFunction()
	  {
		PayloadNearQuery query;
		TopDocs hits;

		query = NewPhraseQuery("field", "twenty two", true, new MinPayloadFunction());
		QueryUtils.check(query);
		// all 10 hits should have score = 2 (min payload value)
		hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		Assert.IsTrue("should be 10 hits", hits.totalHits == 10);
		for (int j = 0; j < hits.scoreDocs.length; j++)
		{
		  ScoreDoc doc = hits.scoreDocs[j];
		  Assert.IsTrue(doc.score + " does not equal: " + 2, doc.score == 2);
		  Explanation explain = Searcher.explain(query, hits.scoreDocs[j].doc);
		  string exp = explain.ToString();
		  Assert.IsTrue(exp, exp.IndexOf("MinPayloadFunction") > -1);
		  Assert.IsTrue(hits.scoreDocs[j].score + " explain value does not equal: " + 2, explain.Value == 2f);
		}
	  }
	  private SpanQuery[] Clauses
	  {
		  get
		  {
			  SpanNearQuery q1, q2;
			  q1 = SpanNearQuery("field2", "twenty two");
			  q2 = SpanNearQuery("field2", "twenty three");
			  SpanQuery[] clauses = new SpanQuery[2];
			  clauses[0] = q1;
			  clauses[1] = q2;
			  return clauses;
		  }
	  }
	  private SpanNearQuery SpanNearQuery(string fieldName, string words)
	  {
		string[] wordList = words.Split("[\\s]+", true);
		SpanQuery[] clauses = new SpanQuery[wordList.Length];
		for (int i = 0;i < clauses.Length;i++)
		{
		  clauses[i] = new PayloadTermQuery(new Term(fieldName, wordList[i]), new AveragePayloadFunction());
		}
		return new SpanNearQuery(clauses, 10000, false);
	  }

	  public virtual void TestLongerSpan()
	  {
		PayloadNearQuery query;
		TopDocs hits;
		query = NewPhraseQuery("field", "nine hundred ninety nine", true, new AveragePayloadFunction());
		hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		ScoreDoc doc = hits.scoreDocs[0];
		//    System.out.println("Doc: " + doc.toString());
		//    System.out.println("Explain: " + searcher.explain(query, doc.doc));
		Assert.IsTrue("there should only be one hit", hits.totalHits == 1);
		// should have score = 3 because adjacent terms have payloads of 2,4
		Assert.IsTrue(doc.score + " does not equal: " + 3, doc.score == 3);
	  }

	  public virtual void TestComplexNested()
	  {
		PayloadNearQuery query;
		TopDocs hits;

		// combine ordered and unordered spans with some nesting to make sure all payloads are counted

		SpanQuery q1 = NewPhraseQuery("field", "nine hundred", true, new AveragePayloadFunction());
		SpanQuery q2 = NewPhraseQuery("field", "ninety nine", true, new AveragePayloadFunction());
		SpanQuery q3 = NewPhraseQuery("field", "nine ninety", false, new AveragePayloadFunction());
		SpanQuery q4 = NewPhraseQuery("field", "hundred nine", false, new AveragePayloadFunction());
		SpanQuery[] clauses = new SpanQuery[] {new PayloadNearQuery(new SpanQuery[] {q1,q2}, 0, true), new PayloadNearQuery(new SpanQuery[] {q3,q4}, 0, false)};
		query = new PayloadNearQuery(clauses, 0, false);
		hits = Searcher.search(query, null, 100);
		Assert.IsTrue("hits is null and it shouldn't be", hits != null);
		// should be only 1 hit - doc 999
		Assert.IsTrue("should only be one hit", hits.scoreDocs.length == 1);
		// the score should be 3 - the average of all the underlying payloads
		ScoreDoc doc = hits.scoreDocs[0];
		//    System.out.println("Doc: " + doc.toString());
		//    System.out.println("Explain: " + searcher.explain(query, doc.doc));
		Assert.IsTrue(doc.score + " does not equal: " + 3, doc.score == 3);
	  }

	  internal class BoostingSimilarity : DefaultSimilarity
	  {

		public override float QueryNorm(float sumOfSquaredWeights)
		{
		  return 1.0f;
		}

		public override float Coord(int overlap, int maxOverlap)
		{
		  return 1.0f;
		}

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
		  return 1.0f;
		}

		public override float Tf(float freq)
		{
		  return 1.0f;
		}

		// idf used for phrase queries
		public override Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
		{
		  return new Explanation(1.0f, "Inexplicable");
		}
	  }
	}

}