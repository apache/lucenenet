namespace Lucene.Net.Codecs.Lucene3x
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    /// <summary>
    /// Writes 3.x-like indexes (not perfect emulation yet) for testing only!
    /// @lucene.experimental
    /// </summary>
    public class PreFlexRWCodec : Lucene3xCodec
    {
        private readonly PostingsFormat Postings = new PreFlexRWPostingsFormat();
        private readonly Lucene3xNormsFormat Norms = new PreFlexRWNormsFormat();
        private readonly FieldInfosFormat FieldInfos = new PreFlexRWFieldInfosFormat();
        private readonly TermVectorsFormat TermVectors = new PreFlexRWTermVectorsFormat();
        private readonly SegmentInfoFormat SegmentInfos = new PreFlexRWSegmentInfoFormat();
        private readonly StoredFieldsFormat StoredFields = new PreFlexRWStoredFieldsFormat();
        private readonly bool _oldFormatImpersonationIsActive;

        /// <summary>
        /// LUCENENET specific
        /// Creates the codec with OldFormatImpersonationIsActive = true.
        /// </summary>
        /// <remarks>
        /// Added so that SPIClassIterator can locate this Codec.  The iterator
        /// only recognises classes that have empty constructors.
        /// </remarks>
        public PreFlexRWCodec()
            : this(true)
        { }

        /// <summary>
        /// </summary>
        /// <param name="oldFormatImpersonationIsActive">
        /// LUCENENET specific
        /// Added to remove dependency on then-static <see cref="LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE"/>
        /// </param>
        public PreFlexRWCodec(bool oldFormatImpersonationIsActive) : base()
        {
            _oldFormatImpersonationIsActive = oldFormatImpersonationIsActive;
        }

        public override PostingsFormat PostingsFormat()
        {
            if (_oldFormatImpersonationIsActive)
            {
                return Postings;
            }
            else
            {
                return base.PostingsFormat();
            }
        }

        public override NormsFormat NormsFormat()
        {
            if (_oldFormatImpersonationIsActive)
            {
                return Norms;
            }
            else
            {
                return base.NormsFormat();
            }
        }

        public override SegmentInfoFormat SegmentInfoFormat()
        {
            if (_oldFormatImpersonationIsActive)
            {
                return SegmentInfos;
            }
            else
            {
                return base.SegmentInfoFormat();
            }
        }

        public override FieldInfosFormat FieldInfosFormat()
        {
            if (_oldFormatImpersonationIsActive)
            {
                return FieldInfos;
            }
            else
            {
                return base.FieldInfosFormat();
            }
        }

        public override TermVectorsFormat TermVectorsFormat()
        {
            if (_oldFormatImpersonationIsActive)
            {
                return TermVectors;
            }
            else
            {
                return base.TermVectorsFormat();
            }
        }

        public override StoredFieldsFormat StoredFieldsFormat()
        {
            if (_oldFormatImpersonationIsActive)
            {
                return StoredFields;
            }
            else
            {
                return base.StoredFieldsFormat();
            }
        }
    }
}