using System.Collections.Generic;

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

namespace org.apache.lucene.analysis.miscellaneous
{


	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Test <seealso cref="KeepWordFilter"/> </summary>
	public class TestKeepWordFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopAndGo() throws Exception
	  public virtual void testStopAndGo()
	  {
		ISet<string> words = new HashSet<string>();
		words.Add("aaa");
		words.Add("bbb");

		string input = "xxx yyy aaa zzz BBB ccc ddd EEE";

		// Test Stopwords
		TokenStream stream = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		stream = new KeepWordFilter(TEST_VERSION_CURRENT, stream, new CharArraySet(TEST_VERSION_CURRENT, words, true));
		assertTokenStreamContents(stream, new string[] {"aaa", "BBB"}, new int[] {3, 2});

		// Now force case
		stream = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		stream = new KeepWordFilter(TEST_VERSION_CURRENT, stream, new CharArraySet(TEST_VERSION_CURRENT,words, false));
		assertTokenStreamContents(stream, new string[] {"aaa"}, new int[] {3});

		// Test Stopwords
		stream = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		stream = new KeepWordFilter(Version.LUCENE_43, false, stream, new CharArraySet(TEST_VERSION_CURRENT, words, true));
		assertTokenStreamContents(stream, new string[] {"aaa", "BBB"}, new int[] {1, 1});

		// Now force case
		stream = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		stream = new KeepWordFilter(Version.LUCENE_43, false, stream, new CharArraySet(TEST_VERSION_CURRENT,words, false));
		assertTokenStreamContents(stream, new string[] {"aaa"}, new int[] {1});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Set<String> words = new java.util.HashSet<>();
		ISet<string> words = new HashSet<string>();
		words.Add("a");
		words.Add("b");

		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this, words);

		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestKeepWordFilter outerInstance;

		  private ISet<string> words;

		  public AnalyzerAnonymousInnerClassHelper(TestKeepWordFilter outerInstance, ISet<string> words)
		  {
			  this.outerInstance = outerInstance;
			  this.words = words;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenStream stream = new KeepWordFilter(TEST_VERSION_CURRENT, tokenizer, new CharArraySet(TEST_VERSION_CURRENT, words, true));
			return new TokenStreamComponents(tokenizer, stream);
		  }
	  }
	}

}