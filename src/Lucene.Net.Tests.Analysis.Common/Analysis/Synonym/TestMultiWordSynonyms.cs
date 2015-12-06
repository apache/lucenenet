using System;
using System.Collections.Generic;

namespace org.apache.lucene.analysis.synonym
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
	using StringMockResourceLoader = org.apache.lucene.analysis.util.StringMockResourceLoader;


	/// <summary>
	/// @since solr 1.4
	/// </summary>
	public class TestMultiWordSynonyms : BaseTokenStreamFactoryTestCase
	{

	  /// @deprecated Remove this test in 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("Remove this test in 5.0") public void testMultiWordSynonymsOld() throws java.io.IOException
	  [Obsolete("Remove this test in 5.0")]
	  public virtual void testMultiWordSynonymsOld()
	  {
		IList<string> rules = new List<string>();
		rules.Add("a b c,d");
		SlowSynonymMap synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);

		SlowSynonymFilter ts = new SlowSynonymFilter(new MockTokenizer(new StringReader("a e"), MockTokenizer.WHITESPACE, false), synMap);
		// This fails because ["e","e"] is the value of the token stream
		assertTokenStreamContents(ts, new string[] {"a", "e"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMultiWordSynonyms() throws Exception
	  public virtual void testMultiWordSynonyms()
	  {
		Reader reader = new StringReader("a e");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Synonym", TEST_VERSION_CURRENT, new StringMockResourceLoader("a b c,d"), "synonyms", "synonyms.txt").create(stream);
		// This fails because ["e","e"] is the value of the token stream
		assertTokenStreamContents(stream, new string[] {"a", "e"});
	  }
	}

}