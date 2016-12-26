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
    /// Used by MockDirectoryWrapper to create an input stream that
    /// keeps track of when it's been closed.
    /// </summary>

    public class MockIndexInputWrapper : IndexInput
    {
        private MockDirectoryWrapper Dir;
        internal readonly string Name;
        private IndexInput @delegate;
        private bool IsClone;
        private bool Closed;

        /// <summary>
        /// Construct an empty output buffer. </summary>
        public MockIndexInputWrapper(MockDirectoryWrapper dir, string name, IndexInput @delegate)
            : base("MockIndexInputWrapper(name=" + name + " delegate=" + @delegate + ")")
        {
            this.Name = name;
            this.Dir = dir;
            this.@delegate = @delegate;
        }

        public override void Dispose()
        {
            try
            {
                // turn on the following to look for leaks closing inputs,
                // after fixing TestTransactions
                // Dir.MaybeThrowDeterministicException();
            }
            finally
            {
                Closed = true;
                @delegate.Dispose();
                // Pending resolution on LUCENE-686 we may want to
                // remove the conditional check so we also track that
                // all clones get closed:
                if (!IsClone)
                {
                    Dir.RemoveIndexInput(this, Name);
                }
            }
        }

        private void EnsureOpen()
        {
            if (Closed)
            {
                throw new Exception("Abusing closed IndexInput!");
            }
        }

        public override object Clone()
        {
            EnsureOpen();
            Dir.InputCloneCount_Renamed.IncrementAndGet();
            IndexInput iiclone = (IndexInput)@delegate.Clone();
            MockIndexInputWrapper clone = new MockIndexInputWrapper(Dir, Name, iiclone);
            clone.IsClone = true;
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
                throw new RuntimeException("BUG: cloned file was not open?");
              }
            }
            */
            return clone;
        }

        public override long FilePointer
        {
            get
            {
                EnsureOpen();
                return @delegate.FilePointer;
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

        public override short ReadShort()
        {
            EnsureOpen();
            return @delegate.ReadShort();
        }

        public override int ReadInt()
        {
            EnsureOpen();
            return @delegate.ReadInt();
        }

        public override long ReadLong()
        {
            EnsureOpen();
            return @delegate.ReadLong();
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

        public override int ReadVInt()
        {
            EnsureOpen();
            return @delegate.ReadVInt();
        }

        public override long ReadVLong()
        {
            EnsureOpen();
            return @delegate.ReadVLong();
        }

        public override string ToString()
        {
            return "MockIndexInputWrapper(" + @delegate + ")";
        }
    }
}