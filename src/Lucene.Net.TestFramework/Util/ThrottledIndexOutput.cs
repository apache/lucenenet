using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Threading;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements. See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License. You may obtain a copy of the License at
     *
     * http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Intentionally slow <see cref="IndexOutput"/> for testing.
    /// </summary>
    public class ThrottledIndexOutput : IndexOutput
    {
        public const int DEFAULT_MIN_WRITTEN_BYTES = 1024;
        private readonly int bytesPerSecond;
        private readonly IndexOutput @delegate; // LUCENENET: marked readonly
        private readonly long flushDelayMillis; // LUCENENET: marked readonly
        private readonly long closeDelayMillis; // LUCENENET: marked readonly
        private readonly long seekDelayMillis; // LUCENENET: marked readonly
        private long pendingBytes;
        private readonly long minBytesWritten; // LUCENENET: marked readonly
        private long timeElapsed;
        private readonly byte[] bytes = new byte[1];

        public virtual ThrottledIndexOutput NewFromDelegate(IndexOutput output)
        {
            return new ThrottledIndexOutput(bytesPerSecond, flushDelayMillis, closeDelayMillis, seekDelayMillis, minBytesWritten, output);
        }

        public ThrottledIndexOutput(int bytesPerSecond, long delayInMillis, IndexOutput @delegate)
            : this(bytesPerSecond, delayInMillis, delayInMillis, delayInMillis, DEFAULT_MIN_WRITTEN_BYTES, @delegate)
        {
        }

        public ThrottledIndexOutput(int bytesPerSecond, long delays, int minBytesWritten, IndexOutput @delegate)
            : this(bytesPerSecond, delays, delays, delays, minBytesWritten, @delegate)
        {
        }

        public static int MBitsToBytes(int mbits)
        {
            return mbits * 125000;
        }

        public ThrottledIndexOutput(int bytesPerSecond, long flushDelayMillis, long closeDelayMillis, long seekDelayMillis, long minBytesWritten, IndexOutput @delegate)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(bytesPerSecond > 0);
            this.@delegate = @delegate;
            this.bytesPerSecond = bytesPerSecond;
            this.flushDelayMillis = flushDelayMillis;
            this.closeDelayMillis = closeDelayMillis;
            this.seekDelayMillis = seekDelayMillis;
            this.minBytesWritten = minBytesWritten;
        }

        public override void Flush()
        {
            Sleep(flushDelayMillis);
            @delegate.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Sleep(closeDelayMillis + GetDelay(true));
                }
                finally
                {
                    @delegate?.Dispose(); // LUCENENET specific - only call if non-null
                }
            }
        }

        public override long Position => @delegate.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

        [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
        public override void Seek(long pos)
        {
            Sleep(seekDelayMillis);
            @delegate.Seek(pos);
        }

        public override void WriteByte(byte b)
        {
            bytes[0] = b;
            WriteBytes(bytes, 0, 1);
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            long before = J2N.Time.NanoTime();
            // TODO: sometimes, write only half the bytes, then
            // sleep, then 2nd half, then sleep, so we sometimes
            // interrupt having only written not all bytes
            @delegate.WriteBytes(b, offset, length);
            timeElapsed += J2N.Time.NanoTime() - before;
            pendingBytes += length;
            Sleep(GetDelay(false));
        }

        protected internal virtual long GetDelay(bool closing)
        {
            if (pendingBytes > 0 && (closing || pendingBytes > minBytesWritten))
            {
                long actualBps = (timeElapsed / pendingBytes) * 1000000000L; // nano to sec
                if (actualBps > bytesPerSecond)
                {
                    long expected = (pendingBytes * 1000L / bytesPerSecond);
                    long delay = expected - (timeElapsed / 1000000L);
                    pendingBytes = 0;
                    timeElapsed = 0;
                    return delay;
                }
            }
            return 0;
        }

        private static void Sleep(long ms)
        {
            if (ms <= 0)
            {
                return;
            }

            try
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(ms));
            }
            catch (Exception e) when (e.IsInterruptedException())
            {
#pragma warning disable IDE0001 // Simplify name
                throw new Util.ThreadInterruptedException(e);
#pragma warning restore IDE0001 // Simplify name
            }
        }

        public override long Length
        {
            get => @delegate.Length;
            set => @delegate.Length = value;
        }

        public override void CopyBytes(DataInput input, long numBytes)
        {
            @delegate.CopyBytes(input, numBytes);
        }

        public override long Checksum
            => @delegate.Checksum;
    }
}