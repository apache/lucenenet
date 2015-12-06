using System;

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

namespace org.apache.lucene.analysis.reverse
{


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using Version = org.apache.lucene.util.Version;

	public class TestReverseStringFilter : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFilter() throws Exception
	  public virtual void testFilter()
	  {
		TokenStream stream = new MockTokenizer(new StringReader("Do have a nice day"), MockTokenizer.WHITESPACE, false); // 1-4 length string
		ReverseStringFilter filter = new ReverseStringFilter(TEST_VERSION_CURRENT, stream);
		assertTokenStreamContents(filter, new string[] {"oD", "evah", "a", "ecin", "yad"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFilterWithMark() throws Exception
	  public virtual void testFilterWithMark()
	  {
		TokenStream stream = new MockTokenizer(new StringReader("Do have a nice day"), MockTokenizer.WHITESPACE, false); // 1-4 length string
		ReverseStringFilter filter = new ReverseStringFilter(TEST_VERSION_CURRENT, stream, '\u0001');
		assertTokenStreamContents(filter, new string[] {"\u0001oD", "\u0001evah", "\u0001a", "\u0001ecin", "\u0001yad"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReverseString() throws Exception
	  public virtual void testReverseString()
	  {
		assertEquals("A", ReverseStringFilter.reverse(TEST_VERSION_CURRENT, "A"));
		assertEquals("BA", ReverseStringFilter.reverse(TEST_VERSION_CURRENT, "AB"));
		assertEquals("CBA", ReverseStringFilter.reverse(TEST_VERSION_CURRENT, "ABC"));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReverseChar() throws Exception
	  public virtual void testReverseChar()
	  {
		char[] buffer = new char[] {'A', 'B', 'C', 'D', 'E', 'F'};
		ReverseStringFilter.reverse(TEST_VERSION_CURRENT, buffer, 2, 3);
		assertEquals("ABEDCF", new string(buffer));
	  }

	  /// <summary>
	  /// Test the broken 3.0 behavior, for back compat </summary>
	  /// @deprecated (3.1) Remove in Lucene 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) Remove in Lucene 5.0") public void testBackCompat() throws Exception
	  [Obsolete("(3.1) Remove in Lucene 5.0")]
	  public virtual void testBackCompat()
	  {
		assertEquals("\uDF05\uD866\uDF05\uD866", ReverseStringFilter.reverse(Version.LUCENE_30, "𩬅𩬅"));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReverseSupplementary() throws Exception
	  public virtual void testReverseSupplementary()
	  {
		// supplementary at end
		assertEquals("𩬅艱鍟䇹愯瀛", ReverseStringFilter.reverse(TEST_VERSION_CURRENT, "瀛愯䇹鍟艱𩬅"));
		// supplementary at end - 1
		assertEquals("a𩬅艱鍟䇹愯瀛", ReverseStringFilter.reverse(TEST_VERSION_CURRENT, "瀛愯䇹鍟艱𩬅a"));
		// supplementary at start
		assertEquals("fedcba𩬅", ReverseStringFilter.reverse(TEST_VERSION_CURRENT, "𩬅abcdef"));
		// supplementary at start + 1
		assertEquals("fedcba𩬅z", ReverseStringFilter.reverse(TEST_VERSION_CURRENT, "z𩬅abcdef"));
		// supplementary medial
		assertEquals("gfe𩬅dcba", ReverseStringFilter.reverse(TEST_VERSION_CURRENT, "abcd𩬅efg"));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReverseSupplementaryChar() throws Exception
	  public virtual void testReverseSupplementaryChar()
	  {
		// supplementary at end
		char[] buffer = "abc瀛愯䇹鍟艱𩬅".ToCharArray();
		ReverseStringFilter.reverse(TEST_VERSION_CURRENT, buffer, 3, 7);
		assertEquals("abc𩬅艱鍟䇹愯瀛", new string(buffer));
		// supplementary at end - 1
		buffer = "abc瀛愯䇹鍟艱𩬅d".ToCharArray();
		ReverseStringFilter.reverse(TEST_VERSION_CURRENT, buffer, 3, 8);
		assertEquals("abcd𩬅艱鍟䇹愯瀛", new string(buffer));
		// supplementary at start
		buffer = "abc𩬅瀛愯䇹鍟艱".ToCharArray();
		ReverseStringFilter.reverse(TEST_VERSION_CURRENT, buffer, 3, 7);
		assertEquals("abc艱鍟䇹愯瀛𩬅", new string(buffer));
		// supplementary at start + 1
		buffer = "abcd𩬅瀛愯䇹鍟艱".ToCharArray();
		ReverseStringFilter.reverse(TEST_VERSION_CURRENT, buffer, 3, 8);
		assertEquals("abc艱鍟䇹愯瀛𩬅d", new string(buffer));
		// supplementary medial
		buffer = "abc瀛愯𩬅def".ToCharArray();
		ReverseStringFilter.reverse(TEST_VERSION_CURRENT, buffer, 3, 7);
		assertEquals("abcfed𩬅愯瀛", new string(buffer));
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestReverseStringFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestReverseStringFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new ReverseStringFilter(TEST_VERSION_CURRENT, tokenizer));
		  }
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
		  private readonly TestReverseStringFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestReverseStringFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new ReverseStringFilter(TEST_VERSION_CURRENT, tokenizer));
		  }
	  }
	}

}