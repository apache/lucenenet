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

/// To enable compression support in Lucene.Net 1.9 using .NET 1.1. Framework,
/// you will need to define 'SHARP_ZIP_LIB' and referance the SharpLibZip 
/// library.  The SharpLibZip library can be downloaded from: 
/// http://www.icsharpcode.net/OpenSource/SharpZipLib/
/// 
/// You can use any other cmpression library you have by plugging it into this
/// code or by providing your own adapter as in this one.


#if SHARP_ZIP_LIB

using System;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System.IO;

namespace Lucene.Net.Index.Compression
{
    public class SharpZipLibAdapter : SupportClass.CompressionSupport.ICompressionAdapter
    {
        public byte[] Compress(byte[] input, int offset, int length)
        {
            // Create the compressor with highest level of compression
            Deflater compressor = new Deflater();
            compressor.SetLevel(Deflater.BEST_COMPRESSION);

            // Give the compressor the data to compress
            compressor.SetInput(input, offset, length);
            compressor.Finish();

            /*
             * Create an expandable byte array to hold the compressed data.
             * You cannot use an array that's the same size as the orginal because
             * there is no guarantee that the compressed data will be smaller than
             * the uncompressed data.
             */
            MemoryStream bos = new MemoryStream(input.Length);

            // Compress the data
            byte[] buf = new byte[1024];
            while (!compressor.IsFinished)
            {
                int count = compressor.Deflate(buf);
                bos.Write(buf, 0, count);
            }

            // Get the compressed data
            return bos.ToArray();
        }

        public byte[] Uncompress(byte[] input)
        {
            Inflater decompressor = new Inflater();
            decompressor.SetInput(input);

            // Create an expandable byte array to hold the decompressed data
            MemoryStream bos = new MemoryStream(input.Length);

            // Decompress the data
            byte[] buf = new byte[1024];
            while (!decompressor.IsFinished)
            {
                int count = decompressor.Inflate(buf);
                bos.Write(buf, 0, count);
            }

            // Get the decompressed data
            return bos.ToArray();
        }
    }
}

#endif