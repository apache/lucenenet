using System.Collections.Generic;

namespace Lucene.Net.Index
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
	using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
	using AutomatonQuery = Lucene.Net.Search.AutomatonQuery;
	using CheckHits = Lucene.Net.Search.CheckHits;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Lucene.Net.Util.Automaton;

	public class TestTermsEnum2 : LuceneTestCase
	{
	  private Directory Dir;
	  private IndexReader Reader;
	  private IndexSearcher Searcher;
	  private SortedSet<BytesRef> Terms; // the terms we put in the index
	  private Automaton TermsAutomaton; // automata of the same
	  internal int NumIterations;

	  public override void SetUp()
	  {
		base.setUp();
		// we generate aweful regexps: good for testing.
		// but for preflex codec, the test can be very slow, so use less iterations.
		NumIterations = Codec.Default.Name.Equals("Lucene3x") ? 10 * RANDOM_MULTIPLIER : atLeast(50);
		Dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.KEYWORD, false)).setMaxBufferedDocs(TestUtil.Next(random(), 50, 1000)));
		Document doc = new Document();
		Field field = newStringField("field", "", Field.Store.YES);
		doc.add(field);
		Terms = new SortedSet<>();

		int num = atLeast(200);
		for (int i = 0; i < num; i++)
		{
		  string s = TestUtil.randomUnicodeString(random());
		  field.StringValue = s;
		  Terms.add(new BytesRef(s));
		  writer.addDocument(doc);
		}

		TermsAutomaton = BasicAutomata.makeStringUnion(Terms);

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
	  /// tests a pre-intersected automaton against the original </summary>
	  public virtual void TestFiniteVersusInfinite()
	  {
		for (int i = 0; i < NumIterations; i++)
		{
		  string reg = AutomatonTestUtil.randomRegexp(random());
		  Automaton automaton = (new RegExp(reg, RegExp.NONE)).toAutomaton();
		  IList<BytesRef> matchedTerms = new List<BytesRef>();
		  foreach (BytesRef t in Terms)
		  {
			if (BasicOperations.run(automaton, t.utf8ToString()))
			{
			  matchedTerms.Add(t);
			}
		  }

		  Automaton alternate = BasicAutomata.makeStringUnion(matchedTerms);
		  //System.out.println("match " + matchedTerms.size() + " " + alternate.getNumberOfStates() + " states, sigma=" + alternate.getStartPoints().length);
		  //AutomatonTestUtil.minimizeSimple(alternate);
		  //System.out.println("minmize done");
		  AutomatonQuery a1 = new AutomatonQuery(new Term("field", ""), automaton);
		  AutomatonQuery a2 = new AutomatonQuery(new Term("field", ""), alternate);
		  CheckHits.checkEqual(a1, Searcher.search(a1, 25).scoreDocs, Searcher.search(a2, 25).scoreDocs);
		}
	  }

	  /// <summary>
	  /// seeks to every term accepted by some automata </summary>
	  public virtual void TestSeeking()
	  {
		for (int i = 0; i < NumIterations; i++)
		{
		  string reg = AutomatonTestUtil.randomRegexp(random());
		  Automaton automaton = (new RegExp(reg, RegExp.NONE)).toAutomaton();
		  TermsEnum te = MultiFields.getTerms(Reader, "field").iterator(null);
		  List<BytesRef> unsortedTerms = new List<BytesRef>(Terms);
		  Collections.shuffle(unsortedTerms, random());

		  foreach (BytesRef term in unsortedTerms)
		  {
			if (BasicOperations.run(automaton, term.utf8ToString()))
			{
			  // term is accepted
			  if (random().nextBoolean())
			  {
				// seek exact
				Assert.IsTrue(te.seekExact(term));
			  }
			  else
			  {
				// seek ceil
				Assert.AreEqual(SeekStatus.FOUND, te.seekCeil(term));
				Assert.AreEqual(term, te.term());
			  }
			}
		  }
		}
	  }

	  /// <summary>
	  /// mixes up seek and next for all terms </summary>
	  public virtual void TestSeekingAndNexting()
	  {
		for (int i = 0; i < NumIterations; i++)
		{
		  TermsEnum te = MultiFields.getTerms(Reader, "field").iterator(null);

		  foreach (BytesRef term in Terms)
		  {
			int c = random().Next(3);
			if (c == 0)
			{
			  Assert.AreEqual(term, te.next());
			}
			else if (c == 1)
			{
			  Assert.AreEqual(SeekStatus.FOUND, te.seekCeil(term));
			  Assert.AreEqual(term, te.term());
			}
			else
			{
			  Assert.IsTrue(te.seekExact(term));
			}
		  }
		}
	  }

	  /// <summary>
	  /// tests intersect: TODO start at a random term! </summary>
	  public virtual void TestIntersect()
	  {
		for (int i = 0; i < NumIterations; i++)
		{
		  string reg = AutomatonTestUtil.randomRegexp(random());
		  Automaton automaton = (new RegExp(reg, RegExp.NONE)).toAutomaton();
		  CompiledAutomaton ca = new CompiledAutomaton(automaton, SpecialOperations.isFinite(automaton), false);
		  TermsEnum te = MultiFields.getTerms(Reader, "field").intersect(ca, null);
		  Automaton expected = BasicOperations.intersection(TermsAutomaton, automaton);
		  SortedSet<BytesRef> found = new SortedSet<BytesRef>();
		  while (te.next() != null)
		  {
			found.Add(BytesRef.deepCopyOf(te.term()));
		  }

		  Automaton actual = BasicAutomata.makeStringUnion(found);
		  Assert.IsTrue(BasicOperations.sameLanguage(expected, actual));
		}
	  }
	}

}