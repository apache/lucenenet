using System;

namespace Lucene.Net.Codecs.Compressing
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


	using RandomInts = com.carrotsearch.randomizedtesting.generators.RandomInts;

	public abstract class AbstractTestLZ4CompressionMode : AbstractTestCompressionMode
	{

	  public override sbyte[] Test(sbyte[] decompressed)
	  {
		sbyte[] compressed = base.Test(decompressed);
		int off = 0;
		int decompressedOff = 0;
		for (;;)
		{
		  int token = compressed[off++] & 0xFF;
		  int literalLen = (int)((uint)token >> 4);
		  if (literalLen == 0x0F)
		  {
			while (compressed[off] == unchecked((sbyte) 0xFF))
			{
			  literalLen += 0xFF;
			  ++off;
			}
			literalLen += compressed[off++] & 0xFF;
		  }
		  // skip literals
		  off += literalLen;
		  decompressedOff += literalLen;

		  // check that the stream ends with literals and that there are at least
		  // 5 of them
		  if (off == compressed.Length)
		  {
			Assert.AreEqual(decompressed.Length, decompressedOff);
			Assert.IsTrue("lastLiterals=" + literalLen + ", bytes=" + decompressed.Length, literalLen >= LZ4.LAST_LITERALS || literalLen == decompressed.Length);
			break;
		  }

		  int matchDec = (compressed[off++] & 0xFF) | ((compressed[off++] & 0xFF) << 8);
		  // check that match dec is not 0
		  Assert.IsTrue(matchDec + " " + decompressedOff, matchDec > 0 && matchDec <= decompressedOff);

		  int matchLen = token & 0x0F;
		  if (matchLen == 0x0F)
		  {
			while (compressed[off] == unchecked((sbyte) 0xFF))
			{
			  matchLen += 0xFF;
			  ++off;
			}
			matchLen += compressed[off++] & 0xFF;
		  }
		  matchLen += LZ4.MIN_MATCH;

		  // if the match ends prematurely, the next sequence should not have
		  // literals or this means we are wasting space
		  if (decompressedOff + matchLen < decompressed.Length - LZ4.LAST_LITERALS)
		  {
			bool moreCommonBytes = decompressed[decompressedOff + matchLen] == decompressed[decompressedOff - matchDec + matchLen];
			bool nextSequenceHasLiterals = ((int)((uint)(compressed[off] & 0xFF) >> 4)) != 0;
			Assert.IsTrue(!moreCommonBytes || !nextSequenceHasLiterals);
		  }

		  decompressedOff += matchLen;
		}
		Assert.AreEqual(decompressed.Length, decompressedOff);
		return compressed;
	  }

	  public virtual void TestShortLiteralsAndMatchs()
	  {
		// literals and matchs lengths <= 15
		sbyte[] decompressed = "1234562345673456745678910123".getBytes(StandardCharsets.UTF_8);
		Test(decompressed);
	  }

	  public virtual void TestLongMatchs()
	  {
		// match length >= 20
		sbyte[] decompressed = new sbyte[RandomInts.randomIntBetween(random(), 300, 1024)];
		for (int i = 0; i < decompressed.Length; ++i)
		{
		  decompressed[i] = (sbyte) i;
		}
		Test(decompressed);
	  }

	  public virtual void TestLongLiterals()
	  {
		// long literals (length >= 16) which are not the last literals
		sbyte[] decompressed = RandomArray(RandomInts.randomIntBetween(random(), 400, 1024), 256);
		int matchRef = random().Next(30);
		int matchOff = RandomInts.randomIntBetween(random(), decompressed.Length - 40, decompressed.Length - 20);
		int matchLength = RandomInts.randomIntBetween(random(), 4, 10);
		Array.Copy(decompressed, matchRef, decompressed, matchOff, matchLength);
		Test(decompressed);
	  }

	  public virtual void TestMatchRightBeforeLastLiterals()
	  {
		Test(new sbyte[] {1,2,3,4, 1,2,3,4, 1,2,3,4,5});
	  }

	}

}