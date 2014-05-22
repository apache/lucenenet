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
	using CharsRef = Lucene.Net.Util.CharsRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;
	using Automaton = Lucene.Net.Util.Automaton.Automaton;
	using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
	using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;

	/// <summary>
	/// Create an index with random unicode terms
	/// Generates random regexps, and validates against a simple impl.
	/// </summary>
	public class TestRegexpRandom2 : LuceneTestCase
	{
	  protected internal IndexSearcher Searcher1;
	  protected internal IndexSearcher Searcher2;
	  private IndexReader Reader;
	  private Directory Dir;
	  protected internal string FieldName;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		FieldName = random().nextBoolean() ? "field" : ""; // sometimes use an empty string as field name
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.KEYWORD, false)).setMaxBufferedDocs(TestUtil.Next(random(), 50, 1000)));
		Document doc = new Document();
		Field field = newStringField(FieldName, "", Field.Store.NO);
		doc.add(field);
		IList<string> terms = new List<string>();
		int num = atLeast(200);
		for (int i = 0; i < num; i++)
		{
		  string s = TestUtil.randomUnicodeString(random());
		  field.StringValue = s;
		  terms.Add(s);
		  writer.addDocument(doc);
		}

		if (VERBOSE)
		{
		  // utf16 order
		  terms.Sort();
		  Console.WriteLine("UTF16 order:");
		  foreach (string s in terms)
		  {
			Console.WriteLine("  " + UnicodeUtil.toHexString(s));
		  }
		}

		Reader = writer.Reader;
		Searcher1 = newSearcher(Reader);
		Searcher2 = newSearcher(Reader);
		writer.close();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  /// <summary>
	  /// a stupid regexp query that just blasts thru the terms </summary>
	  private class DumbRegexpQuery : MultiTermQuery
	  {
		  private readonly TestRegexpRandom2 OuterInstance;

		internal readonly Automaton Automaton;

		internal DumbRegexpQuery(TestRegexpRandom2 outerInstance, Term term, int flags) : base(term.field())
		{
			this.OuterInstance = outerInstance;
		  RegExp re = new RegExp(term.text(), flags);
		  Automaton = re.toAutomaton();
		}

		protected internal override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
		{
		  return new SimpleAutomatonTermsEnum(this, terms.iterator(null));
		}

		private class SimpleAutomatonTermsEnum : FilteredTermsEnum
		{
			internal bool InstanceFieldsInitialized = false;

			internal virtual void InitializeInstanceFields()
			{
				RunAutomaton = new CharacterRunAutomaton(outerInstance.Automaton);
			}

			private readonly TestRegexpRandom2.DumbRegexpQuery OuterInstance;

		  internal CharacterRunAutomaton RunAutomaton;
		  internal CharsRef Utf16 = new CharsRef(10);

		  internal SimpleAutomatonTermsEnum(TestRegexpRandom2.DumbRegexpQuery outerInstance, TermsEnum tenum) : base(tenum)
		  {
			  this.OuterInstance = outerInstance;

			  if (!InstanceFieldsInitialized)
			  {
				  InitializeInstanceFields();
				  InstanceFieldsInitialized = true;
			  }
			InitialSeekTerm = new BytesRef("");
		  }

		  protected internal override AcceptStatus Accept(BytesRef term)
		  {
			UnicodeUtil.UTF8toUTF16(term.bytes, term.offset, term.length, Utf16);
			return RunAutomaton.run(Utf16.chars, 0, Utf16.length) ? AcceptStatus.YES : AcceptStatus.NO;
		  }
		}

		public override string ToString(string field)
		{
		  return field.ToString() + Automaton.ToString();
		}
	  }

	  /// <summary>
	  /// test a bunch of random regular expressions </summary>
	  public virtual void TestRegexps()
	  {
		// we generate aweful regexps: good for testing.
		// but for preflex codec, the test can be very slow, so use less iterations.
		int num = Codec.Default.Name.Equals("Lucene3x") ? 100 * RANDOM_MULTIPLIER : atLeast(1000);
		for (int i = 0; i < num; i++)
		{
		  string reg = AutomatonTestUtil.randomRegexp(random());
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: regexp=" + reg);
		  }
		  AssertSame(reg);
		}
	  }

	  /// <summary>
	  /// check that the # of hits is the same as from a very
	  /// simple regexpquery implementation.
	  /// </summary>
	  protected internal virtual void AssertSame(string regexp)
	  {
		RegexpQuery smart = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);
		DumbRegexpQuery dumb = new DumbRegexpQuery(this, new Term(FieldName, regexp), RegExp.NONE);

		TopDocs smartDocs = Searcher1.search(smart, 25);
		TopDocs dumbDocs = Searcher2.search(dumb, 25);

		CheckHits.checkEqual(smart, smartDocs.scoreDocs, dumbDocs.scoreDocs);
	  }
	}

}