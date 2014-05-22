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


	/// <summary>
	/// Simple implementation of <seealso cref="ChecksumIndexInput"/> that wraps
	/// another input and delegates calls.
	/// </summary>
	public class BufferedChecksumIndexInput : ChecksumIndexInput
	{
	  internal readonly IndexInput Main;
	  internal readonly Checksum Digest;

	  /// <summary>
	  /// Creates a new BufferedChecksumIndexInput </summary>
	  public BufferedChecksumIndexInput(IndexInput main) : base("BufferedChecksumIndexInput(" + main + ")")
	  {
		this.Main = main;
		this.Digest = new BufferedChecksum(new CRC32());
	  }

	  public override sbyte ReadByte()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte b = main.readByte();
		sbyte b = Main.ReadByte();
		Digest.update(b);
		return b;
	  }

	  public override void ReadBytes(sbyte[] b, int offset, int len)
	  {
		Main.ReadBytes(b, offset, len);
		Digest.update(b, offset, len);
	  }

	  public override long Checksum
	  {
		  get
		  {
			return Digest.Value;
		  }
	  }

	  public override void Close()
	  {
		Main.Close();
	  }

	  public override long FilePointer
	  {
		  get
		  {
			return Main.FilePointer;
		  }
	  }

	  public override long Length()
	  {
		return Main.Length();
	  }

	  public override IndexInput Clone()
	  {
		throw new System.NotSupportedException();
	  }
	}

}