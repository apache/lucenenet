/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
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
	
	/// <summary>File-based {@link Directory} implementation that uses mmap for input.
	/// 
	/// <p>To use this, invoke Java with the System property
	/// Lucene.Net.FSDirectory.class set to
	/// Lucene.Net.Store.MMapDirectory.  This will cause {@link
	/// FSDirectory#GetDirectory(File,boolean)} to return instances of this class.
	/// </summary>
	public class MMapDirectory : FSDirectory
	{
		
		private class MMapIndexInput : IndexInput, System.ICloneable
		{
			
			private System.IO.MemoryStream buffer;
			private long length;
			
			internal MMapIndexInput(System.IO.FileStream raf)
			{
                byte[] data = new byte[raf.Length];
                raf.Read(data, 0, (int) raf.Length);
				this.length = raf.Length;
                this.buffer = new System.IO.MemoryStream(data);  // this.buffer = raf.getChannel().map(MapMode.READ_ONLY, 0, length);    // {{Aroush-1.9}}
			}
			
			public override byte ReadByte()
			{
                return (byte) buffer.ReadByte();
			}
			
			public override void  ReadBytes(byte[] b, int offset, int len)
			{
                buffer.Read(b, offset, len);
			}
			
			public override long GetFilePointer()
			{
				return buffer.Position;
			}
			
			public override void  Seek(long pos)
			{
				buffer.Seek(pos, System.IO.SeekOrigin.Begin);
			}
			
			public override long Length()
			{
				return length;
			}
			
			public override System.Object Clone()
			{
				MMapIndexInput clone = (MMapIndexInput) base.Clone();
				// clone.buffer = buffer.duplicate();   // {{Aroush-1.9}}
				return clone;
			}
			
			public override void  Close()
			{
			}
		}
		
		private class MultiMMapIndexInput : IndexInput, System.ICloneable
		{
			
			private System.IO.MemoryStream[] buffers;
			private int[] bufSizes; // keep here, ByteBuffer.size() method is optional
			
			private long length;
			
			private int curBufIndex;
			private int maxBufSize;
			
			private System.IO.MemoryStream curBuf;    // redundant for speed: buffers[curBufIndex]
			private int curAvail; // redundant for speed: (bufSizes[curBufIndex] - curBuf.position())
			
			
			public MultiMMapIndexInput(System.IO.FileStream raf, int maxBufSize)
			{
				this.length = raf.Length;
				this.maxBufSize = maxBufSize;
				
				if (maxBufSize <= 0)
					throw new System.ArgumentException("Non positive maxBufSize: " + maxBufSize);
				
				if ((length / maxBufSize) > System.Int32.MaxValue)
				{
					throw new System.ArgumentException("RandomAccessFile too big for maximum buffer size: " + raf.ToString());
				}
				
				int nrBuffers = (int) (length / maxBufSize);
				if ((nrBuffers * maxBufSize) < length)
					nrBuffers++;
				
				this.buffers = new System.IO.MemoryStream[nrBuffers];   // {{Aroush-1.9}}
				this.bufSizes = new int[nrBuffers];
				
				long bufferStart = 0;
				System.IO.FileStream rafc = raf;
				for (int bufNr = 0; bufNr < nrBuffers; bufNr++)
				{
                    byte[] data = new byte[rafc.Length];
                    raf.Read(data, 0, (int) rafc.Length);

					int bufSize = (length > (bufferStart + maxBufSize)) ? maxBufSize : (int) (length - bufferStart);
					this.buffers[bufNr] = new System.IO.MemoryStream(data);     // rafc.map(MapMode.READ_ONLY, bufferStart, bufSize);     // {{Aroush-1.9}}
					this.bufSizes[bufNr] = bufSize;
					bufferStart += bufSize;
				}
				Seek(0L);
			}
			
			public override byte ReadByte()
			{
				// Performance might be improved by reading ahead into an array of
				// eg. 128 bytes and readByte() from there.
				if (curAvail == 0)
				{
					curBufIndex++;
					curBuf = buffers[curBufIndex]; // index out of bounds when too many bytes requested
					curBuf.Seek(0, System.IO.SeekOrigin.Begin);
					curAvail = bufSizes[curBufIndex];
				}
				curAvail--;
				return (byte) curBuf.ReadByte();
			}
			
			public override void  ReadBytes(byte[] b, int offset, int len)
			{
				while (len > curAvail)
				{
					curBuf.Read(b, offset, curAvail);
					len -= curAvail;
					offset += curAvail;
					curBufIndex++;
					curBuf = buffers[curBufIndex]; // index out of bounds when too many bytes requested
					curBuf.Seek(0, System.IO.SeekOrigin.Begin);
					curAvail = bufSizes[curBufIndex];
				}
				curBuf.Read(b, offset, len);
				curAvail -= len;
			}
			
			public override long GetFilePointer()
			{
				return (curBufIndex * (long) maxBufSize) + curBuf.Position;
			}
			
			public override void  Seek(long pos)
			{
				curBufIndex = (int) (pos / maxBufSize);
				curBuf = buffers[curBufIndex];
				int bufOffset = (int) (pos - (curBufIndex * maxBufSize));
				curBuf.Seek(bufOffset, System.IO.SeekOrigin.Begin);
				curAvail = bufSizes[curBufIndex] - bufOffset;
			}
			
			public override long Length()
			{
				return length;
			}
			
			public override System.Object Clone()
			{
				MultiMMapIndexInput clone = (MultiMMapIndexInput) base.Clone();
				clone.buffers = new System.IO.MemoryStream[buffers.Length];
				// No need to clone bufSizes.
				// Since most clones will use only one buffer, duplicate() could also be
				// done lazy in clones, eg. when adapting curBuf.
				for (int bufNr = 0; bufNr < buffers.Length; bufNr++)
				{
					clone.buffers[bufNr] = buffers[bufNr];    // clone.buffers[bufNr] = buffers[bufNr].duplicate();   // {{Aroush-1.9}} how do we clone?!
				}
				try
				{
					clone.Seek(GetFilePointer());
				}
				catch (System.IO.IOException ioe)
				{
					System.Exception newException = new System.Exception("", ioe);  // {{Aroush-2.0}} This should be SystemException
					throw newException;
				}
				return clone;
			}
			
			public override void  Close()
			{
			}
		}
		
		private int MAX_BBUF = System.Int32.MaxValue;
		
		public override IndexInput OpenInput(System.String name)
		{
			System.IO.FileInfo f = new System.IO.FileInfo(System.IO.Path.Combine(GetFile().FullName, name));
			System.IO.FileStream raf = new System.IO.FileStream(f.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read); 
			try
			{
				return (raf.Length <= MAX_BBUF) ? (IndexInput) new MMapIndexInput(raf) : (IndexInput) new MultiMMapIndexInput(raf, MAX_BBUF);
			}
			finally
			{
				raf.Close();
			}
		}
		
		public override IndexInput OpenInput(System.String name, int bufferSize)
		{
			return OpenInput(name);
		}
	}
}