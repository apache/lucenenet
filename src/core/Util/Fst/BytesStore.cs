using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Util.Fst
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
	using DataOutput = Lucene.Net.Store.DataOutput;

	// TODO: merge with PagedBytes, except PagedBytes doesn't
	// let you read while writing which FST needs

	internal class BytesStore : DataOutput
	{

	  private readonly IList<sbyte[]> Blocks = new List<sbyte[]>();

	  private readonly int BlockSize;
	  private readonly int BlockBits_Renamed;
	  private readonly int BlockMask;

	  private sbyte[] Current;
	  private int NextWrite;

	  public BytesStore(int blockBits)
	  {
		this.BlockBits_Renamed = blockBits;
		BlockSize = 1 << blockBits;
		BlockMask = BlockSize-1;
		NextWrite = BlockSize;
	  }

	  /// <summary>
	  /// Pulls bytes from the provided IndexInput. </summary>
	  public BytesStore(DataInput @in, long numBytes, int maxBlockSize)
	  {
		int blockSize = 2;
		int blockBits = 1;
		while (blockSize < numBytes && blockSize < maxBlockSize)
		{
		  blockSize *= 2;
		  blockBits++;
		}
		this.BlockBits_Renamed = blockBits;
		this.BlockSize = blockSize;
		this.BlockMask = blockSize-1;
		long left = numBytes;
		while (left > 0)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int chunk = (int) Math.min(blockSize, left);
		  int chunk = (int) Math.Min(blockSize, left);
		  sbyte[] block = new sbyte[chunk];
		  @in.ReadBytes(block, 0, block.Length);
		  Blocks.Add(block);
		  left -= chunk;
		}

		// So .getPosition still works
		NextWrite = Blocks[Blocks.Count - 1].Length;
	  }

	  /// <summary>
	  /// Absolute write byte; you must ensure dest is < max
	  ///  position written so far. 
	  /// </summary>
	  public virtual void WriteByte(int dest, sbyte b)
	  {
		int blockIndex = dest >> BlockBits_Renamed;
		sbyte[] block = Blocks[blockIndex];
		block[dest & BlockMask] = b;
	  }

	  public override void WriteByte(sbyte b)
	  {
		if (NextWrite == BlockSize)
		{
		  Current = new sbyte[BlockSize];
		  Blocks.Add(Current);
		  NextWrite = 0;
		}
		Current[NextWrite++] = b;
	  }

	  public override void WriteBytes(sbyte[] b, int offset, int len)
	  {
		while (len > 0)
		{
		  int chunk = BlockSize - NextWrite;
		  if (len <= chunk)
		  {
			Array.Copy(b, offset, Current, NextWrite, len);
			NextWrite += len;
			break;
		  }
		  else
		  {
			if (chunk > 0)
			{
			  Array.Copy(b, offset, Current, NextWrite, chunk);
			  offset += chunk;
			  len -= chunk;
			}
			Current = new sbyte[BlockSize];
			Blocks.Add(Current);
			NextWrite = 0;
		  }
		}
	  }

	  internal virtual int BlockBits
	  {
		  get
		  {
			return BlockBits_Renamed;
		  }
	  }

	  /// <summary>
	  /// Absolute writeBytes without changing the current
	  ///  position.  Note: this cannot "grow" the bytes, so you
	  ///  must only call it on already written parts. 
	  /// </summary>
	  internal virtual void WriteBytes(long dest, sbyte[] b, int offset, int len)
	  {
		//System.out.println("  BS.writeBytes dest=" + dest + " offset=" + offset + " len=" + len);
		Debug.Assert(dest + len <= Position, "dest=" + dest + " pos=" + Position + " len=" + len);

		// Note: weird: must go "backwards" because copyBytes
		// calls us with overlapping src/dest.  If we
		// go forwards then we overwrite bytes before we can
		// copy them:

		/*
		int blockIndex = dest >> blockBits;
		int upto = dest & blockMask;
		byte[] block = blocks.get(blockIndex);
		while (len > 0) {
		  int chunk = blockSize - upto;
		  System.out.println("    cycle chunk=" + chunk + " len=" + len);
		  if (len <= chunk) {
		    System.arraycopy(b, offset, block, upto, len);
		    break;
		  } else {
		    System.arraycopy(b, offset, block, upto, chunk);
		    offset += chunk;
		    len -= chunk;
		    blockIndex++;
		    block = blocks.get(blockIndex);
		    upto = 0;
		  }
		}
		*/

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long end = dest + len;
		long end = dest + len;
		int blockIndex = (int)(end >> BlockBits_Renamed);
		int downTo = (int)(end & BlockMask);
		if (downTo == 0)
		{
		  blockIndex--;
		  downTo = BlockSize;
		}
		sbyte[] block = Blocks[blockIndex];

		while (len > 0)
		{
		  //System.out.println("    cycle downTo=" + downTo + " len=" + len);
		  if (len <= downTo)
		  {
			//System.out.println("      final: offset=" + offset + " len=" + len + " dest=" + (downTo-len));
			Array.Copy(b, offset, block, downTo - len, len);
			break;
		  }
		  else
		  {
			len -= downTo;
			//System.out.println("      partial: offset=" + (offset + len) + " len=" + downTo + " dest=0");
			Array.Copy(b, offset + len, block, 0, downTo);
			blockIndex--;
			block = Blocks[blockIndex];
			downTo = BlockSize;
		  }
		}
	  }

	  /// <summary>
	  /// Absolute copy bytes self to self, without changing the
	  ///  position. Note: this cannot "grow" the bytes, so must
	  ///  only call it on already written parts. 
	  /// </summary>
	  public virtual void CopyBytes(long src, long dest, int len)
	  {
		//System.out.println("BS.copyBytes src=" + src + " dest=" + dest + " len=" + len);
		Debug.Assert(src < dest);

		// Note: weird: must go "backwards" because copyBytes
		// calls us with overlapping src/dest.  If we
		// go forwards then we overwrite bytes before we can
		// copy them:

		/*
		int blockIndex = src >> blockBits;
		int upto = src & blockMask;
		byte[] block = blocks.get(blockIndex);
		while (len > 0) {
		  int chunk = blockSize - upto;
		  System.out.println("  cycle: chunk=" + chunk + " len=" + len);
		  if (len <= chunk) {
		    writeBytes(dest, block, upto, len);
		    break;
		  } else {
		    writeBytes(dest, block, upto, chunk);
		    blockIndex++;
		    block = blocks.get(blockIndex);
		    upto = 0;
		    len -= chunk;
		    dest += chunk;
		  }
		}
		*/

		long end = src + len;

		int blockIndex = (int)(end >> BlockBits_Renamed);
		int downTo = (int)(end & BlockMask);
		if (downTo == 0)
		{
		  blockIndex--;
		  downTo = BlockSize;
		}
		sbyte[] block = Blocks[blockIndex];

		while (len > 0)
		{
		  //System.out.println("  cycle downTo=" + downTo);
		  if (len <= downTo)
		  {
			//System.out.println("    finish");
			WriteBytes(dest, block, downTo - len, len);
			break;
		  }
		  else
		  {
			//System.out.println("    partial");
			len -= downTo;
			WriteBytes(dest + len, block, 0, downTo);
			blockIndex--;
			block = Blocks[blockIndex];
			downTo = BlockSize;
		  }
		}
	  }

	  /// <summary>
	  /// Writes an int at the absolute position without
	  ///  changing the current pointer. 
	  /// </summary>
	  public virtual void WriteInt(long pos, int value)
	  {
		int blockIndex = (int)(pos >> BlockBits_Renamed);
		int upto = (int)(pos & BlockMask);
		sbyte[] block = Blocks[blockIndex];
		int shift = 24;
		for (int i = 0;i < 4;i++)
		{
		  block[upto++] = (sbyte)(value >> shift);
		  shift -= 8;
		  if (upto == BlockSize)
		  {
			upto = 0;
			blockIndex++;
			block = Blocks[blockIndex];
		  }
		}
	  }

	  /// <summary>
	  /// Reverse from srcPos, inclusive, to destPos, inclusive. </summary>
	  public virtual void Reverse(long srcPos, long destPos)
	  {
		Debug.Assert(srcPos < destPos);
		Debug.Assert(destPos < Position);
		//System.out.println("reverse src=" + srcPos + " dest=" + destPos);

		int srcBlockIndex = (int)(srcPos >> BlockBits_Renamed);
		int src = (int)(srcPos & BlockMask);
		sbyte[] srcBlock = Blocks[srcBlockIndex];

		int destBlockIndex = (int)(destPos >> BlockBits_Renamed);
		int dest = (int)(destPos & BlockMask);
		sbyte[] destBlock = Blocks[destBlockIndex];
		//System.out.println("  srcBlock=" + srcBlockIndex + " destBlock=" + destBlockIndex);

		int limit = (int)(destPos - srcPos + 1) / 2;
		for (int i = 0;i < limit;i++)
		{
		  //System.out.println("  cycle src=" + src + " dest=" + dest);
		  sbyte b = srcBlock[src];
		  srcBlock[src] = destBlock[dest];
		  destBlock[dest] = b;
		  src++;
		  if (src == BlockSize)
		  {
			srcBlockIndex++;
			srcBlock = Blocks[srcBlockIndex];
			//System.out.println("  set destBlock=" + destBlock + " srcBlock=" + srcBlock);
			src = 0;
		  }

		  dest--;
		  if (dest == -1)
		  {
			destBlockIndex--;
			destBlock = Blocks[destBlockIndex];
			//System.out.println("  set destBlock=" + destBlock + " srcBlock=" + srcBlock);
			dest = BlockSize-1;
		  }
		}
	  }

	  public virtual void SkipBytes(int len)
	  {
		while (len > 0)
		{
		  int chunk = BlockSize - NextWrite;
		  if (len <= chunk)
		  {
			NextWrite += len;
			break;
		  }
		  else
		  {
			len -= chunk;
			Current = new sbyte[BlockSize];
			Blocks.Add(Current);
			NextWrite = 0;
		  }
		}
	  }

	  public virtual long Position
	  {
		  get
		  {
			return ((long) Blocks.Count - 1) * BlockSize + NextWrite;
		  }
		  set
		  {
				int bufferIndex = (int)(value >> OuterInstance.BlockBits_Renamed);
				nextBuffer = bufferIndex + 1;
				OuterInstance.Current = OuterInstance.Blocks[bufferIndex];
				nextRead = (int)(value & OuterInstance.BlockMask);
				Debug.Assert(outerInstance.Position == value);
		  }
	  }

	  /// <summary>
	  /// Pos must be less than the max position written so far!
	  ///  Ie, you cannot "grow" the file with this! 
	  /// </summary>
	  public virtual void Truncate(long newLen)
	  {
		Debug.Assert(newLen <= Position);
		Debug.Assert(newLen >= 0);
		int blockIndex = (int)(newLen >> BlockBits_Renamed);
		NextWrite = (int)(newLen & BlockMask);
		if (NextWrite == 0)
		{
		  blockIndex--;
		  NextWrite = BlockSize;
		}
		Blocks.subList(blockIndex + 1, Blocks.Count).clear();
		if (newLen == 0)
		{
		  Current = null;
		}
		else
		{
		  Current = Blocks[blockIndex];
		}
		Debug.Assert(newLen == Position);
	  }

	  public virtual void Finish()
	  {
		if (Current != null)
		{
		  sbyte[] lastBuffer = new sbyte[NextWrite];
		  Array.Copy(Current, 0, lastBuffer, 0, NextWrite);
		  Blocks[Blocks.Count - 1] = lastBuffer;
		  Current = null;
		}
	  }

	  /// <summary>
	  /// Writes all of our bytes to the target <seealso cref="DataOutput"/>. </summary>
	  public virtual void WriteTo(DataOutput @out)
	  {
		foreach (sbyte[] block in Blocks)
		{
		  @out.WriteBytes(block, 0, block.Length);
		}
	  }

	  public virtual FST.BytesReader ForwardReader
	  {
		  get
		  {
			if (Blocks.Count == 1)
			{
			  return new ForwardBytesReader(Blocks[0]);
			}
			return new BytesReaderAnonymousInnerClassHelper(this);
		  }
	  }

	  private class BytesReaderAnonymousInnerClassHelper : FST.BytesReader
	  {
		  private readonly BytesStore OuterInstance;

		  public BytesReaderAnonymousInnerClassHelper(BytesStore outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  nextRead = outerInstance.BlockSize;
		  }

		  private sbyte[] OuterInstance.Current;
		  private int nextBuffer;
		  private int nextRead;

		  public override sbyte ReadByte()
		  {
			if (nextRead == OuterInstance.BlockSize)
			{
			  OuterInstance.Current = OuterInstance.Blocks[nextBuffer++];
			  nextRead = 0;
			}
			return OuterInstance.Current[nextRead++];
		  }

		  public override void SkipBytes(int count)
		  {
			Position = outerInstance.Position + count;
		  }

		  public override void ReadBytes(sbyte[] b, int offset, int len)
		  {
			while (len > 0)
			{
			  int chunkLeft = OuterInstance.BlockSize - nextRead;
			  if (len <= chunkLeft)
			  {
				Array.Copy(OuterInstance.Current, nextRead, b, offset, len);
				nextRead += len;
				break;
			  }
			  else
			  {
				if (chunkLeft > 0)
				{
				  Array.Copy(OuterInstance.Current, nextRead, b, offset, chunkLeft);
				  offset += chunkLeft;
				  len -= chunkLeft;
				}
				OuterInstance.Current = OuterInstance.Blocks[nextBuffer++];
				nextRead = 0;
			  }
			}
		  }

		  public override long Position
		  {
			  get
			  {
				return ((long) nextBuffer - 1) * OuterInstance.BlockSize + nextRead;
			  }
			  set
			  {
				// NOTE: a little weird because if you
				// setPosition(0), the next byte you read is
				// bytes[0] ... but I would expect bytes[-1] (ie,
				// EOF)...?
				int bufferIndex = (int)(value >> OuterInstance.BlockBits_Renamed);
				nextBuffer = bufferIndex - 1;
				OuterInstance.Current = OuterInstance.Blocks[bufferIndex];
				nextRead = (int)(value & OuterInstance.BlockMask);
				Debug.Assert(outerInstance.Position == value, "pos=" + value + " getPos()=" + outerInstance.Position);
			  }
		  }


		  public override bool Reversed()
		  {
			return false;
		  }
	  }

	  public virtual FST.BytesReader ReverseReader
	  {
		  get
		  {
			return GetReverseReader(true);
		  }
	  }

	  internal virtual FST.BytesReader GetReverseReader(bool allowSingle)
	  {
		if (allowSingle && Blocks.Count == 1)
		{
		  return new ReverseBytesReader(Blocks[0]);
		}
		return new BytesReaderAnonymousInnerClassHelper2(this);
	  }

	  private class BytesReaderAnonymousInnerClassHelper2 : FST.BytesReader
	  {
		  private readonly BytesStore OuterInstance;

		  public BytesReaderAnonymousInnerClassHelper2(BytesStore outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  outerInstance.Current = outerInstance.Blocks.Count == 0 ? null : outerInstance.Blocks[0];
			  nextBuffer = -1;
			  nextRead = 0;
		  }

		  private sbyte[] OuterInstance.Current;
		  private int nextBuffer;
		  private int nextRead;

		  public override sbyte ReadByte()
		  {
			if (nextRead == -1)
			{
			  OuterInstance.Current = OuterInstance.Blocks[nextBuffer--];
			  nextRead = OuterInstance.BlockSize-1;
			}
			return OuterInstance.Current[nextRead--];
		  }

		  public override void SkipBytes(int count)
		  {
			Position = outerInstance.Position - count;
		  }

		  public override void ReadBytes(sbyte[] b, int offset, int len)
		  {
			for (int i = 0;i < len;i++)
			{
			  b[offset + i] = readByte();
			}
		  }

		  public override long Position
		  {
			  get
			  {
				return ((long) nextBuffer + 1) * OuterInstance.BlockSize + nextRead;
			  }
		  }


		  public override bool Reversed()
		  {
			return true;
		  }
	  }
	}

}