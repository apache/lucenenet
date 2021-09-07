using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Index;
using System;

namespace Lucene.Net.Codecs.Appending
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
    /// Appending Postings Implementation
    /// </summary>
    [PostingsFormatName("Appending")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    internal class AppendingPostingsFormat : PostingsFormat
    {
        //// LUCENENET specific - removed this static variable because our name is determined by the PostingsFormatNameAttribute
        //public static string CODEC_NAME = "Appending";

        public AppendingPostingsFormat() 
            : base()
        {}

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw UnsupportedOperationException.Create("This codec can only be used for reading");
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
#pragma warning disable 612, 618
            using var postings = new Lucene40PostingsReader(state.Directory, state.FieldInfos,
                state.SegmentInfo,
                state.Context, state.SegmentSuffix);
            var ret = new AppendingTermsReader(
                state.Directory,
                state.FieldInfos,
                state.SegmentInfo,
                postings,
                state.Context,
                state.SegmentSuffix,
                state.TermsIndexDivisor);

            return ret;
#pragma warning restore 612, 618
        }
    }
}

