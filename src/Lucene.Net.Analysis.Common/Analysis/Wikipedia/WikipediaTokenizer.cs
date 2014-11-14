using System.Collections.Generic;
using System.Text;

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

namespace org.apache.lucene.analysis.wikipedia
{

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using FlagsAttribute = org.apache.lucene.analysis.tokenattributes.FlagsAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using AttributeSource = org.apache.lucene.util.AttributeSource;



	/// <summary>
	/// Extension of StandardTokenizer that is aware of Wikipedia syntax.  It is based off of the
	/// Wikipedia tutorial available at http://en.wikipedia.org/wiki/Wikipedia:Tutorial, but it may not be complete.
	/// <p/>
	/// <p/>
	/// @lucene.experimental
	/// </summary>
	public sealed class WikipediaTokenizer : Tokenizer
	{
	  public const string INTERNAL_LINK = "il";
	  public const string EXTERNAL_LINK = "el";
	  //The URL part of the link, i.e. the first token
	  public const string EXTERNAL_LINK_URL = "elu";
	  public const string CITATION = "ci";
	  public const string CATEGORY = "c";
	  public const string BOLD = "b";
	  public const string ITALICS = "i";
	  public const string BOLD_ITALICS = "bi";
	  public const string HEADING = "h";
	  public const string SUB_HEADING = "sh";

	  public const int ALPHANUM_ID = 0;
	  public const int APOSTROPHE_ID = 1;
	  public const int ACRONYM_ID = 2;
	  public const int COMPANY_ID = 3;
	  public const int EMAIL_ID = 4;
	  public const int HOST_ID = 5;
	  public const int NUM_ID = 6;
	  public const int CJ_ID = 7;
	  public const int INTERNAL_LINK_ID = 8;
	  public const int EXTERNAL_LINK_ID = 9;
	  public const int CITATION_ID = 10;
	  public const int CATEGORY_ID = 11;
	  public const int BOLD_ID = 12;
	  public const int ITALICS_ID = 13;
	  public const int BOLD_ITALICS_ID = 14;
	  public const int HEADING_ID = 15;
	  public const int SUB_HEADING_ID = 16;
	  public const int EXTERNAL_LINK_URL_ID = 17;

	  /// <summary>
	  /// String token types that correspond to token type int constants </summary>
	  public static readonly string[] TOKEN_TYPES = new string [] {"<ALPHANUM>", "<APOSTROPHE>", "<ACRONYM>", "<COMPANY>", "<EMAIL>", "<HOST>", "<NUM>", "<CJ>", INTERNAL_LINK, EXTERNAL_LINK, CITATION, CATEGORY, BOLD, ITALICS, BOLD_ITALICS, HEADING, SUB_HEADING, EXTERNAL_LINK_URL};

	  /// <summary>
	  /// Only output tokens
	  /// </summary>
	  public const int TOKENS_ONLY = 0;
	  /// <summary>
	  /// Only output untokenized tokens, which are tokens that would normally be split into several tokens
	  /// </summary>
	  public const int UNTOKENIZED_ONLY = 1;
	  /// <summary>
	  /// Output the both the untokenized token and the splits
	  /// </summary>
	  public const int BOTH = 2;
	  /// <summary>
	  /// This flag is used to indicate that the produced "Token" would, if <seealso cref="#TOKENS_ONLY"/> was used, produce multiple tokens.
	  /// </summary>
	  public const int UNTOKENIZED_TOKEN_FLAG = 1;
	  /// <summary>
	  /// A private instance of the JFlex-constructed scanner
	  /// </summary>
	  private readonly WikipediaTokenizerImpl scanner;

	  private int tokenOutput = TOKENS_ONLY;
	  private HashSet<string> untokenizedTypes = java.util.Collections.emptySet();
	  private IEnumerator<AttributeSource.State> tokens = null;

	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly TypeAttribute typeAtt = addAttribute(typeof(TypeAttribute));
	  private readonly PositionIncrementAttribute posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly FlagsAttribute flagsAtt = addAttribute(typeof(FlagsAttribute));

	  private bool first;

	  /// <summary>
	  /// Creates a new instance of the <seealso cref="WikipediaTokenizer"/>. Attaches the
	  /// <code>input</code> to a newly created JFlex scanner.
	  /// </summary>
	  /// <param name="input"> The Input Reader </param>
	  public WikipediaTokenizer(Reader input) : this(input, TOKENS_ONLY, System.Linq.Enumerable.Empty<string>())
	  {
	  }

	  /// <summary>
	  /// Creates a new instance of the <seealso cref="org.apache.lucene.analysis.wikipedia.WikipediaTokenizer"/>.  Attaches the
	  /// <code>input</code> to a the newly created JFlex scanner.
	  /// </summary>
	  /// <param name="input"> The input </param>
	  /// <param name="tokenOutput"> One of <seealso cref="#TOKENS_ONLY"/>, <seealso cref="#UNTOKENIZED_ONLY"/>, <seealso cref="#BOTH"/> </param>
	  public WikipediaTokenizer(Reader input, int tokenOutput, HashSet<string> untokenizedTypes) : base(input)
	  {
		this.scanner = new WikipediaTokenizerImpl(this.input);
		init(tokenOutput, untokenizedTypes);
	  }

	  /// <summary>
	  /// Creates a new instance of the <seealso cref="org.apache.lucene.analysis.wikipedia.WikipediaTokenizer"/>.  Attaches the
	  /// <code>input</code> to a the newly created JFlex scanner. Uses the given <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/>.
	  /// </summary>
	  /// <param name="input"> The input </param>
	  /// <param name="tokenOutput"> One of <seealso cref="#TOKENS_ONLY"/>, <seealso cref="#UNTOKENIZED_ONLY"/>, <seealso cref="#BOTH"/> </param>
	  public WikipediaTokenizer(AttributeFactory factory, Reader input, int tokenOutput, HashSet<string> untokenizedTypes) : base(factory, input)
	  {
		this.scanner = new WikipediaTokenizerImpl(this.input);
		init(tokenOutput, untokenizedTypes);
	  }

	  private void init(int tokenOutput, HashSet<string> untokenizedTypes)
	  {
		// TODO: cutover to enum
		if (tokenOutput != TOKENS_ONLY && tokenOutput != UNTOKENIZED_ONLY && tokenOutput != BOTH)
		{
		  throw new System.ArgumentException("tokenOutput must be TOKENS_ONLY, UNTOKENIZED_ONLY or BOTH");
		}
		this.tokenOutput = tokenOutput;
		this.untokenizedTypes = untokenizedTypes;
	  }

	  /*
	  * (non-Javadoc)
	  *
	  * @see org.apache.lucene.analysis.TokenStream#next()
	  */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		if (tokens != null && tokens.hasNext())
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  AttributeSource.State state = tokens.next();
		  restoreState(state);
		  return true;
		}
		clearAttributes();
		int tokenType = scanner.NextToken;

		if (tokenType == WikipediaTokenizerImpl.YYEOF)
		{
		  return false;
		}
		string type = WikipediaTokenizerImpl.TOKEN_TYPES[tokenType];
		if (tokenOutput == TOKENS_ONLY || untokenizedTypes.Contains(type) == false)
		{
		  setupToken();
		}
		else if (tokenOutput == UNTOKENIZED_ONLY && untokenizedTypes.Contains(type) == true)
		{
		  collapseTokens(tokenType);

		}
		else if (tokenOutput == BOTH)
		{
		  //collapse into a single token, add it to tokens AND output the individual tokens
		  //output the untokenized Token first
		  collapseAndSaveTokens(tokenType, type);
		}
		int posinc = scanner.PositionIncrement;
		if (first && posinc == 0)
		{
		  posinc = 1; // don't emit posinc=0 for the first token!
		}
		posIncrAtt.PositionIncrement = posinc;
		typeAtt.Type = type;
		first = false;
		return true;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void collapseAndSaveTokens(int tokenType, String type) throws java.io.IOException
	  private void collapseAndSaveTokens(int tokenType, string type)
	  {
		//collapse
		StringBuilder buffer = new StringBuilder(32);
		int numAdded = scanner.setText(buffer);
		//TODO: how to know how much whitespace to add
		int theStart = scanner.yychar();
		int lastPos = theStart + numAdded;
		int tmpTokType;
		int numSeen = 0;
		IList<AttributeSource.State> tmp = new List<AttributeSource.State>();
		setupSavedToken(0, type);
		tmp.Add(captureState());
		//while we can get a token and that token is the same type and we have not transitioned to a new wiki-item of the same type
		while ((tmpTokType = scanner.NextToken) != WikipediaTokenizerImpl.YYEOF && tmpTokType == tokenType && scanner.NumWikiTokensSeen > numSeen)
		{
		  int currPos = scanner.yychar();
		  //append whitespace
		  for (int i = 0; i < (currPos - lastPos); i++)
		  {
			buffer.Append(' ');
		  }
		  numAdded = scanner.setText(buffer);
		  setupSavedToken(scanner.PositionIncrement, type);
		  tmp.Add(captureState());
		  numSeen++;
		  lastPos = currPos + numAdded;
		}
		//trim the buffer
		// TODO: this is inefficient
		string s = buffer.ToString().Trim();
		termAtt.setEmpty().append(s);
		offsetAtt.setOffset(correctOffset(theStart), correctOffset(theStart + s.Length));
		flagsAtt.Flags = UNTOKENIZED_TOKEN_FLAG;
		//The way the loop is written, we will have proceeded to the next token.  We need to pushback the scanner to lastPos
		if (tmpTokType != WikipediaTokenizerImpl.YYEOF)
		{
		  scanner.yypushback(scanner.yylength());
		}
		tokens = tmp.GetEnumerator();
	  }

	  private void setupSavedToken(int positionInc, string type)
	  {
		setupToken();
		posIncrAtt.PositionIncrement = positionInc;
		typeAtt.Type = type;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void collapseTokens(int tokenType) throws java.io.IOException
	  private void collapseTokens(int tokenType)
	  {
		//collapse
		StringBuilder buffer = new StringBuilder(32);
		int numAdded = scanner.setText(buffer);
		//TODO: how to know how much whitespace to add
		int theStart = scanner.yychar();
		int lastPos = theStart + numAdded;
		int tmpTokType;
		int numSeen = 0;
		//while we can get a token and that token is the same type and we have not transitioned to a new wiki-item of the same type
		while ((tmpTokType = scanner.NextToken) != WikipediaTokenizerImpl.YYEOF && tmpTokType == tokenType && scanner.NumWikiTokensSeen > numSeen)
		{
		  int currPos = scanner.yychar();
		  //append whitespace
		  for (int i = 0; i < (currPos - lastPos); i++)
		  {
			buffer.Append(' ');
		  }
		  numAdded = scanner.setText(buffer);
		  numSeen++;
		  lastPos = currPos + numAdded;
		}
		//trim the buffer
		// TODO: this is inefficient
		string s = buffer.ToString().Trim();
		termAtt.setEmpty().append(s);
		offsetAtt.setOffset(correctOffset(theStart), correctOffset(theStart + s.Length));
		flagsAtt.Flags = UNTOKENIZED_TOKEN_FLAG;
		//The way the loop is written, we will have proceeded to the next token.  We need to pushback the scanner to lastPos
		if (tmpTokType != WikipediaTokenizerImpl.YYEOF)
		{
		  scanner.yypushback(scanner.yylength());
		}
		else
		{
		  tokens = null;
		}
	  }

	  private void setupToken()
	  {
		scanner.getText(termAtt);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int start = scanner.yychar();
		int start = scanner.yychar();
		offsetAtt.setOffset(correctOffset(start), correctOffset(start + termAtt.length()));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		base.close();
		scanner.yyreset(input);
	  }

	  /*
	  * (non-Javadoc)
	  *
	  * @see org.apache.lucene.analysis.TokenStream#reset()
	  */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		scanner.yyreset(input);
		tokens = null;
		scanner.reset();
		first = true;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void end() throws java.io.IOException
	  public override void end()
	  {
		base.end();
		// set final offset
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int finalOffset = correctOffset(scanner.yychar() + scanner.yylength());
		int finalOffset = correctOffset(scanner.yychar() + scanner.yylength());
		this.offsetAtt.setOffset(finalOffset, finalOffset);
	  }
	}
}