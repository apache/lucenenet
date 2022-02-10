using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Sep
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

    // TODO: -- should we switch "hasProx" higher up?  and
    // create two separate docs readers, one that also reads
    // prox and one that doesn't?

    /// <summary>
    /// Concrete class that reads the current doc/freq/skip
    /// postings format.    
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class SepPostingsReader : PostingsReaderBase
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly Int32IndexInput freqIn;
        private readonly Int32IndexInput docIn;
        private readonly Int32IndexInput posIn;
        private readonly IndexInput payloadIn;
        private readonly IndexInput skipIn;
#pragma warning restore CA2213 // Disposable fields should be disposed

        private int skipInterval;
        private int maxSkipLevels;
        private int skipMinimum;

        public SepPostingsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo segmentInfo, IOContext context,
            Int32StreamFactory intFactory, string segmentSuffix)
        {
            bool success = false;
            try
            {
                string docFileName = IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, SepPostingsWriter.DOC_EXTENSION);
                docIn = intFactory.OpenInput(dir, docFileName, context);

                skipIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, SepPostingsWriter.SKIP_EXTENSION), context);

                if (fieldInfos.HasFreq)
                {
                    freqIn = intFactory.OpenInput(dir, IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, SepPostingsWriter.FREQ_EXTENSION), context);
                }
                else
                {
                    freqIn = null;
                }
                if (fieldInfos.HasProx)
                {
                    posIn = intFactory.OpenInput(dir, IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, SepPostingsWriter.POS_EXTENSION), context);
                    payloadIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, SepPostingsWriter.PAYLOAD_EXTENSION), context);
                }
                else
                {
                    posIn = null;
                    payloadIn = null;
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    Dispose();
                }
            }
        }

        public override void Init(IndexInput termsIn)
        {
            // Make sure we are talking to the matching past writer
            CodecUtil.CheckHeader(termsIn, SepPostingsWriter.CODEC,
              SepPostingsWriter.VERSION_START, SepPostingsWriter.VERSION_START);
            skipInterval = termsIn.ReadInt32();
            maxSkipLevels = termsIn.ReadInt32();
            skipMinimum = termsIn.ReadInt32();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IOUtils.Dispose(freqIn, docIn, skipIn, posIn, payloadIn);
            }
        }

        internal sealed class SepTermState : BlockTermState
        {
            // We store only the seek point to the docs file because
            // the rest of the info (freqIndex, posIndex, etc.) is
            // stored in the docs file:
            internal Int32IndexInput.Index docIndex;
            internal Int32IndexInput.Index posIndex;
            internal Int32IndexInput.Index freqIndex;
            internal long payloadFP;
            internal long skipFP;

            public override object Clone()
            {
                var other = new SepTermState();
                other.CopyFrom(this);
                return other;
            }

            public override void CopyFrom(TermState other)
            {
                base.CopyFrom(other);
                SepTermState other_ = (SepTermState)other;
                if (docIndex is null)
                {
                    docIndex = (Int32IndexInput.Index)other_.docIndex.Clone();
                }
                else
                {
                    docIndex.CopyFrom(other_.docIndex);
                }
                if (other_.freqIndex != null)
                {
                    if (freqIndex is null)
                    {
                        freqIndex = (Int32IndexInput.Index)other_.freqIndex.Clone();
                    }
                    else
                    {
                        freqIndex.CopyFrom(other_.freqIndex);
                    }
                }
                else
                {
                    freqIndex = null;
                }
                if (other_.posIndex != null)
                {
                    if (posIndex is null)
                    {
                        posIndex = (Int32IndexInput.Index)other_.posIndex.Clone();
                    }
                    else
                    {
                        posIndex.CopyFrom(other_.posIndex);
                    }
                }
                else
                {
                    posIndex = null;
                }
                payloadFP = other_.payloadFP;
                skipFP = other_.skipFP;
            }

            public override string ToString()
            {
                return base.ToString() + " docIndex=" + docIndex + " freqIndex=" + freqIndex + " posIndex=" + posIndex +
                       " payloadFP=" + payloadFP + " skipFP=" + skipFP;
            }
        }

        public override BlockTermState NewTermState()
        {
            SepTermState state = new SepTermState();
            state.docIndex = docIn.GetIndex();
            if (freqIn != null)
            {
                state.freqIndex = freqIn.GetIndex();
            }
            if (posIn != null)
            {
                state.posIndex = posIn.GetIndex();
            }
            return state;
        }

        public override void DecodeTerm(long[] empty, DataInput input, FieldInfo fieldInfo, BlockTermState termState,
            bool absolute)
        {
            SepTermState termState_ = (SepTermState)termState;
            termState_.docIndex.Read(input, absolute);
            if (fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
            {
                termState_.freqIndex.Read(input, absolute);
                if (fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    //System.out.println("  freqIndex=" + termState.freqIndex);
                    termState_.posIndex.Read(input, absolute);
                    //System.out.println("  posIndex=" + termState.posIndex);
                    if (fieldInfo.HasPayloads)
                    {
                        if (absolute)
                        {
                            termState_.payloadFP = input.ReadVInt64();
                        }
                        else
                        {
                            termState_.payloadFP += input.ReadVInt64();
                        }
                        //System.out.println("  payloadFP=" + termState.payloadFP);
                    }
                }
            }

            if (termState_.DocFreq >= skipMinimum)
            {
                //System.out.println("   readSkip @ " + in.getPosition());
                if (absolute)
                {
                    termState_.skipFP = input.ReadVInt64();
                }
                else
                {
                    termState_.skipFP += input.ReadVInt64();
                }
                //System.out.println("  skipFP=" + termState.skipFP);
            }
            else if (absolute)
            {
                termState_.skipFP = 0;
            }
        }

        public override DocsEnum Docs(FieldInfo fieldInfo, BlockTermState termState, IBits liveDocs, DocsEnum reuse,
            DocsFlags flags)
        {
            SepTermState termState_ = (SepTermState)termState;

            // If you are using ParellelReader, and pass in a
            // reused DocsAndPositionsEnum, it could have come
            // from another reader also using sep codec
            if (reuse is null || !(reuse is SepDocsEnum docsEnum) || docsEnum.startDocIn != docIn)
                docsEnum = new SepDocsEnum(this);

            return docsEnum.Init(fieldInfo, termState_, liveDocs);
        }

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState termState,
            IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
            SepTermState termState_ = (SepTermState)termState;

            // If you are using ParellelReader, and pass in a
            // reused DocsAndPositionsEnum, it could have come
            // from another reader also using sep codec
            if (reuse is null || !(reuse is SepDocsAndPositionsEnum postingsEnum) || postingsEnum.startDocIn != docIn)
                postingsEnum = new SepDocsAndPositionsEnum(this);

            return postingsEnum.Init(fieldInfo, termState_, liveDocs);
        }

        internal class SepDocsEnum : DocsEnum
        {
            private readonly SepPostingsReader outerInstance;

            private int docFreq;
            private int doc = -1;
            private int accum;
            private int count;
            private int freq;
            //private long freqStart; // LUCENENET: Not used

            // TODO: -- should we do omitTF with 2 different enum classes?
            private bool omitTF;
            private IndexOptions indexOptions;
            private bool storePayloads;
            private IBits liveDocs;
            private readonly Int32IndexInput.Reader docReader;
            private readonly Int32IndexInput.Reader freqReader;
            private long skipFP;

            private readonly Int32IndexInput.Index docIndex;
            private readonly Int32IndexInput.Index freqIndex;
            private readonly Int32IndexInput.Index posIndex;
            internal readonly Int32IndexInput startDocIn;

            // TODO: -- should we do hasProx with 2 different enum classes?

            private bool skipped;
            private SepSkipListReader skipper;

            internal SepDocsEnum(SepPostingsReader outerInstance)
            {
                this.outerInstance = outerInstance;

                startDocIn = outerInstance.docIn;
                docReader = outerInstance.docIn.GetReader();
                docIndex = outerInstance.docIn.GetIndex();
                if (outerInstance.freqIn != null)
                {
                    freqReader = outerInstance.freqIn.GetReader();
                    freqIndex = outerInstance.freqIn.GetIndex();
                }
                else
                {
                    freqReader = null;
                    freqIndex = null;
                }
                if (outerInstance.posIn != null)
                {
                    posIndex = outerInstance.posIn.GetIndex();                 // only init this so skipper can read it
                }
                else
                {
                    posIndex = null;
                }
            }

            internal virtual SepDocsEnum Init(FieldInfo fieldInfo, SepTermState termState, IBits liveDocs)
            {
                this.liveDocs = liveDocs;
                this.indexOptions = fieldInfo.IndexOptions;
                omitTF = indexOptions == IndexOptions.DOCS_ONLY;
                storePayloads = fieldInfo.HasPayloads;

                // TODO: can't we only do this if consumer
                // skipped consuming the previous docs?
                docIndex.CopyFrom(termState.docIndex);
                docIndex.Seek(docReader);

                if (!omitTF)
                {
                    freqIndex.CopyFrom(termState.freqIndex);
                    freqIndex.Seek(freqReader);
                }

                docFreq = termState.DocFreq;
                // NOTE: unused if docFreq < skipMinimum:
                skipFP = termState.skipFP;
                count = 0;
                doc = -1;
                accum = 0;
                freq = 1;
                skipped = false;

                return this;
            }

            public override int NextDoc()
            {
                while (true)
                {
                    if (count == docFreq)
                    {
                        return doc = NO_MORE_DOCS;
                    }

                    count++;

                    // Decode next doc
                    //System.out.println("decode docDelta:");
                    accum += docReader.Next();

                    if (!omitTF)
                    {
                        //System.out.println("decode freq:");
                        freq = freqReader.Next();
                    }

                    if (liveDocs is null || liveDocs.Get(accum))
                    {
                        break;
                    }
                }
                return (doc = accum);
            }

            public override int Freq => freq;

            public override int DocID => doc;

            public override int Advance(int target)
            {
                if ((target - outerInstance.skipInterval) >= doc && docFreq >= outerInstance.skipMinimum)
                {

                    // There are enough docs in the posting to have
                    // skip data, and its not too close

                    if (skipper is null)
                    {
                        // This DocsEnum has never done any skipping
                        skipper = new SepSkipListReader((IndexInput)outerInstance.skipIn.Clone(),
                                                        outerInstance.freqIn,
                                                        outerInstance.docIn,
                                                        outerInstance.posIn,
                                                        outerInstance.maxSkipLevels, outerInstance.skipInterval);

                    }

                    if (!skipped)
                    {
                        // We haven't yet skipped for this posting
                        skipper.Init(skipFP,
                                     docIndex,
                                     freqIndex,
                                     posIndex,
                                     0,
                                     docFreq,
                                     storePayloads);
                        skipper.SetIndexOptions(indexOptions);

                        skipped = true;
                    }

                    int newCount = skipper.SkipTo(target);

                    if (newCount > count)
                    {

                        // Skipper did move
                        if (!omitTF)
                        {
                            skipper.FreqIndex.Seek(freqReader);
                        }
                        skipper.DocIndex.Seek(docReader);
                        count = newCount;
                        doc = accum = skipper.Doc;
                    }
                }

                // Now, linear scan for the rest:
                do
                {
                    if (NextDoc() == NO_MORE_DOCS)
                    {
                        return NO_MORE_DOCS;
                    }
                } while (target > doc);

                return doc;
            }

            public override long GetCost()
            {
                return docFreq;
            }
        }

        internal class SepDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly SepPostingsReader outerInstance;

            private int docFreq;
            private int doc = -1;
            private int accum;
            private int count;
            private int freq;
            //private long freqStart; // LUCENENET: Not used

            private bool storePayloads;
            private IBits liveDocs;
            private readonly Int32IndexInput.Reader docReader;
            private readonly Int32IndexInput.Reader freqReader;
            private readonly Int32IndexInput.Reader posReader;
            private readonly IndexInput payloadIn;
            private long skipFP;

            private readonly Int32IndexInput.Index docIndex;
            private readonly Int32IndexInput.Index freqIndex;
            private readonly Int32IndexInput.Index posIndex;
            internal readonly Int32IndexInput startDocIn;

            private long payloadFP;

            private int pendingPosCount;
            private int position;
            private int payloadLength;
            private long pendingPayloadBytes;

            private bool skipped;
            private SepSkipListReader skipper;
            private bool payloadPending;
            private bool posSeekPending;

            internal SepDocsAndPositionsEnum(SepPostingsReader outerInstance)
            {
                this.outerInstance = outerInstance;

                startDocIn = outerInstance.docIn;
                docReader = outerInstance.docIn.GetReader();
                docIndex = outerInstance.docIn.GetIndex();
                freqReader = outerInstance.freqIn.GetReader();
                freqIndex = outerInstance.freqIn.GetIndex();
                posReader = outerInstance.posIn.GetReader();
                posIndex = outerInstance.posIn.GetIndex();
                payloadIn = (IndexInput)outerInstance.payloadIn.Clone();
            }

            internal virtual SepDocsAndPositionsEnum Init(FieldInfo fieldInfo, SepTermState termState, IBits liveDocs)
            {
                this.liveDocs = liveDocs;
                storePayloads = fieldInfo.HasPayloads;
                //System.out.println("Sep D&P init");

                // TODO: can't we only do this if consumer
                // skipped consuming the previous docs?
                docIndex.CopyFrom(termState.docIndex);
                docIndex.Seek(docReader);
                //System.out.println("  docIndex=" + docIndex);

                freqIndex.CopyFrom(termState.freqIndex);
                freqIndex.Seek(freqReader);
                //System.out.println("  freqIndex=" + freqIndex);

                posIndex.CopyFrom(termState.posIndex);
                //System.out.println("  posIndex=" + posIndex);
                posSeekPending = true;
                payloadPending = false;

                payloadFP = termState.payloadFP;
                skipFP = termState.skipFP;
                //System.out.println("  skipFP=" + skipFP);

                docFreq = termState.DocFreq;
                count = 0;
                doc = -1;
                accum = 0;
                pendingPosCount = 0;
                pendingPayloadBytes = 0;
                skipped = false;

                return this;
            }

            public override int NextDoc()
            {
                while (true)
                {
                    if (count == docFreq)
                    {
                        return doc = NO_MORE_DOCS;
                    }

                    count++;

                    // TODO: maybe we should do the 1-bit trick for encoding
                    // freq=1 case?

                    // Decode next doc
                    //System.out.println("  sep d&p read doc");
                    accum += docReader.Next();

                    //System.out.println("  sep d&p read freq");
                    freq = freqReader.Next();

                    pendingPosCount += freq;

                    if (liveDocs is null || liveDocs.Get(accum))
                    {
                        break;
                    }
                }

                position = 0;
                return (doc = accum);
            }

            public override int Freq => freq;

            public override int DocID => doc;

            public override int Advance(int target)
            {
                //System.out.println("SepD&P advance target=" + target + " vs current=" + doc + " this=" + this);

                if ((target - outerInstance.skipInterval) >= doc && docFreq >= outerInstance.skipMinimum)
                {

                    // There are enough docs in the posting to have
                    // skip data, and its not too close

                    if (skipper is null)
                    {
                        //System.out.println("  create skipper");
                        // This DocsEnum has never done any skipping
                        skipper = new SepSkipListReader((IndexInput)outerInstance.skipIn.Clone(),
                                                        outerInstance.freqIn,
                                                        outerInstance.docIn,
                                                        outerInstance.posIn,
                                                        outerInstance.maxSkipLevels, outerInstance.skipInterval);
                    }

                    if (!skipped)
                    {
                        //System.out.println("  init skip data skipFP=" + skipFP);
                        // We haven't yet skipped for this posting
                        skipper.Init(skipFP,
                                     docIndex,
                                     freqIndex,
                                     posIndex,
                                     payloadFP,
                                     docFreq,
                                     storePayloads);
                        skipper.SetIndexOptions(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
                        skipped = true;
                    }
                    int newCount = skipper.SkipTo(target);
                    //System.out.println("  skip newCount=" + newCount + " vs " + count);

                    if (newCount > count)
                    {

                        // Skipper did move
                        skipper.FreqIndex.Seek(freqReader);
                        skipper.DocIndex.Seek(docReader);
                        //System.out.println("  doc seek'd to " + skipper.getDocIndex());
                        // NOTE: don't seek pos here; do it lazily
                        // instead.  Eg a PhraseQuery may skip to many
                        // docs before finally asking for positions...
                        posIndex.CopyFrom(skipper.PosIndex);
                        posSeekPending = true;
                        count = newCount;
                        doc = accum = skipper.Doc;
                        //System.out.println("    moved to doc=" + doc);
                        //payloadIn.seek(skipper.getPayloadPointer());
                        payloadFP = skipper.PayloadPointer;
                        pendingPosCount = 0;
                        pendingPayloadBytes = 0;
                        payloadPending = false;
                        payloadLength = skipper.PayloadLength;
                        //System.out.println("    move payloadLen=" + payloadLength);
                    }
                }

                // Now, linear scan for the rest:
                do
                {
                    if (NextDoc() == NO_MORE_DOCS)
                    {
                        //System.out.println("  advance nextDoc=END");
                        return NO_MORE_DOCS;
                    }
                    //System.out.println("  advance nextDoc=" + doc);
                } while (target > doc);

                //System.out.println("  return doc=" + doc);
                return doc;
            }

            public override int NextPosition()
            {
                if (posSeekPending)
                {
                    posIndex.Seek(posReader);
                    payloadIn.Seek(payloadFP);
                    posSeekPending = false;
                }

                // scan over any docs that were iterated without their
                // positions
                while (pendingPosCount > freq)
                {
                    int code2 = posReader.Next();
                    if (storePayloads && (code2 & 1) != 0)
                    {
                        // Payload length has changed
                        payloadLength = posReader.Next();
                        if (Debugging.AssertsEnabled) Debugging.Assert(payloadLength >= 0);
                    }
                    pendingPosCount--;
                    position = 0;
                    pendingPayloadBytes += payloadLength;
                }

                int code = posReader.Next();

                if (storePayloads)
                {
                    if ((code & 1) != 0)
                    {
                        // Payload length has changed
                        payloadLength = posReader.Next();
                        if (Debugging.AssertsEnabled) Debugging.Assert(payloadLength >= 0);
                    }
                    position += code.TripleShift(1);
                    pendingPayloadBytes += payloadLength;
                    payloadPending = payloadLength > 0;
                }
                else
                {
                    position += code;
                }

                pendingPosCount--;
                if (Debugging.AssertsEnabled) Debugging.Assert(pendingPosCount >= 0);
                return position;
            }

            public override int StartOffset => -1;

            public override int EndOffset => -1;

            private BytesRef payload;

            public override BytesRef GetPayload()
            {
                if (!payloadPending)
                {
                    return null;
                }

                if (pendingPayloadBytes == 0)
                {
                    return payload;
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(pendingPayloadBytes >= payloadLength);

                if (pendingPayloadBytes > payloadLength)
                {
                    payloadIn.Seek(payloadIn.Position + (pendingPayloadBytes - payloadLength)); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }

                if (payload is null)
                {
                    payload = new BytesRef();
                    payload.Bytes = new byte[payloadLength];
                }
                else if (payload.Bytes.Length < payloadLength)
                {
                    payload.Grow(payloadLength);
                }

                payloadIn.ReadBytes(payload.Bytes, 0, payloadLength);
                payload.Length = payloadLength;
                pendingPayloadBytes = 0;
                return payload;
            }

            public override long GetCost()
            {
                return docFreq;
            }
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
            // TODO: remove sep layout, its fallen behind on features...
        }
    }
}