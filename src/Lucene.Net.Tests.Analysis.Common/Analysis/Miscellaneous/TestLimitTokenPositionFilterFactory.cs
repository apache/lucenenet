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


	public class TestLimitTokenPositionFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxPosition1() throws Exception
	  public virtual void testMaxPosition1()
	  {
		foreach (bool consumeAll in new bool[]{true, false})
		{
		  Reader reader = new StringReader("A1 B2 C3 D4 E5 F6");
		  MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		  // if we are consuming all tokens, we can use the checks, otherwise we can't
		  tokenizer.EnableChecks = consumeAll;
		  TokenStream stream = tokenizer;
		  stream = tokenFilterFactory("LimitTokenPosition", LimitTokenPositionFilterFactory.MAX_TOKEN_POSITION_KEY, "1", LimitTokenPositionFilterFactory.CONSUME_ALL_TOKENS_KEY, Convert.ToString(consumeAll)).create(stream);
		  assertTokenStreamContents(stream, new string[]{"A1"});
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMissingParam() throws Exception
	  public virtual void testMissingParam()
	  {
		try
		{
		  tokenFilterFactory("LimitTokenPosition");
		  fail();
		}
		catch (System.ArgumentException e)
		{
		  assertTrue("exception doesn't mention param: " + e.Message, 0 < e.Message.indexOf(LimitTokenPositionFilterFactory.MAX_TOKEN_POSITION_KEY));
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxPosition1WithShingles() throws Exception
	  public virtual void testMaxPosition1WithShingles()
	  {
		foreach (bool consumeAll in new bool[]{true, false})
		{
		  Reader reader = new StringReader("one two three four five");
		  MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		  // if we are consuming all tokens, we can use the checks, otherwise we can't
		  tokenizer.EnableChecks = consumeAll;
		  TokenStream stream = tokenizer;
		  stream = tokenFilterFactory("Shingle", "minShingleSize", "2", "maxShingleSize", "3", "outputUnigrams", "true").create(stream);
		  stream = tokenFilterFactory("LimitTokenPosition", LimitTokenPositionFilterFactory.MAX_TOKEN_POSITION_KEY, "1", LimitTokenPositionFilterFactory.CONSUME_ALL_TOKENS_KEY, Convert.ToString(consumeAll)).create(stream);
		  assertTokenStreamContents(stream, new string[]{"one", "one two", "one two three"});
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testConsumeAllTokens() throws Exception
	  public virtual void testConsumeAllTokens()
	  {
		Reader reader = new StringReader("A1 B2 C3 D4 E5 F6");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("LimitTokenPosition", "maxTokenPosition", "3", "consumeAllTokens", "true").create(stream);
		assertTokenStreamContents(stream, new string[] {"A1", "B2", "C3"});
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
		  tokenFilterFactory("LimitTokenPosition", "maxTokenPosition", "3", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}