using System.Diagnostics;

namespace org.apache.lucene.codecs.memory
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


	using FieldInfo = org.apache.lucene.index.FieldInfo;
	using IndexOptions = org.apache.lucene.index.FieldInfo.IndexOptions;
	using DataInput = org.apache.lucene.store.DataInput;
	using DataOutput = org.apache.lucene.store.DataOutput;
	using Outputs = org.apache.lucene.util.fst.Outputs;
	using LongsRef = org.apache.lucene.util.LongsRef;

	/// <summary>
	/// An FST <seealso cref="Outputs"/> implementation for 
	/// <seealso cref="FSTTermsWriter"/>.
	/// 
	/// @lucene.experimental
	/// </summary>

	// NOTE: outputs should be per-field, since
	// longsSize is fixed for each field
	internal class FSTTermOutputs : Outputs<FSTTermOutputs.TermData>
	{
	  private static readonly TermData NO_OUTPUT = new TermData();
	  //private static boolean TEST = false;
	  private readonly bool hasPos;
	  private readonly int longsSize;

	  /// <summary>
	  /// Represents the metadata for one term.
	  /// On an FST, only long[] part is 'shared' and pushed towards root.
	  /// byte[] and term stats will be kept on deeper arcs.
	  /// </summary>
	  internal class TermData
	  {
		internal long[] longs;
		internal sbyte[] bytes;
		internal int docFreq;
		internal long totalTermFreq;
		internal TermData()
		{
		  this.longs = null;
		  this.bytes = null;
		  this.docFreq = 0;
		  this.totalTermFreq = -1;
		}
		internal TermData(long[] longs, sbyte[] bytes, int docFreq, long totalTermFreq)
		{
		  this.longs = longs;
		  this.bytes = bytes;
		  this.docFreq = docFreq;
		  this.totalTermFreq = totalTermFreq;
		}

		// NOTE: actually, FST nodes are seldom 
		// identical when outputs on their arcs 
		// aren't NO_OUTPUTs.
		public override int GetHashCode()
		{
		  int hash = 0;
		  if (longs != null)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = longs.length;
			int end = longs.Length;
			for (int i = 0; i < end; i++)
			{
			  hash -= (int)longs[i];
			}
		  }
		  if (bytes != null)
		  {
			hash = -hash;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = bytes.length;
			int end = bytes.Length;
			for (int i = 0; i < end; i++)
			{
			  hash += bytes[i];
			}
		  }
		  hash += (int)(docFreq + totalTermFreq);
		  return hash;
		}

		public override bool Equals(object other_)
		{
		  if (other_ == this)
		  {
			return true;
		  }
		  else if (!(other_ is FSTTermOutputs.TermData))
		  {
			return false;
		  }
		  TermData other = (TermData) other_;
		  return statsEqual(this, other) && longsEqual(this, other) && bytesEqual(this, other);

		}
	  }

	  protected internal FSTTermOutputs(FieldInfo fieldInfo, int longsSize)
	  {
		this.hasPos = (fieldInfo.IndexOptions != FieldInfo.IndexOptions.DOCS_ONLY);
		this.longsSize = longsSize;
	  }

	  public override TermData common(TermData t1, TermData t2)
	  //
	  // The return value will be the smaller one, when these two are 
	  // 'comparable', i.e. 
	  // 1. every value in t1 is not larger than in t2, or
	  // 2. every value in t1 is not smaller than t2.
	  //
	  {
		//if (TEST) System.out.print("common("+t1+", "+t2+") = ");
		if (t1 == NO_OUTPUT || t2 == NO_OUTPUT)
		{
		  //if (TEST) System.out.println("ret:"+NO_OUTPUT);
		  return NO_OUTPUT;
		}
		Debug.Assert(t1.longs.Length == t2.longs.Length);

		long[] min = t1.longs, max = t2.longs;
		int pos = 0;
		TermData ret;

		while (pos < longsSize && min[pos] == max[pos])
		{
		  pos++;
		}
		if (pos < longsSize) // unequal long[]
		{
		  if (min[pos] > max[pos])
		  {
			min = t2.longs;
			max = t1.longs;
		  }
		  // check whether strictly smaller
		  while (pos < longsSize && min[pos] <= max[pos])
		  {
			pos++;
		  }
		  if (pos < longsSize || allZero(min)) // not comparable or all-zero
		  {
			ret = NO_OUTPUT;
		  }
		  else
		  {
			ret = new TermData(min, null, 0, -1);
		  }
		} // equal long[]
		else
		{
		  if (statsEqual(t1, t2) && bytesEqual(t1, t2))
		  {
			ret = t1;
		  }
		  else if (allZero(min))
		  {
			ret = NO_OUTPUT;
		  }
		  else
		  {
			ret = new TermData(min, null, 0, -1);
		  }
		}
		//if (TEST) System.out.println("ret:"+ret);
		return ret;
	  }

	  public override TermData subtract(TermData t1, TermData t2)
	  {
		//if (TEST) System.out.print("subtract("+t1+", "+t2+") = ");
		if (t2 == NO_OUTPUT)
		{
		  //if (TEST) System.out.println("ret:"+t1);
		  return t1;
		}
		Debug.Assert(t1.longs.Length == t2.longs.Length);

		int pos = 0;
		long diff = 0;
		long[] share = new long[longsSize];

		while (pos < longsSize)
		{
		  share[pos] = t1.longs[pos] - t2.longs[pos];
		  diff += share[pos];
		  pos++;
		}

		TermData ret;
		if (diff == 0 && statsEqual(t1, t2) && bytesEqual(t1, t2))
		{
		  ret = NO_OUTPUT;
		}
		else
		{
		  ret = new TermData(share, t1.bytes, t1.docFreq, t1.totalTermFreq);
		}
		//if (TEST) System.out.println("ret:"+ret);
		return ret;
	  }

	  // TODO: if we refactor a 'addSelf(TermData other)',
	  // we can gain about 5~7% for fuzzy queries, however this also 
	  // means we are putting too much stress on FST Outputs decoding?
	  public override TermData add(TermData t1, TermData t2)
	  {
		//if (TEST) System.out.print("add("+t1+", "+t2+") = ");
		if (t1 == NO_OUTPUT)
		{
		  //if (TEST) System.out.println("ret:"+t2);
		  return t2;
		}
		else if (t2 == NO_OUTPUT)
		{
		  //if (TEST) System.out.println("ret:"+t1);
		  return t1;
		}
		Debug.Assert(t1.longs.Length == t2.longs.Length);

		int pos = 0;
		long[] accum = new long[longsSize];

		while (pos < longsSize)
		{
		  accum[pos] = t1.longs[pos] + t2.longs[pos];
		  pos++;
		}

		TermData ret;
		if (t2.bytes != null || t2.docFreq > 0)
		{
		  ret = new TermData(accum, t2.bytes, t2.docFreq, t2.totalTermFreq);
		}
		else
		{
		  ret = new TermData(accum, t1.bytes, t1.docFreq, t1.totalTermFreq);
		}
		//if (TEST) System.out.println("ret:"+ret);
		return ret;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void write(TermData data, org.apache.lucene.store.DataOutput out) throws java.io.IOException
	  public override void write(TermData data, DataOutput @out)
	  {
		int bit0 = allZero(data.longs) ? 0 : 1;
		int bit1 = ((data.bytes == null || data.bytes.Length == 0) ? 0 : 1) << 1;
		int bit2 = ((data.docFreq == 0) ? 0 : 1) << 2;
		int bits = bit0 | bit1 | bit2;
		if (bit1 > 0) // determine extra length
		{
		  if (data.bytes.Length < 32)
		  {
			bits |= (data.bytes.Length << 3);
			@out.writeByte((sbyte)bits);
		  }
		  else
		  {
			@out.writeByte((sbyte)bits);
			@out.writeVInt(data.bytes.Length);
		  }
		}
		else
		{
		  @out.writeByte((sbyte)bits);
		}
		if (bit0 > 0) // not all-zero case
		{
		  for (int pos = 0; pos < longsSize; pos++)
		  {
			@out.writeVLong(data.longs[pos]);
		  }
		}
		if (bit1 > 0) // bytes exists
		{
		  @out.writeBytes(data.bytes, 0, data.bytes.Length);
		}
		if (bit2 > 0) // stats exist
		{
		  if (hasPos)
		  {
			if (data.docFreq == data.totalTermFreq)
			{
			  @out.writeVInt((data.docFreq << 1) | 1);
			}
			else
			{
			  @out.writeVInt((data.docFreq << 1));
			  @out.writeVLong(data.totalTermFreq - data.docFreq);
			}
		  }
		  else
		  {
			@out.writeVInt(data.docFreq);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public TermData read(org.apache.lucene.store.DataInput in) throws java.io.IOException
	  public override TermData read(DataInput @in)
	  {
		long[] longs = new long[longsSize];
		sbyte[] bytes = null;
		int docFreq = 0;
		long totalTermFreq = -1;
		int bits = @in.readByte() & 0xff;
		int bit0 = bits & 1;
		int bit1 = bits & 2;
		int bit2 = bits & 4;
		int bytesSize = ((int)((uint)bits >> 3));
		if (bit1 > 0 && bytesSize == 0) // determine extra length
		{
		  bytesSize = @in.readVInt();
		}
		if (bit0 > 0) // not all-zero case
		{
		  for (int pos = 0; pos < longsSize; pos++)
		  {
			longs[pos] = @in.readVLong();
		  }
		}
		if (bit1 > 0) // bytes exists
		{
		  bytes = new sbyte[bytesSize];
		  @in.readBytes(bytes, 0, bytesSize);
		}
		if (bit2 > 0) // stats exist
		{
		  int code = @in.readVInt();
		  if (hasPos)
		  {
			totalTermFreq = docFreq = (int)((uint)code >> 1);
			if ((code & 1) == 0)
			{
			  totalTermFreq += @in.readVLong();
			}
		  }
		  else
		  {
			docFreq = code;
		  }
		}
		return new TermData(longs, bytes, docFreq, totalTermFreq);
	  }

	  public override TermData NoOutput
	  {
		  get
		  {
			return NO_OUTPUT;
		  }
	  }

	  public override string outputToString(TermData data)
	  {
		return data.ToString();
	  }

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: static boolean statsEqual(final TermData t1, final TermData t2)
	  internal static bool statsEqual(TermData t1, TermData t2)
	  {
		return t1.docFreq == t2.docFreq && t1.totalTermFreq == t2.totalTermFreq;
	  }
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: static boolean bytesEqual(final TermData t1, final TermData t2)
	  internal static bool bytesEqual(TermData t1, TermData t2)
	  {
		if (t1.bytes == null && t2.bytes == null)
		{
		  return true;
		}
		return t1.bytes != null && t2.bytes != null && Arrays.Equals(t1.bytes, t2.bytes);
	  }
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: static boolean longsEqual(final TermData t1, final TermData t2)
	  internal static bool longsEqual(TermData t1, TermData t2)
	  {
		if (t1.longs == null && t2.longs == null)
		{
		  return true;
		}
		return t1.longs != null && t2.longs != null && Arrays.Equals(t1.longs, t2.longs);
	  }
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: static boolean allZero(final long[] l)
	  internal static bool allZero(long[] l)
	  {
		for (int i = 0; i < l.Length; i++)
		{
		  if (l[i] != 0)
		  {
			return false;
		  }
		}
		return true;
	  }
	}

}