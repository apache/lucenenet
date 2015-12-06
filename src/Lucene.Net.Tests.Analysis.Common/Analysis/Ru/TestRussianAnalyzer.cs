using System;

namespace org.apache.lucene.analysis.ru
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
	/// Test case for RussianAnalyzer.
	/// </summary>

	public class TestRussianAnalyzer : BaseTokenStreamTestCase
	{

		 /// <summary>
		 /// Check that RussianAnalyzer doesnt discard any numbers </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDigitsInRussianCharset() throws java.io.IOException
		public virtual void testDigitsInRussianCharset()
		{
		  RussianAnalyzer ra = new RussianAnalyzer(TEST_VERSION_CURRENT);
		  assertAnalyzesTo(ra, "text 1000", new string[] {"text", "1000"});
		}

		/// @deprecated (3.1) remove this test in Lucene 5.0: stopwords changed 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) remove this test in Lucene 5.0: stopwords changed") public void testReusableTokenStream30() throws Exception
		[Obsolete("(3.1) remove this test in Lucene 5.0: stopwords changed")]
		public virtual void testReusableTokenStream30()
		{
		  Analyzer a = new RussianAnalyzer(Version.LUCENE_30);
		  assertAnalyzesTo(a, "Вместе с тем о силе электромагнитной энергии имели представление еще", new string[] {"вмест", "сил", "электромагнитн", "энерг", "имел", "представлен"});
		  assertAnalyzesTo(a, "Но знание это хранилось в тайне", new string[] {"знан", "хран", "тайн"});
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
		public virtual void testReusableTokenStream()
		{
		  Analyzer a = new RussianAnalyzer(TEST_VERSION_CURRENT);
		  assertAnalyzesTo(a, "Вместе с тем о силе электромагнитной энергии имели представление еще", new string[] {"вмест", "сил", "электромагнитн", "энерг", "имел", "представлен"});
		  assertAnalyzesTo(a, "Но знание это хранилось в тайне", new string[] {"знан", "эт", "хран", "тайн"});
		}


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWithStemExclusionSet() throws Exception
		public virtual void testWithStemExclusionSet()
		{
		  CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		  set.add("представление");
		  Analyzer a = new RussianAnalyzer(TEST_VERSION_CURRENT, RussianAnalyzer.DefaultStopSet, set);
		  assertAnalyzesTo(a, "Вместе с тем о силе электромагнитной энергии имели представление еще", new string[] {"вмест", "сил", "электромагнитн", "энерг", "имел", "представление"});

		}

		/// <summary>
		/// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
		public virtual void testRandomStrings()
		{
		  checkRandomData(random(), new RussianAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
		}
	}

}