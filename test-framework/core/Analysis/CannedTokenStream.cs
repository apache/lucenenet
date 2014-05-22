namespace Lucene.Net.Analysis
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

	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using OffsetAttribute = Lucene.Net.Analysis.Tokenattributes.OffsetAttribute;
	using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using PositionLengthAttribute = Lucene.Net.Analysis.Tokenattributes.PositionLengthAttribute;

	/// <summary>
	/// TokenStream from a canned list of Tokens.
	/// </summary>
	public sealed class CannedTokenStream : TokenStream
	{
	  private readonly Token[] Tokens;
	  private int Upto = 0;
	  private readonly CharTermAttribute TermAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly PositionIncrementAttribute PosIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly PositionLengthAttribute PosLengthAtt = addAttribute(typeof(PositionLengthAttribute));
	  private readonly OffsetAttribute OffsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly PayloadAttribute PayloadAtt = addAttribute(typeof(PayloadAttribute));
	  private readonly int FinalOffset;
	  private readonly int FinalPosInc;

	  public CannedTokenStream(params Token[] tokens)
	  {
		this.Tokens = tokens;
		FinalOffset = 0;
		FinalPosInc = 0;
	  }

	  /// <summary>
	  /// If you want trailing holes, pass a non-zero
	  ///  finalPosInc. 
	  /// </summary>
	  public CannedTokenStream(int finalPosInc, int finalOffset, params Token[] tokens)
	  {
		this.Tokens = tokens;
		this.FinalOffset = finalOffset;
		this.FinalPosInc = finalPosInc;
	  }

	  public override void End()
	  {
		base.End();
		PosIncrAtt.PositionIncrement = FinalPosInc;
		OffsetAtt.SetOffset(FinalOffset, FinalOffset);
	  }

	  public override bool IncrementToken()
	  {
		if (Upto < Tokens.Length)
		{
		  Token token = Tokens[Upto++];
		  // TODO: can we just capture/restoreState so
		  // we get all attrs...?
		  ClearAttributes();
		  TermAtt.SetEmpty();
		  TermAtt.Append(token.ToString());
		  PosIncrAtt.PositionIncrement = token.PositionIncrement;
		  PosLengthAtt.PositionLength = token.PositionLength;
		  OffsetAtt.SetOffset(token.StartOffset(), token.EndOffset());
		  PayloadAtt.Payload = token.Payload;
		  return true;
		}
		else
		{
		  return false;
		}
	  }
	}

}