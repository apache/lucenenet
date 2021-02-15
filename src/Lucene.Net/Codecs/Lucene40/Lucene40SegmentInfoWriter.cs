using Lucene.Net.Support;
using System;

namespace Lucene.Net.Codecs.Lucene40
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

    using Directory = Lucene.Net.Store.Directory;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Lucene 4.0 implementation of <see cref="SegmentInfoWriter"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Lucene40SegmentInfoFormat"/>
    [Obsolete]
    public class Lucene40SegmentInfoWriter : SegmentInfoWriter
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40SegmentInfoWriter()
        {
        }

        /// <summary>
        /// Save a single segment's info. </summary>
        public override void Write(Directory dir, SegmentInfo si, FieldInfos fis, IOContext ioContext)
        {
            string fileName = IndexFileNames.SegmentFileName(si.Name, "", Lucene40SegmentInfoFormat.SI_EXTENSION);
            si.AddFile(fileName);

            IndexOutput output = dir.CreateOutput(fileName, ioContext);

            bool success = false;
            try
            {
                CodecUtil.WriteHeader(output, Lucene40SegmentInfoFormat.CODEC_NAME, Lucene40SegmentInfoFormat.VERSION_CURRENT);
                // Write the Lucene version that created this segment, since 3.1
                output.WriteString(si.Version);
                output.WriteInt32(si.DocCount);

                output.WriteByte((byte)(si.UseCompoundFile ? SegmentInfo.YES : SegmentInfo.NO));
                output.WriteStringStringMap(si.Diagnostics);
                output.WriteStringStringMap(Collections.EmptyMap<string, string>());
                output.WriteStringSet(si.GetFiles());

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(output);
                    si.Dir.DeleteFile(fileName);
                }
                else
                {
                    output.Dispose();
                }
            }
        }
    }
}