using Lucene.Net.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using ArrayUtil = Lucene.Net.Util.ArrayUtil;
using BytesRef = Lucene.Net.Util.BytesRef;

namespace Lucene.Net.Codecs.Compressing
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

    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// A compression mode. Tells how much effort should be spent on compression and
    /// decompression of stored fields.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class CompressionMode
    {
        /// <summary>
        /// A compression mode that trades compression ratio for speed. Although the
        /// compression ratio might remain high, compression and decompression are
        /// very fast. Use this mode with indices that have a high update rate but
        /// should be able to load documents from disk quickly.
        /// </summary>
        public static readonly CompressionMode FAST = new CompressionModeAnonymousClass();

        private sealed class CompressionModeAnonymousClass : CompressionMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Compressor NewCompressor()
            {
                return new LZ4FastCompressor();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Decompressor NewDecompressor()
            {
                return LZ4_DECOMPRESSOR;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override string ToString()
            {
                return "FAST";
            }
        }

        /// <summary>
        /// A compression mode that trades speed for compression ratio. Although
        /// compression and decompression might be slow, this compression mode should
        /// provide a good compression ratio. this mode might be interesting if/when
        /// your index size is much bigger than your OS cache.
        /// </summary>
        public static readonly CompressionMode HIGH_COMPRESSION = new CompressionModeAnonymousClass2();

        private sealed class CompressionModeAnonymousClass2 : CompressionMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Compressor NewCompressor()
            {
                return new DeflateCompressor(CompressionLevel.Optimal);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Decompressor NewDecompressor()
            {
                return new DeflateDecompressor();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override string ToString()
            {
                return "HIGH_COMPRESSION";
            }
        }

        /// <summary>
        /// This compression mode is similar to <see cref="FAST"/> but it spends more time
        /// compressing in order to improve the compression ratio. This compression
        /// mode is best used with indices that have a low update rate but should be
        /// able to load documents from disk quickly.
        /// </summary>
        public static readonly CompressionMode FAST_DECOMPRESSION = new CompressionModeAnonymousClass3();

        private sealed class CompressionModeAnonymousClass3 : CompressionMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Compressor NewCompressor()
            {
                return new LZ4HighCompressor();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Decompressor NewDecompressor()
            {
                return LZ4_DECOMPRESSOR;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override string ToString()
            {
                return "FAST_DECOMPRESSION";
            }
        }

        /// <summary>
        /// Sole constructor. </summary>
        protected CompressionMode()
        {
        }

        /// <summary>
        /// Create a new <see cref="Compressor"/> instance.
        /// </summary>
        public abstract Compressor NewCompressor();

        /// <summary>
        /// Create a new <see cref="Decompressor"/> instance.
        /// </summary>
        public abstract Decompressor NewDecompressor();

        private static readonly Decompressor LZ4_DECOMPRESSOR = new DecompressorAnonymousClass();

        private sealed class DecompressorAnonymousClass : Decompressor
        {
            public override void Decompress(DataInput @in, int originalLength, int offset, int length, BytesRef bytes)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(offset + length <= originalLength);
                // add 7 padding bytes, this is not necessary but can help decompression run faster
                if (bytes.Bytes.Length < originalLength + 7)
                {
                    bytes.Bytes = new byte[ArrayUtil.Oversize(originalLength + 7, 1)];
                }
                int decompressedLength = LZ4.Decompress(@in, offset + length, bytes.Bytes, 0);
                if (decompressedLength > originalLength)
                {
                    throw new CorruptIndexException("Corrupted: lengths mismatch: " + decompressedLength + " > " + originalLength + " (resource=" + @in + ")");
                }
                bytes.Offset = offset;
                bytes.Length = length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override object Clone()
            {
                return this;
            }
        }

        private sealed class LZ4FastCompressor : Compressor
        {
            private readonly LZ4.HashTable ht;

            internal LZ4FastCompressor()
            {
                ht = new LZ4.HashTable();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Compress(byte[] bytes, int off, int len, DataOutput @out)
            {
                LZ4.Compress(bytes, off, len, @out, ht);
            }
        }

        private sealed class LZ4HighCompressor : Compressor
        {
            internal readonly LZ4.HCHashTable ht;

            internal LZ4HighCompressor()
            {
                ht = new LZ4.HCHashTable();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Compress(byte[] bytes, int off, int len, DataOutput @out)
            {
                LZ4.CompressHC(bytes, off, len, @out, ht);
            }
        }

        private sealed class DeflateDecompressor : Decompressor
        {
            public override void Decompress(DataInput input, int originalLength, int offset, int length, BytesRef bytes)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(offset + length <= originalLength);
                if (length == 0)
                {
                    bytes.Length = 0;
                    return;
                }

                byte[] compressedBytes = new byte[input.ReadVInt32()];
                input.ReadBytes(compressedBytes, 0, compressedBytes.Length);
                byte[] decompressedBytes = null;

                using (MemoryStream decompressedStream = new MemoryStream())
                using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
                {
                    using DeflateStream dStream = new DeflateStream(compressedStream, System.IO.Compression.CompressionMode.Decompress);
                    dStream.CopyTo(decompressedStream);
                    decompressedBytes = decompressedStream.ToArray();
                }

                if (decompressedBytes.Length != originalLength)
                {
                    throw new CorruptIndexException("Length mismatch: " + decompressedBytes.Length + " != " + originalLength + " (resource=" + input + ")");
                }

                bytes.Bytes = decompressedBytes;
                bytes.Offset = offset;
                bytes.Length = length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override object Clone()
            {
                return new DeflateDecompressor();
            }
        }

        private class DeflateCompressor : Compressor
        {
            private readonly CompressionLevel compressionLevel; // LUCENENET: marked readonly
            internal DeflateCompressor(CompressionLevel level)
            {
                compressionLevel = level;
            }

            public override void Compress(byte[] bytes, int off, int len, DataOutput output)
            {
                // LUCENENET specific - since DeflateStream works a bit differently than Java's Deflate class,
                // we are unable to assert the total count
                byte[] resultArray = null;
                using (MemoryStream compressionMemoryStream = new MemoryStream())
                {
                    using (DeflateStream deflateStream = new DeflateStream(compressionMemoryStream, compressionLevel))
                    {
                        deflateStream.Write(bytes, off, len);
                    }
                    resultArray = compressionMemoryStream.ToArray();
                }

                if (resultArray.Length == 0)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(len == 0, "{0}", len);
                    output.WriteVInt32(0);
                    //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
                }
                else
                {
                    output.WriteVInt32(resultArray.Length);
                    output.WriteBytes(resultArray, resultArray.Length);
                }
            }
        }
    }
}
