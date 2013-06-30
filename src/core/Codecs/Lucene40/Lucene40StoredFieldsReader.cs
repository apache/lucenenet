using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    public sealed class Lucene40StoredFieldsReader : StoredFieldsReader, ICloneable, IDisposable
    {
        private readonly FieldInfos fieldInfos;
        private readonly IndexInput fieldsStream;
        private readonly IndexInput indexStream;
        private int numTotalDocs;
        private int size;
        private bool closed;

        public override object Clone()
        {
            EnsureOpen();
            return new Lucene40StoredFieldsReader(fieldInfos, numTotalDocs, size, (IndexInput)fieldsStream.Clone(), (IndexInput)indexStream.Clone());
        }

        private Lucene40StoredFieldsReader(FieldInfos fieldInfos, int numTotalDocs, int size, IndexInput fieldsStream, IndexInput indexStream)
        {
            this.fieldInfos = fieldInfos;
            this.numTotalDocs = numTotalDocs;
            this.size = size;
            this.fieldsStream = fieldsStream;
            this.indexStream = indexStream;
        }

        public Lucene40StoredFieldsReader(Directory d, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            String segment = si.name;
            bool success = false;
            fieldInfos = fn;
            try
            {
                fieldsStream = d.OpenInput(IndexFileNames.SegmentFileName(segment, "", Lucene40StoredFieldsWriter.FIELDS_EXTENSION), context);
                String indexStreamFN = IndexFileNames.SegmentFileName(segment, "", Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);
                indexStream = d.OpenInput(indexStreamFN, context);

                CodecUtil.CheckHeader(indexStream, Lucene40StoredFieldsWriter.CODEC_NAME_IDX, Lucene40StoredFieldsWriter.VERSION_START, Lucene40StoredFieldsWriter.VERSION_CURRENT);
                CodecUtil.CheckHeader(fieldsStream, Lucene40StoredFieldsWriter.CODEC_NAME_DAT, Lucene40StoredFieldsWriter.VERSION_START, Lucene40StoredFieldsWriter.VERSION_CURRENT);
                //assert HEADER_LENGTH_DAT == fieldsStream.getFilePointer();
                //assert HEADER_LENGTH_IDX == indexStream.getFilePointer();
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
                    }
                    catch { } // ensure we throw our original exception
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
                    IOUtils.Close(fieldsStream, indexStream);
                    closed = true;
                }
            }
        }

        public int Size
        {
            get { return size; }
        }

        private void SeekIndex(int docID)
        {
            indexStream.Seek(Lucene40StoredFieldsWriter.HEADER_LENGTH_IDX + docID * 8L);
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
            int numeric = bits & Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_MASK;
            if (numeric != 0)
            {
                switch (numeric)
                {
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_INT:
                        visitor.IntField(info, fieldsStream.ReadInt());
                        return;
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_LONG:
                        visitor.LongField(info, fieldsStream.ReadLong());
                        return;
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_FLOAT:
                        visitor.FloatField(info, Number.IntBitsToFloat(fieldsStream.ReadInt()));
                        return;
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_DOUBLE:
                        visitor.DoubleField(info, BitConverter.Int64BitsToDouble(fieldsStream.ReadLong()));
                        return;
                    default:
                        throw new CorruptIndexException("Invalid numeric type: " + numeric.ToString("X"));
                }
            }
            else
            {
                int length = fieldsStream.ReadVInt();
                byte[] bytes = new byte[length];
                fieldsStream.ReadBytes(bytes, 0, length);
                if ((bits & Lucene40StoredFieldsWriter.FIELD_IS_BINARY) != 0)
                {
                    visitor.BinaryField(info, (sbyte[])(Array)bytes);
                }
                else
                {
                    visitor.StringField(info, IOUtils.CHARSET_UTF_8.GetString(bytes));
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
                        fieldsStream.ReadInt();
                        return;
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_LONG:
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_DOUBLE:
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

        public IndexInput RawDocs(int[] lengths, int startDocID, int numDocs)
        {
            SeekIndex(startDocID);
            long startOffset = indexStream.ReadLong();
            long lastOffset = startOffset;
            int count = 0;
            while (count < numDocs)
            {
                long offset;
                int docID = startDocID + count + 1;
                //assert docID <= numTotalDocs;
                if (docID < numTotalDocs)
                    offset = indexStream.ReadLong();
                else
                    offset = fieldsStream.Length;
                lengths[count++] = (int)(offset - lastOffset);
                lastOffset = offset;
            }

            fieldsStream.Seek(startOffset);

            return fieldsStream;
        }
    }
}
