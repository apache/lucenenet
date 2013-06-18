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
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Store
{
	
	[Serializable]
	public class RAMFile
	{
		protected List<byte[]> buffers = new List<byte[]>();
		internal long length;
		internal RAMDirectory directory;
		internal long sizeInBytes; 
				
		// File used as buffer, in no RAMDirectory
		public RAMFile()
		{
		}
		
		internal RAMFile(RAMDirectory directory)
		{
			this.directory = directory;
		}
		
		// For non-stream access from thread that might be concurrent with writing

	    public virtual long Length
	    {
	        get
	        {
	            lock (this)
	            {
	                return length;
	            }
	        }
	        set
	        {
	            lock (this)
	            {
	                this.length = value;
	            }
	        }
	    }

	    protected internal byte[] AddBuffer(int size)
		{
            byte[] buffer = NewBuffer(size);
            lock (this)
            {
                buffers.Add(buffer);
                sizeInBytes += size;
            }

            if (directory != null)
            {
                Interlocked.Add(ref directory.sizeInBytes, size);
            }

            return buffer;
		}
		
		public byte[] GetBuffer(int index)
		{
			lock (this)
			{
				return buffers[index];
			}
		}
		
		public int NumBuffers
		{
            get
            {
                lock (this)
                {
                    return buffers.Count;
                }
            }
		}
		
		/// <summary> Expert: allocate a new buffer. 
		/// Subclasses can allocate differently. 
		/// </summary>
		/// <param name="size">size of allocated buffer.
		/// </param>
		/// <returns> allocated buffer.
		/// </returns>
		protected virtual byte[] NewBuffer(int size)
		{
			return new byte[size];
		}

	    public virtual long SizeInBytes
	    {
	        get
	        {
	            lock (this)
	            {
	                return sizeInBytes;
	            }
	        }
	    }
	}
}