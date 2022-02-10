using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene3x
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

#pragma warning disable 612, 618
    internal sealed class PreFlexRWTermVectorsWriter : TermVectorsWriter
    {
        private readonly Directory directory;
        private readonly string segment;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput tvx = null, tvd = null, tvf = null;
#pragma warning restore CA2213 // Disposable fields should be disposed

        public PreFlexRWTermVectorsWriter(Directory directory, string segment, IOContext context)
        {
            this.directory = directory;
            this.segment = segment;
            bool success = false;
            try
            {
                // Open files for TermVector storage
                tvx = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION), context);
                tvx.WriteInt32(Lucene3xTermVectorsReader.FORMAT_CURRENT);
                tvd = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION), context);
                tvd.WriteInt32(Lucene3xTermVectorsReader.FORMAT_CURRENT);
                tvf = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION), context);
                tvf.WriteInt32(Lucene3xTermVectorsReader.FORMAT_CURRENT);
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
            lastFieldName = null;
            this.numVectorFields = numVectorFields;
            tvx.WriteInt64(tvd.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            tvx.WriteInt64(tvf.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            tvd.WriteVInt32(numVectorFields);
            fieldCount = 0;
            fps = ArrayUtil.Grow(fps, numVectorFields);
        }

        private long[] fps = new long[10]; // pointers to the tvf before writing each field
        private int fieldCount = 0; // number of fields we have written so far for this document
        private int numVectorFields = 0; // total number of fields we will write for this document
        private string lastFieldName;

        public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(lastFieldName is null || info.Name.CompareToOrdinal(lastFieldName) > 0,"fieldName={0} lastFieldName={1}", info.Name, lastFieldName);
            lastFieldName = info.Name;
            if (payloads)
            {
                throw UnsupportedOperationException.Create("3.x codec does not support payloads on vectors!");
            }
            this.positions = positions;
            this.offsets = offsets;
            lastTerm.Length = 0;
            fps[fieldCount++] = tvf.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            tvd.WriteVInt32(info.Number);
            tvf.WriteVInt32(numTerms);
            sbyte bits = 0x0;
            if (positions)
            {
                bits |= Lucene3xTermVectorsReader.STORE_POSITIONS_WITH_TERMVECTOR;
            }
            if (offsets)
            {
                bits |= Lucene3xTermVectorsReader.STORE_OFFSET_WITH_TERMVECTOR;
            }
            tvf.WriteByte((byte)bits);

            if (Debugging.AssertsEnabled) Debugging.Assert(fieldCount <= numVectorFields);
            if (fieldCount == numVectorFields)
            {
                // last field of the document
                // this is crazy because the file format is crazy!
                for (int i = 1; i < fieldCount; i++)
                {
                    tvd.WriteVInt64(fps[i] - fps[i - 1]);
                }
            }
        }

        private readonly BytesRef lastTerm = new BytesRef(10);

        // NOTE: we override addProx, so we don't need to buffer when indexing.
        // we also don't buffer during bulk merges.
        private int[] offsetStartBuffer = new int[10];

        private int[] offsetEndBuffer = new int[10];
        private int offsetIndex = 0;
        private int offsetFreq = 0;
        private bool positions = false;
        private bool offsets = false;

        public override void StartTerm(BytesRef term, int freq)
        {
            int prefix = StringHelper.BytesDifference(lastTerm, term);
            int suffix = term.Length - prefix;
            tvf.WriteVInt32(prefix);
            tvf.WriteVInt32(suffix);
            tvf.WriteBytes(term.Bytes, term.Offset + prefix, suffix);
            tvf.WriteVInt32(freq);
            lastTerm.CopyBytes(term);
            lastPosition = lastOffset = 0;

            if (offsets && positions)
            {
                // we might need to buffer if its a non-bulk merge
                offsetStartBuffer = ArrayUtil.Grow(offsetStartBuffer, freq);
                offsetEndBuffer = ArrayUtil.Grow(offsetEndBuffer, freq);
                offsetIndex = 0;
                offsetFreq = freq;
            }
        }

        internal int lastPosition = 0;
        internal int lastOffset = 0;

        public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(payload is null);
            if (positions && offsets)
            {
                // write position delta
                tvf.WriteVInt32(position - lastPosition);
                lastPosition = position;

                // buffer offsets
                offsetStartBuffer[offsetIndex] = startOffset;
                offsetEndBuffer[offsetIndex] = endOffset;
                offsetIndex++;

                // dump buffer if we are done
                if (offsetIndex == offsetFreq)
                {
                    for (int i = 0; i < offsetIndex; i++)
                    {
                        tvf.WriteVInt32(offsetStartBuffer[i] - lastOffset);
                        tvf.WriteVInt32(offsetEndBuffer[i] - offsetStartBuffer[i]);
                        lastOffset = offsetEndBuffer[i];
                    }
                }
            }
            else if (positions)
            {
                // write position delta
                tvf.WriteVInt32(position - lastPosition);
                lastPosition = position;
            }
            else if (offsets)
            {
                // write offset deltas
                tvf.WriteVInt32(startOffset - lastOffset);
                tvf.WriteVInt32(endOffset - startOffset);
                lastOffset = endOffset;
            }
        }

        public override void Abort()
        {
            try
            {
                Dispose();
            }
            catch (Exception ignored) when (ignored.IsThrowable())
            {
            }
            IOUtils.DeleteFilesIgnoringExceptions(directory, IndexFileNames.SegmentFileName(segment, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION),
                IndexFileNames.SegmentFileName(segment, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION),
                IndexFileNames.SegmentFileName(segment, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION));
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (4 + ((long)numDocs) * 16 != tvx.Position) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            // this is most likely a bug in Sun JRE 1.6.0_04/_05;
            // we detect that the bug has struck, here, and
            // throw an exception to prevent the corruption from
            // entering the index.  See LUCENE-1282 for
            // details.
            {
                throw RuntimeException.Create("tvx size mismatch: mergedDocs is " + numDocs + " but tvx size is " + tvx.Position + " file=" + tvx.ToString() + "; now aborting this merge to prevent index corruption");
            }
        }

        /// <summary>
        /// Close all streams. </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // make an effort to close all streams we can but remember and re-throw
                // the first exception encountered in this process
                IOUtils.Dispose(tvx, tvd, tvf);
                tvx = tvd = tvf = null;
            }
        }

        public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUTF16Comparer;
    }
#pragma warning restore 612, 618
}