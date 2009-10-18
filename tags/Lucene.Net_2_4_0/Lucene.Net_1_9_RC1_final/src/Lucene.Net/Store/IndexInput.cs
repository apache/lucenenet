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
	
	/// <summary>Abstract base class for input from a file in a {@link Directory}.  A
	/// random-access input stream.  Used for all Lucene index input operations.
	/// </summary>
	/// <seealso cref="Directory">
	/// </seealso>
	public abstract class IndexInput : System.ICloneable
	{
		private char[] chars; // used by readString()
		
		/// <summary>Reads and returns a single byte.</summary>
		/// <seealso cref="IndexOutput.WriteByte(byte)">
		/// </seealso>
		public abstract byte ReadByte();
		
		/// <summary>Reads a specified number of bytes into an array at the specified offset.</summary>
		/// <param name="b">the array to read bytes into
		/// </param>
		/// <param name="offset">the offset in the array to start storing bytes
		/// </param>
		/// <param name="len">the number of bytes to read
		/// </param>
		/// <seealso cref="IndexOutput.WriteBytes(byte[],int)">
		/// </seealso>
		public abstract void  ReadBytes(byte[] b, int offset, int len);
		
		/// <summary>Reads four bytes and returns an int.</summary>
		/// <seealso cref="IndexOutput.WriteInt(int)">
		/// </seealso>
		public virtual int ReadInt()
		{
			return ((ReadByte() & 0xFF) << 24) | ((ReadByte() & 0xFF) << 16) | ((ReadByte() & 0xFF) << 8) | (ReadByte() & 0xFF);
		}
		
		/// <summary>Reads an int stored in variable-length format.  Reads between one and
		/// five bytes.  Smaller values take fewer bytes.  Negative numbers are not
		/// supported.
		/// </summary>
		/// <seealso cref="IndexOutput.WriteVInt(int)">
		/// </seealso>
		public virtual int ReadVInt()
		{
			byte b = ReadByte();
			int i = b & 0x7F;
			for (int shift = 7; (b & 0x80) != 0; shift += 7)
			{
				b = ReadByte();
				i |= (b & 0x7F) << shift;
			}
			return i;
		}
		
		/// <summary>Reads eight bytes and returns a long.</summary>
		/// <seealso cref="IndexOutput.WriteLong(long)">
		/// </seealso>
		public virtual long ReadLong()
		{
			return (((long) ReadInt()) << 32) | (ReadInt() & 0xFFFFFFFFL);
		}
		
		/// <summary>Reads a long stored in variable-length format.  Reads between one and
		/// nine bytes.  Smaller values take fewer bytes.  Negative numbers are not
		/// supported. 
		/// </summary>
		public virtual long ReadVLong()
		{
			byte b = ReadByte();
			long i = b & 0x7F;
			for (int shift = 7; (b & 0x80) != 0; shift += 7)
			{
				b = ReadByte();
				i |= (b & 0x7FL) << shift;
			}
			return i;
		}
		
		/// <summary>Reads a string.</summary>
		/// <seealso cref="IndexOutput.WriteString(String)">
		/// </seealso>
		public virtual System.String ReadString()
		{
			int length = ReadVInt();
			if (chars == null || length > chars.Length)
				chars = new char[length];
			ReadChars(chars, 0, length);
			return new System.String(chars, 0, length);
		}
		
		/// <summary>Reads UTF-8 encoded characters into an array.</summary>
		/// <param name="buffer">the array to read characters into
		/// </param>
		/// <param name="start">the offset in the array to start storing characters
		/// </param>
		/// <param name="length">the number of characters to read
		/// </param>
		/// <seealso cref="IndexOutput.WriteChars(String,int,int)">
		/// </seealso>
		public virtual void  ReadChars(char[] buffer, int start, int length)
		{
			int end = start + length;
			for (int i = start; i < end; i++)
			{
				byte b = ReadByte();
				if ((b & 0x80) == 0)
					buffer[i] = (char) (b & 0x7F);
				else if ((b & 0xE0) != 0xE0)
				{
					buffer[i] = (char) (((b & 0x1F) << 6) | (ReadByte() & 0x3F));
				}
				else
					buffer[i] = (char) (((b & 0x0F) << 12) | ((ReadByte() & 0x3F) << 6) | (ReadByte() & 0x3F));
			}
		}
		
		/// <summary>Closes the stream to futher operations. </summary>
		public abstract void  Close();
		
		/// <summary>Returns the current position in this file, where the next read will
		/// occur.
		/// </summary>
		/// <seealso cref="Seek(long)">
		/// </seealso>
		public abstract long GetFilePointer();
		
		/// <summary>Sets current position in this file, where the next read will occur.</summary>
		/// <seealso cref="GetFilePointer()">
		/// </seealso>
		public abstract void  Seek(long pos);
		
		/// <summary>The number of bytes in the file. </summary>
		public abstract long Length();
		
		/// <summary>Returns a clone of this stream.
		/// 
		/// <p>Clones of a stream access the same data, and are positioned at the same
		/// point as the stream they were cloned from.
		/// 
		/// <p>Expert: Subclasses must ensure that clones may be positioned at
		/// different points in the input from each other and from the stream they
		/// were cloned from.
		/// </summary>
		public virtual System.Object Clone()
		{
			IndexInput clone = null;
			try
			{
				clone = (IndexInput) base.MemberwiseClone();
			}
			catch (System.Exception)
			{
			}
			
			clone.chars = null;
			
			return clone;
		}
	}
}