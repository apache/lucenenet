using System;

namespace org.apache.lucene.analysis.path
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


	using TokenStreamComponents = org.apache.lucene.analysis.Analyzer.TokenStreamComponents;

	public class TestReversePathHierarchyTokenizer : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasicReverse() throws Exception
	  public virtual void testBasicReverse()
	  {
		string path = "/a/b/c";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"/a/b/c", "a/b/c", "b/c", "c"}, new int[]{0, 1, 3, 5}, new int[]{6, 6, 6, 6}, new int[]{1, 0, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEndOfDelimiterReverse() throws Exception
	  public virtual void testEndOfDelimiterReverse()
	  {
		string path = "/a/b/c/";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"/a/b/c/", "a/b/c/", "b/c/", "c/"}, new int[]{0, 1, 3, 5}, new int[]{7, 7, 7, 7}, new int[]{1, 0, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStartOfCharReverse() throws Exception
	  public virtual void testStartOfCharReverse()
	  {
		string path = "a/b/c";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"a/b/c", "b/c", "c"}, new int[]{0, 2, 4}, new int[]{5, 5, 5}, new int[]{1, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStartOfCharEndOfDelimiterReverse() throws Exception
	  public virtual void testStartOfCharEndOfDelimiterReverse()
	  {
		string path = "a/b/c/";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"a/b/c/", "b/c/", "c/"}, new int[]{0, 2, 4}, new int[]{6, 6, 6}, new int[]{1, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOnlyDelimiterReverse() throws Exception
	  public virtual void testOnlyDelimiterReverse()
	  {
		string path = "/";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"/"}, new int[]{0}, new int[]{1}, new int[]{1}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOnlyDelimitersReverse() throws Exception
	  public virtual void testOnlyDelimitersReverse()
	  {
		string path = "//";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"//", "/"}, new int[]{0, 1}, new int[]{2, 2}, new int[]{1, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEndOfDelimiterReverseSkip() throws Exception
	  public virtual void testEndOfDelimiterReverseSkip()
	  {
		string path = "/a/b/c/";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{"/a/b/", "a/b/", "b/"}, new int[]{0, 1, 3}, new int[]{5, 5, 5}, new int[]{1, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStartOfCharReverseSkip() throws Exception
	  public virtual void testStartOfCharReverseSkip()
	  {
		string path = "a/b/c";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{"a/b/", "b/"}, new int[]{0, 2}, new int[]{4, 4}, new int[]{1, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStartOfCharEndOfDelimiterReverseSkip() throws Exception
	  public virtual void testStartOfCharEndOfDelimiterReverseSkip()
	  {
		string path = "a/b/c/";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{"a/b/", "b/"}, new int[]{0, 2}, new int[]{4, 4}, new int[]{1, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOnlyDelimiterReverseSkip() throws Exception
	  public virtual void testOnlyDelimiterReverseSkip()
	  {
		string path = "/";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{}, new int[]{}, new int[]{}, new int[]{}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOnlyDelimitersReverseSkip() throws Exception
	  public virtual void testOnlyDelimitersReverseSkip()
	  {
		string path = "//";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{"/"}, new int[]{0}, new int[]{1}, new int[]{1}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReverseSkip2() throws Exception
	  public virtual void testReverseSkip2()
	  {
		string path = "/a/b/c/";
		ReversePathHierarchyTokenizer t = new ReversePathHierarchyTokenizer(new StringReader(path), 2);
		assertTokenStreamContents(t, new string[]{"/a/", "a/"}, new int[]{0, 1}, new int[]{3, 3}, new int[]{1, 0}, path.Length);
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestReversePathHierarchyTokenizer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestReversePathHierarchyTokenizer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new ReversePathHierarchyTokenizer(reader);
			return new Analyzer.TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }

	  /// <summary>
	  /// blast some random large strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHugeStrings() throws Exception
	  public virtual void testRandomHugeStrings()
	  {
		Random random = random();
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkRandomData(random, a, 100 * RANDOM_MULTIPLIER, 1027);
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestReversePathHierarchyTokenizer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestReversePathHierarchyTokenizer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new ReversePathHierarchyTokenizer(reader);
			return new Analyzer.TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }
	}

}