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
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IOContext = Lucene.Net.Store.IOContext;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Lucene 4.0 Term Vectors format.
    /// <p>Term Vector support is an optional on a field by field basis. It consists of
    /// 3 files.</p>
    /// <ol>
    /// <li><a name="tvx" id="tvx"></a>
    /// <p>The Document Index or .tvx file.</p>
    /// <p>For each document, this stores the offset into the document data (.tvd) and
    /// field data (.tvf) files.</p>
    /// <p>DocumentIndex (.tvx) --&gt; Header,&lt;DocumentPosition,FieldPosition&gt;
    /// <sup>NumDocs</sup></p>
    /// <ul>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///   <li>DocumentPosition --&gt; <seealso cref="DataOutput#writeLong UInt64"/> (offset in the .tvd file)</li>
    ///   <li>FieldPosition --&gt; <seealso cref="DataOutput#writeLong UInt64"/> (offset in the .tvf file)</li>
    /// </ul>
    /// </li>
    /// <li><a name="tvd" id="tvd"></a>
    /// <p>The Document or .tvd file.</p>
    /// <p>this contains, for each document, the number of fields, a list of the fields
    /// with term vector info and finally a list of pointers to the field information
    /// in the .tvf (Term Vector Fields) file.</p>
    /// <p>The .tvd file is used to map out the fields that have term vectors stored
    /// and where the field information is in the .tvf file.</p>
    /// <p>Document (.tvd) --&gt; Header,&lt;NumFields, FieldNums,
    /// FieldPositions&gt; <sup>NumDocs</sup></p>
    /// <ul>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///   <li>NumFields --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>FieldNums --&gt; &lt;FieldNumDelta&gt; <sup>NumFields</sup></li>
    ///   <li>FieldNumDelta --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>FieldPositions --&gt; &lt;FieldPositionDelta&gt; <sup>NumFields-1</sup></li>
    ///   <li>FieldPositionDelta --&gt; <seealso cref="DataOutput#writeVLong VLong"/></li>
    /// </ul>
    /// </li>
    /// <li><a name="tvf" id="tvf"></a>
    /// <p>The Field or .tvf file.</p>
    /// <p>this file contains, for each field that has a term vector stored, a list of
    /// the terms, their frequencies and, optionally, position, offset, and payload
    /// information.</p>
    /// <p>Field (.tvf) --&gt; Header,&lt;NumTerms, Flags, TermFreqs&gt;
    /// <sup>NumFields</sup></p>
    /// <ul>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///   <li>NumTerms --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>Flags --&gt; <seealso cref="DataOutput#writeByte Byte"/></li>
    ///   <li>TermFreqs --&gt; &lt;TermText, TermFreq, Positions?, PayloadData?, Offsets?&gt;
    ///       <sup>NumTerms</sup></li>
    ///   <li>TermText --&gt; &lt;PrefixLength, Suffix&gt;</li>
    ///   <li>PrefixLength --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>Suffix --&gt; <seealso cref="DataOutput#writeString String"/></li>
    ///   <li>TermFreq --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>Positions --&gt; &lt;PositionDelta PayloadLength?&gt;<sup>TermFreq</sup></li>
    ///   <li>PositionDelta --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>PayloadLength --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>PayloadData --&gt; <seealso cref="DataOutput#writeByte Byte"/><sup>NumPayloadBytes</sup></li>
    ///   <li>Offsets --&gt; &lt;<seealso cref="DataOutput#writeVInt VInt"/>, <seealso cref="DataOutput#writeVInt VInt"/>&gt;<sup>TermFreq</sup></li>
    /// </ul>
    /// <p>Notes:</p>
    /// <ul>
    /// <li>Flags byte stores whether this term vector has position, offset, payload.
    /// information stored.</li>
    /// <li>Term byte prefixes are shared. The PrefixLength is the number of initial
    /// bytes from the previous term which must be pre-pended to a term's suffix
    /// in order to form the term's bytes. Thus, if the previous term's text was "bone"
    /// and the term is "boy", the PrefixLength is two and the suffix is "y".</li>
    /// <li>PositionDelta is, if payloads are disabled for the term's field, the
    /// difference between the position of the current occurrence in the document and
    /// the previous occurrence (or zero, if this is the first occurrence in this
    /// document). If payloads are enabled for the term's field, then PositionDelta/2
    /// is the difference between the current and the previous position. If payloads
    /// are enabled and PositionDelta is odd, then PayloadLength is stored, indicating
    /// the length of the payload at the current term position.</li>
    /// <li>PayloadData is metadata associated with a term position. If
    /// PayloadLength is stored at the current position, then it indicates the length
    /// of this payload. If PayloadLength is not stored, then this payload has the same
    /// length as the payload at the previous position. PayloadData encodes the
    /// concatenated bytes for all of a terms occurrences.</li>
    /// <li>Offsets are stored as delta encoded VInts. The first VInt is the
    /// startOffset, the second is the endOffset.</li>
    /// </ul>
    /// </li>
    /// </ol>
    /// </summary>
    public class Lucene40TermVectorsFormat : TermVectorsFormat
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40TermVectorsFormat()
        {
        }

        public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            return new Lucene40TermVectorsReader(directory, segmentInfo, fieldInfos, context);
        }

        public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            return new Lucene40TermVectorsWriter(directory, segmentInfo.Name, context);
        }
    }
}