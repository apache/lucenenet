namespace org.apache.lucene.analysis.pattern
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


	public class TestPatternCaptureGroupTokenFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoPattern() throws Exception
	  public virtual void testNoPattern()
	  {
		testPatterns("foobarbaz", new string[] {}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, false);
		testPatterns("foobarbaz", new string[] {}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, true);

		testPatterns("foo bar baz", new string[] {}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, false);

		testPatterns("foo bar baz", new string[] {}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoMatch() throws Exception
	  public virtual void testNoMatch()
	  {
		testPatterns("foobarbaz", new string[] {"xx"}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, false);
		testPatterns("foobarbaz", new string[] {"xx"}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, true);

		testPatterns("foo bar baz", new string[] {"xx"}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, false);

		testPatterns("foo bar baz", new string[] {"xx"}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoCapture() throws Exception
	  public virtual void testNoCapture()
	  {
		testPatterns("foobarbaz", new string[] {".."}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, false);
		testPatterns("foobarbaz", new string[] {".."}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, true);

		testPatterns("foo bar baz", new string[] {".."}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, false);

		testPatterns("foo bar baz", new string[] {".."}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyCapture() throws Exception
	  public virtual void testEmptyCapture()
	  {
		testPatterns("foobarbaz", new string[] {".(y*)"}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, false);
		testPatterns("foobarbaz", new string[] {".(y*)"}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, true);

		testPatterns("foo bar baz", new string[] {".(y*)"}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, false);

		testPatterns("foo bar baz", new string[] {".(y*)"}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCaptureAll() throws Exception
	  public virtual void testCaptureAll()
	  {
		testPatterns("foobarbaz", new string[] {"(.+)"}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, false);
		testPatterns("foobarbaz", new string[] {"(.+)"}, new string[] {"foobarbaz"}, new int[] {0}, new int[] {9}, new int[] {1}, true);

		testPatterns("foo bar baz", new string[] {"(.+)"}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, false);

		testPatterns("foo bar baz", new string[] {"(.+)"}, new string[] {"foo","bar","baz"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCaptureStart() throws Exception
	  public virtual void testCaptureStart()
	  {
		testPatterns("foobarbaz", new string[] {"^(.)"}, new string[] {"f"}, new int[] {0}, new int[] {9}, new int[] {1}, false);
		testPatterns("foobarbaz", new string[] {"^(.)"}, new string[] {"foobarbaz","f"}, new int[] {0,0}, new int[] {9,9}, new int[] {1,0}, true);

		testPatterns("foo bar baz", new string[] {"^(.)"}, new string[] {"f","b","b"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, false);

		testPatterns("foo bar baz", new string[] {"^(.)"}, new string[] {"foo","f","bar","b","baz","b"}, new int[] {0,0,4,4,8,8}, new int[] {3,3,7,7,11,11}, new int[] {1,0,1,0,1,0}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCaptureMiddle() throws Exception
	  public virtual void testCaptureMiddle()
	  {
		testPatterns("foobarbaz", new string[] {"^.(.)."}, new string[] {"o"}, new int[] {0}, new int[] {9}, new int[] {1}, false);
		testPatterns("foobarbaz", new string[] {"^.(.)."}, new string[] {"foobarbaz","o"}, new int[] {0,0}, new int[] {9,9}, new int[] {1,0}, true);

		testPatterns("foo bar baz", new string[] {"^.(.)."}, new string[] {"o","a","a"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, false);

		testPatterns("foo bar baz", new string[] {"^.(.)."}, new string[] {"foo","o","bar","a","baz","a"}, new int[] {0,0,4,4,8,8}, new int[] {3,3,7,7,11,11}, new int[] {1,0,1,0,1,0}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCaptureEnd() throws Exception
	  public virtual void testCaptureEnd()
	  {
		testPatterns("foobarbaz", new string[] {"(.)$"}, new string[] {"z"}, new int[] {0}, new int[] {9}, new int[] {1}, false);
		testPatterns("foobarbaz", new string[] {"(.)$"}, new string[] {"foobarbaz","z"}, new int[] {0,0}, new int[] {9,9}, new int[] {1,0}, true);

		testPatterns("foo bar baz", new string[] {"(.)$"}, new string[] {"o","r","z"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, false);

		testPatterns("foo bar baz", new string[] {"(.)$"}, new string[] {"foo","o","bar","r","baz","z"}, new int[] {0,0,4,4,8,8}, new int[] {3,3,7,7,11,11}, new int[] {1,0,1,0,1,0}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCaptureStartMiddle() throws Exception
	  public virtual void testCaptureStartMiddle()
	  {
		testPatterns("foobarbaz", new string[] {"^(.)(.)"}, new string[] {"f","o"}, new int[] {0,0}, new int[] {9,9}, new int[] {1,0}, false);
		testPatterns("foobarbaz", new string[] {"^(.)(.)"}, new string[] {"foobarbaz","f","o"}, new int[] {0,0,0}, new int[] {9,9,9}, new int[] {1,0,0}, true);

		testPatterns("foo bar baz", new string[] {"^(.)(.)"}, new string[] {"f","o","b","a","b","a"}, new int[] {0,0,4,4,8,8}, new int[] {3,3,7,7,11,11}, new int[] {1,0,1,0,1,0}, false);

		testPatterns("foo bar baz", new string[] {"^(.)(.)"}, new string[] {"foo","f","o","bar","b","a","baz","b","a"}, new int[] {0,0,0,4,4,4,8,8,8}, new int[] {3,3,3,7,7,7,11,11,11}, new int[] {1,0,0,1,0,0,1,0,0}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCaptureStartEnd() throws Exception
	  public virtual void testCaptureStartEnd()
	  {
		testPatterns("foobarbaz", new string[] {"^(.).+(.)$"}, new string[] {"f","z"}, new int[] {0,0}, new int[] {9,9}, new int[] {1,0}, false);
		testPatterns("foobarbaz", new string[] {"^(.).+(.)$"}, new string[] {"foobarbaz","f","z"}, new int[] {0,0,0}, new int[] {9,9,9}, new int[] {1,0,0}, true);

		testPatterns("foo bar baz", new string[] {"^(.).+(.)$"}, new string[] {"f","o","b","r","b","z"}, new int[] {0,0,4,4,8,8}, new int[] {3,3,7,7,11,11}, new int[] {1,0,1,0,1,0}, false);

		testPatterns("foo bar baz", new string[] {"^(.).+(.)$"}, new string[] {"foo","f","o","bar","b","r","baz","b","z"}, new int[] {0,0,0,4,4,4,8,8,8}, new int[] {3,3,3,7,7,7,11,11,11}, new int[] {1,0,0,1,0,0,1,0,0}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCaptureMiddleEnd() throws Exception
	  public virtual void testCaptureMiddleEnd()
	  {
		testPatterns("foobarbaz", new string[] {"(.)(.)$"}, new string[] {"a","z"}, new int[] {0,0}, new int[] {9,9}, new int[] {1,0}, false);
		testPatterns("foobarbaz", new string[] {"(.)(.)$"}, new string[] {"foobarbaz","a","z"}, new int[] {0,0,0}, new int[] {9,9,9}, new int[] {1,0,0}, true);

		testPatterns("foo bar baz", new string[] {"(.)(.)$"}, new string[] {"o","o","a","r","a","z"}, new int[] {0,0,4,4,8,8}, new int[] {3,3,7,7,11,11}, new int[] {1,0,1,0,1,0}, false);

		testPatterns("foo bar baz", new string[] {"(.)(.)$"}, new string[] {"foo","o","o","bar","a","r","baz","a","z"}, new int[] {0,0,0,4,4,4,8,8,8}, new int[] {3,3,3,7,7,7,11,11,11}, new int[] {1,0,0,1,0,0,1,0,0}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMultiCaptureOverlap() throws Exception
	  public virtual void testMultiCaptureOverlap()
	  {
		testPatterns("foobarbaz", new string[] {"(.(.(.)))"}, new string[] {"foo","oo","o","bar","ar","r","baz","az","z"}, new int[] {0,0,0,0,0,0,0,0,0}, new int[] {9,9,9,9,9,9,9,9,9}, new int[] {1,0,0,0,0,0,0,0,0}, false);
		testPatterns("foobarbaz", new string[] {"(.(.(.)))"}, new string[] {"foobarbaz","foo","oo","o","bar","ar","r","baz","az","z"}, new int[] {0,0,0,0,0,0,0,0,0,0}, new int[] {9,9,9,9,9,9,9,9,9,9}, new int[] {1,0,0,0,0,0,0,0,0,0}, true);

		testPatterns("foo bar baz", new string[] {"(.(.(.)))"}, new string[] {"foo","oo","o","bar","ar","r","baz","az","z"}, new int[] {0,0,0,4,4,4,8,8,8}, new int[] {3,3,3,7,7,7,11,11,11}, new int[] {1,0,0,1,0,0,1,0,0}, false);

		testPatterns("foo bar baz", new string[] {"(.(.(.)))"}, new string[] {"foo","oo","o","bar","ar","r","baz","az","z"}, new int[] {0,0,0,4,4,4,8,8,8}, new int[] {3,3,3,7,7,7,11,11,11}, new int[] {1,0,0,1,0,0,1,0,0}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMultiPattern() throws Exception
	  public virtual void testMultiPattern()
	  {
		testPatterns("aaabbbaaa", new string[] {"(aaa)","(bbb)","(ccc)"}, new string[] {"aaa","bbb","aaa"}, new int[] {0,0,0}, new int[] {9,9,9}, new int[] {1,0,0}, false);
		testPatterns("aaabbbaaa", new string[] {"(aaa)","(bbb)","(ccc)"}, new string[] {"aaabbbaaa","aaa","bbb","aaa"}, new int[] {0,0,0,0}, new int[] {9,9,9,9}, new int[] {1,0,0,0}, true);

		testPatterns("aaa bbb aaa", new string[] {"(aaa)","(bbb)","(ccc)"}, new string[] {"aaa","bbb","aaa"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, false);

		testPatterns("aaa bbb aaa", new string[] {"(aaa)","(bbb)","(ccc)"}, new string[] {"aaa","bbb","aaa"}, new int[] {0,4,8}, new int[] {3,7,11}, new int[] {1,1,1}, true);
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCamelCase() throws Exception
	  public virtual void testCamelCase()
	  {
		testPatterns("letsPartyLIKEits1999_dude", new string[] {"([A-Z]{2,})", "(?<![A-Z])([A-Z][a-z]+)", "(?:^|\\b|(?<=[0-9_])|(?<=[A-Z]{2}))([a-z]+)", "([0-9]+)"}, new string[] {"lets","Party","LIKE","its","1999","dude"}, new int[] {0,0,0,0,0,0}, new int[] {25,25,25,25,25,25}, new int[] {1,0,0,0,0,0,0}, false);
		testPatterns("letsPartyLIKEits1999_dude", new string[] {"([A-Z]{2,})", "(?<![A-Z])([A-Z][a-z]+)", "(?:^|\\b|(?<=[0-9_])|(?<=[A-Z]{2}))([a-z]+)", "([0-9]+)"}, new string[] {"letsPartyLIKEits1999_dude","lets","Party","LIKE","its","1999","dude"}, new int[] {0,0,0,0,0,0,0}, new int[] {25,25,25,25,25,25,25}, new int[] {1,0,0,0,0,0,0,0}, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomString() throws Exception
	  public virtual void testRandomString()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);

		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestPatternCaptureGroupTokenFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestPatternCaptureGroupTokenFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			return new TokenStreamComponents(tokenizer, new PatternCaptureGroupTokenFilter(tokenizer, false, Pattern.compile("((..)(..))")));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void testPatterns(String input, String[] regexes, String[] tokens, int[] startOffsets, int[] endOffsets, int[] positions, boolean preserveOriginal) throws Exception
	  private void testPatterns(string input, string[] regexes, string[] tokens, int[] startOffsets, int[] endOffsets, int[] positions, bool preserveOriginal)
	  {
		Pattern[] patterns = new Pattern[regexes.Length];
		for (int i = 0; i < regexes.Length; i++)
		{
		  patterns[i] = Pattern.compile(regexes[i]);
		}
		TokenStream ts = new PatternCaptureGroupTokenFilter(new MockTokenizer(new StringReader(input), MockTokenizer.WHITESPACE, false), preserveOriginal, patterns);
		assertTokenStreamContents(ts, tokens, startOffsets, endOffsets, positions);
	  }

	}

}