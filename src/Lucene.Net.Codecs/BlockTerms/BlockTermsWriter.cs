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

        public const String CODEC_NAME = "BLOCK_TERMS_DICT";

        // Initial format
        public const int VERSION_START = 0;
        public const int VERSION_APPEND_ONLY = 1;
        public const int VERSION_META_ARRAY = 2;
        public const int VERSION_CHECKSUM = 3;
        public const int VERSION_CURRENT = VERSION_CHECKSUM;

        /** Extension of terms file */
        public const String TERMS_EXTENSION = "tib";

        protected IndexOutput output;
        protected readonly PostingsWriterBase postingsWriter;
        protected readonly FieldInfos fieldInfos;
        protected FieldInfo currentField;
        private readonly TermsIndexWriterBase termsIndexWriter;
        private readonly List<FieldMetaData> fields = new List<FieldMetaData>();

        public BlockTermsWriter(TermsIndexWriterBase termsIndexWriter,
            SegmentWriteState state, PostingsWriterBase postingsWriter)
        {
            String termsFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                TERMS_EXTENSION);
            this.termsIndexWriter = termsIndexWriter;
            output = state.Directory.CreateOutput(termsFileName, state.Context);
            bool success = false;

            try
            {
                fieldInfos = state.FieldInfos;
                WriteHeader(output);
                currentField = null;
                this.postingsWriter = postingsWriter;

                postingsWriter.Init(output); // have consumer write its format/header
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

        public override TermsConsumer AddField(FieldInfo field)
        {
            Debug.Assert(currentField == null || currentField.Name.CompareTo(field.Name) < 0);

            currentField = field;
            var fiw = termsIndexWriter.AddField(field, output.FilePointer);
            return new TermsWriter(fiw, field, postingsWriter);
        }

        public override void Dispose()
        {
            if (output != null)
            {
                try
                {
                    long dirStart = output.FilePointer;

                    output.WriteVInt(fields.Size);

                    foreach (var field in fields)
                    {
                        output.WriteVInt(field.FieldInfo.Number);
                        output.WriteVLong(field.NumTerms);
                        output.WriteVLong(field.TermsStartPointer);
                        if (field.FieldInfo.FieldIndexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
                        {
                            output.WriteVLong(field.SumTotalTermFreq);
                        }
                        output.WriteVLong(field.SumDocFreq);
                        output.WriteVInt(field.DocCount);
                        if (VERSION_CURRENT >= VERSION_META_ARRAY)
                        {
                            output.WriteVInt(field.LongsSize);
                        }

                    }
                    WriteTrailer(dirStart);
                    CodecUtil.WriteFooter(output);
                }
                finally
                {
                    IOUtils.Close(output, postingsWriter, termsIndexWriter);
                    output = null;
                }
            }
        }

        private void WriteTrailer(long dirStart)
        {
            output.WriteLong(dirStart);
        }


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

        private class TermEntry
        {
            public readonly BytesRef Term = new BytesRef();
            public BlockTermState State;
        }

        public class TermsWriter : TermsConsumer
        {
            private readonly FieldInfo fieldInfo;
            private readonly PostingsWriterBase postingsWriter;
            private readonly long termsStartPointer;

            private readonly BytesRef lastPrevTerm = new BytesRef();
            private readonly TermsIndexWriterBase.FieldWriter fieldIndexWriter;

            private long numTerms;
            private long sumTotalTermFreq;
            private long sumDocFreq;
            private int docCount;
            private int longsSize;

            private TermEntry[] pendingTerms;

            private int pendingCount;

            private TermsWriter(
                TermsIndexWriterBase.FieldWriter fieldIndexWriter,
                FieldInfo fieldInfo,
                PostingsWriterBase postingsWriter)
            {
                this.fieldInfo = fieldInfo;
                this.fieldIndexWriter = fieldIndexWriter;
                pendingTerms = new TermEntry[32];
                for (int i = 0; i < pendingTerms.Length; i++)
                {
                    pendingTerms[i] = new TermEntry();
                }
                termsStartPointer = output.FilePointer;
                this.postingsWriter = postingsWriter;
                this.longsSize = postingsWriter.SetField(fieldInfo);
            }

            public override IComparer<BytesRef> Comparator()
            {
                return BytesRef.UTF8SortedAsUnicodeComparer;
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                postingsWriter.StartTerm();
                return postingsWriter;
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {

                Debug.Assert(stats.DocFreq > 0);

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
                    fieldIndexWriter.Add(text, stats, output.FilePointer);
                }

                if (pendingTerms.Length == pendingCount)
                {
                    TermEntry[] newArray =
                        new TermEntry[ArrayUtil.Oversize(pendingCount + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    System.Arraycopy(pendingTerms, 0, newArray, 0, pendingCount);
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
                output.WriteVInt(0);

                this.sumTotalTermFreq = sumTotalTermFreq;
                this.sumDocFreq = sumDocFreq;
                this.docCount = docCount;
                fieldIndexWriter.Finish(output.FilePointer);

                if (numTerms > 0)
                {
                    fields.Add(new FieldMetaData(fieldInfo,
                        numTerms,
                        termsStartPointer,
                        sumTotalTermFreq,
                        sumDocFreq,
                        docCount,
                        longsSize));
                }
            }

            private int SharedPrefix(BytesRef term1, BytesRef term2)
            {
                Debug.Assert(term1.Offset == 0);
                Debug.Assert(term2.Offset == 0);
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

                output.WriteVInt(pendingCount);
                output.WriteVInt(commonPrefix);

                // 2nd pass: write suffixes, as separate byte[] blob
                for (int termCount = 0; termCount < pendingCount; termCount++)
                {
                    int suffix = pendingTerms[termCount].Term.Length - commonPrefix;
                    // TODO: cutover to better intblock codec, instead
                    // of interleaving here:
                    bytesWriter.WriteVInt(suffix);
                    bytesWriter.WriteBytes(pendingTerms[termCount].Term.Bytes, commonPrefix, suffix);
                }
                output.WriteVInt((int) bytesWriter.FilePointer);
                bytesWriter.WriteTo(output);
                bytesWriter.Reset();

                // 3rd pass: write the freqs as byte[] blob
                // TODO: cutover to better intblock codec.  simple64?
                // write prefix, suffix first:
                for (int termCount = 0; termCount < pendingCount; termCount++)
                {
                    BlockTermState state = pendingTerms[termCount].State;

                    Debug.Assert(state != null);

                    bytesWriter.WriteVInt(state.DocFreq);
                    if (fieldInfo.FieldIndexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
                    {
                        bytesWriter.WriteVLong(state.TotalTermFreq - state.DocFreq);
                    }
                }
                output.WriteVInt((int) bytesWriter.FilePointer);
                bytesWriter.WriteTo(output);
                bytesWriter.Reset();

                // 4th pass: write the metadata 
                var longs = new long[longsSize];
                bool absolute = true;
                for (int termCount = 0; termCount < pendingCount; termCount++)
                {
                    BlockTermState state = pendingTerms[termCount].State;
                    postingsWriter.EncodeTerm(longs, bufferWriter, fieldInfo, state, absolute);
                    for (int i = 0; i < longsSize; i++)
                    {
                        bytesWriter.WriteVLong(longs[i]);
                    }
                    bufferWriter.WriteTo(bytesWriter);
                    bufferWriter.Reset();
                    absolute = false;
                }
                output.WriteVInt((int) bytesWriter.FilePointer);
                bytesWriter.WriteTo(output);
                bytesWriter.Reset();

                lastPrevTerm.CopyBytes(pendingTerms[pendingCount - 1].Term);
                pendingCount = 0;
            }
        }

    }
}