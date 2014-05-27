using System;
using System.Diagnostics;
using System.Collections.Generic;

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


	using Lucene.Net.Util;

	/// <summary>
	/// Base IndexInput implementation that uses an array
	/// of ByteBuffers to represent a file.
	/// <p>
	/// Because Java's ByteBuffer uses an int to address the
	/// values, it's necessary to access a file greater
	/// Integer.MAX_VALUE in size using multiple byte buffers.
	/// <p>
	/// For efficiency, this class requires that the buffers
	/// are a power-of-two (<code>chunkSizePower</code>).
	/// </summary>
	internal abstract class ByteBufferIndexInput : IndexInput
	{
	  private ByteBuffer[] Buffers;

	  private readonly long ChunkSizeMask;
	  private readonly int ChunkSizePower;

	  private int Offset;
	  private long Length_Renamed;
	  private string SliceDescription;

	  private int CurBufIndex;

	  private ByteBuffer CurBuf; // redundant for speed: buffers[curBufIndex]

	  private bool IsClone = false;
	  private readonly WeakIdentityMap<ByteBufferIndexInput, bool?> Clones;

	  internal ByteBufferIndexInput(string resourceDescription, ByteBuffer[] buffers, long length, int chunkSizePower, bool trackClones) : base(resourceDescription)
	  {
		this.Buffers = buffers;
		this.Length_Renamed = length;
		this.ChunkSizePower = chunkSizePower;
		this.ChunkSizeMask = (1L << chunkSizePower) - 1L;
		this.Clones = trackClones ? WeakIdentityMap.NewConcurrentHashMap<ByteBufferIndexInput, bool?>() : null;

		Debug.Assert(chunkSizePower >= 0 && chunkSizePower <= 30);
		assert((long)((ulong)length >> chunkSizePower)) < int.MaxValue;

		Seek(0L);
	  }

	  public override sealed sbyte ReadByte()
	  {
		try
		{
		  return CurBuf.get();
		}
		catch (BufferUnderflowException e)
		{
		  do
		  {
			CurBufIndex++;
			if (CurBufIndex >= Buffers.Length)
			{
			  throw new EOFException("read past EOF: " + this);
			}
			CurBuf = Buffers[CurBufIndex];
			CurBuf.position(0);
		  } while (!CurBuf.hasRemaining());
		  return CurBuf.get();
		}
		catch (System.NullReferenceException npe)
		{
		  throw new AlreadyClosedException("Already closed: " + this);
		}
	  }

	  public override sealed void ReadBytes(sbyte[] b, int offset, int len)
	  {
		try
		{
		  CurBuf.get(b, offset, len);
		}
		catch (BufferUnderflowException e)
		{
		  int curAvail = CurBuf.remaining();
		  while (len > curAvail)
		  {
			CurBuf.get(b, offset, curAvail);
			len -= curAvail;
			offset += curAvail;
			CurBufIndex++;
			if (CurBufIndex >= Buffers.Length)
			{
			  throw new EOFException("read past EOF: " + this);
			}
			CurBuf = Buffers[CurBufIndex];
			CurBuf.position(0);
			curAvail = CurBuf.remaining();
		  }
		  CurBuf.get(b, offset, len);
		}
		catch (System.NullReferenceException npe)
		{
		  throw new AlreadyClosedException("Already closed: " + this);
		}
	  }

	  public override sealed short ReadShort()
	  {
		try
		{
		  return CurBuf.Short;
		}
		catch (BufferUnderflowException e)
		{
		  return base.ReadShort();
		}
		catch (System.NullReferenceException npe)
		{
		  throw new AlreadyClosedException("Already closed: " + this);
		}
	  }

	  public override sealed int ReadInt()
	  {
		try
		{
		  return CurBuf.Int;
		}
		catch (BufferUnderflowException e)
		{
		  return base.ReadInt();
		}
		catch (System.NullReferenceException npe)
		{
		  throw new AlreadyClosedException("Already closed: " + this);
		}
	  }

	  public override sealed long ReadLong()
	  {
		try
		{
		  return CurBuf.Long;
		}
		catch (BufferUnderflowException e)
		{
		  return base.ReadLong();
		}
		catch (System.NullReferenceException npe)
		{
		  throw new AlreadyClosedException("Already closed: " + this);
		}
	  }

	  public override sealed long FilePointer
	  {
		  get
		  {
			try
			{
			  return (((long) CurBufIndex) << ChunkSizePower) + CurBuf.position() - Offset;
			}
			catch (System.NullReferenceException npe)
			{
			  throw new AlreadyClosedException("Already closed: " + this);
			}
		  }
	  }

	  public override sealed void Seek(long pos)
	  {
		// necessary in case offset != 0 and pos < 0, but pos >= -offset
		if (pos < 0L)
		{
		  throw new System.ArgumentException("Seeking to negative position: " + this);
		}
		pos += Offset;
		// we use >> here to preserve negative, so we will catch AIOOBE,
		// in case pos + offset overflows.
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bi = (int)(pos >> chunkSizePower);
		int bi = (int)(pos >> ChunkSizePower);
		try
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.nio.ByteBuffer b = buffers[bi];
		  ByteBuffer b = Buffers[bi];
		  b.position((int)(pos & ChunkSizeMask));
		  // write values, on exception all is unchanged
		  this.CurBufIndex = bi;
		  this.CurBuf = b;
		}
		catch (System.IndexOutOfRangeException aioobe)
		{
		  throw new EOFException("seek past EOF: " + this);
		}
		catch (System.ArgumentException iae)
		{
		  throw new EOFException("seek past EOF: " + this);
		}
		catch (System.NullReferenceException npe)
		{
		  throw new AlreadyClosedException("Already closed: " + this);
		}
	  }

	  public override sealed long Length()
	  {
		return Length_Renamed;
	  }

	  public override sealed ByteBufferIndexInput Clone()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ByteBufferIndexInput clone = buildSlice(0L, this.length);
		ByteBufferIndexInput clone = BuildSlice(0L, this.Length_Renamed);
		try
		{
		  clone.Seek(FilePointer);
		}
		catch (System.IO.IOException ioe)
		{
		  throw new Exception("Should never happen: " + this, ioe);
		}

		return clone;
	  }

	  /// <summary>
	  /// Creates a slice of this index input, with the given description, offset, and length. The slice is seeked to the beginning.
	  /// </summary>
	  public ByteBufferIndexInput Slice(string sliceDescription, long offset, long length)
	  {
		if (IsClone) // well we could, but this is stupid
		{
		  throw new InvalidOperationException("cannot slice() " + sliceDescription + " from a cloned IndexInput: " + this);
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ByteBufferIndexInput clone = buildSlice(offset, length);
		ByteBufferIndexInput clone = BuildSlice(offset, length);
		clone.SliceDescription = sliceDescription;
		try
		{
		  clone.Seek(0L);
		}
		catch (System.IO.IOException ioe)
		{
		  throw new Exception("Should never happen: " + this, ioe);
		}

		return clone;
	  }

	  private ByteBufferIndexInput BuildSlice(long offset, long length)
	  {
		if (Buffers == null)
		{
		  throw new AlreadyClosedException("Already closed: " + this);
		}
		if (offset < 0 || length < 0 || offset + length > this.Length_Renamed)
		{
		  throw new System.ArgumentException("slice() " + SliceDescription + " out of bounds: offset=" + offset + ",length=" + length + ",fileLength=" + this.Length_Renamed + ": " + this);
		}

		// include our own offset into the final offset:
		offset += this.Offset;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ByteBufferIndexInput clone = (ByteBufferIndexInput)base.clone();
		ByteBufferIndexInput clone = (ByteBufferIndexInput)base.Clone();
		clone.IsClone = true;
		// we keep clone.clones, so it shares the same map with original and we have no additional cost on clones
		Debug.Assert(clone.Clones == this.Clones);
		clone.Buffers = BuildSlice(Buffers, offset, length);
		clone.Offset = (int)(offset & ChunkSizeMask);
		clone.Length_Renamed = length;

		// register the new clone in our clone list to clean it up on closing:
		if (Clones != null)
		{
		  this.Clones.Put(clone, true);
		}

		return clone;
	  }

	  /// <summary>
	  /// Returns a sliced view from a set of already-existing buffers: 
	  ///  the last buffer's limit() will be correct, but
	  ///  you must deal with offset separately (the first buffer will not be adjusted) 
	  /// </summary>
	  private ByteBuffer[] BuildSlice(ByteBuffer[] buffers, long offset, long length)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long sliceEnd = offset + length;
		long sliceEnd = offset + length;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startIndex = (int)(offset >>> chunkSizePower);
		int startIndex = (int)((long)((ulong)offset >> ChunkSizePower));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endIndex = (int)(sliceEnd >>> chunkSizePower);
		int endIndex = (int)((long)((ulong)sliceEnd >> ChunkSizePower));

		// we always allocate one more slice, the last one may be a 0 byte one
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.nio.ByteBuffer slices[] = new java.nio.ByteBuffer[endIndex - startIndex + 1];
		ByteBuffer[] slices = new ByteBuffer[endIndex - startIndex + 1];

		for (int i = 0; i < slices.Length; i++)
		{
		  slices[i] = buffers[startIndex + i].duplicate();
		}

		// set the last buffer's limit for the sliced view.
		slices[slices.Length - 1].limit((int)(sliceEnd & ChunkSizeMask));

		return slices;
	  }

	  private void UnsetBuffers()
	  {
		Buffers = null;
		CurBuf = null;
		CurBufIndex = 0;
	  }

	  public override sealed void Close()
	  {
		try
		{
		  if (Buffers == null)
		  {
			  return;
		  }

		  // make local copy, then un-set early
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.nio.ByteBuffer[] bufs = buffers;
		  ByteBuffer[] bufs = Buffers;
		  UnsetBuffers();
		  if (Clones != null)
		  {
			Clones.Remove(this);
		  }

		  if (IsClone)
		  {
			  return;
		  }

		  // for extra safety unset also all clones' buffers:
		  if (Clones != null)
		  {
			for (IEnumerator<ByteBufferIndexInput> it = this.Clones.KeyIterator(); it.MoveNext();)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ByteBufferIndexInput clone = it.Current;
			  ByteBufferIndexInput clone = it.Current;
			  Debug.Assert(clone.IsClone);
			  clone.UnsetBuffers();
			}
			this.Clones.Clear();
		  }

		  foreach (ByteBuffer b in bufs)
		  {
			FreeBuffer(b);
		  }
		}
		finally
		{
		  UnsetBuffers();
		}
	  }

	  /// <summary>
	  /// Called when the contents of a buffer will be no longer needed.
	  /// </summary>
	  protected internal abstract void FreeBuffer(ByteBuffer b);

	  public override sealed string ToString()
	  {
		if (SliceDescription != null)
		{
		  return base.ToString() + " [slice=" + SliceDescription + "]";
		}
		else
		{
		  return base.ToString();
		}
	  }
	}

}