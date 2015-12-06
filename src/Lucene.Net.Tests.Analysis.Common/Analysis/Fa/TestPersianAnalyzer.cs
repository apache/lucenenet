namespace org.apache.lucene.analysis.fa
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

	/// <summary>
	/// Test the Persian Analyzer
	/// 
	/// </summary>
	public class TestPersianAnalyzer : BaseTokenStreamTestCase
	{

	  /// <summary>
	  /// This test fails with NPE when the stopwords file is missing in classpath
	  /// </summary>
	  public virtual void testResourcesAvailable()
	  {
		new PersianAnalyzer(TEST_VERSION_CURRENT);
	  }

	  /// <summary>
	  /// This test shows how the combination of tokenization (breaking on zero-width
	  /// non-joiner), normalization (such as treating arabic YEH and farsi YEH the
	  /// same), and stopwords creates a light-stemming effect for verbs.
	  /// 
	  /// These verb forms are from http://en.wikipedia.org/wiki/Persian_grammar
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBehaviorVerbs() throws Exception
	  public virtual void testBehaviorVerbs()
	  {
		Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
		// active present indicative
		assertAnalyzesTo(a, "می‌خورد", new string[] {"خورد"});
		// active preterite indicative
		assertAnalyzesTo(a, "خورد", new string[] {"خورد"});
		// active imperfective preterite indicative
		assertAnalyzesTo(a, "می‌خورد", new string[] {"خورد"});
		// active future indicative
		assertAnalyzesTo(a, "خواهد خورد", new string[] {"خورد"});
		// active present progressive indicative
		assertAnalyzesTo(a, "دارد می‌خورد", new string[] {"خورد"});
		// active preterite progressive indicative
		assertAnalyzesTo(a, "داشت می‌خورد", new string[] {"خورد"});

		// active perfect indicative
		assertAnalyzesTo(a, "خورده‌است", new string[] {"خورده"});
		// active imperfective perfect indicative
		assertAnalyzesTo(a, "می‌خورده‌است", new string[] {"خورده"});
		// active pluperfect indicative
		assertAnalyzesTo(a, "خورده بود", new string[] {"خورده"});
		// active imperfective pluperfect indicative
		assertAnalyzesTo(a, "می‌خورده بود", new string[] {"خورده"});
		// active preterite subjunctive
		assertAnalyzesTo(a, "خورده باشد", new string[] {"خورده"});
		// active imperfective preterite subjunctive
		assertAnalyzesTo(a, "می‌خورده باشد", new string[] {"خورده"});
		// active pluperfect subjunctive
		assertAnalyzesTo(a, "خورده بوده باشد", new string[] {"خورده"});
		// active imperfective pluperfect subjunctive
		assertAnalyzesTo(a, "می‌خورده بوده باشد", new string[] {"خورده"});
		// passive present indicative
		assertAnalyzesTo(a, "خورده می‌شود", new string[] {"خورده"});
		// passive preterite indicative
		assertAnalyzesTo(a, "خورده شد", new string[] {"خورده"});
		// passive imperfective preterite indicative
		assertAnalyzesTo(a, "خورده می‌شد", new string[] {"خورده"});
		// passive perfect indicative
		assertAnalyzesTo(a, "خورده شده‌است", new string[] {"خورده"});
		// passive imperfective perfect indicative
		assertAnalyzesTo(a, "خورده می‌شده‌است", new string[] {"خورده"});
		// passive pluperfect indicative
		assertAnalyzesTo(a, "خورده شده بود", new string[] {"خورده"});
		// passive imperfective pluperfect indicative
		assertAnalyzesTo(a, "خورده می‌شده بود", new string[] {"خورده"});
		// passive future indicative
		assertAnalyzesTo(a, "خورده خواهد شد", new string[] {"خورده"});
		// passive present progressive indicative
		assertAnalyzesTo(a, "دارد خورده می‌شود", new string[] {"خورده"});
		// passive preterite progressive indicative
		assertAnalyzesTo(a, "داشت خورده می‌شد", new string[] {"خورده"});
		// passive present subjunctive
		assertAnalyzesTo(a, "خورده شود", new string[] {"خورده"});
		// passive preterite subjunctive
		assertAnalyzesTo(a, "خورده شده باشد", new string[] {"خورده"});
		// passive imperfective preterite subjunctive
		assertAnalyzesTo(a, "خورده می‌شده باشد", new string[] {"خورده"});
		// passive pluperfect subjunctive
		assertAnalyzesTo(a, "خورده شده بوده باشد", new string[] {"خورده"});
		// passive imperfective pluperfect subjunctive
		assertAnalyzesTo(a, "خورده می‌شده بوده باشد", new string[] {"خورده"});

		// active present subjunctive
		assertAnalyzesTo(a, "بخورد", new string[] {"بخورد"});
	  }

	  /// <summary>
	  /// This test shows how the combination of tokenization and stopwords creates a
	  /// light-stemming effect for verbs.
	  /// 
	  /// In this case, these forms are presented with alternative orthography, using
	  /// arabic yeh and whitespace. This yeh phenomenon is common for legacy text
	  /// due to some previous bugs in Microsoft Windows.
	  /// 
	  /// These verb forms are from http://en.wikipedia.org/wiki/Persian_grammar
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBehaviorVerbsDefective() throws Exception
	  public virtual void testBehaviorVerbsDefective()
	  {
		Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
		// active present indicative
		assertAnalyzesTo(a, "مي خورد", new string[] {"خورد"});
		// active preterite indicative
		assertAnalyzesTo(a, "خورد", new string[] {"خورد"});
		// active imperfective preterite indicative
		assertAnalyzesTo(a, "مي خورد", new string[] {"خورد"});
		// active future indicative
		assertAnalyzesTo(a, "خواهد خورد", new string[] {"خورد"});
		// active present progressive indicative
		assertAnalyzesTo(a, "دارد مي خورد", new string[] {"خورد"});
		// active preterite progressive indicative
		assertAnalyzesTo(a, "داشت مي خورد", new string[] {"خورد"});

		// active perfect indicative
		assertAnalyzesTo(a, "خورده است", new string[] {"خورده"});
		// active imperfective perfect indicative
		assertAnalyzesTo(a, "مي خورده است", new string[] {"خورده"});
		// active pluperfect indicative
		assertAnalyzesTo(a, "خورده بود", new string[] {"خورده"});
		// active imperfective pluperfect indicative
		assertAnalyzesTo(a, "مي خورده بود", new string[] {"خورده"});
		// active preterite subjunctive
		assertAnalyzesTo(a, "خورده باشد", new string[] {"خورده"});
		// active imperfective preterite subjunctive
		assertAnalyzesTo(a, "مي خورده باشد", new string[] {"خورده"});
		// active pluperfect subjunctive
		assertAnalyzesTo(a, "خورده بوده باشد", new string[] {"خورده"});
		// active imperfective pluperfect subjunctive
		assertAnalyzesTo(a, "مي خورده بوده باشد", new string[] {"خورده"});
		// passive present indicative
		assertAnalyzesTo(a, "خورده مي شود", new string[] {"خورده"});
		// passive preterite indicative
		assertAnalyzesTo(a, "خورده شد", new string[] {"خورده"});
		// passive imperfective preterite indicative
		assertAnalyzesTo(a, "خورده مي شد", new string[] {"خورده"});
		// passive perfect indicative
		assertAnalyzesTo(a, "خورده شده است", new string[] {"خورده"});
		// passive imperfective perfect indicative
		assertAnalyzesTo(a, "خورده مي شده است", new string[] {"خورده"});
		// passive pluperfect indicative
		assertAnalyzesTo(a, "خورده شده بود", new string[] {"خورده"});
		// passive imperfective pluperfect indicative
		assertAnalyzesTo(a, "خورده مي شده بود", new string[] {"خورده"});
		// passive future indicative
		assertAnalyzesTo(a, "خورده خواهد شد", new string[] {"خورده"});
		// passive present progressive indicative
		assertAnalyzesTo(a, "دارد خورده مي شود", new string[] {"خورده"});
		// passive preterite progressive indicative
		assertAnalyzesTo(a, "داشت خورده مي شد", new string[] {"خورده"});
		// passive present subjunctive
		assertAnalyzesTo(a, "خورده شود", new string[] {"خورده"});
		// passive preterite subjunctive
		assertAnalyzesTo(a, "خورده شده باشد", new string[] {"خورده"});
		// passive imperfective preterite subjunctive
		assertAnalyzesTo(a, "خورده مي شده باشد", new string[] {"خورده"});
		// passive pluperfect subjunctive
		assertAnalyzesTo(a, "خورده شده بوده باشد", new string[] {"خورده"});
		// passive imperfective pluperfect subjunctive
		assertAnalyzesTo(a, "خورده مي شده بوده باشد", new string[] {"خورده"});

		// active present subjunctive
		assertAnalyzesTo(a, "بخورد", new string[] {"بخورد"});
	  }

	  /// <summary>
	  /// This test shows how the combination of tokenization (breaking on zero-width
	  /// non-joiner or space) and stopwords creates a light-stemming effect for
	  /// nouns, removing the plural -ha.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBehaviorNouns() throws Exception
	  public virtual void testBehaviorNouns()
	  {
		Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "برگ ها", new string[] {"برگ"});
		assertAnalyzesTo(a, "برگ‌ها", new string[] {"برگ"});
	  }

	  /// <summary>
	  /// Test showing that non-persian text is treated very much like SimpleAnalyzer
	  /// (lowercased, etc)
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBehaviorNonPersian() throws Exception
	  public virtual void testBehaviorNonPersian()
	  {
		Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "English test.", new string[] {"english", "test"});
	  }

	  /// <summary>
	  /// Basic test ensuring that tokenStream works correctly.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
	  public virtual void testReusableTokenStream()
	  {
		Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(a, "خورده مي شده بوده باشد", new string[] {"خورده"});
		assertAnalyzesTo(a, "برگ‌ها", new string[] {"برگ"});
	  }

	  /// <summary>
	  /// Test that custom stopwords work, and are not case-sensitive.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCustomStopwords() throws Exception
	  public virtual void testCustomStopwords()
	  {
		PersianAnalyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT, new CharArraySet(TEST_VERSION_CURRENT, asSet("the", "and", "a"), false));
		assertAnalyzesTo(a, "The quick brown fox.", new string[] {"quick", "brown", "fox"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new PersianAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}