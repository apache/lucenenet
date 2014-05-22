using System;

namespace Lucene.Net.Util.Fst
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

	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;

	public class TestBytesStore : LuceneTestCase
	{

	  public virtual void TestRandom()
	  {

		int iters = atLeast(10);
		for (int iter = 0;iter < iters;iter++)
		{
		  int numBytes = TestUtil.Next(random(), 1, 200000);
		  sbyte[] expected = new sbyte[numBytes];
		  int blockBits = TestUtil.Next(random(), 8, 15);
		  BytesStore bytes = new BytesStore(blockBits);
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + iter + " numBytes=" + numBytes + " blockBits=" + blockBits);
		  }

		  int pos = 0;
		  while (pos < numBytes)
		  {
			int op = random().Next(8);
			if (VERBOSE)
			{
			  Console.WriteLine("  cycle pos=" + pos);
			}
			switch (op)
			{

			case 0:
			{
				// write random byte
				sbyte b = (sbyte) random().Next(256);
				if (VERBOSE)
				{
				  Console.WriteLine("    writeByte b=" + b);
				}

				expected[pos++] = b;
				bytes.writeByte(b);
			}
			  break;

			case 1:
			{
				// write random byte[]
				int len = random().Next(Math.Min(numBytes - pos, 100));
				sbyte[] temp = new sbyte[len];
				random().nextBytes(temp);
				if (VERBOSE)
				{
				  Console.WriteLine("    writeBytes len=" + len + " bytes=" + Arrays.ToString(temp));
				}
				Array.Copy(temp, 0, expected, pos, temp.Length);
				bytes.writeBytes(temp, 0, temp.Length);
				pos += len;
			}
			  break;

			case 2:
			{
				// write int @ absolute pos
				if (pos > 4)
				{
				  int x = random().Next();
				  int randomPos = random().Next(pos - 4);
				  if (VERBOSE)
				  {
					Console.WriteLine("    abs writeInt pos=" + randomPos + " x=" + x);
				  }
				  bytes.writeInt(randomPos, x);
				  expected[randomPos++] = (sbyte)(x >> 24);
				  expected[randomPos++] = (sbyte)(x >> 16);
				  expected[randomPos++] = (sbyte)(x >> 8);
				  expected[randomPos++] = (sbyte) x;
				}
			}
			  break;

			case 3:
			{
				// reverse bytes
				if (pos > 1)
				{
				  int len = TestUtil.Next(random(), 2, Math.Min(100, pos));
				  int start;
				  if (len == pos)
				  {
					start = 0;
				  }
				  else
				  {
					start = random().Next(pos - len);
				  }
				  int end = start + len - 1;
				  if (VERBOSE)
				  {
					Console.WriteLine("    reverse start=" + start + " end=" + end + " len=" + len + " pos=" + pos);
				  }
				  bytes.reverse(start, end);

				  while (start <= end)
				  {
					sbyte b = expected[end];
					expected[end] = expected[start];
					expected[start] = b;
					start++;
					end--;
				  }
				}
			}
			  break;

			case 4:
			{
				// abs write random byte[]
				if (pos > 2)
				{
				  int randomPos = random().Next(pos - 1);
				  int len = TestUtil.Next(random(), 1, Math.Min(pos - randomPos - 1, 100));
				  sbyte[] temp = new sbyte[len];
				  random().nextBytes(temp);
				  if (VERBOSE)
				  {
					Console.WriteLine("    abs writeBytes pos=" + randomPos + " len=" + len + " bytes=" + Arrays.ToString(temp));
				  }
				  Array.Copy(temp, 0, expected, randomPos, temp.Length);
				  bytes.writeBytes(randomPos, temp, 0, temp.Length);
				}
			}
			  break;

			case 5:
			{
				// copyBytes
				if (pos > 1)
				{
				  int src = random().Next(pos - 1);
				  int dest = TestUtil.Next(random(), src + 1, pos - 1);
				  int len = TestUtil.Next(random(), 1, Math.Min(300, pos - dest));
				  if (VERBOSE)
				  {
					Console.WriteLine("    copyBytes src=" + src + " dest=" + dest + " len=" + len);
				  }
				  Array.Copy(expected, src, expected, dest, len);
				  bytes.copyBytes(src, dest, len);
				}
			}
			  break;

			case 6:
			{
				// skip
				int len = random().Next(Math.Min(100, numBytes - pos));

				if (VERBOSE)
				{
				  Console.WriteLine("    skip len=" + len);
				}

				pos += len;
				bytes.skipBytes(len);

				// NOTE: must fill in zeros in case truncate was
				// used, else we get false fails:
				if (len > 0)
				{
				  sbyte[] zeros = new sbyte[len];
				  bytes.writeBytes(pos - len, zeros, 0, len);
				}
			}
			  break;

			case 7:
			{
				// absWriteByte
				if (pos > 0)
				{
				  int dest = random().Next(pos);
				  sbyte b = (sbyte) random().Next(256);
				  expected[dest] = b;
				  bytes.writeByte(dest, b);
				}
				break;
			}
			}

			Assert.AreEqual(pos, bytes.Position);

			if (pos > 0 && random().Next(50) == 17)
			{
			  // truncate
			  int len = TestUtil.Next(random(), 1, Math.Min(pos, 100));
			  bytes.truncate(pos - len);
			  pos -= len;
			  Arrays.fill(expected, pos, pos + len, (sbyte) 0);
			  if (VERBOSE)
			  {
				Console.WriteLine("    truncate len=" + len + " newPos=" + pos);
			  }
			}

			if ((pos > 0 && random().Next(200) == 17))
			{
			  Verify(bytes, expected, pos);
			}
		  }

		  BytesStore bytesToVerify;

		  if (random().nextBoolean())
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: save/load final bytes");
			}
			Directory dir = newDirectory();
			IndexOutput @out = dir.createOutput("bytes", IOContext.DEFAULT);
			bytes.writeTo(@out);
			@out.close();
			IndexInput @in = dir.openInput("bytes", IOContext.DEFAULT);
			bytesToVerify = new BytesStore(@in, numBytes, TestUtil.Next(random(), 256, int.MaxValue));
			@in.close();
			dir.close();
		  }
		  else
		  {
			bytesToVerify = bytes;
		  }

		  Verify(bytesToVerify, expected, numBytes);
		}
	  }

	  private void Verify(BytesStore bytes, sbyte[] expected, int totalLength)
	  {
		Assert.AreEqual(totalLength, bytes.Position);
		if (totalLength == 0)
		{
		  return;
		}
		if (VERBOSE)
		{
		  Console.WriteLine("  verify...");
		}

		// First verify whole thing in one blast:
		sbyte[] actual = new sbyte[totalLength];
		if (random().nextBoolean())
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("    bulk: reversed");
		  }
		  // reversed
		  FST.BytesReader r = bytes.ReverseReader;
		  Assert.IsTrue(r.reversed());
		  r.Position = totalLength - 1;
		  r.readBytes(actual, 0, actual.Length);
		  int start = 0;
		  int end = totalLength - 1;
		  while (start < end)
		  {
			sbyte b = actual[start];
			actual[start] = actual[end];
			actual[end] = b;
			start++;
			end--;
		  }
		}
		else
		{
		  // forward
		  if (VERBOSE)
		  {
			Console.WriteLine("    bulk: forward");
		  }
		  FST.BytesReader r = bytes.ForwardReader;
		  Assert.IsFalse(r.reversed());
		  r.readBytes(actual, 0, actual.Length);
		}

		for (int i = 0;i < totalLength;i++)
		{
		  Assert.AreEqual("byte @ index=" + i, expected[i], actual[i]);
		}

		FST.BytesReader r;

		// Then verify ops:
		bool reversed = random().nextBoolean();
		if (reversed)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("    ops: reversed");
		  }
		  r = bytes.ReverseReader;
		}
		else
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("    ops: forward");
		  }
		  r = bytes.ForwardReader;
		}

		if (totalLength > 1)
		{
		  int numOps = TestUtil.Next(random(), 100, 200);
		  for (int op = 0;op < numOps;op++)
		  {

			int numBytes = random().Next(Math.Min(1000, totalLength - 1));
			int pos;
			if (reversed)
			{
			  pos = TestUtil.Next(random(), numBytes, totalLength - 1);
			}
			else
			{
			  pos = random().Next(totalLength - numBytes);
			}
			if (VERBOSE)
			{
			  Console.WriteLine("    op iter=" + op + " reversed=" + reversed + " numBytes=" + numBytes + " pos=" + pos);
			}
			sbyte[] temp = new sbyte[numBytes];
			r.Position = pos;
			Assert.AreEqual(pos, r.Position);
			r.readBytes(temp, 0, temp.Length);
			for (int i = 0;i < numBytes;i++)
			{
			  sbyte expectedByte;
			  if (reversed)
			  {
				expectedByte = expected[pos - i];
			  }
			  else
			  {
				expectedByte = expected[pos + i];
			  }
			  Assert.AreEqual("byte @ index=" + i, expectedByte, temp[i]);
			}

			int left;
			int expectedPos;

			if (reversed)
			{
			  expectedPos = pos - numBytes;
			  left = (int) r.Position;
			}
			else
			{
			  expectedPos = pos + numBytes;
			  left = (int)(totalLength - r.Position);
			}
			Assert.AreEqual(expectedPos, r.Position);

			if (left > 4)
			{
			  int skipBytes = random().Next(left - 4);

			  int expectedInt = 0;
			  if (reversed)
			  {
				expectedPos -= skipBytes;
				expectedInt |= (expected[expectedPos--] & 0xFF) << 24;
				expectedInt |= (expected[expectedPos--] & 0xFF) << 16;
				expectedInt |= (expected[expectedPos--] & 0xFF) << 8;
				expectedInt |= (expected[expectedPos--] & 0xFF);
			  }
			  else
			  {
				expectedPos += skipBytes;
				expectedInt |= (expected[expectedPos++] & 0xFF) << 24;
				expectedInt |= (expected[expectedPos++] & 0xFF) << 16;
				expectedInt |= (expected[expectedPos++] & 0xFF) << 8;
				expectedInt |= (expected[expectedPos++] & 0xFF);
			  }

			  if (VERBOSE)
			  {
				Console.WriteLine("    skip numBytes=" + skipBytes);
				Console.WriteLine("    readInt");
			  }

			  r.skipBytes(skipBytes);
			  Assert.AreEqual(expectedInt, r.readInt());
			}
		  }
		}
	  }
	}

}