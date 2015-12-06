using System;
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

namespace org.apache.lucene.analysis.pattern
{


	using TestUtil = org.apache.lucene.util.TestUtil;
	using Ignore = org.junit.Ignore;

	/// <summary>
	/// Tests <seealso cref="PatternReplaceCharFilter"/>
	/// </summary>
	public class TestPatternReplaceCharFilter : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFailingDot() throws java.io.IOException
	  public virtual void testFailingDot()
	  {
		checkOutput("A. .B.", "\\.[\\s]*", ".", "A..B.", "A..B.");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLongerReplacement() throws java.io.IOException
	  public virtual void testLongerReplacement()
	  {
		checkOutput("XXabcZZabcYY", "abc", "abcde", "XXabcdeZZabcdeYY", "XXabcccZZabcccYY");
		checkOutput("XXabcabcYY", "abc", "abcde", "XXabcdeabcdeYY", "XXabcccabcccYY");
		checkOutput("abcabcYY", "abc", "abcde", "abcdeabcdeYY", "abcccabcccYY");
		checkOutput("YY", "^", "abcde", "abcdeYY", "YYYYYYY");
			// Should be: "-----YY" but we're enforcing non-negative offsets.
		checkOutput("YY", "$", "abcde", "YYabcde", "YYYYYYY");
		checkOutput("XYZ", ".", "abc", "abcabcabc", "XXXYYYZZZ");
		checkOutput("XYZ", ".", "$0abc", "XabcYabcZabc", "XXXXYYYYZZZZ");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testShorterReplacement() throws java.io.IOException
	  public virtual void testShorterReplacement()
	  {
		checkOutput("XXabcZZabcYY", "abc", "xy", "XXxyZZxyYY", "XXabZZabYY");
		checkOutput("XXabcabcYY", "abc", "xy", "XXxyxyYY", "XXababYY");
		checkOutput("abcabcYY", "abc", "xy", "xyxyYY", "ababYY");
		checkOutput("abcabcYY", "abc", "", "YY", "YY");
		checkOutput("YYabcabc", "abc", "", "YY", "YY");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void checkOutput(String input, String pattern, String replacement, String expectedOutput, String expectedIndexMatchedOutput) throws java.io.IOException
	  private void checkOutput(string input, string pattern, string replacement, string expectedOutput, string expectedIndexMatchedOutput)
	  {
		  CharFilter cs = new PatternReplaceCharFilter(pattern(pattern), replacement, new StringReader(input));

		StringBuilder output = new StringBuilder();
		for (int chr = cs.read(); chr > 0; chr = cs.read())
		{
		  output.Append((char) chr);
		}

		StringBuilder indexMatched = new StringBuilder();
		for (int i = 0; i < output.Length; i++)
		{
		  indexMatched.Append((cs.correctOffset(i) < 0 ? "-" : input[cs.correctOffset(i)]));
		}

		bool outputGood = expectedOutput.Equals(output.ToString());
		bool indexMatchedGood = expectedIndexMatchedOutput.Equals(indexMatched.ToString());

		if (!outputGood || !indexMatchedGood || false)
		{
		  Console.WriteLine("Pattern : " + pattern);
		  Console.WriteLine("Replac. : " + replacement);
		  Console.WriteLine("Input   : " + input);
		  Console.WriteLine("Output  : " + output);
		  Console.WriteLine("Expected: " + expectedOutput);
		  Console.WriteLine("Output/i: " + indexMatched);
		  Console.WriteLine("Expected: " + expectedIndexMatchedOutput);
		  Console.WriteLine();
		}

		assertTrue("Output doesn't match.", outputGood);
		assertTrue("Index-matched output doesn't match.", indexMatchedGood);
	  }

	  //           1111
	  // 01234567890123
	  // this is test.
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNothingChange() throws java.io.IOException
	  public virtual void testNothingChange()
	  {
		const string BLOCK = "this is test.";
		CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1$2$3", new StringReader(BLOCK));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"this", "is", "test."}, new int[] {0, 5, 8}, new int[] {4, 7, 13}, BLOCK.Length);
	  }

	  // 012345678
	  // aa bb cc
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReplaceByEmpty() throws java.io.IOException
	  public virtual void testReplaceByEmpty()
	  {
		const string BLOCK = "aa bb cc";
		CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "", new StringReader(BLOCK));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {});
	  }

	  // 012345678
	  // aa bb cc
	  // aa#bb#cc
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test1block1matchSameLength() throws java.io.IOException
	  public virtual void test1block1matchSameLength()
	  {
		const string BLOCK = "aa bb cc";
		CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1#$2#$3", new StringReader(BLOCK));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"aa#bb#cc"}, new int[] {0}, new int[] {8}, BLOCK.Length);
	  }

	  //           11111
	  // 012345678901234
	  // aa bb cc dd
	  // aa##bb###cc dd
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test1block1matchLonger() throws java.io.IOException
	  public virtual void test1block1matchLonger()
	  {
		const string BLOCK = "aa bb cc dd";
		CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1##$2###$3", new StringReader(BLOCK));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"aa##bb###cc", "dd"}, new int[] {0, 9}, new int[] {8, 11}, BLOCK.Length);
	  }

	  // 01234567
	  //  a  a
	  //  aa  aa
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test1block2matchLonger() throws java.io.IOException
	  public virtual void test1block2matchLonger()
	  {
		const string BLOCK = " a  a";
		CharFilter cs = new PatternReplaceCharFilter(pattern("a"), "aa", new StringReader(BLOCK));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"aa", "aa"}, new int[] {1, 4}, new int[] {2, 5}, BLOCK.Length);
	  }

	  //           11111
	  // 012345678901234
	  // aa  bb   cc dd
	  // aa#bb dd
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test1block1matchShorter() throws java.io.IOException
	  public virtual void test1block1matchShorter()
	  {
		const string BLOCK = "aa  bb   cc dd";
		CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1#$2", new StringReader(BLOCK));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"aa#bb", "dd"}, new int[] {0, 12}, new int[] {11, 14}, BLOCK.Length);
	  }

	  //           111111111122222222223333
	  // 0123456789012345678901234567890123
	  //   aa bb cc --- aa bb aa   bb   cc
	  //   aa  bb  cc --- aa bb aa  bb  cc
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test1blockMultiMatches() throws java.io.IOException
	  public virtual void test1blockMultiMatches()
	  {
		const string BLOCK = "  aa bb cc --- aa bb aa   bb   cc";
		CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)\\s+(cc)"), "$1  $2  $3", new StringReader(BLOCK));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"aa", "bb", "cc", "---", "aa", "bb", "aa", "bb", "cc"}, new int[] {2, 6, 9, 11, 15, 18, 21, 25, 29}, new int[] {4, 8, 10, 14, 17, 20, 23, 27, 33}, BLOCK.Length);
	  }

	  //           11111111112222222222333333333
	  // 012345678901234567890123456789012345678
	  //   aa bb cc --- aa bb aa. bb aa   bb cc
	  //   aa##bb cc --- aa##bb aa. bb aa##bb cc

	  //   aa bb cc --- aa bbbaa. bb aa   b cc

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test2blocksMultiMatches() throws java.io.IOException
	  public virtual void test2blocksMultiMatches()
	  {
		const string BLOCK = "  aa bb cc --- aa bb aa. bb aa   bb cc";

		CharFilter cs = new PatternReplaceCharFilter(pattern("(aa)\\s+(bb)"), "$1##$2", new StringReader(BLOCK));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"aa##bb", "cc", "---", "aa##bb", "aa.", "bb", "aa##bb", "cc"}, new int[] {2, 8, 11, 15, 21, 25, 28, 36}, new int[] {7, 10, 14, 20, 24, 27, 35, 38}, BLOCK.Length);
	  }

	  //           11111111112222222222333333333
	  // 012345678901234567890123456789012345678
	  //  a bb - ccc . --- bb a . ccc ccc bb
	  //  aa b - c . --- b aa . c c b
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testChain() throws java.io.IOException
	  public virtual void testChain()
	  {
		const string BLOCK = " a bb - ccc . --- bb a . ccc ccc bb";
		CharFilter cs = new PatternReplaceCharFilter(pattern("a"), "aa", new StringReader(BLOCK));
		cs = new PatternReplaceCharFilter(pattern("bb"), "b", cs);
		cs = new PatternReplaceCharFilter(pattern("ccc"), "c", cs);
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"aa", "b", "-", "c", ".", "---", "b", "aa", ".", "c", "c", "b"}, new int[] {1, 3, 6, 8, 12, 14, 18, 21, 23, 25, 29, 33}, new int[] {2, 5, 7, 11, 13, 17, 20, 22, 24, 28, 32, 35}, BLOCK.Length);
	  }

	  private Pattern pattern(string p)
	  {
		return Pattern.compile(p);
	  }

	  /// <summary>
	  /// A demonstration of how backtracking regular expressions can lead to relatively 
	  /// easy DoS attacks.
	  /// </summary>
	  /// <seealso cref= "http://swtch.com/~rsc/regexp/regexp1.html" </seealso>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore public void testNastyPattern() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testNastyPattern()
	  {
		Pattern p = Pattern.compile("(c.+)*xy");
		string input = "[;<!--aecbbaa--><    febcfdc fbb = \"fbeeebff\" fc = dd   >\\';<eefceceaa e= babae\" eacbaff =\"fcfaccacd\" = bcced>>><  bccaafe edb = ecfccdff\"   <?</script><    edbd ebbcd=\"faacfcc\" aeca= bedbc ceeaac =adeafde aadccdaf = \"afcc ffda=aafbe &#x16921ed5\"1843785582']";
		for (int i = 0; i < input.Length; i++)
		{
		  Matcher matcher = p.matcher(input.Substring(0, i));
		  long t = DateTimeHelperClass.CurrentUnixTimeMillis();
		  if (matcher.find())
		  {
			Console.WriteLine(matcher.group());
		  }
		  Console.WriteLine(i + " > " + (DateTimeHelperClass.CurrentUnixTimeMillis() - t) / 1000.0);
		}
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		int numPatterns = 10 + random().Next(20);
		Random random = new Random(random().nextLong());
		for (int i = 0; i < numPatterns; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.regex.Pattern p = org.apache.lucene.util.TestUtil.randomPattern(random());
		  Pattern p = TestUtil.randomPattern(random());

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String replacement = org.apache.lucene.util.TestUtil.randomSimpleString(random);
		  string replacement = TestUtil.randomSimpleString(random);
		  Analyzer a = new AnalyzerAnonymousInnerClassHelper(this, p, replacement);

		  /* max input length. don't make it longer -- exponential processing
		   * time for certain patterns. */ 
		  const int maxInputLength = 30;
		  /* ASCII only input?: */
		  const bool asciiOnly = true;
		  checkRandomData(random, a, 250 * RANDOM_MULTIPLIER, maxInputLength, asciiOnly);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestPatternReplaceCharFilter outerInstance;

		  private Pattern p;
		  private string replacement;

		  public AnalyzerAnonymousInnerClassHelper(TestPatternReplaceCharFilter outerInstance, Pattern p, string replacement)
		  {
			  this.outerInstance = outerInstance;
			  this.p = p;
			  this.replacement = replacement;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }

		  protected internal override Reader initReader(string fieldName, Reader reader)
		  {
			return new PatternReplaceCharFilter(p, replacement, reader);
		  }
	  }
	}

}