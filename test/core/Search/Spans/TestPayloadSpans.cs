using System;
using System.Collections.Generic;

namespace Lucene.Net.Search.Spans
{

	/// <summary>
	/// Copyright 2004 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using TokenFilter = Lucene.Net.Analysis.TokenFilter;
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using Tokenizer = Lucene.Net.Analysis.Tokenizer;
	using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using TextField = Lucene.Net.Document.TextField;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using Term = Lucene.Net.Index.Term;
	using PayloadHelper = Lucene.Net.Search.Payloads.PayloadHelper;
	using PayloadSpanUtil = Lucene.Net.Search.Payloads.PayloadSpanUtil;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestPayloadSpans : LuceneTestCase
	{
	  private IndexSearcher Searcher_Renamed;
	  private Similarity Similarity = new DefaultSimilarity();
	  protected internal IndexReader IndexReader;
	  private IndexReader CloseIndexReader;
	  private Directory Directory;

	  public override void SetUp()
	  {
		base.setUp();
		PayloadHelper helper = new PayloadHelper();
		Searcher_Renamed = helper.SetUp(random(), Similarity, 1000);
		IndexReader = Searcher_Renamed.IndexReader;
	  }

	  public virtual void TestSpanTermQuery()
	  {
		SpanTermQuery stq;
		Spans spans;
		stq = new SpanTermQuery(new Term(PayloadHelper.FIELD, "seventy"));
		spans = MultiSpansWrapper.Wrap(IndexReader.Context, stq);
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		CheckSpans(spans, 100, 1, 1, 1);

		stq = new SpanTermQuery(new Term(PayloadHelper.NO_PAYLOAD_FIELD, "seventy"));
		spans = MultiSpansWrapper.Wrap(IndexReader.Context, stq);
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		CheckSpans(spans, 100, 0, 0, 0);
	  }

	  public virtual void TestSpanFirst()
	  {

		SpanQuery match;
		SpanFirstQuery sfq;
		match = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
		sfq = new SpanFirstQuery(match, 2);
		Spans spans = MultiSpansWrapper.Wrap(IndexReader.Context, sfq);
		CheckSpans(spans, 109, 1, 1, 1);
		//Test more complicated subclause
		SpanQuery[] clauses = new SpanQuery[2];
		clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
		clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "hundred"));
		match = new SpanNearQuery(clauses, 0, true);
		sfq = new SpanFirstQuery(match, 2);
		CheckSpans(MultiSpansWrapper.Wrap(IndexReader.Context, sfq), 100, 2, 1, 1);

		match = new SpanNearQuery(clauses, 0, false);
		sfq = new SpanFirstQuery(match, 2);
		CheckSpans(MultiSpansWrapper.Wrap(IndexReader.Context, sfq), 100, 2, 1, 1);

	  }

	  public virtual void TestSpanNot()
	  {
		SpanQuery[] clauses = new SpanQuery[2];
		clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
		clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "three"));
		SpanQuery spq = new SpanNearQuery(clauses, 5, true);
		SpanNotQuery snq = new SpanNotQuery(spq, new SpanTermQuery(new Term(PayloadHelper.FIELD, "two")));



		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer(this)).setSimilarity(Similarity));

		Document doc = new Document();
		doc.add(newTextField(PayloadHelper.FIELD, "one two three one four three", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader reader = writer.Reader;
		writer.close();


		CheckSpans(MultiSpansWrapper.Wrap(reader.Context, snq), 1,new int[]{2});
		reader.close();
		directory.close();
	  }

	  public virtual void TestNestedSpans()
	  {
		SpanTermQuery stq;
		Spans spans;
		IndexSearcher searcher = Searcher;
		stq = new SpanTermQuery(new Term(PayloadHelper.FIELD, "mark"));
		spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, stq);
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		CheckSpans(spans, 0, null);


		SpanQuery[] clauses = new SpanQuery[3];
		clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));
		clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));
		clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
		SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 12, false);

		spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, spanNearQuery);
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		CheckSpans(spans, 2, new int[]{3,3});


		clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
		clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));
		clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));

		spanNearQuery = new SpanNearQuery(clauses, 6, true);

		spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, spanNearQuery);

		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		CheckSpans(spans, 1, new int[]{3});

		clauses = new SpanQuery[2];

		clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "xx"));
		clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "rr"));

		spanNearQuery = new SpanNearQuery(clauses, 6, true);

		// xx within 6 of rr

		SpanQuery[] clauses2 = new SpanQuery[2];

		clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "yy"));
		clauses2[1] = spanNearQuery;

		SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses2, 6, false);

		// yy within 6 of xx within 6 of rr

		spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, nestedSpanNearQuery);
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		CheckSpans(spans, 2, new int[]{3,3});
		CloseIndexReader.close();
		Directory.close();
	  }

	  public virtual void TestFirstClauseWithoutPayload()
	  {
		Spans spans;
		IndexSearcher searcher = Searcher;

		SpanQuery[] clauses = new SpanQuery[3];
		clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "nopayload"));
		clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "qq"));
		clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "ss"));

		SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 6, true);

		SpanQuery[] clauses2 = new SpanQuery[2];

		clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "pp"));
		clauses2[1] = spanNearQuery;

		SpanNearQuery snq = new SpanNearQuery(clauses2, 6, false);

		SpanQuery[] clauses3 = new SpanQuery[2];

		clauses3[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "np"));
		clauses3[1] = snq;

		SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses3, 6, false);
		spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, nestedSpanNearQuery);

		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		CheckSpans(spans, 1, new int[]{3});
		CloseIndexReader.close();
		Directory.close();
	  }

	  public virtual void TestHeavilyNestedSpanQuery()
	  {
		Spans spans;
		IndexSearcher searcher = Searcher;

		SpanQuery[] clauses = new SpanQuery[3];
		clauses[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "one"));
		clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "two"));
		clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "three"));

		SpanNearQuery spanNearQuery = new SpanNearQuery(clauses, 5, true);

		clauses = new SpanQuery[3];
		clauses[0] = spanNearQuery;
		clauses[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "five"));
		clauses[2] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "six"));

		SpanNearQuery spanNearQuery2 = new SpanNearQuery(clauses, 6, true);

		SpanQuery[] clauses2 = new SpanQuery[2];
		clauses2[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "eleven"));
		clauses2[1] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "ten"));
		SpanNearQuery spanNearQuery3 = new SpanNearQuery(clauses2, 2, false);

		SpanQuery[] clauses3 = new SpanQuery[3];
		clauses3[0] = new SpanTermQuery(new Term(PayloadHelper.FIELD, "nine"));
		clauses3[1] = spanNearQuery2;
		clauses3[2] = spanNearQuery3;

		SpanNearQuery nestedSpanNearQuery = new SpanNearQuery(clauses3, 6, false);

		spans = MultiSpansWrapper.Wrap(searcher.TopReaderContext, nestedSpanNearQuery);
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		CheckSpans(spans, 2, new int[]{8, 8});
		CloseIndexReader.close();
		Directory.close();
	  }

	  public virtual void TestShrinkToAfterShortestMatch()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new TestPayloadAnalyzer(this)));

		Document doc = new Document();
		doc.add(new TextField("content", new StringReader("a b c d e f g h i j a k")));
		writer.addDocument(doc);

		IndexReader reader = writer.Reader;
		IndexSearcher @is = newSearcher(reader);
		writer.close();

		SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
		SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
		SpanQuery[] sqs = new SpanQuery[] {stq1, stq2};
		SpanNearQuery snq = new SpanNearQuery(sqs, 1, true);
		Spans spans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);

		TopDocs topDocs = @is.search(snq, 1);
		Set<string> payloadSet = new HashSet<string>();
		for (int i = 0; i < topDocs.scoreDocs.length; i++)
		{
		  while (spans.next())
		  {
			ICollection<sbyte[]> payloads = spans.Payload;

			foreach (sbyte [] payload in payloads)
			{
			  payloadSet.add(new string(payload, StandardCharsets.UTF_8));
			}
		  }
		}
		Assert.AreEqual(2, payloadSet.size());
		Assert.IsTrue(payloadSet.contains("a:Noise:10"));
		Assert.IsTrue(payloadSet.contains("k:Noise:11"));
		reader.close();
		directory.close();
	  }

	  public virtual void TestShrinkToAfterShortestMatch2()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new TestPayloadAnalyzer(this)));

		Document doc = new Document();
		doc.add(new TextField("content", new StringReader("a b a d k f a h i k a k")));
		writer.addDocument(doc);
		IndexReader reader = writer.Reader;
		IndexSearcher @is = newSearcher(reader);
		writer.close();

		SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
		SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
		SpanQuery[] sqs = new SpanQuery[] {stq1, stq2};
		SpanNearQuery snq = new SpanNearQuery(sqs, 0, true);
		Spans spans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);

		TopDocs topDocs = @is.search(snq, 1);
		Set<string> payloadSet = new HashSet<string>();
		for (int i = 0; i < topDocs.scoreDocs.length; i++)
		{
		  while (spans.next())
		  {
			ICollection<sbyte[]> payloads = spans.Payload;
			foreach (sbyte[] payload in payloads)
			{
			  payloadSet.add(new string(payload, StandardCharsets.UTF_8));
			}
		  }
		}
		Assert.AreEqual(2, payloadSet.size());
		Assert.IsTrue(payloadSet.contains("a:Noise:10"));
		Assert.IsTrue(payloadSet.contains("k:Noise:11"));
		reader.close();
		directory.close();
	  }

	  public virtual void TestShrinkToAfterShortestMatch3()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new TestPayloadAnalyzer(this)));

		Document doc = new Document();
		doc.add(new TextField("content", new StringReader("j k a l f k k p a t a k l k t a")));
		writer.addDocument(doc);
		IndexReader reader = writer.Reader;
		IndexSearcher @is = newSearcher(reader);
		writer.close();

		SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
		SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
		SpanQuery[] sqs = new SpanQuery[] {stq1, stq2};
		SpanNearQuery snq = new SpanNearQuery(sqs, 0, true);
		Spans spans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);

		TopDocs topDocs = @is.search(snq, 1);
		Set<string> payloadSet = new HashSet<string>();
		for (int i = 0; i < topDocs.scoreDocs.length; i++)
		{
		  while (spans.next())
		  {
			ICollection<sbyte[]> payloads = spans.Payload;

			foreach (sbyte [] payload in payloads)
			{
			  payloadSet.add(new string(payload, StandardCharsets.UTF_8));
			}
		  }
		}
		Assert.AreEqual(2, payloadSet.size());
		if (VERBOSE)
		{
		  foreach (String payload in payloadSet)
		  {
			Console.WriteLine("match:" + payload);
		  }

		}
		Assert.IsTrue(payloadSet.contains("a:Noise:10"));
		Assert.IsTrue(payloadSet.contains("k:Noise:11"));
		reader.close();
		directory.close();
	  }

	  public virtual void TestPayloadSpanUtil()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer(this)).setSimilarity(Similarity));

		Document doc = new Document();
		doc.add(newTextField(PayloadHelper.FIELD, "xx rr yy mm  pp", Field.Store.YES));
		writer.addDocument(doc);

		IndexReader reader = writer.Reader;
		writer.close();
		IndexSearcher searcher = newSearcher(reader);

		PayloadSpanUtil psu = new PayloadSpanUtil(searcher.TopReaderContext);

		ICollection<sbyte[]> payloads = psu.getPayloadsForQuery(new TermQuery(new Term(PayloadHelper.FIELD, "rr")));
		if (VERBOSE)
		{
		  Console.WriteLine("Num payloads:" + payloads.Count);
		  foreach (sbyte [] bytes in payloads)
		  {
			Console.WriteLine(new string(bytes, StandardCharsets.UTF_8));
		  }
		}
		reader.close();
		directory.close();
	  }

	  private void CheckSpans(Spans spans, int expectedNumSpans, int expectedNumPayloads, int expectedPayloadLength, int expectedFirstByte)
	  {
		Assert.IsTrue("spans is null and it shouldn't be", spans != null);
		//each position match should have a span associated with it, since there is just one underlying term query, there should
		//only be one entry in the span
		int seen = 0;
		while (spans.next() == true)
		{
		  //if we expect payloads, then isPayloadAvailable should be true
		  if (expectedNumPayloads > 0)
		  {
			Assert.IsTrue("isPayloadAvailable is not returning the correct value: " + spans.PayloadAvailable + " and it should be: " + (expectedNumPayloads > 0), spans.PayloadAvailable == true);
		  }
		  else
		  {
			Assert.IsTrue("isPayloadAvailable should be false", spans.PayloadAvailable == false);
		  }
		  //See payload helper, for the PayloadHelper.FIELD field, there is a single byte payload at every token
		  if (spans.PayloadAvailable)
		  {
			ICollection<sbyte[]> payload = spans.Payload;
			Assert.IsTrue("payload Size: " + payload.Count + " is not: " + expectedNumPayloads, payload.Count == expectedNumPayloads);
			foreach (sbyte [] thePayload in payload)
			{
			  Assert.IsTrue("payload[0] Size: " + thePayload.length + " is not: " + expectedPayloadLength, thePayload.length == expectedPayloadLength);
			  Assert.IsTrue(thePayload[0] + " does not equal: " + expectedFirstByte, thePayload[0] == expectedFirstByte);

			}

		  }
		  seen++;
		}
		Assert.IsTrue(seen + " does not equal: " + expectedNumSpans, seen == expectedNumSpans);
	  }

	  private IndexSearcher Searcher
	  {
		  get
		  {
			Directory = newDirectory();
			string[] docs = new string[]{"xx rr yy mm  pp","xx yy mm rr pp", "nopayload qq ss pp np", "one two three four five six seven eight nine ten eleven", "nine one two three four five six seven eight eleven ten"};
			RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer(this)).setSimilarity(Similarity));
    
			Document doc = null;
			for (int i = 0; i < docs.Length; i++)
			{
			  doc = new Document();
			  string docText = docs[i];
			  doc.add(newTextField(PayloadHelper.FIELD, docText, Field.Store.YES));
			  writer.addDocument(doc);
			}
    
			CloseIndexReader = writer.Reader;
			writer.close();
    
			IndexSearcher searcher = newSearcher(CloseIndexReader);
			return searcher;
		  }
	  }

	  private void CheckSpans(Spans spans, int numSpans, int[] numPayloads)
	  {
		int cnt = 0;

		while (spans.next() == true)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nSpans Dump --");
		  }
		  if (spans.PayloadAvailable)
		  {
			ICollection<sbyte[]> payload = spans.Payload;
			if (VERBOSE)
			{
			  Console.WriteLine("payloads for span:" + payload.Count);
			  foreach (sbyte [] bytes in payload)
			  {
				Console.WriteLine("doc:" + spans.doc() + " s:" + spans.start() + " e:" + spans.end() + " " + new string(bytes, StandardCharsets.UTF_8));
			  }
			}

			Assert.AreEqual(numPayloads[cnt],payload.Count);
		  }
		  else
		  {
			Assert.IsFalse("Expected spans:" + numPayloads[cnt] + " found: 0",numPayloads.Length > 0 && numPayloads[cnt] > 0);
		  }
		  cnt++;
		}

		Assert.AreEqual(numSpans, cnt);
	  }

	  internal sealed class PayloadAnalyzer : Analyzer
	  {
		  private readonly TestPayloadSpans OuterInstance;

		  public PayloadAnalyzer(TestPayloadSpans outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
		  return new TokenStreamComponents(result, new PayloadFilter(OuterInstance, result));
		}
	  }

	  internal sealed class PayloadFilter : TokenFilter
	  {
		  private readonly TestPayloadSpans OuterInstance;

		internal Set<string> Entities = new HashSet<string>();
		internal Set<string> Nopayload = new HashSet<string>();
		internal int Pos;
		internal PayloadAttribute PayloadAtt;
		internal CharTermAttribute TermAtt;
		internal PositionIncrementAttribute PosIncrAtt;

		public PayloadFilter(TestPayloadSpans outerInstance, TokenStream input) : base(input)
		{
			this.OuterInstance = outerInstance;
		  Pos = 0;
		  Entities.add("xx");
		  Entities.add("one");
		  Nopayload.add("nopayload");
		  Nopayload.add("np");
		  TermAtt = addAttribute(typeof(CharTermAttribute));
		  PosIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
		  PayloadAtt = addAttribute(typeof(PayloadAttribute));
		}

		public override bool IncrementToken()
		{
		  if (input.IncrementToken())
		  {
			string token = TermAtt.ToString();

			if (!Nopayload.contains(token))
			{
			  if (Entities.contains(token))
			  {
				PayloadAtt.Payload = new BytesRef(token + ":Entity:" + Pos);
			  }
			  else
			  {
				PayloadAtt.Payload = new BytesRef(token + ":Noise:" + Pos);
			  }
			}
			Pos += PosIncrAtt.PositionIncrement;
			return true;
		  }
		  return false;
		}

		public override void Reset()
		{
		  base.reset();
		  this.Pos = 0;
		}
	  }

	  public sealed class TestPayloadAnalyzer : Analyzer
	  {
		  private readonly TestPayloadSpans OuterInstance;

		  public TestPayloadAnalyzer(TestPayloadSpans outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
		  return new TokenStreamComponents(result, new PayloadFilter(OuterInstance, result));
		}
	  }
	}

}