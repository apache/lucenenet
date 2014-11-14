namespace org.apache.lucene.analysis.standard
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


	using UAX29URLEmailTokenizerImpl31 = org.apache.lucene.analysis.standard.std31.UAX29URLEmailTokenizerImpl31;
	using UAX29URLEmailTokenizerImpl34 = org.apache.lucene.analysis.standard.std34.UAX29URLEmailTokenizerImpl34;
	using UAX29URLEmailTokenizerImpl36 = org.apache.lucene.analysis.standard.std36.UAX29URLEmailTokenizerImpl36;
	using UAX29URLEmailTokenizerImpl40 = org.apache.lucene.analysis.standard.std40.UAX29URLEmailTokenizerImpl40;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// This class implements Word Break rules from the Unicode Text Segmentation 
	/// algorithm, as specified in                 `
	/// <a href="http://unicode.org/reports/tr29/">Unicode Standard Annex #29</a> 
	/// URLs and email addresses are also tokenized according to the relevant RFCs.
	/// <p/>
	/// Tokens produced are of the following types:
	/// <ul>
	///   <li>&lt;ALPHANUM&gt;: A sequence of alphabetic and numeric characters</li>
	///   <li>&lt;NUM&gt;: A number</li>
	///   <li>&lt;URL&gt;: A URL</li>
	///   <li>&lt;EMAIL&gt;: An email address</li>
	///   <li>&lt;SOUTHEAST_ASIAN&gt;: A sequence of characters from South and Southeast
	///       Asian languages, including Thai, Lao, Myanmar, and Khmer</li>
	///   <li>&lt;IDEOGRAPHIC&gt;: A single CJKV ideographic character</li>
	///   <li>&lt;HIRAGANA&gt;: A single hiragana character</li>
	/// </ul>
	/// <a name="version"/>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating UAX29URLEmailTokenizer:
	/// <ul>
	///   <li> As of 3.4, Hiragana and Han characters are no longer wrongly split
	///   from their combining characters. If you use a previous version number,
	///   you get the exact broken behavior for backwards compatibility.
	/// </ul>
	/// </para>
	/// </summary>

	public sealed class UAX29URLEmailTokenizer : Tokenizer
	{
	  /// <summary>
	  /// A private instance of the JFlex-constructed scanner </summary>
	  private readonly StandardTokenizerInterface scanner;

	  public const int ALPHANUM = 0;
	  public const int NUM = 1;
	  public const int SOUTHEAST_ASIAN = 2;
	  public const int IDEOGRAPHIC = 3;
	  public const int HIRAGANA = 4;
	  public const int KATAKANA = 5;
	  public const int HANGUL = 6;
	  public const int URL = 7;
	  public const int EMAIL = 8;

	  /// <summary>
	  /// String token types that correspond to token type int constants </summary>
	  public static readonly string[] TOKEN_TYPES = new string [] {StandardTokenizer.TOKEN_TYPES[StandardTokenizer.ALPHANUM], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.NUM], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.SOUTHEAST_ASIAN], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.IDEOGRAPHIC], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HIRAGANA], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.KATAKANA], StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HANGUL], "<URL>", "<EMAIL>"};

	  private int skippedPositions;

	  private int maxTokenLength = StandardAnalyzer.DEFAULT_MAX_TOKEN_LENGTH;

	  /// <summary>
	  /// Set the max allowed token length.  Any token longer
	  ///  than this is skipped. 
	  /// </summary>
	  public int MaxTokenLength
	  {
		  set
		  {
			if (value < 1)
			{
			  throw new System.ArgumentException("maxTokenLength must be greater than zero");
			}
			this.maxTokenLength = value;
		  }
		  get
		  {
			return maxTokenLength;
		  }
	  }


	  /// <summary>
	  /// Creates a new instance of the UAX29URLEmailTokenizer.  Attaches
	  /// the <code>input</code> to the newly created JFlex scanner.
	  /// </summary>
	  /// <param name="input"> The input reader </param>
	  public UAX29URLEmailTokenizer(Version matchVersion, Reader input) : base(input)
	  {
		this.scanner = getScannerFor(matchVersion);
	  }

	  /// <summary>
	  /// Creates a new UAX29URLEmailTokenizer with a given <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/>
	  /// </summary>
	  public UAX29URLEmailTokenizer(Version matchVersion, AttributeFactory factory, Reader input) : base(factory, input)
	  {
		this.scanner = getScannerFor(matchVersion);
	  }

	  private StandardTokenizerInterface getScannerFor(Version matchVersion)
	  {
		// best effort NPE if you dont call reset
		if (matchVersion.onOrAfter(Version.LUCENE_47))
		{
		  return new UAX29URLEmailTokenizerImpl(input);
		}
		else if (matchVersion.onOrAfter(Version.LUCENE_40))
		{
		  return new UAX29URLEmailTokenizerImpl40(input);
		}
		else if (matchVersion.onOrAfter(Version.LUCENE_36))
		{
		  return new UAX29URLEmailTokenizerImpl36(input);
		}
		else if (matchVersion.onOrAfter(Version.LUCENE_34))
		{
		  return new UAX29URLEmailTokenizerImpl34(input);
		}
		else
		{
		  return new UAX29URLEmailTokenizerImpl31(input);
		}
	  }

	  // this tokenizer generates three attributes:
	  // term offset, positionIncrement and type
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly PositionIncrementAttribute posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		clearAttributes();
		skippedPositions = 0;

		while (true)
		{
		  int tokenType = scanner.NextToken;

		  if (tokenType == StandardTokenizerInterface_Fields.YYEOF)
		  {
			return false;
		  }

		  if (scanner.yylength() <= maxTokenLength)
		  {
			posIncrAtt.PositionIncrement = skippedPositions + 1;
			scanner.getText(termAtt);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int start = scanner.yychar();
			int start = scanner.yychar();
			offsetAtt.setOffset(correctOffset(start), correctOffset(start + termAtt.length()));
			typeAtt.Type = TOKEN_TYPES[tokenType];
			return true;
		  }
		  else
			// When we skip a too-long term, we still increment the
			// position increment
		  {
			skippedPositions++;
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final void end() throws java.io.IOException
	  public override void end()
	  {
		base.end();
		// set final offset
		int finalOffset = correctOffset(scanner.yychar() + scanner.yylength());
		offsetAtt.setOffset(finalOffset, finalOffset);
		// adjust any skipped tokens
		posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		base.close();
		scanner.yyreset(input);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		scanner.yyreset(input);
		skippedPositions = 0;
	  }
	}

}