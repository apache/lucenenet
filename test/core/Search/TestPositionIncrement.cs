using System;
using System.Collections.Generic;

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


	using Lucene.Net.Analysis;
	using OffsetAttribute = Lucene.Net.Analysis.Tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using TextField = Lucene.Net.Document.TextField;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using PayloadSpanUtil = Lucene.Net.Search.Payloads.PayloadSpanUtil;
	using MultiSpansWrapper = Lucene.Net.Search.Spans.MultiSpansWrapper;
	using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
	using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
	using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
	using Spans = Lucene.Net.Search.Spans.Spans;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using BytesRef = Lucene.Net.Util.BytesRef;

	/// <summary>
	/// Term position unit test.
	/// 
	/// 
	/// </summary>
	public class TestPositionIncrement : LuceneTestCase
	{

	  internal const bool VERBOSE = false;

	  public virtual void TestSetPosition()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
		Directory store = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), store, analyzer);
		Document d = new Document();
		d.add(newTextField("field", "bogus", Field.Store.YES));
		writer.addDocument(d);
		IndexReader reader = writer.Reader;
		writer.close();


		IndexSearcher searcher = newSearcher(reader);

		DocsAndPositionsEnum pos = MultiFields.getTermPositionsEnum(searcher.IndexReader, MultiFields.getLiveDocs(searcher.IndexReader), "field", new BytesRef("1"));
		pos.nextDoc();
		// first token should be at position 0
		Assert.AreEqual(0, pos.nextPosition());

		pos = MultiFields.getTermPositionsEnum(searcher.IndexReader, MultiFields.getLiveDocs(searcher.IndexReader), "field", new BytesRef("2"));
		pos.nextDoc();
		// second token should be at position 2
		Assert.AreEqual(2, pos.nextPosition());

		PhraseQuery q;
		ScoreDoc[] hits;

		q = new PhraseQuery();
		q.add(new Term("field", "1"));
		q.add(new Term("field", "2"));
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		// same as previous, just specify positions explicitely.
		q = new PhraseQuery();
		q.add(new Term("field", "1"),0);
		q.add(new Term("field", "2"),1);
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		// specifying correct positions should find the phrase.
		q = new PhraseQuery();
		q.add(new Term("field", "1"),0);
		q.add(new Term("field", "2"),2);
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		q = new PhraseQuery();
		q.add(new Term("field", "2"));
		q.add(new Term("field", "3"));
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		q = new PhraseQuery();
		q.add(new Term("field", "3"));
		q.add(new Term("field", "4"));
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		// phrase query would find it when correct positions are specified. 
		q = new PhraseQuery();
		q.add(new Term("field", "3"),0);
		q.add(new Term("field", "4"),0);
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		// phrase query should fail for non existing searched term 
		// even if there exist another searched terms in the same searched position. 
		q = new PhraseQuery();
		q.add(new Term("field", "3"),0);
		q.add(new Term("field", "9"),0);
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		// multi-phrase query should succed for non existing searched term
		// because there exist another searched terms in the same searched position. 
		MultiPhraseQuery mq = new MultiPhraseQuery();
		mq.add(new Term[]{new Term("field", "3"),new Term("field", "9")},0);
		hits = searcher.search(mq, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		q = new PhraseQuery();
		q.add(new Term("field", "2"));
		q.add(new Term("field", "4"));
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		q = new PhraseQuery();
		q.add(new Term("field", "3"));
		q.add(new Term("field", "5"));
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		q = new PhraseQuery();
		q.add(new Term("field", "4"));
		q.add(new Term("field", "5"));
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		q = new PhraseQuery();
		q.add(new Term("field", "2"));
		q.add(new Term("field", "5"));
		hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		reader.close();
		store.close();
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestPositionIncrement OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestPositionIncrement outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new TokenizerAnonymousInnerClassHelper(this, reader));
		  }

		  private class TokenizerAnonymousInnerClassHelper : Tokenizer
		  {
			  private readonly AnalyzerAnonymousInnerClassHelper OuterInstance;

			  public TokenizerAnonymousInnerClassHelper(AnalyzerAnonymousInnerClassHelper outerInstance, Reader reader) : base(reader)
			  {
				  this.outerInstance = outerInstance;
				  TOKENS = {"1", "2", "3", "4", "5"};
				  INCREMENTS = {1, 2, 1, 0, 1};
				  i = 0;
				  posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
				  termAtt = addAttribute(typeof(CharTermAttribute));
				  offsetAtt = addAttribute(typeof(OffsetAttribute));
			  }

					// TODO: use CannedTokenStream
			  private readonly string[] TOKENS;
			  private readonly int[] INCREMENTS;
			  private int i;

			  internal PositionIncrementAttribute posIncrAtt;
			  internal CharTermAttribute termAtt;
			  internal OffsetAttribute offsetAtt;

			  public override bool IncrementToken()
			  {
				if (i == TOKENS.length)
				{
				  return false;
				}
				ClearAttributes();
				termAtt.append(TOKENS[i]);
				offsetAtt.SetOffset(i,i);
				posIncrAtt.PositionIncrement = INCREMENTS[i];
				i++;
				return true;
			  }

			  public override void Reset()
			  {
				base.reset();
				this.i = 0;
			  }
		  }
	  }

	  public virtual void TestPayloadsPos0()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, new MockPayloadAnalyzer());
		Document doc = new Document();
		doc.add(new TextField("content", new StringReader("a a b c d e a f g h i j a b k k")));
		writer.addDocument(doc);

		IndexReader readerFromWriter = writer.Reader;
		AtomicReader r = SlowCompositeReaderWrapper.wrap(readerFromWriter);

		DocsAndPositionsEnum tp = r.termPositionsEnum(new Term("content", "a"));

		int count = 0;
		Assert.IsTrue(tp.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		// "a" occurs 4 times
		Assert.AreEqual(4, tp.freq());
		Assert.AreEqual(0, tp.nextPosition());
		Assert.AreEqual(1, tp.nextPosition());
		Assert.AreEqual(3, tp.nextPosition());
		Assert.AreEqual(6, tp.nextPosition());

		// only one doc has "a"
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, tp.nextDoc());

		IndexSearcher @is = newSearcher(readerFromWriter);

		SpanTermQuery stq1 = new SpanTermQuery(new Term("content", "a"));
		SpanTermQuery stq2 = new SpanTermQuery(new Term("content", "k"));
		SpanQuery[] sqs = new SpanQuery[] {stq1, stq2};
		SpanNearQuery snq = new SpanNearQuery(sqs, 30, false);

		count = 0;
		bool sawZero = false;
		if (VERBOSE)
		{
		  Console.WriteLine("\ngetPayloadSpans test");
		}
		Spans pspans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);
		while (pspans.next())
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("doc " + pspans.doc() + ": span " + pspans.start() + " to " + pspans.end());
		  }
		  ICollection<sbyte[]> payloads = pspans.Payload;
		  sawZero |= pspans.start() == 0;
		  foreach (sbyte[] bytes in payloads)
		  {
			count++;
			if (VERBOSE)
			{
			  Console.WriteLine("  payload: " + new string(bytes, StandardCharsets.UTF_8));
			}
		  }
		}
		Assert.IsTrue(sawZero);
		Assert.AreEqual(5, count);

		// System.out.println("\ngetSpans test");
		Spans spans = MultiSpansWrapper.Wrap(@is.TopReaderContext, snq);
		count = 0;
		sawZero = false;
		while (spans.next())
		{
		  count++;
		  sawZero |= spans.start() == 0;
		  // System.out.println(spans.doc() + " - " + spans.start() + " - " +
		  // spans.end());
		}
		Assert.AreEqual(4, count);
		Assert.IsTrue(sawZero);

		// System.out.println("\nPayloadSpanUtil test");

		sawZero = false;
		PayloadSpanUtil psu = new PayloadSpanUtil(@is.TopReaderContext);
		ICollection<sbyte[]> pls = psu.getPayloadsForQuery(snq);
		count = pls.Count;
		foreach (sbyte[] bytes in pls)
		{
		  string s = new string(bytes, StandardCharsets.UTF_8);
		  //System.out.println(s);
		  sawZero |= s.Equals("pos: 0");
		}
		Assert.AreEqual(5, count);
		Assert.IsTrue(sawZero);
		writer.close();
		@is.IndexReader.close();
		dir.close();
	  }
	}

}