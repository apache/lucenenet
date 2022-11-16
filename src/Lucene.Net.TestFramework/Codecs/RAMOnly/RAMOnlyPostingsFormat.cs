using J2N.Text;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.RAMOnly
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
        private static readonly IComparer<BytesRef> reverseUnicodeComparer = new ComparerAnonymousClass();

#pragma warning disable 659 // LUCENENET: Overrides Equals but not GetHashCode
        private sealed class ComparerAnonymousClass : IComparer<BytesRef>
#pragma warning restore 659
        {
            public ComparerAnonymousClass()
            { }

            public int Compare(BytesRef t1, BytesRef t2)
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
        { }

        // Postings state:
        internal class RAMPostings : FieldsProducer
        {
            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            internal readonly IDictionary<string, RAMField> fieldToTerms = new JCG.SortedDictionary<string, RAMField>(StringComparer.Ordinal);

            public override Terms GetTerms(string field)
            {
                fieldToTerms.TryGetValue(field, out RAMField result);
                return result;
            }

            public override int Count => fieldToTerms.Count;

            public override IEnumerator<string> GetEnumerator()
                => fieldToTerms.Keys.GetEnumerator();

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
            internal readonly JCG.SortedDictionary<string, RAMTerm> termToDocs = new JCG.SortedDictionary<string, RAMTerm>(StringComparer.Ordinal);
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

            public override long Count => termToDocs.Count;

            public override long SumTotalTermFreq => sumTotalTermFreq;

            public override long SumDocFreq => sumDocFreq;

            public override int DocCount => docCount;

            public override TermsEnum GetEnumerator()
            {
                return new RAMTermsEnum(this);
            }

            public override IComparer<BytesRef> Comparer => reverseUnicodeComparer;

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            public override bool HasFreqs
                => IndexOptionsComparer.Default.Compare(info.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;

            public override bool HasOffsets
                => IndexOptionsComparer.Default.Compare(info.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

            public override bool HasPositions
                => IndexOptionsComparer.Default.Compare(info.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;

            public override bool HasPayloads => info.HasPayloads;
        }

        internal class RAMTerm
        {
            internal readonly string term;
            internal long totalTermFreq;
            internal readonly IList<RAMDoc> docs = new JCG.List<RAMDoc>();

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
            private readonly RAMPostings postings;
            private readonly RAMTermsConsumer termsConsumer = new RAMTermsConsumer();

            public RAMFieldsConsumer(RAMPostings postings)
            {
                this.postings = postings;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                if (IndexOptionsComparer.Default.Compare(field.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0)
                {
                    throw UnsupportedOperationException.Create("this codec cannot index offsets");
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
            private RAMField field;
            private readonly RAMPostingsWriterImpl postingsWriter = new RAMPostingsWriterImpl();
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
                => BytesRef.UTF8SortedAsUnicodeComparer;

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(stats.DocFreq > 0);
                if (Debugging.AssertsEnabled) Debugging.Assert(stats.DocFreq == current.docs.Count);
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
            private RAMTerm term;
            private RAMDoc current;
            private int posUpto = 0;

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
                if (Debugging.AssertsEnabled) Debugging.Assert(startOffset == -1);
                if (Debugging.AssertsEnabled) Debugging.Assert(endOffset == -1);
                current.positions[posUpto] = position;
                if (payload != null && payload.Length > 0)
                {
                    if (current.payloads is null)
                    {
                        current.payloads = new byte[current.positions.Length][];
                    }
                    var bytes = current.payloads[posUpto] = new byte[payload.Length];
                    Arrays.Copy(payload.Bytes, payload.Offset, bytes, 0, payload.Length);
                }
                posUpto++;
            }

            public override void FinishDoc()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(posUpto == current.positions.Length);
            }
        }

        internal class RAMTermsEnum : TermsEnum
        {
            internal IEnumerator<string> it;
            internal string current;
            private readonly RAMField ramField;

            public RAMTermsEnum(RAMField field)
            {
                this.ramField = field;
            }

            public override IComparer<BytesRef> Comparer
                => BytesRef.UTF8SortedAsUnicodeComparer;

            public override bool MoveNext()
            {
                EnsureEnumeratorInitialized();
                if (it.MoveNext())
                {
                    current = it.Current;
                    return current != null;
                }
                else
                {
                    return false;
                }
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (MoveNext())
                    return new BytesRef(current);
                return null;
            }

            private void EnsureEnumeratorInitialized() // LUCENENET specific - factored out initialization step
            {
                if (it is null)
                {
                    if (current is null)
                    {
                        it = ramField.termToDocs.Keys.GetEnumerator();
                    }
                    else
                    {
                        //It = RamField.TermToDocs.tailMap(Current).Keys.GetEnumerator();
                        it = ramField.termToDocs.Where(kvpair => string.CompareOrdinal(kvpair.Key, current) >= 0).Select(pair => pair.Key).GetEnumerator();
                    }
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
                => throw UnsupportedOperationException.Create();

            public override long Ord
                => throw UnsupportedOperationException.Create();

            // TODO: reuse BytesRef
            public override BytesRef Term => current is null ? null : new BytesRef(current);

            public override int DocFreq
                => ramField.termToDocs[current].docs.Count;

            public override long TotalTermFreq
                => ramField.termToDocs[current].totalTermFreq;

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
            //private int posUpto = 0; // LUCENENET: Never read


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
                        if (liveDocs is null || liveDocs.Get(current.docID))
                        {
                            //posUpto = 0; // LUCENENET: Never read
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
                => current.positions.Length;

            public override int DocID
                => current.docID;

            public override long GetCost()
                => ramTerm.docs.Count;
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
                        if (liveDocs is null || liveDocs.Get(current.docID))
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
                => current.positions.Length;

            public override int DocID
                => current.docID;

            public override int NextPosition()
                => current.positions[posUpto++];

            public override int StartOffset => -1;

            public override int EndOffset => -1;

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
                => ramTerm.docs.Count;
        }

        // Holds all indexes created, keyed by the ID assigned in fieldsConsumer
        private readonly IDictionary<int, RAMPostings> state = new Dictionary<int, RAMPostings>();

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

            UninterruptableMonitor.Enter(state);
            try
            {
                state[id] = postings;
            }
            finally
            {
                UninterruptableMonitor.Exit(state);
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

            UninterruptableMonitor.Enter(state);
            try
            {
                return state.TryGetValue(id, out RAMPostings value) ? value : null;
            }
            finally
            {
                UninterruptableMonitor.Exit(state);
            }
        }
    }
}