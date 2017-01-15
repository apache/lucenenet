using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Codecs.ramonly
{
    using Lucene.Net.Support;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Stores all postings data in RAM, but writes a small
    ///  token (header + single int) to identify which "slot" the
    ///  index is using in RAM HashMap.
    ///
    ///  NOTE: this codec sorts terms by reverse-unicode-order!
    /// </summary>

    public sealed class RAMOnlyPostingsFormat : PostingsFormat
    {
        // For fun, test that we can override how terms are
        // sorted, and basic things still work -- this comparer
        // sorts in reversed unicode code point order:
        private static readonly IComparer<BytesRef> reverseUnicodeComparer = new ComparerAnonymousInnerClassHelper();

        private class ComparerAnonymousInnerClassHelper : IComparer<BytesRef>
        {
            public ComparerAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(BytesRef t1, BytesRef t2)
            {
                var b1 = t1.Bytes;
                var b2 = t2.Bytes;
                int b1Stop;
                int b1Upto = t1.Offset;
                int b2Upto = t2.Offset;
                if (t1.Length < t2.Length)
                {
                    b1Stop = t1.Offset + t1.Length;
                }
                else
                {
                    b1Stop = t1.Offset + t2.Length;
                }
                while (b1Upto < b1Stop)
                {
                    int bb1 = b1[b1Upto++] & 0xff;
                    int bb2 = b2[b2Upto++] & 0xff;
                    if (bb1 != bb2)
                    {
                        //System.out.println("cmp 1=" + t1 + " 2=" + t2 + " return " + (bb2-bb1));
                        return bb2 - bb1;
                    }
                }

                // One is prefix of another, or they are equal
                return t2.Length - t1.Length;
            }

            public override bool Equals(object other)
            {
                return this == other;
            }
        }

        public RAMOnlyPostingsFormat()
            : base("RAMOnly")
        {
        }

        // Postings state:
        internal class RAMPostings : FieldsProducer
        {
            internal readonly IDictionary<string, RAMField> FieldToTerms = new SortedDictionary<string, RAMField>();

            public override Terms Terms(string field)
            {
                return FieldToTerms[field];
            }

            public override int Count
            {
                get { return FieldToTerms.Count; }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return FieldToTerms.Keys.GetEnumerator();
            }

            public override void Dispose()
            {
            }

            public override long RamBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (RAMField field in FieldToTerms.Values)
                {
                    sizeInBytes += field.RamBytesUsed();
                }
                return sizeInBytes;
            }

            public override void CheckIntegrity()
            {
            }
        }

        internal class RAMField : Terms
        {
            internal readonly string Field;
            internal readonly SortedDictionary<string, RAMTerm> TermToDocs = new SortedDictionary<string, RAMTerm>();
            internal long SumTotalTermFreq_Renamed;
            internal long SumDocFreq_Renamed;
            internal int DocCount_Renamed;
            internal readonly FieldInfo Info;

            internal RAMField(string field, FieldInfo info)
            {
                this.Field = field;
                this.Info = info;
            }

            /// <summary>
            /// Returns approximate RAM bytes used </summary>
            public virtual long RamBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (RAMTerm term in TermToDocs.Values)
                {
                    sizeInBytes += term.RamBytesUsed();
                }
                return sizeInBytes;
            }

            public override long Count
            {
                get { return TermToDocs.Count; }
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return SumTotalTermFreq_Renamed;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    return SumDocFreq_Renamed;
                }
            }

            public override int DocCount
            {
                get
                {
                    return DocCount_Renamed;
                }
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return new RAMTermsEnum(this);
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    return reverseUnicodeComparer;
                }
            }

            public override bool HasFreqs
            {
                get { return Info.IndexOptions >= IndexOptions.DOCS_AND_FREQS; }
            }

            public override bool HasOffsets
            {
                get { return Info.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS; }
            }

            public override bool HasPositions
            {
                get { return Info.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS; }
            }

            public override bool HasPayloads
            {
                get { return Info.HasPayloads; }
            }
        }

        internal class RAMTerm
        {
            internal readonly string Term;
            internal long TotalTermFreq;
            internal readonly IList<RAMDoc> Docs = new List<RAMDoc>();

            public RAMTerm(string term)
            {
                this.Term = term;
            }

            /// <summary>
            /// Returns approximate RAM bytes used </summary>
            public virtual long RamBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (RAMDoc rDoc in Docs)
                {
                    sizeInBytes += rDoc.RamBytesUsed();
                }
                return sizeInBytes;
            }
        }

        internal class RAMDoc
        {
            internal readonly int DocID;
            internal readonly int[] Positions;
            internal byte[][] Payloads;

            public RAMDoc(int docID, int freq)
            {
                this.DocID = docID;
                Positions = new int[freq];
            }

            /// <summary>
            /// Returns approximate RAM bytes used </summary>
            public virtual long RamBytesUsed()
            {
                long sizeInBytes = 0;
                sizeInBytes += (Positions != null) ? RamUsageEstimator.SizeOf(Positions) : 0;

                if (Payloads != null)
                {
                    foreach (var payload in Payloads)
                    {
                        sizeInBytes += (payload != null) ? RamUsageEstimator.SizeOf(payload) : 0;
                    }
                }
                return sizeInBytes;
            }
        }

        // Classes for writing to the postings state
        private class RAMFieldsConsumer : FieldsConsumer
        {
            internal readonly RAMPostings Postings;
            internal readonly RAMTermsConsumer TermsConsumer = new RAMTermsConsumer();

            public RAMFieldsConsumer(RAMPostings postings)
            {
                this.Postings = postings;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                if (field.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                {
                    throw new System.NotSupportedException("this codec cannot index offsets");
                }
                RAMField ramField = new RAMField(field.Name, field);
                Postings.FieldToTerms[field.Name] = ramField;
                TermsConsumer.Reset(ramField);
                return TermsConsumer;
            }

            public override void Dispose()
            {
                // TODO: finalize stuff
            }
        }

        private class RAMTermsConsumer : TermsConsumer
        {
            internal RAMField Field;
            internal readonly RAMPostingsWriterImpl PostingsWriter = new RAMPostingsWriterImpl();
            internal RAMTerm Current;

            internal virtual void Reset(RAMField field)
            {
                this.Field = field;
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                string term = text.Utf8ToString();
                Current = new RAMTerm(term);
                PostingsWriter.Reset(Current);
                return PostingsWriter;
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                Debug.Assert(stats.DocFreq > 0);
                Debug.Assert(stats.DocFreq == Current.Docs.Count);
                Current.TotalTermFreq = stats.TotalTermFreq;
                Field.TermToDocs[Current.Term] = Current;
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                Field.SumTotalTermFreq_Renamed = sumTotalTermFreq;
                Field.SumDocFreq_Renamed = sumDocFreq;
                Field.DocCount_Renamed = docCount;
            }
        }

        internal class RAMPostingsWriterImpl : PostingsConsumer
        {
            internal RAMTerm Term;
            internal RAMDoc Current;
            internal int PosUpto = 0;

            public virtual void Reset(RAMTerm term)
            {
                this.Term = term;
            }

            public override void StartDoc(int docID, int freq)
            {
                Current = new RAMDoc(docID, freq);
                Term.Docs.Add(Current);
                PosUpto = 0;
            }

            public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
            {
                Debug.Assert(startOffset == -1);
                Debug.Assert(endOffset == -1);
                Current.Positions[PosUpto] = position;
                if (payload != null && payload.Length > 0)
                {
                    if (Current.Payloads == null)
                    {
                        Current.Payloads = new byte[Current.Positions.Length][];
                    }
                    var bytes = Current.Payloads[PosUpto] = new byte[payload.Length];
                    Array.Copy(payload.Bytes, payload.Offset, bytes, 0, payload.Length);
                }
                PosUpto++;
            }

            public override void FinishDoc()
            {
                Debug.Assert(PosUpto == Current.Positions.Length);
            }
        }

        internal class RAMTermsEnum : TermsEnum
        {
            internal IEnumerator<string> It;
            internal string Current;
            internal readonly RAMField RamField;

            public RAMTermsEnum(RAMField field)
            {
                this.RamField = field;
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

            public override BytesRef Next()
            {
                if (It == null)
                {
                    if (Current == null)
                    {
                        It = RamField.TermToDocs.Keys.GetEnumerator();
                    }
                    else
                    {
                        //It = RamField.TermToDocs.tailMap(Current).Keys.GetEnumerator();
                        It = RamField.TermToDocs.Where(kvpair => String.Compare(kvpair.Key, Current) >= 0).ToDictionary(kvpair => kvpair.Key, kvpair => kvpair.Value).Keys.GetEnumerator();
                    }
                }
                if (It.MoveNext())
                {
                    Current = It.Current;
                    return new BytesRef(Current);
                }
                else
                {
                    return null;
                }
            }

            public override SeekStatus SeekCeil(BytesRef term)
            {
                Current = term.Utf8ToString();
                It = null;
                if (RamField.TermToDocs.ContainsKey(Current))
                {
                    return SeekStatus.FOUND;
                }
                else
                {
                    if (Current.CompareTo(RamField.TermToDocs.Last().Key) > 0)
                    {
                        return SeekStatus.END;
                    }
                    else
                    {
                        return SeekStatus.NOT_FOUND;
                    }
                }
            }

            public override void SeekExact(long ord)
            {
                throw new System.NotSupportedException();
            }

            public override long Ord
            {
                get { throw new System.NotSupportedException(); }
            }

            public override BytesRef Term
            {
                get
                {
                    // TODO: reuse BytesRef
                    return new BytesRef(Current);
                }
            }

            public override int DocFreq
            {
                get { return RamField.TermToDocs[Current].Docs.Count; }
            }

            public override long TotalTermFreq
            {
                get { return RamField.TermToDocs[Current].TotalTermFreq; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                return new RAMDocsEnum(RamField.TermToDocs[Current], liveDocs);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                return new RAMDocsAndPositionsEnum(RamField.TermToDocs[Current], liveDocs);
            }
        }

        private class RAMDocsEnum : DocsEnum
        {
            private readonly RAMTerm RamTerm;
            private readonly IBits LiveDocs;
            private RAMDoc Current;
            private int Upto = -1;
            private int PosUpto = 0;

            public RAMDocsEnum(RAMTerm ramTerm, IBits liveDocs)
            {
                this.RamTerm = ramTerm;
                this.LiveDocs = liveDocs;
            }

            public override int Advance(int targetDocID)
            {
                return SlowAdvance(targetDocID);
            }

            // TODO: override bulk read, for better perf
            public override int NextDoc()
            {
                while (true)
                {
                    Upto++;
                    if (Upto < RamTerm.Docs.Count)
                    {
                        Current = RamTerm.Docs[Upto];
                        if (LiveDocs == null || LiveDocs.Get(Current.DocID))
                        {
                            PosUpto = 0;
                            return Current.DocID;
                        }
                    }
                    else
                    {
                        return NO_MORE_DOCS;
                    }
                }
            }

            public override int Freq
            {
                get { return Current.Positions.Length; }
            }

            public override int DocID
            {
                get { return Current.DocID; }
            }

            public override long Cost()
            {
                return RamTerm.Docs.Count;
            }
        }

        private class RAMDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly RAMTerm RamTerm;
            private readonly IBits LiveDocs;
            private RAMDoc Current;
            private int Upto = -1;
            private int PosUpto = 0;

            public RAMDocsAndPositionsEnum(RAMTerm ramTerm, IBits liveDocs)
            {
                this.RamTerm = ramTerm;
                this.LiveDocs = liveDocs;
            }

            public override int Advance(int targetDocID)
            {
                return SlowAdvance(targetDocID);
            }

            // TODO: override bulk read, for better perf
            public override int NextDoc()
            {
                while (true)
                {
                    Upto++;
                    if (Upto < RamTerm.Docs.Count)
                    {
                        Current = RamTerm.Docs[Upto];
                        if (LiveDocs == null || LiveDocs.Get(Current.DocID))
                        {
                            PosUpto = 0;
                            return Current.DocID;
                        }
                    }
                    else
                    {
                        return NO_MORE_DOCS;
                    }
                }
            }

            public override int Freq
            {
                get { return Current.Positions.Length; }
            }

            public override int DocID
            {
                get { return Current.DocID; }
            }

            public override int NextPosition()
            {
                return Current.Positions[PosUpto++];
            }

            public override int StartOffset
            {
                get { return -1; }
            }

            public override int EndOffset
            {
                get { return -1; }
            }

            public override BytesRef Payload
            {
                get
                {
                    if (Current.Payloads != null && Current.Payloads[PosUpto - 1] != null)
                    {
                        return new BytesRef(Current.Payloads[PosUpto - 1]);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public override long Cost()
            {
                return RamTerm.Docs.Count;
            }
        }

        // Holds all indexes created, keyed by the ID assigned in fieldsConsumer
        private readonly IDictionary<int?, RAMPostings> State = new Dictionary<int?, RAMPostings>();

        private readonly AtomicLong NextID = new AtomicLong();

        private readonly string RAM_ONLY_NAME = "RAMOnly";
        private const int VERSION_START = 0;
        private const int VERSION_LATEST = VERSION_START;

        private const string ID_EXTENSION = "id";

        public override FieldsConsumer FieldsConsumer(SegmentWriteState writeState)
        {
            int id = (int)NextID.IncrementAndGet();

            // TODO -- ok to do this up front instead of
            // on close....?  should be ok?
            // Write our ID:
            string idFileName = IndexFileNames.SegmentFileName(writeState.SegmentInfo.Name, writeState.SegmentSuffix, ID_EXTENSION);
            IndexOutput @out = writeState.Directory.CreateOutput(idFileName, writeState.Context);
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(@out, RAM_ONLY_NAME, VERSION_LATEST);
                @out.WriteVInt(id);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(@out);
                }
                else
                {
                    IOUtils.Close(@out);
                }
            }

            RAMPostings postings = new RAMPostings();
            RAMFieldsConsumer consumer = new RAMFieldsConsumer(postings);

            lock (State)
            {
                State[id] = postings;
            }
            return consumer;
        }

        public override FieldsProducer FieldsProducer(SegmentReadState readState)
        {
            // Load our ID:
            string idFileName = IndexFileNames.SegmentFileName(readState.SegmentInfo.Name, readState.SegmentSuffix, ID_EXTENSION);
            IndexInput @in = readState.Directory.OpenInput(idFileName, readState.Context);
            bool success = false;
            int id;
            try
            {
                CodecUtil.CheckHeader(@in, RAM_ONLY_NAME, VERSION_START, VERSION_LATEST);
                id = @in.ReadVInt();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(@in);
                }
                else
                {
                    IOUtils.Close(@in);
                }
            }

            lock (State)
            {
                return State[id];
            }
        }
    }
}