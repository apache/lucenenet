using System.Diagnostics;

namespace Lucene.Net.Codecs.asserting
{
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IIndexableField = Lucene.Net.Index.IIndexableField;
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

    using Lucene41StoredFieldsFormat = Lucene.Net.Codecs.Lucene41.Lucene41StoredFieldsFormat;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using StoredFieldVisitor = Lucene.Net.Index.StoredFieldVisitor;

    /// <summary>
    /// Just like <seealso cref="Lucene41StoredFieldsFormat"/> but with additional asserts.
    /// </summary>
    public class AssertingStoredFieldsFormat : StoredFieldsFormat
    {
        private readonly StoredFieldsFormat @in = new Lucene41StoredFieldsFormat();

        public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            return new AssertingStoredFieldsReader(@in.FieldsReader(directory, si, fn, context), si.DocCount);
        }

        public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
        {
            return new AssertingStoredFieldsWriter(@in.FieldsWriter(directory, si, context));
        }

        internal class AssertingStoredFieldsReader : StoredFieldsReader
        {
            internal readonly StoredFieldsReader @in;
            internal readonly int MaxDoc;

            internal AssertingStoredFieldsReader(StoredFieldsReader @in, int maxDoc)
            {
                this.@in = @in;
                this.MaxDoc = maxDoc;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
            }

            public override void VisitDocument(int n, StoredFieldVisitor visitor)
            {
                Debug.Assert(n >= 0 && n < MaxDoc);
                @in.VisitDocument(n, visitor);
            }

            public override object Clone()
            {
                return new AssertingStoredFieldsReader((StoredFieldsReader)@in.Clone(), MaxDoc);
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

        internal class AssertingStoredFieldsWriter : StoredFieldsWriter
        {
            internal readonly StoredFieldsWriter @in;
            internal int NumWritten;
            internal int FieldCount;
            internal Status DocStatus;

            internal AssertingStoredFieldsWriter(StoredFieldsWriter @in)
            {
                this.@in = @in;
                this.DocStatus = Status.UNDEFINED;
            }

            public override void StartDocument(int numStoredFields)
            {
                Debug.Assert(DocStatus != Status.STARTED);
                @in.StartDocument(numStoredFields);
                Debug.Assert(FieldCount == 0);
                FieldCount = numStoredFields;
                NumWritten++;
                DocStatus = Status.STARTED;
            }

            public override void FinishDocument()
            {
                Debug.Assert(DocStatus == Status.STARTED);
                Debug.Assert(FieldCount == 0);
                @in.FinishDocument();
                DocStatus = Status.FINISHED;
            }

            public override void WriteField(FieldInfo info, IIndexableField field)
            {
                Debug.Assert(DocStatus == Status.STARTED);
                @in.WriteField(info, field);
                Debug.Assert(FieldCount > 0);
                FieldCount--;
            }

            public override void Abort()
            {
                @in.Abort();
            }

            public override void Finish(FieldInfos fis, int numDocs)
            {
                Debug.Assert(DocStatus == (numDocs > 0 ? Status.FINISHED : Status.UNDEFINED));
                @in.Finish(fis, numDocs);
                Debug.Assert(FieldCount == 0);
                Debug.Assert(numDocs == NumWritten);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
            }
        }
    }
}