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


	using org.apache.lucene.analysis.tokenattributes;

	/// @deprecated Remove this test in Lucene 5.0 
	[Obsolete("Remove this test in Lucene 5.0")]
	public class TestSlowSynonymFilter : BaseTokenStreamTestCase
	{

	  internal static IList<string> strings(string str)
	  {
		string[] arr = str.Split(" ", true);
		return Arrays.asList(arr);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void assertTokenizesTo(SlowSynonymMap dict, String input, String expected[]) throws java.io.IOException
	  internal static void assertTokenizesTo(SlowSynonymMap dict, string input, string[] expected)
	  {
		Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		SlowSynonymFilter stream = new SlowSynonymFilter(tokenizer, dict);
		assertTokenStreamContents(stream, expected);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void assertTokenizesTo(SlowSynonymMap dict, String input, String expected[], int posIncs[]) throws java.io.IOException
	  internal static void assertTokenizesTo(SlowSynonymMap dict, string input, string[] expected, int[] posIncs)
	  {
		Tokenizer tokenizer = new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false);
		SlowSynonymFilter stream = new SlowSynonymFilter(tokenizer, dict);
		assertTokenStreamContents(stream, expected, posIncs);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void assertTokenizesTo(SlowSynonymMap dict, java.util.List<org.apache.lucene.analysis.Token> input, String expected[], int posIncs[]) throws java.io.IOException
	  internal static void assertTokenizesTo(SlowSynonymMap dict, IList<Token> input, string[] expected, int[] posIncs)
	  {
		TokenStream tokenizer = new IterTokenStream(input);
		SlowSynonymFilter stream = new SlowSynonymFilter(tokenizer, dict);
		assertTokenStreamContents(stream, expected, posIncs);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void assertTokenizesTo(SlowSynonymMap dict, java.util.List<org.apache.lucene.analysis.Token> input, String expected[], int startOffsets[], int endOffsets[], int posIncs[]) throws java.io.IOException
	  internal static void assertTokenizesTo(SlowSynonymMap dict, IList<Token> input, string[] expected, int[] startOffsets, int[] endOffsets, int[] posIncs)
	  {
		TokenStream tokenizer = new IterTokenStream(input);
		SlowSynonymFilter stream = new SlowSynonymFilter(tokenizer, dict);
		assertTokenStreamContents(stream, expected, startOffsets, endOffsets, posIncs);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMatching() throws java.io.IOException
	  public virtual void testMatching()
	  {
		SlowSynonymMap map = new SlowSynonymMap();

		bool orig = false;
		bool merge = true;
		map.add(strings("a b"), tokens("ab"), orig, merge);
		map.add(strings("a c"), tokens("ac"), orig, merge);
		map.add(strings("a"), tokens("aa"), orig, merge);
		map.add(strings("b"), tokens("bb"), orig, merge);
		map.add(strings("z x c v"), tokens("zxcv"), orig, merge);
		map.add(strings("x c"), tokens("xc"), orig, merge);

		assertTokenizesTo(map, "$", new string[] {"$"});
		assertTokenizesTo(map, "a", new string[] {"aa"});
		assertTokenizesTo(map, "a $", new string[] {"aa", "$"});
		assertTokenizesTo(map, "$ a", new string[] {"$", "aa"});
		assertTokenizesTo(map, "a a", new string[] {"aa", "aa"});
		assertTokenizesTo(map, "b", new string[] {"bb"});
		assertTokenizesTo(map, "z x c v", new string[] {"zxcv"});
		assertTokenizesTo(map, "z x c $", new string[] {"z", "xc", "$"});

		// repeats
		map.add(strings("a b"), tokens("ab"), orig, merge);
		map.add(strings("a b"), tokens("ab"), orig, merge);

		// FIXME: the below test intended to be { "ab" }
		assertTokenizesTo(map, "a b", new string[] {"ab", "ab", "ab"});

		// check for lack of recursion
		map.add(strings("zoo"), tokens("zoo"), orig, merge);
		assertTokenizesTo(map, "zoo zoo $ zoo", new string[] {"zoo", "zoo", "$", "zoo"});
		map.add(strings("zoo"), tokens("zoo zoo"), orig, merge);
		// FIXME: the below test intended to be { "zoo", "zoo", "zoo", "zoo", "$", "zoo", "zoo" }
		// maybe this was just a typo in the old test????
		assertTokenizesTo(map, "zoo zoo $ zoo", new string[] {"zoo", "zoo", "zoo", "zoo", "zoo", "zoo", "$", "zoo", "zoo", "zoo"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIncludeOrig() throws java.io.IOException
	  public virtual void testIncludeOrig()
	  {
		SlowSynonymMap map = new SlowSynonymMap();

		bool orig = true;
		bool merge = true;
		map.add(strings("a b"), tokens("ab"), orig, merge);
		map.add(strings("a c"), tokens("ac"), orig, merge);
		map.add(strings("a"), tokens("aa"), orig, merge);
		map.add(strings("b"), tokens("bb"), orig, merge);
		map.add(strings("z x c v"), tokens("zxcv"), orig, merge);
		map.add(strings("x c"), tokens("xc"), orig, merge);

		assertTokenizesTo(map, "$", new string[] {"$"}, new int[] {1});
		assertTokenizesTo(map, "a", new string[] {"a", "aa"}, new int[] {1, 0});
		assertTokenizesTo(map, "a", new string[] {"a", "aa"}, new int[] {1, 0});
		assertTokenizesTo(map, "$ a", new string[] {"$", "a", "aa"}, new int[] {1, 1, 0});
		assertTokenizesTo(map, "a $", new string[] {"a", "aa", "$"}, new int[] {1, 0, 1});
		assertTokenizesTo(map, "$ a !", new string[] {"$", "a", "aa", "!"}, new int[] {1, 1, 0, 1});
		assertTokenizesTo(map, "a a", new string[] {"a", "aa", "a", "aa"}, new int[] {1, 0, 1, 0});
		assertTokenizesTo(map, "b", new string[] {"b", "bb"}, new int[] {1, 0});
		assertTokenizesTo(map, "z x c v", new string[] {"z", "zxcv", "x", "c", "v"}, new int[] {1, 0, 1, 1, 1});
		assertTokenizesTo(map, "z x c $", new string[] {"z", "x", "xc", "c", "$"}, new int[] {1, 1, 0, 1, 1});

		// check for lack of recursion
		map.add(strings("zoo zoo"), tokens("zoo"), orig, merge);
		// CHECKME: I think the previous test (with 4 zoo's), was just a typo.
		assertTokenizesTo(map, "zoo zoo $ zoo", new string[] {"zoo", "zoo", "zoo", "$", "zoo"}, new int[] {1, 0, 1, 1, 1});

		map.add(strings("zoo"), tokens("zoo zoo"), orig, merge);
		assertTokenizesTo(map, "zoo zoo $ zoo", new string[] {"zoo", "zoo", "zoo", "$", "zoo", "zoo", "zoo"}, new int[] {1, 0, 1, 1, 1, 0, 1});
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMapMerge() throws java.io.IOException
	  public virtual void testMapMerge()
	  {
		SlowSynonymMap map = new SlowSynonymMap();

		bool orig = false;
		bool merge = true;
		map.add(strings("a"), tokens("a5,5"), orig, merge);
		map.add(strings("a"), tokens("a3,3"), orig, merge);

		assertTokenizesTo(map, "a", new string[] {"a3", "a5"}, new int[] {1, 2});

		map.add(strings("b"), tokens("b3,3"), orig, merge);
		map.add(strings("b"), tokens("b5,5"), orig, merge);

		assertTokenizesTo(map, "b", new string[] {"b3", "b5"}, new int[] {1, 2});

		map.add(strings("a"), tokens("A3,3"), orig, merge);
		map.add(strings("a"), tokens("A5,5"), orig, merge);

		assertTokenizesTo(map, "a", new string[] {"a3", "A3", "a5", "A5"}, new int[] {1, 0, 2, 0});

		map.add(strings("a"), tokens("a1"), orig, merge);
		assertTokenizesTo(map, "a", new string[] {"a1", "a3", "A3", "a5", "A5"}, new int[] {1, 2, 0, 2, 0});

		map.add(strings("a"), tokens("a2,2"), orig, merge);
		map.add(strings("a"), tokens("a4,4 a6,2"), orig, merge);
		assertTokenizesTo(map, "a", new string[] {"a1", "a2", "a3", "A3", "a4", "a5", "A5", "a6"}, new int[] {1, 1, 1, 0, 1, 1, 0, 1});
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOverlap() throws java.io.IOException
	  public virtual void testOverlap()
	  {
		SlowSynonymMap map = new SlowSynonymMap();

		bool orig = false;
		bool merge = true;
		map.add(strings("qwe"), tokens("qq/ww/ee"), orig, merge);
		map.add(strings("qwe"), tokens("xx"), orig, merge);
		map.add(strings("qwe"), tokens("yy"), orig, merge);
		map.add(strings("qwe"), tokens("zz"), orig, merge);
		assertTokenizesTo(map, "$", new string[] {"$"});
		assertTokenizesTo(map, "qwe", new string[] {"qq", "ww", "ee", "xx", "yy", "zz"}, new int[] {1, 0, 0, 0, 0, 0});

		// test merging within the map

		map.add(strings("a"), tokens("a5,5 a8,3 a10,2"), orig, merge);
		map.add(strings("a"), tokens("a3,3 a7,4 a9,2 a11,2 a111,100"), orig, merge);
		assertTokenizesTo(map, "a", new string[] {"a3", "a5", "a7", "a8", "a9", "a10", "a11", "a111"}, new int[] {1, 2, 2, 1, 1, 1, 1, 100});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPositionIncrements() throws java.io.IOException
	  public virtual void testPositionIncrements()
	  {
		SlowSynonymMap map = new SlowSynonymMap();

		bool orig = false;
		bool merge = true;

		// test that generated tokens start at the same posInc as the original
		map.add(strings("a"), tokens("aa"), orig, merge);
		assertTokenizesTo(map, tokens("a,5"), new string[] {"aa"}, new int[] {5});
		assertTokenizesTo(map, tokens("b,1 a,0"), new string[] {"b", "aa"}, new int[] {1, 0});

		// test that offset of first replacement is ignored (always takes the orig offset)
		map.add(strings("b"), tokens("bb,100"), orig, merge);
		assertTokenizesTo(map, tokens("b,5"), new string[] {"bb"}, new int[] {5});
		assertTokenizesTo(map, tokens("c,1 b,0"), new string[] {"c", "bb"}, new int[] {1, 0});

		// test that subsequent tokens are adjusted accordingly
		map.add(strings("c"), tokens("cc,100 c2,2"), orig, merge);
		assertTokenizesTo(map, tokens("c,5"), new string[] {"cc", "c2"}, new int[] {5, 2});
		assertTokenizesTo(map, tokens("d,1 c,0"), new string[] {"d", "cc", "c2"}, new int[] {1, 0, 2});
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testPositionIncrementsWithOrig() throws java.io.IOException
	  public virtual void testPositionIncrementsWithOrig()
	  {
		SlowSynonymMap map = new SlowSynonymMap();

		bool orig = true;
		bool merge = true;

		// test that generated tokens start at the same offset as the original
		map.add(strings("a"), tokens("aa"), orig, merge);
		assertTokenizesTo(map, tokens("a,5"), new string[] {"a", "aa"}, new int[] {5, 0});
		assertTokenizesTo(map, tokens("b,1 a,0"), new string[] {"b", "a", "aa"}, new int[] {1, 0, 0});

		// test that offset of first replacement is ignored (always takes the orig offset)
		map.add(strings("b"), tokens("bb,100"), orig, merge);
		assertTokenizesTo(map, tokens("b,5"), new string[] {"b", "bb"}, new int[] {5, 0});
		assertTokenizesTo(map, tokens("c,1 b,0"), new string[] {"c", "b", "bb"}, new int[] {1, 0, 0});

		// test that subsequent tokens are adjusted accordingly
		map.add(strings("c"), tokens("cc,100 c2,2"), orig, merge);
		assertTokenizesTo(map, tokens("c,5"), new string[] {"c", "cc", "c2"}, new int[] {5, 0, 2});
		assertTokenizesTo(map, tokens("d,1 c,0"), new string[] {"d", "c", "cc", "c2"}, new int[] {1, 0, 0, 2});
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOffsetBug() throws java.io.IOException
	  public virtual void testOffsetBug()
	  {
		// With the following rules:
		// a a=>b
		// x=>y
		// analysing "a x" causes "y" to have a bad offset (end less than start)
		// SOLR-167
		SlowSynonymMap map = new SlowSynonymMap();

		bool orig = false;
		bool merge = true;

		map.add(strings("a a"), tokens("b"), orig, merge);
		map.add(strings("x"), tokens("y"), orig, merge);

		// "a a x" => "b y"
		assertTokenizesTo(map, tokens("a,1,0,1 a,1,2,3 x,1,4,5"), new string[] {"b", "y"}, new int[] {0, 4}, new int[] {3, 5}, new int[] {1, 1});
	  }


	  /// <summary>
	  ///*
	  /// Return a list of tokens according to a test string format:
	  /// a b c  =>  returns List<Token> [a,b,c]
	  /// a/b   => tokens a and b share the same spot (b.positionIncrement=0)
	  /// a,3/b/c => a,b,c all share same position (a.positionIncrement=3, b.positionIncrement=0, c.positionIncrement=0)
	  /// a,1,10,11  => "a" with positionIncrement=1, startOffset=10, endOffset=11 </summary>
	  /// @deprecated (3.0) does not support attributes api 
	  [Obsolete("(3.0) does not support attributes api")]
	  private IList<Token> tokens(string str)
	  {
		string[] arr = str.Split(" ", true);
		IList<Token> result = new List<Token>();
		for (int i = 0; i < arr.Length; i++)
		{
		  string[] toks = arr[i].Split("/", true);
		  string[] @params = toks[0].Split(",", true);

		  int posInc;
		  int start;
		  int end;

		  if (@params.Length > 1)
		  {
			posInc = int.Parse(@params[1]);
		  }
		  else
		  {
			posInc = 1;
		  }

		  if (@params.Length > 2)
		  {
			start = int.Parse(@params[2]);
		  }
		  else
		  {
			start = 0;
		  }

		  if (@params.Length > 3)
		  {
			end = int.Parse(@params[3]);
		  }
		  else
		  {
			end = start + @params[0].Length;
		  }

		  Token t = new Token(@params[0],start,end,"TEST");
		  t.PositionIncrement = posInc;

		  result.Add(t);
		  for (int j = 1; j < toks.Length; j++)
		  {
			t = new Token(toks[j],0,0,"TEST");
			t.PositionIncrement = 0;
			result.Add(t);
		  }
		}
		return result;
	  }

	  /// @deprecated (3.0) does not support custom attributes 
	  [Obsolete("(3.0) does not support custom attributes")]
	  private class IterTokenStream : TokenStream
	  {
		internal readonly Token[] tokens;
		internal int index = 0;
		internal CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		internal OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
		internal PositionIncrementAttribute posIncAtt = addAttribute(typeof(PositionIncrementAttribute));
		internal FlagsAttribute flagsAtt = addAttribute(typeof(FlagsAttribute));
		internal TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));
		internal PayloadAttribute payloadAtt = addAttribute(typeof(PayloadAttribute));

		public IterTokenStream(params Token[] tokens) : base()
		{
		  this.tokens = tokens;
		}

		public IterTokenStream(ICollection<Token> tokens) : this(tokens.toArray(new Token[tokens.Count]))
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (index >= tokens.Length)
		  {
			return false;
		  }
		  else
		  {
			clearAttributes();
			Token token = tokens[index++];
			termAtt.setEmpty().append(token);
			offsetAtt.setOffset(token.startOffset(), token.endOffset());
			posIncAtt.PositionIncrement = token.PositionIncrement;
			flagsAtt.Flags = token.Flags;
			typeAtt.Type = token.type();
			payloadAtt.Payload = token.Payload;
			return true;
		  }
		}
	  }
	}

}