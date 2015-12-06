using System;

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


	public class TestLimitTokenCountFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws Exception
	  public virtual void test()
	  {
		foreach (bool consumeAll in new bool[]{true, false})
		{
		  Reader reader = new StringReader("A1 B2 C3 D4 E5 F6");
		  MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		  tokenizer.EnableChecks = consumeAll;
		  TokenStream stream = tokenizer;
		  stream = tokenFilterFactory("LimitTokenCount", LimitTokenCountFilterFactory.MAX_TOKEN_COUNT_KEY, "3", LimitTokenCountFilterFactory.CONSUME_ALL_TOKENS_KEY, Convert.ToString(consumeAll)).create(stream);
		  assertTokenStreamContents(stream, new string[]{"A1", "B2", "C3"});
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRequired() throws Exception
	  public virtual void testRequired()
	  {
		// param is required
		try
		{
		  tokenFilterFactory("LimitTokenCount");
		  fail();
		}
		catch (System.ArgumentException e)
		{
		  assertTrue("exception doesn't mention param: " + e.Message, 0 < e.Message.indexOf(LimitTokenCountFilterFactory.MAX_TOKEN_COUNT_KEY));
		}
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
		  tokenFilterFactory("LimitTokenCount", LimitTokenCountFilterFactory.MAX_TOKEN_COUNT_KEY, "3", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}