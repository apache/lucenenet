namespace org.apache.lucene.analysis.no
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
	/// Simple tests to ensure the Norwegian Minimal stem factory is working.
	/// </summary>
	public class TestNorwegianMinimalStemFilterFactory : BaseTokenStreamFactoryTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStemming() throws Exception
	  public virtual void testStemming()
	  {
		Reader reader = new StringReader("eple eplet epler eplene eplets eplenes");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("NorwegianMinimalStem").create(stream);
		assertTokenStreamContents(stream, new string[] {"epl", "epl", "epl", "epl", "epl", "epl"});
	  }

	  /// <summary>
	  /// Test stemming with variant set explicitly to Bokmål </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBokmaalStemming() throws Exception
	  public virtual void testBokmaalStemming()
	  {
		Reader reader = new StringReader("eple eplet epler eplene eplets eplenes");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("NorwegianMinimalStem", "variant", "nb").create(stream);
		assertTokenStreamContents(stream, new string[] {"epl", "epl", "epl", "epl", "epl", "epl"});
	  }

	  /// <summary>
	  /// Test stemming with variant set explicitly to Nynorsk </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNynorskStemming() throws Exception
	  public virtual void testNynorskStemming()
	  {
		Reader reader = new StringReader("gut guten gutar gutane gutens gutanes");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("NorwegianMinimalStem", "variant", "nn").create(stream);
		assertTokenStreamContents(stream, new string[] {"gut", "gut", "gut", "gut", "gut", "gut"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("NorwegianMinimalStem", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}