using System;

namespace Lucene.Net.Codecs.Lucene42
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

    using Lucene40LiveDocsFormat = Lucene.Net.Codecs.Lucene40.Lucene40LiveDocsFormat;
    using Lucene40SegmentInfoFormat = Lucene.Net.Codecs.Lucene40.Lucene40SegmentInfoFormat;
    using Lucene41StoredFieldsFormat = Lucene.Net.Codecs.Lucene41.Lucene41StoredFieldsFormat;
    using PerFieldDocValuesFormat = Lucene.Net.Codecs.Perfield.PerFieldDocValuesFormat;
    using PerFieldPostingsFormat = Lucene.Net.Codecs.Perfield.PerFieldPostingsFormat;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Implements the Lucene 4.2 index format, with configurable per-field postings
    /// and docvalues formats.
    /// <p>
    /// If you want to reuse functionality of this codec in another codec, extend
    /// <seealso cref="FilterCodec"/>.
    /// </summary>
    /// <seealso cref= Lucene.Net.Codecs.Lucene42 package documentation for file format details.
    /// @lucene.experimental </seealso>
    /// @deprecated Only for reading old 4.2 segments
    // NOTE: if we make largish changes in a minor release, easier to just make Lucene43Codec or whatever
    // if they are backwards compatible or smallish we can probably do the backwards in the postingsreader
    // (it writes a minor version, etc).
    [Obsolete("Only for reading old 4.2 segments")]
    public class Lucene42Codec : Codec
    {
        private readonly StoredFieldsFormat fieldsFormat = new Lucene41StoredFieldsFormat();
        private readonly TermVectorsFormat vectorsFormat = new Lucene42TermVectorsFormat();
        private readonly FieldInfosFormat fieldInfosFormat = new Lucene42FieldInfosFormat();
        private readonly SegmentInfoFormat infosFormat = new Lucene40SegmentInfoFormat();
        private readonly LiveDocsFormat liveDocsFormat = new Lucene40LiveDocsFormat();

        private readonly PostingsFormat postingsFormat;

        private class PerFieldPostingsFormatAnonymousInnerClassHelper : PerFieldPostingsFormat
        {
            private readonly Lucene42Codec outerInstance;

            public PerFieldPostingsFormatAnonymousInnerClassHelper(Lucene42Codec outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return outerInstance.GetPostingsFormatForField(field);
            }
        }

        private readonly DocValuesFormat docValuesFormat;

        private class PerFieldDocValuesFormatAnonymousInnerClassHelper : PerFieldDocValuesFormat
        {
            private readonly Lucene42Codec outerInstance;

            public PerFieldDocValuesFormatAnonymousInnerClassHelper(Lucene42Codec outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return outerInstance.GetDocValuesFormatForField(field);
            }
        }

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene42Codec()
            : base("Lucene42")
        {
            postingsFormat = new PerFieldPostingsFormatAnonymousInnerClassHelper(this);
            docValuesFormat = new PerFieldDocValuesFormatAnonymousInnerClassHelper(this);
        }

        public override sealed StoredFieldsFormat StoredFieldsFormat
        {
            get { return fieldsFormat; }
        }

        public override sealed TermVectorsFormat TermVectorsFormat
        {
            get { return vectorsFormat; }
        }

        public override sealed PostingsFormat PostingsFormat
        {
            get { return postingsFormat; }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get { return fieldInfosFormat; }
        }

        public override SegmentInfoFormat SegmentInfoFormat
        {
            get { return infosFormat; }
        }

        public override sealed LiveDocsFormat LiveDocsFormat
        {
            get { return liveDocsFormat; }
        }

        /// <summary>
        /// Returns the postings format that should be used for writing
        ///  new segments of <code>field</code>.
        ///
        ///  The default implementation always returns "Lucene41"
        /// </summary>
        public virtual PostingsFormat GetPostingsFormatForField(string field)
        {
            return defaultFormat;
        }

        /// <summary>
        /// Returns the docvalues format that should be used for writing
        ///  new segments of <code>field</code>.
        ///
        ///  The default implementation always returns "Lucene42"
        /// </summary>
        public virtual DocValuesFormat GetDocValuesFormatForField(string field)
        {
            return defaultDVFormat;
        }

        public override sealed DocValuesFormat DocValuesFormat
        {
            get { return docValuesFormat; }
        }

        private readonly PostingsFormat defaultFormat = Codecs.PostingsFormat.ForName("Lucene41");
        private readonly DocValuesFormat defaultDVFormat = Codecs.DocValuesFormat.ForName("Lucene42");

        private readonly NormsFormat normsFormat = new Lucene42NormsFormatAnonymousInnerClassHelper();

        private class Lucene42NormsFormatAnonymousInnerClassHelper : Lucene42NormsFormat
        {
            public Lucene42NormsFormatAnonymousInnerClassHelper()
            {
            }

            public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
            {
                throw new System.NotSupportedException("this codec can only be used for reading");
            }
        }

        public override NormsFormat NormsFormat
        {
            get { return normsFormat; }
        }
    }
}