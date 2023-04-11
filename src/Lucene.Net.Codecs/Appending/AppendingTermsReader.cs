using Lucene.Net.Index;
using Lucene.Net.Store;
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
    /// Reads append-only terms from AppendingTermsWriter.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [Obsolete("Only for reading old Appending segments")]
    public class AppendingTermsReader : BlockTreeTermsReader<object>
    {
        private const string APPENDING_TERMS_CODEC_NAME = "APPENDING_TERMS_DICT";
        private const string APPENDING_TERMS_INDEX_CODEC_NAME = "APPENDING_TERMS_INDEX";

        public AppendingTermsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo info,
            PostingsReaderBase postingsReader,
            IOContext ioContext, string segmentSuffix, int indexDivisor)
            : base(dir, fieldInfos, info, postingsReader, ioContext, segmentSuffix, indexDivisor, subclassState: null)
        {
        }

        protected override int ReadHeader(IndexInput input)
        {
            return CodecUtil.CheckHeader(input, APPENDING_TERMS_CODEC_NAME,
                BlockTreeTermsWriter.VERSION_START,
                BlockTreeTermsWriter.VERSION_CURRENT);
        }

        protected override int ReadIndexHeader(IndexInput input)
        {
            return CodecUtil.CheckHeader(input, APPENDING_TERMS_INDEX_CODEC_NAME,
                BlockTreeTermsWriter.VERSION_START,
                BlockTreeTermsWriter.VERSION_CURRENT);
        }

        protected override void SeekDir(IndexInput input, long dirOffset)
        {
            input.Seek(input.Length - sizeof(long)/8);
            long offset = input.ReadInt64();
            input.Seek(offset);
        }
    }
}
