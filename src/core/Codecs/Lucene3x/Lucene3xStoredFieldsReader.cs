using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    internal sealed class Lucene3xStoredFieldsReader : StoredFieldsReader, ICloneable, IDisposable
    {
        private const int FORMAT_SIZE = 4;

        /** Extension of stored fields file */
        public const string FIELDS_EXTENSION = "fdt";

        /** Extension of stored fields index file */
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
        private readonly IndexInput fieldsStream;
        private readonly IndexInput indexStream;
        private int numTotalDocs;
        private int size;
        private bool closed;
        private readonly int format;

        // The docID offset where our docs begin in the index
        // file.  This will be 0 if we have our own private file.
        private int docStoreOffset;

        // when we are inside a compound share doc store (CFX),
        // (lucene 3.0 indexes only), we privately open our own fd.
        private readonly CompoundFileDirectory storeCFSReader;

        public override object Clone()
        {
            EnsureOpen();
            return new Lucene3xStoredFieldsReader(fieldInfos, numTotalDocs, size, format, docStoreOffset, (IndexInput)fieldsStream.Clone(), (IndexInput)indexStream.Clone());
        }

        public static void CheckCodeVersion(Directory dir, String segment)
        {
            String indexStreamFN = IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION);
            IndexInput idxStream = dir.OpenInput(indexStreamFN, IOContext.DEFAULT);

            try
            {
                int format = idxStream.ReadInt();
                if (format < FORMAT_MINIMUM)
                    throw new IndexFormatTooOldException(idxStream, format, FORMAT_MINIMUM, FORMAT_CURRENT);
                if (format > FORMAT_CURRENT)
                    throw new IndexFormatTooNewException(idxStream, format, FORMAT_MINIMUM, FORMAT_CURRENT);
            }
            finally
            {
                idxStream.Dispose();
            }
        }

        // Used only by clone
        private Lucene3xStoredFieldsReader(FieldInfos fieldInfos, int numTotalDocs, int size, int format, int docStoreOffset,
                             IndexInput fieldsStream, IndexInput indexStream)
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
            String segment = Lucene3xSegmentInfoFormat.GetDocStoreSegment(si);
            int docStoreOffset = Lucene3xSegmentInfoFormat.GetDocStoreOffset(si);
            int size = si.DocCount;
            bool success = false;
            fieldInfos = fn;
            try
            {
                if (docStoreOffset != -1 && Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(si))
                {
                    d = storeCFSReader = new CompoundFileDirectory(si.dir,
                        IndexFileNames.SegmentFileName(segment, "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION), context, false);
                }
                else
                {
                    storeCFSReader = null;
                }
                fieldsStream = d.OpenInput(IndexFileNames.SegmentFileName(segment, "", FIELDS_EXTENSION), context);
                String indexStreamFN = IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION);
                indexStream = d.OpenInput(indexStreamFN, context);

                format = indexStream.ReadInt();

                if (format < FORMAT_MINIMUM)
                    throw new IndexFormatTooOldException(indexStream, format, FORMAT_MINIMUM, FORMAT_CURRENT);
                if (format > FORMAT_CURRENT)
                    throw new IndexFormatTooNewException(indexStream, format, FORMAT_MINIMUM, FORMAT_CURRENT);

                long indexSize = indexStream.Length - FORMAT_SIZE;

                if (docStoreOffset != -1)
                {
                    // We read only a slice out of this shared fields file
                    this.docStoreOffset = docStoreOffset;
                    this.size = size;

                    // Verify the file is long enough to hold all of our
                    // docs
                    //assert ((int) (indexSize / 8)) >= size + this.docStoreOffset: "indexSize=" + indexSize + " size=" + size + " docStoreOffset=" + docStoreOffset;
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
                    }
                    catch { } // keep our original exception
                }
            }
        }

        private void EnsureOpen()
        {
            if (closed)
            {
                throw new AlreadyClosedException("this FieldsReader is closed");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!closed)
                {
                    IOUtils.Close(fieldsStream, indexStream, storeCFSReader);
                    closed = true;
                }
            }
        }

        private void SeekIndex(int docID)
        {
            indexStream.Seek(FORMAT_SIZE + (docID + docStoreOffset) * 8L);
        }

        public override void VisitDocument(int n, StoredFieldVisitor visitor)
        {
            SeekIndex(n);
            fieldsStream.Seek(indexStream.ReadLong());

            int numFields = fieldsStream.ReadVInt();
            for (int fieldIDX = 0; fieldIDX < numFields; fieldIDX++)
            {
                int fieldNumber = fieldsStream.ReadVInt();
                FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);

                int bits = fieldsStream.ReadByte() & 0xFF;
                //assert bits <= (FIELD_IS_NUMERIC_MASK | FIELD_IS_BINARY): "bits=" + Integer.toHexString(bits);

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
                        visitor.IntField(info, fieldsStream.ReadInt());
                        return;
                    case FIELD_IS_NUMERIC_LONG:
                        visitor.LongField(info, fieldsStream.ReadLong());
                        return;
                    case FIELD_IS_NUMERIC_FLOAT:
                        visitor.FloatField(info, Number.IntBitsToFloat(fieldsStream.ReadInt()));
                        return;
                    case FIELD_IS_NUMERIC_DOUBLE:
                        visitor.DoubleField(info, BitConverter.Int64BitsToDouble(fieldsStream.ReadLong()));
                        return;
                    default:
                        throw new CorruptIndexException("Invalid numeric type: " + numeric.ToString("X"));
                }
            }
            else
            {
                int length = fieldsStream.ReadVInt();
                sbyte[] bytes = new sbyte[length];
                fieldsStream.ReadBytes(bytes, 0, length);
                if ((bits & FIELD_IS_BINARY) != 0)
                {
                    visitor.BinaryField(info, bytes);
                }
                else
                {
                    visitor.StringField(info, IOUtils.CHARSET_UTF_8.GetString((byte[])(Array)bytes));
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
                        fieldsStream.ReadInt();
                        return;
                    case FIELD_IS_NUMERIC_LONG:
                    case FIELD_IS_NUMERIC_DOUBLE:
                        fieldsStream.ReadLong();
                        return;
                    default:
                        throw new CorruptIndexException("Invalid numeric type: " + numeric.ToString("X"));
                }
            }
            else
            {
                int length = fieldsStream.ReadVInt();
                fieldsStream.Seek(fieldsStream.FilePointer + length);
            }
        }
    }
}
