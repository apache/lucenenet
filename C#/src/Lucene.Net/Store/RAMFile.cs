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
	
	[Serializable]
	public class RAMFile
	{
		
		private const long serialVersionUID = 1L;
		
		private System.Collections.ArrayList buffers = new System.Collections.ArrayList();
		internal long length;
		internal RAMDirectory directory;
		internal long sizeInBytes; // Only maintained if in a directory; updates synchronized on directory
		
		// This is publicly modifiable via Directory.touchFile(), so direct access not supported
		private long lastModified = System.DateTime.Now.Ticks;
		
		// File used as buffer, in no RAMDirectory
		protected internal RAMFile()
		{
		}
		
		public /*internal*/ RAMFile(RAMDirectory directory)
		{
			this.directory = directory;
		}
		
		// For non-stream access from thread that might be concurrent with writing
		internal virtual long GetLength()
		{
			lock (this)
			{
				return length;
			}
		}
		
		internal virtual void  SetLength(long length)
		{
			lock (this)
			{
				this.length = length;
			}
		}
		
		// For non-stream access from thread that might be concurrent with writing
		internal virtual long GetLastModified()
		{
			lock (this)
			{
				return lastModified;
			}
		}
		
		internal virtual void  SetLastModified(long lastModified)
		{
			lock (this)
			{
				this.lastModified = lastModified;
			}
		}
		
		internal byte[] AddBuffer(int size)
		{
			lock (this)
			{
				byte[] buffer = new byte[size];
				if (directory != null)
					lock (directory)
					{
						// Ensure addition of buffer and adjustment to directory size are atomic wrt directory
						buffers.Add(buffer);
						directory.sizeInBytes += size;
						sizeInBytes += size;
					}
				else
					buffers.Add(buffer);
				return buffer;
			}
		}
		
		internal byte[] GetBuffer(int index)
		{
			lock (this)
			{
				return (byte[]) buffers[index];
			}
		}
		
		internal int NumBuffers()
		{
			lock (this)
			{
				return buffers.Count;
			}
		}
		
		/// <summary> Expert: allocate a new buffer. 
		/// Subclasses can allocate differently. 
		/// </summary>
		/// <param name="size">size of allocated buffer.
		/// </param>
		/// <returns> allocated buffer.
		/// </returns>
		protected internal virtual byte[] NewBuffer(int size)
		{
			return new byte[size];
		}
		
		// Only valid if in a directory
		internal virtual long GetSizeInBytes()
		{
			lock (directory)
			{
				return sizeInBytes;
			}
		}

        public long sizeInBytes_ForNUnitTest
        {
            get { return sizeInBytes; }
        }

        public RAMDirectory directory_ForNUnitTest
        {
            set { directory = value; }
        }

        public long length_ForNUnitTest
        {
            get { return length; }
        }

        public long GetSizeInBytes_ForNUnitTest()
        {
            return GetSizeInBytes();
        }
    }
}