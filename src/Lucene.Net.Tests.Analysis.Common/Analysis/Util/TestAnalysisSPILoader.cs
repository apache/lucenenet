using System.Collections.Generic;

namespace org.apache.lucene.analysis.util
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


	using HTMLStripCharFilterFactory = org.apache.lucene.analysis.charfilter.HTMLStripCharFilterFactory;
	using LowerCaseFilterFactory = org.apache.lucene.analysis.core.LowerCaseFilterFactory;
	using WhitespaceTokenizerFactory = org.apache.lucene.analysis.core.WhitespaceTokenizerFactory;
	using RemoveDuplicatesTokenFilterFactory = org.apache.lucene.analysis.miscellaneous.RemoveDuplicatesTokenFilterFactory;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;

	public class TestAnalysisSPILoader : LuceneTestCase
	{

	  private IDictionary<string, string> versionArgOnly()
	  {
		return new HashMapAnonymousInnerClassHelper(this);
	  }

	  private class HashMapAnonymousInnerClassHelper : Dictionary<string, string>
	  {
		  private readonly TestAnalysisSPILoader outerInstance;

		  public HashMapAnonymousInnerClassHelper(TestAnalysisSPILoader outerInstance)
		  {
			  this.outerInstance = outerInstance;

			  this.put("luceneMatchVersion", TEST_VERSION_CURRENT.ToString());
		  }

	  }

	  public virtual void testLookupTokenizer()
	  {
		assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.forName("Whitespace", versionArgOnly()).GetType());
		assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.forName("WHITESPACE", versionArgOnly()).GetType());
		assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.forName("whitespace", versionArgOnly()).GetType());
	  }

	  public virtual void testBogusLookupTokenizer()
	  {
		try
		{
		  TokenizerFactory.forName("sdfsdfsdfdsfsdfsdf", new Dictionary<string, string>());
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}

		try
		{
		  TokenizerFactory.forName("!(**#$U*#$*", new Dictionary<string, string>());
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}
	  }

	  public virtual void testLookupTokenizerClass()
	  {
		assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.lookupClass("Whitespace"));
		assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.lookupClass("WHITESPACE"));
		assertSame(typeof(WhitespaceTokenizerFactory), TokenizerFactory.lookupClass("whitespace"));
	  }

	  public virtual void testBogusLookupTokenizerClass()
	  {
		try
		{
		  TokenizerFactory.lookupClass("sdfsdfsdfdsfsdfsdf");
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}

		try
		{
		  TokenizerFactory.lookupClass("!(**#$U*#$*");
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}
	  }

	  public virtual void testAvailableTokenizers()
	  {
		assertTrue(TokenizerFactory.availableTokenizers().contains("whitespace"));
	  }

	  public virtual void testLookupTokenFilter()
	  {
		assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.forName("Lowercase", versionArgOnly()).GetType());
		assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.forName("LOWERCASE", versionArgOnly()).GetType());
		assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.forName("lowercase", versionArgOnly()).GetType());

		assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.forName("RemoveDuplicates", versionArgOnly()).GetType());
		assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.forName("REMOVEDUPLICATES", versionArgOnly()).GetType());
		assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.forName("removeduplicates", versionArgOnly()).GetType());
	  }

	  public virtual void testBogusLookupTokenFilter()
	  {
		try
		{
		  TokenFilterFactory.forName("sdfsdfsdfdsfsdfsdf", new Dictionary<string, string>());
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}

		try
		{
		  TokenFilterFactory.forName("!(**#$U*#$*", new Dictionary<string, string>());
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}
	  }

	  public virtual void testLookupTokenFilterClass()
	  {
		assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.lookupClass("Lowercase"));
		assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.lookupClass("LOWERCASE"));
		assertSame(typeof(LowerCaseFilterFactory), TokenFilterFactory.lookupClass("lowercase"));

		assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.lookupClass("RemoveDuplicates"));
		assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.lookupClass("REMOVEDUPLICATES"));
		assertSame(typeof(RemoveDuplicatesTokenFilterFactory), TokenFilterFactory.lookupClass("removeduplicates"));
	  }

	  public virtual void testBogusLookupTokenFilterClass()
	  {
		try
		{
		  TokenFilterFactory.lookupClass("sdfsdfsdfdsfsdfsdf");
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}

		try
		{
		  TokenFilterFactory.lookupClass("!(**#$U*#$*");
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}
	  }

	  public virtual void testAvailableTokenFilters()
	  {
		assertTrue(TokenFilterFactory.availableTokenFilters().contains("lowercase"));
		assertTrue(TokenFilterFactory.availableTokenFilters().contains("removeduplicates"));
	  }

	  public virtual void testLookupCharFilter()
	  {
		assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.forName("HTMLStrip", versionArgOnly()).GetType());
		assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.forName("HTMLSTRIP", versionArgOnly()).GetType());
		assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.forName("htmlstrip", versionArgOnly()).GetType());
	  }

	  public virtual void testBogusLookupCharFilter()
	  {
		try
		{
		  CharFilterFactory.forName("sdfsdfsdfdsfsdfsdf", new Dictionary<string, string>());
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}

		try
		{
		  CharFilterFactory.forName("!(**#$U*#$*", new Dictionary<string, string>());
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}
	  }

	  public virtual void testLookupCharFilterClass()
	  {
		assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.lookupClass("HTMLStrip"));
		assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.lookupClass("HTMLSTRIP"));
		assertSame(typeof(HTMLStripCharFilterFactory), CharFilterFactory.lookupClass("htmlstrip"));
	  }

	  public virtual void testBogusLookupCharFilterClass()
	  {
		try
		{
		  CharFilterFactory.lookupClass("sdfsdfsdfdsfsdfsdf");
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}

		try
		{
		  CharFilterFactory.lookupClass("!(**#$U*#$*");
		  fail();
		}
		catch (System.ArgumentException)
		{
		  //
		}
	  }

	  public virtual void testAvailableCharFilters()
	  {
		assertTrue(CharFilterFactory.availableCharFilters().contains("htmlstrip"));
	  }
	}

}