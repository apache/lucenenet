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


	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;

	public class TestCJKBigramFilter : BaseTokenStreamTestCase
	{
	  internal Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(t, new CJKBigramFilter(t));
		  }
	  }

	  internal Analyzer unibiAnalyzer = new AnalyzerAnonymousInnerClassHelper2();

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper2()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(t, new CJKBigramFilter(t, 0xff, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHuge() throws Exception
	  public virtual void testHuge()
	  {
		assertAnalyzesTo(analyzer, "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた", new string[] {"多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた", "た多", "多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHanOnly() throws Exception
	  public virtual void testHanOnly()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
		assertAnalyzesTo(a, "多くの学生が試験に落ちた。", new string[] {"多", "く", "の", "学生", "が", "試験", "に", "落", "ち", "た"}, new int[] {0, 1, 2, 3, 5, 6, 8, 9, 10, 11}, new int[] {1, 2, 3, 5, 6, 8, 9, 10, 11, 12}, new string[] {"<SINGLE>", "<HIRAGANA>", "<HIRAGANA>", "<DOUBLE>", "<HIRAGANA>", "<DOUBLE>", "<HIRAGANA>", "<SINGLE>", "<HIRAGANA>", "<HIRAGANA>", "<SINGLE>"}, new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1}, new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestCJKBigramFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(TestCJKBigramFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(t, new CJKBigramFilter(t, CJKBigramFilter.HAN));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAllScripts() throws Exception
	  public virtual void testAllScripts()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper4(this);
		assertAnalyzesTo(a, "多くの学生が試験に落ちた。", new string[] {"多く", "くの", "の学", "学生", "生が", "が試", "試験", "験に", "に落", "落ち", "ちた"});
	  }

	  private class AnalyzerAnonymousInnerClassHelper4 : Analyzer
	  {
		  private readonly TestCJKBigramFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper4(TestCJKBigramFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(t, new CJKBigramFilter(t, 0xff, false));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUnigramsAndBigramsAllScripts() throws Exception
	  public virtual void testUnigramsAndBigramsAllScripts()
	  {
		assertAnalyzesTo(unibiAnalyzer, "多くの学生が試験に落ちた。", new string[] {"多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た"}, new int[] {0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11}, new int[] {1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12}, new string[] {"<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<DOUBLE>", "<SINGLE>"}, new int[] {1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1}, new int[] {1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUnigramsAndBigramsHanOnly() throws Exception
	  public virtual void testUnigramsAndBigramsHanOnly()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper5(this);
		assertAnalyzesTo(a, "多くの学生が試験に落ちた。", new string[] {"多", "く", "の", "学", "学生", "生", "が", "試", "試験", "験", "に", "落", "ち", "た"}, new int[] {0, 1, 2, 3, 3, 4, 5, 6, 6, 7, 8, 9, 10, 11}, new int[] {1, 2, 3, 4, 5, 5, 6, 7, 8, 8, 9, 10, 11, 12}, new string[] {"<SINGLE>", "<HIRAGANA>", "<HIRAGANA>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<HIRAGANA>", "<SINGLE>", "<DOUBLE>", "<SINGLE>", "<HIRAGANA>", "<SINGLE>", "<HIRAGANA>", "<HIRAGANA>", "<SINGLE>"}, new int[] {1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, 1}, new int[] {1, 1, 1, 1, 2, 1, 1, 1, 2, 1, 1, 1, 1, 1});
	  }

	  private class AnalyzerAnonymousInnerClassHelper5 : Analyzer
	  {
		  private readonly TestCJKBigramFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper5(TestCJKBigramFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(t, new CJKBigramFilter(t, CJKBigramFilter.HAN, true));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUnigramsAndBigramsHuge() throws Exception
	  public virtual void testUnigramsAndBigramsHuge()
	  {
		assertAnalyzesTo(unibiAnalyzer, "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた" + "多くの学生が試験に落ちた", new string[] {"多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た", "た多", "多", "多く", "く", "くの", "の", "の学", "学", "学生", "生", "生が", "が", "が試", "試", "試験", "験", "験に", "に", "に落", "落", "落ち", "ち", "ちた", "た"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomUnibiStrings() throws Exception
	  public virtual void testRandomUnibiStrings()
	  {
		checkRandomData(random(), unibiAnalyzer, 1000 * RANDOM_MULTIPLIER);
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomUnibiHugeStrings() throws Exception
	  public virtual void testRandomUnibiHugeStrings()
	  {
		Random random = random();
		checkRandomData(random, unibiAnalyzer, 100 * RANDOM_MULTIPLIER, 8192);
	  }
	}

}