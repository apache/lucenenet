using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
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
    /// Selects index terms according to provided pluggable
    /// <see cref="IndexTermSelector"/>, and stores them in a prefix trie that's
    /// loaded entirely in RAM stored as an <see cref="FST{T}"/>.  This terms
    /// index only supports unsigned byte term sort order
    /// (unicode codepoint order when the bytes are UTF8).
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class VariableGapTermsIndexWriter : TermsIndexWriterBase
    {
        protected IndexOutput m_output;

        /// <summary>Extension of terms index file.</summary>
        internal const string TERMS_INDEX_EXTENSION = "tiv";

        internal const string CODEC_NAME = "VARIABLE_GAP_TERMS_INDEX";
        internal const int VERSION_START = 0;
        internal const int VERSION_APPEND_ONLY = 1;
        internal const int VERSION_CHECKSUM = 2;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        private readonly IList<FSTFieldWriter> fields = new JCG.List<FSTFieldWriter>();

        //private readonly FieldInfos fieldInfos; // unread  // LUCENENET: Not used
        private readonly IndexTermSelector policy;

        /// <summary>
        /// Hook for selecting which terms should be placed in the terms index.
        /// <para/>
        /// <see cref="NewField(FieldInfo)"/> is called at the start of each new field, and
        /// <see cref="IsIndexTerm(BytesRef, TermStats)"/> for each term in that field.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public abstract class IndexTermSelector
        {
            /// <summary>
            /// Called sequentially on every term being written,
            /// returning <c>true</c> if this term should be indexed.
            /// </summary>
            public abstract bool IsIndexTerm(BytesRef term, TermStats stats);

            /// <summary>Called when a new field is started.</summary>
            public abstract void NewField(FieldInfo fieldInfo);
        }

        /// <remarks>
        /// Same policy as <see cref="FixedGapTermsIndexWriter"/>
        /// </remarks>
        public sealed class EveryNTermSelector : IndexTermSelector
        {
            private int count;
            private readonly int interval;

            public EveryNTermSelector(int interval)
            {
                this.interval = interval;
                // First term is first indexed term
                count = interval;
            }

            public override bool IsIndexTerm(BytesRef term, TermStats stats)
            {
                if (count >= interval)
                {
                    count = 1;
                    return true;
                }
                else
                {
                    count++;
                    return false;
                }
            }

            public override void NewField(FieldInfo fieldInfo)
            {
                count = interval;
            }
        }

        /// <summary>
        /// Sets an index term when docFreq >= docFreqThresh, or
        /// every interval terms.  This should reduce seek time
        /// to high docFreq terms. 
        /// </summary>
        public sealed class EveryNOrDocFreqTermSelector : IndexTermSelector
        {
            private int count;
            private readonly int docFreqThresh;
            private readonly int interval;

            public EveryNOrDocFreqTermSelector(int docFreqThresh, int interval)
            {
                this.interval = interval;
                this.docFreqThresh = docFreqThresh;

                // First term is first indexed term:
                count = interval;
            }

            public override bool IsIndexTerm(BytesRef term, TermStats stats)
            {
                if (stats.DocFreq >= docFreqThresh || count >= interval)
                {
                    count = 1;
                    return true;
                }
                else
                {
                    count++;
                    return false;
                }
            }

            public override void NewField(FieldInfo fieldInfo)
            {
                count = interval;
            }
        }

        // TODO: it'd be nice to let the FST builder prune based
        // on term count of each node (the prune1/prune2 that it
        // accepts), and build the index based on that.  This
        // should result in a more compact terms index, more like
        // a prefix trie than the other selectors, because it
        // only stores enough leading bytes to get down to N
        // terms that may complete that prefix.  It becomes
        // "deeper" when terms are dense, and "shallow" when they
        // are less dense.
        //
        // However, it's not easy to make that work this this
        // API, because that pruning doesn't immediately know on
        // seeing each term whether that term will be a seek point
        // or not.  It requires some non-causality in the API, ie
        // only on seeing some number of future terms will the
        // builder decide which past terms are seek points.
        // Somehow the API'd need to be able to return a "I don't
        // know" value, eg like a Future, which only later on is
        // flipped (frozen) to true or false.
        //
        // We could solve this with a 2-pass approach, where the
        // first pass would build an FSA (no outputs) solely to
        // determine which prefixes are the 'leaves' in the
        // pruning. The 2nd pass would then look at this prefix
        // trie to mark the seek points and build the FST mapping
        // to the true output.
        //
        // But, one downside to this approach is that it'd result
        // in uneven index term selection.  EG with prune1=10, the
        // resulting index terms could be as frequent as every 10
        // terms or as rare as every <maxArcCount> * 10 (eg 2560),
        // in the extremes.

        public VariableGapTermsIndexWriter(SegmentWriteState state, IndexTermSelector policy)
        {
            string indexFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, TERMS_INDEX_EXTENSION);
            m_output = state.Directory.CreateOutput(indexFileName, state.Context);
            bool success = false;
            try
            {
                //fieldInfos = state.FieldInfos; // LUCENENET: Not used
                this.policy = policy;
                WriteHeader(m_output);
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
            ////System.out.println("VGW: field=" + field.name);
            policy.NewField(field);
            FSTFieldWriter writer = new FSTFieldWriter(this, field, termsFilePointer);
            fields.Add(writer);
            return writer;
        }

        /// <remarks>
        /// NOTE: If your codec does not sort in unicode code point order,
        /// you must override this method to simply return <c>indexedTerm.Length</c>.
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

        private class FSTFieldWriter : FieldWriter
        {
            private readonly VariableGapTermsIndexWriter outerInstance;

            private readonly Builder<Int64> fstBuilder;
            private readonly PositiveInt32Outputs fstOutputs;
            private readonly long startTermsFilePointer;

            internal readonly FieldInfo fieldInfo;
            internal FST<Int64> fst;
            internal readonly long indexStart;

            private readonly BytesRef lastTerm = new BytesRef();
            private bool first = true;

            public FSTFieldWriter(VariableGapTermsIndexWriter outerInstance, FieldInfo fieldInfo, long termsFilePointer)
            {
                this.outerInstance = outerInstance;

                this.fieldInfo = fieldInfo;
                fstOutputs = PositiveInt32Outputs.Singleton;
                fstBuilder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, fstOutputs);
                indexStart = outerInstance.m_output.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                ////System.out.println("VGW: field=" + fieldInfo.name);

                // Always put empty string in
                fstBuilder.Add(new Int32sRef(), termsFilePointer);
                startTermsFilePointer = termsFilePointer;
            }

            public override bool CheckIndexTerm(BytesRef text, TermStats stats)
            {
                //System.out.println("VGW: index term=" + text.utf8ToString());
                // NOTE: we must force the first term per field to be
                // indexed, in case policy doesn't:
                if (outerInstance.policy.IsIndexTerm(text, stats) || first)
                {
                    first = false;
                    //System.out.println("  YES");
                    return true;
                }
                else
                {
                    lastTerm.CopyBytes(text);
                    return false;
                }
            }

            private readonly Int32sRef scratchIntsRef = new Int32sRef();

            public override void Add(BytesRef text, TermStats stats, long termsFilePointer)
            {
                if (text.Length == 0)
                {
                    // We already added empty string in ctor
                    if (Debugging.AssertsEnabled) Debugging.Assert(termsFilePointer == startTermsFilePointer);
                    return;
                }
                int lengthSave = text.Length;
                text.Length = outerInstance.IndexedTermPrefixLength(lastTerm, text);
                try
                {
                    fstBuilder.Add(Util.Fst.Util.ToInt32sRef(text, scratchIntsRef), termsFilePointer);
                }
                finally
                {
                    text.Length = lengthSave;
                }
                lastTerm.CopyBytes(text);
            }

            public override void Finish(long termsFilePointer)
            {
                fst = fstBuilder.Finish();
                if (fst != null)
                {
                    fst.Save(outerInstance.m_output);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_output != null)
                {
                    try
                    {
                        long dirStart = m_output.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        int fieldCount = fields.Count;

                        int nonNullFieldCount = 0;
                        for (int i = 0; i < fieldCount; i++)
                        {
                            FSTFieldWriter field = fields[i];
                            if (field.fst != null)
                            {
                                nonNullFieldCount++;
                            }
                        }

                        m_output.WriteVInt32(nonNullFieldCount);
                        for (int i = 0; i < fieldCount; i++)
                        {
                            FSTFieldWriter field = fields[i];
                            if (field.fst != null)
                            {
                                m_output.WriteVInt32(field.fieldInfo.Number);
                                m_output.WriteVInt64(field.indexStart);
                            }
                        }
                        WriteTrailer(dirStart);
                        CodecUtil.WriteFooter(m_output);
                    }
                    finally
                    {
                        m_output.Dispose();
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