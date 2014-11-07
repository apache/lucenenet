using System;
using System.Diagnostics;

namespace org.apache.lucene.analysis.util
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


	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using RamUsageEstimator = org.apache.lucene.util.RamUsageEstimator;

	/// <summary>
	/// Acts like a forever growing char[] as you read
	///  characters into it from the provided reader, but
	///  internally it uses a circular buffer to only hold the
	///  characters that haven't been freed yet.  This is like a
	///  PushbackReader, except you don't have to specify
	///  up-front the max size of the buffer, but you do have to
	///  periodically call <seealso cref="#freeBefore"/>. 
	/// </summary>

	public sealed class RollingCharBuffer
	{

	  private Reader reader;

	  private char[] buffer = new char[512];

	  // Next array index to write to in buffer:
	  private int nextWrite;

	  // Next absolute position to read from reader:
	  private int nextPos;

	  // How many valid chars (wrapped) are in the buffer:
	  private int count;

	  // True if we hit EOF
	  private bool end;

	  /// <summary>
	  /// Clear array and switch to new reader. </summary>
	  public void reset(Reader reader)
	  {
		this.reader = reader;
		nextPos = 0;
		nextWrite = 0;
		count = 0;
		end = false;
	  }

	  /* Absolute position read.  NOTE: pos must not jump
	   * ahead by more than 1!  Ie, it's OK to read arbitarily
	   * far back (just not prior to the last {@link
	   * #freeBefore}), but NOT ok to read arbitrarily far
	   * ahead.  Returns -1 if you hit EOF. */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int get(int pos) throws java.io.IOException
	  public int get(int pos)
	  {
		//System.out.println("    get pos=" + pos + " nextPos=" + nextPos + " count=" + count);
		if (pos == nextPos)
		{
		  if (end)
		  {
			return -1;
		  }
		  if (count == buffer.Length)
		  {
			// Grow
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] newBuffer = new char[org.apache.lucene.util.ArrayUtil.oversize(1+count, org.apache.lucene.util.RamUsageEstimator.NUM_BYTES_CHAR)];
			char[] newBuffer = new char[ArrayUtil.oversize(1 + count, RamUsageEstimator.NUM_BYTES_CHAR)];
			//System.out.println(Thread.currentThread().getName() + ": cb grow " + newBuffer.length);
			Array.Copy(buffer, nextWrite, newBuffer, 0, buffer.Length - nextWrite);
			Array.Copy(buffer, 0, newBuffer, buffer.Length - nextWrite, nextWrite);
			nextWrite = buffer.Length;
			buffer = newBuffer;
		  }
		  if (nextWrite == buffer.Length)
		  {
			nextWrite = 0;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int toRead = buffer.length - Math.max(count, nextWrite);
		  int toRead = buffer.Length - Math.Max(count, nextWrite);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int readCount = reader.read(buffer, nextWrite, toRead);
		  int readCount = reader.read(buffer, nextWrite, toRead);
		  if (readCount == -1)
		  {
			end = true;
			return -1;
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ch = buffer[nextWrite];
		  int ch = buffer[nextWrite];
		  nextWrite += readCount;
		  count += readCount;
		  nextPos += readCount;
		  return ch;
		}
		else
		{
		  // Cannot read from future (except by 1):
		  Debug.Assert(pos < nextPos);

		  // Cannot read from already freed past:
		  Debug.Assert(nextPos - pos <= count, "nextPos=" + nextPos + " pos=" + pos + " count=" + count);

		  return buffer[getIndex(pos)];
		}
	  }

	  // For assert:
	  private bool inBounds(int pos)
	  {
		return pos >= 0 && pos < nextPos && pos >= nextPos - count;
	  }

	  private int getIndex(int pos)
	  {
		int index = nextWrite - (nextPos - pos);
		if (index < 0)
		{
		  // Wrap:
		  index += buffer.Length;
		  Debug.Assert(index >= 0);
		}
		return index;
	  }

	  public char[] get(int posStart, int length)
	  {
		Debug.Assert(length > 0);
		Debug.Assert(inBounds(posStart), "posStart=" + posStart + " length=" + length);
		//System.out.println("    buffer.get posStart=" + posStart + " len=" + length);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startIndex = getIndex(posStart);
		int startIndex = getIndex(posStart);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endIndex = getIndex(posStart + length);
		int endIndex = getIndex(posStart + length);
		//System.out.println("      startIndex=" + startIndex + " endIndex=" + endIndex);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] result = new char[length];
		char[] result = new char[length];
		if (endIndex >= startIndex && length < buffer.Length)
		{
		  Array.Copy(buffer, startIndex, result, 0, endIndex - startIndex);
		}
		else
		{
		  // Wrapped:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int part1 = buffer.length-startIndex;
		  int part1 = buffer.Length - startIndex;
		  Array.Copy(buffer, startIndex, result, 0, part1);
		  Array.Copy(buffer, 0, result, buffer.Length - startIndex, length - part1);
		}
		return result;
	  }

	  /// <summary>
	  /// Call this to notify us that no chars before this
	  ///  absolute position are needed anymore. 
	  /// </summary>
	  public void freeBefore(int pos)
	  {
		Debug.Assert(pos >= 0);
		Debug.Assert(pos <= nextPos);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int newCount = nextPos - pos;
		int newCount = nextPos - pos;
		Debug.Assert(newCount <= count, "newCount=" + newCount + " count=" + count);
		Debug.Assert(newCount <= buffer.Length, "newCount=" + newCount + " buf.length=" + buffer.Length);
		count = newCount;
	  }
	}

}