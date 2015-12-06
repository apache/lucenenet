using System.Collections.Generic;

namespace org.apache.lucene.analysis.core
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
	using TokenFilterFactory = org.apache.lucene.analysis.util.TokenFilterFactory;

	/// <summary>
	/// Testcase for <seealso cref="TypeTokenFilterFactory"/>
	/// </summary>
	public class TestTypeTokenFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInform() throws Exception
	  public virtual void testInform()
	  {
		TypeTokenFilterFactory factory = (TypeTokenFilterFactory) tokenFilterFactory("Type", "types", "stoptypes-1.txt", "enablePositionIncrements", "true");
		ISet<string> types = factory.StopTypes;
		assertTrue("types is null and it shouldn't be", types != null);
		assertTrue("types Size: " + types.Count + " is not: " + 2, types.Count == 2);
		assertTrue("enablePositionIncrements was set to true but not correctly parsed", factory.EnablePositionIncrements);

		factory = (TypeTokenFilterFactory) tokenFilterFactory("Type", "types", "stoptypes-1.txt, stoptypes-2.txt", "enablePositionIncrements", "false", "useWhitelist", "true");
		types = factory.StopTypes;
		assertTrue("types is null and it shouldn't be", types != null);
		assertTrue("types Size: " + types.Count + " is not: " + 4, types.Count == 4);
		assertTrue("enablePositionIncrements was set to false but not correctly parsed", !factory.EnablePositionIncrements);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCreationWithBlackList() throws Exception
	  public virtual void testCreationWithBlackList()
	  {
		TokenFilterFactory factory = tokenFilterFactory("Type", "types", "stoptypes-1.txt, stoptypes-2.txt", "enablePositionIncrements", "true");
		NumericTokenStream input = new NumericTokenStream();
		input.IntValue = 123;
		factory.create(input);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCreationWithWhiteList() throws Exception
	  public virtual void testCreationWithWhiteList()
	  {
		TokenFilterFactory factory = tokenFilterFactory("Type", "types", "stoptypes-1.txt, stoptypes-2.txt", "enablePositionIncrements", "true", "useWhitelist", "true");
		NumericTokenStream input = new NumericTokenStream();
		input.IntValue = 123;
		factory.create(input);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMissingTypesParameter() throws Exception
	  public virtual void testMissingTypesParameter()
	  {
		try
		{
		  tokenFilterFactory("Type", "enablePositionIncrements", "false");
		  fail("not supplying 'types' parameter should cause an IllegalArgumentException");
		}
		catch (System.ArgumentException)
		{
		  // everything ok
		}
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("Type", "types", "stoptypes-1.txt", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}