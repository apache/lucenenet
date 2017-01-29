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
        protected IndexOutput Output; // out

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
            Output = state.Directory.CreateOutput(indexFileName, state.Context);
            bool success = false;
            try
            {
                _fieldInfos = state.FieldInfos;
                WriteHeader(Output);
                Output.WriteInt(_termIndexInterval);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(Output);
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

            internal readonly FieldInfo FieldInfo;
            internal int NumIndexTerms;
            internal readonly long IndexStart;
            internal readonly long TermsStart;
            internal long PackedIndexStart;
            internal long PackedOffsetsStart;
            private long _numTerms;

            // TODO: we could conceivably make a PackedInts wrapper
            // that auto-grows... then we wouldn't force 6 bytes RAM
            // per index term:
            private short[] _termLengths;
            private int[] _termsPointerDeltas;
            private long _lastTermsPointer;
            private long _totTermLength;

            private readonly BytesRef _lastTerm = new BytesRef();

            internal SimpleFieldWriter(FixedGapTermsIndexWriter outerInstance, FieldInfo fieldInfo, long termsFilePointer)
            {
                this.outerInstance = outerInstance;
                FieldInfo = fieldInfo;
                IndexStart = outerInstance.Output.FilePointer;
                TermsStart = _lastTermsPointer = termsFilePointer;
                _termLengths = new short[0];
                _termsPointerDeltas = new int[0];
            }

            public override bool CheckIndexTerm(BytesRef text, TermStats stats)
            {
                // First term is first indexed term:
                //System.output.println("FGW: checkIndexTerm text=" + text.utf8ToString());
                if (0 == (_numTerms++ % outerInstance._termIndexInterval))
                    return true;

                // save last term just before next index term so we
                // can compute wasted suffix
                if (0 == _numTerms % outerInstance._termIndexInterval)
                    _lastTerm.CopyBytes(text);

                return false;
            }

            public override void Add(BytesRef text, TermStats stats, long termsFilePointer)
            {
                int indexedTermLength = outerInstance.IndexedTermPrefixLength(_lastTerm, text);

                // write only the min prefix that shows the diff
                // against prior term
                outerInstance.Output.WriteBytes(text.Bytes, text.Offset, indexedTermLength);

                if (_termLengths.Length == NumIndexTerms)
                {
                    _termLengths = ArrayUtil.Grow(_termLengths);
                }
                if (_termsPointerDeltas.Length == NumIndexTerms)
                {
                    _termsPointerDeltas = ArrayUtil.Grow(_termsPointerDeltas);
                }

                // save delta terms pointer
                _termsPointerDeltas[NumIndexTerms] = (int)(termsFilePointer - _lastTermsPointer);
                _lastTermsPointer = termsFilePointer;

                // save term length (in bytes)
                Debug.Assert(indexedTermLength <= short.MaxValue);
                _termLengths[NumIndexTerms] = (short)indexedTermLength;
                _totTermLength += indexedTermLength;

                _lastTerm.CopyBytes(text);
                NumIndexTerms++;
            }

            public override void Finish(long termsFilePointer)
            {
                // write primary terms dict offsets
                PackedIndexStart = outerInstance.Output.FilePointer;

                PackedInts.Writer w = PackedInts.GetWriter(outerInstance.Output, NumIndexTerms,
                    PackedInts.BitsRequired(termsFilePointer),
                    PackedInts.DEFAULT);

                // relative to our indexStart
                long upto = 0;
                for (int i = 0; i < NumIndexTerms; i++)
                {
                    upto += _termsPointerDeltas[i];
                    w.Add(upto);
                }
                w.Finish();

                PackedOffsetsStart = outerInstance.Output.FilePointer;

                // write offsets into the byte[] terms
                w = PackedInts.GetWriter(outerInstance.Output, 1 + NumIndexTerms, PackedInts.BitsRequired(_totTermLength),
                    PackedInts.DEFAULT);
                upto = 0;
                for (int i = 0; i < NumIndexTerms; i++)
                {
                    w.Add(upto);
                    upto += _termLengths[i];
                }
                w.Add(upto);
                w.Finish();

                // our referrer holds onto us, while other fields are
                // being written, so don't tie up this RAM:
                _termLengths = null;
                _termsPointerDeltas = null;
            }
        }

        public override void Dispose()
        {
            if (Output != null)
            {
                bool success = false;
                try
                {
                    long dirStart = Output.FilePointer;
                    int fieldCount = _fields.Count;

                    int nonNullFieldCount = 0;
                    for (int i = 0; i < fieldCount; i++)
                    {
                        SimpleFieldWriter field = _fields[i];
                        if (field.NumIndexTerms > 0)
                        {
                            nonNullFieldCount++;
                        }
                    }

                    Output.WriteVInt(nonNullFieldCount);
                    for (int i = 0; i < fieldCount; i++)
                    {
                        SimpleFieldWriter field = _fields[i];
                        if (field.NumIndexTerms > 0)
                        {
                            Output.WriteVInt(field.FieldInfo.Number);
                            Output.WriteVInt(field.NumIndexTerms);
                            Output.WriteVLong(field.TermsStart);
                            Output.WriteVLong(field.IndexStart);
                            Output.WriteVLong(field.PackedIndexStart);
                            Output.WriteVLong(field.PackedOffsetsStart);
                        }
                    }
                    WriteTrailer(dirStart);
                    CodecUtil.WriteFooter(Output);
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Close(Output);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException(Output);
                    }
                    Output = null;
                }
            }
        }

        private void WriteTrailer(long dirStart)
        {
            Output.WriteLong(dirStart);
        }
    }
}