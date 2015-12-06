using System;

namespace org.apache.lucene.analysis.cjk
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


	using MappingCharFilter = org.apache.lucene.analysis.charfilter.MappingCharFilter;
	using NormalizeCharMap = org.apache.lucene.analysis.charfilter.NormalizeCharMap;
	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

	/// <summary>
	/// Most tests adopted from TestCJKTokenizer
	/// </summary>
	public class TestCJKAnalyzer : BaseTokenStreamTestCase
	{
	  private Analyzer analyzer = new CJKAnalyzer(TEST_VERSION_CURRENT);

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testJa1() throws java.io.IOException
	  public virtual void testJa1()
	  {
		assertAnalyzesTo(analyzer, "一二三四五六七八九十", new string[] {"一二", "二三", "三四", "四五", "五六", "六七", "七八", "八九", "九十"}, new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8}, new int[] {2, 3, 4, 5, 6, 7, 8, 9, 10}, new string[] {"<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>"}, new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testJa2() throws java.io.IOException
	  public virtual void testJa2()
	  {
		assertAnalyzesTo(analyzer, "一 二三四 五六七八九 十", new string[] {"一", "二三", "三四", "五六", "六七", "七八", "八九", "十"}, new int[] {0, 2, 3, 6, 7, 8, 9, 12}, new int[] {1, 4, 5, 8, 9, 10, 11, 13}, new string[] {"<SINGLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<SINGLE>"}, new int[] {1, 1, 1, 1, 1, 1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testC() throws java.io.IOException
	  public virtual void testC()
	  {
		assertAnalyzesTo(analyzer, "abc defgh ijklmn opqrstu vwxy z", new string[] {"abc", "defgh", "ijklmn", "opqrstu", "vwxy", "z"}, new int[] {0, 4, 10, 17, 25, 30}, new int[] {3, 9, 16, 24, 29, 31}, new string[] {"<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>"}, new int[] {1, 1, 1, 1, 1, 1});
	  }

	  /// <summary>
	  /// LUCENE-2207: wrong offset calculated by end() 
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFinalOffset() throws java.io.IOException
	  public virtual void testFinalOffset()
	  {
		assertAnalyzesTo(analyzer, "あい", new string[] {"あい"}, new int[] {0}, new int[] {2}, new string[] {"<DOUBLE>"}, new int[] {1});

		assertAnalyzesTo(analyzer, "あい   ", new string[] {"あい"}, new int[] {0}, new int[] {2}, new string[] {"<DOUBLE>"}, new int[] {1});

		assertAnalyzesTo(analyzer, "test", new string[] {"test"}, new int[] {0}, new int[] {4}, new string[] {"<ALPHANUM>"}, new int[] {1});

		assertAnalyzesTo(analyzer, "test   ", new string[] {"test"}, new int[] {0}, new int[] {4}, new string[] {"<ALPHANUM>"}, new int[] {1});

		assertAnalyzesTo(analyzer, "あいtest", new string[] {"あい", "test"}, new int[] {0, 2}, new int[] {2, 6}, new string[] {"<DOUBLE>", "<ALPHANUM>"}, new int[] {1, 1});

		assertAnalyzesTo(analyzer, "testあい    ", new string[] {"test", "あい"}, new int[] {0, 4}, new int[] {4, 6}, new string[] {"<ALPHANUM>", "<DOUBLE>"}, new int[] {1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMix() throws java.io.IOException
	  public virtual void testMix()
	  {
		assertAnalyzesTo(analyzer, "あいうえおabcかきくけこ", new string[] {"あい", "いう", "うえ", "えお", "abc", "かき", "きく", "くけ", "けこ"}, new int[] {0, 1, 2, 3, 5, 8, 9, 10, 11}, new int[] {2, 3, 4, 5, 8, 10, 11, 12, 13}, new string[] {"<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<ALPHANUM>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>"}, new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMix2() throws java.io.IOException
	  public virtual void testMix2()
	  {
		assertAnalyzesTo(analyzer, "あいうえおabんcかきくけ こ", new string[] {"あい", "いう", "うえ", "えお", "ab", "ん", "c", "かき", "きく", "くけ", "こ"}, new int[] {0, 1, 2, 3, 5, 7, 8, 9, 10, 11, 14}, new int[] {2, 3, 4, 5, 7, 8, 9, 11, 12, 13, 15}, new string[] {"<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<ALPHANUM>", "<SINGLE>", "<ALPHANUM>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<SINGLE>"}, new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1});
	  }

	  /// <summary>
	  /// Non-english text (outside of CJK) is treated normally, according to unicode rules 
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNonIdeographic() throws java.io.IOException
	  public virtual void testNonIdeographic()
	  {
		assertAnalyzesTo(analyzer, "一 روبرت موير", new string[] {"一", "روبرت", "موير"}, new int[] {0, 2, 8}, new int[] {1, 7, 12}, new string[] {"<SINGLE>", "<ALPHANUM>", "<ALPHANUM>"}, new int[] {1, 1, 1});
	  }

	  /// <summary>
	  /// Same as the above, except with a nonspacing mark to show correctness.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNonIdeographicNonLetter() throws java.io.IOException
	  public virtual void testNonIdeographicNonLetter()
	  {
		assertAnalyzesTo(analyzer, "一 رُوبرت موير", new string[] {"一", "رُوبرت", "موير"}, new int[] {0, 2, 9}, new int[] {1, 8, 13}, new string[] {"<SINGLE>", "<ALPHANUM>", "<ALPHANUM>"}, new int[] {1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSurrogates() throws java.io.IOException
	  public virtual void testSurrogates()
	  {
		assertAnalyzesTo(analyzer, "𩬅艱鍟䇹愯瀛", new string[] {"𩬅艱", "艱鍟", "鍟䇹", "䇹愯", "愯瀛"}, new int[] {0, 2, 3, 4, 5}, new int[] {3, 4, 5, 6, 7}, new string[] {"<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>"}, new int[] {1, 1, 1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws java.io.IOException
	  public virtual void testReusableTokenStream()
	  {
		assertAnalyzesTo(analyzer, "あいうえおabcかきくけこ", new string[] {"あい", "いう", "うえ", "えお", "abc", "かき", "きく", "くけ", "けこ"}, new int[] {0, 1, 2, 3, 5, 8, 9, 10, 11}, new int[] {2, 3, 4, 5, 8, 10, 11, 12, 13}, new string[] {"<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<ALPHANUM>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>"}, new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1});

		assertAnalyzesTo(analyzer, "あいうえおabんcかきくけ こ", new string[] {"あい", "いう", "うえ", "えお", "ab", "ん", "c", "かき", "きく", "くけ", "こ"}, new int[] {0, 1, 2, 3, 5, 7, 8, 9, 10, 11, 14}, new int[] {2, 3, 4, 5, 7, 8, 9, 11, 12, 13, 15}, new string[] {"<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<ALPHANUM>", "<SINGLE>", "<ALPHANUM>", "<DOUBLE>", "<DOUBLE>", "<DOUBLE>", "<SINGLE>"}, new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSingleChar() throws java.io.IOException
	  public virtual void testSingleChar()
	  {
		assertAnalyzesTo(analyzer, "一", new string[] {"一"}, new int[] {0}, new int[] {1}, new string[] {"<SINGLE>"}, new int[] {1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTokenStream() throws java.io.IOException
	  public virtual void testTokenStream()
	  {
		assertAnalyzesTo(analyzer, "一丁丂", new string[] {"一丁", "丁丂"}, new int[] {0, 1}, new int[] {2, 3}, new string[] {"<DOUBLE>", "<DOUBLE>"}, new int[] {1, 1});
	  }

	  /// <summary>
	  /// test that offsets are correct when mappingcharfilter is previously applied </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testChangedOffsets() throws java.io.IOException
	  public virtual void testChangedOffsets()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.charfilter.NormalizeCharMap.Builder builder = new org.apache.lucene.analysis.charfilter.NormalizeCharMap.Builder();
		NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		builder.add("a", "一二");
		builder.add("b", "二三");
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.charfilter.NormalizeCharMap norm = builder.build();
		NormalizeCharMap norm = builder.build();
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, norm);

		assertAnalyzesTo(analyzer, "ab", new string[] {"一二", "二二", "二三"}, new int[] {0, 0, 1}, new int[] {1, 1, 2});

		// note: offsets are strange since this is how the charfilter maps them... 
		// before bigramming, the 4 tokens look like:
		//   { 0, 0, 1, 1 },
		//   { 0, 1, 1, 2 }
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestCJKAnalyzer outerInstance;

		  private NormalizeCharMap norm;

		  public AnalyzerAnonymousInnerClassHelper(TestCJKAnalyzer outerInstance, NormalizeCharMap norm)
		  {
			  this.outerInstance = outerInstance;
			  this.norm = norm;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, new CJKBigramFilter(tokenizer));
		  }

		  protected internal override Reader initReader(string fieldName, Reader reader)
		  {
			return new MappingCharFilter(norm, reader);
		  }
	  }

	  private class FakeStandardTokenizer : TokenFilter
	  {
		internal readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));

		public FakeStandardTokenizer(TokenStream input) : base(input)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (input.incrementToken())
		  {
			typeAtt.Type = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.IDEOGRAPHIC];
			return true;
		  }
		  else
		  {
			return false;
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSingleChar2() throws Exception
	  public virtual void testSingleChar2()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);

		assertAnalyzesTo(analyzer, "一", new string[] {"一"}, new int[] {0}, new int[] {1}, new string[] {"<SINGLE>"}, new int[] {1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestCJKAnalyzer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestCJKAnalyzer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenFilter filter = new FakeStandardTokenizer(tokenizer);
			filter = new StopFilter(TEST_VERSION_CURRENT, filter, CharArraySet.EMPTY_SET);
			filter = new CJKBigramFilter(filter);
			return new TokenStreamComponents(tokenizer, filter);
		  }
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new CJKAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHugeStrings() throws Exception
	  public virtual void testRandomHugeStrings()
	  {
		Random random = random();
		checkRandomData(random, new CJKAnalyzer(TEST_VERSION_CURRENT), 100 * RANDOM_MULTIPLIER, 8192);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestCJKAnalyzer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestCJKAnalyzer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new CJKBigramFilter(tokenizer));
		  }
	  }
	}

}