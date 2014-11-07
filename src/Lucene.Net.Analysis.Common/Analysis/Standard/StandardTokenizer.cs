using System;

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

namespace org.apache.lucene.analysis.standard
{


	using StandardTokenizerImpl31 = org.apache.lucene.analysis.standard.std31.StandardTokenizerImpl31;
	using StandardTokenizerImpl34 = org.apache.lucene.analysis.standard.std34.StandardTokenizerImpl34;
	using StandardTokenizerImpl40 = org.apache.lucene.analysis.standard.std40.StandardTokenizerImpl40;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// A grammar-based tokenizer constructed with JFlex.
	/// <para>
	/// As of Lucene version 3.1, this class implements the Word Break rules from the
	/// Unicode Text Segmentation algorithm, as specified in 
	/// <a href="http://unicode.org/reports/tr29/">Unicode Standard Annex #29</a>.
	/// <p/>
	/// </para>
	/// <para>Many applications have specific tokenizer needs.  If this tokenizer does
	/// not suit your application, please consider copying this source code
	/// directory to your project and maintaining your own grammar-based tokenizer.
	/// 
	/// <a name="version"/>
	/// </para>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating StandardTokenizer:
	/// <ul>
	///   <li> As of 3.4, Hiragana and Han characters are no longer wrongly split
	///   from their combining characters. If you use a previous version number,
	///   you get the exact broken behavior for backwards compatibility.
	///   <li> As of 3.1, StandardTokenizer implements Unicode text segmentation.
	///   If you use a previous version number, you get the exact behavior of
	///   <seealso cref="ClassicTokenizer"/> for backwards compatibility.
	/// </ul>
	/// </para>
	/// </summary>

	public sealed class StandardTokenizer : Tokenizer
	{
	  /// <summary>
	  /// A private instance of the JFlex-constructed scanner </summary>
	  private StandardTokenizerInterface scanner;

	  public const int ALPHANUM = 0;
	  /// @deprecated (3.1) 
	  [Obsolete("(3.1)")]
	  public const int APOSTROPHE = 1;
	  /// @deprecated (3.1) 
	  [Obsolete("(3.1)")]
	  public const int ACRONYM = 2;
	  /// @deprecated (3.1) 
	  [Obsolete("(3.1)")]
	  public const int COMPANY = 3;
	  public const int EMAIL = 4;
	  /// @deprecated (3.1) 
	  [Obsolete("(3.1)")]
	  public const int HOST = 5;
	  public const int NUM = 6;
	  /// @deprecated (3.1) 
	  [Obsolete("(3.1)")]
	  public const int CJ = 7;

	  /// @deprecated (3.1) 
	  [Obsolete("(3.1)")]
	  public const int ACRONYM_DEP = 8;

	  public const int SOUTHEAST_ASIAN = 9;
	  public const int IDEOGRAPHIC = 10;
	  public const int HIRAGANA = 11;
	  public const int KATAKANA = 12;
	  public const int HANGUL = 13;

	  /// <summary>
	  /// String token types that correspond to token type int constants </summary>
	  public static readonly string[] TOKEN_TYPES = new string [] {"<ALPHANUM>", "<APOSTROPHE>", "<ACRONYM>", "<COMPANY>", "<EMAIL>", "<HOST>", "<NUM>", "<CJ>", "<ACRONYM_DEP>", "<SOUTHEAST_ASIAN>", "<IDEOGRAPHIC>", "<HIRAGANA>", "<KATAKANA>", "<HANGUL>"};

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
	  /// Creates a new instance of the <seealso cref="org.apache.lucene.analysis.standard.StandardTokenizer"/>.  Attaches
	  /// the <code>input</code> to the newly created JFlex scanner.
	  /// </summary>
	  /// <param name="input"> The input reader
	  /// 
	  /// See http://issues.apache.org/jira/browse/LUCENE-1068 </param>
	  public StandardTokenizer(Version matchVersion, Reader input) : base(input)
	  {
		init(matchVersion);
	  }

	  /// <summary>
	  /// Creates a new StandardTokenizer with a given <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> 
	  /// </summary>
	  public StandardTokenizer(Version matchVersion, AttributeFactory factory, Reader input) : base(factory, input)
	  {
		init(matchVersion);
	  }

	  private void init(Version matchVersion)
	  {
		if (matchVersion.onOrAfter(Version.LUCENE_47))
		{
		  this.scanner = new StandardTokenizerImpl(input);
		}
		else if (matchVersion.onOrAfter(Version.LUCENE_40))
		{
		  this.scanner = new StandardTokenizerImpl40(input);
		}
		else if (matchVersion.onOrAfter(Version.LUCENE_34))
		{
		  this.scanner = new StandardTokenizerImpl34(input);
		}
		else if (matchVersion.onOrAfter(Version.LUCENE_31))
		{
		  this.scanner = new StandardTokenizerImpl31(input);
		}
		else
		{
		  this.scanner = new ClassicTokenizerImpl(input);
		}
	  }

	  // this tokenizer generates three attributes:
	  // term offset, positionIncrement and type
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly PositionIncrementAttribute posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));

	  /*
	   * (non-Javadoc)
	   *
	   * @see org.apache.lucene.analysis.TokenStream#next()
	   */
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
			// This 'if' should be removed in the next release. For now, it converts
			// invalid acronyms to HOST. When removed, only the 'else' part should
			// remain.
			if (tokenType == StandardTokenizer.ACRONYM_DEP)
			{
			  typeAtt.Type = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HOST];
			  termAtt.Length = termAtt.length() - 1; // remove extra '.'
			}
			else
			{
			  typeAtt.Type = StandardTokenizer.TOKEN_TYPES[tokenType];
			}
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