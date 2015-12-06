using System;
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


	using PatternKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.PatternKeywordMarkerFilter;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using FrenchStemFilter = org.apache.lucene.analysis.fr.FrenchStemFilter;
	using IndicTokenizer = org.apache.lucene.analysis.@in.IndicTokenizer;
	using DutchStemFilter = org.apache.lucene.analysis.nl.DutchStemFilter;
	using ReversePathHierarchyTokenizer = org.apache.lucene.analysis.path.ReversePathHierarchyTokenizer;
	using TeeSinkTokenFilter = org.apache.lucene.analysis.sinks.TeeSinkTokenFilter;
	using SnowballFilter = org.apache.lucene.analysis.snowball.SnowballFilter;
	using CharFilterFactory = org.apache.lucene.analysis.util.CharFilterFactory;
	using ResourceLoader = org.apache.lucene.analysis.util.ResourceLoader;
	using ResourceLoaderAware = org.apache.lucene.analysis.util.ResourceLoaderAware;
	using StringMockResourceLoader = org.apache.lucene.analysis.util.StringMockResourceLoader;
	using TokenFilterFactory = org.apache.lucene.analysis.util.TokenFilterFactory;
	using TokenizerFactory = org.apache.lucene.analysis.util.TokenizerFactory;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;

	/// <summary>
	/// Tests that any newly added Tokenizers/TokenFilters/CharFilters have a
	/// corresponding factory (and that the SPI configuration is correct)
	/// </summary>
	public class TestAllAnalyzersHaveFactories : LuceneTestCase
	{

	  // these are test-only components (e.g. test-framework)
	  private static readonly ISet<Type> testComponents = Collections.newSetFromMap(new IdentityHashMap<Type, bool?>());
	  static TestAllAnalyzersHaveFactories()
	  {
		Collections.addAll<Type>(testComponents, typeof(MockTokenizer), typeof(MockCharFilter), typeof(MockFixedLengthPayloadFilter), typeof(MockGraphTokenFilter), typeof(MockHoleInjectingTokenFilter), typeof(MockRandomLookaheadTokenFilter), typeof(MockTokenFilter), typeof(MockVariableLengthPayloadFilter), typeof(ValidatingTokenFilter), typeof(CrankyTokenFilter));
		Collections.addAll<Type>(crazyComponents, typeof(CachingTokenFilter), typeof(TeeSinkTokenFilter));
		Collections.addAll<Type>(deprecatedDuplicatedComponents, typeof(DutchStemFilter), typeof(FrenchStemFilter), typeof(IndicTokenizer));
		Collections.addAll<Type>(oddlyNamedComponents, typeof(ReversePathHierarchyTokenizer), typeof(SnowballFilter), typeof(PatternKeywordMarkerFilter), typeof(SetKeywordMarkerFilter)); // this is called SnowballPorterFilterFactory -  this is supported via an option to PathHierarchyTokenizer's factory
	  }

	  // these are 'crazy' components like cachingtokenfilter. does it make sense to add factories for these?
	  private static readonly ISet<Type> crazyComponents = Collections.newSetFromMap(new IdentityHashMap<Type, bool?>());

	  // these are deprecated components that are just exact dups of other functionality: they dont need factories
	  // (they never had them)
	  private static readonly ISet<Type> deprecatedDuplicatedComponents = Collections.newSetFromMap(new IdentityHashMap<Type, bool?>());

	  // these are oddly-named (either the actual analyzer, or its factory)
	  // they do actually have factories.
	  // TODO: clean this up!
	  private static readonly ISet<Type> oddlyNamedComponents = Collections.newSetFromMap(new IdentityHashMap<Type, bool?>());

	  private static readonly ResourceLoader loader = new StringMockResourceLoader("");

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws Exception
	  public virtual void test()
	  {
		IList<Type> analysisClasses = new List<Type>();
		((List<Type>)analysisClasses).AddRange(TestRandomChains.getClassesForPackage("org.apache.lucene.analysis"));
		((List<Type>)analysisClasses).AddRange(TestRandomChains.getClassesForPackage("org.apache.lucene.collation"));

		foreach (Class c in analysisClasses)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int modifiers = c.getModifiers();
		  int modifiers = c.Modifiers;
		  if (Modifier.isAbstract(modifiers) || !Modifier.isPublic(modifiers) || c.Synthetic || c.AnonymousClass || c.MemberClass || c.Interface || testComponents.Contains(c) || crazyComponents.Contains(c) || oddlyNamedComponents.Contains(c) || deprecatedDuplicatedComponents.Contains(c) || c.isAnnotationPresent(typeof(Deprecated)) || !(c.IsSubclassOf(typeof(Tokenizer)) || c.IsSubclassOf(typeof(TokenFilter)) || c.IsSubclassOf(typeof(CharFilter))))
		  { // deprecated ones are typically back compat hacks
			// don't waste time with abstract classes
			continue;
		  }

		  IDictionary<string, string> args = new Dictionary<string, string>();
		  args["luceneMatchVersion"] = TEST_VERSION_CURRENT.ToString();

		  if (c.IsSubclassOf(typeof(Tokenizer)))
		  {
			string clazzName = c.SimpleName;
			assertTrue(clazzName.EndsWith("Tokenizer", StringComparison.Ordinal));
			string simpleName = clazzName.Substring(0, clazzName.Length - 9);
			assertNotNull(TokenizerFactory.lookupClass(simpleName));
			TokenizerFactory instance = null;
			try
			{
			  instance = TokenizerFactory.forName(simpleName, args);
			  assertNotNull(instance);
			  if (instance is ResourceLoaderAware)
			  {
				((ResourceLoaderAware) instance).inform(loader);
			  }
			  assertSame(c, instance.create(new StringReader("")).GetType());
			}
			catch (System.ArgumentException e)
			{
			  if (e.InnerException is NoSuchMethodException)
			  {
				// there is no corresponding ctor available
				throw e;
			  }
			  // TODO: For now pass because some factories have not yet a default config that always works
			}
		  }
		  else if (c.IsSubclassOf(typeof(TokenFilter)))
		  {
			string clazzName = c.SimpleName;
			assertTrue(clazzName.EndsWith("Filter", StringComparison.Ordinal));
			string simpleName = clazzName.Substring(0, clazzName.Length - (clazzName.EndsWith("TokenFilter", StringComparison.Ordinal) ? 11 : 6));
			assertNotNull(TokenFilterFactory.lookupClass(simpleName));
			TokenFilterFactory instance = null;
			try
			{
			  instance = TokenFilterFactory.forName(simpleName, args);
			  assertNotNull(instance);
			  if (instance is ResourceLoaderAware)
			  {
				((ResourceLoaderAware) instance).inform(loader);
			  }
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Class<? extends org.apache.lucene.analysis.TokenStream> createdClazz = instance.create(new KeywordTokenizer(new java.io.StringReader(""))).getClass();
			  Type<?> createdClazz = instance.create(new KeywordTokenizer(new StringReader(""))).GetType();
			  // only check instance if factory have wrapped at all!
			  if (typeof(KeywordTokenizer) != createdClazz)
			  {
				assertSame(c, createdClazz);
			  }
			}
			catch (System.ArgumentException e)
			{
			  if (e.InnerException is NoSuchMethodException)
			  {
				// there is no corresponding ctor available
				throw e;
			  }
			  // TODO: For now pass because some factories have not yet a default config that always works
			}
		  }
		  else if (c.IsSubclassOf(typeof(CharFilter)))
		  {
			string clazzName = c.SimpleName;
			assertTrue(clazzName.EndsWith("CharFilter", StringComparison.Ordinal));
			string simpleName = clazzName.Substring(0, clazzName.Length - 10);
			assertNotNull(CharFilterFactory.lookupClass(simpleName));
			CharFilterFactory instance = null;
			try
			{
			  instance = CharFilterFactory.forName(simpleName, args);
			  assertNotNull(instance);
			  if (instance is ResourceLoaderAware)
			  {
				((ResourceLoaderAware) instance).inform(loader);
			  }
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: Class<? extends java.io.Reader> createdClazz = instance.create(new java.io.StringReader("")).getClass();
			  Type<?> createdClazz = instance.create(new StringReader("")).GetType();
			  // only check instance if factory have wrapped at all!
			  if (typeof(StringReader) != createdClazz)
			  {
				assertSame(c, createdClazz);
			  }
			}
			catch (System.ArgumentException e)
			{
			  if (e.InnerException is NoSuchMethodException)
			  {
				// there is no corresponding ctor available
				throw e;
			  }
			  // TODO: For now pass because some factories have not yet a default config that always works
			}
		  }
		}
	  }
	}

}