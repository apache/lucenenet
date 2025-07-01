using Lucene.Net.Codecs.BlockTerms;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene41Ords
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
    /// Customized version of <see cref="Lucene41PostingsFormat"/> that uses
    /// <see cref="FixedGapTermsIndexWriter"/>.
    /// </summary>
    [PostingsFormatName("Lucene41WithOrds")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class Lucene41WithOrds : PostingsFormat
    {
        public Lucene41WithOrds()
            : base()
        {
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase docs = new Lucene41PostingsWriter(state);

            // TODO: should we make the terms index more easily
            // pluggable?  Ie so that this codec would record which
            // index impl was used, and switch on loading?
            // Or... you must make a new Codec for this?
            TermsIndexWriterBase indexWriter;
            bool success = false;
            try
            {
                indexWriter = new FixedGapTermsIndexWriter(state);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    docs.Dispose();
                }
            }

            success = false;
            try
            {
                // Must use BlockTermsWriter (not BlockTree) because
                // BlockTree doens't support ords (yet)...
                FieldsConsumer ret = new BlockTermsWriter(indexWriter, state, docs);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    try
                    {
                        docs.Dispose();
                    }
                    finally
                    {
                        indexWriter.Dispose();
                    }
                }
            }
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase postings = new Lucene41PostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);
            TermsIndexReaderBase indexReader;

            bool success = false;
            try
            {
                indexReader = new FixedGapTermsIndexReader(state.Directory,
                                                           state.FieldInfos,
                                                           state.SegmentInfo.Name,
                                                           state.TermsIndexDivisor,
                                                           BytesRef.UTF8SortedAsUnicodeComparer,
                                                           state.SegmentSuffix, state.Context);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    postings.Dispose();
                }
            }

            success = false;
            try
            {
                FieldsProducer ret = new BlockTermsReader(indexReader,
                                                          state.Directory,
                                                          state.FieldInfos,
                                                          state.SegmentInfo,
                                                          postings,
                                                          state.Context,
                                                          state.SegmentSuffix);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    try
                    {
                        postings.Dispose();
                    }
                    finally
                    {
                        indexReader.Dispose();
                    }
                }
            }
        }

#pragma warning disable 414
        /// <summary>
        /// Extension of freq postings file
        /// </summary>
        internal const string FREQ_EXTENSION = "frq"; // LUCENENET NOTE: Not used

        /// <summary>
        /// Extension of prox postings file
        /// </summary>
        internal const string PROX_EXTENSION = "prx"; // LUCENENET NOTE: Not used
#pragma warning restore 414
    }
}
