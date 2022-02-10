using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
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

    /// <summary>
    /// Lucene 4.0 FieldInfos writer.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Lucene40FieldInfosFormat"/>
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
                output.WriteVInt32(infos.Count);
                foreach (FieldInfo fi in infos)
                {
                    IndexOptions indexOptions = fi.IndexOptions;
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
                        // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                        if (Debugging.AssertsEnabled) Debugging.Assert(IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.HasPayloads);
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
                    output.WriteVInt32(fi.Number);
                    output.WriteByte((byte)bits);

                    // pack the DV types in one byte
                    byte dv = DocValuesByte(fi.DocValuesType, fi.GetAttribute(Lucene40FieldInfosReader.LEGACY_DV_TYPE_KEY));
                    byte nrm = DocValuesByte(fi.NormType, fi.GetAttribute(Lucene40FieldInfosReader.LEGACY_NORM_TYPE_KEY));
                    if (Debugging.AssertsEnabled) Debugging.Assert((dv & (~0xF)) == 0 && (nrm & (~0x0F)) == 0);
                    var val = (byte)(0xff & ((nrm << 4) | (byte)dv));
                    output.WriteByte(val);
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
                    IOUtils.DisposeWhileHandlingException(output);
                }
            }
        }

        /// <summary>
        /// 4.0-style docvalues byte </summary>
        public virtual byte DocValuesByte(DocValuesType type, string legacyTypeAtt)
        {
            if (type == DocValuesType.NONE)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(legacyTypeAtt is null);
                return 0;
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(legacyTypeAtt != null);
                //return (sbyte)LegacyDocValuesType.ordinalLookup[legacyTypeAtt];
                return (byte)legacyTypeAtt.ToLegacyDocValuesType();
            }
        }
    }
}