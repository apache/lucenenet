using System;
using System.IO;
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
    /// A <see cref="DataOutput"/> wrapping a plain <see cref="Stream"/>.
    /// </summary>
    public class OutputStreamDataOutput : DataOutput, IDisposable
    {
        private readonly Stream _os;
        private int disposed = 0; // LUCENENET specific - allow double-dispose

        public OutputStreamDataOutput(Stream os)
        {
            this._os = os ?? throw new ArgumentNullException(nameof(os)); // LUCENENET specific - added null guard clause
        }

        public override void WriteByte(byte b)
        {
            _os.WriteByte(b);
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            _os.Write(b, offset, length);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="OutputStreamDataOutput"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the <see cref="OutputStreamDataOutput"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>
        // LUCENENET specific - implemented proper dispose pattern
        protected virtual void Dispose(bool disposing)
        {
            if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

            if (disposing)
            {
                _os.Dispose();
            }
        }
    }
}