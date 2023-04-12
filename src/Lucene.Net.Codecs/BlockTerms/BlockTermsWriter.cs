using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
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

    // TODO: Currently we encode all terms between two indexed terms as a block
    // But we could decouple the two, ie allow several blocks in between two indexed terms

    /// <summary>
    /// Writes terms dict, block-encoding (column stride) each term's metadata 
    /// for each set of terms between two index terms.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class BlockTermsWriter : FieldsConsumer
    {
        internal const string CODEC_NAME = "BLOCK_TERMS_DICT";

        // Initial format
        public const int VERSION_START = 0;
        public const int VERSION_APPEND_ONLY = 1;
        public const int VERSION_META_ARRAY = 2;
        public const int VERSION_CHECKSUM = 3;
        public readonly static int VERSION_CURRENT = VERSION_CHECKSUM;

        /// <summary>Extension of terms file</summary>
        public readonly static string TERMS_EXTENSION = "tib";

#pragma warning disable CA2213 // Disposable fields should be disposed
        protected IndexOutput m_output;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly PostingsWriterBase postingsWriter;
        //private readonly FieldInfos fieldInfos; // LUCENENET: Not used
        private FieldInfo currentField;
        private readonly TermsIndexWriterBase termsIndexWriter;

        private class FieldMetaData
        {
            public FieldInfo FieldInfo { get; private set; }
            public long NumTerms { get; private set; }
            public long TermsStartPointer { get; private set; }
            public long SumTotalTermFreq { get; private set; }
            public long SumDocFreq { get; private set; }
            public int DocCount { get; private set; }
            /// <summary>
            /// NOTE: This was longsSize (field) in Lucene.
            /// </summary>
            public int Int64sSize { get; private set; }

            public FieldMetaData(FieldInfo fieldInfo, long numTerms, long termsStartPointer, long sumTotalTermFreq,
                long sumDocFreq, int docCount, int int64sSize)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(numTerms > 0);

                FieldInfo = fieldInfo;
                TermsStartPointer = termsStartPointer;
                NumTerms = numTerms;
                SumTotalTermFreq = sumTotalTermFreq;
                SumDocFreq = sumDocFreq;
                DocCount = docCount;
                Int64sSize = int64sSize;
            }
        }

        private readonly IList<FieldMetaData> fields = new JCG.List<FieldMetaData>();

        // private final String segment;

        public BlockTermsWriter(TermsIndexWriterBase termsIndexWriter,
            SegmentWriteState state, PostingsWriterBase postingsWriter)
        {
            string termsFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, TERMS_EXTENSION);
            this.termsIndexWriter = termsIndexWriter;
            m_output = state.Directory.CreateOutput(termsFileName, state.Context);
            bool success = false;
            try
            {
                //fieldInfos = state.FieldInfos; // LUCENENET: Not used
                WriteHeader(m_output);
                currentField = null;
                this.postingsWriter = postingsWriter;
                // segment = state.segmentName;

                //System.out.println("BTW.init seg=" + state.segmentName);

                postingsWriter.Init(m_output); // have consumer write its format/header
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

        public override TermsConsumer AddField(FieldInfo field)
        {
            //System.out.println("\nBTW.addField seg=" + segment + " field=" + field.name);
            if (Debugging.AssertsEnabled) Debugging.Assert(currentField is null || currentField.Name.CompareToOrdinal(field.Name) < 0);
            currentField = field;
            TermsIndexWriterBase.FieldWriter fieldIndexWriter = termsIndexWriter.AddField(field, m_output.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            return new TermsWriter(this, fieldIndexWriter, field, postingsWriter);
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

                        m_output.WriteVInt32(fields.Count);
                        foreach (FieldMetaData field in fields)
                        {
                            m_output.WriteVInt32(field.FieldInfo.Number);
                            m_output.WriteVInt64(field.NumTerms);
                            m_output.WriteVInt64(field.TermsStartPointer);
                            if (field.FieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                            {
                                m_output.WriteVInt64(field.SumTotalTermFreq);
                            }
                            m_output.WriteVInt64(field.SumDocFreq);
                            m_output.WriteVInt32(field.DocCount);
                            if (VERSION_CURRENT >= VERSION_META_ARRAY)
                            {
                                m_output.WriteVInt32(field.Int64sSize);
                            }
                        }
                        WriteTrailer(dirStart);
                        CodecUtil.WriteFooter(m_output);
                    }
                    finally
                    {
                        IOUtils.Dispose(m_output, postingsWriter, termsIndexWriter);
                        m_output = null;
                    }
                }
            }
        }

        private void WriteTrailer(long dirStart)
        {
            m_output.WriteInt64(dirStart);
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

            private readonly FieldInfo fieldInfo;
            private readonly PostingsWriterBase postingsWriter;
            private readonly long termsStartPointer;
            private long numTerms;
            private readonly TermsIndexWriterBase.FieldWriter fieldIndexWriter;
            //long sumTotalTermFreq; // LUCENENET: Not used
            //long sumDocFreq; // LUCENENET: Not used
            //int docCount; // LUCENENET: Not used
            private readonly int longsSize;

            private TermEntry[] pendingTerms;

            private int pendingCount;

            internal TermsWriter(
                BlockTermsWriter outerInstance,
                TermsIndexWriterBase.FieldWriter fieldIndexWriter,
                FieldInfo fieldInfo,
                PostingsWriterBase postingsWriter)
            {
                this.outerInstance = outerInstance;

                this.fieldInfo = fieldInfo;
                this.fieldIndexWriter = fieldIndexWriter;
                pendingTerms = new TermEntry[32];
                for (int i = 0; i < pendingTerms.Length; i++)
                {
                    pendingTerms[i] = new TermEntry();
                }
                termsStartPointer = outerInstance.m_output.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                this.postingsWriter = postingsWriter;
                this.longsSize = postingsWriter.SetField(fieldInfo);
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                //System.out.println("BTW: startTerm term=" + fieldInfo.name + ":" + text.utf8ToString() + " " + text + " seg=" + segment);
                postingsWriter.StartTerm();
                return postingsWriter;
            }

            private readonly BytesRef lastPrevTerm = new BytesRef();

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(stats.DocFreq > 0);
                //System.out.println("BTW: finishTerm term=" + fieldInfo.name + ":" + text.utf8ToString() + " " + text + " seg=" + segment + " df=" + stats.docFreq);

                bool isIndexTerm = fieldIndexWriter.CheckIndexTerm(text, stats);

                if (isIndexTerm)
                {
                    if (pendingCount > 0)
                    {
                        // Instead of writing each term, live, we gather terms
                        // in RAM in a pending buffer, and then write the
                        // entire block in between index terms:
                        FlushBlock();
                    }
                    fieldIndexWriter.Add(text, stats, outerInstance.m_output.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    //System.out.println("  index term!");
                }

                if (pendingTerms.Length == pendingCount)
                {
                    TermEntry[] newArray = new TermEntry[ArrayUtil.Oversize(pendingCount + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Arrays.Copy(pendingTerms, 0, newArray, 0, pendingCount);
                    for (int i = pendingCount; i < newArray.Length; i++)
                    {
                        newArray[i] = new TermEntry();
                    }
                    pendingTerms = newArray;
                }
                TermEntry te = pendingTerms[pendingCount];
                te.Term.CopyBytes(text);
                te.State = postingsWriter.NewTermState();
                te.State.DocFreq = stats.DocFreq;
                te.State.TotalTermFreq = stats.TotalTermFreq;
                postingsWriter.FinishTerm(te.State);

                pendingCount++;
                numTerms++;
            }

            // Finishes all terms in this field
            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                if (pendingCount > 0)
                {
                    FlushBlock();
                }
                // EOF marker:
                outerInstance.m_output.WriteVInt32(0);

                //this.sumTotalTermFreq = sumTotalTermFreq; // LUCENENET: Not used
                //this.sumDocFreq = sumDocFreq; // LUCENENET: Not used
                //this.docCount = docCount; // LUCENENET: Not used
                fieldIndexWriter.Finish(outerInstance.m_output.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                if (numTerms > 0)
                {
                    outerInstance.fields.Add(new FieldMetaData(fieldInfo,
                                                 numTerms,
                                                 termsStartPointer,
                                                 sumTotalTermFreq,
                                                 sumDocFreq,
                                                 docCount,
                                                 longsSize));
                }
            }

            private static int SharedPrefix(BytesRef term1, BytesRef term2) // LUCENENET: CA1822: Mark members as static
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(term1.Offset == 0);
                    Debugging.Assert(term2.Offset == 0);
                }
                int pos1 = 0;
                int pos1End = pos1 + Math.Min(term1.Length, term2.Length);
                int pos2 = 0;
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

            private readonly RAMOutputStream bytesWriter = new RAMOutputStream();
            private readonly RAMOutputStream bufferWriter = new RAMOutputStream();

            private void FlushBlock()
            {
                //System.out.println("BTW.flushBlock seg=" + segment + " pendingCount=" + pendingCount + " fp=" + out.getFilePointer());

                // First pass: compute common prefix for all terms
                // in the block, against term before first term in
                // this block:
                int commonPrefix = SharedPrefix(lastPrevTerm, pendingTerms[0].Term);
                for (int termCount = 1; termCount < pendingCount; termCount++)
                {
                    commonPrefix = Math.Min(commonPrefix,
                                            SharedPrefix(lastPrevTerm,
                                                         pendingTerms[termCount].Term));
                }

                outerInstance.m_output.WriteVInt32(pendingCount);
                outerInstance.m_output.WriteVInt32(commonPrefix);

                // 2nd pass: write suffixes, as separate byte[] blob
                for (int termCount = 0; termCount < pendingCount; termCount++)
                {
                    int suffix = pendingTerms[termCount].Term.Length - commonPrefix;
                    // TODO: cutover to better intblock codec, instead
                    // of interleaving here:
                    bytesWriter.WriteVInt32(suffix);
                    bytesWriter.WriteBytes(pendingTerms[termCount].Term.Bytes, commonPrefix, suffix);
                }
                outerInstance.m_output.WriteVInt32((int)bytesWriter.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                bytesWriter.WriteTo(outerInstance.m_output);
                bytesWriter.Reset();

                // 3rd pass: write the freqs as byte[] blob
                // TODO: cutover to better intblock codec.  simple64?
                // write prefix, suffix first:
                for (int termCount = 0; termCount < pendingCount; termCount++)
                {
                    BlockTermState state = pendingTerms[termCount].State;
                    if (Debugging.AssertsEnabled) Debugging.Assert(state != null);
                    bytesWriter.WriteVInt32(state.DocFreq);
                    if (fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                    {
                        bytesWriter.WriteVInt64(state.TotalTermFreq - state.DocFreq);
                    }
                }
                outerInstance.m_output.WriteVInt32((int)bytesWriter.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                bytesWriter.WriteTo(outerInstance.m_output);
                bytesWriter.Reset();

                // 4th pass: write the metadata 
                long[] longs = new long[longsSize];
                bool absolute = true;
                for (int termCount = 0; termCount < pendingCount; termCount++)
                {
                    BlockTermState state = pendingTerms[termCount].State;
                    postingsWriter.EncodeTerm(longs, bufferWriter, fieldInfo, state, absolute);
                    for (int i = 0; i < longsSize; i++)
                    {
                        bytesWriter.WriteVInt64(longs[i]);
                    }
                    bufferWriter.WriteTo(bytesWriter);
                    bufferWriter.Reset();
                    absolute = false;
                }
                outerInstance.m_output.WriteVInt32((int)bytesWriter.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                bytesWriter.WriteTo(outerInstance.m_output);
                bytesWriter.Reset();

                lastPrevTerm.CopyBytes(pendingTerms[pendingCount - 1].Term);
                pendingCount = 0;
            }
        }
    }
}