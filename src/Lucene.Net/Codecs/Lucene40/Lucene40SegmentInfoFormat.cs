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

    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Lucene 4.0 Segment info format.
    /// <para>
    /// Files:
    /// <list type="bullet">
    ///   <item><description><tt>.si</tt>: Header, SegVersion, SegSize, IsCompoundFile, Diagnostics, Attributes, Files</description></item>
    /// </list>
    /// </para>
    /// Data types:
    /// <para>
    /// <list type="bullet">
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///   <item><description>SegSize --&gt; Int32 (<see cref="Store.DataOutput.WriteInt32(int)"/>) </description></item>
    ///   <item><description>SegVersion --&gt; String (<see cref="Store.DataOutput.WriteString(string)"/>) </description></item>
    ///   <item><description>Files --&gt; ISet&lt;String&gt; (<see cref="Store.DataOutput.WriteStringSet(System.Collections.Generic.ISet{string})"/>) </description></item>
    ///   <item><description>Diagnostics, Attributes --&gt; IDictionary&lt;String,String&gt; (<see cref="Store.DataOutput.WriteStringStringMap(System.Collections.Generic.IDictionary{string, string})"/>) </description></item>
    ///   <item><description>IsCompoundFile --&gt; Int8 (<see cref="Store.DataOutput.WriteByte(byte)"/>) </description></item>
    /// </list>
    /// </para>
    /// Field Descriptions:
    /// <para>
    /// <list type="bullet">
    ///   <item><description>SegVersion is the code version that created the segment.</description></item>
    ///   <item><description>SegSize is the number of documents contained in the segment index.</description></item>
    ///   <item><description>IsCompoundFile records whether the segment is written as a compound file or
    ///       not. If this is -1, the segment is not a compound file. If it is 1, the segment
    ///       is a compound file.</description></item>
    ///   <item><description>Checksum contains the CRC32 checksum of all bytes in the segments_N file up
    ///       until the checksum. This is used to verify integrity of the file on opening the
    ///       index.</description></item>
    ///   <item><description>The Diagnostics Map is privately written by <see cref="Index.IndexWriter"/>, as a debugging aid,
    ///       for each segment it creates. It includes metadata like the current Lucene
    ///       version, OS, .NET/Java version, why the segment was created (merge, flush,
    ///       addIndexes), etc.</description></item>
    ///   <item><description>Attributes: a key-value map of codec-private attributes.</description></item>
    ///   <item><description>Files is a list of files referred to by this segment.</description></item>
    /// </list>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Index.SegmentInfos"/>
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

        public override SegmentInfoReader SegmentInfoReader => reader;

        // we must unfortunately support write, to allow addIndexes to write a new .si with rewritten filenames:
        // see LUCENE-5377
        public override SegmentInfoWriter SegmentInfoWriter => writer;

        /// <summary>
        /// File extension used to store <see cref="SegmentInfo"/>. </summary>
        public const string SI_EXTENSION = "si";

        internal const string CODEC_NAME = "Lucene40SegmentInfo";
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;
    }
}