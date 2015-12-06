using System;

namespace org.apache.lucene.analysis.core
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


	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using TestUtil = org.apache.lucene.util.TestUtil;
	using Automaton = org.apache.lucene.util.automaton.Automaton;
	using BasicOperations = org.apache.lucene.util.automaton.BasicOperations;
	using CharacterRunAutomaton = org.apache.lucene.util.automaton.CharacterRunAutomaton;
	using State = org.apache.lucene.util.automaton.State;
	using Transition = org.apache.lucene.util.automaton.Transition;

	/// <summary>
	/// Compares MockTokenizer (which is simple with no optimizations) with equivalent 
	/// core tokenizers (that have optimizations like buffering).
	/// 
	/// Any tests here need to probably consider unicode version of the JRE (it could
	/// cause false fails).
	/// </summary>
	public class TestDuelingAnalyzers : LuceneTestCase
	{
	  private CharacterRunAutomaton jvmLetter;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		// build an automaton matching this jvm's letter definition
		State initial = new State();
		State accept = new State();
		accept.Accept = true;
		for (int i = 0; i <= 0x10FFFF; i++)
		{
		  if (char.IsLetter(i))
		  {
			initial.addTransition(new Transition(i, i, accept));
		  }
		}
		Automaton single = new Automaton(initial);
		single.reduce();
		Automaton repeat = BasicOperations.repeat(single);
		jvmLetter = new CharacterRunAutomaton(repeat);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLetterAscii() throws Exception
	  public virtual void testLetterAscii()
	  {
		Random random = random();
		Analyzer left = new MockAnalyzer(random, jvmLetter, false);
		Analyzer right = new AnalyzerAnonymousInnerClassHelper(this);
		for (int i = 0; i < 1000; i++)
		{
		  string s = TestUtil.randomSimpleString(random);
		  assertEquals(s, left.tokenStream("foo", newStringReader(s)), right.tokenStream("foo", newStringReader(s)));
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestDuelingAnalyzers outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestDuelingAnalyzers outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

	  // not so useful since its all one token?!
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLetterAsciiHuge() throws Exception
	  public virtual void testLetterAsciiHuge()
	  {
		Random random = random();
		int maxLength = 8192; // CharTokenizer.IO_BUFFER_SIZE*2
		MockAnalyzer left = new MockAnalyzer(random, jvmLetter, false);
		left.MaxTokenLength = 255; // match CharTokenizer's max token length
		Analyzer right = new AnalyzerAnonymousInnerClassHelper2(this);
		int numIterations = atLeast(50);
		for (int i = 0; i < numIterations; i++)
		{
		  string s = TestUtil.randomSimpleString(random, maxLength);
		  assertEquals(s, left.tokenStream("foo", newStringReader(s)), right.tokenStream("foo", newStringReader(s)));
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestDuelingAnalyzers outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestDuelingAnalyzers outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLetterHtmlish() throws Exception
	  public virtual void testLetterHtmlish()
	  {
		Random random = random();
		Analyzer left = new MockAnalyzer(random, jvmLetter, false);
		Analyzer right = new AnalyzerAnonymousInnerClassHelper3(this);
		for (int i = 0; i < 1000; i++)
		{
		  string s = TestUtil.randomHtmlishString(random, 20);
		  assertEquals(s, left.tokenStream("foo", newStringReader(s)), right.tokenStream("foo", newStringReader(s)));
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestDuelingAnalyzers outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(TestDuelingAnalyzers outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLetterHtmlishHuge() throws Exception
	  public virtual void testLetterHtmlishHuge()
	  {
		Random random = random();
		int maxLength = 1024; // this is number of elements, not chars!
		MockAnalyzer left = new MockAnalyzer(random, jvmLetter, false);
		left.MaxTokenLength = 255; // match CharTokenizer's max token length
		Analyzer right = new AnalyzerAnonymousInnerClassHelper4(this);
		int numIterations = atLeast(50);
		for (int i = 0; i < numIterations; i++)
		{
		  string s = TestUtil.randomHtmlishString(random, maxLength);
		  assertEquals(s, left.tokenStream("foo", newStringReader(s)), right.tokenStream("foo", newStringReader(s)));
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper4 : Analyzer
	  {
		  private readonly TestDuelingAnalyzers outerInstance;

		  public AnalyzerAnonymousInnerClassHelper4(TestDuelingAnalyzers outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLetterUnicode() throws Exception
	  public virtual void testLetterUnicode()
	  {
		Random random = random();
		Analyzer left = new MockAnalyzer(random(), jvmLetter, false);
		Analyzer right = new AnalyzerAnonymousInnerClassHelper5(this);
		for (int i = 0; i < 1000; i++)
		{
		  string s = TestUtil.randomUnicodeString(random);
		  assertEquals(s, left.tokenStream("foo", newStringReader(s)), right.tokenStream("foo", newStringReader(s)));
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper5 : Analyzer
	  {
		  private readonly TestDuelingAnalyzers outerInstance;

		  public AnalyzerAnonymousInnerClassHelper5(TestDuelingAnalyzers outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLetterUnicodeHuge() throws Exception
	  public virtual void testLetterUnicodeHuge()
	  {
		Random random = random();
		int maxLength = 4300; // CharTokenizer.IO_BUFFER_SIZE + fudge
		MockAnalyzer left = new MockAnalyzer(random, jvmLetter, false);
		left.MaxTokenLength = 255; // match CharTokenizer's max token length
		Analyzer right = new AnalyzerAnonymousInnerClassHelper6(this);
		int numIterations = atLeast(50);
		for (int i = 0; i < numIterations; i++)
		{
		  string s = TestUtil.randomUnicodeString(random, maxLength);
		  assertEquals(s, left.tokenStream("foo", newStringReader(s)), right.tokenStream("foo", newStringReader(s)));
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper6 : Analyzer
	  {
		  private readonly TestDuelingAnalyzers outerInstance;

		  public AnalyzerAnonymousInnerClassHelper6(TestDuelingAnalyzers outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

	  // we only check a few core attributes here.
	  // TODO: test other things
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void assertEquals(String s, org.apache.lucene.analysis.TokenStream left, org.apache.lucene.analysis.TokenStream right) throws Exception
	  public virtual void assertEquals(string s, TokenStream left, TokenStream right)
	  {
		left.reset();
		right.reset();
		CharTermAttribute leftTerm = left.addAttribute(typeof(CharTermAttribute));
		CharTermAttribute rightTerm = right.addAttribute(typeof(CharTermAttribute));
		OffsetAttribute leftOffset = left.addAttribute(typeof(OffsetAttribute));
		OffsetAttribute rightOffset = right.addAttribute(typeof(OffsetAttribute));
		PositionIncrementAttribute leftPos = left.addAttribute(typeof(PositionIncrementAttribute));
		PositionIncrementAttribute rightPos = right.addAttribute(typeof(PositionIncrementAttribute));

		while (left.incrementToken())
		{
		  assertTrue("wrong number of tokens for input: " + s, right.incrementToken());
		  assertEquals("wrong term text for input: " + s, leftTerm.ToString(), rightTerm.ToString());
		  assertEquals("wrong position for input: " + s, leftPos.PositionIncrement, rightPos.PositionIncrement);
		  assertEquals("wrong start offset for input: " + s, leftOffset.startOffset(), rightOffset.startOffset());
		  assertEquals("wrong end offset for input: " + s, leftOffset.endOffset(), rightOffset.endOffset());
		};
		assertFalse("wrong number of tokens for input: " + s, right.incrementToken());
		left.end();
		right.end();
		assertEquals("wrong final offset for input: " + s, leftOffset.endOffset(), rightOffset.endOffset());
		left.close();
		right.close();
	  }

	  // TODO: maybe push this out to TestUtil or LuceneTestCase and always use it instead?
	  private static Reader newStringReader(string s)
	  {
		Random random = random();
		Reader r = new StringReader(s);
		if (random.nextBoolean())
		{
		  r = new MockReaderWrapper(random, r);
		}
		return r;
	  }
	}

}