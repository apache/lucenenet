using Lucene.Net.Store;
using System;
using System.IO;

namespace Lucene.Net.Replicator
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
    /// A <see cref="Stream"/> which wraps an <see cref="IndexInput"/>.
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    public class IndexInputStream : Stream
    {
        private readonly IndexInput input;

        public IndexInputStream(IndexInput input)
        {
            this.input = input;
        }

        public override void Flush()
        {
            throw IllegalStateException.Create("Cannot flush a readonly stream."); // LUCENENET TODO: Change to NotSupportedException ?
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw IllegalStateException.Create("Cannot change length of a readonly stream."); // LUCENENET TODO: Change to NotSupportedException ?
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = (int) (input.Length - input.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            int readCount = Math.Min(remaining, count);
            input.ReadBytes(buffer, offset, readCount);
            return readCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidCastException("Cannot write to a readonly stream."); // LUCENENET TODO: Change to NotSupportedException ?
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => input.Length;

        public override long Position
        {
            get => input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            set => input.Seek(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                input.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}