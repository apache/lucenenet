using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    /// Writes terms dict, block-encoding (column stride) each term's metadata 
    /// for each set of terms between two index terms
    /// 
    /// lucene.experimental
    /// </summary>
    /// <remarks>
    /// TODO Currently we encode all terms between two indexed terms as a block
    /// But we could decouple the two, ie allow several blocks in between two indexed terms
    /// </remarks>
    public class BlockTermsWriter : FieldsConsumer
    {
        internal const string CODEC_NAME = "BLOCK_TERMS_DICT";

        // Initial format
        public const int VERSION_START = 0;
        public const int VERSION_APPEND_ONLY = 1;
        public const int VERSION_META_ARRAY = 2;
        public const int VERSION_CHECKSUM = 3;
        public const int VERSION_CURRENT = VERSION_CHECKSUM;

        /// <summary>Extension of terms file</summary>
        public const string TERMS_EXTENSION = "tib";

        protected IndexOutput m_output;
        private readonly PostingsWriterBase postingsWriter;
        private readonly FieldInfos fieldInfos;
        private FieldInfo currentField;
        private readonly TermsIndexWriterBase termsIndexWriter;
        
        protected class FieldMetaData
        {
            public FieldInfo FieldInfo { get; private set; }
            public long NumTerms { get; private set; }
            public long TermsStartPointer { get; private set; }
            public long SumTotalTermFreq { get; private set; }
            public long SumDocFreq { get; private set; }
            public int DocCount { get; private set; }
            public int LongsSize { get; private set; }

            public FieldMetaData(FieldInfo fieldInfo, long numTerms, long termsStartPointer, long sumTotalTermFreq,
                long sumDocFreq, int docCount, int longsSize)
            {
                Debug.Assert(numTerms > 0);

                FieldInfo = fieldInfo;
                TermsStartPointer = termsStartPointer;
                NumTerms = numTerms;
                SumTotalTermFreq = sumTotalTermFreq;
                SumDocFreq = sumDocFreq;
                DocCount = docCount;
                LongsSize = longsSize;
            }
        }

        private readonly List<FieldMetaData> _fields = new List<FieldMetaData>();

        public BlockTermsWriter(TermsIndexWriterBase termsIndexWriter,
            SegmentWriteState state, PostingsWriterBase postingsWriter)
        {
            var termsFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                TERMS_EXTENSION);
            this.termsIndexWriter = termsIndexWriter;
            m_output = state.Directory.CreateOutput(termsFileName, state.Context);
            var success = false;

            try
            {
                fieldInfos = state.FieldInfos;
                WriteHeader(m_output);
                currentField = null;
                this.postingsWriter = postingsWriter;

                postingsWriter.Init(m_output); // have consumer write its format/header
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

        public override TermsConsumer AddField(FieldInfo field)
        {
            Debug.Assert(currentField == null || currentField.Name.CompareTo(field.Name) < 0);

            currentField = field;
            var fiw = termsIndexWriter.AddField(field, m_output.FilePointer);
            return new TermsWriter(this, fiw, field, postingsWriter);
        }

        public override void Dispose()
        {
            if (m_output == null) return;

            try
            {
                var dirStart = m_output.FilePointer;

                m_output.WriteVInt(_fields.Count);

                foreach (var field in _fields)
                {
                    m_output.WriteVInt(field.FieldInfo.Number);
                    m_output.WriteVLong(field.NumTerms);
                    m_output.WriteVLong(field.TermsStartPointer);
                    if (field.FieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                    {
                        m_output.WriteVLong(field.SumTotalTermFreq);
                    }
                    m_output.WriteVLong(field.SumDocFreq);
                    m_output.WriteVInt(field.DocCount);
                    if (VERSION_CURRENT >= VERSION_META_ARRAY)
                    {
                        m_output.WriteVInt(field.LongsSize);
                    }

                }
                WriteTrailer(dirStart);
                CodecUtil.WriteFooter(m_output);
            }
            finally
            {
                IOUtils.Close(m_output, postingsWriter, termsIndexWriter);
                m_output = null;
            }
        }

        private void WriteTrailer(long dirStart)
        {
            m_output.WriteLong(dirStart);
        }

        private class TermEntry
        {
            public BytesRef Term { get; private set; }
            public BlockTermState State { get; set; }

            public TermEntry()
            {
                Term = new BytesRef();
            }
        }

        internal class TermsWriter : TermsConsumer
        {
            private readonly BlockTermsWriter outerInstance;

            private readonly FieldInfo _fieldInfo;
            private readonly PostingsWriterBase _postingsWriter;
            private readonly long _termsStartPointer;
            private long _numTerms;
            private readonly TermsIndexWriterBase.FieldWriter _fieldIndexWriter;
            private long _sumTotalTermFreq;
            private long _sumDocFreq;
            private int _docCount;
            private readonly int _longsSize;

            private TermEntry[] _pendingTerms;

            private int _pendingCount;

            internal TermsWriter(
                BlockTermsWriter outerInstance,
                TermsIndexWriterBase.FieldWriter fieldIndexWriter,
                FieldInfo fieldInfo,
                PostingsWriterBase postingsWriter)
            {
                this.outerInstance = outerInstance;
                _fieldInfo = fieldInfo;
                _fieldIndexWriter = fieldIndexWriter;

                _pendingTerms = new TermEntry[32];
                for (int i = 0; i < _pendingTerms.Length; i++)
                {
                    _pendingTerms[i] = new TermEntry();
                }
                _termsStartPointer = this.outerInstance.m_output.FilePointer;
                _postingsWriter = postingsWriter;
                _longsSize = postingsWriter.SetField(fieldInfo);
            }

            public override IComparer<BytesRef> Comparer
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }   
        
            public override PostingsConsumer StartTerm(BytesRef text)
            {
                _postingsWriter.StartTerm();
                return _postingsWriter;
            }

            private readonly BytesRef _lastPrevTerm = new BytesRef();

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                Debug.Assert(stats.DocFreq > 0);

                var isIndexTerm = _fieldIndexWriter.CheckIndexTerm(text, stats);

                if (isIndexTerm)
                {
                    if (_pendingCount > 0)
                    {
                        // Instead of writing each term, live, we gather terms
                        // in RAM in a pending buffer, and then write the
                        // entire block in between index terms:
                        FlushBlock();
                    }
                    _fieldIndexWriter.Add(text, stats, outerInstance.m_output.FilePointer);
                }

                if (_pendingTerms.Length == _pendingCount)
                {
                    var newArray =
                        new TermEntry[ArrayUtil.Oversize(_pendingCount + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(_pendingTerms, 0, newArray, 0, _pendingCount);
                    for (var i = _pendingCount; i < newArray.Length; i++)
                    {
                        newArray[i] = new TermEntry();
                    }
                    _pendingTerms = newArray;
                }
                var te = _pendingTerms[_pendingCount];
                te.Term.CopyBytes(text);
                te.State = _postingsWriter.NewTermState();
                te.State.DocFreq = stats.DocFreq;
                te.State.TotalTermFreq = stats.TotalTermFreq;
                _postingsWriter.FinishTerm(te.State);

                _pendingCount++;
                _numTerms++;
            }

            // Finishes all terms in this field
            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                if (_pendingCount > 0)
                {
                    FlushBlock();
                }

                // EOF marker:
                outerInstance.m_output.WriteVInt(0);

                _sumTotalTermFreq = sumTotalTermFreq;
                _sumDocFreq = sumDocFreq;
                _docCount = docCount;
                _fieldIndexWriter.Finish(outerInstance.m_output.FilePointer);

                if (_numTerms > 0)
                {
                    outerInstance._fields.Add(new FieldMetaData(_fieldInfo,
                        _numTerms,
                        _termsStartPointer,
                        sumTotalTermFreq,
                        sumDocFreq,
                        docCount,
                        _longsSize));
                }
            }

            private int SharedPrefix(BytesRef term1, BytesRef term2)
            {
                Debug.Assert(term1.Offset == 0);
                Debug.Assert(term2.Offset == 0);

                var pos1 = 0;
                var pos1End = pos1 + Math.Min(term1.Length, term2.Length);
                var pos2 = 0;

                while (pos1 < pos1End)
                {
                    if (term1.Bytes[pos1] != term2.Bytes[pos2])
                    {
                        return pos1;
                    }
                    pos1++;
                    pos2++;
                }

                return pos1;
            }

            private readonly RAMOutputStream _bytesWriter = new RAMOutputStream();
            private readonly RAMOutputStream _bufferWriter = new RAMOutputStream();

            private void FlushBlock()
            {
                // First pass: compute common prefix for all terms
                // in the block, against term before first term in
                // this block:

                int commonPrefix = SharedPrefix(_lastPrevTerm, _pendingTerms[0].Term);
                for (int termCount = 1; termCount < _pendingCount; termCount++)
                {
                    commonPrefix = Math.Min(commonPrefix,
                        SharedPrefix(_lastPrevTerm,
                            _pendingTerms[termCount].Term));
                }

                outerInstance.m_output.WriteVInt(_pendingCount);
                outerInstance.m_output.WriteVInt(commonPrefix);

                // 2nd pass: write suffixes, as separate byte[] blob
                for (var termCount = 0; termCount < _pendingCount; termCount++)
                {
                    var suffix = _pendingTerms[termCount].Term.Length - commonPrefix;
                    // TODO: cutover to better intblock codec, instead
                    // of interleaving here:
                    _bytesWriter.WriteVInt(suffix);
                    _bytesWriter.WriteBytes(_pendingTerms[termCount].Term.Bytes, commonPrefix, suffix);
                }
                outerInstance.m_output.WriteVInt((int)_bytesWriter.FilePointer);
                _bytesWriter.WriteTo(outerInstance.m_output);
                _bytesWriter.Reset();

                // 3rd pass: write the freqs as byte[] blob
                // TODO: cutover to better intblock codec.  simple64?
                // write prefix, suffix first:
                for (int termCount = 0; termCount < _pendingCount; termCount++)
                {
                    BlockTermState state = _pendingTerms[termCount].State;

                    Debug.Assert(state != null);

                    _bytesWriter.WriteVInt(state.DocFreq);
                    if (_fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                    {
                        _bytesWriter.WriteVLong(state.TotalTermFreq - state.DocFreq);
                    }
                }
                outerInstance.m_output.WriteVInt((int)_bytesWriter.FilePointer);
                _bytesWriter.WriteTo(outerInstance.m_output);
                _bytesWriter.Reset();

                // 4th pass: write the metadata 
                var longs = new long[_longsSize];
                bool absolute = true;
                for (int termCount = 0; termCount < _pendingCount; termCount++)
                {
                    BlockTermState state = _pendingTerms[termCount].State;
                    _postingsWriter.EncodeTerm(longs, _bufferWriter, _fieldInfo, state, absolute);
                    for (int i = 0; i < _longsSize; i++)
                    {
                        _bytesWriter.WriteVLong(longs[i]);
                    }
                    _bufferWriter.WriteTo(_bytesWriter);
                    _bufferWriter.Reset();
                    absolute = false;
                }
                outerInstance.m_output.WriteVInt((int)_bytesWriter.FilePointer);
                _bytesWriter.WriteTo(outerInstance.m_output);
                _bytesWriter.Reset();

                _lastPrevTerm.CopyBytes(_pendingTerms[_pendingCount - 1].Term);
                _pendingCount = 0;
            }
        }
    }
}