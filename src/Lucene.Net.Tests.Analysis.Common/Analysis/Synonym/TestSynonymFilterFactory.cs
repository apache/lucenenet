using System;

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


	using PatternTokenizerFactory = org.apache.lucene.analysis.pattern.PatternTokenizerFactory;
	using TokenFilterFactory = org.apache.lucene.analysis.util.TokenFilterFactory;
	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;
	using ClasspathResourceLoader = org.apache.lucene.analysis.util.ClasspathResourceLoader;
	using StringMockResourceLoader = org.apache.lucene.analysis.util.StringMockResourceLoader;
	using Version = org.apache.lucene.util.Version;

	public class TestSynonymFilterFactory : BaseTokenStreamFactoryTestCase
	{

	  /// <summary>
	  /// checks for synonyms of "GB" in synonyms.txt </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void checkSolrSynonyms(org.apache.lucene.analysis.util.TokenFilterFactory factory) throws Exception
	  private void checkSolrSynonyms(TokenFilterFactory factory)
	  {
		Reader reader = new StringReader("GB");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = factory.create(stream);
		assertTrue(stream is SynonymFilter);
		assertTokenStreamContents(stream, new string[] {"GB", "gib", "gigabyte", "gigabytes"}, new int[] {1, 0, 0, 0});
	  }

	  /// <summary>
	  /// checks for synonyms of "second" in synonyms-wordnet.txt </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void checkWordnetSynonyms(org.apache.lucene.analysis.util.TokenFilterFactory factory) throws Exception
	  private void checkWordnetSynonyms(TokenFilterFactory factory)
	  {
		Reader reader = new StringReader("second");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = factory.create(stream);
		assertTrue(stream is SynonymFilter);
		assertTokenStreamContents(stream, new string[] {"second", "2nd", "two"}, new int[] {1, 0, 0});
	  }

	  /// <summary>
	  /// test that we can parse and use the solr syn file </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSynonyms() throws Exception
	  public virtual void testSynonyms()
	  {
		checkSolrSynonyms(tokenFilterFactory("Synonym", "synonyms", "synonyms.txt"));
	  }

	  /// <summary>
	  /// test that we can parse and use the solr syn file, with the old impl </summary>
	  /// @deprecated Remove this test in Lucene 5.0  
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("Remove this test in Lucene 5.0") public void testSynonymsOld() throws Exception
	  [Obsolete("Remove this test in Lucene 5.0")]
	  public virtual void testSynonymsOld()
	  {
		Reader reader = new StringReader("GB");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Synonym", Version.LUCENE_33, new ClasspathResourceLoader(this.GetType()), "synonyms", "synonyms.txt").create(stream);
		assertTrue(stream is SlowSynonymFilter);
		assertTokenStreamContents(stream, new string[] {"GB", "gib", "gigabyte", "gigabytes"}, new int[] {1, 0, 0, 0});
	  }

	  /// <summary>
	  /// test multiword offsets with the old impl </summary>
	  /// @deprecated Remove this test in Lucene 5.0  
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("Remove this test in Lucene 5.0") public void testMultiwordOffsetsOld() throws Exception
	  [Obsolete("Remove this test in Lucene 5.0")]
	  public virtual void testMultiwordOffsetsOld()
	  {
		Reader reader = new StringReader("national hockey league");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Synonym", Version.LUCENE_33, new StringMockResourceLoader("national hockey league, nhl"), "synonyms", "synonyms.txt").create(stream);
		// WTF?
		assertTokenStreamContents(stream, new string[] {"national", "nhl", "hockey", "league"}, new int[] {0, 0, 0, 0}, new int[] {22, 22, 22, 22}, new int[] {1, 0, 1, 1});
	  }

	  /// <summary>
	  /// if the synonyms are completely empty, test that we still analyze correctly </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptySynonyms() throws Exception
	  public virtual void testEmptySynonyms()
	  {
		Reader reader = new StringReader("GB");
		TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
		stream = tokenFilterFactory("Synonym", TEST_VERSION_CURRENT, new StringMockResourceLoader(""), "synonyms", "synonyms.txt").create(stream); // empty file!
		assertTokenStreamContents(stream, new string[] {"GB"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFormat() throws Exception
	  public virtual void testFormat()
	  {
		checkSolrSynonyms(tokenFilterFactory("Synonym", "synonyms", "synonyms.txt", "format", "solr"));
		checkWordnetSynonyms(tokenFilterFactory("Synonym", "synonyms", "synonyms-wordnet.txt", "format", "wordnet"));
		// explicit class should work the same as the "solr" alias
//JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
		checkSolrSynonyms(tokenFilterFactory("Synonym", "synonyms", "synonyms.txt", "format", typeof(SolrSynonymParser).FullName));
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("Synonym", "synonyms", "synonyms.txt", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }

	  internal const string TOK_SYN_ARG_VAL = "argument";
	  internal const string TOK_FOO_ARG_VAL = "foofoofoo";

	  /// <summary>
	  /// Test that we can parse TokenierFactory's arguments </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTokenizerFactoryArguments() throws Exception
	  public virtual void testTokenizerFactoryArguments()
	  {
		// diff versions produce diff delegator behavior,
		// all should be (mostly) equivilent for our test purposes.
		doTestTokenizerFactoryArguments(Version.LUCENE_33, typeof(SlowSynonymFilterFactory));
		doTestTokenizerFactoryArguments(Version.LUCENE_34, typeof(FSTSynonymFilterFactory));
		doTestTokenizerFactoryArguments(Version.LUCENE_35, typeof(FSTSynonymFilterFactory));

		doTestTokenizerFactoryArguments(Version.LUCENE_CURRENT, typeof(FSTSynonymFilterFactory));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected void doTestTokenizerFactoryArguments(final org.apache.lucene.util.Version ver, final Class delegatorClass) throws Exception
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  protected internal virtual void doTestTokenizerFactoryArguments(Version ver, Type delegatorClass)
	  {

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String clazz = org.apache.lucene.analysis.pattern.PatternTokenizerFactory.class.getName();
//JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
		string clazz = typeof(PatternTokenizerFactory).FullName;
		TokenFilterFactory factory = null;

		// simple arg form
		factory = tokenFilterFactory("Synonym", ver, "synonyms", "synonyms.txt", "tokenizerFactory", clazz, "pattern", "(.*)", "group", "0");
		assertDelegator(factory, delegatorClass);

		// prefix
		factory = tokenFilterFactory("Synonym", ver, "synonyms", "synonyms.txt", "tokenizerFactory", clazz, "tokenizerFactory.pattern", "(.*)", "tokenizerFactory.group", "0");
		assertDelegator(factory, delegatorClass);

		// sanity check that sub-PatternTokenizerFactory fails w/o pattern
		try
		{
		  factory = tokenFilterFactory("Synonym", ver, "synonyms", "synonyms.txt", "tokenizerFactory", clazz);
		  fail("tokenizerFactory should have complained about missing pattern arg");
		}
		catch (Exception)
		{
		  // :NOOP:
		}

		// sanity check that sub-PatternTokenizerFactory fails on unexpected
		try
		{
		  factory = tokenFilterFactory("Synonym", ver, "synonyms", "synonyms.txt", "tokenizerFactory", clazz, "tokenizerFactory.pattern", "(.*)", "tokenizerFactory.bogusbogusbogus", "bogus", "tokenizerFactory.group", "0");
		  fail("tokenizerFactory should have complained about missing pattern arg");
		}
		catch (Exception)
		{
		  // :NOOP:
		}
	  }

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: private static void assertDelegator(final org.apache.lucene.analysis.util.TokenFilterFactory factory, final Class delegatorClass)
	  private static void assertDelegator(TokenFilterFactory factory, Type delegatorClass)
	  {
		assertNotNull(factory);
		assertTrue("factory not expected class: " + factory.GetType(), factory is SynonymFilterFactory);
		SynonymFilterFactory synFac = (SynonymFilterFactory) factory;
		object delegator = synFac.Delegator;
		assertNotNull(delegator);
		assertTrue("delegator not expected class: " + delegator.GetType(), delegatorClass.IsInstanceOfType(delegator));

	  }
	}



}