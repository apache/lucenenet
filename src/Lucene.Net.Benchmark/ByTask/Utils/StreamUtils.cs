using ICSharpCode.SharpZipLib.BZip2;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Lucene.Net.Benchmarks.ByTask.Utils
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
    /// Stream utilities.
    /// </summary>
    public static class StreamUtils // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>Buffer size used across the benchmark package</summary>
        public static readonly int BUFFER_SIZE = 1 << 16; // 64K

        // LUCENENET specific - de-nested Type and renamed FileType

        private static readonly IDictionary<string, FileType?> extensionToType = new Dictionary<string, FileType?>() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            // these in are lower case, we will lower case at the test as well
            { ".bz2", FileType.BZIP2 },
            { ".bzip", FileType.BZIP2 },
            { ".gz", FileType.GZIP },
            { ".gzip", FileType.GZIP },
        };

        /// <summary>
        /// Returns an <see cref="Stream"/> over the requested file. This method
        /// attempts to identify the appropriate <see cref="Stream"/> instance to return
        /// based on the file name (e.g., if it ends with .bz2 or .bzip, return a
        /// 'bzip' <see cref="Stream"/>).
        /// </summary>
        public static Stream GetInputStream(FileInfo file)
        {
            // First, create a FileInputStream, as this will be required by all types.
            // Wrap with BufferedInputStream for better performance
            Stream @in = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
            return GetFileType(file).GetInputStream(@in);
        }

        /// <summary>Return the type of the file, or <c>null</c> if unknown.</summary>
        private static FileType GetFileType(FileInfo file)
        {
            FileType? type = null;
            string fileName = file.Name;
            int idx = fileName.LastIndexOf('.');
            if (idx != -1)
            {
                extensionToType.TryGetValue(fileName.Substring(idx).ToLowerInvariant(), out type);
            }
            return type ?? FileType.PLAIN ;
        }

        /// <summary>
        /// Returns an <see cref="Stream"/> over the requested file, identifying
        /// the appropriate <see cref="Stream"/> instance similar to <see cref="GetInputStream(FileInfo)"/>.
        /// </summary>
        public static Stream GetOutputStream(FileInfo file)
        {
            // First, create a FileInputStream, as this will be required by all types.
            // Wrap with BufferedInputStream for better performance
            Stream os = new FileStream(file.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            return GetFileType(file).GetOutputStream(os);
        }
    }

    /// <summary>File format type.</summary>
    public enum FileType
    {
        /// <summary>
        /// BZIP2 is automatically used for <b>.bz2</b> and <b>.bzip2</b> extensions.
        /// </summary>
        BZIP2,

        /// <summary>
        /// GZIP is automatically used for <b>.gz</b> and <b>.gzip</b> extensions.
        /// </summary>
        GZIP,

        /// <summary>
        /// Plain text is used for anything which is not GZIP or BZIP.
        /// </summary>
        PLAIN
    }

    internal static class FileTypeExtensions
    {
        public static Stream GetInputStream(this FileType fileType, Stream input)
        {
            switch (fileType)
            {
                case FileType.BZIP2:
                    return new BZip2InputStream(input);
                case FileType.GZIP:
                    return new GZipStream(input, CompressionMode.Decompress); 
                default:
                    return input;
            }
        }

        public static Stream GetOutputStream(this FileType fileType, Stream output)
        {
            switch (fileType)
            {
                case FileType.BZIP2:
                    return new BZip2OutputStream(output);
                case FileType.GZIP:
                    return new GZipStream(output, CompressionMode.Compress);
                default:
                    return output;
            }
        }
    }
}
