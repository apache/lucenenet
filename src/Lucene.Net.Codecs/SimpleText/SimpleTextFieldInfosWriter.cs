﻿using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

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

    using BytesRef = Util.BytesRef;
    using Directory = Store.Directory;
    using DocValuesType = Index.DocValuesType;
    using FieldInfo = Index.FieldInfo;
    using FieldInfos = Index.FieldInfos;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IOContext = Store.IOContext;
    using IOUtils = Util.IOUtils;

    /// <summary>
    /// Writes plain text field infos files.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextFieldInfosWriter : FieldInfosWriter
    {
        /// <summary>
        /// Extension of field infos. </summary>
        internal const string FIELD_INFOS_EXTENSION = "inf";

        internal static readonly BytesRef NUMFIELDS = new BytesRef("number of fields ");
        internal static readonly BytesRef NAME = new BytesRef("  name ");
        internal static readonly BytesRef NUMBER = new BytesRef("  number ");
        internal static readonly BytesRef ISINDEXED = new BytesRef("  indexed ");
        internal static readonly BytesRef STORETV = new BytesRef("  term vectors ");
        internal static readonly BytesRef STORETVPOS = new BytesRef("  term vector positions ");
        internal static readonly BytesRef STORETVOFF = new BytesRef("  term vector offsets ");
        internal static readonly BytesRef PAYLOADS = new BytesRef("  payloads ");
        internal static readonly BytesRef NORMS = new BytesRef("  norms ");
        internal static readonly BytesRef NORMS_TYPE = new BytesRef("  norms type ");
        internal static readonly BytesRef DOCVALUES = new BytesRef("  doc values ");
        internal static readonly BytesRef DOCVALUES_GEN = new BytesRef("  doc values gen ");
        internal static readonly BytesRef INDEXOPTIONS = new BytesRef("  index options ");
        internal static readonly BytesRef NUM_ATTS = new BytesRef("  attributes ");
        internal static readonly BytesRef ATT_KEY = new BytesRef("    key ");
        internal static readonly BytesRef ATT_VALUE = new BytesRef("    value ");

        public override void Write(Directory directory, string segmentName, string segmentSuffix, FieldInfos infos,
            IOContext context)
        {
            var fileName = IndexFileNames.SegmentFileName(segmentName, segmentSuffix, FIELD_INFOS_EXTENSION);
            var output = directory.CreateOutput(fileName, context);
            var scratch = new BytesRef();
            var success = false;

            try
            {
                SimpleTextUtil.Write(output, NUMFIELDS);
                SimpleTextUtil.Write(output, infos.Count.ToString(CultureInfo.InvariantCulture), scratch);
                SimpleTextUtil.WriteNewline(output);

                foreach (FieldInfo fi in infos)
                {
                    SimpleTextUtil.Write(output, NAME);
                    SimpleTextUtil.Write(output, fi.Name, scratch);
                    SimpleTextUtil.WriteNewline(output);

                    SimpleTextUtil.Write(output, NUMBER);
                    SimpleTextUtil.Write(output, fi.Number.ToString(CultureInfo.InvariantCulture), scratch);
                    SimpleTextUtil.WriteNewline(output);

                    SimpleTextUtil.Write(output, ISINDEXED);
                    SimpleTextUtil.Write(output, CultureInfo.InvariantCulture.TextInfo.ToLower(fi.IsIndexed.ToString()), scratch);
                    SimpleTextUtil.WriteNewline(output);

                    if (fi.IsIndexed)
                    {
                        // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                        if (Debugging.AssertsEnabled) Debugging.Assert(IndexOptionsComparer.Default.Compare(fi.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 || !fi.HasPayloads);
                        SimpleTextUtil.Write(output, INDEXOPTIONS);
                        SimpleTextUtil.Write(output, 
                            fi.IndexOptions != IndexOptions.NONE ? fi.IndexOptions.ToString() : string.Empty, 
                            scratch);
                        SimpleTextUtil.WriteNewline(output);
                    }

                    SimpleTextUtil.Write(output, STORETV);
                    SimpleTextUtil.Write(output, CultureInfo.InvariantCulture.TextInfo.ToLower(fi.HasVectors.ToString()), scratch);
                    SimpleTextUtil.WriteNewline(output);

                    SimpleTextUtil.Write(output, PAYLOADS);
                    SimpleTextUtil.Write(output, CultureInfo.InvariantCulture.TextInfo.ToLower(fi.HasPayloads.ToString()), scratch);
                    SimpleTextUtil.WriteNewline(output);

                    SimpleTextUtil.Write(output, NORMS);
                    SimpleTextUtil.Write(output, CultureInfo.InvariantCulture.TextInfo.ToLower((!fi.OmitsNorms).ToString()), scratch);
                    SimpleTextUtil.WriteNewline(output);

                    SimpleTextUtil.Write(output, NORMS_TYPE);
                    SimpleTextUtil.Write(output, GetDocValuesType(fi.NormType), scratch);
                    SimpleTextUtil.WriteNewline(output);

                    SimpleTextUtil.Write(output, DOCVALUES);
                    SimpleTextUtil.Write(output, GetDocValuesType(fi.DocValuesType), scratch);
                    SimpleTextUtil.WriteNewline(output);

                    SimpleTextUtil.Write(output, DOCVALUES_GEN);
                    SimpleTextUtil.Write(output, fi.DocValuesGen.ToString(CultureInfo.InvariantCulture), scratch);
                    SimpleTextUtil.WriteNewline(output);

                    IDictionary<string, string> atts = fi.Attributes;
                    int numAtts = atts is null ? 0 : atts.Count;
                    SimpleTextUtil.Write(output, NUM_ATTS);
                    SimpleTextUtil.Write(output, numAtts.ToString(CultureInfo.InvariantCulture), scratch);
                    SimpleTextUtil.WriteNewline(output);

                    if (numAtts <= 0 || atts is null) continue;
                    foreach (var entry in atts)
                    {
                        SimpleTextUtil.Write(output, ATT_KEY);
                        SimpleTextUtil.Write(output, entry.Key, scratch);
                        SimpleTextUtil.WriteNewline(output);

                        SimpleTextUtil.Write(output, ATT_VALUE);
                        SimpleTextUtil.Write(output, entry.Value, scratch);
                        SimpleTextUtil.WriteNewline(output);
                    }
                }
                SimpleTextUtil.WriteChecksum(output, scratch);
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

        private static string GetDocValuesType(DocValuesType type)
        {
            // LUCENENET specific - need to write false for NONE
            return type != DocValuesType.NONE ? type.ToString() : "false";
        }
    }
}