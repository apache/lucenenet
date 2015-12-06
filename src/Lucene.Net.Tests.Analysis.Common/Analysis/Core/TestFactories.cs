using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace org.apache.lucene.analysis.core
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


	using AbstractAnalysisFactory = org.apache.lucene.analysis.util.AbstractAnalysisFactory;
	using CharFilterFactory = org.apache.lucene.analysis.util.CharFilterFactory;
	using MultiTermAwareComponent = org.apache.lucene.analysis.util.MultiTermAwareComponent;
	using ResourceLoaderAware = org.apache.lucene.analysis.util.ResourceLoaderAware;
	using StringMockResourceLoader = org.apache.lucene.analysis.util.StringMockResourceLoader;
	using TokenFilterFactory = org.apache.lucene.analysis.util.TokenFilterFactory;
	using TokenizerFactory = org.apache.lucene.analysis.util.TokenizerFactory;
	using AttributeFactory = org.apache.lucene.util.AttributeSource.AttributeFactory;

	/// <summary>
	/// Sanity check some things about all factories,
	/// we do our best to see if we can sanely initialize it with
	/// no parameters and smoke test it, etc.
	/// </summary>
	// TODO: move this, TestRandomChains, and TestAllAnalyzersHaveFactories
	// to an integration test module that sucks in all analysis modules.
	// currently the only way to do this is via eclipse etc (LUCENE-3974)
	public class TestFactories : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws java.io.IOException
	  public virtual void test()
	  {
		foreach (string tokenizer in TokenizerFactory.availableTokenizers())
		{
		  doTestTokenizer(tokenizer);
		}

		foreach (string tokenFilter in TokenFilterFactory.availableTokenFilters())
		{
		  doTestTokenFilter(tokenFilter);
		}

		foreach (string charFilter in CharFilterFactory.availableCharFilters())
		{
		  doTestCharFilter(charFilter);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void doTestTokenizer(String tokenizer) throws java.io.IOException
	  private void doTestTokenizer(string tokenizer)
	  {
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Class<? extends org.apache.lucene.analysis.util.TokenizerFactory> factoryClazz = org.apache.lucene.analysis.util.TokenizerFactory.lookupClass(tokenizer);
		Type<?> factoryClazz = TokenizerFactory.lookupClass(tokenizer);
		TokenizerFactory factory = (TokenizerFactory) initialize(factoryClazz);
		if (factory != null)
		{
		  // we managed to fully create an instance. check a few more things:

		  // if it implements MultiTermAware, sanity check its impl
		  if (factory is MultiTermAwareComponent)
		  {
			AbstractAnalysisFactory mtc = ((MultiTermAwareComponent) factory).MultiTermComponent;
			assertNotNull(mtc);
			// its not ok to return e.g. a charfilter here: but a tokenizer could wrap a filter around it
			assertFalse(mtc is CharFilterFactory);
		  }

		  // beast it just a little, it shouldnt throw exceptions:
		  // (it should have thrown them in initialize)
		  checkRandomData(random(), new FactoryAnalyzer(factory, null, null), 100, 20, false, false);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void doTestTokenFilter(String tokenfilter) throws java.io.IOException
	  private void doTestTokenFilter(string tokenfilter)
	  {
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Class<? extends org.apache.lucene.analysis.util.TokenFilterFactory> factoryClazz = org.apache.lucene.analysis.util.TokenFilterFactory.lookupClass(tokenfilter);
		Type<?> factoryClazz = TokenFilterFactory.lookupClass(tokenfilter);
		TokenFilterFactory factory = (TokenFilterFactory) initialize(factoryClazz);
		if (factory != null)
		{
		  // we managed to fully create an instance. check a few more things:

		  // if it implements MultiTermAware, sanity check its impl
		  if (factory is MultiTermAwareComponent)
		  {
			AbstractAnalysisFactory mtc = ((MultiTermAwareComponent) factory).MultiTermComponent;
			assertNotNull(mtc);
			// its not ok to return a charfilter or tokenizer here, this makes no sense
			assertTrue(mtc is TokenFilterFactory);
		  }

		  // beast it just a little, it shouldnt throw exceptions:
		  // (it should have thrown them in initialize)
		  checkRandomData(random(), new FactoryAnalyzer(assertingTokenizer, factory, null), 100, 20, false, false);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void doTestCharFilter(String charfilter) throws java.io.IOException
	  private void doTestCharFilter(string charfilter)
	  {
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Class<? extends org.apache.lucene.analysis.util.CharFilterFactory> factoryClazz = org.apache.lucene.analysis.util.CharFilterFactory.lookupClass(charfilter);
		Type<?> factoryClazz = CharFilterFactory.lookupClass(charfilter);
		CharFilterFactory factory = (CharFilterFactory) initialize(factoryClazz);
		if (factory != null)
		{
		  // we managed to fully create an instance. check a few more things:

		  // if it implements MultiTermAware, sanity check its impl
		  if (factory is MultiTermAwareComponent)
		  {
			AbstractAnalysisFactory mtc = ((MultiTermAwareComponent) factory).MultiTermComponent;
			assertNotNull(mtc);
			// its not ok to return a tokenizer or tokenfilter here, this makes no sense
			assertTrue(mtc is CharFilterFactory);
		  }

		  // beast it just a little, it shouldnt throw exceptions:
		  // (it should have thrown them in initialize)
		  checkRandomData(random(), new FactoryAnalyzer(assertingTokenizer, null, factory), 100, 20, false, false);
		}
	  }

	  /// <summary>
	  /// tries to initialize a factory with no arguments </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.analysis.util.AbstractAnalysisFactory initialize(Class<? extends org.apache.lucene.analysis.util.AbstractAnalysisFactory> factoryClazz) throws java.io.IOException
	  private AbstractAnalysisFactory initialize<T1>(Type<T1> factoryClazz) where T1 : org.apache.lucene.analysis.util.AbstractAnalysisFactory
	  {
		IDictionary<string, string> args = new Dictionary<string, string>();
		args["luceneMatchVersion"] = TEST_VERSION_CURRENT.ToString();
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Constructor<? extends org.apache.lucene.analysis.util.AbstractAnalysisFactory> ctor;
		Constructor<?> ctor;
		try
		{
		  ctor = factoryClazz.GetConstructor(typeof(IDictionary));
		}
		catch (Exception)
		{
		  throw new Exception("factory '" + factoryClazz + "' does not have a proper ctor!");
		}

		AbstractAnalysisFactory factory = null;
		try
		{
		  factory = ctor.newInstance(args);
		}
		catch (InstantiationException e)
		{
		  throw new Exception(e);
		}
		catch (IllegalAccessException e)
		{
		  throw new Exception(e);
		}
		catch (InvocationTargetException e)
		{
		  if (e.InnerException is System.ArgumentException)
		  {
			// its ok if we dont provide the right parameters to throw this
			return null;
		  }
		}

		if (factory is ResourceLoaderAware)
		{
		  try
		  {
			((ResourceLoaderAware) factory).inform(new StringMockResourceLoader(""));
		  }
		  catch (IOException)
		  {
			// its ok if the right files arent available or whatever to throw this
		  }
		  catch (System.ArgumentException)
		  {
			// is this ok? I guess so
		  }
		}
		return factory;
	  }

	  // some silly classes just so we can use checkRandomData
	  private TokenizerFactory assertingTokenizer = new TokenizerFactoryAnonymousInnerClassHelper(new Dictionary<string, string>());

	  private class TokenizerFactoryAnonymousInnerClassHelper : TokenizerFactory
	  {
		  public TokenizerFactoryAnonymousInnerClassHelper(Dictionary<string, string> java) : base(Hashtable<string, string>)
		  {
		  }

		  public override MockTokenizer create(AttributeFactory factory, Reader input)
		  {
			return new MockTokenizer(factory, input);
		  }
	  }

	  private class FactoryAnalyzer : Analyzer
	  {
		internal readonly TokenizerFactory tokenizer;
		internal readonly CharFilterFactory charFilter;
		internal readonly TokenFilterFactory tokenfilter;

		internal FactoryAnalyzer(TokenizerFactory tokenizer, TokenFilterFactory tokenfilter, CharFilterFactory charFilter)
		{
		  Debug.Assert(tokenizer != null);
		  this.tokenizer = tokenizer;
		  this.charFilter = charFilter;
		  this.tokenfilter = tokenfilter;
		}

		protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		{
		  Tokenizer tf = tokenizer.create(reader);
		  if (tokenfilter != null)
		  {
			return new TokenStreamComponents(tf, tokenfilter.create(tf));
		  }
		  else
		  {
			return new TokenStreamComponents(tf);
		  }
		}

		protected internal override Reader initReader(string fieldName, Reader reader)
		{
		  if (charFilter != null)
		  {
			return charFilter.create(reader);
		  }
		  else
		  {
			return reader;
		  }
		}
	  }
	}

}