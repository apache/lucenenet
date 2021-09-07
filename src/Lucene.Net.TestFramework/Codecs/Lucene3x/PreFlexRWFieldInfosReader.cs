using Lucene.Net.Index;
using Lucene.Net.Store;
using System.Collections.Generic;

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
    internal class PreFlexRWFieldInfosReader : FieldInfosReader
    {
        internal const int FORMAT_MINIMUM = PreFlexRWFieldInfosWriter.FORMAT_START;

        public override FieldInfos Read(Directory directory, string segmentName, string segmentSuffix, IOContext iocontext)
        {
            string fileName = IndexFileNames.SegmentFileName(segmentName, "", PreFlexRWFieldInfosWriter.FIELD_INFOS_EXTENSION);
            IndexInput input = directory.OpenInput(fileName, iocontext);

            try
            {
                int format = input.ReadVInt32();

                if (format > FORMAT_MINIMUM)
                {
                    throw new IndexFormatTooOldException(input, format, FORMAT_MINIMUM, PreFlexRWFieldInfosWriter.FORMAT_CURRENT);
                }
                if (format < PreFlexRWFieldInfosWriter.FORMAT_CURRENT && format != PreFlexRWFieldInfosWriter.FORMAT_PREFLEX_RW)
                {
                    throw new IndexFormatTooNewException(input, format, FORMAT_MINIMUM, PreFlexRWFieldInfosWriter.FORMAT_CURRENT);
                }

                int size = input.ReadVInt32(); //read in the size
                FieldInfo[] infos = new FieldInfo[size];

                for (int i = 0; i < size; i++)
                {
                    string name = input.ReadString();
                    int fieldNumber = format == PreFlexRWFieldInfosWriter.FORMAT_PREFLEX_RW ? input.ReadInt32() : i;
                    byte bits = input.ReadByte();
                    bool isIndexed = (bits & PreFlexRWFieldInfosWriter.IS_INDEXED) != 0;
                    bool storeTermVector = (bits & PreFlexRWFieldInfosWriter.STORE_TERMVECTOR) != 0;
                    bool omitNorms = (bits & PreFlexRWFieldInfosWriter.OMIT_NORMS) != 0;
                    bool storePayloads = (bits & PreFlexRWFieldInfosWriter.STORE_PAYLOADS) != 0;
                    IndexOptions indexOptions;
                    if (!isIndexed)
                    {
                        indexOptions = IndexOptions.NONE;
                    }
                    else if ((bits & PreFlexRWFieldInfosWriter.OMIT_TERM_FREQ_AND_POSITIONS) != 0)
                    {
                        indexOptions = IndexOptions.DOCS_ONLY;
                    }
                    else if ((bits & PreFlexRWFieldInfosWriter.OMIT_POSITIONS) != 0)
                    {
                        if (format <= PreFlexRWFieldInfosWriter.FORMAT_OMIT_POSITIONS)
                        {
                            indexOptions = IndexOptions.DOCS_AND_FREQS;
                        }
                        else
                        {
                            throw new CorruptIndexException("Corrupt fieldinfos, OMIT_POSITIONS set but format=" + format + " (resource: " + input + ")");
                        }
                    }
                    else
                    {
                        indexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    }

                    // LUCENE-3027: past indices were able to write
                    // storePayloads=true when omitTFAP is also true,
                    // which is invalid.  We correct that, here:
                    if (indexOptions != IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    {
                        storePayloads = false;
                    }

                    DocValuesType normType = isIndexed && !omitNorms ? DocValuesType.NUMERIC : DocValuesType.NONE;
                    if (format == PreFlexRWFieldInfosWriter.FORMAT_PREFLEX_RW && normType != DocValuesType.NONE)
                    {
                        // RW can have norms but doesn't write them
                        normType = input.ReadByte() != 0 ? DocValuesType.NUMERIC : DocValuesType.NONE;
                    }

                    infos[i] = new FieldInfo(name, 
                                            isIndexed, 
                                            fieldNumber, 
                                            storeTermVector, 
                                            omitNorms, 
                                            storePayloads, 
                                            indexOptions, 
                                            DocValuesType.NONE, 
                                            normType, 
                                            null);
                }

                if (input.Position != input.Length) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                {
                    throw new CorruptIndexException("did not read all bytes from file \"" + fileName + "\": read " + input.Position + " vs size " + input.Length + " (resource: " + input + ")");
                }
                return new FieldInfos(infos);
            }
            finally
            {
                input.Dispose();
            }
        }

        //public static void Files(Directory dir, SegmentInfo info, ISet<string> files) // LUCENENET: Not used
        //{
        //    files.Add(IndexFileNames.SegmentFileName(info.Name, "", PreFlexRWFieldInfosWriter.FIELD_INFOS_EXTENSION));
        //}
    }
}