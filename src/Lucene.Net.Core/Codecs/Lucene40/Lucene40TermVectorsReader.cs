using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Support;

namespace Lucene.Net.Codecs.Lucene40
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

    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Lucene 4.0 Term Vectors reader.
    /// <p>
    /// It reads .tvd, .tvf, and .tvx files.
    /// </summary>
    /// <seealso cref= Lucene40TermVectorsFormat </seealso>
    public class Lucene40TermVectorsReader : TermVectorsReader, IDisposable
    {
        internal const sbyte STORE_POSITIONS_WITH_TERMVECTOR = 0x1;

        internal const sbyte STORE_OFFSET_WITH_TERMVECTOR = 0x2;

        internal const sbyte STORE_PAYLOAD_WITH_TERMVECTOR = 0x4;

        /// <summary>
        /// Extension of vectors fields file </summary>
        internal const string VECTORS_FIELDS_EXTENSION = "tvf";

        /// <summary>
        /// Extension of vectors documents file </summary>
        internal const string VECTORS_DOCUMENTS_EXTENSION = "tvd";

        /// <summary>
        /// Extension of vectors index file </summary>
        internal const string VECTORS_INDEX_EXTENSION = "tvx";

        internal const string CODEC_NAME_FIELDS = "Lucene40TermVectorsFields";
        internal const string CODEC_NAME_DOCS = "Lucene40TermVectorsDocs";
        internal const string CODEC_NAME_INDEX = "Lucene40TermVectorsIndex";

        internal const int VERSION_NO_PAYLOADS = 0;
        internal const int VERSION_PAYLOADS = 1;
        internal const int VERSION_START = VERSION_NO_PAYLOADS;
        internal const int VERSION_CURRENT = VERSION_PAYLOADS;

        internal static readonly long HEADER_LENGTH_FIELDS = CodecUtil.HeaderLength(CODEC_NAME_FIELDS);
        internal static readonly long HEADER_LENGTH_DOCS = CodecUtil.HeaderLength(CODEC_NAME_DOCS);
        internal static readonly long HEADER_LENGTH_INDEX = CodecUtil.HeaderLength(CODEC_NAME_INDEX);

        private FieldInfos FieldInfos;

        private IndexInput Tvx;
        private IndexInput Tvd;
        private IndexInput Tvf;
        private int Size_Renamed;
        private int NumTotalDocs;

        /// <summary>
        /// Used by clone. </summary>
        internal Lucene40TermVectorsReader(FieldInfos fieldInfos, IndexInput tvx, IndexInput tvd, IndexInput tvf, int size, int numTotalDocs)
        {
            this.FieldInfos = fieldInfos;
            this.Tvx = tvx;
            this.Tvd = tvd;
            this.Tvf = tvf;
            this.Size_Renamed = size;
            this.NumTotalDocs = numTotalDocs;
        }

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40TermVectorsReader(Directory d, SegmentInfo si, FieldInfos fieldInfos, IOContext context)
        {
            string segment = si.Name;
            int size = si.DocCount;

            bool success = false;

            try
            {
                string idxName = IndexFileNames.SegmentFileName(segment, "", VECTORS_INDEX_EXTENSION);
                Tvx = d.OpenInput(idxName, context);
                int tvxVersion = CodecUtil.CheckHeader(Tvx, CODEC_NAME_INDEX, VERSION_START, VERSION_CURRENT);

                string fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_DOCUMENTS_EXTENSION);
                Tvd = d.OpenInput(fn, context);
                int tvdVersion = CodecUtil.CheckHeader(Tvd, CODEC_NAME_DOCS, VERSION_START, VERSION_CURRENT);
                fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_FIELDS_EXTENSION);
                Tvf = d.OpenInput(fn, context);
                int tvfVersion = CodecUtil.CheckHeader(Tvf, CODEC_NAME_FIELDS, VERSION_START, VERSION_CURRENT);
                Debug.Assert(HEADER_LENGTH_INDEX == Tvx.FilePointer);
                Debug.Assert(HEADER_LENGTH_DOCS == Tvd.FilePointer);
                Debug.Assert(HEADER_LENGTH_FIELDS == Tvf.FilePointer);
                Debug.Assert(tvxVersion == tvdVersion);
                Debug.Assert(tvxVersion == tvfVersion);

                NumTotalDocs = (int)(Tvx.Length - HEADER_LENGTH_INDEX >> 4);

                this.Size_Renamed = NumTotalDocs;
                Debug.Assert(size == 0 || NumTotalDocs == size);

                this.FieldInfos = fieldInfos;
                success = true;
            }
            finally
            {
                // With lock-less commits, it's entirely possible (and
                // fine) to hit a FileNotFound exception above. In
                // this case, we want to explicitly close any subset
                // of things that were opened so that we don't have to
                // wait for a GC to do so.
                if (!success)
                {
                    try
                    {
                        Dispose();
                    } // ensure we throw our original exception
                    catch (Exception t)
                    {
                    }
                }
            }
        }

        // Used for bulk copy when merging
        internal virtual IndexInput TvdStream
        {
            get
            {
                return Tvd;
            }
        }

        // Used for bulk copy when merging
        internal virtual IndexInput TvfStream
        {
            get
            {
                return Tvf;
            }
        }

        // Not private to avoid synthetic access$NNN methods
        internal virtual void SeekTvx(int docNum)
        {
            Tvx.Seek(docNum * 16L + HEADER_LENGTH_INDEX);
        }

        /// <summary>
        /// Retrieve the length (in bytes) of the tvd and tvf
        ///  entries for the next numDocs starting with
        ///  startDocID.  this is used for bulk copying when
        ///  merging segments, if the field numbers are
        ///  congruent.  Once this returns, the tvf & tvd streams
        ///  are seeked to the startDocID.
        /// </summary>
        internal void RawDocs(int[] tvdLengths, int[] tvfLengths, int startDocID, int numDocs)
        {
            if (Tvx == null)
            {
                CollectionsHelper.Fill(tvdLengths, 0);
                CollectionsHelper.Fill(tvfLengths, 0);
                return;
            }

            SeekTvx(startDocID);

            long tvdPosition = Tvx.ReadLong();
            Tvd.Seek(tvdPosition);

            long tvfPosition = Tvx.ReadLong();
            Tvf.Seek(tvfPosition);

            long lastTvdPosition = tvdPosition;
            long lastTvfPosition = tvfPosition;

            int count = 0;
            while (count < numDocs)
            {
                int docID = startDocID + count + 1;
                Debug.Assert(docID <= NumTotalDocs);
                if (docID < NumTotalDocs)
                {
                    tvdPosition = Tvx.ReadLong();
                    tvfPosition = Tvx.ReadLong();
                }
                else
                {
                    tvdPosition = Tvd.Length;
                    tvfPosition = Tvf.Length;
                    Debug.Assert(count == numDocs - 1);
                }
                tvdLengths[count] = (int)(tvdPosition - lastTvdPosition);
                tvfLengths[count] = (int)(tvfPosition - lastTvfPosition);
                count++;
                lastTvdPosition = tvdPosition;
                lastTvfPosition = tvfPosition;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IOUtils.Close(Tvx, Tvd, Tvf);
        }

        ///
        /// <returns> The number of documents in the reader </returns>
        internal virtual int Size // LUCENENET TODO: Rename Count
        {
            get { return Size_Renamed; }
        }

        private class TVFields : Fields
        {
            private readonly Lucene40TermVectorsReader OuterInstance;

            private readonly int[] FieldNumbers;
            private readonly long[] FieldFPs;
            private readonly IDictionary<int, int> FieldNumberToIndex = new Dictionary<int, int>();

            public TVFields(Lucene40TermVectorsReader outerInstance, int docID)
            {
                this.OuterInstance = outerInstance;
                outerInstance.SeekTvx(docID);
                outerInstance.Tvd.Seek(outerInstance.Tvx.ReadLong());

                int fieldCount = outerInstance.Tvd.ReadVInt();
                Debug.Assert(fieldCount >= 0);
                if (fieldCount != 0)
                {
                    FieldNumbers = new int[fieldCount];
                    FieldFPs = new long[fieldCount];
                    for (int fieldUpto = 0; fieldUpto < fieldCount; fieldUpto++)
                    {
                        int fieldNumber = outerInstance.Tvd.ReadVInt();
                        FieldNumbers[fieldUpto] = fieldNumber;
                        FieldNumberToIndex[fieldNumber] = fieldUpto;
                    }

                    long position = outerInstance.Tvx.ReadLong();
                    FieldFPs[0] = position;
                    for (int fieldUpto = 1; fieldUpto < fieldCount; fieldUpto++)
                    {
                        position += outerInstance.Tvd.ReadVLong();
                        FieldFPs[fieldUpto] = position;
                    }
                }
                else
                {
                    // TODO: we can improve writer here, eg write 0 into
                    // tvx file, so we know on first read from tvx that
                    // this doc has no TVs
                    FieldNumbers = null;
                    FieldFPs = null;
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return GetFieldInfoEnumerable().GetEnumerator();
            }

            private IEnumerable<string> GetFieldInfoEnumerable()
            {
                int fieldUpto = 0;
                while (FieldNumbers != null && fieldUpto < FieldNumbers.Length)
                {
                    yield return OuterInstance.FieldInfos.FieldInfo(FieldNumbers[fieldUpto++]).Name;
                }
            }

            public override Terms Terms(string field)
            {
                FieldInfo fieldInfo = OuterInstance.FieldInfos.FieldInfo(field);
                if (fieldInfo == null)
                {
                    // No such field
                    return null;
                }

                int fieldIndex;
                if (!FieldNumberToIndex.TryGetValue(fieldInfo.Number, out fieldIndex))
                {
                    // Term vectors were not indexed for this field
                    return null;
                }

                return new TVTerms(OuterInstance, FieldFPs[fieldIndex]);
            }

            public override int Size
            {
                get
                {
                    if (FieldNumbers == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return FieldNumbers.Length;
                    }
                }
            }
        }

        private class TVTerms : Terms
        {
            private readonly Lucene40TermVectorsReader OuterInstance;

            private readonly int NumTerms;
            private readonly long TvfFPStart;
            private readonly bool StorePositions;
            private readonly bool StoreOffsets;
            private readonly bool StorePayloads;

            public TVTerms(Lucene40TermVectorsReader outerInstance, long tvfFP)
            {
                this.OuterInstance = outerInstance;
                outerInstance.Tvf.Seek(tvfFP);
                NumTerms = outerInstance.Tvf.ReadVInt();
                byte bits = outerInstance.Tvf.ReadByte();
                StorePositions = (bits & STORE_POSITIONS_WITH_TERMVECTOR) != 0;
                StoreOffsets = (bits & STORE_OFFSET_WITH_TERMVECTOR) != 0;
                StorePayloads = (bits & STORE_PAYLOAD_WITH_TERMVECTOR) != 0;
                TvfFPStart = outerInstance.Tvf.FilePointer;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                TVTermsEnum termsEnum;
                if (reuse is TVTermsEnum)
                {
                    termsEnum = (TVTermsEnum)reuse;
                    if (!termsEnum.CanReuse(OuterInstance.Tvf))
                    {
                        termsEnum = new TVTermsEnum(OuterInstance);
                    }
                }
                else
                {
                    termsEnum = new TVTermsEnum(OuterInstance);
                }
                termsEnum.Reset(NumTerms, TvfFPStart, StorePositions, StoreOffsets, StorePayloads);
                return termsEnum;
            }

            public override long Size
            {
                get { return NumTerms; }
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return -1;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    // Every term occurs in just one doc:
                    return NumTerms;
                }
            }

            public override int DocCount
            {
                get
                {
                    return 1;
                }
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    // TODO: really indexer hardwires
                    // this...?  I guess codec could buffer and re-sort...
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

            public override bool HasFreqs
            {
                get { return true; }
            }

            public override bool HasOffsets
            {
                get { return StoreOffsets; }
            }

            public override bool HasPositions
            {
                get { return StorePositions; }
            }

            public override bool HasPayloads
            {
                get { return StorePayloads; }
            }
        }

        private class TVTermsEnum : TermsEnum
        {
            private readonly Lucene40TermVectorsReader OuterInstance;

            private readonly IndexInput OrigTVF;
            private readonly IndexInput Tvf;
            private int NumTerms;
            private int NextTerm;
            private int Freq;
            private readonly BytesRef LastTerm = new BytesRef();
            private readonly BytesRef Term_Renamed = new BytesRef();
            private bool StorePositions;
            private bool StoreOffsets;
            private bool StorePayloads;
            private long TvfFP;

            private int[] Positions;
            private int[] StartOffsets;
            private int[] EndOffsets;

            // one shared byte[] for any term's payloads
            private int[] PayloadOffsets;

            private int LastPayloadLength;
            private byte[] PayloadData;

            // NOTE: tvf is pre-positioned by caller
            public TVTermsEnum(Lucene40TermVectorsReader outerInstance)
            {
                this.OuterInstance = outerInstance;
                this.OrigTVF = outerInstance.Tvf;
                Tvf = (IndexInput)OrigTVF.Clone();
            }

            public virtual bool CanReuse(IndexInput tvf)
            {
                return tvf == OrigTVF;
            }

            public virtual void Reset(int numTerms, long tvfFPStart, bool storePositions, bool storeOffsets, bool storePayloads)
            {
                this.NumTerms = numTerms;
                this.StorePositions = storePositions;
                this.StoreOffsets = storeOffsets;
                this.StorePayloads = storePayloads;
                NextTerm = 0;
                Tvf.Seek(tvfFPStart);
                TvfFP = tvfFPStart;
                Positions = null;
                StartOffsets = null;
                EndOffsets = null;
                PayloadOffsets = null;
                PayloadData = null;
                LastPayloadLength = -1;
            }

            // NOTE: slow!  (linear scan)
            public override SeekStatus SeekCeil(BytesRef text)
            {
                if (NextTerm != 0)
                {
                    int cmp = text.CompareTo(Term_Renamed);
                    if (cmp < 0)
                    {
                        NextTerm = 0;
                        Tvf.Seek(TvfFP);
                    }
                    else if (cmp == 0)
                    {
                        return SeekStatus.FOUND;
                    }
                }

                while (Next() != null)
                {
                    int cmp = text.CompareTo(Term_Renamed);
                    if (cmp < 0)
                    {
                        return SeekStatus.NOT_FOUND;
                    }
                    else if (cmp == 0)
                    {
                        return SeekStatus.FOUND;
                    }
                }

                return SeekStatus.END;
            }

            public override void SeekExact(long ord)
            {
                throw new System.NotSupportedException();
            }

            public override BytesRef Next()
            {
                if (NextTerm >= NumTerms)
                {
                    return null;
                }
                Term_Renamed.CopyBytes(LastTerm);
                int start = Tvf.ReadVInt();
                int deltaLen = Tvf.ReadVInt();
                Term_Renamed.Length = start + deltaLen;
                Term_Renamed.Grow(Term_Renamed.Length);
                Tvf.ReadBytes(Term_Renamed.Bytes, start, deltaLen);
                Freq = Tvf.ReadVInt();

                if (StorePayloads)
                {
                    Positions = new int[Freq];
                    PayloadOffsets = new int[Freq];
                    int totalPayloadLength = 0;
                    int pos = 0;
                    for (int posUpto = 0; posUpto < Freq; posUpto++)
                    {
                        int code = Tvf.ReadVInt();
                        pos += (int)((uint)code >> 1);
                        Positions[posUpto] = pos;
                        if ((code & 1) != 0)
                        {
                            // length change
                            LastPayloadLength = Tvf.ReadVInt();
                        }
                        PayloadOffsets[posUpto] = totalPayloadLength;
                        totalPayloadLength += LastPayloadLength;
                        Debug.Assert(totalPayloadLength >= 0);
                    }
                    PayloadData = new byte[totalPayloadLength];
                    Tvf.ReadBytes(PayloadData, 0, PayloadData.Length);
                } // no payloads
                else if (StorePositions)
                {
                    // TODO: we could maybe reuse last array, if we can
                    // somehow be careful about consumer never using two
                    // D&PEnums at once...
                    Positions = new int[Freq];
                    int pos = 0;
                    for (int posUpto = 0; posUpto < Freq; posUpto++)
                    {
                        pos += Tvf.ReadVInt();
                        Positions[posUpto] = pos;
                    }
                }

                if (StoreOffsets)
                {
                    StartOffsets = new int[Freq];
                    EndOffsets = new int[Freq];
                    int offset = 0;
                    for (int posUpto = 0; posUpto < Freq; posUpto++)
                    {
                        StartOffsets[posUpto] = offset + Tvf.ReadVInt();
                        offset = EndOffsets[posUpto] = StartOffsets[posUpto] + Tvf.ReadVInt();
                    }
                }

                LastTerm.CopyBytes(Term_Renamed);
                NextTerm++;
                return Term_Renamed;
            }

            public override BytesRef Term
            {
                get { return Term_Renamed; }
            }

            public override long Ord()
            {
                throw new System.NotSupportedException();
            }

            public override int DocFreq()
            {
                return 1;
            }

            public override long TotalTermFreq()
            {
                return Freq;
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags) // ignored
            {
                TVDocsEnum docsEnum;
                if (reuse != null && reuse is TVDocsEnum)
                {
                    docsEnum = (TVDocsEnum)reuse;
                }
                else
                {
                    docsEnum = new TVDocsEnum();
                }
                docsEnum.Reset(liveDocs, Freq);
                return docsEnum;
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                if (!StorePositions && !StoreOffsets)
                {
                    return null;
                }

                TVDocsAndPositionsEnum docsAndPositionsEnum;
                if (reuse != null && reuse is TVDocsAndPositionsEnum)
                {
                    docsAndPositionsEnum = (TVDocsAndPositionsEnum)reuse;
                }
                else
                {
                    docsAndPositionsEnum = new TVDocsAndPositionsEnum();
                }
                docsAndPositionsEnum.Reset(liveDocs, Positions, StartOffsets, EndOffsets, PayloadOffsets, PayloadData);
                return docsAndPositionsEnum;
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }
        }

        // NOTE: sort of a silly class, since you can get the
        // freq() already by TermsEnum.totalTermFreq
        private class TVDocsEnum : DocsEnum
        {
            private bool DidNext;
            private int Doc = -1;
            private int Freq_Renamed;
            private IBits LiveDocs;

            public override int Freq
            {
                get { return Freq_Renamed; }
            }

            public override int DocID
            {
                get { return Doc; }
            }

            public override int NextDoc()
            {
                if (!DidNext && (LiveDocs == null || LiveDocs.Get(0)))
                {
                    DidNext = true;
                    return (Doc = 0);
                }
                else
                {
                    return (Doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public virtual void Reset(IBits liveDocs, int freq)
            {
                this.LiveDocs = liveDocs;
                this.Freq_Renamed = freq;
                this.Doc = -1;
                DidNext = false;
            }

            public override long Cost()
            {
                return 1;
            }
        }

        private sealed class TVDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private bool DidNext;
            private int Doc = -1;
            private int NextPos;
            private IBits LiveDocs;
            private int[] Positions;
            private int[] StartOffsets;
            private int[] EndOffsets;
            private int[] PayloadOffsets;
            private readonly BytesRef Payload_Renamed = new BytesRef();
            private byte[] PayloadBytes;

            public override int Freq
            {
                get
                {
                    if (Positions != null)
                    {
                        return Positions.Length;
                    }
                    else
                    {
                        Debug.Assert(StartOffsets != null);
                        return StartOffsets.Length;
                    }
                }
            }

            public override int DocID
            {
                get { return Doc; }
            }

            public override int NextDoc()
            {
                if (!DidNext && (LiveDocs == null || LiveDocs.Get(0)))
                {
                    DidNext = true;
                    return (Doc = 0);
                }
                else
                {
                    return (Doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public void Reset(IBits liveDocs, int[] positions, int[] startOffsets, int[] endOffsets, int[] payloadLengths, byte[] payloadBytes)
            {
                this.LiveDocs = liveDocs;
                this.Positions = positions;
                this.StartOffsets = startOffsets;
                this.EndOffsets = endOffsets;
                this.PayloadOffsets = payloadLengths;
                this.PayloadBytes = payloadBytes;
                this.Doc = -1;
                DidNext = false;
                NextPos = 0;
            }

            public override BytesRef Payload
            {
                get
                {
                    if (PayloadOffsets == null)
                    {
                        return null;
                    }
                    else
                    {
                        int off = PayloadOffsets[NextPos - 1];
                        int end = NextPos == PayloadOffsets.Length ? PayloadBytes.Length : PayloadOffsets[NextPos];
                        if (end - off == 0)
                        {
                            return null;
                        }
                        Payload_Renamed.Bytes = PayloadBytes;
                        Payload_Renamed.Offset = off;
                        Payload_Renamed.Length = end - off;
                        return Payload_Renamed;
                    }
                }
            }

            public override int NextPosition()
            {
                Debug.Assert((Positions != null && NextPos < Positions.Length) || StartOffsets != null && NextPos < StartOffsets.Length);

                if (Positions != null)
                {
                    return Positions[NextPos++];
                }
                else
                {
                    NextPos++;
                    return -1;
                }
            }

            public override int StartOffset
            {
                get
                {
                    if (StartOffsets == null)
                    {
                        return -1;
                    }
                    else
                    {
                        return StartOffsets[NextPos - 1];
                    }
                }
            }

            public override int EndOffset
            {
                get
                {
                    if (EndOffsets == null)
                    {
                        return -1;
                    }
                    else
                    {
                        return EndOffsets[NextPos - 1];
                    }
                }
            }

            public override long Cost()
            {
                return 1;
            }
        }

        public override Fields Get(int docID)
        {
            if (Tvx != null)
            {
                Fields fields = new TVFields(this, docID);
                if (fields.Size == 0)
                {
                    // TODO: we can improve writer here, eg write 0 into
                    // tvx file, so we know on first read from tvx that
                    // this doc has no TVs
                    return null;
                }
                else
                {
                    return fields;
                }
            }
            else
            {
                return null;
            }
        }

        public override object Clone()
        {
            IndexInput cloneTvx = null;
            IndexInput cloneTvd = null;
            IndexInput cloneTvf = null;

            // These are null when a TermVectorsReader was created
            // on a segment that did not have term vectors saved
            if (Tvx != null && Tvd != null && Tvf != null)
            {
                cloneTvx = (IndexInput)Tvx.Clone();
                cloneTvd = (IndexInput)Tvd.Clone();
                cloneTvf = (IndexInput)Tvf.Clone();
            }

            return new Lucene40TermVectorsReader(FieldInfos, cloneTvx, cloneTvd, cloneTvf, Size_Renamed, NumTotalDocs);
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
        }
    }
}