using System.Threading;

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
    /// Used by MockRAMDirectory to create an output stream that
    /// will throw anSystem.IO.IOException on fake disk full, track max
    /// disk space actually used, and maybe throw random
    ///System.IO.IOExceptions.
    /// </summary>

    public class MockIndexOutputWrapper : IndexOutput
    {
        private MockDirectoryWrapper Dir;
        private readonly IndexOutput @delegate;
        private bool First = true;
        internal readonly string Name;

        internal byte[] SingleByte = new byte[1];

        /// <summary>
        /// Construct an empty output buffer. </summary>
        public MockIndexOutputWrapper(MockDirectoryWrapper dir, IndexOutput @delegate, string name)
        {
            this.Dir = dir;
            this.Name = name;
            this.@delegate = @delegate;
        }

        private void CheckCrashed()
        {
            // If MockRAMDir crashed since we were opened, then don't write anything
            if (Dir.Crashed)
            {
                throw new System.IO.IOException("MockRAMDirectory was crashed; cannot write to " + Name);
            }
        }

        private void CheckDiskFull(byte[] b, int offset, DataInput @in, long len)
        {
            long freeSpace = Dir.MaxSize == 0 ? 0 : Dir.MaxSize - Dir.SizeInBytes();
            long realUsage = 0;

            // Enforce disk full:
            if (Dir.MaxSize != 0 && freeSpace <= len)
            {
                // Compute the real disk free.  this will greatly slow
                // down our test but makes it more accurate:
                realUsage = Dir.RecomputedActualSizeInBytes;
                freeSpace = Dir.MaxSize - realUsage;
            }

            if (Dir.MaxSize != 0 && freeSpace <= len)
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
                if (realUsage > Dir.MaxUsedSize)
                {
                    Dir.MaxUsedSize = realUsage;
                }
                string message = "fake disk full at " + Dir.RecomputedActualSizeInBytes + " bytes when writing " + Name + " (file length=" + @delegate.Length;
                if (freeSpace > 0)
                {
                    message += "; wrote " + freeSpace + " of " + len + " bytes";
                }
                message += ")";
                /*if (LuceneTestCase.VERBOSE)
                {
                  Console.WriteLine(Thread.CurrentThread.Name + ": MDW: now throw fake disk full");
                  (new Exception()).printStackTrace(System.out);
                }*/
                throw new System.IO.IOException(message);
            }
        }

        public override void Dispose()
        {
            try
            {
                Dir.MaybeThrowDeterministicException();
            }
            finally
            {
                @delegate.Dispose();
                if (Dir.TrackDiskUsage_Renamed)
                {
                    // Now compute actual disk usage & track the maxUsedSize
                    // in the MockDirectoryWrapper:
                    long size = Dir.RecomputedActualSizeInBytes;
                    if (size > Dir.MaxUsedSize)
                    {
                        Dir.MaxUsedSize = size;
                    }
                }
                Dir.RemoveIndexOutput(this, Name);
            }
        }

        public override void Flush()
        {
            Dir.MaybeThrowDeterministicException();
            @delegate.Flush();
        }

        public override void WriteByte(byte b)
        {
            SingleByte[0] = b;
            WriteBytes(SingleByte, 0, 1);
        }

        public override void WriteBytes(byte[] b, int offset, int len)
        {
            CheckCrashed();
            CheckDiskFull(b, offset, null, len);

            if (Dir.RandomState.Next(200) == 0)
            {
                int half = len / 2;
                @delegate.WriteBytes(b, offset, half);
                Thread.Sleep(0);
                @delegate.WriteBytes(b, offset + half, len - half);
            }
            else
            {
                @delegate.WriteBytes(b, offset, len);
            }

            Dir.MaybeThrowDeterministicException();

            if (First)
            {
                // Maybe throw random exception; only do this on first
                // write to a new file:
                First = false;
                Dir.MaybeThrowIOException(Name);
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
            @delegate.Seek(pos);
        }

        public override long Length
        {
            get
            {
                return @delegate.Length;
            }

            set
            {
                @delegate.Length = value;
            }
        }

        public override void CopyBytes(DataInput input, long numBytes)
        {
            CheckCrashed();
            CheckDiskFull(null, 0, input, numBytes);

            @delegate.CopyBytes(input, numBytes);
            Dir.MaybeThrowDeterministicException();
        }

        public override long Checksum
        {
            get
            {
                return @delegate.Checksum;
            }
        }

        public override string ToString()
        {
            return "MockIndexOutputWrapper(" + @delegate + ")";
        }
    }
}