using Lucene.Net.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;

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

    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexFormatTooNewException = Lucene.Net.Index.IndexFormatTooNewException;
    using IndexFormatTooOldException = Lucene.Net.Index.IndexFormatTooOldException;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using StoredFieldVisitor = Lucene.Net.Index.StoredFieldVisitor;

    /// <summary>
    /// Class responsible for access to stored document fields.
    /// <para/>
    /// It uses &lt;segment&gt;.fdt and &lt;segment&gt;.fdx; files.
    /// </summary>
    [Obsolete("Only for reading existing 3.x indexes")]
    internal sealed class Lucene3xStoredFieldsReader : StoredFieldsReader, IDisposable // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private const int FORMAT_SIZE = 4;

        /// <summary>
        /// Extension of stored fields file. </summary>
        public const string FIELDS_EXTENSION = "fdt";

        /// <summary>
        /// Extension of stored fields index file. </summary>
        public const string FIELDS_INDEX_EXTENSION = "fdx";

        // Lucene 3.0: Removal of compressed fields
        internal const int FORMAT_LUCENE_3_0_NO_COMPRESSED_FIELDS = 2;

        // Lucene 3.2: NumericFields are stored in binary format
        internal const int FORMAT_LUCENE_3_2_NUMERIC_FIELDS = 3;

        // NOTE: if you introduce a new format, make it 1 higher
        // than the current one, and always change this if you
        // switch to a new format!
        public const int FORMAT_CURRENT = FORMAT_LUCENE_3_2_NUMERIC_FIELDS;

        // when removing support for old versions, leave the last supported version here
        internal const int FORMAT_MINIMUM = FORMAT_LUCENE_3_0_NO_COMPRESSED_FIELDS;

        // NOTE: bit 0 is free here!  You can steal it!
        public const int FIELD_IS_BINARY = 1 << 1;

        // the old bit 1 << 2 was compressed, is now left out

        private const int _NUMERIC_BIT_SHIFT = 3;
        internal const int FIELD_IS_NUMERIC_MASK = 0x07 << _NUMERIC_BIT_SHIFT;

        public const int FIELD_IS_NUMERIC_INT = 1 << _NUMERIC_BIT_SHIFT;
        public const int FIELD_IS_NUMERIC_LONG = 2 << _NUMERIC_BIT_SHIFT;
        public const int FIELD_IS_NUMERIC_FLOAT = 3 << _NUMERIC_BIT_SHIFT;
        public const int FIELD_IS_NUMERIC_DOUBLE = 4 << _NUMERIC_BIT_SHIFT;

        private readonly FieldInfos fieldInfos;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IndexInput fieldsStream;
        private readonly IndexInput indexStream;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly int numTotalDocs; // LUCENENET: marked readonly
        private readonly int size; // LUCENENET: marked readonly
        private bool closed;
        private readonly int format;

        // The docID offset where our docs begin in the index
        // file.  this will be 0 if we have our own private file.
        private readonly int docStoreOffset; // LUCENENET: marked readonly

        // when we are inside a compound share doc store (CFX),
        // (lucene 3.0 indexes only), we privately open our own fd.
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly CompoundFileDirectory storeCFSReader;
#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>
        /// Returns a cloned FieldsReader that shares open
        /// IndexInputs with the original one.  It is the caller's
        /// job not to close the original FieldsReader until all
        /// clones are called (eg, currently SegmentReader manages
        /// this logic).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object Clone()
        {
            EnsureOpen();
            return new Lucene3xStoredFieldsReader(fieldInfos, numTotalDocs, size, format, docStoreOffset, (IndexInput)fieldsStream.Clone(), (IndexInput)indexStream.Clone());
        }

        /// <summary>
        /// Verifies that the code version which wrote the segment is supported. </summary>
        public static void CheckCodeVersion(Directory dir, string segment)
        {
            string indexStreamFN = IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION);
            IndexInput idxStream = dir.OpenInput(indexStreamFN, IOContext.DEFAULT);

            try
            {
                int format = idxStream.ReadInt32();
                if (format < FORMAT_MINIMUM)
                {
                    throw new IndexFormatTooOldException(idxStream, format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }
                if (format > FORMAT_CURRENT)
                {
                    throw new IndexFormatTooNewException(idxStream, format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }
            }
            finally
            {
                idxStream.Dispose();
            }
        }

        // Used only by clone
        private Lucene3xStoredFieldsReader(FieldInfos fieldInfos, int numTotalDocs, int size, int format, int docStoreOffset, IndexInput fieldsStream, IndexInput indexStream)
        {
            this.fieldInfos = fieldInfos;
            this.numTotalDocs = numTotalDocs;
            this.size = size;
            this.format = format;
            this.docStoreOffset = docStoreOffset;
            this.fieldsStream = fieldsStream;
            this.indexStream = indexStream;
            this.storeCFSReader = null;
        }

        public Lucene3xStoredFieldsReader(Directory d, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            string segment = Lucene3xSegmentInfoFormat.GetDocStoreSegment(si);
            int docStoreOffset = Lucene3xSegmentInfoFormat.GetDocStoreOffset(si);
            int size = si.DocCount;
            bool success = false;
            fieldInfos = fn;
            try
            {
                if (docStoreOffset != -1 && Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(si))
                {
                    d = storeCFSReader = new CompoundFileDirectory(si.Dir, IndexFileNames.SegmentFileName(segment, "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION), context, false);
                }
                else
                {
                    storeCFSReader = null;
                }
                fieldsStream = d.OpenInput(IndexFileNames.SegmentFileName(segment, "", FIELDS_EXTENSION), context);
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION);
                indexStream = d.OpenInput(indexStreamFN, context);

                format = indexStream.ReadInt32();

                if (format < FORMAT_MINIMUM)
                {
                    throw new IndexFormatTooOldException(indexStream, format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }
                if (format > FORMAT_CURRENT)
                {
                    throw new IndexFormatTooNewException(indexStream, format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }

                long indexSize = indexStream.Length - FORMAT_SIZE;

                if (docStoreOffset != -1)
                {
                    // We read only a slice out of this shared fields file
                    this.docStoreOffset = docStoreOffset;
                    this.size = size;

                    // Verify the file is long enough to hold all of our
                    // docs
                    if (Debugging.AssertsEnabled) Debugging.Assert(((int)(indexSize / 8)) >= size + this.docStoreOffset, "indexSize={0} size={1} docStoreOffset={2}", indexSize, size, docStoreOffset);
                }
                else
                {
                    this.docStoreOffset = 0;
                    this.size = (int)(indexSize >> 3);
                    // Verify two sources of "maxDoc" agree:
                    if (this.size != si.DocCount)
                    {
                        throw new CorruptIndexException("doc counts differ for segment " + segment + ": fieldsReader shows " + this.size + " but segmentInfo shows " + si.DocCount);
                    }
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
                    } // keep our original exception
                    catch (Exception t) when (t.IsThrowable())
                    {
                        // ignored
                    }
                }
            }
        }

        /// <exception cref="ObjectDisposedException"> If this FieldsReader is disposed. </exception>
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
        /// This means that the Fields values will not be accessible.
        /// </summary>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!closed)
                {
                    IOUtils.Dispose(fieldsStream, indexStream, storeCFSReader);
                    closed = true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SeekIndex(int docID)
        {
            indexStream.Seek(FORMAT_SIZE + (docID + docStoreOffset) * 8L);
        }

        public override sealed void VisitDocument(int n, StoredFieldVisitor visitor)
        {
            SeekIndex(n);
            fieldsStream.Seek(indexStream.ReadInt64());

            int numFields = fieldsStream.ReadVInt32();
            for (int fieldIDX = 0; fieldIDX < numFields; fieldIDX++)
            {
                int fieldNumber = fieldsStream.ReadVInt32();
                FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);

                int bits = fieldsStream.ReadByte() & 0xFF;
                if (Debugging.AssertsEnabled) Debugging.Assert(bits <= (FIELD_IS_NUMERIC_MASK | FIELD_IS_BINARY),"bits={0:x}", bits);

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
            int numeric = bits & FIELD_IS_NUMERIC_MASK;
            if (numeric != 0)
            {
                switch (numeric)
                {
                    case FIELD_IS_NUMERIC_INT:
                        visitor.Int32Field(info, fieldsStream.ReadInt32());
                        return;

                    case FIELD_IS_NUMERIC_LONG:
                        visitor.Int64Field(info, fieldsStream.ReadInt64());
                        return;

                    case FIELD_IS_NUMERIC_FLOAT:
                        visitor.SingleField(info, J2N.BitConversion.Int32BitsToSingle(fieldsStream.ReadInt32()));
                        return;

                    case FIELD_IS_NUMERIC_DOUBLE:
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
                if ((bits & FIELD_IS_BINARY) != 0)
                {
                    visitor.BinaryField(info, bytes);
                }
                else
                {
                    visitor.StringField(info, IOUtils.CHARSET_UTF_8.GetString(bytes));
                }
            }
        }

        private void SkipField(int bits)
        {
            int numeric = bits & FIELD_IS_NUMERIC_MASK;
            if (numeric != 0)
            {
                switch (numeric)
                {
                    case FIELD_IS_NUMERIC_INT:
                    case FIELD_IS_NUMERIC_FLOAT:
                        fieldsStream.ReadInt32();
                        return;

                    case FIELD_IS_NUMERIC_LONG:
                    case FIELD_IS_NUMERIC_DOUBLE:
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            // everything is stored on disk
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CheckIntegrity()
        {
        }
    }
}