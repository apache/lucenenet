namespace org.apache.lucene.analysis.th
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
	using CharArrayIterator = org.apache.lucene.analysis.util.CharArrayIterator;
	using SegmentingTokenizerBase = org.apache.lucene.analysis.util.SegmentingTokenizerBase;

	/// <summary>
	/// Tokenizer that use <seealso cref="BreakIterator"/> to tokenize Thai text.
	/// <para>WARNING: this tokenizer may not be supported by all JREs.
	///    It is known to work with Sun/Oracle and Harmony JREs.
	///    If your application needs to be fully portable, consider using ICUTokenizer instead,
	///    which uses an ICU Thai BreakIterator that will always be available.
	/// </para>
	/// </summary>
	public class ThaiTokenizer : SegmentingTokenizerBase
	{
	  /// <summary>
	  /// True if the JRE supports a working dictionary-based breakiterator for Thai.
	  /// If this is false, this tokenizer will not work at all!
	  /// </summary>
	  public static readonly bool DBBI_AVAILABLE;
	  private static readonly BreakIterator proto = BreakIterator.getWordInstance(new Locale("th"));
	  static ThaiTokenizer()
	  {
		// check that we have a working dictionary-based break iterator for thai
		proto.Text = "ภาษาไทย";
		DBBI_AVAILABLE = proto.isBoundary(4);
	  }

	  /// <summary>
	  /// used for breaking the text into sentences </summary>
	  private static readonly BreakIterator sentenceProto = BreakIterator.getSentenceInstance(Locale.ROOT);

	  private readonly BreakIterator wordBreaker;
	  private readonly CharArrayIterator wrapper = CharArrayIterator.newWordInstance();

	  internal int sentenceStart;
	  internal int sentenceEnd;

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));

	  /// <summary>
	  /// Creates a new ThaiTokenizer </summary>
	  public ThaiTokenizer(Reader reader) : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader)
	  {
	  }

	  /// <summary>
	  /// Creates a new ThaiTokenizer, supplying the AttributeFactory </summary>
	  public ThaiTokenizer(AttributeFactory factory, Reader reader) : base(factory, reader, (BreakIterator)sentenceProto.clone())
	  {
		if (!DBBI_AVAILABLE)
		{
		  throw new System.NotSupportedException("This JRE does not have support for Thai segmentation");
		}
		wordBreaker = (BreakIterator)proto.clone();
	  }

	  protected internal override void setNextSentence(int sentenceStart, int sentenceEnd)
	  {
		this.sentenceStart = sentenceStart;
		this.sentenceEnd = sentenceEnd;
		wrapper.setText(buffer, sentenceStart, sentenceEnd - sentenceStart);
		wordBreaker.Text = wrapper;
	  }

	  protected internal override bool incrementWord()
	  {
		int start = wordBreaker.current();
		if (start == BreakIterator.DONE)
		{
		  return false; // BreakIterator exhausted
		}

		// find the next set of boundaries, skipping over non-tokens
		int end_Renamed = wordBreaker.next();
		while (end_Renamed != BreakIterator.DONE && !char.IsLetterOrDigit(char.codePointAt(buffer, sentenceStart + start, sentenceEnd)))
		{
		  start = end_Renamed;
		  end_Renamed = wordBreaker.next();
		}

		if (end_Renamed == BreakIterator.DONE)
		{
		  return false; // BreakIterator exhausted
		}

		clearAttributes();
		termAtt.copyBuffer(buffer, sentenceStart + start, end_Renamed - start);
		offsetAtt.setOffset(correctOffset(offset + sentenceStart + start), correctOffset(offset + sentenceStart + end_Renamed));
		return true;
	  }
	}

}