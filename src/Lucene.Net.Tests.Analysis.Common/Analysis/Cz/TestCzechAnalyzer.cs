using System;

namespace org.apache.lucene.analysis.cz
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

	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Test the CzechAnalyzer
	/// 
	/// Before Lucene 3.1, CzechAnalyzer was a StandardAnalyzer with a custom 
	/// stopword list. As of 3.1 it also includes a stemmer.
	/// 
	/// </summary>
	public class TestCzechAnalyzer : BaseTokenStreamTestCase
	{
	  /// @deprecated (3.1) Remove this test when support for 3.0 indexes is no longer needed. 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) Remove this test when support for 3.0 indexes is no longer needed.") public void testStopWordLegacy() throws Exception
	  [Obsolete("(3.1) Remove this test when support for 3.0 indexes is no longer needed.")]
	  public virtual void testStopWordLegacy()
	  {
		assertAnalyzesTo(new CzechAnalyzer(Version.LUCENE_30), "Pokud mluvime o volnem", new string[] {"mluvime", "volnem"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopWord() throws Exception
	  public virtual void testStopWord()
	  {
		assertAnalyzesTo(new CzechAnalyzer(TEST_VERSION_CURRENT), "Pokud mluvime o volnem", new string[] {"mluvim", "voln"});
	  }

	  /// @deprecated (3.1) Remove this test when support for 3.0 indexes is no longer needed. 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) Remove this test when support for 3.0 indexes is no longer needed.") public void testReusableTokenStreamLegacy() throws Exception
	  [Obsolete("(3.1) Remove this test when support for 3.0 indexes is no longer needed.")]
	  public virtual void testReusableTokenStreamLegacy()
	  {
		Analyzer analyzer = new CzechAnalyzer(Version.LUCENE_30);
		assertAnalyzesTo(analyzer, "Pokud mluvime o volnem", new string[] {"mluvime", "volnem"});
		assertAnalyzesTo(analyzer, "Česká Republika", new string[] {"česká", "republika"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		Analyzer analyzer = new CzechAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(analyzer, "Pokud mluvime o volnem", new string[] {"mluvim", "voln"});
		assertAnalyzesTo(analyzer, "Česká Republika", new string[] {"česk", "republik"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithStemExclusionSet() throws java.io.IOException
	  public virtual void testWithStemExclusionSet()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("hole");
		CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, set);
		assertAnalyzesTo(cz, "hole desek", new string[] {"hole", "desk"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new CzechAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}