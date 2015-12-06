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


	using org.apache.lucene.analysis;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PayloadAttribute = org.apache.lucene.analysis.tokenattributes.PayloadAttribute;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using Version = org.apache.lucene.util.Version;

	public class TestAnalyzers : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSimple() throws Exception
	  public virtual void testSimple()
	  {
		Analyzer a = new SimpleAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "foo bar FOO BAR", new string[] {"foo", "bar", "foo", "bar"});
		assertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[] {"foo", "bar", "foo", "bar"});
		assertAnalyzesTo(a, "foo.bar.FOO.BAR", new string[] {"foo", "bar", "foo", "bar"});
		assertAnalyzesTo(a, "U.S.A.", new string[] {"u", "s", "a"});
		assertAnalyzesTo(a, "C++", new string[] {"c"});
		assertAnalyzesTo(a, "B2B", new string[] {"b", "b"});
		assertAnalyzesTo(a, "2B", new string[] {"b"});
		assertAnalyzesTo(a, "\"QUOTED\" word", new string[] {"quoted", "word"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNull() throws Exception
	  public virtual void testNull()
	  {
		Analyzer a = new WhitespaceAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "foo bar FOO BAR", new string[] {"foo", "bar", "FOO", "BAR"});
		assertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[] {"foo", "bar", ".", "FOO", "<>", "BAR"});
		assertAnalyzesTo(a, "foo.bar.FOO.BAR", new string[] {"foo.bar.FOO.BAR"});
		assertAnalyzesTo(a, "U.S.A.", new string[] {"U.S.A."});
		assertAnalyzesTo(a, "C++", new string[] {"C++"});
		assertAnalyzesTo(a, "B2B", new string[] {"B2B"});
		assertAnalyzesTo(a, "2B", new string[] {"2B"});
		assertAnalyzesTo(a, "\"QUOTED\" word", new string[] {"\"QUOTED\"", "word"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStop() throws Exception
	  public virtual void testStop()
	  {
		Analyzer a = new StopAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "foo bar FOO BAR", new string[] {"foo", "bar", "foo", "bar"});
		assertAnalyzesTo(a, "foo a bar such FOO THESE BAR", new string[] {"foo", "bar", "foo", "bar"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void verifyPayload(TokenStream ts) throws java.io.IOException
	  internal virtual void verifyPayload(TokenStream ts)
	  {
		PayloadAttribute payloadAtt = ts.getAttribute(typeof(PayloadAttribute));
		ts.reset();
		for (sbyte b = 1;;b++)
		{
		  bool hasNext = ts.incrementToken();
		  if (!hasNext)
		  {
			  break;
		  }
		  // System.out.println("id="+System.identityHashCode(nextToken) + " " + t);
		  // System.out.println("payload=" + (int)nextToken.getPayload().toByteArray()[0]);
		  assertEquals(b, payloadAtt.Payload.bytes[0]);
		}
	  }

	  // Make sure old style next() calls result in a new copy of payloads
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPayloadCopy() throws java.io.IOException
	  public virtual void testPayloadCopy()
	  {
		string s = "how now brown cow";
		TokenStream ts;
		ts = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(s));
		ts = new PayloadSetter(ts);
		verifyPayload(ts);

		ts = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(s));
		ts = new PayloadSetter(ts);
		verifyPayload(ts);
	  }

	  // LUCENE-1150: Just a compile time test, to ensure the
	  // StandardAnalyzer constants remain publicly accessible
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unused") public void _testStandardConstants()
	  public virtual void _testStandardConstants()
	  {
		int x = StandardTokenizer.ALPHANUM;
		x = StandardTokenizer.APOSTROPHE;
		x = StandardTokenizer.ACRONYM;
		x = StandardTokenizer.COMPANY;
		x = StandardTokenizer.EMAIL;
		x = StandardTokenizer.HOST;
		x = StandardTokenizer.NUM;
		x = StandardTokenizer.CJ;
		string[] y = StandardTokenizer.TOKEN_TYPES;
	  }

	  private class LowerCaseWhitespaceAnalyzer : Analyzer
	  {

		public override TokenStreamComponents createComponents(string fieldName, Reader reader)
		{
		  Tokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, reader);
		  return new TokenStreamComponents(tokenizer, new LowerCaseFilter(TEST_VERSION_CURRENT, tokenizer));
		}

	  }

	  private class UpperCaseWhitespaceAnalyzer : Analyzer
	  {

		public override TokenStreamComponents createComponents(string fieldName, Reader reader)
		{
		  Tokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, reader);
		  return new TokenStreamComponents(tokenizer, new UpperCaseFilter(TEST_VERSION_CURRENT, tokenizer));
		}

	  }


	  /// <summary>
	  /// Test that LowercaseFilter handles entire unicode range correctly
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLowerCaseFilter() throws java.io.IOException
	  public virtual void testLowerCaseFilter()
	  {
		Analyzer a = new LowerCaseWhitespaceAnalyzer();
		// BMP
		assertAnalyzesTo(a, "AbaCaDabA", new string[] {"abacadaba"});
		// supplementary
		assertAnalyzesTo(a, "\ud801\udc16\ud801\udc16\ud801\udc16\ud801\udc16", new string[] {"\ud801\udc3e\ud801\udc3e\ud801\udc3e\ud801\udc3e"});
		assertAnalyzesTo(a, "AbaCa\ud801\udc16DabA", new string[] {"abaca\ud801\udc3edaba"});
		// unpaired lead surrogate
		assertAnalyzesTo(a, "AbaC\uD801AdaBa", new string [] {"abac\uD801adaba"});
		// unpaired trail surrogate
		assertAnalyzesTo(a, "AbaC\uDC16AdaBa", new string [] {"abac\uDC16adaba"});
	  }

	  /// <summary>
	  /// Test that LowercaseFilter handles entire unicode range correctly
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUpperCaseFilter() throws java.io.IOException
	  public virtual void testUpperCaseFilter()
	  {
		Analyzer a = new UpperCaseWhitespaceAnalyzer();
		// BMP
		assertAnalyzesTo(a, "AbaCaDabA", new string[] {"ABACADABA"});
		// supplementary
		assertAnalyzesTo(a, "\ud801\udc3e\ud801\udc3e\ud801\udc3e\ud801\udc3e", new string[] {"\ud801\udc16\ud801\udc16\ud801\udc16\ud801\udc16"});
		assertAnalyzesTo(a, "AbaCa\ud801\udc3eDabA", new string[] {"ABACA\ud801\udc16DABA"});
		// unpaired lead surrogate
		assertAnalyzesTo(a, "AbaC\uD801AdaBa", new string [] {"ABAC\uD801ADABA"});
		// unpaired trail surrogate
		assertAnalyzesTo(a, "AbaC\uDC16AdaBa", new string [] {"ABAC\uDC16ADABA"});
	  }


	  /// <summary>
	  /// Test that LowercaseFilter handles the lowercasing correctly if the term
	  /// buffer has a trailing surrogate character leftover and the current term in
	  /// the buffer ends with a corresponding leading surrogate.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLowerCaseFilterLowSurrogateLeftover() throws java.io.IOException
	  public virtual void testLowerCaseFilterLowSurrogateLeftover()
	  {
		// test if the limit of the termbuffer is correctly used with supplementary
		// chars
		WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("BogustermBogusterm\udc16"));
		LowerCaseFilter filter = new LowerCaseFilter(TEST_VERSION_CURRENT, tokenizer);
		assertTokenStreamContents(filter, new string[] {"bogustermbogusterm\udc16"});
		filter.reset();
		string highSurEndingUpper = "BogustermBoguster\ud801";
		string highSurEndingLower = "bogustermboguster\ud801";
		tokenizer.Reader = new StringReader(highSurEndingUpper);
		assertTokenStreamContents(filter, new string[] {highSurEndingLower});
		assertTrue(filter.hasAttribute(typeof(CharTermAttribute)));
		char[] termBuffer = filter.getAttribute(typeof(CharTermAttribute)).buffer();
		int length = highSurEndingLower.Length;
		assertEquals('\ud801', termBuffer[length - 1]);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLowerCaseTokenizer() throws java.io.IOException
	  public virtual void testLowerCaseTokenizer()
	  {
		StringReader reader = new StringReader("Tokenizer \ud801\udc1ctest");
		LowerCaseTokenizer tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, reader);
		assertTokenStreamContents(tokenizer, new string[] {"tokenizer", "\ud801\udc44test"});
	  }

	  /// @deprecated (3.1) 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1)") public void testLowerCaseTokenizerBWCompat() throws java.io.IOException
	  [Obsolete("(3.1)")]
	  public virtual void testLowerCaseTokenizerBWCompat()
	  {
		StringReader reader = new StringReader("Tokenizer \ud801\udc1ctest");
		LowerCaseTokenizer tokenizer = new LowerCaseTokenizer(Version.LUCENE_30, reader);
		assertTokenStreamContents(tokenizer, new string[] {"tokenizer", "test"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWhitespaceTokenizer() throws java.io.IOException
	  public virtual void testWhitespaceTokenizer()
	  {
		StringReader reader = new StringReader("Tokenizer \ud801\udc1ctest");
		WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, reader);
		assertTokenStreamContents(tokenizer, new string[] {"Tokenizer", "\ud801\udc1ctest"});
	  }

	  /// @deprecated (3.1) 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1)") public void testWhitespaceTokenizerBWCompat() throws java.io.IOException
	  [Obsolete("(3.1)")]
	  public virtual void testWhitespaceTokenizerBWCompat()
	  {
		StringReader reader = new StringReader("Tokenizer \ud801\udc1ctest");
		WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(Version.LUCENE_30, reader);
		assertTokenStreamContents(tokenizer, new string[] {"Tokenizer", "\ud801\udc1ctest"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new WhitespaceAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
		checkRandomData(random(), new SimpleAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
		checkRandomData(random(), new StopAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }

	  /// <summary>
	  /// blast some random large strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHugeStrings() throws Exception
	  public virtual void testRandomHugeStrings()
	  {
		Random random = random();
		checkRandomData(random, new WhitespaceAnalyzer(TEST_VERSION_CURRENT), 100 * RANDOM_MULTIPLIER, 8192);
		checkRandomData(random, new SimpleAnalyzer(TEST_VERSION_CURRENT), 100 * RANDOM_MULTIPLIER, 8192);
		checkRandomData(random, new StopAnalyzer(TEST_VERSION_CURRENT), 100 * RANDOM_MULTIPLIER, 8192);
	  }
	}

	internal sealed class PayloadSetter : TokenFilter
	{
		private bool InstanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			p = new BytesRef(data,0,1);
		}

	  internal PayloadAttribute payloadAtt;
	  public PayloadSetter(TokenStream input) : base(input)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
		payloadAtt = addAttribute(typeof(PayloadAttribute));
	  }

	  internal sbyte[] data = new sbyte[1];
	  internal BytesRef p;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		bool hasNext = input.incrementToken();
		if (!hasNext)
		{
			return false;
		}
		payloadAtt.Payload = p; // reuse the payload / byte[]
		data[0]++;
		return true;
	  }
	}

}