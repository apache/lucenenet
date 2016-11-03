using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using Lucene.Net.Support;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;

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
    /// <p/>
    /// It uses &lt;segment&gt;.fdt and &lt;segment&gt;.fdx; files.
    /// </summary>
    /// @deprecated Only for reading existing 3.x indexes
    [Obsolete("Only for reading existing 3.x indexes")]
    public sealed class Lucene3xStoredFieldsReader : StoredFieldsReader, IDisposable
    {
        private const int FORMAT_SIZE = 4;

        /// <summary>
        /// Extension of stored fields file </summary>
        public const string FIELDS_EXTENSION = "fdt";

        /// <summary>
        /// Extension of stored fields index file </summary>
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
        public static readonly int FIELD_IS_BINARY = 1 << 1;

        // the old bit 1 << 2 was compressed, is now left out

        private const int _NUMERIC_BIT_SHIFT = 3;
        internal static readonly int FIELD_IS_NUMERIC_MASK = 0x07 << _NUMERIC_BIT_SHIFT;

        public const int FIELD_IS_NUMERIC_INT = 1 << _NUMERIC_BIT_SHIFT;
        public const int FIELD_IS_NUMERIC_LONG = 2 << _NUMERIC_BIT_SHIFT;
        public const int FIELD_IS_NUMERIC_FLOAT = 3 << _NUMERIC_BIT_SHIFT;
        public const int FIELD_IS_NUMERIC_DOUBLE = 4 << _NUMERIC_BIT_SHIFT;

        private readonly FieldInfos FieldInfos;
        private readonly IndexInput FieldsStream;
        private readonly IndexInput IndexStream;
        private int NumTotalDocs;
        private int Size;
        private bool Closed;
        private readonly int Format;

        // The docID offset where our docs begin in the index
        // file.  this will be 0 if we have our own private file.
        private int DocStoreOffset;

        // when we are inside a compound share doc store (CFX),
        // (lucene 3.0 indexes only), we privately open our own fd.
        private readonly CompoundFileDirectory StoreCFSReader;

        /// <summary>
        /// Returns a cloned FieldsReader that shares open
        ///  IndexInputs with the original one.  It is the caller's
        ///  job not to close the original FieldsReader until all
        ///  clones are called (eg, currently SegmentReader manages
        ///  this logic).
        /// </summary>
        public override object Clone()
        {
            EnsureOpen();
            return new Lucene3xStoredFieldsReader(FieldInfos, NumTotalDocs, Size, Format, DocStoreOffset, (IndexInput)FieldsStream.Clone(), (IndexInput)IndexStream.Clone());
        }

        /// <summary>
        /// Verifies that the code version which wrote the segment is supported. </summary>
        public static void CheckCodeVersion(Directory dir, string segment)
        {
            string indexStreamFN = IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION);
            IndexInput idxStream = dir.OpenInput(indexStreamFN, IOContext.DEFAULT);

            try
            {
                int format = idxStream.ReadInt();
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
            this.FieldInfos = fieldInfos;
            this.NumTotalDocs = numTotalDocs;
            this.Size = size;
            this.Format = format;
            this.DocStoreOffset = docStoreOffset;
            this.FieldsStream = fieldsStream;
            this.IndexStream = indexStream;
            this.StoreCFSReader = null;
        }

        public Lucene3xStoredFieldsReader(Directory d, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            string segment = Lucene3xSegmentInfoFormat.GetDocStoreSegment(si);
            int docStoreOffset = Lucene3xSegmentInfoFormat.GetDocStoreOffset(si);
            int size = si.DocCount;
            bool success = false;
            FieldInfos = fn;
            try
            {
                if (docStoreOffset != -1 && Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(si))
                {
                    d = StoreCFSReader = new CompoundFileDirectory(si.Dir, IndexFileNames.SegmentFileName(segment, "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION), context, false);
                }
                else
                {
                    StoreCFSReader = null;
                }
                FieldsStream = d.OpenInput(IndexFileNames.SegmentFileName(segment, "", FIELDS_EXTENSION), context);
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION);
                IndexStream = d.OpenInput(indexStreamFN, context);

                Format = IndexStream.ReadInt();

                if (Format < FORMAT_MINIMUM)
                {
                    throw new IndexFormatTooOldException(IndexStream, Format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }
                if (Format > FORMAT_CURRENT)
                {
                    throw new IndexFormatTooNewException(IndexStream, Format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }

                long indexSize = IndexStream.Length() - FORMAT_SIZE;

                if (docStoreOffset != -1)
                {
                    // We read only a slice out of this shared fields file
                    this.DocStoreOffset = docStoreOffset;
                    this.Size = size;

                    // Verify the file is long enough to hold all of our
                    // docs
                    Debug.Assert(((int)(indexSize / 8)) >= size + this.DocStoreOffset, "indexSize=" + indexSize + " size=" + size + " docStoreOffset=" + docStoreOffset);
                }
                else
                {
                    this.DocStoreOffset = 0;
                    this.Size = (int)(indexSize >> 3);
                    // Verify two sources of "maxDoc" agree:
                    if (this.Size != si.DocCount)
                    {
                        throw new CorruptIndexException("doc counts differ for segment " + segment + ": fieldsReader shows " + this.Size + " but segmentInfo shows " + si.DocCount);
                    }
                }
                NumTotalDocs = (int)(indexSize >> 3);
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
                    catch (Exception t)
                    {
                    }
                }
            }
        }

        /// <exception cref="AlreadyClosedException"> if this FieldsReader is closed </exception>
        private void EnsureOpen()
        {
            if (Closed)
            {
                throw new AlreadyClosedException("this FieldsReader is closed");
            }
        }

        /// <summary>
        /// Closes the underlying <seealso cref="Lucene.Net.Store.IndexInput"/> streams.
        /// this means that the Fields values will not be accessible.
        /// </summary>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!Closed)
                {
                    IOUtils.Close(FieldsStream, IndexStream, StoreCFSReader);
                    Closed = true;
                }
            }
        }

        private void SeekIndex(int docID)
        {
            IndexStream.Seek(FORMAT_SIZE + (docID + DocStoreOffset) * 8L);
        }

        public override void VisitDocument(int n, StoredFieldVisitor visitor)
        {
            SeekIndex(n);
            FieldsStream.Seek(IndexStream.ReadLong());

            int numFields = FieldsStream.ReadVInt();
            for (int fieldIDX = 0; fieldIDX < numFields; fieldIDX++)
            {
                int fieldNumber = FieldsStream.ReadVInt();
                FieldInfo fieldInfo = FieldInfos.FieldInfo(fieldNumber);

                int bits = FieldsStream.ReadByte() & 0xFF;
                Debug.Assert(bits <= (FIELD_IS_NUMERIC_MASK | FIELD_IS_BINARY), "bits=" + bits.ToString("x"));

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
                        visitor.IntField(info, FieldsStream.ReadInt());
                        return;

                    case FIELD_IS_NUMERIC_LONG:
                        visitor.LongField(info, FieldsStream.ReadLong());
                        return;

                    case FIELD_IS_NUMERIC_FLOAT:
                        visitor.FloatField(info, Number.IntBitsToFloat(FieldsStream.ReadInt()));
                        return;

                    case FIELD_IS_NUMERIC_DOUBLE:
                        visitor.DoubleField(info, BitConverter.Int64BitsToDouble(FieldsStream.ReadLong()));
                        return;

                    default:
                        throw new CorruptIndexException("Invalid numeric type: " + numeric.ToString("x"));
                }
            }
            else
            {
                int length = FieldsStream.ReadVInt();
                var bytes = new byte[length];
                FieldsStream.ReadBytes(bytes, 0, length);
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
                        FieldsStream.ReadInt();
                        return;

                    case FIELD_IS_NUMERIC_LONG:
                    case FIELD_IS_NUMERIC_DOUBLE:
                        FieldsStream.ReadLong();
                        return;

                    default:
                        throw new CorruptIndexException("Invalid numeric type: " + numeric.ToString("x"));
                }
            }
            else
            {
                int length = FieldsStream.ReadVInt();
                FieldsStream.Seek(FieldsStream.FilePointer + length);
            }
        }

        public override long RamBytesUsed()
        {
            // everything is stored on disk
            return 0;
        }

        public override void CheckIntegrity()
        {
        }
    }
}