using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene42
{
    using Directory = Lucene.Net.Store.Directory;

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

    using DocValuesType = Lucene.Net.Index.DocValuesType;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Lucene 4.2 FieldInfos writer.
    /// </summary>
    /// <seealso> cref= Lucene42FieldInfosFormat
    /// @lucene.experimental </seealso>
    [Obsolete]
    public sealed class Lucene42FieldInfosWriter : FieldInfosWriter
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene42FieldInfosWriter()
        {
        }

        public override void Write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos, IOContext context)
        {
            string fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene42FieldInfosFormat.EXTENSION);
            IndexOutput output = directory.CreateOutput(fileName, context);
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(output, Lucene42FieldInfosFormat.CODEC_NAME, Lucene42FieldInfosFormat.FORMAT_CURRENT);
                output.WriteVInt(infos.Count);
                foreach (FieldInfo fi in infos)
                {
                    IndexOptions? indexOptions = fi.IndexOptions;
                    sbyte bits = 0x0;
                    if (fi.HasVectors)
                    {
                        bits |= Lucene42FieldInfosFormat.STORE_TERMVECTOR;
                    }
                    if (fi.OmitsNorms)
                    {
                        bits |= Lucene42FieldInfosFormat.OMIT_NORMS;
                    }
                    if (fi.HasPayloads)
                    {
                        bits |= Lucene42FieldInfosFormat.STORE_PAYLOADS;
                    }
                    if (fi.IsIndexed)
                    {
                        bits |= Lucene42FieldInfosFormat.IS_INDEXED;
                        Debug.Assert(indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS || !fi.HasPayloads);
                        if (indexOptions == IndexOptions.DOCS_ONLY)
                        {
                            bits |= Lucene42FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS;
                        }
                        else if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                        {
                            bits |= Lucene42FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS;
                        }
                        else if (indexOptions == IndexOptions.DOCS_AND_FREQS)
                        {
                            bits |= Lucene42FieldInfosFormat.OMIT_POSITIONS;
                        }
                    }
                    output.WriteString(fi.Name);
                    output.WriteVInt(fi.Number);
                    output.WriteByte((byte)bits);

                    // pack the DV types in one byte
                    var dv = DocValuesByte(fi.DocValuesType);
                    var nrm = DocValuesByte(fi.NormType);
                    Debug.Assert((dv & (~0xF)) == 0 && (nrm & (~0x0F)) == 0);
                    var val = unchecked((sbyte)(0xff & ((nrm << 4) | dv)));
                    output.WriteByte((byte)val);
                    output.WriteStringStringMap(fi.Attributes);
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    output.Dispose();
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(output);
                }
            }
        }

        private static sbyte DocValuesByte(DocValuesType? type)
        {
            if (type == null)
            {
                return 0;
            }
            else if (type == DocValuesType.NUMERIC)
            {
                return 1;
            }
            else if (type == DocValuesType.BINARY)
            {
                return 2;
            }
            else if (type == DocValuesType.SORTED)
            {
                return 3;
            }
            else if (type == DocValuesType.SORTED_SET)
            {
                return 4;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}