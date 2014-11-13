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

namespace Lucene.Net.Codecs.Memory
{

    using System;
    using System.Diagnostics;
    using System.Collections.Generic;

    using Lucene41PostingsFormat = Lucene41.Lucene41PostingsFormat;
    using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
    using DocsEnum = Index.DocsEnum;
    using IndexOptions = Index.FieldInfo.IndexOptions;
    using FieldInfo = Index.FieldInfo;
    using Fields = Index.Fields;
    using OrdTermState = Index.OrdTermState;
    using SegmentReadState = Index.SegmentReadState;
    using SegmentWriteState = Index.SegmentWriteState;
    using TermState = Index.TermState;
    using Terms = Index.Terms;
    using TermsEnum = Index.TermsEnum;
    using IOContext = Store.IOContext;
    using RAMOutputStream = Store.RAMOutputStream;
    using ArrayUtil = Util.ArrayUtil;
    using Bits = Util.Bits;
    using BytesRef = Util.BytesRef;
    using RamUsageEstimator = Util.RamUsageEstimator;
    using CompiledAutomaton = Util.Automaton.CompiledAutomaton;
    using RunAutomaton = Util.Automaton.RunAutomaton;
    using Transition = Util.Automaton.Transition;

    // TODO: 
    //   - build depth-N prefix hash?
    //   - or: longer dense skip lists than just next byte?

    /// <summary>
    /// Wraps <seealso cref="Lucene41PostingsFormat"/> format for on-disk
    ///  storage, but then at read time loads and stores all
    ///  terms & postings directly in RAM as byte[], int[].
    /// 
    ///  <para><b>WARNING</b>: This is
    ///  exceptionally RAM intensive: it makes no effort to
    ///  compress the postings data, storing terms as separate
    ///  byte[] and postings as separate int[], but as a result it 
    ///  gives substantial increase in search performance.
    /// 
    /// </para>
    ///  <para>This postings format supports <seealso cref="TermsEnum#ord"/>
    ///  and <seealso cref="TermsEnum#seekExact(long)"/>.
    /// 
    /// </para>
    ///  <para>Because this holds all term bytes as a single
    ///  byte[], you cannot have more than 2.1GB worth of term
    ///  bytes in a single segment.
    /// 
    /// @lucene.experimental 
    /// </para>
    /// </summary>

    public sealed class DirectPostingsFormat : PostingsFormat
    {

        private readonly int _minSkipCount;
        private readonly int _lowFreqCutoff;

        private const int DEFAULT_MIN_SKIP_COUNT = 8;
        private const int DEFAULT_LOW_FREQ_CUTOFF = 32;

        // TODO: allow passing/wrapping arbitrary postings format?

        public DirectPostingsFormat() : this(DEFAULT_MIN_SKIP_COUNT, DEFAULT_LOW_FREQ_CUTOFF)
        {
        }

        /// <summary>
        /// minSkipCount is how many terms in a row must have the
        ///  same prefix before we put a skip pointer down.  Terms
        ///  with docFreq less than or equal lowFreqCutoff will use a single int[]
        ///  to hold all docs, freqs, position and offsets; terms
        ///  with higher docFreq will use separate arrays. 
        /// </summary>
        public DirectPostingsFormat(int minSkipCount, int lowFreqCutoff) : base("Direct")
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
            if (state.Context.Context != IOContext.Context.MERGE)
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
            internal readonly IDictionary<string, DirectField> fields = new SortedDictionary<string, DirectField>();

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public DirectFields(index.SegmentReadState state, index.Fields fields, int minSkipCount, int lowFreqCutoff) throws java.io.IOException
            public DirectFields(SegmentReadState state, Fields fields, int minSkipCount, int lowFreqCutoff)
            {
                foreach (string field in fields)
                {
                    this.fields[field] = new DirectField(state, field, fields.terms(field), minSkipCount, lowFreqCutoff);
                }
            }

            public override IEnumerator<string> iterator()
            {
                return Collections.unmodifiableSet(fields.Keys).GetEnumerator();
            }

            public override Terms terms(string field)
            {
                return fields[field];
            }

            public override int size()
            {
                return fields.Count;
            }

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

            public override void close()
            {
            }

            public override long ramBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (KeyValuePair<string, DirectField> entry in fields.SetOfKeyValuePairs())
                {
                    sizeInBytes += entry.Key.length()*RamUsageEstimator.NUM_BYTES_CHAR;
                    sizeInBytes += entry.Value.ramBytesUsed();
                }
                return sizeInBytes;
            }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void checkIntegrity() throws java.io.IOException
            public override void checkIntegrity()
            {
                // if we read entirely into ram, we already validated.
                // otherwise returned the raw postings reader
            }
        }

        private sealed class DirectField : Terms
        {

            private abstract class TermAndSkip
            {
                public int[] skips;

                /// <summary>
                /// Returns the approximate number of RAM bytes used </summary>
                public abstract long ramBytesUsed();
            }

            private sealed class LowFreqTerm : TermAndSkip
            {
                public readonly int[] postings;
                public readonly sbyte[] payloads;
                public readonly int docFreq;
                public readonly int totalTermFreq;

                public LowFreqTerm(int[] postings, sbyte[] payloads, int docFreq, int totalTermFreq)
                {
                    this.postings = postings;
                    this.payloads = payloads;
                    this.docFreq = docFreq;
                    this.totalTermFreq = totalTermFreq;
                }

                public override long ramBytesUsed()
                {
                    return ((postings != null) ? RamUsageEstimator.sizeOf(postings) : 0) +
                           ((payloads != null) ? RamUsageEstimator.sizeOf(payloads) : 0);
                }
            }

            // TODO: maybe specialize into prx/no-prx/no-frq cases?
            private sealed class HighFreqTerm : TermAndSkip
            {
                public readonly long totalTermFreq;
                public readonly int[] docIDs;
                public readonly int[] freqs;
                public readonly int[][] positions;
                public readonly sbyte[][][] payloads;

                public HighFreqTerm(int[] docIDs, int[] freqs, int[][] positions, sbyte[][][] payloads,
                    long totalTermFreq)
                {
                    this.docIDs = docIDs;
                    this.freqs = freqs;
                    this.positions = positions;
                    this.payloads = payloads;
                    this.totalTermFreq = totalTermFreq;
                }

                public override long ramBytesUsed()
                {
                    long sizeInBytes = 0;
                    sizeInBytes += (docIDs != null) ? RamUsageEstimator.sizeOf(docIDs) : 0;
                    sizeInBytes += (freqs != null) ? RamUsageEstimator.sizeOf(freqs) : 0;

                    if (positions != null)
                    {
                        foreach (int[] position in positions)
                        {
                            sizeInBytes += (position != null) ? RamUsageEstimator.sizeOf(position) : 0;
                        }
                    }

                    if (payloads != null)
                    {
                        foreach (sbyte[][] payload in payloads)
                        {
                            if (payload != null)
                            {
                                foreach (sbyte[] pload in payload)
                                {
                                    sizeInBytes += (pload != null) ? RamUsageEstimator.sizeOf(pload) : 0;
                                }
                            }
                        }
                    }

                    return sizeInBytes;
                }
            }

            internal readonly sbyte[] termBytes;
            internal readonly int[] termOffsets;

            internal readonly int[] skips;
            internal readonly int[] skipOffsets;

            internal readonly TermAndSkip[] terms;
            internal readonly bool hasFreq;
            internal readonly bool hasPos;
            internal readonly bool hasOffsets_Renamed;
            internal readonly bool hasPayloads_Renamed;
            internal readonly long sumTotalTermFreq;
            internal readonly int docCount;
            internal readonly long sumDocFreq;
            internal int skipCount;

            // TODO: maybe make a separate builder?  These are only
            // used during load:
            internal int count;
            internal int[] sameCounts = new int[10];
            internal readonly int minSkipCount;

            private sealed class IntArrayWriter
            {
                internal int[] ints = new int[10];
                internal int upto;

                public void add(int value)
                {
                    if (ints.Length == upto)
                    {
                        ints = ArrayUtil.grow(ints);
                    }
                    ints[upto++] = value;
                }

                public int[] get()
                {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] arr = new int[upto];
                    int[] arr = new int[upto];
                    Array.Copy(ints, 0, arr, 0, upto);
                    upto = 0;
                    return arr;
                }
            }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public DirectField(index.SegmentReadState state, String field, index.Terms termsIn, int minSkipCount, int lowFreqCutoff) throws java.io.IOException
            public DirectField(SegmentReadState state, string field, Terms termsIn, int minSkipCount, int lowFreqCutoff)
            {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final index.FieldInfo fieldInfo = state.fieldInfos.fieldInfo(field);
                FieldInfo fieldInfo = state.fieldInfos.fieldInfo(field);

                sumTotalTermFreq = termsIn.SumTotalTermFreq;
                sumDocFreq = termsIn.SumDocFreq;
                docCount = termsIn.DocCount;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numTerms = (int) termsIn.size();
                int numTerms = (int) termsIn.size();
                if (numTerms == -1)
                {
                    throw new System.ArgumentException("codec does not provide Terms.size()");
                }
                terms = new TermAndSkip[numTerms];
                termOffsets = new int[1 + numTerms];

                sbyte[] termBytes = new sbyte[1024];

                this.minSkipCount = minSkipCount;

                hasFreq = fieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_ONLY) > 0;
                hasPos = fieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS) > 0;
                hasOffsets_Renamed = fieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) > 0;
                hasPayloads_Renamed = fieldInfo.hasPayloads();

                BytesRef term;
                DocsEnum docsEnum = null;
                DocsAndPositionsEnum docsAndPositionsEnum = null;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final index.TermsEnum termsEnum = termsIn.iterator(null);
                TermsEnum termsEnum = termsIn.iterator(null);
                int termOffset = 0;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final IntArrayWriter scratch = new IntArrayWriter();
                IntArrayWriter scratch = new IntArrayWriter();

                // Used for payloads, if any:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.RAMOutputStream ros = new store.RAMOutputStream();
                RAMOutputStream ros = new RAMOutputStream();

                // if (DEBUG) {
                //   System.out.println("\nLOAD terms seg=" + state.segmentInfo.name + " field=" + field + " hasOffsets=" + hasOffsets + " hasFreq=" + hasFreq + " hasPos=" + hasPos + " hasPayloads=" + hasPayloads);
                // }

                while ((term = termsEnum.next()) != null)
                {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int docFreq = termsEnum.docFreq();
                    int docFreq = termsEnum.docFreq();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long totalTermFreq = termsEnum.totalTermFreq();
                    long totalTermFreq = termsEnum.totalTermFreq();

                    // if (DEBUG) {
                    //   System.out.println("  term=" + term.utf8ToString());
                    // }

                    termOffsets[count] = termOffset;

                    if (termBytes.Length < (termOffset + term.length))
                    {
                        termBytes = ArrayUtil.grow(termBytes, termOffset + term.length);
                    }
                    Array.Copy(term.bytes, term.offset, termBytes, termOffset, term.length);
                    termOffset += term.length;
                    termOffsets[count + 1] = termOffset;

                    if (hasPos)
                    {
                        docsAndPositionsEnum = termsEnum.docsAndPositions(null, docsAndPositionsEnum);
                    }
                    else
                    {
                        docsEnum = termsEnum.docs(null, docsEnum);
                    }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TermAndSkip ent;
                    TermAndSkip ent;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final index.DocsEnum docsEnum2;
                    DocsEnum docsEnum2;
                    if (hasPos)
                    {
                        docsEnum2 = docsAndPositionsEnum;
                    }
                    else
                    {
                        docsEnum2 = docsEnum;
                    }

                    int docID;

                    if (docFreq <= lowFreqCutoff)
                    {

                        ros.reset();

                        // Pack postings for low-freq terms into a single int[]:
                        while ((docID = docsEnum2.nextDoc()) != DocsEnum.NO_MORE_DOCS)
                        {
                            scratch.add(docID);
                            if (hasFreq)
                            {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int freq = docsEnum2.freq();
                                int freq = docsEnum2.freq();
                                scratch.add(freq);
                                if (hasPos)
                                {
                                    for (int pos = 0; pos < freq; pos++)
                                    {
                                        scratch.add(docsAndPositionsEnum.nextPosition());
                                        if (hasOffsets_Renamed)
                                        {
                                            scratch.add(docsAndPositionsEnum.startOffset());
                                            scratch.add(docsAndPositionsEnum.endOffset());
                                        }
                                        if (hasPayloads_Renamed)
                                        {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.BytesRef payload = docsAndPositionsEnum.getPayload();
                                            BytesRef payload = docsAndPositionsEnum.Payload;
                                            if (payload != null)
                                            {
                                                scratch.add(payload.length);
                                                ros.writeBytes(payload.bytes, payload.offset, payload.length);
                                            }
                                            else
                                            {
                                                scratch.add(0);
                                            }
                                        }
                                    }
                                }
                            }
                        }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] payloads;
                        sbyte[] payloads;
                        if (hasPayloads_Renamed)
                        {
                            ros.flush();
                            payloads = new sbyte[(int) ros.length()];
                            ros.writeTo(payloads, 0);
                        }
                        else
                        {
                            payloads = null;
                        }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] postings = scratch.get();
                        int[] postings = scratch.get();

                        ent = new LowFreqTerm(postings, payloads, docFreq, (int) totalTermFreq);
                    }
                    else
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] docs = new int[docFreq];
                        int[] docs = new int[docFreq];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] freqs;
                        int[] freqs;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[][] positions;
                        int[][] positions;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[][][] payloads;
                        sbyte[][][] payloads;
                        if (hasFreq)
                        {
                            freqs = new int[docFreq];
                            if (hasPos)
                            {
                                positions = new int[docFreq][];
                                if (hasPayloads_Renamed)
                                {
                                    payloads = new sbyte[docFreq][][];
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
                        while ((docID = docsEnum2.nextDoc()) != DocsEnum.NO_MORE_DOCS)
                        {
                            docs[upto] = docID;
                            if (hasFreq)
                            {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int freq = docsEnum2.freq();
                                int freq = docsEnum2.freq();
                                freqs[upto] = freq;
                                if (hasPos)
                                {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int mult;
                                    int mult;
                                    if (hasOffsets_Renamed)
                                    {
                                        mult = 3;
                                    }
                                    else
                                    {
                                        mult = 1;
                                    }
                                    if (hasPayloads_Renamed)
                                    {
                                        payloads[upto] = new sbyte[freq][];
                                    }
                                    positions[upto] = new int[mult*freq];
                                    int posUpto = 0;
                                    for (int pos = 0; pos < freq; pos++)
                                    {
                                        positions[upto][posUpto] = docsAndPositionsEnum.nextPosition();
                                        if (hasPayloads_Renamed)
                                        {
                                            BytesRef payload = docsAndPositionsEnum.Payload;
                                            if (payload != null)
                                            {
                                                sbyte[] payloadBytes = new sbyte[payload.length];
                                                Array.Copy(payload.bytes, payload.offset, payloadBytes, 0,
                                                    payload.length);
                                                payloads[upto][pos] = payloadBytes;
                                            }
                                        }
                                        posUpto++;
                                        if (hasOffsets_Renamed)
                                        {
                                            positions[upto][posUpto++] = docsAndPositionsEnum.startOffset();
                                            positions[upto][posUpto++] = docsAndPositionsEnum.endOffset();
                                        }
                                    }
                                }
                            }

                            upto++;
                        }
                        Debug.Assert(upto == docFreq);
                        ent = new HighFreqTerm(docs, freqs, positions, payloads, totalTermFreq);
                    }

                    terms[count] = ent;
                    setSkips(count, termBytes);
                    count++;
                }

                // End sentinel:
                termOffsets[count] = termOffset;

                finishSkips();

                //System.out.println(skipCount + " skips: " + field);

                this.termBytes = new sbyte[termOffset];
                Array.Copy(termBytes, 0, this.termBytes, 0, termOffset);

                // Pack skips:
                this.skips = new int[skipCount];
                this.skipOffsets = new int[1 + numTerms];

                int skipOffset = 0;
                for (int i = 0; i < numTerms; i++)
                {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] termSkips = terms[i].skips;
                    int[] termSkips = terms[i].skips;
                    skipOffsets[i] = skipOffset;
                    if (termSkips != null)
                    {
                        Array.Copy(termSkips, 0, skips, skipOffset, termSkips.Length);
                        skipOffset += termSkips.Length;
                        terms[i].skips = null;
                    }
                }
                this.skipOffsets[numTerms] = skipOffset;
                Debug.Assert(skipOffset == skipCount);
            }

            /// <summary>
            /// Returns approximate RAM bytes used </summary>
            public long ramBytesUsed()
            {
                long sizeInBytes = 0;
                sizeInBytes += ((termBytes != null) ? RamUsageEstimator.sizeOf(termBytes) : 0);
                sizeInBytes += ((termOffsets != null) ? RamUsageEstimator.sizeOf(termOffsets) : 0);
                sizeInBytes += ((skips != null) ? RamUsageEstimator.sizeOf(skips) : 0);
                sizeInBytes += ((skipOffsets != null) ? RamUsageEstimator.sizeOf(skipOffsets) : 0);
                sizeInBytes += ((sameCounts != null) ? RamUsageEstimator.sizeOf(sameCounts) : 0);

                if (terms != null)
                {
                    foreach (TermAndSkip termAndSkip in terms)
                    {
                        sizeInBytes += (termAndSkip != null) ? termAndSkip.ramBytesUsed() : 0;
                    }
                }

                return sizeInBytes;
            }

            // Compares in unicode (UTF8) order:
            internal int compare(int ord, BytesRef other)
            {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] otherBytes = other.bytes;
                sbyte[] otherBytes = other.bytes;

                int upto = termOffsets[ord];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termLen = termOffsets[1+ord] - upto;
                int termLen = termOffsets[1 + ord] - upto;
                int otherUpto = other.offset;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int stop = upto + Math.min(termLen, other.length);
                int stop = upto + Math.Min(termLen, other.length);
                while (upto < stop)
                {
                    int diff = (termBytes[upto++] & 0xFF) - (otherBytes[otherUpto++] & 0xFF);
                    if (diff != 0)
                    {
                        return diff;
                    }
                }

                // One is a prefix of the other, or, they are equal:
                return termLen - other.length;
            }

            internal void setSkips(int termOrd, sbyte[] termBytes)
            {

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termLength = termOffsets[termOrd+1] - termOffsets[termOrd];
                int termLength = termOffsets[termOrd + 1] - termOffsets[termOrd];

                if (sameCounts.Length < termLength)
                {
                    sameCounts = ArrayUtil.grow(sameCounts, termLength);
                }

                // Update skip pointers:
                if (termOrd > 0)
                {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int lastTermLength = termOffsets[termOrd] - termOffsets[termOrd-1];
                    int lastTermLength = termOffsets[termOrd] - termOffsets[termOrd - 1];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int limit = Math.min(termLength, lastTermLength);
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
                                    saveSkip(termOrd, sameCounts[i]);
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
                            saveSkip(termOrd, sameCounts[i]);
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

            internal void finishSkips()
            {
                Debug.Assert(count == terms.Length);
                int lastTermOffset = termOffsets[count - 1];
                int lastTermLength = termOffsets[count] - lastTermOffset;

                for (int i = 0; i < lastTermLength; i++)
                {
                    if (sameCounts[i] >= minSkipCount)
                    {
                        // Go back and add a skip pointer:
                        saveSkip(count, sameCounts[i]);
                    }
                }

                // Reverse the skip pointers so they are "nested":
                for (int termID = 0; termID < terms.Length; termID++)
                {
                    TermAndSkip term = terms[termID];
                    if (term.skips != null && term.skips.Length > 1)
                    {
                        for (int pos = 0; pos < term.skips.Length/2; pos++)
                        {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int otherPos = term.skips.length-pos-1;
                            int otherPos = term.skips.Length - pos - 1;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int temp = term.skips[pos];
                            int temp = term.skips[pos];
                            term.skips[pos] = term.skips[otherPos];
                            term.skips[otherPos] = temp;
                        }
                    }
                }
            }

            internal void saveSkip(int ord, int backCount)
            {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TermAndSkip term = terms[ord - backCount];
                TermAndSkip term = terms[ord - backCount];
                skipCount++;
                if (term.skips == null)
                {
                    term.skips = new int[] {ord};
                }
                else
                {
                    // Normally we'd grow at a slight exponential... but
                    // given that the skips themselves are already log(N)
                    // we can grow by only 1 and still have amortized
                    // linear time:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] newSkips = new int[term.skips.length+1];
                    int[] newSkips = new int[term.skips.Length + 1];
                    Array.Copy(term.skips, 0, newSkips, 0, term.skips.Length);
                    term.skips = newSkips;
                    term.skips[term.skips.Length - 1] = ord;
                }
            }

            public override TermsEnum iterator(TermsEnum reuse)
            {
                DirectTermsEnum termsEnum;
                if (reuse != null && reuse is DirectTermsEnum)
                {
                    termsEnum = (DirectTermsEnum) reuse;
                    if (!termsEnum.canReuse(terms))
                    {
                        termsEnum = new DirectTermsEnum(this);
                    }
                }
                else
                {
                    termsEnum = new DirectTermsEnum(this);
                }
                termsEnum.reset();
                return termsEnum;
            }

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: @Override public index.TermsEnum intersect(util.automaton.CompiledAutomaton compiled, final util.BytesRef startTerm)
            public override TermsEnum intersect(CompiledAutomaton compiled, BytesRef startTerm)
            {
                return new DirectIntersectTermsEnum(this, compiled, startTerm);
            }

            public override long size()
            {
                return terms.Length;
            }

            public override long SumTotalTermFreq
            {
                get { return sumTotalTermFreq; }
            }

            public override long SumDocFreq
            {
                get { return sumDocFreq; }
            }

            public override int DocCount
            {
                get { return docCount; }
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparator; }
            }

            public override bool hasFreqs()
            {
                return hasFreq;
            }

            public override bool hasOffsets()
            {
                return hasOffsets_Renamed;
            }

            public override bool hasPositions()
            {
                return hasPos;
            }

            public override bool hasPayloads()
            {
                return hasPayloads_Renamed;
            }

            private sealed class DirectTermsEnum : TermsEnum
            {
                private readonly DirectPostingsFormat.DirectField outerInstance;

                public DirectTermsEnum(DirectPostingsFormat.DirectField outerInstance)
                {
                    this.outerInstance = outerInstance;
                }


                internal readonly BytesRef scratch = new BytesRef();
                internal int termOrd;

                internal bool canReuse(TermAndSkip[] other)
                {
                    return outerInstance.terms == other;
                }

                internal BytesRef setTerm()
                {
                    scratch.bytes = outerInstance.termBytes;
                    scratch.offset = outerInstance.termOffsets[termOrd];
                    scratch.length = outerInstance.termOffsets[termOrd + 1] - outerInstance.termOffsets[termOrd];
                    return scratch;
                }

                public void reset()
                {
                    termOrd = -1;
                }

                public override IComparer<BytesRef> Comparator
                {
                    get { return BytesRef.UTF8SortedAsUnicodeComparator; }
                }

                public override BytesRef next()
                {
                    termOrd++;
                    if (termOrd < outerInstance.terms.Length)
                    {
                        return setTerm();
                    }
                    else
                    {
                        return null;
                    }
                }

                public override TermState termState()
                {
                    OrdTermState state = new OrdTermState();
                    state.ord = termOrd;
                    return state;
                }

                // If non-negative, exact match; else, -ord-1, where ord
                // is where you would insert the term.
                internal int findTerm(BytesRef term)
                {

                    // Just do binary search: should be (constant factor)
                    // faster than using the skip list:
                    int low = 0;
                    int high = outerInstance.terms.Length - 1;

                    while (low <= high)
                    {
                        int mid = (int) ((uint) (low + high) >> 1);
                        int cmp = outerInstance.compare(mid, term);
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

                public override SeekStatus seekCeil(BytesRef term)
                {
                    // TODO: we should use the skip pointers; should be
                    // faster than bin search; we should also hold
                    // & reuse current state so seeking forwards is
                    // faster
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ord = findTerm(term);
                    int ord = findTerm(term);
                    // if (DEBUG) {
                    //   System.out.println("  find term=" + term.utf8ToString() + " ord=" + ord);
                    // }
                    if (ord >= 0)
                    {
                        termOrd = ord;
                        setTerm();
                        return SeekStatus.FOUND;
                    }
                    else if (ord == -outerInstance.terms.Length - 1)
                    {
                        return SeekStatus.END;
                    }
                    else
                    {
                        termOrd = -ord - 1;
                        setTerm();
                        return SeekStatus.NOT_FOUND;
                    }
                }

                public override bool seekExact(BytesRef term)
                {
                    // TODO: we should use the skip pointers; should be
                    // faster than bin search; we should also hold
                    // & reuse current state so seeking forwards is
                    // faster
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ord = findTerm(term);
                    int ord = findTerm(term);
                    if (ord >= 0)
                    {
                        termOrd = ord;
                        setTerm();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public override void seekExact(long ord)
                {
                    termOrd = (int) ord;
                    setTerm();
                }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void seekExact(util.BytesRef term, index.TermState state) throws java.io.IOException
                public override void seekExact(BytesRef term, TermState state)
                {
                    termOrd = (int) ((OrdTermState) state).ord;
                    setTerm();
                    Debug.Assert(term.Equals(scratch));
                }

                public override BytesRef term()
                {
                    return scratch;
                }

                public override long ord()
                {
                    return termOrd;
                }

                public override int docFreq()
                {
                    if (outerInstance.terms[termOrd] is LowFreqTerm)
                    {
                        return ((LowFreqTerm) outerInstance.terms[termOrd]).docFreq;
                    }
                    else
                    {
                        return ((HighFreqTerm) outerInstance.terms[termOrd]).docIDs.Length;
                    }
                }

                public override long totalTermFreq()
                {
                    if (outerInstance.terms[termOrd] is LowFreqTerm)
                    {
                        return ((LowFreqTerm) outerInstance.terms[termOrd]).totalTermFreq;
                    }
                    else
                    {
                        return ((HighFreqTerm) outerInstance.terms[termOrd]).totalTermFreq;
                    }
                }

                public override DocsEnum docs(Bits liveDocs, DocsEnum reuse, int flags)
                {
                    // TODO: implement reuse, something like Pulsing:
                    // it's hairy!

                    if (outerInstance.terms[termOrd] is LowFreqTerm)
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] postings = ((LowFreqTerm) terms[termOrd]).postings;
                        int[] postings = ((LowFreqTerm) outerInstance.terms[termOrd]).postings;
                        if (outerInstance.hasFreq)
                        {
                            if (outerInstance.hasPos)
                            {
                                int posLen;
                                if (outerInstance.hasOffsets_Renamed)
                                {
                                    posLen = 3;
                                }
                                else
                                {
                                    posLen = 1;
                                }
                                if (outerInstance.hasPayloads_Renamed)
                                {
                                    posLen++;
                                }
                                LowFreqDocsEnum docsEnum;
                                if (reuse is LowFreqDocsEnum)
                                {
                                    docsEnum = (LowFreqDocsEnum) reuse;
                                    if (!docsEnum.canReuse(liveDocs, posLen))
                                    {
                                        docsEnum = new LowFreqDocsEnum(liveDocs, posLen);
                                    }
                                }
                                else
                                {
                                    docsEnum = new LowFreqDocsEnum(liveDocs, posLen);
                                }

                                return docsEnum.reset(postings);
                            }
                            else
                            {
                                LowFreqDocsEnumNoPos docsEnum;
                                if (reuse is LowFreqDocsEnumNoPos)
                                {
                                    docsEnum = (LowFreqDocsEnumNoPos) reuse;
                                    if (!docsEnum.canReuse(liveDocs))
                                    {
                                        docsEnum = new LowFreqDocsEnumNoPos(liveDocs);
                                    }
                                }
                                else
                                {
                                    docsEnum = new LowFreqDocsEnumNoPos(liveDocs);
                                }

                                return docsEnum.reset(postings);
                            }
                        }
                        else
                        {
                            LowFreqDocsEnumNoTF docsEnum;
                            if (reuse is LowFreqDocsEnumNoTF)
                            {
                                docsEnum = (LowFreqDocsEnumNoTF) reuse;
                                if (!docsEnum.canReuse(liveDocs))
                                {
                                    docsEnum = new LowFreqDocsEnumNoTF(liveDocs);
                                }
                            }
                            else
                            {
                                docsEnum = new LowFreqDocsEnumNoTF(liveDocs);
                            }

                            return docsEnum.reset(postings);
                        }
                    }
                    else
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final HighFreqTerm term = (HighFreqTerm) terms[termOrd];
                        HighFreqTerm term = (HighFreqTerm) outerInstance.terms[termOrd];

                        HighFreqDocsEnum docsEnum;
                        if (reuse is HighFreqDocsEnum)
                        {
                            docsEnum = (HighFreqDocsEnum) reuse;
                            if (!docsEnum.canReuse(liveDocs))
                            {
                                docsEnum = new HighFreqDocsEnum(liveDocs);
                            }
                        }
                        else
                        {
                            docsEnum = new HighFreqDocsEnum(liveDocs);
                        }

                        //System.out.println("  DE for term=" + new BytesRef(terms[termOrd].term).utf8ToString() + ": " + term.docIDs.length + " docs");
                        return docsEnum.reset(term.docIDs, term.freqs);
                    }
                }

                public override DocsAndPositionsEnum docsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse,
                    int flags)
                {
                    if (!outerInstance.hasPos)
                    {
                        return null;
                    }

                    // TODO: implement reuse, something like Pulsing:
                    // it's hairy!

                    if (outerInstance.terms[termOrd] is LowFreqTerm)
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final LowFreqTerm term = ((LowFreqTerm) terms[termOrd]);
                        LowFreqTerm term = ((LowFreqTerm) outerInstance.terms[termOrd]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] postings = term.postings;
                        int[] postings = term.postings;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] payloads = term.payloads;
                        sbyte[] payloads = term.payloads;
                        return
                            (new LowFreqDocsAndPositionsEnum(liveDocs, outerInstance.hasOffsets_Renamed,
                                outerInstance.hasPayloads_Renamed)).reset(postings, payloads);
                    }
                    else
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final HighFreqTerm term = (HighFreqTerm) terms[termOrd];
                        HighFreqTerm term = (HighFreqTerm) outerInstance.terms[termOrd];
                        return
                            (new HighFreqDocsAndPositionsEnum(liveDocs, outerInstance.hasOffsets_Renamed)).Reset(
                                term.docIDs, term.freqs, term.positions, term.payloads);
                    }
                }
            }

            private sealed class DirectIntersectTermsEnum : TermsEnum
            {
                private readonly DirectPostingsFormat.DirectField outerInstance;

                internal readonly RunAutomaton runAutomaton;
                internal readonly CompiledAutomaton compiledAutomaton;
                internal int termOrd;
                internal readonly BytesRef scratch = new BytesRef();

                private sealed class State
                {
                    private readonly DirectPostingsFormat.DirectField.DirectIntersectTermsEnum outerInstance;

                    public State(DirectPostingsFormat.DirectField.DirectIntersectTermsEnum outerInstance)
                    {
                        this.outerInstance = outerInstance;
                    }

                    internal int changeOrd;
                    internal int state;
                    internal Transition[] transitions;
                    internal int transitionUpto;
                    internal int transitionMax;
                    internal int transitionMin;
                }

                internal State[] states;
                internal int stateUpto;

                public DirectIntersectTermsEnum(DirectPostingsFormat.DirectField outerInstance,
                    CompiledAutomaton compiled, BytesRef startTerm)
                {
                    this.outerInstance = outerInstance;
                    runAutomaton = compiled.runAutomaton;
                    compiledAutomaton = compiled;
                    termOrd = -1;
                    states = new State[1];
                    states[0] = new State(this);
                    states[0].changeOrd = outerInstance.terms.Length;
                    states[0].state = runAutomaton.InitialState;
                    states[0].transitions = compiledAutomaton.sortedTransitions[states[0].state];
                    states[0].transitionUpto = -1;
                    states[0].transitionMax = -1;

                    //System.out.println("IE.init startTerm=" + startTerm);

                    if (startTerm != null)
                    {
                        int skipUpto = 0;
                        if (startTerm.length == 0)
                        {
                            if (outerInstance.terms.Length > 0 && outerInstance.termOffsets[1] == 0)
                            {
                                termOrd = 0;
                            }
                        }
                        else
                        {
                            termOrd++;

                            for (int i = 0; i < startTerm.length; i++)
                            {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int label = startTerm.bytes[startTerm.offset+i] & 0xFF;
                                int label = startTerm.bytes[startTerm.offset + i] & 0xFF;

                                while (label > states[i].transitionMax)
                                {
                                    states[i].transitionUpto++;
                                    Debug.Assert(states[i].transitionUpto < states[i].transitions.Length);
                                    states[i].transitionMin = states[i].transitions[states[i].transitionUpto].Min;
                                    states[i].transitionMax = states[i].transitions[states[i].transitionUpto].Max;
                                    Debug.Assert(states[i].transitionMin >= 0);
                                    Debug.Assert(states[i].transitionMin <= 255);
                                    Debug.Assert(states[i].transitionMax >= 0);
                                    Debug.Assert(states[i].transitionMax <= 255);
                                }

                                // Skip forwards until we find a term matching
                                // the label at this position:
                                while (termOrd < outerInstance.terms.Length)
                                {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int skipOffset = skipOffsets[termOrd];
                                    int skipOffset = outerInstance.skipOffsets[termOrd];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numSkips = skipOffsets[termOrd+1] - skipOffset;
                                    int numSkips = outerInstance.skipOffsets[termOrd + 1] - skipOffset;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termOffset = termOffsets[termOrd];
                                    int termOffset = outerInstance.termOffsets[termOrd];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termLength = termOffsets[1+termOrd] - termOffset;
                                    int termLength = outerInstance.termOffsets[1 + termOrd] - termOffset;

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
                                    else if (label < (outerInstance.termBytes[termOffset + i] & 0xFF))
                                    {
                                        termOrd--;
                                        // if (DEBUG) {
                                        //   System.out.println("  no match; already beyond; return termOrd=" + termOrd);
                                        // }
                                        stateUpto -= skipUpto;
                                        Debug.Assert(stateUpto >= 0);
                                        return;
                                    }
                                    else if (label == (outerInstance.termBytes[termOffset + i] & 0xFF))
                                    {
                                        // if (DEBUG) {
                                        //   System.out.println("    label[" + i + "] matches");
                                        // }
                                        if (skipUpto < numSkips)
                                        {
                                            grow();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int nextState = runAutomaton.step(states[stateUpto].state, label);
                                            int nextState = runAutomaton.step(states[stateUpto].state, label);

                                            // Automaton is required to accept startTerm:
                                            Debug.Assert(nextState != -1);

                                            stateUpto++;
                                            states[stateUpto].changeOrd = outerInstance.skips[skipOffset + skipUpto++];
                                            states[stateUpto].state = nextState;
                                            states[stateUpto].transitions =
                                                compiledAutomaton.sortedTransitions[nextState];
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
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startTermOrd = termOrd;
                                            int startTermOrd = termOrd;
                                            while (termOrd < outerInstance.terms.Length &&
                                                   outerInstance.compare(termOrd, startTerm) <= 0)
                                            {
                                                Debug.Assert(termOrd == startTermOrd ||
                                                             outerInstance.skipOffsets[termOrd] ==
                                                             outerInstance.skipOffsets[termOrd + 1]);
                                                termOrd++;
                                            }
                                            Debug.Assert(termOrd - startTermOrd < outerInstance.minSkipCount);
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
                                nextLabelContinue:
                                ;
                            }
                            nextLabelBreak:
                            ;
                        }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termOffset = termOffsets[termOrd];
                        int termOffset = outerInstance.termOffsets[termOrd];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termLen = termOffsets[1+termOrd] - termOffset;
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

                public override IComparer<BytesRef> Comparator
                {
                    get { return BytesRef.UTF8SortedAsUnicodeComparator; }
                }

                internal void grow()
                {
                    if (states.Length == 1 + stateUpto)
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State[] newStates = new State[states.length+1];
                        State[] newStates = new State[states.Length + 1];
                        Array.Copy(states, 0, newStates, 0, states.Length);
                        newStates[states.Length] = new State(this);
                        states = newStates;
                    }
                }

                public override BytesRef next()
                {
                    // if (DEBUG) {
                    //   System.out.println("\nIE.next");
                    // }

                    termOrd++;
                    int skipUpto = 0;

                    if (termOrd == 0 && outerInstance.termOffsets[1] == 0)
                    {
                        // Special-case empty string:
                        Debug.Assert(stateUpto == 0);
                        // if (DEBUG) {
                        //   System.out.println("  visit empty string");
                        // }
                        if (runAutomaton.isAccept(states[0].state))
                        {
                            scratch.bytes = outerInstance.termBytes;
                            scratch.offset = 0;
                            scratch.length = 0;
                            return scratch;
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
                            return null;
                        }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State state = states[stateUpto];
                        State state = states[stateUpto];
                        if (termOrd == state.changeOrd)
                        {
                            // Pop:
                            // if (DEBUG) {
                            //   System.out.println("  pop stateUpto=" + stateUpto);
                            // }
                            stateUpto--;
                            /*
				if (DEBUG) {
				  try {
				    //System.out.println("    prefix pop " + new BytesRef(terms[termOrd].term, 0, Math.min(stateUpto, terms[termOrd].term.length)).utf8ToString());
				    System.out.println("    prefix pop " + new BytesRef(terms[termOrd].term, 0, Math.min(stateUpto, terms[termOrd].term.length)));
				  } catch (ArrayIndexOutOfBoundsException aioobe) {
				    System.out.println("    prefix pop " + new BytesRef(terms[termOrd].term, 0, Math.min(stateUpto, terms[termOrd].term.length)));
				  }
				}
				*/

                            continue;
                        }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termOffset = termOffsets[termOrd];
                        int termOffset = outerInstance.termOffsets[termOrd];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termLength = termOffsets[termOrd+1] - termOffset;
                        int termLength = outerInstance.termOffsets[termOrd + 1] - termOffset;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int skipOffset = skipOffsets[termOrd];
                        int skipOffset = outerInstance.skipOffsets[termOrd];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numSkips = skipOffsets[termOrd+1] - skipOffset;
                        int numSkips = outerInstance.skipOffsets[termOrd + 1] - skipOffset;

                        // if (DEBUG) {
                        //   System.out.println("  term=" + new BytesRef(termBytes, termOffset, termLength).utf8ToString() + " skips=" + Arrays.toString(skips));
                        // }

                        Debug.Assert(termOrd < state.changeOrd);

                        Debug.Assert(stateUpto <= termLength, "term.length=" + termLength + "; stateUpto=" + stateUpto);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int label = termBytes[termOffset+stateUpto] & 0xFF;
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
                                    return null;
                                }
                                else
                                {
                                    Debug.Assert(state.changeOrd > termOrd);
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
                            Debug.Assert(state.transitionUpto < state.transitions.Length,
                                " state.transitionUpto=" + state.transitionUpto + " vs " + state.transitions.Length);
                            state.transitionMin = state.transitions[state.transitionUpto].Min;
                            state.transitionMax = state.transitions[state.transitionUpto].Max;
                            Debug.Assert(state.transitionMin >= 0);
                            Debug.Assert(state.transitionMin <= 255);
                            Debug.Assert(state.transitionMax >= 0);
                            Debug.Assert(state.transitionMax <= 255);
                        }

                        /*
			  if (DEBUG) {
			    System.out.println("    check ord=" + termOrd + " term[" + stateUpto + "]=" + (char) label + "(" + label + ") term=" + new BytesRef(terms[termOrd].term).utf8ToString() + " trans " +
			                       (char) state.transitionMin + "(" + state.transitionMin + ")" + "-" + (char) state.transitionMax + "(" + state.transitionMax + ") nextChange=+" + (state.changeOrd - termOrd) + " skips=" + (skips == null ? "null" : Arrays.toString(skips)));
			    System.out.println("    check ord=" + termOrd + " term[" + stateUpto + "]=" + Integer.toHexString(label) + "(" + label + ") term=" + new BytesRef(termBytes, termOffset, termLength) + " trans " +
			                       Integer.toHexString(state.transitionMin) + "(" + state.transitionMin + ")" + "-" + Integer.toHexString(state.transitionMax) + "(" + state.transitionMax + ") nextChange=+" + (state.changeOrd - termOrd) + " skips=" + (skips == null ? "null" : Arrays.toString(skips)));
			  }
			  */

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLabel = state.transitionMin;
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
                                int mid = (int) ((uint) (low + high) >> 1);
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

                        int nextState = runAutomaton.step(states[stateUpto].state, label);

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
                            // Push:
                            // if (DEBUG) {
                            //   System.out.println("  push");
                            // }
                            /*
				if (DEBUG) {
				  try {
				    //System.out.println("    prefix push " + new BytesRef(term, 0, stateUpto+1).utf8ToString());
				    System.out.println("    prefix push " + new BytesRef(term, 0, stateUpto+1));
				  } catch (ArrayIndexOutOfBoundsException aioobe) {
				    System.out.println("    prefix push " + new BytesRef(term, 0, stateUpto+1));
				  }
				}
				*/

                            grow();
                            stateUpto++;
                            states[stateUpto].state = nextState;
                            states[stateUpto].changeOrd = outerInstance.skips[skipOffset + skipUpto++];
                            states[stateUpto].transitions = compiledAutomaton.sortedTransitions[nextState];
                            states[stateUpto].transitionUpto = -1;
                            states[stateUpto].transitionMax = -1;

                            if (stateUpto == termLength)
                            {
                                // if (DEBUG) {
                                //   System.out.println("  term ends after push");
                                // }
                                if (runAutomaton.isAccept(nextState))
                                {
                                    // if (DEBUG) {
                                    //   System.out.println("  automaton accepts: return");
                                    // }
                                    scratch.bytes = outerInstance.termBytes;
                                    scratch.offset = outerInstance.termOffsets[termOrd];
                                    scratch.length = outerInstance.termOffsets[1 + termOrd] - scratch.offset;
                                    // if (DEBUG) {
                                    //   System.out.println("  ret " + scratch.utf8ToString());
                                    // }
                                    return scratch;
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

                            if (compiledAutomaton.commonSuffixRef != null)
                            {
                                //System.out.println("suffix " + compiledAutomaton.commonSuffixRef.utf8ToString());
                                Debug.Assert(compiledAutomaton.commonSuffixRef.offset == 0);
                                if (termLength < compiledAutomaton.commonSuffixRef.length)
                                {
                                    termOrd++;
                                    skipUpto = 0;
                                    goto nextTermContinue;
                                }
                                int offset = termOffset + termLength - compiledAutomaton.commonSuffixRef.length;
                                for (int suffix = 0; suffix < compiledAutomaton.commonSuffixRef.length; suffix++)
                                {
                                    if (outerInstance.termBytes[offset + suffix] !=
                                        compiledAutomaton.commonSuffixRef.bytes[suffix])
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
                                nextState = runAutomaton.step(nextState,
                                    outerInstance.termBytes[termOffset + upto] & 0xFF);
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

                            if (runAutomaton.isAccept(nextState))
                            {
                                scratch.bytes = outerInstance.termBytes;
                                scratch.offset = outerInstance.termOffsets[termOrd];
                                scratch.length = outerInstance.termOffsets[1 + termOrd] - scratch.offset;
                                // if (DEBUG) {
                                //   System.out.println("  match tail; return " + scratch.utf8ToString());
                                //   System.out.println("  ret2 " + scratch.utf8ToString());
                                // }
                                return scratch;
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
                        nextTermContinue:
                        ;
                    }
                    nextTermBreak:
                    ;
                }

                public override TermState termState()
                {
                    OrdTermState state = new OrdTermState();
                    state.ord = termOrd;
                    return state;
                }

                public override BytesRef term()
                {
                    return scratch;
                }

                public override long ord()
                {
                    return termOrd;
                }

                public override int docFreq()
                {
                    if (outerInstance.terms[termOrd] is LowFreqTerm)
                    {
                        return ((LowFreqTerm) outerInstance.terms[termOrd]).docFreq;
                    }
                    else
                    {
                        return ((HighFreqTerm) outerInstance.terms[termOrd]).docIDs.Length;
                    }
                }

                public override long totalTermFreq()
                {
                    if (outerInstance.terms[termOrd] is LowFreqTerm)
                    {
                        return ((LowFreqTerm) outerInstance.terms[termOrd]).totalTermFreq;
                    }
                    else
                    {
                        return ((HighFreqTerm) outerInstance.terms[termOrd]).totalTermFreq;
                    }
                }

                public override DocsEnum docs(Bits liveDocs, DocsEnum reuse, int flags)
                {
                    // TODO: implement reuse, something like Pulsing:
                    // it's hairy!

                    if (outerInstance.terms[termOrd] is LowFreqTerm)
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] postings = ((LowFreqTerm) terms[termOrd]).postings;
                        int[] postings = ((LowFreqTerm) outerInstance.terms[termOrd]).postings;
                        if (outerInstance.hasFreq)
                        {
                            if (outerInstance.hasPos)
                            {
                                int posLen;
                                if (outerInstance.hasOffsets_Renamed)
                                {
                                    posLen = 3;
                                }
                                else
                                {
                                    posLen = 1;
                                }
                                if (outerInstance.hasPayloads_Renamed)
                                {
                                    posLen++;
                                }
                                return (new LowFreqDocsEnum(liveDocs, posLen)).reset(postings);
                            }
                            else
                            {
                                return (new LowFreqDocsEnumNoPos(liveDocs)).reset(postings);
                            }
                        }
                        else
                        {
                            return (new LowFreqDocsEnumNoTF(liveDocs)).reset(postings);
                        }
                    }
                    else
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final HighFreqTerm term = (HighFreqTerm) terms[termOrd];
                        HighFreqTerm term = (HighFreqTerm) outerInstance.terms[termOrd];
                        //  System.out.println("DE for term=" + new BytesRef(terms[termOrd].term).utf8ToString() + ": " + term.docIDs.length + " docs");
                        return (new HighFreqDocsEnum(liveDocs)).reset(term.docIDs, term.freqs);
                    }
                }

                public override DocsAndPositionsEnum docsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse,
                    int flags)
                {
                    if (!outerInstance.hasPos)
                    {
                        return null;
                    }

                    // TODO: implement reuse, something like Pulsing:
                    // it's hairy!

                    if (outerInstance.terms[termOrd] is LowFreqTerm)
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final LowFreqTerm term = ((LowFreqTerm) terms[termOrd]);
                        LowFreqTerm term = ((LowFreqTerm) outerInstance.terms[termOrd]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] postings = term.postings;
                        int[] postings = term.postings;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] payloads = term.payloads;
                        sbyte[] payloads = term.payloads;
                        return
                            (new LowFreqDocsAndPositionsEnum(liveDocs, outerInstance.hasOffsets_Renamed,
                                outerInstance.hasPayloads_Renamed)).reset(postings, payloads);
                    }
                    else
                    {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final HighFreqTerm term = (HighFreqTerm) terms[termOrd];
                        HighFreqTerm term = (HighFreqTerm) outerInstance.terms[termOrd];
                        return
                            (new HighFreqDocsAndPositionsEnum(liveDocs, outerInstance.hasOffsets_Renamed)).Reset(
                                term.docIDs, term.freqs, term.positions, term.payloads);
                    }
                }

                public override SeekStatus seekCeil(BytesRef term)
                {
                    throw new System.NotSupportedException();
                }

                public override void seekExact(long ord)
                {
                    throw new System.NotSupportedException();
                }
            }
        }

        // Docs only:
        private sealed class LowFreqDocsEnumNoTF : DocsEnum
        {
            internal int[] postings;
            internal readonly Bits liveDocs;
            internal int upto;

            public LowFreqDocsEnumNoTF(Bits liveDocs)
            {
                this.liveDocs = liveDocs;
            }

            public bool canReuse(Bits liveDocs)
            {
                return liveDocs == this.liveDocs;
            }

            public DocsEnum reset(int[] postings)
            {
                this.postings = postings;
                upto = -1;
                return this;
            }

            // TODO: can do this w/o setting members?

            public override int nextDoc()
            {
                upto++;
                if (liveDocs == null)
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
                        if (liveDocs.get(postings[upto]))
                        {
                            return postings[upto];
                        }
                        upto++;
                    }
                }
                return NO_MORE_DOCS;
            }

            public override int docID()
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

            public override int freq()
            {
                return 1;
            }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
            public override int advance(int target)
            {
                // Linear scan, but this is low-freq term so it won't
                // be costly:
                return slowAdvance(target);
            }

            public override long cost()
            {
                return postings.Length;
            }
        }

        // Docs + freqs:
        private sealed class LowFreqDocsEnumNoPos : DocsEnum
        {
            internal int[] postings;
            internal readonly Bits liveDocs;
            internal int upto;

            public LowFreqDocsEnumNoPos(Bits liveDocs)
            {
                this.liveDocs = liveDocs;
            }

            public bool canReuse(Bits liveDocs)
            {
                return liveDocs == this.liveDocs;
            }

            public DocsEnum reset(int[] postings)
            {
                this.postings = postings;
                upto = -2;
                return this;
            }

            // TODO: can do this w/o setting members?
            public override int nextDoc()
            {
                upto += 2;
                if (liveDocs == null)
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
                        if (liveDocs.get(postings[upto]))
                        {
                            return postings[upto];
                        }
                        upto += 2;
                    }
                }
                return NO_MORE_DOCS;
            }

            public override int docID()
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

            public override int freq()
            {
                return postings[upto + 1];
            }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
            public override int advance(int target)
            {
                // Linear scan, but this is low-freq term so it won't
                // be costly:
                return slowAdvance(target);
            }

            public override long cost()
            {
                return postings.Length/2;
            }
        }

        // Docs + freqs + positions/offets:
        private sealed class LowFreqDocsEnum : DocsEnum
        {
            internal int[] postings;
            internal readonly Bits liveDocs;
            internal readonly int posMult;
            internal int upto;
            internal int freq_Renamed;

            public LowFreqDocsEnum(Bits liveDocs, int posMult)
            {
                this.liveDocs = liveDocs;
                this.posMult = posMult;
                // if (DEBUG) {
                //   System.out.println("LowFreqDE: posMult=" + posMult);
                // }
            }

            public bool canReuse(Bits liveDocs, int posMult)
            {
                return liveDocs == this.liveDocs && posMult == this.posMult;
            }

            public DocsEnum reset(int[] postings)
            {
                this.postings = postings;
                upto = -2;
                freq_Renamed = 0;
                return this;
            }

            // TODO: can do this w/o setting members?
            public override int nextDoc()
            {
                upto += 2 + freq_Renamed*posMult;
                // if (DEBUG) {
                //   System.out.println("  nextDoc freq=" + freq + " upto=" + upto + " vs " + postings.length);
                // }
                if (liveDocs == null)
                {
                    if (upto < postings.Length)
                    {
                        freq_Renamed = postings[upto + 1];
                        Debug.Assert(freq_Renamed > 0);
                        return postings[upto];
                    }
                }
                else
                {
                    while (upto < postings.Length)
                    {
                        freq_Renamed = postings[upto + 1];
                        Debug.Assert(freq_Renamed > 0);
                        if (liveDocs.get(postings[upto]))
                        {
                            return postings[upto];
                        }
                        upto += 2 + freq_Renamed*posMult;
                    }
                }
                return NO_MORE_DOCS;
            }

            public override int docID()
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

            public override int freq()
            {
                // TODO: can I do postings[upto+1]?
                return freq_Renamed;
            }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
            public override int advance(int target)
            {
                // Linear scan, but this is low-freq term so it won't
                // be costly:
                return slowAdvance(target);
            }

            public override long cost()
            {
                // TODO: could do a better estimate
                return postings.Length/2;
            }
        }

        private sealed class LowFreqDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            internal int[] postings;
            internal readonly Bits liveDocs;
            internal readonly int posMult;
            internal readonly bool hasOffsets;
            internal readonly bool hasPayloads;
            internal readonly BytesRef payload = new BytesRef();
            internal int upto;
            internal int docID_Renamed;
            internal int freq_Renamed;
            internal int skipPositions;
            internal int startOffset_Renamed;
            internal int endOffset_Renamed;
            internal int lastPayloadOffset;
            internal int payloadOffset;
            internal int payloadLength;
            internal sbyte[] payloadBytes;

            public LowFreqDocsAndPositionsEnum(Bits liveDocs, bool hasOffsets, bool hasPayloads)
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

            public DocsAndPositionsEnum reset(int[] postings, sbyte[] payloadBytes)
            {
                this.postings = postings;
                upto = 0;
                skipPositions = 0;
                startOffset_Renamed = -1;
                endOffset_Renamed = -1;
                docID_Renamed = -1;
                payloadLength = 0;
                this.payloadBytes = payloadBytes;
                return this;
            }

            public override int nextDoc()
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

                if (liveDocs == null)
                {
                    if (upto < postings.Length)
                    {
                        docID_Renamed = postings[upto++];
                        freq_Renamed = postings[upto++];
                        skipPositions = freq_Renamed;
                        return docID_Renamed;
                    }
                }
                else
                {
                    while (upto < postings.Length)
                    {
                        docID_Renamed = postings[upto++];
                        freq_Renamed = postings[upto++];
                        if (liveDocs.get(docID_Renamed))
                        {
                            skipPositions = freq_Renamed;
                            return docID_Renamed;
                        }
                        if (hasPayloads)
                        {
                            for (int i = 0; i < freq_Renamed; i++)
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
                            upto += posMult*freq_Renamed;
                        }
                    }
                }

                return docID_Renamed = NO_MORE_DOCS;
            }

            public override int docID()
            {
                return docID_Renamed;
            }

            public override int freq()
            {
                return freq_Renamed;
            }

            public override int nextPosition()
            {
                Debug.Assert(skipPositions > 0);
                skipPositions--;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pos = postings[upto++];
                int pos = postings[upto++];
                if (hasOffsets)
                {
                    startOffset_Renamed = postings[upto++];
                    endOffset_Renamed = postings[upto++];
                }
                if (hasPayloads)
                {
                    payloadLength = postings[upto++];
                    lastPayloadOffset = payloadOffset;
                    payloadOffset += payloadLength;
                }
                return pos;
            }

            public override int startOffset()
            {
                return startOffset_Renamed;
            }

            public override int endOffset()
            {
                return endOffset_Renamed;
            }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
            public override int advance(int target)
            {
                return slowAdvance(target);
            }

            public override BytesRef Payload
            {
                get
                {
                    if (payloadLength > 0)
                    {
                        payload.bytes = payloadBytes;
                        payload.offset = lastPayloadOffset;
                        payload.length = payloadLength;
                        return payload;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public override long cost()
            {
                // TODO: could do a better estimate
                return postings.Length/2;
            }
        }

        // Docs + freqs:
        private sealed class HighFreqDocsEnum : DocsEnum
        {
            internal int[] docIDs;
            internal int[] freqs;
            internal readonly Bits liveDocs;
            internal int upto;
            internal int docID_Renamed = -1;

            public HighFreqDocsEnum(Bits liveDocs)
            {
                this.liveDocs = liveDocs;
            }

            public bool canReuse(Bits liveDocs)
            {
                return liveDocs == this.liveDocs;
            }

            public int[] DocIDs
            {
                get { return docIDs; }
            }

            public int[] Freqs
            {
                get { return freqs; }
            }

            public DocsEnum reset(int[] docIDs, int[] freqs)
            {
                this.docIDs = docIDs;
                this.freqs = freqs;
                docID_Renamed = upto = -1;
                return this;
            }

            public override int nextDoc()
            {
                upto++;
                if (liveDocs == null)
                {
                    try
                    {
                        return docID_Renamed = docIDs[upto];
                    }
                    catch (System.IndexOutOfRangeException)
                    {
                    }
                }
                else
                {
                    while (upto < docIDs.Length)
                    {
                        if (liveDocs.get(docIDs[upto]))
                        {
                            return docID_Renamed = docIDs[upto];
                        }
                        upto++;
                    }
                }
                return docID_Renamed = NO_MORE_DOCS;
            }

            public override int docID()
            {
                return docID_Renamed;
            }

            public override int freq()
            {
                if (freqs == null)
                {
                    return 1;
                }
                else
                {
                    return freqs[upto];
                }
            }

            public override int advance(int target)
            {
                /*
		  upto++;
		  if (upto == docIDs.length) {
		    return docID = NO_MORE_DOCS;
		  }
		  final int index = Arrays.binarySearch(docIDs, upto, docIDs.length, target);
		  if (index < 0) {
		    upto = -index - 1;
		  } else {
		    upto = index;
		  }
		  if (liveDocs != null) {
		    while (upto < docIDs.length) {
		      if (liveDocs.get(docIDs[upto])) {
		        break;
		      }
		      upto++;
		    }
		  }
		  if (upto == docIDs.length) {
		    return NO_MORE_DOCS;
		  } else {
		    return docID = docIDs[upto];
		  }
		  */

                //System.out.println("  advance target=" + target + " cur=" + docID() + " upto=" + upto + " of " + docIDs.length);
                // if (DEBUG) {
                //   System.out.println("advance target=" + target + " len=" + docIDs.length);
                // }
                upto++;
                if (upto == docIDs.Length)
                {
                    return docID_Renamed = NO_MORE_DOCS;
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

                    int mid = (int) ((uint) (low + high) >> 1);
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
                        if (liveDocs.get(docIDs[upto]))
                        {
                            break;
                        }
                        upto++;
                    }
                }
                if (upto == docIDs.Length)
                {
                    //System.out.println("    return END");
                    return docID_Renamed = NO_MORE_DOCS;
                }
                else
                {
                    //System.out.println("    return docID=" + docIDs[upto] + " upto=" + upto);
                    return docID_Renamed = docIDs[upto];
                }
            }

            public override long cost()
            {
                return docIDs.Length;
            }
        }

        // TODO: specialize offsets and not
        private sealed class HighFreqDocsAndPositionsEnum : DocsAndPositionsEnum
        {

            private readonly BytesRef _payload = new BytesRef();

            internal int[] docIDs;
            internal int[] freqs;
            internal int[][] positions;
            internal sbyte[][][] payloads;
            internal readonly Bits liveDocs;
            internal readonly bool hasOffsets;
            internal readonly int posJump;
            internal int upto;
            internal int docID_Renamed = -1;
            internal int posUpto;
            internal int[] curPositions;

            public HighFreqDocsAndPositionsEnum(Bits liveDocs, bool hasOffsets)
            {
                this.liveDocs = liveDocs;
                this.hasOffsets = hasOffsets;
                posJump = hasOffsets ? 3 : 1;
            }

            public int[] DocIDs
            {
                get { return docIDs; }
            }

            public int[][] Positions
            {
                get { return positions; }
            }

            public int PosJump
            {
                get { return posJump; }
            }

            public Bits LiveDocs
            {
                get { return liveDocs; }
            }

            public DocsAndPositionsEnum Reset(int[] docIDs, int[] freqs, int[][] positions, sbyte[][][] payloads)
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
                if (liveDocs == null)
                {
                    if (upto < docIDs.Length)
                    {
                        posUpto = -posJump;
                        curPositions = positions[upto];
                        return docID_Renamed = docIDs[upto];
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
                            return docID_Renamed = docIDs[upto];
                        }
                        upto++;
                    }
                }

                return docID_Renamed = NO_MORE_DOCS;
            }

            public override int Freq()
            {
                return freqs[upto];
            }

            public override int DocID()
            {
                return docID_Renamed;
            }

            public override int NextPosition()
            {
                posUpto += posJump;
                return curPositions[posUpto];
            }

            public override int StartOffset()
            {
                if (hasOffsets)
                    return curPositions[posUpto + 1];
                
                return -1;
            }

            public override int EndOffset()
            {
                if (hasOffsets)
                    return curPositions[posUpto + 2];
                
                return -1;
            }

            public override int Advance(int target)
            {
                upto++;
                if (upto == docIDs.Length)
                {
                    return docID_Renamed = NO_MORE_DOCS;
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

                    int mid = (int) ((uint) (low + high) >> 1);
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
                    return docID_Renamed = NO_MORE_DOCS;
                }
                else
                {
                    //System.out.println("    return docID=" + docIDs[upto] + " upto=" + upto);
                    posUpto = -posJump;
                    curPositions = positions[upto];
                    return docID_Renamed = docIDs[upto];
                }
            }

            public override BytesRef Payload
            {
                get
                {
                    if (payloads == null)
                        return null;
                
                    var payloadBytes = payloads[upto][posUpto/(hasOffsets ? 3 : 1)];
                    if (payloadBytes == null)
                    {
                        return null;
                    }
                    _payload.Bytes = payloadBytes;
                    _payload.Length = payloadBytes.Length;
                    _payload.Offset = 0;
                    return _payload;
                }
            }

            public override long Cost()
            {
                return docIDs.Length;
            }

        }
    }
}