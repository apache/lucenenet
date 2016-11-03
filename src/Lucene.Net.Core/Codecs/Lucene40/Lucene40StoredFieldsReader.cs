using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene40
{
    using Lucene.Net.Support;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;

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
    /// <p/>
    /// It uses &lt;segment&gt;.fdt and &lt;segment&gt;.fdx; files.
    /// </summary>
    /// <seealso cref= Lucene40StoredFieldsFormat
    /// @lucene.internal </seealso>
    public sealed class Lucene40StoredFieldsReader : StoredFieldsReader, IDisposable
    {
        private readonly FieldInfos FieldInfos;
        private readonly IndexInput FieldsStream;
        private readonly IndexInput IndexStream;
        private int NumTotalDocs;
        private int Size_Renamed;
        private bool Closed;

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
            return new Lucene40StoredFieldsReader(FieldInfos, NumTotalDocs, Size_Renamed, (IndexInput)FieldsStream.Clone(), (IndexInput)IndexStream.Clone());
        }

        /// <summary>
        /// Used only by clone. </summary>
        private Lucene40StoredFieldsReader(FieldInfos fieldInfos, int numTotalDocs, int size, IndexInput fieldsStream, IndexInput indexStream)
        {
            this.FieldInfos = fieldInfos;
            this.NumTotalDocs = numTotalDocs;
            this.Size_Renamed = size;
            this.FieldsStream = fieldsStream;
            this.IndexStream = indexStream;
        }

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40StoredFieldsReader(Directory d, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            string segment = si.Name;
            bool success = false;
            FieldInfos = fn;
            try
            {
                FieldsStream = d.OpenInput(IndexFileNames.SegmentFileName(segment, "", Lucene40StoredFieldsWriter.FIELDS_EXTENSION), context);
                string indexStreamFN = IndexFileNames.SegmentFileName(segment, "", Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);
                IndexStream = d.OpenInput(indexStreamFN, context);

                CodecUtil.CheckHeader(IndexStream, Lucene40StoredFieldsWriter.CODEC_NAME_IDX, Lucene40StoredFieldsWriter.VERSION_START, Lucene40StoredFieldsWriter.VERSION_CURRENT);
                CodecUtil.CheckHeader(FieldsStream, Lucene40StoredFieldsWriter.CODEC_NAME_DAT, Lucene40StoredFieldsWriter.VERSION_START, Lucene40StoredFieldsWriter.VERSION_CURRENT);
                Debug.Assert(Lucene40StoredFieldsWriter.HEADER_LENGTH_DAT == FieldsStream.FilePointer);
                Debug.Assert(Lucene40StoredFieldsWriter.HEADER_LENGTH_IDX == IndexStream.FilePointer);
                long indexSize = IndexStream.Length() - Lucene40StoredFieldsWriter.HEADER_LENGTH_IDX;
                this.Size_Renamed = (int)(indexSize >> 3);
                // Verify two sources of "maxDoc" agree:
                if (this.Size_Renamed != si.DocCount)
                {
                    throw new CorruptIndexException("doc counts differ for segment " + segment + ": fieldsReader shows " + this.Size_Renamed + " but segmentInfo shows " + si.DocCount);
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
                    } // ensure we throw our original exception
                    catch (Exception)
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
        /// <exception cref="IOException"> If an I/O error occurs </exception>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!Closed)
                {
                    IOUtils.Close(FieldsStream, IndexStream);
                    Closed = true;
                }
            }
        }

        /// <summary>
        /// Returns number of documents. </summary>
        public int Size()
        {
            return Size_Renamed;
        }

        private void SeekIndex(int docID)
        {
            IndexStream.Seek(Lucene40StoredFieldsWriter.HEADER_LENGTH_IDX + docID * 8L);
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
                Debug.Assert(bits <= (Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_MASK | Lucene40StoredFieldsWriter.FIELD_IS_BINARY), "bits=" + bits.ToString("x"));

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
                        visitor.IntField(info, FieldsStream.ReadInt());
                        return;

                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_LONG:
                        visitor.LongField(info, FieldsStream.ReadLong());
                        return;

                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_FLOAT:
                        visitor.FloatField(info, Number.IntBitsToFloat(FieldsStream.ReadInt()));
                        return;

                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_DOUBLE:
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
                if ((bits & Lucene40StoredFieldsWriter.FIELD_IS_BINARY) != 0)
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
            int numeric = bits & Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_MASK;
            if (numeric != 0)
            {
                switch (numeric)
                {
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_INT:
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_FLOAT:
                        FieldsStream.ReadInt();
                        return;

                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_LONG:
                    case Lucene40StoredFieldsWriter.FIELD_IS_NUMERIC_DOUBLE:
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

        /// <summary>
        /// Returns the length in bytes of each raw document in a
        ///  contiguous range of length numDocs starting with
        ///  startDocID.  Returns the IndexInput (the fieldStream),
        ///  already seeked to the starting point for startDocID.
        /// </summary>
        public IndexInput RawDocs(int[] lengths, int startDocID, int numDocs)
        {
            SeekIndex(startDocID);
            long startOffset = IndexStream.ReadLong();
            long lastOffset = startOffset;
            int count = 0;
            while (count < numDocs)
            {
                long offset;
                int docID = startDocID + count + 1;
                Debug.Assert(docID <= NumTotalDocs);
                if (docID < NumTotalDocs)
                {
                    offset = IndexStream.ReadLong();
                }
                else
                {
                    offset = FieldsStream.Length();
                }
                lengths[count++] = (int)(offset - lastOffset);
                lastOffset = offset;
            }

            FieldsStream.Seek(startOffset);

            return FieldsStream;
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