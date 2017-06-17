using System;
using System.Collections.Generic;

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

    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using Directory = Lucene.Net.Store.Directory;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Lucene 4.0 implementation of <see cref="SegmentInfoReader"/>.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    /// <seealso cref="Lucene40SegmentInfoFormat"/>
    [Obsolete("Only for reading old 4.0-4.5 segments")]
    public class Lucene40SegmentInfoReader : SegmentInfoReader
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40SegmentInfoReader()
        {
        }

        public override SegmentInfo Read(Directory dir, string segment, IOContext context)
        {
            string fileName = IndexFileNames.SegmentFileName(segment, "", Lucene40SegmentInfoFormat.SI_EXTENSION);
            IndexInput input = dir.OpenInput(fileName, context);
            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Lucene40SegmentInfoFormat.CODEC_NAME, Lucene40SegmentInfoFormat.VERSION_START, Lucene40SegmentInfoFormat.VERSION_CURRENT);
                string version = input.ReadString();
                int docCount = input.ReadInt32();
                if (docCount < 0)
                {
                    throw new CorruptIndexException("invalid docCount: " + docCount + " (resource=" + input + ")");
                }
                bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;
                IDictionary<string, string> diagnostics = input.ReadStringStringMap();
                input.ReadStringStringMap(); // read deprecated attributes
                ISet<string> files = input.ReadStringSet();

                CodecUtil.CheckEOF(input);

                SegmentInfo si = new SegmentInfo(dir, version, segment, docCount, isCompoundFile, null, diagnostics);
                si.SetFiles(files);

                success = true;

                return si;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(input);
                }
                else
                {
                    input.Dispose();
                }
            }
        }
    }
}