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


	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;

	/// <summary>
	/// Basic tests for <seealso cref="SegmentingTokenizerBase"/> </summary>
	public class TestSegmentingTokenizerBase : BaseTokenStreamTestCase
	{
	  private Analyzer sentence = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new WholeSentenceTokenizer(reader));
		  }
	  }

	  private Analyzer sentenceAndWord = new AnalyzerAnonymousInnerClassHelper2();

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper2()
		  {
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new SentenceAndWordTokenizer(reader));
		  }
	  }

	  /// <summary>
	  /// Some simple examples, just outputting the whole sentence boundaries as "terms" </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBasics() throws java.io.IOException
	  public virtual void testBasics()
	  {
		assertAnalyzesTo(sentence, "The acronym for United States is U.S. but this doesn't end a sentence", new string[] {"The acronym for United States is U.S. but this doesn't end a sentence"});
		assertAnalyzesTo(sentence, "He said, \"Are you going?\" John shook his head.", new string[] {"He said, \"Are you going?\" ", "John shook his head."});
	  }

	  /// <summary>
	  /// Test a subclass that sets some custom attribute values </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCustomAttributes() throws java.io.IOException
	  public virtual void testCustomAttributes()
	  {
		assertAnalyzesTo(sentenceAndWord, "He said, \"Are you going?\" John shook his head.", new string[] {"He", "said", "Are", "you", "going", "John", "shook", "his", "head"}, new int[] {0, 3, 10, 14, 18, 26, 31, 37, 41}, new int[] {2, 7, 13, 17, 23, 30, 36, 40, 45}, new int[] {1, 1, 1, 1, 1, 2, 1, 1, 1});
	  }

	  /// <summary>
	  /// Tests tokenstream reuse </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReuse() throws java.io.IOException
	  public virtual void testReuse()
	  {
		assertAnalyzesTo(sentenceAndWord, "He said, \"Are you going?\"", new string[] {"He", "said", "Are", "you", "going"}, new int[] {0, 3, 10, 14, 18}, new int[] {2, 7, 13, 17, 23}, new int[] {1, 1, 1, 1, 1});
		assertAnalyzesTo(sentenceAndWord, "John shook his head.", new string[] {"John", "shook", "his", "head"}, new int[] {0, 5, 11, 15}, new int[] {4, 10, 14, 19}, new int[] {1, 1, 1, 1});
	  }

	  /// <summary>
	  /// Tests TokenStream.end() </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEnd() throws java.io.IOException
	  public virtual void testEnd()
	  {
		// BaseTokenStreamTestCase asserts that end() is set to our StringReader's length for us here.
		// we add some junk whitespace to the end just to test it.
		assertAnalyzesTo(sentenceAndWord, "John shook his head          ", new string[] {"John", "shook", "his", "head"});
		assertAnalyzesTo(sentenceAndWord, "John shook his head.          ", new string[] {"John", "shook", "his", "head"});
	  }

	  /// <summary>
	  /// Tests terms which span across boundaries </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHugeDoc() throws java.io.IOException
	  public virtual void testHugeDoc()
	  {
		StringBuilder sb = new StringBuilder();
		char[] whitespace = new char[4094];
		Arrays.fill(whitespace, '\n');
		sb.Append(whitespace);
		sb.Append("testing 1234");
		string input = sb.ToString();
		assertAnalyzesTo(sentenceAndWord, input, new string[] {"testing", "1234"});
	  }

	  /// <summary>
	  /// Tests the handling of binary/malformed data </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHugeTerm() throws java.io.IOException
	  public virtual void testHugeTerm()
	  {
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < 10240; i++)
		{
		  sb.Append('a');
		}
		string input = sb.ToString();
		char[] token = new char[1024];
		Arrays.fill(token, 'a');
		string expectedToken = new string(token);
		string[] expected = new string[] {expectedToken, expectedToken, expectedToken, expectedToken, expectedToken, expectedToken, expectedToken, expectedToken, expectedToken, expectedToken};
		assertAnalyzesTo(sentence, input, expected);
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), sentence, 10000 * RANDOM_MULTIPLIER);
		checkRandomData(random(), sentenceAndWord, 10000 * RANDOM_MULTIPLIER);
	  }

	  // some tokenizers for testing

	  /// <summary>
	  /// silly tokenizer that just returns whole sentences as tokens </summary>
	  internal class WholeSentenceTokenizer : SegmentingTokenizerBase
	  {
		internal int sentenceStart, sentenceEnd;
		internal bool hasSentence;

		internal CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		internal OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));

		public WholeSentenceTokenizer(Reader reader) : base(reader, BreakIterator.getSentenceInstance(Locale.ROOT))
		{
		}

		protected internal override void setNextSentence(int sentenceStart, int sentenceEnd)
		{
		  this.sentenceStart = sentenceStart;
		  this.sentenceEnd = sentenceEnd;
		  hasSentence = true;
		}

		protected internal override bool incrementWord()
		{
		  if (hasSentence)
		  {
			hasSentence = false;
			clearAttributes();
			termAtt.copyBuffer(buffer, sentenceStart, sentenceEnd - sentenceStart);
			offsetAtt.setOffset(correctOffset(offset + sentenceStart), correctOffset(offset + sentenceEnd));
			return true;
		  }
		  else
		  {
			return false;
		  }
		}
	  }

	  /// <summary>
	  /// simple tokenizer, that bumps posinc + 1 for tokens after a 
	  /// sentence boundary to inhibit phrase queries without slop.
	  /// </summary>
	  internal class SentenceAndWordTokenizer : SegmentingTokenizerBase
	  {
		internal int sentenceStart, sentenceEnd;
		internal int wordStart, wordEnd;
		internal int posBoost = -1; // initially set to -1 so the first word in the document doesn't get a pos boost

		internal CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
		internal OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
		internal PositionIncrementAttribute posIncAtt = addAttribute(typeof(PositionIncrementAttribute));

		public SentenceAndWordTokenizer(Reader reader) : base(reader, BreakIterator.getSentenceInstance(Locale.ROOT))
		{
		}

		protected internal override void setNextSentence(int sentenceStart, int sentenceEnd)
		{
		  this.wordStart = this.wordEnd = this.sentenceStart = sentenceStart;
		  this.sentenceEnd = sentenceEnd;
		  posBoost++;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
		public override void reset()
		{
		  base.reset();
		  posBoost = -1;
		}

		protected internal override bool incrementWord()
		{
		  wordStart = wordEnd;
		  while (wordStart < sentenceEnd)
		  {
			if (char.IsLetterOrDigit(buffer[wordStart]))
			{
			  break;
			}
			wordStart++;
		  }

		  if (wordStart == sentenceEnd)
		  {
			  return false;
		  }

		  wordEnd = wordStart + 1;
		  while (wordEnd < sentenceEnd && char.IsLetterOrDigit(buffer[wordEnd]))
		  {
			wordEnd++;
		  }

		  clearAttributes();
		  termAtt.copyBuffer(buffer, wordStart, wordEnd - wordStart);
		  offsetAtt.setOffset(correctOffset(offset + wordStart), correctOffset(offset + wordEnd));
		  posIncAtt.PositionIncrement = posIncAtt.PositionIncrement + posBoost;
		  posBoost = 0;
		  return true;
		}
	  }
	}

}