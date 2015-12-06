using System;
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


namespace org.apache.lucene.analysis.wikipedia
{


	using FlagsAttribute = org.apache.lucene.analysis.tokenattributes.FlagsAttribute;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.wikipedia.WikipediaTokenizer.*;

	/// <summary>
	/// Basic Tests for <seealso cref="WikipediaTokenizer"/>
	/// 
	/// </summary>
	public class WikipediaTokenizerTest : BaseTokenStreamTestCase
	{
	  protected internal const string LINK_PHRASES = "click [[link here again]] click [http://lucene.apache.org here again] [[Category:a b c d]]";

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSimple() throws Exception
	  public virtual void testSimple()
	  {
		string text = "This is a [[Category:foo]]";
		WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(text));
		assertTokenStreamContents(tf, new string[] {"This", "is", "a", "foo"}, new int[] {0, 5, 8, 21}, new int[] {4, 7, 9, 24}, new string[] {"<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", CATEGORY}, new int[] {1, 1, 1, 1}, text.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHandwritten() throws Exception
	  public virtual void testHandwritten()
	  {
		// make sure all tokens are in only one type
		string test = "[[link]] This is a [[Category:foo]] Category  This is a linked [[:Category:bar none withstanding]] " + "Category This is (parens) This is a [[link]]  This is an external URL [http://lucene.apache.org] " + "Here is ''italics'' and ''more italics'', '''bold''' and '''''five quotes''''' " + " This is a [[link|display info]]  This is a period.  Here is $3.25 and here is 3.50.  Here's Johnny.  " + "==heading== ===sub head=== followed by some text  [[Category:blah| ]] " + "''[[Category:ital_cat]]''  here is some that is ''italics [[Category:foo]] but is never closed." + "'''same [[Category:foo]] goes for this '''''and2 [[Category:foo]] and this" + " [http://foo.boo.com/test/test/ Test Test] [http://foo.boo.com/test/test/test.html Test Test]" + " [http://foo.boo.com/test/test/test.html?g=b&c=d Test Test] <ref>Citation</ref> <sup>martian</sup> <span class=\"glue\">code</span>";

		WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(test));
		assertTokenStreamContents(tf, new string[] {"link", "This", "is", "a", "foo", "Category", "This", "is", "a", "linked", "bar", "none", "withstanding", "Category", "This", "is", "parens", "This", "is", "a", "link", "This", "is", "an", "external", "URL", "http://lucene.apache.org", "Here", "is", "italics", "and", "more", "italics", "bold", "and", "five", "quotes", "This", "is", "a", "link", "display", "info", "This", "is", "a", "period", "Here", "is", "3.25", "and", "here", "is", "3.50", "Here's", "Johnny", "heading", "sub", "head", "followed", "by", "some", "text", "blah", "ital", "cat", "here", "is", "some", "that", "is", "italics", "foo", "but", "is", "never", "closed", "same", "foo", "goes", "for", "this", "and2", "foo", "and", "this", "http://foo.boo.com/test/test/", "Test", "Test", "http://foo.boo.com/test/test/test.html", "Test", "Test", "http://foo.boo.com/test/test/test.html?g=b&c=d", "Test", "Test", "Citation", "martian", "code"}, new string[] {INTERNAL_LINK, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", CATEGORY, CATEGORY, CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", INTERNAL_LINK, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", EXTERNAL_LINK_URL, "<ALPHANUM>", "<ALPHANUM>", ITALICS, "<ALPHANUM>", ITALICS, ITALICS, BOLD, "<ALPHANUM>", BOLD_ITALICS, BOLD_ITALICS, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", INTERNAL_LINK, INTERNAL_LINK, INTERNAL_LINK, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<APOSTROPHE>", "<ALPHANUM>", HEADING, SUB_HEADING, SUB_HEADING, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", CATEGORY, CATEGORY, CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", ITALICS, CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", BOLD, CATEGORY, "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", BOLD_ITALICS, CATEGORY, "<ALPHANUM>", "<ALPHANUM>", EXTERNAL_LINK_URL, EXTERNAL_LINK, EXTERNAL_LINK, EXTERNAL_LINK_URL, EXTERNAL_LINK, EXTERNAL_LINK, EXTERNAL_LINK_URL, EXTERNAL_LINK, EXTERNAL_LINK, CITATION, "<ALPHANUM>", "<ALPHANUM>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLinkPhrases() throws Exception
	  public virtual void testLinkPhrases()
	  {
		WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(LINK_PHRASES));
		checkLinkPhrases(tf);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void checkLinkPhrases(WikipediaTokenizer tf) throws java.io.IOException
	  private void checkLinkPhrases(WikipediaTokenizer tf)
	  {
		assertTokenStreamContents(tf, new string[] {"click", "link", "here", "again", "click", "http://lucene.apache.org", "here", "again", "a", "b", "c", "d"}, new int[] {1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLinks() throws Exception
	  public virtual void testLinks()
	  {
		string test = "[http://lucene.apache.org/java/docs/index.html#news here] [http://lucene.apache.org/java/docs/index.html?b=c here] [https://lucene.apache.org/java/docs/index.html?b=c here]";
		WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(test));
		assertTokenStreamContents(tf, new string[] {"http://lucene.apache.org/java/docs/index.html#news", "here", "http://lucene.apache.org/java/docs/index.html?b=c", "here", "https://lucene.apache.org/java/docs/index.html?b=c", "here"}, new string[] {EXTERNAL_LINK_URL, EXTERNAL_LINK, EXTERNAL_LINK_URL, EXTERNAL_LINK, EXTERNAL_LINK_URL, EXTERNAL_LINK});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLucene1133() throws Exception
	  public virtual void testLucene1133()
	  {
		ISet<string> untoks = new HashSet<string>();
		untoks.Add(WikipediaTokenizer.CATEGORY);
		untoks.Add(WikipediaTokenizer.ITALICS);
		//should be exactly the same, regardless of untoks
		WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(LINK_PHRASES), WikipediaTokenizer.TOKENS_ONLY, untoks);
		checkLinkPhrases(tf);
		string test = "[[Category:a b c d]] [[Category:e f g]] [[link here]] [[link there]] ''italics here'' something ''more italics'' [[Category:h   i   j]]";
		tf = new WikipediaTokenizer(new StringReader(test), WikipediaTokenizer.UNTOKENIZED_ONLY, untoks);
		assertTokenStreamContents(tf, new string[] {"a b c d", "e f g", "link", "here", "link", "there", "italics here", "something", "more italics", "h   i   j"}, new int[] {11, 32, 42, 47, 56, 61, 71, 86, 98, 124}, new int[] {18, 37, 46, 51, 60, 66, 83, 95, 110, 133}, new int[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBoth() throws Exception
	  public virtual void testBoth()
	  {
		ISet<string> untoks = new HashSet<string>();
		untoks.Add(WikipediaTokenizer.CATEGORY);
		untoks.Add(WikipediaTokenizer.ITALICS);
		string test = "[[Category:a b c d]] [[Category:e f g]] [[link here]] [[link there]] ''italics here'' something ''more italics'' [[Category:h   i   j]]";
		//should output all the indivual tokens plus the untokenized tokens as well.  Untokenized tokens
		WikipediaTokenizer tf = new WikipediaTokenizer(new StringReader(test), WikipediaTokenizer.BOTH, untoks);
		assertTokenStreamContents(tf, new string[] {"a b c d", "a", "b", "c", "d", "e f g", "e", "f", "g", "link", "here", "link", "there", "italics here", "italics", "here", "something", "more italics", "more", "italics", "h   i   j", "h", "i", "j"}, new int[] {11, 11, 13, 15, 17, 32, 32, 34, 36, 42, 47, 56, 61, 71, 71, 79, 86, 98, 98, 103, 124, 124, 128, 132}, new int[] {18, 12, 14, 16, 18, 37, 33, 35, 37, 46, 51, 60, 66, 83, 78, 83, 95, 110, 102, 110, 133, 125, 129, 133}, new int[] {1, 0, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0, 1, 1});

		// now check the flags, TODO: add way to check flags from BaseTokenStreamTestCase?
		tf = new WikipediaTokenizer(new StringReader(test), WikipediaTokenizer.BOTH, untoks);
		int[] expectedFlags = new int[] {UNTOKENIZED_TOKEN_FLAG, 0, 0, 0, 0, UNTOKENIZED_TOKEN_FLAG, 0, 0, 0, 0, 0, 0, 0, UNTOKENIZED_TOKEN_FLAG, 0, 0, 0, UNTOKENIZED_TOKEN_FLAG, 0, 0, UNTOKENIZED_TOKEN_FLAG, 0, 0, 0};
		FlagsAttribute flagsAtt = tf.addAttribute(typeof(FlagsAttribute));
		tf.reset();
		for (int i = 0; i < expectedFlags.Length; i++)
		{
		  assertTrue(tf.incrementToken());
		  assertEquals("flags " + i, expectedFlags[i], flagsAtt.Flags);
		}
		assertFalse(tf.incrementToken());
		tf.close();
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly WikipediaTokenizerTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(WikipediaTokenizerTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new WikipediaTokenizer(reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

	  /// <summary>
	  /// blast some random large strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHugeStrings() throws Exception
	  public virtual void testRandomHugeStrings()
	  {
		Random random = random();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkRandomData(random, a, 100 * RANDOM_MULTIPLIER, 8192);
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly WikipediaTokenizerTest outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(WikipediaTokenizerTest outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new WikipediaTokenizer(reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }
	}

}