namespace Lucene.Net.Codecs.Memory
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

    using Lucene41PostingsWriter = Lucene41.Lucene41PostingsWriter;
    using Lucene41PostingsReader = Lucene41.Lucene41PostingsReader;
    using SegmentReadState = Index.SegmentReadState;
    using SegmentWriteState = Index.SegmentWriteState;
    using IOUtils = Util.IOUtils;

    /// <summary>
    /// FST term dict + Lucene41PBF
    /// </summary>
    [PostingsFormatName("FST41")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class FSTPostingsFormat : PostingsFormat
    {
        public FSTPostingsFormat() 
            : base()
        {
        }

        public override string ToString()
        {
            return Name;
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase postingsWriter = new Lucene41PostingsWriter(state);

            bool success = false;
            try
            {
                FieldsConsumer ret = new FSTTermsWriter(state, postingsWriter);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(postingsWriter);
                }
            }
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase postingsReader = new Lucene41PostingsReader(state.Directory, state.FieldInfos,
                state.SegmentInfo, state.Context, state.SegmentSuffix);
            bool success = false;
            try
            {
                FieldsProducer ret = new FSTTermsReader(state, postingsReader);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(postingsReader);
                }
            }
        }
    }
}