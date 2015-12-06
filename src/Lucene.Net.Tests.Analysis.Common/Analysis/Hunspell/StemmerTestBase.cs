using System.Collections.Generic;

namespace org.apache.lucene.analysis.hunspell
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


	using CharsRef = org.apache.lucene.util.CharsRef;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;

	/// <summary>
	/// base class for hunspell stemmer tests </summary>
	internal abstract class StemmerTestBase : LuceneTestCase
	{
	  private static Stemmer stemmer;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void init(String affix, String dictionary) throws java.io.IOException, java.text.ParseException
	  internal static void init(string affix, string dictionary)
	  {
		init(false, affix, dictionary);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void init(boolean ignoreCase, String affix, String... dictionaries) throws java.io.IOException, java.text.ParseException
	  internal static void init(bool ignoreCase, string affix, params string[] dictionaries)
	  {
		if (dictionaries.Length == 0)
		{
		  throw new System.ArgumentException("there must be at least one dictionary");
		}

		System.IO.Stream affixStream = typeof(StemmerTestBase).getResourceAsStream(affix);
		if (affixStream == null)
		{
		  throw new FileNotFoundException("file not found: " + affix);
		}

		System.IO.Stream[] dictStreams = new System.IO.Stream[dictionaries.Length];
		for (int i = 0; i < dictionaries.Length; i++)
		{
		  dictStreams[i] = typeof(StemmerTestBase).getResourceAsStream(dictionaries[i]);
		  if (dictStreams[i] == null)
		  {
			throw new FileNotFoundException("file not found: " + dictStreams[i]);
		  }
		}

		try
		{
		  Dictionary dictionary = new Dictionary(affixStream, Arrays.asList(dictStreams), ignoreCase);
		  stemmer = new Stemmer(dictionary);
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(affixStream);
		  IOUtils.closeWhileHandlingException(dictStreams);
		}
	  }

	  internal static void assertStemsTo(string s, params string[] expected)
	  {
		assertNotNull(stemmer);
		Arrays.sort(expected);

		IList<CharsRef> stems = stemmer.stem(s);
		string[] actual = new string[stems.Count];
		for (int i = 0; i < actual.Length; i++)
		{
		  actual[i] = stems[i].ToString();
		}
		Arrays.sort(actual);

		assertArrayEquals("expected=" + Arrays.ToString(expected) + ",actual=" + Arrays.ToString(actual), expected, actual);
	  }
	}

}