using System;

namespace Lucene.Net.Codecs.Cranky
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

    /// <summary>
    /// Codec for testing that throws random IOExceptions
    /// </summary>
    public class CrankyCodec : FilterCodec
    {
        private readonly Random random;

        /// <summary>
        /// Wrap the provided codec with crankiness.
        /// Try passing Asserting for the most fun.
        /// </summary>
        public CrankyCodec(Codec @delegate, Random random)
            // we impersonate the passed-in codec, so we don't need to be in SPI,
            // and so we don't change file formats
            : base(@delegate, @delegate.Name)
        {
            this.random = random;
        }

        public override DocValuesFormat DocValuesFormat
            => new CrankyDocValuesFormat(m_delegate.DocValuesFormat, random);

        public override FieldInfosFormat FieldInfosFormat
            => new CrankyFieldInfosFormat(m_delegate.FieldInfosFormat, random);

        public override LiveDocsFormat LiveDocsFormat
            => new CrankyLiveDocsFormat(m_delegate.LiveDocsFormat, random);

        public override NormsFormat NormsFormat
            => new CrankyNormsFormat(m_delegate.NormsFormat, random);

        public override PostingsFormat PostingsFormat
            => new CrankyPostingsFormat(m_delegate.PostingsFormat, random);

        public override SegmentInfoFormat SegmentInfoFormat
            => new CrankySegmentInfoFormat(m_delegate.SegmentInfoFormat, random);

        public override StoredFieldsFormat StoredFieldsFormat
            => new CrankyStoredFieldsFormat(m_delegate.StoredFieldsFormat, random);

        public override TermVectorsFormat TermVectorsFormat
            => new CrankyTermVectorsFormat(m_delegate.TermVectorsFormat, random);

        public override string ToString() => $"Cranky({m_delegate})";
    }
}
