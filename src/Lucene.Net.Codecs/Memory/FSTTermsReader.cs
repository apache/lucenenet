using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
    using ByteArrayDataInput = Store.ByteArrayDataInput;
    using ByteRunAutomaton = Util.Automaton.ByteRunAutomaton;
    using BytesRef = Util.BytesRef;
    using CompiledAutomaton = Util.Automaton.CompiledAutomaton;
    using CorruptIndexException = Index.CorruptIndexException;
    using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
    using DocsEnum = Index.DocsEnum;
    using FieldInfo = Index.FieldInfo;
    using FieldInfos = Index.FieldInfos;
    using IBits = Util.IBits;
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
    /// <para/>
    /// The FST directly maps each term and its metadata,
    /// it is memory resident.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class FSTTermsReader : FieldsProducer
    {
        // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
        private readonly IDictionary<string, TermsReader> fields = new JCG.SortedDictionary<string, TermsReader>(StringComparer.Ordinal);
        private readonly PostingsReaderBase postingsReader;
        //static boolean TEST = false;
        private readonly int version;

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
                int numFields = @in.ReadVInt32();
                for (int i = 0; i < numFields; i++)
                {
                    int fieldNumber = @in.ReadVInt32();
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);
                    long numTerms = @in.ReadVInt64();
                    long sumTotalTermFreq = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY ? -1 : @in.ReadVInt64();
                    long sumDocFreq = @in.ReadVInt64();
                    int docCount = @in.ReadVInt32();
                    int longsSize = @in.ReadVInt32();
                    TermsReader current = new TermsReader(this, fieldInfo, @in, numTerms, sumTotalTermFreq, sumDocFreq, docCount, longsSize);
                    // LUCENENET NOTE: This simulates a put operation in Java,
                    // getting the prior value first before setting it.
                    fields.TryGetValue(fieldInfo.Name, out TermsReader previous);
                    fields[fieldInfo.Name] = current;
                    CheckFieldSummary(state.SegmentInfo, @in, current, previous);
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(@in);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(@in);
                }
            }
        }

        private static int ReadHeader(IndexInput @in) // LUCENENET: CA1822: Mark members as static
        {
            return CodecUtil.CheckHeader(@in, FSTTermsWriter.TERMS_CODEC_NAME, FSTTermsWriter.TERMS_VERSION_START, FSTTermsWriter.TERMS_VERSION_CURRENT);
        }

        private void SeekDir(IndexInput @in)
        {
            if (version >= FSTTermsWriter.TERMS_VERSION_CHECKSUM)
            {
                @in.Seek(@in.Length - CodecUtil.FooterLength() - 8);
            }
            else
            {
                @in.Seek(@in.Length - 8);
            }
            @in.Seek(@in.ReadInt64());
        }


        private static void CheckFieldSummary(SegmentInfo info, IndexInput @in, TermsReader field, TermsReader previous) // LUCENENET: CA1822: Mark members as static
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
            private readonly FSTTermsReader outerInstance;

            internal readonly FieldInfo fieldInfo;
            private readonly long numTerms;
            internal readonly long sumTotalTermFreq;
            internal readonly long sumDocFreq;
            internal readonly int docCount;
            //private readonly int longsSize; // LUCENENET: Not used
            internal readonly FST<FSTTermOutputs.TermData> dict;

            internal TermsReader(FSTTermsReader outerInstance, FieldInfo fieldInfo, IndexInput @in, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize)
            {
                this.outerInstance = outerInstance;
                this.fieldInfo = fieldInfo;
                this.numTerms = numTerms;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.sumDocFreq = sumDocFreq;
                this.docCount = docCount;
                //this.longsSize = longsSize; // LUCENENET: Not used
                this.dict = new FST<FSTTermOutputs.TermData>(@in, new FSTTermOutputs(fieldInfo, longsSize));
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
                private readonly FSTTermsReader.TermsReader outerInstance;

                /// <summary>Current term, null when enum ends or unpositioned.</summary>
                internal BytesRef term;

                /// <summary>Current term stats + decoded metadata (customized by PBF).</summary>
                internal readonly BlockTermState state;

                /// <summary>Current term stats + undecoded metadata (long[] &amp; byte[]).</summary>
                internal FSTTermOutputs.TermData meta;
                internal ByteArrayDataInput bytesReader;

                /// <summary>
                /// Decodes metadata into customized term state. </summary>
                internal abstract void DecodeMetaData();

                private protected BaseTermsEnum(FSTTermsReader.TermsReader outerInstance) // LUCENENET: Changed from internal to private protected
                {
                    this.outerInstance = outerInstance;
                    this.state = outerInstance.outerInstance.postingsReader.NewTermState();
                    this.bytesReader = new ByteArrayDataInput();
                    this.term = null;
                    // NOTE: metadata will only be initialized in child class
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

                public override void SeekExact(long ord)
                {
                    throw UnsupportedOperationException.Create();
                }

                public override long Ord => throw UnsupportedOperationException.Create();
            }


            // Iterates through all terms in this field
            private sealed class SegmentTermsEnum : BaseTermsEnum
            {
                private readonly FSTTermsReader.TermsReader outerInstance;

                private readonly BytesRefFSTEnum<FSTTermOutputs.TermData> fstEnum;

                /// <summary>True when current term's metadata is decoded.</summary>
                private bool decoded;

                /// <summary>True when current enum is 'positioned' by <see cref="SeekExact(BytesRef, TermState)"/>.</summary>
                private bool seekPending;

                internal SegmentTermsEnum(FSTTermsReader.TermsReader outerInstance)
                    : base(outerInstance)
                {
                    this.outerInstance = outerInstance;
                    this.fstEnum = new BytesRefFSTEnum<FSTTermOutputs.TermData>(outerInstance.dict);
                    this.decoded = false;
                    this.seekPending = false;
                    this.meta = null;
                }

                public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                // Let PBF decode metadata from long[] and byte[]
                internal override void DecodeMetaData()
                {
                    if (!decoded && !seekPending)
                    {
                        if (meta.bytes != null)
                        {
                            bytesReader.Reset(meta.bytes, 0, meta.bytes.Length);
                        }
                        outerInstance.outerInstance.postingsReader.DecodeTerm(meta.longs, bytesReader, outerInstance.fieldInfo, state, true);
                        decoded = true;
                    }
                }

                // Update current enum according to FSTEnum
                internal void UpdateEnum(BytesRefFSTEnum.InputOutput<FSTTermOutputs.TermData> pair)
                {
                    if (pair is null)
                    {
                        term = null;
                    }
                    else
                    {
                        term = pair.Input;
                        meta = pair.Output;
                        state.DocFreq = meta.docFreq;
                        state.TotalTermFreq = meta.totalTermFreq;
                    }
                    decoded = false;
                    seekPending = false;
                }

                public override bool MoveNext()
                {
                    if (seekPending) // previously positioned, but termOutputs not fetched
                    {
                        seekPending = false;
                        SeekStatus status = SeekCeil(term);
                        if (Debugging.AssertsEnabled) Debugging.Assert(status == SeekStatus.FOUND); // must positioned on valid term
                    }
                    // LUCENENET specific - extracted logic of UpdateEnum() so we can eliminate the null check
                    var moved = fstEnum.MoveNext();
                    if (moved)
                    {
                        var pair = fstEnum.Current;
                        term = pair.Input;
                        meta = pair.Output;
                        state.DocFreq = meta.docFreq;
                        state.TotalTermFreq = meta.totalTermFreq;
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
                private readonly FSTTermsReader.TermsReader outerInstance;

                /// <summary>True when current term's metadata is decoded.</summary>
                private bool decoded;

                /// <summary>True when there is pending term when calling <see cref="MoveNext()"/>.</summary>
                private bool pending;

                /// <summary>
                /// stack to record how current term is constructed,
                /// used to accumulate metadata or rewind term:
                ///   level == term.Length + 1,
                ///     == 0 when term is null
                /// </summary>
                private Frame[] stack;
                private int level;

                /// <summary>
                /// To which level the metadata is accumulated
                /// so that we can accumulate metadata lazily.
                /// </summary>
                private int metaUpto;

                /// <summary>Term dict fst.</summary>
                private readonly FST<FSTTermOutputs.TermData> fst;
                private readonly FST.BytesReader fstReader;
                private readonly Outputs<FSTTermOutputs.TermData> fstOutputs;

                /// <summary>Query automaton to intersect with.</summary>
                private readonly ByteRunAutomaton fsa;

                internal sealed class Frame
                {
                    /// <summary>Fst stats.</summary>
                    internal FST.Arc<FSTTermOutputs.TermData> fstArc;

                    /// <summary>Automaton stats.</summary>
                    internal int fsaState;

                    internal Frame()
                    {
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
                    this.fstReader = fst.GetBytesReader();
                    this.fstOutputs = outerInstance.dict.Outputs;
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

                    this.meta = null;
                    this.metaUpto = 1;
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

                public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                internal override void DecodeMetaData()
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(term != null);
                    if (!decoded)
                    {
                        if (meta.bytes != null)
                        {
                            bytesReader.Reset(meta.bytes, 0, meta.bytes.Length);
                        }
                        outerInstance.outerInstance.postingsReader.DecodeTerm(meta.longs, bytesReader, outerInstance.fieldInfo, state, true);
                        decoded = true;
                    }
                }

                /// <summary>
                /// Lazily accumulate meta data, when we got a accepted term. </summary>
                /// <exception cref="IOException"/>
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
                    state.DocFreq = meta.docFreq;
                    state.TotalTermFreq = meta.totalTermFreq;
                }

                public override SeekStatus SeekCeil(BytesRef target)
                {
                    decoded = false;
                    term = DoSeekCeil(target);
                    LoadMetaData();
                    if (term is null)
                    {
                        return SeekStatus.END;
                    }
                    else
                    {
                        return term.Equals(target) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
                    }
                }

                public override bool MoveNext()
                {
                    //if (TEST) System.out.println("Enum next()");
                    if (pending)
                    {
                        pending = false;
                        LoadMetaData();
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
                    LoadMetaData();
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
                        if (frame is null || frame.fstArc.Label != label)
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

                /// <summary> Virtual frame, never pop. </summary>
                private Frame LoadVirtualFrame(Frame frame)
                {
                    frame.fstArc.Output = fstOutputs.NoOutput;
                    frame.fstArc.NextFinalOutput = fstOutputs.NoOutput;
                    frame.fsaState = -1;
                    return frame;
                }

                /// <summary> Load frame for start arc(node) on fst. </summary>
                private Frame LoadFirstFrame(Frame frame)
                {
                    frame.fstArc = fst.GetFirstArc(frame.fstArc);
                    frame.fsaState = fsa.InitialState;
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
                    frame.fstArc = fst.ReadFirstRealTargetArc(top.fstArc.Target, frame.fstArc, fstReader);
                    frame.fsaState = fsa.Step(top.fsaState, frame.fstArc.Label);
                    //if (TEST) System.out.println(" loadExpand frame="+frame);
                    if (frame.fsaState == -1)
                    {
                        return LoadNextFrame(top, frame);
                    }
                    return frame;
                }

                /// <summary> Load frame for sibling arc(node) on fst. </summary>
                private Frame LoadNextFrame(Frame top, Frame frame)
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
                /// arc.label >= label and !fsa.reject(arc.label)
                /// </summary>
                private Frame LoadCeilFrame(int label, Frame top, Frame frame)
                {
                    FST.Arc<FSTTermOutputs.TermData> arc = frame.fstArc;
                    arc = Util.ReadCeilArc(label, fst, top.fstArc, arc, fstReader);
                    if (arc is null)
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

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private bool IsAccept(Frame frame) // reach a term both fst&fsa accepts
                {
                    return fsa.IsAccept(frame.fsaState) && frame.fstArc.IsFinal;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static bool IsValid(Frame frame) // reach a prefix both fst&fsa won't reject // LUCENENET: CA1822: Mark members as static
                {
                    return frame.fsaState != -1; //frame != null &&
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static bool CanGrow(Frame frame) // can walk forward on both fst&fsa // LUCENENET: CA1822: Mark members as static
                {
                    return frame.fsaState != -1 && FST<Memory.FSTTermOutputs.TermData>.TargetHasArcs(frame.fstArc);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static bool CanRewind(Frame frame) // can jump to sibling // LUCENENET: CA1822: Mark members as static
                {
                    return !frame.fstArc.IsLast;
                }

                private void PushFrame(Frame frame)
                {
                    term = Grow(frame.fstArc.Label);
                    level++;
                    //if (TEST) System.out.println("  term=" + term + " level=" + level);
                }

                private Frame PopFrame()
                {
                    term = Shrink();
                    level--;
                    metaUpto = metaUpto > level ? level : metaUpto;
                    //if (TEST) System.out.println("  term=" + term + " level=" + level);
                    return stack[level + 1];
                }

                private Frame NewFrame()
                {
                    if (level + 1 == stack.Length)
                    {
                        Frame[] temp = new Frame[ArrayUtil.Oversize(level + 2, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
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
                ramBytesUsed += r.dict is null ? 0 : r.dict.GetSizeInBytes();
            }
            return ramBytesUsed;
        }

        public override void CheckIntegrity()
        {
            postingsReader.CheckIntegrity();
        }
    }
}