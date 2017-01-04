using System;
using System.Collections.Generic;
using IndexFileNames = Lucene.Net.Index.IndexFileNames;

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

    using Lucene40LiveDocsFormat = Lucene.Net.Codecs.Lucene40.Lucene40LiveDocsFormat;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Supports the Lucene 3.x index format (readonly) </summary>
    /// @deprecated Only for reading existing 3.x indexes
    [Obsolete("Only for reading existing 3.x indexes")]
    public class Lucene3xCodec : Codec
    {
        public Lucene3xCodec()
            : base("Lucene3x")
        {
        }

        private readonly PostingsFormat postingsFormat = new Lucene3xPostingsFormat();

        private readonly StoredFieldsFormat fieldsFormat = new Lucene3xStoredFieldsFormat();

        private readonly TermVectorsFormat vectorsFormat = new Lucene3xTermVectorsFormat();

        private readonly FieldInfosFormat fieldInfosFormat = new Lucene3xFieldInfosFormat();

        private readonly SegmentInfoFormat infosFormat = new Lucene3xSegmentInfoFormat();

        private readonly Lucene3xNormsFormat normsFormat = new Lucene3xNormsFormat();

        /// <summary>
        /// Extension of compound file for doc store files </summary>
        internal const string COMPOUND_FILE_STORE_EXTENSION = "cfx";

        // TODO: this should really be a different impl
        private readonly LiveDocsFormat liveDocsFormat = new Lucene40LiveDocsFormat();

        // 3.x doesn't support docvalues
        private readonly DocValuesFormat docValuesFormat = new DocValuesFormatAnonymousInnerClassHelper();

        private class DocValuesFormatAnonymousInnerClassHelper : DocValuesFormat
        {
            public DocValuesFormatAnonymousInnerClassHelper()
                : base("Lucene3x")
            {
            }

            public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
            {
                throw new System.NotSupportedException("this codec cannot write docvalues");
            }

            public override DocValuesProducer FieldsProducer(SegmentReadState state)
            {
                return null; // we have no docvalues, ever
            }
        }

        public override PostingsFormat PostingsFormat
        {
            get { return postingsFormat; }
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return docValuesFormat; }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return fieldsFormat; }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get { return vectorsFormat; }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get { return fieldInfosFormat; }
        }

        public override SegmentInfoFormat SegmentInfoFormat
        {
            get { return infosFormat; }
        }

        public override NormsFormat NormsFormat
        {
            get { return normsFormat; }
        }

        public override LiveDocsFormat LiveDocsFormat
        {
            get { return liveDocsFormat; }
        }

        /// <summary>
        /// Returns file names for shared doc stores, if any, else
        /// null.
        /// </summary>
        public static ISet<string> GetDocStoreFiles(SegmentInfo info)
        {
            if (Lucene3xSegmentInfoFormat.GetDocStoreOffset(info) != -1)
            {
                string dsName = Lucene3xSegmentInfoFormat.GetDocStoreSegment(info);
                ISet<string> files = new HashSet<string>();
                if (Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(info))
                {
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", COMPOUND_FILE_STORE_EXTENSION));
                }
                else
                {
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION));
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION));
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION));
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION));
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION));
                }
                return files;
            }
            else
            {
                return null;
            }
        }
    }
}