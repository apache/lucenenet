using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Codecs.RAMOnly
{
    
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
    /// token (header + single int) to identify which "slot" the
    /// index is using in RAM dictionary.
    /// <para/>
    /// NOTE: this codec sorts terms by reverse-unicode-order!
    /// </summary>
    [PostingsFormatName("RAMOnly")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class RAMOnlyPostingsFormat : PostingsFormat
    {
        // For fun, test that we can override how terms are
        // sorted, and basic things still work -- this comparer
        // sorts in reversed unicode code point order:
        private static readonly IComparer<BytesRef> reverseUnicodeComparer = new ComparerAnonymousInnerClassHelper();

#pragma warning disable 659 // LUCENENET: Overrides Equals but not GetHashCode
        private class ComparerAnonymousInnerClassHelper : IComparer<BytesRef>
#pragma warning restore 659
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
            : base()
        {
        }

        // Postings state:
        internal class RAMPostings : FieldsProducer
        {
            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            internal readonly IDictionary<string, RAMField> fieldToTerms = new SortedDictionary<string, RAMField>(StringComparer.Ordinal);

            public override Terms GetTerms(string field)
            {
                RAMField result;
                fieldToTerms.TryGetValue(field, out result);
                return result;
            }

            public override int Count
            {
                get { return fieldToTerms.Count; }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return fieldToTerms.Keys.GetEnumerator();
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override long RamBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (RAMField field in fieldToTerms.Values)
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
            internal readonly string field;

            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            internal readonly SortedDictionary<string, RAMTerm> termToDocs = new SortedDictionary<string, RAMTerm>(StringComparer.Ordinal);
            internal long sumTotalTermFreq;
            internal long sumDocFreq;
            internal int docCount;
            internal readonly FieldInfo info;

            internal RAMField(string field, FieldInfo info)
            {
                this.field = field;
                this.info = info;
            }

            /// <summary>
            /// Returns approximate RAM bytes used </summary>
            public virtual long RamBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (RAMTerm term in termToDocs.Values)
                {
                    sizeInBytes += term.RamBytesUsed();
                }
                return sizeInBytes;
            }

            public override long Count
            {
                get { return termToDocs.Count; }
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return sumTotalTermFreq;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    return sumDocFreq;
                }
            }

            public override int DocCount
            {
                get
                {
                    return docCount;
                }
            }

            public override TermsEnum GetIterator(TermsEnum reuse)
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
                get { return info.IndexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS) >= 0; }
            }

            public override bool HasOffsets
            {
                get { return info.IndexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0; }
            }

            public override bool HasPositions
            {
                get { return info.IndexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0; }
            }

            public override bool HasPayloads
            {
                get { return info.HasPayloads; }
            }
        }

        internal class RAMTerm
        {
            internal readonly string term;
            internal long totalTermFreq;
            internal readonly IList<RAMDoc> docs = new List<RAMDoc>();

            public RAMTerm(string term)
            {
                this.term = term;
            }

            /// <summary>
            /// Returns approximate RAM bytes used </summary>
            public virtual long RamBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (RAMDoc rDoc in docs)
                {
                    sizeInBytes += rDoc.RamBytesUsed();
                }
                return sizeInBytes;
            }
        }

        internal class RAMDoc
        {
            internal readonly int docID;
            internal readonly int[] positions;
            internal byte[][] payloads;

            public RAMDoc(int docID, int freq)
            {
                this.docID = docID;
                positions = new int[freq];
            }

            /// <summary>
            /// Returns approximate RAM bytes used </summary>
            public virtual long RamBytesUsed()
            {
                long sizeInBytes = 0;
                sizeInBytes += (positions != null) ? RamUsageEstimator.SizeOf(positions) : 0;

                if (payloads != null)
                {
                    foreach (var payload in payloads)
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
            internal readonly RAMPostings postings;
            internal readonly RAMTermsConsumer termsConsumer = new RAMTermsConsumer();

            public RAMFieldsConsumer(RAMPostings postings)
            {
                this.postings = postings;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                if (field.IndexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0)
                {
                    throw new System.NotSupportedException("this codec cannot index offsets");
                }
                RAMField ramField = new RAMField(field.Name, field);
                postings.fieldToTerms[field.Name] = ramField;
                termsConsumer.Reset(ramField);
                return termsConsumer;
            }

            protected override void Dispose(bool disposing)
            {
                // TODO: finalize stuff
            }
        }

        private class RAMTermsConsumer : TermsConsumer
        {
            internal RAMField field;
            internal readonly RAMPostingsWriterImpl postingsWriter = new RAMPostingsWriterImpl();
            internal RAMTerm current;

            internal virtual void Reset(RAMField field)
            {
                this.field = field;
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                string term = text.Utf8ToString();
                current = new RAMTerm(term);
                postingsWriter.Reset(current);
                return postingsWriter;
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
                Debug.Assert(stats.DocFreq == current.docs.Count);
                current.totalTermFreq = stats.TotalTermFreq;
                field.termToDocs[current.term] = current;
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                field.sumTotalTermFreq = sumTotalTermFreq;
                field.sumDocFreq = sumDocFreq;
                field.docCount = docCount;
            }
        }

        internal class RAMPostingsWriterImpl : PostingsConsumer
        {
            internal RAMTerm term;
            internal RAMDoc current;
            internal int posUpto = 0;

            public virtual void Reset(RAMTerm term)
            {
                this.term = term;
            }

            public override void StartDoc(int docID, int freq)
            {
                current = new RAMDoc(docID, freq);
                term.docs.Add(current);
                posUpto = 0;
            }

            public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
            {
                Debug.Assert(startOffset == -1);
                Debug.Assert(endOffset == -1);
                current.positions[posUpto] = position;
                if (payload != null && payload.Length > 0)
                {
                    if (current.payloads == null)
                    {
                        current.payloads = new byte[current.positions.Length][];
                    }
                    var bytes = current.payloads[posUpto] = new byte[payload.Length];
                    Array.Copy(payload.Bytes, payload.Offset, bytes, 0, payload.Length);
                }
                posUpto++;
            }

            public override void FinishDoc()
            {
                Debug.Assert(posUpto == current.positions.Length);
            }
        }

        internal class RAMTermsEnum : TermsEnum
        {
            internal IEnumerator<string> it;
            internal string current;
            internal readonly RAMField ramField;

            public RAMTermsEnum(RAMField field)
            {
                this.ramField = field;
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
                if (it == null)
                {
                    if (current == null)
                    {
                        it = ramField.termToDocs.Keys.GetEnumerator();
                    }
                    else
                    {
                        //It = RamField.TermToDocs.tailMap(Current).Keys.GetEnumerator();
                        it = ramField.termToDocs.Where(kvpair => String.CompareOrdinal(kvpair.Key, current) >= 0).ToDictionary(kvpair => kvpair.Key, kvpair => kvpair.Value).Keys.GetEnumerator();
                    }
                }
                if (it.MoveNext())
                {
                    current = it.Current;
                    return new BytesRef(current);
                }
                else
                {
                    return null;
                }
            }

            public override SeekStatus SeekCeil(BytesRef term)
            {
                current = term.Utf8ToString();
                it = null;
                if (ramField.termToDocs.ContainsKey(current))
                {
                    return SeekStatus.FOUND;
                }
                else
                {
                    if (current.CompareToOrdinal(ramField.termToDocs.Last().Key) > 0)
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
                    return new BytesRef(current);
                }
            }

            public override int DocFreq
            {
                get { return ramField.termToDocs[current].docs.Count; }
            }

            public override long TotalTermFreq
            {
                get { return ramField.termToDocs[current].totalTermFreq; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                return new RAMDocsEnum(ramField.termToDocs[current], liveDocs);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                return new RAMDocsAndPositionsEnum(ramField.termToDocs[current], liveDocs);
            }
        }

        private class RAMDocsEnum : DocsEnum
        {
            private readonly RAMTerm ramTerm;
            private readonly IBits liveDocs;
            private RAMDoc current;
            private int upto = -1;
#pragma warning disable 414
            private int posUpto = 0; // LUCENENET NOTE: Not used
#pragma warning restore 414

            public RAMDocsEnum(RAMTerm ramTerm, IBits liveDocs)
            {
                this.ramTerm = ramTerm;
                this.liveDocs = liveDocs;
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
                    upto++;
                    if (upto < ramTerm.docs.Count)
                    {
                        current = ramTerm.docs[upto];
                        if (liveDocs == null || liveDocs.Get(current.docID))
                        {
                            posUpto = 0;
                            return current.docID;
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
                get { return current.positions.Length; }
            }

            public override int DocID
            {
                get { return current.docID; }
            }

            public override long GetCost()
            {
                return ramTerm.docs.Count;
            }
        }

        private class RAMDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly RAMTerm ramTerm;
            private readonly IBits liveDocs;
            private RAMDoc current;
            private int upto = -1;
            private int posUpto = 0;

            public RAMDocsAndPositionsEnum(RAMTerm ramTerm, IBits liveDocs)
            {
                this.ramTerm = ramTerm;
                this.liveDocs = liveDocs;
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
                    upto++;
                    if (upto < ramTerm.docs.Count)
                    {
                        current = ramTerm.docs[upto];
                        if (liveDocs == null || liveDocs.Get(current.docID))
                        {
                            posUpto = 0;
                            return current.docID;
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
                get { return current.positions.Length; }
            }

            public override int DocID
            {
                get { return current.docID; }
            }

            public override int NextPosition()
            {
                return current.positions[posUpto++];
            }

            public override int StartOffset
            {
                get { return -1; }
            }

            public override int EndOffset
            {
                get { return -1; }
            }

            public override BytesRef GetPayload()
            {
                if (current.payloads != null && current.payloads[posUpto - 1] != null)
                {
                    return new BytesRef(current.payloads[posUpto - 1]);
                }
                else
                {
                    return null;
                }
            }

            public override long GetCost()
            {
                return ramTerm.docs.Count;
            }
        }

        // Holds all indexes created, keyed by the ID assigned in fieldsConsumer
        private readonly IDictionary<int?, RAMPostings> state = new Dictionary<int?, RAMPostings>();

        private readonly AtomicInt64 nextID = new AtomicInt64();

        private readonly string RAM_ONLY_NAME = "RAMOnly";
        private const int VERSION_START = 0;
        private const int VERSION_LATEST = VERSION_START;

        private const string ID_EXTENSION = "id";

        public override FieldsConsumer FieldsConsumer(SegmentWriteState writeState)
        {
            int id = (int)nextID.GetAndIncrement();

            // TODO -- ok to do this up front instead of
            // on close....?  should be ok?
            // Write our ID:
            string idFileName = IndexFileNames.SegmentFileName(writeState.SegmentInfo.Name, writeState.SegmentSuffix, ID_EXTENSION);
            IndexOutput @out = writeState.Directory.CreateOutput(idFileName, writeState.Context);
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(@out, RAM_ONLY_NAME, VERSION_LATEST);
                @out.WriteVInt32(id);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(@out);
                }
                else
                {
                    IOUtils.Dispose(@out);
                }
            }

            RAMPostings postings = new RAMPostings();
            RAMFieldsConsumer consumer = new RAMFieldsConsumer(postings);

            lock (state)
            {
                state[id] = postings;
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
                id = @in.ReadVInt32();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(@in);
                }
                else
                {
                    IOUtils.Dispose(@in);
                }
            }

            lock (state)
            {
                return state[id];
            }
        }
    }
}