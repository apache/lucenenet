using System;
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

    using PerFieldPostingsFormat = Lucene.Net.Codecs.PerField.PerFieldPostingsFormat;

    /// <summary>
    /// Implements the Lucene 4.0 index format, with configurable per-field postings formats.
    /// <para/>
    /// If you want to reuse functionality of this codec in another codec, extend
    /// <see cref="FilterCodec"/>.
    /// <para/>
    /// See <see cref="Lucene.Net.Codecs.Lucene40"/> package documentation for file format details.
    /// </summary>
    // NOTE: if we make largish changes in a minor release, easier to just make Lucene42Codec or whatever
    // if they are backwards compatible or smallish we can probably do the backwards in the postingsreader
    // (it writes a minor version, etc).
    [Obsolete("Only for reading old 4.0 segments")]
    [CodecName("Lucene40")] // LUCENENET specific - using CodecName attribute to ensure the default name passed from subclasses is the same as this class name
    public class Lucene40Codec : Codec
    {
        private readonly StoredFieldsFormat fieldsFormat = new Lucene40StoredFieldsFormat();
        private readonly TermVectorsFormat vectorsFormat = new Lucene40TermVectorsFormat();
        private readonly FieldInfosFormat fieldInfosFormat = new Lucene40FieldInfosFormat();
        private readonly SegmentInfoFormat infosFormat = new Lucene40SegmentInfoFormat();
        private readonly LiveDocsFormat liveDocsFormat = new Lucene40LiveDocsFormat();

        private readonly PostingsFormat postingsFormat;

        private sealed class PerFieldPostingsFormatAnonymousClass : PerFieldPostingsFormat
        {
            private readonly Lucene40Codec outerInstance;

            public PerFieldPostingsFormatAnonymousClass(Lucene40Codec outerInstance)
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
        public Lucene40Codec()
            : base()
        {
            postingsFormat = new PerFieldPostingsFormatAnonymousClass(this);
        }

        public override sealed StoredFieldsFormat StoredFieldsFormat => fieldsFormat;

        public override sealed TermVectorsFormat TermVectorsFormat => vectorsFormat;

        public override sealed PostingsFormat PostingsFormat => postingsFormat;

        public override FieldInfosFormat FieldInfosFormat => fieldInfosFormat;

        public override SegmentInfoFormat SegmentInfoFormat => infosFormat;

        private readonly DocValuesFormat defaultDVFormat = new Lucene40DocValuesFormat();

        public override DocValuesFormat DocValuesFormat => defaultDVFormat;

        private readonly NormsFormat normsFormat = new Lucene40NormsFormat();

        public override NormsFormat NormsFormat => normsFormat;

        public override sealed LiveDocsFormat LiveDocsFormat => liveDocsFormat;

        /// <summary>
        /// Returns the postings format that should be used for writing
        /// new segments of <paramref name="field"/>.
        /// <para/>
        /// The default implementation always returns "Lucene40".
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual PostingsFormat GetPostingsFormatForField(string field)
        {
            // LUCENENET specific - lazy initialize the codec to ensure we get the correct type if overridden.
            if (defaultFormat is null)
            {
                defaultFormat = Codecs.PostingsFormat.ForName("Lucene40");
            }
            return defaultFormat;
        }

        // LUCENENET specific - lazy initialize the codec to ensure we get the correct type if overridden.
        private PostingsFormat defaultFormat;
    }
}