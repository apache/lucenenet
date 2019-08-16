using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Asserting
{
    using AssertingAtomicReader = Lucene.Net.Index.AssertingAtomicReader;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexOptions = Lucene.Net.Index.IndexOptions;

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

    using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
    using OpenBitSet = Lucene.Net.Util.OpenBitSet;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using Terms = Lucene.Net.Index.Terms;

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
            internal readonly FieldsProducer @in;

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
                Debug.Assert(iterator != null);
                return iterator;
            }

            public override Terms GetTerms(string field)
            {
                Terms terms = @in.GetTerms(field);
                return terms == null ? null : new AssertingAtomicReader.AssertingTerms(terms);
            }

            public override int Count
            {
                get { return @in.Count; }
            }

            [Obsolete("iterate fields and add their Count instead.")]
            public override long UniqueTermCount
            {
                get
                {
                    return @in.UniqueTermCount;
                }
            }

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
            internal readonly FieldsConsumer @in;

            internal AssertingFieldsConsumer(FieldsConsumer @in)
            {
                this.@in = @in;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                TermsConsumer consumer = @in.AddField(field);
                Debug.Assert(consumer != null);
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
            internal readonly TermsConsumer @in;
            private readonly FieldInfo fieldInfo;
            internal BytesRef lastTerm = null;
            internal TermsConsumerState state = TermsConsumerState.INITIAL;
            internal AssertingPostingsConsumer lastPostingsConsumer = null;
            internal long sumTotalTermFreq = 0;
            internal long sumDocFreq = 0;
            internal OpenBitSet visitedDocs = new OpenBitSet();

            internal AssertingTermsConsumer(TermsConsumer @in, FieldInfo fieldInfo)
            {
                this.@in = @in;
                this.fieldInfo = fieldInfo;
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                Debug.Assert(state == TermsConsumerState.INITIAL || state == TermsConsumerState.START && lastPostingsConsumer.docFreq == 0);
                state = TermsConsumerState.START;
                Debug.Assert(lastTerm == null || @in.Comparer.Compare(text, lastTerm) > 0);
                lastTerm = BytesRef.DeepCopyOf(text);
                return lastPostingsConsumer = new AssertingPostingsConsumer(@in.StartTerm(text), fieldInfo, visitedDocs);
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                Debug.Assert(state == TermsConsumerState.START);
                state = TermsConsumerState.INITIAL;
                Debug.Assert(text.Equals(lastTerm));
                Debug.Assert(stats.DocFreq > 0); // otherwise, this method should not be called.
                Debug.Assert(stats.DocFreq == lastPostingsConsumer.docFreq);
                sumDocFreq += stats.DocFreq;
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    Debug.Assert(stats.TotalTermFreq == -1);
                }
                else
                {
                    Debug.Assert(stats.TotalTermFreq == lastPostingsConsumer.totalTermFreq);
                    sumTotalTermFreq += stats.TotalTermFreq;
                }
                @in.FinishTerm(text, stats);
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                Debug.Assert(state == TermsConsumerState.INITIAL || state == TermsConsumerState.START && lastPostingsConsumer.docFreq == 0);
                state = TermsConsumerState.FINISHED;
                Debug.Assert(docCount >= 0);
                Debug.Assert(docCount == visitedDocs.Cardinality());
                Debug.Assert(sumDocFreq >= docCount);
                Debug.Assert(sumDocFreq == this.sumDocFreq);
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    Debug.Assert(sumTotalTermFreq == -1);
                }
                else
                {
                    Debug.Assert(sumTotalTermFreq >= sumDocFreq);
                    Debug.Assert(sumTotalTermFreq == this.sumTotalTermFreq);
                }
                @in.Finish(sumTotalTermFreq, sumDocFreq, docCount);
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    return @in.Comparer;
                }
            }
        }

        internal enum PostingsConsumerState
        {
            INITIAL,
            START
        }

        internal class AssertingPostingsConsumer : PostingsConsumer
        {
            internal readonly PostingsConsumer @in;
            private readonly FieldInfo fieldInfo;
            internal readonly OpenBitSet visitedDocs;
            internal PostingsConsumerState state = PostingsConsumerState.INITIAL;
            internal int freq;
            internal int positionCount;
            internal int lastPosition = 0;
            internal int lastStartOffset = 0;
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
                Debug.Assert(state == PostingsConsumerState.INITIAL);
                state = PostingsConsumerState.START;
                Debug.Assert(docID >= 0);
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    Debug.Assert(freq == -1);
                    this.freq = 0; // we don't expect any positions here
                }
                else
                {
                    Debug.Assert(freq > 0);
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
                Debug.Assert(state == PostingsConsumerState.START);
                Debug.Assert(positionCount < freq);
                positionCount++;
                Debug.Assert(position >= lastPosition || position == -1); // we still allow -1 from old 3.x indexes
                lastPosition = position;
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                {
                    Debug.Assert(startOffset >= 0);
                    Debug.Assert(startOffset >= lastStartOffset);
                    lastStartOffset = startOffset;
                    Debug.Assert(endOffset >= startOffset);
                }
                else
                {
                    Debug.Assert(startOffset == -1);
                    Debug.Assert(endOffset == -1);
                }
                if (payload != null)
                {
                    Debug.Assert(fieldInfo.HasPayloads);
                }
                @in.AddPosition(position, payload, startOffset, endOffset);
            }

            public override void FinishDoc()
            {
                Debug.Assert(state == PostingsConsumerState.START);
                state = PostingsConsumerState.INITIAL;
                if (fieldInfo.IndexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                {
                    Debug.Assert(positionCount == 0); // we should not have fed any positions!
                }
                else
                {
                    Debug.Assert(positionCount == freq);
                }
                @in.FinishDoc();
            }
        }
    }
}