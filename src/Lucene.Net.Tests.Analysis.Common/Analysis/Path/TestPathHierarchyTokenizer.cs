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


	using MappingCharFilter = org.apache.lucene.analysis.charfilter.MappingCharFilter;
	using NormalizeCharMap = org.apache.lucene.analysis.charfilter.NormalizeCharMap;

	public class TestPathHierarchyTokenizer : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasic() throws Exception
	  public virtual void testBasic()
	  {
		string path = "/a/b/c";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"/a", "/a/b", "/a/b/c"}, new int[]{0, 0, 0}, new int[]{2, 4, 6}, new int[]{1, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEndOfDelimiter() throws Exception
	  public virtual void testEndOfDelimiter()
	  {
		string path = "/a/b/c/";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"/a", "/a/b", "/a/b/c", "/a/b/c/"}, new int[]{0, 0, 0, 0}, new int[]{2, 4, 6, 7}, new int[]{1, 0, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStartOfChar() throws Exception
	  public virtual void testStartOfChar()
	  {
		string path = "a/b/c";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"a", "a/b", "a/b/c"}, new int[]{0, 0, 0}, new int[]{1, 3, 5}, new int[]{1, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStartOfCharEndOfDelimiter() throws Exception
	  public virtual void testStartOfCharEndOfDelimiter()
	  {
		string path = "a/b/c/";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"a", "a/b", "a/b/c", "a/b/c/"}, new int[]{0, 0, 0, 0}, new int[]{1, 3, 5, 6}, new int[]{1, 0, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOnlyDelimiter() throws Exception
	  public virtual void testOnlyDelimiter()
	  {
		string path = "/";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"/"}, new int[]{0}, new int[]{1}, new int[]{1}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOnlyDelimiters() throws Exception
	  public virtual void testOnlyDelimiters()
	  {
		string path = "//";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path));
		assertTokenStreamContents(t, new string[]{"/", "//"}, new int[]{0, 0}, new int[]{1, 2}, new int[]{1, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReplace() throws Exception
	  public virtual void testReplace()
	  {
		string path = "/a/b/c";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), '/', '\\');
		assertTokenStreamContents(t, new string[]{"\\a", "\\a\\b", "\\a\\b\\c"}, new int[]{0, 0, 0}, new int[]{2, 4, 6}, new int[]{1, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWindowsPath() throws Exception
	  public virtual void testWindowsPath()
	  {
		string path = "c:\\a\\b\\c";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), '\\', '\\');
		assertTokenStreamContents(t, new string[]{"c:", "c:\\a", "c:\\a\\b", "c:\\a\\b\\c"}, new int[]{0, 0, 0, 0}, new int[]{2, 4, 6, 8}, new int[]{1, 0, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNormalizeWinDelimToLinuxDelim() throws Exception
	  public virtual void testNormalizeWinDelimToLinuxDelim()
	  {
		NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		builder.add("\\", "/");
		NormalizeCharMap normMap = builder.build();
		string path = "c:\\a\\b\\c";
		Reader cs = new MappingCharFilter(normMap, new StringReader(path));
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(cs);
		assertTokenStreamContents(t, new string[]{"c:", "c:/a", "c:/a/b", "c:/a/b/c"}, new int[]{0, 0, 0, 0}, new int[]{2, 4, 6, 8}, new int[]{1, 0, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasicSkip() throws Exception
	  public virtual void testBasicSkip()
	  {
		string path = "/a/b/c";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{"/b", "/b/c"}, new int[]{2, 2}, new int[]{4, 6}, new int[]{1, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEndOfDelimiterSkip() throws Exception
	  public virtual void testEndOfDelimiterSkip()
	  {
		string path = "/a/b/c/";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{"/b", "/b/c", "/b/c/"}, new int[]{2, 2, 2}, new int[]{4, 6, 7}, new int[]{1, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStartOfCharSkip() throws Exception
	  public virtual void testStartOfCharSkip()
	  {
		string path = "a/b/c";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{"/b", "/b/c"}, new int[]{1, 1}, new int[]{3, 5}, new int[]{1, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStartOfCharEndOfDelimiterSkip() throws Exception
	  public virtual void testStartOfCharEndOfDelimiterSkip()
	  {
		string path = "a/b/c/";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{"/b", "/b/c", "/b/c/"}, new int[]{1, 1, 1}, new int[]{3, 5, 6}, new int[]{1, 0, 0}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOnlyDelimiterSkip() throws Exception
	  public virtual void testOnlyDelimiterSkip()
	  {
		string path = "/";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{}, new int[]{}, new int[]{}, new int[]{}, path.Length);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOnlyDelimitersSkip() throws Exception
	  public virtual void testOnlyDelimitersSkip()
	  {
		string path = "//";
		PathHierarchyTokenizer t = new PathHierarchyTokenizer(new StringReader(path), 1);
		assertTokenStreamContents(t, new string[]{"/"}, new int[]{1}, new int[]{2}, new int[]{1}, path.Length);
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
		  private readonly TestPathHierarchyTokenizer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestPathHierarchyTokenizer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new PathHierarchyTokenizer(reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
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
		  private readonly TestPathHierarchyTokenizer outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestPathHierarchyTokenizer outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new PathHierarchyTokenizer(reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }
	  }
	}

}