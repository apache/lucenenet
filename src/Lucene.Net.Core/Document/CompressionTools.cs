using Lucene.Net.Util;
using System.IO;
using System.IO.Compression;

namespace Lucene.Net.Documents
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
    /// Simple utility class providing static methods to
    ///  compress and decompress binary data for stored fields.
    ///  this class uses java.util.zip.Deflater and Inflater
    ///  classes to compress and decompress.
    /// </summary>

    public class CompressionTools
    {
        // Export only static methods
        private CompressionTools()
        {
        }

        /// <summary>
        /// Compresses the specified byte range using the
        ///  specified compressionLevel 
        /// </summary>
        public static byte[] Compress(byte[] value, int offset, int length, CompressionLevel compressionLevel)
        {
            byte[] resultArray = null;
            using (MemoryStream compressionMemoryStream = new MemoryStream())
            {
                using (DeflateStream deflateStream = new DeflateStream(compressionMemoryStream, compressionLevel))
                {

                    deflateStream.Write(value, offset, length);
                }
                resultArray = compressionMemoryStream.ToArray();
            }
            return resultArray;
        }

        /// <summary>
        /// Compresses the specified byte range, with default Optimal level 
        /// </summary>
        public static byte[] Compress(byte[] value, int offset, int length)
        {
            return Compress(value, offset, length, CompressionLevel.Optimal);
        }

        /// <summary>
        /// Compresses all bytes in the array, with default Optimal level </summary>
        public static byte[] Compress(byte[] value)
        {
            return Compress(value, 0, value.Length, CompressionLevel.Optimal);
        }

        /// <summary>
        /// Compresses the String value, with default BEST_COMPRESSION level </summary>
        public static byte[] CompressString(string value)
        {
            return CompressString(value, CompressionLevel.Optimal);
        }

        /// <summary>
        /// Compresses the String value using the specified
        ///  compressionLevel.
        /// </summary>
        public static byte[] CompressString(string value, CompressionLevel compressionLevel)
        {
            var result = new BytesRef();
            UnicodeUtil.UTF16toUTF8(value.ToCharArray(), 0, value.Length, result);
            return Compress(result.Bytes, 0, result.Length, compressionLevel);
        }

        /// <summary>
        /// Decompress the byte array previously returned by
        ///  compress (referenced by the provided BytesRef)
        /// </summary>
        public static byte[] Decompress(BytesRef bytes)
        {
            return Decompress(bytes.Bytes, bytes.Offset, bytes.Length);
        }

        /// <summary>
        /// Decompress the byte array previously returned by
        ///  compress
        /// </summary>
        public static byte[] Decompress(byte[] value)
        {
            return Decompress(value, 0, value.Length);
        }

        /// <summary>
        /// Decompress the byte array previously returned by
        ///  compress
        /// </summary>
        public static byte[] Decompress(byte[] value, int offset, int length)
        {
            byte[] decompressedBytes = null;

            using (MemoryStream decompressedStream = new MemoryStream())
            {
                using (MemoryStream compressedStream = new MemoryStream(value))
                {
                    using (DeflateStream dStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                    {
                        dStream.CopyTo(decompressedStream);
                    }
                }
                decompressedBytes = decompressedStream.ToArray();
            }

            return decompressedBytes;
        }

        /// <summary>
        /// Decompress the byte array previously returned by
        ///  compressString back into a String
        /// </summary>
        public static string DecompressString(byte[] value)
        {
            return DecompressString(value, 0, value.Length);
        }

        /// <summary>
        /// Decompress the byte array previously returned by
        ///  compressString back into a String
        /// </summary>
        public static string DecompressString(byte[] value, int offset, int length)
        {
            byte[] bytes = Decompress(value, offset, length);
            CharsRef result = new CharsRef(bytes.Length);
            UnicodeUtil.UTF8toUTF16(bytes, 0, bytes.Length, result);
            return new string(result.Chars, 0, result.Length);
        }

        /// <summary>
        /// Decompress the byte array (referenced by the provided BytesRef)
        ///  previously returned by compressString back into a String
        /// </summary>
        public static string DecompressString(BytesRef bytes)
        {
            return DecompressString(bytes.Bytes, bytes.Offset, bytes.Length);
        }
    }
}
