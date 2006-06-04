/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Lucene.Net.Store
{
	
	/// <summary>Base implementation class for buffered {@link IndexInput}. </summary>
	public abstract class BufferedIndexInput : IndexInput, System.ICloneable
	{
		internal static readonly int BUFFER_SIZE;
		
		private byte[] buffer;
		
		private long bufferStart = 0; // position in file of buffer
		private int bufferLength = 0; // end of valid bytes
		private int bufferPosition = 0; // next byte to read
		
		public override byte ReadByte()
		{
			if (bufferPosition >= bufferLength)
				Refill();
			return buffer[bufferPosition++];
		}
		
		public override void  ReadBytes(byte[] b, int offset, int len)
		{
			if (len < BUFFER_SIZE)
			{
				for (int i = 0; i < len; i++)
				    // read byte-by-byte
					b[i + offset] = (byte) ReadByte();
			}
			else
			{
				// read all-at-once
				long start = GetFilePointer();
				SeekInternal(start);
				ReadInternal(b, offset, len);
				
				bufferStart = start + len; // adjust stream variables
				bufferPosition = 0;
				bufferLength = 0; // trigger refill() on read
			}
		}
		
		private void  Refill()
		{
			long start = bufferStart + bufferPosition;
			long end = start + BUFFER_SIZE;
			if (end > Length())
			    // don't read past EOF
				end = Length();
			bufferLength = (int) (end - start);
			if (bufferLength <= 0)
				throw new System.IO.IOException("read past EOF");
			
			if (buffer == null)
				buffer = new byte[BUFFER_SIZE]; // allocate buffer lazily
			ReadInternal(buffer, 0, bufferLength);
			
			bufferStart = start;
			bufferPosition = 0;
		}
		
		/// <summary>Expert: implements buffer refill.  Reads bytes from the current position
		/// in the input.
		/// </summary>
		/// <param name="b">the array to read bytes into
		/// </param>
		/// <param name="offset">the offset in the array to start storing bytes
		/// </param>
		/// <param name="length">the number of bytes to read
		/// </param>
		public abstract void  ReadInternal(byte[] b, int offset, int length);
		
		public override long GetFilePointer()
		{
			return bufferStart + bufferPosition;
		}
		
		public override void  Seek(long pos)
		{
			if (pos >= bufferStart && pos < (bufferStart + bufferLength))
				bufferPosition = (int) (pos - bufferStart);
			    // seek within buffer
			else
			{
				bufferStart = pos;
				bufferPosition = 0;
				bufferLength = 0; // trigger refill() on read()
				SeekInternal(pos);
			}
		}
		
		/// <summary>Expert: implements seek.  Sets current position in this file, where the
		/// next {@link #ReadInternal(byte[],int,int)} will occur.
		/// </summary>
		/// <seealso cref="ReadInternal(byte[],int,int)">
		/// </seealso>
		public abstract void  SeekInternal(long pos);
		
		public override System.Object Clone()
		{
			BufferedIndexInput clone = (BufferedIndexInput) base.Clone();
			
			if (buffer != null)
			{
				clone.buffer = new byte[BUFFER_SIZE];
				Array.Copy(buffer, 0, clone.buffer, 0, bufferLength);
			}
			
			return clone;
		}

        static BufferedIndexInput()
		{
			BUFFER_SIZE = BufferedIndexOutput.BUFFER_SIZE;
		}
	}
}