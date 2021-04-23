using System;
using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
using Directory = Lucene.Net.Store.Directory;

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

    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IOContext = Lucene.Net.Store.IOContext;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Lucene3x ReadOnly <see cref="TermVectorsFormat"/> implementation 
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [Obsolete("(4.0) this is only used to read indexes created before 4.0.")]
    internal class Lucene3xTermVectorsFormat : TermVectorsFormat
    {
        public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            string fileName = IndexFileNames.SegmentFileName(Lucene3xSegmentInfoFormat.GetDocStoreSegment(segmentInfo), "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION);

            // Unfortunately, for 3.x indices, each segment's
            // FieldInfos can lie about hasVectors (claim it's true
            // when really it's false).... so we have to carefully
            // check if the files really exist before trying to open
            // them (4.x has fixed this):
            bool exists;
            if (Lucene3xSegmentInfoFormat.GetDocStoreOffset(segmentInfo) != -1 && Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(segmentInfo))
            {
                string cfxFileName = IndexFileNames.SegmentFileName(Lucene3xSegmentInfoFormat.GetDocStoreSegment(segmentInfo), "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION);
                if (segmentInfo.Dir.FileExists(cfxFileName))
                {
                    Directory cfsDir = new CompoundFileDirectory(segmentInfo.Dir, cfxFileName, context, false);
                    try
                    {
                        exists = cfsDir.FileExists(fileName);
                    }
                    finally
                    {
                        cfsDir.Dispose();
                    }
                }
                else
                {
                    exists = false;
                }
            }
            else
            {
                exists = directory.FileExists(fileName);
            }

            if (!exists)
            {
                // 3x's FieldInfos sometimes lies and claims a segment
                // has vectors when it doesn't:
                return null;
            }
            else
            {
                return new Lucene3xTermVectorsReader(directory, segmentInfo, fieldInfos, context);
            }
        }

        public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            throw UnsupportedOperationException.Create("this codec can only be used for reading");
        }
    }
}