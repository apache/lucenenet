namespace org.apache.lucene.analysis.tr
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
	/// Simple tests to ensure the Turkish lowercase filter factory is working.
	/// </summary>
	public class TestTurkishLowerCaseFilterFactory : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Ensure the filter actually lowercases text.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCasing() throws Exception
	  public virtual void testCasing()
	  {
		Reader reader = new StringReader("AĞACI");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("TurkishLowerCase").create(stream);
		assertTokenStreamContents(stream, new string[] {"ağacı"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("TurkishLowerCase", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}