using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.asserting
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
    /// Just like <seealso cref="Lucene41PostingsFormat"/> but with additional asserts.
    /// </summary>
    public sealed class AssertingPostingsFormat : PostingsFormat
    {
        private readonly PostingsFormat @in = new Lucene41PostingsFormat();

        public AssertingPostingsFormat()
            : base("Asserting")
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

            public override void Dispose()
            {
                Dispose(true);
            }

            protected void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
            }

            public override IEnumerator<string> GetEnumerator()
            {
                IEnumerator<string> iterator = @in.GetEnumerator();
                Debug.Assert(iterator != null);
                return iterator;
            }

            public override Terms Terms(string field)
            {
                Terms terms = @in.Terms(field);
                return terms == null ? null : new AssertingAtomicReader.AssertingTerms(terms);
            }

            public override int Size
            {
                get { return @in.Size; }
            }

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

            public override void Dispose()
            {
                Dispose(true);
            }

            protected void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
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
            internal BytesRef LastTerm = null;
            internal TermsConsumerState State = TermsConsumerState.INITIAL;
            internal AssertingPostingsConsumer LastPostingsConsumer = null;
            internal long SumTotalTermFreq = 0;
            internal long SumDocFreq = 0;
            internal OpenBitSet VisitedDocs = new OpenBitSet();

            internal AssertingTermsConsumer(TermsConsumer @in, FieldInfo fieldInfo)
            {
                this.@in = @in;
                this.fieldInfo = fieldInfo;
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                Debug.Assert(State == TermsConsumerState.INITIAL || State == TermsConsumerState.START && LastPostingsConsumer.DocFreq == 0);
                State = TermsConsumerState.START;
                Debug.Assert(LastTerm == null || @in.Comparator.Compare(text, LastTerm) > 0);
                LastTerm = BytesRef.DeepCopyOf(text);
                return LastPostingsConsumer = new AssertingPostingsConsumer(@in.StartTerm(text), fieldInfo, VisitedDocs);
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                Debug.Assert(State == TermsConsumerState.START);
                State = TermsConsumerState.INITIAL;
                Debug.Assert(text.Equals(LastTerm));
                Debug.Assert(stats.DocFreq > 0); // otherwise, this method should not be called.
                Debug.Assert(stats.DocFreq == LastPostingsConsumer.DocFreq);
                SumDocFreq += stats.DocFreq;
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    Debug.Assert(stats.TotalTermFreq == -1);
                }
                else
                {
                    Debug.Assert(stats.TotalTermFreq == LastPostingsConsumer.TotalTermFreq);
                    SumTotalTermFreq += stats.TotalTermFreq;
                }
                @in.FinishTerm(text, stats);
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                Debug.Assert(State == TermsConsumerState.INITIAL || State == TermsConsumerState.START && LastPostingsConsumer.DocFreq == 0);
                State = TermsConsumerState.FINISHED;
                Debug.Assert(docCount >= 0);
                Debug.Assert(docCount == VisitedDocs.Cardinality());
                Debug.Assert(sumDocFreq >= docCount);
                Debug.Assert(sumDocFreq == this.SumDocFreq);
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    Debug.Assert(sumTotalTermFreq == -1);
                }
                else
                {
                    Debug.Assert(sumTotalTermFreq >= sumDocFreq);
                    Debug.Assert(sumTotalTermFreq == this.SumTotalTermFreq);
                }
                @in.Finish(sumTotalTermFreq, sumDocFreq, docCount);
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return @in.Comparator;
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
            internal readonly OpenBitSet VisitedDocs;
            internal PostingsConsumerState State = PostingsConsumerState.INITIAL;
            internal int Freq;
            internal int PositionCount;
            internal int LastPosition = 0;
            internal int LastStartOffset = 0;
            internal int DocFreq = 0;
            internal long TotalTermFreq = 0;

            internal AssertingPostingsConsumer(PostingsConsumer @in, FieldInfo fieldInfo, OpenBitSet visitedDocs)
            {
                this.@in = @in;
                this.fieldInfo = fieldInfo;
                this.VisitedDocs = visitedDocs;
            }

            public override void StartDoc(int docID, int freq)
            {
                Debug.Assert(State == PostingsConsumerState.INITIAL);
                State = PostingsConsumerState.START;
                Debug.Assert(docID >= 0);
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY)
                {
                    Debug.Assert(freq == -1);
                    this.Freq = 0; // we don't expect any positions here
                }
                else
                {
                    Debug.Assert(freq > 0);
                    this.Freq = freq;
                    TotalTermFreq += freq;
                }
                this.PositionCount = 0;
                this.LastPosition = 0;
                this.LastStartOffset = 0;
                DocFreq++;
                VisitedDocs.Set(docID);
                @in.StartDoc(docID, freq);
            }

            public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
            {
                Debug.Assert(State == PostingsConsumerState.START);
                Debug.Assert(PositionCount < Freq);
                PositionCount++;
                Debug.Assert(position >= LastPosition || position == -1); // we still allow -1 from old 3.x indexes
                LastPosition = position;
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                {
                    Debug.Assert(startOffset >= 0);
                    Debug.Assert(startOffset >= LastStartOffset);
                    LastStartOffset = startOffset;
                    Debug.Assert(endOffset >= startOffset);
                }
                else
                {
                    Debug.Assert(startOffset == -1);
                    Debug.Assert(endOffset == -1);
                }
                if (payload != null)
                {
                    Debug.Assert(fieldInfo.HasPayloads());
                }
                @in.AddPosition(position, payload, startOffset, endOffset);
            }

            public override void FinishDoc()
            {
                Debug.Assert(State == PostingsConsumerState.START);
                State = PostingsConsumerState.INITIAL;
                if (fieldInfo.IndexOptions < IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    Debug.Assert(PositionCount == 0); // we should not have fed any positions!
                }
                else
                {
                    Debug.Assert(PositionCount == Freq);
                }
                @in.FinishDoc();
            }
        }
    }
}