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

	using SynonymFilter = org.apache.lucene.analysis.synonym.SynonymFilter;
	using SynonymMap = org.apache.lucene.analysis.synonym.SynonymMap;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using Test = org.junit.Test;


	public class TestLimitTokenPositionFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxPosition2() throws java.io.IOException
	  public virtual void testMaxPosition2()
	  {
		foreach (bool consumeAll in new bool[]{true, false})
		{
		  Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);

		  // don't use assertAnalyzesTo here, as the end offset is not the end of the string (unless consumeAll is true, in which case its correct)!
		  assertTokenStreamContents(a.tokenStream("dummy", "1  2     3  4  5"), new string[]{"1", "2"}, new int[]{0, 3}, new int[]{1, 4}, consumeAll ? 16 : null);
		  assertTokenStreamContents(a.tokenStream("dummy", new StringReader("1 2 3 4 5")), new string[]{"1", "2"}, new int[]{0, 2}, new int[]{1, 3}, consumeAll ? 9 : null);

		  // less than the limit, ensure we behave correctly
		  assertTokenStreamContents(a.tokenStream("dummy", "1  "), new string[]{"1"}, new int[]{0}, new int[]{1}, consumeAll ? 3 : null);

		  // equal to limit
		  assertTokenStreamContents(a.tokenStream("dummy", "1  2  "), new string[]{"1", "2"}, new int[]{0, 3}, new int[]{1, 4}, consumeAll ? 6 : null);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestLimitTokenPositionFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestLimitTokenPositionFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			// if we are consuming all tokens, we can use the checks, otherwise we can't
			tokenizer.EnableChecks = consumeAll;
			return new TokenStreamComponents(tokenizer, new LimitTokenPositionFilter(tokenizer, 2, consumeAll));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxPosition3WithSynomyms() throws java.io.IOException
	  public virtual void testMaxPosition3WithSynomyms()
	  {
		foreach (bool consumeAll in new bool[]{true, false})
		{
		  MockTokenizer tokenizer = new MockTokenizer(new StringReader("one two three four five"), MockTokenizer.WHITESPACE, false);
		  // if we are consuming all tokens, we can use the checks, otherwise we can't
		  tokenizer.EnableChecks = consumeAll;

		  SynonymMap.Builder builder = new SynonymMap.Builder(true);
		  builder.add(new CharsRef("one"), new CharsRef("first"), true);
		  builder.add(new CharsRef("one"), new CharsRef("alpha"), true);
		  builder.add(new CharsRef("one"), new CharsRef("beguine"), true);
		  CharsRef multiWordCharsRef = new CharsRef();
		  SynonymMap.Builder.join(new string[]{"and", "indubitably", "single", "only"}, multiWordCharsRef);
		  builder.add(new CharsRef("one"), multiWordCharsRef, true);
		  SynonymMap.Builder.join(new string[]{"dopple", "ganger"}, multiWordCharsRef);
		  builder.add(new CharsRef("two"), multiWordCharsRef, true);
		  SynonymMap synonymMap = builder.build();
		  TokenStream stream = new SynonymFilter(tokenizer, synonymMap, true);
		  stream = new LimitTokenPositionFilter(stream, 3, consumeAll);

		  // "only", the 4th word of multi-word synonym "and indubitably single only" is not emitted, since its position is greater than 3.
		  assertTokenStreamContents(stream, new string[]{"one", "first", "alpha", "beguine", "and", "two", "indubitably", "dopple", "three", "single", "ganger"}, new int[]{1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0});
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testIllegalArguments() throws Exception
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
	  public virtual void testIllegalArguments()
	  {
		new LimitTokenPositionFilter(new MockTokenizer(new StringReader("one two three four five")), 0);
	  }
	}

}