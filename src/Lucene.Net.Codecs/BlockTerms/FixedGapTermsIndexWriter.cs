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
    using Index;
    using Store;
    using Util;
    using Util.Packed;

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
        protected IndexOutput m_output;

        /// <summary>Extension of terms index file</summary>
        internal const string TERMS_INDEX_EXTENSION = "tii";
        internal const string CODEC_NAME = "SIMPLE_STANDARD_TERMS_INDEX";
        internal const int VERSION_START = 0;
        internal const int VERSION_APPEND_ONLY = 1;
        internal const int VERSION_CHECKSUM = 1000; // 4.x "skipped" trunk's monotonic addressing: give any user a nice exception
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        private readonly int _termIndexInterval;

        private readonly List<SimpleFieldWriter> _fields = new List<SimpleFieldWriter>();

        private readonly FieldInfos _fieldInfos; // unread

        public FixedGapTermsIndexWriter(SegmentWriteState state)
        {
            string indexFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                TERMS_INDEX_EXTENSION);
            _termIndexInterval = state.TermIndexInterval;
            m_output = state.Directory.CreateOutput(indexFileName, state.Context);
            bool success = false;
            try
            {
                _fieldInfos = state.FieldInfos;
                WriteHeader(m_output);
                m_output.WriteInt(_termIndexInterval);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(m_output);
                }
            }
        }

        private void WriteHeader(IndexOutput output)
        {
            CodecUtil.WriteHeader(output, CODEC_NAME, VERSION_CURRENT);
        }

        public override FieldWriter AddField(FieldInfo field, long termsFilePointer)
        {
            var writer = new SimpleFieldWriter(this, field, termsFilePointer);
            _fields.Add(writer);
            return writer;
        }

        /// <remarks>
        /// NOTE: if your codec does not sort in unicode code
        /// point order, you must override this method, to simply
        /// return indexedTerm.length.
        /// </remarks>
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
                indexStart = outerInstance.m_output.FilePointer;
                termsStart = lastTermsPointer = termsFilePointer;
                termLengths = new short[0];
                termsPointerDeltas = new int[0];
            }

            public override bool CheckIndexTerm(BytesRef text, TermStats stats)
            {
                // First term is first indexed term:
                //System.output.println("FGW: checkIndexTerm text=" + text.utf8ToString());
                if (0 == (numTerms++ % outerInstance._termIndexInterval))
                    return true;

                // save last term just before next index term so we
                // can compute wasted suffix
                if (0 == numTerms % outerInstance._termIndexInterval)
                    lastTerm.CopyBytes(text);

                return false;
            }

            public override void Add(BytesRef text, TermStats stats, long termsFilePointer)
            {
                int indexedTermLength = outerInstance.IndexedTermPrefixLength(lastTerm, text);

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
                Debug.Assert(indexedTermLength <= short.MaxValue);
                termLengths[numIndexTerms] = (short)indexedTermLength;
                totTermLength += indexedTermLength;

                lastTerm.CopyBytes(text);
                numIndexTerms++;
            }

            public override void Finish(long termsFilePointer)
            {
                // write primary terms dict offsets
                packedIndexStart = outerInstance.m_output.FilePointer;

                PackedInts.Writer w = PackedInts.GetWriter(outerInstance.m_output, numIndexTerms,
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

                packedOffsetsStart = outerInstance.m_output.FilePointer;

                // write offsets into the byte[] terms
                w = PackedInts.GetWriter(outerInstance.m_output, 1 + numIndexTerms, PackedInts.BitsRequired(totTermLength),
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

        public override void Dispose()
        {
            if (m_output != null)
            {
                bool success = false;
                try
                {
                    long dirStart = m_output.FilePointer;
                    int fieldCount = _fields.Count;

                    int nonNullFieldCount = 0;
                    for (int i = 0; i < fieldCount; i++)
                    {
                        SimpleFieldWriter field = _fields[i];
                        if (field.numIndexTerms > 0)
                        {
                            nonNullFieldCount++;
                        }
                    }

                    m_output.WriteVInt(nonNullFieldCount);
                    for (int i = 0; i < fieldCount; i++)
                    {
                        SimpleFieldWriter field = _fields[i];
                        if (field.numIndexTerms > 0)
                        {
                            m_output.WriteVInt(field.fieldInfo.Number);
                            m_output.WriteVInt(field.numIndexTerms);
                            m_output.WriteVLong(field.termsStart);
                            m_output.WriteVLong(field.indexStart);
                            m_output.WriteVLong(field.packedIndexStart);
                            m_output.WriteVLong(field.packedOffsetsStart);
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
                        IOUtils.Close(m_output);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException(m_output);
                    }
                    m_output = null;
                }
            }
        }

        private void WriteTrailer(long dirStart)
        {
            m_output.WriteLong(dirStart);
        }
    }
}