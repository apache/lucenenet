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

using Lucene.Net.Support;

namespace Lucene.Net.Codecs.Memory
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Util.Fst;
    using ArrayUtil = Util.ArrayUtil;
    using Bits = Util.Bits;
    using ByteArrayDataInput = Store.ByteArrayDataInput;
    using ByteRunAutomaton = Util.Automaton.ByteRunAutomaton;
    using BytesRef = Util.BytesRef;
    using CompiledAutomaton = Util.Automaton.CompiledAutomaton;
    using CorruptIndexException = Index.CorruptIndexException;
    using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
    using DocsEnum = Index.DocsEnum;
    using FieldInfo = Index.FieldInfo;
    using FieldInfos = Index.FieldInfos;
    using IndexFileNames = Index.IndexFileNames;
    using IndexInput = Store.IndexInput;
    using IndexOptions = Index.IndexOptions;
    using IOUtils = Util.IOUtils;
    using RamUsageEstimator = Util.RamUsageEstimator;
    using SegmentInfo = Index.SegmentInfo;
    using SegmentReadState = Index.SegmentReadState;
    using Terms = Index.Terms;
    using TermsEnum = Index.TermsEnum;
    using TermState = Index.TermState;
    using Util = Util.Fst.Util;

    /// <summary>
    /// FST-based terms dictionary reader.
    /// 
    /// The FST directly maps each term and its metadata, 
    /// it is memory resident.
    /// 
    /// @lucene.experimental
    /// </summary>

    public class FSTTermsReader : FieldsProducer
    {
        internal readonly SortedDictionary<string, TermsReader> fields = new SortedDictionary<string, TermsReader>();
        internal readonly PostingsReaderBase postingsReader;
        //static boolean TEST = false;
        internal readonly int version;

        public FSTTermsReader(SegmentReadState state, PostingsReaderBase postingsReader)
        {
            string termsFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, FSTTermsWriter.TERMS_EXTENSION);

            this.postingsReader = postingsReader;
            IndexInput @in = state.Directory.OpenInput(termsFileName, state.Context);

            bool success = false;
            try
            {
                version = ReadHeader(@in);
                if (version >= FSTTermsWriter.TERMS_VERSION_CHECKSUM)
                {
                    CodecUtil.ChecksumEntireFile(@in);
                }
                this.postingsReader.Init(@in);
                SeekDir(@in);

                FieldInfos fieldInfos = state.FieldInfos;
                int numFields = @in.ReadVInt();
                for (int i = 0; i < numFields; i++)
                {
                    int fieldNumber = @in.ReadVInt();
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);
                    long numTerms = @in.ReadVLong();
                    long sumTotalTermFreq = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY ? -1 : @in.ReadVLong();
                    long sumDocFreq = @in.ReadVLong();
                    int docCount = @in.ReadVInt();
                    int longsSize = @in.ReadVInt();
                    TermsReader current = new TermsReader(this, fieldInfo, @in, numTerms, sumTotalTermFreq, sumDocFreq, docCount, longsSize);
                    TermsReader previous;
                    // LUCENENET NOTE: This simulates a put operation in Java,
                    // getting the prior value first before setting it.
                    fields.TryGetValue(fieldInfo.Name, out previous);
                    fields[fieldInfo.Name] = current;
                    CheckFieldSummary(state.SegmentInfo, @in, current, previous);
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(@in);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(@in);
                }
            }
        }

        private int ReadHeader(IndexInput @in)
        {
            return CodecUtil.CheckHeader(@in, FSTTermsWriter.TERMS_CODEC_NAME, FSTTermsWriter.TERMS_VERSION_START, FSTTermsWriter.TERMS_VERSION_CURRENT);
        }

        private void SeekDir(IndexInput @in)
        {
            if (version >= FSTTermsWriter.TERMS_VERSION_CHECKSUM)
            {
                @in.Seek(@in.Length() - CodecUtil.FooterLength() - 8);
            }
            else
            {
                @in.Seek(@in.Length() - 8);
            }
            @in.Seek(@in.ReadLong());
        }
        
        
        private void CheckFieldSummary(SegmentInfo info, IndexInput @in, TermsReader field, TermsReader previous)
        {
            // #docs with field must be <= #docs
            if (field.docCount < 0 || field.docCount > info.DocCount)
            {
                throw new CorruptIndexException("invalid docCount: " + field.docCount + " maxDoc: " + info.DocCount + " (resource=" + @in + ")");
            }
            // #postings must be >= #docs with field
            if (field.sumDocFreq < field.docCount)
            {
                throw new CorruptIndexException("invalid sumDocFreq: " + field.sumDocFreq + " docCount: " + field.docCount + " (resource=" + @in + ")");
            }
            // #positions must be >= #postings
            if (field.sumTotalTermFreq != -1 && field.sumTotalTermFreq < field.sumDocFreq)
            {
                throw new CorruptIndexException("invalid sumTotalTermFreq: " + field.sumTotalTermFreq + " sumDocFreq: " + field.sumDocFreq + " (resource=" + @in + ")");
            }
            if (previous != null)
            {
                throw new CorruptIndexException("duplicate fields: " + field.fieldInfo.Name + " (resource=" + @in + ")");
            }
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return Collections.UnmodifiableSet<string>(fields.Keys).GetEnumerator();
        }

        public override Terms Terms(string field)
        {
            Debug.Assert(field != null);
            return fields[field];
        }

        public override int Size
        {
            get { return fields.Count; }
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
            private readonly FSTTermsReader outerInstance;

            internal readonly FieldInfo fieldInfo;
            internal readonly long numTerms;
            internal readonly long sumTotalTermFreq;
            internal readonly long sumDocFreq;
            internal readonly int docCount;
            internal readonly int longsSize;
            internal readonly FST<FSTTermOutputs.TermData> dict;

            internal TermsReader(FSTTermsReader outerInstance, FieldInfo fieldInfo, IndexInput @in, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize)
            {
                this.outerInstance = outerInstance;
                this.fieldInfo = fieldInfo;
                this.numTerms = numTerms;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.sumDocFreq = sumDocFreq;
                this.docCount = docCount;
                this.longsSize = longsSize;
                this.dict = new FST<FSTTermOutputs.TermData>(@in, new FSTTermOutputs(fieldInfo, longsSize));
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
                get{ return fieldInfo.IndexOptions.Value.CompareTo(IndexOptions.DOCS_AND_FREQS) >= 0; }
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
                private readonly FSTTermsReader.TermsReader outerInstance;

                /* Current term, null when enum ends or unpositioned */
                internal BytesRef term_Renamed;

                /* Current term stats + decoded metadata (customized by PBF) */
                internal readonly BlockTermState state;

                /* Current term stats + undecoded metadata (long[] & byte[]) */
                internal FSTTermOutputs.TermData meta;
                internal ByteArrayDataInput bytesReader;

                /// <summary>
                /// Decodes metadata into customized term state </summary>
                internal abstract void DecodeMetaData();

                internal BaseTermsEnum(FSTTermsReader.TermsReader outerInstance)
                {
                    this.outerInstance = outerInstance;
                    this.state = outerInstance.outerInstance.postingsReader.NewTermState();
                    this.bytesReader = new ByteArrayDataInput();
                    this.term_Renamed = null;
                    // NOTE: metadata will only be initialized in child class
                }

                public override TermState TermState()
                {
                    DecodeMetaData();
                    return (TermState) state.Clone();
                }

                public override BytesRef Term
                {
                    get { return term_Renamed; }
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
                private readonly FSTTermsReader.TermsReader outerInstance;

                internal readonly BytesRefFSTEnum<FSTTermOutputs.TermData> fstEnum;

                /* True when current term's metadata is decoded */
                internal bool decoded;

                /* True when current enum is 'positioned' by seekExact(TermState) */
                internal bool seekPending;

                //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
                //ORIGINAL LINE: SegmentTermsEnum() throws java.io.IOException
                internal SegmentTermsEnum(FSTTermsReader.TermsReader outerInstance) : base(outerInstance)
                {
                    this.outerInstance = outerInstance;
                    this.fstEnum = new BytesRefFSTEnum<FSTTermOutputs.TermData>(outerInstance.dict);
                    this.decoded = false;
                    this.seekPending = false;
                    this.meta = null;
                }

                public override IComparer<BytesRef> Comparator
                {
                    get
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                }

                // Let PBF decode metadata from long[] and byte[]
                internal override void DecodeMetaData()
                {
                    if (!decoded && !seekPending)
                    {
                        if (meta.BYTES != null)
                        {
                            bytesReader.Reset(meta.BYTES, 0, meta.BYTES.Length);
                        }
                        outerInstance.outerInstance.postingsReader.DecodeTerm(meta.LONGS, bytesReader, outerInstance.fieldInfo, state, true);
                        decoded = true;
                    }
                }

                // Update current enum according to FSTEnum
                internal void UpdateEnum(BytesRefFSTEnum.InputOutput<FSTTermOutputs.TermData> pair)
                {
                    if (pair == null)
                    {
                        term_Renamed = null;
                    }
                    else
                    {
                        term_Renamed = pair.Input;
                        meta = pair.Output;
                        state.DocFreq = meta.DOC_FREQ;
                        state.TotalTermFreq = meta.TOTAL_TERM_FREQ;
                    }
                    decoded = false;
                    seekPending = false;
                }

                public override BytesRef Next()
                {
                    if (seekPending) // previously positioned, but termOutputs not fetched
                    {
                        seekPending = false;
                        SeekStatus status = SeekCeil(term_Renamed);
                        Debug.Assert(status == SeekStatus.FOUND); // must positioned on valid term
                    }
                    UpdateEnum(fstEnum.Next());
                    return term_Renamed;
                }

                public override bool SeekExact(BytesRef target)
                {
                    UpdateEnum(fstEnum.SeekExact(target));
                    return term_Renamed != null;
                }

                public override SeekStatus SeekCeil(BytesRef target)
                {
                    UpdateEnum(fstEnum.SeekCeil(target));
                    if (term_Renamed == null)
                    {
                        return SeekStatus.END;
                    }
                    else
                    {
                        return term_Renamed.Equals(target) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
                    }
                }

                public override void SeekExact(BytesRef target, TermState otherState)
                {
                    if (!target.Equals(term_Renamed))
                    {
                        state.CopyFrom(otherState);
                        term_Renamed = BytesRef.DeepCopyOf(target);
                        seekPending = true;
                    }
                }
            }

            // Iterates intersect result with automaton (cannot seek!)
            private sealed class IntersectTermsEnum : BaseTermsEnum
            {
                private readonly FSTTermsReader.TermsReader outerInstance;

                /* True when current term's metadata is decoded */
                internal bool decoded;

                /* True when there is pending term when calling next() */
                internal bool pending;

                /* stack to record how current term is constructed, 
                 * used to accumulate metadata or rewind term:
                 *   level == term.Length + 1,
                 *         == 0 when term is null */
                internal Frame[] stack;
                internal int level;

                /* to which level the metadata is accumulated 
                 * so that we can accumulate metadata lazily */
                internal int metaUpto;

                /* term dict fst */
                internal readonly FST<FSTTermOutputs.TermData> fst;
                internal readonly FST.BytesReader fstReader;
                internal readonly Outputs<FSTTermOutputs.TermData> fstOutputs;

                /* query automaton to intersect with */
                internal readonly ByteRunAutomaton fsa;

                internal sealed class Frame
                {
                    private readonly FSTTermsReader.TermsReader.IntersectTermsEnum outerInstance;

                    /* fst stats */
                    internal FST.Arc<FSTTermOutputs.TermData> fstArc;

                    /* automaton stats */
                    internal int fsaState;

                    internal Frame(FSTTermsReader.TermsReader.IntersectTermsEnum outerInstance)
                    {
                        this.outerInstance = outerInstance;
                        this.fstArc = new FST.Arc<FSTTermOutputs.TermData>();
                        this.fsaState = -1;
                    }

                    public override string ToString()
                    {
                        return "arc=" + fstArc + " state=" + fsaState;
                    }
                }

                internal IntersectTermsEnum(FSTTermsReader.TermsReader outerInstance, CompiledAutomaton compiled, BytesRef startTerm) : base(outerInstance)
                {
                    this.outerInstance = outerInstance;
                    //if (TEST) System.out.println("Enum init, startTerm=" + startTerm);
                    this.fst = outerInstance.dict;
                    this.fstReader = fst.BytesReader;
                    this.fstOutputs = outerInstance.dict.Outputs;
                    this.fsa = compiled.RunAutomaton;
                    this.level = -1;
                    this.stack = new Frame[16];
                    for (int i = 0; i < stack.Length; i++)
                    {
                        this.stack[i] = new Frame(this);
                    }

                    Frame frame;
                    frame = LoadVirtualFrame(NewFrame());
                    this.level++;
                    frame = LoadFirstFrame(NewFrame());
                    PushFrame(frame);

                    this.meta = null;
                    this.metaUpto = 1;
                    this.decoded = false;
                    this.pending = false;

                    if (startTerm == null)
                    {
                        pending = IsAccept(TopFrame());
                    }
                    else
                    {
                        DoSeekCeil(startTerm);
                        pending = !startTerm.Equals(term_Renamed) && IsValid(TopFrame()) && IsAccept(TopFrame());
                    }
                }

                public override IComparer<BytesRef> Comparator
                {
                    get
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                }

                internal override void DecodeMetaData()
                {
                    Debug.Assert(term_Renamed != null);
                    if (!decoded)
                    {
                        if (meta.BYTES != null)
                        {
                            bytesReader.Reset(meta.BYTES, 0, meta.BYTES.Length);
                        }
                        outerInstance.outerInstance.postingsReader.DecodeTerm(meta.LONGS, bytesReader, outerInstance.fieldInfo, state, true);
                        decoded = true;
                    }
                }

                /// <summary>
                /// Lazily accumulate meta data, when we got a accepted term </summary>
                //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
                //ORIGINAL LINE: void loadMetaData() throws java.io.IOException
                internal void LoadMetaData()
                {
                    FST.Arc<FSTTermOutputs.TermData> last, next;
                    last = stack[metaUpto].fstArc;
                    while (metaUpto != level)
                    {
                        metaUpto++;
                        next = stack[metaUpto].fstArc;
                        next.Output = fstOutputs.Add(next.Output, last.Output);
                        last = next;
                    }
                    if (last.IsFinal)
                    {
                        meta = fstOutputs.Add(last.Output, last.NextFinalOutput);
                    }
                    else
                    {
                        meta = last.Output;
                    }
                    state.DocFreq = meta.DOC_FREQ;
                    state.TotalTermFreq = meta.TOTAL_TERM_FREQ;
                }

                public override SeekStatus SeekCeil(BytesRef target)
                {
                    decoded = false;
                    term_Renamed = DoSeekCeil(target);
                    LoadMetaData();
                    if (term_Renamed == null)
                    {
                        return SeekStatus.END;
                    }
                    else
                    {
                        return term_Renamed.Equals(target) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
                    }
                }

                public override BytesRef Next()
                {
                    //if (TEST) System.out.println("Enum next()");
                    if (pending)
                    {
                        pending = false;
                        LoadMetaData();
                        return term_Renamed;
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
                    LoadMetaData();
                    return term_Renamed;
                }

                internal BytesRef DoSeekCeil(BytesRef target)
                {
                    //if (TEST) System.out.println("Enum doSeekCeil()");
                    Frame frame = null;
                    int label, upto = 0, limit = target.Length;
                    while (upto < limit) // to target prefix, or ceil label (rewind prefix)
                    {
                        frame = NewFrame();
                        label = target.Bytes[upto] & 0xff;
                        frame = LoadCeilFrame(label, TopFrame(), frame);
                        if (frame == null || frame.fstArc.Label != label)
                        {
                            break;
                        }
                        Debug.Assert(IsValid(frame)); // target must be fetched from automaton
                        PushFrame(frame);
                        upto++;
                    }
                    if (upto == limit) // got target
                    {
                        return term_Renamed;
                    }
                    if (frame != null) // got larger term('s prefix)
                    {
                        PushFrame(frame);
                        return IsAccept(frame) ? term_Renamed : Next();
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
                            return IsAccept(frame) ? term_Renamed : Next();
                        }
                    }
                    return null;
                }

                /// <summary> Virtual frame, never pop </summary>
                internal Frame LoadVirtualFrame(Frame frame)
                {
                    frame.fstArc.Output = fstOutputs.NoOutput;
                    frame.fstArc.NextFinalOutput = fstOutputs.NoOutput;
                    frame.fsaState = -1;
                    return frame;
                }

                /// <summary> Load frame for start arc(node) on fst </summary>
                internal Frame LoadFirstFrame(Frame frame)
                {
                    frame.fstArc = fst.GetFirstArc(frame.fstArc);
                    frame.fsaState = fsa.InitialState;
                    return frame;
                }

                /// <summary>
                /// Load frame for target arc(node) on fst </summary>
                internal Frame LoadExpandFrame(Frame top, Frame frame)
                {
                    if (!CanGrow(top))
                    {
                        return null;
                    }
                    frame.fstArc = fst.ReadFirstRealTargetArc(top.fstArc.Target, frame.fstArc, fstReader);
                    frame.fsaState = fsa.Step(top.fsaState, frame.fstArc.Label);
                    //if (TEST) System.out.println(" loadExpand frame="+frame);
                    if (frame.fsaState == -1)
                    {
                        return LoadNextFrame(top, frame);
                    }
                    return frame;
                }

                /// <summary> Load frame for sibling arc(node) on fst </summary>
                internal Frame LoadNextFrame(Frame top, Frame frame)
                {
                    if (!CanRewind(frame))
                    {
                        return null;
                    }
                    while (!frame.fstArc.IsLast)
                    {
                        frame.fstArc = fst.ReadNextRealArc(frame.fstArc, fstReader);
                        frame.fsaState = fsa.Step(top.fsaState, frame.fstArc.Label);
                        if (frame.fsaState != -1)
                        {
                            break;
                        }
                    }
                    //if (TEST) System.out.println(" loadNext frame="+frame);
                    if (frame.fsaState == -1)
                    {
                        return null;
                    }
                    return frame;
                }

                /// <summary>
                /// Load frame for target arc(node) on fst, so that 
                ///  arc.label >= label and !fsa.reject(arc.label) 
                /// </summary>
                internal Frame LoadCeilFrame(int label, Frame top, Frame frame)
                {
                    FST.Arc<FSTTermOutputs.TermData> arc = frame.fstArc;
                    arc = Util.ReadCeilArc(label, fst, top.fstArc, arc, fstReader);
                    if (arc == null)
                    {
                        return null;
                    }
                    frame.fsaState = fsa.Step(top.fsaState, arc.Label);
                    //if (TEST) System.out.println(" loadCeil frame="+frame);
                    if (frame.fsaState == -1)
                    {
                        return LoadNextFrame(top, frame);
                    }
                    return frame;
                }

                internal bool IsAccept(Frame frame) // reach a term both fst&fsa accepts
                {
                    return fsa.IsAccept(frame.fsaState) && frame.fstArc.IsFinal;
                }
                internal bool IsValid(Frame frame) // reach a prefix both fst&fsa won't reject
                {
                    return frame.fsaState != -1; //frame != null &&
                }
                internal bool CanGrow(Frame frame) // can walk forward on both fst&fsa
                {
                    return frame.fsaState != -1 && FST<Memory.FSTTermOutputs.TermData>.TargetHasArcs(frame.fstArc);
                }
                internal bool CanRewind(Frame frame) // can jump to sibling
                {
                    return !frame.fstArc.IsLast;
                }

                internal void PushFrame(Frame frame)
                {
                    term_Renamed = Grow(frame.fstArc.Label);
                    level++;
                    //if (TEST) System.out.println("  term=" + term + " level=" + level);
                }

                internal Frame PopFrame()
                {
                    term_Renamed = Shrink();
                    level--;
                    metaUpto = metaUpto > level ? level : metaUpto;
                    //if (TEST) System.out.println("  term=" + term + " level=" + level);
                    return stack[level + 1];
                }

                internal Frame NewFrame()
                {
                    if (level + 1 == stack.Length)
                    {
                        Frame[] temp = new Frame[ArrayUtil.Oversize(level + 2, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                        Array.Copy(stack, 0, temp, 0, stack.Length);
                        for (int i = stack.Length; i < temp.Length; i++)
                        {
                            temp[i] = new Frame(this);
                        }
                        stack = temp;
                    }
                    return stack[level + 1];
                }

                internal Frame TopFrame()
                {
                    return stack[level];
                }

                internal BytesRef Grow(int label)
                {
                    if (term_Renamed == null)
                    {
                        term_Renamed = new BytesRef(new byte[16], 0, 0);
                    }
                    else
                    {
                        if (term_Renamed.Length == term_Renamed.Bytes.Length)
                        {
                            term_Renamed.Grow(term_Renamed.Length + 1);
                        }
                        term_Renamed.Bytes[term_Renamed.Length++] = (byte)label;
                    }
                    return term_Renamed;
                }

                internal BytesRef Shrink()
                {
                    if (term_Renamed.Length == 0)
                    {
                        term_Renamed = null;
                    }
                    else
                    {
                        term_Renamed.Length--;
                    }
                    return term_Renamed;
                }
            }
        }

        internal static void Walk<T>(FST<T> fst)
        {
            List<FST.Arc<T>> queue = new List<FST.Arc<T>>();
            FST.BytesReader reader = fst.BytesReader;
            FST.Arc<T> startArc = fst.GetFirstArc(new FST.Arc<T>());
            queue.Add(startArc);
            BitArray seen = new BitArray(queue.Count);
            while (queue.Count > 0)
            {
                FST.Arc<T> arc = queue[0];
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
                ramBytesUsed += r.dict == null ? 0 : r.dict.SizeInBytes();
            }
            return ramBytesUsed;
        }

        public override void CheckIntegrity()
        {
            postingsReader.CheckIntegrity();
        }
    }

}