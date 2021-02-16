using J2N.Numerics;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    using DocValuesType = Lucene.Net.Index.DocValuesType;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Lucene 4.0 FieldInfos reader.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    /// <seealso cref="Lucene40FieldInfosFormat"/>
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

                int size = input.ReadVInt32(); //read in the size
                FieldInfo[] infos = new FieldInfo[size];

                for (int i = 0; i < size; i++)
                {
                    string name = input.ReadString();
                    int fieldNumber = input.ReadVInt32();
                    byte bits = input.ReadByte();
                    bool isIndexed = (bits & Lucene40FieldInfosFormat.IS_INDEXED) != 0;
                    bool storeTermVector = (bits & Lucene40FieldInfosFormat.STORE_TERMVECTOR) != 0;
                    bool omitNorms = (bits & Lucene40FieldInfosFormat.OMIT_NORMS) != 0;
                    bool storePayloads = (bits & Lucene40FieldInfosFormat.STORE_PAYLOADS) != 0;
                    IndexOptions indexOptions;
                    if (!isIndexed)
                    {
                        indexOptions = IndexOptions.NONE;
                    }
                    else if ((bits & Lucene40FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS) != 0)
                    {
                        indexOptions = IndexOptions.DOCS_ONLY;
                    }
                    else if ((bits & Lucene40FieldInfosFormat.OMIT_POSITIONS) != 0)
                    {
                        indexOptions = IndexOptions.DOCS_AND_FREQS;
                    }
                    else if ((bits & Lucene40FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS) != 0)
                    {
                        indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                    }
                    else
                    {
                        indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    }

                    // LUCENE-3027: past indices were able to write
                    // storePayloads=true when omitTFAP is also true,
                    // which is invalid.  We correct that, here:
                    // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                    if (isIndexed && IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                    {
                        storePayloads = false;
                    }
                    // DV Types are packed in one byte
                    byte val = input.ReadByte();
                    LegacyDocValuesType oldValuesType = GetDocValuesType((sbyte)(val & 0x0F));
                    LegacyDocValuesType oldNormsType = GetDocValuesType((sbyte)(val.TripleShift(4) & 0x0F));
                    IDictionary<string, string> attributes = input.ReadStringStringMap();
                    if (oldValuesType.GetMapping() != DocValuesType.NONE)
                    {
                        attributes[LEGACY_DV_TYPE_KEY] = oldValuesType.ToString();
                    }
                    if (oldNormsType.GetMapping() != DocValuesType.NONE)
                    {
                        if (oldNormsType.GetMapping() != DocValuesType.NUMERIC)
                        {
                            throw new CorruptIndexException("invalid norm type: " + oldNormsType + " (resource=" + input + ")");
                        }
                        attributes[LEGACY_NORM_TYPE_KEY] = oldNormsType.ToString();
                    }
                    infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads, indexOptions, oldValuesType.GetMapping(), oldNormsType.GetMapping(), attributes);
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
                    IOUtils.DisposeWhileHandlingException(input);
                }
            }
        }

        internal static readonly string LEGACY_DV_TYPE_KEY = typeof(Lucene40FieldInfosReader).Name + ".dvtype";
        internal static readonly string LEGACY_NORM_TYPE_KEY = typeof(Lucene40FieldInfosReader).Name + ".normtype";

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
            //return LegacyDocValuesType.Values[b];
            return (LegacyDocValuesType)b;
        }
    }

    internal enum LegacyDocValuesType : sbyte
    {
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
    }

    internal static class LegacyDocValuesTypeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DocValuesType GetMapping(this LegacyDocValuesType legacyDocValuesType)
        {
            return mapping[legacyDocValuesType];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LegacyDocValuesType ToLegacyDocValuesType(this string name) // Was ValueOf in Java
        {
            return (LegacyDocValuesType)Enum.Parse(typeof(LegacyDocValuesType), name);
        }

        // mapping of 4.0 types -> 4.2 types
        internal static IDictionary<LegacyDocValuesType, DocValuesType> mapping = new Dictionary<LegacyDocValuesType, DocValuesType>
        {
            { LegacyDocValuesType.NONE, DocValuesType.NONE },
            { LegacyDocValuesType.VAR_INTS, DocValuesType.NUMERIC },
            { LegacyDocValuesType.FLOAT_32, DocValuesType.NUMERIC },
            { LegacyDocValuesType.FLOAT_64, DocValuesType.NUMERIC },
            { LegacyDocValuesType.BYTES_FIXED_STRAIGHT, DocValuesType.BINARY },
            { LegacyDocValuesType.BYTES_FIXED_DEREF, DocValuesType.BINARY },
            { LegacyDocValuesType.BYTES_VAR_STRAIGHT, DocValuesType.BINARY },
            { LegacyDocValuesType.BYTES_VAR_DEREF, DocValuesType.BINARY },
            { LegacyDocValuesType.FIXED_INTS_16, DocValuesType.NUMERIC },
            { LegacyDocValuesType.FIXED_INTS_32, DocValuesType.NUMERIC },
            { LegacyDocValuesType.FIXED_INTS_64, DocValuesType.NUMERIC },
            { LegacyDocValuesType.FIXED_INTS_8, DocValuesType.NUMERIC },
            { LegacyDocValuesType.BYTES_FIXED_SORTED, DocValuesType.SORTED },
            { LegacyDocValuesType.BYTES_VAR_SORTED, DocValuesType.SORTED }
        };
    }
}