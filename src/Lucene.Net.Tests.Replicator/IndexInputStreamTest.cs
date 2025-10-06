using Lucene.Net.Attributes;
using Lucene.Net.Index;
using Lucene.Net.Replicator;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Tests.Replicator
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    //Note: LUCENENET specific
    [LuceneNetSpecific]
    public class IndexInputStreamTest : LuceneTestCase
    {

        [Test]
        [LuceneNetSpecific]
        public void Read_RemainingIndexInputLargerThanReadCount_ReturnsReadCount()
        {
            byte[] buffer = new byte[8.KiloBytes()];
            Random.NextBytes(buffer);
            IndexInputStream stream = new IndexInputStream(new MockIndexInput(buffer));

            int readBytes = 2.KiloBytes();
            byte[] readBuffer = new byte[readBytes];
            Assert.AreEqual(stream.Read(readBuffer, 0, readBytes), readBytes);
        }

        [Test]
        [LuceneNetSpecific]
        public void Read_RemainingIndexInputLargerThanReadCount_ReturnsExpectedSection([Range(1, 8)] int section)
        {
            byte[] buffer = new byte[8.KiloBytes()];
            Random.NextBytes(buffer);
            IndexInputStream stream = new IndexInputStream(new MockIndexInput(buffer));

            int readBytes = 1.KiloBytes();
            byte[] readBuffer = new byte[readBytes];
            for (int i = section; i > 0; i--)
            {
                var numRead = stream.Read(readBuffer, 0, readBytes); // LUCENENET specific - asserting that we read the entire buffer
                Assert.AreEqual(readBytes, numRead);
            }

            Assert.AreEqual(readBuffer, buffer.Skip((section - 1) * readBytes).Take(readBytes).ToArray());
        }

        /// <summary>
        /// Test for GitHub issue #1158: Integer overflow in IndexInputStream.Read()
        /// https://github.com/apache/lucenenet/issues/1158
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public void TestGitHubIssue1158_IndexInputStream_Read_IntegerOverflow()
        {
            // This test verifies the fix for the integer overflow bug in IndexInputStream.Read()
            // Bug: When input.Length - input.Position > int.MaxValue, casting to int causes overflow
            // Previously on line 68: int remaining = (int)(input.Length - input.Position);

            // Arrange: Create a mock IndexInput that simulates a very large file
            var largeIndexInput = new LargeFileMockIndexInput();
            var stream = new IndexInputStream(largeIndexInput);

            // Position the stream at the beginning (position = 0)
            // Length is int.MaxValue + 1000L, so (Length - Position) = int.MaxValue + 1000L
            // When cast to int, this overflows and becomes negative: -2147482649
            largeIndexInput.Seek(0);

            // Act: Try to read from the stream
            byte[] buffer = new byte[50];

            // The bug manifests here:
            // remaining = (int)(int.MaxValue + 1000L) = -2147482649 (negative due to overflow)
            // readCount = Math.Min(-2147482649, 50) = -2147482649 (negative)
            // This would cause ReadBytes to be called with negative length

            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                // If we get here without exception, check if the read was successful
                // With the bug, bytesRead might be negative or cause other issues
                Assert.IsTrue(bytesRead >= 0, $"BytesRead should not be negative, but was {bytesRead}");
                Assert.AreEqual(50, bytesRead, "Should read exactly 50 bytes");
            }
            catch (ArgumentException ex)
            {
                // The bug may cause an ArgumentException when ReadBytes is called with negative length
                Assert.Fail($"Integer overflow caused ArgumentException: {ex.Message}");
            }
        }

    }

    /// <summary>
    /// Mock IndexInput that simulates a file larger than int.MaxValue
    /// to test for integer overflow issues
    /// </summary>
    internal class LargeFileMockIndexInput : IndexInput
    {
        private long position = 0;
        private readonly long length = (long)int.MaxValue + 1000L;

        public LargeFileMockIndexInput() : base("LargeFileMockIndexInput")
        {
        }

        public override long Length => length;

        public override long Position => position;

        public override byte ReadByte()
        {
            if (position >= length)
                throw new System.IO.EndOfStreamException();
            position++;
            return 0;
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            // Validate parameters to prevent unexpected behavior
            if (b == null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative");
            }

            if (len < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(len), "Length cannot be negative");
            }

            if (offset + len > b.Length)
            {
                throw new ArgumentException("The sum of offset and length exceeds the buffer length");
            }

            ReadBytes(b.AsSpan(offset, len));
        }

        // LUCENENET: Use Span<byte> instead of byte[] for better compatibility.
        public override void ReadBytes(Span<byte> destination)
        {
            int len = destination.Length;
            long available = length - position;
            if (available < len)
            {
                throw new System.IO.EndOfStreamException();
            }
            // Simulate reading by advancing position
            position += len;
            // Fill buffer with dummy data
            for (int i = 0; i < len; i++)
            {
                destination[/*offset +*/ i] = 0;
            }
        }

        public override void Seek(long pos)
        {
            if (pos < 0 || pos > length)
                throw new ArgumentOutOfRangeException(nameof(pos));
            position = pos;
        }

        public override object Clone()
        {
            var clone = new LargeFileMockIndexInput();
            clone.position = this.position;
            return clone;
        }

        protected override void Dispose(bool disposing)
        {
            // No resources to dispose
        }
    }

    //Note: LUCENENET specific
    internal static class ByteHelperExtensions
    {
        public static int KiloBytes(this int value)
        {
            return value * 1024;
        }
    }
}
