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

namespace Lucene.Net.Codecs.Pulsing
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Lucene.Net.Util;

    /// <summary>
    /// Concrete class that reads the current doc/freq/skip postings format 
    /// 
    /// @lucene.experimental
    /// 
    /// TODO: -- should we switch "hasProx" higher up?  and
    /// create two separate docs readers, one that also reads
    /// prox and one that doesn't?
    /// </summary>
    public class PulsingPostingsReader : PostingsReaderBase
    {

        // Fallback reader for non-pulsed terms:
        private readonly PostingsReaderBase _wrappedPostingsReader;
        private readonly SegmentReadState segmentState;
        private int maxPositions;
        private int version;
        private SortedDictionary<int, int> fields;

        public PulsingPostingsReader(SegmentReadState state, PostingsReaderBase wrappedPostingsReader)
        {
            this._wrappedPostingsReader = wrappedPostingsReader;
            this.segmentState = state;
        }

        public override void Init(IndexInput termsIn)
        {
            version = CodecUtil.CheckHeader(termsIn, PulsingPostingsWriter.CODEC,
                PulsingPostingsWriter.VERSION_START,
                PulsingPostingsWriter.VERSION_CURRENT);

            maxPositions = termsIn.ReadVInt();
            _wrappedPostingsReader.Init(termsIn);

            if (_wrappedPostingsReader is PulsingPostingsReader || version < PulsingPostingsWriter.VERSION_META_ARRAY)
            {
                fields = null;
            }
            else
            {
                fields = new SortedDictionary<int, int>();
                String summaryFileName = IndexFileNames.SegmentFileName(segmentState.SegmentInfo.Name,
                    segmentState.SegmentSuffix, PulsingPostingsWriter.SUMMARY_EXTENSION);
                IndexInput input = null;

                try
                {
                    input =
                        segmentState.Directory.OpenInput(summaryFileName, segmentState.Context);
                    CodecUtil.CheckHeader(input,
                        PulsingPostingsWriter.CODEC,
                        version,
                        PulsingPostingsWriter.VERSION_CURRENT);

                    int numField = input.ReadVInt();
                    for (int i = 0; i < numField; i++)
                    {
                        int fieldNum = input.ReadVInt();
                        int longsSize = input.ReadVInt();
                        fields.Add(fieldNum, longsSize);
                    }
                }
                finally
                {
                    IOUtils.CloseWhileHandlingException(input);
                }
            }
        }

        public override BlockTermState NewTermState()
        {
            var state = new PulsingTermState {WrappedTermState = _wrappedPostingsReader.NewTermState()};
            return state;
        }

        public override void DecodeTerm(long[] empty, DataInput input, FieldInfo fieldInfo, BlockTermState _termState,
            bool absolute)
        {
            PulsingTermState termState = (PulsingTermState) _termState;

            Debug.Debug.Assert((empty.Length == 0);
            termState.Absolute = termState.Absolute || absolute;
            // if we have positions, its total TF, otherwise its computed based on docFreq.
            // TODO Double check this is right..
            long count = FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS.CompareTo(fieldInfo.IndexOptions) <= 0
                ? termState.TotalTermFreq
                : termState.DocFreq;
            //System.out.println("  count=" + count + " threshold=" + maxPositions);

            if (count <= maxPositions)
            {
                // Inlined into terms dict -- just read the byte[] blob in,
                // but don't decode it now (we only decode when a DocsEnum
                // or D&PEnum is pulled):
                termState.PostingsSize = input.ReadVInt();
                if (termState.Postings == null || termState.Postings.Length < termState.PostingsSize)
                {
                    termState.Postings = new byte[ArrayUtil.Oversize(termState.PostingsSize, 1)];
                }
                // TODO: sort of silly to copy from one big byte[]
                // (the blob holding all inlined terms' blobs for
                // current term block) into another byte[] (just the
                // blob for this term)...
                input.ReadBytes(termState.Postings, 0, termState.PostingsSize);
                //System.out.println("  inlined bytes=" + termState.postingsSize);
                termState.Absolute = termState.Absolute || absolute;
            }
            else
            {
                int longsSize = fields == null ? 0 : fields[fieldInfo.Number];
                if (termState.Longs == null)
                {
                    termState.Longs = new long[longsSize];
                }
                for (int i = 0; i < longsSize; i++)
                {
                    termState.Longs[i] = input.ReadVLong();
                }
                termState.PostingsSize = -1;
                termState.WrappedTermState.DocFreq = termState.DocFreq;
                termState.WrappedTermState.TotalTermFreq = termState.TotalTermFreq;
                _wrappedPostingsReader.DecodeTerm(termState.Longs, input, fieldInfo,
                    termState.WrappedTermState,
                    termState.Absolute);
                termState.Absolute = false;
            }
        }

        public override DocsEnum Docs(FieldInfo field, BlockTermState _termState, Bits liveDocs, DocsEnum reuse,
            int flags)
        {
            PulsingTermState termState = (PulsingTermState) _termState;
            if (termState.PostingsSize != -1)
            {
                PulsingDocsEnum postings;
                if (reuse is PulsingDocsEnum)
                {
                    postings = (PulsingDocsEnum) reuse;
                    if (!postings.CanReuse(field))
                    {
                        postings = new PulsingDocsEnum(field);
                    }
                }
                else
                {
                    // the 'reuse' is actually the wrapped enum
                    PulsingDocsEnum previous = (PulsingDocsEnum) GetOther(reuse);
                    if (previous != null && previous.CanReuse(field))
                    {
                        postings = previous;
                    }
                    else
                    {
                        postings = new PulsingDocsEnum(field);
                    }
                }
                if (reuse != postings)
                {
                    SetOther(postings, reuse); // postings.other = reuse
                }
                return postings.Reset(liveDocs, termState);
            }
            else
            {
                if (reuse is PulsingDocsEnum)
                {
                    DocsEnum wrapped = _wrappedPostingsReader.Docs(field, termState.WrappedTermState, liveDocs,
                        GetOther(reuse), flags);
                    SetOther(wrapped, reuse); // wrapped.other = reuse
                    return wrapped;
                }
                else
                {
                    return _wrappedPostingsReader.Docs(field, termState.WrappedTermState, liveDocs, reuse, flags);
                }
            }
        }

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo field, BlockTermState _termState, Bits liveDocs,
            DocsAndPositionsEnum reuse,
            int flags)
        {

            PulsingTermState termState = (PulsingTermState) _termState;

            if (termState.PostingsSize != -1)
            {
                PulsingDocsAndPositionsEnum postings;
                if (reuse is PulsingDocsAndPositionsEnum)
                {
                    postings = (PulsingDocsAndPositionsEnum) reuse;
                    if (!postings.CanReuse(field))
                    {
                        postings = new PulsingDocsAndPositionsEnum(field);
                    }
                }
                else
                {
                    // the 'reuse' is actually the wrapped enum
                    PulsingDocsAndPositionsEnum previous = (PulsingDocsAndPositionsEnum) GetOther(reuse);
                    if (previous != null && previous.CanReuse(field))
                    {
                        postings = previous;
                    }
                    else
                    {
                        postings = new PulsingDocsAndPositionsEnum(field);
                    }
                }
                if (reuse != postings)
                {
                    SetOther(postings, reuse); // postings.other = reuse 
                }
                return postings.reset(liveDocs, termState);
            }
            else
            {
                if (reuse is PulsingDocsAndPositionsEnum)
                {
                    DocsAndPositionsEnum wrapped = _wrappedPostingsReader.DocsAndPositions(field,
                        termState.WrappedTermState,
                        liveDocs, (DocsAndPositionsEnum) GetOther(reuse),
                        flags);
                    SetOther(wrapped, reuse); // wrapped.other = reuse
                    return wrapped;
                }
                else
                {
                    return _wrappedPostingsReader.DocsAndPositions(field, termState.WrappedTermState, liveDocs, reuse,
                        flags);
                }
            }
        }

        public override long RamBytesUsed()
        {
            return ((_wrappedPostingsReader != null) ? _wrappedPostingsReader.RamBytesUsed() : 0);
        }

        public override void CheckIntegrity()
        {
            _wrappedPostingsReader.CheckIntegrity();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                _wrappedPostingsReader.Dispose();
        }
        
        /// <summary>
        /// for a docsenum, gets the 'other' reused enum.
        /// Example: Pulsing(Standard).
        /// when doing a term range query you are switching back and forth
        /// between Pulsing and Standard
        ///  
        /// The way the reuse works is that Pulsing.other = Standard and
        /// Standard.other = Pulsing.
        /// </summary>
        private DocsEnum GetOther(DocsEnum de)
        {
            if (de == null)
            {
                return null;
            }
            else
            {
                AttributeSource atts = de.Attributes();
                return atts.AddAttribute(PulsingEnumAttribute.Enums().get(this);
            }
        }

        /// <summary>
        /// for a docsenum, sets the 'other' reused enum.
        /// see GetOther for an example.
        /// </summary>
        private DocsEnum SetOther(DocsEnum de, DocsEnum other)
        {
            AttributeSource atts = de.Attributes();
            return atts.AddAttribute(PulsingEnumAttributeImpl.Enums().put(this, other));
        }

        ///<summary>
        /// A per-docsenum attribute that stores additional reuse information
        /// so that pulsing enums can keep a reference to their wrapped enums,
        /// and vice versa. this way we can always reuse.
        /// 
        /// @lucene.internal 
        /// </summary>
        public interface IPulsingEnumAttribute : IAttribute
        {
            Dictionary<PulsingPostingsReader, DocsEnum> Enums();
        }

        internal class PulsingTermState : BlockTermState
        {
            public bool Absolute { get; set; }
            public long[] Longs { get; set; }
            public byte[] Postings { get; set; }
            public int PostingsSize { get; set; } // -1 if this term was not inlined
            public BlockTermState WrappedTermState { get; set; }

            public override object Clone()
            {
                PulsingTermState clone = (PulsingTermState) base.Clone();
                if (PostingsSize != -1)
                {
                    clone.Postings = new byte[PostingsSize];
                    Array.Copy(Postings, 0, clone.Postings, 0, PostingsSize);
                }
                else
                {
                    Debug.Debug.Assert((WrappedTermState != null);
                    clone.WrappedTermState = (BlockTermState) WrappedTermState.Clone();
                    clone.Absolute = Absolute;
                    if (Longs != null)
                    {
                        clone.Longs = new long[Longs.Length];
                        Array.Copy(Longs, 0, clone.Longs, 0, Longs.Length);
                    }
                }
                return clone;
            }

            public override void CopyFrom(TermState other)
            {
                base.CopyFrom(other);
                var _other = (PulsingTermState) other;
                PostingsSize = _other.PostingsSize;
                if (_other.PostingsSize != -1)
                {
                    if (Postings == null || Postings.Length < _other.PostingsSize)
                    {
                        Postings = new byte[ArrayUtil.Oversize(_other.PostingsSize, 1)];
                    }
                    System.Array.Copy(_other.Postings, 0, Postings, 0, _other.PostingsSize);
                }
                else
                {
                    WrappedTermState.CopyFrom(_other.WrappedTermState);
                }
            }

            public override String ToString()
            {
                if (PostingsSize == -1)
                {
                    return "PulsingTermState: not inlined: wrapped=" + WrappedTermState;
                }
                else
                {
                    return "PulsingTermState: inlined size=" + PostingsSize + " " + base.ToString();
                }
            }
        }

        internal class PulsingDocsEnum : DocsEnum
        {
            private byte[] postingsBytes;
            private readonly ByteArrayDataInput postings = new ByteArrayDataInput();
            private readonly FieldInfo.IndexOptions_e? indexOptions;
            private readonly bool storePayloads;
            private readonly bool storeOffsets;
            private Bits liveDocs;

            private int docID = -1;
            private int accum;
            private int freq;
            private int payloadLength;
            private int cost;

            public PulsingDocsEnum(FieldInfo fieldInfo)
            {
                indexOptions = fieldInfo.IndexOptions;
                storePayloads = fieldInfo.HasPayloads();
                storeOffsets = indexOptions.Value.CompareTo(FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            }

            public PulsingDocsEnum Reset(Bits liveDocs, PulsingTermState termState)
            {
                Debug.Debug.Assert((termState.PostingsSize != -1);

                // Must make a copy of termState's byte[] so that if
                // app does TermsEnum.next(), this DocsEnum is not affected
                if (postingsBytes == null)
                {
                    postingsBytes = new byte[termState.PostingsSize];
                }
                else if (postingsBytes.Length < termState.PostingsSize)
                {
                    postingsBytes = ArrayUtil.Grow(postingsBytes, termState.PostingsSize);
                }
                System.Array.Copy(termState.Postings, 0, postingsBytes, 0, termState.PostingsSize);
                postings.Reset(postingsBytes, 0, termState.PostingsSize);
                docID = -1;
                accum = 0;
                freq = 1;
                cost = termState.DocFreq;
                payloadLength = 0;
                this.liveDocs = liveDocs;
                return this;
            }

            public bool CanReuse(FieldInfo fieldInfo)
            {
                return indexOptions == fieldInfo.IndexOptions && storePayloads == fieldInfo.HasPayloads();
            }

            public override int DocID()
            {
                return docID;
            }

            public override int NextDoc()
            {
                //System.out.println("PR nextDoc this= "+ this);
                while (true)
                {
                    if (postings.Eof())
                    {
                        return docID = NO_MORE_DOCS;
                    }

                    int code = postings.ReadVInt();
                    if (indexOptions == FieldInfo.IndexOptions_e.DOCS_ONLY)
                    {
                        accum += code;
                    }
                    else
                    {
                        accum += (int)((uint)code >> 1); ; // shift off low bit
                        if ((code & 1) != 0)
                        {
                            // if low bit is set
                            freq = 1; // freq is one
                        }
                        else
                        {
                            freq = postings.ReadVInt(); // else read freq
                        }

                        if (indexOptions.Value.CompareTo(FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS) >= 0)
                        {
                            // Skip positions
                            if (storePayloads)
                            {
                                for (int pos = 0; pos < freq; pos++)
                                {
                                    int posCode = postings.ReadVInt();
                                    if ((posCode & 1) != 0)
                                    {
                                        payloadLength = postings.ReadVInt();
                                    }
                                    if (storeOffsets && (postings.ReadVInt() & 1) != 0)
                                    {
                                        // new offset length
                                        postings.ReadVInt();
                                    }
                                    if (payloadLength != 0)
                                    {
                                        postings.SkipBytes(payloadLength);
                                    }
                                }
                            }
                            else
                            {
                                for (int pos = 0; pos < freq; pos++)
                                {
                                    // TODO: skipVInt
                                    postings.ReadVInt();
                                    if (storeOffsets && (postings.ReadVInt() & 1) != 0)
                                    {
                                        // new offset length
                                        postings.ReadVInt();
                                    }
                                }
                            }
                        }
                    }

                    if (liveDocs == null || liveDocs.Get(accum))
                    {
                        return (docID = accum);
                    }

                }
            }

            public override int Advance(int target)
            {
                return docID = SlowAdvance(target);
            }

            public override long Cost()
            {
                return cost;
            }

            public override int Freq()
            {
                return freq;
            }
        }

        internal class PulsingDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private byte[] postingsBytes;
            private readonly ByteArrayDataInput postings = new ByteArrayDataInput();
            private readonly bool storePayloads;
            private readonly bool storeOffsets;
            // note: we could actually reuse across different options, if we passed this to reset()
            // and re-init'ed storeOffsets accordingly (made it non-final)
            private readonly FieldInfo.IndexOptions_e? indexOptions;

            private Bits liveDocs;
            private int docID = -1;
            private int accum;
            private int freq;
            private int posPending;
            private int position;
            private int payloadLength;
            private BytesRef payload;
            private int startOffset;
            private int offsetLength;

            private bool payloadRetrieved;
            private int cost;

            public PulsingDocsAndPositionsEnum(FieldInfo fieldInfo)
            {
                indexOptions = fieldInfo.IndexOptions;
                storePayloads = fieldInfo.HasPayloads();
                storeOffsets =
                    indexOptions.Value.CompareTo(FieldInfo.IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            }

            public PulsingDocsAndPositionsEnum reset(Bits liveDocs, PulsingTermState termState)
            {
                Debug.Debug.Assert((termState.PostingsSize != -1);

                if (postingsBytes == null)
                {
                    postingsBytes = new byte[termState.PostingsSize];
                }
                else if (postingsBytes.Length < termState.PostingsSize)
                {
                    postingsBytes = ArrayUtil.Grow(postingsBytes, termState.PostingsSize);
                }

                System.Array.Copy(termState.Postings, 0, postingsBytes, 0, termState.PostingsSize);
                postings.Reset(postingsBytes, 0, termState.PostingsSize);
                this.liveDocs = liveDocs;
                payloadLength = 0;
                posPending = 0;
                docID = -1;
                accum = 0;
                cost = termState.DocFreq;
                startOffset = storeOffsets ? 0 : -1; // always return -1 if no offsets are stored
                offsetLength = 0;
                //System.out.println("PR d&p reset storesPayloads=" + storePayloads + " bytes=" + bytes.length + " this=" + this);
                return this;
            }

            public bool CanReuse(FieldInfo fieldInfo)
            {
                return indexOptions == fieldInfo.IndexOptions && storePayloads == fieldInfo.HasPayloads();
            }

            public override int NextDoc()
            {

                while (true)
                {

                    SkipPositions();

                    if (postings.Eof())
                    {
                        return docID = NO_MORE_DOCS;
                    }

                    int code = postings.ReadVInt();
                    accum += (int)((uint)code >> 1); // shift off low bit 
                    if ((code & 1) != 0)
                    {
                        // if low bit is set
                        freq = 1; // freq is one
                    }
                    else
                    {
                        freq = postings.ReadVInt(); // else read freq
                    }
                    posPending = freq;
                    startOffset = storeOffsets ? 0 : -1; // always return -1 if no offsets are stored

                    if (liveDocs == null || liveDocs.Get(accum))
                    {
                        position = 0;
                        return (docID = accum);
                    }
                }
            }

            public override int Freq()
            {
                return freq;
            }

            public override int DocID()
            {
                return docID;
            }

            public override int Advance(int target)
            {
                return docID = SlowAdvance(target);
            }

            public override int NextPosition()
            {
                Debug.Debug.Assert((posPending > 0);

                posPending--;

                if (storePayloads)
                {
                    if (!payloadRetrieved)
                    {
                        postings.SkipBytes(payloadLength);
                    }
                    int code = postings.ReadVInt();
                    if ((code & 1) != 0)
                    {
                        payloadLength = postings.ReadVInt();
                    }
                    position += (int)((uint)code >> 1);
                    payloadRetrieved = false;
                }
                else
                {
                    position += postings.ReadVInt();
                }

                if (storeOffsets)
                {
                    int offsetCode = postings.ReadVInt();
                    if ((offsetCode & 1) != 0)
                    {
                        // new offset length
                        offsetLength = postings.ReadVInt();
                    }
                    startOffset += (int)((uint)offsetCode >> 1);
                }

                return position;
            }

            public override int StartOffset()
            {
                return startOffset;
            }

            public override int EndOffset()
            {
                return startOffset + offsetLength;
            }

            public override BytesRef Payload
            {
                get
                {
                    if (payloadRetrieved)
                    {
                        return payload;
                    }
                    else if (storePayloads && payloadLength > 0)
                    {
                        payloadRetrieved = true;
                        if (payload == null)
                        {
                            payload = new BytesRef(payloadLength);
                        }
                        else
                        {
                            payload.Grow(payloadLength);
                        }
                        postings.ReadBytes(payload.Bytes, 0, payloadLength);
                        payload.Length = payloadLength;
                        return payload;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            private void SkipPositions()
            {
                while (posPending != 0)
                {
                    NextPosition();
                }
                if (storePayloads && !payloadRetrieved)
                {
                    postings.SkipBytes(payloadLength);
                    payloadRetrieved = true;
                }
            }
            
            public override long Cost()
            {
                return cost;
            }
        }
        
        /// <summary>
        /// Implementation of {@link PulsingEnumAttribute} for reuse of
        /// wrapped postings readers underneath pulsing.
        /// 
        /// @lucene.internal
        /// </summary>
        internal sealed class PulsingEnumAttributeImpl : AttributeImpl, IPulsingEnumAttribute
        {
            // we could store 'other', but what if someone 'chained' multiple postings readers,
            // this could cause problems?
            // TODO: we should consider nuking this map and just making it so if you do this,
            // you don't reuse? and maybe pulsingPostingsReader should throw an exc if it wraps
            // another pulsing, because this is just stupid and wasteful. 
            // we still have to be careful in case someone does Pulsing(Stomping(Pulsing(...
            private readonly Dictionary<PulsingPostingsReader, DocsEnum> _enums = new Dictionary<PulsingPostingsReader, DocsEnum>();

            public Dictionary<PulsingPostingsReader, DocsEnum> Enums()
            {
                return _enums;
            }
            public override void Clear()
            {
                // our state is per-docsenum, so this makes no sense.
                // its best not to clear, in case a wrapped enum has a per-doc attribute or something
                // and is calling clearAttributes(), so they don't nuke the reuse information!
            }

            public override void CopyTo(AttributeImpl target)
            {
                // this makes no sense for us, because our state is per-docsenum.
                // we don't want to copy any stuff over to another docsenum ever!
            }

        }

    }
}
