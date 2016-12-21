using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene40
{
    using Directory = Lucene.Net.Store.Directory;
    using DocValuesType = Lucene.Net.Index.DocValuesType;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using IndexOptions = Lucene.Net.Index.IndexOptions;

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

    using LegacyDocValuesType = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosReader.LegacyDocValuesType;

    /// <summary>
    /// Lucene 4.0 FieldInfos writer.
    /// </summary>
    /// <seealso> cref= Lucene40FieldInfosFormat
    /// @lucene.experimental </seealso>
    [Obsolete]
    public class Lucene40FieldInfosWriter : FieldInfosWriter
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40FieldInfosWriter()
        {
        }

        public override void Write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos, IOContext context)
        {
            string fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene40FieldInfosFormat.FIELD_INFOS_EXTENSION);
            IndexOutput output = directory.CreateOutput(fileName, context);
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(output, Lucene40FieldInfosFormat.CODEC_NAME, Lucene40FieldInfosFormat.FORMAT_CURRENT);
                output.WriteVInt(infos.Size);
                foreach (FieldInfo fi in infos)
                {
                    IndexOptions? indexOptions = fi.IndexOptions;
                    sbyte bits = 0x0;
                    if (fi.HasVectors)
                    {
                        bits |= Lucene40FieldInfosFormat.STORE_TERMVECTOR;
                    }
                    if (fi.OmitsNorms)
                    {
                        bits |= Lucene40FieldInfosFormat.OMIT_NORMS;
                    }
                    if (fi.HasPayloads)
                    {
                        bits |= Lucene40FieldInfosFormat.STORE_PAYLOADS;
                    }
                    if (fi.IsIndexed)
                    {
                        bits |= Lucene40FieldInfosFormat.IS_INDEXED;
                        Debug.Assert(indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS || !fi.HasPayloads);
                        if (indexOptions == IndexOptions.DOCS_ONLY)
                        {
                            bits |= Lucene40FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS;
                        }
                        else if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                        {
                            bits |= Lucene40FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS;
                        }
                        else if (indexOptions == IndexOptions.DOCS_AND_FREQS)
                        {
                            bits |= Lucene40FieldInfosFormat.OMIT_POSITIONS;
                        }
                    }
                    output.WriteString(fi.Name);
                    output.WriteVInt(fi.Number);
                    output.WriteByte((byte)bits);

                    // pack the DV types in one byte
                    sbyte dv = DocValuesByte(fi.DocValuesType, fi.GetAttribute(Lucene40FieldInfosReader.LEGACY_DV_TYPE_KEY));
                    sbyte nrm = DocValuesByte(fi.NormType, fi.GetAttribute(Lucene40FieldInfosReader.LEGACY_NORM_TYPE_KEY));
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

        /// <summary>
        /// 4.0-style docvalues byte </summary>
        public virtual sbyte DocValuesByte(DocValuesType? type, string legacyTypeAtt)
        {
            if (type == null)
            {
                Debug.Assert(legacyTypeAtt == null);
                return 0;
            }
            else
            {
                Debug.Assert(legacyTypeAtt != null);
                return (sbyte)LegacyDocValuesType.ordinalLookup[legacyTypeAtt];
            }
        }
    }
}