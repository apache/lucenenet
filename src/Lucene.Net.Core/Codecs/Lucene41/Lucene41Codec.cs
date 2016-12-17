using System;

namespace Lucene.Net.Codecs.Lucene41
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

    using CompressingStoredFieldsFormat = Lucene.Net.Codecs.Compressing.CompressingStoredFieldsFormat;
    using CompressionMode = Lucene.Net.Codecs.Compressing.CompressionMode;
    using Directory = Lucene.Net.Store.Directory;
    using IOContext = Lucene.Net.Store.IOContext;
    using Lucene40DocValuesFormat = Lucene.Net.Codecs.Lucene40.Lucene40DocValuesFormat;
    using Lucene40FieldInfosFormat = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosFormat;
    using Lucene40LiveDocsFormat = Lucene.Net.Codecs.Lucene40.Lucene40LiveDocsFormat;
    using Lucene40NormsFormat = Lucene.Net.Codecs.Lucene40.Lucene40NormsFormat;
    using Lucene40SegmentInfoFormat = Lucene.Net.Codecs.Lucene40.Lucene40SegmentInfoFormat;
    using Lucene40TermVectorsFormat = Lucene.Net.Codecs.Lucene40.Lucene40TermVectorsFormat;
    using PerFieldPostingsFormat = Lucene.Net.Codecs.Perfield.PerFieldPostingsFormat;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Implements the Lucene 4.1 index format, with configurable per-field postings formats.
    /// <p>
    /// If you want to reuse functionality of this codec in another codec, extend
    /// <seealso cref="FilterCodec"/>.
    /// </summary>
    /// <seealso cref= Lucene.Net.Codecs.Lucene41 package documentation for file format details. </seealso>
    /// @deprecated Only for reading old 4.0 segments
    /// @lucene.experimental
    [Obsolete("Only for reading old 4.0 segments")]
    public class Lucene41Codec : Codec
    {
        // TODO: slightly evil
        private readonly StoredFieldsFormat fieldsFormat = new CompressingStoredFieldsFormatAnonymousInnerClassHelper("Lucene41StoredFields", CompressionMode.FAST, 1 << 14);

        private class CompressingStoredFieldsFormatAnonymousInnerClassHelper : CompressingStoredFieldsFormat
        {
            public CompressingStoredFieldsFormatAnonymousInnerClassHelper(string formatName, CompressionMode compressionMode, int chunkSize)
                : base(formatName, compressionMode, chunkSize)
            {
            }

            public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
            {
                throw new System.NotSupportedException("this codec can only be used for reading");
            }
        }

        private readonly TermVectorsFormat VectorsFormat = new Lucene40TermVectorsFormat();
        private readonly FieldInfosFormat FieldInfosFormat_Renamed = new Lucene40FieldInfosFormat();
        private readonly SegmentInfoFormat InfosFormat = new Lucene40SegmentInfoFormat();
        private readonly LiveDocsFormat LiveDocsFormat_Renamed = new Lucene40LiveDocsFormat();

        private readonly PostingsFormat postingsFormat;

        private class PerFieldPostingsFormatAnonymousInnerClassHelper : PerFieldPostingsFormat
        {
            private readonly Lucene41Codec OuterInstance;

            public PerFieldPostingsFormatAnonymousInnerClassHelper(Lucene41Codec outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return OuterInstance.GetPostingsFormatForField(field);
            }
        }

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene41Codec()
            : base("Lucene41")
        {
            postingsFormat = new PerFieldPostingsFormatAnonymousInnerClassHelper(this);
        }

        // TODO: slightly evil
        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return fieldsFormat; }
        }

        public override sealed TermVectorsFormat TermVectorsFormat
        {
            get { return VectorsFormat; }
        }

        public override sealed PostingsFormat PostingsFormat
        {
            get { return postingsFormat; }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get { return FieldInfosFormat_Renamed; }
        }

        public override SegmentInfoFormat SegmentInfoFormat
        {
            get { return InfosFormat; }
        }

        public override sealed LiveDocsFormat LiveDocsFormat
        {
            get { return LiveDocsFormat_Renamed; }
        }

        /// <summary>
        /// Returns the postings format that should be used for writing
        ///  new segments of <code>field</code>.
        ///
        ///  The default implementation always returns "Lucene41"
        /// </summary>
        public virtual PostingsFormat GetPostingsFormatForField(string field)
        {
            return DefaultFormat;
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return DvFormat; }
        }

        private readonly PostingsFormat DefaultFormat = Codecs.PostingsFormat.ForName("Lucene41");
        private readonly DocValuesFormat DvFormat = new Lucene40DocValuesFormat();
        private readonly NormsFormat NormsFormat_Renamed = new Lucene40NormsFormat();

        public override NormsFormat NormsFormat
        {
            get { return NormsFormat_Renamed; }
        }
    }
}