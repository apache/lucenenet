using System;
using System.Collections.Generic;

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

	/// <summary>
	/// Test huge RAMFile with more than Integer.MAX_VALUE bytes. </summary>
	public class TestHugeRamFile : LuceneTestCase
	{

	  private static readonly long MAX_VALUE = (long) 2 * (long) int.MaxValue;

	  /// <summary>
	  /// Fake a huge ram file by using the same byte buffer for all 
	  /// buffers under maxint. 
	  /// </summary>
	  private class DenseRAMFile : RAMFile
	  {
		internal long Capacity = 0;
		internal Dictionary<int?, sbyte[]> SingleBuffers = new Dictionary<int?, sbyte[]>();
		protected internal override sbyte[] NewBuffer(int size)
		{
		  Capacity += size;
		  if (Capacity <= MAX_VALUE)
		  {
			// below maxint we reuse buffers
			sbyte[] buf = SingleBuffers[Convert.ToInt32(size)];
			if (buf == null)
			{
			  buf = new sbyte[size];
			  //System.out.println("allocate: "+size);
			  SingleBuffers[Convert.ToInt32(size)] = buf;
			}
			return buf;
		  }
		  //System.out.println("allocate: "+size); System.out.flush();
		  return new sbyte[size];
		}
	  }

	  /// <summary>
	  /// Test huge RAMFile with more than Integer.MAX_VALUE bytes. (LUCENE-957) </summary>
	  public virtual void TestHugeFile()
	  {
		DenseRAMFile f = new DenseRAMFile();
		// output part
		RAMOutputStream @out = new RAMOutputStream(f);
		sbyte[] b1 = new sbyte[RAMOutputStream.BUFFER_SIZE];
		sbyte[] b2 = new sbyte[RAMOutputStream.BUFFER_SIZE / 3];
		for (int i = 0; i < b1.Length; i++)
		{
		  b1[i] = (sbyte)(i & 0x0007F);
		}
		for (int i = 0; i < b2.Length; i++)
		{
		  b2[i] = (sbyte)(i & 0x0003F);
		}
		long n = 0;
		Assert.AreEqual("output length must match",n,@out.length());
		while (n <= MAX_VALUE - b1.Length)
		{
		  @out.writeBytes(b1,0,b1.Length);
		  @out.flush();
		  n += b1.Length;
		  Assert.AreEqual("output length must match",n,@out.length());
		}
		//System.out.println("after writing b1's, length = "+out.length()+" (MAX_VALUE="+MAX_VALUE+")");
		int m = b2.Length;
		long L = 12;
		for (int j = 0; j < L; j++)
		{
		  for (int i = 0; i < b2.Length; i++)
		  {
			b2[i]++;
		  }
		  @out.writeBytes(b2,0,m);
		  @out.flush();
		  n += m;
		  Assert.AreEqual("output length must match",n,@out.length());
		}
		@out.close();
		// input part
		RAMInputStream @in = new RAMInputStream("testcase", f);
		Assert.AreEqual("input length must match",n,@in.length());
		//System.out.println("input length = "+in.length()+" % 1024 = "+in.length()%1024);
		for (int j = 0; j < L; j++)
		{
		  long loc = n - (L - j) * m;
		  @in.seek(loc / 3);
		  @in.seek(loc);
		  for (int i = 0; i < m; i++)
		  {
			sbyte bt = @in.readByte();
			sbyte expected = (sbyte)(1 + j + (i & 0x0003F));
			Assert.AreEqual("must read same value that was written! j=" + j + " i=" + i,expected,bt);
		  }
		}
	  }
	}

}