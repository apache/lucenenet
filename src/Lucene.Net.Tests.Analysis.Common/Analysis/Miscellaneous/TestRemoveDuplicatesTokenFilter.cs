using System.Collections.Generic;

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

namespace org.apache.lucene.analysis.miscellaneous
{

	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using SynonymFilter = org.apache.lucene.analysis.synonym.SynonymFilter;
	using SynonymMap = org.apache.lucene.analysis.synonym.SynonymMap;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using CharsRef = org.apache.lucene.util.CharsRef;
	using TestUtil = org.apache.lucene.util.TestUtil;


	public class TestRemoveDuplicatesTokenFilter : BaseTokenStreamTestCase
	{

	  public static Token tok(int pos, string t, int start, int end)
	  {
		Token tok = new Token(t,start,end);
		tok.PositionIncrement = pos;
		return tok;
	  }
	  public static Token tok(int pos, string t)
	  {
		return tok(pos, t, 0,0);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDups(final String expected, final org.apache.lucene.analysis.Token... tokens) throws Exception
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public virtual void testDups(string expected, params Token[] tokens)
	  {

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Iterator<org.apache.lucene.analysis.Token> toks = java.util.Arrays.asList(tokens).iterator();
		IEnumerator<Token> toks = Arrays.asList(tokens).GetEnumerator();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.TokenStream ts = new RemoveDuplicatesTokenFilter((new org.apache.lucene.analysis.TokenStream()
		TokenStream ts = new RemoveDuplicatesTokenFilter((new TokenStreamAnonymousInnerClassHelper(this, toks)));

		assertTokenStreamContents(ts, expected.Split("\\s", true));
	  }

	  private class TokenStreamAnonymousInnerClassHelper : TokenStream
	  {
		  private readonly TestRemoveDuplicatesTokenFilter outerInstance;

		  private IEnumerator<Token> toks;

		  public TokenStreamAnonymousInnerClassHelper(TestRemoveDuplicatesTokenFilter outerInstance, IEnumerator<Token> toks)
		  {
			  this.outerInstance = outerInstance;
			  this.toks = toks;
			  termAtt = addAttribute(typeof(CharTermAttribute));
			  offsetAtt = addAttribute(typeof(OffsetAttribute));
			  posIncAtt = addAttribute(typeof(PositionIncrementAttribute));
		  }

		  internal CharTermAttribute termAtt;
		  internal OffsetAttribute offsetAtt;
		  internal PositionIncrementAttribute posIncAtt;
		  public override bool incrementToken()
		  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			if (toks.hasNext())
			{
			  clearAttributes();
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			  Token tok = toks.next();
			  termAtt.setEmpty().append(tok);
			  offsetAtt.setOffset(tok.startOffset(), tok.endOffset());
			  posIncAtt.PositionIncrement = tok.PositionIncrement;
			  return true;
			}
			else
			{
			  return false;
			}
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoDups() throws Exception
	  public virtual void testNoDups()
	  {

		testDups("A B B C D E",tok(1,"A", 0, 4),tok(1,"B", 5, 10),tok(1,"B",11, 15),tok(1,"C",16, 20),tok(0,"D",16, 20),tok(1,"E",21, 25));

	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSimpleDups() throws Exception
	  public virtual void testSimpleDups()
	  {

		testDups("A B C D E",tok(1,"A", 0, 4),tok(1,"B", 5, 10),tok(0,"B",11, 15),tok(1,"C",16, 20),tok(0,"D",16, 20),tok(1,"E",21, 25));

	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComplexDups() throws Exception
	  public virtual void testComplexDups()
	  {

		testDups("A B C D E F G H I J K",tok(1,"A"),tok(1,"B"),tok(0,"B"),tok(1,"C"),tok(1,"D"),tok(0,"D"),tok(0,"D"),tok(1,"E"),tok(1,"F"),tok(0,"F"),tok(1,"G"),tok(0,"H"),tok(0,"H"),tok(1,"I"),tok(1,"J"),tok(0,"K"),tok(0,"J"));

	  }

	  // some helper methods for the below test with synonyms
	  private string randomNonEmptyString()
	  {
		while (true)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s = org.apache.lucene.util.TestUtil.randomUnicodeString(random()).trim();
		  string s = TestUtil.randomUnicodeString(random()).trim();
		  if (s.Length != 0 && s.IndexOf('\u0000') == -1)
		  {
			return s;
		  }
		}
	  }

	  private void add(SynonymMap.Builder b, string input, string output, bool keepOrig)
	  {
		b.add(new CharsRef(input.replaceAll(" +", "\u0000")), new CharsRef(output.replaceAll(" +", "\u0000")), keepOrig);
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numIters = atLeast(10);
		int numIters = atLeast(10);
		for (int i = 0; i < numIters; i++)
		{
		  SynonymMap.Builder b = new SynonymMap.Builder(random().nextBoolean());
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numEntries = atLeast(10);
		  int numEntries = atLeast(10);
		  for (int j = 0; j < numEntries; j++)
		  {
			add(b, randomNonEmptyString(), randomNonEmptyString(), random().nextBoolean());
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.synonym.SynonymMap map = b.build();
		  SynonymMap map = b.build();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean ignoreCase = random().nextBoolean();
		  bool ignoreCase = random().nextBoolean();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Analyzer analyzer = new org.apache.lucene.analysis.Analyzer()
		  Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, map, ignoreCase);

		  checkRandomData(random(), analyzer, 200);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestRemoveDuplicatesTokenFilter outerInstance;

		  private SynonymMap map;
		  private bool ignoreCase;

		  public AnalyzerAnonymousInnerClassHelper(TestRemoveDuplicatesTokenFilter outerInstance, SynonymMap map, bool ignoreCase)
		  {
			  this.outerInstance = outerInstance;
			  this.map = map;
			  this.ignoreCase = ignoreCase;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
			TokenStream stream = new SynonymFilter(tokenizer, map, ignoreCase);
			return new TokenStreamComponents(tokenizer, new RemoveDuplicatesTokenFilter(stream));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestRemoveDuplicatesTokenFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestRemoveDuplicatesTokenFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new TokenStreamComponents(tokenizer, new RemoveDuplicatesTokenFilter(tokenizer));
		  }
	  }

	}

}