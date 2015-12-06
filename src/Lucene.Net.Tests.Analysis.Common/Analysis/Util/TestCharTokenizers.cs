using System.Text;

namespace org.apache.lucene.analysis.util
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


	using LetterTokenizer = org.apache.lucene.analysis.core.LetterTokenizer;
	using LowerCaseTokenizer = org.apache.lucene.analysis.core.LowerCaseTokenizer;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using TestUtil = org.apache.lucene.util.TestUtil;


	/// <summary>
	/// Testcase for <seealso cref="CharTokenizer"/> subclasses
	/// </summary>
	public class TestCharTokenizers : BaseTokenStreamTestCase
	{

	  /*
	   * test to read surrogate pairs without loosing the pairing 
	   * if the surrogate pair is at the border of the internal IO buffer
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReadSupplementaryChars() throws java.io.IOException
	  public virtual void testReadSupplementaryChars()
	  {
		StringBuilder builder = new StringBuilder();
		// create random input
		int num = 1024 + random().Next(1024);
		num *= RANDOM_MULTIPLIER;
		for (int i = 1; i < num; i++)
		{
		  builder.Append("\ud801\udc1cabc");
		  if ((i % 10) == 0)
		  {
			builder.Append(" ");
		  }
		}
		// internal buffer size is 1024 make sure we have a surrogate pair right at the border
		builder.Insert(1023, "\ud801\udc1c");
		Tokenizer tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader(builder.ToString()));
		assertTokenStreamContents(tokenizer, builder.ToString().ToLower(Locale.ROOT).split(" "));
	  }

	  /*
	   * test to extend the buffer TermAttribute buffer internally. If the internal
	   * alg that extends the size of the char array only extends by 1 char and the
	   * next char to be filled in is a supplementary codepoint (using 2 chars) an
	   * index out of bound exception is triggered.
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExtendCharBuffer() throws java.io.IOException
	  public virtual void testExtendCharBuffer()
	  {
		for (int i = 0; i < 40; i++)
		{
		  StringBuilder builder = new StringBuilder();
		  for (int j = 0; j < 1 + i; j++)
		  {
			builder.Append("a");
		  }
		  builder.Append("\ud801\udc1cabc");
		  Tokenizer tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader(builder.ToString()));
		  assertTokenStreamContents(tokenizer, new string[] {builder.ToString().ToLower(Locale.ROOT)});
		}
	  }

	  /*
	   * tests the max word length of 255 - tokenizer will split at the 255 char no matter what happens
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxWordLength() throws java.io.IOException
	  public virtual void testMaxWordLength()
	  {
		StringBuilder builder = new StringBuilder();

		for (int i = 0; i < 255; i++)
		{
		  builder.Append("A");
		}
		Tokenizer tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader(builder.ToString() + builder.ToString()));
		assertTokenStreamContents(tokenizer, new string[] {builder.ToString().ToLower(Locale.ROOT), builder.ToString().ToLower(Locale.ROOT)});
	  }

	  /*
	   * tests the max word length of 255 with a surrogate pair at position 255
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxWordLengthWithSupplementary() throws java.io.IOException
	  public virtual void testMaxWordLengthWithSupplementary()
	  {
		StringBuilder builder = new StringBuilder();

		for (int i = 0; i < 254; i++)
		{
		  builder.Append("A");
		}
		builder.Append("\ud801\udc1c");
		Tokenizer tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader(builder.ToString() + builder.ToString()));
		assertTokenStreamContents(tokenizer, new string[] {builder.ToString().ToLower(Locale.ROOT), builder.ToString().ToLower(Locale.ROOT)});
	  }

	  // LUCENE-3642: normalize SMP->BMP and check that offsets are correct
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCrossPlaneNormalization() throws java.io.IOException
	  public virtual void testCrossPlaneNormalization()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
		int num = 1000 * RANDOM_MULTIPLIER;
		for (int i = 0; i < num; i++)
		{
		  string s = TestUtil.randomUnicodeString(random());
		  TokenStream ts = analyzer.tokenStream("foo", s);
		  try
		  {
			ts.reset();
			OffsetAttribute offsetAtt = ts.addAttribute(typeof(OffsetAttribute));
			while (ts.incrementToken())
			{
			  string highlightedText = StringHelperClass.SubstringSpecial(s, offsetAtt.startOffset(), offsetAtt.endOffset());
			  for (int j = 0, cp = 0; j < highlightedText.Length; j += char.charCount(cp))
			  {
				cp = char.ConvertToUtf32(highlightedText, j);
				assertTrue("non-letter:" + cp.ToString("x"), char.IsLetter(cp));
			  }
			}
			ts.end();
		  }
		  finally
		  {
			IOUtils.closeWhileHandlingException(ts);
		  }
		}
		// just for fun
		checkRandomData(random(), analyzer, num);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestCharTokenizers outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestCharTokenizers outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new LetterTokenizerAnonymousInnerClassHelper(this, TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }

		  private class LetterTokenizerAnonymousInnerClassHelper : LetterTokenizer
		  {
			  private readonly AnalyzerAnonymousInnerClassHelper outerInstance;

			  public LetterTokenizerAnonymousInnerClassHelper(AnalyzerAnonymousInnerClassHelper outerInstance, UnknownType TEST_VERSION_CURRENT, Reader reader) : base(TEST_VERSION_CURRENT, reader)
			  {
				  this.outerInstance = outerInstance;
			  }

			  protected internal override int normalize(int c)
			  {
				if (c > 0xffff)
				{
				  return 'δ';
				}
				else
				{
				  return c;
				}
			  }
		  }
	  }

	  // LUCENE-3642: normalize BMP->SMP and check that offsets are correct
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCrossPlaneNormalization2() throws java.io.IOException
	  public virtual void testCrossPlaneNormalization2()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this);
		int num = 1000 * RANDOM_MULTIPLIER;
		for (int i = 0; i < num; i++)
		{
		  string s = TestUtil.randomUnicodeString(random());
		  TokenStream ts = analyzer.tokenStream("foo", s);
		  try
		  {
			ts.reset();
			OffsetAttribute offsetAtt = ts.addAttribute(typeof(OffsetAttribute));
			while (ts.incrementToken())
			{
			  string highlightedText = StringHelperClass.SubstringSpecial(s, offsetAtt.startOffset(), offsetAtt.endOffset());
			  for (int j = 0, cp = 0; j < highlightedText.Length; j += char.charCount(cp))
			  {
				cp = char.ConvertToUtf32(highlightedText, j);
				assertTrue("non-letter:" + cp.ToString("x"), char.IsLetter(cp));
			  }
			}
			ts.end();
		  }
		  finally
		  {
			IOUtils.closeWhileHandlingException(ts);
		  }
		}
		// just for fun
		checkRandomData(random(), analyzer, num);
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestCharTokenizers outerInstance;

		  public AnalyzerAnonymousInnerClassHelper2(TestCharTokenizers outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new LetterTokenizerAnonymousInnerClassHelper2(this, TEST_VERSION_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, tokenizer);
		  }

		  private class LetterTokenizerAnonymousInnerClassHelper2 : LetterTokenizer
		  {
			  private readonly AnalyzerAnonymousInnerClassHelper2 outerInstance;

			  public LetterTokenizerAnonymousInnerClassHelper2(AnalyzerAnonymousInnerClassHelper2 outerInstance, UnknownType TEST_VERSION_CURRENT, Reader reader) : base(TEST_VERSION_CURRENT, reader)
			  {
				  this.outerInstance = outerInstance;
			  }

			  protected internal override int normalize(int c)
			  {
				if (c <= 0xffff)
				{
				  return 0x1043C;
				}
				else
				{
				  return c;
				}
			  }
		  }
	  }
	}

}