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
	
	/// <summary> A memory-resident {@link IndexInput} implementation.
	/// 
	/// </summary>
	/// <version>  $Id: RAMInputStream.java 598693 2007-11-27 17:01:21Z mikemccand $
	/// </version>
	
	public class RAMInputStream : IndexInput, System.ICloneable
	{
		internal static readonly int BUFFER_SIZE;
		
		private RAMFile file;
		private long length;
		
		private byte[] currentBuffer;
		private int currentBufferIndex;
		
		private int bufferPosition;
		private long bufferStart;
		private int bufferLength;

        // for testing
        public static RAMInputStream RAMInputStream_ForNUnitTest(RAMFile f)
        {
            return new RAMInputStream(f);
        }
		
		public /*internal*/ RAMInputStream(RAMFile f)
		{
			file = f;
			length = file.length;
			if (length / BUFFER_SIZE >= System.Int32.MaxValue)
			{
				throw new System.IO.IOException("Too large RAMFile! " + length);
			}
			
			// make sure that we switch to the
			// first needed buffer lazily
			currentBufferIndex = - 1;
			currentBuffer = null;
		}
		
		public override void  Close()
		{
			// nothing to do here
		}
		
		public override long Length()
		{
			return length;
		}
		
		public override byte ReadByte()
		{
			if (bufferPosition >= bufferLength)
			{
				currentBufferIndex++;
				SwitchCurrentBuffer();
			}
			return currentBuffer[bufferPosition++];
		}
		
		public override void  ReadBytes(byte[] b, int offset, int len)
		{
			while (len > 0)
			{
				if (bufferPosition >= bufferLength)
				{
					currentBufferIndex++;
					SwitchCurrentBuffer();
				}
				
				int remainInBuffer = bufferLength - bufferPosition;
				int bytesToCopy = len < remainInBuffer ? len : remainInBuffer;
				Array.Copy(currentBuffer, bufferPosition, b, offset, bytesToCopy);
				offset += bytesToCopy;
				len -= bytesToCopy;
				bufferPosition += bytesToCopy;
			}
		}
		
		private void  SwitchCurrentBuffer()
		{
			if (currentBufferIndex >= file.NumBuffers())
			{
				// end of file reached, no more buffers left
				throw new System.IO.IOException("Read past EOF");
			}
			else
			{
				currentBuffer = file.GetBuffer(currentBufferIndex);
				bufferPosition = 0;
				bufferStart = (long) BUFFER_SIZE * (long) currentBufferIndex;
				long buflen = length - bufferStart;
				bufferLength = buflen > BUFFER_SIZE ? BUFFER_SIZE : (int) buflen;
			}
		}
		
		public override long GetFilePointer()
		{
			return currentBufferIndex < 0 ? 0 : bufferStart + bufferPosition;
		}
		
		public override void  Seek(long pos)
		{
			if (currentBuffer == null || pos < bufferStart || pos >= bufferStart + BUFFER_SIZE)
			{
				currentBufferIndex = (int) (pos / BUFFER_SIZE);
				SwitchCurrentBuffer();
			}
			bufferPosition = (int) (pos % BUFFER_SIZE);
		}

        // {{Aroush-1.9}} Do we need this Clone()?!
        /* override public System.Object Clone()
        {
            return null;
        }
        */

		static RAMInputStream()
		{
			BUFFER_SIZE = RAMOutputStream.BUFFER_SIZE;
		}
	}
}