using System;
using System.Diagnostics;

namespace Lucene.Net.Util
{

	/// <summary>
	/// Licensed to the Apache Software Foundation (ASF) under one or more
	/// contributor license agreements.  See the NOTICE file distributed with
	/// this work for additional information regarding copyright ownership.
	/// The ASF licenses this file to You under the Apache License, Version 2.0
	/// (the "License"); you may not use this file except in compliance with
	/// the License.  You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>

	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using Ignore = org.junit.Ignore;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore("You must increase heap to > 2 G to run this") public class Test2BPagedBytes extends LuceneTestCase
	public class Test2BPagedBytes : LuceneTestCase
	{

	  public virtual void Test()
	  {
		BaseDirectoryWrapper dir = newFSDirectory(createTempDir("test2BPagedBytes"));
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling.NEVER;
		}
		PagedBytes pb = new PagedBytes(15);
		IndexOutput dataOutput = dir.createOutput("foo", IOContext.DEFAULT);
		long netBytes = 0;
		long seed = random().nextLong();
		long lastFP = 0;
		Random r2 = new Random(seed);
		while (netBytes < 1.1 * int.MaxValue)
		{
		  int numBytes = TestUtil.Next(r2, 1, 32768);
		  sbyte[] bytes = new sbyte[numBytes];
		  r2.nextBytes(bytes);
		  dataOutput.writeBytes(bytes, bytes.Length);
		  long fp = dataOutput.FilePointer;
		  Debug.Assert(fp == lastFP + numBytes);
		  lastFP = fp;
		  netBytes += numBytes;
		}
		dataOutput.close();
		IndexInput input = dir.openInput("foo", IOContext.DEFAULT);
		pb.copy(input, input.length());
		input.close();
		PagedBytes.Reader reader = pb.freeze(true);

		r2 = new Random(seed);
		netBytes = 0;
		while (netBytes < 1.1 * int.MaxValue)
		{
		  int numBytes = TestUtil.Next(r2, 1, 32768);
		  sbyte[] bytes = new sbyte[numBytes];
		  r2.nextBytes(bytes);
		  BytesRef expected = new BytesRef(bytes);

		  BytesRef actual = new BytesRef();
		  reader.fillSlice(actual, netBytes, numBytes);
		  Assert.AreEqual(expected, actual);

		  netBytes += numBytes;
		}
		dir.close();
	  }
	}

}