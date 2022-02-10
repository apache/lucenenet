using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
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
    /// Just like <see cref="Lucene40TermVectorsFormat"/> but with additional asserts.
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
            private readonly TermVectorsReader @in;

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
                return fields is null ? null : new AssertingFields(fields);
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
            private readonly TermVectorsWriter @in;
            private Status docStatus, fieldStatus, termStatus;
            private int docCount, fieldCount, termCount, positionCount;
            private bool hasPositions;

            internal AssertingTermVectorsWriter(TermVectorsWriter @in)
            {
                this.@in = @in;
                docStatus = Status.UNDEFINED;
                fieldStatus = Status.UNDEFINED;
                termStatus = Status.UNDEFINED;
                fieldCount = termCount = positionCount = 0;
            }

            public override void StartDocument(int numVectorFields)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldCount == 0);
                if (Debugging.AssertsEnabled) Debugging.Assert(docStatus != Status.STARTED);
                @in.StartDocument(numVectorFields);
                docStatus = Status.STARTED;
                fieldCount = numVectorFields;
                docCount++;
            }

            public override void FinishDocument()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldCount == 0);
                if (Debugging.AssertsEnabled) Debugging.Assert(docStatus == Status.STARTED);
                @in.FinishDocument();
                docStatus = Status.FINISHED;
            }

            public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(termCount == 0);
                if (Debugging.AssertsEnabled) Debugging.Assert(docStatus == Status.STARTED);
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldStatus != Status.STARTED);
                @in.StartField(info, numTerms, positions, offsets, payloads);
                fieldStatus = Status.STARTED;
                termCount = numTerms;
                hasPositions = positions || offsets || payloads;
            }

            public override void FinishField()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(termCount == 0);
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldStatus == Status.STARTED);
                @in.FinishField();
                fieldStatus = Status.FINISHED;
                --fieldCount;
            }

            public override void StartTerm(BytesRef term, int freq)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(docStatus == Status.STARTED);
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldStatus == Status.STARTED);
                if (Debugging.AssertsEnabled) Debugging.Assert(termStatus != Status.STARTED);
                @in.StartTerm(term, freq);
                termStatus = Status.STARTED;
                positionCount = hasPositions ? freq : 0;
            }

            public override void FinishTerm()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(positionCount == 0);
                if (Debugging.AssertsEnabled) Debugging.Assert(docStatus == Status.STARTED);
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldStatus == Status.STARTED);
                if (Debugging.AssertsEnabled) Debugging.Assert(termStatus == Status.STARTED);
                @in.FinishTerm();
                termStatus = Status.FINISHED;
                --termCount;
            }

            public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(docStatus == Status.STARTED);
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldStatus == Status.STARTED);
                if (Debugging.AssertsEnabled) Debugging.Assert(termStatus == Status.STARTED);
                @in.AddPosition(position, startOffset, endOffset, payload);
                --positionCount;
            }

            public override void Abort()
            {
                @in.Abort();
            }

            public override void Finish(FieldInfos fis, int numDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(docCount == numDocs);
                if (Debugging.AssertsEnabled) Debugging.Assert(docStatus == (numDocs > 0 ? Status.FINISHED : Status.UNDEFINED));
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldStatus != Status.STARTED);
                if (Debugging.AssertsEnabled) Debugging.Assert(termStatus != Status.STARTED);
                @in.Finish(fis, numDocs);
            }

            public override IComparer<BytesRef> Comparer => @in.Comparer;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
            }
        }
    }
}