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
	/// Simple tests to ensure the apostrophe filter factory is working.
	/// </summary>
	public class TestApostropheFilterFactory : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Ensure the filter actually removes characters after an apostrophe.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testApostrophes() throws Exception
	  public virtual void testApostrophes()
	  {
		Reader reader = new StringReader("Türkiye'de 2003'te Van Gölü'nü gördüm");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Apostrophe").create(stream);
		assertTokenStreamContents(stream, new string[]{"Türkiye", "2003", "Van", "Gölü", "gördüm"});
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
		  tokenFilterFactory("Apostrophe", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameter(s):"));
		}
	  }
	}

}