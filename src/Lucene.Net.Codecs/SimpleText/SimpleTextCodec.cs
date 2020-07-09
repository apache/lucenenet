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

    /// <summary>
    /// Plain text index format.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    [CodecName("SimpleText")] // LUCENENET specific - using CodecName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class SimpleTextCodec : Codec
    {
        private readonly PostingsFormat _postings = new SimpleTextPostingsFormat();
        private readonly StoredFieldsFormat _storedFields = new SimpleTextStoredFieldsFormat();
        private readonly SegmentInfoFormat _segmentInfos = new SimpleTextSegmentInfoFormat();
        private readonly FieldInfosFormat _fieldInfosFormatRenamed = new SimpleTextFieldInfosFormat();
        private readonly TermVectorsFormat _vectorsFormat = new SimpleTextTermVectorsFormat();
        private readonly NormsFormat _normsFormatRenamed = new SimpleTextNormsFormat();
        private readonly LiveDocsFormat _liveDocs = new SimpleTextLiveDocsFormat();
        private readonly DocValuesFormat _dvFormat = new SimpleTextDocValuesFormat();

        public SimpleTextCodec() 
            : base()
        {
        }

        public override PostingsFormat PostingsFormat => _postings;

        public override StoredFieldsFormat StoredFieldsFormat => _storedFields;

        public override TermVectorsFormat TermVectorsFormat => _vectorsFormat;

        public override FieldInfosFormat FieldInfosFormat => _fieldInfosFormatRenamed;

        public override SegmentInfoFormat SegmentInfoFormat => _segmentInfos;

        public override NormsFormat NormsFormat => _normsFormatRenamed;

        public override LiveDocsFormat LiveDocsFormat => _liveDocs;

        public override DocValuesFormat DocValuesFormat => _dvFormat;
    }
}