namespace org.apache.lucene.analysis.miscellaneous
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


	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using TestUtil = org.apache.lucene.util.TestUtil;
	using Test = org.junit.Test;

	public class TestCodepointCountFilter : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFilterWithPosIncr() throws Exception
	  public virtual void testFilterWithPosIncr()
	  {
		TokenStream stream = new MockTokenizer(new StringReader("short toolong evenmuchlongertext a ab toolong foo"), MockTokenizer.WHITESPACE, false);
		CodepointCountFilter filter = new CodepointCountFilter(TEST_VERSION_CURRENT, stream, 2, 6);
		assertTokenStreamContents(filter, new string[]{"short", "ab", "foo"}, new int[]{1, 4, 2});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestCodepointCountFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestCodepointCountFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new CodepointCountFilter(TEST_VERSION_CURRENT, tokenizer, 0, 5));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws java.io.IOException
	  public virtual void testRandomStrings()
	  {
		for (int i = 0; i < 10000; i++)
		{
		  string text = TestUtil.randomUnicodeString(random(), 100);
		  int min = TestUtil.Next(random(), 0, 100);
		  int max = TestUtil.Next(random(), 0, 100);
		  int count = text.codePointCount(0, text.Length);
		  if (min > max)
		  {
			int temp = min;
			min = max;
			max = temp;
		  }
		  bool expected = count >= min && count <= max;
		  TokenStream stream = new KeywordTokenizer(new StringReader(text));
		  stream = new CodepointCountFilter(TEST_VERSION_CURRENT, stream, min, max);
		  stream.reset();
		  assertEquals(expected, stream.incrementToken());
		  stream.end();
		  stream.close();
		}
	  }

	  /// <summary>
	  /// checking the validity of constructor arguments
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testIllegalArguments() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testIllegalArguments()
	  {
		new CodepointCountFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("accept only valid arguments"), MockTokenizer.WHITESPACE, false), 4, 1);
	  }
	}

}