using System;

namespace org.apache.lucene.analysis.fr
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
	/// Test case for FrenchAnalyzer.
	/// 
	/// </summary>

	public class TestFrenchAnalyzer : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAnalyzer() throws Exception
	  public virtual void testAnalyzer()
	  {
		FrenchAnalyzer fa = new FrenchAnalyzer(TEST_VERSION_CURRENT);

		assertAnalyzesTo(fa, "", new string[] { });

		assertAnalyzesTo(fa, "chien chat cheval", new string[] {"chien", "chat", "cheval"});

		assertAnalyzesTo(fa, "chien CHAT CHEVAL", new string[] {"chien", "chat", "cheval"});

		assertAnalyzesTo(fa, "  chien  ,? + = -  CHAT /: > CHEVAL", new string[] {"chien", "chat", "cheval"});

		assertAnalyzesTo(fa, "chien++", new string[] {"chien"});

		assertAnalyzesTo(fa, "mot \"entreguillemet\"", new string[] {"mot", "entreguilemet"});

		// let's do some french specific tests now

		/* 1. couldn't resist
		 I would expect this to stay one term as in French the minus
		sign is often used for composing words */
		assertAnalyzesTo(fa, "Jean-François", new string[] {"jean", "francoi"});

		// 2. stopwords
		assertAnalyzesTo(fa, "le la chien les aux chat du des à cheval", new string[] {"chien", "chat", "cheval"});

		// some nouns and adjectives
		assertAnalyzesTo(fa, "lances chismes habitable chiste éléments captifs", new string[] {"lanc", "chism", "habitabl", "chist", "element", "captif"});

		// some verbs
		assertAnalyzesTo(fa, "finissions souffrirent rugissante", new string[] {"finision", "soufrirent", "rugisant"});

		// some everything else
		// aujourd'hui stays one term which is OK
		assertAnalyzesTo(fa, "C3PO aujourd'hui oeuf ïâöûàä anticonstitutionnellement Java++ ", new string[] {"c3po", "aujourd'hui", "oeuf", "ïaöuaä", "anticonstitutionel", "java"});

		// some more everything else
		// here 1940-1945 stays as one term, 1940:1945 not ?
		assertAnalyzesTo(fa, "33Bis 1940-1945 1940:1945 (---i+++)*", new string[] {"33bi", "1940", "1945", "1940", "1945", "i"});

	  }

	  /// @deprecated (3.1) remove this test for Lucene 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) remove this test for Lucene 5.0") public void testAnalyzer30() throws Exception
	  [Obsolete("(3.1) remove this test for Lucene 5.0")]
	  public virtual void testAnalyzer30()
	  {
		  FrenchAnalyzer fa = new FrenchAnalyzer(Version.LUCENE_30);

		  assertAnalyzesTo(fa, "", new string[] { });

		  assertAnalyzesTo(fa, "chien chat cheval", new string[] {"chien", "chat", "cheval"});

		  assertAnalyzesTo(fa, "chien CHAT CHEVAL", new string[] {"chien", "chat", "cheval"});

		  assertAnalyzesTo(fa, "  chien  ,? + = -  CHAT /: > CHEVAL", new string[] {"chien", "chat", "cheval"});

		  assertAnalyzesTo(fa, "chien++", new string[] {"chien"});

		  assertAnalyzesTo(fa, "mot \"entreguillemet\"", new string[] {"mot", "entreguillemet"});

		  // let's do some french specific tests now

		  /* 1. couldn't resist
		   I would expect this to stay one term as in French the minus
		  sign is often used for composing words */
		  assertAnalyzesTo(fa, "Jean-François", new string[] {"jean", "françois"});

		  // 2. stopwords
		  assertAnalyzesTo(fa, "le la chien les aux chat du des à cheval", new string[] {"chien", "chat", "cheval"});

		  // some nouns and adjectives
		  assertAnalyzesTo(fa, "lances chismes habitable chiste éléments captifs", new string[] {"lanc", "chism", "habit", "chist", "élément", "captif"});

		  // some verbs
		  assertAnalyzesTo(fa, "finissions souffrirent rugissante", new string[] {"fin", "souffr", "rug"});

		  // some everything else
		  // aujourd'hui stays one term which is OK
		  assertAnalyzesTo(fa, "C3PO aujourd'hui oeuf ïâöûàä anticonstitutionnellement Java++ ", new string[] {"c3po", "aujourd'hui", "oeuf", "ïâöûàä", "anticonstitutionnel", "jav"});

		  // some more everything else
		  // here 1940-1945 stays as one term, 1940:1945 not ?
		  assertAnalyzesTo(fa, "33Bis 1940-1945 1940:1945 (---i+++)*", new string[] {"33bis", "1940-1945", "1940", "1945", "i"});

	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		FrenchAnalyzer fa = new FrenchAnalyzer(TEST_VERSION_CURRENT);
		// stopwords
		  assertAnalyzesTo(fa, "le la chien les aux chat du des à cheval", new string[] {"chien", "chat", "cheval"});

		  // some nouns and adjectives
		  assertAnalyzesTo(fa, "lances chismes habitable chiste éléments captifs", new string[] {"lanc", "chism", "habitabl", "chist", "element", "captif"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExclusionTableViaCtor() throws Exception
	  public virtual void testExclusionTableViaCtor()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		set.add("habitable");
		FrenchAnalyzer fa = new FrenchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, set);
		assertAnalyzesTo(fa, "habitable chiste", new string[] {"habitable", "chist"});

		fa = new FrenchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET, set);
		assertAnalyzesTo(fa, "habitable chiste", new string[] {"habitable", "chist"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testElision() throws Exception
	  public virtual void testElision()
	  {
		FrenchAnalyzer fa = new FrenchAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(fa, "voir l'embrouille", new string[] {"voir", "embrouil"});
	  }

	  /// <summary>
	  /// Prior to 3.1, this analyzer had no lowercase filter.
	  /// stopwords were case sensitive. Preserve this for back compat. </summary>
	  /// @deprecated (3.1) Remove this test in Lucene 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("(3.1) Remove this test in Lucene 5.0") public void testBuggyStopwordsCasing() throws java.io.IOException
	  [Obsolete("(3.1) Remove this test in Lucene 5.0")]
	  public virtual void testBuggyStopwordsCasing()
	  {
		FrenchAnalyzer a = new FrenchAnalyzer(Version.LUCENE_30);
		assertAnalyzesTo(a, "Votre", new string[] {"votr"});
	  }

	  /// <summary>
	  /// Test that stopwords are not case sensitive
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopwordsCasing() throws java.io.IOException
	  public virtual void testStopwordsCasing()
	  {
		FrenchAnalyzer a = new FrenchAnalyzer(Version.LUCENE_31);
		assertAnalyzesTo(a, "Votre", new string[] { });
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new FrenchAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }

	  /// <summary>
	  /// test accent-insensitive </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAccentInsensitive() throws Exception
	  public virtual void testAccentInsensitive()
	  {
		Analyzer a = new FrenchAnalyzer(TEST_VERSION_CURRENT);
		checkOneTerm(a, "sécuritaires", "securitair");
		checkOneTerm(a, "securitaires", "securitair");
	  }
	}

}