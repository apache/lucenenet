using System;
using System.Diagnostics;
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

    using DataInput = Lucene.Net.Store.DataInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;

    /// <summary>
    /// Intentionally slow IndexOutput for testing.
    /// </summary>
    public class ThrottledIndexOutput : IndexOutput
    {
        public const int DEFAULT_MIN_WRITTEN_BYTES = 1024;
        private readonly int BytesPerSecond;
        private IndexOutput @delegate;
        private long FlushDelayMillis;
        private long CloseDelayMillis;
        private long SeekDelayMillis;
        private long PendingBytes;
        private long MinBytesWritten;
        private long TimeElapsed;
        private readonly byte[] Bytes = new byte[1];

        public virtual ThrottledIndexOutput NewFromDelegate(IndexOutput output)
        {
            return new ThrottledIndexOutput(BytesPerSecond, FlushDelayMillis, CloseDelayMillis, SeekDelayMillis, MinBytesWritten, output);
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
            Debug.Assert(bytesPerSecond > 0);
            this.@delegate = @delegate;
            this.BytesPerSecond = bytesPerSecond;
            this.FlushDelayMillis = flushDelayMillis;
            this.CloseDelayMillis = closeDelayMillis;
            this.SeekDelayMillis = seekDelayMillis;
            this.MinBytesWritten = minBytesWritten;
        }

        public override void Flush()
        {
            Sleep(FlushDelayMillis);
            @delegate.Flush();
        }

        public override void Dispose()
        {
            try
            {
                Sleep(CloseDelayMillis + GetDelay(true));
            }
            finally
            {
                @delegate.Dispose();
            }
        }

        public override long FilePointer
        {
            get
            {
                return @delegate.FilePointer;
            }
        }

        public override void Seek(long pos)
        {
            Sleep(SeekDelayMillis);
            @delegate.Seek(pos);
        }

        public override void WriteByte(byte b)
        {
            Bytes[0] = b;
            WriteBytes(Bytes, 0, 1);
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            long before = DateTime.Now.Ticks;
            // TODO: sometimes, write only half the bytes, then
            // sleep, then 2nd half, then sleep, so we sometimes
            // interrupt having only written not all bytes
            @delegate.WriteBytes(b, offset, length);
            TimeElapsed += (DateTime.Now.Ticks - before) * 100;
            PendingBytes += length;
            Sleep(GetDelay(false));
        }

        protected internal virtual long GetDelay(bool closing)
        {
            if (PendingBytes > 0 && (closing || PendingBytes > MinBytesWritten))
            {
                long actualBps = (TimeElapsed / PendingBytes) * 1000000000L; // nano to sec
                if (actualBps > BytesPerSecond)
                {
                    long expected = (PendingBytes * 1000L / BytesPerSecond);
                    long delay = expected - (TimeElapsed / 1000000L);
                    PendingBytes = 0;
                    TimeElapsed = 0;
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
#if !NETCORE
            try
            {
#endif 
                Thread.Sleep(TimeSpan.FromMilliseconds(ms));
#if !NETCORE
            }
            catch (ThreadInterruptedException e)
            {
                throw new ThreadInterruptedException("Thread Interrupted Exception", e);
            }
#endif
        }

        public override long Length
        {
            set
            {
                @delegate.Length = value;
            }

            get
            {
                return @delegate.Length;
            }
        }

        public override void CopyBytes(DataInput input, long numBytes)
        {
            @delegate.CopyBytes(input, numBytes);
        }

        public override long Checksum
        {
            get
            {
                return @delegate.Checksum;
            }
        }
    }
}