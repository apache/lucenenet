namespace org.apache.lucene.analysis.th
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
	/// Simple tests to ensure the Thai word filter factory is working.
	/// </summary>
	public class TestThaiTokenizerFactory : BaseTokenStreamFactoryTestCase
	{
	  /// <summary>
	  /// Ensure the filter actually decomposes text.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWordBreak() throws Exception
	  public virtual void testWordBreak()
	  {
		assumeTrue("JRE does not support Thai dictionary-based BreakIterator", ThaiTokenizer.DBBI_AVAILABLE);
		Tokenizer tokenizer = tokenizerFactory("Thai").create(new StringReader("การที่ได้ต้องแสดงว่างานดี"));
		assertTokenStreamContents(tokenizer, new string[] {"การ", "ที่", "ได้", "ต้อง", "แสดง", "ว่า", "งาน", "ดี"});
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		assumeTrue("JRE does not support Thai dictionary-based BreakIterator", ThaiTokenizer.DBBI_AVAILABLE);
		try
		{
		  tokenizerFactory("Thai", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}

}