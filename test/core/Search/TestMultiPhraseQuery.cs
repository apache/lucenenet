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


	using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
	using Token = Lucene.Net.Analysis.Token;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using Ignore = org.junit.Ignore;

	/// <summary>
	/// this class tests the MultiPhraseQuery class.
	/// 
	/// 
	/// </summary>
	public class TestMultiPhraseQuery : LuceneTestCase
	{

	  public virtual void TestPhrasePrefix()
	  {
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		Add("blueberry pie", writer);
		Add("blueberry strudel", writer);
		Add("blueberry pizza", writer);
		Add("blueberry chewing gum", writer);
		Add("bluebird pizza", writer);
		Add("bluebird foobar pizza", writer);
		Add("piccadilly circus", writer);

		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);

		// search for "blueberry pi*":
		MultiPhraseQuery query1 = new MultiPhraseQuery();
		// search for "strawberry pi*":
		MultiPhraseQuery query2 = new MultiPhraseQuery();
		query1.add(new Term("body", "blueberry"));
		query2.add(new Term("body", "strawberry"));

		LinkedList<Term> termsWithPrefix = new LinkedList<Term>();

		// this TermEnum gives "piccadilly", "pie" and "pizza".
		string prefix = "pi";
		TermsEnum te = MultiFields.getFields(reader).terms("body").iterator(null);
		te.seekCeil(new BytesRef(prefix));
		do
		{
		  string s = te.term().utf8ToString();
		  if (s.StartsWith(prefix))
		  {
			termsWithPrefix.AddLast(new Term("body", s));
		  }
		  else
		  {
			break;
		  }
		} while (te.next() != null);

		query1.add(termsWithPrefix.toArray(new Term[0]));
		Assert.AreEqual("body:\"blueberry (piccadilly pie pizza)\"", query1.ToString());
		query2.add(termsWithPrefix.toArray(new Term[0]));
		Assert.AreEqual("body:\"strawberry (piccadilly pie pizza)\"", query2.ToString());

		ScoreDoc[] result;
		result = searcher.search(query1, null, 1000).scoreDocs;
		Assert.AreEqual(2, result.Length);
		result = searcher.search(query2, null, 1000).scoreDocs;
		Assert.AreEqual(0, result.Length);

		// search for "blue* pizza":
		MultiPhraseQuery query3 = new MultiPhraseQuery();
		termsWithPrefix.Clear();
		prefix = "blue";
		te.seekCeil(new BytesRef(prefix));

		do
		{
		  if (te.term().utf8ToString().StartsWith(prefix))
		  {
			termsWithPrefix.AddLast(new Term("body", te.term().utf8ToString()));
		  }
		} while (te.next() != null);

		query3.add(termsWithPrefix.toArray(new Term[0]));
		query3.add(new Term("body", "pizza"));

		result = searcher.search(query3, null, 1000).scoreDocs;
		Assert.AreEqual(2, result.Length); // blueberry pizza, bluebird pizza
		Assert.AreEqual("body:\"(blueberry bluebird) pizza\"", query3.ToString());

		// test slop:
		query3.Slop = 1;
		result = searcher.search(query3, null, 1000).scoreDocs;

		// just make sure no exc:
		searcher.explain(query3, 0);

		Assert.AreEqual(3, result.Length); // blueberry pizza, bluebird pizza, bluebird
										// foobar pizza

		MultiPhraseQuery query4 = new MultiPhraseQuery();
		try
		{
		  query4.add(new Term("field1", "foo"));
		  query4.add(new Term("field2", "foobar"));
		  Assert.Fail();
		}
		catch (System.ArgumentException e)
		{
		  // okay, all terms must belong to the same field
		}

		writer.close();
		reader.close();
		indexStore.close();
	  }

	  // LUCENE-2580
	  public virtual void TestTall()
	  {
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		Add("blueberry chocolate pie", writer);
		Add("blueberry chocolate tart", writer);
		IndexReader r = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(r);
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(new Term("body", "blueberry"));
		q.add(new Term("body", "chocolate"));
		q.add(new Term[] {new Term("body", "pie"), new Term("body", "tart")});
		Assert.AreEqual(2, searcher.search(q, 1).totalHits);
		r.close();
		indexStore.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore public void testMultiSloppyWithRepeats() throws java.io.IOException
	  public virtual void TestMultiSloppyWithRepeats() //LUCENE-3821 fixes sloppy phrase scoring, except for this known problem
	  {
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		Add("a b c d e f g h i k", writer);
		IndexReader r = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(r);

		MultiPhraseQuery q = new MultiPhraseQuery();
		// this will fail, when the scorer would propagate [a] rather than [a,b],
		q.add(new Term[] {new Term("body", "a"), new Term("body", "b")});
		q.add(new Term[] {new Term("body", "a")});
		q.Slop = 6;
		Assert.AreEqual(1, searcher.search(q, 1).totalHits); // should match on "a b"

		r.close();
		indexStore.close();
	  }

	  public virtual void TestMultiExactWithRepeats()
	  {
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		Add("a b c d e f g h i k", writer);
		IndexReader r = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(r);
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(new Term[] {new Term("body", "a"), new Term("body", "d")}, 0);
		q.add(new Term[] {new Term("body", "a"), new Term("body", "f")}, 2);
		Assert.AreEqual(1, searcher.search(q, 1).totalHits); // should match on "a b"
		r.close();
		indexStore.close();
	  }

	  private void Add(string s, RandomIndexWriter writer)
	  {
		Document doc = new Document();
		doc.add(newTextField("body", s, Field.Store.YES));
		writer.addDocument(doc);
	  }

	  public virtual void TestBooleanQueryContainingSingleTermPrefixQuery()
	  {
		// this tests against bug 33161 (now fixed)
		// In order to cause the bug, the outer query must have more than one term
		// and all terms required.
		// The contained PhraseMultiQuery must contain exactly one term array.
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		Add("blueberry pie", writer);
		Add("blueberry chewing gum", writer);
		Add("blue raspberry pie", writer);

		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);
		// this query will be equivalent to +body:pie +body:"blue*"
		BooleanQuery q = new BooleanQuery();
		q.add(new TermQuery(new Term("body", "pie")), BooleanClause.Occur_e.MUST);

		MultiPhraseQuery trouble = new MultiPhraseQuery();
		trouble.add(new Term[] {new Term("body", "blueberry"), new Term("body", "blue")});
		q.add(trouble, BooleanClause.Occur_e.MUST);

		// exception will be thrown here without fix
		ScoreDoc[] hits = searcher.search(q, null, 1000).scoreDocs;

		Assert.AreEqual("Wrong number of hits", 2, hits.Length);

		// just make sure no exc:
		searcher.explain(q, 0);

		writer.close();
		reader.close();
		indexStore.close();
	  }

	  public virtual void TestPhrasePrefixWithBooleanQuery()
	  {
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		Add("this is a test", "object", writer);
		Add("a note", "note", writer);

		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);

		// this query will be equivalent to +type:note +body:"a t*"
		BooleanQuery q = new BooleanQuery();
		q.add(new TermQuery(new Term("type", "note")), BooleanClause.Occur_e.MUST);

		MultiPhraseQuery trouble = new MultiPhraseQuery();
		trouble.add(new Term("body", "a"));
		trouble.add(new Term[] {new Term("body", "test"), new Term("body", "this")});
		q.add(trouble, BooleanClause.Occur_e.MUST);

		// exception will be thrown here without fix for #35626:
		ScoreDoc[] hits = searcher.search(q, null, 1000).scoreDocs;
		Assert.AreEqual("Wrong number of hits", 0, hits.Length);
		writer.close();
		reader.close();
		indexStore.close();
	  }

	  public virtual void TestNoDocs()
	  {
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		Add("a note", "note", writer);

		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);

		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(new Term("body", "a"));
		q.add(new Term[] {new Term("body", "nope"), new Term("body", "nope")});
		Assert.AreEqual("Wrong number of hits", 0, searcher.search(q, null, 1).totalHits);

		// just make sure no exc:
		searcher.explain(q, 0);

		writer.close();
		reader.close();
		indexStore.close();
	  }

	  public virtual void TestHashCodeAndEquals()
	  {
		MultiPhraseQuery query1 = new MultiPhraseQuery();
		MultiPhraseQuery query2 = new MultiPhraseQuery();

		Assert.AreEqual(query1.GetHashCode(), query2.GetHashCode());
		Assert.AreEqual(query1, query2);

		Term term1 = new Term("someField", "someText");

		query1.add(term1);
		query2.add(term1);

		Assert.AreEqual(query1.GetHashCode(), query2.GetHashCode());
		Assert.AreEqual(query1, query2);

		Term term2 = new Term("someField", "someMoreText");

		query1.add(term2);

		Assert.IsFalse(query1.GetHashCode() == query2.GetHashCode());
		Assert.IsFalse(query1.Equals(query2));

		query2.add(term2);

		Assert.AreEqual(query1.GetHashCode(), query2.GetHashCode());
		Assert.AreEqual(query1, query2);
	  }

	  private void Add(string s, string type, RandomIndexWriter writer)
	  {
		Document doc = new Document();
		doc.add(newTextField("body", s, Field.Store.YES));
		doc.add(newStringField("type", type, Field.Store.NO));
		writer.addDocument(doc);
	  }

	  // LUCENE-2526
	  public virtual void TestEmptyToString()
	  {
		(new MultiPhraseQuery()).ToString();
	  }

	  public virtual void TestCustomIDF()
	  {
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		Add("this is a test", "object", writer);
		Add("a note", "note", writer);

		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);
		searcher.Similarity = new DefaultSimilarityAnonymousInnerClassHelper(this);

		MultiPhraseQuery query = new MultiPhraseQuery();
		query.add(new Term[] {new Term("body", "this"), new Term("body", "that")});
		query.add(new Term("body", "is"));
		Weight weight = query.createWeight(searcher);
		Assert.AreEqual(10f * 10f, weight.ValueForNormalization, 0.001f);

		writer.close();
		reader.close();
		indexStore.close();
	  }

	  private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
	  {
		  private readonly TestMultiPhraseQuery OuterInstance;

		  public DefaultSimilarityAnonymousInnerClassHelper(TestMultiPhraseQuery outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
		  {
			return new Explanation(10f, "just a test");
		  }
	  }

	  public virtual void TestZeroPosIncr()
	  {
		Directory dir = new RAMDirectory();
		Token[] tokens = new Token[3];
		tokens[0] = new Token();
		tokens[0].append("a");
		tokens[0].PositionIncrement = 1;
		tokens[1] = new Token();
		tokens[1].append("b");
		tokens[1].PositionIncrement = 0;
		tokens[2] = new Token();
		tokens[2].append("c");
		tokens[2].PositionIncrement = 0;

		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new TextField("field", new CannedTokenStream(tokens)));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new TextField("field", new CannedTokenStream(tokens)));
		writer.addDocument(doc);
		IndexReader r = writer.Reader;
		writer.close();
		IndexSearcher s = newSearcher(r);
		MultiPhraseQuery mpq = new MultiPhraseQuery();
		//mpq.setSlop(1);

		// NOTE: not great that if we do the else clause here we
		// get different scores!  MultiPhraseQuery counts that
		// phrase as occurring twice per doc (it should be 1, I
		// think?).  this is because MultipleTermPositions is able to
		// return the same position more than once (0, in this
		// case):
		if (true)
		{
		  mpq.add(new Term[] {new Term("field", "b"), new Term("field", "c")}, 0);
		  mpq.add(new Term[] {new Term("field", "a")}, 0);
		}
		else
		{
		  mpq.add(new Term[] {new Term("field", "a")}, 0);
		  mpq.add(new Term[] {new Term("field", "b"), new Term("field", "c")}, 0);
		}
		TopDocs hits = s.search(mpq, 2);
		Assert.AreEqual(2, hits.totalHits);
		Assert.AreEqual(hits.scoreDocs[0].score, hits.scoreDocs[1].score, 1e-5);
		/*
		for(int hit=0;hit<hits.totalHits;hit++) {
		  ScoreDoc sd = hits.scoreDocs[hit];
		  System.out.println("  hit doc=" + sd.doc + " score=" + sd.score);
		}
		*/
		r.close();
		dir.close();
	  }

	  private static Token MakeToken(string text, int posIncr)
	  {
		Token t = new Token();
		t.append(text);
		t.PositionIncrement = posIncr;
		return t;
	  }

	  private static readonly Token[] INCR_0_DOC_TOKENS = new Token[] {MakeToken("x", 1), MakeToken("a", 1), MakeToken("1", 0), MakeToken("m", 1), MakeToken("b", 1), MakeToken("1", 0), MakeToken("n", 1), MakeToken("c", 1), MakeToken("y", 1)};

	  private static readonly Token[] INCR_0_QUERY_TOKENS_AND = new Token[] {MakeToken("a", 1), MakeToken("1", 0), MakeToken("b", 1), MakeToken("1", 0), MakeToken("c", 1)};

	  private static readonly Token[][] INCR_0_QUERY_TOKENS_AND_OR_MATCH = new Token[][] {new Token[] {MakeToken("a", 1)}, new Token[] {MakeToken("x", 1), MakeToken("1", 0)}, new Token[] {MakeToken("b", 2)}, new Token[] {MakeToken("x", 2), MakeToken("1", 0)}, new Token[] {MakeToken("c", 3)}};

	  private static readonly Token[][] INCR_0_QUERY_TOKENS_AND_OR_NO_MATCHN = new Token[][] {new Token[] {MakeToken("x", 1)}, new Token[] {MakeToken("a", 1), MakeToken("1", 0)}, new Token[] {MakeToken("x", 2)}, new Token[] {MakeToken("b", 2), MakeToken("1", 0)}, new Token[] {MakeToken("c", 3)}};

	  /// <summary>
	  /// using query parser, MPQ will be created, and will not be strict about having all query terms 
	  /// in each position - one of each position is sufficient (OR logic)
	  /// </summary>
	  public virtual void TestZeroPosIncrSloppyParsedAnd()
	  {
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(new Term[]{new Term("field", "a"), new Term("field", "1")}, -1);
		q.add(new Term[]{new Term("field", "b"), new Term("field", "1")}, 0);
		q.add(new Term[]{new Term("field", "c")}, 1);
		DoTestZeroPosIncrSloppy(q, 0);
		q.Slop = 1;
		DoTestZeroPosIncrSloppy(q, 0);
		q.Slop = 2;
		DoTestZeroPosIncrSloppy(q, 1);
	  }

	  private void DoTestZeroPosIncrSloppy(Query q, int nExpected)
	  {
		Directory dir = newDirectory(); // random dir
		IndexWriterConfig cfg = newIndexWriterConfig(TEST_VERSION_CURRENT, null);
		IndexWriter writer = new IndexWriter(dir, cfg);
		Document doc = new Document();
		doc.add(new TextField("field", new CannedTokenStream(INCR_0_DOC_TOKENS)));
		writer.addDocument(doc);
		IndexReader r = DirectoryReader.open(writer,false);
		writer.close();
		IndexSearcher s = newSearcher(r);

		if (VERBOSE)
		{
		  Console.WriteLine("QUERY=" + q);
		}

		TopDocs hits = s.search(q, 1);
		Assert.AreEqual("wrong number of results", nExpected, hits.totalHits);

		if (VERBOSE)
		{
		  for (int hit = 0;hit < hits.totalHits;hit++)
		  {
			ScoreDoc sd = hits.scoreDocs[hit];
			Console.WriteLine("  hit doc=" + sd.doc + " score=" + sd.score);
		  }
		}

		r.close();
		dir.close();
	  }

	  /// <summary>
	  /// PQ AND Mode - Manually creating a phrase query
	  /// </summary>
	  public virtual void TestZeroPosIncrSloppyPqAnd()
	  {
		PhraseQuery pq = new PhraseQuery();
		int pos = -1;
		foreach (Token tap in INCR_0_QUERY_TOKENS_AND)
		{
		  pos += tap.PositionIncrement;
		  pq.add(new Term("field",tap.ToString()), pos);
		}
		DoTestZeroPosIncrSloppy(pq, 0);
		pq.Slop = 1;
		DoTestZeroPosIncrSloppy(pq, 0);
		pq.Slop = 2;
		DoTestZeroPosIncrSloppy(pq, 1);
	  }

	  /// <summary>
	  /// MPQ AND Mode - Manually creating a multiple phrase query
	  /// </summary>
	  public virtual void TestZeroPosIncrSloppyMpqAnd()
	  {
		MultiPhraseQuery mpq = new MultiPhraseQuery();
		int pos = -1;
		foreach (Token tap in INCR_0_QUERY_TOKENS_AND)
		{
		  pos += tap.PositionIncrement;
		  mpq.add(new Term[]{new Term("field",tap.ToString())}, pos); //AND logic
		}
		DoTestZeroPosIncrSloppy(mpq, 0);
		mpq.Slop = 1;
		DoTestZeroPosIncrSloppy(mpq, 0);
		mpq.Slop = 2;
		DoTestZeroPosIncrSloppy(mpq, 1);
	  }

	  /// <summary>
	  /// MPQ Combined AND OR Mode - Manually creating a multiple phrase query
	  /// </summary>
	  public virtual void TestZeroPosIncrSloppyMpqAndOrMatch()
	  {
		MultiPhraseQuery mpq = new MultiPhraseQuery();
		foreach (Token tap[] in INCR_0_QUERY_TOKENS_AND_OR_MATCH)
		{
		  Term[] terms = TapTerms(tap);
		  int pos = tap[0].PositionIncrement - 1;
		  mpq.add(terms, pos); //AND logic in pos, OR across lines
		}
		DoTestZeroPosIncrSloppy(mpq, 0);
		mpq.Slop = 1;
		DoTestZeroPosIncrSloppy(mpq, 0);
		mpq.Slop = 2;
		DoTestZeroPosIncrSloppy(mpq, 1);
	  }

	  /// <summary>
	  /// MPQ Combined AND OR Mode - Manually creating a multiple phrase query - with no match
	  /// </summary>
	  public virtual void TestZeroPosIncrSloppyMpqAndOrNoMatch()
	  {
		MultiPhraseQuery mpq = new MultiPhraseQuery();
		foreach (Token tap[] in INCR_0_QUERY_TOKENS_AND_OR_NO_MATCHN)
		{
		  Term[] terms = TapTerms(tap);
		  int pos = tap[0].PositionIncrement - 1;
		  mpq.add(terms, pos); //AND logic in pos, OR across lines
		}
		DoTestZeroPosIncrSloppy(mpq, 0);
		mpq.Slop = 2;
		DoTestZeroPosIncrSloppy(mpq, 0);
	  }

	  private Term[] TapTerms(Token[] tap)
	  {
		Term[] terms = new Term[tap.Length];
		for (int i = 0; i < terms.Length; i++)
		{
		  terms[i] = new Term("field",tap[i].ToString());
		}
		return terms;
	  }

	  public virtual void TestNegativeSlop()
	  {
		MultiPhraseQuery query = new MultiPhraseQuery();
		query.add(new Term("field", "two"));
		query.add(new Term("field", "one"));
		try
		{
		  query.Slop = -2;
		  Assert.Fail("didn't get expected exception");
		}
		catch (System.ArgumentException expected)
		{
		  // expected exception
		}
	  }

	}

}