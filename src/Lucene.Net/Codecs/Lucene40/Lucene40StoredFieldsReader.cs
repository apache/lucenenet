using Lucene.Net.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

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

    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using StoredFieldVisitor = Lucene.Net.Index.StoredFieldVisitor;

    /// <summary>
    /// Class responsible for access to stored document fields.
    /// <para/>
    /// It uses &lt;segment&gt;.fdt and &lt;segment&gt;.fdx; files.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <seealso cref="Lucene40StoredFieldsFormat"/>
    public sealed class Lucene40StoredFieldsReader : StoredFieldsReader // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private readonly FieldInfos fieldInfos;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IndexInput fieldsStream;
        private readonly IndexInput indexStream;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly int numTotalDocs; // LUCENENET: marked readonly
        private readonly int size; // LUCENENET: marked readonly
        private bool closed;

        /// <summary>
        /// Returns a cloned FieldsReader that shares open
        /// <see cref="IndexInput"/>s with the original one.  It is the caller's
        /// job not to dispose the original FieldsReader until all
        /// clones are called (eg, currently <see cref="Index.SegmentReader"/> manages
        /// this logic).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object Clone()
        {
            EnsureOpen();
            return new Lucene40StoredFieldsReader(fieldInfos, numTotalDocs, size, (IndexInput)fieldsStream.Clone(), (IndexInput)indexStream.Clone());
        }

        /// <summary>
        /// Used only by clone. </summary>
        private Lucene40StoredFieldsReader(FieldInfos fieldInfos, int numTotalDocs, int size, IndexInput fieldsStream, IndexInput indexStream)
        {
            this.fieldInfos = fieldInfos;
            this.numTotalDocs = numTotalDocs;
            this.size = size;
            this.fieldsStream = fieldsStream;
            this.indexStream = indexStream;
        }

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40StoredFieldsReader(Directory d, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            string segment = si.Name;
            bool success = false;
            fieldInfos = fn;
            try
            {
                fieldsStream = d.OpenInput(IndexFileNames.SegmentFileName(segment, "", Lucene40StoredFieldsWriter.FIELDS_EXTENSION), context);
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, "", Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);
                indexStream = d.OpenInput(indexStreamFN, context);

                CodecUtil.CheckHeader(indexStream, Lucene40StoredFieldsWriter.CODEC_NAME_IDX, Lucene40StoredFieldsWriter.VERSION_START, Lucene40StoredFieldsWriter.VERSION_CURRENT);
                CodecUtil.CheckHeader(fieldsStream, Lucene40StoredFieldsWriter.CODEC_NAME_DAT, Lucene40StoredFieldsWriter.VERSION_START, Lucene40StoredFieldsWriter.VERSION_CURRENT);
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(Lucene40StoredFieldsWriter.HEADER_LENGTH_DAT == fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    Debugging.Assert(Lucene40StoredFieldsWriter.HEADER_LENGTH_IDX == indexStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }
                long indexSize = indexStream.Length - Lucene40StoredFieldsWriter.HEADER_LENGTH_IDX;
                this.size = (int)(indexSize >> 3);
                // Verify two sources of "maxDoc" agree:
                if (this.size != si.DocCount)
                {
                    throw new CorruptIndexException("doc counts differ for segment " + segment + ": fieldsReader shows " + this.size + " but segmentInfo shows " + si.DocCount);
                }
                numTotalDocs = (int)(indexSize >> 3);
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
                    catch (Exception t) when (t.IsThrowable())
                    {
                    }
                }
            }
        }

        /// <exception cref="ObjectDisposedException"> if this FieldsReader is disposed. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureOpen()
        {
            if (closed)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this FieldsReader is disposed.");
            }
        }

        /// <summary>
        /// Closes the underlying <see cref="Lucene.Net.Store.IndexInput"/> streams.
        /// This means that the <see cref="Index.Fields"/> values will not be accessible.
        /// </summary>
        /// <exception cref="IOException"> If an I/O error occurs. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!closed)
                {
                    IOUtils.Dispose(fieldsStream, indexStream);
                    closed = true;
                }
            }
        }

        /// <summary>
        /// Returns number of documents.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public int Count => size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SeekIndex(int docID)
        {
            indexStream.Seek(Lucene40StoredFieldsWriter.HEADER_LENGTH_IDX + docID * 8L);
        }

        public override void VisitDocument(int n, StoredFieldVisitor visitor)
        {
            SeekIndex(n);
            fieldsStream.Seek(indexStream.ReadInt64());

            int numFields = fieldsStream.ReadVInt32();
            for (int fieldIDX = 0; fieldIDX < numFields; fieldIDX++)
            {
                int fieldNumber = fieldsStream.ReadVInt32();
                FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);

                int bits = fieldsStream.ReadByte() & 0xFF;
                if (Debugging.AssertsEnabled) Debugging.Assert(bits <= (Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_MASK | Lucene40StoredFieldsWriter.FIELD_IS_BINARY),"bits={0:x}", bits);

                switch (visitor.NeedsField(fieldInfo))
                {
                    case StoredFieldVisitor.Status.YES:
                        ReadField(visitor, fieldInfo, bits);
                        break;

                    case StoredFieldVisitor.Status.NO:
                        SkipField(bits);
                        break;

                    case StoredFieldVisitor.Status.STOP:
                        return;
                }
            }
        }

        private void ReadField(StoredFieldVisitor visitor, FieldInfo info, int bits)
        {
            int numeric = bits & Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_MASK;
            if (numeric != 0)
            {
                switch (numeric)
                {
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_INT:
                        visitor.Int32Field(info, fieldsStream.ReadInt32());
                        return;

                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_LONG:
                        visitor.Int64Field(info, fieldsStream.ReadInt64());
                        return;

                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_FLOAT:
                        visitor.SingleField(info, J2N.BitConversion.Int32BitsToSingle(fieldsStream.ReadInt32()));
                        return;

                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_DOUBLE:
                        visitor.DoubleField(info, J2N.BitConversion.Int64BitsToDouble(fieldsStream.ReadInt64()));
                        return;

                    default:
                        throw new CorruptIndexException("Invalid numeric type: " + numeric.ToString("x"));
                }
            }
            else
            {
                int length = fieldsStream.ReadVInt32();
                var bytes = new byte[length];
                fieldsStream.ReadBytes(bytes, 0, length);
                if ((bits & Lucene40StoredFieldsWriter.FIELD_IS_BINARY) != 0)
                {
                    visitor.BinaryField(info, bytes);
                }
                else
                {
#pragma warning disable 612, 618
                    visitor.StringField(info, IOUtils.CHARSET_UTF_8.GetString(bytes));
#pragma warning restore 612, 618
                }
            }
        }

        private void SkipField(int bits)
        {
            int numeric = bits & Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_MASK;
            if (numeric != 0)
            {
                switch (numeric)
                {
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_INT:
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_FLOAT:
                        fieldsStream.ReadInt32();
                        return;

                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_LONG:
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_DOUBLE:
                        fieldsStream.ReadInt64();
                        return;

                    default:
                        throw new CorruptIndexException("Invalid numeric type: " + numeric.ToString("x"));
                }
            }
            else
            {
                int length = fieldsStream.ReadVInt32();
                fieldsStream.Seek(fieldsStream.Position + length); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
        }

        /// <summary>
        /// Returns the length in bytes of each raw document in a
        /// contiguous range of length <paramref name="numDocs"/> starting with
        /// <paramref name="startDocID"/>.  Returns the <see cref="IndexInput"/> (the fieldStream),
        /// already seeked to the starting point for <paramref name="startDocID"/>.
        /// </summary>
        public IndexInput RawDocs(int[] lengths, int startDocID, int numDocs)
        {
            SeekIndex(startDocID);
            long startOffset = indexStream.ReadInt64();
            long lastOffset = startOffset;
            int count = 0;
            while (count < numDocs)
            {
                long offset;
                int docID = startDocID + count + 1;
                if (Debugging.AssertsEnabled) Debugging.Assert(docID <= numTotalDocs);
                if (docID < numTotalDocs)
                {
                    offset = indexStream.ReadInt64();
                }
                else
                {
                    offset = fieldsStream.Length;
                }
                lengths[count++] = (int)(offset - lastOffset);
                lastOffset = offset;
            }

            fieldsStream.Seek(startOffset);

            return fieldsStream;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CheckIntegrity()
        {
        }
    }
}