using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

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

    internal class CrankyTermVectorsFormat : TermVectorsFormat
    {
        private readonly TermVectorsFormat @delegate;
        private readonly Random random;

        internal CrankyTermVectorsFormat(TermVectorsFormat @delegate, Random random)
        {
            this.@delegate = @delegate;
            this.random = random;
        }

        public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo,
            FieldInfos fieldInfos, IOContext context)
            => @delegate.VectorsReader(directory, segmentInfo, fieldInfos, context);

        public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            if (random.Next(100) == 0)
            {
                throw new IOException("Fake IOException from TermVectorsFormat.VectorsWriter()");
            }

            return new CrankyTermVectorsWriter(@delegate.VectorsWriter(directory, segmentInfo, context), random);
        }

        private class CrankyTermVectorsWriter : TermVectorsWriter
        {
            private readonly TermVectorsWriter @delegate;
            private readonly Random random;

            public CrankyTermVectorsWriter(TermVectorsWriter @delegate, Random random)
            {
                this.@delegate = @delegate;
                this.random = random;
            }

            public override void Abort()
            {
                @delegate.Abort();

                if (random.Next(100) == 0)
                {
                    throw RuntimeException.Create(new IOException("Fake IOException from TermVectorsWriter.Abort()"));
                }
            }

            public override int Merge(MergeState mergeState)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.Merge()");
                }

                return base.Merge(mergeState);
            }

            public override void Finish(FieldInfos fis, int numDocs)
            {
                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.Finish()");
                }

                @delegate.Finish(fis, numDocs);
            }

            protected override void Dispose(bool disposing)
            {
                @delegate.Dispose();

                if (random.Next(100) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.Dispose()");
                }
            }

            // per doc/field methods: lower probability since they are invoked so many times.

            public override void StartDocument(int numVectorFields)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.StartDocument()");
                }

                @delegate.StartDocument(numVectorFields);
            }

            public override void FinishDocument()
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.FinishDocument()");
                }

                @delegate.FinishDocument();
            }

            public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.StartField()");
                }

                @delegate.StartField(info, numTerms, positions, offsets, payloads);
            }

            public override void FinishField()
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.FinishField()");
                }

                @delegate.FinishField();
            }

            public override void StartTerm(BytesRef term, int freq)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.StartTerm()");
                }

                @delegate.StartTerm(term, freq);
            }

            public override void FinishTerm()
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.FinishTerm()");
                }

                @delegate.FinishTerm();
            }

            public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.AddPosition()");
                }

                @delegate.AddPosition(position, startOffset, endOffset, payload);
            }

            public override void AddProx(int numProx, DataInput positions, DataInput offsets)
            {
                if (random.Next(10000) == 0)
                {
                    throw new IOException("Fake IOException from TermVectorsWriter.AddProx()");
                }

                base.AddProx(numProx, positions, offsets);
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    if (random.Next(10000) == 0)
                    {
                        throw new IOException("Fake IOException from TermVectorsWriter.Comparer");
                    }

                    return @delegate.Comparer;
                }
            }
        }
    }
}
