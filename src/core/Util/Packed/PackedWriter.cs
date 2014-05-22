using System.Diagnostics;

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

	using DataOutput = Lucene.Net.Store.DataOutput;


	// Packs high order byte first, to match
	// IndexOutput.writeInt/Long/Short byte order

	internal sealed class PackedWriter : PackedInts.Writer
	{

	  internal bool Finished;
	  internal readonly PackedInts.Format Format_Renamed;
	  internal readonly BulkOperation Encoder;
	  internal readonly sbyte[] NextBlocks;
	  internal readonly long[] NextValues;
	  internal readonly int Iterations;
	  internal int Off;
	  internal int Written;

	  internal PackedWriter(PackedInts.Format format, DataOutput @out, int valueCount, int bitsPerValue, int mem) : base(@out, valueCount, bitsPerValue)
	  {
		this.Format_Renamed = format;
		Encoder = BulkOperation.Of(format, bitsPerValue);
		Iterations = Encoder.ComputeIterations(valueCount, mem);
		NextBlocks = new sbyte[Iterations * Encoder.ByteBlockCount()];
		NextValues = new long[Iterations * Encoder.ByteValueCount()];
		Off = 0;
		Written = 0;
		Finished = false;
	  }

	  protected internal override PackedInts.Format Format
	  {
		  get
		  {
			return Format_Renamed;
		  }
	  }

	  public override void Add(long v)
	  {
		Debug.Assert(BitsPerValue_Renamed == 64 || (v >= 0 && v <= PackedInts.MaxValue(BitsPerValue_Renamed)), BitsPerValue_Renamed);
		Debug.Assert(!Finished);
		if (ValueCount != -1 && Written >= ValueCount)
		{
		  throw new EOFException("Writing past end of stream");
		}
		NextValues[Off++] = v;
		if (Off == NextValues.Length)
		{
		  Flush();
		}
		++Written;
	  }

	  public override void Finish()
	  {
		Debug.Assert(!Finished);
		if (ValueCount != -1)
		{
		  while (Written < ValueCount)
		  {
			Add(0L);
		  }
		}
		Flush();
		Finished = true;
	  }

	  private void Flush()
	  {
		Encoder.Encode(NextValues, 0, NextBlocks, 0, Iterations);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int blockCount = (int) format.byteCount(PackedInts.VERSION_CURRENT, off, bitsPerValue);
		int blockCount = (int) Format_Renamed.byteCount(PackedInts.VERSION_CURRENT, Off, BitsPerValue_Renamed);
		@out.WriteBytes(NextBlocks, blockCount);
		Arrays.fill(NextValues, 0L);
		Off = 0;
	  }

	  public override int Ord()
	  {
		return Written - 1;
	  }
	}

}