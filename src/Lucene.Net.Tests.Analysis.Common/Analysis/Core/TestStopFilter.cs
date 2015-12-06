using System;
using System.Collections.Generic;
using System.Text;

namespace org.apache.lucene.analysis.core
{

	/// <summary>
	/// Copyright 2005 The Apache Software Foundation
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


	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using English = org.apache.lucene.util.English;
	using Version = org.apache.lucene.util.Version;


	public class TestStopFilter : BaseTokenStreamTestCase
	{

	  // other StopFilter functionality is already tested by TestStopAnalyzer

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExactCase() throws java.io.IOException
	  public virtual void testExactCase()
	  {
		StringReader reader = new StringReader("Now is The Time");
		CharArraySet stopWords = new CharArraySet(TEST_VERSION_CURRENT, asSet("is", "the", "Time"), false);
		TokenStream stream = new StopFilter(TEST_VERSION_CURRENT, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopWords);
		assertTokenStreamContents(stream, new string[] {"Now", "The"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopFilt() throws java.io.IOException
	  public virtual void testStopFilt()
	  {
		StringReader reader = new StringReader("Now is The Time");
		string[] stopWords = new string[] {"is", "the", "Time"};
		CharArraySet stopSet = StopFilter.makeStopSet(TEST_VERSION_CURRENT, stopWords);
		TokenStream stream = new StopFilter(TEST_VERSION_CURRENT, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet);
		assertTokenStreamContents(stream, new string[] {"Now", "The"});
	  }

	  /// <summary>
	  /// Test Position increments applied by StopFilter with and without enabling this option.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopPositons() throws java.io.IOException
	  public virtual void testStopPositons()
	  {
		StringBuilder sb = new StringBuilder();
		List<string> a = new List<string>();
		for (int i = 0; i < 20; i++)
		{
		  string w = English.intToEnglish(i).trim();
		  sb.Append(w).Append(" ");
		  if (i % 3 != 0)
		  {
			  a.Add(w);
		  }
		}
		log(sb.ToString());
		string[] stopWords = a.ToArray();
		for (int i = 0; i < a.Count; i++)
		{
			log("Stop: " + stopWords[i]);
		}
		CharArraySet stopSet = StopFilter.makeStopSet(TEST_VERSION_CURRENT, stopWords);
		// with increments
		StringReader reader = new StringReader(sb.ToString());
		StopFilter stpf = new StopFilter(Version.LUCENE_40, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet);
		doTestStopPositons(stpf,true);
		// without increments
		reader = new StringReader(sb.ToString());
		stpf = new StopFilter(Version.LUCENE_43, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet);
		doTestStopPositons(stpf,false);
		// with increments, concatenating two stop filters
		List<string> a0 = new List<string>();
		List<string> a1 = new List<string>();
		for (int i = 0; i < a.Count; i++)
		{
		  if (i % 2 == 0)
		  {
			a0.Add(a[i]);
		  }
		  else
		  {
			a1.Add(a[i]);
		  }
		}
		string[] stopWords0 = a0.ToArray();
		for (int i = 0; i < a0.Count; i++)
		{
			log("Stop0: " + stopWords0[i]);
		}
		string[] stopWords1 = a1.ToArray();
		for (int i = 0; i < a1.Count; i++)
		{
			log("Stop1: " + stopWords1[i]);
		}
		CharArraySet stopSet0 = StopFilter.makeStopSet(TEST_VERSION_CURRENT, stopWords0);
		CharArraySet stopSet1 = StopFilter.makeStopSet(TEST_VERSION_CURRENT, stopWords1);
		reader = new StringReader(sb.ToString());
		StopFilter stpf0 = new StopFilter(TEST_VERSION_CURRENT, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet0); // first part of the set
		stpf0.EnablePositionIncrements = true;
		StopFilter stpf01 = new StopFilter(TEST_VERSION_CURRENT, stpf0, stopSet1); // two stop filters concatenated!
		doTestStopPositons(stpf01,true);
	  }

	  // LUCENE-3849: make sure after .end() we see the "ending" posInc
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEndStopword() throws Exception
	  public virtual void testEndStopword()
	  {
		CharArraySet stopSet = StopFilter.makeStopSet(TEST_VERSION_CURRENT, "of");
		StopFilter stpf = new StopFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("test of"), MockTokenizer.WHITESPACE, false), stopSet);
		assertTokenStreamContents(stpf, new string[] {"test"}, new int[] {0}, new int[] {4}, null, new int[] {1}, null, 7, 1, null, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void doTestStopPositons(StopFilter stpf, boolean enableIcrements) throws java.io.IOException
	  private void doTestStopPositons(StopFilter stpf, bool enableIcrements)
	  {
		log("---> test with enable-increments-" + (enableIcrements?"enabled":"disabled"));
		stpf.EnablePositionIncrements = enableIcrements;
		CharTermAttribute termAtt = stpf.getAttribute(typeof(CharTermAttribute));
		PositionIncrementAttribute posIncrAtt = stpf.getAttribute(typeof(PositionIncrementAttribute));
		stpf.reset();
		for (int i = 0; i < 20; i += 3)
		{
		  assertTrue(stpf.incrementToken());
		  log("Token " + i + ": " + stpf);
		  string w = English.intToEnglish(i).trim();
		  assertEquals("expecting token " + i + " to be " + w,w,termAtt.ToString());
		  assertEquals("all but first token must have position increment of 3",enableIcrements?(i == 0?1:3):1,posIncrAtt.PositionIncrement);
		}
		assertFalse(stpf.incrementToken());
		stpf.end();
		stpf.close();
	  }

	  // print debug info depending on VERBOSE
	  private static void log(string s)
	  {
		if (VERBOSE)
		{
		  Console.WriteLine(s);
		}
	  }

	  // stupid filter that inserts synonym of 'hte' for 'the'
	  private class MockSynonymFilter : TokenFilter
	  {
		  private readonly TestStopFilter outerInstance;

		internal State bufferedState;
		internal CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		internal PositionIncrementAttribute posIncAtt = addAttribute(typeof(PositionIncrementAttribute));

		internal MockSynonymFilter(TestStopFilter outerInstance, TokenStream input) : base(input)
		{
			this.outerInstance = outerInstance;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (bufferedState != null)
		  {
			restoreState(bufferedState);
			posIncAtt.PositionIncrement = 0;
			termAtt.setEmpty().append("hte");
			bufferedState = null;
			return true;
		  }
		  else if (input.incrementToken())
		  {
			if (termAtt.ToString().Equals("the"))
			{
			  bufferedState = captureState();
			}
			return true;
		  }
		  else
		  {
			return false;
		  }
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
		public override void reset()
		{
		  base.reset();
		  bufferedState = null;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFirstPosInc() throws Exception
	  public virtual void testFirstPosInc()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);

		assertAnalyzesTo(analyzer, "the quick brown fox", new string[] {"hte", "quick", "brown", "fox"}, new int[] {1, 1, 1, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestStopFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestStopFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenFilter filter = new MockSynonymFilter(outerInstance, tokenizer);
			StopFilter stopfilter = new StopFilter(Version.LUCENE_43, filter, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
			stopfilter.EnablePositionIncrements = false;
			return new TokenStreamComponents(tokenizer, stopfilter);
		  }
	  }
	}

}