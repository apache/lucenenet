using System;

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


	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using TestUtil = org.apache.lucene.util.TestUtil;

	public class TestRollingCharBuffer : LuceneTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws Exception
	  public virtual void test()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ITERS = atLeast(1000);
		int ITERS = atLeast(1000);

		RollingCharBuffer buffer = new RollingCharBuffer();

		Random random = random();
		for (int iter = 0;iter < ITERS;iter++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int stringLen = random.nextBoolean() ? random.nextInt(50) : random.nextInt(20000);
		  int stringLen = random.nextBoolean() ? random.Next(50) : random.Next(20000);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s;
		  string s;
		  if (stringLen == 0)
		  {
			s = "";
		  }
		  else
		  {
			s = TestUtil.randomUnicodeString(random, stringLen);
		  }
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter=" + iter + " s.length()=" + s.Length);
		  }
		  buffer.reset(new StringReader(s));
		  int nextRead = 0;
		  int availCount = 0;
		  while (nextRead < s.Length)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  cycle nextRead=" + nextRead + " avail=" + availCount);
			}
			if (availCount == 0 || random.nextBoolean())
			{
			  // Read next char
			  if (VERBOSE)
			  {
				Console.WriteLine("    new char");
			  }
			  assertEquals(s[nextRead], buffer.get(nextRead));
			  nextRead++;
			  availCount++;
			}
			else if (random.nextBoolean())
			{
			  // Read previous char
			  int pos = TestUtil.Next(random, nextRead - availCount, nextRead - 1);
			  if (VERBOSE)
			  {
				Console.WriteLine("    old char pos=" + pos);
			  }
			  assertEquals(s[pos], buffer.get(pos));
			}
			else
			{
			  // Read slice
			  int length;
			  if (availCount == 1)
			  {
				length = 1;
			  }
			  else
			  {
				length = TestUtil.Next(random, 1, availCount);
			  }
			  int start;
			  if (length == availCount)
			  {
				start = nextRead - availCount;
			  }
			  else
			  {
				start = nextRead - availCount + random.Next(availCount - length);
			  }
			  if (VERBOSE)
			  {
				Console.WriteLine("    slice start=" + start + " length=" + length);
			  }
			  assertEquals(s.Substring(start, length), new string(buffer.get(start, length)));
			}

			if (availCount > 0 && random.Next(20) == 17)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int toFree = random.nextInt(availCount);
			  int toFree = random.Next(availCount);
			  if (VERBOSE)
			  {
				Console.WriteLine("    free " + toFree + " (avail=" + (availCount - toFree) + ")");
			  }
			  buffer.freeBefore(nextRead - (availCount - toFree));
			  availCount -= toFree;
			}
		  }
		}
	  }
	}

}