using Lucene.Net.Support;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene46
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

    using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using DocValuesType_e = Lucene.Net.Index.DocValuesType_e;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Lucene 4.6 FieldInfos reader.
    ///
    /// @lucene.experimental </summary>
    /// <seealso cref= Lucene46FieldInfosFormat </seealso>
    internal sealed class Lucene46FieldInfosReader : FieldInfosReader
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene46FieldInfosReader()
        {
        }

        public override FieldInfos Read(Directory directory, string segmentName, string segmentSuffix, IOContext context)
        {
            string fileName = IndexFileNames.SegmentFileName(segmentName, segmentSuffix, Lucene46FieldInfosFormat.EXTENSION);
            ChecksumIndexInput input = directory.OpenChecksumInput(fileName, context);

            bool success = false;
            try
            {
                int codecVersion = CodecUtil.CheckHeader(input, Lucene46FieldInfosFormat.CODEC_NAME, Lucene46FieldInfosFormat.FORMAT_START, Lucene46FieldInfosFormat.FORMAT_CURRENT);

                int size = input.ReadVInt(); //read in the size
                FieldInfo[] infos = new FieldInfo[size];

                for (int i = 0; i < size; i++)
                {
                    string name = input.ReadString();
                    int fieldNumber = input.ReadVInt();
                    byte bits = input.ReadByte();
                    bool isIndexed = (bits & Lucene46FieldInfosFormat.IS_INDEXED) != 0;
                    bool storeTermVector = (bits & Lucene46FieldInfosFormat.STORE_TERMVECTOR) != 0;
                    bool omitNorms = (bits & Lucene46FieldInfosFormat.OMIT_NORMS) != 0;
                    bool storePayloads = (bits & Lucene46FieldInfosFormat.STORE_PAYLOADS) != 0;
                    IndexOptions indexOptions;
                    if (!isIndexed)
                    {
                        indexOptions = default(IndexOptions);
                    }
                    else if ((bits & Lucene46FieldInfosFormat.OMIT_TERM_FREQ_AND_POSITIONS) != 0)
                    {
                        indexOptions = IndexOptions.DOCS_ONLY;
                    }
                    else if ((bits & Lucene46FieldInfosFormat.OMIT_POSITIONS) != 0)
                    {
                        indexOptions = IndexOptions.DOCS_AND_FREQS;
                    }
                    else if ((bits & Lucene46FieldInfosFormat.STORE_OFFSETS_IN_POSTINGS) != 0)
                    {
                        indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                    }
                    else
                    {
                        indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    }

                    // DV Types are packed in one byte
                    byte val = input.ReadByte();
                    DocValuesType_e? docValuesType = GetDocValuesType(input, (sbyte)(val & 0x0F));
                    DocValuesType_e? normsType = GetDocValuesType(input, (sbyte)(((int)((uint)val >> 4)) & 0x0F));
                    long dvGen = input.ReadLong();
                    IDictionary<string, string> attributes = input.ReadStringStringMap();
                    infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads, indexOptions, docValuesType, normsType, CollectionsHelper.UnmodifiableMap(attributes));
                    infos[i].DocValuesGen = dvGen;
                }

                if (codecVersion >= Lucene46FieldInfosFormat.FORMAT_CHECKSUM)
                {
                    CodecUtil.CheckFooter(input);
                }
                else
                {
                    CodecUtil.CheckEOF(input);
                }
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

        private static DocValuesType_e? GetDocValuesType(IndexInput input, sbyte b)
        {
            if (b == 0)
            {
                return default(DocValuesType_e?);
            }
            else if (b == 1)
            {
                return DocValuesType_e.NUMERIC;
            }
            else if (b == 2)
            {
                return DocValuesType_e.BINARY;
            }
            else if (b == 3)
            {
                return DocValuesType_e.SORTED;
            }
            else if (b == 4)
            {
                return DocValuesType_e.SORTED_SET;
            }
            else
            {
                throw new CorruptIndexException("invalid docvalues byte: " + b + " (resource=" + input + ")");
            }
        }
    }
}