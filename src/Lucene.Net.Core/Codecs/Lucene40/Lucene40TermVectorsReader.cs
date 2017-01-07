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

        private FieldInfos fieldInfos;

        private IndexInput tvx;
        private IndexInput tvd;
        private IndexInput tvf;
        private int size;
        private int numTotalDocs;

        /// <summary>
        /// Used by clone. </summary>
        internal Lucene40TermVectorsReader(FieldInfos fieldInfos, IndexInput tvx, IndexInput tvd, IndexInput tvf, int size, int numTotalDocs)
        {
            this.fieldInfos = fieldInfos;
            this.tvx = tvx;
            this.tvd = tvd;
            this.tvf = tvf;
            this.size = size;
            this.numTotalDocs = numTotalDocs;
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
                tvx = d.OpenInput(idxName, context);
                int tvxVersion = CodecUtil.CheckHeader(tvx, CODEC_NAME_INDEX, VERSION_START, VERSION_CURRENT);

                string fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_DOCUMENTS_EXTENSION);
                tvd = d.OpenInput(fn, context);
                int tvdVersion = CodecUtil.CheckHeader(tvd, CODEC_NAME_DOCS, VERSION_START, VERSION_CURRENT);
                fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_FIELDS_EXTENSION);
                tvf = d.OpenInput(fn, context);
                int tvfVersion = CodecUtil.CheckHeader(tvf, CODEC_NAME_FIELDS, VERSION_START, VERSION_CURRENT);
                Debug.Assert(HEADER_LENGTH_INDEX == tvx.FilePointer);
                Debug.Assert(HEADER_LENGTH_DOCS == tvd.FilePointer);
                Debug.Assert(HEADER_LENGTH_FIELDS == tvf.FilePointer);
                Debug.Assert(tvxVersion == tvdVersion);
                Debug.Assert(tvxVersion == tvfVersion);

                numTotalDocs = (int)(tvx.Length - HEADER_LENGTH_INDEX >> 4);

                this.size = numTotalDocs;
                Debug.Assert(size == 0 || numTotalDocs == size);

                this.fieldInfos = fieldInfos;
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
                return tvd;
            }
        }

        // Used for bulk copy when merging
        internal virtual IndexInput TvfStream
        {
            get
            {
                return tvf;
            }
        }

        // Not private to avoid synthetic access$NNN methods
        internal virtual void SeekTvx(int docNum)
        {
            tvx.Seek(docNum * 16L + HEADER_LENGTH_INDEX);
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
            if (tvx == null)
            {
                CollectionsHelper.Fill(tvdLengths, 0);
                CollectionsHelper.Fill(tvfLengths, 0);
                return;
            }

            SeekTvx(startDocID);

            long tvdPosition = tvx.ReadLong();
            tvd.Seek(tvdPosition);

            long tvfPosition = tvx.ReadLong();
            tvf.Seek(tvfPosition);

            long lastTvdPosition = tvdPosition;
            long lastTvfPosition = tvfPosition;

            int count = 0;
            while (count < numDocs)
            {
                int docID = startDocID + count + 1;
                Debug.Assert(docID <= numTotalDocs);
                if (docID < numTotalDocs)
                {
                    tvdPosition = tvx.ReadLong();
                    tvfPosition = tvx.ReadLong();
                }
                else
                {
                    tvdPosition = tvd.Length;
                    tvfPosition = tvf.Length;
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
                IOUtils.Close(tvx, tvd, tvf);
        }

        /// <summary>
        /// The number of documents in the reader 
        /// NOTE: This was size() in Lucene.
        /// </summary>
        internal virtual int Count
        {
            get { return size; }
        }

        private class TVFields : Fields
        {
            private readonly Lucene40TermVectorsReader outerInstance;

            private readonly int[] fieldNumbers;
            private readonly long[] fieldFPs;
            private readonly IDictionary<int, int> fieldNumberToIndex = new Dictionary<int, int>();

            public TVFields(Lucene40TermVectorsReader outerInstance, int docID)
            {
                this.outerInstance = outerInstance;
                outerInstance.SeekTvx(docID);
                outerInstance.tvd.Seek(outerInstance.tvx.ReadLong());

                int fieldCount = outerInstance.tvd.ReadVInt();
                Debug.Assert(fieldCount >= 0);
                if (fieldCount != 0)
                {
                    fieldNumbers = new int[fieldCount];
                    fieldFPs = new long[fieldCount];
                    for (int fieldUpto = 0; fieldUpto < fieldCount; fieldUpto++)
                    {
                        int fieldNumber = outerInstance.tvd.ReadVInt();
                        fieldNumbers[fieldUpto] = fieldNumber;
                        fieldNumberToIndex[fieldNumber] = fieldUpto;
                    }

                    long position = outerInstance.tvx.ReadLong();
                    fieldFPs[0] = position;
                    for (int fieldUpto = 1; fieldUpto < fieldCount; fieldUpto++)
                    {
                        position += outerInstance.tvd.ReadVLong();
                        fieldFPs[fieldUpto] = position;
                    }
                }
                else
                {
                    // TODO: we can improve writer here, eg write 0 into
                    // tvx file, so we know on first read from tvx that
                    // this doc has no TVs
                    fieldNumbers = null;
                    fieldFPs = null;
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return GetFieldInfoEnumerable().GetEnumerator();
            }

            private IEnumerable<string> GetFieldInfoEnumerable()
            {
                int fieldUpto = 0;
                while (fieldNumbers != null && fieldUpto < fieldNumbers.Length)
                {
                    yield return outerInstance.fieldInfos.FieldInfo(fieldNumbers[fieldUpto++]).Name;
                }
            }

            public override Terms Terms(string field)
            {
                FieldInfo fieldInfo = outerInstance.fieldInfos.FieldInfo(field);
                if (fieldInfo == null)
                {
                    // No such field
                    return null;
                }

                int fieldIndex;
                if (!fieldNumberToIndex.TryGetValue(fieldInfo.Number, out fieldIndex))
                {
                    // Term vectors were not indexed for this field
                    return null;
                }

                return new TVTerms(outerInstance, fieldFPs[fieldIndex]);
            }

            public override int Count
            {
                get
                {
                    if (fieldNumbers == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return fieldNumbers.Length;
                    }
                }
            }
        }

        private class TVTerms : Terms
        {
            private readonly Lucene40TermVectorsReader outerInstance;

            private readonly int numTerms;
            private readonly long tvfFPStart;
            private readonly bool storePositions;
            private readonly bool storeOffsets;
            private readonly bool storePayloads;

            public TVTerms(Lucene40TermVectorsReader outerInstance, long tvfFP)
            {
                this.outerInstance = outerInstance;
                outerInstance.tvf.Seek(tvfFP);
                numTerms = outerInstance.tvf.ReadVInt();
                byte bits = outerInstance.tvf.ReadByte();
                storePositions = (bits & STORE_POSITIONS_WITH_TERMVECTOR) != 0;
                storeOffsets = (bits & STORE_OFFSET_WITH_TERMVECTOR) != 0;
                storePayloads = (bits & STORE_PAYLOAD_WITH_TERMVECTOR) != 0;
                tvfFPStart = outerInstance.tvf.FilePointer;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                TVTermsEnum termsEnum;
                if (reuse is TVTermsEnum)
                {
                    termsEnum = (TVTermsEnum)reuse;
                    if (!termsEnum.CanReuse(outerInstance.tvf))
                    {
                        termsEnum = new TVTermsEnum(outerInstance);
                    }
                }
                else
                {
                    termsEnum = new TVTermsEnum(outerInstance);
                }
                termsEnum.Reset(numTerms, tvfFPStart, storePositions, storeOffsets, storePayloads);
                return termsEnum;
            }

            public override long Count
            {
                get { return numTerms; }
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
                    return numTerms;
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
                get { return storeOffsets; }
            }

            public override bool HasPositions
            {
                get { return storePositions; }
            }

            public override bool HasPayloads
            {
                get { return storePayloads; }
            }
        }

        private class TVTermsEnum : TermsEnum
        {
            private readonly Lucene40TermVectorsReader outerInstance;

            private readonly IndexInput origTVF;
            private readonly IndexInput tvf;
            private int numTerms;
            private int nextTerm;
            private int freq;
            private readonly BytesRef lastTerm = new BytesRef();
            private readonly BytesRef term = new BytesRef();
            private bool storePositions;
            private bool storeOffsets;
            private bool storePayloads;
            private long tvfFP;

            private int[] positions;
            private int[] startOffsets;
            private int[] endOffsets;

            // one shared byte[] for any term's payloads
            private int[] payloadOffsets;

            private int lastPayloadLength;
            private byte[] payloadData;

            // NOTE: tvf is pre-positioned by caller
            public TVTermsEnum(Lucene40TermVectorsReader outerInstance)
            {
                this.outerInstance = outerInstance;
                this.origTVF = outerInstance.tvf;
                tvf = (IndexInput)origTVF.Clone();
            }

            public virtual bool CanReuse(IndexInput tvf)
            {
                return tvf == origTVF;
            }

            public virtual void Reset(int numTerms, long tvfFPStart, bool storePositions, bool storeOffsets, bool storePayloads)
            {
                this.numTerms = numTerms;
                this.storePositions = storePositions;
                this.storeOffsets = storeOffsets;
                this.storePayloads = storePayloads;
                nextTerm = 0;
                tvf.Seek(tvfFPStart);
                tvfFP = tvfFPStart;
                positions = null;
                startOffsets = null;
                endOffsets = null;
                payloadOffsets = null;
                payloadData = null;
                lastPayloadLength = -1;
            }

            // NOTE: slow!  (linear scan)
            public override SeekStatus SeekCeil(BytesRef text)
            {
                if (nextTerm != 0)
                {
                    int cmp = text.CompareTo(term);
                    if (cmp < 0)
                    {
                        nextTerm = 0;
                        tvf.Seek(tvfFP);
                    }
                    else if (cmp == 0)
                    {
                        return SeekStatus.FOUND;
                    }
                }

                while (Next() != null)
                {
                    int cmp = text.CompareTo(term);
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
                if (nextTerm >= numTerms)
                {
                    return null;
                }
                term.CopyBytes(lastTerm);
                int start = tvf.ReadVInt();
                int deltaLen = tvf.ReadVInt();
                term.Length = start + deltaLen;
                term.Grow(term.Length);
                tvf.ReadBytes(term.Bytes, start, deltaLen);
                freq = tvf.ReadVInt();

                if (storePayloads)
                {
                    positions = new int[freq];
                    payloadOffsets = new int[freq];
                    int totalPayloadLength = 0;
                    int pos = 0;
                    for (int posUpto = 0; posUpto < freq; posUpto++)
                    {
                        int code = tvf.ReadVInt();
                        pos += (int)((uint)code >> 1);
                        positions[posUpto] = pos;
                        if ((code & 1) != 0)
                        {
                            // length change
                            lastPayloadLength = tvf.ReadVInt();
                        }
                        payloadOffsets[posUpto] = totalPayloadLength;
                        totalPayloadLength += lastPayloadLength;
                        Debug.Assert(totalPayloadLength >= 0);
                    }
                    payloadData = new byte[totalPayloadLength];
                    tvf.ReadBytes(payloadData, 0, payloadData.Length);
                } // no payloads
                else if (storePositions)
                {
                    // TODO: we could maybe reuse last array, if we can
                    // somehow be careful about consumer never using two
                    // D&PEnums at once...
                    positions = new int[freq];
                    int pos = 0;
                    for (int posUpto = 0; posUpto < freq; posUpto++)
                    {
                        pos += tvf.ReadVInt();
                        positions[posUpto] = pos;
                    }
                }

                if (storeOffsets)
                {
                    startOffsets = new int[freq];
                    endOffsets = new int[freq];
                    int offset = 0;
                    for (int posUpto = 0; posUpto < freq; posUpto++)
                    {
                        startOffsets[posUpto] = offset + tvf.ReadVInt();
                        offset = endOffsets[posUpto] = startOffsets[posUpto] + tvf.ReadVInt();
                    }
                }

                lastTerm.CopyBytes(term);
                nextTerm++;
                return term;
            }

            public override BytesRef Term
            {
                get { return term; }
            }

            public override long Ord
            {
                get { throw new System.NotSupportedException(); }
            }

            public override int DocFreq
            {
                get { return 1; }
            }

            public override long TotalTermFreq
            {
                get { return freq; }
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
                docsEnum.Reset(liveDocs, freq);
                return docsEnum;
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                if (!storePositions && !storeOffsets)
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
                docsAndPositionsEnum.Reset(liveDocs, positions, startOffsets, endOffsets, payloadOffsets, payloadData);
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
            private bool didNext;
            private int doc = -1;
            private int freq;
            private IBits liveDocs;

            public override int Freq
            {
                get { return freq; }
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                if (!didNext && (liveDocs == null || liveDocs.Get(0)))
                {
                    didNext = true;
                    return (doc = 0);
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public virtual void Reset(IBits liveDocs, int freq)
            {
                this.liveDocs = liveDocs;
                this.freq = freq;
                this.doc = -1;
                didNext = false;
            }

            public override long Cost()
            {
                return 1;
            }
        }

        private sealed class TVDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private bool didNext;
            private int doc = -1;
            private int nextPos;
            private IBits liveDocs;
            private int[] positions;
            private int[] startOffsets;
            private int[] endOffsets;
            private int[] payloadOffsets;
            private readonly BytesRef payload = new BytesRef();
            private byte[] payloadBytes;

            public override int Freq
            {
                get
                {
                    if (positions != null)
                    {
                        return positions.Length;
                    }
                    else
                    {
                        Debug.Assert(startOffsets != null);
                        return startOffsets.Length;
                    }
                }
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                if (!didNext && (liveDocs == null || liveDocs.Get(0)))
                {
                    didNext = true;
                    return (doc = 0);
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public void Reset(IBits liveDocs, int[] positions, int[] startOffsets, int[] endOffsets, int[] payloadLengths, byte[] payloadBytes)
            {
                this.liveDocs = liveDocs;
                this.positions = positions;
                this.startOffsets = startOffsets;
                this.endOffsets = endOffsets;
                this.payloadOffsets = payloadLengths;
                this.payloadBytes = payloadBytes;
                this.doc = -1;
                didNext = false;
                nextPos = 0;
            }

            public override BytesRef Payload
            {
                get
                {
                    if (payloadOffsets == null)
                    {
                        return null;
                    }
                    else
                    {
                        int off = payloadOffsets[nextPos - 1];
                        int end = nextPos == payloadOffsets.Length ? payloadBytes.Length : payloadOffsets[nextPos];
                        if (end - off == 0)
                        {
                            return null;
                        }
                        payload.Bytes = payloadBytes;
                        payload.Offset = off;
                        payload.Length = end - off;
                        return payload;
                    }
                }
            }

            public override int NextPosition()
            {
                Debug.Assert((positions != null && nextPos < positions.Length) || startOffsets != null && nextPos < startOffsets.Length);

                if (positions != null)
                {
                    return positions[nextPos++];
                }
                else
                {
                    nextPos++;
                    return -1;
                }
            }

            public override int StartOffset
            {
                get
                {
                    if (startOffsets == null)
                    {
                        return -1;
                    }
                    else
                    {
                        return startOffsets[nextPos - 1];
                    }
                }
            }

            public override int EndOffset
            {
                get
                {
                    if (endOffsets == null)
                    {
                        return -1;
                    }
                    else
                    {
                        return endOffsets[nextPos - 1];
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
            if (tvx != null)
            {
                Fields fields = new TVFields(this, docID);
                if (fields.Count == 0)
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
            if (tvx != null && tvd != null && tvf != null)
            {
                cloneTvx = (IndexInput)tvx.Clone();
                cloneTvd = (IndexInput)tvd.Clone();
                cloneTvf = (IndexInput)tvf.Clone();
            }

            return new Lucene40TermVectorsReader(fieldInfos, cloneTvx, cloneTvd, cloneTvf, size, numTotalDocs);
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