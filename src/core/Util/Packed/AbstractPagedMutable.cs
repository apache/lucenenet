using System;
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

	/// <summary>
	/// Base implementation for <seealso cref="PagedMutable"/> and <seealso cref="PagedGrowableWriter"/>.
	/// @lucene.internal
	/// </summary>
	public abstract class AbstractPagedMutable<T> : LongValues where T : AbstractPagedMutable<T>
	{

	  internal static readonly int MIN_BLOCK_SIZE = 1 << 6;
	  internal static readonly int MAX_BLOCK_SIZE = 1 << 30;

	  internal readonly long Size_Renamed;
	  internal readonly int PageShift;
	  internal readonly int PageMask;
	  internal readonly PackedInts.Mutable[] SubMutables;
	  internal readonly int BitsPerValue;

	  internal AbstractPagedMutable(int bitsPerValue, long size, int pageSize)
	  {
		this.BitsPerValue = bitsPerValue;
		this.Size_Renamed = size;
        PageShift = PackedInts.CheckBlockSize(pageSize, MIN_BLOCK_SIZE, MAX_BLOCK_SIZE);
		PageMask = pageSize - 1;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numPages = numBlocks(size, pageSize);
        int numPages = PackedInts.NumBlocks(size, pageSize);
		SubMutables = new PackedInts.Mutable[numPages];
	  }

	  protected internal void FillPages()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numPages = numBlocks(size, pageSize());
        int numPages = PackedInts.NumBlocks(Size_Renamed, PageSize());
		for (int i = 0; i < numPages; ++i)
		{
		  // do not allocate for more entries than necessary on the last page
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int valueCount = i == numPages - 1 ? lastPageSize(size) : pageSize();
		  int valueCount = i == numPages - 1 ? LastPageSize(Size_Renamed) : PageSize();
		  SubMutables[i] = NewMutable(valueCount, BitsPerValue);
		}
	  }

	  protected internal abstract PackedInts.Mutable NewMutable(int valueCount, int bitsPerValue);

	  internal int LastPageSize(long size)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int sz = indexInPage(size);
		int sz = IndexInPage(size);
		return sz == 0 ? PageSize() : sz;
	  }

	  internal int PageSize()
	  {
		return PageMask + 1;
	  }

	  /// <summary>
	  /// The number of values. </summary>
	  public long Size()
	  {
		return Size_Renamed;
	  }

	  internal int PageIndex(long index)
	  {
		return (int)((long)((ulong)index >> PageShift));
	  }

	  internal int IndexInPage(long index)
	  {
		return (int) index & PageMask;
	  }

	  public override sealed long Get(long index)
	  {
		Debug.Assert(index >= 0 && index < Size_Renamed);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pageIndex = pageIndex(index);
		int pageIndex = PageIndex(index);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int indexInPage = indexInPage(index);
		int indexInPage = IndexInPage(index);
		return SubMutables[pageIndex].Get(indexInPage);
	  }

	  /// <summary>
	  /// Set value at <code>index</code>. </summary>
	  public void Set(long index, long value)
	  {
		Debug.Assert(index >= 0 && index < Size_Renamed);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pageIndex = pageIndex(index);
		int pageIndex = PageIndex(index);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int indexInPage = indexInPage(index);
		int indexInPage = IndexInPage(index);
		SubMutables[pageIndex].Set(indexInPage, value);
	  }

	  protected internal virtual long BaseRamBytesUsed()
	  {
		return RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_LONG + 3 * RamUsageEstimator.NUM_BYTES_INT;
	  }

	  /// <summary>
	  /// Return the number of bytes used by this object. </summary>
	  public virtual long RamBytesUsed()
	  {
		long bytesUsed = RamUsageEstimator.AlignObjectSize(BaseRamBytesUsed());
		bytesUsed += RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + (long) RamUsageEstimator.NUM_BYTES_OBJECT_REF * SubMutables.Length);
		foreach (PackedInts.Mutable gw in SubMutables)
		{
		  bytesUsed += gw.RamBytesUsed();
		}
		return bytesUsed;
	  }

	  protected internal abstract T NewUnfilledCopy(long newSize);

	  /// <summary>
	  /// Create a new copy of size <code>newSize</code> based on the content of
	  ///  this buffer. this method is much more efficient than creating a new
	  ///  instance and copying values one by one. 
	  /// </summary>
	  public T Resize(long newSize)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final T copy = newUnfilledCopy(newSize);
		T copy = NewUnfilledCopy(newSize);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numCommonPages = Math.min(copy.subMutables.length, subMutables.length);
		int numCommonPages = Math.Min(copy.SubMutables.Length, SubMutables.Length);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long[] copyBuffer = new long[1024];
		long[] copyBuffer = new long[1024];
		for (int i = 0; i < copy.SubMutables.Length; ++i)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int valueCount = i == copy.subMutables.length - 1 ? lastPageSize(newSize) : pageSize();
		  int valueCount = i == copy.SubMutables.Length - 1 ? LastPageSize(newSize) : PageSize();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bpv = i < numCommonPages ? subMutables[i].getBitsPerValue() : this.bitsPerValue;
		  int bpv = i < numCommonPages ? SubMutables[i].BitsPerValue : this.BitsPerValue;
		  copy.SubMutables[i] = NewMutable(valueCount, bpv);
		  if (i < numCommonPages)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int copyLength = Math.min(valueCount, subMutables[i].size());
			int copyLength = Math.Min(valueCount, SubMutables[i].Size());
			PackedInts.Copy(SubMutables[i], 0, copy.SubMutables[i], 0, copyLength, copyBuffer);
		  }
		}
		return copy;
	  }

	  /// <summary>
	  /// Similar to <seealso cref="ArrayUtil#grow(long[], int)"/>. </summary>
	  public T Grow(long minSize)
	  {
		Debug.Assert(minSize >= 0);
		if (minSize <= Size())
		{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") final T result = (T) this;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
		  T result = (T) this;
		  return result;
		}
		long extra = (long)((ulong)minSize >> 3);
		if (extra < 3)
		{
		  extra = 3;
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long newSize = minSize + extra;
		long newSize = minSize + extra;
		return Resize(newSize);
	  }

	  /// <summary>
	  /// Similar to <seealso cref="ArrayUtil#grow(long[])"/>. </summary>
	  public T Grow()
	  {
		return Grow(Size() + 1);
	  }

	  public override sealed string ToString()
	  {
		return this.GetType().Name + "(size=" + Size() + ",pageSize=" + PageSize() + ")";
	  }

	}

}