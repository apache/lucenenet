using System;
using System.IO;

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
    /// A <seealso cref="DataInput"/> wrapping a plain <seealso cref="InputStream"/>.
    /// </summary>
    public class InputStreamDataInput : DataInput, IDisposable
    {
        private BinaryReader _reader;

        public InputStreamDataInput(Stream @is)
        {
            this._reader = new BinaryReader(@is);
        }

        public override byte ReadByte()
        {
            int v = _reader.ReadByte();
            if (v == -1)
            {
                throw new EndOfStreamException();
            }
            return (byte)v;
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                int cnt = _reader.Read(b, offset, len);
                if (cnt < 0)
                {
                    // Partially read the input, but no more data available in the stream.
                    throw new EndOfStreamException();
                }
                len -= cnt;
                offset += cnt;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _reader.Dispose();
                }

                _reader = null;
                disposed = true;
            }
        }
    }
}