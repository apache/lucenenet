using System;
using System.Collections.Generic;

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
    /// Used by <see cref="MockDirectoryWrapper"/> to create an input stream that
    /// keeps track of when it's been disposed.
    /// </summary>
    public class MockIndexInputWrapper : IndexInput
    {
        private readonly MockDirectoryWrapper dir; // LUCENENET: marked readonly
        internal readonly string name;
        private readonly IndexInput @delegate; // LUCENENET: marked readonly
        private bool isClone;
        private bool closed;

        /// <summary>
        /// Construct an empty output buffer. </summary>
        public MockIndexInputWrapper(MockDirectoryWrapper dir, string name, IndexInput @delegate)
            : base("MockIndexInputWrapper(name=" + name + " delegate=" + @delegate + ")")
        {
            this.name = name;
            this.dir = dir;
            this.@delegate = @delegate;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // turn on the following to look for leaks closing inputs,
                    // after fixing TestTransactions
                    // Dir.MaybeThrowDeterministicException();
                }
                finally
                {
                    closed = true;
                    @delegate.Dispose();
                    // Pending resolution on LUCENE-686 we may want to
                    // remove the conditional check so we also track that
                    // all clones get closed:
                    if (!isClone)
                    {
                        dir.RemoveIndexInput(this, name);
                    }
                }
            }
        }

        private void EnsureOpen()
        {
            if (closed)
            {
                throw RuntimeException.Create("Abusing closed IndexInput!");
            }
        }

        public override object Clone()
        {
            EnsureOpen();
            dir.inputCloneCount.IncrementAndGet();
            IndexInput iiclone = (IndexInput)@delegate.Clone();
            MockIndexInputWrapper clone = new MockIndexInputWrapper(dir, name, iiclone);
            clone.isClone = true;
            // Pending resolution on LUCENE-686 we may want to
            // uncomment this code so that we also track that all
            // clones get closed:
            /*
            synchronized(dir.openFiles) {
              if (dir.openFiles.containsKey(name)) {
                Integer v = (Integer) dir.openFiles.get(name);
                v = Integer.valueOf(v.intValue()+1);
                dir.openFiles.put(name, v);
              } else {
                throw RuntimeException.Create("BUG: cloned file was not open?");
              }
            }
            */
            return clone;
        }

        public override long Position // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        {
            get
            {
                EnsureOpen();
                return @delegate.Position;
            }
        }

        public override void Seek(long pos)
        {
            EnsureOpen();
            @delegate.Seek(pos);
        }

        public override long Length
        {
            get
            {
                EnsureOpen();
                return @delegate.Length;
            }
        }

        public override byte ReadByte()
        {
            EnsureOpen();
            return @delegate.ReadByte();
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            EnsureOpen();
            @delegate.ReadBytes(b, offset, len);
        }

        public override void ReadBytes(byte[] b, int offset, int len, bool useBuffer)
        {
            EnsureOpen();
            @delegate.ReadBytes(b, offset, len, useBuffer);
        }

        /// <summary>
        /// NOTE: this was readShort() in Lucene
        /// </summary>
        public override short ReadInt16()
        {
            EnsureOpen();
            return @delegate.ReadInt16();
        }

        /// <summary>
        /// NOTE: this was readInt() in Lucene
        /// </summary>
        public override int ReadInt32()
        {
            EnsureOpen();
            return @delegate.ReadInt32();
        }

        /// <summary>
        /// NOTE: this was readLong() in Lucene
        /// </summary>
        public override long ReadInt64()
        {
            EnsureOpen();
            return @delegate.ReadInt64();
        }

        public override string ReadString()
        {
            EnsureOpen();
            return @delegate.ReadString();
        }

        public override IDictionary<string, string> ReadStringStringMap()
        {
            EnsureOpen();
            return @delegate.ReadStringStringMap();
        }

        /// <summary>
        /// NOTE: this was readVInt() in Lucene
        /// </summary>
        public override int ReadVInt32()
        {
            EnsureOpen();
            return @delegate.ReadVInt32();
        }

        /// <summary>
        /// NOTE: this was readVLong() in Lucene
        /// </summary>
        public override long ReadVInt64()
        {
            EnsureOpen();
            return @delegate.ReadVInt64();
        }

        public override string ToString()
        {
            return "MockIndexInputWrapper(" + @delegate + ")";
        }
    }
}