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

namespace Lucene.Net.Codecs.Appending
{
    using System;
    using Lucene40;
    using Index;

    /// <summary>
    /// Appending Postigns Implementation
    /// </summary>
    internal class AppendingPostingsFormat : PostingsFormat
    {
        public static String CODEC_NAME = "Appending";

        public AppendingPostingsFormat() : base(CODEC_NAME)
        {}

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException("This codec can only be used for reading");
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
#pragma warning disable 612, 618
            using (var postings = new Lucene40PostingsReader(state.Directory, state.FieldInfos,
                state.SegmentInfo,
                state.Context, state.SegmentSuffix))
            {
                var ret = new AppendingTermsReader(
                    state.Directory,
                    state.FieldInfos,
                    state.SegmentInfo,
                    postings,
                    state.Context,
                    state.SegmentSuffix,
                    state.TermsIndexDivisor);

                return ret;
            }
#pragma warning restore 612, 618
        }
    }
}

