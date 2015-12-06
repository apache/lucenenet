using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

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

namespace org.apache.lucene.analysis.synonym
{


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using org.apache.lucene.analysis.tokenattributes;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using TestUtil = org.apache.lucene.util.TestUtil;

	public class TestSynonymMapFilter : BaseTokenStreamTestCase
	{

	  private SynonymMap.Builder b;
	  private Tokenizer tokensIn;
	  private SynonymFilter tokensOut;
	  private CharTermAttribute termAtt;
	  private PositionIncrementAttribute posIncrAtt;
	  private PositionLengthAttribute posLenAtt;
	  private OffsetAttribute offsetAtt;

	  private void add(string input, string output, bool keepOrig)
	  {
		if (VERBOSE)
		{
		  Console.WriteLine("  add input=" + input + " output=" + output + " keepOrig=" + keepOrig);
		}
		CharsRef inputCharsRef = new CharsRef();
		SynonymMap.Builder.join(input.Split(" +", true), inputCharsRef);

		CharsRef outputCharsRef = new CharsRef();
		SynonymMap.Builder.join(output.Split(" +", true), outputCharsRef);

		b.add(inputCharsRef, outputCharsRef, keepOrig);
	  }

	  private void assertEquals(CharTermAttribute term, string expected)
	  {
		assertEquals(expected.Length, term.length());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] buffer = term.buffer();
		char[] buffer = term.buffer();
		for (int chIDX = 0;chIDX < expected.Length;chIDX++)
		{
		  assertEquals(expected[chIDX], buffer[chIDX]);
		}
	  }

	  // For the output string: separate positions with a space,
	  // and separate multiple tokens at each position with a
	  // /.  If a token should have end offset != the input
	  // token's end offset then add :X to it:

	  // TODO: we should probably refactor this guy to use/take analyzer,
	  // the tests are a little messy
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void verify(String input, String output) throws Exception
	  private void verify(string input, string output)
	  {
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: verify input=" + input + " expectedOutput=" + output);
		}

		tokensIn.Reader = new StringReader(input);
		tokensOut.reset();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String[] expected = output.split(" ");
		string[] expected = output.Split(" ", true);
		int expectedUpto = 0;
		while (tokensOut.incrementToken())
		{

		  if (VERBOSE)
		  {
			Console.WriteLine("  incr token=" + termAtt.ToString() + " posIncr=" + posIncrAtt.PositionIncrement + " startOff=" + offsetAtt.startOffset() + " endOff=" + offsetAtt.endOffset());
		  }

		  assertTrue(expectedUpto < expected.Length);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startOffset = offsetAtt.startOffset();
		  int startOffset = offsetAtt.startOffset();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endOffset = offsetAtt.endOffset();
		  int endOffset = offsetAtt.endOffset();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String[] expectedAtPos = expected[expectedUpto++].split("/");
		  string[] expectedAtPos = expected[expectedUpto++].Split("/", true);
		  for (int atPos = 0;atPos < expectedAtPos.Length;atPos++)
		  {
			if (atPos > 0)
			{
			  assertTrue(tokensOut.incrementToken());
			  if (VERBOSE)
			  {
				Console.WriteLine("  incr token=" + termAtt.ToString() + " posIncr=" + posIncrAtt.PositionIncrement + " startOff=" + offsetAtt.startOffset() + " endOff=" + offsetAtt.endOffset());
			  }
			}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int colonIndex = expectedAtPos[atPos].indexOf(':');
			int colonIndex = expectedAtPos[atPos].IndexOf(':');
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int underbarIndex = expectedAtPos[atPos].indexOf('_');
			int underbarIndex = expectedAtPos[atPos].IndexOf('_');
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String expectedToken;
			string expectedToken;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int expectedEndOffset;
			int expectedEndOffset;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int expectedPosLen;
			int expectedPosLen;
			if (colonIndex != -1)
			{
			  expectedToken = expectedAtPos[atPos].Substring(0, colonIndex);
			  if (underbarIndex != -1)
			  {
				expectedEndOffset = int.Parse(expectedAtPos[atPos].Substring(1 + colonIndex, underbarIndex - (1 + colonIndex)));
				expectedPosLen = int.Parse(expectedAtPos[atPos].Substring(1 + underbarIndex));
			  }
			  else
			  {
				expectedEndOffset = int.Parse(expectedAtPos[atPos].Substring(1 + colonIndex));
				expectedPosLen = 1;
			  }
			}
			else
			{
			  expectedToken = expectedAtPos[atPos];
			  expectedEndOffset = endOffset;
			  expectedPosLen = 1;
			}
			assertEquals(expectedToken, termAtt.ToString());
			assertEquals(atPos == 0 ? 1 : 0, posIncrAtt.PositionIncrement);
			// start/end offset of all tokens at same pos should
			// be the same:
			assertEquals(startOffset, offsetAtt.startOffset());
			assertEquals(expectedEndOffset, offsetAtt.endOffset());
			assertEquals(expectedPosLen, posLenAtt.PositionLength);
		  }
		}
		tokensOut.end();
		tokensOut.close();
		if (VERBOSE)
		{
		  Console.WriteLine("  incr: END");
		}
		assertEquals(expectedUpto, expected.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDontKeepOrig() throws Exception
	  public virtual void testDontKeepOrig()
	  {
		b = new SynonymMap.Builder(true);
		add("a b", "foo", false);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Analyzer analyzer = new org.apache.lucene.analysis.Analyzer()
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, map);

		assertAnalyzesTo(analyzer, "a b c", new string[] {"foo", "c"}, new int[] {0, 4}, new int[] {3, 5}, null, new int[] {1, 1}, new int[] {1, 1}, true);
		checkAnalysisConsistency(random(), analyzer, false, "a b c");
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, false));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDoKeepOrig() throws Exception
	  public virtual void testDoKeepOrig()
	  {
		b = new SynonymMap.Builder(true);
		add("a b", "foo", true);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Analyzer analyzer = new org.apache.lucene.analysis.Analyzer()
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this, map);

		assertAnalyzesTo(analyzer, "a b c", new string[] {"a", "foo", "b", "c"}, new int[] {0, 0, 2, 4}, new int[] {1, 3, 3, 5}, null, new int[] {1, 0, 1, 1}, new int[] {1, 2, 1, 1}, true);
		checkAnalysisConsistency(random(), analyzer, false, "a b c");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper2(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, false));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasic() throws Exception
	  public virtual void testBasic()
	  {
		b = new SynonymMap.Builder(true);
		add("a", "foo", true);
		add("a b", "bar fee", true);
		add("b c", "dog collar", true);
		add("c d", "dog harness holder extras", true);
		add("m c e", "dog barks loudly", false);
		add("i j k", "feep", true);

		add("e f", "foo bar", false);
		add("e f", "baz bee", false);

		add("z", "boo", false);
		add("y", "bee", true);

		tokensIn = new MockTokenizer(new StringReader("a"), MockTokenizer.WHITESPACE, true);
		tokensIn.reset();
		assertTrue(tokensIn.incrementToken());
		assertFalse(tokensIn.incrementToken());
		tokensIn.end();
		tokensIn.close();

		tokensOut = new SynonymFilter(tokensIn, b.build(), true);
		termAtt = tokensOut.addAttribute(typeof(CharTermAttribute));
		posIncrAtt = tokensOut.addAttribute(typeof(PositionIncrementAttribute));
		posLenAtt = tokensOut.addAttribute(typeof(PositionLengthAttribute));
		offsetAtt = tokensOut.addAttribute(typeof(OffsetAttribute));

		verify("a b c", "a/bar b/fee c");

		// syn output extends beyond input tokens
		verify("x a b c d", "x a/bar b/fee c/dog d/harness holder extras");

		verify("a b a", "a/bar b/fee a/foo");

		// outputs that add to one another:
		verify("c d c d", "c/dog d/harness c/holder/dog d/extras/harness holder extras");

		// two outputs for same input
		verify("e f", "foo/baz bar/bee");

		// verify multi-word / single-output offsets:
		verify("g i j k g", "g i/feep:7_3 j k g");

		// mixed keepOrig true/false:
		verify("a m c e x", "a/foo dog barks loudly x");
		verify("c d m c e x", "c/dog d/harness holder/dog extras/barks loudly x");
		assertTrue(tokensOut.CaptureCount > 0);

		// no captureStates when no syns matched
		verify("p q r s t", "p q r s t");
		assertEquals(0, tokensOut.CaptureCount);

		// no captureStates when only single-input syns, w/ no
		// lookahead needed, matched
		verify("p q z y t", "p q boo y/bee t");
		assertEquals(0, tokensOut.CaptureCount);
	  }

	  private string getRandomString(char start, int alphabetSize, int length)
	  {
		Debug.Assert(alphabetSize <= 26);
		char[] s = new char[2 * length];
		for (int charIDX = 0;charIDX < length;charIDX++)
		{
		  s[2 * charIDX] = (char)(start + random().Next(alphabetSize));
		  s[2 * charIDX + 1] = ' ';
		}
		return new string(s);
	  }

	  private class OneSyn
	  {
		internal string @in;
		internal IList<string> @out;
		internal bool keepOrig;
	  }

	  public virtual string slowSynMatcher(string doc, IList<OneSyn> syns, int maxOutputLength)
	  {
		assertTrue(doc.Length % 2 == 0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numInputs = doc.length()/2;
		int numInputs = doc.Length / 2;
		bool[] keepOrigs = new bool[numInputs];
		bool[] hasMatch = new bool[numInputs];
		Arrays.fill(keepOrigs, false);
		string[] outputs = new string[numInputs + maxOutputLength];
		OneSyn[] matches = new OneSyn[numInputs];
		foreach (OneSyn syn in syns)
		{
		  int idx = -1;
		  while (true)
		  {
			idx = doc.IndexOf(syn.@in, 1 + idx, StringComparison.Ordinal);
			if (idx == -1)
			{
			  break;
			}
			assertTrue(idx % 2 == 0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int matchIDX = idx/2;
			int matchIDX = idx / 2;
			assertTrue(syn.@in.Length % 2 == 1);
			if (matches[matchIDX] == null)
			{
			  matches[matchIDX] = syn;
			}
			else if (syn.@in.Length > matches[matchIDX].@in.Length)
			{
			  // Greedy conflict resolution: longer match wins:
			  matches[matchIDX] = syn;
			}
			else
			{
			  assertTrue(syn.@in.Length < matches[matchIDX].@in.Length);
			}
		  }
		}

		// Greedy conflict resolution: if syn matches a range of inputs,
		// it prevents other syns from matching that range
		for (int inputIDX = 0;inputIDX < numInputs;inputIDX++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final OneSyn match = matches[inputIDX];
		  OneSyn match = matches[inputIDX];
		  if (match != null)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int synInLength = (1+match.in.length())/2;
			int synInLength = (1 + match.@in.Length) / 2;
			for (int nextInputIDX = inputIDX + 1;nextInputIDX < numInputs && nextInputIDX < (inputIDX + synInLength);nextInputIDX++)
			{
			  matches[nextInputIDX] = null;
			}
		  }
		}

		// Fill overlapping outputs:
		for (int inputIDX = 0;inputIDX < numInputs;inputIDX++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final OneSyn syn = matches[inputIDX];
		  OneSyn syn = matches[inputIDX];
		  if (syn == null)
		  {
			continue;
		  }
		  for (int idx = 0;idx < (1 + syn.@in.Length) / 2;idx++)
		  {
			hasMatch[inputIDX + idx] = true;
			keepOrigs[inputIDX + idx] |= syn.keepOrig;
		  }
		  foreach (string synOut in syn.@out)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String[] synOutputs = synOut.split(" ");
			string[] synOutputs = synOut.Split(" ", true);
			assertEquals(synOutputs.Length, (1 + synOut.Length) / 2);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int matchEnd = inputIDX + synOutputs.length;
			int matchEnd = inputIDX + synOutputs.Length;
			int synUpto = 0;
			for (int matchIDX = inputIDX;matchIDX < matchEnd;matchIDX++)
			{
			  if (outputs[matchIDX] == null)
			  {
				outputs[matchIDX] = synOutputs[synUpto++];
			  }
			  else
			  {
				outputs[matchIDX] = outputs[matchIDX] + "/" + synOutputs[synUpto++];
			  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endOffset;
			  int endOffset;
			  if (matchIDX < numInputs)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int posLen;
				int posLen;
				if (synOutputs.Length == 1)
				{
				  // Add full endOffset
				  endOffset = (inputIDX * 2) + syn.@in.Length;
				  posLen = syn.keepOrig ? (1 + syn.@in.Length) / 2 : 1;
				}
				else
				{
				  // Add endOffset matching input token's
				  endOffset = (matchIDX * 2) + 1;
				  posLen = 1;
				}
				outputs[matchIDX] = outputs[matchIDX] + ":" + endOffset + "_" + posLen;
			  }
			}
		  }
		}

		StringBuilder sb = new StringBuilder();
		string[] inputTokens = doc.Split(" ", true);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int limit = inputTokens.length + maxOutputLength;
		int limit = inputTokens.Length + maxOutputLength;
		for (int inputIDX = 0;inputIDX < limit;inputIDX++)
		{
		  bool posHasOutput = false;
		  if (inputIDX >= numInputs && outputs[inputIDX] == null)
		  {
			break;
		  }
		  if (inputIDX < numInputs && (!hasMatch[inputIDX] || keepOrigs[inputIDX]))
		  {
			assertTrue(inputTokens[inputIDX].Length != 0);
			sb.Append(inputTokens[inputIDX]);
			posHasOutput = true;
		  }

		  if (outputs[inputIDX] != null)
		  {
			if (posHasOutput)
			{
			  sb.Append('/');
			}
			sb.Append(outputs[inputIDX]);
		  }
		  else if (!posHasOutput)
		  {
			continue;
		  }
		  if (inputIDX < limit - 1)
		  {
			sb.Append(' ');
		  }
		}

		return sb.ToString();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandom() throws Exception
	  public virtual void testRandom()
	  {

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int alphabetSize = org.apache.lucene.util.TestUtil.nextInt(random(), 2, 7);
		int alphabetSize = TestUtil.Next(random(), 2, 7);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int docLen = atLeast(3000);
		int docLen = atLeast(3000);
		//final int docLen = 50;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String document = getRandomString('a', alphabetSize, docLen);
		string document = getRandomString('a', alphabetSize, docLen);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: doc=" + document);
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numSyn = atLeast(5);
		int numSyn = atLeast(5);
		//final int numSyn = 2;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String,OneSyn> synMap = new java.util.HashMap<>();
		IDictionary<string, OneSyn> synMap = new Dictionary<string, OneSyn>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<OneSyn> syns = new java.util.ArrayList<>();
		IList<OneSyn> syns = new List<OneSyn>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean dedup = random().nextBoolean();
		bool dedup = random().nextBoolean();
		if (VERBOSE)
		{
		  Console.WriteLine("  dedup=" + dedup);
		}
		b = new SynonymMap.Builder(dedup);
		for (int synIDX = 0;synIDX < numSyn;synIDX++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String synIn = getRandomString('a', alphabetSize, org.apache.lucene.util.TestUtil.nextInt(random(), 1, 5)).trim();
		  string synIn = getRandomString('a', alphabetSize, TestUtil.Next(random(), 1, 5)).Trim();
		  OneSyn s = synMap[synIn];
		  if (s == null)
		  {
			s = new OneSyn();
			s.@in = synIn;
			syns.Add(s);
			s.@out = new List<>();
			synMap[synIn] = s;
			s.keepOrig = random().nextBoolean();
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String synOut = getRandomString('0', 10, org.apache.lucene.util.TestUtil.nextInt(random(), 1, 5)).trim();
		  string synOut = getRandomString('0', 10, TestUtil.Next(random(), 1, 5)).Trim();
		  s.@out.Add(synOut);
		  add(synIn, synOut, s.keepOrig);
		  if (VERBOSE)
		  {
			Console.WriteLine("  syns[" + synIDX + "] = " + s.@in + " -> " + s.@out + " keepOrig=" + s.keepOrig);
		  }
		}

		tokensIn = new MockTokenizer(new StringReader("a"), MockTokenizer.WHITESPACE, true);
		tokensIn.reset();
		assertTrue(tokensIn.incrementToken());
		assertFalse(tokensIn.incrementToken());
		tokensIn.end();
		tokensIn.close();

		tokensOut = new SynonymFilter(tokensIn, b.build(), true);
		termAtt = tokensOut.addAttribute(typeof(CharTermAttribute));
		posIncrAtt = tokensOut.addAttribute(typeof(PositionIncrementAttribute));
		posLenAtt = tokensOut.addAttribute(typeof(PositionLengthAttribute));
		offsetAtt = tokensOut.addAttribute(typeof(OffsetAttribute));

		if (dedup)
		{
		  pruneDups(syns);
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String expected = slowSynMatcher(document, syns, 5);
		string expected = slowSynMatcher(document, syns, 5);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: expected=" + expected);
		}

		verify(document, expected);
	  }

	  private void pruneDups(IList<OneSyn> syns)
	  {
		ISet<string> seen = new HashSet<string>();
		foreach (OneSyn syn in syns)
		{
		  int idx = 0;
		  while (idx < syn.@out.Count)
		  {
			string @out = syn.@out[idx];
			if (!seen.Contains(@out))
			{
			  seen.Add(@out);
			  idx++;
			}
			else
			{
			  syn.@out.RemoveAt(idx);
			}
		  }
		  seen.Clear();
		}
	  }

	  private string randomNonEmptyString()
	  {
		while (true)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s = org.apache.lucene.util.TestUtil.randomUnicodeString(random()).trim();
		  string s = TestUtil.randomUnicodeString(random()).trim();
		  if (s.Length != 0 && s.IndexOf('\u0000') == -1)
		  {
			return s;
		  }
		}
	  }

	  /// <summary>
	  /// simple random test, doesn't verify correctness.
	  ///  does verify it doesnt throw exceptions, or that the stream doesn't misbehave
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandom2() throws Exception
	  public virtual void testRandom2()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numIters = atLeast(3);
		int numIters = atLeast(3);
		for (int i = 0; i < numIters; i++)
		{
		  b = new SynonymMap.Builder(random().nextBoolean());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numEntries = atLeast(10);
		  int numEntries = atLeast(10);
		  for (int j = 0; j < numEntries; j++)
		  {
			add(randomNonEmptyString(), randomNonEmptyString(), random().nextBoolean());
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		  SynonymMap map = b.build();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean ignoreCase = random().nextBoolean();
		  bool ignoreCase = random().nextBoolean();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Analyzer analyzer = new org.apache.lucene.analysis.Analyzer()
		  Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, map, ignoreCase);

		  checkRandomData(random(), analyzer, 100);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;
		  private bool ignoreCase;

		  public AnalyzerAnonymousInnerClassHelper(TestSynonymMapFilter outerInstance, SynonymMap map, bool ignoreCase)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
			  this.ignoreCase = ignoreCase;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, ignoreCase));
		  }
	  }

	  // NOTE: this is an invalid test... SynFilter today can't
	  // properly consume a graph... we can re-enable this once
	  // we fix that...
	  /*
	  // Adds MockGraphTokenFilter before SynFilter:
	  public void testRandom2GraphBefore() throws Exception {
	    final int numIters = atLeast(10);
	    Random random = random();
	    for (int i = 0; i < numIters; i++) {
	      b = new SynonymMap.Builder(random.nextBoolean());
	      final int numEntries = atLeast(10);
	      for (int j = 0; j < numEntries; j++) {
	        add(randomNonEmptyString(), randomNonEmptyString(), random.nextBoolean());
	      }
	      final SynonymMap map = b.build();
	      final boolean ignoreCase = random.nextBoolean();
	      
	      final Analyzer analyzer = new Analyzer() {
	        @Override
	        protected TokenStreamComponents createComponents(String fieldName, Reader reader) {
	          Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
	          TokenStream graph = new MockGraphTokenFilter(random(), tokenizer);
	          return new TokenStreamComponents(tokenizer, new SynonymFilter(graph, map, ignoreCase));
	        }
	      };
	
	      checkRandomData(random, analyzer, 1000*RANDOM_MULTIPLIER);
	    }
	  }
	  */

	  // Adds MockGraphTokenFilter after SynFilter:
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandom2GraphAfter() throws Exception
	  public virtual void testRandom2GraphAfter()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numIters = atLeast(3);
		int numIters = atLeast(3);
		Random random = random();
		for (int i = 0; i < numIters; i++)
		{
		  b = new SynonymMap.Builder(random.nextBoolean());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numEntries = atLeast(10);
		  int numEntries = atLeast(10);
		  for (int j = 0; j < numEntries; j++)
		  {
			add(randomNonEmptyString(), randomNonEmptyString(), random.nextBoolean());
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		  SynonymMap map = b.build();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean ignoreCase = random.nextBoolean();
		  bool ignoreCase = random.nextBoolean();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Analyzer analyzer = new org.apache.lucene.analysis.Analyzer()
		  Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this, map, ignoreCase);

		  checkRandomData(random, analyzer, 100);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;
		  private bool ignoreCase;

		  public AnalyzerAnonymousInnerClassHelper2(TestSynonymMapFilter outerInstance, SynonymMap map, bool ignoreCase)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
			  this.ignoreCase = ignoreCase;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
			TokenStream syns = new SynonymFilter(tokenizer, map, ignoreCase);
			TokenStream graph = new MockGraphTokenFilter(random(), syns);
			return new TokenStreamComponents(tokenizer, graph);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Random random = random();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numIters = atLeast(10);
		int numIters = atLeast(10);
		for (int i = 0; i < numIters; i++)
		{
		  b = new SynonymMap.Builder(random.nextBoolean());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numEntries = atLeast(10);
		  int numEntries = atLeast(10);
		  for (int j = 0; j < numEntries; j++)
		  {
			add(randomNonEmptyString(), randomNonEmptyString(), random.nextBoolean());
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		  SynonymMap map = b.build();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean ignoreCase = random.nextBoolean();
		  bool ignoreCase = random.nextBoolean();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Analyzer analyzer = new org.apache.lucene.analysis.Analyzer()
		  Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper3(this, map, ignoreCase);

		  checkAnalysisConsistency(random, analyzer, random.nextBoolean(), "");
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;
		  private bool ignoreCase;

		  public AnalyzerAnonymousInnerClassHelper3(TestSynonymMapFilter outerInstance, SynonymMap map, bool ignoreCase)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
			  this.ignoreCase = ignoreCase;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, ignoreCase));
		  }
	  }

	  /// <summary>
	  /// simple random test like testRandom2, but for larger docs
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHuge() throws Exception
	  public virtual void testRandomHuge()
	  {
		Random random = random();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numIters = atLeast(3);
		int numIters = atLeast(3);
		for (int i = 0; i < numIters; i++)
		{
		  b = new SynonymMap.Builder(random.nextBoolean());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numEntries = atLeast(10);
		  int numEntries = atLeast(10);
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + i + " numEntries=" + numEntries);
		  }
		  for (int j = 0; j < numEntries; j++)
		  {
			add(randomNonEmptyString(), randomNonEmptyString(), random.nextBoolean());
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		  SynonymMap map = b.build();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean ignoreCase = random.nextBoolean();
		  bool ignoreCase = random.nextBoolean();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Analyzer analyzer = new org.apache.lucene.analysis.Analyzer()
		  Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper4(this, map, ignoreCase);

		  checkRandomData(random, analyzer, 100, 1024);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper4 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;
		  private bool ignoreCase;

		  public AnalyzerAnonymousInnerClassHelper4(TestSynonymMapFilter outerInstance, SynonymMap map, bool ignoreCase)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
			  this.ignoreCase = ignoreCase;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, ignoreCase));
		  }
	  }

	  // LUCENE-3375
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testVanishingTerms() throws Exception
	  public virtual void testVanishingTerms()
	  {
		string testFile = "aaa => aaaa1 aaaa2 aaaa3\n" + "bbb => bbbb1 bbbb2\n";

		SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(random()));
		parser.parse(new StringReader(testFile));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = parser.build();
		SynonymMap map = parser.build();

		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper5(this, map);

		// where did my pot go?!
		assertAnalyzesTo(analyzer, "xyzzy bbb pot of gold", new string[] {"xyzzy", "bbbb1", "pot", "bbbb2", "of", "gold"});

		// this one nukes 'pot' and 'of'
		// xyzzy aaa pot of gold -> xyzzy aaaa1 aaaa2 aaaa3 gold
		assertAnalyzesTo(analyzer, "xyzzy aaa pot of gold", new string[] {"xyzzy", "aaaa1", "pot", "aaaa2", "of", "aaaa3", "gold"});
	  }

	  private class AnalyzerAnonymousInnerClassHelper5 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper5(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasic2() throws Exception
	  public virtual void testBasic2()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = false;
		add("aaa", "aaaa1 aaaa2 aaaa3", keepOrig);
		add("bbb", "bbbb1 bbbb2", keepOrig);
		tokensIn = new MockTokenizer(new StringReader("a"), MockTokenizer.WHITESPACE, true);
		tokensIn.reset();
		assertTrue(tokensIn.incrementToken());
		assertFalse(tokensIn.incrementToken());
		tokensIn.end();
		tokensIn.close();

		tokensOut = new SynonymFilter(tokensIn, b.build(), true);
		termAtt = tokensOut.addAttribute(typeof(CharTermAttribute));
		posIncrAtt = tokensOut.addAttribute(typeof(PositionIncrementAttribute));
		posLenAtt = tokensOut.addAttribute(typeof(PositionLengthAttribute));
		offsetAtt = tokensOut.addAttribute(typeof(OffsetAttribute));

		if (keepOrig)
		{
		  verify("xyzzy bbb pot of gold", "xyzzy bbb/bbbb1 pot/bbbb2 of gold");
		  verify("xyzzy aaa pot of gold", "xyzzy aaa/aaaa1 pot/aaaa2 of/aaaa3 gold");
		}
		else
		{
		  verify("xyzzy bbb pot of gold", "xyzzy bbbb1 pot/bbbb2 of gold");
		  verify("xyzzy aaa pot of gold", "xyzzy aaaa1 pot/aaaa2 of/aaaa3 gold");
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMatching() throws Exception
	  public virtual void testMatching()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = false;
		add("a b", "ab", keepOrig);
		add("a c", "ac", keepOrig);
		add("a", "aa", keepOrig);
		add("b", "bb", keepOrig);
		add("z x c v", "zxcv", keepOrig);
		add("x c", "xc", keepOrig);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper6(this, map);

		checkOneTerm(a, "$", "$");
		checkOneTerm(a, "a", "aa");
		checkOneTerm(a, "b", "bb");

		assertAnalyzesTo(a, "a $", new string[] {"aa", "$"}, new int[] {1, 1});

		assertAnalyzesTo(a, "$ a", new string[] {"$", "aa"}, new int[] {1, 1});

		assertAnalyzesTo(a, "a a", new string[] {"aa", "aa"}, new int[] {1, 1});

		assertAnalyzesTo(a, "z x c v", new string[] {"zxcv"}, new int[] {1});

		assertAnalyzesTo(a, "z x c $", new string[] {"z", "xc", "$"}, new int[] {1, 1, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper6 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper6(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRepeatsOff() throws Exception
	  public virtual void testRepeatsOff()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = false;
		add("a b", "ab", keepOrig);
		add("a b", "ab", keepOrig);
		add("a b", "ab", keepOrig);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper7(this, map);

		assertAnalyzesTo(a, "a b", new string[] {"ab"}, new int[] {1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper7 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper7(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRepeatsOn() throws Exception
	  public virtual void testRepeatsOn()
	  {
		b = new SynonymMap.Builder(false);
		const bool keepOrig = false;
		add("a b", "ab", keepOrig);
		add("a b", "ab", keepOrig);
		add("a b", "ab", keepOrig);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper8(this, map);

		assertAnalyzesTo(a, "a b", new string[] {"ab", "ab", "ab"}, new int[] {1, 0, 0});
	  }

	  private class AnalyzerAnonymousInnerClassHelper8 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper8(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRecursion() throws Exception
	  public virtual void testRecursion()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = false;
		add("zoo", "zoo", keepOrig);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper9(this, map);

		assertAnalyzesTo(a, "zoo zoo $ zoo", new string[] {"zoo", "zoo", "$", "zoo"}, new int[] {1, 1, 1, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper9 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper9(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRecursion2() throws Exception
	  public virtual void testRecursion2()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = false;
		add("zoo", "zoo", keepOrig);
		add("zoo", "zoo zoo", keepOrig);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper10(this, map);

		// verify("zoo zoo $ zoo", "zoo/zoo zoo/zoo/zoo $/zoo zoo/zoo zoo");
		assertAnalyzesTo(a, "zoo zoo $ zoo", new string[] {"zoo", "zoo", "zoo", "zoo", "zoo", "$", "zoo", "zoo", "zoo", "zoo"}, new int[] {1, 0, 1, 0, 0, 1, 0, 1, 0, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper10 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper10(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOutputHangsOffEnd() throws Exception
	  public virtual void testOutputHangsOffEnd()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = false;
		// b hangs off the end (no input token under it):
		add("a", "a b", keepOrig);
		tokensIn = new MockTokenizer(new StringReader("a"), MockTokenizer.WHITESPACE, true);
		tokensIn.reset();
		assertTrue(tokensIn.incrementToken());
		assertFalse(tokensIn.incrementToken());
		tokensIn.end();
		tokensIn.close();

		tokensOut = new SynonymFilter(tokensIn, b.build(), true);
		termAtt = tokensOut.addAttribute(typeof(CharTermAttribute));
		posIncrAtt = tokensOut.addAttribute(typeof(PositionIncrementAttribute));
		offsetAtt = tokensOut.addAttribute(typeof(OffsetAttribute));
		posLenAtt = tokensOut.addAttribute(typeof(PositionLengthAttribute));

		// Make sure endOffset inherits from previous input token:
		verify("a", "a b:1");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIncludeOrig() throws Exception
	  public virtual void testIncludeOrig()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = true;
		add("a b", "ab", keepOrig);
		add("a c", "ac", keepOrig);
		add("a", "aa", keepOrig);
		add("b", "bb", keepOrig);
		add("z x c v", "zxcv", keepOrig);
		add("x c", "xc", keepOrig);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper11(this, map);

		assertAnalyzesTo(a, "$", new string[] {"$"}, new int[] {1});
		assertAnalyzesTo(a, "a", new string[] {"a", "aa"}, new int[] {1, 0});
		assertAnalyzesTo(a, "a", new string[] {"a", "aa"}, new int[] {1, 0});
		assertAnalyzesTo(a, "$ a", new string[] {"$", "a", "aa"}, new int[] {1, 1, 0});
		assertAnalyzesTo(a, "a $", new string[] {"a", "aa", "$"}, new int[] {1, 0, 1});
		assertAnalyzesTo(a, "$ a !", new string[] {"$", "a", "aa", "!"}, new int[] {1, 1, 0, 1});
		assertAnalyzesTo(a, "a a", new string[] {"a", "aa", "a", "aa"}, new int[] {1, 0, 1, 0});
		assertAnalyzesTo(a, "b", new string[] {"b", "bb"}, new int[] {1, 0});
		assertAnalyzesTo(a, "z x c v", new string[] {"z", "zxcv", "x", "c", "v"}, new int[] {1, 0, 1, 1, 1});
		assertAnalyzesTo(a, "z x c $", new string[] {"z", "x", "xc", "c", "$"}, new int[] {1, 1, 0, 1, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper11 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper11(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRecursion3() throws Exception
	  public virtual void testRecursion3()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = true;
		add("zoo zoo", "zoo", keepOrig);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper12(this, map);

		assertAnalyzesTo(a, "zoo zoo $ zoo", new string[] {"zoo", "zoo", "zoo", "$", "zoo"}, new int[] {1, 0, 1, 1, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper12 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper12(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRecursion4() throws Exception
	  public virtual void testRecursion4()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = true;
		add("zoo zoo", "zoo", keepOrig);
		add("zoo", "zoo zoo", keepOrig);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper13(this, map);

		assertAnalyzesTo(a, "zoo zoo $ zoo", new string[] {"zoo", "zoo", "zoo", "$", "zoo", "zoo", "zoo"}, new int[] {1, 0, 1, 1, 1, 0, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper13 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper13(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMultiwordOffsets() throws Exception
	  public virtual void testMultiwordOffsets()
	  {
		b = new SynonymMap.Builder(true);
		const bool keepOrig = true;
		add("national hockey league", "nhl", keepOrig);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SynonymMap map = b.build();
		SynonymMap map = b.build();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper14(this, map);

		assertAnalyzesTo(a, "national hockey league", new string[] {"national", "nhl", "hockey", "league"}, new int[] {0, 0, 9, 16}, new int[] {8, 22, 15, 22}, new int[] {1, 0, 1, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper14 : Analyzer
	  {
		  private readonly TestSynonymMapFilter outerInstance;

		  private SynonymMap map;

		  public AnalyzerAnonymousInnerClassHelper14(TestSynonymMapFilter outerInstance, SynonymMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmpty() throws Exception
	  public virtual void testEmpty()
	  {
		Tokenizer tokenizer = new MockTokenizer(new StringReader("aa bb"));
		try
		{
		  new SynonymFilter(tokenizer, (new SynonymMap.Builder(true)).build(), true);
		  fail("did not hit expected exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		  assertEquals("fst must be non-null", iae.Message);
		}
	  }
	}

}