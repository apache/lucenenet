using System.Collections.Generic;
using System.Text;

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

	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using WhitespaceTokenizer = org.apache.lucene.analysis.core.WhitespaceTokenizer;
	using PorterStemFilter = org.apache.lucene.analysis.en.PorterStemFilter;
	using StemmerOverrideMap = org.apache.lucene.analysis.miscellaneous.StemmerOverrideFilter.StemmerOverrideMap;
	using TestUtil = org.apache.lucene.util.TestUtil;

	/// 
	public class TestStemmerOverrideFilter : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOverride() throws java.io.IOException
	  public virtual void testOverride()
	  {
		// lets make booked stem to books
		// the override filter will convert "booked" to "books",
		// but also mark it with KeywordAttribute so Porter will not change it.
		StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder();
		builder.add("booked", "books");
		Tokenizer tokenizer = new KeywordTokenizer(new StringReader("booked"));
		TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.build()));
		assertTokenStreamContents(stream, new string[] {"books"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIgnoreCase() throws java.io.IOException
	  public virtual void testIgnoreCase()
	  {
		// lets make booked stem to books
		// the override filter will convert "booked" to "books",
		// but also mark it with KeywordAttribute so Porter will not change it.
		StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(true);
		builder.add("boOkEd", "books");
		Tokenizer tokenizer = new KeywordTokenizer(new StringReader("BooKeD"));
		TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.build()));
		assertTokenStreamContents(stream, new string[] {"books"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoOverrides() throws java.io.IOException
	  public virtual void testNoOverrides()
	  {
		StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(true);
		Tokenizer tokenizer = new KeywordTokenizer(new StringReader("book"));
		TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.build()));
		assertTokenStreamContents(stream, new string[] {"book"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomRealisticWhiteSpace() throws java.io.IOException
	  public virtual void testRandomRealisticWhiteSpace()
	  {
		IDictionary<string, string> map = new Dictionary<string, string>();
		int numTerms = atLeast(50);
		for (int i = 0; i < numTerms; i++)
		{
		  string randomRealisticUnicodeString = TestUtil.randomRealisticUnicodeString(random());
		  char[] charArray = randomRealisticUnicodeString.ToCharArray();
		  StringBuilder builder = new StringBuilder();
		  for (int j = 0; j < charArray.Length;)
		  {
			int cp = char.codePointAt(charArray, j, charArray.Length);
			if (!char.IsWhiteSpace(cp))
			{
			  builder.appendCodePoint(cp);
			}
			j += char.charCount(cp);
		  }
		  if (builder.Length > 0)
		  {
			string value = TestUtil.randomSimpleString(random());
			map[builder.ToString()] = value.Length == 0 ? "a" : value;

		  }
		}
		if (map.Count == 0)
		{
		  map["booked"] = "books";
		}
		StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(random().nextBoolean());
		ISet<KeyValuePair<string, string>> entrySet = map.SetOfKeyValuePairs();
		StringBuilder input = new StringBuilder();
		IList<string> output = new List<string>();
		foreach (KeyValuePair<string, string> entry in entrySet)
		{
		  builder.add(entry.Key, entry.Value);
		  if (random().nextBoolean() || output.Count == 0)
		  {
			input.Append(entry.Key).Append(" ");
			output.Add(entry.Value);
		  }
		}
		Tokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(input.ToString()));
		TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, builder.build()));
		assertTokenStreamContents(stream, output.ToArray());
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomRealisticKeyword() throws java.io.IOException
	  public virtual void testRandomRealisticKeyword()
	  {
		IDictionary<string, string> map = new Dictionary<string, string>();
		int numTerms = atLeast(50);
		for (int i = 0; i < numTerms; i++)
		{
		  string randomRealisticUnicodeString = TestUtil.randomRealisticUnicodeString(random());
		  if (randomRealisticUnicodeString.Length > 0)
		  {
			string value = TestUtil.randomSimpleString(random());
			map[randomRealisticUnicodeString] = value.Length == 0 ? "a" : value;
		  }
		}
		if (map.Count == 0)
		{
		  map["booked"] = "books";
		}
		StemmerOverrideFilter.Builder builder = new StemmerOverrideFilter.Builder(random().nextBoolean());
		ISet<KeyValuePair<string, string>> entrySet = map.SetOfKeyValuePairs();
		foreach (KeyValuePair<string, string> entry in entrySet)
		{
		  builder.add(entry.Key, entry.Value);
		}
		StemmerOverrideMap build = builder.build();
		foreach (KeyValuePair<string, string> entry in entrySet)
		{
		  if (random().nextBoolean())
		  {
			Tokenizer tokenizer = new KeywordTokenizer(new StringReader(entry.Key));
			TokenStream stream = new PorterStemFilter(new StemmerOverrideFilter(tokenizer, build));
			assertTokenStreamContents(stream, new string[] {entry.Value});
		  }
		}
	  }
	}

}