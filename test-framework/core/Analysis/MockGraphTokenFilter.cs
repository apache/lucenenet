using System;

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
	using TestUtil = Lucene.Net.Util.TestUtil;

	// TODO: sometimes remove tokens too...?

	/// <summary>
	/// Randomly inserts overlapped (posInc=0) tokens with
	///  posLength sometimes > 1.  The chain must have
	///  an OffsetAttribute.  
	/// </summary>

	public sealed class MockGraphTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position>
	{

	  private static bool DEBUG = false;

	  private readonly CharTermAttribute TermAtt = addAttribute(typeof(CharTermAttribute));

	  private readonly long Seed;
	  private Random Random;

	  public MockGraphTokenFilter(Random random, TokenStream input) : base(input)
	  {
		Seed = random.nextLong();
	  }

	  protected internal override Position NewPosition()
	  {
		return new Position();
	  }

	  protected internal override void AfterPosition()
	  {
		if (DEBUG)
		{
		  Console.WriteLine("MockGraphTF.afterPos");
		}
		if (Random.Next(7) == 5)
		{

		  int posLength = TestUtil.NextInt(Random, 1, 5);

		  if (DEBUG)
		  {
			Console.WriteLine("  do insert! posLen=" + posLength);
		  }

		  Position posEndData = positions.get(OutputPos + posLength);

		  // Look ahead as needed until we figure out the right
		  // endOffset:
		  while (!End && posEndData.EndOffset == -1 && InputPos <= (OutputPos + posLength))
		  {
			if (!PeekToken())
			{
			  break;
			}
		  }

		  if (posEndData.EndOffset != -1)
		  {
			// Notify super class that we are injecting a token:
			InsertToken();
			ClearAttributes();
			PosLenAtt.PositionLength = posLength;
			TermAtt.append(TestUtil.RandomUnicodeString(Random));
			PosIncAtt.PositionIncrement = 0;
			OffsetAtt.SetOffset(positions.get(OutputPos).startOffset, posEndData.EndOffset);
			if (DEBUG)
			{
			  Console.WriteLine("  inject: outputPos=" + OutputPos + " startOffset=" + OffsetAtt.StartOffset() + " endOffset=" + OffsetAtt.EndOffset() + " posLength=" + PosLenAtt.PositionLength);
			}
			// TODO: set TypeAtt too?
		  }
		  else
		  {
			// Either 1) the tokens ended before our posLength,
			// or 2) our posLength ended inside a hole from the
			// input.  In each case we just skip the inserted
			// token.
		  }
		}
	  }

	  public override void Reset()
	  {
		base.reset();
		// NOTE: must be "deterministically random" because
		// baseTokenStreamTestCase pulls tokens twice on the
		// same input and asserts they are the same:
		this.Random = new Random(Seed);
	  }

	  public override void Close()
	  {
		base.close();
		this.Random = null;
	  }

	  public override bool IncrementToken()
	  {
		if (DEBUG)
		{
		  Console.WriteLine("MockGraphTF.incr inputPos=" + InputPos + " outputPos=" + OutputPos);
		}
		if (Random == null)
		{
		  throw new Exception("incrementToken called in wrong state!");
		}
		return NextToken();
	  }
	}

}