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

    using PerFieldPostingsFormat = Lucene.Net.Codecs.Perfield.PerFieldPostingsFormat;

    /// <summary>
    /// Implements the Lucene 4.0 index format, with configurable per-field postings formats.
    /// <p>
    /// If you want to reuse functionality of this codec in another codec, extend
    /// <seealso cref="FilterCodec"/>.
    /// </summary>
    /// <seealso cref= Lucene.Net.Codecs.Lucene40 package documentation for file format details. </seealso>
    /// @deprecated Only for reading old 4.0 segments
    // NOTE: if we make largish changes in a minor release, easier to just make Lucene42Codec or whatever
    // if they are backwards compatible or smallish we can probably do the backwards in the postingsreader
    // (it writes a minor version, etc).
    [Obsolete("Only for reading old 4.0 segments")]
    public class Lucene40Codec : Codec
    {
        private readonly StoredFieldsFormat FieldsFormat = new Lucene40StoredFieldsFormat();
        private readonly TermVectorsFormat VectorsFormat = new Lucene40TermVectorsFormat();
        private readonly FieldInfosFormat FieldInfosFormat_Renamed = new Lucene40FieldInfosFormat();
        private readonly SegmentInfoFormat InfosFormat = new Lucene40SegmentInfoFormat();
        private readonly LiveDocsFormat LiveDocsFormat_Renamed = new Lucene40LiveDocsFormat();

        private readonly PostingsFormat postingsFormat;

        private class PerFieldPostingsFormatAnonymousInnerClassHelper : PerFieldPostingsFormat
        {
            private readonly Lucene40Codec OuterInstance;

            public PerFieldPostingsFormatAnonymousInnerClassHelper(Lucene40Codec outerInstance)
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
        public Lucene40Codec()
            : base("Lucene40")
        {
            postingsFormat = new PerFieldPostingsFormatAnonymousInnerClassHelper(this);
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

        private readonly DocValuesFormat DefaultDVFormat = new Lucene40DocValuesFormat();

        public override DocValuesFormat DocValuesFormat
        {
            get { return DefaultDVFormat; }
        }

        private readonly NormsFormat NormsFormat_Renamed = new Lucene40NormsFormat();

        public override NormsFormat NormsFormat
        {
            get { return NormsFormat_Renamed; }
        }

        public override sealed LiveDocsFormat LiveDocsFormat
        {
            get { return LiveDocsFormat_Renamed; }
        }

        /// <summary>
        /// Returns the postings format that should be used for writing
        ///  new segments of <code>field</code>.
        ///
        ///  The default implementation always returns "Lucene40"
        /// </summary>
        public virtual PostingsFormat GetPostingsFormatForField(string field)
        {
            return DefaultFormat;
        }

        private readonly PostingsFormat DefaultFormat = Codecs.PostingsFormat.ForName("Lucene40");
    }
}