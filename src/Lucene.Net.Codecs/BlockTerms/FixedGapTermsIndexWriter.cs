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
 * WITHoutput WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
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
    using Lucene.Net.Util.Packed;

    /// <summary>
    /// Selects every Nth term as and index term, and hold term
    /// bytes (mostly) fully expanded in memory.  This terms index
    /// supports seeking by ord.  See {@link
    /// VariableGapTermsIndexWriter} for a more memory efficient
    /// terms index that does not support seeking by ord.
    ///
    /// @lucene.experimental */    
    /// </summary>
    public class FixedGapTermsIndexWriter : TermsIndexWriterBase
    {
        protected IndexOutput output;

        /** Extension of terms index file */
        private static readonly String TERMS_INDEX_EXTENSION = "tii";
        private static readonly String CODEC_NAME = "SIMPLE_STANDARD_TERMS_INDEX";
        private static readonly int VERSION_START = 0;
        private static readonly int VERSION_APPEND_ONLY = 1;

        private static readonly int VERSION_CHECKSUM = 1000;

        // 4.x "skipped" trunk's monotonic addressing: give any user a nice exception
        private static readonly int VERSION_CURRENT = VERSION_CHECKSUM;
        private readonly int termIndexInterval;
        private readonly List<SimpleFieldWriter> fields = new List<SimpleFieldWriter>();
        private readonly FieldInfos fieldInfos;  //@SuppressWarnings("unused") 

        public FixedGapTermsIndexWriter(SegmentWriteState state)
        {
            String indexFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                TERMS_INDEX_EXTENSION);
            termIndexInterval = state.TermIndexInterval;
            output = state.Directory.CreateOutput(indexFileName, state.Context);
            bool success = false;
            try
            {
                fieldInfos = state.FieldInfos;
                WriteHeader(output);
                output.WriteInt(termIndexInterval);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(output);
                }
            }
        }

        private void WriteHeader(IndexOutput output)
        {
            CodecUtil.WriteHeader(output, CODEC_NAME, VERSION_CURRENT);
        }

        public override FieldWriter AddField(FieldInfo field, long termsFilePointer)
        {
            //System.output.println("FGW: addFfield=" + field.name);
            SimpleFieldWriter writer = new SimpleFieldWriter(field, termsFilePointer);
            fields.Add(writer);
            return writer;
        }

        /// <remarks>
        /// NOTE: if your codec does not sort in unicode code
        /// point order, you must override this method, to simply
        /// return indexedTerm.length.
        /// </remarks>
        protected int IndexedTermPrefixLength(BytesRef priorTerm, BytesRef indexedTerm)
        {
            // As long as codec sorts terms in unicode codepoint
            // order, we can safely strip off the non-distinguishing
            // suffix to save RAM in the loaded terms index.
            int idxTermOffset = indexedTerm.Offset;
            int priorTermOffset = priorTerm.Offset;
            int limit = Math.Min(priorTerm.Length, indexedTerm.Length);
            for (int byteIdx = 0; byteIdx < limit; byteIdx++)
            {
                if (priorTerm.Bytes[priorTermOffset + byteIdx] != indexedTerm.Bytes[idxTermOffset + byteIdx])
                {
                    return byteIdx + 1;
                }
            }
            return Math.Min(1 + priorTerm.Length, indexedTerm.Length);
        }

        public void Dispose()
        {
            if (output != null)
            {
                bool success = false;
                try
                {
                    long dirStart = output.FilePointer;
                    int fieldCount = fields.Count;

                    int nonNullFieldCount = 0;
                    for (int i = 0; i < fieldCount; i++)
                    {
                        SimpleFieldWriter field = fields[i];
                        if (field.numIndexTerms > 0)
                        {
                            nonNullFieldCount++;
                        }
                    }

                    output.WriteVInt(nonNullFieldCount);
                    for (int i = 0; i < fieldCount; i++)
                    {
                        SimpleFieldWriter field = fields[i];
                        if (field.numIndexTerms > 0)
                        {
                            output.WriteVInt(field.fieldInfo.Number);
                            output.WriteVInt(field.numIndexTerms);
                            output.WriteVLong(field.termsStart);
                            output.WriteVLong(field.indexStart);
                            output.WriteVLong(field.packedIndexStart);
                            output.WriteVLong(field.packedOffsetsStart);
                        }
                    }
                    WriteTrailer(dirStart);
                    CodecUtil.WriteFooter(output);
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Close(output);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException(output);
                    }
                    output = null;
                }
            }
        }

        private void WriteTrailer(long dirStart)
        {
            output.WriteLong(dirStart);
        }


        private class SimpleFieldWriter : FieldWriter
        {
            public readonly FieldInfo fieldInfo;
            public int numIndexTerms;
            public readonly long indexStart;
            public readonly long termsStart;
            public long packedIndexStart;
            public long packedOffsetsStart;
            private long numTerms;

            // TODO: we could conceivably make a PackedInts wrapper
            // that auto-grows... then we wouldn't force 6 bytes RAM
            // per index term:
            private short[] termLengths;
            private int[] termsPointerDeltas;
            private long lastTermsPointer;
            private long totTermLength;

            private readonly BytesRef lastTerm = new BytesRef();

            public SimpleFieldWriter(FieldInfo fieldInfo, long termsFilePointer)
            {
                this.fieldInfo = fieldInfo;
                indexStart = output.FilePointer;
                termsStart = lastTermsPointer = termsFilePointer;
                termLengths = new short[0];
                termsPointerDeltas = new int[0];
            }

            public override bool CheckIndexTerm(BytesRef text, TermStats stats)
            {
                // First term is first indexed term:
                //System.output.println("FGW: checkIndexTerm text=" + text.utf8ToString());
                if (0 == (numTerms++ % termIndexInterval))
                {
                    return true;
                }
                else
                {
                    if (0 == numTerms % termIndexInterval)
                    {
                        // save last term just before next index term so we
                        // can compute wasted suffix
                        lastTerm.CopyBytes(text);
                    }
                    return false;
                }
            }

            public override void Add(BytesRef text, TermStats stats, long termsFilePointer)
            {
                int indexedTermLength = IndexedTermPrefixLength(lastTerm, text);
                //System.output.println("FGW: add text=" + text.utf8ToString() + " " + text + " fp=" + termsFilePointer);

                // write only the min prefix that shows the diff
                // against prior term
                output.WriteBytes(text.Bytes, text.Offset, indexedTermLength);

                if (termLengths.Length == numIndexTerms)
                {
                    termLengths = ArrayUtil.Grow(termLengths);
                }
                if (termsPointerDeltas.Length == numIndexTerms)
                {
                    termsPointerDeltas = ArrayUtil.Grow(termsPointerDeltas);
                }

                // save delta terms pointer
                termsPointerDeltas[numIndexTerms] = (int)(termsFilePointer - lastTermsPointer);
                lastTermsPointer = termsFilePointer;

                // save term length (in bytes)
                Debug.Assert(indexedTermLength <= Short.MAX_VALUE);
                termLengths[numIndexTerms] = (short)indexedTermLength;
                totTermLength += indexedTermLength;

                lastTerm.CopyBytes(text);
                numIndexTerms++;
            }

            public override void Finish(long termsFilePointer)
            {

                // write primary terms dict offsets
                packedIndexStart = output.FilePointer;

                PackedInts.Writer w = PackedInts.GetWriter(output, numIndexTerms,
                    PackedInts.BitsRequired(termsFilePointer),
                    PackedInts.DEFAULT);

                // relative to our indexStart
                long upto = 0;
                for (int i = 0; i < numIndexTerms; i++)
                {
                    upto += termsPointerDeltas[i];
                    w.Add(upto);
                }
                w.Finish();

                packedOffsetsStart = output.FilePointer;

                // write offsets into the byte[] terms
                w = PackedInts.GetWriter(output, 1 + numIndexTerms, PackedInts.BitsRequired(totTermLength),
                    PackedInts.DEFAULT);
                upto = 0;
                for (int i = 0; i < numIndexTerms; i++)
                {
                    w.Add(upto);
                    upto += termLengths[i];
                }
                w.Add(upto);
                w.Finish();

                // our referrer holds onto us, while other fields are
                // being written, so don't tie up this RAM:
                termLengths = null;
                termsPointerDeltas = null;
            }
        }
    }
}