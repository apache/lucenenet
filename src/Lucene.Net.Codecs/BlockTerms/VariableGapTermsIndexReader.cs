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

namespace Lucene.Net.Codecs.BlockTerms
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Lucene.Net.Util;
    using Lucene.Net.Util.Fst;

/** See {@link VariableGapTermsIndexWriter}
 * 
 * @lucene.experimental */

    public class VariableGapTermsIndexReader : TermsIndexReaderBase
    {

        private readonly PositiveIntOutputs fstOutputs = PositiveIntOutputs.Singleton;
        private readonly int indexDivisor;
        private readonly IndexInput input;       // Closed if indexLoaded is true:
        private volatile bool indexLoaded;

        private readonly Dictionary<FieldInfo, FieldIndexData> fields = new Dictionary<FieldInfo, FieldIndexData>();

        private long dirOffset;                 // start of the field info data
        private readonly int version;
        private readonly String segment;

        public VariableGapTermsIndexReader(Directory dir, FieldInfos fieldInfos, String segment, int indexDivisor,
            String segmentSuffix, IOContext context)
        {
            input =
                dir.OpenInput(
                    IndexFileNames.SegmentFileName(segment, segmentSuffix,
                        VariableGapTermsIndexWriter.TERMS_INDEX_EXTENSION), new IOContext(context, true));
            this.segment = segment;
            bool success = false;

            Debug.Debug.Assert((indexDivisor == -1 || indexDivisor > 0);

            try
            {

                version = readHeader(input);
                this.indexDivisor = indexDivisor;

                if (version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
                {
                    CodecUtil.ChecksumEntireFile(input);
                }

                SeekDir(in,
                dirOffset)
                ;

                // Read directory
                int numFields = input.ReadVInt();
                if (numFields < 0)
                {
                    throw new CorruptIndexException("invalid numFields: " + numFields + " (resource=" + input + ")");
                }

                for (int i = 0; i < numFields; i++)
                {
                    final
                    int field = in.
                    readVInt();
                    final
                    long indexStart = in.
                    readVLong();
                    final
                    FieldInfo fieldInfo = fieldInfos.fieldInfo(field);
                    FieldIndexData previous = fields.put(fieldInfo, new FieldIndexData(fieldInfo, indexStart));
                    if (previous != null)
                    {
                        throw new CorruptIndexException("duplicate field: " + fieldInfo.name + " (resource=" +in + ")" )
                        ;
                    }
                }
                success = true;
            }
            finally
            {
                if (indexDivisor > 0)
                {
                in.
                    close();
                    in =
                    null;
                    if (success)
                    {
                        indexLoaded = true;
                    }
                }
            }
        }


        private int ReadHeader(IndexInput input)
        {
            int version = CodecUtil.CheckHeader(input, VariableGapTermsIndexWriter.CODEC_NAME,
                VariableGapTermsIndexWriter.VERSION_START, VariableGapTermsIndexWriter.VERSION_CURRENT);
            if (version < VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                dirOffset = input.ReadLong();
            }
            return version;
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override bool SupportsOrd()
        {
            return false;
        }

        public override int GetDivisor()
        {
            return indexDivisor;
        }

        public override FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo)
        {
            FieldIndexData fieldData = fields[fieldInfo];
            if (fieldData.Fst == null)
            {
                return null;
            }
            else
            {
                return new IndexEnum(fieldData.Fst);
            }
        }

        public override void Close()
        {
            if (input !=
                null && !indexLoaded)
            {
                input.Close();
            }
        }

        private void SeekDir(IndexInput input, long dirOffset)
        {
            if (version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length() - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadLong();
            }
            else if (version >= VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length() - 8);
                dirOffset = input.ReadLong();
            }
            input.Seek(dirOffset);
        }

        public override long RamBytesUsed()
        {
            long sizeInBytes = 0;

            foreach (var entry in fields.Values)
            {
                sizeInBytes += entry.RamBytesUsed();
            }

            return sizeInBytes;
        }


        internal class FieldIndexData
        {

            private readonly long indexStart;
            // Set only if terms index is loaded:
            public volatile FST<long> Fst;

            public FieldIndexData(FieldInfo fieldInfo, long indexStart)
            {
                this.indexStart = indexStart;

                if (indexDivisor > 0)
                {
                    loadTermsIndex();
                }
            }

            private void loadTermsIndex()
            {
                if (Fst == null)
                {
                    IndexInput clone = input.Clone();
                    clone.Seek(indexStart);
                    Fst = new FST<>(clone, fstOutputs);
                    clone.Close();

                    /*
        final String dotFileName = segment + "_" + fieldInfo.name + ".dot";
        Writer w = new OutputStreamWriter(new FileOutputStream(dotFileName));
        Util.toDot(fst, w, false, false);
        System.out.println("FST INDEX: SAVED to " + dotFileName);
        w.close();
        */

                    if (indexDivisor > 1)
                    {
                        // subsample
                        IntsRef scratchIntsRef = new IntsRef();
                        PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
                        Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, outputs);
                        BytesRefFSTEnum<long> fstEnum = new BytesRefFSTEnum<long>(fst);
                        BytesRefFSTEnum.InputOutput<long> result;
                        int count = indexDivisor;
                        while ((result = fstEnum.Next()) != null)
                        {
                            if (count == indexDivisor)
                            {
                                builder.Add(Util.ToIntsRef(result.Input, scratchIntsRef), result.Output);
                                count = 0;
                            }
                            count++;
                        }
                        Fst = builder.Finish();
                    }
                }
            }

            /** Returns approximate RAM bytes used */

            public long RamBytesUsed()
            {
                return Fst == null ? 0 : Fst.SizeInBytes();
            }
        }

        internal class IndexEnum : FieldIndexEnum
        {
            private readonly BytesRefFSTEnum<long> fstEnum;
            private BytesRefFSTEnum<long>.InputOutput<long> current;

            public IndexEnum(FST<long> fst)
            {
                fstEnum = new BytesRefFSTEnum<long>(fst);
            }

            public override BytesRef Term()
            {
                if (current == null)
                {
                    return null;
                }
                else
                {
                    return current.Input;
                }
            }

            public override long Seek(BytesRef target)
            {
                //System.out.println("VGR: seek field=" + fieldInfo.name + " target=" + target);
                current = fstEnum.SeekFloor(target);
                //System.out.println("  got input=" + current.input + " output=" + current.output);
                return current.Output;
            }

            public override long Next()
            {
                //System.out.println("VGR: next field=" + fieldInfo.name);
                current = fstEnum.Next();
                if (current == null)
                {
                    //System.out.println("  eof");
                    return -1;
                }
                else
                {
                    return current.Output;
                }
            }

            public override long Ord()
            {
                throw new NotImplementedException();
            }

            public override long Seek(long ord)
            {
                throw new NotImplementedException();
            }
        }

    }
}
