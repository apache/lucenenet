using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.Memory
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

    using ArrayUtil = Util.ArrayUtil;
    using BytesRef = Util.BytesRef;
    using CompiledAutomaton = Util.Automaton.CompiledAutomaton;
    using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
    using DocsEnum = Index.DocsEnum;
    using FieldInfo = Index.FieldInfo;
    using Fields = Index.Fields;
    using IBits = Util.IBits;
    using IndexOptions = Index.IndexOptions;
    using IOContext = Store.IOContext;
    using OrdTermState = Index.OrdTermState;
    using RAMOutputStream = Store.RAMOutputStream;
    using RamUsageEstimator = Util.RamUsageEstimator;
    using RunAutomaton = Util.Automaton.RunAutomaton;
    using SegmentReadState = Index.SegmentReadState;
    using SegmentWriteState = Index.SegmentWriteState;
    using Terms = Index.Terms;
    using TermsEnum = Index.TermsEnum;
    using TermState = Index.TermState;
    using Transition = Util.Automaton.Transition;

    // TODO:
    //   - build depth-N prefix hash?
    //   - or: longer dense skip lists than just next byte?

    /// <summary>
    /// Wraps <see cref="Lucene41.Lucene41PostingsFormat"/> format for on-disk
    /// storage, but then at read time loads and stores all
    /// terms &amp; postings directly in RAM as byte[], int[].
    ///
    /// <para><b>WARNING</b>: This is
    /// exceptionally RAM intensive: it makes no effort to
    /// compress the postings data, storing terms as separate
    /// byte[] and postings as separate int[], but as a result it
    /// gives substantial increase in search performance.
    ///
    /// </para>
    /// <para>This postings format supports <see cref="TermsEnum.Ord"/>
    /// and <see cref="TermsEnum.SeekExact(long)"/>.
    ///
    /// </para>
    /// <para>Because this holds all term bytes as a single
    /// byte[], you cannot have more than 2.1GB worth of term
    /// bytes in a single segment.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    [PostingsFormatName("Direct")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class DirectPostingsFormat : PostingsFormat
    {
        private readonly int _minSkipCount;
        private readonly int _lowFreqCutoff;

        private const int DEFAULT_MIN_SKIP_COUNT = 8;
        private const int DEFAULT_LOW_FREQ_CUTOFF = 32;

        // TODO: allow passing/wrapping arbitrary postings format?

        public DirectPostingsFormat()
            : this(DEFAULT_MIN_SKIP_COUNT, DEFAULT_LOW_FREQ_CUTOFF)
        {
        }

        /// <summary>
        /// <paramref name="minSkipCount"/> is how many terms in a row must have the
        /// same prefix before we put a skip pointer down.  Terms
        /// with docFreq less than or equal <paramref name="lowFreqCutoff"/> will use a single int[]
        /// to hold all docs, freqs, position and offsets; terms
        /// with higher docFreq will use separate arrays.
        /// </summary>
        public DirectPostingsFormat(int minSkipCount, int lowFreqCutoff)
            : base()
        {
            _minSkipCount = minSkipCount;
            _lowFreqCutoff = lowFreqCutoff;
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            return ForName("Lucene41").FieldsConsumer(state);
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            var postings = ForName("Lucene41").FieldsProducer(state);
            if (state.Context.Context != IOContext.UsageContext.MERGE)
            {
                FieldsProducer loadedPostings;
                try
                {
                    postings.CheckIntegrity();
                    loadedPostings = new DirectFields(state, postings, _minSkipCount, _lowFreqCutoff);
                }
                finally
                {
                    postings.Dispose();
                }
                return loadedPostings;
            }
            else
            {
                // Don't load postings for merge:
                return postings;
            }
        }

        private sealed class DirectFields : FieldsProducer
        {
            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            private readonly IDictionary<string, DirectField> fields = new JCG.SortedDictionary<string, DirectField>(StringComparer.Ordinal);

            public DirectFields(SegmentReadState state, Fields fields, int minSkipCount, int lowFreqCutoff)
            {
                foreach (string field in fields)
                {
                    this.fields[field] = new DirectField(state, field, fields.GetTerms(field), minSkipCount, lowFreqCutoff);
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return fields.Keys.GetEnumerator(); // LUCENENET NOTE: enumerators are not writable in .NET
            }

            public override Terms GetTerms(string field)
            {
                fields.TryGetValue(field, out DirectField result);
                return result;
            }

            public override int Count => fields.Count;

            [Obsolete("iterate fields and add their Count instead.")]
            public override long UniqueTermCount
            {
                get
                {
                    long numTerms = 0;
                    foreach (DirectField field in fields.Values)
                    {
                        numTerms += field.terms.Length;
                    }
                    return numTerms;
                }
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override long RamBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (KeyValuePair<string, DirectField> entry in fields)
                {
                    sizeInBytes += entry.Key.Length* RamUsageEstimator.NUM_BYTES_CHAR;
                    sizeInBytes += entry.Value.RamBytesUsed();
                }
                return sizeInBytes;
            }

            public override void CheckIntegrity()
            {
                // if we read entirely into ram, we already validated.
                // otherwise returned the raw postings reader
            }
        }

        private sealed class DirectField : Terms
        {
            internal abstract class TermAndSkip
            {
                [WritableArray]
                [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
                public int[] Skips
                {
                    get => skips;
                    set => skips = value;
                }
                private int[] skips;

                /// <summary>
                /// Returns the approximate number of RAM bytes used. </summary>
                public abstract long RamBytesUsed();
            }

            private sealed class LowFreqTerm : TermAndSkip
            {
                [WritableArray]
                [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
                public int[] Postings => postings;

                private readonly int[] postings;

                [WritableArray]
                [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
                public byte[] Payloads => payloads;

                private readonly byte[] payloads;

                public int DocFreq => docFreq;
                private readonly int docFreq;

                public int TotalTermFreq => totalTermFreq;
                private readonly int totalTermFreq;

                public LowFreqTerm(int[] postings, byte[] payloads, int docFreq, int totalTermFreq)
                {
                    this.postings = postings;
                    this.payloads = payloads;
                    this.docFreq = docFreq;
                    this.totalTermFreq = totalTermFreq;
                }

                public override long RamBytesUsed()
                {
                    return ((postings != null) ? RamUsageEstimator.SizeOf(postings) : 0) +
                           ((payloads != null) ? RamUsageEstimator.SizeOf(payloads) : 0);
                }
            }

            // TODO: maybe specialize into prx/no-prx/no-frq cases?
            private sealed class HighFreqTerm : TermAndSkip
            {
                public long TotalTermFreq => totalTermFreq;
                private readonly long totalTermFreq;

                [WritableArray]
                [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
                public int[] DocIDs => docIDs;

                private readonly int[] docIDs;

                [WritableArray]
                [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
                public int[] Freqs => freqs;

                private readonly int[] freqs;

                [WritableArray]
                [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
                public int[][] Positions => positions;

                private readonly int[][] positions;

                [WritableArray]
                [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
                public byte[][][] Payloads => payloads;

                private readonly byte[][][] payloads;

                public HighFreqTerm(int[] docIDs, int[] freqs, int[][] positions, byte[][][] payloads,
                    long totalTermFreq)
                {
                    this.docIDs = docIDs;
                    this.freqs = freqs;
                    this.positions = positions;
                    this.payloads = payloads;
                    this.totalTermFreq = totalTermFreq;
                }

                public override long RamBytesUsed()
                {
                    long sizeInBytes = 0;
                    sizeInBytes += (docIDs != null) ? RamUsageEstimator.SizeOf(docIDs) : 0;
                    sizeInBytes += (freqs != null) ? RamUsageEstimator.SizeOf(freqs) : 0;

                    if (positions != null)
                    {
                        foreach (int[] position in positions)
                        {
                            sizeInBytes += (position != null) ? RamUsageEstimator.SizeOf(position) : 0;
                        }
                    }

                    if (payloads != null)
                    {
                        foreach (var payload in payloads)
                        {
                            if (payload != null)
                            {
                                foreach (var pload in payload)
                                {
                                    sizeInBytes += (pload != null) ? RamUsageEstimator.SizeOf(pload) : 0;
                                }
                            }
                        }
                    }

                    return sizeInBytes;
                }
            }

            private readonly byte[] termBytes;
            private readonly int[] termOffsets;

            private readonly int[] skips;
            private readonly int[] skipOffsets;

            internal readonly TermAndSkip[] terms;
            private readonly bool hasFreq;
            private readonly bool hasPos;
            private readonly bool hasOffsets;
            private readonly bool hasPayloads;
            private readonly long sumTotalTermFreq;
            private readonly int docCount;
            private readonly long sumDocFreq;
            private int skipCount;

            // TODO: maybe make a separate builder?  These are only
            // used during load:
            private readonly int count;
            private int[] sameCounts = new int[10];
            private readonly int minSkipCount;

            /// <summary>
            /// NOTE: This was IntArrayWriter in Lucene.
            /// </summary>
            private sealed class Int32ArrayWriter
            {
                private int[] ints = new int[10];
                private int upto;

                public void Add(int value)
                {
                    if (ints.Length == upto)
                    {
                        ints = ArrayUtil.Grow(ints);
                    }
                    ints[upto++] = value;
                }

                public int[] Get()
                {
                    var arr = new int[upto];
                    Arrays.Copy(ints, 0, arr, 0, upto);
                    upto = 0;
                    return arr;
                }
            }

            public DirectField(SegmentReadState state, string field, Terms termsIn, int minSkipCount, int lowFreqCutoff)
            {
                FieldInfo fieldInfo = state.FieldInfos.FieldInfo(field);

                sumTotalTermFreq = termsIn.SumTotalTermFreq;
                sumDocFreq = termsIn.SumDocFreq;
                docCount = termsIn.DocCount;

                int numTerms = (int) termsIn.Count;
                if (numTerms == -1)
                {
                    throw new ArgumentException("codec does not provide Terms.Count");
                }
                terms = new TermAndSkip[numTerms];
                termOffsets = new int[1 + numTerms];

                byte[] termBytes = new byte[1024];

                this.minSkipCount = minSkipCount;

                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                hasFreq = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_ONLY) > 0;
                hasPos = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS) > 0;
                hasOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) > 0;
                hasPayloads = fieldInfo.HasPayloads;

                BytesRef term;
                DocsEnum docsEnum = null;
                DocsAndPositionsEnum docsAndPositionsEnum = null;
                TermsEnum termsEnum = termsIn.GetEnumerator();
                int termOffset = 0;

                Int32ArrayWriter scratch = new Int32ArrayWriter();

                // Used for payloads, if any:
                RAMOutputStream ros = new RAMOutputStream();

                // if (DEBUG) {
                //   System.out.println("\nLOAD terms seg=" + state.segmentInfo.name + " field=" + field + " hasOffsets=" + hasOffsets + " hasFreq=" + hasFreq + " hasPos=" + hasPos + " hasPayloads=" + hasPayloads);
                // }

                while (termsEnum.MoveNext())
                {
                    term = termsEnum.Term;
                    int docFreq = termsEnum.DocFreq;
                    long totalTermFreq = termsEnum.TotalTermFreq;

                    // if (DEBUG) {
                    //   System.out.println("  term=" + term.utf8ToString());
                    // }

                    termOffsets[count] = termOffset;

                    if (termBytes.Length < (termOffset + term.Length))
                    {
                        termBytes = ArrayUtil.Grow(termBytes, termOffset + term.Length);
                    }
                    Arrays.Copy(term.Bytes, term.Offset, termBytes, termOffset, term.Length);
                    termOffset += term.Length;
                    termOffsets[count + 1] = termOffset;

                    if (hasPos)
                    {
                        docsAndPositionsEnum = termsEnum.DocsAndPositions(null, docsAndPositionsEnum);
                    }
                    else
                    {
                        docsEnum = termsEnum.Docs(null, docsEnum);
                    }

                    TermAndSkip ent;

                    DocsEnum docsEnum2;
                    docsEnum2 = hasPos ? docsAndPositionsEnum : docsEnum;

                    int docID;

                    if (docFreq <= lowFreqCutoff)
                    {

                        ros.Reset();

                        // Pack postings for low-freq terms into a single int[]:
                        while ((docID = docsEnum2.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                        {
                            scratch.Add(docID);
                            if (hasFreq)
                            {
                                int freq = docsEnum2.Freq;
                                scratch.Add(freq);
                                if (hasPos)
                                {
                                    for (int pos = 0; pos < freq; pos++)
                                    {
                                        scratch.Add(docsAndPositionsEnum.NextPosition());
                                        if (hasOffsets)
                                        {
                                            scratch.Add(docsAndPositionsEnum.StartOffset);
                                            scratch.Add(docsAndPositionsEnum.EndOffset);
                                        }
                                        if (hasPayloads)
                                        {
                                            BytesRef payload = docsAndPositionsEnum.GetPayload();
                                            if (payload != null)
                                            {
                                                scratch.Add(payload.Length);
                                                ros.WriteBytes(payload.Bytes, payload.Offset, payload.Length);
                                            }
                                            else
                                            {
                                                scratch.Add(0);
                                            }
                                        }
                                    }
                                }
                            }
                        }


                        byte[] payloads;
                        if (hasPayloads)
                        {
                            ros.Flush();
                            payloads = new byte[(int) ros.Length];
                            ros.WriteTo(payloads, 0);
                        }
                        else
                        {
                            payloads = null;
                        }

                        int[] postings = scratch.Get();

                        ent = new LowFreqTerm(postings, payloads, docFreq, (int) totalTermFreq);
                    }
                    else
                    {
                        var docs = new int[docFreq];
                        int[] freqs;
                        int[][] positions;
                        byte[][][] payloads;

                        if (hasFreq)
                        {
                            freqs = new int[docFreq];
                            if (hasPos)
                            {
                                positions = new int[docFreq][];
                                if (hasPayloads)
                                {
                                    payloads = new byte[docFreq][][];
                                }
                                else
                                {
                                    payloads = null;
                                }
                            }
                            else
                            {
                                positions = null;
                                payloads = null;
                            }
                        }
                        else
                        {
                            freqs = null;
                            positions = null;
                            payloads = null;
                        }

                        // Use separate int[] for the postings for high-freq
                        // terms:
                        int upto = 0;
                        while ((docID = docsEnum2.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                        {
                            docs[upto] = docID;
                            if (hasFreq)
                            {
                                int freq = docsEnum2.Freq;
                                freqs[upto] = freq;
                                if (hasPos)
                                {
                                    int mult;
                                    if (hasOffsets)
                                    {
                                        mult = 3;
                                    }
                                    else
                                    {
                                        mult = 1;
                                    }
                                    if (hasPayloads)
                                    {
                                        payloads[upto] = new byte[freq][];
                                    }
                                    positions[upto] = new int[mult*freq];
                                    int posUpto = 0;
                                    for (int pos = 0; pos < freq; pos++)
                                    {
                                        positions[upto][posUpto] = docsAndPositionsEnum.NextPosition();
                                        if (hasPayloads)
                                        {
                                            BytesRef payload = docsAndPositionsEnum.GetPayload();
                                            if (payload != null)
                                            {
                                                var payloadBytes = new byte[payload.Length];
                                                Arrays.Copy(payload.Bytes, payload.Offset, payloadBytes, 0,
                                                    payload.Length);
                                                payloads[upto][pos] = payloadBytes;
                                            }
                                        }
                                        posUpto++;
                                        if (hasOffsets)
                                        {
                                            positions[upto][posUpto++] = docsAndPositionsEnum.StartOffset;
                                            positions[upto][posUpto++] = docsAndPositionsEnum.EndOffset;
                                        }
                                    }
                                }
                            }

                            upto++;
                        }
                        if (Debugging.AssertsEnabled) Debugging.Assert(upto == docFreq);
                        ent = new HighFreqTerm(docs, freqs, positions, payloads, totalTermFreq);
                    }

                    terms[count] = ent;
                    SetSkips(count, termBytes);
                    count++;
                }

                // End sentinel:
                termOffsets[count] = termOffset;

                FinishSkips();

                //System.out.println(skipCount + " skips: " + field);

                this.termBytes = new byte[termOffset];
                Arrays.Copy(termBytes, 0, this.termBytes, 0, termOffset);

                // Pack skips:
                this.skips = new int[skipCount];
                this.skipOffsets = new int[1 + numTerms];

                int skipOffset = 0;
                for (int i = 0; i < numTerms; i++)
                {
                    int[] termSkips = terms[i].Skips;
                    skipOffsets[i] = skipOffset;
                    if (termSkips != null)
                    {
                        Arrays.Copy(termSkips, 0, skips, skipOffset, termSkips.Length);
                        skipOffset += termSkips.Length;
                        terms[i].Skips = null;
                    }
                }
                this.skipOffsets[numTerms] = skipOffset;
                if (Debugging.AssertsEnabled) Debugging.Assert(skipOffset == skipCount);
            }

            /// <summary>Returns approximate RAM bytes used. </summary>
            public long RamBytesUsed()
            {
                long sizeInBytes = 0;
                sizeInBytes += ((termBytes != null) ? RamUsageEstimator.SizeOf(termBytes) : 0);
                sizeInBytes += ((termOffsets != null) ? RamUsageEstimator.SizeOf(termOffsets) : 0);
                sizeInBytes += ((skips != null) ? RamUsageEstimator.SizeOf(skips) : 0);
                sizeInBytes += ((skipOffsets != null) ? RamUsageEstimator.SizeOf(skipOffsets) : 0);
                sizeInBytes += ((sameCounts != null) ? RamUsageEstimator.SizeOf(sameCounts) : 0);

                if (terms != null)
                {
                    foreach (TermAndSkip termAndSkip in terms)
                    {
                        sizeInBytes += (termAndSkip != null) ? termAndSkip.RamBytesUsed() : 0;
                    }
                }

                return sizeInBytes;
            }

            // Compares in unicode (UTF8) order:
            private int Compare(int ord, BytesRef other)
            {
                byte[] otherBytes = other.Bytes;

                int upto = termOffsets[ord];
                int termLen = termOffsets[1 + ord] - upto;
                int otherUpto = other.Offset;

                int stop = upto + Math.Min(termLen, other.Length);
                while (upto < stop)
                {
                    int diff = (termBytes[upto++] & 0xFF) - (otherBytes[otherUpto++] & 0xFF);
                    if (diff != 0)
                    {
                        return diff;
                    }
                }

                // One is a prefix of the other, or, they are equal:
                return termLen - other.Length;
            }

            private void SetSkips(int termOrd, byte[] termBytes)
            {
                int termLength = termOffsets[termOrd + 1] - termOffsets[termOrd];

                if (sameCounts.Length < termLength)
                {
                    sameCounts = ArrayUtil.Grow(sameCounts, termLength);
                }

                // Update skip pointers:
                if (termOrd > 0)
                {

                    int lastTermLength = termOffsets[termOrd] - termOffsets[termOrd - 1];
                    int limit = Math.Min(termLength, lastTermLength);

                    int lastTermOffset = termOffsets[termOrd - 1];
                    int termOffset = termOffsets[termOrd];

                    int i = 0;
                    for (; i < limit; i++)
                    {
                        if (termBytes[lastTermOffset++] == termBytes[termOffset++])
                        {
                            sameCounts[i]++;
                        }
                        else
                        {
                            for (; i < limit; i++)
                            {
                                if (sameCounts[i] >= minSkipCount)
                                {
                                    // Go back and add a skip pointer:
                                    SaveSkip(termOrd, sameCounts[i]);
                                }
                                sameCounts[i] = 1;
                            }
                            break;
                        }
                    }

                    for (; i < lastTermLength; i++)
                    {
                        if (sameCounts[i] >= minSkipCount)
                        {
                            // Go back and add a skip pointer:
                            SaveSkip(termOrd, sameCounts[i]);
                        }
                        sameCounts[i] = 0;
                    }
                    for (int j = limit; j < termLength; j++)
                    {
                        sameCounts[j] = 1;
                    }
                }
                else
                {
                    for (int i = 0; i < termLength; i++)
                    {
                        sameCounts[i]++;
                    }
                }
            }

            private void FinishSkips()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(count == terms.Length);
                int lastTermOffset = termOffsets[count - 1];
                int lastTermLength = termOffsets[count] - lastTermOffset;

                for (int i = 0; i < lastTermLength; i++)
                {
                    if (sameCounts[i] >= minSkipCount)
                    {
                        // Go back and add a skip pointer:
                        SaveSkip(count, sameCounts[i]);
                    }
                }

                // Reverse the skip pointers so they are "nested":
                for (int termID = 0; termID < terms.Length; termID++)
                {
                    TermAndSkip term = terms[termID];
                    if (term.Skips != null && term.Skips.Length > 1)
                    {
                        for (int pos = 0; pos < term.Skips.Length/2; pos++)
                        {
                            int otherPos = term.Skips.Length - pos - 1;

                            int temp = term.Skips[pos];
                            term.Skips[pos] = term.Skips[otherPos];
                            term.Skips[otherPos] = temp;
                        }
                    }
                }
            }

            private void SaveSkip(int ord, int backCount)
            {
                TermAndSkip term = terms[ord - backCount];
                skipCount++;
                if (term.Skips is null)
                {
                    term.Skips = new int[] {ord};
                }
                else
                {
                    // Normally we'd grow at a slight exponential... but
                    // given that the skips themselves are already log(N)
                    // we can grow by only 1 and still have amortized
                    // linear time:
                    int[] newSkips = new int[term.Skips.Length + 1];
                    Arrays.Copy(term.Skips, 0, newSkips, 0, term.Skips.Length);
                    term.Skips = newSkips;
                    term.Skips[term.Skips.Length - 1] = ord;
                }
            }

            public override TermsEnum GetEnumerator()
            {
                var termsEnum = new DirectTermsEnum(this);
                termsEnum.Reset();
                return termsEnum;
            }

            public override TermsEnum GetEnumerator(TermsEnum reuse)
            {
                if (!(reuse is DirectTermsEnum termsEnum) || !termsEnum.CanReuse(terms))
                    termsEnum = new DirectTermsEnum(this);

                termsEnum.Reset();
                return termsEnum;
            }

            public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
            {
                return new DirectIntersectTermsEnum(this, compiled, startTerm);
            }

            public override long Count => terms.Length;

            public override long SumTotalTermFreq => sumTotalTermFreq;

            public override long SumDocFreq => sumDocFreq;

            public override int DocCount => docCount;

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override bool HasFreqs => hasFreq;

            public override bool HasOffsets => hasOffsets;

            public override bool HasPositions => hasPos;

            public override bool HasPayloads => hasPayloads;

            private sealed class DirectTermsEnum : TermsEnum
            {
                private readonly DirectPostingsFormat.DirectField outerInstance;

                private readonly BytesRef scratch = new BytesRef();
                private int termOrd;

                public DirectTermsEnum(DirectPostingsFormat.DirectField outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                internal bool CanReuse(TermAndSkip[] other)
                {
                    return outerInstance.terms == other;
                }

                internal BytesRef SetTerm()
                {
                    scratch.Bytes = outerInstance.termBytes;
                    scratch.Offset = outerInstance.termOffsets[termOrd];
                    scratch.Length = outerInstance.termOffsets[termOrd + 1] - outerInstance.termOffsets[termOrd];
                    return scratch;
                }

                public void Reset()
                {
                    termOrd = -1;
                }

                public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                public override bool MoveNext()
                {
                    termOrd++;
                    if (termOrd < outerInstance.terms.Length)
                    {
                        SetTerm();
                        return true;
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
                        return scratch;
                    return null;
                }

                public override TermState GetTermState()
                {
                    OrdTermState state = new OrdTermState();
                    state.Ord = termOrd;
                    return state;
                }

                // If non-negative, exact match; else, -ord-1, where ord
                // is where you would insert the term.
                private int FindTerm(BytesRef term)
                {

                    // Just do binary search: should be (constant factor)
                    // faster than using the skip list:
                    int low = 0;
                    int high = outerInstance.terms.Length - 1;

                    while (low <= high)
                    {
                        int mid = (low + high).TripleShift(1);
                        int cmp = outerInstance.Compare(mid, term);
                        if (cmp < 0)
                        {
                            low = mid + 1;
                        }
                        else if (cmp > 0)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            return mid; // key found
                        }
                    }

                    return -(low + 1); // key not found.
                }

                public override SeekStatus SeekCeil(BytesRef term)
                {
                    // TODO: we should use the skip pointers; should be
                    // faster than bin search; we should also hold
                    // & reuse current state so seeking forwards is
                    // faster
                    int ord = FindTerm(term);
                    // if (DEBUG) {
                    //   System.out.println("  find term=" + term.utf8ToString() + " ord=" + ord);
                    // }
                    if (ord >= 0)
                    {
                        termOrd = ord;
                        SetTerm();
                        return SeekStatus.FOUND;
                    }
                    else if (ord == -outerInstance.terms.Length - 1)
                    {
                        return SeekStatus.END;
                    }
                    else
                    {
                        termOrd = -ord - 1;
                        SetTerm();
                        return SeekStatus.NOT_FOUND;
                    }
                }

                public override bool SeekExact(BytesRef term)
                {
                    // TODO: we should use the skip pointers; should be
                    // faster than bin search; we should also hold
                    // & reuse current state so seeking forwards is
                    // faster
                    int ord = FindTerm(term);
                    if (ord >= 0)
                    {
                        termOrd = ord;
                        SetTerm();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public override void SeekExact(long ord)
                {
                    termOrd = (int) ord;
                    SetTerm();
                }

                public override void SeekExact(BytesRef term, TermState state)
                {
                    termOrd = (int) ((OrdTermState) state).Ord;
                    SetTerm();
                    if (Debugging.AssertsEnabled) Debugging.Assert(term.Equals(scratch));
                }

                public override BytesRef Term => scratch;

                public override long Ord => termOrd;

                public override int DocFreq =>
                    outerInstance.terms[termOrd] is LowFreqTerm lowFreqTerm ?
                    lowFreqTerm.DocFreq :
                    ((HighFreqTerm)outerInstance.terms[termOrd]).DocIDs.Length;


                public override long TotalTermFreq =>
                    outerInstance.terms[termOrd] is LowFreqTerm lowFreqTerm ?
                    lowFreqTerm.TotalTermFreq :
                    ((HighFreqTerm)outerInstance.terms[termOrd]).TotalTermFreq;


                public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
                {
                    // TODO: implement reuse, something like Pulsing:
                    // it's hairy!

                    if (outerInstance.terms[termOrd] is LowFreqTerm lowFreqTerm)
                    {
                        int[] postings = lowFreqTerm.Postings;
                        if (outerInstance.hasFreq)
                        {
                            if (outerInstance.hasPos)
                            {
                                int posLen;
                                if (outerInstance.hasOffsets)
                                {
                                    posLen = 3;
                                }
                                else
                                {
                                    posLen = 1;
                                }
                                if (outerInstance.hasPayloads)
                                {
                                    posLen++;
                                }
                                if (reuse is LowFreqDocsEnum docsEnum)
                                {
                                    if (!docsEnum.CanReuse(liveDocs, posLen))
                                    {
                                        docsEnum = new LowFreqDocsEnum(liveDocs, posLen);
                                    }
                                }
                                else
                                {
                                    docsEnum = new LowFreqDocsEnum(liveDocs, posLen);
                                }

                                return docsEnum.Reset(postings);
                            }
                            else
                            {
                                if (reuse is LowFreqDocsEnumNoPos docsEnum)
                                {
                                    if (!docsEnum.CanReuse(liveDocs))
                                    {
                                        docsEnum = new LowFreqDocsEnumNoPos(liveDocs);
                                    }
                                }
                                else
                                {
                                    docsEnum = new LowFreqDocsEnumNoPos(liveDocs);
                                }

                                return docsEnum.Reset(postings);
                            }
                        }
                        else
                        {
                            if (reuse is LowFreqDocsEnumNoTF docsEnum)
                            {
                                if (!docsEnum.CanReuse(liveDocs))
                                {
                                    docsEnum = new LowFreqDocsEnumNoTF(liveDocs);
                                }
                            }
                            else
                            {
                                docsEnum = new LowFreqDocsEnumNoTF(liveDocs);
                            }

                            return docsEnum.Reset(postings);
                        }
                    }
                    else
                    {
                        HighFreqTerm term = (HighFreqTerm) outerInstance.terms[termOrd];

                        if (reuse is HighFreqDocsEnum docsEnum)
                        {
                            if (!docsEnum.CanReuse(liveDocs))
                            {
                                docsEnum = new HighFreqDocsEnum(liveDocs);
                            }
                        }
                        else
                        {
                            docsEnum = new HighFreqDocsEnum(liveDocs);
                        }

                        //System.out.println("  DE for term=" + new BytesRef(terms[termOrd].term).utf8ToString() + ": " + term.docIDs.length + " docs");
                        return docsEnum.Reset(term.DocIDs, term.Freqs);
                    }
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse,
                    DocsAndPositionsFlags flags)
                {
                    if (!outerInstance.hasPos)
                    {
                        return null;
                    }

                    // TODO: implement reuse, something like Pulsing:
                    // it's hairy!

                    if (outerInstance.terms[termOrd] is LowFreqTerm lowFreqTerm)
                    {
                        int[] postings = lowFreqTerm.Postings;
                        byte[] payloads = lowFreqTerm.Payloads;
                        return
                            (new LowFreqDocsAndPositionsEnum(liveDocs, outerInstance.hasOffsets,
                                outerInstance.hasPayloads)).Reset(postings, payloads);
                    }
                    else
                    {   HighFreqTerm term = (HighFreqTerm) outerInstance.terms[termOrd];
                        return
                            (new HighFreqDocsAndPositionsEnum(liveDocs, outerInstance.hasOffsets)).Reset(
                                term.DocIDs, term.Freqs, term.Positions, term.Payloads);
                    }
                }
            }

            private sealed class DirectIntersectTermsEnum : TermsEnum
            {
                private readonly DirectPostingsFormat.DirectField outerInstance;

                private readonly RunAutomaton runAutomaton;
                private readonly CompiledAutomaton compiledAutomaton;
                private int termOrd;
                private readonly BytesRef scratch = new BytesRef();

                private sealed class State
                {
                    internal int changeOrd;
                    internal int state;
                    internal Transition[] transitions;
                    internal int transitionUpto;
                    internal int transitionMax;
                    internal int transitionMin;
                }

                private State[] states;
                private int stateUpto;

                public DirectIntersectTermsEnum(DirectPostingsFormat.DirectField outerInstance,
                    CompiledAutomaton compiled, BytesRef startTerm)
                {
                    this.outerInstance = outerInstance;
                    runAutomaton = compiled.RunAutomaton;
                    compiledAutomaton = compiled;
                    termOrd = -1;
                    states = new State[1];
                    states[0] = new State();
                    states[0].changeOrd = outerInstance.terms.Length;
                    states[0].state = runAutomaton.InitialState;
                    states[0].transitions = compiledAutomaton.SortedTransitions[states[0].state];
                    states[0].transitionUpto = -1;
                    states[0].transitionMax = -1;

                    //System.out.println("IE.init startTerm=" + startTerm);

                    if (startTerm != null)
                    {
                        int skipUpto = 0;
                        if (startTerm.Length == 0)
                        {
                            if (outerInstance.terms.Length > 0 && outerInstance.termOffsets[1] == 0)
                            {
                                termOrd = 0;
                            }
                        }
                        else
                        {
                            termOrd++;

                            for (int i = 0; i < startTerm.Length; i++)
                            {
                                int label = startTerm.Bytes[startTerm.Offset + i] & 0xFF;

                                while (label > states[i].transitionMax)
                                {
                                    states[i].transitionUpto++;
                                    if (Debugging.AssertsEnabled) Debugging.Assert(states[i].transitionUpto < states[i].transitions.Length);
                                    states[i].transitionMin = states[i].transitions[states[i].transitionUpto].Min;
                                    states[i].transitionMax = states[i].transitions[states[i].transitionUpto].Max;
                                    if (Debugging.AssertsEnabled)
                                    {
                                        Debugging.Assert(states[i].transitionMin >= 0);
                                        Debugging.Assert(states[i].transitionMin <= 255);
                                        Debugging.Assert(states[i].transitionMax >= 0);
                                        Debugging.Assert(states[i].transitionMax <= 255);
                                    }
                                }

                                // Skip forwards until we find a term matching
                                // the label at this position:
                                while (termOrd < outerInstance.terms.Length)
                                {
                                    int skipOffset = outerInstance.skipOffsets[termOrd];
                                    int numSkips = outerInstance.skipOffsets[termOrd + 1] - skipOffset;
                                    int termOffset_i = outerInstance.termOffsets[termOrd];
                                    int termLength = outerInstance.termOffsets[1 + termOrd] - termOffset_i;

                                    // if (DEBUG) {
                                    //   System.out.println("  check termOrd=" + termOrd + " term=" + new BytesRef(termBytes, termOffset, termLength).utf8ToString() + " skips=" + Arrays.toString(skips) + " i=" + i);
                                    // }

                                    if (termOrd == states[stateUpto].changeOrd)
                                    {
                                        // if (DEBUG) {
                                        //   System.out.println("  end push return");
                                        // }
                                        stateUpto--;
                                        termOrd--;
                                        return;
                                    }

                                    if (termLength == i)
                                    {
                                        termOrd++;
                                        skipUpto = 0;
                                        // if (DEBUG) {
                                        //   System.out.println("    term too short; next term");
                                        // }
                                    }
                                    else if (label < (outerInstance.termBytes[termOffset_i + i] & 0xFF))
                                    {
                                        termOrd--;
                                        // if (DEBUG) {
                                        //   System.out.println("  no match; already beyond; return termOrd=" + termOrd);
                                        // }
                                        stateUpto -= skipUpto;
                                        if (Debugging.AssertsEnabled) Debugging.Assert(stateUpto >= 0);
                                        return;
                                    }
                                    else if (label == (outerInstance.termBytes[termOffset_i + i] & 0xFF))
                                    {
                                        // if (DEBUG) {
                                        //   System.out.println("    label[" + i + "] matches");
                                        // }
                                        if (skipUpto < numSkips)
                                        {
                                            Grow();

                                            int nextState = runAutomaton.Step(states[stateUpto].state, label);

                                            // Automaton is required to accept startTerm:
                                            if (Debugging.AssertsEnabled) Debugging.Assert(nextState != -1);

                                            stateUpto++;
                                            states[stateUpto].changeOrd = outerInstance.skips[skipOffset + skipUpto++];
                                            states[stateUpto].state = nextState;
                                            states[stateUpto].transitions =
                                                compiledAutomaton.SortedTransitions[nextState];
                                            states[stateUpto].transitionUpto = -1;
                                            states[stateUpto].transitionMax = -1;
                                            //System.out.println("  push " + states[stateUpto].transitions.length + " trans");

                                            // if (DEBUG) {
                                            //   System.out.println("    push skip; changeOrd=" + states[stateUpto].changeOrd);
                                            // }

                                            // Match next label at this same term:
                                            goto nextLabelContinue;
                                        }
                                        else
                                        {
                                            // if (DEBUG) {
                                            //   System.out.println("    linear scan");
                                            // }
                                            // Index exhausted: just scan now (the
                                            // number of scans required will be less
                                            // than the minSkipCount):

                                            int startTermOrd = termOrd;
                                            while (termOrd < outerInstance.terms.Length &&
                                                   outerInstance.Compare(termOrd, startTerm) <= 0)
                                            {
                                                if (Debugging.AssertsEnabled) Debugging.Assert(termOrd == startTermOrd ||
                                                             outerInstance.skipOffsets[termOrd] ==
                                                             outerInstance.skipOffsets[termOrd + 1]);
                                                termOrd++;
                                            }
                                            if (Debugging.AssertsEnabled) Debugging.Assert(termOrd - startTermOrd < outerInstance.minSkipCount);
                                            termOrd--;
                                            stateUpto -= skipUpto;
                                            // if (DEBUG) {
                                            //   System.out.println("  end termOrd=" + termOrd);
                                            // }
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        if (skipUpto < numSkips)
                                        {
                                            termOrd = outerInstance.skips[skipOffset + skipUpto];
                                            // if (DEBUG) {
                                            //   System.out.println("  no match; skip to termOrd=" + termOrd);
                                            // }
                                        }
                                        else
                                        {
                                            // if (DEBUG) {
                                            //   System.out.println("  no match; next term");
                                            // }
                                            termOrd++;
                                        }
                                        skipUpto = 0;
                                    }
                                }

                                // startTerm is >= last term so enum will not
                                // return any terms:
                                termOrd--;
                                // if (DEBUG) {
                                //   System.out.println("  beyond end; no terms will match");
                                // }
                                return;
                                nextLabelContinue: {/* LUCENENET: intentionally blank */}
                            }
                            //nextLabelBreak: ; // LUCENENET NOTE: Not used
                        }

                        int termOffset = outerInstance.termOffsets[termOrd];
                        int termLen = outerInstance.termOffsets[1 + termOrd] - termOffset;

                        if (termOrd >= 0 &&
                            !startTerm.Equals(new BytesRef(outerInstance.termBytes, termOffset, termLen)))
                        {
                            stateUpto -= skipUpto;
                            termOrd--;
                        }
                        // if (DEBUG) {
                        //   System.out.println("  loop end; return termOrd=" + termOrd + " stateUpto=" + stateUpto);
                        // }
                    }
                }

                public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                private void Grow()
                {
                    if (states.Length == 1 + stateUpto)
                    {
                        State[] newStates = new State[states.Length + 1];
                        Arrays.Copy(states, 0, newStates, 0, states.Length);
                        newStates[states.Length] = new State();
                        states = newStates;
                    }
                }

                public override bool MoveNext()
                {
                    // if (DEBUG) {
                    //   System.out.println("\nIE.next");
                    // }

                    termOrd++;
                    int skipUpto = 0;

                    if (termOrd == 0 && outerInstance.termOffsets[1] == 0)
                    {
                        // Special-case empty string:
                        if (Debugging.AssertsEnabled) Debugging.Assert(stateUpto == 0);
                        // if (DEBUG) {
                        //   System.out.println("  visit empty string");
                        // }
                        if (runAutomaton.IsAccept(states[0].state))
                        {
                            scratch.Bytes = outerInstance.termBytes;
                            scratch.Offset = 0;
                            scratch.Length = 0;
                            return true;
                        }
                        termOrd++;
                    }


                    while (true)
                    {
                        // if (DEBUG) {
                        //   System.out.println("  cycle termOrd=" + termOrd + " stateUpto=" + stateUpto + " skipUpto=" + skipUpto);
                        // }
                        if (termOrd == outerInstance.terms.Length)
                        {
                            // if (DEBUG) {
                            //   System.out.println("  return END");
                            // }
                            return false;
                        }

                        State state = states[stateUpto];
                        if (termOrd == state.changeOrd)
                        {
                            // Pop:
                            // if (DEBUG) {
                            //   System.out.println("  pop stateUpto=" + stateUpto);
                            // }
                            stateUpto--;

                            continue;
                        }

                        int termOffset = outerInstance.termOffsets[termOrd];
                        int termLength = outerInstance.termOffsets[termOrd + 1] - termOffset;
                        int skipOffset = outerInstance.skipOffsets[termOrd];
                        int numSkips = outerInstance.skipOffsets[termOrd + 1] - skipOffset;

                        // if (DEBUG) {
                        //   System.out.println("  term=" + new BytesRef(termBytes, termOffset, termLength).utf8ToString() + " skips=" + Arrays.toString(skips));
                        // }

                        if (Debugging.AssertsEnabled) Debugging.Assert(termOrd < state.changeOrd);

                        if (Debugging.AssertsEnabled) Debugging.Assert(stateUpto <= termLength, "term.length={0}; stateUpto={1}", termLength, stateUpto);
                        int label = outerInstance.termBytes[termOffset + stateUpto] & 0xFF;

                        while (label > state.transitionMax)
                        {
                            //System.out.println("  label=" + label + " vs max=" + state.transitionMax + " transUpto=" + state.transitionUpto + " vs " + state.transitions.length);
                            state.transitionUpto++;
                            if (state.transitionUpto == state.transitions.Length)
                            {
                                // We've exhausted transitions leaving this
                                // state; force pop+next/skip now:
                                //System.out.println("forcepop: stateUpto=" + stateUpto);
                                if (stateUpto == 0)
                                {
                                    termOrd = outerInstance.terms.Length;
                                    return false;
                                }
                                else
                                {
                                    if (Debugging.AssertsEnabled) Debugging.Assert(state.changeOrd > termOrd);
                                    // if (DEBUG) {
                                    //   System.out.println("  jumpend " + (state.changeOrd - termOrd));
                                    // }
                                    //System.out.println("  jump to termOrd=" + states[stateUpto].changeOrd + " vs " + termOrd);
                                    termOrd = states[stateUpto].changeOrd;
                                    skipUpto = 0;
                                    stateUpto--;
                                }
                                goto nextTermContinue;
                            }
                            if (Debugging.AssertsEnabled) Debugging.Assert(state.transitionUpto < state.transitions.Length,
                                " state.transitionUpto={0} vs {1}", state.transitionUpto, state.transitions.Length);
                            state.transitionMin = state.transitions[state.transitionUpto].Min;
                            state.transitionMax = state.transitions[state.transitionUpto].Max;
                            if (Debugging.AssertsEnabled)
                            {
                                Debugging.Assert(state.transitionMin >= 0);
                                Debugging.Assert(state.transitionMin <= 255);
                                Debugging.Assert(state.transitionMax >= 0);
                                Debugging.Assert(state.transitionMax <= 255);
                            }
                        }

                        int targetLabel = state.transitionMin;

                        if ((outerInstance.termBytes[termOffset + stateUpto] & 0xFF) < targetLabel)
                        {
                            // if (DEBUG) {
                            //   System.out.println("    do bin search");
                            // }
                            //int startTermOrd = termOrd;
                            int low = termOrd + 1;
                            int high = state.changeOrd - 1;
                            while (true)
                            {
                                if (low > high)
                                {
                                    // Label not found
                                    termOrd = low;
                                    // if (DEBUG) {
                                    //   System.out.println("      advanced by " + (termOrd - startTermOrd));
                                    // }
                                    //System.out.println("  jump " + (termOrd - startTermOrd));
                                    skipUpto = 0;
                                    goto nextTermContinue;
                                }
                                int mid = (low + high).TripleShift(1);
                                int cmp = (outerInstance.termBytes[outerInstance.termOffsets[mid] + stateUpto] & 0xFF) -
                                          targetLabel;
                                // if (DEBUG) {
                                //   System.out.println("      bin: check label=" + (char) (termBytes[termOffsets[low] + stateUpto] & 0xFF) + " ord=" + mid);
                                // }
                                if (cmp < 0)
                                {
                                    low = mid + 1;
                                }
                                else if (cmp > 0)
                                {
                                    high = mid - 1;
                                }
                                else
                                {
                                    // Label found; walk backwards to first
                                    // occurrence:
                                    while (mid > termOrd &&
                                           (outerInstance.termBytes[outerInstance.termOffsets[mid - 1] + stateUpto] &
                                            0xFF) == targetLabel)
                                    {
                                        mid--;
                                    }
                                    termOrd = mid;
                                    // if (DEBUG) {
                                    //   System.out.println("      advanced by " + (termOrd - startTermOrd));
                                    // }
                                    //System.out.println("  jump " + (termOrd - startTermOrd));
                                    skipUpto = 0;
                                    goto nextTermContinue;
                                }
                            }
                        }

                        int nextState = runAutomaton.Step(states[stateUpto].state, label);

                        if (nextState == -1)
                        {
                            // Skip
                            // if (DEBUG) {
                            //   System.out.println("  automaton doesn't accept; skip");
                            // }
                            if (skipUpto < numSkips)
                            {
                                // if (DEBUG) {
                                //   System.out.println("  jump " + (skips[skipOffset+skipUpto]-1 - termOrd));
                                // }
                                termOrd = outerInstance.skips[skipOffset + skipUpto];
                            }
                            else
                            {
                                termOrd++;
                            }
                            skipUpto = 0;
                        }
                        else if (skipUpto < numSkips)
                        {
                            Grow();
                            stateUpto++;
                            states[stateUpto].state = nextState;
                            states[stateUpto].changeOrd = outerInstance.skips[skipOffset + skipUpto++];
                            states[stateUpto].transitions = compiledAutomaton.SortedTransitions[nextState];
                            states[stateUpto].transitionUpto = -1;
                            states[stateUpto].transitionMax = -1;

                            if (stateUpto == termLength)
                            {
                                // if (DEBUG) {
                                //   System.out.println("  term ends after push");
                                // }
                                if (runAutomaton.IsAccept(nextState))
                                {
                                    // if (DEBUG) {
                                    //   System.out.println("  automaton accepts: return");
                                    // }
                                    scratch.Bytes = outerInstance.termBytes;
                                    scratch.Offset = outerInstance.termOffsets[termOrd];
                                    scratch.Length = outerInstance.termOffsets[1 + termOrd] - scratch.Offset;
                                    // if (DEBUG) {
                                    //   System.out.println("  ret " + scratch.utf8ToString());
                                    // }
                                    return true;
                                }
                                else
                                {
                                    // if (DEBUG) {
                                    //   System.out.println("  automaton rejects: nextTerm");
                                    // }
                                    termOrd++;
                                    skipUpto = 0;
                                }
                            }
                        }
                        else
                        {
                            // Run the non-indexed tail of this term:

                            // TODO: add assert that we don't inc too many times

                            if (compiledAutomaton.CommonSuffixRef != null)
                            {
                                //System.out.println("suffix " + compiledAutomaton.commonSuffixRef.utf8ToString());
                                if (Debugging.AssertsEnabled) Debugging.Assert(compiledAutomaton.CommonSuffixRef.Offset == 0);
                                if (termLength < compiledAutomaton.CommonSuffixRef.Length)
                                {
                                    termOrd++;
                                    skipUpto = 0;
                                    goto nextTermContinue;
                                }
                                int offset = termOffset + termLength - compiledAutomaton.CommonSuffixRef.Length;
                                for (int suffix = 0; suffix < compiledAutomaton.CommonSuffixRef.Length; suffix++)
                                {
                                    if (outerInstance.termBytes[offset + suffix] !=
                                        compiledAutomaton.CommonSuffixRef.Bytes[suffix])
                                    {
                                        termOrd++;
                                        skipUpto = 0;
                                        goto nextTermContinue;
                                    }
                                }
                            }

                            int upto = stateUpto + 1;
                            while (upto < termLength)
                            {
                                nextState = runAutomaton.Step(nextState, outerInstance.termBytes[termOffset + upto] & 0xFF);
                                if (nextState == -1)
                                {
                                    termOrd++;
                                    skipUpto = 0;
                                    // if (DEBUG) {
                                    //   System.out.println("  nomatch tail; next term");
                                    // }
                                    goto nextTermContinue;
                                }
                                upto++;
                            }

                            if (runAutomaton.IsAccept(nextState))
                            {
                                scratch.Bytes = outerInstance.termBytes;
                                scratch.Offset = outerInstance.termOffsets[termOrd];
                                scratch.Length = outerInstance.termOffsets[1 + termOrd] - scratch.Offset;
                                // if (DEBUG) {
                                //   System.out.println("  match tail; return " + scratch.utf8ToString());
                                //   System.out.println("  ret2 " + scratch.utf8ToString());
                                // }
                                return true;
                            }
                            else
                            {
                                termOrd++;
                                skipUpto = 0;
                                // if (DEBUG) {
                                //   System.out.println("  nomatch tail; next term");
                                // }
                            }
                        }
                    nextTermContinue: {/* LUCENENET: intentionally blank */}
                    }

                    //nextTermBreak: ; // LUCENENET NOTE: Not used
                }

                [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override BytesRef Next()
                {
                    if (MoveNext())
                        return scratch;
                    return null;
                }

                public override TermState GetTermState()
                {
                    OrdTermState state = new OrdTermState();
                    state.Ord = termOrd;
                    return state;
                }

                public override BytesRef Term => scratch;

                public override long Ord => termOrd;

                public override int DocFreq =>
                    outerInstance.terms[termOrd] is LowFreqTerm lowFreqTerm ?
                    lowFreqTerm.DocFreq :
                    ((HighFreqTerm)outerInstance.terms[termOrd]).DocIDs.Length;

                public override long TotalTermFreq =>
                    outerInstance.terms[termOrd] is LowFreqTerm lowFreqTerm ?
                    lowFreqTerm.TotalTermFreq :
                    ((HighFreqTerm)outerInstance.terms[termOrd]).TotalTermFreq;


                public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
                {
                    // TODO: implement reuse, something like Pulsing:
                    // it's hairy!

                    if (outerInstance.terms[termOrd] is LowFreqTerm lowFreqTerm)
                    {
                        int[] postings = lowFreqTerm.Postings;
                        if (outerInstance.hasFreq)
                        {
                            if (outerInstance.hasPos)
                            {
                                int posLen;
                                if (outerInstance.hasOffsets)
                                {
                                    posLen = 3;
                                }
                                else
                                {
                                    posLen = 1;
                                }
                                if (outerInstance.hasPayloads)
                                {
                                    posLen++;
                                }
                                return (new LowFreqDocsEnum(liveDocs, posLen)).Reset(postings);
                            }
                            else
                            {
                                return (new LowFreqDocsEnumNoPos(liveDocs)).Reset(postings);
                            }
                        }
                        else
                        {
                            return (new LowFreqDocsEnumNoTF(liveDocs)).Reset(postings);
                        }
                    }
                    else
                    {
                        HighFreqTerm term = (HighFreqTerm) outerInstance.terms[termOrd];
                        //  System.out.println("DE for term=" + new BytesRef(terms[termOrd].term).utf8ToString() + ": " + term.docIDs.length + " docs");
                        return (new HighFreqDocsEnum(liveDocs)).Reset(term.DocIDs, term.Freqs);
                    }
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse,
                    DocsAndPositionsFlags flags)
                {
                    if (!outerInstance.hasPos)
                    {
                        return null;
                    }

                    // TODO: implement reuse, something like Pulsing:
                    // it's hairy!

                    if (outerInstance.terms[termOrd] is LowFreqTerm)
                    {
                        LowFreqTerm term = ((LowFreqTerm) outerInstance.terms[termOrd]);
                        int[] postings = term.Postings;
                        byte[] payloads = term.Payloads;
                        return
                            (new LowFreqDocsAndPositionsEnum(liveDocs, outerInstance.hasOffsets,
                                outerInstance.hasPayloads)).Reset(postings, payloads);
                    }
                    else
                    {
                        HighFreqTerm term = (HighFreqTerm) outerInstance.terms[termOrd];
                        return
                            (new HighFreqDocsAndPositionsEnum(liveDocs, outerInstance.hasOffsets)).Reset(
                                term.DocIDs, term.Freqs, term.Positions, term.Payloads);
                    }
                }

                public override SeekStatus SeekCeil(BytesRef term)
                {
                    throw UnsupportedOperationException.Create();
                }

                public override void SeekExact(long ord)
                {
                    throw UnsupportedOperationException.Create();
                }
            }
        }

        // Docs only:
        private sealed class LowFreqDocsEnumNoTF : DocsEnum
        {
            private int[] postings;
            private readonly IBits liveDocs;
            private int upto;

            public LowFreqDocsEnumNoTF(IBits liveDocs)
            {
                this.liveDocs = liveDocs;
            }

            public bool CanReuse(IBits liveDocs)
            {
                return liveDocs == this.liveDocs;
            }

            public DocsEnum Reset(int[] postings)
            {
                this.postings = postings;
                upto = -1;
                return this;
            }

            // TODO: can do this w/o setting members?

            public override int NextDoc()
            {
                // LUCENENET: Refactored to avoid throwing IndexOutOfRangeExcpetion in the normal flow
                upto++;
                if (liveDocs is null)
                {
                    if (upto < postings.Length)
                    {
                        return postings[upto];
                    }
                }
                else
                {
                    while (upto < postings.Length)
                    {
                        if (liveDocs.Get(postings[upto]))
                        {
                            return postings[upto];
                        }
                        upto++;
                    }
                }
                return NO_MORE_DOCS;
            }

            public override int DocID
            {
                get
                {
                    if (upto < 0)
                    {
                        return -1;
                    }
                    else if (upto < postings.Length)
                    {
                        return postings[upto];
                    }
                    else
                    {
                        return NO_MORE_DOCS;
                    }
                }
            }

            public override int Freq => 1;

            public override int Advance(int target)
            {
                // Linear scan, but this is low-freq term so it won't
                // be costly:
                return SlowAdvance(target);
            }

            public override long GetCost()
            {
                return postings.Length;
            }
        }

        // Docs + freqs:
        private sealed class LowFreqDocsEnumNoPos : DocsEnum
        {
            private int[] postings;
            private readonly IBits liveDocs;
            private int upto;

            public LowFreqDocsEnumNoPos(IBits liveDocs)
            {
                this.liveDocs = liveDocs;
            }

            public bool CanReuse(IBits liveDocs)
            {
                return liveDocs == this.liveDocs;
            }

            public DocsEnum Reset(int[] postings)
            {
                this.postings = postings;
                upto = -2;
                return this;
            }

            // TODO: can do this w/o setting members?
            public override int NextDoc()
            {
                upto += 2;
                if (liveDocs is null)
                {
                    if (upto < postings.Length)
                    {
                        return postings[upto];
                    }
                }
                else
                {
                    while (upto < postings.Length)
                    {
                        if (liveDocs.Get(postings[upto]))
                        {
                            return postings[upto];
                        }
                        upto += 2;
                    }
                }
                return NO_MORE_DOCS;
            }

            public override int DocID
            {
                get
                {
                    if (upto < 0)
                    {
                        return -1;
                    }
                    else if (upto < postings.Length)
                    {
                        return postings[upto];
                    }
                    else
                    {
                        return NO_MORE_DOCS;
                    }
                }
            }

            public override int Freq => postings[upto + 1];

            public override int Advance(int target)
            {
                // Linear scan, but this is low-freq term so it won't
                // be costly:
                return SlowAdvance(target);
            }

            public override long GetCost()
            {
                return postings.Length/2;
            }
        }

        // Docs + freqs + positions/offets:
        private sealed class LowFreqDocsEnum : DocsEnum
        {
            private int[] postings;
            private readonly IBits liveDocs;
            private readonly int posMult;
            private int upto;
            private int freq;

            public LowFreqDocsEnum(IBits liveDocs, int posMult)
            {
                this.liveDocs = liveDocs;
                this.posMult = posMult;
                // if (DEBUG) {
                //   System.out.println("LowFreqDE: posMult=" + posMult);
                // }
            }

            public bool CanReuse(IBits liveDocs, int posMult)
            {
                return liveDocs == this.liveDocs && posMult == this.posMult;
            }

            public DocsEnum Reset(int[] postings)
            {
                this.postings = postings;
                upto = -2;
                freq = 0;
                return this;
            }

            // TODO: can do this w/o setting members?
            public override int NextDoc()
            {
                upto += 2 + freq*posMult;
                // if (DEBUG) {
                //   System.out.println("  nextDoc freq=" + freq + " upto=" + upto + " vs " + postings.length);
                // }
                if (liveDocs is null)
                {
                    if (upto < postings.Length)
                    {
                        freq = postings[upto + 1];
                        if (Debugging.AssertsEnabled) Debugging.Assert(freq > 0);
                        return postings[upto];
                    }
                }
                else
                {
                    while (upto < postings.Length)
                    {
                        freq = postings[upto + 1];
                        if (Debugging.AssertsEnabled) Debugging.Assert(freq > 0);
                        if (liveDocs.Get(postings[upto]))
                        {
                            return postings[upto];
                        }
                        upto += 2 + freq*posMult;
                    }
                }
                return NO_MORE_DOCS;
            }

            public override int DocID
            {
                get
                {
                    // TODO: store docID member?
                    if (upto < 0)
                    {
                        return -1;
                    }
                    else if (upto < postings.Length)
                    {
                        return postings[upto];
                    }
                    else
                    {
                        return NO_MORE_DOCS;
                    }
                }
            }

            public override int Freq =>
                // TODO: can I do postings[upto+1]?
                freq;

            public override int Advance(int target)
            {
                // Linear scan, but this is low-freq term so it won't
                // be costly:
                return SlowAdvance(target);
            }

            public override long GetCost()
            {
                // TODO: could do a better estimate
                return postings.Length/2;
            }
        }

        private sealed class LowFreqDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private int[] postings;
            private readonly IBits liveDocs;
            private readonly int posMult;
            private readonly bool hasOffsets;
            private readonly bool hasPayloads;
            private readonly BytesRef payload = new BytesRef();
            private int upto;
            private int docID;
            private int freq;
            private int skipPositions;
            private int startOffset;
            private int endOffset;
            private int lastPayloadOffset;
            private int payloadOffset;
            private int payloadLength;
            private byte[] payloadBytes;

            public LowFreqDocsAndPositionsEnum(IBits liveDocs, bool hasOffsets, bool hasPayloads)
            {
                this.liveDocs = liveDocs;
                this.hasOffsets = hasOffsets;
                this.hasPayloads = hasPayloads;
                if (hasOffsets)
                {
                    if (hasPayloads)
                    {
                        posMult = 4;
                    }
                    else
                    {
                        posMult = 3;
                    }
                }
                else if (hasPayloads)
                {
                    posMult = 2;
                }
                else
                {
                    posMult = 1;
                }
            }

            public DocsAndPositionsEnum Reset(int[] postings, byte[] payloadBytes)
            {
                this.postings = postings;
                upto = 0;
                skipPositions = 0;
                startOffset = -1;
                endOffset = -1;
                docID = -1;
                payloadLength = 0;
                this.payloadBytes = payloadBytes;
                return this;
            }

            public override int NextDoc()
            {
                if (hasPayloads)
                {
                    for (int i = 0; i < skipPositions; i++)
                    {
                        upto++;
                        if (hasOffsets)
                        {
                            upto += 2;
                        }
                        payloadOffset += postings[upto++];
                    }
                }
                else
                {
                    upto += posMult*skipPositions;
                }

                if (liveDocs is null)
                {
                    if (upto < postings.Length)
                    {
                        docID = postings[upto++];
                        freq = postings[upto++];
                        skipPositions = freq;
                        return docID;
                    }
                }
                else
                {
                    while (upto < postings.Length)
                    {
                        docID = postings[upto++];
                        freq = postings[upto++];
                        if (liveDocs.Get(docID))
                        {
                            skipPositions = freq;
                            return docID;
                        }
                        if (hasPayloads)
                        {
                            for (int i = 0; i < freq; i++)
                            {
                                upto++;
                                if (hasOffsets)
                                {
                                    upto += 2;
                                }
                                payloadOffset += postings[upto++];
                            }
                        }
                        else
                        {
                            upto += posMult*freq;
                        }
                    }
                }

                return docID = NO_MORE_DOCS;
            }

            public override int DocID => docID;

            public override int Freq => freq;

            public override int NextPosition()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(skipPositions > 0);
                skipPositions--;

                int pos = postings[upto++];
                if (hasOffsets)
                {
                    startOffset = postings[upto++];
                    endOffset = postings[upto++];
                }
                if (hasPayloads)
                {
                    payloadLength = postings[upto++];
                    lastPayloadOffset = payloadOffset;
                    payloadOffset += payloadLength;
                }
                return pos;
            }

            public override int StartOffset => startOffset;

            public override int EndOffset => endOffset;

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public override BytesRef GetPayload()
            {
                if (payloadLength > 0)
                {
                    payload.Bytes = payloadBytes;
                    payload.Offset = lastPayloadOffset;
                    payload.Length = payloadLength;
                    return payload;
                }
                else
                {
                    return null;
                }
            }

            public override long GetCost()
            {
                // TODO: could do a better estimate
                return postings.Length/2;
            }
        }

        // Docs + freqs:
        private sealed class HighFreqDocsEnum : DocsEnum
        {
            private int[] docIDs;
            private int[] freqs;
            private readonly IBits liveDocs;
            private int upto;
            private int docID = -1;

            public HighFreqDocsEnum(IBits liveDocs)
            {
                this.liveDocs = liveDocs;
            }

            public bool CanReuse(IBits liveDocs)
            {
                return liveDocs == this.liveDocs;
            }

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[] DocIDs => docIDs;

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[] Freqs => freqs;

            public DocsEnum Reset(int[] docIDs, int[] freqs)
            {
                this.docIDs = docIDs;
                this.freqs = freqs;
                docID = upto = -1;
                return this;
            }

            public override int NextDoc()
            {
                upto++;
                if (liveDocs is null)
                {
                    // LUCENENET: Proactively check bounds so we don't have to catch and ingore an exception
                    if (upto >= 0 && upto < docIDs.Length)
                        return docID = docIDs[upto];
                }
                else
                {
                    while (upto < docIDs.Length)
                    {
                        if (liveDocs.Get(docIDs[upto]))
                        {
                            return docID = docIDs[upto];
                        }
                        upto++;
                    }
                }
                return docID = NO_MORE_DOCS;
            }

            public override int DocID => docID;

            public override int Freq
            {
                get
                {
                    if (freqs is null)
                    {
                        return 1;
                    }
                    else
                    {
                        return freqs[upto];
                    }
                }
            }

            public override int Advance(int target)
            {

                //System.out.println("  advance target=" + target + " cur=" + docID() + " upto=" + upto + " of " + docIDs.length);
                // if (DEBUG) {
                //   System.out.println("advance target=" + target + " len=" + docIDs.length);
                // }
                upto++;
                if (upto == docIDs.Length)
                {
                    return docID = NO_MORE_DOCS;
                }

                // First "grow" outwards, since most advances are to
                // nearby docs:
                int inc = 10;
                int nextUpto = upto + 10;
                int low;
                int high;
                while (true)
                {
                    //System.out.println("  grow nextUpto=" + nextUpto + " inc=" + inc);
                    if (nextUpto >= docIDs.Length)
                    {
                        low = nextUpto - inc;
                        high = docIDs.Length - 1;
                        break;
                    }
                    //System.out.println("    docID=" + docIDs[nextUpto]);

                    if (target <= docIDs[nextUpto])
                    {
                        low = nextUpto - inc;
                        high = nextUpto;
                        break;
                    }
                    inc *= 2;
                    nextUpto += inc;
                }

                // Now do normal binary search
                //System.out.println("    after fwd: low=" + low + " high=" + high);

                while (true)
                {

                    if (low > high)
                    {
                        // Not exactly found
                        //System.out.println("    break: no match");
                        upto = low;
                        break;
                    }

                    int mid = (low + high).TripleShift(1);
                    int cmp = docIDs[mid] - target;
                    //System.out.println("    bsearch low=" + low + " high=" + high+ ": docIDs[" + mid + "]=" + docIDs[mid]);

                    if (cmp < 0)
                    {
                        low = mid + 1;
                    }
                    else if (cmp > 0)
                    {
                        high = mid - 1;
                    }
                    else
                    {
                        // Found target
                        upto = mid;
                        //System.out.println("    break: match");
                        break;
                    }
                }

                //System.out.println("    end upto=" + upto + " docID=" + (upto >= docIDs.length ? NO_MORE_DOCS : docIDs[upto]));

                if (liveDocs != null)
                {
                    while (upto < docIDs.Length)
                    {
                        if (liveDocs.Get(docIDs[upto]))
                        {
                            break;
                        }
                        upto++;
                    }
                }
                if (upto == docIDs.Length)
                {
                    //System.out.println("    return END");
                    return docID = NO_MORE_DOCS;
                }
                else
                {
                    //System.out.println("    return docID=" + docIDs[upto] + " upto=" + upto);
                    return docID = docIDs[upto];
                }
            }

            public override long GetCost()
            {
                return docIDs.Length;
            }
        }

        // TODO: specialize offsets and not
        private sealed class HighFreqDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private int[] docIDs;
            private int[] freqs;
            private int[][] positions;
            private byte[][][] payloads;
            private readonly IBits liveDocs;
            private readonly bool hasOffsets;
            private readonly int posJump;
            private int upto;
            private int docID = -1;
            private int posUpto;
            private int[] curPositions;

            public HighFreqDocsAndPositionsEnum(IBits liveDocs, bool hasOffsets)
            {
                this.liveDocs = liveDocs;
                this.hasOffsets = hasOffsets;
                posJump = hasOffsets ? 3 : 1;
            }

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[] DocIDs => docIDs;

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[][] Positions => positions;

            public int PosJump => posJump;

            public IBits LiveDocs => liveDocs;

            public DocsAndPositionsEnum Reset(int[] docIDs, int[] freqs, int[][] positions, byte[][][] payloads)
            {
                this.docIDs = docIDs;
                this.freqs = freqs;
                this.positions = positions;
                this.payloads = payloads;
                upto = -1;
                return this;
            }

            public override int NextDoc()
            {
                upto++;
                if (liveDocs is null)
                {
                    if (upto < docIDs.Length)
                    {
                        posUpto = -posJump;
                        curPositions = positions[upto];
                        return docID = docIDs[upto];
                    }
                }
                else
                {
                    while (upto < docIDs.Length)
                    {
                        if (liveDocs.Get(docIDs[upto]))
                        {
                            posUpto = -posJump;
                            curPositions = positions[upto];
                            return docID = docIDs[upto];
                        }
                        upto++;
                    }
                }

                return docID = NO_MORE_DOCS;
            }

            public override int Freq => freqs[upto];

            public override int DocID => docID;

            public override int NextPosition()
            {
                posUpto += posJump;
                return curPositions[posUpto];
            }

            public override int StartOffset
            {
                get
                {
                    if (hasOffsets)
                        return curPositions[posUpto + 1];

                    return -1;
                }
            }

            public override int EndOffset
            {
                get
                {
                    if (hasOffsets)
                        return curPositions[posUpto + 2];

                    return -1;
                }
            }

            public override int Advance(int target)
            {
                upto++;
                if (upto == docIDs.Length)
                {
                    return docID = NO_MORE_DOCS;
                }

                // First "grow" outwards, since most advances are to
                // nearby docs:
                int inc = 10;
                int nextUpto = upto + 10;
                int low;
                int high;
                while (true)
                {
                    //System.out.println("  grow nextUpto=" + nextUpto + " inc=" + inc);
                    if (nextUpto >= docIDs.Length)
                    {
                        low = nextUpto - inc;
                        high = docIDs.Length - 1;
                        break;
                    }
                    //System.out.println("    docID=" + docIDs[nextUpto]);

                    if (target <= docIDs[nextUpto])
                    {
                        low = nextUpto - inc;
                        high = nextUpto;
                        break;
                    }
                    inc *= 2;
                    nextUpto += inc;
                }

                // Now do normal binary search
                //System.out.println("    after fwd: low=" + low + " high=" + high);

                while (true)
                {

                    if (low > high)
                    {
                        // Not exactly found
                        //System.out.println("    break: no match");
                        upto = low;
                        break;
                    }

                    int mid = (low + high).TripleShift(1);
                    int cmp = docIDs[mid] - target;
                    //System.out.println("    bsearch low=" + low + " high=" + high+ ": docIDs[" + mid + "]=" + docIDs[mid]);

                    if (cmp < 0)
                    {
                        low = mid + 1;
                    }
                    else if (cmp > 0)
                    {
                        high = mid - 1;
                    }
                    else
                    {
                        // Found target
                        upto = mid;
                        //System.out.println("    break: match");
                        break;
                    }
                }

                //System.out.println("    end upto=" + upto + " docID=" + (upto >= docIDs.length ? NO_MORE_DOCS : docIDs[upto]));

                if (liveDocs != null)
                {
                    while (upto < docIDs.Length)
                    {
                        if (liveDocs.Get(docIDs[upto]))
                        {
                            break;
                        }
                        upto++;
                    }
                }
                if (upto == docIDs.Length)
                {
                    //System.out.println("    return END");
                    return docID = NO_MORE_DOCS;
                }
                else
                {
                    //System.out.println("    return docID=" + docIDs[upto] + " upto=" + upto);
                    posUpto = -posJump;
                    curPositions = positions[upto];
                    return docID = docIDs[upto];
                }
            }

            private readonly BytesRef _payload = new BytesRef();

            public override BytesRef GetPayload()
            {
                if (payloads is null)
                    return null;

                var payloadBytes = payloads[upto][posUpto/(hasOffsets ? 3 : 1)];
                if (payloadBytes is null)
                {
                    return null;
                }
                _payload.Bytes = payloadBytes;
                _payload.Length = payloadBytes.Length;
                _payload.Offset = 0;
                return _payload;
            }

            public override long GetCost()
            {
                return docIDs.Length;
            }
        }
    }
}