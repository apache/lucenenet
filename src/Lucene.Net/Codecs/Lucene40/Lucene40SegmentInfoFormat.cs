using System;

namespace Lucene.Net.Codecs.Lucene40
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

    // javadocs
    using SegmentInfo = Lucene.Net.Index.SegmentInfo; // javadocs

    // javadocs
    // javadocs

    /// <summary>
    /// Lucene 4.0 Segment info format.
    /// <p>
    /// Files:
    /// <ul>
    ///   <li><tt>.si</tt>: Header, SegVersion, SegSize, IsCompoundFile, Diagnostics, Attributes, Files
    /// </ul>
    /// </p>
    /// Data types:
    /// <p>
    /// <ul>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///   <li>SegSize --&gt; <seealso cref="DataOutput#writeInt Int32"/></li>
    ///   <li>SegVersion --&gt; <seealso cref="DataOutput#writeString String"/></li>
    ///   <li>Files --&gt; <seealso cref="DataOutput#writeStringSet Set&lt;String&gt;"/></li>
    ///   <li>Diagnostics, Attributes --&gt; <seealso cref="DataOutput#writeStringStringMap Map&lt;String,String&gt;"/></li>
    ///   <li>IsCompoundFile --&gt; <seealso cref="DataOutput#writeByte Int8"/></li>
    /// </ul>
    /// </p>
    /// Field Descriptions:
    /// <p>
    /// <ul>
    ///   <li>SegVersion is the code version that created the segment.</li>
    ///   <li>SegSize is the number of documents contained in the segment index.</li>
    ///   <li>IsCompoundFile records whether the segment is written as a compound file or
    ///       not. If this is -1, the segment is not a compound file. If it is 1, the segment
    ///       is a compound file.</li>
    ///   <li>Checksum contains the CRC32 checksum of all bytes in the segments_N file up
    ///       until the checksum. this is used to verify integrity of the file on opening the
    ///       index.</li>
    ///   <li>The Diagnostics Map is privately written by <seealso cref="IndexWriter"/>, as a debugging aid,
    ///       for each segment it creates. It includes metadata like the current Lucene
    ///       version, OS, Java version, why the segment was created (merge, flush,
    ///       addIndexes), etc.</li>
    ///   <li>Attributes: a key-value map of codec-private attributes.</li>
    ///   <li>Files is a list of files referred to by this segment.</li>
    /// </ul>
    /// </p>
    /// </summary>
    /// <seealso cref= SegmentInfos
    /// @lucene.experimental </seealso>
    /// @deprecated Only for reading old 4.0-4.5 segments, and supporting IndexWriter.addIndexes
    [Obsolete("Only for reading old 4.0-4.5 segments, and supporting IndexWriter.AddIndexes()")]
    public class Lucene40SegmentInfoFormat : SegmentInfoFormat
    {
        private readonly SegmentInfoReader reader = new Lucene40SegmentInfoReader();
        private readonly SegmentInfoWriter writer = new Lucene40SegmentInfoWriter();

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40SegmentInfoFormat()
        {
        }

        public override SegmentInfoReader SegmentInfoReader
        {
            get
            {
                return reader;
            }
        }

        // we must unfortunately support write, to allow addIndexes to write a new .si with rewritten filenames:
        // see LUCENE-5377
        public override SegmentInfoWriter SegmentInfoWriter
        {
            get
            {
                return writer;
            }
        }

        /// <summary>
        /// File extension used to store <seealso cref="SegmentInfo"/>. </summary>
        public readonly static string SI_EXTENSION = "si";

        internal readonly static string CODEC_NAME = "Lucene40SegmentInfo";
        internal readonly static int VERSION_START = 0;
        internal readonly static int VERSION_CURRENT = VERSION_START;
    }
}