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

	using IntIndexInput = Lucene.Net.Codecs.sep.IntIndexInput;
	using DataInput = Lucene.Net.Store.DataInput;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;

	/// <summary>
	/// Reads IndexInputs written with {@link
	///  MockSingleIntIndexOutput}.  NOTE: this class is just for
	///  demonstration purposes (it is a very slow way to read a
	///  block of ints).
	/// 
	/// @lucene.experimental
	/// </summary>
	public class MockSingleIntIndexInput : IntIndexInput
	{
	  private readonly IndexInput @in;

	  public MockSingleIntIndexInput(Directory dir, string fileName, IOContext context)
	  {
		@in = dir.openInput(fileName, context);
		CodecUtil.checkHeader(@in, MockSingleIntIndexOutput.CODEC, MockSingleIntIndexOutput.VERSION_START, MockSingleIntIndexOutput.VERSION_START);
	  }

	  public override Reader Reader()
	  {
		return new Reader(@in.clone());
	  }

	  public override void Close()
	  {
		@in.close();
	  }

	  /// <summary>
	  /// Just reads a vInt directly from the file.
	  /// </summary>
	  public class Reader : IntIndexInput.Reader
	  {
		// clone:
		internal readonly IndexInput @in;

		public Reader(IndexInput @in)
		{
		  this.@in = @in;
		}

		/// <summary>
		/// Reads next single int </summary>
		public override int Next()
		{
		  //System.out.println("msii.next() fp=" + in.getFilePointer() + " vs " + in.length());
		  return @in.readVInt();
		}
	  }

	  internal class MockSingleIntIndexInputIndex : IntIndexInput.Index
	  {
		  private readonly MockSingleIntIndexInput OuterInstance;

		  public MockSingleIntIndexInputIndex(MockSingleIntIndexInput outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		internal long Fp;

		public override void Read(DataInput indexIn, bool absolute)
		{
		  if (absolute)
		  {
			Fp = indexIn.readVLong();
		  }
		  else
		  {
			Fp += indexIn.readVLong();
		  }
		}

		public override void CopyFrom(IntIndexInput.Index other)
		{
		  Fp = ((MockSingleIntIndexInputIndex) other).Fp;
		}

		public override void Seek(IntIndexInput.Reader other)
		{
		  ((Reader) other).@in.seek(Fp);
		}

		public override string ToString()
		{
		  return Convert.ToString(Fp);
		}

		public override Index Clone()
		{
		  MockSingleIntIndexInputIndex other = new MockSingleIntIndexInputIndex(OuterInstance);
		  other.Fp = Fp;
		  return other;
		}
	  }

	  public override Index Index()
	  {
		return new MockSingleIntIndexInputIndex(this);
	  }
	}


}