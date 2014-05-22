using System;
using System.Diagnostics;

// this file has been automatically generated, DO NOT EDIT

namespace Lucene.Net.Util.Packed
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

	using DataInput = Lucene.Net.Store.DataInput;


	/// <summary>
	/// Packs integers into 3 bytes (24 bits per value).
	/// @lucene.internal
	/// </summary>
	internal sealed class Packed8ThreeBlocks : PackedInts.MutableImpl
	{
	  internal readonly sbyte[] Blocks;

	  public static readonly int MAX_SIZE = int.MaxValue / 3;

	  internal Packed8ThreeBlocks(int valueCount) : base(valueCount, 24)
	  {
		if (valueCount > MAX_SIZE)
		{
		  throw new System.IndexOutOfRangeException("MAX_SIZE exceeded");
		}
		Blocks = new sbyte[valueCount * 3];
	  }

	  internal Packed8ThreeBlocks(int packedIntsVersion, DataInput @in, int valueCount) : this(valueCount)
	  {
		@in.ReadBytes(Blocks, 0, 3 * valueCount);
		// because packed ints have not always been byte-aligned
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int remaining = (int)(PackedInts.Format.PACKED.byteCount(packedIntsVersion, valueCount, 24) - 3L * valueCount * 1);
		int remaining = (int)(PackedInts.Format.PACKED.byteCount(packedIntsVersion, valueCount, 24) - 3L * valueCount * 1);
		for (int i = 0; i < remaining; ++i)
		{
		   @in.ReadByte();
		}
	  }

	  public override long Get(int index)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int o = index * 3;
		int o = index * 3;
		return (Blocks[o] & 0xFFL) << 16 | (Blocks[o + 1] & 0xFFL) << 8 | (Blocks[o + 2] & 0xFFL);
	  }

	  public override int Get(int index, long[] arr, int off, int len)
	  {
		Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
		Debug.Assert(index >= 0 && index < ValueCount);
		Debug.Assert(off + len <= arr.Length);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int gets = Math.min(valueCount - index, len);
		int gets = Math.Min(ValueCount - index, len);
		for (int i = index * 3, end = (index + gets) * 3; i < end; i += 3)
		{
		  arr[off++] = (Blocks[i] & 0xFFL) << 16 | (Blocks[i + 1] & 0xFFL) << 8 | (Blocks[i + 2] & 0xFFL);
		}
		return gets;
	  }

	  public override void Set(int index, long value)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int o = index * 3;
		int o = index * 3;
		Blocks[o] = (sbyte)((long)((ulong)value >> 16));
		Blocks[o + 1] = (sbyte)((long)((ulong)value >> 8));
		Blocks[o + 2] = (sbyte) value;
	  }

	  public override int Set(int index, long[] arr, int off, int len)
	  {
		Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
		Debug.Assert(index >= 0 && index < ValueCount);
		Debug.Assert(off + len <= arr.Length);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int sets = Math.min(valueCount - index, len);
		int sets = Math.Min(ValueCount - index, len);
		for (int i = off, o = index * 3, end = off + sets; i < end; ++i)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long value = arr[i];
		  long value = arr[i];
		  Blocks[o++] = (sbyte)((long)((ulong)value >> 16));
		  Blocks[o++] = (sbyte)((long)((ulong)value >> 8));
		  Blocks[o++] = (sbyte) value;
		}
		return sets;
	  }

	  public override void Fill(int fromIndex, int toIndex, long val)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte block1 = (byte)(val >>> 16);
		sbyte block1 = (sbyte)((long)((ulong)val >> 16));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte block2 = (byte)(val >>> 8);
		sbyte block2 = (sbyte)((long)((ulong)val >> 8));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte block3 = (byte) val;
		sbyte block3 = (sbyte) val;
		for (int i = fromIndex * 3, end = toIndex * 3; i < end; i += 3)
		{
		  Blocks[i] = block1;
		  Blocks[i + 1] = block2;
		  Blocks[i + 2] = block3;
		}
	  }

	  public override void Clear()
	  {
		Arrays.fill(Blocks, (sbyte) 0);
	  }

	  public override long RamBytesUsed()
	  {
		return RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 2 * RamUsageEstimator.NUM_BYTES_INT + RamUsageEstimator.NUM_BYTES_OBJECT_REF) + RamUsageEstimator.SizeOf(Blocks); // blocks ref -  valueCount,bitsPerValue
	  }

	  public override string ToString()
	  {
		return this.GetType().SimpleName + "(bitsPerValue=" + BitsPerValue_Renamed + ", size=" + Size() + ", elements.length=" + Blocks.Length + ")";
	  }
	}

}