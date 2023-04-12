using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.BlockTerms
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
    /// Handles a terms dict, but decouples all details of
    /// doc/freqs/positions reading to an instance of 
    /// <see cref="PostingsReaderBase"/>.  This class is reusable for
    /// codecs that use a different format for
    /// docs/freqs/positions (though codecs are also free to
    /// make their own terms dict impl).
    /// <para/>
    /// This class also interacts with an instance of
    /// <see cref="TermsIndexReaderBase"/>, to abstract away the specific
    /// implementation of the terms dict index. 
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class BlockTermsReader : FieldsProducer
    {
        // Open input to the main terms dict file (_X.tis)
        private readonly IndexInput input;

        // Reads the terms dict entries, to gather state to
        // produce DocsEnum on demand
        private readonly PostingsReaderBase postingsReader;

        private readonly IDictionary<string, FieldReader> fields = new JCG.SortedDictionary<string, FieldReader>(StringComparer.Ordinal);

        // Reads the terms index
        private TermsIndexReaderBase indexReader;

        // keeps the dirStart offset
        private long dirOffset;

        private readonly int version;

        /// <summary>
        /// Used as a key for the terms cache
        /// </summary>
        private class FieldAndTerm : DoubleBarrelLRUCache.CloneableKey
        {
            public string Field { get; set; }
            private BytesRef Term { get; set; }

            public FieldAndTerm()
            {
            }

            private FieldAndTerm(FieldAndTerm other)
            {
                Field = other.Field;
                Term = BytesRef.DeepCopyOf(other.Term);
            }

            public override bool Equals(object other)
            {
                var o = (FieldAndTerm)other;
                return o.Field.Equals(Field, StringComparison.Ordinal) && Term.BytesEquals(o.Term);
            }

            public override object Clone()
            {
                return new FieldAndTerm(this);
            }

            public override int GetHashCode()
            {
                return Field.GetHashCode() * 31 + Term.GetHashCode();
            }
        }

        // private string segment;

        public BlockTermsReader(TermsIndexReaderBase indexReader, Directory dir, FieldInfos fieldInfos, SegmentInfo info,
            PostingsReaderBase postingsReader, IOContext context,
            string segmentSuffix)
        {
            this.postingsReader = postingsReader;

            // this.segment = segment;
            input = dir.OpenInput(IndexFileNames.SegmentFileName(info.Name, segmentSuffix, BlockTermsWriter.TERMS_EXTENSION),
                               context);

            bool success = false;
            try
            {
                version = ReadHeader(input);

                // Have PostingsReader init itself
                postingsReader.Init(input);

                // Read per-field details
                SeekDir(input, dirOffset);

                int numFields = input.ReadVInt32();
                if (numFields < 0)
                {
                    throw new CorruptIndexException("invalid number of fields: " + numFields + " (resource=" + input + ")");
                }
                for (int i = 0; i < numFields; i++)
                {
                    int field = input.ReadVInt32();
                    long numTerms = input.ReadVInt64();
                    if (Debugging.AssertsEnabled) Debugging.Assert(numTerms >= 0);
                    long termsStartPointer = input.ReadVInt64();
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                    long sumTotalTermFreq = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY ? -1 : input.ReadVInt64();
                    long sumDocFreq = input.ReadVInt64();
                    int docCount = input.ReadVInt32();
                    int longsSize = version >= BlockTermsWriter.VERSION_META_ARRAY ? input.ReadVInt32() : 0;
                    if (docCount < 0 || docCount > info.DocCount)
                    { // #docs with field must be <= #docs
                        throw new CorruptIndexException("invalid docCount: " + docCount + " maxDoc: " + info.DocCount + " (resource=" + input + ")");
                    }
                    if (sumDocFreq < docCount)
                    {  // #postings must be >= #docs with field
                        throw new CorruptIndexException("invalid sumDocFreq: " + sumDocFreq + " docCount: " + docCount + " (resource=" + input + ")");
                    }
                    if (sumTotalTermFreq != -1 && sumTotalTermFreq < sumDocFreq)
                    { // #positions must be >= #postings
                        throw new CorruptIndexException("invalid sumTotalTermFreq: " + sumTotalTermFreq + " sumDocFreq: " + sumDocFreq + " (resource=" + input + ")");
                    }
                    FieldReader previous = fields.Put(fieldInfo.Name, new FieldReader(this, fieldInfo, numTerms, termsStartPointer, sumTotalTermFreq, sumDocFreq, docCount, longsSize));
                    if (previous != null)
                    {
                        throw new CorruptIndexException("duplicate fields: " + fieldInfo.Name + " (resource=" + input + ")");
                    }
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    input.Dispose();
                }
            }

            this.indexReader = indexReader;
        }

        private int ReadHeader(DataInput input)
        {
            int version = CodecUtil.CheckHeader(input, BlockTermsWriter.CODEC_NAME,
                          BlockTermsWriter.VERSION_START,
                          BlockTermsWriter.VERSION_CURRENT);
            if (version < BlockTermsWriter.VERSION_APPEND_ONLY)
            {
                dirOffset = input.ReadInt64();
            }
            return version;
        }

        private void SeekDir(IndexInput input, long dirOffset)
        {
            if (version >= BlockTermsWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadInt64();
            }
            else if (version >= BlockTermsWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length - 8);
                dirOffset = input.ReadInt64();
            }
            input.Seek(dirOffset);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    try
                    {
                        if (indexReader != null)
                        {
                            indexReader.Dispose();
                        }
                    }
                    finally
                    {
                        // null so if an app hangs on to us (ie, we are not
                        // GCable, despite being closed) we still free most
                        // ram
                        indexReader = null;
                        if (input != null)
                        {
                            input.Dispose();
                        }
                    }
                }
                finally
                {
                    if (postingsReader != null)
                    {
                        postingsReader.Dispose();
                    }
                }
            }
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return fields.Keys.GetEnumerator(); // LUCENENET NOTE: enumerators are not writable in .NET
        }

        public override Terms GetTerms(string field)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(field != null);

            fields.TryGetValue(field, out FieldReader result);
            return result;
        }

        public override int Count => fields.Count;

        private class FieldReader : Terms
        {
            private readonly BlockTermsReader outerInstance;

            private readonly long numTerms;
            private readonly FieldInfo fieldInfo;
            private readonly long termsStartPointer;
            private readonly long sumTotalTermFreq;
            private readonly long sumDocFreq;
            private readonly int docCount;
            private readonly int longsSize;

            public FieldReader(BlockTermsReader outerInstance, FieldInfo fieldInfo, long numTerms, long termsStartPointer, long sumTotalTermFreq,
                long sumDocFreq, int docCount, int longsSize)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(numTerms > 0);

                this.outerInstance = outerInstance;

                this.fieldInfo = fieldInfo;
                this.numTerms = numTerms;
                this.termsStartPointer = termsStartPointer;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.sumDocFreq = sumDocFreq;
                this.docCount = docCount;
                this.longsSize = longsSize;
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override TermsEnum GetEnumerator()
            {
                return new SegmentTermsEnum(this);
            }

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            public override bool HasFreqs => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;

            public override bool HasOffsets => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

            public override bool HasPositions => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;

            public override bool HasPayloads => fieldInfo.HasPayloads;

            public override long Count => numTerms;

            public override long SumTotalTermFreq => sumTotalTermFreq;

            public override long SumDocFreq => sumDocFreq;

            public override int DocCount => docCount;

            // Iterates through terms in this field
            private class SegmentTermsEnum : TermsEnum
            {
                private readonly FieldReader outerInstance;

                private readonly IndexInput input;
                private readonly BlockTermState state;
                private readonly bool doOrd;
                private readonly FieldAndTerm fieldTerm = new FieldAndTerm();
                private readonly TermsIndexReaderBase.FieldIndexEnum indexEnum;
                private readonly BytesRef term = new BytesRef();

                /* This is true if indexEnum is "still" seek'd to the index term
                 for the current term. We set it to true on seeking, and then it
                 remains valid until next() is called enough times to load another
                 terms block: */
                private bool indexIsCurrent;

                /* True if we've already called .next() on the indexEnum, to "bracket"
                the current block of terms: */
                private bool didIndexNext;

                /* Next index term, bracketing the current block of terms; this is
                only valid if didIndexNext is true: */
                private BytesRef nextIndexTerm;

                /* True after seekExact(TermState), do defer seeking.  If the app then
                calls next() (which is not "typical"), then we'll do the real seek */
                private bool seekPending;

                /* How many blocks we've read since last seek.  Once this
                 is >= indexEnum.getDivisor() we set indexIsCurrent to false (since
                 the index can no long bracket seek-within-block). */
                private int blocksSinceSeek;

                private byte[] termSuffixes;
                private readonly ByteArrayDataInput termSuffixesReader = new ByteArrayDataInput();

                /* Common prefix used for all terms in this block. */
                private int termBlockPrefix;

                /* How many terms in current block */
                private int blockTermCount;

                private byte[] docFreqBytes;
                private readonly ByteArrayDataInput freqReader = new ByteArrayDataInput();
                private int metaDataUpto;

                private readonly long[] longs;
                private byte[] bytes;
                private ByteArrayDataInput bytesReader;

                public SegmentTermsEnum(FieldReader outerInstance)
                {
                    this.outerInstance = outerInstance;

                    input = (IndexInput)outerInstance.outerInstance.input.Clone();
                    input.Seek(outerInstance.termsStartPointer);
                    indexEnum = outerInstance.outerInstance.indexReader.GetFieldEnum(outerInstance.fieldInfo);
                    doOrd = outerInstance.outerInstance.indexReader.SupportsOrd;
                    fieldTerm.Field = outerInstance.fieldInfo.Name;
                    state = outerInstance.outerInstance.postingsReader.NewTermState();
                    state.TotalTermFreq = -1;
                    state.Ord = -1;

                    termSuffixes = new byte[128];
                    docFreqBytes = new byte[64];
                    //System.out.println("BTR.enum init this=" + this + " postingsReader=" + postingsReader);
                    longs = new long[outerInstance.longsSize];
                }

                public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                /// <remarks>
                /// TODO: we may want an alternate mode here which is
                /// "if you are about to return NOT_FOUND I won't use
                /// the terms data from that"; eg FuzzyTermsEnum will
                /// (usually) just immediately call seek again if we
                /// return NOT_FOUND so it's a waste for us to fill in
                /// the term that was actually NOT_FOUND 
                /// </remarks>
                public override SeekStatus SeekCeil(BytesRef target)
                {
                    if (indexEnum is null)
                    {
                        throw IllegalStateException.Create("terms index was not loaded");
                    }

                    //System.out.println("BTR.seek seg=" + segment + " target=" + fieldInfo.name + ":" + target.utf8ToString() + " " + target + " current=" + term().utf8ToString() + " " + term() + " indexIsCurrent=" + indexIsCurrent + " didIndexNext=" + didIndexNext + " seekPending=" + seekPending + " divisor=" + indexReader.getDivisor() + " this="  + this);
                    if (didIndexNext)
                    {
                        if (nextIndexTerm is null)
                        {
                            //System.out.println("  nextIndexTerm=null");
                        }
                        else
                        {
                            //System.out.println("  nextIndexTerm=" + nextIndexTerm.utf8ToString());
                        }
                    }

                    bool doSeek = true;

                    // See if we can avoid seeking, because target term
                    // is after current term but before next index term:
                    if (indexIsCurrent)
                    {

                        int cmp = BytesRef.UTF8SortedAsUnicodeComparer.Compare(term, target);

                        if (cmp == 0)
                        {
                            // Already at the requested term
                            return SeekStatus.FOUND;
                        }
                        else if (cmp < 0)
                        {

                            // Target term is after current term
                            if (!didIndexNext)
                            {
                                if (indexEnum.Next() == -1)
                                {
                                    nextIndexTerm = null;
                                }
                                else
                                {
                                    nextIndexTerm = indexEnum.Term;
                                }
                                //System.out.println("  now do index next() nextIndexTerm=" + (nextIndexTerm is null ? "null" : nextIndexTerm.utf8ToString()));
                                didIndexNext = true;
                            }

                            if (nextIndexTerm is null || BytesRef.UTF8SortedAsUnicodeComparer.Compare(target, nextIndexTerm) < 0)
                            {
                                // Optimization: requested term is within the
                                // same term block we are now in; skip seeking
                                // (but do scanning):
                                doSeek = false;
                                //System.out.println("  skip seek: nextIndexTerm=" + (nextIndexTerm is null ? "null" : nextIndexTerm.utf8ToString()));
                            }
                        }
                    }

                    if (doSeek)
                    {
                        //System.out.println("  seek");

                        // Ask terms index to find biggest indexed term (=
                        // first term in a block) that's <= our text:
                        input.Seek(indexEnum.Seek(target));
                        bool result = NextBlock();

                        // Block must exist since, at least, the indexed term
                        // is in the block:
                        if (Debugging.AssertsEnabled) Debugging.Assert(result);

                        indexIsCurrent = true;
                        didIndexNext = false;
                        blocksSinceSeek = 0;

                        if (doOrd)
                        {
                            state.Ord = indexEnum.Ord - 1;
                        }

                        term.CopyBytes(indexEnum.Term);
                        //System.out.println("  seek: term=" + term.utf8ToString());
                    }
                    else
                    {
                        //System.out.println("  skip seek");
                        if (state.TermBlockOrd == blockTermCount && !NextBlock())
                        {
                            indexIsCurrent = false;
                            return SeekStatus.END;
                        }
                    }

                    seekPending = false;

                    int common = 0;

                    // Scan within block.  We could do this by calling
                    // _next() and testing the resulting term, but this
                    // is wasteful.  Instead, we first confirm the
                    // target matches the common prefix of this block,
                    // and then we scan the term bytes directly from the
                    // termSuffixesreader's byte[], saving a copy into
                    // the BytesRef term per term.  Only when we return
                    // do we then copy the bytes into the term.

                    while (true)
                    {

                        // First, see if target term matches common prefix
                        // in this block:
                        if (common < termBlockPrefix)
                        {
                            int cmp = (term.Bytes[common] & 0xFF) - (target.Bytes[target.Offset + common] & 0xFF);
                            if (cmp < 0)
                            {

                                // TODO: maybe we should store common prefix
                                // in block header?  (instead of relying on
                                // last term of previous block)

                                // Target's prefix is after the common block
                                // prefix, so term cannot be in this block
                                // but it could be in next block.  We
                                // must scan to end-of-block to set common
                                // prefix for next block:
                                if (state.TermBlockOrd < blockTermCount)
                                {
                                    while (state.TermBlockOrd < blockTermCount - 1)
                                    {
                                        state.TermBlockOrd++;
                                        state.Ord++;
                                        termSuffixesReader.SkipBytes(termSuffixesReader.ReadVInt32());
                                    }
                                    int suffix = termSuffixesReader.ReadVInt32();
                                    term.Length = termBlockPrefix + suffix;
                                    if (term.Bytes.Length < term.Length)
                                    {
                                        term.Grow(term.Length);
                                    }
                                    termSuffixesReader.ReadBytes(term.Bytes, termBlockPrefix, suffix);
                                }
                                state.Ord++;

                                if (!NextBlock())
                                {
                                    indexIsCurrent = false;
                                    return SeekStatus.END;
                                }
                                common = 0;

                            }
                            else if (cmp > 0)
                            {
                                // Target's prefix is before the common prefix
                                // of this block, so we position to start of
                                // block and return NOT_FOUND:
                                if (Debugging.AssertsEnabled) Debugging.Assert(state.TermBlockOrd == 0);

                                int suffix = termSuffixesReader.ReadVInt32();
                                term.Length = termBlockPrefix + suffix;
                                if (term.Bytes.Length < term.Length)
                                {
                                    term.Grow(term.Length);
                                }
                                termSuffixesReader.ReadBytes(term.Bytes, termBlockPrefix, suffix);
                                return SeekStatus.NOT_FOUND;
                            }
                            else
                            {
                                common++;
                            }

                            continue;
                        }

                        // Test every term in this block
                        while (true)
                        {
                            state.TermBlockOrd++;
                            state.Ord++;

                            int suffix = termSuffixesReader.ReadVInt32();

                            // We know the prefix matches, so just compare the new suffix:
                            int termLen = termBlockPrefix + suffix;
                            int bytePos = termSuffixesReader.Position;

                            bool next = false;
                            int limit = target.Offset + (termLen < target.Length ? termLen : target.Length);
                            int targetPos = target.Offset + termBlockPrefix;
                            while (targetPos < limit)
                            {
                                int cmp = (termSuffixes[bytePos++] & 0xFF) - (target.Bytes[targetPos++] & 0xFF);
                                if (cmp < 0)
                                {
                                    // Current term is still before the target;
                                    // keep scanning
                                    next = true;
                                    break;
                                }
                                else if (cmp > 0)
                                {
                                    // Done!  Current term is after target. Stop
                                    // here, fill in real term, return NOT_FOUND.
                                    term.Length = termBlockPrefix + suffix;
                                    if (term.Bytes.Length < term.Length)
                                    {
                                        term.Grow(term.Length);
                                    }
                                    termSuffixesReader.ReadBytes(term.Bytes, termBlockPrefix, suffix);
                                    //System.out.println("  NOT_FOUND");
                                    return SeekStatus.NOT_FOUND;
                                }
                            }

                            if (!next && target.Length <= termLen)
                            {
                                term.Length = termBlockPrefix + suffix;
                                if (term.Bytes.Length < term.Length)
                                {
                                    term.Grow(term.Length);
                                }
                                termSuffixesReader.ReadBytes(term.Bytes, termBlockPrefix, suffix);

                                if (target.Length == termLen)
                                {
                                    // Done!  Exact match.  Stop here, fill in
                                    // real term, return FOUND.
                                    //System.out.println("  FOUND");
                                    return SeekStatus.FOUND;
                                }
                                else
                                {
                                    //System.out.println("  NOT_FOUND");
                                    return SeekStatus.NOT_FOUND;
                                }
                            }

                            if (state.TermBlockOrd == blockTermCount)
                            {
                                // Must pre-fill term for next block's common prefix
                                term.Length = termBlockPrefix + suffix;
                                if (term.Bytes.Length < term.Length)
                                {
                                    term.Grow(term.Length);
                                }
                                termSuffixesReader.ReadBytes(term.Bytes, termBlockPrefix, suffix);
                                break;
                            }
                            else
                            {
                                termSuffixesReader.SkipBytes(suffix);
                            }
                        }

                        // The purpose of the terms dict index is to seek
                        // the enum to the closest index term before the
                        // term we are looking for.  So, we should never
                        // cross another index term (besides the first
                        // one) while we are scanning:

                        if (Debugging.AssertsEnabled) Debugging.Assert(indexIsCurrent);

                        if (!NextBlock())
                        {
                            //System.out.println("  END");
                            indexIsCurrent = false;
                            return SeekStatus.END;
                        }
                        common = 0;
                    }
                }

                public override bool MoveNext()
                {
                    //System.out.println("BTR.next() seekPending=" + seekPending + " pendingSeekCount=" + state.termBlockOrd);

                    // If seek was previously called and the term was cached,
                    // usually caller is just going to pull a D/&PEnum or get
                    // docFreq, etc.  But, if they then call next(),
                    // this method catches up all internal state so next()
                    // works properly:
                    if (seekPending)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(!indexIsCurrent);
                        input.Seek(state.BlockFilePointer);
                        int pendingSeekCount = state.TermBlockOrd;
                        bool result = NextBlock();

                        long savOrd = state.Ord;

                        // Block must exist since seek(TermState) was called w/ a
                        // TermState previously returned by this enum when positioned
                        // on a real term:
                        if (Debugging.AssertsEnabled) Debugging.Assert(result);

                        while (state.TermBlockOrd < pendingSeekCount)
                        {
                            BytesRef nextResult = _next();
                            if (Debugging.AssertsEnabled) Debugging.Assert(nextResult != null);
                        }
                        seekPending = false;
                        state.Ord = savOrd;
                    }
                    //System.out.println("BTR._next seg=" + segment + " this=" + this + " termCount=" + state.termBlockOrd + " (vs " + blockTermCount + ")");
                    if (state.TermBlockOrd == blockTermCount && !NextBlock())
                    {
                        //System.out.println("  eof");
                        indexIsCurrent = false;
                        return false;
                    }

                    // TODO: cutover to something better for these ints!  simple64?
                    int suffix = termSuffixesReader.ReadVInt32();
                    //System.out.println("  suffix=" + suffix);

                    term.Length = termBlockPrefix + suffix;
                    if (term.Bytes.Length < term.Length)
                    {
                        term.Grow(term.Length);
                    }
                    termSuffixesReader.ReadBytes(term.Bytes, termBlockPrefix, suffix);
                    state.TermBlockOrd++;

                    // NOTE: meaningless in the non-ord case
                    state.Ord++;

                    return term != null;
                }

                [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override BytesRef Next()
                {
                    if (MoveNext())
                        return term;
                    return null;
                }

                /// <summary>
                /// Decodes only the term bytes of the next term.  If caller then asks for
                /// metadata, ie docFreq, totalTermFreq or pulls a D/P Enum, we then (lazily)
                /// decode all metadata up to the current term
                /// </summary>
                /// <returns></returns>
#pragma warning disable IDE1006 // Naming Styles
                private BytesRef _next()
#pragma warning restore IDE1006 // Naming Styles
                {
                    //System.out.println("BTR._next seg=" + segment + " this=" + this + " termCount=" + state.termBlockOrd + " (vs " + blockTermCount + ")");
                    if (state.TermBlockOrd == blockTermCount && !NextBlock())
                    {
                        //System.out.println("  eof");
                        indexIsCurrent = false;
                        return null;
                    }

                    // TODO: cutover to something better for these ints!  simple64?
                    int suffix = termSuffixesReader.ReadVInt32();
                    //System.out.println("  suffix=" + suffix);

                    term.Length = termBlockPrefix + suffix;
                    if (term.Bytes.Length < term.Length)
                    {
                        term.Grow(term.Length);
                    }
                    termSuffixesReader.ReadBytes(term.Bytes, termBlockPrefix, suffix);
                    state.TermBlockOrd++;

                    // NOTE: meaningless in the non-ord case
                    state.Ord++;

                    //System.out.println("  return term=" + fieldInfo.name + ":" + term.utf8ToString() + " " + term + " tbOrd=" + state.termBlockOrd);
                    return term;
                }

                public override BytesRef Term => term;

                public override int DocFreq
                {
                    get
                    {
                        //System.out.println("BTR.docFreq");
                        DecodeMetaData();
                        //System.out.println("  return " + state.docFreq);
                        return state.DocFreq;
                    }
                }

                public override long TotalTermFreq
                {
                    get
                    {
                        DecodeMetaData();
                        return state.TotalTermFreq;
                    }
                }

                public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
                {
                    //System.out.println("BTR.docs this=" + this);
                    DecodeMetaData();
                    //System.out.println("BTR.docs:  state.docFreq=" + state.docFreq);
                    return outerInstance.outerInstance.postingsReader.Docs(outerInstance.fieldInfo, state, liveDocs, reuse, flags);
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse,
                    DocsAndPositionsFlags flags)
                {
                    // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                    if (IndexOptionsComparer.Default.Compare(outerInstance.fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                    {
                        // Positions were not indexed:
                        return null;
                    }

                    DecodeMetaData();
                    return outerInstance.outerInstance.postingsReader.DocsAndPositions(outerInstance.fieldInfo, state, liveDocs, reuse, flags);
                }

                public override void SeekExact(BytesRef target, TermState otherState)
                {
                    //System.out.println("BTR.seekExact termState target=" + target.utf8ToString() + " " + target + " this=" + this);
                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(otherState != null && otherState is BlockTermState);
                        Debugging.Assert(!doOrd || ((BlockTermState)otherState).Ord < outerInstance.numTerms);
                    }
                    state.CopyFrom(otherState);
                    seekPending = true;
                    indexIsCurrent = false;
                    term.CopyBytes(target);
                }

                public override TermState GetTermState()
                {
                    //System.out.println("BTR.termState this=" + this);
                    DecodeMetaData();
                    TermState ts = (TermState)state.Clone();
                    //System.out.println("  return ts=" + ts);
                    return ts;
                }

                public override void SeekExact(long ord)
                {
                    //System.out.println("BTR.seek by ord ord=" + ord);
                    if (indexEnum is null)
                    {
                        throw IllegalStateException.Create("terms index was not loaded");
                    }

                    if (Debugging.AssertsEnabled) Debugging.Assert(ord < outerInstance.numTerms);

                    // TODO: if ord is in same terms block and
                    // after current ord, we should avoid this seek just
                    // like we do in the seek(BytesRef) case
                    input.Seek(indexEnum.Seek(ord));
                    bool result = NextBlock();

                    // Block must exist since ord < numTerms:
                    if (Debugging.AssertsEnabled) Debugging.Assert(result);

                    indexIsCurrent = true;
                    didIndexNext = false;
                    blocksSinceSeek = 0;
                    seekPending = false;

                    state.Ord = indexEnum.Ord - 1;
                    if (Debugging.AssertsEnabled) Debugging.Assert(state.Ord >= -1, "Ord={0}", state.Ord);
                    term.CopyBytes(indexEnum.Term);

                    // Now, scan:
                    int left = (int)(ord - state.Ord);
                    while (left > 0)
                    {
                        BytesRef term = _next();
                        if (Debugging.AssertsEnabled) Debugging.Assert(term != null);
                        left--;
                        if (Debugging.AssertsEnabled) Debugging.Assert(indexIsCurrent);
                    }
                }

                public override long Ord
                {
                    get
                    {
                        if (!doOrd)
                        {
                            throw UnsupportedOperationException.Create();
                        }
                        return state.Ord;
                    }
                }

                // Does initial decode of next block of terms; this
                // doesn't actually decode the docFreq, totalTermFreq,
                // postings details (frq/prx offset, etc.) metadata;
                // it just loads them as byte[] blobs which are then      
                // decoded on-demand if the metadata is ever requested
                // for any term in this block.  This enables terms-only
                // intensive consumes (eg certain MTQs, respelling) to
                // not pay the price of decoding metadata they won't
                // use.

                private bool NextBlock()
                {
                    // TODO: we still lazy-decode the byte[] for each
                    // term (the suffix), but, if we decoded
                    // all N terms up front then seeking could do a fast
                    // bsearch w/in the block...

                    //System.out.println("BTR.nextBlock() fp=" + in.getFilePointer() + " this=" + this);
                    state.BlockFilePointer = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    blockTermCount = input.ReadVInt32();
                    //System.out.println("  blockTermCount=" + blockTermCount);
                    if (blockTermCount == 0)
                    {
                        return false;
                    }
                    termBlockPrefix = input.ReadVInt32();

                    // term suffixes:
                    int len = input.ReadVInt32();
                    if (termSuffixes.Length < len)
                    {
                        termSuffixes = new byte[ArrayUtil.Oversize(len, 1)];
                    }
                    //System.out.println("  termSuffixes len=" + len);
                    input.ReadBytes(termSuffixes, 0, len);
                    termSuffixesReader.Reset(termSuffixes, 0, len);

                    // docFreq, totalTermFreq
                    len = input.ReadVInt32();
                    if (docFreqBytes.Length < len)
                    {
                        docFreqBytes = new byte[ArrayUtil.Oversize(len, 1)];
                    }
                    //System.out.println("  freq bytes len=" + len);
                    input.ReadBytes(docFreqBytes, 0, len);
                    freqReader.Reset(docFreqBytes, 0, len);

                    // metadata
                    len = input.ReadVInt32();
                    if (bytes is null)
                    {
                        bytes = new byte[ArrayUtil.Oversize(len, 1)];
                        bytesReader = new ByteArrayDataInput();
                    }
                    else if (bytes.Length < len)
                    {
                        bytes = new byte[ArrayUtil.Oversize(len, 1)];
                    }
                    input.ReadBytes(bytes, 0, len);
                    bytesReader.Reset(bytes, 0, len);

                    metaDataUpto = 0;
                    state.TermBlockOrd = 0;

                    blocksSinceSeek++;
                    indexIsCurrent = indexIsCurrent && (blocksSinceSeek < outerInstance.outerInstance.indexReader.Divisor);
                    //System.out.println("  indexIsCurrent=" + indexIsCurrent);

                    return true;
                }

                private void DecodeMetaData()
                {
                    //System.out.println("BTR.decodeMetadata mdUpto=" + metaDataUpto + " vs termCount=" + state.termBlockOrd + " state=" + state);
                    if (!seekPending)
                    {
                        // TODO: cutover to random-access API
                        // here.... really stupid that we have to decode N
                        // wasted term metadata just to get to the N+1th
                        // that we really need...

                        // lazily catch up on metadata decode:
                        int limit = state.TermBlockOrd;
                        bool absolute = metaDataUpto == 0;
                        // TODO: better API would be "jump straight to term=N"???
                        while (metaDataUpto < limit)
                        {
                            //System.out.println("  decode mdUpto=" + metaDataUpto);
                            // TODO: we could make "tiers" of metadata, ie,
                            // decode docFreq/totalTF but don't decode postings
                            // metadata; this way caller could get
                            // docFreq/totalTF w/o paying decode cost for
                            // postings

                            // TODO: if docFreq were bulk decoded we could
                            // just skipN here:

                            // docFreq, totalTermFreq
                            state.DocFreq = freqReader.ReadVInt32();
                            //System.out.println("    dF=" + state.docFreq);
                            if (outerInstance.fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                            {
                                state.TotalTermFreq = state.DocFreq + freqReader.ReadVInt64();
                                //System.out.println("    totTF=" + state.totalTermFreq);
                            }
                            // metadata
                            for (int i = 0; i < longs.Length; i++)
                            {
                                longs[i] = bytesReader.ReadVInt64();
                            }
                            outerInstance.outerInstance.postingsReader.DecodeTerm(longs, bytesReader, outerInstance.fieldInfo, state, absolute);
                            metaDataUpto++;
                            absolute = false;
                        }
                    }
                    else
                    {
                        //System.out.println("  skip! seekPending");
                    }
                }
            }
        }

        public override long RamBytesUsed()
        {
            long sizeInBytes = (postingsReader != null) ? postingsReader.RamBytesUsed() : 0;
            sizeInBytes += (indexReader != null) ? indexReader.RamBytesUsed() : 0;
            return sizeInBytes;
        }

        public override void CheckIntegrity()
        {
            // verify terms
            if (version >= BlockTermsWriter.VERSION_CHECKSUM)
            {
                CodecUtil.ChecksumEntireFile(input);
            }
            // verify postings
            postingsReader.CheckIntegrity();
        }
    }
}