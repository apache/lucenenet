using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    using ByteSequenceOutputs = Util.Fst.ByteSequenceOutputs;
    using BytesRef = Util.BytesRef;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
    using DocsEnum = Index.DocsEnum;
    using FieldInfo = Index.FieldInfo;
    using FieldInfos = Index.FieldInfos;
    using FST = Util.Fst.FST;
    using IBits = Util.IBits;
    using IndexFileNames = Index.IndexFileNames;
    using IndexInput = Store.IndexInput;
    using IndexOptions = Index.IndexOptions;
    using IndexOutput = Store.IndexOutput;
    using Int32sRef = Util.Int32sRef;
    using IOContext = Store.IOContext;
    using IOUtils = Util.IOUtils;
    using PackedInt32s = Util.Packed.PackedInt32s;
    using RAMOutputStream = Store.RAMOutputStream;
    using RamUsageEstimator = Util.RamUsageEstimator;
    using SegmentReadState = Index.SegmentReadState;
    using SegmentWriteState = Index.SegmentWriteState;
    using Terms = Index.Terms;
    using TermsEnum = Index.TermsEnum;
    using Util = Util.Fst.Util;

    // TODO: would be nice to somehow allow this to act like
    // InstantiatedIndex, by never writing to disk; ie you write
    // to this Codec in RAM only and then when you open a reader
    // it pulls the FST directly from what you wrote w/o going
    // to disk.

    /// <summary>
    /// Stores terms &amp; postings (docs, positions, payloads) in
    /// RAM, using an FST.
    /// 
    /// <para>Note that this codec implements advance as a linear
    /// scan!  This means if you store large fields in here,
    /// queries that rely on advance will (AND BooleanQuery,
    /// PhraseQuery) will be relatively slow!
    /// </para>
    /// @lucene.experimental 
    /// </summary>

    // TODO: Maybe name this 'Cached' or something to reflect
    // the reality that it is actually written to disk, but
    // loads itself in ram?
    [PostingsFormatName("Memory")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class MemoryPostingsFormat : PostingsFormat
    {
        private readonly bool doPackFST;
        private readonly float acceptableOverheadRatio;

        public MemoryPostingsFormat() 
            : this(false, PackedInt32s.DEFAULT)
        {
        }

        /// <summary>
        /// Create <see cref="MemoryPostingsFormat"/>, specifying advanced FST options. </summary>
        /// <param name="doPackFST"> <c>true</c> if a packed FST should be built.
        ///        NOTE: packed FSTs are limited to ~2.1 GB of postings. </param>
        /// <param name="acceptableOverheadRatio"> Allowable overhead for packed <see cref="int"/>s
        ///        during FST construction. </param>
        public MemoryPostingsFormat(bool doPackFST, float acceptableOverheadRatio) 
            : base()
        {
            this.doPackFST = doPackFST;
            this.acceptableOverheadRatio = acceptableOverheadRatio;
        }

        public override string ToString()
        {
            return "PostingsFormat(name=" + Name + " doPackFST= " + doPackFST + ")";
        }

        private sealed class TermsWriter : TermsConsumer
        {
            private readonly IndexOutput @out;
            private readonly FieldInfo field;
            private readonly Builder<BytesRef> builder;
            private readonly ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
            //private readonly bool doPackFST; // LUCENENET: Never read
            //private readonly float acceptableOverheadRatio; // LUCENENET: Never read
            private int termCount;

            public TermsWriter(IndexOutput @out, FieldInfo field, bool doPackFST, float acceptableOverheadRatio)
            {
                postingsWriter = new PostingsWriter(this);

                this.@out = @out;
                this.field = field;
                //this.doPackFST = doPackFST; // LUCENENET: Never read
                //this.acceptableOverheadRatio = acceptableOverheadRatio; // LUCENENET: Never read
                builder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, doPackFST, acceptableOverheadRatio, true, 15);
            }

            private class PostingsWriter : PostingsConsumer
            {
                private readonly MemoryPostingsFormat.TermsWriter outerInstance;

                public PostingsWriter(MemoryPostingsFormat.TermsWriter outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                private int lastDocID;
                private int lastPos;
                private int lastPayloadLen;

                // NOTE: not private so we don't pay access check at runtime:
                internal int docCount;
                internal RAMOutputStream buffer = new RAMOutputStream();

                private int lastOffsetLength;
                private int lastOffset;

                public override void StartDoc(int docID, int termDocFreq)
                {
                    int delta = docID - lastDocID;
                    if (Debugging.AssertsEnabled) Debugging.Assert(docID == 0 || delta > 0);
                    lastDocID = docID;
                    docCount++;

                    if (outerInstance.field.IndexOptions == IndexOptions.DOCS_ONLY)
                    {
                        buffer.WriteVInt32(delta);
                    }
                    else if (termDocFreq == 1)
                    {
                        buffer.WriteVInt32((delta << 1) | 1);
                    }
                    else
                    {
                        buffer.WriteVInt32(delta << 1);
                        if (Debugging.AssertsEnabled) Debugging.Assert(termDocFreq > 0);
                        buffer.WriteVInt32(termDocFreq);
                    }

                    lastPos = 0;
                    lastOffset = 0;
                }

                public override void AddPosition(int pos, BytesRef payload, int startOffset, int endOffset)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(payload is null || outerInstance.field.HasPayloads);

                    //System.out.println("      addPos pos=" + pos + " payload=" + payload);

                    int delta = pos - lastPos;
                    if (Debugging.AssertsEnabled) Debugging.Assert(delta >= 0);
                    lastPos = pos;

                    int payloadLen = 0;

                    if (outerInstance.field.HasPayloads)
                    {
                        payloadLen = payload is null ? 0 : payload.Length;
                        if (payloadLen != lastPayloadLen)
                        {
                            lastPayloadLen = payloadLen;
                            buffer.WriteVInt32((delta << 1) | 1);
                            buffer.WriteVInt32(payloadLen);
                        }
                        else
                        {
                            buffer.WriteVInt32(delta << 1);
                        }
                    }
                    else
                    {
                        buffer.WriteVInt32(delta);
                    }

                    // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                    if (IndexOptionsComparer.Default.Compare(outerInstance.field.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0)
                    {
                        // don't use startOffset - lastEndOffset, because this creates lots of negative vints for synonyms,
                        // and the numbers aren't that much smaller anyways.
                        int offsetDelta = startOffset - lastOffset;
                        int offsetLength = endOffset - startOffset;
                        if (offsetLength != lastOffsetLength)
                        {
                            buffer.WriteVInt32(offsetDelta << 1 | 1);
                            buffer.WriteVInt32(offsetLength);
                        }
                        else
                        {
                            buffer.WriteVInt32(offsetDelta << 1);
                        }
                        lastOffset = startOffset;
                        lastOffsetLength = offsetLength;
                    }

                    if (payloadLen > 0)
                    {
                        buffer.WriteBytes(payload.Bytes, payload.Offset, payloadLen);
                    }
                }

                public override void FinishDoc()
                {
                }

                public virtual PostingsWriter Reset()
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(buffer.Position == 0); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    lastDocID = 0;
                    docCount = 0;
                    lastPayloadLen = 0;
                    lastOffsetLength = -1;
                    return this;
                }
            }

            private readonly PostingsWriter postingsWriter;

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                return postingsWriter.Reset();
            }

            private readonly RAMOutputStream buffer2 = new RAMOutputStream();
            private readonly BytesRef spare = new BytesRef();
            private byte[] finalBuffer = new byte[128];

            private readonly Int32sRef scratchIntsRef = new Int32sRef();

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(postingsWriter.docCount == stats.DocFreq);

                if (Debugging.AssertsEnabled) Debugging.Assert(buffer2.Position == 0); // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                buffer2.WriteVInt32(stats.DocFreq);
                if (field.IndexOptions != IndexOptions.DOCS_ONLY)
                {
                    buffer2.WriteVInt64(stats.TotalTermFreq - stats.DocFreq);
                }
                int pos = (int)buffer2.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                buffer2.WriteTo(finalBuffer, 0);
                buffer2.Reset();

                int totalBytes = pos + (int)postingsWriter.buffer.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                if (totalBytes > finalBuffer.Length)
                {
                    finalBuffer = ArrayUtil.Grow(finalBuffer, totalBytes);
                }
                postingsWriter.buffer.WriteTo(finalBuffer, pos);
                postingsWriter.buffer.Reset();

                spare.Bytes = finalBuffer;
                spare.Length = totalBytes;

                //System.out.println("    finishTerm term=" + text.utf8ToString() + " " + totalBytes + " bytes totalTF=" + stats.totalTermFreq);
                //for(int i=0;i<totalBytes;i++) {
                //  System.out.println("      " + Integer.toHexString(finalBuffer[i]&0xFF));
                //}

                builder.Add(Util.ToInt32sRef(text, scratchIntsRef), BytesRef.DeepCopyOf(spare));
                termCount++;
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                if (termCount > 0)
                {
                    @out.WriteVInt32(termCount);
                    @out.WriteVInt32(field.Number);
                    if (field.IndexOptions != IndexOptions.DOCS_ONLY)
                    {
                        @out.WriteVInt64(sumTotalTermFreq);
                    }
                    @out.WriteVInt64(sumDocFreq);
                    @out.WriteVInt32(docCount);
                    FST<BytesRef> fst = builder.Finish();
                    fst.Save(@out);
                    //System.out.println("finish field=" + field.name + " fp=" + out.getFilePointer());
                }
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;
        }

        private const string EXTENSION = "ram"; // LUCENENET specific - made into const
        private const string CODEC_NAME = "MemoryPostings";
        private const int VERSION_START = 0;
        private const int VERSION_CURRENT = VERSION_START;

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {

            string fileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, EXTENSION);
            IndexOutput @out = state.Directory.CreateOutput(fileName, state.Context);
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(@out, CODEC_NAME, VERSION_CURRENT);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(@out);
                }
            }

            return new FieldsConsumerAnonymousClass(this, @out);
        }

        private sealed class FieldsConsumerAnonymousClass : FieldsConsumer
        {
            private readonly MemoryPostingsFormat outerInstance;

            private readonly IndexOutput @out;

            public FieldsConsumerAnonymousClass(MemoryPostingsFormat outerInstance, IndexOutput @out)
            {
                this.outerInstance = outerInstance;
                this.@out = @out;
            }

            public override TermsConsumer AddField(FieldInfo field)
            {
                //System.out.println("\naddField field=" + field.name);
                return new TermsWriter(@out, field, outerInstance.doPackFST, outerInstance.acceptableOverheadRatio);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // EOF marker:
                    try
                    {
                        @out.WriteVInt32(0);
                        CodecUtil.WriteFooter(@out);
                    }
                    finally
                    {
                        @out.Dispose();
                    }
                }
            }
        }

        private sealed class FSTDocsEnum : DocsEnum
        {
            private readonly IndexOptions indexOptions;
            private readonly bool storePayloads;
            private byte[] buffer = new byte[16];
            private readonly ByteArrayDataInput @in; // LUCENENET: marked readonly

            private IBits liveDocs;
            private int docUpto;
            private int docID = -1;
            private int accum;
            private int freq;
            private int payloadLen;
            private int numDocs;

            public FSTDocsEnum(IndexOptions indexOptions, bool storePayloads)
            {
                @in = new ByteArrayDataInput(buffer);

                this.indexOptions = indexOptions;
                this.storePayloads = storePayloads;
            }

            public bool CanReuse(IndexOptions indexOptions, bool storePayloads)
            {
                return indexOptions == this.indexOptions && storePayloads == this.storePayloads;
            }

            public FSTDocsEnum Reset(BytesRef bufferIn, IBits liveDocs, int numDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(numDocs > 0);
                if (buffer.Length < bufferIn.Length)
                {
                    buffer = ArrayUtil.Grow(buffer, bufferIn.Length);
                }
                @in.Reset(buffer, 0, bufferIn.Length);
                Arrays.Copy(bufferIn.Bytes, bufferIn.Offset, buffer, 0, bufferIn.Length);
                this.liveDocs = liveDocs;
                docID = -1;
                accum = 0;
                docUpto = 0;
                freq = 1;
                payloadLen = 0;
                this.numDocs = numDocs;
                return this;
            }

            public override int NextDoc()
            {
                while (true)
                {
                    //System.out.println("  nextDoc cycle docUpto=" + docUpto + " numDocs=" + numDocs + " fp=" + in.getPosition() + " this=" + this);
                    if (docUpto == numDocs)
                    {
                        // System.out.println("    END");
                        return docID = NO_MORE_DOCS;
                    }
                    docUpto++;
                    if (indexOptions == IndexOptions.DOCS_ONLY)
                    {
                        accum += @in.ReadVInt32();
                    }
                    else
                    {
                        int code = @in.ReadVInt32();
                        accum += code.TripleShift(1);
                        //System.out.println("  docID=" + accum + " code=" + code);
                        if ((code & 1) != 0)
                        {
                            freq = 1;
                        }
                        else
                        {
                            freq = @in.ReadVInt32();
                            if (Debugging.AssertsEnabled) Debugging.Assert(freq > 0);
                        }

                        if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                        {
                            // Skip positions/payloads
                            for (int posUpto = 0; posUpto < freq; posUpto++)
                            {
                                if (!storePayloads)
                                {
                                    @in.ReadVInt32();
                                }
                                else
                                {
                                    int posCode = @in.ReadVInt32();
                                    if ((posCode & 1) != 0)
                                    {
                                        payloadLen = @in.ReadVInt32();
                                    }
                                    @in.SkipBytes(payloadLen);
                                }
                            }
                        }
                        else if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                        {
                            // Skip positions/offsets/payloads
                            for (int posUpto = 0; posUpto < freq; posUpto++)
                            {
                                int posCode = @in.ReadVInt32();
                                if (storePayloads && ((posCode & 1) != 0))
                                {
                                    payloadLen = @in.ReadVInt32();
                                }
                                if ((@in.ReadVInt32() & 1) != 0)
                                {
                                    // new offset length
                                    @in.ReadVInt32();
                                }
                                if (storePayloads)
                                {
                                    @in.SkipBytes(payloadLen);
                                }
                            }
                        }
                    }

                    if (liveDocs is null || liveDocs.Get(accum))
                    {
                        //System.out.println("    return docID=" + accum + " freq=" + freq);
                        return (docID = accum);
                    }
                }
            }

            public override int DocID => docID;

            public override int Advance(int target)
            {
                // TODO: we could make more efficient version, but, it
                // should be rare that this will matter in practice
                // since usually apps will not store "big" fields in
                // this codec!
                return SlowAdvance(target);
            }

            public override int Freq => freq;

            public override long GetCost()
            {
                return numDocs;
            }
        }

        private sealed class FSTDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly bool storePayloads;
            private byte[] buffer = new byte[16];
            private readonly ByteArrayDataInput @in; // LUCENENET: marked readonly

            private IBits liveDocs;
            private int docUpto;
            private int docID = -1;
            private int accum;
            private int freq;
            private int numDocs;
            private int posPending;
            private int payloadLength;
            private readonly bool storeOffsets;
            private int offsetLength;
            private int startOffset;

            private int pos;
            private readonly BytesRef payload = new BytesRef();

            public FSTDocsAndPositionsEnum(bool storePayloads, bool storeOffsets)
            {
                @in = new ByteArrayDataInput(buffer);

                this.storePayloads = storePayloads;
                this.storeOffsets = storeOffsets;
            }

            public bool CanReuse(bool storePayloads, bool storeOffsets)
            {
                return storePayloads == this.storePayloads && storeOffsets == this.storeOffsets;
            }

            public FSTDocsAndPositionsEnum Reset(BytesRef bufferIn, IBits liveDocs, int numDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(numDocs > 0);

                // System.out.println("D&P reset bytes this=" + this);
                // for(int i=bufferIn.offset;i<bufferIn.length;i++) {
                //   System.out.println("  " + Integer.toHexString(bufferIn.bytes[i]&0xFF));
                // }

                if (buffer.Length < bufferIn.Length)
                {
                    buffer = ArrayUtil.Grow(buffer, bufferIn.Length);
                }
                @in.Reset(buffer, 0, bufferIn.Length - bufferIn.Offset);
                Arrays.Copy(bufferIn.Bytes, bufferIn.Offset, buffer, 0, bufferIn.Length);
                this.liveDocs = liveDocs;
                docID = -1;
                accum = 0;
                docUpto = 0;
                payload.Bytes = buffer;
                payloadLength = 0;
                this.numDocs = numDocs;
                posPending = 0;
                startOffset = storeOffsets ? 0 : -1; // always return -1 if no offsets are stored
                offsetLength = 0;
                return this;
            }

            public override int NextDoc()
            {
                while (posPending > 0)
                {
                    NextPosition();
                }
                while (true)
                {
                    //System.out.println("  nextDoc cycle docUpto=" + docUpto + " numDocs=" + numDocs + " fp=" + in.getPosition() + " this=" + this);
                    if (docUpto == numDocs)
                    {
                        //System.out.println("    END");
                        return docID = NO_MORE_DOCS;
                    }
                    docUpto++;

                    int code = @in.ReadVInt32();
                    accum += code.TripleShift(1);
                    if ((code & 1) != 0)
                    {
                        freq = 1;
                    }
                    else
                    {
                        freq = @in.ReadVInt32();
                        if (Debugging.AssertsEnabled) Debugging.Assert(freq > 0);
                    }

                    if (liveDocs is null || liveDocs.Get(accum))
                    {
                        pos = 0;
                        startOffset = storeOffsets ? 0 : -1;
                        posPending = freq;
                        //System.out.println("    return docID=" + accum + " freq=" + freq);
                        return (docID = accum);
                    }

                    // Skip positions
                    for (int posUpto = 0; posUpto < freq; posUpto++)
                    {
                        if (!storePayloads)
                        {
                            @in.ReadVInt32();
                        }
                        else
                        {
                            int skipCode = @in.ReadVInt32();
                            if ((skipCode & 1) != 0)
                            {
                                payloadLength = @in.ReadVInt32();
                                //System.out.println("    new payloadLen=" + payloadLength);
                            }
                        }

                        if (storeOffsets)
                        {
                            if ((@in.ReadVInt32() & 1) != 0)
                            {
                                // new offset length
                                offsetLength = @in.ReadVInt32();
                            }
                        }

                        if (storePayloads)
                        {
                            @in.SkipBytes(payloadLength);
                        }
                    }
                }
            }

            public override int NextPosition()
            {
                //System.out.println("    nextPos storePayloads=" + storePayloads + " this=" + this);
                if (Debugging.AssertsEnabled) Debugging.Assert(posPending > 0);
                posPending--;
                if (!storePayloads)
                {
                    pos += @in.ReadVInt32();
                }
                else
                {
                    int code = @in.ReadVInt32();
                    pos += code.TripleShift(1);
                    if ((code & 1) != 0)
                    {
                        payloadLength = @in.ReadVInt32();
                        //System.out.println("      new payloadLen=" + payloadLength);
                        //} else {
                        //System.out.println("      same payloadLen=" + payloadLength);
                    }
                }

                if (storeOffsets)
                {
                    int offsetCode = @in.ReadVInt32();
                    if ((offsetCode & 1) != 0)
                    {
                        // new offset length
                        offsetLength = @in.ReadVInt32();
                    }
                    startOffset += offsetCode.TripleShift(1);
                }

                if (storePayloads)
                {
                    payload.Offset = @in.Position;
                    @in.SkipBytes(payloadLength);
                    payload.Length = payloadLength;
                }

                //System.out.println("      pos=" + pos + " payload=" + payload + " fp=" + in.getPosition());
                return pos;
            }

            public override int StartOffset => startOffset;

            public override int EndOffset => startOffset + offsetLength;

            public override BytesRef GetPayload()
            {
                return payload.Length > 0 ? payload : null;
            }

            public override int DocID => docID;

            public override int Advance(int target)
            {
                // TODO: we could make more efficient version, but, it
                // should be rare that this will matter in practice
                // since usually apps will not store "big" fields in
                // this codec!
                return SlowAdvance(target);
            }

            public override int Freq => freq;

            public override long GetCost()
            {
                return numDocs;
            }
        }

        private sealed class FSTTermsEnum : TermsEnum
        {
            private readonly FieldInfo field;
            private readonly BytesRefFSTEnum<BytesRef> fstEnum;
            private readonly ByteArrayDataInput buffer = new ByteArrayDataInput();
            private bool didDecode;

            private int docFreq;
            private long totalTermFreq;
            private BytesRefFSTEnum.InputOutput<BytesRef> current;
            private readonly BytesRef postingsSpare = new BytesRef(); // LUCENENET: marked readonly

            public FSTTermsEnum(FieldInfo field, FST<BytesRef> fst)
            {
                this.field = field;
                fstEnum = new BytesRefFSTEnum<BytesRef>(fst);
            }

            private void DecodeMetaData()
            {
                if (!didDecode)
                {
                    buffer.Reset(current.Output.Bytes, current.Output.Offset, current.Output.Length);
                    docFreq = buffer.ReadVInt32();
                    if (field.IndexOptions != IndexOptions.DOCS_ONLY)
                    {
                        totalTermFreq = docFreq + buffer.ReadVInt64();
                    }
                    else
                    {
                        totalTermFreq = -1;
                    }
                    postingsSpare.Bytes = current.Output.Bytes;
                    postingsSpare.Offset = buffer.Position;
                    postingsSpare.Length = current.Output.Length - (buffer.Position - current.Output.Offset);
                    //System.out.println("  df=" + docFreq + " totTF=" + totalTermFreq + " offset=" + buffer.getPosition() + " len=" + current.output.length);
                    didDecode = true;
                }
            }

            public override bool SeekExact(BytesRef text)
            {
                //System.out.println("te.seekExact text=" + field.name + ":" + text.utf8ToString() + " this=" + this);
                current = fstEnum.SeekExact(text);
                didDecode = false;
                return current != null;
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                //System.out.println("te.seek text=" + field.name + ":" + text.utf8ToString() + " this=" + this);
                current = fstEnum.SeekCeil(text);
                if (current is null)
                {
                    return SeekStatus.END;
                }
                else
                {

                    // System.out.println("  got term=" + current.input.utf8ToString());
                    // for(int i=0;i<current.output.length;i++) {
                    //   System.out.println("    " + Integer.toHexString(current.output.bytes[i]&0xFF));
                    // }

                    didDecode = false;

                    if (text.Equals(current.Input))
                    {
                        //System.out.println("  found!");
                        return SeekStatus.FOUND;
                    }
                    else
                    {
                        //System.out.println("  not found: " + current.input.utf8ToString());
                        return SeekStatus.NOT_FOUND;
                    }
                }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                DecodeMetaData();

                if (reuse is null || !(reuse is FSTDocsEnum docsEnum) || !docsEnum.CanReuse(field.IndexOptions, field.HasPayloads))
                    docsEnum = new FSTDocsEnum(field.IndexOptions, field.HasPayloads);

                return docsEnum.Reset(this.postingsSpare, liveDocs, docFreq);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                bool hasOffsets = IndexOptionsComparer.Default.Compare(field.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                if (IndexOptionsComparer.Default.Compare(field.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                {
                    return null;
                }
                DecodeMetaData();
                if (reuse is null || !(reuse is FSTDocsAndPositionsEnum docsAndPositionsEnum) || !docsAndPositionsEnum.CanReuse(field.HasPayloads, hasOffsets))
                    docsAndPositionsEnum = new FSTDocsAndPositionsEnum(field.HasPayloads, hasOffsets);

                //System.out.println("D&P reset this=" + this);
                return docsAndPositionsEnum.Reset(postingsSpare, liveDocs, docFreq);
            }

            public override BytesRef Term => current.Input;

            public override bool MoveNext()
            {
                //System.out.println("te.next");
                if (fstEnum.MoveNext())
                {
                    current = fstEnum.Current;
                    didDecode = false;
                    //System.out.println("  term=" + field.name + ":" + current.input.utf8ToString());
                    return current != null;
                }
                current = null;
                //System.out.println("  END");
                return false;
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (MoveNext())
                    return current.Input;
                return null;
            }

            public override int DocFreq
            {
                get
                {
                    DecodeMetaData();
                    return docFreq;
                }
            }

            public override long TotalTermFreq
            {
                get
                {
                    DecodeMetaData();
                    return totalTermFreq;
                }
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override void SeekExact(long ord)
            {
                // NOTE: we could add this...
                throw UnsupportedOperationException.Create();
            }

            public override long Ord =>
                // NOTE: we could add this...
                throw UnsupportedOperationException.Create();
        }

        private sealed class TermsReader : Terms
        {
            private readonly long sumTotalTermFreq;
            private readonly long sumDocFreq;
            private readonly int docCount;
            private readonly int termCount;
            internal FST<BytesRef> fst;
            private readonly ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
            internal readonly FieldInfo field;

            public TermsReader(FieldInfos fieldInfos, IndexInput @in, int termCount)
            {
                this.termCount = termCount;
                int fieldNumber = @in.ReadVInt32();
                field = fieldInfos.FieldInfo(fieldNumber);
                if (field.IndexOptions != IndexOptions.DOCS_ONLY)
                {
                    sumTotalTermFreq = @in.ReadVInt64();
                }
                else
                {
                    sumTotalTermFreq = -1;
                }
                sumDocFreq = @in.ReadVInt64();
                docCount = @in.ReadVInt32();

                fst = new FST<BytesRef>(@in, outputs);
            }

            public override long SumTotalTermFreq => sumTotalTermFreq;

            public override long SumDocFreq => sumDocFreq;

            public override int DocCount => docCount;

            public override long Count => termCount;

            public override TermsEnum GetEnumerator()
            {
                return new FSTTermsEnum(field, fst);
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            public override bool HasFreqs => IndexOptionsComparer.Default.Compare(field.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;

            public override bool HasOffsets => IndexOptionsComparer.Default.Compare(field.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

            public override bool HasPositions => IndexOptionsComparer.Default.Compare(field.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;

            public override bool HasPayloads => field.HasPayloads;

            public long RamBytesUsed()
            {
                return ((fst != null) ? fst.GetSizeInBytes() : 0);
            }
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            string fileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, EXTENSION);
            ChecksumIndexInput @in = state.Directory.OpenChecksumInput(fileName, IOContext.READ_ONCE);

            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            var fields = new JCG.SortedDictionary<string, TermsReader>(StringComparer.Ordinal);

            try
            {
                CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_CURRENT);
                while (true)
                {
                    int termCount = @in.ReadVInt32();
                    if (termCount == 0)
                    {
                        break;
                    }

                    TermsReader termsReader = new TermsReader(state.FieldInfos, @in, termCount);
                    // System.out.println("load field=" + termsReader.field.name);
                    fields.Add(termsReader.field.Name, termsReader);
                }
                CodecUtil.CheckFooter(@in);
            }
            finally
            {
                @in.Dispose();
            }

            return new FieldsProducerAnonymousClass(fields);
        }

        private sealed class FieldsProducerAnonymousClass : FieldsProducer
        {
            private readonly IDictionary<string, TermsReader> _fields;

            public FieldsProducerAnonymousClass(IDictionary<string, TermsReader> fields)
            {
                _fields = fields;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return _fields.Keys.GetEnumerator(); // LUCENENET NOTE: enumerators are not writable in .NET
            }

            public override Terms GetTerms(string field)
            {
                _fields.TryGetValue(field, out TermsReader result);
                return result;
            }

            public override int Count => _fields.Count;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // Drop ref to FST:
                    foreach (var field in _fields)
                    {
                        field.Value.fst = null;
                    }
                }
            }

            public override long RamBytesUsed()
            {
                long sizeInBytes = 0;
                foreach (var entry in _fields)
                {
                    sizeInBytes += (entry.Key.Length * RamUsageEstimator.NUM_BYTES_CHAR);
                    sizeInBytes += entry.Value.RamBytesUsed();
                }
                return sizeInBytes;
            }

            public override void CheckIntegrity()
            {
            }
        }
    }
}