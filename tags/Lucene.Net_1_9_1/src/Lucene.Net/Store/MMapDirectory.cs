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
// using ByteBuffer = java.nio.ByteBuffer;                  // {{Aroush-1.9}}
// using FileChannel = java.nio.channels.FileChannel;       // {{Aroush-1.9}}
// using MapMode = java.nio.channels.FileChannel.MapMode;   // {{Aroush-1.9}}

namespace Lucene.Net.Store
{
	
	/// <summary>File-based {@link Directory} implementation that uses mmap for input.
	/// 
	/// <p>To use this, invoke Java with the System property
	/// Lucene.Net.FSDirectory.class set to
	/// Lucene.Net.store.MMapDirectory.  This will cause {@link
	/// FSDirectory#GetDirectory(File,boolean)} to return instances of this class.
	/// </summary>
	public class MMapDirectory : FSDirectory
	{
		
		private class MMapIndexInput : IndexInput, System.ICloneable
		{
			
			private System.IO.FileStream buffer;    // private ByteBuffer buffer;   // {{Aroush-1.9}}
			private long length;
			
			internal MMapIndexInput(System.IO.FileStream raf)
			{
				this.length = raf.Length;
				// this.buffer = raf.getChannel().map(MapMode.READ_ONLY, 0, length);    // {{Aroush-1.9}}
			}
			
			public override byte ReadByte()
			{
				return 0;   // return buffer.get_Renamed(); // {{Aroush-1.9}}
			}
			
			public override void  ReadBytes(byte[] b, int offset, int len)
			{
				// buffer.get_Renamed(b, offset, len);  // {{Aroush-1.9}}
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
		
		/* Added class MultiMMapIndexInput, Paul Elschot.
		* Slightly adapted constructor of MMapIndexInput.
		* Licensed under the Apache License, Version 2.0.
		*/
		private class MultiMMapIndexInput:IndexInput, System.ICloneable
		{
			
			private System.IO.FileStream[] buffers; // private ByteBuffer[] buffers;    // {{Aroush-1.9}}
			private int[] bufSizes; // keep here, ByteBuffer.size() method is optional
			
			private long length;
			
			private int curBufIndex;
			private int maxBufSize;
			
			private System.IO.FileStream curBuf;    // private ByteBuffer curBuf; // {{Aroush-1.9}}    // redundant for speed: buffers[curBufIndex]
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
				
				this.buffers = new System.IO.FileStream[nrBuffers]; // this.buffers = new ByteBuffer[nrBuffers];   // {{Aroush-1.9}}
				this.bufSizes = new int[nrBuffers];
				
				long bufferStart = 0;
				System.IO.FileStream rafc = null;   // FileChannel rafc = raf.getChannel();    // {{Aroush-1.9}}
				for (int bufNr = 0; bufNr < nrBuffers; bufNr++)
				{
					int bufSize = (length > (bufferStart + maxBufSize))?maxBufSize:(int) (length - bufferStart);
					// this.buffers[bufNr] = rafc.map(MapMode.READ_ONLY, bufferStart, bufSize);    // {{Aroush-1.9}}
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
				return 0;   // return curBuf.get_Renamed();     // {{Aroush-1.9}}
			}
			
			public override void  ReadBytes(byte[] b, int offset, int len)
			{
				while (len > curAvail)
				{
					// curBuf.get_Renamed(b, offset, curAvail);    // {{Aroush-1.9}}
					len -= curAvail;
					offset += curAvail;
					curBufIndex++;
					curBuf = buffers[curBufIndex]; // index out of bounds when too many bytes requested
					curBuf.Seek(0, System.IO.SeekOrigin.Begin);
					curAvail = bufSizes[curBufIndex];
				}
				// curBuf.get_Renamed(b, offset, len); // {{Aroush-1.9}}
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
				// clone.buffers = new ByteBuffer[buffers.length];  // {{Aroush-1.9}}
				// No need to clone bufSizes.
				// Since most clones will use only one buffer, duplicate() could also be
				// done lazy in clones, eg. when adapting curBuf.
				for (int bufNr = 0; bufNr < buffers.Length; bufNr++)
				{
					// clone.buffers[bufNr] = buffers[bufNr].duplicate();   // {{Aroush-1.9}}
				}
				try
				{
					clone.Seek(GetFilePointer());
				}
				catch (System.IO.IOException ioe)
				{
					throw new System.Exception(ioe.ToString()); // {{Aroush-1.9}} should be re-thrown as RuntimeException
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
	}
}