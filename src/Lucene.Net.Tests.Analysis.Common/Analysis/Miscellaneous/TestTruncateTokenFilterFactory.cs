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

	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;


	/// <summary>
	/// Simple tests to ensure the simple truncation filter factory is working.
	/// </summary>
	public class TestTruncateTokenFilterFactory : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Ensure the filter actually truncates text.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTruncating() throws Exception
	  public virtual void testTruncating()
	  {
		Reader reader = new StringReader("abcdefg 1234567 ABCDEFG abcde abc 12345 123");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Truncate", TruncateTokenFilterFactory.PREFIX_LENGTH_KEY, "5").create(stream);
		assertTokenStreamContents(stream, new string[]{"abcde", "12345", "ABCDE", "abcde", "abc", "12345", "123"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("Truncate", TruncateTokenFilterFactory.PREFIX_LENGTH_KEY, "5", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameter(s):"));
		}
	  }

	  /// <summary>
	  /// Test that negative prefix length result in exception
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNonPositivePrefixLengthArgument() throws Exception
	  public virtual void testNonPositivePrefixLengthArgument()
	  {
		try
		{
		  tokenFilterFactory("Truncate", TruncateTokenFilterFactory.PREFIX_LENGTH_KEY, "-5");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains(TruncateTokenFilterFactory.PREFIX_LENGTH_KEY + " parameter must be a positive number: -5"));
		}
	  }
	}



}