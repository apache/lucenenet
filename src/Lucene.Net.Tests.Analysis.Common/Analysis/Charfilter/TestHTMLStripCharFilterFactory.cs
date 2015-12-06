namespace org.apache.lucene.analysis.charfilter
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


	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;

	/// <summary>
	/// Simple tests to ensure this factory is working
	/// </summary>
	public class TestHTMLStripCharFilterFactory : BaseTokenStreamFactoryTestCase
	{


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNothingChanged() throws Exception
	  public virtual void testNothingChanged()
	  {
		//                             11111111112
		//                   012345678901234567890
		const string text = "this is only a test.";
		Reader cs = charFilterFactory("HTMLStrip", "escapedTags", "a, Title").create(new StringReader(text));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"this", "is", "only", "a", "test."}, new int[] {0, 5, 8, 13, 15}, new int[] {4, 7, 12, 14, 20});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoEscapedTags() throws Exception
	  public virtual void testNoEscapedTags()
	  {
		//                             11111111112222222222333333333344
		//                   012345678901234567890123456789012345678901
		const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
		Reader cs = charFilterFactory("HTMLStrip").create(new StringReader(text));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"this", "is", "only", "a", "test."}, new int[] {3, 12, 18, 27, 32}, new int[] {11, 14, 26, 28, 41});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEscapedTags() throws Exception
	  public virtual void testEscapedTags()
	  {
		//                             11111111112222222222333333333344
		//                   012345678901234567890123456789012345678901
		const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
		Reader cs = charFilterFactory("HTMLStrip", "escapedTags", "U i").create(new StringReader(text));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"<u>this</u>", "is", "only", "a", "<I>test</I>."}, new int[] {0, 12, 18, 27, 29}, new int[] {11, 14, 26, 28, 41});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSeparatorOnlyEscapedTags() throws Exception
	  public virtual void testSeparatorOnlyEscapedTags()
	  {
		//                             11111111112222222222333333333344
		//                   012345678901234567890123456789012345678901
		const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
		Reader cs = charFilterFactory("HTMLStrip", "escapedTags", ",, , ").create(new StringReader(text));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"this", "is", "only", "a", "test."}, new int[] {3, 12, 18, 27, 32}, new int[] {11, 14, 26, 28, 41});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyEscapedTags() throws Exception
	  public virtual void testEmptyEscapedTags()
	  {
		//                             11111111112222222222333333333344
		//                   012345678901234567890123456789012345678901
		const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
		Reader cs = charFilterFactory("HTMLStrip", "escapedTags", "").create(new StringReader(text));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"this", "is", "only", "a", "test."}, new int[] {3, 12, 18, 27, 32}, new int[] {11, 14, 26, 28, 41});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSingleEscapedTag() throws Exception
	  public virtual void testSingleEscapedTag()
	  {
		//                             11111111112222222222333333333344
		//                   012345678901234567890123456789012345678901
		const string text = "<u>this</u> is <b>only</b> a <I>test</I>.";
		Reader cs = charFilterFactory("HTMLStrip", "escapedTags", ", B\r\n\t").create(new StringReader(text));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[] {"this", "is", "<b>only</b>", "a", "test."}, new int[] {3, 12, 15, 27, 32}, new int[] {11, 14, 26, 28, 41});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  charFilterFactory("HTMLStrip", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}