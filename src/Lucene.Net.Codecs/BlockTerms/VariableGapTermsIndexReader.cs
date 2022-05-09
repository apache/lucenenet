using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Codecs.BlockTerms
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
    /// See <see cref="VariableGapTermsIndexWriter"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class VariableGapTermsIndexReader : TermsIndexReaderBase
    {
        private readonly PositiveInt32Outputs fstOutputs = PositiveInt32Outputs.Singleton;
        private readonly int indexDivisor;

        // Closed if indexLoaded is true:
        private readonly IndexInput input;
        private readonly /*volatile*/ bool indexLoaded;

        private readonly IDictionary<FieldInfo, FieldIndexData> fields = new Dictionary<FieldInfo, FieldIndexData>();

        // start of the field info data
        private long dirOffset;

        private readonly int version;

        //private readonly string segment; // LUCENENET: Not used

        public VariableGapTermsIndexReader(Directory dir, FieldInfos fieldInfos, string segment, int indexDivisor,
            string segmentSuffix, IOContext context)
        {
            input = dir.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, VariableGapTermsIndexWriter.TERMS_INDEX_EXTENSION), new IOContext(context, true));
            //this.segment = segment; // LUCENENET: Not used
            bool success = false;
            if (Debugging.AssertsEnabled) Debugging.Assert(indexDivisor == -1 || indexDivisor > 0);

            try
            {
                version = ReadHeader(input);
                this.indexDivisor = indexDivisor;

                if (version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
                {
                    CodecUtil.ChecksumEntireFile(input);
                }

                SeekDir(input, dirOffset);

                // Read directory
                int numFields = input.ReadVInt32();
                if (numFields < 0)
                {
                    throw new CorruptIndexException("invalid numFields: " + numFields + " (resource=" + input + ")");
                }

                for (int i = 0; i < numFields; i++)
                {
                    int field = input.ReadVInt32();
                    long indexStart = input.ReadVInt64();
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                    FieldIndexData previous = fields.Put(fieldInfo, new FieldIndexData(this, /* fieldInfo, // LUCENENET: Not referenced */ indexStart));
                    if (previous != null)
                    {
                        throw new CorruptIndexException("duplicate field: " + fieldInfo.Name + " (resource=" + input + ")");
                    }
                }
                success = true;
            }
            finally
            {
                if (indexDivisor > 0)
                {
                    input.Dispose();
                    input = null;
                    if (success)
                    {
                        indexLoaded = true;
                    }
                }
            }
        }

        public override int Divisor => indexDivisor;

        private int ReadHeader(IndexInput input)
        {
            int version = CodecUtil.CheckHeader(input, VariableGapTermsIndexWriter.CODEC_NAME,
                VariableGapTermsIndexWriter.VERSION_START, VariableGapTermsIndexWriter.VERSION_CURRENT);
            if (version < VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                dirOffset = input.ReadInt64();
            }
            return version;
        }

        private class IndexEnum : FieldIndexEnum
        {
            private readonly BytesRefFSTEnum<Int64> fstEnum;
            private BytesRefFSTEnum.InputOutput<Int64> current;

            public IndexEnum(FST<Int64> fst)
            {
                fstEnum = new BytesRefFSTEnum<Int64>(fst);
            }

            public override BytesRef Term
            {
                get
                {
                    if (current is null)
                    {
                        return null;
                    }
                    else
                    {
                        return current.Input;
                    }
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
                if (!fstEnum.MoveNext())
                {
                    current = null;
                    //System.out.println("  eof");
                    return -1;
                }
                else
                {
                    current = fstEnum.Current;
                    return current.Output;
                }
            }

            public override long Ord => throw UnsupportedOperationException.Create();

            public override long Seek(long ord)
            {
                throw UnsupportedOperationException.Create();
            }
        }

        public override bool SupportsOrd => false;

        private class FieldIndexData
        {
            private readonly VariableGapTermsIndexReader outerInstance;

            private readonly long indexStart;
            // Set only if terms index is loaded:
            internal volatile FST<Int64> fst;

            public FieldIndexData(VariableGapTermsIndexReader outerInstance, /*FieldInfo fieldInfo, // LUCENENET: Not referenced */ long indexStart)
            {
                this.outerInstance = outerInstance;

                this.indexStart = indexStart;

                if (outerInstance.indexDivisor > 0)
                {
                    LoadTermsIndex();
                }
            }

            private void LoadTermsIndex()
            {
                if (fst is null)
                {
                    using (IndexInput clone = (IndexInput)outerInstance.input.Clone())
                    {
                        clone.Seek(indexStart);
                        fst = new FST<Int64>(clone, outerInstance.fstOutputs);
                    } // clone.Dispose();

                    /*
                    final String dotFileName = segment + "_" + fieldInfo.name + ".dot";
                    Writer w = new OutputStreamWriter(new FileOutputStream(dotFileName));
                    Util.toDot(fst, w, false, false);
                    System.out.println("FST INDEX: SAVED to " + dotFileName);
                    w.close();
                    */

                    if (outerInstance.indexDivisor > 1)
                    {
                        // subsample
                        Int32sRef scratchIntsRef = new Int32sRef();
                        PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
                        Builder<Int64> builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, outputs);
                        BytesRefFSTEnum<Int64> fstEnum = new BytesRefFSTEnum<Int64>(fst);
                        BytesRefFSTEnum.InputOutput<Int64> result;
                        int count = outerInstance.indexDivisor;
                        while (fstEnum.MoveNext())
                        {
                            result = fstEnum.Current;
                            if (count == outerInstance.indexDivisor)
                            {
                                builder.Add(Util.Fst.Util.ToInt32sRef(result.Input, scratchIntsRef), result.Output);
                                count = 0;
                            }
                            count++;
                        }
                        fst = builder.Finish();
                    }
                }
            }

            /// <summary>Returns approximate RAM bytes used.</summary>
            public virtual long RamBytesUsed()
            {
                return fst is null ? 0 : fst.GetSizeInBytes();
            }
        }

        public override FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo)
        {
            if (!fields.TryGetValue(fieldInfo, out FieldIndexData fieldData) || fieldData is null || fieldData.fst is null)
            {
                return null;
            }
            else
            {
                return new IndexEnum(fieldData.fst);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (input != null && !indexLoaded)
                {
                    input.Dispose();
                }
            }
        }

        private void SeekDir(IndexInput input, long dirOffset)
        {
            if (version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadInt64();
            }
            else if (version >= VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length - 8);
                dirOffset = input.ReadInt64();
            }
            input.Seek(dirOffset);
        }

        public override long RamBytesUsed()
        {
            long sizeInBytes = 0;
            foreach (FieldIndexData entry in fields.Values)
            {
                sizeInBytes += entry.RamBytesUsed();
            }
            return sizeInBytes;
        }
    }
}
