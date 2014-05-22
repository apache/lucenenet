using System;

namespace Lucene.Net.Store
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

	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	using Test = org.junit.Test;

	public class TestCopyBytes : LuceneTestCase
	{

	  private sbyte Value(int idx)
	  {
		return unchecked((sbyte)((idx % 256) * (1 + (idx / 256))));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCopyBytes() throws Exception
	  public virtual void TestCopyBytes()
	  {
		int num = atLeast(10);
		for (int iter = 0; iter < num; iter++)
		{
		  Directory dir = newDirectory();
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + iter + " dir=" + dir);
		  }

		  // make random file
		  IndexOutput @out = dir.createOutput("test", newIOContext(random()));
		  sbyte[] bytes = new sbyte[TestUtil.Next(random(), 1, 77777)];
		  int size = TestUtil.Next(random(), 1, 1777777);
		  int upto = 0;
		  int byteUpto = 0;
		  while (upto < size)
		  {
			bytes[byteUpto++] = Value(upto);
			upto++;
			if (byteUpto == bytes.Length)
			{
			  @out.writeBytes(bytes, 0, bytes.Length);
			  byteUpto = 0;
			}
		  }

		  @out.writeBytes(bytes, 0, byteUpto);
		  Assert.AreEqual(size, @out.FilePointer);
		  @out.close();
		  Assert.AreEqual(size, dir.fileLength("test"));

		  // copy from test -> test2
		  IndexInput @in = dir.openInput("test", newIOContext(random()));

		  @out = dir.createOutput("test2", newIOContext(random()));

		  upto = 0;
		  while (upto < size)
		  {
			if (random().nextBoolean())
			{
			  @out.writeByte(@in.readByte());
			  upto++;
			}
			else
			{
			  int chunk = Math.Min(TestUtil.Next(random(), 1, bytes.Length), size - upto);
			  @out.copyBytes(@in, chunk);
			  upto += chunk;
			}
		  }
		  Assert.AreEqual(size, upto);
		  @out.close();
		  @in.close();

		  // verify
		  IndexInput in2 = dir.openInput("test2", newIOContext(random()));
		  upto = 0;
		  while (upto < size)
		  {
			if (random().nextBoolean())
			{
			  sbyte v = in2.readByte();
			  Assert.AreEqual(Value(upto), v);
			  upto++;
			}
			else
			{
			  int limit = Math.Min(TestUtil.Next(random(), 1, bytes.Length), size - upto);
			  in2.readBytes(bytes, 0, limit);
			  for (int byteIdx = 0; byteIdx < limit; byteIdx++)
			  {
				Assert.AreEqual(Value(upto), bytes[byteIdx]);
				upto++;
			  }
			}
		  }
		  in2.close();

		  dir.deleteFile("test");
		  dir.deleteFile("test2");

		  dir.close();
		}
	  }

	  // LUCENE-3541
	  public virtual void TestCopyBytesWithThreads()
	  {
		int datalen = TestUtil.Next(random(), 101, 10000);
		sbyte[] data = new sbyte[datalen];
		random().nextBytes(data);

		Directory d = newDirectory();
		IndexOutput output = d.createOutput("data", IOContext.DEFAULT);
		output.writeBytes(data, 0, datalen);
		output.close();

		IndexInput input = d.openInput("data", IOContext.DEFAULT);
		IndexOutput outputHeader = d.createOutput("header", IOContext.DEFAULT);
		// copy our 100-byte header
		outputHeader.copyBytes(input, 100);
		outputHeader.close();

		// now make N copies of the remaining bytes
		CopyThread[] copies = new CopyThread[10];
		for (int i = 0; i < copies.Length; i++)
		{
		  copies[i] = new CopyThread(input.clone(), d.createOutput("copy" + i, IOContext.DEFAULT));
		}

		for (int i = 0; i < copies.Length; i++)
		{
		  copies[i].Start();
		}

		for (int i = 0; i < copies.Length; i++)
		{
		  copies[i].Join();
		}

		for (int i = 0; i < copies.Length; i++)
		{
		  IndexInput copiedData = d.openInput("copy" + i, IOContext.DEFAULT);
		  sbyte[] dataCopy = new sbyte[datalen];
		  Array.Copy(data, 0, dataCopy, 0, 100); // copy the header for easy testing
		  copiedData.readBytes(dataCopy, 100, datalen - 100);
		  assertArrayEquals(data, dataCopy);
		  copiedData.close();
		}
		input.close();
		d.close();

	  }

	  internal class CopyThread : System.Threading.Thread
	  {
		internal readonly IndexInput Src;
		internal readonly IndexOutput Dst;

		internal CopyThread(IndexInput src, IndexOutput dst)
		{
		  this.Src = src;
		  this.Dst = dst;
		}

		public override void Run()
		{
		  try
		  {
			Dst.copyBytes(Src, Src.length() - 100);
			Dst.close();
		  }
		  catch (IOException ex)
		  {
			throw new Exception(ex);
		  }
		}
	  }
	}

}