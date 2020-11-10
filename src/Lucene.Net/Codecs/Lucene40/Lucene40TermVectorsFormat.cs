using System.Runtime.CompilerServices;

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

    using Directory = Lucene.Net.Store.Directory;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IOContext = Lucene.Net.Store.IOContext;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Lucene 4.0 Term Vectors format.
    /// <para>Term Vector support is an optional on a field by field basis. It consists of
    /// 3 files.</para>
    /// <list type="number">
    /// <item><description><a name="tvx" id="tvx"></a>
    /// <para>The Document Index or .tvx file.</para>
    /// <para>For each document, this stores the offset into the document data (.tvd) and
    /// field data (.tvf) files.</para>
    /// <para>DocumentIndex (.tvx) --&gt; Header,&lt;DocumentPosition,FieldPosition&gt;
    /// <sup>NumDocs</sup></para>
    /// <list type="bullet">
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///   <item><description>DocumentPosition --&gt; UInt64 (<see cref="Store.DataOutput.WriteInt64(long)"/>)  (offset in the .tvd file)</description></item>
    ///   <item><description>FieldPosition --&gt; UInt64 (<see cref="Store.DataOutput.WriteInt64(long)"/>)  (offset in the .tvf file)</description></item>
    /// </list>
    /// </description></item>
    /// <item><description><a name="tvd" id="tvd"></a>
    /// <para>The Document or .tvd file.</para>
    /// <para>This contains, for each document, the number of fields, a list of the fields
    /// with term vector info and finally a list of pointers to the field information
    /// in the .tvf (Term Vector Fields) file.</para>
    /// <para>The .tvd file is used to map out the fields that have term vectors stored
    /// and where the field information is in the .tvf file.</para>
    /// <para>Document (.tvd) --&gt; Header,&lt;NumFields, FieldNums,
    /// FieldPositions&gt; <sup>NumDocs</sup></para>
    /// <list type="bullet">
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///   <item><description>NumFields --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>FieldNums --&gt; &lt;FieldNumDelta&gt; <sup>NumFields</sup></description></item>
    ///   <item><description>FieldNumDelta --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>FieldPositions --&gt; &lt;FieldPositionDelta&gt; <sup>NumFields-1</sup></description></item>
    ///   <item><description>FieldPositionDelta --&gt; VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </description></item>
    /// </list>
    /// </description></item>
    /// <item><description><a name="tvf" id="tvf"></a>
    /// <para>The Field or .tvf file.</para>
    /// <para>This file contains, for each field that has a term vector stored, a list of
    /// the terms, their frequencies and, optionally, position, offset, and payload
    /// information.</para>
    /// <para>Field (.tvf) --&gt; Header,&lt;NumTerms, Flags, TermFreqs&gt;
    /// <sup>NumFields</sup></para>
    /// <list type="bullet">
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///   <item><description>NumTerms --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>Flags --&gt; Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) </description></item>
    ///   <item><description>TermFreqs --&gt; &lt;TermText, TermFreq, Positions?, PayloadData?, Offsets?&gt;
    ///       <sup>NumTerms</sup></description></item>
    ///   <item><description>TermText --&gt; &lt;PrefixLength, Suffix&gt;</description></item>
    ///   <item><description>PrefixLength --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>Suffix --&gt; String (<see cref="Store.DataOutput.WriteString(string)"/>) </description></item>
    ///   <item><description>TermFreq --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>Positions --&gt; &lt;PositionDelta PayloadLength?&gt;<sup>TermFreq</sup></description></item>
    ///   <item><description>PositionDelta --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>PayloadLength --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>PayloadData --&gt; Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) <sup>NumPayloadBytes</sup></description></item>
    ///   <item><description>Offsets --&gt; &lt;VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>), VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) &gt;<sup>TermFreq</sup></description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    /// <item><description>Flags byte stores whether this term vector has position, offset, payload.
    /// information stored.</description></item>
    /// <item><description>Term byte prefixes are shared. The PrefixLength is the number of initial
    /// bytes from the previous term which must be pre-pended to a term's suffix
    /// in order to form the term's bytes. Thus, if the previous term's text was "bone"
    /// and the term is "boy", the PrefixLength is two and the suffix is "y".</description></item>
    /// <item><description>PositionDelta is, if payloads are disabled for the term's field, the
    /// difference between the position of the current occurrence in the document and
    /// the previous occurrence (or zero, if this is the first occurrence in this
    /// document). If payloads are enabled for the term's field, then PositionDelta/2
    /// is the difference between the current and the previous position. If payloads
    /// are enabled and PositionDelta is odd, then PayloadLength is stored, indicating
    /// the length of the payload at the current term position.</description></item>
    /// <item><description>PayloadData is metadata associated with a term position. If
    /// PayloadLength is stored at the current position, then it indicates the length
    /// of this payload. If PayloadLength is not stored, then this payload has the same
    /// length as the payload at the previous position. PayloadData encodes the
    /// concatenated bytes for all of a terms occurrences.</description></item>
    /// <item><description>Offsets are stored as delta encoded VInts. The first VInt is the
    /// startOffset, the second is the endOffset.</description></item>
    /// </list>
    /// </description></item>
    /// </list>
    /// </summary>
    public class Lucene40TermVectorsFormat : TermVectorsFormat
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40TermVectorsFormat()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            return new Lucene40TermVectorsReader(directory, segmentInfo, fieldInfos, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            return new Lucene40TermVectorsWriter(directory, segmentInfo.Name, context);
        }
    }
}