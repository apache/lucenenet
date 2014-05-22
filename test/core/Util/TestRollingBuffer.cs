using System;

namespace Lucene.Net.Util
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

	public class TestRollingBuffer : LuceneTestCase
	{

	  private class Position : RollingBuffer.Resettable
	  {
		public int Pos;

		public override void Reset()
		{
		  Pos = -1;
		}
	  }

	  public virtual void Test()
	  {

		RollingBuffer<Position> buffer = new RollingBufferAnonymousInnerClassHelper(this);

		for (int iter = 0;iter < 100 * RANDOM_MULTIPLIER;iter++)
		{

		  int freeBeforePos = 0;
		  int maxPos = atLeast(10000);
		  FixedBitSet posSet = new FixedBitSet(maxPos + 1000);
		  int posUpto = 0;
		  Random random = random();
		  while (freeBeforePos < maxPos)
		  {
			if (random.Next(4) == 1)
			{
			  int limit = rarely() ? 1000 : 20;
			  int inc = random.Next(limit);
			  int pos = freeBeforePos + inc;
			  posUpto = Math.Max(posUpto, pos);
			  if (VERBOSE)
			  {
				Console.WriteLine("  check pos=" + pos + " posUpto=" + posUpto);
			  }
			  Position posData = buffer.get(pos);
			  if (!posSet.getAndSet(pos))
			  {
				Assert.AreEqual(-1, posData.Pos);
				posData.Pos = pos;
			  }
			  else
			  {
				Assert.AreEqual(pos, posData.Pos);
			  }
			}
			else
			{
			  if (posUpto > freeBeforePos)
			  {
				freeBeforePos += random.Next(posUpto - freeBeforePos);
			  }
			  if (VERBOSE)
			  {
				Console.WriteLine("  freeBeforePos=" + freeBeforePos);
			  }
			  buffer.freeBefore(freeBeforePos);
			}
		  }

		  buffer.reset();
		}
	  }

	  private class RollingBufferAnonymousInnerClassHelper : RollingBuffer<Position>
	  {
		  private readonly TestRollingBuffer OuterInstance;

		  public RollingBufferAnonymousInnerClassHelper(TestRollingBuffer outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override Position NewInstance()
		  {
			Position pos = new Position();
			pos.Pos = -1;
			return pos;
		  }
	  }
	}

}