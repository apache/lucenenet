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

	using BytesRef = Lucene.Net.Util.BytesRef;

	/// <summary>
	/// DataInput backed by a byte array.
	/// <b>WARNING:</b> this class omits all low-level checks.
	/// @lucene.experimental 
	/// </summary>
	public sealed class ByteArrayDataInput : DataInput
	{

	  private sbyte[] Bytes;

	  private int Pos;
	  private int Limit;

	  public ByteArrayDataInput(sbyte[] bytes)
	  {
		Reset(bytes);
	  }

	  public ByteArrayDataInput(sbyte[] bytes, int offset, int len)
	  {
		Reset(bytes, offset, len);
	  }

	  public ByteArrayDataInput()
	  {
		Reset(BytesRef.EMPTY_BYTES);
	  }

	  public void Reset(sbyte[] bytes)
	  {
		Reset(bytes, 0, bytes.Length);
	  }

	  // NOTE: sets pos to 0, which is not right if you had
	  // called reset w/ non-zero offset!!
	  public void Rewind()
	  {
		Pos = 0;
	  }

	  public int Position
	  {
		  get
		  {
			return Pos;
		  }
		  set
		  {
			this.Pos = value;
		  }
	  }


	  public void Reset(sbyte[] bytes, int offset, int len)
	  {
		this.Bytes = bytes;
		Pos = offset;
		Limit = offset + len;
	  }

	  public int Length()
	  {
		return Limit;
	  }

	  public bool Eof()
	  {
		return Pos == Limit;
	  }

	  public override void SkipBytes(long count)
	  {
		Pos += (int)count;
	  }

	  public override short ReadShort()
	  {
		return (short)(((Bytes[Pos++] & 0xFF) << 8) | (Bytes[Pos++] & 0xFF));
	  }

	  public override int ReadInt()
	  {
		return ((Bytes[Pos++] & 0xFF) << 24) | ((Bytes[Pos++] & 0xFF) << 16) | ((Bytes[Pos++] & 0xFF) << 8) | (Bytes[Pos++] & 0xFF);
	  }

	  public override long ReadLong()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int i1 = ((bytes[pos++] & 0xff) << 24) | ((bytes[pos++] & 0xff) << 16) | ((bytes[pos++] & 0xff) << 8) | (bytes[pos++] & 0xff);
		int i1 = ((Bytes[Pos++] & 0xff) << 24) | ((Bytes[Pos++] & 0xff) << 16) | ((Bytes[Pos++] & 0xff) << 8) | (Bytes[Pos++] & 0xff);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int i2 = ((bytes[pos++] & 0xff) << 24) | ((bytes[pos++] & 0xff) << 16) | ((bytes[pos++] & 0xff) << 8) | (bytes[pos++] & 0xff);
		int i2 = ((Bytes[Pos++] & 0xff) << 24) | ((Bytes[Pos++] & 0xff) << 16) | ((Bytes[Pos++] & 0xff) << 8) | (Bytes[Pos++] & 0xff);
		return (((long)i1) << 32) | (i2 & 0xFFFFFFFFL);
	  }

	  public override int ReadVInt()
	  {
		sbyte b = Bytes[Pos++];
		if (b >= 0)
		{
			return b;
		}
		int i = b & 0x7F;
		b = Bytes[Pos++];
		i |= (b & 0x7F) << 7;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		i |= (b & 0x7F) << 14;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		i |= (b & 0x7F) << 21;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		// Warning: the next ands use 0x0F / 0xF0 - beware copy/paste errors:
		i |= (b & 0x0F) << 28;
		if ((b & 0xF0) == 0)
		{
			return i;
		}
		throw new Exception("Invalid vInt detected (too many bits)");
	  }

	  public override long ReadVLong()
	  {
		sbyte b = Bytes[Pos++];
		if (b >= 0)
		{
			return b;
		}
		long i = b & 0x7FL;
		b = Bytes[Pos++];
		i |= (b & 0x7FL) << 7;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		i |= (b & 0x7FL) << 14;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		i |= (b & 0x7FL) << 21;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		i |= (b & 0x7FL) << 28;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		i |= (b & 0x7FL) << 35;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		i |= (b & 0x7FL) << 42;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		i |= (b & 0x7FL) << 49;
		if (b >= 0)
		{
			return i;
		}
		b = Bytes[Pos++];
		i |= (b & 0x7FL) << 56;
		if (b >= 0)
		{
			return i;
		}
		throw new Exception("Invalid vLong detected (negative values disallowed)");
	  }

	  // NOTE: AIOOBE not EOF if you read too much
	  public override sbyte ReadByte()
	  {
		return Bytes[Pos++];
	  }

	  // NOTE: AIOOBE not EOF if you read too much
	  public override void ReadBytes(sbyte[] b, int offset, int len)
	  {
		Array.Copy(Bytes, Pos, b, offset, len);
		Pos += len;
	  }
	}

}