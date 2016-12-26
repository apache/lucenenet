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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using Lucene.Net.Util.Automaton;
    using Lucene.Net.Util.Fst;

    /// <summary>
    /// FST-based terms dictionary reader.
    /// 
    /// The FST index maps each term and its ord, and during seek 
    /// the ord is used fetch metadata from a single block.
    /// The term dictionary is fully memory resident.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class FSTOrdTermsReader : FieldsProducer
    {
        internal const int INTERVAL = FSTOrdTermsWriter.SKIP_INTERVAL;
        internal readonly SortedDictionary<string, TermsReader> fields = new SortedDictionary<string, TermsReader>();
        internal readonly PostingsReaderBase postingsReader;
        internal int version;
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
                int numFields = blockIn.ReadVInt();
                for (int i = 0; i < numFields; i++)
                {
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(blockIn.ReadVInt());
                    bool hasFreq = fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY;
                    long numTerms = blockIn.ReadVLong();
                    long sumTotalTermFreq = hasFreq ? blockIn.ReadVLong() : -1;
                    long sumDocFreq = blockIn.ReadVLong();
                    int docCount = blockIn.ReadVInt();
                    int longsSize = blockIn.ReadVInt();
                    var index = new FST<long?>(indexIn, PositiveIntOutputs.Singleton);

                    var current = new TermsReader(this, fieldInfo, blockIn, numTerms, sumTotalTermFreq, sumDocFreq, docCount, longsSize, index);
                    TermsReader previous;
                    // LUCENENET NOTE: This simulates a put operation in Java,
                    // getting the prior value first before setting it.
                    fields.TryGetValue(fieldInfo.Name, out previous);
                    fields[fieldInfo.Name] = current;
                    CheckFieldSummary(state.SegmentInfo, indexIn, blockIn, current, previous);
                }
                if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
                {
                    CodecUtil.CheckFooter(indexIn);
                }
                else
                {
                    CodecUtil.CheckEOF(indexIn);
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(indexIn, blockIn);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(indexIn, blockIn);
                }
            }
        }

        private int ReadHeader(IndexInput @in)
        {
            return CodecUtil.CheckHeader(@in, FSTOrdTermsWriter.TERMS_CODEC_NAME, FSTOrdTermsWriter.TERMS_VERSION_START, FSTOrdTermsWriter.TERMS_VERSION_CURRENT);
        }

        private void SeekDir(IndexInput @in)
        {
            if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
            {
                @in.Seek(@in.Length() - CodecUtil.FooterLength() - 8);
            }
            else
            {
                @in.Seek(@in.Length() - 8);
            }
            @in.Seek(@in.ReadLong());
        }

        private static void CheckFieldSummary(SegmentInfo info, IndexInput indexIn, IndexInput blockIn, TermsReader field, TermsReader previous)
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
            return Collections.UnmodifiableSet(fields.Keys).GetEnumerator();
        }

        public override Terms Terms(string field)
        {
            Debug.Assert(field != null);
            return fields[field];
        }

        public override int Size
        {
            get
            {
                {
                    return fields.Count;
                }
            }
        }

        public override void Dispose()
        {
            try
            {
                IOUtils.Close(postingsReader);
            }
            finally
            {
                fields.Clear();
            }
        }

        internal sealed class TermsReader : Terms
        {
            private readonly FSTOrdTermsReader outerInstance;

            internal readonly FieldInfo fieldInfo;
            internal readonly long numTerms;
            internal readonly long sumTotalTermFreq;
            internal readonly long sumDocFreq;
            internal readonly int docCount;
            internal readonly int longsSize;
            internal readonly FST<long?> index;

            internal readonly int numSkipInfo;
            internal readonly long[] skipInfo;
            internal readonly byte[] statsBlock;
            internal readonly byte[] metaLongsBlock;
            internal readonly byte[] metaBytesBlock;

            internal TermsReader(FSTOrdTermsReader outerInstance, FieldInfo fieldInfo, IndexInput blockIn, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize, FST<long?> index)
            {
                this.outerInstance = outerInstance;
                this.fieldInfo = fieldInfo;
                this.numTerms = numTerms;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.sumDocFreq = sumDocFreq;
                this.docCount = docCount;
                this.longsSize = longsSize;
                this.index = index;

                Debug.Assert((numTerms & (~0xffffffffL)) == 0);
                int numBlocks = (int)(numTerms + INTERVAL - 1) / INTERVAL;
                this.numSkipInfo = longsSize + 3;
                this.skipInfo = new long[numBlocks * numSkipInfo];
                this.statsBlock = new byte[(int)blockIn.ReadVLong()];
                this.metaLongsBlock = new byte[(int)blockIn.ReadVLong()];
                this.metaBytesBlock = new byte[(int)blockIn.ReadVLong()];

                int last = 0, next = 0;
                for (int i = 1; i < numBlocks; i++)
                {
                    next = numSkipInfo * i;
                    for (int j = 0; j < numSkipInfo; j++)
                    {
                        skipInfo[next + j] = skipInfo[last + j] + blockIn.ReadVLong();
                    }
                    last = next;
                }
                blockIn.ReadBytes(statsBlock, 0, statsBlock.Length);
                blockIn.ReadBytes(metaLongsBlock, 0, metaLongsBlock.Length);
                blockIn.ReadBytes(metaBytesBlock, 0, metaBytesBlock.Length);
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

            public override bool HasFreqs
            {
                get { return fieldInfo.IndexOptions.Value.CompareTo(IndexOptions.DOCS_AND_FREQS) >= 0; }
            }

            public override bool HasOffsets
            {
                get { return fieldInfo.IndexOptions.Value.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0; }
            }

            public override bool HasPositions
            {
                get { return fieldInfo.IndexOptions.Value.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0; }
            }

            public override bool HasPayloads
            {
                get { return fieldInfo.HasPayloads; }
            }

            public override long Size
            {
                get { return numTerms; }
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

            public override TermsEnum Iterator(TermsEnum reuse)
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
                internal readonly ByteArrayDataInput statsReader = new ByteArrayDataInput();
                internal readonly ByteArrayDataInput metaLongsReader = new ByteArrayDataInput();
                internal readonly ByteArrayDataInput metaBytesReader = new ByteArrayDataInput();

                /* To which block is buffered */
                internal int statsBlockOrd;
                internal int metaBlockOrd;

                /* Current buffered metadata (long[] & byte[]) */
                internal long[][] longs;
                internal int[] bytesStart;
                internal int[] bytesLength;

                /* Current buffered stats (df & ttf) */
                internal int[] docFreq_Renamed;
                internal long[] totalTermFreq_Renamed;

                internal BaseTermsEnum(TermsReader outerInstance)
                {
                    this.outerInstance = outerInstance;
                    this.state = outerInstance.outerInstance.postingsReader.NewTermState();
                    this.term = null;
                    this.statsReader.Reset(outerInstance.statsBlock);
                    this.metaLongsReader.Reset(outerInstance.metaLongsBlock);
                    this.metaBytesReader.Reset(outerInstance.metaBytesBlock);

                    this.longs = RectangularArrays.ReturnRectangularLongArray(INTERVAL, outerInstance.longsSize);
                    this.bytesStart = new int[INTERVAL];
                    this.bytesLength = new int[INTERVAL];
                    this.docFreq_Renamed = new int[INTERVAL];
                    this.totalTermFreq_Renamed = new long[INTERVAL];
                    this.statsBlockOrd = -1;
                    this.metaBlockOrd = -1;
                    if (!outerInstance.HasFreqs)
                    {
                        Arrays.Fill(totalTermFreq_Renamed, -1);
                    }
                }

                public override IComparer<BytesRef> Comparator
                {
                    get
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                }

                /// <summary>
                /// Decodes stats data into term state </summary>
                internal virtual void DecodeStats()
                {
                    int upto = (int)ord % INTERVAL;
                    int oldBlockOrd = statsBlockOrd;
                    statsBlockOrd = (int)ord / INTERVAL;
                    if (oldBlockOrd != statsBlockOrd)
                    {
                        RefillStats();
                    }
                    state.DocFreq = docFreq_Renamed[upto];
                    state.TotalTermFreq = totalTermFreq_Renamed[upto];
                }

                /// <summary>
                /// Let PBF decode metadata </summary>
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
                /// Load current stats shard </summary>
                internal void RefillStats()
                {
                    var offset = statsBlockOrd * outerInstance.numSkipInfo;
                    var statsFP = (int)outerInstance.skipInfo[offset];
                    statsReader.Position = statsFP;
                    for (int i = 0; i < INTERVAL && !statsReader.Eof; i++)
                    {
                        int code = statsReader.ReadVInt();
                        if (outerInstance.HasFreqs)
                        {
                            docFreq_Renamed[i] = ((int)((uint)code >> 1));
                            if ((code & 1) == 1)
                            {
                                totalTermFreq_Renamed[i] = docFreq_Renamed[i];
                            }
                            else
                            {
                                totalTermFreq_Renamed[i] = docFreq_Renamed[i] + statsReader.ReadVLong();
                            }
                        }
                        else
                        {
                            docFreq_Renamed[i] = code;
                        }
                    }
                }

                /// <summary>
                /// Load current metadata shard </summary>
                internal void RefillMetadata()
                {
                    var offset = metaBlockOrd * outerInstance.numSkipInfo;
                    var metaLongsFP = (int)outerInstance.skipInfo[offset + 1];
                    var metaBytesFP = (int)outerInstance.skipInfo[offset + 2];
                    metaLongsReader.Position = metaLongsFP;
                    for (int j = 0; j < outerInstance.longsSize; j++)
                    {
                        longs[0][j] = outerInstance.skipInfo[offset + 3 + j] + metaLongsReader.ReadVLong();
                    }
                    bytesStart[0] = metaBytesFP;
                    bytesLength[0] = (int)metaLongsReader.ReadVLong();
                    for (int i = 1; i < INTERVAL && !metaLongsReader.Eof; i++)
                    {
                        for (int j = 0; j < outerInstance.longsSize; j++)
                        {
                            longs[i][j] = longs[i - 1][j] + metaLongsReader.ReadVLong();
                        }
                        bytesStart[i] = bytesStart[i - 1] + bytesLength[i - 1];
                        bytesLength[i] = (int)metaLongsReader.ReadVLong();
                    }
                }

                public override TermState TermState()
                {
                    DecodeMetaData();
                    return (TermState)state.Clone();
                }

                public override BytesRef Term
                {
                    get { return term; }
                }

                public override int DocFreq()
                {
                    return state.DocFreq;
                }

                public override long TotalTermFreq()
                {
                    return state.TotalTermFreq;
                }

                public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
                {
                    DecodeMetaData();
                    return outerInstance.outerInstance.postingsReader.Docs(outerInstance.fieldInfo, state, liveDocs, reuse, flags);
                }

                public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
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
                    throw new System.NotSupportedException();
                }

                public override long Ord()
                {
                    throw new System.NotSupportedException();
                }
            }

            // Iterates through all terms in this field
            private sealed class SegmentTermsEnum : BaseTermsEnum
            {
                private readonly FSTOrdTermsReader.TermsReader outerInstance;

                private readonly BytesRefFSTEnum<long?> fstEnum;

                /* True when current term's metadata is decoded */
                private bool decoded;

                /* True when current enum is 'positioned' by seekExact(TermState) */
                private bool seekPending;

                internal SegmentTermsEnum(FSTOrdTermsReader.TermsReader outerInstance) : base(outerInstance)
                {
                    this.outerInstance = outerInstance;
                    this.fstEnum = new BytesRefFSTEnum<long?>(outerInstance.index);
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
                private void UpdateEnum(BytesRefFSTEnum.InputOutput<long?> pair)
                {
                    if (pair == null)
                    {
                        term = null;
                    }
                    else
                    {
                        term = pair.Input;
                        ord = pair.Output.Value;
                        DecodeStats();
                    }
                    decoded = false;
                    seekPending = false;
                }

                public override BytesRef Next()
                {
                    if (seekPending) // previously positioned, but termOutputs not fetched
                    {
                        seekPending = false;
                        var status = SeekCeil(term);
                        Debug.Assert(status == SeekStatus.FOUND); // must positioned on valid term
                    }
                    UpdateEnum(fstEnum.Next());
                    return term;
                }

                public override bool SeekExact(BytesRef target)
                {
                    UpdateEnum(fstEnum.SeekExact(target));
                    return term != null;
                }

                public override SeekStatus SeekCeil(BytesRef target)
                {
                    UpdateEnum(fstEnum.SeekCeil(target));
                    if (term == null)
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
                private readonly FSTOrdTermsReader.TermsReader outerInstance;

                /* True when current term's metadata is decoded */
                private bool decoded;

                /* True when there is pending term when calling next() */
                private bool pending;

                /* stack to record how current term is constructed, 
                 * used to accumulate metadata or rewind term:
                 *   level == term.length + 1,
                 *         == 0 when term is null */
                private Frame[] stack;
                private int level;

                /* term dict fst */
                private readonly FST<long?> fst;
                private readonly FST.BytesReader fstReader;
                private readonly Outputs<long?> fstOutputs;

                /* query automaton to intersect with */
                private readonly ByteRunAutomaton fsa;

                private sealed class Frame
                {
                    /* fst stats */
                    internal FST.Arc<long?> arc;

                    /* automaton stats */
                    internal int state;

                    internal Frame()
                    {
                        this.arc = new FST.Arc<long?>();
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
                    this.outerInstance = outerInstance;
                    this.fst = outerInstance.index;
                    this.fstReader = fst.BytesReader;
                    this.fstOutputs = outerInstance.index.Outputs;
                    this.fsa = compiled.RunAutomaton;
                    this.level = -1;
                    this.stack = new Frame[16];
                    for (int i = 0; i < stack.Length; i++)
                    {
                        this.stack[i] = new Frame();
                    }

                    Frame frame;
                    frame = LoadVirtualFrame(NewFrame());
                    this.level++;
                    frame = LoadFirstFrame(NewFrame());
                    PushFrame(frame);

                    this.decoded = false;
                    this.pending = false;

                    if (startTerm == null)
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
                    Debug.Assert(arc.NextFinalOutput == fstOutputs.NoOutput);
                    ord = arc.Output.Value;
                    base.DecodeStats();
                }

                public override SeekStatus SeekCeil(BytesRef target)
                {
                    throw new System.NotSupportedException();
                }

                public override BytesRef Next()
                {
                    //if (TEST) System.out.println("Enum next()");
                    if (pending)
                    {
                        pending = false;
                        DecodeStats();
                        return term;
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
                        return null;
                    DFSContinue:;
                    }
                DFSBreak:
                    DecodeStats();
                    return term;
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
                        if (frame == null || frame.arc.Label != label)
                        {
                            break;
                        }
                        Debug.Assert(IsValid(frame)); // target must be fetched from automaton
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
                        return IsAccept(frame) ? term : Next();
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
                            return IsAccept(frame) ? term : Next();
                        }
                    }
                    return null;
                }

                /// <summary>
                /// Virtual frame, never pop </summary>
                private Frame LoadVirtualFrame(Frame frame)
                {
                    frame.arc.Output = fstOutputs.NoOutput;
                    frame.arc.NextFinalOutput = fstOutputs.NoOutput;
                    frame.state = -1;
                    return frame;
                }

                /// <summary>
                /// Load frame for start arc(node) on fst </summary>
                private Frame LoadFirstFrame(Frame frame)
                {
                    frame.arc = fst.GetFirstArc(frame.arc);
                    frame.state = fsa.InitialState;
                    return frame;
                }

                /// <summary>
                /// Load frame for target arc(node) on fst </summary>
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
                /// Load frame for sibling arc(node) on fst </summary>
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
                ///  arc.label >= label and !fsa.reject(arc.label) 
                /// </summary>
                private Frame LoadCeilFrame(int label, Frame top, Frame frame)
                {
                    var arc = frame.arc;
                    arc = Util.ReadCeilArc(label, fst, top.arc, arc, fstReader);
                    if (arc == null)
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

                private bool IsAccept(Frame frame) // reach a term both fst&fsa accepts
                {
                    return fsa.IsAccept(frame.state) && frame.arc.IsFinal;
                }

                private bool IsValid(Frame frame) // reach a prefix both fst&fsa won't reject
                {
                    return frame.state != -1; //frame != null &&
                }

                private bool CanGrow(Frame frame) // can walk forward on both fst&fsa
                {
                    return frame.state != -1 && FST<long?>.TargetHasArcs(frame.arc);
                }

                private bool CanRewind(Frame frame) // can jump to sibling
                {
                    return !frame.arc.IsLast;
                }

                private void PushFrame(Frame frame)
                {
                    var arc = frame.arc;
                    arc.Output = fstOutputs.Add(TopFrame().arc.Output, arc.Output);
                    term = Grow(arc.Label);
                    level++;
                    Debug.Assert(frame == stack[level]);
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
                        Array.Copy(stack, 0, temp, 0, stack.Length);
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
                    if (term == null)
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

        internal static void Walk<T>(FST<T> fst)
        {
            var queue = new List<FST.Arc<T>>();

            // Java version was BitSet(), but in .NET we don't have a zero contructor BitArray. 
            // Couldn't find the default size in BitSet, so went with zero here.
            var seen = new BitArray(0); 
            var reader = fst.BytesReader;
            var startArc = fst.GetFirstArc(new FST.Arc<T>());
            queue.Add(startArc);
            while (queue.Count > 0)
            {
                //FST.Arc<T> arc = queue.Remove(0);
                var arc = queue[0];
                queue.RemoveAt(0); 

                long node = arc.Target;
                //System.out.println(arc);
                if (FST<T>.TargetHasArcs(arc) && !seen.SafeGet((int)node))
                {
                    seen.SafeSet((int)node, true);
                    fst.ReadFirstRealTargetArc(node, arc, reader);
                    while (true)
                    {
                        queue.Add((new FST.Arc<T>()).CopyFrom(arc));
                        if (arc.IsLast)
                        {
                            break;
                        }
                        else
                        {
                            fst.ReadNextRealArc(arc, reader);
                        }
                    }
                }
            }
        }

        public override long RamBytesUsed()
        {
            long ramBytesUsed = 0;
            foreach (TermsReader r in fields.Values)
            {
                if (r.index != null)
                {
                    ramBytesUsed += r.index.SizeInBytes();
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