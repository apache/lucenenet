using Lucene.Net.Util;
using System;
using System.IO;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Store
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

    /// <summary>
    /// Used by <see cref="MockDirectoryWrapper"/> to create an output stream that
    /// will throw an <see cref="IOException"/> on fake disk full, track max
    /// disk space actually used, and maybe throw random
    /// <see cref="IOException"/>s.
    /// </summary>
    public class MockIndexOutputWrapper : IndexOutput
    {
        private readonly MockDirectoryWrapper dir;
        private readonly IndexOutput @delegate;
        private bool first = true;
        internal readonly string name;

        internal byte[] singleByte = new byte[1];

        /// <summary>
        /// Construct an empty output buffer. </summary>
        public MockIndexOutputWrapper(MockDirectoryWrapper dir, IndexOutput @delegate, string name)
        {
            this.dir = dir;
            this.name = name;
            this.@delegate = @delegate;
        }

        private void CheckCrashed()
        {
            // If MockRAMDir crashed since we were opened, then don't write anything
            if (dir.crashed)
            {
                throw new IOException("MockDirectoryWrapper was crashed; cannot write to " + name);
            }
        }

        private void CheckDiskFull(byte[] b, int offset, DataInput @in, long len)
        {
            long freeSpace = dir.maxSize == 0 ? 0 : dir.maxSize - dir.GetSizeInBytes();
            long realUsage = 0;

            // Enforce disk full:
            if (dir.maxSize != 0 && freeSpace <= len)
            {
                // Compute the real disk free.  this will greatly slow
                // down our test but makes it more accurate:
                realUsage = dir.GetRecomputedActualSizeInBytes();
                freeSpace = dir.maxSize - realUsage;
            }

            if (dir.maxSize != 0 && freeSpace <= len)
            {
                if (freeSpace > 0)
                {
                    realUsage += freeSpace;
                    if (b != null)
                    {
                        @delegate.WriteBytes(b, offset, (int)freeSpace);
                    }
                    else
                    {
                        @delegate.CopyBytes(@in, len);
                    }
                }
                if (realUsage > dir.maxUsedSize)
                {
                    dir.maxUsedSize = realUsage;
                }
                string message = "fake disk full at " + dir.GetRecomputedActualSizeInBytes() + " bytes when writing " + name + " (file length=" + @delegate.Length;
                if (freeSpace > 0)
                {
                    message += "; wrote " + freeSpace + " of " + len + " bytes";
                }
                message += ")";
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": MDW: now throw fake disk full");
                    Console.WriteLine(Environment.StackTrace);
                }
                throw new IOException(message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    dir.MaybeThrowDeterministicException();
                }
                finally
                {
                    @delegate.Dispose();
                    if (dir.trackDiskUsage)
                    {
                        // Now compute actual disk usage & track the maxUsedSize
                        // in the MockDirectoryWrapper:
                        long size = dir.GetRecomputedActualSizeInBytes();
                        if (size > dir.maxUsedSize)
                        {
                            dir.maxUsedSize = size;
                        }
                    }
                    dir.RemoveIndexOutput(this, name);
                }
            }
        }

        public override void Flush()
        {
            dir.MaybeThrowDeterministicException();
            @delegate.Flush();
        }

        public override void WriteByte(byte b)
        {
            singleByte[0] = b;
            WriteBytes(singleByte, 0, 1);
        }

        public override void WriteBytes(byte[] b, int offset, int len)
        {
            CheckCrashed();
            CheckDiskFull(b, offset, null, len);

            if (dir.randomState.Next(200) == 0)
            {
                int half = len / 2;
                @delegate.WriteBytes(b, offset, half);
                Thread.Yield();
                @delegate.WriteBytes(b, offset + half, len - half);
            }
            else
            {
                @delegate.WriteBytes(b, offset, len);
            }

            dir.MaybeThrowDeterministicException();

            if (first)
            {
                // Maybe throw random exception; only do this on first
                // write to a new file:
                first = false;
                dir.MaybeThrowIOException(name);
            }
        }

        public override long Position => @delegate.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

        [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
        public override void Seek(long pos)
        {
            @delegate.Seek(pos);
        }

        public override long Length
        {
            get => @delegate.Length;
            set => @delegate.Length = value;
        }

        public override void CopyBytes(DataInput input, long numBytes)
        {
            CheckCrashed();
            CheckDiskFull(null, 0, input, numBytes);

            @delegate.CopyBytes(input, numBytes);
            dir.MaybeThrowDeterministicException();
        }

        public override long Checksum => @delegate.Checksum;

        public override string ToString()
        {
            return "MockIndexOutputWrapper(" + @delegate + ")";
        }
    }
}