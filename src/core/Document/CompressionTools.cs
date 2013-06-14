/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


// To enable compression support in Lucene.Net ,
// you will need to define 'SHARP_ZIP_LIB' and reference the SharpLibZip 
// library.  The SharpLibZip library can be downloaded from: 
// http://www.icsharpcode.net/OpenSource/SharpZipLib/

using System;
using Lucene.Net.Support;
using Lucene.Net.Util;
using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

namespace Lucene.Net.Documents
{

    /// <summary>Simple utility class providing static methods to
    /// compress and decompress binary data for stored fields.
    /// This class uses java.util.zip.Deflater and Inflater
    /// classes to compress and decompress.
    /// </summary>
    public static class CompressionTools
    {

        /// <summary>Compresses the specified byte range using the
        /// specified compressionLevel (constants are defined in
        /// java.util.zip.Deflater). 
        /// </summary>
        public static byte[] Compress(sbyte[] value_Renamed, int offset, int length, int compressionLevel)
        {
            /* Create an expandable byte array to hold the compressed data.
            * You cannot use an array that's the same size as the orginal because
            * there is no guarantee that the compressed data will be smaller than
            * the uncompressed data. */
            System.IO.MemoryStream bos = new System.IO.MemoryStream(length);

            Deflater compressor = SharpZipLib.CreateDeflater();

            try
            {
                compressor.SetLevel(compressionLevel);
                compressor.SetInput((byte[])(Array)value_Renamed, offset, length);
                compressor.Finish();

                // Compress the data
                byte[] buf = new byte[1024];
                while (!compressor.IsFinished)
                {
                    int count = compressor.Deflate(buf);
                    bos.Write(buf, 0, count);
                }
            }
            finally
            {
            }

            return bos.ToArray();
        }


        /// <summary>Compresses the specified byte range, with default BEST_COMPRESSION level </summary>
        public static byte[] Compress(sbyte[] value_Renamed, int offset, int length)
        {
            return Compress(value_Renamed, offset, length, Deflater.BEST_COMPRESSION);
        }

        /// <summary>Compresses all bytes in the array, with default BEST_COMPRESSION level </summary>
        public static byte[] Compress(sbyte[] value_Renamed)
        {
            return Compress(value_Renamed, 0, value_Renamed.Length, Deflater.BEST_COMPRESSION);
        }

        /// <summary>Compresses the String value, with default BEST_COMPRESSION level </summary>
        public static byte[] CompressString(String value_Renamed)
        {
            return CompressString(value_Renamed, Deflater.BEST_COMPRESSION);
        }

        public static byte[] CompressString(String value, int compressionLevel)
        {
            BytesRef result = new BytesRef();
            UnicodeUtil.UTF16toUTF8(value, 0, value.Length, result);
            return Compress(result.bytes, 0, result.length, compressionLevel);
        }


        public static byte[] Decompress(BytesRef bytes)
        {
            return Decompress(bytes.bytes, bytes.offset, bytes.length);
        }

        public static byte[] Decompress(sbyte[] value_Renamed, int offset, int length)
        {
            // Create an expandable byte array to hold the decompressed data
            System.IO.MemoryStream bos = new System.IO.MemoryStream(value_Renamed.Length);

            Inflater decompressor = SharpZipLib.CreateInflater();

            try
            {
                decompressor.SetInput((byte[])(Array)value_Renamed);

                // Decompress the data
                byte[] buf = new byte[1024];
                while (!decompressor.IsFinished)
                {
                    int count = decompressor.Inflate(buf);
                    bos.Write(buf, 0, count);
                }
            }
            finally
            {

            }

            return bos.ToArray();
        }

        /** Decompress the byte array previously returned by
         *  compressString back into a String */
        public static String DecompressString(sbyte[] value)
        {
            return DecompressString(value, 0, value.Length);
        }

        /** Decompress the byte array previously returned by
         *  compressString back into a String */
        public static String DecompressString(sbyte[] value, int offset, int length)
        {
            byte[] bytes = Decompress(value, offset, length);
            CharsRef result = new CharsRef(bytes.Length);
            UnicodeUtil.UTF8toUTF16((sbyte[])(Array)bytes, 0, bytes.Length, result);
            return new String(result.chars, 0, result.length);
        }

        /** Decompress the byte array (referenced by the provided BytesRef) 
         *  previously returned by compressString back into a String */
        public static String DecompressString(BytesRef bytes)
        {
            return DecompressString(bytes.bytes, bytes.offset, bytes.length);
        }


    }
}

