using System;

namespace Lucene.Net.Codecs.Lucene3x
{
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;

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

    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Codec that reads the pre-flex-indexing postings
    ///  format.  It does not provide a writer because newly
    ///  written segments should use the Codec configured on IndexWriter.
    /// </summary>
    /// @deprecated (4.0) this is only used to read indexes created
    /// before 4.0.
    /// @lucene.experimental
    [Obsolete("(4.0) this is only used to read indexes created")]
    internal class Lucene3xPostingsFormat : PostingsFormat
    {
        /// <summary>
        /// Extension of terms file </summary>
        public const string TERMS_EXTENSION = "tis";

        /// <summary>
        /// Extension of terms index file </summary>
        public const string TERMS_INDEX_EXTENSION = "tii";

        /// <summary>
        /// Extension of freq postings file </summary>
        public const string FREQ_EXTENSION = "frq";

        /// <summary>
        /// Extension of prox postings file </summary>
        public const string PROX_EXTENSION = "prx";

        public Lucene3xPostingsFormat()
            : base("Lucene3x")
        {
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new System.NotSupportedException("this codec can only be used for reading");
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new Lucene3xFields(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.TermsIndexDivisor);
        }
    }
}