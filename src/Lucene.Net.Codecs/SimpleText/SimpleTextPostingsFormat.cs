namespace Lucene.Net.Codecs.SimpleText
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

    using SegmentWriteState = Index.SegmentWriteState;
    using SegmentReadState = Index.SegmentReadState;
    using IndexFileNames = Index.IndexFileNames;

    /// <summary>
    /// For debugging, curiosity, transparency only!!  Do not
    /// use this codec in production.
    /// 
    /// <para>This codec stores all postings data in a single
    /// human-readable text file (_N.pst).  You can view this in
    /// any text editor, and even edit it to alter your index.
    /// </para>
    /// @lucene.experimental 
    /// </summary>
    [PostingsFormatName("SimpleText")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class SimpleTextPostingsFormat : PostingsFormat
    {
        public SimpleTextPostingsFormat() 
            : base()
        {
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new SimpleTextFieldsWriter(state);
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new SimpleTextFieldsReader(state);
        }

        /// <summary>
        /// Extension of freq postings file. </summary>
        internal const string POSTINGS_EXTENSION = "pst";

        internal static string GetPostingsFileName(string segment, string segmentSuffix)
        {
            return IndexFileNames.SegmentFileName(segment, segmentSuffix, POSTINGS_EXTENSION);
        }
    }
}