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

        public override PostingsFormat PostingsFormat()
        {
            if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
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
            if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
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
            if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
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
            if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
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
            if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
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
            if (LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
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