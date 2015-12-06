using System;

namespace org.apache.lucene.analysis.snowball
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


	using TokenStreamComponents = org.apache.lucene.analysis.Analyzer.TokenStreamComponents;
	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using StandardAnalyzer = org.apache.lucene.analysis.standard.StandardAnalyzer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using FlagsAttribute = org.apache.lucene.analysis.tokenattributes.FlagsAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PayloadAttribute = org.apache.lucene.analysis.tokenattributes.PayloadAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using Version = org.apache.lucene.util.Version;

	public class TestSnowball : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEnglish() throws Exception
	  public virtual void testEnglish()
	  {
		Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "English");
		assertAnalyzesTo(a, "he abhorred accents", new string[]{"he", "abhor", "accent"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopwords() throws Exception
	  public virtual void testStopwords()
	  {
		Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "English", StandardAnalyzer.STOP_WORDS_SET);
		assertAnalyzesTo(a, "the quick brown fox jumped", new string[]{"quick", "brown", "fox", "jump"});
	  }

	  /// <summary>
	  /// Test english lowercasing. Test both cases (pre-3.1 and post-3.1) to ensure
	  /// we lowercase I correct for non-Turkish languages in either case.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEnglishLowerCase() throws Exception
	  public virtual void testEnglishLowerCase()
	  {
		Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "English");
		assertAnalyzesTo(a, "cryogenic", new string[] {"cryogen"});
		assertAnalyzesTo(a, "CRYOGENIC", new string[] {"cryogen"});

		Analyzer b = new SnowballAnalyzer(Version.LUCENE_30, "English");
		assertAnalyzesTo(b, "cryogenic", new string[] {"cryogen"});
		assertAnalyzesTo(b, "CRYOGENIC", new string[] {"cryogen"});
	  }

	  /// <summary>
	  /// Test turkish lowercasing
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTurkish() throws Exception
	  public virtual void testTurkish()
	  {
		Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "Turkish");

		assertAnalyzesTo(a, "ağacı", new string[] {"ağaç"});
		assertAnalyzesTo(a, "AĞACI", new string[] {"ağaç"});
	  }

	  /// <summary>
	  /// Test turkish lowercasing (old buggy behavior) </summary>
	  /// @deprecated (3.1) Remove this when support for 3.0 indexes is no longer required (5.0) 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) Remove this when support for 3.0 indexes is no longer required (5.0)") public void testTurkishBWComp() throws Exception
	  [Obsolete("(3.1) Remove this when support for 3.0 indexes is no longer required (5.0)")]
	  public virtual void testTurkishBWComp()
	  {
		Analyzer a = new SnowballAnalyzer(Version.LUCENE_30, "Turkish");
		// AĞACI in turkish lowercases to ağacı, but with lowercase filter ağaci.
		// this fails due to wrong casing, because the stemmer
		// will only remove -ı, not -i
		assertAnalyzesTo(a, "ağacı", new string[] {"ağaç"});
		assertAnalyzesTo(a, "AĞACI", new string[] {"ağaci"});
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		Analyzer a = new SnowballAnalyzer(TEST_VERSION_CURRENT, "English");
		assertAnalyzesTo(a, "he abhorred accents", new string[]{"he", "abhor", "accent"});
		assertAnalyzesTo(a, "she abhorred him", new string[]{"she", "abhor", "him"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFilterTokens() throws Exception
	  public virtual void testFilterTokens()
	  {
		SnowballFilter filter = new SnowballFilter(new TestTokenStream(this), "English");
		CharTermAttribute termAtt = filter.getAttribute(typeof(CharTermAttribute));
		OffsetAttribute offsetAtt = filter.getAttribute(typeof(OffsetAttribute));
		TypeAttribute typeAtt = filter.getAttribute(typeof(TypeAttribute));
		PayloadAttribute payloadAtt = filter.getAttribute(typeof(PayloadAttribute));
		PositionIncrementAttribute posIncAtt = filter.getAttribute(typeof(PositionIncrementAttribute));
		FlagsAttribute flagsAtt = filter.getAttribute(typeof(FlagsAttribute));

		filter.incrementToken();

		assertEquals("accent", termAtt.ToString());
		assertEquals(2, offsetAtt.startOffset());
		assertEquals(7, offsetAtt.endOffset());
		assertEquals("wrd", typeAtt.type());
		assertEquals(3, posIncAtt.PositionIncrement);
		assertEquals(77, flagsAtt.Flags);
		assertEquals(new BytesRef(new sbyte[]{0,1,2,3}), payloadAtt.Payload);
	  }

	  private sealed class TestTokenStream : TokenStream
	  {
		  private readonly TestSnowball outerInstance;

		internal readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		internal readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
		internal readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));
		internal readonly PayloadAttribute payloadAtt = addAttribute(typeof(PayloadAttribute));
		internal readonly PositionIncrementAttribute posIncAtt = addAttribute(typeof(PositionIncrementAttribute));
		internal readonly FlagsAttribute flagsAtt = addAttribute(typeof(FlagsAttribute));

		internal TestTokenStream(TestSnowball outerInstance) : base()
		{
			this.outerInstance = outerInstance;
		}

		public override bool incrementToken()
		{
		  clearAttributes();
		  termAtt.setEmpty().append("accents");
		  offsetAtt.setOffset(2, 7);
		  typeAtt.Type = "wrd";
		  posIncAtt.PositionIncrement = 3;
		  payloadAtt.Payload = new BytesRef(new sbyte[]{0,1,2,3});
		  flagsAtt.Flags = 77;
		  return true;
		}
	  }

	  /// <summary>
	  /// for testing purposes ONLY </summary>
	  public static string[] SNOWBALL_LANGS = new string[] {"Armenian", "Basque", "Catalan", "Danish", "Dutch", "English", "Finnish", "French", "German2", "German", "Hungarian", "Irish", "Italian", "Kp", "Lovins", "Norwegian", "Porter", "Portuguese", "Romanian", "Russian", "Spanish", "Swedish", "Turkish"};

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		foreach (String lang in SNOWBALL_LANGS)
		{
		  Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		  checkOneTerm(a, "", "");
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestSnowball outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestSnowball outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new Analyzer.TokenStreamComponents(tokenizer, new SnowballFilter(tokenizer, lang));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws java.io.IOException
	  public virtual void testRandomStrings()
	  {
		foreach (string lang in SNOWBALL_LANGS)
		{
		  checkRandomStrings(lang);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void checkRandomStrings(final String snowballLanguage) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public virtual void checkRandomStrings(string snowballLanguage)
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, snowballLanguage);
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestSnowball outerInstance;

		  private string snowballLanguage;

		  public AnalyzerAnonymousInnerClassHelper2(TestSnowball outerInstance, string snowballLanguage)
		  {
			  this.outerInstance = outerInstance;
			  this.snowballLanguage = snowballLanguage;
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new MockTokenizer(reader);
			return new Analyzer.TokenStreamComponents(t, new SnowballFilter(t, snowballLanguage));
		  }
	  }
	}
}