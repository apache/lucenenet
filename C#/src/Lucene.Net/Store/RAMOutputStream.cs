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
	
	/// <summary> A memory-resident {@link IndexOutput} implementation.
	/// 
	/// </summary>
	/// <version>  $Id: RAMOutputStream.java 488330 2006-12-18 16:45:29Z mikemccand $
	/// </version>
	
	public class RAMOutputStream : BufferedIndexOutput
	{
		private RAMFile file;
		private long pointer = 0;
		
		/// <summary>Construct an empty output buffer. </summary>
		public RAMOutputStream() : this(new RAMFile())
		{
		}
		
		internal RAMOutputStream(RAMFile f)
		{
			file = f;
		}
		
		/// <summary>Copy the current contents of this buffer to the named output. </summary>
		public virtual void  WriteTo(IndexOutput out_Renamed)
		{
			Flush();
			long end = file.length;
			long pos = 0;
			int buffer = 0;
			while (pos < end)
			{
				int length = BUFFER_SIZE;
				long nextPos = pos + length;
				if (nextPos > end)
				{
					// at the last buffer
					length = (int) (end - pos);
				}
				out_Renamed.WriteBytes((byte[]) file.buffers[buffer++], length);
				pos = nextPos;
			}
		}
		
		/// <summary>Resets this to an empty buffer. </summary>
		public virtual void  Reset()
		{
			try
			{
				Seek(0);
			}
			catch (System.IO.IOException e)
			{
				// should never happen
				throw new System.SystemException(e.ToString());
			}
			
			file.SetLength(0);
		}
		
		public override void  FlushBuffer(byte[] src, int len)
		{
			byte[] buffer;
			int bufferPos = 0;
			while (bufferPos != len)
			{
				int bufferNumber = (int) (pointer / BUFFER_SIZE);
				int bufferOffset = (int) (pointer % BUFFER_SIZE);
				int bytesInBuffer = BUFFER_SIZE - bufferOffset;
				int remainInSrcBuffer = len - bufferPos;
				int bytesToCopy = bytesInBuffer >= remainInSrcBuffer ? remainInSrcBuffer : bytesInBuffer;
				
				if (bufferNumber == file.buffers.Count)
					buffer = file.AddBuffer(BUFFER_SIZE);
				else
					buffer = (byte[]) file.buffers[bufferNumber];
				
				Array.Copy(src, bufferPos, buffer, bufferOffset, bytesToCopy);
				bufferPos += bytesToCopy;
				pointer += bytesToCopy;
			}
			
			if (pointer > file.length)
				file.SetLength(pointer);
			
			file.SetLastModified(System.DateTime.Now.Ticks);
		}
		
		public override void  Close()
		{
			base.Close();
		}
		
		public override void  Seek(long pos)
		{
			base.Seek(pos);
			pointer = pos;
		}
		public override long Length()
		{
			return file.length;
		}
	}
}