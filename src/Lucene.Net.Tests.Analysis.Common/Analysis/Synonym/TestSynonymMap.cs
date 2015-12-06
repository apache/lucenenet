using System;
using System.Collections.Generic;

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


	using NGramTokenizerFactory = org.apache.lucene.analysis.ngram.NGramTokenizerFactory;
	using AbstractAnalysisFactory = org.apache.lucene.analysis.util.AbstractAnalysisFactory;
	using TokenizerFactory = org.apache.lucene.analysis.util.TokenizerFactory;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using ResourceLoader = org.apache.lucene.analysis.util.ResourceLoader;

	/// @deprecated Remove this test in Lucene 5.0 
	[Obsolete("Remove this test in Lucene 5.0")]
	public class TestSynonymMap : LuceneTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInvalidMappingRules() throws Exception
	  public virtual void testInvalidMappingRules()
	  {
		SlowSynonymMap synMap = new SlowSynonymMap(true);
		IList<string> rules = new List<string>(1);
		rules.Add("a=>b=>c");
		try
		{
			SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
			fail("IllegalArgumentException must be thrown.");
		}
		catch (System.ArgumentException)
		{
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReadMappingRules() throws Exception
	  public virtual void testReadMappingRules()
	  {
		SlowSynonymMap synMap;

		// (a)->[b]
		IList<string> rules = new List<string>();
		rules.Add("a=>b");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(1, synMap.submap.size());
		assertTokIncludes(synMap, "a", "b");

		// (a)->[c]
		// (b)->[c]
		rules.Clear();
		rules.Add("a,b=>c");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(2, synMap.submap.size());
		assertTokIncludes(synMap, "a", "c");
		assertTokIncludes(synMap, "b", "c");

		// (a)->[b][c]
		rules.Clear();
		rules.Add("a=>b,c");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(1, synMap.submap.size());
		assertTokIncludes(synMap, "a", "b");
		assertTokIncludes(synMap, "a", "c");

		// (a)->(b)->[a2]
		//      [a1]
		rules.Clear();
		rules.Add("a=>a1");
		rules.Add("a b=>a2");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(1, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a1");
		assertEquals(1, getSubSynonymMap(synMap, "a").submap.size());
		assertTokIncludes(getSubSynonymMap(synMap, "a"), "b", "a2");

		// (a)->(b)->[a2]
		//      (c)->[a3]
		//      [a1]
		rules.Clear();
		rules.Add("a=>a1");
		rules.Add("a b=>a2");
		rules.Add("a c=>a3");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(1, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a1");
		assertEquals(2, getSubSynonymMap(synMap, "a").submap.size());
		assertTokIncludes(getSubSynonymMap(synMap, "a"), "b", "a2");
		assertTokIncludes(getSubSynonymMap(synMap, "a"), "c", "a3");

		// (a)->(b)->[a2]
		//      [a1]
		// (b)->(c)->[b2]
		//      [b1]
		rules.Clear();
		rules.Add("a=>a1");
		rules.Add("a b=>a2");
		rules.Add("b=>b1");
		rules.Add("b c=>b2");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(2, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a1");
		assertEquals(1, getSubSynonymMap(synMap, "a").submap.size());
		assertTokIncludes(getSubSynonymMap(synMap, "a"), "b", "a2");
		assertTokIncludes(synMap, "b", "b1");
		assertEquals(1, getSubSynonymMap(synMap, "b").submap.size());
		assertTokIncludes(getSubSynonymMap(synMap, "b"), "c", "b2");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRead1waySynonymRules() throws Exception
	  public virtual void testRead1waySynonymRules()
	  {
		SlowSynonymMap synMap;

		// (a)->[a]
		// (b)->[a]
		IList<string> rules = new List<string>();
		rules.Add("a,b");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", false, null);
		assertEquals(2, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a");
		assertTokIncludes(synMap, "b", "a");

		// (a)->[a]
		// (b)->[a]
		// (c)->[a]
		rules.Clear();
		rules.Add("a,b,c");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", false, null);
		assertEquals(3, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a");
		assertTokIncludes(synMap, "b", "a");
		assertTokIncludes(synMap, "c", "a");

		// (a)->[a]
		// (b1)->(b2)->[a]
		rules.Clear();
		rules.Add("a,b1 b2");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", false, null);
		assertEquals(2, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a");
		assertEquals(1, getSubSynonymMap(synMap, "b1").submap.size());
		assertTokIncludes(getSubSynonymMap(synMap, "b1"), "b2", "a");

		// (a1)->(a2)->[a1][a2]
		// (b)->[a1][a2]
		rules.Clear();
		rules.Add("a1 a2,b");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", false, null);
		assertEquals(2, synMap.submap.size());
		assertEquals(1, getSubSynonymMap(synMap, "a1").submap.size());
		assertTokIncludes(getSubSynonymMap(synMap, "a1"), "a2", "a1");
		assertTokIncludes(getSubSynonymMap(synMap, "a1"), "a2", "a2");
		assertTokIncludes(synMap, "b", "a1");
		assertTokIncludes(synMap, "b", "a2");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRead2waySynonymRules() throws Exception
	  public virtual void testRead2waySynonymRules()
	  {
		SlowSynonymMap synMap;

		// (a)->[a][b]
		// (b)->[a][b]
		IList<string> rules = new List<string>();
		rules.Add("a,b");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(2, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a");
		assertTokIncludes(synMap, "a", "b");
		assertTokIncludes(synMap, "b", "a");
		assertTokIncludes(synMap, "b", "b");

		// (a)->[a][b][c]
		// (b)->[a][b][c]
		// (c)->[a][b][c]
		rules.Clear();
		rules.Add("a,b,c");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(3, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a");
		assertTokIncludes(synMap, "a", "b");
		assertTokIncludes(synMap, "a", "c");
		assertTokIncludes(synMap, "b", "a");
		assertTokIncludes(synMap, "b", "b");
		assertTokIncludes(synMap, "b", "c");
		assertTokIncludes(synMap, "c", "a");
		assertTokIncludes(synMap, "c", "b");
		assertTokIncludes(synMap, "c", "c");

		// (a)->[a]
		//      [b1][b2]
		// (b1)->(b2)->[a]
		//             [b1][b2]
		rules.Clear();
		rules.Add("a,b1 b2");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(2, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a");
		assertTokIncludes(synMap, "a", "b1");
		assertTokIncludes(synMap, "a", "b2");
		assertEquals(1, getSubSynonymMap(synMap, "b1").submap.size());
		assertTokIncludes(getSubSynonymMap(synMap, "b1"), "b2", "a");
		assertTokIncludes(getSubSynonymMap(synMap, "b1"), "b2", "b1");
		assertTokIncludes(getSubSynonymMap(synMap, "b1"), "b2", "b2");

		// (a1)->(a2)->[a1][a2]
		//             [b]
		// (b)->[a1][a2]
		//      [b]
		rules.Clear();
		rules.Add("a1 a2,b");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, null);
		assertEquals(2, synMap.submap.size());
		assertEquals(1, getSubSynonymMap(synMap, "a1").submap.size());
		assertTokIncludes(getSubSynonymMap(synMap, "a1"), "a2", "a1");
		assertTokIncludes(getSubSynonymMap(synMap, "a1"), "a2", "a2");
		assertTokIncludes(getSubSynonymMap(synMap, "a1"), "a2", "b");
		assertTokIncludes(synMap, "b", "a1");
		assertTokIncludes(synMap, "b", "a2");
		assertTokIncludes(synMap, "b", "b");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBigramTokenizer() throws Exception
	  public virtual void testBigramTokenizer()
	  {
		SlowSynonymMap synMap;

		// prepare bi-gram tokenizer factory
		IDictionary<string, string> args = new Dictionary<string, string>();
		args[AbstractAnalysisFactory.LUCENE_MATCH_VERSION_PARAM] = "4.4";
		args["minGramSize"] = "2";
		args["maxGramSize"] = "2";
		TokenizerFactory tf = new NGramTokenizerFactory(args);

		// (ab)->(bc)->(cd)->[ef][fg][gh]
		IList<string> rules = new List<string>();
		rules.Add("abcd=>efgh");
		synMap = new SlowSynonymMap(true);
		SlowSynonymFilterFactory.parseRules(rules, synMap, "=>", ",", true, tf);
		assertEquals(1, synMap.submap.size());
		assertEquals(1, getSubSynonymMap(synMap, "ab").submap.size());
		assertEquals(1, getSubSynonymMap(getSubSynonymMap(synMap, "ab"), "bc").submap.size());
		assertTokIncludes(getSubSynonymMap(getSubSynonymMap(synMap, "ab"), "bc"), "cd", "ef");
		assertTokIncludes(getSubSynonymMap(getSubSynonymMap(synMap, "ab"), "bc"), "cd", "fg");
		assertTokIncludes(getSubSynonymMap(getSubSynonymMap(synMap, "ab"), "bc"), "cd", "gh");
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLoadRules() throws Exception
	  public virtual void testLoadRules()
	  {
		IDictionary<string, string> args = new Dictionary<string, string>();
		args["synonyms"] = "something.txt";
		SlowSynonymFilterFactory ff = new SlowSynonymFilterFactory(args);
		ff.inform(new ResourceLoaderAnonymousInnerClassHelper(this));

		SlowSynonymMap synMap = ff.SynonymMap;
		assertEquals(2, synMap.submap.size());
		assertTokIncludes(synMap, "a", "a");
		assertTokIncludes(synMap, "a", "b");
		assertTokIncludes(synMap, "b", "a");
		assertTokIncludes(synMap, "b", "b");
	  }

	  private class ResourceLoaderAnonymousInnerClassHelper : ResourceLoader
	  {
		  private readonly TestSynonymMap outerInstance;

		  public ResourceLoaderAnonymousInnerClassHelper(TestSynonymMap outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  public override T newInstance<T>(string cname, Type<T> expectedType)
		  {
			throw new Exception("stub");
		  }

//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: @Override public <T> Class<? extends T> findClass(String cname, Class<T> expectedType)
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: @Override public <T> Class<? extends T> findClass(String cname, Class<T> expectedType)
		  public override Type<?> findClass<T>(string cname, Type<T> expectedType) where ? : T
		  {
			throw new Exception("stub");
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public java.io.InputStream openResource(String resource) throws java.io.IOException
		  public override System.IO.Stream openResource(string resource)
		  {
			if (!"something.txt".Equals(resource))
			{
			  throw new Exception("should not get a differnt resource");
			}
			else
			{
			  return new ByteArrayInputStream("a,b".GetBytes("UTF-8"));
			}
		  }
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void assertTokIncludes(SlowSynonymMap map, String src, String exp) throws Exception
	  private void assertTokIncludes(SlowSynonymMap map, string src, string exp)
	  {
		Token[] tokens = map.submap.get(src).synonyms;
		bool inc = false;
		foreach (Token token in tokens)
		{
		  if (exp.Equals(new string(token.buffer(), 0, token.length())))
		  {
			inc = true;
		  }
		}
		assertTrue(inc);
	  }

	  private SlowSynonymMap getSubSynonymMap(SlowSynonymMap map, string src)
	  {
		return map.submap.get(src);
	  }
	}

}