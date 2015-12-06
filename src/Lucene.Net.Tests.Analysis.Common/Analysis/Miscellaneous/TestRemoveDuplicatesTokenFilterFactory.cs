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
	/// Simple tests to ensure this factory is working </summary>
	public class TestRemoveDuplicatesTokenFilterFactory : BaseTokenStreamFactoryTestCase
	{

	  public static Token tok(int pos, string t, int start, int end)
	  {
		Token tok = new Token(t,start,end);
		tok.PositionIncrement = pos;
		return tok;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDups(final String expected, final org.apache.lucene.analysis.Token... tokens) throws Exception
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public virtual void testDups(string expected, params Token[] tokens)
	  {
		TokenStream stream = new CannedTokenStream(tokens);
		stream = tokenFilterFactory("RemoveDuplicates").create(stream);
		assertTokenStreamContents(stream, expected.Split("\\s", true));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSimpleDups() throws Exception
	  public virtual void testSimpleDups()
	  {
		testDups("A B C D E",tok(1,"A", 0, 4),tok(1,"B", 5, 10),tok(0,"B",11, 15),tok(1,"C",16, 20),tok(0,"D",16, 20),tok(1,"E",21, 25));
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("RemoveDuplicates", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}