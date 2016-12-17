using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene40
{
    using Lucene.Net.Support;

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
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Lucene 4.0 FieldInfos reader.
    ///
    /// @lucene.experimental </summary>
    /// <seealso cref= Lucene40FieldInfosFormat </seealso>
    /// @deprecated Only for reading old 4.0 and 4.1 segments
    [Obsolete("Only for reading old 4.0 and 4.1 segments")]
    internal class Lucene40FieldInfosReader : FieldInfosReader
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40FieldInfosReader()
        {
        }

        public override FieldInfos Read(Directory directory, string segmentName, string segmentSuffix, IOContext iocontext)
        {
            string fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene40FieldInfosFormat.FIELD_INFOS_EXTENSION);
            IndexInput input = directory.OpenInput(fileName, iocontext);

            bool success = false;
            try
            {
                CodecUtil.CheckHeader(input, Lucene40FieldInfosFormat.CODEC_NAME, Lucene40FieldInfosFormat.FORMAT_START, Lucene40FieldInfosFormat.FORMAT_CURRENT);

                int size = input.ReadVInt(); //read in the size
                FieldInfo[] infos = new FieldInfo[size];

                for (int i = 0; i < size; i++)
                {
                    string name = input.ReadString();
                    int fieldNumber = input.ReadVInt();
                    byte bits = input.ReadByte();
                    bool isIndexed = (bits & Lucene40FieldInfosFormat.IS_INDEXED) != 0;
                    bool storeTermVector = (bits & Lucene40FieldInfosFormat.STORE_TERMVECTOR) != 0;
                    bool omitNorms = (bits & Lucene40FieldInfosFormat.OMIT_NORMS) != 0;
                    bool storePayloads = (bits & Lucene40FieldInfosFormat.STORE_PAYLOADS) != 0;
                    FieldInfo.IndexOptions indexOptions;
                    if (!isIndexed)
                    {
                        indexOptions = default(FieldInfo.IndexOptions);
                    }
                    else if ((bits & Lucene40FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS) != 0)
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
                    }
                    else if ((bits & Lucene40FieldInfosFormat.OMIT_POSITIONS) != 0)
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;
                    }
                    else if ((bits & Lucene40FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS) != 0)
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                    }
                    else
                    {
                        indexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    }

                    // LUCENE-3027: past indices were able to write
                    // storePayloads=true when omitTFAP is also true,
                    // which is invalid.  We correct that, here:
                    if (isIndexed && indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                    {
                        storePayloads = false;
                    }
                    // DV Types are packed in one byte
                    byte val = input.ReadByte();
                    LegacyDocValuesType oldValuesType = GetDocValuesType((sbyte)(val & 0x0F));
                    LegacyDocValuesType oldNormsType = GetDocValuesType((sbyte)(((int)((uint)val >> 4)) & 0x0F));
                    IDictionary<string, string> attributes = input.ReadStringStringMap();
                    if (oldValuesType.Mapping != null)
                    {
                        attributes[LEGACY_DV_TYPE_KEY] = oldValuesType.Name;
                    }
                    if (oldNormsType.Mapping != null)
                    {
                        if (oldNormsType.Mapping != FieldInfo.DocValuesType_e.NUMERIC)
                        {
                            throw new CorruptIndexException("invalid norm type: " + oldNormsType + " (resource=" + input + ")");
                        }
                        attributes[LEGACY_NORM_TYPE_KEY] = oldNormsType.Name;
                    }
                    infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads, indexOptions, oldValuesType.Mapping, oldNormsType.Mapping, attributes);
                }
                CodecUtil.CheckEOF(input);
                FieldInfos fieldInfos = new FieldInfos(infos);
                success = true;
                return fieldInfos;
            }
            finally
            {
                if (success)
                {
                    input.Dispose();
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(input);
                }
            }
        }

        internal static readonly string LEGACY_DV_TYPE_KEY = typeof(Lucene40FieldInfosReader).Name + ".dvtype";
        internal static readonly string LEGACY_NORM_TYPE_KEY = typeof(Lucene40FieldInfosReader).Name + ".normtype";

        internal class LegacyDocValuesType
        {
            internal static readonly LegacyDocValuesType NONE = new LegacyDocValuesType("NONE", null);
            internal static readonly LegacyDocValuesType VAR_INTS = new LegacyDocValuesType("VAR_INTS", FieldInfo.DocValuesType_e.NUMERIC);
            internal static readonly LegacyDocValuesType FLOAT_32 = new LegacyDocValuesType("FLOAT_32", FieldInfo.DocValuesType_e.NUMERIC);
            internal static readonly LegacyDocValuesType FLOAT_64 = new LegacyDocValuesType("FLOAT_64", FieldInfo.DocValuesType_e.NUMERIC);
            internal static readonly LegacyDocValuesType BYTES_FIXED_STRAIGHT = new LegacyDocValuesType("BYTES_FIXED_STRAIGHT", FieldInfo.DocValuesType_e.BINARY);
            internal static readonly LegacyDocValuesType BYTES_FIXED_DEREF = new LegacyDocValuesType("BYTES_FIXED_DEREF", FieldInfo.DocValuesType_e.BINARY);
            internal static readonly LegacyDocValuesType BYTES_VAR_STRAIGHT = new LegacyDocValuesType("BYTES_VAR_STRAIGHT", FieldInfo.DocValuesType_e.BINARY);
            internal static readonly LegacyDocValuesType BYTES_VAR_DEREF = new LegacyDocValuesType("BYTES_VAR_DEREF", FieldInfo.DocValuesType_e.BINARY);
            internal static readonly LegacyDocValuesType FIXED_INTS_16 = new LegacyDocValuesType("FIXED_INTS_16", FieldInfo.DocValuesType_e.NUMERIC);
            internal static readonly LegacyDocValuesType FIXED_INTS_32 = new LegacyDocValuesType("FIXED_INTS_32", FieldInfo.DocValuesType_e.NUMERIC);
            internal static readonly LegacyDocValuesType FIXED_INTS_64 = new LegacyDocValuesType("FIXED_INTS_64", FieldInfo.DocValuesType_e.NUMERIC);
            internal static readonly LegacyDocValuesType FIXED_INTS_8 = new LegacyDocValuesType("FIXED_INTS_8", FieldInfo.DocValuesType_e.NUMERIC);
            internal static readonly LegacyDocValuesType BYTES_FIXED_SORTED = new LegacyDocValuesType("BYTES_FIXED_SORTED", FieldInfo.DocValuesType_e.SORTED);
            internal static readonly LegacyDocValuesType BYTES_VAR_SORTED = new LegacyDocValuesType("BYTES_VAR_SORTED", FieldInfo.DocValuesType_e.SORTED);

            private static readonly LegacyDocValuesType[] values = new[] {
                NONE,
                VAR_INTS,
                FLOAT_32,
                FLOAT_64,
                BYTES_FIXED_STRAIGHT,
                BYTES_FIXED_DEREF,
                BYTES_VAR_STRAIGHT,
                BYTES_VAR_DEREF,
                FIXED_INTS_16,
                FIXED_INTS_32,
                FIXED_INTS_64,
                FIXED_INTS_8,
                BYTES_FIXED_SORTED,
                BYTES_VAR_SORTED
            };

            private static readonly IDictionary<string, LegacyDocValuesType> nameLookup = new HashMap<string, LegacyDocValuesType>(StringComparer.OrdinalIgnoreCase) {
                {"NONE", NONE},
                {"VAR_INTS", VAR_INTS},
                {"FLOAT_32", FLOAT_32},
                {"FLOAT_64", FLOAT_64},
                {"BYTES_FIXED_STRAIGHT", BYTES_FIXED_STRAIGHT},
                {"BYTES_FIXED_DEREF", BYTES_FIXED_DEREF},
                {"BYTES_VAR_STRAIGHT", BYTES_VAR_STRAIGHT},
                {"BYTES_VAR_DEREF", BYTES_VAR_DEREF},
                {"FIXED_INTS_16", FIXED_INTS_16},
                {"FIXED_INTS_32", FIXED_INTS_32},
                {"FIXED_INTS_64", FIXED_INTS_64},
                {"FIXED_INTS_8", FIXED_INTS_8},
                {"BYTES_FIXED_SORTED", BYTES_FIXED_SORTED},
                {"BYTES_VAR_SORTED", BYTES_VAR_SORTED}
            };

            public static readonly IDictionary<string, int> ordinalLookup = new HashMap<string, int>(14) {
                {"NONE", 0},
                {"VAR_INTS", 1},
                {"FLOAT_32", 2},
                {"FLOAT_64", 3},
                {"BYTES_FIXED_STRAIGHT", 4},
                {"BYTES_FIXED_DEREF", 5},
                {"BYTES_VAR_STRAIGHT", 6},
                {"BYTES_VAR_DEREF", 7},
                {"FIXED_INTS_16", 8},
                {"FIXED_INTS_32", 9},
                {"FIXED_INTS_64", 10},
                {"FIXED_INTS_8", 11},
                {"BYTES_FIXED_SORTED", 12},
                {"BYTES_VAR_SORTED", 13}
            };

            private readonly FieldInfo.DocValuesType_e? mapping;
            private readonly string name;

            private LegacyDocValuesType(string name, FieldInfo.DocValuesType_e? mapping)
            {
                this.name = name;
                this.mapping = mapping;
            }

            public FieldInfo.DocValuesType_e? Mapping // LUCENENET TODO: Can we make this non-nullable?
            {
                get { return mapping; }
            }

            public string Name
            {
                get { return name; }
            }

            public static LegacyDocValuesType[] Values
            {
                get
                {
                    return values;
                }
            }

            public static LegacyDocValuesType ValueOf(string value)
            {
                return nameLookup[value];
            }
        }

        // mapping of 4.0 types -> 4.2 types
        /*internal enum LegacyDocValuesType
        {
          NONE = null,
          VAR_INTS = Lucene.Net.Index.FieldInfo.DocValuesType_e.NUMERIC,
          FLOAT_32 = Lucene.Net.Index.FieldInfo.DocValuesType_e.NUMERIC,
          FLOAT_64 = Lucene.Net.Index.FieldInfo.DocValuesType_e.NUMERIC,
          BYTES_FIXED_STRAIGHT = Lucene.Net.Index.FieldInfo.DocValuesType_e.BINARY,
          BYTES_FIXED_DEREF = Lucene.Net.Index.FieldInfo.DocValuesType_e.BINARY,
          BYTES_VAR_STRAIGHT = Lucene.Net.Index.FieldInfo.DocValuesType_e.BINARY,
          BYTES_VAR_DEREF = Lucene.Net.Index.FieldInfo.DocValuesType_e.BINARY,
          FIXED_INTS_16 = Lucene.Net.Index.FieldInfo.DocValuesType_e.NUMERIC,
          FIXED_INTS_32 = Lucene.Net.Index.FieldInfo.DocValuesType_e.NUMERIC,
          FIXED_INTS_64 = Lucene.Net.Index.FieldInfo.DocValuesType_e.NUMERIC,
          FIXED_INTS_8 = Lucene.Net.Index.FieldInfo.DocValuesType_e.NUMERIC,
          BYTES_FIXED_SORTED = Lucene.Net.Index.FieldInfo.DocValuesType_e.SORTED,
          BYTES_VAR_SORTED = Lucene.Net.Index.FieldInfo.DocValuesType_e.SORTED
        }*/

        // decodes a 4.0 type
        private static LegacyDocValuesType GetDocValuesType(sbyte b)
        {
            return LegacyDocValuesType.Values[b];
        }
    }
}