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

	using FlagsAttribute = org.apache.lucene.analysis.tokenattributes.FlagsAttribute;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using PayloadAttribute = org.apache.lucene.analysis.tokenattributes.PayloadAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using TypeAttribute = org.apache.lucene.analysis.tokenattributes.TypeAttribute;
	using BytesRef = org.apache.lucene.util.BytesRef;


	/// <summary>
	/// Joins two token streams and leaves the last token of the first stream available
	/// to be used when updating the token values in the second stream based on that token.
	/// 
	/// The default implementation adds last prefix token end offset to the suffix token start and end offsets.
	/// <p/>
	/// <b>NOTE:</b> This filter might not behave correctly if used with custom Attributes, i.e. Attributes other than
	/// the ones located in org.apache.lucene.analysis.tokenattributes. 
	/// </summary>
	public class PrefixAwareTokenFilter : TokenStream
	{

	  private TokenStream prefix;
	  private TokenStream suffix;

	  private CharTermAttribute termAtt;
	  private PositionIncrementAttribute posIncrAtt;
	  private PayloadAttribute payloadAtt;
	  private OffsetAttribute offsetAtt;
	  private TypeAttribute typeAtt;
	  private FlagsAttribute flagsAtt;

	  private CharTermAttribute p_termAtt;
	  private PositionIncrementAttribute p_posIncrAtt;
	  private PayloadAttribute p_payloadAtt;
	  private OffsetAttribute p_offsetAtt;
	  private TypeAttribute p_typeAtt;
	  private FlagsAttribute p_flagsAtt;

	  public PrefixAwareTokenFilter(TokenStream prefix, TokenStream suffix) : base(suffix)
	  {
		this.suffix = suffix;
		this.prefix = prefix;
		prefixExhausted = false;

		termAtt = addAttribute(typeof(CharTermAttribute));
		posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
		payloadAtt = addAttribute(typeof(PayloadAttribute));
		offsetAtt = addAttribute(typeof(OffsetAttribute));
		typeAtt = addAttribute(typeof(TypeAttribute));
		flagsAtt = addAttribute(typeof(FlagsAttribute));

		p_termAtt = prefix.addAttribute(typeof(CharTermAttribute));
		p_posIncrAtt = prefix.addAttribute(typeof(PositionIncrementAttribute));
		p_payloadAtt = prefix.addAttribute(typeof(PayloadAttribute));
		p_offsetAtt = prefix.addAttribute(typeof(OffsetAttribute));
		p_typeAtt = prefix.addAttribute(typeof(TypeAttribute));
		p_flagsAtt = prefix.addAttribute(typeof(FlagsAttribute));
	  }

	  private Token previousPrefixToken = new Token();
	  private Token reusableToken = new Token();

	  private bool prefixExhausted;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (!prefixExhausted)
		{
		  Token nextToken = getNextPrefixInputToken(reusableToken);
		  if (nextToken == null)
		  {
			prefixExhausted = true;
		  }
		  else
		  {
			previousPrefixToken.reinit(nextToken);
			// Make it a deep copy
			BytesRef p = previousPrefixToken.Payload;
			if (p != null)
			{
			  previousPrefixToken.Payload = p.clone();
			}
			CurrentToken = nextToken;
			return true;
		  }
		}

		Token nextToken = getNextSuffixInputToken(reusableToken);
		if (nextToken == null)
		{
		  return false;
		}

		nextToken = updateSuffixToken(nextToken, previousPrefixToken);
		CurrentToken = nextToken;
		return true;
	  }

	  private Token CurrentToken
	  {
		  set
		  {
			if (value == null)
			{
				return;
			}
			clearAttributes();
			termAtt.copyBuffer(value.buffer(), 0, value.length());
			posIncrAtt.PositionIncrement = value.PositionIncrement;
			flagsAtt.Flags = value.Flags;
			offsetAtt.setOffset(value.startOffset(), value.endOffset());
			typeAtt.Type = value.type();
			payloadAtt.Payload = value.Payload;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.analysis.Token getNextPrefixInputToken(org.apache.lucene.analysis.Token token) throws java.io.IOException
	  private Token getNextPrefixInputToken(Token token)
	  {
		if (!prefix.incrementToken())
		{
			return null;
		}
		token.copyBuffer(p_termAtt.buffer(), 0, p_termAtt.length());
		token.PositionIncrement = p_posIncrAtt.PositionIncrement;
		token.Flags = p_flagsAtt.Flags;
		token.setOffset(p_offsetAtt.startOffset(), p_offsetAtt.endOffset());
		token.Type = p_typeAtt.type();
		token.Payload = p_payloadAtt.Payload;
		return token;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.analysis.Token getNextSuffixInputToken(org.apache.lucene.analysis.Token token) throws java.io.IOException
	  private Token getNextSuffixInputToken(Token token)
	  {
		if (!suffix.incrementToken())
		{
			return null;
		}
		token.copyBuffer(termAtt.buffer(), 0, termAtt.length());
		token.PositionIncrement = posIncrAtt.PositionIncrement;
		token.Flags = flagsAtt.Flags;
		token.setOffset(offsetAtt.startOffset(), offsetAtt.endOffset());
		token.Type = typeAtt.type();
		token.Payload = payloadAtt.Payload;
		return token;
	  }

	  /// <summary>
	  /// The default implementation adds last prefix token end offset to the suffix token start and end offsets.
	  /// </summary>
	  /// <param name="suffixToken"> a token from the suffix stream </param>
	  /// <param name="lastPrefixToken"> the last token from the prefix stream </param>
	  /// <returns> consumer token </returns>
	  public virtual Token updateSuffixToken(Token suffixToken, Token lastPrefixToken)
	  {
		suffixToken.setOffset(lastPrefixToken.endOffset() + suffixToken.startOffset(), lastPrefixToken.endOffset() + suffixToken.endOffset());
		return suffixToken;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void end() throws java.io.IOException
	  public override void end()
	  {
		prefix.end();
		suffix.end();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		prefix.close();
		suffix.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
	  public override void reset()
	  {
		base.reset();
		if (prefix != null)
		{
		  prefixExhausted = false;
		  prefix.reset();
		}
		if (suffix != null)
		{
		  suffix.reset();
		}


	  }

	  public virtual TokenStream Prefix
	  {
		  get
		  {
			return prefix;
		  }
		  set
		  {
			this.prefix = value;
		  }
	  }


	  public virtual TokenStream Suffix
	  {
		  get
		  {
			return suffix;
		  }
		  set
		  {
			this.suffix = value;
		  }
	  }

	}

}