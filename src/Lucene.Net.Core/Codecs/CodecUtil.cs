using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Diagnostics;

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
    /// <p>
    /// Writing codec headers is useful to ensure that a file is in
    /// the format you think it is.
    ///
    /// @lucene.experimental
    /// </summary>

    public sealed class CodecUtil
    {
        private CodecUtil() // no instance
        {
        }

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
        /// identify the file and a version number. this header can
        /// be parsed and validated with
        /// <seealso cref="#checkHeader(DataInput, String, int, int) checkHeader()"/>.
        /// <p>
        /// CodecHeader --&gt; Magic,CodecName,Version
        /// <ul>
        ///    <li>Magic --&gt; <seealso cref="DataOutput#writeInt Uint32"/>. this
        ///        identifies the start of the header. It is always {@value #CODEC_MAGIC}.
        ///    <li>CodecName --&gt; <seealso cref="DataOutput#writeString String"/>. this
        ///        is a string to identify this file.
        ///    <li>Version --&gt; <seealso cref="DataOutput#writeInt Uint32"/>. Records
        ///        the version of the file.
        /// </ul>
        /// <p>
        /// Note that the length of a codec header depends only upon the
        /// name of the codec, so this length can be computed at any time
        /// with <seealso cref="#headerLength(String)"/>.
        /// </summary>
        /// <param name="out"> Output stream </param>
        /// <param name="codec"> String to identify this file. It should be simple ASCII,
        ///              less than 128 characters in length. </param>
        /// <param name="version"> Version number </param>
        /// <exception cref="IOException"> If there is an I/O error writing to the underlying medium. </exception>
        public static void WriteHeader(DataOutput @out, string codec, int version)
        {
            BytesRef bytes = new BytesRef(codec);
            if (bytes.Length != codec.Length || bytes.Length >= 128)
            {
                throw new System.ArgumentException("codec must be simple ASCII, less than 128 characters in length [got " + codec + "]");
            }
            @out.WriteInt(CODEC_MAGIC);
            @out.WriteString(codec);
            @out.WriteInt(version);
        }

        /// <summary>
        /// Computes the length of a codec header.
        /// </summary>
        /// <param name="codec"> Codec name. </param>
        /// <returns> length of the entire codec header. </returns>
        /// <seealso cref= #writeHeader(DataOutput, String, int) </seealso>
        public static int HeaderLength(string codec)
        {
            return 9 + codec.Length;
        }

        /// <summary>
        /// Reads and validates a header previously written with
        /// <seealso cref="#writeHeader(DataOutput, String, int)"/>.
        /// <p>
        /// When reading a file, supply the expected <code>codec</code> and
        /// an expected version range (<code>minVersion to maxVersion</code>).
        /// </summary>
        /// <param name="in"> Input stream, positioned at the point where the
        ///        header was previously written. Typically this is located
        ///        at the beginning of the file. </param>
        /// <param name="codec"> The expected codec name. </param>
        /// <param name="minVersion"> The minimum supported expected version number. </param>
        /// <param name="maxVersion"> The maximum supported expected version number. </param>
        /// <returns> The actual version found, when a valid header is found
        ///         that matches <code>codec</code>, with an actual version
        ///         where <code>minVersion <= actual <= maxVersion</code>.
        ///         Otherwise an exception is thrown. </returns>
        /// <exception cref="CorruptIndexException"> If the first four bytes are not
        ///         <seealso cref="#CODEC_MAGIC"/>, or if the actual codec found is
        ///         not <code>codec</code>. </exception>
        /// <exception cref="IndexFormatTooOldException"> If the actual version is less
        ///         than <code>minVersion</code>. </exception>
        /// <exception cref="IndexFormatTooNewException"> If the actual version is greater
        ///         than <code>maxVersion</code>. </exception>
        /// <exception cref="IOException"> If there is an I/O error reading from the underlying medium. </exception>
        /// <seealso cref= #writeHeader(DataOutput, String, int) </seealso>
        public static int CheckHeader(DataInput @in, string codec, int minVersion, int maxVersion)
        {
            // Safety to guard against reading a bogus string:
            int actualHeader = @in.ReadInt();
            if (actualHeader != CODEC_MAGIC)
            {
                throw new System.IO.IOException("codec header mismatch: actual header=" + actualHeader + " vs expected header=" + CODEC_MAGIC + " (resource: " + @in + ")");
            }
            return CheckHeaderNoMagic(@in, codec, minVersion, maxVersion);
        }

        /// <summary>
        /// Like {@link
        ///  #checkHeader(DataInput,String,int,int)} except this
        ///  version assumes the first int has already been read
        ///  and validated from the input.
        /// </summary>
        public static int CheckHeaderNoMagic(DataInput @in, string codec, int minVersion, int maxVersion)
        {
            string actualCodec = @in.ReadString();
            if (!actualCodec.Equals(codec))
            {
                throw new System.IO.IOException("codec mismatch: actual codec=" + actualCodec + " vs expected codec=" + codec + " (resource: " + @in + ")");
            }

            int actualVersion = @in.ReadInt();
            if (actualVersion < minVersion)
            {
                throw new System.IO.IOException("Version: " + actualVersion + " is not supported. Minimum Version number is " + minVersion + ".");
            }
            if (actualVersion > maxVersion)
            {
                throw new System.IO.IOException("Version: " + actualVersion + " is not supported. Maximum Version number is " + maxVersion + ".");
            }

            return actualVersion;
        }

        /// <summary>
        /// Writes a codec footer, which records both a checksum
        /// algorithm ID and a checksum. this footer can
        /// be parsed and validated with
        /// <seealso cref="#checkFooter(ChecksumIndexInput) checkFooter()"/>.
        /// <p>
        /// CodecFooter --&gt; Magic,AlgorithmID,Checksum
        /// <ul>
        ///    <li>Magic --&gt; <seealso cref="DataOutput#writeInt Uint32"/>. this
        ///        identifies the start of the footer. It is always {@value #FOOTER_MAGIC}.
        ///    <li>AlgorithmID --&gt; <seealso cref="DataOutput#writeInt Uint32"/>. this
        ///        indicates the checksum algorithm used. Currently this is always 0,
        ///        for zlib-crc32.
        ///    <li>Checksum --&gt; <seealso cref="DataOutput#writeLong Uint32"/>. The
        ///        actual checksum value for all previous bytes in the stream, including
        ///        the bytes from Magic and AlgorithmID.
        /// </ul>
        /// </summary>
        /// <param name="out"> Output stream </param>
        /// <exception cref="IOException"> If there is an I/O error writing to the underlying medium. </exception>
        public static void WriteFooter(IndexOutput @out)
        {
            @out.WriteInt(FOOTER_MAGIC);
            @out.WriteInt(0);
            @out.WriteLong(@out.Checksum);
        }

        /// <summary>
        /// Computes the length of a codec footer.
        /// </summary>
        /// <returns> length of the entire codec footer. </returns>
        /// <seealso cref= #writeFooter(IndexOutput) </seealso>
        public static int FooterLength()
        {
            return 16;
        }

        /// <summary>
        /// Validates the codec footer previously written by <seealso cref="#writeFooter"/>. </summary>
        /// <returns> actual checksum value </returns>
        /// <exception cref="IOException"> if the footer is invalid, if the checksum does not match,
        ///                     or if {@code in} is not properly positioned before the footer
        ///                     at the end of the stream. </exception>
        public static long CheckFooter(ChecksumIndexInput @in)
        {
            ValidateFooter(@in);
            long actualChecksum = @in.Checksum;
            long expectedChecksum = @in.ReadLong();
            if (expectedChecksum != actualChecksum)
            {
                throw new System.IO.IOException("checksum failed (hardware problem?) : expected=" + expectedChecksum.ToString("x") + " actual=" + actualChecksum.ToString("x") + " (resource=" + @in + ")");
            }
            if (@in.FilePointer != @in.Length())
            {
                throw new System.IO.IOException("did not read all bytes from file: read " + @in.FilePointer + " vs size " + @in.Length() + " (resource: " + @in + ")");
            }
            return actualChecksum;
        }

        /// <summary>
        /// Returns (but does not validate) the checksum previously written by <seealso cref="#checkFooter"/>. </summary>
        /// <returns> actual checksum value </returns>
        /// <exception cref="IOException"> if the footer is invalid </exception>
        public static long RetrieveChecksum(IndexInput @in)
        {
            @in.Seek(@in.Length() - FooterLength());
            ValidateFooter(@in);
            return @in.ReadLong();
        }

        private static void ValidateFooter(IndexInput @in)
        {
            int magic = @in.ReadInt();
            if (magic != FOOTER_MAGIC)
            {
                throw new System.IO.IOException("codec footer mismatch: actual footer=" + magic + " vs expected footer=" + FOOTER_MAGIC + " (resource: " + @in + ")");
            }

            int algorithmID = @in.ReadInt();
            if (algorithmID != 0)
            {
                throw new System.IO.IOException("codec footer mismatch: unknown algorithmID: " + algorithmID);
            }
        }

        /// <summary>
        /// Checks that the stream is positioned at the end, and throws exception
        /// if it is not. </summary>
        /// @deprecated Use <seealso cref="#checkFooter"/> instead, this should only used for files without checksums
        [Obsolete("Use CheckFooter() instead")]
        public static void CheckEOF(IndexInput @in)
        {
            if (@in.FilePointer != @in.Length())
            {
                throw new System.IO.IOException("did not read all bytes from file: read " + @in.FilePointer + " vs size " + @in.Length() + " (resource: " + @in + ")");
            }
        }

        /// <summary>
        /// Clones the provided input, reads all bytes from the file, and calls <seealso cref="#checkFooter"/>
        /// <p>
        /// Note that this method may be slow, as it must process the entire file.
        /// If you just need to extract the checksum value, call <seealso cref="#retrieveChecksum"/>.
        /// </summary>
        public static long ChecksumEntireFile(IndexInput input)
        {
            IndexInput clone = (IndexInput)input.Clone();
            clone.Seek(0);
            ChecksumIndexInput @in = new BufferedChecksumIndexInput(clone);
            Debug.Assert(@in.FilePointer == 0);
            @in.Seek(@in.Length() - FooterLength());
            return CheckFooter(@in);
        }
    }
}