using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

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

    /// <summary>
    /// @lucene.internal
    /// @lucene.experimental
    /// </summary>
    internal class PreFlexRWFieldInfosWriter : FieldInfosWriter
    {
        // TODO move to test-framework preflex RW?

        /// <summary>
        /// Extension of field infos </summary>
        internal const string FIELD_INFOS_EXTENSION = "fnm";

        // First used in 2.9; prior to 2.9 there was no format header
        internal const int FORMAT_START = -2;

        // First used in 3.4: omit only positional information
        internal const int FORMAT_OMIT_POSITIONS = -3;

        internal const int FORMAT_PREFLEX_RW = int.MinValue;

        // whenever you add a new format, make it 1 smaller (negative version logic)!
        internal const int FORMAT_CURRENT = FORMAT_OMIT_POSITIONS;

        internal const sbyte IS_INDEXED = 0x1;
        internal const sbyte STORE_TERMVECTOR = 0x2;
        internal const sbyte OMIT_NORMS = 0x10;
        internal const sbyte STORE_PAYLOADS = 0x20;
        internal const sbyte OMIT_TERM_FREQ_AND_POSITIONS = 0x40;
        internal const sbyte OMIT_POSITIONS = -128;

        public override void Write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos, IOContext context)
        {
            string fileName = IndexFileNames.SegmentFileName(segmentName, "", FIELD_INFOS_EXTENSION);
            IndexOutput output = directory.CreateOutput(fileName, context);
            bool success = false;
            try
            {
                output.WriteVInt32(FORMAT_PREFLEX_RW);
                output.WriteVInt32(infos.Count);
                foreach (FieldInfo fi in infos)
                {
                    sbyte bits = 0x0;
                    if (fi.HasVectors)
                    {
                        bits |= STORE_TERMVECTOR;
                    }
                    if (fi.OmitsNorms)
                    {
                        bits |= OMIT_NORMS;
                    }
                    if (fi.HasPayloads)
                    {
                        bits |= STORE_PAYLOADS;
                    }
                    if (fi.IsIndexed)
                    {
                        bits |= IS_INDEXED;
                        if (Debugging.AssertsEnabled) Debugging.Assert(fi.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS || !fi.HasPayloads);
                        if (fi.IndexOptions == IndexOptions.DOCS_ONLY)
                        {
                            bits |= OMIT_TERM_FREQ_AND_POSITIONS;
                        }
                        else if (fi.IndexOptions == IndexOptions.DOCS_AND_FREQS)
                        {
                            bits |= OMIT_POSITIONS;
                        }
                    }
                    output.WriteString(fi.Name);
                    /*
                     * we need to write the field number since IW tries
                     * to stabelize the field numbers across segments so the
                     * FI ordinal is not necessarily equivalent to the field number
                     */
                    output.WriteInt32(fi.Number);
                    output.WriteByte((byte)bits);
                    if (fi.IsIndexed && !fi.OmitsNorms)
                    {
                        // to allow null norm types we need to indicate if norms are written
                        // only in RW case
                        output.WriteByte((byte)(fi.NormType == Index.DocValuesType.NONE ? 0 : 1));
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(fi.Attributes is null); // not used or supported
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
    }
}