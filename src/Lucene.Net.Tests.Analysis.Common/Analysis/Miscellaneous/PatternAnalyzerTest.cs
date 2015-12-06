using System;
using System.Text;
using System.Threading;

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

	using UncaughtExceptionHandler = Thread.UncaughtExceptionHandler;

	using StopAnalyzer = org.apache.lucene.analysis.core.StopAnalyzer;

	/// <summary>
	/// Verifies the behavior of PatternAnalyzer.
	/// </summary>
	public class PatternAnalyzerTest : BaseTokenStreamTestCase
	{

	  /// <summary>
	  /// Test PatternAnalyzer when it is configured with a non-word pattern.
	  /// Behavior can be similar to SimpleAnalyzer (depending upon options)
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNonWordPattern() throws java.io.IOException
	  public virtual void testNonWordPattern()
	  {
		// Split on non-letter pattern, do not lowercase, no stopwords
		PatternAnalyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.NON_WORD_PATTERN, false, null);
		check(a, "The quick brown Fox,the abcd1234 (56.78) dc.", new string[] {"The", "quick", "brown", "Fox", "the", "abcd", "dc"});

		// split on non-letter pattern, lowercase, english stopwords
		PatternAnalyzer b = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.NON_WORD_PATTERN, true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
		check(b, "The quick brown Fox,the abcd1234 (56.78) dc.", new string[] {"quick", "brown", "fox", "abcd", "dc"});
	  }

	  /// <summary>
	  /// Test PatternAnalyzer when it is configured with a whitespace pattern.
	  /// Behavior can be similar to WhitespaceAnalyzer (depending upon options)
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWhitespacePattern() throws java.io.IOException
	  public virtual void testWhitespacePattern()
	  {
		// Split on whitespace patterns, do not lowercase, no stopwords
		PatternAnalyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.WHITESPACE_PATTERN, false, null);
		check(a, "The quick brown Fox,the abcd1234 (56.78) dc.", new string[] {"The", "quick", "brown", "Fox,the", "abcd1234", "(56.78)", "dc."});

		// Split on whitespace patterns, lowercase, english stopwords
		PatternAnalyzer b = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.WHITESPACE_PATTERN, true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
		check(b, "The quick brown Fox,the abcd1234 (56.78) dc.", new string[] {"quick", "brown", "fox,the", "abcd1234", "(56.78)", "dc."});
	  }

	  /// <summary>
	  /// Test PatternAnalyzer when it is configured with a custom pattern. In this
	  /// case, text is tokenized on the comma ","
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCustomPattern() throws java.io.IOException
	  public virtual void testCustomPattern()
	  {
		// Split on comma, do not lowercase, no stopwords
		PatternAnalyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, Pattern.compile(","), false, null);
		check(a, "Here,Are,some,Comma,separated,words,", new string[] {"Here", "Are", "some", "Comma", "separated", "words"});

		// split on comma, lowercase, english stopwords
		PatternAnalyzer b = new PatternAnalyzer(TEST_VERSION_CURRENT, Pattern.compile(","), true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
		check(b, "Here,Are,some,Comma,separated,words,", new string[] {"here", "some", "comma", "separated", "words"});
	  }

	  /// <summary>
	  /// Test PatternAnalyzer against a large document.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHugeDocument() throws java.io.IOException
	  public virtual void testHugeDocument()
	  {
		StringBuilder document = new StringBuilder();
		// 5000 a's
		char[] largeWord = new char[5000];
		Arrays.fill(largeWord, 'a');
		document.Append(largeWord);

		// a space
		document.Append(' ');

		// 2000 b's
		char[] largeWord2 = new char[2000];
		Arrays.fill(largeWord2, 'b');
		document.Append(largeWord2);

		// Split on whitespace patterns, do not lowercase, no stopwords
		PatternAnalyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, PatternAnalyzer.WHITESPACE_PATTERN, false, null);
		check(a, document.ToString(), new string[]
		{
			new string(largeWord),
			new string(largeWord2)
		});
	  }

	  /// <summary>
	  /// Verify the analyzer analyzes to the expected contents. For PatternAnalyzer,
	  /// several methods are verified:
	  /// <ul>
	  /// <li>Analysis with a normal Reader
	  /// <li>Analysis with a FastStringReader
	  /// <li>Analysis with a String
	  /// </ul>
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void check(PatternAnalyzer analyzer, String document, String expected[]) throws java.io.IOException
	  private void check(PatternAnalyzer analyzer, string document, string[] expected)
	  {
		// ordinary analysis of a Reader
		assertAnalyzesTo(analyzer, document, expected);

		// analysis with a "FastStringReader"
		TokenStream ts = analyzer.tokenStream("dummy", new PatternAnalyzer.FastStringReader(document));
		assertTokenStreamContents(ts, expected);

		// analysis of a String, uses PatternAnalyzer.tokenStream(String, String)
		TokenStream ts2 = analyzer.tokenStream("dummy", new StringReader(document));
		assertTokenStreamContents(ts2, expected);
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer a = new PatternAnalyzer(TEST_VERSION_CURRENT, Pattern.compile(","), true, StopAnalyzer.ENGLISH_STOP_WORDS_SET);

		// dodge jre bug http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=7104012
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Thread.UncaughtExceptionHandler savedHandler = Thread.getDefaultUncaughtExceptionHandler();
		UncaughtExceptionHandler savedHandler = Thread.DefaultUncaughtExceptionHandler;
		Thread.DefaultUncaughtExceptionHandler = new UncaughtExceptionHandlerAnonymousInnerClassHelper(this, savedHandler);

		try
		{
		  Thread.DefaultUncaughtExceptionHandler;
		  checkRandomData(random(), a, 10000 * RANDOM_MULTIPLIER);
		}
		catch (System.IndexOutOfRangeException ex)
		{
		  assumeTrue("not failing due to jre bug ", !isJREBug7104012(ex));
		  throw ex; // otherwise rethrow
		}
		finally
		{
		  Thread.DefaultUncaughtExceptionHandler = savedHandler;
		}
	  }

	  private class UncaughtExceptionHandlerAnonymousInnerClassHelper : UncaughtExceptionHandler
	  {
		  private readonly PatternAnalyzerTest outerInstance;

		  private UncaughtExceptionHandler savedHandler;

		  public UncaughtExceptionHandlerAnonymousInnerClassHelper(PatternAnalyzerTest outerInstance, UncaughtExceptionHandler savedHandler)
		  {
			  this.outerInstance = outerInstance;
			  this.savedHandler = savedHandler;
		  }

		  public override void uncaughtException(Thread thread, Exception throwable)
		  {
			assumeTrue("not failing due to jre bug ", !isJREBug7104012(throwable));
			// otherwise its some other bug, pass to default handler
			savedHandler.uncaughtException(thread, throwable);
		  }
	  }

	  internal static bool isJREBug7104012(Exception t)
	  {
		if (!(t is System.IndexOutOfRangeException))
		{
		  // BaseTokenStreamTestCase now wraps exc in a new RuntimeException:
		  t = t.InnerException;
		  if (!(t is System.IndexOutOfRangeException))
		  {
			return false;
		  }
		}
		StackTraceElement[] trace = t.StackTrace;
		foreach (StackTraceElement st in trace)
		{
		  if ("java.text.RuleBasedBreakIterator".Equals(st.ClassName) || "sun.util.locale.provider.RuleBasedBreakIterator".Equals(st.ClassName) && "lookupBackwardState".Equals(st.MethodName))
		  {
			return true;
		  }
		}
		return false;
	  }
	}

}