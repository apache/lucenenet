using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.asserting
{
    using AssertingAtomicReader = Lucene.Net.Index.AssertingAtomicReader;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
    using IOContext = Lucene.Net.Store.IOContext;

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

    using Lucene40TermVectorsFormat = Lucene.Net.Codecs.Lucene40.Lucene40TermVectorsFormat;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Just like <seealso cref="Lucene40TermVectorsFormat"/> but with additional asserts.
    /// </summary>
    public class AssertingTermVectorsFormat : TermVectorsFormat
    {
        private readonly TermVectorsFormat @in = new Lucene40TermVectorsFormat();

        public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            return new AssertingTermVectorsReader(@in.VectorsReader(directory, segmentInfo, fieldInfos, context));
        }

        public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            return new AssertingTermVectorsWriter(@in.VectorsWriter(directory, segmentInfo, context));
        }

        internal class AssertingTermVectorsReader : TermVectorsReader
        {
            internal readonly TermVectorsReader @in;

            internal AssertingTermVectorsReader(TermVectorsReader @in)
            {
                this.@in = @in;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
            }

            public override Fields Get(int doc)
            {
                Fields fields = @in.Get(doc);
                return fields == null ? null : new AssertingAtomicReader.AssertingFields(fields);
            }

            public override object Clone()
            {
                return new AssertingTermVectorsReader((TermVectorsReader)@in.Clone());
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

        internal enum Status
        {
            UNDEFINED,
            STARTED,
            FINISHED
        }

        internal class AssertingTermVectorsWriter : TermVectorsWriter
        {
            internal readonly TermVectorsWriter @in;
            internal Status DocStatus, FieldStatus, TermStatus;
            internal int DocCount, FieldCount, TermCount, PositionCount;
            internal bool HasPositions;

            internal AssertingTermVectorsWriter(TermVectorsWriter @in)
            {
                this.@in = @in;
                DocStatus = Status.UNDEFINED;
                FieldStatus = Status.UNDEFINED;
                TermStatus = Status.UNDEFINED;
                FieldCount = TermCount = PositionCount = 0;
            }

            public override void StartDocument(int numVectorFields)
            {
                Debug.Assert(FieldCount == 0);
                Debug.Assert(DocStatus != Status.STARTED);
                @in.StartDocument(numVectorFields);
                DocStatus = Status.STARTED;
                FieldCount = numVectorFields;
                DocCount++;
            }

            public override void FinishDocument()
            {
                Debug.Assert(FieldCount == 0);
                Debug.Assert(DocStatus == Status.STARTED);
                @in.FinishDocument();
                DocStatus = Status.FINISHED;
            }

            public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
            {
                Debug.Assert(TermCount == 0);
                Debug.Assert(DocStatus == Status.STARTED);
                Debug.Assert(FieldStatus != Status.STARTED);
                @in.StartField(info, numTerms, positions, offsets, payloads);
                FieldStatus = Status.STARTED;
                TermCount = numTerms;
                HasPositions = positions || offsets || payloads;
            }

            public override void FinishField()
            {
                Debug.Assert(TermCount == 0);
                Debug.Assert(FieldStatus == Status.STARTED);
                @in.FinishField();
                FieldStatus = Status.FINISHED;
                --FieldCount;
            }

            public override void StartTerm(BytesRef term, int freq)
            {
                Debug.Assert(DocStatus == Status.STARTED);
                Debug.Assert(FieldStatus == Status.STARTED);
                Debug.Assert(TermStatus != Status.STARTED);
                @in.StartTerm(term, freq);
                TermStatus = Status.STARTED;
                PositionCount = HasPositions ? freq : 0;
            }

            public override void FinishTerm()
            {
                Debug.Assert(PositionCount == 0);
                Debug.Assert(DocStatus == Status.STARTED);
                Debug.Assert(FieldStatus == Status.STARTED);
                Debug.Assert(TermStatus == Status.STARTED);
                @in.FinishTerm();
                TermStatus = Status.FINISHED;
                --TermCount;
            }

            public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
            {
                Debug.Assert(DocStatus == Status.STARTED);
                Debug.Assert(FieldStatus == Status.STARTED);
                Debug.Assert(TermStatus == Status.STARTED);
                @in.AddPosition(position, startOffset, endOffset, payload);
                --PositionCount;
            }

            public override void Abort()
            {
                @in.Abort();
            }

            public override void Finish(FieldInfos fis, int numDocs)
            {
                Debug.Assert(DocCount == numDocs);
                Debug.Assert(DocStatus == (numDocs > 0 ? Status.FINISHED : Status.UNDEFINED));
                Debug.Assert(FieldStatus != Status.STARTED);
                Debug.Assert(TermStatus != Status.STARTED);
                @in.Finish(fis, numDocs);
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    return @in.Comparer;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
            }
        }
    }
}