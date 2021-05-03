using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs
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
    /// Utility class for reading and writing versioned headers.
    /// <para/>
    /// Writing codec headers is useful to ensure that a file is in
    /// the format you think it is.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public static class CodecUtil // LUCENENET specific - marked static because all members are static
    {
        /// <summary>
        /// Constant to identify the start of a codec header.
        /// </summary>
        public static readonly int CODEC_MAGIC = 0x3fd76c17;

        /// <summary>
        /// Constant to identify the start of a codec footer.
        /// </summary>
        public static readonly int FOOTER_MAGIC = ~CODEC_MAGIC;

        /// <summary>
        /// Writes a codec header, which records both a string to
        /// identify the file and a version number. This header can
        /// be parsed and validated with
        /// <see cref="CheckHeader(DataInput, string, int, int)"/>.
        /// <para/>
        /// CodecHeader --&gt; Magic,CodecName,Version
        /// <list type="bullet">
        ///    <item><description>Magic --&gt; Uint32 (<see cref="DataOutput.WriteInt32(int)"/>). this
        ///        identifies the start of the header. It is always <see cref="CODEC_MAGIC"/>.</description></item>
        ///    <item><description>CodecName --&gt; String (<see cref="DataOutput.WriteString(string)"/>). this
        ///        is a string to identify this file.</description></item>
        ///    <item><description>Version --&gt; Uint32 (<see cref="DataOutput.WriteInt32(int)"/>). Records
        ///        the version of the file.</description></item>
        /// </list>
        /// <para/>
        /// Note that the length of a codec header depends only upon the
        /// name of the codec, so this length can be computed at any time
        /// with <see cref="HeaderLength(string)"/>.
        /// </summary>
        /// <param name="out"> Output stream </param>
        /// <param name="codec"> String to identify this file. It should be simple ASCII,
        ///              less than 128 characters in length. </param>
        /// <param name="version"> Version number </param>
        /// <exception cref="IOException"> If there is an I/O error writing to the underlying medium. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHeader(DataOutput @out, string codec, int version)
        {
            BytesRef bytes = new BytesRef(codec);
            if (bytes.Length != codec.Length || bytes.Length >= 128)
            {
                throw new ArgumentException("codec must be simple ASCII, less than 128 characters in length [got " + codec + "]");
            }
            @out.WriteInt32(CODEC_MAGIC);
            @out.WriteString(codec);
            @out.WriteInt32(version);
        }

        /// <summary>
        /// Computes the length of a codec header.
        /// </summary>
        /// <param name="codec"> Codec name. </param>
        /// <returns> Length of the entire codec header. </returns>
        /// <seealso cref="WriteHeader(DataOutput, string, int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HeaderLength(string codec)
        {
            return 9 + codec.Length;
        }

        /// <summary>
        /// Reads and validates a header previously written with
        /// <see cref="WriteHeader(DataOutput, string, int)"/>.
        /// <para/>
        /// When reading a file, supply the expected <paramref name="codec"/> and
        /// an expected version range (<paramref name="minVersion"/> to <paramref name="maxVersion"/>).
        /// </summary>
        /// <param name="in"> Input stream, positioned at the point where the
        ///        header was previously written. Typically this is located
        ///        at the beginning of the file. </param>
        /// <param name="codec"> The expected codec name. </param>
        /// <param name="minVersion"> The minimum supported expected version number. </param>
        /// <param name="maxVersion"> The maximum supported expected version number. </param>
        /// <returns> The actual version found, when a valid header is found
        ///         that matches <paramref name="codec"/>, with an actual version
        ///         where <c>minVersion &lt;= actual &lt;= maxVersion</c>.
        ///         Otherwise an exception is thrown. </returns>
        /// <exception cref="Index.CorruptIndexException"> If the first four bytes are not
        ///         <see cref="CODEC_MAGIC"/>, or if the actual codec found is
        ///         not <paramref name="codec"/>. </exception>
        /// <exception cref="Index.IndexFormatTooOldException"> If the actual version is less
        ///         than <paramref name="minVersion"/>. </exception>
        /// <exception cref="Index.IndexFormatTooNewException"> If the actual version is greater
        ///         than <paramref name="maxVersion"/>. </exception>
        /// <exception cref="IOException"> If there is an I/O error reading from the underlying medium. </exception>
        /// <seealso cref="WriteHeader(DataOutput, string, int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CheckHeader(DataInput @in, string codec, int minVersion, int maxVersion)
        {
            // Safety to guard against reading a bogus string:
            int actualHeader = @in.ReadInt32();
            if (actualHeader != CODEC_MAGIC)
            {
                throw new IOException("codec header mismatch: actual header=" + actualHeader + " vs expected header=" + CODEC_MAGIC + " (resource: " + @in + ")");
            }
            return CheckHeaderNoMagic(@in, codec, minVersion, maxVersion);
        }

        /// <summary>
        /// Like 
        /// <see cref="CheckHeader(DataInput,string,int,int)"/> except this
        /// version assumes the first <see cref="int"/> has already been read
        /// and validated from the input.
        /// </summary>
        public static int CheckHeaderNoMagic(DataInput @in, string codec, int minVersion, int maxVersion)
        {
            string actualCodec = @in.ReadString();
            if (!actualCodec.Equals(codec, StringComparison.Ordinal))
            {
                throw new IOException("codec mismatch: actual codec=" + actualCodec + " vs expected codec=" + codec + " (resource: " + @in + ")");
            }

            int actualVersion = @in.ReadInt32();
            if (actualVersion < minVersion)
            {
                throw new IOException("Version: " + actualVersion + " is not supported. Minimum Version number is " + minVersion + ".");
            }
            if (actualVersion > maxVersion)
            {
                throw new IOException("Version: " + actualVersion + " is not supported. Maximum Version number is " + maxVersion + ".");
            }

            return actualVersion;
        }

        /// <summary>
        /// Writes a codec footer, which records both a checksum
        /// algorithm ID and a checksum. This footer can
        /// be parsed and validated with
        /// <see cref="CheckFooter(ChecksumIndexInput)"/>.
        /// <para/>
        /// CodecFooter --&gt; Magic,AlgorithmID,Checksum
        /// <list type="bullet">
        ///    <item><description>Magic --&gt; Uint32 (<see cref="DataOutput.WriteInt32(int)"/>). this
        ///        identifies the start of the footer. It is always <see cref="FOOTER_MAGIC"/>.</description></item>
        ///    <item><description>AlgorithmID --&gt; Uint32 (<see cref="DataOutput.WriteInt32(int)"/>). this
        ///        indicates the checksum algorithm used. Currently this is always 0,
        ///        for zlib-crc32.</description></item>
        ///    <item><description>Checksum --&gt; Uint32 (<see cref="DataOutput.WriteInt64(long)"/>). The
        ///        actual checksum value for all previous bytes in the stream, including
        ///        the bytes from Magic and AlgorithmID.</description></item>
        /// </list>
        /// </summary>
        /// <param name="out"> Output stream </param>
        /// <exception cref="IOException"> If there is an I/O error writing to the underlying medium. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFooter(IndexOutput @out)
        {
            @out.WriteInt32(FOOTER_MAGIC);
            @out.WriteInt32(0);
            @out.WriteInt64(@out.Checksum);
        }

        /// <summary>
        /// Computes the length of a codec footer.
        /// </summary>
        /// <returns> Length of the entire codec footer. </returns>
        /// <seealso cref="WriteFooter(IndexOutput)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FooterLength()
        {
            return 16;
        }

        /// <summary>
        /// Validates the codec footer previously written by <see cref="WriteFooter(IndexOutput)"/>. </summary>
        /// <returns> Actual checksum value. </returns>
        /// <exception cref="IOException"> If the footer is invalid, if the checksum does not match,
        ///                     or if <paramref name="in"/> is not properly positioned before the footer
        ///                     at the end of the stream. </exception>
        public static long CheckFooter(ChecksumIndexInput @in)
        {
            ValidateFooter(@in);
            long actualChecksum = @in.Checksum;
            long expectedChecksum = @in.ReadInt64();
            if (expectedChecksum != actualChecksum)
            {
                throw new IOException("checksum failed (hardware problem?) : expected=" + expectedChecksum.ToString("x") + " actual=" + actualChecksum.ToString("x") + " (resource=" + @in + ")");
            }
            if (@in.Position != @in.Length) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            {
                throw new IOException("did not read all bytes from file: read " + @in.Position + " vs size " + @in.Length + " (resource: " + @in + ")"); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            return actualChecksum;
        }

        /// <summary>
        /// Returns (but does not validate) the checksum previously written by <see cref="CheckFooter(ChecksumIndexInput)"/>. </summary>
        /// <returns> actual checksum value </returns>
        /// <exception cref="IOException"> If the footer is invalid. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long RetrieveChecksum(IndexInput @in)
        {
            @in.Seek(@in.Length - FooterLength());
            ValidateFooter(@in);
            return @in.ReadInt64();
        }

        private static void ValidateFooter(IndexInput @in)
        {
            int magic = @in.ReadInt32();
            if (magic != FOOTER_MAGIC)
            {
                throw new IOException("codec footer mismatch: actual footer=" + magic + " vs expected footer=" + FOOTER_MAGIC + " (resource: " + @in + ")");
            }

            int algorithmID = @in.ReadInt32();
            if (algorithmID != 0)
            {
                throw new IOException("codec footer mismatch: unknown algorithmID: " + algorithmID);
            }
        }

        /// <summary>
        /// Checks that the stream is positioned at the end, and throws exception
        /// if it is not. </summary>
        [Obsolete("Use CheckFooter(ChecksumIndexInput) instead, this should only used for files without checksums.")]
        public static void CheckEOF(IndexInput @in)
        {
            if (@in.Position != @in.Length) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            {
                throw new IOException("did not read all bytes from file: read " + @in.Position + " vs size " + @in.Length + " (resource: " + @in + ")");
            }
        }

        /// <summary>
        /// Clones the provided input, reads all bytes from the file, and calls <see cref="CheckFooter(ChecksumIndexInput)"/>
        /// <para/>
        /// Note that this method may be slow, as it must process the entire file.
        /// If you just need to extract the checksum value, call <see cref="RetrieveChecksum(IndexInput)"/>.
        /// </summary>
        public static long ChecksumEntireFile(IndexInput input)
        {
            IndexInput clone = (IndexInput)input.Clone();
            clone.Seek(0);
            ChecksumIndexInput @in = new BufferedChecksumIndexInput(clone);
            if (Debugging.AssertsEnabled) Debugging.Assert(@in.Position == 0); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            @in.Seek(@in.Length - FooterLength());
            return CheckFooter(@in);
        }
    }
}