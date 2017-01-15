using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;

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

    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using StringHelper = Lucene.Net.Util.StringHelper;

    internal sealed class PreFlexRWTermVectorsWriter : TermVectorsWriter
    {
        private readonly Directory Directory;
        private readonly string Segment;
        private IndexOutput Tvx = null, Tvd = null, Tvf = null;

        public PreFlexRWTermVectorsWriter(Directory directory, string segment, IOContext context)
        {
            this.Directory = directory;
            this.Segment = segment;
            bool success = false;
            try
            {
                // Open files for TermVector storage
                Tvx = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION), context);
                Tvx.WriteInt(Lucene3xTermVectorsReader.FORMAT_CURRENT);
                Tvd = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION), context);
                Tvd.WriteInt(Lucene3xTermVectorsReader.FORMAT_CURRENT);
                Tvf = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION), context);
                Tvf.WriteInt(Lucene3xTermVectorsReader.FORMAT_CURRENT);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    Abort();
                }
            }
        }

        public override void StartDocument(int numVectorFields)
        {
            LastFieldName = null;
            this.NumVectorFields = numVectorFields;
            Tvx.WriteLong(Tvd.FilePointer);
            Tvx.WriteLong(Tvf.FilePointer);
            Tvd.WriteVInt(numVectorFields);
            FieldCount = 0;
            Fps = ArrayUtil.Grow(Fps, numVectorFields);
        }

        private long[] Fps = new long[10]; // pointers to the tvf before writing each field
        private int FieldCount = 0; // number of fields we have written so far for this document
        private int NumVectorFields = 0; // total number of fields we will write for this document
        private string LastFieldName;

        public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            Debug.Assert(LastFieldName == null || info.Name.CompareTo(LastFieldName) > 0, "fieldName=" + info.Name + " lastFieldName=" + LastFieldName);
            LastFieldName = info.Name;
            if (payloads)
            {
                throw new System.NotSupportedException("3.x codec does not support payloads on vectors!");
            }
            this.Positions = positions;
            this.Offsets = offsets;
            LastTerm.Length = 0;
            Fps[FieldCount++] = Tvf.FilePointer;
            Tvd.WriteVInt(info.Number);
            Tvf.WriteVInt(numTerms);
            sbyte bits = 0x0;
            if (positions)
            {
                bits |= Lucene3xTermVectorsReader.STORE_POSITIONS_WITH_TERMVECTOR;
            }
            if (offsets)
            {
                bits |= Lucene3xTermVectorsReader.STORE_OFFSET_WITH_TERMVECTOR;
            }
            Tvf.WriteByte((byte)bits);

            Debug.Assert(FieldCount <= NumVectorFields);
            if (FieldCount == NumVectorFields)
            {
                // last field of the document
                // this is crazy because the file format is crazy!
                for (int i = 1; i < FieldCount; i++)
                {
                    Tvd.WriteVLong(Fps[i] - Fps[i - 1]);
                }
            }
        }

        private readonly BytesRef LastTerm = new BytesRef(10);

        // NOTE: we override addProx, so we don't need to buffer when indexing.
        // we also don't buffer during bulk merges.
        private int[] OffsetStartBuffer = new int[10];

        private int[] OffsetEndBuffer = new int[10];
        private int OffsetIndex = 0;
        private int OffsetFreq = 0;
        private bool Positions = false;
        private bool Offsets = false;

        public override void StartTerm(BytesRef term, int freq)
        {
            int prefix = StringHelper.BytesDifference(LastTerm, term);
            int suffix = term.Length - prefix;
            Tvf.WriteVInt(prefix);
            Tvf.WriteVInt(suffix);
            Tvf.WriteBytes(term.Bytes, term.Offset + prefix, suffix);
            Tvf.WriteVInt(freq);
            LastTerm.CopyBytes(term);
            LastPosition = LastOffset = 0;

            if (Offsets && Positions)
            {
                // we might need to buffer if its a non-bulk merge
                OffsetStartBuffer = ArrayUtil.Grow(OffsetStartBuffer, freq);
                OffsetEndBuffer = ArrayUtil.Grow(OffsetEndBuffer, freq);
                OffsetIndex = 0;
                OffsetFreq = freq;
            }
        }

        internal int LastPosition = 0;
        internal int LastOffset = 0;

        public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
        {
            Debug.Assert(payload == null);
            if (Positions && Offsets)
            {
                // write position delta
                Tvf.WriteVInt(position - LastPosition);
                LastPosition = position;

                // buffer offsets
                OffsetStartBuffer[OffsetIndex] = startOffset;
                OffsetEndBuffer[OffsetIndex] = endOffset;
                OffsetIndex++;

                // dump buffer if we are done
                if (OffsetIndex == OffsetFreq)
                {
                    for (int i = 0; i < OffsetIndex; i++)
                    {
                        Tvf.WriteVInt(OffsetStartBuffer[i] - LastOffset);
                        Tvf.WriteVInt(OffsetEndBuffer[i] - OffsetStartBuffer[i]);
                        LastOffset = OffsetEndBuffer[i];
                    }
                }
            }
            else if (Positions)
            {
                // write position delta
                Tvf.WriteVInt(position - LastPosition);
                LastPosition = position;
            }
            else if (Offsets)
            {
                // write offset deltas
                Tvf.WriteVInt(startOffset - LastOffset);
                Tvf.WriteVInt(endOffset - startOffset);
                LastOffset = endOffset;
            }
        }

        public override void Abort()
        {
            try
            {
                Dispose();
            }
            catch (Exception ignored)
            {
            }
            IOUtils.DeleteFilesIgnoringExceptions(Directory, IndexFileNames.SegmentFileName(Segment, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION), IndexFileNames.SegmentFileName(Segment, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION), IndexFileNames.SegmentFileName(Segment, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION));
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (4 + ((long)numDocs) * 16 != Tvx.FilePointer)
            // this is most likely a bug in Sun JRE 1.6.0_04/_05;
            // we detect that the bug has struck, here, and
            // throw an exception to prevent the corruption from
            // entering the index.  See LUCENE-1282 for
            // details.
            {
                throw new Exception("tvx size mismatch: mergedDocs is " + numDocs + " but tvx size is " + Tvx.FilePointer + " file=" + Tvx.ToString() + "; now aborting this merge to prevent index corruption");
            }
        }

        /// <summary>
        /// Close all streams. </summary>
        protected override void Dispose(bool disposing)
        {
            // make an effort to close all streams we can but remember and re-throw
            // the first exception encountered in this process
            IOUtils.Close(Tvx, Tvd, Tvf);
            Tvx = Tvd = Tvf = null;
        }

        public override IComparer<BytesRef> Comparer
        {
            get
            {
                return BytesRef.UTF8SortedAsUTF16Comparer;
            }
        }
    }
}