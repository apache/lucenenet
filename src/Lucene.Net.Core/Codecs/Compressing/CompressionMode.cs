using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
        public static readonly CompressionMode FAST = new CompressionModeAnonymousInnerClassHelper();

        private class CompressionModeAnonymousInnerClassHelper : CompressionMode
        {
            public CompressionModeAnonymousInnerClassHelper()
            {
            }

            public override Compressor NewCompressor()
            {
                return new LZ4FastCompressor();
            }

            public override Decompressor NewDecompressor()
            {
                return LZ4_DECOMPRESSOR;
            }

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
        public static readonly CompressionMode HIGH_COMPRESSION = new CompressionModeAnonymousInnerClassHelper2();

        private class CompressionModeAnonymousInnerClassHelper2 : CompressionMode
        {
            public CompressionModeAnonymousInnerClassHelper2()
            {
            }

            public override Compressor NewCompressor()
            {
                return new DeflateCompressor(System.IO.Compression.CompressionLevel.Optimal);
            }

            public override Decompressor NewDecompressor()
            {
                return new DeflateDecompressor();
            }

            public override string ToString()
            {
                return "HIGH_COMPRESSION";
            }
        }

        /// <summary>
        /// this compression mode is similar to <seealso cref="#FAST"/> but it spends more time
        /// compressing in order to improve the compression ratio. this compression
        /// mode is best used with indices that have a low update rate but should be
        /// able to load documents from disk quickly.
        /// </summary>
        public static readonly CompressionMode FAST_DECOMPRESSION = new CompressionModeAnonymousInnerClassHelper3();

        private class CompressionModeAnonymousInnerClassHelper3 : CompressionMode
        {
            public CompressionModeAnonymousInnerClassHelper3()
            {
            }

            public override Compressor NewCompressor()
            {
                return new LZ4HighCompressor();
            }

            public override Decompressor NewDecompressor()
            {
                return LZ4_DECOMPRESSOR;
            }

            public override string ToString()
            {
                return "FAST_DECOMPRESSION";
            }
        }

        /// <summary>
        /// Sole constructor. </summary>
        protected internal CompressionMode()
        {
        }

        /// <summary>
        /// Create a new <seealso cref="Compressor"/> instance.
        /// </summary>
        public abstract Compressor NewCompressor();

        /// <summary>
        /// Create a new <seealso cref="Decompressor"/> instance.
        /// </summary>
        public abstract Decompressor NewDecompressor();

        private static readonly Decompressor LZ4_DECOMPRESSOR = new DecompressorAnonymousInnerClassHelper();

        private class DecompressorAnonymousInnerClassHelper : Decompressor
        {
            public DecompressorAnonymousInnerClassHelper()
            {
            }

            public override void Decompress(DataInput @in, int originalLength, int offset, int length, BytesRef bytes)
            {
                Debug.Assert(offset + length <= originalLength);
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

            public override void Compress(byte[] bytes, int off, int len, DataOutput @out)
            {
                LZ4.CompressHC(bytes, off, len, @out, ht);
            }
        }

        private sealed class DeflateDecompressor : Decompressor
        {

            internal DeflateDecompressor()
            {
            }

            public override void Decompress(DataInput input, int originalLength, int offset, int length, BytesRef bytes)
            {
                Debug.Assert(offset + length <= originalLength);
                if (length == 0)
                {
                    bytes.Length = 0;
                    return;
                }

                byte[] compressedBytes = new byte[input.ReadVInt()];
                input.ReadBytes(compressedBytes, 0, compressedBytes.Length);
                byte[] decompressedBytes = null;

                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
                    {
                        using (DeflateStream dStream = new DeflateStream(compressedStream, System.IO.Compression.CompressionMode.Decompress))
                        {
                            dStream.CopyTo(decompressedStream);
                        }
                    }
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

            public override object Clone()
            {
                return new DeflateDecompressor();
            }
        }

        private class DeflateCompressor : Compressor
        {
            private CompressionLevel compressionLevel;
            internal DeflateCompressor(CompressionLevel level)
            {
                compressionLevel = level;
            }

            public override void Compress(byte[] bytes, int off, int len, DataOutput output)
            {
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
                    Debug.Assert(len == 0, len.ToString());
                    output.WriteVInt(0);
                    return;
                }
                else
                {
                    output.WriteVInt(resultArray.Length);
                    output.WriteBytes(resultArray, resultArray.Length);
                }
            }
        }
    }
}
