using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Asserting
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
    /// Just like <see cref="Lucene41PostingsFormat"/> but with additional asserts.
    /// </summary>
    [PostingsFormatName("Asserting")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class AssertingPostingsFormat : PostingsFormat
    {
        private readonly PostingsFormat @in = new Lucene41PostingsFormat();

        public AssertingPostingsFormat()
            : base()
        {
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new AssertingFieldsConsumer(@in.FieldsConsumer(state));
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            return new AssertingFieldsProducer(@in.FieldsProducer(state));
        }

        internal class AssertingFieldsProducer : FieldsProducer
        {
            private readonly FieldsProducer @in;

            internal AssertingFieldsProducer(FieldsProducer @in)
            {
                this.@in = @in;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    @in.Dispose();
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                IEnumerator<string> iterator = @in.GetEnumerator();
                if (Debugging.ShouldAssert(iterator != null)) Debugging.ThrowAssert();
                return iterator;
            }

            public override Terms GetTerms(string field)
            {
                Terms terms = @in.GetTerms(field);
                return terms == null ? null : new AssertingTerms(terms);
            }

            public override int Count => @in.Count;

            [Obsolete("iterate fields and add their Count instead.")]
            public override long UniqueTermCount => @in.UniqueTermCount;

            public override long RamBytesUsed()
            {
                return @in.RamBytesUsed();
            }

            public override void CheckIntegrity()
            {
                @in.CheckIntegrity();
            }
        }

        internal class AssertingFieldsConsumer : FieldsConsumer
        {
            private readonly FieldsConsumer @in;

            internal AssertingFieldsConsumer(FieldsConsumer @in)
            {
                this.@in = @in;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                TermsConsumer consumer = @in.AddField(field);
                if (Debugging.ShouldAssert(consumer != null)) Debugging.ThrowAssert();
                return new AssertingTermsConsumer(consumer, field);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    @in.Dispose();
                }
            }
        }

        internal enum TermsConsumerState
        {
            INITIAL,
            START,
            FINISHED
        }

        internal class AssertingTermsConsumer : TermsConsumer
        {
            private readonly TermsConsumer @in;
            private readonly FieldInfo fieldInfo;
            private BytesRef lastTerm = null;
            private TermsConsumerState state = TermsConsumerState.INITIAL;
            private AssertingPostingsConsumer lastPostingsConsumer = null;
            private long sumTotalTermFreq = 0;
            private long sumDocFreq = 0;
            private OpenBitSet visitedDocs = new OpenBitSet();

            internal AssertingTermsConsumer(TermsConsumer @in, FieldInfo fieldInfo)
            {
                this.@in = @in;
                this.fieldInfo = fieldInfo;
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                if (Debugging.ShouldAssert(state == TermsConsumerState.INITIAL || state == TermsConsumerState.START && lastPostingsConsumer.docFreq == 0)) Debugging.ThrowAssert();
                state = TermsConsumerState.START;
                if (Debugging.AssertsEnabled) Debugging.Assert(lastTerm == null || @in.Comparer.Compare(text, lastTerm) > 0);
                lastTerm = BytesRef.DeepCopyOf(text);
                return lastPostingsConsumer = new AssertingPostingsConsumer(@in.StartTerm(text), fieldInfo, visitedDocs);
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                if (Debugging.ShouldAssert(state == TermsConsumerState.START)) Debugging.ThrowAssert();
                state = TermsConsumerState.INITIAL;
                if (Debugging.ShouldAssert(text.Equals(lastTerm))) Debugging.ThrowAssert();
                if (Debugging.ShouldAssert(stats.DocFreq > 0)) Debugging.ThrowAssert(); // otherwise, this method should not be called.
                if (Debugging.ShouldAssert(stats.DocFreq == lastPostingsConsumer.docFreq)) Debugging.ThrowAssert();
                sumDocFreq += stats.DocFreq;
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    if (Debugging.ShouldAssert(stats.TotalTermFreq == -1)) Debugging.ThrowAssert();
                }
                else
                {
                    if (Debugging.ShouldAssert(stats.TotalTermFreq == lastPostingsConsumer.totalTermFreq)) Debugging.ThrowAssert();
                    sumTotalTermFreq += stats.TotalTermFreq;
                }
                @in.FinishTerm(text, stats);
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                if (Debugging.ShouldAssert(state == TermsConsumerState.INITIAL || state == TermsConsumerState.START && lastPostingsConsumer.docFreq == 0)) Debugging.ThrowAssert();
                state = TermsConsumerState.FINISHED;
                if (Debugging.ShouldAssert(docCount >= 0)) Debugging.ThrowAssert();
                if (Debugging.ShouldAssert(docCount == visitedDocs.Cardinality())) Debugging.ThrowAssert();
                if (Debugging.ShouldAssert(sumDocFreq >= docCount)) Debugging.ThrowAssert();
                if (Debugging.ShouldAssert(sumDocFreq == this.sumDocFreq)) Debugging.ThrowAssert();
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    if (Debugging.ShouldAssert(sumTotalTermFreq == -1)) Debugging.ThrowAssert();
                }
                else
                {
                    if (Debugging.ShouldAssert(sumTotalTermFreq >= sumDocFreq)) Debugging.ThrowAssert();
                    if (Debugging.ShouldAssert(sumTotalTermFreq == this.sumTotalTermFreq)) Debugging.ThrowAssert();
                }
                @in.Finish(sumTotalTermFreq, sumDocFreq, docCount);
            }

            public override IComparer<BytesRef> Comparer => @in.Comparer;
        }

        internal enum PostingsConsumerState
        {
            INITIAL,
            START
        }

        internal class AssertingPostingsConsumer : PostingsConsumer
        {
            private readonly PostingsConsumer @in;
            private readonly FieldInfo fieldInfo;
            private readonly OpenBitSet visitedDocs;
            private PostingsConsumerState state = PostingsConsumerState.INITIAL;
            private int freq;
            private int positionCount;
            private int lastPosition = 0;
            private int lastStartOffset = 0;
            internal int docFreq = 0;
            internal long totalTermFreq = 0;

            internal AssertingPostingsConsumer(PostingsConsumer @in, FieldInfo fieldInfo, OpenBitSet visitedDocs)
            {
                this.@in = @in;
                this.fieldInfo = fieldInfo;
                this.visitedDocs = visitedDocs;
            }

            public override void StartDoc(int docID, int freq)
            {
                if (Debugging.ShouldAssert(state == PostingsConsumerState.INITIAL)) Debugging.ThrowAssert();
                state = PostingsConsumerState.START;
                if (Debugging.ShouldAssert(docID >= 0)) Debugging.ThrowAssert();
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    if (Debugging.ShouldAssert(freq == -1)) Debugging.ThrowAssert();
                    this.freq = 0; // we don't expect any positions here
                }
                else
                {
                    if (Debugging.ShouldAssert(freq > 0)) Debugging.ThrowAssert();
                    this.freq = freq;
                    totalTermFreq += freq;
                }
                this.positionCount = 0;
                this.lastPosition = 0;
                this.lastStartOffset = 0;
                docFreq++;
                visitedDocs.Set(docID);
                @in.StartDoc(docID, freq);
            }

            public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
            {
                if (Debugging.ShouldAssert(state == PostingsConsumerState.START)) Debugging.ThrowAssert();
                if (Debugging.ShouldAssert(positionCount < freq)) Debugging.ThrowAssert();
                positionCount++;
                if (Debugging.ShouldAssert(position >= lastPosition || position == -1)) Debugging.ThrowAssert(); // we still allow -1 from old 3.x indexes
                lastPosition = position;
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                {
                    if (Debugging.ShouldAssert(startOffset >= 0)) Debugging.ThrowAssert();
                    if (Debugging.ShouldAssert(startOffset >= lastStartOffset)) Debugging.ThrowAssert();
                    lastStartOffset = startOffset;
                    if (Debugging.ShouldAssert(endOffset >= startOffset)) Debugging.ThrowAssert();
                }
                else
                {
                    if (Debugging.ShouldAssert(startOffset == -1)) Debugging.ThrowAssert();
                    if (Debugging.ShouldAssert(endOffset == -1)) Debugging.ThrowAssert();
                }
                if (payload != null)
                {
                    if (Debugging.ShouldAssert(fieldInfo.HasPayloads)) Debugging.ThrowAssert();
                }
                @in.AddPosition(position, payload, startOffset, endOffset);
            }

            public override void FinishDoc()
            {
                if (Debugging.ShouldAssert(state == PostingsConsumerState.START)) Debugging.ThrowAssert();
                state = PostingsConsumerState.INITIAL;
                if (fieldInfo.IndexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                {
                    if (Debugging.ShouldAssert(positionCount == 0)) Debugging.ThrowAssert(); // we should not have fed any positions!
                }
                else
                {
                    if (Debugging.ShouldAssert(positionCount == freq)) Debugging.ThrowAssert();
                }
                @in.FinishDoc();
            }
        }
    }
}