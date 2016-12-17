namespace Lucene.Net.Codecs.Lucene46
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
    /// Lucene 4.6 Segment info format.
    /// <p>
    /// Files:
    /// <ul>
    ///   <li><tt>.si</tt>: Header, SegVersion, SegSize, IsCompoundFile, Diagnostics, Files, Footer
    /// </ul>
    /// </p>
    /// Data types:
    /// <p>
    /// <ul>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///   <li>SegSize --&gt; <seealso cref="DataOutput#writeInt Int32"/></li>
    ///   <li>SegVersion --&gt; <seealso cref="DataOutput#writeString String"/></li>
    ///   <li>Files --&gt; <seealso cref="DataOutput#writeStringSet Set&lt;String&gt;"/></li>
    ///   <li>Diagnostics --&gt; <seealso cref="DataOutput#writeStringStringMap Map&lt;String,String&gt;"/></li>
    ///   <li>IsCompoundFile --&gt; <seealso cref="DataOutput#writeByte Int8"/></li>
    ///   <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
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
    ///   <li>The Diagnostics Map is privately written by <seealso cref="IndexWriter"/>, as a debugging aid,
    ///       for each segment it creates. It includes metadata like the current Lucene
    ///       version, OS, Java version, why the segment was created (merge, flush,
    ///       addIndexes), etc.</li>
    ///   <li>Files is a list of files referred to by this segment.</li>
    /// </ul>
    /// </p>
    /// </summary>
    /// <seealso cref= SegmentInfos
    /// @lucene.experimental </seealso>
    public class Lucene46SegmentInfoFormat : SegmentInfoFormat
    {
        private readonly SegmentInfoReader Reader = new Lucene46SegmentInfoReader();
        private readonly SegmentInfoWriter Writer = new Lucene46SegmentInfoWriter();

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene46SegmentInfoFormat()
        {
        }

        public override SegmentInfoReader SegmentInfoReader
        {
            get
            {
                return Reader;
            }
        }

        public override SegmentInfoWriter SegmentInfoWriter
        {
            get
            {
                return Writer;
            }
        }

        /// <summary>
        /// File extension used to store <seealso cref="SegmentInfo"/>. </summary>
        public readonly static string SI_EXTENSION = "si";

        internal const string CODEC_NAME = "Lucene46SegmentInfo";
        internal const int VERSION_START = 0;
        internal const int VERSION_CHECKSUM = 1;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;
    }
}