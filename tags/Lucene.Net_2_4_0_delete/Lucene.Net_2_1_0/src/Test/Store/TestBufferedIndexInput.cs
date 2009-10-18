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

using NUnit.Framework;

namespace Lucene.Net.Store
{
	[TestFixture]
	public class TestBufferedIndexInput
	{
		// Call readByte() repeatedly, past the buffer boundary, and see that it
		// is working as expected.
		// Our input comes from a dynamically generated/ "file" - see
		// MyBufferedIndexInput below.
        [Test]
		public virtual void  TestReadByte()
		{
			MyBufferedIndexInput input = new MyBufferedIndexInput();
			for (int i = 0; i < BufferedIndexInput.BUFFER_SIZE_ForNUnitTest * 10; i++)
			{
				Assert.AreEqual(input.ReadByte(), Byten(i));
			}
		}
		
		// Call readBytes() repeatedly, with various chunk sizes (from 1 byte to
		// larger than the buffer size), and see that it returns the bytes we expect.
		// Our input comes from a dynamically generated "file" -
		// see MyBufferedIndexInput below.
        [Test]
		public virtual void  TestReadBytes()
		{
			MyBufferedIndexInput input = new MyBufferedIndexInput();
			int pos = 0;
			// gradually increasing size:
			for (int size = 1; size < BufferedIndexInput.BUFFER_SIZE_ForNUnitTest * 10; size = size + size / 200 + 1)
			{
				CheckReadBytes(input, size, pos);
				pos += size;
			}
			// wildly fluctuating size:
			for (long i = 0; i < 1000; i++)
			{
				// The following function generates a fluctuating (but repeatable)
				// size, sometimes small (<100) but sometimes large (>10000)
				int size1 = (int) (i % 7 + 7 * (i % 5) + 7 * 5 * (i % 3) + 5 * 5 * 3 * (i % 2));
				int size2 = (int) (i % 11 + 11 * (i % 7) + 11 * 7 * (i % 5) + 11 * 7 * 5 * (i % 3) + 11 * 7 * 5 * 3 * (i % 2));
				int size = (i % 3 == 0)?size2 * 10:size1;
				CheckReadBytes(input, size, pos);
				pos += size;
			}
			// constant small size (7 bytes):
			for (int i = 0; i < BufferedIndexInput.BUFFER_SIZE_ForNUnitTest; i++)
			{
				CheckReadBytes(input, 7, pos);
				pos += 7;
			}
		}
		private void  CheckReadBytes(BufferedIndexInput input, int size, int pos)
		{
			// Just to see that "offset" is treated properly in readBytes(), we
			// add an arbitrary offset at the beginning of the array
			int offset = size % 10; // arbitrary
			byte[] b = new byte[offset + size];
			input.ReadBytes(b, offset, size);
			for (int i = 0; i < size; i++)
			{
				Assert.AreEqual(b[offset + i], Byten(pos + i));
			}
		}
		
		// This tests that attempts to readBytes() past an EOF will fail, while
		// reads up to the EOF will succeed. The EOF is determined by the
		// BufferedIndexInput's arbitrary length() value.
        [Test]
		public virtual void  TestEOF()
		{
			MyBufferedIndexInput input = new MyBufferedIndexInput(1024);
			// see that we can read all the bytes at one go:
			CheckReadBytes(input, (int) input.Length(), 0);
			// go back and see that we can't read more than that, for small and
			// large overflows:
			int pos = (int) input.Length() - 10;
			input.Seek(pos);
			CheckReadBytes(input, 10, pos);
			input.Seek(pos);
			try
			{
				CheckReadBytes(input, 11, pos);
				Assert.Fail("Block read past end of file");
			}
			catch (System.IO.IOException e)
			{
				/* success */
			}
			input.Seek(pos);
			try
			{
				CheckReadBytes(input, 50, pos);
				Assert.Fail("Block read past end of file");
			}
			catch (System.IO.IOException e)
			{
				/* success */
			}
			input.Seek(pos);
			try
			{
				CheckReadBytes(input, 100000, pos);
				Assert.Fail("Block read past end of file");
			}
			catch (System.IO.IOException e)
			{
				/* success */
			}
		}
		
		// byten emulates a file - Byten(n) returns the n'th byte in that file.
		// MyBufferedIndexInput reads this "file".
		private static byte Byten(long n)
		{
			return (byte) (n * n % 256);
		}

		private class MyBufferedIndexInput : BufferedIndexInput
		{
			private long pos;
			private long len;
			public MyBufferedIndexInput(long len)
			{
				this.len = len;
				this.pos = 0;
			}
			public MyBufferedIndexInput() : this(System.Int64.MaxValue)
			{
			}

            public override void  ReadInternal(byte[] b, int offset, int length)
			{
				for (int i = offset; i < offset + length; i++)
					b[i] = Lucene.Net.Store.TestBufferedIndexInput.Byten(pos++);
			}
			
			public override void  SeekInternal(long pos)
			{
				this.pos = pos;
			}
			
			public override void  Close()
			{
			}
			
			public override long Length()
			{
				return len;
			}
		}
	}
}