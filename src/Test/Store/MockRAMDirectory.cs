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
	
	/// <summary> This is a subclass of RAMDirectory that adds methods
	/// intented to be used only by unit tests.
	/// </summary>
	/// <version>  $Id: RAMDirectory.java 437897 2006-08-29 01:13:10Z yonik $
	/// </version>
	
	[Serializable]
	public class MockRAMDirectory : RAMDirectory
	{
        internal long maxSize;
		
		// Max actual bytes used. This is set by MockRAMOutputStream:
		internal long maxUsedSize;
		internal double randomIOExceptionRate;
		internal System.Random randomState;
		
		public MockRAMDirectory() : base()
		{
		}
		public MockRAMDirectory(System.String dir) : base(dir)
		{
		}
		public MockRAMDirectory(Directory dir) : base(dir)
		{
		}
		public MockRAMDirectory(System.IO.FileInfo dir) : base(dir)
		{
		}

        virtual public long GetMaxSizeInBytes()
        {
            return this.maxSize;
        }

        virtual public void SetMaxSizeInBytes(long maxSize)
        {
            this.maxSize = maxSize;
        }

        /// <summary> Returns the peek actual storage used (bytes) in this
        /// directory.
        /// </summary>
        virtual public long GetMaxUsedSizeInBytes()
        {
            return this.maxUsedSize;
        }

        public virtual void  ResetMaxUsedSizeInBytes()
		{
			this.maxUsedSize = GetRecomputedActualSizeInBytes();
		}
		
		/// <summary> If 0.0, no exceptions will be thrown.  Else this should
		/// be a double 0.0 - 1.0.  We will randomly throw an
		/// IOException on the first write to an OutputStream based
		/// on this probability.
		/// </summary>
		public virtual void  SetRandomIOExceptionRate(double rate, long seed)
		{
			randomIOExceptionRate = rate;
			// seed so we have deterministic behaviour:
			randomState = new System.Random((System.Int32) seed);
		}

		public virtual double GetRandomIOExceptionRate()
		{
			return randomIOExceptionRate;
		}
		
		internal virtual void  MaybeThrowIOException()
		{
            if (randomIOExceptionRate > 0.0)
            {
                int number = System.Math.Abs(randomState.Next() % 1000);
                if (number < randomIOExceptionRate * 1000)
                {
                    throw new System.IO.IOException("a random IOException");
                }
            }
        }
		
		public override IndexOutput CreateOutput(System.String name)
		{
			RAMFile file = new RAMFile(this);
			lock (this)
			{
				RAMFile existing = (RAMFile) fileMap_ForNUnitTest[name];
				if (existing != null)
				{
					sizeInBytes_ForNUnitTest -= existing.sizeInBytes_ForNUnitTest;
					existing.directory_ForNUnitTest = null;
				}
				fileMap_ForNUnitTest[name] = file;
			}
			
			return new MockRAMOutputStream(this, file);
		}

        /// <summary>Provided for testing purposes.  Use sizeInBytes() instead. </summary>
        virtual internal long GetRecomputedSizeInBytes()
        {
            lock (this)
            {
                long size = 0;
                System.Collections.IEnumerator it = fileMap_ForNUnitTest.Values.GetEnumerator();
                while (it.MoveNext())
                {
                    size += ((RAMFile) it.Current).GetSizeInBytes_ForNUnitTest();
                }
                return size;
            }
        }

        /// <summary>Like getRecomputedSizeInBytes(), but, uses actual file
        /// lengths rather than buffer allocations (which are
        /// quantized up to nearest
        /// BufferedIndexOutput.BUFFER_SIZE (now 1024) bytes.
        /// </summary>
        virtual internal long GetRecomputedActualSizeInBytes()
        {
            long size = 0;
            System.Collections.IEnumerator it = fileMap_ForNUnitTest.Values.GetEnumerator();
            while (it.MoveNext())
            {
                size += ((RAMFile) it.Current).length_ForNUnitTest;
            }
            return size;
        }
    }
}