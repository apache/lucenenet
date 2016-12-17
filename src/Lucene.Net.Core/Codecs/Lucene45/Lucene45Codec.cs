using System;

namespace Lucene.Net.Codecs.Lucene45
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
    using Lucene42FieldInfosFormat = Lucene.Net.Codecs.Lucene42.Lucene42FieldInfosFormat;
    using Lucene42NormsFormat = Lucene.Net.Codecs.Lucene42.Lucene42NormsFormat;
    using Lucene42TermVectorsFormat = Lucene.Net.Codecs.Lucene42.Lucene42TermVectorsFormat;
    using PerFieldDocValuesFormat = Lucene.Net.Codecs.Perfield.PerFieldDocValuesFormat;
    using PerFieldPostingsFormat = Lucene.Net.Codecs.Perfield.PerFieldPostingsFormat;

    /// <summary>
    /// Implements the Lucene 4.5 index format, with configurable per-field postings
    /// and docvalues formats.
    /// <p>
    /// If you want to reuse functionality of this codec in another codec, extend
    /// <seealso cref="FilterCodec"/>.
    /// </summary>
    /// <seealso cref= Lucene.Net.Codecs.Lucene45 package documentation for file format details.
    /// @lucene.experimental </seealso>
    /// @deprecated Only for reading old 4.3-4.5 segments
    // NOTE: if we make largish changes in a minor release, easier to just make Lucene46Codec or whatever
    // if they are backwards compatible or smallish we can probably do the backwards in the postingsreader
    // (it writes a minor version, etc).
    [Obsolete("Only for reading old 4.3-4.5 segments")]
    public class Lucene45Codec : Codec
    {
        private readonly StoredFieldsFormat FieldsFormat = new Lucene41StoredFieldsFormat();
        private readonly TermVectorsFormat VectorsFormat = new Lucene42TermVectorsFormat();
        private readonly FieldInfosFormat FieldInfosFormat_Renamed = new Lucene42FieldInfosFormat();
        private readonly SegmentInfoFormat InfosFormat = new Lucene40SegmentInfoFormat();
        private readonly LiveDocsFormat LiveDocsFormat_Renamed = new Lucene40LiveDocsFormat();

        private readonly PostingsFormat postingsFormat;

        private class PerFieldPostingsFormatAnonymousInnerClassHelper : PerFieldPostingsFormat
        {
            private readonly Lucene45Codec OuterInstance;

            public PerFieldPostingsFormatAnonymousInnerClassHelper(Lucene45Codec outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                return OuterInstance.GetPostingsFormatForField(field);
            }
        }

        private readonly DocValuesFormat docValuesFormat;

        private class PerFieldDocValuesFormatAnonymousInnerClassHelper : PerFieldDocValuesFormat
        {
            private readonly Lucene45Codec OuterInstance;

            public PerFieldDocValuesFormatAnonymousInnerClassHelper(Lucene45Codec outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override DocValuesFormat GetDocValuesFormatForField(string field)
            {
                return OuterInstance.GetDocValuesFormatForField(field);
            }
        }

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene45Codec()
            : base("Lucene45")
        {
            postingsFormat = new PerFieldPostingsFormatAnonymousInnerClassHelper(this);
            docValuesFormat = new PerFieldDocValuesFormatAnonymousInnerClassHelper(this);
        }

        public override sealed StoredFieldsFormat StoredFieldsFormat
        {
            get { return FieldsFormat; }
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

        /// <summary>
        /// Returns the docvalues format that should be used for writing
        ///  new segments of <code>field</code>.
        ///
        ///  The default implementation always returns "Lucene45"
        /// </summary>
        public virtual DocValuesFormat GetDocValuesFormatForField(string field)
        {
            return DefaultDVFormat;
        }

        public override sealed DocValuesFormat DocValuesFormat
        {
            get { return docValuesFormat; }
        }

        private readonly PostingsFormat DefaultFormat = Codecs.PostingsFormat.ForName("Lucene41");
        private readonly DocValuesFormat DefaultDVFormat = Codecs.DocValuesFormat.ForName("Lucene45");

        private readonly NormsFormat NormsFormat_Renamed = new Lucene42NormsFormat();

        public override sealed NormsFormat NormsFormat
        {
            get { return NormsFormat_Renamed; }
        }
    }
}