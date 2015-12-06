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

	public class TestCodepointCountFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPositionIncrements() throws Exception
	  public virtual void testPositionIncrements()
	  {
		Reader reader = new StringReader("foo foobar super-duper-trooper");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("CodepointCount", "min", "4", "max", "10").create(stream);
		assertTokenStreamContents(stream, new string[] {"foobar"}, new int[] {2});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("CodepointCount", "min", "4", "max", "5", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }

	  /// <summary>
	  /// Test that invalid arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidArguments() throws Exception
	  public virtual void testInvalidArguments()
	  {
		try
		{
		  Reader reader = new StringReader("foo foobar super-duper-trooper");
		  TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		  tokenFilterFactory("CodepointCount", CodepointCountFilterFactory.MIN_KEY, "5", CodepointCountFilterFactory.MAX_KEY, "4").create(stream);
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("maximum length must not be greater than minimum length"));
		}
	  }
	}
}