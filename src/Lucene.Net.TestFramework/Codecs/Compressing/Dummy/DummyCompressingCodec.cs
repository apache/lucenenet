using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Compressing.Dummy
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
    /// <see cref="CompressingCodec"/> that does not compress data, useful for testing. </summary>
    // In its own package to make sure the oal.codecs.compressing classes are
    // visible enough to let people write their own CompressionMode
    [CodecName("DummyCompressingStoredFields")]
    public class DummyCompressingCodec : CompressingCodec
    {
        public static readonly CompressionMode DUMMY = new CompressionModeAnonymousClass();

        private sealed class CompressionModeAnonymousClass : CompressionMode
        {
            public CompressionModeAnonymousClass()
            { }

            public override Compressor NewCompressor()
            {
                return DUMMY_COMPRESSOR;
            }

            public override Decompressor NewDecompressor()
            {
                return DUMMY_DECOMPRESSOR;
            }

            public override string ToString()
            {
                return "DUMMY";
            }
        }

        private static readonly Decompressor DUMMY_DECOMPRESSOR = new DecompressorAnonymousClass();

        private sealed class DecompressorAnonymousClass : Decompressor
        {
            public override void Decompress(DataInput @in, int originalLength, int offset, int length, BytesRef bytes)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(offset + length <= originalLength);
                if (bytes.Bytes.Length < originalLength)
                {
                    bytes.Bytes = new byte[ArrayUtil.Oversize(originalLength, 1)];
                }
                @in.ReadBytes(bytes.Bytes, 0, offset + length);
                bytes.Offset = offset;
                bytes.Length = length;
            }

            public override object Clone()
            {
                return this;
            }
        }

        private static readonly Compressor DUMMY_COMPRESSOR = new CompressorAnonymousClass();

        private sealed class CompressorAnonymousClass : Compressor
        {
            public override void Compress(byte[] bytes, int off, int len, DataOutput @out)
            {
                @out.WriteBytes(bytes, off, len);
            }
        }

        /// <summary>
        /// Constructor that allows to configure the <paramref name="chunkSize"/>. </summary>
        public DummyCompressingCodec(int chunkSize, bool withSegmentSuffix)
            : base(withSegmentSuffix ? "DummyCompressingStoredFields" : "", DUMMY, chunkSize)
        { }

        /// <summary>
        /// Default constructor. </summary>
        public DummyCompressingCodec()
            : this(1 << 14, false)
        { }
    }
}