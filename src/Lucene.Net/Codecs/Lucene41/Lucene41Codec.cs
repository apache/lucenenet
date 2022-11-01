using System;
using System.Runtime.CompilerServices;

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
    using PerFieldPostingsFormat = Lucene.Net.Codecs.PerField.PerFieldPostingsFormat;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Implements the Lucene 4.1 index format, with configurable per-field postings formats.
    /// <para/>
    /// If you want to reuse functionality of this codec in another codec, extend
    /// <see cref="FilterCodec"/>.
    /// <para/>
    /// See <see cref="Lucene.Net.Codecs.Lucene41"/> package documentation for file format details.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    [Obsolete("Only for reading old 4.0 segments")]
    [CodecName("Lucene41")] // LUCENENET specific - using CodecName attribute to ensure the default name passed from subclasses is the same as this class name
    public class Lucene41Codec : Codec
    {
        // TODO: slightly evil
        private readonly StoredFieldsFormat fieldsFormat = new CompressingStoredFieldsFormatAnonymousClass("Lucene41StoredFields", CompressionMode.FAST, 1 << 14);

        private sealed class CompressingStoredFieldsFormatAnonymousClass : CompressingStoredFieldsFormat
        {
            public CompressingStoredFieldsFormatAnonymousClass(string formatName, CompressionMode compressionMode, int chunkSize)
                : base(formatName, compressionMode, chunkSize)
            {
            }

            public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
            {
                throw UnsupportedOperationException.Create("this codec can only be used for reading");
            }
        }

        private readonly TermVectorsFormat vectorsFormat = new Lucene40TermVectorsFormat();
        private readonly FieldInfosFormat fieldInfosFormat = new Lucene40FieldInfosFormat();
        private readonly SegmentInfoFormat infosFormat = new Lucene40SegmentInfoFormat();
        private readonly LiveDocsFormat liveDocsFormat = new Lucene40LiveDocsFormat();

        private readonly PostingsFormat postingsFormat;

        private sealed class PerFieldPostingsFormatAnonymousClass : PerFieldPostingsFormat
        {
            private readonly Lucene41Codec outerInstance;

            public PerFieldPostingsFormatAnonymousClass(Lucene41Codec outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return outerInstance.GetPostingsFormatForField(field);
            }
        }

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene41Codec()
            : base()
        {
            postingsFormat = new PerFieldPostingsFormatAnonymousClass(this);
        }

        // TODO: slightly evil
        public override StoredFieldsFormat StoredFieldsFormat => fieldsFormat;

        public override sealed TermVectorsFormat TermVectorsFormat => vectorsFormat;

        public override sealed PostingsFormat PostingsFormat => postingsFormat;

        public override FieldInfosFormat FieldInfosFormat => fieldInfosFormat;

        public override SegmentInfoFormat SegmentInfoFormat => infosFormat;

        public override sealed LiveDocsFormat LiveDocsFormat => liveDocsFormat;

        /// <summary>
        /// Returns the postings format that should be used for writing
        /// new segments of <paramref name="field"/>.
        /// <para/>
        /// The default implementation always returns "Lucene41"
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual PostingsFormat GetPostingsFormatForField(string field)
        {
            // LUCENENET specific - lazy initialize the codec to ensure we get the correct type if overridden.
            if (defaultFormat is null)
            {
                defaultFormat = Codecs.PostingsFormat.ForName("Lucene41");
            }
            return defaultFormat;
        }

        public override DocValuesFormat DocValuesFormat => dvFormat;

        // LUCENENET specific - lazy initialize the codec to ensure we get the correct type if overridden.
        private PostingsFormat defaultFormat;
        private readonly DocValuesFormat dvFormat = new Lucene40DocValuesFormat();
        private readonly NormsFormat normsFormat = new Lucene40NormsFormat();

        public override NormsFormat NormsFormat => normsFormat;
    }
}