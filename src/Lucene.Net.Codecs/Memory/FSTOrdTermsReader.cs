using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BitSet = Lucene.Net.Util.OpenBitSet;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

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

    /// <summary>
    /// FST-based terms dictionary reader.
    /// <para/>
    /// The FST index maps each term and its ord, and during seek
    /// the ord is used fetch metadata from a single block.
    /// The term dictionary is fully memory resident.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class FSTOrdTermsReader : FieldsProducer
    {
        private const int INTERVAL = FSTOrdTermsWriter.SKIP_INTERVAL;

        // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
        private readonly IDictionary<string, TermsReader> fields = new JCG.SortedDictionary<string, TermsReader>(StringComparer.Ordinal);
        private readonly PostingsReaderBase postingsReader;
        private readonly int version; // LUCENENET: marked readonly
        //static final boolean TEST = false;

        public FSTOrdTermsReader(SegmentReadState state, PostingsReaderBase postingsReader)
        {
            string termsIndexFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, FSTOrdTermsWriter.TERMS_INDEX_EXTENSION);
            string termsBlockFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, FSTOrdTermsWriter.TERMS_BLOCK_EXTENSION);

            this.postingsReader = postingsReader;
            ChecksumIndexInput indexIn = null;
            IndexInput blockIn = null;
            bool success = false;
            try
            {
                indexIn = state.Directory.OpenChecksumInput(termsIndexFileName, state.Context);
                blockIn = state.Directory.OpenInput(termsBlockFileName, state.Context);
                version = ReadHeader(indexIn);
                ReadHeader(blockIn);
                if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
                {
                    CodecUtil.ChecksumEntireFile(blockIn);
                }

                this.postingsReader.Init(blockIn);
                SeekDir(blockIn);

                FieldInfos fieldInfos = state.FieldInfos;
                int numFields = blockIn.ReadVInt32();
                for (int i = 0; i < numFields; i++)
                {
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(blockIn.ReadVInt32());
                    bool hasFreq = fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY;
                    long numTerms = blockIn.ReadVInt64();
                    long sumTotalTermFreq = hasFreq ? blockIn.ReadVInt64() : -1;
                    long sumDocFreq = blockIn.ReadVInt64();
                    int docCount = blockIn.ReadVInt32();
                    int longsSize = blockIn.ReadVInt32();
                    var index = new FST<Int64>(indexIn, PositiveInt32Outputs.Singleton);

                    var current = new TermsReader(this, fieldInfo, blockIn, numTerms, sumTotalTermFreq, sumDocFreq, docCount, longsSize, index);
                    // LUCENENET NOTE: This simulates a put operation in Java,
                    // getting the prior value first before setting it.
                    fields.TryGetValue(fieldInfo.Name, out TermsReader previous);
                    fields[fieldInfo.Name] = current;
                    CheckFieldSummary(state.SegmentInfo, indexIn, blockIn, current, previous);
                }
                if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
                {
                    CodecUtil.CheckFooter(indexIn);
                }
                else
                {
#pragma warning disable 612, 618
                    CodecUtil.CheckEOF(indexIn);
#pragma warning restore 612, 618
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(indexIn, blockIn);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(indexIn, blockIn);
                }
            }
        }

        private static int ReadHeader(IndexInput @in) // LUCENENET: CA1822: Mark members as static
        {
            return CodecUtil.CheckHeader(@in, FSTOrdTermsWriter.TERMS_CODEC_NAME, FSTOrdTermsWriter.TERMS_VERSION_START, FSTOrdTermsWriter.TERMS_VERSION_CURRENT);
        }

        private void SeekDir(IndexInput @in)
        {
            if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
            {
                @in.Seek(@in.Length - CodecUtil.FooterLength() - 8);
            }
            else
            {
                @in.Seek(@in.Length - 8);
            }
            @in.Seek(@in.ReadInt64());
        }

        private static void CheckFieldSummary(SegmentInfo info, IndexInput indexIn, IndexInput blockIn, TermsReader field, TermsReader previous) // LUCENENET: CA1822: Mark members as static
        {
            // #docs with field must be <= #docs
            if (field.docCount < 0 || field.docCount > info.DocCount)
            {
                throw new CorruptIndexException("invalid docCount: " + field.docCount + " maxDoc: " + info.DocCount + " (resource=" + indexIn + ", " + blockIn + ")");
            }
            // #postings must be >= #docs with field
            if (field.sumDocFreq < field.docCount)
            {
                throw new CorruptIndexException("invalid sumDocFreq: " + field.sumDocFreq + " docCount: " + field.docCount + " (resource=" + indexIn + ", " + blockIn + ")");
            }
            // #positions must be >= #postings
            if (field.sumTotalTermFreq != -1 && field.sumTotalTermFreq < field.sumDocFreq)
            {
                throw new CorruptIndexException("invalid sumTotalTermFreq: " + field.sumTotalTermFreq + " sumDocFreq: " + field.sumDocFreq + " (resource=" + indexIn + ", " + blockIn + ")");
            }
            if (previous != null)
            {
                throw new CorruptIndexException("duplicate fields: " + field.fieldInfo.Name + " (resource=" + indexIn + ", " + blockIn + ")");
            }
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return fields.Keys.GetEnumerator(); // LUCENENET NOTE: enumerators are not writable in .NET
        }

        public override Terms GetTerms(string field)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(field != null);
            fields.TryGetValue(field, out TermsReader result);
            return result;
        }

        public override int Count => fields.Count;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    IOUtils.Dispose(postingsReader);
                }
                finally
                {
                    fields.Clear();
                }
            }
        }

        internal sealed class TermsReader : Terms
        {
            private readonly FSTOrdTermsReader outerInstance;

            internal readonly FieldInfo fieldInfo;
            private readonly long numTerms;
            internal readonly long sumTotalTermFreq;
            internal readonly long sumDocFreq;
            internal readonly int docCount;
            private readonly int longsSize;
            internal readonly FST<Int64> index;

            private readonly int numSkipInfo;
            internal readonly long[] skipInfo;
            internal readonly byte[] statsBlock;
            internal readonly byte[] metaLongsBlock;
            internal readonly byte[] metaBytesBlock;

            internal TermsReader(FSTOrdTermsReader outerInstance, FieldInfo fieldInfo, IndexInput blockIn, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize, FST<Int64> index)
            {
                this.outerInstance = outerInstance;
                this.fieldInfo = fieldInfo;
                this.numTerms = numTerms;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.sumDocFreq = sumDocFreq;
                this.docCount = docCount;
                this.longsSize = longsSize;
                this.index = index;

                if (Debugging.AssertsEnabled) Debugging.Assert((numTerms & (~0xffffffffL)) == 0);
                int numBlocks = (int)(numTerms + INTERVAL - 1) / INTERVAL;
                this.numSkipInfo = longsSize + 3;
                this.skipInfo = new long[numBlocks * numSkipInfo];
                this.statsBlock = new byte[(int)blockIn.ReadVInt64()];
                this.metaLongsBlock = new byte[(int)blockIn.ReadVInt64()];
                this.metaBytesBlock = new byte[(int)blockIn.ReadVInt64()];

                int last = 0, next; // LUCENENET: IDE0059: Remove unnecessary value assignment
                for (int i = 1; i < numBlocks; i++)
                {
                    next = numSkipInfo * i;
                    for (int j = 0; j < numSkipInfo; j++)
                    {
                        skipInfo[next + j] = skipInfo[last + j] + blockIn.ReadVInt64();
                    }
                    last = next;
                }
                blockIn.ReadBytes(statsBlock, 0, statsBlock.Length);
                blockIn.ReadBytes(metaLongsBlock, 0, metaLongsBlock.Length);
                blockIn.ReadBytes(metaBytesBlock, 0, metaBytesBlock.Length);
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            public override bool HasFreqs => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;

            public override bool HasOffsets => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

            public override bool HasPositions => IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;

            public override bool HasPayloads => fieldInfo.HasPayloads;

            public override long Count => numTerms;

            public override long SumTotalTermFreq => sumTotalTermFreq;

            public override long SumDocFreq => sumDocFreq;

            public override int DocCount => docCount;

            public override TermsEnum GetEnumerator()
            {
                return new SegmentTermsEnum(this);
            }

            public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
            {
                return new IntersectTermsEnum(this, compiled, startTerm);
            }

            // Only wraps common operations for PBF interact
            internal abstract class BaseTermsEnum : TermsEnum
            {
                private readonly FSTOrdTermsReader.TermsReader outerInstance;

                /* Current term, null when enum ends or unpositioned */
                internal BytesRef term;

                /* Current term's ord, starts from 0 */
                internal long ord;

                /* Current term stats + decoded metadata (customized by PBF) */
                internal readonly BlockTermState state;

                /* Datainput to load stats & metadata */
                private readonly ByteArrayDataInput statsReader = new ByteArrayDataInput();
                private readonly ByteArrayDataInput metaLongsReader = new ByteArrayDataInput();
                private readonly ByteArrayDataInput metaBytesReader = new ByteArrayDataInput();

                /* To which block is buffered */
                private int statsBlockOrd;
                private int metaBlockOrd;

                /* Current buffered metadata (long[] & byte[]) */
                private readonly long[][] longs; // LUCENENET: marked readonly
                private readonly int[] bytesStart; // LUCENENET: marked readonly
                private readonly int[] bytesLength; // LUCENENET: marked readonly

                /* Current buffered stats (df & ttf) */
                private readonly int[] docFreq; // LUCENENET: marked readonly
                private readonly long[] totalTermFreq; // LUCENENET: marked readonly

                private protected BaseTermsEnum(TermsReader outerInstance) // LUCENENET: Changed from internal to private protected
                {
                    this.outerInstance = outerInstance;
                    this.state = outerInstance.outerInstance.postingsReader.NewTermState();
                    this.term = null;
                    this.statsReader.Reset(outerInstance.statsBlock);
                    this.metaLongsReader.Reset(outerInstance.metaLongsBlock);
                    this.metaBytesReader.Reset(outerInstance.metaBytesBlock);

                    this.longs = RectangularArrays.ReturnRectangularArray<long>(INTERVAL, outerInstance.longsSize);
                    this.bytesStart = new int[INTERVAL];
                    this.bytesLength = new int[INTERVAL];
                    this.docFreq = new int[INTERVAL];
                    this.totalTermFreq = new long[INTERVAL];
                    this.statsBlockOrd = -1;
                    this.metaBlockOrd = -1;
                    if (!outerInstance.HasFreqs)
                    {
                        Arrays.Fill(totalTermFreq, -1);
                    }
                }

                public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                /// <summary>
                /// Decodes stats data into term state. </summary>
                internal virtual void DecodeStats()
                {
                    int upto = (int)ord % INTERVAL;
                    int oldBlockOrd = statsBlockOrd;
                    statsBlockOrd = (int)ord / INTERVAL;
                    if (oldBlockOrd != statsBlockOrd)
                    {
                        RefillStats();
                    }
                    state.DocFreq = docFreq[upto];
                    state.TotalTermFreq = totalTermFreq[upto];
                }

                /// <summary>
                /// Let PBF decode metadata. </summary>
                internal virtual void DecodeMetaData()
                {
                    int upto = (int)ord % INTERVAL;
                    int oldBlockOrd = metaBlockOrd;
                    metaBlockOrd = (int)ord / INTERVAL;
                    if (metaBlockOrd != oldBlockOrd)
                    {
                        RefillMetadata();
                    }
                    metaBytesReader.Position = bytesStart[upto];
                    outerInstance.outerInstance.postingsReader.DecodeTerm(longs[upto], metaBytesReader, outerInstance.fieldInfo, state, true);
                }

                /// <summary>
                /// Load current stats shard. </summary>
                internal void RefillStats()
                {
                    var offset = statsBlockOrd * outerInstance.numSkipInfo;
                    var statsFP = (int)outerInstance.skipInfo[offset];
                    statsReader.Position = statsFP;
                    for (int i = 0; i < INTERVAL && !statsReader.Eof; i++)
                    {
                        int code = statsReader.ReadVInt32();
                        if (outerInstance.HasFreqs)
                        {
                            docFreq[i] = code.TripleShift(1);
                            if ((code & 1) == 1)
                            {
                                totalTermFreq[i] = docFreq[i];
                            }
                            else
                            {
                                totalTermFreq[i] = docFreq[i] + statsReader.ReadVInt64();
                            }
                        }
                        else
                        {
                            docFreq[i] = code;
                        }
                    }
                }

                /// <summary>
                /// Load current metadata shard. </summary>
                internal void RefillMetadata()
                {
                    var offset = metaBlockOrd * outerInstance.numSkipInfo;
                    var metaLongsFP = (int)outerInstance.skipInfo[offset + 1];
                    var metaBytesFP = (int)outerInstance.skipInfo[offset + 2];
                    metaLongsReader.Position = metaLongsFP;
                    for (int j = 0; j < outerInstance.longsSize; j++)
                    {
                        longs[0][j] = outerInstance.skipInfo[offset + 3 + j] + metaLongsReader.ReadVInt64();
                    }
                    bytesStart[0] = metaBytesFP;
                    bytesLength[0] = (int)metaLongsReader.ReadVInt64();
                    for (int i = 1; i < INTERVAL && !metaLongsReader.Eof; i++)
                    {
                        for (int j = 0; j < outerInstance.longsSize; j++)
                        {
                            longs[i][j] = longs[i - 1][j] + metaLongsReader.ReadVInt64();
                        }
                        bytesStart[i] = bytesStart[i - 1] + bytesLength[i - 1];
                        bytesLength[i] = (int)metaLongsReader.ReadVInt64();
                    }
                }

                public override TermState GetTermState()
                {
                    DecodeMetaData();
                    return (TermState)state.Clone();
                }

                public override BytesRef Term => term;

                public override int DocFreq => state.DocFreq;

                public override long TotalTermFreq => state.TotalTermFreq;

                public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
                {
                    DecodeMetaData();
                    return outerInstance.outerInstance.postingsReader.Docs(outerInstance.fieldInfo, state, liveDocs, reuse, flags);
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
                {
                    if (!outerInstance.HasPositions)
                    {
                        return null;
                    }
                    DecodeMetaData();
                    return outerInstance.outerInstance.postingsReader.DocsAndPositions(outerInstance.fieldInfo, state, liveDocs, reuse, flags);
                }

                // TODO: this can be achieved by making use of Util.getByOutput()
                //           and should have related tests
                public override void SeekExact(long ord)
                {
                    throw UnsupportedOperationException.Create();
                }

                public override long Ord => throw UnsupportedOperationException.Create();
            }

            // Iterates through all terms in this field
            private sealed class SegmentTermsEnum : BaseTermsEnum
            {
                private readonly BytesRefFSTEnum<Int64> fstEnum;

                /* True when current term's metadata is decoded */
                private bool decoded;

                /* True when current enum is 'positioned' by seekExact(TermState) */
                private bool seekPending;

                internal SegmentTermsEnum(FSTOrdTermsReader.TermsReader outerInstance) : base(outerInstance)
                {
                    this.fstEnum = new BytesRefFSTEnum<Int64>(outerInstance.index);
                    this.decoded = false;
                    this.seekPending = false;
                }

                internal override void DecodeMetaData()
                {
                    if (!decoded && !seekPending)
                    {
                        base.DecodeMetaData();
                        decoded = true;
                    }
                }

                // Update current enum according to FSTEnum
                private void UpdateEnum(BytesRefFSTEnum.InputOutput<Int64> pair)
                {
                    if (pair is null)
                    {
                        term = null;
                    }
                    else
                    {
                        term = pair.Input;
                        ord = pair.Output;
                        DecodeStats();
                    }
                    decoded = false;
                    seekPending = false;
                }

                public override bool MoveNext()
                {
                    if (seekPending) // previously positioned, but termOutputs not fetched
                    {
                        seekPending = false;
                        var status = SeekCeil(term);
                        if (Debugging.AssertsEnabled) Debugging.Assert(status == SeekStatus.FOUND); // must positioned on valid term
                    }
                    // LUCENENET specific - extracted logic of UpdateEnum() so we can eliminate the null check
                    var moved = fstEnum.MoveNext();
                    if (moved)
                    {
                        var pair = fstEnum.Current;
                        term = pair.Input;
                        ord = pair.Output;
                        DecodeStats();
                    }
                    else
                    {
                        term = null;
                    }
                    decoded = false;
                    seekPending = false;
                    return moved && term != null;
                }

                [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override BytesRef Next()
                {
                    if (MoveNext())
                        return term;
                    return null;
                }

                public override bool SeekExact(BytesRef target)
                {
                    UpdateEnum(fstEnum.SeekExact(target));
                    return term != null;
                }

                public override SeekStatus SeekCeil(BytesRef target)
                {
                    UpdateEnum(fstEnum.SeekCeil(target));
                    if (term is null)
                    {
                        return SeekStatus.END;
                    }
                    else
                    {
                        return term.Equals(target) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
                    }
                }

                public override void SeekExact(BytesRef target, TermState otherState)
                {
                    if (!target.Equals(term))
                    {
                        state.CopyFrom(otherState);
                        term = BytesRef.DeepCopyOf(target);
                        seekPending = true;
                    }
                }
            }

            // Iterates intersect result with automaton (cannot seek!)
            private sealed class IntersectTermsEnum : BaseTermsEnum
            {
                /// <summary>True when current term's metadata is decoded.</summary>
                private bool decoded;

                /// <summary>True when there is pending term when calling <see cref="MoveNext()"/>.</summary>
                private bool pending;

                /// <summary>
                /// stack to record how current term is constructed,
                /// used to accumulate metadata or rewind term:
                ///   level == term.length + 1,
                ///     == 0 when term is null
                /// </summary>
                private Frame[] stack;
                private int level;

                /// <summary>term dict fst</summary>
                private readonly FST<Int64> fst;
                private readonly FST.BytesReader fstReader;
                private readonly Outputs<Int64> fstOutputs;

                /// <summary>Query automaton to intersect with.</summary>
                private readonly ByteRunAutomaton fsa;

                private sealed class Frame
                {
                    /// <summary>fst stats</summary>
                    internal FST.Arc<Int64> arc;

                    /// <summary>automaton stats</summary>
                    internal int state;

                    internal Frame()
                    {
                        this.arc = new FST.Arc<Int64>();
                        this.state = -1;
                    }

                    public override string ToString()
                    {
                        return "arc=" + arc + " state=" + state;
                    }
                }

                internal IntersectTermsEnum(TermsReader outerInstance, CompiledAutomaton compiled, BytesRef startTerm) : base(outerInstance)
                {
                    //if (TEST) System.out.println("Enum init, startTerm=" + startTerm);
                    this.fst = outerInstance.index;
                    this.fstReader = fst.GetBytesReader();
                    this.fstOutputs = outerInstance.index.Outputs;
                    this.fsa = compiled.RunAutomaton;
                    this.level = -1;
                    this.stack = new Frame[16];
                    for (int i = 0; i < stack.Length; i++)
                    {
                        this.stack[i] = new Frame();
                    }

                    Frame frame;
                    /*frame = */LoadVirtualFrame(NewFrame()); // LUCENENET: IDE0059: Remove unnecessary value assignment
                    this.level++;
                    frame = LoadFirstFrame(NewFrame());
                    PushFrame(frame);

                    this.decoded = false;
                    this.pending = false;

                    if (startTerm is null)
                    {
                        pending = IsAccept(TopFrame());
                    }
                    else
                    {
                        DoSeekCeil(startTerm);
                        pending = !startTerm.Equals(term) && IsValid(TopFrame()) && IsAccept(TopFrame());
                    }
                }

                internal override void DecodeMetaData()
                {
                    if (!decoded)
                    {
                        base.DecodeMetaData();
                        decoded = true;
                    }
                }

                internal override void DecodeStats()
                {
                    var arc = TopFrame().arc;
                    if (Debugging.AssertsEnabled) Debugging.Assert(arc.NextFinalOutput == fstOutputs.NoOutput);
                    ord = arc.Output;
                    base.DecodeStats();
                }

                public override SeekStatus SeekCeil(BytesRef target)
                {
                    throw UnsupportedOperationException.Create();
                }

                public override bool MoveNext()
                {
                    //if (TEST) System.out.println("Enum next()");
                    if (pending)
                    {
                        pending = false;
                        DecodeStats();
                        return term != null;
                    }
                    decoded = false;
                    while (level > 0)
                    {
                        Frame frame = NewFrame();
                        if (LoadExpandFrame(TopFrame(), frame) != null) // has valid target
                        {
                            PushFrame(frame);
                            if (IsAccept(frame)) // gotcha
                            {
                                break;
                            }
                            continue; // check next target
                        }
                        frame = PopFrame();
                        while (level > 0)
                        {
                            if (LoadNextFrame(TopFrame(), frame) != null) // has valid sibling
                            {
                                PushFrame(frame);
                                if (IsAccept(frame)) // gotcha
                                {
                                    goto DFSBreak;
                                }
                                goto DFSContinue; // check next target
                            }
                            frame = PopFrame();
                        }
                        return false;
                    DFSContinue: {/* LUCENENET: intentionally blank */}
                    }
                DFSBreak:
                    DecodeStats();
                    return term != null;
                }

                [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override BytesRef Next()
                {
                    if (MoveNext())
                        return term;
                    return null;
                }

                private BytesRef DoSeekCeil(BytesRef target)
                {
                    //if (TEST) System.out.println("Enum doSeekCeil()");
                    Frame frame = null;
                    int label, upto = 0, limit = target.Length;
                    while (upto < limit) // to target prefix, or ceil label (rewind prefix)
                    {
                        frame = NewFrame();
                        label = target.Bytes[upto] & 0xff;
                        frame = LoadCeilFrame(label, TopFrame(), frame);
                        if (frame is null || frame.arc.Label != label)
                        {
                            break;
                        }
                        if (Debugging.AssertsEnabled) Debugging.Assert(IsValid(frame)); // target must be fetched from automaton
                        PushFrame(frame);
                        upto++;
                    }
                    if (upto == limit) // got target
                    {
                        return term;
                    }
                    if (frame != null) // got larger term('s prefix)
                    {
                        PushFrame(frame);
                        return IsAccept(frame) ? term : (MoveNext() ? term : null);
                    }
                    while (level > 0) // got target's prefix, advance to larger term
                    {
                        frame = PopFrame();
                        while (level > 0 && !CanRewind(frame))
                        {
                            frame = PopFrame();
                        }
                        if (LoadNextFrame(TopFrame(), frame) != null)
                        {
                            PushFrame(frame);
                            return IsAccept(frame) ? term : (MoveNext() ? term : null);
                        }
                    }
                    return null;
                }

                /// <summary>
                /// Virtual frame, never pop. </summary>
                private Frame LoadVirtualFrame(Frame frame)
                {
                    frame.arc.Output = fstOutputs.NoOutput;
                    frame.arc.NextFinalOutput = fstOutputs.NoOutput;
                    frame.state = -1;
                    return frame;
                }

                /// <summary>
                /// Load frame for start arc(node) on fst. </summary>
                private Frame LoadFirstFrame(Frame frame)
                {
                    frame.arc = fst.GetFirstArc(frame.arc);
                    frame.state = fsa.InitialState;
                    return frame;
                }

                /// <summary>
                /// Load frame for target arc(node) on fst. </summary>
                private Frame LoadExpandFrame(Frame top, Frame frame)
                {
                    if (!CanGrow(top))
                    {
                        return null;
                    }
                    frame.arc = fst.ReadFirstRealTargetArc(top.arc.Target, frame.arc, fstReader);
                    frame.state = fsa.Step(top.state, frame.arc.Label);
                    //if (TEST) System.out.println(" loadExpand frame="+frame);
                    if (frame.state == -1)
                    {
                        return LoadNextFrame(top, frame);
                    }
                    return frame;
                }

                /// <summary>
                /// Load frame for sibling arc(node) on fst. </summary>
                private Frame LoadNextFrame(Frame top, Frame frame)
                {
                    if (!CanRewind(frame))
                    {
                        return null;
                    }
                    while (!frame.arc.IsLast)
                    {
                        frame.arc = fst.ReadNextRealArc(frame.arc, fstReader);
                        frame.state = fsa.Step(top.state, frame.arc.Label);
                        if (frame.state != -1)
                        {
                            break;
                        }
                    }
                    //if (TEST) System.out.println(" loadNext frame="+frame);
                    if (frame.state == -1)
                    {
                        return null;
                    }
                    return frame;
                }

                /// <summary>
                /// Load frame for target arc(node) on fst, so that
                /// arc.label >= label and !fsa.reject(arc.label)
                /// </summary>
                private Frame LoadCeilFrame(int label, Frame top, Frame frame)
                {
                    var arc = frame.arc;
                    arc = Util.Fst.Util.ReadCeilArc(label, fst, top.arc, arc, fstReader);
                    if (arc is null)
                    {
                        return null;
                    }
                    frame.state = fsa.Step(top.state, arc.Label);
                    //if (TEST) System.out.println(" loadCeil frame="+frame);
                    if (frame.state == -1)
                    {
                        return LoadNextFrame(top, frame);
                    }
                    return frame;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private bool IsAccept(Frame frame) // reach a term both fst&fsa accepts
                {
                    return fsa.IsAccept(frame.state) && frame.arc.IsFinal;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static bool IsValid(Frame frame) // reach a prefix both fst&fsa won't reject // LUCENENET: CA1822: Mark members as static
                {
                    return frame.state != -1; //frame != null &&
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static bool CanGrow(Frame frame) // can walk forward on both fst&fsa // LUCENENET: CA1822: Mark members as static
                {
                    return frame.state != -1 && FST<Int64>.TargetHasArcs(frame.arc);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static bool CanRewind(Frame frame) // can jump to sibling // LUCENENET: CA1822: Mark members as static
                {
                    return !frame.arc.IsLast;
                }

                private void PushFrame(Frame frame)
                {
                    var arc = frame.arc;
                    arc.Output = fstOutputs.Add(TopFrame().arc.Output, arc.Output);
                    term = Grow(arc.Label);
                    level++;
                    if (Debugging.AssertsEnabled) Debugging.Assert(frame == stack[level]);
                }

                private Frame PopFrame()
                {
                    term = Shrink();
                    return stack[level--];
                }

                private Frame NewFrame()
                {
                    if (level + 1 == stack.Length)
                    {
                        var temp = new Frame[ArrayUtil.Oversize(level + 2, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                        Arrays.Copy(stack, 0, temp, 0, stack.Length);
                        for (int i = stack.Length; i < temp.Length; i++)
                        {
                            temp[i] = new Frame();
                        }
                        stack = temp;
                    }
                    return stack[level + 1];
                }

                private Frame TopFrame()
                {
                    return stack[level];
                }

                private BytesRef Grow(int label)
                {
                    if (term is null)
                    {
                        term = new BytesRef(new byte[16], 0, 0);
                    }
                    else
                    {
                        if (term.Length == term.Bytes.Length)
                        {
                            term.Grow(term.Length + 1);
                        }
                        term.Bytes[term.Length++] = (byte)label;
                    }
                    return term;
                }

                private BytesRef Shrink()
                {
                    if (term.Length == 0)
                    {
                        term = null;
                    }
                    else
                    {
                        term.Length--;
                    }
                    return term;
                }
            }
        }

        // LUCENENET specific - removed Walk<T>(FST<T> fst) because it is dead code

        public override long RamBytesUsed()
        {
            long ramBytesUsed = 0;
            foreach (TermsReader r in fields.Values)
            {
                if (r.index != null)
                {
                    ramBytesUsed += r.index.GetSizeInBytes();
                    ramBytesUsed += RamUsageEstimator.SizeOf(r.metaBytesBlock);
                    ramBytesUsed += RamUsageEstimator.SizeOf(r.metaLongsBlock);
                    ramBytesUsed += RamUsageEstimator.SizeOf(r.skipInfo);
                    ramBytesUsed += RamUsageEstimator.SizeOf(r.statsBlock);
                }
            }
            return ramBytesUsed;
        }

        public override void CheckIntegrity()
        {
            postingsReader.CheckIntegrity();
        }
    }
}