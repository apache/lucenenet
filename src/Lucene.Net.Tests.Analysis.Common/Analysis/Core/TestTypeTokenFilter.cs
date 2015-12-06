using System;
using System.Collections.Generic;
using System.Text;

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

	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using English = org.apache.lucene.util.English;
	using Version = org.apache.lucene.util.Version;



	public class TestTypeTokenFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTypeFilter() throws java.io.IOException
	  public virtual void testTypeFilter()
	  {
		StringReader reader = new StringReader("121 is palindrome, while 123 is not");
		ISet<string> stopTypes = asSet("<NUM>");
		TokenStream stream = new TypeTokenFilter(TEST_VERSION_CURRENT, true, new StandardTokenizer(TEST_VERSION_CURRENT, reader), stopTypes);
		assertTokenStreamContents(stream, new string[]{"is", "palindrome", "while", "is", "not"});
	  }

	  /// <summary>
	  /// Test Position increments applied by TypeTokenFilter with and without enabling this option.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopPositons() throws java.io.IOException
	  public virtual void testStopPositons()
	  {
		StringBuilder sb = new StringBuilder();
		for (int i = 10; i < 20; i++)
		{
		  if (i % 3 != 0)
		  {
			sb.Append(i).Append(" ");
		  }
		  else
		  {
			string w = English.intToEnglish(i).trim();
			sb.Append(w).Append(" ");
		  }
		}
		log(sb.ToString());
		string[] stopTypes = new string[]{"<NUM>"};
		ISet<string> stopSet = asSet(stopTypes);

		// with increments
		StringReader reader = new StringReader(sb.ToString());
		TypeTokenFilter typeTokenFilter = new TypeTokenFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, reader), stopSet);
		testPositons(typeTokenFilter);

		// without increments
		reader = new StringReader(sb.ToString());
		typeTokenFilter = new TypeTokenFilter(Version.LUCENE_43, false, new StandardTokenizer(TEST_VERSION_CURRENT, reader), stopSet);
		testPositons(typeTokenFilter);

	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void testPositons(TypeTokenFilter stpf) throws java.io.IOException
	  private void testPositons(TypeTokenFilter stpf)
	  {
		TypeAttribute typeAtt = stpf.getAttribute(typeof(TypeAttribute));
		CharTermAttribute termAttribute = stpf.getAttribute(typeof(CharTermAttribute));
		PositionIncrementAttribute posIncrAtt = stpf.getAttribute(typeof(PositionIncrementAttribute));
		stpf.reset();
		bool enablePositionIncrements = stpf.EnablePositionIncrements;
		while (stpf.incrementToken())
		{
		  log("Token: " + termAttribute.ToString() + ": " + typeAtt.type() + " - " + posIncrAtt.PositionIncrement);
		  assertEquals("if position increment is enabled the positionIncrementAttribute value should be 3, otherwise 1", posIncrAtt.PositionIncrement, enablePositionIncrements ? 3 : 1);
		}
		stpf.end();
		stpf.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTypeFilterWhitelist() throws java.io.IOException
	  public virtual void testTypeFilterWhitelist()
	  {
		StringReader reader = new StringReader("121 is palindrome, while 123 is not");
		ISet<string> stopTypes = Collections.singleton("<NUM>");
		TokenStream stream = new TypeTokenFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, reader), stopTypes, true);
		assertTokenStreamContents(stream, new string[]{"121", "123"});
	  }

	  // print debug info depending on VERBOSE
	  private static void log(string s)
	  {
		if (VERBOSE)
		{
		  Console.WriteLine(s);
		}
	  }
	}

}