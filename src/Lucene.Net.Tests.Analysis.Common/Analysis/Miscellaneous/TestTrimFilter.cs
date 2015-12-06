using System;
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
	using org.apache.lucene.analysis.tokenattributes;
	using Version = org.apache.lucene.util.Version;

	public class TestTrimFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTrim() throws Exception
	  public virtual void testTrim()
	  {
		char[] a = " a ".ToCharArray();
		char[] b = "b   ".ToCharArray();
		char[] ccc = "cCc".ToCharArray();
		char[] whitespace = "   ".ToCharArray();
		char[] empty = "".ToCharArray();

		TokenStream ts = new IterTokenStream(new Token(a, 0, a.Length, 1, 5), new Token(b, 0, b.Length, 6, 10), new Token(ccc, 0, ccc.Length, 11, 15), new Token(whitespace, 0, whitespace.Length, 16, 20), new Token(empty, 0, empty.Length, 21, 21));
		ts = new TrimFilter(TEST_VERSION_CURRENT, ts, false);

		assertTokenStreamContents(ts, new string[] {"a", "b", "cCc", "", ""});

		a = " a".ToCharArray();
		b = "b ".ToCharArray();
		ccc = " c ".ToCharArray();
		whitespace = "   ".ToCharArray();
		ts = new IterTokenStream(new Token(a, 0, a.Length, 0, 2), new Token(b, 0, b.Length, 0, 2), new Token(ccc, 0, ccc.Length, 0, 3), new Token(whitespace, 0, whitespace.Length, 0, 3));
		ts = new TrimFilter(Version.LUCENE_43, ts, true);

		assertTokenStreamContents(ts, new string[] {"a", "b", "c", ""}, new int[] {1, 0, 1, 3}, new int[] {2, 1, 2, 3}, null, new int[] {1, 1, 1, 1}, null, null, false);
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

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
		checkRandomData(random(), a, 1000 * RANDOM_MULTIPLIER);

		Analyzer b = new AnalyzerAnonymousInnerClassHelper2(this);
		checkRandomData(random(), b, 1000 * RANDOM_MULTIPLIER);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestTrimFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestTrimFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
			return new TokenStreamComponents(tokenizer, new TrimFilter(Version.LUCENE_43, tokenizer, true));
		  }
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestTrimFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestTrimFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
			return new TokenStreamComponents(tokenizer, new TrimFilter(TEST_VERSION_CURRENT, tokenizer, false));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestTrimFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(TestTrimFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean updateOffsets = random().nextBoolean();
			bool updateOffsets = random().nextBoolean();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.Version version = updateOffsets ? org.apache.lucene.util.Version.LUCENE_43 : TEST_VERSION_CURRENT;
			Version version = updateOffsets ? Version.LUCENE_43 : TEST_VERSION_CURRENT;
			return new TokenStreamComponents(tokenizer, new TrimFilter(version, tokenizer, updateOffsets));
		  }
	  }
	}

}