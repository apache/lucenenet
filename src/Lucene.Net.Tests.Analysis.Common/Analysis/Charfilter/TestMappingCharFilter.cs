using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

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

namespace org.apache.lucene.analysis.charfilter
{


	using TestUtil = org.apache.lucene.util.TestUtil;
	using UnicodeUtil = org.apache.lucene.util.UnicodeUtil;

	public class TestMappingCharFilter : BaseTokenStreamTestCase
	{

	  internal NormalizeCharMap normMap;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();

		builder.add("aa", "a");
		builder.add("bbb", "b");
		builder.add("cccc", "cc");

		builder.add("h", "i");
		builder.add("j", "jj");
		builder.add("k", "kkk");
		builder.add("ll", "llll");

		builder.add("empty", "");

		// BMP (surrogate pair):
		builder.add(UnicodeUtil.newString(new int[] {0x1D122}, 0, 1), "fclef");

		builder.add("\uff01", "full-width-exclamation");

		normMap = builder.build();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReaderReset() throws Exception
	  public virtual void testReaderReset()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("x"));
		char[] buf = new char[10];
		int len = cs.read(buf, 0, 10);
		assertEquals(1, len);
		assertEquals('x', buf[0]);
		len = cs.read(buf, 0, 10);
		assertEquals(-1, len);

		// rewind
		cs.reset();
		len = cs.read(buf, 0, 10);
		assertEquals(1, len);
		assertEquals('x', buf[0]);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNothingChange() throws Exception
	  public virtual void testNothingChange()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("x"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"x"}, new int[]{0}, new int[]{1}, 1);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test1to1() throws Exception
	  public virtual void test1to1()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("h"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"i"}, new int[]{0}, new int[]{1}, 1);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test1to2() throws Exception
	  public virtual void test1to2()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("j"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"jj"}, new int[]{0}, new int[]{1}, 1);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test1to3() throws Exception
	  public virtual void test1to3()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("k"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"kkk"}, new int[]{0}, new int[]{1}, 1);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test2to4() throws Exception
	  public virtual void test2to4()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("ll"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"llll"}, new int[]{0}, new int[]{2}, 2);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test2to1() throws Exception
	  public virtual void test2to1()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("aa"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"a"}, new int[]{0}, new int[]{2}, 2);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test3to1() throws Exception
	  public virtual void test3to1()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("bbb"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"b"}, new int[]{0}, new int[]{3}, 3);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test4to2() throws Exception
	  public virtual void test4to2()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("cccc"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"cc"}, new int[]{0}, new int[]{4}, 4);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test5to0() throws Exception
	  public virtual void test5to0()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("empty"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[0], new int[]{}, new int[]{}, 5);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNonBMPChar() throws Exception
	  public virtual void testNonBMPChar()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader(UnicodeUtil.newString(new int[] {0x1D122}, 0, 1)));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"fclef"}, new int[]{0}, new int[]{2}, 2);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFullWidthChar() throws Exception
	  public virtual void testFullWidthChar()
	  {
		CharFilter cs = new MappingCharFilter(normMap, new StringReader("\uff01"));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"full-width-exclamation"}, new int[]{0}, new int[]{1}, 1);
	  }

	  //
	  //                1111111111222
	  //      01234567890123456789012
	  //(in)  h i j k ll cccc bbb aa
	  //
	  //                1111111111222
	  //      01234567890123456789012
	  //(out) i i jj kkk llll cc b a
	  //
	  //    h, 0, 1 =>    i, 0, 1
	  //    i, 2, 3 =>    i, 2, 3
	  //    j, 4, 5 =>   jj, 4, 5
	  //    k, 6, 7 =>  kkk, 6, 7
	  //   ll, 8,10 => llll, 8,10
	  // cccc,11,15 =>   cc,11,15
	  //  bbb,16,19 =>    b,16,19
	  //   aa,20,22 =>    a,20,22
	  //
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTokenStream() throws Exception
	  public virtual void testTokenStream()
	  {
		string testString = "h i j k ll cccc bbb aa";
		CharFilter cs = new MappingCharFilter(normMap, new StringReader(testString));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"i","i","jj","kkk","llll","cc","b","a"}, new int[]{0,2,4,6,8,11,16,20}, new int[]{1,3,5,7,10,15,19,22}, testString.Length);
	  }

	  //
	  //
	  //        0123456789
	  //(in)    aaaa ll h
	  //(out-1) aa llll i
	  //(out-2) a llllllll i
	  //
	  // aaaa,0,4 => a,0,4
	  //   ll,5,7 => llllllll,5,7
	  //    h,8,9 => i,8,9
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testChained() throws Exception
	  public virtual void testChained()
	  {
		string testString = "aaaa ll h";
		CharFilter cs = new MappingCharFilter(normMap, new MappingCharFilter(normMap, new StringReader(testString)));
		TokenStream ts = new MockTokenizer(cs, MockTokenizer.WHITESPACE, false);
		assertTokenStreamContents(ts, new string[]{"a","llllllll","i"}, new int[]{0,5,8}, new int[]{4,7,9}, testString.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandom() throws Exception
	  public virtual void testRandom()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);

		int numRounds = RANDOM_MULTIPLIER * 10000;
		checkRandomData(random(), analyzer, numRounds);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestMappingCharFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestMappingCharFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }

		  protected internal override Reader initReader(string fieldName, Reader reader)
		  {
			return new MappingCharFilter(outerInstance.normMap, reader);
		  }
	  }

	  //@Ignore("wrong finalOffset: https://issues.apache.org/jira/browse/LUCENE-3971")
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testFinalOffsetSpecialCase() throws Exception
	  public virtual void testFinalOffsetSpecialCase()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		builder.add("t", "");
		// even though this below rule has no effect, the test passes if you remove it!!
		builder.add("tmakdbl", "c");

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final NormalizeCharMap map = builder.build();
		NormalizeCharMap map = builder.build();

		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this, map);

		string text = "gzw f quaxot";
		checkAnalysisConsistency(random(), analyzer, false, text);
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestMappingCharFilter outerInstance;

		  private NormalizeCharMap map;

		  public AnalyzerAnonymousInnerClassHelper2(TestMappingCharFilter outerInstance, NormalizeCharMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }

		  protected internal override Reader initReader(string fieldName, Reader reader)
		  {
			return new MappingCharFilter(map, reader);
		  }
	  }

	  //@Ignore("wrong finalOffset: https://issues.apache.org/jira/browse/LUCENE-3971")
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomMaps() throws Exception
	  public virtual void testRandomMaps()
	  {
		int numIterations = atLeast(3);
		for (int i = 0; i < numIterations; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final NormalizeCharMap map = randomMap();
		  NormalizeCharMap map = randomMap();
		  Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper3(this, map);
		  int numRounds = 100;
		  checkRandomData(random(), analyzer, numRounds);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestMappingCharFilter outerInstance;

		  private NormalizeCharMap map;

		  public AnalyzerAnonymousInnerClassHelper3(TestMappingCharFilter outerInstance, NormalizeCharMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }

		  protected internal override Reader initReader(string fieldName, Reader reader)
		  {
			return new MappingCharFilter(map, reader);
		  }
	  }

	  private NormalizeCharMap randomMap()
	  {
		Random random = random();
		NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		// we can't add duplicate keys, or NormalizeCharMap gets angry
		ISet<string> keys = new HashSet<string>();
		int num = random.Next(5);
		//System.out.println("NormalizeCharMap=");
		for (int i = 0; i < num; i++)
		{
		  string key = TestUtil.randomSimpleString(random);
		  if (!keys.Contains(key) && key.Length != 0)
		  {
			string value = TestUtil.randomSimpleString(random);
			builder.add(key, value);
			keys.Add(key);
			//System.out.println("mapping: '" + key + "' => '" + value + "'");
		  }
		}
		return builder.build();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomMaps2() throws Exception
	  public virtual void testRandomMaps2()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Random random = random();
		Random random = random();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numIterations = atLeast(3);
		int numIterations = atLeast(3);
		for (int iter = 0;iter < numIterations;iter++)
		{

		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST iter=" + iter);
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char endLetter = (char) org.apache.lucene.util.TestUtil.nextInt(random, 'b', 'z');
		  char endLetter = (char) TestUtil.Next(random, 'b', 'z');

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String,String> map = new java.util.HashMap<>();
		  IDictionary<string, string> map = new Dictionary<string, string>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		  NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numMappings = atLeast(5);
		  int numMappings = atLeast(5);
		  if (VERBOSE)
		  {
			Console.WriteLine("  mappings:");
		  }
		  while (map.Count < numMappings)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String key = org.apache.lucene.util.TestUtil.randomSimpleStringRange(random, 'a', endLetter, 7);
			string key = TestUtil.randomSimpleStringRange(random, 'a', endLetter, 7);
			if (key.Length != 0 && !map.ContainsKey(key))
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String value = org.apache.lucene.util.TestUtil.randomSimpleString(random);
			  string value = TestUtil.randomSimpleString(random);
			  map[key] = value;
			  builder.add(key, value);
			  if (VERBOSE)
			  {
				Console.WriteLine("    " + key + " -> " + value);
			  }
			}
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final NormalizeCharMap charMap = builder.build();
		  NormalizeCharMap charMap = builder.build();

		  if (VERBOSE)
		  {
			Console.WriteLine("  test random documents...");
		  }

		  for (int iter2 = 0;iter2 < 100;iter2++)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String content = org.apache.lucene.util.TestUtil.randomSimpleStringRange(random, 'a', endLetter, atLeast(1000));
			string content = TestUtil.randomSimpleStringRange(random, 'a', endLetter, atLeast(1000));

			if (VERBOSE)
			{
			  Console.WriteLine("  content=" + content);
			}

			// Do stupid dog-slow mapping:

			// Output string:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StringBuilder output = new StringBuilder();
			StringBuilder output = new StringBuilder();

			// Maps output offset to input offset:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Integer> inputOffsets = new java.util.ArrayList<>();
			IList<int?> inputOffsets = new List<int?>();

			int cumDiff = 0;
			int charIdx = 0;
			while (charIdx < content.Length)
			{

			  int matchLen = -1;
			  string matchRepl = null;

			  foreach (KeyValuePair<string, string> ent in map.SetOfKeyValuePairs())
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String match = ent.getKey();
				string match = ent.Key;
				if (charIdx + match.Length <= content.Length)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int limit = charIdx+match.length();
				  int limit = charIdx + match.Length;
				  bool matches = true;
				  for (int charIdx2 = charIdx;charIdx2 < limit;charIdx2++)
				  {
					if (match[charIdx2 - charIdx] != content[charIdx2])
					{
					  matches = false;
					  break;
					}
				  }

				  if (matches)
				  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String repl = ent.getValue();
					string repl = ent.Value;
					if (match.Length > matchLen)
					{
					  // Greedy: longer match wins
					  matchLen = match.Length;
					  matchRepl = repl;
					}
				  }
				}
			  }

			  if (matchLen != -1)
			  {
				// We found a match here!
				if (VERBOSE)
				{
				  Console.WriteLine("    match=" + content.Substring(charIdx, matchLen) + " @ off=" + charIdx + " repl=" + matchRepl);
				}
				output.Append(matchRepl);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int minLen = Math.min(matchLen, matchRepl.length());
				int minLen = Math.Min(matchLen, matchRepl.Length);

				// Common part, directly maps back to input
				// offset:
				for (int outIdx = 0;outIdx < minLen;outIdx++)
				{
				  inputOffsets.Add(output.Length - matchRepl.Length + outIdx + cumDiff);
				}

				cumDiff += matchLen - matchRepl.Length;
				charIdx += matchLen;

				if (matchRepl.Length < matchLen)
				{
				  // Replacement string is shorter than matched
				  // input: nothing to do
				}
				else if (matchRepl.Length > matchLen)
				{
				  // Replacement string is longer than matched
				  // input: for all the "extra" chars we map
				  // back to a single input offset:
				  for (int outIdx = matchLen;outIdx < matchRepl.Length;outIdx++)
				  {
					inputOffsets.Add(output.Length + cumDiff - 1);
				  }
				}
				else
				{
				  // Same length: no change to offset
				}

				Debug.Assert(inputOffsets.Count == output.Length, "inputOffsets.size()=" + inputOffsets.Count + " vs output.length()=" + output.Length);
			  }
			  else
			  {
				inputOffsets.Add(output.Length + cumDiff);
				output.Append(content[charIdx]);
				charIdx++;
			  }
			}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String expected = output.toString();
			string expected = output.ToString();
			if (VERBOSE)
			{
			  Console.Write("    expected:");
			  for (int charIdx2 = 0;charIdx2 < expected.Length;charIdx2++)
			  {
				Console.Write(" " + expected[charIdx2] + "/" + inputOffsets[charIdx2]);
			  }
			  Console.WriteLine();
			}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final MappingCharFilter mapFilter = new MappingCharFilter(charMap, new java.io.StringReader(content));
			MappingCharFilter mapFilter = new MappingCharFilter(charMap, new StringReader(content));

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StringBuilder actualBuilder = new StringBuilder();
			StringBuilder actualBuilder = new StringBuilder();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Integer> actualInputOffsets = new java.util.ArrayList<>();
			IList<int?> actualInputOffsets = new List<int?>();

			// Now consume the actual mapFilter, somewhat randomly:
			while (true)
			{
			  if (random.nextBoolean())
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ch = mapFilter.read();
				int ch = mapFilter.read();
				if (ch == -1)
				{
				  break;
				}
				actualBuilder.Append((char) ch);
			  }
			  else
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] buffer = new char[org.apache.lucene.util.TestUtil.nextInt(random, 1, 100)];
				char[] buffer = new char[TestUtil.Next(random, 1, 100)];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int off = buffer.length == 1 ? 0 : random.nextInt(buffer.length-1);
				int off = buffer.Length == 1 ? 0 : random.Next(buffer.Length - 1);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int count = mapFilter.read(buffer, off, buffer.length-off);
				int count = mapFilter.read(buffer, off, buffer.Length - off);
				if (count == -1)
				{
				  break;
				}
				else
				{
				  actualBuilder.Append(buffer, off, count);
				}
			  }

			  if (random.Next(10) == 7)
			  {
				// Map offsets
				while (actualInputOffsets.Count < actualBuilder.Length)
				{
				  actualInputOffsets.Add(mapFilter.correctOffset(actualInputOffsets.Count));
				}
			  }
			}

			// Finish mappping offsets
			while (actualInputOffsets.Count < actualBuilder.Length)
			{
			  actualInputOffsets.Add(mapFilter.correctOffset(actualInputOffsets.Count));
			}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String actual = actualBuilder.toString();
			string actual = actualBuilder.ToString();

			// Verify:
			assertEquals(expected, actual);
			assertEquals(inputOffsets, actualInputOffsets);
		  }
		}
	  }
	}

}