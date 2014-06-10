using System;

namespace Lucene.Net.Codecs.mocksep
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

	using IOContext = Lucene.Net.Store.IOContext;
	using DataOutput = Lucene.Net.Store.DataOutput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using Directory = Lucene.Net.Store.Directory;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using IntIndexOutput = Lucene.Net.Codecs.sep.IntIndexOutput;

	/// <summary>
	/// Writes ints directly to the file (not in blocks) as
	///  vInt.
	/// 
	/// @lucene.experimental
	/// </summary>
	public class MockSingleIntIndexOutput : IntIndexOutput
	{
	  private readonly IndexOutput @out;
	  internal const string CODEC = "SINGLE_INTS";
	  internal const int VERSION_START = 0;
	  internal const int VERSION_CURRENT = VERSION_START;

	  public MockSingleIntIndexOutput(Directory dir, string fileName, IOContext context)
	  {
		@out = dir.CreateOutput(fileName, context);
		bool success = false;
		try
		{
		  CodecUtil.WriteHeader(@out, CODEC, VERSION_CURRENT);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.CloseWhileHandlingException(@out);
		  }
		}
	  }

	  /// <summary>
	  /// Write an int to the primary file </summary>
	  public override void Write(int v)
	  {
		@out.WriteVInt(v);
	  }

	  public override Index Index()
	  {
		return new MockSingleIntIndexOutputIndex(this);
	  }

	  public override void Close()
	  {
		@out.Close();
	  }

	  public override string ToString()
	  {
		return "MockSingleIntIndexOutput fp=" + @out.FilePointer;
	  }

	  private class MockSingleIntIndexOutputIndex : IntIndexOutput.Index
	  {
		  private readonly MockSingleIntIndexOutput OuterInstance;

		  public MockSingleIntIndexOutputIndex(MockSingleIntIndexOutput outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		internal long Fp;
		internal long LastFP;
		public override void Mark()
		{
		  Fp = OuterInstance.@out.FilePointer;
		}
		public override void CopyFrom(IntIndexOutput.Index other, bool copyLast)
		{
		  Fp = ((MockSingleIntIndexOutputIndex) other).Fp;
		  if (copyLast)
		  {
			LastFP = ((MockSingleIntIndexOutputIndex) other).Fp;
		  }
		}
		public override void Write(DataOutput indexOut, bool absolute)
		{
		  if (absolute)
		  {
			indexOut.WriteVLong(Fp);
		  }
		  else
		  {
			indexOut.WriteVLong(Fp - LastFP);
		  }
		  LastFP = Fp;
		}

		public override string ToString()
		{
		  return Convert.ToString(Fp);
		}
	  }
	}

}