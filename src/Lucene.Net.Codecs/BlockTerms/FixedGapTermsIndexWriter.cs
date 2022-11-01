using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// Selects every Nth term as and index term, and hold term
    /// bytes (mostly) fully expanded in memory.  This terms index
    /// supports seeking by ord.  See 
    /// <see cref="VariableGapTermsIndexWriter"/> for a more memory efficient
    /// terms index that does not support seeking by ord.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class FixedGapTermsIndexWriter : TermsIndexWriterBase
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        protected IndexOutput m_output;
#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>Extension of terms index file</summary>
        internal const string TERMS_INDEX_EXTENSION = "tii";
        internal const string CODEC_NAME = "SIMPLE_STANDARD_TERMS_INDEX";
        internal const int VERSION_START = 0;
        internal const int VERSION_APPEND_ONLY = 1;
        internal const int VERSION_CHECKSUM = 1000; // 4.x "skipped" trunk's monotonic addressing: give any user a nice exception
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        private readonly int termIndexInterval;

        private readonly IList<SimpleFieldWriter> fields = new JCG.List<SimpleFieldWriter>();

        //private readonly FieldInfos fieldInfos; // unread  // LUCENENET: Not used

        public FixedGapTermsIndexWriter(SegmentWriteState state)
        {
            string indexFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, TERMS_INDEX_EXTENSION);
            termIndexInterval = state.TermIndexInterval;
            m_output = state.Directory.CreateOutput(indexFileName, state.Context);
            bool success = false;
            try
            {
                //fieldInfos = state.FieldInfos; // LUCENENET: Not used
                WriteHeader(m_output);
                m_output.WriteInt32(termIndexInterval);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(m_output);
                }
            }
        }

        private static void WriteHeader(IndexOutput output) // LUCENENET: CA1822: Mark members as static
        {
            CodecUtil.WriteHeader(output, CODEC_NAME, VERSION_CURRENT);
        }

        public override FieldWriter AddField(FieldInfo field, long termsFilePointer)
        {
            //System.out.println("FGW: addFfield=" + field.name);
            SimpleFieldWriter writer = new SimpleFieldWriter(this, field, termsFilePointer);
            fields.Add(writer);
            return writer;
        }

        /// <summary>
        /// NOTE: if your codec does not sort in unicode code
        /// point order, you must override this method, to simply
        /// return <c>indexedTerm.Length</c>.
        /// </summary>
        protected virtual int IndexedTermPrefixLength(BytesRef priorTerm, BytesRef indexedTerm)
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

        private class SimpleFieldWriter : FieldWriter
        {
            private readonly FixedGapTermsIndexWriter outerInstance;

            internal readonly FieldInfo fieldInfo;
            internal int numIndexTerms;
            internal readonly long indexStart;
            internal readonly long termsStart;
            internal long packedIndexStart;
            internal long packedOffsetsStart;
            private long numTerms;

            // TODO: we could conceivably make a PackedInts wrapper
            // that auto-grows... then we wouldn't force 6 bytes RAM
            // per index term:
            private short[] termLengths;
            private int[] termsPointerDeltas;
            private long lastTermsPointer;
            private long totTermLength;

            private readonly BytesRef lastTerm = new BytesRef();

            internal SimpleFieldWriter(FixedGapTermsIndexWriter outerInstance, FieldInfo fieldInfo, long termsFilePointer)
            {
                this.outerInstance = outerInstance;

                this.fieldInfo = fieldInfo;
                indexStart = outerInstance.m_output.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                termsStart = lastTermsPointer = termsFilePointer;
                termLengths = EMPTY_INT16S;
                termsPointerDeltas = EMPTY_INT32S;
            }

            public override bool CheckIndexTerm(BytesRef text, TermStats stats)
            {
                // First term is first indexed term:
                //System.output.println("FGW: checkIndexTerm text=" + text.utf8ToString());
                if (0 == (numTerms++ % outerInstance.termIndexInterval))
                {
                    return true;
                }
                else
                {
                    if (0 == numTerms % outerInstance.termIndexInterval)
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
                int indexedTermLength = outerInstance.IndexedTermPrefixLength(lastTerm, text);
                //System.out.println("FGW: add text=" + text.utf8ToString() + " " + text + " fp=" + termsFilePointer);

                // write only the min prefix that shows the diff
                // against prior term
                outerInstance.m_output.WriteBytes(text.Bytes, text.Offset, indexedTermLength);

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
                if (Debugging.AssertsEnabled) Debugging.Assert(indexedTermLength <= short.MaxValue);
                termLengths[numIndexTerms] = (short)indexedTermLength;
                totTermLength += indexedTermLength;

                lastTerm.CopyBytes(text);
                numIndexTerms++;
            }

            public override void Finish(long termsFilePointer)
            {
                // write primary terms dict offsets
                packedIndexStart = outerInstance.m_output.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                PackedInt32s.Writer w = PackedInt32s.GetWriter(outerInstance.m_output, numIndexTerms, PackedInt32s.BitsRequired(termsFilePointer), PackedInt32s.DEFAULT);

                // relative to our indexStart
                long upto = 0;
                for (int i = 0; i < numIndexTerms; i++)
                {
                    upto += termsPointerDeltas[i];
                    w.Add(upto);
                }
                w.Finish();

                packedOffsetsStart = outerInstance.m_output.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                // write offsets into the byte[] terms
                w = PackedInt32s.GetWriter(outerInstance.m_output, 1 + numIndexTerms, PackedInt32s.BitsRequired(totTermLength), PackedInt32s.DEFAULT);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_output != null)
                {
                    bool success = false;
                    try
                    {
                        long dirStart = m_output.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
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

                        m_output.WriteVInt32(nonNullFieldCount);
                        for (int i = 0; i < fieldCount; i++)
                        {
                            SimpleFieldWriter field = fields[i];
                            if (field.numIndexTerms > 0)
                            {
                                m_output.WriteVInt32(field.fieldInfo.Number);
                                m_output.WriteVInt32(field.numIndexTerms);
                                m_output.WriteVInt64(field.termsStart);
                                m_output.WriteVInt64(field.indexStart);
                                m_output.WriteVInt64(field.packedIndexStart);
                                m_output.WriteVInt64(field.packedOffsetsStart);
                            }
                        }
                        WriteTrailer(dirStart);
                        CodecUtil.WriteFooter(m_output);
                        success = true;
                    }
                    finally
                    {
                        if (success)
                        {
                            IOUtils.Dispose(m_output);
                        }
                        else
                        {
                            IOUtils.DisposeWhileHandlingException(m_output);
                        }
                        m_output = null;
                    }
                }
            }
        }

        private void WriteTrailer(long dirStart)
        {
            m_output.WriteInt64(dirStart);
        }
    }
}