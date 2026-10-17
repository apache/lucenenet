using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Codecs.Cranky
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

    internal class CrankyPostingsFormat : PostingsFormat
    {
        private readonly PostingsFormat @delegate;
        private readonly Random random;

        internal CrankyPostingsFormat(PostingsFormat @delegate, Random random)
            // we impersonate the passed-in codec, so we don't need to be in SPI,
            // and so we don't change file formats
            : base(@delegate.Name)
        {
            this.@delegate = @delegate;
            this.random = random;
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            if (random.Next(100) == 0)
            {
                throw new IOException("Fake IOException from PostingsFormat.FieldsConsumer()");
            }

            return new CrankyFieldsConsumer(@delegate.FieldsConsumer(state), random);
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state) => @delegate.FieldsProducer(state);

        private class CrankyFieldsConsumer : FieldsConsumer
        {
            private readonly FieldsConsumer @delegate;
            private readonly Random random;

            public CrankyFieldsConsumer(FieldsConsumer @delegate, Random random)
            {
                this.@delegate = @delegate;
                this.random = random;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from FieldsConsumer.AddField()");
                }

                return new CrankyTermsConsumer(@delegate.AddField(field), random);
            }

            public override void Merge(MergeState mergeState, Fields fields)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from FieldsConsumer.Merge()");
                }

                base.Merge(mergeState, fields);
            }

            protected override void Dispose(bool disposing)
            {
                @delegate.Dispose();
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from FieldsConsumer.Dispose()");
                }
            }
        }

        private class CrankyTermsConsumer : TermsConsumer
        {
            private readonly TermsConsumer @delegate;
            private readonly Random random;

            public CrankyTermsConsumer(TermsConsumer @delegate, Random random)
            {
                this.@delegate = @delegate;
                this.random = random;
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermsConsumer.StartTerm()");
                }

                return new CrankyPostingsConsumer(@delegate.StartTerm(text), random);
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermsConsumer.FinishTerm()");
                }

                @delegate.FinishTerm(text, stats);
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from TermsConsumer.Finish()");
                }

                @delegate.Finish(sumTotalTermFreq, sumDocFreq, docCount);
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    if (random.Next(100) == 0)
                    {
                        throw new IOException("Fake IOException from TermsConsumer.Comparer");
                    }

                    return @delegate.Comparer;
                }
            }

            public override void Merge(MergeState mergeState, IndexOptions indexOptions, TermsEnum termsEnum)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from TermsConsumer.merge()");
                }

                base.Merge(mergeState, indexOptions, termsEnum);
            }
        }

        private class CrankyPostingsConsumer : PostingsConsumer
        {
            private readonly PostingsConsumer @delegate;
            private readonly Random random;

            public CrankyPostingsConsumer(PostingsConsumer @delegate, Random random)
            {
                this.@delegate = @delegate;
                this.random = random;
            }

            public override void StartDoc(int docId, int freq)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from PostingsConsumer.StartDoc()");
                }

                @delegate.StartDoc(docId, freq);
            }

            public override void FinishDoc()
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from PostingsConsumer.FinishDoc()");
                }

                @delegate.FinishDoc();
            }

            public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from PostingsConsumer.AddPosition()");
                }

                @delegate.AddPosition(position, payload, startOffset, endOffset);
            }

            public override TermStats Merge(MergeState mergeState, IndexOptions indexOptions, DocsEnum postings,
                FixedBitSet visitedDocs)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from PostingsConsumer.Merge()");
                }

                return base.Merge(mergeState, indexOptions, postings, visitedDocs);
            }
        }
    }
}
